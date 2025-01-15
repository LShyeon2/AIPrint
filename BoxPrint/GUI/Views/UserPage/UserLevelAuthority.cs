using System.Xml.Serialization;

namespace BoxPrint.GUI.Views.UserPage
{
    class UserLevelAuthority
    {
        /// <summary>
        /// IO 접점이 속한 Module ID
        /// </summary>
        /// <value>Module ID</value>
        [XmlAttribute("ListNo")]
        public int ListNo { get; set; }

        [XmlAttribute("Group")]
        public string Group { get; set; }

        [XmlAttribute("Menu")]
        public string Menu { get; set; }

        [XmlAttribute("UserUse")]
        public bool UserUse { get; set; }

        public UserLevelAuthority(int listno, string group, string menu, bool use)
        {
            ListNo = listno;
            Group = group;
            Menu = menu;
            UserUse = use;
        }

    }
}
