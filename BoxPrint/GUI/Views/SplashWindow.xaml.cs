using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace BoxPrint.GUI.Views
{
    /// <summary>
    /// SplashWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class SplashWindow : Window
    {
        //Thread loadingThread;
        Storyboard Showboard;
        Storyboard Hideboard;
        private delegate void ShowDelegate(string txt);
        private delegate void HideDelegate();
        ShowDelegate showDelegate;
        HideDelegate hideDelegate;

        private int i;
        private double startPos;
        private DispatcherTimer timer;

        public SplashWindow()
        {
            InitializeComponent();
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(50);
            timer.Tick += new EventHandler(timer_Tick);
            //timer.Start();

            //GlobalData.Current.SendEvent += Current_Message;

            showDelegate = new ShowDelegate(this.showText);
            hideDelegate = new HideDelegate(this.hideText);
            Showboard = this.Resources["showStoryBoard"] as Storyboard;
            Hideboard = this.Resources["HideStoryBoard"] as Storyboard;

            this.Dispatcher.Invoke(showDelegate, "Program Loading");

            i = 0;

            // Store start position of sliding canvas
            startPos = Canvas.GetLeft(SlidingCanvas);

            // Create animation timer

            //StartAnimation();

            Thread t1 = new Thread(new ThreadStart(DoSomething));
            t1.IsBackground = true;
            t1.Start();
        }

        private void DoSomething()
        {
            try
            {
                while (true)
                {

                    if (GlobalData.Current != null)
                    {
                        if (GlobalData.Current.GlobalInitComp)
                        {
                            this.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)delegate () { Close(); });
                            break;
                        }
                    }

                    if (i < 16)
                    {
                        Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                        {
                            Canvas.SetLeft(SlidingCanvas, Canvas.GetLeft(SlidingCanvas) + 14);
                        }));
                        i++;
                    }
                    else
                    {
                        i = 0;
                        Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                        {
                            Canvas.SetLeft(SlidingCanvas, startPos);
                        }));
                    }
                    Thread.Sleep(100);
                }
            }
            catch (ThreadAbortException e)
            {
                Console.WriteLine(e);
                Thread.ResetAbort();
            }
            finally
            {
                Console.WriteLine("Clearing resource...");
            }
        }

        /// <summary>
        /// Event 전송에 대하여 Display 한다.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Current_Message(object sender, EventArgs e)
        {
            string JInfo = (string)sender;

            this.Dispatcher.Invoke(showDelegate, JInfo);

            //if (JInfo == "Initializing : GlobalData Complete")
            //{
            //    this.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)delegate () { Close(); });
            //    timer.Stop();
            //}
            //Thread.Sleep(2000);

            this.Dispatcher.Invoke(hideDelegate);
        }

        private void showText(string txt)
        {
            txtLoading.Text = txt;
            BeginStoryboard(Showboard);
        }
        private void hideText()
        {
            BeginStoryboard(Hideboard);
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            if (this.IsVisible)
            {
                i++;

                if (GlobalData.Current.GlobalInitComp)
                {
                    this.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)delegate () { Close(); });
                    timer.Stop();
                }


                if (i < 16)
                {
                    // Move SlidingCanvas containg the three colored dots 14 units to the right
                    Canvas.SetLeft(SlidingCanvas, Canvas.GetLeft(SlidingCanvas) + 14);
                }
                else
                {
                    // Move SlidingCanvas back to its starting position and reset counter
                    i = 0;
                    Canvas.SetLeft(SlidingCanvas, startPos);
                }
            }

        }

        //public double Progress
        //{
        //    get { return progressBar.Value; }
        //    set { progressBar.Value = value; }
        //}

        //public void StartAnimation()
        //{
        //    timer.Start();
        //}

        //public void StopAnimation()
        //{
        //    timer.Stop();
        //}

        //private void load()
        //{
        //    Thread.Sleep(2000);
        //    this.Dispatcher.Invoke(showDelegate, "first data to loading");
        //    Thread.Sleep(2000);
        //    //load data 
        //    this.Dispatcher.Invoke(hideDelegate);

        //    Thread.Sleep(2000);
        //    this.Dispatcher.Invoke(showDelegate, "second data loading");
        //    Thread.Sleep(2000);
        //    //load data
        //    this.Dispatcher.Invoke(hideDelegate);

        //    Thread.Sleep(2000);
        //    this.Dispatcher.Invoke(showDelegate, "last data loading");
        //    Thread.Sleep(2000);
        //    //load data 
        //    this.Dispatcher.Invoke(hideDelegate);

        //    //close the window
        //    Thread.Sleep(2000);
        //    this.Dispatcher.Invoke(DispatcherPriority.Normal,(Action)delegate () { Close(); });
        //}
    }
}
