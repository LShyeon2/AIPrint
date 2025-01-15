using BoxPrint.GUI.UIControls;
using BoxPrint.Log;
using BoxPrint.Modules;
using BoxPrint.Modules.Conveyor;
using BoxPrint.Modules.Shelf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace BoxPrint.GUI.UserControls
{
    /// <summary>
    /// Interaction logic for ShelfSetterPage.xaml
    /// </summary>
    public partial class ShelfSetterPage : Window, INotifyPropertyChanged
    {
        private Dictionary<string, Border> ShelfBdList;
        //220525 HHJ SCS 개선     //- 쉘프 생성시 내부 리스트에 업데이트 하지않고 글로벌 리스트에 업데이트
        //private Dictionary<string, ShelfItem> ShelfList;

        private bool bAddRatioShelfSize = false;
        private double ShelfSize = 0;
        private double ShelfThick = 1;  //1고정

        private EnumToBrushConverter etb = new EnumToBrushConverter();

        private bool bSuccess = false;
        private LoadingSpinner loadSpinner = null;

        public event PropertyChangedEventHandler PropertyChanged;
        private double _VerticalNumberWidth;
        public double VerticalNumberWidth
        {
            get { return _VerticalNumberWidth; }
            set
            {
                _VerticalNumberWidth = value;
                RaisePropertyChanged("VerticalNumberWidth");

            }
        }

        private string _FrontBank;
        public string FrontBank
        {
            get => _FrontBank;
            set
            {
                _FrontBank = value;
                RaisePropertyChanged("FrontBank");
            }
        }
        
        private string _RearBank;
        public string RearBank
        {
            get => _RearBank;
            set
            {
                _RearBank = value;
                RaisePropertyChanged("RearBank");
            }
        }

        private double _MaxCvWidth = 0;
        protected void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ShelfSetterPage()
        {
            InitializeComponent();
            DataContext = this;

            ShelfBdList = new Dictionary<string, Border>();
            //220525 HHJ SCS 개선     //- 쉘프 생성시 내부 리스트에 업데이트 하지않고 글로벌 리스트에 업데이트
            //ShelfList = new Dictionary<string, ShelfItem>();

            selCanvas = new Canvas();
            Canvas.SetZIndex(selCanvas, 9999);
            selReatangle = new Rectangle()
            {
                Fill = Brushes.LightBlue,
                Stroke = Brushes.Blue,
                StrokeThickness = 2,
                RadiusX = 5,
                Opacity = 0.5,
            };
            selCanvas.Children.Add(selReatangle);

            //cbbType.Items.Clear();
            cbbType.ItemsSource = null;
            cbbType.ItemsSource = Enum.GetValues(typeof(eShelfType));

            cbbEnable.Items.Clear();
            cbbEnable.Items.Add(true);
            cbbEnable.Items.Add(false);

            cbbDeadZone.Items.Clear();
            cbbDeadZone.Items.Add(true);
            cbbDeadZone.Items.Add(false);

            cbbZoneName.Items.Clear();
            for (int i = 0; i < 10; i++)
            {
                cbbZoneName.Items.Add(i + 1);
            }

            cbbFloor.Items.Clear();
            for (int j = 0; j < 3; j++)
            {
                cbbFloor.Items.Add(j + 1);
            }

            FrontBank = "Fronat Bank" + string.Format("({0})", GlobalData.Current.FrontBankNum);
            RearBank = "Rear Bank" + string.Format("({0})", GlobalData.Current.RearBankNum);
        }

        private void SetShelfSize()
        {
            try
            {
                //Front, Rear의 사이즈 및 Max Row, Col 갯수는 동일하다고 가정하고 진행한다.
                //동일하지 않은경우 Front, Rear 개별 체크가 필요함.
                double tmpShelfSize, tmpGridSize;
                bAddRatioShelfSize = false;
                _MaxCvWidth = 0;

                tmpShelfSize = Math.Truncate(GridFrontShelf.ActualHeight / GlobalData.Current.SystemParameter.FrontYcount);
                tmpGridSize = tmpShelfSize * GlobalData.Current.SystemParameter.FrontXcount;

                if (tmpGridSize > bdMain.ActualWidth)
                {
                    tmpShelfSize = Math.Truncate(bdMain.ActualWidth / GlobalData.Current.SystemParameter.FrontXcount);
                    tmpGridSize = tmpShelfSize * GlobalData.Current.SystemParameter.FrontYcount;

                    //이만큼으로 사이즈 리사이즈해주고 옵셋을 줘야하나? 이건 고민해봐야할듯? 아니면 길게 직사각형으로 내줘야하나?
                }
                //220418 HHJ SCS 개선     //- 쉘프 가로사이즈 변경
                else if ((tmpGridSize * 2) < bdMain.ActualWidth)
                {
                    bAddRatioShelfSize = true;
                }

                ShelfSize = tmpShelfSize;
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
            }
        }
        private void GridInit(Grid grd)
        {
            grd.Children.Clear();

            grd.HorizontalAlignment = HorizontalAlignment.Stretch;
            grd.VerticalAlignment = VerticalAlignment.Stretch;

            grd.ColumnDefinitions.Clear();
            grd.RowDefinitions.Clear();
        }
        private void GridInit(int X, int Y)
        {
            Binding bd = new Binding();
            bd.Path = new PropertyPath("VerticalNumberWidth");

            #region FrontShelf
            GridInit(GridFrontShelf);
            for (int i = 0; i < X; i++)
            {
                ColumnDefinition colDef1 = new ColumnDefinition();

                if (i.Equals(0))
                {
                    colDef1.SetBinding(ColumnDefinition.WidthProperty, bd);
                }

                if (i.Equals(X - 1))
                {
                    colDef1.SetBinding(ColumnDefinition.WidthProperty, bd);
                }

                GridFrontShelf.ColumnDefinitions.Add(colDef1);
            }
            //Define the Rows 세로
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
                ColumnDefinition colDef1 = new ColumnDefinition();

                if (i.Equals(0))
                {
                    colDef1.SetBinding(ColumnDefinition.WidthProperty, bd);
                }

                if (i.Equals(X - 1))
                {
                    colDef1.SetBinding(ColumnDefinition.WidthProperty, bd);
                }

                GridRearShelf.ColumnDefinitions.Add(colDef1);
            }
            //Define the Rows 세로
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
                ColumnDefinition colDef1 = new ColumnDefinition();

                if (i.Equals(0))
                {
                    colDef1.SetBinding(ColumnDefinition.WidthProperty, bd);
                }

                if (i.Equals(X - 1))
                {
                    colDef1.SetBinding(ColumnDefinition.WidthProperty, bd);
                }

                GridFrontCV.ColumnDefinitions.Add(colDef1);
            }
            //Define the Rows 세로
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
                ColumnDefinition colDef1 = new ColumnDefinition();

                if (i.Equals(0))
                {
                    colDef1.SetBinding(ColumnDefinition.WidthProperty, bd);
                }

                if (i.Equals(X - 1))
                {
                    colDef1.SetBinding(ColumnDefinition.WidthProperty, bd);
                }

                GridRearCV.ColumnDefinitions.Add(colDef1);
            }
            //Define the Rows 세로
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
                ColumnDefinition colDef1 = new ColumnDefinition();

                if (i.Equals(0))
                {
                    colDef1.SetBinding(ColumnDefinition.WidthProperty, bd);
                }

                if (i.Equals(X - 1))
                {
                    colDef1.SetBinding(ColumnDefinition.WidthProperty, bd);
                }

                GridFrontXArray.ColumnDefinitions.Add(colDef1);
            }
            for (int i = 1; i < X - 1; i++)
            {
                TextBlock tb = new TextBlock();
                tb.Text = i.ToString();
                tb.HorizontalAlignment = HorizontalAlignment.Center;
                tb.VerticalAlignment = VerticalAlignment.Center;

                tb.TextAlignment = TextAlignment.Center;
                Grid.SetColumn(tb, i);
                Grid.SetRow(tb, 0);
                GridFrontXArray.Children.Add(tb);
            }
            #endregion
            #region RearBay
            GridInit(GridRearXArray);
            for (int i = 0; i < X; i++)
            {
                ColumnDefinition colDef1 = new ColumnDefinition();

                if (i.Equals(0))
                {
                    colDef1.SetBinding(ColumnDefinition.WidthProperty, bd);
                }

                if (i.Equals(X - 1))
                {
                    colDef1.SetBinding(ColumnDefinition.WidthProperty, bd);
                }

                GridRearXArray.ColumnDefinitions.Add(colDef1);
            }
            for (int i = 1; i < X - 1; i++)
            {
                TextBlock tb = new TextBlock();
                tb.Text = i.ToString();
                tb.HorizontalAlignment = HorizontalAlignment.Center;
                tb.VerticalAlignment = VerticalAlignment.Center;

                tb.TextAlignment = TextAlignment.Center;
                Grid.SetColumn(tb, i);
                Grid.SetRow(tb, 0);
                GridRearXArray.Children.Add(tb);
            }
            #endregion
            #region FrontLevel
            GridInit(gridFrontLevel);
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
                tb.Margin = new Thickness(0, 0, 20, 0);

                Grid.SetColumn(tb, 0);
                Grid.SetRow(tb, i);

                //220426 HHJ SCS 개선     //- 세로 숫자 표현 방식 변경
                //grid.Children.Add(tb);
                gridFrontLevel.Children.Add(tb);
            }
            #endregion
            #region RearLevel
            GridInit(gridRearLevel);
            for (int i = 0; i < Y; i++)
            {
                RowDefinition rowDef1 = new RowDefinition();
                gridRearLevel.RowDefinitions.Add(rowDef1);
            }
            for (int i = 0; i < Y; i++)
            {
                TextBlock tb = new TextBlock();
                tb.Text = (Y - i).ToString();
                tb.HorizontalAlignment = HorizontalAlignment.Right;
                tb.VerticalAlignment = VerticalAlignment.Center;
                tb.TextAlignment = TextAlignment.Right;
                tb.Margin = new Thickness(0, 0, 20, 0);

                Grid.SetColumn(tb, 0);
                Grid.SetRow(tb, i);

                //220426 HHJ SCS 개선     //- 세로 숫자 표현 방식 변경
                //grid.Children.Add(tb);
                gridRearLevel.Children.Add(tb);
            }
            #endregion
        }

        #region Layout 설정관리 함수들
        Canvas selCanvas;
        Rectangle selReatangle;

        private void SetGrid_Click(object sender, RoutedEventArgs e)
        {
            ShelfBdList.Clear();
            //220525 HHJ SCS 개선     //- 쉘프 생성시 내부 리스트에 업데이트 하지않고 글로벌 리스트에 업데이트
            //ShelfList.Clear();
            GlobalData.Current.ShelfMgr.FrontData.Clear();
            GlobalData.Current.ShelfMgr.RearData.Clear();

            if (int.TryParse(txtGrdX.Text, out int x) && int.TryParse(txtGrdY.Text, out int y))
            {
                GlobalData.Current.SystemParameter.RearXcount = x;
                GlobalData.Current.SystemParameter.RearYcount = y;

                GlobalData.Current.SystemParameter.FrontXcount = x;
                GlobalData.Current.SystemParameter.FrontYcount = y;
                SetShelfSize();
                InitControl();
            }
        }

        private void ToolTipControl_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Border shelf)
            {
                shelf.BorderThickness = new Thickness(2);
                shelf.ToolTip = null;
            }
            else if (sender is CVUserControl cv)
            {
                cv.ToolTip = null;
            }
        }
        private void ToolTipControl_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Border shelf)
            {
                shelf.BorderThickness = new Thickness(4);
                //220525 HHJ SCS 개선     //- 쉘프 생성시 내부 리스트에 업데이트 하지않고 글로벌 리스트에 업데이트
                //if (ShelfList.ContainsKey(shelf.Tag.ToString()))
                //{
                //    ShelfItem item = ShelfList[shelf.Tag.ToString()];
                //    ucCustomToolTip ucTooltip = new ucCustomToolTip(item);
                //    ToolTip tTip = new ToolTip();
                //    tTip.SetResourceReference(Control.StyleProperty, "CustomToolTip");
                //    tTip.Content = ucTooltip;

                //    shelf.ToolTip = tTip;
                //}
                if (GlobalData.Current.ShelfMgr.GetShelf(shelf.Tag.ToString()) is ShelfItem item)
                {
                    ucCustomToolTip ucTooltip = new ucCustomToolTip(item);
                    ToolTip tTip = new ToolTip();
                    tTip.SetResourceReference(Control.StyleProperty, "CustomToolTip");
                    tTip.Content = ucTooltip;
                    shelf.ToolTip = tTip;
                }
            }
            ////220519 HHJ SCS 개선     //- CVUserControl ToolTip 추가
            else if (sender is CVUserControl cv)
            {
                CV_BaseModule cbase = cv.GetCVModule();
                ucCustomToolTip ucTooltip = new ucCustomToolTip(cbase);
                ToolTip tTip = new ToolTip();
                tTip.SetResourceReference(Control.StyleProperty, "CustomToolTip");
                tTip.Content = ucTooltip;

                cv.ToolTip = tTip;
            }
        }

        private void InitControl()
        {
            #region Control set 
            GridInit(GlobalData.Current.SystemParameter.RearXcount + 2, GlobalData.Current.SystemParameter.RearYcount);

            GridTeachingInit(GridRearShelf);
            GridTeachingInit(GridFrontShelf);

            //GridArrayDisplayInfo(GridFrontCV, eShelfBank.Front);
            //GridArrayDisplayInfo(GridRearCV, eShelfBank.Rear);
            GridArrayDisplayInfo(GridFrontCV, GlobalData.Current.FrontBankNum);
            GridArrayDisplayInfo(GridRearCV, GlobalData.Current.RearBankNum);
            #endregion

            VerticalNumberWidth = _MaxCvWidth;
        }
        //private void GridArrayDisplayInfo(Grid g, eShelfBank shelfbank)
        private void GridArrayDisplayInfo(Grid g, int shelfbank)
        {
            try
            {
                int iGrdRowCnt = g.RowDefinitions.Count;
                int iGrdColCnt = g.ColumnDefinitions.Count - 2;

                foreach (var lineItem in GlobalData.Current.PortManager.ModuleList)
                {
                    //생성하려는 뱅크와 추가하려는 뱅크가 같아야함.
                    if ((int)shelfbank != lineItem.Value.Position_Bank)
                        continue;

                    Canvas canvas = new Canvas();
                    Grid grid = ConverSet(lineItem.Value, lineItem.Value.Position_Bank, lineItem.Value.Position_Bay, lineItem.Value.Position_Level);

                    if (grid != null)
                    {
                        double degree = 0;

                        //모듈리스트가 1개 보다 큰 경우에만 위치 조정이 필요하다.
                        if (lineItem.Value.ModuleList.Count > 1)
                        {
                            double cvsize = grid.Height / lineItem.Value.ModuleList.Count;

                            //1보다 같거나 크고, 쉘프 최대수치보다 같거나 작다면 (쉘프가 그려지는 공간 내) -1한 값으로 계산
                            //그 외에 C/V가 구성이 된다면 그냥 값으로 계산.
                            if (lineItem.Value.Position_Bay >= 1 && lineItem.Value.Position_Bay <= iGrdColCnt)
                            {
                                if (_MaxCvWidth < cvsize * (lineItem.Value.ModuleList.Count - 1))
                                    _MaxCvWidth = cvsize * (lineItem.Value.ModuleList.Count - 1);

                                //220525 HHJ SCS 개선     //- CV 사이즈관련 개선
                                //bAddRatioShelfSize가 true면 쉘프가 직사각형이 되기에 추가 연산이 더 들어가야하고
                                //false라면 정사각형이기에 추가연산없이 CV사이즈만큼만 빼주면된다.
                                //Canvas.SetTop(grid,
                                //            cvsize * (lineItem.Value.ModuleList.Count - 1) - (cvsize / 2));
                                if (bAddRatioShelfSize)
                                    Canvas.SetTop(grid,
                                            cvsize * (lineItem.Value.ModuleList.Count - 1) - (cvsize / 2));
                                else
                                    Canvas.SetTop(grid,
                                            cvsize * (lineItem.Value.ModuleList.Count - 1));
                            }
                            else
                            {
                                if (_MaxCvWidth < cvsize * (lineItem.Value.ModuleList.Count + 1))
                                    _MaxCvWidth = cvsize * (lineItem.Value.ModuleList.Count + 1);
                            }

                            if (lineItem.Value.CVRotate)
                            {
                                ePortInOutType direction = ePortInOutType.Unknown;
                                foreach (CV_BaseModule cv in lineItem.Value.ModuleList)
                                {
                                    direction = cv.PortInOutType;
                                    break;
                                }

                                if (direction.Equals(ePortInOutType.OUTPUT))
                                {
                                    degree = 270;
                                }
                                else
                                {
                                    degree = 90;
                                }
                            }
                        }

                        TransformGroup tgroup = new TransformGroup();
                        RotateTransform rt = new RotateTransform(degree);

                        //TransformGroup.Children.Add(st);
                        tgroup.Children.Add(rt);

                        canvas.LayoutTransform = tgroup;    //캔버스 자체가 회전해야 1번이 Shelf에 정렬되게 된다.

                        canvas.Children.Add(grid);

                        canvas.Height = grid.Height;
                        canvas.Width = grid.Width;
                        //220411 HHJ SCS 개선     //- 쉘프 그리드 세로 숫자 추가
                        //Grid.SetColumn(canvas, lineItem.Value.Position_Bay - 1);
                        Grid.SetColumn(canvas, lineItem.Value.Position_Bay);
                        //UI의 Row Grid는 Grid 열배열과 반대이다. (1번줄이 마지막줄, 마지막줄이 1번줄)
                        //열의 최대값 - 설정 레벨이 그리드에 추가되어야할 실제 레벨이다.
                        Grid.SetRow(canvas, iGrdRowCnt - lineItem.Value.Position_Level);
                        g.Children.Add(canvas);
                    }
                    g.ClipToBounds = true;
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
            }

        }
        private Grid ConverSet(Modules.CVLine.CVLineModule lineItem, int bank, int bay, int level)
        {
            Grid grid = new Grid();
            int YLocation = 0;

            try
            {
                //Define the Rows 세로
                for (int i = 0; i < lineItem.ModuleList.Count(); i++)
                {
                    RowDefinition RowDef = new RowDefinition();
                    grid.RowDefinitions.Add(RowDef);
                }

                double cvSize = ShelfSize - 5;

                grid.Width = cvSize;
                grid.Height = lineItem.ModuleList.Count() * cvSize;

                foreach (var CVItem in lineItem.ModuleList)
                {
                    string tooltip = string.Empty;

                    CVUserControl CV = new CVUserControl(CVItem);
                    CV.gridStateLampArray.Visibility = Visibility.Hidden;

                    //220325 HHJ SCS 개발     //- Layoutview 수정
                    //CVUserControl Degisn Width, Height를 가져올 방법이 없는데, 100, 90으로 고정되어있다.
                    TransformGroup TransformGroup = new TransformGroup();
                    TransformGroup.Children.Add(new ScaleTransform(cvSize / 100, cvSize / 90));
                    CV.LayoutTransform = TransformGroup;

                    //220519 HHJ SCS 개선     //- CVUserControl ToolTip 추가
                    CV.MouseEnter += ToolTipControl_MouseEnter;
                    CV.MouseLeave += ToolTipControl_MouseLeave;

                    if (tooltip != string.Empty)
                        CV.ToolTip = tooltip;

                    Grid.SetRow(CV, YLocation++);

                    grid.Children.Add(CV);
                }

                return grid;
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
            }

            return grid;
        }
        private void GridTeachingInit(Grid g)
        {
            try
            {
                LoadGridShelfCreate(g);
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
            }
        }
        private void LoadGridShelfCreate(Grid grid)
        {
            try
            {
                int irow = 0;
                grid.Children.Clear();
                int imaxCol = -1;

                if (!int.TryParse(txtGrdX.Text, out imaxCol))
                {
                    return;
                }

                for (int row = grid.RowDefinitions.Count - 1; row >= 0; row--)
                {
                    for (int col = 0; col < grid.ColumnDefinitions.Count; col++)
                    {
                        //220411 HHJ SCS 개선     //- 쉘프 그리드 세로 숫자 추가
                        if (col.Equals(0))
                        {
                            continue;
                        }


                        if (col > imaxCol)
                        {
                            continue;
                        }

                        //int bank = grid.Tag.Equals("Front") ? (int)eShelfBank.Front : (int)eShelfBank.Rear;
                        int bank = grid.Tag.Equals("Front") ? GlobalData.Current.FrontBankNum : GlobalData.Current.RearBankNum;
                        int bay = col;
                        int level = irow + 1;

                        ShelfItem item = new ShelfItem(ShelfTagHelper.GetTag(bank, bay, level))
                        {
                            ShelfType = eShelfType.Short,
                            SHELFUSE = true,
                        };

                        //220525 HHJ SCS 개선     //- 쉘프 생성시 내부 리스트에 업데이트 하지않고 글로벌 리스트에 업데이트
                        //기존 주석풀어줌.
                        //if (bank.Equals((int)eShelfBank.Front))
                        if (bank.Equals(GlobalData.Current.FrontBankNum))
                        {
                            if (!GlobalData.Current.ShelfMgr.FrontData.ContainKey(item.TagName))
                                GlobalData.Current.ShelfMgr.FrontData.Add(item);
                        }
                        else
                        {
                            if (!GlobalData.Current.ShelfMgr.RearData.ContainKey(item.TagName))
                                GlobalData.Current.ShelfMgr.RearData.Add(item);
                        }

                        Border shelfbd = new Border();
                        shelfbd.Margin = new Thickness(ShelfThick);
                        //shelfbd.ToolTip = item.TagName;
                        shelfbd.Tag = item.TagName;


                        if (bAddRatioShelfSize)
                            shelfbd.Width = (ShelfSize * 1.5) - (ShelfThick * 2);
                        else
                            shelfbd.Width = ShelfSize - (ShelfThick * 2);

                        shelfbd.Height = ShelfSize - (ShelfThick * 2);

                        shelfbd.HorizontalAlignment = HorizontalAlignment.Stretch;
                        shelfbd.VerticalAlignment = VerticalAlignment.Stretch;

                        shelfbd.BorderThickness = new Thickness(2);

                        Grid grdChild = new Grid();
                        Line DisableLine1 = new Line(), DisableLine2 = new Line();

                        Binding WidthBinding = new Binding();
                        WidthBinding.Source = grdChild;
                        WidthBinding.Path = new PropertyPath("ActualWidth");

                        Binding HeightBinding = new Binding();
                        HeightBinding.Source = grdChild;
                        HeightBinding.Path = new PropertyPath("ActualHeight");

                        DisableLine1.X1 = 0;
                        DisableLine1.Y1 = 0;
                        DisableLine1.SetBinding(Line.X2Property, WidthBinding);
                        DisableLine1.SetBinding(Line.Y2Property, HeightBinding);
                        DisableLine1.Stroke = Application.Current.Resources["ShelfDisableStroke"] as Brush;
                        DisableLine1.StrokeThickness = 2;

                        DisableLine2.X1 = 0;
                        DisableLine2.SetBinding(Line.Y1Property, HeightBinding);
                        DisableLine2.SetBinding(Line.X2Property, WidthBinding);
                        DisableLine2.Y2 = 0;
                        DisableLine2.Stroke = Application.Current.Resources["ShelfDisableStroke"] as Brush;
                        DisableLine2.StrokeThickness = 2;

                        grdChild.Children.Add(DisableLine1);
                        grdChild.Children.Add(DisableLine2);

                        shelfbd.Child = grdChild;
                        shelfbd.Background = Brushes.Transparent;

                        shelfbd.MouseEnter += ToolTipControl_MouseEnter;
                        shelfbd.MouseLeave += ToolTipControl_MouseLeave;

                        Grid.SetColumn(shelfbd, col);
                        Grid.SetRow(shelfbd, row);
                        grid.Children.Add(shelfbd);

                        ShelfBdList.Add(item.TagName, shelfbd);
                        //220525 HHJ SCS 개선     //- 쉘프 생성시 내부 리스트에 업데이트 하지않고 글로벌 리스트에 업데이트
                        //ShelfList.Add(item.TagName, item);
                        ChangeControl(item);
                    }

                    irow++;
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
            }
        }
        public void ChangeControl(ShelfItem item)
        {
            try
            {
                Border bd;

                if (!ShelfBdList.ContainsKey(item.TagName))
                    return;

                bd = ShelfBdList[item.TagName];

                //ShelfType
                bd.BorderBrush = etb.Convert(item.ShelfType, typeof(eShelfType), "", null) as Brush;

                if (item.DeadZone)
                    bd.Visibility = Visibility.Hidden;
                else
                    bd.Visibility = Visibility.Visible;

                SetShelfAble(bd, item.SHELFUSE);

                SetShelfZoneName(bd, item.ZONE, item.FloorNum);        //220921 조숭진

            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
            }
        }
        private void SetShelfAble(Border bd, bool able)
        {
            if (bd.Child is Grid child)
            {
                foreach (var v in child.Children)
                {
                    if (v is Line li)
                    {
                        li.Visibility = able ? Visibility.Collapsed : Visibility.Visible;
                    }
                }
            }
        }

        //220921 조숭진
        private void SetShelfZoneName(Border bd, string zonename,int floor)
        {
            if (bd.Child is Grid child)
            {
                child.Children.Clear();
                TextBlock textBlock = new TextBlock();

                //221014 HHJ SCS 개선     //Shelf 생성 페이지 오류 수정
                //textBlock.Text = name.Substring(name.IndexOf("_") + 2);
                textBlock.Text = string.Format("F{0}:{1}",floor, zonename?.Substring(zonename.IndexOf("_") + 2)); //230407 RGJ 쉘프 생성 층 정보 추가 

                textBlock.HorizontalAlignment = HorizontalAlignment.Center;
                textBlock.VerticalAlignment = VerticalAlignment.Top;
                Grid.SetRow(textBlock, 0);
                child.Children.Add(textBlock);
            }
        }

        /// <summary>
        /// Keep the initial position when you clicked
        /// </summary>
        Point _InitPos;

        /// <summary>
        /// A Flag to filter and active selection only when mouse button is held
        /// </summary>
        bool _LeftMouseHeld = false;

        /// <summary>
        /// Collection of elements selected
        /// </summary>
        private List<object> _ResultsList = new List<object>();



        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (selCanvas is null)
                return;

            //초기화는 무조건 진행함.
            foreach (Border bd in _ResultsList)
            {
                bd.Background = Brushes.Transparent;
            }
            _ResultsList.Clear();

            if (sender is Grid grd)
            {
                grd.Children.Add(selCanvas);
                _InitPos = e.GetPosition(grd);
                grd.CaptureMouse();

                Canvas.SetLeft(selReatangle, _InitPos.X);
                Canvas.SetTop(selReatangle, _InitPos.X);
                selReatangle.Visibility = Visibility.Visible;
                _LeftMouseHeld = true;
            }
        }

        private void Grid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (selCanvas is null)
                return;

            if (sender is Grid grd)
            {
                _LeftMouseHeld = false;
                grd.ReleaseMouseCapture();

                selReatangle.Visibility = Visibility.Collapsed;
                selReatangle.Width = 0;
                selReatangle.Height = 0;

                grd.Children.Remove(selCanvas);
            }
            //var Limit = (FrameworkElement)sender;

            //// Set left mouse button state to released
            //_LeftMouseHeld = false;

            //Limit.ReleaseMouseCapture();

            //// Hide all the listbox (if you forget to specify width and height you will have remanent coordinates
            //SelectBox.Visibility = Visibility.Collapsed;
            //SelectBox.Width = 0;
            //SelectBox.Height = 0;

            //if (_ResultsList.Count > 0)
            //{
            //    foreach (FrameworkElement r in _ResultsList)
            //    {
            //        //Debug.WriteLine(r.Name);
            //    }
            //}
        }

        private void Grid_MouseMove(object sender, MouseEventArgs e)
        {
            if (selCanvas is null)
                return;

            if (sender is Grid grd)
            {
                if (_LeftMouseHeld)
                {
                    // Get current position relative to the grid content
                    Point currentPos = e.GetPosition(grd);

                    /*
                        Parameters can't be negative then we will invert base according to mouse value
                    */

                    // X coordinates
                    if (currentPos.X > _InitPos.X)
                    {
                        Canvas.SetLeft(selReatangle, _InitPos.X);
                        selReatangle.Width = currentPos.X - _InitPos.X;
                    }
                    else
                    {
                        Canvas.SetLeft(selReatangle, currentPos.X);
                        selReatangle.Width = _InitPos.X - currentPos.X;
                    }

                    // Y coordinates
                    if (currentPos.Y > _InitPos.Y)
                    {
                        Canvas.SetTop(selReatangle, _InitPos.Y);
                        selReatangle.Height = currentPos.Y - _InitPos.Y;
                    }
                    else
                    {
                        Canvas.SetTop(selReatangle, currentPos.Y);
                        selReatangle.Height = _InitPos.Y - currentPos.Y;
                    }

                    foreach (var v in _ResultsList)
                    {
                        if (v is Border bd)
                        {
                            bd.Background = Brushes.Transparent;
                        }
                    }
                    _ResultsList.Clear();
                    ///*
                    // * With a rectangle geometry you could add every shapes INSIDE the rectangle
                    // * With a point geometry you must go over the shape to select it.
                    // */
                    VisualTreeHelper.HitTest(grd,
                        new HitTestFilterCallback(Filter),
                        new HitTestResultCallback(MyHitTestResult),
                        /*new PointHitTestParameters(currentPos)*/
                        new GeometryHitTestParameters(new RectangleGeometry(new Rect(_InitPos, currentPos)))
                        );
                }
            }
        }

        private void cbbType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox cbb)
            {
                if (cbb.SelectedItem == null)
                    return;

                foreach (Border bd in _ResultsList)
                {
                    //220525 HHJ SCS 개선     //- 쉘프 생성시 내부 리스트에 업데이트 하지않고 글로벌 리스트에 업데이트
                    //if (ShelfList.ContainsKey(bd.Tag.ToString()))
                    //{
                    //    ShelfItem item = ShelfList[bd.Tag.ToString()];
                    //    item.SHELFTYPE = (int)cbb.SelectedItem;

                    //    ChangeControl(item);
                    //}
                    if (GlobalData.Current.ShelfMgr.GetShelf(bd.Tag.ToString()) is ShelfItem item)
                    {
                        item.SHELFTYPE = (int)cbb.SelectedItem;
                        ChangeControl(item);
                    }
                }

                foreach (Border bd in _ResultsList)
                {
                    bd.Background = Brushes.LightYellow;
                }

                cbb.SelectedItem = null;
            }
        }

        private void cbbEnable_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox cbb)
            {
                if (cbb.SelectedItem == null)
                    return;

                foreach (Border bd in _ResultsList)
                {
                    //220525 HHJ SCS 개선     //- 쉘프 생성시 내부 리스트에 업데이트 하지않고 글로벌 리스트에 업데이트
                    //if (ShelfList.ContainsKey(bd.Tag.ToString()))
                    //{
                    //    ShelfItem item = ShelfList[bd.Tag.ToString()];
                    //    item.SHELFUSE = (bool)cbb.SelectedItem;

                    //    ChangeControl(item);
                    //}
                    if (GlobalData.Current.ShelfMgr.GetShelf(bd.Tag.ToString()) is ShelfItem item)
                    {
                        item.SHELFUSE = (bool)cbb.SelectedItem;
                        ChangeControl(item);
                    }
                }

                foreach (Border bd in _ResultsList)
                {
                    bd.Background = Brushes.LightYellow;
                }

                cbb.SelectedItem = null;
            }
        }

        private void cbbDeadZone_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox cbb)
            {
                if (cbb.SelectedItem == null)
                    return;

                foreach (Border bd in _ResultsList)
                {
                    //220525 HHJ SCS 개선     //- 쉘프 생성시 내부 리스트에 업데이트 하지않고 글로벌 리스트에 업데이트
                    //if (ShelfList.ContainsKey(bd.Tag.ToString()))
                    //{
                    //    ShelfItem item = ShelfList[bd.Tag.ToString()];
                    //    item.DeadZone = (bool)cbb.SelectedItem;

                    //    ChangeControl(item);
                    //}
                    if (GlobalData.Current.ShelfMgr.GetShelf(bd.Tag.ToString()) is ShelfItem item)
                    {
                        item.DeadZone = (bool)cbb.SelectedItem;
                        ChangeControl(item);
                    }
                }

                foreach (Border bd in _ResultsList)
                {
                    bd.Background = Brushes.LightYellow;
                }

                cbb.SelectedItem = null;
            }
        }

        private HitTestFilterBehavior Filter(DependencyObject potentialHitTestTarget)
        {
            // Type of return is very important
            if (potentialHitTestTarget is Border)
            {
                if (!_ResultsList.Contains(potentialHitTestTarget))
                {

                    _ResultsList.Add(potentialHitTestTarget);
                    ((Border)potentialHitTestTarget).Background = Brushes.LightYellow;
                }
                return HitTestFilterBehavior.ContinueSkipChildren;
            }

            return HitTestFilterBehavior.Continue;

        }

        // Return the result of the hit test to the callback.
        public HitTestResultBehavior MyHitTestResult(HitTestResult result)
        {
            // Set the behavior to return visuals at all z-order levels.
            return HitTestResultBehavior.Continue;
        }

        private async void SaveData_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (grdSetter.Children.Contains(loadSpinner))
                {
                    grdSetter.Children.Remove(loadSpinner);
                    loadSpinner = null;
                }

                loadSpinner = new LoadingSpinner()
                {
                    Diameter = 60,
                    Color1 = Colors.Gray,
                    Color2 = Colors.Transparent,
                    Msg = "SAVING...",
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                };

                grdSetter.Children.Add(loadSpinner);
                grdSetter.IsEnabled = false;

                if (GlobalData.Current.ShelfMgr.CreateCheckShelfData() == true)        //230309 쉘프 생성시 zone name, shelf type이 없으면 생성 안시킴
                {
                    bSuccess = await GlobalData.Current.ShelfMgr.SaveShelfDataAsync();
                    //bSuccess = await TestTask();
                }

                if (grdSetter.Children.Contains(loadSpinner))
                {
                    grdSetter.Children.Remove(loadSpinner);
                    loadSpinner = null;
                }
            }
            catch (Exception)
            {

            }

            grdSetter.IsEnabled = true;
        }
        #endregion

        //220921 조숭진
        private void cbbZoneName_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox cbb)
            {
                if (cbb.SelectedItem == null)
                    return;

                foreach (Border bd in _ResultsList)
                {
                    if (GlobalData.Current.ShelfMgr.GetShelf(bd.Tag.ToString()) is ShelfItem item)
                    {
                        item.ZONE = GlobalData.Current.EQPID + "_" + string.Format("Z{0:D2}", cbb.SelectedItem);
                        ChangeControl(item);
                    }
                }

                foreach (Border bd in _ResultsList)
                {
                    bd.Background = Brushes.LightYellow;
                }

                cbb.SelectedItem = null;
            }

        }

        private void cbbFloor_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox cbb)
            {
                if (cbb.SelectedItem == null)
                    return;

                foreach (Border bd in _ResultsList)
                {
                    if (GlobalData.Current.ShelfMgr.GetShelf(bd.Tag.ToString()) is ShelfItem item)
                    {
                        item.FloorNum = Convert.ToInt32(cbb.SelectedItem);
                        ChangeControl(item);
                    }
                }

                foreach (Border bd in _ResultsList)
                {
                    bd.Background = Brushes.LightYellow;
                }

                cbb.SelectedItem = null;
            }
        }
    }
}
