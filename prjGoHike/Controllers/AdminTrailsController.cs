
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using NetTopologySuite.Triangulate;
using prjGoHike.Models;
using prjGoHike.ViewModels;
using System.Text.Json;

public class AdminTrailsController : Controller
{
    private readonly GoHikeDataContext _context;

    public AdminTrailsController(GoHikeDataContext context)
    {
        _context = context;
    }

    // GET: AdminTrails
    public async Task<IActionResult> Index()
    {
        var trails = await _context.Trails.ToListAsync();
        var twList = trails.Select(t => new CTrailWrap(t)).ToList();
        return View(twList);
    }

    // GET: AdminTrails/Details/5
    public async Task<IActionResult> Map(long? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var trail = await _context.Trails
            .FirstOrDefaultAsync(m => m.TrailId == id);
        if (trail == null)
        {
            return NotFound();
        }

        var segments = await _context.TrailSegments
                            .Where(s => s.TrailId == id)
                            .ToListAsync();

        var viewModel = new TrailMapViewModel
        {
            TrailId = trail.TrailId,
            TrailName = trail.TrailName,
            Segments = segments.Where(s => s.RoutePath != null)
                               .Select(s => new TrailSegmentMapViewModel
                               {
                                   TrailSegmentId = s.TrailSegmentId,
                                   Source = s.Source,
                                   Coordinates = s.RoutePath.Coordinates
                                   .Select(c => new[] { c.X, c.Y })
                                   .ToArray()
                               })
                               .ToList()
        };

        return View(viewModel);
    }

    // GET: AdminTrails/Create
    public IActionResult Create()
    {
        return View(new TrailCreateViewModel());
    }

    // POST: AdminTrails/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TrailCreateViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        LineString routePath;

        // 1. 驗證檔案大小、副檔名及 JSON 格式
        // 2. 驗證 FeatureCollection 只有一個 Feature
        // 3. 驗證 geometry 是 LineString 或 MultiLineString
        // 4. 轉成 NetTopologySuite Geometry，並設定 SRID = 4326
        try
        {
            routePath = await ReadTrailGeometryAsync(vm.GeoJsonFile!);
        }
        catch (InvalidDataException dex)
        {
            ModelState.AddModelError(
                nameof(vm.GeoJsonFile),
                dex.Message
            );
            return View(vm);
        }


        if (routePath == null)
            return View(vm);

        var trail = new Trail()
        {
            TrailName = vm.TrailName.Trim(),
            Region = vm.Region.Trim(),
            DifficultyLevel = vm.DifficultyLevel,
            DistanceKm = vm.DistanceKm,
            PermitRequired = vm.PermitRequired,
            GuideRequired = vm.GuideRequired,
            RegulationNote = vm.RegulationNote,
            IsPublished = vm.IsPublished
        };

        trail.TrailSegments.Add(new TrailSegment()
        {
            RoutePath = routePath,
            Source = "Create Upload (GeoJSON)"
        });

        _context.Add(trail);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // GET: AdminTrails/Edit/5
    public async Task<IActionResult> Edit(long? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var traildb = await _context.Trails
            .Include(t => t.TrailSegments)
            .FirstOrDefaultAsync(t => t.TrailId == id);
        if (traildb == null)
        {
            return NotFound();
        }
        var segment = traildb.TrailSegments.FirstOrDefault();
        var vm = new TrailEditViewModel
        {
            TrailId = traildb.TrailId,
            TrailName = traildb.TrailName,
            Region = traildb.Region,
            DifficultyLevel = traildb.DifficultyLevel,
            DistanceKm = traildb.DistanceKm,
            PermitRequired = traildb.PermitRequired,
            GuideRequired = traildb.GuideRequired,
            RegulationNote = traildb.RegulationNote,
            IsPublished = traildb.IsPublished,

            CurrentRouteCoordinates = segment?.RoutePath.Coordinates
        .Select(c => new[] { c.X, c.Y })
        .ToArray()
        ?? Array.Empty<double[]>()
        };
        return View(vm);
    }

    // POST: AdminTrails/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? id, TrailEditViewModel vm)
    {
        if (id != vm.TrailId || !ModelState.IsValid)
        {
            return NotFound();
        }

        // 因為後臺匯入的步道會有多段資料
        var trail = await _context.Trails
                      .Include(t => t.TrailSegments)
                      .SingleOrDefaultAsync(t => t.TrailId == id);

        if (trail == null)
        {
            return NotFound();
        }

        LineString? newRoutePath = null;

        // 有選擇新檔案時才驗證、覆蓋路線。
        if (vm.GeoJsonFile != null)
        {
            try
            {
                newRoutePath =
                    await ReadTrailGeometryAsync(vm.GeoJsonFile);
            }
            catch (InvalidDataException ex)
            {
                ModelState.AddModelError(
                    nameof(vm.GeoJsonFile),
                    ex.Message
                );
            }
        }

        if (!ModelState.IsValid)
        {
            // 重新補上既有路線，讓畫面仍可顯示地圖
            vm.CurrentRouteCoordinates = GetRouteCoordinates(trail);
            return View(vm);
        }

        // 更新 Trail 的一般欄位。
        trail.TrailName = vm.TrailName.Trim();
        trail.Region = vm.Region.Trim();
        trail.DifficultyLevel = vm.DifficultyLevel;
        trail.DistanceKm = vm.DistanceKm;
        trail.PermitRequired = vm.PermitRequired;
        trail.GuideRequired = vm.GuideRequired;
        trail.RegulationNote = vm.RegulationNote;
        trail.IsPublished = vm.IsPublished;

        // 有上傳新檔案才覆蓋 RoutePath。
        if (newRoutePath != null)
        {
            TrailSegment? segment =
                trail.TrailSegments.SingleOrDefault();

            if (segment == null)
            {
                trail.TrailSegments.Add(
                    new TrailSegment
                    {
                        RoutePath = newRoutePath,
                        Source = "Edit Upload (GeoJSON)"
                    }
                );
            }
            else
            {
                segment.RoutePath = newRoutePath;
                segment.Source = "Edit Upload (GeoJSON)";
            }
        }

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!TrailExists(vm.TrailId))
            {
                return NotFound();
            }

            throw;
        }

        return RedirectToAction(nameof(Index));
    }

    // GET: AdminTrails/Delete/5
    public async Task<IActionResult> Delete(long? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var traildb = await _context.Trails
            .FirstOrDefaultAsync(m => m.TrailId == id);
        if (traildb == null)
        {
            return NotFound();
        }
        CTrailWrap tw = new CTrailWrap()
        {
            trail = traildb
        };
        return View(tw);
    }

    // POST: AdminTrails/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(long? id)
    {
        var trail = await _context.Trails
            .Include(t => t.TrailSegments)
            .SingleOrDefaultAsync(t => t.TrailId == id);

        if (trail != null)
        {
            _context.TrailSegments.RemoveRange(trail.TrailSegments);
            _context.Trails.Remove(trail);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private static async Task<LineString> ReadTrailGeometryAsync(
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

            string geometryType =
                GetRequiredString(geometryElement, "type");

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

    private static string GetRequiredString(
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

    private static LineString CreateLineString(
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

    private static Coordinate ReadCoordinate(
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

    private static double[][] GetRouteCoordinates(
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

    private bool TrailExists(long? trailid)
    {
        return _context.Trails.Any(e => e.TrailId == trailid);
    }
}
