namespace CommercialNews.BuildingBlocks.SharedKernel.Results;

    public sealed record Error
    {
        public string Code { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
        public ErrorType Type { get; init; }
        public IReadOnlyCollection<string> Details { get; init; } = Array.Empty<string>();
        
        public static Error Validation(string code, string message, params string[] details) =>
            new ()
            {
                Code = code,
                Message = message,
                Type = ErrorType.Validation,
                Details =   NormalizeDetails(details)
            };

        public static Error Unauthorized(string code, string message, params string[] details) =>
        new()
        {
            Code = code,
            Message = message,
            Type = ErrorType.Unauthorized,
            Details = NormalizeDetails(details)
        };

        public static Error Forbidden(string code, string message, params string[] details) =>
            new()
            {
                Code = code,
                Message = message,
                Type = ErrorType.Forbidden,
                Details = NormalizeDetails(details)
            };

        public static Error NotFound(string code, string message, params string[] details) =>
            new()
            {
                Code = code,
                Message = message,
                Type = ErrorType.NotFound,
                Details = NormalizeDetails(details)
            };

        public static Error Conflict(string code, string message, params string[] details) =>
            new()
            {
                Code = code,
                Message = message,
                Type = ErrorType.Conflict,
                Details = NormalizeDetails(details)
            };

        public static Error RateLimited(string code, string message, params string[] details) =>
            new()
            {
                Code = code,
                Message = message,
                Type = ErrorType.RateLimited,
                Details = NormalizeDetails(details)
            };

        public static Error Failure(string code, string message, params string[] details) =>
            new()
            {
                Code = code,
                Message = message,
                Type = ErrorType.Failure,
                Details = NormalizeDetails(details)
            };

         private static IReadOnlyCollection<string> NormalizeDetails(IEnumerable<string>? details)
        {
            if (details is null)
            {
                return Array.Empty<string>();
            }

            return details
                .Where(static x => !string.IsNullOrWhiteSpace(x))
                .Select(static x => x.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }
    }
