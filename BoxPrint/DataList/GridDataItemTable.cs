using BoxPrint.Log;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace BoxPrint.DataList
{
    [XmlRoot("GridDataItem")]
    public class GridItemListItemInfo
    {
        [XmlAttribute("Section")]
        public string Section { get; set; }

        [XmlAttribute("GridItem")]
        public string GridItem { get; set; }

        [XmlAttribute("GridWidth")]
        public int GridWidth { get; set; }

        [XmlAttribute("BindingItem")]
        public string BindingItem { get; set; }

        [XmlAttribute("BindingStringFormat")]
        public string BindingStringFormat { get; set; }
        public static System.Xml.XmlElement GetXmlElement(System.Xml.XmlDocument xmldoc, GridItemListItemInfo gridItem)
        {
            System.Xml.XmlElement xmlElem = xmldoc.CreateElement(gridItem.GetType().Name);
            System.Xml.XmlAttribute xmlAttr = null;

            // Mode
            xmlAttr = xmldoc.CreateAttribute(PropertyExtension.ExtractPropertyName(() => gridItem.Section));
            xmlAttr.Value = gridItem.Section.ToString();
            xmlElem.Attributes.Append(xmlAttr);

            xmlAttr = xmldoc.CreateAttribute(PropertyExtension.ExtractPropertyName(() => gridItem.GridItem));
            xmlAttr.Value = gridItem.GridItem.ToString();
            xmlElem.Attributes.Append(xmlAttr);

            xmlAttr = xmldoc.CreateAttribute(PropertyExtension.ExtractPropertyName(() => gridItem.GridWidth));
            xmlAttr.Value = gridItem.GridWidth.ToString().ToLower();
            xmlElem.Attributes.Append(xmlAttr);

            xmlAttr = xmldoc.CreateAttribute(PropertyExtension.ExtractPropertyName(() => gridItem.BindingItem));
            xmlAttr.Value = gridItem.BindingItem.ToString();
            xmlElem.Attributes.Append(xmlAttr);

            xmlAttr = xmldoc.CreateAttribute(PropertyExtension.ExtractPropertyName(() => gridItem.BindingStringFormat));
            xmlAttr.Value = gridItem.BindingStringFormat.ToString();
            xmlElem.Attributes.Append(xmlAttr);

            return xmlElem;
        }
    }


    [XmlRoot("GridItemListItem")]
    public class GridItemListItem : ICollection<GridItemListItemInfo>
    {
        private List<GridItemListItemInfo> _items = new List<GridItemListItemInfo>();
        public List<GridItemListItemInfo> Items
        {
            get
            {
                return _items;
            }
        }

        public List<GridItemListItemInfo> this[string section]
        {
            get
            {
                List<GridItemListItemInfo> retlist = new List<GridItemListItemInfo>();

                foreach (GridItemListItemInfo info in _items)
                {
                    if (info.Section == section)
                        retlist.Add(info);
                }

                return retlist;
                //if (_items != null && _items.Where(r => r.Section == section).Count() > 0)
                //{
                //    return _items.Where(r => r.Section == section).First();
                //}
                //return null;
            }
        }

        public static void Serialize(string fileName, GridItemListItem list)
        {
            string path = System.IO.Path.GetDirectoryName(fileName);
            if (!System.IO.Directory.Exists(path))
                System.IO.Directory.CreateDirectory(path);

            lock (typeof(GridItemListItem))
            {
                #region //$ XML 개선.[2014.04.09] : File 생성 방식 변경 했었으나, 원례 방식대로 사용하며, 예외 처리 추가
                FileStream fs = null;
                try
                {
                    XmlSerializer xmlSer = new XmlSerializer(typeof(GridItemListItem));
                    fs = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None);
                    xmlSer.Serialize(fs, list);
                    fs.Close();

                    fs.Dispose();
                    fs = null;
                }
                catch (Exception ex)
                {
                    LogManager.WriteConsoleLog(eLogLevel.Error, "{0} - Exception:{1}", System.Reflection.MethodBase.GetCurrentMethod().Name, ex);
                }
                finally
                {
                    try
                    {
                        if (fs != null) fs.Dispose();
                    }
                    catch (Exception ex)
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Error, "{0} - finally Exception:{1}", System.Reflection.MethodBase.GetCurrentMethod().Name, ex);
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

        public static GridItemListItem Deserialize(string fileName)
        {
            lock (typeof(GridItemListItem))
            {
                if (System.IO.File.Exists(fileName))
                {
                    XmlSerializer xmlSer = new XmlSerializer(typeof(GridItemListItem));
                    FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.None);
                    GridItemListItem gridDataTable = (GridItemListItem)xmlSer.Deserialize(fs);
                    fs.Close();
                    return gridDataTable;
                }
                else
                {
                    GridItemListItem gridDataTable = new GridItemListItem();
                    return gridDataTable;
                }
            }
        }

        //$ XML 개선.[2014.04.08] : XML Serialize 처리 변경작업(List)
        public static void SerializeListX(string fileName, GridItemListItem list)
        {
            TextWriter sw = null;
            try
            {
                XmlDocument xmldoc = new XmlDocument();

                // xml Declaration
                XmlDeclaration decNode = xmldoc.CreateXmlDeclaration("1.0", "UTF-8", null);
                XmlElement root = xmldoc.DocumentElement;

                xmldoc.InsertBefore(decNode, root);

                XmlElement gridDataTable = xmldoc.CreateElement(@"GridItemListItem");
                gridDataTable.SetAttribute("xmlns:xsi", "http://www.w3.org/2001/XMLSchema-instance");
                gridDataTable.SetAttribute("xmlns:xsd", "http://www.w3.org/2001/XMLSchema");
                xmldoc.AppendChild(gridDataTable);

                foreach (var item in list)
                {
                    XmlElement gridDataTableitem = GridItemListItemInfo.GetXmlElement(xmldoc, item);
                    gridDataTable.AppendChild(gridDataTableitem);
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

        #region ICollection<GidDataItemTable> Members

        public void Add(GridItemListItemInfo item)
        {
            _items.Add(item);
        }

        public void Add(string section, string griditem, int gridwidth)
        {
            GridItemListItemInfo item = new GridItemListItemInfo();
            item.Section = section;
            item.GridItem = griditem;
            item.GridWidth = gridwidth;

            _items.Add(item);
        }

        public void Clear()
        {
            _items.Clear();
        }

        public bool Contains(GridItemListItemInfo item)
        {
            return _items.Contains(item);
        }

        //public bool ContainKey(string Tagname)
        //{
        //    bool result = false;
        //    if (_items != null && _items.Where(r => r.TagName == Tagname).Count() > 0)
        //    {
        //        result = true;
        //    }
        //    return result;
        //}

        public void CopyTo(GridItemListItemInfo[] array, int arrayIndex)
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

        public bool Remove(GridItemListItemInfo item)
        {
            return _items.Remove(item);
        }

        public bool Remove(string section)
        {
            for (int i = _items.Count - 1; i >= 0; i--)
            {
                if (Items[i].Section == section)
                {
                    _items.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region IEnumerable<GridDataItem> Members

        public IEnumerator<GridItemListItemInfo> GetEnumerator()
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
