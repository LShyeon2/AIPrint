using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace WCF_LBS.DataParameter
{
    public class Parameter_TOWERLAMP : ParameterItem
    {
        public string Green = "0";         //0:Off 1:On
        public string Yellow = "0";        //0:Off 1:On
        public string Red = "0";           //0:Off 1:On
        public string Buzzer = "0";        //0:Off 1:light alarm 2:Heavy alarm
        public string MuteMode = "0";      //0:Mute off(Buzzer 활성)  1:Mute On(Buzzer 비활성)
        public Parameter_TOWERLAMP()
        {
            ReadData();
        }
        //실제 I/O를 획득
        public override bool ReadData()
        {
            //실제 데이터를 펌웨어에서 얻어온다. 미구현
            return true;
        }
        public override XElement XMLRendering()
        {
            //타워램프는 하위 유닛이 없어서 직접 파라미터를 넣는다.
            throw new NotImplementedException();
        }
        public List<ParameterItem> GetParameters()
        {
            List<ParameterItem> paralist = new List<ParameterItem>();
            paralist.Add(new ParameterItem("GREEN", Green));
            paralist.Add(new ParameterItem("YELLOW", Yellow));
            paralist.Add(new ParameterItem("RED", Red));
            paralist.Add(new ParameterItem("BUZZER", Buzzer));
            paralist.Add(new ParameterItem("MUTEMODE", MuteMode));
            return paralist;
        }

    }
}
