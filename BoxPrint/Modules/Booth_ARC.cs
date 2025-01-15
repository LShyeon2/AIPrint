using System;
using System.Threading;
using Stockerfirmware.CCLink;
using Stockerfirmware.Log;
using Stockerfirmware.DataList;
using WCF_LBS.Commands;
using WCF_LBS.DataParameter;
using Stockerfirmware.Alarm;
using Stockerfirmware.SSCNet;


namespace Stockerfirmware.Modules
{
    // 2021.07.12 RGJ
    //- Booth 모듈 세분화(PMAC, ARC, PLC)
    public class Booth_ARC : BoothBase
    {
        public Booth_ARC (string Name, bool simul)
            : base(Name, simul)
        {
            LogManager.WriteConsoleLog(eLogLevel.Info, "Booth Module : ARC Created!");
        }
        protected override void BoothRun()
        {
            LogManager.WriteConsoleLog(eLogLevel.Info, "Booth Run Start");

            while (!ExitThread)
            {
                try
                {
                    bool Reset = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "SWITCH_PB_RESET");
                    //리셋 부터 처리한다.
                    if (Reset)
                    {
                        DoLBS_ResetAction();
                    }

                    BoothState = ProcessPanelSwitchButton(); //OP 패널 제어로직 처리

                    bool bAbnormal = DoAbnormalCheck();//부스 상태 체크해서 알람 발생까지
                    if (bAbnormal)
                    {
                        if (BoothState == eBoothState.AutoStart)
                        {
                            BoothState = eBoothState.Manual;
                        }
                    }

                    //부스 상태봐서 컨베이어 오토 런 가동
                    if (BoothState == eBoothState.AutoStart)
                    {
                        if (BoothState != BeforeState) //상태가 변할때만 명령
                        {
                            GlobalData.Current.LineManager.AutoJobAllLine();
                        }
                    }
                    else
                    {
                        if (BoothState != BeforeState) //상태가 변할때만 명령
                        {
                            GlobalData.Current.LineManager.AutoJobStopAllLine();
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
                }
                Thread.Sleep(BoothRunCycleTime);
            }
            LogManager.WriteConsoleLog(eLogLevel.Info, "Booth Run End");
        }
        public override bool DoAbnormalCheck()
        {
            //EMS 버튼 체크
            var EMSResult = CheckEMO_Pressed();
            for (int i = 0; i < EMSResult.Length; i++)
            {
                if (EMSResult[i])
                {
                    string alarmName = string.Format("EMS_E{0:D2}_PRESSED", i + 1);
                    GlobalData.Current.Alarm_Manager.AlarmOccurbyName(alarmName, this.ModuleName);
                    GlobalData.Current.LineManager.EmergencyManual_PortStop();
                }
            }
            
            //메뉴얼 포트 EMS 버튼 체크
            if (CheckManualPortEMS()) //ON ERROR
            {
                GlobalData.Current.LineManager.EmergencyManual_PortStop();
                //GlobalData.Current.Alarm_Manager.AlarmOccurbyName("EMS_E10_PRESSED", this.ModuleName);//메뉴얼 포트는 보고 제외
            }
            //라이트 커튼 체크
            CheckLightCurtain_Error();
            //도어 상태 체크
            if (CheckHomeDoorOpen()) //ON ERROR
            {
                GlobalData.Current.Alarm_Manager.AlarmOccurbyName("HOME_DOOR_OPEN", this.ModuleName);
                //LogManager.WriteConsoleLog(eLogLevel.Error, "HOME_DOOR_OPEN");
            }
            if (CheckHomeOPDoorOpen()) //ON ERROR
            {
                GlobalData.Current.Alarm_Manager.AlarmOccurbyName("HOME_OPPOSITE_DOOR_OPEN", this.ModuleName);
                //LogManager.WriteConsoleLog(eLogLevel.Error, "HOME_OPPOSITE_DOOR_OPEN");
            }
            if (CheckMaintDoorOpen()) //ON ERROR //비사용
            {
                GlobalData.Current.Alarm_Manager.AlarmOccurbyName("MAINTENANCE_DOOR_OPEN", this.ModuleName);
                //LogManager.WriteConsoleLog(eLogLevel.Error, "MAINTENANCE_DOOR_OPEN");
            }

            //lsj SESS Door
            Port_DoorOpen = GetPortDoorOpenState();
            if (Port_DoorOpen)
            {
                GlobalData.Current.Alarm_Manager.AlarmOccurbyName("AUTO_DOOR_MOTION_ERROR", this.ModuleName);
            }

  

            //전원 릴레이
            if (!CheckPower_Relay()) //OFF ERROR
            {
                GlobalData.Current.Alarm_Manager.AlarmOccurbyName("POWER_RELAY_DOWN", this.ModuleName);
                //LogManager.WriteConsoleLog(eLogLevel.Error, "POWER_RELAY_DOWN");
            }
            //인버터 전원
            if (!CheckInverterPower()) //OFF ERROR
            {
                GlobalData.Current.Alarm_Manager.AlarmOccurbyName("INVERTER_POWER_DOWN", this.ModuleName);
                //LogManager.WriteConsoleLog(eLogLevel.Error, "INVERTER_POWER_DOWN");
            }
            //연기 감지
            if (CheckSmokeDetect()) //ON ERROR
            {
                GlobalData.Current.Alarm_Manager.AlarmOccurbyName("SMOKE_DETECTED", this.ModuleName);
                //LogManager.WriteConsoleLog(eLogLevel.Error, "SMOKE_DETECTED");
            }
          
            //공압 상태 체크
            if (!CheckAirSupply()) //OFF ERROR
            {
                GlobalData.Current.Alarm_Manager.AlarmOccurbyName("AIR_PRESSURE_DEGRADATION", this.ModuleName);
                //LogManager.WriteConsoleLog(eLogLevel.Error, "AIR_PRESSURE_DEGRADATION");
            }


            bool AlarmOccurred = GlobalData.Current.Alarm_Manager.CheckHeavyAlarmExist(); //다른 모듈 알람도 알람 발생으로 본다.
            return AlarmOccurred;
        }
        /// <summary>
        /// 라이트 커튼 이상 있을시 더블 체크
        /// </summary>
        protected void CheckLightCurtain_Error()
        {
            var LCResult = CheckLightCurtain_Detected();
            bool FirstCheck = false;
            for (int i = 0; i < LCResult.Length; i++)
            {
                if (LCResult[i])
                {
                    FirstCheck = true;
                }
            }
            //라이트 커튼 이상 체크되면 1초 대기후 다시 검사
            if(FirstCheck) //211202 RGJ 소박스 SETUP
            {
                Thread.Sleep(1000);
                LCResult = CheckLightCurtain_Detected();
                for (int i = 0; i < LCResult.Length; i++)
                {
                    if (LCResult[i])
                    {
                        var CV = GlobalData.Current.LineManager.GetCVModuleByLightCurtainNumber(i + 1);
                        if (CV != null)
                        {
                            GlobalData.Current.Alarm_Manager.AlarmOccurbyName("LIGHT_CURTAIN_DETECTED", CV.ModuleName);
                            break;
                        }
                    }
                }
            }
            else
            {
                return;
            }
           
        }

        protected override void DoLBS_ResetAction()
        {
            try
            {
                NowOnReset = true; //211207 리셋램프 점등용 플래그 추가.
                bool RMHomeUse = false;

                if (CheckPower_Relay() == false)
                    RMHomeUse = true;

                //lsj SESS Door
                if (GetPortDoorOpenState())
                {
                    foreach (var Lineitem in GlobalData.Current.LineManager.ModuleList) //컨베이어 라인별 순회
                    {
                        foreach (var CVitem in Lineitem.Value.ModuleList)
                        {
                            if (CVitem.UseDoor)
                                CCLinkManager.CCLCurrent.WriteIO(GlobalData.Current.MainBooth.ModuleName, String.Format("CV_DOOR_OPEN_SOL_{0}", CVitem.DoorNumber), false);
                        }
                    }
                }

                SetSafetyRelayReset();
                //로봇 모듈 리셋
                foreach (var rmItem in GlobalData.Current.mRMManager.ModuleList)
                {
                    //통신 체크
                    if (!rmItem.Value.RobotOnlineConncet)
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "로봇 연결이 디스커넥트 되어 재연결 시도합니다.");
                        rmItem.Value.RobotOnlineConncet = rmItem.Value.RMConnecting(rmItem.Value.RMIp, rmItem.Value.RMPort);
                    }

                    //lsj SESS 주석
                    //if (rmItem.Value.RMAlarmCheck()) //알람 상태면 리셋 요청
                    //{
                    //    Thread.Sleep(1000);

                    //    rmItem.Value.RMAlarmReset();
                    //}

                    //if (RMHomeUse == true && CheckPower_Relay() == true)
                    //{
                    //    rmItem.Value.RMinitCmd();
                    //}

                    //lsj SESS 변경
                    if (rmItem.Value.RMAlarmCheck())
                    {
                        rmItem.Value.RMinitCmd();
                    }

                    //210514 lsj 상태보고 추가
                    //GData.WCF_mgr.ReportRobotStatus(rmItem.Value.ModuleName);

                }
                //OHT 도어 언락 상태면 도어락 한다.
                if (GetOHTInPortDoorUnLockState() || GetOHTOutPortDoorUnLockState())
                {
                    CCLinkManager.CCLCurrent.WriteIO(GlobalData.Current.MainBooth.ModuleName, "DOOR_OPEN_SOL_OHT_IN", false);
                    CCLinkManager.CCLCurrent.WriteIO(GlobalData.Current.MainBooth.ModuleName, "DOOR_OPEN_SOL_OHT_OUT", false);
                }
                //컨베이어 모듈 리셋
                foreach (var Lineitem in GlobalData.Current.LineManager.ModuleList) //컨베이어 라인별 순회
                {
                    foreach (var CVitem in Lineitem.Value.ModuleList)
                    {
                        CVitem.ReleaseAbort(); //- 예전에 유지되던 Abort가 남아있을수 있기에 포트 Reset 시 초기화.
                        if (CVitem.NextCVCommand == eCVCommand.ErrorHandling) //알람 상태면 리셋 요청
                        {
                            CVitem.CV_ErrorReset();
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

        protected override eBoothState ProcessPanelSwitchButton()
        {
            if (SimulMode)
            {
                if (SCS_SimulState != eBoothState.None)
                {
                    return SCS_SimulState;
                }
                return eBoothState.AutoStart;
            }
            //Auto,Manual 스위치 체크
            //Start,Stop 버튼 체크
            bool AutoSW = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "SWITCH_SS_AUTO");
            bool ManualSW = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "SWITCH_SS_MANUAL");
            bool Start = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "SWITCH_PB_START");
            bool Stop = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "SWITCH_PB_STOP");
            bool Unlock = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "DOOR_UNLOCK_S/W");
    
            if (ManualSW)//메뉴얼
            {
                if(GlobalData.Current.LBSType == eLBSType.BoxLBS)
                {
                    SetLightCurtainMute(LightcurtainSyncNumber, true);
                }
                if (Unlock)
                {
                    //UNLOCK ENABLE 들어오면 SAFETY, ROBOT 으로 출력 내보낸다. 
                    bool UnlockEnable = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "DOOR_UNLOCK_ENABLE");
                    if (UnlockEnable)
                    {
                        Thread.Sleep(200);
                        CCLinkManager.CCLCurrent.WriteIO(ModuleName, "DOOR_OPEN_TO_PLC", UnlockEnable);
                        CCLinkManager.CCLCurrent.WriteIO(ModuleName, "DOOR_OPEN_FROM_PLC", UnlockEnable);
                    }
                    SetInnerLampControl(true);
                }
                else
                {
                    CCLinkManager.CCLCurrent.WriteIO(ModuleName, "DOOR_OPEN_TO_PLC", false);
                    //UNLOCK ENABLE 꺼지면 SAFETY, ROBOT 으로 출력 내보낸다. 
                    bool UnlockEnable = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "DOOR_UNLOCK_ENABLE");
                    if (!UnlockEnable)
                    {
                        //Thread.Sleep(200);
                        CCLinkManager.CCLCurrent.WriteIO(ModuleName, "DOOR_OPEN_FROM_PLC", UnlockEnable);

                    }
                    SetInnerLampControl(false);
                }
                return eBoothState.Manual;
            }
            
            if (AutoSW && !ManualSW)
            {
                if (Stop) //스탑은 바로 반영
                {
                    StartSWCycleElapsed = 0;
                    return eBoothState.Manual;
                }
                else if (Start)
                {
                    //Start 버튼은 3초 이상 눌려야 변경한다.
                    StartSWCycleElapsed++;
                    if (StartSWCycleElapsed > 3) //30 -> 3 로 확인 사이클 변경
                    {
                        StartSWCycleElapsed = 0;
                        if (CurrentState == eBoothState.Manual) //현재 에러 상태가 아니고 메뉴얼일때 오토로 변경
                        {
                            var autoSpeed = GlobalData.Current.mRMManager["RM1"].AutoRunSpeed;
                            GlobalData.Current.mRMManager["RM1"].MoveSpeed = autoSpeed; //오토 상태 바꿀때 고속으로 원복
                            GlobalData.Current.mRMManager["RM1"].SetAutoSpeedInit(autoSpeed);

                            if (GlobalData.Current.LBSType == eLBSType.BoxLBS)
                            {
                                SetLightCurtainMute(LightcurtainSyncNumber, false);
                            }
                            return eBoothState.AutoStart;
                        }
                    }
                }
                else if (!Start)
                {
                    StartSWCycleElapsed = 0;
                }
            }
            return CurrentState;
        }

        protected override bool CheckManualPortEMS()
        {
            if (SimulMode)
            {
                return false;
            }
            bool EMS1 = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "MANUAL_PORT_EMS_1");
            bool EMS2 = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "MANUAL_PORT_EMS_2");
            return EMS1 || EMS2;
        }
        protected override bool CheckInverterPower()
        {

            if (!SimulMode)
            {
                bool bManualPort = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "KM12_MC_ON");
                bool bAutoInOHTPort = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "KM34_MC_ON");

                if (bManualPort && bAutoInOHTPort) //정상 On 상태
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
        /// <summary>
        /// 부스 Door Open 상태 체크
        /// 0:CLOSE    1:HP OPEN   2:OP OPEN   3:BOTH OPEN
        /// </summary>
        /// <returns></returns>
        public override string GetBoothDoorOpenState()
        {
            int Result = 0;
            bool HomeDoor = CheckHomeDoorOpen();
            bool HomeOPDoor = CheckHomeOPDoorOpen();
            if (HomeDoor)
            {
                Result += 1;
            }
            if (HomeOPDoor)
            {
                Result += 2;
            }
            return Result.ToString();
        }

        public override bool GetDoorUnLockState()
        {
            if (!SimulMode)
            {
                bool bDoorUnlock = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "DOOR_OPEN_TO_PLC");
                return bDoorUnlock;
            }
            return false;
        }

        /// <summary>
        /// 라이트커튼 뮤트 신호를 제어한다.
        /// </summary>
        /// <param name="OnOff"></param>
        /// <returns></returns>
        public override bool SetLightCurtainMute(int LC_Number, bool OnOff,int SyncSeq = 0)
        {
            if (1 <= LC_Number && LC_Number <= LightCurtainCount)
            {
                if (SimulMode)
                {
                    return true;
                }
                else
                {
                    if (LC_Number == LightcurtainSyncNumber) //싱크 로직
                    {
                        LCSyncMuteArray[SyncSeq] = OnOff;
                        if(OnOff == false) //OFF 일때 아직 다른 모듈이 On 상태라면 뮤트 Off 를 스킵한다.
                        {
                            foreach(bool bMute in LCSyncMuteArray)
                            {
                                if(bMute)
                                {
                                    return false;
                                }
                            }
                        }
                    }

                    if(OnOff ==  true) //이미 뮤팅되어있으면 또 뮤팅 할 필요 없음
                    {
                        bool Mute = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "LIGHT_CURTAIN_MUTE_" + LC_Number);
                        if(Mute)
                        {
                            return true;
                        }
                    }
          
                    
                    //ON 시킬때는 먼저 Off후 다시 On.
                    if (OnOff)
                    {
                        CCLinkManager.CCLCurrent.WriteIO(ModuleName, "LIGHT_CURTAIN_MUTE_" + LC_Number, false);
                        Thread.Sleep(50);
                        CCLinkManager.CCLCurrent.WriteIO(ModuleName, "LIGHT_CURTAIN_MUTE_" + LC_Number, OnOff);
                    }
                    else //Off 할때는 딜레이 필요없음.
                    {
                        CCLinkManager.CCLCurrent.WriteIO(ModuleName, "LIGHT_CURTAIN_MUTE_" + LC_Number, false);
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
        public override bool GetLightCurtainMuteState(int LC_Number)
        {
            if (1 <= LC_Number && LC_Number <= LightCurtainCount)
            {
                if (SimulMode)
                {
                    return true;
                }
                else
                {
                    DateTime dt = DateTime.Now;
                    do
                    {
                        bool muteOn = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "LIGHT_CURTAIN_MUTE_" + LC_Number);
                        bool muteOnState = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "LIGHT_CURTAIN_MUTE_ON_" + LC_Number);
                        if(muteOn)
                        {
                            if (muteOnState)
                            {
                                return true;
                            }
                        }
                        else
                        {
                            return false;
                        }
                      
                    }
                    while (!IsTimeOut(dt, 3));
                    return false;
                }
            }
            else
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "GetLightCurtainMuteState  {0}은 잘못된 인자 값입니다.", LC_Number);
                return false;
            }
        }
    }
}
