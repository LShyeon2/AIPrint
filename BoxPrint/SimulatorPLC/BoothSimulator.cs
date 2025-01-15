using PLCProtocol.Base;
using PLCProtocol.DataClass;
using System;
using System.Threading;

namespace BoxPrint.SimulatorPLC
{
    public class BoothSimulator : BaseSimulator
    {
        private DateTime PLCHeartBeatToggleTime = DateTime.Now;
        private Double PLCHeartBeatToggleDelay = 1; //1sec
        private bool HeartBeat = true;

        private int PLCTimeOut = 2;
        DateTime PauseDT = DateTime.MinValue;
        DateTime ResumeDT = DateTime.MinValue;
        DateTime TimeSyncDT = DateTime.MinValue;
        private bool DoorOpened = false;
        public BoothSimulator()
        {
            PLCModuleType = "BOOTH";
        }
        public void SetDoorState(bool Open)
        {
            DoorOpened = Open;
            PLC_DoorOpenState = Open;
        }
        private void ToggleHeartbeat()
        {
            if (IsTimeOut(PLCHeartBeatToggleTime, PLCHeartBeatToggleDelay))
            {
                PLCHeartBeatToggleTime = DateTime.Now;
                HeartBeat = !HeartBeat; //Toggle
                PLC_HeartBeat = HeartBeat ? (short)1 : (short)0;
            }
        }
        public override void SetPLCAddress(int plcNum, int PLCReadOffset, int PLCWriteOffset)
        {
            PLCtoPC = ProtocolHelper.GetPLCItem(eAreaType.PLCtoPC, "BOOTH", (short)plcNum, (ushort)PLCReadOffset);
            PCtoPLC = ProtocolHelper.GetPLCItem(eAreaType.PCtoPLC, "BOOTH", (short)plcNum, (ushort)PLCWriteOffset);
        }
        public override void PLCAutoCycleRun()
        {
            PLC_AutoState = true;
            while (!PLCExit)
            {
                try
                {
                    if (!PLCRunState)
                    {
                        Thread.Sleep(PLCCycleTime);
                        continue;
                    }
                    ToggleHeartbeat(); //하트 비트 처리 

                    //Pause 체크
                    if (PC_PauseReq)
                    {
                        Thread.Sleep(100);
                        PLC_PauseResponse = true;
                        if (PauseDT == DateTime.MinValue)
                        {
                            PauseDT = DateTime.Now;
                        }
                        if (IsTimeOut(PauseDT, PLCTimeOut))
                        {
                            PLC_PauseResponse = false;
                            PauseDT = DateTime.MinValue;
                        }
                        PLC_PauseState = true;
                        PLC_AutoState = false;
                    }
                    else
                    {
                        PLC_PauseResponse = false;
                        PauseDT = DateTime.MinValue;
                    }

                    //Resume 체크
                    if (PC_ResumeReq)
                    {
                        Thread.Sleep(100);
                        PLC_ResumeResponse = true;
                        if (ResumeDT == DateTime.MinValue)
                        {
                            ResumeDT = DateTime.Now;
                        }
                        if (IsTimeOut(ResumeDT, PLCTimeOut))
                        {
                            PLC_ResumeResponse = false;
                            ResumeDT = DateTime.MinValue;
                        }
                        PLC_PauseState = false;
                        PLC_AutoState = true;
                    }
                    else
                    {
                        PLC_ResumeResponse = false;
                        ResumeDT = DateTime.MinValue;
                    }

                    //TimeSync 체크
                    if (PC_TimeSyncReq)
                    {
                        PLC_TimeSyncResponse = true;
                        if (TimeSyncDT == DateTime.MinValue)
                        {
                            TimeSyncDT = DateTime.Now;
                        }
                        if (IsTimeOut(TimeSyncDT, PLCTimeOut))
                        {
                            PLC_TimeSyncResponse = false;
                            TimeSyncDT = DateTime.MinValue;
                        }
                    }
                    else
                    {
                        PLC_TimeSyncResponse = false;
                        TimeSyncDT = DateTime.MinValue;
                    }
                }
                catch (Exception)
                {
                    ;
                }
                Thread.Sleep(PLCCycleTime);
            }

        }

        #region PLCInterface PC->PLC Write Area
        public short PC_InterlockRelease
        {
            // set { GData.protocolManager.Write(SPLC_Name, PCtoPLC, "PC_InterlockRelease", value); }
            get { return GData.protocolManager.ReadShort(SPLC_Name, PCtoPLC, "PC_InterlockRelease"); }
        }
        public short PC_HeartBeat
        {
            //set { GData.protocolManager.Write(SPLC_Name, PCtoPLC, "PC_HeartBeat", value); }
            get { return GData.protocolManager.ReadShort(SPLC_Name, PCtoPLC, "PC_HeartBeat"); }
        }

        #region 시간 동기화 변수

        public short PC_TimeSync_YY
        {
            //set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_TimeSync_YY", value); }
            get { return GData.protocolManager.ReadShort(SPLC_Name, PCtoPLC, "PC_TimeSync_YY"); }
        }
        public short PC_TimeSync_MM
        {
            //set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_TimeSync_MM", value); }
            get { return GData.protocolManager.ReadShort(SPLC_Name, PCtoPLC, "PC_TimeSync_YY"); }
        }
        public short PC_TimeSync_DD
        {
            //set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_TimeSync_DD", value); }
            get { return GData.protocolManager.ReadShort(SPLC_Name, PCtoPLC, "PC_TimeSync_YY"); }
        }
        public short PC_TimeSync_hh
        {
            //set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_TimeSync_hh", value); }
            get { return GData.protocolManager.ReadShort(SPLC_Name, PCtoPLC, "PC_TimeSync_YY"); }
        }
        public short PC_TimeSync_mm
        {
            //set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_TimeSync_mm", value); }
            get { return GData.protocolManager.ReadShort(SPLC_Name, PCtoPLC, "PC_TimeSync_YY"); }
        }
        public short PC_TimeSync_ss
        {
            //set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_TimeSync_ss", value); }
            get { return GData.protocolManager.ReadShort(SPLC_Name, PCtoPLC, "PC_TimeSync_YY"); }
        }
        #endregion

        public short PC_SCSVersion1
        {
            //set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_SCSVersion1", value); }
            get { return GData.protocolManager.ReadShort(SPLC_Name, PCtoPLC, "PC_TimeSync_YY"); }
        }
        public short PC_SCSVersion2
        {
            //set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_SCSVersion2", value); }
            get { return GData.protocolManager.ReadShort(SPLC_Name, PCtoPLC, "PC_TimeSync_YY"); }
        }

        #region HP 타워 램프 제어
        public eTowerLampMode PC_TowerLamp_HPRed
        {
            //set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_TowerLamp_HPRed", (ushort)value); }
            get { return (eTowerLampMode)GData.protocolManager.ReadShort(SPLC_Name, PCtoPLC, "PC_TowerLamp_HPRed"); }
        }
        public eTowerLampMode PC_TowerLamp_HPYellow
        {
            //set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_TowerLamp_HPYellow", (ushort)value); }
            get { return (eTowerLampMode)GData.protocolManager.ReadShort(SPLC_Name, PCtoPLC, "PC_TowerLamp_HPYellow"); }
        }
        public eTowerLampMode PC_TowerLamp_HPGreen
        {
            //set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_TowerLamp_HPGreen", (ushort)value); }
            get { return (eTowerLampMode)GData.protocolManager.ReadShort(SPLC_Name, PCtoPLC, "PC_TowerLamp_HPGreen"); }
        }
        public eTowerLampMode PC_TowerLamp_HPBlue
        {
            //set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_TowerLamp_HPBlue", (ushort)value); }
            get { return (eTowerLampMode)GData.protocolManager.ReadShort(SPLC_Name, PCtoPLC, "PC_TowerLamp_HPGreen"); }
        }
        public eTowerLampMode PC_TowerLamp_HPWhite
        {
            //set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_TowerLamp_HPWhite", (ushort)value); }
            get { return (eTowerLampMode)GData.protocolManager.ReadShort(SPLC_Name, PCtoPLC, "PC_TowerLamp_HPWhite"); }
        }
        #endregion

        #region OP 타워 램프 제어
        public eTowerLampMode PC_TowerLamp_OPRed
        {
            //set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_TowerLamp_OPRed", (ushort)value); }
            get { return (eTowerLampMode)GData.protocolManager.ReadShort(SPLC_Name, PCtoPLC, "PC_TowerLamp_OPRed"); }
        }
        public eTowerLampMode PC_TowerLamp_OPYellow
        {
            //set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_TowerLamp_OPYellow", (ushort)value); }
            get { return (eTowerLampMode)GData.protocolManager.ReadShort(SPLC_Name, PCtoPLC, "PC_TowerLamp_OPYellow"); }
        }
        public eTowerLampMode PC_TowerLamp_OPGreen
        {
            //set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_TowerLamp_OPGreen", (ushort)value); }
            get { return (eTowerLampMode)GData.protocolManager.ReadShort(SPLC_Name, PCtoPLC, "PC_TowerLamp_OPGreen"); }
        }
        public eTowerLampMode PC_TowerLamp_OPBlue
        {
            //set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_TowerLamp_OPBlue", (ushort)value); }
            get { return (eTowerLampMode)GData.protocolManager.ReadShort(SPLC_Name, PCtoPLC, "PC_TowerLamp_OPBlue"); }
        }
        public eTowerLampMode PC_TowerLamp_OPWhite
        {
            //set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_TowerLamp_OPWhite", (ushort)value); }
            get { return (eTowerLampMode)GData.protocolManager.ReadShort(SPLC_Name, PCtoPLC, "PC_TowerLamp_OPWhite"); }
        }
        #endregion

        public eBuzzerSoundType PC_BuzzerHP
        {
            //set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_BuzzerHP", (ushort)value); }
            get { return (eBuzzerSoundType)GData.protocolManager.ReadShort(SPLC_Name, PCtoPLC, "PC_BuzzerHP"); }
        }
        public eBuzzerSoundType PC_BuzzerOP
        {
            //set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_BuzzerOP", (ushort)value); }
            get { return (eBuzzerSoundType)GData.protocolManager.ReadShort(SPLC_Name, PCtoPLC, "PC_BuzzerHP"); }
        }

        public short PC_RM1_Availability
        {
            //set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_Crane1_Availability", value); }
            get { return GData.protocolManager.ReadShort(SPLC_Name, PCtoPLC, "PC_RM1_Availability"); }
        }
        public short PC_RM2_Availability
        {
            //set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_Crane2_Availability", value); }
            get { return GData.protocolManager.ReadShort(SPLC_Name, PCtoPLC, "PC_RM2_Availability"); }
        }
        public short PC_SystemStart
        {
            //set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_SystemStart", value); }
            get { return GData.protocolManager.ReadShort(SPLC_Name, PCtoPLC, "PC_SystemStart"); }
        }
        public bool PC_PauseReq
        {
            //set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_PauseReq", value); }
            get { return GData.protocolManager.ReadBit(SPLC_Name, PCtoPLC, "PC_PauseReq"); }
        }
        public bool PC_ResumeReq
        {
            //set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_ResumeReq", value); }
            get { return GData.protocolManager.ReadBit(SPLC_Name, PCtoPLC, "PC_ResumeReq"); }
        }
        public bool PC_TimeSyncReq
        {
            //set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_TimeSyncReq", value); }
            get { return GData.protocolManager.ReadBit(SPLC_Name, PCtoPLC, "PC_TimeSyncReq"); }
        }
        public bool PC_RM1ReportComp
        {
            //set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_RM1ReportComp", value); }
            get { return GData.protocolManager.ReadBit(SPLC_Name, PCtoPLC, "PC_RM1ReportComp"); }
        }
        public bool PC_RM2ReportComp
        {
            //set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_RM2ReportComp", value); }
            get { return GData.protocolManager.ReadBit(SPLC_Name, PCtoPLC, "PC_RM2ReportComp"); }
        }
        #endregion

        #region  PLCInterface PLC->PC Read Area
        public short PLC_HeartBeat
        {
            //get { return GData.protocolManager.ReadBit(SPLC_Name, PLCtoPC, "PLC_HeartBeat"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_HeartBeat", value); }
        }
        public bool PLC_HPDoorOpenState
        {
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_HPDoorOpenState", value); }
        }
        public bool PLC_OPDoorOpenState
        {
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_OPDoorOpenState", value); }
        }
        public bool PLC_Crane1_Availability
        {
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_Crane1_Availability", value); }
        }
        public bool PLC_Crane2_Availability
        {
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_Crane2_Availability", value); }
        }
        public short PLC_FireShutterOperation
        {
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_FireShutterOperation", value); }
        }


        public bool PLC_PauseResponse
        {
            //get { return GData.protocolManager.ReadBit(SPLC_Name, PLCtoPC, "PLC_PauseResponse"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_PauseResponse", value); }
        }
        public bool PLC_ResumeResponse
        {
            //get { return GData.protocolManager.ReadBit(SPLC_Name, PLCtoPC, "PLC_ResumeResponse"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_ResumeResponse", value); }
        }
        public bool PLC_TimeSyncResponse
        {
            //get { return GData.protocolManager.ReadBit(SPLC_Name, PLCtoPC, "PLC_TimeSyncResponse"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_TimeSyncResponse", value); }
        }
        public bool PLC_PauseState
        {
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_PauseState", value); }
        }
        public bool PLC_AutoState
        {
            get { return GData.protocolManager.ReadBit(SPLC_Name, PLCtoPC, "PLC_AutoState"); }
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_AutoState", value); }
        }
        public bool PLC_DoorOpenState
        {
            set { GData.protocolManager.Write(SPLC_Name, PLCtoPC, "PLC_DoorOpenState", value); }
        }
        #endregion
    }
}
