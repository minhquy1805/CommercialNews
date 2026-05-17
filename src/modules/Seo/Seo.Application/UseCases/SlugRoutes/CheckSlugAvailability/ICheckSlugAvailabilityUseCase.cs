using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Seo.Application.Contracts.SlugRegistry.Requests;
using Seo.Application.Contracts.SlugRegistry.Responses;

namespace Seo.Application.UseCases.SlugRoutes.CheckSlugAvailability;

public interface ICheckSlugAvailabilityUseCase
{
    Task<Result<CheckSlugAvailabilityResponse>> ExecuteAsync(
        CheckSlugAvailabilityRequest request,
        CancellationToken cancellationToken = default);
}