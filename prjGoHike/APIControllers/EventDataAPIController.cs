using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using prjGoHike.APIControllers;
using prjGoHike.DTO.GroupJoinDTO;
using prjGoHike.Hubs;
using prjGoHike.Models;
using System.Security.Claims;

[Route("api/[controller]")]
[ApiController]
public class EventDataAPIController : BaseController
{
    private readonly GoHikeDataContext _db;
    private readonly IWebHostEnvironment _environment;
    private readonly IHubContext<EventHub> _hubContext;
    public EventDataAPIController(GoHikeDataContext db, IWebHostEnvironment environment
        ,IHubContext<EventHub> hubContext)
    {
        _db = db;
        _environment = environment;
        _hubContext = hubContext;
    }
    


    // GET: api/CEventDataWarp
    //活動欄位的部分
    [HttpGet]
    public async Task<IActionResult> GetCEventDataWarp()
    {
        var eventCount = _db.EventData.Select(e => e.EventId).Count();
        
        if (_db.EventData != null)
        {
            var eventdata = await _db.EventData.Select(e => new EventDataResponseDTO
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
                EveryMountainCount = e.Mountain.EventData.Count(),
                MountainName = e.Mountain.MountainName,
                Longitude = e.Mountain.Longitude,
                Latitude = e.Mountain.Latitude,
                CurrentParticipants = e.EventRegistrationAndMemberLists.Count(e=>e.RegistrationStatus == 1),
                LeaderUserId = e.LeaderUserId
                //先做假資料測試
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
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> PostCEventDataWarp([FromForm] EventDataDTO eventdata)
    {
        string SavedFilePath = "";
        string UploadsFolder = "";
        string BaseUrl = $"{Request.Scheme}://{Request.Host}";
        //得到請求端使用的協定/得到請求端的路由
        string checkExtension = Path.GetExtension(eventdata.ActivityPhoto.FileName);
        string[] allowtExtension = { ".jpg", ".png", ".GIF", ".jpeg" };
        var userValidClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!ModelState.IsValid)
        {
            return ErrorResponse("欄位驗證失敗", null, 400);
        }
        if (userValidClaim == null || !long.TryParse(userValidClaim, out long currentUserID))
        {
            return Unauthorized();
        }
        if(eventdata.MaximumNumber == 0)
        {
            return ErrorResponse("請選擇可參與人數", null, 400);
        }

        if(eventdata.ActivityPhoto.Length > 0)
        {
            if(eventdata.ActivityPhoto.Length > 10 * 1024* 1024)
            {
                return ErrorResponse("請上傳檔案大小10MB以內的圖片");
            }
            if (!allowtExtension.Contains(checkExtension))
            {
                return ErrorResponse("請上傳副檔名為：jpg、png、GIF、jpeg的圖片檔案");
            }
        }
        if(eventdata.EventStartTime < DateTime.Now)
        {
            return ErrorResponse("請選擇大於當前日期的時間", null, 400);
        }
        if(eventdata.EventEndTime > eventdata.EventStartTime && eventdata.EventEndTime > DateTime.Now)
        {
            return ErrorResponse("無法選擇小於當前日期的時間", null, 400);
        }



        if (eventdata.ActivityPhoto != null)
        {
            UploadsFolder = Path.Combine(_environment.WebRootPath, "assets", "JoinGroup_Images");
            //把上傳路徑存到一個變數裡
            string FileExtension = Path.GetExtension(eventdata.ActivityPhoto.FileName);
            //把傳進來的圖片副檔名存到一個變數
            string UniqueFileName = $"{Guid.NewGuid()}{FileExtension}";
            //使用guid方法創建一個全新的亂數名字加上副檔名
            string FilePath = Path.Combine(UploadsFolder, UniqueFileName);
            //跟轉成亂數的圖片與副檔名進行路徑名稱合併
            using (var stream = new FileStream(FilePath, FileMode.Create))
            {
                await eventdata.ActivityPhoto.CopyToAsync(stream);
            }

            SavedFilePath = $"{BaseUrl}/assets/JoinGroup_Images/{UniqueFileName}";
        }
        
        EventData Event =  new EventData
        {

                MountainId = eventdata.MountainId,
                EventName = eventdata.EventName,
                MaximumNumber = eventdata.MaximumNumber,
                ActivityStatus = eventdata.ActivityStatus,
                ActivityPhoto = SavedFilePath,
                Description = eventdata.Description,
                EventStartTime = eventdata.EventStartTime,
                EventEndTime = eventdata.EventEndTime,
                EventDate = DateTime.Now,
                ReviewRequired = true,
                ReviewStatus = "",
                HasActiveReport = false,
                LeaderUserId = currentUserID
                //其實在自動生成的eventdata Class裡面 已經有關連到mountain這張表 所以不用擔心的是 沒有加就代表沒資料
                //而是會透過mountainID去找到對應的山的資料
                //MountainName = "",
                //DifficultyLevel = 0,
                //MountainsPermitRequired = false,
                //NationalParkPermitRequired = false
                
            };
        int CurrentPeople = 0;
        //現在的活動報名人數 用來充數用 為了signalR
        _db.EventData.Add(Event);

        await _db.SaveChangesAsync();

        await _hubContext.Clients.All.SendAsync("EventDataChanged", Event.MountainId, CurrentPeople);
        //對著所有request的那方進行廣播 然後傳送一個自訂的事件名,加上剛剛才熱騰騰從前端傳來的
        //使用者所選擇的山的id 傳過去的其實就是當改變時會有通知的欄位

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
