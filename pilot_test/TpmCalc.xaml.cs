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
using System.Windows.Shapes;

namespace pilot_test
{
    public partial class TpmCalc : Window
    {
        public int CurrentTPM
        {
            get { return (int)GetValue(CurrentTPMProperty); }
            set { SetValue(CurrentTPMProperty, value); }
        }
        public static readonly DependencyProperty CurrentTPMProperty =
            DependencyProperty.Register("CurrentTPM", typeof(int), typeof(TpmCalc), new PropertyMetadata(new PropertyChangedCallback(ReCalc)));

        public int ActualTicks
        {
            get { return (int)GetValue(ActualTicksProperty); }
            set { SetValue(ActualTicksProperty, value); }
        }
        public static readonly DependencyProperty ActualTicksProperty =
            DependencyProperty.Register("ActualTicks", typeof(int), typeof(TpmCalc), new PropertyMetadata(new PropertyChangedCallback(ReCalc)));

        public float MeasuredDistance
        {
            get { return (float)GetValue(MeasuredDistanceProperty); }
            set { SetValue(MeasuredDistanceProperty, value); }
        }
        public static readonly DependencyProperty MeasuredDistanceProperty =
            DependencyProperty.Register("MeasuredDistance", typeof(float), typeof(TpmCalc), new PropertyMetadata(0F, new PropertyChangedCallback(ReCalc)));

        public int NewTPM
        {
            get { return (int)GetValue(NewTPMProperty); }
            set { SetValue(NewTPMProperty, value); }
        }
        public static readonly DependencyProperty NewTPMProperty =
            DependencyProperty.Register("NewTPM", typeof(int), typeof(TpmCalc), new PropertyMetadata(new PropertyChangedCallback(ReCalc)));

        public TpmCalc()
        {
            InitializeComponent();
        }

        static void ReCalc(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // so actualTicks/measuredDistance is actual number
            // but we SHOULD take into account how far it was off before
            var _instance = (TpmCalc)d;
            _instance.NewTPM = (int)(_instance.ActualTicks / _instance.MeasuredDistance);
            
        }

    }
}
