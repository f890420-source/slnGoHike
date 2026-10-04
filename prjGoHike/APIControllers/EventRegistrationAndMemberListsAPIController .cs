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
using prjGoHike.Services;
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
                if (Condition_observe_service.HasEnded(ev))
                {
                    return ErrorResponse("活動已結束，無法報名", null, 400);
                }
                if (Condition_observe_service.HasStarted(ev))
                {
                    return ErrorResponse("活動已開始，無法報名", null, 400);
                }

            
                var existing = await _db.EventRegistrationAndMemberLists
                    .FirstOrDefaultAsync(r => r.UserId == userId && r.EventId == ev.EventId);

                if (existing != null && existing.RegistrationStatus == 1)
                {
                    return ErrorResponse("無法重複報名", null, 400);
                }

                
                if (existing?.CancelledAt != null)
                {
                    var waited = DateTime.Now - existing.CancelledAt.Value;
                    if (waited < Condition_observe_service.RejoinCooldown)
                    {
                        int left = (int)Math.Ceiling((Condition_observe_service.RejoinCooldown - waited).TotalMinutes);
                        //取出時間做加減
                        return ErrorResponse($"退出後需等待 30 分鐘才能重新報名，還剩 {left} 分鐘", null, 400);
                    }
                }

                int activeCount = await Condition_observe_service.CountActiveParticipationAsync(_db, userId);
                if (activeCount >= Condition_observe_service.MaxActiveEvents)
                {
                    return ErrorResponse($"同時參與的活動已達 {Condition_observe_service.MaxActiveEvents} 個上限，需等其中一個活動結束才能再報名", null, 400);
                }

                var currentCount = await _db.EventRegistrationAndMemberLists
                    .CountAsync(r => r.EventId == ev.EventId && r.RegistrationStatus == 1);
                if (currentCount >= ev.MaximumNumber)
                {
                    return ErrorResponse("報名人數已超過上限", null, 400);
                }

                if (existing != null)
                {
                    existing.RegistrationStatus = 1;
                    existing.CancelledAt = null;
                    existing.CreatedAt = DateTime.Now;
                }
                else
                {
                    _db.EventRegistrationAndMemberLists.Add(new EventRegistrationAndMemberList
                    {
                        UserId = userId,
                        EventId = ev.EventId,
                        RegistrationStatus = 1,
                        EmergencyContact = "",
                        CreatedAt = DateTime.Now
                    });
                }

                currentCount++;
                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                await _hubContext.Clients.All.SendAsync("EventDataChanged", ev.MountainId, currentCount);
                return SuccessResponse(new { ev.EventId });
            }
            catch (Exception ex) when (ex is DbUpdateException || ex is SqlException)
            {
                return ErrorResponse("報名人數眾多，請稍後再試一次", null, 409);
            }
        }
        
        [Authorize]
        [HttpPost("withdraw/{eventId}")]
        public async Task<IActionResult> Withdraw(long eventId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null || !long.TryParse(userIdClaim, out long userId))
            {
                return Unauthorized();
            }

            var ev = await _db.EventData.FirstOrDefaultAsync(e => e.EventId == eventId);
            if (ev == null)
            {
                return ErrorResponse("查無此活動", null, 404);
            }
            if (ev.LeaderUserId == userId)
            {
                return ErrorResponse("發起人無法退出自己的活動，如不舉辦請刪除活動", null, 400);
            }
            if (Condition_observe_service.HasEnded(ev) || Condition_observe_service.HasStarted(ev))
            {
                return ErrorResponse("活動已開始，無法退出", null, 400);
            }

            var registration = await _db.EventRegistrationAndMemberLists
                .FirstOrDefaultAsync(r => r.UserId == userId && r.EventId == eventId && r.RegistrationStatus == 1);
            if (registration == null)
            {
                return ErrorResponse("你沒有報名這個活動", null, 400);
            }

            
            registration.RegistrationStatus = 0;
            registration.CancelledAt = DateTime.Now;
            await _db.SaveChangesAsync();

            int currentCount = await _db.EventRegistrationAndMemberLists
                .CountAsync(r => r.EventId == eventId && r.RegistrationStatus == 1);
            await _hubContext.Clients.All.SendAsync("EventDataChanged", ev.MountainId, currentCount);

            return SuccessResponse("已退出活動");
        }


    }
}

