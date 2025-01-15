using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoxPrint.DataList
{
    public class ActiveZone
    {
        public string ZoneName;
        public int ZoneTotalCount;
        public int ZoneCapa;
        public eZoneType ZoneType;
        public int ZoneOccupied
        {
            get
            {
                int Occupied = ZoneTotalCount - ZoneCapa;
                if(Occupied < 0)
                {
                    return 0;
                }
                else
                {
                    return Occupied;
                }
            }
        }
        public ActiveZone(string ZName,eZoneType ZT)
        {
            ZoneName = ZName;
            ZoneType = ZT;
        }

    }
}
