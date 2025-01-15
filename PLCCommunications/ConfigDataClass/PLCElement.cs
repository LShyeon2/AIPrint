//220315 HHJ SCS 개발     //- MXComponentNet, SharedMemory 개선
using System.Configuration;

namespace PLCCommunications.ConfigDataClass
{
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
        public PLCElement this[int index]
        {
            get { return (PLCElement)BaseGet(index); }
            set
            {
                if (Count > 0 && BaseGet(index) != null)
                {
                    BaseRemoveAt(index);
                }
                base.BaseAdd(index, value);
            }
        }

        public new PLCElement this[short num]
        {
            get { return (PLCElement)BaseGet(num); }
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
            return new PLCElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return (element as PLCElement).Num;
        }
        #endregion
    }

    public class PLCElement : ConfigurationElement
    {
        private ConfigurationProperty _Num = new ConfigurationProperty("Num", typeof(short));
        [ConfigurationProperty("Num", IsRequired = true, IsKey = true)]
        public short Num
        {
            get { return (short)base[_Num]; }
            set { base[_Num] = _Num; }
        }

        private ConfigurationProperty _LogicalStationNum = new ConfigurationProperty("LogicalStationNum", typeof(short));
        [ConfigurationProperty("LogicalStationNum", IsRequired = true, IsKey = true)]
        public short LogicalStationNum
        {
            get { return (short)base[_LogicalStationNum]; }
            set { base[_LogicalStationNum] = _LogicalStationNum; }
        }

        private ConfigurationProperty _BaseAddresses = new ConfigurationProperty("BaseAddresses", typeof(AddressElement));
        [ConfigurationProperty("BaseAddresses", IsRequired = false, DefaultValue = null)]
        public AddressElement BaseAddresses
        {
            get { return (AddressElement)base[_BaseAddresses]; }
        }

        //210906 HHJ 개발     //- PLC Interface Base 개발
        private ConfigurationProperty _BitDeviceType = new ConfigurationProperty("BitDeviceType", typeof(string));
        [ConfigurationProperty("BitDeviceType", IsRequired = true)]
        public string BitDeviceType
        {
            get { return (string)base[_BitDeviceType]; }
            set { base[_BitDeviceType] = _BitDeviceType; }
        }
        private ConfigurationProperty _WordDeviceType = new ConfigurationProperty("WordDeviceType", typeof(string));
        [ConfigurationProperty("WordDeviceType", IsRequired = true)]
        public string WordDeviceType
        {
            get { return (string)base[_WordDeviceType]; }
            set { base[_BitDeviceType] = _WordDeviceType; }
        }
        private ConfigurationProperty _DataSizes = new ConfigurationProperty("DataSize", typeof(DataSize));
        [ConfigurationProperty("DataSize", IsRequired = false, DefaultValue = null)]
        public DataSize DataSize
        {
            get { return (DataSize)base[_DataSizes]; }
        }
    }

    public class AddressElement : ConfigurationElement
    {
        private ConfigurationProperty _BasePlcLB = new ConfigurationProperty("BasePlcLB", typeof(ushort));
        [ConfigurationProperty("BasePlcLB", IsRequired = true)]
        public ushort BasePlcLB
        {
            get { return (ushort)base[_BasePlcLB]; }
            set { base[_BasePlcLB] = _BasePlcLB; }
        }

        private ConfigurationProperty _BasePlcLW = new ConfigurationProperty("BasePlcLW", typeof(ushort));
        [ConfigurationProperty("BasePlcLW", IsRequired = true)]
        public ushort BasePlcLW
        {
            get { return (ushort)base[_BasePlcLW]; }
            set { base[_BasePlcLW] = _BasePlcLW; }
        }

        private ConfigurationProperty _BasePcLB = new ConfigurationProperty("BasePcLB", typeof(ushort));
        [ConfigurationProperty("BasePcLB", IsRequired = true)]
        public ushort BasePcLB
        {
            get { return (ushort)base[_BasePcLB]; }
            set { base[_BasePcLB] = _BasePcLB; }
        }

        private ConfigurationProperty _BasePcLW = new ConfigurationProperty("BasePcLW", typeof(ushort));
        [ConfigurationProperty("BasePcLW", IsRequired = true)]
        public ushort BasePcLW
        {
            get { return (ushort)base[_BasePcLW]; }
            set { base[_BasePcLW] = _BasePcLW; }
        }
    }

    public class DataSize : ConfigurationElement
    {
        private ConfigurationProperty _PLCBitSize = new ConfigurationProperty("PLCBitSize", typeof(ushort));
        [ConfigurationProperty("PLCBitSize", IsRequired = true)]
        public ushort PLCBitSize
        {
            get { return (ushort)base[_PLCBitSize]; }
            set { base[_PLCBitSize] = _PLCBitSize; }
        }

        private ConfigurationProperty _PLCWordSize = new ConfigurationProperty("PLCWordSize", typeof(ushort));
        [ConfigurationProperty("PLCWordSize", IsRequired = true)]
        public ushort PLCWordSize
        {
            get { return (ushort)base[_PLCWordSize]; }
            set { base[_PLCWordSize] = _PLCWordSize; }
        }

        private ConfigurationProperty _PCBitSize = new ConfigurationProperty("PCBitSize", typeof(ushort));
        [ConfigurationProperty("PCBitSize", IsRequired = true)]
        public ushort PCBitSize
        {
            get { return (ushort)base[_PCBitSize]; }
            set { base[_PCBitSize] = _PCBitSize; }
        }

        private ConfigurationProperty _PCWordSize = new ConfigurationProperty("PCWordSize", typeof(ushort));
        [ConfigurationProperty("PCWordSize", IsRequired = true)]
        public ushort PCWordSize
        {
            get { return (ushort)base[_PCWordSize]; }
            set { base[_PCWordSize] = _PCWordSize; }
        }
    }
}
