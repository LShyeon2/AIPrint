/*---------------------------------------------------------------------------*/
/*  PROGRAM:      SSC_INTERRUPT_DRIVE                                        */
/*  NAME:         ssc Serov DriveS.cs                                        */
/*  DESCRIPTION:  ssc interrupt drive  source file                           */
/*                                                                           */
/*  Copyright (C) 2015 Toptec Corporation                                    */
/*  All Rights Reserved                                                      */
/*---------------------------------------------------------------------------*/
using mc2xxstd;
using BoxPrint.Log;
using System;
using System.Collections.Generic;
using System.Threading;

namespace BoxPrint.SSCNet
{
    public class SSCInterruptDrive : IDisposable
    {
        private const int AXIS_MAX = 32;
        private const int AXIS_MIN = 1;
        private const int DRIVE_FIN_TIMEOUT = 10000;
        private const int RDY_ON_TIMEOUT = 10000;
        private const short THREAD_PRIORITY_TIME_CRITICAL = 15;
        private const short INTERRUPT_THREAD_PRIORITY = THREAD_PRIORITY_TIME_CRITICAL;

        public int SSC_FIN_STS_RDY = (0);
        public int SSC_FIN_STS_STP = (1);
        public int SSC_FIN_STS_MOV = (2);
        public int SSC_FIN_STS_ALM_STP = (3);
        public int SSC_FIN_STS_ALM_MOV = (4);


        const int MOVEONDE = 0;
        const int MOVEE = 1;

        static int board_id = 0;
        static int channel = 1;
        public int exist_axis; //= 0x00000005;			/* exist axis flag [ bit0: axis1, bit1: axis2, ... bit31: axis32 ]	*/

        private int maxDefultParaCount = 100;

        public const int SSC_BIT_LSP = (0x0001);
        public const int SSC_BIT_LSN = (0x0002);
        public const int SSC_BIT_DOG = (0x0004);

        public short[] jogAccSpeed = new short[32];
        public short[] jogDccSpeed = new short[32];
        public int[] PreMoveSpeed = new int[32];
        public int[] CenteringSpeed = new int[32];
        public int[] MoveSpeed = new int[32];

        public int[] MoveAcc = new int[32];
        public int[] MoveDcc = new int[32];

        public int[] CurPostion = new int[32];

        public int[] pointPrePostion = new int[32];
        public int[] pointUnCenteringPostion = new int[32];

        public short[] ABS24D = new short[32];
        public short[] ABS24E = new short[32];
        public short[] ABS24F = new short[32];

        //static int dufultmaxParaCount = 0;

        public short[] wC_OPAlarm = new short[32];
        public short[] wC_SVAlarm = new short[32];
        public short wC_ChAlarm = 0;

        public short[] lw_DogSensorStatus = new short[32];

        public bool[] lw_DogSensoLSP = new bool[32];
        public bool[] lw_DogSensoLSN = new bool[32];
        public bool[] lw_DogSensoLSH = new bool[32];

        public int[,] wC_MotionIO = new int[32, 32];

        long[] dC_PosFeedback = new long[32];
        long[] dC_PosDrop = new long[32];
        long[] dC_SpdFeedback = new long[32];
        long[] dC_RealTorque = new long[32];
        long[] dC_PeekTorque = new long[32];

        public short[] iservoOnState = new short[32];

        bool[,] bC_ServoState = new bool[32, 20];

        public Dictionary<string, object> PostionData { get; set; }

        public short SerovAlarmCode { get; set; }
        //public SqlManager sql { get; set; }

        private string ServoConfigfile;
        private string ServoPAini;

        public struct PRM_TBL
        {
            public int axis_num;					/* axis number [ 0: system, 1-32: axis ]							*/
            public short prm_num;					/* parameter number													*/
            public short prm_data;					/* parameter data													*/

            public PRM_TBL(int ax_num, short p_num, short p_data)
            {
                axis_num = ax_num;
                prm_num = p_num;
                prm_data = p_data;
            }
        };

        PRM_TBL[] prm_tbl = new PRM_TBL[500];


        public void Dispose()
        {
            LogManager.WriteServoLog(eLogLevel.Info, "서보 시스템을 종료합니다.");
            sClosedServo();
        }

        public SSCInterruptDrive(string configFile, string Parafile)
        {
            ServoConfigfile = configFile;
            ServoPAini = Parafile;
        }

        /*---------------------------------------------------------------------------*/
        /* [Function]                                                                */
        /*   main                                                                    */
        /*                                                                           */
        /* [Argument]                                                                */
        /*    none                                                                   */
        /*                                                                           */
        /* [Return]                                                                  */
        /*    none                                                                   */
        /*---------------------------------------------------------------------------*/
        public int sscMainStart()
        {
            bool load_lib_flg;
            int ans;

            /* load libfary */
            load_lib_flg = sLoadLibDLL();
            if (load_lib_flg == false)
            {
                return (0);
            }

            this.GetServoParaconfig();

            /* start sscnet system */
            ans = StartSscnet();
            if (ans != SscApi.SSC_OK)
            {
                ///* stop sscnet system */
                CloseSscnet();
                ///* free libfary */
                SscApi.FreeLibraryDll();
                LogManager.WriteServoLog(eLogLevel.Error, "sscMainStart Fail.");
                return ans;
            }
            return ans;
        }


        public bool sLoadLibDLL()
        {
            bool load_lib_flg;
            load_lib_flg = SscApi.LoadLibraryDll();
            return load_lib_flg;

        }

        public void sClosedServo()
        {
            /* stop sscnet system */
            CloseSscnet();

            /* free library */
            SscApi.FreeLibraryDll();
        }

        /*---------------------------------------------------------------------------*/
        /* [Function]                                                                */
        /*   start sscnet system                                                     */
        /*                                                                           */
        /* [Argument]                                                                */
        /*    none                                                                   */
        /*                                                                           */
        /* [Return]                                                                  */
        /*    SSC_OK : function succeeded                                            */
        /*    SSC_NG : function failed                                               */
        /*---------------------------------------------------------------------------*/
        private int StartSscnet()
        {
            int ans = 0;
            ushort code;
            ushort detail_code;


            /* open device driver */
            ans = sOpen();
            if (ans != SscApi.SSC_OK)
            {
                LogManager.WriteServoLog(eLogLevel.Info, String.Format("sscOpen failure. sscGetLastError=0x{0:X8}", SscApi.sscGetLastError()));
                return (SscApi.SSC_NG);
            }

            /* reboot system */
            ans = sReboot(ans);
            if (ans != SscApi.SSC_OK)
            {
                LogManager.WriteServoLog(eLogLevel.Info, String.Format("sscReboot failure. sscGetLastError=0x{0:X8}", SscApi.sscGetLastError()));
                return (SscApi.SSC_NG);
            }

            /* set default parameter */
            ans = sResetAllParameter();
            if (ans != SscApi.SSC_OK)
            {
                LogManager.WriteServoLog(eLogLevel.Info, String.Format("sscResetAllParameter failure. sscGetLastError. sscGetLastError=0x{0:X8}", SscApi.sscGetLastError()));
                return (SscApi.SSC_NG);
            }

            this.sSetParameter();

            ans = sChangeParameter(ans);
            if (ans != SscApi.SSC_OK)
            {
                LogManager.WriteServoLog(eLogLevel.Info, String.Format("sscChangeParameter failure. sscGetLastError=0x{0:X8}", SscApi.sscGetLastError()));
                return (SscApi.SSC_NG);
            }


            /* start system */
            ans = sSystemStart(ans);
            if (ans != SscApi.SSC_OK)
            {
                LogManager.WriteServoLog(eLogLevel.Info, String.Format("sscSystemStart failure.. sscGetLastError=0x{0:X8}", SscApi.sscGetLastError()));
                return (SscApi.SSC_NG);
            }

            /* check control alarm */
            ans = sGetAlarm(out code, out detail_code);
            if (ans != SscApi.SSC_OK)
            {
                LogManager.WriteServoLog(eLogLevel.Error, "sscGetAlarm(system alarm) failure sscGetLastError=0x{0:X8}", SscApi.sscGetLastError());

                return (SscApi.SSC_NG);
            }
            else if (code != 0)
            {
                /*======================================================*/
                /* Please add processing to this position if necessary. */
                /*======================================================*/
                LogManager.WriteServoLog(eLogLevel.Error, "system alarm : 0x{0:X2}(0x{1:X2})}", code, detail_code);

                LogManager.WriteServoLog(eLogLevel.Info, String.Format("system alarm : 0x{0:X2}(0x{1:X2})}", code, detail_code));
                return (SscApi.SSC_NG);
            }


            ans = sAlarmCheck();
            if (ans != SscApi.SSC_OK)
            {
                LogManager.WriteServoLog(eLogLevel.Error, "sAlarmCheck failure sscGetLastError=0x{0:X8}", SscApi.sscGetLastError());
                LogManager.WriteServoLog(eLogLevel.Info, String.Format("sAlarmCheck failure sscGetLastError=0x{0:X8}", SscApi.sscGetLastError()));
                return (SscApi.SSC_NG);
            }

            /* start device driver interrupt */
            ans = sIntStart(ans);
            if (ans != SscApi.SSC_OK)
            {
                LogManager.WriteServoLog(eLogLevel.Error, "sIntStart failure sscGetLastError=0x{0:X8}", SscApi.sscGetLastError());
                return (SscApi.SSC_NG);
            }

            /* enable position board interrupt */
            ans = sintEnable(ans);
            if (ans != SscApi.SSC_OK)
            {
                LogManager.WriteServoLog(eLogLevel.Error, "sintEnable failure sscGetLastError=0x{0:X8}", SscApi.sscGetLastError());
                return (SscApi.SSC_NG);
            }

            /*======================================================*/
            /* Please add processing to this position if necessary. */
            /*======================================================*/

            return (SscApi.SSC_OK);
        }

        public int sAlarmCheck()
        {
            int ans;
            int axis_cnt;
            ushort code;
            ushort detail_code;

            /* check axis alarm */
            for (axis_cnt = 0; axis_cnt < exist_axis; axis_cnt++)
            {
                if ((exist_axis & (1 << axis_cnt)) == SscApi.SSC_BIT_OFF)
                {
                    continue;
                }

                /* check operation alarm */
                ans = SscApi.sscGetAlarm(board_id, channel, axis_cnt + 1, SscApi.SSC_ALARM_OPERATION, out code, out detail_code);

                //AlarmCode = code;

                if (ans != SscApi.SSC_OK)
                {
                    LogManager.WriteServoLog(eLogLevel.Error, "sscGetAlarm(operation alarm) failure sscGetLastError=0x{0:X8}", SscApi.sscGetLastError());
                    return (SscApi.SSC_NG);
                }
                //else if (code != 0)
                //{
                //    /*======================================================*/
                //    /* Please add processing to this position if necessary. */
                //    /*======================================================*/

                //    Console.WriteLine("INFO", "operation alarm : 0x{0:X2}(0x{1:X2})", code, detail_code);
                //    return (SscApi.SSC_NG);
                //}

                /* check servo alarm */
                ans = SscApi.sscGetAlarm(board_id, channel, axis_cnt + 1, SscApi.SSC_ALARM_SERVO, out code, out detail_code);

                //AlarmCode = code;
                if (ans != SscApi.SSC_OK)
                {
                    LogManager.WriteServoLog(eLogLevel.Error, "sscGetAlarm(operation alarm) failure sscGetLastError=0x{0:X8}", SscApi.sscGetLastError());
                    return (SscApi.SSC_NG);
                }
                else if (code != 0)
                {
                    /*======================================================*/
                    /* Please add processing to this position if necessary. */
                    /*======================================================*/

                    short code1;
                    SscApi.sscGetSystemStatusCode(0, 1, out code1);
                    LogManager.WriteServoLog(eLogLevel.Error, "sscSystemStart failure sscGetLastError {0}", code1);

                    Console.WriteLine("INFO", "servo alarm : 0x{0:X2}(0x{1:X2})", code, detail_code);
                    return (SscApi.SSC_NG);
                }
            }

            return SscApi.SSC_OK;
        }


        public int sintEnable(int ans)
        {
            ans = SscApi.sscIntEnable(board_id, channel);
            if (ans != SscApi.SSC_OK)
            {
                LogManager.WriteServoLog(eLogLevel.Error, "sscIntEnable failure sscGetLastError=0x{0:X8}", SscApi.sscGetLastError());
                return (SscApi.SSC_NG);
            }
            return ans;
        }

        public void sSetParameter()
        {
            this.sResetAllParameter();

            this.LoadParameterValue();

        }
        public int sGetParameter(int Axis, short ParaNum, out short ParaValue)
        {
            int ans = SscApi.sscCheckParameter(board_id, channel, Axis, ParaNum, out ParaValue);
            if (ans != SscApi.SSC_OK)
            {
                LogManager.WriteServoLog(eLogLevel.Error, "sscIntEnable failure sscGetLastError=0x{0:X8}", SscApi.sscGetLastError());
            }
            return ans;
        }

        public int sIntStart(int ans)
        {
            ans = SscApi.sscIntStart(board_id, INTERRUPT_THREAD_PRIORITY);
            if (ans != SscApi.SSC_OK)
            {
                LogManager.WriteServoLog(eLogLevel.Error, "sscIntStart failure sscGetLastError=0x{0:X8}", SscApi.sscGetLastError());
                return (SscApi.SSC_NG);
            }
            return ans;
        }
        // sGetAlarm 필요없는 첫번째 ans 인수 삭제.
        //시스템 알람 정보를 가져온다.
        public int sGetAlarm(out ushort code, out ushort detail_code)
        {
            int ans = SscApi.sscGetAlarm(board_id, channel, 0, SscApi.SSC_ALARM_SYSTEM, out code, out detail_code);
            if (ans != SscApi.SSC_OK)
            {
                LogManager.WriteServoLog(eLogLevel.Error, "sGetAlarm failure sscGetLastError=0x{0:X8}", SscApi.sscGetLastError());
                return (SscApi.SSC_NG);
            }
            return ans;
        }
        //서보 알람 정보를 가져온다.
        public int sGetServoAlarm(int AxixNum, out ushort code, out ushort detail_code)
        {
            int ans = SscApi.sscGetAlarm(board_id, channel, AxixNum, SscApi.SSC_ALARM_SERVO, out code, out detail_code);
            if (ans != SscApi.SSC_OK)
            {
                LogManager.WriteServoLog(eLogLevel.Info, String.Format("sscGetAlarm failure sscGetLastError=0x{0:X8}", SscApi.sscGetLastError()));
                LogManager.WriteServoLog(eLogLevel.Fatal, String.Format("sGetServoAlarm failure sscGetLastError=0x{0:X8}", SscApi.sscGetLastError()));
                return (SscApi.SSC_NG);
            }
            return ans;
        }
        //운전 알람 정보를 가져온다.
        public int sGetOperAlarm(int AxisNum, out ushort code, out ushort detail_code)
        {
            int ans = SscApi.sscGetAlarm(board_id, channel, AxisNum, SscApi.SSC_ALARM_OPERATION, out code, out detail_code);
            if (ans != SscApi.SSC_OK)
            {
                LogManager.WriteServoLog(eLogLevel.Info, String.Format("sscGetAlarm failure sscGetLastError=0x{0:X8}", SscApi.sscGetLastError()));
                LogManager.WriteServoLog(eLogLevel.Fatal, String.Format("sGetOperAlarm failure sscGetLastError=0x{0:X8}", SscApi.sscGetLastError()));
                return (SscApi.SSC_NG);
            }
            return ans;
        }


        public int sSystemStart(int ans)
        {
            ans = SscApi.sscSystemStart(board_id, channel, SscApi.SSC_DEFAULT_TIMEOUT);
            if (ans != SscApi.SSC_OK)
            {
                LogManager.WriteServoLog(eLogLevel.Error, "sscSystemStart failure sscGetLastError=0x{0:X8}", SscApi.sscGetLastError());

                short code;
                SscApi.sscGetSystemStatusCode(0, 1, out code);
                LogManager.WriteServoLog(eLogLevel.Error, "sscSystemStart failure sscGetLastError {0}", code);

                return (SscApi.SSC_NG);
            }
            return ans;
        }

        public int sChangeParameter(int ans)
        {
            ans = SscApi.sscChangeParameter(board_id, channel, 0, 14, 0x5AE1); //외부 EMID 무효화
            /* change parameter */
            foreach (PRM_TBL prm_tbl_tmp in prm_tbl)
            {
                ans = SscApi.sscChangeParameter(board_id, channel, prm_tbl_tmp.axis_num, prm_tbl_tmp.prm_num, prm_tbl_tmp.prm_data);
                if (ans != SscApi.SSC_OK)
                {
                    LogManager.WriteServoLog(eLogLevel.Error, "ssChangeParameter failure sscGetLastError=0x{0:X8}", SscApi.sscGetLastError());
                    return (SscApi.SSC_NG);
                }
            }
            return ans;
        }

        public int sResetAllParameter()
        {
            int ans;

            ans = SscApi.sscResetAllParameter(board_id, channel, SscApi.SSC_DEFAULT_TIMEOUT);
            if (ans != SscApi.SSC_OK)
            {
                LogManager.WriteServoLog(eLogLevel.Error, "sscResetAllParameter failure sscGetLastError=0x{0:X8}", SscApi.sscGetLastError());
            }

            return ans;
        }

        public int sReboot(int ans)
        {
            ans = SscApi.sscReboot(board_id, channel, SscApi.SSC_DEFAULT_TIMEOUT);
            if (ans != SscApi.SSC_OK)
            {
                LogManager.WriteServoLog(eLogLevel.Error, "sscReboot failure sscGetLastError=0x{0:X8}", SscApi.sscGetLastError());
            }
            return ans;
        }


        public int sMReboot(int ans)
        {
            //int ans = 0;
            ushort code;
            ushort detail_code;


            /* reboot system */
            ans = sReboot(ans);
            if (ans != SscApi.SSC_OK)
            {
                LogManager.WriteServoLog(eLogLevel.Info, String.Format("sscReboot failure. sscGetLastError=0x{0:X8}", SscApi.sscGetLastError()));
                return (SscApi.SSC_NG);
            }

            /* set default parameter */
            ans = sResetAllParameter();
            if (ans != SscApi.SSC_OK)
            {
                LogManager.WriteServoLog(eLogLevel.Info, String.Format("sscResetAllParameter failure. sscGetLastError. sscGetLastError=0x{0:X8}", SscApi.sscGetLastError()));
                return (SscApi.SSC_NG);
            }

            this.sSetParameter();

            ans = sChangeParameter(ans);
            if (ans != SscApi.SSC_OK)
            {
                LogManager.WriteServoLog(eLogLevel.Info, String.Format("sscChangeParameter failure. sscGetLastError=0x{0:X8}", SscApi.sscGetLastError()));
                return (SscApi.SSC_NG);
            }


            /* start system */
            ans = sSystemStart(ans);
            if (ans != SscApi.SSC_OK)
            {
                LogManager.WriteServoLog(eLogLevel.Info, String.Format("sscSystemStart failure.. sscGetLastError=0x{0:X8}", SscApi.sscGetLastError()));
                return (SscApi.SSC_NG);
            }

            /* check control alarm */
            ans = sGetAlarm(out code, out detail_code);
            if (ans != SscApi.SSC_OK)
            {
                LogManager.WriteServoLog(eLogLevel.Error, "sscGetAlarm(system alarm) failure sscGetLastError=0x{0:X8}", SscApi.sscGetLastError());
                return (SscApi.SSC_NG);
            }
            else if (code != 0)
            {
                /*======================================================*/
                /* Please add processing to this position if necessary. */
                /*======================================================*/
                LogManager.WriteServoLog(eLogLevel.Error, "system alarm : 0x{0:X2}(0x{1:X2})}", code, detail_code);
                LogManager.WriteServoLog(eLogLevel.Info, String.Format("system alarm : 0x{0:X2}(0x{1:X2})}", code, detail_code));
                return (SscApi.SSC_NG);
            }


            ans = sAlarmCheck();
            if (ans != SscApi.SSC_OK)
            {
                LogManager.WriteServoLog(eLogLevel.Error, "sAlarmCheck failure sscGetLastError=0x{0:X8}", SscApi.sscGetLastError());
                return (SscApi.SSC_NG);
            }

            /* start device driver interrupt */
            ans = sIntStart(ans);
            if (ans != SscApi.SSC_OK)
            {
                LogManager.WriteServoLog(eLogLevel.Error, "sIntStart failure sscGetLastError=0x{0:X8}", SscApi.sscGetLastError());
                return (SscApi.SSC_NG);
            }

            /* enable position board interrupt */
            ans = sintEnable(ans);
            if (ans != SscApi.SSC_OK)
            {
                LogManager.WriteServoLog(eLogLevel.Error, "sintEnable failure sscGetLastError=0x{0:X8}", SscApi.sscGetLastError());
                return (SscApi.SSC_NG);
            }

            /*======================================================*/
            /* Please add processing to this position if necessary. */
            /*======================================================*/

            return (SscApi.SSC_OK);
            //return ans;
        }



        public int sOpen()
        {
            int ans;
            ans = SscApi.sscOpen(board_id);

            if (ans != SscApi.SSC_OK)
            {
                LogManager.WriteServoLog(eLogLevel.Error, "sscOpen failure sscGetLastError=0x{0:X8}", SscApi.sscGetLastError());
            }
            return ans;
        }

        /*---------------------------------------------------------------------------*/
        /* [Function]                                                                */
        /*   stop sscnet system                                                      */
        /*                                                                           */
        /* [Argument]                                                                */
        /*    none                                                                   */
        /*                                                                           */
        /* [Return]                                                                  */
        /*    SSC_OK : function succeeded                                            */
        /*    SSC_NG : function failed                                               */
        /*---------------------------------------------------------------------------*/
        public int CloseSscnet()
        {
            int ans;
            short statuscode;

            /*======================================================*/
            /* Please add processing to this position if necessary. */
            /*======================================================*/

            /* disable device driver interrupt */
            ans = SscApi.sscIntDisable(board_id, channel);
            if (ans != SscApi.SSC_OK)
            {
                LogManager.WriteServoLog(eLogLevel.Error, "sscIntDisable failure sscGetLastError=0x{0:X8}", SscApi.sscGetLastError());
                return (SscApi.SSC_NG);
            }

            /* end device driver */
            ans = SscApi.sscIntEnd(board_id);
            if (ans != SscApi.SSC_OK)
            {
                LogManager.WriteServoLog(eLogLevel.Error, "sscIntEnd failure sscGetLastError=0x{0:X8}", SscApi.sscGetLastError());
                return (SscApi.SSC_NG);
            }

            /* check channel ready fin */
            ans = SscApi.sscGetSystemStatusCode(board_id, channel, out statuscode);
            if (ans != SscApi.SSC_OK)
            {
                LogManager.WriteServoLog(eLogLevel.Error, "sscGetSystemStatusCode failure sscGetLastError=0x{0:X8}", SscApi.sscGetLastError());
                return (SscApi.SSC_NG);
            }
            else if (statuscode != SscApi.SSC_STS_CODE_READY_FIN)
            {
#if null	
				/* please reboot if necessary. */
				/* reboot system */
				ans = SscApi.sscReboot( board_id, channel, SscApi.SSC_DEFAULT_TIMEOUT );
				if( ans != SscApi.SSC_OK )
				{
					Console.WriteLine( "sscReboot failure. sscGetLastError=0x{0:X8}", SscApi.sscGetLastError() );
					return( SscApi.SSC_NG );
				}
#endif
            }

            /* close device driver */
            ans = SscApi.sscClose(board_id);
            if (ans != SscApi.SSC_OK)
            {
                LogManager.WriteServoLog(eLogLevel.Error, "sscClose failure sscGetLastError=0x{0:X8}", SscApi.sscGetLastError());
                return (SscApi.SSC_NG);
            }

            return (SscApi.SSC_OK);
        }

        /*---------------------------------------------------------------------------*/
        /* [Function]                                                                */
        /*   start interrupt drive main                                              */
        /*                                                                           */
        /* [Argument]                                                                */
        /*    none                                                                   */
        /*                                                                           */
        /* [Return]                                                                  */
        /*    none                                                                   */
        /*---------------------------------------------------------------------------*/
        public void StartInterruptDriveMain()
        {
            int axis_cnt;

            Thread[] threadAx = new Thread[AXIS_MAX];

            /* create thread (drive home) */
            for (axis_cnt = 0; axis_cnt < AXIS_MAX; axis_cnt++)
            {
                if ((exist_axis & (1 << axis_cnt)) == SscApi.SSC_BIT_OFF)
                {
                    continue;
                }

                threadAx[axis_cnt] = new Thread(new ParameterizedThreadStart(InterruptDriveHome));
                threadAx[axis_cnt].IsBackground = true;
                threadAx[axis_cnt].Start(axis_cnt + 1);
            }

            /* wait thread (drive home) */
            for (axis_cnt = 0; axis_cnt < AXIS_MAX; axis_cnt++)
            {
                if ((exist_axis & (1 << axis_cnt)) == SscApi.SSC_BIT_OFF)
                {
                    continue;
                }

                threadAx[axis_cnt].Join();
            }

            /* create thread (drive auto) */
            for (axis_cnt = 0; axis_cnt < AXIS_MAX; axis_cnt++)
            {
                if ((exist_axis & (1 << axis_cnt)) == SscApi.SSC_BIT_OFF)
                {
                    continue;
                }

                threadAx[axis_cnt] = new Thread(new ParameterizedThreadStart(InterruptDriveAuto));
                threadAx[axis_cnt].IsBackground = true;
                threadAx[axis_cnt].Start(axis_cnt + 1);
            }

            /* wait thread (drive auto) */
            for (axis_cnt = 0; axis_cnt < AXIS_MAX; axis_cnt++)
            {
                if ((exist_axis & (1 << axis_cnt)) == SscApi.SSC_BIT_OFF)
                {
                    continue;
                }

                threadAx[axis_cnt].Join();
            }
            return;
        }

        public void sAxisHomeALLMove()
        {
            for (int i = 0; i <= exist_axis - 1; i++)
            {
                this.sAxisHomeMove(i + 1);
            }
            LogManager.WriteServoLog(eLogLevel.Info, "Servo All Home Command Sent");
        }

        public bool sAxisHomeMove(int iAxis)
        {
            int ans;

            /*---------------------------------------------------------------------*/
            /*  preparation for driving                                            */
            /*---------------------------------------------------------------------*/
            /* servo on */
            ans = sServoCmdBit(iAxis);
            if (ans != SscApi.SSC_OK)
            {
                LogManager.WriteServoLog(eLogLevel.Error, "sscSetCommandBitSignalEx failure. axnum={0}, sscGetLastError=0x{1:X8}", iAxis, SscApi.sscGetLastError());
                return false;
            }

            /* check servo on */
            short servoOn;
            ans = sCheckServoOn(iAxis, out servoOn);
            if (ans != SscApi.SSC_OK && servoOn != 1)
            {
                LogManager.WriteServoLog(eLogLevel.Error, "sCheckServoOn failure. axnum={0}, sscGetLastError=0x{1:X8}", iAxis, SscApi.sscGetLastError());
                return false;
            }

            /* reset int event */
            ans = sResetCommand(iAxis);
            if (ans != SscApi.SSC_OK)
            {
                LogManager.WriteServoLog(eLogLevel.Error, "sscResetIntDriveFin failure. axnum={0}, sscGetLastError=0x{1:X8}", iAxis, SscApi.sscGetLastError());
                return false;
            }

            /*---------------------------------------------------------------------*/
            /*  Start home drive                                                   */
            /*---------------------------------------------------------------------*/
            /* home return start */
            ans = sHomeCommand(iAxis);
            if (ans != SscApi.SSC_OK)
            {
                LogManager.WriteServoLog(eLogLevel.Error, "sHomeCommand failure sscGetLastError=0x{0:X8}", SscApi.sscGetLastError());
                return false;
            }
            return true;
        }


        /*---------------------------------------------------------------------------*/
        /* [Function]                                                                */
        /*   interrupt drive home                                                    */
        /*                                                                           */
        /* [Argument]                                                                */
        /*    object axis number                                                     */
        /*                                                                           */
        /* [Return]                                                                  */
        /*    none                                                                   */
        /*---------------------------------------------------------------------------*/
        public void InterruptDriveHome(object args)
        {
            int ans;
            int axnum;
            int fin_status;

            axnum = (int)args;



            /*---------------------------------------------------------------------*/
            /*  preparation for driving                                            */
            /*---------------------------------------------------------------------*/
            /* servo on */
            ans = sServoCmdBit(axnum);
            if (ans != SscApi.SSC_OK)
            {
                LogManager.WriteServoLog(eLogLevel.Error, "sscSetCommandBitSignalEx failure. axnum={0}, sscGetLastError=0x{1:X8}", axnum, SscApi.sscGetLastError());
                return;
            }

            /* check servo on */
            short servoOn;
            ans = sCheckServoOn(axnum, out servoOn);
            if (ans != SscApi.SSC_OK && servoOn != 1)
            {
                LogManager.WriteServoLog(eLogLevel.Error, "sCheckServoOn failure. axnum={0}, sscGetLastError=0x{1:X8}", axnum, SscApi.sscGetLastError());
                return;
            }

            /* reset int event */
            ans = sResetCommand(axnum);
            if (ans != SscApi.SSC_OK)
            {
                LogManager.WriteServoLog(eLogLevel.Error, "sscResetIntDriveFin failure. axnum={0}, sscGetLastError=0x{1:X8}", axnum, SscApi.sscGetLastError());
                return;
            }

            /*---------------------------------------------------------------------*/
            /*  Start home drive                                                   */
            /*---------------------------------------------------------------------*/
            /* home return start */
            ans = sHomeCommand(axnum);
            if (ans != SscApi.SSC_OK)
            {
                LogManager.WriteServoLog(eLogLevel.Error, "sHomeCommand failure. axnum={0}, sscGetLastError=0x{1:X8}", axnum, SscApi.sscGetLastError());
                return;
            }

            /* check drive finish */
            ans = sCheckDrivefinish(ref ans, axnum, out fin_status);
            if (ans != SscApi.SSC_OK)
            {
                LogManager.WriteServoLog(eLogLevel.Error, "sCheckDrivefinish failure. axnum={0}, sscGetLastError=0x{1:X8}", axnum, SscApi.sscGetLastError());
                Console.WriteLine("INFO", "sCheckDrivefinish failure. axnum={0}, sscGetLastError=0x{1:X8}", axnum, SscApi.sscGetLastError());
                return;
            }

            /* check drive finish status */
            CheckDriveFinStatus(axnum, fin_status, "home return finish.");

            return;
        }

        /// <summary>
        /// 축 정의된 모든 서보를 On 시킨다.
        /// </summary>
        /// <returns></returns>
        public bool sServoOnCommandALL()
        {
            bool bServoOn = true;
            int AxisServoOn = -1;
            for (int i = 1; i < exist_axis + 1; i++)
            {
                AxisServoOn = this.sServoOnCommand(i);
                if (AxisServoOn != 0) //서보온 실패.
                {
                    if (i != 9) // 9번축 서보온 Fail은 무시한다.(10번 동기화축으로 확인)
                    {
                        bServoOn = false;
                    }
                }
            }
            return bServoOn;

        }

        public int sServoOnCommand(int axnum)
        {
            int ans;


            /*---------------------------------------------------------------------*/
            /*  preparation for driving                                            */
            /*---------------------------------------------------------------------*/
            /* servo on */
            ans = sServoCmdBit(axnum);
            if (ans != SscApi.SSC_OK)
            {
                LogManager.WriteServoLog(eLogLevel.Error, "sServoCmdBit failure. axnum={0}, sscGetLastError=0x{1:X8}", axnum, SscApi.sscGetLastError());
                return -1;
            }

            /* check servo on */
            short servoOn;
            ans = sCheckServoOn(axnum, out servoOn);

            if (ans != SscApi.SSC_OK && servoOn != 1)
            {
                LogManager.WriteServoLog(eLogLevel.Info, String.Format("sCheckServoOn failure. axnum={0}, sscGetLastError=0x{1:X8}", axnum, SscApi.sscGetLastError()));
                LogManager.WriteServoLog(eLogLevel.Error, "sCheckServoOn failure. axnum={0}, sscGetLastError=0x{1:X8}", axnum, SscApi.sscGetLastError());

                short code1;
                SscApi.sscGetSystemStatusCode(0, 1, out code1);
                LogManager.WriteServoLog(eLogLevel.Fatal, String.Format("sscSystemStart failure sscGetLastError {0}", code1));

                return -1;
            }

            /* reset int event */
            ans = sResetCommand(axnum);
            if (ans != SscApi.SSC_OK)
            {
                LogManager.WriteServoLog(eLogLevel.Error, "sResetCommand failure. axnum={0}, sscGetLastError=0x{1:X8}", axnum, SscApi.sscGetLastError());
                return -1;
            }

            return 0;
        }

        public int sCheckDrivefinish(ref int ans, int axnum, out int fin_status)
        {
            //int fin_status;
            //ans = SscApi.sscWaitIntDriveFin(board_id, channel, axnum, SscApi.SSC_FIN_TYPE_INP, out fin_status, DRIVE_FIN_TIMEOUT);

            ans = SscApi.sscGetDriveFinStatus(board_id, channel, axnum, SscApi.SSC_FIN_TYPE_SMZ, out fin_status);

            return ans;

            //return value 값 정의
            //SscApi.SSC_FIN_STS_STP;
            //SscApi.SSC_FIN_STS_MOV;
            //SscApi.SSC_FIN_ALM_STP;
            //SscApi.SSC_FIN_STS_MOV;

        }
        #region 비사용
        /*
        public bool sCheckDrivefinish(int axnum )
        {
            int fin_status;
            int ans;
            //ans = SscApi.sscWaitIntDriveFin(board_id, channel, axnum, SscApi.SSC_FIN_TYPE_INP, out fin_status, DRIVE_FIN_TIMEOUT);

            ans = SscApi.sscGetDriveFinStatus(board_id, channel, axnum, SscApi.SSC_FIN_TYPE_INP, out fin_status);

            if (ans == SscApi.SSC_OK)
            {
                if ((fin_status == SscApi.SSC_FIN_STS_RDY || fin_status == SscApi.SSC_FIN_STS_STP)) 
                {
                    return true;
                }
                else
	            {
                     LogManager.WriteServoLog(eLogLevel.Info, "Servo 동작중 axnum={0},sscGetLastError=0x{1:X8}", axnum, SscApi.sscGetLastError());
                     return false;
	            }
            }
            else
	        {
                LogManager.WriteServoLog(eLogLevel.Error, "postionMoveComplet failure. axnum={0},sscGetLastError=0x{1:X8}", axnum, SscApi.sscGetLastError());
                return false;
	        }


        }
         */
        #endregion
        //181225 postion 완료 체크 추가
        public bool sCheckPostionfinish(int axnum, int Cmd_position)
        {

            LogManager.WriteServoLog(eLogLevel.Info, "postionMoveComplet {0}", Cmd_position);

            int Curposition;
            int tolerance = 100;
            int ans = SscApi.sscGetCurrentFbPositionFast(board_id, channel, axnum, out Curposition);

            int tmpP = Cmd_position + tolerance;
            int tmpN = Cmd_position - tolerance;

            if (ans == SscApi.SSC_OK)
            {
                if (tmpP >= Curposition && Cmd_position <= Curposition)
                    return true;
                else if (tmpN <= Curposition && Cmd_position >= Curposition)
                    return true;
                else if (Cmd_position == Curposition)
                    return true;
                else
                    return false;
            }
            else
            {
                LogManager.WriteServoLog(eLogLevel.Error, "postionMoveComplet failure. axnum={0},sscGetLastError=0x{1:X8}", axnum, SscApi.sscGetLastError());
                return false;
            }

        }

        private static int sHomeCommand(int axnum)
        {
            int ans;
            ans = SscApi.sscHomeReturnStart(board_id, channel, axnum);
            return ans;
        }


        private static int sResetCommand(int axnum)
        {
            int ans;
            ans = SscApi.sscResetIntDriveFin(board_id, channel, axnum);
            return ans;
        }

        public int sCheckServoOn(int axnum, out short servoOnState)
        {
            int answer = SscApi.sscCheckServoOnNoWait(board_id, channel, axnum, out servoOnState);

            return answer;
        }
        public void sCheckServoOnAll()
        {
            int ans = 0;

            for (int i = 0; i <= exist_axis - 1; i++)
            {
                ans = SscApi.sscCheckServoOnNoWait(board_id, channel, i + 1, out iservoOnState[i]);
                if (ans != SscApi.SSC_OK)
                {
                    LogManager.WriteServoLog(eLogLevel.Error, "sscCheckServoOnNoWait failure. axnum={0}, sscGetLastError=0x{1:X8}", i, SscApi.sscGetLastError());
                }
            }

        }

        private int sServoCmdBit(int axnum)
        {
            int ans;
            ans = SscApi.sscSetCommandBitSignalEx(board_id, channel, axnum, SscApi.SSC_CMDBIT_AX_SON, SscApi.SSC_BIT_ON);
            return ans;
        }


        public void sAllServoOFFCmd()
        {
            for (int i = 1; i <= exist_axis; i++)
            {
                this.sServoOFFCmd(i);
            }
        }

        public int sServoOFFCmd(int axnum)
        {
            int ans;
            ans = SscApi.sscSetCommandBitSignalEx(board_id, channel, axnum, SscApi.SSC_CMDBIT_AX_SON, SscApi.SSC_BIT_OFF);
            if (ans != SscApi.SSC_OK)
            {
                LogManager.WriteServoLog(eLogLevel.Error, "sscSetCommandBitSignalEx Servo Off failure. axnum={0}, sscGetLastError=0x{1:X8}", axnum, SscApi.sscGetLastError());
            }
            return ans;
        }

        public int sDriveStop()
        {

            int ans = 0;
            //short stopStatus = 0;
            for (int i = 1; i <= exist_axis; i++)
            {
                ans = SscApi.sscDriveStop(board_id, channel, i, SscApi.SSC_DEFAULT_TIMEOUT);
                //190104 RGJ
                //ans = SscApi.sscDriveStopNoWait(board_id, channel, i, out stopStatus); // sscDriveStop은 정지까지 블락 되므로 NoWait 로 변경.
                if (ans != SscApi.SSC_OK)
                {
                    LogManager.WriteServoLog(eLogLevel.Error, "sscDriveStop failure. axnum={0}, sscGetLastError=0x{1:X8}", i, SscApi.sscGetLastError());
                }
            }

            return ans;
        }

        //개별 축 동작 정지 추가.
        public int sServoStop(int nAxis)
        {

            int ans = 0;

            ans = SscApi.sscDriveStop(board_id, channel, nAxis, SscApi.SSC_DEFAULT_TIMEOUT);
            if (ans != SscApi.SSC_OK)
            {
                LogManager.WriteServoLog(eLogLevel.Error, "sscDriveStop failure. axnum={0}, sscGetLastError=0x{1:X8}", nAxis, SscApi.sscGetLastError());
                //return ans;
            }
            return ans;
        }
        public bool sAxisPointMove(int nAxis, int MovePoint, int moveSpeed, ushort acctime = 0, ushort dcctime = 0)
        {
            int ans;

            PNT_DATA_EX[] PntData = new PNT_DATA_EX[1]; // 1축만 Move해서 1배열로 셋팅

            //this.sDriveStop();

            /*---------------------------------------------------------------------*/
            /*  preparation for driving                                            */
            /*---------------------------------------------------------------------*/

            int s = 0;
            int Postionfind = MovePoint;


            PntData.Initialize();

            PntData[s].position = Postionfind;					/* 10mm							*/
            //PntData[0].speed = (ushort)(MoveSpeed[s] * 10);		/* 200mm/s						*/
            PntData[s].speed = (ushort)(moveSpeed);		/* 200mm/s			200제거			*/

            if (acctime == 0)
                PntData[s].actime = (ushort)MoveAcc[nAxis - 1];				/* 100ms						*/
            else
                PntData[s].actime = acctime;
            if (dcctime == 0)
                PntData[s].dctime = (ushort)MoveDcc[nAxis - 1];				/* 100ms						*/
            else
                PntData[s].dctime = dcctime;

            PntData[s].dwell = 0;								    /* 0ms							*/
            PntData[s].subcmd = SscApi.SSC_SUBCMD_POS_ABS		    /* Absolute Position			*/
                                        | SscApi.SSC_SUBCMD_STOP_SMZ;	/* Smoothing Stop				*/
            PntData[s].s_curve = 100;								/* 100% 						*/


            /* point data1 write */
            ans = SscApi.sscSetPointDataEx(board_id, channel, nAxis, 0, ref PntData[s]);
            if (ans != SscApi.SSC_OK)
            {
                LogManager.WriteServoLog(eLogLevel.Info, String.Format("sscSetPointDataEx failure. axnum={0}, sscGetLastError=0x{1:X8}", s + 1, SscApi.sscGetLastError()));
                return false;
            }
            /*---------------------------------------------------------------------*/
            /*  Start auto drive                                                   */
            /*---------------------------------------------------------------------*/
            /* reset int event */
            ans = SscApi.sscResetIntDriveFin(board_id, channel, nAxis);
            if (ans != SscApi.SSC_OK)
            {
                LogManager.WriteServoLog(eLogLevel.Info, String.Format("sscResetIntDriveFin failure. axnum={0}, sscGetLastError=0x{1:X8}", s + 1, SscApi.sscGetLastError()));
                return false;
            }

            /* auto start */
            ans = SscApi.sscAutoStart(board_id, channel, nAxis, 0, 0);
            if (ans != SscApi.SSC_OK)
            {
                int ssc = SscApi.sscGetLastError();

                LogManager.WriteServoLog(eLogLevel.Error, "sscAutoStart failure. axnum={0}, sscGetLastError=0x{1:X8}", s + 1, SscApi.sscGetLastError());
                return false;
            }

            return true;
        }

        public void sAbsoluteMoveCmdAction(int noffset, int axnum, int Dir, int speed)
        {
            int ans;


            int curPostion = 0;
            int pointPostion;
            PNT_DATA_EX PntData = new PNT_DATA_EX();

            this.sDriveStop();

            if (noffset == 0 || axnum == 0)
                return;

            this.sCurrenPostionOn(axnum, out curPostion);

            pointPostion = Dir == SscApi.SSC_DIR_MINUS ? (curPostion - noffset) : (curPostion + noffset);


            /*---------------------------------------------------------------------*/
            /*  Absolute Move for driving                                          */
            /*---------------------------------------------------------------------*/

            PntData.Initialize();

            PntData.position = pointPostion;				    /* 10mm							*/
            PntData.speed = (ushort)speed;          			/* 200mm/s						*/
            PntData.actime = (ushort)MoveAcc[axnum - 1];				/* 100ms						*/
            PntData.dctime = (ushort)MoveDcc[axnum - 1];				/* 100ms						*/
            PntData.dwell = 0;								    /* 0ms							*/
            PntData.subcmd = SscApi.SSC_SUBCMD_POS_ABS		    /* Absolute Position			*/
                                        | SscApi.SSC_SUBCMD_STOP_SMZ;	/* Smoothing Stop				*/
            PntData.s_curve = 100;								/* 100% 						*/


            /* point data1 write */
            ans = SscApi.sscSetPointDataEx(board_id, channel, axnum, 0, ref PntData);
            if (ans != SscApi.SSC_OK)
            {
                LogManager.WriteServoLog(eLogLevel.Info, String.Format("sscSetPointDataEx failure. axnum={0}, sscGetLastError=0x{1:X8}", axnum, SscApi.sscGetLastError()));
                return;
            }
            /*---------------------------------------------------------------------*/
            /*  Start auto drive                                                   */
            /*---------------------------------------------------------------------*/
            /* reset int event */
            ans = SscApi.sscResetIntDriveFin(board_id, channel, axnum);
            if (ans != SscApi.SSC_OK)
            {
                LogManager.WriteServoLog(eLogLevel.Info, String.Format("sscResetIntDriveFin failure. axnum={0}, sscGetLastError=0x{1:X8}", axnum, SscApi.sscGetLastError()));
                return;

            }

            /* auto start */
            ans = SscApi.sscAutoStart(board_id, channel, axnum, 0, 0);
            if (ans != SscApi.SSC_OK)
            {
                LogManager.WriteServoLog(eLogLevel.Info, String.Format("sscAutoStart failure. axnum={0}, sscGetLastError=0x{1:X8}", axnum, SscApi.sscGetLastError()));
                return;
            }

            return;
        }


        #region 비사용
        /*
        public int sMoveActonDone()
        {
            int ans = 0;
          
            int[] fin_status = new int[32];

           
            if (servoMoterSkip)
                return SscApi.SSC_FIN_STS_STP;


            for (int i = 0; i <= exist_axis - 1; i++)
            {
                // check drive finish 
                ans = sCheckDrivefinish(ref ans, i + 1, out fin_status[i]);
                if (ans != SscApi.SSC_OK)
                {
                    LogManager.WriteServoLog(eLogLevel.Error, "sscWaitIntDriveFin failure. axnum={0}, sscGetLastError=0x{1:X8}", i + 1, SscApi.sscGetLastError());
                    return fin_status[i];
                }

                // check drive finish status 
                CheckDriveFinStatus(i + 1, fin_status[i], "check drive finish.");


                if (fin_status[i] == SscApi.SSC_FIN_STS_ALM_STP)
                {
                    return SscApi.SSC_FIN_STS_ALM_STP;
                }
                else if (fin_status[i] == SscApi.SSC_FIN_STS_ALM_MOV)
                {
                    return SscApi.SSC_FIN_STS_ALM_MOV;
                }
                else if (fin_status[i] == SscApi.SSC_FIN_STS_MOV)
                {
                    return SscApi.SSC_FIN_STS_MOV;
                }
            }
            return SscApi.SSC_FIN_STS_STP;

        } 
        */
        #endregion
        public void InterruptDriveAuto(object args)
        {
            int ans;
            int axnum;
            int fin_status;
            PNT_DATA_EX[] PntData = new PNT_DATA_EX[2];

            axnum = (int)args;

            /*---------------------------------------------------------------------*/
            /*  preparation for driving                                            */
            /*---------------------------------------------------------------------*/
            /* point data1 set */
            PntData[0].Initialize();

            PntData[0].position = 10000;							/* 10mm							*/
            PntData[0].speed = 20;								/* 200mm/s						*/
            PntData[0].actime = 100;								/* 100ms						*/
            PntData[0].dctime = 100;								/* 100ms						*/
            PntData[0].dwell = 0;								/* 0ms							*/
            PntData[0].subcmd = SscApi.SSC_SUBCMD_POS_ABS		/* Absolute Position			*/
                                        | SscApi.SSC_SUBCMD_STOP_SMZ;	/* Smoothing Stop				*/
            PntData[0].s_curve = 100;								/* 100% 						*/

            /* point data2 set */
            PntData[1].Initialize();

            PntData[1].position = 0;								/* -10mm 						*/
            PntData[1].speed = 20;								/* 200mm/s						*/
            PntData[1].actime = 100;								/* 100ms						*/
            PntData[1].dctime = 100;								/* 100ms						*/
            PntData[1].dwell = 0;								/* 0ms							*/
            PntData[1].subcmd = SscApi.SSC_SUBCMD_POS_ABS		/* Absolute Position			*/
                                        | SscApi.SSC_SUBCMD_STOP_SMZ;	/* Smoothing Stop				*/
            PntData[1].s_curve = 100;								/* 100% 						*/

            /* point data1 write */
            ans = SscApi.sscSetPointDataEx(board_id, channel, axnum, 0, ref PntData[0]);
            if (ans != SscApi.SSC_OK)
            {
                LogManager.WriteServoLog(eLogLevel.Error, "sscSetPointDataEx failure. axnum={0}, sscGetLastError=0x{1:X8}", axnum, SscApi.sscGetLastError());
                return;
            }

            /* point data2 write */
            ans = SscApi.sscSetPointDataEx(board_id, channel, axnum, 1, ref PntData[1]);
            if (ans != SscApi.SSC_OK)
            {
                LogManager.WriteServoLog(eLogLevel.Error, "sscSetPointDataEx failure. axnum={0}, sscGetLastError=0x{1:X8}", axnum, SscApi.sscGetLastError());
                return;
            }

            /*---------------------------------------------------------------------*/
            /*  Start auto drive                                                   */
            /*---------------------------------------------------------------------*/
            /* reset int event */
            ans = SscApi.sscResetIntDriveFin(board_id, channel, axnum);
            if (ans != SscApi.SSC_OK)
            {
                LogManager.WriteServoLog(eLogLevel.Error, "sscResetIntDriveFin failure. axnum={0}, sscGetLastError=0x{1:X8}", axnum, SscApi.sscGetLastError());
                return;
            }

            /* auto start */
            ans = SscApi.sscAutoStart(board_id, channel, axnum, 0, 1);
            if (ans != SscApi.SSC_OK)
            {
                LogManager.WriteServoLog(eLogLevel.Error, "sscAutoStart failure. axnum={0}, sscGetLastError=0x{1:X8}", axnum, SscApi.sscGetLastError());
                return;
            }

            /* check drive finish */
            //ans = sCheckDrivefinish(ref ans, axnum, out fin_status);
            ans = SscApi.sscWaitIntDriveFin(board_id, channel, axnum, SscApi.SSC_FIN_TYPE_INP, out fin_status, DRIVE_FIN_TIMEOUT);
            if (ans != SscApi.SSC_OK)
            {
                LogManager.WriteServoLog(eLogLevel.Error, "sscGetAlarm(operation alarm) failure sscGetLastError=0x{0:X8}", SscApi.sscGetLastError());
                return;
            }

            /* check drive finish status */
            CheckDriveFinStatus(axnum, fin_status, "drive finish.");

            return;
        }


        /*---------------------------------------------------------------------------*/
        /* [Function]                                                                */
        /*   check drive finish status                                               */
        /*                                                                           */
        /* [Argument]                                                                */
        /*    axnum        axis number                                               */
        /*    fin_status   drive finish status                                       */
        /*    drv_fin_msg  drive finish message                                      */
        /*                                                                           */
        /* [Return]                                                                  */
        /*    SSC_OK : function succeeded                                            */
        /*    SSC_NG : function failed                                               */
        /*---------------------------------------------------------------------------*/
        public int CheckDriveFinStatus(int axnum, int fin_status, String drv_fin_msg)
        {
            int ans;
            int position;
            ushort code;
            ushort detail_code;

            if (fin_status == SscApi.SSC_FIN_STS_STP)
            {
                ans = SscApi.sscGetCurrentCmdPositionFast(board_id, channel, axnum, out position);
                if (ans != SscApi.SSC_OK)
                {
                    LogManager.WriteServoLog(eLogLevel.Error, "sscGetCurrentCmdPositionFast failure. axnum={0},sscGetLastError=0x{1:X8}", axnum, SscApi.sscGetLastError());

                    return (SscApi.SSC_NG);
                }
                LogManager.WriteServoLog(eLogLevel.Info, String.Format("{0} axnum={1}, command pos.={2}", drv_fin_msg, axnum, position));
            }
            else if (fin_status == SscApi.SSC_FIN_STS_MOV)
            {
                //logAxis.D("now driving. axnum={0}", axnum);  
                return (SscApi.SSC_NG);
            }
            else if (fin_status == SscApi.SSC_FIN_STS_ALM_STP)
            {
                ans = SscApi.sscGetAlarm(board_id, channel, axnum, SscApi.SSC_ALARM_OPERATION, out code, out detail_code);
                if (ans != SscApi.SSC_OK)
                {
                    LogManager.WriteServoLog(eLogLevel.Error, "sscGetAlarm failure(operation alarm). axnum={0},sscGetLastError=0x{1:X8}", axnum, SscApi.sscGetLastError());

                    return (SscApi.SSC_NG);
                }

                LogManager.WriteServoLog(eLogLevel.Error, "drive alarm occurrence(stop completion). axnum={0}, operation alarm=0x{1:X2}(0x{2:X2})", axnum, code, detail_code);

                return (SscApi.SSC_NG);
            }
            else if (fin_status == SscApi.SSC_FIN_STS_ALM_MOV)
            {
                ans = SscApi.sscGetAlarm(board_id, channel, axnum, SscApi.SSC_ALARM_OPERATION, out code, out detail_code);
                if (ans != SscApi.SSC_OK)
                {
                    LogManager.WriteServoLog(eLogLevel.Error, "sscGetAlarm failure(operation alarm). axnum={0},sscGetLastError=0x{1:X8}", axnum, SscApi.sscGetLastError());
                    return (SscApi.SSC_NG);
                }
                LogManager.WriteServoLog(eLogLevel.Error, "drive alarm occurrence(stopping deceleration). axnum={0}, operation alarm=0x{1:X2}(0x{2:X2})", axnum, code, detail_code);
                return (SscApi.SSC_NG);
            }
            else
            {
                LogManager.WriteServoLog(eLogLevel.Error, "not support. axnum={0}", axnum);
                return (SscApi.SSC_NG);
            }

            return (SscApi.SSC_OK);
        }


        public void Servo_JogMoving(int i_Axis, int jogspeed, int li_Direct)
        {

            try
            {
                int ans;

                PNT_DATA_EX[] PntData = new PNT_DATA_EX[2];

                /*---------------------------------------------------------------------*/
                /*  Start Jog drive                                                   */
                /*---------------------------------------------------------------------*/
                if (li_Direct == SscApi.SSC_DIR_PLUS)
                {
                    ans = SscApi.sscJogStart(board_id, channel, i_Axis, jogspeed, jogAccSpeed[i_Axis], jogDccSpeed[i_Axis], SscApi.SSC_DIR_PLUS);

                    if (ans != SscApi.SSC_OK)
                    {
                        LogManager.WriteServoLog(eLogLevel.Error, "sscWaitIntDriveFin failure. axnum={0}, sscGetLastError=0x{1:X8}", i_Axis, SscApi.sscGetLastError());
                        return;
                    }
                }
                else
                {
                    ans = SscApi.sscJogStart(board_id, channel, i_Axis, jogspeed, jogAccSpeed[i_Axis], jogDccSpeed[i_Axis], SscApi.SSC_DIR_MINUS);
                    if (ans != SscApi.SSC_OK)
                    {
                        LogManager.WriteServoLog(eLogLevel.Error, "sscWaitIntDriveFin failure. axnum={0}, sscGetLastError=0x{1:X8}", i_Axis, SscApi.sscGetLastError());
                        return;
                    }
                }

            }
            catch (Exception ex)
            {
                LogManager.WriteServoLog(eLogLevel.Error, ex.ToString());
            }


            return;
        }



        public void Servo_JogStop()
        {
            try
            {

                for (int i = 1; i <= exist_axis; i++)
                {
                    if (SscApi.sscJogStop(board_id, channel, i) != 0)
                    {
                        LogManager.WriteServoLog(eLogLevel.Error, "sscJogStop failure. axnum={0}, sscGetLastError=0x{1:X8}", i, SscApi.sscGetLastError());
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteServoLog(eLogLevel.Error, ex.ToString());
            }

        }

        public void sServo_AlarmReset()
        {
            int ans;

            /*---------------------------------------------------------------------*/
            /*  Start Jog drive                                                   */
            /*---------------------------------------------------------------------*/

            for (int i = 0; i <= exist_axis - 1; i++)
            {
                ans = SscApi.sscGetControlAlarmCode(board_id, channel, out wC_ChAlarm);
                if (ans != SscApi.SSC_OK)
                {
                    LogManager.WriteServoLog(eLogLevel.Error, "sscGetControlAlarmCode failure. axnum={0}, sscGetLastError=0x{1:X8}", i + 1, SscApi.sscGetLastError());
                }

                if (wC_ChAlarm != 0x0000)
                {
                    ans = SscApi.sscControlAlarmReset(board_id, channel);
                    if (ans != SscApi.SSC_OK)
                    {
                        LogManager.WriteServoLog(eLogLevel.Error, "sscControlAlarmReset failure. axnum={0}, sscGetLastError=0x{1:X8}", i + 1, SscApi.sscGetLastError());
                    }
                    LogManager.WriteServoLog(eLogLevel.Error, "sscControlAlarmReset Reset clear . axnum={0}, sscGetLastError=0x{1:X8}", i + 1, SscApi.sscGetLastError());
                }

                ans = SscApi.sscGetOperationAlarmCode(board_id, channel, i + 1, out wC_OPAlarm[i]);
                if (ans != SscApi.SSC_OK)
                {
                    LogManager.WriteServoLog(eLogLevel.Error, "sscGetOperationAlarmCode failure. axnum={0}, sscGetLastError=0x{1:X8}", i + 1, SscApi.sscGetLastError());
                }

                if (wC_OPAlarm[i] != 0x0000)
                {
                    ans = SscApi.sscOperationAlarmReset(board_id, channel, i + 1);
                    if (ans != SscApi.SSC_OK)
                    {
                        LogManager.WriteServoLog(eLogLevel.Error, "sscOperationAlarmReset failure. axnum={0}, sscGetLastError=0x{1:X8}", i + 1, SscApi.sscGetLastError());
                    }
                    LogManager.WriteServoLog(eLogLevel.Error, "sscOperationAlarmReset Reset clear . axnum={0}, sscGetLastError=0x{1:X8}", i + 1, SscApi.sscGetLastError());
                }

                ans = SscApi.sscGetServoAlarmCode(board_id, channel, i + 1, out wC_SVAlarm[i]);
                if (ans != SscApi.SSC_OK)
                {
                    LogManager.WriteServoLog(eLogLevel.Error, "sscGetServoAlarmCode failure. axnum={0}, sscGetLastError=0x{1:X8}", i + 1, SscApi.sscGetLastError());
                }

                if (wC_SVAlarm[i] != 0x0000)
                {
                    ans = SscApi.sscServoAlarmReset(board_id, channel, i + 1);
                    if (ans != SscApi.SSC_OK)
                    {
                        LogManager.WriteServoLog(eLogLevel.Error, "sscOperationAlarmReset failure. axnum={0}, sscGetLastError=0x{1:X8}", i + 1, SscApi.sscGetLastError());
                    }

                    LogManager.WriteServoLog(eLogLevel.Error, "sscServoAlarmReset Reset clear . axnum={0}, sscGetLastError=0x{1:X8}", i + 1, SscApi.sscGetLastError());
                }
            }


        }


        public bool sServo_AlarmCheck()
        {
            int ans;

            /*---------------------------------------------------------------------*/
            /*  Alarm Check                                                        */
            /*---------------------------------------------------------------------*/


            for (int i = 0; i <= exist_axis - 1; i++)
            {
                ans = SscApi.sscGetOperationAlarmCode(board_id, channel, i + 1, out wC_OPAlarm[i]);

                SerovAlarmCode = wC_OPAlarm[0];


                if (ans != SscApi.SSC_OK)
                {
                    LogManager.WriteServoLog(eLogLevel.Error, "sscGetOperationAlarmCode failure. axnum={0}, sscGetLastError=0x{1:X8}", i + 1, SscApi.sscGetLastError());
                    return true;
                }

                if (SerovAlarmCode != 0)
                    return true;

                ans = SscApi.sscGetServoAlarmCode(board_id, channel, i + 1, out wC_SVAlarm[i]);


                SerovAlarmCode = wC_SVAlarm[0];


                if (ans != SscApi.SSC_OK)
                {
                    LogManager.WriteServoLog(eLogLevel.Error, "sscGetServoAlarmCode failure. axnum={0}, sscGetLastError=0x{1:X8}", i + 1, SscApi.sscGetLastError());
                    return true;
                }
                if (SerovAlarmCode != 0)
                    return true;

            }

            ans = SscApi.sscGetControlAlarmCode(board_id, channel, out wC_ChAlarm);
            SerovAlarmCode = wC_ChAlarm;


            if (ans != SscApi.SSC_OK)
            {
                LogManager.WriteServoLog(eLogLevel.Error, "sscGetControlAlarmCode failure. , sscGetLastError=0x{0:X8}", SscApi.sscGetLastError());
                return true;
            }

            if (SerovAlarmCode != 0)
                return true;

            return false;
        }


        public int DogSensorMotion_Check(int axis)
        {

            int ans = 0;

            int sensor = 0;

            ans = SscApi.sscGetIoStatusFast(board_id, channel, axis, out lw_DogSensorStatus[0]);

            sensor = lw_DogSensorStatus[0];

            return sensor;


        }

        public void MotionMonitoring(int li_Axis)
        {
            int ans;
            short[] lw_MonNum = new short[4];
            short[] lw_MonData = new short[4];


            ans = SscApi.sscGetMonitor(board_id, channel, li_Axis, lw_MonNum, lw_MonData);

            if (ans != SscApi.SSC_OK)
            {
                LogManager.WriteServoLog(eLogLevel.Error, "sscGetMonitor failure. axnum={0},sscGetLastError=0x{1:X8}", li_Axis, SscApi.sscGetLastError());
            }


            //dC_PosFeedback[li_Axis] = MAKELONG(lw_MonData[0], lw_MonData[1]) ;
            //dC_PosDrop     = MAKELONG(lw_MonData[2], lw_MonData[3]) ;  //한축당 4개까지만 처리될 수 있다.
            //dC_SpdFeedback[li_Axis] = MAKELONG(lw_MonData[0], lw_MonData[1]) ;
            dC_RealTorque[li_Axis] = lw_MonData[2];
            dC_PeekTorque[li_Axis] = lw_MonData[3];
            //dC_AlarmNum    = lw_MonData[8] ;
            //dC_AlarmBit	 = lw_MonData[9] ;
        }
        public bool[] sGetAxisStatus(int Axis)
        {
            bool[] Status = new bool[8];
            short[] AxisStat = new short[8];
            int ans;
            ans = SscApi.sscGetAxisStatusBits(board_id, channel, Axis, AxisStat);

            if (ans != SscApi.SSC_OK)
            {
                LogManager.WriteServoLog(eLogLevel.Error, "sscGetMonitor failure. axnum={0},sscGetLastError=0x{1:X8}", Axis, SscApi.sscGetLastError());
            }
            Status[0] = ((AxisStat[0] >> 0) & 0x01) == 1;	//RDY Servo Ready
            Status[1] = ((AxisStat[0] >> 1) & 0x01) == 1;	//INP 인포지션
            Status[2] = ((AxisStat[0] >> 4) & 0x01) == 1;	//TLC 토크제한중
            Status[3] = ((AxisStat[0] >> 5) & 0x01) == 1;  //SALM 서보알람중
            Status[4] = ((AxisStat[0] >> 6) & 0x01) == 1;  //SWRN 서보경고중
            Status[5] = ((AxisStat[0] >> 8) & 0x01) == 1;	//OP 운전중
            Status[6] = ((AxisStat[0] >> 11) & 0x01) == 1;  //ZP 원점복귀 완료
            Status[7] = ((AxisStat[0] >> 12) & 0x01) == 1;  //OALM 운전 알람중
            return Status;
        }


        /// <summary>
        /// DefaultParameter 값을 조회하는 메서드
        /// GetDefaultParameterValue -> LoadParameterValue 이름 변경.
        /// </summary>
        protected void LoadParameterValue()
        {

            if (string.IsNullOrEmpty(ServoPAini))
                return;

            int Paranum = 0;

            #region System
            for (int i = 1; i <= 10; i++)
            {

                string spara = INI_Helper.ReadValue(ServoPAini, "System", i.ToString());

                if (!string.IsNullOrEmpty(spara))
                {
                    string[] values = spara.Split(' '); // 읽은 줄에서 공백을 구분자로 잘라냅니다.'
                    values[0].Trim();
                    values[1].Trim();

                    string sParaName = values[0].Substring(2, 4);
                    string sParaValse = values[1].Substring(2, 4);

                    int tmpsParaName = int.Parse(sParaName, System.Globalization.NumberStyles.AllowHexSpecifier);
                    prm_tbl[Paranum].prm_num = (short)tmpsParaName;

                    int tmpsParaValse = int.Parse(sParaValse, System.Globalization.NumberStyles.AllowHexSpecifier);
                    prm_tbl[Paranum].prm_data = (short)tmpsParaValse;

                    Paranum++;
                }
                else
                {
                    continue;
                }
            }
            #endregion

            #region Default Axis
            for (int s = 1; s <= exist_axis; s++)
            {
                for (int i = 1; i <= maxDefultParaCount; i++)
                {

                    string spara = INI_Helper.ReadValue(ServoPAini, "Default", i.ToString());

                    if (!string.IsNullOrEmpty(spara))
                    {
                        string[] values = spara.Split(' '); // 읽은 줄에서 공백을 구분자로 잘라냅니다.'
                        values[0].Trim();
                        values[1].Trim();

                        string sParaName = values[0].Substring(2, 4);
                        string sParaValse = values[1].Substring(2, 4);

                        prm_tbl[Paranum].axis_num = s;
                        //prm_tbl[Paranum].prm_num = (short)Convert.ToInt16(sParaName, 16);
                        //prm_tbl[Paranum].prm_data = (short)Convert.ToInt16(sParaValse, 16);

                        int tmpsParaName = int.Parse(sParaName, System.Globalization.NumberStyles.AllowHexSpecifier);
                        prm_tbl[Paranum].prm_num = (short)tmpsParaName;

                        int tmpsParaValse = int.Parse(sParaValse, System.Globalization.NumberStyles.AllowHexSpecifier);
                        prm_tbl[Paranum].prm_data = (short)tmpsParaValse;

                        Paranum++;
                    }
                    else
                    {
                        continue;
                    }
                }
            }
            #endregion

            for (int s = 1; s <= exist_axis; s++)
            {
                for (int i = 1; i <= 100; i++)
                {
                    string spara = INI_Helper.ReadValue(ServoPAini, "Axis" + s.ToString(), i.ToString());

                    if (!string.IsNullOrEmpty(spara))
                    {
                        string[] values = spara.Split(' '); // 읽은 줄에서 공백을 구분자로 잘라냅니다.'
                        values[0].Trim();
                        values[1].Trim();

                        string sParaName = values[0].Substring(2, 4);
                        string sParaValse = values[1].Substring(2, 4);

                        prm_tbl[Paranum].axis_num = s;
                        //prm_tbl[Paranum].prm_num = (short)Convert.ToInt16(sParaName, 16);
                        //prm_tbl[Paranum].prm_data = (short)Convert.ToInt16(sParaValse, 16);


                        int tmpsParaName = int.Parse(sParaName, System.Globalization.NumberStyles.AllowHexSpecifier);
                        prm_tbl[Paranum].prm_num = (short)tmpsParaName;

                        int tmpsParaValse = int.Parse(sParaValse, System.Globalization.NumberStyles.AllowHexSpecifier);
                        prm_tbl[Paranum].prm_data = (short)tmpsParaValse;

                        Paranum++;

                    }
                    else
                    {
                        continue;
                    }
                }
                // 절대위치 서보파라미터 0x024D , 0x024E , 0x024F 저장 및 적용 추가.
                //절대값 파라미터는 마지막에 넣어준다.
                prm_tbl[Paranum].axis_num = (short)s;
                prm_tbl[Paranum].prm_num = 0x024D;
                prm_tbl[Paranum].prm_data = ABS24D[s - 1];
                Paranum++;

                prm_tbl[Paranum].axis_num = (short)s;
                prm_tbl[Paranum].prm_num = 0x024E;
                prm_tbl[Paranum].prm_data = ABS24E[s - 1];
                Paranum++;

                prm_tbl[Paranum].axis_num = (short)s;
                prm_tbl[Paranum].prm_num = 0x024F;
                prm_tbl[Paranum].prm_data = ABS24F[s - 1];
                Paranum++;

            }




        }


        /// <summary>
        /// Servo Para config  값을 조회하는 메서드
        /// </summary>
        protected void GetServoParaconfig()
        {
            if (string.IsNullOrEmpty(ServoConfigfile))
                return;

            board_id = Convert.ToInt16(INI_Helper.ReadValue(ServoConfigfile, "Config", "BoardID"));
            channel = Convert.ToInt16(INI_Helper.ReadValue(ServoConfigfile, "Config", "channel"));
            exist_axis = Convert.ToInt16(INI_Helper.ReadValue(ServoConfigfile, "Config", "MaxAxis"));

            for (int i = 0; i <= exist_axis - 1; i++)
            {
                jogAccSpeed[i] = Convert.ToInt16(INI_Helper.ReadValue(ServoConfigfile, "JogAcc", (i + 1).ToString()));
                jogDccSpeed[i] = Convert.ToInt16(INI_Helper.ReadValue(ServoConfigfile, "JogDec", (i + 1).ToString()));

                MoveAcc[i] = Convert.ToInt32(INI_Helper.ReadValue(ServoConfigfile, "MoveAcc", (i + 1).ToString()));
                MoveDcc[i] = Convert.ToInt32(INI_Helper.ReadValue(ServoConfigfile, "MoveDec", (i + 1).ToString()));

                ABS24D[i] = Convert.ToInt16(INI_Helper.ReadValue(ServoConfigfile, "ABS24D", (i + 1).ToString()));
                ABS24E[i] = Convert.ToInt16(INI_Helper.ReadValue(ServoConfigfile, "ABS24E", (i + 1).ToString()));
                ABS24F[i] = Convert.ToInt16(INI_Helper.ReadValue(ServoConfigfile, "ABS24F", (i + 1).ToString()));
            }

        }

        public void WriteServoHomeParameter_Ini(int axis, short ABS24D, short ABS24E, short ABS24F)
        {
            INI_Helper.WriteValue(ServoConfigfile, "ABS24D", axis.ToString(), ABS24D.ToString());
            INI_Helper.WriteValue(ServoConfigfile, "ABS24E", axis.ToString(), ABS24E.ToString());
            INI_Helper.WriteValue(ServoConfigfile, "ABS24F", axis.ToString(), ABS24F.ToString());
        }


        public int sCmdPostion(int axnum, out int position)
        {
            int ans;
            ans = SscApi.sscGetCurrentCmdPositionFast(board_id, channel, axnum, out position);
            if (ans != SscApi.SSC_OK)
            {
                LogManager.WriteServoLog(eLogLevel.Error, "sscGetCurrentCmdPositionFast failure. axnum={0},sscGetLastError=0x{1:X8}", axnum, SscApi.sscGetLastError());
                return (SscApi.SSC_NG);
            }
            else if (ans != SscApi.SSC_OK)
            {
                return SscApi.SSC_NG;
            }
            return SscApi.SSC_NG;
        }

        public int sCurrenPostionOn(int axnum, out int position)
        {
            int ans;
            ans = SscApi.sscGetCurrentFbPositionFast(board_id, channel, axnum, out position);
            if (ans == SscApi.SSC_OK)
            {
                return SscApi.SSC_OK;
            }

            else if (ans != SscApi.SSC_OK)
            {
                LogManager.WriteServoLog(eLogLevel.Info, String.Format("sscGetCurrentFbPositionFast failure. axnum={0},sscGetLastError=0x{1:X8}", axnum, SscApi.sscGetLastError()));
                return SscApi.SSC_NG;
            }

            return SscApi.SSC_NG;
        }


        public void AccTimeChange(int axnum, short Acctime)
        {
            int ans;
            int pntnum = 0;

            ans = SscApi.sscChangeAutoAccTime(board_id, channel, axnum, pntnum, Acctime);
            if (ans == SscApi.SSC_OK)
            {
                LogManager.WriteServoLog(eLogLevel.Error, "sscChangeAutoAccTime failure. axnum={0},sscGetLastError=0x{1:X8}", axnum, SscApi.sscGetLastError());
            }
        }
        public void DccTimeChange(int axnum, short Dcctime)
        {
            int ans;
            int pntnum = 0;

            ans = SscApi.sscChangeAutoAccTime(board_id, channel, axnum, pntnum, Dcctime);
            if (ans == SscApi.SSC_OK)
            {
                LogManager.WriteServoLog(eLogLevel.Error, "sscChangeAutoAccTime failure. axnum={0},sscGetLastError=0x{1:X8}", axnum, SscApi.sscGetLastError());
            }
        }

        public void AutoSpeedChange(int axnum, int speed)
        {
            int ans;
            int pntnum = 0;

            ans = SscApi.sscChangeAutoSpeed(board_id, channel, axnum, pntnum, speed);
            if (ans == SscApi.SSC_OK)
            {
                LogManager.WriteServoLog(eLogLevel.Error, "sscGetAlarm(operation alarm) failure sscGetLastError=0x{0:X8}", SscApi.sscGetLastError());
            }
        }

    }
}
