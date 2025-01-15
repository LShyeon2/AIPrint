
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Stockerfirmware.DataList;
using Stockerfirmware.Log;
using Stockerfirmware.Modules.Conveyor;
using Stockerfirmware.Modules.CVLine;
using Stockerfirmware.Modules.Shelf;

namespace Stockerfirmware.GUI.Views
{
    /// <summary>
    /// LayOutView.xaml에 대한 상호 작용 논리
    /// //2020.09.15 Main Layout 추가
    /// //2020.09.17 treeView 추가
    /// //2020.09.23 LCS Cmd Job 관련 Display 
    /// </summary>
    public partial class PortLayOutView : Page
    {
        Dictionary<string, CVUserControl> CVUserControlDic = new Dictionary<string, CVUserControl>();
        CVUserControl LastSelected = null;

        List<SortingRecClass> PvalitemList = new List<SortingRecClass>();
        List<SortingRecClass> BankBayLevl = new List<SortingRecClass>();
        private List<string> ListHeader = new List<string>();

        private string strtag = string.Empty;
        private ShelfItem shefitem;

        private double MainPaneloffset = 0;
        bool TestRed = false;
        bool StopperUp = false;
        bool LightCurtain = false;
        bool TestTurn = false;
        
        //private List <RMUserControl> RMList = new List<RMUserControl>();
        DispatcherTimer timer = new DispatcherTimer();    // timer 객체생성
        public PortLayOutView()
        {
            InitializeComponent();
            InitControl();

            GdSetHeader(eSectionHeader.Main_BankByalevel.ToString(), DGBankBayLevel);
            slider1.Value = 0.6;

            timer.Interval = TimeSpan.FromMilliseconds(200);    //시간간격 설정
            timer.Tick += new EventHandler(timer_Tick);          //이벤트 추가
            timer.Start();
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            if (GlobalData.Current.SendTagEvent == "Ports") //불필요한 I/O 보드 Read 방지
            {
                //컨베이어 Entry,Stop 상태 조회 및 UI갱신
                foreach (var cvItem in CVUserControlDic)
                {
                    //cvItem.Value.ChangeEntryStopState(cvItem.Value.GetCVModule().IN_ENTRY, cvItem.Value.GetCVModule().IN_STOP);
                }
            }
            button_ModuleCustom1.Content = LastSelected?.GetCVModule().CustomActionName1;
            button_ModuleCustom2.Content = LastSelected?.GetCVModule().CustomActionName2;
            button_ModuleCustom3.Content = LastSelected?.GetCVModule().CustomActionName3;
        }

        public void UpdateBankBayLevel()
        {
            if (LastSelected != null)
            {
                string tag = LastSelected.GetCVModule().GetRobotCommandTag();

                SetDGRMTeachData(tag, true);
                SetDGRBankBayLevel(tag);
            }

        }
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            MainPaneloffset = gMain.ActualWidth;

            MainPaneloffset = grdMainLayOut.ActualWidth;
        }

        #region Create init

        private void GdSetHeader(string tag, DataGrid Dg )
        {
            List<GridItemListItemInfo> lsititem = GlobalData.Current.GetGridItemList(tag);

            Dg.Columns.Clear();

            foreach (var item in lsititem)
            {
                DataGridTextColumn addedcol = new DataGridTextColumn();

                addedcol.HeaderStyle = GetStyle(true);
                addedcol.CellStyle = GetStyle(false);

                if (item.GridItem.Contains("\\"))        //\ 있다면 \를 기준으로 띄워쓰기 해준다.
                {
                    addedcol.Header = item.GridItem.Replace("\\", "\n");
                }
                else
                    addedcol.Header = item.GridItem;

                addedcol.Binding = new Binding(item.BindingItem);
                addedcol.Width = item.GridWidth;
                addedcol.IsReadOnly = true;

                Dg.Columns.Add(addedcol);
                ListHeader.Add(addedcol.Header.ToString());
            }

        }

        private Style GetStyle(bool bHeader)
        {
            Style retStyle = new Style();

            try
            {

                if (bHeader)
                {
                    retStyle.Setters.Add(new Setter
                    {
                        Property = FontSizeProperty,
                        Value = 14.0
                    });

                    retStyle.Setters.Add(new Setter
                    {
                        Property = System.Windows.Controls.Control.HorizontalAlignmentProperty,
                        Value = HorizontalAlignment.Stretch
                    });
                    retStyle.Setters.Add(new Setter
                    {
                        Property = System.Windows.Controls.Control.ForegroundProperty,
                        Value = Brushes.Green
                    });
                    retStyle.Setters.Add(new Setter
                    {
                        Property = System.Windows.Controls.Control.HorizontalContentAlignmentProperty,
                        Value = HorizontalAlignment.Center
                    });
                    retStyle.Setters.Add(new Setter
                    {
                        Property = System.Windows.Controls.Control.VerticalContentAlignmentProperty,
                        Value = VerticalAlignment.Center
                    });
                }
                else
                {
                    retStyle.Setters.Add(new Setter
                    {
                        Property = FontSizeProperty,
                        Value = 13.0
                    });

                    retStyle.Setters.Add(new Setter
                    {
                        Property = TextBlock.TextAlignmentProperty,
                        Value = TextAlignment.Center
                    });
                    retStyle.Setters.Add(new Setter
                    {
                        Property = System.Windows.Controls.Control.ForegroundProperty,
                        Value = Brushes.BlueViolet
                    });
                    retStyle.Setters.Add(new Setter
                    {
                        Property = System.Windows.Controls.Control.HorizontalContentAlignmentProperty,
                        Value = HorizontalAlignment.Center
                    });
                    retStyle.Setters.Add(new Setter
                    {
                        Property = System.Windows.Controls.Control.VerticalContentAlignmentProperty,
                        Value = VerticalAlignment.Center
                    });
                }
      
            }
            catch (Exception ex)
            {
                Log.LogManager.WriteCSLog(eLogLevel.Fatal, System.Reflection.MethodBase.GetCurrentMethod().ToString() + "Fail");      //200509 HHJ MaskProject    //MainWindow Event 추가
                MessageBox.Show(string.Format(ex.ToString()));
            }

            return retStyle;
        }

        private void InitControl()
        {
            //2021.04.08 lim,
            if (GlobalData.Current.UseBCR)
            {
                button_RFID_Read.Content = "BCR Read"; // GlobalData.Current.UseBCR ? "BCR Read" : "RFID Read";
                //button_RFID_Write.Visibility = Visibility.Collapsed;
            }

            button.Visibility = GlobalData.Current.GlobalSimulMode ? Visibility.Visible : Visibility.Collapsed;
            button1.Visibility = GlobalData.Current.GlobalSimulMode ? Visibility.Visible : Visibility.Collapsed;
            button2.Visibility = GlobalData.Current.GlobalSimulMode ? Visibility.Visible : Visibility.Collapsed;
            button3.Visibility = GlobalData.Current.GlobalSimulMode ? Visibility.Visible : Visibility.Collapsed;
            button_SimulTray.Visibility = GlobalData.Current.GlobalSimulMode ? Visibility.Visible : Visibility.Collapsed;
            LoadConveyorLayout(grdMainLayOut,  GlobalData.Current.CurrentFilePaths(System.Environment.CurrentDirectory) + GlobalData.Current.PortUI1F_ConfigPath);
            LoadConveyorLayout(grdUpperLayOut, GlobalData.Current.CurrentFilePaths(System.Environment.CurrentDirectory) + GlobalData.Current.PortUI2F_ConfigPath);
        }
        private void LoadConveyorLayout(Grid TargetGrid,string UIConfigPath)
        {
            try
            {
                PortUIItemList PList = PortUIItemList.Deserialize(UIConfigPath);

                int XCount = PList.GetUI_XSize() + 3;
                int YCount = PList.GetUI_YSize() + 3;
                int Booth = PList.Get_BoothPos();

                TargetGrid.Width = 100 * XCount;
                TargetGrid.HorizontalAlignment = HorizontalAlignment.Stretch;
                TargetGrid.VerticalAlignment = VerticalAlignment.Stretch;
                TargetGrid.ColumnDefinitions.Clear();
                for (int i = 0; i < XCount; i++)
                {
                    ColumnDefinition colDef1 = new ColumnDefinition();
                    TargetGrid.ColumnDefinitions.Add(colDef1);
                }
                TargetGrid.Children.Clear();

                #region 포트 레이아웃
                foreach (var lineItem in GlobalData.Current.PortManager.ModuleList)
                {
                    foreach (var CVItem in lineItem.Value.ModuleList)
                    {
                        if (PList[CVItem.ModuleName] == null)
                            continue;

                        CVUserControl CV = new CVUserControl(CVItem);

                        //2021.06.10 lim, front 쪽에 위치한 in/out port 모두 방향을 맞추기 위해서 CVUserControl에서 방향 설정
                        CV.ChangeTrayAdvanceDirection(PList[CVItem.ModuleName].ControlDirection);
                        if (PList[CVItem.ModuleName].RotateAngle > 0)
                        {
                            CV.SetControlRotate(PList[CVItem.ModuleName].RotateAngle);
                        }
                        CV.MouseDown += CV_MouseDown;
                        CV.MouseDoubleClick += CV_MouseDoubleClick;
                        CVUserControlDic.Add(CVItem.ModuleName, CV);
                        Grid.SetColumn(CV, PList[CVItem.ModuleName].XPosition);
                        Grid.SetRow(CV, PList[CVItem.ModuleName].YPosition);

                        //if(PList[CVItem.ModuleName].Bank == 1)
                        //    Grid.SetRow(CV, Booth - PList[CVItem.ModuleName].YPosition);
                        //else
                        //    Grid.SetRow(CV, PList[CVItem.ModuleName].YPosition + Booth);

                        TargetGrid.Children.Add(CV);
                    }
                }
                #endregion

                #region 부스 표현
                foreach (var bItem in PList)
                {
                    if (bItem.UIType == "Block")
                    {
                        //컨베이어 위에 1F Booth 표현
                        Border border = new Border();
                        border.Height = 80;
                        border.Width = 100;
                        border.BorderBrush = new SolidColorBrush(Colors.LightGray);
                        border.BorderThickness = new Thickness(1);
                        border.Background = new SolidColorBrush(Colors.White);
                        Grid.SetColumn(border, bItem.XPosition);
                        Grid.SetRow(border, bItem.YPosition);

                        //if (bItem.Bank == 1)
                        //    Grid.SetRow(border, Booth - bItem.YPosition);
                        //else
                        //    Grid.SetRow(border, bItem.YPosition + Booth);


                        Label lbl = new Label();
                        lbl.Content = bItem.Text;
                        lbl.FontWeight = FontWeights.Bold;
                        lbl.FontSize = 18;
                        lbl.VerticalAlignment = VerticalAlignment.Center;
                        lbl.HorizontalAlignment = HorizontalAlignment.Center;
                        border.Child = lbl;
                        TargetGrid.Children.Add(border);
                    }
                }
                #endregion
            
               

            }
            catch (Exception ex)
            {
                
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
            }
        }



        private void CV_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ViewDetailPortState();
        }

        private void CV_MouseDown(object sender, MouseButtonEventArgs e)
        {
            CVUserControl CVC = sender as CVUserControl;

            if(LastSelected != null)
            {
                LastSelected.BorderThickness = new Thickness(1);
                LastSelected.BorderBrush = Brushes.Transparent;
            }
            if(CVC != null)
            {
                CV_BaseModule TargetCV = CVC.GetCVModule();
                textBox_CVModule.Text = TargetCV.ModuleName;
                textBox_CVType.Text = TargetCV.CVModuleType.ToString();
                textBox_CVCommand.Text = TargetCV.NextCVCommand.ToString();
                textBox_LineModule.Text = TargetCV.ParentModule.ModuleName;
                textBox_CarrierID.Text = TargetCV.GetCarrierID();

                //2021.05.24 lim, OHTRobot 포트는 컨베이어가 없음, TurnOHTIF 후방 스톱퍼, 턴, 역회전 사용
                this.button_LineINVStop.IsEnabled = (TargetCV.CVModuleType != eCVType.OHTRobot);
                this.button_Stop.IsEnabled = (TargetCV.CVModuleType != eCVType.OHTRobot);
                this.button_FH.IsEnabled = (TargetCV.CVModuleType != eCVType.OHTRobot);
                this.button_FL.IsEnabled = (TargetCV.CVModuleType != eCVType.OHTRobot);

                this.button_RFID_Read.IsEnabled = TargetCV.UseBCR;
                this.button_Turn_Home.IsEnabled = TargetCV.UseServoHomeMode;

                this.button_MuteOn.IsEnabled = TargetCV.UseLightCurtain;
                this.button_MuteOff.IsEnabled = TargetCV.UseLightCurtain;

                this.button_Door_Open.IsEnabled = TargetCV.UseDoor;
                this.button_Door_Close.IsEnabled = TargetCV.UseDoor;

                this.button_StopperFWD_Up.IsEnabled = TargetCV.UseStopper;
                this.button_StopperFWD_Down.IsEnabled = TargetCV.UseStopper;

                bool BWDStopperUse = (TargetCV.CVModuleType == eCVType.Turn || TargetCV.CVModuleType == eCVType.OHTIF ||
                    TargetCV.CVModuleType == eCVType.TurnEQIF || TargetCV.CVModuleType == eCVType.TurnOHTIF ||
                    TargetCV.CVModuleType == eCVType.TurnBridge || TargetCV.CVModuleType == eCVType.Stacker || TargetCV.CVModuleType == eCVType.StackerBox || TargetCV.CVModuleType == eCVType.ShuttleTurn);

                BWDStopperUse = TargetCV.UseBackStopper || BWDStopperUse; //211224 RGJ Plain CV BackStopper 기능 추가.

                this.button_StopperBWD_Up.IsEnabled = TargetCV.UseStopper && BWDStopperUse;
                this.button_StopperBWD_Down.IsEnabled = TargetCV.UseStopper && BWDStopperUse;


                this.button_Turn.IsEnabled   = (TargetCV.CVModuleType == eCVType.Turn || TargetCV.CVModuleType == eCVType.TurnEQIF || TargetCV.CVModuleType == eCVType.TurnOHTIF || TargetCV.CVModuleType == eCVType.TurnBridge || TargetCV.CVModuleType == eCVType.ShuttleTurn);
                this.button_Return.IsEnabled = (TargetCV.CVModuleType == eCVType.Turn || TargetCV.CVModuleType == eCVType.TurnEQIF || TargetCV.CVModuleType == eCVType.TurnOHTIF || TargetCV.CVModuleType == eCVType.TurnBridge || TargetCV.CVModuleType == eCVType.ShuttleTurn);

                if(string.IsNullOrEmpty(TargetCV.CustomActionName1))
                {
                    button_ModuleCustom1.Content = "";
                    button_ModuleCustom1.IsEnabled = false;
                }
                else
                {
                    button_ModuleCustom1.IsEnabled = true;
                    this.button_ModuleCustom1.Content = TargetCV.CustomActionName1;
                    this.button_ModuleCustom1.Tag = TargetCV.CustomActionTag1;
                }
                if (string.IsNullOrEmpty(TargetCV.CustomActionName2))
                {
                    button_ModuleCustom2.Content = "";
                    button_ModuleCustom2.IsEnabled = false;
                }
                else
                {
                    button_ModuleCustom2.IsEnabled = true;
                    this.button_ModuleCustom2.Content = TargetCV.CustomActionName2;
                    this.button_ModuleCustom2.Tag = TargetCV.CustomActionTag2;
                }
                if (string.IsNullOrEmpty(TargetCV.CustomActionName3))
                {
                    button_ModuleCustom3.Content = "";
                    button_ModuleCustom3.IsEnabled = false;
                }
                else
                {
                    button_ModuleCustom3.IsEnabled = true;
                    this.button_ModuleCustom3.Content = TargetCV.CustomActionName3;
                    this.button_ModuleCustom3.Tag = TargetCV.CustomActionTag3;
                }

                this.button_SimulTray.IsEnabled = GlobalData.Current.GlobalSimulMode && TargetCV.IsInPort;

                this.button_RH.IsEnabled  = (TargetCV.CVModuleType == eCVType.Turn || TargetCV.CVModuleType == eCVType.TurnEQIF || TargetCV.CVModuleType == eCVType.TurnOHTIF || TargetCV.CVModuleType == eCVType.TurnBridge || TargetCV.CVModuleType == eCVType.ShuttleTurn); //역회전은  턴포트만 존재
                this.button_RL.IsEnabled  = (TargetCV.CVModuleType == eCVType.Turn || TargetCV.CVModuleType == eCVType.TurnEQIF || TargetCV.CVModuleType == eCVType.TurnOHTIF || TargetCV.CVModuleType == eCVType.TurnBridge || TargetCV.CVModuleType == eCVType.ShuttleTurn);

                this.button_ForceUnload.Visibility = (TargetCV.CVModuleType == eCVType.Stacker) ? Visibility.Visible : Visibility.Hidden;

                this.textBox_CVStep.Text = TargetCV.LocalActionStep.ToString();

                CVC.BorderThickness = new Thickness(3);
                CVC.BorderBrush = Brushes.OrangeRed;
                LastSelected = CVC;
                UpdateBankBayLevel();
            }
        }

        private void SetDGRMTeachData(string Tag,bool bFront)
        {
            PvalitemList.Clear();
            foreach (var item in GlobalData.Current.mRMManager.ModuleList)
            {
                if (bFront)
                    shefitem = item.Value.FrontData.Where(r => r.TagName == Tag).FirstOrDefault();
                else
                    shefitem = item.Value.RearData.Where(r => r.TagName == Tag).FirstOrDefault();
                if (shefitem != null)
                {
                    PvalitemList.Add(new SortingRecClass(shefitem, item.Key));
                }
            }
        }
        
        private void SetDGRBankBayLevel(string Tag)
        {
            BankBayLevl.Clear();

            BankBayLevl.Add(new SortingRecClass(Tag));

            if (BankBayLevl != null)
            {
                DGBankBayLevel.ItemsSource = BankBayLevl;
                DGBankBayLevel.Items.Refresh();
            }
        }

        #endregion

        #region Zoom관련 Event

        #region Event 관련

       
        #endregion
        private void btnAnimate_Click(object sender, RoutedEventArgs e)
        {
            // We may have already set the LayoutTransform to a ScaleTransform.
            // If not, do so now.

            var scaler = gMain.LayoutTransform as ScaleTransform;
            
            if (scaler == null)
            {
                scaler = new ScaleTransform(1.0, 1.0);
                gMain.LayoutTransform = scaler;
            }

            // We'll need a DoubleAnimation object to drive 
            // the ScaleX and ScaleY properties.
            DoubleAnimation animator = new DoubleAnimation()
            {
                Duration = new Duration(TimeSpan.FromMilliseconds(600)),
            };

            // Toggle the scale between 1.0 and 1.5.

            if (scaler.ScaleX == 1.0)
            {
                animator.To =0.1;
            }
            else
            {
                animator.To = 1.0;
            }

            scaler.BeginAnimation(ScaleTransform.ScaleXProperty, animator);
            scaler.BeginAnimation(ScaleTransform.ScaleYProperty, animator);
        }

        private void slider1_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            // The user is clicking on the slider, probably about to drag it.
            var scaler = gMain.LayoutTransform as ScaleTransform;

            if (scaler != null && scaler.HasAnimatedProperties)
            {
                // This means the current ScaleX and ScaleY properties were set via
                // animation, which has a higher value precedence than a locally set
                // value, so we need to remove the animation by setting a null 
                // AnimationTimeline before we can set a local value when the user
                // drags the slider (in slider1_ValueChanged).

                scaler.ScaleX = scaler.ScaleX;
                scaler.ScaleY = scaler.ScaleY;

                // Remove the animation, causing the local values (set above) to apply.
                scaler.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                scaler.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            }
        }

        private void slider1_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (gMain == null) return;

            var scalerMain = gMain.LayoutTransform as ScaleTransform;

            var scalerUpper = grdUpperLayOut.LayoutTransform as ScaleTransform;

            if (scalerMain == null)
            {
                grdMainLayOut.LayoutTransform = new ScaleTransform(slider1.Value, slider1.Value);
            }
            else
            {
                scalerMain.ScaleX = slider1.Value;
                scalerMain.ScaleY = slider1.Value;
            }

            if (scalerUpper == null)
            {
                grdUpperLayOut.LayoutTransform = new ScaleTransform(slider1.Value, slider1.Value);
            }
            else
            {
                scalerUpper.ScaleX = slider1.Value;
                scalerUpper.ScaleY = slider1.Value;
            }
        }

        private void btnInstant_Click(object sender, RoutedEventArgs e)
        {
            var scaler = gMain.LayoutTransform as ScaleTransform;
           // var scaler = mainPanel.LayoutTransform as ScaleTransform;

            if (scaler == null)
            {
                // Currently no zoom, so go instantly to max zoom.
                // mainPanel.LayoutTransform = new ScaleTransform(1.5, 1.5);
                gMain.LayoutTransform = new ScaleTransform(1, 1);
            }
            else
            {
                double curZoomFactor = scaler.ScaleX;

                // If the current ScaleX and ScaleY properties were set by animation,
                // we'll have to remove the animation before we can explicitly set
                // them to "local" values.

                if (scaler.HasAnimatedProperties)
                {
                    // Remove the animation by assigning a null 
                    // AnimationTimeline to the properties.
                    // Note that this causes them to revert to 
                    // their most recently assigned "local" values.

                    scaler.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                    scaler.BeginAnimation(ScaleTransform.ScaleYProperty, null);
                }

                if (curZoomFactor == 0.1)
                {
                    scaler.ScaleX = 1;
                    scaler.ScaleY = 1;
                }
                else
                {
                    scaler.ScaleX = 0.1;
                    scaler.ScaleY = 0.1;
                }
            }
        }
        

        private void btnZoomP_Click(object sender, RoutedEventArgs e)
        {
            if (slider1 != null)
            {
                slider1.Value = slider1.Value + 0.05;
            }
        }

        private void btnZoomN_Click(object sender, RoutedEventArgs e)
        {
            if (slider1 != null)
            {
                slider1.Value = slider1.Value - 0.05;
            }
        }
        #endregion
        
        #region Tree 관련

        
        private TreeViewItem GetTreeView(string text, Color boxColor)
        {
            TreeViewItem item = new TreeViewItem();
            item.IsExpanded = true;

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
        private TreeViewItem GetTreeView(string text, string val , string imagePath)
        {
            TreeViewItem item = new TreeViewItem();

            item.IsExpanded = true;

            // create stack panel
            StackPanel stack = new StackPanel();
            stack.Orientation = Orientation.Horizontal;

            // create Image
            Image image = new Image();

            //image.Source = new BitmapImage(new Uri("pack://application:,,/Images/" + imagePath));

            BitmapImage result = new BitmapImage();

            result.BeginInit();

            //string p = GlobalData.Current.CurrentFilePaths(System.Environment.CurrentDirectory) + "\\Image\\Led_Blue.png";
            string p = GlobalData.Current.CurrentFilePaths(System.Environment.CurrentDirectory) + imagePath;
            result.UriSource = new Uri(p);
            result.EndInit();

            image.Source = result;
            
            // Label
            Label lbl = new Label();
            lbl.Content = text + " : " + val;
            
            // Add into stack
            stack.Children.Add(image);
            stack.Children.Add(lbl);

            // assign stack to header
            item.Header = stack;
            return item;

        }
        #endregion

        private void GridSplitter_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
             slider1.Value = (grdMainLayOut.ActualWidth / MainPaneloffset) - 0.1 ;
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            foreach(var CVItem in CVUserControlDic)
            {
                
                if (!TestRed)
                {
                    CVItem.Value.CVStopAnimate();
                    CVItem.Value.ChangeStateLampColor();
                }
                else
                {
                    CVItem.Value.CVRunAnimate();
                    CVItem.Value.ChangeStateLampColor(eCVAutoManualState.Auto);
                }
            }
            TestRed = !TestRed;
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {


            foreach (var CVItem in CVUserControlDic)
            {
                if (TestTurn)
                {
                    CVItem.Value.CVTurn_Animate();
                }
                else
                {
                    CVItem.Value.CVReturn_Animate();
                }
            }
            TestTurn = !TestTurn;
        }
        private void button2_Click(object sender, RoutedEventArgs e)
        {
            foreach (var CVItem in CVUserControlDic)
            {
                CVItem.Value.ChangeStopperView(StopperUp ? eCV_StopperState.Up : eCV_StopperState.Down, true);
                CVItem.Value.ChangeStopperView(StopperUp ? eCV_StopperState.Up : eCV_StopperState.Down, false);
            }
            this.StopperUp = !StopperUp;
        }
        private void button3_Click(object sender, RoutedEventArgs e)
        {
            foreach (var CVItem in CVUserControlDic)
            {
                CVItem.Value.ChangeLightCurtainView(LightCurtain);
            }
            LightCurtain = !LightCurtain;
        }
        private void CVLine_CommandClick(object sender, RoutedEventArgs e)
        {
            if (LastSelected == null)
            {
                return;
            }
            CVLineModule SelectedCVLine = LastSelected.GetCVModule().ParentModule as CVLineModule;
            if (SelectedCVLine == null)
                return;
            Button b = sender as Button;
            string Command = b.Tag.ToString();

            if (Command == "Line_Abort")
            {
                if (SelectedCVLine.LocalRunStep != 1)
                {
                    System.Windows.MessageBox.Show("현재 해당 라인은 전송 동작중이 아닙니다.", "작업 상태 확인", MessageBoxButton.OK);
                    return;
                }

                string msg = string.Format("{0} 라인에 진행중인 전송동작을 중단 하시겠습니까?.", SelectedCVLine.ModuleName);
                MessageBoxResult result = System.Windows.MessageBox.Show(msg, "작업 중단 확인", MessageBoxButton.OKCancel);
                if (result == MessageBoxResult.OK)
                {
                    SelectedCVLine.RequestAbortTransAction();
                    return;
                }
            }
            else if(Command == "Line_Reset")
            {
                if (SelectedCVLine.LocalRunStep != CVLineModule.ErrorStep)
                {
                    System.Windows.MessageBox.Show("현재 해당 라인은 에러 상태가 아닙니다.", "작업 상태 확인", MessageBoxButton.OK);
                    return;
                }

                string msg = string.Format("{0} 라인에 에러를 리셋하시겠습니까? 다시 트레이 로딩 스텝으로 되돌립니다.\r\n[경고: 라인내 실물 트레이가 없는 상태에서 실행하십시오.]", SelectedCVLine.ModuleName);
                MessageBoxResult result = System.Windows.MessageBox.Show(msg, "리셋 확인", MessageBoxButton.OKCancel);
                if (result == MessageBoxResult.OK)
                {
                    SelectedCVLine.RequestErrorReset();
                    return;
                }
            }
            else if(Command == "Line_CV_Stop")
            {
                SelectedCVLine.LineEmergencyAction();
            }
        }
        private void CV_CommandClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (LastSelected == null) { return; }
                var CVModule = LastSelected.GetCVModule();
                Button b = sender as Button;
                string Command = b.Tag.ToString();
                if (Command == "DetailView")
                {
                    ViewDetailPortState();
                }
                else if (Command == "CV_EStop" || Command == "CV_Stop")
                {
                    //정지 명령은 확인창을 스킵
                    Task<bool> MunualTask = Task<bool>.Factory.StartNew(() =>
                    {
                        return CVModule.CV_ManualCommandAction(Command);
                    });
                }
                else if(Command == "Door_Open" && GlobalData.Current.MainBooth.SCState == eSCState.AUTO)
                {
                    MessageBoxResult result = System.Windows.MessageBox.Show("현재 Auto 모드입니다. 장비를 정지하고 다시 시도 하세요.", "LBS Run 상태 체크", MessageBoxButton.OK);
                    return;
                }
                else
                {
                    string msg = string.Format("{0} Conveyor 모듈에  [{1}]  메뉴얼 명령을 실행하시겠습니까?.", LastSelected.GetCVModule().ModuleName, Command);
                    MessageBoxResult result = System.Windows.MessageBox.Show(msg, "Conveyor Manual Command", MessageBoxButton.OKCancel);
                    if (result == MessageBoxResult.OK)
                    {
                        Task<bool> MunualTask = Task<bool>.Factory.StartNew(() =>
                        {
                            return CVModule.CV_ManualCommandAction(Command);
                        });
                    }
                    else
                    {
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
            }

        }
       

        private void Page_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            slider1.Value = slider1.Value + ((double)(e.Delta)) / 2000;
        }

        private void button_Cancel_Trans_Click(object sender, RoutedEventArgs e)
        {

        }
        private void ViewDetailPortState()
        {
            if (LastSelected == null)
                return;
            PortDetailWindow PDW = new PortDetailWindow(LastSelected.GetCVModule());
            PDW.Owner = Application.Current.MainWindow; // We must also set the owner for this to work.
            PDW.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            PDW.ShowDialog();
        }
    }
}
