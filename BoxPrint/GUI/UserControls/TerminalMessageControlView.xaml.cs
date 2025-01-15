using BoxPrint.GUI.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace BoxPrint.GUI.UserControls
{
    /// <summary>
    /// TerminalMessageControlView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class TerminalMessageControlView : UserControl
    {
        DispatcherTimer timer = new DispatcherTimer();

        public TerminalMessageControlView()
        {
            InitializeComponent();
            vm = new TerminalMessageControlViewModel();
            DataContext = vm;

            GlobalData.Current.DBManager.DbGetProcedureTerminalMSG();

            //if(GlobalData.Current.ServerClientType == eServerClientType.Client)
            //{
            //    timer.Interval = TimeSpan.FromMilliseconds(10000);
            //    timer.Tick += new EventHandler(timer_Tick);          //이벤트 추가
            //}
        }

        //private void timer_Tick(object sender, EventArgs e)
        //{
        //    //GlobalData.Current.TerminalMessageRefreshedOccur();
        //}

        //private void Page_Unloaded(object sender, RoutedEventArgs e)
        //{
        //    timer.Stop();
        //}

        //private void Page_Loaded(object sender, RoutedEventArgs e)
        //{
        //    if (!timer.IsEnabled)
        //    {
        //        timer.Start();
        //    }
        //}

        private TerminalMessageControlViewModel vm { get; set; }
    }
}
