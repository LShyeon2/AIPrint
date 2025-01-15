namespace BoxPrint.Modules.Conveyor
{
    public class CV_ActionPara
    {
        public eCVCommandType CVCommandType = eCVCommandType.None;
        public eCV_Speed Speed = eCV_Speed.Low;
        public CV_ActionPara(eCVCommandType commandType, eCV_Speed spd)
        {
            CVCommandType = commandType;
            Speed = spd;
        }
    }
}
