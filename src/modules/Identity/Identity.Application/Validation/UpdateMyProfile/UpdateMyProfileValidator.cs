using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Identity.Application.Contracts.UpdateMyProfile;
using Identity.Application.Errors;

namespace Identity.Application.Validation.UpdateMyProfile;

public static class UpdateMyProfileValidator
{
    private const int FullNameMaxLength = 200;

    public static Error? Validate(UpdateMyProfileRequestDto? request)
    {
        if (request is null)
        {
            return IdentityErrors.Profile.InvalidRequest;
        }

        if (!string.IsNullOrWhiteSpace(request.FullName) &&
            request.FullName.Trim().Length > FullNameMaxLength)
        {
            return IdentityErrors.Profile.FullNameTooLong;
        }

        return null;
    }

    public static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
