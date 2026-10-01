using System.Text.Json;
using System.Text.Json.Nodes;
using NetTopologySuite.Algorithm;
using NetTopologySuite.Geometries;
using prjGoHike.DTO.GoHikeSafe;

static class GeoJsonValidationChecks
{
    public const string ReversedPolygon = """
        {"type":"Polygon","coordinates":[
          [[121,24,10],[121,28,10],[125,28,10],[125,24,10],[121,24,10]],
          [[122,25,20],[123,25,20],[123,26,20],[122,26,20],[122,25,20]]]}
        """;

    public static void Run(Action<bool, string> check)
    {
        var valid = new[]
        {
            """{"coordinates":[121,24],"type":"Point","note":{"properties":"foreign data"},"bbox":[120,23,122,25]}""",
            """{"type":"MultiPoint","coordinates":[[179,24],[-179,25]],"bbox":[178,23,-178,26]}""",
            """{"type":"LineString","coordinates":[[121,24,10],[122,25,20]],"bbox":[120,23,0,123,26,30]}""",
            """{"type":"MultiLineString","coordinates":[[[121,24],[122,25]],[[123,26],[124,27]]]}""",
            ReversedPolygon,
            new JsonObject { ["type"] = "MultiPolygon", ["coordinates"] = new JsonArray(JsonNode.Parse(ReversedPolygon)!["coordinates"]!.DeepClone()) }.ToJsonString(),
            new JsonObject { ["type"] = "GeometryCollection", ["geometries"] = new JsonArray(
                new JsonObject { ["type"] = "GeometryCollection", ["geometries"] = new JsonArray(JsonNode.Parse(ReversedPolygon)) }) }.ToJsonString()
        };
        foreach (var json in valid)
        {
            var shape = Read(json);
            check(shape.SRID == 4326, "All seven Geometry types use SRID 4326.");
            CheckOrientation(shape, check);
        }
        var polygon = (Polygon)Read(ReversedPolygon);
        check(polygon.ExteriorRing.CoordinateSequence.GetZ(0) == 10
            && polygon.GetInteriorRingN(0).CoordinateSequence.GetZ(0) == 20,
            "Direction correction preserves shell and hole heights.");
        var roundTrip = JsonSerializer.Deserialize<IndicatorSegmentDto>(JsonSerializer.Serialize(
            new IndicatorSegmentDto { Shape = polygon }))!.Shape;
        check(((Polygon)roundTrip).GetInteriorRingN(0).CoordinateSequence.GetZ(0) == 20,
            "NTS output preserves height and can be read again.");

        var invalid = new[]
        {
            """{"type":"point","coordinates":[121,24]}""",
            """{"type":"Point","coordinates":[[121,24]]}""",
            """{"type":"Point","coordinates":[121,24,10,20]}""",
            """{"type":"Point","coordinates":[121,91]}""",
            """{"type":"Point","coordinates":[181,24]}""",
            """{"type":"Point","coordinates":[121,24,1e400]}""",
            """{"type":"Point","coordinates":["121",24]}""",
            """{"type":"LineString","coordinates":[[121,24]]}""",
            """{"type":"Polygon","coordinates":[[[121,24],[122,24],[122,25],[121,25]]]}""",
            """{"type":"Polygon","coordinates":[[[121,24,10],[122,24,10],[122,25,10],[121,24,11]]]}""",
            """{"type":"Polygon","coordinates":[[[121,24],[122,25],[121,25],[122,24],[121,24]]]}""",
            """{"type":"MultiLineString","coordinates":[[]]}""",
            """{"type":"GeometryCollection","geometries":[]}""",
            """{"type":"Feature","geometry":{"type":"Point","coordinates":[121,24]},"properties":{}}""",
            """{"type":"Point","coordinates":[121,24],"properties":{}}""",
            """{"type":"Point","coordinates":[121,24],"crs":{}}""",
            """{"type":"Point","coordinates":[121,24],"bbox":[122,23,123,25]}""",
            """{"type":"Point","coordinates":[121,24],"bbox":[120,23,0,122,25,30]}""",
            """{"type":"Point","coordinates":[121,24],"bbox":[120,25,122,23]}""",
            """{"type":"Point","coordinates":[121,24],"bbox":[120,23,122,25,0]}"""
        };
        foreach (var json in invalid)
        {
            try { Read(json); }
            catch (JsonException ex)
            {
                check(ex.Message.Contains("shape"), "Invalid Geometry has a controlled field-specific error.");
                continue;
            }
            check(false, "Invalid GeoJSON must be rejected: " + json);
        }
    }

    private static Geometry Read(string json) => JsonSerializer.Deserialize<IndicatorSegmentDto>(
        new JsonObject { ["shape"] = JsonNode.Parse(json) }.ToJsonString(),
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!.Shape;

    public static void CheckOrientation(Geometry shape, Action<bool, string> check)
    {
        check(shape.SRID == 4326, "Nested geometry retains SRID 4326.");
        if (shape is Polygon polygon)
        {
            check(Orientation.IsCCW(polygon.ExteriorRing.CoordinateSequence), "Stored shell is counterclockwise.");
            for (var i = 0; i < polygon.NumInteriorRings; i++)
                check(!Orientation.IsCCW(polygon.GetInteriorRingN(i).CoordinateSequence), "Stored hole is clockwise.");
        }
        else if (shape is GeometryCollection collection)
            for (var i = 0; i < collection.NumGeometries; i++) CheckOrientation(collection.GetGeometryN(i), check);
    }
}
