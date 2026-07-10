namespace PulseRisk.Application.Common;

public static class Pagination
{
    public const int DefaultPageNumber = 1;
    public const int DefaultPageSize = 50;
    public const int MaxPageSize = 200;

    public static (int PageNumber, int PageSize) Normalize(int pageNumber, int pageSize)
    {
        var normalizedPageSize = pageSize <= 0
            ? DefaultPageSize
            : Math.Min(pageSize, MaxPageSize);

        var maxPageNumber = (int)Math.Min(
            int.MaxValue,
            ((long)int.MaxValue / normalizedPageSize) + 1L);

        var normalizedPageNumber = pageNumber < DefaultPageNumber
            ? DefaultPageNumber
            : Math.Min(pageNumber, maxPageNumber);

        return (normalizedPageNumber, normalizedPageSize);
    }

    public static int CalculateOffset(int pageNumber, int pageSize)
    {
        var (normalizedPageNumber, normalizedPageSize) = Normalize(pageNumber, pageSize);

        return (normalizedPageNumber - 1) * normalizedPageSize;
    }
}
