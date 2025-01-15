//220324 HHJ SCS 개발     //- 확인 창, 입력창 추가
//CustomMessageBox 추가
using BoxPrint.GUI.UIControls;
using BoxPrint.Log;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using TranslationByMarkupExtension;

namespace BoxPrint.GUI.ETC
{
    internal partial class MessageBoxPopupView : Window
    {
        #region internal Methods
        internal string Message
        {
            get
            {
                return TextBlock_Message1.Text;
            }
            set
            {
                TextBlock_Message1.Text = value;
            }
        }

        internal string MessageHead { get; set; }
        internal string MessageValue
        {
            get
            {
                return TextBlock_Message2.Text;
            }
            set
            {
                TextBlock_Message2.Text = value;
            }
        }

        internal string InputItemName
        {
            get
            {
                return lbl_InputItemName.Content.ToString();
            }
            set
            {
                lbl_InputItemName.Content = value;
            }
        }

        private string combinedtext1 = string.Empty;
        private string combinedtext2 = string.Empty;
        private string combinedtext3 = string.Empty;
        private string combinedtext4 = string.Empty;
        private bool combinedmsg = false;

        private MessageBoxButton MessageBoxButtonStyle;
        public MessageBoxResult Result { get; set; }

        internal MessageBoxPopupView(string message, MessageBoxImage boxIcon = MessageBoxImage.None, bool rcvisComplete = true)
        {
            InitializeComponent();

            Message = message;
            grdInput.Visibility = Visibility.Collapsed;
            TextBlock_MessageHead.Visibility = Visibility.Collapsed;
            TextBlock_Message2.Visibility = Visibility.Collapsed;
            TextBlock_Message3.Visibility = Visibility.Collapsed;
            DisplayButtons(MessageBoxButton.OK, boxIcon, rcvisComplete);
        }

        internal MessageBoxPopupView(string message, MessageBoxButton button, MessageBoxImage boxIcon = MessageBoxImage.None, bool rcvisComplete = true)
        {
            InitializeComponent();

            Message = message;

            grdInput.Visibility = Visibility.Collapsed;
            TextBlock_MessageHead.Visibility = Visibility.Collapsed;
            TextBlock_Message2.Visibility = Visibility.Collapsed;
            TextBlock_Message3.Visibility = Visibility.Collapsed;
            DisplayButtons(button, boxIcon, rcvisComplete);
        }

        internal MessageBoxPopupView(string message, string inputitem, MessageBoxButton button, MessageBoxImage boxIcon = MessageBoxImage.None)
        {
            InitializeComponent();

            Message = message;

            grdInput.Visibility = Visibility.Visible;
            lbl_InputItemName.Content = inputitem;
            TextBlock_Message2.Visibility = Visibility.Collapsed;
            TextBlock_Message3.Visibility = Visibility.Collapsed;
            DisplayButtons(button, boxIcon);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="messageHead"></param>
        /// <param name="message1"></param>
        /// <param name="message2"></param>
        /// <param name="message3"></param>
        /// <param name="button"></param>
        /// <param name="boxIcon"></param>
        /// <param name="text1">리소스언어에서 조합되는 이름</param>
        /// <param name="text2">조합되어야하는 text1</param>
        /// <param name="text3">조합되어야하는 text2</param>
        /// <param name="bCombined">조합여부</param>
        /// <param name="rcvisComplete"></param>
        internal MessageBoxPopupView(string messageHead, string message1, string message2, string message3, MessageBoxButton button, MessageBoxImage boxIcon = MessageBoxImage.None, string text1 = "", string text2 = "", string text3 = "", bool bCombined = false, bool rcvisComplete = true)
        {
            InitializeComponent();

            MessageHead = messageHead;
            MessageValue = message2;

            if (bCombined)
            {
                combinedtext1 = text1;
                combinedtext2 = text2;
                combinedtext3 = text3;
                if (!string.IsNullOrEmpty(message1))
                    combinedtext4 = message1;
                combinedmsg = bCombined;
            }

            if (bCombined && !string.IsNullOrEmpty(message1))
            {
                //Message = messageHead;
                //TextBlock_MessageHead.Visibility = string.IsNullOrEmpty(combinedtext1) ? Visibility.Collapsed : Visibility.Visible;
                //TextBlock_Message1.Visibility = string.IsNullOrEmpty(combinedtext2) ? Visibility.Collapsed : Visibility.Visible;
                //TextBlock_Message2.Visibility = string.IsNullOrEmpty(combinedtext3) ? Visibility.Collapsed : Visibility.Visible;
                //TextBlock_Message3.Visibility = string.IsNullOrEmpty(combinedtext4) ? Visibility.Collapsed : Visibility.Visible;

                //TextBlock_MessageHead.Text = combinedtext1;
                //TextBlock_Message1.Text = combinedtext2;
                //TextBlock_Message2.Text = combinedtext3;
                //TextBlock_Message3.Text = combinedtext4;

                Message = messageHead;
                //TextBlock_MessageHead.Visibility = string.IsNullOrEmpty(messageHead) ? Visibility.Collapsed : Visibility.Visible;
                TextBlock_MessageHead.Visibility = Visibility.Collapsed;
                TextBlock_Message1.Visibility = string.IsNullOrEmpty(Message) ? Visibility.Collapsed : Visibility.Visible;
                TextBlock_Message2.Visibility = string.IsNullOrEmpty(message2) ? Visibility.Collapsed : Visibility.Visible;
                TextBlock_Message3.Visibility = string.IsNullOrEmpty(message3) ? Visibility.Collapsed : Visibility.Visible;

                TextBlock_MessageHead.Text = messageHead;
                TextBlock_Message1.Text = messageHead;
                TextBlock_Message2.Text = message2;
                TextBlock_Message3.Text = message3;
            }
            else
            {
                TextBlock_MessageHead.Visibility = string.IsNullOrEmpty(messageHead) ? Visibility.Collapsed : Visibility.Visible;
                TextBlock_Message1.Visibility = string.IsNullOrEmpty(message1) ? Visibility.Collapsed : Visibility.Visible;
                TextBlock_Message2.Visibility = string.IsNullOrEmpty(message2) ? Visibility.Collapsed : Visibility.Visible;
                TextBlock_Message3.Visibility = string.IsNullOrEmpty(message3) ? Visibility.Collapsed : Visibility.Visible;

                TextBlock_MessageHead.Text = messageHead;
                TextBlock_Message1.Text = message1;
                TextBlock_Message2.Text = message2;
                TextBlock_Message3.Text = message3;
            }

            grdInput.Visibility = Visibility.Collapsed;

            DisplayButtons(button, boxIcon, rcvisComplete);
        }
        #endregion

        private void DisplayButtons(MessageBoxButton button, MessageBoxImage boxIcon, bool rcvisComplete = true)
        {
            switch ((int)boxIcon)
            {
                case 0: //None
                    MainIcon.Visibility = Visibility.Collapsed;
                    break;
                case 16://Hand, Stop, Error
                    MainIcon.Tag = "X";
                    break;
                case 32://Question
                    MainIcon.Tag = "?";
                    break;
                case 48://Exclamation, Warning
                    MainIcon.Tag = "!";
                    break;
                case 64://Asterisk, Information
                    MainIcon.Tag = "i";
                    break;
            }


            MessageBoxButtonStyle = button;
            switch (button)
            {
                case MessageBoxButton.OKCancel:
                    Button_Yes.Visibility = Visibility.Visible;
                    Button_Yes.Content = TranslationManager.Instance.Translate("OK").ToString();
                    Button_No.Visibility = Visibility.Visible;
                    Button_No.Content = TranslationManager.Instance.Translate("CANCEL").ToString();
                    break;

                case MessageBoxButton.YesNo:
                case MessageBoxButton.YesNoCancel:
                    Button_Yes.Visibility = Visibility.Visible;
                    Button_Yes.Content = TranslationManager.Instance.Translate("YES").ToString();
                    Button_No.Visibility = Visibility.Visible;
                    Button_No.Content = TranslationManager.Instance.Translate("NO").ToString();
                    break;

                default:
                    if(rcvisComplete)
                    {
                        Button_Yes.Visibility = Visibility.Visible;
                        Button_Yes.Content = TranslationManager.Instance.Translate("OK").ToString();
                        Button_No.Visibility = Visibility.Collapsed;
                        grdbutton.ColumnDefinitions.RemoveAt(1);
                    }
                    else
                    {
                        Button_No.Visibility = Visibility.Visible;
                        Button_No.Content = TranslationManager.Instance.Translate("OK").ToString();
                        Button_Yes.Visibility = Visibility.Collapsed;
                        grdbutton.ColumnDefinitions.RemoveAt(1);
                    }

                    break;
            }
        }

        //SuHwan_20220817 : 버튼 통합
        private void SK_ButtonControl_Click(object sender, RoutedEventArgs e)
        {
            try //240404 RGJ 클릭 이벤트 예외 못잡으면 프로그램 다운 되므로 전부 예외처리함.
            {
                if (sender is SK_ButtonControl senderBuffer)
                {
                    var positionBuffer = Mouse.GetPosition(cnvPaper);
                    Canvas.SetLeft(pathCircle, positionBuffer.X);
                    Canvas.SetTop(pathCircle, positionBuffer.Y);

                    MainBorder.IsHitTestVisible = false;

                    pathCircle.Fill = senderBuffer.Background;
                    pathComplete.Opacity = 0;
                    pathComplete.Tag = senderBuffer.Tag.ToString();
                    var storyboardBuffer = (FindResource("storyboarCircleScale") as Storyboard);
                    storyboardBuffer.Completed += Storyboard_Completed;
                    storyboardBuffer.Begin();


                    switch (senderBuffer.Tag.ToString())
                    {
                        case "YES":
                            switch (MessageBoxButtonStyle)
                            {
                                case MessageBoxButton.OKCancel:
                                    Result = MessageBoxResult.OK;
                                    break;

                                default:
                                    Result = MessageBoxResult.Yes;
                                    break;
                            }
                            break;

                        case "NO":
                            Result = MessageBoxResult.No;
                            break;

                        case "CANCEL":
                            Result = MessageBoxResult.Cancel;
                            break;
                    }

                    if (string.IsNullOrEmpty(MessageHead))
                    {
                        if (Message.Contains("\n"))
                        {
                            Message = Message.Replace("\n", "");
                        }

                        if (Message.Contains("사용자 로그인"))
                        {
                            string username = Message.Substring(0, Message.IndexOf("사용자"));
                            string tempmsg = Message.Substring(Message.IndexOf("사용자"));

                            LogManager.WriteOperatorLog(string.Format("사용자가 {0} 에서 {1} 을/를 Click하였습니다.", Message, senderBuffer.Tag.ToString()),
                                "BUTTON", senderBuffer.Tag.ToString(), GlobalData.Current.CurrentIP, string.Empty, GlobalData.Current.ClientPCName, GlobalData.Current.CurrentUserID, 6,
                                username, tempmsg, senderBuffer.Tag.ToString());
                        }
                        else
                        {
                            LogManager.WriteOperatorLog(string.Format("사용자가 {0} 에서 {1} 을/를 Click하였습니다.", Message, senderBuffer.Tag.ToString()),
                                "BUTTON", senderBuffer.Tag.ToString(), GlobalData.Current.CurrentIP, string.Empty, GlobalData.Current.ClientPCName, GlobalData.Current.CurrentUserID, 5,
                                Message, senderBuffer.Tag.ToString());
                        }
                    }
                    else
                    {
                        if (combinedmsg)
                        {
                            if (string.IsNullOrEmpty(combinedtext4))
                            {
                                LogManager.WriteOperatorLog(string.Format("사용자가 {0} {1} {2} 을/를 Click하였습니다.", MessageHead, MessageValue, senderBuffer.Tag.ToString()),
                                    "BUTTON", senderBuffer.Tag.ToString(), GlobalData.Current.CurrentIP, string.Empty, GlobalData.Current.ClientPCName, GlobalData.Current.CurrentUserID, 6,
                                    MessageHead, combinedtext1, combinedtext2, combinedtext3, senderBuffer.Tag.ToString());
                            }
                            else
                            {
                                LogManager.WriteOperatorLog(string.Format("사용자가 {0} 에서 {1} 을/를 Click하였습니다.", Message, senderBuffer.Tag.ToString()),
                                    "BUTTON", senderBuffer.Tag.ToString(), GlobalData.Current.CurrentIP, string.Empty, GlobalData.Current.ClientPCName, GlobalData.Current.CurrentUserID, 5,
                                    combinedtext4, combinedtext1, combinedtext2, combinedtext3, senderBuffer.Tag.ToString());
                            }
                            combinedmsg = false;
                        }
                        else
                        {
                            LogManager.WriteOperatorLog(string.Format("사용자가 {0} {1} {2} 을/를 Click하였습니다.", MessageHead, MessageValue, senderBuffer.Tag.ToString()),
                                "BUTTON", senderBuffer.Tag.ToString(), GlobalData.Current.CurrentIP, string.Empty, GlobalData.Current.ClientPCName, GlobalData.Current.CurrentUserID, 6,
                                MessageHead, MessageValue, senderBuffer.Tag.ToString());
                        }

                        MessageHead = string.Empty;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
            }

        }

        private void Storyboard_Completed(object sender, EventArgs e)
        {
            Close();
        }

        public CustomMessageBoxResult ShowResult()
        {
            this.ShowDialog();
            CustomMessageBoxResult mBoxResult = new CustomMessageBoxResult()
            {
                Result = this.Result,
                InputResult = txt_InputItemName.Text,
            };
            return mBoxResult;
        }

        public static MessageBoxResult Show(string message, MessageBoxImage boxIcon = MessageBoxImage.None, bool rcvisComplete = true)
        {
            MessageBoxPopupView mbox = new MessageBoxPopupView(message, boxIcon, rcvisComplete);
            mbox.ShowDialog();

            return mbox.Result;
        }

        public static MessageBoxResult Show(string message, MessageBoxButton button, MessageBoxImage boxIcon = MessageBoxImage.None, bool rcvisComplete = true)
        {
            MessageBoxPopupView mbox = new MessageBoxPopupView(message, button, boxIcon, rcvisComplete);
            mbox.ShowDialog();

            return mbox.Result;
        }

        public static MessageBoxResult Show(string messageHead, string message1, string message2, string message3, MessageBoxButton button, MessageBoxImage boxIcon = MessageBoxImage.None, string text1 = "", string text2 = "", string text3 = "", bool bCombined = false, bool rcvisComplete = true)
        {
            MessageBoxPopupView mbox = new MessageBoxPopupView(messageHead, message1, message2, message3, button, boxIcon, text1, text2, text3, bCombined, rcvisComplete);
            mbox.ShowDialog();

            return mbox.Result;
        }

        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key.Equals(Key.Enter) || e.Key.Equals(Key.Space))
                SK_ButtonControl_Click(Button_Yes, null);
            else if (e.Key.Equals(Key.Escape))
            {
                if (Button_No.IsVisible)
                    SK_ButtonControl_Click(Button_No, null);
            }
        }
    }

    public class CustomMessageBoxResult
    {
        public MessageBoxResult Result;
        public string InputResult;
    }
}
