using BoxPrint.GUI.UIControls;
using BoxPrint.GUI.ViewModels;
using BoxPrint.Log;
using System;
using System.Windows;
using System.Windows.Input;

namespace BoxPrint.GUI.ETC
{
    /// <summary>
    /// UnitIODetailView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class UnitIODetailView : Window
    {
        private UnitIODetailViewModel vm;

        public UnitIODetailView(ControlBase selectunit, bool IsPlayBack)
        {
            InitializeComponent();

            vm = new UnitIODetailViewModel(IsPlayBack);
            DataContext = vm;

            vm.AbleViewModel(selectunit);
        }

        private void SK_ButtonControl_Click(object sender, RoutedEventArgs e)
        {
            try //240404 RGJ 클릭 이벤트 예외 못잡으면 프로그램 다운 되므로 전부 예외처리함.
            {
                string buttonname = string.Empty;

                if (sender is SK_ButtonControl senderBuffer)
                {
                    switch (senderBuffer.Tag)
                    {
                        case "Cancel":
                            vm.DisableViewmodel();
                            Close();
                            buttonname = "EXIT";
                            break;

                        default:
                            break;
                    }

                    Close();

                    LogManager.WriteOperatorLog(string.Format("사용자가 {0}의 수동지시 상세 {1} 을/를 Click하였습니다.", vm.UnitID, buttonname),
                        "CLOSE", senderBuffer.Tag, GlobalData.Current.CurrentIP, string.Empty, GlobalData.Current.ClientPCName, GlobalData.Current.CurrentUserID, 9,
                        vm.UnitID, buttonname);
                }
            }
            catch (Exception ex)
            {
                Log.LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
            }
            
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }
    }
}
