using System;
using System.Threading;
using WCF_LBS.DataParameter;
using Stockerfirmware.CCLink;
using Stockerfirmware.Log;
using Stockerfirmware.DataList.CV;

namespace Stockerfirmware.Modules.Conveyor
{
    /// <summary>
    /// OHT 와 인터페이스 하는 컨베이어 모듈
    /// </summary>
    public class CV_OHTIFModule : CV_BaseModule
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
                if(_CurrentStep != value)
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "CurrentModule:{0} CurrentStep:{1} NextStep:{2} ", ModuleName, _CurrentStep,value);
                    //스텝이 바뀌면 로그를 찍는다.
                    _CurrentStep = value;
                }
            }
        }


        public CV_OHTIFModule(string mName, bool simul) : base(mName, simul)
        {
            CVModuleType = eCVType.OHTIF;
        }
        /// <summary>
        /// 스톱퍼를 업(클로즈) 동작 제어.
        /// </summary>
        /// <param name="ForwardStopper"> True 정방향기준 앞쪽의 스톱퍼를 UP 한다.</param>
        /// <returns></returns>
        public override bool CVStopperClose(bool ForwardStopper = true)
        {
            if (!UseStopper) //스톱퍼 미사용 모듈은 바로리턴.
            {
                return true;
            }
            if (GetCVStopperState(ForwardStopper) == eCV_StopperState.Up) //상태 봐서 동작 완료되어있으면 동작 필요 없음
            {
                return true;
            }
            if (SimulMode)
            {
                Thread.Sleep(500);
                if (ForwardStopper)
                {
                    LifeTime_Stopper_FWD_CylinderCounter++; //소모품 관리 변수 업데이트
                    LastFWD_StopperState = eCV_StopperState.Up;
                }
                else
                {
                    LifeTime_Stopper_BWD_CylinderCounter++; //소모품 관리 변수 업데이트
                    LastBWD_StopperState = eCV_StopperState.Up;
                }
                return true;
            }
            if(CheckOHTArmDetected()) //Arm 감지되면 스톱퍼 동작 금지
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "OHT Arm 감지되어 스톱퍼 동작 중단합니다.");
                return false; 
            }
            CCLinkManager.CCLCurrent.WriteIO(ModuleName, string.Format("CV_STOPPER_{0}_DOWN", ForwardStopper ? "FWD" : "BWD"), false);
            Thread.Sleep(IODelay);
            CCLinkManager.CCLCurrent.WriteIO(ModuleName, string.Format("CV_STOPPER_{0}_UP", ForwardStopper ? "FWD" : "BWD"), true);
            DateTime dt = DateTime.Now;
            while (!IsTimeOut(dt, StopperUpdownTimeout))
            {
                if (GetCVStopperState(ForwardStopper) == eCV_StopperState.Up)
                {
                    Thread.Sleep(IODelay);
                    //헌팅성을 제외하기 위해 한번더 확인.
                    if (GetCVStopperState(ForwardStopper) == eCV_StopperState.Up)
                    {
                        if (ForwardStopper)
                        {
                            LifeTime_Stopper_FWD_CylinderCounter++; //소모품 관리 변수 업데이트
                            LastFWD_StopperState = eCV_StopperState.Up;
                        }
                        else
                        {
                            LifeTime_Stopper_BWD_CylinderCounter++; //소모품 관리 변수 업데이트
                            LastBWD_StopperState = eCV_StopperState.Up;
                        }
                        return true;
                    }
                }
                Thread.Sleep(IODelay);
            }
            GlobalData.Current.Alarm_Manager.AlarmOccurbyName("STOPPER_MOTION_ERROR", ModuleName);
            return false; // 타임아웃이내 실행 실패.
        }
        public override bool CheckOHTArmDetected()
        {
            if(SimulMode)
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
                    CV_RunStop();
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
                        if(PIOCompleted)  //정상종료
                        {
                            LogManager.WriteConsoleLog(eLogLevel.Info, "OHTLoadAction() Exit!");
                            InsertTray(new Tray("ERROR", true)); //더미 트레이 생성

                            //센터링 작업 시작
                            CVStopperClose(true);
                            CVStopperClose(false);

                            Thread.Sleep(200);
                            ReportModule_LCS();
                            return new OHT_PIOResult(true,this.ModuleName, eOHT_PIOResult.Complete, "");
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
            return new OHT_PIOResult(true,this.ModuleName, eOHT_PIOResult.Aborted, "");

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
                    CV_RunStop();
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
                    if (AutoManual != eCVAutoManual.Auto && NextCVCommand != eCVCommand.ErrorHandling) //에러핸들링은 바로 들어간다.
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
                        case eCVCommand.ReceiveTray:
                            Result = ReceiveTrayAction();
                            break;
                        case eCVCommand.SendTray:
                            Result = SendTrayAction();
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
            RemoveTray();
            CurrentActionDesc = "초기화 동작중입니다.";
            LocalActionStep = 0;
            //인버터 에러 체크
            if (!CVRunner.CV_Reset())
            {
                //인버터 에러 리셋 실패하면 Error
                GlobalData.Current.Alarm_Manager.AlarmOccurbyName("INVERTER_RUN_ERROR", ModuleName);
                return new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "인버터 에러 리셋에 실패하였습니다.");
            }
            if(CheckOHTArmDetected())
            {
                //OHT Arm 감지 되면 초기화 중단.
                GlobalData.Current.Alarm_Manager.AlarmOccurbyName("SHUTTLE_POSITION_ERROR", ModuleName);
                return new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "OHT Arm 감지되어 초기화 실패하였습니다..");
            }
            //트레이 유무 재하 상태 체크
            if (IsTrayExist())
            {
                if (IN_ENTRY && IN_STOP)  //정위치 OK
                {
                    ReportModule_LCS();
                    if (CurrentTray == null)
                    {
                        //트레이 데이터가 없다면 빈 트레이 생성
                        InsertTray(new Tray("ERROR", true));
                    }
                    if (IsInPort)
                    {
                        NextCVCommand = eCVCommand.SendTray;
                    }
                    else
                    {
                        if (UseRFID) //RFID 다시 찍는다.
                        {
                            string RFIDReadValue = CVRFID_Read();//RFID 읽기
                            UpdateTrayTagID(RFIDReadValue);
                        }
                        NextCVCommand = eCVCommand.TrayUnload;
                    }
                   
                }
                else //하나만 들어왔으면 초기화 불가
                {
                    GlobalData.Current.Alarm_Manager.AlarmOccur(IsInPort ? "508" : "507", ModuleName); //재하감지 에러 발생.
                    return new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "초기 재하 센서 값이 이상합니다.");
                }           
            }
            else 
            {
                RemoveTray();//혹시 트레이 데이터 남아있으면 삭제.
                if (IsInPort) //InPort 면 트레이 로딩대기
                {
                    NextCVCommand = eCVCommand.TrayLoad;
                }
                else
                {
                    //OutPort 면 트레이 수신대기
                    NextCVCommand = eCVCommand.ReceiveTray;
                }
                GlobalData.Current.WCF_mgr.ReportPortStatus(ModuleName); //초기화 보고 추가.
            }
            return new CV_ActionResult(ModuleName, eCV_ActionResult.Complete, "InitializeAction 완료.");
        }

        protected override CV_ActionResult TrayLoadAction()
        {
            LogManager.WriteConsoleLog(eLogLevel.Info, "{0} Module Tray Load Start", this.ModuleName);
            CurrentActionDesc = "트레이 로드 동작으로 진입합니다.";
            LocalActionStep = 0;
            CV_ActionResult errorResult = new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "동작중 에러 발생하였습니다.");
            ClearInternalSignals();
            //OHT의 트레이 로딩을 대기
            if (IsTrayExist()) //로딩스탭인데 트레이가 있어선 안된다.
            {
                GlobalData.Current.Alarm_Manager.AlarmOccur(IsInPort ? "508" : "507", ModuleName); //재하감지 에러 발생.
                return new CV_ActionResult(ModuleName, eCV_ActionResult.Aborted, "Load 명령이나 이미 트레이가 존재합니다.");
            }
            ReportModule_LCS();
            OHT_PIOResult result = OHTLoadPIO();
            switch(result.PIOResult)
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
                    return new CV_ActionResult(ModuleName, eCV_ActionResult.TimeOut, "OOHT->Port 트레이 로딩 에러 발생.");
                default:
                    return errorResult;
            }
        }
        protected override CV_ActionResult TrayUnloadAction()
        {
            LogManager.WriteConsoleLog(eLogLevel.Info, "{0} Module Tray Unload Start", this.ModuleName);
            CurrentActionDesc = "트레이 언로드 동작으로 진입합니다.";
            if (SimulMode)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "CV 모듈: {0} 시뮬 모드로  트레이는 3초후 자동으로 삭제됩니다.", this.ModuleName);
                Thread.Sleep(3000);
                RemoveTray();
                ReportModule_LCS(); // 트레이 삭제 보고
                NextCVCommand = eCVCommand.ReceiveTray;
                LogManager.WriteConsoleLog(eLogLevel.Info, "CV 모듈: {0} 트레이가 삭제 되었습니다.", this.ModuleName);
                return new CV_ActionResult(ModuleName, eCV_ActionResult.Complete, "OHT -> Port Tray Load  완료");
            }
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
                    return new CV_ActionResult(ModuleName, eCV_ActionResult.TimeOut, "OOHT->Port 트레이 언로딩 에러 발생.");
                default:
                    return errorResult;
            }
        }
        protected override CV_ActionResult ReceiveTrayAction()
        {
            LogManager.WriteConsoleLog(eLogLevel.Info, "{0} Module ReceiveTrayAction Start", this.ModuleName);
            CurrentActionDesc = "트레이 수신 동작으로 진입합니다.";
            RemoveTray(); //혹시 데이타가 남아있으면 삭제
            ReportModule_LCS();
            bool bStepEnd = false;
            LocalActionStep = 0;
            CV_ActionResult errorResult = new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "동작중 에러 발생하였습니다.");
            if (IsTrayExist()) //받기 스탭인데 트레이가 있어선 안된다.
            {
                GlobalData.Current.Alarm_Manager.AlarmOccur(IsInPort ? "508" : "507", ModuleName); //재하감지 에러 발생.
                return new CV_ActionResult(ModuleName, eCV_ActionResult.Aborted, "Receive 명령이나 트레이가 존재합니다.");
            }
            while (!bStepEnd)
            {
                if (AutoManual != eCVAutoManual.Auto && LocalActionStep <= 1)
                {
                    CV_RunStop();
                    NextCVCommand = eCVCommand.ReceiveTray;
                    return new CV_ActionResult(ModuleName, eCV_ActionResult.Complete, "Auto 상태가 해제되었습니다..");
                }
                if (DoAbnormalCheck())
                {
                    //문제 발생
                    Internal_ToNextCV_Error = true;
                    Internal_ToPrevCV_Error = true;
                    CV_RunStop();
                    LocalActionStep = ErrorStep;
                }
                if (ActionAbortRequested)
                {
                    CV_RunStop();
                    ActionAbortRequested = false;
                    return new CV_ActionResult(ModuleName, eCV_ActionResult.Aborted, "전송 작업이 취소되었습니다.");
                }
                switch (LocalActionStep)
                {
                    case 0: //스톱퍼 업 동작
                        CurrentActionDesc = "스톱퍼 업 동작중";
                        ClearInternalSignals();
                        if (UseStopper)
                        {
                            if (CVStopperClose(true) && CVStopperOpen(false)) //정방향 끝 스톱퍼는 닫고 정방향 시작 스톱퍼는 연다.
                            {
                                LocalActionStep++;
                            }
                            else
                            {
                                //스톱퍼  동작실패 
                                errorResult = new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "Stopper  동작 실패하였습니다.");
                                LocalActionStep = ErrorStep;
                            }
                        }
                        else
                        {
                            LocalActionStep++;
                        }
                        break;
                    case 1: //컨베이어 PIO 시작
                        CurrentActionDesc = "전 컨베이어 배출 요청 대기중";
                        if (PrevCV.Internal_ToNextCV_UnloadRequest && !PrevCV.Internal_ToNextCV_Error) //배출 요청 확인
                        {
                            if (UseStopper)
                            {
                                if (CVStopperClose(true) && CVStopperOpen(false)) //정방향 끝 스톱퍼는 닫고 정방향 시작 스톱퍼는 연다.
                                {
                                    //체크만 하고 넘어간다.
                                }
                                else
                                {
                                    //스톱퍼  동작실패 
                                    errorResult = new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "Stopper  동작 실패하였습니다.");
                                    LocalActionStep = ErrorStep;
                                }
                            }
                            //받는 컨베이어에서 먼저 컨베이어 가동
                            bool result = CVForwardRun(eCV_Speed.High);
                            if (result)
                            {
                                this.Internal_ToPrevCV_LoadRequest = true; //진입 허가
                                LocalActionStep++;
                            }
                            else
                            {
                                errorResult = new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "컨베이어 Forward 동작 실패하였습니다.");
                                LocalActionStep = ErrorStep;
                            }
                        }
                        break;
                    case 2:  //배출 완료 시그널 대기
                        CurrentActionDesc = "전 컨베이어 배출 완료 대기중";
                        if (IsTimeOut(StepTimeOutDT, TrayTransTimeout)) // 스텝 타임아웃 체크
                        {
                            LogManager.WriteConsoleLog(eLogLevel.Info, "Module:{0} Step:{1} ReceiveTrayAction Time Out 발생", ModuleName, LocalActionStep);
                            CV_RunStop();
                            if (UseAutoRecovery)
                            {
                                LogManager.WriteConsoleLog(eLogLevel.Info, "자동 복구 모드 이므로 처음 스텝으로 되돌아 갑니다.");
                                LocalActionStep = 0;
                            }
                            else
                            {
                                GlobalData.Current.Alarm_Manager.AlarmOccur("501", ModuleName); //트레이 타임 오버
                                LocalActionStep = ErrorStep;
                            }
                            break;
                        }
                        //완료전 배출 컨베이어에 에러 체크
                        if (PrevCV.Internal_ToNextCV_Error)
                        {
                            CV_RunStop(); //
                            LocalActionStep = ErrorStep;
                        }
                        //배출 완료 시그널 체크
                        if (PrevCV.Internal_ToNextCV_OutComplete)
                        {
                            //Entry 입력도착 확인 STOP 조건 추가.
                            if (IN_ENTRY || IN_STOP)
                            {   
                                ReportModule_LCS(); //도착시 트레이 보고
                                LocalActionStep++;
                            }
                        }
                        break;
                    
                    case 3: //감속 위치까지 트레이 전송
                        CurrentActionDesc = "감속 위치로 전송중";
                        if (IsTimeOut(StepTimeOutDT, TrayTransTimeout)) // 스텝 타임아웃 체크
                        {
                            LogManager.WriteConsoleLog(eLogLevel.Info, "Module:{0} Step:{1} ReceiveTrayAction Time Out 발생", ModuleName, LocalActionStep);
                            CV_RunStop();
                            if (UseAutoRecovery)
                            {
                                LogManager.WriteConsoleLog(eLogLevel.Info, "자동 복구 모드 이므로 처음 스텝으로 되돌아 갑니다.");
                                LocalActionStep = 0;
                            }
                            else
                            {
                                GlobalData.Current.Alarm_Manager.AlarmOccur("501", ModuleName); //트레이 타임 오버
                                LocalActionStep = ErrorStep;
                            }
                            break;
                        }
                        if (IN_SLOW) //감속위치 확인
                        {
                            CV_RunStop();
                            LocalActionStep++;
                        }
                        break;
                    case 4:
                        CurrentActionDesc = "트레이 데이터 체크중";
                        if (IsTimeOut(StepTimeOutDT, TrayTransTimeout)) // 스텝 타임아웃 체크
                        {
                            errorResult = new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "Tray 데이터가 전송되지 않았습니다.");
                            LocalActionStep = ErrorStep;
                        }
                        if (CurrentTray != null) //이전 모듈이 전달한 Tray 데이터 확인
                        {
                            LogManager.WriteConsoleLog(eLogLevel.Info, "CV :{0} 트레이 ID:{0})", CurrentTray.CarrierID);
                            if (UseRFID)
                            {
                                string RFIDReadValue = CVRFID_Read();//RFID 읽기
                                UpdateTrayTagID(RFIDReadValue);
                            }
                            bool result = CVForwardRun(eCV_Speed.Low); //감속으로 보낸다.
                            if (result)
                            {
                                LocalActionStep++;
                            }
                            else
                            {
                                errorResult = new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "컨베이어 Forward 동작 실패하였습니다.");
                                LocalActionStep = ErrorStep;
                            }
                        }
                        break;
                    case 5: //정지 위치까지 트레이 전송
                        CurrentActionDesc = "정지 위치로 전송중";
                        if (IsTimeOut(StepTimeOutDT, TrayTransTimeout)) // 스텝 타임아웃 체크
                        {
                            LogManager.WriteConsoleLog(eLogLevel.Info, "Module:{0} Step:{1} ReceiveTrayAction Time Out 발생", ModuleName, LocalActionStep);
                            CV_RunStop();
                            if (UseAutoRecovery)
                            {
                                LogManager.WriteConsoleLog(eLogLevel.Info, "자동 복구 모드 이므로 처음 스텝으로 되돌아 갑니다.");
                                LocalActionStep = 0;
                            }
                            else
                            {
                                GlobalData.Current.Alarm_Manager.AlarmOccur("501", ModuleName); //트레이 타임 오버
                                LocalActionStep = ErrorStep;
                            }
                            break;
                        }
                        if (IN_STOP) //정지위치 확인
                        {
                            this.Internal_ToPrevCV_InComplete = true; //도착 완료
                            Thread.Sleep(ReceiveStopDelay);
                            CV_RunStop();
                            LocalActionStep++;
                        }
                        break;
                    case 6: //배출 컨베이어 신호 OFF 확인
                        CurrentActionDesc = "전 컨베이어 신호 OFF 대기중";
                        if (IsTimeOut(StepTimeOutDT, TrayTransTimeout)) // 스텝 타임아웃 체크
                        {
                            LogManager.WriteConsoleLog(eLogLevel.Info, "Module:{0} Step:{1} ReceiveTrayAction Time Out 발생", ModuleName, LocalActionStep);
                            CV_RunStop();
                            if (UseAutoRecovery)
                            {
                                LogManager.WriteConsoleLog(eLogLevel.Info, "자동 복구 모드 이므로 처음 스텝으로 되돌아 갑니다.");
                                LocalActionStep = 0;
                            }
                            else
                            {
                                GlobalData.Current.Alarm_Manager.AlarmOccur("501", ModuleName); //트레이 타임 오버
                                LocalActionStep = ErrorStep;
                            }
                            break;
                        }
                        if (!PrevCV.Internal_ToNextCV_OutComplete && !PrevCV.Internal_ToNextCV_UnloadRequest)
                        {
                            Internal_ToPrevCV_InComplete = false; //신호 OFF
                            Internal_ToPrevCV_LoadRequest = false; //신호 OFF
                            LocalActionStep++;
                        }
                        break;
                    case 7: //Tray 수신 프로세스 완료
                        CurrentActionDesc = "트레이 수신완료";
                        bStepEnd = true;
                        NextCVCommand = eCVCommand.TrayUnload;
                        LogManager.WriteConsoleLog(eLogLevel.Info, "CV :{0} 트레이 수신 프로세스 완료)", ModuleName);
                        break;
                    case ErrorStep: //에러발생
                        CurrentActionDesc = "트레이 수신중 에러 발생 에러 스탭 :" + _LocalRecoveryStep;
                        return errorResult;
                    default:
                        return new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "CV_ActionProcess 동작중 유효한 Step 이 아닙니다.");
                }
                Thread.Sleep(LocalStepCycleDelay);
            }
            return new CV_ActionResult(ModuleName, eCV_ActionResult.Complete, "Tray 수신 완료");
        }
        protected override CV_ActionResult SendTrayAction()
        {
            LogManager.WriteConsoleLog(eLogLevel.Info, "{0} Module SendTrayAction Start", this.ModuleName);
            CurrentActionDesc = "트레이 배출 동작으로 진입합니다.";
            bool bStepEnd = false;
            LocalActionStep = 0;
            CV_ActionResult errorResult = new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "동작중 에러 발생하였습니다.");
            if (!IsTrayExist()) //보내기 스탭인데 트레이가 없어선 안된다.
            {
                GlobalData.Current.Alarm_Manager.AlarmOccur(IsInPort ? "508" : "507", ModuleName); //재하감지 에러 발생.
                return new CV_ActionResult(ModuleName, eCV_ActionResult.Aborted, "Send 명령이나 트레이가 없습니다.");
            }
            while (!bStepEnd)
            {
                if (AutoManual != eCVAutoManual.Auto && LocalActionStep <= 1)
                {
                    CV_RunStop();
                    NextCVCommand = eCVCommand.SendTray;
                    return new CV_ActionResult(ModuleName, eCV_ActionResult.Complete, "Auto 상태가 해제되었습니다..");
                }
                if (DoAbnormalCheck())
                {
                    //문제 발생
                    Internal_ToNextCV_Error = true;
                    Internal_ToPrevCV_Error = true;
                    CV_RunStop();
                    LocalActionStep = ErrorStep;
                }
                if (ActionAbortRequested)
                {
                    CV_RunStop();
                    ActionAbortRequested = false;
                    return new CV_ActionResult(ModuleName, eCV_ActionResult.Aborted, "전송 작업이 취소되었습니다.");
                }
                switch (LocalActionStep)
                {
                    case 0: //다음 CV모듈로  UnloadRequest On
                        CurrentActionDesc = "다음 컨베이어로 배출 요청중 ";
                        ClearInternalSignals();

                        this.Internal_ToNextCV_UnloadRequest = true;
                        LocalActionStep++;
                        
                        break;
                    case 1: //진입 허가 대기
                        CurrentActionDesc = "다음 컨베이어 쪽 진입 허가 대기중 ";
                        if (NextCV.Internal_ToPrevCV_LoadRequest && !NextCV.Internal_ToPrevCV_Error)
                        {
                            //허가 받고 스톱퍼 동작
                            if (UseStopper)
                            {
                                if (CVStopperClose(false) && CVStopperOpen(true)) //정방향 끝 스톱퍼는 열고 정방향 시작 스톱퍼는 닫는다..
                                {
                                    this.Internal_ToNextCV_UnloadRequest = true;
                                }
                                else
                                {
                                    Thread.Sleep(1000);
                                    if (CVStopperClose(false) && CVStopperOpen(true)) //정방향 끝 스톱퍼는 열고 정방향 시작 스톱퍼는 닫는다..
                                    {
                                        this.Internal_ToNextCV_UnloadRequest = true;
                                    }
                                    else
                                    {
                                        //스톱퍼  동작실패 
                                        errorResult = new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "Stopper  동작 실패하였습니다.");
                                        LocalActionStep = ErrorStep;
                                    }
                                }
                            }
                            Thread.Sleep(200);
                            //허가 받고 컨베이어 가동
                            bool result = CVForwardRun(eCV_Speed.High);
                            if (result)
                            {
                                LocalActionStep++;
                            }
                            else
                            {
                                errorResult = new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "컨베이어 Forward 동작 실패하였습니다.");
                                LocalActionStep = ErrorStep;
                            }
                        }
                        break;
                    case 2: //Stop 신호 Off 면 배출되었다고 본다.
                        CurrentActionDesc = "트레이 배출 진행중";
                        if (IsTimeOut(StepTimeOutDT, TrayTransTimeout)) // 스텝 타임아웃 체크
                        {
                            LogManager.WriteConsoleLog(eLogLevel.Info, "Module:{0} Step:{1} SendTrayAction Time Out 발생", ModuleName, LocalActionStep);
                            CV_RunStop();
                            if (UseAutoRecovery)
                            {
                                LogManager.WriteConsoleLog(eLogLevel.Info, "자동 복구 모드 이므로 처음 스텝으로 되돌아 갑니다.");
                                LocalActionStep = 0;
                            }
                            else
                            {
                                GlobalData.Current.Alarm_Manager.AlarmOccur("501", ModuleName); //트레이 타임 오버
                                LocalActionStep = ErrorStep;
                            }
                            break;
                        }
                        if (!IN_STOP)
                        {
                            Thread.Sleep(TraySendRemainDelay);
                            Internal_ToNextCV_OutComplete = true;
                            CV_RunStop();
                            NextCV.InsertTray(RemoveTray());// 트레이 정보를 옮겨준다.
                            ReportModule_LCS(); //배출시 보고
                            LocalActionStep++;
                        }
                        break;
                    case 3: //진입 완료 신호 대기
                        CurrentActionDesc = "다음 컨베이어 쪽 진입 완료 신호 대기중";
                        if (IsTimeOut(StepTimeOutDT, TrayTransTimeout)) // 스텝 타임아웃 체크
                        {
                            LogManager.WriteConsoleLog(eLogLevel.Info, "Module:{0} Step:{1} SendTrayAction Time Out 발생", ModuleName, LocalActionStep);
                            CV_RunStop();
                            if (UseAutoRecovery)
                            {
                                LogManager.WriteConsoleLog(eLogLevel.Info, "자동 복구 모드 이므로 스텝 SendTray Action 자동완료처리.");
                                bStepEnd = true; //반송 프로세스 완료
                                NextCVCommand = eCVCommand.TrayLoad;
                            }
                            else
                            {
                                GlobalData.Current.Alarm_Manager.AlarmOccur("501", ModuleName); //트레이 타임 오버
                                LocalActionStep = ErrorStep;
                            }
                            break;
                        }
                        if (NextCV.Internal_ToPrevCV_InComplete && !NextCV.Internal_ToPrevCV_Error)
                        {
                            //트레이가 진입완료 했으므로 PIO 신호 OFF
                            this.Internal_ToNextCV_OutComplete = false;
                            this.Internal_ToNextCV_UnloadRequest = false;
                            LocalActionStep++;
                        }
                        break;
                    case 4: //수신 컨베이어 신호 Off 대기
                        CurrentActionDesc = "다음 컨베이어 쪽 신호 OFF 대기중";
                        if (IsTimeOut(StepTimeOutDT, TrayTransTimeout)) // 스텝 타임아웃 체크
                        {
                            LogManager.WriteConsoleLog(eLogLevel.Info, "Module:{0} Step:{1} SendTrayAction Time Out 발생", ModuleName, LocalActionStep);
                            CV_RunStop();
                            if (UseAutoRecovery)
                            {
                                LogManager.WriteConsoleLog(eLogLevel.Info, "자동 복구 모드 이므로 스텝 SendTray Action 자동완료처리.");
                                bStepEnd = true; //반송 프로세스 완료
                                NextCVCommand = eCVCommand.TrayLoad;
                            }
                            else
                            {
                                GlobalData.Current.Alarm_Manager.AlarmOccur("501", ModuleName); //트레이 타임 오버
                                LocalActionStep = ErrorStep;
                            }
                            break;
                        }
                        if (!NextCV.Internal_ToPrevCV_InComplete && !NextCV.Internal_ToPrevCV_LoadRequest) //신호 OFF 확인
                        {
                            //정상 OFF 했으므로  PIO 종료
                            LocalActionStep++;
                        }
                        break;
                    case 5:
                        CurrentActionDesc = "트레이 배출 완료";
                        bStepEnd = true; //반송 프로세스 완료
                        LogManager.WriteConsoleLog(eLogLevel.Info, "CV :{0} 트레이 배출완료)", ModuleName);
                        NextCVCommand = eCVCommand.TrayLoad;
                        break;
                    case ErrorStep: //에러발생
                        CurrentActionDesc = "트레이 배출중 에러 발생 에러 스탭 :" + _LocalRecoveryStep;
                        return errorResult;
                    default:
                        return new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "CV_ActionProcess 동작중 유효한 Step 이 아닙니다.");
                }
                Thread.Sleep(LocalStepCycleDelay);
            }
            return new CV_ActionResult(ModuleName, eCV_ActionResult.Complete, "Tray 배출 완료");
        }

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

        #region OHT PIO Step Function

        private void LoadStep00_Start()
        {
            CurrentActionDesc = "OHT Load PIOStep: 0  OHT Vaild 신호를 대기합니다.";
            if (AutoManual != eCVAutoManual.Auto)
            {
                CurrentStep = eOHTPIOStep.LoadStep00_Start; //오토 해제 되면 여기서 홀딩
                return;
            }
            ClearOHTPIOBits();// 210617 RGJ 스탭 시작시 OHT 비트 초기화.
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
                if(!CVStopperClose(false))
                {
                    CurrentStep = eOHTPIOStep.LoadErrorRecovery_Start;
                }
            }
            //OHT Valid 상태 확인
            if(!IN_OHTIF_VALID)
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
                    CV_RunStop();
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
                    CV_RunStop();
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
                    CV_RunStop();
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
                    if(LogWrote == false)
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
                
                if (!IN_OHTIF_BUSY && !IN_OHTIF_TR_REQ && IN_OHTIF_COMPLETE )
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
                    CV_RunStop();
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
            else if(RecoveryOption == eOHTPIORecoveryOption.PIO_Restart) //재시작
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
            ClearOHTPIOBits();// 210617 RGJ 스탭 시작시 OHT 비트 초기화.
            //동작전 내부에 트레이가 있어야 한다.
            if (!this.IsTrayExist())
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "Unload 동작이나 내부에  Tray 가 없어서 종료");
                //스텝 시작 에서 트레이 Off 시 에러없이 종료한다.
                CurrentStep = eOHTPIOStep.Step_Done;
            }
            //동작전 전후방 스톱퍼 닫히 상태로 대기 한다.
            if (GetCVStopperState(true) != eCV_StopperState.Up)
            {
                if (!CVStopperClose(true))
                {
                    CurrentStep = eOHTPIOStep.UnloadErrorRecovery_Start;
                }
            }
            if (GetCVStopperState(false) != eCV_StopperState.Up)
            {
                if (!CVStopperClose(false))
                {
                    CurrentStep = eOHTPIOStep.UnloadErrorRecovery_Start;
                }
            }
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
            //동작전 전방 스톱퍼 열린 상태여야 한다.
            if (!CVStopperOpen(true))
            {
                CurrentStep = eOHTPIOStep.UnloadErrorRecovery_Start;
                return;
            }
            if (!CVStopperOpen(false))
            {
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
                    CV_RunStop();
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
                    //동작전 전방 스톱퍼 열린 상태여야 한다.
                    if (!CVStopperOpen(true))
                    {
                        CurrentStep = eOHTPIOStep.UnloadErrorRecovery_Start;
                        return;
                    }
                    if (!CVStopperOpen(false))
                    {
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
                    CV_RunStop();
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
                    CV_RunStop();
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
                    if(LogWrote == false)
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
                    CV_RunStop();
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
                if (!IN_OHTIF_VALID && !IN_OHTIF_COMPLETE && !IN_OHTIF_CS_0 && !IN_OHTIF_CS_1&& !IN_OHTIF_CS_2 && !IN_OHTIF_CS_3)
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "[VALID Off] [CS All Off] [COMPLETE Off] 확인 ");
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

            if(RecoveryRequeset) //사용자 리셋 요청이 들어오면
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

        /// <summary>
        /// OHT 모듈은 셔틀로 보고를 올린다.
        /// </summary>
        public override void ReportModule_LCS()
        {
            if(IsInPort)
            {
                GlobalData.Current.WCF_mgr.ReportPortStatus(ModuleName);
            }
            else
            {
                GlobalData.Current.WCF_mgr.ReportShuttleStatus(ModuleName, false);
            }
            
        }
        public void ReportShuttleReset_LCS() //리셋시 Contain 보고를 따로 올려준다.
        {
            if (IsInPort)
            {
                GlobalData.Current.WCF_mgr.ReportPortStatus(ModuleName, true);
            }
            else
            {
                GlobalData.Current.WCF_mgr.ReportShuttleStatus(ModuleName, true);
            }
           
        }

        /// <summary>
        /// OHT 셔틀 보고 파라미터
        /// </summary>
        /// <returns></returns>
        public override Parameter_SHUTTLE GetShttleStautsPara(bool isReset)
        {
            Parameter_SHUTTLE PShuttule = new Parameter_SHUTTLE(this.ModuleName);
            PShuttule.UnitID = this.ModuleName;
            PShuttule.Auto = AutoManual == eCVAutoManual.Auto ? "1" : "0";
            PShuttule.Type = this.DefalutDirection == eCV_Direction.ToBooth ? "1" : "2";

            if (IsInPort)
            {
                if (NextCVCommand == eCVCommand.TrayLoad)
                {
                    PShuttule.Request = IsTrayExist() ? "0" : "1";
                    PShuttule.PortReady = IsTrayExist() ? "0" : "1";
                }
            }
            else
            {
                if (NextCVCommand == eCVCommand.TrayUnload)
                {
                    PShuttule.Request = IsTrayExist() ? "1" : "0";
                    PShuttule.PortReady = IsTrayExist() ? "1" : "0";
                }
            }

            PShuttule.CarrierContain = IsTrayExist() ? "1" : "0";
            PShuttule.TagID = CurrentTray != null ? CurrentTray.GetHexTagID() : "";
            PShuttule.CarrierSize = "0"; //0으로 고정값
            PShuttule.CarrierID = CurrentTray != null ? CurrentTray.GetHexCarrierID() : "";

            if(isReset)
            {
                PShuttule.TagID = "";
                PShuttule.CarrierID = "";
                PShuttule.Request = "0";
                PShuttule.PortReady = "0";
            }

            if (this.NextCVCommand == eCVCommand.ErrorHandling || CheckModuleAlarmExist())
            {
                PShuttule.ErrorStatus = "1";
                PShuttule.ErrorCode = GetModuleLastAlarmCode();      //5DIGIT CODE
                if (PShuttule.ErrorCode == "0") //알람 상태인데 알람 코드가 없으면 임시로 올려준다.
                {
                    PShuttule.ErrorCode = IsInPort ? "508" : "507";
                }
            }
            else
            {
                PShuttule.ErrorStatus = "0";
                PShuttule.ErrorCode = "0"; //211126 RGJ 포트 보고 00000 으로 올라가는 문제로 수정.
            }

            return PShuttule;
        }

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
            GlobalData.Current.WCF_mgr.ReportPortStatus(ModuleName); // LCS 대응을 위해 에러리셋 보고는  포트로 한번보고
            Thread.Sleep(50);
            GlobalData.Current.WCF_mgr.ReportShuttleStatus(ModuleName, false); //셔틀로 추가 보고
        }

    }

 
}
