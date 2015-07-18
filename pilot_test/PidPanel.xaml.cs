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
    public partial class PidPanel : UserControl
    {
        public event EventHandler Click;

        public PidPanel()
        {
            InitializeComponent();            
        }

        private void Click_Click(object sender, RoutedEventArgs e)
        {
            if (Click != null)
                Click(this, e);
            //MainWindow._theInstance.Pid_Click(sender, e);
        }
    }

    public class PidData
    {
        public float Kp { get; set; }
        public float Ki { get; set; }
        public float Kd { get; set; }
    }
}
