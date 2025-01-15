using BoxPrint.GUI.ClassArray;
using BoxPrint.GUI.ETC;
using BoxPrint.GUI.EventCollection;
using BoxPrint.Log;
using BoxPrint.Modules.User;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml;
using TranslationByMarkupExtension;
using System.IO;

namespace BoxPrint.GUI.Views.UserPage
{
    #region 권한 XML 미사용 추후 삭제 예정
    //public class AuthorityData
    //{
    //    public string Menu_KOR { get; set; }

    //    public string Menu { get; set; }

    //    public string Menu_CHN { get; set; }

    //    public string Menu_HUN { get; set; }

    //    public bool ReadAuxthority { get; set; }

    //    public bool ModifyAuxthority { get; set; }
    //}

    //public class UserAuthorityData
    //{

    //    public string No { get; set; }   // Id 라는 속성

    //    public string MenuID_KOR { get; set; }  // Name 이라는 속성

    //    public string MenuID { get; set; }  // Name 이라는 속성

    //    public string MenuID_CHN { get; set; }  // Name 이라는 속성

    //    public string MenuID_HUN { get; set; }  // Name 이라는 속성

    //    public string Menu_KOR { get; set; }

    //    public string Menu { get; set; }

    //    public string Menu_CHN { get; set; }

    //    public string Menu_HUN { get; set; }

    //    public bool ReadAuxthority { get; set; }

    //    public bool ModifyAuxthority { get; set; }
    //}
    #endregion 
    /// <summary>
    /// GroupAccountManagementPage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class GroupAccountManagementPage : Page
    {
        private string _AuthorityFilePath = string.Empty;
        public string AuthorityFilePath
        {
            get
            {
                return _AuthorityFilePath;
            }
            private set
            {
                _AuthorityFilePath = value;
            }
        }

        public GUIColorBase GUIColorMembers = new GUIColorBase();
        eThemeColor currentThemeColorName = eThemeColor.NONE;
        private List<string> ListHeader = new List<string>();

        public delegate void EventHandler_ChangeAuthority();
        public static event EventHandler_ChangeAuthority _EventHandler_ChangeAuthority;


        //private List<User> dbuser;
        //private int searchCount = 0;
        public static GroupAccountManagementPage Current { get; private set; }

        public GroupAccountManagementPage()
        {
            FileInfo File = new FileInfo(GlobalData.Current.CurrentFilePaths("") + GlobalData.Current.AuthorityFilePath);
            if (!File.Exists)
            {
                string temppath = GlobalData.Current.FilePathChange(GlobalData.Current.CurrentFilePaths(""), GlobalData.Current.AuthorityFilePath);
                AuthorityFilePath = temppath;
            }
            else
            {
                AuthorityFilePath = GlobalData.Current.CurrentFilePaths("") + GlobalData.Current.AuthorityFilePath;
            }

            InitializeComponent();
            InitControl();

            GroupAccountManagementPage.Current = this;
            setGUIUserChangeVisibility();

            MainWindow._EventCall_ThemeColorChange += new MainWindow.EventHandler_ChangeThemeColor(this.eventGUIThemeColorChange);//테마 색상 이벤트
            GUIColorMembers = GlobalData.Current.GuiColor;
            UIEventCollection.Instance.OnCultureChanged += _UIEventCollection_OnCultureChanged;
        }

        private void _UIEventCollection_OnCultureChanged(string cultureKey)
        {
            switch (cultureKey)
            {
                case "ko-KR":
                    dgrdUserGroup.Columns[1].Visibility = Visibility.Visible;
                    dgrdUserGroup.Columns[5].Visibility = Visibility.Visible;

                    dgrdUserGroup.Columns[2].Visibility = Visibility.Hidden;
                    dgrdUserGroup.Columns[6].Visibility = Visibility.Hidden;

                    dgrdUserGroup.Columns[3].Visibility = Visibility.Hidden;
                    dgrdUserGroup.Columns[7].Visibility = Visibility.Hidden;

                    dgrdUserGroup.Columns[4].Visibility = Visibility.Hidden;
                    dgrdUserGroup.Columns[8].Visibility = Visibility.Hidden;
                    break;
                case "en-US":
                    dgrdUserGroup.Columns[1].Visibility = Visibility.Hidden;
                    dgrdUserGroup.Columns[5].Visibility = Visibility.Hidden;

                    dgrdUserGroup.Columns[2].Visibility = Visibility.Visible;
                    dgrdUserGroup.Columns[6].Visibility = Visibility.Visible;

                    dgrdUserGroup.Columns[3].Visibility = Visibility.Hidden;
                    dgrdUserGroup.Columns[7].Visibility = Visibility.Hidden;

                    dgrdUserGroup.Columns[4].Visibility = Visibility.Hidden;
                    dgrdUserGroup.Columns[8].Visibility = Visibility.Hidden;
                    break;
                case "zh-CN":
                    dgrdUserGroup.Columns[1].Visibility = Visibility.Hidden;
                    dgrdUserGroup.Columns[5].Visibility = Visibility.Hidden;

                    dgrdUserGroup.Columns[2].Visibility = Visibility.Hidden;
                    dgrdUserGroup.Columns[6].Visibility = Visibility.Hidden;

                    dgrdUserGroup.Columns[3].Visibility = Visibility.Visible;
                    dgrdUserGroup.Columns[7].Visibility = Visibility.Visible;

                    dgrdUserGroup.Columns[4].Visibility = Visibility.Hidden;
                    dgrdUserGroup.Columns[8].Visibility = Visibility.Hidden;
                    break;
                case "hu-HU":
                    dgrdUserGroup.Columns[1].Visibility = Visibility.Hidden;
                    dgrdUserGroup.Columns[5].Visibility = Visibility.Hidden;

                    dgrdUserGroup.Columns[2].Visibility = Visibility.Hidden;
                    dgrdUserGroup.Columns[6].Visibility = Visibility.Hidden;

                    dgrdUserGroup.Columns[3].Visibility = Visibility.Hidden;
                    dgrdUserGroup.Columns[7].Visibility = Visibility.Hidden;

                    dgrdUserGroup.Columns[4].Visibility = Visibility.Visible;
                    dgrdUserGroup.Columns[8].Visibility = Visibility.Visible;
                    break;
            }
        }

        private void InitControl()
        {
            foreach (var item in Enum.GetValues(typeof(eUserLevel)))
            {
                UserLevelComboBox.Items.Add(item);
            }
            //CreatFile();
            EditXmlFile();
        }

        public void eventGUIThemeColorChange()
        {
            if (GlobalData.Current.SendTagEvent != "User")
                return;

            setGUIThemeColorChange();
        }
        private void setGUIThemeColorChange()
        {

            if (currentThemeColorName == GUIColorMembers._currentThemeName)
                return;

            currentThemeColorName = GUIColorMembers._currentThemeName;

            //colorBuffer_NormalBorderBackground.Fill = GUIColorMembers.NormalBorderBackground;
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
                            row = dgrdUserGroup.SelectedItem as User;

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

        private void DataGridRow_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            DataGridRow dgr = sender as DataGridRow;

            if (dgr != null)
            {
                dgrdUserGroup.SelectedItem = null;
                dgr.IsSelected = true;
                dgr.ContextMenu = (ContextMenu)this.Resources["UserContextMenuEdit"];
                dgr.ContextMenu.IsOpen = true;
            }
        }

        private void DataGrid_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            DataGrid dg = sender as DataGrid;

            if (dg != null)
            {
                int iselect = dg.SelectedIndex;

                if (iselect != -1)
                {
                    DataGridRow dgr = (DataGridRow)dg.ItemContainerGenerator.ContainerFromIndex(iselect);

                    if (dgr != null)
                    {
                        if (dgr.ContextMenu != null)
                        {
                            if (dgr.ContextMenu.IsOpen) return;
                        }
                    }
                }

                dg.ContextMenu = (ContextMenu)this.Resources["UserContextMenuCreate"];
                dg.ContextMenu.IsOpen = true;
            }
        }

        //private void txtEmployno_IsMouseCapturedChanged(object sender, DependencyPropertyChangedEventArgs e)
        //{
        //    UserSearchtxt.Text = string.Empty;
        //    UserSearchtxt.Foreground = new SolidColorBrush(Colors.Black);
        //}

        //텍스트박스에서 초점이 떠났을때, 빈텍스트면 텍스트박스 초기화
        //private void txtEmployno_LostFocus(object sender, RoutedEventArgs e)
        //{
        //    if (UserSearchtxt.Text.Length == 0)
        //    {
        //        UserSearchtxt.Text = "그룹 검색";
        //        UserSearchtxt.Foreground = new SolidColorBrush(Colors.Gray);
        //    }
        //}

        #region Authorityxml
        public void CreatFile()
        {

            XmlDocument xdoc = new XmlDocument();

            XmlNode root = xdoc.CreateElement("AuthorityLevels");
            xdoc.AppendChild(root);

            foreach (var item in Enum.GetValues(typeof(eUserLevel)))
            {
                XmlNode AuthLv = xdoc.CreateElement("AuthorityLevel");
                XmlAttribute AuthLvattr = xdoc.CreateAttribute("Level");
                AuthLvattr.Value = item.ToString();
                AuthLv.Attributes.Append(AuthLvattr);

                foreach (var item1 in Enum.GetValues(typeof(eUserLevelAuthority)))
                {
                    XmlNode Auth = xdoc.CreateElement("Authority");
                    XmlAttribute Authattr = xdoc.CreateAttribute("Name");
                    Authattr.Value = item1.ToString();
                    Auth.Attributes.Append(Authattr);

                    Authattr = xdoc.CreateAttribute("Read");
                    Authattr.Value = "False";
                    Auth.Attributes.Append(Authattr);

                    Authattr = xdoc.CreateAttribute("Modify");
                    Authattr.Value = "False";
                    Auth.Attributes.Append(Authattr);

                    AuthLv.AppendChild(Auth);
                }
                root.AppendChild(AuthLv);
            }

            //xdoc.Save(GlobalData.Current.CurrentFilePaths("") + GlobalData.Current.AuthorityFilePath);
            xdoc.Save(this.AuthorityFilePath);
        }

        public void EditXmlFile()
        {
            XmlDocument xdoc = new XmlDocument();
            //xdoc.Load(GlobalData.Current.CurrentFilePaths("") + GlobalData.Current.AuthorityFilePath);
            xdoc.Load(this.AuthorityFilePath);
            XmlNodeList nodes = xdoc.SelectNodes("/AuthorityLevels/AuthorityLevel");
            bool bchange = false;

            //================================================================================eUserLevel, eUserLevelAuthority에 새로운게 있을때
            foreach (var item_eUserLevelAuthority in Enum.GetValues(typeof(eUserLevelAuthority)))
            {
                foreach (XmlNode item_AuthorityLevel in nodes)
                {
                    foreach (XmlNode item_Authority in item_AuthorityLevel)
                    {
                        if (item_eUserLevelAuthority.ToString() == item_Authority.Attributes["Name"].Value)
                        {
                            bchange = true;
                        }
                    }
                    if (!bchange)
                    {
                        XmlNode Auth = xdoc.CreateElement("Authority");
                        XmlAttribute Authattr = xdoc.CreateAttribute("Name");
                        Authattr.Value = item_eUserLevelAuthority.ToString();
                        Auth.Attributes.Append(Authattr);

                        Authattr = xdoc.CreateAttribute("Read");
                        Authattr.Value = "False";
                        Auth.Attributes.Append(Authattr);

                        Authattr = xdoc.CreateAttribute("Modify");
                        Authattr.Value = "False";
                        Auth.Attributes.Append(Authattr);

                        item_AuthorityLevel.AppendChild(Auth);
                        //xdoc.Save(GlobalData.Current.CurrentFilePaths("") + GlobalData.Current.AuthorityFilePath);'
                        xdoc.Save(this.AuthorityFilePath);
                    }
                    bchange = false;
                }
            }
            //================================================================================eUserLevel, eUserLevelAuthority에 새로운게 있을때


            //================================================================================eUserLevel, eUserLevelAuthority에 없는게 있을때

            foreach (XmlNode item_AuthorityLevel in nodes)
            {
                foreach (XmlNode item_Authority in item_AuthorityLevel)
                {
                    foreach (var item_eUserLevelAuthority in Enum.GetValues(typeof(eUserLevelAuthority)))
                    {
                        if (item_eUserLevelAuthority.ToString() == item_Authority.Attributes["Name"].Value)
                        {
                            bchange = true;
                        }
                    }
                    if (!bchange)
                    {
                        item_AuthorityLevel.RemoveChild(item_Authority);
                    }
                    bchange = false;
                }
            }

            //================================================================================eUserLevel, eUserLevelAuthority에 없는게 있을때

            //xdoc.Save(GlobalData.Current.CurrentFilePaths("") + GlobalData.Current.AuthorityFilePath);
            xdoc.Save(this.AuthorityFilePath);
        }
        #region XML 기반 SearchAuthority 주석처리
        //public void SearchAuthority()
        //{
        //    if (UserLevelComboBox.SelectedItem == null)
        //        return;
        //    List<AuthorityData> AuthorityDList = new List<AuthorityData>();
        //    AuthorityData tempAuthorityData = new AuthorityData();
        //    List<AuthorityData> TempAuthorityDList = new List<AuthorityData>();

        //    XmlDocument xdoc = new XmlDocument();
        //    //xdoc.Load(GlobalData.Current.CurrentFilePaths("") + GlobalData.Current.AuthorityFilePath);
        //    xdoc.Load(this.AuthorityFilePath);
        //    XmlNodeList nodes = xdoc.SelectNodes("/AuthorityLevels/AuthorityLevel");

        //    foreach (XmlNode item in nodes)
        //    {
        //        string SelectLevel = UserLevelComboBox.SelectedItem.ToString();
        //        if (item.Attributes["Level"].Value == SelectLevel)
        //        {
        //            foreach (XmlNode item1 in item)
        //            {
        //                if (item1.Attributes["Read"].Value == null)
        //                {
        //                    item1.Attributes["Read"].Value = "False";
        //                    //xdoc.Save(GlobalData.Current.CurrentFilePaths("") + GlobalData.Current.AuthorityFilePath);
        //                    xdoc.Save(this.AuthorityFilePath);
        //                }
        //                if (item1.Attributes["Modify"].Value == null)
        //                {
        //                    item1.Attributes["Modify"].Value = "False";
        //                    //xdoc.Save(GlobalData.Current.CurrentFilePaths("") + GlobalData.Current.AuthorityFilePath);
        //                    xdoc.Save(this.AuthorityFilePath);
        //                }
        //                AuthorityDList.Add(new AuthorityData { Menu_KOR = item1.Attributes["Name_KOR"].Value,
        //                                                       Menu     = item1.Attributes["Name"].Value,
        //                                                       Menu_CHN = item1.Attributes["Name_CHN"].Value,
        //                                                       Menu_HUN = item1.Attributes["Name_HUN"].Value,
        //                                                       ReadAuxthority = Convert.ToBoolean(item1.Attributes["Read"].Value),
        //                                                       ModifyAuxthority = Convert.ToBoolean(item1.Attributes["Modify"].Value), });
        //            }
        //            break;
        //        }
        //    }


        //    //===============================================================================================================

        //    List<UserAuthorityData> DataList = new List<UserAuthorityData>();
        //    int i = 1;
        //    foreach (var item in AuthorityDList)
        //    {
        //        //DataList.Add(new UserAuthorityData { No = i.ToString(), MenuID = item, Menu = item, ReadAuxthority = new DataGridCheckBoxColumn(), ModifyAuxthority = new DataGridCheckBoxColumn(), });
        //        DataList.Add(new UserAuthorityData { No = i.ToString(),
        //                                             MenuID_KOR = item.Menu_KOR, Menu_KOR = item.Menu_KOR,
        //                                             MenuID     = item.Menu,     Menu     = item.Menu,
        //                                             MenuID_CHN = item.Menu_CHN, Menu_CHN = item.Menu_CHN,
        //                                             MenuID_HUN = item.Menu_HUN, Menu_HUN = item.Menu_HUN,
        //                                             ReadAuxthority = item.ReadAuxthority,
        //                                             ModifyAuxthority = item.ModifyAuxthority, });
        //        i++;
        //    }

        //    switch(TranslationManager.Instance.CurrentLanguage.ToString())
        //    {
        //        case "ko-KR":
        //            dgrdUserGroup.Columns[1].Visibility = Visibility.Visible;
        //            dgrdUserGroup.Columns[5].Visibility = Visibility.Visible;

        //            dgrdUserGroup.Columns[2].Visibility = Visibility.Hidden;
        //            dgrdUserGroup.Columns[6].Visibility = Visibility.Hidden;

        //            dgrdUserGroup.Columns[3].Visibility = Visibility.Hidden;
        //            dgrdUserGroup.Columns[7].Visibility = Visibility.Hidden;

        //            dgrdUserGroup.Columns[4].Visibility = Visibility.Hidden;
        //            dgrdUserGroup.Columns[8].Visibility = Visibility.Hidden;
        //            break;
        //        case "en-US":
        //            dgrdUserGroup.Columns[1].Visibility = Visibility.Hidden;
        //            dgrdUserGroup.Columns[5].Visibility = Visibility.Hidden;

        //            dgrdUserGroup.Columns[2].Visibility = Visibility.Visible;
        //            dgrdUserGroup.Columns[6].Visibility = Visibility.Visible;

        //            dgrdUserGroup.Columns[3].Visibility = Visibility.Hidden;
        //            dgrdUserGroup.Columns[7].Visibility = Visibility.Hidden;

        //            dgrdUserGroup.Columns[4].Visibility = Visibility.Hidden;
        //            dgrdUserGroup.Columns[8].Visibility = Visibility.Hidden;
        //            break;
        //        case "zh-CN":
        //            dgrdUserGroup.Columns[1].Visibility = Visibility.Hidden;
        //            dgrdUserGroup.Columns[5].Visibility = Visibility.Hidden;

        //            dgrdUserGroup.Columns[2].Visibility = Visibility.Hidden;
        //            dgrdUserGroup.Columns[6].Visibility = Visibility.Hidden;

        //            dgrdUserGroup.Columns[3].Visibility = Visibility.Visible;
        //            dgrdUserGroup.Columns[7].Visibility = Visibility.Visible;

        //            dgrdUserGroup.Columns[4].Visibility = Visibility.Hidden;
        //            dgrdUserGroup.Columns[8].Visibility = Visibility.Hidden;
        //            break;
        //        case "hu-HU":
        //            dgrdUserGroup.Columns[1].Visibility = Visibility.Hidden;
        //            dgrdUserGroup.Columns[5].Visibility = Visibility.Hidden;

        //            dgrdUserGroup.Columns[2].Visibility = Visibility.Hidden;
        //            dgrdUserGroup.Columns[6].Visibility = Visibility.Hidden;

        //            dgrdUserGroup.Columns[3].Visibility = Visibility.Hidden;
        //            dgrdUserGroup.Columns[7].Visibility = Visibility.Hidden;

        //            dgrdUserGroup.Columns[4].Visibility = Visibility.Visible;
        //            dgrdUserGroup.Columns[8].Visibility = Visibility.Visible;
        //            break;
        //    }

        //    //====================================================================================================================

        //    dgrdUserGroup.AutoGenerateColumns = false;
        //    dgrdUserGroup.CanUserAddRows = false;
        //    dgrdUserGroup.ItemsSource = DataList;

        //}
        #endregion

        public void SearchAuthority()
        {
            if (UserLevelComboBox.SelectedItem == null)
            {
                return;
            }
            string SelectLevel = UserLevelComboBox.SelectedItem.ToString();
            GlobalData.Current.AuthorityMng.UpdateFullAuthorityFromDB();//조회전 디비에서 업데이트 한번 진행

            #region UI 컬럼설정
            switch (TranslationManager.Instance.CurrentLanguage.ToString())
            {
                case "ko-KR":
                    dgrdUserGroup.Columns[1].Visibility = Visibility.Visible;
                    dgrdUserGroup.Columns[5].Visibility = Visibility.Visible;

                    dgrdUserGroup.Columns[2].Visibility = Visibility.Hidden;
                    dgrdUserGroup.Columns[6].Visibility = Visibility.Hidden;

                    dgrdUserGroup.Columns[3].Visibility = Visibility.Hidden;
                    dgrdUserGroup.Columns[7].Visibility = Visibility.Hidden;

                    dgrdUserGroup.Columns[4].Visibility = Visibility.Hidden;
                    dgrdUserGroup.Columns[8].Visibility = Visibility.Hidden;
                    break;
                case "en-US":
                    dgrdUserGroup.Columns[1].Visibility = Visibility.Hidden;
                    dgrdUserGroup.Columns[5].Visibility = Visibility.Hidden;

                    dgrdUserGroup.Columns[2].Visibility = Visibility.Visible;
                    dgrdUserGroup.Columns[6].Visibility = Visibility.Visible;

                    dgrdUserGroup.Columns[3].Visibility = Visibility.Hidden;
                    dgrdUserGroup.Columns[7].Visibility = Visibility.Hidden;

                    dgrdUserGroup.Columns[4].Visibility = Visibility.Hidden;
                    dgrdUserGroup.Columns[8].Visibility = Visibility.Hidden;
                    break;
                case "zh-CN":
                    dgrdUserGroup.Columns[1].Visibility = Visibility.Hidden;
                    dgrdUserGroup.Columns[5].Visibility = Visibility.Hidden;

                    dgrdUserGroup.Columns[2].Visibility = Visibility.Hidden;
                    dgrdUserGroup.Columns[6].Visibility = Visibility.Hidden;

                    dgrdUserGroup.Columns[3].Visibility = Visibility.Visible;
                    dgrdUserGroup.Columns[7].Visibility = Visibility.Visible;

                    dgrdUserGroup.Columns[4].Visibility = Visibility.Hidden;
                    dgrdUserGroup.Columns[8].Visibility = Visibility.Hidden;
                    break;
                case "hu-HU":
                    dgrdUserGroup.Columns[1].Visibility = Visibility.Hidden;
                    dgrdUserGroup.Columns[5].Visibility = Visibility.Hidden;

                    dgrdUserGroup.Columns[2].Visibility = Visibility.Hidden;
                    dgrdUserGroup.Columns[6].Visibility = Visibility.Hidden;

                    dgrdUserGroup.Columns[3].Visibility = Visibility.Hidden;
                    dgrdUserGroup.Columns[7].Visibility = Visibility.Hidden;

                    dgrdUserGroup.Columns[4].Visibility = Visibility.Visible;
                    dgrdUserGroup.Columns[8].Visibility = Visibility.Visible;
                    break;
            }
            #endregion


            dgrdUserGroup.AutoGenerateColumns = false;
            dgrdUserGroup.CanUserAddRows = false;
            dgrdUserGroup.ItemsSource = GlobalData.Current.AuthorityMng.GetAuthorityItemsList(SelectLevel);

        }

        #region XML 기반 코드
        //public void DataGridSave()
        //{
        //    List<UserAuthorityData> ListUserAuthorityData = new List<UserAuthorityData>();

        //    foreach (UserAuthorityData item in dgrdUserGroup.ItemsSource)
        //    {
        //        ListUserAuthorityData.Add(item);
        //    }

        //    XmlDocument xdoc = new XmlDocument();
        //    //xdoc.Load(GlobalData.Current.CurrentFilePaths("") + GlobalData.Current.AuthorityFilePath);
        //    xdoc.Load(this.AuthorityFilePath);
        //    XmlNodeList nodes = xdoc.SelectNodes("/AuthorityLevels/AuthorityLevel");

        //    foreach (XmlNode item in nodes)
        //    {
        //        if (item.Attributes["Level"].Value == UserLevelComboBox.SelectedItem.ToString())
        //        {
        //            foreach (var item1 in ListUserAuthorityData)
        //            {
        //                foreach (XmlNode item2 in item)
        //                {
        //                    if (item1.Menu == item2.Attributes["Name"].Value)
        //                    {
        //                        item2.Attributes["Read"].Value = item1.ReadAuxthority.ToString();
        //                        item2.Attributes["Modify"].Value = item1.ModifyAuxthority.ToString();
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    //xdoc.Save(GlobalData.Current.CurrentFilePaths("") + GlobalData.Current.AuthorityFilePath);
        //    xdoc.Save(this.AuthorityFilePath);
        //    ReAuthorization();
        //}

        //public void ReAuthorization()
        //{
        //    #region 로그인 유저 권한 부여
        //    GlobalData.Current.LoginUserAuthority.Clear();
        //    XmlDocument xdoc = new XmlDocument();
        //    //xdoc.Load(GlobalData.Current.CurrentFilePaths("") + GlobalData.Current.AuthorityFilePath);
        //    xdoc.Load(this.AuthorityFilePath);
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
        //    _EventHandler_ChangeAuthority();
        //    #endregion
        //}
        #endregion

        #region DB 기반 코드

        public void DataGridSave()
        {
            foreach (AuthorityItem item in dgrdUserGroup.ItemsSource)
            {
                GlobalData.Current.AuthorityMng.UpdateAuthorityToDB(item);
            }

            GlobalData.Current.AuthorityMng.UpdateFullAuthorityFromDB(); //다시 권한 가져온다.
            ReAuthorization();
        }

        public void ReAuthorization()
        {
            #region 로그인 유저 권한 부여
            GlobalData.Current.AuthorityMng.UpdateLoginUserAuthority(GlobalData.Current.UserMng.CurrentUser.UserLevel, GlobalData.Current.LoginUserAuthority);
            _EventHandler_ChangeAuthority();
            #endregion
        }
        #endregion

        #endregion

        public void SetInit()
        {
            if (UserLevelComboBox.SelectedItem == null)
            {
                CreatFile();
                GlobalData.Current.LoginUserAuthority.Clear();
                _EventHandler_ChangeAuthority();
                return;
            }
            CreatFile();
            SearchAuthority();
            DataGridSave();
            UserLevelComboBox.SelectedItem = null;
            dgrdUserGroup.ItemsSource = null;
        }

        private void Find_Click(object sender, RoutedEventArgs e)
        {
            try //240404 RGJ 클릭 이벤트 예외 못잡으면 프로그램 다운 되므로 전부 예외처리함.
            {
                Button btn = sender as Button;
                switch (btn.Tag.ToString())
                {
                    case "Search":
                        SearchAuthority();
                        break;
                    case "Search_Init":
                        //SetInit();
                        break;
                    default:
                        break;
                }

                LogManager.WriteOperatorLog(string.Format("사용자가 {0} {1} 을/를 Click하였습니다.", "Group", btn.Tag.ToString()),
                    "CLICK", btn.Tag.ToString(), GlobalData.Current.CurrentIP, string.Empty, GlobalData.Current.ClientPCName, GlobalData.Current.CurrentUserID, 4,
                    "Group", btn.Tag.ToString());
            }
            catch (Exception ex)
            {
                Log.LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
            }
        }

        public void setGUIUserChangeVisibility()
        {
            if (MainWindow.checkLoginUserLevel != null)
            {
                if (MainWindow.checkLoginUserLevel.ToString() == "Admin")
                {
                    GroupRights.Visibility = Visibility.Visible;
                }
                else
                {
                    GroupRights.Visibility = Visibility.Hidden;
                }
            }
            else
            {
                GroupRights.Visibility = Visibility.Hidden;
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
                    //User row = null;
                    switch (mitem.Tag.ToString())
                    {
                        case "SAVE":
                            msg = TranslationManager.Instance.Translate(string.Format("Do you want to Edit User?")).ToString();
                            msgbox = new MessageBoxPopupView(msg, MessageBoxButton.OKCancel, MessageBoxImage.Question);
                            break;
                        default:
                            return;
                    }

                    CustomMessageBoxResult mBoxResult = msgbox.ShowResult();

                    if (mBoxResult.Result.Equals(MessageBoxResult.OK))
                    {
                        switch (mitem.Tag.ToString())
                        {
                            case "SAVE":
                                DataGridSave();
                                break;
                            default:
                                return;
                        }

                        LogManager.WriteOperatorLog(string.Format("사용자가 {0} {1} 을/를 Click하였습니다.", "Group", mitem.Tag.ToString()),
                            "CLICK", mitem.Tag.ToString(), GlobalData.Current.CurrentIP, string.Empty, GlobalData.Current.ClientPCName, GlobalData.Current.CurrentUserID, 4,
                            "Group", mitem.Tag.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
            }
            
        }
    }

}
