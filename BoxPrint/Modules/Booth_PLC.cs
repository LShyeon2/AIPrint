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
    public class Booth_PLC : BoothBase
    {
        public Booth_PLC(string Name, bool simul)
            : base(Name, simul)
        {
            LogManager.WriteConsoleLog(eLogLevel.Info, "Booth Module : PLC Created!");
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
                        LogManager.WriteConsoleLog(eLogLevel.Info, "Alarm Reset");
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

                            if (GlobalData.Current.RMSection.RM1Element.RMType == eRMType.PLC_EHER || GlobalData.Current.RMSection.RM1Element.RMType == eRMType.PLC_UTL)
                                SetEQAuto(true);        //2021.06.23 lim,
                        }
                    }
                    else
                    {
                        if (BoothState != BeforeState) //상태가 변할때만 명령
                        {
                            GlobalData.Current.LineManager.AutoJobStopAllLine();

                            if (GlobalData.Current.RMSection.RM1Element.RMType == eRMType.PLC_EHER || GlobalData.Current.RMSection.RM1Element.RMType == eRMType.PLC_UTL)
                                SetEQAuto(false);       //2021.06.23 lim,
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
                    //GlobalData.Current.LineManager.EmergencyOHTOut_PortStop();
                    //GlobalData.Current.LineManager.EmergencyOHTIn_PortStop();
                }
            }
            //메뉴얼 포트 EMS 버튼 체크
            if (CheckManualPortEMS()) //ON ERROR
            {
                GlobalData.Current.LineManager.EmergencyManual_PortStop();
                //GlobalData.Current.Alarm_Manager.AlarmOccurbyName("EMS_E10_PRESSED", this.ModuleName);//메뉴얼 포트는 보고 제외
            }

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
            if (CheckMaintDoorOpen()) //ON ERROR
            {
                GlobalData.Current.Alarm_Manager.AlarmOccurbyName("MAINTENANCE_DOOR_OPEN", this.ModuleName);
                //LogManager.WriteConsoleLog(eLogLevel.Error, "MAINTENANCE_DOOR_OPEN");
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
            
            bool AlarmOccurred = GlobalData.Current.Alarm_Manager.CheckHeavyAlarmExist(); //다른 모듈 알람도 알람 발생으로 본다.
            return AlarmOccurred;
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
                if (Unlock)
                {
                    ////언락으로 키 돌리면 Door Open Sol On
                    CCLinkManager.CCLCurrent.WriteIO(ModuleName, "DOOR_OPEN_TO_PLC", true);
                    //UNLOCK ENABLE 들어오면 SAFETY, ROBOT 으로 출력 내보낸다. 
                    bool UnlockEnable = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "DOOR_UNLOCK_ENABLE");
                    if (UnlockEnable)
                    {
                        CCLinkManager.CCLCurrent.WriteIO(ModuleName, "DOOR_OPEN_FROM_PLC", UnlockEnable);

                    }
                    SetInnerLampControl(true);
                }
                else
                {
                    ////메뉴얼로 키 돌리면 Door Open Sol Off
                    CCLinkManager.CCLCurrent.WriteIO(ModuleName, "DOOR_OPEN_TO_PLC", false);
                    //UNLOCK ENABLE 꺼지면 SAFETY, ROBOT 으로 출력 내보낸다. 
                    bool UnlockEnable = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "DOOR_UNLOCK_ENABLE");
                    if (!UnlockEnable)
                    {
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

        protected override void DoLBS_ResetAction()
        {
            try
            {
                NowOnReset = true; //211207 리셋램프 점등용 플래그 추가.
                bool RMHomeUse = false;

                LogManager.WriteConsoleLog(eLogLevel.Info, "check Power");
                if (CheckPower_Relay() == false)
                {
                    RMHomeUse = true;

                    if (GlobalData.Current.RMSection.RM1Element.RMType == eRMType.PLC_EHER || GlobalData.Current.RMSection.RM1Element.RMType == eRMType.PLC_UTL)
                        SetServoMC(true);   //2021.06.23 lim,
                }

                LogManager.WriteConsoleLog(eLogLevel.Info, "Relay Reset");
                SetSafetyRelayReset();
                //로봇 모듈 리셋
                foreach (var rmItem in GlobalData.Current.mRMManager.ModuleList)
                {
                    //통신 체크
                    if (!rmItem.Value.RobotOnlineConncet)
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "로봇 연결이 디스커넥트 되어 재연결 시도합니다.");
                        rmItem.Value.RobotOnlineConncet = rmItem.Value.RMConnecting(rmItem.Value.RMIp, 1);
                    }

                    Thread.Sleep(100);

                    if (rmItem.Value.RMAlarmCheck()) //알람 상태면 리셋 요청
                    {
                        rmItem.Value.RMAlarmReset();
                    }

                    if (RMHomeUse == true && CheckPower_Relay() == true)
                    {
                        rmItem.Value.RMinitCmd();
                    }
                    //210514 lsj 상태보고 추가
                    //GData.WCF_mgr.ReportRobotStatus(rmItem.Value.ModuleName);

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
                //if (!GlobalData.Current.Alarm_Manager.CheckHeavyAlarmExist())
                //{
                //    CurrentState = eBoothState.Manual;
                //}
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

        //2021.06.23 lim,
        public void SetServoMC(bool OnOff)
        {
            if (SimulMode)
                return;
            else
            {
                CCLinkManager.CCLCurrent.WriteIO(ModuleName, "SERVO_MC_ON", OnOff);
                CCLinkManager.CCLCurrent.WriteIO(ModuleName, "INVERTER_MC_ON", OnOff);
                CCLinkManager.CCLCurrent.WriteIO(ModuleName, "SERVO_CONTROL_MC_ON", OnOff);
            }
        }

        //2021.06.23 lim,
        public void SetEQAuto(bool OnOff)
        {
            if (SimulMode)
                return;
            else
                CCLinkManager.CCLCurrent.WriteIO(ModuleName, "EQ_AUTO_MODE", OnOff);
        }

        public override bool GetDoorUnLockState()
        {
            if (!SimulMode)
            {
                bool bDoorUnlock = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "DOOR_UNLOCK_ENABLE");    //2021.06.18 LIM,
                return bDoorUnlock;
            }
            return false;
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
    }
}
