namespace PulseRisk.Application.Common;

public static class Pagination
{
    public const int DefaultPageNumber = 1;
    public const int DefaultPageSize = 50;
    public const int MaxPageSize = 200;

    public static (int PageNumber, int PageSize) Normalize(int pageNumber, int pageSize)
    {
        var normalizedPageNumber = pageNumber < DefaultPageNumber
            ? DefaultPageNumber
            : pageNumber;

        var normalizedPageSize = pageSize <= 0
            ? DefaultPageSize
            : Math.Min(pageSize, MaxPageSize);

        return (normalizedPageNumber, normalizedPageSize);
    }
}
