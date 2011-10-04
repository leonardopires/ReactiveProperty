﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;

namespace Codeplex.Reactive.Serialization
{
    public static class SerializeHelper
    {
        static IEnumerable<PropertyInfo> GetIValueProperties(object target)
        {
            return target.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(pi => pi.PropertyType == typeof(IValue));
        }

        public static string PackReactivePropertyValue(object target)
        {
            var values = GetIValueProperties(target)
                .ToDictionary(pi => pi.Name, pi =>
                {
                    var ivalue = (IValue)pi.GetValue(target, null);
                    return (ivalue != null) ? ivalue.Value : null;
                });

            var sb = new StringBuilder();
            var serializer = new DataContractSerializer(values.GetType());
            using (var writer = XmlWriter.Create(sb))
            {
                serializer.WriteObject(writer, values);
            }
            return sb.ToString();
        }

        public static void UnpackReactivePropertyValue(object target, string packedData)
        {
            Dictionary<string, object> values;
            var serializer = new DataContractSerializer(typeof(Dictionary<string, object>));
            using (var sr = new StringReader(packedData))
            using (var reader = XmlReader.Create(sr))
            {
                values = (Dictionary<string, object>)serializer.ReadObject(reader);
            }

            var query = GetIValueProperties(target)
                .Select(pi =>
                {
                    var attr = (DataMemberAttribute)pi.GetCustomAttributes(typeof(DataMemberAttribute), false).FirstOrDefault();
                    var order = (attr != null) ? attr.Order : int.MinValue;
                    return new { pi, order };
                })
                .OrderBy(a => a.order)
                .ThenBy(a => a.pi.Name);

            foreach (var item in query)
            {
                object value;
                if (values.TryGetValue(item.pi.Name, out value))
                {
                    var ivalue = (IValue)item.pi.GetValue(target, null);
                    if (ivalue != null) ivalue.Value = value;
                }
            }
        }
    }
}