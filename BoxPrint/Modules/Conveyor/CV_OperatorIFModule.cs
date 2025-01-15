using System;
using System.Threading;
using Stockerfirmware.CCLink;
using Stockerfirmware.Log;
using Stockerfirmware.DataList.CV;


namespace Stockerfirmware.Modules.Conveyor
{
    /// <summary>
    /// 작업자 인터페이스 하는 컨베이어 모듈
    /// </summary>
    public class CV_OperatorIFModule : CV_BaseModule
    {
        public CV_OperatorIFModule(string mName, bool simul) : base(mName, simul)
        {
            CVModuleType = eCVType.OperatorIF;
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
                    if (IsInPort)
                    {
                        //스톱위치면 바로 SendTray으로
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
                            NextCVCommand = eCVCommand.SendTray;
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

        protected override CV_ActionResult TrayLoadAction()
        {
            CurrentActionDesc = "트레이 로드 동작으로 진입합니다.";
            bool LastStopValue = false;
            bool LastEntryValue = false;

            //오퍼레이터의 트레이 로딩을 대기
            bool bStepEnd = false;
            LocalActionStep = 0;
            DateTime dt = DateTime.Now;
            CV_ActionResult errorResult = new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "동작중 에러 발생하였습니다.");
            ClearInternalSignals();
           
            ReportModule_LCS();
            
            while (!bStepEnd)
            {
                if (AutoManual != eCVAutoManual.Auto)
                {
                    CV_RunStop();
                    NextCVCommand = eCVCommand.TrayLoad;
                    return new CV_ActionResult(ModuleName, eCV_ActionResult.Complete, "Auto 상태가 해제되었습니다..");
                }
                if (DoAbnormalCheck())
                {
                    //문제 발생
                    CV_RunStop();
                    LocalActionStep = ErrorStep;
                }
                if(IsEntryStopChanged) // I/O 변경시마다 보고 올림
                {
                    ReportModule_LCS();
                    IsEntryStopChanged = false;
                }
                switch (LocalActionStep)
                {
                    case 0:
                        CurrentActionDesc = "트레이 재하 감지 대기중.";
                        if (IN_ENTRY) //Entry 센서 감지 체크
                        {
                            DateTime DT = DateTime.Now;
                            LogManager.WriteConsoleLog(eLogLevel.Info, "CV :{0} Entry 센서 Tray 감지)", ModuleName);
                            //감지 되었으면 정보 없는  트레이 생성.
                            InsertTray(new Tray("ERROR", true));

                            if (UseBypassMode) //바이패스 모드일경우 바로 Stop 위치로 전송
                            {
                                while (!IsTimeOut(DT, 3))
                                {
                                    if (!IN_ENTRY)
                                    {
                                        LocalActionStep = 0;
                                        break;
                                    }
                                }
                                if(!IN_ENTRY)
                                {
                                    LocalActionStep = 0;
                                }
                                else
                                {
                                    LocalActionStep = 2;
                                }
                               
                            }
                            else //스위치 입력 대기
                            {
                                LocalActionStep = 1;
                            }
                        }
                        break;
                    case 1:
                        CurrentActionDesc = "오퍼레이터 시작 스위치 대기중";
                        if (IN_START_SW)
                        {
                            LogManager.WriteConsoleLog(eLogLevel.Info, "CV :{0} Start 스위치 On 체크)", ModuleName);
                            LocalActionStep = 2;
                        }
                        break;
                    case 2: //RFID PIO Step 
                        CurrentActionDesc = "트레이 로딩완료";
                        //구현 생략
                        //=========
                        bStepEnd = true;
                        LogManager.WriteConsoleLog(eLogLevel.Info, "CV :{0} 트레이 RFID Interface 완료)", ModuleName);
                        NextCVCommand = eCVCommand.SendTray;
                        break;
                    case ErrorStep: //에러발생
                        LogManager.WriteConsoleLog(eLogLevel.Info, "CVTrayLoadAction 동작중 ERROR 발생하였습니다.");
                        return errorResult;

                    default:
                        return new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "CV_ActionProcess 동작중 유효한 Step 이 아닙니다.");
                }
                Thread.Sleep(LocalStepCycleDelay);
            }
            //시퀀스 확인 필요
            return new CV_ActionResult(ModuleName, eCV_ActionResult.Complete, "Tray  Manual Load  완료");
        }
        protected override CV_ActionResult TrayUnloadAction()
        {
            CurrentActionDesc = "트레이 언로드 동작으로 진입합니다.";
            //오퍼레이터의 트레이 언로딩을 대기
            bool bStepEnd = false;
            LocalActionStep = 0;
            CV_ActionResult errorResult = new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "동작중 에러 발생하였습니다.");
            ClearInternalSignals();
            if (!IsTrayExist()) //언로딩스탭인데 트레이가 없으면 가져간 걸로 보고 다시 Receive 로 보낸다.
            {
                //리퀘스트 OFF
                //this.REQUEST = "0";
                RemoveTray();
                ReportModule_LCS(); // 트레이 삭제 보고
                NextCVCommand = eCVCommand.ReceiveTray;
                LogManager.WriteConsoleLog(eLogLevel.Info, "CV 모듈: {0} 트레이가 삭제 되었습니다.", this.ModuleName);
                return new CV_ActionResult(ModuleName, eCV_ActionResult.Complete, "Tray  Manual UnLoad  재하감지 없어서 완료처리");
            }
            else
            {
                ////리퀘스트 ON
                //this.REQUEST = "1";
                ReportModule_LCS();
            }
            if (SimulMode)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "CV 모듈: {0} 시뮬 모드로  트레이는 3초후 자동으로 삭제됩니다.", this.ModuleName);
                Thread.Sleep(3000);
                RemoveTray();
                ReportModule_LCS(); // 트레이 삭제 보고
                NextCVCommand = eCVCommand.ReceiveTray;
                LogManager.WriteConsoleLog(eLogLevel.Info, "CV 모듈: {0} 트레이가 삭제 되었습니다.", this.ModuleName);
                return new CV_ActionResult(ModuleName, eCV_ActionResult.Complete, "Tray  Manual UnLoad  완료");
            }
            while (!bStepEnd)
            {
                if (AutoManual != eCVAutoManual.Auto)
                {
                    CV_RunStop();
                    NextCVCommand = eCVCommand.TrayUnload;
                    return new CV_ActionResult(ModuleName, eCV_ActionResult.Complete, "Auto 상태가 해제되었습니다..");
                }
                switch (LocalActionStep)
                {
                    case 0:
                        if (UseRFID)
                        {
                            CurrentActionDesc = "트레이 RFID Read 진행중.";
                            string RFIDReadValue = CVRFID_Read();//RFID 읽기
                            UpdateTrayTagID(RFIDReadValue);
                            ReportModule_LCS();
                            LocalActionStep++;
                        }
                        else
                        {
                            LocalActionStep++;
                        }
                        break;
                    case 1:
                        CurrentActionDesc = "트레이 메뉴얼 언로딩을 대기합니다.";
                        if (!TrayExistBySensor()) //센서 확인해서 없으면 완료처리
                        {
                            bStepEnd = true; //언로드 프로세스 완료
                            ReportModule_LCS();
                            NextCVCommand = eCVCommand.ReceiveTray;
                            LogManager.WriteConsoleLog(eLogLevel.Info, "CV :{0} 트레이 배출완료)", ModuleName);
                        }
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
            return new CV_ActionResult(ModuleName, eCV_ActionResult.Complete, "Tray  Manual UnLoad  완료");
        }


        protected override CV_ActionResult ReceiveTrayAction()
        {
            LogManager.WriteConsoleLog(eLogLevel.Info, "{0} Module ReceiveTrayAction Start", this.ModuleName);
            CurrentActionDesc = "트레이 수신 동작으로 진입합니다.";
            RemoveTray(); //혹시 데이타가 남아있으면 삭제
            bool bStepEnd = false;
            LocalActionStep = 0;
            ActionAbortRequested = false;
            CV_ActionResult errorResult = new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "동작중 에러 발생하였습니다.");
            ClearInternalSignals();
            if (IsTrayExist()) //받기 스탭인데 트레이가 있어선 안된다.
            {
                GlobalData.Current.Alarm_Manager.AlarmOccur(IsInPort ? "508" : "507", ModuleName); //재하감지 에러 발생.
                return new CV_ActionResult(ModuleName, eCV_ActionResult.Aborted, "Receive 명령이나 트레이가 존재합니다.");
            }
            while (!bStepEnd)
            {
                if(AutoManual != eCVAutoManual.Auto && LocalActionStep <= 1)
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
                            //배출 요청이 있는데 이미 트레이가 있다면 작업자의 임의 배치가 있을수도 있음.
                            if(IsTrayExist())
                            {
                                errorResult = new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "Receive Step 이나 컨베이어에 이미 트레이가 존재합니다.");
                                GlobalData.Current.Alarm_Manager.AlarmOccur("506", ModuleName); //DOUBLE_TRAY_DETECTED 
                                LocalActionStep = ErrorStep;
                                break;
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
                                ReportModule_LCS();
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
                                ReportModule_LCS();
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
                            this.Internal_ToPrevCV_InComplete = true; //도착 완료
                            Thread.Sleep(500); //정지후 잠시 안정화 대기
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
                                ReportModule_LCS();
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
                            if (UseRFID)
                            {
                                string RFIDReadValue = CVRFID_Read();//RFID 읽기
                                UpdateTrayTagID(RFIDReadValue);
                            }
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
            ActionAbortRequested = false;
            LocalActionStep = 0;
            CV_ActionResult errorResult = new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "동작중 에러 발생하였습니다.");
            ClearInternalSignals();
            if (!IsTrayExist()) //보내기 스탭인데 트레이가 없어선 안된다.
            {
                if(UseAutoRecovery)
                {
                    NextCVCommand = eCVCommand.TrayLoad;
                    return new CV_ActionResult(ModuleName, eCV_ActionResult.Complete, "Send 명령이나 트레이가 없기에 Load 로 되돌아갑니다..");
                }
                else
                {
                    GlobalData.Current.Alarm_Manager.AlarmOccur(IsInPort ? "508" : "507", ModuleName); //재하감지 에러 발생.
                    return new CV_ActionResult(ModuleName, eCV_ActionResult.Aborted, "Send 명령이나 트레이가 없습니다.");
                }
                
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
                        if(!TrayExist) //스탭 넘어왔으나 감지 안되면 
                        {
                            if (UseAutoRecovery)
                            {
                                NextCVCommand = eCVCommand.TrayLoad;
                                return new CV_ActionResult(ModuleName, eCV_ActionResult.Complete, "Send 명령이나 트레이가 없기에 Load 로 되돌아갑니다..");
                            }
                            else
                            {
                                GlobalData.Current.Alarm_Manager.AlarmOccur(IsInPort ? "508" : "507", ModuleName); //재하감지 에러 발생.
                                return new CV_ActionResult(ModuleName, eCV_ActionResult.Aborted, "Send 명령이나 트레이가 없습니다.");
                            }
                        }
                        //HA
                        if (UseColorSensor)
                        {
                            if (!CheckColorSensor()) //Ctray 검증
                            {
                                GlobalData.Current.Alarm_Manager.AlarmOccur("572", ModuleName); //OTHER_SIZE_MATERIAL_DETECTED
                                LocalActionStep = ErrorStep;
                                break;
                            }

                        }
                        this.Internal_ToNextCV_UnloadRequest = true;
                        LocalActionStep++;
                        break;
                    case 1: //진입 허가 대기
                        CurrentActionDesc = "다음 컨베이어 쪽 진입 허가 대기중 ";
                        if (!IN_ENTRY && !IN_STOP)//Send 대기중 재하 감지 OFF되면 받기 동작으로 보낸다.
                        {
                            bStepEnd = true; //반송 프로세스 완료
                            NextCVCommand = eCVCommand.TrayLoad;
                            return new CV_ActionResult(ModuleName, eCV_ActionResult.Complete, "Send 대기중에 재하감지 Off되었습니다.");
                        }

                        if (NextCV.Internal_ToPrevCV_LoadRequest && !NextCV.Internal_ToPrevCV_Error)
                        {
                            if (UseStopper)
                            {
                                if (!CVStopperOpen())
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
                                if(IN_STOP) //컨베이어 가동전 이미 스탑 신호 On 상태면 스탑 Off 대기로 간다.
                                {
                                    LocalActionStep += 2; 
                                }
                                else
                                {
                                    LocalActionStep++;
                                }
                                
                            }
                            else
                            {
                                errorResult = new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "컨베이어 Forward 동작 실패하였습니다.");
                                LocalActionStep = ErrorStep;
                            }
                        }
                        break;

                    case 2: //Entry 신호 Off Stop 신호 On 대기
                        CurrentActionDesc = "트레이 STOP 위치까지 전송";
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
                        if (!IN_ENTRY && IN_STOP)
                        {
                            LocalActionStep++;
                        }
                        break;

                    case 3: //Stop 신호 Off 면 배출되었다고 본다.
                        CurrentActionDesc = "트레이 배출 진행중";
                        if (IsTimeOut(StepTimeOutDT, TrayTransTimeout)) // 스텝 타임아웃 체크
                        {
                            LogManager.WriteConsoleLog(eLogLevel.Info, "Module:{0} Step:{1} SendTrayAction Time Out 발생", ModuleName, LocalActionStep);
                            CV_RunStop();
                            if (UseAutoRecovery)
                            {
                                LogManager.WriteConsoleLog(eLogLevel.Info, "자동 복구 모드 이므로 처음 스텝으로 되돌아 갑니다.");
                                LocalActionStep = 0;
                                ReportModule_LCS();
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
                            if (NextCV.IN_ENTRY || SimulMode)   //2021.05.25 lim,
                            {
                                Thread.Sleep(TraySendRemainDelay);
                                Internal_ToNextCV_OutComplete = true;
                                CV_RunStop();
                                NextCV.InsertTray(RemoveTray());// 트레이 정보를 옮겨준다.
                                ReportModule_LCS(); //배출시 보고
                                LocalActionStep++;
                            }
                            else //다음 모듈 엔트리 신호 없으면 오퍼레이터 손으로 꺼낸것으로 간주.
                            {
                                LogManager.WriteConsoleLog(eLogLevel.Info, "STOP 신호 OFF 되었으나 다음 CV ENTRY 신호 OFF 되었기에 로드로 STEP 되돌림.");
                                CV_RunStop();
                                ClearInternalSignals();
                                RemoveTray();
                                bStepEnd = true;
                                NextCVCommand = eCVCommand.TrayLoad;
                                ReportModule_LCS(); // 보고
                                break;
                            }
                        }
                        break;
                    case 4: //진입 완료 신호 대기
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
                                ReportModule_LCS();
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
                    case 5: //수신 컨베이어 신호 Off 대기
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
                                ReportModule_LCS();
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
                    case 6:
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

        protected bool IN_START_SW
        {
            get
            {
                if (SimulMode)
                {
                    return Simul_In_IOArray[8];
                }
                bool bValue = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "CV_START_SW");
                return bValue;
            }
            set
            {
                if (SimulMode)
                {
                    Simul_In_IOArray[8] = value;
                    return;
                }
                else
                {
                    //throw new Exception("입력값에다 쓸수 없습니다.");
                    //INPUT에 쓸수없지만 로그에만 남겨둔다.
                    Log.LogManager.WriteConsoleLog(eLogLevel.Error, "잘못된 I/O 엑세스.입력 IN_STOP 를 쓰기 시도했습니다.");
                }
            }
        }

        public override bool CheckStartSwitch()
        {
            if (IsInPort)
            {
                bool sw = IN_START_SW;

                return sw;
            }
            else
            {
                return false;
            }
        }
        public override void StartSwitchLampControl(bool OnOff)
        {
            if (IsInPort)
            {
                CCLinkManager.CCLCurrent.WriteIO(this.ModuleName, "CV_START_SW_LAMP", OnOff);
            }
        }
    }
}
