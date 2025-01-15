using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace BoxPrint.GUI.ViewModels
{
    public static class ExtensionMethods
    {
        /// <summary>
        /// DB에 저장하기 위해 DataClass 직렬화
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
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
        /// <summary>
        /// DB에서 가져온 직렬화 데이터를 DataClass로 역직렬화
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="type">저장해야할 데이터의 타입 typeof(저장할데이터 클래스 명)</param>
        /// <returns></returns>
        public static object DataDeserialize<T>(this T value, Type type)
        {
            if (value == null)
            {
                throw new ArgumentException("Value is Null.", nameof(value));
            }
            try
            {
                string xmlString = value.ToString();
                var xmlserializer = new XmlSerializer(type);
                using (var stringReader = new StringReader(xmlString))
                {
                    var v = xmlserializer.Deserialize(stringReader);
                    return v;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred", ex);
            }
        }
        /// <summary>
        /// Perform a deep copy of the object via serialization.
        /// </summary>
        /// <typeparam name="T">The type of object being copied.</typeparam>
        /// <param name="value">The object instance to copy.</param>
        /// <returns>A deep copy of the object.</returns>
        public static T DeepClone<T>(this T value)
        {
            try
            {
                if (!typeof(T).IsSerializable)
                {
                    throw new ArgumentException("The type must be serializable.", nameof(value));
                }

                //Data를 직렬화하여 Xml String 형식으로 변경
                string strvalueSerialize = DataSerialize(value);
                //Xml String을 역직렬화 하여 Data Type으로 변경하여 리턴
                return (T)strvalueSerialize.DataDeserialize(typeof(T));
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
