namespace Content.Application.Ports.Persistence
{
    public interface ICategoryRepository
    {
        Task<bool> ExistsByIdAsync(
            long categoryId,
            CancellationToken cancellationToken = default);
    }
}

