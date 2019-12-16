using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fly.Objects.Common
{
    public class StaffUserTempPermission
    {
        public Guid UserId { get; set; }

        public string PermissionKey { get; set; }

        public DateTime FromTime { get; set; }

        public DateTime ToTime { get; set; }

        public DateTime UpdatedOn { get; set; }

        public Guid UpdatedBy { get; set; }

        public bool Valid
        {
            get
            {
                return string.IsNullOrWhiteSpace(PermissionKey) == false
                    && FromTime <= DateTime.Now
                    && DateTime.Now <= ToTime;
            }
        }
    }
}
