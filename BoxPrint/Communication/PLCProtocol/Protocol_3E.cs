
using Communication.PLCProtocol;
using PLCProtocol.Base;
using PLCProtocol.DataClass;
using BoxPrint.Config;       //20220728 조숭진 config 방식 변경
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PLCProtocol
{
    public class Protocol_3E : Protocol_MCProtocolBase
    {
        public Protocol_3E(PLCElement element) : base(element) {; }

        protected override byte[] SubHeader(bool bAddedserialNo = true)
        {
            //3E타입은 5000
            //바이너리 타입은 시리얼 번호 앞뒤 바이트 변환이 필요. ex) 1234 -> 3412
            if (ComType.Equals(eCommunicationType.Ascii))
                return Encoding.Default.GetBytes("5000");
            else
            {
                List<byte> lists = new List<byte>();
                byte[] bytes = ProtocolHelper.UShortToByte(0x5000);
                lists.AddRange(bytes);
                return lists.ToArray();
            }
        }

        //서브헤더 리스폰스는 프레임에 따라 다름. 하위 프로토콜 타입으로 구분하면 상위정보인 프레임정보를 필요로 하기에 프레임단에서 처리
        protected override bool CheckResponseSubHeader(byte[] recv, out byte[] other)
        {
            byte[] responseSubHeader = SubHeader(false);        //리스폰스 D400 + 시리얼 + 0000
            int iStart = 0, iCount = responseSubHeader.Length;  //총길이는 만들어진 길이와 같아야함.
            List<byte> lists = recv.ToList();                   //배열 요소 삭제를 위해 리스트 변환
            List<byte> temp = lists.GetRange(iStart, iCount);   //삭제할 요소 가져옴
            lists.RemoveRange(iStart, iCount);                  //가져온 요소 삭제
            other = lists.ToArray();                            //삭제되고 남은 요소들 배열로 재변환

            //3E에서는 아스키는 responseSubHeader의 가장 앞자리를 'D'로,
            //바이너리는 2번 바이트를 D0로 바꿔주면 되지만 4E가 진행하지 않기에 3E도 그냥 빼놓음.
            return true;
        }
    }
}
