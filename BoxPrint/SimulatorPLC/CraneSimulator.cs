using PLCProtocol.Base;
using PLCProtocol.DataClass;
using BoxPrint.Log;
using System;
using System.ComponentModel;
using System.Threading;

namespace BoxPrint.SimulatorPLC
{
    public class CraneSimulator : BaseSimulator
    {
        private bool FirstRM;
        private int ActionDelay = 50;
        private bool OneRackPause = false;

        #region 시뮬레이션 변수들
        public bool SimulJobDone = false;

        eCraneCommand Command;
        public short CommandID;
        public int TargetBank;
        public int TargetBay;
        public int TargetLevel;
        public int TargetWorkPlace;
        //enumCraneTarget TargetType;
        string CarrierIDFork1;
        string CarrierIDFork2;
        ePalletSize CarrierPalletSize;


        private int Offset_X = 2000; //Bay 간격
        private int Offset_Z = 3000; //Level 간격 
        private int Position_Fork = 1000; //포킹 포지션 
        private int Position_ZUp = 1000;

        //private int SimulX_AccVector = 10; //Drive 가속
        //private int SimulZ_AccVector = 10; //Z 축 가속
        //private int SimulF_AccVector = 10; //Fork 축 가속 

        private int SimulX_SPDVector = 100; //Drive 속도
        private int SimulZ_SPDVector = 100; //Z 축 속도
        private int SimulF_SPDVector = 10; //Fork 축 속도

        private int _ForkAxisPosition;
        public int ForkAxisPosition
        {
            get
            {
                return _ForkAxisPosition;
            }
            set
            {
                _ForkAxisPosition = value;
                PLC_RM_FPosition = value;
            }
        }
        private int _XAxisPosition;
        public int XAxisPosition
        {
            get
            {
                return _XAxisPosition;
            }
            set
            {
                _XAxisPosition = value;
                PLC_RM_XPosition = value;
                PLC_RM_Current_Bay = (short)Robot_Simul_BAY;
            }
        }



        private int _ZAxisPosition;
        public int ZAxisPosition
        {
            get
            {
                return _ZAxisPosition;
            }
            set
            {
                _ZAxisPosition = value;
                PLC_RM_ZPosition = value;
                PLC_RM_Current_Level = (short)Robot_Simul_LEVEL;
            }
        }

        private bool _RMFrontDoubleStorage;
        public bool RMFrontDoubleStorage
        {
            get
            {
                return _RMFrontDoubleStorage;
            }
            set
            {
                _RMFrontDoubleStorage = value;
                PLC_RM_Front_DoubleStorage = value;
            }
        }

        private bool _RMRearDoubleStorage;
        public bool RMRearDoubleStorage
        {
            get
            {
                return _RMRearDoubleStorage;
            }
            set
            {
                _RMRearDoubleStorage = value;
                PLC_RM_Rear_DoubleStorage = value;
            }
        }

        public int Robot_Simul_BANK
        {
            get
            {
                //230718 조숭진
                //return 1;
                return GlobalData.Current.FrontBankNum;
            }
        }
        public int Robot_Simul_BAY
        {
            get
            {
                return XAxisPosition / Offset_X;
            }
        }
        public int Robot_Simul_LEVEL
        {
            get
            {
                return ZAxisPosition / Offset_Z;
            }
        }

        private eCraneArmState _CraneArmState = eCraneArmState.Center;

        private eCraneArmState CraneArmState
        {
            get => _CraneArmState;
            set
            {
                _CraneArmState = value;
                PLC_Fork1_Extend = value;
            }
        }

        bool SimulFireFlag = false;

        public bool EmptyRetriveTestMode
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
        public bool DoubleStorageTestMode
        {
            get
            {
                return _DebugTestModes[1];
            }
            set
            {
                _DebugTestModes[1] = value;
                OnPropertyChanged(new PropertyChangedEventArgs("DebugTestModes"));
            }
        }

        public bool PortIFErrorTestMode
        {
            get
            {
                return _DebugTestModes[2];
            }
            set
            {
                _DebugTestModes[2] = value;
                OnPropertyChanged(new PropertyChangedEventArgs("DebugTestModes"));
            }
        }

        public bool ForkFireTest
        {
            get
            {
                return _DebugTestModes[3];
            }
            set
            {
                //241030 HoN 화재 관련 추가 수정        //화재 발생은 무조건 발생하지 않도록 한다.
                if (PC_FireOccurred)
                {
                    PLC_FireState = true;
                }

                _DebugTestModes[3] = value;
                OnPropertyChanged(new PropertyChangedEventArgs("DebugTestModes"));
            }
        }

        eCraneUIState CraneSimulState = eCraneUIState.ONLINE;

        public double MoveSpeed;

        public double JogSpeed;

        #endregion

 
        public CraneSimulator(int RMNumber)
        {
            PLCModuleType = "CRANE";
            FirstRM = RMNumber == 1;
        }
        public override void SetPLCAddress(int plcNum, int PLCReadOffset, int PLCWriteOffset)
        {
            PLCtoPC = ProtocolHelper.GetPLCItem(eAreaType.PLCtoPC, "CRANE", (short)plcNum, (ushort)PLCReadOffset);
            PCtoPLC = ProtocolHelper.GetPLCItem(eAreaType.PCtoPLC, "CRANE", (short)plcNum, (ushort)PLCWriteOffset);
        }
        public void SetInitPosition(int bank, int Bay, int Level)
        {
            ForkAxisPosition = 0;
            XAxisPosition = Bay * Offset_X;
            ZAxisPosition = Level * Offset_Z;
        }

        public bool IsFireState()
        {
            return PLC_FireState;
        }
        public bool GetOneRackPauseMode()
        {
            return OneRackPause;
        }
        public void SetOneRackPauseMode(bool Pause)
        {
            OneRackPause = Pause;
            if (OneRackPause)
            {
                PLC_CraneCommandState = eCraneCommand.REMOTE_HOME;
                PLC_SCMODE = eCraneSCMode.ONE_RACK_PAUSE;
            }
            else
            {
                PLC_SCMODE = eCraneSCMode.AUTO_RUN;
                PLC_CraneCommandState = eCraneCommand.NONE;
            }
        }
        public void SetOnFire(bool OnOff)
        {
            //Carrier 있을때만 화재 발생가능.
            if (PLC_CarrierExistFork1)
            {
                PLC_FireState = OnOff;
                Console.WriteLine("{0} Simul FireOn", this.SPLC_Name);
            }
            else
            {
                if (!OnOff) //캐리어 없을시 Off만 가능.
                {
                    PLC_FireState = false;
                    Console.WriteLine("{0} Simul FireOff", this.SPLC_Name);
                }
            }
        }
        public void RecvCraneCommandData()
        {
            Command = PC_CraneCommand;
            CommandID = PC_CommandNumber_FORK1;
            CarrierIDFork1 = PC_CarrierID_FORK1;
            CarrierIDFork2 = PC_CarrierID_FORK2;
            TargetBank = PC_DestBank_FORK1;
            TargetBay = PC_DestBay_FORK1;
            TargetLevel = PC_DestLevel_FORK1;
            TargetWorkPlace = PC_DestWorkPlace_FORK1;
            CarrierPalletSize = PC_PalletSize;

            //받은 커맨드를 PLC에 쓴다.
            PLC_CarrierID_FORK1 = CarrierIDFork1;
            PLC_CraneCommandState = Command;
            PLC_CommandNumber_FORK1 = CommandID;
            PLC_DestBank_FORK1 = (short)TargetBank;
            PLC_DestBay_FORK1 = (short)TargetBay;
            PLC_DestLevel_FORK1 = (short)TargetLevel;
            PLC_DestWorkPlace_FORK1 = (short)TargetWorkPlace;
            PLC_PalletSize = CarrierPalletSize;
        }

        public override void PLCAutoCycleRun()
        {
            PLC_RM_Current_Bank = (short)Robot_Simul_BANK;
            PLC_CraneJobState = eCraneJobState.JobComplete_Fork1;
            PLC_CarrierExistFork1 = false;
            PLC_SCMODE = eCraneSCMode.AUTO_RUN;
            PLC_CraneActionState = eCraneSCState.IDLE;
            PLC_ActiveState = eCraneActiveState.ACTIVE;
            PLC_OnlineMode = eCraneOnlineMode.REMOTE;

            if (GlobalData.Current.mRMManager[FirstRM ? 1 : 2].InSlotCarrier != null) //시뮬레이션 데이터 보정
            {
                PLC_CarrierExistFork1 = true;
                //240808 HoN 시뮬레이션 관련 수정
                //-> 재실행 시 화물감지는 업데이트하나 CarrierID는 업데이트 되지않음
                PLC_CarrierID_FORK1 = GlobalData.Current.mRMManager[FirstRM ? 1 : 2].InSlotCarrier.CarrierID;
            }
            //PLC_CarrierExistFork1 = false;
            while (!PLCExit)
            {
                if (!PLCRunState)
                {
                    Thread.Sleep(PLCCycleTime);
                    continue;
                }

                #region Remote Control Check
                if (PC_EMG_STOP)
                {
                    CraneSimulState = eCraneUIState.ERROR;
                    PLC_CraneActionState = eCraneSCState.ERROR;
                    PLC_ActiveState = eCraneActiveState.INACTIVE;
                    CurrentAlarmCode = (short)(1037);
                }

                //일단 주석
                //if(PC_ActiveCommand)
                //{
                //    PLCPause = false;
                //}
                //if(PC_PauseCommand)
                //{
                //    PLCPause = true;
                //}
                if (SelfAlarmClearReq)
                {
                    SelfAlarmClearReq = false;
                    PLC_CraneActionState = eCraneSCState.IDLE;
                    DateTime dt = DateTime.Now;
                    while (!IsTimeOut(dt,3))
                    {
                        if(FirstRM)
                        {
                            if( PLCSimulatorManager.Instance.BS.PC_RM1ReportComp)
                            {
                                break;
                            }
                        }
                        else
                        {
                            if (PLCSimulatorManager.Instance.BS.PC_RM2ReportComp)
                            {
                                break;
                            }
                            break;
                        }
                    }
                    CurrentAlarmCode = 0;
                    CurrentWarninCode = 0;
                    PLC_ErrorCode = CurrentAlarmCode;
                    CraneSimulState = eCraneUIState.ONLINE;
                    PLC_ActiveState = eCraneActiveState.ACTIVE;
                    PLC_CraneActionState = eCraneSCState.IDLE;
                    PLC_CraneCommandState = eCraneCommand.NONE;
                    PLC_CommandAck = 0;
                }

                if (PC_ErrorReset)
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "CraneSimulator : {0}  PC_ErrorReset On Check!", this.SPLC_Name);
                    CraneSimulState = eCraneUIState.ONLINE;
                    PLC_ActiveState = eCraneActiveState.ACTIVE;
                    PLC_CraneActionState = eCraneSCState.IDLE;
                    PLC_CraneCommandState = eCraneCommand.NONE;
                    CurrentAlarmCode = 0;
                    CurrentWarninCode = 0;
                    PLC_ErrorCode = CurrentAlarmCode;
                    PLC_CommandAck = 0;
                    Thread.Sleep(100);
                }
                else
                {
                    //SCS에서 내려주는 알람 체크
                    short pcACode = PC_AlarmCode;
                    if (pcACode > 0 && CurrentAlarmCode == 0)
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "CraneSimulator : {0}  PC_AlarmCode Set ", this.SPLC_Name);
                        CraneSimulState = eCraneUIState.ERROR;
                        PLC_CraneActionState = eCraneSCState.ERROR;
                        PLC_ActiveState = eCraneActiveState.INACTIVE;
                        CurrentAlarmCode = pcACode;
                    }

                    if (CurrentAlarmCode > 0)
                    {
                        PLC_ErrorCode = CurrentAlarmCode;
                        CraneSimulState = eCraneUIState.ERROR;
                        PLC_CraneActionState = eCraneSCState.ERROR;
                        PLC_ActiveState = eCraneActiveState.INACTIVE;
                        Thread.Sleep(PLCCycleTime);
                        continue;
                    }
                }

  

                if (PC_RemoveFork1Data)
                {
                    PLC_CarrierID_FORK1 = "";
                }
                if (PC_RemoveFork2Data)
                {
                    PLC_CarrierID_FORK2 = "";
                }
                if (PC_RemoveAllForkData)
                {
                    PLC_CarrierID_FORK1 = "";
                    PLC_CarrierID_FORK2 = "";
                }
                if (PC_EmptyRetrievalReset)
                {

                }
                if (PC_FireOccurred)
                {

                }
                if (PC_FireReset)
                {
                    PLC_FireCommand = false;
                    PLC_FireState = false;
                }

                #endregion

               

                //SCS Command Check
                if (PLCPause)
                {
                    continue;
                }

                switch (CraneSimulState)
                {
                    case eCraneUIState.ONLINE:             //온라인 대기 상태
                        //SCS 커맨드를 수신
                        if (PC_CommandWriteComplete == 1)
                        {
                            RecvCraneCommandData();
                            //PLC Ack
                            PLC_CommandAck = 1;
                            PLC_CraneJobState = eCraneJobState.Busy;
                            PLC_CraneActionState = eCraneSCState.BUSY;
                            if (Command == eCraneCommand.PICKUP)
                            {
                                CraneSimulState = eCraneUIState.GETTING;
                            }
                            else if (Command == eCraneCommand.UNLOAD)
                            {
                                CraneSimulState = eCraneUIState.PUTTING;
                            }
                            else if (Command == eCraneCommand.MOVE)
                            {
                                CraneSimulState = eCraneUIState.MOVING;
                            }
                            else if (Command == eCraneCommand.LOCAL_HOME || Command == eCraneCommand.REMOTE_HOME)
                            {
                                CraneSimulState = eCraneUIState.HOMING;
                            }
                        }
                        break;
                    case eCraneUIState.OFFLINE:           //오프라인 상태
                        break;
                    case eCraneUIState.HOMING:            //홈 복귀중
                        SimulHomeAction();
                        PLC_CraneJobState = eCraneJobState.JobComplete_Fork1;
                        while (true)
                        {
                            if (PC_CommandWriteComplete == 0)
                            {
                                PLC_CommandAck = 0;
                                CraneSimulState = eCraneUIState.ONLINE;
                                PLC_CraneCommandState = eCraneCommand.NONE;
                                PLC_CraneActionState = eCraneSCState.IDLE;
                                break;
                            }
                            Thread.Sleep(PLCCycleTime);
                        }
                        break;
                    case eCraneUIState.PUTTING:            //PUT 동작중
                    case eCraneUIState.GETTING:            //GET 동작중
                    case eCraneUIState.MOVING:             //이동 동작중
                    case eCraneUIState.FIRE_UNLOADING:     //화재출고 동작중
                        bool MotionResult = SimulMotionRun();
                        if (MotionResult)
                        {
                            Thread.Sleep(500);
                            PLC_CraneJobState = eCraneJobState.JobComplete_Fork1;
                            while (true)
                            {
                                if (PC_CommandWriteComplete == 0)
                                {
                                    PLC_CommandAck = 0;
                                    
                                    if (CraneSimulState == eCraneUIState.PUTTING && ForkFireTest)
                                    {
                                        ForkFireTest = false;
                                        PLC_FireState = false;
                                    }

                                    CraneSimulState = eCraneUIState.ONLINE;
                                    PLC_CraneCommandState = eCraneCommand.NONE;
                                    PLC_CraneActionState = eCraneSCState.IDLE;
                                    break;
                                }
                                Thread.Sleep(PLCCycleTime);
                            }
                        }
                        else
                        {
                            CraneSimulState = eCraneUIState.ERROR;
                        }
                        break;
                    case eCraneUIState.ERROR:             //에러상태
                        PLC_CommandAck = 0;
                        break;
                }

                //동작처리 구현 추가.
                Thread.Sleep(PLCCycleTime);
            }
        }



        #region 시뮬레이션 동작

        private bool SimulMotionRun()
        {
            
            switch (Command)
            {
                case eCraneCommand.PICKUP:
                    SimulJobDone = SimulGetAction();
                    break;
                case eCraneCommand.UNLOAD:
                    SimulJobDone = SimulPutAction();
                    break;
                case eCraneCommand.MOVE:
                    SimulJobDone = SimulMoveAction();
                    break;
            }
            return SimulJobDone;

        }
        private bool SimulGetAction()
        {
            PLC_CraneJobState = eCraneJobState.Busy;

            if (!PLC_FireState && ForkFireTest)
            {
                PLC_FireState = true;
            }

            bool MoveSuccess = SimulMoveAction(); //이동동작부터 한다.
            if (!MoveSuccess)
            {
                return false;
            }
            if (SimulFireFlag)
            {
                CraneSimulState = eCraneUIState.FIRE_UNLOADING;
            }
            else
            {
                CraneSimulState = eCraneUIState.GETTING;
            }
            if (EmptyRetriveTestMode)
            {
                EmptyRetriveTestMode = false;
                CurrentAlarmCode = short.Parse(GlobalData.SOURCE_EMPTY_ALARM_CODE);
                LogManager.WriteConsoleLog(eLogLevel.Info, "Crane : {0} 공출고 테스트 모드이므로 강제로 알람 발생합니다.", SPLC_Name);
                //SimulEmptyRetriveON = true;
                CraneSimulState = eCraneUIState.ERROR;
                return false;
            }

            if (PortIFErrorTestMode)
            {
                PortIFErrorTestMode = false;
                CurrentAlarmCode = short.Parse(GlobalData.PORT_IF_ALARM_CODE);
                LogManager.WriteConsoleLog(eLogLevel.Info, "Crane : {0} 포트  인터페이스 에러 테스트 모드이므로 강제로 알람 발생합니다.", SPLC_Name);
                CraneSimulState = eCraneUIState.ERROR;
                return false;
            }

            //Fork Extand
            if (!ForkMove(TargetBank == 1, true))
            {
                return false;
            }
            Thread.Sleep(100);

            //ZAxis UP
            ZAxisPosition = ZAxisPosition + Position_ZUp;


            //Fork Retract
            if(!ForkMove(TargetBank == 1, false))
            {
                return false;
            }
            Thread.Sleep(100);

            //ZAxis Down
            ZAxisPosition = ZAxisPosition - Position_ZUp;

            //241030 HoN 화재 관련 추가 수정        //화재 발생은 무조건 발생하지 않도록 한다.
            //if (PC_FireOccurred)
            //{
            //    PLC_FireState = true;
            //}

            PLC_CarrierExistFork1 = true;

            return true;
        }
        private bool SimulPutAction()
        {
            PLC_CraneJobState = eCraneJobState.Busy;
            bool MoveSuccess = SimulMoveAction(); //이동동작부터 한다.
            if (!MoveSuccess)
            {
                return false;
            }
            if (SimulFireFlag)
            {
                CraneSimulState = eCraneUIState.FIRE_UNLOADING;
            }
            else
            {
                CraneSimulState = eCraneUIState.PUTTING;
            }
            if (DoubleStorageTestMode)
            {
                DoubleStorageTestMode = false;
                CurrentAlarmCode = short.Parse(GlobalData.DOUBLE_STORAGE_ALARM_CODE);
                LogManager.WriteConsoleLog(eLogLevel.Info, "Crane : {0} 더블 스토리지 테스트 모드이므로 강제로 알람 발생합니다.", SPLC_Name);
                //SimulDoubleStorageON = true;
                
                if (TargetBank == 1)
                    RMFrontDoubleStorage = true;
                else
                    RMRearDoubleStorage = true;

                CraneSimulState = eCraneUIState.ERROR;
                return false;
            }
            //ZAxis UP
            ZAxisPosition = ZAxisPosition + Position_ZUp;

            //Fork Extand
            if(!ForkMove(TargetBank == 1, true))
            {
                return false;
            }
            Thread.Sleep(100);

            //ZAxis Down
            ZAxisPosition = ZAxisPosition - Position_ZUp;

            //Fork Retract
            if(!ForkMove(TargetBank == 1, false))
            {
                return false;
            }
            Thread.Sleep(100);

            PLC_CarrierExistFork1 = false;

            return true;
        }
        private bool SimulMoveAction()
        {
            PLC_CraneJobState = eCraneJobState.Busy;
            if (SimulFireFlag)
            {
                CraneSimulState = eCraneUIState.FIRE_UNLOADING;
            }
            else
            {
                CraneSimulState = eCraneUIState.MOVING;
            }
            CraneSimulator RM_Another = null;
            if (FirstRM)
            {
                RM_Another = PLCSimulatorManager.Instance.C2;
            }
            else
            {
                RM_Another = PLCSimulatorManager.Instance.C1;
            }
            //목표 축 위치를 미리 계산
            int TargetXPos = TargetBay * Offset_X;
            int TargetZPos = TargetLevel * Offset_Z;
            LogManager.WriteConsoleLog(eLogLevel.Info, "{0} Now Simul Moving Start!", SPLC_Name);
            while (true)
            {
                if (!PLC_FireState && ForkFireTest)
                {
                    PLC_FireState = true;
                }

                if (PC_EMG_STOP)
                {
                    CraneSimulState = eCraneUIState.ERROR;
                    return false;
                }

                //현재 위치 PLC 업데이트
                //240808 HoN 시뮬레이션 관련 수정
                //-> 시뮬 진행 시 끝단 포트 작업하면 0점 혹은 끝점으로 이동함. 시뮬상에서는 Bank가 업데이트 되지않아 태그로 검색이 되지않기에 타겟으로 업데이트
                //PLC_RM_Current_Bank = (short)Robot_Simul_BANK;
                PLC_RM_Current_Bank = (short)TargetBank;
                PLC_RM_Current_Bay = (short)Robot_Simul_BAY;
                PLC_RM_Current_Level = (short)Robot_Simul_LEVEL;
                if (TargetBay == Robot_Simul_BAY && TargetLevel == Robot_Simul_LEVEL)
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "{0} Now Simul Moving End!", SPLC_Name);
                    return true;
                }
                //Z축 동작
                if (Math.Abs(TargetZPos - ZAxisPosition) > SimulZ_SPDVector)
                {
                    if (TargetZPos > ZAxisPosition)
                    {
                        ZAxisPosition += SimulZ_SPDVector;
                    }
                    else
                    {
                        ZAxisPosition -= SimulZ_SPDVector;
                    }

                }
                else
                {
                    ZAxisPosition = TargetZPos;
                }

                if (TargetBay != Robot_Simul_BAY)
                {
                    if (TargetBay > Robot_Simul_BAY) //+ 정방향
                    {
                        if (FirstRM)
                        {
                            if (RM_Another == null || RM_Another.Robot_Simul_BAY - 3 > Robot_Simul_BAY)//일정 거리 여유가 있어야 이동 가능 
                            {
                                if (Math.Abs(TargetXPos - XAxisPosition) > SimulX_SPDVector)
                                {
                                    XAxisPosition += SimulX_SPDVector;
                                }
                                else
                                {
                                    XAxisPosition = TargetXPos;
                                }
                            }
                            else
                            {
                                //이동불가 대기
                            }
                        }
                        else //2번째 RM 은 +이동 항상 가능
                        {
                            if (Math.Abs(TargetXPos - XAxisPosition) > SimulX_SPDVector)
                            {
                                XAxisPosition += SimulX_SPDVector;
                            }
                            else
                            {
                                XAxisPosition = TargetXPos;
                            }
                        }
                    }
                    else   //- 역방향
                    {
                        if (FirstRM) //1번째 RM 은 -이동 항상 가능
                        {
                            if (Math.Abs(TargetXPos - XAxisPosition) > SimulX_SPDVector)
                            {
                                XAxisPosition -= SimulX_SPDVector;
                            }
                            else
                            {
                                XAxisPosition = TargetXPos;
                            }
                        }
                        else
                        {
                            if (RM_Another == null || RM_Another.Robot_Simul_BAY + 3 < Robot_Simul_BAY)//일정 거리 여유가 있어야 이동 가능 
                            {
                                if (Math.Abs(TargetXPos - XAxisPosition) > SimulX_SPDVector)
                                {
                                    XAxisPosition -= SimulX_SPDVector;
                                }
                                else
                                {
                                    XAxisPosition = TargetXPos;
                                }
                            }
                            else
                            {
                                //이동불가 대기
                            }
                        }
                    }
                }

                Thread.Sleep(ActionDelay);
            }

        }

        private bool SimulHomeAction()
        {
            CraneSimulState = eCraneUIState.HOMING;

            //Fork Retract
            while (ForkAxisPosition != 0)
            {
                if (ForkAxisPosition > 0)
                {
                    ForkAxisPosition -= SimulF_SPDVector;
                }
                else
                {
                    ForkAxisPosition += SimulF_SPDVector;
                }
                if (Math.Abs(ForkAxisPosition) <= 100)
                {
                    ForkAxisPosition = 0;
                    break;
                }
                Thread.Sleep(ActionDelay);
            }

            CraneArmState = eCraneArmState.Center;
            Thread.Sleep(300);

            CraneSimulState = eCraneUIState.ONLINE;
            return true;
        }

        private bool ForkMove(bool Front, bool Forking)
        {
            if (Forking) //Fork 뻗는동작
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "{0} Now Simul Forking Start!", SPLC_Name);
                CraneArmState = Front ? eCraneArmState.ExtendFront : eCraneArmState.ExtendRear;
                while (Math.Abs(ForkAxisPosition) < Position_Fork)
                {
                    if(PC_EMG_STOP)
                    {
                        CraneSimulState = eCraneUIState.ERROR;
                        return false;
                    }
                    if (Front)
                    {
                        ForkAxisPosition += SimulF_SPDVector;
                    }
                    else
                    {
                        ForkAxisPosition -= SimulF_SPDVector;
                    }
                    Thread.Sleep(ActionDelay);
                }
                if (Front)
                {
                    ForkAxisPosition = Position_Fork;
                }
                else
                {
                    ForkAxisPosition = -Position_Fork;
                }
                LogManager.WriteConsoleLog(eLogLevel.Info, "{0} Now Simul Forking End!", SPLC_Name);
                return true;
            }
            else //Fork 접기
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "{0} Now Simul Retracting Start!", SPLC_Name);
                CraneArmState = eCraneArmState.Center;
                while (ForkAxisPosition != 0)
                {
                    if (PC_EMG_STOP)
                    {
                        CraneSimulState = eCraneUIState.ERROR;
                        return false;
                    }
                    if (ForkAxisPosition > 0)
                    {
                        ForkAxisPosition -= SimulF_SPDVector;
                    }
                    else
                    {
                        ForkAxisPosition += SimulF_SPDVector;
                    }
                    if (Math.Abs(ForkAxisPosition) <= 100)
                    {
                        ForkAxisPosition = 0;
                    }
                    Thread.Sleep(ActionDelay);
                }
                LogManager.WriteConsoleLog(eLogLevel.Info, "{0} Now Simul Retracting End!", SPLC_Name);
                return true;
            }

        }
        #endregion


        #region PLCInterface

        #region RECV DATA Read Area
        public string PLC_CarrierID_FORK1
        {
            //240808 HoN 시뮬레이션 관련 수정
            get { return GData.protocolManager.ReadString(SPLC_Name, PLCtoPC, "PLC_CarrierID_FORK1"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_CarrierID_FORK1", value); }
        }
        public string PLC_CarrierID_FORK2
        {
            //get { return GData.protocolManager.ReadString(SPLC_Name, PLCtoPC, "PLC_CarrierID_FORK2"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_CarrierID_FORK2", value); }
        }

        public ePalletSize PLC_PalletSize
        {
            //get { return (ePalletSize)GData.protocolManager.ReadShort(SPLC_Name, PLCtoPC, "PLC_PalletSize"); }
            set 
            {
                GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_PalletSize", value);
            }
        }

        public eCraneJobState PLC_CraneJobState
        {
            //get { return (eCraneJobState)GData.protocolManager.ReadShort(SPLC_Name, PLCtoPC, "PLC_CraneJobState"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_CraneJobState", value); }
        }

        public eCraneCommand PLC_CraneCommandState
        {
            //get { return (eCraneCommand)GData.protocolManager.ReadShort(SPLC_Name, PLCtoPC, "PLC_CraneCommandState"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_CraneCommandState", value); }
        }

        public short PLC_CommandNumber_FORK1
        {
            //get { return GData.protocolManager.ReadShort(SPLC_Name, PLCtoPC, "PLC_CommandNumber_FORK1"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_CommandNumber_FORK1", value); }
        }

        public short PLC_SourceBank_FORK1
        {
            //get { return GData.protocolManager.ReadShort(SPLC_Name, PLCtoPC, "PLC_SourceBank_FORK1"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_SourceBank_FORK1", value); }
        }
        public short PLC_SourceBay_FORK1
        {
            //get { return GData.protocolManager.ReadShort(SPLC_Name, PLCtoPC, "PLC_SourceBay_FORK1"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_SourceBank_FORK1", value); }
        }
        public short PLC_SourceLevel_FORK1
        {
            //get { return GData.protocolManager.ReadShort(SPLC_Name, PLCtoPC, "PLC_SourceLevel_FORK1"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_SourceLevel_FORK1", value); }

        }
        public short PLC_SourceWorkPlace_FORK1
        {
            //get { return GData.protocolManager.ReadShort(SPLC_Name, PLCtoPC, "PLC_SourceWorkPlace_FORK1"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_SourceWorkPlace_FORK1", value); }

        }
        public short PLC_DestBank_FORK1
        {
            //get { return GData.protocolManager.ReadShort(SPLC_Name, PLCtoPC, "PLC_DestBank_FORK1"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_DestBank_FORK1", value); }

        }
        public short PLC_DestBay_FORK1
        {
            //get { return GData.protocolManager.ReadShort(SPLC_Name, PLCtoPC, "PLC_DestBay_FORK1"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_DestBay_FORK1", value); }
        }
        public short PLC_DestLevel_FORK1
        {
            //get { return GData.protocolManager.ReadShort(SPLC_Name, PLCtoPC, "PLC_DestLevel_FORK1"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_DestLevel_FORK1", value); }
        }
        public short PLC_DestWorkPlace_FORK1
        {
            //get { return GData.protocolManager.ReadShort(SPLC_Name, PLCtoPC, "PLC_DestWorkPlace_FORK1"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_DestWorkPlace_FORK1", value); }
        }

        public short PLC_CommandUseFork
        {
            //get { return GData.protocolManager.ReadShort(SPLC_Name, PLCtoPC, "PLC_CommandUseFork"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_CommandUseFork", value); }
        }
        public short PLC_CommandNumber_FORK2
        {
            //get { return GData.protocolManager.ReadShort(SPLC_Name, PLCtoPC, "PLC_CommandNumber_FORK2"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_CommandNumber_FORK2", value); }
        }
        public short PLC_SourceBank_FORK2
        {
            //get { return GData.protocolManager.ReadShort(SPLC_Name, PLCtoPC, "PLC_SourceBank_FORK2"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_SourceBank_FORK2", value); }
        }
        public short PLC_SourceBay_FORK2
        {
            //get { return GData.protocolManager.ReadShort(SPLC_Name, PLCtoPC, "PLC_SourceBay_FORK2"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_SourceBay_FORK2", value); }
        }
        public short PLC_SourceLevel_FORK2
        {
            //get { return GData.protocolManager.ReadShort(SPLC_Name, PLCtoPC, "PLC_SourceLevel_FORK2"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_SourceLevel_FORK2", value); }
        }
        public short PLC_SourceWorkPlace_FORK2
        {
            //get { return GData.protocolManager.ReadShort(SPLC_Name, PLCtoPC, "PLC_SourceWorkPlace_FORK2"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_SourceWorkPlace_FORK2", value); }
        }
        public short PLC_DestBank_FORK2
        {
            //get { return GData.protocolManager.ReadShort(SPLC_Name, PLCtoPC, "PLC_DestBank_FORK2"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_DestBank_FORK2", value); }
        }
        public short PLC_DestBay_FORK2
        {
            //get { return GData.protocolManager.ReadShort(SPLC_Name, PLCtoPC, "PLC_DestBay_FORK2"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_DestBay_FORK2", value); }
        }
        public short PLC_DestLevel_FORK2
        {
            //get { return GData.protocolManager.ReadShort(SPLC_Name, PLCtoPC, "PLC_DestLevel_FORK2"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_DestLevel_FORK2", value); }
        }
        public short PLC_DestWorkPlace_FORK2
        {
            //get { return GData.protocolManager.ReadShort(SPLC_Name, PLCtoPC, "PLC_DestWorkPlace_FORK2"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_DestWorkPlace_FORK2", value); }
        }

        public short PLC_CommandAck
        {
            //get { return GData.protocolManager.ReadShort(SPLC_Name, PLCtoPC, "PLC_CommandAck"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_CommandAck", value); }
        }
        public eCraneOnlineMode PLC_OnlineMode
        {
            //get { return (eCraneOnlineMode)GData.protocolManager.ReadShort(SPLC_Name, PLCtoPC, "PLC_OnlineMode"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_OnlineMode", value); }
        }

        public eCraneSCMode PLC_SCMODE
        {
            //get { return (eCraneSCMode)GData.protocolManager.ReadShort(SPLC_Name, PLCtoPC, "PLC_SCMODE"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_SCMODE", value); }
        }

        public eCraneSCState PLC_CraneActionState
        {
            //get { return (eCraneActionState)GData.protocolManager.ReadShort(SPLC_Name, PLCtoPC, "PLC_CraneActionState"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_CraneActionState", value); }
        }

        public bool PLC_CarrierExistFork1
        {
            get { return GData.protocolManager.ReadBit(SPLC_Name, PLCtoPC, "PLC_CarrierExistFork1"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_CarrierExistFork1", value); }
        }
        public bool PLC_CarrierExistFork2
        {
            //get { return GData.protocolManager.ReadBit(SPLC_Name, PLCtoPC, "PLC_CarrierExistFork2"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_CarrierExistFork2", value); }
        }
        public eCraneActiveState PLC_ActiveState
        {
            //get { return GData.protocolManager.ReadShort(SPLC_Name, PLCtoPC, "PLC_ActiveState"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_ActiveState", value); }
        }
        public bool PLC_FireCommand
        {
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_FireCommand", value); }
        }

        public bool PLC_FireState
        {

            get { return GData.protocolManager.ReadBit(SPLC_Name, PLCtoPC, "PLC_FireState"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_FireState", value); }
        }
        public short PLC_WarningCode
        {
            //get { return GData.protocolManager.ReadShort(SPLC_Name, PLCtoPC, "PLC_WarningCode"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_WarningCode", value); }
        }
        public short PLC_ErrorCode
        {
            //get { return GData.protocolManager.ReadShort(SPLC_Name, PLCtoPC, "PLC_ErrorCode"); }
            set
            {
                GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_ErrorCode", value);
            }

        }

        public eCraneArmState PLC_Fork1_Extend
        {
            //get { return (eCraneArmState)GData.protocolManager.ReadShort(SPLC_Name, PLCtoPC, "PLC_Fork1_Extend"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_Fork1_Extend", value); }
        }
        public eCraneArmState PLC_Fork2_Extend
        {
            //get { return (eCraneArmState)GData.protocolManager.ReadShort(SPLC_Name, PLCtoPC, "PLC_Fork2_Extend"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_Fork2_Extend", value); }
        }


        public bool PLC_Fork1_ErrorRecommandReady
        {
            //get { return GData.protocolManager.ReadBit(SPLC_Name, PLCtoPC, "PLC_Fork1_ErrorRecommandReady"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_Fork1_ErrorRecommandReady", value); }
        }
        public bool PLC_Fork1_ErrorDoubleStorage
        {
            //get { return GData.protocolManager.ReadBit(SPLC_Name, PLCtoPC, "PLC_Fork1_ErrorDoubleStorage"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_Fork1_ErrorDoubleStorage", value); }
        }
        public bool PLC_Fork1_EmptyRetrieve
        {
            //get { return GData.protocolManager.ReadBit(SPLC_Name, PLCtoPC, "PLC_Fork1_EmptyRetrieve"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_Fork1_EmptyRetrieve", value); }
        }

        public bool PLC_Fork2_ErrorRecommandReady
        {
            //get { return GData.protocolManager.ReadBit(SPLC_Name, PLCtoPC, "PLC_Fork2_ErrorRecommandReady"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_Fork2_ErrorRecommandReady", value); }
        }
        public bool PLC_Fork2_ErrorDoubleStorage
        {
            //get { return GData.protocolManager.ReadBit(SPLC_Name, PLCtoPC, "PLC_Fork2_ErrorDoubleStorage"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_Fork2_ErrorDoubleStorage", value); }
        }
        public bool PLC_Fork2_EmptyRetrieve
        {
            //get { return GData.protocolManager.ReadBit(SPLC_Name, PLCtoPC, "PLC_Fork2_EmptyRetrieve"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_Fork2_EmptyRetrieve", value); }
        }

        #region CV Forking Bit
        public bool PLC_CV1Forking
        {
            //get { return GData.protocolManager.ReadBit(SPLC_Name, PLCtoPC, "PLC_CV1Forking"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_CV1Forking", value); }
        }
        public bool PLC_CV2Forking
        {
            //get { return GData.protocolManager.ReadBit(SPLC_Name, PLCtoPC, "PLC_CV2Forking"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_CV2Forking", value); }
        }
        public bool PLC_CV3Forking
        {
            //get { return GData.protocolManager.ReadBit(SPLC_Name, PLCtoPC, "PLC_CV3Forking"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_CV3Forking", value); }
        }
        public bool PLC_CV4Forking
        {
            //get { return GData.protocolManager.ReadBit(SPLC_Name, PLCtoPC, "PLC_CV4Forking"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_CV4Forking", value); }
        }
        public bool PLC_CV5Forking
        {
            //get { return GData.protocolManager.ReadBit(SPLC_Name, PLCtoPC, "PLC_CV5Forking"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_CV5Forking", value); }
        }
        public bool PLC_CV6Forking
        {
            //get { return GData.protocolManager.ReadBit(SPLC_Name, PLCtoPC, "PLC_CV6Forking"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_CV6Forking", value); }
        }
        public bool PLC_CV7Forking
        {
            //get { return GData.protocolManager.ReadBit(SPLC_Name, PLCtoPC, "PLC_CV7Forking"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_CV7Forking", value); }
        }
        public bool PLC_CV8Forking
        {
            //get { return GData.protocolManager.ReadBit(SPLC_Name, PLCtoPC, "PLC_CV8Forking"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_CV8Forking", value); }
        }
        public bool PLC_CV9Forking
        {
            //get { return GData.protocolManager.ReadBit(SPLC_Name, PLCtoPC, "PLC_CV9Forking"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_CV9Forking", value); }
        }
        public bool PLC_CV10Forking
        {
            //get { return GData.protocolManager.ReadBit(SPLC_Name, PLCtoPC, "PLC_CV10Forking"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_CV10Forking", value); }
        }
        public bool PLC_CV11Forking
        {
            //get { return GData.protocolManager.ReadBit(SPLC_Name, PLCtoPC, "PLC_CV11Forking"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_CV11Forking", value); }
        }
        public bool PLC_CV12Forking
        {
            //get { return GData.protocolManager.ReadBit(SPLC_Name, PLCtoPC, "PLC_CV12Forking"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_CV12Forking", value); }
        }
        public bool PLC_CV13Forking
        {
            //get { return GData.protocolManager.ReadBit(SPLC_Name, PLCtoPC, "PLC_CV13Forking"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_CV13Forking", value); }
        }
        public bool PLC_CV14Forking
        {
            //get { return GData.protocolManager.ReadBit(SPLC_Name, PLCtoPC, "PLC_CV14Forking"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_CV14Forking", value); }
        }
        public bool PLC_CV15Forking
        {
            //get { return GData.protocolManager.ReadBit(SPLC_Name, PLCtoPC, "PLC_CV15Forking"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_CV15Forking", value); }
        }
        public bool PLC_CV16Forking
        {
            //get { return GData.protocolManager.ReadBit(SPLC_Name, PLCtoPC, "PLC_CV16Forking"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_CV16Forking", value); }
        }
        #endregion

        public int PLC_RM_XPosition
        {
            set
            {
                GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_RM_XPosition", value);
            }
        }
        public int PLC_RM_ZPosition
        {
            set
            {
                GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_RM_ZPosition", value);
            }
        }
        public int PLC_RM_FPosition
        {
            set
            {
                GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_RM_FPosition", value);
            }
        }

        //public short PLC_RM_Low_XPosition //주행축 하위 2Byte
        //{
        //    set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_RM_Low_XPosition", value); }
        //}
        //public short PLC_RM_High_XPosition //주행축 상위 2Byte
        //{
        //    set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_RM_High_XPosition", value); }
        //}
        //public short PLC_RM_Low_ZPosition //승강축 하위 2Byte
        //{
        //    set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_RM_Low_ZPosition", value); }
        //}
        //public short PLC_RM_High_ZPosition //승강축 상위 2Byte
        //{
        //    set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_RM_High_ZPosition", value); }
        //}
        //public short PLC_RM_Low_FPosition //포크축 하위 2Byte
        //{
        //    set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_RM_Low_FPosition", value); }
        //}
        //public short PLC_RM_High_FPosition //포크축 상위 2Byte
        //{
        //    set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_RM_High_FPosition", value); }
        //}

        public bool PLC_FireJobCancelAble
        {
            //get { return GData.protocolManager.ReadBit(SPLC_Name, PLCtoPC, "PLC_FireJobCancelAble"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_FireJobCancelAble", value); }
        }
        public bool PLC_FireJobCancelBlock
        {
            //get { return GData.protocolManager.ReadBit(SPLC_Name, PLCtoPC, "PLC_FireJobCancelBlock"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_FireJobCancelBlock", value); }
        }
        public short PLC_RM_Current_Bank
        {
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_RM_Current_Bank", value); }
        }
        public short PLC_RM_Current_Bay
        {
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_RM_Current_Bay", value); }
        }
        public short PLC_RM_Current_Level
        {
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_RM_Current_Level", value); }
        }

        public bool PLC_RM_Front_DoubleStorage
        {
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_RM_Front_DoubleStorage", value); }
        }

        public bool PLC_RM_Rear_DoubleStorage
        {
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_RM_Rear_DoubleStorage", value); }
        }
        #endregion


        #region SEND DATA Write Data Area
        //사용 예시) 확장은 필요없을것으로 생각됨. 특정 모듈에 기재해야하는 비트 / 워드 존재 시, 확장된 모듈에 추가를 하면됨.
        public string PC_CarrierID_FORK1
        {
            //set
            //{
            //    if (value.Length > 40) //40자리 제한
            //    {
            //        GData.protocolManager.Write(SPLC_Name, PCtoPLC, "PC_CarrierID_FORK1", value.Substring(0, 40));
            //    }
            //    else
            //    {
            //        GData.protocolManager.Write(SPLC_Name, PCtoPLC, "PC_CarrierID_FORK1", value);
            //    }
            //}
            get { return GData.protocolManager.ReadString(SPLC_Name, PCtoPLC, "PC_CarrierID_FORK1"); }
        }
        public string PC_CarrierID_FORK2
        {
            //set
            //{
            //    if (value.Length > 40) //40자리 제한
            //    {
            //        GData.protocolManager.Write(SPLC_Name, PCtoPLC, "PC_CarrierID_FORK2", value.Substring(0, 40));
            //    }
            //    else
            //    {
            //        GData.protocolManager.Write(SPLC_Name, PCtoPLC, "PC_CarrierID_FORK2", value);
            //    }
            //}
            get { return GData.protocolManager.ReadString(SPLC_Name, PCtoPLC, "PC_CarrierID_FORK2"); }
        }

        #region Crane Command Parameter

        public eCraneCommand PC_CraneCommand
        {
            //set { GData.protocolManager.Write(SPLC_Name, PCtoPLC, "PC_CraneCommand", (ushort)value); }
            get { return (eCraneCommand)GData.protocolManager.ReadShort(SPLC_Name, PCtoPLC, "PC_CraneCommand"); }
        }
        public short PC_CommandNumber_FORK1
        {
            //set { GData.protocolManager.Write(SPLC_Name, PCtoPLC, "PC_CommandNumber_FORK1", value); }
            get { return GData.protocolManager.ReadShort(SPLC_Name, PCtoPLC, "PC_CommandNumber_FORK1"); }
        }


        public short PC_SourceBank_FORK1
        {
            //set { GData.protocolManager.Write(SPLC_Name, PCtoPLC, "PC_SourceBank_FORK1", value); }
            get { return GData.protocolManager.ReadShort(SPLC_Name, PCtoPLC, "PC_SourceBank_FORK1"); }
        }
        public short PC_SourceBay_FORK1
        {
            //set { GData.protocolManager.Write(SPLC_Name, PCtoPLC, "PC_SourceBay_FORK1", value); }
            get { return GData.protocolManager.ReadShort(SPLC_Name, PCtoPLC, "PC_SourceBay_FORK1"); }

        }
        public short PC_SourceLevel_FORK1
        {
            //set { GData.protocolManager.Write(SPLC_Name, PCtoPLC, "PC_SourceLevel_FORK1", value); }
            get { return GData.protocolManager.ReadShort(SPLC_Name, PCtoPLC, "PC_SourceLevel_FORK1"); }
        }
        public short PC_SourceWorkPlace_FORK1
        {
            //set { GData.protocolManager.Write(SPLC_Name, PCtoPLC, "PC_SourceWorkPlace_FORK1", value); }
            get { return GData.protocolManager.ReadShort(SPLC_Name, PCtoPLC, "PC_SourceWorkPlace_FORK1"); }
        }
        public short PC_DestBank_FORK1
        {
            //set { GData.protocolManager.Write(SPLC_Name, PCtoPLC, "PC_DestBank_FORK1", value); }
            get { return GData.protocolManager.ReadShort(SPLC_Name, PCtoPLC, "PC_DestBank_FORK1"); }
        }
        public short PC_DestBay_FORK1
        {
            //set { GData.protocolManager.Write(SPLC_Name, PCtoPLC, "PC_DestBay_FORK1", value); }
            get { return GData.protocolManager.ReadShort(SPLC_Name, PCtoPLC, "PC_DestBay_FORK1"); }
        }
        public short PC_DestLevel_FORK1
        {
            //set { GData.protocolManager.Write(SPLC_Name, PCtoPLC, "PC_DestLevel_FORK1", value); }
            get { return GData.protocolManager.ReadShort(SPLC_Name, PCtoPLC, "PC_DestLevel_FORK1"); }
        }
        public short PC_DestWorkPlace_FORK1
        {
            //set { GData.protocolManager.Write(SPLC_Name, PCtoPLC, "PC_DestWorkPlace_FORK1", value); }
            get { return GData.protocolManager.ReadShort(SPLC_Name, PCtoPLC, "PC_DestWorkPlace_FORK1"); }
        }

        public short PC_CommandUseFork
        {
            //set { GData.protocolManager.Write(SPLC_Name, PCtoPLC, "PC_CommandUseFork", value); }
            get { return GData.protocolManager.ReadShort(SPLC_Name, PCtoPLC, "PC_CommandUseFork"); }
        }
        public short PC_CommandNumber_FORK2
        {
            //set { GData.protocolManager.Write(SPLC_Name, PCtoPLC, "PC_CommandNumber_FORK2", value); }
            get { return GData.protocolManager.ReadShort(SPLC_Name, PCtoPLC, "PC_CommandNumber_FORK2"); }
        }
        public short PC_SourceBank_FORK2
        {
            //set { GData.protocolManager.Write(SPLC_Name, PCtoPLC, "PC_SourceBank_FORK2", value); }
            get { return GData.protocolManager.ReadShort(SPLC_Name, PCtoPLC, "PC_SourceBank_FORK2"); }
        }
        public short PC_SourceBay_FORK2
        {
            //set { GData.protocolManager.Write(SPLC_Name, PCtoPLC, "PC_SourceBay_FORK2", value); }
            get { return GData.protocolManager.ReadShort(SPLC_Name, PCtoPLC, "PC_SourceBay_FORK2"); }
        }
        public short PC_SourceLevel_FORK2
        {
            //set { GData.protocolManager.Write(SPLC_Name, PCtoPLC, "PC_SourceLevel_FORK2", value); }
            get { return GData.protocolManager.ReadShort(SPLC_Name, PCtoPLC, "PC_SourceLevel_FORK2"); }
        }
        public short PC_SourceWorkPlace_FORK2
        {
            //set { GData.protocolManager.Write(SPLC_Name, PCtoPLC, "PC_SourceWorkPlace_FORK2", value); }
            get { return GData.protocolManager.ReadShort(SPLC_Name, PCtoPLC, "PC_SourceWorkPlace_FORK2"); }
        }
        public short PC_DestBank_FORK2
        {
            //set { GData.protocolManager.Write(SPLC_Name, PCtoPLC, "PC_DestBank_FORK2", value); }
            get { return GData.protocolManager.ReadShort(SPLC_Name, PCtoPLC, "PC_DestBank_FORK2"); }
        }
        public short PC_DestBay_FORK2
        {
            //set { GData.protocolManager.Write(SPLC_Name, PCtoPLC, "PC_DestBay_FORK2", value); }
            get { return GData.protocolManager.ReadShort(SPLC_Name, PCtoPLC, "PC_DestBay_FORK2"); }
        }
        public short PC_DestLevel_FORK2
        {
            //set { GData.protocolManager.Write(SPLC_Name, PCtoPLC, "PC_DestLevel_FORK2", value); }
            get { return GData.protocolManager.ReadShort(SPLC_Name, PCtoPLC, "PC_DestLevel_FORK2"); }
        }
        public short PC_DestWorkPlace_FORK2
        {
            //set { GData.protocolManager.Write(SPLC_Name, PCtoPLC, "PC_DestWorkPlace_FORK2", value); }
            get { return GData.protocolManager.ReadShort(SPLC_Name, PCtoPLC, "PC_DestWorkPlace_FORK2"); }
        }

        public short PC_CommandWriteComplete
        {
            //set { GData.protocolManager.Write(SPLC_Name, PCtoPLC, "PC_CommandWriteComplete", value); }
            get { return GData.protocolManager.ReadShort(SPLC_Name, PCtoPLC, "PC_CommandWriteComplete"); }
        }
        public short PC_CarrierStability //0 저강성(저속동작) 1 고강성(고속가능)
        {
            //set { GData.protocolManager.Write(SPLC_Name, PCtoPLC, "PC_CarrierStability", value); }
            get { return GData.protocolManager.ReadShort(SPLC_Name, PCtoPLC, "PC_CarrierStability"); }
        }

        public ePalletSize PC_PalletSize
        {
            get
            {
                return (ePalletSize)GData.protocolManager.ReadShort(SPLC_Name, PCtoPLC, "PC_PalletSize");
            }
        }

        #endregion

        #region REMOTE CONTROL
        public bool PC_EMG_STOP
        {
            //set { GData.protocolManager.Write(SPLC_Name, PCtoPLC, "PC_EMG_STOP", value); }
            get { return GData.protocolManager.ReadBit(SPLC_Name, PCtoPLC, "PC_EMG_STOP"); }
        }
        public bool PC_ActiveCommand
        {
            //set { GData.protocolManager.Write(SPLC_Name, PCtoPLC, "PC_ActiveCommand", value); }
            get { return GData.protocolManager.ReadBit(SPLC_Name, PCtoPLC, "PC_ActiveCommand"); }
        }
        public bool PC_PauseCommand
        {
            //set { GData.protocolManager.Write(SPLC_Name, PCtoPLC, "PC_PauseCommand", value); }
            get { return GData.protocolManager.ReadBit(SPLC_Name, PCtoPLC, "PC_PauseCommand"); }
        }
        public bool PC_ErrorReset
        {
            //set { GData.protocolManager.Write(SPLC_Name, PCtoPLC, "PC_ErrorReset", value); }
            get { return GData.protocolManager.ReadBit(SPLC_Name, PCtoPLC, "PC_ErrorReset"); }
        }

        public bool PC_RemoveFork1Data
        {
            //set { GData.protocolManager.Write(SPLC_Name, PCtoPLC, "PC_RemoveFork1Data", value); }
            get { return GData.protocolManager.ReadBit(SPLC_Name, PCtoPLC, "PC_RemoveFork1Data"); }
        }
        public bool PC_RemoveFork2Data
        {
            //set { GData.protocolManager.Write(SPLC_Name, PCtoPLC, "PC_RemoveFork2Data", value); }
            get { return GData.protocolManager.ReadBit(SPLC_Name, PCtoPLC, "PC_RemoveFork2Data"); }
        }
        public bool PC_RemoveAllForkData
        {
            //set { GData.protocolManager.Write(SPLC_Name, PCtoPLC, "PC_RemoveAllForkData", value); }
            get { return GData.protocolManager.ReadBit(SPLC_Name, PCtoPLC, "PC_RemoveAllForkData"); }
        }

        public bool PC_EmptyRetrievalReset
        {
            //set { GData.protocolManager.Write(SPLC_Name, PCtoPLC, "PC_EmptyRetrievalReset", value); }
            get { return GData.protocolManager.ReadBit(SPLC_Name, PCtoPLC, "PC_EmptyRetrievalReset"); }
        }
        public bool PC_FireOccurred
        {
            //set { GData.protocolManager.Write(SPLC_Name, PCtoPLC, "PC_FireOccurred", value); }
            get { return GData.protocolManager.ReadBit(SPLC_Name, PCtoPLC, "PC_FireOccurred"); }
        }
        public bool PC_FireReset
        {
            //set { GData.protocolManager.Write(SPLC_Name, PCtoPLC, "PC_FireReset", value); }
            get { return GData.protocolManager.ReadBit(SPLC_Name, PCtoPLC, "PC_FireReset"); }
        }


        #endregion

        public short PC_AlarmCode
        {
            //set { GData.protocolManager.Write(SPLC_Name, PCtoPLC, "PC_AlarmCode", value); }
            get { return GData.protocolManager.ReadShort(SPLC_Name, PCtoPLC, "PC_AlarmCode"); }

        }
        public short PC_OneRackComp
        {
            //set { GData.protocolManager.Write(SPLC_Name, PCtoPLC, "PC_OneRackComp", value); }
            get { return GData.protocolManager.ReadShort(SPLC_Name, PCtoPLC, "PC_OneRackComp"); }

        }
        
        #endregion

        #endregion
    }
}
