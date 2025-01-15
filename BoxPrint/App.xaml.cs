using BoxPrint.LOCALIZATION;
using BoxPrint.Log;
using System.Globalization;
using System.Windows;
using TranslationByMarkupExtension;

namespace BoxPrint
{
    /// <summary>
    /// App.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class App : Application
    {

        public App()
        {
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;

            //이거추가
            TranslationManager.Instance.TranslationProvider = new ResxTranslationProvider();

            // 한국어로 시작
            Resource.Culture = new CultureInfo("ko-KR");

            // 중국어로 시작
            //Resource.Culture = new CultureInfo("zh-CN");

            // 헝가리어로 시작
            //Resource.Culture = new CultureInfo("hu-HU");
        }

        /// <summary>
        /// // 2021.10.25 RGJ
        /// // -펌웨어 UI 예외 발생시 로그 찍도록 추가.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            LogManager.WriteConsoleLog(eLogLevel.Error, e.Exception.ToString());
        }


        //protected override void OnStartup(StartupEventArgs e)
        //{
        //    base.OnStartup(e);

        //    //initialize the splash screen and set it as the application main window
        //    var splashScreen = new SplashWindow();
        //    this.MainWindow = splashScreen;
        //    splashScreen.Show();

        //    //in order to ensure the UI stays responsive, we need to
        //    //do the work on a different thread
        //    Task.Factory.StartNew(() =>
        //    {
        //        ////we need to do the work in batches so that we can report progress
        //        //for (int i = 1; i <= 100; i++)
        //        //{
        //        //    //simulate a part of work being done
        //        //    System.Threading.Thread.Sleep(30);

        //        //    //because we're not on the UI thread, we need to use the Dispatcher
        //        //    //associated with the splash screen to update the progress bar
        //        //    splashScreen.Dispatcher.Invoke(() => splashScreen.Progress = i);
        //        //}

        //        //once we're done we need to use the Dispatcher
        //        //to create and show the main window

        //        //this.Dispatcher.Invoke(() =>
        //        //{
        //        //    //initialize the main window, set it as the application main window
        //        //    //and close the splash screen
        //        //    var mainWindow = new MainWindow();
        //        //    this.MainWindow = mainWindow;
        //        //    mainWindow.Show();
        //        //    //splashScreen.Close();
        //        //});
        //    });
        //}

    }
}
