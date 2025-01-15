using System;
using System.Threading;
using Stockerfirmware.Log;
using Stockerfirmware.Alarm;
using Stockerfirmware.DataList.CV;
using Stockerfirmware.CCLink;

namespace Stockerfirmware.Modules.Conveyor
{
    /// <summary>
    /// 셔틀과 직연결 안되어있는 컨베이어모듈
    /// </summary>
    public class CV_SlavePlainModule : CV_BaseModule
    {
        private bool ByPassAction = false; //현재 정지위치에서 스탑없이 바로 보내는 바이패스 상태인지 체크
        private CV_ShuttleTurnModule MasterShuttle;
        public CV_SlavePlainModule(string mName, bool simul) : base(mName, simul)
        {
            CVModuleType = eCVType.Plain;
        }
        public void SetMasterShuttle(CV_ShuttleTurnModule Master)
        {
            if (MasterShuttle == null)
            {
                MasterShuttle = Master;
            }
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
                        case eCVCommand.ReceiveTray:
                            if(IsInPort)
                            {
                                Result = ReceiveTrayAction();
                            }
                            else
                            {
                                Result = ReceiveTrayFromShuttleAction(); //배출 포트면 셔틀로 부터 트레이 수신
                            }
                            break;
                        case eCVCommand.SendTray:
                            if (IsInPort)
                            {
                                Result = SendTrayToShuttleAction(); //투입 포트면 셔틀로 트레이 보낸다.
                            }
                            else
                            {
                                Result = SendTrayAction();
                            }
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
            catch(Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
            }
        }

        protected override CV_ActionResult InitializeAction()
        {
            LogManager.WriteConsoleLog(eLogLevel.Info, "{0} Module InitializeAction Start", this.ModuleName);
            CurrentActionDesc = "초기화 동작중입니다.";
            ByPassAction = false;
            LocalActionStep = 0;
            //인버터 에러 체크
            if (!CVRunner.CV_Reset())
            {
                //인버터 에러 리셋 실패하면 Error
                GlobalData.Current.Alarm_Manager.AlarmOccurbyName("INVERTER_RUN_ERROR", ModuleName);
                return new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "인버터 에러 리셋에 실패하였습니다.");
            }
            //트레이 데이터 상태 체크
            if(IsTrayExist())
            {
                if (CurrentTray == null)
                {
                    //트레이 데이터가 없다면 빈 트레이 생성
                    InsertTray(new Tray("ERROR", true));
                }
                //다음 모듈 Entry 센서 체크가 필요 한지 검토
                if(IN_STOP) 
                {
                    //스톱위치면 바로 SendTray으로
                    NextCVCommand = eCVCommand.SendTray;
                }
                else
                {
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
            else
            {
                RemoveTray();//혹시 트레이 데이터 남아있으면 삭제.
                //트레이가 없으면 ReceiveTray
                if (!IN_ENTRY && !IN_STOP)
                {
                    NextCVCommand = eCVCommand.ReceiveTray;
                }
            }
            return new CV_ActionResult(ModuleName, eCV_ActionResult.Complete, "InitializeAction 완료.");
        }
        protected override CV_ActionResult ReceiveTrayAction()
        {
            LogManager.WriteConsoleLog(eLogLevel.Info, "{0} Module ReceiveTrayAction Start", this.ModuleName);
            CurrentActionDesc = "트레이 수신 동작으로 진입합니다.";
            RemoveTray(); //혹시 데이타가 남아있으면 삭제
            ByPassAction = false;
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
                        CurrentActionDesc = "스톱퍼 동작중";
                        ClearInternalSignals();
                        if (UseStopper)
                        {
                            if (CVStopperClose()) //전진 스톱퍼를 닫고 후진 스톱퍼를 연다
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
                        if (IsTrayExist()) //받기 스탭인데 트레이가 있어선 안된다.
                        {
                            GlobalData.Current.Alarm_Manager.AlarmOccur(IsInPort ? "508" : "507", ModuleName); //재하감지 에러 발생.
                            return new CV_ActionResult(ModuleName, eCV_ActionResult.Aborted, "Receive 명령이나 트레이가 존재합니다.");
                        }
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
                    case 2: //다음 컨베이어 모듈이 수신 대기 상태면 스톱퍼 열고 바이 패싱을 준비
                        CurrentActionDesc = "ByPassing 을 위한 준비동작";
                        if (ByPassAction)
                        {
                            if (UseStopper)
                            {
                                if (CVStopperOpen())
                                {
                                    LocalActionStep++;
                                }
                                else
                                {
                                    //스톱퍼 오픈 동작실패 
                                    errorResult = new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "Stopper Up 동작 실패하였습니다.");
                                    LocalActionStep = ErrorStep;
                                    break;
                                }
                            }
                            else
                            {
                                LocalActionStep++;
                            }
                        }
                        else
                        {
                            LocalActionStep++;
                        }
                        break;
                    case 3:  //배출 완료 시그널 대기
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
                    case 4: //Stop 위치까지 트레이 전송
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
                            if (!ByPassAction)//바이패스모드면 스톱 안시키고 바로 Send로 
                            {
                                if (UseStopper)
                                {
                                    Thread.Sleep(200); //스톱퍼 위치까지 
                                }
                                CV_RunStop();
                                Thread.Sleep(100); //정지후 잠시 안정화 대기
                            }
                            LocalActionStep++;
                        }
                        break;
                    case 5: //배출 컨베이어 신호 OFF 확인
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
                        //lsj SESS Bumpjar
                        if (UseRFID) //RFID 다시 찍는다.
                        {
                            string RFIDReadValue = CVRFID_Read();//RFID 읽기
                            UpdateTrayTagID(RFIDReadValue);
                        }
                        if (!PrevCV.Internal_ToNextCV_OutComplete && !PrevCV.Internal_ToNextCV_UnloadRequest)
                        {
                            Internal_ToPrevCV_InComplete = false; //신호 OFF
                            Internal_ToPrevCV_LoadRequest = false; //신호 OFF
                            LocalActionStep++;
                        }
                        
                        break;


                    case 6:
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
                    case 7: //Tray 수신 프로세스 완료
                        CurrentActionDesc = "트레이 수신완료";
                        bStepEnd = true;
                        NextCVCommand = eCVCommand.SendTray;
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
                        CurrentActionDesc = "다음 컨베이어 쪽 진입 허가 대기중";
                        if (!IN_ENTRY && !IN_STOP)//대기중 재하 감지 OFF되면 받기 동작으로 보낸다.
                        {
                            bStepEnd = true; //반송 프로세스 완료
                            NextCVCommand = eCVCommand.ReceiveTray;
                            ReportModule_LCS();
                            return new CV_ActionResult(ModuleName, eCV_ActionResult.Complete, "대기중에 재하감지 Off되었습니다.");
                        }

                        if (NextCV.Internal_ToPrevCV_LoadRequest && !NextCV.Internal_ToPrevCV_Error)
                        {
                            ByPassAction = false; //bypassMode 상태였다면 Off
                            if (UseStopper)
                            {
                                if (!CVStopperOpen())
                                {
                                    //스톱퍼 오픈 동작실패 
                                    errorResult = new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "Stopper Down 동작 실패하였습니다.");
                                    LocalActionStep = ErrorStep;
                                    break;
                                }
                                //원가절감으로 스톱퍼 다운 정위치를 알수 없기때문에 딜레이로 조정.
                                Thread.Sleep(100);
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
                        else //이미 선 신호를 먼저 주었기 때문에 신호가 안나왔다는것은 문제가 발생함.
                        {
                            if (ByPassAction)
                            {
                                ByPassAction = false;
                                CV_RunStop();
                                errorResult = new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "ByPassMode 진입하였으나 다음 컨베이어 로드신호가 없습니다.");
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
                            Thread.Sleep(TraySendRemainDelay);
                            Internal_ToNextCV_OutComplete = true;
                            //CV_RunStop(); //211224 RGJ 컨베이어 Send 동작 수정.
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
                                NextCVCommand = eCVCommand.ReceiveTray;
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
                                NextCVCommand = eCVCommand.ReceiveTray;
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
                            CV_RunStop(); //211224 RGJ 컨베이어 Send 동작 수정.
                            LocalActionStep++;
                        }
                        break;
                    case 5:
                        CurrentActionDesc = "트레이 배출 완료";
                        bStepEnd = true; //반송 프로세스 완료
                        LogManager.WriteConsoleLog(eLogLevel.Info, "CV :{0} 트레이 배출완료)", ModuleName);
                        NextCVCommand = eCVCommand.ReceiveTray;
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


        protected  CV_ActionResult ReceiveTrayFromShuttleAction()
        {
            LogManager.WriteConsoleLog(eLogLevel.Info, "{0} Module ReceiveTrayFromShuttleAction Start", this.ModuleName);
            if (MasterShuttle == null)
            {
                //마스터 셔틀 정의 안되어 있으면 에러
                LogManager.WriteConsoleLog(eLogLevel.Info, "Module:{0}  Send 명령이나 마스텨 셔틀이 정의 되지 않았습니다.", ModuleName);
                return new CV_ActionResult(ModuleName, eCV_ActionResult.Aborted, "Send 명령이나 마스텨 셔틀이 정의 되지 않았습니다.");
            }
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
                        if (IsTrayExist()) //받기 스탭인데 트레이가 있어선 안된다.
                        {
                            GlobalData.Current.Alarm_Manager.AlarmOccur(IsInPort ? "508" : "507", ModuleName); //재하감지 에러 발생.
                            return new CV_ActionResult(ModuleName, eCV_ActionResult.Aborted, "Receive 명령이나 트레이가 존재합니다.");
                        }
                        if (MasterShuttle.Internal_ToSlaveCV_UnloadRequest && !MasterShuttle.Internal_ToSlaveCV_Error) //배출 요청 확인
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
                        if (MasterShuttle.Internal_ToSlaveCV_Error)
                        {
                            CV_RunStop(); //
                            LocalActionStep = ErrorStep;
                        }
                        //배출 완료 시그널 체크
                        if (MasterShuttle.Internal_ToSlaveCV_OutComplete)
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
                        if (!MasterShuttle.Internal_ToSlaveCV_OutComplete && !MasterShuttle.Internal_ToSlaveCV_UnloadRequest)
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
                        NextCVCommand = eCVCommand.SendTray;
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
        protected  CV_ActionResult SendTrayToShuttleAction()
        {
            LogManager.WriteConsoleLog(eLogLevel.Info, "{0} Module SendTrayToShuttleAction Start", this.ModuleName);
            if (MasterShuttle == null)
            {
                //마스터 셔틀 정의 안되어 있으면 에러
                LogManager.WriteConsoleLog(eLogLevel.Info, "Module:{0}  Send 명령이나 마스텨 셔틀이 정의 되지 않았습니다.", ModuleName);
                return new CV_ActionResult(ModuleName, eCV_ActionResult.Aborted, "Send 명령이나 마스텨 셔틀이 정의 되지 않았습니다.");
            }
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
                        if (MasterShuttle.Internal_ToSlaveCV_LoadRequest && !MasterShuttle.Internal_ToSlaveCV_Error)
                        {
                            //동작전 마스터 셔틀 위치 다시 한번 체크
                            if (MasterShuttle.GetCurrentShuttlePosition() != eShuttlePosition.Slave) //슬레이브 위치가 아니면 에러
                            {

                                LogManager.WriteConsoleLog(eLogLevel.Info, "Module:{0} Step:{1} SendTrayToShuttleAction 위치 인터락 이상 발생", ModuleName, LocalActionStep);
                                errorResult = new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "Send Tray 전 마스터 셔틀 위치가 올바르지 않습니다..");
                                LocalActionStep = ErrorStep;
                                break;
                            }
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
                            MasterShuttle.InsertTray(RemoveTray());// 트레이 정보를 옮겨준다.
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
                                NextCVCommand = eCVCommand.ReceiveTray;
                            }
                            else
                            {
                                GlobalData.Current.Alarm_Manager.AlarmOccur("501", ModuleName); //CONVEYOR_OVER_TIME_ERROR
                                LocalActionStep = ErrorStep;
                            }
                            break;
                        }
                        if (MasterShuttle.Internal_ToSlaveCV_InComplete && !MasterShuttle.Internal_ToSlaveCV_Error)
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
                                NextCVCommand = eCVCommand.ReceiveTray;
                            }
                            else
                            {
                                GlobalData.Current.Alarm_Manager.AlarmOccur("501", ModuleName); //CONVEYOR_OVER_TIME_ERROR
                                LocalActionStep = ErrorStep;
                            }
                            break;
                        }
                        if (!MasterShuttle.Internal_ToSlaveCV_InComplete && !MasterShuttle.Internal_ToSlaveCV_LoadRequest) //신호 OFF 확인
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
                        NextCVCommand = eCVCommand.ReceiveTray;
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
    }
}
