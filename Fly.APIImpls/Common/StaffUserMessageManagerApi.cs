using Fly.APIs.Common;
using Fly.Framework.Common;
using Fly.Objects.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fly.APIImpls.Common
{
    public class StaffUserMessageManagerApi : IStaffUserMessageManagerApi
    {
        #region private variable
        private static ConcurrentDictionary<Guid, DateTime> OnlineStaffUserIds = new ConcurrentDictionary<Guid, DateTime>(32, 16);
        private static IWebSocketServerManager webSocketServerManager;
        #endregion
        #region ctor
        static StaffUserMessageManagerApi()
        {
            webSocketServerManager = Api<IWebSocketServerManager>.GetInstance("MMServer");
            var t = Task.Run(() =>
            {
                while (true)
                {
                    Thread.Sleep(1000 * 30);
                    ClearInactiveStaffUsers();
                }
            });
        }
        #endregion
        #region public method
        public void BindSocketEvents()
        {
            webSocketServerManager.AddDisconnectedEvent(StaffUserDisconnected);
            webSocketServerManager.AddNewConnectionEvent(StaffUserNewConnection);
            webSocketServerManager.AddDataReceivedEvent(StaffUserDataReceived);
        }
        public IEnumerable<Guid> GetOnlineStaffUsers()
        {
            return OnlineStaffUserIds.Select(r => r.Key);
        }
        public bool CheckStaffUserIsOnline(Guid userId)
        {
            return OnlineStaffUserIds.ContainsKey(userId);
        }
        public void SetStaffUserOnline(Guid userId)
        {
            StaffOnline(userId);
        }
        public void SetStaffUserOffline(Guid userId)
        {
            StaffOffline(userId);
        }
        public void NotifyStaffUsers(Guid? userId, string message)
        {
            if (!userId.HasValue)
                return;
            this.NotifyStaffUsers(userId.Value, message);
        }
        public void NotifyStaffUsers(Guid userId, string message)
        {
            var userIds = new Guid[] { userId };
            this.NotifyStaffUsers(userIds, r => message);
        }
        public void NotifyStaffUsers(IEnumerable<Guid?> userIds, string message)
        {
            this.NotifyStaffUsers(userIds.Where(r => r.HasValue).Select(x => (Guid)x), message);
        }
        public void NotifyStaffUsers(IEnumerable<Guid> userIds, string message)
        {
            this.NotifyStaffUsers(userIds, r => message);
        }
        public void NotifyStaffUsers(IEnumerable<Guid?> userIds, Func<Guid, string> msgGenarateByUserId)
        {
            this.NotifyStaffUsers(userIds.Where(r => r.HasValue).Select(x => (Guid)x), msgGenarateByUserId);
        }
        public void NotifyStaffUsers(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;
            webSocketServerManager?.Send(message);
        }
        public void NotifyStaffUsers(IEnumerable<Guid> userIds, Func<Guid, string> msgGenarateByUserId)
        {
            if (userIds == null || !userIds.Any())
            {
                return;
            }
            foreach (var userId in userIds)
            {
                webSocketServerManager?.Send(userId.ToString(), msgGenarateByUserId(userId));
            }
        }
        #endregion
        #region private method
        private static void StaffOnline(Guid userId)
        {
            if (!OnlineStaffUserIds.ContainsKey(userId))
            {
                webSocketServerManager?.Send(userId.ToString(), Utils.GenerateSocketMsg("WelcomeMsg", "您已上线"));
            }
            webSocketServerManager?.Send(userId.ToString(), Utils.GenerateSocketMsg("IsOnline", "true"));
            OnlineStaffUserIds.AddOrUpdate(userId, DateTime.Now, (a, b) => b = DateTime.Now);
        }
        private static void StaffOffline(Guid userId)
        {
            DateTime dt;
            OnlineStaffUserIds.TryRemove(userId, out dt);
            webSocketServerManager?.Send(userId.ToString(), Utils.GenerateSocketMsg("IsOnline", "false"));
            webSocketServerManager?.Send(userId.ToString(), Utils.GenerateSocketMsg("WelcomeMsg", "您已离线"));
        }
        private static Guid GetStaffUserId(object sender)
        {
            var socketConnection = sender as ISocketConnection;
            Guid userId;
            Guid.TryParse(socketConnection?.GetRouteKey(), out userId);
            return userId;
        }
        private static void StaffUserDisconnected(object sender, EventArgs e)
        {
            var socketConnection = sender as ISocketConnection;
            if (socketConnection == null)
                return;
            var userId = GetStaffUserId(sender);
            StaffOffline(userId);
        }
        private static void StaffUserNewConnection(object sender, EventArgs e)
        {
            var socketConnection = sender as ISocketConnection;
            if (socketConnection == null)
                return;
            var userId = GetStaffUserId(sender);
            StaffOnline(userId);
        }
        private static void StaffUserDataReceived(object sender, string message, EventArgs e)
        {
            var userId = GetStaffUserId(sender);
            StaffOnline(userId);
        }
        private static void ClearInactiveStaffUsers()
        {
            var staffOfflineTimeOut = 0;
            int.TryParse(ConfigurationManager.AppSettings["StaffOfflineTimeOut"], out staffOfflineTimeOut);
            if(staffOfflineTimeOut == 0)
                staffOfflineTimeOut = 30;
            var inactionUserIds = OnlineStaffUserIds.Where(item => item.Value.CompareTo(DateTime.Now.AddMinutes(-staffOfflineTimeOut)) < 0).Select(r => r.Key);
            foreach (var id in inactionUserIds)
            {
                StaffOffline(id);
            }
        }
        #endregion
    }
}
