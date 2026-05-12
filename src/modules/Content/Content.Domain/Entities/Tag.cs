using Content.Domain.Common;
using Content.Domain.Constants;
using Content.Domain.Exceptions;

namespace Content.Domain.Entities
{
    public sealed class Tag : ContentSoftDeletableEntity
    {
        private Tag()
        {
        }

        public long TagId { get; private set; }
        public string PublicId { get; private set; } = string.Empty;
        public string Name { get; private set; } = string.Empty;
        public string NameNormalized { get; private set; } = string.Empty;
        public string? Description { get; private set; }
        public bool IsActive { get; private set; }

        public bool CanBeAttachedToArticle => IsActive && !IsDeleted;

        public static Tag Create(
            string publicId,
            string name,
            string nameNormalized,
            string? description,
            bool isActive,
            DateTime nowUtc,
            long? actorUserId)
        {
            string normalizedPublicId = ValidatePublicId(publicId);
            ValidateName(name);
            ValidateNameNormalized(nameNormalized);
            ValidateDescription(description);

            var tag = new Tag
            {
                PublicId = normalizedPublicId,
                Name = ContentText.NormalizeRequired(name),
                NameNormalized = ContentText.NormalizeRequired(nameNormalized),
                Description = ContentText.NormalizeOptional(description),
                IsActive = isActive
            };

            tag.InitializeTracking(nowUtc, actorUserId);
            tag.InitializeDeletion();

            return tag;
        }

        public void Update(
            string name,
            string nameNormalized,
            string? description,
            bool isActive,
            DateTime nowUtc,
            long? actorUserId)
        {
            EnsureTagNotDeleted();

            ValidateName(name);
            ValidateNameNormalized(nameNormalized);
            ValidateDescription(description);

            Name = ContentText.NormalizeRequired(name);
            NameNormalized = ContentText.NormalizeRequired(nameNormalized);
            Description = ContentText.NormalizeOptional(description);
            IsActive = isActive;
            MarkUpdated(nowUtc, actorUserId);
        }

        public void SoftDelete(DateTime nowUtc, long? actorUserId)
        {
            if (IsDeleted)
            {
                throw new ContentDomainException(
                    "CONTENT.TAG_ALREADY_DELETED",
                    "The tag has already been deleted.");
            }

            IsActive = false;
            MarkDeleted(nowUtc, actorUserId);
        }

        public void Restore(DateTime nowUtc, long? actorUserId)
        {
            if (!IsDeleted)
            {
                throw new ContentDomainException(
                    "CONTENT.TAG_NOT_DELETED",
                    "The tag is not deleted.");
            }

            IsActive = true;
            MarkRestored(nowUtc, actorUserId);
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
            long version)
        {
            ContentGuard.AgainstInvalidId(
                tagId,
                "CONTENT.TAG_INVALID_TAG_ID",
                "TagId must be greater than 0.");

            string normalizedPublicId = ValidatePublicId(publicId);
            ValidateName(name);
            ValidateNameNormalized(nameNormalized);
            ValidateDescription(description);

            ContentGuard.AgainstInvalidVersion(
                version,
                "CONTENT.TAG_INVALID_VERSION",
                "Version must be greater than 0.");
            ContentGuard.AgainstUpdatedBeforeCreated(
                updatedAt,
                createdAt,
                "CONTENT.TAG_INVALID_UPDATED_AT",
                "UpdatedAt cannot be earlier than CreatedAt.");
            ContentGuard.AgainstDeletedBeforeCreated(
                deletedAt,
                createdAt,
                "CONTENT.TAG_INVALID_DELETED_AT",
                "DeletedAt cannot be earlier than CreatedAt.");

            var tag = new Tag
            {
                TagId = tagId,
                PublicId = normalizedPublicId,
                Name = ContentText.NormalizeRequired(name),
                NameNormalized = ContentText.NormalizeRequired(nameNormalized),
                Description = ContentText.NormalizeOptional(description),
                IsActive = isActive
            };

            tag.RehydrateTracking(
                createdAt,
                updatedAt,
                createdByUserId,
                updatedByUserId,
                version);
            tag.RehydrateDeletion(isDeleted, deletedAt, deletedByUserId);

            return tag;
        }

        public void Activate(DateTime nowUtc, long? actorUserId)
        {
            EnsureTagNotDeleted();

            if (IsActive)
            {
                return;
            }

            IsActive = true;
            MarkUpdated(nowUtc, actorUserId);
        }

        public void Deactivate(DateTime nowUtc, long? actorUserId)
        {
            EnsureTagNotDeleted();

            if (!IsActive)
            {
                return;
            }

            IsActive = false;
            MarkUpdated(nowUtc, actorUserId);
        }

        private void EnsureTagNotDeleted()
        {
            EnsureNotDeleted(
                "CONTENT.TAG_ALREADY_DELETED",
                "The tag has already been deleted.");
        }

        private static string ValidatePublicId(string publicId)
        {
            return PublicIdRules.ValidateAndNormalize(
                publicId,
                "CONTENT.TAG_PUBLIC_ID_REQUIRED",
                "CONTENT.TAG_PUBLIC_ID_INVALID",
                "PublicId");
        }

        private static void ValidateName(string name)
        {
            ContentGuard.AgainstRequiredText(
                name,
                "CONTENT.TAG_NAME_REQUIRED",
                "Name is required.");
            ContentGuard.AgainstTooLong(
                name,
                ContentFieldLimits.TagNameMaxLength,
                "CONTENT.TAG_NAME_TOO_LONG",
                $"Name cannot exceed {ContentFieldLimits.TagNameMaxLength} characters.");
        }

        private static void ValidateNameNormalized(string nameNormalized)
        {
            ContentGuard.AgainstRequiredText(
                nameNormalized,
                "CONTENT.TAG_NAME_NORMALIZED_REQUIRED",
                "NameNormalized is required.");
            ContentGuard.AgainstTooLong(
                nameNormalized,
                ContentFieldLimits.TagNameNormalizedMaxLength,
                "CONTENT.TAG_NAME_NORMALIZED_TOO_LONG",
                $"NameNormalized cannot exceed {ContentFieldLimits.TagNameNormalizedMaxLength} characters.");
        }

        private static void ValidateDescription(string? description)
        {
            ContentGuard.AgainstTooLong(
                description,
                ContentFieldLimits.TagDescriptionMaxLength,
                "CONTENT.TAG_DESCRIPTION_TOO_LONG",
                $"Description cannot exceed {ContentFieldLimits.TagDescriptionMaxLength} characters.");
        }
    }
}
