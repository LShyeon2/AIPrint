using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace BoxPrint.DataList
{
    [XmlRoot("TurnTeachingItem")]
    public class TurnTeachingItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }

        [XmlAttribute("Name")]
        public int Name { get; set; }

        [XmlAttribute("Axis")]
        public int Axis { get; set; }

        [XmlAttribute("TagName")]
        public string TagName { get; set; }

        [XmlAttribute("PositionValue")]
        public int PositionValue { get; set; }

        private bool _IsSelected;
        [XmlIgnore]
        public bool IsSelected
        {
            get
            {
                return _IsSelected;
            }
            set
            {
                _IsSelected = value;
                OnPropertyChanged(new PropertyChangedEventArgs("IsSelected"));
            }
        }

        public static System.Xml.XmlElement GetXmlElement(System.Xml.XmlDocument xmldoc, TurnTeachingItem TeachingItem)
        {
            System.Xml.XmlElement xmlElem = xmldoc.CreateElement(TeachingItem.GetType().Name);
            System.Xml.XmlAttribute xmlAttr = null;

            xmlAttr = xmldoc.CreateAttribute(PropertyExtension.ExtractPropertyName(() => TeachingItem.Name));
            xmlAttr.Value = TeachingItem.Name.ToString();
            xmlElem.Attributes.Append(xmlAttr);

            xmlAttr = xmldoc.CreateAttribute(PropertyExtension.ExtractPropertyName(() => TeachingItem.Axis));
            xmlAttr.Value = TeachingItem.Axis.ToString();
            xmlElem.Attributes.Append(xmlAttr);

            xmlAttr = xmldoc.CreateAttribute(PropertyExtension.ExtractPropertyName(() => TeachingItem.TagName));
            xmlAttr.Value = TeachingItem.TagName.ToString();
            xmlElem.Attributes.Append(xmlAttr);

            xmlAttr = xmldoc.CreateAttribute(PropertyExtension.ExtractPropertyName(() => TeachingItem.PositionValue));
            xmlAttr.Value = TeachingItem.PositionValue.ToString().ToLower();
            xmlElem.Attributes.Append(xmlAttr);

            return xmlElem;
        }
    }

    [XmlRoot("TurnTeachingItemList")]
    public class TurnTeachingItemList : ICollection<TurnTeachingItem>
    {
        private List<TurnTeachingItem> _items = new List<TurnTeachingItem>();
        public List<TurnTeachingItem> Items
        {
            get
            {
                return _items;
            }
        }

        public TurnTeachingItem this[int ModeID]
        {
            get
            {
                if (_items != null && _items.Where(r => r.Name == ModeID).Count() > 0)
                {
                    return _items.Where(r => r.Name == ModeID).First();
                }
                return null;
            }
        }


        public static void Serialize(string fileName, TurnTeachingItemList list)
        {
            string path = System.IO.Path.GetDirectoryName(fileName);
            if (!System.IO.Directory.Exists(path))
                System.IO.Directory.CreateDirectory(path);

            lock (typeof(TurnTeachingItemList))
            {
                #region //$ XML 개선.[2014.04.09] : File 생성 방식 변경 했었으나, 원례 방식대로 사용하며, 예외 처리 추가
                FileStream fs = null;
                try
                {
                    XmlSerializer xmlSer = new XmlSerializer(typeof(TurnTeachingItemList));
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

        public static TurnTeachingItemList Deserialize(string fileName)
        {
            lock (typeof(TurnTeachingItemList))
            {
                if (System.IO.File.Exists(fileName))
                {
                    XmlSerializer xmlSer = new XmlSerializer(typeof(TurnTeachingItemList));
                    FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.None);
                    TurnTeachingItemList TeachingListList = (TurnTeachingItemList)xmlSer.Deserialize(fs);
                    fs.Close();
                    return TeachingListList;
                }
                else
                {
                    TurnTeachingItemList TeachingListList = new TurnTeachingItemList();
                    return TeachingListList;
                }
            }
        }

        //$ XML 개선.[2014.04.08] : XML Serialize 처리 변경작업(List)
        public static void SerializeListX(string fileName, TurnTeachingItemList list)
        {
            TextWriter sw = null;
            try
            {
                XmlDocument xmldoc = new XmlDocument();

                // xml Declaration
                XmlDeclaration decNode = xmldoc.CreateXmlDeclaration("1.0", "UTF-8", null);
                XmlElement root = xmldoc.DocumentElement;

                xmldoc.InsertBefore(decNode, root);

                XmlElement xmlTeachingListList = xmldoc.CreateElement(@"LeftShutterTeachingListList");
                xmlTeachingListList.SetAttribute("xmlns:xsi", "http://www.w3.org/2001/XMLSchema-instance");
                xmlTeachingListList.SetAttribute("xmlns:xsd", "http://www.w3.org/2001/XMLSchema");
                xmldoc.AppendChild(xmlTeachingListList);

                foreach (var item in list)
                {
                    XmlElement xmlTeachingListItem = TurnTeachingItem.GetXmlElement(xmldoc, item);
                    xmlTeachingListList.AppendChild(xmlTeachingListItem);
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

        public void Add(TurnTeachingItem item)
        {
            _items.Add(item);
        }

        public void Add(int Name, string TagName, int PositionValue)
        {
            TurnTeachingItem item = new TurnTeachingItem();
            item.Name = Name;
            item.TagName = TagName;
            item.PositionValue = PositionValue;

            _items.Add(item);
        }

        public void Clear()
        {
            _items.Clear();
        }

        public bool Contains(TurnTeachingItem item)
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

        public void CopyTo(TurnTeachingItem[] array, int arrayIndex)
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

        public bool Remove(TurnTeachingItem item)
        {
            return _items.Remove(item);
        }

        public bool Remove(int number)
        {
            for (int i = _items.Count - 1; i >= 0; i--)
            {
                if (Items[i].Name == number)
                {
                    _items.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region IEnumerable<TeachingList> Members

        public IEnumerator<TurnTeachingItem> GetEnumerator()
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
