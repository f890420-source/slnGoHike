using System.Net;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using prjGoHike.Models;
using prjGoHike.Services;

static class TrailIndicatorApiChecks
{
    public static async Task RunAsync(TestDataContext context,
        Func<HttpMethod, string, JsonNode?, HttpStatusCode, Task<JsonNode?>> send,
        Action<string?, string> authenticate, Action<bool, string> check, JsonNode openApi)
    {
        var trail = new Trail { TrailId = 91001, TrailName = "Related trail", IsPublished = true };
        var empty = new Trail { TrailId = 91002, TrailName = "Empty trail", IsPublished = true };
        var hidden = new Trail { TrailId = 91003, TrailName = "Hidden trail", IsPublished = false };
        var other = new Trail { TrailId = 91004, TrailName = "Other trail", IsPublished = true };
        var inactiveOnly = new Trail { TrailId = 91005, TrailName = "Inactive indicators", IsPublished = true };
        foreach (var item in new[] { trail, empty, hidden, other, inactiveOnly }) context.Trails.Add(item);
        var candidate = new Indicator { IndicatorId = 92002, IndicatorName = "Candidate", IndicatorType = "Risk", IndicatorLevel = 3, IsActive = true };
        var zero = new Indicator { IndicatorId = 92001, IndicatorName = "Zero score", IndicatorType = "Risk", IsActive = true };
        var scored = new Indicator { IndicatorId = 92003, IndicatorName = "Scored", IndicatorType = "Risk", IsActive = true };
        var inactive = new Indicator { IndicatorId = 92004, IndicatorName = "Inactive", IndicatorType = "Risk", IsActive = false };
        var unrelated = new Indicator { IndicatorId = 92005, IndicatorName = "Other trail only", IndicatorType = "Risk", IsActive = true };
        foreach (var item in new[] { candidate, zero, scored, inactive, unrelated }) context.Indicators.Add(item);
        // Deliberately unordered. Navigation properties represent the mapped relationship.
        Link(trail, scored, 5.5m, 0m);
        Link(trail, candidate, null, 12.34m);
        Link(trail, zero, 0m, null);
        Link(trail, inactive, null, 1m);
        Link(hidden, candidate, null, 1m);
        Link(inactiveOnly, inactive, null, 1m);
        Link(other, unrelated, null, 1m);

        var saves = context.Saves;
        authenticate(null, "1");
        var result = (await send(HttpMethod.Get, $"/api/trails/{trail.TrailId}/indicators", null, HttpStatusCode.OK))!["data"]!;
        var items = result["indicators"]!.AsArray();
        check(result["trailId"]!.GetValue<long>() == trail.TrailId && result["hasIndicators"]!.GetValue<bool>(),
            "Anonymous client receives the requested published trail and association flag.");
        check(items.Select(item => item!["indicatorId"]!.GetValue<long>()).SequenceEqual(new long[] { 92001, 92002, 92003 }),
            "Includes unscored, zero and nonzero scores in ID order; excludes inactive and other-trail indicators.");
        check(items[0]!["evaluatedScore"]!.GetValue<decimal>() == 0m && items[0]!["distanceMeters"] is null
            && items[0]!["indicatorLevel"] is null && items[1]!["evaluatedScore"] is null
            && items[2]!["evaluatedScore"]!.GetValue<decimal>() == 5.5m,
            "Null scores, unknown distance/level and actual zero remain distinct.");
        check(items[1]!["indicatorName"]!.GetValue<string>() == "Candidate"
            && items[1]!["indicatorType"]!.GetValue<string>() == "Risk"
            && items[1]!["indicatorLevel"]!.GetValue<int>() == 3
            && items[1]!["distanceMeters"]!.GetValue<decimal>() == 12.34m,
            "Projects public indicator fields and persisted relation distance.");
        check(result.AsObject().Count == 3 && items.All(item => item!.AsObject().Count == 6),
            "Compact DTO excludes geometry, internal job data and navigation cycles.");
        foreach (var item in new[] { empty, inactiveOnly })
        {
            var noMatches = (await send(HttpMethod.Get, $"/api/trails/{item.TrailId}/indicators", null, HttpStatusCode.OK))!["data"]!;
            check(!noMatches["hasIndicators"]!.GetValue<bool>() && noMatches["indicators"]!.AsArray().Count == 0,
                "Published trail with no visible associations returns false and an empty list.");
        }
        foreach (var id in new[] { hidden.TrailId, long.MaxValue })
            await send(HttpMethod.Get, $"/api/trails/{id}/indicators", null, HttpStatusCode.NotFound);
        foreach (var id in new[] { 0, -1 })
            await send(HttpMethod.Get, $"/api/trails/{id}/indicators", null, HttpStatusCode.BadRequest);
        var detail = (await send(HttpMethod.Get, $"/api/trails/{trail.TrailId}", null, HttpStatusCode.OK))!["data"]!.AsObject();
        check(detail.ContainsKey("trailSegDtos") && !detail.ContainsKey("indicators") && !detail.ContainsKey("hasIndicators"),
            "Existing public trail detail preserves its DTO contract.");
        context.FailTrailRead = true;
        try
        {
            var failure = await send(HttpMethod.Get, $"/api/trails/{trail.TrailId}/indicators", null, HttpStatusCode.InternalServerError);
            check(!failure!.ToJsonString().Contains("private database details"), "Read failures return a controlled error.");
        }
        finally { context.FailTrailRead = false; }
        check(context.Saves == saves, "Association reads never save data or start synchronization.");

        var operation = openApi["paths"]!["/api/trails/{id}/indicators"]!["get"]!;
        check(operation["summary"] is not null && operation["description"] is not null,
            "OpenAPI describes the public association endpoint and its semantics.");
        foreach (var status in new[] { "200", "400", "404", "500" })
            check(operation["responses"]![status] is not null, "OpenAPI documents association status codes.");
        check(openApi["components"]!["schemas"]!["TrailIndicatorsDto"]!["properties"]!["hasIndicators"] is not null,
            "OpenAPI exposes the computed association flag for Angular clients.");

        void Link(Trail source, Indicator indicator, decimal? score, decimal? distance)
        {
            var link = new TrailIndicator { Trail = source, TrailId = source.TrailId,
                Indicator = indicator, IndicatorId = indicator.IndicatorId, EvaluatedScore = score, DistanceMeters = distance };
            source.TrailIndicators.Add(link);
            indicator.TrailIndicators.Add(link);
            context.TrailIndicators.Add(link);
        }
    }

    public static void CheckSqlTranslation(GoHikeDataContext context, Action<bool, string> check)
    {
        // Translate the exact production query with the production EF SQL Server model.
        var sql = TrailIndicatorQuery.ForPublishedTrail(context, 91001).ToQueryString();
        check(sql.Contains("[TrailIndicators]") && sql.Contains("[Indicators]") && sql.Contains("JOIN")
            && sql.Contains("[IsPublished]") && sql.Contains("[IsActive]"),
            "Production navigation projection translates to SQL with publication/activation filters.");
        check(sql.Contains("[DistanceMeters]") && sql.Contains("[EvaluatedScore]")
            && !sql.Contains("[TrailSegments]") && !sql.Contains("[IndicatorSegments]") && !sql.Contains("[Shape]"),
            "SQL projection fetches relation fields without geometry or segment queries.");
        check(context.Model.FindEntityType(typeof(TrailIndicator))!.FindProperty(nameof(TrailIndicator.EvaluatedScore))!.IsNullable,
            "Production EF relation keeps scores nullable.");
    }
}
