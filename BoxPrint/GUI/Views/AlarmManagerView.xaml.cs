
using BoxPrint.Alarm;
using BoxPrint.GUI.ETC;
using BoxPrint.GUI.EventCollection;
using BoxPrint.GUI.UIControls; //220916 조숭진
using BoxPrint.GUI.ViewModels;
using BoxPrint.GUI.Views.UserPage;
using BoxPrint.Log;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TranslationByMarkupExtension;

namespace BoxPrint.GUI.Views
{
    /// <summary>
    /// Page1.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class AlarmManagerView : Page, INotifyPropertyChanged
    {
        //이벤트 : AlarmManagerPopupView 에 선택된 알람 데이타를 보낸다
        public static event AlarmManagerPopupView.EventHandler_AlarmManagerDataChange _EventCall_AlarmManagerDataChange;//이벤트  
        //이벤트 :
        //AlarmManagerPopupView 에 팝업 생성 상태를 받아온다
        public delegate void EventHandler_PopupOpen(bool Message);
        public delegate void EventHandler_refreshDataGrid(string rcvModuleType);     //220711 조숭진

        private ObservableList<AlarmData> _CurrentAlarmList;
        private AlarmData _selectAlarmData;
        private bool _isPopupOpen;

        ////SuHwan_20221226 : [1차 UI검수] 폰트 사이즈 설정
        //protected int _UIFontSize_Large = 14;  //큰폰트
        //public int UIFontSize_Large
        //{
        //    get => _UIFontSize_Large;
        //    set
        //    {
        //        if (_UIFontSize_Large == value) return;
        //        _UIFontSize_Large = value;

        //        RaisePropertyChanged("UIFontSize_Large");
        //    }
        //}

        //protected int _UIFontSize_Medium = 12; //중간폰트
        //public int UIFontSize_Medium
        //{
        //    get => _UIFontSize_Medium;
        //    set
        //    {
        //        if (_UIFontSize_Medium == value) return;
        //        _UIFontSize_Medium = value;

        //        RaisePropertyChanged("UIFontSize_Medium");
        //    }
        //}

        //protected int _UIFontSize_Small = 10;  //작은폰트
        //public int UIFontSize_Small
        //{
        //    get => _UIFontSize_Small;
        //    set
        //    {
        //        if (_UIFontSize_Small == value) return;
        //        _UIFontSize_Small = value;

        //        RaisePropertyChanged("UIFontSize_Small");
        //    }
        //}

        private ViewModelAlarmManagerView vm;

        //재산변경 이벤트
        protected virtual void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public event PropertyChangedEventHandler PropertyChanged;

        //생성자
        public AlarmManagerView()
        {
            InitializeComponent();
            vm = new ViewModelAlarmManagerView();
            InitializeComboBox();

            //DataContext = this;
            DataContext = vm;

            //230102 YSW 사용자 권한에 따른 버튼 잠금
            ModifyAuthorityCheck();
            LogInPopupView._EventHandler_LoginChange += ModifyAuthorityCheck;
            GroupAccountManagementPage._EventHandler_ChangeAuthority += ModifyAuthorityCheck;

            AlarmManagerPopupView._EventCall_PopupOpen += new EventHandler_PopupOpen(this.setPopupState);
            AlarmManagerPopupView._EventCall_refreshDataGrid += new EventHandler_refreshDataGrid(this.refreshData);     //220711 조숭진
            GlobalData.Current.OnAlarmManagerViewRefreshed += Current_OnAlarmManagerViewRefreshed;
            UIEventCollection.Instance.OnCultureChanged += _UIEventCollection_OnCultureChanged;
        }

        private void _UIEventCollection_OnCultureChanged(string cultureKey)
        {
            switch (cultureKey)
            {
                case "ko-KR":
                    dataGridAlarmList.Columns[5].Visibility = Visibility.Visible;
                    dataGridAlarmList.Columns[6].Visibility = Visibility.Hidden;
                    dataGridAlarmList.Columns[7].Visibility = Visibility.Hidden;
                    dataGridAlarmList.Columns[8].Visibility = Visibility.Hidden;
                    break;
                case "en-US":
                    dataGridAlarmList.Columns[5].Visibility = Visibility.Hidden;
                    dataGridAlarmList.Columns[6].Visibility = Visibility.Visible;
                    dataGridAlarmList.Columns[7].Visibility = Visibility.Hidden;
                    dataGridAlarmList.Columns[8].Visibility = Visibility.Hidden;
                    break;
                case "zh-CN":
                    dataGridAlarmList.Columns[5].Visibility = Visibility.Hidden;
                    dataGridAlarmList.Columns[6].Visibility = Visibility.Hidden;
                    dataGridAlarmList.Columns[7].Visibility = Visibility.Visible;
                    dataGridAlarmList.Columns[8].Visibility = Visibility.Hidden;
                    break;
                case "hu-HU":
                    dataGridAlarmList.Columns[5].Visibility = Visibility.Hidden;
                    dataGridAlarmList.Columns[6].Visibility = Visibility.Hidden;
                    dataGridAlarmList.Columns[7].Visibility = Visibility.Hidden;
                    dataGridAlarmList.Columns[8].Visibility = Visibility.Visible;
                    break;
            }
        }

        private void Current_OnAlarmManagerViewRefreshed()
        {
            refreshData("All");

            vm.CurAlarmListRefresh();
        }

        //230119 YSW 수정권한 잠금
        public void ModifyAuthorityCheck()
        {
            if (GlobalData.Current.LoginUserAuthority.Contains("ModifyAlarmManager"))
            {
                ModifyAuthorityDockPanel.IsHitTestVisible = true;
                ModifyAuthorityDockPanel.Opacity = 1;
                LockIcon.Visibility = Visibility.Collapsed;
            }
            else
            {
                ModifyAuthorityDockPanel.IsHitTestVisible = false;
                ModifyAuthorityDockPanel.Opacity = 0.3;
                LockIcon.Visibility = Visibility.Visible;
            }
        }

        //콤보박스 초기화
        private void InitializeComboBox()
        {
            comboBoxModuleType.Items.Clear();

            try
            {
                _CurrentAlarmList = GlobalData.Current.Alarm_Manager.getAllAlarmList();
                List<string> listModuleType = new List<string>();

                foreach (var item in _CurrentAlarmList)
                {
                    if (!listModuleType.Contains(item.ModuleType))
                    {
                        listModuleType.Add(item.ModuleType);
                    }
                }

                ComboBoxItem comboBoxItemBuffer = new ComboBoxItem { Style = Resources["comboBoxItem_style"] as Style, Content = "All" };
                comboBoxModuleType.Items.Add(comboBoxItemBuffer);

                foreach (var item in listModuleType)
                {
                    comboBoxItemBuffer = new ComboBoxItem { Style = Resources["comboBoxItem_style"] as Style, Content = item };
                    comboBoxModuleType.Items.Add(comboBoxItemBuffer);
                }

                comboBoxModuleType.SelectedIndex = 0;
                ChangeDataGrid("All");

            }
            catch (Exception e)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, e.ToString());
            }
        }

        //데이타 그리드 내용 변경
        private void ChangeDataGrid(string rcvModuleType)
        {
            IEnumerable<AlarmData> sortAlarmList;
            _CurrentAlarmList = GlobalData.Current.Alarm_Manager.getAllAlarmList();
            dataGridAlarmList.Items.Clear();

            try
            {
                sortAlarmList = rcvModuleType == "All" ? _CurrentAlarmList : _CurrentAlarmList.Where(R => R.ModuleType == rcvModuleType);

                int listNumber = 1;
                foreach (var item in sortAlarmList)
                {
                    item.ListNo = listNumber;
                    dataGridAlarmList.Items.Add(item);
                    listNumber++;
                }
                switch(TranslationManager.Instance.CurrentLanguage.ToString())
                {
                    case "ko-KR":
                        dataGridAlarmList.Columns[5].Visibility = Visibility.Visible;
                        dataGridAlarmList.Columns[6].Visibility = Visibility.Hidden;
                        dataGridAlarmList.Columns[7].Visibility = Visibility.Hidden;
                        dataGridAlarmList.Columns[8].Visibility = Visibility.Hidden;
                        break;
                    case "en-US":
                        dataGridAlarmList.Columns[5].Visibility = Visibility.Hidden;
                        dataGridAlarmList.Columns[6].Visibility = Visibility.Visible;
                        dataGridAlarmList.Columns[7].Visibility = Visibility.Hidden;
                        dataGridAlarmList.Columns[8].Visibility = Visibility.Hidden;
                        break;
                    case "zh-CN":
                        dataGridAlarmList.Columns[5].Visibility = Visibility.Hidden;
                        dataGridAlarmList.Columns[6].Visibility = Visibility.Hidden;
                        dataGridAlarmList.Columns[7].Visibility = Visibility.Visible;
                        dataGridAlarmList.Columns[8].Visibility = Visibility.Hidden;
                        break;
                    case "hu-HU":
                        dataGridAlarmList.Columns[5].Visibility = Visibility.Hidden;
                        dataGridAlarmList.Columns[6].Visibility = Visibility.Hidden;
                        dataGridAlarmList.Columns[7].Visibility = Visibility.Hidden;
                        dataGridAlarmList.Columns[8].Visibility = Visibility.Visible;
                        break;
                }
            }
            catch (Exception e)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, e.ToString());
            }
        }

        //데이타 그리드 셀 더블 클릭
        private void DataGridCell_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            DataGridCell cell = e.Source as DataGridCell;

            if (cell != null)
            {
                //IOPoint IO = cell.DataContext as IOPoint;
                //if (IO != null)
                //{
                //    bool IOValue = CCLinkManager.CCLCurrent.ReadIO(IO.ModuleID, IO.Name);

                //    CCLinkManager.CCLCurrent.WriteIO(IO.ModuleID, IO.Name, !IOValue);
                //}
            }

        }

        //데이타 그리드 셀렉트 셀 변경 
        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is DataGrid senderBuffer)
            {
                _selectAlarmData = senderBuffer.SelectedItem as AlarmData;

                if (_selectAlarmData != null)
                {
                    textblockSelectedItemName.Text = "[ " + _selectAlarmData.AlarmName.Replace("_", " ") + " ]";
                    switch(TranslationManager.Instance.CurrentLanguage.ToString())
                    {
                        case "ko-KR":
                            textblockSolution.Text = _selectAlarmData.Solution;
                            break;
                        case "en-US":
                            textblockSolution.Text = _selectAlarmData.Solution_ENG;
                            break;
                        case "zh-CN":
                            textblockSolution.Text = _selectAlarmData.Solution_CHN;
                            break;
                        case "hu-HU":
                            textblockSolution.Text = _selectAlarmData.Solution_HUN;
                            break;
                    }

                    if (_isPopupOpen)
                        _EventCall_AlarmManagerDataChange(_selectAlarmData);//이벤트 발사
                }
                else
                {
                    textblockSelectedItemName.Text = "";
                    textblockSolution.Text = "";
                }

            }
        }

        //버튼 클릭 통합 이벤트
        private void ButtonControl_Click(object sender, RoutedEventArgs e)
        {
            try //240404 RGJ 클릭 이벤트 예외 못잡으면 프로그램 다운 되므로 전부 예외처리함.
            {
                if (sender is Button senderBuffer)
                {
                    switch (senderBuffer.Tag)
                    {
                        //220916 조숭진 case load 추가
                        case "XML_Load":
                            GlobalData.Current.Alarm_Manager.AlarmXmlLoad();
                            InitializeComboBox();
                            break;
                        case "Search_Init":
                            InitializeComboBox();
                            break;

                        case "Search":
                            ChangeDataGrid(comboBoxModuleType.Text.ToString());
                            break;
                        case "Add":
                        case "Modify":
                        case "Delete":
                            if (!_isPopupOpen)
                            {
                                AlarmManagerPopupView kw = new AlarmManagerPopupView(senderBuffer.Tag.ToString(), _selectAlarmData);
                                kw.Owner = Application.Current.MainWindow; // We must also set the owner for this to work.
                                kw.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                                kw.Show();
                            }
                            break;
                        case "Export":
                            Export_Excel(); //2303039 엑셀 Export 추가.
                            break;
                        //220916 조숭진 s
                        default:
                            if (sender is SK_ButtonControl senderBuffer2)
                            {
                                switch (senderBuffer2.TagName)
                                {
                                    case "Add":
                                    case "Modify":
                                    case "Delete":
                                        //case "Export":
                                        if (!_isPopupOpen)
                                        {
                                            AlarmManagerPopupView kw = new AlarmManagerPopupView(senderBuffer2.TagName, _selectAlarmData);
                                            kw.Owner = Application.Current.MainWindow; // We must also set the owner for this to work.
                                            kw.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                                            kw.Show();
                                        }
                                        break;
                                }
                            }
                            break;
                            //220916 조숭진 e
                    }

                    LogManager.WriteOperatorLog(string.Format("사용자가 {0} {1} 을/를 Click하였습니다.", "Stocker Alarm 관리", senderBuffer.Tag.ToString()),
                        "CLICK", senderBuffer.Tag.ToString(), GlobalData.Current.CurrentIP, string.Empty, GlobalData.Current.ClientPCName, GlobalData.Current.CurrentUserID, 4,
                        "Stocker Alarm 관리", senderBuffer.Tag.ToString());
                }
            }
            catch (Exception ex)
            {
                Log.LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
            }
            

        }

        //이벤트로 팝업 오픈 상태 확인
        public void setPopupState(bool rcvIsOpen)
        {
            _isPopupOpen = rcvIsOpen;
        }

        //220711 조숭진
        public void refreshData(string rcvModuleType)
        {
            ChangeDataGrid(rcvModuleType);
        }

        private void Export_Excel()
        {
            vm.ExcuteLogExport();

            //vm.ExcelImport();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            dataGridAlarmList.ScrollIntoView(dataGridAlarmList.Items[0]);
        }
    }

    public class ButtonControl : Button, INotifyPropertyChanged
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

        //패스 이미지
        private string _pathData;
        public string PathData
        {
            get { return _pathData; }
            set
            {
                if (_pathData != value)
                {
                    _pathData = value;
                    OnPropertyChanged("PathData");
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

        //현제 넓이
        public double CurrentWidth { get { return this.ActualWidth; } }

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
