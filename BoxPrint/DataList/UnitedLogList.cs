namespace BoxPrint.DataList
{
    public class UnitedLogList
    {
        //ALARM, BCR, HSMS, Transfer... 추후 더 추가됨.
        private object _LogName;
        public object LogName
        {
            get
            {
                return _LogName;
            }
            set
            {
                _LogName = value;
            }
        }

        //string type 로그 기록 시간
        //ALARM : 빈 값
        //BCR : 기록 시간
        //HSMS : 기록 시간
        //Tranfer : 기록 시간
        private object _RecodeTime;
        public object RecodeTime
        {
            get
            {
                return _RecodeTime;
            }
            set
            {
                _RecodeTime = value;
            }
        }

        //ALARM : alarm 발생 eqpid
        //BCR : bcr이 있는 unit id
        //HSMS : Direction
        //Transfer : assign rm
        private object _Col_1;
        public object Col_1
        {
            get
            {
                return _Col_1;
            }
            set
            {
                _Col_1 = value;
            }
        }

        //HSMS : stream/function
        //BCR : bcr number
        //ALARM : alarm id
        //Transfer : command id
        private object _Col_2;
        public object Col_2
        {
            get
            {
                return _Col_2;
            }
            set
            {
                _Col_2 = value;
            }
        }

        //ALARM : alarm name
        //BCR : bcr reading data
        //HSMS : system byte
        //transfer : command type
        private object _Col_3;
        public object Col_3
        {
            get
            {
                return _Col_3;
            }
            set
            {
                _Col_3 = value;
            }
        }

        //BCR : 빈 값
        //ALARM : alarm description
        //HSMS : RCMD
        //transfer : tc status
        private object _Col_4;
        public object Col_4
        {
            get
            {
                return _Col_4;
            }
            set
            {
                _Col_4 = value;
            }
        }

        //BCR : 빈 값
        //ALARM : 경알람 여부
        //HSMS : SVID
        //transfer : 캐리어 아이디
        private object _Col_5;
        public object Col_5
        {
            get
            {
                return _Col_5;
            }
            set
            {
                _Col_5 = value;
            }
        }

        //BCR : 빈 값
        //ALARM : alarm 발생 시간
        //HSMS : CEID
        //transfer : 출발지 아이디
        private object _Col_6;
        public object Col_6
        {
            get
            {
                return _Col_6;
            }
            set
            {
                _Col_6 = value;
            }
        }

        //BCR : 빈 값
        //ALARM : alarm clear 시간
        //HSMS : RPTID
        //transfer : 목적지 아이디
        private object _Col_7;
        public object Col_7
        {
            get
            {
                return _Col_7;
            }
            set
            {
                _Col_7 = value;
            }
        }

        //BCR : 빈 값
        //ALARM : 빈 값
        //HSMS : ACK/NACK CODE
        //transfer : 캐리어 현재 위치
        private object _Col_8;
        public object Col_8
        {
            get
            {
                return _Col_8;
            }
            set
            {
                _Col_8 = value;
            }
        }

        //BCR : 빈 값
        //ALARM : 빈 값
        //HSMS : command id
        //transfer : job status
        private object _Col_9;
        public object Col_9
        {
            get
            {
                return _Col_9;
            }
            set
            {
                _Col_9 = value;
            }
        }

        //BCR : 빈 값
        //ALARM : 빈 값
        //HSMS : 캐리지 아이디 혹은 unit id
        //transfer : 빈 값
        private object _Col_10;
        public object Col_10
        {
            get
            {
                return _Col_10;
            }
            set
            {
                _Col_10 = value;
            }
        }

        //BCR : 빈 값
        //ALARM : 빈 값
        //HSMS : transfer source id
        //transfer : 빈 값
        private object _Col_11;
        public object Col_11
        {
            get
            {
                return _Col_11;
            }
            set
            {
                _Col_11 = value;
            }
        }

        //BCR : 빈 값
        //ALARM : 빈 값
        //HSMS : transfer destination id
        //transfer : 빈 값
        private object _Col_12;
        public object Col_12
        {
            get
            {
                return _Col_12;
            }
            set
            {
                _Col_12 = value;
            }
        }

        //BCR : 빈 값
        //ALARM : 빈 값
        //HSMS : S2F31의 time 혹은 terminal message값
        //transfer : 빈 값
        private object _Col_13;
        public object Col_13
        {
            get
            {
                return _Col_13;
            }
            set
            {
                _Col_13 = value;
            }
        }
    }
}
