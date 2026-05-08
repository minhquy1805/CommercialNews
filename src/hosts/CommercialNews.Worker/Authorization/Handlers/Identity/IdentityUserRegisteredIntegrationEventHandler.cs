using System.Text.Json;
using Authorization.Application.Consumers.Identity;
using Authorization.Application.Consumers.Identity.Payloads;
using CommercialNews.BuildingBlocks.Outbox.IntegrationEvents;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Identity.Application.Outbox;

namespace CommercialNews.Worker.Authorization.Handlers.Identity;

public sealed class IdentityUserRegisteredIntegrationEventHandler
    : IAuthorizationIntegrationEventHandler
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly IIdentityUserRegisteredConsumerService _consumerService;
    private readonly ILogger<IdentityUserRegisteredIntegrationEventHandler> _logger;

    public IdentityUserRegisteredIntegrationEventHandler(
        IIdentityUserRegisteredConsumerService consumerService,
        ILogger<IdentityUserRegisteredIntegrationEventHandler> logger)
    {
        _consumerService = consumerService
            ?? throw new ArgumentNullException(nameof(consumerService));
        _logger = logger
            ?? throw new ArgumentNullException(nameof(logger));
    }

    public string EventType =>
        IdentityIntegrationEventTypes.UserRegistered;

    public async Task<Result> HandleAsync(
        OutboxIntegrationEventEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        IdentityUserRegisteredPayload? payload;

        try
        {
            payload = envelope.Payload.Deserialize<IdentityUserRegisteredPayload>(
                JsonOptions);
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(
                exception,
                "Failed to deserialize identity user registered payload. MessageId={MessageId}, EventType={EventType}",
                envelope.MessageId,
                envelope.EventType);

            return Result.Failure(
                Error.Validation(
                    code: "AUTHORIZATION.IDENTITY_USER_REGISTERED_PAYLOAD_INVALID",
                    message: "Identity user registered payload is invalid."));
        }

        if (payload is null)
        {
            return Result.Failure(
                Error.Validation(
                    code: "AUTHORIZATION.IDENTITY_USER_REGISTERED_PAYLOAD_REQUIRED",
                    message: "Identity user registered payload is required."));
        }

        Result<IdentityUserRegisteredRoleAssignmentResult> result =
            await _consumerService.AssignDefaultRoleAsync(
                messageId: envelope.MessageId,
                correlationId: envelope.CorrelationId,
                payload: payload,
                cancellationToken: cancellationToken);

        if (result.IsFailure)
        {
            return Result.Failure(result.Error!);
        }

        IdentityUserRegisteredRoleAssignmentResult assignmentResult = result.Value;

        _logger.LogInformation(
            "Identity user registered authorization event consumed. MessageId={MessageId}, UserId={UserId}, RoleId={RoleId}, WasAlreadyAssigned={WasAlreadyAssigned}",
            envelope.MessageId,
            assignmentResult.UserId,
            assignmentResult.RoleId,
            assignmentResult.WasAlreadyAssigned);

        return Result.Success();
    }
}
