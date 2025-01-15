using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;


namespace BoxPrint.DataList
{

    /// <summary>
    /// P Mac 자료 구조
    /// </summary>
    [XmlRoot("PMacDataListItem")]
    public class PMacDataListItem
    {
        [XmlAttribute("number")]
        public int number { get; set; }

        // 수정
        [XmlAttribute("TagName")]
        public string TagName { get; set; }

        // 수정
        [XmlAttribute("Definition")]
        public string Definition { get; set; }

        // 수정
        [XmlAttribute("Description")]
        public string Description { get; set; }

        // 수정
        [XmlAttribute("Note")]
        public string Note { get; set; }

        // 수정
        [XmlAttribute("DataType")]
        public string DataType { get; set; }

        /// <summary>
        ///$ XML 개선 : XmlElement
        /// </summary>
        /// <returns></returns>
        public static System.Xml.XmlElement GetXmlElement(System.Xml.XmlDocument xmldoc, PMacDataListItem TeachingListItem)
        {
            System.Xml.XmlElement xmlElem = xmldoc.CreateElement(TeachingListItem.GetType().Name);
            System.Xml.XmlAttribute xmlAttr = null;

            // Mode
            xmlAttr = xmldoc.CreateAttribute(PropertyExtension.ExtractPropertyName(() => TeachingListItem.number));
            xmlAttr.Value = TeachingListItem.number.ToString();
            xmlElem.Attributes.Append(xmlAttr);

            xmlAttr = xmldoc.CreateAttribute(PropertyExtension.ExtractPropertyName(() => TeachingListItem.TagName));
            xmlAttr.Value = TeachingListItem.TagName.ToString();
            xmlElem.Attributes.Append(xmlAttr);

            xmlAttr = xmldoc.CreateAttribute(PropertyExtension.ExtractPropertyName(() => TeachingListItem.Definition));
            xmlAttr.Value = TeachingListItem.Definition.ToString();
            xmlElem.Attributes.Append(xmlAttr);

            xmlAttr = xmldoc.CreateAttribute(PropertyExtension.ExtractPropertyName(() => TeachingListItem.Description));
            xmlAttr.Value = TeachingListItem.Description.ToString();
            xmlElem.Attributes.Append(xmlAttr);

            xmlAttr = xmldoc.CreateAttribute(PropertyExtension.ExtractPropertyName(() => TeachingListItem.Note));
            xmlAttr.Value = TeachingListItem.Note.ToString();
            xmlElem.Attributes.Append(xmlAttr);

            xmlAttr = xmldoc.CreateAttribute(PropertyExtension.ExtractPropertyName(() => TeachingListItem.DataType));
            xmlAttr.Value = TeachingListItem.DataType.ToString();
            xmlElem.Attributes.Append(xmlAttr);

            return xmlElem;
        }
    }

    [XmlRoot("PMacDataListList")]
    public class PMacDataList : ICollection<PMacDataListItem>
    {
        private List<PMacDataListItem> _items = new List<PMacDataListItem>();
        public List<PMacDataListItem> Items
        {
            get
            {
                return _items;
            }
        }

        public PMacDataListItem this[int ModeID]
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

        public static void Serialize(string fileName, PMacDataList list)
        {
            string path = System.IO.Path.GetDirectoryName(fileName);
            if (!System.IO.Directory.Exists(path))
                System.IO.Directory.CreateDirectory(path);

            lock (typeof(PMacDataListItem))
            {
                #region // File 생성 방식 변경 했었으나, 원례 방식대로 사용하며, 예외 처리 추가
                FileStream fs = null;
                try
                {
                    XmlSerializer xmlSer = new XmlSerializer(typeof(PMacDataList));
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

            }
        }

        public static PMacDataList Deserialize(string fileName)
        {
            lock (typeof(PMacDataListItem))
            {
                if (System.IO.File.Exists(fileName))
                {
                    XmlSerializer xmlSer = new XmlSerializer(typeof(PMacDataList));
                    FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.None);
                    PMacDataList nDataList = (PMacDataList)xmlSer.Deserialize(fs);
                    fs.Close();
                    return nDataList;
                }
                else
                {
                    PMacDataList nDataList = new PMacDataList();
                    return nDataList;
                }
            }
        }

        public static void SerializeListX(string fileName, PMacDataList list)
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
                    XmlElement xmlTeachingListItem = PMacDataListItem.GetXmlElement(xmldoc, item);
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

        public void Add(PMacDataListItem item)
        {
            _items.Add(item);
        }

        public void Add(int number, string TagName, string Definition, string Description, string Note, string DataType)
        {
            PMacDataListItem item = new PMacDataListItem();
            item.number = number;
            item.TagName = TagName;
            item.Definition = Definition;
            item.Description = Description;
            item.Note = Note;
            item.DataType = DataType;

            _items.Add(item);
        }

        public void Clear()
        {
            _items.Clear();
        }

        public bool Contains(PMacDataListItem item)
        {
            return _items.Contains(item);
        }

        public bool ContainKey(string Tagname)
        {
            bool result = false;
            if (_items != null && _items.Where(r => r.TagName == Tagname).Count() > 0)
            {
                result = true;
            }
            return result;
        }

        public void CopyTo(PMacDataListItem[] array, int arrayIndex)
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

        public bool Remove(PMacDataListItem item)
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

        public IEnumerator<PMacDataListItem> GetEnumerator()
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
