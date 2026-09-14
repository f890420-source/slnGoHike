using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using NetTopologySuite.Geometries;

namespace prjGoHike.Services;

/// <summary>Documents the GeoJSON wire format instead of NetTopologySuite internals.</summary>
public sealed class GeoJsonSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (!typeof(Geometry).IsAssignableFrom(context.JsonTypeInfo.Type))
            return Task.CompletedTask;

        schema.Type = JsonSchemaType.Object;
        schema.Description = "GeoJSON 幾何物件。coordinates 的巢狀層數依 type 而定，位置順序為經度、緯度（可含高度）；GeometryCollection 使用 geometries。";
        schema.Properties = new Dictionary<string, IOpenApiSchema>
        {
            ["type"] = new OpenApiSchema
            {
                Type = JsonSchemaType.String,
                Enum = new[] { "Point", "MultiPoint", "LineString", "MultiLineString", "Polygon", "MultiPolygon", "GeometryCollection" }
                    .Select(value => (JsonNode)JsonValue.Create(value)!).ToList()
            },
            ["coordinates"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Array,
                Description = "Point 為數值陣列；其他座標型別為依 GeoJSON 規範巢狀排列的陣列。",
                Items = new OpenApiSchema()
            },
            ["geometries"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Array,
                Description = "僅 GeometryCollection 使用，元素為 GeoJSON 幾何物件。",
                Items = new OpenApiSchema { Type = JsonSchemaType.Object }
            }
        };
        schema.Required = new HashSet<string> { "type" };
        schema.Example = JsonNode.Parse("""
            { "type": "LineString", "coordinates": [[121.56, 25.03], [121.57, 25.04]] }
            """);
        return Task.CompletedTask;
    }
}
