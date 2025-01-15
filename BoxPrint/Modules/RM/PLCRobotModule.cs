using System;
using System.Threading;
using System.Linq;
using WCF_LBS;
using WCF_LBS.Commands;
using WCF_LBS.DataParameter;
using Stockerfirmware.PMac;
using System.Text;
using Stockerfirmware.Communication; //201210 lsj 아진제어기 추가
using Stockerfirmware.DataList;
using Stockerfirmware.DataList.CV;
using Stockerfirmware.Log;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.IO;
using System.Xml;
using System.Windows.Threading;
using System.Threading.Tasks;
using System.Windows;//2021년 6월 1일 화요일 오전 9:16:42 - Editted by 수환 : 
using Stockerfirmware.CCLink;//2021년 6월 11일 금요일 오전 9:27:21 - Editted by 수환 : CClink 추가
using System.Collections.Generic;
using Stockerfirmware.Modules.Shelf;

//2021년 5월 18일 화요일 오후 1:21:30 - Editted by 수환 :
//기존 ARC Module 에서 가능한 명칭들을 통일한 상태로 통신 기능을 PLC로 변경 

//2021년 5월 25일 화요일 오전 11:17:27 - Editted by 수환 : 
//기존 ARC Module 과 I/F 부분이 변경되어 기능 추가

namespace Stockerfirmware.Modules.RM
{
    //2021년 5월 25일 화요일 오전 11:17:27 - Editted by 수환 : 사양 추가로 PLC Robot State 를 따로 작성
    class PLCRobotState
    {
        public bool nRmHome = false;        // (1=Home완료, 0=Not Home)
        public bool nRmBusy = false;        //Busy or Idle(1=Busy, 0=Idle)
        public bool nRmAlarm = false;        //Alarm or Normal (1=Alarm, 0=Normal)
        public bool nRmEMG = false;        //Emg or Normal (1=Emg, 0=Normal)
        public bool nRmArmDetect = false;        //Arm Object 센서 (1=유, 0=무)
        public bool nRmArmExtend = false;        //Arm Extend 상태 (1=Extend, 0=Retract)
        public bool nRmJobState = false;        //작업상태(1=작업중, 0=대기중)
        public string Position_Bank = string.Empty; //현재 위치(Bank, 1=1Bank, 2=2Bank)
        public string Position_Bay = string.Empty; //현재 위치(Bay)
        public string Position_Level = string.Empty; //현재 위치(Level)
        public string ErrorCode = string.Empty; //Error Code
        public string nRmClampState = string.Empty; //Clamp 상태(0: None, 1:Clamp, 2:Unclamp)
        public bool nRmAutoTeach = false;        //(1: AutoTeaching 중, 0: None)

        public bool isTilt_LR4 = false;            //LR1(1:자재 좌우 기울어짐1, false=Normal)
        public bool isTilt_LR2 = false;            //LR2(1:자재 좌우 기울어짐2, false=Normal)
        public bool isTilt_FB1 = false;            //LR3(1:자재 좌우 기울어짐3, false=Normal)
        public bool isTilt_LR3 = false;            //LR4(1:자재 좌우 기울어짐4, false=Normal)
        public bool isTilt_LR1 = false;            //FB1(1:자재 전후 기울어짐1, false=Normal)
        public bool isTilt_FB2 = false;            //FB2(1:자재 전후 기울어짐2, false=Normal)
        public string Mileage_Z = string.Empty;     //Z축 주행거리(소모품 추가)
        public string Mileage_T = string.Empty;     //T축 주행거리(소모품 추가)
        public string Mileage_Y = string.Empty;     //Y축 주행거리(소모품 추가)
        public string Mileage_A = string.Empty;     //A축 주행거리(소모품 추가)
    }

    //2021년 5월 28일 금요일 오전 11:28:51 - Editted by 수환 : updataRMBitState 에서 한번에 비트 읽어서 따로 빼놓음
    class PLCIFState
    {
        public eBitState isOnCIMAlive           = eBitState.OFF; //CIM 하트비트
        public eBitState isOnCommand            = eBitState.OFF; //CIM 명령 전송 PIO 비트
        public eBitState isOnPLCAlive           = eBitState.OFF; //PLC 하트비트
        public eBitState isOnAlarm              = eBitState.OFF; //PLC 알람 발생 비트
        public eBitState isOnCommanReply        = eBitState.OFF; //PLC 명령 확인 PIO 비트 

        public eBitState isOnEMS                = eBitState.OFF; //CIM EMS 상태  
        public eBitState isOnDoorOpen		    = eBitState.OFF; //CIM Door Open 
        public eBitState isOnTray_Align_MI12    = eBitState.OFF; //CIM 메뉴얼 포트 기우러짐 센서 
        public eBitState isOnTray_Align_AI21    = eBitState.OFF; //CIM 오토 2번 포트 기우러짐 센서 
        public eBitState isOnTray_Align_AI11    = eBitState.OFF; //CIM 오토 1번 포트 기우러짐 센서 
        public eBitState isOnAlarm_CIM          = eBitState.OFF; //CIM 알람 발생시 로봇 정지 명령 	//2021년 7월 9일 금요일 오후 4:35:20 - Editted by 수환 : 추가
    }


    //RobotModule
    class PLCRobotModule : RMModuleBase
    {
        //2021년 5월 25일 화요일 오후 1:31:27 - Editted by 수환 : 추가 부분들
        public SharedMemoryClass _sharedMemory;
        public ParameterList_PLC _plcParameter = new ParameterList_PLC();
        public string _commandAddress_Start;
        public string _replyAddress_Start;
        public int _commandAddress_Length;
        public string _IFAddress_Start;
        public string _alarmAddress_Start;//2021년 6월 30일 수요일 오후 4:00:11 - Editted by 수환 : 로봇 알람코드 추가
        PLCIFState RobotIF;

        private static Object thisLock = new Object();

        protected ManualResetEvent MR_connectionEvent = new ManualResetEvent(false);

        readonly int CommandDelay = 500;
        readonly int ActionDelay = 1000;
        string TRANSFERRING = "0";
        string BANK = "1";
        string BAY = "1";
        string LEVEL = "1";
        string CARRIERCONTAIN = "0";
        string HOME = "1";
        string EMERGENCYS = "0";
        string BUSY = "0";
        string ARMSTRETCH = "0";
        string ERRORSTAUTS = "0";
        string ERRORCODE = "0";
        string AUTOTEACHING = "0";
        string CHUCK = "0";

        #region Robot Aalrm 
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
        public const string ROBOT_TRAY_FB_TILT_ERROR = "18"; //트레이 앞뒤 틸트
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

        PLCRobotState RobotState;

        public readonly int use = 1;

        private bool _RobotOnlineConncet = false;
        private bool _RobotSharedMemoryConncet = false;

        public override bool RobotOnlineConncet
        {
            get
            {
                return _RobotOnlineConncet;
            }
            set
            {
                _RobotOnlineConncet = value;
            }
        }

        private bool _Place_Sensor_Exist;
        public override bool Place_Sensor_Exist
        {
            get
            {
                return _Place_Sensor_Exist;
            }
            set
            {
                if (this.IOSimulMode)
                {
                    //2020.11.26 WCF 접속 보고시 Hand 센서 확인
                    INI_Helper.WriteValue(RMStateFilePath, "STATE", "Place_Sensor_Exist", _Place_Sensor_Exist ? "1" : "0");
                }

                _Place_Sensor_Exist = value;
            }
        }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="simul"></param>
        /// <param name="RMType"></param>
        /// <param name="ioSimul"></param>
        public PLCRobotModule(string ID, bool simul, eRMType RMType, bool ioSimul)
            : base(ID, simul, RMType, ioSimul)
        {
            try
            {
                _plcParameter._endMARK = " ";//2021년 5월 18일 화요일 오후 1:25:40 - Editted by 수환 : EndMark = CR(16 진수 "D") 

                ePlcNetType netTypeBuffer = (RMType == eRMType.PLC_EHER) ? ePlcNetType.Ether : ePlcNetType.UTL;

                _sharedMemory = new SharedMemoryClass();

                ModuleName = ID;

                RMIp = GlobalData.Current.RMSection.RM1Element.IPAddress;
                RMPort = GlobalData.Current.RMSection.RM1Element.Port;

                // c 티칭값 상태값
                string tmpPath = string.Format("\\Data\\RM\\FrontTeachingData{0}.xml", ModuleName);
                //this.FrontData = ShelfItemList.Deserialize(GData.CurrentFilePaths(GData.FullPath) + tmpPath);

                tmpPath = string.Format("\\Data\\RM\\RearTeachingData{0}.xml", ModuleName);
                //this.RearData = ShelfItemList.Deserialize(GData.CurrentFilePaths(GData.FullPath) + tmpPath);

                // Port 티칭값
                tmpPath = string.Format("\\Data\\RM\\PortTeachingData{0}_ARC.xml", ModuleName);
                this.PortData = ShelfItemList.Deserialize(GData.CurrentFilePaths(GData.FullPath) + tmpPath);

                RMStateFilePath = string.Format(GData.CurrentFilePaths(GData.FullPath) + "\\Data\\RM\\StateData{0}.ini", ModuleName);

                //2021년 5월 25일 화요일 오후 1:33:24 - Editted by 수환 : 추가 부분
                PLCXmlRead();
                this._commandAddress_Start = _plcParameter._dicPLCIFMap_Word["RobotCommand"]._startAddress;
                this._commandAddress_Length = _plcParameter._dicPLCIFMap_Word["RobotCommand"]._length;
                this._replyAddress_Start = _plcParameter._dicPLCIFMap_Word["Pixed1"]._startAddress;
                this._IFAddress_Start = _plcParameter._dicPLCIFMap_Bit[eBitIFMap.CIMAlive.ToString()]._startAddress;
                this._alarmAddress_Start    = _plcParameter._dicPLCIFMap_Word["AlarmCode"]._startAddress; //2021년 6월 30일 수요일 오후 3:51:06 - Editted by 수환 : 로봇 알람 코드 추가

                _plcParameter._PioData.changeState(ePIOState.Idel);

                if (this.SimulMode)
                    Place_Sensor_Exist = SimulPlace_Sensor();

                RMConnecting(RMIp, RMPort);

                RobotState = new PLCRobotState();
                RobotIF = new PLCIFState();

                GData = GlobalData.Current;

                //computeAlwaysCheckPLC();//2021년 5월 28일 금요일 오전 10:59:22 - Editted by 수환 : 상시 체크 Task 생성

                createAlwaysCheckPLC();


                Thread runThread = new Thread(new ThreadStart(Run));
                runThread.Start();
            }
            catch(Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Fatal, "== PLCRobotModule.PLCRobotModule() Exception : {0}", ex.ToString());
            }

        }

        /// <summary>
        /// PLC Robot 에 접속
        /// </summary>
        /// <param name="m_ip"></param>
        /// <param name="m_port"></param>
        /// <returns></returns>
        public override bool RMConnecting(string m_ip, int m_port)
        {
            if (this.RobotOnlineConncet == true)
                return true;

            try
            {
                bool isConnect = _sharedMemory.SharedMemoryConnect();
                string messageBuffer = string.Empty;

                if (isConnect)
                {
                    messageBuffer = "PLC Robot 공유 메모리 로 접속 하였습니다.";
                    this.RobotOnlineConncet = true;
                }
                else
                {
                    Thread.Sleep(3000);//3초 대기 후 다시 시도
                    isConnect = _sharedMemory.SharedMemoryConnect();

                    messageBuffer = (isConnect == true) ? "PLC Robot 공유 메모리 로 접속 하였습니다." : "PLC Robot 공유 메모리 접속 실패하였습니다.";
                    this.RobotOnlineConncet = (isConnect == true) ? true : false;
                }

                LogManager.WriteConsoleLog(eLogLevel.Info, messageBuffer);
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
            }



            return this.RobotOnlineConncet;
        }

        /// <summary>
        /// 메인 스레드
        /// </summary>
        public override void Run()
        {
            try
            {
                //2021년 5월 25일 화요일 오전 11:37:07 - Editted by 수환 : 스레드 하나로 관리 하려고 슬립 부분 조정
                while (true)
                {
                    ////2021년 6월 24일 목요일 오후 1:40:27 - Editted by 수환 : 실시간 비트 체크
                    //BitCheck();

                    ////2021년 7월 8일 목요일 오후 1:40:27 - Editted by 수환 : 인터락 추가
                    //InterlockCheck();

                    //명령들어왔는지 체크
                    if (CurrentCmd != null)
                    {
                        switch (CurrentCmd.Command)
                        {
                            case enumMessageName.CRANE_MOVE:
                                if (this.ActionCommand(CurrentCmd))
                                {
                                    GData.WCF_mgr.ReportRobotStatus(ModuleName);
                                    StartProcess(CurrentCmd);
                                }
                                GData.CraneActiveJobList.Remove(CurrentCmd);
                                CurrentCmd = null;
                                break;

                            case enumMessageName.CRANE_GET:
                            case enumMessageName.CRANE_S_GET:
                            case enumMessageName.CRANE_EMO_GET:
                                if (this.ActionCommand(CurrentCmd))
                                {
                                    GData.WCF_mgr.ReportRobotStatus(ModuleName);
                                    StartProcess(CurrentCmd);
                                }
                                GData.CraneActiveJobList.Remove(CurrentCmd);
                                CurrentCmd = null;
                                break;

                            case enumMessageName.CRANE_PUT:
                            case enumMessageName.CRANE_S_PUT:
                            case enumMessageName.CRANE_EMO_PUT:
                                if (this.ActionCommand(CurrentCmd))
                                {
                                    GData.WCF_mgr.ReportRobotStatus(ModuleName);
                                    StartProcess(CurrentCmd);
                                }

                                GData.CraneActiveJobList.Remove(CurrentCmd);
                                CurrentCmd = null;
                                break;

                            case enumMessageName.CRANE_RETURN_HOME:
                            case enumMessageName.CRANE_EMO_RETURN_HOME:
                                if (this.ActionCommand(CurrentCmd))
                                {
                                    CraneHomeAction();
                                }
                                else
                                {
                                    GData.CraneActiveJobList.Remove(CurrentCmd);
                                    CurrentCmd = null;
                                }
                                break;

                            // 2020.11.10 LCS Robot 동작 테스트 수정 Crane Start, Crane Stop , Error Rest
                            case enumMessageName.CRANE_START:
                                if (this.ActionCommand(CurrentCmd))
                                {
                                    CraneStartAction();
                                }
                                else
                                {
                                    GData.CraneActiveJobList.Remove(CurrentCmd);
                                    CurrentCmd = null;
                                }
                                break;

                            case enumMessageName.CRANE_STOP:
                                if (this.ActionCommand(CurrentCmd))
                                {
                                    CraneStopAction();
                                }
                                else
                                {
                                    GData.CraneActiveJobList.Remove(CurrentCmd);
                                    CurrentCmd = null;
                                }
                                break;

                            case enumMessageName.CRANE_ERROR_RESET:
                                if (this.ActionCommand(CurrentCmd))
                                {
                                    GData.WCF_mgr.ReportRobotStatus(ModuleName);
                                    StartProcess(CurrentCmd);
                                }
                                GData.CraneActiveJobList.Remove(CurrentCmd);
                                CurrentCmd = null;
                                break;

                            case enumMessageName.CRANE_CHUCK:
                                CraneChuckAction();
                                break;

                            case enumMessageName.CRANE_UNCHUCK:
                                CraneUnChuckAction();
                                break;

                            default:
                                CurrentCmd = null;
                                break;
                        }
                    }

                    Thread.Sleep(100);
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Fatal, "== PLCRobotModule.Run() Exception : {0}", ex.ToString());
            }
        }

        /// <summary>
        /// Run에서 온 커멘드 분석 : Run->ActionCommand
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        public override bool ActionCommand(CraneCommand cmd)
        {

            try
            {
                LogManager.WriteRobotLog(eLogLevel.Info, this.ModuleName, "ActionCommand :{0} TargetBank:{1} TargetLevel:{2} TargetBay:{3} TargetCarrierID:{4}  TargetType:{5}", cmd.Command, cmd.TargetBank,
                    cmd.TargetLevel, cmd.TargetBay, cmd.TargetCarrierID, cmd.TargetType);

                //if (this.RMAlarmCheck())
                //    return false;

                ShelfItem Targert = new ShelfItem();

                #region Convert cmd 분석
                eRMCommand ConvertCmd = eRMCommand.None;
                switch (cmd.Command)
                {
                    case enumMessageName.None:
                        break;
                    case enumMessageName.SYSTEM_CHECK:
                        break;
                    case enumMessageName.STATUS_DATA_REQUEST:
                        break;
                    case enumMessageName.STATUS_DATA_REPORT:
                        break;

                    case enumMessageName.CRANE_GET:
                    case enumMessageName.CRANE_EMO_GET:
                    case enumMessageName.CRANE_S_GET:
                        ConvertCmd = eRMCommand.GET;
                        break;

                    case enumMessageName.CRANE_PUT:
                    case enumMessageName.CRANE_EMO_PUT:
                    case enumMessageName.CRANE_S_PUT:
                        ConvertCmd = eRMCommand.PUT;
                        break;

                    case enumMessageName.CRANE_MOVE:
                        ConvertCmd = eRMCommand.MOVE;
                        break;

                    case enumMessageName.CRANE_START:
                        return true;

                    case enumMessageName.CRANE_STOP:
                        return PLCStop();

                    case enumMessageName.CRANE_ERROR_RESET:
                        return PLCErrorReset();

                    case enumMessageName.CRANE_RETURN_HOME:
                    case enumMessageName.CRANE_EMO_RETURN_HOME:
                        return RMinitCmd();

                    case enumMessageName.PORT_MANUAL:
                        break;
                    case enumMessageName.CRANE_CHUCK:
                        break;
                    case enumMessageName.CRANE_UNCHUCK:
                        break;
                    case enumMessageName.CRANE_ATTEACH_START:
                        break;
                    case enumMessageName.CRANE_ATTEACH_STOP:
                        break;
                    case enumMessageName.IO_MONITORING_REQUEST:
                        break;
                    case enumMessageName.TOWERLAMP_DATA_REPORT:
                        break;
                    case enumMessageName.TOWERLAMP_SET:
                        break;
                    default:
                        break;
                }
                #endregion

                // 인터락 check
                if (ConvertCmd == eRMCommand.None)
                    return false;

                string tmpshelfID = string.Empty;

                //cmd.TargetType
                switch (cmd.TargetType)
                {
                    case enumCraneTarget.None:
                        break;
                    case enumCraneTarget.PORT:
                        #region port

                        //Targert = PortData.Where(r => r.TagName.Substring( == cmd.TargetLevel.ToString()).FirstOrDefault();
                        //2021년 6월 11일 금요일 오전 11:32:24 - Editted by 수환 : 확인하자
                        //Targert.TagName = String.Format("{0}{1}{2}", cmd.TargetBank.ToString("D2"), cmd.TargetLevel.ToString("D2"), cmd.TargetBay.ToString("D2"));
                        Targert.TagName = String.Format("{0}{1}{2}", cmd.TargetBank.ToString("D2"), cmd.TargetBay.ToString("D2"), cmd.TargetLevel.ToString("D2"));

                        #endregion
                        break;
                    case enumCraneTarget.SHELF:

                        tmpshelfID = String.Format("{0}{1}{2}", cmd.TargetBank, cmd.TargetLevel.ToString("D3"), cmd.TargetBay.ToString("D3"));

                        if (cmd.TargetBank == (int)eShelfBank.Rear)
                        {
                            Targert = RearData.Where(r => r.TagName == tmpshelfID).FirstOrDefault();
                        }
                        else if (cmd.TargetBank == (int)eShelfBank.Front)
                        {
                            Targert = FrontData.Where(r => r.TagName == tmpshelfID).FirstOrDefault();
                        }

                        break;

                    default:
                        break;
                }
                return RMMoveCommand(Targert, ConvertCmd, cmd, false);
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Fatal, "== PLCRobotModule.ActionCommand() Exception : {0}", ex.ToString());
                return false;
            }

        }

        /// <summary>
        /// ActionCommand 의 명령을 RM에게 보낸 후 완료 : Run >> ActionCommand >> RMMoveCommand
        /// </summary>
        /// <param name="ShelfId"></param>
        /// <param name="Type"></param>
        /// <param name="cmd"></param>
        /// <param name="bfrom"></param>
        /// <returns></returns>
        public override bool RMMoveCommand(ShelfItem ShelfId, eRMCommand Type, CraneCommand cmd, bool bfrom = false)
        {
            //string ChangeId;
            bool CompleteFlag = false;
            string RMCmd = string.Empty;

            if (RobotOnlineConncet == false)
            {
                return false;
            }

            //210218 lsj 인터락 추가
            #region 도어 인터락 체크

            if (GlobalData.Current.MainBooth.GetBoothDoorOpenState() == "0")
            {
                //LogManager.WriteConsoleLog(eLogLevel.Info, "TagName:{0} Type:{1} cmd:{2} 명령을 실행 합니다. ", ShelfId.TagName, Type, cmd);
            }
            else
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "TagName:{0} Type:{1} cmd:{2} 명령을 취소합니다. 도어인터락 에러!", ShelfId.TagName, Type, cmd);
                return false;
            }

            #endregion


            if (ShelfId == null)
            {
                GlobalData.Current.Alarm_Manager.AlarmOccur("13", this.ModuleName); // ShelfID가 비어 있습니다.
                return false;
            }


            #region // 2021.01.28 Move Command 시  RobotAccessAble Check 추가

            var Cv = GlobalData.Current.LineManager.GetCVModuleByTag(ShelfId.TagName.Substring(4, 2));
            if (Cv != null) // CV Module이면
            {
                //센서 체크
                if (Type == eRMCommand.GET && !Cv.CarrierExist)
                {
                    GlobalData.Current.Alarm_Manager.AlarmOccur("41", this.ModuleName); // MATERIAL_NOT_DETECTED 자재감지되지않음 
                    return false;
                }
                else if (Type == eRMCommand.PUT && Cv.CarrierExist)
                {
                    GlobalData.Current.Alarm_Manager.AlarmOccur("40", this.ModuleName); // MATERIAL_DETECTED 자재감지됨
                    return false;
                }
                //포트 엑세스 가능 체크
                if (Type != eRMCommand.MOVE) //무브 동작은 판단할 필요 없음
                {
                    DateTime DT = DateTime.Now;
                    while (true) //포트와 로봇간 오토 모드 변환 속도 때문에 에러발생하여 리트라이 추가.
                    {
                        if (Cv.RobotAccessAble)
                        {
                            LogManager.WriteConsoleLog(eLogLevel.Info, "{0} 모듈의 RobotAccessAble 신호를 확인하였습니다.", Cv.ModuleName);
                            break;
                        }
                        else
                        {
                            if (IsTimeOut(DT, 5) == true) //넉넉하게 5초 기다려본다.
                            {
                                GlobalData.Current.Alarm_Manager.AlarmOccur("42", this.ModuleName); // INPORT_MATERIAL_CANNOT_SETTLED 
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
            else //쉘프 일때
            {
                // 2021.020.19 TrayHeight 인터락 추가
                if (Type == eRMCommand.PUT && RMParameter.TrayHeightInterLock == use)
                {
                    if (!CheckTrayInterLock(ShelfId.TagName, this.CurrentTrayHeight))
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "Shelf :{0} <=== {1} 투입중 트레이 높이매칭에 실패하였습니다.", ShelfId.TagName, CurrentTrayHeight);
                        GlobalData.Current.Alarm_Manager.AlarmOccur(ROBOT_PUT_FAIL, this.ModuleName); // 높이 알람은 없어서 PUT 알람으로 변경.
                        return false;
                    }
                }
            }
            #endregion

            if (Type != eRMCommand.MOVE && Type != eRMCommand.GET && Type != eRMCommand.PUT)
            {
                //알람발생 추가 예정
                return false;
            }

            if (RobotState.nRmBusy == true || RobotState.nRmAlarm == true)
            {
                GlobalData.Current.Alarm_Manager.AlarmOccur("2", this.ModuleName);  // 로봇수행중 명령중복발생
                return false;
            }

            if (ShelfId.TagName.Substring(0, 1) == "0")
            {
                RMCmd = ShelfId.TagName;
            }
            else
            {
                RMCmd = String.Format("0{0}{1}{2}", ShelfId.TagName.Substring(0, 1), ShelfId.TagName.Substring(5, 2), ShelfId.TagName.Substring(2, 2));
            }

            switch (Type)
            {
                case eRMCommand.MOVE:
                    this.MoveReq = true;  // 190724  명령시 Button 동작 확인
                    CompleteFlag = PLCMove(RMCmd);
                    break;

                case eRMCommand.GET:
                    this.GetReq = true;  // 190724  명령시 Button 동작 확인
                    CompleteFlag = PLCGet(RMCmd);
                    break;

                case eRMCommand.PUT:
                    this.PutReq = true;  // 190724  명령시 Button 동작 확인
                    CompleteFlag = PLCPut(RMCmd);
                    break;

                case eRMCommand.AUTOTEACH:
                    return true; // Auto Teach은 리턴 바로

                default:
                    break;
            }

            Thread.Sleep(200);

            //SuHwan_20210628 : 일단 주석처리
            //if (CompleteFlag == true)
            //{
            //    if (RobotState.nRmAlarm == true)
            //    {
            //        GlobalData.Current.Alarm_Manager.AlarmOccur(RobotState.ErrorCode, this.ModuleName);
            //        return false;
            //    }

            //    XAxisPostion = Convert.ToDecimal(RMCmd.Substring(2, 2));

            //    ShelfUpDate(ShelfId.TagName, Type);

            //    if (this.SimulMode)
            //    {
            //        if (Type == eManualCommand.PUT)
            //        {
            //            this.Place_Sensor_Exist = false;
            //            this.CarrierID = string.Empty;
            //        }
            //        else if (Type == eManualCommand.GET)
            //        {
            //            this.Place_Sensor_Exist = true;
            //            if (cmd != null)
            //                this.CarrierID = cmd.TargetCarrierID;
            //        }
            //    }
            //    return true;
            //}
            //else
            //{
            //    GlobalData.Current.Alarm_Manager.AlarmOccur("9001", this.ModuleName);  //연결불량 알람
            //    return false;
            //}

            if (CompleteFlag == true)
            {
                DateTime curTime = DateTime.Now;
                string CheckRxbuffer = string.Empty;
                bool ArmStateFlag = false;

                while (!IsTimeOut(curTime, 30))
                {
                    if (SimulMode) break;

                    if (RobotState.nRmAlarm == true)
                    {
                        //210514 lsj 자재감지 추가
                        if (RobotState.ErrorCode == "40") //자재감지 에러
                        {
                            return true;
                        }

                        GlobalData.Current.Alarm_Manager.AlarmOccur(RobotState.ErrorCode, this.ModuleName);
                        return false;
                    }

                    //완료 체크
                    if (RobotState.nRmHome == true && RobotState.nRmBusy == false && RobotState.nRmAlarm == false)
                    {
                        Thread.Sleep(500);
                        //ArcStateUpdate();
                        break;
                    }
                    Thread.Sleep(500);
                }

                XAxisPostion = Convert.ToDecimal(RMCmd.Substring(2, 2));

                ShelfUpDate(ShelfId.TagName, Type);

                if ((Type != eRMCommand.ArmFolding) && (Type != eRMCommand.ArmStretch) && (int.Parse(ShelfId.TagName.Substring(4, 2)) > 20)) //201214 임광일 : Manual Move는 Skip
                {
                    var RobotAccessCV = GlobalData.Current.LineManager.GetCVModuleByTag(ShelfId.TagName.Substring(4, 2));

                    if (Type == eRMCommand.PUT)
                    {
                        Tray PutTray = new Tray("ERROR", true);
                        if (CurrentCmd == null)
                        {
                            ;
                        }
                        else if (string.IsNullOrEmpty(CurrentCmd.TargetTagID)) //Tag 없을시 
                        {
                            PutTray.UpdateCarrierID(CurrentCmd.TargetCarrierID); //캐리어 아이디만 업데이트
                        }
                        else
                        {
                            PutTray.UpdateTagID(CurrentCmd.TargetTagID, false); //상위에서 내려준 태그 아이디 트레이에 써서 포트로 보낸다.

                            if (PutTray.CarrierID != CurrentCmd.TargetCarrierID) //CTray 로직
                            {
                                LogManager.WriteConsoleLog(eLogLevel.Info, "Tag 중 CarrierID : {0} 와 내려준 CarrierID: {1} 항목이 다르므로 내려준 CarrierID로 업데이트 합니다.", PutTray.CarrierID, CurrentCmd.TargetCarrierID);
                                PutTray.UpdateCarrierID(CurrentCmd.TargetCarrierID);
                            }
                        }

                        RobotAccessCV?.NotifyTrayLoadComplete(PutTray, this);
                        this.CarrierID = string.Empty;
                    }
                    else if (Type == eRMCommand.GET)
                    {
                        // 2021.020.19 TrayHeight 인터락 추가
                        //this.CurrentTrayHeight = GlobalData.Current.LineManager.GetCVModuleByTag(ShelfId.TagName.Substring(4, 2)).GetTrayHeightByData();

                        RobotAccessCV?.NotifyTrayUnloadComplete(this);

                        if (cmd != null)
                            this.CarrierID = cmd.TargetCarrierID;
                    }

                    if (this.IOSimulMode)
                    {
                        if (Type == eRMCommand.PUT)
                        {
                            this.Place_Sensor_Exist = false;
                            this.CarrierID = string.Empty;
                        }
                        else if (Type == eRMCommand.GET)
                        {
                            this.Place_Sensor_Exist = true;
                            if (cmd != null)
                                this.CarrierID = cmd.TargetCarrierID;
                        }
                    }
                }
                if (!this.IOSimulMode)
                    this.Place_Sensor_Exist = RobotState.nRmArmDetect;


                return true;
            }
            else
            {
                GlobalData.Current.Alarm_Manager.AlarmOccur(ROBOT_COMMUNICATE_FAIL, this.ModuleName);  //연결불량 알람
                return false;
            }

        }
        /// <summary>
        /// 상시 체크
        /// </summary>
        private void checkAlwaysCheckPLC()
        {
            if (RobotOnlineConncet)
            {
                updataRMBitState();
                updateRmWordState();
            }
        }

        /// <summary>
        /// 상시체크 테스크 생성 하는곳
        /// </summary>
        private void createAlwaysCheckPLC()
        {
            //만약을 위해 켄슬토큰 생성했는데 필요하면 사용하자..
            CancellationToken token = _plcParameter._taskCancelToken.Token;

            Task task = Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    try
                    {
                        token.ThrowIfCancellationRequested();

                        if (RobotOnlineConncet)
                        {
                            updataRMBitState();
                            updateRmWordState();

                            //2021년 7월 8일 목요일 오후 1:40:27 - Editted by 수환 : 인터락 추가
                            InterlockCheck();
                        }
                        else
                        {
                            bool AlarmExist = GlobalData.Current.Alarm_Manager.CheckAlarmExist(ROBOT_COMMUNICATE_FAIL);

                            if (!AlarmExist)
                            {
                                GlobalData.Current.Alarm_Manager.AlarmOccur(ROBOT_COMMUNICATE_FAIL, this.ModuleName);
                            }
                        }


                        //2021년 6월 24일 목요일 오후 1:40:27 - Editted by 수환 : 실시간 비트 체크
                        BitCheck();



                        Thread.Sleep(10);
                    }
                    catch (Exception ex)
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Fatal, "== PLCRobotModule.createAlwaysCheckPLC() Exception : {0}", ex.ToString());
                    }
                }

            }, token);
        }

        /// <summary>
        /// 비트를 스트링으로 계산 하는곳
        /// </summary>
        /// <param name="rcvBit"></param>
        /// <returns></returns>
        public string computeBitToString(int rcvBit)
        {
            string binaryString = string.Empty;

            //가져온 정보를 비트 형식으로 변경
            binaryString = Convert.ToString(rcvBit, 2);
            //1Word = 16bit 나머지부분 0으로 채워줌
            binaryString = binaryString.PadLeft(16, '0');
            //PLC F -> 0순으로 데이터를 기입해줌. 뒤집어준다.
            return string.Concat(binaryString.Reverse());
        }

        /// <summary>
        /// 인트를 핵사로 바꾸는 곳
        /// </summary>
        /// <param name="rcvData"></param>
        /// <returns></returns>
        public string IntToHex(int rcvData)
        {
            byte[] bytes = BitConverter.GetBytes(rcvData);
            return Encoding.Default.GetString(bytes).Trim('\0');
        }


        /// <summary>
        /// 비트 에서 읽어 상태 갱신(상시 체크)
        /// </summary>
        public void updataRMBitState()
        {
            string CheckRxbuffer = string.Empty;
            CheckRxbuffer = SharedMemoryReadBit(_IFAddress_Start, 5);

            if (string.IsNullOrEmpty(CheckRxbuffer))
                return;

            //여기여기
            RobotIF.isOnCIMAlive        = (CheckRxbuffer.Substring(0, 1) == "1") ? eBitState.ON : eBitState.OFF;
            RobotIF.isOnCommand         = (CheckRxbuffer.Substring(1, 1) == "1") ? eBitState.ON : eBitState.OFF;
            RobotIF.isOnPLCAlive        = (CheckRxbuffer.Substring(2, 1) == "1") ? eBitState.ON : eBitState.OFF;
            RobotIF.isOnAlarm           = (CheckRxbuffer.Substring(3, 1) == "1") ? eBitState.ON : eBitState.OFF;
            RobotIF.isOnCommanReply     = (CheckRxbuffer.Substring(4, 1) == "1") ? eBitState.ON : eBitState.OFF;

            //RobotIF.isOnEMS = (CheckRxbuffer.Substring(10, 1) == "1") ? eBitState.ON : eBitState.OFF;
            //RobotIF.isOnDoorOpen = (CheckRxbuffer.Substring(11, 1) == "1") ? eBitState.ON : eBitState.OFF;
            //RobotIF.isOnTray_Align_MI12 = (CheckRxbuffer.Substring(12, 1) == "1") ? eBitState.ON : eBitState.OFF;
            //RobotIF.isOnTray_Align_AI21 = (CheckRxbuffer.Substring(13, 1) == "1") ? eBitState.ON : eBitState.OFF;
            //RobotIF.isOnTray_Align_AI11 = (CheckRxbuffer.Substring(14, 1) == "1") ? eBitState.ON : eBitState.OFF;
            //RobotIF.isOnAlarm_CIM = (CheckRxbuffer.Substring(15, 1) == "1") ? eBitState.ON : eBitState.OFF;

            //CimAlive 1초 마다 온/오프
            if (IsTimeOut(_plcParameter._PioData._timeOutCimAlive, 1) == true) //1초마다 비트 변경
            {
                _plcParameter._PioData._timeOutCimAlive = DateTime.Now;
                PLCIFMAP_Value ifBuffer = _plcParameter._dicPLCIFMap_Bit[eBitIFMap.CIMAlive.ToString()];

                bool isComplete = (RobotIF.isOnCIMAlive == eBitState.ON) ?
                    SharedMemoryWriteBit(ifBuffer._startAddress, eBitState.OFF) : SharedMemoryWriteBit(ifBuffer._startAddress, eBitState.ON);
            }

            //PLCAlive 확인 중 3초간 변경 없으면 알람 발생
            bool checkAliveBitSame = (_plcParameter._PioData._oldPLCAliveBit == RobotIF.isOnPLCAlive) ? true : false;
            if (checkAliveBitSame)
            {
                if (_plcParameter._PioData._timeOutPLCAlive == null)
                    _plcParameter._PioData._timeOutPLCAlive = DateTime.Now; ;

                if (IsTimeOut(Convert.ToDateTime(_plcParameter._PioData._timeOutPLCAlive), 100) == true) //3초 동안 변경 되는지 체크//2021년 6월 10일 목요일 오후 4:54:47 - Editted by 수환 : 100초에서 바꾸자
                {
                    _plcParameter._PioData._timeOutPLCAlive = null;
                    GlobalData.Current.Alarm_Manager.AlarmOccur(ROBOT_COMMUNICATE_FAIL, this.ModuleName); //통신 불량 알람
                    RobotOnlineConncet = false; //2021년 6월 30일 수요일 오후 3:33:39 - Editted by 수환 : 로봇 동작 인터락 추가
                }
            }
            else
            {
                if (_plcParameter._PioData._timeOutPLCAlive != null)
                    _plcParameter._PioData._timeOutPLCAlive = null;
            }

        }

        /// <summary>
        /// 워드 에서 읽어 RM 상태 갱신(상시 체크)
        /// </summary>
        /// <returns></returns>
        protected void updateRmWordState()
        {
            // string CheckRxbuffer = string.Empty;

            string CheckRxbuffer = string.Empty;

            CheckRxbuffer = SharedMemoryReadWord(_replyAddress_Start, 80);

            if (string.IsNullOrEmpty(CheckRxbuffer))
                return;

            if (CheckRxbuffer.Substring(0, 5) == "?MSTS")
            {
                RobotState.nRmHome = CheckRxbuffer.Substring(5, 1) == "1" ? true : false;
                RobotState.nRmBusy = CheckRxbuffer.Substring(6, 1) == "1" ? true : false;
                RobotState.nRmAlarm = CheckRxbuffer.Substring(7, 1) == "1" ? true : false;
                RobotState.nRmEMG = CheckRxbuffer.Substring(8, 1) == "1" ? true : false;
                RobotState.nRmArmDetect = CheckRxbuffer.Substring(9, 1) == "1" ? true : false;
                RobotState.nRmArmExtend = CheckRxbuffer.Substring(10, 1) == "1" ? true : false;
                RobotState.nRmJobState = CheckRxbuffer.Substring(15, 1) == "1" ? true : false;
                RobotState.Position_Bank = CheckRxbuffer.Substring(16, 1);
                RobotState.Position_Bay = CheckRxbuffer.Substring(17, 2);
                RobotState.Position_Level = CheckRxbuffer.Substring(19, 2);
                //RobotState.ErrorCode = CheckRxbuffer.Substring(23, 2); //2021년 6월 30일 수요일 오후 4:06:43 - Editted by 수환 : 로봇 알람코드 받는곳이 바뀜

                RobotState.nRmClampState = CheckRxbuffer.Substring(25, 2);
                RobotState.nRmAutoTeach = CheckRxbuffer.Substring(26, 1) == "1" ? true : false;


                RobotState.isTilt_LR4 = CheckRxbuffer.Substring(27, 1) == "1" ? true : false;
                RobotState.isTilt_LR2 = CheckRxbuffer.Substring(28, 1) == "1" ? true : false;
                RobotState.isTilt_FB1 = CheckRxbuffer.Substring(29, 1) == "1" ? true : false;
                RobotState.isTilt_LR3 = CheckRxbuffer.Substring(30, 1) == "1" ? true : false;
                RobotState.isTilt_LR1 = CheckRxbuffer.Substring(31, 1) == "1" ? true : false;
                RobotState.isTilt_FB2 = CheckRxbuffer.Substring(32, 1) == "1" ? true : false;
                RobotState.Mileage_Z = CheckRxbuffer.Substring(33, 5);
                RobotState.Mileage_T = CheckRxbuffer.Substring(38, 5);
                RobotState.Mileage_Y = CheckRxbuffer.Substring(43, 5);
                RobotState.Mileage_A = CheckRxbuffer.Substring(48, 5);

                this.Place_Sensor_Exist = RobotState.nRmArmDetect;
            }

            //2021년 6월 30일 수요일 오후 4:08:32 - Editted by 수환 : 로봇 알람 발생시 알람 코드 받아오자
            if(RobotState.nRmAlarm)
            {
                CheckRxbuffer = string.Empty;
                //2021년 7월 8일 수요일 오후 4:08:32 - Editted by 수환 : 공백 삭제 추가
                CheckRxbuffer = SharedMemoryReadWord(_alarmAddress_Start, 4).Trim();

                if (string.IsNullOrEmpty(CheckRxbuffer))
                    RobotState.ErrorCode = string.Empty;

                if(RobotState.ErrorCode != CheckRxbuffer)
                    RobotState.ErrorCode = CheckRxbuffer;
            }
            else// 알람이 없으면 empty 로 초기화 시켜놓자
            {
                if(!string.IsNullOrEmpty(RobotState.ErrorCode))
                {
                    RobotState.ErrorCode = string.Empty;
                }
            }

            return;
        }

        /// <summary>
        /// RM 동작 완료 확인 
        /// </summary>
        /// <param name="rcvCommand"></param>
        /// <returns></returns>
        protected bool CheckRMComplite(string rcvCommand)
        {
            DateTime curTime = DateTime.Now;
            bool ArmExtendFlag = false;
            bool ArmTransferFlag = false;

            while (!IsTimeOut(curTime, 60)) //1분
            {

                if (RobotState.nRmBusy == true && ArmTransferFlag == false) //처음 트랜스퍼 보고
                {
                    GData.WCF_mgr.ReportRobotStatus(ModuleName);
                    ArmTransferFlag = true;
                }

                if (RobotState.nRmArmExtend == true && ArmExtendFlag == false) //암 스트레치 보고
                {
                    GData.WCF_mgr.ReportRobotStatus(ModuleName);
                    ArmExtendFlag = true;
                }

                if (!RobotState.nRmBusy)//Busy,idle 로 동작 컴플리트 확인 상태 확인
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "PLC Robot Cmd receive = {0}", rcvCommand);
                    this.InitReq = false;
                    return true;
                }
                DoEvents();

            }

            LogManager.WriteConsoleLog(eLogLevel.Info, "PLC Robot Cmd receive Time Out");

            return false;
        }

        /// <summary>
        /// 로봇 커멘드 센드
        /// </summary>
        /// <param name="rcvMessage"></param>
        /// <returns></returns>
        public bool CommandSend(string rcvMessage)
        {

            if (RobotOnlineConncet == false)
                return false;

            if (rcvMessage.Length > _commandAddress_Length)
                return false;


            SharedMemoryWriteWord(_commandAddress_Start, rcvMessage.PadRight(13, ' '));

            LogManager.WriteConsoleLog(eLogLevel.Info, "PLC Robot Cmd send = {0}", rcvMessage);

            DateTime curTime = DateTime.Now;
            //주석 지우자
            _plcParameter._PioData.changeState(ePIOState.Executeing);

            while (!IsTimeOut(curTime,5)) //5초
            {
                if (interfaceCommandSignal() && _plcParameter._PioData._pioState == ePIOState.Complete)//PIO 진행
                    return true;

                Thread.Sleep(100);
            }

            //2021년 5월 28일 금요일 오후 4:54:07 - Editted by 수환 : 
            //_pioState 가 error 처리 되는데 사용할때가 있으면 사용하자.. 현재는 없음..

            LogManager.WriteConsoleLog(eLogLevel.Info, "PLC Robot Cmd PIO Time Out {0}", "aa");
            _plcParameter._PioData.changeState(ePIOState.Error);
            

            return false;
        }

        /// <summary>
        /// PLC 와 Bit PIO 하는 곳
        /// </summary>
        /// <returns></returns>
        public bool interfaceCommandSignal()
        {
            if (_plcParameter._PioData._pioState != ePIOState.Executeing)
                return false;

            int stepNo = _plcParameter._PioData._stepNo;
            bool isComplete = false;
            PLCIFMAP_Value ifBuffer;

            switch (stepNo)
            {
                case 0://상태 확인 및 시작
                    if (RobotIF.isOnCommanReply == eBitState.ON)//시작 안했는데 PLC에서 비트가 활성화 되면 에러처리하자
                        return processPIOError("CommanReply Bit ON Error", stepNo);

                    ifBuffer = _plcParameter._dicPLCIFMap_Bit[eBitIFMap.Command.ToString()];
                    isComplete = SharedMemoryWriteBit(ifBuffer._startAddress, eBitState.OFF);

                    LogManager.WriteConsoleLog(eLogLevel.Info, "PLC Robot PIO Start - Step:{0}", stepNo.ToString());
                    stepNo++;
                    break;

                case 1://Command Bit On
                    ifBuffer = _plcParameter._dicPLCIFMap_Bit[eBitIFMap.Command.ToString()];
                    isComplete = SharedMemoryWriteBit(ifBuffer._startAddress, eBitState.ON);

                    if (isComplete)
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "PLC Robot [CIM -> PLC] Command Bit On - Step:{0}", stepNo.ToString());
                        _plcParameter._PioData._timeOutPIO = DateTime.Now;
                        stepNo++;
                    }
                    else
                        return processPIOError("Command Bit ON Write Error", stepNo);

                    break;

                case 2://CommanReply On 대기
                    if (RobotIF.isOnCommanReply == eBitState.ON)
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "PLC Robot [PLC -> Cim] Command Bit On - Step:{0}", stepNo.ToString());
                        _plcParameter._PioData._timeOutPIO = null;
                        stepNo++;
                    }
                    //2021년 6월 2일 수요일 오후 1:35:52 - Editted by 수환 : 2->3 초로 변경
                    else if (IsTimeOut(Convert.ToDateTime(_plcParameter._PioData._timeOutPIO), 300) == true) //2초 가 넘으면 에러
                        return processPIOError("CommanReply Bit On TimeOut Error", stepNo);

                    break;

                case 3://Command Bit Off
                    ifBuffer = _plcParameter._dicPLCIFMap_Bit[eBitIFMap.Command.ToString()];
                    isComplete = SharedMemoryWriteBit(ifBuffer._startAddress, eBitState.OFF);

                    if (isComplete)
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "PLC Robot [CIM -> PLC] Command Bit Off - Step:{0}", stepNo.ToString());
                        _plcParameter._PioData._timeOutPIO = DateTime.Now;
                        stepNo++;
                    }
                    else
                        return processPIOError("Command Bit OFF Write Error", stepNo);

                    break;

                case 4://CommanReply Off 대기
                    if (RobotIF.isOnCommanReply == eBitState.OFF)
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "PLC Robot [PLC -> Cim] Command Bit Off - Step:{0}", stepNo.ToString());
                        _plcParameter._PioData._timeOutPIO = null;
                        stepNo++;
                    }
                    //2021년 6월 2일 수요일 오후 1:36:29 - Editted by 수환 : 2->3 초 로 변경
                    else if (IsTimeOut(Convert.ToDateTime(_plcParameter._PioData._timeOutPIO), 300) == true) //3초 가 넘으면 에러
                        return processPIOError("CommanReply Bit Off TimeOut Error", stepNo);
                    break;

                case 5://완료 처리
                    LogManager.WriteConsoleLog(eLogLevel.Info, "PLC Robot PIO Complete - Step:{0}", stepNo.ToString());
                    _plcParameter._PioData.changeState(ePIOState.Complete);
                    return true;

                default:
                    return processPIOError("Cannot Find Step Number", 999);

            }

            _plcParameter._PioData._stepNo = stepNo;
            return false;
        }

        /// <summary>
        /// PIO 에러 처리 하는곳
        /// </summary>
        /// <param name="rcvMessage"></param>
        /// <param name="rcvStepNo"></param>
        /// <returns></returns>
        public bool processPIOError(string rcvMessage, int rcvStepNo)
        {
            //2021년 6월 24일 목요일 오후 3:40:57 - Editted by 수환 : 에러시 비트 다 죽인다
            PLCIFMAP_Value ifBuffer = _plcParameter._dicPLCIFMap_Bit[eBitIFMap.Command.ToString()];
            SharedMemoryWriteBit(ifBuffer._startAddress, eBitState.OFF);

            _plcParameter._PioData.changeState(ePIOState.Error);
            
            LogManager.WriteConsoleLog(eLogLevel.Info, "PLC Robot {0} - Step:{1}", rcvMessage, rcvStepNo);


            return false;
        }

        #region <- Robot Command ->
        protected bool PLCHome()
        {
            string sendMessage;
            string sCmd = string.Empty;
            sCmd = string.Format("MHOM{0}", _plcParameter._endMARK);

            CommandSend(sCmd);

            //2021년 7월 8일 목요일 오후 3:24:11 - Editted by 수환 : 홈 메시지 추가
            if (CheckRMComplite(sCmd) && RobotState.nRmHome)
            {
                sendMessage = "RM1 의 HOME 동작 을 완료 하였습니다.";
                GlobalData.Current.SendMessageEvent = sendMessage;
                return true;
            }                 
            else
            {
                sendMessage = "RM1 의 HOME 동작 을 실패 하였습니다.";
                GlobalData.Current.SendMessageEvent = sendMessage;
                return false;
            }       
        }

        protected bool PLCGet(string RevCmd)
        {
            string sCmd = string.Empty;
            sCmd = string.Format("MGET{0}{1}", RevCmd, _plcParameter._endMARK);

            CommandSend(sCmd);

            return CheckRMComplite(sCmd);
        }

        protected bool PLCPut(string RevCmd)
        {
            string sCmd = string.Empty;
            sCmd = string.Format("MPUT{0}{1}", RevCmd, _plcParameter._endMARK);

            CommandSend(sCmd);

            return CheckRMComplite(sCmd);
        }

        protected bool PLCMove(string RevCmd)
        {
            string sCmd = string.Empty;
            sCmd = string.Format("MAPP{0}{1}", RevCmd, _plcParameter._endMARK);

            CommandSend(sCmd);

            return CheckRMComplite(sCmd);
        }

        protected bool PLCStop()
        {
            string sCmd = string.Empty;
            sCmd = string.Format("MSTP{0}", _plcParameter._endMARK);

            CommandSend(sCmd);

            return CheckRMComplite(sCmd);
        }

        protected bool PLCErrorReset()
        {
            string sCmd = string.Empty;
            sCmd = string.Format("MRES{0}", _plcParameter._endMARK);

            CommandSend(sCmd);

            return CheckRMComplite(sCmd);
        }

        protected bool PLCAutoTeachingStart()
        {
            string sCmd = string.Empty;
            sCmd = string.Format("STTC{0}", _plcParameter._endMARK);

            CommandSend(sCmd);

            return CheckRMComplite(sCmd);
        }

        protected bool PLCAutoTeachingStop()
        {
            string sCmd = string.Empty;
            sCmd = string.Format("STAT{0}", _plcParameter._endMARK);

            CommandSend(sCmd);

            return CheckRMComplite(sCmd);
        }

        protected bool PLCEMOHome()
        {
            string sCmd = string.Empty;
            sCmd = string.Format("STAT{0}", _plcParameter._endMARK);

            CommandSend(sCmd);

            return CheckRMComplite(sCmd);
        }

        protected bool PLCEMOGet(string RevCmd)
        {
            string sCmd = string.Empty;
            sCmd = string.Format("EGET{0}{1}", RevCmd, _plcParameter._endMARK);

            CommandSend(sCmd);

            return CheckRMComplite(sCmd);
        }

        protected bool PLCEMOPut(string RevCmd)
        {
            string sCmd = string.Empty;
            sCmd = string.Format("EPUT{0}{1}", RevCmd, _plcParameter._endMARK);

            CommandSend(sCmd);

            return CheckRMComplite(sCmd);
        }

        /// <summary>
        /// RM 이니셜 명령
        /// </summary>
        /// <returns></returns>
        public override bool RMinitCmd()
        {
            // 초기화시 버튼 블링크 사용에 사용한다
            this.InitReq = true;

            if (RobotOnlineConncet != true)
            {
                if (RMConnecting(GlobalData.Current.RMSection.RM1Element.IPAddress, GlobalData.Current.RMSection.RM1Element.Port) == false)
                    return false;
            }

            return (PLCHome() == true) ? true : false;
        }

        public override bool RMHandGrip()   // 메뉴얼 사용
        {
            string sCmd = string.Empty;
            sCmd = string.Format("CLMP{0}", _plcParameter._endMARK);

            CommandSend(sCmd);

            return CheckRMComplite(sCmd);
        }

        public override bool RMHandUnGrip() // 메뉴얼 사용
        {
            string sCmd = string.Empty;
            sCmd = string.Format("UNCL{0}", _plcParameter._endMARK);

            CommandSend(sCmd);

            return CheckRMComplite(sCmd);
        }

        //210416 lsj ARC 혼용 수정
        public override string GetUnGrip()
        {
            return (RobotState.nRmClampState == "2") ? "1" : "0";
        }
        //210416 lsj ARC 혼용 수정
        public override string GetGrip()
        {
            return (RobotState.nRmClampState == "1") ? "1" : "0";
        }

        public override bool CheckRMBusy() 
        {
            return RobotState.nRmBusy;
        }

        public override eRMState GetRMState() 
        {
            if (RobotState.nRmHome == true)
            {
                return eRMState.Initialized_Idle;
            }
            else
            {
                return eRMState.Unknown;
            }
        }

        /// <summary>
        /// RM 정지 명령 
        /// </summary>
        /// <returns></returns>
        public override bool MoveStopCmd()  // 메뉴얼 사용
        {
            return (PLCStop() == true) ? true : false;
        }

        public override void RMModChange(eRMMode mode)
        {
            return;
        }

        #endregion

        #region<- WCP ->

        public override bool SetWCFCommand(CraneCommand Cmd)
        {
            //2021년 7월 9일 금요일 오후 3:49:25 - Editted by 수환 : 타이밍 때문에 0.5초 대기
            if (CurrentCmd != null)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "현재 명령: {0} 진행중에 {1} 명령이 내려와 0.5초 대기", CurrentCmd.Command, Cmd.Command);
                Thread.Sleep(500);
            }

            if (CurrentCmd != null)
            {
                //작업중에 명령이 또 내려오면 일단 버린다.
                LogManager.WriteConsoleLog(eLogLevel.Info, "현재 명령: {0} 진행중에 {1} 명령이 내려왔으므로 버림", CurrentCmd.Command, Cmd.Command);
                return false;
            }
            CurrentCmd = Cmd;

            GData.CraneActiveJobList.Add(Cmd);

            StartProcess(Cmd);
            return true;
        }

        private void CraneStopAction()
        {
            Thread.Sleep(CommandDelay);
            this.EMERGENCYS = "1";
            Thread.Sleep(CommandDelay);
            GData.CraneActiveJobList.Remove(CurrentCmd);
            StartProcess(CurrentCmd);
            Thread.Sleep(CommandDelay);
            CurrentCmd = null;
            GData.WCF_mgr.ReportRobotStatus(ModuleName);
        }

        private void CraneStartAction()
        {
            Thread.Sleep(CommandDelay);
            this.EMERGENCYS = "0";
            Thread.Sleep(CommandDelay);
            GData.CraneActiveJobList.Remove(CurrentCmd);
            StartProcess(CurrentCmd);
            Thread.Sleep(CommandDelay);
            CurrentCmd = null;
            GData.WCF_mgr.ReportRobotStatus(ModuleName);
        }

        private void CraneAutoTeachingStartAction()
        {
            Thread.Sleep(CommandDelay);
            this.AUTOTEACHING = "1";
            GData.WCF_mgr.ReportRobotStatus(ModuleName);
            GData.CraneActiveJobList.Remove(CurrentCmd);
            StartProcess(CurrentCmd);
            Thread.Sleep(CommandDelay);
            CurrentCmd = null;
        }

        private void CraneAutoTeachingStopAction()
        {
            Thread.Sleep(CommandDelay);
            this.AUTOTEACHING = "2";
            GData.CraneActiveJobList.Remove(CurrentCmd);
            StartProcess(CurrentCmd);
            Thread.Sleep(CommandDelay);
            CurrentCmd = null;
            GData.WCF_mgr.ReportRobotStatus(ModuleName);
        }

        private void CraneResetAction()
        {
            Thread.Sleep(CommandDelay);
            this.ERRORSTAUTS = "0";
            this.ERRORCODE = "0";
            GData.CraneActiveJobList.Remove(CurrentCmd);
            StartProcess(CurrentCmd);
            Thread.Sleep(CommandDelay);
            CurrentCmd = null;
            GData.WCF_mgr.ReportRobotStatus(ModuleName);
        }

        private void CraneHomeAction()
        {
            Thread.Sleep(CommandDelay);
            this.ARMSTRETCH = "0";
            this.HOME = "1";
            GData.CraneActiveJobList.Remove(CurrentCmd);
            StartProcess(CurrentCmd);
            Thread.Sleep(CommandDelay);
            CurrentCmd = null;
            GData.WCF_mgr.ReportRobotStatus(ModuleName);
        }

        private void CraneGetAction()
        {
            //Step 1
            Thread.Sleep(CommandDelay);
            this.BUSY = "1";
            this.TRANSFERRING = "1";
            GData.WCF_mgr.ReportRobotStatus(ModuleName);
            Thread.Sleep(ActionDelay);

            //Step 2
            this.ARMSTRETCH = "1";
            this.BANK = CurrentCmd.TargetBank.ToString();
            this.BAY = CurrentCmd.TargetBay.ToString();
            this.LEVEL = CurrentCmd.TargetLevel.ToString();
            GData.WCF_mgr.ReportRobotStatus(ModuleName);
            Thread.Sleep(ActionDelay);

            //Step 3
            this.CARRIERCONTAIN = "1";
            this.ARMSTRETCH = "0";
            GData.WCF_mgr.ReportRobotStatus(ModuleName);
            Thread.Sleep(ActionDelay);

            //Step 4
            this.HOME = "1";
            this.BUSY = "0";
            this.TRANSFERRING = "0";

            GData.CraneActiveJobList.Remove(CurrentCmd);
            StartProcess(CurrentCmd);
            CurrentCmd = null;
            Thread.Sleep(CommandDelay);
            GData.WCF_mgr.ReportRobotStatus(ModuleName);
        }

        private void CranePutAction()
        {
            //Step 1
            Thread.Sleep(CommandDelay);
            this.BUSY = "1";
            this.TRANSFERRING = "1";
            GData.WCF_mgr.ReportRobotStatus(ModuleName);
            Thread.Sleep(ActionDelay);

            //Step 2
            this.ARMSTRETCH = "1";
            this.BANK = CurrentCmd.TargetBank.ToString();
            this.BAY = CurrentCmd.TargetBay.ToString();
            this.LEVEL = CurrentCmd.TargetLevel.ToString();
            GData.WCF_mgr.ReportRobotStatus(ModuleName);
            Thread.Sleep(ActionDelay);

            //Step 3
            this.CARRIERCONTAIN = "0";
            this.ARMSTRETCH = "0";
            GData.WCF_mgr.ReportRobotStatus(ModuleName);
            Thread.Sleep(ActionDelay);

            //Step 4
            this.HOME = "1";
            this.BUSY = "0";
            this.TRANSFERRING = "0";

            GData.CraneActiveJobList.Remove(CurrentCmd);
            StartProcess(CurrentCmd);
            CurrentCmd = null;
            Thread.Sleep(CommandDelay);
            GData.WCF_mgr.ReportRobotStatus(ModuleName);
        }

        private void CraneMoveAction()
        {
            Thread.Sleep(CommandDelay);
            this.BUSY = "1";
            this.TRANSFERRING = "1";
            GData.WCF_mgr.ReportRobotStatus(ModuleName);
            Thread.Sleep(ActionDelay);

            this.BANK = CurrentCmd.TargetBank.ToString();
            this.BAY = CurrentCmd.TargetBay.ToString();
            this.LEVEL = CurrentCmd.TargetLevel.ToString();
            GData.WCF_mgr.ReportRobotStatus(ModuleName);
            Thread.Sleep(ActionDelay);


            this.BUSY = "0";
            this.TRANSFERRING = "0";

            Thread.Sleep(CommandDelay);
            GData.CraneActiveJobList.Remove(CurrentCmd);
            StartProcess(CurrentCmd);
            CurrentCmd = null;
            GData.WCF_mgr.ReportRobotStatus(ModuleName);
        }

        private void CraneChuckAction()
        {
            Thread.Sleep(CommandDelay);
            this.CHUCK = "1";

            Thread.Sleep(CommandDelay);

            GData.CraneActiveJobList.Remove(CurrentCmd);
            StartProcess(CurrentCmd);

            CurrentCmd = null;
            GData.WCF_mgr.ReportRobotStatus(ModuleName);
        }

        private void CraneUnChuckAction()
        {
            Thread.Sleep(CommandDelay);
            this.CHUCK = "2";

            Thread.Sleep(CommandDelay);
            GData.CraneActiveJobList.Remove(CurrentCmd);
            StartProcess(CurrentCmd);

            CurrentCmd = null;
            GData.WCF_mgr.ReportRobotStatus(ModuleName);
        }

        public override Parameter_ROBOT GetRobotStatusPara()
        {
            Parameter_ROBOT PRobot = new Parameter_ROBOT(this.ModuleName);

            //if (SimulMode)
            //{
            //    PRobot.Transferring = this.TRANSFERRING;
            //    PRobot.Bank = this.BANK;
            //    PRobot.Bay = this.BAY;
            //    PRobot.Level = this.LEVEL;
            //    PRobot.CarrierContain = (this.Place_Sensor_Exist) ? "1" : "0";
            //    PRobot.Home = this.HOME;
            //    PRobot.EmergencyStop = this.EMERGENCYS;
            //    PRobot.Busy = CheckRMBusy() ? "1" : "0";      //0: IDLE   1:BUSY
            //    PRobot.ArmStretch = ARMSTRETCH;
            //    PRobot.ErrorStatus = CheckModuleAlarmExist() ? "1" : "0";      //0:NORMAL   1:ERROR
            //    PRobot.ErrorCode = GetModuleLastAlarmCode();      //5DIGIT CODE
            //    if (PRobot.ErrorCode == "88") //임시로직
            //    {
            //        PRobot.ErrorCode = "18";
            //    }
            //    PRobot.Chuck = this.CHUCK;
            //}
            //else
            {
                //PRobot.Transferring = this.TRANSFERRING;
                PRobot.Transferring = (RobotState.nRmBusy) ? "1" : "0";

                PRobot.Bank = RobotState.Position_Bank;//this.BANK;
                PRobot.Bay = RobotState.Position_Bay;//this.BAY;
                PRobot.Level = RobotState.Position_Level;//this.LEVEL;
                PRobot.CarrierContain = (RobotState.nRmArmDetect) ? "1" : "0";
                PRobot.Home = (RobotState.nRmHome) ? "1" : "0";
                PRobot.EmergencyStop = (RobotState.nRmEMG) ? "1" : "0";
                PRobot.Busy = (RobotState.nRmBusy) ? "1" : "0";
                PRobot.ArmStretch = (RobotState.nRmArmExtend) ? "1" : "0";

                PRobot.ErrorStatus = (RobotState.nRmAlarm) ? "1" : "0";       //0:NORMAL   1:ERROR
                PRobot.ErrorCode = (RobotState.ErrorCode);                    //5DIGIT CODE

                PRobot.Chuck = RobotState.nRmClampState;

                //    PRobot.Transferring = this.TRANSFERRING;
                //    PRobot.Bank = this.BANK;
                //    PRobot.Bay = this.BAY;
                //    PRobot.Level = this.LEVEL;
                //    PRobot.CarrierContain = (this.Place_Sensor_Exist) ? "1" : "0";
                //    PRobot.Home = this.HOME;
                //    PRobot.EmergencyStop = this.EMERGENCYS;
                //    PRobot.Busy = CheckRMBusy() ? "1" : "0";      //0: IDLE   1:BUSY
                //    PRobot.ArmStretch = ARMSTRETCH;
                //    PRobot.ErrorStatus = CheckModuleAlarmExist() ? "1" : "0";      //0:NORMAL   1:ERROR
                //    PRobot.ErrorCode = GetModuleLastAlarmCode();      //5DIGIT CODE
                //    if (PRobot.ErrorCode == "88") //임시로직
                //    {
                //        PRobot.ErrorCode = "18";
                //    }
                //    PRobot.Chuck = this.CHUCK;
            }

            return PRobot;
        }

        #endregion

        public override void CloseController()
        {
            try
            {

            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
            }
        }

        #region 소모품 관련
        //2021년 6월 24일 목요일 오후 3:24:11 - Editted by 수환 : 소모품 운영 어떻게 해야되는지 확인이 필요하다

        public override int GetFork_CycleDistance()
        {
            return Convert.ToInt32(RobotState.Mileage_A);
        }
        public override int GetZ_CycleDistance()
        {
            return Convert.ToInt32(RobotState.Mileage_Z);
        }
        public override int GetDrive_CycleDistance()
        {
            return Convert.ToInt32(RobotState.Mileage_Y);
        }
        public override int GetTurn_CycleDistance()
        {          
            return   Convert.ToInt32(RobotState.Mileage_T);
        }

        #endregion


        /// <summary>
        /// PLC XML 데이타 읽기.. 간단히 읽기만
        /// </summary>
        public void PLCXmlRead()
        {
            string FrontTeachDataPath = "\\Data\\RM\\Parameter_PLC.xml";
            string FullPath = System.Environment.CurrentDirectory;
            var purchaseOrderFilepath = Path.Combine(GlobalData.Current.CurrentFilePaths(FullPath) + FrontTeachDataPath);
            XElement purchaseOrder = XElement.Load(purchaseOrderFilepath);

            //Descendants 카운터를 불러오는 법을 몰라 포문으로 작성 나중에 바꾸자
            for (int i = 0; i < 2; i++)
            {
                string descendantsBuffer = (i == 0) ? "BitAddressListItem" : "WordAddressListItem";

                foreach (var item in purchaseOrder.Descendants(descendantsBuffer))
                {
                    PLCIFMAP_Value valueBuffer = new PLCIFMAP_Value();

                    valueBuffer._name = String.IsNullOrEmpty(item.Attribute("Name").Value.ToString()) ? string.Empty : item.Attribute("Name").Value.ToString();
                    valueBuffer._number = String.IsNullOrEmpty(item.Attribute("number").Value.ToString()) ? -1 : Convert.ToInt32(item.Attribute("number").Value);
                    valueBuffer._definition = String.IsNullOrEmpty(item.Attribute("Definition").Value.ToString()) ? string.Empty : item.Attribute("Definition").Value.ToString();
                    valueBuffer._description = String.IsNullOrEmpty(item.Attribute("Description").Value.ToString()) ? string.Empty : item.Attribute("Description").Value.ToString();
                    valueBuffer._area = String.IsNullOrEmpty(item.Attribute("Area").Value.ToString()) ? string.Empty : item.Attribute("Area").Value.ToString();
                    valueBuffer._startAddress = String.IsNullOrEmpty(item.Attribute("StartAddress").Value.ToString()) ? string.Empty : item.Attribute("StartAddress").Value.ToString();
                    valueBuffer._EndAddress = String.IsNullOrEmpty(item.Attribute("EndAddress").Value.ToString()) ? string.Empty : item.Attribute("EndAddress").Value.ToString();
                    valueBuffer._length = String.IsNullOrEmpty(item.Attribute("Length").Value.ToString()) ? -1 : Convert.ToInt32(item.Attribute("Length").Value);

                    //Message Type : Enum 에서 찾아서 넣기
                    if (!String.IsNullOrEmpty(item.Attribute("Type").Value.ToString()))
                    {
                        string textBuffer = item.Attribute("Type").Value.ToString();

                        foreach (ePLCMessageType item2 in Enum.GetValues(typeof(ePLCMessageType)))
                        {
                            if (textBuffer.StartsWith(item2.ToString(), StringComparison.InvariantCultureIgnoreCase))
                            {
                                valueBuffer._type = item2;
                                break;
                            }
                        }
                    }

                    if (descendantsBuffer == "BitAddressListItem")
                        _plcParameter._dicPLCIFMap_Bit.Add(valueBuffer._name, valueBuffer);
                    else
                        _plcParameter._dicPLCIFMap_Word.Add(valueBuffer._name, valueBuffer);
                }
            }
        }

        /// <summary>
        /// 공유메모리 워드 읽기
        /// </summary>
        /// <param name="szDevice"></param>
        /// <param name="iSize"></param>
        /// <returns></returns>
        public string SharedMemoryReadWord(String szDevice, int iSize = 1)
        {
            int[] arrDeviceValue = new int[iSize];
            bool isConncet = false;
            string returnValue = string.Empty;

            //220315 HHJ SCS 개발     //- MXComponentNet, SharedMemory 개선     //에러 주석처리
            //isConncet = _sharedMemory.ReadDeviceBlock(szDevice, iSize, out arrDeviceValue);

            if (!isConncet) //정상적으로 리턴받으면 0을 받는다.
            {
                //알람 추가 하자
                _RobotSharedMemoryConncet = false;
            }

            //읽어온 워드만큼 바이트로 변경하여 스트링형으로 가져온다.
            for (int i = 0; i < arrDeviceValue.Length; i++)
            {
                returnValue += (char)arrDeviceValue[i];
                //(char)

                //byte[] bytes = BitConverter.GetBytes(arrDeviceValue[i]);
                //returnValue += Encoding.Default.GetString(bytes).Trim('\0');
            }

            return returnValue;
        }

        /// <summary>
        /// 공유메모리 워드 쓰기
        /// </summary>
        /// <param name="szDevice"></param>
        /// <param name="rcvMessage"></param>
        /// <returns></returns>
        public bool SharedMemoryWriteWord(String szDevice, string rcvMessage)
        {
            bool isConncet = false;
            int[] sArray;

            sArray = new int[rcvMessage.Length];

            for (int i = 0; i < rcvMessage.Length; i++)
            {
                sArray[i] = Convert.ToInt32(rcvMessage[i]);
            }

            //220315 HHJ SCS 개발     //- MXComponentNet, SharedMemory 개선     //에러 주석처리
            //isConncet = _sharedMemory.WriteDeviceBlock(szDevice, ref sArray);

            if (!isConncet) //정상적으로 리턴받으면 0을 받는다.
            {
                //알람 추가 하자
                _RobotSharedMemoryConncet = false;
                return false;
            }

            return true;
        }

        /// <summary>
        /// 공유메모리 비트 읽기
        /// </summary>
        /// <param name="szDevice"></param>
        /// <param name="iSize"></param>
        /// <returns></returns>
        public string SharedMemoryReadBit(String szDevice, int iSize = 1)
        {
            int[] arrDeviceValue = new int[iSize];
            bool isConncet = false;
            string returnValue = string.Empty;

            //220315 HHJ SCS 개발     //- MXComponentNet, SharedMemory 개선     //에러 주석처리
            //isConncet = _sharedMemory.ReadDeviceBlock(szDevice, iSize, out arrDeviceValue);

            if (!isConncet) //정상적으로 리턴받으면 0을 받는다.
            {
                //알람 추가 하자
                _RobotSharedMemoryConncet = false;
            }

            //읽어온 워드만큼 바이트로 변경하여 스트링형으로 가져온다.
            for (int i = 0; i < arrDeviceValue.Length; i++)
            {
                returnValue += arrDeviceValue[i].ToString();
            }

            return returnValue;
        }

        /// <summary>
        /// 공유메모리 비트 쓰기
        /// </summary>
        /// <param name="rcvDevice"></param>
        /// <param name="rcvBitState"></param>
        /// <returns></returns>
        public bool SharedMemoryWriteBit(String rcvDevice, eBitState rcvBitState)
        {
            bool isConncet = false;
            int[] sArray = new int[1];

            sArray[0] = (int)rcvBitState;

            //220315 HHJ SCS 개발     //- MXComponentNet, SharedMemory 개선     //에러 주석처리
            //isConncet = _sharedMemory.WriteDeviceBlock(rcvDevice, ref sArray);

            if (!isConncet) //정상적으로 리턴받으면 0을 받는다.
            {
                //알람 추가 하자
                _RobotSharedMemoryConncet = false;
                return false;
            }

            return true;
        }

        //2021년 6월 24일 목요일 오후 1:35:21 - Editted by 수환 : 
        /// <summary>
        /// 실시간으로 비트 확인 및 넘기기
        /// </summary>
        public void BitCheck()
        {
            //bool setBitState = (RobotIF.isOnEMS == eBitState.ON) ? true : false;
            //if (CCLinkManager.CCLCurrent.ReadIO(GlobalData.Current.MainBooth.ModuleName, "SWITCH_EMS1") != setBitState)
            //    CCLinkManager.CCLCurrent.WriteIO(GlobalData.Current.MainBooth.ModuleName, "ROBOT_STOP", setBitState);


            //RobotIF.isOnEMS = (CheckRxbuffer.Substring(10, 1) == "1") ? eBitState.ON : eBitState.OFF;
            //RobotIF.isOnDoorOpen = (CheckRxbuffer.Substring(11, 1) == "1") ? eBitState.ON : eBitState.OFF;
            //RobotIF.isOnTray_Align_MI12 = (CheckRxbuffer.Substring(12, 1) == "1") ? eBitState.ON : eBitState.OFF;
            //RobotIF.isOnTray_Align_AI21 = (CheckRxbuffer.Substring(13, 1) == "1") ? eBitState.ON : eBitState.OFF;
            //RobotIF.isOnTray_Align_AI11 = (CheckRxbuffer.Substring(14, 1) == "1") ? eBitState.ON : eBitState.OFF;
            //RobotIF.isOnAlarm_CIM = (CheckRxbuffer.Substring(15, 1) == "1") ? eBitState.ON : eBitState.OFF;
        }

        /// <summary>
        /// 이터락 체크하는곳 
        /// </summary>
        public void InterlockCheck()
        {
            bool AlarmExist;

            if(!RobotState.nRmHome)
            {
                if (!string.IsNullOrEmpty(RobotState.ErrorCode))
                {
                    AlarmExist = GlobalData.Current.Alarm_Manager.CheckAlarmExist(ROBOT_HOME_ERROR);

                    if (!AlarmExist)
                    {
                        GlobalData.Current.Alarm_Manager.AlarmOccur(ROBOT_HOME_ERROR, this.ModuleName);
                    }
                }
            }

            if(RobotState.nRmAlarm)//로봇 알람 상태 확인
            {
                if(!string.IsNullOrEmpty(RobotState.ErrorCode))
                {
                    AlarmExist = GlobalData.Current.Alarm_Manager.CheckAlarmExist(RobotState.ErrorCode);

                    if (!AlarmExist)
                    {
                        GlobalData.Current.Alarm_Manager.AlarmOccur(RobotState.ErrorCode, this.ModuleName);
                    }
                }
            }

            if(RobotState.nRmEMG)//로봇 EMG 상태 확인 
            {
                AlarmExist = GlobalData.Current.Alarm_Manager.CheckAlarmExist(ROBOT_EMG_ERROR);

                if (!AlarmExist)
                {
                    GlobalData.Current.Alarm_Manager.AlarmOccur(ROBOT_EMG_ERROR, this.ModuleName);
                }
            }

        }

        /// <summary>
        /// 알람 체크
        /// </summary>
        /// <returns></returns>
        public override bool RMAlarmCheck()
        {
            return RobotState.nRmAlarm;
        }

        /// <summary>
        /// 알람 리셋 
        /// </summary>
        /// <returns></returns>
        public override bool RMAlarmReset()
        {
            return PLCErrorReset();
        }
    }
}