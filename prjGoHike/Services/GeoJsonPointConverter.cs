using System.Text.Json;
using System.Text.Json.Serialization;
using NetTopologySuite.Geometries;

namespace prjGoHike.Services;

/// <summary>Uses the existing GeoJSON validation and restricts input to a point.</summary>
public sealed class GeoJsonPointConverter : JsonConverter<Point>
{
    private static readonly GeoJsonGeometryConverter GeometryConverter = new();

    public override Point? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var geometry = GeometryConverter.Read(ref reader, typeof(Geometry), options);
        return geometry as Point ?? throw new JsonException("location：位置必須是有效的 GeoJSON Point。");
    }

    public override void Write(Utf8JsonWriter writer, Point value, JsonSerializerOptions options) =>
        GeometryConverter.Write(writer, value, options);
}
