using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

/// 200302 Alarm List 자료 구조 추가
/// 
namespace BoxPrint.DataList
{

    /// <summary>
    ///Alarm List 자료 구조
    /// </summary>
    [XmlRoot("AlarmDataListItem")]
    public class AlarmDataListItem
    {
        [XmlAttribute("number")]
        public int number { get; set; }

        [XmlAttribute("AlarmName")]
        public string AlarmName { get; set; }

        [XmlAttribute("Code")]
        public int Code { get; set; }

        [XmlAttribute("DEFINITION")]
        public string DEFINITION { get; set; }


        /// <summary>
        ///$ XML 개선 : XmlElement
        /// </summary>
        /// <returns></returns>
        public static System.Xml.XmlElement GetXmlElement(System.Xml.XmlDocument xmldoc, AlarmDataListItem AlarmListItem)
        {
            System.Xml.XmlElement xmlElem = xmldoc.CreateElement(AlarmListItem.GetType().Name);
            System.Xml.XmlAttribute xmlAttr = null;

            // number
            xmlAttr = xmldoc.CreateAttribute(PropertyExtension.ExtractPropertyName(() => AlarmListItem.number));
            xmlAttr.Value = AlarmListItem.number.ToString();
            xmlElem.Attributes.Append(xmlAttr);

            // AlarmName
            xmlAttr = xmldoc.CreateAttribute(PropertyExtension.ExtractPropertyName(() => AlarmListItem.AlarmName));
            xmlAttr.Value = AlarmListItem.AlarmName.ToString();
            xmlElem.Attributes.Append(xmlAttr);

            // Code
            xmlAttr = xmldoc.CreateAttribute(PropertyExtension.ExtractPropertyName(() => AlarmListItem.Code));
            xmlAttr.Value = AlarmListItem.Code.ToString();
            xmlElem.Attributes.Append(xmlAttr);

            // DEFINITION
            xmlAttr = xmldoc.CreateAttribute(PropertyExtension.ExtractPropertyName(() => AlarmListItem.DEFINITION));
            xmlAttr.Value = AlarmListItem.DEFINITION.ToString();
            xmlElem.Attributes.Append(xmlAttr);

            return xmlElem;
        }
    }

    [XmlRoot("AlarmDataListList")]
    public class AlarmDataList : ICollection<AlarmDataListItem>
    {
        private List<AlarmDataListItem> _items = new List<AlarmDataListItem>();
        public List<AlarmDataListItem> Items
        {
            get
            {
                return _items;
            }
        }

        public AlarmDataListItem this[int ModeID]
        {
            get
            {
                if (_items != null && _items.Where(r => r.number == ModeID).Count() > 0)
                {
                    return _items.Where(r => r.number == ModeID).First();
                }
                return null;
            }
        }


        public static void Serialize(string fileName, AlarmDataList list)
        {
            string path = System.IO.Path.GetDirectoryName(fileName);
            if (!System.IO.Directory.Exists(path))
                System.IO.Directory.CreateDirectory(path);

            lock (typeof(AlarmDataListItem))
            {
                #region // 예외 처리 추가
                FileStream fs = null;
                try
                {
                    XmlSerializer xmlSer = new XmlSerializer(typeof(AlarmDataList));
                    fs = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None);
                    xmlSer.Serialize(fs, list);
                    fs.Close();

                    fs.Dispose();
                    fs = null;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("{0} - Exception:{1}", System.Reflection.MethodBase.GetCurrentMethod().Name, ex);
                }
                finally
                {
                    try
                    {
                        if (fs != null) fs.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("{0} - finally Exception:{1}", System.Reflection.MethodBase.GetCurrentMethod().Name, ex);
                    }
                    finally
                    {
                        fs = null;
                    }
                }
                #endregion

                //$ XML 개선.[2014.04.09] : 해당 내역은 비휘발성 데이터라 하기 내역 사용 안함.
                //SerializeListX(fileName, list);
            }
        }

        public static AlarmDataList Deserialize(string fileName)
        {
            lock (typeof(AlarmDataListItem))
            {
                if (System.IO.File.Exists(fileName))
                {
                    XmlSerializer xmlSer = new XmlSerializer(typeof(AlarmDataList));
                    FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.None);
                    AlarmDataList nDataList = (AlarmDataList)xmlSer.Deserialize(fs);
                    fs.Close();
                    return nDataList;
                }
                else
                {
                    AlarmDataList nDataList = new AlarmDataList();
                    return nDataList;
                }
            }
        }

        public static void SerializeListX(string fileName, AlarmDataList list)
        {
            TextWriter sw = null;
            try
            {
                XmlDocument xmldoc = new XmlDocument();

                // xml Declaration
                XmlDeclaration decNode = xmldoc.CreateXmlDeclaration("1.0", "UTF-8", null);
                XmlElement root = xmldoc.DocumentElement;

                xmldoc.InsertBefore(decNode, root);

                XmlElement xmlnDataList = xmldoc.CreateElement(@"nDataList");
                xmlnDataList.SetAttribute("xmlns:xsi", "http://www.w3.org/2001/XMLSchema-instance");
                xmlnDataList.SetAttribute("xmlns:xsd", "http://www.w3.org/2001/XMLSchema");
                xmldoc.AppendChild(xmlnDataList);

                foreach (var item in list)
                {
                    XmlElement xmlTeachingListItem = AlarmDataListItem.GetXmlElement(xmldoc, item);
                    xmlnDataList.AppendChild(xmlTeachingListItem);
                }

                using (sw = new StreamWriter(fileName, false, System.Text.Encoding.UTF8))
                {
                    xmldoc.Save(sw);
                }

                xmldoc = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} - Exception:{1}", System.Reflection.MethodBase.GetCurrentMethod().Name, ex);
            }
            finally
            {
                try
                {
                    if (sw != null) sw.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("{0} - finally Exception:{1}", System.Reflection.MethodBase.GetCurrentMethod().Name, ex);
                }
                finally
                {
                    sw = null;
                }
            }
        }

        #region ICollection<TeachingList> Members

        public void Add(AlarmDataListItem item)
        {
            _items.Add(item);
        }

        public void Add(int number, string AlarmName, int Code, string DEFINITION)
        {
            AlarmDataListItem item = new AlarmDataListItem();
            item.number = number;
            item.AlarmName = AlarmName;
            item.Code = Code;
            item.DEFINITION = DEFINITION;

            _items.Add(item);
        }

        public void Clear()
        {
            _items.Clear();
        }

        public bool Contains(AlarmDataListItem item)
        {
            return _items.Contains(item);
        }

        public bool ContainKey(string AlarmName)
        {
            bool result = false;
            if (_items != null && _items.Where(r => r.AlarmName == AlarmName).Count() > 0)
            {
                result = true;
            }
            return result;
        }

        public void CopyTo(AlarmDataListItem[] array, int arrayIndex)
        {
            _items.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return _items.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(AlarmDataListItem item)
        {
            return _items.Remove(item);
        }

        public bool Remove(int number)
        {
            for (int i = _items.Count - 1; i >= 0; i--)
            {
                if (Items[i].number == number)
                {
                    _items.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region IEnumerable<TeachingList> Members

        public IEnumerator<AlarmDataListItem> GetEnumerator()
        {
            return _items.GetEnumerator();
        }
        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _items.GetEnumerator();
        }
        #endregion
    }
}
