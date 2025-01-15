using Stockerfirmware.Log;
using System;
using System.Text;
using System.Xml.Linq;

namespace WCF_LBS.DataParameter
{
    public class Parameter_PORT : ParameterItem
    {
        public string UnitID = "PORT1";
        public string Auto = "0";              //0:MANUAL   1:AUTO
        public string Type = "1";              //1:INPUT     2:OUTPUT
        public string Request = "0";           //0:NONE    1:L-REQ U-LEQ
        public string CarrierContain = "0";    //0:EMPTY    1:CONTAIN
        public string TagID = "ERROR";         //HEX VALUE (112*2)
        public string CarrierSize = "0";       //0:DEFAULT TRAY LBS:0~4  SMALLBOX LBS : REEL-1,TRAY-2,OVERSIZE-4 SENSOR ERROR-4
        public string PortReady = "1";         //0: Not Ready 1:Ready
        public string RobotComplete = "0";      //0:Default
        public string ErrorStatus = "0";       //0:NORMAL   1:ERROR
        public string ErrorCode = "0";     //5DIGIT CODE  //211126 RGJ 포트 보고 00000 으로 올라가는 문제로 수정.
        public Parameter_PORT(string unitID)
        {
            UnitID = unitID;
            ReadData();
        }
        //실제 I/O를 획득
        public override bool ReadData()
        {
            //실제 데이터를 펌웨어에서 얻어온다. 미구현
            return true;
        }
        public override XElement XMLRendering()
        {
            XElement port = new XElement("PORT");
            XElement unit = new XElement("UNIT");
            unit.Add(new XElement("UNITID", UnitID));
            unit.Add(new XElement("AUTO", Auto));
            unit.Add(new XElement("TYPE", Type));
            unit.Add(new XElement("REQUEST", Request));
            unit.Add(new XElement("CARRIERCONTAIN", CarrierContain));
            unit.Add(new XElement("TAGID", GetAsciiStringtoHexString(TagID)));
            unit.Add(new XElement("CARRIERSIZE", CarrierSize));
            unit.Add(new XElement("PORTREADY", PortReady));
            unit.Add(new XElement("ROBOTCOMPLETE", RobotComplete));
            unit.Add(new XElement("ERRORSTATUS", ErrorStatus));
            unit.Add(new XElement("ERRORCODE", ErrorCode));
            port.Add(unit);
            return port;
        }
        public string GetAsciiStringtoHexString(string Asciistring)
        {
            try
            {
                if (Asciistring == null)
                {
                    return "";
                }
                StringBuilder sb = new StringBuilder();
                foreach (var c in Asciistring)
                {
                    sb.AppendFormat("{0:X2}", (int)c);
                }
                return sb.ToString();
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(Stockerfirmware.eLogLevel.Info, ex.ToString());
                return "ERROR";
            }
        }

    }
}
