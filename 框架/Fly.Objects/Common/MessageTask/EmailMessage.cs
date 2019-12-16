using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fly.Objects.Common.MessageTask
{
    public class EmailMessage
    {
        public string FromAddress { get; set; }

        public string FromDisplay { get; set; }

        public string Subject { get; set; }

        public string Body { get; set; }

        public bool IsHtml { get; set; }
    }
}
