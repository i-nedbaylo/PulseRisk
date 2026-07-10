using FluentAssertions;
using PulseRisk.Application.Common;

namespace PulseRisk.UnitTests.Application.Common;

public sealed class PaginationTests
{
    [Theory]
    [InlineData(0, 0, 1, 50)]
    [InlineData(-10, -20, 1, 50)]
    [InlineData(3, 500, 3, 200)]
    public void Normalize_ShouldClampInvalidPagingValues(
        int pageNumber,
        int pageSize,
        int expectedPageNumber,
        int expectedPageSize)
    {
        var result = Pagination.Normalize(pageNumber, pageSize);

        result.PageNumber.Should().Be(expectedPageNumber);
        result.PageSize.Should().Be(expectedPageSize);
    }

    [Fact]
    public void Normalize_WhenPageNumberIsTooLarge_ShouldClampToSafeOffset()
    {
        var result = Pagination.Normalize(int.MaxValue, Pagination.MaxPageSize);

        result.PageNumber.Should().Be((int.MaxValue / Pagination.MaxPageSize) + 1);
        result.PageSize.Should().Be(Pagination.MaxPageSize);
        Pagination.CalculateOffset(result.PageNumber, result.PageSize)
            .Should()
            .BeLessThanOrEqualTo(int.MaxValue);
    }

    [Fact]
    public void CalculateOffset_ShouldNormalizeBeforeCalculatingOffset()
    {
        var offset = Pagination.CalculateOffset(int.MaxValue, Pagination.MaxPageSize);

        offset.Should().BeGreaterThan(0);
        offset.Should().BeLessThanOrEqualTo(int.MaxValue);
    }
}
