using BoxPrint.CCLink;
using BoxPrint.Log;
using System;
using System.Linq;
using System.Threading;

namespace BoxPrint.SSCNet
{

    //5. 운전시에는 EM2(강제 정지 2), LSP(정회전 스트로크 엔드) 및 LSN(역회전 스트로크 엔드)을 반드시 ON으로 해 주십시오.(B접점)
    //6. ALM(고장)은 알람이 발생하고 있지 않는 정상시에는 ON이 됩니다.(B접점)
    public class ServoAxis_IOControl
    {
        protected bool[] Simul_In_IOArray = new bool[32];
        protected bool[] Simul_Out_IOArray = new bool[32];
        public bool SimulMode { get; set; }

        protected readonly bool IsStackerServo;
        protected readonly int ThreadCycle = 50; //서보 Cycle 딜레이 변경 200ms->50ms
        protected readonly int PositionMoveTimeout = 30;
        protected string ParentModuleName;

        public ServoAxis_IOControl(string ParantName, bool Simulmode)
        {
            ParentModuleName = ParantName;
            SimulMode = Simulmode;
            Thread LimitHomeCheckThread = new Thread(new ThreadStart(MonitorRun));
            LimitHomeCheckThread.IsBackground = true;
            LimitHomeCheckThread.Start();
        }
        public ServoAxis_IOControl(string ParantName, bool StackerUse, bool Simulmode)
        {
            ParentModuleName = ParantName;
            IsStackerServo = StackerUse;
            SimulMode = Simulmode;
            Thread LimitHomeCheckThread = new Thread(new ThreadStart(MonitorRun));
            LimitHomeCheckThread.IsBackground = true;
            LimitHomeCheckThread.Start();
        }


        //홈 도그,리미트 센서 전달 모니터링
        protected virtual void MonitorRun()
        {
            try //-메인 루프 예외 발생시 로그 찍도록 추가.
            {
                GlobalData.Current.MRE_CVLineCreateEvent.WaitOne();
                DoServoResetAction(); //초기화 동작시 서보 리셋
                while (true)
                {
                    SV_OUT_HOME_DOG = SV_IN_HOME_DOG;
                    SV_OUT_MINUS_LIMIT = SV_IN_MINUS_LIMIT;
                    SV_OUT_PLUS_LIMIT = SV_IN_PLUS_LIMIT;
                    Thread.Sleep(20);
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
            }
        }

        #region Servo Control 입력 I/O 서보 DO

        protected bool SV_IN_ALARM
        {
            get
            {
                if (SimulMode)
                {
                    return Simul_In_IOArray[20];
                }
                bool bValue = CCLinkManager.CCLCurrent.ReadIO(ParentModuleName, "SV_IN_ALARM");
                return bValue;
            }
            set
            {
                if (SimulMode)
                {
                    Simul_In_IOArray[20] = value;
                    return;
                }
                else
                {
                    //throw new Exception("입력값에다 쓸수 없습니다.");
                    //INPUT에 쓸수없지만 로그에만 남겨둔다.
                    LogManager.WriteConsoleLog(eLogLevel.Error, "잘못된 I/O 엑세스.입력 SV_IN_ALARM 를 쓰기 시도했습니다.");
                }
            }
        }
        protected bool SV_IN_CPO
        {
            get
            {
                if (SimulMode)
                {
                    return Simul_In_IOArray[21];
                }
                bool bValue = CCLinkManager.CCLCurrent.ReadIO(ParentModuleName, "SV_IN_CPO");
                return bValue;
            }
            set
            {
                if (SimulMode)
                {
                    Simul_In_IOArray[21] = value;
                    return;
                }
                else
                {
                    //throw new Exception("입력값에다 쓸수 없습니다.");
                    //INPUT에 쓸수없지만 로그에만 남겨둔다.
                    LogManager.WriteConsoleLog(eLogLevel.Error, "잘못된 I/O 엑세스.입력 SV_IN_CPO 를 쓰기 시도했습니다.");
                }
            }
        }
        protected bool SV_IN_ZP_COMPLETE
        {
            get
            {
                if (SimulMode)
                {
                    return Simul_In_IOArray[22];
                }
                bool bValue = CCLinkManager.CCLCurrent.ReadIO(ParentModuleName, "SV_IN_ZP_COMPLETE");
                return bValue;
            }
            set
            {
                if (SimulMode)
                {
                    Simul_In_IOArray[22] = value;
                    return;
                }
                else
                {
                    //throw new Exception("입력값에다 쓸수 없습니다.");
                    //INPUT에 쓸수없지만 로그에만 남겨둔다.
                    LogManager.WriteConsoleLog(eLogLevel.Error, "잘못된 I/O 엑세스.입력 SV_IN_ZP_COMPLETE 를 쓰기 시도했습니다.");
                }
            }
        }
        protected bool SV_IN_MOVE_END
        {
            get
            {
                if (SimulMode)
                {
                    return Simul_In_IOArray[23];
                }
                bool bValue = CCLinkManager.CCLCurrent.ReadIO(ParentModuleName, "SV_IN_MOVE_END");
                return bValue;
            }
            set
            {
                if (SimulMode)
                {
                    Simul_In_IOArray[23] = value;
                    return;
                }
                else
                {
                    //throw new Exception("입력값에다 쓸수 없습니다.");
                    //INPUT에 쓸수없지만 로그에만 남겨둔다.
                    LogManager.WriteConsoleLog(eLogLevel.Error, "잘못된 I/O 엑세스.입력 SV_IN_MOVE_END 를 쓰기 시도했습니다.");
                }
            }
        }
        protected bool SV_IN_INPOSITION
        {
            get
            {
                if (SimulMode)
                {
                    return Simul_In_IOArray[24];
                }
                bool bValue = CCLinkManager.CCLCurrent.ReadIO(ParentModuleName, "SV_IN_INPOSITION");
                return bValue;
            }
            set
            {
                if (SimulMode)
                {
                    Simul_In_IOArray[24] = value;
                    return;
                }
                else
                {
                    //throw new Exception("입력값에다 쓸수 없습니다.");
                    //INPUT에 쓸수없지만 로그에만 남겨둔다.
                    LogManager.WriteConsoleLog(eLogLevel.Error, "잘못된 I/O 엑세스.입력 SV_IN_INPOSITION 를 쓰기 시도했습니다.");
                }
            }
        }
        protected bool SV_IN_READY
        {
            get
            {
                if (SimulMode)
                {
                    return Simul_In_IOArray[25];
                }
                bool bValue = CCLinkManager.CCLCurrent.ReadIO(ParentModuleName, "SV_IN_READY");
                return bValue;
            }
            set
            {
                if (SimulMode)
                {
                    Simul_In_IOArray[25] = value;
                    return;
                }
                else
                {
                    //throw new Exception("입력값에다 쓸수 없습니다.");
                    //INPUT에 쓸수없지만 로그에만 남겨둔다.
                    LogManager.WriteConsoleLog(eLogLevel.Error, "잘못된 I/O 엑세스.입력 SV_IN_READY 를 쓰기 시도했습니다.");
                }
            }
        }

        protected bool SV_IN_MINUS_LIMIT
        {
            get
            {
                if (SimulMode)
                {
                    return Simul_In_IOArray[26];
                }
                bool bValue = CCLinkManager.CCLCurrent.ReadIO(ParentModuleName, "SV_IN_MINUS_LIMIT");
                return bValue;
            }
            set
            {
                if (SimulMode)
                {
                    Simul_In_IOArray[26] = value;
                    return;
                }
                else
                {
                    //throw new Exception("입력값에다 쓸수 없습니다.");
                    //INPUT에 쓸수없지만 로그에만 남겨둔다.
                    LogManager.WriteConsoleLog(eLogLevel.Error, "잘못된 I/O 엑세스.입력 SV_IN_MINUS_LIMIT 를 쓰기 시도했습니다.");
                }
            }
        }
        protected bool SV_IN_HOME_DOG
        {
            get
            {
                if (SimulMode)
                {
                    return Simul_In_IOArray[27];
                }
                bool bValue = CCLinkManager.CCLCurrent.ReadIO(ParentModuleName, "SV_IN_HOME_DOG");
                return bValue;
            }
            set
            {
                if (SimulMode)
                {
                    Simul_In_IOArray[27] = value;
                    return;
                }
                else
                {
                    //throw new Exception("입력값에다 쓸수 없습니다.");
                    //INPUT에 쓸수없지만 로그에만 남겨둔다.
                    LogManager.WriteConsoleLog(eLogLevel.Error, "잘못된 I/O 엑세스.입력 SV_IN_HOME_DOG 를 쓰기 시도했습니다.");
                }
            }
        }
        protected bool SV_IN_TURN_POS
        {
            get
            {
                if (SimulMode)
                {
                    return Simul_In_IOArray[28];
                }
                bool bValue = CCLinkManager.CCLCurrent.ReadIO(ParentModuleName, "SV_IN_TURN_POS");
                return bValue;
            }
            set
            {
                if (SimulMode)
                {
                    Simul_In_IOArray[28] = value;
                    return;
                }
                else
                {
                    //throw new Exception("입력값에다 쓸수 없습니다.");
                    //INPUT에 쓸수없지만 로그에만 남겨둔다.
                    LogManager.WriteConsoleLog(eLogLevel.Error, "잘못된 I/O 엑세스.입력 SV_IN_TURN_POS 를 쓰기 시도했습니다.");
                }
            }
        }
        protected bool SV_IN_PLUS_LIMIT
        {
            get
            {
                if (SimulMode)
                {
                    return Simul_In_IOArray[29];
                }
                bool bValue = CCLinkManager.CCLCurrent.ReadIO(ParentModuleName, "SV_IN_PLUS_LIMIT");
                return bValue;
            }
            set
            {
                if (SimulMode)
                {
                    Simul_In_IOArray[29] = value;
                    return;
                }
                else
                {
                    //throw new Exception("입력값에다 쓸수 없습니다.");
                    //INPUT에 쓸수없지만 로그에만 남겨둔다.
                    LogManager.WriteConsoleLog(eLogLevel.Error, "잘못된 I/O 엑세스.입력 SV_IN_PLUS_LIMIT 를 쓰기 시도했습니다.");
                }
            }
        }

        protected bool SV_IN_STACK_POS
        {
            get
            {
                if (SimulMode)
                {
                    return Simul_In_IOArray[30];
                }
                bool bValue = CCLinkManager.CCLCurrent.ReadIO(ParentModuleName, "SV_IN_STACK_POS");
                return bValue;
            }
            set
            {
                if (SimulMode)
                {
                    Simul_In_IOArray[30] = value;
                    return;
                }
                else
                {
                    //throw new Exception("입력값에다 쓸수 없습니다.");
                    //INPUT에 쓸수없지만 로그에만 남겨둔다.
                    LogManager.WriteConsoleLog(eLogLevel.Error, "잘못된 I/O 엑세스.입력 SV_IN_STACK_POS 를 쓰기 시도했습니다.");
                }
            }
        }

        protected bool SV_IN_STACK_OVER_ERROR
        {
            get
            {
                if (SimulMode)
                {
                    return Simul_In_IOArray[31];
                }
                bool bValue = CCLinkManager.CCLCurrent.ReadIO(ParentModuleName, "CV_STACK_ERROR_CHECK");
                return bValue;
            }
            set
            {
                if (SimulMode)
                {
                    Simul_In_IOArray[31] = value;
                    return;
                }
                else
                {
                    //throw new Exception("입력값에다 쓸수 없습니다.");
                    //INPUT에 쓸수없지만 로그에만 남겨둔다.
                    LogManager.WriteConsoleLog(eLogLevel.Error, "잘못된 I/O 엑세스.입력 CV_STACK_ERROR_CHECK 를 쓰기 시도했습니다.");
                }
            }
        }

        protected bool SV_IN_STACK_LOWER_POS
        {
            get
            {
                if (SimulMode)
                {
                    return Simul_In_IOArray[31];
                }
                bool bValue = CCLinkManager.CCLCurrent.ReadIO(ParentModuleName, "SV_IN_STACK_LOWER_POS");
                return bValue;
            }
            set
            {
                if (SimulMode)
                {
                    Simul_In_IOArray[31] = value;
                    return;
                }
                else
                {
                    //throw new Exception("입력값에다 쓸수 없습니다.");
                    //INPUT에 쓸수없지만 로그에만 남겨둔다.
                    LogManager.WriteConsoleLog(eLogLevel.Error, "잘못된 I/O 엑세스.입력 SV_IN_STACK_LOWER_POS 를 쓰기 시도했습니다.");
                }
            }
        }


        #endregion

        #region Servo Control 출력 I/O 서보 DI
        protected bool SV_OUT_FORCE_STOP
        {
            get
            {
                if (SimulMode)
                {
                    return Simul_Out_IOArray[20];
                }
                bool bValue = CCLinkManager.CCLCurrent.ReadIO(ParentModuleName, "SV_OUT_FORCE_STOP");
                return bValue;
            }
            set
            {
                if (SimulMode)
                {
                    Simul_Out_IOArray[20] = value;
                    return;
                }
                CCLinkManager.CCLCurrent.WriteIO(ParentModuleName, "SV_OUT_FORCE_STOP", value);
            }

        }
        protected bool SV_OUT_SERVO_ON
        {
            get
            {
                if (SimulMode)
                {
                    return Simul_Out_IOArray[21];
                }
                bool bValue = CCLinkManager.CCLCurrent.ReadIO(ParentModuleName, "SV_OUT_SERVO_ON");
                return bValue;
            }
            set
            {
                if (SimulMode)
                {
                    Simul_Out_IOArray[21] = value;
                    return;
                }
                CCLinkManager.CCLCurrent.WriteIO(ParentModuleName, "SV_OUT_SERVO_ON", value);
            }
        }
        protected bool SV_OUT_SELECT_MODE
        {
            get
            {
                if (SimulMode)
                {
                    return Simul_Out_IOArray[22];
                }
                bool bValue = CCLinkManager.CCLCurrent.ReadIO(ParentModuleName, "SV_OUT_SELECT_MODE");
                return bValue;
            }
            set
            {
                if (SimulMode)
                {
                    Simul_Out_IOArray[22] = value;
                    return;
                }
                CCLinkManager.CCLCurrent.WriteIO(ParentModuleName, "SV_OUT_SELECT_MODE", value);
            }
        }
        protected bool SV_OUT_FWD_RUN
        {
            get
            {
                if (SimulMode)
                {
                    return Simul_Out_IOArray[23];
                }
                bool bValue = CCLinkManager.CCLCurrent.ReadIO(ParentModuleName, "SV_OUT_FWD_RUN");
                return bValue;
            }
            set
            {
                if (SimulMode)
                {
                    Simul_Out_IOArray[23] = value;
                    return;
                }
                CCLinkManager.CCLCurrent.WriteIO(ParentModuleName, "SV_OUT_FWD_RUN", value);
            }
        }
        protected bool SV_OUT_BWD_RUN
        {
            get
            {
                if (SimulMode)
                {
                    return Simul_Out_IOArray[24];
                }
                bool bValue = CCLinkManager.CCLCurrent.ReadIO(ParentModuleName, "SV_OUT_BWD_RUN");
                return bValue;
            }
            set
            {
                if (SimulMode)
                {
                    Simul_Out_IOArray[24] = value;
                    return;
                }
                CCLinkManager.CCLCurrent.WriteIO(ParentModuleName, "SV_OUT_BWD_RUN", value);
            }
        }
        protected bool SV_OUT_HOME_DOG
        {
            get
            {
                if (SimulMode)
                {
                    return Simul_Out_IOArray[25];
                }
                bool bValue = CCLinkManager.CCLCurrent.ReadIO(ParentModuleName, "SV_OUT_HOME_DOG");
                return bValue;
            }
            set
            {
                if (SimulMode)
                {
                    Simul_Out_IOArray[25] = value;
                    return;
                }
                CCLinkManager.CCLCurrent.WriteIO(ParentModuleName, "SV_OUT_HOME_DOG", value);
            }
        }
        protected bool SV_OUT_PLUS_LIMIT
        {
            get
            {
                if (SimulMode)
                {
                    return Simul_Out_IOArray[26];
                }
                bool bValue = CCLinkManager.CCLCurrent.ReadIO(ParentModuleName, "SV_OUT_PLUS_LIMIT");
                return bValue;
            }
            set
            {
                if (SimulMode)
                {
                    Simul_Out_IOArray[26] = value;
                    return;
                }
                CCLinkManager.CCLCurrent.WriteIO(ParentModuleName, "SV_OUT_PLUS_LIMIT", value);
            }
        }
        protected bool SV_OUT_MINUS_LIMIT
        {
            get
            {
                if (SimulMode)
                {
                    return Simul_Out_IOArray[27];
                }
                bool bValue = CCLinkManager.CCLCurrent.ReadIO(ParentModuleName, "SV_OUT_MINUS_LIMIT");
                return bValue;
            }
            set
            {
                if (SimulMode)
                {
                    Simul_Out_IOArray[27] = value;
                    return;
                }
                CCLinkManager.CCLCurrent.WriteIO(ParentModuleName, "SV_OUT_MINUS_LIMIT", value);
            }
        }
        protected bool SV_OUT_SELECT_POINT1
        {
            get
            {
                if (SimulMode)
                {
                    return Simul_Out_IOArray[28];
                }
                bool bValue = CCLinkManager.CCLCurrent.ReadIO(ParentModuleName, "SV_OUT_SELECT_POINT1");
                return bValue;
            }
            set
            {
                if (SimulMode)
                {
                    Simul_Out_IOArray[28] = value;
                    return;
                }
                CCLinkManager.CCLCurrent.WriteIO(ParentModuleName, "SV_OUT_SELECT_POINT1", value);
            }
        }
        protected bool SV_OUT_SELECT_POINT2
        {
            get
            {
                if (SimulMode)
                {
                    return Simul_Out_IOArray[29];
                }
                bool bValue = CCLinkManager.CCLCurrent.ReadIO(ParentModuleName, "SV_OUT_SELECT_POINT2");
                return bValue;
            }
            set
            {
                if (SimulMode)
                {
                    Simul_Out_IOArray[29] = value;
                    return;
                }
                CCLinkManager.CCLCurrent.WriteIO(ParentModuleName, "SV_OUT_SELECT_POINT2", value);
            }
        }
        protected bool SV_OUT_SELECT_POINT3
        {
            get
            {
                if (SimulMode)
                {
                    return Simul_Out_IOArray[30];
                }
                bool bValue = CCLinkManager.CCLCurrent.ReadIO(ParentModuleName, "SV_OUT_SELECT_POINT3");
                return bValue;
            }
            set
            {
                if (SimulMode)
                {
                    Simul_Out_IOArray[30] = value;
                    return;
                }
                CCLinkManager.CCLCurrent.WriteIO(ParentModuleName, "SV_OUT_SELECT_POINT3", value);
            }
        }
        protected bool SV_OUT_SELECT_POINT4
        {
            get
            {
                if (SimulMode)
                {
                    return Simul_Out_IOArray[31];
                }
                bool bValue = CCLinkManager.CCLCurrent.ReadIO(ParentModuleName, "SV_OUT_SELECT_POINT4");
                return bValue;
            }
            set
            {
                if (SimulMode)
                {
                    Simul_Out_IOArray[31] = value;
                    return;
                }
                CCLinkManager.CCLCurrent.WriteIO(ParentModuleName, "SV_OUT_SELECT_POINT4", value);
            }
        }

        #endregion


        public virtual void DoServoForceStop()
        {
            SV_OUT_FORCE_STOP = false;
            SV_OUT_SELECT_POINT1 = false; //기존 포인트 번호가 남아있을수도 있으니 모두 OFF
            SV_OUT_SELECT_POINT2 = false;
            SV_OUT_SELECT_POINT3 = false;
            SV_OUT_SELECT_POINT4 = false;
            return;
        }

        public virtual bool DoServoResetAction()
        {
            SV_OUT_FORCE_STOP = true;
            SV_OUT_SELECT_MODE = true;
            SV_OUT_SERVO_ON = true;
            Thread.Sleep(50);
            SV_OUT_SELECT_POINT1 = false; //기존 포인트 번호가 남아있을수도 있으니 모두 OFF
            SV_OUT_SELECT_POINT2 = false;
            SV_OUT_SELECT_POINT3 = false;
            SV_OUT_SELECT_POINT4 = false;
            Thread.Sleep(50);
            bool bReady = SV_IN_READY;
            bool bInPos = SV_IN_INPOSITION;
            bool bAlarm = SV_IN_ALARM;
            bool bCPO = SV_IN_CPO;
            bool bMend = SV_IN_MOVE_END;

            LogManager.WriteConsoleLog(eLogLevel.Info, "DoServoResetAction Result [ InPos : {0}  CPO : {1}  Mend : {2}  Ready : {3}  Alarm : {4}]", bInPos, bCPO, bMend, bReady, bAlarm);
            if (bReady && bInPos && bAlarm && bCPO && bMend)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public virtual bool DoServoPointHomeAction()
        {
            if (SimulMode)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "DoServoPointHomeAction SimulSkip!");

                return true;
            }
            SV_OUT_SELECT_POINT1 = false; //홈 동작은 선택 비트 전부오프한다. 
            SV_OUT_SELECT_POINT2 = false;
            SV_OUT_SELECT_POINT3 = false;
            SV_OUT_SELECT_POINT4 = false;

            //2021.07.23 lim, 구동 조건
            SV_OUT_FORCE_STOP = true;
            SV_OUT_SERVO_ON = true;
            SV_OUT_SELECT_MODE = true;

            bool bMend = SV_IN_MOVE_END;
            bool bAlarm = SV_IN_ALARM;
            if (!bMend && bAlarm)  //동작전 상태 체크  //2021.07.23 lim, bMend 상시 off 상태
            {
                Thread.Sleep(20);
                //시작 비트 On/Off 로 가동
                SV_OUT_FWD_RUN = true;
                Thread.Sleep(50);
                SV_OUT_FWD_RUN = false;
                Thread.Sleep(50);
                //동작 완료를 기다린다.
                bool ZP;
                bool CPO;
                bool IPO;
                DateTime dt = DateTime.Now;
                while (!IsTimeOut(dt, PositionMoveTimeout))
                {
                    ZP = SV_IN_ZP_COMPLETE;
                    CPO = SV_IN_CPO;
                    IPO = SV_IN_INPOSITION;
                    bAlarm = SV_IN_ALARM; //알람 발생시 OFF
                    if (!bAlarm) //알람 비트 Off 되면 에러발생
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "DoServoPointHomeAction Move Alarm Occurred!");
                        return false;
                    }
                    if (ZP && IPO)
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "DoServoPointHomeAction Move Done!");
                        return true;
                    }
                    Thread.Sleep(ThreadCycle);
                }
                LogManager.WriteConsoleLog(eLogLevel.Info, "DoServoPointHomeAction Failed! Reason: Timeover");
                return false;
            }
            return false;
        }
        public virtual bool DoServoPointTableAction(byte pointNumber)
        {
            if (pointNumber > 16 || pointNumber <= 0) //포인트 번호 유효성 검사
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "Module : {0} DoServoPointTableAction Failed! pointNumber : {1}", ParentModuleName, pointNumber);
                return false;
            }
            if (SimulMode)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "DoServoPointTableAction SimulSkip! Number: {1}", pointNumber);

                return true;
            }
            SV_OUT_SELECT_POINT1 = false; //기존 포인트 번호가 남아있을수도 있으니 모두 OFF
            SV_OUT_SELECT_POINT2 = false;
            SV_OUT_SELECT_POINT3 = false;
            SV_OUT_SELECT_POINT4 = false;

            //2021.07.23 lim, 구동 조건
            SV_OUT_FORCE_STOP = true;
            SV_OUT_SERVO_ON = true;
            SV_OUT_SELECT_MODE = true;

            bool bInPos = SV_IN_INPOSITION;
            bool bCPO = SV_IN_CPO;
            bool bMend = SV_IN_MOVE_END;
            bool bAlarm = SV_IN_ALARM;
            bool bReady = SV_IN_READY;

            if (bReady && bInPos && bAlarm && !bMend)  //동작전 상태 체크
            {
                if (pointNumber > 15)
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "Module : {0} DoServoPointTableAction Failed! pointNumber : {1}", ParentModuleName, pointNumber);
                    return false;
                }
                var PointBitArray = PointNumberToBitTable(pointNumber);
                SV_OUT_SELECT_POINT1 = PointBitArray[0];
                SV_OUT_SELECT_POINT2 = PointBitArray[1];
                SV_OUT_SELECT_POINT3 = PointBitArray[2];
                SV_OUT_SELECT_POINT4 = PointBitArray[3];
                Thread.Sleep(20);
                //시작 비트 On/Off 로 가동
                SV_OUT_FWD_RUN = true;
                Thread.Sleep(50);
                SV_OUT_FWD_RUN = false;
                Thread.Sleep(1000);
                //동작 완료를 기다린다.
                bool InPos;
                bool CPO;
                bool Mend;
                DateTime dt = DateTime.Now;
                while (!IsTimeOut(dt, PositionMoveTimeout))
                {
                    if (IsStackerServo && CheckStackOverError()) //스택커 적재 에러 높이 도달  체크
                    {
                        SV_OUT_FORCE_STOP = false; //에러 높이 도달하면 정지
                        LogManager.WriteConsoleLog(eLogLevel.Info, "DoServoPointTableAction Stacker Over Alarm Occurred!");
                        return false;
                    }
                    InPos = SV_IN_INPOSITION;
                    //InPos = PointNumberToInpos(pointNumber);  //2021.07.23 lim, 포지션 이동 완료시 센서값 확인 센서 위치 셋팅 후 사용
                    CPO = SV_IN_CPO; // 조일치
                    Mend = SV_IN_MOVE_END;
                    bAlarm = SV_IN_ALARM; //알람 발생시 OFF
                    if (!bAlarm) //알람 비트 Off 되면 에러발생
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "DoServoPointTableAction Move Alarm Occurred!");
                        return false;
                    }
                    if (InPos && CPO && !Mend)
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "DoServoPointTableAction Point : {0} Move Done!", pointNumber);
                        return true;
                    }
                    Thread.Sleep(ThreadCycle);
                }
                LogManager.WriteConsoleLog(eLogLevel.Info, "DoServoPointTableAction Failed! Point : {0} Timeover", pointNumber);
                return false;
            }
            else //초기 조건 실패
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "DoServoResetAction Start Condition Faield! Result [ InPos : {0}  CPO : {1}  Mend : {2}  Ready : {3}  Alarm : {4}]", bInPos, bCPO, bMend, bReady, bAlarm);
                return false;
            }

        }

        public bool CheckHomePosition()
        {
            return SV_IN_HOME_DOG;
        }
        public bool CheckTurnPosition()
        {
            return SV_IN_TURN_POS;
        }
        public bool CheckStackPosition()
        {
            return SV_IN_STACK_POS;
        }
        public bool CheckStackOverError()
        {
            return SV_IN_STACK_OVER_ERROR;
        }
        protected bool IsTimeOut(DateTime dtstart, double secTimeout)
        {
            secTimeout = secTimeout * 1000;

            TimeSpan TLimite = TimeSpan.FromMilliseconds(secTimeout);
            TimeSpan tspan = DateTime.Now.Subtract(dtstart);
            return (tspan > TLimite) ? true : false;
        }

        protected virtual bool[] PointNumberToBitTable(int number)
        {
            bool[] Converted = new bool[4];
            if (number > 15 || number < 0)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "PointNumberToBitTable 입력 number :{0} 값이 잘못되었습니다.", number);
                return Converted;
            }
            bool[] tempArray = Convert.ToString(number, 2).PadLeft(4, '0').Select(c => c == '1').Reverse().ToArray();
            for (int i = 0; i < 4; i++)
            {
                Converted[i] = tempArray[i];
            }
            return Converted;
        }
    }


}
