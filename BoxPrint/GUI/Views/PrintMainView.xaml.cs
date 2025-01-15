using BoxPrint.GUI.ClassArray;
using BoxPrint.GUI.Views.PrintPage;
using BoxPrint.GUI.Views.UserPage;
using System;
using System.Collections.Generic;
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

namespace BoxPrint.GUI.Views
{
    /// <summary>
    /// PrintMainView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class PrintMainView : Page
    {
        PrintStatePage _PrintStatePage = new PrintStatePage();
        RecipeEditPage _RecipeEditPage = new RecipeEditPage();
        PrintScenarioPage _PrintScenarioPage = new PrintScenarioPage();
        //PrintEditImagePage _PrintEditImagePage = new PrintEditImagePage();

        public GUIColorBase GUIColorMembers = new GUIColorBase();
        //eThemeColor currentThemeColorName = eThemeColor.NONE;
        private List<string> ListHeader = new List<string>();


        public PrintMainView()
        {
            InitializeComponent();
            InitControl();

            GUIColorMembers = GlobalData.Current.GuiColor;
        }

        private void InitControl()
        {

            framePrint.Content = _PrintStatePage;
        }


        private void PrintManagement_Click(object sender, RoutedEventArgs e)
        {
            try //240404 RGJ 클릭 이벤트 예외 못잡으면 프로그램 다운 되므로 전부 예외처리함.
            {
                Control button = sender as Control;
                switch ((string)button.Tag)
                {
                    case "PrintPage":
                        PrintPagetxb.Text = "State";

                        colorBuffer_StateClickForeground.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom("#FFFFFFFF");
                        colorBuffer_StateClickBackground.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom("#FFF47725");
                        colorBuffer_RecipeClickForeground.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom("#FF4B4B4B");
                        colorBuffer_RecipeClickBackground.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom("#FFF4F4F4");
                        colorBuffer_ScenarioClickForeground.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom("#FF4B4B4B");
                        colorBuffer_ScenarioClickBackground.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom("#FFF4F4F4");
                        //Userbtn.Foreground = colorBuffer_UserClickForeground.Fill;
                        //Userbtn.Background = colorBuffer_UserClickBackground.Fill;
                        //Groupbtn.Foreground = colorBuffer_GroupClickForeground.Fill;
                        //Groupbtn.Background = colorBuffer_GroupClickBackground.Fill;
                        framePrint.Content = _PrintStatePage;
                        break;
                    case "RecipePage":
                        PrintPagetxb.Text = "Edit Recipe";
                        colorBuffer_StateClickForeground.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom("#FF4B4B4B");
                        colorBuffer_StateClickBackground.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom("#FFF4F4F4");
                        colorBuffer_RecipeClickForeground.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom("#FFFFFFFF");
                        colorBuffer_RecipeClickBackground.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom("#FFF47725");
                        colorBuffer_ScenarioClickForeground.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom("#FF4B4B4B");
                        colorBuffer_ScenarioClickBackground.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom("#FFF4F4F4");
                        //Groupbtn.Foreground = colorBuffer_UserClickForeground.Fill;
                        //Groupbtn.Background = colorBuffer_UserClickBackground.Fill;
                        //Userbtn.Foreground = colorBuffer_GroupClickForeground.Fill;
                        //Userbtn.Background = colorBuffer_GroupClickBackground.Fill;
                        framePrint.Content = _RecipeEditPage;
                        break;

                    case "ScenarioPage":
                        PrintPagetxb.Text = "Edit Scenario";
                        colorBuffer_StateClickForeground.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom("#FF4B4B4B");
                        colorBuffer_StateClickBackground.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom("#FFF4F4F4");
                        colorBuffer_RecipeClickForeground.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom("#FF4B4B4B");
                        colorBuffer_RecipeClickBackground.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom("#FFF4F4F4");
                        colorBuffer_ScenarioClickForeground.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom("#FFFFFFFF");
                        colorBuffer_ScenarioClickBackground.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom("#FFF47725");
                        framePrint.Content = _PrintScenarioPage;
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
