using Content.Domain.Exceptions;

namespace Content.Domain.Entities
{
    public sealed class Tag
    {
        private const int PublicIdLength = 26;
        private const int NameMaxLength = 150;
        private const int NameNormalizedMaxLength = 150;
        private const int DescriptionMaxLength = 500;

        private Tag(
            long tagId,
            string publicId,
            string name,
            string nameNormalized,
            string? description,
            bool isActive,
            DateTime createdAt,
            DateTime updatedAt,
            long? createdByUserId,
            long? updatedByUserId,
            bool isDeleted,
            DateTime? deletedAt,
            long? deletedByUserId,
            int version)
        {
            TagId = tagId;
            PublicId = publicId;
            Name = name;
            NameNormalized = nameNormalized;
            Description = description;
            IsActive = isActive;
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
            CreatedByUserId = createdByUserId;
            UpdatedByUserId = updatedByUserId;
            IsDeleted = isDeleted;
            DeletedAt = deletedAt;
            DeletedByUserId = deletedByUserId;
            Version = version;
        }

        public long TagId { get; private set; }
        public string PublicId { get; private set; }
        public string Name { get; private set; }
        public string NameNormalized { get; private set; }
        public string? Description { get; private set; }
        public bool IsActive { get; private set; }

        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        public long? CreatedByUserId { get; private set; }
        public long? UpdatedByUserId { get; private set; }

        public bool IsDeleted { get; private set; }
        public DateTime? DeletedAt { get; private set; }
        public long? DeletedByUserId { get; private set; }

        public int Version { get; private set; }

        public static Tag Create(
            string publicId,
            string name,
            string nameNormalized,
            string? description,
            bool isActive,
            DateTime nowUtc,
            long? actorUserId)
        {
            ValidatePublicId(publicId);
            ValidateName(name);
            ValidateNameNormalized(nameNormalized);
            ValidateDescription(description);

            return new Tag(
                tagId: 0,
                publicId: publicId.Trim(),
                name: name.Trim(),
                nameNormalized: nameNormalized.Trim(),
                description: NormalizeDescription(description),
                isActive: isActive,
                createdAt: nowUtc,
                updatedAt: nowUtc,
                createdByUserId: actorUserId,
                updatedByUserId: actorUserId,
                isDeleted: false,
                deletedAt: null,
                deletedByUserId: null,
                version: 1);
        }

        public void Update(
            string name,
            string nameNormalized,
            string? description,
            bool isActive,
            DateTime nowUtc,
            long? actorUserId)
        {
            EnsureNotDeleted();

            ValidateName(name);
            ValidateNameNormalized(nameNormalized);
            ValidateDescription(description);

            Name = name.Trim();
            NameNormalized = nameNormalized.Trim();
            Description = NormalizeDescription(description);
            IsActive = isActive;
            UpdatedAt = nowUtc;
            UpdatedByUserId = actorUserId;
            Version++;
        }

        public void SoftDelete(
            DateTime nowUtc,
            long? actorUserId)
        {
            if (IsDeleted)
            {
                throw new ContentDomainException(
                    "CONTENT.TAG_ALREADY_DELETED",
                    "The tag has already been deleted.");
            }

            IsDeleted = true;
            DeletedAt = nowUtc;
            DeletedByUserId = actorUserId;
            UpdatedAt = nowUtc;
            UpdatedByUserId = actorUserId;
            Version++;
        }

        public void Restore(
            DateTime nowUtc,
            long? actorUserId)
        {
            if (!IsDeleted)
            {
                throw new ContentDomainException(
                    "CONTENT.TAG_NOT_DELETED",
                    "The tag is not deleted.");
            }

            IsDeleted = false;
            DeletedAt = null;
            DeletedByUserId = null;
            UpdatedAt = nowUtc;
            UpdatedByUserId = actorUserId;
            Version++;
        }

        public static Tag Rehydrate(
            long tagId,
            string publicId,
            string name,
            string nameNormalized,
            string? description,
            bool isActive,
            DateTime createdAt,
            DateTime updatedAt,
            long? createdByUserId,
            long? updatedByUserId,
            bool isDeleted,
            DateTime? deletedAt,
            long? deletedByUserId,
            int version)
        {
            if (tagId <= 0)
            {
                throw new ContentDomainException(
                    "CONTENT.TAG_INVALID_TAG_ID",
                    "TagId must be greater than 0.");
            }

            ValidatePublicId(publicId);
            ValidateName(name);
            ValidateNameNormalized(nameNormalized);
            ValidateDescription(description);

            if (version <= 0)
            {
                throw new ContentDomainException(
                    "CONTENT.TAG_INVALID_VERSION",
                    "Version must be greater than 0.");
            }

            if (updatedAt < createdAt)
            {
                throw new ContentDomainException(
                    "CONTENT.TAG_INVALID_UPDATED_AT",
                    "UpdatedAt cannot be earlier than CreatedAt.");
            }

            if (deletedAt.HasValue && deletedAt.Value < createdAt)
            {
                throw new ContentDomainException(
                    "CONTENT.TAG_INVALID_DELETED_AT",
                    "DeletedAt cannot be earlier than CreatedAt.");
            }

            return new Tag(
                tagId: tagId,
                publicId: publicId.Trim(),
                name: name.Trim(),
                nameNormalized: nameNormalized.Trim(),
                description: NormalizeDescription(description),
                isActive: isActive,
                createdAt: createdAt,
                updatedAt: updatedAt,
                createdByUserId: createdByUserId,
                updatedByUserId: updatedByUserId,
                isDeleted: isDeleted,
                deletedAt: deletedAt,
                deletedByUserId: deletedByUserId,
                version: version);
        }

        private void EnsureNotDeleted()
        {
            if (IsDeleted)
            {
                throw new ContentDomainException(
                    "CONTENT.TAG_ALREADY_DELETED",
                    "The tag has already been deleted.");
            }
        }

        private static void ValidatePublicId(string publicId)
        {
            if (string.IsNullOrWhiteSpace(publicId))
            {
                throw new ContentDomainException(
                    "CONTENT.TAG_PUBLIC_ID_REQUIRED",
                    "PublicId is required.");
            }

            string trimmed = publicId.Trim();

            if (trimmed.Length != PublicIdLength)
            {
                throw new ContentDomainException(
                    "CONTENT.TAG_PUBLIC_ID_INVALID",
                    $"PublicId must be exactly {PublicIdLength} characters.");
            }
        }

        private static void ValidateName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ContentDomainException(
                    "CONTENT.TAG_NAME_REQUIRED",
                    "Name is required.");
            }

            if (name.Trim().Length > NameMaxLength)
            {
                throw new ContentDomainException(
                    "CONTENT.TAG_NAME_TOO_LONG",
                    $"Name cannot exceed {NameMaxLength} characters.");
            }
        }

        private static void ValidateNameNormalized(string nameNormalized)
        {
            if (string.IsNullOrWhiteSpace(nameNormalized))
            {
                throw new ContentDomainException(
                    "CONTENT.TAG_NAME_NORMALIZED_REQUIRED",
                    "NameNormalized is required.");
            }

            if (nameNormalized.Trim().Length > NameNormalizedMaxLength)
            {
                throw new ContentDomainException(
                    "CONTENT.TAG_NAME_NORMALIZED_TOO_LONG",
                    $"NameNormalized cannot exceed {NameNormalizedMaxLength} characters.");
            }
        }

        private static void ValidateDescription(string? description)
        {
            if (description is not null && description.Trim().Length > DescriptionMaxLength)
            {
                throw new ContentDomainException(
                    "CONTENT.TAG_DESCRIPTION_TOO_LONG",
                    $"Description cannot exceed {DescriptionMaxLength} characters.");
            }
        }

        private static string? NormalizeDescription(string? description)
        {
            if (string.IsNullOrWhiteSpace(description))
            {
                return null;
            }

            return description.Trim();
        }
    }
}