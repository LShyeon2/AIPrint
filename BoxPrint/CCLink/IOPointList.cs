using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Serialization;

namespace BoxPrint.CCLink
{
    /// <summary>
    /// IO 접점에 대한 정보를 나타내는 클래스
    /// </summary>
    public class IOPoint : INotifyPropertyChanged
    {
        /// <summary>
        /// IO 접점이 속한 Module ID
        /// </summary>
        /// <value>Module ID</value>
        [XmlAttribute("moduleID")]
        public string ModuleID { get; set; }

        /// <summary>
        /// IO 접점 이름
        /// </summary>
        /// <value>IO 접점 이름</value>
        [XmlAttribute("name")]
        public string Name { get; set; }

        /// <summary>
        /// IO 접점 상세 내역
        /// </summary>
        /// <value>상세 내역</value>
        [XmlAttribute("description")]
        public string Description { get; set; }

        /// <summary>
        /// IO 접점이 속한 그룹
        /// </summary>
        /// <value>그룹 명</value>
        [XmlAttribute("group")]
        public eIOGroup Group { get; set; }

        /// <summary>
        /// IO 접점 In, Out 방향
        /// </summary>
        /// <value>방향</value>
        [XmlAttribute("direction")]
        public eIODirectionTypeList Direction { get; set; }

        /// <summary>
        /// IO 접점 주소
        /// </summary>
        /// <value>주소</value>
        [XmlAttribute("address")]
        public String Address { get; set; }

        /// <summary>
        /// A접점, B접점 (true : A접점, false : B접점)
        /// </summary>
        /// <value>접점 타입</value>
        [XmlAttribute("active")]
        public bool Active { get; set; }

        /// <summary>
        /// IO 보드가 2개 이상 필요할 때 사용, Default : 0
        /// </summary>
        /// <value>보드 번호</value>
        [XmlAttribute("board")]
        public int Board { get; set; }


        /// <summary>
        /// IO Simul Value
        /// </summary>
        /// <value>시뮬용 가상 값</value>
        [XmlAttribute("simulValue")]
        public bool SimulValue { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }
        /// <summary>
        /// 최근에 읽어본 IO 상태값 저장
        /// </summary>
        /// <value>접점 값</value>
        private bool _LastReadValue;

        public bool LastReadValue
        {
            get
            {
                return _LastReadValue;
            }
            set
            {
                //값이 다를때만 이벤트 발생
                if (value != LastReadValue)
                {
                    _LastReadValue = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("IOStatus"));

                }
            }
        }
        public string IOStatus
        {
            get
            {
                return LastReadValue ? "ON" : "OFF";
            }
        }

    }

    /// <summary>
    /// IOPoint에 대한 List 클래스
    /// </summary>
    [XmlRoot("IOPointList")]
    public class IOPointList : ICollection<IOPoint>
    {
        private static Object thisLock = new Object();

        private List<IOPoint> _ioPointList = new List<IOPoint>();

        /// <summary>
        /// 현재 List에서 입력한 Index에 해당하는 IO 정보를 가져온다
        /// </summary>
        /// <param name="i">Index</param>
        /// <returns>IOPoint</returns>
        public IOPoint this[int i]
        {
            get
            {
                if (i < 0 || i >= _ioPointList.Count)
                {
                    throw new ArgumentOutOfRangeException();
                }
                return _ioPointList[i];
            }
        }
        /// </summary>

        /// <summary>
        /// 현재 List에서 입력한 Module ID, Name 에 해당하는 IO 정보를 가져온다
        /// <param name="moduleID">Module ID</param>
        /// <param name="name">이름</param>
        /// <returns>IOPoint</returns>
        public IOPoint this[string moduleID, string name]
        {
            get
            {
                String modID = moduleID.ToUpper();

                var q = from item in _ioPointList
                        where item.ModuleID.ToUpper() == modID.ToUpper() && item.Name == name.ToUpper()
                        select item;
                if (q.Count() > 0)
                {
                    return q.First();
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// 현재 List에서 입력한 Module ID, Name에 해당하는 주소값을 가져온다
        /// </summary>
        /// <param name="moduleID">Module ID</param>
        /// <param name="name">이름</param>
        /// <returns>주소값</returns>
        public int GetAddress(string moduleID, string name)
        {
            String modID = moduleID.ToUpper();

            var q = from item in _ioPointList
                    where item.ModuleID.ToUpper() == modID.ToUpper() && item.Name == name.ToUpper()
                    select item;
            if (q.Count() > 0)
            {
                return Convert.ToInt32(q.First().Address);
            }
            else
            {
                return -2;
            }
        }

        /// <summary>
        /// 현재 List를 xml 파일로 저장한다
        /// </summary>
        /// <param name="fileName">파일 경로</param>
        /// <param name="ioPointList">저장할 List</param>
        public static void Serialize(string fileName, IOPointList ioPointList)
        {
            lock (thisLock)
            {
                #region //$ XML 개선.[2014.04.09] : XML 개선은 못하고, 예외 처리 추가
                FileStream fs = null;
                try
                {
                    XmlSerializer xmlSer = new XmlSerializer(typeof(IOPointList));
                    fs = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
                    xmlSer.Serialize(fs, ioPointList);
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

        /// <summary>
        /// xml 파일로 부터 IO 정보 List를 가져온다
        /// </summary>
        /// <param name="fileName">파일 경로</param>
        /// <returns>IOPointList</returns>
        public static IOPointList Deserialize(string fileName)
        {
            bool bSuccess = false;
            int nLoopCnt = 0;
            IOPointList ioPointList = null;

            lock (thisLock)
            {

                while (!bSuccess && nLoopCnt < 5)
                {
                    try
                    {
                        XmlSerializer xmlSer = new XmlSerializer(typeof(IOPointList));
                        FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                        ioPointList = (IOPointList)xmlSer.Deserialize(fs);
                        fs.Close();

                        bSuccess = true;
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(fileName);
                        Console.WriteLine("== IOPointList.Deserialize() Exception : {0}", ex.ToString());
                        bSuccess = false;
                        nLoopCnt++;
                    }
                    Thread.Sleep(50);
                }
            }

            return (bSuccess) ? ioPointList : null;
        }



        #region ICollection<IOPoint> Members

        public void Add(IOPoint item)
        {
            _ioPointList.Add(item);
        }

        public void Clear()
        {
            _ioPointList.Clear();
        }

        public bool Contains(IOPoint item)
        {
            return _ioPointList.Contains(item);
        }

        public void CopyTo(IOPoint[] array, int arrayIndex)
        {
            _ioPointList.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return _ioPointList.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(IOPoint item)
        {
            return _ioPointList.Remove(item);
        }

        #endregion

        #region IEnumerable<PPIDItem> Members

        public IEnumerator<IOPoint> GetEnumerator()
        {
            return _ioPointList.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _ioPointList.GetEnumerator();
        }

        #endregion
    }

    public class IOEventArgs : EventArgs
    {
        public IOEventArgs(String moduleID, String ioPointName, bool ioPointValue, bool ioPointRawValue, String ioPointAddr, eIODirectionTypeList ioDirection)
        {
            ModuleID = moduleID;
            IOPointName = ioPointName;
            IOPointValue = ioPointValue;
            IOPointRawValue = ioPointRawValue;
            IOPointAddress = ioPointAddr;
            IODirection = ioDirection;
        }

        public String ModuleID { get; set; }
        public String IOPointName { get; set; }
        public bool IOPointValue { get; set; }
        public bool IOPointRawValue { get; set; }
        public String IOPointAddress { get; set; }
        public eIODirectionTypeList IODirection { get; set; }
    }
}
