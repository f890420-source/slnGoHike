using NetTopologySuite;
using NetTopologySuite.Geometries;
using prjGoHike.Models;
using System.Text.Json;

namespace prjGoHike.Services
{
    public class TrailGeometryService
    {

        public static LineString CreateLineString(
        GeometryFactory factory,
        JsonElement coordinates)
        {
            if (coordinates.ValueKind != JsonValueKind.Array ||
                coordinates.GetArrayLength() < 2)
            {
                throw new InvalidDataException(
                    "LineString 至少需要兩個座標點。"
                );
            }

            Coordinate[] points = coordinates
                .EnumerateArray()
                .Select(ReadCoordinate)
                .ToArray();

            return factory.CreateLineString(points);
        }

        public static string GetRequiredString(
        JsonElement element,
        string propertyName)
        {
            if (!element.TryGetProperty(
                    propertyName,
                    out JsonElement property) ||
                property.ValueKind != JsonValueKind.String)
            {
                throw new InvalidDataException(
                    $"GeoJSON 缺少 {propertyName}。"
                );
            }

            return property.GetString()!;
        }

        public static double[][] GetRouteCoordinates(
        Trail trail)
        {
            TrailSegment? segment =
                trail.TrailSegments.SingleOrDefault();

            if (segment?.RoutePath == null)
            {
                return Array.Empty<double[]>();
            }

            return segment.RoutePath.Coordinates
                .Select(coordinate => new[]
                {
            coordinate.X,
            coordinate.Y
                })
                .ToArray();
        }

        public static Coordinate ReadCoordinate(
        JsonElement position)
        {
            if (position.ValueKind != JsonValueKind.Array ||
                position.GetArrayLength() < 2)
            {
                throw new InvalidDataException(
                    "每個座標必須包含經度與緯度。"
                );
            }

            if (!position[0].TryGetDouble(out double longitude) ||
                !position[1].TryGetDouble(out double latitude))
            {
                throw new InvalidDataException(
                    "經緯度必須是數字。"
                );
            }

            if (longitude is < -180 or > 180)
            {
                throw new InvalidDataException(
                    $"經度超出範圍：{longitude}。"
                );
            }

            if (latitude is < -90 or > 90)
            {
                throw new InvalidDataException(
                    $"緯度超出範圍：{latitude}。"
                );
            }

            // GeoJSON：[經度, 緯度]
            // NTS：Coordinate(X, Y)
            return new Coordinate(longitude, latitude);
        }

        public static async Task<LineString> ReadTrailGeometryAsync(
        IFormFile file)
        {
            const long maxFileSize = 10 * 1024 * 1024;

            if (file.Length == 0)
            {
                throw new InvalidDataException(
                    "上傳的 GeoJSON 是空檔案。"
                );
            }

            if (file.Length > maxFileSize)
            {
                throw new InvalidDataException(
                    "GeoJSON 檔案不可超過 10 MB。"
                );
            }

            try
            {
                await using Stream stream = file.OpenReadStream();

                using JsonDocument document =
                    await JsonDocument.ParseAsync(stream);

                JsonElement root = document.RootElement;

                if (GetRequiredString(root, "type") !=
                    "FeatureCollection")
                {
                    throw new InvalidDataException(
                        "GeoJSON 類型必須是 FeatureCollection。"
                    );
                }

                if (!root.TryGetProperty(
                        "features",
                        out JsonElement features) ||
                    features.ValueKind != JsonValueKind.Array)
                {
                    throw new InvalidDataException(
                        "GeoJSON 缺少 features 陣列。"
                    );
                }

                if (features.GetArrayLength() != 1)
                {
                    throw new InvalidDataException(
                        "GeoJSON 必須且只能包含一個 Feature。"
                    );
                }

                JsonElement feature = features[0];

                if (GetRequiredString(feature, "type") != "Feature")
                {
                    throw new InvalidDataException(
                        "features 內容必須是 Feature。"
                    );
                }

                if (!feature.TryGetProperty(
                        "geometry",
                        out JsonElement geometryElement) ||
                    geometryElement.ValueKind != JsonValueKind.Object)
                {
                    throw new InvalidDataException(
                        "Feature 缺少 geometry。"
                    );
                }

                string geometryType = GetRequiredString(geometryElement, "type");

                if (geometryType != "LineString")
                {
                    throw new InvalidDataException(
                        "目前只支援 LineString 路線。"
                    );
                }

                if (!geometryElement.TryGetProperty(
                        "coordinates",
                        out JsonElement coordinates))
                {
                    throw new InvalidDataException(
                        "geometry 缺少 coordinates。"
                    );
                }

                GeometryFactory factory =
                    NtsGeometryServices.Instance
                        .CreateGeometryFactory(srid: 4326);

                return CreateLineString(factory, coordinates);
            }
            catch (JsonException)
            {
                throw new InvalidDataException(
                    "檔案不是有效的 JSON 格式。"
                );
            }
        }
    }
}
