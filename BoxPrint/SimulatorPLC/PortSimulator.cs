using PLCProtocol.Base;
using PLCProtocol.DataClass;
using BoxPrint.DataList;
using BoxPrint.Log;
using BoxPrint.Modules.Conveyor;
using System;
using System.ComponentModel;
using System.Threading;

namespace BoxPrint.SimulatorPLC
{
    public class PortSimulator : BaseSimulator
    {
        private eCVAutoManualState PortAutoManual = eCVAutoManualState.AutoRun;
        private bool BCRExist = false;
        private bool PassOnly = false;
        private ePortType PType = ePortType.LP;
        private eCVType CMType = eCVType.RobotIF;
        private ePortInOutType PInOutType = ePortInOutType.BOTH;
        private ePortSize PSize = ePortSize.Short;
        public CarrierItem PortSimulCarrier;
        private PortSimulator NextSimulPort = null;
        private PortSimulator PrevSimulPort = null;
        private CV_BaseModule BaseModule;
        private bool CarrierGenerationMode
        {
            get
            {
                return _DebugTestModes[0];
            }
            set
            {
                _DebugTestModes[0] = value;
                OnPropertyChanged(new PropertyChangedEventArgs("DebugTestModes"));
            }
        }


        public void SleepRandomTime(int MaxSleepMilisec)
        {
            Random r = new Random();
            int sleepTime = r.Next(10, MaxSleepMilisec);
            Thread.Sleep(sleepTime);
        }

        public bool IsLineEndPoint
        {
            get
            {
                return NextSimulPort == null;
            }
        }

        private bool PSimulCarrierExist
        {
            get
            {
                return PortSimulCarrier != null;
            }
        }
        public string NextSimulPortName
        {
            get
            {
                if (BaseModule != null && BaseModule.NextCV != null)
                {
                    return "PLC_" + BaseModule.NextCV.ModuleName;
                }
                else
                {
                    return string.Empty;
                }
            }
        }
        public string PrevSimulPortName
        {
            get
            {
                if (BaseModule != null && BaseModule.PrevCV != null)
                {
                    return "PLC_" + BaseModule.PrevCV.ModuleName;
                }
                else
                {
                    return string.Empty;
                }
            }
        }
        public PortSimulator()
        {
            PLCModuleType = "PORT";
        }
        public void SetBase(CV_BaseModule cv)
        {
            if (BaseModule == null)
            {
                BaseModule = cv;
            }
        }
        public void RemoveSimulCarrier()
        {
            //231025 HHJ 빈값으로 초기화 필요.
            CarrierItem cItem = new CarrierItem();
            PC_CarrierID = cItem.CarrierID;
            PC_DestCPUNum = cItem.DestCpu;
            PC_DestTrackNum = cItem.DestTrack;
            PC_TrayType = cItem.TrayType;
            PC_TrayStackCount = cItem.SV_TrayStackCount;
            PC_Polarity = cItem.Polarity;
            PC_ProductEmpty = cItem.ProductEmpty;
            PC_WinderDirection = cItem.WinderDirection;
            PC_ProductQuantiry = (short)cItem.ProductQuantity;
            PC_CellPackingLine = cItem.SV_FinalLoc;
            PC_InnerTrayType = cItem.InnerTrayType;
            PC_PalletSize = cItem.PalletSize;
            PC_UnCoatedPart = cItem.UncoatedPart;
            PC_CoreType = cItem.CoreType;
            PC_ProductEnd = cItem.ProductEnd;
            PC_ValidationNG = cItem.SV_ValidationNG;

            PortSimulCarrier = null;
            PLC_CarrierSensor = false;
        }

        public void ResetSimulCarrier()
        {
            PortSimulCarrier = null;
            InitPLCSignal();
            ActionStep = 0;
        }

        public bool TransferCarrierItem(PortSimulator To)
        {
            if (To != null)
            {
                if (!To.PSimulCarrierExist)
                {
                    To.RecvCarrierItem(PortSimulCarrier);
                    RemoveSimulCarrier();
                    return true;
                }
            }
            return false;
        }
        public void SetNextSimulPort(PortSimulator next)
        {
            NextSimulPort = next;
        }
        public void SetPrevSimulPort(PortSimulator Prev)
        {
            PrevSimulPort = Prev;
        }
        public void ToggleGenerationMode()
        {
            CarrierGenerationMode = !CarrierGenerationMode;
        }

        public void SetSimulProperty(eCVType CVType, ePortType pType, bool bcrExist, bool PlainPass, ePortInOutType InOutType)
        {
            CMType = CVType; //LKJ 추가
            PType = pType;
            BCRExist = bcrExist;
            PassOnly = PlainPass;
            PInOutType = InOutType;
        }

        public override void SetPLCAddress(int plcNum, int PLCReadOffset, int PLCWriteOffset)
        {
            PLCtoPC = ProtocolHelper.GetPLCItem(eAreaType.PLCtoPC, "CV", (short)plcNum, (ushort)PLCReadOffset);
            PCtoPLC = ProtocolHelper.GetPLCItem(eAreaType.PCtoPLC, "CV", (short)plcNum, (ushort)PLCWriteOffset);
        }
        public void CreateNewSimulCarrier()
        {
            if (PortSimulCarrier == null)
            {
                CarrierItem cItem = new CarrierItem();
                //적당히 값을 넣어둔다.
                cItem.CarrierID = CarrierStorage.Instance.GetNewSimulCarrierID2();
                cItem.TrayType = eTrayType.FULL;
                cItem.TrayStackCount = "10";
                cItem.Polarity = ePolarity.ANODE;
                cItem.ProductEmpty = eProductEmpty.FULL;
                cItem.WinderDirection = eWinderDirection.NONE;
                cItem.ProductQuantity = 200;
                cItem.InnerTrayType = eInnerTrayType.FULL;
                if (PSize == ePortSize.Short)
                {
                    cItem.PalletSize = ePalletSize.Cell_Short;
                }
                else if (PSize == ePortSize.Long)
                {
                    cItem.PalletSize = ePalletSize.Cell_Long;
                }
                cItem.CarrierSize = CarrierItem.ConvertCarrierSize(cItem.PalletSize);
                cItem.UncoatedPart = eUnCoatedPart.FRONT;
                cItem.ValidationNG = "0";


                PortSimulCarrier = cItem;
                //PC 트래킹 데이터에 쓴다.
                PC_CarrierID = cItem.CarrierID;
                PC_DestCPUNum = cItem.DestCpu;
                PC_DestTrackNum = cItem.DestTrack;
                PC_TrayType = cItem.TrayType;
                PC_TrayStackCount = cItem.SV_TrayStackCount;
                PC_Polarity = cItem.Polarity;
                PC_ProductEmpty = cItem.ProductEmpty;
                PC_WinderDirection = cItem.WinderDirection;
                PC_ProductQuantiry = (short)cItem.ProductQuantity;
                PC_CellPackingLine = cItem.SV_FinalLoc;
                PC_InnerTrayType = cItem.InnerTrayType;
                PC_PalletSize = cItem.PalletSize;
                PC_UnCoatedPart = cItem.UncoatedPart;
                PC_CoreType = cItem.CoreType;
                PC_ProductEnd = cItem.ProductEnd;
                PC_ValidationNG = cItem.SV_ValidationNG;

                PLC_CarrierSensor = true;       //231025 HHJ Carrier Create 시 Bit Update
            }
        }
        public bool RecvCarrierItem(CarrierItem cItem)
        {
            if (cItem == null)
            {
                return false;
            }
            if (PSimulCarrierExist)
            {
                return false;
            }
            PortSimulCarrier = cItem;


            //PC 트래킹 데이터에 쓴다.
            PC_CarrierID = cItem.CarrierID;
            PC_DestCPUNum = cItem.DestCpu;
            PC_DestTrackNum = cItem.DestTrack;
            PC_TrayType = cItem.TrayType;
            PC_TrayStackCount = cItem.SV_TrayStackCount;
            PC_Polarity = cItem.Polarity;
            PC_ProductEmpty = cItem.ProductEmpty;
            PC_WinderDirection = cItem.WinderDirection;
            PC_ProductQuantiry = (short)cItem.ProductQuantity;
            PC_CellPackingLine = cItem.SV_FinalLoc;
            PC_InnerTrayType = cItem.InnerTrayType;
            PC_PalletSize = cItem.PalletSize;
            PC_UnCoatedPart = cItem.UncoatedPart;
            PC_CoreType = cItem.CoreType;
            PC_ProductEnd = cItem.ProductEnd;
            PC_ValidationNG = cItem.SV_ValidationNG;
            return true;
        }



        public void UpdateCarrierFromTrackingData()
        {
            CarrierItem cItem = new CarrierItem();
            cItem.CarrierID = PC_CarrierID;
            cItem.Destination = ConvertDestination(PC_DestCPUNum, PC_DestTrackNum);
            cItem.TrayType = PC_TrayType;
            cItem.TrayStackCount = PC_TrayStackCount.ToString();
            cItem.Polarity = PC_Polarity;
            cItem.ProductEmpty = PC_ProductEmpty;
            cItem.WinderDirection = PC_WinderDirection;
            cItem.ProductQuantity = PC_ProductQuantiry;
            cItem.FinalLoc = PC_CellPackingLine.ToString();
            cItem.InnerTrayType = PC_InnerTrayType;
            cItem.PalletSize = PC_PalletSize;
            cItem.CarrierSize = CarrierItem.ConvertCarrierSize(cItem.PalletSize);

            cItem.UncoatedPart = PC_UnCoatedPart;
            cItem.CoreType = PC_CoreType;
            cItem.ProductEnd = PC_ProductEnd;
            cItem.ValidationNG = PC_ValidationNG.ToString();


            PortSimulCarrier = cItem;
        }
        private void InitPLCSignal()
        {
            PLC_LoadRequest = false;
            PLC_LoadComplete = false;
            PLC_UnloadRequest = false;
            PLC_UnloadComplete = false;

            PLC_BCRReadRequest = false;
            PLC_CarrierSensor = false;
        }
        public override void PLCAutoCycleRun()
        {
            PC_McsSelect = 1;//LKJ 임의로
            PLC_KeySwitch = true;
            PLC_PortType = PInOutType;
            PLC_PortSizeShort = (BaseModule.PortSize == ePortSize.Short);
            while (!PLCExit)
            {
                if (!PLCRunState)
                {
                    Thread.Sleep(PLCCycleTime);
                    continue;
                }
                if (PC_McsSelect == 0)
                {
                    Thread.Sleep(PLCCycleTime);
                    continue;
                }
                if (SelfAlarmClearReq)
                {
                    SelfAlarmClearReq = false;
                    PLC_AlarmClear = true;
                    DateTime dt = DateTime.Now;
                    while (!IsTimeOut(dt, 3))
                    {
                        if(PC_CIMReportComp)
                        {
                            break;
                        }
                    }
                    PLC_AlarmClear = false;
                    CurrentAlarmCode = 0;
                    CurrentWarninCode = 0;
                    PLC_ErrorCode = CurrentAlarmCode;
                }
                if (PC_CIMAlarmClear)
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "PortSimulator : {0}  PC_CIMAlarmClear On Check!", this.SPLC_Name);
                    PLC_AlarmClear = true;
                    CurrentAlarmCode = 0;
                    CurrentWarninCode = 0;
                    PLC_ErrorCode = CurrentAlarmCode;
                    DateTime dt = DateTime.Now;
                    while (!IsTimeOut(dt, 3))
                    {
                        if (PC_CIMReportComp)
                        {
                            break;
                        }
                    }
                    PLC_AlarmClear = false;
                    Thread.Sleep(100);
                }
                else
                {

                    //SCS에서 내려주는 알람 체크
                    short pcACode = PC_ErrorCode;
                    if (pcACode > 0)
                    {
                        CurrentAlarmCode = pcACode;
                    }
                    if (CurrentAlarmCode > 0)
                    {
                        PLC_ErrorCode = CurrentAlarmCode;
                        Thread.Sleep(PLCCycleTime);
                        continue;
                    }
                }

                if (PC_PortTypeChange)  //포트 타입 체인지 시뮬 구현
                {
                    PortChangeAction();
                    ActionStep = 0;
                }
                if (PInOutType == ePortInOutType.INPUT)
                {
                    if(CarrierGenerationMode)
                    {
                        CreateNewSimulCarrier();
                    }
                    LoadAction();
                }
                else if (PInOutType == ePortInOutType.OUTPUT)
                {
                    UnloadAction();
                }
                //동작처리 구현 추가.
                Thread.Sleep(PLCCycleTime);
            }
        }

        public void ToggleAutoManual()
        {
            if(PortAutoManual == eCVAutoManualState.AutoRun)
            {
                PLC_KeySwitch = false;
                PortAutoManual = eCVAutoManualState.ManualRun;
                StopSimulPLC();

            }
            else
            {
                PLC_KeySwitch = true;
                PortAutoManual = eCVAutoManualState.AutoRun;
                StartSimulPLC();
            }
        }

        private void PortChangeAction()
        {
            PInOutType = PC_SCSMode;

            var temp = NextSimulPort; //앞뒤 시뮬 포트 방향 전환
            NextSimulPort = PrevSimulPort;
            PrevSimulPort = temp;


            PLC_PortTypeChange = true;
            PLC_PortType = PInOutType;
            Thread.Sleep(200);
            PLC_PortTypeChange = false;
        }
        //Booth 로 캐리어 밀어넣는 시퀀스
        public void LoadAction()
        {
            if(PC_TrackPause > 0)
            {
                return;
            }

            //240808 HoN 시뮬레이션 관련 수정
            //-> 캐리어가 없는데 ActionStep이 0이 아니라면 0으로 초기화 해준다.
            if (!PSimulCarrierExist && !ActionStep.Equals(0))
                ActionStep = 0;

            switch (ActionStep)
            {
                case 0: //Carrier 감지 대기
                    if (PSimulCarrierExist)
                    {
                        PLC_CarrierSensor = true;
                        if (IsLineEndPoint && PInOutType == ePortInOutType.INPUT)
                        {
                            PLC_UnloadRequest = true;
                        }
                        Thread.Sleep(1000);
                        ActionStep++;
                    }
                    break;
                case 1: //BCR Read 및 데이터 검증 요청
                    if (BCRExist)
                    {
                        PLC_BCRReadRequest = true;
                    }
                    PLC_CarrierSensor = true;
                    Thread.Sleep(200);
                    ActionStep++;
                    break;
                case 2: //SCS BCR Read 완료대기
                    if (BCRExist)
                    {
                        if (PC_BCRReadComplete)
                        {
                            Thread.Sleep(200);
                            PLC_BCRReadRequest = false; //BCR 요청 비트 Off
                            ActionStep++;
                        }
                    }
                    else
                    {
                        ActionStep++;
                    }
                    break;
                case 3: //TransferPossible 대기
                    if(!PLC_CarrierSensor && PortSimulCarrier == null)
                    {
                        //캐리어가 도중에 강제로 없어지면 스텝 복귀한다.
                        ActionStep = 0;
                        break;
                    }
                    if (IsLineEndPoint)
                    {
                        PLC_BCRReadRequest = false; //BCR 요청 비트 Off
                        ActionStep++;
                    }
                    else
                    {
                        if (PassOnly || PC_TransferPossible)
                        {
                            PLC_BCRReadRequest = false; //BCR 요청 비트 Off
                            ActionStep++;
                        }
                    }
                    break;
                case 4:
                    if (IsLineEndPoint)
                    {
                        if (PInOutType == ePortInOutType.INPUT)
                        {
                            ActionStep = 10;
                            PLC_UnloadRequest = true;
                            Thread.Sleep(1000);

                        }
                        else if (PInOutType == ePortInOutType.OUTPUT) //배출 포트 끝단이면 그냥 캐리어 삭제
                        {
                            RemoveSimulCarrier();
                            ActionStep++;
                        }
                    }
                    else
                    {
                        UpdateCarrierFromTrackingData();
                        if (!NextSimulPort.PSimulCarrierExist) //캐리어 넘길수 있으면 넘기기
                        {
                            Thread.Sleep(SimulCycleTime);
                            bool TResult = TransferCarrierItem(NextSimulPort); //시뮬이므로 가상으로 데이타만 옮긴다.
                            if (TResult)
                            {
                                PLC_UnloadRequest = false;
                                PLC_BCRReadRequest = false;
                                PLC_CarrierSensor = false;

                                PC_TransferPossible = false; //PLC 가 off함.
                                PC_BCRReadComplete = false;

                                PortSimulCarrier = null; //초기화
                                ActionStep++;
                                Thread.Sleep(1000);
                            }
                        }
                    }
                    break;
                case 5://SCS Bit Off 대기
                    if (!PC_BCRReadComplete && !PC_TransferPossible)
                    {
                        ActionStep = 0;
                        Thread.Sleep(1000);
                    }
                    break;
                case 10:// PIO Bit On       
                    if (BaseModule.RobotAccessAble)
                    {

                        // PLC_UnloadRequest = true;
                        ActionStep++;

                    }
                    break;
                case 11:
                    if (BaseModule.CarrierGetComplete)
                    {

                        PLC_UnloadRequest = false;
                        PLC_BCRReadRequest = false;
                        PLC_CarrierSensor = false;
                        PortSimulCarrier = null; //초기화
                        ActionStep = 0;
                        Thread.Sleep(1000);

                    }
                    //Crane ForkingBit On 대기
                    break;
                default:
                    break;
            }
        }
        //Booth 에서 캐리어 가져오는 시퀀스
        public void UnloadAction()
        {
            if (PC_TrackPause > 0)
            {
                return;
            }
            switch (ActionStep)
            {
                case 0: //Unload Request
                    if (CMType == eCVType.RobotIF || CMType == eCVType.WaterPool)
                    {
                        PLC_LoadRequest = true;
                        //PortSimulCarrier = BaseModule.CurrentCarrier;
                        ActionStep = 10; //HS Step
                    }
                    else
                    {
                        ActionStep++;
                    }
                    break;
                case 1: //Carrier 감지 대기
                    if (PSimulCarrierExist)
                    {
                        ActionStep++;
                    }
                    break;
                case 2: //BCR Read 및 데이터 검증 요청
                    if (BCRExist)
                    {
                        PLC_BCRReadRequest = true;
                    }
                    PLC_CarrierSensor = true;
                    ActionStep++;
                    break;
                case 3: //SCS BCR Read 완료대기
                    if (BCRExist && this.PInOutType == ePortInOutType.INPUT)
                    {
                        if (PC_BCRReadComplete)
                        {
                            PortSimulCarrier.CarrierID = PC_CarrierID;
                            ActionStep++;
                        }
                    }
                    else
                    {
                        Thread.Sleep(SimulCycleTime);
                        ActionStep++;
                    }
                    break;
                case 4: //TransferPossible 대기
                    if (PassOnly || PC_TransferPossible)
                    {

                        ActionStep++;
                    }
                    break;
                case 5:
                    if (IsLineEndPoint)
                    {
                        if (PInOutType == ePortInOutType.INPUT)
                        {

                            PC_TransferPossible = false; //PLC 가 off함.
                            PC_BCRReadComplete = false;

                            ActionStep = 10;

                        }
                        else if (PInOutType == ePortInOutType.OUTPUT) //배출 포트 끝단이면 그냥 캐리어 삭제
                        {
                            PC_TransferPossible = false; //PLC 가 off함.
                            PC_BCRReadComplete = false;
                            
                            Thread.Sleep(5000); //대기 시간 5초 있다 삭제
                            RemoveSimulCarrier();
                            PLC_BCRReadRequest = false;
                            PLC_CarrierSensor = false;



                            Thread.Sleep(SimulCycleTime);
                            ActionStep++;
                        }
                    }
                    else
                    {
                        if (!NextSimulPort.PSimulCarrierExist) //캐리어 넘길수 있으면 넘기기
                        {
                            bool TResult = TransferCarrierItem(NextSimulPort); //시뮬이므로 가상으로 데이타만 옮긴다.
                            if (TResult)
                            {

                                PC_TransferPossible = false; //PLC 가 off함.
                                PC_BCRReadComplete = false;

                                PLC_BCRReadRequest = false;
                                PLC_CarrierSensor = false;
                                ActionStep++;
                            }
                        }
                    }
                    break;
                case 6://SCS Bit Off 대기
                    if (!PC_BCRReadComplete && !PC_TransferPossible)
                    {
                        Thread.Sleep(SimulCycleTime);
                        PLC_BCRReadRequest = false;
                        PLC_CarrierSensor = false;
                        ActionStep = 0;
                    }
                    break;
                case 10:
                    //Crane ForkingBit On 대기
                    if (BaseModule.CarrierPutComplete)
                    {
                        PortSimulCarrier = BaseModule.CurrentCarrier;
                        RecvCarrierItem(PortSimulCarrier);
                        ActionStep++;
                    }
                    break;
                case 11:
                    if (PSimulCarrierExist) //데이터를 써주면 확인 가능할텐데 못해서.....
                    {
                        //241030 HoN 화재 관련 추가 수정        //화재수조는 화물감지 켜지않는다. 화재수조가 아닐때만 화물감지 켜준다.
                        if (CMType != eCVType.WaterPool)
                        {
                            PLC_CarrierSensor = true;
                        }
                        
                        ActionStep++;
                    }
                    break;
                case 12:
                    if (BaseModule.PC_TransferPossible && !BaseModule.CarrierPutComplete)
                    {
                        if (IsLineEndPoint && CMType == eCVType.RobotIF)
                        {
                            RemoveSimulCarrier();
                            PC_TransferPossible = false; //PLC 가 off함.
                            PLC_BCRReadRequest = false;
                            PLC_CarrierSensor = false;
                            PLC_LoadRequest = false;
                            PortSimulCarrier = null; //초기화
                            ActionStep = 0;
                        }
                        else if (!NextSimulPort.PSimulCarrierExist) //데이터가 없어서 아마 null익셥션 날듯 한번해보자
                        {
                            bool TResult = TransferCarrierItem(NextSimulPort); //시뮬이므로 가상으로 데이타만 옮긴다.
                            if (TResult)
                            {
                                PC_TransferPossible = false; //PLC 가 off함.
                                PLC_BCRReadRequest = false;
                                PLC_CarrierSensor = false;
                                PLC_LoadRequest = false;
                                PortSimulCarrier = null; //초기화
                                ActionStep = 0;
                            }
                        }
                        else
                        {
                            //임시 필요없을지도...
                            //Thread.Sleep(SimulCycleTime);
                            //PC_TransferPossible = false; //PLC 가 off함.
                            //PLC_BCRReadRequest = false;
                            //PLC_CarrierSensor = false;
                            //PLC_LoadRequest = false;
                            //PortSimulCarrier = null; //초기화
                            //ActionStep = 0;
                        }
                        Thread.Sleep(SimulCycleTime);
                    }
                    //241030 HoN 화재 관련 추가 수정        //대기로직 변경
                    else if (CMType == eCVType.WaterPool)
                    {
                        //여기까지오면 반송 완료된거 LR OFF한다.      //더 받을 수 없으니 기존처럼 스텝을 변경시키지는 않는다.
                        PLC_LoadRequest = false;
                    }
                    break;
                default:
                    break;
            }
        }

        #region PLCInterface

        #region Tracking Read/Write Data  Area
        //사용 예시) 확장은 필요없을것으로 생각됨. 특정 모듈에 기재해야하는 비트 / 워드 존재 시, 확장된 모듈에 추가를 하면됨.
        public string PC_CarrierID
        {
            get
            {
                string readCarrierID = GData.protocolManager.ReadString(SPLC_Name, PCtoPLC, "PC_CarrierID");
                return readCarrierID.Replace(" ", "");//공백 제거    
            }

            set
            {
                if (value.Length > 40) //40자리 제한
                {
                    GData.protocolManager.Write(SPLC_Name, PCtoPLC, "PC_CarrierID", (value.Substring(0, 40)).Replace(" ", ""));
                }
                else
                {
                    GData.protocolManager.Write(SPLC_Name, PCtoPLC, "PC_CarrierID", value.Replace(" ", ""));
                }
            }
        }
        /// <summary>
        /// Track No는 상위 바이트에는 CPU Group 번호, 하위 바이트에는 CV Number를 기재한다.
        /// Ex)Track번호 21099 -> 상위 바이트 0x15, 하위바이트 0x63 기입
        /// </summary>
        public short PC_DestCPUNum
        {
            get { return GData.protocolManager.ReadShort(SPLC_Name, PCtoPLC, "PC_DestCPUNum"); }
            set { GData.protocolManager.Write(SPLC_Name, PCtoPLC, "PC_DestCPUNum", value); }
        }
        public short PC_DestTrackNum
        {
            get { return GData.protocolManager.ReadShort(SPLC_Name, PCtoPLC, "PC_DestTrackNum"); }
            set { GData.protocolManager.Write(SPLC_Name, PCtoPLC, "PC_DestTrackNum", value); }
        }
        public static string ConvertDestination(short Group, short TrackNumber)
        {
            string Dest = Group.ToString() + TrackNumber.ToString();
            return Dest;
        }

        public eTrayType PC_TrayType
        {
            get { return (eTrayType)GData.protocolManager.ReadShort(SPLC_Name, PCtoPLC, "PC_TrayType"); }
            set { GData.protocolManager.Write(SPLC_Name, PCtoPLC, "PC_TrayType", (ushort)value); }
        }
        public short PC_TrayStackCount
        {
            get { return GData.protocolManager.ReadShort(SPLC_Name, PCtoPLC, "PC_TrayStackCount"); }
            set { GData.protocolManager.Write(SPLC_Name, PCtoPLC, "PC_TrayStackCount", value); }
        }
        public ePolarity PC_Polarity
        {
            get { return (ePolarity)GData.protocolManager.ReadShort(SPLC_Name, PCtoPLC, "PC_Polarity"); }
            set { GData.protocolManager.Write(SPLC_Name, PCtoPLC, "PC_Polarity", (ushort)value); }
        }
        public eProductEmpty PC_ProductEmpty
        {
            get { return (eProductEmpty)GData.protocolManager.ReadShort(SPLC_Name, PCtoPLC, "PC_ProductEmpty"); }
            set { GData.protocolManager.Write(SPLC_Name, PCtoPLC, "PC_ProductEmpty", (ushort)value); }
        }
        public eWinderDirection PC_WinderDirection
        {
            get { return (eWinderDirection)GData.protocolManager.ReadShort(SPLC_Name, PCtoPLC, "PC_WinderDirection"); }
            set { GData.protocolManager.Write(SPLC_Name, PCtoPLC, "PC_WinderDirection", (ushort)value); }
        }
        public short PC_ProductQuantiry
        {
            get { return GData.protocolManager.ReadShort(SPLC_Name, PCtoPLC, "PC_ProductQuantiry"); }
            set { GData.protocolManager.Write(SPLC_Name, PCtoPLC, "PC_ProductQuantiry", value); }
        }
        public short PC_CellPackingLine
        {
            get { return GData.protocolManager.ReadShort(SPLC_Name, PCtoPLC, "PC_CellPackingLine"); }
            set { GData.protocolManager.Write(SPLC_Name, PCtoPLC, "PC_CellPackingLine", value); }
        }
        public eInnerTrayType PC_InnerTrayType //2자리 아스키값
        {
            get { return (eInnerTrayType)GData.protocolManager.ReadShort(SPLC_Name, PCtoPLC, "PC_InnerTrayType"); }

            set
            { 
                GData.protocolManager.Write(SPLC_Name, PCtoPLC, "PC_InnerTrayType", (ushort)value); 
            }
        }
        public ePalletSize PC_PalletSize
        {
            get 
            {
                return (ePalletSize)GData.protocolManager.ReadShort(SPLC_Name, PCtoPLC, "PC_PalletSize");
            }
            set 
            {
                GData.protocolManager.Write(SPLC_Name, PCtoPLC, "PC_PalletSize", (ushort)value);
            }
        }
        public eUnCoatedPart PC_UnCoatedPart
        {
            get { return (eUnCoatedPart)GData.protocolManager.ReadShort(SPLC_Name, PCtoPLC, "PC_UnCoatedPart"); }
            set { GData.protocolManager.Write(SPLC_Name, PCtoPLC, "PC_UnCoatedPart", (ushort)value); }
        }
        public eCoreType PC_CoreType
        {
            get { return (eCoreType)GData.protocolManager.ReadShort(SPLC_Name, PCtoPLC, "PC_CoreType"); }
            set { GData.protocolManager.Write(SPLC_Name, PCtoPLC, "PC_CoreType", (ushort)value); }
        }
        public eProductEnd PC_ProductEnd
        {
            get { return (eProductEnd)GData.protocolManager.ReadShort(SPLC_Name, PCtoPLC, "PC_ProductEnd"); }
            set { GData.protocolManager.Write(SPLC_Name, PCtoPLC, "PC_ProductEnd", (ushort)value); }
        }
        public short PC_ValidationNG
        {
            get { return GData.protocolManager.ReadShort(SPLC_Name, PCtoPLC, "PC_ValidationNG"); }
            set { GData.protocolManager.Write(SPLC_Name, PCtoPLC, "PC_ValidationNG", value); }
        }
        #endregion

        #region NonTracking Read/Write Data Area
        public short PC_TrackPause // 0 => 정상   1 => Pause 요청
        {
            get { return GData.protocolManager.ReadShort(SPLC_Name, PCtoPLC, "PC_TrackPause"); }
            set { GData.protocolManager.Write(SPLC_Name, PCtoPLC, "PC_TrackPause", value); }
        }

        /// <summary>
        /// 1: 입고모드
        /// 2. 출고모드
        /// 3. 양방향모드
        /// </summary>
        public ePortInOutType PC_SCSMode
        {
            get 
            {
                {
                    return (ePortInOutType)GData.protocolManager.ReadShort(SPLC_Name, PCtoPLC, "PC_SCSMode");
                }
            }
            set 
            {
                {
                    GData.protocolManager.Write(SPLC_Name, PCtoPLC, "PC_SCSMode", (ushort)value);
                }
            }
        }

        public short PC_ErrorCode
        {
            get { return GData.protocolManager.ReadShort(SPLC_Name, PCtoPLC, "PC_ErrorCode"); }
            set { GData.protocolManager.Write(SPLC_Name, PCtoPLC, "PC_ErrorCode", value); }
        }

        /// <summary>
        /// 0일경우 : 부저 OFF
        /// 1일경우 : 부저 ON
        /// 9일경우 : Validation NG
        /// </summary>
        public eBuzzerControlMode PC_Buzzer
        {
            get { return (eBuzzerControlMode)GData.protocolManager.ReadShort(SPLC_Name, PCtoPLC, "PC_Buzzer"); }
            set { GData.protocolManager.Write(SPLC_Name, PCtoPLC, "PC_Buzzer", (ushort)value); }
        }

        public short PC_McsSelect
        {
            get { return GData.protocolManager.ReadShort(SPLC_Name, PCtoPLC, "PC_McsSelect"); }
            set { GData.protocolManager.Write(SPLC_Name, PCtoPLC, "PC_McsSelect", value); }
        }

        public bool PC_BCRReadComplete
        {
            get { return GData.protocolManager.ReadBit(SPLC_Name, PCtoPLC, "PC_BCRReadComplete"); }
            set { GData.protocolManager.Write(SPLC_Name, PCtoPLC, "PC_BCRReadComplete", value); }

        }

        public bool PC_TransferPossible
        {
            get { return GData.protocolManager.ReadBit(SPLC_Name, PCtoPLC, "PC_TransferPossible"); }
            set { GData.protocolManager.Write(SPLC_Name, PCtoPLC, "PC_TransferPossible", value); }
        }
        public bool PC_CIMAlarmClear
        {
            get { return GData.protocolManager.ReadBit(SPLC_Name, PCtoPLC, "PC_CIMAlarmClear"); }
            set { GData.protocolManager.Write(SPLC_Name, PCtoPLC, "PC_CIMAlarmClear", value); }
        }
        public bool PC_CIMReportComp
        {
            get { return GData.protocolManager.ReadBit(SPLC_Name, PCtoPLC, "PC_CIMReportComp"); }
            set { GData.protocolManager.Write(SPLC_Name, PCtoPLC, "PC_CIMReportComp", value); }
        }
        //미사용 주석처리
        ///// <summary>
        ///// CIM에서 BCR Read시 PLC에서 기재한 정보와 실물 정보가 다를경우, 자재정보 변경을 위해 Bit On 해준다
        ///// </summary>
        //public bool PC_BCRReadValidNGResponse
        //{
        //    get { return GData.protocolManager.ReadBit(SPLC_Name, PCtoPLC, "PC_BCRReadValidNGResponse"); }
        //    set { GData.protocolManager.Write(SPLC_Name, PCtoPLC, "PC_BCRReadValidNGResponse", value); }
        //}

        /// <summary>
        /// CIM에서 BCR Read 에러 발생시 Bit On해준다.
        /// </summary>
        public bool PC_BCRReadFail
        {
            get { return GData.protocolManager.ReadBit(SPLC_Name, PCtoPLC, "PC_BCRReadFail"); }
            set { GData.protocolManager.Write(SPLC_Name, PCtoPLC, "PC_BCRReadFail", value); }
        }

        public bool PC_PortTypeChange
        {
            get { return GData.protocolManager.ReadBit(SPLC_Name, PCtoPLC, "PC_PortTypeChange"); }
            set { GData.protocolManager.Write(SPLC_Name, PCtoPLC, "PC_PortTypeChange", value); }
        }
        public bool PC_VehicleJobAssign
        {
            get { return GData.protocolManager.ReadBit(SPLC_Name, PCtoPLC, "PC_VehicleJobAssign"); }
            set { GData.protocolManager.Write(SPLC_Name, PCtoPLC, "PC_VehicleJobAssign", value); }
        }
        /// <summary>
        /// Operator_LoadReq, Operator_UnloadReq가 ON된 경우 해당 요청에 맞추어 레포트 진행 후,
        /// 해당 Bit ON(Operator_LoadReq, Operator_UnloadReq가 같이 ON되는 경우는 PLC에서 사전에 방지 필요.)
        /// </summary>

        //public bool PC_CraneBusy
        //{
        //    get { return GData.protocolManager.ReadBit(SPLC_Name, PCtoPLC, "PC_CraneBusy"); }
        //    set { GData.protocolManager.Write(SPLC_Name, PCtoPLC, "PC_CraneBusy", value); }
        //}
        //public bool PC_CraneJobComplete
        //{
        //    get { return GData.protocolManager.ReadBit(SPLC_Name, PCtoPLC, "PC_CraneJobComplete"); }
        //    set { GData.protocolManager.Write(SPLC_Name, PCtoPLC, "PC_CraneJobComplete", value); }
        //}
        #endregion

        #region  NonTracking  Read Only PLC Area
        public short PLC_ErrorCode
        {
            //get { return GData.protocolManager.ReadShort(SPLC_Name, PLCtoPC, "PLC_ErrorCode"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_ErrorCode", value); }
        }

        #region Status Data
        public bool PLC_KeySwitch
        {
            //get { return GData.protocolManager.ReadBit(SPLC_Name, PLCtoPC, "PLC_KeySwitch"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_KeySwitch", value); }
        }
        public bool PLC_CVBusy
        {
            //get { return GData.protocolManager.ReadBit(SPLC_Name, PLCtoPC, "PLC_CVBusy"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_CVBusy", value); }
        }
        public bool PLC_PortAccessMode
        {
            //get { return GData.protocolManager.ReadBit(SPLC_Name, PLCtoPC, "PLC_PortAccessMode"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_PortAccessMode", value); }
        }
        //도착대 도착
        public bool PLC_CarrierInPos
        {
            //get { return GData.protocolManager.ReadBit(SPLC_Name, PLCtoPC, "PLC_CarrierInPos"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_CarrierInPos", value); }
        }
        public bool PLC_SCLoadingHS
        {
            //get { return GData.protocolManager.ReadBit(SPLC_Name, PLCtoPC, "PLC_SCLoadingHS"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_SCLoadingHS", value); }
        }
        public bool PLC_SCUnloadingHS
        {
            //get { return GData.protocolManager.ReadBit(SPLC_Name, PLCtoPC, "PLC_SCUnloadingHS"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_SCUnloadingHS", value); }
        }

        public bool PLC_AGVAccessAble
        {
            //get { return GData.protocolManager.ReadBit(SPLC_Name, PLCtoPC, "PLC_AGVAccessAble"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_AGVAccessAble", value); }
        }

        public bool PLC_LoadRequest
        {
            //get { return GData.protocolManager.ReadBit(SPLC_Name, PLCtoPC, "PLC_LoadRequest"); }
            set
            {
                GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_LoadRequest", value);
                PLC_SCUnloadingHS = value;  //230314 plc 체크비트 추가.
            }
        }
        public bool PLC_LoadComplete
        {
            //get { return GData.protocolManager.ReadBit(SPLC_Name, PLCtoPC, "PLC_LoadComplete"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_LoadComplete", value); }
        }
        public bool PLC_UnloadRequest
        {
            //get { return GData.protocolManager.ReadBit(SPLC_Name, PLCtoPC, "PLC_UnloadRequest"); }
            set 
            { 
                GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_UnloadRequest", value);
                PLC_SCLoadingHS = value;  //230314 plc 체크비트 추가.
            }
        }
        public bool PLC_UnloadComplete
        {
            //get { return GData.protocolManager.ReadBit(SPLC_Name, PLCtoPC, "PLC_UnloadComplete"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_UnloadComplete", value); }
        }

        public bool PLC_NGUnloadRequest
        {
            //get { return GData.protocolManager.ReadBit(SPLC_Name, PLCtoPC, "PLC_NGUnloadRequest"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_NGUnloadRequest", value); }
        }
        public bool PLC_BCRReadRequest
        {
            //get { return GData.protocolManager.ReadBit(SPLC_Name, PLCtoPC, "PLC_BCRReadRequest"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_BCRReadRequest", value); }
        }
        public bool PLC_Timeout
        {
            //get { return GData.protocolManager.ReadBit(SPLC_Name, PLCtoPC, "PLC_Timeout"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_Timeout", value); }
        }

        #endregion

        #region Sensor Data
        /// <summary>
        /// 45 - 0 bit
        /// </summary>
        public bool PLC_CarrierSensor
        {
            get { return GData.protocolManager.ReadBit(SPLC_Name, PLCtoPC, "PLC_CarrierSensor"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_CarrierSensor", value); }
        }
        /// <summary>
        /// 45 - 1 bit
        /// </summary>
        public bool PLC_EmptyBobinSensor
        {
            //get { return GData.protocolManager.ReadBit(SPLC_Name, PLCtoPC, "PLC_EmptyBobinSensor"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_EmptyBobinSensor", value); }
        }
        /// <summary>
        /// 45 - 2 bit
        /// </summary>
        public bool PLC_MaterialSensor
        {
            //get { return GData.protocolManager.ReadBit(SPLC_Name, PLCtoPC, "PLC_MaterialSensor"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_MaterialSensor", value); }
        }
        /// <summary>
        /// 45 - 5 bit
        /// </summary>
        public bool PLC_PortTypeChange
        {
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_PortTypeChange", value); }
        }


        /// <summary>
        /// 45 - 8 bit
        /// </summary>
        public bool PLC_AlarmClear
        {
            //get { return GData.protocolManager.ReadBit(SPLC_Name, PLCtoPC, "PLC_AlarmClear"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_AlarmClear", value); }
        }
        /// <summary>
        /// 45 - 10 bit
        /// </summary>
        public bool PLC_FireShutterOn
        {
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_FireShutterOn",value); }
        }
        /// <summary>
        /// 45 - 12 bit
        /// </summary>
        public bool PLC_DataForceClear
        {
            get { return GData.protocolManager.ReadBit(SPLC_Name, PLCtoPC, "PLC_DataForceClear"); }
        }
        /// <summary>
        /// 45 - 14 bit
        /// </summary>
        public bool PLC_PortSizeShort
        {
            get { return GData.protocolManager.ReadBit(SPLC_Name, PLCtoPC, "PLC_PortSizeShort"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_PortSizeShort", value); }
        }



        //미사용 주석
        ///// <summary>
        ///// 45 - 15 bit
        ///// BCR Read Fail에 의한 Oprator 실물 확인시 자재 정보와 실물정보가 상이하여
        ///// 자재 정보의 업데이트가 필요한경우 해당 비트를 켜준다.
        ///// </summary>
        //public bool PLC_BCRReadValidNGRequest
        //{
        //    get { return GData.protocolManager.ReadBit(SPLC_Name, PLCtoPC, "PLC_BCRReadValidNGRequest"); }
        //}
        #endregion

        public short PLC_CVPosition
        {
            //get { return GData.protocolManager.ReadShort(SPLC_Name, PLCtoPC, "PLC_CVPosition"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_CVPosition", value); }
        }

        /// <summary>
        /// "LOW BYTE (우측 표의 BIT 위치 번호 참고)
        ///  VD MG구간 사용영역, 분채 잔량 사용영역
        ///  16BIT 중 6BIT 사용하여 1/2/3/4/5/6 번 위치의 값을 ON/OFF한다
        ///  
        ///  HIGH BYTE
        ///  (우측 표의 BIT 위치 번호 참고)
        ///  16BIT 중 6BIT 사용하여 1/2/3/4/5/6 번 위치에 잔량센서 감지시  ON/OFF 한다
        ///  감지시: ON(1)
        ///  미감지시: OFF(0)_
        /// </summary>
        //미사용 주석
        //public short PLC_MGSensorValue
        //{
        //    get { return GData.protocolManager.ReadShort(SPLC_Name, PLCtoPC, "PLC_MGSensorValue"); }
        //}
        public bool PLC_MGSensorPos1
        {
            //get { return GData.protocolManager.ReadBit(SPLC_Name, PLCtoPC, "PLC_MGSensorPos1"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_MGSensorPos1", value); }
        }
        public bool PLC_MGSensorPos2
        {
            //get { return GData.protocolManager.ReadBit(SPLC_Name, PLCtoPC, "PLC_MGSensorPos2"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_MGSensorPos2", value); }
        }
        public bool PLC_MGSensorPos3
        {
            //get { return GData.protocolManager.ReadBit(SPLC_Name, PLCtoPC, "PLC_MGSensorPos3"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_MGSensorPos3", value); }
        }
        public bool PLC_MGSensorPos4
        {
            //get { return GData.protocolManager.ReadBit(SPLC_Name, PLCtoPC, "PLC_MGSensorPos4"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_MGSensorPos4", value); }
        }
        public bool PLC_MGSensorPos5
        {
            //get { return GData.protocolManager.ReadBit(SPLC_Name, PLCtoPC, "PLC_MGSensorPos5"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_MGSensorPos5", value); }
        }
        public bool PLC_MGSensorPos6
        {
            //get { return GData.protocolManager.ReadBit(SPLC_Name, PLCtoPC, "PLC_MGSensorPos6"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_MGSensorPos6", value); }
        }

        public bool PLC_MGRemainPos1
        {
            //get { return GData.protocolManager.ReadBit(SPLC_Name, PLCtoPC, "PLC_MGRemainPos1"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_MGRemainPos1", value); }
        }
        public bool PLC_MGRemainPos2
        {
            //get { return GData.protocolManager.ReadBit(SPLC_Name, PLCtoPC, "PLC_MGRemainPos2"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_MGRemainPos2", value); }
        }
        public bool PLC_MGRemainPos3
        {
            //get { return GData.protocolManager.ReadBit(SPLC_Name, PLCtoPC, "PLC_MGRemainPos3"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_MGRemainPos3", value); }
        }
        public bool PLC_MGRemainPos4
        {
            //get { return GData.protocolManager.ReadBit(SPLC_Name, PLCtoPC, "PLC_MGRemainPos4"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_MGRemainPos4", value); }
        }
        public bool PLC_MGRemainPos5
        {
            //get { return GData.protocolManager.ReadBit(SPLC_Name, PLCtoPC, "PLC_MGRemainPos5"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_MGRemainPos5", value); }
        }
        public bool PLC_MGRemainPos6
        {
            //get { return GData.protocolManager.ReadBit(SPLC_Name, PLCtoPC, "PLC_MGRemainPos6"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_MGRemainPos6", value); }
        }
        public ePortInOutType PLC_PortType
        {
            set
            {
                {
                    GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_PortType", (ushort)value);
                }
            }
        }
        public ePortType PLC_SCSMode
        {
            //get { return (ePortType)GData.protocolManager.ReadShort(SPLC_Name, PLCtoPC, "PLC_SCSMode"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_SCSMode", value); }
        }
        #endregion

        #endregion

    }
}
