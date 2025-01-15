using BoxPrint.Modules.User;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using TranslationByMarkupExtension;

namespace BoxPrint.GUI.ETC
{
    /// <summary>
    /// Interaction logic for UserCreatePopupView.xaml
    /// </summary>
    public partial class UserCreatePopupView : Window, INotifyPropertyChanged
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

        private User ResultUser = null;
        private bool bResult = false;
        private bool bedit = false;

        public UserCreatePopupView(string caption, User usr = null)
        {
            InitializeComponent();
            DataContext = this;

            ViewTitle = TranslationManager.Instance.Translate(caption).ToString();
            if (!(usr is null))
            {
                //수정의 경우는 레벨, 비밀번호만 수정되게 해야하나?
                //일단 전부다 수정되는걸로 한다. 특정 부분만 수정이 필요하면 텍스트박스 이네이블을 변경해줘야함.
                //화면에는 유저 이름과 아이디만 띄워주고 비밀번호는 보여주지않음.
                UserName = usr.UserName;
                UserID = usr.UserID;
            }

            cbbLevel.ItemsSource = Enum.GetValues(typeof(eUserLevel));

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

        public CustomUserCreatenEditResult UserCreatenEdit()
        {
            this.ShowDialog();
            CustomUserCreatenEditResult mResult = new CustomUserCreatenEditResult()
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
                if (sender is Button btn)
                {
                    switch (btn.Tag.ToString())
                    {
                        case "Confirm":
                            if (string.IsNullOrEmpty(UserName) || string.IsNullOrEmpty(UserID) || string.IsNullOrEmpty(UserPW))
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

                                    userdata = new User(UserName, UserID, UserPW, lvl, Convert.ToBoolean(cbbUse.Text));
                                    ResultUser = userdata;
                                    bResult = true;
                                }
                                else
                                {
                                    userdata = new User(UserName, UserID, UserPW, lvl, true);
                                    ResultUser = userdata;
                                    bResult = true;
                                }
                            }
                            else
                            {
                                MessageBoxPopupView.Show(TranslationManager.Instance.Translate("Select Level is Abnormal").ToString(), MessageBoxImage.Stop, false);
                                return;
                            }

                            break;
                        case "Cancel":
                            bResult = false;
                            ResultUser = null;
                            break;
                    }

                    Close();
                }
            }
            catch (Exception ex)
            {
                Log.LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
            }
            
        }
    }
    public class CustomUserCreatenEditResult
    {
        public bool Result;
        public User ResultUser;
    }
}
