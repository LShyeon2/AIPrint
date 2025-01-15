using PLCProtocol.DataClass;
using BoxPrint;
using BoxPrint.Config;       //20220728 조숭진 config 방식 변경
using BoxPrint.Log;

namespace PLCProtocol.Base
{
    public class ProtocolBase
    {
        public delegate void ProtocolStateChange(PLCStateData stateData);
        public event ProtocolStateChange OnProtocolStateChange;

        public virtual short PlcNum { get; }
        public bool _IsConnect { get; set; }
        public virtual bool IsConnect { get; set; }//SuHwan_20221116 : [simul]
        public string IP
        {
            get;
            protected set;
        }
        public int Port
        {
            get;
            protected set;
        }
        protected PLCStateData plcState { get; set; }

        public ProtocolBase(PLCElement element)
        {
            if (element != null)
            {
                PlcNum = element.Num;
            }

            if (plcState is null) plcState = new PLCStateData();
            plcState.ConnectInfo = "Base";
        }

        public int BackupProtocolNum
        {
            get;
            protected set;
        }
        protected ProtocolBase BackupProtocol;


        public virtual bool Connect() { return false; }
        public virtual bool Close() { return false; }
        public virtual bool Read(PLCDataItem pItem, out byte[] readValue, out byte ErrorCode) { readValue = null; ErrorCode = 0; return false; }
        public virtual bool Write(PLCDataItem pItem, object writeValue,out byte ErrorCode) { ErrorCode = 0; return false; }
        public virtual object ReadValueConvert(PLCDataItem pItem, byte[] readValue) { return false; }

        protected virtual void SetPlcStateData(ePLCStateDataState state)
        {
            ePLCStateDataState prevState = plcState.State;
            LogManager.WriteConsoleLog(BoxPrint.eLogLevel.Info, "Set PLC State {0} {1} {2} -> {3}",plcState.PLCName, plcState.ConnectInfo, plcState.State, state);
            plcState.StateChangeTime = System.DateTime.Now;
            plcState.State = state;

            OnProtocolStateChange?.Invoke(plcState);        //230911 HHJ PLC 상태 관련 추가
            
            if (prevState != state)
            {
                GlobalData.Current.DBManager.DbSetProcedurePLCInfo(plcState, PlcNum + 1);
            }
        }

        public void SetBackupProtocol(ProtocolBase BackupLine)
        {
            if (BackupLine != null)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "SetBackupProtocol Main:{0} Backup:{1}", this.plcState?.PLCName, BackupLine.plcState?.PLCName);
                BackupProtocol = BackupLine;
            }
            else
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "SetBackupProtocol args Null check");
            }
        }

        public virtual PLCStateData GetPlcStateData()
        {
            return plcState;
        }

        public ProtocolBase GetBackupProtocol()
        {
            return BackupProtocol;
        }
        public bool IsBackupProtocolExist()
        {
            return BackupProtocol != null;
        }
    }
}
