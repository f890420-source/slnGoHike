using System.Text.Json;
using System.Text.Json.Serialization;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO.Converters;

namespace prjGoHike.Services;

/// <summary>Validates API shape input while delegating geometry IO to NTS.</summary>
public sealed class GeoJsonGeometryConverter : JsonConverter<Geometry>
{
    // Separate options avoid calling this property converter recursively.
    private static readonly JsonSerializerOptions GeometryOptions = CreateGeometryOptions();

    private static JsonSerializerOptions CreateGeometryOptions()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new GeoJsonConverterFactory(
            NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326)));
        return options;
    }

    public override Geometry? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        GeoJsonGeometryValidator.ValidateJson(document.RootElement);
        Geometry geometry;
        try
        {
            geometry = document.RootElement.Deserialize<Geometry>(GeometryOptions)!;
        }
        catch (Exception ex) when (ex is JsonException or ArgumentException or FormatException)
        {
            throw new JsonException("GeoJSON 無法解析為有效的幾何物件。");
        }
        return GeoJsonGeometryValidator.ValidateAndOrient(geometry);
    }

    public override void Write(Utf8JsonWriter writer, Geometry value, JsonSerializerOptions options) =>
        JsonSerializer.Serialize(writer, value, GeometryOptions);
}
