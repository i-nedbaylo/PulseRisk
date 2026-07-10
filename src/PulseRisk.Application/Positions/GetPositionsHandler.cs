using PulseRisk.Application.Repositories;

namespace PulseRisk.Application.Positions;

public sealed class GetPositionsHandler(IPositionRepository positions)
{
    public async Task<IReadOnlyCollection<PositionDto>> HandleAsync(
        GetPositionsQuery query,
        CancellationToken cancellationToken)
    {
        var result = await positions.SearchAsync(query, cancellationToken);

        return result.Select(position => position.ToDto()).ToArray();
    }
}
