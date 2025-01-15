using System;
using System.Threading;
using Stockerfirmware.CCLink;
using Stockerfirmware.Log;
using Stockerfirmware.DataList.CV;

namespace Stockerfirmware.Modules.Conveyor
{
    //트레이 스태킹 모듈
    public class CV_StackerModule : CV_BaseModule
    {
        private bool _ForceUnloadRequest = false;
        private bool ForceUnloadRequest
        {
            get
            {
                if(_ForceUnloadRequest == true) //true 값 가져갈때는 변수값을 초기화 해서 한번만 실행토록 보장.
                {
                    _ForceUnloadRequest = false;
                    return true;
                }
                return false;
            }
            set
            {
                _ForceUnloadRequest = value;
            }
        }

        #region UI 용 기능들 추가.
        public override string CustomActionName3
        {
            get
            {
                if(GetCVHolderState() == eCV_StackerHoldState.Hold)
                {
                    return "Holder Release";
                }
                else
                {
                    return "Holder Lock";
                }
            }

            protected set
            {
                base.CustomActionName3 = value;
            }
        }
        public CV_StackerModule(string mName, bool simul) : base(mName, simul)
        {
            CVModuleType = eCVType.Stacker;
            CustomActionTag1 = "ServoUP";
            CustomActionTag2 = "ServoDown";
            CustomActionTag3 = "HoldLock";

            CustomActionName1 = "ServoUP";
            CustomActionName2 = "ServoDown";
            CustomActionName3 = "ServoLock";
        }
        public override void DoCustomAction(string ActionTag)
        {
            switch(ActionTag)
            {
                case "ServoUP":
                    IOServo.DoServoPointTableAction(2);
                    break;
                case "ServoDown":
                    IOServo.DoServoPointTableAction(1);
                    break;
                case "HoldLock":
                    if (GetCVHolderState() == eCV_StackerHoldState.Hold)
                    {
                        CVStackHolderControl(false);
                    }
                    else
                    {
                        CVStackHolderControl(true);
                    }
                    break;
                default:
                    break;
            }
        }
        #endregion
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
                        case eCVCommand.ModuleAction:
                            Result = StackingAction();
                            break;
                        case eCVCommand.ModuleAction_ForceMode:
                            Result = StackingAction(true);
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
            ForceUnloadRequest = false;
            //세퍼레이터 혹시 모르니 Unchuck 추가
            ChuckSeperator(false);
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
                    NextCVCommand = eCVCommand.SendTray;
                }
                else
                {
                    //동작전  전부 스톱퍼 UP
                    if (!CVStopperClose(true) && !CVStopperClose(false))
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
        
        /// <summary>
        /// 하단에 대기중인 트레이를 스택킹한다.
        /// </summary>
        /// <param name="FroceUnload">강제 배출 사용할지 결정</param>
        /// <returns></returns>
        protected CV_ActionResult StackingAction(bool ForceUnload = false)
        {
            //1.현재 상부에 대기 중인 트레이 높이 체크[인터락]
            //2.스톱퍼 상태 체크.
            //3.들어온 트레이 높이 체크
            //4.1 들어온 높이가 일정 높이 이상이면 바로 배출
            //5.UP 동작
            //6.높이 리미트 센서 체크
            //7.1 리미트 도달 -> 홀더 릴리즈 하여 스택된 트레이를 내린다.
            //7.2 리미트 미도달 -> 홀더 락하여 스택된 트레이 고정.
            //8.DOWN 동작
            //9.결과 리턴.
            LogManager.WriteConsoleLog(eLogLevel.Info, "{0} Module DoStackingAction Start", this.ModuleName);
            CurrentActionDesc = "트레이 스택 동작으로 진입합니다.";
            bool bStepEnd = false;
            ClearInternalSignals();
            LocalActionStep = 0;
            CV_ActionResult errorResult = new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "동작중 에러 발생하였습니다.");
            if(ForceUnload)
            {
                if (CheckStackUpperCheck())  //아무리 강제 배출이라도 리미트 도달했는데 또올리면 안된다.
                {
                    if (IsTrayExist())
                    {
                        GlobalData.Current.Alarm_Manager.AlarmOccur("511", ModuleName); //높이 감지 이상
                        return new CV_ActionResult(ModuleName, eCV_ActionResult.Aborted, "Stack 강제 배출 모드이나 리미트 조건 이상");
                    }
                }
            }
            else
            {
                if (!IsTrayExist()) //스택킹 스탭인데 트레이가 없어선 안된다.
                {
                    GlobalData.Current.Alarm_Manager.AlarmOccur(IsInPort ? "508" : "507", ModuleName); //재하감지 에러 발생.
                    return new CV_ActionResult(ModuleName, eCV_ActionResult.Aborted, "Stack 명령이나 트레이가 없습니다.");
                }
                if (CheckStackUpperCheck()) //스택킹 스탭인데 이미 리미트에 도달했다.
                {
                    GlobalData.Current.Alarm_Manager.AlarmOccur("511", ModuleName); //높이 감지 이상
                    return new CV_ActionResult(ModuleName, eCV_ActionResult.Aborted, "Stack 명령이나 이미 스택 트레이 리미트에 도달 하였습니다.");
                }
            }
            
            while (!bStepEnd)
            {
                if (AutoManual != eCVAutoManual.Auto && LocalActionStep <= 1)
                {
                    CV_RunStop();
                    NextCVCommand = eCVCommand.ModuleAction;
                    return new CV_ActionResult(ModuleName, eCV_ActionResult.Complete, "Auto 상태가 해제되었습니다..");
                }
                if (DoAbnormalCheck())
                {
                    //문제 발생
                    CV_RunStop();
                    IOServo.DoServoForceStop();
                    LocalActionStep = ErrorStep;
                }
                switch (LocalActionStep)
                {
                    case 0: //모든 스톱퍼 센터링 동작
                        CurrentActionDesc = "모든 스톱퍼 센터링 동작";  
                        if (CVStopperClose(true) && CVStopperClose(false)) //스톱퍼를 열었다가 다시 연다.
                        {
                            Thread.Sleep(100)
;                           if(CVStopperOpen(true) && CVStopperOpen(false)) //스톱퍼를 열었다가 다시 연다.
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
                            //스톱퍼  동작실패 
                            errorResult = new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "Stopper  동작 실패하였습니다.");
                            LocalActionStep = ErrorStep;
                        }
                        break;
                    case 1: //들어온 트레이 높이 다시체크
                        CurrentActionDesc = "트레이 높이 체크";
                        if(CheckTrayNeedStack() || ForceUnload) //스택 진행 높이 체크 [강제 배출 모드이면  스택작업]
                        {
                            LocalActionStep++;
                        }
                        else //높이 센서 감지되면 바로 배출
                        {
                            bStepEnd = true; //스택 프로세스 완료
                            LogManager.WriteConsoleLog(eLogLevel.Info, "CV :{0} 트레이가 스택할 높이가 아니므로  배출 스탭으로 보냅니다.)", ModuleName);
                            NextCVCommand = eCVCommand.SendTray;
                        }    
                        break;
                    case 2: //홀더 락을 잡는다.
                        CurrentActionDesc = "홀더 락업 중";
                        if (CVStackHolderControl(true)) // 서보 업 동작전 미리 락을 잡아놓는다.
                        {
                            LocalActionStep++;
                        }
                        else 
                        {
                            errorResult = new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "Holder 동작 실패하였습니다.");
                            LocalActionStep = ErrorStep;
                        }
                        break;
                    case 3: //서보 UP 동작 진행
                        CurrentActionDesc = "서보 UP 동작 진행";
                        //동작전 다시 스톱퍼 상태 체크
                        if(GetCVStopperState(true) == eCV_StopperState.Down && GetCVStopperState(false) == eCV_StopperState.Down)
                        {
                            bool bUpResult = IOServo.DoServoPointTableAction(2); //상부 위치로 보낸다.
                            if (bUpResult && IOServo.CheckStackPosition())
                            {
                                LocalActionStep++;
                            }
                            else
                            {
                                //서보  동작실패 
                                errorResult = new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "서보  Up 동작 실패하였습니다.");
                                LocalActionStep = ErrorStep;
                            }
                        }
                        else
                        {
                            //스톱퍼 동작상태 이상 
                            errorResult = new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "Stopper  동작 실패하였습니다.");
                            LocalActionStep = ErrorStep;
                        }
                       
                        break;
                    case 4: //센서 체크 및 홀딩 동작
                        CurrentActionDesc = "리미트 센서 체크중";
                        if (CheckStackUpperCheck() || ForceUnload) //상부 리미트에 감지 되었으면 배출 [강제 배출 모드이면 높이 상관없이 배출]
                        {
                            LogManager.WriteConsoleLog(eLogLevel.Info, "CV :{0} 상부 리미트에 감지 되었으므로 락을 해제하고 배출준비합니다.강제배출 모드: {1})", ModuleName,ForceUnload);
                            if (CVStackHolderControl(false)) //홀더 해제
                            {
                                LocalActionStep++;
                            }
                            else
                            {
                                //홀더 동작실패 
                                errorResult = new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "Holder  동작 실패하였습니다.");
                                LocalActionStep = ErrorStep;
                            }
                        }
                        else //상부 리미트에 감지 안되어 있으면 홀딩
                        {
                            LogManager.WriteConsoleLog(eLogLevel.Info, "CV :{0} 상부 리미트에 감지 안되었으므로 락하고 스택킹합니다.",ModuleName);
                            if (CVStackHolderControl(true))
                            {
                                LocalActionStep++;
                            }
                            else
                            {
                                //홀더 동작실패 
                                errorResult = new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "Holder  동작 실패하였습니다.");
                                LocalActionStep = ErrorStep;
                            }
                        }
                        break;
                    case 5: //서보 다운 동작시작
                        CurrentActionDesc = "서보 Down 동작 진행";
                        //동작전 다시 스톱퍼 상태 체크
                        if (GetCVStopperState(true) == eCV_StopperState.Down && GetCVStopperState(false) == eCV_StopperState.Down)
                        {
                            bool bDownResult = IOServo.DoServoPointTableAction(1); //하부 위치로 보낸다.
                            if (bDownResult && IOServo.CheckHomePosition())
                            {
                                if (IsTrayExist())//트레이가 존재하면 배출
                                {
                                    LogManager.WriteConsoleLog(eLogLevel.Info, "CV :{0} 스택 동작후 트레이감지 되었으므로 배출 스텝으로 갑니다", ModuleName);
                                    LocalActionStep += 2;
                                }
                                else
                                {
                                    LocalActionStep++;
                                }
                            }
                            else
                            {
                                //서보  동작실패 
                                errorResult = new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "서보 Down 동작 실패하였습니다.");
                                LocalActionStep = ErrorStep;
                            }
                        }
                        else
                        {
                            //스톱퍼 동작상태 이상 
                            errorResult = new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "Stopper  동작 실패하였습니다.");
                            LocalActionStep = ErrorStep;
                        }
                        break;
                    case 6:
                        CurrentActionDesc = "트레이 스택 완료";
                        bStepEnd = true; //스택 프로세스 완료
                        LogManager.WriteConsoleLog(eLogLevel.Info, "CV :{0} 트레이 스택후 추가 트레이를 대기합니다.)", ModuleName);
                        NextCVCommand = eCVCommand.ReceiveTray;
                        break;
                    case 7:
                        CurrentActionDesc = "트레이 스택 완료 후 배출 준비";
                        bStepEnd = true; //스택 프로세스 완료
                        LogManager.WriteConsoleLog(eLogLevel.Info, "CV :{0} 트레이 스택을 완료하였으므로  배출 스탭으로 보냅니다.)", ModuleName);
                        NextCVCommand = eCVCommand.SendTray;
                        break;
                    case ErrorStep: //에러발생
                        CurrentActionDesc = "트레이 스택중 에러 발생 에러 스탭 :" + _LocalRecoveryStep;
                        return errorResult;
                    default:
                        return new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "CV_ActionProcess 동작중 유효한 Step 이 아닙니다.");
                }
                Thread.Sleep(LocalStepCycleDelay);
            }
            return new CV_ActionResult(ModuleName, eCV_ActionResult.Complete, "Tray 스택동작 완료");
        }
        protected override CV_ActionResult ReceiveTrayAction()
        {
            LogManager.WriteConsoleLog(eLogLevel.Info, "{0} Module ReceiveTrayAction Start", this.ModuleName);
            CurrentActionDesc = "트레이 수신 동작으로 진입합니다.";
            ForceUnloadRequest = false;
            RemoveTray(); //혹시 데이타가 남아있으면 삭제
            bool bStepEnd = false;
            LocalActionStep = 0;
            CV_ActionResult errorResult = new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "동작중 에러 발생하였습니다.");
            if (IsTrayExist()) //받기 스탭인데 트레이가 있어선 안된다.
            {
                GlobalData.Current.Alarm_Manager.AlarmOccur(IsInPort ? "508" : "507", ModuleName); //재하감지 에러 발생.
                return new CV_ActionResult(ModuleName, eCV_ActionResult.Aborted, "Receive 명령이나 트레이가 존재합니다.");
            }
            //동작전 서보 홈 상태 체크
            if (!IOServo.CheckHomePosition())
            {
                GlobalData.Current.Alarm_Manager.AlarmOccur("605", ModuleName); //서보 위치 에러 발생.
                return new CV_ActionResult(ModuleName, eCV_ActionResult.Aborted, "Receive 명령이나 서보 위치가 이상합니다.");
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
                        //-스택커 강제 배출 모드 구현
                        if (CheckForceUnloadButton()) //강제 배출 요청이 들어왔는지 체크 받기
                        {
                            LogManager.WriteConsoleLog(eLogLevel.Info, "Module:{0} Step:{1} ReceiveTrayAction 강제 언로드 버튼 입력 발생", ModuleName, LocalActionStep);
                            NextCVCommand = eCVCommand.ModuleAction_ForceMode;
                            return new CV_ActionResult(ModuleName, eCV_ActionResult.Complete, "트레이 수신전 강제 배출 동작으로 변경");
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
                    case 7: //Tray 수신 프로세스 완료
                        CurrentActionDesc = "트레이 수신완료";
                        LogManager.WriteConsoleLog(eLogLevel.Info, "CV :{0} 트레이 수신 프로세스 완료", ModuleName);
                        //여기서 높이체크 해서 스택킹 할건지 그냥 보낼건지 결정
                        if (CheckTrayNeedStack())
                        {
                            LogManager.WriteConsoleLog(eLogLevel.Info, "CV :{0} 트레이 높이체크 결과 스택시작", ModuleName);
                            NextCVCommand = eCVCommand.ModuleAction;
                        }
                        else
                        {
                            LogManager.WriteConsoleLog(eLogLevel.Info, "CV :{0} 트레이 높이체크 결과 스택패스", ModuleName);
                            NextCVCommand = eCVCommand.SendTray;
                        }
                        bStepEnd = true;
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
            //동작전 서보 홈 상태 체크
            if (!IOServo.CheckHomePosition())
            {
                GlobalData.Current.Alarm_Manager.AlarmOccur("605", ModuleName); //서보 위치 에러 발생.
                return new CV_ActionResult(ModuleName, eCV_ActionResult.Aborted, "Send 명령이나 서보 위치가 이상합니다.");
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


        /// <summary>
        /// 진입한 트레이가 스택대상인지 판정.
        /// </summary>
        /// <returns></returns>
        protected bool CheckTrayNeedStack()
        {
            if (SimulMode)
            {
                return false;
            }
            bool bCheck = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "CV_STACK_HEIGHT_CHECK");
            return !bCheck; //센서 감지 되었으면 스택안하고 보낸다.
        }



        /// <summary>
        /// 현재 쌓여 있는 스택이 리미트까지 도달했는지 체크
        /// </summary>
        /// <returns></returns>
        protected bool CheckStackUpperCheck()
        {
            if(SimulMode)
            {
                return false;
            }
            bool bLimit = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "CV_STACK_UPPER_CHECK");
            return bLimit;
        }

        /// <summary>
        /// 스톱퍼를 업(클로즈) 동작 제어. 양방향 동시 제어필요
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

        public override eCV_StopperState GetCVStopperState(bool ForwardStopper = true)
        {
            if (UseStopper)
            {
                if (SimulMode)
                {
                    if (ForwardStopper)
                    {
                        return LastFWD_StopperState;
                    }
                    else
                    {
                        return LastBWD_StopperState;
                    }
                }
                bool bUp1 = CCLinkManager.CCLCurrent.ReadIO(ModuleName, string.Format("CV_STOPPER_{0}_UP_CHECK1", ForwardStopper ? "FWD" : "BWD"));
                bool bUp2 = CCLinkManager.CCLCurrent.ReadIO(ModuleName, string.Format("CV_STOPPER_{0}_UP_CHECK2", ForwardStopper ? "FWD" : "BWD"));
                if (bUp1 && bUp2)
                {
                    return eCV_StopperState.Up;
                }
                else if (!bUp1 && !bUp2)
                {
                    return eCV_StopperState.Down;
                }
                else
                {
                    return eCV_StopperState.Unknown;
                }
                
            }
            else
            {
                return eCV_StopperState.Down;
            }
        }

        public eCV_ChuckState ChuckSeperator(bool Chuck)
        {
            if(Chuck)
            {
                CCLinkManager.CCLCurrent.WriteIO(ModuleName,"CV_IN_1ST_BWD", false);
                //CCLinkManager.CCLCurrent.WriteIO(ModuleName, "CV_OUT_1ST_BWD",false);
                Thread.Sleep(IODelay);
                CCLinkManager.CCLCurrent.WriteIO(ModuleName, "CV_IN_1ST_FWD", true);
                //CCLinkManager.CCLCurrent.WriteIO(ModuleName, "CV_OUT_1ST_FWD", true);

                return eCV_ChuckState.Chuck; //비사용으로 체크 안함
            }
            else
            {
                CCLinkManager.CCLCurrent.WriteIO(ModuleName, "CV_IN_1ST_FWD", false);
                //CCLinkManager.CCLCurrent.WriteIO(ModuleName, "CV_OUT_1ST_FWD", false);
                Thread.Sleep(IODelay);
                CCLinkManager.CCLCurrent.WriteIO(ModuleName, "CV_IN_1ST_BWD", true);
                //CCLinkManager.CCLCurrent.WriteIO(ModuleName, "CV_OUT_1ST_BWD", true);

                return eCV_ChuckState.Unchuck; //비사용으로 체크 안함
            }
        }
        public override bool DoHomeAction()
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
        public override void CV_RunEMGStop()
        {
            //서보 EMO 추가.
            IOServo.DoServoForceStop();
            CVRunner.CV_EMGStop();
            ConveyorRunning = GetCurrentRunSpeed();

        }
        /// <summary>
        /// // 2021.10.20 RGJ
        ///-스택커 강제 배출 모드 구현
        /// </summary>
        /// <returns></returns>
        private bool CheckForceUnloadButton()
        {
            if (SimulMode)
            {
                return false;
            }
            //return IN_START_SW;//실제 버튼이 추가되면 주석 해제.
            return ForceUnloadRequest;
        }

        //물리적 버튼이 없으므로 GUI 버튼 이벤트에서 호출 토록 한다.
        public void PushForceUnloadButton()
        {
            ForceUnloadRequest = true;
        }
    }
}
