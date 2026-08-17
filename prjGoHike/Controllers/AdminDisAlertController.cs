
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using prjGoHike.ViewModels;
using prjGoHike.Models;

public class AdminDisAlertController : Controller
{
    private readonly GoHikeDataContext _context;

    public AdminDisAlertController(GoHikeDataContext context)
    {
        _context = context;
    }

    // GET: CDISASTERALERTWRAPS
    public async Task<IActionResult> Index()    
    {
        var disasterAlert = await _context.DisasterAlerts.ToListAsync();
        var dwList = disasterAlert.Select(d => new CDisasterAlertWrap(d)).ToList();
        return View(dwList);
    }

    // GET: CDISASTERALERTWRAPS/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: CDISASTERALERTWRAPS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("disasterAlert,AlertId,AlertType,AlertTitle,AlertDescription,SeverityLevel,EffectiveFrom,EffectiveTo,SourceAgency,SourceUrl,IsActive")] DisasterAlert disasterAlert)
    {
        if (ModelState.IsValid)
        {
            _context.Add(disasterAlert);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View();
    }

    // GET: CDISASTERALERTWRAPS/Edit/5
    public async Task<IActionResult> Edit(long? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var disalertdb = await _context.DisasterAlerts.FirstOrDefaultAsync(x => x.AlertId == id);
        if (disalertdb == null)
        {
            return NotFound();
        }
        CDisasterAlertWrap dw = new CDisasterAlertWrap()
        {
            disasterAlert = disalertdb
        };
        return View(dw);
    }

    // POST: CDISASTERALERTWRAPS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(long? id, [Bind("disasterAlert,AlertId,AlertType,AlertTitle,AlertDescription,SeverityLevel,EffectiveFrom,EffectiveTo,SourceAgency,SourceUrl,IsActive")] DisasterAlert disasterAlert)
    {
        if (id != disasterAlert.AlertId || !ModelState.IsValid)
        {
            return NotFound();
        }

        try
        {
            _context.Update(disasterAlert);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!CDisasterAlertWrapExists(disasterAlert.AlertId))
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

    // GET: CDISASTERALERTWRAPS/Delete/5
    public async Task<IActionResult> Delete(long? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var disalertdb = await _context.DisasterAlerts
            .FirstOrDefaultAsync(m => m.AlertId == id);
        if (disalertdb == null)
        {
            return NotFound();
        }
        CDisasterAlertWrap dw = new CDisasterAlertWrap()
        { 
            disasterAlert = disalertdb
        };
        return View(dw);
    }

    // POST: CDISASTERALERTWRAPS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(long? id)
    {
        var disalertdb = await _context.DisasterAlerts.FirstOrDefaultAsync(x => x.AlertId == id);
        if (disalertdb != null)
        {
            _context.DisasterAlerts.Remove(disalertdb);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool CDisasterAlertWrapExists(long? alertid)
    {
        return _context.DisasterAlerts.Any(e => e.AlertId == alertid);
    }
}
