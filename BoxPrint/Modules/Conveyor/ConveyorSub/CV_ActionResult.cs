namespace BoxPrint.Modules.Conveyor
{
    public class CV_ActionResult
    {
        string actionModuleID;
        public eCV_ActionResult actionResult;
        public string message;
        public CV_ActionResult(string ModuleID, eCV_ActionResult result, string msg)
        {
            actionModuleID = ModuleID;
            actionResult = result;
            message = msg;
        }
    }
}
