using CommercialNews.BuildingBlocks.SharedKernel.Results;
using FluentValidation;
using MediatR;

namespace Audit.Application.Behaviors;

public sealed class AuditValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public AuditValidationBehavior(
        IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators
            ?? throw new ArgumentNullException(nameof(validators));
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(next);

        var validators = _validators.ToArray();

        if (validators.Length == 0)
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            validators.Select(validator =>
                validator.ValidateAsync(context, cancellationToken)));

        var errors = validationResults
            .SelectMany(result => result.Errors)
            .Where(failure => failure is not null)
            .Select(failure => failure.ErrorMessage)
            .Where(message => !string.IsNullOrWhiteSpace(message))
            .Select(message => message.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (errors.Length == 0)
        {
            return await next();
        }

        var error = Error.Validation(
            code: "AUDIT.VALIDATION_FAILED",
            message: "One or more audit validations failed.",
            details: errors);

        return CreateFailureResponse(error);
    }

    private static TResponse CreateFailureResponse(
        Error error)
    {
        var responseType = typeof(TResponse);

        if (responseType == typeof(Result))
        {
            return (TResponse)(object)Result.Failure(error);
        }

        if (responseType.IsGenericType &&
            responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var valueType = responseType.GetGenericArguments()[0];

            var failureMethod = typeof(Result<>)
                .MakeGenericType(valueType)
                .GetMethod(
                    nameof(Result<object>.Failure),
                    [typeof(Error)]);

            if (failureMethod is null)
            {
                throw new InvalidOperationException(
                    $"Could not find Failure method on response type '{responseType.Name}'.");
            }

            return (TResponse)failureMethod.Invoke(
                obj: null,
                parameters: [error])!;
        }

        throw new InvalidOperationException(
            $"AuditValidationBehavior only supports Result or Result<T> responses. Response type: '{responseType.Name}'.");
    }
}