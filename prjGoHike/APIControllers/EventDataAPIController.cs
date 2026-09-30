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
    



    
    [HttpGet]
    public async Task<IActionResult> GetEventData()
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
                ActivityStatus = e.EventEndTime < DateTime.Now
                ? "已結束"
                : (e.EventRegistrationAndMemberLists.Count(r => r.RegistrationStatus == 1) >= e.MaximumNumber
                ? "已額滿"
                : "招募中"),
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
                CurrentParticipants = e.EventRegistrationAndMemberLists.Count(r => r.RegistrationStatus == 1),
                LeaderUserId = e.LeaderUserId,
                eventRegistrationAndMemberListResponsesdto = e.EventRegistrationAndMemberLists.Where(r => r.RegistrationStatus == 1)
                .OrderBy(r => r.SignUpId)
                .Select(r => new EventRegistrationAndMemberListResponseDTO{
                    SignUpId = r.SignUpId,
                    UserId = r.UserId,
                    EventId = r.EventId,
                    RegistrationStatus = r.RegistrationStatus,
                    EmergencyContact = r.EmergencyContact,
                    CreatedAt = r.CreatedAt,
                    AvatarBlurState = r.User.AvatarBlurState,
                    AvatarUrl = r.User.AvatarUrl,
                    Nickname = r.User.Nickname,
                    Skills = r.User.UserSkillTags
                    .Where(t => t.IsDisplayed)
                    .Select(t => t.SkillTag.TagName)
                    .ToList()
                }).ToList()
                
                
            }).ToListAsync();

            return SuccessResponse(eventdata);
        }
        else
        {
            return NotFoundResponse("");
        }
        
        
    }

    [Authorize]
    [HttpPut("{eventid}")]
    public async Task<IActionResult> PutEventData(long eventid, [FromForm] EventDataUpdateDTO eventdata)
    {
        var currentUserID = GetCurrentUserId();
        if (currentUserID == null)
        {
            return Unauthorized();
        }
        if (!ModelState.IsValid)
        {
            return ErrorResponse("欄位驗證失敗", null, 400);
        }

        var Event = await _db.EventData.FirstOrDefaultAsync(e => e.EventId == eventid);
        if (Event == null)
        {
            return ErrorResponse("查無此活動", null, 404);
        }
        if (Event.LeaderUserId != currentUserID)
        {
            return ErrorResponse("只有發起人可以修改活動", null, 403);
        }
        if (Event.EventEndTime < DateTime.Now)
        {
            return ErrorResponse("活動已結束，無法修改", null, 400);
        }

        if (string.IsNullOrWhiteSpace(eventdata.EventName))
        {
            return ErrorResponse("活動名稱無法為空", null, 400);
        }
        if (eventdata.MaximumNumber <= 0)
        {
            return ErrorResponse("請選擇可參與人數", null, 400);
        }
        
        if (eventdata.EventStartTime != Event.EventStartTime && eventdata.EventStartTime < DateTime.Now)
        {
            return ErrorResponse("請選擇大於當前日期的時間", null, 400);
        }
        if (eventdata.EventEndTime <= eventdata.EventStartTime)
        {
            return ErrorResponse("結束時間必須晚於開始時間", null, 400);
        }

        string newName = eventdata.EventName.Trim();
        
        bool repeatName = await _db.EventData.AnyAsync(e => e.EventName == newName && e.EventId != eventid);
        if (repeatName)
        {
            return ErrorResponse("無法輸入相同活動名稱", null, 400);
        }

        int currentPeople = await _db.EventRegistrationAndMemberLists
            .CountAsync(r => r.EventId == eventid && r.RegistrationStatus == 1);
        if (eventdata.MaximumNumber < currentPeople)
        {
            return ErrorResponse($"上限人數不能少於目前已報名的 {currentPeople} 人", null, 400);
        }

        
        string? oldPhoto = null;
        if (eventdata.ActivityPhoto != null && eventdata.ActivityPhoto.Length > 0)
        {
            var (url, error) = await SaveActivityPhotoAsync(eventdata.ActivityPhoto);
            if (error != null)
            {
                return ErrorResponse(error);
            }
            oldPhoto = Event.ActivityPhoto;
            Event.ActivityPhoto = url!;
        }

        Event.EventName = newName;
        Event.MaximumNumber = eventdata.MaximumNumber;
        Event.EventStartTime = eventdata.EventStartTime;
        Event.EventEndTime = eventdata.EventEndTime;
        Event.Description = eventdata.Description ?? "";

        await _db.SaveChangesAsync();

        DeleteActivityPhotoFile(oldPhoto);

        await _hubContext.Clients.All.SendAsync("EventDataChanged", Event.MountainId, currentPeople);

        return SuccessResponse(new { Event.EventId, Event.EventName, Event.ActivityPhoto });
    }

 
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> PostEventData([FromForm] EventDataDTO eventdata)
    {
        string SavedFilePath = "";
        
        
        
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

        if(eventdata.EventStartTime < DateTime.Now)
        {
            return ErrorResponse("請選擇大於當前日期的時間", null, 400);
        }
        if(eventdata.EventEndTime < eventdata.EventStartTime || eventdata.EventEndTime < DateTime.Now)
        {
            return ErrorResponse("無法選擇小於當前日期的時間", null, 400);
        }
        if(eventdata.EventName == null)
        {
            return ErrorResponse("活動名稱無法為空", null, 400);
        }
        
        var repeatName = _db.EventData.FirstOrDefault(e => e.EventName == eventdata.EventName);
        if(repeatName != null && repeatName.EventName == eventdata.EventName)
        {
            return ErrorResponse("無法輸入相同活動名稱",null, 400);
        }

        if (eventdata.ActivityPhoto != null && eventdata.ActivityPhoto.Length > 0)
        {
            var (url, error) = await SaveActivityPhotoAsync(eventdata.ActivityPhoto);
            if (error != null)
            {
                return ErrorResponse(error);
            }
            SavedFilePath = url!;
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
        
        //現在的活動報名人數 用來充數用 為了signalR
        Event.EventRegistrationAndMemberLists.Add(new EventRegistrationAndMemberList
        {
            UserId = currentUserID,
            RegistrationStatus = 1,
            EmergencyContact = "",
            CreatedAt = DateTime.Now
        });

        _db.EventData.Add(Event);

        await _db.SaveChangesAsync();
        int CurrentPeople = 1;
        await _hubContext.Clients.All.SendAsync("EventDataChanged", Event.MountainId, CurrentPeople);
        //對著所有request的那方進行廣播 然後傳送一個自訂的事件名,加上剛剛才熱騰騰從前端傳來的
        //使用者所選擇的山的id 傳過去的其實就是當改變時會有通知的欄位

        return SuccessResponse(new { Event.EventId, Event.EventName });
    }


    [Authorize]
    [HttpDelete("{eventid}")]
    public async Task<IActionResult> DeleteEventData(long eventid)
    {
        var currentUserID = GetCurrentUserId();
        if (currentUserID == null)
        {
            return Unauthorized();
        }

        var Event = await _db.EventData.FirstOrDefaultAsync(e => e.EventId == eventid);
        if (Event == null)
        {
            return ErrorResponse("查無此活動", null, 404);
        }
        if (Event.LeaderUserId != currentUserID)
        {
            return ErrorResponse("只有發起人可以刪除活動", null, 403);
        }

        var mountainId = Event.MountainId;
        string? photo = Event.ActivityPhoto;

        
        _db.GroupAttendances.RemoveRange(_db.GroupAttendances.Where(g => g.GroupId == eventid));
        _db.EventLeaderRatings.RemoveRange(_db.EventLeaderRatings.Where(r => r.EventId == eventid));
        _db.EventReportComplaints.RemoveRange(_db.EventReportComplaints.Where(r => r.EventId == eventid));
        _db.EventRegistrationAndMemberLists.RemoveRange(_db.EventRegistrationAndMemberLists.Where(r => r.EventId == eventid));

        
        var suspensions = await _db.SuspensionSchedules.Where(s => s.EventId == eventid).ToListAsync();
        foreach (var s in suspensions)
        {
            s.EventId = null;
        }

        _db.EventData.Remove(Event);

       
        await _db.SaveChangesAsync();

        
        DeleteActivityPhotoFile(photo);

        await _hubContext.Clients.All.SendAsync("EventDataChanged", mountainId, 0);

        return SuccessResponse("刪除成功");
    }
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif" };

    private long? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return long.TryParse(claim, out long id) ? id : null;
    }

   
    private async Task<(string? url, string? error)> SaveActivityPhotoAsync(IFormFile photo)
    {
        if (photo.Length > 10 * 1024 * 1024)
        {
            return (null, "請上傳檔案大小10MB以內的圖片");
        }

        string extension = Path.GetExtension(photo.FileName).ToLowerInvariant();
      
        if (!AllowedExtensions.Contains(extension))
        {
            return (null, "請上傳副檔名為：jpg、png、gif、jpeg的圖片檔案");
        }

        string uploadsFolder = Path.Combine(_environment.WebRootPath, "assets", "JoinGroup_Images");
        Directory.CreateDirectory(uploadsFolder);
       

        string uniqueFileName = $"{Guid.NewGuid()}{extension}";
        using (var stream = new FileStream(Path.Combine(uploadsFolder, uniqueFileName), FileMode.Create))
        {
            await photo.CopyToAsync(stream);
        }

        string baseUrl = $"{Request.Scheme}://{Request.Host}";
        return ($"{baseUrl}/assets/JoinGroup_Images/{uniqueFileName}", null);
    }

    
    private void DeleteActivityPhotoFile(string? photoUrl)
    {
        if (string.IsNullOrEmpty(photoUrl)) return;

        try
        {
           
            string fileName = Uri.TryCreate(photoUrl, UriKind.Absolute, out var uri)
                ? Path.GetFileName(uri.LocalPath)
                : Path.GetFileName(photoUrl);

            string filePath = Path.Combine(_environment.WebRootPath, "assets", "JoinGroup_Images", fileName);
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }
        catch
        {
            
        }
    }
}
