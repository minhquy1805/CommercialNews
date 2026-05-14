using Content.Domain.Common;
using Content.Domain.Constants;
using Content.Domain.Exceptions;

namespace Content.Domain.Entities
{
    public sealed class Category : ContentSoftDeletableEntity
    {
        public long CategoryId { get; private set; }
        public string PublicId { get; private set; } = string.Empty;

        public long? ParentCategoryId { get; private set; }

        public string Name { get; private set; } = string.Empty;
        public string NameNormalized { get; private set; } = string.Empty;
        public string? Description { get; private set; }

        public bool IsActive { get; private set; }
        public int DisplayOrder { get; private set; }

        private Category()
        {
        }

        public bool CanBeUsedByArticle => IsActive && !IsDeleted;

        public static Category Create(
            string publicId,
            long? parentCategoryId,
            string name,
            string nameNormalized,
            string? description,
            bool isActive,
            int displayOrder,
            DateTime nowUtc,
            long? actorUserId)
        {
            string normalizedPublicId = ValidatePublicId(publicId);
            ValidateParentCategoryId(parentCategoryId);
            ValidateName(name);
            ValidateNameNormalized(nameNormalized);
            ValidateDisplayOrder(displayOrder);

            var category = new Category
            {
                PublicId = normalizedPublicId,
                ParentCategoryId = parentCategoryId,
                Name = ContentText.NormalizeRequired(name),
                NameNormalized = ContentText.NormalizeRequired(nameNormalized),
                Description = ContentText.NormalizeOptional(description),
                IsActive = isActive,
                DisplayOrder = displayOrder
            };

            category.InitializeTracking(nowUtc, actorUserId);
            category.InitializeDeletion();

            return category;
        }

        public static Category Rehydrate(
            long categoryId,
            string publicId,
            long? parentCategoryId,
            string name,
            string nameNormalized,
            string? description,
            bool isActive,
            int displayOrder,
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
                categoryId,
                "CONTENT.CATEGORY_INVALID_CATEGORY_ID",
                "Category id must be greater than zero.");
            ContentGuard.AgainstInvalidVersion(
                version,
                "CONTENT.CATEGORY_INVALID_VERSION",
                "Category version must be greater than zero.");

            ContentGuard.AgainstUpdatedBeforeCreated(
                updatedAt,
                createdAt,
                "CONTENT.CATEGORY_INVALID_UPDATED_AT",
                "UpdatedAt cannot be earlier than CreatedAt.");

            ContentGuard.AgainstDeletedBeforeCreated(
                deletedAt,
                createdAt,
                "CONTENT.CATEGORY_INVALID_DELETED_AT",
                "DeletedAt cannot be earlier than CreatedAt.");

            string normalizedPublicId = ValidatePublicId(publicId);
            ValidateParentCategoryId(parentCategoryId);
            ValidateName(name);
            ValidateNameNormalized(nameNormalized);
            ValidateDisplayOrder(displayOrder);

            if (parentCategoryId.HasValue && parentCategoryId.Value == categoryId)
            {
                throw new ContentDomainException(
                    "CONTENT.CATEGORY_PARENT_SELF_REFERENCE",
                    "Category cannot be its own parent.");
            }

            var category = new Category
            {
                CategoryId = categoryId,
                PublicId = normalizedPublicId,
                ParentCategoryId = parentCategoryId,
                Name = ContentText.NormalizeRequired(name),
                NameNormalized = ContentText.NormalizeRequired(nameNormalized),
                Description = ContentText.NormalizeOptional(description),
                IsActive = isActive,
                DisplayOrder = displayOrder
            };

            category.RehydrateTracking(
                createdAt,
                updatedAt,
                createdByUserId,
                updatedByUserId,
                version);
            category.RehydrateDeletion(isDeleted, deletedAt, deletedByUserId);

            return category;
        }

        public void Update(
            long? parentCategoryId,
            string name,
            string nameNormalized,
            string? description,
            bool isActive,
            int displayOrder,
            DateTime nowUtc,
            long? actorUserId)
        {
            EnsureCategoryNotDeleted();

            ValidateName(name);
            ValidateNameNormalized(nameNormalized);
            ValidateParentCategoryId(parentCategoryId);
            ValidateDisplayOrder(displayOrder);

            if (CategoryId > 0 && parentCategoryId.HasValue && parentCategoryId.Value == CategoryId)
            {
                throw new ContentDomainException(
                    "CONTENT.CATEGORY_PARENT_SELF_REFERENCE",
                    "Category cannot be its own parent.");
            }

            ParentCategoryId = parentCategoryId;
            Name = ContentText.NormalizeRequired(name);
            NameNormalized = ContentText.NormalizeRequired(nameNormalized);
            Description = ContentText.NormalizeOptional(description);
            IsActive = isActive;
            DisplayOrder = displayOrder;
            MarkUpdated(nowUtc, actorUserId);
        }

        public void Activate(DateTime nowUtc, long? actorUserId)
        {
            EnsureCategoryNotDeleted();

            if (IsActive)
            {
                return;
            }

            IsActive = true;
            MarkUpdated(nowUtc, actorUserId);
        }

        public void Deactivate(DateTime nowUtc, long? actorUserId)
        {
            EnsureCategoryNotDeleted();

            if (!IsActive)
            {
                return;
            }

            IsActive = false;
            MarkUpdated(nowUtc, actorUserId);
        }

        public void SoftDelete(DateTime nowUtc, long? actorUserId)
        {
            if (IsDeleted)
            {
                throw new ContentDomainException(
                    "CONTENT.CATEGORY_ALREADY_DELETED",
                    "Category is already deleted.");
            }

            IsActive = false;
            MarkDeleted(nowUtc, actorUserId);
        }

        public void Restore(DateTime nowUtc, long? actorUserId)
        {
            if (!IsDeleted)
            {
                throw new ContentDomainException(
                    "CONTENT.CATEGORY_NOT_DELETED",
                    "Category is not deleted.");
            }

            IsActive = true;
            MarkRestored(nowUtc, actorUserId);
        }

        private void EnsureCategoryNotDeleted()
        {
            EnsureNotDeleted(
                "CONTENT.CATEGORY_ALREADY_DELETED",
                "Category is already deleted.");
        }

        private static string ValidatePublicId(string publicId)
        {
            return PublicIdRules.ValidateAndNormalize(
                publicId,
                "CONTENT.CATEGORY_PUBLIC_ID_REQUIRED",
                "CONTENT.CATEGORY_PUBLIC_ID_INVALID",
                "Category public id");
        }

        private static void ValidateParentCategoryId(long? parentCategoryId)
        {
            ContentGuard.AgainstInvalidOptionalId(
                parentCategoryId,
                "CONTENT.CATEGORY_PARENT_ID_INVALID",
                "Parent category id must be greater than zero.");
        }

        private static void ValidateName(string name)
        {
            ContentGuard.AgainstRequiredText(
                name,
                "CONTENT.CATEGORY_NAME_REQUIRED",
                "Category name is required.");
            ContentGuard.AgainstTooLong(
                name,
                ContentFieldLimits.CategoryNameMaxLength,
                "CONTENT.CATEGORY_NAME_TOO_LONG",
                $"Category name must not exceed {ContentFieldLimits.CategoryNameMaxLength} characters.");
        }

        private static void ValidateNameNormalized(string nameNormalized)
        {
            ContentGuard.AgainstRequiredText(
                nameNormalized,
                "CONTENT.CATEGORY_NAME_NORMALIZED_REQUIRED",
                "Category normalized name is required.");
            ContentGuard.AgainstTooLong(
                nameNormalized,
                ContentFieldLimits.CategoryNameNormalizedMaxLength,
                "CONTENT.CATEGORY_NAME_NORMALIZED_TOO_LONG",
                $"Category normalized name must not exceed {ContentFieldLimits.CategoryNameNormalizedMaxLength} characters.");
        }

        private static void ValidateDisplayOrder(int displayOrder)
        {
            if (displayOrder < 0)
            {
                throw new ContentDomainException(
                    "CONTENT.CATEGORY_DISPLAY_ORDER_INVALID",
                    "Category display order must be greater than or equal to zero.");
            }
        }

    }
}
