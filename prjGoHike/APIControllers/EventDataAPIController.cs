using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using prjGoHike.APIControllers;
using prjGoHike.DTO.GroupJoinDTO;
using prjGoHike.Models;

[Route("api/[controller]")]
[ApiController]
public class EventDataAPIController : BaseController
{
    private readonly GoHikeDataContext _db;
    public EventDataAPIController(GoHikeDataContext db)
    {
        _db = db;
    }

    // GET: api/CEventDataWarp
    //活動欄位的部分
    [HttpGet]
    public async Task<IActionResult> GetCEventDataWarp()
    {
        var eventCount = _db.EventData.Select(e => e.EventId).Count();
        if (_db.EventData != null)
        {
            var eventdata = await _db.EventData.Select(e => new EventDataDTO
            {
                EventId = e.EventId,
                MountainId = e.MountainId,
                EventName = e.EventName,
                MaximumNumber = e.MaximumNumber,
                ActivityStatus = e.ActivityStatus,
                ActivityPhoto = e.ActivityPhoto,
                Description = e.Description,
                MountainsPermitRequired = e.Mountain.MountainsPermitRequired,
                NationalParkPermitRequired = e.Mountain.NationalParkPermitRequired,
                EventStartTime = e.EventStartTime,
                EventEndTime = e.EventEndTime,
                EventCount = eventCount,
                MountainName = e.Mountain.MountainName,
                Longitude = e.Mountain.Longitude,
                Latitude = e.Mountain.Latitude
            }).ToListAsync();

            return SuccessResponse(eventdata);
        }
        else
        {
            return NotFoundResponse("");
        }
        
        
    }

    // GET: api/CEventDataWarp/5
    //[HttpGet("{eventid}")]
    //public async Task<ActionResult<CEventDataWarp>> GetCEventDataWarp(long eventid)
    //{
    //    var ceventdatawarp = await _db.CEventDataWarp.FindAsync(eventid);

    //    if (ceventdatawarp == null)
    //    {
    //        return NotFound();
    //    }

    //    return ceventdatawarp;
    //}

    // PUT: api/CEventDataWarp/5
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    //[HttpPut("{eventid}")]
    //public async Task<IActionResult> PutCEventDataWarp(long? eventid, CEventDataWarp ceventdatawarp)
    //{
    //    if (eventid != ceventdatawarp.EventId)
    //    {
    //        return BadRequest();
    //    }

    //    _db.Entry(ceventdatawarp).State = EntityState.Modified;

    //    try
    //    {
    //        await _db.SaveChangesAsync();
    //    }
    //    catch (DbUpdateConcurrencyException)
    //    {
    //        if (!CEventDataWarpExists(eventid))
    //        {
    //            return NotFound();
    //        }
    //        else
    //        {
    //            throw;
    //        }
    //    }

    //    return NoContent();
    //}

    //// POST: api/CEventDataWarp
    //// To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPost]
    public async Task<IActionResult> PostCEventDataWarp(EventDataDTO eventdata)
    {
            CEventDataWarp Event =  new CEventDataWarp
            {
                EventId = eventdata.EventId,
                MountainId = eventdata.MountainId,
                EventName = eventdata.EventName,
                MaximumNumber = eventdata.MaximumNumber,
                ActivityStatus = eventdata.ActivityStatus,
                ActivityPhoto = eventdata.ActivityPhoto,
                Description = eventdata.Description,
                EventStartTime = eventdata.EventStartTime,
                EventEndTime = eventdata.EventEndTime
            };

        _db.CEventDataWarp.Add(Event);
        await _db.SaveChangesAsync();

        return SuccessResponse(Event);
    }

    // DELETE: api/CEventDataWarp/5
    //[HttpDelete("{eventid}")]
    //public async Task<IActionResult> DeleteCEventDataWarp(long? eventid)
    //{
    //    var ceventdatawarp = await _db.CEventDataWarp.FindAsync(eventid);
    //    if (ceventdatawarp == null)
    //    {
    //        return NotFound();
    //    }

    //    _db.CEventDataWarp.Remove(ceventdatawarp);
    //    await _db.SaveChangesAsync();

    //    return NoContent();
    //}

    //private bool CEventDataWarpExists(long? eventid)
    //{
    //    return _db.CEventDataWarp.Any(e => e.EventId == eventid);
    //}
}
