﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Fly.Framework.Common
{
    /*
    // Read and Access
    // Parse (from JsonString to DynamicJson)
    var json = DynamicJson.Parse(@"{""foo"":""json"", ""bar"":100, ""nest"":{ ""foobar"":true } }");

    var r1 = json.foo; // "json" - dynamic(string)
    var r2 = json.bar; // 100 - dynamic(double)
    var r3 = json.nest.foobar; // true - dynamic(bool)
    var r4 = json["nest"]["foobar"]; // can access indexer

    // Operate

    // Check Defined Peroperty
    // .name() is shortcut of IsDefined("name")
    var b1_1 = json.IsDefined("foo"); // true
    var b2_1 = json.IsDefined("foooo"); // false
    var b1_2 = json.foo(); // true            
    var b2_2 = json.foooo(); // false;

    // Add
    json.Arr = new string[] { "NOR", "XOR" }; // Add Array
    json.Obj1 = new { }; // Add Object
    json.Obj2 = new { foo = "abc", bar = 100 }; // Add and Init

    // Delete
    // ("name") is shortcut of Delete("name")
    json.Delete("foo");
    json.Arr.Delete(0);
    json("bar");
    json.Arr(1);

    // Replace
    json.Obj1 = 5000;

    // Create New JsonObject
    dynamic newjson = new DynamicJson();
    newjson.str = "aaa";
    newjson.obj = new { foo = "bar" };

    // Serialize
    var reJson = json.ToString(); // {"nest":{"foobar":true},"Arr":["XOR"],"Obj1":5000,"Obj2":{"foo":"abc","bar":100}}


    // Enumerate

    // DynamicJson - (IsArray)
    var arrayJson = DynamicJson.Parse(@"[1,10,200,300]");
    foreach (int item in arrayJson)
    {
        Console.WriteLine(item); // 1, 10, 200, 300
    }

    // DynamicJson - (IsObject)
    var objectJson = DynamicJson.Parse(@"{""foo"":""json"",""bar"":100}");
    foreach (KeyValuePair<string, object> item in objectJson)
    {
        Console.WriteLine(item.Key + ":" + item.Value); // foo:json, bar:100
    }


    // Convert/Deserialize

    public class FooBar
    {
        public string foo { get; set; }
        public int bar { get; set; }
    }

    // (type) is shortcut of Deserialize<type>()
    var array1 = arrayJson.Deserialize<int[]>();
    var array2 = (int[])arrayJson;
    int[] array3 = arrayJson;

    // mapping by public property name
    var foobar1 = objectJson.Deserialize<FooBar>();
    var foobar2 = (FooBar)objectJson;
    FooBar foobar3 = objectJson;

    // with linq
    var objectJsonList = DynamicJson.Parse(@"[{""bar"":50},{""bar"":100}]");
    var barSum = ((FooBar[])objectJsonList).Select(fb => fb.bar).Sum(); // 150
    var dynamicWithLinq = ((dynamic[])objectJsonList).Select(d => d.bar);

    // Serialize to JSON from Object

    // Serialize (from Object to JsonString)
    var obj = new
    {
        Name = "Foo",
        Age = 30,
        Address = new
        {
            Country = "Japan",
            City = "Tokyo"
        },
        Like = new[] { "Microsoft", "Xbox" }
    };
    // {"Name":"Foo","Age":30,"Address":{"Country":"Japan","City":"Tokyo"},"Like":["Microsoft","Xbox"]}
    var jsonStringFromObj = DynamicJson.Serialize(obj);

    // [{"foo":"fooooo!","bar":1000},{"foo":"orz","bar":10}]
    var foobar = new FooBar[] { 
            new FooBar { foo = "fooooo!", bar = 1000 }, 
            new FooBar { foo = "orz", bar = 10 } };
    var jsonFoobar = DynamicJson.Serialize(foobar);


    // Notice: CornerCase

    var nestJson = DynamicJson.Parse(@"{""tes"":10,""nest"":{""a"":0}");

    nestJson.nest(); // This equals json.IsDefined("nest")
    nestJson.nest("a"); // This equals json.nest.Delete("a")

    // if name is C#'s reserved word then put prefix "@"
    var json = DynamicJson.Parse(@"{""int"":10,""event"":null}");
    var r1 = json.@int; // 10.0
    var r2 = json.@event; // null
     */
    public class DynamicJson : DynamicObject
    {
        private enum JsonType
        {
            @string, number, boolean, @object, array, @null
        }

        // public static methods

        /// <summary>from JsonSring to DynamicJson</summary>
        public static dynamic Parse(string json)
        {
            try
            {
                return Parse(json, Encoding.Unicode);
            }
            catch (Exception ex)
            {
                Logger.ErrorFormat(true, "Json反解析失败，输入的字符串：{0}\r\nException:\r\n" + ex.ToString(), json);
                throw;
            }
        }

        /// <summary>from JsonSring to DynamicJson</summary>
        public static dynamic Parse(string json, Encoding encoding)
        {
            try
            {
                using (var reader = JsonReaderWriterFactory.CreateJsonReader(encoding.GetBytes(json), XmlDictionaryReaderQuotas.Max))
                {
                    return ToValue(XElement.Load(reader));
                }
            }
            catch (Exception ex)
            {
                Logger.ErrorFormat(true, "Json反解析失败，输入的字符串：{0}\r\nException:\r\n" + ex.ToString(), json);
                throw;
            }
        }

        /// <summary>from JsonSringStream to DynamicJson</summary>
        public static dynamic Parse(Stream stream)
        {
            using (var reader = JsonReaderWriterFactory.CreateJsonReader(stream, XmlDictionaryReaderQuotas.Max))
            {
                return ToValue(XElement.Load(reader));
            }
        }

        /// <summary>from JsonSringStream to DynamicJson</summary>
        public static dynamic Parse(Stream stream, Encoding encoding)
        {
            using (var reader = JsonReaderWriterFactory.CreateJsonReader(stream, encoding, XmlDictionaryReaderQuotas.Max, _ => { }))
            {
                return ToValue(XElement.Load(reader));
            }
        }

        /// <summary>create JsonSring from primitive or IEnumerable or Object({public property name:property value})</summary>
        public static string Serialize(object obj)
        {
            return CreateJsonString(new XStreamingElement("root", CreateTypeAttr(GetJsonType(obj)), CreateJsonNode(obj)));
        }

        // private static methods

        private static dynamic ToValue(XElement element)
        {
            var type = (JsonType)Enum.Parse(typeof(JsonType), element.Attribute("type").Value);
            switch (type)
            {
                case JsonType.boolean:
                    return (bool)element;
                case JsonType.number:
                    return (double)element;
                case JsonType.@string:
                    return (string)element;
                case JsonType.@object:
                case JsonType.array:
                    return new DynamicJson(element, type);
                case JsonType.@null:
                default:
                    return null;
            }
        }

        private static JsonType GetJsonType(object obj)
        {
            if (obj == null) return JsonType.@null;

            switch (Type.GetTypeCode(obj.GetType()))
            {
                case TypeCode.Boolean:
                    return JsonType.boolean;
                case TypeCode.String:
                case TypeCode.Char:
                case TypeCode.DateTime:
                    return JsonType.@string;
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                case TypeCode.SByte:
                case TypeCode.Byte:
                    return JsonType.number;
                case TypeCode.Object:
                    return (obj is IEnumerable) ? JsonType.array : JsonType.@object;
                case TypeCode.DBNull:
                case TypeCode.Empty:
                default:
                    return JsonType.@null;
            }
        }

        private static XAttribute CreateTypeAttr(JsonType type)
        {
            return new XAttribute("type", type.ToString());
        }

        private static object CreateJsonNode(object obj)
        {
            var type = GetJsonType(obj);
            switch (type)
            {
                case JsonType.@string:
                case JsonType.number:
                    return obj;
                case JsonType.boolean:
                    return obj.ToString().ToLower();
                case JsonType.@object:
                    return CreateXObject(obj);
                case JsonType.array:
                    return CreateXArray(obj as IEnumerable);
                case JsonType.@null:
                default:
                    return null;
            }
        }

        private static IEnumerable<XStreamingElement> CreateXArray<T>(T obj) where T : IEnumerable
        {
            return obj.Cast<object>()
                .Select(o => new XStreamingElement("item", CreateTypeAttr(GetJsonType(o)), CreateJsonNode(o)));
        }

        private static IEnumerable<XStreamingElement> CreateXObject(object obj)
        {
            return obj.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(pi => new { Name = pi.Name, Value = pi.GetValue(obj, null) })
                .Select(a => new XStreamingElement(a.Name, CreateTypeAttr(GetJsonType(a.Value)), CreateJsonNode(a.Value)));
        }

        private static string CreateJsonString(XStreamingElement element)
        {
            using (var ms = new MemoryStream())
            using (var writer = JsonReaderWriterFactory.CreateJsonWriter(ms, Encoding.Unicode))
            {
                element.WriteTo(writer);
                writer.Flush();
                return Encoding.Unicode.GetString(ms.ToArray());
            }
        }

        // dynamic structure represents JavaScript Object/Array

        readonly XElement xml;
        readonly JsonType jsonType;

        /// <summary>create blank JSObject</summary>
        public DynamicJson()
        {
            xml = new XElement("root", CreateTypeAttr(JsonType.@object));
            jsonType = JsonType.@object;
        }

        private DynamicJson(XElement element, JsonType type)
        {
            Debug.Assert(type == JsonType.array || type == JsonType.@object);

            xml = element;
            jsonType = type;
        }

        public bool IsObject { get { return jsonType == JsonType.@object; } }

        public bool IsArray { get { return jsonType == JsonType.array; } }

        /// <summary>has property or not</summary>
        public bool IsDefined(string name)
        {
            return IsObject && (xml.Element(name) != null);
        }

        /// <summary>has property or not</summary>
        public bool IsDefined(int index)
        {
            return IsArray && (xml.Elements().ElementAtOrDefault(index) != null);
        }

        /// <summary>delete property</summary>
        public bool Delete(string name)
        {
            var elem = xml.Element(name);
            if (elem != null)
            {
                elem.Remove();
                return true;
            }
            else return false;
        }

        /// <summary>delete property</summary>
        public bool Delete(int index)
        {
            var elem = xml.Elements().ElementAtOrDefault(index);
            if (elem != null)
            {
                elem.Remove();
                return true;
            }
            else return false;
        }

        /// <summary>mapping to Array or Class by Public PropertyName</summary>
        public T Deserialize<T>()
        {
            return (T)Deserialize(typeof(T));
        }

        private object Deserialize(Type type)
        {
            return (IsArray) ? DeserializeArray(type) : DeserializeObject(type);
        }

        private dynamic DeserializeValue(XElement element, Type elementType)
        {
            var value = ToValue(element);
            if (value is DynamicJson)
            {
                value = ((DynamicJson)value).Deserialize(elementType);
            }
            return Convert.ChangeType(value, elementType);
        }

        private object DeserializeObject(Type targetType)
        {
            var result = Activator.CreateInstance(targetType);
            var dict = targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite)
                .ToDictionary(pi => pi.Name, pi => pi);
            foreach (var item in xml.Elements())
            {
                PropertyInfo propertyInfo;
                if (!dict.TryGetValue(item.Name.LocalName, out propertyInfo)) continue;
                var value = DeserializeValue(item, propertyInfo.PropertyType);
                propertyInfo.SetValue(result, value, null);
            }
            return result;
        }

        private object DeserializeArray(Type targetType)
        {
            if (targetType.IsArray) // Foo[]
            {
                var elemType = targetType.GetElementType();
                dynamic array = Array.CreateInstance(elemType, xml.Elements().Count());
                var index = 0;
                foreach (var item in xml.Elements())
                {
                    array[index++] = DeserializeValue(item, elemType);
                }
                return array;
            }
            else // List<Foo>
            {
                var elemType = targetType.GetGenericArguments()[0];
                dynamic list = Activator.CreateInstance(targetType);
                foreach (var item in xml.Elements())
                {
                    list.Add(DeserializeValue(item, elemType));
                }
                return list;
            }
        }

        // Delete
        public override bool TryInvoke(InvokeBinder binder, object[] args, out object result)
        {
            result = (IsArray)
                ? Delete((int)args[0])
                : Delete((string)args[0]);
            return true;
        }

        // IsDefined, if has args then TryGetMember
        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            if (args.Length > 0)
            {
                result = null;
                return false;
            }

            result = IsDefined(binder.Name);
            return true;
        }

        // Deserialize or foreach(IEnumerable)
        public override bool TryConvert(ConvertBinder binder, out object result)
        {
            if (binder.Type == typeof(IEnumerable) || binder.Type == typeof(object[]))
            {
                var ie = (IsArray)
                    ? xml.Elements().Select(x => ToValue(x))
                    : xml.Elements().Select(x => (dynamic)new KeyValuePair<string, object>(x.Name.LocalName, ToValue(x)));
                result = (binder.Type == typeof(object[])) ? ie.ToArray() : ie;
            }
            else
            {
                result = Deserialize(binder.Type);
            }
            return true;
        }

        private bool TryGet(XElement element, out object result)
        {
            if (element == null)
            {
                result = null;
                return false;
            }

            result = ToValue(element);
            return true;
        }

        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
        {
            return (IsArray)
                ? TryGet(xml.Elements().ElementAtOrDefault((int)indexes[0]), out result)
                : TryGet(xml.Element((string)indexes[0]), out result);
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            return (IsArray)
                ? TryGet(xml.Elements().ElementAtOrDefault(int.Parse(binder.Name)), out result)
                : TryGet(xml.Element(binder.Name), out result);
        }

        private bool TrySet(string name, object value)
        {
            var type = GetJsonType(value);
            var element = xml.Element(name);
            if (element == null)
            {
                xml.Add(new XElement(name, CreateTypeAttr(type), CreateJsonNode(value)));
            }
            else
            {
                element.Attribute("type").Value = type.ToString();
                element.ReplaceNodes(CreateJsonNode(value));
            }

            return true;
        }

        private bool TrySet(int index, object value)
        {
            var type = GetJsonType(value);
            var e = xml.Elements().ElementAtOrDefault(index);
            if (e == null)
            {
                xml.Add(new XElement("item", CreateTypeAttr(type), CreateJsonNode(value)));
            }
            else
            {
                e.Attribute("type").Value = type.ToString();
                e.ReplaceNodes(CreateJsonNode(value));
            }

            return true;
        }

        public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value)
        {
            return (IsArray)
                ? TrySet((int)indexes[0], value)
                : TrySet((string)indexes[0], value);
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            return (IsArray)
                ? TrySet(int.Parse(binder.Name), value)
                : TrySet(binder.Name, value);
        }

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            return (IsArray)
                ? xml.Elements().Select((x, i) => i.ToString())
                : xml.Elements().Select(x => x.Name.LocalName);
        }

        /// <summary>Serialize to JsonString</summary>
        public override string ToString()
        {
            // <foo type="null"></foo> is can't serialize. replace to <foo type="null" />
            foreach (var elem in xml.Descendants().Where(x => x.Attribute("type").Value == "null"))
            {
                elem.RemoveNodes();
            }
            return CreateJsonString(new XStreamingElement("root", CreateTypeAttr(jsonType), xml.Elements()));
        }
    }
}