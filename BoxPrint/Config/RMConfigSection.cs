
using System.Configuration;

namespace BoxPrint.Config
{
    public class RMConfigSection : ConfigurationSection
    {
        private ConfigurationProperty _SystemName = new ConfigurationProperty("SystemName", typeof(string));
        //private ConfigurationProperty _OffsetSendFlag = new ConfigurationProperty("OffsetSendFlag", typeof(bool));
        //private ConfigurationProperty _MultipleCommand = new ConfigurationProperty("MultipleCommand", typeof(bool));
        //private ConfigurationProperty _RobotConnectTimeOut = new ConfigurationProperty("RobotConnectTimeOut", typeof(int));
        //private ConfigurationProperty _RobotRcvTimeOut = new ConfigurationProperty("RobotRcvTimeOut", typeof(int));
        //private ConfigurationProperty _InitMoveingTimeOut = new ConfigurationProperty("InitMoveingTimeOut", typeof(int));
        //private ConfigurationProperty _WaitIOTimeOut = new ConfigurationProperty("WaitIOTimeOut", typeof(int));

        [ConfigurationProperty("SystemName", IsRequired = true)]
        public string SystemName
        {
            get { return (string)base[_SystemName]; }
            set { base[_SystemName] = _SystemName; }
        }
        [ConfigurationProperty("RackMaster")]
        public RMUnitConfigElement RM1Element
        {
            get
            {
                return (RMUnitConfigElement)this["RackMaster"];
            }
            set
            {
                value = (RMUnitConfigElement)this["RackMaster"];
            }
        }
        [ConfigurationProperty("RackMaster_Second")]
        public RMUnitConfigElement RM2Element
        {
            get
            {
                return (RMUnitConfigElement)this["RackMaster_Second"];
            }
            set
            {
                value = (RMUnitConfigElement)this["RackMaster_Second"];
            }
        }



        //[ConfigurationProperty("OffsetSendFlag", IsRequired = true)]
        //public bool OffsetSendFlag
        //{
        //    get { return (bool)base[_OffsetSendFlag]; }
        //    set { base[_OffsetSendFlag] = _OffsetSendFlag; }
        //}

        //[ConfigurationProperty("MultipleCommand", IsRequired = true)]
        //public bool MultipleCommand
        //{
        //    get { return (bool)base[_MultipleCommand]; }
        //    set { base[_MultipleCommand] = _MultipleCommand; }
        //}

        //[ConfigurationProperty("RobotConnectTimeOut", IsRequired = true)]
        //public int RobotConnectTimeOut
        //{
        //    get { return (int)base[_RobotConnectTimeOut]; }
        //    set { base[_RobotConnectTimeOut] = _RobotConnectTimeOut; }
        //}

        //[ConfigurationProperty("RobotRcvTimeOut", IsRequired = true)]
        //public int RobotRcvTimeOut
        //{
        //    get { return (int)base[_RobotRcvTimeOut]; }
        //    set { base[_RobotRcvTimeOut] = _RobotRcvTimeOut; }
        //}

        //[ConfigurationProperty("InitMoveingTimeOut", IsRequired = true)]
        //public int InitMoveingTimeOut
        //{
        //    get { return (int)base[_InitMoveingTimeOut]; }
        //    set { base[_InitMoveingTimeOut] = _InitMoveingTimeOut; }
        //}
        //[ConfigurationProperty("WaitIOTimeOut", IsRequired = true)]
        //public int WaitIOTimeOut
        //{
        //    get { return (int)base[_WaitIOTimeOut]; }
        //    set { base[_WaitIOTimeOut] = _WaitIOTimeOut; }
        //}
    }
}