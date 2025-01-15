using PLCProtocol.DataClass;
using BoxPrint;
using BoxPrint.DataList;
using BoxPrint.Log;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using BoxPrint.GUI.EventCollection;


namespace PLCProtocol
{
    public class ProtocolManager_Client : ProtocolManager
    {
        private Dictionary<string, string> dicReferencePLCData = new Dictionary<string, string>();

        /// <summary>
        /// DB에서 읽어온 PLCDataInfo 리스트를 로컬메모리 안에 다시 넣는다
        /// </summary>
        /// <param name="rcvModuleID"></param>
        /// <param name="rcvDirection"></param>
        /// <param name="rcvListPLCDataInfo"></param>
        public void ConvertToMemoryBuffer_PLCDataInfo(string rcvModuleID, ConcurrentDictionary<string, PLCDataItem> rcvDirection, List<PLCDataInfo> rcvListPLCDataInfo)
        {
            string[] buffeSplitr = null;

            try
            {
                //확인용으로 처음꺼 하나 가져온다
                var dicFist = rcvDirection.First();


                //디비에서 받아온 데이타를 '/' 로 스프릿 하여 배열에 저장 한다
                var whereResult = rcvListPLCDataInfo.Where(item => item.ModuleID == rcvModuleID && item.Direction == dicFist.Value.Area).FirstOrDefault();

                if (whereResult == null)
                    return;
                else
                {
                    //확인용 PLCData 
                    string nameBuffer = dicFist.Value.ModuleType.ToString() + "_" + dicFist.Value.Area.ToString();
                    if (!dicReferencePLCData.ContainsKey(nameBuffer))
                    {
                        dicReferencePLCData.Add(nameBuffer, "");
                    }

                    var checkPLCData = dicReferencePLCData[nameBuffer];

                    if (checkPLCData == whereResult.PLCData)
                        return;

                    checkPLCData = whereResult.PLCData;

                    buffeSplitr = checkPLCData.Split('/');
                }

                int i = 0;
                //여기가 중요한데.. rcvDirection 로  targetPLCDataInfo.DicPLCData 순서를 찾아서 배열에 있는 값을 넣어준다
                foreach (KeyValuePair<string, PLCDataItem> item in rcvDirection.OrderBy(s => s.Value.AddressOffset).ThenBy(s => s.Value.BitOffset))
                {
                    if (item.Key.Contains("BatchRead"))
                        continue;

                    Write_LocalMemory(rcvModuleID, rcvDirection, item.Key, buffeSplitr[i].Trim('\0'));

                    i++;
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
            }


        }

        /// <summary>
        /// 디비에서 읽어오기
        /// </summary>
        public override void GetPLCDataInfoFromDB()
        {
            try
            {
                var listPLCDataInfoBuffer = GlobalData.Current.DBManager.DbGetProcedurePIOInfo();

                ConvertToMemoryBuffer_PLCDataInfo(GlobalData.Current.EQPID, GlobalData.Current.MainBooth.PCtoPLC, listPLCDataInfoBuffer);
                ConvertToMemoryBuffer_PLCDataInfo(GlobalData.Current.EQPID, GlobalData.Current.MainBooth.PLCtoPC, listPLCDataInfoBuffer);

                foreach (var item in GlobalData.Current.mRMManager.ModuleList.Values)
                {
                    ConvertToMemoryBuffer_PLCDataInfo(item.ModuleName, item.PCtoPLC, listPLCDataInfoBuffer);
                    ConvertToMemoryBuffer_PLCDataInfo(item.ModuleName, item.PLCtoPC, listPLCDataInfoBuffer);
                }

                //LKJ 20221102
                foreach (var item in GlobalData.Current.PortManager.AllCVList)
                {
                    ConvertToMemoryBuffer_PLCDataInfo(item.ModuleName, item.PCtoPLC, listPLCDataInfoBuffer);
                    ConvertToMemoryBuffer_PLCDataInfo(item.ModuleName, item.PLCtoPC, listPLCDataInfoBuffer);
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
            }

        }

        /// <summary>
        /// 생성자
        /// </summary>
        public ProtocolManager_Client() : base(null)
        {
            UIEventCollection.Instance.OnRequestPlcState += OnRequestPlcState;
        }
        private void OnRequestPlcState()
        {
            List<PLCStateData> stateDataList = GlobalData.Current.DBManager.DbGetProcedurePLCInfo();
            UIEventCollection.Instance.InvokerResponsePlcState(stateDataList.OrderBy(o => o.ConnectInfo).ToList());
        }

        public override void DisposeEvent()
        {
            UIEventCollection.Instance.OnRequestPlcState -= OnRequestPlcState;
        }
        public override bool Connect() { return false; }
        public override bool CheckConnection(short PLCNumber) { return false; }
        public override bool CheckALLPLCConnection() { return false; }
        //230103 HHJ SCS 개선
        //public override bool ReadFullRaw(short PLCNumber, PLCDataItem pItem) { return false; }
        public override bool ReadFullRaw(eDataChangeUnitType unitType, string unitName, short PLCNumber, PLCDataItem pItem) { return false; }
        public override bool Write(string ModuleName, ConcurrentDictionary<string, PLCDataItem> items, string key, object value) { return false; }
    }
    //public class ProtocolManager_Client : ProtocolManager
    //{

    //    /// <summary>
    //    /// DB 에서 받아온 IO를 딕셔너리로 변경해서 글로발의 dicPLCDataInfo에 저장
    //    /// </summary>
    //    /// <param name="rcvModuleID"></param>
    //    /// <param name="rcvDirection"></param>
    //    /// <param name="rcvListPLCDataInfo"></param>
    //    public override void ConvertDictionaryPLCDataInfo(string rcvModuleID, ConcurrentDictionary<string, PLCDataItem> rcvDirection, List<PLCDataInfo> rcvListPLCDataInfo)
    //    {
    //        string[] buffeSplitr = null;
    //        PLCDataInfo targetPLCDataInfo = new PLCDataInfo();

    //        try
    //        {
    //            //확인용으로 처음꺼 하나 가져온다
    //            var dicFist = rcvDirection.First();

    //            //변경을 진행할 모듈의 PLCDataInfo 를 dicPLCDataInfo 에 rcvModuleID 를 키값으로 사용해 가져온다
    //            if (dicFist.Value.Area == eAreaType.PCtoPLC)
    //            {
    //                if (GlobalData.Current.dicPLCDataInfo_PCtoPLC.ContainsKey(rcvModuleID))
    //                {
    //                    targetPLCDataInfo = GlobalData.Current.dicPLCDataInfo_PCtoPLC[rcvModuleID];
    //                }
    //                else
    //                {
    //                    GlobalData.Current.dicPLCDataInfo_PCtoPLC.TryAdd(rcvModuleID, new PLCDataInfo());
    //                }

    //            }
    //            else if (dicFist.Value.Area == eAreaType.PLCtoPC)
    //            {
    //                if (GlobalData.Current.dicPLCDataInfo_PLCtoPC.ContainsKey(rcvModuleID))
    //                {
    //                    targetPLCDataInfo = GlobalData.Current.dicPLCDataInfo_PLCtoPC[rcvModuleID];
    //                }
    //                else
    //                {
    //                    GlobalData.Current.dicPLCDataInfo_PLCtoPC.TryAdd(rcvModuleID, new PLCDataInfo());
    //                }
    //            }
    //            else
    //                return;

    //            //디비에서 받아온 데이타를 '/' 로 스프릿 하여 배열에 저장 한다
    //            var whereResult = rcvListPLCDataInfo.Where(item => item.ModuleID == rcvModuleID && item.Direction == dicFist.Value.Area).FirstOrDefault();

    //            if (whereResult == null)
    //                return;
    //            else
    //            {
    //                if (targetPLCDataInfo.PLCData == whereResult.PLCData)
    //                    return;

    //                targetPLCDataInfo.PLCData = whereResult.PLCData;

    //                buffeSplitr = targetPLCDataInfo.PLCData.Split('/');
    //            }

    //            int i = 0;
    //            //여기가 중요한데.. rcvDirection 로  targetPLCDataInfo.DicPLCData 순서를 찾아서 배열에 있는 값을 넣어준다
    //            foreach (KeyValuePair<string, PLCDataItem> item in rcvDirection.OrderBy(s => s.Value.AddressOffset).ThenBy(s => s.Value.BitOffset))
    //            {
    //                if (item.Key.Contains("BatchRead"))
    //                    continue;

    //                if (targetPLCDataInfo.DicPLCData.ContainsKey(item.Key))
    //                {
    //                    if (targetPLCDataInfo.DicPLCData[item.Key] != buffeSplitr[i])
    //                        targetPLCDataInfo.DicPLCData[item.Key] = buffeSplitr[i];
    //                }
    //                else
    //                {
    //                    targetPLCDataInfo.DicPLCData.TryAdd(item.Key, buffeSplitr[i]);
    //                }

    //                i++;
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
    //        }


    //    }


    //    public override void GetPLCDataInfoFromDB()
    //    {
    //        try
    //        {
    //            var listPLCDataInfoBuffer = GlobalData.Current.DBManager.DbGetProcedurePIOInfo();

    //            foreach (var item in GlobalData.Current.mRMManager.ModuleList.Values)
    //            {
    //                ConvertDictionaryPLCDataInfo(item.ModuleName, item.PCtoPLC, listPLCDataInfoBuffer);
    //                ConvertDictionaryPLCDataInfo(item.ModuleName, item.PLCtoPC, listPLCDataInfoBuffer);
    //            }

    //        }
    //        catch (Exception ex)
    //        {
    //            LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
    //        }

    //    }

    //    //public void ConvertDictionaryAllPLCDataInfo(string rcvModuleID, ConcurrentDictionary<string, PLCDataItem> rcvDirection)
    //    //{
    //    //    string[] buffeSplitr = null;
    //    //    PLCDataInfo targetPLCDataInfo;

    //    //    var listDBPIOInfo = GlobalData.Current.DBManager.DbGetProcedurePIOInfo();

    //    //    foreach(var item in listDBPIOInfo)
    //    //    {
    //    //        if (item.Direction.ToString() == "PCtoPLC")
    //    //            targetPLCDataInfo = dicPLCDataInfo_PCtoPLC[item.ModuleID];
    //    //        else if (item.Direction.ToString() == "PLCtoPC")
    //    //            targetPLCDataInfo = dicPLCDataInfo_PLCtoPC[item.ModuleID];
    //    //        else
    //    //            return;





    //    //        if (targetPLCDataInfo.PLCData == item.PLCData)
    //    //            continue;

    //    //        targetPLCDataInfo.PLCData = item.PLCData;
    //    //        buffeSplitr = targetPLCDataInfo.PLCData.Split('/');

    //    //        int i = 0;
    //    //        //여기가 중요한데.. rcvDirection 로  targetPLCDataInfo.DicPLCData 순서를 찾아서 배열에 있는 값을 넣어준다
    //    //        foreach (KeyValuePair<string, PLCDataItem> item2 in rcvDirection.OrderBy(s => s.Value.AddressOffset).ThenBy(s => s.Value.BitOffset))
    //    //        {
    //    //            if (targetPLCDataInfo.DicPLCData.ContainsKey(item2.Key))
    //    //            {
    //    //                if (targetPLCDataInfo.DicPLCData[item2.Key] != buffeSplitr[i])
    //    //                    targetPLCDataInfo.DicPLCData[item2.Key] = buffeSplitr[i];
    //    //            }

    //    //            i++;
    //    //        }
    //    //    }
    //    //}

    //    public ProtocolManager_Client() : base(null)
    //    {

    //    }
    //    public override bool Connect() { return false; }
    //    public override bool CheckConnection(short PLCNumber) { return false; }
    //    public override bool CheckALLPLCConnection() { return false; }
    //    public override bool ReadFullRaw(short PLCNumber, PLCDataItem pItem) { return false; }
    //    public override object Read(string ModuleName, ConcurrentDictionary<string, PLCDataItem> items, string key) { return null; }
    //    public override bool ReadBit(string ModuleName, ConcurrentDictionary<string, PLCDataItem> items, string key)
    //    {
    //        var returnBuffer = getPLCDataInfo(ModuleName, items, key);

    //        return (returnBuffer == "1") ? true : false;
    //    }
    //    public override short ReadShort(string ModuleName, ConcurrentDictionary<string, PLCDataItem> items, string key)
    //    {
    //        short value;
    //        var returnBuffer = getPLCDataInfo(ModuleName, items, key);

    //        if (short.TryParse(returnBuffer, out value))
    //            return value;
    //        else
    //            return 0;
    //    }
    //    public override string ReadString(string ModuleName, ConcurrentDictionary<string, PLCDataItem> items, string key)
    //    {
    //        var returnBuffer = getPLCDataInfo(ModuleName, items, key);

    //        return string.IsNullOrEmpty(returnBuffer) ? string.Empty : returnBuffer;
    //    }
    //    public override bool Write(string ModuleName, ConcurrentDictionary<string, PLCDataItem> items, string key, object value) { return false; }

    //    /// <summary>
    //    /// GlobalData.Current.dicPLCDataInfo 에서 String으로 값 가져오기
    //    /// </summary>
    //    /// <param name="rcvModuleName"></param>
    //    /// <param name="rcvDirection"></param>
    //    /// <param name="rcvKey"></param>
    //    /// <returns></returns>
    //    private string getPLCDataInfo(string rcvModuleName, ConcurrentDictionary<string, PLCDataItem> rcvDicPLCDataItem, string rcvKey)
    //    {
    //        PLCDataInfo PLCDataInfoBuffer = new PLCDataInfo();

    //        try
    //        {
    //            //확인용으로 처음꺼 하나 가져온다
    //            var dicFist = rcvDicPLCDataItem.First();

    //            if (dicFist.Value.Area == eAreaType.PCtoPLC)
    //            {
    //                if (GlobalData.Current.dicPLCDataInfo_PCtoPLC.ContainsKey(rcvModuleName))
    //                    PLCDataInfoBuffer = GlobalData.Current.dicPLCDataInfo_PCtoPLC[rcvModuleName];
    //            }
    //            else if (dicFist.Value.Area == eAreaType.PLCtoPC)
    //            {
    //                if (GlobalData.Current.dicPLCDataInfo_PLCtoPC.ContainsKey(rcvModuleName))
    //                    PLCDataInfoBuffer = GlobalData.Current.dicPLCDataInfo_PLCtoPC[rcvModuleName];
    //            }
    //            else
    //                return null;

    //            if (PLCDataInfoBuffer.DicPLCData.ContainsKey(rcvKey))
    //            {
    //                if (PLCDataInfoBuffer.DicPLCData.TryGetValue(rcvKey, out string getResult))
    //                {
    //                    //string[] buffeSplitr = getResult.Split('/');
    //                    return getResult.Trim('\0');
    //                }
    //            }

    //            return null;
    //        }
    //        catch (Exception ex)
    //        {
    //            LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
    //            return null;
    //        }
    //    }
    //}
}
