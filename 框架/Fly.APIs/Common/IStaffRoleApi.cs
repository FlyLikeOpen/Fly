using Fly.Objects.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fly.APIs.Common
{
    public interface IStaffRoleApi : IObjectGetter<StaffRole>
    {
        void RemoveAllRoleCache();

        void RemoveAllRolePermissionCache();

        IList<StaffRole> GetAllRoles();

        IList<StaffRole> Query(string name, Guid? containsUserId, string hasPermissionKey,
            int pageIndex, int pageSize, out int totalCount);

        Guid Create(string name, string desc);

        void Update(Guid roleId, string name, string desc);

        void Delete(Guid roleId);

        void SetPermissionKeys(Guid roleId, IEnumerable<string> permissionKeys);

        IList<string> GetPermissionKeys(Guid roleId);

        IList<string> GetPermissionKeys(IEnumerable<Guid> roleIds);

        void SetUsers(Guid roleId, IEnumerable<Guid> userIds);

        IList<Guid> GetUsers(Guid roleId);

        IDictionary<Guid, IList<Guid>> GetUsers(IEnumerable<Guid> roleIds);
    }
}
