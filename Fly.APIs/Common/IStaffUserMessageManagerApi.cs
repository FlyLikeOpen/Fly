using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fly.APIs.Common
{
    public interface IStaffUserMessageManagerApi
    {
        /// <summary>
        /// Global里使用，只能调用一次
        /// </summary>
        void BindSocketEvents();
        bool CheckStaffUserIsOnline(Guid userId);
        IEnumerable<Guid> GetOnlineStaffUsers();
        void SetStaffUserOnline(Guid userId);
        void SetStaffUserOffline(Guid userId);
        void NotifyStaffUsers(string message);
        void NotifyStaffUsers(Guid? userId, string message);
        void NotifyStaffUsers(Guid userId, string message);
        void NotifyStaffUsers(IEnumerable<Guid?> userIds, string message);
        void NotifyStaffUsers(IEnumerable<Guid> userIds, string message);
        void NotifyStaffUsers(IEnumerable<Guid?> userIds, Func<Guid, string> msgGenarateByUserId);
        void NotifyStaffUsers(IEnumerable<Guid> userIds, Func<Guid, string> msgGenarateByUserId);
    }
}
