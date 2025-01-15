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
using Stockerfirmware.DataList.LifeTime;
using Stockerfirmware.Log;
using Stockerfirmware.GUI.ClassArray;

namespace Stockerfirmware.GUI.Views
{

    // 2020.09.24 RGJ
    /// <summary>
    /// IOMonitorView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class LifeTimeView : Page
    {
        private string strtag = string.Empty;

        private delegate void D_Set_StringValue(string nValue);

        public GUIColorBase GUIColorMembers = new GUIColorBase();
        eThemeColor currentThemeColorName = eThemeColor.NONE;

        public LifeTimeView()
        {
            InitializeComponent();
            GlobalData.Current.SendTagChange += Current_ReceiveEvent;
            LoadTree();

            //220330 seongwon 테마 색상 바인딩
            MainWindow._EventCall_ThemeColorChange += new MainWindow.EventHandler_ChangeThemeColor(this.eventGUIThemeColorChange);//테마 색상 이벤트
            GUIColorMembers = GlobalData.Current.GuiColor;

            //시작할때 전체 뷰를 보여준다.
            dataGrid_Parts.ItemsSource = GlobalData.Current.PartsLife_mgr.GetModuleList("ALL");
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
            }
            catch (Exception ex)
            {
                Log.LogManager.WriteCSLog(eLogLevel.Fatal, ex.ToString());
            }
        }


        #region Tree 관련
        private void LoadTree()
        {
            treeView_Select.Items.Clear();
            //전체보기
            treeView_Select.Items.Add(GetTreeView("ALL", @"\Image\AxisState.png"));
            //로봇
            foreach (var item in GlobalData.Current.mRMManager.ModuleList)
            {
                treeView_Select.Items.Add(GetTreeView(item.Value.ModuleName, @"\Image\crane_icon_small.png"));
            }
            //port
            foreach (var item in GlobalData.Current.LineManager.ModuleList)
            {
                TreeViewItem tvCVLine = GetTreeView(item.Value.ModuleName, Colors.AliceBlue);

                foreach(var cvItem in item.Value.ModuleList)
                {
                    TreeViewItem tvCV = GetTreeView(cvItem.ModuleName, @"\Image\Conveyor_small.png");
                    tvCVLine.Items.Add(tvCV);
                }
                treeView_Select.Items.Add(tvCVLine);
            }
        }

        private TreeViewItem GetTreeView(string text, Color boxColor)
        {
            TreeViewItem item = new TreeViewItem();
            item.IsExpanded = false;

            // create stack panel
            StackPanel stack = new StackPanel();
            stack.Orientation = Orientation.Horizontal;

            // create Image

            Border border = new Border();
            border.Width = 8;
            border.Height = 10;
            border.Background = new SolidColorBrush(boxColor);

            // Label
            Label lbl = new Label();
            lbl.Content = text;


            stack.Children.Add(border);
            stack.Children.Add(lbl);

            //item.HeaderTemplate.ad  


            item.Header = stack;
            return item;

        }
        private TreeViewItem GetTreeView(string ModuleID, string imagePath)
        {
            TreeViewItem item = new TreeViewItem();
            item.IsExpanded = false;
            item.Tag = ModuleID;
            // create stack panel
            StackPanel stack = new StackPanel();
            stack.Orientation = Orientation.Horizontal;

            // create Image
            Image image = new Image();

            //image.Source = new BitmapImage(new Uri("pack://application:,,/Images/" + imagePath));

            BitmapImage result = new BitmapImage();

            result.BeginInit();

            string p = GlobalData.Current.CurrentFilePaths(System.Environment.CurrentDirectory) + imagePath;
            result.UriSource = new Uri(p);
            result.EndInit();

            image.Source = result;
            image.Width = 30;
            image.Height = 30;
            image.Stretch = Stretch.Uniform;
            // Label
            Label lbl = new Label();
            lbl.Content = ModuleID;

            // Add into stack
            stack.Children.Add(image);
            stack.Children.Add(lbl);

            // assign stack to header
            item.Header = stack;

            item.Selected += Item_Selected;
            return item;

        }

        private void Item_Selected(object sender, RoutedEventArgs e)
        {
            TreeViewItem tv = sender as TreeViewItem;
            if (tv != null)
            {
                ChangePartsGroup(tv.Tag.ToString());
            }
        }

        #endregion


        private void ChangePartsGroup(string moduleID)
        {
            if (string.IsNullOrEmpty(moduleID))
            {
                return;
            }
            dataGrid_Parts.ItemsSource = GlobalData.Current.PartsLife_mgr.GetModuleList(moduleID);

        }

        private void button_ItemReset_Click(object sender, RoutedEventArgs e)
        {
            if(dataGrid_Parts.SelectedIndex >= 0)
            {
                var parts = dataGrid_Parts.SelectedCells[0].Item as PartsLifeItem;
                if (parts != null)
                {
                    string Message = string.Format("모듈 :{0} 의 파츠: {1} 소모량 값을 초기화 하시겠습니까?", parts.ModuleName, parts.PartsName);
                    var Result = System.Windows.MessageBox.Show(Message, "소모품 초기화", MessageBoxButton.YesNo);
                    if (Result == MessageBoxResult.Yes)
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "{0} 파츠 :{1} 모델 :{2} 소모량 값을 초기화합니다.", parts.ModuleName, parts.PartsName, parts.PartsModel);
                        GlobalData.Current.PartsLife_mgr.PartsResetRequest(parts.ModuleName, parts.PartsName);
                    }
                }
            }
            else
            {
                return;
            }

        }

        //220330 seongwon 테마 색상 바인딩
        public void eventGUIThemeColorChange()
        {
            if (GlobalData.Current.SendTagEvent != "LifeTime")
                return;

            setGUIThemeColorChange();
        }

        private void setGUIThemeColorChange()
        {

            if (currentThemeColorName == GUIColorMembers._currentThemeName)
                return;

            currentThemeColorName = GUIColorMembers._currentThemeName;

            colorBuffer_LifeTimeViewMainBackground.Fill = GUIColorMembers.NormalBorderBackground;
            colorBuffer_LifeTimeViewButtonBackground.Fill = GUIColorMembers.MainMenuButtonBackground;
            colorBuffer_LifeTimeViewForeground.Fill = GUIColorMembers.MainMenuForeground;
            colorBuffer_LifeTimeViewBorderBrush.Fill = GUIColorMembers.MainMenuButtonBorderBrush;
            colorBuffer_LifeTimeViewButtonBackground_Enter.Fill = GUIColorMembers.NormalButtonBackground_Enter;
            colorBuffer_LifeTimeViewButtonBorderBrush.Fill = GUIColorMembers.MainMenuButtonBorderBrush;
        }

    }
   
 
}
