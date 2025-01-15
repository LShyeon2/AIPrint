//220525 HHJ SCS 개선     //- PLC MxVer5 Update
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace PLCProtocol.DataClass
{
    public class PLCDataItem
    {
        [XmlAttribute("ModuleType")]
        public string ModuleType { get; set; }
        /// <summary>
        /// PLC 번호. PLC 2개 이상인 경우 필요.
        /// </summary>
        [XmlAttribute("PLCNum")]
        public short PLCNum { get; set; }
        /// <summary>
        /// PCtoPLC, PLCtoPC 
        /// </summary>
        [XmlAttribute("Area")]
        public eAreaType Area { get; set; }
        /// <summary>
        /// 데이터의 타입. (문자열, 숫자형, 비트형)
        /// </summary>
        [XmlAttribute("DataType")]
        public eDataType DataType { get; set; }
        /// <summary>
        /// 데이터의 타입. (문자열, 숫자형, 비트형)
        /// </summary>
        [XmlAttribute("Device")]
        public eDevice DeviceType { get; set; }
        /// <summary>
        /// 아이템 이름
        /// </summary>
        [XmlAttribute("ItemName")]
        public string ItemName { get; set; }
        /// <summary>
        /// 어드레스 옵셋
        /// </summary>
        [XmlAttribute("AddressOffset")]
        public int AddressOffset { get; set; }
        /// <summary>
        /// Bit가 D어드레스인 경우는 1Word의 0~15까지에 각 비트의 옵셋
        /// </summary>
        [XmlAttribute("BitOffset")]
        public int BitOffset { get; set; }
        /// <summary>
        /// 데이터 사이즈
        /// </summary>
        [XmlAttribute("Size")]
        public int Size { get; set; }
        /// <summary>
        /// 혹시 숫자에 소숫점 찍어야하면 이걸 사용해야함.
        /// </summary>
        [XmlAttribute("DecimalPoint")]
        public int DecimalPoint { get; set; }
        /// <summary>
        /// 계산된 최종 PLC 어드레스 옵셋
        /// </summary>
        public int ItemPLCAddress { get; set; }

        public PLCDataItem DeepCopy()
        {
            //InnerItems는 우선 제외한다.
            return new PLCDataItem()
            {
                PLCNum = this.PLCNum,
                Area = this.Area,
                DataType = this.DataType,
                DeviceType = this.DeviceType,
                ItemName = this.ItemName,
                AddressOffset = this.AddressOffset,
                BitOffset = this.BitOffset,
                Size = this.Size,
                DecimalPoint = this.DecimalPoint,
                ModuleType = this.ModuleType        //221011 db를 위해
            };
        }
    }

    public class PLCDataXmlControl
    {
        public List<PLCDataItem> Deserialize(string fileName)
        {
            bool bSuccess = false;
            List<PLCDataItem> bindPlcItemList = new List<PLCDataItem>();

            try
            {
                XmlSerializer xmlSer = new XmlSerializer(typeof(List<PLCDataItem>));
                FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                List<PLCDataItem> PlcItemList = (List<PLCDataItem>)xmlSer.Deserialize(fs);
                fs.Close();
                foreach (PLCDataItem plcdata in PlcItemList)
                {
                    bindPlcItemList.Add(plcdata);
                }
                bSuccess = true;
            }
            catch (Exception)
            {

            }
            return bSuccess ? bindPlcItemList : null;
        }
    }
}
