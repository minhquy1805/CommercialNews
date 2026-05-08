using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.RequestContext;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Identity.Application.Contracts.LoginHistory.GetMyLoginHistory;
using Identity.Application.Errors;
using Identity.Application.Models.QueryModels;
using Identity.Application.Ports.Persistence;
using Identity.Application.Validation.LoginHistory.GetMyLoginHistory;

namespace Identity.Application.UseCases.LoginHistory.GetMyLoginHistory;

public sealed class GetMyLoginHistoryUseCase : IGetMyLoginHistoryUseCase
{
    private readonly IRequestContext _requestContext;
    private readonly ILoginHistoryRepository _loginHistoryRepository;

    public GetMyLoginHistoryUseCase(
        IRequestContext requestContext,
        ILoginHistoryRepository loginHistoryRepository)
    {
        _requestContext = requestContext ?? throw new ArgumentNullException(nameof(requestContext));
        _loginHistoryRepository = loginHistoryRepository ?? throw new ArgumentNullException(nameof(loginHistoryRepository));
    }

    public async Task<Result<GetMyLoginHistoryResponseDto>> ExecuteAsync(
        GetMyLoginHistoryRequestDto request,
        CancellationToken cancellationToken = default)
    {
        Error? validationError = GetMyLoginHistoryValidator.Validate(request);
        if (validationError is not null)
        {
            return Result<GetMyLoginHistoryResponseDto>.Failure(validationError);
        }

        long? currentUserId = _requestContext.CurrentUserId;
        if (currentUserId is null)
        {
            return Result<GetMyLoginHistoryResponseDto>.Failure(
                IdentityErrors.Auth.Unauthenticated);
        }

        int page = GetMyLoginHistoryValidator.NormalizePage(request.Page);
        int pageSize = GetMyLoginHistoryValidator.NormalizePageSize(request.PageSize);

        var query = new LoginHistoryByUserQuery
        {
            UserId = currentUserId.Value,
            Succeeded = request.Succeeded,
            FromAttemptedAt = request.FromAttemptedAt,
            ToAttemptedAt = request.ToAttemptedAt,
            Page = page,
            PageSize = pageSize
        };

        try
        {
            var result = await _loginHistoryRepository.SelectByUserIdAsync(
                query,
                cancellationToken);

            return Result<GetMyLoginHistoryResponseDto>.Success(
                new GetMyLoginHistoryResponseDto
                {
                    Items = result.Items
                        .Select(MapItem)
                        .ToArray(),
                    Page = result.Page,
                    PageSize = result.PageSize,
                    TotalItems = result.TotalItems
                });
        }
        catch (PersistenceException)
        {
            return Result<GetMyLoginHistoryResponseDto>.Failure(
                IdentityErrors.LoginHistory.QueryFailed);
        }
    }

    private static LoginHistoryItemDto MapItem(
        LoginHistoryListResultItem item)
    {
        return new LoginHistoryItemDto
        {
            LoginId = item.LoginId,
            Succeeded = item.Succeeded,
            FailureReason = item.FailureReason,
            AttemptedAt = item.AttemptedAt,
            IpAddress = item.IpAddress,
            UserAgent = item.UserAgent,
            CorrelationId = item.CorrelationId
        };
    }
}