using Stockerfirmware.DataList;
using Stockerfirmware.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WCF_LBS.Commands;

namespace Stockerfirmware.Scheduler
{
    public class CycleScheduler : BaseScheduler
    {
        eTrayHeight CurrentScheduleHeight = eTrayHeight.Height0;
        public CycleScheduler()
        {
            SchedulerName = "CycleScheduler";
        }

        private CraneCommand GetNextCommand()
        {
            CraneCommand cmd = null;
            cmd = CheckShelf_PutCommand();
            if(cmd != null)
            {
                return cmd;
            }
            cmd = CheckShelf_GetCommand();
            if (cmd != null)
            {
                return cmd;
            }
            return cmd;
        }
        /// <summary>
        /// 스케쥴러 초기화.
        /// </summary>
        public override void InitScheduler()
        {
            InitData();
            Thread t = new Thread(new ThreadStart(SchedulerAutoRun));
            t.IsBackground = true;
            t.Start();
        }
        private void InitData()
        {
            CurrentScheduleHeight = eTrayHeight.Height0;
            foreach (var sItem in GlobalData.Current.MainBooth.FrontData)
            {
                sItem.Scheduled = false;
            }
            foreach (var sItem in GlobalData.Current.MainBooth.RearData)
            {
                sItem.Scheduled = false;
            }
        }
        public override void SchedulerAutoRun()
        {
            TargetRM = GlobalData.Current.mRMManager["RM1"];
            while (true)
            {
                if(!StartSignal)
                {
                    Thread.Sleep(CycleTime);
                    continue;
                }
                if(GlobalData.Current.MainBooth.BoothState != eBoothState.AutoStart)
                {
                    Thread.Sleep(CycleTime);
                    continue;
                }
                //if(GlobalData.Current.WCF_mgr.GetLCS_CommunicationState() == System.ServiceModel.CommunicationState.Opened) //LCS 연결 되어있으면 동작 안함.
                //{
                //    Thread.Sleep(CycleTime);
                //    continue;
                //}
                if(TargetRM.CheckCurrentComandExist())
                {
                    Thread.Sleep(CycleTime);
                    continue;
                }
                CraneCommand cmd = GetNextCommand();
                if(cmd != null)
                {
                    TargetRM.SetWCFCommand(cmd);
                }
                Thread.Sleep(CycleTime);
            }
        }
        private CraneCommand CheckShelf_PutCommand()
        {
            if(TargetRM.GetRMState() != eRMState.Initialized_Idle && !TargetRM.SimulMode)
            {
                return null;
            }
            if (TargetRM.Place_Sensor_Exist)
            {
                //높이가 맞는 front 쉘프를 찾는다.
                var TargetFrontShelf = GlobalData.Current.MainBooth.FrontData.Where(
                shelf => shelf.EXIST_STATE == (int)eCarrierExist.Empty &&
                !shelf.Scheduled && !shelf.DeadZone &&
                (int)TargetRM.CurrentTrayHeight == shelf.TrayHeight).FirstOrDefault();
                if (TargetFrontShelf != null)
                {
                    return new CraneCommand(WCF_LBS.enumMessageName.CRANE_PUT, WCF_LBS.enumCraneTarget.SHELF, TargetFrontShelf.ShelfBank, TargetFrontShelf.ShelfBay, TargetFrontShelf.ShelfLevel, "", TargetRM.CarrierID);
                }
                
                //높이가 맞는 rear 쉘프를 찾는다.
                var TargetRearShelf = GlobalData.Current.MainBooth.RearData.Where(
                    shelf => shelf.EXIST_STATE == (int)eCarrierExist.Empty &&
                    !shelf.Scheduled && !shelf.DeadZone &&
                    (int)TargetRM.CurrentTrayHeight == shelf.TrayHeight).FirstOrDefault();
                if (TargetRearShelf != null)
                {
                    return new CraneCommand(WCF_LBS.enumMessageName.CRANE_PUT, WCF_LBS.enumCraneTarget.SHELF, TargetRearShelf.ShelfBank, TargetRearShelf.ShelfBay, TargetRearShelf.ShelfLevel, "", TargetRM.CarrierID);
                }
            }
            return null;
        }
        private CraneCommand CheckShelf_GetCommand()
        {
            if (TargetRM.GetRMState() != eRMState.Initialized_Idle && !TargetRM.SimulMode)
            {
                return null;
            }
            if (!TargetRM.Place_Sensor_Exist)
            {
                //Get 작업전 넣을수 있는 쉘프가 있는지 확인.
                bool PutAbleFrontShelfExist =
                          GlobalData.Current.MainBooth.FrontData.Where(
                shelf => shelf.EXIST_STATE == (int)eCarrierExist.Empty &&
                !shelf.Scheduled && !shelf.DeadZone &&
                (int)CurrentScheduleHeight == shelf.TrayHeight).Count() > 0;

                bool PutAbleRearShelfExist =
                         GlobalData.Current.MainBooth.RearData.Where(
               shelf => shelf.EXIST_STATE == (int)eCarrierExist.Empty &&
               !shelf.Scheduled && !shelf.DeadZone &&
               (int)CurrentScheduleHeight == shelf.TrayHeight).Count() > 0;

                if (PutAbleFrontShelfExist || PutAbleRearShelfExist)
                {
                    //높이가 맞는 front 쉘프를 찾는다.
                    var TargetFrontShelf = GlobalData.Current.MainBooth.FrontData.Where(
                        shelf => shelf.EXIST_STATE == (int)eCarrierExist.Exist && !shelf.DeadZone &&
                        shelf.TrayHeight == (int)CurrentScheduleHeight).FirstOrDefault();
                    if (TargetFrontShelf != null)
                    {
                        return new CraneCommand(WCF_LBS.enumMessageName.CRANE_GET, WCF_LBS.enumCraneTarget.SHELF, TargetFrontShelf.ShelfBank, TargetFrontShelf.ShelfBay, TargetFrontShelf.ShelfLevel, "", "Test" + (int)CurrentScheduleHeight);
                    }

                    //높이가 맞는 rear 쉘프를 찾는다.
                    var TargetRearShelf = GlobalData.Current.MainBooth.RearData.Where(
                        shelf => shelf.EXIST_STATE == (int)eCarrierExist.Exist && !shelf.DeadZone &&
                        shelf.TrayHeight == (int)CurrentScheduleHeight).FirstOrDefault();
                    if (TargetRearShelf != null)
                    {
                        return new CraneCommand(WCF_LBS.enumMessageName.CRANE_GET, WCF_LBS.enumCraneTarget.SHELF, TargetRearShelf.ShelfBank, TargetRearShelf.ShelfBay, TargetRearShelf.ShelfLevel, "", "Test" + (int)CurrentScheduleHeight);
                    }
                }

                LogManager.WriteConsoleLog(eLogLevel.Info, "스케쥴러 : {0} 높이에 맞는 작업대상 쉘프가 없으므로 다음 높이로 작업을 바꿉니다.", CurrentScheduleHeight);
                //맞는 높이 쉘프가 없으므로 목표 쉘프 변경
                if (CurrentScheduleHeight == eTrayHeight.Height0 || CurrentScheduleHeight == eTrayHeight.Height1)
                {
                    CurrentScheduleHeight++;
                }
                else
                {   
                    CurrentScheduleHeight = eTrayHeight.Height0;
                    InitData();
                }
                LogManager.WriteConsoleLog(eLogLevel.Info, "스케쥴러 : {0} 높이로 작업 변경 되었습니다.", CurrentScheduleHeight);

            }
            return null;
        }

    }
}
