using BoxPrint.DataList;
using BoxPrint.DataList.MCS;
using BoxPrint.Log;
using BoxPrint.Modules.Shelf;
using System;
using System.Diagnostics;
using System.Threading;

namespace BoxPrint.Modules.Conveyor
{
    /// <summary>
    /// 작업자와 인터페이스 하는 포트 모듈
    /// </summary>
    public class CV_ManualModule : CV_BaseModule
    {
        private string HostGenerateCarrierID;
        private CarrierItem KeyInCarrier = null;
        private bool ValidationNGReceived = false;
        private bool AutoKeyinReceived = false;     //2024.06.27 lim, AutoKeyin 수신 확인
        private bool AutoKeyinDataCheck = false;     //2024.06.27 lim, AutoKeyin 수신 확인

        //public readonly int ValidationWaitTime = 5; //테스트를 위해 길게 잡아둠      //220921 조숭진 base로 옮김.
        public CV_ManualModule(string mName, bool simul) : base(mName, simul)
        {
            CVModuleType = eCVType.Manual;
            PortType = ePortType.LP; //Loading Port
            _PortAccessMode = ePortAceessMode.MANUAL;
        }
        public override void SetKeyInCarrierItem(CarrierItem carrierItem)
        {
            KeyInCarrier = carrierItem;
        }
        public void ResetKeyInCarrierItem()
        {
            KeyInCarrier = null;
        }
        public override void SetCarrierGeneratorRequset(string CarrierID)
        {
            HostGenerateCarrierID = CarrierID;
        }
        public bool CheckKeyInComplete()
        {
            return KeyInCarrier != null;
        }
        public bool CheckHostGenerateCommandRecv()
        {
            if (SimulMode)
            {
                return true;
            }
            else
            {
                if (KeyInCarrier != null && KeyInCarrier.CarrierID == HostGenerateCarrierID) //Key In 받은 캐리어 아이디랑 호스트 생성 캐리어 아이디 일치 체크
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
        public bool CheckValidationNGState()
        {
            if (SimulMode)
            {
                return false; //시뮬에서는 상위 검증 스킵
            }
            return ValidationNGReceived;
        }
        public override void SetBuzzerState(eBuzzerControlMode buzzerMode, bool NeedAlarmOccur)
        {
            PC_Buzzer = buzzerMode;
            ValidationNGReceived = true; //메인 루프에서 처리하도록 플래그 On
            if (NeedAlarmOccur)
            {
                GlobalData.Current.Alarm_Manager.AlarmOccurbyName("PORT_VALIDATION_NG", ModuleName);
            }

        }
        //2024.06.27 lim, Auto Keyin 데이터 수신 확인
        public bool CheckAutoKeyinReceived()
        {
            if (SimulMode)
            {
                return true; //시뮬에서는 상위 검증 스킵
            }
            return AutoKeyinReceived;
        }
        //2024.06.27 lim, Auto Keyin 데이터 정합성 확인
        public bool CheckAutoKeyinData()
        {
            if (SimulMode)
            {
                return true; //시뮬에서는 상위 검증 스킵
            }
            return AutoKeyinDataCheck;
        }
        //2024.06.27 lim, Auto Keyin 데이터 입력
        public override void SetAutoKeyinState(string Pallet_Size, bool NeedAlarmOccur)
        {
            AutoKeyinReceived = true; //메인 루프에서 처리하도록 플래그 On
            AutoKeyinDataCheck = !NeedAlarmOccur;
            if (NeedAlarmOccur)
            {
                if (Pallet_Size == "SHORT")
                    GlobalData.Current.Alarm_Manager.AlarmOccurbyName("PORT_PALLETSIZE_DATA_MISMATCH_SHORT", ModuleName);
                else
                    GlobalData.Current.Alarm_Manager.AlarmOccurbyName("PORT_PALLETSIZE_DATA_MISMATCH_LONG", ModuleName);
            }
        }
        protected override void CVMainRun()
        {
            GlobalData.Current.MRE_GlobalDataCreatedEvent.WaitOne(); //모든 모듈 생성전까지 Run 대기
            GlobalData.Current.MRE_FirstPLCReadEvent.WaitOne(); //처음 PLC Read 전까지 Run 대기
            CV_ActionResult Result = null;
            NextCVCommand = eCVCommand.Initialize;
            //bool bFirstInit = true;
            PC_SCSMode = this.PortInOutType; //초기화 작업시 현재 포트 모드를 써준다.
            //230302 프로그램 첫 기동 시 resume전 reconsile을 위해 한번만 체크하게 한다.
            if (!CarrierExistBySensor())
            {
                ResetCarrierData();
            }
            else
            {
                if (CarrierStorage.Instance.GetInModuleCarrierItem(ModuleName) is CarrierItem carrier)
                {
                    UpdateCarrier(carrier.CarrierID, false);
                }
            }
            SetTrackPause(!CVUSE);//사이클 돌기전에 USE 상태에 따른 TrackPause 신호를 준다.
            PC_PortEnable = CVUSE;

            while (!ThreadExitRequested)
            {

                try //-메인 루프 예외 발생시 로그 찍도록 추가.
                {
                    //DefaultSlot.SetCarrierExist(PLC_CarrierSensor);     //221014 HHJ SCS 개선     //- C/V CarrierExist 실시간 반영
                    if (!CVUSE) //비사용 포트는 여기서 스탑
                    {
                        CurrentActionDesc = "Wait Port Enable";
                        Thread.Sleep(LocalStepCycleDelay);
                        continue;
                    }
                    if (DoAbnormalCheck()) //에러체크는 항상 하도록 변경.
                    {
                        NextCVCommand = eCVCommand.ErrorHandling;
                    }

                    if (AutoManualState != eCVAutoManualState.AutoRun && NextCVCommand != eCVCommand.ErrorHandling) //에러핸들링은 바로 들어간다.
                    {
                        //220803 조숭진 최초 이니셜 시 outofservice보고 s
                        //if (GlobalData.Current.MainBooth != null
                        //    && GlobalData.Current.MainBooth.CurrentOnlineState == eOnlineState.Remote
                        //    && bFirstInit == true)
                        //{
                        //    bFirstInit = false;
                        //    //S6F11 402
                        //    //GlobalData.Current.HSMS.SendS6F11(402, "PORT", this);
                        //}
                        //220803 조숭진 최초 이니셜 시 outofservice보고 e
                        //포트 타입 변경 요청이 있으면 변경한다.
                        if (CheckPLCPortTypeChangeRequest() && !CarrierExistBySensor())
                        {
                            ePortInOutType ReqType = PLC_PortType;
                            ChangePortInOutType(ReqType);
                        }
                        CurrentActionDesc = "Auto 모드를 기다립니다.";
                        Thread.Sleep(LocalStepCycleDelay);
                        continue;
                    }
                    //241202 RGJ 포트 Main Run 시스템 상태가 Auto 가 아니면 대기
                    if (GlobalData.Current.MainBooth.SCState != eSCState.AUTO)
                    {
                        if (NextCVCommand == eCVCommand.Initialize || NextCVCommand == eCVCommand.ErrorHandling)
                        {
                            ;//시스템 상태 오토 아니어도 들어가야 함.
                        }
                        else
                        {
                            Thread.Sleep(LocalStepCycleDelay);
                            continue;
                        }
                    }
                    switch (NextCVCommand)
                    {
                        case eCVCommand.Initialize:
                            Result = InitializeAction();
                            break;
                        case eCVCommand.AutoAction:
                            Result = CVAutoAction();
                            break;
                        case eCVCommand.WaitCarrierLoad:
                            Result = ManualCarrierLoadAction();
                            break;
                        case eCVCommand.WaitCarrierRemove:
                            Result = ManualCarrierUnloadAction();
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
                catch (Exception ex)
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
                }
            }

        }

        protected override CV_ActionResult InitializeAction()
        {
            LogManager.WriteConsoleLog(eLogLevel.Info, "{0} Module InitializeAction Start", this.ModuleName);
            CurrentActionDesc = "초기화 동작중입니다.";
            LocalActionStep = 0;
            PC_SCSMode = this.PortInOutType; //초기화 작업시 현재 포트 모드를 써준다.
            PC_TransferPossible = false; //초기화시 반송 명령 해제

            //2024.05.27 lim, bcr fail 비트 초기화 추가
            PC_BCRReadFail = false;

            //240102 rhj 포트 타입 변경 요청이 있으면 변경한다.
            if (CheckPLCPortTypeChangeRequest())
            {
                ePortInOutType ReqType = PLC_PortType;
                ChangePortInOutType(ReqType);
            }

            //PLC 상태 체크
            NextCVCommand = eCVCommand.AutoAction;

            //220803 조숭진 이니셜라이즈 완료 후 inservice 보고
            //GlobalData.Current.HSMS.SendS6F11(401, "PORT", this);

            return new CV_ActionResult(ModuleName, eCV_ActionResult.Complete, "InitializeAction 완료.");
        }

        protected override CV_ActionResult CVAutoAction()
        {
            if (IsInPort)
            {
                return ManualCarrierLoadAction();
            }
            else
            {
                if(CarrierExistBySensor()) 
                {
                    return ManualCarrierUnloadAction();//캐리어 감지되면 제거대기
                }
                else
                {
                    return ReceiveCarrierAction(); //캐리어 감지안되면 수신대기
                }
            }
        }
        /// <summary>
        /// Output 배출 모드 일때 안쪽 컨베이어로 부터 캐리어를 받는다.
        /// </summary>
        /// <returns></returns>
        protected override CV_ActionResult ReceiveCarrierAction()
        {
            LogManager.WriteConsoleLog(eLogLevel.Info, "{0} Module ReceiveCarrierAction Start", this.ModuleName);
            CV_ActionResult errorResult = new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "동작중 에러 발생하였습니다.");
            CurrentActionDesc = "메뉴얼 포트 오토 동작중입니다.";

            //다시 Carrier수신할때 BCR 플래그 해제
            PC_BCRReadComplete = false; //해당 비트 PLC 에서 오프 지만 스탭 초기화므로 OFF

            bool bStepEnd = false;
            LocalActionStep = 0;
            try
            {
                while (!bStepEnd)
                {
                    if (PortInOutTypeChanged)
                    {
                        PortInOutTypeChanged = false;
                        NextCVCommand = eCVCommand.Initialize;
                        Thread.Sleep(LocalStepCycleDelay);
                        return new CV_ActionResult(ModuleName, eCV_ActionResult.Complete, "PortInOutType 변경으로 CVAutoAction 중단.");
                    }
                    if (DoAbnormalCheck())
                    {
                        CurrentActionDesc = "Auto 모드를 기다립니다.";
                        NextCVCommand = eCVCommand.ErrorHandling;
                        return new CV_ActionResult(ModuleName, eCV_ActionResult.Aborted, "상태 이상 으로 CVAutoAction 중단.");
                    }
                    if (AutoManualState != eCVAutoManualState.AutoRun)
                    {
                        CurrentActionDesc = "Auto 모드를 기다립니다.";
                        Thread.Sleep(LocalStepCycleDelay);
                        return new CV_ActionResult(ModuleName, eCV_ActionResult.Complete, "메뉴얼 모드 변경으로 CVAutoAction 중단.");
                    }
                    if (CheckPLCPortTypeChangeRequest() && !CarrierExistBySensor())
                    {
                        NextCVCommand = eCVCommand.Initialize;
                        return new CV_ActionResult(ModuleName, eCV_ActionResult.Complete, "PLCPortTypeChangeRequest 요청으로 ReceiveCarrierAction 중단");
                    }
                    if (!CarrierExistBySensor() && DefaultSlot.MaterialExist) //실제 센서에 없으면 UI 업데이트
                    {
                        //DefaultSlot.SetCarrierExist(false); //UI 표현용 값 Reset
                    }
                    switch (LocalActionStep)
                    {
                        case 0: //Carrier 감지 대기
                            CurrentActionDesc = "PLC Carrier 감지 대기중";
                            if (CarrierExistBySensor()) //Carrier 감지체크
                            {
                                LocalActionStep++;
                            }
                            break;
                        case 1: 
                            CurrentActionDesc = "Carrier 데이터 검증중";
                            string ReadCarrierID = string.Empty;
                            //PLC 가 Write 한 데이터로 캐리어 데이터를 만든다.
                            CarrierItem InPortCarrier = CarrierStorage.Instance.CreateInPortCarrier(this);
                            if (string.IsNullOrEmpty(InPortCarrier.CarrierID)) //캐리어 아이디가 비었으면 UNK 라도 넣어야 함.
                            {
                                InPortCarrier.CarrierID = CarrierStorage.Instance.GetNewPortUnknownCarrierID();//UNK-XXXX 로 임시 저장.
                                CarrierStorage.Instance.InsertCarrier(InPortCarrier); //신규 UNK 화물을 스토리지에 넣어둔다.
                                PC_CarrierID = InPortCarrier.CarrierID;     //unk id를 tracking data에 써준다
                            }
                            UpdateCarrier(InPortCarrier.CarrierID);
                            McsJob Job = GlobalData.Current.McdList.GetCarrierJob(InPortCarrier.CarrierID);
                            GlobalData.Current.HSMS.SendS6F11(309, "CarrierItem", InPortCarrier, "JobData", Job, "PORT", this);  //CarrierWaitOut 309
                            if (IsTerminalPort) //끝단 포트면 반송 완료 처리함
                            {
                                ProcessCarrierJobEnd(DefaultSlot.MaterialName);
                            }
                            //S6F11 ZoneCapacityChanged Report 310
                            GlobalData.Current.HSMS.SendS6F11(310, "ZONENAME", ModuleName);
                            PC_TransferPossible = true; //CV 구동 지시
                            LocalActionStep++;
                            CurrentActionDesc = "캐리어 BCR Read 완료";
                            break;
                        case 2:
                            CurrentActionDesc = "캐리어 수신 프로세스 완료";
                            bStepEnd = true; //로드 프로세스 완료
                            Thread.Sleep(1000);
                            NextCVCommand = eCVCommand.WaitCarrierRemove; //수동 배출 대기
                            LogManager.WriteConsoleLog(eLogLevel.Info, "CV :{0} Carrier Transfer Done!)", ModuleName);
                            break;
                        case 50:
                            //아직 명확한 사양이 안나와서 대기
                            break;
                        case ErrorStep: //에러발생
                            CurrentActionDesc = "캐리어 로딩중 에러 발생 에러 스탭";
                            LogManager.WriteConsoleLog(eLogLevel.Info, "ReceiveCarrierAction {0} Error Occurred", ModuleName);
                            return errorResult;

                        default:
                            return new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "CV_ActionProcess 동작중 유효한 Step 이 아닙니다.");
                    }

                    Thread.Sleep(LocalStepCycleDelay);
                }
                return new CV_ActionResult(ModuleName, eCV_ActionResult.Complete, "캐리어 수신 완료");
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
                return new CV_ActionResult(ModuleName, eCV_ActionResult.Aborted, "예외 발생으로 인한 CVAutoAction 중단.");
            }
        }

        /// <summary>
        /// 작업자로 부터 캐리어 로딩을 받는다.
        /// </summary>
        /// <returns></returns>
        protected override CV_ActionResult ManualCarrierLoadAction()
        {
            LogManager.WriteConsoleLog(eLogLevel.Info, "{0} Module ManualCarrierLoadAction Start", this.ModuleName);
            CV_ActionResult errorResult = new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "동작중 에러 발생하였습니다.");
            CurrentActionDesc = "인포트 오토 동작중입니다.";
            CarrierItem InPortCarrier = null;
            bool LastBCRFailed = false;
            bool bStepEnd = false;
            LocalActionStep = 0;
            DateTime dtKeyInStart = DateTime.Now; //키인 타임아웃 기준시각설정
            PC_BCRReadComplete = false;
            try
            {
                while (!bStepEnd)
                {
                    if (PortInOutTypeChanged)
                    {
                        PortInOutTypeChanged = false;
                        NextCVCommand = eCVCommand.Initialize;
                        Thread.Sleep(LocalStepCycleDelay);
                        return new CV_ActionResult(ModuleName, eCV_ActionResult.Complete, "PortInOutType 변경으로 ManualCarrierLoadAction 중단.");
                    }
                    if (DoAbnormalCheck())
                    {
                        CurrentActionDesc = "Auto 모드를 기다립니다.";
                        NextCVCommand = eCVCommand.ErrorHandling;
                        return new CV_ActionResult(ModuleName, eCV_ActionResult.Aborted, "상태 이상 으로 ManualCarrierLoadAction 중단.");
                    }
                    if (AutoManualState != eCVAutoManualState.AutoRun)
                    {
                        CurrentActionDesc = "Auto 모드를 기다립니다.";
                        Thread.Sleep(LocalStepCycleDelay);
                        return new CV_ActionResult(ModuleName, eCV_ActionResult.Complete, "메뉴얼 모드 변경으로 ManualCarrierLoadAction 중단.");
                    }
                    if (CheckPLCPortTypeChangeRequest() && !CarrierExistBySensor())
                    {
                        NextCVCommand = eCVCommand.Initialize;
                        return new CV_ActionResult(ModuleName, eCV_ActionResult.Complete, "PLCPortTypeChangeRequest 요청으로 ReceiveCarrierAction 중단");
                    }
                    if (!CarrierExistBySensor() && DefaultSlot.MaterialExist) //실제 센서에 없으면 UI 업데이트
                    {
                        //DefaultSlot.SetCarrierExist(false); //UI 표현용 값 Reset
                    }
                    //PC_BCRReadFail = false; //해당 비트 PLC 에서 오프함.

                    #region 실 운용 로직
                    switch (LocalActionStep)
                    {
                        case 0: //Carrier 감지 대기
                            CurrentActionDesc = "PLC Carrier 감지 대기중";
                            if (CarrierExistBySensor()) //Carrier 감지체크
                            {
                                //DefaultSlot.SetCarrierExist(true); //UI 표현용 값 Set
                                LastBCRFailed = false;
                                //센서 값 유지 체크 로직 추가.
                                bool bSensorHunting = false;
                                Stopwatch timeWatch = Stopwatch.StartNew(); //센서 유지되는지 추가.헌팅성 방지
                                while (!IsTimeout_SW(timeWatch, 1)) //일단 1초 유지로 판단
                                {
                                    if (!CarrierExistBySensor()) //센서가 유지되지 못했다.
                                    {
                                        LogManager.WriteConsoleLog(eLogLevel.Info, "Port : {0} CarrierID receiving Sensor is Hunting! Step Backward.", this.ModuleName);
                                        bSensorHunting = true;
                                        break; //While break
                                    }
                                    Thread.Sleep(200);
                                }
                                if (bSensorHunting)
                                {
                                    //DefaultSlot.SetCarrierExist(false); //UI 표현용 값 Set
                                    LocalActionStep = 0;
                                    break;
                                }
                                PC_BCRReadFail = false; //Fail 비트 초기화
                                LocalActionStep++;
                            }
                            break;
                        case 1: //PLC 로부터 BCR Read Req 를 대기한다.
                            CurrentActionDesc = "PLC BCR Read Req 대기중";
                            if(PC_TransferPossible)
                            {
                                PC_TransferPossible = false; //혹시 구동지시 살아있으면 Off 한다.
                            }
                            if (PLC_BCRReadRequest && UseBCR) //BCR 요청 대기
                            {
                                ResetKeyInCarrierItem();//혹시 기존 키인 캐리어 데이터 남아있으면 삭제.
                                LogManager.WriteConsoleLog(eLogLevel.Info, "{0} Module Manual BCR Req On", this.ModuleName);
                                LocalActionStep++;
                                break;
                            }
                            else if (!UseBCR)
                            {
                                ResetKeyInCarrierItem();//혹시 기존 키인 캐리어 데이터 남아있으면 삭제.
                                LogManager.WriteConsoleLog(eLogLevel.Info, "{0} Module Manual No BCR Use Skip Step => Wait Key In", this.ModuleName);
                                LocalActionStep++; //Key In Step
                                break;
                            }
                            break;
                        case 2: //BCR Read 존재하면 Read수행
                            CurrentActionDesc = "Carrier BCR Read";
                            string ReadCarrierID = string.Empty;
                            if (UseBCR) //BCR 있으면 BCR Reading
                            {
                                PC_BCRReadFail = false; //BCR Fail 미리 초기화
                                ReadCarrierID = CVBCR_Read();
                                LogManager.WriteConsoleLog(eLogLevel.Info, "Module : {0} BCR ReadResult {1}", ModuleName, ReadCarrierID);
                                if (ReadCarrierID == "ERROR") //BCR 실패
                                {
                                    //READ BCR FAILED
                                    LastBCRFailed = true;
                                    //UNK-XXXX 언노운 캐리어 Write
                                    PC_CarrierID = CarrierStorage.Instance.GetNewPortUnknownCarrierID(); //Tracking Data UNK CarrierID Write
                                    Thread.Sleep(200);

                                    #region BCR 실패시 역물류로 빼는 스텝 유도
                                    PC_BCRReadFail = true;
                                    LocalActionStep = 8;
                                    #endregion

                                    LogManager.WriteConsoleLog(eLogLevel.Info, "Module:{0} Step:{1} ManualCarrierLoadAction  BCR Failed!", ModuleName, LocalActionStep);
                                    break;
                                }
                                else //BCR 성공
                                {
                                    dtKeyInStart = DateTime.Now;
                                    PC_CarrierID = ReadCarrierID; //실제 데이터로 Write
                                    Thread.Sleep(200);
                                }
                                //2024.06.26 lim, 셀버퍼는 자동 Keyin 기능 추가 예정
                                if (GlobalData.Current.MainSection.UseAutoKeyin)       //2024.07.06 lim, 자동 Keyin 스텝 변경
                                {
                                    Thread.Sleep(500);		//2024.08.05 lim, CarrierId Write 시간 추가
                                    LocalActionStep = 100;
                                }
                                else
                                    LocalActionStep++; //성공하면 Key In 으로 넘어간다.
                                break;
                            }
                            else //BCR 없으면 바로 KeyIn대기로 넘어간다.
                            {
                                LogManager.WriteConsoleLog(eLogLevel.Info, "Module:{0} Step:{1} ManualCarrierLoadAction NoBCR step Skip", ModuleName, LocalActionStep);
                                LocalActionStep++;
                            }
                            CurrentActionDesc = "캐리어 BCR Read 완료";
                            break;
                        case 3: //무조건 KeyIn 대기
                            CurrentActionDesc = "Wait Key In Data";
                            //작업자의 Key In 완료를 대기한다.
                            if (CheckKeyInComplete())
                            {
                                WriteTrackingData(KeyInCarrier); //입력받은 키인 데이터를 트래킹 데이터에 쓴다.
                                //CarrierGeneratorRequest CEID 312
                                //GlobalData.Current.HSMS.SendS6F11(312, "PORT", this, "CARRIERITEM", KeyInCarrier); //KEY IN 되었으면 생성 요청 보고 //20230306 해당 보고 미사용 삭제. 
                                PC_BCRReadComplete = true; //키인 받으면 ReadComplete On.
                                Thread.Sleep(1000);//PLC Key In 데이터 안정화대기
                                LocalActionStep++;
                            }
                            break;
                        case 4://KeyIn 입력받은 정보로 캐리어 생성 및 보고
                            string PLC_CarrierID = PC_CarrierID;  //240816 RGJ 포트에서 화물 중복 체크
                            if (GlobalData.Current.PortManager.CheckCarrierDuplicated(this, PLC_CarrierID)
                                || GlobalData.Current.McdList.IsJobListCheck_DUP_ID(this.ModuleName, PLC_CarrierID))
                            {
                                //존재시 들어온  캐리어 DUP 처리
                                string DupCarrierID = CarrierStorage.Instance.GetNewDuplicateUnknownCarrierID(PLC_CarrierID);
                                PC_CarrierID = DupCarrierID;
                                Thread.Sleep(1000); //Write 안정화 딜레이 추가.
                                LogManager.WriteConsoleLog(eLogLevel.Info, "Port Carriername Changed by Duplication. Port : {0} CarrierID :{1}", ModuleName, PLC_CarrierID);
                            }
                            InPortCarrier = CarrierStorage.Instance.CreateInPortCarrier(this); //KeyIn 받고 왔으므로 CarrierID 공백 체크 안함.

                            if (ShelfManager.Instance.CheckCarrierDuplicated(InPortCarrier.CarrierID)) //캐리어 중복 체크
                            {
                                InPortCarrier.LastReadResult = eIDReadStatus.DUPLICATE; //보고용 결과 입력
                            }
                            else if(LastBCRFailed)
                            {
                                InPortCarrier.LastReadResult = eIDReadStatus.FAILURE; //보고용 결과 입력
                            }

                            GlobalData.Current.HSMS.SendS6F11(601, "CarrierItem", InPortCarrier); //S6F11 CarrierIDRead 이벤트 발신 부터

                            if (ShelfManager.Instance.CheckCarrierDuplicated(InPortCarrier.CarrierID)) //보고후 캐리어 중복 체크 처리해야한다.
                            {
                                ShelfManager.Instance.ProcessCarrierDuplicated(InPortCarrier.CarrierID); //존재시 해당 캐리어 처리
                            }

                            CarrierStorage.Instance.InsertCarrier(InPortCarrier);
                            UpdateCarrier(InPortCarrier.CarrierID);
                            CurrentCarrier.CarrierState = eCarrierState.WAIT_IN;

                            //S6F11 WaitIn Report 308
                            GlobalData.Current.HSMS.SendS6F11(308, "CarrierItem", InPortCarrier, "Port", this);
                            LastCarrierWaitInTime = DateTime.Now;
                            //S6F11 ZoneCapacityChanged Report 310
                            GlobalData.Current.HSMS.SendS6F11(310, "ZONENAME", ModuleName);
                            ValidationNGReceived = false; //Flag 초기화
                            LocalActionStep++;
                            break;
                        case 5: //상위 Validation NG 내려오는지 체크
                            //LogManager.WriteConsoleLog(eLogLevel.Info, "메뉴얼 포트 : {0} Validation 대기중 TimeOut {1}.", ModuleName, ValidationWaitDuration);
                            if (IsTimeout_SW(SW_CarrierWaitInTime, ValidationWaitDuration))
                            {
                                //시간내에 밸디데이션NG 또는 작업이 안내려왔다. 직접 알람 발생.
                                GlobalData.Current.Alarm_Manager.AlarmOccurbyName("PORT_VALIDATION_TIMEOUT", ModuleName); //240116 RGJ 메뉴얼 포트 밸리데이션 타임아웃 추가.조범석 매니저 이름 변경 요청 진짜 NG 와 타임아웃 구분되어야 함 .
                                ValidationNGReceived = false; //Flag 초기화
                                NextCVCommand = eCVCommand.WaitCarrierRemove;
                                LogManager.WriteConsoleLog(eLogLevel.Info, "Module:{0} Step:{1} ManualCarrierLoadAction Validation Test Time Out!", ModuleName, LocalActionStep);
                                return new CV_ActionResult(ModuleName, eCV_ActionResult.Complete, "Validation NG 로 CVAutoAction 중단.");
                            }
                            else
                            {
                                if (CheckValidationNGState())
                                {
                                    ValidationNGReceived = false; //Flag 초기화
                                    NextCVCommand = eCVCommand.WaitCarrierRemove;
                                    LogManager.WriteConsoleLog(eLogLevel.Info, "Validation NG Received Load Aborted.", ModuleName);
                                    return new CV_ActionResult(ModuleName, eCV_ActionResult.Complete, "Validation NG 로 CVAutoAction 중단.");
                                }
                                else if (GlobalData.Current.McdList.IsJobListCheck(GetConnectedRobotIFModuleName(), InPortCarrier.CarrierID)) //반송 명령 내려왔으면 넘어간다.
                                {
                                    LogManager.WriteConsoleLog(eLogLevel.Info, "Module:{0} Step:{1} ManualCarrierLoadAction Validation Test Pass by Job Received.", ModuleName, LocalActionStep);
                                    //정상 캐리어 판정으로 보고 다음 포트로 보낸다.
                                    PC_TransferPossible = true; //CV 구동 지시
                                    LocalActionStep++;
                                    break;
                                }
                                Thread.Sleep(LocalStepCycleDelay);
                            }
                            break;

                        case 6://화물감지 센서와 BCR Reading Req Off 대기
                            CurrentActionDesc = "BCR 요청 비트 OFF 대기중";
                            if (PLC_BCRReadRequest == false && PLC_CarrierSensor == false)
                            {
                                ResetCarrierData();
                                //PC_BCRReadComplete = false; //해당 비트 PLC 에서 오프함.
                                //PC_TransferPossible = false; //해당 OFF 는 PLC 에서 하도록 사양체크.
                                LocalActionStep++;
                            }
                            break;
                        case 7: //메뉴얼 포트 로드 완료
                            CurrentActionDesc = "캐리어 반송프로세스 완료";
                            bStepEnd = true; //로드 프로세스 완료
                            NextCVCommand = eCVCommand.AutoAction; //캐리어 이동에는 관여하지 않는다. 바로 다음 BCR Req 신호대기
                            LogManager.WriteConsoleLog(eLogLevel.Info, "CV :{0} Carrier manual loading completed.)", ModuleName);
                            break;

                        case 8: //BCR ReadFail 처리 케이스 추가. 
                            CurrentActionDesc = "캐리어 로딩중 BCR 에러 발생";
                            if(PLC_BCRReadRequest == false) //BCR Read Req 꺼지는거 보고 BCR_Fail Off
                            {
                                Thread.Sleep(100); //잠시 대기후
                                PC_BCRReadFail = false; //PLC 에서 BCR 바로 꺼도 상관없다고 함.
                                bStepEnd = true; //프로세스 완료
                                NextCVCommand = eCVCommand.WaitCarrierRemove; //배출대기로 보낸다.
                                LogManager.WriteConsoleLog(eLogLevel.Info, "CV :{0} Carrier Manual BCR Fail Abort.)", ModuleName);
                                return new CV_ActionResult(ModuleName, eCV_ActionResult.Complete, "Manual BCR Failed");
                            }
                            break;

                            //2024.06.26 lim, 자동 Keyin 기능 추가 예정
                        case 100:   //자동 Keyin Data 확인 스탭
                            CurrentActionDesc = "Send Carrier ID to MCS";
                            //KeyInCarrier = ReadTrackingData();
                            if (!string.IsNullOrEmpty(PC_CarrierID))    //2024.08.05 lim, 공백일 경우 데이터 들어 올때까지 대기
                            {
                                LogManager.WriteConsoleLog(eLogLevel.Info, "Module:{0} Step:{1} PC_CarrierID {2}", ModuleName, LocalActionStep, PC_CarrierID);
                                InPortCarrier = CarrierStorage.Instance.CreateInPortCarrier(this); //KeyIn 받고 왔으므로 CarrierID 공백 체크 안함.

                                //MCS에 Product_Empty, Pallet_Size 값 요청 
                                LastCarrierWaitInTime = DateTime.Now;
                                GlobalData.Current.HSMS.SendS6F11(313, "PORT", this, "CarrierItem", InPortCarrier); //S6F11 InfoRequest 이벤트 
                                AutoKeyinReceived = false;

                                LocalActionStep++;
                            }

                            break;
                        case 101:   //상위 Carrier Data 정합성 체크
                            CurrentActionDesc = "Send Carrier ID to MCS";
                            if (IsTimeout_SW(SW_CarrierWaitInTime, ValidationWaitDuration) && GlobalData.Current.MainBooth.CurrentOnlineState == eOnlineState.Remote)
                            {
                                //시간내에 밸디데이션NG 또는 작업이 안내려왔다. 직접 알람 발생.
                                GlobalData.Current.Alarm_Manager.AlarmOccurbyName("PORT_INFO_REQUEST_TIMEOUT", ModuleName); 
                                AutoKeyinReceived = false; //Flag 초기화
                                NextCVCommand = eCVCommand.WaitCarrierRemove;
                                LogManager.WriteConsoleLog(eLogLevel.Info, "Module:{0} Step:{1} ManualCarrierLoadAction Info Request Time Out!", ModuleName, LocalActionStep);
                                return new CV_ActionResult(ModuleName, eCV_ActionResult.Complete, "Info Request Time Out 으로 CVAutoAction 중단.");
                            }
                            else
                            {
                                if (CheckValidationNGState())
                                {
                                    ValidationNGReceived = false; //Flag 초기화
                                    NextCVCommand = eCVCommand.WaitCarrierRemove;
                                    LogManager.WriteConsoleLog(eLogLevel.Info, "Validation NG Received Load Aborted.", ModuleName);
                                    return new CV_ActionResult(ModuleName, eCV_ActionResult.Complete, "Validation NG 로 CVAutoAction 중단.");
                                }
                                // info 데이터 수신 확인
                                else if (CheckAutoKeyinReceived())
                                {
                                    // 데이터 수신 후 정합성 비교
                                    AutoKeyinReceived = false; //Flag 초기화
                                    PC_BCRReadComplete = true; //키인 받으면 ReadComplete On.

                                    if (CheckAutoKeyinData())
                                    {
                                        LogManager.WriteConsoleLog(eLogLevel.Info, "Module:{0} Step:{1} ManualCarrierLoadAction AutoKeyin Pass.", ModuleName, LocalActionStep);
                                        //정상 캐리어 판정으로 보고 다음 포트로 보낸다.
                                        Thread.Sleep(1500);//PLC Key In 데이터 안정화대기 500 -> 1500ms 변경.
                                        LocalActionStep = 4;
                                    }
                                    else
                                    {
                                        NextCVCommand = eCVCommand.WaitCarrierRemove;
                                        LogManager.WriteConsoleLog(eLogLevel.Info, "Pallet Size Mismatch Load Aborted.", ModuleName);
                                        return new CV_ActionResult(ModuleName, eCV_ActionResult.Complete, "Pallet Size Mismatch 로 CVAutoAction 중단.");
                                    }
                                }
                                //2024.08.05 lim, Offline 일때 작업자가 데이터 넣으면 진행 하도록 수정
                                else if (CheckKeyInComplete())
                                {
                                    WriteTrackingData(KeyInCarrier); //입력받은 키인 데이터를 트래킹 데이터에 쓴다.
                                    //CarrierGeneratorRequest CEID 312
                                    //GlobalData.Current.HSMS.SendS6F11(312, "PORT", this, "CARRIERITEM", KeyInCarrier); //KEY IN 되었으면 생성 요청 보고 //20230306 해당 보고 미사용 삭제. 
                                    LogManager.WriteConsoleLog(eLogLevel.Info, "Module:{0} Step:{1} ManualCarrierLoadAction ManualKeyin Pass.", ModuleName, LocalActionStep);

                                    PC_BCRReadComplete = true; //키인 받으면 ReadComplete On.
                                    Thread.Sleep(1000);//PLC Key In 데이터 안정화대기
                                    LocalActionStep = 4;
                                }
                                Thread.Sleep(LocalStepCycleDelay);
                            }
                            break;
                        case ErrorStep: //에러발생
                            CurrentActionDesc = "캐리어 로딩중 에러 발생 에러 스탭";
                            LogManager.WriteConsoleLog(eLogLevel.Info, "ManualCarrierLoadAction => ERROR Occurred.");
                            return errorResult;

                        default:
                            return new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "CV_ActionProcess 동작중 유효한 Step 이 아닙니다.");
                    }

                    #endregion

                    Thread.Sleep(LocalStepCycleDelay);
                }
                return new CV_ActionResult(ModuleName, eCV_ActionResult.Complete, "Carrier 수신 완료");
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
                return new CV_ActionResult(ModuleName, eCV_ActionResult.Aborted, "예외 발생으로 인한 CVAutoAction 중단.");
            }
        }
        /// <summary>
        /// 작업자가  캐리어 언로딩(배출)을 기다린다.
        /// </summary>
        /// <returns></returns>

        /// <summary>
        /// 작업자 캐리어 제거를 대기한다
        /// </summary>
        /// <returns></returns>
        protected override CV_ActionResult ManualCarrierUnloadAction()
        {
            LogManager.WriteConsoleLog(eLogLevel.Info, "{0} Module ManualCarrierUnloadAction Start", this.ModuleName);
            CV_ActionResult errorResult = new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "동작중 에러 발생하였습니다.");
            CurrentActionDesc = "메뉴얼포트 캐리어 제거  동작중입니다.";
            LocalActionStep = 0;
            bool bStepEnd = false;
            try
            {
                while (!bStepEnd)
                {
                    if (PortInOutTypeChanged)
                    {
                        PortInOutTypeChanged = false;
                        NextCVCommand = eCVCommand.Initialize;
                        Thread.Sleep(LocalStepCycleDelay);
                        return new CV_ActionResult(ModuleName, eCV_ActionResult.Complete, "PortInOutType 변경으로 CVAutoAction 중단.");
                    }

                    if (DoAbnormalCheck())
                    {
                        CurrentActionDesc = "Auto 모드를 기다립니다.";
                        NextCVCommand = eCVCommand.ErrorHandling;
                        return new CV_ActionResult(ModuleName, eCV_ActionResult.Aborted, "상태 이상 으로 CVAutoAction 중단.");

                    }
                    if (AutoManualState != eCVAutoManualState.AutoRun)
                    {
                        CurrentActionDesc = "Auto 모드를 기다립니다.";
                        Thread.Sleep(LocalStepCycleDelay);
                        return new CV_ActionResult(ModuleName, eCV_ActionResult.Complete, "메뉴얼 모드 변경으로 CVAutoAction 중단.");
                    }
                    //231206 RGJ 메뉴얼 포트 해당 로직 삭제 어차피 이스텝으로 왔으면 제거가 되어야 한다.
                    //if (CheckPLCPortTypeChangeRequest() && !CarrierExistBySensor())
                    //{
                    //    NextCVCommand = eCVCommand.Initialize;
                    //    return new CV_ActionResult(ModuleName, eCV_ActionResult.Complete, "PLCPortTypeChangeRequest 요청으로 ReceiveCarrierAction 중단");
                    //}

                    switch (LocalActionStep)
                    {
                        case 0://메뉴얼로 제거를 할려면 일단 캐리어가 존재해야한다.
                            CurrentActionDesc = "PLC Carrier 감지 대기중";
                            PC_BCRReadFail = false; //Fail 비트 초기화
                            if (CarrierExistBySensor()) //투입 대기중
                            {
                                LocalActionStep++;
                            }
                            else //센서 감지안되면 이미 포트에서 제거한걸로 간주
                            {
                                //PC_TransferPossible = false; //해당 OFF 는 PLC 에서 하도록 사양체크.
                                //CarrierItem RemovedCarrier = CurrentCarrier;

                                CarrierItem RemovedCarrier = InSlotCarrier; //240604 RGJ CurrentCarrier 는 화물 감지 OFF 되면 Null 리턴 되므로 InSlotCarrier 로 변경함.
                                if (RemovedCarrier != null) //혹시 캐리어 데이터 없으면 관련보고 생략
                                {
                                    //231110 RGJ  메뉴얼 포트에서 없어지면 STK 도메인에서 캐리어 삭제 해야함.
                                    RemoveSCSCarrierData(); //STK 도메인 삭제
                                }
                                else
                                {
                                    LogManager.WriteConsoleLog(eLogLevel.Info, "ManualCarrierUnloadAction Module:{0} Step:{1} There is No Carrier Data to Report.", ModuleName, LocalActionStep);
                                }
                                //S6F11 ZoneCapacityChanged Report 310
                                GlobalData.Current.HSMS.SendS6F11(310, "ZONENAME", ModuleName);
                                bStepEnd = true; //언로드 프로세스 완료
                                NextCVCommand = eCVCommand.AutoAction; //캐리어 이동에는 관여하지 않는다. 바로 다음 BCR Req 신호대기
                                LogManager.WriteConsoleLog(eLogLevel.Info, "ManualCarrierUnloadAction CV :{0} Carrier is not detected -> Step Done.", ModuleName);
                            }
                            break;
                        case 1:
                            CurrentActionDesc = "PLC Carrier 감지 대기중";
                            if (!CarrierExistBySensor()) //PLC 에서 제거 확인
                            {
                                //CarrierItem RemovedCarrier = CurrentCarrier;
                                CarrierItem RemovedCarrier = InSlotCarrier; //240604 RGJ CurrentCarrier 는 화물 감지 OFF 되면 Null 리턴 되므로 InSlotCarrier 로 변경함.
                                if (RemovedCarrier != null) //혹시 캐리어 데이터 없으면 관련보고 생략
                                {
                                    //string RemoveID = RemovedCarrier.CarrierID;

                                    //220607 조숭진 db삭제를 위해 목적지 추가되어 false 삭제.
                                    //20220531 조숭진 이미 db에서는 지웠는데 여기서 또 지워가지고 insert가 된다. 그래서 false 추가
                                    //230407 조숭진 carrierinfo db에 남아 있어 수정.
                                    //RemoveCarrierData(false); //캐리어 센서 Off 되면 캐리어 포트에서 데이터 삭제

                                    //if (IsTerminalPort) //끝단 포트면 스토커내 캐리어 삭제 처리한다.
                                    //{
                                    //    RemoveSCSCarrierData(); //STK 도메인 삭제
                                    //}
                                    //else
                                    //{
                                    //    ResetCarrierData();
                                    //}

                                    LogManager.WriteConsoleLog(eLogLevel.Info, "ManualCarrierUnloadAction Module:{0} Step:{1} Carrier :{2} remove from STK Domain", ModuleName, LocalActionStep , RemovedCarrier.CarrierID);
                                    RemoveSCSCarrierData(); //240604 RGJ 투입모드라도 수동으로 제거 했으면 STK 도메인에서 삭제해야함..

                                    //PC_TransferPossible = false; //해당 OFF 는 PLC 에서 하도록 사양체크.
                                }
                                else
                                {
                                    LogManager.WriteConsoleLog(eLogLevel.Info, "ManualCarrierUnloadAction Module:{0} Step:{1} There is No Carrier Data to Report.", ModuleName, LocalActionStep);
                                }
                                //S6F11 ZoneCapacityChanged Report 310
                                GlobalData.Current.HSMS.SendS6F11(310, "ZONENAME", ModuleName);

                                bStepEnd = true; //언로드 프로세스 완료
                                NextCVCommand = eCVCommand.AutoAction; //캐리어 이동에는 관여하지 않는다. 바로 다음 BCR Req 신호대기
                                LogManager.WriteConsoleLog(eLogLevel.Info, "ManualCarrierUnloadAction CV :{0} 캐리어 언로드 완료)", ModuleName);
                            }
                            break;
                        case ErrorStep: //에러발생
                            CurrentActionDesc = "캐리어 로딩중 에러 발생 에러 스탭";
                            LogManager.WriteConsoleLog(eLogLevel.Info, "ManualCarrierUnloadAction {0} Error Occurred.", ModuleName);
                            return errorResult;

                        default:
                            return new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "CV_ActionProcess 동작중 유효한 Step 이 아닙니다.");
                    }

                }
                return new CV_ActionResult(ModuleName, eCV_ActionResult.Complete, "Carrier 메뉴얼 제거 완료");
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
                return new CV_ActionResult(ModuleName, eCV_ActionResult.Aborted, "예외 발생으로 인한 CVAutoAction 중단.");
            }
        }
    }
}
