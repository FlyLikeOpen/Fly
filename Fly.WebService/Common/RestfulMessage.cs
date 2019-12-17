using Fly.Framework.Common;

namespace Fly.WebService.Common
{
    public class RestfulMessage
    {
        public string MessageID { get; set; }

        public string Subject { get; set; }

        public string SubscriberID
        {
            get;
            set;
        }

        public string Content { get; set; }

        public T GetData<T>()
        {
            if (Content == null)
            {
                return default(T);
            }
            return Serialization.XmlDeserialize<T>(Content);
        }
    }
}