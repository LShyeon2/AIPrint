
using BoxPrint.Log;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BoxPrint.Scheduler
{

    public class ClientScheduler : BaseScheduler
    {
        public ClientScheduler()
        {
            string value = string.Empty;
            SchedulerName = "ClientScheduler";
        }

        public Thread t;
        private bool threadExit = true;
        /// <summary>
        /// 스케쥴러 초기화.
        /// </summary>
        public override void InitScheduler()
        {
            InitData();
            t = new Thread(new ThreadStart(SchedulerAutoRun));
            t.Name = "SchedulerAutoRun";
            t.IsBackground = true;
            t.Start();
        }

        /// <summary>
        /// 데이타 초기화
        /// </summary>
        private void InitData()
        {

        }

        public override void MapChangeForExitThread()
        {
            threadExit = false;
        }

        /// <summary>
        /// 스케쥴러 루프
        /// </summary>
        public override void SchedulerAutoRun()
        {
            //230801 클라이언트 스케줄러 시작할 때 접속했다는 정보를 남긴다.
            GlobalData.Current.DBManager.DbSetProcedureConnectClientInfo();
            GlobalData.Current.MRE_MapViewChangeEvent.WaitOne();

            while (threadExit)
            {
                try
                {
                    //221102 YSW EQPList 초기화
                    if (!GlobalData.Current.DBManager.IsConnect)
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "{0} SchedulerAutoRun Holding State - DB Disconnected",SchedulerName);
                        Thread.Sleep(1000);
                        continue;
                    }

                    GlobalData.Current.DBManager.DbGetProcedureEQPInfo();
                    //디비 검색할 EQPID 가 없으면 안된다
                    if (string.IsNullOrEmpty(GlobalData.Current.EQPID))
                    {
                        Thread.Sleep(CycleTime);
                        continue;
                    }

                    switch (GlobalData.Current.SendTagEvent)
                    {
                        case "MainPage":

                            if (GlobalData.Current.DBManager.IsConnect)
                            {
                                GlobalData.Current.CarrierStore.GetCarrierItemsFromDB();

                                GlobalData.Current.ShelfMgr.GetShelfData();

                                GlobalData.Current.McdList = GlobalData.Current.DBManager.DbGetProcedureJobInfo();

                                GlobalData.Current.protocolManager.GetPLCDataInfoFromDB();
                            }
                            break;

                        case "Alarm":
                            break;

                        case "Config":
                            //GlobalData.Current.ConfigDataRefresh();
                            break;

                        case "RMParameter":
                        case "RMPval":
                        case "AxisState":
                            break;

                        case "FirmwareLog":
                            break;

                        case "AlarmLog":
                            break;

                        case "TransferLog":
                            break;

                        case "BcrNgLog":
                            break;

                        case "SecsLog":
                            break;

                        case "OperatorLog":
                            break;

                        case "InformLog":
                            break;

                        case "WPSMonitor":
                            break;

                        case "Ports":
                            break;

                        case "User":
                            break;

                        case "CarrierSearch":
                            break;

                        case "MapViewer":
                            break;


                        case "Login":
                            break;
                        case "AlarmManager":
                            break;

                        case "EXIT":
                            break;

                        default:
                            break;

                    }

                    Thread.Sleep(500); //230420 RGJ 디비 부하 감소 위해 갱신 딜레이 늘림 300 -> 500
                }
                catch (Exception ex)
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
                }
            }

            //t.Join(); //241121 RGJ 자기가 돌던 쓰레드 조인 기다리는것 무의미 하므로 삭제.
        }
    }
}
