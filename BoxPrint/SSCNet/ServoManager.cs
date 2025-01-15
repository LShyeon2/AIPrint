using BoxPrint.DataList;
using BoxPrint.Log;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BoxPrint.SSCNet
{
    public class ServoManager : IDisposable
    {

        public readonly int MaxAxis;
        private SSCInterruptDrive ServoDrive;
        private List<ServoAxis> ServoAxisList;
        private int TurnSpeed = 30;
        private int MoveSkipLimit = 10;
        public readonly bool ServoSimulMode;

        public TurnTeachingItemList TurnTeachingList
        {
            get;
            private set;
        }

        public ServoAxis this[int axis]
        {
            get
            {
                return ServoAxisList[axis - 1];
            }
        }

        private static ServoManager servo_mgr;
        private ServoManager(bool SimulMode)
        {
            servo_mgr = this;
            ServoSimulMode = SimulMode;
            LoadTeachingFile();
            ServoDrive = new SSCInterruptDrive(GetServoConfigFile(), GetServoParaFile());
            int ServoAnswer = ServoDrive.sscMainStart();
            if (ServoAnswer < 0 && !ServoSimulMode) //초기화 실패
            {
                throw new Exception("서보 초기화에 실패하였습니다.");
            }
            MaxAxis = ServoDrive.exist_axis;
            ServoAxisList = new List<ServoAxis>();
            for (int i = 0; i < MaxAxis; i++)
            {
                ServoAxisList.Add(new ServoAxis("Axis" + i, i + 1, false, 150000, -5000, ServoSimulMode));
                int Answer = ServoAxisList[i].ServoOn();
            }
        }

        public bool RequestRebootServoSystem()
        {
            int answer = 0;
            answer = ServoDrive.sMReboot(0);
            return answer == 0;
        }
        public void LoadTeachingFile()
        {
            TurnTeachingList = TurnTeachingItemList.Deserialize(GetServoTeachingFile());
        }
        public List<ServoAxis> GetServoList()
        {
            return ServoAxisList;
        }
        public void UpdateServoPosition()
        {
            foreach (var sItem in ServoAxisList)
            {
                sItem.UpdateServoAxis();
            }
        }

        public static ServoManager GetManagerInstance()
        {
            if (servo_mgr == null)
            {
                servo_mgr = new ServoManager(GlobalData.Current.GlobalSimulMode);
            }
            return servo_mgr;
        }
        public SSCInterruptDrive GetServoDrive()
        {
            return ServoDrive;
        }

        private bool RequesetServoAction(int axis, string tagName)
        {
            try
            {
                ServoAxis TargetAxis = ServoAxisList[axis - 1];
                int PositionValue = 0;

                if (TargetAxis == null)
                {
                    return false;
                }
                if (axis < 1 || axis > MaxAxis)
                {
                    return false;
                }

                var TList = TurnTeachingList.Where(t => t.TagName == tagName.ToUpper() && t.Axis == axis);
                if (TList.Count() == 1) //중복은 허용 안함.
                {
                    PositionValue = TList.First().PositionValue;
                }
                else
                {
                    LogManager.WriteConsoleLog(eLogLevel.Error, "RequesetServoAction Axis : {0} tagName:{1} 티칭 리스트가 없거나 중복 되었습니다.", axis, tagName);
                    return false;
                }
                //동작전 이미 서보가 목표값과 리미트 이내면 동작할 필요 없음.
                if (getDiffABS(PositionValue, TargetAxis.CurrentPosition) < MoveSkipLimit)
                {
                    LogManager.WriteConsoleLog(eLogLevel.Error, "Servo Move Skip : Already InPostion!");
                    return true;
                }
                return TargetAxis.AxisPositionMoveAndWait(PositionValue, TurnSpeed);
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
                return false;
            }
        }

        public bool RequesetTurn(int axis, eTurnCommand turnCommand)
        {
            bool Result = RequesetServoAction(axis, turnCommand.ToString());
            return Result;
        }

        public bool RequesetStackerUpDown(int axis, bool up)
        {
            string tag = up ? "UP" : "DOWN";
            bool Result = RequesetServoAction(axis, tag);
            return Result;
        }

        public bool CheckTurnAxisInPosition(int axis, eTurnCommand turnCommand)
        {
            ServoAxis TargetAxis = ServoAxisList[axis - 1];
            int PositionValue = 0;

            if (TargetAxis == null)
            {
                return false;
            }
            if (axis < 1 || axis > MaxAxis)
            {
                return false;
            }
            var TList = TurnTeachingList.Where(t => t.TagName == turnCommand.ToString().ToUpper() && t.Axis == axis);
            if (TList.Count() == 1) //중복은 허용 안함.
            {
                PositionValue = TList.First().PositionValue;
            }
            else
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, "RequesetTurn Axis : {0} tagName:{1} 티칭 리스트가 없거나 중복 되었습니다.", axis, turnCommand);
                return false;
            }
            bool InPos = TargetAxis.CheckPositionMoveDone(PositionValue);
            return InPos;
        }

        public void RequesetAllStop()
        {
            foreach (var sItem in ServoAxisList)
            {
                sItem.ServoStop();
            }
        }

        public string GetServoConfigFile()
        {
            string sReturn;
            string paths = System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            int indexnum = 0;
            indexnum = paths.IndexOf("\\bin");
            paths = paths.Remove(indexnum);
            sReturn = Path.Combine(paths, "Data\\Servo", "ServoConfig.ini");

            return sReturn;
        }
        public string GetServoParaFile()
        {
            string sReturn;

            /*
            int nLength = 10; // \\bin\\Debug 빼기 위해서 고정함

            //추출할 갯수가 문자열 길이보다 긴지?
            if (nLength > sString.Length)
            {
                //길다!
                //길다면 원본의 길이만큼 리턴해 준다.
                nLength = sString.Length;
            }
            //문자열 추출
            sReturn = sString.Substring(0, sString.Length - nLength);
            sReturn = sReturn + "\\Data\\ServoPA.ini";
            */

            string paths = System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            int indexnum = 0;
            indexnum = paths.IndexOf("\\bin");
            paths = paths.Remove(indexnum);
            sReturn = Path.Combine(paths, "Data\\Servo", "ServoPA.ini");

            return sReturn;
        }

        public string GetServoTeachingFile()
        {
            string sReturn;
            string FileName = string.Format("ServoTeachingData.xml");
            /*
            int nLength = 10; // \\bin\\Debug 빼기 위해서 고정함

            //추출할 갯수가 문자열 길이보다 긴지?
            if (nLength > sString.Length)
            {
                //길다!
                //길다면 원본의 길이만큼 리턴해 준다.
                nLength = sString.Length;
            }
            //문자열 추출
            sReturn = sString.Substring(0, sString.Length - nLength);
            sReturn = sReturn + "\\Data\\ServoPA.ini";
            */

            string paths = System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            int indexnum = 0;
            indexnum = paths.IndexOf("\\bin");
            paths = paths.Remove(indexnum);
            sReturn = Path.Combine(paths, "Data\\Servo", FileName);

            return sReturn;
        }

        private int getDiffABS(int a, int b)
        {
            int c = a - b;
            if (c >= 0)
                return c;
            else
                return -c;
        }

        #region IDisposable Support
        private bool disposedValue = false; // 중복 호출을 검색하려면

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 관리되는 상태(관리되는 개체)를 삭제합니다.
                }
                if (ServoSimulMode) //시뮬 모드에서는 정리 불필요
                {
                    return;
                }
                RequesetAllStop();

                foreach (var sItem in ServoAxisList)
                {
                    sItem.ServoOff();
                }

                ServoDrive.CloseSscnet();

                disposedValue = true;
            }
        }

        // TODO: 위의 Dispose(bool disposing)에 관리되지 않는 리소스를 해제하는 코드가 포함되어 있는 경우에만 종료자를 재정의합니다.
        ~ServoManager()
        {
            // 이 코드를 변경하지 마세요. 위의 Dispose(bool disposing)에 정리 코드를 입력하세요.
            Dispose(false);
        }

        // 삭제 가능한 패턴을 올바르게 구현하기 위해 추가된 코드입니다.
        void IDisposable.Dispose()
        {
            // 이 코드를 변경하지 마세요. 위의 Dispose(bool disposing)에 정리 코드를 입력하세요.
            Dispose(true);
            // TODO: 위의 종료자가 재정의된 경우 다음 코드 줄의 주석 처리를 제거합니다.
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
