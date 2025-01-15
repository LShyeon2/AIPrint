using BoxPrint.Database;
using BoxPrint.DataList;
using BoxPrint.DataList.MCS;
using BoxPrint.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace BoxPrint.Modules.Shelf
{
    //구현예정
    /// <summary>
    /// 쉘프 관리자
    /// </summary>
    public class ShelfManager : SingletonBase<ShelfManager>
    {
        private static object thisLock = new object();

        private double _LoadRatioWarning = 90; //적재율 경고 기준치  
        public double LoadRatioWarning
        {
            get
            {
                return _LoadRatioWarning;
            }
            private set
            {
                if (0 <= value  && value <= 100)
                {
                    _LoadRatioWarning = value;
                }
            }
        }
        public bool IsLoadRatioWarningState
        {
            get
            {
                return GetShelfLoadRatio() >= LoadRatioWarning;
            }
        }

        private bool bExitSignal = false;
        public readonly int FireCheckCycleDelay = 1000;

        public readonly string FullPath = System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
        public readonly string FrontTeachDataPath = "\\Data\\" + GlobalData.TestModel + "RM\\FrontTeachingDataRM1.xml";
        public readonly string RearTeachDataPath = "\\Data\\" + GlobalData.TestModel + "RM\\RearTeachingDataRM1.xml";

        public ManualResetEvent MRE = new ManualResetEvent(false);

        /// <summary>
        /// Zone Name 정의 안되어 있는 쉘프를 위한 기본 ZoneName 미리 정의
        /// </summary>
        public string DefaultShelfZoneName
        {
            get
            {
                return GlobalData.Current.EQPID + "_Z01";
            }
        }



        public ShelfItemList FrontData { get; set; }
        public ShelfItemList RearData { get; set; }

        //SuHwan_20221110 : [ServerClient] 최적화 검색용
        private Dictionary<string, ShelfItem> dicFrontData = new Dictionary<string, ShelfItem>();
        private Dictionary<string, ShelfItem> dicRearData = new Dictionary<string, ShelfItem>();

        //220421 HHJ SCS 개선     //- xml, db 별도 사용으로 변경
        /// <summary>
        /// 일단 어디서 가져와야할지 정해지지않아서 강제로 지정하는것으로 함.
        /// </summary>
        //220524 HHJ SCS 개선     //- Shelf Xml제거
        //private bool bUseDB = false;    

        //전체 쉘프 데이타
        private ShelfItemList _AllData = new ShelfItemList();
        private ShelfItemList _TempAllData = new ShelfItemList();
        public ShelfItemList AllData
        {
            get
            {
                if (_AllData != null)
                {
                    _TempAllData = _AllData;
                    return _AllData;
                }
                else
                {
                    if (GlobalData.Current.ServerClientType == eServerClientType.Client)
                    {
                        lock (thisLock)
                        {
                            _TempAllData = new ShelfItemList();
                            foreach (var sItem in FrontData)
                            {
                                _TempAllData.Add(sItem);
                            }
                            foreach (var sItem in RearData)
                            {
                                _TempAllData.Add(sItem);
                            }
                            _AllData = _TempAllData;
                            return _AllData;
                        }
                    }
                    else
                    {
                        _TempAllData = new ShelfItemList();
                        foreach (var sItem in FrontData)
                        {
                            _TempAllData.Add(sItem);
                            sItem.SetPlayBackTrace(); //쉘프 데이터 획득후 플레이백 로그 저장.
                        }
                        foreach (var sItem in RearData)
                        {
                            _TempAllData.Add(sItem);
                            sItem.SetPlayBackTrace(); //쉘프 데이터 획득후 플레이백 로그 저장.
                        }
                        _AllData = _TempAllData;
                        return _AllData;
                    }
                }
            }
        }

        Thread FireThread = null;

        //private List<Shelf> shelfList;
        public ShelfManager()
        {
            //220401 HHJ SCS 개선     //- Xml, DB 혼용에 따른 초기 생성 불가능 현상 조치
            //초기 인스턴스 생성시에 Get하지않음.
            //bool Online = true;
            //if (Online)
            //{
            //    GetShelfDataFromDB();
            //}
            //else
            //{
            //    GetShelfDataFromXML();
            //}

            //220418 HHJ SCS 개선     //- LayOut 설정 관리 추가
            FrontData = new ShelfItemList();
            RearData = new ShelfItemList();
            //220524 HHJ SCS 개선     //- Shelf Xml제거
            //bUseDB = true;      //220421 HHJ SCS 개선     //- xml, db 별도 사용으로 변경
        }

        #region Min,Max Calc Method
        //min,max 값은 한번만 계산하고 이후 저장값 리턴하도록 함.
        private int _MaxBank = -1;
        private int _MaxBay = -1;
        private int _MaxLevel = -1;
        private int _MinBank = -1;
        private int _MinBay = -1;
        private int _MinLevel = -1;

        public int GetMaxBank()
        {
            if (AllData.Count == 0)
            {
                return 1;
            }
            if (_MaxBank < 0)
            {
                _MaxBank = AllData.Max(s => s.ShelfBank);
            }
            return _MaxBank;
        }
        public int GetMaxBay(bool bReload = false)
        {
            if (AllData.Count == 0)
            {
                return 1;
            }
            if (_MaxBay < 0)
            {
                _MaxBay = AllData.Max(s => s.ShelfBay);
            }
            if (bReload)
            {
                _MaxBay = AllData.Max(s => s.ShelfBay);
            }
            return _MaxBay;
        }
        public int GetMaxLevel(bool bReload = false)
        {
            if (AllData.Count == 0)
            {
                return 1;
            }
            if (_MaxLevel < 0)
            {
                _MaxLevel = AllData.Max(s => s.ShelfLevel);
            }
            if (bReload)
            {
                _MaxLevel = AllData.Max(s => s.ShelfLevel);
            }
            return _MaxLevel;
        }
        public int GetMinBank()
        {
            if (AllData.Count == 0)
            {
                return 1;
            }
            if (_MinBank < 0)
            {
                _MinBank = AllData.Min(s => s.ShelfBank);
            }
            return _MinBank;
        }
        public int GetMinBay()
        {
            if (AllData.Count == 0)
            {
                return 1;
            }
            if (_MinBay < 0)
            {
                _MinBay = AllData.Min(s => s.ShelfBay);
            }
            return _MinBay;
        }
        public int GetMinLevel()
        {
            if (AllData.Count == 0)
            {
                return 1;
            }
            if (_MinLevel < 0)
            {
                _MinLevel = AllData.Min(s => s.ShelfLevel);
            }
            return _MinLevel;
        }
        #endregion

        /// <summary>
        /// DB Shelf Data를 현재 정보로 리프레시한다.
        /// </summary>
        public void RefreshDBUpdate(bool GlobalInit)
        {
            foreach(var sItem in AllData)
            {
                SaveShelfData(sItem, GlobalInit);
            }
        }

        /// <summary>
        /// 221108 RGJ
        /// 화재 감시 쓰레드 생성자에서 분리
        /// </summary>
        public void StartFireCheckRun()
        {
            if (FireThread == null) //한번만 생성한다. 
            {
                FireThread = new Thread(new ThreadStart(FireCheckRun)); //방화 시퀀스 추가.
                FireThread.Name = "ShelfFireCheckThread";
                FireThread.IsBackground = true;
                FireThread.Start();
            }
        }

        public void ScribeRMEvent()
        {
            foreach (var rm in GlobalData.Current.mRMManager.ModuleList)
            {
                rm.Value.OnShelfUpdate += ShelfManager_OnShelfUpdate;
            }
        }

        private void ShelfManager_OnShelfUpdate(object sender, RM.RMModuleBase.ShelfUpDateEventArgs e)
        {
            var TargetShelf = GetShelf(e.Tag);
            if (TargetShelf != null)
            {
                TargetShelf.NotifyShelfStatusChanged();
            }
        }

        /// <summary>
        /// 현재 가용가능한 쉘프 수량을 계산한다.
        /// </summary>
        /// <returns></returns>
        public int CalcShelfZoneCapa(string ZoneName)
        {
            //220803 조숭진 shelfuse 추가
            int Capa = AllData.Where(s => s.CheckCarrierExist() == false && s.ZONE == ZoneName && s.ShelfAvailable).Count();
            return Capa;
        }
        public int CalcShelfZoneTotalCount(string ZoneName)
        {
            //220803 조숭진 shelfuse 추가
            int Capa = AllData.Where(s => !s.DeadZone && s.ZONE == ZoneName).Count();
            return Capa;
        }

        public int CalcDistance(int a, int b)
        {
            int c = a - b;
            return c > 0 ? c : -c;
        }
        public int CalcDistance(ICarrierStoreAble a, ICarrierStoreAble b)
        {
            int BankDistance = a.iBank == b.iBank ? 0 : 1;
            int BayDistance = a.iBay - b.iBay;
            int LevelDistance = a.iLevel - b.iLevel;

            BayDistance = (BayDistance < 0 ? -BayDistance : BayDistance);
            LevelDistance = (LevelDistance < 0 ? -LevelDistance : LevelDistance);

            return BankDistance + BayDistance + LevelDistance;
        }
        //220421 HHJ SCS 개선     //- xml, db 별도 사용으로 변경
        public void GetShelfData()
        {
            //220524 HHJ SCS 개선     //- Shelf Xml제거
            //if (bUseDB)
            //    GetShelfDataFromDB();
            //else
            //    GetShelfDataFromXML();
            GetShelfDataFromDB();

            //SuHwan_20230127 : [ServerClinet] _AllData이건 한번 만들면 초기화가 안되어서 초기화 시킴
            _AllData = null;

        }




        public void SaveShelfData()
        {
            //220524 HHJ SCS 개선     //- Shelf Xml제거
            //if (bUseDB)
            //    SaveShelfDataFromDB();
            //else
            //    SaveShelfDataFromXML();
            SaveShelfDataFromDB();
        }
        //220523 HHJ SCS 개선     //- ShelfSetterControl 신규 추가
        public async Task<bool> SaveShelfDataAsync()
        {
            try
            {
                await Task.Run(() =>
                {
                    //220524 HHJ SCS 개선     //- Shelf Xml제거
                    //if (bUseDB)
                    //    SaveShelfDataFromDB();
                    //else
                    //    SaveShelfDataFromXML();
                    SaveShelfDataFromDB();
                });

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        /// <summary>
        /// //방화 시퀀스 추가.
        /// 쉘프 연기 센서 체크하고 연기 감지시 스케쥴러에 방화 작업 추가요청.
        /// </summary>
        private void FireCheckRun()
        {
            MRE.WaitOne(); //Shelf Load 완료 대기
            while (bExitSignal == false)
            {
                try
                {
                    foreach (var sItem in AllData)
                    {
                        if (sItem.CheckSmokeSensor())
                        {
                            //230928 첫 화재발생 시 화재대응하러 갔으나 공출고일 수 있다. 처음에만 보고하고 공출고된 아이디인 쉘프는 스킵한다.
                            if (sItem.CheckCarrierExist() && !sItem.CarrierID.Contains("UNKEMP"))
                            {
                                if (sItem.NeedFireAlarmReport) //보고는 한번만 하고 작업은 계속 생성할지 모니터링 한다.
                                {
                                    //LogManager.WriteConsoleLog(eLogLevel.Info, "{0} bank {1} bay {2} level Shelf 에 연기감지 발생 방화작업을 요청합니다.", sItem.ShelfBank, sItem.ShelfBay, sItem.ShelfLevel);
                                    LogManager.WriteConsoleLog(eLogLevel.Info, "{0} bank {1} bay {2} level Shelf Fire Alarm Set. Request Fire Job!", sItem.ShelfBank, sItem.ShelfBay, sItem.ShelfLevel);

                                    Thread.Sleep(100);
                                    GlobalData.Current.Alarm_Manager.AlarmOccurbyName("FIRE_AND_SMOKE_DETECTED_INSHELF", sItem.iLocName); //[230503 CIM 검수] 쉘프명 으로 보고함
                                    sItem.NeedFireAlarmReport = false; //중복 보고 방지를 위해 Flag 제어

                                }

                                //241030 HoN 화재 관련 추가 수정        //-. PLC로 알려주는 Bit 화재 발생하면 무조건 전 Crane ON 처리. -> OFF시점은 Operator가 수동으로 해야함. 이를 수행하지 않아 발생하는 문제는 오퍼레이터 조작미스로 처리
                                if (!GlobalData.Current.mRMManager.CheckCraneFireOccurred())
                                {
                                    GlobalData.Current.mRMManager.NotifyFireCommand(true);
                                }
                                 
                            }
                            else
                            {
                                if (GlobalData.Current.GlobalSimulMode)
                                {
                                    sItem.SetSimulSmokeSensor(false);
                                }
                            }
                        }
                    }
                    Thread.Sleep(FireCheckCycleDelay);
                    
                }
                catch (Exception ex)
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
                }
            }
        }
        public void RequestAllShelfFireReset()
        {
            foreach (var sItem in AllData)
            {
                sItem.SetFireSensorState(false);
            }
        }
        public void SaveShelfData(ShelfItem shelfitem, bool GlobalInit = false)
        {
            //220524 HHJ SCS 개선     //- Shelf Xml제거
            //if (bUseDB)
            //    GlobalData.Current.DBManager.DbSetShelfInfo(shelfitem);
            //else
            //{
            //    if (shelfitem.ShelfBank.Equals(eShelfBank.Front))
            //        ShelfItemList.Serialize(GlobalData.Current.CurrentFilePaths(GlobalData.Current.FullPath) + FrontTeachDataPath, FrontData);
            //    else
            //        ShelfItemList.Serialize(GlobalData.Current.CurrentFilePaths(GlobalData.Current.FullPath) + RearTeachDataPath, RearData);
            //}

            //GlobalData.Current.DBManager.DbSetShelfInfo(shelfitem);

            GlobalData.Current.DBManager.DbSetProcedureShelfInfo(shelfitem, GlobalInit);
        }
        private void SaveShelfDataFromDB()
        {
            GlobalData.Current.DBManager.CreateShelfInfo(FrontData);
            GlobalData.Current.DBManager.CreateShelfInfo(RearData);
        }
        //220524 HHJ SCS 개선     //- Shelf Xml제거
        //private void SaveShelfDataFromXML()
        //{
        //    ShelfItemList.Serialize(GlobalData.Current.CurrentFilePaths(GlobalData.Current.FullPath) + FrontTeachDataPath, FrontData);
        //    ShelfItemList.Serialize(GlobalData.Current.CurrentFilePaths(GlobalData.Current.FullPath) + RearTeachDataPath, RearData);
        //}

        //220421 HHJ SCS 개선     //- xml, db 별도 사용으로 변경
        //220318 조숭진 oracledbmanager 추가
        ////public void GetShelfDataFromDB()
        private void GetShelfDataFromDB()
        {
            #region 삭제 주석
            //FrontData = GlobalData.Current.DBManager.GetDBShelfInfo(string.Format("{0:D2}", (int)eShelfBank.Front));
            //RearData = GlobalData.Current.DBManager.GetDBShelfInfo(string.Format("{0:D2}", (int)eShelfBank.Rear));
            //FrontData = GlobalData.Current.DBManager.DbGetProcedureShelfInfo(string.Format("{0:D2}", (int)eShelfBank.Front));
            //RearData = GlobalData.Current.DBManager.DbGetProcedureShelfInfo(string.Format("{0:D2}", (int)eShelfBank.Rear));

            //var ShelfBuffer = GlobalData.Current.DBManager.DbGetProcedureShelfInfo(string.Format("{0:D2}", (int)eShelfBank.Front));
            //if (FrontData.Count > 0)
            //{
            //    foreach (var item in FrontData)
            //    {
            //        var whereValue = ShelfBuffer.Where(s => s.TagName == item.TagName).FirstOrDefault();

            //        if (whereValue != null)
            //        {
            //            setShelfParameter(item, whereValue);
            //        }

            //    }
            //}
            //else
            //{
            //    FrontData = ShelfBuffer;
            //}

            //ShelfBuffer = GlobalData.Current.DBManager.DbGetProcedureShelfInfo(string.Format("{0:D2}", (int)eShelfBank.Rear));
            //if (RearData.Count > 0)
            //{
            //    foreach (var item in RearData)
            //    {
            //        var whereValue = ShelfBuffer.Where(s => s.TagName == item.TagName).FirstOrDefault();

            //        if (whereValue != null)
            //        {
            //            setShelfParameter(item, whereValue);
            //        }

            //    }
            //}
            //else
            //{
            //    RearData = ShelfBuffer;
            //}
            #endregion


            //SuHwan_20221114 : [ServerClient] 수정
            if (FrontData.Count > 0)
            {
                //GlobalData.Current.DBManager.DbGetProcedureShelfInfoForClient(string.Format("{0:D2}", (int)eShelfBank.Front));
                GlobalData.Current.DBManager.DbGetProcedureShelfInfoForClient(string.Format("{0:D2}", GlobalData.Current.FrontBankNum));
            }
            else
            {
                //FrontData = GlobalData.Current.DBManager.DbGetProcedureShelfInfo(string.Format("{0:D2}", (int)eShelfBank.Front));
                FrontData = GlobalData.Current.DBManager.DbGetProcedureShelfInfo(string.Format("{0:D2}", GlobalData.Current.FrontBankNum));
            }

            if (RearData.Count > 0)
            {
                //GlobalData.Current.DBManager.DbGetProcedureShelfInfoForClient(string.Format("{0:D2}", (int)eShelfBank.Rear));
                GlobalData.Current.DBManager.DbGetProcedureShelfInfoForClient(string.Format("{0:D2}", GlobalData.Current.RearBankNum));
            }
            else
            {
                //RearData = GlobalData.Current.DBManager.DbGetProcedureShelfInfo(string.Format("{0:D2}", (int)eShelfBank.Rear));
                RearData = GlobalData.Current.DBManager.DbGetProcedureShelfInfo(string.Format("{0:D2}", GlobalData.Current.RearBankNum));
            }

        }

        /// <summary>
        /// 쉘프 파라메타 설정
        /// </summary>
        /// <param name="rcvTarget"></param>
        /// <param name="rcvSource"></param>
        public void setShelfParameter(ShelfItem rcvTarget, ShelfItem rcvSource)
        {

            rcvTarget.DeadZone = rcvSource.DeadZone;
            rcvTarget.ZONE = rcvSource.ZONE;
            rcvTarget.SHELFUSE = rcvSource.SHELFUSE;
            rcvTarget.SHELFTYPE = rcvSource.SHELFTYPE;
            //rcvTarget.ShelfStatus = (eShelfStatus)rcvSource.ShelfStatus;
            rcvTarget.ShelfMemo = rcvSource.ShelfMemo;
            rcvTarget.FireSensorValue = rcvSource.FireSensorValue; //240820 RGJ 화재 상태 클라이언트 표시 추가.
            if (rcvTarget.GetCarrierID() != rcvSource.GetCarrierID())
            {
                if (string.IsNullOrEmpty(rcvSource.GetCarrierID()))
                {
                    rcvTarget.ResetCarrierData();
                }
                else
                {
                    rcvTarget.UpdateCarrier(rcvSource.GetCarrierID(), false);
                }
            }
            //Shelf Status Mismatch Case 추가.
            switch (rcvSource.ShelfStatus)
            {
                case eShelfStatus.EMPTY:
                    if (CarrierStorage.Instance.GetInModuleCarrierItem(rcvSource.TagName) is CarrierItem carrier) //캐리어 스토리지에는 해당 쉘프에 화물이 위치해 있음. 
                    {
                        if (carrier.CarrierID.StartsWith("UNK"))
                        {
                            rcvTarget.ShelfStatus = eShelfStatus.UNKSHELF;
                        }
                        else
                        {
                            rcvTarget.ShelfStatus = eShelfStatus.OCCUPIED;
                        }
                    }
                    else
                    {
                        rcvTarget.ShelfStatus = (eShelfStatus)rcvSource.ShelfStatus;
                    }
                    break;
                case eShelfStatus.OCCUPIED:
                case eShelfStatus.UNKSHELF:
                    if (CarrierStorage.Instance.GetInModuleCarrierItem(rcvSource.TagName) == null) //캐리어 스토리지에는 해당 쉘프에 화물이 없음
                    {
                        rcvTarget.ShelfStatus = eShelfStatus.EMPTY;
                    }
                    else
                    {
                        if (rcvSource.CarrierID.StartsWith("UNK")) //240805 RGJ UNK 클라이언트 상태 표시 다시한번 셋.
                        {
                            rcvTarget.ShelfStatus = eShelfStatus.UNKSHELF;
                        }
                        else
                        {
                            rcvTarget.ShelfStatus = (eShelfStatus)rcvSource.ShelfStatus;
                        }

                    }
                    break;
                default:
                    rcvTarget.ShelfStatus = (eShelfStatus)rcvSource.ShelfStatus;
                    break;
            }

        }

        //220524 HHJ SCS 개선     //- Shelf Xml제거
        //220421 HHJ SCS 개선     //- xml, db 별도 사용으로 변경
        //public void GetShelfDataFromXML()
        //private void GetShelfDataFromXML()
        //{
        //    FrontData = ShelfItemList.Deserialize(GlobalData.Current.CurrentFilePaths(FullPath) + FrontTeachDataPath);
        //    RearData = ShelfItemList.Deserialize(GlobalData.Current.CurrentFilePaths(FullPath) + RearTeachDataPath);
        //}
        public ShelfItem GetShelf(int bank, int bay, int Level)
        {
            //if (bank == 1)
            if(bank == GlobalData.Current.FrontBankNum)
            {
                return FrontData.GetShelfItem(bank, bay, Level);
            }
            //else if (bank == 2)
            else if(bank == GlobalData.Current.RearBankNum)
            {
                return RearData.GetShelfItem(bank, bay, Level);
            }
            return null;
        }
        public ShelfItem GetShelf(string bank, string bay, string level)
        {
            if (int.TryParse(bank, out int ibank) && int.TryParse(bay, out int ibay) && int.TryParse(level, out int ilevel))
            {
                return GetShelf(ibank, ibay, ilevel);
            }
            else
            {
                return null;
            }

        }
        public ShelfItem GetShelf(string Tag)
        {
            if (string.IsNullOrEmpty(Tag))
            {
                return null;
            }

            {
                if (Tag.Substring(0, 2) == string.Format("{0:D2}", GlobalData.Current.FrontBankNum))
                {
                    return FrontData.GetShelfItem(Tag);
                }
                else if (Tag.Substring(0, 2) == string.Format("{0:D2}", GlobalData.Current.RearBankNum))
                {
                    return RearData.GetShelfItem(Tag);
                }
            }
            return null;
        }
        public bool ContainShelf(string Tag)
        {
            return AllData.Where(s => s.TagName == Tag).Count() > 0;//SuHwan_20221109 :  1 -> 0 으로 변경
        }
        public ShelfItem GetProperDestShelf(string ZoneName, ICarrierStoreAble SourceItem)
        {
            if (SourceItem == null || string.IsNullOrEmpty(ZoneName))
            {
                return null;
            }
            ShelfItem DestShelf = null;
            if (GlobalData.Current.SCSType == eSCSType.Dual)
            {
                //로드 밸런싱 로직 추가.목적 => 공용 쉘프에 여유분이 있게 유지해야함.공용 쉘프 없으면  HandOver 불가로 설비 정체가능성.
                //1번 전용 소스에서는  가급적 1번 전용 쉘프선택
                //2번 전용 소스에서는  가급적 2번 전용 쉘프선택
                //공용구간 소스에서는  가급적 공용쉘프 구역을 후순위로 해서 공용 쉘프에 여유분이 있게 유지해야함.
                int FirstExBay = GlobalData.Current.Scheduler.RM1_ExclusiveBay;
                int SecondExBay = GlobalData.Current.Scheduler.RM2_ExclusiveBay;
                eCraneExZone SourceExzone = GlobalData.Current.Scheduler.GetCraneExZone(SourceItem); //소스의 위치 파악
                switch (SourceExzone)
                {
                    case eCraneExZone.FirstCraneZone:
                        IOrderedEnumerable<ShelfItem> selectedShelf_First = from shelf in AllData
                                                                            where shelf.ZONE == ZoneName && shelf.ShelfAvailable && !shelf.Scheduled && !shelf.CheckCarrierExist() && shelf.ShelfBay <= FirstExBay && !shelf.HandOverProtect && !shelf.FireSensorValue //화재 상태인 쉘프는 목적지로 지정하면 안됨.
                                                                            orderby CalcDistance(shelf, SourceItem) ascending
                                                                            select shelf;
                        DestShelf = selectedShelf_First.FirstOrDefault();
                        break;
                    case eCraneExZone.SecondCraneZone:
                        IOrderedEnumerable<ShelfItem> selectedShelf_Second = from shelf in AllData
                                                                             where shelf.ZONE == ZoneName && shelf.ShelfAvailable && !shelf.Scheduled && !shelf.CheckCarrierExist() && shelf.ShelfBay >= SecondExBay && !shelf.HandOverProtect && !shelf.FireSensorValue //화재 상태인 쉘프는 목적지로 지정하면 안됨.
                                                                             orderby CalcDistance(shelf, SourceItem) ascending
                                                                             select shelf;
                        DestShelf = selectedShelf_Second.FirstOrDefault();
                        break;
                    default: //231012 RGJ 핸드오버 프로텍트 쉘프 지정.
                        IOrderedEnumerable<ShelfItem> selectedShelf_Both = from shelf in AllData
                                                                           where shelf.ZONE == ZoneName && shelf.ShelfAvailable && !shelf.Scheduled && !shelf.CheckCarrierExist() && (shelf.ShelfBay <= FirstExBay || shelf.ShelfBay >= SecondExBay) && !shelf.HandOverProtect && !shelf.FireSensorValue //화재 상태인 쉘프는 목적지로 지정하면 안됨.
                                                                           orderby CalcDistance(shelf, SourceItem) ascending
                                                                           select shelf;
                        DestShelf = selectedShelf_Both.FirstOrDefault();
                        break;
                }

                //2024.08.12 lim, 추가 우선 순위 검색 추가  HandOverArea(Handover 주변 8칸) 제외하고 가까운곳으로
                if (DestShelf != null) //선순위 검색 성공.
                {
                    return DestShelf;
                }
                else //선순위 검색 실패하면 기존 가까운곳으로 보낸다.
                {
                    //231012 RGJ 핸드오버 프로텍트 쉘프 지정.
                    var selectedShelf = from shelf in AllData
                                        where shelf.ZONE == ZoneName && shelf.ShelfAvailable && !shelf.Scheduled && !shelf.CheckCarrierExist() &&
                                        !shelf.HandOverArea && !shelf.FireSensorValue //화재 상태인 쉘프는 목적지로 지정하면 안됨.
                                        orderby CalcDistance(shelf, SourceItem) ascending
                                        select shelf;
                    DestShelf = selectedShelf.FirstOrDefault();
                }

                if (DestShelf != null) //선순위 검색 성공.
                {
                    return DestShelf;
                }
                else //선순위 검색 실패하면 기존 가까운곳으로 보낸다.
                {
                    //231012 RGJ 핸드오버 프로텍트 쉘프 지정.
                    var selectedShelf = from shelf in AllData
                                        where shelf.ZONE == ZoneName && shelf.ShelfAvailable && !shelf.Scheduled && !shelf.CheckCarrierExist() && !shelf.HandOverProtect && !shelf.FireSensorValue //화재 상태인 쉘프는 목적지로 지정하면 안됨.
                                        orderby CalcDistance(shelf, SourceItem) ascending
                                        select shelf;


                    return selectedShelf.FirstOrDefault();
                }
            }
            else //싱글 크레인 => 소스에서 가까운 쉘프검색
            {
                var selectedShelf = from shelf in AllData
                                    where shelf.ZONE == ZoneName && shelf.ShelfAvailable && !shelf.Scheduled && !shelf.CheckCarrierExist() && !shelf.FireSensorValue //화재 상태인 쉘프는 목적지로 지정하면 안됨.
                                    orderby CalcDistance(shelf, SourceItem) ascending
                                    select shelf;


                return selectedShelf.FirstOrDefault();
            }
        }
        //Hand Over 에 필요한 버퍼 쉘프를 찾지만 가능한 중간위치 쉘프를 선택한다.
        public ShelfItem GetProperBufferShelf(int MinBay, int MaxBay, int DestBay)
        {
            if(MinBay < 0) //음수 베이 없음 조건이상
            {
                return null;
            }
            if (MinBay > MaxBay) //조건이상
            {
                return null;
            }
            int nearbay = NearBayDistance(MinBay, MaxBay, DestBay);

            int MidBay = (MaxBay + MinBay) / 2;

            var selectedShelf = from s in AllData
                                where MinBay <= s.ShelfBay && s.ShelfBay <= MaxBay && s.ShelfAvailable && !s.Scheduled && !s.CheckCarrierExist() && !s.FireSensorValue //화재 상태인 쉘프는 목적지로 지정하면 안됨.
                                orderby CalcBayDistance(s.ShelfBay, MidBay) ascending
                                select s;

            return selectedShelf.FirstOrDefault();

        }

        //Hand Over 에 필요한 버퍼 쉘프를 찾지만 가능한 중간위치 쉘프를 선택한다. (높이 추가)
        //2024.08.12 lim, Handover 할때 Level도 고려
        public ShelfItem GetProperBufferShelf(int MinBay, int MaxBay, int DestBay, int HalfLvl)
        {
            if (MinBay < 0) //음수 베이 없음 조건이상
            {
                return null;
            }
            if (MinBay > MaxBay) //조건이상
            {
                return null;
            }
            int nearbay = NearBayDistance(MinBay, MaxBay, DestBay);

            int MidBay = (MaxBay + MinBay) / 2;

            var selectedShelf = from s in AllData
                                where MinBay <= s.ShelfBay && s.ShelfBay <= MaxBay && s.ShelfAvailable && !s.Scheduled && !s.CheckCarrierExist() && !s.FireSensorValue //화재 상태인 쉘프는 목적지로 지정하면 안됨.
                                orderby CalcBayDistance(s.ShelfBay, MidBay) ascending, CalcDistance(s.ShelfLevel, HalfLvl) ascending
                                select s;

            return selectedShelf.FirstOrDefault();

        }

        public int CalcBayDistance(int a, int b)
        {
            int c = b - a;
            return c > 0 ? c : -c;
        }

        public int NearBayDistance(int min, int max, int dest)
        {
            int a = Math.Abs(dest - min);
            int b = Math.Abs(dest - max);

            return a > b ? max : min;
        }

        public ShelfItem GetProperWithDrawShelf(int targetBay)
        {

            var selectedShelf = FrontData.Items.Where(s => s.ShelfBay == targetBay && !s.DeadZone).FirstOrDefault(); //작업에 선택할 목표 쉘프
            if (selectedShelf != null)
            {
                return selectedShelf;
            }
            else
            {
                return RearData.Items.Where(s => s.ShelfBay == targetBay && !s.DeadZone).FirstOrDefault(); //작업에 선택할 목표 쉘프
            }
        }
        //220413 HHJ SCS 개선     //- UI 실행 초기부하 감소
        public List<string> GetShelfExistIDList()
        {
            List<string> tmp = new List<string>();

            tmp.AddRange((from shelf in GlobalData.Current.MainBooth.FrontData
                          where !string.IsNullOrEmpty(shelf.CarrierID)
                          select shelf.CarrierID).ToList());
            tmp.AddRange((from shelf in GlobalData.Current.MainBooth.RearData
                          where !string.IsNullOrEmpty(shelf.CarrierID)
                          select shelf.CarrierID).ToList());

            return tmp;
        }
        //public List<ShelfItem> GetShelfItemByCarrierID(string cid)
        //{
        //    List<ShelfItem> tmp = new List<ShelfItem>();

        //    tmp.AddRange(FrontData.Items.Where(s => s.CarrierID == cid).ToList());
        //    tmp.AddRange(RearData.Items.Where(s => s.CarrierID == cid).ToList());

        //    return tmp;
        //}
        public ShelfItem GetShelfItemByCarrierID(string cid)
        {
            //220905 조숭진 캐리어 아이디가 완전 똑같은 것으로 찾자..
            try
            {
                return AllData.First(s => s.CarrierID.Equals(cid));
            }
            catch (Exception)
            {
                return null;
            }
            //return AllData.Where(s => s.CarrierID == cid).FirstOrDefault();
        }


        /// <summary>
        /// 작업자로 부터 캐리어 생성 요청 받았을때 처리
        /// 추가 인자 받아야 하지만 일단 캐리어ID 만 받는다.
        /// </summary>
        /// <param name="CarrierID"></param>
        //220523 HHJ SCS 개선     //- ShelfSetterControl 신규 추가
        #region 이전
        //public void GenerateCarrierRequest(string ShelfID , string cID)
        //{
        //    ShelfItem SItem = GetShelf(ShelfID);
        //    if (SItem != null)
        //    {
        //        if (GlobalData.Current.MainBooth.CurrentOnlineState == eOnlineState.Offline) //OffLine 인 경우 보고 없이 생성
        //        {
        //            //인스톨할때 쉘프 설정이 전부다 이루어 져야한다.
        //            CarrierItem carrier = new CarrierItem()
        //            {
        //                CarrierID = cID,
        //                CarrierSize = SItem.ShelfType.Equals(eShelfType.Short) ? eCarrierSize.Short : eCarrierSize.Long,
        //                CarrierType = "Plate",
        //                LotID = "",     //Lot정보 기입도 해야하나?
        //                ProductQuantity = 500,
        //                Polarity = ePolarity.ANODE,
        //                InnerTrayType = eInnerTrayType.NO_DEFINE,
        //                ProductEmpty = eProductEmpty.FULL,
        //                UncoatedPart = eUnCoatedPart.NA,
        //                WinderDirection = eWinderDirection.UP,
        //                CarryInTime = DateTime.Now,
        //            };
        //            CarrierStorage.Instance.InsertCarrier(carrier);
        //            SItem.DefaultSlot.SetCarrierData(carrier.CarrierID);
        //        }
        //        else
        //        {
        //            McsJob tempJob = new McsJob();
        //            tempJob.CommandID = "";
        //            tempJob.CarrierID = cID;
        //            tempJob.CarrierLoc = SItem.TagName;
        //            tempJob.CarrierZoneName = SItem.iZoneName;
        //            //S6F11 CarrierGeneratorRequest CEID 312
        //            GlobalData.Current.HSMS.SendMessageAsync("S6F11", new Dictionary<string, object>() { { "CEID", 312 }, { "JobData", tempJob } });
        //        }
        //    }
        //}
        #endregion
        public bool GenerateCarrierRequest(string ShelfID, CarrierItem carrier)
        {
            ShelfItem SItem = GetShelf(ShelfID);
            if (SItem != null)
            {
                bool Inserted = CarrierStorage.Instance.InsertCarrier(carrier);
                if(Inserted)
                {
                    SItem.UpdateCarrier(carrier.CarrierID, true, true);
                    SItem.NotifyShelfStatusChanged();
                    if (GlobalData.Current.MainBooth.CurrentOnlineState != eOnlineState.Offline_EQ) //OffLine 인 경우 보고 없이 생성
                    {
                        McsJob tempJob = new McsJob();
                        tempJob.CommandID = "";
                        tempJob.CarrierID = carrier.CarrierID;
                        //tempJob.CarrierZoneName = SItem.iZoneName;
                        //S6F11 CarrierGeneratorRequest CEID 312
                        //GlobalData.Current.HSMS.SendS6F11(311, "JOBDATA", tempJob);
                        McsJob TempJob = new McsJob();
                        TempJob.CarrierID = carrier.CarrierID;

                        //S6F11 CarrierIDRead CEID 301 CarrierInstallCompleted
                        GlobalData.Current.HSMS.SendS6F11(301, "JOBDATA", TempJob, "SHELFITEM", SItem);
                        //Zone Capacity Changed
                        GlobalData.Current.HSMS.SendS6F11(310, "ZONENAME", SItem.iZoneName);
                    }
                }
                return Inserted;
            }
            return false;
        }
        ///// <summary>
        ///// MCS 에서 캐리어 생성 응답이 왔을경우 처리.
        ///// </summary>
        ///// <param name="ShelfID"></param>
        ///// <param name="cID"></param>
        //public void HostCommand_CarrierGenerate(string ShelfID, string cID)
        //{
        //    Task.Factory.StartNew(() =>
        //    {
        //        Thread.Sleep(200);
        //        ShelfItem SItem = GetShelf(ShelfID);
        //        if (SItem != null)
        //        {
        //            CarrierItem carrier = new CarrierItem()
        //            {

        //                CarrierID = cID,
        //                CarrierLocation = SItem.iLocName,
        //                CarrierSize = SItem.ShelfType.Equals(eShelfType.Short) ? eCarrierSize.Short : eCarrierSize.Long,
        //                CarrierType = "Plate",
        //                LotID = "",     //Lot정보 기입도 해야하나?
        //                ProductQuantity = 500,
        //                Polarity = ePolarity.ANODE,
        //                InnerTrayType = eInnerTrayType.NONE,
        //                ProductEmpty = eProductEmpty.FULL,
        //                UncoatedPart = eUnCoatedPart.NA,
        //                WinderDirection = eWinderDirection.UP,
        //                CarrierState = eCarrierState.COMPLETED,
        //                CarryInTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),       //20220526 조숭진 hsms 사양에 맞게 string으로 변경 yyyyMMddHHmmssfff      
        //            };
        //            CarrierStorage.Instance.InsertCarrier(carrier);
        //            SItem.UpdateCarrier(carrier.CarrierID, true, true);
        //            SItem.NotifyShelfStatusChanged();
        //            //S6F11 CarrierIDRead CEID 601
        //            GlobalData.Current.HSMS.SendS6F11(601, "JOBDATA", carrier);
        //            //S6F11 ZoneCaapacityChange CEID 310
        //            GlobalData.Current.HSMS.SendS6F11(310, "ZONENAME", SItem.iZoneName);
        //        }
        //    });

        //}

        /// <summary>
        /// MCS 에서 캐리어 생성 응답이 왔을경우 처리
        /// </summary>
        /// <param name="ShelfID"></param>
        /// <param name="cID"></param>
        public void HostCommand_CarrierInstall(string ShelfID, string cID)
        {
            Task.Factory.StartNew(() =>
            {
                Thread.Sleep(200);
                ShelfItem SItem = GetShelf(ShelfID);
                if (SItem != null)
                {
                    CarrierItem carrier = new CarrierItem()
                    {
                        CarrierID = cID,
                        CarrierLocation = SItem.iLocName,
                        CarrierSize = SItem.ShelfType.Equals(eShelfType.Short) ? eCarrierSize.Short : eCarrierSize.Long,
                        CarrierType = "Plate",
                        LotID = "",     //Lot정보 기입도 해야하나?
                        ProductQuantity = 500,
                        Polarity = ePolarity.ANODE,
                        InnerTrayType = eInnerTrayType.NONE,
                        ProductEmpty = eProductEmpty.FULL,
                        UncoatedPart = eUnCoatedPart.NA,
                        WinderDirection = eWinderDirection.NONE,
                        CarrierState = eCarrierState.COMPLETED,
                        CarryInTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),       //20220526 조숭진 hsms 사양에 맞게 string으로 변경 yyyyMMddHHmmssfff      
                    };

                    CarrierStorage.Instance.InsertCarrier(carrier);
                    SItem.UpdateCarrier(carrier.CarrierID, true, true);
                    SItem.NotifyShelfStatusChanged();

                    McsJob TempJob = new McsJob();
                    TempJob.CarrierID = carrier.CarrierID;

                    //S6F11 CarrierIDRead CEID 301 CarrierInstallCompleted
                    GlobalData.Current.HSMS.SendS6F11(301, "JOBDATA", TempJob , "SHELFITEM", SItem);

                    //S6F11 CarrierIDRead CEID 312
                    //GlobalData.Current.HSMS.SendS6F11(312, "JOBDATA", carrier);

                    //S6F11 ZoneCaapacityChange CEID 310
                    GlobalData.Current.HSMS.SendS6F11(310, "ZONENAME", SItem.iZoneName);

                }
            });

        }

        public void RequestCarrierRemove(string ShelfID)
        {
            ShelfItem SItem = GetShelf(ShelfID);
            RequestCarrierRemove(SItem);
        }

        public void RequestCarrierRemove(ShelfItem TargetShelf)
        {
            Task.Factory.StartNew(() =>
            {
                Thread.Sleep(200);
            
              if (TargetShelf != null)
              {
                  CarrierItem ReportCarrier = TargetShelf.InSlotCarrier;
                  if(ReportCarrier == null)
                  {
                      return;
                  }
                  TargetShelf.RemoveSCSCarrierData(); //STK 도메인 삭제


                    //SuHwan_20230202 : [ServerClient] 
                    GlobalData.Current.ShelfMgr.SaveShelfData(TargetShelf);
              }

             });
        }

        public bool CheckCarrierDuplicated(string CarrierID)
        {
            ShelfItem DupInShelf = GetShelfItemByCarrierID(CarrierID);
            if (DupInShelf != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void ProcessCarrierDuplicated(string DupCarrierID)
        {
            ShelfItem DupInShelf = GetShelfItemByCarrierID(DupCarrierID);
            if (DupInShelf != null)
            {
                ePalletSize BeforePalletSize = DupInShelf.PalletSize; //240719 RGJ 듀플리케이션 발생시 PalletSize 보존.

                CarrierItem RenameCarrier = DupInShelf.InSlotCarrier;

                //DB 예외발생으로 수정함
                DupInShelf.RemoveSCSCarrierData();//STK 도메인 삭제 //한번 삭제했다가
                Thread.Sleep(50);
                //완전히 새로 생성해서 다시 투입
                CarrierItem DupCarrier = CarrierStorage.Instance.CreateDUPUnknownCarrier(DupInShelf.TagName, DupCarrierID, RenameCarrier.CarrierSize); //새로 중복 캐리어 생성.
                DupCarrier.PalletSize = BeforePalletSize; //240719 RGJ 듀플리케이션 발생시 PalletSize 보존.
                CarrierStorage.Instance.InsertCarrier(DupCarrier); //다시 캐리어 추가
                DupInShelf.UpdateCarrier(DupCarrier.CarrierID);
                GlobalData.Current.ShelfMgr.SaveShelfData(DupInShelf); //240806 RGJ 쉘프 디비 업데이트 추가.

                //보고용 임시잡 변수 생성
                McsJob TempJob = new McsJob();
                TempJob.CarrierID = RenameCarrier.CarrierID;
                GlobalData.Current.HSMS.SendS6F11(301, "JOBDATA", TempJob, "SHELFITEM", DupInShelf); //S6F11 CEID 301 CarrierInstallCompleted

                GlobalData.Current.HSMS.SendS6F11(310, "ZONENAME", DupInShelf.iZoneName); //S6F11 ZoneCapacityChanged Report 310
            }
        }

        /// <summary>
        /// 공출고 발생시 사양 변경으로 추가.
        /// </summary>
        /// <param name="EmptyCarrierID">목표였던 캐리어 아이디</param>
        public void ProcessCarrierSourceEmpty(McsJob Job, string EmptyCarrierID)
        {
            ShelfItem SouceShelf = GetShelfItemByCarrierID(EmptyCarrierID);
            if (SouceShelf != null)
            {
                CarrierItem RenameCarrier = SouceShelf.InSlotCarrier;

                //DB 예외발생으로 수정함
                SouceShelf.RemoveSCSCarrierData();//STK 도메인 삭제 //한번 삭제했다가
                Thread.Sleep(50);
                //완전히 새로 생성해서 다시 투입
                CarrierItem EmptyCarrier = CarrierStorage.Instance.CreateSourceEmptyCarrier(SouceShelf.TagName, EmptyCarrierID, RenameCarrier.PalletSize); //새로 중복 캐리어 생성.
                CarrierStorage.Instance.InsertCarrier(EmptyCarrier); //다시 캐리어 추가
                SouceShelf.UpdateCarrier(EmptyCarrier.CarrierID);
                GlobalData.Current.ShelfMgr.SaveShelfData(SouceShelf); //240806 RGJ 쉘프 디비 업데이트 추가.
                GlobalData.Current.HSMS.SendS6F11(301, "JOBDATA", Job,"SHELFITEM", SouceShelf); //S6F11 CEID 301 CarrierInstallCompleted

                GlobalData.Current.HSMS.SendS6F11(310, "ZONENAME", SouceShelf.iZoneName); //S6F11 ZoneCapacityChanged Report 310
            }
        }

        /// <summary>
        /// ZoneName 과 매칭 되는 쉘프 존재 유무 체크
        /// </summary>
        /// <param name="ZoneName"></param>
        /// <returns></returns>
        public bool CheckShelfZoneNameExist(string ZoneName)
        {
            foreach (var sItem in AllData)
            {
                if (sItem.ZONE == ZoneName)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 쉘프생성 시 Zone Name, Shelf Type 체크
        /// </summary>
        /// <returns></returns>
        public bool CreateCheckShelfData()
        {
            foreach (var sItem in AllData)
            {
                if (sItem.DeadZone == false &&
                    (sItem.ZONE == string.Empty || sItem.ShelfType == eShelfType.Unknown))
                {
                    return false;
                }
            }

            return true;
        }

        //뱅크당 하나씩 핸드오버용 보호 쉘프를 설정한다.
        public void SetProtectShelf()
        {
            int MidBay = (GetMinBay() + GetMaxBay())/2;
            int MidLevel = (GetMinLevel() + GetMaxLevel()) / 2;
            var Shelfbank1 = GetShelf(1, MidBay, MidLevel);
            var Shelfbank2 = GetShelf(2, MidBay, MidLevel);
            if(Shelfbank1 != null)
            {
                Shelfbank1.SetHandoverProtect(true);
                LogManager.WriteConsoleLog(eLogLevel.Info, "Set Shelf: {0} as Protect Shelf for Handover ", Shelfbank1.TagName);
            }
            if (Shelfbank2 != null)
            {
                Shelfbank2.SetHandoverProtect(true);
                LogManager.WriteConsoleLog(eLogLevel.Info, "Set Shelf: {0} as Protect Shelf for Handover ", Shelfbank2.TagName);
            }

            //2024.08.12 lim,  Handover 주변을 마지막으로 체우도록 영역 설정 일단 1로 사용 
            int AreaSize = 1;
            for (int i = -AreaSize; i <= AreaSize; i++)
            {
                for (int j = -AreaSize; j <= AreaSize; j++)
                {
                    var Sbank1 = GetShelf(1, MidBay + i, MidLevel + j);
                    var Sbank2 = GetShelf(2, MidBay + i, MidLevel + j);

                    if (Sbank1 != null)
                    {
                        Sbank1.SetHandOverArea(true);
                        LogManager.WriteConsoleLog(eLogLevel.Info, "Set Shelf: {0} as Set Handover Area", Sbank1.TagName);
                    }
                    if (Sbank2 != null)
                    {
                        Sbank2.SetHandOverArea(true);
                        LogManager.WriteConsoleLog(eLogLevel.Info, "Set Shelf: {0} as Set Handover Area", Sbank2.TagName);
                    }
                }
            }
        }

        #region PlayBack

        #endregion

        #region 통계지원
        public double GetShelfLoadRatio()
        {
            double AccessAbleShelfCount = AllData.Count(s => s.ShelfAvailable);
            double OccupiedShelfCount = AllData.Count(s => s.CheckCarrierExist());
            if(AccessAbleShelfCount <= double.Epsilon)
            {
                return 0;
            }
            return (OccupiedShelfCount / AccessAbleShelfCount) * 100;
        }
        public int GetAvailableShelfCount()
        {
            return  AllData.Count(s => s.ShelfAvailable);

        }
        public int GetOccupiedShelfCount()
        {
            return AllData.Count(s => s.CheckCarrierExist() && s.SHELFUSE);   //2024.09.20 lim, 미사용인 자재는 제외 Load rate 가 100 넘어감

        }

        public int GetUNKIDShelfCount()
        {
            return AllData.Count(s => s.CarrierID.Contains("UNK") && s.SHELFUSE);   //2024.09.20 lim, 미사용인 자재는 제외 Load rate 가 100 넘어감
        }

        public int GetFullCarrierCount()
        {
            return AllData.Count(s => s.GetProductEmpty() == eProductEmpty.FULL && s.SHELFUSE);     //2024.09.20 lim, 미사용인 자재는 제외 Load rate 가 100 넘어감
        }
        public int GetEmptyCarrierCount()
        {
            return AllData.Count(s => s.GetProductEmpty() == eProductEmpty.EMPTY && s.SHELFUSE);    //2024.09.20 lim, 미사용인 자재는 제외 Load rate 가 100 넘어감
        }
        public int GetNoneCarrierCount()
        {
            return AllData.Count(s => s.GetProductEmpty() == eProductEmpty.NONE && s.CheckCarrierExist() && s.SHELFUSE);    //2024.09.20 lim, 미사용인 자재는 제외 Load rate 가 100 넘어감
        }

        public string GetLoadRatioMessage()
        {
            double AccessAbleShelfCount = GetAvailableShelfCount();
            double OccupiedShelfCount = GetOccupiedShelfCount();
            double LoadRatio;
            if (AccessAbleShelfCount <= double.Epsilon)
            {
                LoadRatio = 0;
            }
            else
            {
                LoadRatio = (OccupiedShelfCount / AccessAbleShelfCount) * 100;
            }
            return string.Format("{0:0.00}%  ({1}/{2})", LoadRatio, OccupiedShelfCount, AccessAbleShelfCount);
        }

        #region 쉘프 여러 개이고 쉘프별로 zonename이 다를 때 사용.
        public void ReportShelfDisable(List<string> ListZoneName)
        {
            for (int i = 0; i < ListZoneName.Count; i++)
            {
                GlobalData.Current.HSMS.SendS6F11(903, "ZONENAME", ListZoneName[i]);
                GlobalData.Current.HSMS.SendS6F11(310, "ZONENAME", ListZoneName[i]);
            }
        }
        #endregion
        #endregion
    }
}
