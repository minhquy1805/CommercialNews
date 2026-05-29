using Microsoft.Extensions.Options;
using Notifications.Application.Configuration;
using Notifications.Application.Contracts.Services;
using Notifications.Application.Ports.Services;
using Notifications.Domain.Enums;

namespace Notifications.Infrastructure.Services;

public sealed class EmailDeliveryRetryPolicy : IEmailDeliveryRetryPolicy
{
    private readonly EmailDeliveryOptions _options;

    public EmailDeliveryRetryPolicy(
        IOptions<EmailDeliveryOptions> options)
    {
        _options = options?.Value
            ?? throw new ArgumentNullException(nameof(options));
    }

    public RetryDecision Evaluate(
        EmailDeliveryRetryContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.AttemptCount >= _options.MaxAttemptCount)
        {
            return RetryDecision.MarkDead("Max attempt count reached.");
        }

        if (EmailErrorClass.IsGenerallyTerminal(context.ErrorClass))
        {
            return RetryDecision.MarkDead("Terminal error class.");
        }

        if (!EmailErrorClass.IsGenerallyRetryable(context.ErrorClass))
        {
            return RetryDecision.MarkDead("Non-retryable error class.");
        }

        int delaySeconds = CalculateDelaySeconds(context.AttemptCount);

        DateTime nextRetryAt = context.NowUtc.AddSeconds(delaySeconds);

        return RetryDecision.RetryAt(
            nextRetryAt,
            reason: $"Retry scheduled after {delaySeconds} seconds.");
    }

    private int CalculateDelaySeconds(int attemptCount)
    {
        int safeAttemptCount = Math.Max(1, attemptCount);

        double exponentialDelay =
            _options.InitialRetryDelaySeconds * Math.Pow(2, safeAttemptCount - 1);

        int cappedDelay = Math.Min(
            (int)exponentialDelay,
            _options.MaxRetryDelaySeconds);

        return Math.Max(1, cappedDelay);
    }
}
