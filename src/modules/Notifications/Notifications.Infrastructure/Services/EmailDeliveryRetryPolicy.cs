using Notifications.Application.Contracts.Services;
using Notifications.Application.Ports.Services;
using Notifications.Domain.Enums;

namespace Notifications.Application.Services;

public sealed class EmailDeliveryRetryPolicy : IEmailDeliveryRetryPolicy
{
    private const int MaxAttempts = 5;

    public RetryDecision Evaluate(EmailDeliveryRetryContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.AttemptCount >= MaxAttempts)
        {
            return RetryDecision.MarkDead("Maximum retry attempts exceeded.");
        }

        if (string.Equals(context.ErrorClass, EmailErrorClass.Permanent, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(context.ErrorClass, EmailErrorClass.Policy, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(context.ErrorClass, EmailErrorClass.Template, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(context.ErrorClass, EmailErrorClass.Validation, StringComparison.OrdinalIgnoreCase))
        {
            return RetryDecision.MarkDead("Failure is classified as terminal.");
        }

        if (string.Equals(context.ErrorClass, EmailErrorClass.Transient, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(context.ErrorClass, EmailErrorClass.Ambiguous, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(context.ErrorClass, EmailErrorClass.Provider, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(context.ErrorClass, EmailErrorClass.Unknown, StringComparison.OrdinalIgnoreCase))
        {
            TimeSpan delay = CalculateBackoff(context.AttemptCount, context.IsAmbiguous);

            return RetryDecision.RetryAt(
                context.NowUtc.Add(delay),
                "Failure is retryable.");
        }

        return RetryDecision.MarkDead("Unsupported error classification.");
    }

    private static TimeSpan CalculateBackoff(int attemptCount, bool isAmbiguous)
    {
        int safeAttempt = Math.Clamp(attemptCount, 1, MaxAttempts);

        int seconds = safeAttempt switch
        {
            1 => 30,
            2 => 120,
            3 => 300,
            4 => 900,
            _ => 1800
        };

        if (isAmbiguous)
        {
            seconds *= 2;
        }

        return TimeSpan.FromSeconds(seconds);
    }
}