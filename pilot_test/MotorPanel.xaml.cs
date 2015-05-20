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

namespace pilot_test
{
    /// <summary>
    /// Interaction logic for pidControl.xaml
    /// </summary>
    public partial class MotorPanel : UserControl
    {
        public MotorPanel()
        {
            InitializeComponent();
        }

        private void MotorPower_TextChanged(object sender, TextChangedEventArgs e)
        {
            CheckPower((sender as TextBox).Text);        // no await intentionally       
        }

        float lastPpower;
        async void CheckPower(string p)
        {
            await Task.Delay(1000);
            float newPower;
            if (float.TryParse(p, out newPower))
            {
                Trace.WriteLine($"New power={newPower}", "3");
                MainWindow.SendPilot(new { Cmd = "Pwr", M1 = newPower, M2 = newPower });
                lastPpower = newPower;
            }
        }
    }
}
