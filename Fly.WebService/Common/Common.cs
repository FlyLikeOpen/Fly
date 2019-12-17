using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml.Serialization;

namespace IBB360.WMSLocal.WebService.Common
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

        public T GetData<T>(SerializerType serializerType)
        {
            if (Content == null)
            {
                return default(T);
            }
            if (serializerType == SerializerType.BinaryBase64)
            {
                return SerializationUtility.BinaryDeserialize<T>(Content);
            }
            if (serializerType == SerializerType.Json)
            {
                return SerializationUtility.JsonDeserialize<T>(Content);
            }
            return SerializationUtility.XmlDeserialize<T>(Content);
        }
    }

    public enum SerializerType
    {
        Xml = 0,
        Json = 1,
        BinaryBase64 = 2
    }

    internal static class SerializationUtility
    {
        #region Xml序列化

        public static string ToXmlString(this object serialObject)
        {
            return XmlSerialize(serialObject);
        }

        public static string XmlSerialize(object serialObject)
        {
            if (serialObject == null)
            {
                return string.Empty;
            }
            StringBuilder sb = new StringBuilder();
            XmlSerializer ser = new XmlSerializer(serialObject.GetType());
            using (TextWriter writer = new StringWriter(sb))
            {
                ser.Serialize(writer, serialObject);
                return writer.ToString();
            }
        }

        public static object XmlDeserialize(string str, Type type)
        {
            if (str == null || str.Trim().Length <= 0)
            {
                return null;
            }
            XmlSerializer mySerializer = new XmlSerializer(type);
            using (TextReader reader = new StringReader(str))
            {
                return mySerializer.Deserialize(reader);
            }
        }

        public static T XmlDeserialize<T>(string str)
        {
            return (T)XmlDeserialize(str, typeof(T));
        }

        #endregion Xml序列化

        #region 二进制序列化

        public static string ToBinaryBase64String(this object serialObject)
        {
            return BinarySerialize(serialObject);
        }

        public static string BinarySerialize(object serialObject)
        {
            if (serialObject == null)
            {
                return string.Empty;
            }
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(ms, serialObject);
                ms.Position = 0;
                return Convert.ToBase64String(ms.ToArray());
            }
        }

        public static object BinaryDeserialize(string str)
        {
            if (str == null || str.Trim().Length <= 0)
            {
                return null;
            }
            using (MemoryStream ms = new MemoryStream(Convert.FromBase64String(str)))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                return formatter.Deserialize(ms);
            }
        }

        public static T BinaryDeserialize<T>(string str)
        {
            return (T)BinaryDeserialize(str);
        }

        #endregion 二进制序列化

        #region Json序列化

        public static string ToJsonString(this object serialObject)
        {
            return JsonSerialize(serialObject);
        }

        public static string JsonSerialize(object serialObject)
        {
            if (serialObject == null)
            {
                return string.Empty;
            }

            using (MemoryStream stream = new MemoryStream())
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(serialObject.GetType());
                serializer.WriteObject(stream, serialObject);
                byte[] s = stream.ToArray();
                return Encoding.UTF8.GetString(s, 0, s.Length);
            }
        }

        public static object JsonDeserialize(string str, Type type)
        {
            if (str == null || str.Trim().Length <= 0)
            {
                return null;
            }
            using (Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(str)))
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(type);
                return serializer.ReadObject(stream);
            }
        }

        public static T JsonDeserialize<T>(string str)
        {
            return (T)JsonDeserialize(str, typeof(T));
        }

        #endregion Json序列化
    }
}
