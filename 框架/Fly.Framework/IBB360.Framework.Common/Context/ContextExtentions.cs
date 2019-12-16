using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fly.Framework.Common
{
    public static class ContextExtentions
    {
        public static bool IsAdmin(this IContext context)
        {
            object v = context["IsAdmin"];
            if(v == null)
            {
                return false;
            }
            return string.Equals(v.ToString(), "true", StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
