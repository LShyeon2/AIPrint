using System.Threading;
using Stockerfirmware.Log;
using Stockerfirmware.Modules.RM;
using Stockerfirmware.DataList.CV;
using System;
using Stockerfirmware.CCLink;

namespace Stockerfirmware.Modules.Conveyor
{
    /// <summary>
    /// 로봇과 인터페이스 하는 컨베이어 모듈 [범퍼자 전용]
    /// </summary>
    public class CV_RobotBJModule : CV_BaseModule
    {
        public CV_RobotBJModule(string mName, bool simul) : base(mName, simul)
        {
            CVModuleType = eCVType.RobotIF;
            SetReceiveStopDelay(1200); //기본값
        }

        public override bool CheckTrayLoadingPosition()
        {
            //Tray 재하 체크
            //Tray 얼라인 체크
            bool bEntryPos = IN_ENTRY;//엔트리위치가 정지위치
            bool bAlign = CheckTrayAligned();
            return bEntryPos && bAlign;
        }
        public override bool CheckTrayUnloadingPosition()
        {
            //Tray 재하 체크
            //Tray 얼라인 체크
            bool bStopPos = IN_STOP; //스탑위치가 정지위치
            bool bAlign = CheckTrayDecelSensor();
            return bStopPos && bAlign;
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
        protected override CV_ActionResult TrayLoadAction()
        {
            try
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "{0} Module Tray Load Start", this.ModuleName);
                CurrentActionDesc = "트레이 로드 동작으로 진입합니다.";

                ClearInternalSignals();
                //로봇의 트레이 로딩을 대기
                if (IsTrayExist()) //로딩스탭인데 트레이가 있어선 안된다.
                {
                    if (IN_ENTRY && TrayLoaded)
                    {
                        //사이클 정지후 재시작 하는 경우 처리 케이스
                        ReportModule_LCS();
                        LogManager.WriteConsoleLog(eLogLevel.Info, "Module :{0} Tray Load 동작중 Entry 감지 및 트레이 로드 처리 되었으므로 정상 로드 처리합니다.", this.ModuleName);
                    }
                    else
                    {
                        ReportModule_LCS();
                        GlobalData.Current.Alarm_Manager.AlarmOccur(IsInPort ? "508" : "507", ModuleName); //재하감지 에러 발생.
                        return new CV_ActionResult(ModuleName, eCV_ActionResult.Aborted, "Load 명령이나 이미 트레이가 존재합니다.");
                    }
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
                if (NextCV.CVModuleType == eCVType.StackBuffer) //스택커 인터락 추가.
                {
                    bool HeightOver = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "CV_TRAY_HEIGHT_OVER");
                    if (HeightOver)
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "{0} 모듈 스택커 진입전 높이 이상이 감지되었습니다.", this.ModuleName, CurrentTray.CarrierID);
                        GlobalData.Current.Alarm_Manager.AlarmOccur("511", ModuleName); //높이 오버 알람 발생.
                        return new CV_ActionResult(ModuleName, eCV_ActionResult.Aborted, "스택커 진입전 높이 이상이 감지되었습니다.");
                    }

                }
                NextCVCommand = eCVCommand.SendTray;
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
        protected override CV_ActionResult TrayUnloadAction()
        {
            try
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "{0} Module Tray Unload Start", this.ModuleName);
                CurrentActionDesc = "트레이 언로드 동작으로 진입합니다.";

                ClearInternalSignals();
                //배출전 정위치 및 decel 확인
                if (!CheckTrayUnloadingPosition())
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "{0} 트레이 언로딩 위치가 올바르지 않습니다.", this.ModuleName);
                    return new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "트레이 언로딩 위치가 올바르지 않습니다.");
                }
                //로봇의 트레이 로딩을 대기
                if (IsTrayExist()) //언로딩스텝이면 트레이가 있어야 한다.
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
                CurrentActionDesc = "RM 로딩을 대기합니다.";
                while (true)
                {
                    if (ActionAbortRequested)
                    {
                        NextCVCommand = eCVCommand.ErrorHandling;
                        ReportModule_LCS();
                        ActionAbortRequested = false;
                        return new CV_ActionResult(ModuleName, eCV_ActionResult.Aborted, "대기중에 재하감지 Off되었습니다.");
                    }
                    if (AutoManual != eCVAutoManual.Auto) //대기중에 Auto 상태 해제되면 종료처리하고 빠져나옴
                    {
                        NextCVCommand = eCVCommand.TrayUnload;
                        ReportModule_LCS();
                        return new CV_ActionResult(ModuleName, eCV_ActionResult.Complete, "대기중에 Auto 상태가 해제 되었습니다.");
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
                NextCVCommand = eCVCommand.ReceiveTray;
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
            //라이트 커튼 상태보고 뮤트 해제
            if (CheckLightCurtainMute())
            {
                LightCurtainMuteControl(false);
            }
            //트레이 센서 상태 체크
            if (TrayExistBySensor())
            {
                if (!TrayExistByData())
                {
                    //트레이 데이터가 없다면 빈 트레이 생성
                    InsertTray(new Tray("ERROR", true));
                }
                //다음 모듈 Entry 센서 체크가 필요 한지 검토

                if (IN_STOP) //스톱위치면 다음 명령으로
                {
                    if (IsInPort) //InPort 면 로봇의 언로딩을 대기
                    {
                        if (UseRFID) //RFID 다시 찍는다.
                        {
                            string RFIDReadValue = CVRFID_Read();//RFID 읽기
                            UpdateTrayTagID(RFIDReadValue);
                        }
                        //                       CurrentTray.SetTrayHeight(CheckTrayHeight());
                        NextCVCommand = eCVCommand.TrayUnload;
                    }
                    else
                    {
                        //OutPort 면 배출 대기
                        NextCVCommand = eCVCommand.SendTray;
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
                            if (IsInPort) //InPort 면 로봇의 언로딩을 대기
                            {
                                if (UseRFID) //RFID 다시 찍는다.
                                {
                                    string RFIDReadValue = CVRFID_Read();//RFID 읽기
                                    UpdateTrayTagID(RFIDReadValue);
                                }
                                //                               CurrentTray.SetTrayHeight(CheckTrayHeight());
                                NextCVCommand = eCVCommand.TrayUnload;
                            }
                            else
                            {
                                //OutPort 면 배출 대기
                                NextCVCommand = eCVCommand.SendTray;
                            }
                            break;
                        }
                    }
                }
            }
            else
            {
                //lsj SESS Bumpjar
                //동작전 스톱퍼 UP
                if (!CVStopperClose())
                {
                    return new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "트레이 초기화중 스톱퍼 동작실패하였습니다.");
                }

                RemoveTray();//혹시 트레이 데이터 남아있으면 삭제.
                if (IsInPort) //InPort 면 트레이 수신대기
                {
                    NextCVCommand = eCVCommand.ReceiveTray;
                }
                else
                {
                    //OutPort 면 트레이 로딩대기
                    NextCVCommand = eCVCommand.TrayLoad;
                }
            }
            return new CV_ActionResult(ModuleName, eCV_ActionResult.Complete, "InitializeAction 완료.");
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
                        ClearInternalSignals();
                        if (PrevCV.Internal_ToNextCV_UnloadRequest && !PrevCV.Internal_ToNextCV_Error) //배출 요청 확인
                        {
                            if (UseStopper)//lsj SESS Bumpjar
                            {
                                if (IsInPort)
                                {
                                    if (!CVStopperOpen())
                                    {
                                        //스톱퍼 업 동작실패 
                                        errorResult = new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "Stopper Down 동작 실패하였습니다.");
                                        LocalActionStep = ErrorStep;
                                        break;
                                    }
                                }
                            }
                            //라이트 커튼 해제
                            if (!LightCurtainMuteControl(true))
                            {
                                GlobalData.Current.Alarm_Manager.AlarmOccur("510", ModuleName); //라이튼 커튼 뮤트 실패
                                errorResult = new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "라이트 커튼 Mute 동작에 실패하였습니다.");
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
                            if(IN_ENTRY || IN_STOP)
                            {
                                CurrentTray?.SetTrayHeight(eTrayHeight.Height0);
                                ReportModule_LCS(); //도착시 트레이 보고
                                LocalActionStep++;
                            }
                        }
                        break;

                    case 3:
                        CurrentActionDesc = "얼라인 위치로 전송중";
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
                        //ha
                        if (this.CheckTrayDecelSensor()) //Decel 센서 보고 저속으로 감속
                        {
                            bool result = CVForwardRun(eCV_Speed.Low);
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
                            //스톱 위치 도달하면 스톱퍼까지 도달하기 위해서 잠시 대기
                            //lsj SESS Bumpjar
                            Thread.Sleep(ReceiveStopDelay);

                            CV_RunStop();
                            Thread.Sleep(100); //정지후 잠시 안정화 대기
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
                    case 6:
                        CurrentActionDesc = "라이트 커튼 재가동중";
                        if (!LightCurtainMuteControl(false))//라이트 커튼 작동
                        {
                            errorResult = new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "라이트 커튼 Mute 동작에 실패하였습니다.");
                            LocalActionStep = ErrorStep;
                        }
                        LocalActionStep++;
                        break;
                    case 7: //트레이 검증
                        CurrentActionDesc = "트레이 데이터 체크중";
                        //ha 주석
                        //if (!CheckTrayAligned()) //얼라인 체크
                        //{
                        //    errorResult = new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "Align 센서값 이상발생.");
                        //    LocalActionStep = ErrorStep;
                        //}
                        //eTrayHeight TrayHeight = CheckTrayHeight();  //높이 
                        //if (TrayHeight == eTrayHeight.OverHeight)
                        //{
                        //    GlobalData.Current.Alarm_Manager.AlarmOccur("509", ModuleName); //TRAY HEIGHT ALARM
                        //}
                        //CurrentTray.SetTrayHeight(TrayHeight);

                        if (UseRFID)
                        {
                            string RFIDReadValue = CVRFID_Read(false,3);//RFID 읽기
                            UpdateTrayTagID(RFIDReadValue);
                        }
                        //lsj SESS Bumpjar
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

                        break;

                    case 8: //Tray 수신 프로세스 완료.
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
                        if (CurrentTray == null)
                        {
                            //트레이 정보가 없으면 더미 생성
                            InsertTray(new Tray("ERROR", true));
                        }
                        ClearInternalSignals();
                        this.Internal_ToNextCV_UnloadRequest = true;
                        LocalActionStep++;
                        break;
                    case 1: //진입 허가 대기
                        CurrentActionDesc = "다음 컨베이어 쪽 진입 허가 대기중 ";
                        if (NextCV.Internal_ToPrevCV_LoadRequest && !NextCV.Internal_ToPrevCV_Error)
                        {
                            //라이트 커튼 해제
                            if (!LightCurtainMuteControl(true))
                            {
                                GlobalData.Current.Alarm_Manager.AlarmOccur("510", ModuleName); //라이튼 커튼 뮤트 실패
                                errorResult = new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "라이트 커튼 Mute 동작에 실패하였습니다.");
                                LocalActionStep = ErrorStep;
                            }
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
                                LocalActionStep++;
                            }
                            else
                            {
                                errorResult = new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "컨베이어 Forward 동작 실패하였습니다.");
                                LocalActionStep = ErrorStep;
                            }
                        }
                        break;
                    case 2: //Stop 신호 까지 대기
                        CurrentActionDesc = "트레이 Stop 위치까지 전송 진행중";
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
                        if (IN_STOP)
                        {
                            CurrentActionDesc = "트레이 Stop 위치까지 전송완료";
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
                            NextCV.InsertTray(RemoveTray());// 트레이 정보를 옮겨준다.
                            ReportModule_LCS(); //배출시 보고
                            LocalActionStep++;
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
                            CV_RunStop();
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
                        CurrentActionDesc = "라이트 커튼 재가동중";
                        if (!LightCurtainMuteControl(false))//라이트 커튼 작동
                        {
                            errorResult = new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "라이트 커튼 Mute 동작에 실패하였습니다.");
                            LocalActionStep = ErrorStep;
                        }
                        LocalActionStep++;
                        break;
                    case 7:
                        CurrentActionDesc = "트레이 배출 완료";
                        bStepEnd = true; //반송 프로세스 완료
                        LogManager.WriteConsoleLog(eLogLevel.Info, "CV :{0} 트레이 배출완료)", ModuleName);
                        NextCVCommand = eCVCommand.TrayLoad;

                        //lsj SESS Bumpjar
                        if (!CVStopperClose())
                        {
                            return new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "트레이 초기화중 스톱퍼 동작실패하였습니다.");
                        }

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

        public override bool DoAbnormalCheck()
        {
            if (GlobalData.Current.RMSection.RM1Element.RMType == eRMType.PLC_EHER || GlobalData.Current.RMSection.RM1Element.RMType == eRMType.PLC_UTL)
            {
                //2021.07.05 lim, 투입 port 기울어짐 센서 관련 추가
                if (IsInPort && CheckTrayLeanSensor())
                {
                    GlobalData.Current.Alarm_Manager.AlarmOccurbyName("ALIGNMENT_SENSOR_DETECTED", ModuleName);
                    return true;
                }
            }
            return base.DoAbnormalCheck();
        }
    }
}
