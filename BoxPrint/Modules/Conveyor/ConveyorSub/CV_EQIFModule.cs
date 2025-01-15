using System;
using System.Threading;
using Stockerfirmware.Log;
using Stockerfirmware.CCLink;
using Stockerfirmware.DataList.CV;


namespace Stockerfirmware.Modules.Conveyor
{
    public class CV_EQIFModule : CV_BaseModule
    {
        public CV_EQIFModule(string mName ,bool simul) : base(mName ,simul)
        {
            CVModuleType = eCVType.EQIF;
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
            CurrentActionDesc = "초기화 동작중입니다.";
            LocalActionStep = 0;
            //인버터 에러 체크
            if (!CVRunner.CV_Reset())
            {
                //인버터 에러 리셋 실패하면 Error
                GlobalData.Current.Alarm_Manager.AlarmOccurbyName("INVERTER_RUN_ERROR", ModuleName);
                return new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "인버터 에러 리셋에 실패하였습니다.");
            }
            //트레이 데이터 상태 체크
            if (IsTrayExist())
            {
                if (CurrentTray == null)
                {
                    //트레이 데이터가 없다면 빈 트레이 생성
                    InsertTray(new Tray("ERROR", true));
                }
                //다음 모듈 Entry 센서 체크가 필요 한지 검토
                if (IN_STOP)
                {
                    //스톱위치면 바로 SendTray으로
                    if(IsInPort)
                    {
                        NextCVCommand = eCVCommand.SendTray;
                    }
                    else
                    {
                        NextCVCommand = eCVCommand.TrayUnload;
                    }
                }
                else
                {
                    //동작전 스톱퍼 UP
                    if (!CVStopperClose())
                    {
                        return new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "트레이 초기화중 스톱퍼 동작실패하였습니다.");
                    }
                    //정지위치가 아니면 정지위치로 보낸다.
                    CVForwardRun(eCV_Speed.Low);
                    DateTime dt = DateTime.Now;
                    while (true)
                    {
                        if (IsTimeOut(dt, InverterTimeout))
                        {
                            //타임아웃이 걸렸다는 의미는 실물 트레이가 없거나 인버터 문제 발생.
                            GlobalData.Current.Alarm_Manager.AlarmOccurbyName("CONVEYOR_OVER_TIME_ERROR", ModuleName);
                            return new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "트레이 초기화 이동 동작중 타임아웃 발생하였습니다.");
                        }
                        if (IN_STOP)
                        {
                            CVRunner.CV_Stop();
                            if (IsInPort)
                            {
                                NextCVCommand = eCVCommand.SendTray;
                            }
                            else
                            {
                                NextCVCommand = eCVCommand.TrayUnload;
                            }
                            break;
                        }
                    }
                }
            }
            else if (!IN_ENTRY && !IN_STOP)
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
            }
            else //데이타가 없는데 초기화시 트레이 감지 되면 에러상태
            {
                NextCVCommand = eCVCommand.ErrorHandling;
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
            bool bStepEnd = false;
            LocalActionStep = 0;
            OUT_IF_COMPLETE = false;
            OUT_IF_READY = false;
            CV_ActionResult errorResult = new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "동작중 에러 발생하였습니다.");

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
                        NextCVCommand = eCVCommand.SendTray;
                        return new CV_ActionResult(ModuleName, eCV_ActionResult.Complete, "EQ -> Port Tray Load  완료");
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
            while (!bStepEnd)
            {
                if (AutoManual != eCVAutoManual.Auto && LocalActionStep <= 1)
                {
                    CV_RunStop();
                    NextCVCommand = eCVCommand.TrayLoad;
                    return new CV_ActionResult(ModuleName, eCV_ActionResult.Complete, "Auto 상태가 해제되었습니다..");
                }
                if (DoAbnormalCheck())
                {
                    //문제 발생
                    CV_RunStop();
                    OUT_IF_READY = false;
                    OUT_IF_COMPLETE = false;
                    LocalActionStep = ErrorStep;
                }
                if (ActionAbortRequested)
                {
                    CV_RunStop();
                    OUT_IF_READY = false;
                    OUT_IF_COMPLETE = false;
                    ActionAbortRequested = false;
                    return new CV_ActionResult(ModuleName, eCV_ActionResult.Aborted, "로드 동작이 취소되었습니다.");
                }
                switch (LocalActionStep)
                {
                    case 0: //스톱퍼 업 동작
                        CurrentActionDesc = "스톱퍼 업 동작중";
                        if (UseStopper)
                        {
                            if (!CVStopperClose())
                            {
                                //스톱퍼 업 동작실패 
                                LogManager.WriteConsoleLog(eLogLevel.Info, "Stopper Up 동작 실패하였습니다.");
                                errorResult = new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "스톱퍼 동작중 에러가 발생하였습니다.");
                                LocalActionStep = ErrorStep;
                            }
                        }
                        LocalActionStep++;
                        break;
                    case 1: //컨베이어 PIO 시작
                        CurrentActionDesc = "장비(EQ) 배출 요청 대기중";
                        if (IN_IF_READY && !IN_IF_COMPLETE) //배출 요청 확인
                        {
                            //받는 컨베이어에서 먼저 컨베이어 가동
                            bool result = CVForwardRun(eCV_Speed.High);
                            if (result)
                            {
                                OUT_IF_READY = true; //진입 허가
                                LocalActionStep++;
                            }
                            else
                            {

                                LogManager.WriteConsoleLog(eLogLevel.Info, "컨베이어 Forward 동작 실패하였습니다.");
                                errorResult = new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "컨베이어 Forward 동작 실패하였습니다.");
                                LocalActionStep = ErrorStep;
                            }
                        }
                        break;
                    case 2:  //배출 완료 시그널 대기
                        CurrentActionDesc = "장비(EQ) 배출 완료 대기중";
                        if (IsTimeOut(StepTimeOutDT, TrayTransTimeout)) // 스텝 타임아웃 체크
                        {
                            LogManager.WriteConsoleLog(eLogLevel.Info, "Module:{0} Step:{1} TrayLoadAction Time Out 발생", ModuleName, LocalActionStep);
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
                        if (!IN_IF_READY) //동작중 EQ READY OFF
                        {
                            LogManager.WriteConsoleLog(eLogLevel.Info, "컨베이어 PIO 중 EQ Ready 비트가 OFF되었습니다.");
                            CV_RunStop(); //컨베이어 스탑
                            GlobalData.Current.Alarm_Manager.AlarmOccur("500", ModuleName); //EQ_INTERFACE_ERROR.  
                            errorResult = new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "컨베이어 PIO 중 EQ Ready Bit Off되었습니다.");
                            LocalActionStep = ErrorStep;
                        }
                        //배출 완료 시그널 체크
                        if (IN_IF_READY && IN_IF_COMPLETE)
                        {
                            //Entry 입력도착 확인 STOP 조건 추가.
                            if (IN_ENTRY || IN_STOP)
                            {
                                //감지 되었으면 정보 없는  트레이 생성.
                                InsertTray(new Tray("ERROR", true));
                                ReportModule_LCS(); //도착시 트레이 보고
                                LocalActionStep++;
                            }
                        }
                        break;

                    case 3: //Stop 위치까지 트레이 전송
                        CurrentActionDesc = "STOP 위치로 전송중";
                        if (IsTimeOut(StepTimeOutDT, TrayTransTimeout)) // 스텝 타임아웃 체크
                        {
                            LogManager.WriteConsoleLog(eLogLevel.Info, "Module:{0} Step:{1} TrayLoadAction Time Out 발생", ModuleName, LocalActionStep);
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
                        if (IN_STOP) //스톱위치 확인
                        {

                            //스톱 위치 도달하면 스톱퍼까지 도달하기 위해서 잠시 대기
                            Thread.Sleep(ReceiveStopDelay);
                            OUT_IF_COMPLETE = true; // 동작 완료 신호.
                            CV_RunStop();
                            Thread.Sleep(100); //정지후 잠시 안정화 대기
                            LocalActionStep++;
                        }
                        break;
                    case 4: //배출 컨베이어 신호 OFF 확인
                        CurrentActionDesc = "장비(EQ) 신호 OFF 대기중";
                        if (IsTimeOut(StepTimeOutDT, TrayTransTimeout)) // 스텝 타임아웃 체크
                        {
                            LogManager.WriteConsoleLog(eLogLevel.Info, "Module:{0} Step:{1} TrayLoadAction Time Out 발생", ModuleName, LocalActionStep);
                            CV_RunStop();
                            if (UseAutoRecovery)
                            {
                                LogManager.WriteConsoleLog(eLogLevel.Info, "자동 복구 모드 이므로 처음 스텝으로 되돌아 갑니다.");
                                LocalActionStep = 0;
                            }
                            else
                            {
                                GlobalData.Current.Alarm_Manager.AlarmOccur("500", ModuleName); //EQ_INTERFACE_ERROR.                            
                                GlobalData.Current.MainBooth.SetTowerLampSub(true); //SUB 경광등 ON 20210412.
                                LocalActionStep = ErrorStep;
                            }
                            break;
                        }
                        if (!IN_IF_READY && !IN_IF_COMPLETE)
                        {
                            OUT_IF_READY = false; //신호 OFF
                            OUT_IF_COMPLETE = false; //신호 OFF
                            LocalActionStep++;
                        }
                        break;
                    case 5:
                        CurrentActionDesc = "트레이 로드완료";
                        bStepEnd = true; //로드 프로세스 완료
                        NextCVCommand = eCVCommand.SendTray;
                        LogManager.WriteConsoleLog(eLogLevel.Info, "CV :{0} 트레이 정지위치 도착 완료)", ModuleName);
                        break;
                    case ErrorStep: //에러발생
                        CurrentActionDesc = "트레이 로딩중 에러 발생 에러 스탭 :" + _LocalRecoveryStep;
                        LogManager.WriteConsoleLog(eLogLevel.Info, "CVTrayLoadAction 동작중 ERROR 발생하였습니다.");
                        return errorResult;

                    default:
                        return new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "CV_ActionProcess 동작중 유효한 Step 이 아닙니다.");
                }
                Thread.Sleep(LocalStepCycleDelay);
            }
            return new CV_ActionResult(ModuleName, eCV_ActionResult.Complete, "EQ -> Port Tray Load  완료");
        }

        /// <summary>
        /// 외부 장비로 Tray Unload 인터페이스
        /// </summary>
        /// <param name="RequireRecovery"></param>
        /// <returns></returns>
        protected override CV_ActionResult TrayUnloadAction()
        {
            LogManager.WriteConsoleLog(eLogLevel.Info, "{0} Module Tray Unload Start", this.ModuleName);
            CurrentActionDesc = "트레이 언로드 동작으로 진입합니다.";
            bool bStepEnd = false;
            LocalActionStep = 0;
            OUT_IF_COMPLETE = false;
            OUT_IF_READY = false;
            CV_ActionResult errorResult = new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "동작중 에러 발생하였습니다.");

            #region Simulation 전용
            if (SimulMode)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "CV 모듈: {0} 시뮬 모드로  트레이는 3초후 자동으로 삭제됩니다.", this.ModuleName);
                Thread.Sleep(3000);
                RemoveTray();
                ReportModule_LCS(); // 트레이 삭제 보고
                NextCVCommand = eCVCommand.ReceiveTray;
                LogManager.WriteConsoleLog(eLogLevel.Info, "CV 모듈: {0} 트레이가 삭제 되었습니다.", this.ModuleName);
                return new CV_ActionResult(ModuleName, eCV_ActionResult.Complete, "Port -> EQ  Tray UnLoad  완료");
            }
            #endregion
            ClearInternalSignals();
            //장비의 트레이 언로딩을 대기
            if (!IsTrayExist()) //언로딩스탭인데 트레이가 없어선 안된다.
            {
              
                GlobalData.Current.Alarm_Manager.AlarmOccur(IsInPort ? "508" : "507", ModuleName); //재하감지 에러 발생.
                return new CV_ActionResult(ModuleName, eCV_ActionResult.Aborted, "UnLoad 명령이나 트레이가 없습니다.");
            }
            ReportModule_LCS();
            while (!bStepEnd)
            {
                if (AutoManual != eCVAutoManual.Auto && LocalActionStep <= 1)
                {
                    CV_RunStop();
                    NextCVCommand = eCVCommand.TrayUnload;
                    return new CV_ActionResult(ModuleName, eCV_ActionResult.Complete, "Auto 상태가 해제되었습니다..");
                }
                if (DoAbnormalCheck())
                {
                    //문제 발생
                    CV_RunStop();
                    OUT_IF_READY = false;
                    OUT_IF_COMPLETE = false;
                    LocalActionStep = ErrorStep;
                }
                if (ActionAbortRequested)
                {
                    CV_RunStop();
                    OUT_IF_READY = false;
                    OUT_IF_COMPLETE = false;
                    ActionAbortRequested = false;
                    return new CV_ActionResult(ModuleName, eCV_ActionResult.Aborted, "언로드 동작이 취소되었습니다.");
                }
                switch (LocalActionStep)
                {

                    case 0: //다음 CV모듈로  UnloadRequest On
                        CurrentActionDesc = "장비(EQ)로 배출 요청중 ";    
                        OUT_IF_READY = true; //Requeset On
                        LocalActionStep++;
                        break;
                    case 1: //진입 허가 대기
                        CurrentActionDesc = "장비(EQ) 쪽 진입 허가 대기중 ";
                        if (IN_IF_READY && !IN_IF_COMPLETE)
                        {
                            if (UseStopper)
                            {
                                if (!CVStopperOpen())
                                {
                                    //스톱퍼 오픈 동작실패 
                                    errorResult = new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "스톱퍼 동작중 에러가 발생하였습니다.");
                                    LocalActionStep = ErrorStep;
                                }
                            }
                            Thread.Sleep(300); //스톱퍼 안정화 대기

                            //허가 받고 컨베이어 가동
                            bool result = CVForwardRun(eCV_Speed.High);
                            if (result)
                            {
                                LocalActionStep++;
                            }
                            else
                            {
                                LogManager.WriteConsoleLog(eLogLevel.Info, "컨베이어 Forward 동작 실패하였습니다.");
                                errorResult = new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "컨베이어 Forward 동작 실패하였습니다.");
                                LocalActionStep = ErrorStep;
                            }
                        }
                        break;
                    case 2: //Entry 신호 Off Stop 신호 On 대기
                        CurrentActionDesc = "트레이 STOP 위치까지 전송";
                        if (IsTimeOut(StepTimeOutDT, TrayTransTimeout)) // 스텝 타임아웃 체크
                        {
                            LogManager.WriteConsoleLog(eLogLevel.Info, "Module:{0} Step:{1} TrayUnLoadAction Time Out 발생", ModuleName, LocalActionStep);
                            CV_RunStop();
                            if (UseAutoRecovery)
                            {
                                LogManager.WriteConsoleLog(eLogLevel.Info, "자동 복구 모드 이므로 처음 스텝으로 되돌아 갑니다.");
                                LocalActionStep = 0;
                            }
                            else
                            {
                                GlobalData.Current.Alarm_Manager.AlarmOccur("501", ModuleName); //CONVEYOR_OVER_TIME_ERROR
                                LocalActionStep = ErrorStep;
                            }
                            break;
                        }
                        if (!IN_ENTRY && IN_STOP)
                        {
                            LocalActionStep++;
                        }
                        break;

                    case 3: //Stop 신호 Off 면 배출되었다고 본다.
                        CurrentActionDesc = "트레이 배출 진행중";
                        if (IsTimeOut(StepTimeOutDT, TrayTransTimeout)) // 스텝 타임아웃 체크
                        {
                            LogManager.WriteConsoleLog(eLogLevel.Info, "Module:{0} Step:{1} TrayUnLoadAction Time Out 발생", ModuleName, LocalActionStep);
                            CV_RunStop();
                            if (UseAutoRecovery)
                            {
                                LogManager.WriteConsoleLog(eLogLevel.Info, "자동 복구 모드 이므로 처음 스텝으로 되돌아 갑니다.");
                                LocalActionStep = 0;
                            }
                            else
                            {
                                GlobalData.Current.Alarm_Manager.AlarmOccur("501", ModuleName); //CONVEYOR_OVER_TIME_ERROR
                                LocalActionStep = ErrorStep;
                            }
                            break;
                        }
                        if (!IN_STOP)
                        {
                            Thread.Sleep(TraySendRemainDelay);
                            OUT_IF_COMPLETE = true;
                            CV_RunStop();
                            RemoveTray(); //트레이 정보 삭제.
                            ReportModule_LCS();
                            LocalActionStep++;
                        }
                        break;
                    case 4: //진입 완료 신호 대기
                        CurrentActionDesc = "장비(EQ) 쪽 진입 완료 신호 대기중";
                        if (IsTimeOut(StepTimeOutDT, TrayTransTimeout)) // 스텝 타임아웃 체크
                        {
                            LogManager.WriteConsoleLog(eLogLevel.Info, "Module:{0} Step:{1} TrayUnLoadAction Time Out 발생", ModuleName, LocalActionStep);
                            CV_RunStop();
                            if (UseAutoRecovery)
                            {
                                LogManager.WriteConsoleLog(eLogLevel.Info, "자동 복구 모드 이므로 처음 스텝으로 되돌아 갑니다.");
                                LocalActionStep = 0;
                            }
                            else
                            {
                                GlobalData.Current.Alarm_Manager.AlarmOccur("500", ModuleName); //EQ_INTERFACE_ERROR.
                                LocalActionStep = ErrorStep;
                            }
                            break;
                        }
                        if (IN_IF_READY && IN_IF_COMPLETE)
                        {
                            //트레이가 진입완료 했으므로 PIO 신호 OFF
                            OUT_IF_READY = false;
                            OUT_IF_COMPLETE = false;
                            LocalActionStep++;
                        }
                        break;
                    case 5: //수신 컨베이어 신호 Off 대기
                        CurrentActionDesc = "장비(EQ) 쪽 신호 OFF 대기중";
                        if (IsTimeOut(StepTimeOutDT, TrayTransTimeout)) // 스텝 타임아웃 체크
                        {
                            LogManager.WriteConsoleLog(eLogLevel.Info, "Module:{0} Step:{1} TrayUnLoadAction Time Out 발생", ModuleName, LocalActionStep);
                            CV_RunStop();
                            if (UseAutoRecovery)
                            {
                                LogManager.WriteConsoleLog(eLogLevel.Info, "자동 복구 모드 이므로 처음 스텝으로 되돌아 갑니다.");
                                LocalActionStep = 0;
                            }
                            else
                            {
                                GlobalData.Current.Alarm_Manager.AlarmOccur("500", ModuleName); //EQ_INTERFACE_ERROR.
                                LocalActionStep = ErrorStep;
                            }
                            break;
                        }
                        if (!IN_IF_READY && !IN_IF_COMPLETE) //신호 OFF 확인
                        {
                            //정상 OFF 했으므로  PIO 종료
                            LocalActionStep++;
                        }
                        break;
                    case 6:
                        CurrentActionDesc = "트레이 언로딩 완료";
                        bStepEnd = true; //반송 프로세스 완료
                        NextCVCommand = eCVCommand.ReceiveTray;
                        LogManager.WriteConsoleLog(eLogLevel.Info, "CV :{0} 트레이 언로딩 완료)", ModuleName);
                        break;
                    case ErrorStep: //에러발생
                        CurrentActionDesc = "트레이 언로딩중 에러 발생 에러 스탭 :" + _LocalRecoveryStep;
                        LogManager.WriteConsoleLog(eLogLevel.Info, "CVTrayLoadAction 동작중 ERROR 발생하였습니다.");
                        return errorResult;

                    default:
                        return new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "CV_ActionProcess 동작중 유효한 Step 이 아닙니다.");
                }
                Thread.Sleep(LocalStepCycleDelay);
            }
            return new CV_ActionResult(ModuleName, eCV_ActionResult.Complete, "Tray -> EQ UnLoad  완료");
        }

        protected override CV_ActionResult ReceiveTrayAction()
        {
            LogManager.WriteConsoleLog(eLogLevel.Info, "{0} Module ReceiveTrayAction Start", this.ModuleName);
            CurrentActionDesc = "트레이 수신 동작으로 진입합니다.";
            RemoveTray(); //혹시 데이타가 남아있으면 삭제
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
                            if (CVStopperClose())
                            {
                                LocalActionStep++;
                            }
                            else
                            {
                                //스톱퍼 업 동작실패 
                                errorResult = new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "Stopper Up 동작 실패하였습니다.");
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
                  
                    case 3: //Stop 위치까지 트레이 전송
                        CurrentActionDesc = "STOP 위치로 전송중";
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
                        if (IN_STOP) //스톱위치 확인
                        {
                            Thread.Sleep(ReceiveStopDelay); //정지후 잠시 안정화 대기
                            this.Internal_ToPrevCV_InComplete = true; //도착 완료
                            CV_RunStop();
                            Thread.Sleep(100); //정지후 잠시 안정화 대기
                            LocalActionStep++;
                        }
                        break;
                    case 4: //배출 컨베이어 신호 OFF 확인
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
                                GlobalData.Current.Alarm_Manager.AlarmOccur("500", ModuleName); //EQ_INTERFACE_ERROR.
                                GlobalData.Current.MainBooth.SetTowerLampSub(true); //SUB 경광등 ON 20210412.
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
                    case 5:
                        CurrentActionDesc = "트레이 데이터 체크중";
                        if (IsTimeOut(StepTimeOutDT, TrayTransTimeout)) // 스텝 타임아웃 체크
                        {
                            errorResult = new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "Tray 데이터가 전송되지 않았습니다.");
                            LocalActionStep = ErrorStep;
                        }
                        if (CurrentTray != null) //이전 모듈이 전달한 Tray 데이터 확인
                        {
                            LogManager.WriteConsoleLog(eLogLevel.Info, "CV :{0} 트레이 ID:{0})", ModuleName);
                            LocalActionStep++;
                        }
                        break;
                    case 6: //Tray 수신 프로세스 완료
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
            ClearInternalSignals();
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
                            if (UseStopper)
                            {
                                if (!CVStopperOpen()) //신호 받고 스톱퍼 오픈
                                {
                                    //스톱퍼 오픈 동작실패 
                                    errorResult = new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "Stopper Down 동작 실패하였습니다.");
                                    LocalActionStep = ErrorStep;
                                }
                            }
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
                                GlobalData.Current.Alarm_Manager.AlarmOccur("501", ModuleName); //CONVEYOR_OVER_TIME_ERROR
                                LocalActionStep = ErrorStep;
                            }
                            break;
                        }
                        if (!IN_STOP)
                        {
                            Thread.Sleep(TraySendRemainDelay);
                            Internal_ToNextCV_OutComplete = true;
                            //CV_RunStop(); //CV_RunStop(); //211224 RGJ 컨베이어 Send 동작 수정.
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
                                GlobalData.Current.Alarm_Manager.AlarmOccur("501", ModuleName); //CONVEYOR_OVER_TIME_ERROR
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
                                GlobalData.Current.Alarm_Manager.AlarmOccur("501", ModuleName); //CONVEYOR_OVER_TIME_ERROR
                                LocalActionStep = ErrorStep;
                            }
                            break;
                        }
                        if (!NextCV.Internal_ToPrevCV_InComplete && !NextCV.Internal_ToPrevCV_LoadRequest) //신호 OFF 확인
                        {
                            //정상 OFF 했으므로  PIO 종료
                            CV_RunStop(); //211224 RGJ 컨베이어 Send 동작 수정.
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

        private bool IN_IF_READY
        {
            get
            {
                if (SimulMode)
                {
                    return Simul_In_IOArray[14];
                }
                bool bValue = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "CV_IN_IF_READY");
                return bValue;
            }
            set
            {
                if (SimulMode)
                {
                    Simul_In_IOArray[14] = value;
                    return;
                }
                else
                {
                    //throw new Exception("입력값에다 쓸수 없습니다.");
                    //INPUT에 쓸수없지만 로그에만 남겨둔다.
                    Log.LogManager.WriteConsoleLog(eLogLevel.Error, "잘못된 I/O 엑세스.입력 IN_IF_READY 를 쓰기 시도했습니다.");
                }
            }
        }

        private bool IN_IF_COMPLETE
        {
            get
            {
                if (SimulMode)
                {
                    return Simul_In_IOArray[15];
                }
                bool bValue = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "CV_IN_IF_COMPLTETE");
                return bValue;
            }
            set
            {
                if (SimulMode)
                {
                    Simul_In_IOArray[15] = value;
                    return;
                }
                else
                {
                    //throw new Exception("입력값에다 쓸수 없습니다.");
                    //INPUT에 쓸수없지만 로그에만 남겨둔다.
                    Log.LogManager.WriteConsoleLog(eLogLevel.Error, "잘못된 I/O 엑세스.입력 IN_IF_COMPLETE 를 쓰기 시도했습니다.");
                }
            }
        }

        private bool OUT_IF_READY
        {
            get
            {
                if (SimulMode)
                {
                    return Simul_Out_IOArray[14];
                }
                bool bValue = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "CV_OUT_IF_READY");
                return bValue;
            }
            set
            {
                if (SimulMode)
                {
                    Simul_Out_IOArray[14] = value;
                    return;
                }
                CCLinkManager.CCLCurrent.WriteIO(ModuleName, "CV_OUT_IF_READY", value);
            }
        }
        private bool OUT_IF_COMPLETE
        {
            get
            {
                if (SimulMode)
                {
                    return Simul_Out_IOArray[15];
                }
                bool bValue = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "CV_OUT_IF_COMPLTETE");
                return bValue;
            }
            set
            {
                if (SimulMode)
                {
                    Simul_Out_IOArray[15] = value;
                    return;
                }
                CCLinkManager.CCLCurrent.WriteIO(ModuleName, "CV_OUT_IF_COMPLTETE", value);
            }
        }
        protected override CV_ActionResult ErrorHandlingAction()
        {
            CurrentActionDesc = "Error Reset 명령을 기다립니다.";
            CV_RunStop();
            OUT_IF_COMPLETE = false; //에러 발생시 설비 인터페이스 신호 OFF
            OUT_IF_READY = false;
            RecoveryRequeset = false;
            ClearInternalSignals();
            //에러 보고
            ReportModule_LCS();
            while (true)
            {
                if (RecoveryRequeset)
                {
                    RecoveryRequeset = false;
                    NextCVCommand = eCVCommand.Initialize; //모듈에 맞게 초기화작업 다시 시작
                    return new CV_ActionResult(ModuleName, eCV_ActionResult.Complete, "에러 핸들링 요청확인");
                }
                Thread.Sleep(LocalStepCycleDelay);
            }
        }
    }
}
