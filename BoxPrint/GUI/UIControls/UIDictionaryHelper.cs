using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;

namespace BoxPrint.GUI.UIControls
{
    //생성자
    public partial class UIDictionaryHelper : ResourceDictionary
    {
        //툴팁 생성 위치
        private void ToolTipOpenedHandler(object sender, RoutedEventArgs e)
        {
            ToolTip toolTip = (ToolTip)sender;
            UIElement target = toolTip.PlacementTarget;
            Point adjust = target.TranslatePoint(new Point(8, 0), toolTip);
            if (adjust.Y > 0)
            {
                toolTip.Placement = PlacementMode.Top;
            }
            toolTip.Tag = new Thickness(0, 0, 0, 0);
        }


    }

    //툴팁 중앙 배치
    public class CenterToolTipConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.FirstOrDefault(v => v == DependencyProperty.UnsetValue) != null)
            {
                return double.NaN;
            }
            double placementTargetWidth = (double)values[0];
            double toolTipWidth = (double)values[1];
            return (placementTargetWidth / 2.0) - (toolTipWidth / 2.0);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }


    public class LayOutViewScaleConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return Math.Abs((100 / (double)value));
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    public class LayOutViewWidthConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (((double)value - 100) / 9) * 16;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class LayOutViewHeightConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (double)value - 100;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ThicknessMaxConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Thickness thickness = (Thickness)value;
            double horizontalMax = Math.Max(thickness.Left, thickness.Right);
            double verticalMax = Math.Max(thickness.Top, thickness.Bottom);
            return Math.Max(horizontalMax, verticalMax);
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    //버튼 컨트롤
    public class SK_ButtonControl : Button, INotifyPropertyChanged
    {
        private bool PublicButton = false; //권한 상관없는 공용 버튼으로 간주함
        public void SetPublicButton() 
        {
            UserAuthority = true;
            LockIcon = Visibility.Hidden;
            PublicButton = true;
        }

        //230102 YSW 사용자 권한
        private bool _UserAuthority = false; // 나중에 false로 바꿔놓자
        public bool UserAuthority
        {
            get { return _UserAuthority; }
            set
            {
                if(PublicButton)
                {
                    return;
                }
                if (_UserAuthority != value)
                {
                    _UserAuthority = value;
                    OnPropertyChanged("UserAuthority");
                }
            }
        }

        //230102 YSW 사용자 권한
        private Visibility _LockIcon = Visibility.Visible;
        public Visibility LockIcon
        {
            get { return _LockIcon; }
            set
            {
                if (PublicButton)
                {
                    return;
                }
                if (_LockIcon != value)
                {
                    _LockIcon = value;
                    OnPropertyChanged("LockIcon");
                }
            }
        }

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

        //선택 상황
        private bool _isSelect = false;
        public bool IsSelect
        {
            get { return _isSelect; }
            set
            {
                if (_isSelect != value)
                {
                    _isSelect = value;
                    OnPropertyChanged("IsSelect");
                }
            }
        }

        //현제 넓이
        public double CurrentWidth { get { return this.ActualWidth; } }

        //아이콘 색상
        private SolidColorBrush _iconFill = Brushes.Black;
        public SolidColorBrush IconFill
        {
            get { return _iconFill; }
            set
            {
                if (_iconFill != value)
                {
                    _iconFill = value;
                    OnPropertyChanged("IconFill");
                }
            }
        }

        //마우스 오버 색상
        private SolidColorBrush _mouseOverColor = Brushes.Black;
        public SolidColorBrush MouseOverColor
        {
            get { return _mouseOverColor; }
            set
            {
                if (_mouseOverColor != value)
                {
                    _mouseOverColor = value;
                    OnPropertyChanged("MouseOverColor");
                }
            }
        }

        //아이콘 마우스 오버 색상
        private SolidColorBrush _iconMouseOverColor = Brushes.Black;
        public SolidColorBrush IconMouseOverColor
        {
            get { return _iconMouseOverColor; }
            set
            {
                if (_iconMouseOverColor != value)
                {
                    _iconMouseOverColor = value;
                    OnPropertyChanged("IconMouseOverColor");
                }
            }
        }

        //선택 색상
        private SolidColorBrush _SelectColor = Brushes.Lime;
        public SolidColorBrush SelectColor
        {
            get { return _SelectColor; }
            set
            {
                if (_SelectColor != value)
                {
                    _SelectColor = value;
                    OnPropertyChanged("SelectColor");
                }
            }
        }


        //이미지 마진 - 이미지 크기가 조절된다
        private Thickness _imageMargin = new Thickness(15);
        public Thickness ImageMargin
        {
            get { return _imageMargin; }
            set
            {
                if (_imageMargin != value)
                {
                    _imageMargin = value;
                    OnPropertyChanged("ImageMargin");
                }
            }
        }

        //버튼 테두리 둥글기 
        private CornerRadius _frameCornerRadius;
        public CornerRadius FrameCornerRadius
        {
            get { return _frameCornerRadius; }
            set
            {
                if (_frameCornerRadius != value)
                {
                    _frameCornerRadius = value;
                    OnPropertyChanged("FrameCornerRadius");
                }
            }
        }

        //이미지 
        private ImageSource _ImagePath;
        public ImageSource ImagePath
        {
            get { return _ImagePath; }
            set
            {
                if (_ImagePath != value)
                {
                    _ImagePath = value;
                    OnPropertyChanged("ImagePath");
                }
            }
        }

        //SuHwan_20230712 : 툴팁 관련 추가
       
        private string _TooltipDefault;
       /// <summary>
       /// 기본 툴팁 
       /// </summary>
        public string TooltipDefault
        {
            get { return _TooltipDefault; }
            set
            {
                if (_TooltipDefault != value)
                {
                    _TooltipDefault = value;
                    OnPropertyChanged("TooltipDefault");
                }
            }
        }

        private string _TooltipDisable;
        /// <summary>
        /// 미사용중일때 툴팁
        /// </summary>
        public string TooltipDisable
        {
            get { return _TooltipDisable; }
            set
            {
                if (_TooltipDisable != value)
                {
                    _TooltipDisable = value;
                    OnPropertyChanged("TooltipDisable");
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

    //보더 컨트롤
    public class DashedBorderControl : Control, INotifyPropertyChanged
    {
        //버튼 테두리 둥글기 
        private CornerRadius _frameCornerRadius;
        public CornerRadius FrameCornerRadius
        {
            get { return _frameCornerRadius; }
            set
            {
                if (_frameCornerRadius != value)
                {
                    _frameCornerRadius = value;
                    OnPropertyChanged("FrameCornerRadius");
                }
            }
        }

        private DoubleCollection _StrokeDashArray;
        public DoubleCollection StrokeDashArray
        {
            get { return _StrokeDashArray; }
            set
            {
                if (_StrokeDashArray != value)
                {
                    _StrokeDashArray = value;
                    OnPropertyChanged("StrokeDashArray");
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
