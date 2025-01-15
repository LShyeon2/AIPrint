using Communication.PLCProtocol;
using PLCProtocol.Base;
using PLCProtocol.DataClass;
using BoxPrint;
using BoxPrint.Alarm;
using BoxPrint.Communication.PLCProtocol;
using BoxPrint.Config;       //20220728 조숭진 config 방식 변경
using BoxPrint.Database;
using BoxPrint.DataList;
using BoxPrint.GUI.EventCollection;
using BoxPrint.Log;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace PLCProtocol
{
    public delegate void UnitDataChanged(eDataChangeProperty changeType, eDataChangeUnitType unitType, string unitName, object changeData);     //230103 HHJ SCS 개선

    public class ProtocolManager
    {
        private int PLC_CpuCount = 1;
        private bool UsePLCSimulMemory;
        private object LocalMemoryWriteLock = new object();
        private List<ProtocolBase> ProtocolList;
        public long DebugReadCount = 0;
        public long DebugWriteCount = 0;

        //public long DebugReadCountPerSec = 0;
        //public long DebugWriteCountPerSec = 0;

        public PLCMemoryMap PLCMap;


        //SuHwan_20221024 : [ServerClient]
        //디비에 있는 아이오 저장해놓는곳
        public ConcurrentDictionary<string, List<PLCDataInfo>> DBPIOInfo = new ConcurrentDictionary<string, List<PLCDataInfo>>();

        private List<PLCDataInfo> PLCDataInfos = new List<PLCDataInfo>();

        public readonly int IOPollingPeriod = 300; //0.2sec -> 0.3sec 

        //private int IOLogSeq = 0;
        private DateTime LastFullLogTime = DateTime.MinValue;

        public Thread cth;
        private bool threadExit = true;

        public ProtocolManager(PLCSection section)
        {
            if (section != null)
            {
                PLC_CpuCount = section.Count; //PLC 번호가 다른 모듈은 남은 영역에 할당한다.

            }
            PLCMap = new PLCMemoryMap();

            if (GlobalData.Current.ServerClientType == eServerClientType.Client)//SuHwan_20221024 : 추가
            {
                UsePLCSimulMemory = true;
                GlobalData.Current.OnUnitIODataReq += UnitIODataReqAction;
                cth = new Thread(new ThreadStart(UpdatePLCIOData_Client));
                cth.IsBackground = true;
                cth.Name = "PLCIOUpdate";
                cth.Start();
                return;
            }
            else // 서버
            {


                Thread th = new Thread(new ThreadStart(CollectPLCData));
                th.IsBackground = true;
                th.Name = "ProtocolManagerDebug";
                th.Start();

                UsePLCSimulMemory = section.PLCSimulMode;

                ProtocolList = new List<ProtocolBase>();

                //20220728 조숭진 config 방식 변경 s
                //foreach(PLCElement v in section.Plcs)
                //{
                //    ProtocolBase pbase = null;
                //    if (v.Series.Equals(ePLCSeries.MxCom))
                //        pbase = new Protocol_MxComponent(v);
                //    else if (v.Series.Equals(ePLCSeries.Q) || v.Series.Equals(ePLCSeries.R))
                //    {
                //        if (v.Frame.Equals(ePLCFrame.Frame_3E))
                //            pbase = new Protocol_3E(v);
                //        else if (v.Frame.Equals(ePLCFrame.Frame_4E))
                //            pbase = new Protocol_4E(v);
                //    }

                //    if (pbase != null)
                //        protocols.Add(pbase);
                //    else
                //    {
                //        //null이면?
                //    }
                //}
                for (int i = 0; i < section.Count; i++)
                {
                    ProtocolBase pbase = null;

                    //SuHwan_20221116
                    if (UsePLCSimulMemory == true)
                    {
                        pbase = new ProtocolBase(section[i]);
                        pbase.IsConnect = true;
                    }
                    else
                    {
                        if (section[i].Series.Equals(ePLCSeries.MxCom))
                            pbase = new Protocol_MxComponent(section[i]);
                        else if (section[i].Series.Equals(ePLCSeries.Q) || section[i].Series.Equals(ePLCSeries.R))
                        {
                            if (section[i].Frame.Equals(ePLCFrame.Frame_3E))
                                pbase = new Protocol_3E(section[i]);
                            else if (section[i].Frame.Equals(ePLCFrame.Frame_4E))
                                pbase = new Protocol_4E(section[i]);
                        }
                    }

                    if (pbase != null)
                    {
                        pbase.OnProtocolStateChange += OnProtocolStateChange;
                        ProtocolList.Add(pbase);
                    }
                }

                //Main PLC 쪽은 Backup Line 설정해놓는다.
                foreach(var protocol in ProtocolList)
                {
                    Protocol_MCProtocolBase MCP = protocol as Protocol_MCProtocolBase;
                    if (MCP != null && MCP.BackupProtocolNum >= 0) //Backup 프로토콜이 설정되어 있다면
                    {
                        ProtocolBase BackupMCP = ProtocolList.Where(p => p.PlcNum == MCP.BackupProtocolNum).FirstOrDefault();
                        MCP.SetBackupProtocol(BackupMCP); //설정한다.
                    }
                }
               

                //20220728 조숭진 config 방식 변경 e

                //230103 HHJ SCS 개선
                GlobalData.Current.OnUnitIODataReq += UnitIODataReqAction;

                UIEventCollection.Instance.OnRequestPlcState += OnRequestPlcState;
            }
        }

        private void OnProtocolStateChange(PLCStateData stateData)
        {
            UIEventCollection.Instance.InvokerPlcStateChanged(stateData);
        }

        private void OnRequestPlcState()
        {
            List<PLCStateData> stateDataList = new List<PLCStateData>();
            foreach(ProtocolBase v in ProtocolList)
            {
                stateDataList.Add(v.GetPlcStateData());
            }

            UIEventCollection.Instance.InvokerResponsePlcState(stateDataList.OrderBy(o => o.ConnectInfo).ToList());
        }

        //230103 HHJ SCS 개선
        //231108 HHJ SCS Playback 개선
        //private void UnitIODataReqAction(eDataChangeUnitType unitType, string unitKey)
        private void UnitIODataReqAction(eDataChangeUnitType unitType, string unitKey, bool isPlayback)
        {
            try
            {
                //230103 HHJ SCS 개선
                if (!isPlayback)
                    PLCMap.GetPLCUnitData(unitType, unitKey);
            }
            catch (Exception ex)
            {
                _ = ex;
            }
        }

        public virtual bool Connect()
        {
            foreach (ProtocolBase pbase in ProtocolList)
            {
                pbase.Connect();
            }

            return true;
        }
        public virtual void DisposeEvent() { }

        public void Close()
        {
            foreach (ProtocolBase pbase in ProtocolList)
            {
                int no = pbase.PlcNum + 1;
                PLCStateData newPLCState = pbase.GetPlcStateData();
                newPLCState.State = ePLCStateDataState.DisConnect;
                newPLCState.StateChangeTime = DateTime.Now;
                GlobalData.Current.DBManager.DbSetProcedurePLCInfo(newPLCState, no);
            }
        }

        public virtual bool CheckConnection(short PLCNumber)
        {
            ProtocolBase protocol = ProtocolList.Where(r => r.PlcNum == PLCNumber).FirstOrDefault();
            if (protocol != null)
            {
                return protocol.IsConnect;
            }
            else
            {
                return false;
            }
        }
        public virtual bool CheckALLPLCConnection()
        {
            if (UsePLCSimulMemory)
            {
                return true;
            }
            if (ProtocolList == null)
            {
                return false;
            }
            if (ProtocolList.Count == 0)
            {
                return false;
            }
            foreach (var pItem in ProtocolList)
            {
                if (pItem != null)
                {
                    if (!pItem.IsConnect)
                    {
                        return false;
                    }
                }
            }
            return true;

        }
        private void CollectPLCData()
        {
            int Cycle = 0;
            DateTime LastPBSnapTime = DateTime.MinValue; //마지막 스냅샷 시간 저장
            bool FirstReadEventSet = false;
            GlobalData.Current.MRE_GlobalDataCreatedEvent.WaitOne(); //모든 모듈 생성전까지 Run 대기
            while (true)
            {
                Thread.Sleep(IOPollingPeriod);
                //SuHwan_20221116 : [simul]

                if (Cycle % 25 == 0 && !UsePLCSimulMemory)  //연결이 해제되어있으면 25 사이클 마다 연결 시도
                {
                    //LogManager.WriteConsoleLog(eLogLevel.Info, "Debug Info PLC ReadCount: {0}/Sec   WriteCount: {1}/Sec  TotalRead:{2} TotalWrite:{3}",
                    //    DebugReadCountPerSec, DebugWriteCountPerSec, DebugReadCount, DebugWriteCount);
                    //DebugReadCountPerSec = 0;
                    //DebugWriteCountPerSec = 0;

                    bool CheckNGConnectionExist = false;
                    foreach (ProtocolBase pbase in ProtocolList)
                    {
                        if (pbase.IsConnect == false)
                        {
                            LogManager.WriteConsoleLog(eLogLevel.Info, "PLC Disconneted. Retry Connecting to  {0} : {1}", pbase.IP, pbase.Port);
                            if (pbase.Connect() == false) //접속 시도 했는데 실패
                            {
                                CheckNGConnectionExist = true; //연결에 뭔가 문제가 생겼음.
                                LogManager.WriteConsoleLog(eLogLevel.Info, "PLC Connection fail IP: {0} Port:{1}", pbase.IP, pbase.Port);
                                GlobalData.Current.Alarm_Manager.AlarmOccurbyName("PLC_CONNECTION_FAIL", GlobalData.Current.MainBooth.ModuleName);
                                Thread.Sleep(500);
                            }
                        }
                    }
                    //240809 PLC 인터페이스 알람 자동 클리어 추가.
                    if(!CheckNGConnectionExist && GlobalData.Current.Alarm_Manager.CheckAlarmExist()) //알람 아무것도 없는데 아래로직 체크할 필요는 없음.
                    {
                        AlarmData ConAlarm = GlobalData.Current.Alarm_Manager.GetActiveList().Where(a => a.AlarmName == "PLC_CONNECTION_FAIL").FirstOrDefault();
                        if (ConAlarm != null)
                        {
                            GlobalData.Current.Alarm_Manager.AlarmClear(ConAlarm);
                        }

                        AlarmData WriteAlarm = GlobalData.Current.Alarm_Manager.GetActiveList().Where(a => a.AlarmName == "PLC_WRITE_FAIL").FirstOrDefault();
                        if (WriteAlarm != null)
                        {
                            GlobalData.Current.Alarm_Manager.AlarmClear(WriteAlarm);
                        }

                        AlarmData ReadAlarm = GlobalData.Current.Alarm_Manager.GetActiveList().Where(a => a.AlarmName == "PLC_READ_FAIL").FirstOrDefault();
                        if (ReadAlarm != null)
                        {
                            GlobalData.Current.Alarm_Manager.AlarmClear(ReadAlarm);
                        }
                    }
                    Cycle = 0;
                }
                Cycle++;

                //Stopwatch stopwatch = new Stopwatch(); //시간측정 
                //stopwatch.Start(); 
                PLCDataItem pItem;

                //Booth 영역
                pItem = GlobalData.Current.MainBooth.PLCtoPC["PLC_Area_BatchRead"];
                //230103 HHJ SCS 개선
                //ReadFullRaw(pItem.PLCNum, pItem);
                ReadFullRaw(eDataChangeUnitType.eBooth, GlobalData.Current.EQPID, pItem.PLCNum, pItem);
                PLCLocalMemoryToDB(GlobalData.Current.EQPID, eAreaType.PLCtoPC, GlobalData.Current.MainBooth.PLCtoPC);      //221012 조숭진

                Thread.Sleep(20); //같은 CPU 연속 리드 부하 줄여본다.

                pItem = GlobalData.Current.MainBooth.PCtoPLC["PC_Area_BatchRead"];
                //230103 HHJ SCS 개선
                //ReadFullRaw(pItem.PLCNum, pItem);
                ReadFullRaw(eDataChangeUnitType.eBooth, GlobalData.Current.EQPID, pItem.PLCNum, pItem);
                PLCLocalMemoryToDB(GlobalData.Current.EQPID, eAreaType.PCtoPLC, GlobalData.Current.MainBooth.PCtoPLC);      //221012 조숭진

                Thread.Sleep(20); //같은 CPU 연속 리드 부하 줄여본다.

                //Crane 1  영역
                pItem = GlobalData.Current.mRMManager.FirstRM.PLCtoPC["PLC_Area_BatchRead"];
                //230103 HHJ SCS 개선
                //ReadFullRaw(pItem.PLCNum, pItem);
                ReadFullRaw(eDataChangeUnitType.eCrane, GlobalData.Current.mRMManager.FirstRM.ModuleName, pItem.PLCNum, pItem);
                PLCLocalMemoryToDB(GlobalData.Current.mRMManager.FirstRM.ModuleName, eAreaType.PLCtoPC, GlobalData.Current.mRMManager.FirstRM.PLCtoPC);     //221012 조숭진


                Thread.Sleep(20); //같은 CPU 연속 리드 부하 줄여본다.

                pItem = GlobalData.Current.mRMManager.FirstRM.PCtoPLC["PC_Area_BatchRead"];
                //230103 HHJ SCS 개선
                //ReadFullRaw(pItem.PLCNum, pItem);
                ReadFullRaw(eDataChangeUnitType.eCrane, GlobalData.Current.mRMManager.FirstRM.ModuleName, pItem.PLCNum, pItem);
                PLCLocalMemoryToDB(GlobalData.Current.mRMManager.FirstRM.ModuleName, eAreaType.PCtoPLC, GlobalData.Current.mRMManager.FirstRM.PCtoPLC);     //221012 조숭진


                Thread.Sleep(20); //같은 CPU 연속 리드 부하 줄여본다.

                //221012 조숭진 s
                //Crane 2 영역
                if (GlobalData.Current.SCSType == eSCSType.Dual)
                {
                    pItem = GlobalData.Current.mRMManager.SecondRM.PLCtoPC["PLC_Area_BatchRead"];
                    //230103 HHJ SCS 개선
                    //ReadFullRaw(pItem.PLCNum, pItem);
                    ReadFullRaw(eDataChangeUnitType.eCrane, GlobalData.Current.mRMManager.SecondRM.ModuleName, pItem.PLCNum, pItem);
                    PLCLocalMemoryToDB(GlobalData.Current.mRMManager.SecondRM.ModuleName, eAreaType.PLCtoPC, GlobalData.Current.mRMManager.SecondRM.PLCtoPC);


                    Thread.Sleep(20); //같은 CPU 연속 리드 부하 줄여본다.

                    pItem = GlobalData.Current.mRMManager.SecondRM.PCtoPLC["PC_Area_BatchRead"];
                    //230103 HHJ SCS 개선
                    //ReadFullRaw(pItem.PLCNum, pItem);
                    ReadFullRaw(eDataChangeUnitType.eCrane, GlobalData.Current.mRMManager.SecondRM.ModuleName, pItem.PLCNum, pItem);
                    PLCLocalMemoryToDB(GlobalData.Current.mRMManager.SecondRM.ModuleName, eAreaType.PCtoPLC, GlobalData.Current.mRMManager.SecondRM.PCtoPLC);     //221012 조숭진

                }
                //221012 조숭진 e

                //멀티 PLC 대응 개선 
                foreach(var cvItem in GlobalData.Current.PortManager.ReadPivotCVList) //프로그램 시작할때 직접 리스트를 생성해서 조회함
                {
                    //20230803 RGJ 멀티 포트 Read 대응추가
                    pItem = cvItem.PLCtoPC["Area_BatchRead"];
                    if (pItem != null)
                    {
                        ReadFullRaw(eDataChangeUnitType.ePort, "", pItem.PLCNum, pItem);       //C/V는 여기서 처리하지 않을것이기에 빈 값을 사용한다.
                    }

                    Thread.Sleep(20); //같은 CPU 연속 리드 부하 줄여본다.
                }

                #region 기존 로직 삭제 검증 완료후 완전삭제 예정
                //for (int PLCNumber = 0; PLCNumber < PLC_CpuCount; PLCNumber++)
                //{
                //    //CV 영역 
                //    pItem = GlobalData.Current.PortManager.GetFirstPLCCVModule(PLCNumber)?.PLCtoPC["Area_BatchRead"]; //각 PLC Cpu 별 가장 앞 주소 CV 를 찾는다.
                //    var LastCVItem = GlobalData.Current.PortManager.GetLastPLCCVModule(PLCNumber);
                //    //20230803 RGJ 한 CPU 당 16PORT 이상 대응위해 추가.
                //    if (pItem != null)
                //    {
                //        //230103 HHJ SCS 개선
                //        //ReadFullRaw(pItem.PLCNum, pItem);
                //        ReadFullRaw(eDataChangeUnitType.ePort, "", pItem.PLCNum, pItem);       //C/V는 여기서 처리하지 않을것이기에 빈 값을 사용한다.
                //    }

                //}
                #endregion

                //221012 조숭진 s
                foreach (var item in GlobalData.Current.PortManager.AllCVList)
                {
                    //240718 RGJ 포트 비사용 상태라도 I/O 업데이트 해야함.
                    //if (item.CVUSE == false) //item.CVModuleType == eCVType.WaterPool ||
                    //    continue;

                    PLCMap.UpdatePLCUnitData(eDataChangeUnitType.ePort, item.ModuleName, eAreaType.PLCtoPC, item.PLCtoPC, false);
                    PLCMap.UpdatePLCUnitData(eDataChangeUnitType.ePort, item.ModuleName, eAreaType.PCtoPLC, item.PCtoPLC, false);

                    PLCLocalMemoryToDB(item.ModuleName, eAreaType.PLCtoPC, item.PLCtoPC);
                    PLCLocalMemoryToDB(item.ModuleName, eAreaType.PCtoPLC, item.PCtoPLC);
                }
                //221012 조숭진 e

                //stopwatch.Stop(); //시간측정 끝
                //LogManager.WriteConsoleLog(eLogLevel.Info, "PLC Batch Read 수행시간 : {0} ms ", stopwatch.ElapsedMilliseconds); //테스트 결과 평균적으로 50 ~ 70 ms 걸림
                
                if(FirstReadEventSet == false) //처음 대기하고 있는 포트 쓰레드 대기를 풀어준다.
                {
                    FirstReadEventSet = true;
                    GlobalData.Current.MRE_FirstPLCReadEvent.Set();
                }

            }
        }

        public void MapChangeForExitThread()
        {
            threadExit = false;
        }

        private void UpdatePLCIOData_Client()
        {
            GlobalData.Current.MRE_GlobalDataCreatedEvent.WaitOne(); //모든 모듈 생성전까지 Run 대기
            GlobalData.Current.MRE_MapViewChangeEvent.WaitOne();
            while (threadExit)
            {
                //if (GlobalData.Current.MapViewStart)
                //{
                //    cth.Join();
                //    //cth = null;
                //    //return;
                //}
                Thread.Sleep(IOPollingPeriod);

                PLCMap.UpdatePLCUnitData(eDataChangeUnitType.eBooth, GlobalData.Current.EQPID, eAreaType.PCtoPLC, GlobalData.Current.MainBooth.PCtoPLC, true);
                PLCMap.UpdatePLCUnitData(eDataChangeUnitType.eBooth, GlobalData.Current.EQPID, eAreaType.PLCtoPC, GlobalData.Current.MainBooth.PLCtoPC, true);

                foreach( var rItem in GlobalData.Current.mRMManager.ModuleList)
                {
                    PLCMap.UpdatePLCUnitData(eDataChangeUnitType.eCrane, rItem.Value.ModuleName, eAreaType.PLCtoPC, rItem.Value.PLCtoPC, true);
                    PLCMap.UpdatePLCUnitData(eDataChangeUnitType.eCrane, rItem.Value.ModuleName, eAreaType.PCtoPLC, rItem.Value.PCtoPLC, true);
                }

                //SuHwan_20221116 : [simul]
                //221012 조숭진 s
                foreach (var item in GlobalData.Current.PortManager.AllCVList)
                {
                    //if (item.CVUSE == false) //240718 RGJ 포트 비사용 상태라도 I/O 업데이트 해야함.
                    //    continue;

                    PLCMap.UpdatePLCUnitData(eDataChangeUnitType.ePort, item.ModuleName, eAreaType.PLCtoPC, item.PLCtoPC, true);
                    PLCMap.UpdatePLCUnitData(eDataChangeUnitType.ePort, item.ModuleName, eAreaType.PCtoPLC, item.PCtoPLC, true);
                }
            }
            //cth.Join(); //241121 RGJ 자기가 돌던 쓰레드 조인 기다리는것 무의미 하므로 삭제.
        }

        //230103 HHJ SCS 개선
        //public virtual bool ReadFullRaw(short PLCNumber, PLCDataItem pItem)
        public virtual bool ReadFullRaw(eDataChangeUnitType unitType, string unitName, short PLCNumber, PLCDataItem pItem)
        {
            //DebugReadCountPerSec++;
            //DebugReadCount++;
            try
            {

                if (UsePLCSimulMemory) //PLC 시뮬 메모리 모드일경우
                {
                    byte[] ReadBytes = PLCMap.ReadRawMemoryBuffer(pItem);

                    //Write_PlayBackLog(ReadBytes, 0, PLCMap.GetMamoryMapAddress(pItem), pItem.Size * 2); //시뮬모드이면 플레이백 로그 그냥 찍는다.
                    //Write_PlayBackLog(unitType, unitName, pItem.Area, ReadBytes);
                    //230103 HHJ SCS 개선
                    //PLCMap.UpdatePLCUnitData(unitType, unitName, pItem.Area, ReadBytes, false, PLCMap.GetMamoryMapAddress(pItem));       //230103 HHJ SCS 개선
                    PLCMap.UpdatePLCUnitData(unitType, unitName, pItem.Area, ReadBytes, false, PLCMap.GetMamoryMapAddress(pItem), false);       //230103 HHJ SCS 개선
                    return true;
                }
                else
                {
                    ProtocolBase protocol = ProtocolList.Where(r => r.PlcNum.Equals(PLCNumber)).FirstOrDefault();

                    #region 임시 테스트 로직
                    if (GlobalData.Current.DebugUseBackupPLC && protocol.IsBackupProtocolExist())
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "PLC READ FAIL Full Raw  retry BackupLIne :{0} ", protocol.GetBackupProtocol().GetPlcStateData().PLCName);
                        if (protocol.GetBackupProtocol().Read(pItem, out byte[] BackupRead, out byte BackupDebugErrorCode))
                        {
                            LogManager.WriteConsoleLog(eLogLevel.Info, "PLC READ FAIL Full Raw  BackupSuccess!");
                            byte[] ReadBytes = (byte[])protocol.ReadValueConvert(pItem, BackupRead);
                            //내부 메모리에 업데이트
                            ReadBytes.CopyTo(PLCMap.GetBuffer(), PLCMap.GetMamoryMapAddress(pItem));
                            PLCMap.UpdatePLCUnitData(unitType, unitName, pItem.Area, ReadBytes, false, PLCMap.GetMamoryMapAddress(pItem), false);       //230103 HHJ SCS 개선
                            return true;
                        }
                        else //BackupLine 도 실패했다...
                        {
                            LogManager.WriteConsoleLog(eLogLevel.Info, "PLC READ FAIL Full Raw  BackupFailed! {0} FailCode:{1}", pItem.ItemName, BackupDebugErrorCode);
                        }
                    }
                    #endregion

                    //if (protocol.IsConnect == false) //Read 에 체크 들어가므로 중복 삭제.
                    //{
                    //    return false;
                    //}
                    if (protocol.Read(pItem, out byte[] read, out byte DebugErrorCode))
                    {
                        byte[] ReadBytes = (byte[])protocol.ReadValueConvert(pItem, read);
                        //PlayBack Data I/O 변경점 로그부터 찍는다.
                        //CompareProcess(ReadBytes, PLCMap.GetBuffer(), pItem.ItemPLCAddress * 2);

                        //내부 메모리에 업데이트
                        ReadBytes.CopyTo(PLCMap.GetBuffer(), PLCMap.GetMamoryMapAddress(pItem));

                        //Write_PlayBackLog(unitType, unitName, pItem.Area, ReadBytes);
                        //230103 HHJ SCS 개선
                        //PLCMap.UpdatePLCUnitData(unitType, unitName, pItem.Area, ReadBytes, false, PLCMap.GetMamoryMapAddress(pItem));       //230103 HHJ SCS 개선
                        PLCMap.UpdatePLCUnitData(unitType, unitName, pItem.Area, ReadBytes, false, PLCMap.GetMamoryMapAddress(pItem), false);       //230103 HHJ SCS 개선
                        return true;
                    }
                    else
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "PLC READ FAIL Full Raw  {0} FailCode:{1}", pItem.ItemName, DebugErrorCode);
                        //실패할경우 BackupLine 이 있으면 리트라이 해본다.
                        if(protocol.IsBackupProtocolExist())
                        {
                            LogManager.WriteConsoleLog(eLogLevel.Info, "PLC READ FAIL Full Raw  retry BackupLIne :{0} ", protocol.GetBackupProtocol().GetPlcStateData().PLCName) ;
                            if (protocol.GetBackupProtocol().Read(pItem, out byte[] BackupRead, out byte BackupDebugErrorCode))
                            {
                                LogManager.WriteConsoleLog(eLogLevel.Info, "PLC READ FAIL Full Raw  BackupSuccess!");
                                byte[] ReadBytes = (byte[])protocol.ReadValueConvert(pItem, BackupRead);
                                //내부 메모리에 업데이트
                                ReadBytes.CopyTo(PLCMap.GetBuffer(), PLCMap.GetMamoryMapAddress(pItem));
                                PLCMap.UpdatePLCUnitData(unitType, unitName, pItem.Area, ReadBytes, false, PLCMap.GetMamoryMapAddress(pItem), false);       //230103 HHJ SCS 개선
                                return true;
                            }
                            else //BackupLine 도 실패했다...
                            {
                                LogManager.WriteConsoleLog(eLogLevel.Info, "PLC READ FAIL Full Raw  BackupFailed! {0} FailCode:{1}", pItem.ItemName, BackupDebugErrorCode);
                            }

                        }
                        return false;
                    }
                }


            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
                return false;
            }
        }

        /// <summary>
        /// PLC Memory Map 과 PLC 에서 읽어온 값비교 값 변경시 일정길이로 잘라서 플레이백에 저장  
        /// </summary>
        /// <param name="newData"></param>
        /// <param name="IOMap"></param>
        /// <param name="MapStartIndex"></param>
        //public void CompareProcess(byte[] newData, byte[] IOMap, int MapStartIndex)
        //{
        //    //int CompareIndex = 0;
        //    int CompareSize = 100; //100byte 씩 비교해본다.

        //    int CompareCount = 0;
        //    int ZZaturiSize = 0; //일정 크기로 자르고 남은 짜투리
        //    CompareResult Result = CompareResult.UnvalidCondition;

        //    if (newData != null && IOMap != null && IOMap.Length >= newData.Length + MapStartIndex) //초기 조건 체크
        //    {
        //        CompareCount = newData.Length / CompareSize;
        //        ZZaturiSize = newData.Length % CompareSize;

        //        for (int i = 0; i < CompareCount; i++)
        //        {
        //            Result = CompareByteArray(newData, PLCMap.GetBuffer(), i * CompareSize, MapStartIndex + i * CompareSize, CompareSize);
        //            if (Result == CompareResult.CompareMismatch)
        //            {
        //                Write_PlayBackLog(newData, i * CompareSize, MapStartIndex + i * CompareSize, CompareSize);
        //            }
        //        }
        //        //혹시 짜투리가 있으면
        //        if (ZZaturiSize > 0)
        //        {
        //            Result = CompareByteArray(newData, PLCMap.GetBuffer(), CompareCount * CompareSize, MapStartIndex + CompareCount * CompareSize, ZZaturiSize);
        //            if (Result == CompareResult.CompareMismatch)
        //            {
        //                Write_PlayBackLog(newData, CompareCount * CompareSize, MapStartIndex + CompareCount * CompareSize, ZZaturiSize);
        //            }
        //        }
        //    }
        //}
        private CompareResult CompareByteArray(byte[] Source, byte[] Dest, int SouceIndex, int DestIndex, int CompareLength)
        {
            if (Source == null || Dest == null)
            {
                return CompareResult.UnvalidCondition;
            }
            if (Source.Length < SouceIndex + CompareLength)
            {
                return CompareResult.UnvalidCondition;
            }
            if (Dest.Length < DestIndex + CompareLength)
            {
                return CompareResult.UnvalidCondition;
            }
            for (int i = 0; i < CompareLength; i++)
            {
                if (Source[SouceIndex + i] != Dest[DestIndex + i])
                {
                    return CompareResult.CompareMismatch;
                }
            }
            return CompareResult.CompareMatch;
        }
        public enum CompareResult
        {
            CompareMatch,
            CompareMismatch,
            UnvalidCondition
        }

        //비사용 함수 주석처리.
        //public virtual object ReadProtocol(string ModuleName, ConcurrentDictionary<string, PLCDataItem> items, string key)
        //{
        //    //DebugReadCount++;
        //    //DebugReadCountPerSec++;
        //    byte DebugErrorCode = 0;
        //    //Console.WriteLine(string.Format("{0} Read Call", name));
        //    if (!items.ContainsKey(key)) //해당 키 값이 없음.
        //    {
        //        LogManager.WriteConsoleLog(eLogLevel.Info, string.Format("PLC READ FAIL {0} {1}  is not registered", ModuleName, key));
        //        if (!GlobalData.Current.GlobalSimulMode)
        //        {
        //            //GlobalData.Current.Alarm_Manager.AlarmOccurbyName("PLC_READ_FAIL", "BOOTH");
        //        }
        //        throw new Exception(string.Format("{0} {1} is not registered", ModuleName, key));
        //    }

        //    if (items.TryGetValue(key, out PLCDataItem pItem))
        //    {
        //        ProtocolBase protocol = protocols.Where(r => r.PlcNum.Equals(pItem.PLCNum)).FirstOrDefault();

        //        if (protocol.Read(pItem, out byte[] read, out DebugErrorCode))
        //        {
        //            //Console.WriteLine(string.Format("{0} Read Complete", name));
        //            return protocol.ReadValueConvert(pItem, read);
        //        }
        //        else //값 PLC Read 실패
        //        {
        //            //실패시 리트라이 해본다.
        //            if (protocol.Read(pItem, out byte[] readRetry, out DebugErrorCode))
        //            {
        //                //Console.WriteLine(string.Format("{0} Read Complete", name));
        //                return protocol.ReadValueConvert(pItem, readRetry);
        //            }
        //            LogManager.WriteConsoleLog(eLogLevel.Info, string.Format("PLC READ FAIL {0} {1} GetProtocol FailCode:{2}", ModuleName, key, DebugErrorCode));
        //            if (!GlobalData.Current.GlobalSimulMode)
        //            {
        //                //GlobalData.Current.Alarm_Manager.AlarmOccurbyName("PLC_READ_FAIL", "BOOTH");
        //            }
        //            throw new Exception(string.Format("{0} {1} GetProtocol FailCode:{2}", ModuleName, key, DebugErrorCode));
        //        }
        //    }
        //    else //해당 키 획득 실패
        //    {
        //        LogManager.WriteConsoleLog(eLogLevel.Info, string.Format("PLC READ FAIL {0} {1} GetValue Fail", ModuleName, key));
        //        if (!GlobalData.Current.GlobalSimulMode)
        //        {
        //            //GlobalData.Current.Alarm_Manager.AlarmOccurbyName("PLC_READ_FAIL", "BOOTH");
        //        }
        //        throw new Exception(string.Format("{0} {1} GetValue Fail", ModuleName, key));
        //    }
        //}

        public virtual bool ReadBit(string ModuleName, ConcurrentDictionary<string, PLCDataItem> items, string key)
        {
            try
            {
                if (items.ContainsKey(key))
                {
                    PLCDataItem p = items[key];
                    bool Value = PLCMap.ReadBit(p);
                    return Value;
                }
                else
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "PLC ReadBit Failed Key : {0} Not Existed.", key);
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "PLC ReadBit Exception Occurred! \r\n{0}", ex.ToString());
                return false;
            }
        }
        public virtual short ReadShort(string ModuleName, ConcurrentDictionary<string, PLCDataItem> items, string key)
        {
            try
            {
                if (items.ContainsKey(key))
                {
                    PLCDataItem p = items[key];
                    return PLCMap.ReadShort(p);
                }
                else
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "PLC ReadShort Failed Key : {0} Not Existed.", key);
                    return 0;
                }
            }
            catch(Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "PLC ReadShort Exception Occurred! \r\n{0}",ex.ToString());
                return 0;
            }
        }

        public virtual int ReadInt32(string ModuleName, ConcurrentDictionary<string, PLCDataItem> items, string key)
        {
            try
            {
                if (items.ContainsKey(key))
                {
                    PLCDataItem p = items[key];
                    return PLCMap.ReadInt32(p);
                }
                else
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "PLC ReadInt32 Failed Key : {0} Not Existed.", key);
                    return 0;
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "PLC ReadInt32 Exception Occurred! \r\n{0}", ex.ToString());
                return 0;
            }
        }

        public virtual string ReadString(string ModuleName, ConcurrentDictionary<string, PLCDataItem> items, string key)
        {
            try
            {
                if (items.ContainsKey(key))
                {
                    PLCDataItem p = items[key];
                    return PLCMap.ReadString(p);
                }
                else
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "PLC ReadString Failed Key : {0} Not Existed.", key);
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "PLC ReadString Exception Occurred! \r\n{0}", ex.ToString());
                return string.Empty;
            }
        }

        public virtual bool Write(string ModuleName, ConcurrentDictionary<string, PLCDataItem> items, string key, object value)
        {
            //DebugWriteCount++;
            //DebugWriteCountPerSec++;
            if (!items.ContainsKey(key))
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, string.Format("PLC WRITE FAIL {0} {1} is not registered", ModuleName, key));
                if (!GlobalData.Current.GlobalSimulMode)
                {
                    GlobalData.Current.Alarm_Manager.AlarmOccurbyName("PLC_WRITE_FAIL", GlobalData.Current.MainBooth.ModuleName);
                }
                return false;
            }

            if (items.TryGetValue(key, out PLCDataItem pItem))
            {
                if (value is string && pItem.DataType != eDataType.String)
                {
                    switch (pItem.DataType)
                    {
                        case eDataType.Bool:
                            if ((string)value == "1")
                            {
                                value = true;
                            }
                            else if ((string)value == "0")
                            {
                                value = false;
                            }
                            else
                            {
                                return false;
                            }
                            break;
                        case eDataType.Short:
                            short sValue = 0;
                            if (short.TryParse(value.ToString(), out sValue))
                            {
                                value = sValue;
                            }
                            else
                            {
                                return false;
                            }
                            break;
                    }
                }
                if (UsePLCSimulMemory) //로컬 메모리에 Write
                {
                    lock (LocalMemoryWriteLock)
                    {
                        PLCMap.Write(ModuleName, items, key, value);
                    }
                    return true;
                }
                else //PLC 에 Write
                {
                    ProtocolBase protocol = ProtocolList.Where(r => r.PlcNum.Equals(pItem.PLCNum)).FirstOrDefault();

                    if(protocol == null)
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, string.Format("PLC WRITE FAIL {0} {1} address : {2} Protocol is Null", ModuleName, key, pItem.ItemPLCAddress));
                        GlobalData.Current.Alarm_Manager.AlarmOccurbyName("PLC_WRITE_FAIL", GlobalData.Current.MainBooth.ModuleName);
                        return false;
                    }
                    //else if(protocol.IsConnect == false) //이미 Write 함수에 체크 있으므로 생략
                    //{
                    //    Thread.Sleep(500);
                    //    if (protocol.IsConnect == false) //접속 체크 다시 한번 시도
                    //    {
                    //        LogManager.WriteConsoleLog(eLogLevel.Info, string.Format("PLC WRITE FAIL {0} {1} address : {2} Protocol is  Disconnected", ModuleName, key, pItem.ItemPLCAddress));
                    //        return false;
                    //    }
                    //}

                    int retryCount = 0;
                    byte ErrorCode = 0;
                    while(retryCount < 5)
                    {
                        #region 임시 테스트 로직
                        if (GlobalData.Current.DebugUseBackupPLC && protocol.IsBackupProtocolExist())
                        {
                            LogManager.WriteConsoleLog(eLogLevel.Info, "PLC WRITE FAIL retry BackupLIne :{0} ", protocol.GetBackupProtocol().GetPlcStateData().PLCName);
                            if (protocol.GetBackupProtocol().Write(pItem, value, out byte BackupDebugErrorCode))
                            {
                                LogManager.WriteConsoleLog(eLogLevel.Info, "PLC WRITE retry Success BackupLIne :{0} ", protocol.GetBackupProtocol().GetPlcStateData().PLCName);
                                return true;
                            }
                            else //BackupLine 도 실패했다...
                            {
                                LogManager.WriteConsoleLog(eLogLevel.Info, "PLC WRITE FAIL BackupFailed! {0} FailCode:{1}", pItem.ItemName, BackupDebugErrorCode);
                                return false;
                            }
                        }
                        #endregion


                        if (protocol.Write(pItem, value, out ErrorCode))
                        {
                            return true;
                        }
                        else //실패시 리트라이한다.
                        {
                            //실패할경우 BackupLine 이 있으면 리트라이 해본다.
                            if (protocol.IsBackupProtocolExist())
                            {
                                LogManager.WriteConsoleLog(eLogLevel.Info, "PLC WRITE FAIL retry BackupLIne :{0} ", protocol.GetBackupProtocol().GetPlcStateData().PLCName);
                                if (protocol.GetBackupProtocol().Write(pItem, value, out byte BackupDebugErrorCode))
                                {
                                    LogManager.WriteConsoleLog(eLogLevel.Info, "PLC WRITE retry Success BackupLIne :{0} ", protocol.GetBackupProtocol().GetPlcStateData().PLCName);
                                    return true;
                                }
                                else //BackupLine 도 실패했다...
                                {
                                    LogManager.WriteConsoleLog(eLogLevel.Info, "PLC WRITE FAIL BackupFailed! {0} FailCode:{1}", pItem.ItemName, BackupDebugErrorCode);
                                    Thread.Sleep(200);
                                    retryCount++;
                                }
                            }
                            else
                            {
                                LogManager.WriteConsoleLog(eLogLevel.Info, string.Format("PLC WRITE FAIL {0} {1} address : {2} WriteFailed Retry :{3} Ecode:{4} ", ModuleName, key, pItem.ItemPLCAddress, retryCount, ErrorCode));
                                retryCount++;
                                Thread.Sleep(200);
                            }
                        }
                    }
                    //리트라이 해도 실패했다...
                    LogManager.WriteConsoleLog(eLogLevel.Info, string.Format("PLC WRITE FAIL {0} {1} address : {2} WriteDeviceBlock Fail", ModuleName, key, pItem.ItemPLCAddress));
                    GlobalData.Current.Alarm_Manager.AlarmOccurbyName("PLC_WRITE_FAIL", GlobalData.Current.MainBooth.ModuleName);
                    return false;
                }
            }
            else
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, string.Format("{0} {1} GetValue Fail", ModuleName, key));
                if (!GlobalData.Current.GlobalSimulMode)
                {
                    GlobalData.Current.Alarm_Manager.AlarmOccurbyName("PLC_WRITE_FAIL", GlobalData.Current.MainBooth.ModuleName);
                }
                return false;
            }
        }

        public bool Write_LocalMemory(string ModuleName, ConcurrentDictionary<string, PLCDataItem> items, string key, object value)
        {
            //DebugWriteCount++;
            //DebugWriteCountPerSec++;
            if (!items.ContainsKey(key))
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, string.Format("PLC WRITE FAIL {0} {1} is not registered", ModuleName, key));
                if (!GlobalData.Current.GlobalSimulMode)
                {
                    GlobalData.Current.Alarm_Manager.AlarmOccurbyName("PLC_WRITE_FAIL", GlobalData.Current.MainBooth.ModuleName);
                }
                return false;
            }

            if (items.TryGetValue(key, out PLCDataItem pItem))
            {
                if (value is string && pItem.DataType != eDataType.String)
                {
                    switch (pItem.DataType)
                    {
                        case eDataType.Bool:
                            if ((string)value == "1")
                            {
                                value = true;
                            }
                            else if ((string)value == "0")
                            {
                                value = false;
                            }
                            else
                            {
                                return false;
                            }
                            break;
                        case eDataType.Short:
                            short sValue = 0;
                            if (short.TryParse(value.ToString(), out sValue))
                            {
                                value = sValue;
                            }
                            else
                            {
                                return false;
                            }
                            break;
                    }
                }
                if (UsePLCSimulMemory) //로컬 메모리에 Write
                {
                    lock (LocalMemoryWriteLock)
                    {
                        int ByteOffset = PLCMap.GetMamoryMapAddress(pItem);
                        switch (pItem.DataType)
                        {
                            case eDataType.Bool:
                                if (pItem.BitOffset < 8)
                                {
                                    byte MValue = PLCMap.GetBuffer()[ByteOffset];
                                    if ((bool)value == true)
                                    {
                                        MValue = (byte)(MValue | (byte)(0x01 << pItem.BitOffset));
                                    }
                                    else
                                    {
                                        MValue = (byte)(MValue & ~(byte)(0x01 << pItem.BitOffset));
                                    }

                                    PLCMap.GetBuffer()[ByteOffset] = MValue;
                                }
                                else //bit 8이상
                                {
                                    byte MValue = PLCMap.GetBuffer()[ByteOffset + 1];
                                    if ((bool)value == true)
                                    {
                                        MValue = (byte)(MValue | (byte)(0x01 << pItem.BitOffset - 8));
                                    }
                                    else
                                    {
                                        MValue = (byte)(MValue & ~(byte)(0x01 << pItem.BitOffset - 8));
                                    }
                                    PLCMap.GetBuffer()[ByteOffset + 1] = MValue;
                                }
                                break;
                            case eDataType.Short:
                                short sValue = Convert.ToInt16(value);
                                PLCMap.GetBuffer()[ByteOffset] = (byte)(sValue % 256);  //하위비트
                                PLCMap.GetBuffer()[ByteOffset + 1] = (byte)(sValue >> 8); //상위비트
                                break;
                                //230405 int32 추가
                            case eDataType.Int32:
                                int iValue = Convert.ToInt32(value);
                                PLCMap.GetBuffer()[ByteOffset] = (byte)(iValue % 256);  //0 바이트
                                PLCMap.GetBuffer()[ByteOffset + 1] = (byte)((iValue >> 8) % 256); //1 바이트
                                PLCMap.GetBuffer()[ByteOffset + 2] = (byte)((iValue >> 16) % 256); //2 바이트
                                PLCMap.GetBuffer()[ByteOffset + 3] = (byte)((iValue >> 24) % 256); //3 바이트
                                break;
                            case eDataType.String:
                                string PadedString = ((string)value).PadRight(pItem.Size * 2, '\0');//뒤에 공간은 공백으로 채워서 보내야 한다. 매 초기화가 아닌 오버라이팅 방식이기에 자릿수가 달라지면 이전 쓰레기값이 있을 수 있음.
                                byte[] Encoded = Encoding.Default.GetBytes(PadedString);
                                Array.Copy(Encoded, 0, PLCMap.GetBuffer(), ByteOffset, Encoded.Length);
                                break;
                            default:
                                break;
                        }
                    }
                    return true;
                }

                return false;

            }
            else
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, string.Format("{0} {1} GetValue Fail", ModuleName, key));
                if (!GlobalData.Current.GlobalSimulMode)
                {
                    GlobalData.Current.Alarm_Manager.AlarmOccurbyName("PLC_WRITE_FAIL", GlobalData.Current.MainBooth.ModuleName);
                }
                return false;
            }
        }

        //221012 조숭진 pio db s
        private void PLCLocalMemoryToDB(string moduleID, eAreaType areaType, ConcurrentDictionary<string, PLCDataItem> dataitem)
        {
            string tempdata = string.Empty;

            string logtempData = string.Empty;      //241024 HoN PIO Log 기재방식 변경

            foreach (KeyValuePair<string, PLCDataItem> item in dataitem.OrderBy(s => s.Value.AddressOffset).ThenBy(s => s.Value.BitOffset))
            {
                if (item.Key.Contains("BatchRead"))
                    continue;

                object readdata = new object();

                switch (item.Value.DataType)
                {
                    case eDataType.Bool:
                        //if (item.Value.BitOffset < 8)
                        //{
                        //    byte MValue = PLCMap.GetBuffer()[item.Value.ItemPLCAddress * 2];
                        //    MValue = (byte)(MValue >> item.Value.BitOffset);
                        //    readdata = MValue % 2 == 1;
                        //    readdata = readdata.ToString().Equals("True") ? 1 : 0;
                        //}
                        //else //bit 8이상
                        //{
                        //    byte MValue = PLCMap.GetBuffer()[item.Value.ItemPLCAddress * 2 + 1];
                        //    MValue = (byte)(MValue >> (item.Value.BitOffset - 8));
                        //    readdata = MValue % 2 == 1;
                        //    readdata = readdata.ToString().Equals("True") ? 1 : 0;
                        //}
                        readdata = PLCMap.ReadBit(item.Value) ? 1 : 0; //20230127 RGJ PLC Map 구조 변경
                        break;

                    case eDataType.Short:
                        //byte L_Value = PLCMap.GetBuffer()[item.Value.ItemPLCAddress * 2];    //하위비트
                        //byte U_Value = PLCMap.GetBuffer()[item.Value.ItemPLCAddress * 2 + 1];//상위비트
                        //readdata = (short)((U_Value << 8) + L_Value);

                        readdata = PLCMap.ReadShort(item.Value); //20230127 RGJ PLC Map 구조 변경
                        break;

                    case eDataType.Int32:
                        readdata = PLCMap.ReadInt32(item.Value); //20230127 RGJ PLC Map 구조 변경
                        break;


                    case eDataType.String:
                        //try
                        //{
                        //    byte[] TempArr = new byte[item.Value.Size * 2];
                        //    readdata = Encoding.Default.GetString(PLCMap.GetBuffer(), item.Value.ItemPLCAddress * 2, item.Value.Size * 2);
                        //    //return (string)Read(ModuleName, items, key);
                        //}
                        //catch
                        //{
                        //    readdata = string.Empty;
                        //}
                        readdata = PLCMap.ReadString(item.Value); //20230127 RGJ PLC Map 구조 변경
                        break;

                    default:
                        break;
                }

                //241001 HoN PIO Log 개선
                tempdata += readdata.ToString() + "/";
                //tempdata += $"{item.Value.ItemName}[{readdata}]/";    //Client도 같이 수정 필요
                logtempData += $"{item.Value.ItemName}[{readdata}]/";   //241024 HoN PIO Log 기재방식 변경
            }

            //241024 HoN PIO Log 기재방식 변경
            //string logtempdata = moduleID + "," + areaType.ToString() + "," + tempdata;
            string logtempdata = moduleID + "," + areaType.ToString() + "," + logtempData;

            int dataindex = PLCDataInfos.FindIndex(p => p.ModuleID == moduleID && p.Direction == areaType);

            if (dataindex == -1)
            {
                PLCDataInfos.Add(new PLCDataInfo
                {
                    ModuleID = moduleID,
                    Direction = areaType,
                    PLCData = tempdata
                });

                GlobalData.Current.DBManager.DbSetProcedurePIOInfo(moduleID, areaType, tempdata);

                LogManager.WritePIOLog(eLogLevel.Info, logtempdata);
            }
            else
            {
                if (PLCDataInfos[dataindex].PLCData != tempdata)
                {
                    PLCDataInfos[dataindex].PLCData = tempdata;
                    GlobalData.Current.DBManager.DbSetProcedurePIOInfo(moduleID, areaType, tempdata);
                    LogManager.WritePIOLog(eLogLevel.Info, logtempdata);
                }
            }
        }
        //221012 조숭진 pio db e

        //SuHwan_20221101 : [ServerClient]
        public virtual void GetPLCDataInfoFromDB() { }
        public virtual void ConvertDictionaryPLCDataInfo(string rcvModuleID, ConcurrentDictionary<string, PLCDataItem> rcvDirection, List<PLCDataInfo> rcvListPLCDataInfo) { }
    }
}
