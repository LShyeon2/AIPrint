using System;
using System.Threading;
using Stockerfirmware.CCLink;
using Stockerfirmware.Log;
using Stockerfirmware.DataList.CV;
using Stockerfirmware.SSCNet;

namespace Stockerfirmware.Modules.Conveyor
{
    //트레이 180도 반전 기능 모듈
    //트레이 받고 -> 턴
    //트레이 배출하고 ->리턴
    public class CV_TurnModule : CV_BaseModule
    {
        public CV_TurnModule(string mName, bool simul) : base(mName, simul)
        {
            CVModuleType = eCVType.Turn;
        }

        public override bool DoTurnAction()
        {
            if (SimulMode)
            {
                Thread.Sleep(1000);
                LastTurnState = eCV_TurnState.Turn;
                return true;
            }
            if (GetCVStopperState(true) == eCV_StopperState.Up && GetCVStopperState(false) == eCV_StopperState.Up)
            {
                if (TurnType == eTurnType.AirCylinder)
                {
                    CCLinkManager.CCLCurrent.WriteIO(ModuleName, "CV_RETURN", false);
                    Thread.Sleep(IODelay);
                    CCLinkManager.CCLCurrent.WriteIO(ModuleName, "CV_TURN", true);
                    DateTime dt = DateTime.Now;
                    while (!IsTimeOut(dt, TurnTimeout))
                    {
                        if (GetCVTurnState() == eCV_TurnState.Turn)
                        {
                            Thread.Sleep(IODelay);
                            if (GetCVTurnState() == eCV_TurnState.Turn) //헌팅성 방지 이중체크
                            {
                                LastTurnState = eCV_TurnState.Turn;
                                LifeTime_Turn_CylinderCounter++; //소모품 관리 변수 업데이트
                                return true;
                            }
                        }
                        Thread.Sleep(IODelay);
                    }
                    //Turn TimeOut 알람발생
                    GlobalData.Current.Alarm_Manager.AlarmOccurbyName("TURN_MOTION_ERROR", ModuleName);
                    return false;
                }
                else if (TurnType == eTurnType.Servo)
                {
                    bool Result = ServoManager.GetManagerInstance().RequesetTurn(ServoAxis, eTurnCommand.Turn);

                    if (Result)
                    {
                        LastTurnState = eCV_TurnState.Turn;
                        LogManager.WriteConsoleLog(eLogLevel.Info, "Module : {0} Turn Servo 동작 성공", ModuleName);
                        return Result;
                    }
                    else
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "Module : {0} Turn Servo 동작 실패!", ModuleName);
                        GlobalData.Current.Alarm_Manager.AlarmOccurbyName("TURN_MOTION_ERROR", ModuleName);
                        return Result;
                    }
                }
                else if (TurnType == eTurnType.ServoIO)
                {
                    bool Result = this.IOServo.DoServoPointTableAction(2); // [Return : 1 ] [ Turn : 2] 

                    if (Result && GetCVTurnState() == eCV_TurnState.Turn) //I/O 체크도 같이 이중체크
                    {
                        LastTurnState = eCV_TurnState.Turn;
                        LogManager.WriteConsoleLog(eLogLevel.Info, "Module : {0} Turn Servo 동작 성공", ModuleName);
                        return Result;
                    }
                    else
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "Module : {0} Turn Servo 동작 실패!", ModuleName);
                        GlobalData.Current.Alarm_Manager.AlarmOccurbyName("TURN_MOTION_ERROR", ModuleName);
                        return Result;
                    }
                }
                else
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "Module : {0} Turn 방식이 결정되지 않았습니다!", ModuleName);
                    return false;
                }
            }
            else
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "Module : {0} Turn 인터락 조건 이상!", this.ModuleName);
                return false;
            }
        }

        public override bool DoReturnAction()
        {
            if (SimulMode)
            {
                Thread.Sleep(1000);
                LastTurnState = eCV_TurnState.Return;
                return true;
            }
            if (GetCVStopperState(true) == eCV_StopperState.Up && GetCVStopperState(false) == eCV_StopperState.Up)
            {
                if (TurnType == eTurnType.AirCylinder)
                {
                    CCLinkManager.CCLCurrent.WriteIO(ModuleName, "CV_TURN", false);
                    Thread.Sleep(IODelay);
                    CCLinkManager.CCLCurrent.WriteIO(ModuleName, "CV_RETURN", true);
                    DateTime dt = DateTime.Now;
                    while (!IsTimeOut(dt, TurnTimeout))
                    {
                        if (GetCVTurnState() == eCV_TurnState.Return)
                        {
                            Thread.Sleep(IODelay);
                            if (GetCVTurnState() == eCV_TurnState.Return) //헌팅성 방지 이중체크
                            {
                                LastTurnState = eCV_TurnState.Return;
                                LifeTime_Turn_CylinderCounter++; //소모품 관리 변수 업데이트
                                return true;
                            }
                        }
                        Thread.Sleep(IODelay);
                    }
                    //Turn TimeOut 알람발생
                    GlobalData.Current.Alarm_Manager.AlarmOccurbyName("TURN_MOTION_ERROR", ModuleName);
                    return false;
                }
                else if (TurnType == eTurnType.Servo)
                {
                    bool Result = ServoManager.GetManagerInstance().RequesetTurn(ServoAxis, eTurnCommand.Return);
                    if (Result)
                    {
                        LastTurnState = eCV_TurnState.Return;
                        LogManager.WriteConsoleLog(eLogLevel.Info, "Module : {0} Return Servo 동작 성공", ModuleName);
                        return Result;
                    }
                    else
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "Module : {0} Return Servo 동작 실패!", ModuleName);
                        GlobalData.Current.Alarm_Manager.AlarmOccurbyName("TURN_MOTION_ERROR", ModuleName);
                        return Result;
                    }
                }
                else if (TurnType == eTurnType.ServoIO)
                {
                    bool Result = this.IOServo.DoServoPointTableAction(1); // [Return : 1 ] [ Turn : 2] 

                    if (Result && IOServo.CheckHomePosition()) //I/O 체크도 같이 이중체크)
                    {
                        LastTurnState = eCV_TurnState.Return;
                        LogManager.WriteConsoleLog(eLogLevel.Info, "Module : {0} Return Servo 동작 성공", ModuleName);
                        return Result;
                    }
                    else
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "Module : {0} Return Servo 동작 실패!", ModuleName);
                        GlobalData.Current.Alarm_Manager.AlarmOccurbyName("TURN_MOTION_ERROR", ModuleName);
                        return Result;
                    }
                }
                else
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "Module : {0} Return 방식이 결정되지 않았습니다!", ModuleName);
                    return false;
                }
            }
            else
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "Module : {0} Return 인터락 조건 이상!", this.ModuleName);
                return false;
            }
        }

        public override bool DoHomeAction()
        {
            if (SimulMode)
            {
                return true;
            }
            if (GetCVStopperState(true) == eCV_StopperState.Up && GetCVStopperState(false) == eCV_StopperState.Up)
            {
                if (TurnType == eTurnType.AirCylinder)
                {
                    CCLinkManager.CCLCurrent.WriteIO(ModuleName, "CV_TURN", false);
                    Thread.Sleep(IODelay);
                    CCLinkManager.CCLCurrent.WriteIO(ModuleName, "CV_RETURN", true);
                    DateTime dt = DateTime.Now;
                    while (!IsTimeOut(dt, TurnTimeout))
                    {
                        if (GetCVTurnState() == eCV_TurnState.Return)
                        {
                            Thread.Sleep(IODelay);
                            if (GetCVTurnState() == eCV_TurnState.Return) //헌팅성 방지 이중체크
                            {
                                LastTurnState = eCV_TurnState.Return;
                                LifeTime_Turn_CylinderCounter++; //소모품 관리 변수 업데이트
                                return true;
                            }
                        }
                        Thread.Sleep(IODelay);
                    }
                    //Turn TimeOut 알람발생
                    GlobalData.Current.Alarm_Manager.AlarmOccurbyName("TURN_MOTION_ERROR", ModuleName);
                    return false;
                }
                else if (TurnType == eTurnType.Servo)
                {
                    bool Result = ServoManager.GetManagerInstance()[ServoAxis].AxisHomeMoveAndWait();
                    if (Result)
                    {
                        LastTurnState = eCV_TurnState.Return;
                        LogManager.WriteConsoleLog(eLogLevel.Info, "Module : {0} Servo Home 동작 성공", ModuleName);
                        return Result;
                    }
                    else
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "Module : {0} Servo Home 동작 실패!", ModuleName);
                        GlobalData.Current.Alarm_Manager.AlarmOccurbyName("TURN_MOTION_ERROR", ModuleName);
                        return Result;
                    }
                }
                else if (TurnType == eTurnType.ServoIO)
                {
                    bool Result = IOServo.DoServoPointHomeAction();
                    if (Result)
                    {
                        LastTurnState = eCV_TurnState.Return;
                        LogManager.WriteConsoleLog(eLogLevel.Info, "Module : {0} Servo Home 동작 성공", ModuleName);
                        return Result;
                    }
                    else
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "Module : {0} Servo Home 동작 실패!", ModuleName);
                        GlobalData.Current.Alarm_Manager.AlarmOccurbyName("TURN_MOTION_ERROR", ModuleName);
                        return Result;
                    }
                }
                else
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "Module : {0} Servo Home 방식이 결정되지 않았습니다!", ModuleName);
                    return false;
                }
            }
            else
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "Module : {0} Servo Home 인터락 조건 이상!", this.ModuleName);
                return false;
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

        /// <summary>
        /// 턴 모듈 초기화 동작 트레이가 있으면 보낼수 있도록 회전 동작까지 완료한다.
        /// 트레이 없으면 리턴후 트레이 대기.
        /// </summary>
        /// <returns></returns>
        protected override CV_ActionResult InitializeAction()
        {
            try
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "{0} Module Initialize Action Start", this.ModuleName);
                CurrentActionDesc = "초기화 동작중입니다.";
                LocalActionStep = 0;
                //인버터 에러 체크
                if (!CVRunner.CV_Reset())
                {
                    //인버터 에러 리셋 실패하면 Error
                    GlobalData.Current.Alarm_Manager.AlarmOccurbyName("INVERTER_RUN_ERROR", ModuleName);
                    return new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "인버터 에러 리셋에 실패하였습니다.");
                }
                if (TurnType == eTurnType.Servo)
                {
                    //턴온 안되어 있으면 턴온 시킨다.
                    if (!ServoManager.GetManagerInstance()[this.ServoAxis].CheckServoOn())
                    {
                        ServoManager.GetManagerInstance()[this.ServoAxis].ServoOn();
                    }
                }
                else if (TurnType == eTurnType.ServoIO)
                {
                    IOServo.DoServoResetAction(); //초기화 동작시 서보 리셋
                }
                //트레이 유무 상태 체크
                if (IsTrayExist())
                {
                    if (IN_ENTRY && IN_STOP)
                    {
                        //정위치 OK
                        if (CurrentTray == null)
                        {
                            //트레이 데이터가 없다면 빈 트레이 생성
                            InsertTray(new Tray("ERROR", true));
                        }
                        NextCVCommand = eCVCommand.SendTray;
                    }
                    else //하나만 들어왔으면 초기화 불가
                    {
                        GlobalData.Current.Alarm_Manager.AlarmOccur(IsInPort ? "508" : "507", ModuleName); //재하감지 에러 발생.
                        return new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "초기 재하 센서 값이 이상합니다.");
                    }
                    if (!UseBypassMode) //통과 모드가 아니면 턴동작
                    {
                        if (GetCVTurnState() == eCV_TurnState.Turn)  //이미 턴 상태이면 배출 동작으로
                        {
                            LogManager.WriteConsoleLog(eLogLevel.Info, "Turn Module:{0} Initialize Action :트레이 감지 및 이미 턴 상태이므로 배출 스텝으로 보냄.", this.ModuleName);
                        }
                        else //턴 상태가 아니면 
                        {
                            CurrentActionDesc = "스톱퍼 모두 업 동작중";
                            if (CVStopperClose(true) && CVStopperClose(false)) //모든 스톱퍼를 닫는다.
                            {
                                //턴 동작 실행
                                CurrentActionDesc = "턴 동작중";
                                if (!DoTurnAction())
                                {
                                    return new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "초기화중 턴 동작실패하였습니다.");
                                }
                            }
                            else
                            {
                                //스톱퍼  동작실패 
                                return new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "Stopper 동작 실패하였습니다.");
                            }
                            
                        }
                    }
                    NextCVCommand = eCVCommand.SendTray;
                }
                else
                {
                    RemoveTray();//혹시 트레이 데이터 남아있으면 삭제.
                    if (GetCVTurnState() != eCV_TurnState.Return) //리턴 상태가 아니면 리턴 동작 필요
                    {
                        if (CVStopperClose(true) && CVStopperClose(false)) //모든 스톱퍼를 닫는다.
                        {
                            //트레이가 비었다면 리턴 동작 
                            if (!DoReturnAction())
                            {
                                return new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "초기화중 리턴 동작실패하였습니다.");
                            }
                            NextCVCommand = eCVCommand.ReceiveTray; //트레이 수신 대기
                        }
                        else
                        {
                            //스톱퍼  동작실패 
                            return new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "Stopper 동작 실패하였습니다.");
                        }
                    }
                    NextCVCommand = eCVCommand.ReceiveTray;
                }
                return new CV_ActionResult(ModuleName, eCV_ActionResult.Complete, "InitializeAction 완료.");
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
                return new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "초기화중 예외가 발생하여 실패하였습니다.");
            }

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
                    case 0: //스톱퍼 동작
                        CurrentActionDesc = "스톱퍼 업 동작중";
                        ClearInternalSignals();
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
                            this.Internal_ToPrevCV_InComplete = true; //도착 완료
                            Thread.Sleep(400);// 스톱 위치와 스톱퍼 위치 불일치로 대기시간 추가.
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
                            if (UseBypassMode)
                            {
                                LocalActionStep = 8;
                            }
                            else
                            {
                                LocalActionStep = 6;
                            }
                        }
                        break;
                    case 6: //스톱퍼 닫기
                        CurrentActionDesc = "스톱퍼 모두 업 동작중";
                        if (CVStopperClose(true) && CVStopperClose(false)) //모든 스톱퍼를 닫는다.
                        {
                            LocalActionStep++;
                        }
                        else
                        {
                            //스톱퍼  동작실패 
                            errorResult = new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "Stopper  동작 실패하였습니다.");
                            LocalActionStep = ErrorStep;
                        }
                        break;
                    case 7: //Turn 동작시작
                        CurrentActionDesc = "컨베이어 턴 동작중";
                        if (DoTurnAction())
                        {
                            LocalActionStep++;
                        }
                        else
                        {
                            //턴 동작 실패
                            errorResult = new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "턴 동작 실패하였습니다.");
                            LocalActionStep = ErrorStep;
                        }
                        break;
                    case 8: //Tray 수신 프로세스 완료
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
                    case 0: //스톱퍼 모두 닫기 동작
                        CurrentActionDesc = "다음 컨베이어로 배출 요청중 ";
                        ClearInternalSignals();
                        if (CVStopperClose(true) && CVStopperClose(false)) //양 방향 스톱퍼를 닫는다.
                        {
                            this.Internal_ToNextCV_UnloadRequest = true; //다음 CV모듈로  UnloadRequest On
                            LocalActionStep++;
                        }
                        else
                        {
                            //스톱퍼  동작실패 
                            errorResult = new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "Stopper  동작 실패하였습니다.");
                            LocalActionStep = ErrorStep;
                        }
                        break;
                    case 1: //진입 허가 대기
                        CurrentActionDesc = "다음 컨베이어 쪽 진입 허가 대기중 ";
                        if (NextCV.Internal_ToPrevCV_LoadRequest && !NextCV.Internal_ToPrevCV_Error)
                        {
                            if (UseBypassMode)
                            {
                                if (CVStopperClose(false) && CVStopperOpen(true)) //역방향 스톱퍼는 열고 정방향 스톱퍼는 닫는다..
                                {
                                    //OK
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
                                if (CVStopperClose(true) && CVStopperOpen(false)) //역방향 스톱퍼는 닫고 정방향  스톱퍼는 연다.
                                {
                                    //OK
                                }
                                else
                                {
                                    //스톱퍼  동작실패 
                                    errorResult = new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "Stopper  동작 실패하였습니다.");
                                    LocalActionStep = ErrorStep;
                                }
                            }

                            if (UseBypassMode) //통과모드일때는 전진
                            {
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
                            else //턴 이후에는 후진으로 보내야한다.
                            {
                                //허가 받고 컨베이어 가동
                                bool result = CVBackwardRun(eCV_Speed.High);
                                if (result)
                                {
                                    LocalActionStep++;
                                }
                                else
                                {
                                    errorResult = new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "컨베이어 Backward 동작 실패하였습니다.");
                                    LocalActionStep = ErrorStep;
                                }
                            }
                        }
                        break;
                    case 2: //배출 위치 도착 체크
                        CurrentActionDesc = "트레이 배출 위치 체크";
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
                        if (UseBypassMode)
                        {
                            if (IN_STOP) //통과모드일때는 Stop On
                            {
                                LocalActionStep++;
                            }
                        }
                        else
                        {
                            if (IN_ENTRY) //통과모드일때는 Entry On
                            {
                                LocalActionStep++;
                            }
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
                            }
                            else
                            {
                                GlobalData.Current.Alarm_Manager.AlarmOccur("501", ModuleName); //트레이 타임 오버
                                LocalActionStep = ErrorStep;
                            }
                            break;
                        }
                        if (UseBypassMode)
                        {
                            if (!IN_STOP) //통과모드일때는 스톱신호 Off
                            {
                                NextCV.InsertTray(RemoveTray());// 트레이 정보를 옮겨준다.
                                Thread.Sleep(TraySendRemainDelay);
                                Internal_ToNextCV_OutComplete = true;
                                CV_RunStop();
                                LocalActionStep++;
                            }
                        }
                        else
                        {
                            if (!IN_ENTRY) //턴 이후에는 Entry 신호로 배출 판단
                            {
                                NextCV.InsertTray(RemoveTray());// 트레이 정보를 옮겨준다.
                                Thread.Sleep(TraySendRemainDelay);
                                Internal_ToNextCV_OutComplete = true;
                                CV_RunStop();
                                LocalActionStep++;
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
                                NextCVCommand = eCVCommand.ReceiveTray;
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
                            ReportModule_LCS(); //배출시 보고
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
                                NextCVCommand = eCVCommand.ReceiveTray;
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
                            if (UseBypassMode)
                            {
                                LocalActionStep = 7;
                            }
                            else
                            {
                                LocalActionStep = 6; //배출완료하고 리턴동작으로
                            }
                        }
                        break;
                    case 6: //Return 동작시작
                        CurrentActionDesc = "컨베이어 스톱퍼 전체 업 동작중";
                        if (CVStopperClose(true) && CVStopperClose(false)) //모든 스톱퍼를 닫는다.
                        {
                            CurrentActionDesc = "컨베이어 리턴 동작중";
                            if (DoReturnAction())
                            {
                                LocalActionStep++;
                            }
                            else
                            {
                                //턴 동작 실패
                                errorResult = new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "Stopper  동작 실패하였습니다.");
                                LocalActionStep = ErrorStep;
                            }
                        }
                        else
                        {
                            //스톱퍼  동작실패 
                            return new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "Stopper 동작 실패하였습니다.");
                        }
                        break;
                    case 7:
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

        protected override bool DoRecoveryAction()
        {
            bool ActionFailed = false;
            var rData = GlobalData.Current.LineManager.GetCVRecoveryData(this.ModuleName);
            if (rData != null)
            {
                LocalActionStep = rData._ModuleStep;
                //Stopper,Turn 상태 저장데이터로 변경.
                if (rData._StopperState == eCV_StopperState.Up)
                {
                    if (!CVStopperClose())
                        ActionFailed = true;
                }
                else
                {
                    if (!CVStopperOpen())
                        ActionFailed = true;
                }
                if (rData._TurnState == eCV_TurnState.Turn)
                {
                    if (!DoTurnAction())
                        ActionFailed = true;
                }
                else
                {
                    if (!DoReturnAction())
                        ActionFailed = true;
                }
                if (rData._TrayExist)
                {
                    if (!InsertTray(new Tray(rData._TagID, true, rData._TrayHeight)))
                        ActionFailed = true;
                }
                if (this.LastTurnState == eCV_TurnState.Turn)
                {
                    if (!CVBackwardRun(rData._RunState)) //턴 상태면 역회전으로 돌린다.
                        ActionFailed = true;
                }
                else
                {
                    if (!CVForwardRun(rData._RunState)) //마지막에 인버터 가동
                        ActionFailed = true;
                }
            }
            else
            {
                ActionFailed = true;
            }
            return !ActionFailed;
        }

        public override eCV_TurnState GetCVTurnState()
        {
            if (SimulMode)
            {
                return LastTurnState;
            }
            if (TurnType == eTurnType.AirCylinder)
            {
                bool bTurn = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "CV_TURN_CHECK");
                bool bRturn = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "CV_RETURN_CHECK");

                if (bTurn && !bRturn)
                {
                    return eCV_TurnState.Turn;
                }
                else if (!bTurn && bRturn)
                {
                    return eCV_TurnState.Return;
                }
                else
                {
                    return eCV_TurnState.Unknown;
                }
            }
            else if (TurnType == eTurnType.Servo)
            {
                bool bTurn = ServoManager.GetManagerInstance().CheckTurnAxisInPosition(this.ServoAxis, eTurnCommand.Turn);
                bool bRturn = ServoManager.GetManagerInstance().CheckTurnAxisInPosition(this.ServoAxis, eTurnCommand.Return);
                if (bTurn && !bRturn)
                {
                    return eCV_TurnState.Turn;
                }
                else if (!bTurn && bRturn)
                {
                    return eCV_TurnState.Return;
                }
                else
                {
                    return eCV_TurnState.Unknown;
                }
            }
            else if (TurnType == eTurnType.ServoIO)
            {
                bool bTurn = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "CV_TURN_CHECK");
                bool bRturn = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "CV_RETURN_CHECK");
                if (bTurn && !bRturn)
                {
                    return eCV_TurnState.Turn;
                }
                else if (!bTurn && bRturn)
                {
                    return eCV_TurnState.Return;
                }
                else
                {
                    return eCV_TurnState.Unknown;
                }
            }
            else
            {
                return eCV_TurnState.Unknown;
            }
        }

    }
}
