using System;
using System.Xml.Serialization;
namespace BoxPrint.Alarm
{
    public class AlarmData : ICloneable
    {
        //SuHwan_20220526 : 클론
        public object Clone()
        {
            AlarmData clone = MemberwiseClone() as AlarmData;

            return clone;
        }

        /// <summary>
        /// DB 에서의 식별자.
        /// </summary>
        [XmlIgnore]
        public int LogID;

        /// <summary>
        /// 알람 ID
        /// </summary>
        [XmlAttribute("ID")]
        public string AlarmID { get; set; }
        short _AlarmID = 0;
        public short iAlarmID
        {
            get
            {
                if (_AlarmID == 0)
                {
                    short.TryParse(AlarmID, out _AlarmID);

                }
                return _AlarmID;
            }
        }

        /// <summary>
        /// 경알람 구분 플래그
        /// </summary> 
        [XmlAttribute("IsLightAlarm")]
        public bool IsLightAlarm { get; set; }
        public string AlarmLevel
        {
            get
            {
                if (IsLightAlarm)
                {
                    return "N";
                }
                else
                {
                    return "Y";
                }
            }
        }

        /// <summary>
        /// 알람이 발생한 모듈 이름
        /// </summary>
        [XmlAttribute("ModuleType")]
        public String ModuleType { get; set; }

        public String ModuleName { get; set; }

        public String CarrierID { get; set; } //230525 RGJ 알람 데이터 캐리어 ID 항목 추가.

        /// <summary>
        /// 알람 이름
        /// </summary>
        [XmlAttribute("Name")]
        public String AlarmName { get; set; }

        /// <summary>
        /// 알람 상세 설명
        /// </summary>
        [XmlAttribute("Description_KOR")]
        public String Description { get; set; }

        /// <summary>
        /// 알람 상세 설명
        /// </summary>
        [XmlAttribute("Description_ENG")]
        public String Description_ENG { get; set; }

        /// <summary>
        /// 알람 상세 설명
        /// </summary>
        [XmlAttribute("Description_CHN")]
        public String Description_CHN { get; set; }

        /// <summary>
        /// 알람 상세 설명
        /// </summary>
        [XmlAttribute("Description_HUN")]
        public String Description_HUN { get; set; }

        /// <summary>
        /// 알람 해결법
        /// </summary>
        [XmlAttribute("Solution_KOR")]
        public String Solution { get; set; }

        /// <summary>
        /// 알람 해결법
        /// </summary>
        [XmlAttribute("Solution_ENG")]
        public String Solution_ENG { get; set; }

        /// <summary>
        /// 알람 해결법
        /// </summary>
        [XmlAttribute("Solution_CHN")]
        public String Solution_CHN { get; set; }

        /// <summary>
        /// 알람 해결법
        /// </summary>
        [XmlAttribute("Solution_HUN")]
        public String Solution_HUN { get; set; }

        /// <summary>
        /// 발생시각
        /// </summary>
        public DateTime OccurDateTime { get; set; }

        /// <summary>
        /// 해결시각
        /// </summary>
        public DateTime ClearDateTime { get; set; }

        /// <summary>
        /// DB 에서의 식별자.
        /// </summary>
        [XmlIgnore]
        public int ScenarioStep;

        [XmlAttribute("RecoveryOption")]
        public string RecoveryOption { get; set; }

        [XmlArrayAttribute("AlarmRecoveryList")]
        public AlarmRecoveryCmd[] AlarmRecoveryList { get; set; }

        public int ListNo { get; set; }
    }

    public class AlarmRecoveryCmd : ICloneable
    {
        //SuHwan_20220526 : 몰라 일단 클론
        public object Clone()
        {
            return MemberwiseClone() as AlarmRecoveryCmd;
        }

        [XmlAttribute("Command")]
        public string Command { get; set; }

        [XmlAttribute("Description")]
        public string Description { get; set; }

        public AlarmRecoveryCmd()
            : this(string.Empty, string.Empty)
        {
            //SuHwan_20220525 : 나중에 수정하자
            this.Command = " ";
            this.Description = " ";
        }
        public AlarmRecoveryCmd(string command, string description)
        {
            this.Command = command;
            this.Description = description;
        }
    }
}
