using System.Xml.Linq;

namespace WCF_LBS.DataParameter
{
    public class Parameter_BOOTH : ParameterItem
    {
        public string UnitID = "BOOTH";
        public string Auto = "0";      //0:MANUAL   1:AUTO
        public string AutoStart = "0";      //0:STOP     1:START
        public string DoorKeyUnlock = "0";      //0:LOCK     1:UNLOCK
        public string DoorOpen = "0";      //0:CLOSE    1:HP OPEN   2:OP OPEN   3:BOTH OPEN
        public string EmergencyStop = "0";      //0:NORMAL   1:EMG STOP
        public string LightCurtain = "0";      //0:ALL OFF  1:ON
        public string CarrierSize = "0";      //0~4 SIZE
        public string ErrorStatus = "0";      //0:NORMAL   1:ERROR
        public string ErrorCode = "0";      //5DIGIT CODE

        //실제 I/O를 획득
        public override bool ReadData()
        {
            return true;
        }
        public Parameter_BOOTH()
        {
            ///실제 데이터를 펌웨어에서 얻어온다. 미구현
            ReadData();
        }
        public override XElement XMLRendering()
        {
            XElement booth = new XElement("BOOTH");
            XElement unit = new XElement("UNIT");
            unit.Add(new XElement("UNITID", UnitID));
            unit.Add(new XElement("AUTO", Auto));
            unit.Add(new XElement("AUTOSTART", AutoStart));
            unit.Add(new XElement("DOORKEYUNLOCK", DoorKeyUnlock));
            unit.Add(new XElement("DOOROPEN", DoorOpen));
            unit.Add(new XElement("EMERGENCYSTOP", EmergencyStop));
            unit.Add(new XElement("LIGHTCURTAIN", LightCurtain));
            unit.Add(new XElement("CARRIERSIZE", CarrierSize));
            unit.Add(new XElement("ERRORSTATUS", ErrorStatus));
            unit.Add(new XElement("ERRORCODE", ErrorCode));
            booth.Add(unit);
            return booth;
        }

    }
}
