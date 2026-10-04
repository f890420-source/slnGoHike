using Microsoft.EntityFrameworkCore;
using prjGoHike.Models;

namespace prjGoHike.Services
{
    public class Condition_observe_service
    {
        public const string Running = "進行中";
        public const int MaxActiveEvents = 5;
        public static readonly TimeSpan RejoinCooldown = TimeSpan.FromMinutes(30);
        //製作30分鐘的固定時間
       
        public static bool HasStarted(EventData ev) =>
            ev.ActivityStatus == Running || ev.EventStartTime <= DateTime.Now;

        public static bool HasEnded(EventData ev) =>
            ev.EventEndTime < DateTime.Now;

        
        public static Task<int> CountActiveParticipationAsync(GoHikeDataContext db, long userId) =>
            db.EventRegistrationAndMemberLists.CountAsync(r =>
                r.UserId == userId &&
                r.RegistrationStatus == 1 &&
                r.Event.EventEndTime > DateTime.Now);
    }
}
