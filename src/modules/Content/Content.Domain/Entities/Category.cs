using Content.Domain.Exceptions;

namespace Content.Domain.Entities
{
    public sealed class Category
    {
        public long CategoryId { get; private set; }
        public string PublicId { get; private set; } = string.Empty;

        public long? ParentCategoryId { get; private set; }

        public string Name { get; private set; } = string.Empty;
        public string NameNormalized { get; private set; } = string.Empty;
        public string? Description { get; private set; }

        public bool IsActive { get; private set; }
        public int DisplayOrder { get; private set; }

        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        public long? CreatedByUserId { get; private set; }
        public long? UpdatedByUserId { get; private set; }

        public bool IsDeleted { get; private set; }
        public DateTime? DeletedAt { get; private set; }
        public long? DeletedByUserId { get; private set; }

        public long Version { get; private set; }

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
            ValidatePublicId(publicId);
            ValidateParentCategoryId(parentCategoryId);
            ValidateName(name);
            ValidateNameNormalized(nameNormalized);
            ValidateDisplayOrder(displayOrder);

            return new Category
            {
                PublicId = publicId.Trim(),
                ParentCategoryId = parentCategoryId,
                Name = name.Trim(),
                NameNormalized = nameNormalized.Trim(),
                Description = NormalizeOptional(description),
                IsActive = isActive,
                DisplayOrder = displayOrder,
                CreatedAt = nowUtc,
                UpdatedAt = nowUtc,
                CreatedByUserId = actorUserId,
                UpdatedByUserId = actorUserId,
                IsDeleted = false,
                Version = 1
            };
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
            if (categoryId <= 0)
            {
                throw new ContentDomainException(
                    "CONTENT.CATEGORY_INVALID_CATEGORY_ID",
                    "Category id must be greater than zero.");
            }

            if (version <= 0)
            {
                throw new ContentDomainException(
                    "CONTENT.CATEGORY_INVALID_VERSION",
                    "Category version must be greater than zero.");
            }

            ValidatePublicId(publicId);
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

            return new Category
            {
                CategoryId = categoryId,
                PublicId = publicId.Trim(),
                ParentCategoryId = parentCategoryId,
                Name = name.Trim(),
                NameNormalized = nameNormalized.Trim(),
                Description = NormalizeOptional(description),
                IsActive = isActive,
                DisplayOrder = displayOrder,
                CreatedAt = createdAt,
                UpdatedAt = updatedAt,
                CreatedByUserId = createdByUserId,
                UpdatedByUserId = updatedByUserId,
                IsDeleted = isDeleted,
                DeletedAt = deletedAt,
                DeletedByUserId = deletedByUserId,
                Version = version
            };
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
            EnsureNotDeleted();

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
            Name = name.Trim();
            NameNormalized = nameNormalized.Trim();
            Description = NormalizeOptional(description);
            IsActive = isActive;
            DisplayOrder = displayOrder;
            UpdatedAt = nowUtc;
            UpdatedByUserId = actorUserId;
            Version++;
        }

        public void Activate(DateTime nowUtc, long? actorUserId)
        {
            EnsureNotDeleted();

            if (IsActive)
            {
                return;
            }

            IsActive = true;
            UpdatedAt = nowUtc;
            UpdatedByUserId = actorUserId;
            Version++;
        }

        public void Deactivate(DateTime nowUtc, long? actorUserId)
        {
            EnsureNotDeleted();

            if (!IsActive)
            {
                return;
            }

            IsActive = false;
            UpdatedAt = nowUtc;
            UpdatedByUserId = actorUserId;
            Version++;
        }

        public void SoftDelete(DateTime nowUtc, long? actorUserId)
        {
            if (IsDeleted)
            {
                throw new ContentDomainException(
                    "CONTENT.CATEGORY_ALREADY_DELETED",
                    "Category is already deleted.");
            }

            IsDeleted = true;
            IsActive = false;
            DeletedAt = nowUtc;
            DeletedByUserId = actorUserId;
            UpdatedAt = nowUtc;
            UpdatedByUserId = actorUserId;
            Version++;
        }

        public void Restore(DateTime nowUtc, long? actorUserId)
        {
            if (!IsDeleted)
            {
                throw new ContentDomainException(
                    "CONTENT.CATEGORY_NOT_DELETED",
                    "Category is not deleted.");
            }

            IsDeleted = false;
            IsActive = true;
            DeletedAt = null;
            DeletedByUserId = null;
            UpdatedAt = nowUtc;
            UpdatedByUserId = actorUserId;
            Version++;
        }

        private void EnsureNotDeleted()
        {
            if (IsDeleted)
            {
                throw new ContentDomainException(
                    "CONTENT.CATEGORY_ALREADY_DELETED",
                    "Category is already deleted.");
            }
        }

        private static void ValidatePublicId(string publicId)
        {
            if (string.IsNullOrWhiteSpace(publicId))
            {
                throw new ContentDomainException(
                    "CONTENT.CATEGORY_PUBLIC_ID_REQUIRED",
                    "Category public id is required.");
            }

            if (publicId.Trim().Length != 26)
            {
                throw new ContentDomainException(
                    "CONTENT.CATEGORY_PUBLIC_ID_INVALID",
                    "Category public id must be exactly 26 characters.");
            }
        }

        private static void ValidateParentCategoryId(long? parentCategoryId)
        {
            if (parentCategoryId.HasValue && parentCategoryId.Value <= 0)
            {
                throw new ContentDomainException(
                    "CONTENT.CATEGORY_PARENT_ID_INVALID",
                    "Parent category id must be greater than zero.");
            }
        }

        private static void ValidateName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ContentDomainException(
                    "CONTENT.CATEGORY_NAME_REQUIRED",
                    "Category name is required.");
            }

            if (name.Trim().Length > 200)
            {
                throw new ContentDomainException(
                    "CONTENT.CATEGORY_NAME_TOO_LONG",
                    "Category name must not exceed 200 characters.");
            }
        }

        private static void ValidateNameNormalized(string nameNormalized)
        {
            if (string.IsNullOrWhiteSpace(nameNormalized))
            {
                throw new ContentDomainException(
                    "CONTENT.CATEGORY_NAME_NORMALIZED_REQUIRED",
                    "Category normalized name is required.");
            }

            if (nameNormalized.Trim().Length > 200)
            {
                throw new ContentDomainException(
                    "CONTENT.CATEGORY_NAME_NORMALIZED_TOO_LONG",
                    "Category normalized name must not exceed 200 characters.");
            }
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

        private static string? NormalizeOptional(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return value.Trim();
        }
    }
}
