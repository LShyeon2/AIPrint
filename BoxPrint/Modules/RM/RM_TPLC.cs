using BoxPrint.CCLink;
using BoxPrint.Database;
using BoxPrint.DataList;
using BoxPrint.DataList.MCS;
using BoxPrint.Log;
using BoxPrint.Modules.Conveyor;
using BoxPrint.Modules.Shelf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using WCF_LBS.Commands;

namespace BoxPrint.Modules.RM
{
    public class RM_TPLC : RMModuleBase
    {
        //readonly int CommandDelay = 500;
        //readonly int ActionDelay = 1000;

        //220823 조숭진 rmmodulebase로 이동 s
        //readonly int CommandTimeOut = 5;
        //readonly int FireNotifyTimeOut = 2;
        //readonly int PLCIF_Delay = 50;
        //readonly int CraneActionTimeOut = 300;
        //readonly int PLCTimeOut = 3;
        //220823 조숭진 rmmodulebase로 이동 e

        #region SimulFlag
        public short SimulAlarmCode = 0;
        //public bool SimulEmptyRetriveTestMode = false;
        
        //public bool SimulDoubleStorageTestMode = false;

        //private bool SimulEmptyRetriveReset = false;

        //public bool SimulEmptyRetriveON = false;

        //public bool SimulDoubleStorageON = false;

        #endregion


        #region Robot Aalrm         // 2020.11.25 Alarm 테스트 등록및 테스트 작업 

        public const string UNDEFINED_ERROR = "0";
        public const string ROBOT_EMG_ERROR = "1";
        public const string ROBOT_DOUBLE_CMD_ERROR = "2";
        public const string ROBOT_LIMIT_ERROR = "3";
        public const string ROBOT_SYSTEM_ALARM = "4";
        public const string ROBOT_GET_FAIL = "5";
        public const string ROBOT_PUT_FAIL = "6";
        public const string ROBOT_USE_MATERIAL = "7";
        public const string ROBOT_NONE_MATERIAL = "8";
        public const string ROBOT_FOLDING_FAIL = "9";
        public const string ROBOT_CLAMP_FAIL = "10";
        public const string ROBOT_UNCLAMP_FAIL = "11";
        public const string ROBOT_SERVO_OFF = "12";
        public const string ROBOT_DESTINATION_FAIL = "13";
        public const string ROBOT_HOME_ERROR = "14";
        public const string ROBOT_PARAMETER_ERROR = "15";
        public const string HOME_NOT_COMPLETE = "16";
        public const string MATERIAL_SENSOR_NOT_DETECTED = "17";
        public const string ROBOT_TRAY_FB_FRONT_TILT_ERROR = "18"; //트레이 앞 틸트
        public const string ROBOT_TRAY_FB_REAR_TILT_ERROR = "88"; //트레이 뒤 틸트
        public const string SHELF_MATERIAL_STATE_ERROR = "19"; //쉘프 트레이 옆으로 기울어짐
        public const string ROBOT_DRIVE_LIMIT_ERROR = "20";
        public const string ROBOT_LIFT_LIMIT_ERROR = "21";
        public const string ROBOT_TURN_LIMIT_ERROR = "22";
        public const string ROBOT_ARM_LIMIT_ERROR = "23";
        public const string ROBOT_AUTO_TEACHING_FAIL = "24";
        public const string ROBOT_DOOR_OPEN = "25";
        public const string ROBOT_ARM_NOT_HOMEPOSITION = "26";
        public const string ROBOT_LIFT_BELT_ERROR = "30";
        public const string ROBOT_ARM_POSITION_ERROR = "31";
        public const string MATERIAL_DETECTED = "40";
        public const string MATERIAL_NOT_DETECTED = "41";
        public const string INPORT_MATERIAL_CANNOT_SETTLED = "42";
        public const string MATERIAL_HEIGHT_ERROR = "43";
        public const string POSITION_ERROR = "60";
        public const string POSITION_OVER = "99";
        public const string ROBOT_COMMUNICATE_FAIL = "9001";
        public const string ROBOT_CONNECTION_FAIL = "9002";
        #endregion

        #region 소모품 관리

        //주행거리 P변수 정의
        public readonly string pFork_Cycle_MoveDistance = "P1041";           //사이클 포크축 주행거리[mm]
        public readonly string pDrive_Cycle_MoveDistance = "P1042";           //사이클 주행축 주행거리[mm]
        public readonly string pZ_Cycle_MoveDistance = "P1043";           //사이클 높이축 주행거리[mm]
        public readonly string pTurn_Cycle_MoveDistance = "P1044";           //사이클 회전축 주행거리[mm]
        public readonly string pGripper_Cycle_MoveDistance = "P1045";           //사이클 그립축 주행거리[mm]

        #endregion


        public List<IOPoint> RMio { get; private set; }


        #region RM Property 관련 속성

        /// <summary>
        /// 기상반 동작 상태 [오버라이드]
        /// </summary>
        public override eCraneSCState CraneSC_State
        {
            get
            {
                eCraneSCState curValue = PLC_CraneActionState;
                if(_CraneSC_State != curValue )
                {
                    _CraneSC_State = curValue;
                    RaisePropertyChanged("CraneSC_State"); //과거값 대비 변경되면 이벤트 발생.
                }
                return curValue;
            }
        }

        //241001 HDK Crane 작업가능상태 표시 개선
        public override eCraneSCMode CraneSCStatus
        {
            get
            {
                eCraneSCMode curruntValue = PLC_SCMODE;
                if (_CraneSCStatus != curruntValue)
                {
                    _CraneSCStatus = curruntValue;
                    RaisePropertyChanged("CraneSCStatus");

                }
                return curruntValue;
            }
        }

        private bool _RobotOnlineConncet = true;
        public override bool RobotOnlineConncet
        {
            get
            {
                if (SimulMode)
                {
                    return _RobotOnlineConncet;
                }
                else
                {
                    return GData.protocolManager.CheckConnection((short)PLCNumber);
                }
            }
            set
            {
                _RobotOnlineConncet = value;
            }
        }

        private bool _Place_Sensor_Exist;
        public override bool CarrierExistSensor
        {
            get
            {
                if (this.SimulMode)
                {
                    return _Place_Sensor_Exist;
                }
                else
                {
                    return PLC_CarrierExistFork1;
                }
            }
            set
            {
                if (this.SimulMode)
                {
                    _Place_Sensor_Exist = value;

                    //2020.11.26 WCF 접속 보고시 Hand 센서 확인
                    INI_Helper.WriteValue(RMStateFilePath, "STATE", "Place_Sensor_Exist", _Place_Sensor_Exist ? "1" : "0");
                }
            }
        }

        public bool _SimulForkFireDetect = false;
        public override bool ForkFireDetect
        {
            get
            {
                if (this.SimulMode)
                {
                    return _SimulForkFireDetect;
                }
                else
                {
                    bool bDetect = PLC_FireState;
                    return bDetect;
                }
            }
            set
            {
                if (this.SimulMode)
                {
                    _SimulForkFireDetect = value;
                }
            }
        }

        private bool _SuddenlyFire;
        public override bool SuddenlyFire
        {
            get
            {
                return _SuddenlyFire;
            }
            set
            {
                _SuddenlyFire = value;
            }
        }

        private bool _RMMotorMCPower;
        public override bool RMMotorMCPower //210218 lsj 추가
        {
            get
            {
                if (this.SimulMode)
                {
                    return true;
                }
                else
                {
                    return true; //추후 MC 온 상태 필요하면 구현
                }
            }
            set
            {
                if (this.SimulMode)
                {
                    _RMMotorMCPower = value;
                }
            }
        }

        private bool _RMPanelDoor;
        public override bool RMPanelDoor  //210218 lsj 추가
        {
            get
            {
                if (this.SimulMode)
                {
                    return _RMPanelDoor;
                }
                else
                {
                    return true;//추후 Crane 도어 상태 필요하면 구현
                }
            }
            set
            {
                if (this.SimulMode)
                {
                    _RMPanelDoor = value;
                }
            }
        }


        private decimal _MoveSpeed;
        public override decimal MoveSpeed
        {
            get
            {
                return _MoveSpeed;
            }
            set
            {
                _MoveSpeed = value;
            }
        }

        private decimal _JogSpeed;
        public override decimal JogSpeed
        {
            get
            {
                return _JogSpeed;
            }
            set
            {
                _JogSpeed = value;
            }
        }

        private decimal _ForkAxisPosition;
        public override decimal ForkAxisPosition
        {
            get
            {
                if (this.SimulMode)
                {
                    //return _ForkAxisPosition;
                    return PLC_RM_FPosition;
                }
                else
                {
                    return PLC_RM_FPosition;
                }
            }
            set
            {
                if (this.SimulMode)
                {
                    _ForkAxisPosition = value;
                }
                //220919 HHJ SCS 개선     //- ForkAxisPosition Biding Item 변경
                RaisePropertyChanged("ForkAxisPosition");

            }
        }

        private decimal _XAxisPosition;
        public override decimal XAxisPosition
        {
            get
            {
                if (this.SimulMode)
                {
                    //return CurrentBay * 100;
                    return PLC_RM_XPosition;
                }
                else
                {
                    return PLC_RM_XPosition;
                }
            }
            set
            {
                if (this.SimulMode)
                {
                    _XAxisPosition = value;
                }
            }
        }

        private decimal _ZAxisPosition;

        public override decimal ZAxisPosition
        {
            get
            {
                if (this.SimulMode)
                {
                    //return CurrentLevel * 50 + 100;
                    return PLC_RM_ZPosition;
                }
                else
                {
                    return PLC_RM_ZPosition;
                }
            }
            set
            {
                if (this.SimulMode)
                {
                    _ZAxisPosition = value;
                }
            }
        }

        private decimal _TurnAxisPosition;
        public override decimal TurnAxisPosition
        {
            get
            {
                if (this.SimulMode)
                {
                    if (CurrentBank == 1)
                    {
                        return 0;
                    }
                    else
                    {
                        return 180;
                    }
                }
                else
                {
                    return 0;
                    //return this.GetMoterPos((int)eAxisNumber.TurnAxis);
                }
            }
            set
            {
                if (this.SimulMode)
                {
                    _TurnAxisPosition = value;
                }
            }
        }


        private string _Robot_TRANSFERRING;
        public override string Robot_TRANSFERRING
        {
            get
            {
                if (this.SimulMode)
                {
                    return _Robot_TRANSFERRING;
                }
                else
                {
                    return GetRMState() == eRMPmacState.Transfering ? "1" : "0";
                }
            }
            set
            {
                if (this.SimulMode)
                {
                    _Robot_TRANSFERRING = value;
                }
            }
        }



        private string _Robot_BANK;
        public override string Robot_BANK
        {
            get
            {
                if (this.SimulMode)
                {

                    return _Robot_BANK;
                }
                else
                {
                    return PLC_RM_Current_Bank.ToString();
                }
            }
            set
            {
                if (this.SimulMode)
                {
                    _Robot_BANK = value;
                }
            }
        }

        private string _Robot_BAY;
        public override string Robot_BAY
        {
            get
            {
                if (this.SimulMode)
                {
                    return _Robot_BAY;
                }
                else
                {
                    return PLC_RM_Current_Bay.ToString();
                }
            }
            set
            {
                if (this.SimulMode)
                {
                    _Robot_BAY = value;
                }
            }
        }

        private string _Robot_LEVEL;
        public override string Robot_LEVEL
        {
            get
            {
                if (this.SimulMode)
                {
                    return _Robot_LEVEL;
                }
                else
                {
                    return PLC_RM_Current_Level.ToString();
                }
            }
            set
            {
                if (this.SimulMode)
                {
                    _Robot_LEVEL = value;
                }
            }
        }

        //private string _Robot_CARRIERCONTAIN;
        //public override string Robot_CARRIERCONTAIN
        //{
        //    get
        //    {
        //        if (this.SimulMode)
        //        {
        //            return _Robot_CARRIERCONTAIN;
        //        }
        //        else
        //        {
        //            return "!";
        //            //return this.GetMoterPos((int)eAxisNumber.Grip);
        //        }
        //    }
        //    set
        //    {
        //        if (this.SimulMode)
        //        {
        //            _Robot_CARRIERCONTAIN = value;
        //        }
        //    }
        //}

        //private string _Robot_HOME;
        //public override string Robot_HOME
        //{
        //    get
        //    {
        //        if (this.SimulMode)
        //        {
        //            return _Robot_HOME;
        //        }
        //        else
        //        {
        //            return PvarGet(pPLCInitializeState) == 1 ? "1" : "0";
        //        }
        //    }
        //    set
        //    {
        //        if (this.SimulMode)
        //        {
        //            _Robot_HOME = value;
        //        }
        //    }
        //}

        //private string _Robot_EMERGENCYS;
        //public override string Robot_EMERGENCYS
        //{
        //    get
        //    {
        //        if (this.SimulMode)
        //        {
        //            return _Robot_EMERGENCYS;
        //        }
        //        else
        //        {
        //            if (this.PvarGet("DI_EMS_L") == 1 || this.PvarGet("DI_EMS_R") == 1)
        //                return "1";
        //            else
        //                return "0";

        //            ////if (InputMonitoring(EMSButtonLEFT) || InputMonitoring(EMSButtonRIGHT))
        //            //return "1";
        //            //else
        //            //    return "0";
        //        }
        //    }
        //    set
        //    {
        //        if (this.SimulMode)
        //        {
        //            _Robot_EMERGENCYS = value;
        //        }
        //    }
        //}





        private string _Robot_ERRORCODE;
        public override string Robot_ERRORCODE
        {
            get
            {
                if (this.SimulMode)
                {
                    return _Robot_ERRORCODE;
                }
                else
                {
                    return PLC_ErrorCode.ToString();
                }
            }
            set
            {
                if (this.SimulMode)
                {
                    _Robot_ERRORCODE = value;
                }
            }
        }


        #region 2021.02.18 RM 소모품 진단 관련 추가 
        private string _Robot_Fork_Accure;
        public override string Robot_Fork_Accure
        {
            get
            {
                if (this.SimulMode)
                {
                    return _Robot_Fork_Accure;
                }
                else
                {
                    return strPvarGet("Fork_Accure_Pos");
                }
            }
            set
            {
                if (this.SimulMode)
                {
                    _Robot_Fork_Accure = value;
                }
            }
        }

        private string _Robot_Drive_Accure;
        public override string Robot_Drive_Accure
        {
            get
            {
                if (this.SimulMode)
                {
                    return _Robot_Drive_Accure;
                }
                else
                {
                    return strPvarGet("Drive_Accure_Pos");
                }
            }
            set
            {
                if (this.SimulMode)
                {
                    _Robot_Drive_Accure = value;
                }
            }
        }

        private string _Robot_Lift_Accure;
        public override string Robot_Lift_Accure
        {
            get
            {
                if (this.SimulMode)
                {
                    return _Robot_Lift_Accure;
                }
                else
                {
                    return strPvarGet("Lift_Accure_Pos");
                }
            }
            set
            {
                if (this.SimulMode)
                {
                    _Robot_Lift_Accure = value;
                }
            }
        }

        private string _Robot_Turn_Accure;
        public override string Robot_Turn_Accure
        {
            get
            {
                if (this.SimulMode)
                {
                    return _Robot_Turn_Accure;
                }
                else
                {
                    return strPvarGet("Turn_Accure_Pos");
                }
            }
            set
            {
                if (this.SimulMode)
                {
                    _Robot_Turn_Accure = value;
                }
            }
        }

        private string _Robot_Chuck_Accure;
        public override string Robot_Chuck_Accure
        {
            get
            {
                if (this.SimulMode)
                {
                    return _Robot_Chuck_Accure;
                }
                else
                {
                    return strPvarGet("Chuck_Accure_Pos");
                }
            }
            set
            {
                if (this.SimulMode)
                {
                    _Robot_Chuck_Accure = value;
                }
            }
        }

        #endregion

        #endregion

        #region Constructor 

        /// <summary>
        ///  // 2020.12.16 RM Type 추가 ModuleBase 수정
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="simul"></param>
        /// <param name="RMType"></param>
        public RM_TPLC(string ID, bool simul, eRMType RMType, int RMNumber, bool ioSimul)
          : base(ID, simul, RMType, RMNumber, ioSimul)
        {
            ModuleName = ID;

            if (simul)
            {
                if (RMNumber == 1)
                {
                    _Robot_BANK = "1";
                    _Robot_BAY = "1";
                    _Robot_LEVEL = "1";
                }
                if (RMNumber == 2)
                {
                    _Robot_BANK = "1";
                    //_Robot_BAY = "25";      //220325 HHJ SCS 개발     //- Layoutview 수정
                    _Robot_BAY = GlobalData.Current.SystemParameter.RearXcount.ToString();
                    _Robot_LEVEL = "1";
                }
            }


            RMStateFilePath = string.Format(GData.CurrentFilePaths(GData.FullPath) + "\\Data\\RM\\StateData{0}.ini", ModuleName);

            //FileInfo File = new FileInfo(RMStateFilePath);
            //if (!File.Exists)
            //{
            //    string temppath = string.Format("\\Data\\RM\\StateData{0}.ini", ModuleName);
            //    File = new FileInfo(GlobalData.Current.FilePathChange(RMStateFilePath, temppath));

            //    RMStateFilePath = File.FullName;
            //}

            if (this.SimulMode)
            {
                CarrierExistSensor = SimulPlace_Sensor();
            }
            //캐리어 정보 복구시 해당 태그명으로 캐리어 스토리지에서 캐리어 정보를 가져와서 해당 정보로 업데이트한다.
            //shelf.DefaultSlot.SetCarrierData(trayid);
            if (CarrierStorage.Instance.GetInModuleCarrierItem(ModuleName) is CarrierItem carrier)
            {
                UpdateCarrier(carrier.CarrierID, false);
            }

            GData = GlobalData.Current;
            Thread runThread = new Thread(new ThreadStart(Run));
            runThread.IsBackground = true;
            runThread.Name = this.ModuleName + "Main Run";
            runThread.Start();

            //if(simul)
            //{
            //    Thread SimulrunThread = new Thread(new ThreadStart(SimulMotionRun));
            //    SimulrunThread.Name = ModuleName + " SimulMotion";
            //    SimulrunThread.IsBackground = true;
            //    SimulrunThread.Start();
            //}

            //220824 조숭진 timeout은 rm별이 아닌 공통 s
            //string value = string.Empty;
            //if (GData.DBManager.DbGetConfigInfo("RMSection", "CommandTimeout", out value))
            //{
            //    CommandTimeOut = Convert.ToInt32(value);
            //    value = string.Empty;
            //}
            //else
            //{
            //    CommandTimeOut = 5;
            //    GData.DBManager.DbSetProcedureConfigInfo("RMSection", "CommandTimeout", CommandTimeOut.ToString());
            //}

            //if (GData.DBManager.DbGetConfigInfo("RMSection", "FireNotifyTimeOut", out value))
            //{
            //    FireNotifyTimeOut = Convert.ToInt32(value);
            //    value = string.Empty;
            //}
            //else
            //{
            //    FireNotifyTimeOut = 2;
            //    GData.DBManager.DbSetProcedureConfigInfo("RMSection", "FireNotifyTimeOut", FireNotifyTimeOut.ToString());
            //}

            //if (GData.DBManager.DbGetConfigInfo("RMSection", "PLCIF_Delay", out value))
            //{
            //    PLCIF_Delay = Convert.ToInt32(value);
            //    value = string.Empty;
            //}
            //else
            //{
            //    PLCIF_Delay = 50;
            //    GData.DBManager.DbSetProcedureConfigInfo("RMSection", "PLCIF_Delay", PLCIF_Delay.ToString());
            //}

            //if(GData.DBManager.DbGetConfigInfo("RMSection", "CraneActionTimeOut", out value))
            //{
            //    CraneActionTimeOut = Convert.ToInt32(value);
            //    value = string.Empty;
            //}
            //else
            //{
            //    CraneActionTimeOut = 300;
            //    GData.DBManager.DbSetProcedureConfigInfo("RMSection", "CraneActionTimeOut", CraneActionTimeOut.ToString());
            //}

            //if(GData.DBManager.DbGetConfigInfo("RMSection", "PLCTimeOut", out value))
            //{
            //    PLCTimeOut = Convert.ToInt32(value);
            //    value = string.Empty;
            //}
            //else
            //{
            //    PLCTimeOut = 3;
            //    GData.DBManager.DbSetProcedureConfigInfo("RMSection", "PLCTimeOut", PLCTimeOut.ToString());
            //}
            //220824 조숭진 timeout은 rm별이 아닌 공통 e

        }
        #endregion

        #region Scheduler 

        public override void Run()
        {

            try //-메인 루프 예외 발생시 로그 찍도록 추가.
            {
                eCraneSCMode LastRMMode = eCraneSCMode.OFFLINE; //마지막 기상반 모드를 저장해둔다.
                GlobalData.Current.MRE_GlobalDataCreatedEvent.WaitOne(); //모든 모듈 생성전까지 Run 대기
                InitPLCSignal();
                while (!ThreadExitRequested)
                {
                    UpdateAxisInfo();//축별 포지션 업데이트

                    #region Client 로직
                    //SuHwan_20221027 : [ServerClient]
                    if (GlobalData.Current.ServerClientType == eServerClientType.Client)
                    {
                        if (!CarrierExistSensor)
                        {
                            ResetCarrierData();
                        }
                        else
                        {
                            if (GetCarrierID() != PC_CarrierID_FORK1) //231213 RGJ PC_CarrierID_FORK1 로 구분토록 변경.PLC 는 PC 거를 가져다 쓰기에 의미 없음.
                            {
                                UpdateCarrier(PC_CarrierID_FORK1, false);
                            }
                        }
                        Thread.Sleep(100);
                        continue;
                    }
                    #endregion


                    if (RMResetRequest) //UI 알람 리셋 요청 체크
                    {
                        RMAlarmResetAction(); //리셋 동작 진행
                        continue;
                    }
                    else
                    {
                        ProcessPLCAlarm(); //PLC 자체 알람 클리어 요청 진행
                    }


                    //RMAlarmCheck(); //PLC 알람 체크하고 알람 발생. ProcessPLCAlarmReset 알람 발생까지 처리함 주석처리


                    //Crane Rack Mode 체크
                    if (LastRMMode != GetRMMode()) //변경되었을 경우만 처리.
                    {
                        LastRMMode = GetRMMode(); 
                        switch (LastRMMode)
                        {
                            case eCraneSCMode.AUTO_RUN:  //OneRack 모드 원복
                                if (IsFirstRM && GlobalData.Current.Scheduler.CurrentSCOPMode == eSC_OperationMode.SecondRMOnly) //1번 크레인 원복
                                {
                                    PC_OneRackComp = 0;     //2024.09.20 lim, 원렉 모드 완료 신호 추가
                                    Thread.Sleep(200);
                                    GlobalData.Current.Scheduler.ChangeCraneOperationMode(eSC_OperationMode.NormalMode); //모드 변경 요청
                                    PC_OneRackComp = 1;
                                }
                                else if (!IsFirstRM && GlobalData.Current.Scheduler.CurrentSCOPMode == eSC_OperationMode.FirstRMOnly) //2번 크레인 원복
                                {
                                    PC_OneRackComp = 0;    //2024.09.20 lim, 원렉 모드 완료 신호 추가
                                    Thread.Sleep(200);
                                    GlobalData.Current.Scheduler.ChangeCraneOperationMode(eSC_OperationMode.NormalMode); //모드 변경 요청
                                    PC_OneRackComp = 1;
                                }
                                GlobalData.Current.HSMS.SendS6F11(703, "CRANE", this);
                                GlobalData.Current.PortManager.CraneInServiceAction(IsFirstRM); //포트매니저에서 직접 처리
                                break;
                            case eCraneSCMode.MANUAL_RUN:
                                if (IsFirstRM && GlobalData.Current.Scheduler.CurrentSCOPMode == eSC_OperationMode.SecondRMOnly) //1번 크레인 원복
                                {
                                    PC_OneRackComp = 0;    //2024.09.20 lim, 원렉 모드 완료 신호 추가
                                    Thread.Sleep(200);
                                    GlobalData.Current.Scheduler.ChangeCraneOperationMode(eSC_OperationMode.NormalMode); //모드 변경 요청
                                    PC_OneRackComp = 1;
                                }
                                else if (!IsFirstRM && GlobalData.Current.Scheduler.CurrentSCOPMode == eSC_OperationMode.FirstRMOnly) //2번 크레인 원복
                                {
                                    PC_OneRackComp = 0;    //2024.09.20 lim, 원렉 모드 완료 신호 추가
                                    Thread.Sleep(200);
                                    GlobalData.Current.Scheduler.ChangeCraneOperationMode(eSC_OperationMode.NormalMode); //모드 변경 요청
                                    PC_OneRackComp = 1;
                                }
                                GlobalData.Current.HSMS.SendS6F11(703, "CRANE", this);
                                GlobalData.Current.PortManager.CraneOutOfServiceAction(IsFirstRM); //포트매니저에서 직접 처리
                                break;
                            case eCraneSCMode.ONE_RACK_PAUSE: //원랙 모드 변경요청
                                //if (PLC_CraneCommandState == eCraneCommand.REMOTE_HOME || PLC_CraneCommandState == eCraneCommand.LOCAL_HOME) //홈상태 확인.
                                //2024.09.19 lim, 원렉 모드 사용시 크레인 고장으로 원점 불가능 할 경우도 있어서 상태 확인 부분 제외
                                {
                                    eSC_OperationMode ReqOPMode = this.IsFirstRM ? eSC_OperationMode.SecondRMOnly : eSC_OperationMode.FirstRMOnly;
                                    eSC_OperationMode CurrentMode = GlobalData.Current.Scheduler.CurrentSCOPMode;
                                    if (CurrentMode == eSC_OperationMode.NormalMode) //노멀 모드에서만 변경 가능.
                                    {
                                        PC_OneRackComp = 0;    //2024.09.20 lim, 원렉 모드 완료 신호 추가
                                        Thread.Sleep(200);
                                        GlobalData.Current.Scheduler.ChangeCraneOperationMode(ReqOPMode); //모드 변경 요청
                                        PC_OneRackComp = 1;
                                    }
                                }
                                break;
                            case eCraneSCMode.OFFLINE:  //홀드
                                Thread.Sleep(100);
                                continue;
                        }
                    }

                    //명령들어왔는지 체크
                    if (CurrentCmd == null || GetRMMode() != eCraneSCMode.AUTO_RUN)
                    {
                        Thread.Sleep(100);
                        continue;
                    }
                    switch (CurrentCmd.Command)
                    {
                        case eCraneCommand.MOVE:
                            // 
                            if (this.CraneCommandAction(CurrentCmd))
                            {
                                CraneMovePostAction();
                            }
                            else
                            {
                                GData.CraneActiveJobList.Remove(CurrentCmd);
                                RemoveCraneCommand();
                            }

                            break;
                        case eCraneCommand.PICKUP:
                            if (this.CraneCommandAction(CurrentCmd))
                            {
                                CraneGetPostAction();
                            }
                            else
                            {
                                GData.CraneActiveJobList.Remove(CurrentCmd);
                                RemoveCraneCommand();
                            }

                            break;
                        case eCraneCommand.UNLOAD:
                            if (this.CraneCommandAction(CurrentCmd))
                            {
                                CranePutPostAction();
                            }
                            else
                            {
                                GData.CraneActiveJobList.Remove(CurrentCmd);
                                RemoveCraneCommand();
                            }
                            break;
                        case eCraneCommand.LOCAL_HOME:
                            if (this.CraneCommandAction(CurrentCmd))
                            {
                                CraneHomePostAction();
                            }
                            else
                            {
                                GData.CraneActiveJobList.Remove(CurrentCmd);
                                RemoveCraneCommand();
                            }
                            break;
                        default:
                            RemoveCraneCommand();
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
            }
        }

        private void InitPLCSignal()
        {
            PC_CommandWriteComplete = 0;
            PC_CommandNumber_FORK1 = 0;
            PC_CraneCommand = eCraneCommand.NONE;
            PC_DestBank_FORK1 = 0;
            PC_DestBay_FORK1 = 0;
            PC_DestLevel_FORK1 = 0;
            PC_ErrorReset = false;
            PC_DestWorkPlace_FORK1 = 0;
            //230912 RGJ 재시작시 화재 관련 비트도 OFF 한다.
            PC_FireOccurred = false; //재시작시 화재 비트 OFF 
            PC_FireReset = false; //재시작시 화재 비트 OFF
            PC_OneRackComp = 1;    //2024.09.20 lim, 원렉 모드 완료 신호 추가
        }
        //public override void AlarmThread()
        //{
        //    int AlarmCode = 0;
        //    string strCode = string.Empty;

        //    while (true)
        //    {
        //        AlarmCode = GetAlarmCode();

        //        if (this.SimulMode)
        //        {
        //            strCode = GlobalData.Current.SimulationAlarmCode;
        //            if (strCode != "")
        //                strCode = ChangePmacAlarmCode(Convert.ToInt32(strCode));
        //        }



        //        if (AlarmCode > 0)
        //            strCode = ChangePmacAlarmCode(Convert.ToInt32(AlarmCode));

        //        if (strCode != string.Empty && strCode != "0")
        //        {
        //            bool AlarmExist = GlobalData.Current.Alarm_Manager.CheckAlarmExist(strCode);

        //            if (!AlarmExist)
        //            {
        //                GlobalData.Current.Alarm_Manager.AlarmOccur(strCode, this.ModuleName);
        //            }
        //        }
        //        Thread.Sleep(1000);
        //    }
        //}

        public override bool CraneCommandAction(CraneCommand cmd)
        {
            LogManager.WriteConsoleLog(eLogLevel.Info, "ActionCommand :{0} TargetBank:{1}  TargetBay:{2} TargetLevel:{3} TargetCarrierID:{4}  TargetType:{5}", cmd.Command, cmd.TargetBank,
                 cmd.TargetBay, cmd.TargetLevel, cmd.TargetCarrierID, cmd.TargetType);

            #region RM 명령 수행전 체크리스트 수행
            if (this.RMAlarmCheck() > 0)
            {
                return false;
            }

            LogManager.WriteConsoleLog(eLogLevel.Info, "RMMCommandAction Target : {0} CommandType :{1}", cmd.TargetItem.iLocName, cmd.Command);
            ICarrierStoreAble RMTarget = cmd.TargetItem;

            //210218 lsj 인터락 추가
            #region 도어 인터락 체크

            if (GlobalData.Current.MainBooth.GetBoothDoorOpenState()) //도어 Open 상태에서는 모든 동작 불가.
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "TagName:{0} Type:{1} cmd:{2} Cannot Action by DoorInterlock!", cmd.TargetItem.iLocName, cmd.Command, cmd);
                return false;
            }

            #endregion

            if (RMTarget == null)
            {
                return false;
            }

            //220509 HHJ SCS 개선     //- ShelfControl 변경
            ShelfItem TargetShelf = RMTarget as ShelfItem;
            if (TargetShelf != null)
            {
                if (RMNumber == 1)
                    TargetShelf.ShelfBusyRm = eShelfBusyRm.RM1;
                else
                    TargetShelf.ShelfBusyRm = eShelfBusyRm.RM2;
            }


            #region // 2021.01.28 Move Command 시  RobotAccessAble Check 추가
            //var Cv = GlobalData.Current.LineManager.GetCVModuleByTag(ShelfId.TagName);
            var Cv = RMTarget as CV_BaseModule;

            if (Cv != null) // CV Module이면
            {
                //동작전 센서 체크
                if (cmd.Command == eCraneCommand.PICKUP && !Cv.CarrierExist)
                {
                    GlobalData.Current.Alarm_Manager.AlarmOccurbyName("CRANE_PORT_MATERIAL_NOT_DETECTED", this.ModuleName); //CRANE_PORT_MATERIAL_NOT_DETECTED 자재감지되지않음 
                    return false;
                }
                else if (cmd.Command == eCraneCommand.UNLOAD && Cv.CarrierExist)
                {
                    GlobalData.Current.Alarm_Manager.AlarmOccurbyName("CRANE_PORT_MATERIAL_DETECTED", this.ModuleName); //CRANE_PORT_MATERIAL_DETECTED 자재감지됨
                    return false;
                }
                //포트 엑세스 가능 체크
                if (cmd.Command != eCraneCommand.MOVE) //무브 동작은 판단할 필요 없음
                {
                    Stopwatch timeWatch = Stopwatch.StartNew();
                    while (true) //포트와 로봇간 오토 모드 변환 속도 때문에 에러발생하여 리트라이 추가.
                    {

                        if (Cv.RobotAccessAble)
                        {
                            LogManager.WriteConsoleLog(eLogLevel.Info, "{0} Module RobotAccessAble Check Done!.", Cv.ModuleName);
                            break;
                        }
                        else
                        {
                            if (IsTimeout_SW(timeWatch, 60) == true) //넉넉하게 1분 기다려본다.
                            {
                                GlobalData.Current.Alarm_Manager.AlarmOccurbyName("CRANE_PORT_CANNOT_ACCESSIBLE", this.ModuleName); //CRANE_PORT_CANNOT_ACCESSIBLE
                                return false;
                            }
                            else
                            {
                                Thread.Sleep(200);
                                continue;
                            }
                        }
                    }
                }
            }
            #endregion


            //210105 lsj PMacOnlineConncet -> RobotOnlineConncet 변경
            if (!this.RobotOnlineConncet)
            {
                GlobalData.Current.Alarm_Manager.AlarmOccurbyName("PLC_CONNECTION_FAIL", this.ModuleName);  // 로봇명령 수행전 통신 에러발생
                LogManager.WriteRobotLog(eLogLevel.Error, this.ModuleName, "통신연결 Disconnect 발생 하여 Alarm 발생");
                return false;
            }
            #endregion

            #region 명령 전송 
            // 2020.12.11 RM Robot 동작 추가
            LogManager.WriteRobotLog(eLogLevel.Info, this.ModuleName, "TagName:{0} Type:{1} 명령을 실행 합니다. ", RMTarget.iLocName, cmd.Command);

            LogManager.WriteConsoleLog(eLogLevel.Info, "TagName:{0} Type:{1} Executing Command. ", RMTarget.iLocName, cmd.Command);
            bool CommandSendSuccess = false;
            switch (cmd.Command)
            {

                case eCraneCommand.MOVE:
                    this.MoveReq = true;  // 190724  명령시 Button 동작 확인
                    CommandSendSuccess = SendCraneCommand(cmd); // 실행
                    break;
                case eCraneCommand.PICKUP:
                    this.GetReq = true;  // 190724  명령시 Button 동작 확인
                    CommandSendSuccess = SendCraneCommand(cmd); // 실행
                    break;
                case eCraneCommand.UNLOAD:
                    this.PutReq = true;  // 190724  명령시 Button 동작 확인
                    CommandSendSuccess = SendCraneCommand(cmd); // 실행
                    break;
                case eCraneCommand.LOCAL_HOME:
                    CommandSendSuccess = SendCraneCommand(cmd);
                    break;
                default:
                    break;
            }
            if(CommandSendSuccess == false) //커맨드 전송에 실패함.알람은 SendCraneCommand 에서 발생함.
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "TagName:{0} Type:{1} Command Sent Failed. ", RMTarget.iLocName, cmd.Command);
                return false;
            }

            #endregion

            #region Busy 상태 체크 추가
            if (!SimulMode)
            {
                Stopwatch SW_CommandSentTime = Stopwatch.StartNew();
                LogManager.WriteConsoleLog(eLogLevel.Info, "Crane CommandID:{0}  Command:{1} PLC Busy Check Entry!", cmd.CommandID, cmd.Command);
                while (true)
                {
                    if (IsTimeout_SW(SW_CommandSentTime, CommandTimeOut * 2))
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "Crane CommandID:{0}  Command:{1} PLC Busy Time Out! And PC_CommandWriteComplete = {2} Write", cmd.CommandID, cmd.Command, PC_CommandWriteComplete == 1 ? 0 : 1);
                        if (PC_CommandWriteComplete == 1)       //알람 발생하면 command write 끈다.
                        {
                            PC_CommandWriteComplete = 0;
                        }
                        //240213 RGJ Crane Busy 응답이 안들어와도 Timeout 알람 발생
                        GlobalData.Current.Alarm_Manager.AlarmOccurbyName("CRANE_PLC_INTERFACE_ERROR", ModuleName);
                        return false;
                    }
                    if (PLC_CraneJobState == eCraneJobState.Busy)
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "Crane CommandID:{0}  Command:{1} PLC Busy Check!", cmd.CommandID, cmd.Command);
                        break;
                    }
                    if (RMAlarmCheck() > 0)         //busy가 안들어오는 와중에 알람발생하면 
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "Crane CommandID:{0}  Command:{1} Waiting For Busy... PLC RM Alarm Occur! And PC_CommandWriteComplete = {2} Write", cmd.CommandID, cmd.Command, PC_CommandWriteComplete == 1 ? 0 : 1);
                        if (PC_CommandWriteComplete == 1)       //알람 발생하면 command write 끈다.
                        {
                            PC_CommandWriteComplete = 0;
                        }
                        return false;
                    }
                    Thread.Sleep(100);
                }
            }
            #endregion

            #region 완료 대기
            //220524 HHJ SCS 개선     //- Shelf Xml제거
            //기존은 쉘프 티칭데이터에서 포지션을 가져갔으나, PLC에서 정보를 읽어서 업데이트 해야함.
            //원형이 바뀌는 내역인지라 우선 인자를 전부 0으로 처리.
            //return CheckRMMoveComplete(ShelfId.TagName, ShelfId.AxisFork, ShelfId.AxisDrive, ShelfId.AxisZ, ShelfId.AxisT, cmdtype, cmd);

            bool MotionComplete = CheckRMActionComplete(cmd);
            if (!MotionComplete)
            {
                return false;
            }

            #endregion

            #region Carrier 데이터 처리.

            if (this.SimulMode)
            {
                Robot_BANK = cmd.TargetBank.ToString();
                Robot_BAY = cmd.TargetBay.ToString();
                Robot_LEVEL = cmd.TargetLevel.ToString();
                if (cmd.Command == eCraneCommand.UNLOAD)
                {
                    this.CarrierExistSensor = false;
                }
                else if (cmd.Command == eCraneCommand.PICKUP)
                {
                    this.CarrierExistSensor = true;
                }
            }
            #endregion

            if (cmd.TargetType == enumCraneTarget.PORT)
            {
                CV_BaseModule RobotAccessCV = cmd.TargetItem as CV_BaseModule;
                McsJob CommandJob = GlobalData.Current.McdList.FirstOrDefault(r => r.CommandID == cmd.CommandID); //200827 RGJ Job 에서 가져와서 쉘프에 업데이트한다.

                if (cmd.Command == eCraneCommand.UNLOAD)
                {

                    ResetCarrierData();//크레인 데이터 초기화
                    RobotAccessCV.UpdateCarrier(CommandJob?.CarrierID); //포트에 캐리어 데이터 업데이트
                    LogManager.WriteConsoleLog(eLogLevel.Info, "Update Carrier Position   Crane :{0} ==> Port :{1}  CarrierID : {2}", ModuleName, RobotAccessCV.ModuleName, CommandJob?.CarrierID);

                    if(!RobotAccessCV.CVAvailable || RobotAccessCV.NextCVCommand != eCVCommand.WaitCranePut) //240619 RGJ 내려놓는 시점에 포트 다운되었으면 크레인이 데이터라도 써준다.
                    {
                        RobotAccessCV.WriteTrackingData(CommandJob.JobCarrierItem);
                        LogManager.WriteConsoleLog(eLogLevel.Info, "RM Write Port Put Data by Port Abnormal   - PortName :{0} CarrierId : {1}", RobotAccessCV.ModuleName, CommandJob.CarrierID);
                    }

                    RobotAccessCV.NotifyTrayLoadComplete(this, CommandJob); //투입했다고 알려준다.

                }
                else if (cmd.Command == eCraneCommand.PICKUP)
                {
                    RobotAccessCV.NotifyTrayUnloadComplete(this, CommandJob);
                    //데이터는 포트에서 보내준다..
                }
            }
            else // Shelf 작업
            {
                if (cmd != null)
                {
                    if (cmd.Command == eCraneCommand.PICKUP || cmd.Command == eCraneCommand.UNLOAD) //230829 MOVE 내부 로직 안타므로 삭제
                    {
                        UpdateShelf(cmd.TargetTagID, cmd.Command, cmd.TargetCarrierID);
                    }
                }
            }

            return true;
        }
        protected override bool CheckInitComplete()
        {
            //DateTime curTime = DateTime.Now;
            //eRMPmacState mRMState = eRMPmacState.Unknown;
            //while (!IsTimeOut(curTime, RMParameter.InitTimeout))
            //{
            //    mRMState = (eRMPmacState)PvarGet(pRMState);
            //    if (mRMState == eRMPmacState.Initialized_Idle)
            //    {
            //        Log.LogManager.WriteConsoleLog(eLogLevel.Info, "R / M Status  Initializing 완료{0}", mRMState);
            //        Thread.Sleep(100);
            //        this.InitReq = false;
            //        return true;
            //    }
            //    if (this.SimulMode)
            //    {
            //        this.ForkAxisPosition = 0;
            //        this.XAxisPosition = 0;
            //        this.ZAxisPosition = 0;
            //        this.TurnAxisPosition = 0;
            //        this.GripAxisPosition = 0;
            //        return true;
            //    }
            //    //Thread.Sleep(1000);
            //    DoEvents();
            //}
            ////GlobalData.Current.Alarm_Manager.AlarmOccur("16", this.ModuleName); // TIME OUT ALARM
            //GlobalData.Current.Alarm_Manager.AlarmOccur(HOME_NOT_COMPLETE, this.ModuleName); // TIME OUT ALARM

            //this.InitReq = false;
            //return false;

            //PLC는 초기화 완료상태 항목 없음.
            return true;
        }

        public override bool CheckRMCommandExist() //231006 RGJ RM Command 와 실제 PLC Busy 분리함.
        {
            if (CurrentCmd == null)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public override bool CheckRMPLCBusy() //231006 RGJ RM Command 와 실제 PLC Busy 분리함.
        {
            bool PLCBusy = PLC_CraneActionState == eCraneSCState.BUSY;
            bool CommandBusy = PLC_CraneJobState == eCraneJobState.Busy;
            return PLCBusy && CommandBusy;
        }
        protected bool CheckPLCJobComplete(CraneCommand cmd)
        {
            //240123 RGJ 크레인 완료 체크 조건 조건 추가.
            //PLC 자체 에러 리셋도중 커맨드 None 처리하고 완료주는 경우가 있어서 현재 커맨드조건 추가함.
            bool StateChack = eCraneJobState.JobComplete_Fork1 == PLC_CraneJobState;
            bool CommandCheck = cmd.Command == PLC_CraneCommandState;
            
            bool CarrierSensorState = false; //241024 RGJ 크레인 완료 조건에 화물감지 추가.
            if (StateChack) //명령이 완료 되었으면 화물 상태를 점검해본다.
            {
                switch (cmd.Command)
                {
                    case eCraneCommand.PICKUP: //GET 화물 감지가 되어야 한다.
                        CarrierSensorState = PLC_CarrierExistFork1;
                        break;
                    case eCraneCommand.UNLOAD: //화물감지가 되어선 안됨.
                        CarrierSensorState = !PLC_CarrierExistFork1;
                        break;
                    default:
                        CarrierSensorState = true; //기타 명령은 화물감지상태 체크 안함.
                        break;
                }
            }

            if(StateChack && !CommandCheck) //완료는 됬지만 커맨드매칭이 실패.
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "Crane : {0} Command Complete but command type is mismatched. PC CMD :{1}  PLC CMD :{2}", ModuleName, cmd.Command, PLC_CraneCommandState);
            }
            if(StateChack && !CarrierSensorState) //완료는 됬지만 화물 상태가 명령과 안맞음 실패.
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "Crane : {0} Command Complete but Carrier Sensor is mismatched. PC CMD :{1}  PLC Carrier Sensor :{2}", ModuleName, cmd.Command, PLC_CarrierExistFork1);
            }

            return StateChack && CommandCheck && CarrierSensorState;
        }

        protected override bool CheckRMActionComplete(CraneCommand cmd)
        {
            DateTime curTime = DateTime.Now;
            //eRMPmacState mRMState = eRMPmacState.Unknown;
            bool poscomp = false;
            DateTime DtReport = DateTime.Now;
            Stopwatch timeWatch = Stopwatch.StartNew();

            while (!IsTimeout_SW(timeWatch, CraneActionTimeOut))
            {
                if (RMAlarmCheck() > 0) //알람 발생
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "R/M : {0} CMD: {1}  Failed by Alarm Occurred AlarmID:{2}", ModuleName, cmd.Command, RMAlarmCheck());
                    CraneActionAbortProcess(cmd);
                    return false; //20230215 RGJ 브레이크 하면 알람 발생시  타임 아웃도 같이 발생하므로 리턴으로 원복.
                }

                if (!CheckRMAutoMode()) //크레인 오토모드가 풀렸다.알람 발생과 동일처리
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "R/M : {0} CMD: {1}  Failed by ManualMode", ModuleName, cmd.Command);
                    CraneActionAbortProcess(cmd);
                    return false;
                }

                #region 완료
                if (CheckPLCJobComplete(cmd)) // 동작 완료 체크
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "R/M : {0} CMD: {1} PLC Complete Checked!. ", ModuleName, cmd.Command);

                    poscomp = CheckPostionComplete(cmd);
                    LogManager.WriteConsoleLog(eLogLevel.Info, "R/M : {0} CMD: {1} PLC Position Check: {2}. ", ModuleName, cmd.Command , poscomp);
                    if(!poscomp)
                    {
                        Thread.Sleep(1000); //완료됬는데 무브 포지션 안맞는 상태는 이미 이상상태다 타임아웃까지 로그 연달아 찍는거 방지.
                    }
                    if (poscomp) //최종 위치 체크
                    {
                        PC_CommandWriteComplete = 0; //230531 RGJ Command Write Off 는 작업 끝나면 한다.

                        bool AckOffCheck = WaitCraneCommandAckOff(CraneActionTimeOut);

                        if(AckOffCheck == false)
                        {
                            break;
                        }    

                        LogManager.WriteConsoleLog(eLogLevel.Info, "R/M : {0} CMD: {1} Action Completed. ", ModuleName, cmd.Command);

                        LogManager.WriteRobotLog(eLogLevel.Info, this.ModuleName, "TagName:{0} Type:{1} cmd:{2} Action Completed.", cmd.TargetTagID, cmd.Command, cmd);
                        return true;
                    }
                }
                #endregion
                Thread.Sleep(50);
            }

            if (cmd.Command == eCraneCommand.UNLOAD) //PUT 타임아웃 발생
            {
                PC_CommandWriteComplete = 0;//20231220 서정훈 PUT타임아웃 발생시에도 COMMANDWRITE OFF함
                LogManager.WriteRobotLog(eLogLevel.Info, this.ModuleName, "PUT Timout TagName:{0} Type:{1} cmd:{2} Alarm Occurred.", cmd.TargetTagID, cmd.Command, cmd);
                GlobalData.Current.Alarm_Manager.AlarmOccurbyName("CRANE_UNLOAD_TIMEOUT", this.ModuleName); // TIME OUT ALARM
            }
            else if (cmd.Command == eCraneCommand.PICKUP) //GET 타임아웃 발생
            {
                PC_CommandWriteComplete = 0;//20231220 서정훈 GET타임아웃 발생시에도 COMMANDWRITE OFF함
                LogManager.WriteRobotLog(eLogLevel.Info, this.ModuleName, "GET Timout TagName:{0} Type:{1} cmd:{2} Alarm Occurred.", cmd.TargetTagID, cmd.Command, cmd);
                GlobalData.Current.Alarm_Manager.AlarmOccurbyName("CRANE_PICK_TIMEOUT", this.ModuleName); // TIME OUT ALARM
            }
            else if (cmd.Command == eCraneCommand.MOVE) //Move 타임아웃 발생)
            {
                PC_CommandWriteComplete = 0;//20231220 서정훈 MOVE타임아웃 발생시에도 COMMANDWRITE OFF함
                LogManager.WriteRobotLog(eLogLevel.Info, this.ModuleName, "MOVE Timout TagName:{0} Type:{1} cmd:{2} Alarm Occurred.", cmd.TargetTagID, cmd.Command, cmd);
                GlobalData.Current.Alarm_Manager.AlarmOccurbyName("CRANE_MOVE_TIMEOUT", this.ModuleName); // TIME OUT ALARM
            }
            //221007 조숭진 paused 추가
            McsJob jobstatus = GlobalData.Current.McdList.FirstOrDefault(r => r.CommandID == cmd.CommandID);
            if (jobstatus != null)
            {
                jobstatus.TCStatus = eTCState.PAUSED;
            }
            return false;
        }

        public override bool CheckForkIsCenter()
        {
            //2024.05.30 lim, alarm 발생시 가끔 PLC_Fork1_Extend 값이 0으로 바뀔때가 있다. 위치 값도 같이 확인 필요
            bool isCenter = (Math.Abs(PLC_RM_FPosition) < 20);    // 최대 편차가 18mm 까지 발생한다고 함.

            eCraneArmState ArmState = PLC_Fork1_Extend;
            return (ArmState == eCraneArmState.Center) && isCenter;
        }

        /// <summary>
        /// 240212 RGJ
        /// 크레인 동작중 알람이나 메뉴얼전환 등으로 중단 될경우 처리를 루틴화. 
        /// </summary>
        private void CraneActionAbortProcess(CraneCommand cmd)
        {
            //230717 RGJ 알람 발생시 CommandWriteComplete Off 함
            PC_CommandWriteComplete = 0;

            #region 해당기능 스케쥴러로 이동시킴.
            ////221007 조숭진 paused 추가
            //McsJob ErrorJobstatus = GlobalData.Current.McdList.FirstOrDefault(r => r.CommandID == cmd.CommandID);
            //if (ErrorJobstatus != null)
            //{
            //    ErrorJobstatus.TCStatus = eTCState.PAUSED;
            //}
            ////break; //221007 조숭진 break로 변경.

            ////230719 RGJ PICKUP Command 도중에 Alarm 발생시 캐리어가 이미 존재하면 Source 에서 데이터를 크레인으로 옮긴다.
            //if (cmd.Command == eCraneCommand.PICKUP && CarrierExistSensor)
            //{
            //    //cmd.TargetItem.TransferCarrierData(this); //직접 업데이트로 변경.

            //    cmd.TargetItem.ResetCarrierData();
            //    UpdateCarrier(cmd.TargetCarrierID);
            //    LogManager.WriteConsoleLog(eLogLevel.Info, "R/M : {0} CMD: {1} Target:{2} CraneAction Aborted but Carrier is sended at crane. Data Transferred source to crane.", ModuleName, cmd.Command, cmd.TargetItem.iLocName);
            //}
            //else if (cmd.Command == eCraneCommand.PICKUP && !CarrierExistSensor)
            //{
            //    ResetCarrierData();
            //    cmd.TargetItem.UpdateCarrier(cmd.TargetCarrierID);
            //    LogManager.WriteConsoleLog(eLogLevel.Info, "R/M : {0} CMD: {1} Target:{2} CraneAction Aborted but Carrier is not sended at crane. Data Recovery", ModuleName, cmd.Command, cmd.TargetItem.iLocName);
            //}

            ////230719 RGJ UNLOAD Command 도중에 Alarm 발생시 캐리어가 없으면  크레인에서 Target로 데이터를 옮긴다.
            //if (cmd.Command == eCraneCommand.UNLOAD && !CarrierExistSensor)
            //{
            //    //this.TransferCarrierData(cmd.TargetItem); //직접 업데이트로 변경.

            //    //231027 컨베이어 트래킹데이터에 넣어준다.
            //    CV_BaseModule RobotAccessCV = cmd.TargetItem as CV_BaseModule;
            //    if (RobotAccessCV != null)
            //    {
            //        CarrierItem CraneCarrier = InSlotCarrier;
            //        RobotAccessCV.WriteTrackingData(CraneCarrier);
            //    }

            //    ResetCarrierData();
            //    cmd.TargetItem.UpdateCarrier(cmd.TargetCarrierID);

            //    LogManager.WriteConsoleLog(eLogLevel.Info, "R/M : {0} CMD: {1} Target:{2} CraneAction Aborted but Carrier is not sensed at crane.Data Transferred crane to source.", ModuleName, cmd.Command, cmd.TargetItem.iLocName);
            //}
            #endregion
        }

        private bool WaitCraneCommandAckOff(int WaitTime)
        {
            Stopwatch timeWatch = Stopwatch.StartNew(); ; //PLC Off 대기
            while (!IsTimeout_SW(timeWatch, WaitTime))
            {
                if (PLC_CommandAck == 0)
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "R/M : {0} Command Ack Off Check ", ModuleName);
                    return true;
                }
                Thread.Sleep(50);
            }
            LogManager.WriteConsoleLog(eLogLevel.Info, "R/M : {0} Command Ack Off Check Failed ", ModuleName);
            return false;
        }

        private bool CheckPostionComplete(CraneCommand cmd)
        {
            if (SimulMode)
            {
                return true;
            }
            else
            {
                bool bBankMatch = true;
                bool bBayMatch = cmd.TargetBay == CurrentBay;
                bool bLevelMatch = cmd.TargetLevel == CurrentLevel;
                bool WorkPlaceMatch = cmd.TargetItem.iWorkPlaceNumber == PLC_RM_Current_WorkPlace;

                if(cmd.TargetItem is CV_BaseModule cItem) //포트는 워크 플레이스 들어와도 도착한걸로 간주.
                {
                    
                    if((bBankMatch && bBayMatch && bLevelMatch) || WorkPlaceMatch)
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "Crane :{0} CMD :{1} CheckPostionComplete Target Port :{2}  BayMatch:{3} LevelMatch:{4} WorkMatch:{5}",
                            ModuleName, cmd.Command, cItem.ModuleName, bBankMatch, bLevelMatch, WorkPlaceMatch);
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return bBankMatch && bBayMatch && bLevelMatch;
                }
               
            }
        }

        /// <summary>
        ///  // 2021.01.18 RM 동작관련 추가 
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        public override bool CheckMoveComplete(CraneCommand cmd)
        {
            if (this.SimulMode)
                return true;

            try
            {
                if (cmd == null)
                    return true;

                if (cmd.TargetType == enumCraneTarget.PORT) //포트
                {
                    //일단주석
                    //if (cmd.TargetBank == Convert.ToInt32(this.Robot_BANK) && cmd.TargetLevel == Convert.ToInt32(this.Robot_LEVEL))
                    if (cmd.TargetLevel == Convert.ToInt32(this.Robot_LEVEL))
                        return true;
                    else
                        return false;

                }
                else //쉘프
                {
                    //cmd.TargetBank == Convert.ToInt32(this.Robot_BANK) 뱅크 체크 삭제
                    if (cmd.TargetBay == Convert.ToInt32(this.Robot_BAY) && cmd.TargetLevel == Convert.ToInt32(this.Robot_LEVEL))
                        return true;
                    else
                        return false;
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
                return false;
            }

        }


        protected override void TactLogWrite(CraneCommand cmd)
        {
            //필요시 다시 구현.

            //#region tact 관련 
            //if (SimulMode)
            //{
            //    return;
            //}
            //string[] CMD_TargetShelf_ID = this.SetTextSend("CMD_TargetShelf_ID");


            ////cmd.Command == enumCraneCommand.CRANE_EMO_GET
            //if (cmd == null)
            //{
            //    return;
            //}
            //switch (cmd.Command)
            //{
            //    case enumCraneCommand.None:
            //    case enumCraneCommand.SYSTEM_CHECK:
            //    case enumCraneCommand.STATUS_DATA_REQUEST:
            //    case enumCraneCommand.STATUS_DATA_REPORT:
            //        break;
            //    case enumCraneCommand.CRANE_GET:
            //    case enumCraneCommand.CRANE_EMO_GET:
            //    case enumCraneCommand.CRANE_S_GET:
            //        #region get Log
            //        tmpget = this.SetTextSend("P30000");
            //        //tactGetval = string.Empty;

            //        if (tmpget[0] == "1" && tactGetval != tmpget[0])
            //        {
            //            //GData.WCF_mgr.ReportRobotStatus(ModuleName); //로봇 중간 동작 보고 추가.
            //            Log.LogManager.WriteRobotLog(eLogLevel.Info,ModuleName ,"Get 시작 {0}", CMD_TargetShelf_ID[0].ToString());
            //            tactGetval = tmpget[0];
            //        }
            //        else if (tmpget[0] == "2" && tactGetval != tmpget[0])
            //        {
            //            Log.LogManager.WriteRobotLog(eLogLevel.Info, ModuleName, "Get 보간이동 완료 {0}", CMD_TargetShelf_ID[0].ToString());
            //            tactGetval = tmpget[0];
            //        }
            //        else if (tmpget[0] == "3" && tactGetval != tmpget[0])
            //        {
            //            Log.LogManager.WriteRobotLog(eLogLevel.Info, ModuleName, "Get 위치 down 완료 {0}", CMD_TargetShelf_ID[0].ToString());
            //            tactGetval = tmpget[0];
            //        }
            //        else if (tmpget[0] == "4" && tactGetval != tmpget[0])
            //        {
            //            //GData.WCF_mgr.ReportRobotStatus(ModuleName); //로봇 중간 동작 보고 추가.
            //            Log.LogManager.WriteRobotLog(eLogLevel.Info, ModuleName, "GetFork Extend {0}", CMD_TargetShelf_ID[0].ToString());
            //            tactGetval = tmpget[0];
            //        }
            //        else if (tmpget[0] == "5" && tactGetval != tmpget[0])
            //        {
            //            Log.LogManager.WriteRobotLog(eLogLevel.Info, ModuleName, "Get Z Up 완료 {0}", CMD_TargetShelf_ID[0].ToString());
            //            tactGetval = tmpget[0];
            //        }
            //        else if (tmpget[0] == "6" && tactGetval != tmpget[0])
            //        {
            //            Log.LogManager.WriteRobotLog(eLogLevel.Info, ModuleName, "Get Fork Fold {0}", CMD_TargetShelf_ID[0].ToString());
            //            tactGetval = tmpget[0];
            //        }
            //        else if (tmpget[0] == "7" && tactGetval != tmpget[0])
            //        {
            //            Log.LogManager.WriteRobotLog(eLogLevel.Info, ModuleName, "Clamp Grip 완료 {0}", CMD_TargetShelf_ID[0].ToString());
            //            tactGetval = tmpget[0];
            //        }

            //        #endregion
            //        break;
            //    case enumCraneCommand.CRANE_PUT:
            //    case enumCraneCommand.CRANE_EMO_PUT:
            //    case enumCraneCommand.CRANE_S_PUT:
            //        #region put log
            //         tmpput = this.SetTextSend("P30001");
            //         //tactPutval = string.Empty;

            //        if (tmpput[0] == "1" && tactPutval != tmpput[0])
            //        {
            //            //GData.WCF_mgr.ReportRobotStatus(ModuleName); //로봇 중간 동작 보고 추가.
            //            Log.LogManager.WriteRobotLog(eLogLevel.Info, ModuleName, "Put 시작 {0}", CMD_TargetShelf_ID[0].ToString());
            //            tactPutval = tmpput[0];
            //        }
            //        else if (tmpput[0] == "2" && tactPutval != tmpput[0])
            //        {
            //            Log.LogManager.WriteRobotLog(eLogLevel.Info, ModuleName, "Put 보간이동 완료 {0}", CMD_TargetShelf_ID[0].ToString());
            //            tactPutval = tmpput[0];

            //        }
            //        else if (tmpput[0] == "3" && tactPutval != tmpput[0])
            //        {
            //            Log.LogManager.WriteRobotLog(eLogLevel.Info, ModuleName, "Clamp UnGrip 완료 {0}", CMD_TargetShelf_ID[0].ToString());
            //            tactPutval = tmpput[0];

            //        }
            //        else if (tmpput[0] == "4" && tactPutval != tmpput[0])
            //        {
            //            //GData.WCF_mgr.ReportRobotStatus(ModuleName); //로봇 중간 동작 보고 추가.
            //            Log.LogManager.WriteRobotLog(eLogLevel.Info, ModuleName, "Put Fork Extend {0}", CMD_TargetShelf_ID[0].ToString());
            //            tactPutval = tmpput[0];
            //        }
            //        else if (tmpput[0] == "5" && tactPutval != tmpput[0])
            //        {
            //            Log.LogManager.WriteRobotLog(eLogLevel.Info, ModuleName, "PutZ Down 완료 {0}", CMD_TargetShelf_ID[0].ToString());
            //            tactPutval = tmpput[0];
            //        }
            //        else if (tmpput[0] == "6" && tactPutval != tmpput[0])
            //        {
            //            Log.LogManager.WriteRobotLog(eLogLevel.Info, ModuleName, "Put Fork Fold {0}", CMD_TargetShelf_ID[0].ToString());
            //            tactPutval = tmpput[0];
            //        }
            //        #endregion
            //        break;

            //    case enumCraneCommand.CRANE_MOVE:
            //    case enumCraneCommand.CRANE_START:
            //    case enumCraneCommand.CRANE_STOP:
            //    case enumCraneCommand.CRANE_ERROR_RESET:
            //    case enumCraneCommand.CRANE_RETURN_HOME:
            //    case enumCraneCommand.CRANE_EMO_RETURN_HOME:
            //    case enumCraneCommand.PORT_MANUAL:
            //    case enumCraneCommand.CRANE_CHUCK:
            //    case enumCraneCommand.CRANE_UNCHUCK:
            //    case enumCraneCommand.CRANE_ATTEACH_START:
            //    case enumCraneCommand.CRANE_ATTEACH_STOP:
            //    case enumCraneCommand.IO_MONITORING_REQUEST:
            //    case enumCraneCommand.TOWERLAMP_DATA_REPORT:
            //    case enumCraneCommand.TOWERLAMP_SET:
            //    default:
            //        break;
            //}
            //#endregion
        }


        //private bool CheckRMStateInit()
        //{
        //    DateTime curTime = DateTime.Now;
        //    eRMPmacState mRMState = eRMPmacState.Unknown;


        //    while (!IsTimeOut(curTime, RMParameter.InitTimeout))
        //    {

        //        mRMState = (eRMPmacState)PvarGet(pRMState); // p2

        //        UInt64 p0 = PvarGet(pPLCInitializeState); // 1

        //        if (mRMState == eRMPmacState.Initialize_Not_Done && p0 == 1) // p2 == 1
        //        {
        //            Log.LogManager.WriteConsoleLog(eLogLevel.Info, "R / M Status  Initializing 완료{0}", mRMState);
        //            Thread.Sleep(100);
        //            return true;
        //        }
        //        Thread.Sleep(500);
        //    }
        //    return false;
        //}
        public override short RMAlarmCheck()
        {
            short AlarmCode = 0;

            if (this.SimulMode)
            {
                if (GlobalData.Current.Alarm_Manager.CheckModuleHeavyAlarmExist(ModuleName))
                {
                    return 1;
                }
            }
            else
            {
                AlarmCode = PLC_ErrorCode;
                if (AlarmCode > 0)
                {
                    GlobalData.Current.Alarm_Manager.AlarmOccur(AlarmCode.ToString(), ModuleName);
                }
            }
            return AlarmCode;
        }
        public string ChangePmacAlarmCode(int code)
        {
            string valCode = "0";

            LogManager.WriteRobotLog(eLogLevel.Info, this.ModuleName, "Pmac Alarm Code {0} 변환 진행", code);

            //1   EMG 에러  TOPTEC ROBOT       N N   Y
            //2   로봇수행중 명령중복발생    TOPTEC ROBOT       N N   Y
            //3   리밋에러 발생 TOPTEC ROBOT       N N   Y
            //4   시스템 알람  TOPTEC ROBOT       N N   Y
            //5   GET 실패  TOPTEC ROBOT       N N   Y
            //6   PUT 실패  TOPTEC ROBOT       N N   Y
            //7   GET 동작전 Arm위에 자재가 감지되었습니다.TOPTEC ROBOT       N N   Y
            //8   PUT 동작전 Arm위에 자재가 감지되지 않습니다.	TOPTEC ROBOT       N N   Y
            //9   Arm 접기 실패 TOPTEC  ROBOT N   N Y
            //10  Clamp 실패    TOPTEC ROBOT       N N   Y
            //11  UnClamp 실패  TOPTEC ROBOT       N N   Y
            //12  서보오프 TOPTEC  ROBOT N   N Y
            //13  목적지 이상  TOPTEC ROBOT       N N   Y
            //14  홈실행 실패  TOPTEC ROBOT       N N   Y
            //15  파라미터 에러 TOPTEC ROBOT       N N   Y
            //16  홈이 완료되지 않음 TOPTEC  ROBOT N   N Y
            //17  자재감지센서 감지되지않음   TOPTEC ROBOT       N N   Y
            //18  자재감지센서 감지됨  TOPTEC ROBOT       N N   Y
            //19  자재안착상태 불안정  TOPTEC ROBOT       N N   Y
            //20  주행축 리밋에러    TOPTEC ROBOT       N N   Y
            //21  승강축 리밋에러    TOPTEC ROBOT       N N   Y
            //22  회전축 리밋에러    TOPTEC ROBOT       N N   Y
            //23  암축 리밋에러 TOPTEC ROBOT       N N   Y
            //24  오토티칭 실패 TOPTEC ROBOT       N N   Y
            //25  문열림 확인  TOPTEC ROBOT       N N   Y
            //26  암의 위치가 홈이 아님    TOPTEC ROBOT       N N   Y
            //30  승강축 벨트이상    TOPTEC ROBOT       N N   Y
            //31  암 위치이상  TOPTEC ROBOT       N N   Y
            //40  자재감지됨 TOPTEC  ROBOT N   N N
            //41  자재감지되지않음 TOPTEC  ROBOT N   N N
            //42  투입포트에 자재안착할수 없음 TOPTEC  ROBOT N   N Y
            //43  자재 높이 이상 TOPTEC  ROBOT N   N Y
            //60  포지션 에러  TOPTEC ROBOT       N N   Y
            //99  포지션 오버  TOPTEC ROBOT       N N   Y
            //9001    로봇통신실패 TOPTEC  ROBOT N   N Y

            switch (code)
            {
                case 0:    // ERR_NONE 정상 (에러 없음)
                    break;
                case 1:    //ERR_FORK_POS_NOT_SAFETY Fork축이 Safety Pos가 아니면 발생
                    valCode = ROBOT_FOLDING_FAIL;
                    break;

                case 2:    //ERR_TARGET_SHELF_ID_INVALID Target Shelf ID가 지정이 안되면 발생
                    valCode = ROBOT_SYSTEM_ALARM;
                    break;
                case 3:    //ERR_GET_MOVE_ALREADY_EXIST
                    valCode = ROBOT_GET_FAIL;
                    break;
                case 4:    //ERR_IM_NOT_READY 레디상태가 아니면 발생
                case 5:    //ERR_FOUP_NOT_EXIST
                    valCode = ROBOT_GET_FAIL;
                    break;
                case 6:    //ERR_NO_REFLECTOR //반사판 에러
                    valCode = POSITION_ERROR;
                    break;

                case 8:    //ERR_PUT_FOUP_ALREADY_EXIST
                    valCode = MATERIAL_DETECTED;
                    break;
                case 7:    //ERR_PUT_MOVE_DID_NOT_HAVE
                case 9:    //ERR_FOUP_STILL_EXIST
                case 11:    //ERR_SENSOR_CONT_TYPE 센서의 접점이 잘못 설정됐습니다.
                    valCode = ROBOT_PARAMETER_ERROR;
                    break;
                case 91:    //ERR_MOTION_HOLD Rack Master 주행 도중 잘못된 명령이 날아왔습니다.
                    valCode = ROBOT_DOUBLE_CMD_ERROR;
                    break;
                case 101:    //ERR_RM_INIT_NOT_DONE 이니셜이 완료되지 않았습니다.
                case 102:    //ERR_RM_INITIALIZING 이니셜 중입니다.
                    valCode = HOME_NOT_COMPLETE;
                    break;
                case 103:    //ERR_RM_MOVING Rack Master가 이동중입니다.
                case 104:    //ERR_RM_TRANSFERING Rack Master가 반송 작업중입니다.
                    valCode = ROBOT_DOUBLE_CMD_ERROR;
                    break;
                case 105:    //ERR_RM_TEACHING Rack Master가 티칭중입니다.
                case 106:    //ERR_RM_JOGGING Jog 모드 입니다.
                case 107:    //ERR_RM_ERROR_STATE 에러 상태입니다.
                case 108:    //ERR_RM_STATUS_UNKNOWN UNKNOWN 상태입니다.
                case 109:    //ERR_RM_MODE_AUTO Auto 상태 입니다.
                    valCode = ROBOT_PARAMETER_ERROR;
                    break;
                case 110:    //ERR_UNKNOWN_SHELF_ID 목적지가 잘못됐습니다.
                    valCode = ROBOT_DESTINATION_FAIL;
                    break;
                case 111:    //ERR_TRAY_NOT_EXIST_ON_SHELF Shelf에 Tray가 없습니다.
                case 112:    //ERR_TRAY_EXIST_ON_SHELF Shelf에 Tray가 있습니다.
                    valCode = MATERIAL_NOT_DETECTED;
                    break;

                case 113:    //ERR_INITIAL_TIMEOUT 이니셜이 시간 내에 완료되지 않았습니다.
                case 114:    //ERR_MOVE_TIMEOUT MOVE 명령이 시간 내에 완료되지 않았습니다.
                    valCode = ROBOT_HOME_ERROR;
                    break;
                case 115:    //ERR_PUT_TIMEOUT GET 명령이 시간 내에 완료되지 않았습니다.
                    valCode = ROBOT_GET_FAIL;
                    break;
                case 116:    //ERR_GET_TIMEOUT PUT 명령이 시간 내에 완료되지 않았습니다.
                    valCode = ROBOT_PUT_FAIL;
                    break;
                case 118:    //ERR_SHELF_POSITION_ERROR Shelf의 Teach Point에 반사판 미감지시 발생
                    valCode = POSITION_ERROR;
                    break;
                case 119:    //ERR_CMD_MOTION_STOP SCS에서 급정지시켰습니다.
                case 121:    //ERR_FOUP_NOT_EXIST_ON_FORK Fork 위에 FOUP이 감지가 안될경우 발생
                    valCode = ROBOT_NONE_MATERIAL;
                    break;
                case 122:    //ERR_FOUP_EXIST_ON_FORK Fork 위에 FOUP이 감지가 되면 발생
                    valCode = ROBOT_USE_MATERIAL;
                    break;
                case 131:    //ERR_SLANT_CHECK_OPTION 기울어짐 감지 옵션이 잘못됐습니다.
                case 132:    //ERR_SLANT_CHECK_FAIL 기울어짐 감지 실패하였습니다.
                case 133:    //ERR_SLANT_DOWN_LEFT 하단 왼쪽으로 자재가 기울어졌습니다.
                case 134:    //ERR_SLANT_DOWN_RIGHT 하단 오른쪽으로 자재가 기울어졌습니다.
                case 135:    //ERR_SLANT_UP_LEFT 상단 왼쪽으로 자재가 기울어졌습니다.
                case 136:    //ERR_SLANT_UP_RIGHT 상단 오른쪽으로 자재가 기울어졌습니다.
                case 141:    //ERR_TRAY_EXTRUDE_FRONT 자재가 앞으로 돌출됐습니다.
                case 142:    //ERR_TRAY_EXTRUDE_REAR 자재가 뒤로 돌출됐습니다.
                case 143:    //ERR_TRAY_EXTRUDE_ALL 자재가 앞 뒤 모두 돌출됐습니다.
                    valCode = MATERIAL_SENSOR_NOT_DETECTED;
                    break;
                case 151:    //ERR_ID_IS_NOT_PORT 포트가 아닌 곳에서 인터락이 실행됐습니다.
                case 152:    //ERR_LOCATION_NOT_PORT 현재 위치값이 포트 티칭 값과 다릅니다.
                    valCode = ROBOT_PARAMETER_ERROR;
                    break;
                case 181:    //ERR_FORK_NOT_SAFE_POSITION 포크가 전진 상태입니다.
                    valCode = ROBOT_ARM_NOT_HOMEPOSITION;
                    break;
                case 191:    //ERR_CANNOT_CLEAR_ALARM 알람을 해제하지 못하였습니다.
                case 201:    //ERR_UNKNOWN_TEACH_SAVE_OPTION 티칭값 저장 명령이 잘못됐습니다.
                    break;
                case 211:    //ERR_MD_CHG_RM_INIT_NOT_DONE 이니셜이 완료되지 않았습니다. (모드 변경 명령 시)
                case 212:    //ERR_MD_CHG_RM_INITIALIZING 이니셜 중입니다. (모드 변경 명령 시)
                    valCode = ROBOT_HOME_ERROR;
                    break;
                case 213:    //ERR_MD_CHG_RM_MOVING Rack Master가 이동중입니다. (모드 변경 명령 시)
                case 214:    //ERR_MD_CHG_RM_TRANSFERING Rack Master가 반송 작업중입니다. (모드 변경 명령 시)
                case 215:    //ERR_MD_CHG_RM_TEACHING Rack Master가 티칭중입니다. (모드 변경 명령 시)
                case 216:    //ERR_MD_CHG_RM_JOGGING Jog 모드 입니다. (모드 변경 명령 시)
                case 217:    //ERR_MD_CHG_RM_ERROR_STATE 에러 상태입니다. (모드 변경 명령 시)
                case 218:    //ERR_MD_CHG_RM_STATUS_UNKNOWN UNKNOWN 상태입니다. (모드 변경 명령 시)
                case 300:    //ERR_PLC_0_DISABLED 사용 안함
                case 301:    //ERR_PLC_1_DISABLED 사용 안함
                case 302:    //ERR_PLC_2_DISABLED 사용 안함
                case 303:    //ERR_PLC_3_DISABLED 사용 안함
                case 304:    //ERR_PLC_4_DISABLED 사용 안함
                case 305:    //ERR_PLC_5_DISABLED 사용 안함
                case 306:    //ERR_PLC_6_DISABLED 사용 안함
                case 307:    //ERR_PLC_7_DISABLED 사용 안함
                case 308:    //ERR_PLC_8_DISABLED 사용 안함
                case 309:    //ERR_PLC_9_DISABLED 사용 안함
                case 310:    //ERR_PLC_10_DISABLED 엔코더 데이터 업데이트 프로그램이 꺼져있습니다.
                case 311:    //ERR_PLC_11_DISABLED 사용 안함
                case 312:    //ERR_PLC_12_DISABLED 사용 안함
                case 313:    //ERR_PLC_13_DISABLED 사용 안함
                case 314:    //ERR_PLC_14_DISABLED 사용 안함
                case 315:    //ERR_PLC_15_DISABLED 사용 안함
                case 316:    //ERR_PLC_16_DISABLED 사용 안함
                case 317:    //ERR_PLC_17_DISABLED 사용 안함
                case 318:    //ERR_PLC_18_DISABLED 사용 안함
                case 319:    //ERR_PLC_19_DISABLED 사용 안함
                case 320:    //ERR_PLC_20_DISABLED 사용 안함
                case 321:    //ERR_PLC_21_DISABLED 사용 안함
                case 322:    //ERR_PLC_22_DISABLED 사용 안함
                case 323:    //ERR_PLC_23_DISABLED 사용 안함
                case 324:    //ERR_PLC_24_DISABLED 사용 안함
                case 325:    //ERR_PLC_25_DISABLED 사용 안함
                case 326:    //ERR_PLC_26_DISABLED 사용 안함
                case 327:    //ERR_PLC_27_DISABLED 사용 안함
                case 328:    //ERR_PLC_28_DISABLED 사용 안함
                case 329:    //ERR_PLC_29_DISABLED 사용 안함
                case 330:    //ERR_PLC_30_DISABLED 사용 안함
                case 331:    //ERR_PLC_31_DISABLED 사용 안함
                    valCode = ROBOT_PARAMETER_ERROR;
                    break;
                case 401:    //ERR_GRIPPER_HOME_TIMEOUT Grip/ Ungrip 프로그램이 ON으로 변경되지 않았습니다.
                    valCode = ROBOT_CLAMP_FAIL;
                    break;
                case 402:    //ERR_GRIP_DIR_UNKNOWN Grip/ Ungrip 명령이 잘못됐습니다.
                    valCode = ROBOT_UNCLAMP_FAIL;
                    break;
                case 403:    //ERR_GRIPPER_GRIP_TIMEOUT Grip이 일정 시간 내 완료되지 못했습니다.
                    valCode = ROBOT_CLAMP_FAIL;
                    break;
                case 404:    //ERR_GRIPPER_UNGRIP_TIMEOUT Ungrip이 일정 시간 내 완료되지 못했습니다.
                    valCode = ROBOT_UNCLAMP_FAIL;
                    break;
                case 405:    //ERR_GRIP_PLC_RUN_TIMEOUT Grip Ungrip 프로그램이 일정시간 내에 켜지지 않았습니다.
                    valCode = ROBOT_CLAMP_FAIL;
                    break;
                case 406:    //ERR_GRIP_PLC_DIS_TIMEOUT Grip Ungrip 프로그램이 완료 후 꺼지지 않았습니다.
                    valCode = ROBOT_UNCLAMP_FAIL;
                    break;
                case 407:    //ERR_GRIPPER_GRIP Grip 동작이 완료됐는데 Grip 센서 감지가 안됩니다.
                    valCode = ROBOT_CLAMP_FAIL;
                    break;
                case 408:    //ERR_GRIPPER_UNGRIP Ungrip 동작이 완료됐는데  Ungrip 센서 감지가 안됩니다.
                    valCode = ROBOT_UNCLAMP_FAIL;
                    break;
                case 601:    //ERR_RM_CARRIER_EXIST 포크에 자재가 있는데  Get 명령이 내려왔습니다.
                    valCode = ROBOT_USE_MATERIAL;
                    break;

                case 1001:    //    ERR_BELT_1_ERROR 승강축 벨트 상단 1_1이 감지가 안됩니다.
                case 1002:    //    ERR_BELT_1_2_ERROR 승강축 벨트 상단 1_2가 감지가 안됩니다.
                case 1003:    //    ERR_BELT_2_1_ERROR 승강축 벨트 상단 2_1이 감지가 안됩니다.
                case 1004:    //    ERR_BELT_UP2_2_ERROR 승강축 벨트 상단 2_2가 감지가 안됩니다.
                case 1005:    //    ERR_BELT_DOWN1_ERROR 승강축 벨트 하단 1이 감지가 안됩니다.
                case 1006:    //    ERR_BELT_DOWN2_ERROR 승강축 벨트 하단 2가 감지가 안됩니다.
                    valCode = ROBOT_LIFT_BELT_ERROR;
                    break;
                case 1007:    //    ERR_MC_OFF_STATE MC(Magnet Contactor)가 차단됐습니다.
                    valCode = ROBOT_EMG_ERROR;
                    break;
                case 1008:    //    ERR_EMO_SWITCH_OFF RM에 부착된 EMO버튼이 눌렸습니다.
                    valCode = ROBOT_EMG_ERROR;
                    break;
                case 1009:    //    ERR_DOOR_OPEN_STATE 문이 열려있습니다.
                    valCode = ROBOT_DOOR_OPEN;
                    break;
                case 1010:    //    ERR_LC_STATE Light Curtain이 감지되었습니다.
                case 1011:    //    ERR_EBOX_OPEN_STATE 전장 박스가 열려있습니다.
                case 1012:    //    ERR_INVALID_AXIS 축 정보가 잘못됐습니다.
                case 1021:    //    ERR_MC_ON_TIMEOUT 사용 안함
                case 1022:    //    ERR_MC_OFF_TIMEOUT 사용 안함
                case 1023:    //    ERR_OUTPUT_CTRL 사용 안함
                    valCode = ROBOT_SYSTEM_ALARM;
                    break;
                case 1101:    //    ERR_AX1_AMP_ON_TIMEOUT 일정 시간 내에 해당축 Amp가 On 되지 않았습니다.
                case 1102:    //    ERR_AX2_AMP_ON_TIMEOUT 일정 시간 내에 해당축 Amp가 On 되지 않았습니다.
                case 1103:    //    ERR_AX3_AMP_ON_TIMEOUT 일정 시간 내에 해당축 Amp가 On 되지 않았습니다.
                case 1104:    //    ERR_AX4_AMP_ON_TIMEOUT 일정 시간 내에 해당축 Amp가 On 되지 않았습니다.
                case 1105:    //    ERR_AX5_AMP_ON_TIMEOUT 일정 시간 내에 해당축 Amp가 On 되지 않았습니다.
                case 1111:    //    ERR_AX1_AMP_OFF_TIMEOUT 일정 시간 내에 해당축 Amp가 On 되지 않았습니다.
                case 1112:    //    ERR_AX2_AMP_OFF_TIMEOUT 일정 시간 내에 해당축 Amp가 On 되지 않았습니다.
                case 1113:    //    ERR_AX3_AMP_OFF_TIMEOUT 일정 시간 내에 해당축 Amp가 On 되지 않았습니다.
                case 1114:    //    ERR_AX4_AMP_OFF_TIMEOUT 일정 시간 내에 해당축 Amp가 On 되지 않았습니다.
                case 1115:    //    ERR_AX5_AMP_OFF_TIMEOUT 일정 시간 내에 해당축 Amp가 On 되지 않았습니다.
                case 1121:    //    ERR_AX1_AMP_FAULT 포크축 Amp Fault가 발생했습니다.
                case 1122:    //    ERR_AX2_AMP_FAULT 주행축 Amp Fault가 발생했습니다.
                case 1123:    //    ERR_AX3_AMP_FAULT 승강축 Amp Fault가 발생했습니다.
                case 1124:    //    ERR_AX4_AMP_FAULT 회전축 Amp Fault가 발생했습니다.
                case 1125:    //    ERR_AX5_AMP_FAULT 그립축 Amp Fault가 발생했습니다.
                case 1131:    //    ERR_AX1_FOLLOWING_FATAL 포크축 Following Error가 발생했습니다.
                case 1132:    //    ERR_AX2_FOLLOWING_FATAL 주행축 Following Error가 발생했습니다.
                case 1133:    //    ERR_AX3_FOLLOWING_FATAL 승강축 Following Error가 발생했습니다.
                case 1134:    //    ERR_AX4_FOLLOWING_FATAL 회전축 Following Error가 발생했습니다.
                case 1135:    //    ERR_AX5_FOLLOWING_FATAL 그립축 Following Error가 발생했습니다.
                case 1141:    //    ERR_AX1_AMP_OFF_STATE 포크축 Amp가 Off 상태입니다.
                case 1142:    //    ERR_AX2_AMP_OFF_STATE 주행축 Amp가 Off 상태입니다.
                case 1143:    //    ERR_AX3_AMP_OFF_STATE 승강축 Amp가 Off 상태입니다.
                case 1144:    //    ERR_AX4_AMP_OFF_STATE 회전축 Amp가 Off 상태입니다.
                case 1145:    //    ERR_AX5_AMP_OFF_STATE 그립축 Amp가 Off 상태입니다.
                    valCode = ROBOT_SERVO_OFF;
                    break;
                case 1151:    //    ERR_AX1_NEG_SW_LIMIT_OVER 포크축 -방향 Software Limit 값을 초과하는 이동 명령을 받았습니다.
                case 1152:    //    ERR_AX2_NEG_SW_LIMIT_OVER 주행축 -방향 Software Limit 값을 초과하는 이동 명령을 받았습니다.
                case 1153:    //    ERR_AX3_NEG_SW_LIMIT_OVER 승강축 -방향 Software Limit 값을 초과하는 이동 명령을 받았습니다.
                case 1154:    //    ERR_AX4_NEG_SW_LIMIT_OVER 회전축 -방향 Software Limit 값을 초과하는 이동 명령을 받았습니다.
                case 1155:    //    ERR_AX5_NEG_SW_LIMIT_OVER 사용 안함
                case 1156:    //    ERR_AX1_POS_SW_LIMIT_OVER 포크축 +방향 Software Limit 값을 초과하는 이동 명령을 받았습니다.
                case 1157:    //    ERR_AX2_POS_SW_LIMIT_OVER 주행축 +방향 Software Limit 값을 초과하는 이동 명령을 받았습니다.
                case 1158:    //    ERR_AX3_POS_SW_LIMIT_OVER 승강축 +방향 Software Limit 값을 초과하는 이동 명령을 받았습니다.
                case 1159:    //    ERR_AX4_POS_SW_LIMIT_OVER 회전축 +방향 Software Limit 값을 초과하는 이동 명령을 받았습니다.
                    valCode = ROBOT_LIMIT_ERROR;
                    break;
                case 1160:    //    ERR_AX5_POS_SW_LIMIT_OVER 사용 안함
                case 1161:    //    ERR_AX1_SPEED_LIMIT_OVER 사용 안함
                case 1162:    //    ERR_AX2_SPEED_LIMIT_OVER 주행축 -방향 Limit 센서가 감지되었습니다.
                case 1163:    //    ERR_AX3_SPEED_LIMIT_OVER 승강축 -방향 Limit 센서가 감지되었습니다.
                case 1164:    //    ERR_AX4_SPEED_LIMIT_OVER 사용 안함
                case 1165:    //    ERR_AX5_SPEED_LIMIT_OVER 사용 안함
                case 1166:    //    ERR_AX1_POS_HW_LIMIT_OVER 사용 안함
                    valCode = ROBOT_ARM_LIMIT_ERROR;
                    break;
                case 1167:    //    ERR_AX2_POS_HW_LIMIT_OVER 주행축 +방향 Limit 센서가 감지되었습니다.
                    valCode = ROBOT_DRIVE_LIMIT_ERROR;
                    break;
                case 1168:    //    ERR_AX3_POS_HW_LIMIT_OVER 승강축 +방향 Limit 센서가 감지되었습니다.
                    valCode = ROBOT_LIFT_LIMIT_ERROR;
                    break;
                case 1169:    //    ERR_AX4_POS_HW_LIMIT_OVER 사용 안함
                    valCode = ROBOT_TURN_LIMIT_ERROR;
                    break;
                case 1170:    //    ERR_AX5_POS_HW_LIMIT_OVER 사용 안함
                case 1171:    //    ERR_AX1_SPEED_LIMIT_OVER 포크축 속도가 제한 범위를 초과했습니다.
                case 1172:    //    ERR_AX2_SPEED_LIMIT_OVER 주행축 속도가 제한 범위를 초과했습니다.
                case 1173:    //    ERR_AX3_SPEED_LIMIT_OVER 승강축 속도가 제한 범위를 초과했습니다.
                case 1174:    //    ERR_AX4_SPEED_LIMIT_OVER 회전축 속도가 제한 범위를 초과했습니다.
                case 1175:    //    ERR_AX5_SPEED_LIMIT_OVER 그립축 속도가 제한 범위를 초과했습니다.
                case 1181:    //    ERR_AX1_ACCEL_LIMIT_OVER 포크축 가속도가 제한 범위를 초과했습니다.
                case 1182:    //    ERR_AX2_ACCEL_LIMIT_OVER 주행축 가속도가 제한 범위를 초과했습니다.
                case 1183:    //    ERR_AX3_ACCEL_LIMIT_OVER 승강축 가속도가 제한 범위를 초과했습니다.
                case 1184:    //    ERR_AX4_ACCEL_LIMIT_OVER 회전축 가속도가 제한 범위를 초과했습니다.
                case 1185:    //    ERR_AX5_ACCEL_LIMIT_OVER 그립축 가속도가 제한 범위를 초과했습니다.
                case 1191:    //    ERR_AX1_DECEL_LIMIT_OVER 포크축 감속도가 제한 범위를 초과했습니다.
                case 1192:    //    ERR_AX2_DECEL_LIMIT_OVER 주행축 감속도가 제한 범위를 초과했습니다.
                case 1193:    //    ERR_AX3_DECEL_LIMIT_OVER 승강축 감속도가 제한 범위를 초과했습니다.
                case 1194:    //    ERR_AX4_DECEL_LIMIT_OVER 회전축 감속도가 제한 범위를 초과했습니다.
                case 1195:    //    ERR_AX5_DECEL_LIMIT_OVER 그립축 감속도가 제한 범위를 초과했습니다.
                case 1196:    //    ERR_AX1_JOG_DIRECTION 포크축 조그 방향이 잘못 입력됐습니다.
                case 1197:    //    ERR_AX2_JOG_DIRECTION 주행축 조그 방향이 잘못 입력됐습니다.
                case 1198:    //    ERR_AX3_JOG_DIRECTION 승강축 조그 방향이 잘못 입력됐습니다.
                case 1199:    //    ERR_AX4_JOG_DIRECTION 회전축 조그 방향이 잘못 입력됐습니다.
                case 1200:    //    ERR_AX5_JOG_DIRECTION 그립축 조그 방향이 잘못 입력됐습니다.
                case 1206:    //    ERR_AX1_UNKNOWN_POSITION 포크축 조그 방향이 잘못 입력됐습니다.
                case 1207:    //    ERR_AX2_UNKNOWN_POSITION 주행축 조그 방향이 잘못 입력됐습니다.
                case 1208:    //    ERR_AX3_UNKNOWN_POSITION 승강축 조그 방향이 잘못 입력됐습니다.
                case 1209:    //    ERR_AX4_UNKNOWN_POSITION 회전축 조그 방향이 잘못 입력됐습니다.
                case 1210:    //    ERR_AX5_UNKNOWN_POSITION 그립축 조그 방향이 잘못 입력됐습니다.
                    valCode = ROBOT_SYSTEM_ALARM;
                    break;
                case 1601:    //    ERR_FORK_POS_UNSAFE 포크축이 비안전 위치에 있습니다.
                    valCode = ROBOT_ARM_LIMIT_ERROR;
                    break;
                case 1602:    //    ERR_DRIVE_POS_UNSAFE DRIVE축이 비안전 위치에 있습니다.
                    valCode = ROBOT_LIFT_LIMIT_ERROR;
                    break;
                case 1603:    //    ERR_Z_POS_UNSAFE Z축이 비안전 위치에 있습니다.
                    valCode = ROBOT_LIFT_LIMIT_ERROR;
                    break;
                case 1604:    //    ERR_TURN_POS_UNSAFE TUNE축이 비안전 위치에 있습니다.
                    valCode = ROBOT_TURN_LIMIT_ERROR;
                    break;
                case 2000:    //    ERR_MOVE_MOTION_TIMEOUT 사용 안함
                case 2001:    //    ERR_PROG_1_ACTIVE_TIMEOUT 사용 안함
                case 2002:    //    ERR_PROG_2_ACTIVE_TIMEOUT 사용 안함
                case 2003:    //    ERR_PROG_3_ACTIVE_TIMEOUT 포크 원점 이동이 일정 시간내에 완료되지 못했습니다.
                    valCode = ROBOT_HOME_ERROR;
                    break;
                case 2004:    //    ERR_PROG_4_ACTIVE_TIMEOUT 사용 안함
                case 2005:    //    ERR_PROG_5_ACTIVE_TIMEOUT 사용 안함
                case 2006:    //    ERR_PROG_6_ACTIVE_TIMEOUT 사용 안함
                case 2011:    //    ERR_PROG_11_ACTIVE_TIMEOUT 사용 안함
                case 2012:    //    ERR_PROG_12_ACTIVE_TIMEOUT 사용 안함
                case 2013:    //    ERR_PROG_13_ACTIVE_TIMEOUT 사용 안함
                case 2014:    //    ERR_PROG_14_ACTIVE_TIMEOUT 사용 안함
                case 2015:    //    ERR_PROG_15_ACTIVE_TIMEOUT 사용 안함
                case 2021:    //    ERR_PROG_21_ACTIVE_TIMEOUT 사용 안함
                case 2022:    //    ERR_PROG_22_ACTIVE_TIMEOUT 사용 안함
                case 2031:    //    ERR_PROG_31_ACTIVE_TIMEOUT 사용 안함
                case 2041:    //    ERR_PROG_41_ACTIVE_TIMEOUT 사용 안함
                case 2101:    //    ERR_PROG_41_ACTIVE_TIMEOUT 사용 안함
                case 2102:    //    ERR_PROG_102_ACTIVE_TIMEOUT 사용 안함
                    valCode = ROBOT_PARAMETER_ERROR;
                    break;
                case 2500:    //    ERR_PROG_ABORT_TIMEOUT 포크 원점 이동 실행 전에 동작하던 모션이 종료되지 않았습니다.
                    valCode = ROBOT_HOME_ERROR;
                    break;
                case 2501:    //    ERR_PROG_1_ABORT_TIMEOUT 사용 안함
                case 2502:    //    ERR_PROG_2_ABORT_TIMEOUT 사용 안함
                case 2503:    //    ERR_PROG_3_ABORT_TIMEOUT 사용 안함
                case 2504:    //    ERR_PROG_4_ABORT_TIMEOUT 사용 안함
                case 2505:    //    ERR_PROG_5_ABORT_TIMEOUT 사용 안함
                case 2506:    //    ERR_PROG_6_ABORT_TIMEOUT 사용 안함
                case 2511:    //    ERR_PROG_11_ABORT_TIMEOUT 사용 안함
                case 2512:    //    ERR_PROG_12_ABORT_TIMEOUT 사용 안함
                case 2513:    //    ERR_PROG_13_ABORT_TIMEOUT 사용 안함
                case 2514:    //    ERR_PROG_14_ABORT_TIMEOUT 사용 안함
                case 2515:    //    ERR_PROG_15_ABORT_TIMEOUT 사용 안함
                case 2521:    //    ERR_PROG_21_ABORT_TIMEOUT 사용 안함
                case 2522:    //    ERR_PROG_22_ABORT_TIMEOUT 사용 안함
                case 2531:    //    ERR_PROG_31_ABORT_TIMEOUT 사용 안함
                case 2541:    //    ERR_PROG_41_ABORT_TIMEOUT 사용 안함
                case 2601:    //    ERR_PROG_101ABORT_TIMEOUT 사용 안함
                case 2602:    //    ERR_PROG_102_ABORT_TIMEOUT 사용 안함
                case 2901:    //    ERR_PRE_PROG_NOT_FINISHED 이전 프로그램이 종료되지 않았습니다.
                case 3001:    //    ERR_MAX_COLUMN_OVER 최대 행을 벗어났습니다.
                case 3002:    //    ERR_MAX_ROW_OVER 최대 열을 벗어났습니다.
                case 3003:    //    ERR_MAX_INDEX_OVER 최대 Index를 벗어났습니다.
                case 3004:    //    ERR_UNKNOWN_SHELF_DIR Shelf 방향이 입력되지 않았습니다.
                case 3005:    //    ERR_UNKNOWN_SHELF_INDEX Index값이 입력되지 않았습니다.
                case 4001:    //    ERR_FORK_TARGET_INVALID 사용 안함
                case 4002:    //    ERR_DRIVE_TARGET_INVALID 사용 안함
                case 4003:    //    ERR_Z_TARGET_INVALID 사용 안함
                case 4004:    //    ERR_TURN_TARGET_INVALID 사용 안함
                    valCode = ROBOT_PARAMETER_ERROR;
                    break;
                case 5000:    //    ERR_CLAMP_UNCHUCK Clamp 가 Unchuck 이 진행되지 않았습니다.
                    valCode = ROBOT_UNCLAMP_FAIL;
                    break;
                case 5001:    //    ERR_CLAMP_CHUCK Clamp 가 Chuck 이 진행되지 않았습니다.
                    valCode = ROBOT_CLAMP_FAIL;
                    break;
                case 5002:    //    SHELF GET 자제 기울러짐 센서 가 감지 되었습니다.
                    valCode = SHELF_MATERIAL_STATE_ERROR;
                    break;
                case 5003:    //    ERR_CARRIER_TILT 자제 기울러짐 센서 가 감지 되었습니다.
                    valCode = ROBOT_TRAY_FB_FRONT_TILT_ERROR;
                    break;
                case 5004:    //    ERR_CARRIER_TILT 자제 기울러짐 센서 가 감지 되었습니다.
                    valCode = ROBOT_TRAY_FB_REAR_TILT_ERROR;
                    break;


                case 6001:    //    ERR_AUTO_TAECHING_FDEF_DATA 앞면 티칭 기준 값이 없습니다.
                case 6002:    //    ERR_AUTO_TAECHING_RDEF_DATA 뒷면 티칭 기준 값이 없습니다.
                case 6003:    //    ERR_AUTO_TEACHING_DATA_NONE 특정 Shelf 티칭 시 Shelf 개수가 누락되었습니다.
                    valCode = ROBOT_AUTO_TEACHING_FAIL;
                    break;
                case 6004:    //    ERR_TEACH_SHELF_ID_INVALID 특정 Shelf 티칭 시 Shelf ID가 누락되었습니다.
                case 6005:    //    ERR_SHELF_INDEX_INVALID Shelf Index가 잘못되었습니다.
                case 6006:    //    ERR_SHELF_INDEX_TOO_BIG Shelf Index가 최대 배열 값을 초과했습니다.
                case 6007:    //    ERR_DEFAULT_DATA_UNKNOWN 티칭 기준 값이 없습니다.
                case 6008:    //    ERR_TEACH_SHELF_ID_MISSMATCH 사용 안함
                case 6009:    //    ERR_TEACH_POS_UNKNOWN 사용 안함
                case 6010:    //    ERR_ENABLE_CHECK_TIMEOUT rticplc가 Enable 되지 않았습니다.
                case 6011:    //    ERR_TEACH_FIND_FAIL 티칭 시 반사판을 찾지 못했습니다.
                case 6012:    //    ERR_TEACH_APPLY_RUNNING 티칭값 저장중입니다.
                case 7001:    //    ERR_JOG_TYPE_UNKNOWN Jog Type이 입력되지 않았습니다.
                case 7002:    //    ERR_JOG_AXIS_UNKNOWN 축이 입력되지 않았습니다.
                case 7003:    //    ERR_JOG_RUNNING_STATE Jog 동작중입니다.
                case 8001:    //    ERR_INVALID_PLACEMENT 사용 안함
                case 8002:    //    ERR_CARRIER_DETECTED 자재 감지 센서가 감지되었습니다.
                case 8003:    //    ERR_CARRIER_NO_DETECTED 자재 감지 센서가 감지되지 않았습니다.
                case 8004:    //    ERR_CARRIER_NO_DETECT_B 뒷면 자재 감지 센서가 감지되지 않았습니다.
                case 8005:    //    ERR_CARRIER_NO_DETECT_F 앞면 자재 감지 센서가 감지되지 않았습니다.
                case 9001:    //    ERR_PLC10_RUN_WAIT 사용 안함
                case 9031:    //    ERR_WDT_FAULT_BY_RTIC RTIC에 의해 Watchdog Fault가 발생했습니다.
                case 9032:    //    ERR_WDT_FAULT_BY_BGC BGC에 의해 Watchdog Fault가 발생했습니다.
                case 9033:    //    ERR_WDT_FAULT_BY_UNKNOWN 알 수 없는 원인에 의해 Watchdog Fault가 발생했습니다.
                case 9101:    //    ERR_RM_MODE_AUTO Rack Master가 현재 Auto 모드입니다.
                case 9102:    //    ERR_RM_MODE_TEACHING Rack Master가 현재 Teaching 모드입니다.
                case 9110:    //    ERR_RM_NOT_HT_MODE 사용 안함
                case 9111:    //    ERR_HT_JOG_INVALID 사용 안함
                case 9999:   //    ERR_PGM_RUNNING_STATE 사용 안함
                    valCode = ROBOT_PARAMETER_ERROR;
                    break;
                default:
                    valCode = ROBOT_SYSTEM_ALARM;
                    break;
            }



            return valCode;
        }

        #endregion

        public string IntToBinaryString(int number)
        {
            const int mask = 1;
            var binary = string.Empty;
            if (number == 0)
            {
                binary = "0";
                return binary;
            }

            while (number > 0)
            {
                // Logical AND the number and prepend it to the result string
                binary = (number & mask) + binary;
                number = number >> 1;
            }
            return binary;
        }


        private bool isInRange(decimal dec, decimal min, decimal max, bool includesMin = true, bool includesMax = true)
        {
            return (includesMin ? (dec >= min) : (dec > min)) && (includesMax ? (dec <= max) : (dec < max));
        }

        private bool isInForkRange(decimal curPostion, decimal min, decimal max, bool includesMin = true, bool includesMax = true)
        {

            if (curPostion == 0)
            {
                return true;
            }
            else if (curPostion >= min && curPostion <= max)
            {
                return true;
            }
            else
                return false;

            //return (includesMin ? (curPostion >= min) : (curPostion > min)) && (includesMax ? (curPostion <= max) : (curPostion < max));
        }




        /// <summary>
        ///  20200217 Alarm 상태 추가
        /// </summary>
        /// <returns></returns>
        public bool GetAlarmState()
        {
            try
            {
                return CraneSC_State == eCraneSCState.ERROR;
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Fatal, ex.ToString());
            }
            return false;
        }

        public override eCraneArmState GetArmExtendState()
        {
            eCraneArmState CAS = PLC_Fork1_Extend;
            return CAS;
        }
        public short GetAlarmCode()
        {
            try
            {
                //210105 lsj PMacOnlineConncet -> RobotOnlineConncet 변경
                if (!this.RobotOnlineConncet)
                    return 0;

                return PLC_ErrorCode;
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Fatal, ex.ToString());
            }
            return 0;
        }

        /// <summary>
        /// CIM 에서 Alarm 클리어 케이스 처리
        /// </summary>
        /// <returns></returns>
        public override bool RMAlarmResetAction()
        {
            if (SimulMode)
            {
                CraneState = eCraneUIState.ONLINE;
                return true;
            }
            RMResetRequest = false;
            PC_EMG_STOP = false;
            PC_ErrorReset = false;
            if (CraneSC_State == eCraneSCState.IDLE && PLC_ErrorCode == 0) //이미 에러 상태가 아니면 리셋이 필요없다.
            {
                CraneState = eCraneUIState.ONLINE;
                return true;
            }
            PC_ErrorReset = true;
            LogManager.WriteConsoleLog(eLogLevel.Info, "Crane : {0} RMAlarmReset PC_ErrorReset On", ModuleName);
            Stopwatch timeWatch = Stopwatch.StartNew();
            while (true)
            {
                if (IsTimeout_SW(timeWatch, PLCTimeOut))
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "Crane : {0} RMAlarmReset 도중 PLC 응답 Timtout 발생하였습니다.Step :1", ModuleName);
                    PC_ErrorReset = false; //231218 RGJ 크레인 알람 리셋할때 타임아웃 나면 에러 리셋 비트는 Off 시킨다.
                    return false;
                }
                if (CraneSC_State == eCraneSCState.IDLE) //기상반 알람 클리어 체크
                {
                    PC_ErrorReset = false;
                    LogManager.WriteConsoleLog(eLogLevel.Info, "Crane : {0} RMAlarmReset PC_ErrorReset Off", ModuleName);
                    //GlobalData.Current.Alarm_Manager.AlarmClear(ModuleName); //알람 해제        //241023 HoN Alarm Clear 시점 변경      //시점 변경 주석
                    GlobalData.Current.MainBooth.SetPLCRMReportComplete(IsFirstRM, true);
                    PC_AlarmCode = 0;
                    break;
                }
                Thread.Sleep(PLCIF_Delay);
            }
            timeWatch.Restart();
            while (true)
            {
                if (IsTimeout_SW(timeWatch, PLCTimeOut))
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "Crane : {0} RMAlarmReset 도중 PLC Error Code Clear 응답 Timtout 발생하였습니다.Step :2", ModuleName);
                    return false;
                }
                if (PLC_ErrorCode == 0) //PLC 코드 체크
                {
                    GlobalData.Current.Alarm_Manager.AlarmClear(ModuleName); //알람 해제        //241023 HoN Alarm Clear 시점 변경      //시점 변경
                    LogManager.WriteConsoleLog(eLogLevel.Info, "Crane : {0} RMAlarmReset PLC_ErrorCode 0 Check", ModuleName);
                    GlobalData.Current.MainBooth.SetPLCRMReportComplete(IsFirstRM, false);
                    CraneState = eCraneUIState.ONLINE;
                    return true;
                }
                Thread.Sleep(PLCIF_Delay);
            }
        }
        public override eCraneSCMode GetRMMode()
        {
            try
            {
                //210105 lsj PMacOnlineConncet -> RobotOnlineConncet 변경
                if (SimulMode)
                    return eCraneSCMode.AUTO_RUN;

                return PLC_SCMODE;
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Fatal, ex.ToString());
            }

            return eCraneSCMode.OFFLINE;
        }

        private bool SendCraneCommand(CraneCommand cmd)
        {
            int CommandWriteTry = 0;
            int CommandWriteTryLimit = 5;//Write 리트라이 제한
            if (GetRMMode() != eCraneSCMode.AUTO_RUN) //Auto 모드가 아니면 실패처리
            {
                return false;
            }
            PC_CommandWriteComplete = 0; //동작전 초기화

            if (PLC_CommandAck > 0) //이미 Ack 가 올라와 있으면 에러
            {
                Thread.Sleep(500); //한번만 리트라이 해본다.
                if(PLC_CommandAck > 0)
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "Crane :{0} Command :{1} Command    Command_Ack Already On State! Interface Error.", ModuleName, cmd.Command);
                    GlobalData.Current.Alarm_Manager.AlarmOccurbyName("CRANE_PLC_COMMAND_ACK_ON_ERROR", ModuleName); //2024.05.08 lim, Alarm 재정의  //추후 알람 재정의 해야함
                    return false;
                };
            }

            //20240123 RGJ 크레인 커맨드 통신 문제로 커맨드 Parameter Write 실패하고   PC_CommandWriteComplete On 시키면 오동작 된 사례가 있어서 추가함.
            while (true)
            {
                PC_CarrierID_FORK1 = cmd.TargetCarrierID;
                PC_CraneCommand = cmd.Command;
                PC_CommandNumber_FORK1 = cmd.CommandNumber;
                PC_DestBank_FORK1 = (short)cmd.TargetBank; //목적지 설정
                PC_DestBay_FORK1 = (short)cmd.TargetBay;
                PC_DestLevel_FORK1 = (short)cmd.TargetLevel;
                PC_DestWorkPlace_FORK1 = cmd.TargetItem.iWorkPlaceNumber;
                PC_CommandUseFork = 0;
                PC_CarrierStability = 0;

                //230306 캐리어를 들고 있으면 캐리어 사이즈를 써준다. s
                if (CarrierExistSensor)
                {
                    CarrierItem CraneCarrier = InSlotCarrier;
                    if (CraneCarrier != null)
                    {
                        PC_PalletSize = CraneCarrier.PalletSize;
                    }
                }
                else
                {
                    //헝가리는 단장폭 안맞아서 문제되는게 포트에 넣을때만 문제... 중국은 모든 스토커가 장/단폭 겸용이라 주석 풀고 사용함.
                    //헝가리도 단장폭 정보 GET 전에 필요하다고 함. 
                    ePalletSize RecvPalletSize = ePalletSize.NONE;

                    if (cmd.TargetItem is ShelfItem)
                    {
                        ShelfItem TargetShelf = cmd.TargetItem as ShelfItem;
                        RecvPalletSize = TargetShelf.PalletSize;
                    }
                    PC_PalletSize = RecvPalletSize;
                }
                //230306 캐리어를 들고 있으면 캐리어 사이즈를 써준다. 
                Thread.Sleep(300); //231213 RGJ 커맨드 데이터 입력하고 Wrtie 완료 신호 주기전에 안정화 딜레이 추가.

                //Crane Command Matching 커맨드가 PC 영역에 확실히 써져있는지 다시 체크
                bool P1Check = PC_CarrierID_FORK1 == cmd.TargetCarrierID;
                bool P2Check = PC_CraneCommand == cmd.Command;
                bool P3Check = PC_CommandNumber_FORK1 == cmd.CommandNumber;
                bool P4Check = PC_DestBank_FORK1 == (short)cmd.TargetBank; //목적지 설정
                bool P5Check = PC_DestBay_FORK1 == (short)cmd.TargetBay;
                bool P6Check = PC_DestLevel_FORK1 == (short)cmd.TargetLevel;
                bool P7Check = PC_DestWorkPlace_FORK1 == cmd.TargetItem.iWorkPlaceNumber;

                if (P1Check && P2Check && P3Check && P4Check && P5Check && P6Check && P7Check)
                {
                    //OK
                    LogManager.WriteConsoleLog(eLogLevel.Info, "Crane :{0} Command :{1} Command Parameter Check OK", ModuleName, cmd.Command);
                    break;
                }
                else
                {
                    //NG
                    LogManager.WriteConsoleLog(eLogLevel.Info, "Crane :{0} Command :{1} Command Parameter Check NG! Rewrite Parameter. Tried:{2}", ModuleName, cmd.Command, CommandWriteTry);
                    CommandWriteTry++;

                    //2024.05.08 lim, Mismatch Data Log 추가
                    string sTemp = "";
                    sTemp += P1Check ? "" : string.Format("ID PLC : {0}, PC : {1}, ", PC_CarrierID_FORK1, cmd.TargetCarrierID);
                    sTemp += P2Check ? "" : string.Format("Cmd PLC : {0}, PC : {1}, ", PC_CraneCommand, cmd.Command);
                    sTemp += P3Check ? "" : string.Format("Num PLC : {0}, PC : {1}, ", PC_CommandNumber_FORK1, cmd.CommandNumber);
                    sTemp += P4Check ? "" : string.Format("BK PLC : {0}, PC : {1}, ", PC_DestBank_FORK1, (short)cmd.TargetBank); //목적지 설정
                    sTemp += P5Check ? "" : string.Format("Ba PLC : {0}, PC : {1}, ", PC_DestBay_FORK1, (short)cmd.TargetBay);
                    sTemp += P6Check ? "" : string.Format("Lv PLC : {0}, PC : {1}, ", PC_DestLevel_FORK1, (short)cmd.TargetLevel);
                    sTemp += P7Check ? "" : string.Format("Pl PLC : {0}, PC : {1}, ", PC_DestWorkPlace_FORK1, cmd.TargetItem.iWorkPlaceNumber);
                    LogManager.WriteConsoleLog(eLogLevel.Info, "{0} Data Mis match", sTemp);
                    if (CommandWriteTry >= CommandWriteTryLimit) //재시도 회수가 리미트 도달하면 실패처리 (통신이상으로 간주)
                    {
                        

                        GlobalData.Current.Alarm_Manager.AlarmOccurbyName("CRANE_PLC_INTERFACE_ERROR", ModuleName);
                        return false;
                    }
                }
            }

            //241029 HoN CraneCommand 전송시 인터락 추가
            //PC_CommandWriteComplete 직전에 Alarm Check 해준다.      //Alarm이 있으면 PC_CommandWriteComplete 줄 수 없다.
            if (RMAlarmCheck() > 0) //알람 발생
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, $"{ModuleName} Command Check OK, But has Alarm.");
                return false;
            }

            PC_CommandWriteComplete = 1;

            Stopwatch timeWatch = Stopwatch.StartNew();
            while (!IsTimeout_SW(timeWatch, CommandTimeOut))
            {
                //241029 HoN CraneCommand 전송시 인터락 추가
                //PLC_CommandAck 대기중에도 Alarm Check 해준다.      //Alarm이 있으면 PC_CommandWriteComplete 0 변경 후 나간다.
                if (RMAlarmCheck() > 0) //알람 발생
                {
                    PC_CommandWriteComplete = 0;
                    LogManager.WriteConsoleLog(eLogLevel.Info, $"{ModuleName} Wait PLC Command Ack, But has Alarm. PC CommandWriteComplete Clear");
                    return false;
                }

                if (PLC_CommandAck == 1)
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "Crane :{0} Command Ack On -- Crane  Command :{1} CType: {2} Bank:{3} Bay:{4} Level:{5} WorkPlace :{6} PalletSize :{7}",
                            ModuleName, cmd.CommandID, cmd.Command, cmd.TargetBank, cmd.TargetBay, cmd.TargetLevel, cmd.TargetItem.iWorkPlaceNumber, PC_PalletSize);

                    //Ack체크하고 PLC 가 쓴 작업 도착정보를 다시 체크할지 검토필요  
                    //bool bBankMatch = PLC_DestBank_FORK1 == (short)cmd.TargetBank; //목적지 설정
                    //bool bBayMatch = PLC_DestBay_FORK1 == (short)cmd.TargetBay;
                    //bool bLevelMatch = PLC_DestLevel_FORK1 == (short)cmd.TargetLevel;
                    //bool bWorkPlaceMatch = PLC_DestWorkPlace_FORK1 == cmd.TargetItem.iWorkPlaceNumber;

                    //일단 매칭 체크만 한다. Abnormal 사양검토
                    //목표 타겟이 포트면 해당 포트에 Crane Busy On
                    

                    //PC_CommandWriteComplete = 0; //230531 RGJ 삭제 Command Write Off 는 작업 끝나면 한다.
                    return true;
                }
                Thread.Sleep(50);
            }
            LogManager.WriteConsoleLog(eLogLevel.Info, "Crane :{0} Command TimeOut!  -- Crane  Command :{1} CType: {2} Bank:{3} Bay:{4} Level:{5} WorkPlace :{6} PalletSize :{7}",
                         ModuleName, cmd.CommandID, cmd.Command, cmd.TargetBank, cmd.TargetBay, cmd.TargetLevel, cmd.TargetItem.iWorkPlaceNumber, PC_PalletSize);

            PC_CommandWriteComplete = 0;        //알람 발생 전 command write off
            GlobalData.Current.Alarm_Manager.AlarmOccurbyName("CRANE_PLC_INTERFACE_ERROR", ModuleName); 

            return false;
        }

        public override void CloseController()
        {
            try
            {
                //this.Fsave();
                //this.mpmac.Disconnect();
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Fatal, ex.ToString());
            }
        }


        public void ParameterSet(PMacDataList parameter)
        {
            try
            {
                #region 파라메터에 대한 reading 변수 설정

                RMParameter.exist_axis = Convert.ToInt32(parameter.Where(r => r.TagName == "exist_axis").FirstOrDefault().Note);

                //RMParameter.RearXcount = Convert.ToInt32(parameter.Where(r => r.TagName == "RearXcount").FirstOrDefault().Note);
                //RMParameter.RearYcount = Convert.ToInt32(parameter.Where(r => r.TagName == "RearYcount").FirstOrDefault().Note);
                //RMParameter.RearTotal = Convert.ToInt32(parameter.Where(r => r.TagName == "RearTotal").FirstOrDefault().Note);

                //RMParameter.FrontXcount = Convert.ToInt32(parameter.Where(r => r.TagName == "FrontXcount").FirstOrDefault().Note);
                //RMParameter.FrontYcount = Convert.ToInt32(parameter.Where(r => r.TagName == "FrontYcount").FirstOrDefault().Note);
                //RMParameter.FrontTotal = Convert.ToInt32(parameter.Where(r => r.TagName == "FrontTotal").FirstOrDefault().Note);

                //RMParameter.AutoXcount = Convert.ToInt32(parameter.Where(r => r.TagName == "AutoXcount").FirstOrDefault().Note);
                //RMParameter.AutoYcount = Convert.ToInt32(parameter.Where(r => r.TagName == "AutoYcount").FirstOrDefault().Note);
                //RMParameter.AutoTotal = Convert.ToInt32(parameter.Where(r => r.TagName == "AutoTotal").FirstOrDefault().Note);

                //RMParameter.ManualXcount = Convert.ToInt32(parameter.Where(r => r.TagName == "ManualXcount").FirstOrDefault().Note);
                //RMParameter.ManualYcount = Convert.ToInt32(parameter.Where(r => r.TagName == "ManualYcount").FirstOrDefault().Note);
                //RMParameter.ManualTotal = Convert.ToInt32(parameter.Where(r => r.TagName == "ManualTotal").FirstOrDefault().Note);

                RMParameter.HomePosAxis1 = Convert.ToDecimal(parameter.Where(r => r.TagName == "HomePosAxis1").FirstOrDefault().Note);
                RMParameter.HomePosAxis2 = Convert.ToDecimal(parameter.Where(r => r.TagName == "HomePosAxis2").FirstOrDefault().Note);
                RMParameter.HomePosAxis3 = Convert.ToDecimal(parameter.Where(r => r.TagName == "HomePosAxis3").FirstOrDefault().Note);
                RMParameter.HomePosAxis4 = Convert.ToDecimal(parameter.Where(r => r.TagName == "HomePosAxis4").FirstOrDefault().Note);
                RMParameter.HomePosAxis5 = Convert.ToDecimal(parameter.Where(r => r.TagName == "HomePosAxis5").FirstOrDefault().Note); //20200106 rhj axis5추가로 인한 추가

                RMParameter.Appreach_Lower = Convert.ToDecimal(parameter.Where(r => r.TagName == "Appreach_Lower").FirstOrDefault().Note);
                RMParameter.Appreach_Upper = Convert.ToDecimal(parameter.Where(r => r.TagName == "Appreach_Upper").FirstOrDefault().Note);
                RMParameter.ArmActionCompeteRange = Convert.ToDecimal(parameter.Where(r => r.TagName == "ArmActionCompeteRange").FirstOrDefault().Note);

                RMParameter.ForkAxis_Allowablerange = Convert.ToDecimal(parameter.Where(r => r.TagName == "ForkAxis_Allowablerange").FirstOrDefault().Note);
                RMParameter.XAxis_Allowablerange = Convert.ToDecimal(parameter.Where(r => r.TagName == "XAxis_Allowablerange").FirstOrDefault().Note);
                RMParameter.ZAxis_Allowablerange = Convert.ToDecimal(parameter.Where(r => r.TagName == "ZAxis_Allowablerange").FirstOrDefault().Note);
                RMParameter.TurnAxis_Allowablerange = Convert.ToDecimal(parameter.Where(r => r.TagName == "TurnAxis_Allowablerange").FirstOrDefault().Note);

                RMParameter.MoveTimeout = Convert.ToInt32(parameter.Where(r => r.TagName == "MoveTimeout").FirstOrDefault().Note);
                RMParameter.InitTimeout = Convert.ToInt32(parameter.Where(r => r.TagName == "InitTimeout").FirstOrDefault().Note);

                RMParameter.IPAddress = parameter.Where(r => r.TagName == "IPADDRESS").FirstOrDefault().Note.ToString();

                //RMParameter.FrontZone1Xcount = parameter.Where(r => r.TagName == "FrontZone1Xcount").FirstOrDefault().Note.ToString();
                //RMParameter.FrontZone2Xcount = parameter.Where(r => r.TagName == "FrontZone2Xcount").FirstOrDefault().Note.ToString();
                //RMParameter.FrontZone3Xcount = parameter.Where(r => r.TagName == "FrontZone3Xcount").FirstOrDefault().Note.ToString();
                //RMParameter.FrontZone4Xcount = parameter.Where(r => r.TagName == "FrontZone4Xcount").FirstOrDefault().Note.ToString();
                //RMParameter.FrontZone5Xcount = parameter.Where(r => r.TagName == "FrontZone5Xcount").FirstOrDefault().Note.ToString();
                //RMParameter.FrontZone6Xcount = parameter.Where(r => r.TagName == "FrontZone6Xcount").FirstOrDefault().Note.ToString();

                //RMParameter.FrontZone1Ycount = parameter.Where(r => r.TagName == "FrontZone1Ycount").FirstOrDefault().Note.ToString();
                //RMParameter.FrontZone2Ycount = parameter.Where(r => r.TagName == "FrontZone2Ycount").FirstOrDefault().Note.ToString();
                //RMParameter.FrontZone3Ycount = parameter.Where(r => r.TagName == "FrontZone3Ycount").FirstOrDefault().Note.ToString();
                //RMParameter.FrontZone4Ycount = parameter.Where(r => r.TagName == "FrontZone4Ycount").FirstOrDefault().Note.ToString();
                //RMParameter.FrontZone5Ycount = parameter.Where(r => r.TagName == "FrontZone5Ycount").FirstOrDefault().Note.ToString();
                //RMParameter.FrontZone6Ycount = parameter.Where(r => r.TagName == "FrontZone6Ycount").FirstOrDefault().Note.ToString();

                //RMParameter.RearZone1Xcount = parameter.Where(r => r.TagName == "RearZone1Xcount").FirstOrDefault().Note.ToString();
                //RMParameter.RearZone2Xcount = parameter.Where(r => r.TagName == "RearZone2Xcount").FirstOrDefault().Note.ToString();
                //RMParameter.RearZone3Xcount = parameter.Where(r => r.TagName == "RearZone3Xcount").FirstOrDefault().Note.ToString();
                //RMParameter.RearZone4Xcount = parameter.Where(r => r.TagName == "RearZone4Xcount").FirstOrDefault().Note.ToString();
                //RMParameter.RearZone5Xcount = parameter.Where(r => r.TagName == "RearZone5Xcount").FirstOrDefault().Note.ToString();
                //RMParameter.RearZone6Xcount = parameter.Where(r => r.TagName == "RearZone6Xcount").FirstOrDefault().Note.ToString();

                //RMParameter.RearZone1Ycount = parameter.Where(r => r.TagName == "RearZone1Ycount").FirstOrDefault().Note.ToString();
                //RMParameter.RearZone2Ycount = parameter.Where(r => r.TagName == "RearZone2Ycount").FirstOrDefault().Note.ToString();
                //RMParameter.RearZone3Ycount = parameter.Where(r => r.TagName == "RearZone3Ycount").FirstOrDefault().Note.ToString();
                //RMParameter.RearZone4Ycount = parameter.Where(r => r.TagName == "RearZone4Ycount").FirstOrDefault().Note.ToString();
                //RMParameter.RearZone5Ycount = parameter.Where(r => r.TagName == "RearZone5Ycount").FirstOrDefault().Note.ToString();
                //RMParameter.RearZone6Ycount = parameter.Where(r => r.TagName == "RearZone6Ycount").FirstOrDefault().Note.ToString();

                //RMParameter.MaxZoneButtonX = Convert.ToInt32(parameter.Where(r => r.TagName == "MaxZoneButtonX").FirstOrDefault().Note);
                //RMParameter.MaxZoneButtonY = Convert.ToInt32(parameter.Where(r => r.TagName == "MaxZoneButtonY").FirstOrDefault().Note);

                RMParameter.MaxDatacount = Convert.ToInt32(parameter.Where(r => r.TagName == "MaxDatacount").FirstOrDefault().Note);

                RMParameter.ZAxisSoftLimitP = Convert.ToInt32(parameter.Where(r => r.TagName == "ZAxisSoftLimitP").FirstOrDefault().Note);
                RMParameter.TurnAxisSoftLimitP = Convert.ToInt32(parameter.Where(r => r.TagName == "TurnAxisSoftLimitP").FirstOrDefault().Note);
                RMParameter.ForkAxisSoftLimitP = Convert.ToInt32(parameter.Where(r => r.TagName == "ForkAxisSoftLimitP").FirstOrDefault().Note);
                RMParameter.XAxisSoftLimitP = Convert.ToInt32(parameter.Where(r => r.TagName == "XAxisSoftLimitP").FirstOrDefault().Note);

                RMParameter.ZAxisSoftLimitM = Convert.ToInt32(parameter.Where(r => r.TagName == "ZAxisSoftLimitM").FirstOrDefault().Note);
                RMParameter.TurnAxisSoftLimitM = Convert.ToInt32(parameter.Where(r => r.TagName == "TurnAxisSoftLimitM").FirstOrDefault().Note);
                RMParameter.ForkAxisSoftLimitM = Convert.ToInt32(parameter.Where(r => r.TagName == "ForkAxisSoftLimitM").FirstOrDefault().Note);
                RMParameter.XAxisSoftLimitM = Convert.ToInt32(parameter.Where(r => r.TagName == "XAxisSoftLimitM").FirstOrDefault().Note);

                RMParameter.X_MOVE_SPEED_NO_CARRIER = Convert.ToInt32(parameter.Where(r => r.TagName == "X_MOVE_SPEED_NO_CARRIER").FirstOrDefault().Note);
                RMParameter.X_MOVE_ACCEL_NO_CARRIER = Convert.ToInt32(parameter.Where(r => r.TagName == "X_MOVE_ACCEL_NO_CARRIER").FirstOrDefault().Note);
                RMParameter.X_MOVE_DECEL_NO_CARRIER = Convert.ToInt32(parameter.Where(r => r.TagName == "X_MOVE_DECEL_NO_CARRIER").FirstOrDefault().Note);
                RMParameter.X_MOVE_SPEED = Convert.ToInt32(parameter.Where(r => r.TagName == "X_MOVE_SPEED").FirstOrDefault().Note);
                RMParameter.X_MOVE_ACCEL = Convert.ToInt32(parameter.Where(r => r.TagName == "X_MOVE_ACCEL").FirstOrDefault().Note);
                RMParameter.X_MOVE_DECEL = Convert.ToInt32(parameter.Where(r => r.TagName == "X_MOVE_DECEL").FirstOrDefault().Note);
                RMParameter.DRIVE_SPEED = Convert.ToInt32(parameter.Where(r => r.TagName == "DRIVE_SPEED").FirstOrDefault().Note);
                RMParameter.DRIVE_ACCEL = Convert.ToInt32(parameter.Where(r => r.TagName == "DRIVE_ACCEL").FirstOrDefault().Note);
                RMParameter.DRIVE_DECEL = Convert.ToInt32(parameter.Where(r => r.TagName == "DRIVE_DECEL").FirstOrDefault().Note);
                RMParameter.Z_LOW_SPEED = Convert.ToInt32(parameter.Where(r => r.TagName == "Z_LOW_SPEED").FirstOrDefault().Note);
                RMParameter.Z_LOW_ACCEL = Convert.ToInt32(parameter.Where(r => r.TagName == "Z_LOW_ACCEL").FirstOrDefault().Note);
                RMParameter.Z_LOW_DECEL = Convert.ToInt32(parameter.Where(r => r.TagName == "Z_LOW_DECEL").FirstOrDefault().Note);
                RMParameter.TURN_SPEED = Convert.ToInt32(parameter.Where(r => r.TagName == "TURN_SPEED").FirstOrDefault().Note);
                RMParameter.TURN_ACCEL = Convert.ToInt32(parameter.Where(r => r.TagName == "TURN_ACCEL").FirstOrDefault().Note);
                RMParameter.TURN_DECEL = Convert.ToInt32(parameter.Where(r => r.TagName == "TURN_DECEL").FirstOrDefault().Note);

                RMParameter.pVACTORSPEED = Convert.ToInt32(parameter.Where(r => r.TagName == "pVACTORSPEED").FirstOrDefault().Note);
                RMParameter.pVACTTA = Convert.ToInt32(parameter.Where(r => r.TagName == "pVACTTA").FirstOrDefault().Note);
                RMParameter.pVACTTS = Convert.ToInt32(parameter.Where(r => r.TagName == "pVACTTS").FirstOrDefault().Note);
                RMParameter.pVACTTD = Convert.ToInt32(parameter.Where(r => r.TagName == "pVACTTD").FirstOrDefault().Note);

                RMParameter.pClamp_SPEED = Convert.ToInt32(parameter.Where(r => r.TagName == "pClamp_SPEED").FirstOrDefault().Note);
                RMParameter.pClamp_ACCEL = Convert.ToInt32(parameter.Where(r => r.TagName == "pClamp_ACCEL").FirstOrDefault().Note);
                RMParameter.pClamp_DECEL = Convert.ToInt32(parameter.Where(r => r.TagName == "pClamp_DECEL").FirstOrDefault().Note);

                RMParameter.mMachineType = Convert.ToInt32(parameter.Where(r => r.TagName == "MachineType").FirstOrDefault().Note);
                RMParameter.pGetPutSensorCheck = Convert.ToInt32(parameter.Where(r => r.TagName == "GetPutSensorCheck").FirstOrDefault().Note);

                RMParameter.pStandardTeaching = Convert.ToInt32(parameter.Where(r => r.TagName == "StandardTeaching").FirstOrDefault().Note);
                RMParameter.TactLogSave = Convert.ToInt32(parameter.Where(r => r.TagName == "TactLogSave").FirstOrDefault().Note);

                RMParameter.InitDefaultSpeed = Convert.ToInt32(parameter.Where(r => r.TagName == "InitDefaultSpeed").FirstOrDefault().Note);

                // 2021.020.19 TrayHeight 인터락 추가
                RMParameter.TrayHeightInterLock = Convert.ToInt32(parameter.Where(r => r.TagName == "TrayHeightInterLock").FirstOrDefault().Note);


                #endregion
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Fatal, ex.ToString());
            }
        }

        //public void EMGCraneStop()
        //{
        //    try
        //    {
        //        PC_EMG_STOP = true;
        //    }
        //    catch (Exception ex)
        //    {
        //        LogManager.WriteConsoleLog(eLogLevel.Fatal, ex.ToString());
        //    }
        //}

        #region 스케쥴 관련 
        public override bool SetCraneCommand(CraneCommand Cmd)
        {
            CraneCommand _CurrnetCommand = CurrentCmd; //Thread Safe 로컬변수
            if (_CurrnetCommand != null)
            {
                //작업중에 명령이 또 내려오면 일단 버린다.
                LogManager.WriteConsoleLog(eLogLevel.Info, "현재 명령: {0} 진행중에 {1} 명령이 내려왔으므로 버림", _CurrnetCommand.Command, Cmd.Command);
                return false;
            }
            if (Cmd != null)
            {
                CurrentCmd = Cmd;
                GData.CraneActiveJobList.Add(Cmd);
                ActionPostProcess(Cmd);
            }
            return true;
        }
        public override bool RemoveCraneCommand()
        {
            CurrentCmd = null;
            return true;
        }


        private void CraneStopAction()
        {
            //220705 exception 발생하여 index를 찾고 지운다.
            int iindex = GData.CraneActiveJobList.IndexOf(CurrentCmd);
            if (iindex >= 0)
            {
                //210204 lsj 주석
                //Thread.Sleep(CommandDelay);
                //this.EMERGENCYS = "1";
                //Thread.Sleep(CommandDelay);
                GData.CraneActiveJobList.Remove(CurrentCmd);
                ActionPostProcess(CurrentCmd);
                RemoveCraneCommand();
                //GData.WCF_mgr.ReportRobotStatus(ModuleName);
            }
        }

        private void CraneStartAction()
        {
            //220705 exception 발생하여 index를 찾고 지운다.
            int iindex = GData.CraneActiveJobList.IndexOf(CurrentCmd);
            if (iindex >= 0)
            {
                //210204 lsj 주석
                //Thread.Sleep(CommandDelay);
                //this.EMERGENCYS = "0";
                //Thread.Sleep(CommandDelay);
                GData.CraneActiveJobList.Remove(CurrentCmd);
                ActionPostProcess(CurrentCmd);
                RemoveCraneCommand();
                //GData.WCF_mgr.ReportRobotStatus(ModuleName);
            }
        }

        private void CraneAutoTeachingStartAction()
        {
            //220705 exception 발생하여 index를 찾고 지운다.
            int iindex = GData.CraneActiveJobList.IndexOf(CurrentCmd);
            if (iindex >= 0)
            {
                //210204 lsj 주석
                //Thread.Sleep(CommandDelay);
                //this.AUTOTEACHING = "1";
                //GData.WCF_mgr.ReportRobotStatus(ModuleName);
                GData.CraneActiveJobList.Remove(CurrentCmd);
                ActionPostProcess(CurrentCmd);
                RemoveCraneCommand();
            }
        }

        private void CraneAutoTeachingStopAction()
        {
            //220705 exception 발생하여 index를 찾고 지운다.
            int iindex = GData.CraneActiveJobList.IndexOf(CurrentCmd);
            if (iindex >= 0)
            {
                GData.CraneActiveJobList.Remove(CurrentCmd);
                ActionPostProcess(CurrentCmd);
                RemoveCraneCommand();
            }
        }

        private void CraneResetPostAction()
        {
            //220705 exception 발생하여 index를 찾고 지운다.
            int iindex = GData.CraneActiveJobList.IndexOf(CurrentCmd);
            if (iindex >= 0)
            {
                GData.CraneActiveJobList.Remove(CurrentCmd);
                ActionPostProcess(CurrentCmd);
                RemoveCraneCommand();
            }
        }

        private void CraneHomePostAction()
        {
            //220705 exception 발생하여 index를 찾고 지운다.
            int iindex = GData.CraneActiveJobList.IndexOf(CurrentCmd);
            if (iindex >= 0)
            {
                GData.CraneActiveJobList.Remove(CurrentCmd);
                ActionPostProcess(CurrentCmd);
                RemoveCraneCommand();
            }

        }
        private void CraneGetPostAction()
        {
            //220705 exception 발생하여 index를 찾고 지운다.
            int iindex = GData.CraneActiveJobList.IndexOf(CurrentCmd);
            if (iindex >= 0)
            {
                GData.CraneActiveJobList.Remove(CurrentCmd);
                ActionPostProcess(CurrentCmd);
                RemoveCraneCommand();
            }
        }

        private void CranePutPostAction()
        {
            //220705 exception 발생하여 index를 찾고 지운다.
            int iindex = GData.CraneActiveJobList.IndexOf(CurrentCmd);
            if (iindex >= 0)
            {
                GData.CraneActiveJobList.Remove(CurrentCmd);
                ActionPostProcess(CurrentCmd);
                RemoveCraneCommand();
                //GData.WCF_mgr.ReportRobotStatus(ModuleName);
            }
        }

        private void CraneMovePostAction()
        {
            //220705 exception 발생하여 index를 찾고 지운다.
            int iindex = GData.CraneActiveJobList.IndexOf(CurrentCmd);
            if (iindex >= 0)
            {
                GData.CraneActiveJobList.Remove(CurrentCmd);
                ActionPostProcess(CurrentCmd);
                RemoveCraneCommand();
            }
            //GData.WCF_mgr.ReportRobotStatus(ModuleName);
        }
        private void CraneChuckPostAction()
        {
            //220705 exception 발생하여 index를 찾고 지운다.
            int iindex = GData.CraneActiveJobList.IndexOf(CurrentCmd);
            if (iindex >= 0)
            {
                //210204 lsj 주석
                //Thread.Sleep(CommandDelay);
                //this.CHUCK = "1";

                //Thread.Sleep(CommandDelay);

                GData.CraneActiveJobList.Remove(CurrentCmd);
                ActionPostProcess(CurrentCmd);

                RemoveCraneCommand();
                //GData.WCF_mgr.ReportRobotStatus(ModuleName);
            }
        }
        private void CraneUnChuckPostAction()
        {
            //220705 exception 발생하여 index를 찾고 지운다.
            int iindex = GData.CraneActiveJobList.IndexOf(CurrentCmd);
            if (iindex >= 0)
            {
                //210204 lsj 주석
                //Thread.Sleep(CommandDelay);
                //this.CHUCK = "2";

                //Thread.Sleep(CommandDelay);
                GData.CraneActiveJobList.Remove(CurrentCmd);
                ActionPostProcess(CurrentCmd);

                RemoveCraneCommand();
                //GData.WCF_mgr.ReportRobotStatus(ModuleName);
            }
        }


        #endregion

        public override bool CheckRM_MC_On()
        {
            return RMMotorMCPower;
        }
        public override bool CheckRMBoxOpen() //On 정상
        {
            return RMPanelDoor;
        }

        private bool SimulFireFlag = false;
        private bool SimulFireExtinguish = false;
        public override bool RMFireExtinguishWork()
        {
            if (SimulMode)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "{0} 방화작업을 시작합니다. ", this.ModuleName);
                Thread.Sleep(1000);
                LogManager.WriteConsoleLog(eLogLevel.Info, "{0} 방화 스크린 셔터를 닫습니다. ", this.ModuleName);
                Thread.Sleep(1000);
                LogManager.WriteConsoleLog(eLogLevel.Info, "{0} 소화기를 분사 합니다. ", this.ModuleName);
                Thread.Sleep(1000);
                LogManager.WriteConsoleLog(eLogLevel.Info, "{0} 방화작업을 완료합니다. ", this.ModuleName);
                SimulFireExtinguish = true;
            }
            GData.CraneActiveJobList.Remove(CurrentCmd);

            RemoveCraneCommand();

            return true;
        }
        public override bool CheckRMExtinguished()
        {
            if (SimulMode)
            {
                if (SimulFireExtinguish)
                {
                    SimulFireExtinguish = false;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            //사양 추가 필요
            return true;
        }

        public override bool CheckRMAutoMode()
        {
            bool OnlineMode = PLC_OnlineMode == eCraneOnlineMode.REMOTE; //지상반 동작 모드
            bool SCMode = PLC_SCMODE == eCraneSCMode.AUTO_RUN; //기상반 동작 모드
            return OnlineMode && SCMode;
        }

        #region 시뮬레이션 동작


        //private int SimulActionDelay = 300; // 동작별 시뮬 딜레이 추가.
        //private bool SetSimulCommand(CraneCommand cmd)
        //{
        //    if (SimulCMD == null)
        //    {
        //        SimulJobDone = false;
        //        SimulCMD = cmd;
        //        return true;
        //    }
        //    else
        //    {
        //        return false;
        //    }
        //}
        //CraneCommand SimulCMD;
        //bool SimulJobDone = false;
        //private void SimulMotionRun()
        //{
        //    while (true)
        //    {
        //        if (SimulCMD == null)
        //        {
        //            Thread.Sleep(50);
        //            continue;
        //        }
        //        switch (SimulCMD.Command)
        //        {
        //            case eCraneCommand.PICKUP:
        //                //SimulMoveAction();
        //                SimulJobDone = SimulGetAction();
        //                SimulCMD = null;
        //                break;
        //            case eCraneCommand.UNLOAD:
        //                //SimulMoveAction();
        //                SimulJobDone = SimulPutAction();
        //                SimulCMD = null;
        //                break;
        //            case eCraneCommand.MOVE:
        //                SimulMoveAction();
        //                SimulJobDone = true;
        //                SimulCMD = null;
        //                break;
        //            case eCraneCommand.LOCAL_HOME:
        //                SimulJobDone = true;
        //                SimulCMD = null;
        //                break;
        //        }
        //        Thread.Sleep(50);
        //    }
        //}
        //private bool SimulGetAction()
        //{
        //    if (SimulFireFlag)
        //    {
        //        CraneState = eCraneUIState.FIRE_UNLOADING;
        //    }
        //    else
        //    {
        //        CraneState = eCraneUIState.GETTING;
        //    }
        //    if (SimulEmptyRetriveTestMode)
        //    {
        //        LogManager.WriteConsoleLog(eLogLevel.Info, "Crane : {0} 공출고 테스트 모드이므로 강제로 알람 발생합니다.", ModuleName);
        //        GlobalData.Current.Alarm_Manager.AlarmOccur("41", ModuleName);
        //        SimulEmptyRetriveON = true;
        //        //CraneState = eCraneUIState.ERROR;
        //        return false;
        //    }
        //    //UnChuck
        //    this.Robot_CHUCK = "0";
        //    Thread.Sleep(SimulActionDelay);

        //    //Fork Extand

        //    this.Robot_ARMSTRETCH = "1";
        //    if (CurrentBank == 1)
        //    {
        //        ForkAxisPosition = 1000;
        //        this.CraneArmState = eCraneArmState.ExtendFront;
        //    }
        //    else
        //    {
        //        ForkAxisPosition = -1000;
        //        this.CraneArmState = eCraneArmState.ExtendRear;
        //    }

        //    Thread.Sleep(SimulActionDelay);

        //    //Fork UP
        //    //Fork Retract
        //    ForkAxisPosition = 0;
        //    CraneArmState = eCraneArmState.Center;
        //    this.Robot_ARMSTRETCH = "0";
        //    Thread.Sleep(SimulActionDelay);

        //    //Chuck
        //    this.Robot_CHUCK = "1";
        //    Thread.Sleep(SimulActionDelay);
        //    CraneState = eCraneUIState.ONLINE;
        //    return true;
        //}
        //private bool SimulPutAction()
        //{
        //    if (SimulFireFlag)
        //    {
        //        CraneState = eCraneUIState.FIRE_UNLOADING;
        //    }
        //    else
        //    {
        //        CraneState = eCraneUIState.PUTTING;
        //    }
        //    if (SimulDoubleStorageTestMode)
        //    {
        //        LogManager.WriteConsoleLog(eLogLevel.Info, "Crane : {0} 더블 스토리지 테스트 모드이므로 강제로 알람 발생합니다.", ModuleName);
        //        GlobalData.Current.Alarm_Manager.AlarmOccur("40", ModuleName);
        //        SimulDoubleStorageON = true;
        //        //CraneState = eCraneUIState.ERROR;
        //        return false;
        //    }
        //    else
        //    {
        //        SimulDoubleStorageON = false;
        //    }
        //    //UnChuck
        //    this.Robot_CHUCK = "0";
        //    Thread.Sleep(SimulActionDelay);

        //    //Fork Extand

        //    this.Robot_ARMSTRETCH = "1";
        //    if (CurrentBank == 1)
        //    {
        //        ForkAxisPosition = 1000;
        //        this.CraneArmState = eCraneArmState.ExtendFront;
        //    }
        //    else
        //    {
        //        ForkAxisPosition = -1000;
        //        this.CraneArmState = eCraneArmState.ExtendRear;
        //    }
        //    Thread.Sleep(SimulActionDelay);

        //    //Fork Down
        //    //Fork Retract
        //    ForkAxisPosition = 0;
        //    CraneArmState = eCraneArmState.Center;
        //    this.Robot_ARMSTRETCH = "0";
        //    Thread.Sleep(SimulActionDelay);
        //    CraneState = eCraneUIState.ONLINE;
        //    return true;
        //}
        //private bool SimulMoveAction()
        //{
        //    if (SimulFireFlag)
        //    {
        //        CraneState = eCraneUIState.FIRE_UNLOADING;
        //    }
        //    else
        //    {
        //        CraneState = eCraneUIState.MOVING;
        //    }
        //    RMModuleBase RM_Another = null;
        //    if (GlobalData.Current.mRMManager.FirstRM.ModuleName == ModuleName)
        //    {
        //        RM_Another = GlobalData.Current.mRMManager.SecondRM;
        //    }
        //    else
        //    {
        //        RM_Another = GlobalData.Current.mRMManager.FirstRM;
        //    }

        //    while (true)
        //    {
        //        lock (GlobalData.Current.mRMManager.SimulLock)
        //        {
        //            int sBay;
        //            int sLevel;
        //            int.TryParse(Robot_BAY, out sBay);
        //            int.TryParse(Robot_LEVEL, out sLevel);

        //            if (SimulCMD.TargetBank.ToString() == Robot_BANK &&
        //               SimulCMD.TargetBay.ToString() == Robot_BAY &&
        //               SimulCMD.TargetLevel.ToString() == Robot_LEVEL)
        //            {
        //                CraneState = eCraneUIState.ONLINE;
        //                return true;
        //            }
        //            Robot_BANK = SimulCMD.TargetBank.ToString();

        //            if (SimulCMD.TargetLevel != sLevel)
        //            {
        //                if (SimulCMD.TargetLevel > sLevel)
        //                {
        //                    sLevel++;
        //                    Robot_LEVEL = sLevel.ToString();
        //                }
        //                else
        //                {
        //                    sLevel--;
        //                    Robot_LEVEL = sLevel.ToString();
        //                }

        //            }

        //            if (SimulCMD.TargetBay != sBay)
        //            {
        //                if (SimulCMD.TargetBay > sBay) //+ 정방향
        //                {
        //                    if (RM_Another == null || this.ModuleName == GlobalData.Current.mRMManager.SecondRM.ModuleName) //2번째 RM 은 +이동 항상 가능
        //                    {
        //                        sBay++;
        //                        Robot_BAY = sBay.ToString();
        //                    }
        //                    else
        //                    {
        //                        if (RM_Another.CurrentBay - 5 > sBay)//일정 거리 여유가 있어야 이동 가능 
        //                        {
        //                            sBay++;
        //                            Robot_BAY = sBay.ToString();
        //                        }
        //                        else
        //                        {
        //                            //이동불가 대기
        //                        }
        //                    }
        //                }
        //                else   //- 역방향
        //                {
        //                    if (RM_Another == null || this.ModuleName == GlobalData.Current.mRMManager.FirstRM.ModuleName) //1번째 RM 은 -이동 항상 가능
        //                    {
        //                        sBay--;
        //                        Robot_BAY = sBay.ToString();
        //                    }
        //                    else
        //                    {
        //                        if (RM_Another.CurrentBay + 5 < sBay)//일정 거리 여유가 있어야 이동 가능 
        //                        {
        //                            sBay--;
        //                            Robot_BAY = sBay.ToString();
        //                        }
        //                        else
        //                        {
        //                            //이동불가 대기
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //        Thread.Sleep(SimulActionDelay);
        //    }
        //}




        #endregion


        /// <summary>
        /// 랙마스터에 화재 플래그 상태를 가져온다.
        /// </summary>
        /// <returns></returns>
        public override bool GetRMFireStateFlag()
        {
            if (SimulMode)
            {
                return SimulFireFlag;
            }
            return false; //PLC 에서 받는부분 구현 예정.
        }
        /// <summary>
        /// 화재상황임을 랙마스터에  통지 한다.
        /// </summary>
        /// <param name="FireOn"></param>
        /// <returns></returns>
        public override bool NotifyFireCommand(bool FireOn)
        {
            LogManager.WriteConsoleLog(eLogLevel.Info, "Crane :{0} NotifyFireCommand FireState : {1}", ModuleName, FireOn);
            if (SimulMode)
            {
                SimulFireFlag = FireOn;
                return true;
            }
            else
            {
                if (FireOn)
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "Fire Bit On");
                    PC_FireOccurred = true; //PLC 에 Fire Bit On
                }
                else
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "Fire Bit Off");
                    PC_FireOccurred = false; //PLC 에 Fire Bit OFF
                    PC_FireReset = true; //PLC 에 Fire Reset 요청

                    Stopwatch timeWatch = Stopwatch.StartNew();
                    while (!IsTimeout_SW(timeWatch, FireNotifyTimeOut))
                    {
                        if (PLC_FireCommand == false)
                        {
                            PC_FireReset = false;
                            return true;
                        }
                        Thread.Sleep(50);
                    }
                    return false;
                }

            }
            return true;
        }

        public override bool RMEMG_STOP_Request()
        {
            PC_EMG_STOP = true;
            CraneState = eCraneUIState.ERROR;
            //1초후에 끄도록 비동기 태스크 추가.
            var EMOTask = Task.Factory.StartNew(() =>
            {
                Thread.Sleep(1000); //1초 유지
                PC_EMG_STOP = false;
            });
            return true;
        }
        public override bool RMPause_Request()
        {
            if (!SimulMode)
            {
                PC_PauseCommand = true;
                //포즈 응답 비트가 따로 없으므로 스킵
            }

            return true;
        }
        public override bool RMResume_Request()
        {
            if (!SimulMode)
            {
                PC_PauseCommand = false;
                //포즈 응답 비트가 따로 없으므로 스킵
            }
            return true;
        }
        public override bool RMHome_Request()
        {
            if (!CheckRMCommandExist())
            {
                //string CarrierID = PC_CarrierID_FORK1;
                CraneCommand hCmd = new CraneCommand("HOMEREQ", ModuleName, eCraneCommand.LOCAL_HOME, enumCraneTarget.None, this, CarrierID);  //2024.09.09 lim, Crane에 자재 있을때 해당 명령 수행 하면 CarrierID가 삭제 된다.
                SetCraneCommand(hCmd);
            }
            return true;
        }
        /// <summary>
        /// 실제 크레인 내부에서 화재 상태인지 체크
        /// </summary>
        /// <returns></returns>
        public override bool CheckForkInFire()
        {
            if (!SimulMode)
            {
                bool bFire = PLC_FireState;
                return bFire;
            }
            else
            {
                return false;
            }

        }

        public override bool CheckPLCFireCommand()
        {
            if (!SimulMode)
            {
                bool bCmd = PLC_FireCommand;
                return bCmd;
            }
            else
            {
                return false;
            }
        }

        //241030 HoN 화재 관련 추가 수정        //-. PLC로 알려주는 Bit 화재 발생하면 무조건 전 Crane ON 처리. -> OFF시점은 Operator가 수동으로 해야함. 이를 수행하지 않아 발생하는 문제는 오퍼레이터 조작미스로 처리
        public override bool CheckCraneFireOccurred()
        {
            bool bFire = PC_FireOccurred;
            return bFire;
        }

        public override bool CheckEmptyRetriveState()
        {
            bool bPLCEmpty = true;
            bool AlarmCodeMatch = PLC_ErrorCode == short.Parse(GlobalData.SOURCE_EMPTY_ALARM_CODE); //Code 로 체크
            return bPLCEmpty && AlarmCodeMatch;

        }
        public override bool CheckDoubleStorageState()
        {
            bool bPLCDouble = true;
            bool AlarmCodeMatch = PLC_ErrorCode == short.Parse(GlobalData.DOUBLE_STORAGE_ALARM_CODE);//Code 로 체크
            return bPLCDouble && AlarmCodeMatch;
        }
        public override bool CheckPortInterfaceErrorState()
        {
            bool AlarmCodeMatch = PLC_ErrorCode == short.Parse(GlobalData.PORT_IF_ALARM_CODE);//Code 로 체크
            return  AlarmCodeMatch;
        }

        public int CalcCurrentBay(short XPos)
        {
            //추후 계산식 추가.
            int Bay = XPos / 100;
            if (Bay == 0)
            {
                Bay = 1;
            }
            return Bay;
        }
        public int CalcCurrentLevel(short ZPos)
        {
            int Level = ZPos / 100;
            if (Level == 0)
            {
                Level = 1;
            }
            return Level;
        }
        public override bool DoAbnormalCheck()
        {
            short AlarmCode = 0;
            if (!SimulMode)
            {
                AlarmCode = SimulAlarmCode;
            }
            else
            {
                AlarmCode = PLC_ErrorCode; //알람코드 획득

            }
            if (AlarmCode > 0)
            {
                if (GlobalData.Current.Alarm_Manager.CheckModuleHeavyAlarmExist(ModuleName) == false) //알람은 유닛당 1개만 발생
                {
                    GlobalData.Current.Alarm_Manager.AlarmOccur(AlarmCode.ToString(), ModuleName);
                }
                return true;
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// PLC 자체 알람 리셋 대응을 위해 상태가 크레인상태가  IDLE 로 변하면 알람 리셋 시퀀스 수행
        /// </summary>
        private void ProcessPLCAlarm()
        {
            if (SimulMode)
            {
                return;
            }
            short ErrorCode = PLC_ErrorCode;
            if (ErrorCode > 0)//에러 코드 있는 상태일때만 처리
            {
                if (CraneSC_State == eCraneSCState.IDLE)
                {
                    //에러코드 올리고 기상반 동작 상태 갱신 안됬을때 처리해버리는 케이스 방지를 위해
                    Thread.Sleep(1000);// Idle 상태 유지체크 딜레이
                }
                if (CraneSC_State == eCraneSCState.ERROR) //에러 상태이면 알람 올리고 보고
                {
                    GlobalData.Current.Alarm_Manager.AlarmOccur(ErrorCode.ToString(), ModuleName);
                }
                if (CraneSC_State == eCraneSCState.IDLE) //에러 상태가 아니라면  -> PLC 자체 클리어를 의미
                {
                    //GlobalData.Current.Alarm_Manager.AlarmClear(ModuleName); //Alarm Clear      //241023 HoN Alarm Clear 시점 변경      //시점 변경 주석
                    GlobalData.Current.MainBooth.SetPLCRMReportComplete(IsFirstRM, true); //PLC 에게 에러 보고 했음을 알려준다.
                    PC_AlarmCode = 0; //PC 에서 PLC 로 보내는 알람 코드 초기화
                    Stopwatch timeWatch = Stopwatch.StartNew();
                    while (true)
                    {
                        if (IsTimeout_SW(timeWatch, PLCTimeOut))
                        {
                            LogManager.WriteConsoleLog(eLogLevel.Info, "Crane : {0} during ProcessPLCAlarmClear PLC Response Timtout.", ModuleName);
                            return;
                        }
                        if (PLC_ErrorCode == 0)
                        {
                            GlobalData.Current.Alarm_Manager.AlarmClear(ModuleName); //Alarm Clear      //241023 HoN Alarm Clear 시점 변경      //시점 변경
                            GlobalData.Current.MainBooth.SetPLCRMReportComplete(IsFirstRM, false); //에러 보고 비트 해제.
                            return;
                        }
                    }
                }
            }
        }

        private void UpdateAxisInfo()
        {
            if (SimulMode)
            {
                return;
            }
            else
            {
                XAxisPosition = PLC_RM_XPosition;
                ZAxisPosition = PLC_RM_ZPosition;
                ForkAxisPosition = PLC_RM_FPosition;
            }
        }
        public bool NeedPlayBackWrite()
        {
            return GlobalData.Current.UsePlayBackLog;
        }

        public override eCraneUtilState GetCraneUtilState()
        {

            //public enum eCraneSCMode
            //{
            //    OFFLINE = 0,
            //    AUTO_RUN = 1,
            //    MANUAL_RUN = 2,
            //    ONE_RACK_PAUSE = 3,
            //}
            //public enum eCraneSCState
            //{
            //    OFFLINE = 0,
            //    IDLE = 1,              //대기 상태
            //    BUSY = 2,              //동작상태
            //    ERROR = 4,             //에러상태
            //}
            eCraneSCMode CMode = PLC_SCMODE;
            eCraneSCState CState = PLC_CraneActionState;
            eCraneUtilState UtilState = eCraneUtilState.NONE;
            switch (CMode)
            {
                case eCraneSCMode.AUTO_RUN:
                    if (CState == eCraneSCState.IDLE)
                    {
                        UtilState = eCraneUtilState.IDLE;
                    }
                    else if (CState == eCraneSCState.BUSY)
                    {
                        UtilState = eCraneUtilState.RUN;
                    }
                    else if (CState == eCraneSCState.ERROR)
                    {
                        UtilState = eCraneUtilState.ERROR;
                    }
                    else 
                    {
                        UtilState = eCraneUtilState.MANUAL;  //OFFLINE State 는 메뉴얼로 본다.
                    }
                    break;
                case eCraneSCMode.OFFLINE: //오프라인 상태이면 메뉴얼로 보고 알람만 체크한다.
                case eCraneSCMode.MANUAL_RUN: //메뉴얼 상태이면 알람만 체크한다.
                    UtilState = CState == eCraneSCState.ERROR ? eCraneUtilState.ERROR : eCraneUtilState.MANUAL;
                    break;
                case eCraneSCMode.ONE_RACK_PAUSE: //원랙 모드 상태면 리무브
                    UtilState = eCraneUtilState.REMOVE;
                    break;
                default:
                    UtilState = eCraneUtilState.NONE;
                    break;
            }
            return UtilState;
        }
    }
}
