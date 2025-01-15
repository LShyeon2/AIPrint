using BoxPrint.DataList;
using BoxPrint.GUI.ViewModels.PrintPage;
using BoxPrint.Log;
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
using System.Xml.XPath;
using TranslationByMarkupExtension;

namespace BoxPrint.GUI.Views.PrintPage
{
    /// <summary>
    /// RecipeScenarioPage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class PrintScenarioPage : Page
    {

        PrintScenarioViewModel vm;

        private List<string> ListHeader = new List<string>();

        public PrintScenarioPage()
        {
            InitializeComponent();
            initControl();

            vm = new PrintScenarioViewModel();

            DataContext = vm;

            ScenarioStateChange();
        }

        private void initControl()
        {
            GdSetHeader(eSectionHeader.Scenario_Manage.ToString(), dgrdScenarioData);

            //cmbRecipeName.ItemsSource = GlobalData.Current.Recipe_Manager.RecipeList.ToArray();

            foreach(var item in GlobalData.Current.Recipe_Manager.RecipeList)
            {
                cmbRecipeName.Items.Add(item.Recipe_Name);
            }

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


        private void dgrdScenarioData_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            
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
                        case "ScenarioAdd":
                            vm.ScenarioAdd();
                            break;

                        case "ScenarioDel":
                            vm.ScenarioDel();
                            break;

                        case "ScenarioRun":
                            if (vm.CurrentState != ePrintScenarioState.Run)
                            {
                                //vm.CurrentState = ePrintScenarioState.Run;
                                GlobalData.Current.PrintScenarioStateChange(ePrintScenarioState.Run);
                                //vm.ChangeState();
                                ScenarioStateChange();
                            }
                            break;
                        case "ScenarioStop":
                            if (vm.CurrentState != ePrintScenarioState.Stop)
                            {
                                //vm.CurrentState = ePrintScenarioState.Stop;
                                GlobalData.Current.PrintScenarioStateChange(ePrintScenarioState.Stop);

                                ScenarioStateChange();

                            }
                            break;

                        case "ScenarioPause":
                            if (vm.CurrentState != ePrintScenarioState.Paused)
                            {
                                ///vm.CurrentState = ePrintScenarioState.Paused;
                                GlobalData.Current.PrintScenarioStateChange(ePrintScenarioState.Paused);

                                ScenarioStateChange();
                            }
                            break;


                        case "Save":
                            vm.SaveData();
                            GlobalData.Current.ScenarioListRefresh();     //ui refresh 해준다.
                            break;
                        case "Reflash":
                            GlobalData.Current.ScenarioListRefresh();     //ui refresh 해준다.
                            break;
                        case "Orion":
                            // 외부 실행 파일 실행
                            Process.Start(@"C:\Squid Ink\Orion\Bin\SquidInk.Orion.exe");
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

        private void ScenarioStateChange()
        {
            if (vm.CurrentState == ePrintScenarioState.Stop)
            {
                btnScenarioRun.Visibility = Visibility.Visible;
                btnScenarioStop.Visibility = Visibility.Collapsed;
                btnScenarioPause.Visibility = Visibility.Hidden;

                //gridEdit.Visibility = Visibility.Visible;
                gridEdit.IsEnabled = true;
            }
            else if (vm.CurrentState == ePrintScenarioState.Run)
            {
                btnScenarioRun.Visibility = Visibility.Collapsed;
                btnScenarioStop.Visibility = Visibility.Visible;
                btnScenarioPause.Visibility = Visibility.Visible;

                //gridEdit.Visibility = Visibility.Hidden;
                gridEdit.IsEnabled = false;
            }
            else if (vm.CurrentState == ePrintScenarioState.Paused)
            {
                btnScenarioRun.Visibility = Visibility.Visible;
                btnScenarioStop.Visibility = Visibility.Collapsed;
                btnScenarioPause.Visibility = Visibility.Hidden;

                //gridEdit.Visibility = Visibility.Hidden;
                gridEdit.IsEnabled = true;
            }
        }

        private void cmbRecipeName_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //string name = cmbRecipeName.Text;
            if (cmbRecipeName.SelectedItem == null) 
            {
                return;
            }
            string name = cmbRecipeName.SelectedItem.ToString();
            cmbRecipeName.Text = name;

            var select = GlobalData.Current.Recipe_Manager.RecipeList.Where(r => r.Recipe_Name == name).FirstOrDefault();

            if (select != null)
            {
                txtRecipeNo.Text = select.Recipe_No.ToString();
                string a = vm.SelValue.Recipe_Name;
            }
        }
    }
}
