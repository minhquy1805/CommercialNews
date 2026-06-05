using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Identity.Application.Contracts.UpdateMyAvatar;
using Identity.Application.Errors;

namespace Identity.Application.Validation.UpdateMyAvatar;

public static class UpdateMyAvatarValidator
{
    private const long MaxAvatarFileSizeBytes = 5 * 1024 * 1024;

    private static readonly HashSet<string> AllowedImageExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp"
        };

    public static Error? Validate(
        UpdateMyAvatarRequestDto? request)
    {
        if (request is null)
        {
            return IdentityErrors.Profile.InvalidRequest;
        }

        if (request.Content == Stream.Null || !request.Content.CanRead)
        {
            return IdentityErrors.Profile.AvatarFileRequired;
        }

        if (request.Length <= 0)
        {
            return IdentityErrors.Profile.AvatarFileRequired;
        }

        if (request.Length > MaxAvatarFileSizeBytes)
        {
            return IdentityErrors.Profile.AvatarFileTooLarge;
        }

        if (string.IsNullOrWhiteSpace(request.OriginalFileName))
        {
            return IdentityErrors.Profile.AvatarFileRequired;
        }

        if (!IsAllowedExtension(request.OriginalFileName))
        {
            return IdentityErrors.Profile.AvatarFileTypeNotAllowed;
        }

        if (!IsAllowedContentType(request.ContentType))
        {
            return IdentityErrors.Profile.AvatarFileTypeNotAllowed;
        }

        return null;
    }

    private static bool IsAllowedExtension(
        string originalFileName)
    {
        string extension = Path.GetExtension(originalFileName);

        return !string.IsNullOrWhiteSpace(extension)
            && AllowedImageExtensions.Contains(extension);
    }

    private static bool IsAllowedContentType(
        string? contentType)
    {
        return !string.IsNullOrWhiteSpace(contentType)
            && contentType.Trim().StartsWith(
                "image/",
                StringComparison.OrdinalIgnoreCase);
    }
}
