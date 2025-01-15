using BoxPrint.GUI.EventCollection;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;

namespace BoxPrint.GUI.ETC.DateTimePicker
{
    public partial class DateTimePicker : UserControl
    {
        private const string DateTimeFormat = "yyyy.MM.dd HH:mm";
        
        #region "Properties"

        public DateTime SelectedDate
        {
            get => (DateTime)GetValue(SelectedDateProperty);
            set
            {
                SetValue(SelectedDateProperty, value);
                SelectedDate_String = value.ToString(DateTimeFormat);
                DateDisplay.Text = value.ToString(DateTimeFormat);
                if(Hours != null && Min != null)
                {
                    Hours.SelectedIndex = value.Hour % 24;
                    Min.SelectedIndex = value.Minute / 5;
                }

            }
        }

        private string _SelectedDate_String;
        public string SelectedDate_String
        {
            get => _SelectedDate_String;
            set => _SelectedDate_String = value;
        }
        #endregion

        #region "DependencyProperties"

        public static readonly DependencyProperty SelectedDateProperty = DependencyProperty.Register("SelectedDate",
            typeof(DateTime), typeof(DateTimePicker), new FrameworkPropertyMetadata(DateTime.Now, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        #endregion

        public DateTimePicker()
        {
            InitializeComponent();
            CalDisplay.SelectedDatesChanged += CalDisplay_SelectedDatesChanged;
            //CalDisplay.SelectedDate = DateTime.Now.AddDays(0);

            DataContext = this;

            UIEventCollection.Instance.OnCultureChanged += _UIEventCollection_OnCultureChanged;
        }

        private void _UIEventCollection_OnCultureChanged(string cultureKey)
        {
            //this.Language = XmlLanguage.GetLanguage(cultureKey);
        }

        #region "EventHandlers"

        private void CalDisplay_SelectedDatesChanged(object sender, EventArgs e)
        {
            var hours = (Hours?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "0";
            var minutes = (Min?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "0";
            TimeSpan timeSpan = TimeSpan.Parse(hours + ":" + minutes);
            //if (CalDisplay.SelectedDate.Value.Date == DateTime.Today.Date && timeSpan.CompareTo(DateTime.Now.TimeOfDay) < 0)
            //{
            //    timeSpan = TimeSpan.FromHours(DateTime.Now.Hour + 1);
            //}
            var date = CalDisplay.SelectedDate.Value.Date + timeSpan;
            DateDisplay.Text = date.ToString(DateTimeFormat);
            SelectedDate = date;
        }

        private void SaveTime_Click(object sender, RoutedEventArgs e)
        {
            try //240404 RGJ 클릭 이벤트 예외 못잡으면 프로그램 다운 되므로 전부 예외처리함.
            {
                CalDisplay_SelectedDatesChanged(SaveTime, EventArgs.Empty);
                PopUpCalendarButton.IsChecked = false;
            }
            catch (Exception ex)
            {
                Log.LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
            }
        }

        private void Time_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CalDisplay_SelectedDatesChanged(sender, e);
        }

        private void CalDisplay_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (Mouse.Captured is CalendarItem)
            {
                Mouse.Capture(null);
            }
        }

        #endregion

        #region 토글버튼 에니 설정...너무 힘들어서 그냥 비하인드에 기능 작성
        private void PopUpCalendarButton_MouseEnter(object sender, MouseEventArgs e)
        {
            PopUpCalendarButton.IsTabStop = true;
        }

        private void PopUpCalendarButton_MouseLeave(object sender, MouseEventArgs e)
        {
            if (PopUpCalendarButton.IsChecked != true)
                PopUpCalendarButton.IsTabStop = false;
        }

        private void CalendarPopup_Closed(object sender, EventArgs e)
        {
            PopUpCalendarButton.IsTabStop = false;
        }
        #endregion
    }

    [ValueConversion(typeof(bool), typeof(bool))]
    public class InvertBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(bool))
                throw new InvalidOperationException("The target must be a boolean");

            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(bool))
                throw new InvalidOperationException("The target must be a boolean");

            return !(bool)value;
        }
    }
}