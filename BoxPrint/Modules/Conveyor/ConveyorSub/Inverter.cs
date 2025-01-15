using BoxPrint.CCLink;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace BoxPrint.Modules.Conveyor
{
    /// <summary>
    /// 입력: [에러] 
    /// 출력: [정방향] [역방향] [고속] [리셋]
    ///         (1       0        1)           => 정방향 고속가동
    ///         (1       0        0)           => 정방향 저속가동
    ///         (0       1        1)           => 역방향 고속가동
    ///         (0       1        0)           => 역방향 저속가동
    /// </summary>
    public class Inverter : ModuleBase, IConveyorRunnable
    {

        public readonly int IODelay = 50;
        public readonly int ConveyorRunTimeout = 3; //컨베이어 동작명령 타임아웃

        private bool Simul_InverterFWDRunning = false;
        private bool Simul_InverterBWDRunning = false;
        private eCV_Speed Simul_InverterSpeed = eCV_Speed.None;
        private bool BackAble = false; //역방향 회전이 가능한지 체크
        private int InverterBaseAddress = 0;
        public Inverter(string mName, int BaseAddress, bool backAble, bool simul) : base(mName, simul)
        {
            InverterBaseAddress = BaseAddress;
            BackAble = backAble;
        }
        public string GetRunnerName()
        {
            return this.ModuleName;
        }
        /// <summary>
        ///Inverter I/O 생성 방식 변경.포트별 인버터 I/O 개수가 너무 많아서 BaseAddress 기준 코드에서 자동생성으로 변경.원가절감으로 미사용중
        /// </summary>
        /// <returns></returns>
        private List<IOPoint> CreateInverterIOList()
        {
            List<IOPoint> tempList = new List<IOPoint>();
            //"IVT_FORWARD_RUN"            X0     
            //"IVT_BACKWARD_RUN"           X1     
            //"IVT_RUNNING"                X2     
            //"IVT_FREQUENCY_REACHED"      X3  
            //"IVT_OVERLOAD_ALARM"         X4  
            //"IVT_POWER_FAILURE"          X5  
            //"IVT_FREQUENCY_DETECTED"     X6  
            //"IVT_ERROR"                  X7  
            //"IVT_TERMINAL_ABC_FUNC"      X8  
            //"IVT_PR313_FUNC"             X9  
            //"IVT_PR314_FUNC"             X10 
            //"IVT_PR315_FUNC"             X11 
            //"IVT_MONITORING"             X12
            //"IVT_FREQUENCY_SET_COMP1"    X13
            //"IVT_FREQUENCY_SET_COMP2"    X14
            //"IVT_INSTRUCTION_EXECUTED"   X15
            //"IVT_ERR_STATUS"             X26
            //"IVT_REMOTE_STATION_READY"   X27   


            //"IVT_FORWARD"                     Y0
            //"IVT_BACKWARD"                    Y1
            //"IVT_HIGHSPEED"                   Y2
            //"IVT_MIDSPEED"                    Y3
            //"IVT_LOWSPEED"                    Y4
            //"IVT_JOG_OPERERATION"             Y5
            //"IVT_SEC_FUNC"                    Y6
            //"IVT_CURRENT_INPUT_SELECTION"     Y7
            //"IVT_AUTORECOVERY"                Y8
            //"IVT_STOP"                        Y9
            //"IVT_START_SELFHOLD"              Y10
            //"IVT_RESET"                       Y11
            //"IVT_MONITOR"                     Y12
            //"IVT_FREQUENCY_SET1"              Y13
            //"IVT_FREQUENCY_SET2"              Y14
            //"IVT_INSTRUCTION_EXCUTE"          Y15
            //"IVT_ERROR_RESET"                 Y26

            tempList.Add(new IOPoint() { Group = eIOGroup.Port, Name = "IVT_FORWARD_RUN", Description = "IVT_FORWARD_RUN", ModuleID = this.ModuleName, Direction = eIODirectionTypeList.In, Active = true, Board = 0, Address = "0x" + (InverterBaseAddress + 0).ToString("X") });
            tempList.Add(new IOPoint() { Group = eIOGroup.Port, Name = "IVT_BACKWARD_RUN", Description = "IVT_BACKWARD_RUN", ModuleID = this.ModuleName, Direction = eIODirectionTypeList.In, Active = true, Board = 0, Address = "0x" + (InverterBaseAddress + 1).ToString("X") });
            tempList.Add(new IOPoint() { Group = eIOGroup.Port, Name = "IVT_RUNNING", Description = "IVT_RUNNING", ModuleID = this.ModuleName, Direction = eIODirectionTypeList.In, Active = true, Board = 0, Address = "0x" + (InverterBaseAddress + 2).ToString("X") });
            tempList.Add(new IOPoint() { Group = eIOGroup.Port, Name = "IVT_FREQUENCY_REACHED", Description = "IVT_FREQUENCY_REACHED", ModuleID = this.ModuleName, Direction = eIODirectionTypeList.In, Active = true, Board = 0, Address = "0x" + (InverterBaseAddress + 3).ToString("X") });
            tempList.Add(new IOPoint() { Group = eIOGroup.Port, Name = "IVT_OVERLOAD_ALARM", Description = "IVT_OVERLOAD_ALARM", ModuleID = this.ModuleName, Direction = eIODirectionTypeList.In, Active = true, Board = 0, Address = "0x" + (InverterBaseAddress + 4).ToString("X") });
            tempList.Add(new IOPoint() { Group = eIOGroup.Port, Name = "IVT_POWER_FAILURE", Description = "IVT_POWER_FAILURE", ModuleID = this.ModuleName, Direction = eIODirectionTypeList.In, Active = true, Board = 0, Address = "0x" + (InverterBaseAddress + 5).ToString("X") });
            tempList.Add(new IOPoint() { Group = eIOGroup.Port, Name = "IVT_FREQUENCY_DETECTED", Description = "IVT_FREQUENCY_DETECTED", ModuleID = this.ModuleName, Direction = eIODirectionTypeList.In, Active = true, Board = 0, Address = "0x" + (InverterBaseAddress + 6).ToString("X") });
            tempList.Add(new IOPoint() { Group = eIOGroup.Port, Name = "IVT_ERROR", Description = "IVT_ERROR", ModuleID = this.ModuleName, Direction = eIODirectionTypeList.In, Active = true, Board = 0, Address = "0x" + (InverterBaseAddress + 7).ToString("X") });
            tempList.Add(new IOPoint() { Group = eIOGroup.Port, Name = "IVT_TERMINAL_ABC_FUNC", Description = "IVT_TERMINAL_ABC_FUNC", ModuleID = this.ModuleName, Direction = eIODirectionTypeList.In, Active = true, Board = 0, Address = "0x" + (InverterBaseAddress + 8).ToString("X") });
            tempList.Add(new IOPoint() { Group = eIOGroup.Port, Name = "IVT_PR313_FUNC", Description = "IVT_PR313_FUNC", ModuleID = this.ModuleName, Direction = eIODirectionTypeList.In, Active = true, Board = 0, Address = "0x" + (InverterBaseAddress + 9).ToString("X") });
            tempList.Add(new IOPoint() { Group = eIOGroup.Port, Name = "IVT_PR314_FUNC", Description = "IVT_PR314_FUNC", ModuleID = this.ModuleName, Direction = eIODirectionTypeList.In, Active = true, Board = 0, Address = "0x" + (InverterBaseAddress + 10).ToString("X") });
            tempList.Add(new IOPoint() { Group = eIOGroup.Port, Name = "IVT_PR315_FUNC", Description = "IVT_PR315_FUNC", ModuleID = this.ModuleName, Direction = eIODirectionTypeList.In, Active = true, Board = 0, Address = "0x" + (InverterBaseAddress + 11).ToString("X") });
            tempList.Add(new IOPoint() { Group = eIOGroup.Port, Name = "IVT_MONITORING", Description = "IVT_MONITORING", ModuleID = this.ModuleName, Direction = eIODirectionTypeList.In, Active = true, Board = 0, Address = "0x" + (InverterBaseAddress + 12).ToString("X") });
            tempList.Add(new IOPoint() { Group = eIOGroup.Port, Name = "IVT_FREQUENCY_SET_COMP1", Description = "IVT_FREQUENCY_SET_COMP1", ModuleID = this.ModuleName, Direction = eIODirectionTypeList.In, Active = true, Board = 0, Address = "0x" + (InverterBaseAddress + 13).ToString("X") });
            tempList.Add(new IOPoint() { Group = eIOGroup.Port, Name = "IVT_FREQUENCY_SET_COMP2", Description = "IVT_FREQUENCY_SET_COMP2", ModuleID = this.ModuleName, Direction = eIODirectionTypeList.In, Active = true, Board = 0, Address = "0x" + (InverterBaseAddress + 14).ToString("X") });
            tempList.Add(new IOPoint() { Group = eIOGroup.Port, Name = "IVT_INSTRUCTION_EXECUTED", Description = "IVT_INSTRUCTION_EXECUTED", ModuleID = this.ModuleName, Direction = eIODirectionTypeList.In, Active = true, Board = 0, Address = "0x" + (InverterBaseAddress + 15).ToString("X") });
            tempList.Add(new IOPoint() { Group = eIOGroup.Port, Name = "IVT_ERR_STATUS", Description = "IVT_ERR_STATUS", ModuleID = this.ModuleName, Direction = eIODirectionTypeList.In, Active = true, Board = 0, Address = "0x" + (InverterBaseAddress + 26).ToString("X") });
            tempList.Add(new IOPoint() { Group = eIOGroup.Port, Name = "IVT_REMOTE_STATION_READY", Description = "IVT_REMOTE_STATION_READY", ModuleID = this.ModuleName, Direction = eIODirectionTypeList.In, Active = true, Board = 0, Address = "0x" + (InverterBaseAddress + 27).ToString("X") });

            tempList.Add(new IOPoint() { Group = eIOGroup.Port, Name = "IVT_FORWARD", Description = "IVT_FORWARD", ModuleID = this.ModuleName, Direction = eIODirectionTypeList.Out, Active = true, Board = 0, Address = "0x" + (InverterBaseAddress + 0).ToString("X") });
            tempList.Add(new IOPoint() { Group = eIOGroup.Port, Name = "IVT_BACKWARD", Description = "IVT_BACKWARD", ModuleID = this.ModuleName, Direction = eIODirectionTypeList.Out, Active = true, Board = 0, Address = "0x" + (InverterBaseAddress + 1).ToString("X") });
            tempList.Add(new IOPoint() { Group = eIOGroup.Port, Name = "IVT_HIGHSPEED", Description = "IVT_HIGHSPEED", ModuleID = this.ModuleName, Direction = eIODirectionTypeList.Out, Active = true, Board = 0, Address = "0x" + (InverterBaseAddress + 2).ToString("X") });
            tempList.Add(new IOPoint() { Group = eIOGroup.Port, Name = "IVT_MIDSPEED", Description = "IVT_MIDSPEED", ModuleID = this.ModuleName, Direction = eIODirectionTypeList.Out, Active = true, Board = 0, Address = "0x" + (InverterBaseAddress + 3).ToString("X") });
            tempList.Add(new IOPoint() { Group = eIOGroup.Port, Name = "IVT_LOWSPEED", Description = "IVT_LOWSPEED", ModuleID = this.ModuleName, Direction = eIODirectionTypeList.Out, Active = true, Board = 0, Address = "0x" + (InverterBaseAddress + 4).ToString("X") });
            tempList.Add(new IOPoint() { Group = eIOGroup.Port, Name = "IVT_JOG_OPERERATION", Description = "IVT_JOG_OPERERATION", ModuleID = this.ModuleName, Direction = eIODirectionTypeList.Out, Active = true, Board = 0, Address = "0x" + (InverterBaseAddress + 5).ToString("X") });
            tempList.Add(new IOPoint() { Group = eIOGroup.Port, Name = "IVT_SEC_FUNC", Description = "IVT_SEC_FUNC", ModuleID = this.ModuleName, Direction = eIODirectionTypeList.Out, Active = true, Board = 0, Address = "0x" + (InverterBaseAddress + 6).ToString("X") });
            tempList.Add(new IOPoint() { Group = eIOGroup.Port, Name = "IVT_CURRENT_INPUT_SELECTION", Description = "IVT_CURRENT_INPUT_SELECTION", ModuleID = this.ModuleName, Direction = eIODirectionTypeList.Out, Active = true, Board = 0, Address = "0x" + (InverterBaseAddress + 7).ToString("X") });
            tempList.Add(new IOPoint() { Group = eIOGroup.Port, Name = "IVT_AUTORECOVERY", Description = "IVT_AUTORECOVERY", ModuleID = this.ModuleName, Direction = eIODirectionTypeList.Out, Active = true, Board = 0, Address = "0x" + (InverterBaseAddress + 8).ToString("X") });
            tempList.Add(new IOPoint() { Group = eIOGroup.Port, Name = "IVT_STOP", Description = "IVT_STOP", ModuleID = this.ModuleName, Direction = eIODirectionTypeList.Out, Active = true, Board = 0, Address = "0x" + (InverterBaseAddress + 9).ToString("X") });
            tempList.Add(new IOPoint() { Group = eIOGroup.Port, Name = "IVT_START_SELFHOLD", Description = "IVT_START_SELFHOLD", ModuleID = this.ModuleName, Direction = eIODirectionTypeList.Out, Active = true, Board = 0, Address = "0x" + (InverterBaseAddress + 10).ToString("X") });
            tempList.Add(new IOPoint() { Group = eIOGroup.Port, Name = "IVT_RESET", Description = "IVT_RESET", ModuleID = this.ModuleName, Direction = eIODirectionTypeList.Out, Active = true, Board = 0, Address = "0x" + (InverterBaseAddress + 11).ToString("X") });
            tempList.Add(new IOPoint() { Group = eIOGroup.Port, Name = "IVT_MONITOR", Description = "IVT_MONITOR", ModuleID = this.ModuleName, Direction = eIODirectionTypeList.Out, Active = true, Board = 0, Address = "0x" + (InverterBaseAddress + 12).ToString("X") });
            tempList.Add(new IOPoint() { Group = eIOGroup.Port, Name = "IVT_FREQUENCY_SET1", Description = "IVT_FREQUENCY_SET1", ModuleID = this.ModuleName, Direction = eIODirectionTypeList.Out, Active = true, Board = 0, Address = "0x" + (InverterBaseAddress + 13).ToString("X") });
            tempList.Add(new IOPoint() { Group = eIOGroup.Port, Name = "IVT_FREQUENCY_SET2", Description = "IVT_FREQUENCY_SET2", ModuleID = this.ModuleName, Direction = eIODirectionTypeList.Out, Active = true, Board = 0, Address = "0x" + (InverterBaseAddress + 14).ToString("X") });
            tempList.Add(new IOPoint() { Group = eIOGroup.Port, Name = "IVT_INSTRUCTION_EXCUTE", Description = "IVT_INSTRUCTION_EXCUTE", ModuleID = this.ModuleName, Direction = eIODirectionTypeList.Out, Active = true, Board = 0, Address = "0x" + (InverterBaseAddress + 15).ToString("X") });
            tempList.Add(new IOPoint() { Group = eIOGroup.Port, Name = "IVT_ERROR_RESET", Description = "IVT_ERROR_RESET", ModuleID = this.ModuleName, Direction = eIODirectionTypeList.Out, Active = true, Board = 0, Address = "0x" + (InverterBaseAddress + 26).ToString("X") });
            return tempList;
        }
        public bool ClearOutputIO()
        {
            if (SimulMode)
            {
                return true;
            }
            if (BackAble)
            {
                OUT_BACKWARD = false;
                OUT_HIGHSPEED = false;
            }
            OUT_FORWARD = false;
            OUT_LOWSPEED = false;
            OUT_ERROR_RESET = false;
            return true;
        }

        public bool InitConveyorRunner()
        {
            if (SimulMode)
                return true;
            return CV_Reset(); //초기화시 리셋시도
        }
        public bool CV_CheckFWD_Running()
        {
            if (SimulMode)
            {
                return Simul_InverterFWDRunning;
            }

            //I/O 절감으로 변경.
            bool bAlarm = IN_ERROR;
            bool bFwdCommand = OUT_FORWARD;
            return !bAlarm && bFwdCommand;
        }
        public bool CV_CheckBWD_Running()
        {
            if (!BackAble)
                return false;
            if (SimulMode)
            {
                return Simul_InverterBWDRunning;
            }

            //I/O 절감으로 변경.
            bool bAlarm = IN_ERROR;
            bool bBwdCommand = OUT_BACKWARD;
            return !bAlarm && bBwdCommand;
        }
        /// <summary>
        /// 에러 상태 체크
        /// </summary>
        /// <returns>
        /// True  : 에러
        /// False : 정상
        /// </returns>
        public bool CV_CheckError()
        {
            if (SimulMode)
            {
                return false;
            }

            return IN_ERROR;
            //bool bError = IN_ERROR;
            //bool bStatus = IN_ERR_STATUS;
            //return bError || bStatus;
        }
        public bool CV_EMGStop() //EMG 즉시 정지
        {
            if (SimulMode)
            {
                Simul_InverterFWDRunning = false;
                Simul_InverterBWDRunning = false;
                Simul_InverterSpeed = eCV_Speed.None;
                return true;
            }
            CV_Stop();
            return true;
        }
        public bool CV_Stop() //감속 정지
        {
            if (SimulMode)
            {
                Simul_InverterFWDRunning = false;
                Simul_InverterBWDRunning = false;
                Simul_InverterSpeed = eCV_Speed.None;
                return true;
            }
            if (BackAble)
            {
                OUT_BACKWARD = false;
                OUT_HIGHSPEED = false;
            }
            OUT_FORWARD = false;
            OUT_LOWSPEED = false;
            OUT_ERROR_RESET = false;
            return true;
        }
        /// <summary>
        /// 인버터 리셋 요청
        /// </summary>
        /// <returns> 
        /// True:  리셋 성공
        /// False: 리셋 실패
        /// </returns>
        public bool CV_Reset()
        {
            if (SimulMode)
            {
                return true;
            }
            ClearOutputIO();

            OUT_ERROR_RESET = true;
            Thread.Sleep(IODelay);
            OUT_ERROR_RESET = false;
            Thread.Sleep(IODelay);
            if (CV_CheckError()) //리셋 출력 ON -> OFF 후 다시 에러 확인
            {
                return false;
            }
            else
            {
                return true;
            }

        }

        public bool CV_ForwardRun(eCV_Speed spd)
        {
            if (SimulMode)
            {
                Simul_InverterFWDRunning = true;
                Simul_InverterBWDRunning = false;
                Simul_InverterSpeed = spd;
                return true;
            }
            Thread.Sleep(IODelay);
            if (BackAble)
            {
                OUT_BACKWARD = false;
                switch (spd)
                {
                    case eCV_Speed.Low:
                        OUT_HIGHSPEED = false;
                        OUT_LOWSPEED = true;
                        break;
                    case eCV_Speed.Mid:
                        OUT_HIGHSPEED = false;
                        OUT_LOWSPEED = false;
                        break;
                    case eCV_Speed.High:
                        OUT_HIGHSPEED = true;
                        OUT_LOWSPEED = false;
                        break;

                }
            }
            else
            {
                switch (spd)
                {
                    case eCV_Speed.Low:
                        OUT_LOWSPEED = true;
                        break;
                    case eCV_Speed.Mid:
                        OUT_LOWSPEED = false;
                        break;
                    case eCV_Speed.High:
                        OUT_LOWSPEED = false;
                        break;
                }
            }
            OUT_FORWARD = true;


            //입력 비트 확인
            Stopwatch timeWatch = Stopwatch.StartNew();
            while (!IsTimeout_SW(timeWatch, ConveyorRunTimeout))
            {
                if (CV_CheckFWD_Running())
                {
                    return true;
                }
                Thread.Sleep(IODelay);
            }
            GlobalData.Current.Alarm_Manager.AlarmOccurbyName("INVERTER_RUN_ERROR", this.ParentModule.ModuleName);
            return false;
        }
        public bool CV_BackwardRun(eCV_Speed spd)
        {
            if (SimulMode)
            {
                Simul_InverterFWDRunning = false;
                Simul_InverterBWDRunning = true;
                Simul_InverterSpeed = spd;
                return true;
            }
            ClearOutputIO();
            Thread.Sleep(IODelay);
            OUT_FORWARD = false;
            OUT_BACKWARD = true;

            switch (spd)
            {
                case eCV_Speed.Low:
                    OUT_HIGHSPEED = false;
                    OUT_LOWSPEED = true;
                    break;
                case eCV_Speed.Mid:
                    OUT_LOWSPEED = false;
                    OUT_HIGHSPEED = false;
                    break;
                case eCV_Speed.High:
                    OUT_HIGHSPEED = true;
                    OUT_LOWSPEED = false;

                    break;

            }
            //입력 비트 확인
            Stopwatch timeWatch = Stopwatch.StartNew();
            while (!IsTimeout_SW(timeWatch, ConveyorRunTimeout))
            {
                if (CV_CheckBWD_Running())
                {
                    return true;
                }
                Thread.Sleep(IODelay);
            }
            GlobalData.Current.Alarm_Manager.AlarmOccurbyName("INVERTER_RUN_ERROR", this.ParentModule.ModuleName);
            return false;
        }

        public eCV_Speed CV_GetCurrentRunningSpeed()
        {
            if (SimulMode)
            {
                return Simul_InverterSpeed;
            }
            bool bFRun = CV_CheckFWD_Running();
            bool bBRun = CV_CheckBWD_Running();
            if (bFRun || bBRun)
            {
                if (OUT_LOWSPEED) //저속 출력 으로 본다.
                    return eCV_Speed.Low;
                else
                    return eCV_Speed.High;
            }
            else
            {
                return eCV_Speed.None;
            }
        }

        #region Inverter 입력 접점
        private bool IN_ERROR
        {
            get
            {
                bool bValue = CCLinkManager.CCLCurrent.ReadIO(this.ParentModule.ModuleName, "INV_ALARM");
                return bValue;
            }
        }
        #region  I/O 절감으로 삭제
        //private bool IN_FORWARD_RUN
        //{
        //    get
        //    {
        //        bool bValue = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "IVT_FORWARD_RUN");
        //        return bValue;
        //    }
        //}

        //private bool IN_BACKWARD_RUN
        //{
        //    get
        //    {
        //        bool bValue = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "IVT_BACKWARD_RUN");
        //        return bValue;
        //    }
        //}

        //private bool IN_RUNNING
        //{
        //    get
        //    {
        //        bool bValue = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "IVT_RUNNING");
        //        return bValue;
        //    }
        //}

        //private bool IN_FREQUENCY_REACHED
        //{
        //    get
        //    {
        //        bool bValue = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "IVT_FREQUENCY_REACHED");
        //        return bValue;
        //    }
        //}

        //private bool IN_OVERLOAD_ALARM
        //{
        //    get
        //    {
        //        bool bValue = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "IVT_OVERLOAD_ALARM");
        //        return bValue;
        //    }
        //}

        //private bool IN_POWER_FAILURE
        //{
        //    get
        //    {
        //        bool bValue = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "IVT_POWER_FAILURE");
        //        return bValue;
        //    }
        //}

        //private bool IN_FREQUENCY_DETECTED
        //{
        //    get
        //    {
        //        bool bValue = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "IVT_FREQUENCY_DETECTED");
        //        return bValue;
        //    }
        //}

        //private bool IN_TERMINAL_ABC_FUNC
        //{
        //    get
        //    {
        //        bool bValue = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "IVT_TERMINAL_ABC_FUNC");
        //        return bValue;
        //    }
        //}
        //private bool IN_PR313_FUNC
        //{
        //    get
        //    {
        //        bool bValue = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "IVT_PR313_FUNC");
        //        return bValue;
        //    }
        //}
        //private bool IN_PR314_FUNC
        //{
        //    get
        //    {
        //        bool bValue = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "IVT_PR314_FUNC");
        //        return bValue;
        //    }
        //}
        //private bool IN_PR315_FUNC
        //{
        //    get
        //    {
        //        bool bValue = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "IVT_PR315_FUNC");
        //        return bValue;
        //    }
        //}

        //private bool IN_MONITORING
        //{
        //    get
        //    {
        //        bool bValue = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "IVT_MONITORING");
        //        return bValue;
        //    }
        //}
        //private bool IN_FREQUENCY_SET_COMP1
        //{
        //    get
        //    {
        //        bool bValue = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "IVT_FREQUENCY_SET_COMP1");
        //        return bValue;
        //    }
        //}
        //private bool IN_FREQUENCY_SET_COMP2
        //{
        //    get
        //    {
        //        bool bValue = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "IVT_FREQUENCY_SET_COMP2");
        //        return bValue;
        //    }
        //}
        //private bool IN_INSTRUCTION_EXECUTED
        //{
        //    get
        //    {
        //        bool bValue = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "IVT_INSTRUCTION_EXECUTED");
        //        return bValue;
        //    }
        //}
        //private bool IN_ERR_STATUS
        //{
        //    get
        //    {
        //        bool bValue = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "IVT_ERR_STATUS");
        //        return bValue;
        //    }
        //}
        //private bool IN_REMOTE_STATION_READY
        //{
        //    get
        //    {
        //        bool bValue = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "IVT_REMOTE_STATION_READY");
        //        return bValue;
        //    }
        //}
        #endregion

        #endregion

        #region Inverter 출력 접점
        private bool OUT_FORWARD
        {
            get
            {
                bool bValue = CCLinkManager.CCLCurrent.ReadIO(this.ParentModule.ModuleName, "INV_RUN_FWD");
                return bValue;
            }
            set
            {
                CCLinkManager.CCLCurrent.WriteIO(this.ParentModule.ModuleName, "INV_RUN_FWD", value);
            }
        }
        private bool OUT_BACKWARD
        {
            get
            {
                bool bValue = CCLinkManager.CCLCurrent.ReadIO(this.ParentModule.ModuleName, "INV_RUN_BWD");
                return bValue;
            }
            set
            {
                CCLinkManager.CCLCurrent.WriteIO(this.ParentModule.ModuleName, "INV_RUN_BWD", value);
            }
        }
        private bool OUT_HIGHSPEED
        {
            get
            {
                bool bValue = CCLinkManager.CCLCurrent.ReadIO(this.ParentModule.ModuleName, "INV_SPD_HIGH");
                return bValue;
            }
            set
            {
                CCLinkManager.CCLCurrent.WriteIO(this.ParentModule.ModuleName, "INV_SPD_HIGH", value);
            }
        }
        private bool OUT_LOWSPEED
        {
            get
            {
                bool bValue = CCLinkManager.CCLCurrent.ReadIO(this.ParentModule.ModuleName, "INV_SPD_LOW");
                return bValue;
            }
            set
            {
                CCLinkManager.CCLCurrent.WriteIO(this.ParentModule.ModuleName, "INV_SPD_LOW", value);
            }
        }
        private bool OUT_ERROR_RESET
        {
            get
            {
                bool bValue = CCLinkManager.CCLCurrent.ReadIO(this.ParentModule.ModuleName, "INV_RESET");
                return bValue;
            }
            set
            {
                CCLinkManager.CCLCurrent.WriteIO(this.ParentModule.ModuleName, "INV_RESET", value);
            }
        }
        #region I/O 절감으로 삭제
        //private bool OUT_MIDSPEED
        //{
        //    get
        //    {
        //        bool bValue = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "IVT_MIDSPEED");
        //        return bValue;
        //    }
        //    set
        //    {
        //        CCLinkManager.CCLCurrent.WriteIO(ModuleName, "IVT_MIDSPEED", value);
        //    }
        //}
        //private bool OUT_LOWSPEED
        //{
        //    get
        //    {
        //        bool bValue = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "IVT_LOWSPEED");
        //        return bValue;
        //    }
        //    set
        //    {
        //        CCLinkManager.CCLCurrent.WriteIO(ModuleName, "IVT_LOWSPEED", value);
        //    }
        //}
        //private bool OUT_JOG_OPERERATION
        //{
        //    get
        //    {
        //        bool bValue = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "IVT_JOG_OPERERATION");
        //        return bValue;
        //    }
        //    set
        //    {
        //        CCLinkManager.CCLCurrent.WriteIO(ModuleName, "IVT_JOG_OPERERATION", value);
        //    }
        //}
        //private bool OUT_SEC_FUNC
        //{
        //    get
        //    {
        //        bool bValue = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "IVT_SEC_FUNC");
        //        return bValue;
        //    }
        //    set
        //    {
        //        CCLinkManager.CCLCurrent.WriteIO(ModuleName, "IVT_SEC_FUNC", value);
        //    }
        //}
        //private bool OUT_CURRENT_INPUT_SELECTION
        //{
        //    get
        //    {
        //        bool bValue = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "IVT_CURRENT_INPUT_SELECTION");
        //        return bValue;
        //    }
        //    set
        //    {
        //        CCLinkManager.CCLCurrent.WriteIO(ModuleName, "IVT_CURRENT_INPUT_SELECTION", value);
        //    }
        //}
        //private bool OUT_AUTORECOVERY
        //{
        //    get
        //    {
        //        bool bValue = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "IVT_AUTORECOVERY");
        //        return bValue;
        //    }
        //    set
        //    {
        //        CCLinkManager.CCLCurrent.WriteIO(ModuleName, "IVT_AUTORECOVERY", value);
        //    }
        //}
        //private bool OUT_STOP
        //{
        //    get
        //    {
        //        bool bValue = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "IVT_STOP");
        //        return bValue;
        //    }
        //    set
        //    {
        //        CCLinkManager.CCLCurrent.WriteIO(ModuleName, "IVT_STOP", value);
        //    }
        //}
        //private bool OUT_START_SELFHOLD
        //{
        //    get
        //    {
        //        bool bValue = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "IVT_START_SELFHOLD");
        //        return bValue;
        //    }
        //    set
        //    {
        //        CCLinkManager.CCLCurrent.WriteIO(ModuleName, "IVT_START_SELFHOLD", value);
        //    }
        //}
        //private bool OUT_RESET
        //{
        //    get
        //    {
        //        bool bValue = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "INV_RESET");
        //        return bValue;
        //    }
        //    set
        //    {
        //        CCLinkManager.CCLCurrent.WriteIO(ModuleName, "INV_RESET", value);
        //    }
        //}
        //private bool OUT_MONITOR
        //{
        //    get
        //    {
        //        bool bValue = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "IVT_MONITOR");
        //        return bValue;
        //    }
        //    set
        //    {
        //        CCLinkManager.CCLCurrent.WriteIO(ModuleName, "IVT_MONITOR", value);
        //    }
        //}

        //private bool OUT_FREQUENCY_SET1
        //{
        //    get
        //    {
        //        bool bValue = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "IVT_FREQUENCY_SET1");
        //        return bValue;
        //    }
        //    set
        //    {
        //        CCLinkManager.CCLCurrent.WriteIO(ModuleName, "IVT_FREQUENCY_SET1", value);
        //    }
        //}
        //private bool OUT_FREQUENCY_SET2
        //{
        //    get
        //    {
        //        bool bValue = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "IVT_FREQUENCY_SET2");
        //        return bValue;
        //    }
        //    set
        //    {
        //        CCLinkManager.CCLCurrent.WriteIO(ModuleName, "IVT_FREQUENCY_SET2", value);
        //    }
        //}
        //private bool OUT_INSTRUCTION_EXCUTE
        //{
        //    get
        //    {
        //        bool bValue = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "IVT_INSTRUCTION_EXCUTE");
        //        return bValue;
        //    }
        //    set
        //    {
        //        CCLinkManager.CCLCurrent.WriteIO(ModuleName, "IVT_INSTRUCTION_EXCUTE", value);
        //    }
        //}
        #endregion

        #endregion
    }
}
