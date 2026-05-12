using Content.Domain.Constants;
using Content.Domain.Exceptions;

namespace Content.Domain.Common;

public static class PublicIdRules
{
    public const int UlidLength = ContentFieldLimits.PublicIdLength;

    public static string ValidateAndNormalize(
        string publicId,
        string requiredCode,
        string invalidCode,
        string fieldName)
    {
        if (string.IsNullOrWhiteSpace(publicId))
        {
            throw new ContentDomainException(
                requiredCode,
                $"{fieldName} is required.");
        }

        string trimmed = publicId.Trim();

        if (trimmed.Length != UlidLength)
        {
            throw new ContentDomainException(
                invalidCode,
                $"{fieldName} must be exactly {UlidLength} characters.");
        }

        return trimmed;
    }
}
