using System;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Controls;
using System.Windows.Input;
using System.Collections.Generic;
using Stockerfirmware.CCLink;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using Stockerfirmware.Monitor;

namespace Stockerfirmware.GUI.Views
{

    // 2020.09.24 RGJ
    /// <summary>
    /// IOMonitorView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class WPSMonitorView : Page
    {

        public bool ConverterSubView = false;
        private string strtag = string.Empty;
        private delegate void D_Set_StringValue(string nValue);
        DispatcherTimer timer = new DispatcherTimer();    // timer 객체생성


        public WPSMonitorView()
        {
            InitializeComponent();
            timer.Interval = TimeSpan.FromMilliseconds(300);    //시간간격 설정
            timer.Tick += new EventHandler(timer_Tick);          //이벤트 추가
            timer.Start();

            GlobalData.Current.WPS_mgr.Converter_Monitor.OnWPSDataReceived += ConverterMonitor_OnWPSDataReceived;
            GlobalData.Current.WPS_mgr.Converter_SubMonitor.OnWPSDataReceived += Converter_SubMonitor_OnWPSDataReceived;
            GlobalData.Current.WPS_mgr.Regulator_Monitor.OnWPSDataReceived += RegulatorMonitor_OnWPSDataReceived;
          
            GlobalData.Current.SendTagChange += Current_ReceiveEvent;
        }

        private void Converter_SubMonitor_OnWPSDataReceived(object sender, EventArgs e)
        {
            WPSMonitorBase wps = sender as WPSMonitorBase;
            if (wps != null)
            {
                ChangeRegulatorIndicator(Colors.LawnGreen);
            }
        }

        private void RegulatorMonitor_OnWPSDataReceived(object sender, EventArgs e)
        {
            WPSMonitorBase wps = sender as WPSMonitorBase;
            if(wps != null)
            {
                ChangeRegulatorIndicator(Colors.LawnGreen);
            }
        }

        private void ConverterMonitor_OnWPSDataReceived(object sender, EventArgs e)
        {
            WPSMonitorBase wps = sender as WPSMonitorBase;
            if (wps != null)
            {
                ChangeConverterIndicator(Colors.LawnGreen);
            }
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            ChangeConverterIndicator(Colors.Gray);
            ChangeRegulatorIndicator(Colors.Gray);
        }

        private void Current_ReceiveEvent(object sender, EventArgs e)
        {
            string JInfo = (string)sender;
            this.Dispatcher.Invoke(new D_Set_StringValue(_DisplayChange), JInfo);
        }

        private void _DisplayChange(string strtag)
        {
            try
            {
                this.strtag = strtag;
                initLoad(strtag);
            }
            catch (Exception ex)
            {
                Log.LogManager.WriteCSLog(eLogLevel.Fatal, ex.ToString());
            }
        }

        private void initLoad(string tag)
        {
            dataGrid_Converter.ItemsSource = GlobalData.Current.WPS_mgr.Converter_Monitor.WPSItemList;

            dataGrid_Regulator.ItemsSource = GlobalData.Current.WPS_mgr.Regulator_Monitor.WPSItemList;
        }
        private void ChangeConverterView(bool SubViewRequest)
        {
            if(SubViewRequest)
            {
                dataGrid_Converter.ItemsSource = GlobalData.Current.WPS_mgr.Converter_SubMonitor.WPSItemList;
            }
            else
            {
                dataGrid_Converter.ItemsSource = GlobalData.Current.WPS_mgr.Converter_Monitor.WPSItemList;
            }
            
        }

        public void ChangeConverterIndicator(Color targetColor)
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
            {
                LinearGradientBrush newLinearGradientBrush = new LinearGradientBrush();

                newLinearGradientBrush.StartPoint = new Point(0.5, 1);
                newLinearGradientBrush.EndPoint = new Point(0.5, 0);
                newLinearGradientBrush.GradientStops.Add(new GradientStop(Colors.White, 0.0));
                newLinearGradientBrush.GradientStops.Add(new GradientStop(targetColor, 1.0));
                ConverterLamp.Fill = newLinearGradientBrush;
            }));
        }
        public void ChangeRegulatorIndicator(Color targetColor)
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
            {
                LinearGradientBrush newLinearGradientBrush = new LinearGradientBrush();

                newLinearGradientBrush.StartPoint = new Point(0.5, 1);
                newLinearGradientBrush.EndPoint = new Point(0.5, 0);
                newLinearGradientBrush.GradientStops.Add(new GradientStop(Colors.White, 0.0));
                newLinearGradientBrush.GradientStops.Add(new GradientStop(targetColor, 1.0));
                RegulatorLamp.Fill = newLinearGradientBrush;
            }));
        }


        private void button_ChangeMainSub_Click(object sender, RoutedEventArgs e)
        {
            ConverterSubView = !ConverterSubView;
            ChangeConverterView(ConverterSubView);
            if (ConverterSubView)
            {
                Label_Converter.Content = "CONVERTER[S]";
                button_ChangeMainSub.Content = "CPS 메인뷰 전환";
            }
            else
            {
                Label_Converter.Content = "CONVERTER[M]";
                button_ChangeMainSub.Content = "CPS 서브뷰 전환";
            }

            
        }

        private void button_CpsReset_Click(object sender, RoutedEventArgs e)
        {
            if (ConverterSubView) //서브
            {
                string Message = string.Format("서브 CPS를 시작 하시겠습니까?");
                if( System.Windows.MessageBox.Show(Message, "명령 확인", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                {
                    GlobalData.Current.WPS_mgr.Converter_SubMonitor.RequestReset();
                }
            }
            else //메인
            {
                string Message = string.Format("메인 CPS를 리셋하시겠습니까?");
                if (System.Windows.MessageBox.Show(Message, "명령 확인", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                {
                    GlobalData.Current.WPS_mgr.Converter_Monitor.RequestReset();
                }
            }
        }

        private void button_CpsStart_Click(object sender, RoutedEventArgs e)
        {
            if (ConverterSubView) //서브
            {
                string Message = string.Format("서브 CPS를 시작 하시겠습니까?");
                if (System.Windows.MessageBox.Show(Message, "명령 확인", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                {
                    GlobalData.Current.WPS_mgr.Converter_SubMonitor.RequestRun();
                }
            }
            else //메인
            {
                string Message = string.Format("메인 CPS를 시작 하시겠습니까?");

                if (System.Windows.MessageBox.Show(Message, "명령 확인", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                {
                    GlobalData.Current.WPS_mgr.Converter_Monitor.RequestRun();
                }
            }
        }
    }
   
 
}
