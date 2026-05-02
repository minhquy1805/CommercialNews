using System.Text.Json;
using Audit.Application.Consumers.Authorization;
using Audit.Application.Consumers.Authorization.Payloads;
using Audit.Application.Contracts.Ingestion;
using CommercialNews.BuildingBlocks.Outbox.IntegrationEvents;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.Worker.Audit.Handlers;

namespace CommercialNews.Worker.Audit.Handlers.Authorization;

public sealed class AuthorizationUserRoleRevokedAuditHandler : IAuditIntegrationEventHandler
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly IAuthorizationAuditEventIngestionService _ingestionService;
    private readonly ILogger<AuthorizationUserRoleRevokedAuditHandler> _logger;

    public AuthorizationUserRoleRevokedAuditHandler(
        IAuthorizationAuditEventIngestionService ingestionService,
        ILogger<AuthorizationUserRoleRevokedAuditHandler> logger)
    {
        _ingestionService = ingestionService
            ?? throw new ArgumentNullException(nameof(ingestionService));

        _logger = logger
            ?? throw new ArgumentNullException(nameof(logger));
    }

    public string EventType => "authorization.user_role_revoked";

    public async Task<Result> HandleAsync(
        OutboxIntegrationEventEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        UserRoleRevokedAuditPayload? payload;

        try
        {
            payload = envelope.Payload.Deserialize<UserRoleRevokedAuditPayload>(JsonOptions);
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(
                exception,
                "Failed to deserialize authorization user-role revoked audit payload. MessageId={MessageId}, EventType={EventType}",
                envelope.MessageId,
                envelope.EventType);

            return Result.Failure(
                Error.Validation(
                    code: "AUDIT.AUTHORIZATION.USER_ROLE_REVOKED_PAYLOAD_INVALID",
                    message: "Authorization user-role revoked audit payload is invalid."));
        }

        if (payload is null)
        {
            return Result.Failure(
                Error.Validation(
                    code: "AUDIT.AUTHORIZATION.USER_ROLE_REVOKED_PAYLOAD_REQUIRED",
                    message: "Authorization user-role revoked audit payload is required."));
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
            await _ingestionService.IngestUserRoleRevokedAsync(
                context,
                payload,
                cancellationToken);

        if (result.IsFailure)
        {
            var error = result.Error!;

            _logger.LogWarning(
                "Failed to ingest authorization user-role revoked audit event. MessageId={MessageId}, EventType={EventType}, ErrorCode={ErrorCode}, ErrorMessage={ErrorMessage}",
                envelope.MessageId,
                envelope.EventType,
                error.Code,
                error.Message);

            return Result.Failure(error);
        }

        var ingestionResult = result.Value!;

        _logger.LogInformation(
            "Authorization user-role revoked audit event ingested. MessageId={MessageId}, AuditId={AuditId}, WasInserted={WasInserted}, WasDeduped={WasDeduped}",
            envelope.MessageId,
            ingestionResult.AuditId,
            ingestionResult.WasInserted,
            ingestionResult.WasDeduped);

        return Result.Success();
    }
}