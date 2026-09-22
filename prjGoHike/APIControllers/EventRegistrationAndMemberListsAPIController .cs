using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using prjGoHike.DTO.GroupJoinDTO;
using prjGoHike.Hubs;
using prjGoHike.Models;
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

            

            var eventRegistration = _db.EventRegistrationAndMemberLists.Select(r => new EventRegistrationAndMemberListDTO
            {
                SignUpId = r.SignUpId,
                UserId = r.UserId,
                EventId = r.EventId,
                RegistrationStatus = r.RegistrationStatus,
                EmergencyContact = r.EmergencyContact,
                CreatedAt = DateTime.Now
            }).ToListAsync();

            var data = await eventRegistration;
            return SuccessResponse(data);
        }

        // GET: api/EventRegistrationAndMemberListsAPI/5
        //[HttpGet("{id}")]
        //public async Task<IActionResult> GetById(long id)
        //{
        //    if (_db.EventRegistrationAndMemberLists == null)
        //    {
        //        return NotFoundResponse("查無資料表");
        //    }

        //    var entity = await _db.EventRegistrationAndMemberLists
        //        .FirstOrDefaultAsync(e => e.SignUpId == id);

        //    if (entity == null)
        //    {
        //        return NotFoundResponse("查無此筆報名資料");
        //    }

        //    return SuccessResponse(entity);
        //}

        // POST: api/EventRegistrationAndMemberListsAPI
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Postregistration(EventRegistrationAndMemberListDTO registrationData)
        {

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            //找發出http請求的使用者的id
            
            if (userIdClaim == null || !long.TryParse(userIdClaim, out long userId))
            {
                return Unauthorized();
            }
            var RepeatP = await _db.EventRegistrationAndMemberLists.AnyAsync(e => e.UserId == userId && e.EventId == registrationData.EventId && e.RegistrationStatus == 1);
            if (RepeatP)
            {
                return ErrorResponse("無法重複報名", null, 400);
            }
            var limitPeople = await _db.EventData.FirstOrDefaultAsync(e => e.EventId == registrationData.EventId);

            var currentCount = await _db.EventRegistrationAndMemberLists
                .CountAsync(r => r.EventId == registrationData.EventId && r.RegistrationStatus == 1);

            int limit = limitPeople.MaximumNumber;

            if(currentCount >= limit)
            {
                return ErrorResponse("報名人數已超過上限", null, 400);
            }

            var entity = new EventRegistrationAndMemberList
            {
                UserId = userId,
                EventId = registrationData.EventId,
                RegistrationStatus = 1,
                EmergencyContact = "",
                CreatedAt = DateTime.Now
            };

            currentCount++;

            _db.EventRegistrationAndMemberLists.Add(entity);
            await _db.SaveChangesAsync();



            var mountainEvent = 0;
            //代表山的變化 為了signalR

            await _hubContext.Clients.All.SendAsync("EventDataChanged", mountainEvent, currentCount);
            return SuccessResponse(entity);
        }

        // PUT: api/EventRegistrationAndMemberListsAPI/5
        //[HttpPut("{id}")]
        //public async Task<IActionResult> Update(long id, [FromBody] EventRegistrationAndMemberList entity)
        //{
        //    if (id != entity.SignUpId)
        //    {
        //        return BadRequest("Id 不符");
        //    }

        //    var existing = await _db.EventRegistrationAndMemberLists.FindAsync(id);
        //    if (existing == null)
        //    {
        //        return NotFoundResponse("查無此筆報名資料");
        //    }

        //    existing.UserId = entity.UserId;
        //    existing.EventId = entity.EventId;
        //    existing.RegistrationStatus = entity.RegistrationStatus;
        //    existing.EmergencyContact = entity.EmergencyContact;

        //    await _db.SaveChangesAsync();

        //    return SuccessResponse(existing);
        //}

        // DELETE: api/EventRegistrationAndMemberListsAPI/5
        //[HttpDelete("{id}")]
        //public async Task<IActionResult> Delete(long id)
        //{
        //    var entity = await _db.EventRegistrationAndMemberLists.FindAsync(id);
        //    if (entity == null)
        //    {
        //        return NotFoundResponse("查無此筆報名資料");
        //    }

        //    _db.EventRegistrationAndMemberLists.Remove(entity);
        //    await _db.SaveChangesAsync();

        //    return SuccessResponse("刪除成功");
        //}
    }
}

