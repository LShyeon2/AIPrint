using BoxPrint.Log;
using BoxPrint.Modules.Conveyor;
using BoxPrint.Modules.RFID;
using BoxPrint.Modules.Shelf;
using BoxPrint.Modules.Print;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Linq;

namespace BoxPrint.Modules.CVLine
{
    /// <summary>
    /// 컨베이어 라인 관리 클래스
    /// </summary>
    public class CVLineManager
    {
        private object BcrAccessLock = new object();
        private object LockObject = new object();
        private bool SimulMode = false;
        private GlobalData mGdata = null;
        private Dictionary<string, CVLineModule> _LineList;
        //string portConfigPath = GlobalData.Current.CurrentFilePaths(System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName)) + GlobalData.Current.PortConfigPath;

        private string _portConfigPath = string.Empty;
        public string portConfigPath
        {
            get
            {
                return _portConfigPath;
            }
            private set
            {
                _portConfigPath = value;
            }
        }

        //private List<CVLineRecoveryData> RecoveryLineDataList = new List<CVLineRecoveryData>(); //컨베이어라인 복구용 데이터를 임시 저장
        //private List<CVRecoveryData> RecoveryCVDataList = new List<CVRecoveryData>(); //컨베이어 복구용 데이터를 임시 저장
        //private bool PortAutoRecovery = false;

        //220921 조숭진 s
        private int PLCTimeout { get; set; }
        private int LocalStepCycleDelay { get; set; }
        private int ValidationWaitTime { get; set; }
        private int CarrierIDCheckTime { get; set; }
        //220921 조숭진 e

        List<CV_BaseModule> _AllCVList = null;
        public List<CV_BaseModule> AllCVList
        {
            get
            {
                if (_AllCVList == null) //처음 GET 요청 올때만 생성해서 리턴
                {
                    _AllCVList = new List<CV_BaseModule>();
                    foreach (var line in _LineList)
                    {
                        foreach (var cvItem in line.Value.ModuleList)
                        {
                            _AllCVList.Add(cvItem);
                        }
                    }
                    return _AllCVList;
                }
                else //재요청시 리스트 바로 리턴
                {
                    return _AllCVList;
                }
            }
        }

        private List<CV_BaseModule> _ReadPivotCVList = new List<CV_BaseModule>(); //각 PLC CPU 별 Read 해야하는 컨베이어 모듈을 PLC 번호 상관없이 저장해둔다.  (CVTrack => ex 1,17,33...) 

        public ReadOnlyCollection<CV_BaseModule> ReadPivotCVList
        {
            get
            {
                return _ReadPivotCVList.AsReadOnly();
            }
        }

        #region 속성들 정의
        public Dictionary<string, CVLineModule> ModuleList
        {
            get { return _LineList; }
            private set { }
        }

        public CVLineModule this[string moduleID]
        {
            get { return _LineList[moduleID]; }
        }

        #endregion

        //220823 조숭진 db config에서 읽어오기 위해 RFID_ModuleBase로 옮김 s
        private Dictionary<string, RFID_ModuleBase> _BCRList;
        public Dictionary<string, RFID_ModuleBase> BCRList
        {
            get { return _BCRList; }
            private set { }
        }
        //220823 조숭진 db config에서 읽어오기 위해 RFID_ModuleBase로 옮김 e


        private Dictionary<string, TCP_ModuleBase> _UnitList;
        public Dictionary<string, TCP_ModuleBase> UnitList
        {
            get { return _UnitList; }
            private set { }
        }

        /// <summary>
        /// Manager 생성자
        /// </summary>
        public CVLineManager()
        {
            LogManager.WriteConsoleLog(eLogLevel.Info, eModuleList.CV, "CVLineManager start creating.");
            _LineList = new Dictionary<string, CVLineModule>();
            mGdata = GlobalData.Current;
            //LocalDBManager.Current.GetRecoveryDataCVLines(RecoveryLineDataList); //불필요 내역 삭제
            //LocalDBManager.Current.GetRecoveryDataCV(RecoveryCVDataList);

            _UnitList = new Dictionary<string, TCP_ModuleBase>();   //220823 조숭진 db config에서 읽어오기 위해 RFID_ModuleBase로 옮김

            LoadPortLayout();
            //StartAllLineModule();

            //Thread LampThread = new Thread(new ThreadStart(LampControlRun));
            //LampThread.IsBackground = true;
            //LampThread.Start();

            MakeReadListCV(); //20230803 RGJ 멀티 포트 Read 대응추가

            LogManager.WriteConsoleLog(eLogLevel.Info, "CVLineManager has been created.");
        }
        private void MakeReadListCV()
        {
            int LastReadTrack = 1;
            //20230803 RGJ 멀티 포트 Read 대응추가
            short MinCpu = AllCVList.Min(c => c.PLCNum); //최소값 획득
            short MaxCpu = AllCVList.Max(c => c.PLCNum); //최대값 획득

            for(int i = MinCpu; i <= MaxCpu; i++)
            {
                var SameCpu_CV = AllCVList.Where(c => c.PLCNum == i).OrderBy(c => c.TrackNum); //CPU별 필터링 및 트랙번호 정렬
               
                if(SameCpu_CV.Count() > 0)
                {
                    _ReadPivotCVList.Add(SameCpu_CV.FirstOrDefault());//일단 CPU별 최앞단 트랙은 무조건 넣어야 한다.
                    LastReadTrack = SameCpu_CV.FirstOrDefault().TrackNum;
                    for (int j = 1; j < SameCpu_CV.Count(); j++)
                    {
                        if(SameCpu_CV.ElementAt(j).TrackNum  >= LastReadTrack + 16) //16Track 번호 넘어가면 새로 읽기 목록에 넣어둔다.
                        {
                            _ReadPivotCVList.Add(SameCpu_CV.ElementAt(j)); //리스트에 추가
                            LastReadTrack = SameCpu_CV.ElementAt(j).TrackNum; //가장 마지막에 넣은 트랙번호 갱신
                        }
                    }
                }
            }

        }

        public void resetCVLineManager()
        {
            //동작 중이던 쓰레드 중단.
            foreach (var cm in AllCVList)
            {
                if(cm != null)
                {
                    cm.ExitRunThread();
                }
            }
            //클라인어트 쓰레드 종료 대기.
            //다른 동작은 없으므로  Join 대기 생략함.
            Thread.Sleep(500);

            //_BCRList.Clear();
            _UnitList.Clear();
            ModuleList.Clear();
            _LineList.Clear();
            //LoadPortLayout();
            LogManager.WriteConsoleLog(eLogLevel.Info, "CVLineManager start recreating..");
        }
        //private void LampControlRun()
        //{
        //    try //-메인 루프 예외 발생시 로그 찍도록 추가.
        //    {
        //        int ThreadDelay = 100;
        //        int BlinkTick = 0;
        //        if (SimulMode)
        //        {
        //            return;
        //        }
        //        while (true)
        //        {
        //            //누름 감지 제어
        //            foreach (var item in _LineList)
        //            {
        //                foreach (var CVItem in item.Value.ModuleList)
        //                {
        //                    bool SwitchOn = CVItem.CheckStartSwitch();
        //                    bool BlinknOn = CVItem.RequestStartSwitchBlink();
        //                    if (!BlinknOn)
        //                    {
        //                        CVItem.StartSwitchLampControl(SwitchOn);
        //                    }
        //                    else
        //                    {
        //                        CVItem.StartSwitchLampControl(BlinkTick > 5 ? true : false);
        //                    }
        //                }
        //            }
        //            BlinkTick++;
        //            if (BlinkTick >= 10)
        //            {
        //                BlinkTick = 0;
        //            }
        //            Thread.Sleep(ThreadDelay);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
        //    }
        //}

        /// <summary>
        /// 해당 이름 CV라인 상태를 복구할지 결정
        /// </summary>
        /// <param name="LineModuleName"></param>
        /// <returns></returns>
        //public bool CheckRequire_CVlineRecovery(string LineModuleName)
        //{
        //    try
        //    {
        //        if (RecoveryCVDataList != null)
        //        {
        //            //하위 컨베이어 모듈에 트레이가 있는지 확인.
        //            var CVModules = RecoveryCVDataList.Where(CV => CV._LineName == LineModuleName && CV._TrayExist && CV._TagID != "ERROR");
        //            if (CVModules.Count() == 1) //트레이가 존재하는 모듈이 한개여야 유효간주.
        //            {
        //                return true;
        //            }
        //            else
        //            {
        //                return false;
        //            }
        //        }
        //        else
        //        {
        //            return false;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
        //        return false;
        //    }

        //}

        //public CVRecoveryData GetCVRecoveryData(string CVModuleName)
        //{
        //    try
        //    {
        //        if (!string.IsNullOrEmpty(CVModuleName))
        //        {
        //            return RecoveryCVDataList.Find(cv => cv._ModuleName == CVModuleName);
        //        }
        //        else
        //        {
        //            return null;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
        //        return null;
        //    }
        //}
        //public int GetRecovery_LineStep(string LineModuleName)
        //{
        //    try
        //    {
        //        if (RecoveryLineDataList != null)
        //        {
        //            return RecoveryLineDataList.Find(c => c.LineModuleName == LineModuleName).ModuleStep;
        //        }
        //        else
        //        {
        //            return -1;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
        //        return -1;
        //    }
        //}

        //private void StartAllLineModule()
        //{
        //    foreach (var item in _LineList)
        //    {
        //        item.Value.StartCVLine();
        //    }
        //}

        private void Add(CVLineModule CVLine)
        {
            lock (this.LockObject)
            {
                _LineList.Add(CVLine.ModuleName, CVLine);
            }
        }

        public void AutoJobStart(string moduleID = "")
        {
            try
            {
                _LineList["ModuleID"].SetCVLineCommand(eCVLineCommand.AutoJobTask);
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
            }
        }

        public void AutoJobStop(string moduleID)
        {
            try
            {
                _LineList["ModuleID"].SetCVLineCommand(eCVLineCommand.ManualTask);
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
            }
        }

        public void AutoJobAllLine()
        {
            foreach (var item in _LineList)
            {
                item.Value.SetCVLineCommand(eCVLineCommand.AutoJobTask);
            }
        }
        public void AutoJobStopAllLine()
        {
            foreach (var item in _LineList)
            {
                item.Value.SetCVLineCommand(eCVLineCommand.ManualTask);
            }
        }

        public bool IsCVContains(string CVModuleID)
        {
            if (GetCVModule(CVModuleID) == null)
                return false;
            else
                return true;
        }

        public bool IsCVLineContains(string LineModuleID)
        {
            if (string.IsNullOrEmpty(LineModuleID))
                return false;

            return _LineList.ContainsKey(LineModuleID);
        }

        //public CV_BaseModule GetFirstPLCCVModule(int PLCNumber)
        //{
        //    if (FirstPLCDic.ContainsKey(PLCNumber)) //이미 계산했으면 결과만 리턴
        //    {
        //        return FirstPLCDic[PLCNumber];
        //    }
        //    else
        //    {
        //        CV_BaseModule FirstPLCPortModule = null;
        //        //한번만 계산하고 이후에는 결과만 리턴
        //        ushort minBase = ushort.MaxValue;
        //        foreach (var cItem in AllCVList)
        //        {
        //            if (cItem.PLCNum != PLCNumber)
        //            {
        //                continue;
        //            }
        //            if (cItem.BaseAddress <= minBase)
        //            {
        //                minBase = cItem.BaseAddress;
        //                FirstPLCPortModule = cItem;
        //            }
        //        }
        //        if (FirstPLCPortModule != null)
        //        {
        //            FirstPLCDic.Add(PLCNumber, FirstPLCPortModule); //해당 모듈을 찾았으면 딕션너리에 저장해둔다.
        //        }
        //        return FirstPLCPortModule;
        //    }
        //}

        public CV_BaseModule GetCVModule(string CVModuleName)
        {
            foreach (var Line in _LineList)
            {
                var module = Line.Value.ModuleList.Where(c => c.ModuleName == CVModuleName).FirstOrDefault();
                if (module != null)
                {
                    return module;
                }
            }
            return null;
        }

        public bool CheckCVModuleName(string CVModuleName)
        {
            bool bCheck = false;

            foreach (var Line in _LineList)
            {
                bCheck = Line.Value.ModuleList.Where(m => m.ModuleName == CVModuleName).Count() > 0;

                if (bCheck)
                    break;
            }

            return bCheck;
        }


        public CVLineModule GetLineModule(string LineModuleName)
        {
            if (IsCVLineContains(LineModuleName))
            {
                return _LineList[LineModuleName];
            }
            else
            {
                return null;
            }
        }

        public CV_BaseModule GetCVModuleByTag(string RobotTeachingTag)
        {
            foreach (var Line in _LineList)
            {
                if (Line.Value.ModuleList.First.Value.GetRobotCommandTag() == RobotTeachingTag)
                {
                    return Line.Value.ModuleList.First.Value;
                }
            }
            return null;
        }
        public CV_BaseModule GetCVModuleWorkPlace(short workPlaceNum)
        {
            return AllCVList.Where(c => c.iWorkPlaceNumber == workPlaceNum).FirstOrDefault();
        }


        public CV_BaseModule GetCVModuleByServoAxisNumber(int AxisNumber)
        {
            foreach (var Line in _LineList)
            {
                var module = Line.Value.ModuleList.Where(c => c.ServoAxis == AxisNumber).FirstOrDefault();
                if (module != null)
                {
                    return module;
                }
            }
            return null;
        }
        public CV_BaseModule GetCVModuleByLightCurtainNumber(int LCNumber)
        {
            if (LCNumber > 0)
            {
                foreach (var Line in _LineList)
                {
                    var module = Line.Value.ModuleList.Where(c => c.LightCurtainNumber == LCNumber).FirstOrDefault();
                    if (module != null)
                    {
                        return module;
                    }
                }
            }
            return null;
        }


        /// <summary>
        /// 모든 내부 모듈을 정지 시킨다.
        /// </summary>
        public void EmergencyStop()
        {
            foreach (var Pitem in ModuleList)
            {
                Pitem.Value.LineEmergencyAction();
            }
        }
        /// <summary>
        /// 메뉴얼 포트 EMO 동작
        /// </summary>
        public void EmergencyManual_PortStop()
        {
            foreach (var Pitem in ModuleList)
            {
                if (Pitem.Value.LineType == eCVLineType.MaunalIn || Pitem.Value.LineType == eCVLineType.MaunalOut)
                {
                    Pitem.Value.LineEmergencyAction();
                }
                else
                {
                    continue;
                }
            }
        }
        /// <summary>
        /// OHTIn 포트 EMO 동작
        /// </summary>
        public void EmergencyOHTIn_PortStop()
        {
            foreach (var Pitem in ModuleList)
            {
                //2021.05.25 lim, TurnOHTIF 추가
                if (Pitem.Value.LineType == eCVLineType.AutoIn && Pitem.Value.ModuleList.Where(R => (R.CVModuleType == eCVType.OHTIF || R.CVModuleType == eCVType.TurnOHTIF)).Count() > 0)
                {
                    Pitem.Value.LineEmergencyAction();
                }
                else
                {
                    continue;
                }
            }
        }
        /// <summary>
        /// OHTOut 포트 EMO 동작
        /// </summary>
        public void EmergencyOHTOut_PortStop()
        {
            foreach (var Pitem in ModuleList)
            {
                //2021.05.25 lim, TurnOHTIF 추가
                if (Pitem.Value.LineType == eCVLineType.AutoOut && Pitem.Value.ModuleList.Where(R => (R.CVModuleType == eCVType.OHTIF || R.CVModuleType == eCVType.TurnOHTIF)).Count() > 0)
                {
                    Pitem.Value.LineEmergencyAction();
                }
                else
                {
                    continue;
                }
            }
        }
        public void LoadPortLayout()
        {
            bool portconfigchg = false;
            int LineCounter = 0;
            bool AllLineCreated = true;
            Task<CVLineModule>[] ModuleCreateTaskArray = new Task<CVLineModule>[100];
            string PortConfigPath = GlobalData.Current.CurrentFilePaths(System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName)) + GlobalData.Current.PortConfigPath;
            bool configExist = File.Exists(PortConfigPath);
            if (!configExist)
            {
                string temppath = string.Empty;

                temppath = GlobalData.Current.FilePathChange(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName + GlobalData.Current.PortConfigPath, GlobalData.Current.PortConfigPath);
                //if (!bPortConfigPathChange(out temppath))
                //{
                //    throw new FileNotFoundException(PortConfigPath + "파일을 찾을수 없습니다.");
                //}
                //else
                //{
                //    this.portConfigPath = temppath;
                //}
                this.portConfigPath = temppath;
            }
            else
            {
                this.portConfigPath = PortConfigPath;
            }

            //20220728 조숭진 config 방식 변경 s
            //XElement portsElement = XElement.Load(portConfigPath);

            //foreach (XElement portXe in portsElement.Elements())
            //{
            //    ModuleCreateTaskArray[LineCounter] = Task<CVLineModule>.Factory.StartNew(() =>
            //    {
            //        return CreateLineModule(portXe);
            //    });
            //    LineCounter++;
            //}
            //int count = GlobalData.Current.DBManager.DBGetInfoCount("CVLine");
            string strcount = string.Empty;
            int count = 0;

            GlobalData.Current.DBManager.DbGetGlobalConfigValue("CVLine", "Count", out strcount);

            if (!string.IsNullOrEmpty(strcount))
                count = Convert.ToInt32(strcount);

            TimeSetting(true);      //220921 db에서 읽어오기


            portconfigchg = portconfigLoad();

            //220726 조숭진 s
            if (portconfigchg || count == 0)
            {
                if (portconfigchg)
                {
                    GlobalData.Current.DBManager.DBConfigTableDataDelete(GlobalData.Current.EQPID);
                    GlobalData.Current.DBConfigDataInitialize();
                }

                SetDBCVInfoFromXML();
                //count = GlobalData.Current.DBManager.DBGetInfoCount("CVLine");
                GlobalData.Current.DBManager.DbGetGlobalConfigValue("CVLine", "Count", out strcount);
                count = Convert.ToInt32(strcount);
            }

            for (int i = 0; i < count; i++)
            {
                Dictionary<string, string> dictemp = new Dictionary<string, string>();
                string temp = string.Format("CVLine{0}", i + 1);
                //dictemp = GlobalData.Current.DBManager.DBGetCVInfoData(temp);
                dictemp = GlobalData.Current.DBManager.DbGetGlobalCVInfo(temp);

                ModuleCreateTaskArray[LineCounter] = Task<CVLineModule>.Factory.StartNew(() =>
                {
                    return CreateLineModule(dictemp, temp);
                });
                LineCounter++;
            }
            
            
            //20220728 조숭진 config 방식 변경 e

            while (true)
            {
                AllLineCreated = true;
                for (int i = 0; i < LineCounter; i++)
                {
                    AllLineCreated &= ModuleCreateTaskArray[i].IsCompleted;
                }
                if (AllLineCreated)
                {
                    for (int i = 0; i < LineCounter; i++)
                    {
                        Add(ModuleCreateTaskArray[i].Result);
                    }
                    break;
                }
                Thread.Sleep(50);
            }
            TimeoutSet(); //230410 RGJ 타임아웃 세팅 여기서 수행
            foreach (var Pitem in ModuleList)
            {
                Pitem.Value.InitLineCV();//각 하위 모듈을 동작시킨다.
            }

            if (!portconfigchg)
            {
                PortConfigSettingRenewal(_LineList);
            }

            return;
        }

        private int GetCVInfoCountFromXML()
        {
            int cvlinenum = 0;

            XmlDocument xmldoc = new XmlDocument();

            try
            {
                xmldoc.Load(portConfigPath);
                foreach (XmlNode firstnode in xmldoc.ChildNodes)
                {
                    if (firstnode.Attributes != null)
                    {

                        cvlinenum = 0;
                        if (firstnode.ChildNodes.Count != 0)
                        {
                            string cvlinename = string.Empty;

                            //foreach(XmlNode secondnode in firstnode)
                            for (int j = 0; j < firstnode.ChildNodes.Count; j++)
                            {
                                var element1 = firstnode.ChildNodes[j];
                                if (element1.NodeType == XmlNodeType.Element)
                                {
                                    ++cvlinenum;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
                return 0;
            }
            return cvlinenum;
        }

        private Dictionary<string, string> GetCVInfoFromXML(string Line)
        {
            XmlDocument xmldoc = new XmlDocument();
            Dictionary<string, string> dic = new Dictionary<string, string>();

            try
            {
                xmldoc.Load(portConfigPath);
                foreach (XmlNode firstnode in xmldoc.ChildNodes)
                {
                    if (firstnode.Attributes != null)
                    {

                        int cvlinenum = 0;
                        if (firstnode.ChildNodes.Count != 0)
                        {
                            string cvlinename = string.Empty;

                            //foreach(XmlNode secondnode in firstnode)
                            for (int j = 0; j < firstnode.ChildNodes.Count; j++)
                            {
                                var element1 = firstnode.ChildNodes[j];
                                if (element1.NodeType == XmlNodeType.Element)
                                {
                                    ++cvlinenum;
                                    //string section1 = element1.Name;// + cvlinenum.ToString();
                                    cvlinename = Regex.Replace(element1.Name, @"[0-9.]", "");
                                    string section1 = cvlinename + cvlinenum.ToString();

                                    if (section1 != Line)
                                        continue;

                                    for (int i = 0; i < element1.Attributes.Count; i++)
                                    {
                                        var temp = element1.Attributes[i];

                                        dic.Add(temp.Name, temp.Value);
                                        //GlobalData.Current.DBManager.DbSetProcedureConfigInfo(section1, temp.Name, temp.Value, string.Empty, string.Empty);

                                    }

                                    int cvmodulenum = 0;
                                    if (element1.ChildNodes.Count != 0)
                                    {
                                        string cvmodulename = string.Empty;

                                        for (int o = 0; o < element1.ChildNodes.Count; o++)
                                        {
                                            var element2 = element1.ChildNodes[o];
                                            if (element2.NodeType == XmlNodeType.Element)
                                            {
                                                ++cvmodulenum;
                                                //string section2 = section1 + "." + element2.Name;// + cvmodulenum.ToString();
                                                cvmodulename = Regex.Replace(element2.Name, @"[0-9.]", "");
                                                string section2 = section1 + "." + cvmodulename + cvmodulenum.ToString();

                                                for (int t = 0; t < element2.Attributes.Count; t++)
                                                {
                                                    var temp = element2.Attributes[t];
                                                    //GlobalData.Current.DBManager.DbSetProcedureConfigInfo(section2, temp.Name, temp.Value, string.Empty, string.Empty);
                                                }

                                                int cvbcrnum = 0;
                                                if (element2.ChildNodes.Count != 0)
                                                {
                                                    for (int q = 0; q < element2.ChildNodes.Count; q++)
                                                    {
                                                        string cvbcrname = string.Empty;

                                                        var element3 = element2.ChildNodes[q];
                                                        if (element3.NodeType == XmlNodeType.Element)
                                                        {
                                                            ++cvbcrnum;
                                                            //string section3 = section2 + "." + element3.Name;// + cvbcrnum.ToString();
                                                            cvbcrname = Regex.Replace(element3.Name, @"[0-9.]", "");
                                                            string section3 = section2 + "." + cvbcrname + cvbcrnum.ToString();
                                                            for (int w = 0; w < element3.Attributes.Count; w++)
                                                            {
                                                                var temp2 = element3.Attributes[w];
                                                                GlobalData.Current.DBManager.DbSetProcedureConfigInfo(section3, temp2.Name, temp2.Value, string.Empty, string.Empty);

                                                                if (w + 1 == element3.Attributes.Count)
                                                                {
                                                                    string typename = section2 + "." + cvbcrname;
                                                                    //GlobalData.Current.DBManager.DbSetProcedureConfigInfo(typename, "Count", cvbcrnum.ToString(), string.Empty, string.Empty);
                                                                }
                                                            }
                                                        }
                                                        else if (element3.NodeType == XmlNodeType.Attribute)
                                                        {
                                                        }
                                                    }
                                                }
                                            }
                                            else if (element2.NodeType == XmlNodeType.Attribute)
                                            {

                                            }

                                            if (o + 1 == element1.ChildNodes.Count)
                                            {
                                                string typename = section1 + "." + cvmodulename;
                                                //GlobalData.Current.DBManager.DbSetProcedureConfigInfo(typename, "Count", cvmodulenum.ToString(), string.Empty, string.Empty);
                                            }
                                        }

                                    }
                                }
                                else if (element1.NodeType == XmlNodeType.Attribute)
                                {

                                }

                                if (j + 1 == firstnode.ChildNodes.Count)
                                {
                                    string typename = cvlinename;
                                    //GlobalData.Current.DBManager.DbSetProcedureConfigInfo(typename, "Count", cvlinenum.ToString(), string.Empty, string.Empty);
                                }
                            }
                        }
                    }
                    else
                    {
                        if (firstnode.NodeType == XmlNodeType.Element)
                        {
                            foreach (XmlNode secondnode in firstnode)
                            {

                            }
                        }
                        else if (firstnode.NodeType == XmlNodeType.Attribute)
                        {

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
                return dic;
            }

            return dic;
        }

        //20220728 조숭진 config 방식 변경 s
        //public CVLineModule CreateLineModule(XElement CVLineXML)
        //{
        //    eCVLineType PortType = (eCVLineType)Enum.Parse(typeof(eCVLineType), CVLineXML.Attribute("LineType").Value, true);
        //    Type type = Type.GetType("BoxPrint.Modules.CVLine.CVLineModule");
        //    CVLineModule Line = Activator.CreateInstance(type, CVLineXML.Attribute("ModuleName").Value, SimulMode, PortType) as CVLineModule;
        //    Line.SetFloorInfo(CVLineXML.Attribute("LineFloor").Value);

        //    //220316 HHJ SCS 개발     //- Layoutview C/V 회전, Port 겹침 추가
        //    //220520 HHJ SCS 개선     //- CV UserControl 관련 개선
        //    //Line.SetDegree(CVLineXML.Attribute("Degree").Value);
        //    Line.SetDegree(CVLineXML.Attribute("Rotate").Value);

        //    //하위 컨베이어 모듈을 만든다.
        //    CV_BaseModule CurrentCV = null;
        //    foreach (XElement cvXe in CVLineXML.Elements())
        //    {
        //        CurrentCV = CreateConveyorModule(cvXe);
        //        if(CurrentCV.CVModuleType == eCVType.RobotIF || CurrentCV.CVModuleType == eCVType.EQRobot || CurrentCV.CVModuleType == eCVType.OHTRobot)    //2021.05.24 lim, AGV 추가
        //        {
        //            CurrentCV.SetRobotAccessPosition(int.Parse(CVLineXML.Attribute("Bank").Value), int.Parse(CVLineXML.Attribute("Bay").Value), int.Parse(CVLineXML.Attribute("Level").Value));
        //        }
        //        Line.AddConveyor(CurrentCV);
        //        LogManager.WriteConsoleLog(eLogLevel.Info, "Conveyor :{0} Type :{1} => Conveyor Line :{2} 에 추가되었습니다.", CurrentCV.ModuleName, CurrentCV.CVModuleType, Line.ModuleName);
        //    }       
        //    LogManager.WriteConsoleLog(eLogLevel.Info, "Conveyor Line {0} 생성이 완료되었습니다.",Line.ModuleName);
        //    return Line;
        //}
        public CVLineModule CreateLineModule(Dictionary<string, string> CvLine, string linenumber)
        {
            //230214 HHJ SCS 개선
            //string porttype, modulename, floor, degree, bank, bay, level;
            string porttype, modulename, floor, degree, bank, bay, level, cvway;

            CvLine.TryGetValue("LineType", out porttype);
            eCVLineType PortType = (eCVLineType)Enum.Parse(typeof(eCVLineType), porttype, true);
            CvLine.TryGetValue("ModuleName", out modulename);
            Type type = Type.GetType("BoxPrint.Modules.CVLine.CVLineModule");
            CVLineModule Line = Activator.CreateInstance(type, modulename, SimulMode, PortType) as CVLineModule;
            CvLine.TryGetValue("LineFloor", out floor);
            Line.SetFloorInfo(floor);


            //220316 HHJ SCS 개발     //- Layoutview C/V 회전, Port 겹침 추가
            //220520 HHJ SCS 개선     //- CV UserControl 관련 개선
            //Line.SetDegree(CVLineXML.Attribute("Degree").Value);
            CvLine.TryGetValue("Rotate", out degree);
            Line.SetDegree(degree);

            //230214 HHJ SCS 개선
            CvLine.TryGetValue("CVWay", out cvway);
            Line.SetConveyorWay(cvway);

            //하위 컨베이어 모듈을 만든다.
            CV_BaseModule CurrentCV = null;

            //int count = GlobalData.Current.DBManager.DBGetInfoCount(linenumber + "." + "Conveyor");
            string strcount = string.Empty;
            int count = 0;

            GlobalData.Current.DBManager.DbGetGlobalConfigValue(linenumber + "." + "Conveyor", "Count", out strcount);
            if (!string.IsNullOrEmpty(strcount))
                count = Convert.ToInt32(strcount);

            for (int i = 0; i < count; i++)
            {
                Dictionary<string, string> dictemp = new Dictionary<string, string>();
                string temp = string.Format(linenumber + ".Conveyor" + "{0}", i + 1);
                //dictemp = GlobalData.Current.DBManager.DBGetCVInfoData(temp);
                dictemp = GlobalData.Current.DBManager.DbGetGlobalCVInfo(temp);

                //230214 HHJ SCS 개선
                //CurrentCV = CreateConveyorModule(dictemp, temp);
                CurrentCV = CreateConveyorModule(dictemp, temp, Line.CVWay);
                if (CurrentCV.CVModuleType == eCVType.RobotIF || CurrentCV.CVModuleType == eCVType.EQRobot || CurrentCV.CVModuleType == eCVType.WaterPool || CurrentCV.CVModuleType == eCVType.Manual || CurrentCV.CVModuleType == eCVType.Print)    //2021.05.24 lim, AGV 추가
                {
                    CvLine.TryGetValue("Bank", out bank);
                    CvLine.TryGetValue("Bay", out bay);
                    CvLine.TryGetValue("Level", out level);
                    CurrentCV.SetRobotAccessPosition(int.Parse(bank), int.Parse(bay), int.Parse(level));
                }
                Line.AddConveyor(CurrentCV);
                LogManager.WriteConsoleLog(eLogLevel.Info, "Conveyor :{0} Type :{1} => Conveyor Line :{2} added.", CurrentCV.ModuleName, CurrentCV.CVModuleType, Line.ModuleName);
            }
            //현재 라인내 포트 개수가 한개이고 해당 포트가 RobotIFModule 이면 LP 로 변경한다. //220901 RGJ 단일 포트는 LP 로 간주
            if (Line.ModuleList.Count == 1)
            {
                if (Line.ModuleList.First.Value.CVModuleType == eCVType.RobotIF)
                {
                    Line.ModuleList.First.Value.PortType = ePortType.LP;
                }
            }
            LogManager.WriteConsoleLog(eLogLevel.Info, "Conveyor Line {0} has been created.", Line.ModuleName);
            return Line;
        }

        /// <summary>
        /// 240816 RGJ 포트에서 화물 중복 체크
        /// </summary>
        /// <param name="recvPort"></param>
        /// <param name="cid"></param>
        /// <returns></returns>
        public bool CheckCarrierDuplicated(CV_BaseModule recvPort, string cid)
        {
            try
            {
                if(recvPort == null)
                {
                    return false;
                }
                CV_BaseModule DupPort = AllCVList.First(s => s.GetCarrierID().Equals(cid) && recvPort.ModuleName != s.ModuleName && //호출한 포트는 제외해야함.
                 s.ParentModule.ModuleName != recvPort.ParentModule.ModuleName); //같은 연결 라인은 체크 제외함.
                if(DupPort != null)
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "Port Carrier Duplicated Checked! SourcePort: {0} DupPort : {1} DupCarrierID :{2}",
                        recvPort.ModuleName, DupPort.ModuleName, cid);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        //20220728 조숭진 config 방식 변경 e

        //20220728 조숭진 config 방식 변경 s
        //public CV_BaseModule CreateConveyorModule(XElement CVXML)
        //{
        //    eCVType CVType = (eCVType)Enum.Parse(typeof(eCVType), CVXML.Attribute("CVType").Value, true);

        //    Type type = Type.GetType("BoxPrint.Modules.Conveyor." + GetCVClassNamebyType(CVType));

        //    CV_BaseModule cv = Activator.CreateInstance(type, CVXML.Attribute("ModuleName").Value, SimulMode) as CV_BaseModule;

        //    cv.SetPortSize((ePortSize)Enum.Parse(typeof(ePortSize), CVXML.Attribute("PortSize").Value, true));

        //    cv.SetDirection((eCV_Direction)Enum.Parse(typeof(eCV_Direction), CVXML.Attribute("Direction").Value, true));


        //    if(CVXML.Attribute("PortTableID") != null)
        //    {
        //        int PID = -1;
        //        int.TryParse(CVXML.Attribute("PortTableID").Value, out PID);
        //        cv.SetReceiveStopDelay(PID);
        //    }
        //    var InverterInstance = new Inverter(cv.ModuleName + "_INV", 0, false, true);
        //    cv.AttactchInverter(InverterInstance);

        //    //컨베이어 RFID 모듈 생성
        //    XElement RfidXe = CVXML.Element("RFID");
        //    if (RfidXe != null)
        //    {
        //        cv.AttactchRFID(CreateRFIDModule(RfidXe));
        //    }
        //    cv.SetTrackInfo(CVXML.Attribute("TrackGroup").Value, CVXML.Attribute("TrackNum").Value);

        //    //220628 HHJ SCS 개선		//- PLCDataItems 개선
        //    cv.SetPLCNum(short.Parse(CVXML.Attribute("PLCNum").Value));
        //    if (CVXML.Attribute("BaseAddress") != null)
        //    {
        //        cv.SetBaseAddress(Convert.ToInt16(CVXML.Attribute("BaseAddress").Value, 16));
        //    }

        //    //턴모듈 OperatorIF모듈 bypass 여부 입력
        //    if (cv.CVModuleType == eCVType.Turn || cv.CVModuleType == eCVType.OperatorIF || cv.CVModuleType == eCVType.TurnEQIF || cv.CVModuleType == eCVType.TurnOHTIF)    //2021.05.24 lim, TurnOHT 추가
        //    {
        //        cv.SetBypassMode(bool.Parse(CVXML.Attribute("BypassMode").Value));
        //    }

        //    return cv;
        //}
        //230214 HHJ SCS 개선
        //public CV_BaseModule CreateConveyorModule(Dictionary<string, string> cvModule, string modulenumber)
        public CV_BaseModule CreateConveyorModule(Dictionary<string, string> cvModule, string modulenumber, eCVWay cvway)
        {
            string cvtype, modulename, portsize, direction, tableid, tracknum, trackgroup, plcnum, use, fireShutterPos;

            cvModule.TryGetValue("CVType", out cvtype);
            eCVType CVType = (eCVType)Enum.Parse(typeof(eCVType), cvtype, true);

            Type type = Type.GetType("BoxPrint.Modules.Conveyor." + GetCVClassNamebyType(CVType));
            cvModule.TryGetValue("ModuleName", out modulename);
            CV_BaseModule cv = Activator.CreateInstance(type, modulename, SimulMode) as CV_BaseModule;
            cvModule.TryGetValue("PortSize", out portsize);
            cv.SetPortSize((ePortSize)Enum.Parse(typeof(ePortSize), portsize, true));
            cvModule.TryGetValue("Direction", out direction);
            cv.SetDirection((ePortInOutType)Enum.Parse(typeof(ePortInOutType), direction, true));

            cv.SetCVWay(cvway);     //230214 HHJ SCS 개선

            cvModule.TryGetValue("PortTableID", out tableid);
            if (!string.IsNullOrEmpty(tableid))
            {
                int PID = -1;
                int.TryParse(tableid, out PID);
                if (PID > 0)
                {
                    cv.SetPortTableID(PID);
                }
                //cv.SetReceiveStopDelay(PID);
            }
            //230814 RGJ 불필요한 인버터 모듈 생성 모두 삭제
            //var InverterInstance = new Inverter(cv.ModuleName + "_INV", 0, false, true);
            //cv.AttactchInverter(InverterInstance);

            //컨베이어 RFID 모듈 생성
            //int count = GlobalData.Current.DBManager.DBGetInfoCount(modulenumber + "." + "RFID");
            string strcount = string.Empty;
            int count = 0;

            GlobalData.Current.DBManager.DbGetGlobalConfigValue(modulenumber + "." + "UNIT", "Count", out strcount);
            if (!string.IsNullOrEmpty(strcount))
                count = Convert.ToInt32(strcount);

            for (int i = 0; i < count; i++)
            {
                Dictionary<string, string> dictemp = new Dictionary<string, string>();
                string temp = string.Format(modulenumber + ".RFID" + "{0}", i + 1);
                //dictemp = GlobalData.Current.DBManager.DBGetCVInfoData(temp);
                dictemp = GlobalData.Current.DBManager.DbGetGlobalCVInfo(temp);

                cv.AttactchRFID(CreateRFIDModule(dictemp));
                cv.BCRNumSetting(i + 1);        //220922 조숭진
            }

            //if (count != 0)
            //    BCRTimeoutSetting();       //220823 조숭진 db config에서 읽어오기 위해 RFID_ModuleBase로 옮김

            cvModule.TryGetValue("TrackNum", out tracknum);
            cvModule.TryGetValue("TrackGroup", out trackgroup);
            cv.SetTrackInfo(trackgroup, tracknum);

            //220628 HHJ SCS 개선		//- PLCDataItems 개선
            cvModule.TryGetValue("PLCNum", out plcnum);
            cv.SetPLCNum(short.Parse(plcnum));

            //20230131 RGJ 계산으로 PLC 어드레스 획득 주석 처리
            //cvModule.TryGetValue("BaseAddress", out baseaddress);
            //if (!string.IsNullOrEmpty(baseaddress))
            //{
            //    cv.SetBaseAddress(Convert.ToUInt16(baseaddress, 10));
            //}

            //220803 조숭진 cv use 추가
            cvModule.TryGetValue("USE", out use);
            cv.CVUSE = Convert.ToBoolean(use);

            //220921 조숭진 db에서 읽어오기
            //230410 RGJ TimeOut Setting 한번만 수행


            cvModule.TryGetValue("FireShutterPos", out fireShutterPos); //화재 셔터 포지션 설정값 가져온다. UDLR 형식으로
            cv.SetFireShutterPos(fireShutterPos);

            //220726 조숭진 사용안함 s
            ////턴모듈 OperatorIF모듈 bypass 여부 입력
            //if (cv.CVModuleType == eCVType.Turn || cv.CVModuleType == eCVType.OperatorIF || cv.CVModuleType == eCVType.TurnEQIF || cv.CVModuleType == eCVType.TurnOHTIF)    //2021.05.24 lim, TurnOHT 추가
            //{
            //    cv.SetBypassMode(bool.Parse(CVXML.Attribute("BypassMode").Value));
            //}
            //220726 조숭진 사용안함 e


            return cv;
        }
        //20220728 조숭진 config 방식 변경 e

        //20220728 조숭진 config 방식 변경 s
        //private RFID_ModuleBase CreateRFIDModule(XElement RFXML)
        //{
        //    eRFIDComType RFIDComType = (eRFIDComType)Enum.Parse(typeof(eRFIDComType), RFXML.Attribute("CommunicationType").Value, true);

        //    Type type = Type.GetType("BoxPrint.Modules.RFID." + RFXML.Attribute("RFIDType").Value);

        //    RFID_ModuleBase RFID = Activator.CreateInstance(type, RFXML.Attribute("ModuleName").Value, RFIDComType, SimulMode) as RFID_ModuleBase;

        //    RFID.SetCommunicationAddress(RFXML.Attribute("IP")?.Value, RFXML.Attribute("Port")?.Value, RFXML.Attribute("COMPort")?.Value);

        //    return RFID;
        //}
        //private RFID_ModuleBase CreateRFIDModule(Dictionary<string, string> rfid)
        //{
        //    string commtype, rfidtype, modulename, ip, port, comport;

        //    rfid.TryGetValue("CommunicationType", out commtype);
        //    eRFIDComType RFIDComType = (eRFIDComType)Enum.Parse(typeof(eRFIDComType), commtype, true);
        //    rfid.TryGetValue("RFIDType", out rfidtype);
        //    Type type = Type.GetType("BoxPrint.Modules.RFID." + rfidtype);
        //    rfid.TryGetValue("ModuleName", out modulename);
        //    RFID_ModuleBase RFID = Activator.CreateInstance(type, modulename, RFIDComType, SimulMode) as RFID_ModuleBase;

        //    rfid.TryGetValue("IP", out ip);
        //    rfid.TryGetValue("Port", out port);
        //    rfid.TryGetValue("COMPort", out comport);
        //    RFID.SetCommunicationAddress(ip, port, comport);

        //    Add(RFID);      //220823 조숭진 db config에서 읽어오기 위해 RFID_ModuleBase로 옮김
            
        //    return RFID;
        //}
        private RFID_ModuleBase CreateRFIDModule(Dictionary<string, string> unit)
        {
            string commtype, rfidtype, modulename, ip, port, comport;

            unit.TryGetValue("CommunicationType", out commtype);
            eUnitComType UnitComType = (eUnitComType)Enum.Parse(typeof(eUnitComType), commtype, true);
            unit.TryGetValue("RFIDType", out rfidtype);
            Type type = Type.GetType("BoxPrint.Modules.UNIT." + rfidtype);
            unit.TryGetValue("ModuleName", out modulename);
            RFID_ModuleBase UNIT = Activator.CreateInstance(type, modulename, UnitComType, SimulMode) as RFID_ModuleBase;

            unit.TryGetValue("IP", out ip);
            unit.TryGetValue("Port", out port);
            unit.TryGetValue("COMPort", out comport);
            UNIT.SetCommunicationAddress(ip, port, comport);

            Add(UNIT);      //220823 조숭진 db config에서 읽어오기 위해 RFID_ModuleBase로 옮김

            return UNIT;
        }
        //20220728 조숭진 config 방식 변경 e

        private string GetCVClassNamebyType(eCVType CVType)
        {
            {
                return string.Format("CV_{0}Module", CVType);
            }
        }

        public int CalcPortZoneCapa(string ZoneName)
        {
            //220802 조숭진 cvuse 추가
            int Capa = AllCVList.Where(s => s.CarrierExist == false && s.iZoneName == ZoneName && s.CVAvailable).Count();
            return Capa;
        }
        public int CalcShelfZoneTotalCount(string ZoneName)
        {
            //220803 조숭진 shelfuse 추가
            int Capa = AllCVList.Where(s => s.iZoneName == ZoneName).Count();
            return Capa;
        }
        public void RequestPortTypeChange(string TargetPort, ePortInOutType direction)
        {
            Task.Factory.StartNew(() =>
            {
                Thread.Sleep(200);
                var cv = GetCVModule(TargetPort);
                cv.SetDirection(direction);
                GlobalData.Current.HSMS.SendS6F11(406, "PORT", cv); //S6F1 PortTypeChanged CEID = 406
            });
        }

        //220620 HHJ SCS 개선     //- Search Page 추가
        public List<string> GetRobotIFCV()
        {
            List<string> listCVName = new List<string>();
            try
            {
                foreach (CV_BaseModule cv in AllCVList)
                {
                    if (cv.CVModuleType.Equals(eCVType.RobotIF)) //220921 INOUT 관계없음
                        listCVName.Add(cv.ControlName);
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
                listCVName = new List<string>();
            }

            return listCVName;
        }

        //20220728 조숭진 config 방식 변경 s
        private bool SetDBCVInfoFromXML()
        {
            //string portConfigPath = GlobalData.Current.CurrentFilePaths(System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName)) + GlobalData.Current.PortConfigPath;
            XmlDocument xmldoc = new XmlDocument();
            //int cvlinenum = 0;

            try
            {
                xmldoc.Load(portConfigPath);
                foreach (XmlNode firstnode in xmldoc.ChildNodes)
                {
                    if (firstnode.Attributes != null)
                    {
                        //for (int i = 0; i < firstnode.Attributes.Count; i++)
                        //{
                        //    //firstnode.Attributes[0].
                        //    GlobalData.Current.DBManager.DbSetProcedureConfigInfo(firstnode.Name, firstnode.Attributes[i].Name, firstnode.Attributes[i].Value, string.Empty, string.Empty);
                        //}

                        int cvlinenum = 0;
                        if (firstnode.ChildNodes.Count != 0)
                        {
                            string cvlinename = string.Empty;

                            //foreach(XmlNode secondnode in firstnode)
                            for (int j = 0; j < firstnode.ChildNodes.Count; j++)
                            {
                                var element1 = firstnode.ChildNodes[j];
                                if (element1.NodeType == XmlNodeType.Element)
                                {
                                    ++cvlinenum;
                                    //string section1 = element1.Name;// + cvlinenum.ToString();
                                    cvlinename = Regex.Replace(element1.Name, @"[0-9.]", "");
                                    string section1 = cvlinename + cvlinenum.ToString();
                                    for (int i = 0; i < element1.Attributes.Count; i++)
                                    {
                                        //var temp = firstnode.ChildNodes[j].Attributes[i];
                                        var temp = element1.Attributes[i];

                                        GlobalData.Current.DBManager.DbSetProcedureConfigInfo(section1, temp.Name, temp.Value, string.Empty, string.Empty);

                                    }

                                    int cvmodulenum = 0;
                                    if (element1.ChildNodes.Count != 0)
                                    {
                                        string cvmodulename = string.Empty;

                                        for (int o = 0; o < element1.ChildNodes.Count; o++)
                                        {
                                            var element2 = element1.ChildNodes[o];
                                            if (element2.NodeType == XmlNodeType.Element)
                                            {
                                                ++cvmodulenum;
                                                //string section2 = section1 + "." + element2.Name;// + cvmodulenum.ToString();
                                                cvmodulename = Regex.Replace(element2.Name, @"[0-9.]", "");
                                                string section2 = section1 + "." + cvmodulename + cvmodulenum.ToString();

                                                for (int t = 0; t < element2.Attributes.Count; t++)
                                                {
                                                    var temp = element2.Attributes[t];
                                                    GlobalData.Current.DBManager.DbSetProcedureConfigInfo(section2, temp.Name, temp.Value, string.Empty, string.Empty);
                                                }

                                                int cvbcrnum = 0;
                                                if (element2.ChildNodes.Count != 0)
                                                {
                                                    for (int q = 0; q < element2.ChildNodes.Count; q++)
                                                    {
                                                        string cvbcrname = string.Empty;

                                                        var element3 = element2.ChildNodes[q];
                                                        if (element3.NodeType == XmlNodeType.Element)
                                                        {
                                                            ++cvbcrnum;
                                                            //string section3 = section2 + "." + element3.Name;// + cvbcrnum.ToString();
                                                            cvbcrname = Regex.Replace(element3.Name, @"[0-9.]", "");
                                                            string section3 = section2 + "." + cvbcrname + cvbcrnum.ToString();
                                                            for (int w = 0; w < element3.Attributes.Count; w++)
                                                            {
                                                                var temp2 = element3.Attributes[w];
                                                                GlobalData.Current.DBManager.DbSetProcedureConfigInfo(section3, temp2.Name, temp2.Value, string.Empty, string.Empty);

                                                                if (w + 1 == element3.Attributes.Count)
                                                                {
                                                                    //string temp = Regex.Replace(element3.Name, @"[0-9.]", "");
                                                                    //string typename = section3.Substring(0, section3.Length - 1);
                                                                    string typename = section2 + "." + cvbcrname;
                                                                    GlobalData.Current.DBManager.DbSetProcedureConfigInfo(typename, "Count", cvbcrnum.ToString(), string.Empty, string.Empty);
                                                                }
                                                            }
                                                        }
                                                        else if (element3.NodeType == XmlNodeType.Attribute)
                                                        {
                                                        }
                                                    }
                                                }
                                            }
                                            else if (element2.NodeType == XmlNodeType.Attribute)
                                            {

                                            }

                                            if (o + 1 == element1.ChildNodes.Count)
                                            {
                                                //string temp = Regex.Replace(element2.Name, @"[0-9.]", "");
                                                string typename = section1 + "." + cvmodulename;
                                                //typename = typename.Substring(0, typename.Length - 1);
                                                GlobalData.Current.DBManager.DbSetProcedureConfigInfo(typename, "Count", cvmodulenum.ToString(), string.Empty, string.Empty);
                                            }
                                        }

                                    }
                                }
                                else if (element1.NodeType == XmlNodeType.Attribute)
                                {

                                }

                                if (j + 1 == firstnode.ChildNodes.Count)
                                {
                                    string typename = cvlinename;
                                    //typename = Regex.Replace(typename, @"[0-9.]", "");
                                    //typename = typename.Substring(0, typename.Length - 1);
                                    GlobalData.Current.DBManager.DbSetProcedureConfigInfo(typename, "Count", cvlinenum.ToString(), string.Empty, string.Empty);
                                }
                            }
                        }
                    }
                    else
                    {
                        if (firstnode.NodeType == XmlNodeType.Element)
                        {
                            foreach (XmlNode secondnode in firstnode)
                            {

                            }
                        }
                        else if (firstnode.NodeType == XmlNodeType.Attribute)
                        {

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
                return false;
            }

            return true;
        }

        private bool portconfigLoad()
        {
            if (GlobalData.Current.ServerClientType == eServerClientType.Client)
                return false;

            bool bresult = false;
            bool configExist = File.Exists(portConfigPath);
            if (!configExist)
            {
                throw new FileNotFoundException(portConfigPath + "파일을 찾을수 없습니다.");
            }

            XElement portsElement = XElement.Load(portConfigPath);

            foreach (XAttribute portXe in portsElement.Attributes())
            {
                if (portXe.Name == "Change" && portXe.Value.Equals("true", StringComparison.OrdinalIgnoreCase))
                {
                    bresult = true;
                    portXe.Value = "false";
                    portsElement.Save(portConfigPath);
                    break;
                }
            }

            return bresult;
        }

        private void Configmodify()
        {
            XElement portsElement = XElement.Load(portConfigPath);

            foreach (XAttribute portXe in portsElement.Attributes())
            {
                if (portXe.Name == "Change")
                {
                    portXe.Value = "true";
                    portsElement.Save(portConfigPath);
                    break;
                }
            }
        }

        public bool portconfigModify(string configtypename, string configname, string configvalue)
        {
            XmlDocument xmldoc = new XmlDocument();
            bool bChange = false;
            try
            {
                xmldoc.Load(portConfigPath);

                string[] cvarray = Regex.Split(configtypename, @"[_.]");
                int[] cvarraynum = new int[cvarray.Length];
                string temp = string.Empty;

                for (int i = 0; i < cvarray.Length; i++)
                {
                    temp = cvarray[i];
                    temp = Regex.Replace(temp, @"[^0-9.]", "");

                    if (!string.IsNullOrEmpty(temp))
                        cvarraynum[i] = Convert.ToInt32(temp);
                }

                foreach (XmlNode firstnode in xmldoc.ChildNodes)
                {
                    if (firstnode.NodeType != XmlNodeType.Element)
                        continue;

                    if (cvarraynum.Length == 1)
                    {
                        for (int i = 0; i < firstnode.ChildNodes.Count; i++)
                        {
                            if (firstnode.ChildNodes[i].Name != configtypename)
                                continue;
                            else
                            {
                                XmlNode modifynode = firstnode.ChildNodes[i].Attributes.GetNamedItem(configname);
                                modifynode.Value = configvalue;
                                bChange = true;
                                break;
                            }
                        }
                    }
                    else if (cvarraynum.Length == 2)
                    {
                        for (int i = 0; i < firstnode.ChildNodes.Count; i++)
                        {
                            if (firstnode.ChildNodes[i].Name != cvarray[0])
                                continue;
                            else
                            {
                                for (int j = 0; j < firstnode.ChildNodes[i].ChildNodes.Count; j++)
                                {
                                    if (firstnode.ChildNodes[i].ChildNodes[j].Name != cvarray[1])
                                        continue;
                                    else
                                    {
                                        XmlNode modifynode = firstnode.ChildNodes[i].ChildNodes[j].Attributes.GetNamedItem(configname);
                                        modifynode.Value = configvalue;
                                        bChange = true;
                                        break;
                                    }
                                }
                                break;
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < firstnode.ChildNodes.Count; i++)
                        {
                            if (firstnode.ChildNodes[i].Name != cvarray[0])
                                continue;
                            else
                            {
                                for (int j = 0; j < firstnode.ChildNodes[i].ChildNodes.Count; j++)
                                {
                                    if (firstnode.ChildNodes[i].ChildNodes[j].Name != cvarray[1])
                                        continue;
                                    else
                                    {
                                        for (int p = 0; p < firstnode.ChildNodes[i].ChildNodes[j].ChildNodes.Count; p++)
                                        {
                                            if (firstnode.ChildNodes[i].ChildNodes[j].ChildNodes[p].Name != cvarray[2])
                                                continue;
                                            else
                                            {

                                                XmlNode modifynode = firstnode.ChildNodes[i].ChildNodes[j].ChildNodes[p].Attributes.GetNamedItem(configname);
                                                modifynode.Value = configvalue;
                                                bChange = true;
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                if (bChange)
                {
                    xmldoc.Save(portConfigPath);
                    Configmodify();
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
                return false;
            }
            return true;
        }
        //20220728 조숭진 config 방식 변경 e

        //220919 조숭진 함수 수정 s
        //public bool portUSEModify(CVLineModule CVLine)
        public bool portConfigDetailModify(CVLineModule CVLine, string propName)
        {
            XmlDocument xmldoc = new XmlDocument();
            string firstsection = string.Empty, section = string.Empty;

            try
            {
                xmldoc.Load(portConfigPath);

                foreach (XmlNode firstnode in xmldoc.ChildNodes)
                {
                    if (firstnode.NodeType != XmlNodeType.Element)
                        continue;

                    for (int i = 0; i < firstnode.ChildNodes.Count; i++)
                    {
                        if (!firstnode.ChildNodes[i].Name.Contains("CVLine"))
                            continue;

                        XmlNode temp = firstnode.ChildNodes[i].Attributes.GetNamedItem("ModuleName");
                        if (!temp.Value.Equals(CVLine.ControlName))
                            continue;

                        firstsection = firstnode.ChildNodes[i].Name;
                        temp = firstnode.ChildNodes[i];

                        for (int j = 0; j < temp.ChildNodes.Count; j++)
                        {
                            if (propName == "USE")
                            {
                                XmlNode confignode = temp.ChildNodes[j].Attributes.GetNamedItem("USE");
                                CV_BaseModule selcv = GetCVModule(temp.ChildNodes[j].Attributes[0].Value);
                                if (selcv != null)
                                {
                                    confignode.Value = selcv.CVUSE.ToString().ToLower();
                                    section = string.Format("{0}.{1}", firstsection, temp.ChildNodes[j].Name);
                                    GlobalData.Current.DBManager.DbSetProcedureConfigInfo(section, "USE", confignode.Value, string.Empty, string.Empty);
                                }
                                
                            }
                            else if (propName == "Direction")
                            {
                                XmlNode confignode = temp.ChildNodes[j].Attributes.GetNamedItem("Direction");
                                CV_BaseModule selcv = GetCVModule(temp.ChildNodes[j].Attributes[0].Value);
                                if (selcv != null)
                                {
                                    confignode.Value = selcv.PortInOutType.ToString();
                                    section = string.Format("{0}.{1}", firstsection, temp.ChildNodes[j].Name);
                                    GlobalData.Current.DBManager.DbSetProcedureConfigInfo(section, "Direction", confignode.Value, string.Empty, string.Empty);
                                }
                            }
                        }

                        break;
                    }
                }

                xmldoc.Save(portConfigPath);
                Configmodify();
                GlobalData.Current.ConfigDataRefresh();
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
                return false;
            }
            return true;
        }
        //220919 조숭진 함수 수정 e

        //220823 조숭진 db config에서 읽어오기 위해 RFID_ModuleBase로 옮김 s
        public void Add(RFID_ModuleBase BCR)
        {
            lock (BcrAccessLock)
            {
                _BCRList.Add(BCR.ModuleName.ToString(), BCR);
            }
        }
        public void Add(TCP_ModuleBase Unit)
        {
            lock (BcrAccessLock)
            {
                _UnitList.Add(Unit.ModuleName.ToString(), Unit);
            }
        }

        //public bool BCRTimeoutSetting()
        //{
        //    lock (BcrAccessLock)
        //    {
        //        string value = string.Empty;

        //        foreach (var item in BCRList)
        //        {
        //            //if (mGdata.DBManager.DbGetConfigInfo("BCR", "ReadTimeOut", out value))
        //            if (mGdata.DBManager.DbGetGlobalConfigValue("BCR", "ReadTimeOut", out value))
        //            {
        //                item.Value.ReadTimeOut = Convert.ToInt32(value);
        //                value = string.Empty;
        //            }
        //            else
        //            {
        //                item.Value.ReadTimeOut = 10;
        //                mGdata.DBManager.DbSetProcedureConfigInfo("BCR", "ReadTimeOut", item.Value.ReadTimeOut.ToString());
        //            }

        //            //if (mGdata.DBManager.DbGetConfigInfo("BCR", "MaxRetry", out value))
        //            if (mGdata.DBManager.DbGetGlobalConfigValue("BCR", "MaxRetry", out value))
        //            {
        //                item.Value.MaxRetry = Convert.ToInt32(value);
        //                value = string.Empty;
        //            }
        //            else
        //            {
        //                item.Value.MaxRetry = 0;
        //                mGdata.DBManager.DbSetProcedureConfigInfo("BCR", "MaxRetry", item.Value.MaxRetry.ToString());
        //            }

        //        }
        //    }
        //    return true;
        //}
        //220823 조숭진 db config에서 읽어오기 위해 RFID_ModuleBase로 옮김 e

        //220921 조숭진 각종 time관련 db로 옮김. s
        public void TimeSetting(bool Init)
        {
            string value = string.Empty;

            //if (mGdata.DBManager.DbGetConfigInfo("CVLine", "PLCTimeout", out value))
            if (mGdata.DBManager.DbGetGlobalConfigValue("CVLine", "PLCTimeout", out value))
            {
                PLCTimeout = Convert.ToInt32(value);
                value = string.Empty;
            }
            else
            {
                PLCTimeout = 5;
                mGdata.DBManager.DbSetProcedureConfigInfo("CVLine", "PLCTimeout", PLCTimeout.ToString());
            }

            //if(mGdata.DBManager.DbGetConfigInfo("CVLine", "LocalStepCycleDelay", out value))
            if (mGdata.DBManager.DbGetGlobalConfigValue("CVLine", "LocalStepCycleDelay", out value))
            {
                LocalStepCycleDelay = Convert.ToInt32(value);
                value = string.Empty;
            }
            else
            {
                LocalStepCycleDelay = 50;
                mGdata.DBManager.DbSetProcedureConfigInfo("CVLine", "LocalStepCycleDelay", LocalStepCycleDelay.ToString());
            }

            //if(mGdata.DBManager.DbGetConfigInfo("CVLine", "ValidationWaitTime", out value))
            if (mGdata.DBManager.DbGetGlobalConfigValue("CVLine", "ValidationWaitTime", out value))
            {
                ValidationWaitTime = Convert.ToInt32(value);
                value = string.Empty;
            }
            else
            {
                ValidationWaitTime = 5;
                mGdata.DBManager.DbSetProcedureConfigInfo("CVLine", "ValidationWaitTime", ValidationWaitTime.ToString());
            }
            ValidationWaitTime = 120; //임시

            if (mGdata.DBManager.DbGetGlobalConfigValue("CVLine", "CarrierIDCheckTime", out value))
            {
                CarrierIDCheckTime = Convert.ToInt32(value);
                value = string.Empty;
            }
            else
            {
                CarrierIDCheckTime = 10;
                mGdata.DBManager.DbSetProcedureConfigInfo("CVLine", "CarrierIDCheckTime", CarrierIDCheckTime.ToString());
            }

            if (!Init)
            {
                TimeoutSet();
            }
        }

        private void TimeoutSet()
        {
            CV_BaseModule.CarrierIDCheckTimeOutSet(CarrierIDCheckTime);
            CV_BaseModule.PLCTimeoutSet(PLCTimeout);
            CV_BaseModule.ValidationWaitTimeSet(ValidationWaitTime);
            CV_BaseModule.LocalStepCycleDelaySet(LocalStepCycleDelay);
        }
        //220921 조숭진 각종 time관련 db로 옮김. e

        public void CraneOutOfServiceAction(bool IsFirstRM)
        {
            if(GlobalData.Current.SCSType == eSCSType.Dual)
            {
                //int FMaxBay = ShelfManager.Instance.FrontData.MaxBay;
                //int RMaxBay = ShelfManager.Instance.RearData.MaxBay;
                //int HalfBay = (Math.Max(FMaxBay, RMaxBay) + 1) / 2; //기준 정보가 없어서 일단 반분할 함.
                int FirstRMExBay = GlobalData.Current.Scheduler.RM1_ExclusiveBay; //전용 포트만 대상으로 한다.
                int SecondRMExBay = GlobalData.Current.Scheduler.RM2_ExclusiveBay;
                if (IsFirstRM)
                {
                    var TargetPorts = GlobalData.Current.PortManager.AllCVList.Where(cv => cv.CVModuleType == eCVType.RobotIF && cv.Position_Bay <= FirstRMExBay && cv.CVAvailable);
                    foreach (CV_BaseModule port in TargetPorts)
                    {
                        port.NotifyCraneMode(eCraneSCMode.MANUAL_RUN);
                        //GlobalData.Current.HSMS.SendS6F11(402, "PORT", port); //PortOutService 402
                        port.RequestOutserviceReport();
                    }
                }
                else
                {
                    var TargetPorts = GlobalData.Current.PortManager.AllCVList.Where(cv => cv.CVModuleType == eCVType.RobotIF && cv.Position_Bay > SecondRMExBay && cv.CVAvailable);
                    foreach (CV_BaseModule port in TargetPorts)
                    {
                        port.NotifyCraneMode(eCraneSCMode.MANUAL_RUN);
                        //GlobalData.Current.HSMS.SendS6F11(402, "PORT", port); //PortOutService 402
                        port.RequestOutserviceReport();
                    }
                }
            }
            else //전체 연결 포트 대상
            {
                var TargetPorts = GlobalData.Current.PortManager.AllCVList.Where(cv => cv.CVModuleType == eCVType.RobotIF && cv.CVAvailable);
                foreach (CV_BaseModule port in TargetPorts)
                {
                    port.NotifyCraneMode(eCraneSCMode.MANUAL_RUN);
                    //GlobalData.Current.HSMS.SendS6F11(402, "PORT", port); //PortOutService 402
                    port.RequestOutserviceReport();
                }
            }
            
        }
        public void CraneInServiceAction(bool IsFirstRM)
        {
            if (GlobalData.Current.SCSType == eSCSType.Dual)
            {
                //int FMaxBay = ShelfManager.Instance.FrontData.MaxBay;
                //int RMaxBay = ShelfManager.Instance.RearData.MaxBay;
                //int HalfBay = (Math.Max(FMaxBay, RMaxBay) + 1) / 2; //기준 정보가 없어서 일단 반분할 함.
                int FirstRMExBay = GlobalData.Current.Scheduler.RM1_ExclusiveBay; //전용 포트만 대상으로 한다.
                int SecondRMExBay = GlobalData.Current.Scheduler.RM2_ExclusiveBay;

                if (IsFirstRM)
                {
                    var TargetPorts = GlobalData.Current.PortManager.AllCVList.Where(cv => cv.CVModuleType == eCVType.RobotIF && cv.Position_Bay <= FirstRMExBay); // && cv.CVAvailable); //240528 RGJ 크레인 인/아웃 서비스 할때 연결 된 모든 포트에 알려주어야 함. CVAvailable 은 보고때 다시 체크
                    foreach (CV_BaseModule port in TargetPorts)
                    {
                        if (!port.CheckModuleHeavyAlarmExist() && port.AutoManualState == eCVAutoManualState.AutoRun) //정상 상태인 포트만 인서비스 보고
                        {
                            //GlobalData.Current.HSMS.SendS6F11(401, "PORT", port); //PortInService 401
                            port.RequestInserviceReport();
                        }
                        port.NotifyCraneMode(eCraneSCMode.AUTO_RUN); //모드 변경상태는 다 준다.
                    }
                }
                else
                {
                    var TargetPorts = GlobalData.Current.PortManager.AllCVList.Where(cv => cv.CVModuleType == eCVType.RobotIF && cv.Position_Bay >= SecondRMExBay); // && cv.CVAvailable); //240528 RGJ 크레인 인/아웃 서비스 할때 연결 된 모든 포트에 알려주어야 함. CVAvailable 은 보고때 다시 체크
                    foreach (CV_BaseModule port in TargetPorts)
                    {
                        if (!port.CheckModuleHeavyAlarmExist() && port.AutoManualState == eCVAutoManualState.AutoRun)  //정상 상태인 포트만 인서비스 보고
                        {
                            //GlobalData.Current.HSMS.SendS6F11(401, "PORT", port); //PortInService 401
                            port.RequestInserviceReport();
                        }
                        port.NotifyCraneMode(eCraneSCMode.AUTO_RUN); //모드 변경상태는 다 준다.
                    }
                }
            }
            else //전체 연결 포트 대상
            {
                var TargetPorts = GlobalData.Current.PortManager.AllCVList.Where(cv => cv.CVModuleType == eCVType.RobotIF);  // && cv.CVAvailable); //240528 RGJ 크레인 인/아웃 서비스 할때 연결 된 모든 포트에 알려주어야 함. CVAvailable 은 보고때 다시 체크
                foreach (CV_BaseModule port in TargetPorts)
                {
                    if (!port.CheckModuleHeavyAlarmExist() && port.AutoManualState == eCVAutoManualState.AutoRun) //정상 상태인 포트만 인서비스 보고
                    {
                        //GlobalData.Current.HSMS.SendS6F11(401, "PORT", port); //PortInService 401
                        port.RequestInserviceReport();
                    }
                    port.NotifyCraneMode(eCraneSCMode.AUTO_RUN); //모드 변경상태는 다 준다.
                }
            }
        }

        /// <summary>
        /// 해당 캐리어 사이즈를 내보낼만한 적절한 포트를 찾는다.
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public CV_BaseModule GetProperOutPort(ICarrierStoreAble Source, eCarrierSize Size,bool NoHandOver = false)
        {
            //전용 구역 제한 기능 추후 추가필요. 
            var TargetPorts = GlobalData.Current.PortManager.AllCVList.Where(cv => cv.CVModuleType == eCVType.RobotIF && !cv.IsInPort && cv.CVAvailable && cv.CheckCarrierSizeAcceptable(Size));//포트상태까지는 지금 체크 안함.
            if (TargetPorts.Count() <= 0) //Put 가능한 포트가 없다.
            {
                return null;
            }
            CV_BaseModule DestPort = TargetPorts.OrderBy(p => p.CalcDistance(p, Source)).FirstOrDefault();
            return DestPort;
        }

        //230510 프로그램 실행 시 portlayout.xml 파일을 갱신한다.
        private void PortConfigSettingRenewal(Dictionary<string, CVLineModule> ModuleList)
        {
            XmlDocument xmldoc = new XmlDocument();

            if (GlobalData.Current.ServerClientType == eServerClientType.Client)
                return;

            try
            {
                xmldoc.Load(portConfigPath);

                for (int i = 0; i < xmldoc.ChildNodes.Count; i++)
                {
                    var temp = xmldoc.ChildNodes[i];

                    if (temp.NodeType != XmlNodeType.Element)
                        continue;
                    else
                    {
                        bool bInit = false;
                        for (int count = 0; count < temp.ChildNodes.Count; count++)
                        {
                            if (temp.ChildNodes[count].NodeType != XmlNodeType.Element)
                                continue;

                            XmlAttributeCollection tempAttribute = temp.ChildNodes[count].Attributes;

                            if (!ModuleList.ContainsKey(tempAttribute["ModuleName"].Value))
                            {
                                bInit = true;
                                break;
                            }
                        }

                        if (!bInit)
                            return;

                        int delcount = temp.ChildNodes.Count;
                        for (int j = 0; j < delcount; j++)
                        {
                            temp.RemoveChild(temp.ChildNodes[0]);
                        }

                        XmlNode newNode;
                        XmlElement xmlEle;

                        int CVLineNum = 1;

                        foreach (var item in ModuleList)
                        {
                            newNode = xmldoc.ChildNodes[i];

                            xmlEle = xmldoc.CreateElement(string.Format("CVLine{0}", CVLineNum));

                            XmlAttribute xmlAtb;

                            xmlAtb = xmldoc.CreateAttribute("ModuleName");
                            xmlAtb.Value = item.Value.ModuleName;
                            xmlEle.SetAttributeNode(xmlAtb);

                            xmlAtb = xmldoc.CreateAttribute("LineType");
                            xmlAtb.Value = item.Value.LineType.ToString();
                            xmlEle.SetAttributeNode(xmlAtb);

                            xmlAtb = xmldoc.CreateAttribute("Bank");
                            xmlAtb.Value = item.Value.Position_Bank.ToString();
                            xmlEle.SetAttributeNode(xmlAtb);

                            xmlAtb = xmldoc.CreateAttribute("Bay");
                            xmlAtb.Value = item.Value.Position_Bay.ToString();
                            xmlEle.SetAttributeNode(xmlAtb);

                            xmlAtb = xmldoc.CreateAttribute("Level");
                            xmlAtb.Value = item.Value.Position_Level.ToString();
                            xmlEle.SetAttributeNode(xmlAtb);

                            xmlAtb = xmldoc.CreateAttribute("LineFloor");
                            xmlAtb.Value = item.Value.LineFloor;
                            xmlEle.SetAttributeNode(xmlAtb);

                            xmlAtb = xmldoc.CreateAttribute("Rotate");
                            xmlAtb.Value = item.Value.CVRotate.ToString();
                            xmlEle.SetAttributeNode(xmlAtb);

                            xmlAtb = xmldoc.CreateAttribute("CVWay");
                            xmlAtb.Value = item.Value.CVWay.ToString();
                            xmlEle.SetAttributeNode(xmlAtb);

                            newNode.AppendChild(xmlEle);

                            if (item.Value.ModuleList.Count != 0)
                            {
                                newNode = xmlEle;
                                int cvnum = 1;

                                foreach (var cvitem in item.Value.ModuleList)
                                {
                                    xmlEle = xmldoc.CreateElement(string.Format("Conveyor{0}", cvnum));

                                    xmlAtb = xmldoc.CreateAttribute("ModuleName");
                                    xmlAtb.Value = cvitem.ModuleName;
                                    xmlEle.SetAttributeNode(xmlAtb);

                                    xmlAtb = xmldoc.CreateAttribute("USE");
                                    xmlAtb.Value = cvitem.CVUSE.ToString();
                                    xmlEle.SetAttributeNode(xmlAtb);

                                    xmlAtb = xmldoc.CreateAttribute("CVType");
                                    xmlAtb.Value = cvitem.CVModuleType.ToString();
                                    xmlEle.SetAttributeNode(xmlAtb);

                                    xmlAtb = xmldoc.CreateAttribute("TrackGroup");
                                    xmlAtb.Value = cvitem.TrackGroup.ToString();
                                    xmlEle.SetAttributeNode(xmlAtb);

                                    xmlAtb = xmldoc.CreateAttribute("TrackNum");
                                    xmlAtb.Value = string.Format("{0:D3}", cvitem.TrackNum);
                                    xmlEle.SetAttributeNode(xmlAtb);

                                    xmlAtb = xmldoc.CreateAttribute("PortTableID");
                                    if (cvitem.PortTableID == 0)
                                    {
                                        xmlAtb.Value = "-1";
                                    }
                                    else
                                    {
                                        xmlAtb.Value = cvitem.PortTableID.ToString();
                                    }
                                    xmlEle.SetAttributeNode(xmlAtb);

                                    xmlAtb = xmldoc.CreateAttribute("PortSize");
                                    xmlAtb.Value = cvitem.PortSize.ToString();
                                    xmlEle.SetAttributeNode(xmlAtb);

                                    xmlAtb = xmldoc.CreateAttribute("Direction");
                                    xmlAtb.Value = cvitem.PortInOutType.ToString();
                                    xmlEle.SetAttributeNode(xmlAtb);

                                    xmlAtb = xmldoc.CreateAttribute("PLCNum");
                                    xmlAtb.Value = cvitem.PLCNum.ToString();
                                    xmlEle.SetAttributeNode(xmlAtb);

                                    newNode.AppendChild(xmlEle);

                                    if (cvitem.CVRFIDModule != null)
                                    {
                                        XmlNode oldNode;
                                        oldNode = xmlEle.ParentNode;
                                        newNode = xmlEle;

                                        xmlEle = xmldoc.CreateElement("RFID1");

                                        xmlAtb = xmldoc.CreateAttribute("ModuleName");
                                        xmlAtb.Value = cvitem.CVRFIDModule.ModuleName;
                                        xmlEle.SetAttributeNode(xmlAtb);

                                        Type type = cvitem.CVRFIDModule.GetType();
                                        xmlAtb = xmldoc.CreateAttribute("RFIDType");
                                        xmlAtb.Value = type.Name;
                                        xmlEle.SetAttributeNode(xmlAtb);

                                        xmlAtb = xmldoc.CreateAttribute("CommunicationType");
                                        xmlAtb.Value = cvitem.CVRFIDModule.ComType.ToString();
                                        xmlEle.SetAttributeNode(xmlAtb);

                                        xmlAtb = xmldoc.CreateAttribute("IP");
                                        xmlAtb.Value = cvitem.CVRFIDModule.IPAddress;
                                        xmlEle.SetAttributeNode(xmlAtb);

                                        xmlAtb = xmldoc.CreateAttribute("Port");
                                        xmlAtb.Value = cvitem.CVRFIDModule.PortNumber;
                                        xmlEle.SetAttributeNode(xmlAtb);

                                        newNode.AppendChild(xmlEle);

                                        newNode = oldNode;
                                    }
                                    ++cvnum;
                                }
                            }
                            ++CVLineNum;
                        }
                    }
                }
                xmldoc.Save(portConfigPath);
            }
            catch (Exception ex)
            {
                _ = ex;
            }
        }
    }

    public static class DispatcherService
    {
        public static void Invoke(Action action)
        {
            Dispatcher dispatchObject = Application.Current != null ? Application.Current.Dispatcher : null;
            if (dispatchObject == null || dispatchObject.CheckAccess())
                action();
            else
                dispatchObject.Invoke(action);
        }
    }
}
