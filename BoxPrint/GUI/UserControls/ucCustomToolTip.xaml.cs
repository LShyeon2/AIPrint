//220513 HHJ SCS 개선     //- Popup 화면 구성
using BoxPrint.Modules.Conveyor;     //220519 HHJ SCS 개선     //- CVUserControl ToolTip 추가
using BoxPrint.Modules.RM;
using BoxPrint.Modules.Shelf;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TranslationByMarkupExtension;

namespace BoxPrint.GUI
{
    /// <summary>
    /// Interaction logic for ucCustomToolTip.xaml
    /// </summary>
    public partial class ucCustomToolTip : UserControl
    {
        public ucCustomToolTip(object values) : base()
        {
            InitializeComponent();

            List<Grid> grids = new List<Grid>();
            if (values is ShelfItem item)
            {
                if(item.DeadZone)
                {
                    txtName.Text = string.Format("{0}  [{1}]", item.ControlName, TranslationManager.Instance.Translate("DeadZone"));
                }
                else
                {
                    txtName.Text = item.ControlName;

                    grids.Add(GetValueRow(TranslationManager.Instance.Translate("CarrierID").ToString(), item.CarrierID));
                    grids.Add(GetValueRow(TranslationManager.Instance.Translate("Size").ToString(), TranslationManager.Instance.Translate(item.CarrierSize.ToString()).ToString()));
                    grids.Add(GetValueRow(TranslationManager.Instance.Translate("ShelfType").ToString(), TranslationManager.Instance.Translate(item.ShelfType.ToString()).ToString()));
                    grids.Add(GetValueRow(TranslationManager.Instance.Translate("Bank").ToString(), item.iBank));
                    grids.Add(GetValueRow(TranslationManager.Instance.Translate("Bay").ToString(), item.iBay));
                    grids.Add(GetValueRow(TranslationManager.Instance.Translate("Level").ToString(), item.iLevel));
                    grids.Add(GetValueRow(TranslationManager.Instance.Translate("Stored In").ToString() + " ", item.GetCarrierInTime()));
                    grids.Add(GetValueRow(TranslationManager.Instance.Translate("Memo").ToString(), item.ShelfMemo));
                }
            }
            else if (values is RMModuleBase rm)
            {
                txtName.Text = rm.ControlName;

                grids.Add(GetValueRow("X   ", rm.XAxisPosition));
                grids.Add(GetValueRow("Z   ", rm.ZAxisPosition));
                grids.Add(GetValueRow(TranslationManager.Instance.Translate("Fork").ToString(), rm.ForkAxisPosition));
                grids.Add(GetValueRow(TranslationManager.Instance.Translate("CarrierID").ToString(), rm.CarrierID));
                grids.Add(GetValueRow(TranslationManager.Instance.Translate("Command").ToString(), rm.GetCurrentCmd()?.Command.ToString()));
            }
            //220519 HHJ SCS 개선     //- CVUserControl ToolTip 추가
            else if (values is CV_BaseModule cv)
            {
                if (cv.CVModuleType == eCVType.WaterPool)
                {
                    txtName.Text = string.Format("{0}  [{1}]", cv.ControlName, cv.TrackID);
                    grids.Add(GetValueRow(TranslationManager.Instance.Translate("Type").ToString(), TranslationManager.Instance.Translate("Water Tank").ToString() + " "));
                    grids.Add(GetValueRow(TranslationManager.Instance.Translate("InOut").ToString(), TranslationManager.Instance.Translate(cv.PortInOutType.ToString()).ToString()));
                    grids.Add(GetValueRow(TranslationManager.Instance.Translate("Carrier").ToString() + " ", TranslationManager.Instance.Translate(cv.CarrierExistBySensor() ? "EXIST" : "EMPTY").ToString()));
                    grids.Add(GetValueRow(TranslationManager.Instance.Translate("L Req").ToString() + " ", TranslationManager.Instance.Translate(cv.PLC_LoadRequest ? "ON" : "OFF").ToString()));
                    if (cv.Position_Bank != 0 && cv.Position_Level != 0) //행단 없는 포트는 생략
                    {
                        grids.Add(GetValueRow(TranslationManager.Instance.Translate("Bank").ToString(), cv.Position_Bank));
                        grids.Add(GetValueRow(TranslationManager.Instance.Translate("Bay").ToString(), cv.Position_Bay));
                        grids.Add(GetValueRow(TranslationManager.Instance.Translate("Level").ToString(), cv.Position_Level));
                    }
                }
                else
                {
                    txtName.Text = string.Format("{0}  [{1}]", cv.ControlName, cv.TrackID);
                    grids.Add(GetValueRow(TranslationManager.Instance.Translate("CarrierID").ToString(), cv.GetCarrierID()));
                    grids.Add(GetValueRow(TranslationManager.Instance.Translate("Type").ToString(), TranslationManager.Instance.Translate(cv.PortType.ToString()).ToString()));
                    grids.Add(GetValueRow(TranslationManager.Instance.Translate("InOut").ToString(), TranslationManager.Instance.Translate(cv.PortInOutType.ToString()).ToString()));
                    grids.Add(GetValueRow(TranslationManager.Instance.Translate("PalletSize").ToString(), TranslationManager.Instance.Translate(cv.GetPalletSize().ToString()).ToString()));
                    grids.Add(GetValueRow(TranslationManager.Instance.Translate("CarrierSize").ToString(), TranslationManager.Instance.Translate(cv.GetCarrierSize().ToString()).ToString()));
                    grids.Add(GetValueRow(TranslationManager.Instance.Translate("PortSize").ToString(), TranslationManager.Instance.Translate(cv.PortSize.ToString()).ToString()));
                    grids.Add(GetValueRow(TranslationManager.Instance.Translate("CraneAble").ToString(), TranslationManager.Instance.Translate(cv.RobotAccessAble.ToString()).ToString()));
                    if (cv.Position_Bank != 0 && cv.Position_Level != 0) //행단 없는 포트는 생략
                    {
                        grids.Add(GetValueRow(TranslationManager.Instance.Translate("Bank").ToString(), cv.Position_Bank));
                        grids.Add(GetValueRow(TranslationManager.Instance.Translate("Bay").ToString(), cv.Position_Bay));
                        grids.Add(GetValueRow(TranslationManager.Instance.Translate("Level").ToString(), cv.Position_Level));
                    }
                    grids.Add(GetValueRow(TranslationManager.Instance.Translate("Step").ToString(), TranslationManager.Instance.Translate(cv.NextCVCommand.ToString().ToUpper()).ToString()+ "  " + cv.LocalActionStep));
                }
            }

            int irow = 0;
            foreach (Grid g in grids)
            {
                RowDefinition row = new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) };
                grdValues.RowDefinitions.Add(row);
                Grid.SetRow(g, irow++);
                grdValues.Children.Add(g);
            }
        }

        public Grid GetValueRow(string header, object value)
        {
            Grid g = new Grid();
            g.Margin = new Thickness(0, 1, 0, 1);

            ColumnDefinition col1 = new ColumnDefinition() { Width = new GridLength(70) };
            ColumnDefinition col2 = new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) };
            ColumnDefinition col3 = new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) };
            g.ColumnDefinitions.Add(col1);
            g.ColumnDefinitions.Add(col2);
            g.ColumnDefinitions.Add(col3);

            TextBlock tHeader = new TextBlock();
            tHeader.Text = header;
            tHeader.Foreground = Brushes.White;
            tHeader.TextAlignment = TextAlignment.Center;
            Grid.SetColumn(tHeader, 0);
            g.Children.Add(tHeader);

            TextBlock tDivide = new TextBlock();
            tDivide.Text = ":";
            tDivide.Foreground = Brushes.White;
            tDivide.TextAlignment = TextAlignment.Center;
            Grid.SetColumn(tDivide, 1);
            g.Children.Add(tDivide);

            TextBlock tValue = new TextBlock();
            if (value == null || string.IsNullOrEmpty(value.ToString()))
                tValue.Text = TranslationManager.Instance.Translate("NONE").ToString();
            else
                tValue.Text = value.ToString();
            tValue.Foreground = Brushes.White;
            tValue.TextAlignment = TextAlignment.Center;
            tValue.Margin = new Thickness(5, 0, 5, 0);
            Grid.SetColumn(tValue, 2);
            g.Children.Add(tValue);

            return g;
        }
    }
}
