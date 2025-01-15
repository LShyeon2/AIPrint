using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;

namespace BoxPrint.DataList
{
    public static class PropertyExtension
    {
        public static List<String> GetXMLSerializableProperties<T>(T t)
        {
            List<String> serialProperties = new List<string>();
            Type typeclass = typeof(T);
            Type[] types = typeclass.Assembly.GetTypes();
            foreach (Type type in types)
            {
                if (type.IsDefined(typeof(SerializableAttribute), false))
                {
                    PropertyInfo[] properties = type.GetProperties();
                    foreach (PropertyInfo properpty in properties)
                    {
                        if (!properpty.IsDefined(typeof(XmlIgnoreAttribute), false))
                            serialProperties.Add(properpty.Name);
                    }
                }
            }
            return serialProperties;
        }

        /// <summary>
        /// None System Type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static Type[] GetNonSystemTypes<T>()
        {
            var systemTypes = typeof(T).GetProperties().Select(t => t.PropertyType).Where(t => t.Namespace == "System");
            return typeof(T).GetProperties().Select(t => t.PropertyType).Except(systemTypes).ToArray();
        }
        public static string ExtractPropertyName<T>(System.Linq.Expressions.Expression<System.Func<T>> propertyExpression)
        {
            if (propertyExpression == null)
            {
                throw new System.ArgumentNullException();
            }
            var memberExpression = propertyExpression.Body as System.Linq.Expressions.MemberExpression;
            if (memberExpression == null)
            {
                throw new System.ArgumentException();
            }
            var property = memberExpression.Member as System.Reflection.PropertyInfo;
            if (property == null)
            {
                throw new System.ArgumentException();
            }
            var getMethod = property.GetGetMethod(true);
            if (getMethod.IsStatic)
            {
                throw new System.ArgumentException();
            }
            return memberExpression.Member.Name;
        }

        public static string DataSerialize<T>(this T value)
        {
            if (value == null)
            {
                throw new ArgumentException("Value is Null.", nameof(value));
            }
            try
            {
                var xmlserializer = new XmlSerializer(typeof(T));
                var stringWriter = new StringWriter();
                using (var writer = XmlWriter.Create(stringWriter))
                {
                    xmlserializer.Serialize(writer, value);
                    return stringWriter.ToString();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred", ex);
            }
        }
    }
}
