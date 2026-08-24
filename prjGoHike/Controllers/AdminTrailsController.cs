
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using prjGoHike.Models;
using prjGoHike.ViewModels;

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
    public async Task<IActionResult> Details(int? id)
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

        return View(trail);
    }

    // GET: AdminTrails/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: AdminTrails/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("TrailId,TrailName,Region,DifficultyLevel,DistanceKm,PermitRequired,GuideRequired,RegulationNote,TrailPath,IsPublished")] Trail trail)
    {
        if (ModelState.IsValid)
        {
            _context.Add(trail);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View();
    }

    // GET: AdminTrails/Edit/5
    public async Task<IActionResult> Edit(long? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var traildb = await _context.Trails.FindAsync(id);
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

    // POST: AdminTrails/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? id, [Bind("TrailId,TrailName,Region,DifficultyLevel,DistanceKm,EstimatedHours,PermitRequired,GuideRequired,RegulationNote,TrailPath,IsPublished,AlertsTrails,HikeRecordDetails,TrailFeatures,TrailRiskIndicators,TrailSubscriptions,TripReports")] Trail trail)
    {
        if (id != trail.TrailId || !ModelState.IsValid)
        {
            return NotFound();
        }
        try
        {
            _context.Update(trail);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!TrailExists(trail.TrailId))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
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
        var trail = await _context.Trails.FindAsync(id);
        if (trail != null)
        {
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
