using Fly.Objects.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fly.APIs.Common
{
    public interface IStaffPermissionKeyApi
    {
        IList<PermissionKey> GetAllPermissionKeys();
    }

    public class PermissionKey
    {
        public string Text { get; set; }

        public string Value { get; set; }

        public List<PermissionKey> PermissionKeys { get; set; }
    }
}
