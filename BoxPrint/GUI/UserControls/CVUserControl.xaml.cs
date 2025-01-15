using BoxPrint.DataList;
using BoxPrint.Modules.Conveyor;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace BoxPrint.GUI
{
    //
    /// <summary>
    /// CVUserControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class CVUserControl : UserControl
    {
        private ShelfClass mshelf;
        private CV_BaseModule CVModule;
        //Storyboard SB_Turn = null;

        private bool TurnRequest = false;
        private bool ReturnRequest = false;
        RotateTransform rt = new RotateTransform(0, 0, 0);

        //SuHwan_20220411
        Storyboard storyboardBuffer = null;
        bool isStoryboardBegin = false;

        private double _conveyorTurnAngle;
        public double ConveyorTurnAngle
        {
            get { return _conveyorTurnAngle; }
            set
            {
                _conveyorTurnAngle = value;
            }
        }
        bool isTurnStoryboardRun = false;

        public CVUserControl(CV_BaseModule cv)
        {
            InitializeComponent();

            DataContext = this;//SuHwan_20220412

            mshelf = cv.CVRobotTeaching;
            CVModule = cv;
            CVModule.CVPropertyChanged += CVModule_PropertyChanged;
            initLoad();
            //SB_Turn = Resources["TurnAnimation"] as Storyboard;
            pathConveyorTurnImage.Visibility = Visibility.Hidden;

            if (CVModule.CVModuleType == eCVType.Turn || CVModule.CVModuleType == eCVType.TurnEQIF || CVModule.CVModuleType == eCVType.TurnOHTIF || CVModule.CVModuleType == eCVType.TurnBridge || CVModule.CVModuleType == eCVType.ShuttleTurn)    //2021.05.24 lim, TurnOHT 추가
            {
                gridConveyorImage_pathConveyorType.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom("#FFFB9D20");
                pathConveyorTurnImage.Visibility = Visibility.Visible;

                DispatcherTimer TurnTimer = new DispatcherTimer();
                TurnTimer.Interval = TimeSpan.FromMilliseconds(100);    //시간간격 설정
                TurnTimer.Tick += new EventHandler(timer_Tick);          //이벤트 추가
                TurnTimer.Start();
            }
            if (CVModule.AutoManualState == eCVAutoManualState.AutoRun)
            {
                ChangeStateLampColor(eCVAutoManualState.AutoRun);
            }
            else
            {
                ChangeStateLampColor();
            }

            DispatcherTimer AniTimer = new DispatcherTimer();
            AniTimer.Interval = TimeSpan.FromMilliseconds(100);    //시간간격 설정
            AniTimer.Tick += new EventHandler(timer_Tick2);          //이벤트 추가
            AniTimer.Start();

        }
        public void CVTurn_Animate()
        {
            TurnRequest = true;
            ReturnRequest = false;
        }
        public void CVReturn_Animate()
        {
            TurnRequest = false;
            ReturnRequest = true;
        }

        // 컨베어 런 타이머
        private void timer_Tick2(object sender, EventArgs e)
        {
            if (this.isStoryboardBegin == true)
            {
                if (this.storyboardBuffer == null)
                {
                    TimeSpan time = DateTime.Now.TimeOfDay;

                    if (time.Seconds % 2 == 0)
                    {
                        if (time.Milliseconds > 750)
                        {
                            this.storyboardBuffer = Resources["storyboardMoveConveyor"] as Storyboard;
                            this.storyboardBuffer.Begin();
                        }
                    }
                }
            }
        }

        int testTurnAngle = 0;

        // 컨베어 턴 타이머
        private void timer_Tick(object sender, EventArgs e)
        {
            if (TurnRequest)
            {
                RotateTransform rotation = gridConveyorImage.RenderTransform as RotateTransform;
                if (rotation != null)
                    this.ConveyorTurnAngle = rotation.Angle;
                else
                    this.ConveyorTurnAngle = 0;

                this.pathConveyorTurnText.Text = this.ConveyorTurnAngle.ToString();

                if (ConveyorTurnAngle >= testTurnAngle)
                {
                    TurnRequest = false;
                    ReturnRequest = false;
                    return;
                }

                if (isTurnStoryboardRun == false)
                    setTurnAnimation(testTurnAngle);
            }
            else if (ReturnRequest)
            {
                RotateTransform rotation = gridConveyorImage.RenderTransform as RotateTransform;
                if (rotation != null)
                    this.ConveyorTurnAngle = rotation.Angle;
                else
                    this.ConveyorTurnAngle = 0;

                this.pathConveyorTurnText.Text = this.ConveyorTurnAngle.ToString();

                if (ConveyorTurnAngle <= 0)
                {
                    TurnRequest = false;
                    ReturnRequest = false;
                    return;
                }

                if (isTurnStoryboardRun == false)
                    setTurnAnimation(0);
            }




        }

        public void SetControlRotate(double angle)
        {
            RotateTransform rt = new RotateTransform(0, 0, 0);
            rt.Angle = angle;
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
            {
                MainCanvas.RenderTransform = rt;
                rt.Angle = -angle; //텍스트는 역으로 돌려서 항상 0도로 되돌림
                TB_CVSpeed.RenderTransform = rt;
                TB_RFID.RenderTransform = rt;
            }));
        }
        private void CVModule_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {

            switch (e.PropertyName)
            {
                case "ConveyorAutoStateChanged":
                    if (CVModule.AutoManualState == eCVAutoManualState.AutoRun)
                    {
                        ChangeStateLampColor(eCVAutoManualState.AutoRun);
                    }
                    else
                    {
                        ChangeStateLampColor();
                    }
                    break;
                case "ConveyorRunning":
                    eCV_Speed spd = CVModule.GetCurrentRunSpeed();
                    ChangeSpeedState(spd);
                    if (spd == eCV_Speed.None)
                    {
                        CVStopAnimate();
                    }
                    else
                    {
                        CVRunAnimate();
                    }
                    break;
                case "LastFWDStopperState":
                    ChangeStopperView(CVModule.LastFWD_StopperState, true);
                    break;
                case "LastBWDStopperState":
                    ChangeStopperView(CVModule.LastBWD_StopperState, false);
                    break;
                case "LastDoorState":
                    break;
                case "LastTurnState":
                    if (CVModule.LastTurnState == eCV_TurnState.Turn)
                    {
                        CVTurn_Animate();
                    }
                    else if (CVModule.LastTurnState == eCV_TurnState.Return)
                    {
                        CVReturn_Animate();
                    }
                    break;
                case "TrayState":
                    ChangeTrayPosition(CVModule.CarrierExist, CVModule.CarrierPosition);
                    break;
                case "LastRFIDConnected":
                    ChangeRFIDConnetionState(CVModule.CheckRFID_Connection());
                    break;
                case "LastLightCutainMuteOnState":
                    ChangeLightCurtainView(!CVModule.LastLightCutainMuteOnState); //뮤트상태면 라이트 커튼 동작상태가 아님
                    break;
            }
        }
        public void ChangeTrayAdvanceDirection(eDirection Dir)
        {
            switch (Dir)
            {
                case eDirection.Up:
                    gridCVBackgroundArray.RenderTransform = new RotateTransform(0);
                    break;
                case eDirection.Right:
                    gridCVBackgroundArray.RenderTransform = new RotateTransform(90);
                    break;
                case eDirection.Down:
                    gridCVBackgroundArray.RenderTransform = new RotateTransform(180);
                    break;
                case eDirection.Left:
                    gridCVBackgroundArray.RenderTransform = new RotateTransform(270);
                    break;
            }
        }
        private void initLoad()
        {
            TB_RFID.Text = GlobalData.Current.UseBCR ? "B" : "R";     //2021.04.08 lim,

            image_Tray.Visibility = Visibility.Hidden;
            LeftLightCurtain.Visibility = Visibility.Hidden;
            RightLightCurtain.Visibility = Visibility.Hidden;
            LightCurtainRec.Visibility = Visibility.Hidden;

            int bank = (CVModule.ParentModule as BoxPrint.Modules.CVLine.CVLineModule).Position_Bank;
            if (CVModule.PortInOutType == ePortInOutType.INPUT)
            {
                //ChangeTrayAdvanceDirection(true);
                //ChangeTrayAdvanceDirection(bank == 1 ? false : true);     //2021.06.10 lim, front 쪽에 위치한 in/out port 모두 방향을 맞추기 위해서  
                ChangeTrayAdvanceDirection(eDirection.Up);
            }
            else
            {
                //ChangeTrayAdvanceDirection(false);
                //ChangeTrayAdvanceDirection(bank == 1 ? true : false);
                ChangeTrayAdvanceDirection(eDirection.Down);
            }
            //if (CVModule.CVModuleType == eCVType.RobotIF || CVModule.CVModuleType == eCVType.OHTRobot) //로봇 인터페이스 모듈만 라이트 커튼 표시
            if (CVModule.UseLightCurtain) //로봇 인터페이스 모듈만 라이트 커튼 표시  //2021.05.25 lim, 2F 로봇 인터페이스는 미사용 이므로 XML 파일에서 설정된 것만 표시
            {
                this.LeftLightCurtain.Visibility = Visibility.Visible;
                this.RightLightCurtain.Visibility = Visibility.Visible;
                this.LightCurtainRec.Visibility = Visibility.Visible;
            }
            if (!CVModule.UseStopper)
            {
                this.UpperStopper.Visibility = Visibility.Collapsed;
                this.LowerStopper.Visibility = Visibility.Collapsed;
            }
            if (CVModule.CVModuleType != eCVType.Turn && CVModule.CVModuleType != eCVType.TurnEQIF && CVModule.CVModuleType != eCVType.TurnOHTIF && CVModule.CVModuleType != eCVType.TurnBridge && CVModule.CVModuleType != eCVType.ShuttleTurn)    //2021.05.24 lim, TurnOHT 추가
            {
                this.pathConveyorTurnImage.Visibility = Visibility.Collapsed;
            }
            if (CVModule.CVModuleType == eCVType.OHTRobot)      //2021.05.28 lim, OHTRobot는 컨베이어가 없다.
            {
                this.CVStateLamp.Visibility = Visibility.Collapsed;
                this.Border_CVSpeed.Visibility = Visibility.Collapsed;
            }
            ChangeStateLampColor();

            if (!CVModule.UseBCR)
            {
                this.Border_RFID.Visibility = Visibility.Collapsed;
            }
            else
            {
                ChangeRFIDConnetionState(CVModule.CheckRFID_Connection());
            }
            ChangeEntryStopState(false, false);
        }
        public void CVRunAnimate()
        {
            Action action = () =>
            {
                //Storyboard SB_Run = this.Resources["CVRun"] as Storyboard;
                //SB_Run.Begin();

                this.storyboardBuffer = null;
                this.isStoryboardBegin = true;

            };
            Dispatcher.Invoke(action, DispatcherPriority.Render);

        }
        public void CVStopAnimate()
        {

            Action action = () =>
            {
                //Storyboard SB_Run = this.Resources["CVRun"] as Storyboard;
                //SB_Run.Stop();

                this.isStoryboardBegin = false;

                if (this.storyboardBuffer != null)
                    this.storyboardBuffer.Stop();

            };
            Dispatcher.Invoke(action, DispatcherPriority.Render);

        }



        public void ChangeStateLampColor(eCVAutoManualState rcvState = eCVAutoManualState.None)
        {

            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
            {
                //LinearGradientBrush newLinearGradientBrush = new LinearGradientBrush();

                //newLinearGradientBrush.StartPoint = new Point(0.5, 1);
                //newLinearGradientBrush.EndPoint = new Point(0.5, 0);
                //newLinearGradientBrush.GradientStops.Add(new GradientStop(Colors.White, 0.0));
                //newLinearGradientBrush.GradientStops.Add(new GradientStop(targetColor, 1.0));


                //CVStateLamp.Fill = newLinearGradientBrush;

                if (rcvState == eCVAutoManualState.AutoRun)
                {
                    CVStateLamp.Text = "A";
                    CVStateLamp.Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom("#FF44D3A6");
                }
                else
                {
                    CVStateLamp.Text = "M";
                    CVStateLamp.Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom("#FFEC685B");
                }

            }));

        }
        public void ChangeLightCurtainView(bool OnOff)
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
            {
                if (OnOff)
                {
                    this.RightLightCurtain.Fill = new SolidColorBrush(Colors.Yellow);
                    this.LeftLightCurtain.Fill = new SolidColorBrush(Colors.Yellow);
                    this.LightCurtainRec.Opacity = 0.2;
                }
                else
                {
                    this.RightLightCurtain.Fill = new SolidColorBrush(Colors.LightGray);
                    this.LeftLightCurtain.Fill = new SolidColorBrush(Colors.LightGray);
                    this.LightCurtainRec.Opacity = 0.0;
                }
            }));

        }
        public void ChangeStopperView(eCV_StopperState StopperState, bool FowardStopper)
        {
            //2021.06.10 lim, FrontShelf는 컨베이어 방향이 나열 방향이 반대이므로 구분 필요
            int bank = (CVModule.ParentModule as BoxPrint.Modules.CVLine.CVLineModule).Position_Bank;

            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
            {
                // IsInPort  xor FowardStopper xor Bank=1   GUI Stopper 
                //     인 1       후방 0           Bank 0    하단 1    
                //     인 1       전방 1           Bank 0    상단 0     
                //     아웃 0     후방 0           Bank 0    상단 0     
                //     아웃 0     전방 1           Bank 0    하단 1    
                //     인 1       후방 0           Bank 1    하단 0    
                //     인 1       전방 1           Bank 1    상단 1    
                //     아웃 0     후방 0           Bank 1    상단 1    
                //     아웃 0     전방 1           Bank 1    하단 0    

                if (CVModule.IsInPort ^ FowardStopper ^ bank == 1) //xor 연산해서 GUI 반영
                {
                    if (StopperState == eCV_StopperState.Up)
                    {
                        this.LowerStopper.Fill = new SolidColorBrush(Colors.Red);
                        this.LowerStopper.Opacity = 1;
                    }
                    else if (StopperState == eCV_StopperState.Down)
                    {
                        this.LowerStopper.Fill = new SolidColorBrush(Colors.LightGray);
                        this.LowerStopper.Opacity = 0.5;
                    }
                    else if (StopperState == eCV_StopperState.None)
                    {
                        LowerStopper.Visibility = Visibility.Collapsed;
                    }
                }
                else
                {
                    if (StopperState == eCV_StopperState.Up)
                    {
                        this.UpperStopper.Fill = new SolidColorBrush(Colors.Red);
                        this.UpperStopper.Opacity = 1;
                    }
                    else if (StopperState == eCV_StopperState.Down)
                    {
                        this.UpperStopper.Fill = new SolidColorBrush(Colors.LightGray);
                        this.UpperStopper.Opacity = 0.5;
                    }
                    else if (StopperState == eCV_StopperState.None)
                    {
                        UpperStopper.Visibility = Visibility.Collapsed;
                    }
                }
            }));
        }

        public void ChangeSpeedState(eCV_Speed spd)
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
            {
                switch (spd)
                {
                    case eCV_Speed.None:
                        this.TB_CVSpeed.Text = "N";
                        break;
                    case eCV_Speed.High:
                        this.TB_CVSpeed.Text = "3";
                        break;
                    case eCV_Speed.Mid:
                        this.TB_CVSpeed.Text = "2";
                        break;
                    case eCV_Speed.Low:
                        this.TB_CVSpeed.Text = "1";
                        break;
                }
            }));
        }

        public void ChangeTrayPosition(bool Show, double Percent)
        {
            //2021.06.10 lim, FrontShelf는 컨베이어 방향이 나열 방향이 반대이므로 구분 필요
            int bank = (CVModule.ParentModule as BoxPrint.Modules.CVLine.CVLineModule).Position_Bank;

            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
            {
                if (Show)
                {
                    if (CVModule.IsInPort ^ bank == 1) //인포트는 반대로 퍼센트 계산
                    {
                        Percent = 70 - Percent;
                    }
                    if (CVModule.SimulMode)
                    {
                        this.image_Tray.Visibility = Visibility.Visible;
                    }
                    double calcTop = (MainCanvas.ActualHeight * Percent) / 100;
                    Canvas.SetTop(image_Tray, calcTop);
                }
                else
                {
                    this.image_Tray.Visibility = Visibility.Hidden;
                }
            }));
        }

        public void ChangeRFIDConnetionState(bool OnOFF)
        {

            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
            {
                if (OnOFF)
                {

                    this.TB_RFID.Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom("#FF44D3A6");
                }
                else
                {
                    this.TB_RFID.Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom("#FFEC685B");
                }
            }));

        }

        public void ChangeEntryStopState(bool EntryOn, bool StopOn)
        {

            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
            {
                if (this.CVModule.IsInPort) //Inport 일경우 Entry,Stop 스왑한다.
                {
                    Entry_Lamp.Fill = new SolidColorBrush(StopOn ? Colors.LawnGreen : Colors.Transparent);
                    Stop_Lamp.Fill = new SolidColorBrush(EntryOn ? Colors.LawnGreen : Colors.Transparent);
                }
                else
                {
                    Entry_Lamp.Fill = new SolidColorBrush(EntryOn ? Colors.LawnGreen : Colors.Transparent);
                    Stop_Lamp.Fill = new SolidColorBrush(StopOn ? Colors.LawnGreen : Colors.Transparent);
                }
            }));
        }

        public CV_BaseModule GetCVModule()
        {
            return CVModule;
        }

        //테스트용 나중에 지우자   
        private void Grid_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (this.isStoryboardBegin)
            {
                this.isStoryboardBegin = false;

                if (this.storyboardBuffer != null)
                    this.storyboardBuffer.Stop();

                ReturnRequest = true;

            }
            else
            {
                this.storyboardBuffer = null;
                this.isStoryboardBegin = true;

                testTurnAngle = 90;
                TurnRequest = true;
            }

        }

        //턴 스토리보드 생성
        private void setTurnAnimation(double rcvTurnAngle)
        {
            if (isTurnStoryboardRun == false)
            {
                this.isTurnStoryboardRun = true;

                Storyboard storyboardBuffer = new Storyboard();
                DoubleAnimation animationBuffer = new DoubleAnimation();

                animationBuffer.Duration = new Duration(TimeSpan.FromMilliseconds(1000));
                animationBuffer.To = rcvTurnAngle;
                ;
                Storyboard.SetTargetProperty(animationBuffer, new PropertyPath("(UIElement.RenderTransform).(RotateTransform.Angle)"));

                storyboardBuffer.Children.Add(animationBuffer);
                storyboardBuffer.Completed += Storyboard_Completed;
                storyboardBuffer.Begin(this.gridConveyorImage);
            }
        }
        //턴 스토리보드 완료 처리
        private void Storyboard_Completed(object sender, EventArgs e)
        {
            this.isTurnStoryboardRun = false;
        }
    }
}
