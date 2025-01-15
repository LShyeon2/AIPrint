using PLCProtocol.DataClass;
using BoxPrint.Log;
using System;
using System.Collections.Generic;

namespace PLCProtocol.Base
{
    public class CommunicationTypeBase
    {
        protected virtual ePLCSeries PLCSeries { get; }
        #region MC Protocol Req
        protected virtual byte[] AccessRoute() { return null; }
        protected virtual byte[] RequestDataLength(byte[] reqData, byte[] monitoring) { return null; }
        protected virtual byte[] MonitoringTimer() { return null; }
        protected virtual byte[] RequestData(PLCDataItem pItem, bool read, object WriteValue = null, byte[] bitWord = null) { return null; }
        protected virtual byte[] DataCommand(bool read) { return null; }
        protected virtual byte[] DataSubCommand(eDevice device) { return null; }
        protected virtual byte[] DataHeadDevice(PLCDataItem pItem) { return null; }
        protected virtual byte[] HeadDeviceCode(eDevice device) { return null; }
        protected virtual byte[] HeadDeviceNumber(eDevice device, int iaddress) { return null; }
        protected virtual byte[] DataDevicePoints(int datasize) { return null; }
        #endregion

        #region MC Protocol Res
        protected virtual bool CheckResponseAccessRoute(byte[] recv, out byte[] other) { other = null; return false; }
        protected virtual int CheckResponseDataLength(byte[] recv, out byte[] other) { other = null; return -1; }
        protected virtual bool CheckResponseEndCode(byte[] recv, out byte[] other) { other = null; return false; }
        #endregion

        #region Converter
        public virtual byte[] WriteDataConvert(PLCDataItem pItem, object value, byte[] bitWord = null) { return null; }
        public virtual object ReadDataConvert(PLCDataItem pItem, byte[] value) { return null; }
        #endregion

        public byte[] MakeProtocolCommand(PLCDataItem pItem, bool read, object WriteValue = null, byte[] bitWord = null)
        {
            List<byte> lists = new List<byte>();

            try
            {
                byte[] reqData = RequestData(pItem, read, WriteValue, bitWord);
                byte[] monitoringTimer = MonitoringTimer();
                byte[] reqDataLength = RequestDataLength(reqData, monitoringTimer);
                byte[] accessRoute = AccessRoute();

                lists.AddRange(accessRoute);
                lists.AddRange(reqDataLength);
                lists.AddRange(monitoringTimer);
                lists.AddRange(reqData);
            }
            catch (Exception ex)
            {
                _ = ex;
            }
            return lists.ToArray();
        }

        public bool CheckResponse(byte[] response, out byte[] responseValue)
        {
            responseValue = null;
            try
            {
                byte[] temp;
                int ResponseSize = -1;

                if (!CheckResponseAccessRoute(response, out temp))
                {
                    return false;
                }
                ResponseSize = CheckResponseDataLength(temp, out temp);

                if (ResponseSize < 0)
                {

                    return false;
                }
                if (!CheckResponseEndCode(temp, out temp))
                {
                    return false;
                }
                responseValue = temp;
                return true;
            }
            catch (Exception ex)
            {
                _ = ex;
            }
            return false;
        }
    }
}
