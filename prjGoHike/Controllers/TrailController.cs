
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using prjGoHike.Models;

public class TrailController : Controller
{
    private readonly GoHikeDataContext _context;

    public TrailController(GoHikeDataContext context)
    {
        _context = context;
    }

    // GET: TRAILS
    public async Task<IActionResult> Index()    
    {
        return View(await _context.Trails.ToListAsync());
    }

    // GET: TRAILS/Details/5
    public async Task<IActionResult> Details(long? trailid)
    {
        if (trailid == null)
        {
            return NotFound();
        }

        var trail = await _context.Trails
            .FirstOrDefaultAsync(m => m.TrailId == trailid);
        if (trail == null)
        {
            return NotFound();
        }

        return View(trail);
    }

    // GET: TRAILS/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: TRAILS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("TrailId,TrailName,Region,DifficultyLevel,DistanceKm,EstimatedHours,PermitRequired,GuideRequired,RegulationNote,TrailPath,IsPublished,AlertsTrails,HikeRecordDetails,TrailFeatures,TrailRiskIndicators,TrailSubscriptions,TripReports")] Trail trail)
    {
        if (ModelState.IsValid)
        {
            _context.Add(trail);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(trail);
    }

    // GET: TRAILS/Edit/5
    public async Task<IActionResult> Edit(long? trailid)
    {
        if (trailid == null)
        {
            return NotFound();
        }

        var trail = await _context.Trails.FindAsync(trailid);
        if (trail == null)
        {
            return NotFound();
        }
        return View(trail);
    }

    // POST: TRAILS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(long? trailid, [Bind("TrailId,TrailName,Region,DifficultyLevel,DistanceKm,EstimatedHours,PermitRequired,GuideRequired,RegulationNote,TrailPath,IsPublished,AlertsTrails,HikeRecordDetails,TrailFeatures,TrailRiskIndicators,TrailSubscriptions,TripReports")] Trail trail)
    {
        if (trailid != trail.TrailId)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
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
        return View(trail);
    }

    // GET: TRAILS/Delete/5
    public async Task<IActionResult> Delete(long? trailid)
    {
        if (trailid == null)
        {
            return NotFound();
        }

        var trail = await _context.Trails
            .FirstOrDefaultAsync(m => m.TrailId == trailid);
        if (trail == null)
        {
            return NotFound();
        }

        return View(trail);
    }

    // POST: TRAILS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(long? trailid)
    {
        var trail = await _context.Trails.FindAsync(trailid);
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
