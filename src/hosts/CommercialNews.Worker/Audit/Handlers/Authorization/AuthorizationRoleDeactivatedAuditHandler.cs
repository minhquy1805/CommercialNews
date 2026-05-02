using System.Text.Json;
using Audit.Application.Consumers.Authorization;
using Audit.Application.Consumers.Authorization.Payloads;
using Audit.Application.Contracts.Ingestion;
using CommercialNews.BuildingBlocks.Outbox.IntegrationEvents;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.Worker.Audit.Handlers;

namespace CommercialNews.Worker.Audit.Handlers.Authorization;

public sealed class AuthorizationRoleDeactivatedAuditHandler : IAuditIntegrationEventHandler
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly IAuthorizationAuditEventIngestionService _ingestionService;
    private readonly ILogger<AuthorizationRoleDeactivatedAuditHandler> _logger;

    public AuthorizationRoleDeactivatedAuditHandler(
        IAuthorizationAuditEventIngestionService ingestionService,
        ILogger<AuthorizationRoleDeactivatedAuditHandler> logger)
    {
        _ingestionService = ingestionService
            ?? throw new ArgumentNullException(nameof(ingestionService));

        _logger = logger
            ?? throw new ArgumentNullException(nameof(logger));
    }

    public string EventType => "authorization.role_deactivated";

    public async Task<Result> HandleAsync(
        OutboxIntegrationEventEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        RoleDeactivatedAuditPayload? payload;

        try
        {
            payload = envelope.Payload.Deserialize<RoleDeactivatedAuditPayload>(JsonOptions);
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(
                exception,
                "Failed to deserialize authorization role deactivated audit payload. MessageId={MessageId}, EventType={EventType}",
                envelope.MessageId,
                envelope.EventType);

            return Result.Failure(
                Error.Validation(
                    code: "AUDIT.AUTHORIZATION.ROLE_DEACTIVATED_PAYLOAD_INVALID",
                    message: "Authorization role deactivated audit payload is invalid."));
        }

        if (payload is null)
        {
            return Result.Failure(
                Error.Validation(
                    code: "AUDIT.AUTHORIZATION.ROLE_DEACTIVATED_PAYLOAD_REQUIRED",
                    message: "Authorization role deactivated audit payload is required."));
        }

        AuthorizationAuditEnvelopeContext context = AuthorizationAuditEnvelopeContext.Create(
            messageId: envelope.MessageId,
            eventType: envelope.EventType,
            aggregateType: envelope.AggregateType,
            aggregateId: envelope.AggregateId,
            aggregatePublicId: envelope.AggregatePublicId,
            aggregateVersion: envelope.AggregateVersion,
            correlationId: envelope.CorrelationId,
            initiatorUserId: envelope.InitiatorUserId,
            occurredAtUtc: envelope.OccurredAtUtc);

        Result<AuditIngestionResult> result =
            await _ingestionService.IngestRoleDeactivatedAsync(
                context,
                payload,
                cancellationToken);

        if (result.IsFailure)
        {
            var error = result.Error!;

            _logger.LogWarning(
                "Failed to ingest authorization role deactivated audit event. MessageId={MessageId}, EventType={EventType}, ErrorCode={ErrorCode}, ErrorMessage={ErrorMessage}",
                envelope.MessageId,
                envelope.EventType,
                error.Code,
                error.Message);

            return Result.Failure(error);
        }

        var ingestionResult = result.Value!;

        _logger.LogInformation(
            "Authorization role deactivated audit event ingested. MessageId={MessageId}, AuditId={AuditId}, WasInserted={WasInserted}, WasDeduped={WasDeduped}",
            envelope.MessageId,
            ingestionResult.AuditId,
            ingestionResult.WasInserted,
            ingestionResult.WasDeduped);

        return Result.Success();
    }
}