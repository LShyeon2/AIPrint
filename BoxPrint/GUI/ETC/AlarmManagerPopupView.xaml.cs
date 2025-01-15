using BoxPrint.Alarm;
using BoxPrint.DataList;
using BoxPrint.GUI.UIControls;
using BoxPrint.GUI.Views;
using System;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using TranslationByMarkupExtension;

namespace BoxPrint.GUI.ETC
{

    public partial class AlarmManagerPopupView : Window, INotifyPropertyChanged, IDisposable
    {
        //이벤트 : AlarmManagerView 에서 선택된 알람 데이타를 받아서 셋팅한다 
        public delegate void EventHandler_AlarmManagerDataChange(AlarmData Message);
        //이벤트 : AlarmManagerView 에 팝업 생성 됬는지 상태를 보낸다
        public static event AlarmManagerView.EventHandler_PopupOpen _EventCall_PopupOpen;//이벤트  
        public static event AlarmManagerView.EventHandler_refreshDataGrid _EventCall_refreshDataGrid;       //220711 조숭진

        private string _currentType;

        private AlarmData alarmData = new AlarmData();        //220708 조숭진 알람 추가, 수정, 삭제를 위한 변수
        private AlarmData prevalarmData = new AlarmData();        //220708 조숭진 알람 추가, 수정, 삭제를 위한 변수

        private string _PathData;
        public string PathData
        {
            get { return _PathData; }
            set
            {
                if (_PathData != value)
                {
                    _PathData = value;
                    OnPropertyChanged("PathData");
                }
            }
        }

        //재산변경
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        //생성자
        public AlarmManagerPopupView(string rcvTag, AlarmData rcvAlarmData)
        {
            _EventCall_PopupOpen(true);//오픈 이벤트 발사

            InitializeComponent();
            InitializeDesign(rcvTag, rcvAlarmData);
            alarmData = rcvAlarmData;

            AlarmManagerView._EventCall_AlarmManagerDataChange += new EventHandler_AlarmManagerDataChange(this.SetData);
        }


        //파괴
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        //윈도우 닫기 
        private void windowMain_Closed(object sender, EventArgs e)
        {
            _EventCall_PopupOpen(false);//클로즈 이벤트 발사
            this.Dispose();
        }

        //디자인 초기화
        private void InitializeDesign(string rcvTag, AlarmData rcvAlarmData)
        {
            _currentType = rcvTag;

            switch (_currentType)
            {
                case "Add":
                    windowMain.Width = 800;
                    windowMain.Height = 750;
                    gridCenter1.Visibility = Visibility.Visible;
                    gridCenter2.Visibility = Visibility.Collapsed;
                    borderIconBackground.Background = Resources["SK_Blue"] as SolidColorBrush;
                    PathData = "M432 256c0 17.69-14.33 32.01-32 32.01H256v144c0 17.69-14.33 31.99-32 31.99s-32-14.3-32-31.99v-144H48c-17.67 0-32-14.32-32-32.01s14.33-31.99 32-31.99H192v-144c0-17.69 14.33-32.01 32-32.01s32 14.32 32 32.01v144h144C417.7 224 432 238.3 432 256z";
                    textblockPopupName.Text = TranslationManager.Instance.Translate("에러 리스트 추가").ToString();
                    ComboboxLightAlarm.SelectedIndex = 0;        //220711 조숭진 초기화
                    break;
                case "Modify":
                    windowMain.Width = 800;
                    windowMain.Height = 750;
                    gridCenter1.Visibility = Visibility.Visible;
                    gridCenter2.Visibility = Visibility.Collapsed;
                    SetData(rcvAlarmData);
                    borderIconBackground.Background = Resources["SK_Teal"] as SolidColorBrush;
                    PathData = "M362.7 19.32C387.7-5.678 428.3-5.678 453.3 19.32L492.7 58.75C517.7 83.74 517.7 124.3 492.7 149.3L444.3 197.7L314.3 67.72L362.7 19.32zM421.7 220.3L188.5 453.4C178.1 463.8 165.2 471.5 151.1 475.6L30.77 511C22.35 513.5 13.24 511.2 7.03 504.1C.8198 498.8-1.502 489.7 .976 481.2L36.37 360.9C40.53 346.8 48.16 333.9 58.57 323.5L291.7 90.34L421.7 220.3z";
                    textblockPopupName.Text = TranslationManager.Instance.Translate("에러 리스트 수정").ToString();
                    break;
                case "Delete":
                    windowMain.Width = 600;
                    windowMain.Height = 600;
                    gridCenter1.Visibility = Visibility.Collapsed;
                    gridCenter2.Visibility = Visibility.Visible;
                    SetData(rcvAlarmData);
                    borderIconBackground.Background = Resources["SK_Orange"] as SolidColorBrush;
                    PathData = "M400 288h-352c-17.69 0-32-14.32-32-32.01s14.31-31.99 32-31.99h352c17.69 0 32 14.3 32 31.99S417.7 288 400 288z";
                    textblockPopupName.Text = TranslationManager.Instance.Translate("에러 리스트 제거").ToString();
                    break;
                case "Export":
                    windowMain.Width = 600;
                    windowMain.Height = 600;
                    gridCenter1.Visibility = Visibility.Collapsed;
                    gridCenter2.Visibility = Visibility.Visible;
                    borderIconBackground.Background = Resources["SK_Purple"] as SolidColorBrush;
                    PathData = "M384 128h-128V0L384 128zM256 160H384v304c0 26.51-21.49 48-48 48h-288C21.49 512 0 490.5 0 464v-416C0 21.49 21.49 0 48 0H224l.0039 128C224 145.7 238.3 160 256 160zM255 295L216 334.1V232c0-13.25-10.75-24-24-24S168 218.8 168 232v102.1L128.1 295C124.3 290.3 118.2 288 112 288S99.72 290.3 95.03 295c-9.375 9.375-9.375 24.56 0 33.94l80 80c9.375 9.375 24.56 9.375 33.94 0l80-80c9.375-9.375 9.375-24.56 0-33.94S264.4 285.7 255 295z";
                    textblockPopupName.Text = TranslationManager.Instance.Translate("에러 리스트 내보내기").ToString();
                    break;
            }

        }

        //버튼 클릭 이벤트 통합
        private void SK_ButtonControl_Click(object sender, RoutedEventArgs e)
        {
            try //240404 RGJ 클릭 이벤트 예외 못잡으면 프로그램 다운 되므로 전부 예외처리함.
            {
                bool bcheck = false;

                if (sender is SK_ButtonControl senderBuffer)
                {

                    switch (_currentType)
                    {
                        case "Add":
                            bcheck = CheckData();
                            if (bcheck)
                            {
                                addAlarmData();
                                GlobalData.Current.DBManager.DbSetProcedureAlarmInfo(alarmData, false, "ID");
                            }
                            break;
                        case "Modify":
                            bcheck = CheckData();
                            if (bcheck)
                            {
                                prevalarmData = alarmData;
                                addAlarmData();
                                string target = CompareData(alarmData, prevalarmData);
                                GlobalData.Current.DBManager.DbSetProcedureAlarmInfo(alarmData, false, target);
                            }
                            break;
                        case "Delete":
                            if (alarmData != null)
                            {
                                GlobalData.Current.DBManager.DbSetProcedureAlarmInfo(alarmData, true, "ID");
                            }
                            break;
                        case "Export":

                            break;
                    }

                    if (_currentType != "Export")
                    {
                        //220711 조숭진 refresh후 자동 닫기
                        GlobalData.Current.Alarm_Manager.RefreshAllAlarmList(GlobalData.Current.ServerClientType, _currentType, alarmData, prevalarmData);
                        _EventCall_refreshDataGrid("All");

                        if (GlobalData.Current.ServerClientType == eServerClientType.Client)
                        {
                            ClientSetProcedure();
                        }
                    }
                    windowMain_Closed(this, EventArgs.Empty);
                    this.Close();
                }
            }
            catch(Exception ex)
            {
                Log.LogManager.WriteConsoleLog(eLogLevel.Info,ex.ToString());
            }
        }

        private void ClientSetProcedure()
        {
            ClientReqList reqBuffer = new ClientReqList
            {
                EQPID = GlobalData.Current.EQPID,
                CMDType = "Refresh",
                Target = "ALARM",
                TargetID = string.Empty,
                TargetValue = string.Empty,
                ReqTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                Requester = eServerClientType.Client,
                JobID = string.Empty,
            };

            GlobalData.Current.DBManager.DbSetProcedureClientReq(reqBuffer.EQPID, reqBuffer.CMDType, reqBuffer.Target, reqBuffer.TargetID, reqBuffer.TargetValue, reqBuffer.ReqTime, reqBuffer.Requester);
        }

        private string CompareData(AlarmData updateitem, AlarmData previtem)
        {
            if (updateitem.AlarmID == previtem.AlarmID)
            {
                return "ID";
            }
            else if (updateitem.AlarmName == previtem.AlarmName)
            {
                return "NAME";
            }
            else if (updateitem.IsLightAlarm == previtem.IsLightAlarm)
            {
                return "LIGHT";
            }
            else if (updateitem.ModuleType == previtem.ModuleType)
            {
                return "TYPE";
            }
            else if (updateitem.Description == previtem.Description ||
                     updateitem.Description_ENG == previtem.Description_ENG ||
                     updateitem.Description_CHN == previtem.Description_CHN ||
                     updateitem.Description_HUN == previtem.Description_HUN)
            {
                return "DESC";
            }
            else
            {
                return "SOLUTION";
            }
        }

        private void addAlarmData()
        {
            prevalarmData = alarmData;
            alarmData = new AlarmData();

            alarmData.AlarmID = textboxID.Text;
            alarmData.ModuleType = textboxModuleType.Text;
            alarmData.AlarmName = textboxName.Text;
            alarmData.IsLightAlarm = Convert.ToBoolean(ComboboxLightAlarm.SelectedIndex);

            if (_currentType == "Modify")
            {
                switch (TranslationManager.Instance.CurrentLanguage.ToString())
                {
                    case "ko-KR":
                        alarmData.Description = textboxDescription.Text;
                        alarmData.Description_ENG = prevalarmData.Description_ENG;
                        alarmData.Description_CHN = prevalarmData.Description_CHN;
                        alarmData.Description_HUN = prevalarmData.Description_HUN;
                        alarmData.Solution = textboxSolution.Text;
                        alarmData.Solution_ENG = prevalarmData.Solution_ENG;
                        alarmData.Solution_CHN = prevalarmData.Solution_CHN;
                        alarmData.Solution_HUN = prevalarmData.Solution_HUN;
                        break;
                    case "en-US":
                        alarmData.Description = prevalarmData.Description;
                        alarmData.Description_ENG = textboxDescription.Text;
                        alarmData.Description_CHN = prevalarmData.Description_CHN;
                        alarmData.Description_HUN = prevalarmData.Description_HUN;
                        alarmData.Solution = prevalarmData.Solution;
                        alarmData.Solution_ENG = textboxSolution.Text;
                        alarmData.Solution_CHN = prevalarmData.Solution_CHN;
                        alarmData.Solution_HUN = prevalarmData.Solution_HUN;
                        break;
                    case "zh-CN":
                        alarmData.Description = prevalarmData.Description;
                        alarmData.Description_ENG = prevalarmData.Description_ENG;
                        alarmData.Description_CHN = textboxDescription.Text;
                        alarmData.Description_HUN = prevalarmData.Description_HUN;
                        alarmData.Solution = prevalarmData.Solution;
                        alarmData.Solution_ENG = prevalarmData.Solution_ENG;
                        alarmData.Solution_CHN = textboxSolution.Text;
                        alarmData.Solution_HUN = prevalarmData.Solution_HUN;
                        break;
                    case "hu-HU":
                        alarmData.Description = prevalarmData.Description;
                        alarmData.Description_ENG = prevalarmData.Description_ENG;
                        alarmData.Description_CHN = prevalarmData.Description_CHN;
                        alarmData.Description_HUN = textboxDescription.Text;
                        alarmData.Solution = prevalarmData.Solution;
                        alarmData.Solution_ENG = prevalarmData.Solution_ENG;
                        alarmData.Solution_CHN = prevalarmData.Description_CHN;
                        alarmData.Solution_HUN = textboxSolution.Text;
                        break;
                }
            }
            else
            {
                switch (TranslationManager.Instance.CurrentLanguage.ToString())
                {
                    case "ko-KR":
                        alarmData.Description     = textboxDescription.Text;
                        alarmData.Description_ENG = textboxDescription.Text;
                        alarmData.Description_CHN = textboxDescription.Text;
                        alarmData.Description_HUN = textboxDescription.Text;
                        alarmData.Solution     = textboxSolution.Text;
                        alarmData.Solution_ENG = textboxSolution.Text;
                        alarmData.Solution_CHN = textboxSolution.Text;
                        alarmData.Solution_HUN = textboxSolution.Text;
                        break;
                    case "en-US":
                        alarmData.Description     = textboxDescription.Text;
                        alarmData.Description_ENG = textboxDescription.Text;
                        alarmData.Description_CHN = textboxDescription.Text;
                        alarmData.Description_HUN = textboxDescription.Text;
                        alarmData.Solution     = textboxSolution.Text;
                        alarmData.Solution_ENG = textboxSolution.Text;
                        alarmData.Solution_CHN = textboxSolution.Text;
                        alarmData.Solution_HUN = textboxSolution.Text;
                        break;
                    case "zh-CN":
                        alarmData.Description     = textboxDescription.Text;
                        alarmData.Description_ENG = textboxDescription.Text;
                        alarmData.Description_CHN = textboxDescription.Text;
                        alarmData.Description_HUN = textboxDescription.Text;
                        alarmData.Solution     = textboxSolution.Text;
                        alarmData.Solution_ENG = textboxSolution.Text;
                        alarmData.Solution_CHN = textboxSolution.Text;
                        alarmData.Solution_HUN = textboxSolution.Text;
                        break;
                    case "hu-HU":
                        alarmData.Description     = textboxDescription.Text;
                        alarmData.Description_ENG = textboxDescription.Text;
                        alarmData.Description_CHN = textboxDescription.Text;
                        alarmData.Description_HUN = textboxDescription.Text;
                        alarmData.Solution     = textboxSolution.Text;
                        alarmData.Solution_ENG = textboxSolution.Text;
                        alarmData.Solution_CHN = textboxSolution.Text;
                        alarmData.Solution_HUN = textboxSolution.Text;
                        break;
                }
            }

            //alarmData.Description = textboxDescription.Text;
            //alarmData.Solution = textboxSolution.Text;
        }

        //에러 데이타 정상 입력 상태 확인
        private bool CheckData()
        {
            textboxModuleType.BorderBrush = Brushes.Black;
            textboxID.BorderBrush = Brushes.Black;
            textboxName.BorderBrush = Brushes.Black;
            ComboboxLightAlarm.BorderBrush = Brushes.Black;
            textboxDescription.BorderBrush = Brushes.Black;
            textboxSolution.BorderBrush = Brushes.Transparent;

            if (string.IsNullOrEmpty(textboxModuleType.Text.ToString()))
            {
                textboxModuleType.BorderBrush = Resources["SK_Red"] as SolidColorBrush;
                textblockInformation.Text = TranslationManager.Instance.Translate("'모듈타입'이 비어 있습니다.").ToString();
                return false;
            }
            if (string.IsNullOrEmpty(textboxID.Text.ToString()))
            {
                textboxID.BorderBrush = Resources["SK_Red"] as SolidColorBrush;
                textblockInformation.Text = TranslationManager.Instance.Translate("'에러코드'가 비어 있습니다.").ToString();
                return false;
            }
            if (string.IsNullOrEmpty(textboxName.Text.ToString()))
            {
                textboxName.BorderBrush = Resources["SK_Red"] as SolidColorBrush;
                textblockInformation.Text = TranslationManager.Instance.Translate("'에러이름'이 비어 있습니다.").ToString();
                return false;
            }
            if (string.IsNullOrEmpty(ComboboxLightAlarm.SelectedItem.ToString()))
            {
                ComboboxLightAlarm.BorderBrush = Resources["SK_Red"] as SolidColorBrush;
                textblockInformation.Text = TranslationManager.Instance.Translate("'에러레벨'이 비어 있습니다.").ToString();
                return false;
            }
            if (string.IsNullOrEmpty(textboxDescription.Text.ToString()))
            {
                textboxDescription.BorderBrush = Resources["SK_Red"] as SolidColorBrush;
                textblockInformation.Text = TranslationManager.Instance.Translate("'에러메시지'가 비어 있습니다.").ToString();
                return false;
            }
            if (string.IsNullOrEmpty(textboxSolution.Text.ToString()))
            {
                textboxSolution.BorderBrush = Resources["SK_Red"] as SolidColorBrush;
                textblockInformation.Text = TranslationManager.Instance.Translate("'조치방법'이 비어 있습니다.").ToString();
                return false;
            }
            return true;
        }

        //데이타 넣기
        private void SetData(AlarmData rcvAlarmData)
        {
            textblockInformation.Text = "";
            string msg = string.Empty;

            if (rcvAlarmData == null)
            {
                textblockInformation.Text = TranslationManager.Instance.Translate("선택된 Alarm Data가 없습니다.").ToString();
                return;
            }


            if (gridCenter1.Visibility == Visibility.Visible)
            {
                textboxModuleType.Text = rcvAlarmData.ModuleType;
                textboxID.Text = rcvAlarmData.AlarmID;
                textboxName.Text = rcvAlarmData.AlarmName;

                ComboboxLightAlarm.SelectedIndex = rcvAlarmData.IsLightAlarm ? 1 : 0;

                switch (TranslationManager.Instance.CurrentLanguage.ToString())
                {
                    case "ko-KR":
                        textboxDescription.Text = rcvAlarmData.Description;
                        textboxSolution.Text = rcvAlarmData.Solution;
                        break;
                    case "en-US":
                        textboxDescription.Text = rcvAlarmData.Description_ENG;
                        textboxSolution.Text = rcvAlarmData.Solution_ENG;
                        break;
                    case "zh-CN":
                        textboxDescription.Text = rcvAlarmData.Description_CHN;
                        textboxSolution.Text = rcvAlarmData.Solution_CHN;
                        break;
                    case "hu-HU":
                        textboxDescription.Text = rcvAlarmData.Description_HUN;
                        textboxSolution.Text = rcvAlarmData.Solution_HUN;
                        break;
                }
                //textboxDescription.Text = rcvAlarmData.Description;
                //textboxSolution.Text = rcvAlarmData.Solution;
            }
            else if (gridCenter2.Visibility == Visibility.Visible)
            {
                msg = TranslationByMarkupExtension.TranslationManager.Instance.Translate("을 삭제 하시겠습니까?").ToString();
                textblockMessage.Text = string.Format(msg, "\n" + rcvAlarmData.AlarmName.ToString() + "\n" + "( ID : " + rcvAlarmData.AlarmID.ToString() + " )");
            }

        }

        //텍스트 박스 숫자만 쓰게 하기
        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
    }
}