using Stockerfirmware.Database;
using Stockerfirmware.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Stockerfirmware.DataList.LifeTime
{
    public class PartsLifeManager
    {
        private string DefalutRM = "RM1";
        private object DataSync = new object();
        private bool bThreadExit = false;
        private int ThreadDelay = 2000; //갱신 주기 

        private List<PartsLifeItem> PartsList;
        public PartsLifeManager()
        {
            //GlobalData.Current.mRMManager[DefalutRM].ProcessCompleted += PartsLifeManager_ProcessCompleted;
            //PartsList = new List<PartsLifeItem>();
            //DataBaseManager.Current.GetPartsLifeTimeData(PartsList);
            
            //Thread LifeThread = new Thread(new ThreadStart(LifeTimeRun));
            //LifeThread.Start();
        }

        private void CreatePortsLifeTime(int startID)
        {
            int ID = startID;
            foreach (var item in GlobalData.Current.LineManager.ModuleList)
            {
                foreach (var cvItem in item.Value.ModuleList)
                {
                    DataBaseManager.Current.AddPartsLifeTime(ID++, cvItem.ModuleName, "CV_BELT", "TTBU 375 T5-150","쌍림멀티텍","컨베이어 벨트","횟수",3000000);
                    DataBaseManager.Current.AddPartsLifeTime(ID++, cvItem.ModuleName, "CV_TIMINGBELT", "FRBL-2ESD [35*820]", "쌍림멀티텍", "타이밍 벨트", "횟수", 3000000);
                    DataBaseManager.Current.AddPartsLifeTime(ID++, cvItem.ModuleName, "CV_MOTOR", "S9I90GTL", "SPG", "컨베이어모터", "횟수", 15000000);
                    DataBaseManager.Current.AddPartsLifeTime(ID++, cvItem.ModuleName, "CV_REDUCER", "S9KC9B", "SPG", "컨베이어 감속기", "횟수", 15000000);
                }
            }
        }

        private void PartsLifeManager_ProcessCompleted(object sender, Modules.RM.RMModuleBase.ProcessEventArgs e)
        {
            if (e == null)
                return;
            lock (DataSync)
            {
                try
                {
                    var ZAxis = PartsList.Where(p => p.PartsName.Contains("RM_Z_")); //높이축
                    foreach (var part in ZAxis)
                    {
                        if (part.MeasurementUnits == "KM")
                        {
                            part.CurrentValue += (double)e.Z_Moving_Distance / 1000000; //거리를 누적한다.
                        }
                        else
                        {
                            part.CurrentValue += 1; //횟수를 더한다.
                        }
                    }

                    var ForkAxis = PartsList.Where(p => p.PartsName.Contains("RM_A_")); //포크축
                    foreach (var part in ForkAxis)
                    {
                        if (part.MeasurementUnits == "KM")
                        {
                            part.CurrentValue += (double)e.Fork_Moving_Distance / 1000000; //거리를 누적한다.
                        }
                        else
                        {
                            part.CurrentValue += 1; //횟수를 더한다.
                        }
                    }

                    var XAxis = PartsList.Where(p => p.PartsName.Contains("RM_X_")); //주행축
                    foreach (var part in XAxis)
                    {
                        if (part.MeasurementUnits == "KM")
                        {
                            part.CurrentValue += (double)e.Drive_Moving_Distance / 1000000; //거리를 누적한다.
                        }
                        else
                        {
                            part.CurrentValue += 1; //횟수를 더한다.
                        }
                    }
                    var TurnAxis = PartsList.Where(p => p.PartsName.Contains("RM_T_")); //턴축
                    foreach (var part in TurnAxis)
                    {
                        if (part.MeasurementUnits == "KM")
                        {
                            part.CurrentValue += (double)e.Turn_Moving_Distance / 1000000; //거리를 누적한다.
                        }
                        else
                        {
                            part.CurrentValue += 1; //횟수를 더한다.
                        }
                    }
                    var GripAxis = PartsList.Where(p => p.PartsName.Contains("RM_C_")); //그립축
                    foreach (var part in GripAxis)
                    {
                        if (part.MeasurementUnits == "KM")
                        {
                            part.CurrentValue += (double)e.Gripper_Moving_Distance / 1000000; //거리를 누적한다.
                        }
                        else
                        {
                            part.CurrentValue += 1; //횟수를 더한다.
                        }
                    }
                }
                catch(Exception ex)
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
                }
            }
        }

        private void LifeTimeRun()
        {
            while (!bThreadExit) 
            {
                try
                {
                    foreach (var Lineitem in GlobalData.Current.LineManager.ModuleList) //포트 모듈 전체 순회
                    {
                        foreach (var CVitem in Lineitem.Value.ModuleList)
                        {
                            if (CVitem.LifeTime_BeltCounter > 0)
                            {
                                var part = PartsList.Find(p => p.ModuleName == CVitem.ModuleName && p.PartsName == "CV_BELT");
                                if(part== null) //리스트의 명단 정의 안되었으면 건너 뛴다.
                                {
                                    CVitem.ResetLifeTimeCounter();
                                    continue;
                                }
                                PartsList.Find(p => p.ModuleName == CVitem.ModuleName && p.PartsName == "CV_BELT").CurrentValue += CVitem.LifeTime_BeltCounter; //변수 하나로 통합
                                PartsList.Find(p => p.ModuleName == CVitem.ModuleName && p.PartsName == "CV_TIMINGBELT").CurrentValue += CVitem.LifeTime_BeltCounter;
                                PartsList.Find(p => p.ModuleName == CVitem.ModuleName && p.PartsName == "CV_MOTOR").CurrentValue += CVitem.LifeTime_BeltCounter;
                                PartsList.Find(p => p.ModuleName == CVitem.ModuleName && p.PartsName == "CV_REDUCER").CurrentValue += CVitem.LifeTime_BeltCounter;
                            }

                            CVitem.ResetLifeTimeCounter();
                        }
                    }
                    lock (DataSync)
                    {
                        //데이터 베이스에 쓴다.
                        DataBaseManager.Current.UpdateAllParts(PartsList);
                    }
                    //소모량 오버된 항목이 있으면 팝업알림
                    if (PartsList.Where(p => p.LifeOver).Count() > 0)
                    {
                        MainWindow.GetMainWindowInstance().RequestShowLifeTimeWarning();
                    }
                    Thread.Sleep(ThreadDelay);
                }
                catch(Exception ex)
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
                }
            }

        }

        public void PartsResetRequest(string ModuleName,string PartName)
        {
            var Part = PartsList.Find(p => p.ModuleName == ModuleName && p.PartsName == PartName);
            Part.CurrentValue = 0;
        }
        public List<PartsLifeItem> GetModuleList(string ModuleID)
        {
            if(ModuleID == "ALL")
            {
                return PartsList;
            }
            var iList = PartsList.Where(R => R.ModuleName == ModuleID);
            return iList.ToList<PartsLifeItem>();
        }

        public List<PartsLifeItem> GetLifeOverList()
        {
            var iList = PartsList.Where(R => R.LifeOver);
            return iList.ToList<PartsLifeItem>();
        }
    }
}
