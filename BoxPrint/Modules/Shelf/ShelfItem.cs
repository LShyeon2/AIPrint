using BoxPrint.Database;
using BoxPrint.DataList;
using BoxPrint.GUI.UIControls;   //220509 HHJ SCS 개선     //- ShelfControl 변경
using System;

namespace BoxPrint.Modules.Shelf
{
    //220524 HHJ SCS 개선     //- Shelf Xml제거
    public class ShelfItem : ControlBase, ICarrierStoreAble
    {
        private static int OldCarrierPeriod = 30; //오래된 캐리어로 설정할 기간 => Defalut 30일


        private static void SetOldCarrierPeriod(int Period)
        {
            OldCarrierPeriod = Period;
        }

        #region Propertys
        private int _number;
        public int number
        {
            get => _number;
            set
            {
                if (_number == value) return;

                _number = value;
            }
        }
        public string TagName { get; private set; }

        private string _ShelfZoneName;
        public string ZONE
        {
            get
            {
                if (string.IsNullOrEmpty(_ShelfZoneName)) //혹시 ZoneName 정의 안되어 있으면 기본값 사용
                {
                    return ShelfManager.Instance.DefaultShelfZoneName;
                }
                else
                {
                    return _ShelfZoneName;
                }

            }
            set
            {
                if (_ShelfZoneName == value) return;

                _ShelfZoneName = value;
            }
        }

        protected bool _DeadZone;
        public bool DeadZone
        {
            get { return _DeadZone; }
            set
            {
                if (_DeadZone == value) return;

                _DeadZone = value;
                RaisePropertyChanged("DeadZone");
            }
        }

        public eShelfState ShelfState
        {
            get
            {
                if (SHELFUSE && !SHELFBLOCK && !DeadZone)
                {
                    return eShelfState.IN_SERVICE;
                }
                else
                {
                    return eShelfState.OUT_OF_SERVICE;
                }
            }
        }

        //SuHwan_20221005 : [ServerClient]
        protected eShelfStatus _ShelfStatus;
        public eShelfStatus ShelfStatus
        {
            get
            {
                //SuHwan_20221005 : [ServerClient]
                if (GlobalData.Current.ServerClientType == eServerClientType.Client)
                    return _ShelfStatus;
                else
                    return GetShelfStatus();
            }
            set
            {
                if (_ShelfStatus != value)
                {
                    _ShelfStatus = value;
                    RaisePropertyChanged("ShelfStatus");
                }
            }
        }

        public eShelfHSMSStatus ShelfHSMSStatus
        {
            get
            {
                eShelfStatus Shelf_Status = GetShelfStatus();
                if (Shelf_Status == eShelfStatus.RESERVED_GET || Shelf_Status == eShelfStatus.RESERVED_PUT) //2305030 RGJ  ShelfHSMSStatus 예약먼저로 순서 조정
                {
                    return eShelfHSMSStatus.RESERVED;
                }
                else
                {
                    if(CheckCarrierExist())
                    {
                        return eShelfHSMSStatus.OCCUPIED;
                    }
                    else
                    {
                        return eShelfHSMSStatus.EMPTY;
                    }
                }
            }
        }
        public int RUNSTATE { get; set; }

        //231012 RGJ 핸드오버 프로텍트 쉘프 지정.
        private bool _HandOverProtect = false;
        public bool HandOverProtect
        {
            get
            {
                return _HandOverProtect;
            }
            private set
            {
                _HandOverProtect = value;
            }
        }

        //2024.08.12 lim, Handover 영역 설정 (나머지 공간 사용 후 마지막에 체운다)
        private bool _HandOverArea = false;
        public bool HandOverArea
        {
            get
            {
                return _HandOverArea;
            }
            private set
            {
                _HandOverArea = value;
            }
        }

        //220525 HHJ SCS 개선     //- ShelfItem 개선
        private string _UICarrierID = "";
        public string CarrierID
        {
            get
            {
                return GetCarrierID();
            }
            set
            {
                _UICarrierID = value;
            }
        }

        private eProductEmpty _ProductEmpty = eProductEmpty.NONE;
        public eProductEmpty ProductEmpty
        {
            get
            {
                return GetProductEmpty();
            }
            set
            {
                _ProductEmpty = value;
            }
        }

        private eCarrierSize _UICarrierSize = eCarrierSize.Unknown;
        public eCarrierSize CarrierSize
        {
            get
            {
                return GetCarrierSize();
            }
            set
            {
                _UICarrierSize = value;
            }
        }
        private eCarrierHeight _UICarrierHeight = eCarrierHeight.NONE;
        public eCarrierHeight CarrierHeight
        {
            get
            {
                return GetCarrierHeight();
            }
            set
            {
                _UICarrierHeight = value;
            }
        }
        public string GetCarrierID()
        {
            CarrierItem Temp = InSlotCarrier;
            if (Temp != null)
            {
                return Temp.CarrierID;
            }
            else
            {
                //220525 HHJ SCS 개선     //- ShelfItem 개선
                //return _CarrierID;
                return string.Empty;
            }
        }
        public eCarrierSize GetCarrierSize()
        {
            CarrierItem Temp = InSlotCarrier;
            if (Temp != null)
            {
                return Temp.CarrierSize;
            }
            else
            {
                return eCarrierSize.Unknown;
            }
        }
        public eCarrierHeight GetCarrierHeight()
        {
            CarrierItem Temp = InSlotCarrier;
            if (Temp != null)
            {
                return Temp.CarrierHeight;
            }
            else
            {
                return eCarrierHeight.NONE;
            }
        }
        public string GetCarrierHSMSPalletSize()
        {
            CarrierItem Temp = InSlotCarrier;
            if (Temp != null)
            {
                return Temp.HSMSPalletSize;
            }
            else
            {
                return "";
            }
        }
        public eWinderDirection GetCarriereWinderDirection()
        {
            CarrierItem Temp = InSlotCarrier;
            if (Temp != null)
            {
                return Temp.WinderDirection;
            }
            else
            {
                return eWinderDirection.NONE;
            }
        }

        public eProductEmpty GetProductEmpty()
        {
            CarrierItem Temp = InSlotCarrier;
            if (Temp != null)
            {
                return Temp.ProductEmpty;
            }
            else
            {
                return eProductEmpty.NONE;
            }
        }

        public string GetCarrierInTime()
        {
            CarrierItem Temp = InSlotCarrier;
            if (Temp != null)
            {
                return Temp.CarryInTime;
            }
            else
            {
                return string.Empty;
            }
        }
        protected eShelfBusyRm _ShelfBusyRm = eShelfBusyRm.Unknown;
        public eShelfBusyRm ShelfBusyRm
        {
            get { return _ShelfBusyRm; }
            set
            {
                if (_ShelfBusyRm == value) return;

                _ShelfBusyRm = value;
                RaisePropertyChanged("ShelfBusyRm");
            }
        }

        //230306
        public ePalletSize GetPalletSize()
        {
            CarrierItem Temp = InSlotCarrier;

            if (Temp != null)
            {
                return Temp.PalletSize;
            }
            else
            {
                return ePalletSize.NONE;
            }
        }

        /// <summary>
        /// 쉘프 블럭 프로퍼티 추가.
        /// 디비에는 저장하지 않고 스케쥴러가 필요시 변경해서 사용함
        /// 원 크레인 모드 사용시  기존 SHELFUSE를 변경해서는 안되므로 따로 추가해서 사용함. 
        /// </summary>
        protected bool _SHELFBLOCK;
        public bool SHELFBLOCK
        {
            get { return _SHELFBLOCK; }
            set
            {
                if (_SHELFBLOCK == value)
                {
                    return;
                }
                _SHELFBLOCK = value;
                SHELFUSE = !value; //임시
                {
                    if (_SHELFBLOCK) //쉘프가 블럭 되면 OutOfService
                    {
                        //S6f11 ShelfOutOfService CEID 902
                        GlobalData.Current.HSMS.SendS6F11(902, "SHELFITEM", this);
                    }
                    else
                    {
                        if (SHELFUSE) //쉘프가 블럭이 풀려도  SHELFUSE 가 On 상태여야 InService 보고
                        {
                            //S6f11 ShelfInService CEID 901
                            GlobalData.Current.HSMS.SendS6F11(901, "SHELFITEM", this);
                        }
                    }

                    //220803 조숭진 추가
                    GlobalData.Current.HSMS.SendS6F11(310, "ZONENAME", iZoneName);
                }

                RaisePropertyChanged("SHELFBLOCK");
                NotifyShelfStatusChanged();

            }
        }

        protected bool _SHELFUSE;
        public bool SHELFUSE
        {
            get { return _SHELFUSE; }
            set
            {
                if (_SHELFUSE == value)
                {
                    return;
                }
                _SHELFUSE = value;
                {
                    if (_SHELFUSE)
                    {
                        //S6f11 ShelfInService CEID 901
                        GlobalData.Current.HSMS.SendS6F11(901, "SHELFITEM", this);
                    }
                    else
                    {
                        //Shelf 비사용 할때 예약을 해제 한다.
                        Scheduled = false;
                        //S6f11 ShelfOutOfService CEID 902
                        GlobalData.Current.HSMS.SendS6F11(902, "SHELFITEM", this);
                    }

                    //220803 조숭진 추가
                    GlobalData.Current.HSMS.SendS6F11(310, "ZONENAME", iZoneName);
                }

                RaisePropertyChanged("SHELFUSE");
                NotifyShelfStatusChanged();
            }
        }

        /// <summary>
        /// 22.10.24 RGJ 쉘프 가용가능 프로퍼티 하나로 합침
        /// </summary>
        public bool ShelfAvailable
        {
            get { return _SHELFUSE && !_SHELFBLOCK && !_DeadZone; }
        }




        public eCarrierState CarrierState
        {
            get
            {
                CarrierItem cItem = InSlotCarrier;
                if (cItem != null)
                {
                    return cItem.CarrierState;
                }
                else
                {
                    return eCarrierState.NONE;
                }
            }
        }

        public bool USESMOKESENSOR { get; set; }


        protected eShelfType _ShelfType;
        public eShelfType ShelfType
        {
            get => _ShelfType;
            set
            {
                if (_ShelfType == value) return;

                _ShelfType = value;

                SHELFTYPE = (int)value;//SuHwan_20230202 : [ServerClient]

                RaisePropertyChanged("ShelfType");
            }
        }

        private int _SHELFTYPE;
        public int SHELFTYPE
        {
            get => _SHELFTYPE;
            set
            {
                if (_SHELFTYPE == value) return;
                _SHELFTYPE = value;
                ShelfType = (eShelfType)value;
            }
        }

        //231101 HHJ Shelf NG State 추가
        private int _ShelfNGState;
        public int ShelfNGState
        {
            get => _ShelfNGState;
            set
            {
                if (_ShelfNGState == value) return;

                _ShelfNGState = value;

                RaisePropertyChanged("ShelfNGState");
            }
        }

        public int TrayHeight { get; set; }

        public bool EmptyRetrievaled { get; set; }
        public bool DoubleStoraged { get; set; }
        public bool GET_BLOCKED { get; set; }

        public bool PUT_BLOCKED { get; set; }

        public bool Scheduled { get; protected set; }

        public eShelfScheduleState ShelfScheduled
        {
            get
            {
                if (Scheduled)
                {
                    if (CheckCarrierExist()) //캐리어 있으면 Get
                    {
                        return eShelfScheduleState.GET_SCHEDULED;
                    }
                    else
                    {
                        return eShelfScheduleState.PUT_SCHEDULED;
                    }
                }
                return eShelfScheduleState.NONE;
            }
        }

        public bool Removed { get; set; }       //220523 조숭진 hsms s2계열 메세지 추가

        private int _ShelfBank = -1;
        public int ShelfBank
        {
            get
            {
                if (_ShelfBank <= 0) //처음에만 태그에서 불러오고 나중에는 저장된값을 바로 리턴
                {
                    //220420 HHJ SCS 개선     //- ShelfTagHelper 추가
                    _ShelfBank = ShelfTagHelper.GetBank(TagName);
                    return _ShelfBank;
                }
                else
                {
                    return _ShelfBank;
                }
            }
        }

        private int _ShelfBay = -1;
        public int ShelfBay
        {
            get
            {
                if (_ShelfBay <= 0) //처음에만 태그에서 불러오고 나중에는 저장된값을 바로 리턴
                {
                    //220420 HHJ SCS 개선     //- ShelfTagHelper 추가
                    _ShelfBay = ShelfTagHelper.GetBay(TagName);
                    return _ShelfBay;
                }
                else
                {
                    return _ShelfBay;
                }
            }
        }

        private int _ShelfLevel = -1;
        public int ShelfLevel
        {
            get
            {
                if (_ShelfLevel <= 0) //처음에만 태그에서 불러오고 나중에는 저장된값을 바로 리턴
                {
                    //220420 HHJ SCS 개선     //- ShelfTagHelper 추가
                    _ShelfLevel = ShelfTagHelper.GetLevel(TagName);
                    return _ShelfLevel;
                }
                else
                {
                    return _ShelfLevel;
                }
            }
        }

        //220705 조숭진 datetime에서 string으로 변경.
        private string _InstallTime;
        public string InstallTime
        {
            get
            {
                return _InstallTime;
            }
            set
            {
                if (_InstallTime == value) return;
                _InstallTime = value;
            }
        }

        private int _FloorNum = 1;
        public int FloorNum
        {
            get
            {
                return _FloorNum;
            }
            set
            {
                _FloorNum = value;
            }
        }

        private string _ShelfMemo = string.Empty;
        public string ShelfMemo
        {
            get
            {
                return _ShelfMemo;
            }
            set
            {
                if (_ShelfMemo == value)
                {
                    return;
                }
                else
                {
                    _ShelfMemo = value;
                    GlobalData.Current.DBManager.DbSetProcedureShelfInfo(this);
                    RaisePropertyChanged("ShelfMemo");
                }
            }
        }

        //220512 조숭진 화재감지 db추가 s
        #region 화재감지 관련 변수
        private string _eventinfo;
        public string eventinfo
        {
            get
            {
                return _eventinfo;
            }
            set
            {
                value = value?.Trim(); //Null 예외 처리.
                _eventinfo = value;
                //230316 센서에러상태 확인
                if(_eventinfo == "ERROR")
                {
                    FireEventInfo = eFireEventInfo.ERROR;
                }
                else if(_eventinfo == "FIRE")
                {
                    FireEventInfo = eFireEventInfo.FIRE;
                }
                else if(_eventinfo == "SMOKE")
                {
                    FireEventInfo = eFireEventInfo.SMOKE;
                }
                else if (_eventinfo == "SENSOR_NOTHING")
                {
                    FireEventInfo = eFireEventInfo.SENSOR_NOTHING;
                }
                else
                {
                    FireEventInfo = eFireEventInfo.NONE;
                }
            }
        }

        //230911 RGJ DB 에서 올려주는 이벤트를 Enum 으로 치환해서 보관
        private eFireEventInfo _FireEventInfo;
        public eFireEventInfo FireEventInfo
        {
            get
            {
                return _FireEventInfo;
            }
            set
            {
                //if(_FireEventInfo != value)
                {
                    _FireEventInfo = value;
                    RaisePropertyChanged("FireEventInfo");
                }
            }
        }

        //230316 센서에러상태 확인
        private bool _FireSensorError;
        public bool FireSensorError
        {
            get
            {
                return _FireSensorError;
            }
            set
            {
                if (_FireSensorError == value) return;
                _FireSensorError = value;
            }
        }

        private string _curtemp;
        public string curtemp
        {
            get
            {
                return _curtemp;
            }
            set
            {
                if (_curtemp == value) return;
                _curtemp = value;
                RaisePropertyChanged("curtemp");
            }
        }

        private string _smokesense;
        public string smokesense
        {
            get
            {
                return _smokesense;
            }
            set
            {
                if (_smokesense == value) return;
                _smokesense = value;
            }
        }

        private string _warntemp;
        public string warntemp
        {
            get
            {
                return _warntemp;
            }
            set
            {
                if (_warntemp == value) return;
                _warntemp = value;
            }
        }

        private string _dangertemp;
        public string dangertemp
        {
            get
            {
                return _dangertemp;
            }
            set
            {
                if (_dangertemp == value) return;
                _dangertemp = value;
            }
        }

        private DateTime _eventtime;
        public DateTime eventtime
        {
            get
            {
                return _eventtime;
            }
            set
            {
                _eventtime = value;
            }
        }
        #endregion
        //220512 조숭진 화재감지 db추가 e


        public bool NeedFireAlarmReport;
        //220513 HHJ SCS 개선     //- 연기감지 템플릿 추가
        //private bool FireSensorValue; //실제 화재감시 서버에서 올려준 상태값
        protected bool _FireSensorValue;
        public bool FireSensorValue
        {
            get
            {
                if(GlobalData.Current.GlobalSimulMode && GlobalData.Current.ServerInstance)
                {
                    return SimulSmokeSensorValue;
                }
                else
                {
                    return _FireSensorValue;
                }
            }
            set
            {
                if (_FireSensorValue.Equals(value))
                {
                    return;
                }
                _FireSensorValue = value;
                NeedFireAlarmReport = value;
                GlobalData.Current.DBManager.DbSetProcedureShelfInfo(this); //240820 RGJ 화재 상태 클라이언트 표시 추가.
                RaisePropertyChanged("FireSensorValue");
            }
        }

        protected bool _SimulSmokeSensorValue;
        public bool SimulSmokeSensorValue
        {
            get => _SimulSmokeSensorValue;
            set
            {
                if (_SimulSmokeSensorValue.Equals(value)) return;
                _SimulSmokeSensorValue = value;
                NeedFireAlarmReport = value;
                RaisePropertyChanged("SimulSmokeSensorValue");
            }
        }
        #endregion

        #region Constructor
        public ShelfItem(string controlname) : base(controlname, 1)
        {
            TagName = controlname;
            USESMOKESENSOR = true; //임시 화재
        }
        #endregion

        #region ICarrierStoreAble Interface
        public bool UpdateCarrier(string CarrierID, bool DBUpdate = true, bool HostReq = false)
        {
            //220509 HHJ SCS 개선     //- ShelfControl 변경
            //Carrier가 있던 없던 무조건 업데이트 진행한다.

            DefaultSlot.SetCarrierData(CarrierID, DBUpdate);
            DefaultSlot.SetCarrierExist(true);  //Carrier가 없으면 빈값으로의 변경이 되어야함.

            if (HostReq)
            {
                GlobalData.Current.ShelfMgr.SaveShelfData(this);
            }
            NotifyShelfStatusChanged();
            return true;
        }

        public bool ResetCarrierData()
        {
            try
            {
                DefaultSlot.DeleteSlotCarrier();
                return true;
            }
            catch (Exception ex)
            {
                Log.LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
                return false;
            }
        }


        public bool RemoveSCSCarrierData()
        {
           return RemoveSCSCarrierData("");
        }
        /// <summary>
        /// 쉘프에서 삭제 하고 스토리지에서도 삭제.
        /// </summary>
        /// <param name="RelateJobID"></param>
        /// <returns></returns>
        /// 
        public bool RemoveSCSCarrierData(string RelateJobID = "")
        {
            CarrierItem RemovedCarrier = InSlotCarrier;
            if (RemovedCarrier == null)
            {
                return false;
            }
            DefaultSlot.DeleteSlotCarrier();
            NotifyShelfStatusChanged(); //플레이백 로그 여기서 같이 처리.
            CarrierStorage.Instance.RemoveStorageCarrier(RemovedCarrier.CarrierID);
            //S6F11 CarrierRemoveCompleted CEID 302
            GlobalData.Current.HSMS.SendS6F11(302, "JOBID", RelateJobID, "CARRIERITEM", RemovedCarrier, "SHELFITEM", this);
            //S6F11 ZoneCaapacityChange CEID 310
            GlobalData.Current.HSMS.SendS6F11(310, "ZONENAME", iZoneName);
            return true;
        }

        public bool CheckCarrierExist()
        {
            //240820 불필요 if 문 제거.
            //if (SHELFTYPE == (int)eShelfType.FireWaterPool)
            //{
            //    return false;
            //}
            //else
            //{
            //    return DefaultSlot.MaterialExist;
            //} 
            return DefaultSlot.MaterialExist;
        }
        public bool TransferCarrierData(ICarrierStoreAble To)
        {
            if (CheckCarrierExist())
            {
                string CID = GetCarrierID();
                //220711 조숭진 false -> true로 변경
                //DefaultSlot.RemoveCarrierData(false);
                DefaultSlot.DeleteSlotCarrier();
                To.UpdateCarrier(CID);
            }
            return true;
        }



        public void NotifyScheduled(bool Reserved, bool init = false)       //221012 조숭진 init 인자 추가...
        {
            if (Scheduled != Reserved) //예약 상태 변경이 될때만 이벤트 및 플레이백 로그추가.
            {
                Scheduled = Reserved;

                NotifyShelfStatusChanged(); //플레이백 로그 여기서 같이 처리.
                if (!init)
                {
                    GlobalData.Current.ShelfMgr.SaveShelfData(this);
                }
            }
            RaisePropertyChanged("ShelfStatus"); //프로퍼티 변경 이벤트는 발생시킨다.
        }

        public int iBank
        {
            get { return ShelfBank; }
        }
        public int iBay
        {
            get { return ShelfBay; }
        }
        public int iLevel
        {
            get { return ShelfLevel; }
        }
        public short iWorkPlaceNumber
        {
            get { return 0; }
        }

        //230306
        public ePalletSize PalletSize
        {
            get { return GetPalletSize(); }
        }

        public string iGroup
        {
            get { return "SHELF"; }
        }

        public string iLocName
        {
            get { return TagName; }
        }
        public string iZoneName
        {
            get { return ZONE; }
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
        public int CalcBayDistance(int a, int b)
        {
            int c = b - a;
            return c > 0 ? c : -c;
        }
        public string GetTagName()
        {
            return TagName;
        }


        public bool CheckGetAble(string CarrierID)
        {
            bool bCarrierExist = CheckCarrierExist();  //재하 유무
            bool bCarrierIDMatch = CarrierID == this.CarrierID; //230309 CheckGetAble CarrierID 매칭 추가.
            //241018 HoN 화재시나리오 운영 추가       //2. Disable Shelf도 화재 발생하면 수행해야한다.
            if (FireSensorValue)
            {
                //화재 쉘프인 경우는 미사용인 상태에서도 가능해야한다.
                //ShelfAvailable을 빼주고 해당 조건을 풀어 _SHELFUSE 제거 (ShelfAvailable기존 조건 -> _SHELFUSE && !_SHELFBLOCK && !_DeadZone -> _SHELFUSE 제거)
                return bCarrierExist && Scheduled && bCarrierIDMatch && !_SHELFBLOCK && !_DeadZone;
            }
            return bCarrierExist && Scheduled && ShelfAvailable && bCarrierIDMatch; //쉘프 상태
        }
        public bool CheckPutAble()
        {
            bool bShelfEmpty = !CheckCarrierExist(); //재하 유무
            return bShelfEmpty && Scheduled && ShelfAvailable; //쉘프 상태
        }
        public bool CheckCarrierSizeAcceptable(eCarrierSize Size)
        {
            if (Size == eCarrierSize.Unknown) //Size 인자가 Unknown 이면 처리 불능
            {
                return false;
            }
            if (ShelfType == eShelfType.Both) //Both Shelf 항상 가능
            {
                return true;
            }
            if (ShelfType == eShelfType.Long && Size == eCarrierSize.Long) //Long Shelf 에는 Long Carrier만 넣는다. 
            {
                return true;
            }
            if (ShelfType == eShelfType.Short && Size == eCarrierSize.Short) //Short Shelf  에는 Short Carrier만 넣는다. 
            {
                return true;
            }
            return false;
        }

        public override int GetUnitServiceState()
        {
            if (ShelfAvailable)
            {
                return 2;
            }
            else
            {
                return 1;
            }
        }
        #endregion

        public bool CheckModuleAlarmState()
        {
            return false;
        }

        #region Member Method
        public void SetHandoverProtect(bool value) //231012 RGJ 핸드오버 프로텍트 쉘프 지정.
        {
            HandOverProtect = value;
        }

        public void SetHandOverArea(bool value) //2024.08.12 lim, Handover 영역 설정
        {
            HandOverArea = value;
        }

        public void SetFireSensorState(bool value)
        {
            FireSensorValue = value;
        }

        public void SetFireSensorError(bool value)
        {
            FireSensorError = value;
        }

        public void SetSimulSmokeSensor(bool value)
        {
            SimulSmokeSensorValue = value;
            FireSensorValue = value;
        }
        public bool CheckSmokeSensor()
        {
            //241018 HoN 화재시나리오 운영 추가       //2. Disable Shelf도 화재 발생하면 수행해야한다.
            //if (DeadZone || !SHELFUSE) //없는 쉘프이거나 비사용 쉘프는 연기감지 체크안함.
            if (DeadZone)
            {
                return false;
            }
            if (USESMOKESENSOR)
            {
                return FireSensorValue;
            }
            else
            {
                return false;
            }
        }

        public bool CarrierAgeOver()
        {
            if (CheckCarrierExist())
            {
                DateTime CarryIn = DateTime.Parse(InSlotCarrier.CarryInTime);
                return (DateTime.Now - CarryIn) > TimeSpan.FromDays(OldCarrierPeriod);
            }
            else
            {
                return false;
            }
        }
        public eShelfStatus GetShelfStatus()
        {
            //EMPTY = 0,          //대기
            //RESERVED_PUT = 1,   //입고예약
            //RESERVED_GET = 2,   //출고예약
            //OCCUPIED = 3,       //적재됨
            //BLOCKED_PUT = 4,    //입고금지
            //BLOCKED_GET = 5,    //출고금지
            //NOT_USE = 6,        //사용금지
            //DOUBLE_STORAGE = 7, //더블에러
            //SOURCE_EMPTY = 8,   //공출고
            //UNKSHELF = 9, //언노운 캐리어
            bool CarrierExist = CheckCarrierExist();
            //금지상태 부터 표시
            if (!SHELFUSE)
            {
                return eShelfStatus.NOT_USE;
            }
            if (PUT_BLOCKED)
            {
                return eShelfStatus.BLOCKED_PUT;
            }
            if (GET_BLOCKED)
            {
                return eShelfStatus.BLOCKED_GET;
            }
            //에러 상태 표시
            if (CarrierExist && EmptyRetrievaled)
            {
                return eShelfStatus.SOURCE_EMPTY;
            }
            if (!CarrierExist && DoubleStoraged)
            {
                return eShelfStatus.DOUBLE_STORAGE;
            }

            //예약 상태 표시
            if (Scheduled && CarrierExist)
            {
                return eShelfStatus.RESERVED_GET;
            }
            if (Scheduled && !CarrierExist)
            {
                return eShelfStatus.RESERVED_PUT;
            }

            if (CarrierExist)
            {
                if (this.CarrierID.StartsWith("UNK"))
                {
                    return eShelfStatus.UNKSHELF;
                }
                else
                {
                    //host remove에 의해 shelf data가 지워지면 Removed flag를 true로 변경된다.
                    //그 후 재생성됐을대 Removed flag가 true로 되어있어 s2f41 remove의 s2f42보고가 HCNAckCode3으로 보고되고 shelf data가 삭제된다.
                    //이를 방지하기 위해 하기와 같이 flag를 false로 변경시킨다.
                    if (this.Removed == true)
                    {
                        this.Removed = false;
                    }
                    return eShelfStatus.OCCUPIED;
                }
            }
            return eShelfStatus.EMPTY;

        }
        public void NotifyShelfStatusChanged()
        {
            RaisePropertyChanged("ShelfStatus");
        }


        //220523 조숭진 hsms s2계열 메세지 추가
        public void NotifyRemoved()
        {
            Removed = true;
        }
        #endregion

        #region 쉘프 1개씩 바꿀 때 사용
        public void ReportShelfDisable()
        {
            GlobalData.Current.HSMS.SendS6F11(903, "ZONENAME", iZoneName);
            GlobalData.Current.HSMS.SendS6F11(310, "ZONENAME", iZoneName);
        }
        #endregion

        #region PlayBack Interface
        private bool PlaybackTrace = false; //해당 작업 플레이백을 추적할건지 결정.
        public void SetPlayBackTrace()
        {
            PlaybackTrace = true;
        }
        public bool NeedPlayBackWrite()
        {
            return GlobalData.Current.UsePlayBackLog && PlaybackTrace;
        }


        #endregion
    }
}
