using Fly.Objects;
using Fly.Objects.Common;
using System;
using System.Collections.Generic;
using System.Data;

namespace Fly.APIs.Common
{
    public interface IStaffUserApi : IObjectGetter<StaffUser>, IDataStatusUpdater//, INumberAndIdMapper
    {
        IList<StaffUser> GetAllUsers();

        StaffUser GetByLoginId(string loginId);

        IList<StaffUser> GetByLoginId(IEnumerable<string> loginIdList);

        IList<StaffUser> Query(string name, IEnumerable<DataStatus> statusList, Guid? belongsToRoleId, string hasPermissionKey,
            int pageIndex, int pageSize, out int totalCount);

        Guid Create(string loginId, string displayName, string password, string email, string mobile);

        void Update(Guid userId, string displayName, string email, string mobile);

        void ChangePassword(Guid userId, string newPassword);

        bool CheckPassword(Guid userId, string password);

        bool Login(string loginId, string password);

        void Logout();

        void SetRoles(Guid userId, IEnumerable<Guid> roleIds);

        IList<Guid> GetRoles(Guid userId);

        IDictionary<Guid, IList<Guid>> GetRoles(IEnumerable<Guid> userIds);

        void AddTempPermissionSetting(Guid userId, string permissionKey, DateTime fromTime, DateTime toTime);

        void RemoveTempPermissionSetting(Guid userId, string permissionKey);

        IList<StaffUserTempPermission> GetTempPermissionSettings(Guid userId);

        bool HasPermission(Guid userId, string permissionKey);

        bool HasPermissionOfCurrentUser(string permissionKey);

        void NoticeWaitingTask(AdminUserNoticeType noticeType, Guid adminUserId, string url, string title, string taskName, string taskType, string taskDesc);

        void NoticeWaitingTask(AdminUserNoticeType noticeType, IEnumerable<Guid> adminUserIdList, string url, string title, string taskName, string taskType, string taskDesc);   
    }

    public enum AdminUserNoticeType
    {
        BothEmailAndWeiXin = 0,
        OnlyEmail = 1,
        OnlyWeiXin = 2//现在用企业微信发送
    }
}