using BoxPrint.Alarm;
using BoxPrint.DataList;
using BoxPrint.Log;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BoxPrint.Modules
{
    // 2021.07.12 RGJ
    //- Booth 모듈 세분화(PMAC, ARC, PLC)
    public class Booth_SKSCS : BoothBase
    {
        //220315 HHJ SCS 개발     //- MXComponentNet, SharedMemory 개선
        //HeartBeat 발신용 변수 추가.
        private DateTime SCSHeartBeatToggleTime = DateTime.Now;
        private int PLCHeartBeatTimeOut = 20; //10sec 사양없어서 나이브하게 10초로함 -> 임시로 20초로 변경


        private bool HeartBeat_SCS = true; //이전 하트 비트 상태저장

        private bool HeartBeat_PLC = true; //PLC 최근 하트 비트 상태저장
        Stopwatch HeartBeatTimeWatch = Stopwatch.StartNew();


        #region TowerLamp 및 Buzzer Property (OP/HP 따로 관리)
        protected eTowerLampMode _HPTowerLampGreen = eTowerLampMode.OFF;
        protected eTowerLampMode _HPTowerLampYellow = eTowerLampMode.OFF;
        protected eTowerLampMode _HPTowerLampRed = eTowerLampMode.OFF;
        protected eTowerLampMode _HPTowerLampBlue = eTowerLampMode.OFF;
        protected eTowerLampMode _HPTowerLampWhite = eTowerLampMode.OFF;

        protected eTowerLampMode _OPTowerLampGreen = eTowerLampMode.OFF;
        protected eTowerLampMode _OPTowerLampYellow = eTowerLampMode.OFF;
        protected eTowerLampMode _OPTowerLampRed = eTowerLampMode.OFF;
        protected eTowerLampMode _OPTowerLampBlue = eTowerLampMode.OFF;
        protected eTowerLampMode _OPTowerLampWhite = eTowerLampMode.OFF;
        protected eBuzzerSoundType _HPTowerLampBuzzer = eBuzzerSoundType.None;
        protected eBuzzerSoundType _OPTowerLampBuzzer = eBuzzerSoundType.None;

        protected override eTowerLampMode HPTowerLampGreen
        {
            get
            {
                return _HPTowerLampGreen; //상태 변수 보관
            }
            set
            {
                if (_HPTowerLampGreen != value)
                {
                    _HPTowerLampGreen = value;
                    if (!SimulMode)
                    {
                        PC_TowerLamp_HPGreen = value;//PLC Write
                    }
                }
            }
        }
        protected override eTowerLampMode HPTowerLampRed
        {
            get
            {
                return _HPTowerLampRed; //상태 변수 보관
            }
            set
            {
                if (_HPTowerLampRed != value)
                {
                    _HPTowerLampRed = value;
                    if (!SimulMode)
                    {
                        PC_TowerLamp_HPRed = value;//PLC Write
                    }
                }
            }
        }
        protected override eTowerLampMode HPTowerLampYellow
        {
            get
            {
                return _HPTowerLampYellow; //상태 변수 보관
            }
            set
            {
                if (_HPTowerLampYellow != value)
                {
                    _HPTowerLampYellow = value;
                    if (!SimulMode)
                    {
                        PC_TowerLamp_HPYellow = value;//PLC Write
                    }
                }
            }
        }

        protected override eTowerLampMode HPTowerLampBlue
        {
            get
            {
                return _HPTowerLampBlue; //상태 변수 보관
            }
            set
            {
                if (_HPTowerLampBlue != value)
                {
                    _HPTowerLampBlue = value;
                    if (!SimulMode)
                    {
                        PC_TowerLamp_HPBlue = value;//PLC Write
                    }
                }
            }
        }

        protected override eTowerLampMode HPTowerLampWhite
        {
            get
            {
                return _HPTowerLampWhite; //상태 변수 보관
            }
            set
            {
                if (_HPTowerLampWhite != value)
                {
                    _HPTowerLampWhite = value;
                    if (!SimulMode)
                    {
                        PC_TowerLamp_HPWhite = value;//PLC Write
                    }
                }
            }
        }

        protected override eTowerLampMode OPTowerLampGreen
        {
            get
            {
                return _OPTowerLampGreen; //상태 변수 보관
            }
            set
            {
                if (_OPTowerLampGreen != value)
                {
                    _OPTowerLampGreen = value;
                    if (!SimulMode)
                    {
                        PC_TowerLamp_OPGreen = value;//PLC Write
                    }
                }
            }
        }
        protected override eTowerLampMode OPTowerLampRed
        {
            get
            {
                return _OPTowerLampRed; //상태 변수 보관
            }
            set
            {
                if (_OPTowerLampRed != value)
                {
                    _OPTowerLampRed = value;
                    if (!SimulMode)
                    {
                        PC_TowerLamp_OPRed = value;//PLC Write
                    }
                }
            }
        }
        protected override eTowerLampMode OPTowerLampYellow
        {
            get
            {
                return _OPTowerLampYellow; //상태 변수 보관
            }
            set
            {
                if (_OPTowerLampYellow != value)
                {
                    _OPTowerLampYellow = value;
                    if (!SimulMode)
                    {
                        PC_TowerLamp_OPYellow = value;//PLC Write
                    }
                }
            }
        }

        protected override eTowerLampMode OPTowerLampBlue
        {
            get
            {
                return _OPTowerLampBlue; //상태 변수 보관
            }
            set
            {
                if (_OPTowerLampBlue != value)
                {
                    _OPTowerLampBlue = value;
                    if (!SimulMode)
                    {
                        PC_TowerLamp_OPBlue = value;//PLC Write
                    }
                }
            }
        }

        protected override eTowerLampMode OPTowerLampWhite
        {
            get
            {
                return _OPTowerLampWhite; //상태 변수 보관
            }
            set
            {
                if (_OPTowerLampWhite != value)
                {
                    _OPTowerLampWhite = value;
                    if (!SimulMode)
                    {
                        PC_TowerLamp_OPWhite = value;//PLC Write
                    }
                }
            }
        }

        protected override eBuzzerSoundType HPTowerLampBuzzer
        {
            get
            {
                return _HPTowerLampBuzzer; //상태 변수 보관
            }
            set
            {
                if (_HPTowerLampBuzzer != value)
                {
                    _HPTowerLampBuzzer = value;
                    if (!SimulMode)
                    {
                        PC_BuzzerHP = value;//PLC Write
                    }
                }
            }
        }
        protected override eBuzzerSoundType OPTowerLampBuzzer
        {
            get
            {
                return _OPTowerLampBuzzer; //상태 변수 보관
            }
            set
            {
                if (_OPTowerLampBuzzer != value)
                {
                    _OPTowerLampBuzzer = value;
                    if (!SimulMode)
                    {
                        PC_BuzzerOP = value;//PLC Write
                    }
                }
            }
        }
        #endregion

        public Booth_SKSCS(string Name, bool simul)
            : base(Name, simul)
        {
            LogManager.WriteConsoleLog(eLogLevel.Info, "Booth Module : Booth_SKSCS Created!");
        }

        public override void MapChangeForExitThread()
        {
            threadExit = false;
        }

        protected override void BoothRun()
        {
            LogManager.WriteConsoleLog(eLogLevel.Info, "Booth Run Start");
            GlobalData.Current.MRE_GlobalDataCreatedEvent.WaitOne(); //모든 모듈 생성전까지 Run 대기
            GlobalData.Current.MRE_MapViewChangeEvent.WaitOne();
            //조숭진 booth io는 다른 unit io처럼 껏다켯다 할 수 없어 초기화 시 일단 끄고 시작한다.
            PC_PauseReq = false;
            PC_ResumeReq = false;
            PC_TimeSyncReq = false;

            while (threadExit)
            {
                try
                {
                    //if (GlobalData.Current.MapViewStart)
                    //{
                    //    boothThread.Join();
                    //}
                    //ToggleHeartbeat(); //정해진 딜레이 도달시 하트비트 토글 //241028 RGJ SCS 하트비트 전용 쓰레드 할당

                    CheckPLCHeartBeat(); //PLC 하트비트 체크해서 응답없으면 에러발생

                    //PLC단 Resume,Pause Req 대응
                    if (CheckPLCPauseReq())
                    {
                        PLCPauseReqAction();
                    }
                    else if (CheckPLCResumeReq())
                    {
                        PLCResumeReqAction();
                    }


                    bool bAbnormal = DoAbnormalCheck();//부스 상태 체크해서 알람 발생까지
                    if (bAbnormal)
                    {
                        if (SCState == eSCState.AUTO)
                        {
                            SCState = eSCState.PAUSED;
                        }
                    }
                    if (SCState == eSCState.PAUSING)
                    {
                        if (GlobalData.Current.Scheduler.CheckSchedulerPaused())
                        {
                            SCState = eSCState.PAUSED;
                        }
                    }
                    //여기서 타워램프 상태처리
                    //Auto 상태이고 진행중인 작업이 있을경우 녹색 점등
                    //Auto 상태이고 진행중인 작업이 없을경우 녹+흰 점등
                    //Pause 상태일경우 녹색 점멸
                    //Maint (도어 오픈)? 상태일경우 황색 점등
                    //중 알람 에러상태 일경우 적색 점등 + 장음 부저
                    //경 알람 에러상태 일경우 청색 점등 + 단음 부저

                    if (GetBoothDoorOpenState()) //MAINTAIN
                    {
                        SetHPTowerLampBuzzer(eTowerLampMode.OFF, eTowerLampMode.ON, eTowerLampMode.OFF, eTowerLampMode.OFF, eTowerLampMode.OFF, eBuzzerSoundType.None);
                    }
                    else if (GlobalData.Current.Alarm_Manager.CheckHeavyAlarmExist()) //DOWN 
                    {
                        SetHPTowerLampBuzzer(eTowerLampMode.OFF, eTowerLampMode.OFF, eTowerLampMode.ON, eTowerLampMode.OFF, eTowerLampMode.OFF, eBuzzerSoundType.Sound1);
                    }
                    else if (GlobalData.Current.Alarm_Manager.GetActiveAlarmCount() > 0) //WARNING
                    {
                        SetHPTowerLampBuzzer(eTowerLampMode.OFF, eTowerLampMode.OFF, eTowerLampMode.OFF, eTowerLampMode.ON, eTowerLampMode.OFF, eBuzzerSoundType.Sound2);
                    }
                    else if (SCState == eSCState.PAUSED || SCState == eSCState.PAUSING) //STOP
                    {
                        SetHPTowerLampBuzzer(eTowerLampMode.OFF, eTowerLampMode.OFF, eTowerLampMode.Blink, eTowerLampMode.OFF, eTowerLampMode.OFF, eBuzzerSoundType.None);
                    }
                    else if (SCState == eSCState.AUTO && !GlobalData.Current.Scheduler.IsRM1JobProcessing && !GlobalData.Current.Scheduler.IsRM2JobProcessing) //IDLE
                    {
                        SetHPTowerLampBuzzer(eTowerLampMode.ON, eTowerLampMode.OFF, eTowerLampMode.OFF, eTowerLampMode.OFF, eTowerLampMode.ON, eBuzzerSoundType.None);
                    }
                    else if (SCState == eSCState.AUTO && (GlobalData.Current.Scheduler.IsRM1JobProcessing || GlobalData.Current.Scheduler.IsRM2JobProcessing)) //RUN
                    {
                        SetHPTowerLampBuzzer(eTowerLampMode.ON, eTowerLampMode.OFF, eTowerLampMode.OFF, eTowerLampMode.OFF, eTowerLampMode.OFF, eBuzzerSoundType.None);
                    }

                    Thread.Sleep(BoothRunCycleTime);
                }
                catch (Exception ex)
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
                }
            }
            LogManager.WriteConsoleLog(eLogLevel.Info, "Booth Run End");
        }

        /// <summary>
        /// //241028 RGJ SCS 하트비트 전용 쓰레드 할당
        /// 시스템 시간 변경시 설정시간에 따라 토글 안되는 현상있어서 따로 전용 쓰레드 할당.
        /// </summary>
        protected override void HeartBeatRun()
        {
            LogManager.WriteConsoleLog(eLogLevel.Info, "HeartBeatRun Created");
            GlobalData.Current.MRE_GlobalDataCreatedEvent.WaitOne(); //모든 모듈 생성전까지 Run 대기
            GlobalData.Current.MRE_MapViewChangeEvent.WaitOne();
            LogManager.WriteConsoleLog(eLogLevel.Info, "HeartBeatRun Start!");
            while (true)
            {
                try
                {
                    HeartBeat_SCS = !HeartBeat_SCS; //Toggle
                    PC_HeartBeat = HeartBeat_SCS ? (short)1 : (short)0; //PLC Write 
                }
                catch (Exception ex)
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
                }
                Thread.Sleep(1000);
            }
        }

        private void CheckPLCHeartBeat()
        {
            bool ReadHeartBeat = PLC_HeartBeat == 1 ? true : false; //PLC 에서 하트비트 획득
            if (HeartBeat_PLC != ReadHeartBeat) //기존값과 비교해서 토글되었는지 체크
            {
                HeartBeat_PLC = ReadHeartBeat;
                HeartBeatTimeWatch.Restart();
                //2024.07.03 lim, 경알람 클리어를 잘 안함 알람 시간 길어짐. 다시 들어오면 클리어 필요
                AlarmData aData = GlobalData.Current.Alarm_Manager.GetActiveList().Where(a => a.AlarmName == "PLC_HEARTBEAT_STOP").FirstOrDefault();
                if (aData != null)
                    GlobalData.Current.Alarm_Manager.AlarmClear(aData);
            }

            if (GlobalData.Current.ServerClientType == eServerClientType.Server && IsTimeout_SW(HeartBeatTimeWatch, PLCHeartBeatTimeOut))
            {
                GlobalData.Current.Alarm_Manager.AlarmOccurbyName("PLC_HEARTBEAT_STOP", ModuleName);
            }
        }

        /// <summary>
        /// 부스 Door Open 상태 체크
        /// </summary>
        /// <returns></returns>
        public override bool GetBoothDoorOpenState()
        {
            if (SimulMode)
            {
                return false;
            }
            else
            {
                bool bDoorOpened = PLC_DoorOpenState;
                return bDoorOpened;
            }
        }

        public override bool GetDbConnectState()
        {
            return GlobalData.Current.DBManager.IsConnect;
        }

        public override bool DoAbnormalCheck()
        {
            //도어 상태 체크
            if (GetBoothDoorOpenState())
            {
                //PC_InterlockRelease = 1;
                GlobalData.Current.Alarm_Manager.AlarmOccurbyName("BOOTH_DOOR_OPEN", ModuleName);
            }
            //else
            //{
            //    PC_InterlockRelease = 0;
            //}

            if (!GetDbConnectState())
            {
                return true;
            }
            //EMS 버튼 체크
            //var EMSResult = CheckEMO_Pressed();
            //for (int i = 0; i < EMSResult.Length; i++)
            //{
            //    if (EMSResult[i])
            //    {
            //        string alarmName = string.Format("EMS_E{0:D2}_PRESSED", i + 1);
            //        GlobalData.Current.Alarm_Manager.AlarmOccurbyName(alarmName, this.ModuleName);
            //        GlobalData.Current.PortManager.EmergencyManual_PortStop();
            //        GlobalData.Current.PortManager.EmergencyOHTOut_PortStop();
            //        GlobalData.Current.PortManager.EmergencyOHTIn_PortStop();
            //    }
            //}
            ////메뉴얼 포트 EMS 버튼 체크
            //if (CheckManualPortEMS()) //ON ERROR
            //{
            //    GlobalData.Current.PortManager.EmergencyManual_PortStop();
            //    //GlobalData.Current.Alarm_Manager.AlarmOccurbyName("EMS_E10_PRESSED", this.ModuleName);//메뉴얼 포트는 보고 제외
            //}

            ////OHT IN 포트 EMS 버튼 체크
            //if (CheckOHTInPortEMS()) //ON ERROR
            //{
            //    GlobalData.Current.PortManager.EmergencyOHTIn_PortStop();
            //}

            ////OHT OUT 포트 EMS 버튼 체크
            //if (CheckOHTOutPortEMS()) //ON ERROR
            //{
            //    GlobalData.Current.PortManager.EmergencyOHTOut_PortStop();

            //}

            ////도어 상태 체크
            //if (CheckHomeDoorOpen()) //ON ERROR
            //{
            //    GlobalData.Current.Alarm_Manager.AlarmOccurbyName("HOME_DOOR_OPEN", this.ModuleName);
            //    //LogManager.WriteConsoleLog(eLogLevel.Error, "HOME_DOOR_OPEN");
            //}
            //if (CheckHomeOPDoorOpen()) //ON ERROR
            //{
            //    GlobalData.Current.Alarm_Manager.AlarmOccurbyName("HOME_OPPOSITE_DOOR_OPEN", this.ModuleName);
            //    //LogManager.WriteConsoleLog(eLogLevel.Error, "HOME_OPPOSITE_DOOR_OPEN");
            //}
            //if (CheckMaintDoorOpen()) //ON ERROR
            //{
            //    GlobalData.Current.Alarm_Manager.AlarmOccurbyName("MAINTENANCE_DOOR_OPEN", this.ModuleName);
            //    //LogManager.WriteConsoleLog(eLogLevel.Error, "MAINTENANCE_DOOR_OPEN");
            //}
            ////OHT Door 체크 
            //OHTIn_DoorOpen = GetOHTInPortDoorOpenState();
            //if (OHTIn_DoorOpen)
            //{
            //    GlobalData.Current.Alarm_Manager.AlarmOccurbyName("OHT_DOOR_OPEN", this.ModuleName);
            //}
            //OHTOut_DoorOpen = GetOHTOutPortDoorOpenState();
            //if (OHTOut_DoorOpen)
            //{
            //    GlobalData.Current.Alarm_Manager.AlarmOccurbyName("OHT_DOOR_OPEN", this.ModuleName);
            //}
            //OHTIn_DoorOpenSolOn = GetOHTInPortDoorUnLockState();
            //if (OHTIn_DoorOpenSolOn)
            //{
            //    GlobalData.Current.Alarm_Manager.AlarmOccurbyName("OHT_DOOR_UNLOCK", this.ModuleName);
            //}
            //OHTOut_DoorOpenSolOn = GetOHTOutPortDoorUnLockState();
            //if (OHTOut_DoorOpenSolOn)
            //{
            //    GlobalData.Current.Alarm_Manager.AlarmOccurbyName("OHT_DOOR_UNLOCK", this.ModuleName);
            //}

            ////전원 릴레이
            //if (!CheckPower_Relay()) //OFF ERROR
            //{
            //    GlobalData.Current.Alarm_Manager.AlarmOccurbyName("POWER_RELAY_DOWN", this.ModuleName);
            //    //LogManager.WriteConsoleLog(eLogLevel.Error, "POWER_RELAY_DOWN");
            //}
            ////인버터 전원
            //if (!CheckInverterPower()) //OFF ERROR
            //{
            //    GlobalData.Current.Alarm_Manager.AlarmOccurbyName("INVERTER_POWER_DOWN", this.ModuleName);
            //    //LogManager.WriteConsoleLog(eLogLevel.Error, "INVERTER_POWER_DOWN");
            //}
            ////연기 감지
            //if (CheckSmokeDetect()) //ON ERROR
            //{
            //    GlobalData.Current.Alarm_Manager.AlarmOccurbyName("SMOKE_DETECTED", this.ModuleName);
            //    //LogManager.WriteConsoleLog(eLogLevel.Error, "SMOKE_DETECTED");
            //}

            ////공압 상태 체크
            //if (!CheckAirSupply()) //OFF ERROR
            //{
            //    GlobalData.Current.Alarm_Manager.AlarmOccurbyName("AIR_PRESSURE_DEGRADATION", this.ModuleName);
            //    //LogManager.WriteConsoleLog(eLogLevel.Error, "AIR_PRESSURE_DEGRADATION");
            //}


            //bool AlarmOccurred = GlobalData.Current.Alarm_Manager.CheckHeavyAlarmExist(); //다른 모듈 알람도 알람 발생으로 본다.
            bool AlarmOccurred = false;
            return AlarmOccurred;
        }

        protected override eSCState ProcessPanelSwitchButton()
        {
            if (SimulMode)
            {
                return SCState;
            }
            else
            {
                return eSCState.PAUSED;
            }
            //Auto,Manual 스위치 체크
            //Start,Stop 버튼 체크
            //bool AutoSW = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "SWITCH_SS_AUTO");
            //bool ManualSW = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "SWITCH_SS_MANUAL");
            //bool Start = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "SWITCH_PB_START");
            //bool Stop = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "SWITCH_PB_STOP");
            //bool Unlock = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "DOOR_UNLOCK_S/W");

            //if (ManualSW)//메뉴얼
            //{
            //    if (Unlock)
            //    {
            //        //언락으로 키 돌리면 Door Open Sol On
            //        if (!GetDoorUnLockState()) //잠김 상태면 Unlock
            //        {
            //            Thread.Sleep(200);
            //            bool UnlockEnable = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "DOOR_UNLOCK_ENABLE");

            //            if (UnlockEnable)
            //            {
            //                SetDoorUnLock(true);
            //                var SafetySpeed = GlobalData.Current.mRMManager["RM1"].SafetyUnlockSpeed;
            //                GlobalData.Current.mRMManager["RM1"].MoveSpeed = SafetySpeed; //위험대비 문 오프시 속도 낮춤.
            //                GlobalData.Current.mRMManager["RM1"].SetAutoSpeedInit(SafetySpeed);

            //            }
            //        }
            //        SetInnerLampControl(true);
            //    }
            //    else
            //    {
            //        //메뉴얼로 키 돌리면 Door Open Sol Off
            //        if (GetDoorUnLockState()) //열림 상태면 Lock
            //        {
            //            SetDoorUnLock(false);
            //        }
            //        SetInnerLampControl(false);
            //    }
            //    return SCS_SimulState/p
            //}

            //if (AutoSW && !ManualSW)
            //{
            //    if (!Unlock)
            //    {
            //        if (GetDoorUnLockState()) //도어락 열린 상태면 Lock
            //        {
            //            SetDoorUnLock(false);
            //            SetInnerLampControl(false);
            //        }
            //    }

            //    if (Stop) //스탑은 바로 반영
            //    {
            //        StartSWCycleElapsed = 0;
            //        return eBoothState.Manual;
            //    }
            //    else if (Start)
            //    {
            //        //Start 버튼은 3초 이상 눌려야 변경한다.
            //        StartSWCycleElapsed++;
            //        if (StartSWCycleElapsed > 3) //30 -> 3 로 확인 사이클 변경
            //        {
            //            StartSWCycleElapsed = 0;
            //            if (CurrentState == eBoothState.Manual) //현재 에러 상태가 아니고 메뉴얼일때 오토로 변경
            //            {
            //                var autoSpeed = GlobalData.Current.mRMManager["RM1"].AutoRunSpeed;
            //                GlobalData.Current.mRMManager["RM1"].MoveSpeed = autoSpeed; //오토 상태 바꿀때 고속으로 원복
            //                GlobalData.Current.mRMManager["RM1"].SetAutoSpeedInit(autoSpeed);

            //                return eBoothState.AutoStart;
            //            }
            //        }
            //    }
            //    else if (!Start)
            //    {
            //        StartSWCycleElapsed = 0;
            //    }
            //}
            //return CurrentState;
        }

        [Obsolete("SK 미사용 기능")]
        protected override void DoSCS_ResetAction()
        {
            try
            {
                NowOnReset = true; //211207 리셋램프 점등용 플래그 추가.
                SetSafetyRelayReset();
                //로봇 모듈 리셋
                foreach (var rmItem in GlobalData.Current.mRMManager.ModuleList)
                {
                    ////통신 체크
                    //if (!rmItem.Value.RobotOnlineConncet)
                    //{
                    //    LogManager.WriteConsoleLog(eLogLevel.Info, "로봇 연결이 디스커넥트 되어 재연결 시도합니다.");
                    //    rmItem.Value.RobotOnlineConncet = rmItem.Value.ConnectRM(rmItem.Value.RMIp, 1);
                    //}

                    ////210218 lsj 세이프티추가
                    //if (!rmItem.Value.RMMotorMCPower) // MC 떨어졌을때만 리셋
                    //{
                    //    rmItem.Value.RMSafetyReset(true);
                    //    Thread.Sleep(500);
                    //    rmItem.Value.RMSafetyReset(false);
                    //}

                    if (rmItem.Value.RMAlarmCheck() > 0) //알람 상태면 리셋 요청
                    {
                        rmItem.Value.RMAlarmResetAction();
                    }
                }

                //컨베이어 모듈 리셋
                foreach (var Lineitem in GlobalData.Current.PortManager.ModuleList) //컨베이어 라인별 순회
                {
                    foreach (var CVitem in Lineitem.Value.ModuleList)
                    {
                        CVitem.ReleaseAbort(); //- 예전에 유지되던 Abort가 남아있을수 있기에 포트 Reset 시 초기화.
                        if (CVitem.NextCVCommand == eCVCommand.ErrorHandling) //알람 상태면 리셋 요청
                        {
                            CVitem.CV_ErrorResetRequest();
                        }
                    }
                }
                //알람 클리어
                DispatcherService.Invoke((System.Action)(() =>
                {
                    GlobalData.Current.Alarm_Manager.AlarmAllClear();
                }));

                //모듈 상태 보고
                GlobalData.Current.ReportAllStatus();
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
            }
            finally
            {
                NowOnReset = false;
            }
        }

        private bool CheckPLCPauseReq()
        {
            bool bReq = PLC_PauseReq;
            return bReq;
        }

        private bool CheckPLCResumeReq()
        {
            bool bReq = PLC_ResumeReq;
            return bReq;
        }
        /// <summary>
        /// PLC 에서 포즈요청
        /// </summary>
        /// <returns></returns>
        private bool PLCPauseReqAction()
        {
            PC_ResumeReq = false; //기존 요청 플래그 해제     
            PC_PauseReq = false;
            PC_ResumeResponse = false;

            PC_PauseResponse = true; //응답을 준다.
            Stopwatch timeWatch = Stopwatch.StartNew();
            while (!IsTimeout_SW(timeWatch, PLC_Timeout))  //PLC 상태 변경대기.
            {
                if (PLC_PauseState)
                {
                    PC_PauseResponse = false;
                    SCState = eSCState.PAUSING; //중단중 상태로 전이 
                    return true;
                }
            }
            //인터페이스 실패해도 Pausing 상태로
            LogManager.WriteConsoleLog(eLogLevel.Info, "PLCPauseReqAction Pause Time Out!.Force Transfer to Pausing");
            SCState = eSCState.PAUSING; //중단중 상태로 전이 
            return false;
        }

        /// <summary>
        /// PLC 에서 리쥼요청
        /// </summary>
        /// <returns></returns>
        private bool PLCResumeReqAction()
        {
            PC_ResumeReq = false; //기존 요청 플래그 해제     
            PC_PauseReq = false;
            PC_PauseResponse = false;

            PC_ResumeResponse = true; //응답을 준다.
            Stopwatch timeWatch = Stopwatch.StartNew();
            while (!IsTimeout_SW(timeWatch, PLC_Timeout))  //PLC 상태 변경대기.
            {
                if (PLC_AutoState)
                {
                    PC_ResumeResponse = false;
                    SCState = eSCState.AUTO; //오토 상태로 전이 
                    return true;
                }
            }
            LogManager.WriteConsoleLog(eLogLevel.Info, "PLCResumeReqAction Resume Time Out !.");
            //오토상태로 가는건 인터페이스  성공해야함
            return false;
        }


        /// <summary>
        /// PLC 에 포즈를 내린다.
        /// </summary>
        /// <returns></returns>
        public override bool PausePLCAction()
        {
            GlobalData.Current.PortManager.AutoJobStopAllLine();// 포트 포즈 커맨드가 먼저 나간다.
            PC_ResumeReq = false;             //기존 요청 플래그 해제 
            PC_PauseReq = true;
            Stopwatch timeWatch = Stopwatch.StartNew();
            LogManager.WriteConsoleLog(eLogLevel.Info, "SCS Pause PLC Command Req Write.");
            while (!IsTimeout_SW(timeWatch, PLC_Timeout * 2)) //응답을 기다린다. 라인에서는 모자를수도 있어서 연장.
            {
                if (PLC_PauseResponse)
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "SCS Pause PLC PauseResponse Check.");
                    PC_PauseReq = false;
                    //SCState = eSCState.PAUSING; //시스템 포즈는 상위에서 변경하던가 유저가 변경함.
                    return true;
                }
            }
            PC_PauseReq = false;
            LogManager.WriteConsoleLog(eLogLevel.Info, "SCS Pause PLC Time Out!.Force Transfer to Pausing");
            return false;

        }
        /// <summary>
        /// PLC 에 리쥼을 내린다.
        /// </summary>
        /// <returns></returns>
        public override bool ResumePLCAction()
        {
            if (!PLC_AutoState) //PLC Auto 상태가 아니면 불가
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "SCS Resume PLC Resume Command Failed. => PLC_AutoState Off.");
                return false;
            }
            GlobalData.Current.mRMManager.NotifyFireCommand(false);     //241030 HoN 화재 관련 추가 수정        //PLC Resume시 화재관련 신호도 Off (쉘프에 화재 발생 중 이라면 바로 다시 켜진다.)
            GlobalData.Current.PortManager.AutoJobAllLine();// 포트 리쥼 커맨드가 먼저 나간다.
            PC_PauseReq = false;
            PC_ResumeReq = true;
            LogManager.WriteConsoleLog(eLogLevel.Info, "SCS Resume PLC Command Req Write.");
            Stopwatch timeWatch = Stopwatch.StartNew();
            while (!IsTimeout_SW(timeWatch, PLC_Timeout * 2))  //응답을 기다린다.
            {
                if (PLC_ResumeResponse)
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "SCS Resume PLC ResumeResponse.");
                    PC_ResumeReq = false;
                    //SCState = eSCState.AUTO; 시스템 오토는 상위에서 변경하던가 유저가 변경함.
                    return true;
                }
            }
            PC_ResumeReq = false;
            return false;
        }

        public override bool SCSPauseCommand()
        {
            #region 기존 PLC 와 연동 분리

            //if (!SimulMode)
            //{
            //    GlobalData.Current.PortManager.AutoJobStopAllLine();// 포트 포즈 커맨드가 먼저 나간다.
            //    PC_ResumeReq = false;             //기존 요청 플래그 해제 
            //    PC_PauseReq = true;
            //    DateTime dt = DateTime.Now;
            //    LogManager.WriteConsoleLog(eLogLevel.Info, "SCSPauseCommand PLC Pause Command Req Write.");
            //    while (!IsTimeOut(dt, PLC_Timeout * 2)) //응답을 기다린다. 라인에서는 모자를수도 있어서 연장.
            //    {
            //        if (PLC_PauseResponse)
            //        {
            //            LogManager.WriteConsoleLog(eLogLevel.Info, "SCSPauseCommand PLC PauseResponse Check.");
            //            PC_PauseReq = false;
            //            SCState = eSCState.PAUSING; //중단중 상태로 전이 
            //            return true;
            //        }
            //    }
            //    //인터페이스 실패해도 Pausing 상태로
            //    LogManager.WriteConsoleLog(eLogLevel.Info, "SCSPauseCommand Pause Time Out!.Force Transfer to Pausing");
            //    PC_PauseReq = false;

            //    SCState = eSCState.PAUSING; //중단중 상태로 전이 
            //    return true;
            //}
            //else
            //{
            //    //GlobalData.Current.PortManager.AutoJobStopAllLine();// 포트 포즈 커맨드가 먼저 나간다.
            //    SCState = eSCState.PAUSING; //중단중 상태로 전이 
            //    return true;
            //}

            #endregion

            SCState = eSCState.PAUSING; //중단중 상태로 전이 
            return true;
        }
        public override bool SCSResumeCommand()
        {
            #region 기존 PLC 와 연동 분리
            //if (!SimulMode)
            //{
            //    if(!PLC_AutoState) //PLC Auto 상태가 아니면 불가
            //    {
            //        LogManager.WriteConsoleLog(eLogLevel.Info, "SCSResumeCommand PLC Resume Command Failed. => PLC_AutoState Off.");
            //        return false;
            //    }
            //    GlobalData.Current.PortManager.AutoJobAllLine();// 포트 리쥼 커맨드가 먼저 나간다.
            //    PC_PauseReq = false;
            //    PC_ResumeReq = true;
            //    LogManager.WriteConsoleLog(eLogLevel.Info, "SCSResumeCommand PLC Resume Command Req Write.");
            //    DateTime dt = DateTime.Now;
            //    while (!IsTimeOut(dt, PLC_Timeout * 2))  //응답을 기다린다.
            //    {
            //        if (PLC_ResumeResponse)
            //        {
            //            LogManager.WriteConsoleLog(eLogLevel.Info, "SCSResumeCommand PLC ResumeResponse.");
            //            PC_ResumeReq = false;
            //            SCState = eSCState.AUTO;
            //            return true;
            //        }
            //    }
            //    PC_ResumeReq = false;
            //    return false;
            //}
            //else
            //{
            //    GlobalData.Current.PortManager.AutoJobAllLine();// 포트 리쥼 커맨드가 먼저 나간다.
            //    SCState = eSCState.AUTO;
            //}
            #endregion


            if (GlobalData.Current.bFirstMCSResumeReq == false)
            {
                GlobalData.Current.PortManager.AutoJobAllLine();// 포트 리쥼 커맨드가 먼저 나간다.
                GlobalData.Current.bFirstMCSResumeReq = true;
            }


            SCState = eSCState.AUTO;
            return true;
        }
        public override bool SyncPLC_Time()
        {
            if (!SimulMode)
            {
                DateTime dt = DateTime.Now;
                PC_TimeSync_YY = (short)dt.Year;
                PC_TimeSync_MM = (short)dt.Month;
                PC_TimeSync_DD = (short)dt.Day;
                PC_TimeSync_hh = (short)dt.Hour;
                PC_TimeSync_mm = (short)dt.Minute;
                PC_TimeSync_ss = (short)dt.Second;

                PC_TimeSyncReq = true;

                Stopwatch timeWatch = Stopwatch.StartNew();
                while (!IsTimeout_SW(timeWatch, PLC_Timeout))  //응답을 기다린다.
                {
                    if (PLC_TimeSyncResponse)
                    {
                        PC_TimeSyncReq = false;
                        return true;
                    }
                }
                return false;
            }
            else
            {
                return true;
            }
        }
        public override void SetPLCRMReportComplete(bool FirstRM, bool Value)
        {
            if (SimulMode)
            {
                return;
            }
            if (FirstRM)
            {
                PC_RM1ReportComp = Value;
            }
            else
            {
                PC_RM2ReportComp = Value;
            }

        }

        #region PLCInterface PC->PLC Write Area

        public short PC_InterlockRelease
        {
            get { return GData.protocolManager.ReadShort(ModuleName, PCtoPLC, "PC_InterlockRelease"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_InterlockRelease", value); }
        }
        public short PC_HeartBeat
        {
            get { return GData.protocolManager.ReadShort(ModuleName, PCtoPLC, "PC_HeartBeat"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_HeartBeat", value); }
        }

        #region 시간 동기화 변수

        public short PC_TimeSync_YY
        {
            get { return GData.protocolManager.ReadShort(ModuleName, PCtoPLC, "PC_TimeSync_YY"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_TimeSync_YY", value); }
        }
        public short PC_TimeSync_MM
        {
            get { return GData.protocolManager.ReadShort(ModuleName, PCtoPLC, "PC_TimeSync_MM"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_TimeSync_MM", value); }
        }
        public short PC_TimeSync_DD
        {
            get { return GData.protocolManager.ReadShort(ModuleName, PCtoPLC, "PC_TimeSync_DD"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_TimeSync_DD", value); }
        }
        public short PC_TimeSync_hh
        {
            get { return GData.protocolManager.ReadShort(ModuleName, PCtoPLC, "PC_TimeSync_hh"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_TimeSync_hh", value); }
        }
        public short PC_TimeSync_mm
        {
            get { return GData.protocolManager.ReadShort(ModuleName, PCtoPLC, "PC_TimeSync_mm"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_TimeSync_mm", value); }
        }
        public short PC_TimeSync_ss
        {
            get { return GData.protocolManager.ReadShort(ModuleName, PCtoPLC, "PC_TimeSync_ss"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_TimeSync_ss", value); }
        }
        #endregion

        public short PC_SCSVersion1
        {
            get { return GData.protocolManager.ReadShort(ModuleName, PCtoPLC, "PC_SCSVersion1"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_SCSVersion1", value); }
        }
        public short PC_SCSVersion2
        {
            get { return GData.protocolManager.ReadShort(ModuleName, PCtoPLC, "PC_SCSVersion2"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_SCSVersion2", value); }
        }

        #region HP 타워 램프 제어
        public eTowerLampMode PC_TowerLamp_HPRed
        {
            get { return (eTowerLampMode)GData.protocolManager.ReadShort(ModuleName, PCtoPLC, "PC_TowerLamp_HPRed"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_TowerLamp_HPRed", (ushort)value); }
        }
        public eTowerLampMode PC_TowerLamp_HPYellow
        {
            get { return (eTowerLampMode)GData.protocolManager.ReadShort(ModuleName, PCtoPLC, "PC_TowerLamp_HPYellow"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_TowerLamp_HPYellow", (ushort)value); }
        }
        public eTowerLampMode PC_TowerLamp_HPGreen
        {
            get { return (eTowerLampMode)GData.protocolManager.ReadShort(ModuleName, PCtoPLC, "PC_TowerLamp_HPGreen"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_TowerLamp_HPGreen", (ushort)value); }
        }
        public eTowerLampMode PC_TowerLamp_HPBlue
        {
            get { return (eTowerLampMode)GData.protocolManager.ReadShort(ModuleName, PCtoPLC, "PC_TowerLamp_HPBlue"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_TowerLamp_HPBlue", (ushort)value); }
        }
        public eTowerLampMode PC_TowerLamp_HPWhite
        {
            get { return (eTowerLampMode)GData.protocolManager.ReadShort(ModuleName, PCtoPLC, "PC_TowerLamp_HPWhite"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_TowerLamp_HPWhite", (ushort)value); }
        }
        #endregion

        #region OP 타워 램프 제어
        public eTowerLampMode PC_TowerLamp_OPRed
        {
            get { return (eTowerLampMode)GData.protocolManager.ReadShort(ModuleName, PCtoPLC, "PC_TowerLamp_OPRed"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_TowerLamp_OPRed", (ushort)value); }
        }
        public eTowerLampMode PC_TowerLamp_OPYellow
        {
            get { return (eTowerLampMode)GData.protocolManager.ReadShort(ModuleName, PCtoPLC, "PC_TowerLamp_OPYellow"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_TowerLamp_OPYellow", (ushort)value); }
        }
        public eTowerLampMode PC_TowerLamp_OPGreen
        {
            get { return (eTowerLampMode)GData.protocolManager.ReadShort(ModuleName, PCtoPLC, "PC_TowerLamp_OPGreen"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_TowerLamp_OPGreen", (ushort)value); }
        }
        public eTowerLampMode PC_TowerLamp_OPBlue
        {
            get { return (eTowerLampMode)GData.protocolManager.ReadShort(ModuleName, PCtoPLC, "PC_TowerLamp_OPBlue"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_TowerLamp_OPBlue", (ushort)value); }
        }
        public eTowerLampMode PC_TowerLamp_OPWhite
        {
            get { return (eTowerLampMode)GData.protocolManager.ReadShort(ModuleName, PCtoPLC, "PC_TowerLamp_OPWhite"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_TowerLamp_OPWhite", (ushort)value); }
        }
        #endregion

        public eBuzzerSoundType PC_BuzzerHP
        {
            get { return (eBuzzerSoundType)GData.protocolManager.ReadShort(ModuleName, PCtoPLC, "PC_BuzzerHP"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_BuzzerHP", (ushort)value); }
        }
        public eBuzzerSoundType PC_BuzzerOP
        {
            get { return (eBuzzerSoundType)GData.protocolManager.ReadShort(ModuleName, PCtoPLC, "PC_BuzzerOP"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_BuzzerOP", (ushort)value); }
        }

        public short PC_Crane1_Availability
        {
            get { return GData.protocolManager.ReadShort(ModuleName, PCtoPLC, "PC_Crane1_Availability"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_Crane1_Availability", value); }
        }
        public short PC_Crane2_Availability
        {
            get { return GData.protocolManager.ReadShort(ModuleName, PCtoPLC, "PC_Crane2_Availability"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_Crane2_Availability", value); }
        }
        public short PC_SystemStart
        {
            get { return GData.protocolManager.ReadShort(ModuleName, PCtoPLC, "PC_SystemStart"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_SystemStart", value); }
        }
        public bool PC_PauseReq
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PCtoPLC, "PC_PauseReq"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_PauseReq", value); }
        }
        public bool PC_ResumeReq
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PCtoPLC, "PC_ResumeReq"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_ResumeReq", value); }
        }
        public bool PC_TimeSyncReq
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PCtoPLC, "PC_TimeSyncReq"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_TimeSyncReq", value); }
        }
        public bool PC_PauseResponse
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PCtoPLC, "PC_PauseResponse"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_PauseResponse", value); }
        }
        public bool PC_ResumeResponse
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PCtoPLC, "PC_ResumeResponse"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_ResumeResponse", value); }
        }
        public bool PC_RM1ReportComp
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PCtoPLC, "PC_RM1ReportComp"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_RM1ReportComp", value); }
        }
        public bool PC_RM2ReportComp
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PCtoPLC, "PC_RM2ReportComp"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_RM2ReportComp", value); }
        }
        #endregion

        #region  PLCInterface PLC->PC Read Area
        public short PLC_HeartBeat
        {
            get { return GData.protocolManager.ReadShort(ModuleName, PLCtoPC, "PLC_HeartBeat"); }
        }
        public bool PLC_HPDoorOpenState
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_HPDoorOpenState"); }
        }
        public bool PLC_OPDoorOpenState
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_OPDoorOpenState"); }
        }
        public bool PLC_Crane1_Availability
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_Crane1_Availability"); }
        }
        public bool PLC_Crane2_Availability
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_Crane2_Availability"); }
        }
        public short PLC_FireShutterOperation
        {
            get { return GData.protocolManager.ReadShort(ModuleName, PLCtoPC, "PLC_FireShutterOperation"); }
        }
        public bool PLC_PauseResponse
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_PauseResponse"); }
        }
        public bool PLC_ResumeResponse
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_ResumeResponse"); }
        }
        public bool PLC_TimeSyncResponse
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_TimeSyncResponse"); }
        }
        public bool PLC_PauseReq
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_PauseReq"); }
        }
        public bool PLC_ResumeReq
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_ResumeReq"); }
        }
        public bool PLC_PauseState
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_PauseState"); }
        }
        public bool PLC_AutoState
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_AutoState"); }
        }
        public bool PLC_DoorOpenState
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_DoorOpenState"); }
        }
        #endregion
    }
}
