using BoxPrint.DataList;
using BoxPrint.GUI.ETC.LoadingPopup;
using BoxPrint.GUI.ViewModels.PrintPage;
using BoxPrint.Log;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
using System.Windows.Threading;
using System.Xml.XPath;
using TranslationByMarkupExtension;

namespace BoxPrint.GUI.Views.PrintPage
{
    /// <summary>
    /// RecipeManagement.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class RecipeManagement : Page
    {

        private List<string> ListHeader = new List<string>();

        public RecipeManagementViewModel vm;

        //private Timer check_timer = null;
        DispatcherTimer timer = new DispatcherTimer();    //객체생성


        public RecipeManagement()
        {
            InitializeComponent();
            initControl();

            //timer = new DispatcherTimer();
            //timer.Interval = TimeSpan.FromMilliseconds(500); //UI 갱신에 부하가 많아서 딜레이 수정 30 -> 100 
            //timer.Tick += new EventHandler(timer_Tick);

            vm = new RecipeManagementViewModel();

            DataContext = vm;

        }

        private void initControl()
        {
            GdSetHeader(eSectionHeader.Recipe_Manage.ToString(), dgrdRecipeData);

            

            //cmbDataType.ItemsSource = Enum.GetValues(typeof(eRecipeDataType)).Cast<eRecipeDataType>();

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


        private void dgrdRecipeData_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {

            try
            {
            }
            catch (Exception ex)
            {

            }
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try //240404 RGJ 클릭 이벤트 예외 못잡으면 프로그램 다운 되므로 전부 예외처리함.
            {
                //User userdata;
                //ResultUserControl userControlResult = null;
                if (sender is Button btn)
                {
                    string buttonname = string.Empty;

                    switch (btn.Tag.ToString())
                    {
                        case "RecipeChange":
                            vm.RecipeChange();
                            GlobalData.Current.RecipeListRefresh();     //ui refresh 해준다.
                            break;
                        //case "ListDown":
                        //    vm.SelectedDataListDown();
                        //    break;
                        case "RecipeAdd":
                            vm.RecipeAdd();
                            break;

                        case "RecipeDel":
                            vm.RecipeDel();
                            break;

                        case "Save":
                            if (CheckInputData())
                            {
                                vm.SaveData();
                                GlobalData.Current.RecipeListRefresh();     //ui refresh 해준다.
                            }

                            break;
                        case "Reflash":
                            GlobalData.Current.RecipeListRefresh();     //ui refresh 해준다.
                            break;

                    }

                    LogManager.WriteOperatorLog(string.Format("사용자가 {0} {1} 을/를 Click하였습니다.", buttonname, btn.Tag.ToString()),
                        "CLICK", btn.Tag.ToString().ToUpper(), GlobalData.Current.CurrentIP, string.Empty, GlobalData.Current.ClientPCName, GlobalData.Current.CurrentUserID, 4,
                        buttonname, btn.Tag.ToString());

                    //clear();
                }
            }
            catch (Exception ex)
            {
                Log.LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
            }
        }

        private bool CheckInputData()
        {
            bool check = false;

            //string type = txtDataType.Text.ToUpper();
            int number = 0;
            string name = txtRecipeName.Text;

            int.TryParse(txtRecipeNo.Text, out number);

            if (vm.CheckRecipeNo(number))
            {
                check = true;
            }
           
            if (check)
            {
                if (vm.SelValue != null && vm.SelValue.Recipe_Name != name)
                {
                    if (string.IsNullOrEmpty(vm.SelValue.Recipe_Name))
                        vm.SelValue.CreateDate = DateTime.Now;

                    vm.SelValue.Recipe_No = number;
                    vm.SelValue.Recipe_Name = name;
                    vm.SelValue.ModifyDate = DateTime.Now;
                }
            }

            return check;
        }

        private void txtRecipeNo_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            int reNo = 0;

            if (textBox != null)
            {
                if (!int.TryParse(textBox.Text, out reNo))
                {
                    textBox.Background = Brushes.LightPink; // 정합성 오류 표시
                    textBox.ToolTip = "숫자만 입력 가능합니다.";
                }
                else
                {
                    textBox.Background = Brushes.White; // 정상 입력
                }


            }
        }
    }
}
