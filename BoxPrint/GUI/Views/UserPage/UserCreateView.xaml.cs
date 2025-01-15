using BoxPrint.GUI.ETC;
using BoxPrint.Log;
using BoxPrint.Modules.User;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using TranslationByMarkupExtension;

namespace BoxPrint.GUI.Views.UserPage
{
    /// <summary>
    /// UserCreateView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class UserCreateView : Page
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private static object objLock = new object();
        protected virtual void RaisePropertyChanged(string propertyName)
        {
            lock (objLock)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        private string _ViewTitle;
        public string ViewTitle
        {
            get { return _ViewTitle; }
            set
            {
                _ViewTitle = value;
                RaisePropertyChanged("ViewTitle");
            }
        }
        private string _UserName;
        public string UserName
        {
            get { return _UserName; }
            set
            {
                _UserName = value;
                RaisePropertyChanged("UserName");
            }
        }
        private string _UserID;
        public string UserID
        {
            get { return _UserID; }
            set
            {
                _UserID = value;
                RaisePropertyChanged("UserID");
            }
        }
        private string _UserPW;
        public string UserPW
        {
            get { return _UserPW; }
            set
            {
                _UserPW = value;
                RaisePropertyChanged("UserPW");
            }
        }

        protected int _UIFontSize_Large = 14;  //큰폰트
        public int UIFontSize_Large
        {
            get => _UIFontSize_Large;
            set
            {
                if (_UIFontSize_Large == value) return;
                _UIFontSize_Large = value;
                RaisePropertyChanged("UIFontSize_Large");
            }
        }
        protected int _UIFontSize_Medium = 12; //중간폰트
        public int UIFontSize_Medium
        {
            get => _UIFontSize_Medium;
            set
            {
                if (_UIFontSize_Medium == value) return;
                _UIFontSize_Medium = value;

                RaisePropertyChanged("UIFontSize_Medium");
            }
        }
        protected int _UIFontSize_Small = 10;  //작은폰트
        public int UIFontSize_Small
        {
            get => _UIFontSize_Small;
            set
            {
                if (_UIFontSize_Small == value) return;
                _UIFontSize_Small = value;

                RaisePropertyChanged("UIFontSize_Small");
            }
        }

        protected double _BodyTextSize = 12;
        public double BodyTextSize
        {
            get => _BodyTextSize;
            set
            {
                if (_BodyTextSize == value) return;
                _BodyTextSize = value;

                RaisePropertyChanged("BodyTextSize");
            }
        }

        private User ResultUser = null;
        private bool bResult = false;
        private bool bedit = false;

        public UserCreateView(string caption, User usr = null)
        {
            InitializeComponent();
            DataContext = this;
            AutoLogoutTime_updown.InitNumericUpDown(User.DefalutAutoLogoutTime);

            ViewTitle = TranslationManager.Instance.Translate(caption).ToString();
            if (!(usr is null))
            {
                //수정의 경우는 레벨, 비밀번호만 수정되게 해야하나?
                //일단 전부다 수정되는걸로 한다. 특정 부분만 수정이 필요하면 텍스트박스 이네이블을 변경해줘야함.
                //화면에는 유저 이름과 아이디만 띄워주고 비밀번호는 보여주지않음.
                UserID = usr.UserID;
                //UserPW = usr.UserPassword;
                UserName = usr.UserName;
                cbbLevel.SelectedItem = usr.UserLevel;
                AutoLogoutTime_updown.Value = usr.AutoLogoutMinute;
                cbbUse.SelectedItem = usr.UserUse;
            }
            //cbbLevel.Items.Add("=== 선택 ===");
            cbbLevel.ItemsSource = Enum.GetValues(typeof(eUserLevel));
            //AutoLogoutTime_updown.InitNumericUpDown(User.DefalutAutoLogoutTime);             //2024.09.08 lim, User 데이터가 있을때 user 데이터 사용 디폴트 설정은 위에서

            if (caption == "User Edit")
            {
                cbbUse.Items.Clear();
                UseGrd.Visibility = Visibility.Visible;
                gridCenter1.Visibility = Visibility.Collapsed;
                gridCenter2.Visibility = Visibility.Visible;
                cbbUse.Items.Add(true);
                cbbUse.Items.Add(false);
                bedit = true;
            }
            else
            {
                cbbUse.Items.Clear();
                UseGrd.Visibility = Visibility.Hidden;
                bedit = false;
            }
        }

        public CustomUserCreatenEditResult1 UserCreatenEdit()
        {
            //this.ShowDialog();
            CustomUserCreatenEditResult1 mResult = new CustomUserCreatenEditResult1()
            {
                Result = this.bResult,
                ResultUser = ResultUser,
            };
            return mResult;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try //240404 RGJ 클릭 이벤트 예외 못잡으면 프로그램 다운 되므로 전부 예외처리함.
            {
                User userdata;
                ResultUserControl userControlResult = null;
                if (sender is Button btn)
                {
                    string buttonname = string.Empty;

                    switch (btn.Tag.ToString())
                    {
                        case "Confirm":
                            if (string.IsNullOrEmpty(UserName) || string.IsNullOrEmpty(UserID) ) //241209 RGJ admin 계정으로 수정할때 패스워드 없이 수정 가능하게 요청
                            {
                                MessageBoxPopupView.Show(TranslationManager.Instance.Translate("Input Data is Abnormal").ToString(), MessageBoxImage.Stop, false);
                                return;
                            }

                            if (Enum.TryParse(cbbLevel.Text, out eUserLevel lvl))
                            {
                                if (bedit)
                                {
                                    if (string.IsNullOrEmpty(cbbUse.Text))
                                    {
                                        MessageBoxPopupView.Show(TranslationManager.Instance.Translate("Select Use is Abnormal").ToString(), MessageBoxImage.Stop, false);
                                        return;
                                    }
                                    //유저 수정인데 패스워드가 공백이면 다른 부분만 수정한것이므로 기존패스워드를 유지해야함.
                                    if(string.IsNullOrEmpty(UserPW))
                                    {
                                        User EditUser = GlobalData.Current.UserMng.GetUserByID(UserID); //기존 패스워드값을 가져온다.
                                        userdata = new User(UserName, UserID, EditUser.UserPassword, lvl, Convert.ToBoolean(cbbUse.Text), true); //241209 RGJ admin 계정으로 수정할때 패스워드 없이 수정 가능하게 요청
                                    }
                                    else
                                    {
                                        userdata = new User(UserName, UserID, UserPW, lvl, Convert.ToBoolean(cbbUse.Text));
                                    }



                                    userdata.SetAutoLogoutMinute(AutoLogoutTime_updown.Value);
                                    ResultUser = userdata;
                                    bResult = true;
                                    userControlResult = GlobalData.Current.UserMng.UpdateUser(ResultUser);
                                    buttonname = "User Edit";
                                }
                                else
                                {
                                    userdata = new User(UserName, UserID, UserPW, lvl, true);
                                    userdata.SetAutoLogoutMinute(AutoLogoutTime_updown.Value);
                                    ResultUser = userdata;
                                    bResult = true;
                                    userControlResult = GlobalData.Current.UserMng.AddUser(ResultUser);
                                    buttonname = "User Create";
                                }
                            }
                            else
                            {
                                MessageBoxPopupView.Show(TranslationManager.Instance.Translate("Select Level is Abnormal").ToString(), MessageBoxImage.Stop, false);
                                return;
                            }

                            UserAccountManagementPage.Current.InitDatagridView();
                            if (userControlResult != null)
                            {
                                MessageBoxPopupView.Show(userControlResult.strResult, MessageBoxImage.Information);
                            }
                            break;
                        case "Cancel":
                            bResult = false;
                            ResultUser = null;
                            if (bedit)
                            {
                                buttonname = "User Edit";
                            }
                            else
                            {
                                buttonname = "User Create";
                            }
                            break;
                    }

                    LogManager.WriteOperatorLog(string.Format("사용자가 {0} {1} 을/를 Click하였습니다.", buttonname, btn.Tag.ToString()),
                        "CLICK", btn.Tag.ToString().ToUpper(), GlobalData.Current.CurrentIP, string.Empty, GlobalData.Current.ClientPCName, GlobalData.Current.CurrentUserID, 4,
                        buttonname, btn.Tag.ToString());

                    clear();
                }
            }
            catch (Exception ex)
            {
                Log.LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
            }
            
        }

        private void clear()
        {
            UserID_Textbox.Text = null;
            Password_Textbox.Text = null;
            UserNamge_Textbox.Text = null;
            cbbLevel.Text = string.Empty;
            cbbUse.Text = string.Empty;
            AutoLogoutTime_updown.Value = User.DefalutAutoLogoutTime;
        }

    }

    public class CustomUserCreatenEditResult1
    {
        public bool Result;
        public User ResultUser;
    }
}
