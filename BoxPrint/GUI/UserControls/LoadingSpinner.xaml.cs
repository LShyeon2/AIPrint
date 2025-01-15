using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BoxPrint.GUI.UserControls
{
    /// <summary>
    /// Interaction logic for LoadingSpinner.xaml
    /// </summary>
    public partial class LoadingSpinner : UserControl
    {
        public LoadingSpinner()
        {
            InitializeComponent();
        }
        #region Diameter

        public int Diameter
        {
            get { return (int)GetValue(DiameterProperty); }
            set
            {
                if (value < 10)
                    value = 10;
                SetValue(DiameterProperty, value);
            }
        }
        public static readonly DependencyProperty DiameterProperty =
            DependencyProperty.Register("Diameter", typeof(int), typeof(LoadingSpinner), new PropertyMetadata(20, OnDiameterPropertyChanged));

        private static void OnDiameterPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var vm = (LoadingSpinner)d;
            d.CoerceValue(CenterProperty);
            d.CoerceValue(RadiusProperty);
            d.CoerceValue(InnerRadiusProperty);
        }

        #endregion

        #region Radius

        public int Radius
        {
            get { return (int)GetValue(RadiusProperty); }
            set { SetValue(RadiusProperty, value); }
        }
        public static readonly DependencyProperty RadiusProperty =
            DependencyProperty.Register("Radius", typeof(int), typeof(LoadingSpinner), new PropertyMetadata(15, null, OnCoerceRadius));

        private static object OnCoerceRadius(DependencyObject d, object baseValue)
        {
            var control = (LoadingSpinner)d;
            int newRadius = (int)(control.GetValue(DiameterProperty)) / 2;
            return newRadius;
        }

        #endregion

        #region InnerRadius

        public int InnerRadius
        {
            get { return (int)GetValue(InnerRadiusProperty); }
            set { SetValue(InnerRadiusProperty, value); }
        }
        public static readonly DependencyProperty InnerRadiusProperty =
            DependencyProperty.Register("InnerRadius", typeof(int), typeof(LoadingSpinner), new PropertyMetadata(2, null, OnCoerceInnerRadius));

        private static object OnCoerceInnerRadius(DependencyObject d, object baseValue)
        {
            var control = (LoadingSpinner)d;
            int newInnerRadius = (int)(control.GetValue(DiameterProperty)) / 4;
            return newInnerRadius;
        }

        #endregion

        #region Center

        public Point Center
        {
            get { return (Point)GetValue(CenterProperty); }
            set { SetValue(CenterProperty, value); }
        }
        public static readonly DependencyProperty CenterProperty =
            DependencyProperty.Register("Center", typeof(Point), typeof(LoadingSpinner), new PropertyMetadata(new Point(15, 15), null, OnCoerceCenter));

        private static object OnCoerceCenter(DependencyObject d, object baseValue)
        {
            var control = (LoadingSpinner)d;
            int newCenter = (int)(control.GetValue(DiameterProperty)) / 2;
            return new Point(newCenter, newCenter);
        }

        #endregion

        #region Color1

        public Color Color1
        {
            get { return (Color)GetValue(Color1Property); }
            set { SetValue(Color1Property, value); }
        }
        public static readonly DependencyProperty Color1Property =
            DependencyProperty.Register("Color1", typeof(Color), typeof(LoadingSpinner), new PropertyMetadata(Colors.Green));

        #endregion

        #region Color2

        public Color Color2
        {
            get { return (Color)GetValue(Color2Property); }
            set { SetValue(Color2Property, value); }
        }
        public static readonly DependencyProperty Color2Property =
            DependencyProperty.Register("Color2", typeof(Color), typeof(LoadingSpinner), new PropertyMetadata(Colors.Transparent));

        #endregion

        #region Msg

        public string Msg
        {
            get { return (string)GetValue(MsgProperty); }
            set { SetValue(MsgProperty, value); }
        }
        public static readonly DependencyProperty MsgProperty =
            DependencyProperty.Register("Msg", typeof(string), typeof(LoadingSpinner), new PropertyMetadata(string.Empty));

        #endregion
    }
}
