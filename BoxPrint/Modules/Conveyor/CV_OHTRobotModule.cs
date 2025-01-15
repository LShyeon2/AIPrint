using System.Threading;
using Stockerfirmware.Log;
using Stockerfirmware.CCLink;
using Stockerfirmware.DataList.CV;
using Stockerfirmware.Modules.RM;
using System;

namespace Stockerfirmware.Modules.Conveyor
{
    public class CV_OHTRobotModule : CV_BaseModule
    {
        private readonly int T1Timeout = 3; //TR_REQ 수신대기시간
        private readonly int T3Timeout = 3; //BUSY 수신대기시간
        private readonly int T6Timeout = 3; //ALL OFF 대기시간
        private readonly int OHT_ActionTimeout = 60; //READY ON 이후 TRAY 감지대기 시간

        //private bool PIOStarted = false;
        private bool PIOCompleted = false;
        private eOHTPIORecoveryOption RecoveryOption = eOHTPIORecoveryOption.None; //OHT PIO Error 발생시 사용할 복구 옵션
        private eOHTPIOStep _CurrentStep = eOHTPIOStep.Unknown;
        public eOHTPIOStep CurrentStep
        {
            get
            {
                return _CurrentStep;
            }
            set
            {
                if (_CurrentStep != value)
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "CurrentModule:{0} CurrentStep:{1} NextStep:{2} ", ModuleName, _CurrentStep, value);
                    //스텝이 바뀌면 로그를 찍는다.
                    _CurrentStep = value;
                }
            }
        }

        public CV_OHTRobotModule(string mName, bool simul) : base(mName, simul)
        {
            CVModuleType = eCVType.OHTRobot;
        }


        public override bool CheckTrayUnloadingPosition()
        {
            //Tray 재하 체크
            //Tray 얼라인 체크
            //bool bStopPos = IN_STOP; //스탑위치가 정지위치
            //bool bAlign = CheckTrayAligned();
            //return bStopPos && bAlign;

            //2021.06.21 lim, 다름
            bool Stage1 = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "CV_OHT_TRAY_DETECT1");
            bool Stage2 = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "CV_OHT_TRAY_DETECT2");

            return Stage1 && Stage2;
        }

        public override bool CheckOHTArmDetected()
        {
            if (SimulMode)
            {
                return false;
            }
            bool bValue = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "CV_OHTARM_DETECTED");
            return bValue;
        }

        /// <summary>
        /// OHT -> LBS
        /// OHT 가 트레이를 포트에 로딩 PIO 진행.
        /// </summary>
        /// <returns></returns>
        public OHT_PIOResult OHTLoadPIO()
        {
            LogManager.WriteConsoleLog(eLogLevel.Info, "OHTLoadAction() Started!");
            PIOCompleted = false;
            bool bActionExit = false;
            CurrentStep = eOHTPIOStep.LoadStep00_Start;
            while (!bActionExit)
            {
                //이상상태 체크
                if (DoAbnormalCheck())
                {
                    if (CurrentStep == eOHTPIOStep.LoadStep00_Start || CurrentStep == eOHTPIOStep.Step_Done)
                    {
                        CurrentStep = eOHTPIOStep.Step_Done; //스텝 시작이나 종료일때는 그냥 종료.
                    }
                    else
                    {
                        CurrentStep = eOHTPIOStep.LoadErrorRecovery_Start; //에러처리 스텝으로
                    }
                }
                if (ActionAbortRequested)
                {
                    //CV_RunStop();
                    ActionAbortRequested = false;
                    CurrentStep = eOHTPIOStep.LoadErrorRecovery_Start;

                }
                if (SimulMode)
                {
                    if (IsTrayExist())
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "시뮬 모드이므로 데이타 확인후 로드 완료처리");
                        ReportModule_LCS();
                        return new OHT_PIOResult(true, this.ModuleName, eOHT_PIOResult.Complete, "");
                    }
                }
                switch (CurrentStep)
                {
                    case eOHTPIOStep.LoadStep00_Start:
                        LoadStep00_Start();
                        break;
                    case eOHTPIOStep.LoadStep01_Wait_TRReq:
                        LoadStep01_Wait_TRReq();
                        break;
                    case eOHTPIOStep.LoadStep02_Wait_BusyOn:
                        LoadStep02_Wait_BusyOn();
                        break;
                    case eOHTPIOStep.LoadStep03_Wait_OHTComplete:
                        LoadStep03_Wait_OHTComplete();
                        break;
                    case eOHTPIOStep.LoadStep04_Wait_AllOff:
                        LoadStep04_Wait_AllOff();
                        break;
                    case eOHTPIOStep.LoadErrorRecovery_Start:
                        LoadErrorRecovery_Start();
                        break;
                    case eOHTPIOStep.Step_Done:
                        if (PIOCompleted)  //정상종료
                        {
                            LogManager.WriteConsoleLog(eLogLevel.Info, "OHTLoadAction() Exit!");

                            string RFIDReadValue = CVRFID_Read();//RFID 읽기
                            //UpdateTrayTagID(RFIDReadValue);
                            InsertTray(new Tray(RFIDReadValue, true)); //트레이 생성

                            ////센터링 작업 시작
                            //CVStopperClose(true);
                            //CVStopperClose(false);

                            Thread.Sleep(200);
                            ReportModule_LCS();
                            return new OHT_PIOResult(true, this.ModuleName, eOHT_PIOResult.Complete, "");
                        }
                        else  //비정상종료
                        {
                            LogManager.WriteConsoleLog(eLogLevel.Info, "OHTLoadAction() 비정상 Exit!");
                            ReportModule_LCS();
                            return new OHT_PIOResult(true, this.ModuleName, eOHT_PIOResult.Aborted, "");
                        }
                    default:
                        break;
                }
                Thread.Sleep(LocalStepCycleDelay);

            }
            return new OHT_PIOResult(true, this.ModuleName, eOHT_PIOResult.Aborted, "");

        }
        /// <summary>
        /// LBS -> OHT
        /// OHT 가 트레이를 포트에서 언로딩 PIO 진행.
        /// </summary>
        /// <returns></returns>
        public OHT_PIOResult OHTUnloadPIO()
        {
            LogManager.WriteConsoleLog(eLogLevel.Info, "OHTUnloadAction() Started!");
            PIOCompleted = false;
            bool bActionExit = false;
            CurrentStep = eOHTPIOStep.UnloadStep00_Start;
            while (!bActionExit)
            {
                //이상상태 체크
                if (DoAbnormalCheck())
                {
                    if (CurrentStep == eOHTPIOStep.UnloadStep00_Start || CurrentStep == eOHTPIOStep.Step_Done)
                    {
                        CurrentStep = eOHTPIOStep.Step_Done; //스텝 시작이나 종료일때는 그냥 종료.
                    }
                    else
                    {
                        CurrentStep = eOHTPIOStep.UnloadErrorRecovery_Start; //에러처리 스텝으로
                        OUT_OHTIF_ABORT = true;
                    }
                }
                if (ActionAbortRequested)
                {
                    //CV_RunStop();
                    ActionAbortRequested = false;
                    CurrentStep = eOHTPIOStep.UnloadErrorRecovery_Start;
                }
                switch (CurrentStep)
                {
                    case eOHTPIOStep.UnloadStep00_Start:
                        UnloadStep00_Start();
                        break;
                    case eOHTPIOStep.UnloadStep01_Wait_TRReq:
                        UnloadStep01_Wait_TRReq();
                        break;
                    case eOHTPIOStep.UnloadStep02_Wait_BusyOn:
                        UnloadStep02_Wait_BusyOn();
                        break;
                    case eOHTPIOStep.UnloadStep03_Wait_OHTComplete:
                        UnloadStep03_Wait_OHTComplete();
                        break;
                    case eOHTPIOStep.UnloadStep04_Wait_AllOff:
                        UnloadStep04_Wait_AllOff();
                        break;
                    case eOHTPIOStep.UnloadErrorRecovery_Start:
                        UnloadErrorRecovery_Start();
                        break;
                    case eOHTPIOStep.Step_Done:

                        if (PIOCompleted)//정상종료
                        {
                            LogManager.WriteConsoleLog(eLogLevel.Info, "OHTUnloadAction() Exit!");
                            ReportModule_LCS();
                            return new OHT_PIOResult(false, this.ModuleName, eOHT_PIOResult.Complete, "");
                        }
                        else  //비정상종료
                        {
                            LogManager.WriteConsoleLog(eLogLevel.Info, "OHTUnloadAction()  비정상 Exit!");
                            ReportModule_LCS();
                            return new OHT_PIOResult(false, this.ModuleName, eOHT_PIOResult.Complete, "");
                        }
                    default:
                        break;
                }
                Thread.Sleep(LocalStepCycleDelay);
            }
            return new OHT_PIOResult(false, this.ModuleName, eOHT_PIOResult.Aborted, "");
        }


        protected override void CVMainRun()
        {
            try //-메인 루프 예외 발생시 로그 찍도록 추가.
            {
                CV_ActionResult Result = null;
                NextCVCommand = eCVCommand.Initialize;
                while (!ThreadExitRequested)
                {
                    if (AutoManual != eCVAutoManual.Auto)
                    {
                        CurrentActionDesc = "Auto 모드를 기다립니다.";
                        Thread.Sleep(LocalStepCycleDelay);
                        continue;
                    }
                    switch (NextCVCommand)
                    {
                        case eCVCommand.Initialize:
                            Result = InitializeAction();
                            break;
                        case eCVCommand.TrayLoad:
                            Result = TrayLoadAction();
                            break;
                        case eCVCommand.TrayUnload:
                            Result = TrayUnloadAction();
                            break;
                        case eCVCommand.ErrorHandling:
                            Result = ErrorHandlingAction();
                            break;
                        default:
                            break;
                    }
                    if (Result.actionResult != eCV_ActionResult.Complete)
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Error, "Module:{0} NextCVCommand:{1} ErrorMessage:{2}", ModuleName, NextCVCommand, Result.message);
                        NextCVCommand = eCVCommand.ErrorHandling; //비정상 완료면 에러 핸들링으로 보낸다.
                    }
                    //Result GUI 보고
                    LastActionResult = Result?.message;
                    Thread.Sleep(100);
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
            }
        }

        protected override CV_ActionResult InitializeAction()
        {
            LogManager.WriteConsoleLog(eLogLevel.Info, "{0} Module InitializeAction Start", this.ModuleName);
            CurrentActionDesc = "초기화 동작중입니다.";
            LocalActionStep = 0;
            //2021.05.21 lim, OHT Port는 컨베이어가 없으므로 제품 유무만 판단

            //인버터 에러 체크
            //if (!CVRunner.CV_Reset())
            //{
            //    //인버터 에러 리셋 실패하면 Error
            //    GlobalData.Current.Alarm_Manager.AlarmOccurbyName("INVERTER_RUN_ERROR", ModuleName);
            //    return new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "인버터 에러 리셋에 실패하였습니다.");
            //}

            //라이트 커튼 상태보고 뮤트 해제
            if (CheckLightCurtainMute())
            {
                LightCurtainMuteControl(false);
            }
            //트레이 데이터 상태 체크
            if (IsTrayExist())
            {
                RemoveTray();
                ReportModule_LCS();
                if (CurrentTray == null)
                {
                    //트레이 데이터가 없다면 빈 트레이 생성
                    InsertTray(new Tray("ERROR", true));
                }

                if (UseRFID) //RFID 다시 찍는다.
                {
                    string RFIDReadValue = CVRFID_Read();//RFID 읽기
                    UpdateTrayTagID(RFIDReadValue);
                }
                CurrentTray.SetTrayHeight(CheckTrayHeight());
                //스톱위치면 바로 Tray Unload 로
                NextCVCommand = eCVCommand.TrayUnload;

            }
            else
            {
                NextCVCommand = eCVCommand.TrayLoad;
            }
            return new CV_ActionResult(ModuleName, eCV_ActionResult.Complete, "InitializeAction 완료.");
        }

        /// <summary>
        /// 외부 장비에서 Tray Load 인터페이스
        /// </summary>
        /// <param name="RequireRecovery"></param>
        /// <returns></returns>
        protected override CV_ActionResult TrayLoadAction()
        {
            LogManager.WriteConsoleLog(eLogLevel.Info, "{0} Module Tray Load Start", this.ModuleName);
            CurrentActionDesc = "트레이 로드 동작으로 진입합니다.";
            LocalActionStep = 0;

            CV_ActionResult errorResult = new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "동작중 에러 발생하였습니다.");

            if (IsInPort) //Inport 면  OHT PIO
            {
                #region Simulation 전용
                if (SimulMode)
                {
                    while (true)
                    {
                        if (IsTrayExist())
                        {
                            ReportModule_LCS();
                            LogManager.WriteConsoleLog(eLogLevel.Info, "시뮬 모드이므로 데이타 확인후 완료처리");
                            TrayPosition = 80;
                            Simul_In_IOArray[2] = true;
                            NextCVCommand = eCVCommand.TrayUnload;
                            return new CV_ActionResult(ModuleName, eCV_ActionResult.Complete, "OHT -> Port Tray Load  완료");
                        }
                        Thread.Sleep(LocalStepCycleDelay);
                    }
                }
                #endregion

                //장비의 트레이 로딩을 대기
                if (IsTrayExist()) //로딩스탭인데 트레이가 있어선 안된다.
                {
                    GlobalData.Current.Alarm_Manager.AlarmOccur(IsInPort ? "508" : "507", ModuleName); //재하감지 에러 발생.
                    return new CV_ActionResult(ModuleName, eCV_ActionResult.Aborted, "Load 명령이나 이미 트레이가 존재합니다.");
                }
                ReportModule_LCS();
                OHT_PIOResult result = OHTLoadPIO();
                switch (result.PIOResult)
                {
                    case eOHT_PIOResult.Complete:
                        NextCVCommand = eCVCommand.SendTray;
                        return new CV_ActionResult(ModuleName, eCV_ActionResult.Complete, "OHT->Port 트레이 로딩 정상 완료");

                    case eOHT_PIOResult.Aborted:
                        NextCVCommand = eCVCommand.ErrorHandling;
                        return new CV_ActionResult(ModuleName, eCV_ActionResult.Aborted, "OHT->Port 작업 중단.");

                    case eOHT_PIOResult.TimeOut:
                        NextCVCommand = eCVCommand.ErrorHandling;
                        return new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "OHT->Port 트레이 로딩 타임아웃 발생.");

                    case eOHT_PIOResult.ErrorOccured:
                        NextCVCommand = eCVCommand.ErrorHandling;
                        return new CV_ActionResult(ModuleName, eCV_ActionResult.TimeOut, "OHT->Port 트레이 로딩 에러 발생.");
                    default:
                        return errorResult;
                }
            }
            else
            {
                try //OutPort면 로봇 로드 대기
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "{0} Module Tray Load Start", this.ModuleName);
                    CurrentActionDesc = "트레이 로드 동작으로 진입합니다.";

                    ClearInternalSignals();
                    //로봇의 트레이 로딩을 대기
                    if (IsTrayExist()) //로딩스탭인데 트레이가 있어선 안된다.
                    {

                        ReportModule_LCS();
                        GlobalData.Current.Alarm_Manager.AlarmOccur(IsInPort ? "508" : "507", ModuleName); //재하감지 에러 발생.
                        return new CV_ActionResult(ModuleName, eCV_ActionResult.Aborted, "Load 명령이나 이미 트레이가 존재합니다.");
                    }
                    else
                    {
                        ReportModule_LCS();
                        RobotAccessAble = true; //트레이가 없을때만 로봇 엑세스 가능비트 ON
                    }
                    //로봇이 동작 완료후 트레이 로드 플래그 변경 대기.
                    CurrentActionDesc = "RM 로딩을 대기합니다.";
                    while (true)
                    {
                        if (AutoManual != eCVAutoManual.Auto) //대기중에 Auto 상태 해제되면 종료처리하고 빠져나옴
                        {
                            NextCVCommand = eCVCommand.TrayLoad;
                            ReportModule_LCS();
                            return new CV_ActionResult(ModuleName, eCV_ActionResult.Complete, "대기중에 Auto 상태가 해제 되었습니다.");
                        }
                        if (TrayLoaded)
                        {
                            CurrentActionDesc = "로드 완료 신호를 받았습니다.RM 동작 완료를 대기합니다.";
                            RobotAccessAble = false;
                            bool RMStateCheck = CheckRobotActionDone();
                            if (RMStateCheck)
                            {
                                TrayLoaded = false;
                                LogManager.WriteConsoleLog(eLogLevel.Info, "{0} 으로 로봇이 트레이 로딩하였습니다. ", this.ModuleName);
                                break;
                            }
                            else
                            {
                                continue;
                            }
                        }
                        Thread.Sleep(200);
                    }

                    if (UseRFID && !IsInPort) //RFID Write 로직 추가.
                    {
                        if (TrayExistByData())
                        {
                            string WriteData = CurrentTray.GetReportTagID();
                            if (!string.IsNullOrEmpty(WriteData) && WriteData.Length > 8)
                            {
                                WriteData = WriteData.Substring(8);//CarrierID 제외하고 Write
                                LogManager.WriteConsoleLog(eLogLevel.Info, "{0} 모듈 CarrierID:{1}  Data:{2}     RFID 쓰기를 시도합니다.", this.ModuleName, CurrentTray.CarrierID, WriteData);
                                bool WriteResult = CVRFID_Write(WriteData); //쓰기 명령 발신

                                if (WriteResult)
                                {
                                    LogManager.WriteConsoleLog(eLogLevel.Info, "{0} 모듈 CarrierID: {1}  RFID 쓰기에 성공했습니다.", this.ModuleName, CurrentTray.CarrierID);
                                }
                                else
                                {
                                    GlobalData.Current.Alarm_Manager.AlarmOccurbyName("RFID_FAIL", ModuleName);
                                    return new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "RFID 쓰기 도중 에러가 발생했습니다.");
                                }
                            }
                        }
                    }

                    //로딩완료후 리퀘스트 OFF
                    TrayLoaded = false; //로딩 플래그 해제
                    ReportModule_LCS();
                    NextCVCommand = eCVCommand.TrayUnload;
                    return new CV_ActionResult(ModuleName, eCV_ActionResult.Complete, "RM 트레이 로딩에 성공하였습니다.");
                }
                catch (Exception ex)
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
                    return new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "동작중 프로그램 예외가 발생하였습니다.");
                }
                finally
                {
                    RobotAccessAble = false;
                }
            }


        }

        protected override CV_ActionResult TrayUnloadAction()
        {

            LogManager.WriteConsoleLog(eLogLevel.Info, "{0} Module Tray Unload Start", this.ModuleName);
            CurrentActionDesc = "트레이 언로드 동작으로 진입합니다.";
            if (IsInPort) //로봇 트레이 언로딩 대기
            {
                try
                {
                    //배출전 정위치 및 얼라인 확인
                    if (!CheckTrayUnloadingPosition())
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "{0} 트레이 언로딩 위치가 올바르지 않습니다.", this.ModuleName);
                        return new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "트레이 언로딩 위치가 올바르지 않습니다.");
                    }
                    //로봇의 트레이 로딩을 대기
                    if (IsTrayExist()) //언로딩스탭이면 트레이가 있어야 한다.
                    {
                        ReportModule_LCS();
                        RobotAccessAble = true; //트레이가 없을때만 로봇 엑세스 가능비트 ON

                    }
                    else
                    {
                        ReportModule_LCS();
                        GlobalData.Current.Alarm_Manager.AlarmOccur(IsInPort ? "508" : "507", ModuleName); //재하감지 에러 발생.
                        return new CV_ActionResult(ModuleName, eCV_ActionResult.Aborted, "Unload 명령이나  트레이가 없습니다..");

                    }

                    //로봇의 트레이 언로딩을 대기
                    TrayUnloaded = false;
                    //로봇이 동작 완료후 트레이 언로드 플래그 변경 대기
                    CurrentActionDesc = "RM 언로딩을 대기합니다.";
                    while (true)
                    {
                        if (AutoManual != eCVAutoManual.Auto)
                        {
                            //CV_RunStop();
                            NextCVCommand = eCVCommand.TrayUnload;
                            return new CV_ActionResult(ModuleName, eCV_ActionResult.Complete, "Auto 상태가 해제되었습니다..");
                        }

                        if (TrayUnloaded)
                        {
                            CurrentActionDesc = "언로드 완료 신호를 받았습니다.RM 동작 완료를 대기합니다.";
                            bool RMStateCheck = CheckRobotActionDone();
                            bool TrayNotExistCheck = !TrayExistBySensor();
                            if (RMStateCheck && TrayNotExistCheck)
                            {
                                TrayUnloaded = false;
                                RemoveTray();
                                LogManager.WriteConsoleLog(eLogLevel.Info, "{0} 으로 로봇이 트레이 언로딩하였습니다. ", this.ModuleName);
                                break;
                            }
                            else
                            {
                                continue;
                            }
                        }
                        Thread.Sleep(200);
                    }
                    //여기서 LCS 보고
                    ReportModule_LCS();
                    NextCVCommand = eCVCommand.TrayLoad;
                    return new CV_ActionResult(ModuleName, eCV_ActionResult.Complete, "RM 트레이 언로딩에 성공하였습니다.");
                }
                catch (Exception ex)
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
                    return new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "동작중 프로그램 예외가 발생하였습니다.");
                }
                finally
                {
                    RobotAccessAble = false;
                }
            }
            else
            {
                //OutPort면 OHT PIO 대기
                #region Simulation 전용
                if (SimulMode)
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "CV 모듈: {0} 시뮬 모드로  트레이는 3초후 자동으로 삭제됩니다.", this.ModuleName);
                    Thread.Sleep(3000);
                    RemoveTray();
                    ReportModule_LCS(); // 트레이 삭제 보고
                    NextCVCommand = eCVCommand.ReceiveTray;
                    LogManager.WriteConsoleLog(eLogLevel.Info, "CV 모듈: {0} 트레이가 삭제 되었습니다.", this.ModuleName);
                    return new CV_ActionResult(ModuleName, eCV_ActionResult.Complete, "OHT -> Port Tray UnLoad  완료");
                }
                #endregion

                LocalActionStep = 0;
                CV_ActionResult errorResult = new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "동작중 에러 발생하였습니다.");
                ClearInternalSignals();
                //OHT의 트레이 언로딩을 대기
                if (!IsTrayExist()) //언로딩스탭인데 트레이가 없어선 안된다.
                {

                    GlobalData.Current.Alarm_Manager.AlarmOccur(IsInPort ? "508" : "507", ModuleName); //재하감지 에러 발생.
                    return new CV_ActionResult(ModuleName, eCV_ActionResult.Aborted, "UnLoad 명령이나 트레이가 없습니다.");
                }
                ReportModule_LCS();
                OHT_PIOResult result = OHTUnloadPIO();
                switch (result.PIOResult)
                {
                    case eOHT_PIOResult.Complete:
                        NextCVCommand = eCVCommand.ReceiveTray;
                        return new CV_ActionResult(ModuleName, eCV_ActionResult.Complete, "OHT->Port 트레이 언로딩 정상 완료");

                    case eOHT_PIOResult.Aborted:
                        NextCVCommand = eCVCommand.ErrorHandling;
                        return new CV_ActionResult(ModuleName, eCV_ActionResult.Aborted, "OHT->Port 언로딩 작업 중단.");

                    case eOHT_PIOResult.TimeOut:
                        NextCVCommand = eCVCommand.ErrorHandling;
                        return new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "OHT->Port 트레이 언로딩 타임아웃 발생.");

                    case eOHT_PIOResult.ErrorOccured:
                        NextCVCommand = eCVCommand.ErrorHandling;
                        return new CV_ActionResult(ModuleName, eCV_ActionResult.TimeOut, "OHT->Port 트레이 언로딩 에러 발생.");
                    default:
                        return errorResult;
                }
            }

        }
        public override bool DoAbnormalCheck()
        {
            if (!CheckOHTSafetyZone())
            {
                Internal_ToNextCV_Error = true;
                Internal_ToPrevCV_Error = true;
                ErrorOccurred = true;
                return true;
            }
            return false;
        }

        /// <summary>
        /// OHT 구동 영역 간섭 체크
        /// </summary>
        /// <returns>
        /// true 안전 간섭 없음
        /// false 위험 간섭 감지
        /// </returns>
        public bool CheckOHTSafetyZone()
        {
            if (SimulMode)
            {
                return true;
            }
            bool bSafety1 = CCLinkManager.CCLCurrent.ReadIO(GlobalData.Current.MainBooth.ModuleName, "IN_SAFETY_AREA1");
            bool bSafety2 = CCLinkManager.CCLCurrent.ReadIO(GlobalData.Current.MainBooth.ModuleName, "IN_SAFETY_AREA2");
            bool bSafety3 = CCLinkManager.CCLCurrent.ReadIO(GlobalData.Current.MainBooth.ModuleName, "IN_SAFETY_AREA3");
            bool bSafety4 = CCLinkManager.CCLCurrent.ReadIO(GlobalData.Current.MainBooth.ModuleName, "IN_SAFETY_POWER");

            return bSafety1 && bSafety2 && bSafety3 && bSafety4;

        }


        #region OHT PIO Step Function

        public bool PIORecoverySelect(eOHTPIORecoveryOption opt)
        {
            //현재 에러 복구 스텝일때만 유효함
            if (CurrentStep == eOHTPIOStep.LoadErrorRecovery_Start || CurrentStep == eOHTPIOStep.UnloadErrorRecovery_Start)
            {
                if (RecoveryOption == eOHTPIORecoveryOption.None)
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "PIO 복구 옵션 :{0} 선택됨", opt);
                    RecoveryOption = opt;
                    return true;
                }
                else //이미 복구 옵션을 선택했으면 처리가 끝날때 까지 변경 불가
                {
                    return false;
                }
            }
            return false;
        }

        public void ClearOHTPIOBits()
        {
            OUT_OHTIF_ABORT = false;
            OUT_OHTIF_LREQ = false;
            OUT_OHTIF_READY = false;
            OUT_OHTIF_UREQ = false;
        }

        private void LoadStep00_Start()
        {
            CurrentActionDesc = "OHT Load PIOStep: 0  OHT Vaild 신호를 대기합니다.";
            if (AutoManual != eCVAutoManual.Auto)
            {
                CurrentStep = eOHTPIOStep.LoadStep00_Start; //오토 해제 되면 여기서 홀딩
                return;
            }
            //OHT PIO 전 비트 초기화
            ClearOHTPIOBits();
            //동작전 내부에 트레이가 없어야 한다.
            if (this.IsTrayExist())
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "Load 동작이나 내부에 이미 Tray 가 있어서 종료");
                //스텝 시작 에서 트레이 On시 에러
                CurrentStep = eOHTPIOStep.Step_Done;
                return;
            }
            //동작전 전후방 스톱퍼 닫히 상태로 대기 한다.
            if (GetCVStopperState(true) != eCV_StopperState.Up)
            {
                if (!CVStopperClose(true))
                {
                    CurrentStep = eOHTPIOStep.LoadErrorRecovery_Start;
                }
            }
            if (GetCVStopperState(false) != eCV_StopperState.Up)
            {
                if (!CVStopperClose(false))
                {
                    CurrentStep = eOHTPIOStep.LoadErrorRecovery_Start;
                }
            }

            //OHT Valid 상태 확인
            if (!IN_OHTIF_VALID)
            {
                //LogManager.WriteConsoleLog(eLogLevel.Info, "OHT [Valid Off] 체크");
                //스텝 시작 에서 Valid off 시 에러없이 종료한다.
                //CurrentStep = eOHTPIOStep.Step_Done;
                return;
            }
            //CS Bit 상태 확인
            if (!IN_OHTIF_CS_0 && !IN_OHTIF_CS_1 && !IN_OHTIF_CS_2 && !IN_OHTIF_CS_3)
            {
                //LogManager.WriteConsoleLog(eLogLevel.Info, "OHT [CS All Off] 체크");
                //스텝 시작 에서 CS Bit All off 시 에러없이 종료한다.
                //CurrentStep = eOHTPIOStep.Step_Done;
                return;
            }
            //동작전 전방 스톱퍼 열린 상태여야 한다.
            if (!CVStopperOpen(true))
            {
                CurrentStep = eOHTPIOStep.LoadErrorRecovery_Start;
                return;
            }
            if (!CVStopperOpen(false))
            {
                CurrentStep = eOHTPIOStep.LoadErrorRecovery_Start;
                return;
            }

            //라이트 커튼 해제
            if (!LightCurtainMuteControl(true))
            {
                GlobalData.Current.Alarm_Manager.AlarmOccur("510", ModuleName); //라이튼 커튼 뮤트 실패
                CurrentStep = eOHTPIOStep.LoadErrorRecovery_Start;
                return;
            }

            //LoadReq ON
            this.OUT_OHTIF_LREQ = true;
            CurrentStep = eOHTPIOStep.LoadStep01_Wait_TRReq;
        }
        private void LoadStep01_Wait_TRReq()
        {
            CurrentActionDesc = "OHT Load PIOStep: 1  TR_Req 신호를 대기합니다.";
            bool bExit = false;
            DateTime dt = DateTime.Now;
            while (!bExit)
            {
                if (ActionAbortRequested)
                {
                    //CV_RunStop();
                    ActionAbortRequested = false;
                    CurrentStep = eOHTPIOStep.LoadErrorRecovery_Start;
                    return;
                }
                if (IsTimeOut(dt, T1Timeout))
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "T1 타임아웃 체크");
                    GlobalData.Current.Alarm_Manager.AlarmOccurbyName("EQ_INTERFACE_ERROR", this.ModuleName);
                    // 타임 아웃 발생시  에러스텝으로 보낸다.
                    CurrentStep = eOHTPIOStep.LoadErrorRecovery_Start;
                    return;
                }
                //OHT Valid 상태 확인
                if (!IN_OHTIF_VALID)
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "OHT [Valid Off] 체크");
                    GlobalData.Current.Alarm_Manager.AlarmOccurbyName("EQ_INTERFACE_ERROR", this.ModuleName);
                    //Valid off 시  에러스텝으로 보낸다.
                    CurrentStep = eOHTPIOStep.LoadErrorRecovery_Start;
                    return;
                }
                //CS Bit 상태 확인
                if (!IN_OHTIF_CS_0 && !IN_OHTIF_CS_1 && !IN_OHTIF_CS_2 && !IN_OHTIF_CS_3)
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "OHT [CS All Off] 체크");
                    GlobalData.Current.Alarm_Manager.AlarmOccurbyName("EQ_INTERFACE_ERROR", this.ModuleName);
                    //All CS off 시  에러스텝으로 보낸다.
                    CurrentStep = eOHTPIOStep.LoadErrorRecovery_Start;
                    return;
                }
                if (IN_OHTIF_TR_REQ)
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "[TR_REQ ON] Check");
                    ////동작전 전방 스톱퍼 열린 상태여야 한다.
                    //if (!CVStopperOpen(true))
                    //{
                    //    CurrentStep = eOHTPIOStep.LoadErrorRecovery_Start;
                    //    return;
                    //}
                    //if (!CVStopperOpen(false))
                    //{
                    //    CurrentStep = eOHTPIOStep.LoadErrorRecovery_Start;
                    //    return;
                    //}

                    //라이트 커튼 해제
                    if (!LightCurtainMuteControl(true))
                    {
                        GlobalData.Current.Alarm_Manager.AlarmOccur("510", ModuleName); //라이튼 커튼 뮤트 실패
                        CurrentStep = eOHTPIOStep.LoadErrorRecovery_Start;
                        return;
                    }

                    //Ready bit On
                    OUT_OHTIF_READY = true;
                    CurrentStep = eOHTPIOStep.LoadStep02_Wait_BusyOn;
                    return;
                }
                Thread.Sleep(LocalStepCycleDelay);
            }
        }
        private void LoadStep02_Wait_BusyOn()
        {
            CurrentActionDesc = "OHT Load PIOStep: 2  Busy 신호를 대기합니다.";
            bool bExit = false;
            DateTime dt = DateTime.Now;
            while (!bExit)
            {
                if (ActionAbortRequested)
                {
                    //CV_RunStop();
                    ActionAbortRequested = false;
                    CurrentStep = eOHTPIOStep.LoadErrorRecovery_Start;
                    return;
                }
                if (IsTimeOut(dt, T3Timeout))
                {

                    LogManager.WriteConsoleLog(eLogLevel.Info, "T3 타임아웃 체크");
                    GlobalData.Current.Alarm_Manager.AlarmOccurbyName("EQ_INTERFACE_ERROR", this.ModuleName);
                    // 타임 아웃 발생시  에러스텝으로 보낸다.
                    CurrentStep = eOHTPIOStep.LoadErrorRecovery_Start;
                    return;
                }
                //OHT Valid 상태 확인
                if (!IN_OHTIF_VALID)
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "OHT [Valid Off] 체크");
                    GlobalData.Current.Alarm_Manager.AlarmOccurbyName("EQ_INTERFACE_ERROR", this.ModuleName);
                    //Valid off 시 에러스텝으로 보낸다.
                    CurrentStep = eOHTPIOStep.LoadErrorRecovery_Start;
                    return;
                }
                //CS Bit 상태 확인
                if (!IN_OHTIF_CS_0 && !IN_OHTIF_CS_1 && !IN_OHTIF_CS_2 && !IN_OHTIF_CS_3)
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "OHT [CS All Off] 체크");
                    GlobalData.Current.Alarm_Manager.AlarmOccurbyName("EQ_INTERFACE_ERROR", this.ModuleName);
                    //All CS off 시  에러스텝으로 보낸다.
                    CurrentStep = eOHTPIOStep.LoadErrorRecovery_Start;
                    return;
                }
                if (IN_OHTIF_BUSY)
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "[BUSY ON] Check");
                    CurrentStep = eOHTPIOStep.LoadStep03_Wait_OHTComplete;
                    return;
                }
                Thread.Sleep(LocalStepCycleDelay);
            }
        }
        private void LoadStep03_Wait_OHTComplete()
        {
            CurrentActionDesc = "OHT Load PIOStep: 3  Complete 신호를 대기합니다.";
            bool bExit = false;
            bool LogWrote = false; //로그 한번만 찍도록 변경.
            DateTime dt = DateTime.Now;
            while (!bExit)
            {
                if (ActionAbortRequested)
                {
                    //CV_RunStop();
                    ActionAbortRequested = false;
                    CurrentStep = eOHTPIOStep.LoadErrorRecovery_Start;
                    return;
                }
                if (IsTimeOut(dt, OHT_ActionTimeout))
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "OHT Trans 타임아웃 체크");
                    GlobalData.Current.Alarm_Manager.AlarmOccurbyName("EQ_INTERFACE_ERROR", this.ModuleName);
                    // 타임 아웃 발생시  에러스텝으로 보낸다.
                    CurrentStep = eOHTPIOStep.LoadErrorRecovery_Start;
                    return;
                }
                //트레이 감지 되면 L_REQ Off

                if (IsTrayExist())
                {
                    if (LogWrote == false)
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "트레이 감지 체크 L_REQ Off");
                        LogWrote = true;
                    }

                    OUT_OHTIF_LREQ = false;
                }
                //OHT Valid 상태 확인
                if (!IN_OHTIF_VALID)
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "OHT [Valid Off] 체크");
                    GlobalData.Current.Alarm_Manager.AlarmOccurbyName("EQ_INTERFACE_ERROR", this.ModuleName);
                    //Valid off 시 에러스텝으로 보낸다.
                    CurrentStep = eOHTPIOStep.LoadErrorRecovery_Start;
                    return;
                }
                //CS Bit 상태 확인
                if (!IN_OHTIF_CS_0 && !IN_OHTIF_CS_1 && !IN_OHTIF_CS_2 && !IN_OHTIF_CS_3)
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "OHT [CS All Off] 체크");
                    GlobalData.Current.Alarm_Manager.AlarmOccurbyName("EQ_INTERFACE_ERROR", this.ModuleName);
                    //All CS off 시  에러스텝으로 보낸다.
                    CurrentStep = eOHTPIOStep.LoadErrorRecovery_Start;
                    return;
                }

                if (!IN_OHTIF_BUSY && !IN_OHTIF_TR_REQ && IN_OHTIF_COMPLETE)
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "[BUSY Off] [TR_REQ Off] [COMPLETE On] 확인 ");
                    //동작 완료되면 레디 Off
                    OUT_OHTIF_READY = false;
                    CurrentStep = eOHTPIOStep.LoadStep04_Wait_AllOff;
                    return;
                }
                Thread.Sleep(LocalStepCycleDelay);
            }
        }
        private void LoadStep04_Wait_AllOff()
        {
            CurrentActionDesc = "OHT Load PIOStep: 4  OHT 신호 OFF 를 대기합니다.";
            bool bExit = false;
            DateTime dt = DateTime.Now;
            while (!bExit)
            {
                if (ActionAbortRequested)
                {
                    //CV_RunStop();
                    ActionAbortRequested = false;
                    CurrentStep = eOHTPIOStep.LoadErrorRecovery_Start;
                    return;
                }
                if (IsTimeOut(dt, T6Timeout))
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "T6 타임아웃 체크");
                    GlobalData.Current.Alarm_Manager.AlarmOccurbyName("EQ_INTERFACE_ERROR", this.ModuleName);
                    // 타임 아웃 발생시  에러스텝으로 보낸다.
                    CurrentStep = eOHTPIOStep.LoadErrorRecovery_Start;
                    return;
                }
                if (!IN_OHTIF_VALID && !IN_OHTIF_COMPLETE && !IN_OHTIF_CS_0 && !IN_OHTIF_CS_1 && !IN_OHTIF_CS_2 && !IN_OHTIF_CS_3)
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "[VALID Off] [CS All Off] [COMPLETE Off] 확인 ");

                    if (!LightCurtainMuteControl(false))//라이트 커튼 작동
                    {
                        GlobalData.Current.Alarm_Manager.AlarmOccur("510", ModuleName); //라이튼 커튼 뮤트 실패
                        PIOCompleted = false;
                        CurrentStep = eOHTPIOStep.Step_Done;
                        return;
                    }

                    PIOCompleted = true;
                    CurrentStep = eOHTPIOStep.Step_Done;
                    return;
                }
                Thread.Sleep(LocalStepCycleDelay);
            }
        }
        private void LoadErrorRecovery_Start()
        {
            CurrentActionDesc = "OHT Load PIOStep: Error  에러처리를 대기합니다";
            //Port 에 Tray 있을시
            //OHT Error Reset → OHT Auto(다음 명령 수행) → EQ PIO 통신 Reset → EQ Auto(반송 물 작업 시작)

            //OHT 에 Tray 있을시
            //OHT Error Reset → PIO 통신 Reset → EQ Auto → OHT Auto(Loading 재시도)
            //리커버리 명령 대기

            OUT_OHTIF_ABORT = true; //PIO 중지 비트를 ON

            if (RecoveryRequeset)
            {
                RecoveryOption = eOHTPIORecoveryOption.PIO_ForceComplete;
            }


            if (RecoveryOption == eOHTPIORecoveryOption.None)
            {
                return;
            }
            else if (RecoveryOption == eOHTPIORecoveryOption.PIO_ForceComplete)   //강제 종료
            {
                if (IsTrayExist()) //트레이가 있으면 정상완료로 간주.
                {
                    ClearOHTPIOBits();
                    PIOCompleted = true;
                    CurrentStep = eOHTPIOStep.Step_Done;
                    RecoveryOption = eOHTPIORecoveryOption.None;
                    return;
                }
                else //대기 상태로 간다.
                {
                    ClearOHTPIOBits();
                    CurrentStep = eOHTPIOStep.LoadStep00_Start;
                    RecoveryOption = eOHTPIORecoveryOption.None;
                    return;
                }
            }
            else if (RecoveryOption == eOHTPIORecoveryOption.PIO_Restart) //재시작
            {
                if (IsTrayExist()) //이미 트레이가 있으면 재시작 불가.
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "Load PIO 중 PIO_Restart 시도하였으나 이미 Tray 가 존재합니다.");
                    RecoveryOption = eOHTPIORecoveryOption.None;
                    return;
                }
                else //다시 시작스텝으로 되돌림
                {
                    ClearOHTPIOBits();
                    CurrentStep = eOHTPIOStep.LoadStep00_Start;
                    RecoveryOption = eOHTPIORecoveryOption.None;
                    return;
                }
            }
        }


        private void UnloadStep00_Start()
        {
            CurrentActionDesc = "OHT Unload PIOStep: 0  OHT Vaild 신호를 대기합니다.";
            if (AutoManual != eCVAutoManual.Auto)
            {
                CurrentStep = eOHTPIOStep.UnloadStep00_Start; //오토 해제 되면 여기서 홀딩
                return;
            }
            //OHT PIO 전 비트 초기화
            ClearOHTPIOBits();
            //동작전 내부에 트레이가 있어야 한다.
            if (!this.IsTrayExist())
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "Unload 동작이나 내부에  Tray 가 없어서 종료");
                //스텝 시작 에서 트레이 Off 시 에러없이 종료한다.
                CurrentStep = eOHTPIOStep.Step_Done;
            }
            ////동작전 전후방 스톱퍼 닫히 상태로 대기 한다.
            //if (GetCVStopperState(true) != eCV_StopperState.Up)
            //{
            //    if (!CVStopperClose(true))
            //    {
            //        CurrentStep = eOHTPIOStep.UnloadErrorRecovery_Start;
            //    }
            //}
            //if (GetCVStopperState(false) != eCV_StopperState.Up)
            //{
            //    if (!CVStopperClose(false))
            //    {
            //        CurrentStep = eOHTPIOStep.UnloadErrorRecovery_Start;
            //    }
            //}

            //OHT Valid 상태 확인
            if (!IN_OHTIF_VALID)
            {
                //LogManager.WriteConsoleLog(eLogLevel.Info, "OHT [Valid Off] 체크");
                //스텝 시작 에서 Valid off 시 에러없이 종료한다.
                return;
            }
            //CS Bit 상태 확인
            if (!IN_OHTIF_CS_0 && !IN_OHTIF_CS_1 && !IN_OHTIF_CS_2 && !IN_OHTIF_CS_3)
            {
                //LogManager.WriteConsoleLog(eLogLevel.Info, "OHT [CS All Off] 체크");
                //스텝 시작 에서 CS Bit All off 시 에러없이 종료한다.
                return;
            }
            ////동작전 전방 스톱퍼 열린 상태여야 한다.
            //if (!CVStopperOpen(true))
            //{
            //    CurrentStep = eOHTPIOStep.UnloadErrorRecovery_Start;
            //    return;
            //}
            //if (!CVStopperOpen(false))
            //{
            //    CurrentStep = eOHTPIOStep.UnloadErrorRecovery_Start;
            //    return;
            //}

            //라이트 커튼 해제
            if (!LightCurtainMuteControl(true))
            {
                GlobalData.Current.Alarm_Manager.AlarmOccur("510", ModuleName); //라이튼 커튼 뮤트 실패
                CurrentStep = eOHTPIOStep.UnloadErrorRecovery_Start;
                return;
            }

            //UnloadReq ON
            this.OUT_OHTIF_UREQ = true;
            CurrentStep = eOHTPIOStep.UnloadStep01_Wait_TRReq;
        }
        private void UnloadStep01_Wait_TRReq()
        {
            CurrentActionDesc = "OHT Unload PIOStep: 1  TR_Req 신호를 대기합니다.";
            bool bExit = false;
            DateTime dt = DateTime.Now;
            while (!bExit)
            {
                if (ActionAbortRequested)
                {
                    //CV_RunStop();
                    ActionAbortRequested = false;
                    CurrentStep = eOHTPIOStep.UnloadErrorRecovery_Start;
                    return;
                }
                if (IsTimeOut(dt, T1Timeout))
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "T1 타임아웃 체크");
                    GlobalData.Current.Alarm_Manager.AlarmOccurbyName("EQ_INTERFACE_ERROR", this.ModuleName);
                    // 타임 아웃 발생시  에러스텝으로 보낸다.
                    CurrentStep = eOHTPIOStep.UnloadErrorRecovery_Start;
                    return;
                }
                //OHT Valid 상태 확인
                if (!IN_OHTIF_VALID)
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "OHT [Valid Off] 체크");
                    GlobalData.Current.Alarm_Manager.AlarmOccurbyName("EQ_INTERFACE_ERROR", this.ModuleName);
                    //Valid off 시  에러스텝으로 보낸다.
                    CurrentStep = eOHTPIOStep.UnloadErrorRecovery_Start;
                    return;
                }
                //CS Bit 상태 확인
                if (!IN_OHTIF_CS_0 && !IN_OHTIF_CS_1 && !IN_OHTIF_CS_2 && !IN_OHTIF_CS_3)
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "OHT [CS All Off] 체크");
                    GlobalData.Current.Alarm_Manager.AlarmOccurbyName("EQ_INTERFACE_ERROR", this.ModuleName);
                    //All CS off 시  에러스텝으로 보낸다.
                    CurrentStep = eOHTPIOStep.UnloadErrorRecovery_Start;
                    return;
                }
                if (IN_OHTIF_TR_REQ)
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "[TR_REQ ON] Check");
                    ////동작전 전방 스톱퍼 열린 상태여야 한다.
                    //if (!CVStopperOpen(true))
                    //{
                    //    CurrentStep = eOHTPIOStep.UnloadErrorRecovery_Start;
                    //    return;
                    //}
                    //if (!CVStopperOpen(false))
                    //{
                    //    CurrentStep = eOHTPIOStep.UnloadErrorRecovery_Start;
                    //    return;
                    //}

                    //라이트 커튼 해제
                    if (!LightCurtainMuteControl(true))
                    {
                        GlobalData.Current.Alarm_Manager.AlarmOccur("510", ModuleName); //라이튼 커튼 뮤트 실패
                        CurrentStep = eOHTPIOStep.UnloadErrorRecovery_Start;
                        return;
                    }
                    //Ready bit On
                    OUT_OHTIF_READY = true;
                    CurrentStep = eOHTPIOStep.UnloadStep02_Wait_BusyOn;
                    return;
                }
                Thread.Sleep(LocalStepCycleDelay);
            }
        }
        private void UnloadStep02_Wait_BusyOn()
        {
            CurrentActionDesc = "OHT Unload PIOStep: 1  Busy 신호를 대기합니다.";
            bool bExit = false;
            DateTime dt = DateTime.Now;
            while (!bExit)
            {
                if (ActionAbortRequested)
                {
                    //CV_RunStop();
                    ActionAbortRequested = false;
                    CurrentStep = eOHTPIOStep.UnloadErrorRecovery_Start;
                    return;
                }
                if (IsTimeOut(dt, T3Timeout))
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "T3 타임아웃 체크");
                    GlobalData.Current.Alarm_Manager.AlarmOccurbyName("EQ_INTERFACE_ERROR", this.ModuleName);
                    // 타임 아웃 발생시  에러스텝으로 보낸다.
                    CurrentStep = eOHTPIOStep.UnloadErrorRecovery_Start;
                    return;
                }
                //OHT Valid 상태 확인
                if (!IN_OHTIF_VALID)
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "OHT [Valid Off] 체크");
                    GlobalData.Current.Alarm_Manager.AlarmOccurbyName("EQ_INTERFACE_ERROR", this.ModuleName);
                    //Valid off 시 에러스텝으로 보낸다.
                    CurrentStep = eOHTPIOStep.UnloadErrorRecovery_Start;
                    return;
                }
                //CS Bit 상태 확인
                if (!IN_OHTIF_CS_0 && !IN_OHTIF_CS_1 && !IN_OHTIF_CS_2 && !IN_OHTIF_CS_3)
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "OHT [CS All Off] 체크");
                    GlobalData.Current.Alarm_Manager.AlarmOccurbyName("EQ_INTERFACE_ERROR", this.ModuleName);
                    //All CS off 시  에러스텝으로 보낸다.
                    CurrentStep = eOHTPIOStep.UnloadErrorRecovery_Start;
                    return;
                }
                if (IN_OHTIF_BUSY)
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "[BUSY ON] Check");
                    CurrentStep = eOHTPIOStep.UnloadStep03_Wait_OHTComplete;
                    return;
                }
                Thread.Sleep(LocalStepCycleDelay);
            }
        }
        private void UnloadStep03_Wait_OHTComplete()
        {
            CurrentActionDesc = "OHT Unload PIOStep: 3  Complete 신호를 대기합니다.";
            bool bExit = false;
            bool LogWrote = false; //로그 한번만 찍도록 변경.
            DateTime dt = DateTime.Now;
            while (!bExit)
            {
                if (ActionAbortRequested)
                {
                    //CV_RunStop();
                    ActionAbortRequested = false;
                    CurrentStep = eOHTPIOStep.UnloadErrorRecovery_Start;
                    return;
                }
                if (IsTimeOut(dt, OHT_ActionTimeout))
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "OHT Trans 타임아웃 체크");
                    GlobalData.Current.Alarm_Manager.AlarmOccurbyName("EQ_INTERFACE_ERROR", this.ModuleName);
                    // 타임 아웃 발생시  에러스텝으로 보낸다.
                    CurrentStep = eOHTPIOStep.UnloadErrorRecovery_Start;
                    return;
                }
                //트레이 감지 안되면 U_REQ Off
                if (!IsTrayExist())
                {
                    if (LogWrote == false)
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "트레이 비감지 체크 U_REQ Off");
                        LogWrote = true;
                    }

                    OUT_OHTIF_UREQ = false;
                }
                //OHT Valid 상태 확인
                if (!IN_OHTIF_VALID)
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "OHT [Valid Off] 체크");
                    GlobalData.Current.Alarm_Manager.AlarmOccurbyName("EQ_INTERFACE_ERROR", this.ModuleName);
                    //Valid off 시 에러스텝으로 보낸다.
                    CurrentStep = eOHTPIOStep.UnloadErrorRecovery_Start;
                    return;
                }
                //CS Bit 상태 확인
                if (!IN_OHTIF_CS_0 && !IN_OHTIF_CS_1 && !IN_OHTIF_CS_2 && !IN_OHTIF_CS_3)
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "OHT [CS All Off] 체크");
                    GlobalData.Current.Alarm_Manager.AlarmOccurbyName("EQ_INTERFACE_ERROR", this.ModuleName);
                    //All CS off 시  에러스텝으로 보낸다.
                    CurrentStep = eOHTPIOStep.UnloadErrorRecovery_Start;
                    return;
                }

                if (!IN_OHTIF_BUSY && !IN_OHTIF_TR_REQ && IN_OHTIF_COMPLETE)
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "[BUSY Off] [TR_REQ Off] [COMPLETE On] 확인 ");
                    //동작 완료되면 레디 Off
                    OUT_OHTIF_READY = false;
                    CurrentStep = eOHTPIOStep.UnloadStep04_Wait_AllOff;
                    return;
                }
                Thread.Sleep(LocalStepCycleDelay);
            }
        }
        private void UnloadStep04_Wait_AllOff()
        {
            CurrentActionDesc = "OHT Unload PIOStep: 4  OHT 신호 OFF 를 대기합니다.";
            bool bExit = false;
            DateTime dt = DateTime.Now;
            while (!bExit)
            {
                if (ActionAbortRequested)
                {
                    //CV_RunStop();
                    ActionAbortRequested = false;
                    CurrentStep = eOHTPIOStep.UnloadErrorRecovery_Start;
                    return;
                }
                if (IsTimeOut(dt, T6Timeout))
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "T6 타임아웃 체크");
                    GlobalData.Current.Alarm_Manager.AlarmOccurbyName("EQ_INTERFACE_ERROR", this.ModuleName);
                    // 타임 아웃 발생시  에러스텝으로 보낸다.
                    CurrentStep = eOHTPIOStep.UnloadErrorRecovery_Start;
                    return;
                }
                if (!IN_OHTIF_VALID && !IN_OHTIF_COMPLETE && !IN_OHTIF_CS_0 && !IN_OHTIF_CS_1 && !IN_OHTIF_CS_2 && !IN_OHTIF_CS_3)
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "[VALID Off] [CS All Off] [COMPLETE Off] 확인 ");

                    if (!LightCurtainMuteControl(false))//라이트 커튼 작동
                    {
                        GlobalData.Current.Alarm_Manager.AlarmOccur("510", ModuleName); //라이튼 커튼 뮤트 실패
                        PIOCompleted = false;
                        CurrentStep = eOHTPIOStep.Step_Done;
                        return;
                    }

                    PIOCompleted = true;
                    CurrentStep = eOHTPIOStep.Step_Done;
                    return;
                }
                Thread.Sleep(LocalStepCycleDelay);
            }
        }
        private void UnloadErrorRecovery_Start()
        {
            CurrentActionDesc = "OHT Unload PIOStep: Error  에러처리를 대기합니다";
            //Port 에 Tray 있을시
            //OHT Error Reset → PIO 통신 Reset → EQ Auto → OHT Auto(Unloading 재시도)

            //OHT 에 Tray 있을시
            //OHT Error Reset → OHT Auto(다음 명령 수행) → EQ PIO 통신 Reset → EQ Auto

            OUT_OHTIF_ABORT = true; //PIO 중지 비트를 ON

            if (RecoveryRequeset) //사용자 리셋 요청이 들어오면
            {
                RecoveryOption = eOHTPIORecoveryOption.PIO_ForceComplete;
            }

            if (RecoveryOption == eOHTPIORecoveryOption.None)
            {
                return;
            }
            else if (RecoveryOption == eOHTPIORecoveryOption.PIO_ForceComplete)   //강제 종료
            {
                if (!IsTrayExist()) // 트레이가 없으면 정상완료로 간주.
                {
                    ClearOHTPIOBits();
                    PIOCompleted = true;
                    CurrentStep = eOHTPIOStep.Step_Done;
                    RecoveryOption = eOHTPIORecoveryOption.None;
                    return;
                }
                else //대기 상태로 간다.
                {
                    ClearOHTPIOBits();
                    CurrentStep = eOHTPIOStep.UnloadStep00_Start;
                    RecoveryOption = eOHTPIORecoveryOption.None;
                    return;
                }
            }
            else if (RecoveryOption == eOHTPIORecoveryOption.PIO_Restart) //재시작
            {
                if (IsTrayExist()) //이미 트레이가 없으면 재시작 불가.
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "Unload PIO 중 PIO_Restart 시도하였으나 이미 Tray 가 없습니다.");
                    RecoveryOption = eOHTPIORecoveryOption.None;
                    return;
                }
                else //다시 시작스텝으로 되돌림
                {
                    ClearOHTPIOBits();
                    CurrentStep = eOHTPIOStep.UnloadStep00_Start;
                    RecoveryOption = eOHTPIORecoveryOption.None;
                    return;
                }
            }
        }
        #endregion

        #region OHT IF 입력 접점

        private bool IN_OHTIF_VALID
        {
            get
            {
                if (SimulMode)
                {
                    return Simul_In_IOArray[0];
                }
                bool bValue = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "OHTIF_VALID");
                return bValue;
            }
            set
            {
                if (SimulMode)
                {
                    Simul_In_IOArray[0] = value;
                    return;
                }
                else
                {
                    //throw new Exception("입력값에다 쓸수 없습니다.");
                    //INPUT에 쓸수없지만 로그에만 남겨둔다.
                    Log.LogManager.WriteConsoleLog(eLogLevel.Error, "잘못된 I/O 엑세스.입력 IN_OHTIF_VALID 를 쓰기 시도했습니다.");
                }
            }
        }
        private bool IN_OHTIF_CS_0
        {
            get
            {
                if (SimulMode)
                {
                    return Simul_In_IOArray[1];
                }
                bool bValue = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "OHTIF_CS_0");
                return bValue;
            }
            set
            {
                if (SimulMode)
                {
                    Simul_In_IOArray[1] = value;
                    return;
                }
                else
                {
                    //throw new Exception("입력값에다 쓸수 없습니다.");
                    //INPUT에 쓸수없지만 로그에만 남겨둔다.
                    Log.LogManager.WriteConsoleLog(eLogLevel.Error, "잘못된 I/O 엑세스.입력 IN_OHTIF_CS_0 를 쓰기 시도했습니다.");
                }
            }
        }
        private bool IN_OHTIF_CS_1
        {
            get
            {
                if (SimulMode)
                {
                    return Simul_In_IOArray[2];
                }
                bool bValue = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "OHTIF_CS_1");
                return bValue;
            }
            set
            {
                if (SimulMode)
                {
                    Simul_In_IOArray[2] = value;
                    return;
                }
                else
                {
                    //throw new Exception("입력값에다 쓸수 없습니다.");
                    //INPUT에 쓸수없지만 로그에만 남겨둔다.
                    Log.LogManager.WriteConsoleLog(eLogLevel.Error, "잘못된 I/O 엑세스.입력 IN_OHTIF_CS_1 를 쓰기 시도했습니다.");
                }
            }
        }
        private bool IN_OHTIF_CS_2
        {
            get
            {
                if (SimulMode)
                {
                    return Simul_In_IOArray[3];
                }
                bool bValue = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "OHTIF_CS_2");
                return bValue;
            }
            set
            {
                if (SimulMode)
                {
                    Simul_In_IOArray[3] = value;
                    return;
                }
                else
                {
                    //throw new Exception("입력값에다 쓸수 없습니다.");
                    //INPUT에 쓸수없지만 로그에만 남겨둔다.
                    Log.LogManager.WriteConsoleLog(eLogLevel.Error, "잘못된 I/O 엑세스.입력 IN_OHTIF_CS_2 를 쓰기 시도했습니다.");
                }
            }
        }
        private bool IN_OHTIF_CS_3
        {
            get
            {
                if (SimulMode)
                {
                    return Simul_In_IOArray[4];
                }
                bool bValue = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "OHTIF_CS_3");
                return bValue;
            }
            set
            {
                if (SimulMode)
                {
                    Simul_In_IOArray[4] = value;
                    return;
                }
                else
                {
                    //throw new Exception("입력값에다 쓸수 없습니다.");
                    //INPUT에 쓸수없지만 로그에만 남겨둔다.
                    Log.LogManager.WriteConsoleLog(eLogLevel.Error, "잘못된 I/O 엑세스.입력 IN_OHTIF_CS_3 를 쓰기 시도했습니다.");
                }
            }
        }
        private bool IN_OHTIF_TR_REQ
        {
            get
            {
                if (SimulMode)
                {
                    return Simul_In_IOArray[5];
                }
                bool bValue = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "OHTIF_TR_REQ");
                return bValue;
            }
            set
            {
                if (SimulMode)
                {
                    Simul_In_IOArray[5] = value;
                    return;
                }
                else
                {
                    //throw new Exception("입력값에다 쓸수 없습니다.");
                    //INPUT에 쓸수없지만 로그에만 남겨둔다.
                    Log.LogManager.WriteConsoleLog(eLogLevel.Error, "잘못된 I/O 엑세스.입력 IN_OHTIF_TR_REQ 를 쓰기 시도했습니다.");
                }
            }
        }
        private bool IN_OHTIF_BUSY
        {
            get
            {
                if (SimulMode)
                {
                    return Simul_In_IOArray[6];
                }
                bool bValue = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "OHTIF_BUSY");
                return bValue;
            }
            set
            {
                if (SimulMode)
                {
                    Simul_In_IOArray[6] = value;
                    return;
                }
                else
                {
                    //throw new Exception("입력값에다 쓸수 없습니다.");
                    //INPUT에 쓸수없지만 로그에만 남겨둔다.
                    Log.LogManager.WriteConsoleLog(eLogLevel.Error, "잘못된 I/O 엑세스.입력 IN_OHTIF_BUSY 를 쓰기 시도했습니다.");
                }
            }
        }
        private bool IN_OHTIF_COMPLETE
        {
            get
            {
                if (SimulMode)
                {
                    return Simul_In_IOArray[7];
                }
                bool bValue = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "OHTIF_COMPLETE");
                return bValue;
            }
            set
            {
                if (SimulMode)
                {
                    Simul_In_IOArray[7] = value;
                    return;
                }
                else
                {
                    //throw new Exception("입력값에다 쓸수 없습니다.");
                    //INPUT에 쓸수없지만 로그에만 남겨둔다.
                    Log.LogManager.WriteConsoleLog(eLogLevel.Error, "잘못된 I/O 엑세스.입력 IN_OHTIF_COMP 를 쓰기 시도했습니다.");
                }
            }
        }
        #endregion

        #region OHT IF 출력 접점
        private bool OUT_OHTIF_LREQ
        {
            get
            {
                if (SimulMode)
                {
                    return Simul_Out_IOArray[0];
                }
                bool bValue = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "OHTIF_LOADREQ");
                return bValue;
            }
            set
            {
                if (SimulMode)
                {
                    Simul_Out_IOArray[0] = value;
                    return;
                }
                CCLinkManager.CCLCurrent.WriteIO(ModuleName, "OHTIF_LOADREQ", value);
            }
        }
        private bool OUT_OHTIF_UREQ
        {
            get
            {
                if (SimulMode)
                {
                    return Simul_Out_IOArray[1];
                }
                bool bValue = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "OHTIF_UNLOADREQ");
                return bValue;
            }
            set
            {
                if (SimulMode)
                {
                    Simul_Out_IOArray[1] = value;
                    return;
                }
                CCLinkManager.CCLCurrent.WriteIO(ModuleName, "OHTIF_UNLOADREQ", value);
            }
        }
        private bool OUT_OHTIF_READY
        {
            get
            {
                if (SimulMode)
                {
                    return Simul_Out_IOArray[3];
                }
                bool bValue = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "OHTIF_READY");
                return bValue;
            }
            set
            {
                if (SimulMode)
                {
                    Simul_Out_IOArray[3] = value;
                    return;
                }
                CCLinkManager.CCLCurrent.WriteIO(ModuleName, "OHTIF_READY", value);
            }
        }
        private bool OUT_OHTIF_ABORT
        {
            get
            {
                if (SimulMode)
                {
                    return Simul_Out_IOArray[6];
                }
                bool bValue = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "OHTIF_ABORT");
                return bValue;
            }
            set
            {
                if (SimulMode)
                {
                    Simul_Out_IOArray[6] = value;
                    return;
                }
                CCLinkManager.CCLCurrent.WriteIO(ModuleName, "OHTIF_ABORT", value);
            }
        }

        #endregion

        public override void CV_ErrorReset()
        {
            bool Reset = CVRunner.CV_Reset(); //인버터 리셋
            if (Reset)
            {
                if (TrayExist)
                {
                    ReportShuttleReset_LCS();// Contain 항목만 따로 보고
                    Thread.Sleep(300);
                    if (UseRFID)
                    {
                        string RFIDReadValue = CVRFID_Read();//RFID 읽기
                        UpdateTrayTagID(RFIDReadValue);
                    }
                }
                ERRORSTAUTS = "0"; //보고 상태 리셋
                ERRORCODE = "0";

                Internal_ToNextCV_Error = false;
                Internal_ToPrevCV_Error = false;
                ErrorOccurred = false;
            }
            RecoveryRequeset = true; //에러상태 복구요청
            GlobalData.Current.WCF_mgr.ReportPortStatus(ModuleName);
            GlobalData.Current.WCF_mgr.ReportShuttleStatus(ModuleName, false); //OHT 포트는 리셋 보고시 셔틀 보고 추가.
        }

    }
}
