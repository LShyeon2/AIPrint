//220525 HHJ SCS 개선     //- PLC MxVer5 Update
using System.Configuration;

namespace PLCProtocol.DataClass
{
    //20220728 조숭진 config 방식 변경으로 PLCElement -> PLCConfigElement 변경.
    public class PLCElementCollection : ConfigurationElementCollection
    {
        private static ConfigurationPropertyCollection _properties;

        static PLCElementCollection()
        {
            _properties = new ConfigurationPropertyCollection();
        }

        protected override ConfigurationPropertyCollection Properties
        {
            get { return _properties; }
        }

        public override ConfigurationElementCollectionType CollectionType
        {
            get { return ConfigurationElementCollectionType.BasicMap; }
        }

        #region Indexers
        public PLCConfigElement this[int index]
        {
            get { return (PLCConfigElement)BaseGet(index); }
            set
            {
                if (Count > 0 && BaseGet(index) != null)
                {
                    BaseRemoveAt(index);
                }
                base.BaseAdd(index, value);
            }
        }

        public PLCConfigElement this[short num]
        {
            get { return (PLCConfigElement)BaseGet(num); }
        }
        #endregion

        #region Overrides
        protected override string ElementName
        {
            get
            {
                return "PLC";
            }
        }
        protected override ConfigurationElement CreateNewElement()
        {
            return new PLCConfigElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return (element as PLCConfigElement).Num;
        }
        #endregion
    }

    public class PLCConfigElement : ConfigurationElement
    {
        private ConfigurationProperty _Num = new ConfigurationProperty("Num", typeof(short));
        [ConfigurationProperty("Num", IsRequired = true, IsKey = true)]
        public short Num
        {
            get { return (short)base[_Num]; }
            set { base[_Num] = _Num; }
        }

        private ConfigurationProperty _BackupNum = new ConfigurationProperty("BackupNum", typeof(short));
        [ConfigurationProperty("BackupNum", IsRequired = true)]
        public short BackupNum
        {
            get { return (short)base[_BackupNum]; }
            set { base[_BackupNum] = _BackupNum; }
        }

        private ConfigurationProperty _PLCName = new ConfigurationProperty("PLCName", typeof(string));
        [ConfigurationProperty("PLCName", IsRequired = false, DefaultValue = "No Name")]
        public string PLCName
        {
            get { return (string)base[_PLCName]; }
            set { base[_PLCName] = _PLCName; }
        }

        private ConfigurationProperty _Series = new ConfigurationProperty("Series", typeof(ePLCSeries));
        [ConfigurationProperty("Series", IsRequired = false, DefaultValue = ePLCSeries.MxCom)]
        public ePLCSeries Series
        {
            get { return (ePLCSeries)base[_Series]; }
            set { base[_Series] = _Series; }
        }

        private ConfigurationProperty _ComType = new ConfigurationProperty("ComType", typeof(eCommunicationType));
        [ConfigurationProperty("ComType", IsRequired = true, DefaultValue = eCommunicationType.None)]
        public eCommunicationType ComType
        {
            get { return (eCommunicationType)base[_ComType]; }
            set { base[_ComType] = _ComType; }
        }

        private ConfigurationProperty _Frame = new ConfigurationProperty("Frame", typeof(ePLCFrame));
        [ConfigurationProperty("Frame", IsRequired = true, DefaultValue = ePLCFrame.None)]
        public ePLCFrame Frame
        {
            get { return (ePLCFrame)base[_Frame]; }
            set { base[_Frame] = _Frame; }
        }

        private ConfigurationProperty _Ip = new ConfigurationProperty("Ip", typeof(string));
        [ConfigurationProperty("Ip", IsRequired = true, DefaultValue = "0")]
        public string Ip
        {
            get { return (string)base[_Ip]; }
            set { base[_Ip] = _Ip; }
        }

        private ConfigurationProperty _Port = new ConfigurationProperty("Port", typeof(short));
        [ConfigurationProperty("Port", IsRequired = true)]
        public short Port
        {
            get { return (short)base[_Port]; }
            set { base[_Port] = _Port; }
        }

        //private ConfigurationProperty _BaseAddresses = new ConfigurationProperty("BaseAddresses", typeof(AddressElement));
        //[ConfigurationProperty("BaseAddresses", IsRequired = false, DefaultValue = null)]
        //public AddressElement BaseAddresses
        //{
        //    get { return (AddressElement)base[_BaseAddresses]; }
        //}

        //private ConfigurationProperty _DataSizes = new ConfigurationProperty("DataSize", typeof(DataSize));
        //[ConfigurationProperty("DataSize", IsRequired = false, DefaultValue = null)]
        //public DataSize DataSize
        //{
        //    get { return (DataSize)base[_DataSizes]; }
        //}
    }

    public class AddressElement : ConfigurationElement
    {
        //PLC, PC 각 구분이 필요하면 여기서 구분을 진행한다.
        private ConfigurationProperty _StartAddress = new ConfigurationProperty("StartAddress", typeof(ushort));
        [ConfigurationProperty("StartAddress", IsRequired = true)]
        public ushort StartAddress
        {
            get { return (ushort)base[_StartAddress]; }
            set { base[_StartAddress] = _StartAddress; }
        }
    }

    public class DataSize : ConfigurationElement
    {
        //PLC, PC 각 구분이 필요하면 여기서 구분을 진행한다.
        private ConfigurationProperty _ReadSize = new ConfigurationProperty("ReadSize", typeof(ushort));
        [ConfigurationProperty("ReadSize", IsRequired = true)]
        public ushort ReadSize
        {
            get { return (ushort)base[_ReadSize]; }
            set { base[_ReadSize] = _ReadSize; }
        }
    }
}
