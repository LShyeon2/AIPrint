using BoxPrint.GUI.ClassArray;
using BoxPrint.GUI.ETC;
using BoxPrint.Log;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using TranslationByMarkupExtension;

namespace BoxPrint.GUI.Views
{
    /// <summary>
    /// LogView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class LogView : Page
    {
        private string strtag = string.Empty;
        private delegate void D_Set_StringValue(string nValue);
        string SelectecLogPath = "";

        public GUIColorBase GUIColorMembers = new GUIColorBase();
        eThemeColor currentThemeColorName = eThemeColor.NONE;

        public LogView()
        {
            InitializeComponent();
            GlobalData.Current.SendTagChange += Current_ReceiveEvent;

            //220330 seongwon 테마 색상 바인딩
            MainWindow._EventCall_ThemeColorChange += new MainWindow.EventHandler_ChangeThemeColor(this.eventGUIThemeColorChange);//테마 색상 이벤트
            GUIColorMembers = GlobalData.Current.GuiColor;
        }

        private void ShowLog(string filename)
        {
            try
            {
                //File ReadWrite 호환용으로 Source 수정
                string readContents;
                using (FileStream fs = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (var sr = new StreamReader(fs, Encoding.Default))
                    {
                        readContents = sr.ReadToEnd();
                        string[] readContentsArray = readContents.Split(new[] { "\r\n" }, StringSplitOptions.None);

                        this.LogGrid.ItemsSource = readContentsArray.Select(p => new { 로그 = p }).ToList();
                    }
                }
                //string[] lines = File.ReadAllLines(filename, Encoding.Default);

                //this.LogGrid.ItemsSource = lines.Select(p => new { 로그 = p }).ToList();
            }
            catch (FileNotFoundException)
            {

            }
            catch (IOException ioex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ioex.ToString());
                //MessageBox.Show("해당 로그 파일 로드에 실패하였습니다.", "FILE 잠김", MessageBoxButton.OK, MessageBoxImage.Asterisk);

                //SuHwan_20230320 : 메시지 박스 통합
                MessageBoxPopupView msgbox = new MessageBoxPopupView(TranslationManager.Instance.Translate("Exception").ToString(),
                                                                     TranslationManager.Instance.Translate("해당 로그 파일 로드에 실패하였습니다.").ToString(),
                                                                     TranslationManager.Instance.Translate("FILE 잠김").ToString(),
                                                                     "LogView.Xaml.cs(63)", MessageBoxButton.OK, MessageBoxImage.Error, "", "", "", false, false);
                CustomMessageBoxResult mBoxResult = msgbox.ShowResult();
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
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
            //List<GridItemListItemInfo> lsititem = GlobalData.Current.GetGridItemList(tag);

            LogGrid.Columns.Clear();

            cbb_LogItem.SelectedIndex = 0;

            DatePick_Start.SelectedDate = DateTime.Now.Subtract(new TimeSpan(7, 0, 0, 0)) - DateTime.Now.TimeOfDay;     //20220415 조숭진 초기 로그화면 날짜목록 이상.
            DatePick_End.SelectedDate = DateTime.Now - DateTime.Now.TimeOfDay;

            UpdateLogTree(DatePick_Start.SelectedDate, DatePick_End.SelectedDate);

        }

        private void Find_Click(object sender, RoutedEventArgs e)
        {
            try //240404 RGJ 클릭 이벤트 예외 못잡으면 프로그램 다운 되므로 전부 예외처리함.
            {
                string textFind = textBox_Serach.Text;
                ICollectionView icv = CollectionViewSource.GetDefaultView(LogGrid.ItemsSource);
                if (icv == null)
                {
                    return;
                }
                else
                {
                    icv.Filter = new Predicate<object>(TextSearchFilter);
                }
            }
            catch (Exception ex)
            {
                Log.LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
            }
        }
        private bool TextSearchFilter(object o)
        {
            string s = o.ToString();
            if (!string.IsNullOrEmpty(s))
            {
                if (s.ToUpper().Contains(textBox_Serach.Text.ToUpper()))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        private void cbb_LogItem_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateLogTree(DatePick_Start.SelectedDate, DatePick_End.SelectedDate);
        }
        private void DatePick_Start_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateLogTree(DatePick_Start.SelectedDate, DatePick_End.SelectedDate);
        }

        private void UpdateLogTree(DateTime? start, DateTime? end)
        {
            string LogFolderPath = "";
            switch (cbb_LogItem.SelectedValue.ToString())
            {
                case "Console":
                    LogFolderPath = LogManager.GetLogConsolePath;
                    break;
                case "System":
                    LogFolderPath = LogManager.GetLogSysPath;
                    break;
                case "LCS":
                    LogFolderPath = LogManager.GetLogLCSPath;
                    break;
                case "RM1":
                    LogFolderPath = LogManager.GetLogRM1Path;
                    break;
                case "RM2":
                    LogFolderPath = LogManager.GetLogRM2Path;
                    break;
                case "Axis":
                    LogFolderPath = LogManager.GetLogAxisPath;
                    break;
                case "PIO":
                    LogFolderPath = LogManager.GetLogPIOPath;
                    break;
                case "TR":
                    LogFolderPath = LogManager.GetLogTRPath;
                    break;
                case "Port":
                    LogFolderPath = LogManager.GetLogPortPath;
                    break;
                case "Program":
                    LogFolderPath = LogManager.GetLogProgramPath;
                    break;
                case "OracleDB":
                    LogFolderPath = LogManager.GetLogDBPath;
                    break;
            }
            SelectecLogPath = LogFolderPath;
            if (string.IsNullOrEmpty(LogFolderPath) || start == null || end == null)
            {
                return;
            }
            if (start > end)
            {
                MessageBox.Show(TranslationManager.Instance.Translate("기간 설정이 올바르지 않습니다.").ToString(),
                                TranslationManager.Instance.Translate("Check").ToString(),
                                MessageBoxButton.OK, MessageBoxImage.Asterisk);
            }
            //아이템 변경 될때마다 트리 갱신
            treeView.Items.Clear();
            LogTree LT = new LogTree(LogFolderPath, (DateTime)start, (DateTime)end);


            foreach (var dItem in LT.DayList)
            {
                TreeViewItem TItem = new TreeViewItem();
                TItem.Header = dItem.StartTime.ToString("yyyy년 MM월 dd일");
                foreach (var fItem in dItem.FileList)
                {
                    TreeViewItem FItem = new TreeViewItem();
                    FItem.Header = fItem;
                    TItem.Items.Add(FItem);
                }
                treeView.Items.Add(TItem);
            }

        }

        private void treeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            try
            {
                TreeViewItem TVI = (TreeViewItem)((TreeView)sender).SelectedItem;
                if (TVI != null)
                {
                    string fileName = TVI.Header.ToString();

                    ShowLog(SelectecLogPath + "\\" + fileName);
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
            }
        }

        private void textBox_Serach_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Return)
            {
                Find_Click(null, null);
            }
        }

        //220330 seongwon 테마 색상 바인딩
        public void eventGUIThemeColorChange()
        {
            if (GlobalData.Current.SendTagEvent != "FirmwareLog")
                return;

            setGUIThemeColorChange();
        }

        private void setGUIThemeColorChange()
        {

            if (currentThemeColorName == GUIColorMembers._currentThemeName)
                return;

            currentThemeColorName = GUIColorMembers._currentThemeName;

            colorBuffer_FirmwareLogViewMainBackground.Fill = GUIColorMembers.NormalBorderBackground;
            colorBuffer_FirmwareLogViewButtonBackground.Fill = GUIColorMembers.MainMenuButtonBackground;
            colorBuffer_FirmwareLogViewForeground.Fill = GUIColorMembers.MainMenuForeground;
            colorBuffer_FirmwareLogViewBorderBrush.Fill = GUIColorMembers.MainMenuButtonBorderBrush;
            colorBuffer_FirmwareLogViewButtonBackground_Enter.Fill = GUIColorMembers.NormalButtonBackground_Enter;
        }

    }
    public class LogTree
    {
        public List<DayLogList> DayList;
        public LogTree(string LogFolder, DateTime start, DateTime end)
        {
            DirectoryInfo DInfo = new DirectoryInfo(LogFolder);
            DayList = new List<DayLogList>();
            if (!DInfo.Exists)
            {
                return;
            }

            for (DateTime d = (DateTime)start; d <= end; d = d.AddDays(1))
            {
                DayList.Add(new DayLogList(d, new List<string>()));
            }

            foreach (var Fitem in DInfo.GetFiles())
            {
                DateTime dt;
                if (Fitem.Name[0] == '_')
                {
                    dt = DateTime.ParseExact(Fitem.Name.Substring(1, 8), "yyyyMMdd", CultureInfo.InvariantCulture);
                }
                else
                {
                    dt = DateTime.ParseExact(Fitem.Name.Substring(0, 8), "yyyyMMdd", CultureInfo.InvariantCulture);
                }
                //dt = dt.AddHours(-1);
                DayLogList daylog = DayList.Find(d => d.StartTime <= dt && dt < d.StartTime.AddHours(24)); //하루에 맞춰서 집어넣는다.
                if (daylog != null)
                {
                    daylog.FileList.Add(Fitem.Name);
                }
            }
        }
    }
    public class DayLogList
    {
        public DateTime StartTime;
        public List<string> FileList;
        public DayLogList(DateTime d, List<string> Files)
        {
            StartTime = d;
            FileList = Files;
        }
    }
}
