using System.Text.Json;
using NetTopologySuite.Algorithm;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Valid;

namespace prjGoHike.Services;

internal static class GeoJsonGeometryValidator
{
    public static void ValidateJson(JsonElement geometry) => ValidateObject(geometry, "shape");

    private static List<double[]> ValidateObject(JsonElement geometry, string path)
    {
        if (geometry.ValueKind != JsonValueKind.Object)
            Fail(path, "必須是 GeoJSON 幾何物件。");

        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var member in geometry.EnumerateObject())
        {
            if (!names.Add(member.Name)) Fail(path, "不可包含重複的成員名稱。");
            if (member.Name is "geometry" or "properties" or "features")
                Fail(path + "." + member.Name, "幾何物件不可包含其他 GeoJSON 型別的保留成員。");
            if (member.Name == "crs") Fail(path + ".crs", "本 API 僅接受 WGS 84 經緯度，不接受 crs 宣告。");
        }
        var type = Required(geometry, "type", path);
        if (type.ValueKind != JsonValueKind.String) Fail(path + ".type", "必須是幾何型別字串。");
        var typeName = type.GetString();
        List<double[]> positions;
        if (typeName == "GeometryCollection")
        {
            if (geometry.TryGetProperty("coordinates", out _)) Fail(path, "GeometryCollection 不可包含 coordinates。");
            var children = Required(geometry, "geometries", path);
            RequireArray(children, 1, path + ".geometries");
            positions = [];
            var index = 0;
            foreach (var child in children.EnumerateArray())
                positions.AddRange(ValidateObject(child, $"{path}.geometries[{index++}]"));
        }
        else
        {
            if (typeName is not ("Point" or "MultiPoint" or "LineString" or "MultiLineString" or "Polygon" or "MultiPolygon"))
                Fail(path + ".type", "必須是 RFC 7946 的七種幾何型別之一，且大小寫必須正確。");
            if (geometry.TryGetProperty("geometries", out _)) Fail(path, "只有 GeometryCollection 可以包含 geometries。");
            var coordinates = Required(geometry, "coordinates", path);
            var coordinatePath = path + ".coordinates";
            positions = typeName switch
            {
                "Point" => [Position(coordinates, coordinatePath)],
                "MultiPoint" => Positions(coordinates, 1, false, coordinatePath),
                "LineString" => Positions(coordinates, 2, false, coordinatePath),
                "MultiLineString" => Parts(coordinates, false, coordinatePath),
                "Polygon" => Parts(coordinates, true, coordinatePath),
                _ => Polygons(coordinates, coordinatePath)
            };
        }
        if (geometry.TryGetProperty("bbox", out var bbox)) ValidateBbox(bbox, positions, path + ".bbox");
        return positions;
    }

    private static JsonElement Required(JsonElement obj, string name, string path)
    {
        if (!obj.TryGetProperty(name, out var value)) Fail(path + "." + name, "缺少必要成員。");
        return value;
    }

    private static void RequireArray(JsonElement value, int minimum, string path)
    {
        if (value.ValueKind != JsonValueKind.Array || value.GetArrayLength() < minimum)
            Fail(path, $"必須是至少包含 {minimum} 個元素的陣列；本 API 不接受空圖形。");
    }

    private static double Number(JsonElement value, string path)
    {
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetDouble(out var number) || !double.IsFinite(number))
            throw new JsonException($"{path}：必須是有限數值。");
        return number;
    }

    private static double[] Position(JsonElement value, string path)
    {
        RequireArray(value, 2, path);
        if (value.GetArrayLength() > 3) Fail(path, "本 API 的座標只接受經度、緯度及可選高度。");
        var position = value.EnumerateArray().Select((ordinate, i) => Number(ordinate, $"{path}[{i}]")).ToArray();
        if (Math.Abs(position[0]) > 180 || Math.Abs(position[1]) > 90)
            Fail(path, "經度必須介於 -180～180，緯度必須介於 -90～90。");
        return position;
    }

    private static List<double[]> Positions(JsonElement value, int minimum, bool ring, string path)
    {
        RequireArray(value, minimum, path);
        var positions = value.EnumerateArray().Select((position, i) => Position(position, $"{path}[{i}]")).ToList();
        if (ring && !positions[0].SequenceEqual(positions[^1]))
            Fail(path, "環的首尾座標必須完全相同，包含高度。");
        return positions;
    }

    private static List<double[]> Parts(JsonElement value, bool polygon, string path)
    {
        RequireArray(value, 1, path);
        var positions = new List<double[]>();
        var index = 0;
        foreach (var part in value.EnumerateArray())
            positions.AddRange(Positions(part, polygon ? 4 : 2, polygon, $"{path}[{index++}]"));
        return positions;
    }

    private static List<double[]> Polygons(JsonElement value, string path)
    {
        RequireArray(value, 1, path);
        var positions = new List<double[]>();
        var index = 0;
        foreach (var polygon in value.EnumerateArray())
            positions.AddRange(Parts(polygon, true, $"{path}[{index++}]"));
        return positions;
    }

    private static void ValidateBbox(JsonElement value, List<double[]> positions, string path)
    {
        RequireArray(value, 4, path);
        if (value.GetArrayLength() is not (4 or 6)) Fail(path, "bbox 必須包含四個或六個數值。");
        var bounds = value.EnumerateArray().Select((ordinate, i) => Number(ordinate, $"{path}[{i}]")).ToArray();
        var dimension = bounds.Length / 2;
        if (positions.Any(position => position.Length != dimension)) Fail(path, "bbox 維度必須與涵蓋的所有座標一致。");
        if (Math.Abs(bounds[0]) > 180 || Math.Abs(bounds[dimension]) > 180
            || Math.Abs(bounds[1]) > 90 || Math.Abs(bounds[dimension + 1]) > 90)
            Fail(path, "bbox 經緯度超出範圍。");
        for (var axis = 1; axis < dimension; axis++)
            if (bounds[axis] > bounds[axis + dimension]) Fail(path, "bbox 的緯度與高度下界不可大於上界。");
        foreach (var position in positions)
        {
            // A west > east interval crosses the antimeridian.
            var longitudeInside = bounds[0] <= bounds[dimension]
                ? position[0] >= bounds[0] && position[0] <= bounds[dimension]
                : position[0] >= bounds[0] || position[0] <= bounds[dimension];
            // -180 and 180 represent the same meridian.
            if (!longitudeInside && Math.Abs(position[0]) == 180)
                longitudeInside = bounds[0] == -180 || bounds[dimension] == 180;
            if (!longitudeInside) Fail(path, "bbox 必須涵蓋所有座標。");
            for (var axis = 1; axis < dimension; axis++)
                if (position[axis] < bounds[axis] || position[axis] > bounds[axis + dimension])
                    Fail(path, "bbox 必須涵蓋所有座標。");
        }
    }

    public static Geometry ValidateAndOrient(Geometry geometry)
    {
        if (geometry.IsEmpty || geometry.SRID != 4326) Fail("shape", "圖形不可為空，且 SRID 必須是 4326。");
        var error = new IsValidOp(geometry).ValidationError;
        if (error is not null)
        {
            var reason = error.ErrorType switch
            {
                TopologyValidationErrors.SelfIntersection or TopologyValidationErrors.RingSelfIntersection => "圖形有自交或環互相交叉。",
                TopologyValidationErrors.HoleOutsideShell => "Polygon 的洞必須位於外環內。",
                TopologyValidationErrors.NestedHoles => "Polygon 的洞不可互相包含。",
                TopologyValidationErrors.NestedShells => "MultiPolygon 的外環不可互相包含。",
                TopologyValidationErrors.DisconnectedInteriors => "Polygon 的內部不可被洞分割。",
                TopologyValidationErrors.TooFewPoints => "圖形的有效座標點不足。",
                _ => "圖形拓樸無效。"
            };
            Fail("shape", reason);
        }
        return Orient(geometry);
    }

    private static Geometry Orient(Geometry geometry)
    {
        var factory = geometry.Factory;
        return geometry switch
        {
            Polygon polygon => factory.CreatePolygon(OrientRing(polygon.ExteriorRing, true),
                Enumerable.Range(0, polygon.NumInteriorRings).Select(i => OrientRing(polygon.GetInteriorRingN(i), false)).ToArray()),
            MultiPolygon polygons => factory.CreateMultiPolygon(
                Enumerable.Range(0, polygons.NumGeometries).Select(i => (Polygon)Orient(polygons.GetGeometryN(i))).ToArray()),
            // Preserve homogeneous collection types; only heterogeneous collections need recursion here.
            GeometryCollection collection when collection.OgcGeometryType == OgcGeometryType.GeometryCollection => factory.CreateGeometryCollection(
                Enumerable.Range(0, collection.NumGeometries).Select(i => Orient(collection.GetGeometryN(i))).ToArray()),
            _ => geometry
        };
    }

    private static LinearRing OrientRing(LineString ring, bool counterClockwise) =>
        (LinearRing)(Orientation.IsCCW(ring.CoordinateSequence) == counterClockwise ? ring : ring.Reverse());

    private static void Fail(string path, string message) => throw new JsonException($"{path}：{message}");
}
