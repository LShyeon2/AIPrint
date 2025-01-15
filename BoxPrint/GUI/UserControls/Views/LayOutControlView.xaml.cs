using Newtonsoft.Json;
using BoxPrint.DataList;
using BoxPrint.DataList.MCS;
using BoxPrint.GUI.ETC;
using BoxPrint.GUI.ETC.LoadingPopup;
using BoxPrint.GUI.ExtensionCollection;
using BoxPrint.GUI.UIControls;
using BoxPrint.GUI.UserControls.ViewModels;
using BoxPrint.GUI.Views;
using BoxPrint.Log;
using BoxPrint.Modules;
using BoxPrint.Modules.Conveyor;
using BoxPrint.Modules.CVLine;
using BoxPrint.Modules.RM;
using BoxPrint.Modules.Shelf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using TranslationByMarkupExtension;
using WCF_LBS.Commands;
using static BoxPrint.Modules.RM.RMModuleBase;

namespace BoxPrint.GUI.UserControls.Views
{
    /// <summary>
    /// LayOutControlView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class LayOutControlView : UserControl
    {
        #region Variable
        private Dictionary<string, FrameworkElement> ShelfControlList = new Dictionary<string, FrameworkElement>();

        DispatcherTimer timer = new DispatcherTimer();    //객체생성

        private Storyboard _storyboardRM1Position = new Storyboard();
        private Storyboard _storyboardRM2Position = new Storyboard();
        private double _oldRM1Position, _oldRM2Position;

        private double ShelfSize;
        private double ShelfThick = 0;  //1고정

        private double ShelfDividThick = 3;
        private int ShelfDividCount = 5;
        private Brush ShelfDividBrushes = Brushes.Black;

        bool LoadComp = false;

        public LayOutControlViewModel vm { get; private set; }

        private readonly int ControlMaxHeight = 60;     //UI생성에 필요한 최대 높이

        //230217 HHJ SCS 개선     //PlayBack에서도 해당 Control을 사용하도록 추가
        private bool PlayBackControl = false;
        #endregion

        public DateTime mouseDownTime;
        //클릭위치저장
        private Point clickPosition;

        private ePortInOutType[] oldCvPortInOut;
        private bool bReload = false;

        private string CurLanguage;

        #region Constructor
        public LayOutControlView(bool playbackControl)
        {
            InitializeComponent();
          
            //MapView._EventHandler_EQPIDChange += new MapView.EventHandler_EQPIDChange(page_reLoaded);

            //230217 HHJ SCS 개선     //PlayBack에서도 해당 Control을 사용하도록 추가
            PlayBackControl = playbackControl;

            vm = new LayOutControlViewModel(PlayBackControl);

            DataContext = vm;

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(200); //UI 갱신에 부하가 많아서 딜레이 수정 30 -> 100 
            timer.Tick += new EventHandler(timer_Tick);

            CurLanguage = TranslationManager.Instance.CurrentLanguage.ToString();

            string banktrans = TranslationManager.Instance.Translate("BANK Tag").ToString();
            string strMessage = string.Format(banktrans,
                                       GlobalData.Current.FrontBankNum.ToString());
            txtFrontBank.Text = strMessage;

            strMessage = string.Format(banktrans,
                           GlobalData.Current.RearBankNum.ToString());
            txtRearBank.Text = strMessage;
        }

        public void CloseView()
        {
            timer.Stop();
            ShelfControlList.Clear();
            //MapView._EventHandler_EQPIDChange -= new MapView.EventHandler_EQPIDChange(page_reLoaded);
            GC.Collect();
        }
        #endregion

        #region Methods
        #region Timer
        private void timer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (this.IsVisible)
                {
                    if (LodingPopup.Instance.Visibility == Visibility.Visible)
                    {
                        LodingPopup.Instance.AutoStop();

                    }

                    RmPostionUpate();

                    if(GlobalData.Current.ServerClientType == eServerClientType.Client)
                        ConveyorUpdate();

                    if (CurLanguage != TranslationManager.Instance.CurrentLanguage.ToString())
                    {
                        string banktrans = TranslationManager.Instance.Translate("BANK Tag").ToString();
                        string strMessage = string.Format(banktrans,
                                                   GlobalData.Current.FrontBankNum.ToString());
                        txtFrontBank.Text = strMessage;

                        strMessage = string.Format(banktrans,
                                       GlobalData.Current.RearBankNum.ToString());
                        txtRearBank.Text = strMessage;
                        
                        CurLanguage = TranslationManager.Instance.CurrentLanguage.ToString();
                    }
                    #region 쉘프 NG 표기 업데이트
                    foreach(var ShelfItem in  GlobalData.Current.ShelfMgr.AllData)
                    {
                        if(ShelfItem != null)
                        {
                            CarrierItem ShelfCarrier = ShelfItem.InSlotCarrier;
                            if(ShelfCarrier != null)
                            {
                                if(ShelfCarrier.ValidationNG == "1")
                                {
                                    ShelfItem.ShelfNGState = 1;
                                }
                                else
                                {
                                    ShelfItem.ShelfNGState = 0;
                                }
                            }
                            else
                            {
                                ShelfItem.ShelfNGState = 0;
                            }
                        }
                    }
                    #endregion


                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
            }

        }
        #endregion

        #region Event
        #region Load, Unload
        /// <summary>
        /// 컨트롤 로드
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Control_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!LoadComp)
                {
                    SetShelfSize();
                    CanvasRMVisibility();

                    InitControl(bReload);
                    InitRMID();

                    ChangeBankName();

                    LogManager.WriteConsoleLog(eLogLevel.Error, "Layout LoadComp");

                    LoadComp = true;
                    GlobalData.Current.LayoutLoadComp = true;
                    timer.Start();

                    //230321 HHJ SCS 개선     //- CraneOrder Window 추가
                    //vm.SetSelectUnit(GlobalData.Current.mRMManager.FirstRM, false);        //230106 HHJ SCS 개선     //기본 선택은 RM1으로
                    vm.SetSelectUnit(CanvasRM1, false);        //230106 HHJ SCS 개선     //기본 선택은 RM1으로

                    //230316 s
                    if (GlobalData.Current.ServerClientType == eServerClientType.Client)
                    {
                        int count = 0;

                        foreach (var lineitem in GlobalData.Current.PortManager.ModuleList.Values)
                        {
                            count += lineitem.ModuleList.Count;
                        }

                        oldCvPortInOut = new ePortInOutType[count];
                        count = 0;
                        foreach (var lineitem in GlobalData.Current.PortManager.ModuleList.Values)
                        {
                            foreach (var cvitem in lineitem.ModuleList)
                            {
                                oldCvPortInOut[count] = cvitem.PortInOutType;
                                count++;
                            }
                        }
                    }
                    //230316 e
                    if (bReload == true)
                    {
                        GlobalData.Current.MapChangeForClient();
                        bReload = false;
                        GlobalData.Current.MapViewStart = false;
                    }

                    GlobalData.Current.MRE_MapViewChangeEvent.Set();

                    GlobalData.Current.ConsoleWindow();
                    
                }

                if (timer.IsEnabled == false)
                {
                    timer.Tick += timer_Tick;
                    timer.Start();
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
            }
        }
        /// <summary>
        /// 컨트롤 언로드
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Control_Unloaded(object sender, RoutedEventArgs e)
        {
            timer.Stop();
        }
        #endregion

        #region Mouse
        /// <summary>
        /// UnitControl MouseLeave ToolTip Clear
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ToolTipControl_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is UIControlShelf shelf)
            {
                shelf.ToolTip = null;
            }
            else if (sender is UIControlRM rm)
            {
                rm.ToolTip = null;
            }
            else if (sender is UIControlCV cv)
            {
                cv.ToolTip = null;
            }
        }
        /// <summary>
        /// UnitControl MouseLeave ToolTip Create
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ToolTipControl_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is UIControlShelf shelf)
            {
                ControlBase item = GUIExtensionCollection.GetUnit(shelf, PlayBackControl);
                if (item is null) return;
                ucCustomToolTip ucTooltip = new ucCustomToolTip(item);
                ToolTip tTip = new ToolTip();
                tTip.SetResourceReference(Control.StyleProperty, "CustomToolTip");
                tTip.Content = ucTooltip;

                shelf.ToolTip = tTip;
            }
            else if (sender is UIControlRM rm)
            {
                ControlBase item = GUIExtensionCollection.GetUnit(rm, PlayBackControl);
                if (item is null) return;
                ucCustomToolTip ucTooltip = new ucCustomToolTip(item);
                ToolTip tTip = new ToolTip();
                tTip.SetResourceReference(Control.StyleProperty, "CustomToolTip");
                tTip.Content = ucTooltip;

                rm.ToolTip = tTip;
            }
            else if (sender is UIControlCV cv)
            {
                ControlBase item = GUIExtensionCollection.GetUnit(cv, PlayBackControl);
                if (item is null) return;
                ucCustomToolTip ucTooltip = new ucCustomToolTip(item);
                ToolTip tTip = new ToolTip();
                tTip.SetResourceReference(Control.StyleProperty, "CustomToolTip");
                tTip.Content = ucTooltip;

                cv.ToolTip = tTip;
            }
        }
        /// <summary>
        /// UnitControl Select
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LayOutUnit_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ControlBase item = null;
            if (sender is UIControlShelf shelf)
            {
                if (shelf.DeadZone)     return;     //DeadZone True면 MouseEvent 진행하지 않는다.
                item = GUIExtensionCollection.GetUnit(shelf, PlayBackControl);
                if (item is null) return;
            }
            else if (sender is UIControlRM rm)
            {
                item = GUIExtensionCollection.GetUnit(rm, PlayBackControl);
                if (item is null) return;
            }
            else if (sender is UIControlCV cv)
            {
                item = GUIExtensionCollection.GetUnit(cv, PlayBackControl);
                if (item is null) return;
            }
            else
                return;

            vm.SetSelectUnit((UIControlBase)sender, false);
        }
        /// <summary>
        /// Border Mouse Wheel Zoom In / Out
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LayOutBorderMouseWheel(object sender, MouseWheelEventArgs e)
        {
            try
            {
                if (e.Delta > 0)
                {
                    if (vm.ScaleValue < vm.ScaleMax)
                        vm.ScaleValue += vm.ScaleTick;
                }
                else
                {
                    if (vm.ScaleValue > vm.ScaleMin)
                        vm.ScaleValue -= vm.ScaleTick;
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
            }
        }

        //231110 HHJ 실제 LayOut 바깥 영역에서도 Drag Move 가능하도록 수정
        #region UserControl MouseEvent
        /// <summary>
        /// UserControl 자체 MouseLeft Down Event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UserControl_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            mouseDownTime = DateTime.Now;
            //마우스 다운시 현재시각 저장
            clickPosition = e.GetPosition(this);
            // 초기 클릭 위치 가져오기
            LayOutViewBox.CaptureMouse();
        }
        /// <summary>
        /// UserControl 자체 MouseLeft Up Event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UserControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            LayOutViewBox.ReleaseMouseCapture();
        }
        /// <summary>
        /// UserControl 자체 Mouse Move Event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UserControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (LayOutViewBox.IsMouseCaptured && (DateTime.Now - mouseDownTime) > TimeSpan.FromSeconds(0.3))
            // 0.3초가 지날시에만 마우스 이동가능
            {
                Point currentPosition = e.GetPosition(this);
                // 초기위치에서의 이동벡터 계산
                Vector delta = currentPosition - clickPosition;
                //  Viewbox의 위치를 마우스 움직임과 동일하게 이동
                vm.Margins = new Thickness(vm.Margins.Left + delta.X, vm.Margins.Top + delta.Y, 0, 0);
                clickPosition = currentPosition;
                this.Cursor = Cursors.SizeAll;
            }
        }
        /// <summary>
        /// UserControl 자체 Mouse Wheel Event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UserControl_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            try
            {
                if (e.Delta > 0)
                {
                    if (vm.ScaleValue < vm.ScaleMax)
                        vm.ScaleValue += vm.ScaleTick;
                }
                else
                {
                    if (vm.ScaleValue > vm.ScaleMin)
                        vm.ScaleValue -= vm.ScaleTick;
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
            }
        }
        #endregion
        #endregion

        #region Shelf Context Menu
        /// <summary>
        /// Shelf Control 우클릭 Context Menu Open
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LayOutUnit_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (PlayBackControl || GlobalData.Current.CurrentUserID == null)
                return;

            ControlBase item = null;
            if (sender is UIControlShelf shelf)
            {
                if (shelf.DeadZone) return;     //DeadZone True면 MouseEvent 진행하지 않는다.
                item = GUIExtensionCollection.GetUnit(shelf, PlayBackControl);
                if (item is null) return;
            }
            else if (sender is UIControlCV cv)
            {
                item = GUIExtensionCollection.GetUnit(cv, PlayBackControl);
                if (item is null) return;
            }
            else
                return;

            vm.SetSelectUnit((UIControlBase)sender, true);
        }
        #endregion
        #endregion

        #region Create, init
        /// <summary>
        /// Shelf Size 계산
        /// </summary>
        private void SetShelfSize()
        {
            try
            {
                double curActualHeight = LayOutBorder.ActualHeight / 2;
                double curActualWidth = LayOutBorder.ActualWidth;

                //ShelfSize를 계산한다.
                double tmpShelfSize, tmpGridWidth;
                //ActualHeight / Ycount를 해서 쉘프 높이를 구한다.
                tmpShelfSize = Math.Truncate(curActualHeight / GlobalData.Current.SystemParameter.FrontYcount);

                //SuHwan_20230203 : 최대 크기 고정
                if (tmpShelfSize > 30)
                    tmpShelfSize = 30;

                if (GlobalData.Current.SystemParameter.FrontYcount < 8)
                    vm.ChangeOriginValue = (decimal)0.15;
                else
                    vm.ChangeOriginValue = (decimal)0;

                vm.SetDefaultScaleData();

                //구해진 쉘프 높이로 임시 Width값을 계산해본다.
                tmpGridWidth = tmpShelfSize * GlobalData.Current.SystemParameter.FrontXcount;
                //임시 Width값이 ActualWidth보다 크면 Xcount로 GridSize를 재조정한다.
                if (tmpGridWidth > curActualWidth)
                {
                    //ActualWidth / Xcount를 해서 쉘프 너비를 구한다.
                    tmpShelfSize = Math.Truncate(curActualWidth / GlobalData.Current.SystemParameter.FrontXcount);
                    //구해진 쉘프 높이로 임시 Width값을 계산해본다.
                    //tmpShelfSize는 Y로 했던값보다 무조건 작은 케이스. 따로 Y값으로 Height를 계산하지 않음.
                    tmpGridWidth = tmpShelfSize * GlobalData.Current.SystemParameter.FrontXcount;
                }

                //UI에서 제한하는 사이즈보다 임시 ShlefSize가 크다면 UI에서 제한하는 값으로 대체한다.
                if (tmpShelfSize > ControlMaxHeight)
                    tmpShelfSize = ControlMaxHeight;

                //구해진 임시 ShelfSize를 ShelfSize에 넣어준다.
                ShelfSize = tmpShelfSize;

                //221226 HHJ SCS 개선     //미사용 주석
                //계산을 완료하였으니 VerticalAlignment를 Center로 변경해서 Zoom에서 이상없이 사용할 수 있도록 한다.
                //DockMain.VerticalAlignment = VerticalAlignment.Center;
                //bViewLoadComp = true;

                //SuHwan_20230214 : 레일 위치 수정
                RMRail.Width = GlobalData.Current.SystemParameter.FrontXcount * ShelfSize;
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
            }
        }
        /// <summary>
        /// Crane ID Setter
        /// </summary>
        private void InitRMID()
        {
            //221129 YSW Layout RM UnitName 표시
            RMModuleBase rbase1 = GlobalData.Current.mRMManager[CanvasRM1.UnitName];
            CanvasRM1.ControlName = rbase1.ControlName;
            RMModuleBase rbase2 = GlobalData.Current.mRMManager[CanvasRM2.UnitName];
            CanvasRM2.ControlName = rbase2 == null ? "NullCrane" : rbase2.ControlName;

            CanvasRM1.IsPlayBack = PlayBackControl;
            CanvasRM2.IsPlayBack = PlayBackControl;

            //2408XX Crane Rail Playback 이상 개선      //Rail IsPlayBack 설정 누락
            RM1Rail.IsPlayBack = PlayBackControl;
            RM2Rail.IsPlayBack = PlayBackControl;
        }

        private void ChangeBankName()
        {
            CurLanguage = TranslationManager.Instance.CurrentLanguage.ToString();

            string banktrans = TranslationManager.Instance.Translate("BANK Tag").ToString();
            string strMessage = string.Format(banktrans,
                                       GlobalData.Current.FrontBankNum.ToString());
            txtFrontBank.Text = strMessage;

            strMessage = string.Format(banktrans,
                           GlobalData.Current.RearBankNum.ToString());
            txtRearBank.Text = strMessage;
        }

        /// <summary>
        /// //230329 HHJ SCS 개선     //- LayOut CV 생성기준 변경
        /// </summary>
        /// <param name="curX">현재 X(베이) 위치</param>
        /// <param name="maxX">최대 X(베이) 수량</param>
        /// <param name="bdMinBay">최저 낮은 베이 바인딩</param>
        /// <param name="bdMaxBay">최대 높은 베이 바인딩</param>
        /// <returns></returns>
        private ColumnDefinition GetGridColumnDefinition(int curX, int maxX, Binding bdMinBay, Binding bdMaxBay)
        {
            ColumnDefinition colDef = new ColumnDefinition();

            //Lowest 바인딩은 0번에서 진행
            if (curX.Equals(0))
            {
                colDef.SetBinding(ColumnDefinition.WidthProperty, bdMinBay);
            }
            //Highest 바인딩은 X -1번 에서 진행
            else if (curX.Equals(maxX - 1))
            {
                colDef.SetBinding(ColumnDefinition.WidthProperty, bdMaxBay);
            }
            else
            {
                colDef.Width = new GridLength(ShelfSize);
            }

            return colDef;
        }
        /// <summary>
        /// Grid 초기화
        /// </summary>
        /// <param name="grd">초기화 Grid</param>
        private void GridInit(Grid grd)
        {
            grd.Children.Clear();

            grd.HorizontalAlignment = HorizontalAlignment.Stretch;
            grd.VerticalAlignment = VerticalAlignment.Stretch;

            grd.ColumnDefinitions.Clear();
            grd.RowDefinitions.Clear();
        }
        /// <summary>
        /// 내부 Grid 전체 초기화
        /// </summary>
        /// <param name="X">Grid X Count</param>
        /// <param name="Y">Grid Y Count</param>
        private void GridInit(int X, int Y)
        {
            Binding bdMinBay = new Binding();
            bdMinBay.Path = new PropertyPath("MinBayCvWidth");

            Binding bdMaxBay = new Binding();
            bdMaxBay.Path = new PropertyPath("MaxBayCvWidth");

            RotateTransform rtTB = new RotateTransform();
            Binding rtbd = new Binding();
            rtbd.Source = vm;
            rtbd.Path = new PropertyPath("LayOutTextDegree");
            rtbd.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            BindingOperations.SetBinding(rtTB, RotateTransform.AngleProperty, rtbd);

            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
            {
                #region FrontShelf
                GridInit(GridFrontShelf);
                for (int i = 0; i < X; i++)
                {
                    ColumnDefinition colDef = GetGridColumnDefinition(i, X, bdMinBay, bdMaxBay);
                    GridFrontShelf.ColumnDefinitions.Add(colDef);
                }
                
                for (int i = 0; i < Y; i++)
                {
                    RowDefinition rowDef1 = new RowDefinition();
                    GridFrontShelf.RowDefinitions.Add(rowDef1);
                }
                #endregion
                #region RearShelf
                GridInit(GridRearShelf);
                for (int i = 0; i < X; i++)
                {
                    ColumnDefinition colDef = GetGridColumnDefinition(i, X,   bdMinBay, bdMaxBay);
                    GridRearShelf.ColumnDefinitions.Add(colDef);
                }
                for (int i = 0; i < Y; i++)
                {
                    RowDefinition rowDef1 = new RowDefinition();
                    GridRearShelf.RowDefinitions.Add(rowDef1);
                }
                #endregion
                #region FrontCV
                GridInit(GridFrontCV);
                for (int i = 0; i < X; i++)
                {
                    ColumnDefinition colDef = GetGridColumnDefinition(i, X,   bdMinBay, bdMaxBay);
                    GridFrontCV.ColumnDefinitions.Add(colDef);
                }
                
                for (int i = 0; i < Y; i++)
                {
                    RowDefinition rowDef1 = new RowDefinition();
                    GridFrontCV.RowDefinitions.Add(rowDef1);
                }
                #endregion
                #region RearCV
                GridInit(GridRearCV);
                for (int i = 0; i < X; i++)
                {
                    ColumnDefinition colDef = GetGridColumnDefinition(i, X,   bdMinBay, bdMaxBay);
                    GridRearCV.ColumnDefinitions.Add(colDef);
                }
                for (int i = 0; i < Y; i++)
                {
                    RowDefinition rowDef1 = new RowDefinition();
                    GridRearCV.RowDefinitions.Add(rowDef1);
                }
                #endregion
                #region FrontBay
                GridInit(GridFrontXArray);
                for (int i = 0; i < X; i++)
                {
                    ColumnDefinition colDef = GetGridColumnDefinition(i, X,   bdMinBay, bdMaxBay);
                    GridFrontXArray.ColumnDefinitions.Add(colDef);
                }
                //230329 HHJ SCS 개선     //- LayOut CV 생성기준 변경       //X -> X - 1 변경
                //for (int i = 1; i < X; i++)
                for (int i = 1; i < X - 1; i++)
                {
                    TextBlock tb = new TextBlock();
                    tb.Text = i.ToString();
                    tb.Width = ShelfSize;
                    tb.HorizontalAlignment = HorizontalAlignment.Center;
                    tb.VerticalAlignment = VerticalAlignment.Center;
                    tb.TextAlignment = TextAlignment.Center;

                    Grid.SetColumn(tb, i);
                    Grid.SetRow(tb, 0);

                    tb.RenderTransformOrigin = new Point(0.5, 0.5);
                    tb.RenderTransform = rtTB;

                    GridFrontXArray.Children.Add(tb);
                }
                #endregion
                #region RearBay
                GridInit(GridRearXArray);
                for (int i = 0; i < X; i++)
                {
                    ColumnDefinition colDef = GetGridColumnDefinition(i, X,   bdMinBay, bdMaxBay);
                    GridRearXArray.ColumnDefinitions.Add(colDef);
                }

                //230329 HHJ SCS 개선     //- LayOut CV 생성기준 변경       //X -> X - 1 변경
                //for (int i = 1; i < X; i++)
                for (int i = 1; i < X - 1; i++)
                {
                    TextBlock tb = new TextBlock();
                    tb.Text = i.ToString();
                    tb.Width = ShelfSize;
                    tb.HorizontalAlignment = HorizontalAlignment.Center;
                    tb.VerticalAlignment = VerticalAlignment.Center;
                    tb.TextAlignment = TextAlignment.Center;

                    Grid.SetColumn(tb, i);
                    Grid.SetRow(tb, 0);

                    tb.RenderTransformOrigin = new Point(0.5, 0.5);
                    tb.RenderTransform = rtTB;

                    GridRearXArray.Children.Add(tb);
                }
                #endregion
                #region FrontLevel
                GridInit(gridFrontLevel);
                for (int i = 0; i < 2; i++)
                {
                    ColumnDefinition colDef1 = new ColumnDefinition();
                    gridFrontLevel.ColumnDefinitions.Add(colDef1);
                }
                for (int i = 0; i < Y; i++)
                {
                    RowDefinition rowDef1 = new RowDefinition();
                    gridFrontLevel.RowDefinitions.Add(rowDef1);
                }
                for (int i = 0; i < Y; i++)
                {
                    TextBlock tb = new TextBlock();
                    tb.Text = (Y - i).ToString();
                    tb.HorizontalAlignment = HorizontalAlignment.Right;
                    tb.VerticalAlignment = VerticalAlignment.Center;
                    tb.TextAlignment = TextAlignment.Right;
                    tb.Margin = new Thickness(0, 0, 10, 0);

                    Grid.SetColumn(tb, 1);
                    Grid.SetRow(tb, i);

                    tb.RenderTransformOrigin = new Point(0.5, 0.5);
                    tb.RenderTransform = rtTB;

                    gridFrontLevel.Children.Add(tb);
                }
                #endregion
                #region RearLevel
                GridInit(gridRearLevel);
                for (int i = 0; i < 2; i++)
                {
                    ColumnDefinition colDef1 = new ColumnDefinition();
                    gridRearLevel.ColumnDefinitions.Add(colDef1);
                }
                for (int i = 0; i < Y; i++)
                {
                    RowDefinition rowDef1 = new RowDefinition();
                    gridRearLevel.RowDefinitions.Add(rowDef1);
                }
                for (int i = 0; i < Y; i++)
                {
                    TextBlock tb = new TextBlock();
                    tb.Text = (i + 1).ToString();
                    tb.HorizontalAlignment = HorizontalAlignment.Right;
                    tb.VerticalAlignment = VerticalAlignment.Center;
                    tb.TextAlignment = TextAlignment.Right;
                    tb.Margin = new Thickness(0, 0, 10, 0);

                    Grid.SetColumn(tb, 1);
                    Grid.SetRow(tb, i);

                    tb.RenderTransformOrigin = new Point(0.5, 0.5);
                    tb.RenderTransform = rtTB;

                    gridRearLevel.Children.Add(tb);
                }
                #endregion
            }));
        }
        /// <summary>
        /// 최저 베이 침범여부
        /// //230329 HHJ SCS 개선     //- LayOut CV 생성기준 변경
        /// </summary>
        /// <param name="cvWay">CV 방향</param>
        /// <param name="checkBase">체크 기준</param>
        /// <param name="curBay">현재 베이</param>
        /// <param name="lineCnt">내부 CV 수량</param>
        /// <returns></returns>
        private bool CheckLowestInvadeBay(eCVWay cvWay, int checkBase, int curBay, int lineCnt)
        {
            //CV방향이 우 -> 좌로 향하는 라인은 좌측의 끝단을 침범할 수 있음.
            //현재 베이에서 모듈리스트를 뺀 값이 베이스보다 작다면 침범을 하는 경우이다.
            return cvWay.Equals(eCVWay.RightToLeft) && checkBase > curBay - lineCnt;
        }
        /// <summary>
        /// 최대 베이 침범여부
        /// //230329 HHJ SCS 개선     //- LayOut CV 생성기준 변경
        /// </summary>
        /// <param name="cvWay">CV 방향</param>
        /// <param name="checkBase">체크 기준</param>
        /// <param name="curBay">현재 베이</param>
        /// <param name="lineCnt">내부 CV 수량</param>
        /// <returns></returns>
        private bool CheckHighestInvadeBay(eCVWay cvWay, int checkBase, int curBay, int lineCnt)
        {
            //CV방향이 좌 -> 우로 향하는 라인은 우측의 끝단을 침범할 수 있음.
            //현재 베이와 모듈리스트의 합이 베이스보다 크다면 침범을 하는 경우이다.
            return cvWay.Equals(eCVWay.LeftToRight) && checkBase < curBay + lineCnt;
        }
        /// <summary>
        /// LayOutControl 초기화
        /// </summary>
        private void InitControl(bool bReload = false)
        {
            #region Control set 
            vm._tmpMinBayCvWidth = 0;
            vm._tmpMaxBayCvWidth = 0;

            //230329 HHJ SCS 개선     //- LayOut CV 생성기준 변경
            int CVHighestBay = 0;
            //FrontX Count가 크면 GlobalData.Current.SystemParameter.FrontXcount값을, 아니면 GlobalData.Current.SystemParameter.RearXcount을 가져온다
            int ShelfHighestBay = GlobalData.Current.SystemParameter.FrontXcount > GlobalData.Current.SystemParameter.RearXcount ?
                    GlobalData.Current.SystemParameter.FrontXcount : GlobalData.Current.SystemParameter.RearXcount;
            int iGridXCount = 0;
            //우선 RowSpan Case는 제외하고 진행을한다.. RowSpan Case는 2D화면에서는 구현이 불가.
            bool LowestInvadeBay = false;           //CV 라인이 길이방향으로 길어졌을때 최저 컬럼보다 아래를 침범하는지
            bool HighestInvadeBay = false;          //CV 라인이 길이방향으로 길어졌을때 최대 컬럼 위보다 침범하는지

            if (!PlayBackControl)
            {
                CVHighestBay = GlobalData.Current.PortManager.ModuleList.Values.Max(c => c.Position_Bay);

                //1. ModuleList가 1보다 많아야함
                //2. CV의 방향이 LeftToRight 이거나 RightToLeft인 모듈 라인일것
                List<CVLineModule> searchCVLine = GlobalData.Current.PortManager.ModuleList.Values.Where(c => c.ModuleList.Count > 1
                        && (c.CVWay.Equals(eCVWay.LeftToRight) || c.CVWay.Equals(eCVWay.RightToLeft))).ToList();

                //각 모듈라인의 위치를 확인하여 Span으로 사용가능한지 확인한다.
                foreach (CVLineModule line in searchCVLine)
                {
                    //CV 방향이 좌 -> 우로 향하는 라인
                    //현 Bay를 기준으로 보유한 모듈 갯수만큼 더했을때 쉘프의 최대 베이보다 커지는지 확인한다.
                    if (line.CVWay.Equals(eCVWay.LeftToRight))
                    {
                        //라인의 베이와 모듈리스트의 합이 ShelfHighestBay보다 크다면 해당 라인을 위해서 최대 컬럼의 바인딩이 필요하다.
                        //HightestColUseWidthBinding = HightestColUseWidthBinding || (ShelfHighestBay < line.Position_Bay + line.ModuleList.Count);
                        HighestInvadeBay = HighestInvadeBay || CheckHighestInvadeBay(line.CVWay, ShelfHighestBay, line.Position_Bay, line.ModuleList.Count);
                    }
                    //CV 방향이 우 -> 좌로 향하는 라인
                    //현 Bay를 기준으로 보유한 모듈 갯수만큼 빼줬을대 0보다 작아지는지 확인한다.
                    else
                    {
                        //라인의 베이와 모듈리스트의 차가 0보다 작다면 해당 라인을 위해서 최저 컬럼의 바인딩이 필요하다.
                        //LowestColUseWidthBinding = LowestColUseWidthBinding || (0 > line.Position_Bay - line.ModuleList.Count);
                        LowestInvadeBay = LowestInvadeBay || CheckLowestInvadeBay(line.CVWay, 0, line.Position_Bay, line.ModuleList.Count);
                    }
                }
            }

            //CV 최대 베이가 Shelf의 최대 베이보다 크면 CV의 최대베이를 사용하고, 아니면 ShelfHighestBay를 사용한다
            iGridXCount = CVHighestBay > ShelfHighestBay ? CVHighestBay : ShelfHighestBay;
            //GridXCount는 +2해서 보내준다
            GridInit(iGridXCount + 2, GlobalData.Current.SystemParameter.RearYcount);

            ShelfControlList.Clear();

            if (!PlayBackControl)
            {
                //LoadGridShelfCreate(GridRearShelf, GlobalData.Current.MainBooth.RearData, eShelfBank.Rear);
                //LoadGridShelfCreate(GridFrontShelf, GlobalData.Current.MainBooth.FrontData, eShelfBank.Front);
                LoadGridShelfCreate(GridRearShelf, GlobalData.Current.MainBooth.RearData, GlobalData.Current.RearBankNum);
                LoadGridShelfCreate(GridFrontShelf, GlobalData.Current.MainBooth.FrontData, GlobalData.Current.FrontBankNum);
            }

            //GridArrayDisplayInfo(GridFrontCV, GridFrontShelf, eShelfBank.Front, bReload);
            //GridArrayDisplayInfo(GridRearCV, GridRearShelf, eShelfBank.Rear, bReload);
            GridArrayDisplayInfo(GridFrontCV, GridFrontShelf, GlobalData.Current.FrontBankNum, bReload);
            GridArrayDisplayInfo(GridRearCV, GridRearShelf, GlobalData.Current.RearBankNum, bReload);

            #endregion

            if (LowestInvadeBay)
                vm.MinBayCvWidth = !vm._tmpMinBayCvWidth.Equals(0) ? vm._tmpMinBayCvWidth : ShelfSize;
            else
                vm.MinBayCvWidth = ShelfSize;

            if (HighestInvadeBay)
                vm.MaxBayCvWidth = !vm._tmpMaxBayCvWidth.Equals(0) ? vm._tmpMaxBayCvWidth : ShelfSize;
            else
                vm.MaxBayCvWidth = ShelfSize;

            Canvas.SetLeft(RMRail, vm.MinBayCvWidth);

            CanvasRM1.Visibility = Visibility.Visible;
            //CanvasRM2.Visibility = (GlobalData.Current.SCSType == eSCSType.Single) ? Visibility.Hidden : Visibility.Visible;
            if (GlobalData.Current.SCSType == eSCSType.Single)
            {
                CanvasRM2.Visibility = Visibility.Hidden;
                RM2Rail.Visibility = Visibility.Hidden;

                Grid.SetColumnSpan(RM1Rail, 2);
            }
            else
            {
                CanvasRM2.Visibility = Visibility.Visible;
                RM2Rail.Visibility = Visibility.Visible;

                Grid.SetColumnSpan(RM1Rail, 1);
            }
        }

        #region Port(Conveyor)
        /// <summary>
        /// 전체 Conveyor Line 생성
        /// </summary>
        /// <param name="g"></param>
        /// <param name="shelfbank"></param>
        //private void GridArrayDisplayInfo(Grid gridCV, Grid gridShelf, eShelfBank shelfbank, bool bReload = false)
        private void GridArrayDisplayInfo(Grid gridCV, Grid gridShelf, int shelfbank, bool bReload = false)
        {
            try
            {
                int iMaxLevel = 0;
                int iMaxBay = 0;
                SafeObservableCollection<object> cvDmyList = new SafeObservableCollection<object>();

                if (!PlayBackControl)
                {
                    iMaxLevel = ShelfManager.Instance.GetMaxLevel(bReload);
                    iMaxBay = ShelfManager.Instance.GetMaxBay(bReload);

                    foreach (CVLineModule cvLine in GlobalData.Current.PortManager.ModuleList.Values)
                    {
                        cvDmyList.Add(cvLine);
                    }
                }
                foreach (object item in cvDmyList)
                {
                    int iCurBank, iCurBay, iCurLevel, iInnerUnitCount;
                    eCVWay cvWay;       //230329 HHJ SCS 개선     //- LayOut CV 생성기준 변경

                    {
                        CVLineModule dmy = item as CVLineModule;
                        iCurBank = dmy.Position_Bank;
                        iCurBay = dmy.Position_Bay;
                        iCurLevel = dmy.Position_Level;
                        iInnerUnitCount = dmy.ModuleList.Count;

                        cvWay = dmy.CVWay;      //230329 HHJ SCS 개선     //- LayOut CV 생성기준 변경
                    }

                    //생성하려는 뱅크와 추가하려는 뱅크가 같아야함.
                    if ((int)shelfbank != iCurBank)
                        continue;

                    int shelfBay = iCurBay;
                    //int shelfLevel = shelfbank.Equals(eShelfBank.Front) ? iMaxLevel - iCurLevel : iCurLevel - 1;
                    int shelfLevel = shelfbank.Equals(GlobalData.Current.FrontBankNum) ? iMaxLevel - iCurLevel : iCurLevel - 1;

                    Canvas canvas = new Canvas();
                    Grid grid = ConveyorSet(item);

                    if (grid != null)
                    {
                        //모듈리스트가 1개 이상이면 우선 제일 끝단에 있는 C/V Line
                        //좌우 끝단 바인딩 되어있는 수치 변경.
                        //1개 이상인 라인이 끝단이 아닌 중간에있다면 어떻게 처리해야할지 확인필요..
                        double cvsize = ShelfSize;

                        //230329 HHJ SCS 개선     //- LayOut CV 생성기준 변경
                        //최대 베이를 침범하는 경우
                        if (CheckHighestInvadeBay(cvWay, iMaxBay, iCurBay, iInnerUnitCount))
                        {
                            //바인딩을 하기위한 기준값을 계산한다.
                            if (vm._tmpMaxBayCvWidth < cvsize * iInnerUnitCount)
                                vm._tmpMaxBayCvWidth = cvsize * iInnerUnitCount;
                        }
                        //최저 베이를 침범하는 경우
                        else if (CheckLowestInvadeBay(cvWay, iMaxBay, iCurBay, iInnerUnitCount))
                        {
                            //바인딩을 하기위한 기준값을 계산한다.
                            if (vm._tmpMinBayCvWidth < cvsize * iInnerUnitCount)
                                vm._tmpMinBayCvWidth = cvsize * iInnerUnitCount;
                        }

                        if (cvWay.Equals(eCVWay.LeftToRight))
                        {
                            canvas.HorizontalAlignment = HorizontalAlignment.Left;
                        }
                        else if (cvWay.Equals(eCVWay.RightToLeft))
                        {
                            canvas.HorizontalAlignment = HorizontalAlignment.Right;
                        }
                        else
                        {
                            if (iCurBay.Equals(0))
                            {
                                canvas.HorizontalAlignment = HorizontalAlignment.Right;
                            }
                            else if (iCurBay.Equals(iMaxBay))
                            {
                                canvas.HorizontalAlignment = HorizontalAlignment.Left;
                            }
                            else
                            {
                                canvas.HorizontalAlignment = HorizontalAlignment.Center;
                            }
                        }
                            

                        canvas.Children.Add(grid);
                        canvas.Height = grid.Height;
                        canvas.Width = grid.Width;
                        Grid.SetColumn(canvas, shelfBay);
                        Grid.SetRow(canvas, shelfLevel);
                        gridCV.Children.Add(canvas);

                        string cvKey = ShelfTagHelper.GetTag(iCurBank, iCurBay, iCurLevel);
                        if (ShelfControlList.ContainsKey(cvKey))
                        {
                            var removeConrol = ShelfControlList[cvKey];
                            gridShelf.Children.Remove(removeConrol);

                            ShelfControlList.Remove(cvKey);
                        }

                        ShelfControlList.Add(cvKey, grid);

                        //쉘프 제일 마지막단 옆에 붙어있는 컨베이어는 ShelfControlList에 없어서 crane currnet level값이 변경되면 layout상 크레인이 이상한 곳으로 움직인다.
                        //그래서 ShelfControlList에 없는 쉘프만 임의로 넣어주자..
                        if (iCurBay > iMaxBay)
                        {
                            for (int i = 1; i <= iMaxLevel; i++)
                            {
                                cvKey = ShelfTagHelper.GetTag(iCurBank, iCurBay, i);

                                if (!ShelfControlList.ContainsKey(cvKey))
                                {
                                    ShelfControlList.Add(cvKey, grid);
                                }
                            }
                        }
                    }
                    gridCV.ClipToBounds = true;
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
            }
        }
        /// <summary>
        /// Conveyor Line 생성
        /// </summary>
        /// <param name="lineItem"></param>
        /// <param name="cvway"></param>
        /// <returns></returns>
        private Grid ConveyorSet(object item)
        {
            Grid grid = new Grid();
            int curLocation = 0;
            SafeObservableCollection<object> cvDmyList = new SafeObservableCollection<object>();
            int iCurBank, iCurBay, iCurLevel, iInnerUnitCount;
            eCVWay cvWay;

            try
            {
                {
                    CVLineModule dmy = item as CVLineModule;
                    iCurBank = dmy.Position_Bank;
                    iCurBay = dmy.Position_Bay;
                    iCurLevel = dmy.Position_Level;
                    iInnerUnitCount = dmy.ModuleList.Count;

                    cvWay = dmy.CVWay;

                    foreach (var cvLine in dmy.ModuleList)
                    {
                        cvDmyList.Add(cvLine);
                    }
                }

                double cvSize = ShelfSize;

                if (cvWay.Equals(eCVWay.LeftToRight) || cvWay.Equals(eCVWay.RightToLeft))
                {
                    for (int i = 0; i < iInnerUnitCount; i++)
                    {
                        ColumnDefinition colDef = new ColumnDefinition();
                        grid.ColumnDefinitions.Add(colDef);
                    }

                    grid.Width = iInnerUnitCount * cvSize;
                    grid.Height = cvSize;
                }
                else
                {
                    //Define the Rows 세로
                    for (int i = 0; i < iInnerUnitCount; i++)
                    {
                        RowDefinition RowDef = new RowDefinition();
                        grid.RowDefinitions.Add(RowDef);
                    }

                    grid.Width = cvSize;
                    grid.Height = iInnerUnitCount * cvSize;
                }

                foreach (var CVItem in cvDmyList)
                {
                    string curControlName, curTrackID;
                    bool curUseBCR;

                    //if (!PlayBackControl)
                    {
                        CV_BaseModule dmy = CVItem as CV_BaseModule;
                        curControlName = dmy.ControlName;
                        curTrackID = dmy.TrackID;
                        curUseBCR = dmy.UseBCR;      //일단 고정
                    }

                    UIControlCV cv = new UIControlCV();
                    cv.IsPlayBack = PlayBackControl;        //230307 HHJ SCS 개선
                    cv.UnitName = curControlName;
                    cv.SetResourceReference(Control.StyleProperty, "SK_CVStyleNew");

                    cv.Width = ShelfSize;
                    cv.Height = ShelfSize;
                    cv.HorizontalAlignment = HorizontalAlignment.Center;
                    cv.VerticalAlignment = VerticalAlignment.Center;

                    cv.CVWay = cvWay;

                    cv.MouseEnter += ToolTipControl_MouseEnter;
                    cv.MouseLeave += ToolTipControl_MouseLeave;
                    
                    //if (CVItem.CVModuleType != eCVType.WaterPool)     //230301 화재수조도 비트보이게 변경.
                    {
                        //221130 YSW 포트에 포트 경로 번호입력 TEST
                        cv.TrackID = curTrackID;
                        
                        cv.PreviewMouseLeftButtonDown += LayOutUnit_PreviewMouseLeftButtonDown; //220621 HHJ SCS 개선     //- 레이아웃 상태 추가
                        cv.PreviewMouseRightButtonDown += LayOutUnit_PreviewMouseRightButtonDown;
                    }

                    switch (cvWay)
                    {
                        case eCVWay.BottomToTop:
                            Grid.SetRow(cv, curLocation);
                            break;
                        case eCVWay.TopToBottom:
                            Grid.SetRow(cv, (iInnerUnitCount - 1) - curLocation);
                            break;
                        case eCVWay.LeftToRight:
                            Grid.SetColumn(cv, curLocation);
                            break;
                        case eCVWay.RightToLeft:
                            Grid.SetColumn(cv, (iInnerUnitCount - 1) - curLocation);
                            break;
                        default:
                            Grid.SetRow(cv, curLocation);
                            break;
                    }

                    grid.Children.Add(cv);
                    curLocation++;
                }
                return grid;
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
            }

            return grid;
        }
        #endregion

        #region Shelf
        /// <summary>
        /// Shelf 생성
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="TData"></param>
        /// <param name="bank"></param>
        //private void LoadGridShelfCreate(Grid grid, object ShelfData, eShelfBank bank)
        private void LoadGridShelfCreate(Grid grid, object ShelfData, int bank)
        {
            try
            {
                int iMaxBay = 0;
                int iMaxLevel = 0;
                SafeObservableCollection<object> shelfDmyList = new SafeObservableCollection<object>();

                //if (!PlayBackControl)
                {
                    ShelfItemList dmy = ShelfData as ShelfItemList;
                    iMaxBay = dmy.MaxBay;
                    iMaxLevel = dmy.MaxLevel;

                    foreach (ShelfItem shelf in dmy)
                    {
                        shelfDmyList.Add(shelf);
                    }
                }

                Dictionary<int, List<int>> shelfFloor = new Dictionary<int, List<int>>();
                grid.Children.Clear();

                //Bay   - Front, Rear가 동일하게 좌측부터 1
                //Level - Front는 아래부터 1, Rear는 위쪽부터 1
                foreach (object item in shelfDmyList)
                {
                    int iCurBank, iCurBay, iCurLevel, iCurFloor;
                    string strCurTagName;
                    string strInformMemo = string.Empty;        //230405 HHJ SCS 개선     //- Memo 기능 추가
                    bool CurDeadShelf = false;

                    //if (!PlayBackControl)
                    {
                        ShelfItem dmy = item as ShelfItem;
                        iCurBank = dmy.ShelfBank;
                        iCurBay = dmy.ShelfBay;
                        iCurLevel = dmy.ShelfLevel;
                        iCurFloor = dmy.FloorNum;
                        strCurTagName = dmy.TagName;

                        strInformMemo = dmy.ShelfMemo;      //230405 HHJ SCS 개선     //- Memo 기능 추가

                        CurDeadShelf = dmy.DeadZone;

                    }
                    if (!iCurBank.Equals((int)bank))
                        continue;

                    int shelfBay = iCurBay;
                    //Rear는 item.ShelfLevel - 1으로 하면 0부터 입력이니 해결됨.      //Front는 MaxLevel - Current Level해주면 됨
                    //int shelfLevel = bank.Equals(eShelfBank.Front) ? iMaxLevel - iCurLevel : iCurLevel - 1;
                    int shelfLevel = bank.Equals(GlobalData.Current.FrontBankNum) ? iMaxLevel - iCurLevel : iCurLevel - 1;

                    UIControlShelf shelf = new UIControlShelf();
                    shelf.IsPlayBack = PlayBackControl;     //230307 HHJ SCS 개선
                    shelf.UnitName = strCurTagName;
                    shelf.SetResourceReference(Control.StyleProperty, "SK_ShelfStyleNew");
                    shelf.Margin = new Thickness(ShelfThick);

                    //230405 HHJ SCS 개선     //- Memo 기능 추가
                    shelf.ShelfMemo = strInformMemo;
                    shelf.MemoPathSize = ShelfSize / 5;

                    shelf.Width = ShelfSize - (ShelfThick * 2);
                    shelf.Height = ShelfSize - (ShelfThick * 2);

                    shelf.HorizontalAlignment = HorizontalAlignment.Stretch;
                    shelf.VerticalAlignment = VerticalAlignment.Stretch;

                    //X그려주는 DisableLine, DeadZone, 우클릭 이벤트 추가되어야함.
                    shelf.PreviewMouseLeftButtonDown += LayOutUnit_PreviewMouseLeftButtonDown;
                    shelf.PreviewMouseRightButtonDown += LayOutUnit_PreviewMouseRightButtonDown;

                    shelf.MouseEnter += ToolTipControl_MouseEnter;
                    shelf.MouseLeave += ToolTipControl_MouseLeave;
                    Grid.SetColumn(shelf, shelfBay);
                    Grid.SetRow(shelf, shelfLevel);
                    grid.Children.Add(shelf);

                    ShelfControlList.Add(strCurTagName, shelf);

                    //SuHwan_20230130 : 층수 개선
                    if (iCurFloor != 0)
                    {
                        if (CurDeadShelf == false)      //1베이가 데드존이여서 층을 설정하지 않으면 층수가 이상하게 표시되서 추가함.
                        {
                            //230106 HHJ SCS 개선
                            if (!shelfFloor.ContainsKey(iCurFloor))
                            {
                                List<int> listItem = new List<int>() { iCurLevel };
                                shelfFloor.Add(iCurFloor, listItem);
                            }
                            else
                            {
                                List<int> listItem = shelfFloor[iCurFloor];

                                if (!listItem.Contains(iCurLevel))
                                    listItem.Add(iCurLevel);
                            }
                        }
                    }
                }

                ShelfDividThick = 2;
                ShelfDividCount = 5;
                ShelfDividBrushes = Brushes.White;

                int iBayDividCnt = iMaxBay / ShelfDividCount;
                for (int i = 0; i <= iBayDividCnt; i++)
                {
                    //SuHwan_20230120 : 점선보더
                    var bd = new DashedBorderControl()
                    {
                        Style = Resources["DashedBorderStyle"] as Style,
                        BorderBrush = ShelfDividBrushes,
                        Background = Brushes.Transparent,
                        IsHitTestVisible = false,
                        StrokeDashArray = new DoubleCollection() { 3, 3 },
                    };

                    Grid.SetRowSpan(bd, iMaxLevel);
                    if ((i + 1) * ShelfDividCount > iMaxBay)
                    {
                        //마지막은 우측 보더 라인을 제외
                        bd.BorderThickness = new Thickness(ShelfDividThick, 0, 0, 0);
                        if (!(iMaxBay - (i * ShelfDividCount)).Equals(0))
                        {
                            Grid.SetColumnSpan(bd, iMaxBay - (i * ShelfDividCount));
                        }
                    }
                    else
                    {
                        //제일 첫번째는 좌측 보더라인을 제외
                        bd.BorderThickness = new Thickness(i.Equals(0) ? 0 : ShelfDividThick, 0, 0, 0);
                        Grid.SetColumnSpan(bd, ShelfDividCount);
                    }

                    Grid.SetColumn(bd, (i * ShelfDividCount) + 1);
                    Grid.SetRow(bd, 0);
                    grid.Children.Add(bd);
                }

                int iRowCount = 0;
                foreach (var v in shelfFloor.OrderBy(o => o.Key))
                {
                    List<int> listItem = v.Value;
                    int floorLow = listItem.OrderBy(o => o).FirstOrDefault();
                    int floorHigh = listItem.OrderBy(o => o).LastOrDefault();
                    int floorDiff = floorHigh - floorLow + 1;

                    //int level = bank.Equals(eShelfBank.Front) ? iMaxLevel - floorHigh : floorLow - 1;
                    int level = bank.Equals(GlobalData.Current.FrontBankNum) ? iMaxLevel - floorHigh : floorLow - 1;

                    //SuHwan_20230120 : 점선보더
                    var bd = new DashedBorderControl()
                    {
                        Style = Resources["DashedBorderStyle"] as Style,
                        BorderBrush = ShelfDividBrushes,
                        Background = Brushes.Transparent,
                        IsHitTestVisible = false,
                        StrokeDashArray = new DoubleCollection() { 3, 3 },
                    };

                    Grid.SetRowSpan(bd, floorDiff);

                    //if (bank == eShelfBank.Front)
                    if (bank == GlobalData.Current.FrontBankNum)
                    {
                        bd.BorderThickness = new Thickness(0, iRowCount.Equals(shelfFloor.Keys.Count - 1) ? 0 : ShelfDividThick,
                            0, 0);
                    }
                    else
                    {
                        bd.BorderThickness = new Thickness(0, 0,
                            0, iRowCount.Equals(shelfFloor.Keys.Count - 1) ? 0 : ShelfDividThick);
                    }

                    Grid.SetColumnSpan(bd, grid.ColumnDefinitions.Count - 2);

                    Grid.SetColumn(bd, 1);
                    Grid.SetRow(bd, level);
                    grid.Children.Add(bd);

                    iRowCount++;

                    //FloorText추가
                    TextBlock tb = new TextBlock();
                    tb.FontSize = 23;

                    string sfloor = string.Empty;
                    {
                        sfloor = (v.Key == 1) ? "G" : (v.Key - 1).ToString();
                    }

                    tb.Text = string.Format("{0}F", sfloor);

                    tb.HorizontalAlignment = HorizontalAlignment.Left;
                    tb.VerticalAlignment = VerticalAlignment.Center;
                    tb.TextAlignment = TextAlignment.Left;
                    tb.Margin = new Thickness(10, 0, 10, 0);
                    Grid.SetRowSpan(tb, floorDiff);
                    Grid.SetColumn(tb, 0);
                    Grid.SetRow(tb, level);

                    //230314 HHJ SCS 개선
                    RotateTransform rtTB = new RotateTransform();
                    Binding rtbd = new Binding();
                    rtbd.Source = vm;
                    rtbd.Path = new PropertyPath("LayOutTextDegree");
                    rtbd.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                    BindingOperations.SetBinding(rtTB, RotateTransform.AngleProperty, rtbd);
                    tb.RenderTransformOrigin = new Point(0.5, 0.5);
                    tb.RenderTransform = rtTB;

                    //if (bank == eShelfBank.Front)
                    if (bank == GlobalData.Current.FrontBankNum)
                        gridFrontLevel.Children.Add(tb);
                    else
                        gridRearLevel.Children.Add(tb);

                    //FloorBorder 추가
                    Border floorBd = new Border();
                    floorBd.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#FFBFBFBF"); ;
                    floorBd.Opacity = 0.2;
                    floorBd.Margin = new Thickness(0, 2, 0, 2);
                    floorBd.BorderBrush = Brushes.Gray;
                    floorBd.BorderThickness = new Thickness(0);
                    Grid.SetRowSpan(floorBd, floorDiff);
                    Grid.SetColumnSpan(floorBd, 2);
                    Grid.SetColumn(floorBd, 0);
                    Grid.SetRow(floorBd, level);
                    //if (bank == eShelfBank.Front)
                    if (bank == GlobalData.Current.FrontBankNum)
                        gridFrontLevel.Children.Add(floorBd);
                    else
                        gridRearLevel.Children.Add(floorBd);
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
            }
        }
        #endregion

        #endregion

        #region Data Updater
        /// <summary>
        /// Crane 실시간 위치, ActiveJob Path 생성
        /// </summary>
        private void RmPostionUpate()
        {
            try
            {
                List<object> rmList = new List<object>();
                
                //if (!PlayBackControl)
                {
                    Dictionary<string, RMModuleBase> ModuleList = GlobalData.Current.mRMManager.ModuleList;

                    foreach (RMModuleBase module in ModuleList.Values)
                    {
                        rmList.Add(module);
                    }
                }

                foreach (var item in rmList)
                {
                    int rmNum;
                    CraneCommand curCmd;
                    string curTag, curModuleName;
                    eCraneUIState curUIState;
                    decimal curForkAxis;

                    //if (!PlayBackControl)
                    {
                        RMModuleBase dmy = item as RMModuleBase;
                        rmNum = dmy.RMNumber;
                        curCmd = dmy.GetCurrentCmd();
                        curTag = dmy.CurrentTag;
                        curUIState = dmy.CraneState;
                        curForkAxis = dmy.ForkAxisPosition;
                        curModuleName = dmy.ModuleName;
                    }

                    if (rmNum == 1)
                    {
                        //SuHwan_20230201 : 크레인 이동 애니로 변경
                        Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                        {
                            CanvasRM1.CraneState = curUIState;

                            RM1PathGroup.Children.Clear();

                            var currentPosition = Canvas.GetLeft(CanvasRM1);
                            //SuHwan_20230809 : 크래인 이동 애니를 dest -> curTag 로 변경
                            //var destPosition = CalcRMPositionByTag(currentPosition, 0, curCmd, curTag);
                            var destPosition = CalcRMPositionByTag(currentPosition, 0, null, curTag);

                            if (_oldRM1Position != destPosition)
                            {
                                if (currentPosition != destPosition && Math.Abs(currentPosition - destPosition) > 10)
                                {
                                    _oldRM1Position = destPosition;

                                    var slideAnim = new DoubleAnimation
                                    {
                                        To = destPosition,
                                        Duration = TimeSpan.FromSeconds(1)
                                    };
                                    _storyboardRM1Position.Stop();
                                    _storyboardRM1Position.Children.Clear();

                                    Storyboard.SetTarget(slideAnim, CanvasRM1);
                                    Storyboard.SetTargetProperty(slideAnim, new PropertyPath("(Canvas.Left)"));
                                    _storyboardRM1Position.Children.Add(slideAnim);
                                    _storyboardRM1Position.Begin();
                                }
                            }
                            else
                            {
                                if (CanvasRM1.ForkAxisPosition != curForkAxis)
                                    CanvasRM1.ForkAxisPosition = curForkAxis;
                            }
                        }));
                    }
                    if (GlobalData.Current.SCSType == eSCSType.Dual)
                    {
                        if (rmNum == 2)
                        {
                            //SuHwan_20230201 : 크레인 이동 애니로 변경
                            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                            {
                                CanvasRM2.CraneState = curUIState;

                                RM2PathGroup.Children.Clear();

                                var currentPosition = Canvas.GetLeft(CanvasRM2);
                                var destPosition = CalcRMPositionByTag(currentPosition, 0, curCmd, curTag);

                                if (_oldRM2Position != destPosition)
                                {
                                    if (currentPosition != destPosition && Math.Abs(currentPosition - destPosition) > 10)
                                    {
                                        _oldRM2Position = destPosition;

                                        var slideAnim = new DoubleAnimation
                                        {
                                            To = destPosition,
                                            Duration = TimeSpan.FromSeconds(1)
                                        };

                                        _storyboardRM2Position.Stop();
                                        _storyboardRM2Position.Children.Clear();

                                        Storyboard.SetTarget(slideAnim, CanvasRM2);
                                        Storyboard.SetTargetProperty(slideAnim, new PropertyPath("(Canvas.Left)"));
                                        _storyboardRM2Position.Children.Add(slideAnim);
                                        _storyboardRM2Position.Begin();
                                    }
                                }
                                else
                                {
                                    if (CanvasRM2.ForkAxisPosition != curForkAxis)
                                        CanvasRM2.ForkAxisPosition = curForkAxis;
                                }
                            }));
                        }
                    }
                    ActiveJobCollectionChanged(curModuleName, curCmd);
                }
            }
            catch (Exception)
            {

            }
        }

        private void ConveyorUpdate()
        {
            try
            {
                int cvcount = 0;
                foreach (var lineitem in GlobalData.Current.PortManager.ModuleList.Values)
                {
                    foreach (var CVitem in lineitem.ModuleList)
                    {
                        if (oldCvPortInOut[cvcount] != CVitem.PLC_PortType)
                        {
                            oldCvPortInOut[cvcount] = CVitem.PLC_PortType;
                            CVitem.SetDirection(oldCvPortInOut[cvcount]);
                        }
                        cvcount++;
                    }
                }
            }
            catch (Exception)
            {

            }
        }

        /// <summary>
        /// Crane Current Tag 기준 UI Position Get
        /// </summary>
        /// <param name="curCanvasPosition"></param>
        /// <param name="rmPosition"></param>
        /// <param name="DestCmd"></param>
        /// <param name="CurTag"></param>
        /// <returns></returns>
        private double CalcRMPositionByTag(double curCanvasPosition, double rmPosition, CraneCommand DestCmd, string CurTag)
        {
            double ret = 0;
            //220509 HHJ SCS 개선     //- ShelfControl 변경
            //Border debd;
            FrameworkElement debd;
            Point depi = new Point();

            if (DestCmd != null)
            {
                double destPosition = 0;
                string DestTag = ShelfTagHelper.GetTag(DestCmd.TargetBank, DestCmd.TargetBay, DestCmd.TargetLevel);

                //220509 HHJ SCS 개선     //- ShelfControl 변경
                //if (ShelfBdList.ContainsKey(DestTag))
                if (ShelfControlList.ContainsKey(DestTag))
                {
                    //220509 HHJ SCS 개선     //- ShelfControl 변경
                    //debd = ShelfBdList[DestTag];
                    debd = ShelfControlList[DestTag];
                    depi = debd.TransformToAncestor(grdMain).Transform(new Point(0, 0));

                    //220603 HHJ SCS 개선     //- 0번지와 끝번지에 C/V가 있는경우 포지션을 찾지못하는 현상 개선
                    if (DestCmd.TargetBay <= 0)
                    {
                        //destPosition = depi.X - ((ShelfSize - 5) / 2);
                        destPosition = vm.MinBayCvWidth - ((ShelfSize - 5) / 2);
                    }
                    else if (DestCmd.TargetBay > GlobalData.Current.ShelfMgr.FrontData.MaxBay)
                        destPosition = depi.X + ((ShelfSize - 5) / 2);
                    else
                        destPosition = Math.Round(depi.X) + (Math.Round(debd.ActualWidth) / 2);
                    destPosition = destPosition - (Math.Round(CanvasRM1.ActualWidth) / 2);
                }

                //목적지 = 현위치 : 목적지와 현위치가 같은경우 -> 목적지에 도착
                //RM의 중심을 작업하는 쉘프의 중심과 맞춰준다.
                if (DestTag.Equals(CurTag))
                {
                    ret = destPosition;
                }
                //아닌 경우 위치 이동 진행한다.
                else
                {
                    //RmPosition을 받지못하면 현위치 기준으로 특정 기준만큼 shift 시켜준다.
                    if (rmPosition.Equals(0))
                    {
                        int destBay = ShelfTagHelper.GetBay(DestTag);
                        int curBay = ShelfTagHelper.GetBay(CurTag);

                        if (destBay > curBay)
                        {
                            ret = curCanvasPosition + (CanvasRM1.ActualWidth / 2);

                            if (destPosition <= ret)
                                ret = destPosition;
                        }
                        else
                        {
                            ret = curCanvasPosition - (CanvasRM1.ActualWidth / 2);

                            if (destPosition >= ret)
                                ret = destPosition;
                        }
                    }
                    //RMPosition이 있다면 해당 Position으로 계산해서 이동시킨다.
                    else
                    {

                    }
                }
            }
            else
            {
                //220509 HHJ SCS 개선     //- ShelfControl 변경
                //if (ShelfBdList.ContainsKey(CurTag))
                if (ShelfControlList.ContainsKey(CurTag))
                {
                    //220509 HHJ SCS 개선     //- ShelfControl 변경
                    //debd = ShelfBdList[CurTag];
                    debd = ShelfControlList[CurTag];
                    depi = debd.TransformToAncestor(grdMain).Transform(new Point(0, 0));

                    //220603 HHJ SCS 개선     //- 0번지와 끝번지에 C/V가 있는경우 포지션을 찾지못하는 현상 개선
                    int bay = ShelfTagHelper.GetBay(CurTag);
                    if (bay <= 0)
                    {
                        //ret = depi.X - ((ShelfSize - 5) / 2);
                        ret = vm.MinBayCvWidth - ((ShelfSize - 5) / 2);
                    }
                    else if (bay > GlobalData.Current.ShelfMgr.FrontData.MaxBay)
                        ret = depi.X + ((ShelfSize - 5) / 2);
                    else
                        ret = Math.Round(depi.X) + (Math.Round(debd.ActualWidth) / 2);
                    ret = ret - (Math.Round(CanvasRM1.ActualWidth) / 2);
                }
            }

            return ret;
        }
        /// <summary>
        /// Active Job Path 생성
        /// </summary>
        /// <param name="name"></param>
        /// <param name="ccmd"></param>
        private void ActiveJobCollectionChanged(string name, CraneCommand ccmd = null)
        {
            if (ccmd != null || GlobalData.Current.ServerClientType == eServerClientType.Client)
            {
                FrameworkElement debd;

                Point depi = new Point(), rmpi = new Point();

                int TargetBay;
                string tag;
                //SuHwan_20221031 : [ServerClient]
                if (GlobalData.Current.ServerClientType == eServerClientType.Client)
                {
                    //if (!PlayBackControl)
                    {
                        var RMBuffer = GlobalData.Current.mRMManager.ModuleList[name];
                        if (RMBuffer.PLC_CraneJobState != eCraneJobState.Busy)
                            return;

                        TargetBay = RMBuffer.PLC_DestBay_FORK1;
                        tag = ShelfTagHelper.GetTag(RMBuffer.PLC_DestBank_FORK1, TargetBay, RMBuffer.PLC_DestLevel_FORK1);
                    }
                }
                else
                {
                    TargetBay = ccmd.TargetBay;
                    tag = ShelfTagHelper.GetTag(ccmd.TargetBank, TargetBay, ccmd.TargetLevel);

                }

                if (!ShelfControlList.ContainsKey(tag))
                    return;

                debd = ShelfControlList[tag];

                //220413 HHJ SCS 개선     //- UI 실행 초기부하 감소
                //if (de != null)
                if (debd != null)
                {
                    //220321 HHJ SCS 개발     //- ShelfData UI 연동
                    //deShelfItem = de.GetShelfClass().ShelfItem;

                    //출발, 도착지포인트 계산
                    //1번 뱅크(프론트)인 경우는 가져온 포지션에서 높이만큼 더해주고 위드의 절반을 더해준다.
                    //2번 뱅크(리어)인 경우는 가져온 포지션에서 위드의 절반만 더해준다.
                    //도착지, RM의 현 위치를 가져온다. - 우선 소수점은 반올림 처리한다.

                    //220603 HHJ SCS 개선     //- 0번지와 끝번지에 C/V가 있는경우 포지션을 찾지못하는 현상 개선
                    if (TargetBay <= 0)
                    {
                        depi = debd.TransformToAncestor(grdMain).Transform(new Point(0, 0));
                        depi.X = vm.MinBayCvWidth - ((ShelfSize - 5) / 2);
                        //depi.Y = ShelfTagHelper.GetBank(tag).Equals((int)eShelfBank.Front) ? Math.Round(depi.Y) + Math.Round(debd.ActualHeight) : Math.Round(depi.Y);
                        depi.Y = ShelfTagHelper.GetBank(tag).Equals(GlobalData.Current.FrontBankNum) ? Math.Round(depi.Y) + Math.Round(debd.ActualHeight) : Math.Round(depi.Y);
                    }
                    else if (TargetBay > GlobalData.Current.ShelfMgr.FrontData.MaxBay)
                    {
                        depi = debd.TransformToAncestor(grdMain).Transform(new Point(0, 0));
                        depi.X = depi.X + ((ShelfSize - 5) / 2);
                        //depi.Y = ShelfTagHelper.GetBank(tag).Equals((int)eShelfBank.Front) ? Math.Round(depi.Y) + Math.Round(debd.ActualHeight) : Math.Round(depi.Y);
                        depi.Y = ShelfTagHelper.GetBank(tag).Equals(GlobalData.Current.FrontBankNum) ? Math.Round(depi.Y) + Math.Round(debd.ActualHeight) : Math.Round(depi.Y);
                    }
                    else
                    {
                        depi = debd.TransformToAncestor(grdMain).Transform(new Point(0, 0));
                        depi.X = Math.Round(depi.X) + (Math.Round(debd.ActualWidth) / 2) + 3;
                        //depi.Y = ShelfTagHelper.GetBank(tag).Equals((int)eShelfBank.Front) ? Math.Round(depi.Y) + Math.Round(debd.ActualHeight) : Math.Round(depi.Y);
                        depi.Y = ShelfTagHelper.GetBank(tag).Equals(GlobalData.Current.FrontBankNum) ? Math.Round(depi.Y) + Math.Round(debd.ActualHeight) : Math.Round(depi.Y);
                    }
                    if (GlobalData.Current.mRMManager.FirstRM.ModuleName == name)
                    {
                        //RM의 경우는 중간 지점을 위해서 포인트를 가져와야한다.
                        //X는 가져온 포지션에서 위드를 더해준다.
                        //Y는 현 포지션 + Math.Round(rmpi.Y) 높이의 절반
                        rmpi = CanvasRM1.TransformToAncestor(grdMain).Transform(new Point(0, 0));
                        rmpi.X = Math.Round(rmpi.X) + (Math.Round(CanvasRM1.ActualWidth) / 2);
                        rmpi.Y = Math.Round(rmpi.Y) + (Math.Round(CanvasRM1.ActualHeight) / 2) + 3;
                        RM1PathGroup.Children.Clear();

                        RM1PathGroup.Children.Add(new LineGeometry(rmpi, new Point(depi.X, rmpi.Y)));
                        RM1PathGroup.Children.Add(new LineGeometry(new Point(depi.X, rmpi.Y), depi));
                        RM1Path.Stroke = Resources["Rm1Busy"] as SolidColorBrush;
                        RM1Path.StrokeThickness = 3;
                        RM1Path.SetValue(RenderOptions.EdgeModeProperty, EdgeMode.Aliased);
                        RM1Path.Data = RM1PathGroup;
                        RM1Path.StrokeDashArray = DoubleCollection.Parse("3, 1");
                        //220322 HHJ SCS 개발     //- ActiveJobList 한줄만 보이게 변경
                        SafeObservableCollection<CraneCommand> CraneActiveJob = new SafeObservableCollection<CraneCommand>()
                        {
                            ccmd
                        };
                    }
                    else
                    {
                        //RM의 경우는 중간 지점을 위해서 포인트를 가져와야한다.
                        //X는 가져온 포지션에서 위드를 더해준다.
                        //Y는 현 포지션 + Math.Round(rmpi.Y) 높이의 절반
                        rmpi = CanvasRM2.TransformToAncestor(grdMain).Transform(new Point(0, 0));
                        rmpi.X = Math.Round(rmpi.X) + (Math.Round(CanvasRM1.ActualWidth) / 2);
                        rmpi.Y = Math.Round(rmpi.Y) + (Math.Round(CanvasRM2.ActualHeight) / 2) + 9;
                        RM2PathGroup.Children.Clear();

                        RM2PathGroup.Children.Add(new LineGeometry(rmpi, new Point(depi.X, rmpi.Y)));
                        RM2PathGroup.Children.Add(new LineGeometry(new Point(depi.X, rmpi.Y), depi));
                        RM2Path.Stroke = Resources["Rm2Busy"] as SolidColorBrush;
                        RM2Path.StrokeThickness = 3;
                        RM2Path.SetValue(RenderOptions.EdgeModeProperty, EdgeMode.Aliased);
                        RM2Path.Data = RM2PathGroup;
                        RM2Path.StrokeDashArray = DoubleCollection.Parse("3, 1");
                        //220322 HHJ SCS 개발     //- ActiveJobList 한줄만 보이게 변경
                        SafeObservableCollection<CraneCommand> CraneActiveJob = new SafeObservableCollection<CraneCommand>()
                        {
                            ccmd
                        };
                    }
                }
            }
            else
            {

            }
        }
        #endregion

        #region Etc
        /// <summary>
        /// 페이지 리로드
        /// </summary>
        public void page_reLoaded()
        {
            GlobalData.Current.ParameterSet(GlobalData.Current.nParameterList);
            LoadComp = false;
            bReload = true;
            Control_Loaded(null, null);
        }
        /// <summary>
        /// Crane Path 초기화, Single Crane 변경시 Crane2 Hidden 처리
        /// </summary>
        public void CanvasRMVisibility()
        {
            RM1PathGroup.Children.Clear();
            RM1Path.Data = RM1PathGroup;
            RM2PathGroup.Children.Clear();
            RM2Path.Data = RM2PathGroup;

            CanvasRM2.Visibility = (GlobalData.Current.SCSType == eSCSType.Single) ? Visibility.Hidden : Visibility.Visible;
        }
        //230314 HHJ SCS 개선
        public void RotateLayOut()
        {
            TransformGroup vbtg = ViewBoxtransGroup;
            TransformGroup bank1tg = Bank1transGroup;
            TransformGroup bank2tg = Bank2transGroup;

            RotateTransform vbrt = vbtg.Children.OfType<RotateTransform>().First();
            RotateTransform bank1rt = bank1tg.Children.OfType<RotateTransform>().First();
            RotateTransform bank2rt = bank2tg.Children.OfType<RotateTransform>().First();

            double ChangeAngle = vbrt.Angle;

            ChangeAngle += 90;

            if (ChangeAngle >= 360)
                ChangeAngle = 0;

            vbrt.Angle = ChangeAngle;

            foreach (FrameworkElement element in ShelfControlList.Values)
            {
                vm.LayOutTextDegree = -ChangeAngle;
                bank1rt.Angle = -ChangeAngle;
                bank2rt.Angle = -ChangeAngle;

                if (element is Grid grd)
                {
                    foreach (UIControlCV cv in grd.Children)
                    {
                        SetLayOutAngle(cv, ChangeAngle);
                    }
                }
                else if (element is UIControlShelf shelf)
                {
                    SetLayOutAngle(shelf, ChangeAngle);
                }
            }

            SetLayOutAngle(CanvasRM1, ChangeAngle);
            SetLayOutAngle(CanvasRM2, ChangeAngle);

            //230911 HHJ 회전시 배율 자동 조정되도록 수정 Start
            if (ChangeAngle.Equals(90) || ChangeAngle.Equals(270))
            {
                decimal rotateScale = (decimal)(this.ActualHeight < this.ActualWidth ? this.ActualHeight / this.ActualWidth : 1);
                if (rotateScale < 1)
                {
                    rotateScale = Math.Floor(rotateScale * 10) / 10;
                    vm.ScaleValue = rotateScale;
                }
            }
            else
            {
                vm.ScaleValue = vm.GetScaleDefaultValue();
            }
            //230911 HHJ 회전시 배율 자동 조정되도록 수정 End
        }
        private eLayOutAngle GetLayOutAngle(double angle)
        {
            if (angle.Equals(90))
            {
                return eLayOutAngle.eAngle90;
            }
            else if (angle.Equals(180))
            {
                return eLayOutAngle.eAngle180;
            }
            else if (angle.Equals(270))
            {
                return eLayOutAngle.eAngle270;
            }
            else
            {
                return eLayOutAngle.eAngle0;
            }
        }

        /// <summary>
        /// 20230602 JIG LayOutViewBox를 드래그하기 위해 마우스 왼쪽버튼 눌렀을 때 상태변경,마우스 위치받아오기
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LayOutViewBox_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            mouseDownTime = DateTime.Now;
            //마우스 다운시 현재시각 저장
            clickPosition = e.GetPosition(this);
            // 초기 클릭 위치 가져오기
            LayOutViewBox.CaptureMouse();
        }

        /// <summary>
        /// 20230602 JIG LayOutViewBox를 드래그하기 위해 마우스 왼쪽버튼을 떼었을 때 상태변경,마우스 위치받아오기
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LayOutViewBox_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            LayOutViewBox.ReleaseMouseCapture();
        }

        /// <summary>
        ///  20230602 JIG LayOutViewBox를 드래그하기 위해 마우스 위치 설정
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LayOutViewBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (LayOutViewBox.IsMouseCaptured && (DateTime.Now - mouseDownTime) > TimeSpan.FromSeconds(0.3))
            // 0.3초가 지날시에만 마우스 이동가능
            {
                Point currentPosition = e.GetPosition(this);
                // 초기위치에서의 이동벡터 계산
                Vector delta = currentPosition - clickPosition;
                //  Viewbox의 위치를 마우스 움직임과 동일하게 이동
                vm.Margins = new Thickness(vm.Margins.Left + delta.X, vm.Margins.Top + delta.Y, 0, 0);
                clickPosition = currentPosition;
                this.Cursor = Cursors.SizeAll;
            }
        }

        private void Shelf_MouseEnter(object sender, MouseEventArgs e)
        {
            this.Cursor = Cursors.Hand;
        }

        private void Shelf_MouseLeave(object sender, MouseEventArgs e)
        {
            this.Cursor = Cursors.Arrow;
        }

        private void SetLayOutAngle(UIControlBase control, double angle)
        {
            control.LayOutAngle = GetLayOutAngle(angle);
        }
        #endregion
        #endregion
    }
}