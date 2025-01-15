using BoxPrint.DataList;
using BoxPrint.GUI.ClassArray;
using BoxPrint.GUI.ETC;
using BoxPrint.GUI.EventCollection;
using BoxPrint.Log;
using BoxPrint.Modules.User;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using TranslationByMarkupExtension;

namespace BoxPrint.GUI.Views.UserPage
{
    /// <summary>
    /// UserAccountManagementPage.xaml에 대한 상호 작용 논리
    /// </summary>

    public partial class UserAccountManagementPage : Page
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

        UserCreateView m_UCV = null;
        public GUIColorBase GUIColorMembers = new GUIColorBase();
        //eThemeColor currentThemeColorName = eThemeColor.NONE;

        //bool isSubMenuOpen; //SuHwan_20220712 : 아이오 추가

        private List<string> ListHeader = new List<string>();

        //private List<User> dbuser;
        //private int searchCount = 0;
        public static UserAccountManagementPage Current { get; private set; }

        string caption_buf = string.Empty;

        public UserAccountManagementPage()
        {
            InitializeComponent();
            InitControl();
            InitializeVariable();
            setGUIUserChangeVisibility();

            DataContext = this;     //240813 HoN 폰트 사이즈 바인딩 이상 수정

            MainWindow._EventCall_ChangeLanguage += new MainWindow.EventHandler_ChangeLanguage(setLanguage);
            UIEventCollection.Instance.OnCultureChanged += _UIEventCollection_OnCultureChanged;
        }

        private void _UIEventCollection_OnCultureChanged(string cultureKey)
        {
            GdSetHeader(eSectionHeader.User.ToString(), dgrdUser);

            User row = null;
            switch (caption_buf)
            {
                case "Create":
                    UserDeleteGrid.Visibility = Visibility.Collapsed;
                    m_UCV = new UserCreateView("User Create", row);
                    frame_content.Content = m_UCV;
                    break;

                case "Delete":
                    UserDeleteGrid.Visibility = Visibility.Visible;
                    frame_content.Content = null;
                    break;

                case "Edit":
                    UserDeleteGrid.Visibility = Visibility.Collapsed;
                    m_UCV = new UserCreateView("User Edit", row);
                    frame_content.Content = m_UCV;
                    break;
                default:
                    break;
            }
        }

        private void InitControl()
        {
            GdSetHeader(eSectionHeader.User.ToString(), dgrdUser);

            //dgrdUserEventer.ItemsSource = GlobalData.Current.UserMng;
            dgrdUser.ItemsSource = GlobalData.Current.UserMng;
            ((INotifyCollectionChanged)dgrdUser.ItemsSource).CollectionChanged +=
                                            new NotifyCollectionChangedEventHandler(UserListCollectionChanged);
            InitDatagridView(); //데이터 그리드 초기화
        }

        private void InitializeVariable()
        {
            User row = null;
            m_UCV = new UserCreateView("User Create", row);
            frame_content.Content = m_UCV;
            //isSubMenuOpen = false;

            //데이터 그리드 ROW 단일선택모드
            dgrdUser.SelectionMode = (DataGridSelectionMode)SelectionMode.Single;


            //계정 관리 권한 있을시 계쩡관리 화면 확인가능 
            UserAccountManagementPage.Current = this;
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
                else if(item.GridItem == "No")
                    addedcol.Header = TranslationManager.Instance.Translate("NumberOrder").ToString();
                else
                    addedcol.Header = TranslationManager.Instance.Translate(item.GridItem).ToString();

                if (item.BindingItem == "UserUse")
                {
                    DataGridCheckBoxColumn UseCheckcol = new DataGridCheckBoxColumn();

                    UseCheckcol.Binding = new Binding(item.BindingItem);
                    UseCheckcol.Width = new DataGridLength(item.GridWidth, DataGridLengthUnitType.Star);
                    UseCheckcol.IsReadOnly = true;
                    UseCheckcol.Header = TranslationManager.Instance.Translate(item.GridItem).ToString();

                    Dg.Columns.Add(UseCheckcol);
                    ListHeader.Add(UseCheckcol.Header.ToString());
                }
                else
                {
                    addedcol.Binding = new Binding(item.BindingItem);
                    addedcol.Width = new DataGridLength(item.GridWidth, DataGridLengthUnitType.Star);
                    addedcol.IsReadOnly = true;

                    Dg.Columns.Add(addedcol);
                    ListHeader.Add(addedcol.Header.ToString());
                }
            }
        }

        private void UserContextMenu_Click(object sender, RoutedEventArgs e)
        {
            try //240404 RGJ 클릭 이벤트 예외 못잡으면 프로그램 다운 되므로 전부 예외처리함.
            {
                MenuItem mitem = sender as MenuItem;

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
                        default:
                            row = dgrdUser.SelectedItem as User;

                            if (row == null)
                                return;

                            switch (mitem.Tag.ToString())
                            {
                                case "Edit":
                                case "Delete":
                                    //msg = string.Format(TranslationManager.Instance.Translate("User편집 메시지").ToString(),
                                    //                    TranslationManager.Instance.Translate(mitem.Tag.ToString()).ToString(),
                                    //                    row.UserID);
                                    //msgbox = new MessageBoxPopupView(msg, MessageBoxButton.OKCancel, MessageBoxImage.Question);
                                    msg = TranslationManager.Instance.Translate("User편집 메시지").ToString();
                                    msg = string.Format(msg, mitem.Tag.ToString(), row.UserID);

                                    msgbox = new MessageBoxPopupView("Info Message", "", msg, "", MessageBoxButton.OKCancel, MessageBoxImage.Question,
                                        "User편집 메시지", mitem.Tag.ToString(), row.UserID, true);
                                    break;
                                default:
                                    return;
                            }
                            break;
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
                        InitDatagridView();
                        if (userControlResult != null)
                        {
                            MessageBoxPopupView.Show(userControlResult.strResult, MessageBoxImage.Information);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
            }
            
        }

        //private void DataGridRow_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        //{
        //    DataGridRow dgr = sender as DataGridRow;

        //    if (dgr != null)
        //    {
        //        dgrdUser.SelectedItem = null;
        //        dgr.IsSelected = true;
        //        dgr.ContextMenu = (ContextMenu)this.Resources["UserContextMenuEdit"];
        //        dgr.ContextMenu.IsOpen = true;
        //    }
        //}

        //private void DataGrid_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        //{
        //    DataGrid dg = sender as DataGrid;

        //    if (dg != null)
        //    {
        //        int iselect = dg.SelectedIndex;

        //        if (iselect != -1)
        //        {
        //            DataGridRow dgr = (DataGridRow)dg.ItemContainerGenerator.ContainerFromIndex(iselect);

        //            if (dgr != null)
        //            {
        //                if (dgr.ContextMenu != null)
        //                {
        //                    if (dgr.ContextMenu.IsOpen) return;
        //                }
        //            }
        //        }

        //        dg.ContextMenu = (ContextMenu)this.Resources["UserContextMenuAdd"];
        //        dg.ContextMenu.IsOpen = true;
        //    }
        //}

        private void UserListCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            dgrdUser.ItemsSource = GlobalData.Current.UserMng;
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


                    dgrdUser.ItemsSource = getList;
                }
            }
            catch (Exception ex)
            {
                Log.LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
            }
            
        }

        private void txtEmployno_IsMouseCapturedChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (UserSearchtxt.Text == "사용자 검색" || UserSearchtxt.Text == null)
            {
                UserSearchtxt.Text = string.Empty;
            }
            UserSearchtxt.Foreground = new SolidColorBrush(Colors.Black);
        }

        //텍스트박스에서 초점이 떠났을때, 빈텍스트면 텍스트박스 초기화
        private void txtEmployno_LostFocus(object sender, RoutedEventArgs e)
        {
            if (UserSearchtxt.Text.Length == 0)
            {
                //UserSearchtxt.Text = "사용자 검색";
                //UserSearchtxt.Foreground = new SolidColorBrush(Colors.Gray);
                //UserSearchtxt.Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom("#e3e3e3");
            }
        }

        //private void ChangeDataGrid(string rcvModuleType)
        //{
        //    IEnumerable<AlarmData> sortAlarmList;
        //    _CurrentAlarmList = GlobalData.Current.Alarm_Manager.getAllAlarmList();
        //    dataGridAlarmList.Items.Clear();

        //    try
        //    {
        //        sortAlarmList = rcvModuleType == "All" ? _CurrentAlarmList : _CurrentAlarmList.Where(R => R.ModuleType == rcvModuleType);

        //        int listNumber = 1;
        //        foreach (var item in sortAlarmList)
        //        {
        //            AlarmData_View alarmDataBuffer = new AlarmData_View(item);

        //            alarmDataBuffer.ListNo = listNumber;
        //            dataGridAlarmList.Items.Add(alarmDataBuffer);
        //            listNumber++;
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        LogManager.WriteConsoleLog(eLogLevel.Info, e.ToString());
        //    }
        //}

        private void Find_Click(object sender, RoutedEventArgs e)
        {
            try //240404 RGJ 클릭 이벤트 예외 못잡으면 프로그램 다운 되므로 전부 예외처리함.
            {
                Button btn = sender as Button;
                //string textFind = UserSearchtxt.Text;
                ICollectionView icv = CollectionViewSource.GetDefaultView(dgrdUser.ItemsSource);
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
                                if (item.UserID.ToUpper().Contains(UserSearchtxt.Text.ToUpper()))
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
            ICollectionView icv = CollectionViewSource.GetDefaultView(dgrdUser.ItemsSource);
            UserSearchtxt.Text = null;
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
                        if (item.UserID.ToUpper().Contains(UserSearchtxt.Text.ToUpper()))
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

            User oo = o as User;
            if (UserSearchtxt.Text == "사용자 검색")
            {
                return true;
            }
            if (oo.UserID.ToUpper().Contains(UserSearchtxt.Text.ToUpper()))
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
                            row = dgrdUser.SelectedItem as User;
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

    public void setLanguage()
        {
            User row = null;
            User previewrow = null;
            row = dgrdUser.SelectedItem as User;
            if (row != previewrow)
            {
                previewrow = row;
                string msg = TranslationManager.Instance.Translate("을 삭제 하시겠습니까?").ToString();
                SelectUserInformation.Text = string.Format(msg, row.UserName.ToString() + "( ID : " + row.UserID.ToString() + " )");
            }

            if (row == null)
            {
                //SelectUserInformation.Text = "삭제할 유저 정보를 선택해주세요.";
                SelectUserInformation.Text = TranslationManager.Instance.Translate("삭제할 유저 정보를 선택해주세요.").ToString();
            }
        }

        //데이터 그리드 row 선택시 유저정보 삭제 페이지에 정보 띄워주기
        private void dgrdUser_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            User row = null;
            User previewrow = null;
            row = dgrdUser.SelectedItem as User;

            switch(caption_buf)
            {
                case "Edit":
                    UserDeleteGrid.Visibility = Visibility.Collapsed;
                    m_UCV = new UserCreateView("User Edit", row);
                    frame_content.Content = m_UCV;
                    break;
                case "Delete":
                    if (row != previewrow)
                    {
                        previewrow = row;
                        string msg = TranslationManager.Instance.Translate("을 삭제 하시겠습니까?").ToString();
                        SelectUserInformation.Text = string.Format(msg, row.UserName.ToString() + "( ID : " + row.UserID.ToString() + " )");
                    }

                    if (row == null)
                    {
                        SelectUserInformation.Text = TranslationManager.Instance.Translate("삭제할 유저 정보를 선택해주세요.").ToString();
                    }
                    break;
            }
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
                    case "Create":
                        UserDeleteGrid.Visibility = Visibility.Collapsed;
                        m_UCV = new UserCreateView("User Create", row);
                        frame_content.Content = m_UCV;
                        break;
                    case "Delete":
                        UserDeleteGrid.Visibility = Visibility.Visible;
                        frame_content.Content = null;
                        break;
                    case "Edit":
                        UserDeleteGrid.Visibility = Visibility.Collapsed;
                        m_UCV = new UserCreateView("User Edit", row);
                        frame_content.Content = m_UCV;
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

                row = dgrdUser.SelectedItem as User;
                userControlResult = GlobalData.Current.UserMng.DeleteUser(row);
                InitDatagridView();
                if (userControlResult != null)
                {
                    MessageBoxPopupView.Show(userControlResult.strResult, MessageBoxImage.Information);

                    LogManager.WriteOperatorLog(string.Format("사용자가 {0} {1} 을/를 Click하였습니다.", buttonname, btn.Tag.ToString()),
                        "CLICK", buttonname, GlobalData.Current.CurrentIP, string.Empty, GlobalData.Current.ClientPCName, GlobalData.Current.CurrentUserID, 4,
                        buttonname, btn.Tag.ToString());
                }
                //SelectUserInformation.Text = "삭제할 유저 정보를 선택해주세요.";
                SelectUserInformation.Text = TranslationManager.Instance.Translate("삭제할 유저 정보를 선택해주세요.").ToString();
            }
            catch (Exception ex)
            {
                Log.LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
            }
        }
    }
    }
