using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TranslationByMarkupExtension;

namespace BoxPrint.GUI.ETC
{
    public partial class AlarmPopupView : Window
    {
        public delegate void EventHandler_AlarmOccur(string rvcString);

        public static event EventHandler_AlarmOccur _AlarmOccur;

        public AlarmPopupView()
        {
            InitializeComponent();

            try
            {
                AlarmText.Text = TranslationManager.Instance.Translate("에러코드").ToString() + GlobalData.Current.Alarm_Manager.ActiveAlarmList[0].AlarmID + " : " + GlobalData.Current.Alarm_Manager.ActiveAlarmList[0].AlarmName;
            }

            catch (System.ArgumentOutOfRangeException)
            {
                // 알람 없는 않는 경우, 알람 팝업창 내용 X
            }
        }

        private void BorderButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border)
            {
                var senderBuffer = (Border)sender;

                switch (senderBuffer.Tag.ToString())
                {
                    case "EXIT":
                        this.Close();
                        break;
                    case "YES":
                        _AlarmOccur("Alarm");
                        this.Close();
                        break;
                    case "NO":
                        this.Close();
                        break;
                    default:
                        break;
                }
            }
        }
    }
}