namespace Authorization.Application.Contracts.Ports
{
    public interface IUserRoleAssignmentService
    {
        Task<long> AssignAsync(
            long userId,
            long roleId,
            long? assignedByUserId,
            CancellationToken cancellationToken);
    }
}

