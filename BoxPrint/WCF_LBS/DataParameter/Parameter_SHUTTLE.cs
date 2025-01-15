using System.Xml.Linq;

namespace WCF_LBS.DataParameter
{
    public class Parameter_SHUTTLE : ParameterItem
    {
        public string UnitID = "PORT1";
        public string Auto = "0";              //0:MANUAL   1:AUTO
        public string Type = "1";              //1:INPUT     2:OUTPUT
        public string Request = "0";           //0:NONE    1:L-REQ U-LEQ
        public string PortReady = "0";         //0: Not Ready 1:Ready
        public string CarrierContain = "0";    //0:EMPTY    1:CONTAIN
        public string TagID = "ERROR";         //HEX VALUE (112*2)
        public string CarrierSize = "0";       //0:DEFAULT 
        public string CarrierID = "0";         //HEX VALE DEFAULT :0
        public string ErrorStatus = "0";       //0:NORMAL   1:ERROR
        public string ErrorCode = "0";     //5DIGIT CODE //211126 RGJ 포트 보고 00000 으로 올라가는 문제로 수정.

        public string OHTValid = "0";          //0:DEFAULT
        public string OHTCS = "0,0,0,0";       //0,0,0,0 : DEFAULT
        public string OHTTReq = "0";           //0:DEFAULT
        public string OHTBusy = "0";           //0:DEFAULT
        public string OHTComplete = "0";       //0:DEFAULT
        public string EQLReq = "0";            //0:DEFAULT
        public string EQUReq = "0";            //0:DEFAULT
        public string EQAbort = "0";           //0:DEFAULT
        public string EQReady = "0";           //0:DEFAULT

        public Parameter_SHUTTLE(string unitID)
        {
            UnitID = unitID;
            ReadData();
        }
        public override bool ReadData()
        {
            //실제 데이터를 펌웨어에서 얻어온다. 미구현
            return true;
        }
        public override XElement XMLRendering()
        {
            XElement shuttle = new XElement("SHUTTLE");
            XElement unit = new XElement("UNIT");
            unit.Add(new XElement("UNITID", UnitID));
            unit.Add(new XElement("AUTO", Auto));
            unit.Add(new XElement("TYPE", Type));
            unit.Add(new XElement("REQUEST", Request));
            unit.Add(new XElement("PORTREADY", PortReady));
            unit.Add(new XElement("CARRIERCONTAIN", CarrierContain));
            unit.Add(new XElement("TAGID", TagID));
            unit.Add(new XElement("CARRIERSIZE", CarrierSize));
            unit.Add(new XElement("CARRIERID", CarrierID));
            unit.Add(new XElement("ERRORSTATUS", ErrorStatus));
            unit.Add(new XElement("ERRORCODE", ErrorCode));
            unit.Add(new XElement("OHTVALID", OHTValid));
            unit.Add(new XElement("OHTCS", OHTCS));
            unit.Add(new XElement("OHTTRQEQ", OHTTReq));
            unit.Add(new XElement("OHTBUSY", OHTBusy));
            unit.Add(new XElement("OHTCOMPLETE", OHTComplete));
            unit.Add(new XElement("EQLREQ", EQLReq));
            unit.Add(new XElement("EQUREQ", EQUReq));
            unit.Add(new XElement("EQLABORT", EQAbort));
            unit.Add(new XElement("EQREADY", EQReady));
            shuttle.Add(unit);
            return shuttle;
        }
        //실제 I/O를 획득

    }
}
