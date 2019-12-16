using Fly.Framework.Common;
using Fly.Framework.SqlDbAccess;
using Fly.APIs.Common;
using Fly.Objects.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fly.APIImpls.Common
{
    public class StaffRoleApi : IStaffRoleApi
    {
        private const string STAFF_ROLE_CACHE_KEY = "CachedStaffRoleList";

        public void RemoveAllRoleCache()
        {
            Cache.RemoveFromLocalCache(STAFF_ROLE_CACHE_KEY);
        }

        public void RemoveAllRolePermissionCache()
        {
            var allRoles = GetAllRoles();
            foreach (var r in allRoles)
            {
                string key = "r_permission_" + r.Id;
                Cache.RemoveFromLocalCache(key);
            }
        }

        public IList<StaffRole> GetAllRoles()
        {
            return Cache.GetWithLocalCache<List<StaffRole>>(STAFF_ROLE_CACHE_KEY, () =>
            {
                string sql = @"
SELECT
	a.*
FROM
	[FlyBase].[dbo].[StaffRole] AS a WITH(NOLOCK)";
                return SqlHelper.ExecuteEntityList<StaffRole>(DbInstance.OnlyRead, sql);
            }, true, 120);
        }

        public StaffRole Get(Guid id)
        {
            IList<StaffRole> list = this.Get(new Guid[] { id });
            if (list == null || list.Count <= 0)
            {
                return null;
            }
            return list.FirstOrDefault();
        }

        public IList<StaffRole> Get(IEnumerable<Guid> idList)
        {
            if (idList == null || idList.Count() <= 0)
            {
                return new List<StaffRole>(0);
            }

            IList<StaffRole> list = GetAllRoles();
            if (list == null || list.Count <= 0)
            {
                return new List<StaffRole>(0);
            }
            list = list.Where(x => idList.Contains(x.Id)).ToList();
            return list;
        }

        public IList<StaffRole> Query(string name, Guid? containsUserId, string hasPermissionKey,
            int pageIndex, int pageSize, out int totalCount)
        {
            StringBuilder where = new StringBuilder();
            where.Append(@"
FROM
	[FlyBase].[dbo].[StaffRole] AS role WITH(NOLOCK)
WHERE
	1=1");

            if (string.IsNullOrWhiteSpace(name) == false)
            {
                where.AppendFormat(@"
	AND role.[Name] LIKE '%{0}%'", name.Trim().ToSafeSql(true));
            }

            if (containsUserId.HasValue)
            {
                where.AppendFormat(@"
	AND role.[Id] IN
	(
		SELECT
            [RoleId]
		FROM
            [FlyBase].[dbo].[StaffUserInRole] WITH(NOLOCK)
		WHERE
            [UserId]='{0}'
	)", containsUserId.Value);
            }

            if (string.IsNullOrWhiteSpace(hasPermissionKey) == false)
            {
                where.AppendFormat(@"
	AND role.[Id] IN
	(
		SELECT
            [RoleId]
		FROM
            [FlyBase].[dbo].[StaffRolePermission] WITH(NOLOCK)
		WHERE
            [PermissionKey]='{0}'
	)", hasPermissionKey.Trim().ToSafeSql());
            }

            totalCount = SqlHelper.ExecuteScalar<int>(DbInstance.OnlyRead, "SELECT TOP 1 COUNT(*)" + where);

            return SqlHelper.ExecuteEntityList<StaffRole>(DbInstance.OnlyRead, ("SELECT role.*, ROW_NUMBER() OVER(ORDER BY role.[Name] ASC) AS [RowNo]" + where).ToPagerSql(pageIndex, pageSize));
        }

        public Guid Create(string name, string desc)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new BizException("角色名不能为空！");
            }
            name = name.Trim();
            if (name.Length > 256)
            {
                throw new BizException("角色名不能超256个字符！");
            }
            string sql = @"
IF NOT EXISTS (SELECT TOP 1 1 FROM [FlyBase].[dbo].[StaffRole] WITH(NOLOCK) WHERE [Name]=@RoleName)
BEGIN
	INSERT INTO [FlyBase].[dbo].[StaffRole]
    (
	    [Id]
	    ,[Name]
	    ,[Description]
	    ,[CreatedOn]
	    ,[CreatedBy]
    )
	VALUES
	(
		@RoleId
		,@RoleName
		,@Description
		,GETDATE()
        ,@operatorId
	)
END";
            Guid roleId = Guid.NewGuid();
            int x = SqlHelper.ExecuteNonQuery(DbInstance.CanWrite, sql, new
            {
                RoleId = roleId,
                RoleName = name,
                Description = desc,
                operatorId = ContextManager.Current.UserId
            });
            if (x <= 0)
            {
                throw new BizException("已经存在了角色名为'{0}'的角色！", name);
            }
            RemoveAllRoleCache();
            return roleId;
        }

        public void Update(Guid roleId, string name, string desc)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new BizException("角色名不能为空！");
            }
            name = name.Trim();
            if (name.Length > 256)
            {
                throw new BizException("角色名不能超256个字符！");
            }
            var r = Get(roleId);
            if (r == null)
            {
                throw new BizException("该角色已经不存在了");
            }
            string sql = @"
IF NOT EXISTS (SELECT TOP 1 1 FROM [FlyBase].[dbo].[StaffRole] WITH(NOLOCK) WHERE [Name]=@RoleName AND [Id]<>@RoleId)
BEGIN
	UPDATE TOP (1)
		[FlyBase].[dbo].[StaffRole]
	SET
	    [Name]=@RoleName
        ,[Description]=@Description
        ,[UpdatedOn]=GETDATE()
        ,[UpdatedBy]=@operatorId
	WHERE
		[Id]=@RoleId
END";
            int x = SqlHelper.ExecuteNonQuery(DbInstance.CanWrite, sql, new
            {
                RoleId = roleId,
                RoleName = name,
                Description = desc,
                operatorId = ContextManager.Current.UserId
            });
            if (x <= 0)
            {
                throw new BizException("已经存在了其它的名为'{0}'的角色！", name);
            }
            RemoveAllRoleCache();
        }

        public void Delete(Guid roleId)
        {
            var r = Get(roleId);
            if (r == null)
            {
                throw new BizException("该角色已经不存在了");
            }
            var userIds = GetUsers(roleId);
            if (userIds != null && userIds.Count > 0)
            {
                throw new BizException("对不起，此角色下还有用户关联，所以不能删除。请先将此角色的所有用户都取消关联后，再来删除此角色");
            }
            string sql = @"
DELETE TOP (1) FROM
	[FlyBase].[dbo].[StaffRole]
WHERE
	[Id]=@roleId
	AND NOT EXISTS
	(
		SELECT TOP 1
			1
		FROM
			[FlyBase].[dbo].[StaffUserInRole] WITH(NOLOCK)
		WHERE
			[RoleId]=[Id]
	)
IF @@ROWCOUNT>0
BEGIN
	DELETE FROM [FlyBase].[dbo].[StaffRolePermission] WHERE [RoleId]=@roleId
END";
            int x = SqlHelper.ExecuteNonQuery(DbInstance.CanWrite, sql);
            if (x <= 0)
            {
                throw new BizException("对不起，此角色下还有用户关联，所以不能删除。请先将此角色的所有用户都取消关联后，再来删除此角色");
            }
            RemoveAllRoleCache();
            string key = "r_permission_" + roleId;
            Cache.RemoveFromLocalCache(key);
        }

        public void SetPermissionKeys(Guid roleId, IEnumerable<string> permissionKeys)
        {
            var r = Get(roleId);
            if (r == null)
            {
                throw new BizException("该角色已经不存在了");
            }
            StringBuilder sb = new StringBuilder();
            if (permissionKeys != null && permissionKeys.Count() > 0)
            {
                foreach (string key in permissionKeys)
                {
                    if (string.IsNullOrWhiteSpace(key))
                    {
                        continue;
                    }

                    if (sb.Length > 0)
                    {
                        sb.Append(@"
UNION ALL");
                    }

                    sb.AppendFormat(@"
SELECT
    '{0}' AS [RoleId]
    ,'{1}' AS [PermissionKey]
    ,GETDATE() AS [CreatedOn]
    ,'{2}' AS [CreatedBy]", roleId, key.Trim().ToSafeSql(), ContextManager.Current.UserId);
                }
            }

            if (sb.Length > 0)
            {
                sb.Insert(0, @"
INSERT INTO
    [FlyBase].[dbo].[StaffRolePermission]
(
	[RoleId]
	,[PermissionKey]
    ,[CreatedOn]
    ,[CreatedBy]
)");
            }

            // 先删除原来的
            sb.Insert(0, string.Format(@"
DELETE FROM
    [FlyBase].[dbo].[StaffRolePermission]
WHERE
	[RoleId]='{0}'", roleId));

            int x = SqlHelper.ExecuteNonQuery(DbInstance.CanWrite, sb.ToString());
            if (x > 0)
            {
                string key = "r_permission_" + roleId;
                Cache.RemoveFromLocalCache(key);
            }
        }

        public IList<string> GetPermissionKeys(Guid roleId)
        {
            return GetPermissionKeys(new Guid[] { roleId });
        }

        public IList<string> GetPermissionKeys(IEnumerable<Guid> roleIds)
        {
            var rst = GetPermissionKeysForEachRole(roleIds);
            return rst.SelectMany(x => x.Value).Distinct().ToList();
        }

        public IDictionary<Guid, IList<string>> GetPermissionKeysForEachRole(IEnumerable<Guid> roleIds)
        {
            if (roleIds == null || roleIds.Count() <= 0)
            {
                return new Dictionary<Guid, IList<string>>(0);
            }
            roleIds = roleIds.Distinct();
            int c = roleIds.Count();
            Dictionary<Guid, IList<string>> rst = new Dictionary<Guid, IList<string>>(c * 2);
            List<Guid> noCacheIdList = new List<Guid>(c);
            foreach (var roleId in roleIds)
            {
                string key = "r_permission_" + roleId;
                var t = Cache.GetLocalCache(key) as List<string>;
                if (t != null)
                {
                    rst.Add(roleId, t);
                }
                else
                {
                    noCacheIdList.Add(roleId);
                }
            }

            if (noCacheIdList.Count > 0)
            {
                string str = string.Join("','", noCacheIdList);
                string sql = string.Format(@"
SELECT
	[RoleId]
	,[PermissionKey]
FROM
	[FlyBase].[dbo].[StaffRolePermission] WITH(NOLOCK)
WHERE
	[RoleId] IN ('{0}')", str);
                var dic = new Dictionary<Guid, List<string>>(noCacheIdList.Count() * 2);
                SqlHelper.ExecuteReaderAll(reader =>
                {
                    Guid roleId = reader.Field<Guid>("RoleId");
                    string permissionKey = reader.Field<string>("PermissionKey");

                    List<string> tmp;
                    if (dic.TryGetValue(roleId, out tmp))
                    {
                        if (tmp.Contains(permissionKey) == false)
                        {
                            tmp.Add(permissionKey);
                        }
                    }
                    else
                    {
                        dic.Add(roleId, new List<string> { permissionKey });
                    }
                }, DbInstance.OnlyRead, sql);
                foreach (var rid in noCacheIdList)
                {
                    if (dic.ContainsKey(rid) == false)
                    {
                        dic.Add(rid, new List<string>(0));
                    }
                }
                if (dic.Count > 0)
                {
                    foreach (var entry in dic)
                    {
                        string key = "r_permission_" + entry.Key;
                        Cache.SetLocalCache(key, entry.Value, true, 120);
                        rst.Add(entry.Key, entry.Value);
                    }
                }
            }
            return rst;
        }

        public void SetUsers(Guid roleId, IEnumerable<Guid> userIds)
        {
            var r = Get(roleId);
            if (r == null)
            {
                throw new BizException("该角色已经不存在了");
            }
            StringBuilder sb = new StringBuilder();
            if (userIds != null && userIds.Count() > 0)
            {
                foreach (Guid uid in userIds)
                {
                    if (sb.Length > 0)
                    {
                        sb.Append(@"
UNION ALL");
                    }
                    sb.AppendFormat(@"
SELECT '{0}' AS [UserId] ,'{1}' AS [RoleId]", uid, roleId);
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
    [RoleId]='{0}'", roleId));

            var oldUserIdList = GetUsers(roleId);
            int x = SqlHelper.ExecuteNonQuery(DbInstance.CanWrite, sb.ToString());
            if (x > 0)
            {
                // 清除原来的和新的用户的角色缓存
                foreach (Guid uid in oldUserIdList.Union(userIds).Distinct())
                {
                    Cache.RemoveFromLocalCache("roles_" + uid);
                }
                Cache.RemoveFromLocalCache("users_" + roleId);
            }
        }

        public IList<Guid> GetUsers(Guid roleId)
        {
            //return Cache.GetWithLocalCache<List<Guid>>("users_" + roleId, () =>
            //{
            //    string sql = "SELECT [UserId] FROM [FlyBase].[dbo].[StaffUserInRole] WITH(NOLOCK) WHERE [RoleId]=@roleId";
            //    return SqlHelper.ExecuteList<Guid>(r => r.GetGuid(0), DbInstance.OnlyRead, sql, new { roleId });
            //}, true, 120);
            var dic = GetUsers(new Guid[] { roleId });
            if (dic == null || dic.Count <= 0)
            {
                return new List<Guid>(0);
            }
            return dic.First().Value;
        }

        public IDictionary<Guid, IList<Guid>> GetUsers(IEnumerable<Guid> roleIds)
        {
            if (roleIds == null || roleIds.Count() <= 0)
            {
                return new Dictionary<Guid, IList<Guid>>(0);
            }
            roleIds = roleIds.Distinct();
            int c = roleIds.Count();
            Dictionary<Guid, IList<Guid>> rst = new Dictionary<Guid, IList<Guid>>(c * 2);
            List<Guid> noCacheIdList = new List<Guid>(c);
            foreach (var roleId in roleIds)
            {
                string key = "users_" + roleId;
                var t = Cache.GetLocalCache(key) as List<Guid>;
                if (t != null)
                {
                    rst.Add(roleId, t);
                }
                else
                {
                    noCacheIdList.Add(roleId);
                }
            }

            if (noCacheIdList.Count > 0)
            {
                string str = string.Join("','", noCacheIdList);
                string sql = string.Format(@"SELECT [RoleId], [UserId] FROM [FlyBase].[dbo].[StaffUserInRole] WITH(NOLOCK) WHERE [RoleId] IN ('{0}')", str);
                var dic = new Dictionary<Guid, List<Guid>>(noCacheIdList.Count() * 2);
                SqlHelper.ExecuteReaderAll(reader =>
                {
                    Guid roleId = reader.Field<Guid>("RoleId");
                    Guid userId = reader.Field<Guid>("UserId");

                    List<Guid> tmp;
                    if (dic.TryGetValue(roleId, out tmp))
                    {
                        if (tmp.Contains(userId) == false)
                        {
                            tmp.Add(userId);
                        }
                    }
                    else
                    {
                        dic.Add(roleId, new List<Guid> { userId });
                    }
                }, DbInstance.OnlyRead, sql);
                foreach (var rid in noCacheIdList)
                {
                    if (dic.ContainsKey(rid) == false)
                    {
                        dic.Add(rid, new List<Guid>(0));
                    }
                }
                if (dic.Count > 0)
                {
                    foreach (var entry in dic)
                    {
                        string key = "users_" + entry.Key;
                        Cache.SetLocalCache(key, entry.Value, true, 120);
                        rst.Add(entry.Key, entry.Value);
                    }
                }
            }
            return rst;
        }
    }
}
