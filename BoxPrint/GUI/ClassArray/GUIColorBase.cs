using BoxPrint.Log;
using System;
using System.Windows.Media;

namespace BoxPrint.GUI.ClassArray
{
    public class GUIColorBase
    {

        public eThemeColor _currentThemeName;

        public LinearGradientBrush MainMenuBackground { get; set; }
        public Brush MainMenuForeground { get; set; }
        public LinearGradientBrush MainMenuHeaderBackground { get; set; }
        public Brush MainPointLine { get; set; }
        public Brush MainPointLine2 { get; set; }
        public Brush MainBaseBackground { get; set; }
        public LinearGradientBrush MainMenuButtonBackground { get; set; }
        public LinearGradientBrush MainMenuButtonBackground_Enter { get; set; }
        public Brush MainMenuButtonBorderBrush { get; set; }

        public Brush NormalBorderBackground { get; set; }

        public Brush NormalBorderBackground_Dark { get; set; }
        public LinearGradientBrush NormalButtonBackground { get; set; }
        public LinearGradientBrush NormalButtonBackground_Enter { get; set; }
        public LinearGradientBrush NormalButtonBackground2 { get; set; }
        public LinearGradientBrush NormalButtonBackground2_Enter { get; set; }


        public Brush NormalGreen { get; set; }
        public Brush NormalRed { get; set; }
        public Brush NormalYellow { get; set; }
        public Brush NormalGray { get; set; }
        public Brush NormalBlue { get; set; }
        public Brush NormalMint { get; set; }
        public Brush NormalUINavy { get; set; }

        //생성자
        public GUIColorBase()
        {
            _currentThemeName = eThemeColor.DARK;
            setThemeColor(eThemeColor.DARK);
        }

        //색상 설정
        public void setThemeColor(eThemeColor rcvThemeColor)
        {
            try
            {
                var ResourceDictionaryBuffer = new System.Windows.ResourceDictionary();

                switch (rcvThemeColor)
                {
                    case eThemeColor.LIGHT:
                        ResourceDictionaryBuffer.Source = new Uri("GUI/ThemeResources/ThemeColorResourcesLight.xaml", UriKind.RelativeOrAbsolute);
                        _currentThemeName = eThemeColor.LIGHT;
                        break;

                    case eThemeColor.DARK:
                    default:
                        ResourceDictionaryBuffer.Source = new Uri("GUI/ThemeResources/ThemeColorResourcesDark.xaml", UriKind.RelativeOrAbsolute);
                        _currentThemeName = eThemeColor.DARK;
                        break;
                }


                this.MainMenuBackground = ResourceDictionaryBuffer["MainMenuBackground"] as LinearGradientBrush;
                this.MainMenuForeground = ResourceDictionaryBuffer["MainMenuForeground"] as SolidColorBrush;
                this.MainMenuHeaderBackground = ResourceDictionaryBuffer["MainMenuHeaderBackground"] as LinearGradientBrush;
                this.MainPointLine = ResourceDictionaryBuffer["MainPointLine"] as SolidColorBrush;
                this.MainPointLine2 = ResourceDictionaryBuffer["MainPointLine2"] as SolidColorBrush;
                this.MainBaseBackground = ResourceDictionaryBuffer["MainBaseBackground"] as SolidColorBrush;
                this.MainMenuButtonBackground = ResourceDictionaryBuffer["MainMenuButtonBackground"] as LinearGradientBrush;
                this.MainMenuButtonBackground_Enter = ResourceDictionaryBuffer["MainMenuButtonBackground_Enter"] as LinearGradientBrush;
                this.MainMenuButtonBorderBrush = ResourceDictionaryBuffer["MainMenuButtonBorderBrush"] as SolidColorBrush;

                this.NormalBorderBackground = ResourceDictionaryBuffer["NormalBorderBackground"] as SolidColorBrush;
                this.NormalBorderBackground_Dark = ResourceDictionaryBuffer["NormalBorderBackground_Dark"] as SolidColorBrush;
                this.NormalButtonBackground = ResourceDictionaryBuffer["NormalButtonBackground"] as LinearGradientBrush;
                this.NormalButtonBackground_Enter = ResourceDictionaryBuffer["NormalButtonBackground_Enter"] as LinearGradientBrush;
                this.NormalButtonBackground2 = ResourceDictionaryBuffer["NormalButtonBackground2"] as LinearGradientBrush;
                this.NormalButtonBackground2_Enter = ResourceDictionaryBuffer["NormalButtonBackground2_Enter"] as LinearGradientBrush;

                this.NormalGreen = ResourceDictionaryBuffer["NormalGreen"] as SolidColorBrush;
                this.NormalRed = ResourceDictionaryBuffer["NormalRed"] as SolidColorBrush;
                this.NormalYellow = ResourceDictionaryBuffer["NormalYellow"] as SolidColorBrush;
                this.NormalGray = ResourceDictionaryBuffer["NormalGray"] as SolidColorBrush;
                this.NormalBlue = ResourceDictionaryBuffer["NormalBlue"] as SolidColorBrush;
                this.NormalMint = ResourceDictionaryBuffer["NormalMint"] as SolidColorBrush;
                this.NormalUINavy = ResourceDictionaryBuffer["NormalUINavy"] as SolidColorBrush;

            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, String.Format("GUIColorBase-setThemeColor : {0}", ex.ToString()));
            }
        }


        //"MainMenuBackground"
        //"MainMenuForeground"
        //"MainMenuHeaderBackground"
        //"MainPointLine"
        //"MainBaseBackground"
        //"MainMenuButtonBackground"
        //"MainMenuButtonBorderBrush"

        //"NormalGreen" 
        //"NormalRed"   
        //"NormalYellow"
        //"NormalGray"  
        //"NormalBlue"  
        //"NormalMint"  
        //"NormalUINavy"
    }
}
