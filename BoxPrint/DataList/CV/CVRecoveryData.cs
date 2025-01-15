namespace Stockerfirmware.DataList.CV
{
    public class CVRecoveryData
    {
        public string _ModuleName;
        public string _LineName;
        public eCV_Speed _RunState;
        public eCV_StopperState _StopperState;
        public eCV_TurnState _TurnState;
        public eCV_DoorState _DoorState;
        public int _ModuleStep;
        public bool _TrayExist;
        public string _TagID;
        public eTrayHeight _TrayHeight;
        public string _InternalSignals;
    }
}
