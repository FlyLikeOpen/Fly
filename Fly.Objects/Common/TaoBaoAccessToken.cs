using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fly.Objects.Common
{
    public class TaoBaoAccessToken
    {
        public string access_token { get; set; }

        public string token_type { get; set; }

        public long expires_in { get; set; }

        public string refresh_token { get; set; }

        public long re_expires_in { get; set; }

        public string taobao_user_nick { get; set; }

        public string taobao_user_id { get; set; }

        public string ExpiredTime { get; set; }
    }
}
