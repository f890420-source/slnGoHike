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
        schema.Description = "RFC 7946 Geometry；座標為 WGS 84 經度、緯度及可選高度（2D/3D），SRID 4326。" +
            "本 API 拒絕空圖形、無效拓樸及 crs 宣告；Polygon 外環自動調整為逆時針，內環為順時針。";
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
                Description = "Point 為 2 或 3 個有限數值；經度 -180～180、緯度 -90～90。其他型別依 RFC 巢狀排列。LineString 至少兩點；環至少四點且首尾所有數值相同。",
                Items = new OpenApiSchema()
            },
            ["geometries"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Array,
                Description = "僅 GeometryCollection 使用，元素為 GeoJSON 幾何物件。",
                Items = new OpenApiSchema { Type = JsonSchemaType.Object }
            },
            ["bbox"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Array,
                Description = "可選，四個（2D）或六個（3D）有限數值；維度須與所有座標一致且涵蓋所有座標。允許跨日期變更線的 west > east。驗證後不保存。",
                Items = new OpenApiSchema { Type = JsonSchemaType.Number }
            }
        };
        schema.Required = new HashSet<string> { "type" };
        schema.OneOf = new List<IOpenApiSchema>
        {
            new OpenApiSchema
            {
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["type"] = new OpenApiSchema { Enum = new List<JsonNode> { JsonValue.Create("GeometryCollection")! } }
                },
                Required = new HashSet<string> { "geometries" }
            },
            new OpenApiSchema
            {
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["type"] = new OpenApiSchema
                    {
                        Enum = new[] { "Point", "MultiPoint", "LineString", "MultiLineString", "Polygon", "MultiPolygon" }
                            .Select(value => (JsonNode)JsonValue.Create(value)!).ToList()
                    }
                },
                Required = new HashSet<string> { "coordinates" }
            }
        };
        schema.Example = JsonNode.Parse("""
            { "type": "LineString", "coordinates": [[121.56, 25.03], [121.57, 25.04]] }
            """);
        return Task.CompletedTask;
    }
}
