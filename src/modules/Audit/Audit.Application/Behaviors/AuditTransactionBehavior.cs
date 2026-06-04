using Audit.Application.Abstractions.Persistence;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using MediatR;

namespace Audit.Application.Behaviors;

public sealed class AuditTransactionBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IAuditUnitOfWork _unitOfWork;

    public AuditTransactionBehavior(
        IAuditUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(next);

        if (!ShouldUseTransaction(request))
        {
            return await next();
        }

        if (_unitOfWork.HasActiveTransaction)
        {
            return await next();
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var response = await next();

            if (IsFailureResult(response))
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                return response;
            }

            await _unitOfWork.CommitAsync(cancellationToken);

            return response;
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static bool ShouldUseTransaction(
        TRequest request)
    {
        return request is IAuditTransactionalRequest;
    }

    private static bool IsFailureResult(
        TResponse response)
    {
        if (response is Result result)
        {
            return result.IsFailure;
        }

        return false;
    }
}