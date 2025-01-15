using BoxPrint;
using BoxPrint.Modules;
using BoxPrint.Modules.RM;

namespace WCF_LBS.Commands
{
    public class CraneCommand
    {
        private static object CommandNumberSync = new object();
        private short NextCommandNumber = 0;
        private short GetNextCommandNumber()
        {
            lock (CommandNumberSync)
            {
                NextCommandNumber++;
                if (NextCommandNumber == 10000)
                {
                    NextCommandNumber = 1;
                }
                return NextCommandNumber;
            }
        }
        //220318 HHJ SCS 개발	//- ActiveJob 연동 RouteLine 추가
        //public enumCraneCommand Command = enumCraneCommand.None;
        //public enumCraneTarget TargetType = enumCraneTarget.None;
        //public int TargetBank = 0;
        //public int TargetBay = 0;
        //public int TargetLevel = 0;
        //public string TargetTagID = "";
        //public string TargetCarrierID = "";

        public string CommandID { get; set; } //커맨드 아이디 추가.
        public short CommandNumber
        {
            get;
            set;
        }

        public string CraneID { get; set; } //명령 대상 RMID 추가.
        public eCraneCommand Command { get; set; }
        public enumCraneTarget TargetType { get; set; }
        private int _TargetBank = -1;
        public int TargetBank
        {
            get
            {
                if (TargetItem != null)
                {
                    return TargetItem.iBank;
                }
                else
                {
                    return _TargetBank;
                }
            }
            set
            {
                _TargetBank = value;
            }
        }
        private int _TargetBay = -1;
        public int TargetBay
        {
            get
            {
                if (TargetItem != null)
                {
                    return TargetItem.iBay;
                }
                else
                {
                    return _TargetBay;
                }
            }
            set
            {
                _TargetBay = value;
            }
        }
        private int _TargeLevel = -1;
        public int TargetLevel
        {
            get
            {
                if (TargetItem != null)
                {
                    return TargetItem.iLevel;
                }
                else
                {
                    return _TargeLevel;
                }
            }
            set
            {
                _TargeLevel = value;
            }
        }
        public string TargetTagID
        {
            get
            {
                if (TargetItem != null)
                {
                    return TargetItem.iLocName;
                }
                else
                {
                    return string.Empty;
                }
            }
        }
        public string TargetCarrierID { get; set; }

        public ICarrierStoreAble TargetItem;

        //220329 HHJ SCS 개발     //- 커맨드 디폴트 생성추가
        public CraneCommand()
        {
            CommandID = string.Empty;
            CommandNumber = GetNextCommandNumber();
            CraneID = string.Empty;
            Command = eCraneCommand.NONE;
            TargetType = enumCraneTarget.None;
            TargetCarrierID = "";
        }
        public CraneCommand(string CMD_ID, string RM_ID, eCraneCommand Cmd, enumCraneTarget craneTarget, ICarrierStoreAble ActionTarget, string CarrierID)
        {
            CommandID = CMD_ID;
            CommandNumber = GetNextCommandNumber();
            CraneID = RM_ID;
            Command = Cmd;
            TargetType = craneTarget;
            TargetItem = ActionTarget;
            TargetCarrierID = CarrierID;
        }

        public RMModuleBase CommandCrane
        {
            get
            {
                if (!string.IsNullOrEmpty(CraneID))
                {
                    return GlobalData.Current.mRMManager[CraneID];
                }
                else
                {
                    return null;
                }
            }
        }
    }
}
