using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoxPrint.Modules.User
{
    public class AuthorityItem
    {
        public string Authority_Name { get; set; }
        public eUserLevel Authority_Level { get; set; }
        public string Name_KOR{ get; set; }
        public string Name_HUN{ get; set; }
        public string Name_CHN { get; set; }
        public bool ModifyAccess { get; set; }
        public bool ReadAccess { get; set; }
    }
}
