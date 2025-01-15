
using BoxPrint.Log;
using BoxPrint.Modules.Conveyor;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BoxPrint.Modules.CVLine
{
    /// <summary>
    /// 컨베이어 라인 (라인별 N개의 컨베이어 모듈이 들어간다.)
    /// </summary>
    public class CVLineModule : ModuleBase
    {
        //private bool bThreadExit = false;
        //private bool LineResetRequest = false;
        public const int ErrorStep = 999;
        //private readonly int LocalDelay = 100; // 로컬스텝 사이클 타임
        private LinkedList<CV_BaseModule> Internal_CVList;

        public eCVLineType LineType
        {
            get;
            private set;
        }
        private bool _LastMuteOnState = true;
        public bool LastMuteOnState
        {
            get
            {
                return _LastMuteOnState;
            }
            set
            {
                if (_LastMuteOnState != value)
                {
                    _LastMuteOnState = value;
                }
            }
        }
        public string LineFloor
        {
            get;
            protected set;
        }
        //220520 HHJ SCS 개선     //- CV UserControl 관련 개선
        //- CVDegree 속성 제거, Rotate 속성으로 변경
        //220316 HHJ SCS 개발     //- Layoutview C/V 회전, Port 겹침 추가
        //public int CVDegree
        public bool CVRotate
        {
            get;
            protected set;
        }

        public eCVWay CVWay { get; private set; }       //230214 HHJ SCS 개선

        private eCVLineCommand _CurrentCommand = eCVLineCommand.None;
        private eCVLineCommand CurrentCommand
        {
            get
            {
                return _CurrentCommand;
            }
            set
            {
                if (_CurrentCommand != value)
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "CurrentModule:{0} CurrentCommand:{1} NextCommand:{2} ", ModuleName, CurrentCommand, value);
                    //명령이 바뀌면 로그를 찍는다.
                    _CurrentCommand = value;
                }
            }
        }


        private int _LocalRunStep = 0;
        public int LocalRunStep
        {
            get { return _LocalRunStep; }
            private set
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "CVLines: {0} Local Step Before : {1} =>  Next : {2}", ModuleName, _LocalRunStep, value);
                if (value > ErrorStep)
                {
                    _LocalRunStep = ErrorStep;
                }
                else
                {
                    _LocalRunStep = value;
                }
                WriteCurrentState();
            }
        }
        public bool IsInPort
        {
            get
            {
                if (LineType == eCVLineType.AutoIn || LineType == eCVLineType.MaunalIn)
                    return true;
                else
                    return false;
            }
        }

        public int Position_Bank
        {
            get { return ModuleList.First.Value.Position_Bank; }
        }
        public int Position_Bay
        {
            get { return ModuleList.First.Value.Position_Bay; }
        }

        public int Position_Level
        {
            get { return ModuleList.First.Value.Position_Level; }
        }
        /// <summary>
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="simul"></param>
        public CVLineModule(string Name, bool simul, eCVLineType Type)
            : base(Name, simul)
        {
            ModuleName = Name;
            LineType = Type;
            SimulMode = simul;
            Internal_CVList = new LinkedList<CV_BaseModule>();
            //InitModulesIO(); //개별 CVLine IO 를 CCLink Manager 에 추가한다.
        }
        public bool ChangeAllPortInOutType(ePortInOutType RequestType)
        {
            if(CheckCarrierExistInLine()) //240730 RGJ 포트라인내 화물이 있으면 포트 타입 변경 불가.
            {
                return false;
            }
            bool ChangeAllDone = true;
            foreach (var cItem in ModuleList)
            {
                ChangeAllDone &= cItem.ChangePortInOutType(RequestType);
            }
            //220919 조숭진 추가
            if (ChangeAllDone)
            {
                GlobalData.Current.PortManager.portConfigDetailModify(this, "Direction");
            }

            return ChangeAllDone; //모든 포트 동작이 성공해야 True;
        }

        public void ChangeAllPortUseType(bool ReqUse)
        {
            foreach (var cItem in ModuleList)
            {
                cItem.CVUSE = ReqUse;
            }

            GlobalData.Current.PortManager.portConfigDetailModify(this, "USE");
        }

        private void InitModulesIO()
        {
            string fileName = GlobalData.Current.CurrentFilePaths(System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName)) + GlobalData.Current.PortIOFilePath + string.Format("IO_{0}.xml", ModuleName);
            bool success = CCLink.CCLinkManager.CCLCurrent.Add_IOList(fileName);
            if (!success)
            {
                throw new Exception(string.Format("InitModulesIO 에 실패하였습니다. {0} Config 와 I/O 파일을 확인하세요", ModuleName));
            }
        }
        public void SetFloorInfo(string Floor)
        {
            this.LineFloor = Floor;
        }

        //220316 HHJ SCS 개발     //- Layoutview C/V 회전, Port 겹침 추가
        //220520 HHJ SCS 개선     //- CV UserControl 관련 개선
        //public void SetDegree(string degree)
        public void SetDegree(string rotate)
        {
            //220520 HHJ SCS 개선     //- CV UserControl 관련 개선
            //CVDegree = int.TryParse(degree, out int idegree) ? idegree : 0;
            CVRotate = bool.TryParse(rotate, out bool brotate) ? brotate : false;
        }

        //230214 HHJ SCS 개선
        public void SetConveyorWay(string cvway)
        {
            if (!Enum.TryParse(cvway, out eCVWay way))
            {
                CVWay = eCVWay.BottomToTop;
                return;
            }

            CVWay = way;
        }

        public void LineEmergencyAction()
        {
            foreach (var cvItem in Internal_CVList)
            {

                cvItem.CV_RunEMGStop();
                cvItem.RequestAbort();
            }
        }
        public void RequestErrorReset()
        {
            //LineResetRequest = true;
            foreach (var item in Internal_CVList)
            {
                item.ReleaseAbort();
            }
        }
        public bool AddConveyor(CV_BaseModule CV)
        {
            if (Internal_CVList.Where(c => c.ModuleName == CV.ModuleName).Count() == 0) //리스트에 넣기전에 중복검사
            {
                CV.SetParentModule(this);
                if (Internal_CVList.Last != null)
                {
                    //리스트에 넣을때마다 컨베이어간 전후 관계를 설정
                    CV_BaseModule LastCV = Internal_CVList.Last.Value;
                    if (LastCV != null)
                    {
                        LastCV.SetAwayBoothCV(CV);
                        CV.SetToBoothCV(LastCV);
                    }
                }
                Internal_CVList.AddLast(CV);
                return true;
            }
            return false;
        }
        public bool CheckErrorState()
        {
            throw new NotImplementedException();
        }
        public string GetPortErrorCode()
        {
            throw new NotImplementedException();
        }
        public bool CheckAllPortIsOutputMode()
        {
            foreach (var cItem in ModuleList)
            {
                if(cItem.PortInOutType == ePortInOutType.OUTPUT)
                {
                    continue;
                }
                else
                {
                    return false;
                }
            }
            return true;
        }
        public bool CheckAllPortIsInputMode()
        {
            foreach (var cItem in ModuleList)
            {
                if (cItem.PortInOutType == ePortInOutType.INPUT)
                {
                    continue;
                }
                else
                {
                    return false;
                }
            }
            return true;
        }
        /// <summary>
        /// 컨베이러 라인에 하나에 캐리어라도 있는지 체크
        /// </summary>
        /// <returns></returns>
        public bool CheckCarrierExistInLine()
        {
            foreach (var cItem in ModuleList)
            {
                if (cItem.CarrierExistBySensor() || cItem.CarrierExistByData()) //데이터도 같이 본다.
                {
                    return true;
                }
                else
                {
                    continue;
                }
            }
            return false;
        }

        /// <summary>
        /// 컨베이어 라인에 해당 캐리어가 존재하는지 체크
        /// </summary>
        /// <param name="CarrierID"></param>
        /// <returns></returns>
        public bool CheckCarrierExistInLine(string CarrierID)
        {
            foreach (var cItem in ModuleList)
            {
                if (cItem.CarrierExistBySensor() && cItem.GetCarrierID() == CarrierID) //데이터도 같이 본다.
                {
                    return true;
                }
                else
                {
                    continue;
                }
            }
            return false;
        }

        public LinkedList<CV_BaseModule> ModuleList
        {
            get { return Internal_CVList; }
        }

        public bool CheckModuleName(string ModuleName)
        {
            return Internal_CVList.Where(n => n.ModuleName == ModuleName).Count() > 0;
        }

        public bool InitLineCV()
        {
            LogManager.WriteConsoleLog(eLogLevel.Info, "CVLine :{0} start initializing.", this.ModuleName);
            //추후 초기화 코드가 필요하면 넣자.
            foreach (var item in Internal_CVList)
            {
                item.CVInitAction();
            }
            LogManager.WriteConsoleLog(eLogLevel.Info, "CVLine :{0} initialized", this.ModuleName);
            return true;
        }
        #region 동작 변경으로 주석처리

        //public void StartCVLine()
        //{
        //    Thread run = new Thread(new ThreadStart(CVLineRun));
        //    run.Start();
        //}


        //private void CVLineRun()
        //{
        //    LogManager.WriteConsoleLog(eLogLevel.Info, "Module:{0} CVLineRun() Thread 동작을 시작합니다.", this.ModuleName);
        //    //초기화 동작하기전에 하위 모듈 생성이 완료 되기를 대기.

        //    bool InitComplete = InitCVline();
        //    //글로벌 모듈 생성이 끝날때까지 대기
        //    while (!GlobalData.Current.GlobalInitComp)
        //    {
        //        Thread.Sleep(LocalDelay);
        //    }
        //    //런 돌리기전에 복구할지 말지 결정
        //    bool Recovery = GlobalData.Current.LineManager.CheckRequire_CVlineRecovery(this.ModuleName);

        //    if (Recovery)
        //    {
        //        int RecoveryStep = GlobalData.Current.LineManager.GetRecovery_LineStep(this.ModuleName);
        //        LocalRunStep = RecoveryStep;
        //        LogManager.WriteConsoleLog(eLogLevel.Info, "[복구] CVLine Module: {0}  Step : {1} 상태를 복구합니다", this.ModuleName, RecoveryStep);
        //    }
        //    while (!bThreadExit)
        //    {
        //        try
        //        {
        //            if (!InitComplete || CurrentCommand != eCVLineCommand.AutoJobTask)
        //            {
        //                Thread.Sleep(LocalDelay);
        //                continue;
        //            }
        //            //해당 라인 에러 체크
        //            if (DoAbnormalCheck())
        //            {
        //                //정상 상태가 아니면 명령 수행 불가
        //                Thread.Sleep(LocalDelay);
        //                continue;
        //            }
        //            switch (LocalRunStep)
        //            {
        //                case 0:
        //                    bool LoadComplete = WaitTrayLoading(Recovery); //로딩 모듈이 로딩 수행
        //                    if (LoadComplete)
        //                    {
        //                        Recovery = false;
        //                        LocalRunStep++;
        //                    }
        //                    break;
        //                case 1:
        //                    bool TransComplete = DoTransPortTrayAction(Recovery); //로딩모듈에서 언로딩 모듈까지 전송
        //                    if (TransComplete)
        //                    {
        //                        Recovery = false;
        //                        LocalRunStep++;
        //                    }
        //                    else
        //                    {
        //                        LocalRunStep = ErrorStep;
        //                    }
        //                    break;
        //                case 2:
        //                    bool UnLoadComplete = WaitTrayUnloading(Recovery); //언로딩 모듈에서 언로딩 수행
        //                    if (UnLoadComplete)
        //                    {
        //                        Recovery = false;
        //                        LocalRunStep = 0; //다시 로딩스텝으로 되돌린다.
        //                    }
        //                    break;
        //                case ErrorStep:
        //                    //에러 처리 되면 0번스텝으로 되돌린다.
        //                    if (LineResetRequest)
        //                    {
        //                        LogManager.WriteConsoleLog(eLogLevel.Info, "Module {0} 해당 라인 Reset 요청 확인.", this.ModuleName);
        //                        LocalRunStep = 0; //다시 로딩스텝으로 되돌린다.
        //                        LogManager.WriteConsoleLog(eLogLevel.Info, "라인내 트레이 정보 삭제.");
        //                        foreach (var item in Internal_CVList)
        //                        {
        //                            item.RemoveTray();
        //                        }
        //                        LineResetRequest = false;
        //                    }
        //                    break;
        //            }
        //            Thread.Sleep(LocalDelay);
        //        }
        //        catch (Exception ex)
        //        {
        //            LogManager.WriteConsoleLog(eLogLevel.Info, "Module {0} 에서 예외가 발생하였습니다. {1}", this.ModuleName, ex.ToString());
        //        }
        //        Thread.Sleep(LocalDelay);
        //    }

        //}


        ////로드위치에서 트레이 입고동작을 대기.
        //public bool WaitTrayLoading(bool RequireRecovery = false)
        //{
        //    try
        //    {
        //        CV_BaseModule LoadCV = null;
        //        bool LoadResult = false;
        //        //Load 할 컨베이어 선택
        //        if (IsInPort) //In Port 는 끝단에서 로딩 동작 실행.
        //        {
        //            LoadCV = Internal_CVList.Last.Value;
        //        }
        //        else //Out Port 는 시작에서 로딩 동작 실행.
        //        {
        //            LoadCV = Internal_CVList.First.Value;
        //        }

        //        if (LoadCV != null)
        //        {
        //            LoadResult = LoadCV.CVTrayLoadAction(RequireRecovery);
        //            return LoadResult;
        //        }
        //        return false;
        //    }
        //    catch (Exception ex)
        //    {
        //        LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
        //        return false;
        //    }
        //}
        ////언로드위치에서 트레이 출고 동작을 대기
        //public  bool WaitTrayUnloading(bool RequireRecovery = false)
        //{
        //    try
        //    {
        //        CV_BaseModule UnloadCV = null;
        //        bool UnloadResult = false;
        //        //Load 할 컨베이어 선택
        //        if (IsInPort) //In Port 는 시작에서 언로딩 동작 실행.
        //        {
        //            UnloadCV = Internal_CVList.First.Value;
        //        }
        //        else //Out Port 는 끝단에서 언로딩 동작 실행.
        //        {
        //            UnloadCV = Internal_CVList.Last.Value;
        //        }

        //        if (UnloadCV != null)
        //        {
        //            UnloadResult = UnloadCV.CVTrayUnloadAction(RequireRecovery);
        //            return UnloadResult;
        //        }
        //        return false;
        //    }
        //    catch (Exception ex)
        //    {
        //        LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
        //        return false;
        //    }
        //}
        ////로드 위치에서 언로드 위치까지 트레이 전송
        //public  bool DoTransPortTrayAction(bool RequireRecovery = false)
        //{
        //    try
        //    {
        //        //라이튼 커튼 뮤팅 시킨다.
        //        bool MuteOnCheck = RequestLightCurtainMute(true);
        //        if(!MuteOnCheck)
        //        {
        //            LogManager.WriteConsoleLog(eLogLevel.Info, "{0} Line DoTransPortTrayAction() MuteOn Fail", this.ModuleName, RequireRecovery);
        //            return false;
        //        }
        //        if (IsInPort)
        //        {
        //            return DoTransPortTrayAction(Internal_CVList.Count() - 1, 0, RequireRecovery);  //입고 (외부에서 부스 방향)
        //        }
        //        else
        //        {
        //            return DoTransPortTrayAction(0, Internal_CVList.Count() - 1, RequireRecovery); //배출 (부스에서 외부 방향)
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
        //        return false;
        //    }
        //    finally
        //    {
        //        RequestLightCurtainMute(false);
        //    }
        //}
        //private bool DoTransPortTrayAction(int StartModuleIndex, int EndModuleIndex, bool RequireRecovery)
        //{
        //    LogManager.WriteConsoleLog(eLogLevel.Info, "{0} Line DoTransPortTrayAction() RecoveryOpt:{1} Starting...", this.ModuleName, RequireRecovery);
        //    int CVModuleCounter = 0;
        //    bool bTransFailed = false;
        //    eCVCommandType CommandType = RequireRecovery ? eCVCommandType.Recovery : eCVCommandType.Normal;
        //    Task<CV_ActionResult>[] TaskArray = null;
        //    CV_BaseModule StartCV = Internal_CVList.ElementAt(StartModuleIndex);
        //    CV_BaseModule EndCV = Internal_CVList.ElementAt(EndModuleIndex);
        //    if (EndModuleIndex > StartModuleIndex) //정방향 Out 포트
        //    {
        //        CVModuleCounter = EndModuleIndex - StartModuleIndex + 1;
        //        TaskArray = new Task<CV_ActionResult>[CVModuleCounter];
        //        for (int idx = StartModuleIndex; idx <= EndModuleIndex; idx++)
        //        {
        //            int CVIndex = idx;
        //            //개별 CV 별 비동기 쓰레드 가동
        //            TaskArray[CVIndex - StartModuleIndex] = Task<CV_ActionResult>.Factory.StartNew(() =>
        //            {
        //                return Internal_CVList.ElementAt(CVIndex).CV_TransActionProcess(new CV_ActionPara(CommandType, eCV_Speed.High));
        //            });
        //        }

        //        for (int idx = StartModuleIndex; idx <= EndModuleIndex; idx++)
        //        {
        //            //가동된 모듈이 차례대로 완료되길 대기
        //            TaskArray[idx].Wait(600000);//600초까지 대기

        //            if (TaskArray[idx].IsCompleted && TaskArray[idx].Result.actionResult == eCV_ActionResult.Complete) //동작성공
        //            {
        //                //UI 에게 이벤트 발생
        //            }
        //            else //실패
        //            {
        //                bTransFailed = true;
        //                //로그 및 실패 알람 발생.
        //            }
        //        }
        //    }
        //    else if (EndModuleIndex < StartModuleIndex) //역방향 In 포트
        //    {
        //        CVModuleCounter = StartModuleIndex - EndModuleIndex + 1;
        //        TaskArray = new Task<CV_ActionResult>[CVModuleCounter];
        //        for (int idx = StartModuleIndex; idx >= EndModuleIndex; idx--)
        //        {
        //            int CVIndex = idx;
        //            //개별 CV 별 비동기 쓰레드 가동
        //            TaskArray[StartModuleIndex - CVIndex] = Task<CV_ActionResult>.Factory.StartNew(() =>
        //            {
        //                return Internal_CVList.ElementAt(CVIndex).CV_TransActionProcess(new CV_ActionPara(CommandType, eCV_Speed.High));
        //            });
        //        }

        //        for (int idx = StartModuleIndex; idx >= EndModuleIndex; idx--)
        //        {
        //            //가동된 모듈이 차례대로 완료되길 대기
        //            TaskArray[idx].Wait(600000);//600초까지 대기

        //            if (TaskArray[idx].IsCompleted && TaskArray[idx].Result.actionResult == eCV_ActionResult.Complete) //동작성공
        //            {
        //                //UI 에게 이벤트 발생
        //            }
        //            else //실패
        //            {
        //                bTransFailed = true;
        //                //로그 및 실패 알람 발생.
        //            }
        //        }
        //    }
        //    else  //시작 지점과 목표 지점이 같으면 해당 컨베이어 정지 위치까지 전송후 완료
        //    {
        //        TaskArray = new Task<CV_ActionResult>[1];

        //        TaskArray[0] = Task<CV_ActionResult>.Factory.StartNew(() =>
        //        {
        //            return Internal_CVList.ElementAt(0).CV_TransActionProcess(new CV_ActionPara(CommandType, eCV_Speed.High));
        //        });
        //        //가동된 모듈이 차례대로 완료되길 대기
        //        TaskArray[0].Wait(600000);//600초까지 대기

        //        if (TaskArray[0].IsCompleted && TaskArray[0].Result.actionResult == eCV_ActionResult.Complete) //동작성공
        //        {
        //            //UI 에게 이벤트 발생
        //        }
        //        else //실패
        //        {
        //            bTransFailed = true;
        //            //로그 및 실패 알람 발생.
        //        }
        //    }
        //    //모든 동작이 완료되고 트레이가 정위치에 있으면 완료
        //    return !bTransFailed;
        //}
        #endregion

        public void SetCVLineCommand(eCVLineCommand command)
        {
            this.CurrentCommand = command;
            foreach (var cvitem in ModuleList)
            {
                if (command == eCVLineCommand.AutoJobTask)
                {
                    cvitem.SetAutoMode(eCVAutoManualState.AutoRun);
                }
                else
                {
                    cvitem.SetAutoMode(eCVAutoManualState.ManualRun);
                }
            }
        }
        public override bool DoAbnormalCheck()
        {
            return false;
        }

        public override void WriteCurrentState()
        {
            //Database.LocalDBManager.Current.UpdateCVLineState(this);
        }
        public override bool ReadLastState()
        {
            return true;
        }
        public bool IsContainManualPort()
        {
            return Internal_CVList.Where(c => c.CVModuleType == eCVType.Manual).Count() > 0;
        }
        public string GetInlineRobotIFPortName()
        {
            var RobotIF = Internal_CVList.Where(c => c.CVModuleType == eCVType.RobotIF).FirstOrDefault();
            if(RobotIF != null)
            {
                return RobotIF.ModuleName;
            }
            else
            {
                return "";
            }
        }

        //2024.08.13 lim, Auto Port Porttype 변경 막을 목적으로 추가
        public bool IsMultyCV()
        {
            return Internal_CVList.Count() > 1;
        }
    }
}
