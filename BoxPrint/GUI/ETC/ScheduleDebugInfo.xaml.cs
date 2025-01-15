
using BoxPrint.SimulatorPLC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace BoxPrint.GUI.ETC
{
    /// <summary>
    /// ScheduleDebugInfo.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ScheduleDebugInfo : Window
    {
        List<TextBlock> TbList;
        DispatcherTimer timer = new DispatcherTimer();
        BaseSimulator SelectedSimulPLC = null;
        public ScheduleDebugInfo()
        {
            InitializeComponent();
            TbList = new List<TextBlock>();
            int maxCo = GlobalData.Current.ShelfMgr.GetMaxBay() + 2;
            //Border 
            for (int i = 0; i < maxCo; i++)
            {
                TextBlock tb = new TextBlock();
                tb.Text = string.Format("{0:D2}", (i));
                tb.HorizontalAlignment = HorizontalAlignment.Center;
                tb.VerticalAlignment = VerticalAlignment.Center;

                tb.TextAlignment = TextAlignment.Center;
                tb.Margin = new Thickness(9, 6, 9, 6);
                tb.Background = System.Windows.Media.Brushes.LightGray;
                TbList.Add(tb);
                SP_Reserve.Children.Add(tb);
            }
            if(GlobalData.Current.GlobalSimulMode)
            {
                DG_PLCSimulator.ItemsSource = PLCSimulatorManager.Instance.PLCSimulList;
            }
            
            //모니터링용 타이머 시작
            timer.Interval = TimeSpan.FromMilliseconds(100);
            timer.Tick += new EventHandler(timer_Tick);          //이벤트 추가
            timer.Start();
        }

        private void timer_Tick(object sender, EventArgs e)
        {

            int RM1Bay = GlobalData.Current.mRMManager.FirstRM.CurrentBay;
            int RM2Bay = -10;
            if (GlobalData.Current.SCSType == eSCSType.Dual)
            {
                RM2Bay = GlobalData.Current.mRMManager.SecondRM.CurrentBay;
            }
            for (int i = 0; i < TbList.Count(); i++)
            {
                if (getDiffABS(RM1Bay, i) <= 2)
                {
                    TbList[i].Background = System.Windows.Media.Brushes.Yellow;
                }
                else if (getDiffABS(RM2Bay, i) <= 2)
                {
                    TbList[i].Background = System.Windows.Media.Brushes.LawnGreen;
                }
                else
                {
                    TbList[i].Background = System.Windows.Media.Brushes.LightGray;
                }
            }
            var rm1Job = GlobalData.Current.Scheduler.RM1_OnProcessJob;
            var rm2Job = GlobalData.Current.Scheduler.RM2_OnProcessJob;
            tb_SC_RM1Step.Text = rm1Job == null ? "Crane 1 Job waiting..." : rm1Job.Step.ToString();
            tb_SC_RM2Step.Text = rm2Job == null ? "Crane 2 Job waiting..." : rm2Job.Step.ToString();

            tb_SC_RM1_Job.Text = rm1Job == null ? "Not Assigned" : rm1Job.CommandID;
            tb_SC_RM2_Job.Text = rm2Job == null ? "Not Assigned" : rm2Job.CommandID;
        }
        private int getDiffABS(int a, int b)
        {
            int c = a - b;
            if (c >= 0)
                return c;
            else
                return -c;
        }

        private void Btn_PLCStart_Click(object sender, RoutedEventArgs e)
        {
            SelectedSimulPLC?.StartSimulPLC();
        }

        private void Btn_PLCStop_Click(object sender, RoutedEventArgs e)
        {
            SelectedSimulPLC?.StopSimulPLC();
        }

        private void Btn_Crane_EMO_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedSimulPLC is CraneSimulator cs)
            {
                cs.SetAlarmCode(1);
            }
        }


        private void Btn_RemovePortCarrier_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedSimulPLC is PortSimulator ps)
            {
                ps.RemoveSimulCarrier();
            }

        }

        private void Btn_CreatePortCarrier_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedSimulPLC is PortSimulator ps)
            {
                ps.CreateNewSimulCarrier();
            }
        }

        private void DG_PLCSimulator_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is DataGrid senderBuffer)
            {
                SelectedSimulPLC = senderBuffer.SelectedItem as BaseSimulator;

                if (SelectedSimulPLC != null)
                {
                    tb_SelectedSimulModule.Text = SelectedSimulPLC.SPLC_Name;
                }
                else
                {
                    tb_SelectedSimulModule.Text = "NONE";
                }

            }
        }



        private void Btn_PLCAllStart_Click(object sender, RoutedEventArgs e)
        {
            PLCSimulatorManager.Instance.StartSimulPLCModules();
        }

        private void Btn_PLCAllStop_Click(object sender, RoutedEventArgs e)
        {
            PLCSimulatorManager.Instance.StopSimulPLCModules();
        }

        private void Btn_Del_OnprocessJob2_Click(object sender, RoutedEventArgs e)
        {
            GlobalData.Current.Scheduler.RM2_OnProcessJob?.SetJobForceAbort();
        }

        private void Btn_Del_OnprocessJob1_Click(object sender, RoutedEventArgs e)
        {
            GlobalData.Current.Scheduler.RM1_OnProcessJob?.SetJobForceAbort();
        }

        private void Btn_PLCReSet_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedSimulPLC is PortSimulator ps)
            {
                ps.ResetSimulCarrier();
            }
        }

        private void Btn_CreateUnrod_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedSimulPLC is PortSimulator ps)
            {

                GlobalData.Current.PortManager.GetCVModule(SelectedSimulPLC.SPLC_Name);
            }

        }

        private void Btn_CraneSourceEmpty_Test_Click(object sender, RoutedEventArgs e)
        {
            if(SelectedSimulPLC is CraneSimulator cs)
            {
                cs.EmptyRetriveTestMode = true;
            }
        }

        private void Btn_CraneDoubleStorage_Test_Click(object sender, RoutedEventArgs e)
        {
            if(SelectedSimulPLC is CraneSimulator cs)
            {
                cs.DoubleStorageTestMode = true;
            }
        }
        private void Btn_Port_EMO_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedSimulPLC is PortSimulator ps)
            {
                ps.SetAlarmCode(1346);
            }
        }

        private void Btn_CraneAlarmClear_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedSimulPLC is CraneSimulator cs)
            {
                cs.TryAlarmClear();
            }
        }

        private void Btn_PortAlarmClear_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedSimulPLC is PortSimulator ps)
            {
                ps.TryAlarmClear();
            }
        }

        private void Btn_CreateCarrierCont_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedSimulPLC is PortSimulator ps)
            {
                ps.ToggleGenerationMode();
            }
        }

        private void Btn_CranePortIFError_Test_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedSimulPLC is CraneSimulator cs)
            {
                cs.PortIFErrorTestMode = true;
            }
        }

        private void Btn_OneRackMode_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedSimulPLC is CraneSimulator cs)
            {
                cs.SetOneRackPauseMode(!cs.GetOneRackPauseMode());
            }
        }

        private void Btn_ForkFire_Test_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedSimulPLC is CraneSimulator cs)
            {
                cs.ForkFireTest = true;
            }
        }

        private void Btn_CarrierUpdate_Click(object sender, RoutedEventArgs e)
        {
            Log.LogManager.WriteConsoleLog(eLogLevel.Info, $"Module {tb_UpdateTargetModule.Text}, ID {tb_UpdateCarrierID.Text} Forced CarrierUpdate Click");
            var target = GlobalData.Current.GetGlobalCarrierStoreAbleObject(tb_UpdateTargetModule.Text);
            if(target != null)
            {
                if(string.IsNullOrEmpty(tb_UpdateCarrierID.Text))
                {
                    string carrierID = target.GetCarrierID();
                    if (!string.IsNullOrEmpty(target.GetCarrierID()))
                    {
                        DataList.CarrierStorage.Instance.RemoveStorageCarrier(carrierID);
                        target.ResetCarrierData();
                    }
                }
                else
                {
                    DataList.CarrierItem cItem = new DataList.CarrierItem() { CarrierID = tb_UpdateCarrierID.Text };
                    DataList.CarrierStorage.Instance.InsertCarrier(cItem);

                    target.UpdateCarrier(tb_UpdateCarrierID.Text);
                }

            }
        }

        private void Btn_PLC_LogToggle_Click(object sender, RoutedEventArgs e)
        {
            if(GlobalData.Current.WritePLCRawLog)
            {
                GlobalData.Current.WritePLCRawLog = false;
                Btn_PLC_LogToggle.Content = "Start PLC Log Write";
            }
            else
            {
                GlobalData.Current.WritePLCRawLog = true;
                Btn_PLC_LogToggle.Content = "Stop PLC Log Write";
            }
        }

        private void Btn_PLC_BackupLine_Click(object sender, RoutedEventArgs e)
        {
            if (GlobalData.Current.DebugUseBackupPLC)
            {
                GlobalData.Current.DebugUseBackupPLC = false;
                Btn_PLC_BackupLine.Content = "Start BackupLine Use";
            }
            else
            {
                GlobalData.Current.DebugUseBackupPLC = true;
                Btn_PLC_BackupLine.Content = "Stop BackupLine Use";
            }

        }
        //241023 HoN Crane InSlot 강제 Add, Del 기능 추가
        private void Btn_InSlotAdd(object sender, RoutedEventArgs e)
        {
            if (SelectedSimulPLC is CraneSimulator cs)
            {
                string CarrierID = "TESTCARRIER";
                
                BoxPrint.Modules.RM.RMModuleBase testRM = null;
                if (cs.SPLC_Name.Contains("CRANE1"))
                {
                    testRM = GlobalData.Current.mRMManager.FirstRM;
                }
                else
                {
                    testRM = GlobalData.Current.mRMManager.SecondRM;
                }

                DataList.CarrierItem cItem = new DataList.CarrierItem() { CarrierID = CarrierID };
                DataList.CarrierStorage.Instance.InsertCarrier(cItem);
                testRM.UpdateCarrier(CarrierID, false, false);
            }
        }
        private void Btn_InSlotDel(object sender, RoutedEventArgs e)
        {
            if (SelectedSimulPLC is CraneSimulator cs)
            {
                BoxPrint.Modules.RM.RMModuleBase testRM = null;
                if (cs.SPLC_Name.Contains("CRANE1"))
                {
                    testRM = GlobalData.Current.mRMManager.FirstRM;
                }
                else
                {
                    testRM = GlobalData.Current.mRMManager.SecondRM;
                }

                string carrierID = testRM.CarrierID;
                if (!string.IsNullOrEmpty(carrierID))
                {
                    DataList.CarrierStorage.Instance.RemoveStorageCarrier(carrierID);
                    testRM.ResetCarrierData();
                }
            }
        }

        private void Btn_PortModeChagne_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedSimulPLC is PortSimulator ps)
            {
                ps.ToggleAutoManual();
            }
        }
    }
}
