using System;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Controls;
using System.Windows.Input;
using System.Collections.Generic;
using Stockerfirmware.CCLink;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using Stockerfirmware.Modules.Conveyor;

namespace Stockerfirmware.GUI.Views
{

    // 2021.01.25 RGJ
    /// <summary>
    /// PortDetailWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class PortDetailWindow : Window
    {
        private CV_BaseModule CV;
        private readonly ushort IOPointCounterPerPage = 16;

        private int CurrentInputPage = 1;
        private int MaxInputPage = 1;
        private int CurrentOutputPage = 1;
        private int MaxOutputPage = 1;

        private string strtag = string.Empty;
        DispatcherTimer IOtimer = new DispatcherTimer();
        private delegate void D_Set_StringValue(string nValue);

        private List<IOPoint> CurrentViewList;
 
        public PortDetailWindow(CV_BaseModule cv)
        {
            CV = cv;
            
            InitializeComponent();

            //2021.05.24 lim, OHTRobot 포트는 컨베이어가 없음, TurnOHTIF 역회전 후방 스톱퍼 사용
            this.button_FL.IsEnabled = (CV.CVModuleType != eCVType.OHTRobot);

            this.button_RL.IsEnabled = (CV.CVModuleType == eCVType.Turn || CV.CVModuleType == eCVType.TurnEQIF || CV.CVModuleType == eCVType.TurnOHTIF); //역회전은  턴포트만 존재
            this.button_StopperFWD.IsEnabled = CV.UseStopper;
            this.button_StopperBWD.IsEnabled = CV.UseStopper && (CV.CVModuleType == eCVType.Turn || CV.CVModuleType == eCVType.OHTIF || CV.CVModuleType == eCVType.TurnEQIF || CV.CVModuleType == eCVType.TurnOHTIF);
            this.button_RFID_Read.IsEnabled = CV.UseBCR;
            this.button_Door_Open.IsEnabled = CV.UseDoor;
            
            //2021.04.08 lim,
            if (GlobalData.Current.UseBCR)
            {
                this.button_RFID_Read.Content = "BCR Read";
                this.lbl_RFIDReady.Content    = "BCR RD";
                this.lbl_RFIDComplete.Content = "BCR CP";
            }

            ChangeIOGroup(CV.ModuleName);
            //IO 모니터링용 타이머 시작
            IOtimer.Interval = TimeSpan.FromMilliseconds(100);       
            IOtimer.Tick += new EventHandler(IOtimer_Tick);          //이벤트 추가
            IOtimer.Start();
        }
        private void IOtimer_Tick(object sender, EventArgs e)
        {
            //보드 부하감소를 위해서 I/O 화면 보일때만 I/O 갱신
            if(this.IsVisible)
            {
                //I/O 갱신
                foreach(var item in CurrentViewList)
                {
                    item.LastReadValue = CCLinkManager.CCLCurrent.ReadIO(item.ModuleID, item.Name);
                }
                //버튼 상태 갱신
                button_StopperFWD.Content = CV.GetCVStopperState(true) == eCV_StopperState.Up ? "Stopper F Open" : "Stopper F Close";
                button_StopperBWD.Content = CV.GetCVStopperState(false) == eCV_StopperState.Up ? "Stopper B Open" : "Stopper B Close";
                button_Door_Open.Content = CV.GetCVDoorSolOnState() ? "Door Close" : "Door Open";
                //포트 상태 갱신
                tb_Mode.Text = (CV.AutoManualState == eCVAutoManualState.Auto) ? "AUTO" : "MANUAL";
                tb_Contain.Text = CV.CarrierExist ? "ON" : "OFF";
                tb_Request.Text = CV.RequestState;
                tb_Size.Text = CV.GetTrayHeight().ToString();
                tb_Ready.Text = CV.ReadyState;
                tb_Complete.Text = CV.CompleteState;
                tb_RFIDReady.Text = CV.CheckRFID_ReadTriggerSent() ? "Reading" : "";
                tb_RFIDComplete.Text = CV.CheckRFID_ReadCompleted() ? "Completed" : "";
                tb_CarrierID.Text = CV.GetCarrierID();
                tb_PortState.Text = CV.CheckModuleHeavyAlarmExist() ? "ERROR" : "NORMAL";
                tb_ErrorCode.Text = CV.GetModuleLastAlarmCode();
                tb_Step.Text = CV.LocalActionStep.ToString();
                tb_CurrentAction.Text = CV.CurrentActionDesc;
                tb_LastActionResult.Text = CV.LastActionResult;
            }
        }
        private void ChangeIOGroup(string moduleID)
        {
            if(string.IsNullOrEmpty(moduleID))
            {
                return;
            }
            CurrentViewList = GlobalData.Current.CCLink_mgr.GetModuleIOList(moduleID);

            //이미 모듈명이랑 그룹은 필터링 했으므로 In,Out 만 구분
            var InputGroup = CurrentViewList.Where(R => R.Direction == eIODirectionTypeList.In);
            var OutputGroup = CurrentViewList.Where(R => R.Direction == eIODirectionTypeList.Out);

            MaxInputPage = (InputGroup.Count() / IOPointCounterPerPage);
            MaxInputPage += (InputGroup.Count() % IOPointCounterPerPage) == 0 ? 0 : 1; //나머지가 있으면 페이지 추가.

            MaxOutputPage = (OutputGroup.Count() / IOPointCounterPerPage);
            MaxOutputPage += (OutputGroup.Count() % IOPointCounterPerPage) == 0 ? 0 : 1; //나머지가 있으면 페이지 추가.

            ChangeIOPage(1 , true);
            ChangeIOPage(1 , false);


        }
        private void ChangeIOPage(int page,bool IsInput)
        {
            if(IsInput) //Input IO
            {
                if (page > MaxInputPage)
                {
                    CurrentInputPage = 1;
                }
                else if(page < 1)
                {
                    CurrentInputPage = MaxInputPage;
                }
                else
                {
                    CurrentInputPage = page;
                }
                var InputPageGroup = CurrentViewList.Where(R => R.Direction == eIODirectionTypeList.In).Skip((CurrentInputPage - 1) * IOPointCounterPerPage);
                dataGrid_Input.ItemsSource = InputPageGroup.Take(IOPointCounterPerPage);
                Label_InputPage.Content = string.Format("{0} / {1}", CurrentInputPage, MaxInputPage);
            }
            else //Output IO
            {
                if (page > MaxOutputPage)
                {
                    CurrentOutputPage = 1;
                }
                else if (page < 1)
                {
                    CurrentOutputPage = MaxOutputPage;
                }
                else
                {
                    CurrentOutputPage = page;
                }
                var OutputPageGroup = CurrentViewList.Where(R => R.Direction == eIODirectionTypeList.Out).Skip((CurrentOutputPage - 1) * IOPointCounterPerPage);
                dataGrid_Output.ItemsSource = OutputPageGroup.Take(IOPointCounterPerPage);
                Label_OutputPage.Content = string.Format("{0} / {1}", CurrentOutputPage, MaxOutputPage);
            }
            
        }
        private void PageMove_Click(object sender, RoutedEventArgs e)
        {
            Control button = sender as Control;
            switch ((string)button.Tag)
            {
                case "OutputLeft":
                    ChangeIOPage(--CurrentOutputPage, false);
                    break;
                case "OutputRight":
                    ChangeIOPage(++CurrentOutputPage, false);
                    break;
                case "InputLeft":
                    ChangeIOPage(--CurrentInputPage, true);
                    break;
                case "InputRight":
                    ChangeIOPage(++CurrentInputPage, true);
                    break;
            }

        }
        /// <summary>
        /// 셀 더블 클릭시 IO 토글 명령 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DataGridCell_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            DataGridCell cell = e.Source as DataGridCell;

            if(cell != null )
            {
                IOPoint IO = cell.DataContext as IOPoint;
                if (IO != null)
                {
                    bool IOValue = CCLinkManager.CCLCurrent.ReadIO(IO.ModuleID, IO.Name);

                    CCLinkManager.CCLCurrent.WriteIO(IO.ModuleID, IO.Name, !IOValue);
                }
            }
          
        }


        private void button_FL_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            CV.CVForwardRun(eCV_Speed.Low);
        }

        private void button_FL_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            CV.CV_RunStop();
        }

        private void button_RL_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            CV.CVBackwardRun(eCV_Speed.Low);
        }

        private void button_RL_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            CV.CV_RunStop();
        }

        private void button_StopperFWD_Click(object sender, RoutedEventArgs e)
        {
            var state = CV.GetCVStopperState(true); //전방 스톱퍼 상태 획득
            if (state == eCV_StopperState.Up) //상태 반대로 동작
            {
                CV.CVStopperOpen();
            }
            else if (state == eCV_StopperState.Down)
            {
                CV.CVStopperClose();
            }
            else
            {
                //상태 안맞으면 동작안함.
            }
        }

        private void button_StopperBWD_Click(object sender, RoutedEventArgs e)
        {
            var state = CV.GetCVStopperState(false); //후방 스톱퍼 상태 획득
            if (state == eCV_StopperState.Up) //상태 반대로 동작
            {
                CV.CVStopperOpen(false);
            }
            else if (state == eCV_StopperState.Down)
            {
                CV.CVStopperClose(false);
            }
            else
            {
                //상태 안맞으면 동작안함.
            }
        }

        private void button_RFID_Read_Click(object sender, RoutedEventArgs e)
        {
            string RFIDReadValue = CV.CVBCR_Read();
            CV.UpdateCarrierTagID(RFIDReadValue);
        }

        private void button_Door_Open_Click(object sender, RoutedEventArgs e)
        {
            var state = CV.GetCVDoorSolOnState();
            if (state == true)
            {
                CV.CVDoorCloseSol();
            }
            else
            {
                if (GlobalData.Current.MainBooth.SCState == eSCState.AUTO)
                {
                    MessageBoxResult result = System.Windows.MessageBox.Show("현재 Auto 모드입니다. 장비를 정지하고 다시 시도 하세요.", "LBS Run 상태 체크", MessageBoxButton.OK);
                    return;
                }
                CV.CVDoorOpenSol();
            }
        }

        private void button_ErrorReset_Click(object sender, RoutedEventArgs e)
        {
            CV.CV_ErrorResetRequest();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)//ESC 누르면 바로 화면 끈다.
            {
                Close();
            }
        }
    }
   
 
}
