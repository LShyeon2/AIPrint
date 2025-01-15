using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace BoxPrint.DataList
{
    [XmlRoot("PortUIItem")]
    public class PortUIItem
    {
        [XmlAttribute("UIType")]
        public string UIType { get; set; }

        [XmlAttribute("ModuleName")]
        public string ModuleName { get; set; }

        [XmlAttribute("Text")]
        public string Text { get; set; }

        [XmlAttribute("XPosition")]
        public int XPosition { get; set; }

        [XmlAttribute("YPosition")]
        public int YPosition { get; set; }

        [XmlAttribute("RotateAngle")]
        public int RotateAngle { get; set; }


        [XmlAttribute("ControlDirection")]
        public eDirection ControlDirection { get; set; }

        [XmlAttribute("Bank")]
        public int Bank { get; set; }
    }

    [XmlRoot("PortUIItemList")]
    public class PortUIItemList : ICollection<PortUIItem>
    {
        private List<PortUIItem> _items = new List<PortUIItem>();
        public List<PortUIItem> Items
        {
            get
            {
                return _items;
            }
        }

        public PortUIItem this[string PortName]
        {
            get
            {
                if (_items != null && _items.Where(r => r.ModuleName == PortName).Count() > 0)
                {
                    return _items.Where(r => r.ModuleName == PortName).First();
                }
                return null;
            }
        }


        public static void Serialize(string fileName, PortUIItemList list)
        {
            string path = System.IO.Path.GetDirectoryName(fileName);
            if (!System.IO.Directory.Exists(path))
                System.IO.Directory.CreateDirectory(path);

            lock (typeof(PortUIItemList))
            {
                #region //$ XML 개선.[2014.04.09] : File 생성 방식 변경 했었으나, 원례 방식대로 사용하며, 예외 처리 추가
                FileStream fs = null;
                try
                {
                    XmlSerializer xmlSer = new XmlSerializer(typeof(PortUIItemList));
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

        public static PortUIItemList Deserialize(string fileName)
        {
            lock (typeof(PortUIItemList))
            {
                if (System.IO.File.Exists(fileName))
                {
                    XmlSerializer xmlSer = new XmlSerializer(typeof(PortUIItemList));
                    FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.None);
                    PortUIItemList PortList = (PortUIItemList)xmlSer.Deserialize(fs);
                    fs.Close();
                    return PortList;
                }
                else
                {
                    PortUIItemList PortList = new PortUIItemList();
                    return PortList;
                }
            }
        }

        public int GetUI_XSize()
        {
            int x_Max = 1;
            if (_items.Count == 0)
            {
                return 1;
            }
            foreach (var item in Items)
            {
                if (x_Max < item.XPosition)
                {
                    x_Max = item.XPosition;
                }
            }
            return x_Max;

        }

        public int GetUI_YSize()
        {
            int fy_Max = 0;
            int ry_Max = 0;
            if (_items.Count == 0)
            {
                return 1;
            }
            foreach (var item in Items)
            {
                if (item.Bank == 1)
                {
                    if (fy_Max < item.YPosition)
                    {
                        fy_Max = item.YPosition;
                    }
                }
                else if (item.Bank == 2)
                {
                    if (ry_Max < item.YPosition)
                    {
                        ry_Max = item.YPosition;
                    }
                }
            }
            return fy_Max + ry_Max + 1;
        }

        public int Get_BoothPos()
        {
            int fy_Max = 0;

            foreach (var item in Items)
            {
                if (item.Bank == 1)
                {
                    if (fy_Max < item.YPosition)
                    {
                        fy_Max = item.YPosition;
                    }
                }
            }

            if (fy_Max == 0)
                return 0;

            return fy_Max + 1;
        }

        #region ICollection<PortUIItemList> Members

        public void Add(PortUIItem item)
        {
            _items.Add(item);
        }
        public void Clear()
        {
            _items.Clear();
        }

        public bool Contains(PortUIItem item)
        {
            return _items.Contains(item);
        }


        public void CopyTo(PortUIItem[] array, int arrayIndex)
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

        public bool Remove(PortUIItem item)
        {
            return _items.Remove(item);
        }

        #endregion

        #region IEnumerable<PortUIItemList> Members

        public IEnumerator<PortUIItem> GetEnumerator()
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
