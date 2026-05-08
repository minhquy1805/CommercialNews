using Authorization.Application.Consumers.Identity.Payloads;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace Authorization.Application.Consumers.Identity;

public interface IIdentityUserRegisteredConsumerService
{
    Task<Result<IdentityUserRegisteredRoleAssignmentResult>> AssignDefaultRoleAsync(
        string messageId,
        string? correlationId,
        IdentityUserRegisteredPayload payload,
        CancellationToken cancellationToken = default);
}
