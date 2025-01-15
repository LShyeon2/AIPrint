using System;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Stockerfirmware.Log;
using Stockerfirmware.SSCNet;
using Stockerfirmware.DataList;
using Stockerfirmware.Modules.CVLine;

namespace Stockerfirmware.GUI.Views
{

    // 2021.04.02 RGJ
    /// <summary>
    /// ServoView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ServoView : Page
    {
        private ServoManager ServoMgr;
        private string strtag = string.Empty;

        private delegate void D_Set_StringValue(string nValue);

        DispatcherTimer timer = new DispatcherTimer();    // timer 객체생성

        public ServoView()
        {
            InitializeComponent();
            GlobalData.Current.SendTagChange += Current_ReceiveEvent;
            ServoMgr = ServoManager.GetManagerInstance();
            if(ServoMgr.ServoSimulMode)
            {
                Label_ServoTitle.Content += " [Simulation Mode]";
            }
            dataGrid_Teaching.ItemsSource = ServoMgr.TurnTeachingList;
            dataGrid_ServoPosition.ItemsSource = ServoMgr.GetServoList();

            for(int i=0;i < ServoMgr.MaxAxis;i++)
            {
                cb_Axis.Items.Add(i+1);
            }
            cb_Axis.SelectedIndex = 0;
            cb_Axis_SelectionChanged(null, null);
            timer.Interval = TimeSpan.FromMilliseconds(100);    //시간간격 설정
            timer.Tick += new EventHandler(timer_Tick);          //이벤트 추가
            timer.Start();
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            if(!IsVisible)
            {
                return; //화면상 보일때만 서보 상태 갱신
            }
            else
            {
                string SystemAlarm;
                string ServoAlarm;
                string OperAlarm;
                bool MinusLimit;
                bool Home;
                bool PlusLimit;
                //서보온 포지션 상태업데이트
                ServoStateUpdate();

                //선택된 축 상태 업데이트
                int axis = (int)cb_Axis.SelectedValue; 

                //알람 상태 업데이트
                ServoMgr[axis].GetAxisAlarmCode(out SystemAlarm, out ServoAlarm, out OperAlarm);
                textBox_SystemAlarm.Text = SystemAlarm;
                textBox_ServoAlarm.Text = ServoAlarm;
                textBox_OPAlarm.Text = OperAlarm;

                //리미트 센서 업데이트
                ServoMgr[axis].GetSensorState(out MinusLimit, out Home, out PlusLimit);
                this.Rec_MinusLimitSensor.Fill = new SolidColorBrush(MinusLimit? Colors.Red : Colors.White);
                this.Rec_HomeSensor.Fill = new SolidColorBrush(Home? Colors.GreenYellow : Colors.White);
                this.Rec_PlusLimitSensor.Fill = new SolidColorBrush(PlusLimit ? Colors.Red : Colors.White);

                //서보 비트 상태 업데이트
                bool[] status = ServoMgr[axis].GetAxisStatus();
                label_RDY.Background = new SolidColorBrush(status[0] ? Colors.GreenYellow : Colors.White);  //RDY Servo Ready
                label_INP.Background = new SolidColorBrush(status[1] ? Colors.GreenYellow : Colors.White);  //INP 인포지션
                label_TLC.Background = new SolidColorBrush(status[2] ? Colors.GreenYellow : Colors.White);  //TLC 토크제한중
                label_SAL.Background = new SolidColorBrush(status[3] ? Colors.GreenYellow : Colors.White);  //SALM 서보알람중
                label_SWR.Background = new SolidColorBrush(status[4] ? Colors.GreenYellow : Colors.White);  //SWRN 서보경고중
                label_OP.Background = new SolidColorBrush(status[5] ? Colors.GreenYellow : Colors.White);   //OP 운전중
                label_ZP.Background = new SolidColorBrush(status[6] ? Colors.GreenYellow : Colors.White);   //ZP 원점복귀 완료
                label_OAL.Background = new SolidColorBrush(status[7] ? Colors.GreenYellow : Colors.White);  //OALM 운전 알람중
            }
        }
        private void ServoStateUpdate()
        {
            try
            {
                ServoMgr.UpdateServoPosition();
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
            }
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
            }
            catch (Exception ex)
            {
                Log.LogManager.WriteCSLog(eLogLevel.Fatal, ex.ToString());
            }
        }

        private void dataGrid_ServoPosition_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                ServoAxis Axis = e.AddedItems[0] as ServoAxis;
                if (Axis != null)
                {
                    cb_Axis.SelectedIndex = Axis.AxisNumber - 1;
                }
            }
            catch
            {
                ;
            }
        }

        private void button_AxisServoOn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int selectAxis = (int)cb_Axis.SelectedValue; 
                if (ServoMgr[selectAxis].IsServoOn)  //서보 On 시 Off
                {
                    string msg = selectAxis + "Axis Servo OFF 합니다. 진행 하시겠습니까?";
                    MessageBoxResult result = System.Windows.MessageBox.Show(msg, "Command Check", MessageBoxButton.YesNo);
                    if (result == MessageBoxResult.Yes)
                    {
                        ServoMgr[selectAxis].ServoOff();
                    }
                }
                else 
                {
                    string msg = selectAxis + "Axis Servo ON 합니다. 진행 하시겠습니까?";
                    MessageBoxResult result = System.Windows.MessageBox.Show(msg, "Command Check", MessageBoxButton.YesNo);
                    if (result == MessageBoxResult.Yes)  //서보 Off 시 On
                    {
                        ServoMgr[selectAxis].ServoOn();
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
            }
        }

        private void button_AxisHome_Click(object sender, RoutedEventArgs e)
        {
            //서보 Home 동작
            try
            {
                int selectAxis = (int)cb_Axis.SelectedValue; 
                string msg = selectAxis + " Axis Home 작업을 시작하시겠습니까?";
                MessageBoxResult result = System.Windows.MessageBox.Show(msg, "Command Check", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)  //서보 Off 시 On
                {
                    var CV = GlobalData.Current.LineManager.GetCVModuleByServoAxisNumber(selectAxis);
                    if (CV != null)
                    {
                        if (CV.GetCVStopperState(true) == eCV_StopperState.Up && CV.GetCVStopperState(false) == eCV_StopperState.Up)
                        {
                            //스톱퍼 상태 닫힌상태에서만 동작 가능 OK
                        }
                        else
                        {
                            msg = string.Format("{0} 의 스톱퍼 상태가 열려 있습니다. 닫으시겠습니까?", CV.ModuleName);
                            result = System.Windows.MessageBox.Show(msg, "Info Messange", MessageBoxButton.YesNo);
                            if (result == MessageBoxResult.Yes)
                            {
                                CV.CVStopperClose(true);
                                CV.CVStopperClose(false);
                                if (CV.GetCVStopperState(true) == eCV_StopperState.Up && CV.GetCVStopperState(false) == eCV_StopperState.Up)
                                {
                                    //스톱퍼 상태 OK
                                }
                                else //시도했으나 실패
                                {
                                    string Failmsg = string.Format("{0} 의 스톱퍼 동작에 실패하였습니다.동작을 취소합니다.", CV.ModuleName);
                                    System.Windows.MessageBox.Show(Failmsg, "Info Messange", MessageBoxButton.OK);
                                    return;
                                }
                            }
                            else
                            {
                                return;
                            }
                        }
                    }
                    else
                    {
                        //해당축에 정의된 CV 가 없다.
                        LogManager.WriteConsoleLog(eLogLevel.Info, "해당축이 정의된 CV 모듈이 없습니다.");
                        return;
                    }
                    ServoMgr[selectAxis].AxisHomeMove();
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
            }
        }

        private void button_AxisStop_Click(object sender, RoutedEventArgs e)
        {
            //스톱은 확인창 없이 바로 중지
            ServoMgr.RequesetAllStop();
        }

        private void button_AxisHomeSave_Click(object sender, RoutedEventArgs e)
        {

            //홈 절대값 파라미터 파일에 저장
            int selectAxis = (int)cb_Axis.SelectedValue; 
            short Value024D, Value024E, Value024F;
            string msg = string.Format("현재 {0} 축 Home Parameter를 저장하시겠습니까?", selectAxis);
            MessageBoxResult result = System.Windows.MessageBox.Show(msg, "Command Check", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)  //서보 Off 시 On
            {
                if (ServoMgr.ServoSimulMode)
                {
                    return;
                }
                //절대 위치 파라미터 불러오기
                int ans = ServoMgr.GetServoDrive().sGetParameter(selectAxis, 0x024D, out Value024D);
                if (ans != 0)
                {
                    System.Windows.MessageBox.Show("서버 홈 파라미터 0x024D 불러오기에 실패하였습니다.", "실패");
                    return;
                }
                //절대 위치 파라미터 불러오기
                ans = ServoMgr.GetServoDrive().sGetParameter(selectAxis, 0x024E, out Value024E);
                if (ans != 0)
                {
                    MessageBox.Show("서버 홈 파라미터 0x024E 불러오기에 실패하였습니다.", "실패");
                    return;
                }
                //절대 위치 파라미터 불러오기
                ans = ServoMgr.GetServoDrive().sGetParameter(selectAxis, 0x024F, out Value024F);
                if (ans != 0)
                {
                    MessageBox.Show("서버 홈 파라미터 0x024F 불러오기에 실패하였습니다.", "실패");
                    return;
                }
                //절대 위치 파라미터 ini에 쓰기
                ServoMgr.GetServoDrive().WriteServoHomeParameter_Ini(selectAxis, Value024D, Value024E, Value024F);
                MessageBox.Show("현재 {0} 축 서보 홈 파라미터를 파일에 저장하였습니다.", "성공");

            }
        }

        private void button_AxisReset_Click(object sender, RoutedEventArgs e)
        {
            if (ServoMgr.ServoSimulMode)
            {
                return;
            }

            int selectAxis = (int)cb_Axis.SelectedValue; 
            //서보 리셋
            ServoMgr.GetServoDrive().sServo_AlarmReset();
        }
        private void JogAction(bool isPlus,bool isStart)
        {
            int selectAxis = (int)cb_Axis.SelectedValue; 
            int currentSpped = (int)slider_JogSpeed.Value;

            if(isStart) //시작할때는 스톱퍼 상태를 확인한다.
            {
                var CV = GlobalData.Current.LineManager.GetCVModuleByServoAxisNumber(selectAxis);
                if(CV != null)
                {
                    if (CV.GetCVStopperState(true) == eCV_StopperState.Up && CV.GetCVStopperState(false) == eCV_StopperState.Up)
                    {
                        //스톱퍼 상태 닫힌상태에서만 동작 가능 OK
                    }
                    else
                    {
                        string msg = string.Format("{0} 의 스톱퍼 상태가 열려 있습니다. 닫으시겠습니까?",CV.ModuleName);
                        MessageBoxResult result = System.Windows.MessageBox.Show(msg, "Info Messange", MessageBoxButton.YesNo);
                        if (result == MessageBoxResult.Yes)
                        {
                            CV.CVStopperClose(true);
                            CV.CVStopperClose(false);
                            if (CV.GetCVStopperState(true) == eCV_StopperState.Up && CV.GetCVStopperState(false) == eCV_StopperState.Up)
                            {
                                //스톱퍼 상태 OK
                                //안전을 위해 리턴 시키고 다시 눌러서 동작하게한다.
                                return;
                            }
                            else //시도했으나 실패
                            {
                                string Failmsg = string.Format("{0} 의 스톱퍼 동작에 실패하였습니다.동작을 취소합니다.", CV.ModuleName);
                                System.Windows.MessageBox.Show(Failmsg, "Info Messange", MessageBoxButton.OK);
                                return;
                            }
                        }
                        else
                        {
                            return;
                        }
                    }
                }
                else
                {
                    //해당축에 정의된 CV 가 없다.
                    LogManager.WriteConsoleLog(eLogLevel.Info, "해당축이 정의된 CV 모듈이 없습니다.");
                    return;
                }
            }
  
            switch (comboBox_JogMode.SelectedIndex)
            {
                case 0:     //연속 모드
                    if(isStart)
                    {
                        if(ServoMgr.ServoSimulMode == false)
                        {
                            ServoMgr.GetServoDrive().Servo_JogMoving(selectAxis, currentSpped, isPlus ? 0 : 1);
                        }
                    }
                    else
                    {
                        if (ServoMgr.ServoSimulMode == false)
                        {
                            ServoMgr.GetServoDrive().sDriveStop();
                        }
                    }
                    break;
                case 1:     //1 Pulse
                    if(isStart)
                    {
                        ServoMgr[selectAxis].AxisJogMove(isPlus ? 1 : -1, currentSpped);
                    }
                    break;
                case 2:     //10 Pulse
                    if (isStart)
                    {
                        ServoMgr[selectAxis].AxisJogMove(isPlus ? 10 : -10, currentSpped);
                    }
                    break;
                case 3:     //50 Pulse
                    if (isStart)
                    {
                        ServoMgr[selectAxis].AxisJogMove(isPlus ? 50 : -50, currentSpped);
                    }
                    break;
                case 4:     //100 Pulse
                    if (isStart)
                    {
                        ServoMgr[selectAxis].AxisJogMove(isPlus ? 100 : -100, currentSpped);
                    }
                    break;
                case 5:     //500 Pulse
                    if (isStart)
                    {
                        ServoMgr[selectAxis].AxisJogMove(isPlus ? 500 : -500, currentSpped);
                    }
                    break;
                case 6:     //1000 Pulse
                    if (isStart)
                    {
                        ServoMgr[selectAxis].AxisJogMove(isPlus ? 1000 : -1000, currentSpped);
                    }
                    break;
                default:
                    break;
            }
        }

        private void button_Jog_Plus_MouseDown(object sender, MouseButtonEventArgs e)
        {
            JogAction(true, true);
        }
        private void button_Jog_Plus_MouseUp(object sender, MouseButtonEventArgs e)
        {
            JogAction(true, false);
        }

        private void button_Jog_Minus_MouseDown(object sender, MouseButtonEventArgs e)
        {
            JogAction(false, true);
        }

        private void button_Jog_Minus_MouseUp(object sender, MouseButtonEventArgs e)
        {
            JogAction(false, false);
        }

        private void button_Move_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Axis Control GUI 기자재,셔터 인터락 추가.
                TurnTeachingItem turnItem = dataGrid_Teaching.SelectedCells[0].Item as TurnTeachingItem;
                if (turnItem == null)
                    return;
                if (GlobalData.Current.MainBooth.BoothState != eBoothState.AutoStart)
                {
                    string msg = string.Format("{0} 축  Tag : {1}  Position : {2} 위치로 이동하시겠습니까?", turnItem.Axis, turnItem.TagName ,turnItem.PositionValue);
                    MessageBoxResult result = System.Windows.MessageBox.Show(msg, "Command Check", MessageBoxButton.YesNo);
                    if (result == MessageBoxResult.Yes)
                    {
                        var CV = GlobalData.Current.LineManager.GetCVModuleByServoAxisNumber(turnItem.Axis);
                        if (CV != null)
                        {
                            if (CV.GetCVStopperState(true) == eCV_StopperState.Up && CV.GetCVStopperState(false) == eCV_StopperState.Up)
                            {
                                //스톱퍼 상태 닫힌상태에서만 동작 가능 OK
                            }
                            else
                            {
                                msg = string.Format("{0} 의 스톱퍼 상태가 열려 있습니다. 닫으시겠습니까?", CV.ModuleName);
                                result = System.Windows.MessageBox.Show(msg, "Info Messange", MessageBoxButton.YesNo);
                                if (result == MessageBoxResult.Yes)
                                {
                                    CV.CVStopperClose(true);
                                    CV.CVStopperClose(false);
                                    if (CV.GetCVStopperState(true) == eCV_StopperState.Up && CV.GetCVStopperState(false) == eCV_StopperState.Up)
                                    {
                                        //스톱퍼 상태 OK
                                    }
                                    else //시도했으나 실패
                                    {
                                        string Failmsg = string.Format("{0} 의 스톱퍼 동작에 실패하였습니다.동작을 취소합니다.", CV.ModuleName);
                                        System.Windows.MessageBox.Show(Failmsg, "Info Messange", MessageBoxButton.OK);
                                        return;
                                    }
                                }
                                else
                                {
                                    return;
                                }
                            }
                        }
                        else
                        {
                            //해당축에 정의된 CV 가 없다.
                            LogManager.WriteConsoleLog(eLogLevel.Info, "해당축이 정의된 CV 모듈이 없습니다.");
                            return;
                        }

                        ServoMgr[turnItem.Axis].AxisPositionMove(turnItem.PositionValue, (int)slider_JogSpeed.Value);
                    }
                }
                else
                {
                    System.Windows.MessageBox.Show("현재 Auto 상태입니다.메뉴얼로 변경하세요", "경고",MessageBoxButton.OK);
                    return;
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
            }
        }

        private void button_Stop_Click(object sender, RoutedEventArgs e)
        {
            ServoMgr.RequesetAllStop();
        }

        private void button_Refreash_Click(object sender, RoutedEventArgs e)
        {
            if (GlobalData.Current.MainBooth.BoothState != eBoothState.AutoStart)
            {
                string msg = string.Format("티칭 파일을 XML 파일에서 불러 오겠습니까?");
                MessageBoxResult result = System.Windows.MessageBox.Show(msg, "Confirmation", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    ServoMgr.LoadTeachingFile();
                    dataGrid_Teaching.ItemsSource = ServoMgr.TurnTeachingList;
                    dataGrid_Teaching.Items.Refresh();
                    cb_Axis_SelectionChanged(null, null);
                }
            }
            else
            {
                System.Windows.MessageBox.Show("현재 Auto 상태입니다.메뉴얼로 변경하세요", "경고", MessageBoxButton.OK);
                return;
            }
        }

        private void button_PositionSave_Click(object sender, RoutedEventArgs e)
        {

            try
            {
                if (dataGrid_Teaching.SelectedIndex < 0)
                    return;

                if (GlobalData.Current.MainBooth.BoothState != eBoothState.AutoStart)
                {
                    //현재 선택 축
                    int selectAxis = (int)cb_Axis.SelectedValue; 
                    int CurrentPosition = ServoMgr[selectAxis].CurrentPosition;
                    TurnTeachingItem turnItem = dataGrid_Teaching.SelectedCells[0].Item as TurnTeachingItem;

                    //현재 선택 축값과 티칭 리스트 축값 비교
                    if (selectAxis != turnItem.Axis)
                    {
                        System.Windows.MessageBox.Show("선택된 축 과 티칭 리스트 선택이 올바르지 않습니다.", "경고", MessageBoxButton.OK);
                        return;
                    }
                    string msg = string.Format("{0} 축 Value : {1} ====> {2} 축  PositionValue : {3}    Tag : {4}   에 업데이트 하시겠습니까?", selectAxis, CurrentPosition, turnItem.Axis, turnItem.PositionValue, turnItem.TagName);
                    MessageBoxResult result = System.Windows.MessageBox.Show(msg, "Confirmation", MessageBoxButton.YesNo);
                    if (result == MessageBoxResult.Yes)
                    {
                        turnItem.PositionValue = CurrentPosition;
                        dataGrid_Teaching.Items.Refresh();
                    }
                }
                else
                {
                    System.Windows.MessageBox.Show("현재 Auto 상태입니다.메뉴얼로 변경하세요", "경고", MessageBoxButton.OK);
                    return;
                }
            }
            catch(Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
            }
        }

        private void button_XMLSave_Click(object sender, RoutedEventArgs e)
        {
            if (GlobalData.Current.MainBooth.BoothState != eBoothState.AutoStart)
            {
                string msg = string.Format("티칭 리스트를 XML 파일에 저장하시겠습니까?");
                MessageBoxResult result = System.Windows.MessageBox.Show(msg, "Confirmation", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    TurnTeachingItemList TurnList = dataGrid_Teaching.ItemsSource as TurnTeachingItemList;
                    TurnTeachingItemList.Serialize(ServoMgr.GetServoTeachingFile(), TurnList);
                }
            }
            else
            {
                System.Windows.MessageBox.Show("현재 Auto 상태입니다.메뉴얼로 변경하세요", "경고", MessageBoxButton.OK);
                return;
            }
           
        }

        private void cb_Axis_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(string.IsNullOrEmpty(cb_Axis.Text))
            {
                return;
            }
            int selectAxis = (int)cb_Axis.SelectedValue; 
            dataGrid_Teaching.SelectedIndex = -1;
            foreach(TurnTeachingItem item in dataGrid_Teaching.Items)
            {
                if(item.Axis == selectAxis)
                {
                    item.IsSelected = true;
                }
                else
                {
                    item.IsSelected = false;
                }
            }
        }

        private void button_ServoReboot_Click(object sender, RoutedEventArgs e)
        {
            string msg = "Servo System Reboot 하시겠습니까?";
            MessageBoxResult result = System.Windows.MessageBox.Show(msg, "Command Check", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)  //서보 Off 시 On
            {
                ServoMgr.RequesetAllStop();
                if(ServoMgr.RequestRebootServoSystem())
                {
                     System.Windows.MessageBox.Show("서보 리부트에 성공하였습니다.", "서보 리부트 성공", MessageBoxButton.OK);
                }
                else
                {
                     System.Windows.MessageBox.Show("서보 리부트에 실패하였습니다.에러코드를 참고하세요.", "서보 리부트 실패", MessageBoxButton.OK);
                }
            }
        }
    }
   
 
}
