namespace Authorization.Domain.Entities;

public sealed class Role
{
    public long RoleId { get; private set; }
    public string PublicId { get; private set; }
    public string Name { get; private set; }
    public string NameNormalized { get; private set; }
    public string? Description { get; private set; }

    public bool IsSystem { get; private set; }
    public bool IsActive { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public long? CreatedByUserId { get; private set; }
    public long? UpdatedByUserId { get; private set; }

    public Role(
        long roleId,
        string publicId,
        string name,
        string nameNormalized,
        string? description,
        bool isSystem,
        bool isActive,
        DateTime createdAt,
        DateTime updatedAt,
        long? createdByUserId,
        long? updatedByUserId)
    {
        if (roleId < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(roleId), "RoleId cannot be negative.");
        }

        if (string.IsNullOrWhiteSpace(publicId))
        {
            throw new ArgumentException("PublicId is required.", nameof(publicId));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Role name is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(nameNormalized))
        {
            throw new ArgumentException("Normalized role name is required.", nameof(nameNormalized));
        }

        if (updatedAt < createdAt)
        {
            throw new ArgumentException("UpdatedAt cannot be earlier than CreatedAt.");
        }

        RoleId = roleId;
        PublicId = publicId.Trim();
        Name = name.Trim();
        NameNormalized = nameNormalized.Trim();
        Description = NormalizeOptional(description);

        IsSystem = isSystem;
        IsActive = isActive;

        CreatedAt = createdAt;
        UpdatedAt = updatedAt;

        CreatedByUserId = createdByUserId;
        UpdatedByUserId = updatedByUserId;
    }

    public void Rename(
        string newName,
        string newNameNormalized,
        DateTime updatedAt,
        long? updatedByUserId)
    {
        if (string.IsNullOrWhiteSpace(newName))
        {
            throw new ArgumentException("Role name is required.", nameof(newName));
        }

        if (string.IsNullOrWhiteSpace(newNameNormalized))
        {
            throw new ArgumentException("Normalized role name is required.", nameof(newNameNormalized));
        }

        if (updatedAt < CreatedAt)
        {
            throw new ArgumentException("UpdatedAt cannot be earlier than CreatedAt.", nameof(updatedAt));
        }

        Name = newName.Trim();
        NameNormalized = newNameNormalized.Trim();
        UpdatedAt = updatedAt;
        UpdatedByUserId = updatedByUserId;
    }

    public void ChangeDescription(
        string? description,
        DateTime updatedAt,
        long? updatedByUserId)
    {
        if (updatedAt < CreatedAt)
        {
            throw new ArgumentException("UpdatedAt cannot be earlier than CreatedAt.", nameof(updatedAt));
        }

        Description = NormalizeOptional(description);
        UpdatedAt = updatedAt;
        UpdatedByUserId = updatedByUserId;
    }

    public void Activate(
        DateTime updatedAt,
        long? updatedByUserId)
    {
        if (updatedAt < CreatedAt)
        {
            throw new ArgumentException("UpdatedAt cannot be earlier than CreatedAt.", nameof(updatedAt));
        }

        IsActive = true;
        UpdatedAt = updatedAt;
        UpdatedByUserId = updatedByUserId;
    }

    public void Deactivate(
        DateTime updatedAt,
        long? updatedByUserId)
    {
        if (IsSystem)
        {
            throw new InvalidOperationException("System role cannot be deactivated.");
        }

        if (updatedAt < CreatedAt)
        {
            throw new ArgumentException("UpdatedAt cannot be earlier than CreatedAt.", nameof(updatedAt));
        }

        IsActive = false;
        UpdatedAt = updatedAt;
        UpdatedByUserId = updatedByUserId;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}