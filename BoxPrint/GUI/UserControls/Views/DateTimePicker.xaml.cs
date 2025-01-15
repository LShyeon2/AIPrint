using BoxPrint.GUI.EventCollection;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using TranslationByMarkupExtension;

namespace BoxPrint.GUI.UserControls.Views
{
    /// <summary>
    /// DateTimePicker.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class DateTimePicker : UserControl
    {
        #region Variable
        #region Field
        private string DateTimeFormat = "yyyy.MM.dd HH:mm:ss";

        public DateTime SelectedDate
        {
            get => (DateTime)GetValue(SelectedDateProperty);
            set => SetValue(SelectedDateProperty, value);
        }
        #endregion

        #region DependencyProperties
        public static readonly DependencyProperty SelectedDateProperty = DependencyProperty.Register("SelectedDate",
            typeof(DateTime), typeof(DateTimePicker), new FrameworkPropertyMetadata(DateTime.Now, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged));
        /// <summary>
        /// Property Change CallBack
        /// 값이 변경되면 내부 컨트롤 변경 진행
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DateTimePicker dtp && e.NewValue is DateTime dt)
            {
                dtp.DateChanged(dt);
            }
        }
        #endregion
        #endregion

        #region Constructor
        public DateTimePicker()
        {
            InitializeComponent();
            
            _UIEventCollection_OnCultureChanged(TranslationManager.Instance.CurrentLanguage.ToString());
            UIEventCollection.Instance.OnCultureChanged += _UIEventCollection_OnCultureChanged;
        }
        #endregion

        #region Methods
        #region Event
        /// <summary>
        /// 달력, 시분초 선택 후 최종 확인 버튼 클릭
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveTime_Click(object sender, RoutedEventArgs e)
        {
            try //240404 RGJ 클릭 이벤트 예외 못잡으면 프로그램 다운 되므로 전부 예외처리함.
            {
                DateChanged();
                PopUpCalendarButton.IsChecked = false;
            }
            catch (Exception ex)
            {
                Log.LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
            }
        }
        /// <summary>
        /// 언어 변경 이벤트
        /// </summary>
        /// <param name="cultureKey"></param>
        private void _UIEventCollection_OnCultureChanged(string cultureKey)
        {
            if (string.IsNullOrEmpty(cultureKey))
                cultureKey = "ko-KR";

            CalDisplay.Language = XmlLanguage.GetLanguage(cultureKey);

            CultureInfo us = new CultureInfo(cultureKey);
            string DateFormatString = us.DateTimeFormat.ShortDatePattern;
            string TimeFormatString = "HH:mm:ss";        //시:분:초는 고정 포맷으로 사용하고, 24시기준으로 나타낸다

            DateTimeFormat = string.Format("{0} {1}", DateFormatString, TimeFormatString);
            DateChanged();
        }
        /// <summary>
        /// 달력 토글상태 변경
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PopUpCalendarButton_IsHitTestVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            //시간만 휠로 조정하고 OK를 눌리지않으면 다음 달력을 펼쳐볼때 시간이 유지되는 현상이 발생한다.
            //토글이 변경되는 시점으로 PopUp이 열릴때 시간정보를 선택되어있는 시간을 기준으로 초기화 해준다.
            if (!(bool)e.NewValue)
            {
                dtHour.Value = SelectedDate.Hour;
                dtMinute.Value = SelectedDate.Minute;
                dtSecond.Value = SelectedDate.Second;
            }
        }
        #endregion
        #region Etc
        /// <summary>
        /// 컨트롤내 달력 선택정보와 시분초를 가져와서 바인딩될 날짜정보를 업데이트
        /// </summary>
        private void DateChanged()
        {
            var hour = dtHour.Value;
            var minute = dtMinute.Value;
            var second = dtSecond.Value;

            TimeSpan timeSpan = TimeSpan.Parse(string.Format("{0}:{1}:{2}", hour, minute, second));
            var date = CalDisplay.SelectedDate.Value.Date + timeSpan;
            DateDisplay.Text = date.ToString(DateTimeFormat);
            SelectedDate = date;
        }
        /// <summary>
        /// 전달받은 시간값으로 내부 컨트롤 변경
        /// </summary>
        /// <param name="changeDT"></param>
        private void DateChanged(DateTime changeDT)
        {
            dtHour.Value = changeDT.Hour;
            dtMinute.Value = changeDT.Minute;
            dtSecond.Value = changeDT.Second;

            CalDisplay.SelectedDate = changeDT;
            DateChanged();
        }
        #endregion

        #endregion

        private void CalDisplay_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            //Calendar의 Date가 선택되면 마우스를 독점하게되어 다른 컨트롤을 클릭하기전에는 마우스의 독점이 풀리지않음.
            //선택이 되는 이벤트가 발생하면 Mouse Capture 캡쳐를 초기화해줘서 마우스 독점을 없에준다.
            System.Windows.Input.Mouse.Capture(null);
        }
    }
}
