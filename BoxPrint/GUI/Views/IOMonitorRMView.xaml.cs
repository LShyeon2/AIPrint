using BoxPrint.CCLink;
using BoxPrint.GUI.ClassArray;
using BoxPrint.Log;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace BoxPrint.GUI.Views
{
    /// <summary>
    /// IOMonitorRMView.xaml에 대한 상호 작용 논리
    /// 2020.11.18 RM IO 관련 추가
    /// </summary>
    public partial class IOMonitorRMView : Page
    {
        DispatcherTimer timer = new DispatcherTimer();    // timer 객체생성
        private object TokenLock = new object();
        public List<Ellipse> nEllipseSensor = new List<Ellipse>();

        public GUIColorBase GUIColorMembers = new GUIColorBase();
        eThemeColor currentThemeColorName = eThemeColor.NONE;

        public IOMonitorRMView()
        {
            InitializeComponent();

            RmComboSet();
            InitDataGridSet();
            EllSensorAdd();

            //220330 seongwon 테마 색상 바인딩
            MainWindow._EventCall_ThemeColorChange += new MainWindow.EventHandler_ChangeThemeColor(this.eventGUIThemeColorChange);//테마 색상 이벤트
            GUIColorMembers = GlobalData.Current.GuiColor;

            timer.Interval = TimeSpan.FromMilliseconds(1000);    //시간간격 설정
            timer.Tick += new EventHandler(timer_Tick);          //이벤트 추가
            timer.Start();

            cbRM.SelectionChanged += CbRM_SelectionChanged;

        }

        private void CbRM_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            InitDataGridSet();
        }

        private void RmComboSet()
        {
            foreach (var item in GlobalData.Current.mRMManager.ModuleList)
            {
                cbRM.Items.Add(item.Value.ModuleName);
            }
            cbRM.SelectedIndex = 0;
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            if (this.IsVisible)
            {
                UdateDataGridSet();
            }
        }

        private void EllSensorAdd()
        {
            nEllipseSensor.Add(DI_LR1_Sensor);
            nEllipseSensor.Add(DI_LR2_Sensor);
            nEllipseSensor.Add(DI_LR3_Sensor);
            nEllipseSensor.Add(DI_LR4_Sensor);
            nEllipseSensor.Add(DI_Place_Sensor_1);
            nEllipseSensor.Add(DI_Place_Sensor_2);
            nEllipseSensor.Add(DI_Put_Sensor);
            nEllipseSensor.Add(DI_Storage_Sensor);
            nEllipseSensor.Add(DI_FB1_Sensor);
            nEllipseSensor.Add(DI_FB2_Sensor);


            //nEllipseSensor.Add(DI_Clamp_Sensor);
            //nEllipseSensor.Add(DI_UnClamp_Sensor);
        }

        /// <summary>
        ///  grid를 셋팅 한다.
        /// </summary>
        private void InitDataGridSet()
        {
            try
            {
                sortGrid.ItemsSource = GlobalData.Current.CCLink_mgr.GetModuleIOList(cbRM.SelectedItem.ToString());
                sortGrid.Items.Refresh();
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
            }
        }

        private void UdateDataGridSet()
        {
            try
            {
                var rows1 = GetDataGridRows(sortGrid);
                foreach (DataGridRow r in rows1)
                {
                    var item = (IOPoint)r.Item;

                    if (item.Name != "")
                    {
                        bool bval = GlobalData.Current.mRMManager[item.ModuleID].ReadRMSensorIO(item);

                        if (bval)
                        {
                            r.Background = Brushes.Beige;
                            item.LastReadValue = true;
                        }
                        else
                        {
                            r.Background = Brushes.White;
                            item.LastReadValue = false;
                        }


                        var ell = nEllipseSensor.Where(s => s.Name.ToString() == item.Name).FirstOrDefault();
                        if (ell != null)
                        {
                            if (item.LastReadValue)
                            {
                                ell.Fill = Brushes.LightGreen;
                            }
                            else
                            {
                                ell.Fill = Brushes.White;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
            }
        }
        public IEnumerable<System.Windows.Controls.DataGridRow> GetDataGridRows(System.Windows.Controls.DataGrid grid)
        {
            var itemsSource = grid.ItemsSource as IEnumerable;
            if (null == itemsSource) yield return null;
            foreach (var item in itemsSource)
            {
                var row = grid.ItemContainerGenerator.ContainerFromItem(item) as System.Windows.Controls.DataGridRow;
                if (null != row) yield return row;
            }
        }

        private void button_View_Switching_Click(object sender, RoutedEventArgs e)
        {
            int Col = Grid.GetColumn(Grid_FrontSensor); //컬럼값 스왑해서 상태 화면 표시
            if (Col == 2)
            {
                Grid.SetColumn(Grid_FrontSensor, 1);
                Grid.SetColumn(Grid_BackSensor, 2);
            }
            else
            {
                Grid.SetColumn(Grid_FrontSensor, 2);
                Grid.SetColumn(Grid_BackSensor, 1);
            }

        }

        //220330 seongwon 테마 색상 바인딩
        public void eventGUIThemeColorChange()
        {
            if (GlobalData.Current.SendTagEvent != "IOMonitorRM")
                return;

            setGUIThemeColorChange();
        }

        private void setGUIThemeColorChange()
        {

            if (currentThemeColorName == GUIColorMembers._currentThemeName)
                return;

            currentThemeColorName = GUIColorMembers._currentThemeName;

            colorBuffer_IOMonitorViewRMMainBackground.Fill = GUIColorMembers.NormalBorderBackground;
            colorBuffer_IOMonitorViewRMButtonBackground.Fill = GUIColorMembers.MainMenuButtonBackground;
            colorBuffer_IOMonitorViewRMForeground.Fill = GUIColorMembers.MainMenuForeground;
            colorBuffer_IOMonitorViewRMBorderBrush.Fill = GUIColorMembers.MainMenuButtonBorderBrush;
            colorBuffer_IOMonitorViewRMButtonBackground_Enter.Fill = GUIColorMembers.NormalButtonBackground_Enter;
            colorBuffer_IOMonitorViewRMButtonBorderBrush.Fill = GUIColorMembers.MainMenuButtonBorderBrush;
        }
    }
}
