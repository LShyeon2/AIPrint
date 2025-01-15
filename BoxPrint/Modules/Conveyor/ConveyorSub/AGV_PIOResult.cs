namespace BoxPrint.Modules.Conveyor
{
    public class AGV_PIOResult
    {
        bool IsLoadPIO;
        string PIOModuleID;
        public eAGV_PIOResult PIOResult;
        public string message;
        public AGV_PIOResult(bool LoadPIO, string ModuleID, eAGV_PIOResult result, string msg)
        {
            IsLoadPIO = LoadPIO;
            PIOModuleID = ModuleID;
            PIOResult = result;
            message = msg;
        }
    }
}
