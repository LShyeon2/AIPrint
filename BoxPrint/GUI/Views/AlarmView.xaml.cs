using BoxPrint.Alarm;
using BoxPrint.GUI.ClassArray;
using BoxPrint.GUI.ETC;
using BoxPrint.GUI.EventCollection;
using BoxPrint.GUI.UIControls;
using BoxPrint.GUI.Views.UserPage;
using BoxPrint.Log;
using BoxPrint.Modules.User;
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using TranslationByMarkupExtension;

namespace BoxPrint.GUI.Views
{

    // 2020.09.24 RGJ
    /// <summary>
    /// AlarmLogView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class AlarmView : Page
    {
        public delegate void EventHandler_AlarmOccur(string rvcString);

        //public static event EventHandler_AlarmOccur OnAlarmOccurred;  

        //private List<string> ListHeader = new List<string>();
        private string strtag = string.Empty;
        private delegate void D_Set_StringValue(string nValue);
        public GUIColorBase GUIColorMembers = new GUIColorBase();

        //생성자
        public AlarmView()
        {
            InitializeComponent();

            borderbtnAlarmTest.Visibility = GlobalData.Current.GlobalSimulMode ? Visibility.Visible : Visibility.Collapsed;

            AlarmGrid.ItemsSource = GlobalData.Current.Alarm_Manager.ActiveAlarmList;
            GlobalData.Current.SendTagChange += Current_ReceiveEvent;

            //20230711 사용자 권한에 따른 버튼 잠금
            GlobalData.Current.UserMng.OnLoginUserChange += UserMng_OnLoginUserChange;


            GUIColorMembers = GlobalData.Current.GuiColor;

            switch (TranslationManager.Instance.CurrentLanguage.ToString())
            {
                case "ko-KR":
                    AlarmGrid.Columns[6].Visibility = Visibility.Visible;
                    AlarmGrid.Columns[7].Visibility = Visibility.Hidden;
                    AlarmGrid.Columns[8].Visibility = Visibility.Hidden;
                    AlarmGrid.Columns[9].Visibility = Visibility.Hidden;
                    break;
                case "en-US":
                    AlarmGrid.Columns[6].Visibility = Visibility.Hidden;
                    AlarmGrid.Columns[7].Visibility = Visibility.Visible;
                    AlarmGrid.Columns[8].Visibility = Visibility.Hidden;
                    AlarmGrid.Columns[9].Visibility = Visibility.Hidden;
                    break;
                case "zh-CN":
                    AlarmGrid.Columns[6].Visibility = Visibility.Hidden;
                    AlarmGrid.Columns[7].Visibility = Visibility.Hidden;
                    AlarmGrid.Columns[8].Visibility = Visibility.Visible;
                    AlarmGrid.Columns[9].Visibility = Visibility.Hidden;
                    break;
                case "hu-HU":
                    AlarmGrid.Columns[6].Visibility = Visibility.Hidden;
                    AlarmGrid.Columns[7].Visibility = Visibility.Hidden;
                    AlarmGrid.Columns[8].Visibility = Visibility.Hidden;
                    AlarmGrid.Columns[9].Visibility = Visibility.Visible;
                    break;
            }
            UIEventCollection.Instance.OnCultureChanged += _UIEventCollection_OnCultureChanged;
        }

        private void _UIEventCollection_OnCultureChanged(string cultureKey)
        {
            switch (cultureKey)
            {
                case "ko-KR":
                    AlarmGrid.Columns[6].Visibility = Visibility.Visible;
                    AlarmGrid.Columns[7].Visibility = Visibility.Hidden;
                    AlarmGrid.Columns[8].Visibility = Visibility.Hidden;
                    AlarmGrid.Columns[9].Visibility = Visibility.Hidden;
                    break;
                case "en-US":
                    AlarmGrid.Columns[6].Visibility = Visibility.Hidden;
                    AlarmGrid.Columns[7].Visibility = Visibility.Visible;
                    AlarmGrid.Columns[8].Visibility = Visibility.Hidden;
                    AlarmGrid.Columns[9].Visibility = Visibility.Hidden;
                    break;
                case "zh-CN":
                    AlarmGrid.Columns[6].Visibility = Visibility.Hidden;
                    AlarmGrid.Columns[7].Visibility = Visibility.Hidden;
                    AlarmGrid.Columns[8].Visibility = Visibility.Visible;
                    AlarmGrid.Columns[9].Visibility = Visibility.Hidden;
                    break;
                case "hu-HU":
                    AlarmGrid.Columns[6].Visibility = Visibility.Hidden;
                    AlarmGrid.Columns[7].Visibility = Visibility.Hidden;
                    AlarmGrid.Columns[8].Visibility = Visibility.Hidden;
                    AlarmGrid.Columns[9].Visibility = Visibility.Visible;
                    break;
            }

            if (adBuffer != null)
            {
                textblockSelectedItemName.Text = "[ " + adBuffer.AlarmName.Replace("_", " ") + " ]";
                switch (cultureKey)
                {
                    case "ko-KR":
                        textblockSolution.Text = adBuffer.Solution;
                        break;
                    case "en-US":
                        textblockSolution.Text = adBuffer.Solution_ENG;
                        break;
                    case "zh-CN":
                        textblockSolution.Text = adBuffer.Solution_CHN;
                        break;
                    case "hu-HU":
                        textblockSolution.Text = adBuffer.Solution_HUN;
                        break;
                }
            }
            else
            {
                textblockSelectedItemName.Text = "";
                textblockSolution.Text = "";
            }
        }

        private void Current_ReceiveEvent(object sender, EventArgs e)
        {
            string JInfo = (string)sender;
            this.Dispatcher.Invoke(new D_Set_StringValue(_DisplayChange), JInfo);
        }

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
            //불필요한 그리드 클리어 주석 처리
            //AlarmGrid.Columns.Clear();
        }

        private void AlarmGrid_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            //if (AlarmGrid.SelectedIndex == -1)
            //{
            //    this.textBlock_AlarmSolution.Text = string.Empty;
            //    return;
            //}
            //if(AlarmGrid.SelectedCells.Count < 1)
            //{
            //    return;
            //}
            //AlarmData AData = AlarmGrid.SelectedCells[0].Item as AlarmData;
            //if (AData != null)
            //{
            //    this.textBlock_AlarmSolution.Text = AData.Solution;
            //}
            //else
            //{
            //    this.textBlock_AlarmSolution.Text = string.Empty;
            //}
        }

        private void ToggleButton_Click(object sender, RoutedEventArgs e)
        {
            try //240404 RGJ 클릭 이벤트 예외 못잡으면 프로그램 다운 되므로 전부 예외처리함.
            {
                DependencyObject obj = (DependencyObject)e.OriginalSource;
                while (!(obj is DataGridRow) && obj != null) obj = VisualTreeHelper.GetParent(obj);
                if (obj is DataGridRow)
                {
                    if ((obj as DataGridRow).DetailsVisibility == Visibility.Visible)
                    {
                        (obj as DataGridRow).IsSelected = false;
                    }
                    else
                    {
                        (obj as DataGridRow).IsSelected = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
            }
        }

        private void dataGrid1_RowDetailsVisibilityChanged(object sender, DataGridRowDetailsEventArgs e)
        {
            DataGridRow row = e.Row as DataGridRow;
            FrameworkElement tb = GetTemplateChildByName(row, "RowHeaderToggleButton");
            if (tb != null)
            {
                if (row.DetailsVisibility == System.Windows.Visibility.Visible)
                {
                    (tb as ToggleButton).IsChecked = true;
                }
                else
                {
                    (tb as ToggleButton).IsChecked = false;
                }
            }

        }
        public static FrameworkElement GetTemplateChildByName(DependencyObject parent, string name)
        {
            int childnum = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childnum; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is FrameworkElement &&

                        ((FrameworkElement)child).Name == name)
                {
                    return child as FrameworkElement;
                }
                else
                {
                    var s = GetTemplateChildByName(child, name);
                    if (s != null)
                        return s;
                }
            }
            return null;
        }

        private void dataGrid1_Loaded(object sender, RoutedEventArgs e)
        {
            DataGrid dg = sender as DataGrid;
            Border border = VisualTreeHelper.GetChild(dg, 0) as Border;
            ScrollViewer scrollViewer = VisualTreeHelper.GetChild(border, 0) as ScrollViewer;
            Grid grid = VisualTreeHelper.GetChild(scrollViewer, 0) as Grid;
            Button button = VisualTreeHelper.GetChild(grid, 0) as Button;

            if (button != null && button.Command != null && button.Command == DataGrid.SelectAllCommand)
            {
                button.IsEnabled = false;
                button.Opacity = 0;
            }
        }

        private void BorderButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            string alarmmsg = string.Empty;
            if (sender is Border)
            {
                var senderBuffer = (Border)sender;

                if (string.IsNullOrEmpty(senderBuffer.Tag.ToString()))
                    return;

                switch (senderBuffer.Tag.ToString())
                {
                    case "AlarmClear":
                        //선택된 알람을 클리어.
                        if (AlarmGrid.SelectedIndex >= 0)
                        {
                            AlarmData AData = AlarmGrid.SelectedCells[0].Item as AlarmData;
                            if (AData != null)
                            {
                                GlobalData.Current.Alarm_Manager.AlarmClear(AData);
                                if (AlarmGrid.Items.Count > 0)
                                    AlarmGrid.SelectedIndex = 0; //알람 클리어시 자동으로 첫번째 Index 선택하게 함.

                                alarmmsg = AData.AlarmName;
                            }
                        }
                        break;

                    case "AlarmTest":
                        GlobalData.Current.Alarm_Manager.AlarmOccur("1", GlobalData.Current.mRMManager.FirstRM.ModuleName);
                        //GlobalData.Current.Alarm_Manager.AlarmOccur("1", GlobalData.Current.mRMManager.SecondRM.ModuleName);
                        break;
                }

                LogManager.WriteOperatorLog(string.Format("사용자가 알람 {0} 을/를 {1} Click하였습니다.", alarmmsg, senderBuffer.Tag.ToString()),
                    "CLICK", senderBuffer.Tag.ToString(), GlobalData.Current.CurrentIP, string.Empty, GlobalData.Current.ClientPCName, GlobalData.Current.CurrentUserID, 17,
                    alarmmsg, senderBuffer.Tag.ToString());
            }
        }


        public void eventGUIThemeColorChange()
        {
            if (GlobalData.Current.SendTagEvent != "Alarm")
                return;
        }

        AlarmData adBuffer;

        //데이타 그리드 셀렉트 셀 변경 
        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is DataGrid senderBuffer)
            {
                AlarmData alarmDataBuffer = senderBuffer.SelectedItem as AlarmData;
                adBuffer = alarmDataBuffer;

                if (alarmDataBuffer != null)
                {
                    textblockSelectedItemName.Text = "[ " + alarmDataBuffer.AlarmName.Replace("_", " ") + " ]";
                    switch (TranslationManager.Instance.CurrentLanguage.ToString())
                    {
                        case "ko-KR":
                            textblockSolution.Text = alarmDataBuffer.Solution;
                            break;
                        case "en-US":
                            textblockSolution.Text = alarmDataBuffer.Solution_ENG;
                            break;
                        case "zh-CN":
                            textblockSolution.Text = alarmDataBuffer.Solution_CHN;
                            break;
                        case "hu-HU":
                            textblockSolution.Text = alarmDataBuffer.Solution_HUN;
                            break;
                    }
                }
                else
                {
                    textblockSelectedItemName.Text = "";
                    textblockSolution.Text = "";
                }

            }
        }

        private async void SK_ButtonControl_Click(object sender, RoutedEventArgs e)
        {
            try //240404 RGJ 클릭 이벤트 예외 못잡으면 프로그램 다운 되므로 전부 예외처리함.
            {
                string alarmmsg = string.Empty;

                if (sender is SK_ButtonControl senderBuffer)
                {
                    if (string.IsNullOrEmpty(senderBuffer.Tag.ToString()))
                        return;

                    switch (senderBuffer.Tag.ToString())
                    {
                        case "AlarmClear":
                            //선택된 알람을 클리어.
                            if (AlarmGrid.SelectedIndex >= 0)
                            {
                                AlarmData AData = AlarmGrid.SelectedCells[0].Item as AlarmData;
                                if (AData != null)
                                {
                                    alarmmsg = AData.AlarmName;

                                    LogManager.WriteOperatorLog(string.Format("사용자가 {0} {1} 을/를 {2} Click하였습니다.", "Alarm", alarmmsg, senderBuffer.Tag.ToString()),
                                        "CLICK", senderBuffer.Tag.ToString(), GlobalData.Current.CurrentIP, string.Empty, GlobalData.Current.ClientPCName, GlobalData.Current.CurrentUserID, 16,
                                        "Alarm", alarmmsg, senderBuffer.Tag.ToString());

                                    //알람 클리어가 딜레이 되면 Main UI 행 걸리므로 비동기 처리로 변경.
                                    bool Result = await Task<bool>.Factory.StartNew(() =>
                                    {
                                        try
                                        {
                                            GlobalData.Current.Alarm_Manager.AlarmClear(AData);
                                            return true;
                                        }
                                        catch
                                        {
                                            return false;
                                        }

                                    });
                                    if (Result)
                                    {
                                        if (AlarmGrid.Items.Count > 0)
                                            AlarmGrid.SelectedIndex = 0; //알람 클리어시 자동으로 첫번째 Index 선택하게 함.
                                    }

                                }
                            }
                            break;

                        case "AlarmTest":
                            GlobalData.Current.Alarm_Manager.AlarmOccur("1", GlobalData.Current.mRMManager.FirstRM.ModuleName);
                            //Current.Alarm_Manager.AlarmOccur("1", GlobalData.Current.mRMManager.SecondRM.ModuleName);

                            AlarmData Alarm = null;
                            ObservableList<AlarmData> tempAllAlarmList = GlobalData.Current.Alarm_Manager.getAllAlarmList();

                            if (tempAllAlarmList.Where(a => a.AlarmID == "1").Count() != 0)
                            {
                                Alarm = tempAllAlarmList.FirstOrDefault(a => a.AlarmID == "1");
                                alarmmsg = Alarm.AlarmName;
                            }

                            /** if (AlarmGrid.Items.Count > 0)
                            {
                                AlarmGrid.SelectedIndex = 0;
                                _AlarmOccur("AlarmPopup");
                            } **/

                            LogManager.WriteOperatorLog(string.Format("사용자가 {0} {1} {2} 을/를 Click하였습니다.", "Alarm", alarmmsg, senderBuffer.Tag.ToString()),
                                "CLICK", senderBuffer.Tag.ToString(), GlobalData.Current.CurrentIP, string.Empty, GlobalData.Current.ClientPCName, GlobalData.Current.CurrentUserID, 6,
                                "Alarm", alarmmsg, senderBuffer.Tag.ToString());

                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
            }
            
        }

        //20230711 사용자 권한에 따른 버튼 잠금
        private void UserMng_OnLoginUserChange(User usr)
        {
            if (!(usr is null))
            {
                btnAlarmClear.IsEnabled = (GlobalData.Current.LoginUserAuthority.Contains("ModifyAlarmClear")) ? true : false;
            }
            else
            {
                btnAlarmClear.IsEnabled = false;
            }
        }
    }
}