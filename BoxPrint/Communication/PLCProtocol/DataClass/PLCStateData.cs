using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLCProtocol.DataClass
{
    public class PLCStateData
    {
        public string PLCName { get; set; }
        public string ConnectInfo { get; set; }
        public ePLCStateDataState State { get; set; }
        public DateTime StateChangeTime { get; set; }
    }
}
