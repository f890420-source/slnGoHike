using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using prjGoHike.Models;

namespace prjGoHike.Services
{
    public class Condition_observe_service
    {
        public const string Recruiting = "招募中";
        public const string Ended = "已結束";
        public const string full = "已額滿";
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

        public static string CaculateStatus(EventData ev, int participantCount)
        {
            if(HasEnded(ev))
            {
                return Ended;
            }
            if(HasStarted(ev))
            {
                return Running;
            }
            if(participantCount >= ev.MaximumNumber)
            {
                return full;
            }

                return Recruiting;
            
        }
    }
}
