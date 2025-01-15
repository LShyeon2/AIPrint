using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace BoxPrint.GUI.ETC.LoadingPopup
{

    public partial class LodingPopup : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private bool isCreate = false;
        private bool isAutoStop = false;
        DispatcherTimer timer = new DispatcherTimer();


        //싱글톤
        private static readonly Lazy<LodingPopup> instance = new Lazy<LodingPopup>(() => new LodingPopup());
        public static LodingPopup Instance
        {
            get
            {
                return instance.Value;
            }
        }

        //진행률 글자
        public string ProgressText
        {
            get
            {
                return _progressValue.ToString() + "%";
            }
        }

        //진행률 숫자
        private int _progressValue = 0;
        public int ProgressValue
        {
            get
            {
                return _progressValue;
            }
            set
            {
                if (_progressValue == value) return;

                _progressValue = value;
                RaisePropertyChanged("ProgressValue");
                RaisePropertyChanged("ProgressText");
            }
        }

        //진행 정보
        private string _progressInformation = "Information";
        public string ProgressInformation
        {
            get
            {
                return _progressInformation;
            }
            private set
            {
                if (_progressInformation == value) return;

                _progressInformation = value;
                RaisePropertyChanged("ProgressInformation");
            }
        }

        /// <summary>
        /// 생성자
        /// </summary>
        public LodingPopup()
        {
            InitializeComponent();
            DataContext = this;

            timer.Interval = TimeSpan.FromMilliseconds(200);    //시간간격 설정
            timer.Tick += new EventHandler(timer_Tick);          //이벤트 추가
        }

        /// <summary>
        /// 타이머
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer_Tick(object sender, EventArgs e)
        {
            if (ProgressValue >= 100)
            {
                Stop();
                isAutoStop = false;
            }

            if (isAutoStop)
            {

                if (ProgressValue < 99)
                {
                    ProgressValue = 99;
                    ProgressInformation = "Completion Processing..";
                }
                else
                {
                    ProgressValue++;
                }
            }
        }

        /// <summary>
        /// 로딩팝업 시작
        /// </summary>
        public void Start()
        {
            this.Owner = Application.Current.MainWindow;
            //this.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            this.Height = Application.Current.MainWindow.ActualHeight;
            this.Width = Application.Current.MainWindow.ActualWidth;
            var location = Application.Current.MainWindow.PointToScreen(new Point(0, 0));//현제 윈도우 위치를 받아온다
            this.Left = location.X;
            this.Top = location.Y;

            if (!isCreate)
            {
                this.Show();
                this.Visibility = Visibility.Hidden;
                isCreate = true;
            }
            else
            {
                initValue();

                if (!this.IsActive)
                    this.Activate();

                if (!this.IsFocused)
                    this.Focus();
            }

            (FindResource("showMe") as Storyboard).Begin(this);

            timer.Start();
        }

        /// <summary>
        /// 로딩 팝업 중단
        /// </summary>
        public void Stop()
        {
            initValue();
            timer.Stop();
            (FindResource("hideMe") as Storyboard).Begin(this);
        }

        /// <summary>
        /// 값 초기화
        /// </summary>
        public void initValue()
        {
            ProgressValue = 0;
            ProgressInformation = "";
        }

        /// <summary>
        /// 자동으로 100프로 만든뒤 중단
        /// </summary>
        public void AutoStop()
        {
            isAutoStop = true;
        }

        /// <summary>
        /// 진행률 값 넣기
        /// </summary>
        /// <param name="rcvProgressValue"></param>
        /// <param name="rcvProgressInformation"></param>
        public void setProgressValue(int rcvProgressValue, string rcvProgressInformation = "")
        {
            if (ProgressValue > rcvProgressValue)
                return;

            ProgressValue = rcvProgressValue;
            ProgressInformation = rcvProgressInformation;
        }

        /// <summary>
        /// 재산 변경
        /// </summary>
        /// <param name="propertyName"></param>
        protected void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    public class ClippingBorder : Border
    {
        protected override void OnRender(DrawingContext dc)
        {
            OnApplyChildClip();
            base.OnRender(dc);
        }

        public override UIElement Child
        {
            get
            {
                return base.Child;
            }
            set
            {
                if (this.Child != value)
                {
                    if (this.Child != null)
                    {
                        // Restore original clipping
                        this.Child.SetValue(UIElement.ClipProperty, _oldClip);
                    }

                    if (value != null)
                    {
                        _oldClip = value.ReadLocalValue(UIElement.ClipProperty);
                    }
                    else
                    {
                        // If we dont set it to null we could leak a Geometry object
                        _oldClip = null;
                    }

                    base.Child = value;
                }
            }
        }

        protected virtual void OnApplyChildClip()
        {
            UIElement child = this.Child;
            if (child != null)
            {
                _clipRect.RadiusX = _clipRect.RadiusY = Math.Max(0.0, this.CornerRadius.TopLeft - (this.BorderThickness.Left * 0.5));
                _clipRect.Rect = new Rect(Child.RenderSize);
                child.Clip = _clipRect;
            }
        }

        private RectangleGeometry _clipRect = new RectangleGeometry();
        private object _oldClip;
    }
}
