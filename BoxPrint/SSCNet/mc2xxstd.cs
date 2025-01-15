using System;
using System.Runtime.InteropServices;

namespace mc2xxstd
{
    /*----------------------------------------------------------------------*/
    /* extra point table                                                    */
    /*----------------------------------------------------------------------*/
    [StructLayout(LayoutKind.Sequential)]
    public struct PNT_DATA_EX
    {
        /* 0000h */
        public int position;
        public uint speed;
        public ushort actime;
        public ushort dctime;
        public ushort dwell;
        public ushort subcmd;

        /* 0010h */
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[] oas_num;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[] reserve1;

        public byte s_curve;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 11)]
        public byte[] reserve2;

        /* 0020h */

        public void Initialize()
        {
            oas_num = new byte[2];
            reserve1 = new byte[2];
            reserve2 = new byte[11];
        }
    }

    /*----------------------------------------------------------------------*/
    /* other axis status table                                              */
    /*----------------------------------------------------------------------*/
    [StructLayout(LayoutKind.Sequential)]
    public struct OAS_DATA
    {
        /* 0000h */
        public uint opt_own;
        public uint opt_observ;
        public int data_own;
        public int data_observ;

        /* 0010h */
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] reserve1;

        public ulong st_axbit;

        /* 0020h */
        public ushort st_pnt_s;
        public ushort st_pnt_e;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        public byte[] reserve2;

        /* 0030h */
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
        public byte[] reserve3;

        public byte dout_ctrl;
        public byte dout_num;
        public ushort dout_ctrlbit;
        public ushort dout_data;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public byte[] reserve4;

        /* 0068h */

        public void Initialize()
        {
            reserve1 = new byte[8];
            reserve2 = new byte[12];
            reserve3 = new byte[40];
            reserve4 = new byte[10];
        }
    }

    /*----------------------------------------------------------------------*/
    /* press table                                                          */
    /*----------------------------------------------------------------------*/
    [StructLayout(LayoutKind.Sequential)]
    public struct PRESS_DATA
    {
        /* 0000h */
        public int switch_position;
        public int position_limit;
        public int speed_limit;
        public ushort target_torque;
        public ushort continue_time;

        /* 0010h */
        public ushort torque_settle_width;
        public ushort torque_settle_time;
        public ushort actime;
        public ushort dctime;
        public ushort condition;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public byte[] reserve1;

        /* 0020h */

        public void Initialize()
        {
            reserve1 = new byte[6];
        }
    }

    /*----------------------------------------------------------------------*/
    /* sampling error table                                                 */
    /*----------------------------------------------------------------------*/
    [StructLayout(LayoutKind.Sequential)]
    public struct SMP_ACC
    {
        /* 0000h */
        public ushort num;
        public ushort reserve1;
        public uint data;

        /* 0008h */
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SMP_ERR
    {
        /* 0000h */
        public ulong err_ax;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] reserve1;

        public uint err_dat;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] reserve2;

        public uint err_bit;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] reserve3;

        /* 0020h */

        public void Initialize()
        {
            reserve1 = new byte[8];
            reserve2 = new byte[4];
            reserve3 = new byte[4];
        }
    }

    /*----------------------------------------------------------------------*/
    /* sampling table                                                       */
    /*----------------------------------------------------------------------*/
    [StructLayout(LayoutKind.Sequential)]
    public struct SMP_DATA
    {
        /* 0000h */
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public int[] smpdata;

        /* 0080h */
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public ushort[] smpbit;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[] smpdmy;

        /* 0084h */

        public void Initialize()
        {
            smpdata = new int[32];
            smpbit = new ushort[1];
            smpdmy = new byte[2];
        }
    }

    /*----------------------------------------------------------------------*/
    /* log table                                                            */
    /*----------------------------------------------------------------------*/
    [StructLayout(LayoutKind.Sequential)]
    public struct LOG_DATA
    {
        /* 0000h */
        public short axnum;
        public short eventcode;
        public int eventtime;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public short[] eventdata;

        /* 0010h */

        public void Initialize()
        {
            eventdata = new short[4];
        }
    }

    /*----------------------------------------------------------------------*/
    /* alarm history table                                                  */
    /*----------------------------------------------------------------------*/
    [StructLayout(LayoutKind.Sequential)]
    public struct ALH_DATA
    {
        /* 0000h */
        public long system_time;
        public uint free_run_cnt;
        public byte ctrl_cycle;
        public byte event_code;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public sbyte[] reserve1;

        /* 0010h */
        public byte sscnet_type;
        public byte ctrl_mode;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public sbyte[] reserve2;

        public ushort axnum;
        public ushort alarm_code;
        public byte drive_mode;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public sbyte[] reserve3;

        public int cmd_pos;

        /* 0020h */
        public int fb_pos;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 27)]
        public sbyte[] reserve4;

        public byte check_sum;

        /* 0040h */

        public void Initialize()
        {
            reserve1 = new sbyte[2];
            reserve2 = new sbyte[2];
            reserve3 = new sbyte[3];
            reserve4 = new sbyte[27];
        }
    }

    /*----------------------------------------------------------------------*/
    /* interrupt callback table                                             */
    /*----------------------------------------------------------------------*/
    [StructLayout(LayoutKind.Sequential)]
    public struct INT_CB_DATA
    {
        /* 0000h */
        public int board_id;
        public int channel;
        public uint free_run_cnt;
        public byte sys_factor_bit;
        public sbyte reserve1;
        public ushort sys_factor;

        /* 0010h */
        public ulong axis_factor_bit;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public sbyte[] reserve2;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 48)]
        public uint[] axis_factor;

        /* 00E0h */
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x20)]
        public sbyte[] reserve3;

        /* 0100h */
        public uint oas_factor_bit;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        public sbyte[] reserve4;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] oas_factor;

        /* 0130h */
        public ulong pass_factor_bit;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public sbyte[] reserve5;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] pass_factor;

        /* 0180h */
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x180)]
        public sbyte[] reserve6;

        /* 0300H */

        public void Initialize()
        {
            reserve2 = new sbyte[8];
            axis_factor = new uint[48];
            reserve3 = new sbyte[0x20];
            reserve4 = new sbyte[12];
            oas_factor = new byte[32];
            reserve5 = new sbyte[8];
            pass_factor = new byte[64];
            reserve6 = new sbyte[0x180];
        }
    }

    delegate void CallBackFuncDelegate(out INT_CB_DATA pCbData);


    /*%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%*/
    /* mc2xxFuncStd                                                              */
    /*%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%*/

    delegate int TYP_sscGetLastError();
    delegate int TYP_sscGetMountChannel(int board_id, out short mountch);

    delegate int TYP_sscOpen(int board_id);
    delegate int TYP_sscClose(int board_id);

    delegate int TYP_sscGetControlCycle(int board_id, int channel, out short ctrl_cycle);
    delegate int TYP_sscGetBoardVersion(int board_id, int channel, sbyte[] version);
    delegate int TYP_sscGetDriverVersion(int board_id, int channel, int axnum, sbyte[] version);
    delegate int TYP_sscGetUnitVersion(int board_id, int channel, int utnum, sbyte[] version);
    delegate int TYP_sscGetOperationCycleMonitor(int board_id, int channel, out short now, out short max, out short over, out sbyte status);
    delegate int TYP_sscClearOperationCycleMonitor(int board_id, int channel);

    delegate int TYP_sscResetAllParameter(int board_id, int channel, int timeout);
    delegate int TYP_sscChangeParameter(int board_id, int channel, int axnum, short prmnum, short data);
    delegate int TYP_sscChange2Parameter(int board_id, int channel, int axnum, short[] prmnum, short[] data, sbyte[] status);
    delegate int TYP_sscCheckParameter(int board_id, int channel, int axnum, short prmnum, out short data);
    delegate int TYP_sscCheck2Parameter(int board_id, int channel, int axnum, short[] prmnum, short[] data, sbyte[] status);
    delegate int TYP_sscLoadAllParameterFromFlashROM(int board_id, int channel, int timeout);
    delegate int TYP_sscSaveAllParameterToFlashROM(int board_id, int channel, int timeout);
    delegate int TYP_sscCheckSvPrmChangeNumEx(int board_id, int channel, int axnum, short[] prmnum);

    delegate int TYP_sscReboot(int board_id, int channel, int timeout);
    delegate int TYP_sscSystemStart(int board_id, int channel, int timeout);
    delegate int TYP_sscGetSystemStatusCode(int board_id, int channel, out short statuscode);
    delegate int TYP_sscReconnectSSCNET(int board_id, int channel, out ulong ctrl_axbit, out ushort err_code);
    delegate int TYP_sscDisconnectSSCNET(int board_id, int channel, int com_num, out ulong ctrl_axbit, out ushort err_code);

    delegate int TYP_sscSetCommandBitSignalEx(int board_id, int channel, int axnum, int bitnum, int bitdata);
    delegate int TYP_sscGetStatusBitSignalEx(int board_id, int channel, int axnum, int bitnum, out int bitstatus);
    delegate int TYP_sscWaitStatusBitSignalEx(int board_id, int channel, int axnum, int bitnum, int waitstatus, int timeout);

    delegate int TYP_sscSetPointDataEx(int board_id, int channel, int axnum, int pntnum, ref PNT_DATA_EX pPntDataEx);
    delegate int TYP_sscCheckPointDataEx(int board_id, int channel, int axnum, int pntnum, out PNT_DATA_EX pPntDataEx);
    delegate int TYP_sscSetPointOffset(int board_id, int channel, int axnum, short offset);
    delegate int TYP_sscCheckPointOffset(int board_id, int channel, int axnum, out short offset);
    delegate int TYP_sscGetDrivingPointNumber(int board_id, int channel, int axnum, out short driving_pnt);

    delegate int TYP_sscSetPressData(int board_id, int channel, int axnum, ref PRESS_DATA pPressData);
    delegate int TYP_sscGetPressData(int board_id, int channel, int axnum, out PRESS_DATA pPressData);

    delegate int TYP_sscJogStart(int board_id, int channel, int axnum, int speed, short tca, short tcd, sbyte dir);
    delegate int TYP_sscJogStop(int board_id, int channel, int axnum);
    delegate int TYP_sscJogStopNoWait(int board_id, int channel, int axnum, out short stpsts);
    delegate int TYP_sscIncStart(int board_id, int channel, int axnum, int distance, int speed, short tca, short tcd);
    delegate int TYP_sscAutoStart(int board_id, int channel, int axnum, int point_s, int point_e);
    delegate int TYP_sscHomeReturnStart(int board_id, int channel, int axnum);
    delegate int TYP_sscLinearStart(int board_id, int channel, int axnum, int grpnum, int point_s, int point_e);
    delegate int TYP_sscDataSetStart(int board_id, int channel, int axnum);
    delegate int TYP_sscDriveStop(int board_id, int channel, int axnum, int timeout);
    delegate int TYP_sscDriveStopNoWait(int board_id, int channel, int axnum, out short stpsts);
    delegate int TYP_sscDriveRapidStop(int board_id, int channel, int axnum, int timeout);
    delegate int TYP_sscDriveRapidStopNoWait(int board_id, int channel, int axnum, out short stpsts);
    delegate int TYP_sscSetDriveMode(int board_id, int channel, int axnum, int drv_mode);
    delegate int TYP_sscGetDriveMode(int board_id, int channel, int axnum, out int drv_mode);
    delegate int TYP_sscGetDriveFinStatus(int board_id, int channel, int axnum, int fin_type, out int fin_status);
    delegate int TYP_sscChangeControlMode(int board_id, int channel, int axnum, ushort ctrl_mode);

    delegate int TYP_sscChangeManualPosition(int board_id, int channel, int axnum, int position);
    delegate int TYP_sscChangeAutoPosition(int board_id, int channel, int axnum, int pntnum, int position);
    delegate int TYP_sscChangeLinearPosition(int board_id, int channel, int axnum, int grpnum, int pntnum, int[] pPosition);
    delegate int TYP_sscChangeManualSpeed(int board_id, int channel, int axnum, int speed);
    delegate int TYP_sscChangeAutoSpeed(int board_id, int channel, int axnum, int pntnum, int speed);
    delegate int TYP_sscChangeManualAccTime(int board_id, int channel, int axnum, short acctime);
    delegate int TYP_sscChangeAutoAccTime(int board_id, int channel, int axnum, int pntnum, short acctime);
    delegate int TYP_sscChangeManualDecTime(int board_id, int channel, int axnum, short dectime);
    delegate int TYP_sscChangeAutoDecTime(int board_id, int channel, int axnum, int pntnum, short dectime);

    delegate int TYP_sscGetAlarm(int board_id, int channel, int axnum, int alarm_type, out ushort code, out ushort detail_code);
    delegate int TYP_sscResetAlarm(int board_id, int channel, int axnum, int alarm_type);

    delegate int TYP_sscSetMonitor(int board_id, int channel, int axnum, short[] monnum);
    delegate int TYP_sscStopMonitor(int board_id, int channel, int axnum);
    delegate int TYP_sscGetMonitor(int board_id, int channel, int axnum, short[] monnum, short[] mondata);
    delegate int TYP_sscGetCurrentCmdPositionFast(int board_id, int channel, int axnum, out int position);
    delegate int TYP_sscGetCurrentFbPositionFast(int board_id, int channel, int axnum, out int position);
    delegate int TYP_sscGetIoStatusFast(int board_id, int channel, int axnum, out short din);
    delegate int TYP_sscGetCmdSpeedFast(int board_id, int channel, int axnum, out int speed);
    delegate int TYP_sscGetFbSpeedFast(int board_id, int channel, int axnum, out int speed);
    delegate int TYP_sscGetCurrentFbFast(int board_id, int channel, int axnum, out short currentFb);
    delegate int TYP_sscGetPositionDroopFast(int board_id, int channel, int axnum, out int position_dp);
    delegate int TYP_sscGetExFastMonitor(int board_id, int channel, int axnum, short[] mondata);

    delegate int TYP_sscWdEnable(int board_id, int channel, ushort wdcnt);
    delegate int TYP_sscWdDisable(int board_id, int channel);
    delegate int TYP_sscChangeWdCounter(int board_id, int channel);

    delegate int TYP_sscSetOtherAxisStartData(int board_id, int channel, int oas_num, ref OAS_DATA pOasData);
    delegate int TYP_sscGetOtherAxisStartData(int board_id, int channel, int oas_num, out OAS_DATA pOasData);
    delegate int TYP_sscOtherAxisStartAbortOn(int board_id, int channel, int oas_num);
    delegate int TYP_sscOtherAxisStartAbortOff(int board_id, int channel, int oas_num);
    delegate int TYP_sscGetOtherAxisStartStatus(int board_id, int channel, int oas_num, out short status);

    delegate int TYP_sscSetIntPassPositionData(int board_id, int channel, int pass_num, uint pass_option, int pass_data);
    delegate int TYP_sscCheckIntPassPositionData(int board_id, int channel, int pass_num, out uint pass_option, out int pass_data);
    delegate int TYP_sscSetStartingPassNumber(int board_id, int channel, int axnum, int pass_start, int pass_end);
    delegate int TYP_sscGetExecutingPassNumber(int board_id, int channel, int axnum, out short executing_pass);

    delegate int TYP_sscStartSampling(int board_id, int channel);
    delegate int TYP_sscStopSampling(int board_id, int channel);
    delegate int TYP_sscSetSamplingParameter(int board_id, int channel, short prm_num, int prm_data);
    delegate int TYP_sscGetSamplingParameter(int board_id, int channel, short prm_num, out int prm_data);
    delegate int TYP_sscGetSamplingError(int board_id, int channel, out SMP_ERR pSmpErr);
    delegate int TYP_sscGetSamplingStatus(int board_id, int channel, out sbyte status, out short fin_page);
    delegate int TYP_sscGetSamplingData(int board_id, int channel, int page_num, out short valid_num, SMP_DATA[] pSmpData);

    delegate int TYP_sscStartLog(int board_id, int channel);
    delegate int TYP_sscStopLog(int board_id, int channel);
    delegate int TYP_sscCheckLogStatus(int board_id, int channel, out int status);
    delegate int TYP_sscCheckLogEventNum(int board_id, int channel, out int eventnum);
    delegate int TYP_sscReadLogData(int board_id, int channel, int page_num, LOG_DATA[] pLogData);
    delegate int TYP_sscClearLogData(int board_id, int channel);

    delegate int TYP_sscCheckAlarmHistoryEventNum(int board_id, int channel, out int eventnum);
    delegate int TYP_sscGetAlarmHistoryData(int board_id, int channel, int page_num, ALH_DATA[] pAlhData);
    delegate int TYP_sscClearAlarmHistoryData(int board_id, int channel);

    delegate int TYP_sscGetDigitalInputDataBit(int board_id, int channel, int din_num, out int din);
    delegate int TYP_sscGetDigitalInputDataWord(int board_id, int channel, int din_word_num, out ushort din);
    delegate int TYP_sscSetDigitalOutputDataBit(int board_id, int channel, int dout_num, int dout);
    delegate int TYP_sscSetDigitalOutputDataWord(int board_id, int channel, int dout_word_num, ushort dout);
    delegate int TYP_sscGetDigitalOutputDataBit(int board_id, int channel, int dout_num, out int dout);
    delegate int TYP_sscGetDigitalOutputDataWord(int board_id, int channel, int dout_word_num, out ushort dout);

    delegate int TYP_sscSetChar(int board_id, int channel, int offset, sbyte data);
    delegate int TYP_sscSetShort(int board_id, int channel, int offset, short data);
    delegate int TYP_sscSetLong(int board_id, int channel, int offset, int data);
    delegate int TYP_sscSetBlock(int board_id, int channel, int offset, int size, sbyte[] data_p);

    delegate int TYP_sscGetChar(int board_id, int channel, int offset, out sbyte data_p);
    delegate int TYP_sscGetShort(int board_id, int channel, int offset, out short data_p);
    delegate int TYP_sscGetLong(int board_id, int channel, int offset, out int data_p);
    delegate int TYP_sscGetBlock(int board_id, int channel, int offset, int size, sbyte[] data_p);

    delegate int TYP_sscGetMarkDetectionData(int board_id, int channel, int axnum, int mark_num, out int read_fin_num, out sbyte edge, out int position);
    delegate int TYP_sscGetMarkDetectionCounter(int board_id, int channel, int axnum, int mark_num, out int detected_counter);
    delegate int TYP_sscClearMarkDetectionData(int board_id, int channel, int axnum, int mark_num);

    delegate int TYP_sscIfmGetReadErrorCount(int board_id, int channel, out short errcnt);
    delegate int TYP_sscIfmSetHomePosition(int board_id, int channel, int axnum, int mode, short[] param);
    delegate int TYP_sscIfmGetMaximumBufferNumber(int board_id, int channel, int axnum, out short bufnum);
    delegate int TYP_sscIfmGetMaximumBufferNumberEx(int board_id, int channel, int axnum, ushort ctrl_mode, out short bufnum);
    delegate int TYP_sscIfmRenewLatestBuffer(int board_id, int channel, int axnum, int bufdata, out short bufnum);
    delegate int TYP_sscIfmRenewLatestBufferEx(int board_id, int channel, int axnum, ushort ctrl_mode, int bufdata, out short bufnum);
    delegate int TYP_sscIfmCheckLatestBuffer(int board_id, int channel, int axnum, out short bufnum, out int bufdata);
    delegate int TYP_sscIfmCheckLatestBufferEx(int board_id, int channel, int axnum, ushort ctrl_mode, out short bufnum, out int bufdata);
    delegate int TYP_sscIfmGetTransmitBuffer(int board_id, int channel, int axnum, out short bufnum, out int bufdata);
    delegate int TYP_sscIfmGetTransmitBufferEx(int board_id, int channel, int axnum, ushort ctrl_mode, out short bufnum, out int bufdata);
    delegate int TYP_sscIfmTrqSetSpeedLimit(int board_id, int channel, int axnum, int speed);
    delegate int TYP_sscIfmSetControlMode(int board_id, int channel, int axnum, ushort ctrl_mode);
    delegate int TYP_sscIfmGetControlMode(int board_id, int channel, int axnum, out ushort ctrl_mode, out sbyte status);
    delegate int TYP_sscIfmGetEventStatusBits(int board_id, int channel, int bitnum, out ulong status_bits);

    /*%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%*/
    /* mc2xxFuncInt                                                              */
    /*%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%*/

    delegate int TYP_sscIntStart(int board_id, int priority);
    delegate int TYP_sscIntEnd(int board_id);

    delegate int TYP_sscIntEnable(int board_id, int channel);
    delegate int TYP_sscIntDisable(int board_id, int channel);

    delegate int TYP_sscRegisterIntCallback(int board_id, int channel, ref CallBackFuncDelegate cbfunc);
    delegate int TYP_sscUnregisterIntCallback(int board_id, int channel);

    delegate int TYP_sscResetIntEvent(int board_id, int channel, int axnum, int eventnum, int eventfactor);
    delegate int TYP_sscSetIntEvent(int board_id, int channel, int axnum, int eventnum, int eventfactor);
    delegate int TYP_sscWaitIntEvent(int board_id, int channel, int axnum, int eventnum, int eventfactor, int timeout);

    delegate int TYP_sscResetIntEventMulti(int board_id, int channel, int axnum);
    delegate int TYP_sscSetIntEventMulti(int board_id, int channel, int axnum);
    delegate int TYP_sscWaitIntEventMulti(int board_id, int channel, int axnum, int timeout, out uint eventcode);

    delegate int TYP_sscResetIntOasEvent(int board_id, int channel, int axnum, int oas_num);
    delegate int TYP_sscSetIntOasEvent(int board_id, int channel, int axnum, int oas_num);
    delegate int TYP_sscWaitIntOasEvent(int board_id, int channel, int axnum, int oas_num, int oas_type, out int oas_status, int timeout);

    delegate int TYP_sscResetIntPassPosition(int board_id, int channel, int pass_start, int pass_end);
    delegate int TYP_sscSetIntPassPosition(int board_id, int channel, int pass_start, int pass_end);
    delegate int TYP_sscWaitIntPassPosition(int board_id, int channel, int pass_num, out int pass_status, int timeout);

    delegate int TYP_sscResetIntDriveFin(int board_id, int channel, int axnum);
    delegate int TYP_sscSetIntDriveFin(int board_id, int channel, int axnum);
    delegate int TYP_sscWaitIntDriveFin(int board_id, int channel, int axnum, int fin_type, out int fin_status, int timeout);


    /*%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%*/
    /* mc2xxFuncStd (compatible)                                                 */
    /*%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%*/

    /*----------------------------------------------------------------------*/
    /* point table                                                          */
    /*----------------------------------------------------------------------*/
    [StructLayout(LayoutKind.Sequential)]
    public struct PNT_DATA
    {
        /* 0000h */
        public int position;
        public uint speed;
        public ushort actime;
        public ushort dctime;
        public ushort dwell;
        public ushort subcmd;

        /* 0010h */
    }

    delegate int TYP_sscGetIntOutMask(int board_id, out short iom);
    delegate int TYP_sscGetHWIntOut(int board_id, out short hio);

    delegate int TYP_sscCheckChannelReady(int board_id, int channel);

    delegate int TYP_sscSetSystemCommandBitSignal(int board_id, int channel, int offset, int bitno, int data);
    delegate int TYP_sscSetAxisCommandBitSignal(int board_id, int channel, int axnum, int offset, int bitno, int data);
    delegate int TYP_sscGetSystemStatusBits(int board_id, int channel, short[] statusbits);
    delegate int TYP_sscGetAxisStatusBits(int board_id, int channel, int axnum, short[] statusbits);
    delegate int TYP_sscEmgStopOn(int board_id);
    delegate int TYP_sscCheckEmgStopOn(int board_id);
    delegate int TYP_sscCheckEmgStopOnNoWait(int board_id, out short emionsts);
    delegate int TYP_sscEmgStopOff(int board_id);
    delegate int TYP_sscCheckEmgStopOff(int board_id);
    delegate int TYP_sscCheckEmgStopOffNoWait(int board_id, out short emioffsts);
    delegate int TYP_sscAsynchronousOn(int board_id, int channel, int grpnum);
    delegate int TYP_sscCheckAsynchronousOn(int board_id, int channel, int grpnum);
    delegate int TYP_sscCheckAsynchronousOnNoWait(int board_id, int channel, int grpnum, out short asonsts);
    delegate int TYP_sscAsynchronousOff(int board_id, int channel, int grpnum);
    delegate int TYP_sscCheckAsynchronousOff(int board_id, int channel, int grpnum);
    delegate int TYP_sscCheckAsynchronousOffNoWait(int board_id, int channel, int grpnum, out short asofsts);
    delegate int TYP_sscCheckSyncMatchExecOn(int board_id, int channel, int grpnum);
    delegate int TYP_sscCheckSyncMatchExecOnNoWait(int board_id, int channel, int grpnum, out short smeonsts);
    delegate int TYP_sscCheckSyncMatchExecOff(int board_id, int channel, int grpnum);
    delegate int TYP_sscCheckSyncMatchExecOffNoWait(int board_id, int channel, int grpnum, out short smeofsts);
    delegate int TYP_sscServoOn(int board_id, int channel, int axnum);
    delegate int TYP_sscCheckServoOn(int board_id, int channel, int axnum);
    delegate int TYP_sscCheckServoOnNoWait(int board_id, int channel, int axnum, out short svonsts);
    delegate int TYP_sscServoOff(int board_id, int channel, int axnum);
    delegate int TYP_sscCheckServoOff(int board_id, int channel, int axnum);
    delegate int TYP_sscCheckServoOffNoWait(int board_id, int channel, int axnum, out short svofsts);
    delegate int TYP_sscTorqueLimitOn(int board_id, int channel, int axnum);
    delegate int TYP_sscCheckTorqueLimitCrampOn(int board_id, int channel, int axnum);
    delegate int TYP_sscCheckTorqueLimitCrampOnNoWait(int board_id, int channel, int axnum, out short tconsts);
    delegate int TYP_sscTorqueLimitOff(int board_id, int channel, int axnum);
    delegate int TYP_sscCheckTorqueLimitCrampOff(int board_id, int channel, int axnum);
    delegate int TYP_sscCheckTorqueLimitCrampOffNoWait(int board_id, int channel, int axnum, out short tcofsts);
    delegate int TYP_sscCheckPositionSwitchOn(int board_id, int channel, int axnum);
    delegate int TYP_sscCheckPositionSwitchOnNoWait(int board_id, int channel, int axnum, out short psonsts);
    delegate int TYP_sscCheckPositionSwitchOff(int board_id, int channel, int axnum);
    delegate int TYP_sscCheckPositionSwitchOffNoWait(int board_id, int channel, int axnum, out short psofsts);
    delegate int TYP_sscInterlockOn(int board_id, int channel, int axnum);
    delegate int TYP_sscCheckInterlockStopOn(int board_id, int channel, int axnum);
    delegate int TYP_sscCheckInterlockStopOnNoWait(int board_id, int channel, int axnum, out short isonsts);
    delegate int TYP_sscInterlockOff(int board_id, int channel, int axnum);
    delegate int TYP_sscCheckInterlockStopOff(int board_id, int channel, int axnum);
    delegate int TYP_sscCheckInterlockStopOffNoWait(int board_id, int channel, int axnum, out short isofsts);
    delegate int TYP_sscCheckStopPositionOverOn(int board_id, int channel, int axnum);
    delegate int TYP_sscCheckStopPositionOverOnNoWait(int board_id, int channel, int axnum, out short ovonsts);
    delegate int TYP_sscCheckStopPositionOverOff(int board_id, int channel, int axnum);
    delegate int TYP_sscCheckStopPositionOverOffNoWait(int board_id, int channel, int axnum, out short ovofsts);
    delegate int TYP_sscGainChangeOn(int board_id, int channel, int axnum);
    delegate int TYP_sscCheckGainChangeOn(int board_id, int channel, int axnum);
    delegate int TYP_sscCheckGainChangeOnNoWait(int board_id, int channel, int axnum, out short gconsts);
    delegate int TYP_sscGainChangeOff(int board_id, int channel, int axnum);
    delegate int TYP_sscCheckGainChangeOff(int board_id, int channel, int axnum);
    delegate int TYP_sscCheckGainChangeOffNoWait(int board_id, int channel, int axnum, out short gcofsts);
    delegate int TYP_sscCheckTorqueLimitSelectOn(int board_id, int channel, int axnum);
    delegate int TYP_sscCheckTorqueLimitSelectOnNoWait(int board_id, int channel, int axnum, out short tsonsts);
    delegate int TYP_sscCheckTorqueLimitSelectOff(int board_id, int channel, int axnum);
    delegate int TYP_sscCheckTorqueLimitSelectOffNoWait(int board_id, int channel, int axnum, out short tsofsts);
    delegate int TYP_sscCheckInterfareWaitOn(int board_id, int channel, int axnum);
    delegate int TYP_sscCheckInterfareWaitOnNoWait(int board_id, int channel, int axnum, out short iwonsts);
    delegate int TYP_sscCheckInterfareWaitOff(int board_id, int channel, int axnum);
    delegate int TYP_sscCheckInterfareWaitOffNoWait(int board_id, int channel, int axnum, out short iwofsts);

    delegate int TYP_sscSetPointData(int board_id, int channel, int axnum, int pntnum, ref PNT_DATA pPntData);
    delegate int TYP_sscCheckPointData(int board_id, int channel, int axnum, int pntnum, out PNT_DATA pPntData);

    delegate int TYP_sscGetControlAlarmCode(int board_id, int channel, out short code);
    delegate int TYP_sscGetOperationAlarmCode(int board_id, int channel, int axnum, out short code);
    delegate int TYP_sscGetExOperationAlarmCode(int board_id, int channel, int axnum, out short code, out short detail_code);
    delegate int TYP_sscGetServoAlarmCode(int board_id, int channel, int axnum, out short code);

    delegate int TYP_sscControlAlarmReset(int board_id, int channel);
    delegate int TYP_sscOperationAlarmReset(int board_id, int channel, int axnum);
    delegate int TYP_sscServoAlarmReset(int board_id, int channel, int axnum);

    delegate int TYP_sscGetEmgStatus(int board_id, int channel, out short emgstatus);
    delegate int TYP_sscGetAxisFixConf(int board_id, int channel, int axnum, out short code);


    /*%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%*/
    /* mc2xxFuncInt (compatible)                                                 */
    /*%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%*/

    delegate int TYP_sscSetIntFactor(int board_id, int channel, int num, short[] factorbit);
    delegate int TYP_sscGetIntFactor(int board_id, int channel, int num, short[] factorbit);

    delegate int TYP_sscTerminateInt(int board_id, int channel);
    delegate int TYP_sscClearInt(int board_id, int channel);

    delegate int TYP_sscSetPtpEndType(int board_id, int channel, int axnum, short ptpendtype);
    delegate int TYP_sscResetEndChk(int board_id, int channel, int axnum);
    delegate int TYP_sscEndChk(int board_id, int channel, int axnum, out int endstatus);


    static class SscApi
    {
        public const string VER_MC2XXSTD_H = "Ver.1.73";

        static IntPtr hDll;

        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32", SetLastError = true)]
        static extern bool FreeLibrary(IntPtr hModule);

        [DllImport("kernel32", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = false)]
        static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        /*%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%*/
        /* mc2xxFuncStd                                                              */
        /*%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%*/

        /*---------------------------------------------------------------------------*/
        /* sscGetLastError()'s answer data                                           */
        /*---------------------------------------------------------------------------*/

        /* default & unknown */
        public const uint SSC_FUNC_ERR_UNKNOWN = (0xFFFFFFFF);
        public const uint SSC_FUNC_ERR_UNSURPORT_OS = (0xFFFFFFFE);

        /* argument */
        public const uint SSC_FUNC_ERR_ARGUMENT_01 = (0x00000001);
        public const uint SSC_FUNC_ERR_ARGUMENT_02 = (0x00000002);
        public const uint SSC_FUNC_ERR_ARGUMENT_03 = (0x00000003);
        public const uint SSC_FUNC_ERR_ARGUMENT_04 = (0x00000004);
        public const uint SSC_FUNC_ERR_ARGUMENT_05 = (0x00000005);
        public const uint SSC_FUNC_ERR_ARGUMENT_06 = (0x00000006);
        public const uint SSC_FUNC_ERR_ARGUMENT_07 = (0x00000007);
        public const uint SSC_FUNC_ERR_ARGUMENT_08 = (0x00000008);
        public const uint SSC_FUNC_ERR_ARGUMENT_09 = (0x00000009);
        public const uint SSC_FUNC_ERR_ARGUMENT_MISMATCH = (0x00000100);

        /* time out */
        public const uint SSC_FUNC_ERR_TIMEOUT_01 = (0x00010000);
        public const uint SSC_FUNC_ERR_TIMEOUT_02 = (0x00010100);
        public const uint SSC_FUNC_ERR_TIMEOUT_03 = (0x00010200);
        public const uint SSC_FUNC_ERR_TIMEOUT_04 = (0x00010300);
        public const uint SSC_FUNC_ERR_TIMEOUT_05 = (0x00010400);
        public const uint SSC_FUNC_ERR_TIMEOUT_06 = (0x00010500);
        public const uint SSC_FUNC_ERR_TIMEOUT_07 = (0x00010600);
        public const uint SSC_FUNC_ERR_TIMEOUT_08 = (0x00010700);
        public const uint SSC_FUNC_ERR_TIMEOUT_09 = (0x00010800);

        /* general-purpose access */
        public const uint SSC_FUNC_ERR_BOUNDARY_ADDRESS = (0x00011000);
        public const uint SSC_FUNC_ERR_ADDRESS_RANGE_OVER = (0x00011001);

        /* device open and close */
        public const uint SSC_FUNC_ERR_REOPEN = (0x00020000);
        public const uint SSC_FUNC_ERR_UNOPEN = (0x00020010);
        public const uint SSC_FUNC_ERR_NOT_FOUND_BOARD = (0x00021010);
        public const uint SSC_FUNC_ERR_GET_CHANNEL_NUM = (0x00021011);
        public const uint SSC_FUNC_ERR_UNSUPPORT_DEVICE_DRIVER = (0x00021012);

        public const uint SSC_FUNC_ERR_CREATE_SEMAPHORE = (0x00022000);
        public const uint SSC_FUNC_ERR_DELETE_SEMAPHORE = (0x00022001);
        public const uint SSC_FUNC_ERR_WAIT_SEMAPHORE = (0x00022002);
        public const uint SSC_FUNC_ERR_RELEASE_SEMAPHORE = (0x00022003);
        public const uint SSC_FUNC_ERR_CREATE_EVENT = (0x00022010);
        public const uint SSC_FUNC_ERR_DELETE_EVENT = (0x00022011);
        public const uint SSC_FUNC_ERR_RESET_EVENT = (0x00022012);
        public const uint SSC_FUNC_ERR_SET_EVENT = (0x00022013);
        public const uint SSC_FUNC_ERR_WAIT_EVENT = (0x00022014);
        public const uint SSC_FUNC_ERR_WAIT_EVENT_MULTI = (0x00022015);
        public const uint SSC_FUNC_ERR_CREATE_THREAD = (0x00022020);
        public const uint SSC_FUNC_ERR_DELETE_THREAD = (0x00022021);
        public const uint SSC_FUNC_ERR_THREAD_PRIORITY = (0x00022022);
        public const uint SSC_FUNC_ERR_RESUME_THREAD = (0x00022023);
        public const uint SSC_FUNC_ERR_GET_EXIT_CODE_THREAD = (0x00022024);
        public const uint SSC_FUNC_ERR_CREATE_MUTEX = (0x00022040);
        public const uint SSC_FUNC_ERR_DELETE_MUTEX = (0x00022041);
        public const uint SSC_FUNC_ERR_DEVICE_DRIVER = (0x00023000);

        /* function for system */
        public const uint SSC_FUNC_ERR_UNREADY_CHANNEL = (0x00030000);
        public const uint SSC_FUNC_ERR_ALREADY_CHANNEL = (0x00030010);
        public const uint SSC_FUNC_ERR_RUNNING_CHANNEL = (0x00030020);
        public const uint SSC_FUNC_ERR_NOW_ALARM_SYSTEM = (0x00030030);
        public const uint SSC_FUNC_ERR_FLASHROM_PARAM_LOAD = (0x00030040);
        public const uint SSC_FUNC_ERR_STS_BIT_FRNG = (0x00030041);
        public const uint SSC_FUNC_ERR_STS_BIT_FSNG = (0x00030042);
        public const uint SSC_FUNC_ERR_STS_BIT_RCE = (0x00030050);
        public const uint SSC_FUNC_ERR_STS_BIT_CCE = (0x00030051);
        public const uint SSC_FUNC_ERR_ALREADY_ENABLE_WDT = (0x00030060);
        public const uint SSC_FUNC_ERR_ALREADY_DISABLE_WDT = (0x00030061);
        public const uint SSC_FUNC_ERR_STS_BIT_IFMO = (0x00030062);
        public const uint SSC_FUNC_ERR_UNSET_GLOBAL_VALIABLE = (0x00030100);

        /* function for paramter */
        public const uint SSC_FUNC_ERR_STS_BIT_PREN1 = (0x00040000);
        public const uint SSC_FUNC_ERR_STS_BIT_PREN2 = (0x00040001);
        public const uint SSC_FUNC_ERR_MISMATCH_PARAM_READ_NUM1 = (0x00040002);
        public const uint SSC_FUNC_ERR_MISMATCH_PARAM_READ_NUM2 = (0x00040003);
        public const uint SSC_FUNC_ERR_STS_BIT_PWEN1 = (0x00040020);
        public const uint SSC_FUNC_ERR_STS_BIT_PWEN2 = (0x00040021);
        public const uint SSC_FUNC_ERR_STS_BIT_PWED1 = (0x00040022);
        public const uint SSC_FUNC_ERR_STS_BIT_PWED2 = (0x00040023);
        public const uint SSC_FUNC_ERR_MISMATCH_PARAM_WRITE_NUM1 = (0x00040024);
        public const uint SSC_FUNC_ERR_MISMATCH_PARAM_WRITE_NUM2 = (0x00040025);
        public const uint SSC_FUNC_ERR_MISMATCH_PARAM_WRITE_DATA1 = (0x00040026);
        public const uint SSC_FUNC_ERR_MISMATCH_PARAM_WRITE_DATA2 = (0x00040027);

        /* function for monitor */
        public const uint SSC_FUNC_ERR_STS_BIT_MER1 = (0x00050000);
        public const uint SSC_FUNC_ERR_STS_BIT_MER2 = (0x00050001);
        public const uint SSC_FUNC_ERR_STS_BIT_MER3 = (0x00050002);
        public const uint SSC_FUNC_ERR_STS_BIT_MER4 = (0x00050003);
        public const uint SSC_FUNC_ERR_STS_BIT_MESV = (0x00050004);
        public const uint SSC_FUNC_ERR_ALREADY_MONITOR_STOP = (0x00050010);
        public const uint SSC_FUNC_ERR_NOT_START_MONITOR = (0x00050011);
        public const uint SSC_FUNC_ERR_MONITOR_2READ = (0x00050012);

        /* function for axis */
        public const uint SSC_FUNC_ERR_NOW_DRIVING = (0x00060010);
        public const uint SSC_FUNC_ERR_NOW_DRIVING_READY = (0x00060011);
        public const uint SSC_FUNC_ERR_NOW_ALARM_SERVO = (0x00060020);
        public const uint SSC_FUNC_ERR_NOW_ALARM_DRIVE = (0x00060030);
        public const uint SSC_FUNC_ERR_MISMATCH_DRIVE_MODE = (0x00060040);
        public const uint SSC_FUNC_ERR_CHG_POS_DIR = (0x00060041);
        public const uint SSC_FUNC_ERR_STS_BIT_PCE = (0x00060060);
        public const uint SSC_FUNC_ERR_STS_BIT_SCE = (0x00060070);
        public const uint SSC_FUNC_ERR_STS_BIT_TACE = (0x00060080);
        public const uint SSC_FUNC_ERR_STS_BIT_TDCE = (0x00060090);
        public const uint SSC_FUNC_ERR_POINT_NUMBER_OVER = (0x000600A0);
        public const uint SSC_FUNC_ERR_STS_BIT_CTLMCE = (0x000600A1);

        /* function for sampling */
        public const uint SSC_FUNC_ERR_ALREADY_START_SAMPLING = (0x00061010);
        public const uint SSC_FUNC_ERR_ALREADY_STOP_SAMPLING = (0x00061011);
        public const uint SSC_FUNC_ERR_STS_BIT_SREN = (0x00061020);
        public const uint SSC_FUNC_ERR_MISMATCH_SMP_PARAM_READ_NUM = (0x00061021);
        public const uint SSC_FUNC_ERR_STS_BIT_SWEN = (0x00061022);
        public const uint SSC_FUNC_ERR_STS_BIT_SWED = (0x00061023);
        public const uint SSC_FUNC_ERR_MISMATCH_SMP_PARAM_WRITE_NUM = (0x00061024);
        public const uint SSC_FUNC_ERR_MISMATCH_SMP_PARAM_WRITE_DATA = (0x00061025);

        /* function for log */
        public const uint SSC_FUNC_ERR_NOW_LOGGING = (0x00062000);
        public const uint SSC_FUNC_ERR_STS_BIT_LOGRE = (0x00062001);
        public const uint SSC_FUNC_ERR_STS_BIT_LOGIE = (0x00062002);
        public const uint SSC_FUNC_ERR_ALREADY_START_LOG = (0x00062003);
        public const uint SSC_FUNC_ERR_ALREADY_STOP_LOG = (0x00062004);

        /* function for alarm history */
        public const uint SSC_FUNC_ERR_UNSUPPORT_ALH = (0x00062005);
        public const uint SSC_FUNC_ERR_STS_BIT_ALHRE = (0x00062006);
        public const uint SSC_FUNC_ERR_STS_BIT_ALHIE = (0x00062007);

        /* function for mark detection */
        public const uint SSC_FUNC_ERR_MARK_DETECT_UNUSABLE = (0x00070000);
        public const uint SSC_FUNC_ERR_MARK_DETECT_UNDETECTED = (0x00070001);

        /* function for interface mode */
        public const uint SSC_FUNC_ERR_IFM_INP_OFF = (0x000D0000);
        public const uint SSC_FUNC_ERR_STS_BIT_ZSE = (0x000D0001);
        public const uint SSC_FUNC_ERR_IFM_CMD_BUF_FULL = (0x000D0002);
        public const uint SSC_FUNC_ERR_DISABLE_EVENT_DETECT = (0x000D0003);

        /*---------------------------------------------------------------------------*/

        static public TYP_sscGetLastError sscGetLastError;
        static public TYP_sscGetMountChannel sscGetMountChannel;
        static public TYP_sscOpen sscOpen;
        static public TYP_sscClose sscClose;
        static public TYP_sscGetControlCycle sscGetControlCycle;
        static public TYP_sscGetBoardVersion sscGetBoardVersion;
        static public TYP_sscGetDriverVersion sscGetDriverVersion;
        static public TYP_sscGetUnitVersion sscGetUnitVersion;
        static public TYP_sscGetOperationCycleMonitor sscGetOperationCycleMonitor;
        static public TYP_sscClearOperationCycleMonitor sscClearOperationCycleMonitor;
        static public TYP_sscResetAllParameter sscResetAllParameter;
        static public TYP_sscChangeParameter sscChangeParameter;
        static public TYP_sscChange2Parameter sscChange2Parameter;
        static public TYP_sscCheckParameter sscCheckParameter;
        static public TYP_sscCheck2Parameter sscCheck2Parameter;
        static public TYP_sscLoadAllParameterFromFlashROM sscLoadAllParameterFromFlashROM;
        static public TYP_sscSaveAllParameterToFlashROM sscSaveAllParameterToFlashROM;
        static public TYP_sscCheckSvPrmChangeNumEx sscCheckSvPrmChangeNumEx;
        static public TYP_sscReboot sscReboot;
        static public TYP_sscSystemStart sscSystemStart;
        static public TYP_sscGetSystemStatusCode sscGetSystemStatusCode;
        static public TYP_sscReconnectSSCNET sscReconnectSSCNET;
        static public TYP_sscDisconnectSSCNET sscDisconnectSSCNET;
        static public TYP_sscSetCommandBitSignalEx sscSetCommandBitSignalEx;
        static public TYP_sscGetStatusBitSignalEx sscGetStatusBitSignalEx;
        static public TYP_sscWaitStatusBitSignalEx sscWaitStatusBitSignalEx;
        static public TYP_sscSetPointDataEx sscSetPointDataEx;
        static public TYP_sscCheckPointDataEx sscCheckPointDataEx;
        static public TYP_sscSetPointOffset sscSetPointOffset;
        static public TYP_sscCheckPointOffset sscCheckPointOffset;
        static public TYP_sscGetDrivingPointNumber sscGetDrivingPointNumber;
        static public TYP_sscSetPressData sscSetPressData;
        static public TYP_sscGetPressData sscGetPressData;
        static public TYP_sscJogStart sscJogStart;
        static public TYP_sscJogStop sscJogStop;
        static public TYP_sscJogStopNoWait sscJogStopNoWait;
        static public TYP_sscIncStart sscIncStart;
        static public TYP_sscAutoStart sscAutoStart;
        static public TYP_sscHomeReturnStart sscHomeReturnStart;
        static public TYP_sscLinearStart sscLinearStart;
        static public TYP_sscDataSetStart sscDataSetStart;
        static public TYP_sscDriveStop sscDriveStop;
        static public TYP_sscDriveStopNoWait sscDriveStopNoWait;
        static public TYP_sscDriveRapidStop sscDriveRapidStop;
        static public TYP_sscDriveRapidStopNoWait sscDriveRapidStopNoWait;
        static public TYP_sscSetDriveMode sscSetDriveMode;
        static public TYP_sscGetDriveMode sscGetDriveMode;
        static public TYP_sscGetDriveFinStatus sscGetDriveFinStatus;
        static public TYP_sscChangeControlMode sscChangeControlMode;
        static public TYP_sscChangeManualPosition sscChangeManualPosition;
        static public TYP_sscChangeAutoPosition sscChangeAutoPosition;
        static public TYP_sscChangeLinearPosition sscChangeLinearPosition;
        static public TYP_sscChangeManualSpeed sscChangeManualSpeed;
        static public TYP_sscChangeAutoSpeed sscChangeAutoSpeed;
        static public TYP_sscChangeManualAccTime sscChangeManualAccTime;
        static public TYP_sscChangeAutoAccTime sscChangeAutoAccTime;
        static public TYP_sscChangeManualDecTime sscChangeManualDecTime;
        static public TYP_sscChangeAutoDecTime sscChangeAutoDecTime;
        static public TYP_sscGetAlarm sscGetAlarm;
        static public TYP_sscResetAlarm sscResetAlarm;
        static public TYP_sscSetMonitor sscSetMonitor;
        static public TYP_sscStopMonitor sscStopMonitor;
        static public TYP_sscGetMonitor sscGetMonitor;
        static public TYP_sscGetCurrentCmdPositionFast sscGetCurrentCmdPositionFast;
        static public TYP_sscGetCurrentFbPositionFast sscGetCurrentFbPositionFast;
        static public TYP_sscGetIoStatusFast sscGetIoStatusFast;
        static public TYP_sscGetCmdSpeedFast sscGetCmdSpeedFast;
        static public TYP_sscGetFbSpeedFast sscGetFbSpeedFast;
        static public TYP_sscGetCurrentFbFast sscGetCurrentFbFast;
        static public TYP_sscGetPositionDroopFast sscGetPositionDroopFast;
        static public TYP_sscGetExFastMonitor sscGetExFastMonitor;
        static public TYP_sscWdEnable sscWdEnable;
        static public TYP_sscWdDisable sscWdDisable;
        static public TYP_sscChangeWdCounter sscChangeWdCounter;
        static public TYP_sscSetOtherAxisStartData sscSetOtherAxisStartData;
        static public TYP_sscGetOtherAxisStartData sscGetOtherAxisStartData;
        static public TYP_sscOtherAxisStartAbortOn sscOtherAxisStartAbortOn;
        static public TYP_sscOtherAxisStartAbortOff sscOtherAxisStartAbortOff;
        static public TYP_sscGetOtherAxisStartStatus sscGetOtherAxisStartStatus;
        static public TYP_sscSetIntPassPositionData sscSetIntPassPositionData;
        static public TYP_sscCheckIntPassPositionData sscCheckIntPassPositionData;
        static public TYP_sscSetStartingPassNumber sscSetStartingPassNumber;
        static public TYP_sscGetExecutingPassNumber sscGetExecutingPassNumber;
        static public TYP_sscStartSampling sscStartSampling;
        static public TYP_sscStopSampling sscStopSampling;
        static public TYP_sscSetSamplingParameter sscSetSamplingParameter;
        static public TYP_sscGetSamplingParameter sscGetSamplingParameter;
        static public TYP_sscGetSamplingError sscGetSamplingError;
        static public TYP_sscGetSamplingStatus sscGetSamplingStatus;
        static public TYP_sscGetSamplingData sscGetSamplingData;
        static public TYP_sscStartLog sscStartLog;
        static public TYP_sscStopLog sscStopLog;
        static public TYP_sscCheckLogStatus sscCheckLogStatus;
        static public TYP_sscCheckLogEventNum sscCheckLogEventNum;
        static public TYP_sscReadLogData sscReadLogData;
        static public TYP_sscClearLogData sscClearLogData;
        static public TYP_sscCheckAlarmHistoryEventNum sscCheckAlarmHistoryEventNum;
        static public TYP_sscGetAlarmHistoryData sscGetAlarmHistoryData;
        static public TYP_sscClearAlarmHistoryData sscClearAlarmHistoryData;
        static public TYP_sscGetDigitalInputDataBit sscGetDigitalInputDataBit;
        static public TYP_sscGetDigitalInputDataWord sscGetDigitalInputDataWord;
        static public TYP_sscSetDigitalOutputDataBit sscSetDigitalOutputDataBit;
        static public TYP_sscSetDigitalOutputDataWord sscSetDigitalOutputDataWord;
        static public TYP_sscGetDigitalOutputDataBit sscGetDigitalOutputDataBit;
        static public TYP_sscGetDigitalOutputDataWord sscGetDigitalOutputDataWord;
        static public TYP_sscSetChar sscSetChar;
        static public TYP_sscSetShort sscSetShort;
        static public TYP_sscSetLong sscSetLong;
        static public TYP_sscSetBlock sscSetBlock;
        static public TYP_sscGetChar sscGetChar;
        static public TYP_sscGetShort sscGetShort;
        static public TYP_sscGetLong sscGetLong;
        static public TYP_sscGetBlock sscGetBlock;
        static public TYP_sscGetMarkDetectionData sscGetMarkDetectionData;
        static public TYP_sscGetMarkDetectionCounter sscGetMarkDetectionCounter;
        static public TYP_sscClearMarkDetectionData sscClearMarkDetectionData;
        static public TYP_sscIfmGetReadErrorCount sscIfmGetReadErrorCount;
        static public TYP_sscIfmSetHomePosition sscIfmSetHomePosition;
        static public TYP_sscIfmGetMaximumBufferNumber sscIfmGetMaximumBufferNumber;
        static public TYP_sscIfmGetMaximumBufferNumberEx sscIfmGetMaximumBufferNumberEx;
        static public TYP_sscIfmRenewLatestBuffer sscIfmRenewLatestBuffer;
        static public TYP_sscIfmRenewLatestBufferEx sscIfmRenewLatestBufferEx;
        static public TYP_sscIfmCheckLatestBuffer sscIfmCheckLatestBuffer;
        static public TYP_sscIfmCheckLatestBufferEx sscIfmCheckLatestBufferEx;
        static public TYP_sscIfmGetTransmitBuffer sscIfmGetTransmitBuffer;
        static public TYP_sscIfmGetTransmitBufferEx sscIfmGetTransmitBufferEx;
        static public TYP_sscIfmTrqSetSpeedLimit sscIfmTrqSetSpeedLimit;
        static public TYP_sscIfmSetControlMode sscIfmSetControlMode;
        static public TYP_sscIfmGetControlMode sscIfmGetControlMode;
        static public TYP_sscIfmGetEventStatusBits sscIfmGetEventStatusBits;

        /*%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%*/
        /* mc2xxFuncInt                                                              */
        /*%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%*/

        /*---------------------------------------------------------------------------*/
        /* sscGetLastError()'s answer data                                           */
        /*---------------------------------------------------------------------------*/

        /* function for interrupt */
        public const uint SSC_FUNC_ERR_ALREADY_START_INT_DRIVER = (0x10000100);
        public const uint SSC_FUNC_ERR_ALREADY_END_INT_DRIVER = (0x10000101);
        public const uint SSC_FUNC_ERR_ALREADY_OTHER_PROCESS_INT = (0x10000102);
        public const uint SSC_FUNC_ERR_INT_DISABLE_MASK = (0x10000103);
        public const uint SSC_FUNC_ERR_CLEAR_INT = (0x10000104);

        public const uint SSC_FUNC_ERR_NOT_START_INT_DRIVER = (0x10000110);
        public const uint SSC_FUNC_ERR_TERMINATE_INT_DRIVER = (0x10000111);
        public const uint SSC_FUNC_ERR_TERMINATE_NOTIFY_EVENT = (0x10000112);
        public const uint SSC_FUNC_ERR_SET_HOST_APPLICATION_EVENT = (0x10000113);
        public const uint SSC_FUNC_ERR_ALREADY_REREGISTER_CALLBACK = (0x10000200);
        public const uint SSC_FUNC_ERR_ALREADY_UNREREGISTER_CALLBACK = (0x10000201);

        public const uint SSC_FUNC_ERR_ALREADY_START_ISR = (0x10000300);

        /*---------------------------------------------------------------------------*/

        static public TYP_sscIntStart sscIntStart;
        static public TYP_sscIntEnd sscIntEnd;

        static public TYP_sscIntEnable sscIntEnable;
        static public TYP_sscIntDisable sscIntDisable;

        static public TYP_sscRegisterIntCallback sscRegisterIntCallback;
        static public TYP_sscUnregisterIntCallback sscUnregisterIntCallback;

        static public TYP_sscResetIntEvent sscResetIntEvent;
        static public TYP_sscSetIntEvent sscSetIntEvent;
        static public TYP_sscWaitIntEvent sscWaitIntEvent;

        static public TYP_sscResetIntEventMulti sscResetIntEventMulti;
        static public TYP_sscSetIntEventMulti sscSetIntEventMulti;
        static public TYP_sscWaitIntEventMulti sscWaitIntEventMulti;

        static public TYP_sscResetIntOasEvent sscResetIntOasEvent;
        static public TYP_sscSetIntOasEvent sscSetIntOasEvent;
        static public TYP_sscWaitIntOasEvent sscWaitIntOasEvent;

        static public TYP_sscResetIntPassPosition sscResetIntPassPosition;
        static public TYP_sscSetIntPassPosition sscSetIntPassPosition;
        static public TYP_sscWaitIntPassPosition sscWaitIntPassPosition;

        static public TYP_sscResetIntDriveFin sscResetIntDriveFin;
        static public TYP_sscSetIntDriveFin sscSetIntDriveFin;
        static public TYP_sscWaitIntDriveFin sscWaitIntDriveFin;


        /*%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%*/
        /* mc2xxFuncStd (compatible)                                                 */
        /*%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%*/

        /*---------------------------------------------------------------------------*/
        /* sscGetLastError()'s answer data                                           */
        /*---------------------------------------------------------------------------*/

        public const uint SSC_FUNC_ERR_IFC_SET_MONITOR_01 = (0x00040030);
        public const uint SSC_FUNC_ERR_IFC_SET_MONITOR_02 = (0x00040130);
        public const uint SSC_FUNC_ERR_IFC_GET_MONITOR_01 = (0x00040040);
        public const uint SSC_FUNC_ERR_IFC_GET_MONITOR_02 = (0x00040140);
        public const uint SSC_FUNC_ERR_IFC_STOP_MONITOR_01 = (0x00040050);
        public const uint SSC_FUNC_ERR_IFC_STOP_MONITOR_02 = (0x00040150);

        public const uint SSC_FUNC_ERR_INT_DRIVER_START = (0x10000020);
        public const uint SSC_FUNC_ERR_END_TYPE_UNSET = (0x10000050);

        /*---------------------------------------------------------------------------*/

        static public TYP_sscGetIntOutMask sscGetIntOutMask;
        static public TYP_sscGetHWIntOut sscGetHWIntOut;

        static public TYP_sscCheckChannelReady sscCheckChannelReady;

        static public TYP_sscSetSystemCommandBitSignal sscSetSystemCommandBitSignal;
        static public TYP_sscSetAxisCommandBitSignal sscSetAxisCommandBitSignal;
        static public TYP_sscGetSystemStatusBits sscGetSystemStatusBits;
        static public TYP_sscGetAxisStatusBits sscGetAxisStatusBits;
        static public TYP_sscEmgStopOn sscEmgStopOn;
        static public TYP_sscCheckEmgStopOn sscCheckEmgStopOn;
        static public TYP_sscCheckEmgStopOnNoWait sscCheckEmgStopOnNoWait;
        static public TYP_sscEmgStopOff sscEmgStopOff;
        static public TYP_sscCheckEmgStopOff sscCheckEmgStopOff;
        static public TYP_sscCheckEmgStopOffNoWait sscCheckEmgStopOffNoWait;
        static public TYP_sscAsynchronousOn sscAsynchronousOn;
        static public TYP_sscCheckAsynchronousOn sscCheckAsynchronousOn;
        static public TYP_sscCheckAsynchronousOnNoWait sscCheckAsynchronousOnNoWait;
        static public TYP_sscAsynchronousOff sscAsynchronousOff;
        static public TYP_sscCheckAsynchronousOff sscCheckAsynchronousOff;
        static public TYP_sscCheckAsynchronousOffNoWait sscCheckAsynchronousOffNoWait;
        static public TYP_sscCheckSyncMatchExecOn sscCheckSyncMatchExecOn;
        static public TYP_sscCheckSyncMatchExecOnNoWait sscCheckSyncMatchExecOnNoWait;
        static public TYP_sscCheckSyncMatchExecOff sscCheckSyncMatchExecOff;
        static public TYP_sscCheckSyncMatchExecOffNoWait sscCheckSyncMatchExecOffNoWait;
        static public TYP_sscServoOn sscServoOn;
        static public TYP_sscCheckServoOn sscCheckServoOn;
        static public TYP_sscCheckServoOnNoWait sscCheckServoOnNoWait;
        static public TYP_sscServoOff sscServoOff;
        static public TYP_sscCheckServoOff sscCheckServoOff;
        static public TYP_sscCheckServoOffNoWait sscCheckServoOffNoWait;
        static public TYP_sscTorqueLimitOn sscTorqueLimitOn;
        static public TYP_sscCheckTorqueLimitCrampOn sscCheckTorqueLimitCrampOn;
        static public TYP_sscCheckTorqueLimitCrampOnNoWait sscCheckTorqueLimitCrampOnNoWait;
        static public TYP_sscTorqueLimitOff sscTorqueLimitOff;
        static public TYP_sscCheckTorqueLimitCrampOff sscCheckTorqueLimitCrampOff;
        static public TYP_sscCheckTorqueLimitCrampOffNoWait sscCheckTorqueLimitCrampOffNoWait;
        static public TYP_sscCheckPositionSwitchOn sscCheckPositionSwitchOn;
        static public TYP_sscCheckPositionSwitchOnNoWait sscCheckPositionSwitchOnNoWait;
        static public TYP_sscCheckPositionSwitchOff sscCheckPositionSwitchOff;
        static public TYP_sscCheckPositionSwitchOffNoWait sscCheckPositionSwitchOffNoWait;
        static public TYP_sscInterlockOn sscInterlockOn;
        static public TYP_sscCheckInterlockStopOn sscCheckInterlockStopOn;
        static public TYP_sscCheckInterlockStopOnNoWait sscCheckInterlockStopOnNoWait;
        static public TYP_sscInterlockOff sscInterlockOff;
        static public TYP_sscCheckInterlockStopOff sscCheckInterlockStopOff;
        static public TYP_sscCheckInterlockStopOffNoWait sscCheckInterlockStopOffNoWait;
        static public TYP_sscCheckStopPositionOverOn sscCheckStopPositionOverOn;
        static public TYP_sscCheckStopPositionOverOnNoWait sscCheckStopPositionOverOnNoWait;
        static public TYP_sscCheckStopPositionOverOff sscCheckStopPositionOverOff;
        static public TYP_sscCheckStopPositionOverOffNoWait sscCheckStopPositionOverOffNoWait;
        static public TYP_sscGainChangeOn sscGainChangeOn;
        static public TYP_sscCheckGainChangeOn sscCheckGainChangeOn;
        static public TYP_sscCheckGainChangeOnNoWait sscCheckGainChangeOnNoWait;
        static public TYP_sscGainChangeOff sscGainChangeOff;
        static public TYP_sscCheckGainChangeOff sscCheckGainChangeOff;
        static public TYP_sscCheckGainChangeOffNoWait sscCheckGainChangeOffNoWait;
        static public TYP_sscCheckTorqueLimitSelectOn sscCheckTorqueLimitSelectOn;
        static public TYP_sscCheckTorqueLimitSelectOnNoWait sscCheckTorqueLimitSelectOnNoWait;
        static public TYP_sscCheckTorqueLimitSelectOff sscCheckTorqueLimitSelectOff;
        static public TYP_sscCheckTorqueLimitSelectOffNoWait sscCheckTorqueLimitSelectOffNoWait;
        static public TYP_sscCheckInterfareWaitOn sscCheckInterfareWaitOn;
        static public TYP_sscCheckInterfareWaitOnNoWait sscCheckInterfareWaitOnNoWait;
        static public TYP_sscCheckInterfareWaitOff sscCheckInterfareWaitOff;
        static public TYP_sscCheckInterfareWaitOffNoWait sscCheckInterfareWaitOffNoWait;

        static public TYP_sscSetPointData sscSetPointData;
        static public TYP_sscCheckPointData sscCheckPointData;

        static public TYP_sscGetControlAlarmCode sscGetControlAlarmCode;
        static public TYP_sscGetOperationAlarmCode sscGetOperationAlarmCode;
        static public TYP_sscGetExOperationAlarmCode sscGetExOperationAlarmCode;
        static public TYP_sscGetServoAlarmCode sscGetServoAlarmCode;

        static public TYP_sscControlAlarmReset sscControlAlarmReset;
        static public TYP_sscOperationAlarmReset sscOperationAlarmReset;
        static public TYP_sscServoAlarmReset sscServoAlarmReset;

        static public TYP_sscGetEmgStatus sscGetEmgStatus;
        static public TYP_sscGetAxisFixConf sscGetAxisFixConf;


        /*%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%*/
        /* mc2xxFuncInt (compatible)                                                 */
        /*%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%*/

        static public TYP_sscSetIntFactor sscSetIntFactor;
        static public TYP_sscGetIntFactor sscGetIntFactor;

        static public TYP_sscTerminateInt sscTerminateInt;
        static public TYP_sscClearInt sscClearInt;

        static public TYP_sscSetPtpEndType sscSetPtpEndType;
        static public TYP_sscResetEndChk sscResetEndChk;
        static public TYP_sscEndChk sscEndChk;


        /*%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%*/
        /* mc2xxFuncUserDef                                                          */
        /*%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%*/

        public const int SSC_OK = (0);
        public const int SSC_NG = (-1);
        public const int SSC_UNOPEN = (-2);

        public const int SSC_CTRL_CYCLE_ERROR = (0x0000);
        public const int SSC_CTRL_CYCLE_888 = (0x0001);
        public const int SSC_CTRL_CYCLE_444 = (0x0002);
        public const int SSC_CTRL_CYCLE_222 = (0x0003);
        public const int SSC_CTRL_CYCLE_111 = (0x0004);
        public const int SSC_CTRL_CYCLE_1000 = (0x0011);
        public const int SSC_CTRL_CYCLE_0500 = (0x0012);
        public const int SSC_CTRL_CYCLE_0250 = (0x0013);
        public const int SSC_CTRL_CYCLE_0125 = (0x0014);

        public const int SSC_BIT_OCME = (0x01);
        public const int SSC_BIT_OCMW = (0x02);

        public const int SSC_BIT_PWFIN = (0x01);
        public const int SSC_BIT_PWEN = (0x02);
        public const int SSC_BIT_PWED = (0x04);
        public const int SSC_BIT_PRFIN = (0x01);
        public const int SSC_BIT_PREN = (0x02);

        public const int SSC_BIT_OFF = (0);
        public const int SSC_BIT_ON = (1);

        public const int SSC_DIR_PLUS = (0);
        public const int SSC_DIR_MINUS = (1);

        public const int SSC_DRIVING = (0);
        public const int SSC_DRIVE_FIN = (1);

        public const int SSC_DRV_MODE_NONE = (0);
        public const int SSC_DRV_MODE_AUTO = (1);
        public const int SSC_DRV_MODE_HOME = (2);
        public const int SSC_DRV_MODE_JOG = (3);
        public const int SSC_DRV_MODE_INC = (4);
        public const int SSC_DRV_MODE_LINEAR = (6);
        public const int SSC_DRV_MODE_DST = (7);

        public const int SSC_FIN_TYPE_SMZ = (2);
        public const int SSC_FIN_TYPE_CPO = (3);
        public const int SSC_FIN_TYPE_INP = (4);

        public const int SSC_FIN_STS_RDY = (0);
        public const int SSC_FIN_STS_STP = (1);
        public const int SSC_FIN_STS_MOV = (2);
        public const int SSC_FIN_STS_ALM_STP = (3);
        public const int SSC_FIN_STS_ALM_MOV = (4);

        public const int SSC_ALARM_SYSTEM = (0);
        public const int SSC_ALARM_SERVO = (1);
        public const int SSC_ALARM_OPERATION = (2);

        public const int SSC_BIT_LSP = (0x0001);
        public const int SSC_BIT_LSN = (0x0002);
        public const int SSC_BIT_DOG = (0x0004);

        public const int SSC_BIT_OSOP = (0x0001);
        public const int SSC_BIT_OSFIN = (0x0002);
        public const int SSC_BIT_OSERR = (0x0004);

        public const int SSC_BIT_SMPW = (0x01);
        public const int SSC_BIT_SMPO = (0x02);
        public const int SSC_BIT_SMPF = (0x04);
        public const int SSC_BIT_SMPE = (0x08);

        public const int SSC_LOGO_OFF = (0);
        public const int SSC_LOGO_ON = (1);

        public const int SSC_INFINITE = (0);
        public const int SSC_DEFAULT_TIMEOUT = (0);

        public const int SSC_OAS_WAIT_TYPE_NON = (0);
        public const int SSC_OAS_WAIT_TYPE_OP = (1);
        public const int SSC_OAS_WAIT_TYPE_FIN = (2);

        public const int SSC_OAS_STS_NON = (0);
        public const int SSC_OAS_STS_OP = (1);
        public const int SSC_OAS_STS_FIN = (2);
        public const int SSC_OAS_STS_ERR = (3);
        public const int SSC_OAS_STS_OP_ERR = (4);

        public const int SSC_PASS_STS_NON = (0);
        public const int SSC_PASS_STS_FIN = (1);
        public const int SSC_PASS_STS_ERR = (2);

        public const int SMP_DAT_MAX = (32);
        public const int SMP_BIT_MEMSIZE = (1);

        public const int LOG_DAT_MAX = (4);

        public const int CMD_BIT_SYS_MIN = (1);
        public const int CMD_BIT_SYS_MAX = (256);

        public const int STS_BIT_SYS_MIN = (257);
        public const int STS_BIT_SYS_MAX = (512);

        public const int CMD_BIT_AX_MIN = (513);
        public const int CMD_BIT_AX_MAX = (768);

        public const int STS_BIT_AX_MIN = (769);
        public const int STS_BIT_AX_MAX = (1024);

        public const int SSC_INT_SYS_SYSE = (0);
        public const int SSC_INT_SYS_CALM = (1);
        public const int SSC_INT_SYS_02 = (2);
        public const int SSC_INT_SYS_03 = (3);
        public const int SSC_INT_SYS_04 = (4);
        public const int SSC_INT_SYS_05 = (5);
        public const int SSC_INT_SYS_06 = (6);
        public const int SSC_INT_SYS_OCME = (7);
        public const int SSC_INT_SYS_OASF = (8);
        public const int SSC_INT_SYS_PPI = (9);
        public const int SSC_INT_SYS_10 = (10);
        public const int SSC_INT_SYS_11 = (11);
        public const int SSC_INT_SYS_12 = (12);
        public const int SSC_INT_SYS_13 = (13);
        public const int SSC_INT_SYS_14 = (14);
        public const int SSC_INT_SYS_15 = (15);

        public const int SSC_INT_AX_RDY = (0);
        public const int SSC_INT_AX_INP = (1);
        public const int SSC_INT_AX_ZSP = (2);
        public const int SSC_INT_AX_ZPAS = (3);
        public const int SSC_INT_AX_TLC = (4);
        public const int SSC_INT_AX_SALM = (5);
        public const int SSC_INT_AX_SWRN = (6);
        public const int SSC_INT_AX_ABSE = (7);
        public const int SSC_INT_AX_OP = (8);
        public const int SSC_INT_AX_CPO = (9);
        public const int SSC_INT_AX_PF = (10);
        public const int SSC_INT_AX_ZP = (11);
        public const int SSC_INT_AX_SMZ = (12);
        public const int SSC_INT_AX_OALM = (13);
        public const int SSC_INT_AX_OPF = (14);
        public const int SSC_INT_AX_PSW = (15);

        public const int SSC_INT_AX_GAINO = (16);
        public const int SSC_INT_AX_FCLSO = (17);
        public const int SSC_INT_AX_TLSO = (18);
        public const int SSC_INT_AX_SPC = (19);
        public const int SSC_INT_AX_20 = (20);
        public const int SSC_INT_AX_MAK1 = (21);
        public const int SSC_INT_AX_MAK2 = (22);
        public const int SSC_INT_AX_PRSMO = (23);
        public const int SSC_INT_AX_IWT = (24);
        public const int SSC_INT_AX_SINP = (25);
        public const int SSC_INT_AX_26 = (26);
        public const int SSC_INT_AX_27 = (27);
        public const int SSC_INT_AX_28 = (28);
        public const int SSC_INT_AX_29 = (29);
        public const int SSC_INT_AX_30 = (30);
        public const int SSC_INT_AX_31 = (31);

        public const int SSC_EVENT_AX_RDY = (0);
        public const int SSC_EVENT_AX_INP = (1);
        public const int SSC_EVENT_AX_ZSP = (2);
        public const int SSC_EVENT_AX_TLC = (3);
        public const int SSC_EVENT_AX_SALM = (4);
        public const int SSC_EVENT_AX_SWRN = (5);
        public const int SSC_EVENT_AX_ABSE = (6);
        public const int SSC_EVENT_AX_OALM = (7);
        public const int SSC_EVENT_AX_MAK1 = (8);
        public const int SSC_EVENT_AX_MAK2 = (9);
        public const int SSC_EVENT_AX_10 = (10);
        public const int SSC_EVENT_AX_11 = (11);
        public const int SSC_EVENT_AX_12 = (12);
        public const int SSC_EVENT_AX_LSP = (13);
        public const int SSC_EVENT_AX_LSN = (14);
        public const int SSC_EVENT_AX_DOG = (15);

        public const int SSC_STS_CODE_READY_FIN = (0x0001);
        public const int SSC_STS_CODE_RUNNING = (0x000A);

        public const int SSC_SUBCMD_POS_ABS = (0x0000);
        public const int SSC_SUBCMD_POS_INC = (0x0001);
        public const int SSC_SUBCMD_STOP_INP = (0x0000);
        public const int SSC_SUBCMD_STOP_SMZ = (0x0010);
        public const int SSC_SUBCMD_STOP_CONTINUE = (0x0020);
        public const int SSC_SUBCMD_PNT_SWITCH_AFTER = (0x0000);
        public const int SSC_SUBCMD_PNT_SWITCH_BEFORE = (0x0040);
        public const int SSC_SUBCMD_DWELL = (0x0000);
        public const int SSC_SUBCMD_PREDWELL = (0x0080);
        public const int SSC_SUBCMD_PASS_POS_DISABLE = (0x0000);
        public const int SSC_SUBCMD_PASS_POS_ENABLE = (0x0100);
        public const int SSC_SUBCMD_PRESS_DISABLE = (0x0000);
        public const int SSC_SUBCMD_PRESS_ENABLE = (0x0200);

        public const int SSC_PRESS_START_AUTO_CMD = (0x0000);
        public const int SSC_PRESS_START_AUTO_FB = (0x0001);
        public const int SSC_PRESS_START_MANUAL = (0x0002);
        public const int SSC_PRESS_END_AUTO = (0x0000);
        public const int SSC_PRESS_END_MANUAL = (0x0010);

        public const int SSC_CTRL_MODE_POSITION = (0x0000);
        public const int SSC_CTRL_MODE_PRESS = (0x0010);

        public const int SSC_PCIE_DISCONNECT = (0);
        public const int SSC_PCIE_CONNECT = (1);

        public const int SSC_OAS_OWN_RESIDUAL_DISTANCE = (0x00000000);/* compatible */
        public const int SSC_OAS_OWN_REMAINING_DISTANCE = (0x00000000);
        public const int SSC_OAS_OWN_POSITION_PASS = (0x00000001);
        public const int SSC_OAS_OWN_JUDGE_COORD_FB = (0x00000000);
        public const int SSC_OAS_OWN_JUDGE_COORD_CMD = (0x00000010);

        public const int SSC_OAS_OBSERV_DISABLE = (0x00000000);
        public const int SSC_OAS_OBSERV_ENABLE = (0x00000001);
        public const int SSC_OAS_OBSERV_NONE = (0x00000000);
        public const int SSC_OAS_OBSERV_POSITION_PASS = (0x00000010);
        public const int SSC_OAS_OBSERV_JUDGE_COORD_FB = (0x00000000);
        public const int SSC_OAS_OBSERV_JUDGE_COORD_CMD = (0x00000100);
        public const int SSC_OAS_OBSERV_DATA_LESS = (0x00000000);
        public const int SSC_OAS_OBSERV_DATA_MORE = (0x00001000);

        public const int SSC_OAS_DO_DISABLE = (0x00);
        public const int SSC_OAS_DO_ENABLE = (0x01);

        public const int SSC_PASS_DIR_PLUS = (0x00000000);
        public const int SSC_PASS_DIR_MINUS = (0x00000001);
        public const int SSC_PASS_JUDGE_CMD_POS = (0x00000000);
        public const int SSC_PASS_JUDGE_FB_POS = (0x00000010);

        public const int SSC_IFM_CHK_INP_WAIT = (0);
        public const int SSC_IFM_CHK_INP_NOWAIT = (1);
        public const int SSC_IFM_CTRL_MODE_POSITION = (0);
        public const int SSC_IFM_CTRL_MODE_SPEED = (1);
        public const int SSC_IFM_CTRL_MODE_TORQUE = (2);
        public const int SSC_IFM_CTRL_MODE_ERR_OFF = (0);
        public const int SSC_IFM_CTRL_MODE_ERR_ON = (1);

        /*----------------------------------------------------------------------*/
        /* constant number for user application                                 */
        /*----------------------------------------------------------------------*/

        /*------------------------------------------*/
        /* system command bits                      */
        /*------------------------------------------*/
        public const int SSC_CMDBIT_SYS_ITE = (CMD_BIT_SYS_MIN + 0);
        public const int SSC_CMDBIT_SYS_ITS = (CMD_BIT_SYS_MIN + 1);
        public const int SSC_CMDBIT_SYS_03 = (CMD_BIT_SYS_MIN + 2);
        public const int SSC_CMDBIT_SYS_04 = (CMD_BIT_SYS_MIN + 3);
        public const int SSC_CMDBIT_SYS_HMA = (CMD_BIT_SYS_MIN + 4);
        public const int SSC_CMDBIT_SYS_06 = (CMD_BIT_SYS_MIN + 5);
        public const int SSC_CMDBIT_SYS_07 = (CMD_BIT_SYS_MIN + 6);
        public const int SSC_CMDBIT_SYS_08 = (CMD_BIT_SYS_MIN + 7);

        public const int SSC_CMDBIT_SYS_SMPS = (CMD_BIT_SYS_MIN + 8);
        public const int SSC_CMDBIT_SYS_10 = (CMD_BIT_SYS_MIN + 9);
        public const int SSC_CMDBIT_SYS_11 = (CMD_BIT_SYS_MIN + 10);
        public const int SSC_CMDBIT_SYS_12 = (CMD_BIT_SYS_MIN + 11);
        public const int SSC_CMDBIT_SYS_13 = (CMD_BIT_SYS_MIN + 12);
        public const int SSC_CMDBIT_SYS_14 = (CMD_BIT_SYS_MIN + 13);
        public const int SSC_CMDBIT_SYS_15 = (CMD_BIT_SYS_MIN + 14);
        public const int SSC_CMDBIT_SYS_16 = (CMD_BIT_SYS_MIN + 15);

        public const int SSC_CMDBIT_SYS_SEMI = (CMD_BIT_SYS_MIN + 16);
        public const int SSC_CMDBIT_SYS_18 = (CMD_BIT_SYS_MIN + 17);
        public const int SSC_CMDBIT_SYS_19 = (CMD_BIT_SYS_MIN + 18);
        public const int SSC_CMDBIT_SYS_20 = (CMD_BIT_SYS_MIN + 19);
        public const int SSC_CMDBIT_SYS_21 = (CMD_BIT_SYS_MIN + 20);
        public const int SSC_CMDBIT_SYS_22 = (CMD_BIT_SYS_MIN + 21);
        public const int SSC_CMDBIT_SYS_23 = (CMD_BIT_SYS_MIN + 22);
        public const int SSC_CMDBIT_SYS_24 = (CMD_BIT_SYS_MIN + 23);

        public const int SSC_CMDBIT_SYS_25 = (CMD_BIT_SYS_MIN + 24);
        public const int SSC_CMDBIT_SYS_26 = (CMD_BIT_SYS_MIN + 25);
        public const int SSC_CMDBIT_SYS_27 = (CMD_BIT_SYS_MIN + 26);
        public const int SSC_CMDBIT_SYS_28 = (CMD_BIT_SYS_MIN + 27);
        public const int SSC_CMDBIT_SYS_29 = (CMD_BIT_SYS_MIN + 28);
        public const int SSC_CMDBIT_SYS_30 = (CMD_BIT_SYS_MIN + 29);
        public const int SSC_CMDBIT_SYS_31 = (CMD_BIT_SYS_MIN + 30);
        public const int SSC_CMDBIT_SYS_32 = (CMD_BIT_SYS_MIN + 31);

        public const int SSC_CMDBIT_SYS_ITFE = (CMD_BIT_SYS_MIN + 32);
        public const int SSC_CMDBIT_SYS_34 = (CMD_BIT_SYS_MIN + 33);
        public const int SSC_CMDBIT_SYS_35 = (CMD_BIT_SYS_MIN + 34);
        public const int SSC_CMDBIT_SYS_36 = (CMD_BIT_SYS_MIN + 35);
        public const int SSC_CMDBIT_SYS_37 = (CMD_BIT_SYS_MIN + 36);
        public const int SSC_CMDBIT_SYS_38 = (CMD_BIT_SYS_MIN + 37);
        public const int SSC_CMDBIT_SYS_39 = (CMD_BIT_SYS_MIN + 38);
        public const int SSC_CMDBIT_SYS_40 = (CMD_BIT_SYS_MIN + 39);

        public const int SSC_CMDBIT_SYS_41 = (CMD_BIT_SYS_MIN + 40);
        public const int SSC_CMDBIT_SYS_42 = (CMD_BIT_SYS_MIN + 41);
        public const int SSC_CMDBIT_SYS_43 = (CMD_BIT_SYS_MIN + 42);
        public const int SSC_CMDBIT_SYS_44 = (CMD_BIT_SYS_MIN + 43);
        public const int SSC_CMDBIT_SYS_45 = (CMD_BIT_SYS_MIN + 44);
        public const int SSC_CMDBIT_SYS_46 = (CMD_BIT_SYS_MIN + 45);
        public const int SSC_CMDBIT_SYS_47 = (CMD_BIT_SYS_MIN + 46);
        public const int SSC_CMDBIT_SYS_48 = (CMD_BIT_SYS_MIN + 47);

        public const int SSC_CMDBIT_SYS_ASYN1 = (CMD_BIT_SYS_MIN + 48);
        public const int SSC_CMDBIT_SYS_ASYN2 = (CMD_BIT_SYS_MIN + 49);
        public const int SSC_CMDBIT_SYS_ASYN3 = (CMD_BIT_SYS_MIN + 50);
        public const int SSC_CMDBIT_SYS_ASYN4 = (CMD_BIT_SYS_MIN + 51);
        public const int SSC_CMDBIT_SYS_ASYN5 = (CMD_BIT_SYS_MIN + 52);
        public const int SSC_CMDBIT_SYS_ASYN6 = (CMD_BIT_SYS_MIN + 53);
        public const int SSC_CMDBIT_SYS_ASYN7 = (CMD_BIT_SYS_MIN + 54);
        public const int SSC_CMDBIT_SYS_ASYN8 = (CMD_BIT_SYS_MIN + 55);

        public const int SSC_CMDBIT_SYS_57 = (CMD_BIT_SYS_MIN + 56);
        public const int SSC_CMDBIT_SYS_58 = (CMD_BIT_SYS_MIN + 57);
        public const int SSC_CMDBIT_SYS_59 = (CMD_BIT_SYS_MIN + 58);
        public const int SSC_CMDBIT_SYS_60 = (CMD_BIT_SYS_MIN + 59);
        public const int SSC_CMDBIT_SYS_61 = (CMD_BIT_SYS_MIN + 60);
        public const int SSC_CMDBIT_SYS_62 = (CMD_BIT_SYS_MIN + 61);
        public const int SSC_CMDBIT_SYS_63 = (CMD_BIT_SYS_MIN + 62);
        public const int SSC_CMDBIT_SYS_64 = (CMD_BIT_SYS_MIN + 63);

        public const int SSC_CMDBIT_SYS_RBR = (CMD_BIT_SYS_MIN + 64);
        public const int SSC_CMDBIT_SYS_RBS = (CMD_BIT_SYS_MIN + 65);
        public const int SSC_CMDBIT_SYS_CRST = (CMD_BIT_SYS_MIN + 66);
        public const int SSC_CMDBIT_SYS_68 = (CMD_BIT_SYS_MIN + 67);
        public const int SSC_CMDBIT_SYS_SMON = (CMD_BIT_SYS_MIN + 68);
        public const int SSC_CMDBIT_SYS_SMONR = (CMD_BIT_SYS_MIN + 69);
        public const int SSC_CMDBIT_SYS_71 = (CMD_BIT_SYS_MIN + 70);
        public const int SSC_CMDBIT_SYS_72 = (CMD_BIT_SYS_MIN + 71);

        public const int SSC_CMDBIT_SYS_73 = (CMD_BIT_SYS_MIN + 72);
        public const int SSC_CMDBIT_SYS_74 = (CMD_BIT_SYS_MIN + 73);
        public const int SSC_CMDBIT_SYS_75 = (CMD_BIT_SYS_MIN + 74);
        public const int SSC_CMDBIT_SYS_76 = (CMD_BIT_SYS_MIN + 75);
        public const int SSC_CMDBIT_SYS_77 = (CMD_BIT_SYS_MIN + 76);
        public const int SSC_CMDBIT_SYS_78 = (CMD_BIT_SYS_MIN + 77);
        public const int SSC_CMDBIT_SYS_79 = (CMD_BIT_SYS_MIN + 78);
        public const int SSC_CMDBIT_SYS_80 = (CMD_BIT_SYS_MIN + 79);

        public const int SSC_CMDBIT_SYS_LOGC = (CMD_BIT_SYS_MIN + 80);
        public const int SSC_CMDBIT_SYS_LOGR = (CMD_BIT_SYS_MIN + 81);
        public const int SSC_CMDBIT_SYS_83 = (CMD_BIT_SYS_MIN + 82);
        public const int SSC_CMDBIT_SYS_LOGI = (CMD_BIT_SYS_MIN + 83);
        public const int SSC_CMDBIT_SYS_85 = (CMD_BIT_SYS_MIN + 84);
        public const int SSC_CMDBIT_SYS_OCMC = (CMD_BIT_SYS_MIN + 85);
        public const int SSC_CMDBIT_SYS87 = (CMD_BIT_SYS_MIN + 86);
        public const int SSC_CMDBIT_SYS88 = (CMD_BIT_SYS_MIN + 87);

        public const int SSC_CMDBIT_SYS_RCC = (CMD_BIT_SYS_MIN + 88);
        public const int SSC_CMDBIT_SYS_90 = (CMD_BIT_SYS_MIN + 89);
        public const int SSC_CMDBIT_SYS_91 = (CMD_BIT_SYS_MIN + 90);
        public const int SSC_CMDBIT_SYS_CCC = (CMD_BIT_SYS_MIN + 91);
        public const int SSC_CMDBIT_SYS_93 = (CMD_BIT_SYS_MIN + 92);
        public const int SSC_CMDBIT_SYS_94 = (CMD_BIT_SYS_MIN + 93);
        public const int SSC_CMDBIT_SYS_95 = (CMD_BIT_SYS_MIN + 94);
        public const int SSC_CMDBIT_SYS_96 = (CMD_BIT_SYS_MIN + 95);

        public const int SSC_CMDBIT_SYS_97 = (CMD_BIT_SYS_MIN + 96);
        public const int SSC_CMDBIT_SYS_98 = (CMD_BIT_SYS_MIN + 97);
        public const int SSC_CMDBIT_SYS_99 = (CMD_BIT_SYS_MIN + 98);
        public const int SSC_CMDBIT_SYS_100 = (CMD_BIT_SYS_MIN + 99);
        public const int SSC_CMDBIT_SYS_101 = (CMD_BIT_SYS_MIN + 100);
        public const int SSC_CMDBIT_SYS_102 = (CMD_BIT_SYS_MIN + 101);
        public const int SSC_CMDBIT_SYS_103 = (CMD_BIT_SYS_MIN + 102);
        public const int SSC_CMDBIT_SYS_104 = (CMD_BIT_SYS_MIN + 103);

        public const int SSC_CMDBIT_SYS_105 = (CMD_BIT_SYS_MIN + 104);
        public const int SSC_CMDBIT_SYS_106 = (CMD_BIT_SYS_MIN + 105);
        public const int SSC_CMDBIT_SYS_107 = (CMD_BIT_SYS_MIN + 106);
        public const int SSC_CMDBIT_SYS_108 = (CMD_BIT_SYS_MIN + 107);
        public const int SSC_CMDBIT_SYS_109 = (CMD_BIT_SYS_MIN + 108);
        public const int SSC_CMDBIT_SYS_110 = (CMD_BIT_SYS_MIN + 109);
        public const int SSC_CMDBIT_SYS_111 = (CMD_BIT_SYS_MIN + 110);
        public const int SSC_CMDBIT_SYS_112 = (CMD_BIT_SYS_MIN + 111);

        public const int SSC_CMDBIT_SYS_113 = (CMD_BIT_SYS_MIN + 112);
        public const int SSC_CMDBIT_SYS_114 = (CMD_BIT_SYS_MIN + 113);
        public const int SSC_CMDBIT_SYS_115 = (CMD_BIT_SYS_MIN + 114);
        public const int SSC_CMDBIT_SYS_116 = (CMD_BIT_SYS_MIN + 115);
        public const int SSC_CMDBIT_SYS_117 = (CMD_BIT_SYS_MIN + 116);
        public const int SSC_CMDBIT_SYS_118 = (CMD_BIT_SYS_MIN + 117);
        public const int SSC_CMDBIT_SYS_119 = (CMD_BIT_SYS_MIN + 118);
        public const int SSC_CMDBIT_SYS_120 = (CMD_BIT_SYS_MIN + 119);

        public const int SSC_CMDBIT_SYS_121 = (CMD_BIT_SYS_MIN + 120);
        public const int SSC_CMDBIT_SYS_122 = (CMD_BIT_SYS_MIN + 121);
        public const int SSC_CMDBIT_SYS_123 = (CMD_BIT_SYS_MIN + 122);
        public const int SSC_CMDBIT_SYS_124 = (CMD_BIT_SYS_MIN + 123);
        public const int SSC_CMDBIT_SYS_125 = (CMD_BIT_SYS_MIN + 124);
        public const int SSC_CMDBIT_SYS_126 = (CMD_BIT_SYS_MIN + 125);
        public const int SSC_CMDBIT_SYS_127 = (CMD_BIT_SYS_MIN + 126);
        public const int SSC_CMDBIT_SYS_128 = (CMD_BIT_SYS_MIN + 127);

        public const int SSC_CMDBIT_SYS_SPWRT = (CMD_BIT_SYS_MIN + 128);
        public const int SSC_CMDBIT_SYS_130 = (CMD_BIT_SYS_MIN + 129);
        public const int SSC_CMDBIT_SYS_131 = (CMD_BIT_SYS_MIN + 130);
        public const int SSC_CMDBIT_SYS_132 = (CMD_BIT_SYS_MIN + 131);
        public const int SSC_CMDBIT_SYS_133 = (CMD_BIT_SYS_MIN + 132);
        public const int SSC_CMDBIT_SYS_134 = (CMD_BIT_SYS_MIN + 133);
        public const int SSC_CMDBIT_SYS_135 = (CMD_BIT_SYS_MIN + 134);
        public const int SSC_CMDBIT_SYS_136 = (CMD_BIT_SYS_MIN + 135);

        public const int SSC_CMDBIT_SYS_SPRD = (CMD_BIT_SYS_MIN + 136);
        public const int SSC_CMDBIT_SYS_138 = (CMD_BIT_SYS_MIN + 137);
        public const int SSC_CMDBIT_SYS_139 = (CMD_BIT_SYS_MIN + 138);
        public const int SSC_CMDBIT_SYS_140 = (CMD_BIT_SYS_MIN + 139);
        public const int SSC_CMDBIT_SYS_141 = (CMD_BIT_SYS_MIN + 140);
        public const int SSC_CMDBIT_SYS_142 = (CMD_BIT_SYS_MIN + 141);
        public const int SSC_CMDBIT_SYS_143 = (CMD_BIT_SYS_MIN + 142);
        public const int SSC_CMDBIT_SYS_144 = (CMD_BIT_SYS_MIN + 143);

        public const int SSC_CMDBIT_SYS_SMPSW = (CMD_BIT_SYS_MIN + 144);
        public const int SSC_CMDBIT_SYS_146 = (CMD_BIT_SYS_MIN + 145);
        public const int SSC_CMDBIT_SYS_147 = (CMD_BIT_SYS_MIN + 146);
        public const int SSC_CMDBIT_SYS_148 = (CMD_BIT_SYS_MIN + 147);
        public const int SSC_CMDBIT_SYS_SMPSR = (CMD_BIT_SYS_MIN + 148);
        public const int SSC_CMDBIT_SYS_150 = (CMD_BIT_SYS_MIN + 149);
        public const int SSC_CMDBIT_SYS_151 = (CMD_BIT_SYS_MIN + 150);
        public const int SSC_CMDBIT_SYS_152 = (CMD_BIT_SYS_MIN + 151);

        public const int SSC_CMDBIT_SYS_153 = (CMD_BIT_SYS_MIN + 152);
        public const int SSC_CMDBIT_SYS_154 = (CMD_BIT_SYS_MIN + 153);
        public const int SSC_CMDBIT_SYS_155 = (CMD_BIT_SYS_MIN + 154);
        public const int SSC_CMDBIT_SYS_156 = (CMD_BIT_SYS_MIN + 155);
        public const int SSC_CMDBIT_SYS_157 = (CMD_BIT_SYS_MIN + 156);
        public const int SSC_CMDBIT_SYS_158 = (CMD_BIT_SYS_MIN + 157);
        public const int SSC_CMDBIT_SYS_159 = (CMD_BIT_SYS_MIN + 158);
        public const int SSC_CMDBIT_SYS_160 = (CMD_BIT_SYS_MIN + 159);

        public const int SSC_CMDBIT_SYS_161 = (CMD_BIT_SYS_MIN + 160);
        public const int SSC_CMDBIT_SYS_162 = (CMD_BIT_SYS_MIN + 161);
        public const int SSC_CMDBIT_SYS_163 = (CMD_BIT_SYS_MIN + 162);
        public const int SSC_CMDBIT_SYS_164 = (CMD_BIT_SYS_MIN + 163);
        public const int SSC_CMDBIT_SYS_165 = (CMD_BIT_SYS_MIN + 164);
        public const int SSC_CMDBIT_SYS_166 = (CMD_BIT_SYS_MIN + 165);
        public const int SSC_CMDBIT_SYS_167 = (CMD_BIT_SYS_MIN + 166);
        public const int SSC_CMDBIT_SYS_168 = (CMD_BIT_SYS_MIN + 167);

        public const int SSC_CMDBIT_SYS_169 = (CMD_BIT_SYS_MIN + 168);
        public const int SSC_CMDBIT_SYS_170 = (CMD_BIT_SYS_MIN + 169);
        public const int SSC_CMDBIT_SYS_171 = (CMD_BIT_SYS_MIN + 170);
        public const int SSC_CMDBIT_SYS_172 = (CMD_BIT_SYS_MIN + 171);
        public const int SSC_CMDBIT_SYS_173 = (CMD_BIT_SYS_MIN + 172);
        public const int SSC_CMDBIT_SYS_174 = (CMD_BIT_SYS_MIN + 173);
        public const int SSC_CMDBIT_SYS_175 = (CMD_BIT_SYS_MIN + 174);
        public const int SSC_CMDBIT_SYS_176 = (CMD_BIT_SYS_MIN + 175);

        public const int SSC_CMDBIT_SYS_FTR = (CMD_BIT_SYS_MIN + 176);
        public const int SSC_CMDBIT_SYS_FTS = (CMD_BIT_SYS_MIN + 177);
        public const int SSC_CMDBIT_SYS_179 = (CMD_BIT_SYS_MIN + 178);
        public const int SSC_CMDBIT_SYS_180 = (CMD_BIT_SYS_MIN + 179);
        public const int SSC_CMDBIT_SYS_FIR = (CMD_BIT_SYS_MIN + 180);
        public const int SSC_CMDBIT_SYS_FIS = (CMD_BIT_SYS_MIN + 181);
        public const int SSC_CMDBIT_SYS_183 = (CMD_BIT_SYS_MIN + 182);
        public const int SSC_CMDBIT_SYS_184 = (CMD_BIT_SYS_MIN + 183);

        public const int SSC_CMDBIT_SYS_ALHR = (CMD_BIT_SYS_MIN + 184);
        public const int SSC_CMDBIT_SYS_186 = (CMD_BIT_SYS_MIN + 185);
        public const int SSC_CMDBIT_SYS_ALHI = (CMD_BIT_SYS_MIN + 186);
        public const int SSC_CMDBIT_SYS_188 = (CMD_BIT_SYS_MIN + 187);
        public const int SSC_CMDBIT_SYS_189 = (CMD_BIT_SYS_MIN + 188);
        public const int SSC_CMDBIT_SYS_190 = (CMD_BIT_SYS_MIN + 189);
        public const int SSC_CMDBIT_SYS_191 = (CMD_BIT_SYS_MIN + 190);
        public const int SSC_CMDBIT_SYS_192 = (CMD_BIT_SYS_MIN + 191);

        public const int SSC_CMDBIT_SYS_193 = (CMD_BIT_SYS_MIN + 192);
        public const int SSC_CMDBIT_SYS_194 = (CMD_BIT_SYS_MIN + 193);
        public const int SSC_CMDBIT_SYS_195 = (CMD_BIT_SYS_MIN + 194);
        public const int SSC_CMDBIT_SYS_196 = (CMD_BIT_SYS_MIN + 195);
        public const int SSC_CMDBIT_SYS_197 = (CMD_BIT_SYS_MIN + 196);
        public const int SSC_CMDBIT_SYS_198 = (CMD_BIT_SYS_MIN + 197);
        public const int SSC_CMDBIT_SYS_199 = (CMD_BIT_SYS_MIN + 198);
        public const int SSC_CMDBIT_SYS_200 = (CMD_BIT_SYS_MIN + 199);

        public const int SSC_CMDBIT_SYS_201 = (CMD_BIT_SYS_MIN + 200);
        public const int SSC_CMDBIT_SYS_202 = (CMD_BIT_SYS_MIN + 201);
        public const int SSC_CMDBIT_SYS_203 = (CMD_BIT_SYS_MIN + 202);
        public const int SSC_CMDBIT_SYS_204 = (CMD_BIT_SYS_MIN + 203);
        public const int SSC_CMDBIT_SYS_205 = (CMD_BIT_SYS_MIN + 204);
        public const int SSC_CMDBIT_SYS_206 = (CMD_BIT_SYS_MIN + 205);
        public const int SSC_CMDBIT_SYS_207 = (CMD_BIT_SYS_MIN + 206);
        public const int SSC_CMDBIT_SYS_208 = (CMD_BIT_SYS_MIN + 207);

        public const int SSC_CMDBIT_SYS_209 = (CMD_BIT_SYS_MIN + 208);
        public const int SSC_CMDBIT_SYS_210 = (CMD_BIT_SYS_MIN + 209);
        public const int SSC_CMDBIT_SYS_211 = (CMD_BIT_SYS_MIN + 210);
        public const int SSC_CMDBIT_SYS_212 = (CMD_BIT_SYS_MIN + 211);
        public const int SSC_CMDBIT_SYS_213 = (CMD_BIT_SYS_MIN + 212);
        public const int SSC_CMDBIT_SYS_214 = (CMD_BIT_SYS_MIN + 213);
        public const int SSC_CMDBIT_SYS_215 = (CMD_BIT_SYS_MIN + 214);
        public const int SSC_CMDBIT_SYS_216 = (CMD_BIT_SYS_MIN + 215);

        public const int SSC_CMDBIT_SYS_217 = (CMD_BIT_SYS_MIN + 216);
        public const int SSC_CMDBIT_SYS_218 = (CMD_BIT_SYS_MIN + 217);
        public const int SSC_CMDBIT_SYS_219 = (CMD_BIT_SYS_MIN + 218);
        public const int SSC_CMDBIT_SYS_220 = (CMD_BIT_SYS_MIN + 219);
        public const int SSC_CMDBIT_SYS_221 = (CMD_BIT_SYS_MIN + 220);
        public const int SSC_CMDBIT_SYS_222 = (CMD_BIT_SYS_MIN + 221);
        public const int SSC_CMDBIT_SYS_223 = (CMD_BIT_SYS_MIN + 222);
        public const int SSC_CMDBIT_SYS_224 = (CMD_BIT_SYS_MIN + 223);

        public const int SSC_CMDBIT_SYS_225 = (CMD_BIT_SYS_MIN + 224);
        public const int SSC_CMDBIT_SYS_226 = (CMD_BIT_SYS_MIN + 225);
        public const int SSC_CMDBIT_SYS_227 = (CMD_BIT_SYS_MIN + 226);
        public const int SSC_CMDBIT_SYS_228 = (CMD_BIT_SYS_MIN + 227);
        public const int SSC_CMDBIT_SYS_229 = (CMD_BIT_SYS_MIN + 228);
        public const int SSC_CMDBIT_SYS_230 = (CMD_BIT_SYS_MIN + 229);
        public const int SSC_CMDBIT_SYS_231 = (CMD_BIT_SYS_MIN + 230);
        public const int SSC_CMDBIT_SYS_232 = (CMD_BIT_SYS_MIN + 231);

        public const int SSC_CMDBIT_SYS_233 = (CMD_BIT_SYS_MIN + 232);
        public const int SSC_CMDBIT_SYS_234 = (CMD_BIT_SYS_MIN + 233);
        public const int SSC_CMDBIT_SYS_235 = (CMD_BIT_SYS_MIN + 234);
        public const int SSC_CMDBIT_SYS_236 = (CMD_BIT_SYS_MIN + 235);
        public const int SSC_CMDBIT_SYS_237 = (CMD_BIT_SYS_MIN + 236);
        public const int SSC_CMDBIT_SYS_238 = (CMD_BIT_SYS_MIN + 237);
        public const int SSC_CMDBIT_SYS_239 = (CMD_BIT_SYS_MIN + 238);
        public const int SSC_CMDBIT_SYS_240 = (CMD_BIT_SYS_MIN + 239);

        public const int SSC_CMDBIT_SYS_241 = (CMD_BIT_SYS_MIN + 240);
        public const int SSC_CMDBIT_SYS_242 = (CMD_BIT_SYS_MIN + 241);
        public const int SSC_CMDBIT_SYS_243 = (CMD_BIT_SYS_MIN + 242);
        public const int SSC_CMDBIT_SYS_244 = (CMD_BIT_SYS_MIN + 243);
        public const int SSC_CMDBIT_SYS_245 = (CMD_BIT_SYS_MIN + 244);
        public const int SSC_CMDBIT_SYS_246 = (CMD_BIT_SYS_MIN + 245);
        public const int SSC_CMDBIT_SYS_247 = (CMD_BIT_SYS_MIN + 246);
        public const int SSC_CMDBIT_SYS_248 = (CMD_BIT_SYS_MIN + 247);

        public const int SSC_CMDBIT_SYS_249 = (CMD_BIT_SYS_MIN + 248);
        public const int SSC_CMDBIT_SYS_250 = (CMD_BIT_SYS_MIN + 249);
        public const int SSC_CMDBIT_SYS_251 = (CMD_BIT_SYS_MIN + 250);
        public const int SSC_CMDBIT_SYS_252 = (CMD_BIT_SYS_MIN + 251);
        public const int SSC_CMDBIT_SYS_253 = (CMD_BIT_SYS_MIN + 252);
        public const int SSC_CMDBIT_SYS_254 = (CMD_BIT_SYS_MIN + 253);
        public const int SSC_CMDBIT_SYS_255 = (CMD_BIT_SYS_MIN + 254);
        public const int SSC_CMDBIT_SYS_256 = (CMD_BIT_SYS_MIN + 255);


        /*------------------------------------------*/
        /* system status bits                       */
        /*------------------------------------------*/
        public const int SSC_STSBIT_SYS_ITO = (STS_BIT_SYS_MIN + 0);
        public const int SSC_STSBIT_SYS_IITO = (STS_BIT_SYS_MIN + 1);
        public const int SSC_STSBIT_SYS_03 = (STS_BIT_SYS_MIN + 2);
        public const int SSC_STSBIT_SYS_HRIF = (STS_BIT_SYS_MIN + 3);
        public const int SSC_STSBIT_SYS_BMA = (STS_BIT_SYS_MIN + 4);
        public const int SSC_STSBIT_SYS_PRINF = (STS_BIT_SYS_MIN + 5);
        public const int SSC_STSBIT_SYS_07 = (STS_BIT_SYS_MIN + 6);
        public const int SSC_STSBIT_SYS_IFMO = (STS_BIT_SYS_MIN + 7);

        public const int SSC_STSBIT_SYS_SMPW = (STS_BIT_SYS_MIN + 8);
        public const int SSC_STSBIT_SYS_SMPO = (STS_BIT_SYS_MIN + 9);
        public const int SSC_STSBIT_SYS_SMPF = (STS_BIT_SYS_MIN + 10);
        public const int SSC_STSBIT_SYS_SMPE = (STS_BIT_SYS_MIN + 11);
        public const int SSC_STSBIT_SYS_13 = (STS_BIT_SYS_MIN + 12);
        public const int SSC_STSBIT_SYS_AHINF = (STS_BIT_SYS_MIN + 13);
        public const int SSC_STSBIT_SYS_15 = (STS_BIT_SYS_MIN + 14);
        public const int SSC_STSBIT_SYS_16 = (STS_BIT_SYS_MIN + 15);

        public const int SSC_STSBIT_SYS_EMIO = (STS_BIT_SYS_MIN + 16);
        public const int SSC_STSBIT_SYS_18 = (STS_BIT_SYS_MIN + 17);
        public const int SSC_STSBIT_SYS_TSTO = (STS_BIT_SYS_MIN + 18);
        public const int SSC_STSBIT_SYS_20 = (STS_BIT_SYS_MIN + 19);
        public const int SSC_STSBIT_SYS_21 = (STS_BIT_SYS_MIN + 20);
        public const int SSC_STSBIT_SYS_22 = (STS_BIT_SYS_MIN + 21);
        public const int SSC_STSBIT_SYS_EMID = (STS_BIT_SYS_MIN + 22);
        public const int SSC_STSBIT_SYS_24 = (STS_BIT_SYS_MIN + 23);

        public const int SSC_STSBIT_SYS_25 = (STS_BIT_SYS_MIN + 24);
        public const int SSC_STSBIT_SYS_26 = (STS_BIT_SYS_MIN + 25);
        public const int SSC_STSBIT_SYS_27 = (STS_BIT_SYS_MIN + 26);
        public const int SSC_STSBIT_SYS_28 = (STS_BIT_SYS_MIN + 27);
        public const int SSC_STSBIT_SYS_29 = (STS_BIT_SYS_MIN + 28);
        public const int SSC_STSBIT_SYS_30 = (STS_BIT_SYS_MIN + 29);
        public const int SSC_STSBIT_SYS_31 = (STS_BIT_SYS_MIN + 30);
        public const int SSC_STSBIT_SYS_32 = (STS_BIT_SYS_MIN + 31);

        public const int SSC_STSBIT_SYS_33 = (STS_BIT_SYS_MIN + 32);
        public const int SSC_STSBIT_SYS_34 = (STS_BIT_SYS_MIN + 33);
        public const int SSC_STSBIT_SYS_35 = (STS_BIT_SYS_MIN + 34);
        public const int SSC_STSBIT_SYS_36 = (STS_BIT_SYS_MIN + 35);
        public const int SSC_STSBIT_SYS_37 = (STS_BIT_SYS_MIN + 36);
        public const int SSC_STSBIT_SYS_38 = (STS_BIT_SYS_MIN + 37);
        public const int SSC_STSBIT_SYS_39 = (STS_BIT_SYS_MIN + 38);
        public const int SSC_STSBIT_SYS_40 = (STS_BIT_SYS_MIN + 39);

        public const int SSC_STSBIT_SYS_41 = (STS_BIT_SYS_MIN + 40);
        public const int SSC_STSBIT_SYS_42 = (STS_BIT_SYS_MIN + 41);
        public const int SSC_STSBIT_SYS_43 = (STS_BIT_SYS_MIN + 42);
        public const int SSC_STSBIT_SYS_44 = (STS_BIT_SYS_MIN + 43);
        public const int SSC_STSBIT_SYS_45 = (STS_BIT_SYS_MIN + 44);
        public const int SSC_STSBIT_SYS_46 = (STS_BIT_SYS_MIN + 45);
        public const int SSC_STSBIT_SYS_47 = (STS_BIT_SYS_MIN + 46);
        public const int SSC_STSBIT_SYS_48 = (STS_BIT_SYS_MIN + 47);

        public const int SSC_STSBIT_SYS_ASYO1 = (STS_BIT_SYS_MIN + 48);
        public const int SSC_STSBIT_SYS_ASYO2 = (STS_BIT_SYS_MIN + 49);
        public const int SSC_STSBIT_SYS_ASYO3 = (STS_BIT_SYS_MIN + 50);
        public const int SSC_STSBIT_SYS_ASYO4 = (STS_BIT_SYS_MIN + 51);
        public const int SSC_STSBIT_SYS_ASYO5 = (STS_BIT_SYS_MIN + 52);
        public const int SSC_STSBIT_SYS_ASYO6 = (STS_BIT_SYS_MIN + 53);
        public const int SSC_STSBIT_SYS_ASYO7 = (STS_BIT_SYS_MIN + 54);
        public const int SSC_STSBIT_SYS_ASYO8 = (STS_BIT_SYS_MIN + 55);

        public const int SSC_STSBIT_SYS_SYEO1 = (STS_BIT_SYS_MIN + 56);
        public const int SSC_STSBIT_SYS_SYEO2 = (STS_BIT_SYS_MIN + 57);
        public const int SSC_STSBIT_SYS_SYEO3 = (STS_BIT_SYS_MIN + 58);
        public const int SSC_STSBIT_SYS_SYEO4 = (STS_BIT_SYS_MIN + 59);
        public const int SSC_STSBIT_SYS_SYEO5 = (STS_BIT_SYS_MIN + 60);
        public const int SSC_STSBIT_SYS_SYEO6 = (STS_BIT_SYS_MIN + 61);
        public const int SSC_STSBIT_SYS_SYEO7 = (STS_BIT_SYS_MIN + 62);
        public const int SSC_STSBIT_SYS_SYEO8 = (STS_BIT_SYS_MIN + 63);

        public const int SSC_STSBIT_SYS_RBOK = (STS_BIT_SYS_MIN + 64);
        public const int SSC_STSBIT_SYS_RBNG = (STS_BIT_SYS_MIN + 65);
        public const int SSC_STSBIT_SYS_CALM = (STS_BIT_SYS_MIN + 66);
        public const int SSC_STSBIT_SYS_68 = (STS_BIT_SYS_MIN + 67);
        public const int SSC_STSBIT_SYS_SMOUT = (STS_BIT_SYS_MIN + 68);
        public const int SSC_STSBIT_SYS_SMRCH = (STS_BIT_SYS_MIN + 69);
        public const int SSC_STSBIT_SYS_SMER1 = (STS_BIT_SYS_MIN + 70);
        public const int SSC_STSBIT_SYS_SMER2 = (STS_BIT_SYS_MIN + 71);

        public const int SSC_STSBIT_SYS_73 = (STS_BIT_SYS_MIN + 72);
        public const int SSC_STSBIT_SYS_74 = (STS_BIT_SYS_MIN + 73);
        public const int SSC_STSBIT_SYS_75 = (STS_BIT_SYS_MIN + 74);
        public const int SSC_STSBIT_SYS_76 = (STS_BIT_SYS_MIN + 75);
        public const int SSC_STSBIT_SYS_77 = (STS_BIT_SYS_MIN + 76);
        public const int SSC_STSBIT_SYS_78 = (STS_BIT_SYS_MIN + 77);
        public const int SSC_STSBIT_SYS_79 = (STS_BIT_SYS_MIN + 78);
        public const int SSC_STSBIT_SYS_80 = (STS_BIT_SYS_MIN + 79);

        public const int SSC_STSBIT_SYS_LOGO = (STS_BIT_SYS_MIN + 80);
        public const int SSC_STSBIT_SYS_LOGRF = (STS_BIT_SYS_MIN + 81);
        public const int SSC_STSBIT_SYS_LOGRE = (STS_BIT_SYS_MIN + 82);
        public const int SSC_STSBIT_SYS_LOGIF = (STS_BIT_SYS_MIN + 83);
        public const int SSC_STSBIT_SYS_LOGIE = (STS_BIT_SYS_MIN + 84);
        public const int SSC_STSBIT_SYS_OCMCO = (STS_BIT_SYS_MIN + 85);
        public const int SSC_STSBIT_SYS_OCME = (STS_BIT_SYS_MIN + 86);
        public const int SSC_STSBIT_SYS_OCMW = (STS_BIT_SYS_MIN + 87);

        public const int SSC_STSBIT_SYS_RCO = (STS_BIT_SYS_MIN + 88);
        public const int SSC_STSBIT_SYS_RCF = (STS_BIT_SYS_MIN + 89);
        public const int SSC_STSBIT_SYS_RCE = (STS_BIT_SYS_MIN + 90);
        public const int SSC_STSBIT_SYS_CCO = (STS_BIT_SYS_MIN + 91);
        public const int SSC_STSBIT_SYS_CCF = (STS_BIT_SYS_MIN + 92);
        public const int SSC_STSBIT_SYS_CCE = (STS_BIT_SYS_MIN + 93);
        public const int SSC_STSBIT_SYS_95 = (STS_BIT_SYS_MIN + 94);
        public const int SSC_STSBIT_SYS_96 = (STS_BIT_SYS_MIN + 95);

        public const int SSC_STSBIT_SYS_97 = (STS_BIT_SYS_MIN + 96);
        public const int SSC_STSBIT_SYS_98 = (STS_BIT_SYS_MIN + 97);
        public const int SSC_STSBIT_SYS_99 = (STS_BIT_SYS_MIN + 98);
        public const int SSC_STSBIT_SYS_100 = (STS_BIT_SYS_MIN + 99);
        public const int SSC_STSBIT_SYS_101 = (STS_BIT_SYS_MIN + 100);
        public const int SSC_STSBIT_SYS_102 = (STS_BIT_SYS_MIN + 101);
        public const int SSC_STSBIT_SYS_103 = (STS_BIT_SYS_MIN + 102);
        public const int SSC_STSBIT_SYS_104 = (STS_BIT_SYS_MIN + 103);

        public const int SSC_STSBIT_SYS_105 = (STS_BIT_SYS_MIN + 104);
        public const int SSC_STSBIT_SYS_106 = (STS_BIT_SYS_MIN + 105);
        public const int SSC_STSBIT_SYS_107 = (STS_BIT_SYS_MIN + 106);
        public const int SSC_STSBIT_SYS_108 = (STS_BIT_SYS_MIN + 107);
        public const int SSC_STSBIT_SYS_109 = (STS_BIT_SYS_MIN + 108);
        public const int SSC_STSBIT_SYS_110 = (STS_BIT_SYS_MIN + 109);
        public const int SSC_STSBIT_SYS_111 = (STS_BIT_SYS_MIN + 110);
        public const int SSC_STSBIT_SYS_112 = (STS_BIT_SYS_MIN + 111);

        public const int SSC_STSBIT_SYS_113 = (STS_BIT_SYS_MIN + 112);
        public const int SSC_STSBIT_SYS_114 = (STS_BIT_SYS_MIN + 113);
        public const int SSC_STSBIT_SYS_115 = (STS_BIT_SYS_MIN + 114);
        public const int SSC_STSBIT_SYS_116 = (STS_BIT_SYS_MIN + 115);
        public const int SSC_STSBIT_SYS_117 = (STS_BIT_SYS_MIN + 116);
        public const int SSC_STSBIT_SYS_118 = (STS_BIT_SYS_MIN + 117);
        public const int SSC_STSBIT_SYS_119 = (STS_BIT_SYS_MIN + 118);
        public const int SSC_STSBIT_SYS_120 = (STS_BIT_SYS_MIN + 119);

        public const int SSC_STSBIT_SYS_121 = (STS_BIT_SYS_MIN + 120);
        public const int SSC_STSBIT_SYS_122 = (STS_BIT_SYS_MIN + 121);
        public const int SSC_STSBIT_SYS_123 = (STS_BIT_SYS_MIN + 122);
        public const int SSC_STSBIT_SYS_124 = (STS_BIT_SYS_MIN + 123);
        public const int SSC_STSBIT_SYS_125 = (STS_BIT_SYS_MIN + 124);
        public const int SSC_STSBIT_SYS_126 = (STS_BIT_SYS_MIN + 125);
        public const int SSC_STSBIT_SYS_127 = (STS_BIT_SYS_MIN + 126);
        public const int SSC_STSBIT_SYS_128 = (STS_BIT_SYS_MIN + 127);

        public const int SSC_STSBIT_SYS_SPWFIN1 = (STS_BIT_SYS_MIN + 128);
        public const int SSC_STSBIT_SYS_SPWEN1 = (STS_BIT_SYS_MIN + 129);
        public const int SSC_STSBIT_SYS_SPWED1 = (STS_BIT_SYS_MIN + 130);
        public const int SSC_STSBIT_SYS_132 = (STS_BIT_SYS_MIN + 131);
        public const int SSC_STSBIT_SYS_PWFIN2 = (STS_BIT_SYS_MIN + 132);
        public const int SSC_STSBIT_SYS_PWEN2 = (STS_BIT_SYS_MIN + 133);
        public const int SSC_STSBIT_SYS_PWED2 = (STS_BIT_SYS_MIN + 134);
        public const int SSC_STSBIT_SYS_136 = (STS_BIT_SYS_MIN + 135);

        public const int SSC_STSBIT_SYS_SPRFIN1 = (STS_BIT_SYS_MIN + 136);
        public const int SSC_STSBIT_SYS_SPREN1 = (STS_BIT_SYS_MIN + 137);
        public const int SSC_STSBIT_SYS_SPRFIN2 = (STS_BIT_SYS_MIN + 138);
        public const int SSC_STSBIT_SYS_SPREN2 = (STS_BIT_SYS_MIN + 139);
        public const int SSC_STSBIT_SYS_141 = (STS_BIT_SYS_MIN + 140);
        public const int SSC_STSBIT_SYS_142 = (STS_BIT_SYS_MIN + 141);
        public const int SSC_STSBIT_SYS_143 = (STS_BIT_SYS_MIN + 142);
        public const int SSC_STSBIT_SYS_144 = (STS_BIT_SYS_MIN + 143);

        public const int SSC_STSBIT_SYS_SWFIN = (STS_BIT_SYS_MIN + 144);
        public const int SSC_STSBIT_SYS_SWEN = (STS_BIT_SYS_MIN + 145);
        public const int SSC_STSBIT_SYS_SWED = (STS_BIT_SYS_MIN + 146);
        public const int SSC_STSBIT_SYS_148 = (STS_BIT_SYS_MIN + 147);
        public const int SSC_STSBIT_SYS_SRFIN = (STS_BIT_SYS_MIN + 148);
        public const int SSC_STSBIT_SYS_SREN = (STS_BIT_SYS_MIN + 149);
        public const int SSC_STSBIT_SYS_151 = (STS_BIT_SYS_MIN + 150);
        public const int SSC_STSBIT_SYS_152 = (STS_BIT_SYS_MIN + 151);

        public const int SSC_STSBIT_SYS_153 = (STS_BIT_SYS_MIN + 152);
        public const int SSC_STSBIT_SYS_154 = (STS_BIT_SYS_MIN + 153);
        public const int SSC_STSBIT_SYS_155 = (STS_BIT_SYS_MIN + 154);
        public const int SSC_STSBIT_SYS_156 = (STS_BIT_SYS_MIN + 155);
        public const int SSC_STSBIT_SYS_157 = (STS_BIT_SYS_MIN + 156);
        public const int SSC_STSBIT_SYS_158 = (STS_BIT_SYS_MIN + 157);
        public const int SSC_STSBIT_SYS_159 = (STS_BIT_SYS_MIN + 158);
        public const int SSC_STSBIT_SYS_160 = (STS_BIT_SYS_MIN + 159);

        public const int SSC_STSBIT_SYS_161 = (STS_BIT_SYS_MIN + 160);
        public const int SSC_STSBIT_SYS_162 = (STS_BIT_SYS_MIN + 161);
        public const int SSC_STSBIT_SYS_163 = (STS_BIT_SYS_MIN + 162);
        public const int SSC_STSBIT_SYS_164 = (STS_BIT_SYS_MIN + 163);
        public const int SSC_STSBIT_SYS_165 = (STS_BIT_SYS_MIN + 164);
        public const int SSC_STSBIT_SYS_166 = (STS_BIT_SYS_MIN + 165);
        public const int SSC_STSBIT_SYS_167 = (STS_BIT_SYS_MIN + 166);
        public const int SSC_STSBIT_SYS_168 = (STS_BIT_SYS_MIN + 167);

        public const int SSC_STSBIT_SYS_169 = (STS_BIT_SYS_MIN + 168);
        public const int SSC_STSBIT_SYS_170 = (STS_BIT_SYS_MIN + 169);
        public const int SSC_STSBIT_SYS_171 = (STS_BIT_SYS_MIN + 170);
        public const int SSC_STSBIT_SYS_172 = (STS_BIT_SYS_MIN + 171);
        public const int SSC_STSBIT_SYS_173 = (STS_BIT_SYS_MIN + 172);
        public const int SSC_STSBIT_SYS_174 = (STS_BIT_SYS_MIN + 173);
        public const int SSC_STSBIT_SYS_175 = (STS_BIT_SYS_MIN + 174);
        public const int SSC_STSBIT_SYS_176 = (STS_BIT_SYS_MIN + 175);

        public const int SSC_STSBIT_SYS_FROK = (STS_BIT_SYS_MIN + 176);
        public const int SSC_STSBIT_SYS_FRNG = (STS_BIT_SYS_MIN + 177);
        public const int SSC_STSBIT_SYS_FSOK = (STS_BIT_SYS_MIN + 178);
        public const int SSC_STSBIT_SYS_FSNG = (STS_BIT_SYS_MIN + 179);
        public const int SSC_STSBIT_SYS_FIROK = (STS_BIT_SYS_MIN + 180);
        public const int SSC_STSBIT_SYS_FIRNG = (STS_BIT_SYS_MIN + 181);
        public const int SSC_STSBIT_SYS_FIOK = (STS_BIT_SYS_MIN + 182);
        public const int SSC_STSBIT_SYS_FING = (STS_BIT_SYS_MIN + 183);

        public const int SSC_STSBIT_SYS_ALHRF = (STS_BIT_SYS_MIN + 184);
        public const int SSC_STSBIT_SYS_ALHRE = (STS_BIT_SYS_MIN + 185);
        public const int SSC_STSBIT_SYS_ALHIF = (STS_BIT_SYS_MIN + 186);
        public const int SSC_STSBIT_SYS_ALHIE = (STS_BIT_SYS_MIN + 187);
        public const int SSC_STSBIT_SYS_189 = (STS_BIT_SYS_MIN + 188);
        public const int SSC_STSBIT_SYS_190 = (STS_BIT_SYS_MIN + 189);
        public const int SSC_STSBIT_SYS_191 = (STS_BIT_SYS_MIN + 190);
        public const int SSC_STSBIT_SYS_192 = (STS_BIT_SYS_MIN + 191);

        public const int SSC_STSBIT_SYS_193 = (STS_BIT_SYS_MIN + 192);
        public const int SSC_STSBIT_SYS_194 = (STS_BIT_SYS_MIN + 193);
        public const int SSC_STSBIT_SYS_195 = (STS_BIT_SYS_MIN + 194);
        public const int SSC_STSBIT_SYS_196 = (STS_BIT_SYS_MIN + 195);
        public const int SSC_STSBIT_SYS_197 = (STS_BIT_SYS_MIN + 196);
        public const int SSC_STSBIT_SYS_198 = (STS_BIT_SYS_MIN + 197);
        public const int SSC_STSBIT_SYS_199 = (STS_BIT_SYS_MIN + 198);
        public const int SSC_STSBIT_SYS_200 = (STS_BIT_SYS_MIN + 199);

        public const int SSC_STSBIT_SYS_201 = (STS_BIT_SYS_MIN + 200);
        public const int SSC_STSBIT_SYS_202 = (STS_BIT_SYS_MIN + 201);
        public const int SSC_STSBIT_SYS_203 = (STS_BIT_SYS_MIN + 202);
        public const int SSC_STSBIT_SYS_204 = (STS_BIT_SYS_MIN + 203);
        public const int SSC_STSBIT_SYS_205 = (STS_BIT_SYS_MIN + 204);
        public const int SSC_STSBIT_SYS_206 = (STS_BIT_SYS_MIN + 205);
        public const int SSC_STSBIT_SYS_207 = (STS_BIT_SYS_MIN + 206);
        public const int SSC_STSBIT_SYS_208 = (STS_BIT_SYS_MIN + 207);

        public const int SSC_STSBIT_SYS_209 = (STS_BIT_SYS_MIN + 208);
        public const int SSC_STSBIT_SYS_210 = (STS_BIT_SYS_MIN + 209);
        public const int SSC_STSBIT_SYS_211 = (STS_BIT_SYS_MIN + 210);
        public const int SSC_STSBIT_SYS_212 = (STS_BIT_SYS_MIN + 211);
        public const int SSC_STSBIT_SYS_213 = (STS_BIT_SYS_MIN + 212);
        public const int SSC_STSBIT_SYS_214 = (STS_BIT_SYS_MIN + 213);
        public const int SSC_STSBIT_SYS_215 = (STS_BIT_SYS_MIN + 214);
        public const int SSC_STSBIT_SYS_216 = (STS_BIT_SYS_MIN + 215);

        public const int SSC_STSBIT_SYS_217 = (STS_BIT_SYS_MIN + 216);
        public const int SSC_STSBIT_SYS_218 = (STS_BIT_SYS_MIN + 217);
        public const int SSC_STSBIT_SYS_219 = (STS_BIT_SYS_MIN + 218);
        public const int SSC_STSBIT_SYS_220 = (STS_BIT_SYS_MIN + 219);
        public const int SSC_STSBIT_SYS_221 = (STS_BIT_SYS_MIN + 220);
        public const int SSC_STSBIT_SYS_222 = (STS_BIT_SYS_MIN + 221);
        public const int SSC_STSBIT_SYS_223 = (STS_BIT_SYS_MIN + 222);
        public const int SSC_STSBIT_SYS_224 = (STS_BIT_SYS_MIN + 223);

        public const int SSC_STSBIT_SYS_225 = (STS_BIT_SYS_MIN + 224);
        public const int SSC_STSBIT_SYS_226 = (STS_BIT_SYS_MIN + 225);
        public const int SSC_STSBIT_SYS_227 = (STS_BIT_SYS_MIN + 226);
        public const int SSC_STSBIT_SYS_228 = (STS_BIT_SYS_MIN + 227);
        public const int SSC_STSBIT_SYS_229 = (STS_BIT_SYS_MIN + 228);
        public const int SSC_STSBIT_SYS_230 = (STS_BIT_SYS_MIN + 229);
        public const int SSC_STSBIT_SYS_231 = (STS_BIT_SYS_MIN + 230);
        public const int SSC_STSBIT_SYS_232 = (STS_BIT_SYS_MIN + 231);

        public const int SSC_STSBIT_SYS_233 = (STS_BIT_SYS_MIN + 232);
        public const int SSC_STSBIT_SYS_234 = (STS_BIT_SYS_MIN + 233);
        public const int SSC_STSBIT_SYS_235 = (STS_BIT_SYS_MIN + 234);
        public const int SSC_STSBIT_SYS_236 = (STS_BIT_SYS_MIN + 235);
        public const int SSC_STSBIT_SYS_237 = (STS_BIT_SYS_MIN + 236);
        public const int SSC_STSBIT_SYS_238 = (STS_BIT_SYS_MIN + 237);
        public const int SSC_STSBIT_SYS_239 = (STS_BIT_SYS_MIN + 238);
        public const int SSC_STSBIT_SYS_240 = (STS_BIT_SYS_MIN + 239);

        public const int SSC_STSBIT_SYS_241 = (STS_BIT_SYS_MIN + 240);
        public const int SSC_STSBIT_SYS_242 = (STS_BIT_SYS_MIN + 241);
        public const int SSC_STSBIT_SYS_243 = (STS_BIT_SYS_MIN + 242);
        public const int SSC_STSBIT_SYS_244 = (STS_BIT_SYS_MIN + 243);
        public const int SSC_STSBIT_SYS_245 = (STS_BIT_SYS_MIN + 244);
        public const int SSC_STSBIT_SYS_246 = (STS_BIT_SYS_MIN + 245);
        public const int SSC_STSBIT_SYS_247 = (STS_BIT_SYS_MIN + 246);
        public const int SSC_STSBIT_SYS_248 = (STS_BIT_SYS_MIN + 247);

        public const int SSC_STSBIT_SYS_249 = (STS_BIT_SYS_MIN + 248);
        public const int SSC_STSBIT_SYS_250 = (STS_BIT_SYS_MIN + 249);
        public const int SSC_STSBIT_SYS_251 = (STS_BIT_SYS_MIN + 250);
        public const int SSC_STSBIT_SYS_252 = (STS_BIT_SYS_MIN + 251);
        public const int SSC_STSBIT_SYS_253 = (STS_BIT_SYS_MIN + 252);
        public const int SSC_STSBIT_SYS_254 = (STS_BIT_SYS_MIN + 253);
        public const int SSC_STSBIT_SYS_255 = (STS_BIT_SYS_MIN + 254);
        public const int SSC_STSBIT_SYS_256 = (STS_BIT_SYS_MIN + 255);


        /*------------------------------------------*/
        /* axis command bits                        */
        /*------------------------------------------*/
        public const int SSC_CMDBIT_AX_SON = (CMD_BIT_AX_MIN + 0);
        public const int SSC_CMDBIT_AX_2 = (CMD_BIT_AX_MIN + 1);
        public const int SSC_CMDBIT_AX_3 = (CMD_BIT_AX_MIN + 2);
        public const int SSC_CMDBIT_AX_4 = (CMD_BIT_AX_MIN + 3);
        public const int SSC_CMDBIT_AX_TL = (CMD_BIT_AX_MIN + 4);
        public const int SSC_CMDBIT_AX_SRST = (CMD_BIT_AX_MIN + 5);
        public const int SSC_CMDBIT_AX_7 = (CMD_BIT_AX_MIN + 6);
        public const int SSC_CMDBIT_AX_8 = (CMD_BIT_AX_MIN + 7);

        public const int SSC_CMDBIT_AX_ST = (CMD_BIT_AX_MIN + 8);
        public const int SSC_CMDBIT_AX_DIR = (CMD_BIT_AX_MIN + 9);
        public const int SSC_CMDBIT_AX_STP = (CMD_BIT_AX_MIN + 10);
        public const int SSC_CMDBIT_AX_RSTP = (CMD_BIT_AX_MIN + 11);
        public const int SSC_CMDBIT_AX_13 = (CMD_BIT_AX_MIN + 12);
        public const int SSC_CMDBIT_AX_ORST = (CMD_BIT_AX_MIN + 13);
        public const int SSC_CMDBIT_AX_15 = (CMD_BIT_AX_MIN + 14);
        public const int SSC_CMDBIT_AX_16 = (CMD_BIT_AX_MIN + 15);

        public const int SSC_CMDBIT_AX_AUT = (CMD_BIT_AX_MIN + 16);
        public const int SSC_CMDBIT_AX_ZRN = (CMD_BIT_AX_MIN + 17);
        public const int SSC_CMDBIT_AX_JOG = (CMD_BIT_AX_MIN + 18);
        public const int SSC_CMDBIT_AX_S = (CMD_BIT_AX_MIN + 19);
        public const int SSC_CMDBIT_AX_21 = (CMD_BIT_AX_MIN + 20);
        public const int SSC_CMDBIT_AX_LIP = (CMD_BIT_AX_MIN + 21);
        public const int SSC_CMDBIT_AX_DST = (CMD_BIT_AX_MIN + 22);
        public const int SSC_CMDBIT_AX_24 = (CMD_BIT_AX_MIN + 23);

        public const int SSC_CMDBIT_AX_25 = (CMD_BIT_AX_MIN + 24);
        public const int SSC_CMDBIT_AX_26 = (CMD_BIT_AX_MIN + 25);
        public const int SSC_CMDBIT_AX_27 = (CMD_BIT_AX_MIN + 26);
        public const int SSC_CMDBIT_AX_28 = (CMD_BIT_AX_MIN + 27);
        public const int SSC_CMDBIT_AX_29 = (CMD_BIT_AX_MIN + 28);
        public const int SSC_CMDBIT_AX_30 = (CMD_BIT_AX_MIN + 29);
        public const int SSC_CMDBIT_AX_31 = (CMD_BIT_AX_MIN + 30);
        public const int SSC_CMDBIT_AX_32 = (CMD_BIT_AX_MIN + 31);

        public const int SSC_CMDBIT_AX_ITL = (CMD_BIT_AX_MIN + 32);
        public const int SSC_CMDBIT_AX_RMONR = (CMD_BIT_AX_MIN + 33);
        public const int SSC_CMDBIT_AX_35 = (CMD_BIT_AX_MIN + 34);
        public const int SSC_CMDBIT_AX_36 = (CMD_BIT_AX_MIN + 35);
        public const int SSC_CMDBIT_AX_LSPC = (CMD_BIT_AX_MIN + 36);
        public const int SSC_CMDBIT_AX_LSNC = (CMD_BIT_AX_MIN + 37);
        public const int SSC_CMDBIT_AX_DOGC = (CMD_BIT_AX_MIN + 38);
        public const int SSC_CMDBIT_AX_40 = (CMD_BIT_AX_MIN + 39);

        public const int SSC_CMDBIT_AX_SCHG = (CMD_BIT_AX_MIN + 40);
        public const int SSC_CMDBIT_AX_TACHG = (CMD_BIT_AX_MIN + 41);
        public const int SSC_CMDBIT_AX_TDCHG = (CMD_BIT_AX_MIN + 42);
        public const int SSC_CMDBIT_AX_PCHG = (CMD_BIT_AX_MIN + 43);
        public const int SSC_CMDBIT_AX_45 = (CMD_BIT_AX_MIN + 44);
        public const int SSC_CMDBIT_AX_46 = (CMD_BIT_AX_MIN + 45);
        public const int SSC_CMDBIT_AX_47 = (CMD_BIT_AX_MIN + 46);
        public const int SSC_CMDBIT_AX_48 = (CMD_BIT_AX_MIN + 47);

        public const int SSC_CMDBIT_AX_FST = (CMD_BIT_AX_MIN + 48);
        public const int SSC_CMDBIT_AX_50 = (CMD_BIT_AX_MIN + 49);
        public const int SSC_CMDBIT_AX_51 = (CMD_BIT_AX_MIN + 50);
        public const int SSC_CMDBIT_AX_52 = (CMD_BIT_AX_MIN + 51);
        public const int SSC_CMDBIT_AX_53 = (CMD_BIT_AX_MIN + 52);
        public const int SSC_CMDBIT_AX_54 = (CMD_BIT_AX_MIN + 53);
        public const int SSC_CMDBIT_AX_55 = (CMD_BIT_AX_MIN + 54);
        public const int SSC_CMDBIT_AX_56 = (CMD_BIT_AX_MIN + 55);

        public const int SSC_CMDBIT_AX_PPISTP = (CMD_BIT_AX_MIN + 56);
        public const int SSC_CMDBIT_AX_58 = (CMD_BIT_AX_MIN + 57);
        public const int SSC_CMDBIT_AX_59 = (CMD_BIT_AX_MIN + 58);
        public const int SSC_CMDBIT_AX_60 = (CMD_BIT_AX_MIN + 59);
        public const int SSC_CMDBIT_AX_61 = (CMD_BIT_AX_MIN + 60);
        public const int SSC_CMDBIT_AX_62 = (CMD_BIT_AX_MIN + 61);
        public const int SSC_CMDBIT_AX_63 = (CMD_BIT_AX_MIN + 62);
        public const int SSC_CMDBIT_AX_64 = (CMD_BIT_AX_MIN + 63);

        public const int SSC_CMDBIT_AX_GAIN = (CMD_BIT_AX_MIN + 64);
        public const int SSC_CMDBIT_AX_FCLS = (CMD_BIT_AX_MIN + 65);
        public const int SSC_CMDBIT_AX_67 = (CMD_BIT_AX_MIN + 66);
        public const int SSC_CMDBIT_AX_CPC = (CMD_BIT_AX_MIN + 67);
        public const int SSC_CMDBIT_AX_69 = (CMD_BIT_AX_MIN + 68);
        public const int SSC_CMDBIT_AX_70 = (CMD_BIT_AX_MIN + 69);
        public const int SSC_CMDBIT_AX_71 = (CMD_BIT_AX_MIN + 70);
        public const int SSC_CMDBIT_AX_72 = (CMD_BIT_AX_MIN + 71);

        public const int SSC_CMDBIT_AX_73 = (CMD_BIT_AX_MIN + 72);
        public const int SSC_CMDBIT_AX_74 = (CMD_BIT_AX_MIN + 73);
        public const int SSC_CMDBIT_AX_75 = (CMD_BIT_AX_MIN + 74);
        public const int SSC_CMDBIT_AX_76 = (CMD_BIT_AX_MIN + 75);
        public const int SSC_CMDBIT_AX_77 = (CMD_BIT_AX_MIN + 76);
        public const int SSC_CMDBIT_AX_78 = (CMD_BIT_AX_MIN + 77);
        public const int SSC_CMDBIT_AX_79 = (CMD_BIT_AX_MIN + 78);
        public const int SSC_CMDBIT_AX_80 = (CMD_BIT_AX_MIN + 79);

        public const int SSC_CMDBIT_AX_81 = (CMD_BIT_AX_MIN + 80);
        public const int SSC_CMDBIT_AX_82 = (CMD_BIT_AX_MIN + 81);
        public const int SSC_CMDBIT_AX_83 = (CMD_BIT_AX_MIN + 82);
        public const int SSC_CMDBIT_AX_84 = (CMD_BIT_AX_MIN + 83);
        public const int SSC_CMDBIT_AX_ZSC = (CMD_BIT_AX_MIN + 84);
        public const int SSC_CMDBIT_AX_86 = (CMD_BIT_AX_MIN + 85);
        public const int SSC_CMDBIT_AX_87 = (CMD_BIT_AX_MIN + 86);
        public const int SSC_CMDBIT_AX_88 = (CMD_BIT_AX_MIN + 87);

        public const int SSC_CMDBIT_AX_89 = (CMD_BIT_AX_MIN + 88);
        public const int SSC_CMDBIT_AX_MKC1 = (CMD_BIT_AX_MIN + 89);
        public const int SSC_CMDBIT_AX_MKD1 = (CMD_BIT_AX_MIN + 90);
        public const int SSC_CMDBIT_AX_MKSEN1 = (CMD_BIT_AX_MIN + 91);
        public const int SSC_CMDBIT_AX_93 = (CMD_BIT_AX_MIN + 92);
        public const int SSC_CMDBIT_AX_MKC2 = (CMD_BIT_AX_MIN + 93);
        public const int SSC_CMDBIT_AX_MKD2 = (CMD_BIT_AX_MIN + 94);
        public const int SSC_CMDBIT_AX_MKSEN2 = (CMD_BIT_AX_MIN + 95);

        public const int SSC_CMDBIT_AX_97 = (CMD_BIT_AX_MIN + 96);
        public const int SSC_CMDBIT_AX_98 = (CMD_BIT_AX_MIN + 97);
        public const int SSC_CMDBIT_AX_99 = (CMD_BIT_AX_MIN + 98);
        public const int SSC_CMDBIT_AX_100 = (CMD_BIT_AX_MIN + 99);
        public const int SSC_CMDBIT_AX_CTLMC = (CMD_BIT_AX_MIN + 100);
        public const int SSC_CMDBIT_AX_102 = (CMD_BIT_AX_MIN + 101);
        public const int SSC_CMDBIT_AX_103 = (CMD_BIT_AX_MIN + 102);
        public const int SSC_CMDBIT_AX_104 = (CMD_BIT_AX_MIN + 103);

        public const int SSC_CMDBIT_AX_105 = (CMD_BIT_AX_MIN + 104);
        public const int SSC_CMDBIT_AX_106 = (CMD_BIT_AX_MIN + 105);
        public const int SSC_CMDBIT_AX_107 = (CMD_BIT_AX_MIN + 106);
        public const int SSC_CMDBIT_AX_108 = (CMD_BIT_AX_MIN + 107);
        public const int SSC_CMDBIT_AX_109 = (CMD_BIT_AX_MIN + 108);
        public const int SSC_CMDBIT_AX_110 = (CMD_BIT_AX_MIN + 109);
        public const int SSC_CMDBIT_AX_111 = (CMD_BIT_AX_MIN + 110);
        public const int SSC_CMDBIT_AX_112 = (CMD_BIT_AX_MIN + 111);

        public const int SSC_CMDBIT_AX_113 = (CMD_BIT_AX_MIN + 112);
        public const int SSC_CMDBIT_AX_114 = (CMD_BIT_AX_MIN + 113);
        public const int SSC_CMDBIT_AX_115 = (CMD_BIT_AX_MIN + 114);
        public const int SSC_CMDBIT_AX_116 = (CMD_BIT_AX_MIN + 115);
        public const int SSC_CMDBIT_AX_117 = (CMD_BIT_AX_MIN + 116);
        public const int SSC_CMDBIT_AX_118 = (CMD_BIT_AX_MIN + 117);
        public const int SSC_CMDBIT_AX_119 = (CMD_BIT_AX_MIN + 118);
        public const int SSC_CMDBIT_AX_120 = (CMD_BIT_AX_MIN + 119);

        public const int SSC_CMDBIT_AX_121 = (CMD_BIT_AX_MIN + 120);
        public const int SSC_CMDBIT_AX_122 = (CMD_BIT_AX_MIN + 121);
        public const int SSC_CMDBIT_AX_123 = (CMD_BIT_AX_MIN + 122);
        public const int SSC_CMDBIT_AX_124 = (CMD_BIT_AX_MIN + 123);
        public const int SSC_CMDBIT_AX_125 = (CMD_BIT_AX_MIN + 124);
        public const int SSC_CMDBIT_AX_126 = (CMD_BIT_AX_MIN + 125);
        public const int SSC_CMDBIT_AX_127 = (CMD_BIT_AX_MIN + 126);
        public const int SSC_CMDBIT_AX_128 = (CMD_BIT_AX_MIN + 127);

        public const int SSC_CMDBIT_AX_MON = (CMD_BIT_AX_MIN + 128);
        public const int SSC_CMDBIT_AX_MONR = (CMD_BIT_AX_MIN + 129);
        public const int SSC_CMDBIT_AX_131 = (CMD_BIT_AX_MIN + 130);
        public const int SSC_CMDBIT_AX_132 = (CMD_BIT_AX_MIN + 131);
        public const int SSC_CMDBIT_AX_133 = (CMD_BIT_AX_MIN + 132);
        public const int SSC_CMDBIT_AX_134 = (CMD_BIT_AX_MIN + 133);
        public const int SSC_CMDBIT_AX_135 = (CMD_BIT_AX_MIN + 134);
        public const int SSC_CMDBIT_AX_136 = (CMD_BIT_AX_MIN + 135);

        public const int SSC_CMDBIT_AX_137 = (CMD_BIT_AX_MIN + 136);
        public const int SSC_CMDBIT_AX_138 = (CMD_BIT_AX_MIN + 137);
        public const int SSC_CMDBIT_AX_139 = (CMD_BIT_AX_MIN + 138);
        public const int SSC_CMDBIT_AX_140 = (CMD_BIT_AX_MIN + 139);
        public const int SSC_CMDBIT_AX_141 = (CMD_BIT_AX_MIN + 140);
        public const int SSC_CMDBIT_AX_142 = (CMD_BIT_AX_MIN + 141);
        public const int SSC_CMDBIT_AX_143 = (CMD_BIT_AX_MIN + 142);
        public const int SSC_CMDBIT_AX_144 = (CMD_BIT_AX_MIN + 143);

        public const int SSC_CMDBIT_AX_145 = (CMD_BIT_AX_MIN + 144);
        public const int SSC_CMDBIT_AX_146 = (CMD_BIT_AX_MIN + 145);
        public const int SSC_CMDBIT_AX_147 = (CMD_BIT_AX_MIN + 146);
        public const int SSC_CMDBIT_AX_148 = (CMD_BIT_AX_MIN + 147);
        public const int SSC_CMDBIT_AX_149 = (CMD_BIT_AX_MIN + 148);
        public const int SSC_CMDBIT_AX_150 = (CMD_BIT_AX_MIN + 149);
        public const int SSC_CMDBIT_AX_151 = (CMD_BIT_AX_MIN + 150);
        public const int SSC_CMDBIT_AX_152 = (CMD_BIT_AX_MIN + 151);

        public const int SSC_CMDBIT_AX_153 = (CMD_BIT_AX_MIN + 152);
        public const int SSC_CMDBIT_AX_154 = (CMD_BIT_AX_MIN + 153);
        public const int SSC_CMDBIT_AX_155 = (CMD_BIT_AX_MIN + 154);
        public const int SSC_CMDBIT_AX_156 = (CMD_BIT_AX_MIN + 155);
        public const int SSC_CMDBIT_AX_157 = (CMD_BIT_AX_MIN + 156);
        public const int SSC_CMDBIT_AX_158 = (CMD_BIT_AX_MIN + 157);
        public const int SSC_CMDBIT_AX_159 = (CMD_BIT_AX_MIN + 158);
        public const int SSC_CMDBIT_AX_160 = (CMD_BIT_AX_MIN + 159);

        public const int SSC_CMDBIT_AX_PWRT = (CMD_BIT_AX_MIN + 160);
        public const int SSC_CMDBIT_AX_162 = (CMD_BIT_AX_MIN + 161);
        public const int SSC_CMDBIT_AX_163 = (CMD_BIT_AX_MIN + 162);
        public const int SSC_CMDBIT_AX_164 = (CMD_BIT_AX_MIN + 163);
        public const int SSC_CMDBIT_AX_165 = (CMD_BIT_AX_MIN + 164);
        public const int SSC_CMDBIT_AX_166 = (CMD_BIT_AX_MIN + 165);
        public const int SSC_CMDBIT_AX_167 = (CMD_BIT_AX_MIN + 166);
        public const int SSC_CMDBIT_AX_PSF = (CMD_BIT_AX_MIN + 167);

        public const int SSC_CMDBIT_AX_PRD = (CMD_BIT_AX_MIN + 168);
        public const int SSC_CMDBIT_AX_170 = (CMD_BIT_AX_MIN + 169);
        public const int SSC_CMDBIT_AX_171 = (CMD_BIT_AX_MIN + 170);
        public const int SSC_CMDBIT_AX_172 = (CMD_BIT_AX_MIN + 171);
        public const int SSC_CMDBIT_AX_173 = (CMD_BIT_AX_MIN + 172);
        public const int SSC_CMDBIT_AX_174 = (CMD_BIT_AX_MIN + 173);
        public const int SSC_CMDBIT_AX_175 = (CMD_BIT_AX_MIN + 174);
        public const int SSC_CMDBIT_AX_176 = (CMD_BIT_AX_MIN + 175);

        public const int SSC_CMDBIT_AX_177 = (CMD_BIT_AX_MIN + 176);
        public const int SSC_CMDBIT_AX_178 = (CMD_BIT_AX_MIN + 177);
        public const int SSC_CMDBIT_AX_179 = (CMD_BIT_AX_MIN + 178);
        public const int SSC_CMDBIT_AX_180 = (CMD_BIT_AX_MIN + 179);
        public const int SSC_CMDBIT_AX_181 = (CMD_BIT_AX_MIN + 180);
        public const int SSC_CMDBIT_AX_182 = (CMD_BIT_AX_MIN + 181);
        public const int SSC_CMDBIT_AX_183 = (CMD_BIT_AX_MIN + 182);
        public const int SSC_CMDBIT_AX_184 = (CMD_BIT_AX_MIN + 183);

        public const int SSC_CMDBIT_AX_185 = (CMD_BIT_AX_MIN + 184);
        public const int SSC_CMDBIT_AX_186 = (CMD_BIT_AX_MIN + 185);
        public const int SSC_CMDBIT_AX_187 = (CMD_BIT_AX_MIN + 186);
        public const int SSC_CMDBIT_AX_188 = (CMD_BIT_AX_MIN + 187);
        public const int SSC_CMDBIT_AX_189 = (CMD_BIT_AX_MIN + 188);
        public const int SSC_CMDBIT_AX_190 = (CMD_BIT_AX_MIN + 189);
        public const int SSC_CMDBIT_AX_191 = (CMD_BIT_AX_MIN + 190);
        public const int SSC_CMDBIT_AX_192 = (CMD_BIT_AX_MIN + 191);

        public const int SSC_CMDBIT_AX_193 = (CMD_BIT_AX_MIN + 192);
        public const int SSC_CMDBIT_AX_194 = (CMD_BIT_AX_MIN + 193);
        public const int SSC_CMDBIT_AX_195 = (CMD_BIT_AX_MIN + 194);
        public const int SSC_CMDBIT_AX_196 = (CMD_BIT_AX_MIN + 195);
        public const int SSC_CMDBIT_AX_197 = (CMD_BIT_AX_MIN + 196);
        public const int SSC_CMDBIT_AX_198 = (CMD_BIT_AX_MIN + 197);
        public const int SSC_CMDBIT_AX_199 = (CMD_BIT_AX_MIN + 198);
        public const int SSC_CMDBIT_AX_200 = (CMD_BIT_AX_MIN + 199);

        public const int SSC_CMDBIT_AX_201 = (CMD_BIT_AX_MIN + 200);
        public const int SSC_CMDBIT_AX_202 = (CMD_BIT_AX_MIN + 201);
        public const int SSC_CMDBIT_AX_203 = (CMD_BIT_AX_MIN + 202);
        public const int SSC_CMDBIT_AX_204 = (CMD_BIT_AX_MIN + 203);
        public const int SSC_CMDBIT_AX_205 = (CMD_BIT_AX_MIN + 204);
        public const int SSC_CMDBIT_AX_206 = (CMD_BIT_AX_MIN + 205);
        public const int SSC_CMDBIT_AX_207 = (CMD_BIT_AX_MIN + 206);
        public const int SSC_CMDBIT_AX_208 = (CMD_BIT_AX_MIN + 207);

        public const int SSC_CMDBIT_AX_209 = (CMD_BIT_AX_MIN + 208);
        public const int SSC_CMDBIT_AX_210 = (CMD_BIT_AX_MIN + 209);
        public const int SSC_CMDBIT_AX_211 = (CMD_BIT_AX_MIN + 210);
        public const int SSC_CMDBIT_AX_212 = (CMD_BIT_AX_MIN + 211);
        public const int SSC_CMDBIT_AX_213 = (CMD_BIT_AX_MIN + 212);
        public const int SSC_CMDBIT_AX_214 = (CMD_BIT_AX_MIN + 213);
        public const int SSC_CMDBIT_AX_215 = (CMD_BIT_AX_MIN + 214);
        public const int SSC_CMDBIT_AX_216 = (CMD_BIT_AX_MIN + 215);

        public const int SSC_CMDBIT_AX_217 = (CMD_BIT_AX_MIN + 216);
        public const int SSC_CMDBIT_AX_218 = (CMD_BIT_AX_MIN + 217);
        public const int SSC_CMDBIT_AX_219 = (CMD_BIT_AX_MIN + 218);
        public const int SSC_CMDBIT_AX_220 = (CMD_BIT_AX_MIN + 219);
        public const int SSC_CMDBIT_AX_221 = (CMD_BIT_AX_MIN + 220);
        public const int SSC_CMDBIT_AX_222 = (CMD_BIT_AX_MIN + 221);
        public const int SSC_CMDBIT_AX_223 = (CMD_BIT_AX_MIN + 222);
        public const int SSC_CMDBIT_AX_224 = (CMD_BIT_AX_MIN + 223);

        public const int SSC_CMDBIT_AX_225 = (CMD_BIT_AX_MIN + 224);
        public const int SSC_CMDBIT_AX_226 = (CMD_BIT_AX_MIN + 225);
        public const int SSC_CMDBIT_AX_227 = (CMD_BIT_AX_MIN + 226);
        public const int SSC_CMDBIT_AX_228 = (CMD_BIT_AX_MIN + 227);
        public const int SSC_CMDBIT_AX_229 = (CMD_BIT_AX_MIN + 228);
        public const int SSC_CMDBIT_AX_230 = (CMD_BIT_AX_MIN + 229);
        public const int SSC_CMDBIT_AX_231 = (CMD_BIT_AX_MIN + 230);
        public const int SSC_CMDBIT_AX_232 = (CMD_BIT_AX_MIN + 231);

        public const int SSC_CMDBIT_AX_233 = (CMD_BIT_AX_MIN + 232);
        public const int SSC_CMDBIT_AX_234 = (CMD_BIT_AX_MIN + 233);
        public const int SSC_CMDBIT_AX_235 = (CMD_BIT_AX_MIN + 234);
        public const int SSC_CMDBIT_AX_236 = (CMD_BIT_AX_MIN + 235);
        public const int SSC_CMDBIT_AX_237 = (CMD_BIT_AX_MIN + 236);
        public const int SSC_CMDBIT_AX_238 = (CMD_BIT_AX_MIN + 237);
        public const int SSC_CMDBIT_AX_239 = (CMD_BIT_AX_MIN + 238);
        public const int SSC_CMDBIT_AX_240 = (CMD_BIT_AX_MIN + 239);

        public const int SSC_CMDBIT_AX_241 = (CMD_BIT_AX_MIN + 240);
        public const int SSC_CMDBIT_AX_242 = (CMD_BIT_AX_MIN + 241);
        public const int SSC_CMDBIT_AX_243 = (CMD_BIT_AX_MIN + 242);
        public const int SSC_CMDBIT_AX_244 = (CMD_BIT_AX_MIN + 243);
        public const int SSC_CMDBIT_AX_245 = (CMD_BIT_AX_MIN + 244);
        public const int SSC_CMDBIT_AX_246 = (CMD_BIT_AX_MIN + 245);
        public const int SSC_CMDBIT_AX_247 = (CMD_BIT_AX_MIN + 246);
        public const int SSC_CMDBIT_AX_248 = (CMD_BIT_AX_MIN + 247);

        public const int SSC_CMDBIT_AX_249 = (CMD_BIT_AX_MIN + 248);
        public const int SSC_CMDBIT_AX_250 = (CMD_BIT_AX_MIN + 249);
        public const int SSC_CMDBIT_AX_251 = (CMD_BIT_AX_MIN + 250);
        public const int SSC_CMDBIT_AX_252 = (CMD_BIT_AX_MIN + 251);
        public const int SSC_CMDBIT_AX_253 = (CMD_BIT_AX_MIN + 252);
        public const int SSC_CMDBIT_AX_254 = (CMD_BIT_AX_MIN + 253);
        public const int SSC_CMDBIT_AX_255 = (CMD_BIT_AX_MIN + 254);
        public const int SSC_CMDBIT_AX_256 = (CMD_BIT_AX_MIN + 255);


        /*------------------------------------------*/
        /* axis status bits                         */
        /*------------------------------------------*/
        public const int SSC_STSBIT_AX_RDY = (STS_BIT_AX_MIN + 0);
        public const int SSC_STSBIT_AX_INP = (STS_BIT_AX_MIN + 1);
        public const int SSC_STSBIT_AX_ZSP = (STS_BIT_AX_MIN + 2);
        public const int SSC_STSBIT_AX_ZPAS = (STS_BIT_AX_MIN + 3);
        public const int SSC_STSBIT_AX_TLC = (STS_BIT_AX_MIN + 4);
        public const int SSC_STSBIT_AX_SALM = (STS_BIT_AX_MIN + 5);
        public const int SSC_STSBIT_AX_SWRN = (STS_BIT_AX_MIN + 6);
        public const int SSC_STSBIT_AX_ABSE = (STS_BIT_AX_MIN + 7);

        public const int SSC_STSBIT_AX_OP = (STS_BIT_AX_MIN + 8);
        public const int SSC_STSBIT_AX_CPO = (STS_BIT_AX_MIN + 9);
        public const int SSC_STSBIT_AX_PF = (STS_BIT_AX_MIN + 10);
        public const int SSC_STSBIT_AX_ZP = (STS_BIT_AX_MIN + 11);
        public const int SSC_STSBIT_AX_SMZ = (STS_BIT_AX_MIN + 12);
        public const int SSC_STSBIT_AX_OALM = (STS_BIT_AX_MIN + 13);
        public const int SSC_STSBIT_AX_OPF = (STS_BIT_AX_MIN + 14);
        public const int SSC_STSBIT_AX_PSW = (STS_BIT_AX_MIN + 15);

        public const int SSC_STSBIT_AX_AUTO = (STS_BIT_AX_MIN + 16);
        public const int SSC_STSBIT_AX_ZRNO = (STS_BIT_AX_MIN + 17);
        public const int SSC_STSBIT_AX_JO = (STS_BIT_AX_MIN + 18);
        public const int SSC_STSBIT_AX_SO = (STS_BIT_AX_MIN + 19);
        public const int SSC_STSBIT_AX_21 = (STS_BIT_AX_MIN + 20);
        public const int SSC_STSBIT_AX_LIPO = (STS_BIT_AX_MIN + 21);
        public const int SSC_STSBIT_AX_DSTO = (STS_BIT_AX_MIN + 22);
        public const int SSC_STSBIT_AX_24 = (STS_BIT_AX_MIN + 23);

        public const int SSC_STSBIT_AX_25 = (STS_BIT_AX_MIN + 24);
        public const int SSC_STSBIT_AX_26 = (STS_BIT_AX_MIN + 25);
        public const int SSC_STSBIT_AX_27 = (STS_BIT_AX_MIN + 26);
        public const int SSC_STSBIT_AX_28 = (STS_BIT_AX_MIN + 27);
        public const int SSC_STSBIT_AX_29 = (STS_BIT_AX_MIN + 28);
        public const int SSC_STSBIT_AX_30 = (STS_BIT_AX_MIN + 29);
        public const int SSC_STSBIT_AX_31 = (STS_BIT_AX_MIN + 30);
        public const int SSC_STSBIT_AX_32 = (STS_BIT_AX_MIN + 31);

        public const int SSC_STSBIT_AX_ISTP = (STS_BIT_AX_MIN + 32);
        public const int SSC_STSBIT_AX_RMRCH = (STS_BIT_AX_MIN + 33);
        public const int SSC_STSBIT_AX_POV = (STS_BIT_AX_MIN + 34);
        public const int SSC_STSBIT_AX_STO = (STS_BIT_AX_MIN + 35);
        public const int SSC_STSBIT_AX_37 = (STS_BIT_AX_MIN + 36);
        public const int SSC_STSBIT_AX_38 = (STS_BIT_AX_MIN + 37);
        public const int SSC_STSBIT_AX_ZREQ = (STS_BIT_AX_MIN + 38);
        public const int SSC_STSBIT_AX_40 = (STS_BIT_AX_MIN + 39);

        public const int SSC_STSBIT_AX_SCF = (STS_BIT_AX_MIN + 40);
        public const int SSC_STSBIT_AX_TACF = (STS_BIT_AX_MIN + 41);
        public const int SSC_STSBIT_AX_TACE = (STS_BIT_AX_MIN + 42);
        public const int SSC_STSBIT_AX_PCF = (STS_BIT_AX_MIN + 43);
        public const int SSC_STSBIT_AX_SCE = (STS_BIT_AX_MIN + 44);
        public const int SSC_STSBIT_AX_TDCF = (STS_BIT_AX_MIN + 45);
        public const int SSC_STSBIT_AX_TDCE = (STS_BIT_AX_MIN + 46);
        public const int SSC_STSBIT_AX_PCE = (STS_BIT_AX_MIN + 47);

        public const int SSC_STSBIT_AX_49 = (STS_BIT_AX_MIN + 48);
        public const int SSC_STSBIT_AX_50 = (STS_BIT_AX_MIN + 49);
        public const int SSC_STSBIT_AX_51 = (STS_BIT_AX_MIN + 50);
        public const int SSC_STSBIT_AX_52 = (STS_BIT_AX_MIN + 51);
        public const int SSC_STSBIT_AX_53 = (STS_BIT_AX_MIN + 52);
        public const int SSC_STSBIT_AX_54 = (STS_BIT_AX_MIN + 53);
        public const int SSC_STSBIT_AX_55 = (STS_BIT_AX_MIN + 54);
        public const int SSC_STSBIT_AX_56 = (STS_BIT_AX_MIN + 55);

        public const int SSC_STSBIT_AX_PPIOP = (STS_BIT_AX_MIN + 56);
        public const int SSC_STSBIT_AX_PPIFIN = (STS_BIT_AX_MIN + 57);
        public const int SSC_STSBIT_AX_PPIERR = (STS_BIT_AX_MIN + 58);
        public const int SSC_STSBIT_AX_60 = (STS_BIT_AX_MIN + 59);
        public const int SSC_STSBIT_AX_61 = (STS_BIT_AX_MIN + 60);
        public const int SSC_STSBIT_AX_62 = (STS_BIT_AX_MIN + 61);
        public const int SSC_STSBIT_AX_63 = (STS_BIT_AX_MIN + 62);
        public const int SSC_STSBIT_AX_64 = (STS_BIT_AX_MIN + 63);

        public const int SSC_STSBIT_AX_GAINO = (STS_BIT_AX_MIN + 64);
        public const int SSC_STSBIT_AX_FCLSO = (STS_BIT_AX_MIN + 65);
        public const int SSC_STSBIT_AX_TLSO = (STS_BIT_AX_MIN + 66);
        public const int SSC_STSBIT_AX_SPC = (STS_BIT_AX_MIN + 67);
        public const int SSC_STSBIT_AX_69 = (STS_BIT_AX_MIN + 68);
        public const int SSC_STSBIT_AX_70 = (STS_BIT_AX_MIN + 69);
        public const int SSC_STSBIT_AX_71 = (STS_BIT_AX_MIN + 70);
        public const int SSC_STSBIT_AX_PRSMO = (STS_BIT_AX_MIN + 71);

        public const int SSC_STSBIT_AX_IWT = (STS_BIT_AX_MIN + 72);
        public const int SSC_STSBIT_AX_SINP = (STS_BIT_AX_MIN + 73);
        public const int SSC_STSBIT_AX_75 = (STS_BIT_AX_MIN + 74);
        public const int SSC_STSBIT_AX_76 = (STS_BIT_AX_MIN + 75);
        public const int SSC_STSBIT_AX_77 = (STS_BIT_AX_MIN + 76);
        public const int SSC_STSBIT_AX_78 = (STS_BIT_AX_MIN + 77);
        public const int SSC_STSBIT_AX_79 = (STS_BIT_AX_MIN + 78);
        public const int SSC_STSBIT_AX_80 = (STS_BIT_AX_MIN + 79);

        public const int SSC_STSBIT_AX_81 = (STS_BIT_AX_MIN + 80);
        public const int SSC_STSBIT_AX_82 = (STS_BIT_AX_MIN + 81);
        public const int SSC_STSBIT_AX_83 = (STS_BIT_AX_MIN + 82);
        public const int SSC_STSBIT_AX_84 = (STS_BIT_AX_MIN + 83);
        public const int SSC_STSBIT_AX_ZSF = (STS_BIT_AX_MIN + 84);
        public const int SSC_STSBIT_AX_ZSE = (STS_BIT_AX_MIN + 85);
        public const int SSC_STSBIT_AX_87 = (STS_BIT_AX_MIN + 86);
        public const int SSC_STSBIT_AX_88 = (STS_BIT_AX_MIN + 87);

        public const int SSC_STSBIT_AX_MKIF1 = (STS_BIT_AX_MIN + 88);
        public const int SSC_STSBIT_AX_MKCF1 = (STS_BIT_AX_MIN + 89);
        public const int SSC_STSBIT_AX_MKDO1 = (STS_BIT_AX_MIN + 90);
        public const int SSC_STSBIT_AX_MKSEF1 = (STS_BIT_AX_MIN + 91);
        public const int SSC_STSBIT_AX_MKIF2 = (STS_BIT_AX_MIN + 92);
        public const int SSC_STSBIT_AX_MKCF2 = (STS_BIT_AX_MIN + 93);
        public const int SSC_STSBIT_AX_MKDO2 = (STS_BIT_AX_MIN + 94);
        public const int SSC_STSBIT_AX_MKSEF2 = (STS_BIT_AX_MIN + 95);

        public const int SSC_STSBIT_AX_97 = (STS_BIT_AX_MIN + 96);
        public const int SSC_STSBIT_AX_98 = (STS_BIT_AX_MIN + 97);
        public const int SSC_STSBIT_AX_99 = (STS_BIT_AX_MIN + 98);
        public const int SSC_STSBIT_AX_100 = (STS_BIT_AX_MIN + 99);
        public const int SSC_STSBIT_AX_CTLMCF = (STS_BIT_AX_MIN + 100);
        public const int SSC_STSBIT_AX_CTLMCE = (STS_BIT_AX_MIN + 101);
        public const int SSC_STSBIT_AX_103 = (STS_BIT_AX_MIN + 102);
        public const int SSC_STSBIT_AX_104 = (STS_BIT_AX_MIN + 103);

        public const int SSC_STSBIT_AX_105 = (STS_BIT_AX_MIN + 104);
        public const int SSC_STSBIT_AX_106 = (STS_BIT_AX_MIN + 105);
        public const int SSC_STSBIT_AX_107 = (STS_BIT_AX_MIN + 106);
        public const int SSC_STSBIT_AX_108 = (STS_BIT_AX_MIN + 107);
        public const int SSC_STSBIT_AX_109 = (STS_BIT_AX_MIN + 108);
        public const int SSC_STSBIT_AX_110 = (STS_BIT_AX_MIN + 109);
        public const int SSC_STSBIT_AX_111 = (STS_BIT_AX_MIN + 110);
        public const int SSC_STSBIT_AX_112 = (STS_BIT_AX_MIN + 111);

        public const int SSC_STSBIT_AX_113 = (STS_BIT_AX_MIN + 112);
        public const int SSC_STSBIT_AX_114 = (STS_BIT_AX_MIN + 113);
        public const int SSC_STSBIT_AX_115 = (STS_BIT_AX_MIN + 114);
        public const int SSC_STSBIT_AX_116 = (STS_BIT_AX_MIN + 115);
        public const int SSC_STSBIT_AX_117 = (STS_BIT_AX_MIN + 116);
        public const int SSC_STSBIT_AX_118 = (STS_BIT_AX_MIN + 117);
        public const int SSC_STSBIT_AX_119 = (STS_BIT_AX_MIN + 118);
        public const int SSC_STSBIT_AX_120 = (STS_BIT_AX_MIN + 119);

        public const int SSC_STSBIT_AX_121 = (STS_BIT_AX_MIN + 120);
        public const int SSC_STSBIT_AX_122 = (STS_BIT_AX_MIN + 121);
        public const int SSC_STSBIT_AX_123 = (STS_BIT_AX_MIN + 122);
        public const int SSC_STSBIT_AX_124 = (STS_BIT_AX_MIN + 123);
        public const int SSC_STSBIT_AX_125 = (STS_BIT_AX_MIN + 124);
        public const int SSC_STSBIT_AX_126 = (STS_BIT_AX_MIN + 125);
        public const int SSC_STSBIT_AX_127 = (STS_BIT_AX_MIN + 126);
        public const int SSC_STSBIT_AX_128 = (STS_BIT_AX_MIN + 127);

        public const int SSC_STSBIT_AX_MOUT = (STS_BIT_AX_MIN + 128);
        public const int SSC_STSBIT_AX_MRCH = (STS_BIT_AX_MIN + 129);
        public const int SSC_STSBIT_AX_MER1 = (STS_BIT_AX_MIN + 130);
        public const int SSC_STSBIT_AX_MER2 = (STS_BIT_AX_MIN + 131);
        public const int SSC_STSBIT_AX_MER3 = (STS_BIT_AX_MIN + 132);
        public const int SSC_STSBIT_AX_MER4 = (STS_BIT_AX_MIN + 133);
        public const int SSC_STSBIT_AX_MESV = (STS_BIT_AX_MIN + 134);
        public const int SSC_STSBIT_AX_136 = (STS_BIT_AX_MIN + 135);

        public const int SSC_STSBIT_AX_137 = (STS_BIT_AX_MIN + 136);
        public const int SSC_STSBIT_AX_138 = (STS_BIT_AX_MIN + 137);
        public const int SSC_STSBIT_AX_139 = (STS_BIT_AX_MIN + 138);
        public const int SSC_STSBIT_AX_140 = (STS_BIT_AX_MIN + 139);
        public const int SSC_STSBIT_AX_141 = (STS_BIT_AX_MIN + 140);
        public const int SSC_STSBIT_AX_142 = (STS_BIT_AX_MIN + 141);
        public const int SSC_STSBIT_AX_143 = (STS_BIT_AX_MIN + 142);
        public const int SSC_STSBIT_AX_144 = (STS_BIT_AX_MIN + 143);

        public const int SSC_STSBIT_AX_145 = (STS_BIT_AX_MIN + 144);
        public const int SSC_STSBIT_AX_146 = (STS_BIT_AX_MIN + 145);
        public const int SSC_STSBIT_AX_147 = (STS_BIT_AX_MIN + 146);
        public const int SSC_STSBIT_AX_148 = (STS_BIT_AX_MIN + 147);
        public const int SSC_STSBIT_AX_149 = (STS_BIT_AX_MIN + 148);
        public const int SSC_STSBIT_AX_150 = (STS_BIT_AX_MIN + 149);
        public const int SSC_STSBIT_AX_151 = (STS_BIT_AX_MIN + 150);
        public const int SSC_STSBIT_AX_152 = (STS_BIT_AX_MIN + 151);

        public const int SSC_STSBIT_AX_153 = (STS_BIT_AX_MIN + 152);
        public const int SSC_STSBIT_AX_154 = (STS_BIT_AX_MIN + 153);
        public const int SSC_STSBIT_AX_155 = (STS_BIT_AX_MIN + 154);
        public const int SSC_STSBIT_AX_156 = (STS_BIT_AX_MIN + 155);
        public const int SSC_STSBIT_AX_157 = (STS_BIT_AX_MIN + 156);
        public const int SSC_STSBIT_AX_158 = (STS_BIT_AX_MIN + 157);
        public const int SSC_STSBIT_AX_159 = (STS_BIT_AX_MIN + 158);
        public const int SSC_STSBIT_AX_160 = (STS_BIT_AX_MIN + 159);

        public const int SSC_STSBIT_AX_PWFIN1 = (STS_BIT_AX_MIN + 160);
        public const int SSC_STSBIT_AX_PWEN1 = (STS_BIT_AX_MIN + 161);
        public const int SSC_STSBIT_AX_PWED1 = (STS_BIT_AX_MIN + 162);
        public const int SSC_STSBIT_AX_164 = (STS_BIT_AX_MIN + 163);
        public const int SSC_STSBIT_AX_PWFIN2 = (STS_BIT_AX_MIN + 164);
        public const int SSC_STSBIT_AX_PWEN2 = (STS_BIT_AX_MIN + 165);
        public const int SSC_STSBIT_AX_PWED2 = (STS_BIT_AX_MIN + 166);
        public const int SSC_STSBIT_AX_PSCHG = (STS_BIT_AX_MIN + 167);

        public const int SSC_STSBIT_AX_PRFIN1 = (STS_BIT_AX_MIN + 168);
        public const int SSC_STSBIT_AX_PREN1 = (STS_BIT_AX_MIN + 169);
        public const int SSC_STSBIT_AX_PRFIN2 = (STS_BIT_AX_MIN + 170);
        public const int SSC_STSBIT_AX_PREN2 = (STS_BIT_AX_MIN + 171);
        public const int SSC_STSBIT_AX_173 = (STS_BIT_AX_MIN + 172);
        public const int SSC_STSBIT_AX_174 = (STS_BIT_AX_MIN + 173);
        public const int SSC_STSBIT_AX_175 = (STS_BIT_AX_MIN + 174);
        public const int SSC_STSBIT_AX_176 = (STS_BIT_AX_MIN + 175);

        public const int SSC_STSBIT_AX_177 = (STS_BIT_AX_MIN + 176);
        public const int SSC_STSBIT_AX_178 = (STS_BIT_AX_MIN + 177);
        public const int SSC_STSBIT_AX_179 = (STS_BIT_AX_MIN + 178);
        public const int SSC_STSBIT_AX_180 = (STS_BIT_AX_MIN + 179);
        public const int SSC_STSBIT_AX_181 = (STS_BIT_AX_MIN + 180);
        public const int SSC_STSBIT_AX_182 = (STS_BIT_AX_MIN + 181);
        public const int SSC_STSBIT_AX_183 = (STS_BIT_AX_MIN + 182);
        public const int SSC_STSBIT_AX_184 = (STS_BIT_AX_MIN + 183);

        public const int SSC_STSBIT_AX_185 = (STS_BIT_AX_MIN + 184);
        public const int SSC_STSBIT_AX_186 = (STS_BIT_AX_MIN + 185);
        public const int SSC_STSBIT_AX_187 = (STS_BIT_AX_MIN + 186);
        public const int SSC_STSBIT_AX_188 = (STS_BIT_AX_MIN + 187);
        public const int SSC_STSBIT_AX_189 = (STS_BIT_AX_MIN + 188);
        public const int SSC_STSBIT_AX_190 = (STS_BIT_AX_MIN + 189);
        public const int SSC_STSBIT_AX_191 = (STS_BIT_AX_MIN + 190);
        public const int SSC_STSBIT_AX_192 = (STS_BIT_AX_MIN + 191);

        public const int SSC_STSBIT_AX_193 = (STS_BIT_AX_MIN + 192);
        public const int SSC_STSBIT_AX_194 = (STS_BIT_AX_MIN + 193);
        public const int SSC_STSBIT_AX_195 = (STS_BIT_AX_MIN + 194);
        public const int SSC_STSBIT_AX_196 = (STS_BIT_AX_MIN + 195);
        public const int SSC_STSBIT_AX_197 = (STS_BIT_AX_MIN + 196);
        public const int SSC_STSBIT_AX_198 = (STS_BIT_AX_MIN + 197);
        public const int SSC_STSBIT_AX_199 = (STS_BIT_AX_MIN + 198);
        public const int SSC_STSBIT_AX_200 = (STS_BIT_AX_MIN + 199);

        public const int SSC_STSBIT_AX_201 = (STS_BIT_AX_MIN + 200);
        public const int SSC_STSBIT_AX_202 = (STS_BIT_AX_MIN + 201);
        public const int SSC_STSBIT_AX_203 = (STS_BIT_AX_MIN + 202);
        public const int SSC_STSBIT_AX_204 = (STS_BIT_AX_MIN + 203);
        public const int SSC_STSBIT_AX_205 = (STS_BIT_AX_MIN + 204);
        public const int SSC_STSBIT_AX_206 = (STS_BIT_AX_MIN + 205);
        public const int SSC_STSBIT_AX_207 = (STS_BIT_AX_MIN + 206);
        public const int SSC_STSBIT_AX_208 = (STS_BIT_AX_MIN + 207);

        public const int SSC_STSBIT_AX_209 = (STS_BIT_AX_MIN + 208);
        public const int SSC_STSBIT_AX_210 = (STS_BIT_AX_MIN + 209);
        public const int SSC_STSBIT_AX_211 = (STS_BIT_AX_MIN + 210);
        public const int SSC_STSBIT_AX_212 = (STS_BIT_AX_MIN + 211);
        public const int SSC_STSBIT_AX_213 = (STS_BIT_AX_MIN + 212);
        public const int SSC_STSBIT_AX_214 = (STS_BIT_AX_MIN + 213);
        public const int SSC_STSBIT_AX_215 = (STS_BIT_AX_MIN + 214);
        public const int SSC_STSBIT_AX_216 = (STS_BIT_AX_MIN + 215);

        public const int SSC_STSBIT_AX_217 = (STS_BIT_AX_MIN + 216);
        public const int SSC_STSBIT_AX_218 = (STS_BIT_AX_MIN + 217);
        public const int SSC_STSBIT_AX_219 = (STS_BIT_AX_MIN + 218);
        public const int SSC_STSBIT_AX_220 = (STS_BIT_AX_MIN + 219);
        public const int SSC_STSBIT_AX_221 = (STS_BIT_AX_MIN + 220);
        public const int SSC_STSBIT_AX_222 = (STS_BIT_AX_MIN + 221);
        public const int SSC_STSBIT_AX_223 = (STS_BIT_AX_MIN + 222);
        public const int SSC_STSBIT_AX_224 = (STS_BIT_AX_MIN + 223);

        public const int SSC_STSBIT_AX_225 = (STS_BIT_AX_MIN + 224);
        public const int SSC_STSBIT_AX_226 = (STS_BIT_AX_MIN + 225);
        public const int SSC_STSBIT_AX_227 = (STS_BIT_AX_MIN + 226);
        public const int SSC_STSBIT_AX_228 = (STS_BIT_AX_MIN + 227);
        public const int SSC_STSBIT_AX_229 = (STS_BIT_AX_MIN + 228);
        public const int SSC_STSBIT_AX_230 = (STS_BIT_AX_MIN + 229);
        public const int SSC_STSBIT_AX_231 = (STS_BIT_AX_MIN + 230);
        public const int SSC_STSBIT_AX_232 = (STS_BIT_AX_MIN + 231);

        public const int SSC_STSBIT_AX_233 = (STS_BIT_AX_MIN + 232);
        public const int SSC_STSBIT_AX_234 = (STS_BIT_AX_MIN + 233);
        public const int SSC_STSBIT_AX_235 = (STS_BIT_AX_MIN + 234);
        public const int SSC_STSBIT_AX_236 = (STS_BIT_AX_MIN + 235);
        public const int SSC_STSBIT_AX_237 = (STS_BIT_AX_MIN + 236);
        public const int SSC_STSBIT_AX_238 = (STS_BIT_AX_MIN + 237);
        public const int SSC_STSBIT_AX_239 = (STS_BIT_AX_MIN + 238);
        public const int SSC_STSBIT_AX_240 = (STS_BIT_AX_MIN + 239);

        public const int SSC_STSBIT_AX_241 = (STS_BIT_AX_MIN + 240);
        public const int SSC_STSBIT_AX_242 = (STS_BIT_AX_MIN + 241);
        public const int SSC_STSBIT_AX_243 = (STS_BIT_AX_MIN + 242);
        public const int SSC_STSBIT_AX_244 = (STS_BIT_AX_MIN + 243);
        public const int SSC_STSBIT_AX_245 = (STS_BIT_AX_MIN + 244);
        public const int SSC_STSBIT_AX_246 = (STS_BIT_AX_MIN + 245);
        public const int SSC_STSBIT_AX_247 = (STS_BIT_AX_MIN + 246);
        public const int SSC_STSBIT_AX_248 = (STS_BIT_AX_MIN + 247);

        public const int SSC_STSBIT_AX_249 = (STS_BIT_AX_MIN + 248);
        public const int SSC_STSBIT_AX_250 = (STS_BIT_AX_MIN + 249);
        public const int SSC_STSBIT_AX_251 = (STS_BIT_AX_MIN + 250);
        public const int SSC_STSBIT_AX_252 = (STS_BIT_AX_MIN + 251);
        public const int SSC_STSBIT_AX_253 = (STS_BIT_AX_MIN + 252);
        public const int SSC_STSBIT_AX_254 = (STS_BIT_AX_MIN + 253);
        public const int SSC_STSBIT_AX_255 = (STS_BIT_AX_MIN + 254);
        public const int SSC_STSBIT_AX_256 = (STS_BIT_AX_MIN + 255);

        /*---------------------------------------------------------------------------*/

        /*---------------------------------------------------------------------------*/
        /* [Function]                                                                */
        /*   load library                                                            */
        /*                                                                           */
        /* [Argument]                                                                */
        /*    none                                                                   */
        /*                                                                           */
        /* [Return]                                                                  */
        /*    true                                                                   */
        /*    false                                                                  */
        /*---------------------------------------------------------------------------*/
        static public bool LoadLibraryDll()
        {
            if (Environment.Is64BitProcess)
            {   /* 64 bit OS */
                hDll = LoadLibrary("mc2xxstd_x64.dll");
                if (hDll == IntPtr.Zero)
                {
                    //Copy library and retry.
                    string paths = System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
                    int indexnum = 0;
                    indexnum = paths.IndexOf("\\bin");
                    paths = paths.Remove(indexnum);
                    string FileName = paths + @"\DLL\mc2xxstd_x64.dll";
                    //Copy library and retry.
                    System.IO.File.Copy(FileName, System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName) + @"\mc2xxstd_x64.dll", true);
                    hDll = LoadLibrary("mc2xxstd_x64.dll");
                }
            }
            else
            {   /* 32 bit OS */
                hDll = LoadLibrary("mc2xxstd.dll");

                if (hDll == IntPtr.Zero)
                {
                    //Copy library and retry.
                    string paths = System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
                    int indexnum = 0;
                    indexnum = paths.IndexOf("\\bin");
                    paths = paths.Remove(indexnum);
                    string FileName = paths + @"\DLL\mc2xxstd.dll";
                    System.IO.File.Copy(FileName, System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName) + @"\mc2xxstd.dll", true);
                    hDll = LoadLibrary("mc2xxstd.dll");
                }
            }

            if (hDll == IntPtr.Zero)
            {
                return (false);
            }

            /*%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%*/
            /* mc2xxFuncStd                                                              */
            /*%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%*/
            sscGetLastError = (TYP_sscGetLastError)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscGetLastError"), typeof(TYP_sscGetLastError));
            sscGetMountChannel = (TYP_sscGetMountChannel)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscGetMountChannel"), typeof(TYP_sscGetMountChannel));
            sscOpen = (TYP_sscOpen)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscOpen"), typeof(TYP_sscOpen));
            sscClose = (TYP_sscClose)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscClose"), typeof(TYP_sscClose));
            sscGetControlCycle = (TYP_sscGetControlCycle)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscGetControlCycle"), typeof(TYP_sscGetControlCycle));
            sscGetBoardVersion = (TYP_sscGetBoardVersion)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscGetBoardVersion"), typeof(TYP_sscGetBoardVersion));
            sscGetDriverVersion = (TYP_sscGetDriverVersion)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscGetDriverVersion"), typeof(TYP_sscGetDriverVersion));
            sscGetUnitVersion = (TYP_sscGetUnitVersion)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscGetUnitVersion"), typeof(TYP_sscGetUnitVersion));
            sscGetOperationCycleMonitor = (TYP_sscGetOperationCycleMonitor)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscGetOperationCycleMonitor"), typeof(TYP_sscGetOperationCycleMonitor));
            sscClearOperationCycleMonitor = (TYP_sscClearOperationCycleMonitor)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscClearOperationCycleMonitor"), typeof(TYP_sscClearOperationCycleMonitor));
            sscResetAllParameter = (TYP_sscResetAllParameter)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscResetAllParameter"), typeof(TYP_sscResetAllParameter));
            sscChangeParameter = (TYP_sscChangeParameter)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscChangeParameter"), typeof(TYP_sscChangeParameter));
            sscChange2Parameter = (TYP_sscChange2Parameter)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscChange2Parameter"), typeof(TYP_sscChange2Parameter));
            sscCheckParameter = (TYP_sscCheckParameter)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscCheckParameter"), typeof(TYP_sscCheckParameter));
            sscCheck2Parameter = (TYP_sscCheck2Parameter)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscCheck2Parameter"), typeof(TYP_sscCheck2Parameter));
            sscLoadAllParameterFromFlashROM = (TYP_sscLoadAllParameterFromFlashROM)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscLoadAllParameterFromFlashROM"), typeof(TYP_sscLoadAllParameterFromFlashROM));
            sscSaveAllParameterToFlashROM = (TYP_sscSaveAllParameterToFlashROM)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscSaveAllParameterToFlashROM"), typeof(TYP_sscSaveAllParameterToFlashROM));
            sscCheckSvPrmChangeNumEx = (TYP_sscCheckSvPrmChangeNumEx)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscCheckSvPrmChangeNumEx"), typeof(TYP_sscCheckSvPrmChangeNumEx));
            sscReboot = (TYP_sscReboot)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscReboot"), typeof(TYP_sscReboot));
            sscSystemStart = (TYP_sscSystemStart)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscSystemStart"), typeof(TYP_sscSystemStart));
            sscGetSystemStatusCode = (TYP_sscGetSystemStatusCode)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscGetSystemStatusCode"), typeof(TYP_sscGetSystemStatusCode));
            sscReconnectSSCNET = (TYP_sscReconnectSSCNET)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscReconnectSSCNET"), typeof(TYP_sscReconnectSSCNET));
            sscDisconnectSSCNET = (TYP_sscDisconnectSSCNET)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscDisconnectSSCNET"), typeof(TYP_sscDisconnectSSCNET));
            sscSetCommandBitSignalEx = (TYP_sscSetCommandBitSignalEx)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscSetCommandBitSignalEx"), typeof(TYP_sscSetCommandBitSignalEx));
            sscGetStatusBitSignalEx = (TYP_sscGetStatusBitSignalEx)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscGetStatusBitSignalEx"), typeof(TYP_sscGetStatusBitSignalEx));
            sscWaitStatusBitSignalEx = (TYP_sscWaitStatusBitSignalEx)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscWaitStatusBitSignalEx"), typeof(TYP_sscWaitStatusBitSignalEx));
            sscSetPointDataEx = (TYP_sscSetPointDataEx)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscSetPointDataEx"), typeof(TYP_sscSetPointDataEx));
            sscCheckPointDataEx = (TYP_sscCheckPointDataEx)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscCheckPointDataEx"), typeof(TYP_sscCheckPointDataEx));
            sscSetPointOffset = (TYP_sscSetPointOffset)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscSetPointOffset"), typeof(TYP_sscSetPointOffset));
            sscCheckPointOffset = (TYP_sscCheckPointOffset)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscCheckPointOffset"), typeof(TYP_sscCheckPointOffset));
            sscGetDrivingPointNumber = (TYP_sscGetDrivingPointNumber)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscGetDrivingPointNumber"), typeof(TYP_sscGetDrivingPointNumber));
            sscSetPressData = (TYP_sscSetPressData)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscSetPressData"), typeof(TYP_sscSetPressData));
            sscGetPressData = (TYP_sscGetPressData)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscGetPressData"), typeof(TYP_sscGetPressData));
            sscJogStart = (TYP_sscJogStart)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscJogStart"), typeof(TYP_sscJogStart));
            sscJogStop = (TYP_sscJogStop)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscJogStop"), typeof(TYP_sscJogStop));
            sscJogStopNoWait = (TYP_sscJogStopNoWait)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscJogStopNoWait"), typeof(TYP_sscJogStopNoWait));
            sscIncStart = (TYP_sscIncStart)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscIncStart"), typeof(TYP_sscIncStart));
            sscAutoStart = (TYP_sscAutoStart)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscAutoStart"), typeof(TYP_sscAutoStart));
            sscHomeReturnStart = (TYP_sscHomeReturnStart)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscHomeReturnStart"), typeof(TYP_sscHomeReturnStart));
            sscLinearStart = (TYP_sscLinearStart)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscLinearStart"), typeof(TYP_sscLinearStart));
            sscDataSetStart = (TYP_sscDataSetStart)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscDataSetStart"), typeof(TYP_sscDataSetStart));
            sscDriveStop = (TYP_sscDriveStop)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscDriveStop"), typeof(TYP_sscDriveStop));
            sscDriveStopNoWait = (TYP_sscDriveStopNoWait)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscDriveStopNoWait"), typeof(TYP_sscDriveStopNoWait));
            sscDriveRapidStop = (TYP_sscDriveRapidStop)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscDriveRapidStop"), typeof(TYP_sscDriveRapidStop));
            sscDriveRapidStopNoWait = (TYP_sscDriveRapidStopNoWait)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscDriveRapidStopNoWait"), typeof(TYP_sscDriveRapidStopNoWait));
            sscSetDriveMode = (TYP_sscSetDriveMode)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscSetDriveMode"), typeof(TYP_sscSetDriveMode));
            sscGetDriveMode = (TYP_sscGetDriveMode)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscGetDriveMode"), typeof(TYP_sscGetDriveMode));
            sscGetDriveFinStatus = (TYP_sscGetDriveFinStatus)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscGetDriveFinStatus"), typeof(TYP_sscGetDriveFinStatus));
            sscChangeControlMode = (TYP_sscChangeControlMode)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscChangeControlMode"), typeof(TYP_sscChangeControlMode));
            sscChangeManualPosition = (TYP_sscChangeManualPosition)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscChangeManualPosition"), typeof(TYP_sscChangeManualPosition));
            sscChangeAutoPosition = (TYP_sscChangeAutoPosition)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscChangeAutoPosition"), typeof(TYP_sscChangeAutoPosition));
            sscChangeLinearPosition = (TYP_sscChangeLinearPosition)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscChangeLinearPosition"), typeof(TYP_sscChangeLinearPosition));
            sscChangeManualSpeed = (TYP_sscChangeManualSpeed)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscChangeManualSpeed"), typeof(TYP_sscChangeManualSpeed));
            sscChangeAutoSpeed = (TYP_sscChangeAutoSpeed)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscChangeAutoSpeed"), typeof(TYP_sscChangeAutoSpeed));
            sscChangeManualAccTime = (TYP_sscChangeManualAccTime)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscChangeManualAccTime"), typeof(TYP_sscChangeManualAccTime));
            sscChangeAutoAccTime = (TYP_sscChangeAutoAccTime)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscChangeAutoAccTime"), typeof(TYP_sscChangeAutoAccTime));
            sscChangeManualDecTime = (TYP_sscChangeManualDecTime)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscChangeManualDecTime"), typeof(TYP_sscChangeManualDecTime));
            sscChangeAutoDecTime = (TYP_sscChangeAutoDecTime)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscChangeAutoDecTime"), typeof(TYP_sscChangeAutoDecTime));
            sscGetAlarm = (TYP_sscGetAlarm)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscGetAlarm"), typeof(TYP_sscGetAlarm));
            sscResetAlarm = (TYP_sscResetAlarm)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscResetAlarm"), typeof(TYP_sscResetAlarm));
            sscSetMonitor = (TYP_sscSetMonitor)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscSetMonitor"), typeof(TYP_sscSetMonitor));
            sscStopMonitor = (TYP_sscStopMonitor)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscStopMonitor"), typeof(TYP_sscStopMonitor));
            sscGetMonitor = (TYP_sscGetMonitor)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscGetMonitor"), typeof(TYP_sscGetMonitor));
            sscGetCurrentCmdPositionFast = (TYP_sscGetCurrentCmdPositionFast)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscGetCurrentCmdPositionFast"), typeof(TYP_sscGetCurrentCmdPositionFast));
            sscGetCurrentFbPositionFast = (TYP_sscGetCurrentFbPositionFast)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscGetCurrentFbPositionFast"), typeof(TYP_sscGetCurrentFbPositionFast));
            sscGetIoStatusFast = (TYP_sscGetIoStatusFast)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscGetIoStatusFast"), typeof(TYP_sscGetIoStatusFast));
            sscGetCmdSpeedFast = (TYP_sscGetCmdSpeedFast)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscGetCmdSpeedFast"), typeof(TYP_sscGetCmdSpeedFast));
            sscGetFbSpeedFast = (TYP_sscGetFbSpeedFast)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscGetFbSpeedFast"), typeof(TYP_sscGetFbSpeedFast));
            sscGetCurrentFbFast = (TYP_sscGetCurrentFbFast)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscGetCurrentFbFast"), typeof(TYP_sscGetCurrentFbFast));
            sscGetPositionDroopFast = (TYP_sscGetPositionDroopFast)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscGetPositionDroopFast"), typeof(TYP_sscGetPositionDroopFast));
            sscGetExFastMonitor = (TYP_sscGetExFastMonitor)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscGetExFastMonitor"), typeof(TYP_sscGetExFastMonitor));
            sscWdEnable = (TYP_sscWdEnable)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscWdEnable"), typeof(TYP_sscWdEnable));
            sscWdDisable = (TYP_sscWdDisable)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscWdDisable"), typeof(TYP_sscWdDisable));
            sscChangeWdCounter = (TYP_sscChangeWdCounter)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscChangeWdCounter"), typeof(TYP_sscChangeWdCounter));
            sscSetOtherAxisStartData = (TYP_sscSetOtherAxisStartData)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscSetOtherAxisStartData"), typeof(TYP_sscSetOtherAxisStartData));
            sscGetOtherAxisStartData = (TYP_sscGetOtherAxisStartData)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscGetOtherAxisStartData"), typeof(TYP_sscGetOtherAxisStartData));
            sscOtherAxisStartAbortOn = (TYP_sscOtherAxisStartAbortOn)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscOtherAxisStartAbortOn"), typeof(TYP_sscOtherAxisStartAbortOn));
            sscOtherAxisStartAbortOff = (TYP_sscOtherAxisStartAbortOff)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscOtherAxisStartAbortOff"), typeof(TYP_sscOtherAxisStartAbortOff));
            sscGetOtherAxisStartStatus = (TYP_sscGetOtherAxisStartStatus)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscGetOtherAxisStartStatus"), typeof(TYP_sscGetOtherAxisStartStatus));
            sscSetIntPassPositionData = (TYP_sscSetIntPassPositionData)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscSetIntPassPositionData"), typeof(TYP_sscSetIntPassPositionData));
            sscCheckIntPassPositionData = (TYP_sscCheckIntPassPositionData)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscCheckIntPassPositionData"), typeof(TYP_sscCheckIntPassPositionData));
            sscSetStartingPassNumber = (TYP_sscSetStartingPassNumber)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscSetStartingPassNumber"), typeof(TYP_sscSetStartingPassNumber));
            sscGetExecutingPassNumber = (TYP_sscGetExecutingPassNumber)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscGetExecutingPassNumber"), typeof(TYP_sscGetExecutingPassNumber));
            sscStartSampling = (TYP_sscStartSampling)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscStartSampling"), typeof(TYP_sscStartSampling));
            sscStopSampling = (TYP_sscStopSampling)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscStopSampling"), typeof(TYP_sscStopSampling));
            sscSetSamplingParameter = (TYP_sscSetSamplingParameter)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscSetSamplingParameter"), typeof(TYP_sscSetSamplingParameter));
            sscGetSamplingParameter = (TYP_sscGetSamplingParameter)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscGetSamplingParameter"), typeof(TYP_sscGetSamplingParameter));
            sscGetSamplingError = (TYP_sscGetSamplingError)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscGetSamplingError"), typeof(TYP_sscGetSamplingError));
            sscGetSamplingStatus = (TYP_sscGetSamplingStatus)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscGetSamplingStatus"), typeof(TYP_sscGetSamplingStatus));
            sscGetSamplingData = (TYP_sscGetSamplingData)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscGetSamplingData"), typeof(TYP_sscGetSamplingData));
            sscStartLog = (TYP_sscStartLog)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscStartLog"), typeof(TYP_sscStartLog));
            sscStopLog = (TYP_sscStopLog)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscStopLog"), typeof(TYP_sscStopLog));
            sscCheckLogStatus = (TYP_sscCheckLogStatus)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscCheckLogStatus"), typeof(TYP_sscCheckLogStatus));
            sscCheckLogEventNum = (TYP_sscCheckLogEventNum)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscCheckLogEventNum"), typeof(TYP_sscCheckLogEventNum));
            sscReadLogData = (TYP_sscReadLogData)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscReadLogData"), typeof(TYP_sscReadLogData));
            sscClearLogData = (TYP_sscClearLogData)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscClearLogData"), typeof(TYP_sscClearLogData));
            sscCheckAlarmHistoryEventNum = (TYP_sscCheckAlarmHistoryEventNum)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscCheckAlarmHistoryEventNum"), typeof(TYP_sscCheckAlarmHistoryEventNum));
            sscGetAlarmHistoryData = (TYP_sscGetAlarmHistoryData)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscGetAlarmHistoryData"), typeof(TYP_sscGetAlarmHistoryData));
            sscClearAlarmHistoryData = (TYP_sscClearAlarmHistoryData)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscClearAlarmHistoryData"), typeof(TYP_sscClearAlarmHistoryData));
            sscGetDigitalInputDataBit = (TYP_sscGetDigitalInputDataBit)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscGetDigitalInputDataBit"), typeof(TYP_sscGetDigitalInputDataBit));
            sscGetDigitalInputDataWord = (TYP_sscGetDigitalInputDataWord)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscGetDigitalInputDataWord"), typeof(TYP_sscGetDigitalInputDataWord));
            sscSetDigitalOutputDataBit = (TYP_sscSetDigitalOutputDataBit)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscSetDigitalOutputDataBit"), typeof(TYP_sscSetDigitalOutputDataBit));
            sscSetDigitalOutputDataWord = (TYP_sscSetDigitalOutputDataWord)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscSetDigitalOutputDataWord"), typeof(TYP_sscSetDigitalOutputDataWord));
            sscGetDigitalOutputDataBit = (TYP_sscGetDigitalOutputDataBit)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscGetDigitalOutputDataBit"), typeof(TYP_sscGetDigitalOutputDataBit));
            sscGetDigitalOutputDataWord = (TYP_sscGetDigitalOutputDataWord)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscGetDigitalOutputDataWord"), typeof(TYP_sscGetDigitalOutputDataWord));
            sscSetChar = (TYP_sscSetChar)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscSetChar"), typeof(TYP_sscSetChar));
            sscSetShort = (TYP_sscSetShort)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscSetShort"), typeof(TYP_sscSetShort));
            sscSetLong = (TYP_sscSetLong)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscSetLong"), typeof(TYP_sscSetLong));
            sscSetBlock = (TYP_sscSetBlock)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscSetBlock"), typeof(TYP_sscSetBlock));
            sscGetChar = (TYP_sscGetChar)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscGetChar"), typeof(TYP_sscGetChar));
            sscGetShort = (TYP_sscGetShort)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscGetShort"), typeof(TYP_sscGetShort));
            sscGetLong = (TYP_sscGetLong)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscGetLong"), typeof(TYP_sscGetLong));
            sscGetBlock = (TYP_sscGetBlock)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscGetBlock"), typeof(TYP_sscGetBlock));
            sscGetMarkDetectionData = (TYP_sscGetMarkDetectionData)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscGetMarkDetectionData"), typeof(TYP_sscGetMarkDetectionData));
            sscGetMarkDetectionCounter = (TYP_sscGetMarkDetectionCounter)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscGetMarkDetectionCounter"), typeof(TYP_sscGetMarkDetectionCounter));
            sscClearMarkDetectionData = (TYP_sscClearMarkDetectionData)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscClearMarkDetectionData"), typeof(TYP_sscClearMarkDetectionData));
            sscIfmGetReadErrorCount = (TYP_sscIfmGetReadErrorCount)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscIfmGetReadErrorCount"), typeof(TYP_sscIfmGetReadErrorCount));
            sscIfmSetHomePosition = (TYP_sscIfmSetHomePosition)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscIfmSetHomePosition"), typeof(TYP_sscIfmSetHomePosition));
            sscIfmGetMaximumBufferNumber = (TYP_sscIfmGetMaximumBufferNumber)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscIfmGetMaximumBufferNumber"), typeof(TYP_sscIfmGetMaximumBufferNumber));
            sscIfmGetMaximumBufferNumberEx = (TYP_sscIfmGetMaximumBufferNumberEx)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscIfmGetMaximumBufferNumberEx"), typeof(TYP_sscIfmGetMaximumBufferNumberEx));
            sscIfmRenewLatestBuffer = (TYP_sscIfmRenewLatestBuffer)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscIfmRenewLatestBuffer"), typeof(TYP_sscIfmRenewLatestBuffer));
            sscIfmRenewLatestBufferEx = (TYP_sscIfmRenewLatestBufferEx)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscIfmRenewLatestBufferEx"), typeof(TYP_sscIfmRenewLatestBufferEx));
            sscIfmCheckLatestBuffer = (TYP_sscIfmCheckLatestBuffer)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscIfmCheckLatestBuffer"), typeof(TYP_sscIfmCheckLatestBuffer));
            sscIfmCheckLatestBufferEx = (TYP_sscIfmCheckLatestBufferEx)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscIfmCheckLatestBufferEx"), typeof(TYP_sscIfmCheckLatestBufferEx));
            sscIfmGetTransmitBuffer = (TYP_sscIfmGetTransmitBuffer)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscIfmGetTransmitBuffer"), typeof(TYP_sscIfmGetTransmitBuffer));
            sscIfmGetTransmitBufferEx = (TYP_sscIfmGetTransmitBufferEx)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscIfmGetTransmitBufferEx"), typeof(TYP_sscIfmGetTransmitBufferEx));
            sscIfmTrqSetSpeedLimit = (TYP_sscIfmTrqSetSpeedLimit)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscIfmTrqSetSpeedLimit"), typeof(TYP_sscIfmTrqSetSpeedLimit));
            sscIfmSetControlMode = (TYP_sscIfmSetControlMode)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscIfmSetControlMode"), typeof(TYP_sscIfmSetControlMode));
            sscIfmGetControlMode = (TYP_sscIfmGetControlMode)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscIfmGetControlMode"), typeof(TYP_sscIfmGetControlMode));
            sscIfmGetEventStatusBits = (TYP_sscIfmGetEventStatusBits)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscIfmGetEventStatusBits"), typeof(TYP_sscIfmGetEventStatusBits));


            /*%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%*/
            /* mc2xxFuncInt                                                              */
            /*%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%*/
            sscIntStart = (TYP_sscIntStart)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscIntStart"), typeof(TYP_sscIntStart));
            sscIntEnd = (TYP_sscIntEnd)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscIntEnd"), typeof(TYP_sscIntEnd));
            sscIntEnable = (TYP_sscIntEnable)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscIntEnable"), typeof(TYP_sscIntEnable));
            sscIntDisable = (TYP_sscIntDisable)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscIntDisable"), typeof(TYP_sscIntDisable));
            sscRegisterIntCallback = (TYP_sscRegisterIntCallback)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscRegisterIntCallback"), typeof(TYP_sscRegisterIntCallback));
            sscUnregisterIntCallback = (TYP_sscUnregisterIntCallback)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscUnregisterIntCallback"), typeof(TYP_sscUnregisterIntCallback));
            sscResetIntEvent = (TYP_sscResetIntEvent)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscResetIntEvent"), typeof(TYP_sscResetIntEvent));
            sscSetIntEvent = (TYP_sscSetIntEvent)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscSetIntEvent"), typeof(TYP_sscSetIntEvent));
            sscWaitIntEvent = (TYP_sscWaitIntEvent)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscWaitIntEvent"), typeof(TYP_sscWaitIntEvent));
            sscResetIntEventMulti = (TYP_sscResetIntEventMulti)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscResetIntEventMulti"), typeof(TYP_sscResetIntEventMulti));
            sscSetIntEventMulti = (TYP_sscSetIntEventMulti)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscSetIntEventMulti"), typeof(TYP_sscSetIntEventMulti));
            sscWaitIntEventMulti = (TYP_sscWaitIntEventMulti)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscWaitIntEventMulti"), typeof(TYP_sscWaitIntEventMulti));
            sscResetIntOasEvent = (TYP_sscResetIntOasEvent)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscResetIntOasEvent"), typeof(TYP_sscResetIntOasEvent));
            sscSetIntOasEvent = (TYP_sscSetIntOasEvent)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscSetIntOasEvent"), typeof(TYP_sscSetIntOasEvent));
            sscWaitIntOasEvent = (TYP_sscWaitIntOasEvent)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscWaitIntOasEvent"), typeof(TYP_sscWaitIntOasEvent));
            sscResetIntPassPosition = (TYP_sscResetIntPassPosition)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscResetIntPassPosition"), typeof(TYP_sscResetIntPassPosition));
            sscSetIntPassPosition = (TYP_sscSetIntPassPosition)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscSetIntPassPosition"), typeof(TYP_sscSetIntPassPosition));
            sscWaitIntPassPosition = (TYP_sscWaitIntPassPosition)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscWaitIntPassPosition"), typeof(TYP_sscWaitIntPassPosition));
            sscResetIntDriveFin = (TYP_sscResetIntDriveFin)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscResetIntDriveFin"), typeof(TYP_sscResetIntDriveFin));
            sscSetIntDriveFin = (TYP_sscSetIntDriveFin)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscSetIntDriveFin"), typeof(TYP_sscSetIntDriveFin));
            sscWaitIntDriveFin = (TYP_sscWaitIntDriveFin)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscWaitIntDriveFin"), typeof(TYP_sscWaitIntDriveFin));

            /*%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%*/
            /* mc2xxFuncStd (compatible)                                                 */
            /*%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%*/
            sscGetIntOutMask = (TYP_sscGetIntOutMask)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscGetIntOutMask"), typeof(TYP_sscGetIntOutMask));
            sscGetHWIntOut = (TYP_sscGetHWIntOut)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscGetHWIntOut"), typeof(TYP_sscGetHWIntOut));
            sscCheckChannelReady = (TYP_sscCheckChannelReady)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscCheckChannelReady"), typeof(TYP_sscCheckChannelReady));
            sscSetSystemCommandBitSignal = (TYP_sscSetSystemCommandBitSignal)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscSetSystemCommandBitSignal"), typeof(TYP_sscSetSystemCommandBitSignal));
            sscSetAxisCommandBitSignal = (TYP_sscSetAxisCommandBitSignal)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscSetAxisCommandBitSignal"), typeof(TYP_sscSetAxisCommandBitSignal));
            sscGetSystemStatusBits = (TYP_sscGetSystemStatusBits)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscGetSystemStatusBits"), typeof(TYP_sscGetSystemStatusBits));
            sscGetAxisStatusBits = (TYP_sscGetAxisStatusBits)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscGetAxisStatusBits"), typeof(TYP_sscGetAxisStatusBits));
            sscEmgStopOn = (TYP_sscEmgStopOn)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscEmgStopOn"), typeof(TYP_sscEmgStopOn));
            sscCheckEmgStopOn = (TYP_sscCheckEmgStopOn)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscCheckEmgStopOn"), typeof(TYP_sscCheckEmgStopOn));
            sscCheckEmgStopOnNoWait = (TYP_sscCheckEmgStopOnNoWait)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscCheckEmgStopOnNoWait"), typeof(TYP_sscCheckEmgStopOnNoWait));
            sscEmgStopOff = (TYP_sscEmgStopOff)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscEmgStopOff"), typeof(TYP_sscEmgStopOff));
            sscCheckEmgStopOff = (TYP_sscCheckEmgStopOff)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscCheckEmgStopOff"), typeof(TYP_sscCheckEmgStopOff));
            sscCheckEmgStopOffNoWait = (TYP_sscCheckEmgStopOffNoWait)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscCheckEmgStopOffNoWait"), typeof(TYP_sscCheckEmgStopOffNoWait));
            sscAsynchronousOn = (TYP_sscAsynchronousOn)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscAsynchronousOn"), typeof(TYP_sscAsynchronousOn));
            sscCheckAsynchronousOn = (TYP_sscCheckAsynchronousOn)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscCheckAsynchronousOn"), typeof(TYP_sscCheckAsynchronousOn));
            sscCheckAsynchronousOnNoWait = (TYP_sscCheckAsynchronousOnNoWait)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscCheckAsynchronousOnNoWait"), typeof(TYP_sscCheckAsynchronousOnNoWait));
            sscAsynchronousOff = (TYP_sscAsynchronousOff)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscAsynchronousOff"), typeof(TYP_sscAsynchronousOff));
            sscCheckAsynchronousOff = (TYP_sscCheckAsynchronousOff)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscCheckAsynchronousOff"), typeof(TYP_sscCheckAsynchronousOff));
            sscCheckAsynchronousOffNoWait = (TYP_sscCheckAsynchronousOffNoWait)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscCheckAsynchronousOffNoWait"), typeof(TYP_sscCheckAsynchronousOffNoWait));
            sscCheckSyncMatchExecOn = (TYP_sscCheckSyncMatchExecOn)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscCheckSyncMatchExecOn"), typeof(TYP_sscCheckSyncMatchExecOn));
            sscCheckSyncMatchExecOnNoWait = (TYP_sscCheckSyncMatchExecOnNoWait)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscCheckSyncMatchExecOnNoWait"), typeof(TYP_sscCheckSyncMatchExecOnNoWait));
            sscCheckSyncMatchExecOff = (TYP_sscCheckSyncMatchExecOff)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscCheckSyncMatchExecOff"), typeof(TYP_sscCheckSyncMatchExecOff));
            sscCheckSyncMatchExecOffNoWait = (TYP_sscCheckSyncMatchExecOffNoWait)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscCheckSyncMatchExecOffNoWait"), typeof(TYP_sscCheckSyncMatchExecOffNoWait));
            sscServoOn = (TYP_sscServoOn)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscServoOn"), typeof(TYP_sscServoOn));
            sscCheckServoOn = (TYP_sscCheckServoOn)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscCheckServoOn"), typeof(TYP_sscCheckServoOn));
            sscCheckServoOnNoWait = (TYP_sscCheckServoOnNoWait)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscCheckServoOnNoWait"), typeof(TYP_sscCheckServoOnNoWait));
            sscServoOff = (TYP_sscServoOff)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscServoOff"), typeof(TYP_sscServoOff));
            sscCheckServoOff = (TYP_sscCheckServoOff)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscCheckServoOff"), typeof(TYP_sscCheckServoOff));
            sscCheckServoOffNoWait = (TYP_sscCheckServoOffNoWait)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscCheckServoOffNoWait"), typeof(TYP_sscCheckServoOffNoWait));
            sscTorqueLimitOn = (TYP_sscTorqueLimitOn)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscTorqueLimitOn"), typeof(TYP_sscTorqueLimitOn));
            sscCheckTorqueLimitCrampOn = (TYP_sscCheckTorqueLimitCrampOn)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscCheckTorqueLimitCrampOn"), typeof(TYP_sscCheckTorqueLimitCrampOn));
            sscCheckTorqueLimitCrampOnNoWait = (TYP_sscCheckTorqueLimitCrampOnNoWait)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscCheckTorqueLimitCrampOnNoWait"), typeof(TYP_sscCheckTorqueLimitCrampOnNoWait));
            sscTorqueLimitOff = (TYP_sscTorqueLimitOff)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscTorqueLimitOff"), typeof(TYP_sscTorqueLimitOff));
            sscCheckTorqueLimitCrampOff = (TYP_sscCheckTorqueLimitCrampOff)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscCheckTorqueLimitCrampOff"), typeof(TYP_sscCheckTorqueLimitCrampOff));
            sscCheckTorqueLimitCrampOffNoWait = (TYP_sscCheckTorqueLimitCrampOffNoWait)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscCheckTorqueLimitCrampOffNoWait"), typeof(TYP_sscCheckTorqueLimitCrampOffNoWait));
            sscCheckPositionSwitchOn = (TYP_sscCheckPositionSwitchOn)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscCheckPositionSwitchOn"), typeof(TYP_sscCheckPositionSwitchOn));
            sscCheckPositionSwitchOnNoWait = (TYP_sscCheckPositionSwitchOnNoWait)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscCheckPositionSwitchOnNoWait"), typeof(TYP_sscCheckPositionSwitchOnNoWait));
            sscCheckPositionSwitchOff = (TYP_sscCheckPositionSwitchOff)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscCheckPositionSwitchOff"), typeof(TYP_sscCheckPositionSwitchOff));
            sscCheckPositionSwitchOffNoWait = (TYP_sscCheckPositionSwitchOffNoWait)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscCheckPositionSwitchOffNoWait"), typeof(TYP_sscCheckPositionSwitchOffNoWait));
            sscInterlockOn = (TYP_sscInterlockOn)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscInterlockOn"), typeof(TYP_sscInterlockOn));
            sscCheckInterlockStopOn = (TYP_sscCheckInterlockStopOn)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscCheckInterlockStopOn"), typeof(TYP_sscCheckInterlockStopOn));
            sscCheckInterlockStopOnNoWait = (TYP_sscCheckInterlockStopOnNoWait)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscCheckInterlockStopOnNoWait"), typeof(TYP_sscCheckInterlockStopOnNoWait));
            sscInterlockOff = (TYP_sscInterlockOff)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscInterlockOff"), typeof(TYP_sscInterlockOff));
            sscCheckInterlockStopOff = (TYP_sscCheckInterlockStopOff)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscCheckInterlockStopOff"), typeof(TYP_sscCheckInterlockStopOff));
            sscCheckInterlockStopOffNoWait = (TYP_sscCheckInterlockStopOffNoWait)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscCheckInterlockStopOffNoWait"), typeof(TYP_sscCheckInterlockStopOffNoWait));
            sscCheckStopPositionOverOn = (TYP_sscCheckStopPositionOverOn)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscCheckStopPositionOverOn"), typeof(TYP_sscCheckStopPositionOverOn));
            sscCheckStopPositionOverOnNoWait = (TYP_sscCheckStopPositionOverOnNoWait)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscCheckStopPositionOverOnNoWait"), typeof(TYP_sscCheckStopPositionOverOnNoWait));
            sscCheckStopPositionOverOff = (TYP_sscCheckStopPositionOverOff)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscCheckStopPositionOverOff"), typeof(TYP_sscCheckStopPositionOverOff));
            sscCheckStopPositionOverOffNoWait = (TYP_sscCheckStopPositionOverOffNoWait)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscCheckStopPositionOverOffNoWait"), typeof(TYP_sscCheckStopPositionOverOffNoWait));
            sscGainChangeOn = (TYP_sscGainChangeOn)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscGainChangeOn"), typeof(TYP_sscGainChangeOn));
            sscCheckGainChangeOn = (TYP_sscCheckGainChangeOn)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscCheckGainChangeOn"), typeof(TYP_sscCheckGainChangeOn));
            sscCheckGainChangeOnNoWait = (TYP_sscCheckGainChangeOnNoWait)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscCheckGainChangeOnNoWait"), typeof(TYP_sscCheckGainChangeOnNoWait));
            sscGainChangeOff = (TYP_sscGainChangeOff)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscGainChangeOff"), typeof(TYP_sscGainChangeOff));
            sscCheckGainChangeOff = (TYP_sscCheckGainChangeOff)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscCheckGainChangeOff"), typeof(TYP_sscCheckGainChangeOff));
            sscCheckGainChangeOffNoWait = (TYP_sscCheckGainChangeOffNoWait)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscCheckGainChangeOffNoWait"), typeof(TYP_sscCheckGainChangeOffNoWait));
            sscCheckTorqueLimitSelectOn = (TYP_sscCheckTorqueLimitSelectOn)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscCheckTorqueLimitSelectOn"), typeof(TYP_sscCheckTorqueLimitSelectOn));
            sscCheckTorqueLimitSelectOnNoWait = (TYP_sscCheckTorqueLimitSelectOnNoWait)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscCheckTorqueLimitSelectOnNoWait"), typeof(TYP_sscCheckTorqueLimitSelectOnNoWait));
            sscCheckTorqueLimitSelectOff = (TYP_sscCheckTorqueLimitSelectOff)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscCheckTorqueLimitSelectOff"), typeof(TYP_sscCheckTorqueLimitSelectOff));
            sscCheckTorqueLimitSelectOffNoWait = (TYP_sscCheckTorqueLimitSelectOffNoWait)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscCheckTorqueLimitSelectOffNoWait"), typeof(TYP_sscCheckTorqueLimitSelectOffNoWait));
            sscCheckInterfareWaitOn = (TYP_sscCheckInterfareWaitOn)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscCheckInterfareWaitOn"), typeof(TYP_sscCheckInterfareWaitOn));
            sscCheckInterfareWaitOnNoWait = (TYP_sscCheckInterfareWaitOnNoWait)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscCheckInterfareWaitOnNoWait"), typeof(TYP_sscCheckInterfareWaitOnNoWait));
            sscCheckInterfareWaitOff = (TYP_sscCheckInterfareWaitOff)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscCheckInterfareWaitOff"), typeof(TYP_sscCheckInterfareWaitOff));
            sscCheckInterfareWaitOffNoWait = (TYP_sscCheckInterfareWaitOffNoWait)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscCheckInterfareWaitOffNoWait"), typeof(TYP_sscCheckInterfareWaitOffNoWait));
            sscSetPointData = (TYP_sscSetPointData)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscSetPointData"), typeof(TYP_sscSetPointData));
            sscCheckPointData = (TYP_sscCheckPointData)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscCheckPointData"), typeof(TYP_sscCheckPointData));
            sscGetControlAlarmCode = (TYP_sscGetControlAlarmCode)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscGetControlAlarmCode"), typeof(TYP_sscGetControlAlarmCode));
            sscGetOperationAlarmCode = (TYP_sscGetOperationAlarmCode)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscGetOperationAlarmCode"), typeof(TYP_sscGetOperationAlarmCode));
            sscGetExOperationAlarmCode = (TYP_sscGetExOperationAlarmCode)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscGetExOperationAlarmCode"), typeof(TYP_sscGetExOperationAlarmCode));
            sscGetServoAlarmCode = (TYP_sscGetServoAlarmCode)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscGetServoAlarmCode"), typeof(TYP_sscGetServoAlarmCode));
            sscControlAlarmReset = (TYP_sscControlAlarmReset)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscControlAlarmReset"), typeof(TYP_sscControlAlarmReset));
            sscOperationAlarmReset = (TYP_sscOperationAlarmReset)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscOperationAlarmReset"), typeof(TYP_sscOperationAlarmReset));
            sscServoAlarmReset = (TYP_sscServoAlarmReset)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscServoAlarmReset"), typeof(TYP_sscServoAlarmReset));
            sscGetEmgStatus = (TYP_sscGetEmgStatus)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscGetEmgStatus"), typeof(TYP_sscGetEmgStatus));
            sscGetAxisFixConf = (TYP_sscGetAxisFixConf)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscGetAxisFixConf"), typeof(TYP_sscGetAxisFixConf));

            /*%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%*/
            /* mc2xxFuncInt (compatible)                                                 */
            /*%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%*/
            sscSetIntFactor = (TYP_sscSetIntFactor)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscSetIntFactor"), typeof(TYP_sscSetIntFactor));
            sscGetIntFactor = (TYP_sscGetIntFactor)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscGetIntFactor"), typeof(TYP_sscGetIntFactor));
            sscTerminateInt = (TYP_sscTerminateInt)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscTerminateInt"), typeof(TYP_sscTerminateInt));
            sscClearInt = (TYP_sscClearInt)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscClearInt"), typeof(TYP_sscClearInt));
            sscSetPtpEndType = (TYP_sscSetPtpEndType)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscSetPtpEndType"), typeof(TYP_sscSetPtpEndType));
            sscResetEndChk = (TYP_sscResetEndChk)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscResetEndChk"), typeof(TYP_sscResetEndChk));
            sscEndChk = (TYP_sscEndChk)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hDll, "sscEndChk"), typeof(TYP_sscEndChk));

            if (sscGetLastError == null)
            {
                return (false);
            }

            return (true);
        }

        /*---------------------------------------------------------------------------*/

        /*---------------------------------------------------------------------------*/
        /* [Function]                                                                */
        /*   free library                                                            */
        /*                                                                           */
        /* [Argument]                                                                */
        /*    none                                                                   */
        /*                                                                           */
        /* [Return]                                                                  */
        /*    true                                                                   */
        /*    false                                                                  */
        /*---------------------------------------------------------------------------*/
        static public bool FreeLibraryDll()
        {
            if (hDll == IntPtr.Zero)
            {
                return (false);
            }

            FreeLibrary(hDll);
            return (true);
        }
    }
}
