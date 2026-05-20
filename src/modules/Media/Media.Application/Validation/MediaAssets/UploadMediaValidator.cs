using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Media.Application.Contracts.MediaAsset.Requests;
using Media.Application.Errors;
using Media.Domain.Constants;

namespace Media.Application.Validation.MediaAssets;

public static class UploadMediaValidator
{
    private static readonly HashSet<string> AllowedImageExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".gif",
            ".webp"
        };

    private static readonly HashSet<string> AllowedVideoExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".mp4",
            ".webm",
            ".mov",
            ".mkv"
        };

    public static Error? Validate(UploadMediaRequest? request)
    {
        if (request is null)
        {
            return MediaErrors.ValidationFailed;
        }

        if (request.Content == Stream.Null || !request.Content.CanRead)
        {
            return MediaErrors.ValidationFailed;
        }

        if (request.Length <= 0)
        {
            return MediaErrors.ValidationFailed;
        }

        if (string.IsNullOrWhiteSpace(request.OriginalFileName))
        {
            return MediaErrors.ValidationFailed;
        }

        if (string.IsNullOrWhiteSpace(request.MediaType))
        {
            return MediaErrors.MediaAsset.TypeNotAllowed;
        }

        if (!MediaTypes.IsValid(request.MediaType))
        {
            return MediaErrors.MediaAsset.TypeNotAllowed;
        }

        if (!IsContentTypeAllowedForMediaType(
                request.ContentType,
                request.MediaType))
        {
            return MediaErrors.MediaAsset.TypeNotAllowed;
        }

        if (!IsExtensionAllowedForMediaType(
                request.OriginalFileName,
                request.MediaType))
        {
            return MediaErrors.MediaAsset.TypeNotAllowed;
        }

        return null;
    }

    private static bool IsContentTypeAllowedForMediaType(
        string? contentType,
        string mediaType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return false;
        }

        if (string.Equals(
                mediaType,
                MediaTypes.Image,
                StringComparison.OrdinalIgnoreCase))
        {
            return contentType.StartsWith(
                "image/",
                StringComparison.OrdinalIgnoreCase);
        }

        if (string.Equals(
                mediaType,
                MediaTypes.Video,
                StringComparison.OrdinalIgnoreCase))
        {
            return contentType.StartsWith(
                "video/",
                StringComparison.OrdinalIgnoreCase);
        }

        if (string.Equals(
                mediaType,
                MediaTypes.File,
                StringComparison.OrdinalIgnoreCase))
        {
            return !contentType.StartsWith(
                       "image/",
                       StringComparison.OrdinalIgnoreCase)
                   && !contentType.StartsWith(
                       "video/",
                       StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    private static bool IsExtensionAllowedForMediaType(
        string originalFileName,
        string mediaType)
    {
        string extension = Path.GetExtension(originalFileName);

        if (string.IsNullOrWhiteSpace(extension))
        {
            return false;
        }

        if (string.Equals(
                mediaType,
                MediaTypes.Image,
                StringComparison.OrdinalIgnoreCase))
        {
            return AllowedImageExtensions.Contains(extension);
        }

        if (string.Equals(
                mediaType,
                MediaTypes.Video,
                StringComparison.OrdinalIgnoreCase))
        {
            return AllowedVideoExtensions.Contains(extension);
        }

        if (string.Equals(
                mediaType,
                MediaTypes.File,
                StringComparison.OrdinalIgnoreCase))
        {
            return !AllowedImageExtensions.Contains(extension)
                   && !AllowedVideoExtensions.Contains(extension);
        }

        return false;
    }
}