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
using System.Windows.Threading;

namespace pilot_test
{
    /// <summary>
    /// Interaction logic for pidControl.xaml
    /// </summary>
    public partial class MotorPanel : UserControl
    {
        public float MotorPower
        {
            get { return (float)GetValue(MotorPowerProperty); }
            set { SetValue(MotorPowerProperty, value); }
        }
        public static readonly DependencyProperty MotorPowerProperty =
            DependencyProperty.Register("MotorPower", typeof(float), typeof(MotorPanel), new PropertyMetadata(OnMotorPowerChanged));

        static void OnMotorPowerChanged(DependencyObject source, DependencyPropertyChangedEventArgs ea)
        {
            float p = (float)source.GetValue(MotorPowerProperty);
            Trace.WriteLine($"New power={p}", "3");
            MainWindow.SendPilot(new { Cmd = "Pwr", M1 = p, M2 = p });
            lastPpower = p;
        }

        public float Kp
        {
            get { return (float)GetValue(KpProperty); }
            set { SetValue(KpProperty, value); }
        }
        public static readonly DependencyProperty KpProperty =
            DependencyProperty.Register("Kp", typeof(float), typeof(MainWindow), new PropertyMetadata(Settings.Default.Kp));
        public float Ki
        {
            get { return (float)GetValue(KiProperty); }
            set { SetValue(KiProperty, value); }
        }
        public static readonly DependencyProperty KiProperty =
            DependencyProperty.Register("Ki", typeof(float), typeof(MainWindow), new PropertyMetadata(Settings.Default.Ki));
        public float Kd
        {
            get { return (float)GetValue(KdProperty); }
            set { SetValue(KdProperty, value); }
        }
        public static readonly DependencyProperty KdProperty =
            DependencyProperty.Register("Kd", typeof(float), typeof(MainWindow), new PropertyMetadata(Settings.Default.Kd));

        static float lastPpower;

        public MotorPanel()
        {
            InitializeComponent();            
        }
    }
}
