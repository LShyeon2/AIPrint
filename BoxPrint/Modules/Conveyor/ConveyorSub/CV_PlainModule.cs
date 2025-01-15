using System;
using System.Threading;
using Stockerfirmware.Log;
using Stockerfirmware.Alarm;
using Stockerfirmware.DataList.CV;
using Stockerfirmware.CCLink;

namespace Stockerfirmware.Modules.Conveyor
{
    /// <summary>
    /// 단순 이동만 수행하는 컨베이어모듈
    /// </summary>
    public class CV_PlainModule : CV_BaseModule
    {
        private bool ByPassAction = false; //현재 정지위치에서 스탑없이 바로 보내는 바이패스 상태인지 체크
        public CV_PlainModule(string mName, bool simul) : base(mName, simul)
        {
            CVModuleType = eCVType.Plain;
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
                    //211224 RGJ Plain CV BackStopper 기능 추가.
                    if (!CVStopperClose() && !CVStopperOpen(false)) //전진 스톱퍼를 닫고 후진 스톱퍼를 연다
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
                            //211224 RGJ Plain CV BackStopper 기능 추가.
                            if (CVStopperClose() && CVStopperOpen(false)) //전진 스톱퍼를 닫고 후진 스톱퍼를 연다
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
                                if ((NextCV.CVModuleType == eCVType.Plain || NextCV.CVModuleType == eCVType.OperatorIF) //다음 2가지만 바이패스
                                    && NextCV.NextCVCommand == eCVCommand.ReceiveTray && !NextCV.TrayExist && NextCV.LocalActionStep == 1) //바이패스 모드 On
                                {
                                    ByPassAction = true; //바이패스 모드 On
                                    Internal_ToNextCV_UnloadRequest = true; //신호를 미리 준다.
                                }
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
                                    Thread.Sleep(ReceiveStopDelay); //스톱퍼 위치까지 
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
                                //211224 RGJ Plain CV BackStopper 기능 추가.
                                //보내기 전에 센터링 목적으로 뒷쪽 스톱퍼를 닫고 다시 연다 간다.
                                if (!CVStopperClose(false))
                                {
                                    //스톱퍼 오픈 동작실패 
                                    errorResult = new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "Stopper UP 동작 실패하였습니다.");
                                    LocalActionStep = ErrorStep;
                                    break;
                                }
                                //뒷쪽 스톱퍼를 다시 연다.
                                if (!CVStopperOpen(false))
                                {
                                    //스톱퍼 오픈 동작실패 
                                    errorResult = new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "Stopper Down 동작 실패하였습니다.");
                                    LocalActionStep = ErrorStep;
                                    break;
                                }
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
                            if(ByPassAction)
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
                        CurrentActionDesc = "트레이 배출중 에러 발생 에러 스탭 :"+_LocalRecoveryStep;
                        return errorResult;
                    default:
                        return new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "CV_ActionProcess 동작중 유효한 Step 이 아닙니다.");
                }
                Thread.Sleep(LocalStepCycleDelay);
            }
            return new CV_ActionResult(ModuleName, eCV_ActionResult.Complete, "Tray 배출 완료");
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
            if (!UseBackStopper && !ForwardStopper) //후면 스톱퍼 안쓰는데 후면 명령 들어오면 바로 리턴.
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
            CCLinkManager.CCLCurrent.WriteIO(ModuleName, string.Format("CV_STOPPER_{0}_DOWN", ForwardStopper ? "FWD" : "BWD"), false);
            Thread.Sleep(IODelay);
            CCLinkManager.CCLCurrent.WriteIO(ModuleName, string.Format("CV_STOPPER_{0}_UP", ForwardStopper ? "FWD" : "BWD"), true);
            DateTime dt = DateTime.Now;
            while (!IsTimeOut(dt, StopperUpdownTimeout))
            {
                if (GetCVStopperState() == eCV_StopperState.Up)
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
        /// <summary>
        /// 스톱퍼를 다운(오픈) 동작 제어.
        /// </summary>
        /// <param name="ForwardStopper"> True : 정방향기준 앞쪽의 스톱퍼를 Down 한다.</param>
        /// <returns></returns>
        public override bool CVStopperOpen(bool ForwardStopper = true)
        {
            if (!UseStopper) //스톱퍼 미사용 모듈은 바로리턴.
            {
                return true;
            }
            if(!UseBackStopper && !ForwardStopper) //후면 스톱퍼 안쓰는데 후면 명령 들어오면 바로 리턴.
            {
                return true;
            }
            if (GetCVStopperState(ForwardStopper) == eCV_StopperState.Down) //상태 봐서 동작 완료되어있으면 동작 필요 없음
            {
                return true;
            }
            if (SimulMode)
            {
                Thread.Sleep(500);
                if (ForwardStopper)
                {
                    LifeTime_Stopper_FWD_CylinderCounter++; //소모품 관리 변수 업데이트
                    LastFWD_StopperState = eCV_StopperState.Down;

                }
                else
                {
                    LifeTime_Stopper_BWD_CylinderCounter++; //소모품 관리 변수 업데이트
                    LastBWD_StopperState = eCV_StopperState.Down;
                }

                return true;
            }
            CCLinkManager.CCLCurrent.WriteIO(ModuleName, string.Format("CV_STOPPER_{0}_UP", ForwardStopper ? "FWD" : "BWD"), false);
            Thread.Sleep(IODelay);
            CCLinkManager.CCLCurrent.WriteIO(ModuleName, string.Format("CV_STOPPER_{0}_DOWN", ForwardStopper ? "FWD" : "BWD"), true);
            Thread.Sleep(IODelay);
            DateTime dt = DateTime.Now;
            while (!IsTimeOut(dt, StopperUpdownTimeout))
            {

                if (GetCVStopperState(ForwardStopper) == eCV_StopperState.Down)
                {
                    Thread.Sleep(IODelay);
                    //헌팅성을 제외하기 위해 한번더 확인.
                    if (GetCVStopperState(ForwardStopper) == eCV_StopperState.Down)
                    {
                        if (ForwardStopper)
                        {
                            LifeTime_Stopper_FWD_CylinderCounter++; //소모품 관리 변수 업데이트
                            LastFWD_StopperState = eCV_StopperState.Down;
                        }
                        else
                        {
                            LifeTime_Stopper_BWD_CylinderCounter++; //소모품 관리 변수 업데이트
                            LastBWD_StopperState = eCV_StopperState.Down;
                        }
                        return true;
                    }
                }
                Thread.Sleep(IODelay);
            }
            GlobalData.Current.Alarm_Manager.AlarmOccurbyName("STOPPER_MOTION_ERROR", ModuleName);
            return false; // 타임아웃이내 실행 실패.
        }
    }
}
