using Microsoft.EntityFrameworkCore;
using prjGoHike.DTO.GoHikeSafe;
using prjGoHike.Models;

namespace prjGoHike.Services;

public static class TrailIndicatorQuery
{
    public static IQueryable<TrailIndicatorsDto> ForPublishedTrail(GoHikeDataContext context, long trailId) =>
        context.Trails.AsNoTracking()
            .Where(trail => trail.IsPublished && trail.TrailId == trailId)
            .Select(trail => new TrailIndicatorsDto
            {
                TrailId = trail.TrailId,
                Indicators = trail.TrailIndicators
                    .Where(link => link.Indicator.IsActive)
                    .OrderBy(link => link.IndicatorId)
                    .Select(link => new TrailRelatedIndicatorDto
                    {
                        IndicatorId = link.IndicatorId,
                        IndicatorName = link.Indicator.IndicatorName,
                        IndicatorType = link.Indicator.IndicatorType,
                        IndicatorLevel = link.Indicator.IndicatorLevel,
                        DistanceMeters = link.DistanceMeters,
                        EvaluatedScore = link.EvaluatedScore
                    }).ToList()
            });
}
