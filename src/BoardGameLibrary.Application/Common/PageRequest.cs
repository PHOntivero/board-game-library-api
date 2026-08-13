namespace BoardGameLibrary.Application.Common;

public sealed record PageRequest
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 20;
    public const int MaximumPageSize = 100;

    private PageRequest(int page, int pageSize, string sortBy, SortDirection sortDirection)
    {
        Page = page;
        PageSize = pageSize;
        SortBy = sortBy;
        SortDirection = sortDirection;
    }

    public int Page { get; }

    public int PageSize { get; }

    public string SortBy { get; }

    public SortDirection SortDirection { get; }

    public int Offset => (Page - 1) * PageSize;

    public static Result<PageRequest> Create(
        int? page,
        int? pageSize,
        string? sortBy,
        string? sortDirection,
        IReadOnlyCollection<string> allowedSortFields,
        string defaultSortBy,
        SortDirection defaultDirection)
    {
        string[] allowedFields = ValidateSortConfiguration(allowedSortFields, defaultSortBy);

        if (!Enum.IsDefined(defaultDirection))
        {
            throw new ArgumentOutOfRangeException(
                nameof(defaultDirection),
                defaultDirection,
                "The default sort direction is invalid.");
        }

        string canonicalDefaultSort = allowedFields.Single(
            field => string.Equals(field, defaultSortBy, StringComparison.OrdinalIgnoreCase));

        int resolvedPage = page ?? DefaultPage;
        int resolvedPageSize = pageSize ?? DefaultPageSize;
        var errors = new List<Error>();

        if (resolvedPage < 1 ||
            resolvedPageSize < 1 ||
            resolvedPageSize > MaximumPageSize ||
            (long)(resolvedPage - 1) * resolvedPageSize > int.MaxValue)
        {
            errors.Add(Error.Validation(
                ErrorCodes.Common.ValidationFailed,
                $"Page must be at least 1, pageSize must be between 1 and {MaximumPageSize}, and the resulting offset must be supported."));
        }

        string requestedSort = string.IsNullOrWhiteSpace(sortBy)
            ? canonicalDefaultSort
            : sortBy.Trim();
        string? canonicalSort = allowedFields.FirstOrDefault(
            field => string.Equals(field, requestedSort, StringComparison.OrdinalIgnoreCase));

        if (canonicalSort is null)
        {
            errors.Add(Error.Validation(
                ErrorCodes.Common.ValidationFailed,
                $"sortBy must be one of: {string.Join(", ", allowedFields)}."));
        }

        SortDirection? resolvedDirection = ParseSortDirection(sortDirection, defaultDirection);

        if (resolvedDirection is null)
        {
            errors.Add(Error.Validation(
                ErrorCodes.Common.ValidationFailed,
                "sortDirection must be either 'asc' or 'desc'."));
        }

        if (errors.Count > 0)
        {
            return Result<PageRequest>.Failure(errors);
        }

        return Result<PageRequest>.Success(new PageRequest(
            resolvedPage,
            resolvedPageSize,
            canonicalSort!,
            resolvedDirection!.Value));
    }

    private static string[] ValidateSortConfiguration(
        IReadOnlyCollection<string> allowedSortFields,
        string defaultSortBy)
    {
        ArgumentNullException.ThrowIfNull(allowedSortFields);
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultSortBy);

        string[] fields = allowedSortFields
            .Where(field => !string.IsNullOrWhiteSpace(field))
            .Select(field => field.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (fields.Length == 0)
        {
            throw new ArgumentException("At least one sort field must be configured.", nameof(allowedSortFields));
        }

        if (!fields.Contains(defaultSortBy, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException("The default sort field must be included in the allowed fields.", nameof(defaultSortBy));
        }

        return fields;
    }

    private static SortDirection? ParseSortDirection(
        string? value,
        SortDirection defaultDirection)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultDirection;
        }

        if (string.Equals(value.Trim(), "asc", StringComparison.OrdinalIgnoreCase))
        {
            return Common.SortDirection.Ascending;
        }

        if (string.Equals(value.Trim(), "desc", StringComparison.OrdinalIgnoreCase))
        {
            return Common.SortDirection.Descending;
        }

        return null;
    }
}
