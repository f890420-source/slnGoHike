using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using prjGoHike.DTO.GroupJoinDTO;
using prjGoHike.Models;
using System.Security.Claims;

namespace prjGoHike.APIControllers
{
    
    [Route("api/[controller]")]
    [ApiController]
    public class EventRegistrationAndMemberListAPIController : BaseController
    {
        private readonly GoHikeDataContext _db;

        public EventRegistrationAndMemberListAPIController(GoHikeDataContext db)
        {
            _db = db;
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
        public async Task<IActionResult> Postregistration(EventRegistrationAndMemberList registrationData)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null || !long.TryParse(userIdClaim, out long userId))
            {
                return Unauthorized();
            }

            var entity = new EventRegistrationAndMemberList
            {
                UserId = userId,
                EventId = registrationData.EventId,
                RegistrationStatus = 1,
                EmergencyContact = registrationData.EmergencyContact,
                CreatedAt = DateTime.Now
            };

            EventRegistrationAndMemberList reg = new EventRegistrationAndMemberList
            {

                SignUpId = registrationData.SignUpId,
                UserId = registrationData.UserId,
                EventId = registrationData.EventId,
                RegistrationStatus = registrationData.RegistrationStatus,
                EmergencyContact = registrationData.EmergencyContact,
                CreatedAt = registrationData.CreatedAt
            };

            var data = reg;
            
            
            _db.EventRegistrationAndMemberLists.Add(data);
            await _db.SaveChangesAsync();
            return SuccessResponse(data);
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

