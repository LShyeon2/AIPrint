using System;
using System.Windows;
using System.Windows.Controls;

namespace BoxPrint.GUI.ETC
{
    /// <summary>
    /// GroupAuthority.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class GroupAuthority : Window
    {
        public GroupAuthority()
        {
            InitializeComponent();
            InitContorl();
        }

        private void InitContorl()
        {
            LevelGroup.ItemsSource = Enum.GetValues(typeof(eUserLevel));
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try //240404 RGJ 클릭 이벤트 예외 못잡으면 프로그램 다운 되므로 전부 예외처리함.
            {
                if (sender is Button btn)
                {
                    switch (btn.Tag.ToString())
                    {
                        case "Confirm":
                            if (LevelGroup.Text == "admin")
                            {
                                return;
                            }

                            break;
                        case "Cancel":
                            break;
                    }

                    Close();
                }
            }
            catch (Exception ex)
            {
                Log.LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
            }

        }
    }
}
