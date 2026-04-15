using System.Text;
using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Seo.Application.Contracts.SlugRegistry.Requests;
using Seo.Application.Contracts.SlugRegistry.Responses;
using Seo.Application.Errors;
using Seo.Application.Ports.Persistence;
using Seo.Domain.Exceptions;

namespace Seo.Application.UseCases.SlugRoutes.GenerateSlug;

public sealed class GenerateSlugUseCase : IGenerateSlugUseCase
{
    private readonly ISlugRegistryRepository _slugRegistryRepository;

    public GenerateSlugUseCase(
        ISlugRegistryRepository slugRegistryRepository)
    {
        _slugRegistryRepository = slugRegistryRepository;
    }

    public async Task<Result<GenerateSlugResponse>> ExecuteAsync(
        GenerateSlugRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Scope))
            {
                return Result<GenerateSlugResponse>.Failure(
                    SeoErrors.SlugRegistry.InvalidScope);
            }

            if (string.IsNullOrWhiteSpace(request.Source))
            {
                return Result<GenerateSlugResponse>.Failure(
                    SeoErrors.SlugRegistry.SlugRequired);
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

            for (int i = 0; i < maxAttempts; i++)
            {
                string slugToCheck = i == 0 ? baseSlug : $"{baseSlug}-{i + 1}";

                var existing = await _slugRegistryRepository.GetByScopeAndSlugAsync(
                    request.Scope.Trim(),
                    slugToCheck,
                    onlyActive: null,
                    cancellationToken);

                if (existing is null)
                {
                    candidate = slugToCheck;
                    isUnique = true;
                    break;
                }
            }

            return Result<GenerateSlugResponse>.Success(
                new GenerateSlugResponse
                {
                    Scope = request.Scope.Trim(),
                    Source = request.Source.Trim(),
                    SuggestedSlug = candidate,
                    IsUnique = isUnique
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
        string input = value.Trim().ToLowerInvariant();
        StringBuilder builder = new();
        bool previousWasDash = false;

        foreach (char ch in input)
        {
            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(ch);
                previousWasDash = false;
                continue;
            }

            if (char.IsWhiteSpace(ch) || ch is '-' or '_' or '.' or '/')
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

    private static Error MapDomainException(SeoDomainException exception)
    {
        return exception.Code switch
        {
            "SEO.INVALID_SCOPE" => SeoErrors.SlugRegistry.InvalidScope,
            "SEO.INVALID_SLUG" => SeoErrors.SlugRegistry.SlugRequired,
            "SEO.SLUG_TOO_LONG" => SeoErrors.SlugRegistry.SlugTooLong,
            _ => SeoErrors.ValidationFailed
        };
    }

    private static Error MapPersistenceException(PersistenceException exception)
    {
        return exception.Code switch
        {
            _ => SeoErrors.ValidationFailed
        };
    }
}