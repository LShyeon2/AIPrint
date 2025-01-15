using BoxPrint.Modules.User;
using System;
using System.Collections.Generic;
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
using System.ComponentModel;
using BoxPrint.GUI.ClassArray;
using BoxPrint.Log;

namespace BoxPrint.GUI.Views.PrintPage
{
    /// <summary>
    /// RecipeEditPage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class RecipeEditPage : Page
    {

        //240813 HoN 폰트 사이즈 바인딩 이상 수정
        public event PropertyChangedEventHandler PropertyChanged;
        private static object objLock = new object();

        private RecipeModify modiView = null;
        private RecipeManagement ManageView = null;

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

        RecipeModify m_recipeTap1 = null;
        RecipeManagement m_recipeTap2 = null;

        public GUIColorBase GUIColorMembers = new GUIColorBase();

        public RecipeEditPage()
        {
            InitializeComponent();

            modiView = new RecipeModify();
            ManageView = new RecipeManagement();

            DataContext = this;     //240813 HoN 폰트 사이즈 바인딩 이상 수정
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

        /// <summary>
        /// 20230607 JIG TabItem 변경 시 레시피 수정/관리 화면 띄워주기
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            User row = null;
            //ResultUserControl userControlResult = null;
            TabItem selectedTabItem = (TabItem)((TabControl)sender).SelectedItem;

            string selectedTag = selectedTabItem.Tag.ToString();
            //caption_buf = selectedTag;
            switch (selectedTag)
            {
                case "Modify":
                    //m_PSV = new PrintStateView(1);
                    GlobalData.Current.RecipeDataRefresh();
                    frame_content.Content = modiView;
                    break;
                case "Management":
                    //m_PSV = new PrintStateView(2);
                    frame_content.Content = ManageView;
                    break;
                default:
                    break;
            }
        }
    }
}
