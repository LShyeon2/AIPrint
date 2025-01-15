using System.Xml.Linq;

namespace WCF_LBS.DataParameter
{
    public class Parameter_ROBOT : ParameterItem
    {
        public string UnitID = "NONE";
        public string Transferring = "0";      //0:Idle   1:Transferring
        public string Bank = "1";              //1:Left Bank     2:Right Bank
        public string Bay = "1";               //Bay : 1~Count
        public string Level = "1";             //Level : 1~Count
        public string CarrierContain = "0";    //0:EMPTY    1:CONTAIN
        public string Home = "0";              //0:Not Home    1:Home
        public string EmergencyStop = "0";     //0:NORMAL   1:EMG STOP
        public string Busy = "0";              //0:Idle    1:Busy
        public string ArmStretch = "0";        //0:Folding 1:Streched
        public string ErrorStatus = "0";       //0:NORMAL   1:ERROR
        public string ErrorCode = "0";         //5DIGIT CODE
        public string AutoTeaching = "0";      //0:None    1:AutoTeaching
        public string Chuck = "0";             //0:None    1:Chuck     2:Unchuck
        public Parameter_ROBOT(string robotID)
        {
            UnitID = robotID;
            ReadData();
        }
        public override bool ReadData()
        {
            //실제 데이터를 펌웨어에서 얻어온다. 미구현
            return true;
        }
        public override XElement XMLRendering()
        {
            XElement robot = new XElement("ROBOT");
            XElement unit = new XElement("UNIT");
            unit.Add(new XElement("UNITID", UnitID));
            unit.Add(new XElement("TRANSFERING", Transferring));//원래 TRANSFERRING 이 맞으나 기존 LBS.LCS 프로그램이  TRANSFERING로 파싱 하므로 임시 변경.
            //unit.Add(new XElement("TRANSFERRING", Transferring));
            unit.Add(new XElement("BANK", Bank));
            unit.Add(new XElement("BAY", Bay));
            unit.Add(new XElement("LEVEL", Level));
            unit.Add(new XElement("CARRIERCONTAIN", CarrierContain));
            unit.Add(new XElement("HOME", Home));
            unit.Add(new XElement("EMERGENCY", EmergencyStop));
            unit.Add(new XElement("BUSY", Busy));
            unit.Add(new XElement("ARMSTRETCH", ArmStretch));
            unit.Add(new XElement("ERRORSTATUS", ErrorStatus));
            unit.Add(new XElement("ERRORCODE", ErrorCode));
            unit.Add(new XElement("AUTOTEACHING", AutoTeaching));
            unit.Add(new XElement("CHUCK", Chuck));
            robot.Add(unit);
            return robot;
        }
        //실제 I/O를 획득

    }
}
