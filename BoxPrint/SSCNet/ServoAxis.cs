using mc2xxstd;
using BoxPrint.Log;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;

namespace BoxPrint.SSCNet
{
    //개별 서보모터 축 관리 클래스
    public class ServoAxis : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        public static readonly int CurrentPosFailValue = 99999999;

        public static Dictionary<int, ServoAxis> ServoDic = new Dictionary<int, ServoAxis>();
        private SSCInterruptDrive ServoDrive;
        private bool SimulMode;
        private bool HomeReferenced;
        public readonly string AxisName;

        public readonly bool LinearAxis; //기동전 홈 동작이 필요함

        private readonly int MaxSpeed = 500;
        private readonly int PlusSoftLimit;
        private readonly int MinusSoftLimit;
        private readonly int cServoCheckMargin = 100; //서보 디폴트 값 1600
        private readonly int ThreadCycle = 50; //서보 Cycle 딜레이 변경 200ms->50ms
        private readonly int PositionMoveTimeout = 180;

        private readonly int _AxisNumber;
        public int AxisNumber
        {
            get
            {
                return _AxisNumber;
            }
        }
        public bool IsServoOn
        {
            get
            {
                if (SimulMode)
                {
                    return true;
                }
                else
                {
                    return CheckServoOn();
                }
            }
        }
        public bool IsAlarmState
        {
            get
            {
                if (SimulMode)
                {
                    return false;
                }
                else
                {
                    return CheckAxisAlarmExist();
                }
            }
        }


        public int Simul_CurrentPosition;
        public int CurrentPosition
        {
            get
            {

                if (SimulMode)
                {
                    return Simul_CurrentPosition;
                }
                else
                {
                    return GetCurrentServoPosition();
                }
            }
        }

        public int Simul_CommandPosition;
        public int CommandPosition
        {
            get
            {

                if (SimulMode)
                {
                    return Simul_CurrentPosition;
                }
                else
                {
                    return GetCurrentServoPosition();
                }
            }
        }



        public ServoAxis(string Name, int Axis, bool IsLinearAxis, int PlusLimit, int MinusLimit, bool Simul)
        {
            ServoDic.Add(Axis, this); //만일 중복축이 있으면 Exception 발생.
            ServoDrive = ServoManager.GetManagerInstance().GetServoDrive();
            AxisName = Name;
            _AxisNumber = Axis;
            LinearAxis = IsLinearAxis;
            PlusSoftLimit = PlusLimit;
            MinusSoftLimit = MinusLimit;
            SimulMode = Simul;
        }
        public void UpdateServoAxis()
        {
            OnPropertyChanged("IsServoOn");
            OnPropertyChanged("IsAlarmState");
            OnPropertyChanged("CurrentPosition");
            OnPropertyChanged("CommandPosition");
            //CheckServoOn();
            //CheckAxisAlarmExist();
            //GetCurrentServoPosition();
            //GetCommandServoPosition();
        }


        public int ServoOn()
        {
            if (SimulMode)
                return 0;

            int ans = ServoDrive.sServoOnCommand(this.AxisNumber);
            return ans;
        }
        public int ServoStop()
        {
            if (SimulMode)
                return 0;

            int ans = ServoDrive.sServoStop(this.AxisNumber);
            return ans;
        }
        public int ServoOff()
        {
            if (SimulMode)
                return 0;

            int ans = ServoDrive.sServoOFFCmd(this.AxisNumber);
            return ans;
        }
        public bool CheckHomeReferenced() //홈상태가 유효한가?
        {
            if (SimulMode)
                return true;
            else
                return HomeReferenced;
        }
        public void SetHomeReferenced()
        {
            HomeReferenced = true;
        }
        public bool CheckAxisAlarmExist()
        {
            bool bAlarmCodeExist = false;
            ushort AlarmCode = 0;
            ushort DetailCode = 0;
            //시스템 알람 획득
            int ansSystem = ServoDrive.sGetAlarm(out AlarmCode, out DetailCode);
            if (ansSystem != 0)
            {
                LogManager.WriteServoLog(eLogLevel.Warn, String.Format("sGetAlarm failure. axnum={0}, sscGetLastError=0x{1:X8}", AxisNumber, SscApi.sscGetLastError()));
                return true;
            }
            else //응답 정상일경우 알람 코드 확인.
            {
                if (AlarmCode != 0)
                {
                    bAlarmCodeExist = true;
                    LogManager.WriteServoLog(eLogLevel.Warn, String.Format("System Alarm Code :{0:X4} : DetailCode :{1:X4} ", AlarmCode, DetailCode));
                }
            }
            //서보 알람 획득
            int ansServo = ServoDrive.sGetServoAlarm(AxisNumber, out AlarmCode, out DetailCode);
            if (ansServo != 0)
            {
                LogManager.WriteServoLog(eLogLevel.Warn, String.Format("sGetServoAlarm failure. axnum={0}, sscGetLastError=0x{1:X8}", AxisNumber, SscApi.sscGetLastError()));

                return true;
            }
            else //응답 정상일경우 알람 코드 확인.
            {
                if (AlarmCode != 0)
                {
                    bAlarmCodeExist = true;
                    LogManager.WriteServoLog(eLogLevel.Warn, String.Format("Servo Alarm Code :{0:X4} : DetailCode :{1:X4} ", AlarmCode, DetailCode));
                }
            }
            //운영 알람 획득
            int ansOper = ServoDrive.sGetOperAlarm(AxisNumber, out AlarmCode, out DetailCode);
            if (ansOper != 0)
            {
                LogManager.WriteServoLog(eLogLevel.Warn, String.Format("sGetOperAlarm failure. axnum={0}, sscGetLastError=0x{1:X8}", AxisNumber, SscApi.sscGetLastError()));
                return true;
            }
            else //응답 정상일경우 알람 코드 확인.
            {
                if (AlarmCode != 0)
                {
                    bAlarmCodeExist = true;
                    LogManager.WriteServoLog(eLogLevel.Warn, String.Format("Oper Alarm Code :{0:X4} : DetailCode :{1:X4} ", AlarmCode, DetailCode));
                }
            }
            return bAlarmCodeExist;
        }

        public bool GetAxisAlarmCode(out string SystemAlarmCode, out string ServoAlarmCode, out string OperationAlarmCode)
        {
            SystemAlarmCode = "0";
            ServoAlarmCode = "0";
            OperationAlarmCode = "0";
            ushort AlarmCode = 0;
            ushort DetailCode = 0;
            if (SimulMode)
            {
                return true;
            }
            //시스템 알람 획득
            int ansSystem = ServoDrive.sGetAlarm(out AlarmCode, out DetailCode);
            if (ansSystem != 0)
            {
                LogManager.WriteServoLog(eLogLevel.Warn, String.Format("sGetAlarm failure. axnum={0}, sscGetLastError=0x{1:X8}", AxisNumber, SscApi.sscGetLastError()));
                SystemAlarmCode = "Get Fail";
            }
            else //응답 정상일경우 알람 코드 확인.
            {
                if (AlarmCode != 0)
                {
                    SystemAlarmCode = String.Format("{0:X4} - D:{1:X4} ", AlarmCode, DetailCode);
                }
            }
            //서보 알람 획득
            int ansServo = ServoDrive.sGetServoAlarm(AxisNumber, out AlarmCode, out DetailCode);
            if (ansServo != 0)
            {
                LogManager.WriteServoLog(eLogLevel.Warn, String.Format("sGetServoAlarm failure. axnum={0}, sscGetLastError=0x{1:X8}", AxisNumber, SscApi.sscGetLastError()));
                ServoAlarmCode = "Get Fail";
            }
            else //응답 정상일경우 알람 코드 확인.
            {
                if (AlarmCode != 0)
                {
                    ServoAlarmCode = String.Format("{0:X4} - D:{1:X4} ", AlarmCode, DetailCode);
                }
            }
            //운영 알람 획득
            int ansOper = ServoDrive.sGetOperAlarm(AxisNumber, out AlarmCode, out DetailCode);
            if (ansOper != 0)
            {
                LogManager.WriteServoLog(eLogLevel.Warn, String.Format("sGetOperAlarm failure. axnum={0}, sscGetLastError=0x{1:X8}", AxisNumber, SscApi.sscGetLastError()));
                OperationAlarmCode = "Get Fail";
            }
            else //응답 정상일경우 알람 코드 확인.
            {
                if (AlarmCode != 0)
                {
                    OperationAlarmCode = String.Format("{0:X4} - D:{1:X4} ", AlarmCode, DetailCode);
                }
            }
            return true;
        }
        public int GetCurrentServoPosition()
        {
            int ans;
            int Position;
            if (SimulMode)
            {
                return Simul_CurrentPosition;
            }
            ans = ServoDrive.sCurrenPostionOn(AxisNumber, out Position); //실제 축값을 구한다.
            if (ans != SscApi.SSC_OK)
            {
                LogManager.WriteServoLog(eLogLevel.Error, "sCurrenPostionOn failure. axnum={0}, sscGetLastError=0x{1:X8}", AxisNumber, SscApi.sscGetLastError());
                return CurrentPosFailValue;
            }
            return Position;
        }

        public int GetCommandServoPosition()
        {
            int ans;
            int Position;
            if (SimulMode)
            {
                return Simul_CommandPosition;
            }
            ans = ServoDrive.sCmdPostion(AxisNumber, out Position); //실제 축값을 구한다.
            if (ans != SscApi.SSC_OK)
            {
                LogManager.WriteServoLog(eLogLevel.Error, "sCurrenPostionOn failure. axnum={0}, sscGetLastError=0x{1:X8}", AxisNumber, SscApi.sscGetLastError());
                return CurrentPosFailValue;
            }
            return Position;
        }

        public bool[] GetAxisStatus()
        {
            if (SimulMode)
            {
                bool[] Temp = new bool[8];
                Temp[0] = true;
                Temp[1] = true;
                Temp[5] = true;
                return Temp;
            }
            else
            {
                return ServoDrive.sGetAxisStatus(AxisNumber);
            }
            //RDY Servo Ready
            //INP 인포지션
            //TLC 토크제한중
            //SALM 서보알람중
            //SWRN 서보경고중
            //OP 운전중
            //ZP 원점복귀 완료
            //OALM 운전 알람중
        }

        public void GetSensorState(out bool MinusLimit, out bool Home, out bool PlusLimit)
        {
            try
            {
                if (SimulMode)
                {
                    PlusLimit = false;
                    MinusLimit = false;
                    Home = true;
                    return;
                }
                int SensorValue = ServoDrive.DogSensorMotion_Check(AxisNumber);
                PlusLimit = ((SensorValue >> 0 & 0x01) == 1);
                MinusLimit = ((SensorValue >> 1 & 0x01) == 1);
                Home = ((SensorValue >> 2 & 0x01) == 1);
            }
            catch (Exception ex)
            {
                PlusLimit = false;
                MinusLimit = false;
                Home = false;
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
            }
        }

        public bool CheckServoOn()
        {
            if (SimulMode)
            {
                return true;
            }
            short servoOn;
            int ans = ServoDrive.sCheckServoOn(AxisNumber, out servoOn);
            return (ans == SscApi.SSC_OK) && (servoOn == 1);
        }

        public bool CheckPositionMoveDone(int PositionOffset)
        {
            if (SimulMode)
                return true;
            int Position = 0;
            int ans = 0;
            int fin_status = 0;

            bool bNotMoveState = false; ;
            bool bInPosition = false;
            ans = ServoDrive.sCheckDrivefinish(ref ans, AxisNumber, out fin_status); //축 상태 획득.
            if (ans == SscApi.SSC_OK)
            {
                if (fin_status == SscApi.SSC_FIN_STS_RDY || fin_status == SscApi.SSC_FIN_STS_STP)
                    bNotMoveState = true;
            }
            else
            {
                LogManager.WriteServoLog(eLogLevel.Error, "sCheckDrivefinish failure. axnum={0}, sscGetLastError=0x{1:X8}", AxisNumber, SscApi.sscGetLastError());
                return false;
            }
            ans = ServoDrive.sCurrenPostionOn(AxisNumber, out Position); //실제 축값을 구한다.
            if (ans != SscApi.SSC_OK)
            {
                LogManager.WriteServoLog(eLogLevel.Error, "sCurrenPostionOn failure. axnum={0}, sscGetLastError=0x{1:X8}", AxisNumber, SscApi.sscGetLastError());
                return false;

            }
            if (getDiffABS(PositionOffset, Position) <= cServoCheckMargin)
                bInPosition = true;

            return bInPosition && bNotMoveState;
        }


        /// <summary>
        /// 포지션 동작 실행만 시킨다. 결과 확인은  CheckPositionMoveDone에서 따로 확인
        /// //Servo 동작인자에 가감속 적용할수 있도록 추가.0 넣으면 디폴트로드값 적용.
        /// </summary>
        /// <param name="?"></param>
        /// <param name="?"></param>
        /// <returns></returns>
        public bool AxisPositionMove(int MovePosition, int moveSpeed, ushort acctime = 0, ushort dcctime = 0)
        {
            if (this.LinearAxis && !CheckHomeReferenced()) //리니어 축인데 홈 무효화되어있으면 에러
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "AxisPositionMoveAndWait Failed! Axis : {0} Reason : Linear Axis Home Invalid!", AxisNumber);
                return false;
            }
            if (MinusSoftLimit > MovePosition || PlusSoftLimit < MovePosition)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "AxisPositionMoveAndWait Failed! Axis : {0} Reason : Target MovePosition Limit Over!", AxisNumber);
                return false;
            }
            if (0 > moveSpeed || MaxSpeed < moveSpeed)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "AxisPositionMoveAndWait Failed! Axis : {0} Reason : Target moveSpeed Limit Over!", AxisNumber);
                return false;
            }
            if (SimulMode)
            {
                Simul_CurrentPosition = MovePosition;
                return true;
            }
            //동작전 알람 확인.
            bool AlarmExist = CheckAxisAlarmExist();
            if (AlarmExist)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "AxisPositionMove Failed CheckAlarm! Axis : {0}", AxisNumber);
                return false;
            }
            bool Result = ServoDrive.sAxisPointMove(AxisNumber, MovePosition, moveSpeed, acctime, dcctime);
            if (!Result)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "AxisPositionMove Failed! Axis : {0}", AxisNumber);
            }
            return Result;
        }
        /// <summary>
        /// 포지션 동작 실행 시킨다. 완료까지 실행을 블락한다.
        /// //Servo 동작인자에 가감속 적용할수 있도록 추가.0 넣으면 디폴트로드값 적용.
        /// </summary>
        /// <param name="MovePosition"></param>
        /// <param name="moveSpeed"></param>
        /// <returns></returns>
        public bool AxisPositionMoveAndWait(int MovePosition, int moveSpeed, ushort acctime = 0, ushort dcctime = 0)
        {
            if (this.LinearAxis && !CheckHomeReferenced()) //리니어 축인데 홈 무효화되어있으면 에러
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "AxisPositionMoveAndWait Failed! Axis : {0} Reason : Linear Axis Home Invalid!", AxisNumber);
                return false;
            }
            if (MinusSoftLimit > MovePosition || PlusSoftLimit < MovePosition)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "AxisPositionMoveAndWait Failed! Axis : {0} Reason : Target MovePosition Limit Over!", AxisNumber);
                return false;
            }
            if (0 > moveSpeed || MaxSpeed < moveSpeed)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "AxisPositionMoveAndWait Failed! Axis : {0} Reason : Target moveSpeed Limit Over!", AxisNumber);
                return false;
            }
            if (SimulMode)
            {
                Simul_CurrentPosition = MovePosition;
                return true;
            }
            //동작전 알람 확인.
            bool AlarmExist = CheckAxisAlarmExist();
            if (AlarmExist)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "AxisPositionMoveAndWait Failed CheckAlarm! Axis : {0}", AxisNumber);
                return false;
            }
            bool PosMoveResult;
            bool Result = ServoDrive.sAxisPointMove(AxisNumber, MovePosition, moveSpeed, acctime, dcctime);
            if (!Result) //동작 실패하면 바로 리턴
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "AxisPositionMoveAndWait Failed! Axis : {0}", AxisNumber);
                return Result;
            }
            //동작 완료를 기다린다.
            DateTime dt = DateTime.Now;
            while (!IsTimeOut(dt, PositionMoveTimeout))
            {
                PosMoveResult = CheckPositionMoveDone(MovePosition);
                if (PosMoveResult)
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "AxisPositionMoveAndWait Axis : {0} Move Done!", AxisNumber);
                    return true;
                }
                Thread.Sleep(ThreadCycle);
            }
            LogManager.WriteConsoleLog(eLogLevel.Info, "AxisPositionMoveAndWait Failed! Axis : {0} Timeover", AxisNumber);
            return false;
        }

        public bool AxisJogMove(int TargetPulse, int moveSpeed, bool NoWait = true)
        {
            if (SimulMode)
            {
                Simul_CurrentPosition += TargetPulse;
                return true;
            }
            //현재 서보 포지션 값을 구한다.
            int targetPostion = 0;
            int currentPosition = GetCurrentServoPosition();
            if (currentPosition == CurrentPosFailValue)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "AxisJogMove Fail:GetCurrentServoPosition Failed.");
                return false;
            }
            targetPostion = currentPosition + TargetPulse;
            bool Result;
            if (NoWait)
            {
                Result = AxisPositionMove(targetPostion, moveSpeed);
            }
            else
            {
                Result = AxisPositionMoveAndWait(targetPostion, moveSpeed);
            }

            return Result;

        }
        public bool AxisHomeMove()
        {
            if (SimulMode)
            {
                Simul_CurrentPosition = 0;
                return true;
            }
            //동작전 알람 확인.
            bool AlarmExist = CheckAxisAlarmExist();
            if (AlarmExist)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "AxisHomeMove Failed CheckAlarm! Axis : {0}", AxisNumber);
                return false;
            }
            bool HomeStartResult = ServoDrive.sAxisHomeMove(AxisNumber);
            if (!HomeStartResult)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "AxisHomeMove Failed! Axis : {0}", AxisNumber);
            }
            HomeReferenced = HomeStartResult;
            return HomeStartResult;

        }

        public bool AxisHomeMoveAndWait()
        {
            if (SimulMode)
            {
                Simul_CurrentPosition = 0;
                return true;
            }
            //동작전 알람 확인.
            bool AlarmExist = CheckAxisAlarmExist();
            if (AlarmExist)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "AxisHomeMoveAndWait Failed CheckAlarm! Axis : {0}", AxisNumber);
                return false;
            }
            bool PosMoveResult;
            bool HomeStartResult = ServoDrive.sAxisHomeMove(AxisNumber);
            if (!HomeStartResult)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "AxisHomeMoveAndWait Failed! Axis : {0}", AxisNumber);
                return false;
            }
            //동작 완료를 기다린다.
            DateTime dt = DateTime.Now;
            while (!IsTimeOut(dt, PositionMoveTimeout))
            {
                PosMoveResult = CheckPositionMoveDone(0);
                if (PosMoveResult)
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "AxisHomeMoveAndWait Axis : {0} Move Done!", AxisNumber);
                    return true;
                }
                Thread.Sleep(ThreadCycle);
            }
            LogManager.WriteConsoleLog(eLogLevel.Info, "AxisHomeMoveAndWait Failed! Axis : {0} Timeover", AxisNumber);
            return false;

        }

        /// <summary>
        /// 두정수의 차를 구한후 절대값을 취한다.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        private int getDiffABS(int a, int b)
        {
            int c = a - b;
            if (c >= 0)
                return c;
            else
                return -c;
        }

        private bool IsTimeOut(DateTime dtstart, double secTimeout)
        {
            secTimeout = secTimeout * 1000;

            TimeSpan TLimite = TimeSpan.FromMilliseconds(secTimeout);
            TimeSpan tspan = DateTime.Now.Subtract(dtstart);
            return (tspan > TLimite) ? true : false;
        }
    }

}
