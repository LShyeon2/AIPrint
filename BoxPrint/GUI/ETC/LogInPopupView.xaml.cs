using BoxPrint.Log;
using BoxPrint.Modules.User;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;
using TranslationByMarkupExtension;
using System.IO;

namespace BoxPrint.GUI.ETC
{

    public partial class LogInPopupView : Window
    {
        public delegate void EventHandler_LoginChange();
        public static event EventHandler_LoginChange _EventHandler_LoginChange;
        //string AuthorityFilePath = null;

        public LogInPopupView()
        {
            InitializeComponent();

            //220525 HHJ SCS 개선     //- 로그인 화면 개선
            textboxUsername.Focus();

        }
        public void TryLogin(string User,string Password)
        {
            ResultUserControl userControlResult = GlobalData.Current.UserMng.Login(User, Password);

            if (userControlResult.bResult)
            {
                _EventHandler_LoginChange();
            }

        }
        private void UserMng_OnLoginUserChange(User usr)
        {
            _EventHandler_LoginChange();
        }

        //보드버튼 모음
        private void BorderButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border)
            {
                var senderBuffer = (Border)sender;

                switch (senderBuffer.Tag.ToString())
                {
                    case "EXIT":
                        this.Close();
                        break;

                    case "SIGNIN":
                        //220525 HHJ SCS 개선     //- 로그인 화면 개선
                        //bool istextCheckOK =  TextCheck();

                        //if (istextCheckOK)
                        //{
                        //    ResultUserControl userControlResult = GlobalData.Current.UserMng.Login(textboxUsername.Text, textboxPassword.Text);

                        //    if (!userControlResult.bResult)
                        //    {
                        //        MessageBoxPopupView.Show(userControlResult.strResult);
                        //        return;
                        //    }

                        //    this.Close();
                        //}
                        Login();
                        break;

                    default:
                        break;
                }
                string action = string.Empty;

                if (senderBuffer.Tag.ToString() == "EXIT")
                {
                    action = "CLOSE";
                }
                else
                {
                    action = "CLICK";
                }
                LogManager.WriteOperatorLog(string.Format("사용자가 {0} {1} 을/를 Click하였습니다.", "Login", senderBuffer.Tag.ToString()),
                    action, senderBuffer.Tag.ToString(), GlobalData.Current.CurrentIP, string.Empty, GlobalData.Current.ClientPCName, GlobalData.Current.CurrentUserID, 4,
                    "Login", senderBuffer.Tag.ToString());
            }
        }

        //텍스트 검사
        private bool TextCheck()
        {
            if (string.IsNullOrEmpty(textboxUsername.Text))
            {
                textboxHelpMessage.Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom("#FFEC685B");
                textboxHelpMessage.Text = TranslationManager.Instance.Translate("Please Enter Your User ID").ToString();
                return false;
            }
            //220525 HHJ SCS 개선     //- 로그인 화면 개선
            //else if (string.IsNullOrEmpty(textboxPassword.Text))
            else if (string.IsNullOrEmpty(textboxPassword.Password))
            {
                textboxHelpMessage.Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom("#FFEC685B");
                textboxHelpMessage.Text = TranslationManager.Instance.Translate("Please Enter Your Password").ToString();
                return false;
            }
            return true;
        }

        //220525 HHJ SCS 개선     //- 로그인 화면 개선
        private void Login()
        {
            bool istextCheckOK = TextCheck();

            if (istextCheckOK)
            {
                ResultUserControl userControlResult = GlobalData.Current.UserMng.Login(textboxUsername.Text, textboxPassword.Password);

                if (!userControlResult.bResult)
                {
                    MessageBoxPopupView.Show(userControlResult.strResult, MessageBoxImage.Stop, false);
                    return;
                }
                _EventHandler_LoginChange();
                
                #region 로그인 유저 권한 부여 삭제함
                ////230103 LoginUserAuthority TEST
                //GlobalData.Current.LoginUserAuthority.Clear();
                //if (MainWindow.checkLoginUserLevel != null)
                //{
                //    XmlDocument xdoc = new XmlDocument();
                //    //xdoc.Load(GlobalData.Current.CurrentFilePaths("") + GlobalData.Current.AuthorityFilePath);
                //    FileInfo File = new FileInfo(GlobalData.Current.CurrentFilePaths("") + GlobalData.Current.AuthorityFilePath);
                //    if (!File.Exists)
                //    {
                //        xdoc.Load(GlobalData.Current.FilePathChange(GlobalData.Current.CurrentFilePaths(""), GlobalData.Current.AuthorityFilePath));
                //    }
                //    else
                //    {
                //        xdoc.Load(GlobalData.Current.CurrentFilePaths("") + GlobalData.Current.AuthorityFilePath);
                //    }
                //    XmlNodeList nodes = xdoc.SelectNodes("/AuthorityLevels/AuthorityLevel");

                //    foreach (XmlNode item_AuthorityLevel in nodes)
                //    {
                //        if (MainWindow.checkLoginUserLevel == item_AuthorityLevel.Attributes["Level"].Value)
                //        {
                //            foreach (XmlNode item_Authority in item_AuthorityLevel)
                //            {
                //                //보기 권한 저장
                //                if (Convert.ToBoolean(item_Authority.Attributes["Read"].Value))
                //                {
                //                    string LoginUserAuthority = "Read" + item_Authority.Attributes["Name"].Value;
                //                    GlobalData.Current.LoginUserAuthority.Add(LoginUserAuthority);
                //                }
                //                //수정 권한 저장
                //                if (Convert.ToBoolean(item_Authority.Attributes["Modify"].Value))
                //                {
                //                    string LoginUserAuthority = "Modify" + item_Authority.Attributes["Name"].Value;
                //                    GlobalData.Current.LoginUserAuthority.Add(LoginUserAuthority);
                //                }
                //            }
                //        }
                //    }
                //}
                #endregion

                this.Close();
            }
        }

        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (sender is TextBox || sender is PasswordBox)
            {
                if (e.Key.Equals(Key.Enter))
                {
                    Login();
                }
            }
        }
    }
}
