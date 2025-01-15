using Communication.PLCProtocol;
using PLCProtocol.Base;
using PLCProtocol.DataClass;
using BoxPrint.Config;   //20220728 조숭진 config 방식 변경
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PLCProtocol
{
    public class Protocol_4E : Protocol_MCProtocolBase
    {
        public Protocol_4E(PLCElement element) : base(element) {; }

        private int serialNoSeq = 1;

        private int GetNextSerialNo()
        {
            lock(SerialNoSyncLock) //240410 RGJ 동시 쓰레드 접근시 Serial No 동일한 값이 리턴되므로 Lock건다.
            {
                if (serialNoSeq >= 9999) //등호가 빠져서 추가. 10000이 되므로 4자리 오버됨
                {
                    serialNoSeq = 1;
                }
                else
                {
                    serialNoSeq++;
                }
                return serialNoSeq;
            }
        }
        private int GetCurrentSerialNo()
        {
            int SN = 0;
            lock (SerialNoSyncLock) //240410 RGJ 동시 쓰레드 접근시 Serial No 동일한 값이 리턴되므로 Lock건다.
            {
                SN = serialNoSeq;
            }
            return SN;
        }

        protected override byte[] SubHeader(bool bAddedserialNo = true)
        {
            int serialNo = 0;
            if (bAddedserialNo)
            {
                serialNo = GetNextSerialNo();
            }
            else
            {
                serialNo = GetCurrentSerialNo();
            }
            //4E타입은 5400 + 시리얼 번호(커맨드 구분을 위한 번호) + 0000
            //바이너리 타입은 시리얼 번호 앞뒤 바이트 변환이 필요. ex) 1234 -> 3412
            if (ComType.Equals(eCommunicationType.Ascii))
            {
                return Encoding.Default.GetBytes("5400" + serialNo.ToString().PadLeft(4, '0') + "0000");
            }
            else
            {
                List<byte> lists = new List<byte>();
                byte[] bytes = ProtocolHelper.UShortToByte(0x5400);
                lists.AddRange(bytes);
                bytes = ProtocolHelper.UShortToByte((ushort)serialNo, true);  //뒤집어서 전송
                lists.AddRange(bytes);
                bytes = ProtocolHelper.UShortToByte(0x0000);
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

            //멀티 쓰레드 환경에서는 시리얼 넘버 매칭이 안될듯....
            //시리얼 넘버 매칭을 시키려면 시리얼이 있는 리스폰스에
            //자기가 작업한 영역과 데이터를 리스폰스 해줘야 리스트를 가지고 비교를 할탠데,
            //리스폰스에는 서브헤더에 시리얼만 보내고 엑세스 루트만 보낼뿐 디바이스 정보가 존재하지 않음.
            //일단 리스폰스 바이트 배열에서만 지워주고 방법을 찾으면 그때 추가하도록....
            return true;
        }
    }
}
