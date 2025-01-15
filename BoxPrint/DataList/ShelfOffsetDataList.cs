using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

/// 190619 Shelf Offset 자료 구조 추가
/// 
namespace BoxPrint.DataList
{

    /// <summary>
    /// Shelf offset 자료 구조
    /// </summary>
    [XmlRoot("ShelfOffsetDataListItem")]
    public class ShelfOffsetDataListItem
    {
        [XmlAttribute("number")]
        public int number { get; set; }

        // 수정
        [XmlAttribute("TagName")]
        public string TagName { get; set; }

        [XmlAttribute("OffSet")]
        public int OffSet { get; set; }

        /// <summary>
        ///$ XML 개선 : XmlElement
        /// </summary>
        /// <returns></returns>
        public static System.Xml.XmlElement GetXmlElement(System.Xml.XmlDocument xmldoc, ShelfOffsetDataListItem TeachingListItem)
        {
            System.Xml.XmlElement xmlElem = xmldoc.CreateElement(TeachingListItem.GetType().Name);
            System.Xml.XmlAttribute xmlAttr = null;

            // Mode
            xmlAttr = xmldoc.CreateAttribute(PropertyExtension.ExtractPropertyName(() => TeachingListItem.number));
            xmlAttr.Value = TeachingListItem.number.ToString();
            xmlElem.Attributes.Append(xmlAttr);

            // X z 구분
            xmlAttr = xmldoc.CreateAttribute(PropertyExtension.ExtractPropertyName(() => TeachingListItem.TagName));
            xmlAttr.Value = TeachingListItem.TagName.ToString();
            xmlElem.Attributes.Append(xmlAttr);

            // Mode
            xmlAttr = xmldoc.CreateAttribute(PropertyExtension.ExtractPropertyName(() => TeachingListItem.OffSet));
            xmlAttr.Value = TeachingListItem.OffSet.ToString();
            xmlElem.Attributes.Append(xmlAttr);

            return xmlElem;
        }
    }

    [XmlRoot("ShelfOffsetDataListList")]
    public class ShelfOffsetDataList : ICollection<ShelfOffsetDataListItem>
    {
        private List<ShelfOffsetDataListItem> _items = new List<ShelfOffsetDataListItem>();
        public List<ShelfOffsetDataListItem> Items
        {
            get
            {
                return _items;
            }
        }

        public ShelfOffsetDataListItem this[int ModeID]
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


        public static void Serialize(string fileName, ShelfOffsetDataList list)
        {
            string path = System.IO.Path.GetDirectoryName(fileName);
            if (!System.IO.Directory.Exists(path))
                System.IO.Directory.CreateDirectory(path);

            lock (typeof(ShelfOffsetDataListItem))
            {
                #region // 예외 처리 추가
                FileStream fs = null;
                try
                {
                    XmlSerializer xmlSer = new XmlSerializer(typeof(ShelfOffsetDataList));
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

        public static ShelfOffsetDataList Deserialize(string fileName)
        {
            lock (typeof(ShelfOffsetDataListItem))
            {
                if (System.IO.File.Exists(fileName))
                {
                    XmlSerializer xmlSer = new XmlSerializer(typeof(ShelfOffsetDataList));
                    FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.None);
                    ShelfOffsetDataList nDataList = (ShelfOffsetDataList)xmlSer.Deserialize(fs);
                    fs.Close();
                    return nDataList;
                }
                else
                {
                    ShelfOffsetDataList nDataList = new ShelfOffsetDataList();
                    return nDataList;
                }
            }
        }

        public static void SerializeListX(string fileName, ShelfOffsetDataList list)
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
                    XmlElement xmlTeachingListItem = ShelfOffsetDataListItem.GetXmlElement(xmldoc, item);
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

        public void Add(ShelfOffsetDataListItem item)
        {
            _items.Add(item);
        }

        public void Add(int number, int offset, string TagName)
        {
            ShelfOffsetDataListItem item = new ShelfOffsetDataListItem();
            item.number = number;
            item.TagName = TagName;
            item.OffSet = offset;

            _items.Add(item);
        }

        public void Clear()
        {
            _items.Clear();
        }

        public bool Contains(ShelfOffsetDataListItem item)
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

        public void CopyTo(ShelfOffsetDataListItem[] array, int arrayIndex)
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

        public bool Remove(ShelfOffsetDataListItem item)
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

        public IEnumerator<ShelfOffsetDataListItem> GetEnumerator()
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
