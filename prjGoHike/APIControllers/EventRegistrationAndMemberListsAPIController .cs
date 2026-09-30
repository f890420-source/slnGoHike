using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using prjGoHike.DTO.GroupJoinDTO;
using prjGoHike.Hubs;
using prjGoHike.Models;
using System.Data;
using System.Security.Claims;

namespace prjGoHike.APIControllers
{
    
    [Route("api/[controller]")]
    [ApiController]
    public class EventRegistrationAndMemberListAPIController : BaseController
    {
        private readonly GoHikeDataContext _db;
        private readonly IHubContext<EventHub> _hubContext;

        public EventRegistrationAndMemberListAPIController(GoHikeDataContext db,IHubContext<EventHub>hubContext)
        {
            _db = db;
            _hubContext = hubContext;
        }

        // GET: api/EventRegistrationAndMemberListsAPI
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetAllregistration()
        {

            if (_db.EventRegistrationAndMemberLists == null)
            {
                return NotFoundResponse("查無資料表");
            }

            

            var eventRegistration = _db.EventRegistrationAndMemberLists.Select(r => new EventRegistrationAndMemberListResponseDTO
            {
                SignUpId = r.SignUpId,
                UserId = r.UserId,
                EventId = r.EventId,
                RegistrationStatus = r.RegistrationStatus,
                EmergencyContact = r.EmergencyContact,
                AvatarUrl = r.User.AvatarUrl,
                AvatarBlurState = r.User.AvatarBlurState,
                CreatedAt = DateTime.Now
            }).ToListAsync();

            var data = await eventRegistration;
            return SuccessResponse(data);
        }

        
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Postregistration(EventRegistrationAndMemberListDTO registrationData)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null || !long.TryParse(userIdClaim, out long userId))
            {
                return Unauthorized();
            }

            try
            {
                
                await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

                var ev = await _db.EventData.FirstOrDefaultAsync(e => e.EventId == registrationData.EventId);
                if (ev == null)
                {
                    return ErrorResponse("查無此活動", null, 404);
                }

                if (ev.LeaderUserId == userId)
                {
                    return ErrorResponse("發起人無法報名自己的活動", null, 400);
                }

                if (ev.EventEndTime < DateTime.Now)
                {
                    return ErrorResponse("活動已結束，無法報名", null, 400);
                }

                var repeat = await _db.EventRegistrationAndMemberLists
                    .AnyAsync(e => e.UserId == userId && e.EventId == ev.EventId && e.RegistrationStatus == 1);
                if (repeat)
                {
                    return ErrorResponse("無法重複報名", null, 400);
                }

                var currentCount = await _db.EventRegistrationAndMemberLists
                    .CountAsync(r => r.EventId == ev.EventId && r.RegistrationStatus == 1);
                if (currentCount >= ev.MaximumNumber)
                {
                    return ErrorResponse("報名人數已超過上限", null, 400);
                }

                var entity = new EventRegistrationAndMemberList
                {
                    UserId = userId,
                    EventId = ev.EventId,
                    RegistrationStatus = 1,
                    EmergencyContact = "",
                    CreatedAt = DateTime.Now
                };
                _db.EventRegistrationAndMemberLists.Add(entity);

                currentCount++;
                if (currentCount >= ev.MaximumNumber)
                {
                    ev.ActivityStatus = "已額滿";
                }

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                await _hubContext.Clients.All.SendAsync("EventDataChanged", ev.MountainId, currentCount);
                return SuccessResponse(new { entity.SignUpId, entity.EventId, entity.UserId });
            }
            catch (Exception ex) when (ex is DbUpdateException || ex is SqlException)
            {
                
                return ErrorResponse("報名人數眾多，請稍後再試一次", null, 409);
            }
        }

       
    }
}

