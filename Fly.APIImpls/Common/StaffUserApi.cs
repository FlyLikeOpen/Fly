using Fly.Framework.Common;
using Fly.Framework.SqlDbAccess;
using Fly.APIs.Common;
using Fly.Objects;
using Fly.Objects.Common;
using Fly.Objects.Common.MessageTask;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Security;

namespace Fly.APIImpls.Common
{
    public class StaffUserApi : IStaffUserApi
    {
        #region Private Member

        private const string STAFF_USER_CACHE_KEY = "CachedStaffUserList";
        private const string TABLE_NAME = "[FlyBase].[dbo].[StaffUser]";
        private static DataStatusUpdater s_StatusUpdater = new DataStatusUpdater(TABLE_NAME, true);

        //private string GeneratePassword(string password, string salt)
        //{
        //    return Crypto.GetCrypto(CryptoType.MD5).Encrypt(password + salt);
        //}
        private string GeneratePassword(string password, string salt, int format = -1)
        {
            if (format == -1)
            {
                return Crypto.GetCrypto(CryptoType.MD5).Encrypt(password + salt);
            }
            MethodInfo encodePasswordMethodInfo = Cache.GetWithLocalCache("EncodePasswordMethodInfo", () =>
            {
                return typeof(SqlMembershipProvider).GetMethod("EncodePassword", BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof(string), typeof(int), typeof(string) }, null);
            }, true, 60 * 24 * 7);
            return encodePasswordMethodInfo.Invoke(System.Web.Security.Membership.Provider, new object[] { password, format, salt }) as string;
        }

        private string GeneratePasswordSalt()
        {
            return Guid.NewGuid().ToString("N");
        }

        private void RemoveAllUserCache()
        {
            Cache.RemoveFromLocalCache(STAFF_USER_CACHE_KEY);
        }

        private void RemoveAllUserRoleCache()
        {
            var allUsers = GetAllUsers();
            foreach (var u in allUsers)
            {
                string key = "roles_" + u.Id;
                Cache.RemoveFromLocalCache(key);
            }
        }

        private void RemoveAllUserTempPermissionCache()
        {
            var allUsers = GetAllUsers();
            foreach (var u in allUsers)
            {
                string key = "temp_permission_" + u.Id;
                Cache.RemoveFromLocalCache(key);
            }
        }

        private StaffUser CheckUserLogin(string loginId, string password)
        {
            if (string.IsNullOrWhiteSpace(loginId) || string.IsNullOrWhiteSpace(password))
            {
                throw new BizException("登录名或密码不能为空");
            }
            loginId = loginId.Trim();

            string sql = string.Format(@"
SELECT TOP 1
	a.*
FROM
	[FlyBase].[dbo].[StaffUser] AS a WITH(NOLOCK)
WHERE
	a.[LoginId]=@loginId");
            StaffUser au = null;
            string passwordDB = null;
            string passwordSaltDB = null;
            SqlHelper.ExecuteReaderFirst(reader =>
            {
                au = SqlHelper.ConvertReaderToEntity<StaffUser>(reader);
                passwordDB = reader.Field<string>("Password");
                passwordSaltDB = reader.Field<string>("PasswordSalt");
            }, DbInstance.OnlyRead, sql, new { loginId });

            if (au == null)
            {
                throw new BizException("登录名和密码不匹配");
            }

            string pwd = this.GeneratePassword(password, passwordSaltDB);
            if (pwd != passwordDB)
            {
                pwd = this.GeneratePassword(password, passwordSaltDB, 2);
                if (pwd != passwordDB)
                {
                    throw new BizException("登录名和密码不匹配");
                }
            }

            if (au.Status == DataStatus.Deleted)
            {
                throw new BizException("此用户已经被删除，无法恢复，不能再登录");
            }

            if (au.Status == DataStatus.Disabled)
            {
                throw new BizException("此用户已被禁用，不能再登录；需要系统管理员重新启用此账号后才能进行登录");
            }

            if (HasPermission(au.Id, "Fly_Login") == false)
            {
                throw new BizException("账号\"{0}\"没有登陆当前系统的权限", loginId);
            }

            return au;
        }

        #endregion

        public IList<StaffUser> GetAllUsers()
        {
            return Cache.GetWithLocalCache<List<StaffUser>>(STAFF_USER_CACHE_KEY, () =>
            {
                string sql = @"
SELECT
	a.*
FROM
	[FlyBase].[dbo].[StaffUser] AS a WITH(NOLOCK)";
                return SqlHelper.ExecuteEntityList<StaffUser>(DbInstance.OnlyRead, sql);
            }, true, 120);
        }

        public StaffUser GetByLoginId(string loginId)
        {
            var list = GetByLoginId(new string[] { loginId });
            return list.FirstOrDefault();
        }

        public IList<StaffUser> GetByLoginId(IEnumerable<string> loginIdList)
        {
            if (loginIdList == null || loginIdList.Count() <= 0)
            {
                return new List<StaffUser>(0);
            }
            IList<StaffUser> list = GetAllUsers();
            if (list == null || list.Count <= 0)
            {
                return new List<StaffUser>(0);
            }
            list = list.Where(x => loginIdList.Contains(x.LoginId, StringComparer.InvariantCultureIgnoreCase)).ToList();
            return list;
        }

        public IList<StaffUser> Query(string name, IEnumerable<DataStatus> statusList, Guid? belongsToRoleId, string hasPermissionKey,
            int pageIndex, int pageSize, out int totalCount)
        {
            StringBuilder where = new StringBuilder();
            where.Append(@"
FROM
	[FlyBase].[dbo].[StaffUser] AS a WITH(NOLOCK)
WHERE
    1=1");
            if (string.IsNullOrWhiteSpace(name) == false)
            {
                where.AppendFormat(@"
	AND (a.[LoginId] LIKE '%{0}%' OR a.[DisplayName] LIKE '%{0}%' )", name.Trim().ToSafeSql(true));
            }

            if (statusList != null && statusList.Count() > 0)
            {
                string str = string.Join(",", statusList.Select(x => (int)x).Distinct());
                where.AppendFormat(@"
	AND a.[Status] IN ({0})", str);
            }

            if (belongsToRoleId.HasValue)
            {
                where.AppendFormat(@"
	AND a.[Id] IN
	(
		SELECT DISTINCT
            [UserId]
		FROM
            [FlyBase].[dbo].[StaffUserInRole] WITH(NOLOCK)
		WHERE
            [RoleId]='{0}'
	)", belongsToRoleId.Value);
            }

            if (string.IsNullOrWhiteSpace(hasPermissionKey) == false)
            {
                where.AppendFormat(@"
	AND a.[Id] IN
	(
		SELECT
            [UserId]
		FROM
            [FlyBase].[dbo].[StaffUserInRole] AS u1 WITH(NOLOCK)
        INNER JOIN
            [FlyBase].[dbo].[StaffRolePermission] AS u2 WITH(NOLOCK)
                ON u1.[RoleId]=u2.[RoleId]
		WHERE
            u2.[PermissionKey] IN ('{0}', 'WMSLocalAdmin')
		UNION ALL
		SELECT
            [UserId]
		FROM
            [FlyBase].[dbo].[StaffUserTempPermission] WITH(NOLOCK)
		WHERE
            [PermissionKey] IN ('{0}', 'WMSLocalAdmin')
	)", hasPermissionKey.Trim().ToSafeSql());
            }
            totalCount = SqlHelper.ExecuteScalar<int>(DbInstance.OnlyRead, "SELECT TOP 1 COUNT(*)" + where);
            string fields = string.Format(@"
SELECT
    a.*
    ,ROW_NUMBER() OVER(ORDER BY a.[CreatedOn] DESC) AS [RowNo]{0}", where);
            string sql = fields.ToPagerSql(pageIndex, pageSize);
            return SqlHelper.ExecuteEntityList<StaffUser>(DbInstance.OnlyRead, sql);
        }

        public Guid Create(string loginId, string displayName, string password, string email, string mobile)
        {
            if (string.IsNullOrWhiteSpace(loginId))
            {
                throw new BizException("登录名不能为空！");
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new BizException("显示名不能为空！");
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                throw new BizException("密码不能为空！");
            }

            loginId = loginId.Trim();
            displayName = displayName.Trim();

            if (loginId.Length > 32)
            {
                throw new BizException("登录名不能超过32个字符！");
            }

            if (displayName.Length > 32)
            {
                throw new BizException("显示名不能超过32个字符！");
            }

            if (string.IsNullOrWhiteSpace(email) == false)
            {
                email = email.Trim();
                if (Regex.IsMatch(email, @"^\w+([\.-]?\w+)*@\w+([\.-]?\w+)*(\.\w{2,3})+$") == false)
                {
                    throw new BizException("电子邮箱格式不合法！");
                }
                if (email.Length > 256)
                {
                    throw new BizException("电子邮箱不能超过256个字符！");
                }
            }

            if (string.IsNullOrWhiteSpace(mobile) == false
                && HelperUtility.IsMobileFormat(mobile.Trim()) == false)
            {
                throw new BizException("手机号格式不合法！");
            }

            string check_sql = string.Format(@"SELECT TOP 1 1 FROM [FlyBase].[dbo].[StaffUser] WITH(NOLOCK) WHERE [LoginId]='{0}'", loginId.ToSafeSql());
            int? r = SqlHelper.ExecuteScalar<int?>(DbInstance.OnlyRead, check_sql);
            if (r.HasValue && r.Value == 1)
            {
                throw new BizException("对不起，登录名'{0}'已经被使用过了，请换一个登录名！", loginId);
            }

            string passwordSalt = this.GeneratePasswordSalt();
            string encodedPassword = this.GeneratePassword(password, passwordSalt);

            string sql = string.Format(@"
IF NOT EXISTS(SELECT TOP 1 1 FROM [FlyBase].[dbo].[StaffUser] WITH(NOLOCK) WHERE [LoginId]=@LoginId)
BEGIN
	INSERT INTO
		[FlyBase].[dbo].[StaffUser]
	(
		 [Id]
		,[LoginId]
		,[DisplayName]
		,[Password]
		,[PasswordSalt]
		,[Email]
		,[Mobile]
        ,[Status]
		,[CreatedOn]
		,[CreatedBy]
	)
	VALUES
	(
		 @Id
		,@LoginId
		,@DisplayName
		,@Password
		,@PasswordSalt
		,@Email
		,@Mobile
		,@Status
		,GETDATE()
		,@operatorId
	)
END");
            Guid userId = Guid.NewGuid();
            int x = SqlHelper.ExecuteNonQuery(DbInstance.CanWrite, sql, new
            {
                Id = userId,
                LoginId = loginId,
                DisplayName = displayName,
                Password = encodedPassword,
                PasswordSalt = passwordSalt,
                Email = email,
                Mobile = mobile,
                Status = (int)DataStatus.Enabled,
                operatorId = ContextManager.Current.UserId
            });
            if (x <= 0)
            {
                throw new BizException("对不起，登录名'{0}'已经被使用过了，请换一个登录名！", loginId);
            }
            this.RemoveAllUserCache();
            return userId;
        }

        public void Update(Guid userId, string displayName, string email, string mobile)
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new BizException("显示名不能为空！");
            }

            displayName = displayName.Trim();
            if (displayName.Length > 32)
            {
                throw new BizException("显示名不能超过32个字符！");
            }

            if (string.IsNullOrWhiteSpace(email) == false)
            {
                if (Regex.IsMatch(email.Trim(), @"^\w+([\.-]?\w+)*@\w+([\.-]?\w+)*(\.\w{2,3})+$") == false)
                {
                    throw new BizException("电子邮箱格式不合法");
                }
                if (email.Length > 256)
                {
                    throw new BizException("电子邮箱不能超过256个字符！");
                }
            }

            if (string.IsNullOrWhiteSpace(mobile) == false && HelperUtility.IsMobileFormat(mobile.Trim()) == false)
            {
                throw new BizException("手机号格式不合法");
            }

            string sql = @"
UPDATE TOP (1)
	[FlyBase].[dbo].[StaffUser]
SET
	 [DisplayName]=@DisplayName
	,[Email]=@Email
	,[Mobile]=@Mobile
	,[UpdatedOn]=GETDATE()
	,[UpdatedBy]=@operatorId
WHERE
	[Id]=@UserId";

            int x = SqlHelper.ExecuteNonQuery(DbInstance.CanWrite, sql, new
            {
                UserId = userId,
                DisplayName = displayName,
                Email = email,
                Mobile = mobile,
                operatorId = ContextManager.Current.UserId
            });

            if (x > 0)
            {
                this.RemoveAllUserCache();
            }
        }

        public void ChangePassword(Guid userId, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword))
            {
                throw new BizException("密码不能为空！");
            }

            string passwordSalt = this.GeneratePasswordSalt();
            string encodedPassword = this.GeneratePassword(newPassword, passwordSalt);
            string sql = @"
UPDATE TOP (1)
	[FlyBase].[dbo].[StaffUser]
SET
    [Password]=@Password
    ,[PasswordSalt]=@PasswordSalt
WHERE
	[Id]=@UserId";

            int x = SqlHelper.ExecuteNonQuery(DbInstance.CanWrite, sql, new
            {
                UserId = userId,
                Password = encodedPassword,
                PasswordSalt = passwordSalt
            });
        }

        public bool CheckPassword(Guid userId, string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                return false;
            }
            string sql = string.Format(@"
SELECT TOP 1
	a.[Password],
    a.[PasswordSalt]
FROM
	[FlyBase].[dbo].[StaffUser] AS a WITH(NOLOCK)
WHERE
	a.[Id]=@userId");
            string dbPassord = null;
            string passwordSalt = null;
            SqlHelper.ExecuteReaderFirst(r =>
            {
                dbPassord = r.GetString(0);
                passwordSalt = r.GetString(1);
            }, DbInstance.OnlyRead, sql, new { userId });
            if (dbPassord == null || passwordSalt == null)
            {
                return false;
            }
            string pwd = GeneratePassword(password, passwordSalt);
            if (pwd != dbPassord)
            {
                pwd = this.GeneratePassword(password, passwordSalt, 2);
            }
            return (pwd == dbPassord);
        }

        public bool Login(string loginId, string password)
        {
            if (string.IsNullOrWhiteSpace(loginId))
            {
                throw new BizException("登录名不能为空！");
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                throw new BizException("密码不能为空！");
            }

            StaffUser user = CheckUserLogin(loginId, password);
            if (user == null)
            {
                throw new BizException("没有找到对应用户，登录失败");
            }
            Cookie.Set("StaffUser", user.Id);
            return true;
        }

        public void Logout()
        {
            Cookie.Remove("StaffUser");
        }

        #region 用户的角色

        public void SetRoles(Guid userId, IEnumerable<Guid> roleIds)
        {
            StaffUser user = Api<IStaffUserApi>.Instance.Get(userId);
            if (user == null)
            {
                throw new ApplicationException("Id为" + userId + "的StaffUser数据不存在");
            }
            if (user.Status == DataStatus.Deleted)
            {
                throw new BizException("此用户已经被删除，无法再修改其角色");
            }

            StringBuilder sb = new StringBuilder();
            if (roleIds != null && roleIds.Count() > 0)
            {
                foreach (Guid roleId in roleIds)
                {
                    if (sb.Length > 0)
                    {
                        sb.Append(@"
UNION ALL");
                    }

                    sb.AppendFormat(@"
SELECT '{0}' AS [UserId] ,'{1}' AS [RoleId]", userId, roleId);
                }
            }

            if (sb.Length > 0)
            {
                sb.Insert(0, @"
INSERT INTO
    [FlyBase].[dbo].[StaffUserInRole]
(
    [UserId]
    ,[RoleId]
)");
            }

            // 先删除原来的
            sb.Insert(0, string.Format(@"
DELETE FROM
    [FlyBase].[dbo].[StaffUserInRole]
WHERE
	[UserId]='{0}'", userId));

            int x = SqlHelper.ExecuteNonQuery(DbInstance.CanWrite, sb.ToString());
            if (x > 0)
            {
                Cache.RemoveFromLocalCache("roles_" + userId);
                foreach (var roleId in roleIds)
                {
                    Cache.RemoveFromLocalCache("users_" + roleId);
                }
            }
        }

        public IList<Guid> GetRoles(Guid userId)
        {
            //return Cache.GetWithLocalCache<List<Guid>>("roles_" + userId, () =>
            //{
            //    string sql = "SELECT [RoleId] FROM [FlyBase].[dbo].[StaffUserInRole] WITH(NOLOCK) WHERE [UserId]=@userId";
            //    return SqlHelper.ExecuteList<Guid>(r => r.GetGuid(0), DbInstance.OnlyRead, sql, new { userId });
            //}, true, 120);
            var dic = GetRoles(new Guid[] { userId });
            if (dic == null || dic.Count <= 0)
            {
                return new List<Guid>(0);
            }
            return dic.First().Value;
        }

        public IDictionary<Guid, IList<Guid>> GetRoles(IEnumerable<Guid> userIds)
        {
            if (userIds == null || userIds.Count() <= 0)
            {
                return new Dictionary<Guid, IList<Guid>>(0);
            }
            userIds = userIds.Distinct();
            int c = userIds.Count();
            Dictionary<Guid, IList<Guid>> rst = new Dictionary<Guid, IList<Guid>>(c * 2);
            List<Guid> noCacheIdList = new List<Guid>(c);
            foreach (var userId in userIds)
            {
                string key = "roles_" + userId;
                var t = Cache.GetLocalCache(key) as List<Guid>;
                if (t != null)
                {
                    rst.Add(userId, t);
                }
                else
                {
                    noCacheIdList.Add(userId);
                }
            }

            if (noCacheIdList.Count > 0)
            {
                string str = string.Join("','", noCacheIdList);
                string sql = string.Format(@"SELECT  [UserId], [RoleId] FROM [FlyBase].[dbo].[StaffUserInRole] WITH(NOLOCK) WHERE [UserId] IN ('{0}')", str);
                var dic = new Dictionary<Guid, List<Guid>>(noCacheIdList.Count() * 2);
                SqlHelper.ExecuteReaderAll(reader =>
                {
                    Guid userId = reader.Field<Guid>("UserId");
                    Guid roleId = reader.Field<Guid>("RoleId");

                    List<Guid> tmp;
                    if (dic.TryGetValue(userId, out tmp))
                    {
                        if (tmp.Contains(roleId) == false)
                        {
                            tmp.Add(roleId);
                        }
                    }
                    else
                    {
                        dic.Add(userId, new List<Guid> { roleId });
                    }
                }, DbInstance.OnlyRead, sql);
                foreach (var uid in noCacheIdList)
                {
                    if (dic.ContainsKey(uid) == false)
                    {
                        dic.Add(uid, new List<Guid>(0));
                    }
                }
                if (dic.Count > 0)
                {
                    foreach (var entry in dic)
                    {
                        string key = "roles_" + entry.Key;
                        Cache.SetLocalCache(key, entry.Value, true, 120);
                        rst.Add(entry.Key, entry.Value);
                    }
                }
            }
            return rst;
        }

        #endregion

        #region 临时权限

        public void AddTempPermissionSetting(Guid userId, string permissionKey, DateTime fromTime, DateTime toTime)
        {
            StaffUser user = Api<IStaffUserApi>.Instance.Get(userId);
            if (user == null)
            {
                throw new ApplicationException("Id为'" + userId + "'的系统用户不存在！");
            }
            if (string.IsNullOrWhiteSpace(permissionKey))
            {
                throw new BizException("临时权限的权限点不能为空！");
            }
            if (fromTime >= toTime)
            {
                throw new BizException("临时权限的有效期开始时间必须小于结束时间！");
            }

            string sql = @"
UPDATE TOP (1)
	[FlyBase].[dbo].[StaffUserTempPermission]
SET
	[FromTime]=@fromTime,
	[ToTime]=@toTime,
    [UpdatedOn]=GETDATE(),
    [UpdatedBy]=@operatorId
WHERE
	[UserId]=@userId
    AND [PermissionKey]=@permissionKey
IF @@ROWCOUNT<=0
BEGIN
	INSERT INTO [FlyBase].[dbo].[StaffUserTempPermission]
	(
		[UserId], [PermissionKey], [FromTime], [ToTime], [UpdatedOn], [UpdatedBy]
	)
	VALUES
	(
		@userId, @permissionKey, @fromTime, @toTime, GETDATE(), @operatorId
	)
END";
            int x = SqlHelper.ExecuteNonQuery(DbInstance.CanWrite, sql, new { userId, permissionKey, fromTime, toTime, operatorId = ContextManager.Current.UserId });
            if (x > 0)
            {
                string key = "temp_permission_" + userId;
                Cache.RemoveFromLocalCache(key);
            }
        }

        public void RemoveTempPermissionSetting(Guid userId, string permissionKey)
        {
            if (string.IsNullOrWhiteSpace(permissionKey))
            {
                return;
            }
            string sql = "DELETE TOP (1) FROM [FlyBase].[dbo].[StaffUserTempPermission] WHERE [UserId]=@userId AND [PermissionKey]=@permissionKey";
            int x = SqlHelper.ExecuteNonQuery(DbInstance.CanWrite, sql, new { userId, permissionKey });
            if (x > 0)
            {
                string key = "temp_permission_" + userId;
                Cache.RemoveFromLocalCache(key);
            }
        }

        public IList<StaffUserTempPermission> GetTempPermissionSettings(Guid userId)
        {
            string key = "temp_permission_" + userId;
            var list = Cache.GetWithLocalCache<List<StaffUserTempPermission>>(key, () =>
            {
                string sql = @"
SELECT
    *
FROM
    [FlyBase].[dbo].[StaffUserTempPermission] WITH(NOLOCK)
WHERE
    [UserId]=@userId
    AND [FromTime]<=GETDATE()
    AND [ToTime]>=GETDATE()";
                return SqlHelper.ExecuteEntityList<StaffUserTempPermission>(DbInstance.OnlyRead, sql, new { userId });
            }, true, 120);
            return list.FindAll(x => x.Valid);
        }

        #endregion

        public bool HasPermission(Guid userId, string permissionKey)
        {
            var roleIds = GetRoles(userId);
            if (roleIds != null && roleIds.Count > 0)
            {
                IList<string> userAuthKeys = Api<IStaffRoleApi>.Instance.GetPermissionKeys(roleIds);
                if (userAuthKeys != null && userAuthKeys.Count > 0)
                {
                    foreach (string item in userAuthKeys)
                    {
                        if (string.IsNullOrWhiteSpace(item))
                        {
                            continue;
                        }
                        string userKey = item.Trim();
                        if (string.Equals(userKey, permissionKey) || string.Equals(userKey, "FlyAdmin"))
                        {
                            return true;
                        }
                    }
                }
            }

            var tempPermissions = GetTempPermissionSettings(userId);
            foreach (var item in tempPermissions)
            {
                if (item == null || item.Valid == false)
                {
                    continue;
                }
                string userKey = item.PermissionKey.Trim();
                if (string.Equals(userKey, permissionKey) || string.Equals(userKey, "WMSLocalAdmin"))
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasPermissionOfCurrentUser(string permissionKey)
        {
            return HasPermission(ContextManager.Current.UserId, permissionKey);
        }

        public void NoticeWaitingTask(AdminUserNoticeType noticeType, Guid adminUserId, string url, string title, string taskName, string taskType, string taskDesc)
        {
            NoticeWaitingTask(noticeType, new Guid[] { adminUserId }, url, title, taskName, taskType, taskDesc);
        }

        public void NoticeWaitingTask(AdminUserNoticeType noticeType, IEnumerable<Guid> adminUserIdList, string url, string title, string taskName, string taskType, string taskDesc)
        {
            if (adminUserIdList == null || !adminUserIdList.Any())
            {
                return;
            }
            IList<StaffUser> userList = Get(adminUserIdList.Distinct());
            if (userList == null || userList.Count <= 0)
            {
                return;
            }
            List<string> emailList = new List<string>(userList.Count);
            List<string> workWeChatUserIdList = new List<string>(userList.Count);
            foreach (var user in userList)
            {
                if (user.Status != DataStatus.Enabled)
                {
                    continue;
                }
                if (string.IsNullOrWhiteSpace(user.Email) == false
                    && (noticeType == AdminUserNoticeType.BothEmailAndWeiXin || noticeType == AdminUserNoticeType.OnlyEmail))
                {
                    emailList.Add(user.Email);
                }
            }

        }

        #region Get/GetId/GetIdNumber/Enable/Disable/Delete

        //public Guid? GetId(long idNumber)
        //{
        //    IList<StaffUser> list = GetAllUsers();
        //    var u = list.FirstOrDefault(x => x.IdNumber == idNumber);
        //    return u == null ? null : new Guid?(u.Id);
        //}

        //public long? GetIdNumber(Guid id)
        //{
        //    IList<StaffUser> list = GetAllUsers();
        //    var u = list.FirstOrDefault(x => x.Id == id);
        //    return u == null ? null : new long?(u.IdNumber);
        //}

        //public IDictionary<long, Guid> GetId(IEnumerable<long> idNumberList)
        //{
        //    if (idNumberList == null || idNumberList.Count() <= 0)
        //    {
        //        return new Dictionary<long, Guid>(0);
        //    }
        //    IList<StaffUser> list = GetAllUsers();
        //    var uList = list.Where(x => idNumberList.Contains(x.IdNumber)).ToList();
        //    if (uList == null || uList.Count <= 0)
        //    {
        //        return new Dictionary<long, Guid>(0);
        //    }
        //    Dictionary<long, Guid> rst = new Dictionary<long, Guid>(uList.Count * 2);
        //    foreach (var u in uList)
        //    {
        //        rst.Add(u.IdNumber, u.Id);
        //    }
        //    return rst;
        //}

        //public IDictionary<Guid, long> GetIdNumber(IEnumerable<Guid> idList)
        //{
        //    if (idList == null || idList.Count() <= 0)
        //    {
        //        return new Dictionary<Guid, long>(0);
        //    }
        //    IList<StaffUser> list = GetAllUsers();
        //    var uList = list.Where(x => idList.Contains(x.Id)).ToList();
        //    if (uList == null || uList.Count <= 0)
        //    {
        //        return new Dictionary<Guid, long>(0);
        //    }
        //    Dictionary<Guid, long> rst = new Dictionary<Guid, long>(uList.Count * 2);
        //    foreach (var u in uList)
        //    {
        //        rst.Add(u.Id, u.IdNumber);
        //    }
        //    return rst;
        //}

        public StaffUser Get(Guid id)
        {
            IList<StaffUser> list = this.Get(new Guid[] { id });
            if (list == null || list.Count <= 0)
            {
                return null;
            }
            return list.FirstOrDefault();
        }

        public IList<StaffUser> Get(IEnumerable<Guid> idList)
        {
            if (idList == null || idList.Count() <= 0)
            {
                return new List<StaffUser>(0);
            }

            IList<StaffUser> list = GetAllUsers();
            if (list == null || list.Count <= 0)
            {
                return new List<StaffUser>(0);
            }
            list = list.Where(x => idList.Contains(x.Id)).ToList();
            return list;
        }

        public void Enable(Guid id)
        {
            if (s_StatusUpdater.Enable(id))
            {
                RemoveAllUserCache();
            }
        }

        public void Disable(Guid id)
        {
            if (s_StatusUpdater.Disable(id))
            {
                RemoveAllUserCache();
            }
        }

        public void Delete(Guid id)
        {
            if (s_StatusUpdater.Delete(id))
            {
                RemoveAllUserCache();
            }
        }

        #endregion

    }
}
