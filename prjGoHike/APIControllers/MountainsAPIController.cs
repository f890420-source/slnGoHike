using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using prjGoHike.APIControllers;
using prjGoHike.DTO.GroupJoinDTO;
using prjGoHike.Models;
using prjGoHike.Models.Dtos;

[Route("api/[controller]")]
[ApiController]
public class MountainsAPIController : BaseController
{
    private readonly GoHikeDataContext _db;
    public MountainsAPIController(GoHikeDataContext db)
    {
        _db = db;
    }

    // GET: api/Mountain
    //拿到山表的欄位
    [HttpGet]
    public async Task<IActionResult> GetMountain()
    {
        if(_db.Mountains != null)
        {
            var mountain = await _db.Mountains.Select(m => new MountainDTO
            {
                MountainId = m.MountainId,
                MountainName = m.MountainName,
                Location = m.Location,
                Altitude = m.Altitude,
                DifficultyLevel = m.DifficultyLevel,
                MountainsPermitRequired = m.MountainsPermitRequired,
                NationalParkPermitRequired = m.NationalParkPermitRequired,
                Longitude = m.Longitude,
                Latitude = m.Latitude
            }).ToListAsync();
            return SuccessResponse(mountain);
        }
        else
        {
            return NotFoundResponse("");
        }
        

        
    }
}

//    // GET: api/Mountain/5
    
//    [HttpGet("{mountainid}")]
//    public async Task<ActionResult<Mountain>> GetMountain(long eventdataid)
//    {
//        var mountain = await _context.Mountains.FindAsync(mountainid);

//        if (mountain == null)
//        {
//            return NotFound();
//        }

//        return mountain;
//    }

//    // PUT: api/Mountain/5
//    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
//    [HttpPut("{mountainid}")]
//    public async Task<IActionResult> PutMountain(long? mountainid, Mountain mountain)
//    {
//        if (mountainid != mountain.MountainId)
//        {
//            return BadRequest();
//        }

//        _context.Entry(mountain).State = EntityState.Modified;

//        try
//        {
//            await _context.SaveChangesAsync();
//        }
//        catch (DbUpdateConcurrencyException)
//        {
//            if (!MountainExists(mountainid))
//            {
//                return NotFound();
//            }
//            else
//            {
//                throw;
//            }
//        }

//        return NoContent();
//    }

//    // POST: api/Mountain
//    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
//    [HttpPost]
//    public async Task<ActionResult<Mountain>> PostMountain(Mountain mountain)
//    {
//        _context.Mountains.Add(mountain);
//        await _context.SaveChangesAsync();

//        return CreatedAtAction("GetMountain", new { mountainid = mountain.MountainId }, mountain);
//    }

//    // DELETE: api/Mountain/5
//    [HttpDelete("{mountainid}")]
//    public async Task<IActionResult> DeleteMountain(long? mountainid)
//    {
//        var mountain = await _context.Mountains.FindAsync(mountainid);
//        if (mountain == null)
//        {
//            return NotFound();
//        }

//        _context.Mountains.Remove(mountain);
//        await _context.SaveChangesAsync();

//        return NoContent();
//    }

//    private bool MountainExists(long? mountainid)
//    {
//        return _context.Mountains.Any(e => e.MountainId == mountainid);
//    }
//}
