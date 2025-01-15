using BoxPrint.Log;
using System;
using System.Threading;

namespace BoxPrint.SSCNet
{
    //Delta 서보제어 검토중
    public class ServoAxis_DeltaControl : ServoAxis_IOControl
    {

        public ServoAxis_DeltaControl(string ParantName, bool Simulmode)
            : base(ParantName, Simulmode)
        {

        }
        public ServoAxis_DeltaControl(string ParantName, bool StackerUse, bool Simulmode)
            : base(ParantName, StackerUse, Simulmode)
        {

        }
        public override void DoServoForceStop()
        {
            SV_OUT_FORCE_STOP = false;
            SV_OUT_SELECT_POINT1 = false; //기존 포인트 번호가 남아있을수도 있으니 모두 OFF
            SV_OUT_SELECT_POINT2 = false;
            SV_OUT_SELECT_POINT3 = false;
            SV_OUT_SELECT_POINT4 = false;
            return;
        }
        public override bool DoServoResetAction()
        {
            SV_OUT_FORCE_STOP = true;
            SV_OUT_SERVO_ON = true;
            Thread.Sleep(50);
            SV_OUT_SELECT_POINT1 = false; //기존 포인트 번호가 남아있을수도 있으니 모두 OFF
            SV_OUT_SELECT_POINT2 = false;
            SV_OUT_SELECT_POINT3 = false;
            SV_OUT_SELECT_POINT4 = false;
            Thread.Sleep(50);
            bool bReady = SV_IN_READY;
            bool bAlarm = SV_IN_ALARM;
            bool bMend = SV_IN_MOVE_END;

            LogManager.WriteConsoleLog(eLogLevel.Info, "DoServoResetAction Result [ Mend : {0}  Ready : {1}  Alarm : {2}]", bMend, bReady, bAlarm);

            if (bReady && bMend && !bAlarm)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public override bool DoServoPointHomeAction()
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


            SV_OUT_FORCE_STOP = true;
            SV_OUT_SERVO_ON = true;


            bool bMend = SV_IN_MOVE_END;
            bool bAlarm = SV_IN_ALARM;
            if (bMend && !bAlarm)
            {
                Thread.Sleep(20);
                //시작 비트 On/Off 로 가동
                SV_OUT_FWD_RUN = true;
                Thread.Sleep(50);
                SV_OUT_FWD_RUN = false;
                Thread.Sleep(50);
                //동작 완료를 기다린다.
                bool ZP;
                //bool IPO;
                DateTime dt = DateTime.Now;
                while (!IsTimeOut(dt, PositionMoveTimeout))
                {
                    ZP = SV_IN_ZP_COMPLETE;
                    bMend = SV_IN_MOVE_END;
                    bAlarm = SV_IN_ALARM; //알람 발생
                    if (bAlarm) //알람 비트 On 되면 에러발생
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "DoServoPointHomeAction Move Alarm Occurred!");
                        return false;
                    }
                    if (ZP && bMend)
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
        public override bool DoServoPointTableAction(byte pointNumber)
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


            bool bMend = SV_IN_MOVE_END;
            bool bAlarm = SV_IN_ALARM;
            bool bReady = SV_IN_READY;

            if (bReady && !bAlarm && bMend)  //동작전 상태 체크
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

                DateTime dt = DateTime.Now;
                while (!IsTimeOut(dt, PositionMoveTimeout))
                {
                    if (IsStackerServo && CheckStackOverError()) //스택커 적재 에러 높이 도달  체크
                    {
                        SV_OUT_FORCE_STOP = false; //에러 높이 도달하면 정지
                        LogManager.WriteConsoleLog(eLogLevel.Info, "DoServoPointTableAction Stacker Over Alarm Occurred!");
                        return false;
                    }
                    bMend = SV_IN_MOVE_END;
                    bAlarm = SV_IN_ALARM; //알람 발생
                    if (bAlarm) //알람 비트 On 되면 에러발생
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "DoServoPointTableAction Move Alarm Occurred!");
                        return false;
                    }
                    if (bMend && !bAlarm)
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
                LogManager.WriteConsoleLog(eLogLevel.Info, "DoServoResetAction Start Condition Faield! Result [ Mend : {0}  Ready : {1}  Alarm : {2}]", bMend, bReady, bAlarm);
                return false;
            }

        }
    }
}
