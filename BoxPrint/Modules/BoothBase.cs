using PLCProtocol.Base;
using PLCProtocol.DataClass;
using BoxPrint.CCLink;
using BoxPrint.Log;
using BoxPrint.Modules.Shelf;
using System;
using System.Collections.Concurrent;
using System.Threading;
using WCF_LBS.Commands;

namespace BoxPrint.Modules
{
    /// <summary>
    /// 2020.11.18 TowerLamp Command Display
    /// </summary>
    public class TowerLampProcessEventArgs : EventArgs
    {
        public bool IsSuccessful { get; set; }
        public DateTime CompletionTime { get; set; }
        public TowerLampCommand cmd { get; set; }
    }

    public abstract class BoothBase : ModuleBase
    {
        protected int BoothDoorCount = 0;
        protected int LightCurtainCount = 0;
        protected int EMS_SwitchCount = 0;

        protected readonly int BoothRunCycleTime = 100;
        protected bool ExitThread = false;
        protected bool NowOnReset = false; //211207 Reset 중 표시
        protected int ResetBlinkDuration = 5; //211207 Reset 점멸 간격
        public readonly int PLC_Timeout = 5;
        public ConcurrentDictionary<string, PLCDataItem> PLCtoPC = new ConcurrentDictionary<string, PLCDataItem>();
        public ConcurrentDictionary<string, PLCDataItem> PCtoPLC = new ConcurrentDictionary<string, PLCDataItem>();

        public bool MCSConnetionState
        {
            get;
            protected set;
        }

        protected eOnlineState _PreOnlineState = eOnlineState.Offline_EQ;
        protected eOnlineState _CurrentOnlineState = eOnlineState.Offline_EQ;

        public int PLCNumber
        {
            get;
            protected set;
        }
        public int PLCReadOffset
        {
            get;
            protected set;
        }
        public int PLCWriteOffset
        {
            get;
            protected set;
        }

        /// <summary>
        /// 실질적으로 1:1 연결이므로 TCP 연결 된 상태면 연결로 봐서 큰의미는 없음.
        /// MCS 사양에 있으므로 연결 상태 변수로 관리함.
        /// </summary>
        private eCommunicationEstablishState _MCSComEstablishState = eCommunicationEstablishState.NotEstablished; //230421 S1F13 SCS Sending 시나리오 추가됨. 
        public eCommunicationEstablishState MCSComEstablishState
        {
            get
            {
                return _MCSComEstablishState;
            }
            set
            {
                _MCSComEstablishState = value;
            }
        }
        public eOnlineState CurrentOnlineState
        {
            get
            {
                return _CurrentOnlineState;
            }
            set
            {
                if (_CurrentOnlineState == value)
                {
                    return;
                }
                _PreOnlineState = _CurrentOnlineState;
                _CurrentOnlineState = value;

                //[230503 CIM 검수] 해당 내용 삭제
                //Offline에서 Online으로 바뀌는 경우 S1F1 보고
                //if (_PreOnlineState == eOnlineState.Offline_EQ && _CurrentOnlineState != eOnlineState.Offline_EQ)
                //{
                //    GlobalData.Current.HSMS.SendMessageAsync("S1F1");
                //}


                if (_PreOnlineState == eOnlineState.Offline_EQ)
                {
                    if (_CurrentOnlineState == eOnlineState.Remote)
                    {
                        // CEID 3 (Change To Online REMOTE Mode)
                        //GlobalData.Current.HSMS.SendMessageAsync("S6F11", new Dictionary<string, object>() { { "CEID", 3 } });        //220802 조숭진 주석처리
                        GlobalData.Current.HSMS.SendS6F11(3);
                    }

                    //// Last EQPControlState 값 설정
                    //this.LastEQPControlState = value;
                    //SetLastEQPControlStateValue(value);
                }
                else if (_PreOnlineState == eOnlineState.Remote)
                {
                    if (_CurrentOnlineState == eOnlineState.Offline_EQ)
                    {
                        // CEID 1(Change To Offline Mode)
                        GlobalData.Current.HSMS.SendS6F11(1);
                        Thread.Sleep(500); //보고할 시간 확보
                        //HSMS Disconnect
                        //GlobalData.Current.HSMS.Stop(); //Disconnect 임시 해제
                        //Thread.Sleep(200);
                        //GlobalData.Current.HSMS.Start();
                    }
                }

            }
        }

        #region  소박스용 라이트커튼 싱크 로직 추가. 하나의 라이트 커튼을 복수의 컨베이어가 사용할때 뮤팅온/오프 제어를 위해 변수 추가.
        protected int LightcurtainSyncNumber = 0; //복수사용할 라이트커튼 번호
        protected bool[] LCSyncMuteArray = new bool[5]; //5개까지로 제한
        #endregion

        public event EventHandler<TowerLampProcessEventArgs> TowerLampProcessCompleted; // 2020.11.18 TowerLamp Command Display

        #region TowerLamp 및 Buzzer Property (OP/HP 따로 관리)
        protected virtual eTowerLampMode HPTowerLampGreen { get; set; }
        protected virtual eTowerLampMode HPTowerLampRed { get; set; }
        protected virtual eTowerLampMode HPTowerLampYellow { get; set; }

        protected virtual eTowerLampMode HPTowerLampBlue { get; set; }

        protected virtual eTowerLampMode HPTowerLampWhite { get; set; }
        protected virtual eBuzzerSoundType HPTowerLampBuzzer { get; set; }

        protected virtual eTowerLampMode OPTowerLampGreen { get; set; }
        protected virtual eTowerLampMode OPTowerLampRed { get; set; }
        protected virtual eTowerLampMode OPTowerLampYellow { get; set; }

        protected virtual eTowerLampMode OPTowerLampBlue { get; set; }

        protected virtual eTowerLampMode OPTowerLampWhite { get; set; }
        protected virtual eBuzzerSoundType OPTowerLampBuzzer { get; set; }
        #endregion

        //lsj SESS Door 
        public bool Port_DoorOpen
        {
            get;
            protected set;
        }

        public bool OHTIn_DoorOpen
        {
            get;
            protected set;
        }
        public bool OHTOut_DoorOpen
        {
            get;
            protected set;
        }
        public bool OHTIn_DoorOpenSolOn
        {
            get;
            protected set;
        }
        public bool OHTOut_DoorOpenSolOn
        {
            get;
            protected set;
        }

        //220517 조숭진 hsms 메세지 추가
        public eSCState _SCState = eSCState.INIT;
        public eSCState SCState
        {
            get
            {
                return _SCState;
            }
            set
            {
                if (value != _SCState)
                {
                    #region 초기 보고사양
                    if (_SCState == eSCState.INIT && value == eSCState.PAUSED) //초기 시작시 한번만 보고
                    {
                        GlobalData.Current.HSMS.SendS6F11(106, "", null); //SCPaused CEID 106 //검수 사양 반영
                        GlobalData.Current.mRMManager.FirstRM?.RMPause_Request();
                        GlobalData.Current.mRMManager.SecondRM?.RMPause_Request();
                        //GlobalData.Current.PortManager.AutoJobStopAllLine();//상태가 변할때만 명령
                        _SCState = value;
                        return;
                    }
                    #endregion

                    _SCState = value;
                    switch (_SCState)
                    {
                        case eSCState.PAUSED:
                            GlobalData.Current.mRMManager.FirstRM?.RMPause_Request();
                            GlobalData.Current.mRMManager.SecondRM?.RMPause_Request();
                            //GlobalData.Current.PortManager.AutoJobStopAllLine();//상태가 변할때만 명령
                            GlobalData.Current.HSMS.SendS6F11(105, "", null); //SCPauseCompleted CEID 105
                            break;
                        case eSCState.AUTO:
                            GlobalData.Current.mRMManager.FirstRM?.RMResume_Request();
                            GlobalData.Current.mRMManager.SecondRM?.RMResume_Request();
                            //GlobalData.Current.PortManager.AutoJobAllLine();
                            GlobalData.Current.HSMS.SendS6F11(103, "", null); //SCAutoCompleted CEID 103
                            break;
                        case eSCState.PAUSING:
                            //스케쥴러에게 정지 명령을 내린다.
                            GlobalData.Current.Scheduler.StopScheduler();
                            GlobalData.Current.HSMS.SendS6F11(107, "", null); //SCPauseInitiated CEID 107 //검수 사양 반영
                            break;
                    }
                }
                if (SCState == eSCState.AUTO)
                {
                    GlobalData.Current.Scheduler?.StartScheduler();
                }
                else
                {
                    GlobalData.Current.Scheduler?.StopScheduler();
                }
            }
        }
        protected bool TowerLampMuteOn = false;

        public string FullPath = System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);

        public ShelfItemList FrontData
        {
            get
            {
                return ShelfManager.Instance.FrontData;
            }
        }
        public ShelfItemList RearData
        {
            get
            {
                return ShelfManager.Instance.RearData;
            }
        }
        protected int StartSWCycleElapsed = 0;
        public BoothBase(string Name, bool simul)
            : base(Name, simul)
        {
            GlobalData.Current.HSMS.StateChanged += HSMS_StateChanged;
            LogManager.WriteConsoleLog(eLogLevel.Info, "BoothModule has been created. Module Name:{0}", Name);
        }
        public void InitPLCInterface(int plcnum, int ReadOffset, int WriteOffset)
        {
            PLCNumber = plcnum;
            PLCReadOffset = ReadOffset;
            PLCWriteOffset = WriteOffset;

            //220628 HHJ SCS 개선     //- PLCDataItems 개선
            //PLCtoPC = ProtocolHelper.ModulePLCItemSetter(eAreaType.PLCtoPC, PLCReadOffset);
            //PCtoPLC = ProtocolHelper.ModulePLCItemSetter(eAreaType.PCtoPLC, PLCWriteOffset);
            PLCtoPC = ProtocolHelper.GetPLCItem(eAreaType.PLCtoPC, "BOOTH", (short)plcnum, (ushort)PLCReadOffset);
            PCtoPLC = ProtocolHelper.GetPLCItem(eAreaType.PCtoPLC, "BOOTH", (short)plcnum, (ushort)PLCWriteOffset);

        }
        public virtual bool SCSPauseCommand()
        {
            SCState = eSCState.PAUSING; //중단중 상태로 전이 
            return true;
        }
        public virtual bool SCSResumeCommand()
        {
            SCState = eSCState.AUTO; //오토 상태로 전이 
            return true;
        }

        public virtual bool PausePLCAction()
        {
            return false;
        }
        public virtual bool ResumePLCAction()
        {
            return false;
        }

        private void HSMS_StateChanged(object sender, OpenHSMS.HSMSStateChangedEventArgs e)
        {
            MCSConnetionState = e.Enabled;
            if (!e.Enabled)
            {
                CurrentOnlineState = eOnlineState.Offline_EQ; //연결 끊어지면 상태 OffLine 변경.
            }

            //if (!SimulMode)
            {
                foreach (var CVItem in GlobalData.Current.PortManager.AllCVList)
                {
                    CVItem.PC_McsSelect = e.Enabled ? (short)1 : (short)0;   //상태 변경시 MCS Select 비트 반영
                }
            }
        }
        public bool threadExit = true;
        public Thread boothThread;
        public Thread LampThread;
        public Thread HeartBeatThread;  //241028 RGJ SCS 하트비트 전용 쓰레드 할당. 시스템 타임 변경시 토글안되는 현상으로 따로 전용 쓰레드만듬. 
        public Thread CollectUtilizationThread;  //241028 RGJ SCS 하트비트 전용 쓰레드 할당. 시스템 타임 변경시 토글안되는 현상으로 따로 전용 쓰레드만듬. 

        public void StartBooth()
        {
            boothThread = new Thread(new ThreadStart(BoothRun));
            boothThread.Name = "Booth Thread";
            boothThread.IsBackground = true;
            boothThread.Start();

            LampThread = new Thread(new ThreadStart(LampRun)); //211207 RGJ Booth 램프 전용 쓰레드 분리
            LampThread.IsBackground = true;
            LampThread.Start();

            HeartBeatThread = new Thread(new ThreadStart(HeartBeatRun)); //211207 RGJ Booth 램프 전용 쓰레드 분리
            HeartBeatThread.IsBackground = true;
            HeartBeatThread.Start();
        }
        protected virtual void BoothRun()
        {
            throw new NotImplementedException("BoothRun() 은 구현되지 않았습니다.");
        }

        protected virtual void HeartBeatRun()
        {
            return;
        }

        public virtual void MapChangeForExitThread()
        {

        }

        /// <summary>
        /// //211207 RGJ Booth 램프 전용 쓰레드 분리
        /// </summary>
        protected virtual void LampRun()
        {
            //LogManager.WriteConsoleLog(eLogLevel.Info, "Lamp Run Start");
            //try //-메인 루프 예외 발생시 로그 찍도록 추가.
            //{
            //    int ResetSeq = 0;
            //    bool ResetToggle = false;
            //    while (!ExitThread)
            //    {
            //        bool Start = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "SWITCH_PB_START");
            //        bool Stop = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "SWITCH_PB_STOP");
            //        bool Reset = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "SWITCH_PB_RESET");

            //        if (NowOnReset) //리셋 도중이면 리셋 램프 블링크 동작
            //        {
            //            ResetSeq++;
            //            if (ResetSeq % ResetBlinkDuration == 0)
            //            {
            //                ResetSeq = 0;
            //                ResetToggle = !ResetToggle;
            //                CCLinkManager.CCLCurrent.WriteIO(ModuleName, "LAMP_PB_RESET", ResetToggle);
            //            }
            //        }
            //        else
            //        {
            //            //램프 출력부터 제어
            //            CCLinkManager.CCLCurrent.WriteIO(ModuleName, "LAMP_PB_RESET", Reset);

            //            if (Start) //오퍼레이터의 누르는 동작이 우선.
            //            {
            //                CCLinkManager.CCLCurrent.WriteIO(ModuleName, "LAMP_PB_START", true);
            //            }
            //            else if (Stop) //오퍼레이터의 누르는 동작이 우선.
            //            {
            //                CCLinkManager.CCLCurrent.WriteIO(ModuleName, "LAMP_PB_STOP", true);
            //            }
            //            else if (CurrentState == eBoothState.AutoStart)
            //            {
            //                CCLinkManager.CCLCurrent.WriteIO(ModuleName, "LAMP_PB_START", true);
            //                CCLinkManager.CCLCurrent.WriteIO(ModuleName, "LAMP_PB_STOP", false);
            //            }
            //            else if (CurrentState != eBoothState.AutoStart)
            //            {
            //                CCLinkManager.CCLCurrent.WriteIO(ModuleName, "LAMP_PB_START", false);
            //                CCLinkManager.CCLCurrent.WriteIO(ModuleName, "LAMP_PB_STOP", true);
            //            }
            //        }
            //        Thread.Sleep(100);
            //    }
            //}
            //catch (Exception ex)
            //{
            //    LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
            //}
        }

        [Obsolete("SK 미사용 기능")]
        protected virtual void DoSCS_ResetAction()
        {
            throw new NotImplementedException("DoLBS_ResetAction 이 구현되지 않았습니다.");
        }
        protected bool SetHPTowerLampBuzzer(eTowerLampMode green, eTowerLampMode yellow, eTowerLampMode red, eTowerLampMode blue, eTowerLampMode white, eBuzzerSoundType buzzer)
        {
            try
            {
                HPTowerLampGreen = green;
                HPTowerLampRed = red;
                HPTowerLampYellow = yellow;
                HPTowerLampBlue = blue;
                HPTowerLampWhite = white;
                HPTowerLampBuzzer = buzzer;
                return true;
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
                return false;
            }

        }

        protected bool SetOPTowerLampBuzzer(eTowerLampMode green, eTowerLampMode yellow, eTowerLampMode red, eTowerLampMode blue, eTowerLampMode white, eBuzzerSoundType buzzer)
        {
            try
            {
                OPTowerLampGreen = green;
                OPTowerLampRed = yellow;
                OPTowerLampYellow = red;
                OPTowerLampBlue = blue;
                OPTowerLampWhite = white;
                OPTowerLampBuzzer = buzzer;
                return true;
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
                return false;
            }

        }

        public virtual void SetTowerLampSub(bool red)
        {
            if (!SimulMode)
            {
                //실제 I/O 를 Write
                CCLinkManager.CCLCurrent.WriteIO(ModuleName, "TOWER_LAMP_SUB_RED", red);
            }
        }


        /// <summary>
        /// // 2020.11.18 TowerLamp Command Display
        /// </summary>
        /// <param name="cmd"></param>
        public void StartProcess(TowerLampCommand cmd)
        {
            var data = new TowerLampProcessEventArgs();

            try
            {
                data.IsSuccessful = true;
                data.CompletionTime = DateTime.Now;
                data.cmd = cmd;

                DateTime.Now.ToString("ddd, dd MMM yyy HH’:’mm’:’ss ‘GMT’");
                OnTowerLampProcessCompleted(data);

                string sendMessage = string.Format("TowerLamp Commnad Green : {0} Yellow : {1} Red : {2} Buzzer : {3} MuteMode : {4}", cmd.Green, cmd.Yellow, cmd.Red, cmd.Buzzer, cmd.MuteMode);
                GlobalData.Current.SendMessageEvent = sendMessage.ToUpper();
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
            }
        }
        /// <summary>
        /// // 2020.11.18 TowerLamp Command Display
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnTowerLampProcessCompleted(TowerLampProcessEventArgs e)
        {
            TowerLampProcessCompleted?.Invoke(this, e);
        }

        //public bool SetWCFCommand(TowerLampCommand Cmd)
        //{
        //    // 2020.11.18 TowerLamp Command Display
        //    GlobalData.Current.TowerLampActiveJobList.Add(Cmd);
        //    StartProcess(Cmd);

        //    this.HPTowerLampGreen = (Cmd.Green == "1");
        //    this.HPTowerLampYellow = (Cmd.Yellow == "1");
        //    this.HPTowerLampRed = (Cmd.Red == "1");
        //    this.TowerLampMuteOn = (Cmd.MuteMode == "1");
        //    switch (Cmd.Buzzer)
        //    {
        //        case "0":
        //            this.HPTowerLampBuzzer = eBuzzerSoundType.None;
        //            break;
        //        case "1":
        //            this.HPTowerLampBuzzer = eBuzzerSoundType.Sound1;
        //            break;
        //        case "2":
        //            this.HPTowerLampBuzzer = eBuzzerSoundType.Sound2;
        //            break;
        //        default:
        //            this.HPTowerLampBuzzer = eBuzzerSoundType.None;
        //            break;
        //    }
        //    return true;
        //}

        protected virtual eSCState ProcessPanelSwitchButton()
        {
            throw new NotImplementedException("ProcessPanelSwitchButton() 는 구현되지 않았습니다.");
        }

        protected bool[] CheckEMO_Pressed()
        {
            bool[] result = new bool[EMS_SwitchCount];
            if (!SimulMode)
            {
                for (int i = 1; i <= EMS_SwitchCount; i++)
                {
                    if (i == 10) //10번 EMS 없어서 제외
                    {
                        continue;
                    }
                    result[i - 1] = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "SWITCH_EMS" + i);
                }
            }
            return result;
        }

        protected virtual bool CheckManualPortEMS()
        {
            if (SimulMode)
            {
                return false;
            }
            bool EMS = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "MANUAL_PORT_EMS");
            return EMS;
        }
        /// <summary>
        /// 라이트 커튼 체크 아진 제어기 전용
        /// Mute    LightCurtain     Result
        ///  0          0             OK
        ///  0          1             Error  
        ///  1          0             OK
        ///  1          1             OK
        /// </summary>
        /// <returns></returns>
        protected bool[] CheckLightCurtain_Detected()
        {
            bool[] result = new bool[LightCurtainCount];
            if (!SimulMode)
            {
                for (int i = 1; i <= LightCurtainCount; i++)
                {
                    bool MuteOn = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "LIGHT_CURTAIN_MUTE_" + i);
                    bool LightCutainDetected = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "LIGHT_CURTAIN_MUTE_ON_" + i);
                    result[i - 1] = !MuteOn && LightCutainDetected;
                }
            }
            return result;
        }

        protected bool CheckOHTInPortEMS()
        {
            if (SimulMode)
            {
                return false;
            }
            //else if (GlobalData.Current.CurrnetLineSite == eLineSite.Cheonan)
            //{
            //    bool EMS = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "SWITCH_EMS11");
            //    return EMS;
            //}
            else
            {
                return false;
            }
        }

        protected bool CheckOHTOutPortEMS()
        {
            if (SimulMode)
            {
                return false;
            }
            //else if (GlobalData.Current.CurrnetLineSite == eLineSite.Cheonan)
            //{
            //    bool EMS = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "SWITCH_EMS12");
            //    return EMS;
            //}
            else
            {
                return false;
            }
        }


        protected bool CheckPower_Relay()
        {
            if (!SimulMode)
            {
                bool MC_ON = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "KM12_MC_ON");
                return MC_ON;
            }
            return true;
        }

        protected virtual bool CheckInverterPower()
        {

            if (!SimulMode)
            {
                bool bManualPort = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "KM12_MC_ON");
                bool bAutoInOHTPort = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "KM34_MC_ON");
                bool bAutoOutOHTPort = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "KM56_MC_ON");
                if (bManualPort && bAutoInOHTPort && bAutoOutOHTPort) //정상 On 상태
                {
                    return true;
                }
                else
                {
                    //LogManager.WriteConsoleLog(eLogLevel.Info, "인버터 MC Off 체크");
                    //LogManager.WriteConsoleLog(eLogLevel.Info, "ManualPort MC:{0}    AutoInOHTPort MC:{1}    bAutoOutOHTPort MC:{2}", bManualPort, bAutoInOHTPort, bAutoOutOHTPort);
                    return false;
                }
            }
            return true;
        }

        protected bool CheckServoPower()
        {
            if (!SimulMode)
            {
                return CCLinkManager.CCLCurrent.ReadIO(ModuleName, "SERVO_POWER");
            }
            return true;
        }

        protected bool CheckAirSupply()
        {
            if (!SimulMode)
            {
                return CCLinkManager.CCLCurrent.ReadIO(ModuleName, "AIR_SUPPLY_CHECK");
            }
            return true;
        }

        protected bool CheckHomeDoorOpen()
        {
            if (!SimulMode)
            {
                return CCLinkManager.CCLCurrent.ReadIO(ModuleName, "DOOR_OPENED_2");
            }
            return false;
        }
        protected bool CheckHomeOPDoorOpen()
        {
            if (!SimulMode)
            {
                return CCLinkManager.CCLCurrent.ReadIO(ModuleName, "DOOR_OPENED_1");
            }
            return false;
        }
        protected bool CheckMaintDoorOpen()
        {
            return false;
        }
        protected bool CheckRMContorlBoxOpen() //Off 정상
        {
            bool result = true;
            foreach (var rItem in GlobalData.Current.mRMManager.ModuleList)
            {
                if (rItem.Value.RobotOnlineConncet == true) //로봇 열결 안되어 있으면 체크 불가능.
                {
                    result = rItem.Value.CheckRMBoxOpen();
                    if (result)
                    {
                        return true; //박스가 열렸다.
                    }
                }
            }
            return false;
        }
        protected bool CheckRMMCOn() //On 정상
        {
            bool result = false;
            foreach (var rItem in GlobalData.Current.mRMManager.ModuleList)
            {
                result = rItem.Value.CheckRM_MC_On();
                if (!result)
                {
                    return false;
                }
            }
            return true;
        }
        /// <summary>
        /// 부스 Door Open 상태 체크
        /// 0:CLOSE    1:HP OPEN   2:OP OPEN   3:BOTH OPEN
        /// </summary>
        /// <returns></returns>
        public virtual bool GetBoothDoorOpenState()
        {
            throw new NotImplementedException("GetBoothDoorOpenState() 는 구현되지 않았습니다.");
            //int Result = 0;
            //bool HomeDoor = CheckHomeDoorOpen();
            //bool HomeOPDoor = CheckHomeOPDoorOpen();
            //bool MaintDoor = CheckMaintDoorOpen();
            ////사양서상 Main 도어는 없지만 Home Door랑 같이 취급한다.
            //HomeDoor = HomeDoor | MaintDoor;
            //if (HomeDoor)
            //{
            //    Result += 1;
            //}
            //if (HomeOPDoor)
            //{
            //    Result += 2;
            //}
            //return Result.ToString();
        }

        public virtual bool GetDbConnectState() { return false; }

        //lsj SESS Door
        public bool GetPortDoorOpenState()
        {
            if (SimulMode)
            {
                return false;
            }
            foreach (var Lineitem in GlobalData.Current.PortManager.ModuleList) //컨베이어 라인별 순회
            {
                foreach (var CVitem in Lineitem.Value.ModuleList)
                {
                    if (CVitem.UseDoor)
                    {
                        if (CCLinkManager.CCLCurrent.ReadIO(ModuleName, String.Format("CV_DOOR_OPEN_CHECK_{0}", CVitem.DoorNumber)))
                        {
                            return true;
                        }
                    }

                }
            }

            return false;

            //bool Door1 = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "CV_DOOR_OPEN_CHECK_1");
            //bool Door2 = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "CV_DOOR_OPEN_CHECK_2");
            //bool Door3 = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "CV_DOOR_OPEN_CHECK_3");
            //bool Door4 = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "CV_DOOR_OPEN_CHECK_4");
            //bool Door5 = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "CV_DOOR_OPEN_CHECK_5");
            //bool Door6 = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "CV_DOOR_OPEN_CHECK_6");
            //return Door1 || Door2 || Door3 || Door4 || Door5 || Door6; //하나라도 열려 있으면 열림으로 간주
        }

        public bool GetOHTInPortDoorOpenState()
        {
            if (SimulMode)
            {
                return false;
            }
            bool Door1 = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "OHT_IN_DOOR_OPENED_1");
            bool Door2 = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "OHT_IN_DOOR_OPENED_2");
            bool Door3 = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "OHT_IN_DOOR_OPENED_3");
            bool Door4 = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "OHT_IN_DOOR_OPENED_4");
            bool Door5 = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "OHT_IN_DOOR_OPENED_5");
            bool Door6 = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "OHT_IN_DOOR_OPENED_6");
            //bool Door7 = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "OHT_IN_DOOR_OPENED_7");
            return Door1 || Door2 || Door3 || Door4 || Door5 || Door6; //하나라도 열려 있으면 열림으로 간주
        }
        public bool GetOHTOutPortDoorOpenState()
        {
            if (SimulMode)
            {
                return false;
            }
            bool Door1 = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "OHT_OUT_DOOR_OPENED_1");
            bool Door2 = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "OHT_OUT_DOOR_OPENED_2");
            bool Door3 = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "OHT_OUT_DOOR_OPENED_3");
            bool Door4 = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "OHT_OUT_DOOR_OPENED_4");
            bool Door5 = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "OHT_OUT_DOOR_OPENED_5");
            bool Door6 = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "OHT_OUT_DOOR_OPENED_6");
            //bool Door7 = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "OHT_OUT_DOOR_OPENED_7");
            return Door1 || Door2 || Door3 || Door4 || Door5 || Door6; //하나라도 열려 있으면 열림으로 간주
        }

        public bool GetOHTOutPortDoorUnLockState()
        {
            if (!SimulMode)
            {
                bool bDoorUnlock = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "DOOR_OPEN_SOL_OHT_OUT");
                return bDoorUnlock;
            }
            return false;
        }
        public bool GetOHTInPortDoorUnLockState()
        {
            if (!SimulMode)
            {
                bool bDoorUnlock = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "DOOR_OPEN_SOL_OHT_IN");
                return bDoorUnlock;
            }
            return false;
        }

        public bool CheckEMSSwitch()
        {
            bool EMS = false;
            for (int i = 1; i <= EMS_SwitchCount; i++) //EMS 스위치가 하나라도 On 되면 EMS 상태.
            {
                if (i == 10) //10번 EMS 없어서 제외
                {
                    continue;
                }
                EMS = (EMS | CCLinkManager.CCLCurrent.ReadIO(ModuleName, "SWITCH_EMS" + i));
            }
            return EMS;
        }

        public bool CheckLightCurtain()
        {
            if (SimulMode)
                return false;
            return false; //추후 라이트커튼 유무에 따라 추가 필요
        }


        public bool CheckCPSAbnormal()
        {
            //if (!GlobalData.Current.WPS_mgr.UseWPS) //WPS 모듈 사용 안하는 경우
            //{
            //    return false;
            //}
            if (SimulMode)
            {
                return false;
            }
            else
            {

                bool CPS1Run = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "CPS1_IF_RUN");
                bool CPS2Run = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "CPS2_IF_RUN");
                bool CPS1Fault = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "CPS1_IF_FAULT");
                bool CPS2Fault = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "CPS2_IF_FAULT");

                bool RunCheck = CPS1Run || CPS2Run;

                bool AllFault = CPS1Fault && CPS2Fault;

                bool Abnormal = !RunCheck || AllFault;


                return Abnormal;
                //return !CPS1Run || CPS1Fault;
            }
        }

        public bool SetDoorUnLock(bool OnOff)
        {
            if (!SimulMode)
            {
                CCLinkManager.CCLCurrent.WriteIO(ModuleName, "DOOR_OPEN_SOL_BOOTH", OnOff);
                SetAllLightCurtainMute(OnOff);//도어 오픈시 라이트 커튼 뮤트 On
            }
            return true;
        }

        public virtual bool GetDoorUnLockState()
        {
            if (!SimulMode)
            {
                bool bDoorUnlock = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "DOOR_OPEN_SOL_BOOTH");
                return bDoorUnlock;
            }
            return false;
        }

        public void SetSafetyRelayReset()
        {
            if (!SimulMode)
            {
                CCLinkManager.CCLCurrent.WriteIO(ModuleName, "SAFETY_RESET", true); //세이프티 릴레이 On,Off     
                Thread.Sleep(500);
                CCLinkManager.CCLCurrent.WriteIO(ModuleName, "SAFETY_RESET", false); //세이프티 릴레이 On,Off 


            }
        }


        public bool GetBoothAutoMode()
        {
            if (SimulMode)
                return true;
            bool AutoSel = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "SWITCH_SS_AUTO");
            bool ManualSel = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "SWITCH_SS_MANUAL");
            return AutoSel && !ManualSel;
        }
        public bool GetBoothAutoStarted()
        {
            if (SimulMode)
                return true;
            bool AutoSel = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "SWITCH_SS_AUTO");
            bool ManualSel = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "SWITCH_SS_AUTO");
            return AutoSel && !ManualSel;
        }
        public bool SetBoothLight(bool OnOff)
        {
            if (!SimulMode)
            {
                CCLinkManager.CCLCurrent.WriteIO(ModuleName, "BOOTH_LIGHT", OnOff);
            }
            return true;
        }

        /// <summary>
        /// 라이트커튼 뮤트 신호를 제어한다.
        /// </summary>
        /// <param name="OnOff"></param>
        /// <returns></returns>
        public virtual bool SetLightCurtainMute(int LC_Number, bool OnOff, int SyncSeq = 0)
        {
            if (1 <= LC_Number && LC_Number <= LightCurtainCount)
            {
                if (SimulMode)
                {
                    return true;
                }
                else
                {
                    //ON 시킬때는 먼저 Off후 다시 On.
                    if (OnOff)
                    {
                        CCLinkManager.CCLCurrent.WriteIO(ModuleName, "LIGHT_CURTAIN_MUTE_A_" + LC_Number, false);
                        CCLinkManager.CCLCurrent.WriteIO(ModuleName, "LIGHT_CURTAIN_MUTE_B_" + LC_Number, false);
                        Thread.Sleep(50);
                        CCLinkManager.CCLCurrent.WriteIO(ModuleName, "LIGHT_CURTAIN_MUTE_A_" + LC_Number, OnOff);
                        Thread.Sleep(200); //일정시간 대기가 필요
                        CCLinkManager.CCLCurrent.WriteIO(ModuleName, "LIGHT_CURTAIN_MUTE_B_" + LC_Number, OnOff);
                    }
                    else //Off 할때는 딜레이 필요없음.
                    {
                        CCLinkManager.CCLCurrent.WriteIO(ModuleName, "LIGHT_CURTAIN_MUTE_A_" + LC_Number, false);
                        CCLinkManager.CCLCurrent.WriteIO(ModuleName, "LIGHT_CURTAIN_MUTE_B_" + LC_Number, false);
                    }

                    Thread.Sleep(100);
                    bool MuteOnState = GetLightCurtainMuteState(LC_Number);
                    //Lamp I/O 도 출력한다.
                    CCLinkManager.CCLCurrent.WriteIO(ModuleName, "LIGHT_CURTAIN_MUTE_LAMP_" + LC_Number, MuteOnState);
                    return MuteOnState;
                }
            }
            else
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "SetLightCurtainMute  {0}은 잘못된 인자 값입니다.", LC_Number);
                return false;
            }
        }

        /// <summary>
        /// 해당 라이트커튼 뮤트 상태를 조회
        /// </summary
        /// <returns>        
        /// true  : 라이트커튼 뮤트 상태
        /// false : 라이트커튼 인터락 동작 상태
        /// </returns>
        public virtual bool GetLightCurtainMuteState(int LC_Number)
        {
            if (1 <= LC_Number && LC_Number <= LightCurtainCount)
            {
                if (SimulMode)
                {
                    return true;
                }
                else
                {
                    bool mute = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "LIGHT_CURTAIN_MUTE_ON_" + LC_Number);
                    return mute;
                }
            }
            else
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "GetLightCurtainMuteState  {0}은 잘못된 인자 값입니다.", LC_Number);
                return false;
            }
        }

        public void SetInnerLampControl(bool OnOff)
        {
            if (SimulMode)
                return;
            else
                CCLinkManager.CCLCurrent.WriteIO(ModuleName, "DOOR_UNLOCK_LAMP", OnOff);

        }

        public void SetAllLightCurtainMute(bool MuteOn)
        {
            if (SimulMode)
            {
                return;
            }
            for (int i = 1; i <= LightCurtainCount; i++)
            {
                SetLightCurtainMute(i, MuteOn);
            }

        }


        public void SetLightCurtainCounter(int counter)
        {
            //이미 설정되어있으면 스킵
            if (LightCurtainCount > 0)
            {
                return;
            }
            else
            {
                this.LightCurtainCount = counter;
            }

        }
        public void SetLightCurtainSync(int number)
        {
            //이미 설정되어있으면 스킵
            if (LightcurtainSyncNumber > 0)
            {
                return;
            }
            else
            {
                this.LightcurtainSyncNumber = number;
            }

        }
        public void SetEMSCounter(int counter)
        {
            //이미 설정되어있으면 스킵
            if (EMS_SwitchCount > 0)
            {
                return;
            }
            else
            {
                this.EMS_SwitchCount = counter;
            }
        }

        public void CloseBooth()
        {

            ExitThread = true;
            Thread.Sleep(100);
            if (!SimulMode)
            {
                //CCLinkManager.CCLCurrent.WriteIO(ModuleName, "LAMP_PB_START", false);
                //CCLinkManager.CCLCurrent.WriteIO(ModuleName, "LAMP_PB_STOP",  false);
                //CCLinkManager.CCLCurrent.WriteIO(ModuleName, "LAMP_PB_RESET", false);
            }
        }

        public virtual bool SyncPLC_Time()
        {
            throw new NotImplementedException("SyncPLC_Time() 는 구현되지 않았습니다.");
        }
        public virtual void SetPLCRMReportComplete(bool FirstRM, bool Value)
        {
            throw new NotImplementedException("SetPLCRMReportComplete() 는 구현되지 않았습니다.");
        }
    }
}
