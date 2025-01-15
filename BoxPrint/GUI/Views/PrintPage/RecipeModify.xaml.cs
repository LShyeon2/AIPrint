using BoxPrint.Config;
using BoxPrint.DataList;
using BoxPrint.GUI.ViewModels.PrintPage;
using BoxPrint.Log;
using BoxPrint.Modules.User;
using System;
using System.Collections.Generic;
using System.IO;
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
using System.Xml;
using TranslationByMarkupExtension;

namespace BoxPrint.GUI.Views.PrintPage
{
    /// <summary>
    /// RecipeModify.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class RecipeModify : Page
    {

        private List<string> ListHeader = new List<string>();

        public RecipeModifyViewModel vm;

        public RecipeModify()
        {

            InitializeComponent();
            initControl();

            vm = new RecipeModifyViewModel();

            DataContext = vm;

            //dgrdRecipeData.DataContext = vm.SelectedRecipeDataList;
        }

        private void initControl()
        {

            GdSetHeader(eSectionHeader.Recipe_Modify.ToString(), dgrdRecipeData);

            cmbDataType.ItemsSource = Enum.GetValues(typeof(eRecipeDataType)).Cast<eRecipeDataType>();

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
            string sType = cmbDataType.Text.ToUpper(); 
            string sName = cmbConfigName.Text;

            try
            {
                if (string.IsNullOrEmpty(sType)) return;
                cmbDataType.SelectedIndex = (int)Enum.Parse(typeof(eRecipeDataType), sType);

                if (sType.Equals(eRecipeDataType.BASE.ToString()))
                {
                    cmbConfigName.ItemsSource = Enum.GetValues(typeof(eRecipeBaseData)).Cast<eRecipeBaseData>();
                    
                    if (string.IsNullOrEmpty(sName)) return;
                    cmbConfigName.SelectedIndex = (int)Enum.Parse(typeof(eRecipeBaseData), sName);
                }
                else    //
                {
                    cmbConfigName.ItemsSource = Enum.GetValues(typeof(eDataCount)).Cast<eDataCount>();
                    
                    if (string.IsNullOrEmpty(sName)) return;
                    cmbConfigName.SelectedIndex = (int)Enum.Parse(typeof(eDataCount), sName);
                }
            }
            catch (Exception ex)
            {

            }
        }

        private void cmbDataType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string sType = cmbDataType.SelectedItem?.ToString();

            try
            {
                if (string.IsNullOrEmpty(sType)) return;

                if (sType.Equals(eRecipeDataType.BASE.ToString()))
                {
                    cmbConfigName.ItemsSource = Enum.GetValues(typeof(eRecipeBaseData)).Cast<eRecipeBaseData>();
                }
                else    //
                {
                    cmbConfigName.ItemsSource = Enum.GetValues(typeof(eDataCount)).Cast<eDataCount>();
                    cmbConfigName.SelectedIndex = vm.GetDataMaxCount();
                }
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
                        case "ListUp":
                            vm.SelectedDataListUp();
                            break;
                        case "ListDown":
                            vm.SelectedDataListDown();

                            break;
                        case "DataAdd":
                            vm.DataAdd();
                            break;
                        case "DataDel":
                            vm.DataDel();
                            break;
                        case "Save":
                            if (CheckInputData())
                            {
                                vm.SaveData();
                                GlobalData.Current.RecipeDataRefresh();     //ui refresh 해준다.
                            }

                            break;
                        case "Reflash":
                            GlobalData.Current.RecipeDataRefresh();     //ui refresh 해준다.
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
            string type = cmbDataType.SelectedItem.ToString();
            string name = cmbConfigName.SelectedItem.ToString();
            string val = txtConfigValue.Text;

            if (type == eRecipeDataType.BASE.ToString())
            {
                
                check = true;
            }
            else
            {

                check = true;
            }

            if (check)
            {
                vm.SelValue.DataType = type;
                vm.SelValue.Config_NM = name;
                vm.SelValue.Config_Val = val;
            }

            return check;
        }

    }
}
