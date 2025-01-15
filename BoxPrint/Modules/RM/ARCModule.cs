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
using Stockerfirmware.Modules.Shelf;

namespace Stockerfirmware.Modules.RM
{
    class ArcRobotState
    {
        public ArcRobotState()
        {
            //뭐하지
        }

        public bool nRmHome = false;                 // (1=Home완료, 0=Not Home)
        public bool nRmBusy = false;                 //Busy or Idle(1=Busy, 0=Idle)
        public bool nRmAlarm = false;                //Alarm or Normal (1=Alarm, 0=Normal)
        public bool nRmEMG = false;                  //Emg or Normal (1=Emg, 0=Normal)
        public bool nRmArmDetect = false;            //Arm Object 센서 (1=유, 0=무)
        public bool nRmArmExtend = false;            //Arm Extend 상태 (1=Extend, 0=Retract)
        public bool nRmJobState = false;             //작업상태(1=작업중, 0=대기중)
        public string Position_Bank = string.Empty;  //현재 위치(Bank, 1=1Bank, 2=2Bank)
        public string Position_Bay = string.Empty;   //현재 위치(Bay)
        public string Position_Level = string.Empty; //현재 위치(Level)
        public string ErrorCode = string.Empty;      //Error Code
        public string nRmClampState = string.Empty;  //Clamp 상태(0: None, 1:Clamp, 2:Unclamp)
        public bool nRmAutoTeach = false;            //(1: AutoTeaching 중, 0: None)

        public int Z_MoveDistance = 0; //높이축 주행거리
        public int T_MoveDistance = 0; //턴축 주행거리
        public int Y_MoveDistance = 0; //주행축 주행거리
        public int A_MoveDistance = 0; //포크축 주행거리

    }
    class ARCModule : RMModuleBase
    {
        //CraneCommand CurrentCmd;
        public AsyncClientSocket Arc_sock = null;
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

        ArcRobotState RobotState;

        protected const string END_MARK = "\r";
        public bool RxOn;
        public string Rxbuffer;
        public readonly int use = 1;

        private bool _RobotOnlineConncet = false;
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

        #region TCP
        protected void OnError(object sender, AsyncSocketErrorEventArgs e)
        {
        }

        protected void OnReceive(object sender, AsyncSocketReceiveEventArgs e)
        {
            string rData = Encoding.ASCII.GetString(e.ReceiveData);
            rData = rData.Replace("\0", string.Empty);
            //rData = rData.Trim();
            this.Rxbuffer = rData;
            this.RxOn = true;
        }


        protected void OnSend(object sender, AsyncSocketSendEventArgs e)
        {
            //
        }

        protected void OnClose(object sender, AsyncSocketConnectionEventArgs e)
        {
            RobotOnlineConncet = false;
            GlobalData.Current.Alarm_Manager.AlarmOccur("9001", this.ModuleName);  // 로봇수행중 명령중복발생
            LogManager.WriteConsoleLog(eLogLevel.Info, "ARC Robot 접속이 종료되었습니다");
        }

        private void OnConnet(object sender, AsyncSocketConnectionEventArgs e)
        {
            MR_connectionEvent.Set();
            RobotOnlineConncet = true;
            LogManager.WriteConsoleLog(eLogLevel.Info, "ARC Robot 접속 하였습니다.");
        }

        #endregion

        public ARCModule(string ID, bool simul, eRMType RMType, bool ioSimul)
            : base(ID, simul, RMType, ioSimul)
        {
            ModuleName = ID;

            if (ID == "RM2")
            {
                //BAY = "61";
            }

            Arc_sock = new AsyncClientSocket(1);
            Arc_sock.OnConnet += new AsyncSocketConnectEventHandler(OnConnet);
            Arc_sock.OnClose += new AsyncSocketCloseEventHandler(OnClose);
            Arc_sock.OnSend += new AsyncSocketSendEventHandler(OnSend);
            Arc_sock.OnReceive += new AsyncSocketReceiveEventHandler(OnReceive);
            Arc_sock.OnError += new AsyncSocketErrorEventHandler(OnError);

            RMIp = GlobalData.Current.RMSection.RM1Element.IPAddress;
            RMPort = GlobalData.Current.RMSection.RM1Element.Port;


            // c 티칭값 상태값
            string tmpPath = string.Format("\\Data\\" + GlobalData.TestModel + "RM\\FrontTeachingData{0}.xml", ModuleName);
            //this.FrontData = ShelfItemList.Deserialize(GData.CurrentFilePaths(GData.FullPath) + tmpPath);

            tmpPath = string.Format("\\Data\\" + GlobalData.TestModel + "RM\\RearTeachingData{0}.xml", ModuleName);
            //this.RearData = ShelfItemList.Deserialize(GData.CurrentFilePaths(GData.FullPath) + tmpPath);

            // Port 티칭값
            tmpPath = string.Format("\\Data\\" + GlobalData.TestModel + "RM\\PortTeachingData{0}_ARC.xml", ModuleName);
            this.PortData = ShelfItemList.Deserialize(GData.CurrentFilePaths(GData.FullPath) + tmpPath);

            RMStateFilePath = string.Format(GData.CurrentFilePaths(GData.FullPath) + "\\Data\\RM\\StateData{0}.ini", ModuleName);


            RMConnecting(RMIp, RMPort);


            RobotState = new ArcRobotState();

            //210820 예외발생으로 위치변경
            //if (RobotOnlineConncet == true)
            //{
            //    ArcStateUpdate();
            //}

            if (this.SimulMode)
                Place_Sensor_Exist = SimulPlace_Sensor();
            else
                Place_Sensor_Exist = RobotState.nRmArmDetect;

            GData = GlobalData.Current;
            Thread runThread = new Thread(new ThreadStart(Run));
            runThread.Start();
        }


        public override bool RMConnecting(string m_hostName, int m_port)
        {
            if (this.SimulMode)
                return true;

            if (this.RobotOnlineConncet == true)
                return true;

            MR_connectionEvent.Reset();
            Arc_sock.Connect(m_hostName, m_port);
            bool ConnectResult = MR_connectionEvent.WaitOne(1000);

            if (ConnectResult)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "ARC Robot IP:{0}  Port:{1} 로 접속 하였습니다.", m_hostName, m_port);
                return true;
            }
            else
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "ARC Robot IP:{0}  Port:{1} 로 접속 실패하였습니다.", m_hostName, m_port);
                return false;
            }
        }

        public override void Run()
        {
            try //-메인 루프 예외 발생시 로그 찍도록 추가.
            {
                //210820 예외발생으로 위치변경
                if (RobotOnlineConncet == true)
                {
                    ArcStateUpdate();
                }

                while (true)
                {

                    if (GlobalData.Current.MainBooth.BoothState == eBoothState.AutoStart)
                    {
                        //ArcStateUpdate();
                        Thread.Sleep(500);
                    }

                    //명령들어왔는지 체크
                    if (CurrentCmd == null)
                    {
                        Thread.Sleep(100);
                        continue;
                    }

                    switch (CurrentCmd.Command)
                    {
                        case enumMessageName.CRANE_MOVE:
                            // 
                            if (this.ActionCommand(CurrentCmd))
                            {
                                StartProcess(CurrentCmd);
                            }
                            GData.CraneActiveJobList.Remove(CurrentCmd);
                            CurrentCmd = null;
                            Thread.Sleep(50);
                            GData.WCF_mgr.ReportRobotStatus(ModuleName); //210813 아진 로봇 보고 시점 변경
                            break;
                        case enumMessageName.CRANE_GET:
                        case enumMessageName.CRANE_S_GET:
                        case enumMessageName.CRANE_EMO_GET:
                            if (this.ActionCommand(CurrentCmd))
                            {
                                StartProcess(CurrentCmd);
                            }
                            GData.CraneActiveJobList.Remove(CurrentCmd);
                            CurrentCmd = null;
                            Thread.Sleep(50);
                            GData.WCF_mgr.ReportRobotStatus(ModuleName); //210813 아진 로봇 보고 시점 변경
                            break;
                        case enumMessageName.CRANE_PUT:
                        case enumMessageName.CRANE_S_PUT:
                        case enumMessageName.CRANE_EMO_PUT:
                            if (this.ActionCommand(CurrentCmd))
                            {
                                StartProcess(CurrentCmd);
                            }
                            GData.CraneActiveJobList.Remove(CurrentCmd);
                            CurrentCmd = null;
                            Thread.Sleep(50);
                            GData.WCF_mgr.ReportRobotStatus(ModuleName); //210813 아진 로봇 보고 시점 변경
                            break;

                        case enumMessageName.CRANE_RETURN_HOME:
                        case enumMessageName.CRANE_EMO_RETURN_HOME:
                            if (this.ActionCommand(CurrentCmd))
                            {
                                StartProcess(CurrentCmd);
                            }

                            GData.CraneActiveJobList.Remove(CurrentCmd);
                            CurrentCmd = null;
                            Thread.Sleep(50);
                            GData.WCF_mgr.ReportRobotStatus(ModuleName);
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
                        case enumMessageName.CRANE_UNCHUCK:
                            if (this.ActionCommand(CurrentCmd))
                            {
                                StartProcess(CurrentCmd);
                            }

                            GData.CraneActiveJobList.Remove(CurrentCmd);
                            CurrentCmd = null;
                            Thread.Sleep(50);
                            GData.WCF_mgr.ReportRobotStatus(ModuleName);
                            break;
                        default:
                            CurrentCmd = null;
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
            }
        }

        public override bool ActionCommand(CraneCommand cmd)
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

                case enumMessageName.CRANE_EMO_GET:
                    ConvertCmd = eRMCommand.EMCGET;//211021 lsj 명령추가
                    break;

                case enumMessageName.CRANE_GET:
                case enumMessageName.CRANE_S_GET:
                    ConvertCmd = eRMCommand.GET;
                    break;

                case enumMessageName.CRANE_EMO_PUT:
                    ConvertCmd = eRMCommand.EMCPUT;//211021 lsj 명령추가
                    break;

                case enumMessageName.CRANE_PUT:
                case enumMessageName.CRANE_S_PUT:
                    ConvertCmd = eRMCommand.PUT;
                    break;

                case enumMessageName.CRANE_MOVE:
                    ConvertCmd = eRMCommand.MOVE;
                    break;

                case enumMessageName.CRANE_START:
                    return true;

                case enumMessageName.CRANE_STOP:
                    return ArcStop();

                case enumMessageName.CRANE_ERROR_RESET:
                    return ArcErrorReset();

                case enumMessageName.CRANE_RETURN_HOME:
                    return RMinitCmd();

                case enumMessageName.CRANE_EMO_RETURN_HOME:
                    return ArcEMOHome(); //211021 lsj 명령추가

                case enumMessageName.PORT_MANUAL:
                    break;
                case enumMessageName.CRANE_CHUCK:
                    return RMHandGrip(); //211021 lsj 명령추가

                case enumMessageName.CRANE_UNCHUCK:
                    return RMHandUnGrip(); //211021 lsj 명령추가

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

        public override bool RMMoveCommand(ShelfItem ShelfId, eRMCommand Type, CraneCommand cmd, bool bfrom = false)
        {
            //string ChangeId;
            bool CompleteFlag = false;
            string RMCmd = string.Empty;

            //if (RobotOnlineConncet == false)
            //{
            //    return false;
            //}

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
            //210416 lsj ARC 주석 변경
            //var Cv = GlobalData.Current.LineManager.GetCVModuleByTag(ShelfId.TagName);
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
                // 2021.02.19 TrayHeight 인터락 추가
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

            
            if (Type != eRMCommand.MOVE && Type != eRMCommand.GET && Type != eRMCommand.PUT
                && Type != eRMCommand.EMCPUT && Type != eRMCommand.EMCGET) //211021 lsj 명령추가 주석
            {
                //알람발생 추가 예정
                return false;
            }

            ////210119 lsj 명령 들어왔을때 상태 체크
            ArcStateUpdate();

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
            if (!SimulMode)
            {
                switch (Type)
                {
                    case eRMCommand.MOVE:
                        this.MoveReq = true;  // 190724  명령시 Button 동작 확인
                        CompleteFlag = ArcMove(RMCmd);
                        break;

                    case eRMCommand.GET:
                        this.GetReq = true;  // 190724  명령시 Button 동작 확인
                        CompleteFlag = ArcGet(RMCmd);

                        break;
                    case eRMCommand.PUT:
                        this.PutReq = true;  // 190724  명령시 Button 동작 확인
                        CompleteFlag = ArcPut(RMCmd);

                        break;

                    case eRMCommand.EMCGET://211021 lsj 명령추가
                        this.GetReq = true;  // 190724  명령시 Button 동작 확인
                        CompleteFlag = ArcEMOGet(RMCmd);

                        break;
                    case eRMCommand.EMCPUT://211021 lsj 명령추가
                        this.PutReq = true;  // 190724  명령시 Button 동작 확인
                        CompleteFlag = ArcEMOPut(RMCmd);

                        break;

                    case eRMCommand.AUTOTEACH:

                        return true; // Auto Teach은 리턴 바로

                    default:
                        break;
                }
            }
            else
                CompleteFlag = true;
            Thread.Sleep(200);

            if (CompleteFlag == true)
            {
                DateTime curTime = DateTime.Now;
                string CheckRxbuffer = string.Empty;
                bool ArmStateFlag = false;

                while (!IsTimeOut(curTime, 30))
                {
                    if (SimulMode) break;

                    ArcStateUpdate(); //작업 끝났을때 상태체크

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

                    if (RobotState.nRmArmExtend == true && ArmStateFlag == false)
                    {
                        GData.WCF_mgr.ReportRobotStatus(ModuleName);
                        ArmStateFlag = true;
                    }
                    Thread.Sleep(500);
                }

                XAxisPostion = Convert.ToDecimal(RMCmd.Substring(2, 2));

                ShelfUpDate(ShelfId.TagName, Type);

                //if (this.SimulMode)
                //{
                //    if (Type == eManualCommand.PUT)
                //    {
                //        this.Place_Sensor_Exist = false;
                //        this.CarrierID = string.Empty;
                //    }
                //    else if (Type == eManualCommand.GET)
                //    {
                //        this.Place_Sensor_Exist = true;
                //        if (cmd != null)
                //            this.CarrierID = cmd.TargetCarrierID;
                //    }
                //}
                //else
                {
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
                }

                return true;
            }
            else
            {
                RobotOnlineConncet = false;
                RobotState.nRmAlarm = true;
                RobotState.ErrorCode = "9001";

                GlobalData.Current.Alarm_Manager.AlarmOccur("9001", this.ModuleName);  //연결불량 알람
                return false;
            }
        }

        #region Robot Command


        protected bool CheckRMComplite()
        {
            DateTime curTime = DateTime.Now;
            string CheckRxbuffer = string.Empty;
            string AlarmIndex = string.Empty;

            while (!IsTimeOut(curTime, 5)) //5초
            {
                if (RxOn == true)
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "ARC Robot Cmd receive = {0}", Rxbuffer);
                    LogManager.WriteRobotLog(eLogLevel.Info, this.ModuleName, "ARC Robot Cmd receive = {0}", Rxbuffer);

                    CheckRxbuffer = Rxbuffer;
                    Rxbuffer = string.Empty;
                    RxOn = false;
                    this.InitReq = false;

                    if (CheckRxbuffer.Substring(CheckRxbuffer.Length - 1, 1) == "\r")
                    {

                        if (CheckRxbuffer.Substring(0, 1) == "E") //Error 상태
                        {
                            AlarmIndex = CheckRxbuffer.Substring(4, 2);
                            LogManager.WriteConsoleLog(eLogLevel.Info, "Robot Alarm 발생 Alarm Code = {0}", AlarmIndex);
                            //GlobalData.Current.Alarm_Manager.AlarmOccur(AlarmIndex, this.ModuleName);  일단 보류 
                        }

                        return true;
                    }
                    else
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "ARC Robot Cmd receive Error");
                        return false;
                    }
                }
                DoEvents();
            }
            LogManager.WriteConsoleLog(eLogLevel.Info, "ARC Robot Cmd receive Time Out");
            //GlobalData.Current.Alarm_Manager.AlarmOccur(HOME_NOT_COMPLETE, this.ModuleName); // TIME OUT ALARM
            return false;
        }

        protected bool ArcStateUpdate()
        {
            if (!RobotOnlineConncet)
                return false;

            string sCmd = string.Empty;
            sCmd = string.Format("MSTS{0}", END_MARK);
            Arc_sock.Send(Encoding.ASCII.GetBytes(sCmd));
            LogManager.WriteConsoleLog(eLogLevel.Info, "ARC Robot Cmd send = {0}", sCmd);
            LogManager.WriteRobotLog(eLogLevel.Info, this.ModuleName, "ARC Robot Cmd send = {0}", sCmd);

            return ReciveRmState();
        }

        protected bool ReciveRmState()
        {
            DateTime curTime = DateTime.Now;
            string CheckRxbuffer = string.Empty;

            while (!IsTimeOut(curTime, 10)) //10초
            {
                if (RxOn == true)
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "ARC Robot State receive = {0}", Rxbuffer);
                    LogManager.WriteRobotLog(eLogLevel.Info, this.ModuleName, "ARC Robot State receive = {0}", Rxbuffer);

                    CheckRxbuffer = Rxbuffer;
                    Rxbuffer = string.Empty;
                    RxOn = false;
                    this.InitReq = false;

                    if (CheckRxbuffer.Substring(0, 4) == "ERRO")
                    {
                        //lsj SESS
                        continue;

                        //LogManager.WriteConsoleLog(eLogLevel.Info, "ARC Robot Cmd receive Error");
                        //RobotState.nRmAlarm = true;
                        //RobotState.ErrorCode = CheckRxbuffer.Substring(4, 2);

                        ////lsj SESS
                        //if (RobotState.ErrorCode.Substring(0, 1) == "0")
                        //    RobotState.ErrorCode = RobotState.ErrorCode.Substring(1, 1);       
                        
                        //return false;
                    }
                    else if (CheckRxbuffer.Substring(0, 5) != "?MSTS" || CheckRxbuffer.Substring(CheckRxbuffer.Length - 1, 1) != "\r")
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "ARC Robot Cmd receive Error");
                        //return false;
                        break;
                    }                    
                    else
                    {
                        RobotState.nRmHome = (CheckRxbuffer.Substring(5, 1) == "1") ? true : false;
                        RobotState.nRmBusy = CheckRxbuffer.Substring(6, 1) == "1" ? true : false;
                        RobotState.nRmAlarm = CheckRxbuffer.Substring(7, 1) == "1" ? true : false;
                        RobotState.nRmEMG = CheckRxbuffer.Substring(8, 1) == "1" ? true : false;
                        RobotState.nRmArmDetect = CheckRxbuffer.Substring(9, 1) == "1" ? true : false;
                        RobotState.nRmArmExtend = CheckRxbuffer.Substring(10, 1) == "1" ? true : false;
                        RobotState.nRmJobState = CheckRxbuffer.Substring(15, 1) == "1" ? true : false;
                        RobotState.Position_Bank = CheckRxbuffer.Substring(16, 1);
                        RobotState.Position_Bay = CheckRxbuffer.Substring(17, 2);
                        RobotState.Position_Level = CheckRxbuffer.Substring(19, 2);
                        RobotState.ErrorCode = CheckRxbuffer.Substring(23, 2);


                        //211126 RGJ 아진제어기 BAY,LEVEL 01로 보고되는 문제가 있어서 앞자리 0일때는 버림
                        if (RobotState.Position_Bay.Substring(0, 1) == "0")
                            RobotState.Position_Bay = RobotState.Position_Bay.Substring(1, 1);

                        //211126 RGJ 아진제어기 BAY,LEVEL 01로 보고되는 문제가 있어서 앞자리 0일때는 버림
                        if (RobotState.Position_Level.Substring(0, 1) == "0")
                            RobotState.Position_Level = RobotState.Position_Level.Substring(1, 1);

                        //lsj SESS
                        if (RobotState.ErrorCode.Substring(0, 1) == "0")
                            RobotState.ErrorCode = RobotState.ErrorCode.Substring(1, 1);


                        RobotState.nRmClampState = CheckRxbuffer.Substring(25, 1);
                        RobotState.nRmAutoTeach = CheckRxbuffer.Substring(26, 1) == "1" ? true : false;


                        RobotState.Z_MoveDistance = int.Parse(CheckRxbuffer.Substring(33, 5));
                        RobotState.T_MoveDistance = int.Parse(CheckRxbuffer.Substring(38, 5));
                        RobotState.Y_MoveDistance = int.Parse(CheckRxbuffer.Substring(43, 5));
                        RobotState.A_MoveDistance = int.Parse(CheckRxbuffer.Substring(48, 5));

                        LogManager.WriteConsoleLog(eLogLevel.Info, "ARC Robot Cmd State receive success");

                        this.Place_Sensor_Exist = RobotState.nRmArmDetect;

                        return true;
                    }
                }
                DoEvents();
            }

            RobotOnlineConncet = false;
            RobotState.nRmAlarm = true;
            RobotState.ErrorCode = "9001";

            GlobalData.Current.Alarm_Manager.AlarmOccur("9001", this.ModuleName);  //연결불량 알람
            LogManager.WriteConsoleLog(eLogLevel.Info, "ARC Robot Cmd receive Time Out");
            //GlobalData.Current.Alarm_Manager.AlarmOccur(HOME_NOT_COMPLETE, this.ModuleName); // TIME OUT ALARM
            return false;
        }

        protected bool ArcHome()
        {
            DateTime curTime = DateTime.Now;
            string sCmd = string.Empty;
            sCmd = string.Format("MHOM{0}", END_MARK);
            Arc_sock.Send(Encoding.ASCII.GetBytes(sCmd));

            LogManager.WriteConsoleLog(eLogLevel.Info, "ARC Robot Cmd send = {0}", sCmd);

            if (CheckRMComplite())
            {
                while (!IsTimeOut(curTime, 10)) //5초
                {
                    ArcStateUpdate();
                    Thread.Sleep(500);
                    if (RobotState.nRmHome && !RobotState.nRmBusy && !RobotState.nRmAlarm)
                        return true;
                }

                LogManager.WriteConsoleLog(eLogLevel.Info, "ARC Home Cmd fail = {0}", sCmd);
            }
            else
            {
                return false;
            }

            return false;
        }

        protected bool ArcGet(string RevCmd)
        {
            string sCmd = string.Empty;
            sCmd = string.Format("MGET{0}{1}", RevCmd, END_MARK);
            Arc_sock.Send(Encoding.ASCII.GetBytes(sCmd));
            LogManager.WriteConsoleLog(eLogLevel.Info, "ARC Robot Cmd send = {0}", sCmd);
            LogManager.WriteRobotLog(eLogLevel.Info, "ARC Robot Cmd send = {0}", sCmd);

            return CheckRMComplite();
        }

        protected bool ArcPut(string RevCmd)
        {
            string sCmd = string.Empty;
            sCmd = string.Format("MPUT{0}{1}", RevCmd, END_MARK);
            Arc_sock.Send(Encoding.ASCII.GetBytes(sCmd));
            LogManager.WriteConsoleLog(eLogLevel.Info, "ARC Robot Cmd send = {0}", sCmd);
            LogManager.WriteRobotLog(eLogLevel.Info, this.ModuleName, "ARC Robot Cmd send = {0}", sCmd);

            return CheckRMComplite();
        }

        protected bool ArcMove(string RevCmd)
        {
            string sCmd = string.Empty;
            sCmd = string.Format("MAPP{0}{1}", RevCmd, END_MARK);
            Arc_sock.Send(Encoding.ASCII.GetBytes(sCmd));
            LogManager.WriteConsoleLog(eLogLevel.Info, "ARC Robot Cmd send = {0}", sCmd);
            LogManager.WriteRobotLog(eLogLevel.Info, this.ModuleName, "ARC Robot Cmd send = {0}", sCmd);

            return CheckRMComplite();
        }

        protected bool ArcStop()
        {
            string sCmd = string.Empty;
            sCmd = string.Format("MSTP{0}", END_MARK);
            Arc_sock.Send(Encoding.ASCII.GetBytes(sCmd));
            LogManager.WriteRobotLog(eLogLevel.Info, this.ModuleName, "ARC Robot Cmd send = {0}", sCmd);

            return CheckRMComplite();
        }

        protected bool ArcErrorReset()
        {
            DateTime curTime = DateTime.Now;
            string sCmd = string.Empty;
            sCmd = string.Format("MRES{0}", END_MARK);
            Arc_sock.Send(Encoding.ASCII.GetBytes(sCmd));
            LogManager.WriteConsoleLog(eLogLevel.Info, "ARC Robot Cmd send = {0}", sCmd);

            //return CheckRMComplite();
            if (CheckRMComplite())
            {
                while (!IsTimeOut(curTime, 5)) //10초
                {
                    ArcStateUpdate();
                    Thread.Sleep(500);
                    if (RobotState.nRmAlarm == false && RobotState.nRmBusy == false)
                        return true;
                }
            }
            else
            {
                return true;
            }

            return true;
        }

        protected bool ArcAutoTeachingStart()
        {
            string sCmd = string.Empty;
            sCmd = string.Format("STTC{0}", END_MARK);
            Arc_sock.Send(Encoding.ASCII.GetBytes(sCmd));
            LogManager.WriteConsoleLog(eLogLevel.Info, "ARC Robot Cmd send = {0}", sCmd);

            return CheckRMComplite();
        }

        protected bool ArcAutoTeachingStop()
        {
            string sCmd = string.Empty;
            sCmd = string.Format("STAT{0}", END_MARK);
            Arc_sock.Send(Encoding.ASCII.GetBytes(sCmd));
            LogManager.WriteConsoleLog(eLogLevel.Info, "ARC Robot Cmd send = {0}", sCmd);

            return CheckRMComplite();
        }

        protected bool ArcEMOHome()
        {
            DateTime curTime = DateTime.Now;

            string sCmd = string.Empty;
            sCmd = string.Format("EHOM{0}", END_MARK);
            Arc_sock.Send(Encoding.ASCII.GetBytes(sCmd));
            LogManager.WriteConsoleLog(eLogLevel.Info, "ARC Robot Cmd send = {0}", sCmd);

            if (CheckRMComplite()) //211021 lsj 명령추가
            {
                while (!IsTimeOut(curTime, 10)) //10초
                {
                    ArcStateUpdate();
                    Thread.Sleep(500);
                    if (RobotState.nRmHome)
                        return true;
                }

                LogManager.WriteConsoleLog(eLogLevel.Info, "ARC EMG Home Cmd fail = {0}", sCmd);
            }
            else
            {
                return false;
            }

            return false;
        }

        protected bool ArcEMOGet(string RevCmd)
        {
            string sCmd = string.Empty;
            sCmd = string.Format("EGET{0}{1}", RevCmd, END_MARK);
            Arc_sock.Send(Encoding.ASCII.GetBytes(sCmd));
            LogManager.WriteConsoleLog(eLogLevel.Info, "ARC Robot Cmd send = {0}", sCmd);

            return CheckRMComplite();
        }

        protected bool ArcEMOPut(string RevCmd)
        {
            string sCmd = string.Empty;
            //sCmd = string.Format("EGET{0}{1}", RevCmd, END_MARK); //GET으로 잘못 들어감
            sCmd = string.Format("EPUT{0}{1}", RevCmd, END_MARK);
            Arc_sock.Send(Encoding.ASCII.GetBytes(sCmd));
            LogManager.WriteConsoleLog(eLogLevel.Info, "ARC Robot Cmd send = {0}", sCmd);
            return CheckRMComplite();
        }

        public override bool RMinitCmd()
        {
            DateTime curTime = DateTime.Now;
            // 초기화시 버튼 블링크 사용에 사용한다
            this.InitReq = true;

            if (RobotOnlineConncet != true)
            {
                if (RMConnecting(GlobalData.Current.RMSection.RM1Element.IPAddress, GlobalData.Current.RMSection.RM1Element.Port) == false)
                    return false;
            }

            //lsj SESS 홈잡기전에 리셋한번하자
            if (ArcErrorReset() != true)
            {
                return false;
            }

            if (ArcHome() == true)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public override bool RMHandGrip()   // 메뉴얼 사용
        {
            DateTime curTime = DateTime.Now;

            string sCmd = string.Empty;
            sCmd = string.Format("CLMP{0}", END_MARK);
            Arc_sock.Send(Encoding.ASCII.GetBytes(sCmd));
            LogManager.WriteConsoleLog(eLogLevel.Info, "ARC Robot Cmd send = {0}", sCmd);


            if (CheckRMComplite()) //211021 lsj 명령추가
            {
                while (!IsTimeOut(curTime, 10)) //10초
                {
                    ArcStateUpdate();
                    Thread.Sleep(500);
                    if (RobotState.nRmClampState == "1")
                        return true;
                }

                LogManager.WriteConsoleLog(eLogLevel.Info, "ARC Clamp Cmd fail = {0}", sCmd);
            }
            else
            {
                return false;
            }

            return false;
        }

        public override bool RMHandUnGrip() // 메뉴얼 사용
        {
            DateTime curTime = DateTime.Now;
            string sCmd = string.Empty;
            sCmd = string.Format("UNCL{0}", END_MARK);
            Arc_sock.Send(Encoding.ASCII.GetBytes(sCmd));
            LogManager.WriteConsoleLog(eLogLevel.Info, "ARC Robot Cmd send = {0}", sCmd);

            if (CheckRMComplite()) //211021 lsj 명령추가
            {
                while (!IsTimeOut(curTime, 10)) //10초
                {
                    ArcStateUpdate();
                    Thread.Sleep(500);
                    if (RobotState.nRmClampState == "2")
                        return true;
                }

                LogManager.WriteConsoleLog(eLogLevel.Info, "ARC UnClamp Cmd fail = {0}", sCmd);
            }
            else
            {
                return false;
            }

            return false;
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

        public override bool CheckRMBusy() //210416 lsj ARC
        {
            return RobotState.nRmBusy;
        }

        public override eRMState GetRMState() //210416 lsj ARC
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


        //21.01.19 lsj ARC 수정
        public override bool MoveStopCmd()  // 메뉴얼 사용
        {
            if (ArcStop() == true)
            {
                return ArcStateUpdate();
            }
            else
            {
                return false;
            }
        }

        public override void RMModChange(eRMMode mode)
        {
            return;
        }

        #endregion

        #region WCP

        public override bool SetWCFCommand(CraneCommand Cmd)
        {
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

            if (SimulMode)
            {
                PRobot.Transferring = this.TRANSFERRING;
                PRobot.Bank = this.BANK;
                PRobot.Bay = this.BAY;
                PRobot.Level = this.LEVEL;
                PRobot.CarrierContain = (this.Place_Sensor_Exist) ? "1" : "0";
                PRobot.Home = this.HOME;
                PRobot.EmergencyStop = this.EMERGENCYS;
                PRobot.Busy = CheckRMBusy() ? "1" : "0";      //0: IDLE   1:BUSY
                PRobot.ArmStretch = ARMSTRETCH;
                PRobot.ErrorStatus = CheckModuleAlarmExist() ? "1" : "0";      //0:NORMAL   1:ERROR
                PRobot.ErrorCode = GetModuleLastAlarmCode();      //5DIGIT CODE
                if (PRobot.ErrorCode == "88") //임시로직
                {
                    PRobot.ErrorCode = "18";
                }
                PRobot.Chuck = this.CHUCK;
            }
            else
            {
                PRobot.Transferring = (RobotState.nRmBusy) ? "1" : "0"; //211126 RGJ Transferring 보고 0 으로만 보고됨. BUSY 와 동일처리
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
            }
            return PRobot;

        }

        #endregion

        public override void CloseController()
        {
            try
            {
                Arc_sock?.Close();
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
            }
        }
        public override void RMSafetyReset(bool b)
        {
            return;
        }
        public override bool RMAlarmCheck()
        {
            //lsj SESS 주석
            //ArcStateUpdate();
            return RobotState.nRmAlarm;
        }
        public override bool RMAlarmReset()
        {
            return ArcErrorReset();
        }

        #region 소모품 관련

        public override int GetFork_CycleDistance()
        {

            return RobotState.A_MoveDistance;

        }
        public override int GetZ_CycleDistance()
        {
            return RobotState.Z_MoveDistance;
        }
        public override int GetDrive_CycleDistance()
        {
            return RobotState.Y_MoveDistance;

        }
        public override int GetTurn_CycleDistance()
        {
            return RobotState.T_MoveDistance;
        }

        #endregion
    }
}

