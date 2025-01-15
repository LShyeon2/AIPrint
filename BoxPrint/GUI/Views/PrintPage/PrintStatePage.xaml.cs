using BoxPrint.DataList;
using BoxPrint.GUI.ClassArray;
using BoxPrint.GUI.ETC;
using BoxPrint.GUI.EventCollection;
using BoxPrint.GUI.Views.UserPage;
using BoxPrint.Log;
using BoxPrint.Modules.User;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TranslationByMarkupExtension;

namespace BoxPrint.GUI.Views.PrintPage
{
    /// <summary>
    /// PrintStatePage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class PrintStatePage : Page
    {
        //240813 HoN 폰트 사이즈 바인딩 이상 수정
        public event PropertyChangedEventHandler PropertyChanged;
        private static object objLock = new object();
        protected void RaisePropertyChanged(string propertyName)
        {
            lock (objLock)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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

        PrintStateView m_PSV = null;
        public GUIColorBase GUIColorMembers = new GUIColorBase();
        //eThemeColor currentThemeColorName = eThemeColor.NONE;

        //bool isSubMenuOpen; //SuHwan_20220712 : 아이오 추가

        private List<string> ListHeader = new List<string>();

        //private List<User> dbuser;
        //private int searchCount = 0;
        public static PrintStatePage Current { get; private set; }

        string caption_buf = string.Empty;

        public PrintStatePage()
        {
            InitializeComponent();
            InitControl();
            InitializeVariable();
            //setGUIUserChangeVisibility();

            DataContext = this;     //240813 HoN 폰트 사이즈 바인딩 이상 수정

            //MainWindow._EventCall_ChangeLanguage += new MainWindow.EventHandler_ChangeLanguage(setLanguage);
            //UIEventCollection.Instance.OnCultureChanged += _UIEventCollection_OnCultureChanged;
        }

        private void _UIEventCollection_OnCultureChanged(string cultureKey)
        {
            GdSetHeader(eSectionHeader.State_PRT.ToString(), dgrdLog);

            //User row = null;
            switch (caption_buf)
            {
                case "Print1":
                    m_PSV = new PrintStateView(1);
                    frame_content.Content = m_PSV;
                    break;

                //case "Print2":
                //    m_PSV = new PrintStateView(2);
                //    frame_content.Content = m_PSV;
                //    break;
                default:
                    break;
            }
        }

        private void InitControl()
        {
            GdSetHeader(eSectionHeader.State_PRT.ToString(), dgrdLog);

            //dgrdLogEventer.ItemsSource = GlobalData.Current.UserMng;
            dgrdLog.ItemsSource = LogManager.uILogDatas;
            ((INotifyCollectionChanged)dgrdLog.ItemsSource).CollectionChanged +=
                                            new NotifyCollectionChangedEventHandler(UserListCollectionChanged);
            InitDatagridView(); //데이터 그리드 초기화
        }

        private void InitializeVariable()
        {

            m_PSV = new PrintStateView(1);
            frame_content.Content = m_PSV;
            //isSubMenuOpen = false;

            //데이터 그리드 ROW 단일선택모드
            dgrdLog.SelectionMode = (DataGridSelectionMode)SelectionMode.Single;


            //계정 관리 권한 있을시 계쩡관리 화면 확인가능 
            PrintStatePage.Current = this;
        }

        private void GdSetHeader(string tag, DataGrid Dg)
        {
            List<GridItemListItemInfo> lsititem = GlobalData.Current.GetGridItemList(tag);

            Dg.Columns.Clear();

            foreach (var item in lsititem)
            {
                DataGridTextColumn addedcol = new DataGridTextColumn();

                if (item.GridItem.Contains("\\"))        //\ 있다면 \를 기준으로 띄워쓰기 해준다.
                {
                    addedcol.Header = TranslationManager.Instance.Translate(item.GridItem.Replace("\\", "\n")).ToString();
                }
                else
                    addedcol.Header = TranslationManager.Instance.Translate(item.GridItem).ToString();


                //addedcol.Binding = new Binding(item.BindingItem);
                addedcol.Binding = new Binding()
                {
                    Path = new PropertyPath(item.BindingItem, (object[])null),
                    //Source = item.BindingItem,
                    StringFormat = item.BindingStringFormat
                };
                addedcol.Width = new DataGridLength(item.GridWidth, DataGridLengthUnitType.Star);
                addedcol.IsReadOnly = true;

                Dg.Columns.Add(addedcol);
                ListHeader.Add(addedcol.Header.ToString());
            }
        }

        private void UserContextMenu_Click(object sender, RoutedEventArgs e)
        {
            try //240404 RGJ 클릭 이벤트 예외 못잡으면 프로그램 다운 되므로 전부 예외처리함.
            {
                MenuItem mitem = sender as MenuItem;

                //if (mitem != null)
                //{
                //    string msg;
                //    MessageBoxPopupView msgbox = null;
                //    User row = null;
                //    switch (mitem.Tag.ToString())
                //    {
                //        case "Create":
                //            msg = TranslationManager.Instance.Translate("Do you want to Create User?").ToString();
                //            msgbox = new MessageBoxPopupView(msg, MessageBoxButton.OKCancel, MessageBoxImage.Question);
                //            break;
                //        default:
                //            row = dgrdLog.SelectedItem as User;

                //            if (row == null)
                //                return;

                //            switch (mitem.Tag.ToString())
                //            {
                //                case "Edit":
                //                case "Delete":
                //                    //msg = string.Format(TranslationManager.Instance.Translate("User편집 메시지").ToString(),
                //                    //                    TranslationManager.Instance.Translate(mitem.Tag.ToString()).ToString(),
                //                    //                    row.UserID);
                //                    //msgbox = new MessageBoxPopupView(msg, MessageBoxButton.OKCancel, MessageBoxImage.Question);
                //                    msg = TranslationManager.Instance.Translate("User편집 메시지").ToString();
                //                    msg = string.Format(msg, mitem.Tag.ToString(), row.UserID);

                //                    msgbox = new MessageBoxPopupView("Info Message", "", msg, "", MessageBoxButton.OKCancel, MessageBoxImage.Question,
                //                        "User편집 메시지", mitem.Tag.ToString(), row.UserID, true);
                //                    break;
                //                default:
                //                    return;
                //            }
                //            break;
                //    }

                //    CustomMessageBoxResult mBoxResult = msgbox.ShowResult();

                //    if (mBoxResult.Result.Equals(MessageBoxResult.OK))
                //    {
                //        UserCreatePopupView uv;
                //        CustomUserCreatenEditResult userEditResult;
                //        ResultUserControl userControlResult = null;
                //        switch (mitem.Tag.ToString())
                //        {
                //            case "Create":
                //                uv = new UserCreatePopupView("User Create", row);
                //                userEditResult = uv.UserCreatenEdit();

                //                if (userEditResult.Result)
                //                {
                //                    userControlResult = GlobalData.Current.UserMng.AddUser(userEditResult.ResultUser);
                //                }
                //                break;
                //            case "Delete":
                //                userControlResult = GlobalData.Current.UserMng.DeleteUser(row);
                //                break;
                //            case "Edit":
                //                uv = new UserCreatePopupView("User Edit", row);
                //                userEditResult = uv.UserCreatenEdit();

                //                if (userEditResult.Result)
                //                {
                //                    userControlResult = GlobalData.Current.UserMng.UpdateUser(userEditResult.ResultUser);
                //                }
                //                break;
                //        }
                //        InitDatagridView();
                //        if (userControlResult != null)
                //        {
                //            MessageBoxPopupView.Show(userControlResult.strResult, MessageBoxImage.Information);
                //        }
                //    }
                //}
            }
            catch (Exception ex)
            {
                Log.LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
            }

        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //User userdata;
                //ResultUserControl userControlResult = null;
                if (sender is Button btn)
                {
                    string buttonname = string.Empty;

                    switch (btn.Tag.ToString())
                    {
                        case "Orion":
                            // 외부 실행 파일 실행
                            Process.Start(@"C:\Squid Ink\Orion\Bin\SquidInk.Orion.exe");
                            break;
                    }

                    LogManager.WriteOperatorLog(string.Format("사용자가 {0} {1} 을/를 Click하였습니다.", buttonname, btn.Tag.ToString()),
                        "CLICK", btn.Tag.ToString().ToUpper(), GlobalData.Current.CurrentIP, string.Empty, GlobalData.Current.ClientPCName, GlobalData.Current.CurrentUserID, 4,
                        buttonname, btn.Tag.ToString());

                }
            }
            catch (Exception ex)
            {
                // 에러 메시지 표시
                Log.LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
                //MessageBox.Show($"파일 실행 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        private void UserListCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            dgrdLog.ItemsSource = LogManager.uILogDatas;
        }

        private string LastSelect = string.Empty;

        private void UserListSort_Click(object sender, RoutedEventArgs e)
        {
            try //240404 RGJ 클릭 이벤트 예외 못잡으면 프로그램 다운 되므로 전부 예외처리함.
            {
                Button btn = sender as Button;

                if (btn != null)
                {
                    SafeObservableCollection<User> getList = new SafeObservableCollection<User>();

                    if (!LastSelect.Equals(btn.Tag.ToString()))
                    {
                        switch (btn.Tag.ToString())
                        {
                            //case "Engineer":
                            //    getList = GlobalData.Current.UserMng.GetUsersByLevel(eUserLevel.Engineer);
                            //    break;
                            case "Manager":
                                getList = GlobalData.Current.UserMng.GetUsersByLevel(eUserLevel.Manager);
                                break;
                            case "Operator":
                                getList = GlobalData.Current.UserMng.GetUsersByLevel(eUserLevel.Operator);
                                break;
                                //case "Monitor":
                                //    getList = GlobalData.Current.UserMng.GetUsersByLevel(eUserLevel.Monitor);
                                //    break;
                        }
                        LastSelect = btn.Tag.ToString();
                    }
                    else
                    {
                        getList = GlobalData.Current.UserMng;
                        LastSelect = string.Empty;
                    }


                    dgrdLog.ItemsSource = getList;
                }
            }
            catch (Exception ex)
            {
                Log.LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
            }

        }

        private void txtEmployno_IsMouseCapturedChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (LogSearchtxt.Text == "사용자 검색" || LogSearchtxt.Text == null)
            {
                LogSearchtxt.Text = string.Empty;
            }
            LogSearchtxt.Foreground = new SolidColorBrush(Colors.Black);
        }

        //텍스트박스에서 초점이 떠났을때, 빈텍스트면 텍스트박스 초기화
        private void txtEmployno_LostFocus(object sender, RoutedEventArgs e)
        {
            if (LogSearchtxt.Text.Length == 0)
            {
                //LogSearchtxt.Text = "사용자 검색";
                //LogSearchtxt.Foreground = new SolidColorBrush(Colors.Gray);
                //LogSearchtxt.Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom("#e3e3e3");
            }
        }


        private void Find_Click(object sender, RoutedEventArgs e)
        {
            try //240404 RGJ 클릭 이벤트 예외 못잡으면 프로그램 다운 되므로 전부 예외처리함.
            {
                Button btn = sender as Button;
                //string textFind = LogSearchtxt.Text;
                ICollectionView icv = CollectionViewSource.GetDefaultView(dgrdLog.ItemsSource);
                switch (btn.Tag.ToString())
                {
                    case "Search":
                        if (icv == null)
                        {
                            return;
                        }
                        else
                        {
                            int i = 1;
                            foreach (var item in GlobalData.Current.UserMng)
                            {
                                if (item.UserID.ToUpper().Contains(LogSearchtxt.Text.ToUpper()))
                                {
                                    item.ListNo = i;
                                    i++;
                                }
                            }

                            //searchCount = 0;
                            icv.Filter = new Predicate<object>(TextSearchFilter);
                        }

                        break;
                    case "Search_Init":
                        InitDatagridView();
                        break;
                    default:
                        break;
                }

                LogManager.WriteOperatorLog(string.Format("사용자가 {0} {1} 을/를 Click하였습니다.", "User", btn.Tag.ToString()),
                    "CLICK", btn.Tag.ToString(), GlobalData.Current.CurrentIP, string.Empty, GlobalData.Current.ClientPCName, GlobalData.Current.CurrentUserID, 4,
                    "User", btn.Tag.ToString());
            }
            catch (Exception ex)
            {
                Log.LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
            }
        }

        public void InitDatagridView()
        {
            ICollectionView icv = CollectionViewSource.GetDefaultView(dgrdLog.ItemsSource);
            LogSearchtxt.Text = null;
            if (icv == null)
            {
                return;
            }
            else
            {
                if (icv == null)
                {
                    return;
                }
                else
                {
                    int i = 1;
                    foreach (var item in GlobalData.Current.UserMng)
                    {
                        if (item.UserID.ToUpper().Contains(LogSearchtxt.Text.ToUpper()))
                        {
                            item.ListNo = i;
                            i++;
                        }
                    }

                    //searchCount = 0;
                    icv.Filter = new Predicate<object>(TextSearchFilter);
                }
            }
        }

        private bool TextSearchFilter(object o)
        {

            UILogData oo = o as UILogData;
            if (LogSearchtxt.Text == "사용자 검색")
            {
                return true;
            }
            if (oo.logModules.ToString().ToUpper().Contains(LogSearchtxt.Text.ToUpper()))
            {
                return true;
            }

            return false;
        }

        public void setGUIUserChangeVisibility()
        {
            if (MainWindow.checkLoginUserLevel != null)
            {
                if (MainWindow.checkLoginUserLevel.ToString() == "Admin")
                {
                    UserRights.Visibility = Visibility.Visible;
                }
                else
                {
                    UserRights.Visibility = Visibility.Hidden;
                }
            }
            else
            {
                UserRights.Visibility = Visibility.Hidden;
            }
        }

        public void setChangeLoginUserID()
        {
            LoginID.Text = "[ " + MainWindow.checkLoginUserID + " ]";
        }

        private void UserAccountManagementBtn_Click(object sender, RoutedEventArgs e)
        {
            try //240404 RGJ 클릭 이벤트 예외 못잡으면 프로그램 다운 되므로 전부 예외처리함.
            {
                Button mitem = sender as Button;
                if (mitem != null)
                {
                    string msg;
                    MessageBoxPopupView msgbox = null;
                    User row = null;
                    switch (mitem.Tag.ToString())
                    {
                        case "Create":
                            msg = TranslationManager.Instance.Translate("Do you want to Create User?").ToString();
                            msgbox = new MessageBoxPopupView(msg, MessageBoxButton.OKCancel, MessageBoxImage.Question);
                            break;
                        case "Edit":
                            msg = TranslationManager.Instance.Translate("Do you want to Edit User?").ToString();
                            msgbox = new MessageBoxPopupView(msg, MessageBoxButton.OKCancel, MessageBoxImage.Question);
                            break;

                        case "Delete":
                            msg = TranslationManager.Instance.Translate("Do you want to Delete User?").ToString();
                            msgbox = new MessageBoxPopupView(msg, MessageBoxButton.OKCancel, MessageBoxImage.Question);
                            row = dgrdLog.SelectedItem as User;
                            break;

                        default:
                            return;
                    }

                    CustomMessageBoxResult mBoxResult = msgbox.ShowResult();

                    if (mBoxResult.Result.Equals(MessageBoxResult.OK))
                    {
                        UserCreatePopupView uv;
                        CustomUserCreatenEditResult userEditResult;
                        ResultUserControl userControlResult = null;
                        switch (mitem.Tag.ToString())
                        {
                            case "Create":
                                uv = new UserCreatePopupView("User Create", row);
                                userEditResult = uv.UserCreatenEdit();

                                if (userEditResult.Result)
                                {
                                    userControlResult = GlobalData.Current.UserMng.AddUser(userEditResult.ResultUser);
                                }
                                break;
                            case "Delete":
                                //row=
                                userControlResult = GlobalData.Current.UserMng.DeleteUser(row);
                                break;
                            case "Edit":
                                uv = new UserCreatePopupView("User Edit", row);
                                userEditResult = uv.UserCreatenEdit();

                                if (userEditResult.Result)
                                {
                                    userControlResult = GlobalData.Current.UserMng.UpdateUser(userEditResult.ResultUser);
                                }
                                break;
                        }

                        //데이터 그리드 초기화
                        InitDatagridView();

                        if (userControlResult != null)
                        {
                            MessageBoxPopupView.Show(userControlResult.strResult, MessageBoxImage.Information);
                        }

                        LogManager.WriteOperatorLog(string.Format("사용자가 {0} {1} 을/를 Click하였습니다.", "User", mitem.Tag.ToString()),
                            "CLICK", mitem.Tag.ToString(), GlobalData.Current.CurrentIP, string.Empty, GlobalData.Current.ClientPCName, GlobalData.Current.CurrentUserID, 4,
                            "User", mitem.Tag.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
            }
        }


        //데이터 그리드 row 선택시 유저정보 삭제 페이지에 정보 띄워주기
        private void dgrdLog_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        /// <summary>
        /// 20230607 JIG TabItem 변경 시 Create/Delete/Edit 화면 띄워주기
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            User row = null;
            ResultUserControl userControlResult = null;
            TabItem selectedTabItem = (TabItem)((TabControl)sender).SelectedItem;

            string selectedTag = selectedTabItem.Tag.ToString();
            caption_buf = selectedTag;
            switch (selectedTag)
            {
                case "Print1":
                    m_PSV = new PrintStateView(1);
                    frame_content.Content = m_PSV;
                    break;
                case "Print2":
                    m_PSV = new PrintStateView(2);
                    frame_content.Content = m_PSV;
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 20230607 JIG 사용자 관리 유저 삭제
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UserDelete_Click(object sender, RoutedEventArgs e)
        {
            try //240404 RGJ 클릭 이벤트 예외 못잡으면 프로그램 다운 되므로 전부 예외처리함.
            {
                User row = null;
                ResultUserControl userControlResult = null;

                string buttonname = string.Empty;
                Button btn = (Button)sender;
                buttonname = btn.Tag.ToString();

                row = dgrdLog.SelectedItem as User;
                userControlResult = GlobalData.Current.UserMng.DeleteUser(row);
                InitDatagridView();
                if (userControlResult != null)
                {
                    MessageBoxPopupView.Show(userControlResult.strResult, MessageBoxImage.Information);

                    LogManager.WriteOperatorLog(string.Format("사용자가 {0} {1} 을/를 Click하였습니다.", buttonname, btn.Tag.ToString()),
                        "CLICK", buttonname, GlobalData.Current.CurrentIP, string.Empty, GlobalData.Current.ClientPCName, GlobalData.Current.CurrentUserID, 4,
                        buttonname, btn.Tag.ToString());
                }
            }
            catch (Exception ex)
            {
                Log.LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
            }
        }
    }
}
