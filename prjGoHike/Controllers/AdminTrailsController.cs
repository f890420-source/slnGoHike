
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using NetTopologySuite.Triangulate;
using prjGoHike.Models;
using prjGoHike.Services;
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
    public async Task<IActionResult> Index(CKeywordViewModel vm)
    {
        List<Trail>? trails = null;
        if (string.IsNullOrWhiteSpace(vm.txtKeyword))
        {
            trails = await _context.Trails.AsNoTracking().ToListAsync();
        }
        else
        {
            trails = await _context.Trails
                            .AsNoTracking()
                            .Where(t => t.TrailName.Contains(vm.txtKeyword)
                                     || t.Region.Contains(vm.txtKeyword))
                            .ToListAsync();
        }

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
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.TrailId == id);
        if (trail == null)
        {
            return NotFound();
        }

        var segments = await _context.TrailSegments
                            .AsNoTracking()
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
            routePath = await TrailGeometryService.ReadTrailGeometryAsync(vm.GeoJsonFile!);
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
            .AsNoTracking()
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
                    await TrailGeometryService.ReadTrailGeometryAsync(vm.GeoJsonFile);
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
            vm.CurrentRouteCoordinates = TrailGeometryService.GetRouteCoordinates(trail);
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
            .AsNoTracking()
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

    private bool TrailExists(long? trailid)
    {
        return _context.Trails.Any(e => e.TrailId == trailid);
    }
}
