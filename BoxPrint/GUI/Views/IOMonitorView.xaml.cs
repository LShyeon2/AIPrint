using BoxPrint.CCLink;
using BoxPrint.Log;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using TranslationByMarkupExtension;

namespace BoxPrint.GUI.Views
{

    // 2020.09.24 RGJ
    /// <summary>
    /// IOMonitorView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class IOMonitorView : Page
    {
        private readonly ushort IOPointCounterPerPage = 16;

        private int CurrentInputPage = 1;
        private int MaxInputPage = 1;
        private int CurrentOutputPage = 1;
        private int MaxOutputPage = 1;

        private string strtag = string.Empty;
        DispatcherTimer IOtimer = new DispatcherTimer();
        private delegate void D_Set_StringValue(string nValue);

        private List<IOPoint> CurrentViewList;

        public IOMonitorView()
        {
            InitializeComponent();
            GlobalData.Current.SendTagChange += Current_ReceiveEvent;
            InitializeMenu();

        }
        private void Current_ReceiveEvent(object sender, EventArgs e)
        {
            string JInfo = (string)sender;
            this.Dispatcher.Invoke(new D_Set_StringValue(_DisplayChange), JInfo);
        }


        //SuHwan_20220707 : 여기서 참조할것들
        //CV_BaseModule cv = GlobalData.Current.PortManager.GetCVModule(vm.SelValue.Location.ToString());

        //<PLCDataItem ModuleType = "CV"
        //Area="PLCtoPC"	
        //DataType="Short"	
        //Device="W"	
        //ItemName="PLC_SCSMode"				
        //AddressOffset="69"		
        //BitOffset="0"	
        //Size="1"/>

        private void _DisplayChange(string strtag)
        {
            try
            {
                this.strtag = strtag;
                initLoad(strtag);
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Fatal, ex.ToString());
            }
        }

        private void initLoad(string tag)
        {
            //220621 HHJ SCS 개선     //- IO MonitorPage 이동 추가
            //ChangeIOGroup(GlobalData.Current.MainBooth.ModuleName); //처음 시작할때는 부스를 보여준다.
            if (!tag.Equals("IOMonitor"))
                CallModuleID = string.Empty;

            if (string.IsNullOrEmpty(CallModuleID))
                ChangeIOGroup(GlobalData.Current.MainBooth.ModuleName); //처음 시작할때는 부스를 보여준다.
            else
                ChangeIOGroup(CallModuleID);

            //IO 모니터링용 타이머 시작
            IOtimer.Interval = TimeSpan.FromMilliseconds(100);    //시간간격 0.1초.필요시 가감.
            IOtimer.Tick += new EventHandler(IOtimer_Tick);          //이벤트 추가
            IOtimer.Start();

        }

        private void IOtimer_Tick(object sender, EventArgs e)
        {
            //보드 부하감소를 위해서 I/O 화면 보일때만 I/O 갱신
            if (this.IsVisible)
            {
                foreach (var item in CurrentViewList)
                {
                    item.LastReadValue = CCLinkManager.CCLCurrent.ReadIO(item.ModuleID, item.Name);
                }
            }
        }


        private void InitializeMenu()
        {
            stackpanelMenuFrame.Children.Clear();

            Expander expanderBoth = new Expander { Header = GlobalData.Current.MainBooth.ModuleName, Style = Resources["ExpanderStyle"] as Style };
            expanderBoth.Content = GetMenuItem("Booth", GlobalData.Current.MainBooth.ModuleName);
            stackpanelMenuFrame.Children.Add(expanderBoth);

            foreach (var item in GlobalData.Current.mRMManager.ModuleList)
            {
                Expander expanderRM = new Expander { Header = item.Value.ModuleName, Style = Resources["ExpanderStyle"] as Style };
                expanderRM.Content = GetMenuItem("RM1", item.Value.ModuleName);
                stackpanelMenuFrame.Children.Add(expanderRM);
            }

            foreach (var item in GlobalData.Current.PortManager.ModuleList)
            {
                Expander expanderPort = new Expander { Header = item.Value.ModuleName, Style = Resources["ExpanderStyle"] as Style };
                expanderPort.Content = GetMenuItem("Port", item.Value.ModuleName);
                stackpanelMenuFrame.Children.Add(expanderPort);
            }
        }

        private StackPanel GetMenuItem(string rcvModuleType, string rcvModuleName)
        {
            StackPanel stackPanelBuffer = new StackPanel();

            switch (rcvModuleType)
            {
                case "Booth":
                    stackPanelBuffer.Children.Add(new ExpanderItemControl { DisplayName = rcvModuleName, TagName = rcvModuleName, Style = Resources["ExpanderItemStyle"] as Style });
                    break;

                case "RM1":
                    stackPanelBuffer.Children.Add(new ExpanderItemControl { DisplayName = rcvModuleName, TagName = rcvModuleName, Style = Resources["ExpanderItemStyle"] as Style });
                    break;

                case "Port":
                    var moduleListBuffer = GlobalData.Current.PortManager.ModuleList[rcvModuleName];
                    foreach (var cvItem in moduleListBuffer.ModuleList)
                    {
                        stackPanelBuffer.Children.Add(new ExpanderItemControl { DisplayName = cvItem.ModuleName, TagName = cvItem.ModuleName, Style = Resources["ExpanderItemStyle"] as Style });
                    }
                    break;
            }

            return stackPanelBuffer;
        }

        private void MenuItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border senderBuffer)
            {
                ChangeIOGroup(senderBuffer.Tag.ToString());
            }
        }

        private void ChangeIOGroup(string moduleID)
        {
            if (string.IsNullOrEmpty(moduleID))
            {
                return;
            }

            textblockInputName.Text  = TranslationManager.Instance.Translate("I/O Input").ToString()  + " - " + moduleID;
            textblockOutputName.Text = TranslationManager.Instance.Translate("I/O Output").ToString() + " - " + moduleID;

            CurrentViewList = GlobalData.Current.CCLink_mgr.GetModuleIOList(moduleID);

            //이미 모듈명이랑 그룹은 필터링 했으므로 In,Out 만 구분
            var InputGroup = CurrentViewList.Where(R => R.Direction == eIODirectionTypeList.In);
            var OutputGroup = CurrentViewList.Where(R => R.Direction == eIODirectionTypeList.Out);

            MaxInputPage = (InputGroup.Count() / IOPointCounterPerPage);
            MaxInputPage += (InputGroup.Count() % IOPointCounterPerPage) == 0 ? 0 : 1; //나머지가 있으면 페이지 추가.

            MaxOutputPage = (OutputGroup.Count() / IOPointCounterPerPage);
            MaxOutputPage += (OutputGroup.Count() % IOPointCounterPerPage) == 0 ? 0 : 1; //나머지가 있으면 페이지 추가.

            ChangeIOPage(1, true);
            ChangeIOPage(1, false);


        }
        private void ChangeIOPage(int page, bool IsInput)
        {
            if (IsInput) //Input IO
            {
                if (page > MaxInputPage)
                {
                    CurrentInputPage = 1;
                }
                else if (page < 1)
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


        /// <summary>
        /// 셀 더블 클릭시 IO 토글 명령 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DataGrid_DoubleClick(object sender, MouseButtonEventArgs e)
        {

            if (sender is DataGrid senderBuffer)
            {

                if (senderBuffer.SelectedItem != null)
                {
                    IOPoint IO = senderBuffer.SelectedItem as IOPoint;
                    if (IO != null)
                    {
                        bool IOValue = CCLinkManager.CCLCurrent.ReadIO(IO.ModuleID, IO.Name);

                        CCLinkManager.CCLCurrent.WriteIO(IO.ModuleID, IO.Name, !IOValue);
                    }
                }

            }

        }



        private void PageMove_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button senderBuffer)
            {
                switch ((string)senderBuffer.Tag)
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
        }

        //220621 HHJ SCS 개선     //- IO MonitorPage 이동 추가
        private string CallModuleID = string.Empty;
        public void MenuItemCall(string modid)
        {
            CallModuleID = modid;
        }

    }

    public class ExpanderItemControl : Control, INotifyPropertyChanged
    {

        //표시되는 이름
        private string _displayName;
        public string DisplayName
        {
            get { return _displayName; }
            set
            {
                if (_displayName != value)
                {
                    _displayName = value;
                    OnPropertyChanged("DisplayName");
                }
            }
        }

        //테그 이름
        private string _tagName;
        public string TagName
        {
            get { return _tagName; }
            set
            {
                if (_tagName != value)
                {
                    _tagName = value;
                    OnPropertyChanged("TagName");
                }
            }
        }

        //재산변경
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}
