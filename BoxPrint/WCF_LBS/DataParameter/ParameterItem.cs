using System.Xml.Linq;

namespace WCF_LBS.DataParameter
{
    public class ParameterItem : IXlementRender
    {
        public readonly string ParameterName = "";
        public readonly string ParameterValue = "";

        public ParameterItem()
        {

        }

        public ParameterItem(string name, string value)
        {
            ParameterName = name;
            ParameterValue = value;
        }
        public virtual XElement XMLRendering()
        {
            XElement x = new XElement(ParameterName, ParameterValue);
            return x;
        }
        public virtual bool ReadData()
        {
            //Do Nothing...
            return true;
        }
    }
}

