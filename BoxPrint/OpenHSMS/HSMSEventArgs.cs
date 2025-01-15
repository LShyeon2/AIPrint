using System;

namespace BoxPrint.OpenHSMS
{
    /// <summary>
    /// HSMSStateChangedEventArgs 클래스
    /// </summary>
    public class HSMSStateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// HSMS 통신 가능 여부를 나타내는 속성
        /// </summary>
        public bool Enabled { get; set; }
    }

    /// <summary>
    /// HSMSMessageReceivedEventArgs 클래스
    /// </summary>
    public class HSMSMessageReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// HSMS 수신 데이터 메세지 속성
        /// </summary>
        public OSG.Com.HSMS.Common.DataMessage DataMessage { get; set; }

        public HSMSMessageReceivedEventArgs(OSG.Com.HSMS.Common.DataMessage msg)
        {
            this.DataMessage = msg;
        }
    }
}
