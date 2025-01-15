using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace BoxPrint.GUI.ClassArray
{
    public class ZoomBorder : Border, INotifyPropertyChanged
    {
        private UIElement child = null;
        private Point origin;
        private Point start;

        private DateTime mouseDownTime;
        /// <summary>
        /// 재산 변경
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        protected void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// 스케일 보관용
        /// </summary>
        private double _currentScale = 1;//현제 스케일 관리
        public double CurrentScale
        {
            get => _currentScale;
            set
            {
                if (_currentScale == value) return;
                _currentScale = value;
                RaisePropertyChanged("CurrentScale");

                setScale(value);
            }
        }

        public delegate void EventHandler_Excute();//이벤트

        //생성자
        public ZoomBorder()
        {
  
        }


        private TranslateTransform GetTranslateTransform(UIElement element)
        {
            //스케일 확대시 화면도 같이 커지게 : 그리드 전용
            if (element is Border childBorder)
                return (TranslateTransform)((TransformGroup)childBorder.LayoutTransform)
                  .Children.First(tr => tr is TranslateTransform);
            else
                return (TranslateTransform)((TransformGroup)element.RenderTransform)
                  .Children.First(tr => tr is TranslateTransform);
        }

        private ScaleTransform GetScaleTransform(UIElement element)
        {
            //스케일 확대시 화면도 같이 커지게 : 그리드 전용
            if (element is Border childBorder)
                return (ScaleTransform)((TransformGroup)childBorder.LayoutTransform)
              .Children.First(tr => tr is ScaleTransform);
            else
                return (ScaleTransform)((TransformGroup)element.RenderTransform)
              .Children.First(tr => tr is ScaleTransform);
        }

        public override UIElement Child
        {
            get { return base.Child; }
            set
            {
                if (value != null && value != this.Child)
                    this.Initialize(value);
                base.Child = value;
            }
        }

        //초기화
        public void Initialize(UIElement element)
        {
            //스케일 확대시 화면도 같이 커지게 : 보더 전용
            if (element is Border childBorder)
            {
                this.child = childBorder;

                if (child != null)
                {
                    TransformGroup group = new TransformGroup();
                    ScaleTransform st = new ScaleTransform();
                    group.Children.Add(st);
                    TranslateTransform tt = new TranslateTransform();
                    group.Children.Add(tt);
                    childBorder.LayoutTransform = group;
                    childBorder.RenderTransformOrigin = new Point(0.0, 0.0);
                    this.MouseWheel += child_MouseWheel;
                    //this.MouseLeftButtonDown += child_MouseLeftButtonDown;
                    //this.MouseLeftButtonUp += child_MouseLeftButtonUp;
                    this.PreviewMouseLeftButtonDown += child_PreviewMouseLeftButtonDown;
                    this.PreviewMouseLeftButtonUp += child_PreviewMouseLeftButtonUp;

                    this.MouseMove += child_MouseMove;
                    this.PreviewMouseRightButtonDown += new MouseButtonEventHandler(
                        child_PreviewMouseRightButtonDown);
                }
            }
            else
            {
                this.child = element;
                if (child != null)
                {
                    TransformGroup group = new TransformGroup();
                    ScaleTransform st = new ScaleTransform();
                    group.Children.Add(st);
                    TranslateTransform tt = new TranslateTransform();
                    group.Children.Add(tt);
                    child.RenderTransform = group;
                    child.RenderTransformOrigin = new Point(0.0, 0.0);
                    this.MouseWheel += child_MouseWheel;
                    //this.MouseLeftButtonDown += child_MouseLeftButtonDown;
                    //this.MouseLeftButtonUp += child_MouseLeftButtonUp;
                    this.PreviewMouseLeftButtonDown += child_PreviewMouseLeftButtonDown;
                    this.PreviewMouseLeftButtonUp += child_PreviewMouseLeftButtonUp;
                    this.MouseMove += child_MouseMove;
                    this.PreviewMouseRightButtonDown += new MouseButtonEventHandler(
                      child_PreviewMouseRightButtonDown);
                }
            }

        }

        //맵 회전용 정렬
        public void setRotateMap()
        {
            if (child != null)
            {
                // reset zoom
                var st = GetScaleTransform(child);
                st.ScaleX = 1.8;
                st.ScaleY = 1.8;

                // reset pan
                var tt = GetTranslateTransform(child);
                tt.X = -770.0;
                tt.Y = -435.0;
            }
        }

        //초기 위치로 이동
        public void Reset()
        {
            if (child != null)
            {
                // reset zoom
                var st = GetScaleTransform(child);
                st.ScaleX = 1.0;
                st.ScaleY = 1.0;

                // reset pan
                var tt = GetTranslateTransform(child);
                tt.X = 0.0;
                tt.Y = 0.0;

                CurrentScale = 1.0;
            }
        }

        public void setScale(double rcvScale)
        {
            if (child != null)
            {
                var st = GetScaleTransform(child);

                double zoom = rcvScale;

                st.ScaleX = zoom;
                st.ScaleY = zoom;

                CurrentScale = zoom;
            }
        }

        public double getScale()
        {
            if (child != null)
            {
                var st = GetScaleTransform(child);

                return st.ScaleX;
            }

            return 1;
        }

        public void resetCaptureMouse()
        {
            if (child != null)
            {
                child.ReleaseMouseCapture();
                this.Cursor = Cursors.ScrollAll;
            }
        }

        #region Child Events
        //[트랜스 기능] 휠 마우스 - 확대/축소
        private void child_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (child != null)
            {
                var st = GetScaleTransform(child);
                var tt = GetTranslateTransform(child);

                double zoom = e.Delta > 0 ? .1 : -.1;
                if (!(e.Delta > 0) && (st.ScaleX < .5 || st.ScaleY < .5))
                    return;

                if (!(e.Delta < 0) && (st.ScaleX > 1.5 || st.ScaleY > 1.5))
                    return;

                Point relative = e.GetPosition(child);
                //double absoluteX;
                //double absoluteY;

                //absoluteX = relative.X * st.ScaleX + tt.X;
                //absoluteY = relative.Y * st.ScaleY + tt.Y;

                st.ScaleX += zoom;
                st.ScaleY += zoom;

                //tt.X = absoluteX - relative.X * st.ScaleX;
                //tt.Y = absoluteY - relative.Y * st.ScaleY;

                CurrentScale = st.ScaleX;
            }
        }

        private  void child_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e){
            if (child != null)
            {
                mouseDownTime = DateTime.Now;
                //마우스 다운시 마우스다운한 시간 저장
                var tt = GetTranslateTransform(child);
                start = e.GetPosition(this);
                origin = new Point(tt.X, tt.Y);
                child.CaptureMouse();
            }
        }
        private void child_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e){
            if (child != null)
            {
                child.ReleaseMouseCapture();
                this.Cursor = Cursors.Arrow;
            }
        }
       
        //[트랜스 기능] 마우스 우클릭시 위치 및 스케일 초기화
        void child_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.Reset();
        }

        //[트랜스 기능] 마우스 다운한지 0.3초가 지날 시 마우스 이동 
        private void child_MouseMove(object sender, MouseEventArgs e)
        {
            if (child != null)
            {
                if (child.IsMouseCaptured && (DateTime.Now - mouseDownTime) > TimeSpan.FromSeconds(0.5))
                {
                    var tt = GetTranslateTransform(child);
                    Vector v = start - e.GetPosition(this);
                    tt.X = origin.X - v.X;
                    tt.Y = origin.Y - v.Y;
                    this.Cursor = Cursors.SizeAll;
                }
            }
        }  

        #endregion
    }

    /// <summary>
    /// 퍼센트 스트링으로 변환 컨버터
    /// </summary>
    public class PercentageConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            double distanceFromMin = ((double)values[0] - (double)values[1]);
            double sliderRange = ((double)values[2] - (double)values[1]);
            double sliderPercent = 100 * (distanceFromMin / sliderRange);

            CultureInfo ci = new CultureInfo("en-us");
            return (sliderPercent / 100).ToString("P1", ci);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
