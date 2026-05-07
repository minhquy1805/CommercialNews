using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.RequestContext;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Identity.Application.Contracts.LoginHistory.GetUserLoginHistory;
using Identity.Application.Errors;
using Identity.Application.Models.QueryModels;
using Identity.Application.Ports.Persistence;
using Identity.Application.Validation.LoginHistory.GetUserLoginHistory;

namespace Identity.Application.UseCases.LoginHistory.GetUserLoginHistory;

public sealed class GetUserLoginHistoryUseCase : IGetUserLoginHistoryUseCase
{
    private readonly IRequestContext _requestContext;
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly ILoginHistoryRepository _loginHistoryRepository;

    public GetUserLoginHistoryUseCase(
        IRequestContext requestContext,
        IUserAccountRepository userAccountRepository,
        ILoginHistoryRepository loginHistoryRepository)
    {
        _requestContext = requestContext ?? throw new ArgumentNullException(nameof(requestContext));
        _userAccountRepository = userAccountRepository ?? throw new ArgumentNullException(nameof(userAccountRepository));
        _loginHistoryRepository = loginHistoryRepository ?? throw new ArgumentNullException(nameof(loginHistoryRepository));
    }

    public async Task<Result<GetUserLoginHistoryResponseDto>> ExecuteAsync(
        GetUserLoginHistoryRequestDto request,
        CancellationToken cancellationToken = default)
    {
        Error? validationError = GetUserLoginHistoryValidator.Validate(request);
        if (validationError is not null)
        {
            return Result<GetUserLoginHistoryResponseDto>.Failure(validationError);
        }

        long? currentUserId = _requestContext.CurrentUserId;
        if (currentUserId is null)
        {
            return Result<GetUserLoginHistoryResponseDto>.Failure(
                IdentityErrors.Auth.Unauthenticated);
        }

        int page = GetUserLoginHistoryValidator.NormalizePage(request.Page);
        int pageSize = GetUserLoginHistoryValidator.NormalizePageSize(request.PageSize);

        try
        {
            var user = await _userAccountRepository.GetByIdAsync(
                request.UserId,
                cancellationToken);

            if (user is null)
            {
                return Result<GetUserLoginHistoryResponseDto>.Failure(
                    IdentityErrors.User.NotFound);
            }

            var query = new LoginHistoryByUserQuery
            {
                UserId = request.UserId,
                Succeeded = request.Succeeded,
                FromAttemptedAt = request.FromAttemptedAt,
                ToAttemptedAt = request.ToAttemptedAt,
                Page = page,
                PageSize = pageSize
            };

            var result = await _loginHistoryRepository.SelectByUserIdAsync(
                query,
                cancellationToken);

            return Result<GetUserLoginHistoryResponseDto>.Success(
                new GetUserLoginHistoryResponseDto
                {
                    UserId = request.UserId,
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
            return Result<GetUserLoginHistoryResponseDto>.Failure(
                IdentityErrors.LoginHistory.QueryFailed);
        }
    }

    private static UserLoginHistoryItemDto MapItem(
        LoginHistoryListResultItem item)
    {
        return new UserLoginHistoryItemDto
        {
            LoginId = item.LoginId,
            UserId = item.UserId,
            Succeeded = item.Succeeded,
            FailureReason = item.FailureReason,
            AttemptedAt = item.AttemptedAt,
            IpAddress = item.IpAddress,
            UserAgent = item.UserAgent,
            CorrelationId = item.CorrelationId
        };
    }
}