using BoxPrint.GUI.ClassArray;
using BoxPrint.GUI.Views.UserPage;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BoxPrint.GUI.Views
{
    /// <summary>
    /// Interaction logic for UserView.xaml
    /// </summary>
    public partial class UserView : Page
    {
        UserAccountManagementPage _UserAccountManagementPage = new UserAccountManagementPage();
        GroupAccountManagementPage _GroupAccountManagementPage = new GroupAccountManagementPage();

        public GUIColorBase GUIColorMembers = new GUIColorBase();
        //eThemeColor currentThemeColorName = eThemeColor.NONE;
        private List<string> ListHeader = new List<string>();

        public UserView()
        {
            InitializeComponent();
            InitControl();

            GUIColorMembers = GlobalData.Current.GuiColor;
        }
        private void InitControl()
        {

            frameUser.Content = _UserAccountManagementPage;
        }


        private void AccountManagement_Click(object sender, RoutedEventArgs e)
        {
            try //240404 RGJ 클릭 이벤트 예외 못잡으면 프로그램 다운 되므로 전부 예외처리함.
            {
                Control button = sender as Control;
                switch ((string)button.Tag)
                {
                    case "UserPage":
                        UserPagetxb.Text = "User";

                        colorBuffer_UserClickForeground.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom("#FFFFFFFF");
                        colorBuffer_UserClickBackground.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom("#FFF47725");
                        colorBuffer_GroupClickForeground.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom("#FF4B4B4B");
                        colorBuffer_GroupClickBackground.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom("#FFF4F4F4");
                        //Userbtn.Foreground = colorBuffer_UserClickForeground.Fill;
                        //Userbtn.Background = colorBuffer_UserClickBackground.Fill;
                        //Groupbtn.Foreground = colorBuffer_GroupClickForeground.Fill;
                        //Groupbtn.Background = colorBuffer_GroupClickBackground.Fill;
                        frameUser.Content = _UserAccountManagementPage;
                        break;
                    case "GroupPage":
                        UserPagetxb.Text = "Group";
                        colorBuffer_UserClickForeground.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom("#FF4B4B4B");
                        colorBuffer_UserClickBackground.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom("#FFF4F4F4");
                        colorBuffer_GroupClickForeground.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom("#FFFFFFFF");
                        colorBuffer_GroupClickBackground.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom("#FFF47725");
                        //Groupbtn.Foreground = colorBuffer_UserClickForeground.Fill;
                        //Groupbtn.Background = colorBuffer_UserClickBackground.Fill;
                        //Userbtn.Foreground = colorBuffer_GroupClickForeground.Fill;
                        //Userbtn.Background = colorBuffer_GroupClickBackground.Fill;
                        frameUser.Content = _GroupAccountManagementPage;
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
            }
            

        }
    }
}
