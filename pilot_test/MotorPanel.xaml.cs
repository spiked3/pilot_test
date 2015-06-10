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
        public MotorPanel()
        {
            InitializeComponent();            
        }

        private void Send_Click(object sender, RoutedEventArgs e)
        {
            MainWindow._theInstance.Pid_Click(sender, e);
        }
    }
}
