using BoxPrint.Config;
using BoxPrint.Log;
using System;
using System.Collections.Generic;
using System.Threading;

namespace BoxPrint.Modules.RM
{
    public class RMManager
    {
        private GlobalData mGdata = null;
        private Dictionary<string, RMModuleBase> _RMList;

        public object SimulLock = new object();

        private int CollectDelay = 1000;
        private int UtilCollectDelay = 100;

        #region 속성들 정의
        public Dictionary<string, RMModuleBase> ModuleList
        {
            get { return _RMList; }
            private set { }
        }

        public RMModuleBase this[string moduleID]
        {
            get
            {
                if (moduleID == "RM1")
                {
                    return FirstRM;
                }
                else if (moduleID == "RM2")
                {
                    return SecondRM;
                }
                else
                {
                    if (_RMList.ContainsKey(moduleID)) //222.10.24 키값 존재 체크 추가.
                    {
                        return _RMList[moduleID];
                    }
                    else
                    {
                        return null;
                    }
                }
            }
        }
        public RMModuleBase this[int RMNumber]
        {
            get
            {
                foreach (var rm in _RMList)
                {
                    if (rm.Value.RMNumber == RMNumber)
                        return rm.Value;
                }
                return null;
            }
        }

        public RMModuleBase FirstRM
        {
            get
            {
                return this[1];
            }
        }
        public RMModuleBase SecondRM
        {
            get
            {
                return this[2];
            }
        }

        public bool FirstRMCarrierExist
        {
            get
            {
                if (FirstRM != null)
                {
                    return FirstRM.CarrierExistSensor;
                }
                else
                {
                    return false;
                }
            }
        }

        public bool SecondRMCarrierExist
        {
            get
            {
                if (SecondRM != null)
                {
                    return SecondRM.CarrierExistSensor;
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// 240809 RGJ 메인화면 좌상단 캐리어 카운트 조회 업데이트 통합
        /// </summary>
        public int TotalCarrierInCrane
        {
            get
            {
                int Count = 0;

                if(FirstRMCarrierExist && FirstRM.InSlotCarrier != null)
                {
                    Count++;
                }
                if (SecondRMCarrierExist && SecondRM.InSlotCarrier != null)
                {
                    Count++;
                }
                return Count;
            }
        }
        #endregion

        /// <summary>
        /// RMManager 생성자
        /// </summary>


        /// <summary>
        /// Manager 생성자
        /// </summary>
        //public RMManager(RMConfigSection RMSection)       //20220728 조숭진 config 방식 변경
        public RMManager(RMSection RMSection)
        {
            mGdata = GlobalData.Current;
            _RMList = new Dictionary<string, RMModuleBase>();

            InitializeRMManager(RMSection);
        }

        //SuHwan_20220930 : [ServerClient]
        public virtual void InitializeRMManager(RMSection RMSection)
        {
            //SuHwan_20221027 : [ServerClient]
            if (GlobalData.Current.ServerClientType == eServerClientType.Client)
            {
                //RM1 생성
                if (RMSection.RM1Element.RMType == eRMType.TPLC)
                {
                    RM_TPLC RM1 = new RM_TPLC(RMSection.RM1Element.ModuleName, false, RMSection.RM1Element.RMType, 1, false);
                    RM1.SetCranePLCID(RMSection.RM1Element.CraneID);
                    RM1.SetAutoSafetySpeed(RMSection.RM1Element.DoorUnlockSpeed, RMSection.RM1Element.AutoStartSpeed);
                    RM1.InitPLCInterface(RMSection.RM1Element.PLCNum, RMSection.RM1Element.PLCReadOffset, RMSection.RM1Element.PLCWriteOffset);
                    this.Add(RM1);
                }

                //RM2 생성=============================================================================================================
                if (RMSection.RM2Element.RMType == eRMType.TPLC)
                {
                    RM_TPLC RM2 = new RM_TPLC(RMSection.RM2Element.ModuleName, false, RMSection.RM2Element.RMType, 2, false);
                    RM2.SetCranePLCID(RMSection.RM2Element.CraneID);
                    RM2.SetAutoSafetySpeed(RMSection.RM2Element.DoorUnlockSpeed, RMSection.RM2Element.AutoStartSpeed);
                    RM2.InitPLCInterface(RMSection.RM2Element.PLCNum, RMSection.RM2Element.PLCReadOffset, RMSection.RM2Element.PLCWriteOffset); //2022.06.30 RGJ PLC 초기화 코드 
                    this.Add(RM2);
                }
            }
            else
            {
                //RM1 생성
                if (RMSection.RM1Element.RMType == eRMType.TPLC)
                {
                    // 2020.12.16 RM Type 추가 ModuleBase 수정
                    RM_TPLC RM1 = new RM_TPLC(RMSection.RM1Element.ModuleName, RMSection.RM1Element.SimulMode, RMSection.RM1Element.RMType, 1, RMSection.RM1Element.IOSimulMode);
                    RM1.SetCranePLCID(RMSection.RM1Element.CraneID);
                    RM1.SetAutoSafetySpeed(RMSection.RM1Element.DoorUnlockSpeed, RMSection.RM1Element.AutoStartSpeed);
                    //220628 HHJ SCS 개선     //- PLCDataItems 개선
                    //RM1.InitPLCInterface(RMSection.RM1Element.PLCReadOffset, RMSection.RM1Element.PLCWriteOffset); //2022.06.30 RGJ PLC 초기화 코드 
                    RM1.InitPLCInterface(RMSection.RM1Element.PLCNum, RMSection.RM1Element.PLCReadOffset, RMSection.RM1Element.PLCWriteOffset);
                    this.Add(RM1);
                }

                //RM2 생성=============================================================================================================
                if (RMSection.RM2Element.RMType == eRMType.TPLC)
                {
                    // 2020.12.16 RM Type 추가 ModuleBase 수정
                    RM_TPLC RM2 = new RM_TPLC(RMSection.RM2Element.ModuleName, RMSection.RM2Element.SimulMode, RMSection.RM2Element.RMType, 2, RMSection.RM2Element.IOSimulMode);
                    RM2.SetCranePLCID(RMSection.RM2Element.CraneID);
                    RM2.SetAutoSafetySpeed(RMSection.RM2Element.DoorUnlockSpeed, RMSection.RM2Element.AutoStartSpeed);
                    //220628 HHJ SCS 개선     //- PLCDataItems 개선
                    //RM2.InitPLCInterface(RMSection.RM2Element.PLCReadOffset, RMSection.RM2Element.PLCWriteOffset); //2022.06.30 RGJ PLC 초기화 코드 
                    RM2.InitPLCInterface(RMSection.RM2Element.PLCNum, RMSection.RM2Element.PLCReadOffset, RMSection.RM2Element.PLCWriteOffset); //2022.06.30 RGJ PLC 초기화 코드 
                    this.Add(RM2);
                }

                TimeoutSetting();

                Thread th = new Thread(new ThreadStart(CollectVibrationData));
                th.IsBackground = true;
                th.Name = "T-VibrationData";
                th.Start();

                Thread th2 = new Thread(new ThreadStart(CollectUtilData));
                th2.IsBackground = true;
                th2.Name = "T-UtilData";
                th2.Start();
            }
        }
        /// <summary>
        /// 크레인 가동률 데이터 수집 쓰레드
        /// </summary>
        private void CollectUtilData()
        {
            RM_TPLC C1 = null; //사용할 임시 변수들
            RM_TPLC C2 = null;

            eCraneUtilState C1_OLD_UtilState = eCraneUtilState.NONE;
            eCraneUtilState C2_OLD_UtilState = eCraneUtilState.NONE;

            eCraneUtilState C1_NEW_UtilState = eCraneUtilState.NONE;
            eCraneUtilState C2_NEW_UtilState = eCraneUtilState.NONE;

            DateTime C1StateStartTime;
            DateTime C2StateStartTime;

            DateTime C1StateEndTime;
            DateTime C2StateEndTime;

            GlobalData.Current.MRE_GlobalDataCreatedEvent.WaitOne();

            bool DualCrane = GlobalData.Current.SCSType == eSCSType.Dual;
            LogManager.WriteConsoleLog(eLogLevel.Info, "The Utilization of the crane has begun to be collected.");

            C1 = FirstRM as RM_TPLC;
            C1_OLD_UtilState = C1.GetCraneUtilState(); //초기값을 가져온다.
            if(DualCrane)
            {
                C2 = SecondRM as RM_TPLC;
                C2_OLD_UtilState = C2.GetCraneUtilState(); //초기값을 가져온다.
            }
            C1StateStartTime = DateTime.Now;
            C2StateStartTime = DateTime.Now;


            while (true)
            {
                try
                {
                    C1_NEW_UtilState = C1.GetCraneUtilState();   //현재값갱신
                    if (C1_OLD_UtilState != C1_NEW_UtilState) //가동률 상태가 바뀌었으므로 로그를 남긴다.
                    {
                        C1StateEndTime = DateTime.Now; //현재시각으로 상태값 종료 타임찍는다.
                        TimeSpan Ts1 = C1StateEndTime - C1StateStartTime; //경과 초 계산
                        LogManager.WriteCraneUtilLog(C1_OLD_UtilState.ToString(), C1.ModuleName, C1StateStartTime, C1StateEndTime, Ts1.TotalSeconds);

                        C1_OLD_UtilState = C1_NEW_UtilState;
                        C1StateStartTime = DateTime.Now; //로그를 찍고 현재시각으로 상태값 시작 타임찍는다.
                    }
                    if (DualCrane)
                    {

                        C2_NEW_UtilState = C2.GetCraneUtilState();
                        if (C2_OLD_UtilState != C2_NEW_UtilState) //가동률 상태가 바뀌었으므로 로그를 남긴다.
                        {
                            C2StateEndTime = DateTime.Now; //현재시각으로 상태값 종료 타임찍는다.
                            TimeSpan Ts2 = C2StateEndTime - C2StateStartTime; //경과 초 계산
                            LogManager.WriteCraneUtilLog(C2_OLD_UtilState.ToString(), C2.ModuleName, C1StateStartTime, C2StateEndTime, Ts2.TotalSeconds);
                            
                            
                            C2_OLD_UtilState = C2_NEW_UtilState;
                            C2StateStartTime = DateTime.Now; // //로그를 찍고 현재시각으로 상태값 시작 타임찍는다.
                        }
                    }
                }
                catch(Exception ex)
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
                }

                Thread.Sleep(UtilCollectDelay);
            }
        }

        private void CollectVibrationData()
        {
            LogManager.WriteConsoleLog(eLogLevel.Info, "The vibration of the crane has begun to be collected.");
            bool DualCrane = GlobalData.Current.SCSType == eSCSType.Dual;
            DateTime LastLogDeleteTime = DateTime.Now;
            while (true)
            {
                Thread.Sleep(CollectDelay);
                if ((DateTime.Now - LastLogDeleteTime) >= TimeSpan.FromHours(24)) //241128 RGJ 1시간마다 수행 -> 24시간 주기로 변경.
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "Log Clean Up Start => Delete log that have been created for {0} days", LogManager.GetLogStoragePeriod());
                    LogManager.CheckLogStorage(); //231207 RGJ 로그 정리 수행.
                    GlobalData.Current.DBManager.RemoveLogData(LogManager.GetLogStoragePeriod()); //231207 RGJ 디비 로그도 정리 수행.
                    LastLogDeleteTime = DateTime.Now;
                }

                if (GlobalData.Current.MainBooth.SCState != eSCState.AUTO)
                    continue;

                LogManager.WriteRobotLog(eLogLevel.Info, "RM1", "Crane : {0} X_VibrationData :{1}", FirstRM.ModuleName, FirstRM.PLC_RM_X_VibrationData);
                LogManager.WriteRobotLog(eLogLevel.Info, "RM1", "Crane : {0} Z_VibrationData :{1}", FirstRM.ModuleName, FirstRM.PLC_RM_Z_VibrationData);
                if (DualCrane)
                {
                    LogManager.WriteRobotLog(eLogLevel.Info, "RM2", "Crane : {0} X_VibrationData :{1}", SecondRM.ModuleName, SecondRM.PLC_RM_X_VibrationData);
                    LogManager.WriteRobotLog(eLogLevel.Info, "RM2", "Crane : {0} Z_VibrationData :{1}", SecondRM.ModuleName, SecondRM.PLC_RM_Z_VibrationData);
                }
            }
        }
        public RMModuleBase GetAnotherRM(RMModuleBase RM)
        {
            return RM.RMNumber == FirstRM.RMNumber ? SecondRM : FirstRM;
        }

        public void Add(RMModuleBase RM)
        {
            _RMList.Add(RM.ModuleName.ToString(), RM);
        }

        public void Start(string moduleID = "")
        {
            if (string.IsNullOrEmpty(moduleID))
            {
                foreach (var item in _RMList)
                {
                    //item.Value.Start();
                }
            }
            else
            {
                //_cmList[moduleID].Start();
            }
        }

        public void Stop(string moduleID)
        {
            if (string.IsNullOrEmpty(moduleID))
            {
                foreach (var item in _RMList)
                {
                    //item.Value.ThreadStop = true;
                }
            }
            else
            {
                //_cmList[moduleID].ThreadStop = true;
            }
        }

        public bool Contains(string ModuleID)
        {
            if (string.IsNullOrEmpty(ModuleID))
                return false;
            return _RMList.ContainsKey(ModuleID);
        }

        public void CloseControllers()
        {
            try
            {

                foreach (var item in this.ModuleList)
                {
                    item.Value.CloseController();
                }

            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
            }
        }

        public bool TimeoutSetting()
        {
            string value = string.Empty;
            foreach (var item in this.ModuleList)
            {
                //if (mGdata.DBManager.DbGetConfigInfo("RMSection", "CommandTimeout", out value))
                if (mGdata.DBManager.DbGetGlobalConfigValue("RMSection", "CommandTimeout", out value))
                {
                    item.Value.CommandTimeOut = Convert.ToInt32(value);
                    value = string.Empty;
                }
                else
                {
                    item.Value.CommandTimeOut = 5;
                    mGdata.DBManager.DbSetProcedureConfigInfo("RMSection", "CommandTimeout", item.Value.CommandTimeOut.ToString());
                }

                //if (mGdata.DBManager.DbGetConfigInfo("RMSection", "FireNotifyTimeOut", out value))
                if (mGdata.DBManager.DbGetGlobalConfigValue("RMSection", "FireNotifyTimeOut", out value))
                {
                    item.Value.FireNotifyTimeOut = Convert.ToInt32(value);
                    value = string.Empty;
                }
                else
                {
                    item.Value.FireNotifyTimeOut = 2;
                    mGdata.DBManager.DbSetProcedureConfigInfo("RMSection", "FireNotifyTimeOut", item.Value.FireNotifyTimeOut.ToString());
                }

                //if (mGdata.DBManager.DbGetConfigInfo("RMSection", "PLCIF_Delay", out value))
                if (mGdata.DBManager.DbGetGlobalConfigValue("RMSection", "PLCIF_Delay", out value))
                {
                    item.Value.PLCIF_Delay = Convert.ToInt32(value);
                    value = string.Empty;
                }
                else
                {
                    item.Value.PLCIF_Delay = 50;
                    mGdata.DBManager.DbSetProcedureConfigInfo("RMSection", "PLCIF_Delay", item.Value.PLCIF_Delay.ToString());
                }

                //if (mGdata.DBManager.DbGetConfigInfo("RMSection", "CraneActionTimeOut", out value))
                if (mGdata.DBManager.DbGetGlobalConfigValue("RMSection", "CraneActionTimeOut", out value))
                {
                    item.Value.CraneActionTimeOut = Convert.ToInt32(value);
                    value = string.Empty;
                }
                else
                {
                    item.Value.CraneActionTimeOut = 300;
                    mGdata.DBManager.DbSetProcedureConfigInfo("RMSection", "CraneActionTimeOut", item.Value.CraneActionTimeOut.ToString());
                }

                //if (mGdata.DBManager.DbGetConfigInfo("RMSection", "PLCTimeOut", out value))
                if (mGdata.DBManager.DbGetGlobalConfigValue("RMSection", "PLCTimeOut", out value))
                {
                    item.Value.PLCTimeOut = Convert.ToInt32(value);
                    value = string.Empty;
                }
                else
                {
                    item.Value.PLCTimeOut = 5;
                    mGdata.DBManager.DbSetProcedureConfigInfo("RMSection", "PLCTimeOut", item.Value.PLCTimeOut.ToString());
                }
            }
            return true;
        }
        //241018 HoN 화재시나리오 운영 추가       //전체 크레인 화재여부 판단 기능 추가
        public bool CheckCraneFire()
        {
            bool bvalue = false;

            try
            {
                foreach (RMModuleBase rm in _RMList.Values)
                {
                    bvalue |= rm.CheckForkInFire();
                }
            }
            catch(Exception ex)
            {
                bvalue = false;
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
            }
            return bvalue;
        }
        //241030 HoN 화재 관련 추가 수정        //-. PLC로 알려주는 Bit 화재 발생하면 무조건 전 Crane ON 처리. -> OFF시점은 Operator가 수동으로 해야함. 이를 수행하지 않아 발생하는 문제는 오퍼레이터 조작미스로 처리
        public bool CheckCraneFireOccurred()
        {
            bool bvalue = false;

            try
            {
                foreach (RMModuleBase rm in _RMList.Values)
                {
                    bvalue |= rm.CheckCraneFireOccurred();
                }
            }
            catch (Exception ex)
            {
                bvalue = false;
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
            }
            return bvalue;
        }
        public void NotifyFireCommand(bool fire)
        {
            try
            {
                foreach (RMModuleBase rm in _RMList.Values)
                {
                    rm.NotifyFireCommand(fire);
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
            }
        }

        public void MapChangeForExitThread()
        {
            foreach (RMModuleBase rm in _RMList.Values)
            {
                rm.ExitRunThread();
            }
        }
    }
}
