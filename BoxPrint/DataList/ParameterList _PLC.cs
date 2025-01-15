using System;
using System.Collections.Generic;
using System.Threading;


namespace BoxPrint.DataList
{
    //2021년 5월 21일 금요일 오전 10:49:57 - Editted by 수환 : PLC Parameter 모음

    public class ParameterList_PLC
    {
        public string _endMARK { set; get; }//메시지 마지막 문자

        public PLCPIO_Value _PioData = new PLCPIO_Value();

        public CancellationTokenSource _taskCancelToken = new CancellationTokenSource();//2021년 5월 28일 금요일 오전 10:30:57 - Editted by 수환 : 테스크 켄슬 하는 토큰 생성

        //PLC Bit Address 저장용
        public Dictionary<string, PLCIFMAP_Value> _dicPLCIFMap_Bit = new Dictionary<string, PLCIFMAP_Value>();

        //PLC Word Address 저장용
        public Dictionary<string, PLCIFMAP_Value> _dicPLCIFMap_Word = new Dictionary<string, PLCIFMAP_Value>();

        public bool isRMMoveStop = false; //정지용
    }

    //인터페이스 맵 
    public class PLCIFMAP_Value
    {
        public string _name { get; set; }
        public int _number { get; set; }
        public string _definition { get; set; }
        public string _description { get; set; }
        public string _area { get; set; }
        public string _startAddress { get; set; }
        public string _EndAddress { get; set; }
        public int _length { get; set; }
        public ePLCMessageType _type { get; set; }
    }

    //이터페이스 정보 
    public class PLCPIO_Value
    {
        public ePIOState _pioState { set; get; }//피아이오 런 상태
        public int _stepNo { set; get; }//피아이오 진행 스탭
        public DateTime? _timeOutPIO { set; get; }//Nullable 형식 PIO 타암아웃 체크
        public DateTime _timeOutCimAlive { set; get; }//CIM 얼라이브 타암아웃 체크
        public DateTime? _timeOutPLCAlive { set; get; }//Nullable 형식 PLC 얼라이브 타암아웃 체크
        public eBitState _oldPLCAliveBit { set; get; }//피엘씨 얼라이브 비트 확인용

        //피아이오 상태 변경
        public void changeState(ePIOState rcvState)
        {
            this._pioState = rcvState;
            this._stepNo = 0;
            this._timeOutPIO = null;
        }
    }
}
