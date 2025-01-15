using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using WCF_LBS.DataParameter;

namespace WCF_LBS
{
    /// <summary>
    /// LBS.LCS 로 보내는 메시지 최상위 ROOT
    /// </summary>
    public class LCSMessageItem : IXlementRender
    {
        public enumMessageType MsgType;
        public enumMessageName MsgName;
        public string Uid;
        public string Timestamp;
        public string ReturnCode;
        public List<ParameterItem> Parameters;

        public LCSMessageItem()
        {
            Parameters = new List<ParameterItem>();
        }
        public ParameterItem this[string ParaName]
        {
            get
            {
                ParameterItem para = Parameters.FirstOrDefault(p => p.ParameterName == ParaName);
                if (para != null)
                {
                    return para;
                }
                else
                {
                    return new ParameterItem();
                }
            }
        }
        public XElement XMLRendering()
        {
            XElement docu = new XElement("MESSAGE", new XAttribute("Type", MsgType.ToString()));

            docu.Add(new XElement("NAME", MsgType == enumMessageType.Reply ? MsgName.ToString()+"_REPLY" : MsgName.ToString()));
            docu.Add(new XElement("UID", Uid));
            docu.Add(new XElement("TIMESTAMP", Timestamp));
            if (ReturnCode != "NO REPORT")
            {
                docu.Add(new XElement("RETURNCODE", ReturnCode));
            }
            XElement para = new XElement("PARAMETERS", "");
            docu.Add(para);

            foreach(ParameterItem p in Parameters)
            {
                para.Add(p.XMLRendering());
            }
            

            return docu;
        }
    }

  
}
