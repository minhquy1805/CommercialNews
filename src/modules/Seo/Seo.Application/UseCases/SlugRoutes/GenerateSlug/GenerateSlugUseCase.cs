using System.Globalization;
using System.Text;
using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Seo.Application.Contracts.SlugRegistry.Requests;
using Seo.Application.Contracts.SlugRegistry.Responses;
using Seo.Application.Errors;
using Seo.Application.Ports.Persistence;
using Seo.Domain.Constants;
using Seo.Domain.Entities;
using Seo.Domain.Exceptions;

namespace Seo.Application.UseCases.SlugRoutes.GenerateSlug;

public sealed class GenerateSlugUseCase : IGenerateSlugUseCase
{
    private readonly ISlugRegistryRepository _slugRegistryRepository;

    public GenerateSlugUseCase(
        ISlugRegistryRepository slugRegistryRepository)
    {
        _slugRegistryRepository = slugRegistryRepository
            ?? throw new ArgumentNullException(nameof(slugRegistryRepository));
    }

    public async Task<Result<GenerateSlugResponse>> ExecuteAsync(
        GenerateSlugRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            string scope = string.IsNullOrWhiteSpace(request.Scope)
                ? SeoScopes.Public
                : request.Scope.Trim();

            if (!SeoScopes.IsValid(scope))
            {
                return Result<GenerateSlugResponse>.Failure(
                    SeoErrors.SlugRegistry.InvalidScope);
            }

            if (string.IsNullOrWhiteSpace(request.Source))
            {
                return Result<GenerateSlugResponse>.Failure(
                    SeoErrors.SlugRegistry.SlugRequired);
            }

            string? requestedResourceType = string.IsNullOrWhiteSpace(request.ResourceType)
                ? null
                : request.ResourceType.Trim();

            string? requestedResourcePublicId = string.IsNullOrWhiteSpace(request.ResourcePublicId)
                ? null
                : request.ResourcePublicId.Trim();

            if (requestedResourceType is not null &&
                !SeoResourceTypes.IsValid(requestedResourceType))
            {
                return Result<GenerateSlugResponse>.Failure(
                    SeoErrors.Resource.InvalidResourceType);
            }

            if (requestedResourcePublicId is not null &&
                requestedResourcePublicId.Length != 26)
            {
                return Result<GenerateSlugResponse>.Failure(
                    SeoErrors.Resource.InvalidResourcePublicId);
            }

            string baseSlug = Slugify(request.Source);

            if (string.IsNullOrWhiteSpace(baseSlug))
            {
                return Result<GenerateSlugResponse>.Failure(
                    SeoErrors.SlugRegistry.SlugRequired);
            }

            const int maxAttempts = 100;

            string candidate = baseSlug;
            bool isUnique = false;
            SlugRegistry? conflictingRoute = null;

            for (int i = 0; i < maxAttempts; i++)
            {
                string slugToCheck = i == 0
                    ? baseSlug
                    : $"{baseSlug}-{i + 1}";

                SlugRegistry? existing = await _slugRegistryRepository.GetByScopeAndSlugAsync(
                    scope,
                    slugToCheck,
                    onlyActive: true,
                    cancellationToken);

                if (existing is null)
                {
                    candidate = slugToCheck;
                    isUnique = true;
                    conflictingRoute = null;
                    break;
                }

                bool belongsToSameResource =
                    requestedResourceType is not null &&
                    requestedResourcePublicId is not null &&
                    string.Equals(existing.ResourceType, requestedResourceType, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(existing.ResourcePublicId, requestedResourcePublicId, StringComparison.OrdinalIgnoreCase);

                if (belongsToSameResource)
                {
                    candidate = slugToCheck;
                    isUnique = true;
                    conflictingRoute = existing;
                    break;
                }

                conflictingRoute = existing;
            }

            return Result<GenerateSlugResponse>.Success(
                new GenerateSlugResponse
                {
                    Scope = scope,
                    Source = request.Source.Trim(),
                    SuggestedSlug = candidate,
                    IsUnique = isUnique,
                    ExistingResourceType = conflictingRoute?.ResourceType,
                    ExistingResourcePublicId = conflictingRoute?.ResourcePublicId
                });
        }
        catch (PersistenceException exception)
        {
            return Result<GenerateSlugResponse>.Failure(
                MapPersistenceException(exception));
        }
        catch (SeoDomainException exception)
        {
            return Result<GenerateSlugResponse>.Failure(
                MapDomainException(exception));
        }
    }

    private static string Slugify(string value)
    {
        string normalized = RemoveDiacritics(value.Trim().ToLowerInvariant());

        StringBuilder builder = new();
        bool previousWasDash = false;

        foreach (char ch in normalized)
        {
            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(ch);
                previousWasDash = false;
                continue;
            }

            if (char.IsWhiteSpace(ch) || ch is '-' or '_' or '.' or '/' or '\\')
            {
                if (!previousWasDash && builder.Length > 0)
                {
                    builder.Append('-');
                    previousWasDash = true;
                }
            }
        }

        string result = builder.ToString().Trim('-');

        if (result.Length > 200)
        {
            result = result[..200].Trim('-');
        }

        return result;
    }

    private static string RemoveDiacritics(string value)
    {
        string normalized = value.Normalize(NormalizationForm.FormD);
        StringBuilder builder = new();

        foreach (char ch in normalized)
        {
            UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(ch);

            if (category != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(ch);
            }
        }

        return builder
            .ToString()
            .Normalize(NormalizationForm.FormC)
            .Replace('đ', 'd')
            .Replace('Đ', 'd');
    }

    private static Error MapDomainException(SeoDomainException exception)
    {
        return exception.Code switch
        {
            "SEO.INVALID_SCOPE" => SeoErrors.SlugRegistry.InvalidScope,
            "SEO.INVALID_SLUG" => SeoErrors.SlugRegistry.SlugRequired,
            "SEO.SLUG_TOO_LONG" => SeoErrors.SlugRegistry.SlugTooLong,
            "SEO.INVALID_RESOURCE_TYPE" => SeoErrors.Resource.InvalidResourceType,
            "SEO.INVALID_RESOURCE_PUBLIC_ID" => SeoErrors.Resource.InvalidResourcePublicId,
            _ => SeoErrors.ValidationFailed
        };
    }

    private static Error MapPersistenceException(PersistenceException exception)
    {
        return exception.Code switch
        {
            "SEO.INVALID_SCOPE" => SeoErrors.SlugRegistry.InvalidScope,
            "SEO.INVALID_RESOURCE_TYPE" => SeoErrors.Resource.InvalidResourceType,
            "SEO.INVALID_RESOURCE_PUBLIC_ID" => SeoErrors.Resource.InvalidResourcePublicId,
            "SEO.STORE_UNAVAILABLE" => SeoErrors.Infrastructure.StoreUnavailable,
            _ => SeoErrors.ValidationFailed
        };
    }
}