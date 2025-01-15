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
    public class Booth_PMAC : BoothBase
    {
        public Booth_PMAC(string Name, bool simul)
            : base(Name, simul)
        {
            LogManager.WriteConsoleLog(eLogLevel.Info, "Booth Module : PMAC Created!");
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

                    SCState = ProcessPanelSwitchButton(); //OP 패널 제어로직 처리

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
                    GlobalData.Current.PortManager.EmergencyManual_PortStop();
                    GlobalData.Current.PortManager.EmergencyOHTOut_PortStop();
                    GlobalData.Current.PortManager.EmergencyOHTIn_PortStop();
                }
            }
            //메뉴얼 포트 EMS 버튼 체크
            if (CheckManualPortEMS()) //ON ERROR
            {
                GlobalData.Current.PortManager.EmergencyManual_PortStop();
                //GlobalData.Current.Alarm_Manager.AlarmOccurbyName("EMS_E10_PRESSED", this.ModuleName);//메뉴얼 포트는 보고 제외
            }

            //OHT IN 포트 EMS 버튼 체크
            if (CheckOHTInPortEMS()) //ON ERROR
            {
                GlobalData.Current.PortManager.EmergencyOHTIn_PortStop();
            }

            //OHT OUT 포트 EMS 버튼 체크
            if (CheckOHTOutPortEMS()) //ON ERROR
            {
                GlobalData.Current.PortManager.EmergencyOHTOut_PortStop();

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
            //OHT Door 체크 
            OHTIn_DoorOpen = GetOHTInPortDoorOpenState();
            if (OHTIn_DoorOpen)
            {
                GlobalData.Current.Alarm_Manager.AlarmOccurbyName("OHT_DOOR_OPEN", this.ModuleName);
            }
            OHTOut_DoorOpen = GetOHTOutPortDoorOpenState();
            if (OHTOut_DoorOpen)
            {
                GlobalData.Current.Alarm_Manager.AlarmOccurbyName("OHT_DOOR_OPEN", this.ModuleName);
            }
            OHTIn_DoorOpenSolOn = GetOHTInPortDoorUnLockState();
            if (OHTIn_DoorOpenSolOn)
            {
                GlobalData.Current.Alarm_Manager.AlarmOccurbyName("OHT_DOOR_UNLOCK", this.ModuleName);
            }
            OHTOut_DoorOpenSolOn = GetOHTOutPortDoorUnLockState();
            if (OHTOut_DoorOpenSolOn)
            {
                GlobalData.Current.Alarm_Manager.AlarmOccurbyName("OHT_DOOR_UNLOCK", this.ModuleName);
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
            //CPS 전원 공급 체크
            //if (GlobalData.Current.mRMManager["RM1"].RMType == eRMType.PMAC) //210416 lsj Pmac만 사용
            //{
            //    if (CheckCPSAbnormal()) //ON ERROR
            //    {
            //        GlobalData.Current.Alarm_Manager.AlarmOccurbyName("POWER_RELAY_DOWN", this.ModuleName);
            //        //LogManager.WriteConsoleLog(eLogLevel.Error, "CPS_ABNORMAL_STATE");
            //    }

            //    if (!CheckRMMCOn()) //OFF ERROR
            //    {
            //        GlobalData.Current.Alarm_Manager.AlarmOccurbyName("POWER_RELAY_DOWN", this.ModuleName);
            //        //LogManager.WriteConsoleLog(eLogLevel.Error, "POWER_RELAY_DOWN");
            //    }
            //}

            //공압 상태 체크
            if (!CheckAirSupply()) //OFF ERROR
            {
                GlobalData.Current.Alarm_Manager.AlarmOccurbyName("AIR_PRESSURE_DEGRADATION", this.ModuleName);
                //LogManager.WriteConsoleLog(eLogLevel.Error, "AIR_PRESSURE_DEGRADATION");
            }


            bool AlarmOccurred = GlobalData.Current.Alarm_Manager.CheckHeavyAlarmExist(); //다른 모듈 알람도 알람 발생으로 본다.
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


        protected override void DoLBS_ResetAction()
        {
            try
            {
                NowOnReset = true; //211207 리셋램프 점등용 플래그 추가.
                SetSafetyRelayReset();
                //로봇 모듈 리셋
                foreach (var rmItem in GlobalData.Current.mRMManager.ModuleList)
                {
                    //통신 체크
                    if (!rmItem.Value.RobotOnlineConncet)
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "로봇 연결이 디스커넥트 되어 재연결 시도합니다.");
                        rmItem.Value.RobotOnlineConncet = rmItem.Value.ConnectRM(rmItem.Value.RMIp, 1);
                    }

                    //210218 lsj 세이프티추가
                    if (!rmItem.Value.RMMotorMCPower) // MC 떨어졌을때만 리셋
                    {
                        rmItem.Value.RMSafetyReset(true);
                        Thread.Sleep(500);
                        rmItem.Value.RMSafetyReset(false);
                    }

                    if (rmItem.Value.RMAlarmCheck()) //알람 상태면 리셋 요청
                    {
                        rmItem.Value.RMAlarmReset();
                    }
                }
                //CPS 리셋 추가.
                if (CheckCPSAbnormal()) //ON ERROR
                {
                    GlobalData.Current.WPS_mgr.Converter_Monitor.RequestReset();
                    GlobalData.Current.WPS_mgr.Converter_SubMonitor.RequestReset();
                    Thread.Sleep(500);
                    GlobalData.Current.WPS_mgr.Converter_Monitor.RequestRun();
                    GlobalData.Current.WPS_mgr.Converter_SubMonitor.RequestRun();
                }

                //OHT 도어 언락 상태면 도어락 한다.
                if (GetOHTInPortDoorUnLockState() || GetOHTOutPortDoorUnLockState())
                {
                    CCLinkManager.CCLCurrent.WriteIO(GlobalData.Current.MainBooth.ModuleName, "DOOR_OPEN_SOL_OHT_IN", false);
                    CCLinkManager.CCLCurrent.WriteIO(GlobalData.Current.MainBooth.ModuleName, "DOOR_OPEN_SOL_OHT_OUT", false);
                }

                //컨베이어 모듈 리셋
                foreach (var Lineitem in GlobalData.Current.PortManager.ModuleList) //컨베이어 라인별 순회
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

                if (GData.UseServoSystem) //턴 서보시스템 리셋
                {
                    ServoManager.GetManagerInstance().GetServoDrive().sServo_AlarmReset();
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
    }
}
