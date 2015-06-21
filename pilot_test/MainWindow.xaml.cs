using System;
using System.IO.Ports;
using System.Text;
using System.Windows;
using System.Diagnostics;
using Newtonsoft.Json;
using System.Windows.Threading;
using System.Windows.Controls.Primitives;
using System.Threading;
using System.Dynamic;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Linq;
using System.Reflection;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

// DoEvents:  Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate { }));

namespace pilot_test
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region dp

        public float MotorPower
        {
            get { return (float)GetValue(MotorPowerProperty); }
            set { SetValue(MotorPowerProperty, value); }
        }
        public static readonly DependencyProperty MotorPowerProperty =
            DependencyProperty.Register("MotorPower", typeof(float), typeof(MainWindow), new PropertyMetadata(OnMotorPowerChanged));

        public string CommStatus
        {
            get { return (string)GetValue(CommStatusProperty); }
            set { SetValue(CommStatusProperty, value); }
        }
        public static readonly DependencyProperty CommStatusProperty =
            DependencyProperty.Register("CommStatus", typeof(string), typeof(MainWindow), new PropertyMetadata("Not Available"));

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

        public float X
        {
            get { return (float)GetValue(XProperty); }
            set { SetValue(XProperty, value); }
        }
        public static readonly DependencyProperty XProperty =
            DependencyProperty.Register("X", typeof(float), typeof(MainWindow), new PropertyMetadata(0F));

        public float Y
        {
            get { return (float)GetValue(YProperty); }
            set { SetValue(YProperty, value); }
        }
        public static readonly DependencyProperty YProperty =
            DependencyProperty.Register("Y", typeof(float), typeof(MainWindow), new PropertyMetadata(0F));

        public float H
        {
            get { return (float)GetValue(HProperty); }
            set { SetValue(HProperty, value); }
        }
        public static readonly DependencyProperty HProperty =
            DependencyProperty.Register("H", typeof(float), typeof(MainWindow), new PropertyMetadata(0F));

        public OxyPilot oxy1Model
        {
            get { return (OxyPilot)GetValue(oxy1ModelProperty); }
            set { SetValue(oxy1ModelProperty, value); }
        }
        public static readonly DependencyProperty oxy1ModelProperty =
            DependencyProperty.Register("oxy1Model", typeof(OxyPilot), typeof(MainWindow),                
                new PropertyMetadata(new OxyPilot(new[] { "T1", "V1", "I1", "D1", "PW1", "F1" })
                { LegendBorder = OxyColors.Black }));

        public float TurnH
        {
            get { return (float)GetValue(TurnHProperty); }
            set { SetValue(TurnHProperty, value); }
        }
        public static readonly DependencyProperty TurnHProperty =
            DependencyProperty.Register("TurnH", typeof(float), typeof(MainWindow), new PropertyMetadata(Settings.Default.TurnH));

        public float TurnPwr
        {
            get { return (float)GetValue(TurnPwrProperty); }
            set { SetValue(TurnPwrProperty, value); }
        }
        public static readonly DependencyProperty TurnPwrProperty =
            DependencyProperty.Register("TurnPwr", typeof(float), typeof(MainWindow), new PropertyMetadata(Settings.Default.TurnPwr));

        #endregion

        public static MainWindow _theInstance;
        Pilot Pilot;
        
        const float travelThreshold = 0.1F;
        const float turnThreshold = 10F;
        const float turnBase = 20F;

        public ObservableCollection<ButtonBase> CommandList { get { return _CommandList; } set { _CommandList = value; OnPropertyChanged(); } }
        ObservableCollection<ButtonBase> _CommandList = new ObservableCollection<ButtonBase>();

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] String T = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(T));
        }

        public MainWindow()
        {
            _theInstance = this;
            InitializeComponent();
            MotorPower = 0;

            Width = Settings.Default.Width;
            Height = Settings.Default.Height;
            Top = Settings.Default.Top;
            Left = Settings.Default.Left;
            mainGrid.RowDefinitions[1].Height = new GridLength(Settings.Default.Split1);

            if (Width == 0 || Height == 0)
            {
                Width = 640;
                Height = 480;
            }
        }

        void SaveVars()
        {
            Settings.Default.Width = Width;
            Settings.Default.Height = Height;
            Settings.Default.Top = Top;
            Settings.Default.Left = Left;
            Settings.Default.Split1 = mainGrid.RowDefinitions[1].Height.Value;

            Settings.Default.Kp = Kp;
            Settings.Default.Ki = Ki;
            Settings.Default.Kd = Kd;

            Settings.Default.TurnH = TurnH;
            Settings.Default.TurnPwr = TurnPwr;

            Settings.Default.Save();
        }

        void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ButtonBase b;
            spiked3.Console.MessageLevel = 4;   // default
            Trace.WriteLine("Pilot Test Program", "+");

            foreach (MemberInfo mi in GetType().GetMembers())
            {
                var attrs = mi.GetCustomAttributes(typeof(UiButton), true);
                if (attrs.Length > 0)
                {
                    var attr = attrs[0] as UiButton;
                    if (attr.isToggle)
                        b = new ToggleButton { Content = attr.Name, Foreground = attr.Fg, Background = attr.Bg, Style = (Style)FindResource("UiToggle") };
                    else
                        b = new Button { Content = attr.Name, Foreground = attr.Fg, Background = attr.Bg, Style = (Style)FindResource("UiButton") };

                    RoutedEventHandler r = new RoutedEventHandler((s, e2) =>
                    {
                        GetType().InvokeMember(mi.Name, BindingFlags.InvokeMethod, null, this, new object[] { s, e2 });
                    });

                    b.Click += r;
                    CommandList.Add(b);
                }
            }

            Joy1.JoystickMovedListeners += GamepadHandler;
        }

        //protected override void OnContentRendered(EventArgs e)
        //{
        //    //base.OnContentRendered(e);
        //}

        private void Pilot_OnReceive(dynamic json)
        {
            switch ((string)(json["T"]))
            {
                case "Log":
                    Dispatcher.InvokeAsync(() => { RecieveLog(json); });
                    break;
                case "Heartbeat":
                    Dispatcher.InvokeAsync(() => { oxy1Model.Append(json); oxy1.InvalidatePlot(); });
                    break;
                case "Pose":
                    Dispatcher.InvokeAsync(() => { ReceivedPose(json); });
                    break;
                case "Moved":
                    Dispatcher.InvokeAsync(() => { ReceivedMoved(json); });
                    break;
                case "Bumper":
                    Dispatcher.InvokeAsync(() => { ReceivedBumper(json); });
                    break;
            }
        }

        private void RecieveLog(dynamic json)
        {
            // if msg starts with E log as error
            bool isError = false;
            string t = json["Msg"];
            if (t.StartsWith("E"))
            {
                isError = true;
                t = t.Substring(1);
            }
            Trace.WriteLine(t, isError ? "error" : "+");
        }

        private void ReceivedPose(dynamic j)
        {
            X = j.X;
            Y = j.Y;
            H = j.H;
        }

        double lastJoyM1, lastJoyM2;

        private void GamepadHandler(rChordata.DiamondPoint p)
        {
            if (p.Left != lastJoyM1 || p.Right != lastJoyM2)
            {
                Pilot.Send(new { Cmd = "Pwr", M1 = p.Left, M2 = p.Right });
                lastJoyM1 = p.Left;
                lastJoyM2 = p.Right;
            }
        }

        void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Pilot?.Close();
            SaveVars();
        }

        void SaveState_Click(object sender, RoutedEventArgs e)
        {
            SaveVars();
        }

        void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // ---------------------- commands

        [UiButton("Serial", "Black", "White", isToggle = true)]
        public void ToggleButton_Serial(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("::ToggleButton_Serial");
            if ((sender as ToggleButton).IsChecked ?? false)
            {
                Pilot = Pilot.Factory("com7");
                Pilot.OnPilotReceive += Pilot_OnReceive;
                CommStatus = Pilot.CommStatus;
            }
            else if (Pilot != null)
            {
                Pilot.OnPilotReceive -= Pilot_OnReceive;
                Pilot.Close();
            }
        }

        [UiButton("MQTT", "Black", "White", isToggle = true)]
        public void ToggleButton_MQTT(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("::ToggleButton_MQTT");
            if ((sender as ToggleButton).IsChecked ?? false)
            {
                Pilot = Pilot.Factory("127.0.0.1");
                Pilot.OnPilotReceive += Pilot_OnReceive;
                CommStatus = Pilot.CommStatus;
            }
            else if (Pilot != null)
            {
                Pilot.OnPilotReceive -= Pilot_OnReceive;
                Pilot.Close();
            }
        }

        [UiButton("MQTT Pi", "Black", "White", isToggle = true)]
        public void ToggleButton_MQTT_Pi(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("::ToggleButton_MQTT");
            if ((sender as ToggleButton).IsChecked ?? false)
            {
                Pilot = Pilot.Factory("192.168.1.2");
                Pilot.OnPilotReceive += Pilot_OnReceive;
                CommStatus = Pilot.CommStatus;
            }
            else if (Pilot != null)
            {
                Pilot.OnPilotReceive -= Pilot_OnReceive;
                Pilot.Close();
            }
        }

        [UiButton("ESTOP", "White", "Red")]
        public void EStop_Click(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("::EStop_Click");
            Pilot.Send(new { Cmd = "Esc", Value = 0 });
            Pilot.Send(new { Cmd = "Pwr", M1 = 0, M2 = 0 });
        }

        [UiButton("Esc", "Black", "White", isToggle = true)]
        public void ToggleButton_Esc(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("::ToggleButton_Esc");
            int OnOff = (sender as ToggleButton).IsChecked ?? false ? 1 : 0;
            Pilot.Send(new { Cmd = "Esc", Value = OnOff });
        }

        [UiButton("Reset", "Black", "Yellow")]
        public void ResetPose_Click(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("::Button_ResetPose");
            Pilot.Send(new { Cmd = "Reset" });
            X = Y = H = 0f;
        }

        public void Pid_Click(object sender, RoutedEventArgs e)
        {
            // critical you include the decimal point (json decoding rqmt) (use data type float)
            Trace.WriteLine("::Pid_Click");
            Pilot.Send(new { Cmd = "Config", PID = new float[] { Kp, Ki, Kd }});
        }

        [UiButton("Geom")]
        public void Geom_Click(object sender, RoutedEventArgs e)
        {
            // +++  I can NOT resolve the measured value with actual reality :(
            Trace.WriteLine("::Geom_Click");
            // new: ticks per meter, motormax ticks per second 
            Pilot.Send(new { Cmd = "Config", Geom = new float[] { (float)( (1000 / (Math.PI * 175) * 60) ),  450F } });
        }

        [UiButton("Cali")]
        public void Cali_Click(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("::Cali_Click");
            Pilot.Send(new { Cmd = "Config" , MPU = new float[] { -333, -3632, 2311, -1062, 28, -11 } });
        }

        public void Power_Click(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("::Power_Click");
            Pilot.Send(new { Cmd = "Pwr", M1 = MotorPower, M2 = MotorPower });
        }

        [UiButton("HB Off")]
        public void HbOff_Click(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("::Button_HbOff");
            Pilot.Send(new { Cmd = "Heartbeat", Value = 0, Int = 2000 });
        }

        [UiButton("HB fast")]
        public void HbFast_Click(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("::HbFast_Click");
            Pilot.Send(new { Cmd = "Heartbeat", Value = 1, Int = 100 });
        }

        [UiButton("HB slow")]
        public void HbSlow_Click(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("::HbSlow_Click");
            Pilot.Send(new { Cmd = "Heartbeat", Value = 1, Int = 2000 });
        }

        private void Turn_Click(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("::Turn_Click");
            int turnDir = TurnH > H ? 1 : -1;
            Pilot.Send(new { Cmd = "Pwr", M1 = -TurnPwr * turnDir, M2 = TurnPwr * turnDir, hStop = TurnH });
        }

        [UiButton("Bumper", "Black", "White", isToggle = true)]
        public void ToggleButton_Bumper(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("::ToggleButton_Bumper");
            int OnOff = (sender as ToggleButton).IsChecked ?? false ? 1 : 0;
            Pilot.Send(new { Cmd = "Bumper", Value = OnOff });
        }

        static void OnMotorPowerChanged(DependencyObject source, DependencyPropertyChangedEventArgs ea)
        {
            float p = (float)source.GetValue(MotorPowerProperty);
            Trace.WriteLine($"New power={p}", "3");            
            _theInstance.Dispatcher.Invoke(()=> { _theInstance.Pilot.Send(new { Cmd = "Pwr", M1 = p, M2 = p }); } );
        }

        [UiButton("Straight 1M", "White", "Magenta")]
        public void Straight_1M(object sender, RoutedEventArgs e)
        {
            //float distGoal = 1F;
            //Trace.WriteLine("::Straight_1M");
            //float startX = X, startY = Y, startH = H;

            //DateTime lastTime = DateTime.Now;

            //    DateTime nowTime = DateTime.Now;
            //    TimeSpan elapsed = nowTime - lastTime;
            //    float min = distGoal - travelThreshold,
            //        dist = Math.Abs(distance(startX, X, startY, Y));
            //    bool arrived = dist >= min;
            //    if (arrived)
            //    {
            //        Trace.WriteLine($" Arrive");
            //        Pilot.Send(new { Cmd = "Pwr", M1 = 0, M2 = 0 });
            //        return;
            //    }

            //    //Adjustment = constrain(Adjustment, -10, 10);
            //    //Trace.WriteLine($" Adjust {Adjustment} M1({40 - Adjustment}) M1({40 + Adjustment})");
            //    Pilot.Send(new { Cmd = "Pwr", M1 = 40.0 , M2 = 40.0});
        }

        private void ReceivedMoved(dynamic j)
        {
            string v = j["Value"] == 1 ? "True" : "False";
            Trace.WriteLine($"Received Moved ({v})");
        }

        private void ReceivedBumper(dynamic j)
        {
            string v = j["Value"] == 1 ? "True" : "False";
            Trace.WriteLine($"Received Bumper ({v})");
        }

        private float constrain(float v, int mi, int ma)
        {
            return Math.Max(Math.Min(ma, v), mi);
        }

        static float distance(float startX, float x, float startY, float y)
        {
            return (float)(Math.Sqrt((x - startX) * (x - startX) + (y - startY) * (y - startY)));
        }

        float previousIntegral, previousDerivative, previousError;

        private void Power0_Click(object sender, RoutedEventArgs e)
        {
            Pilot.Send(new { Cmd = "Pwr", M1 = 0, M2 = 0 });
        }

        private float simplePid(float err, float Kp, float Ki, float Kd, TimeSpan elapsed)
        {
            float dt = (float)elapsed.TotalSeconds;
            float integral = (previousIntegral + err) * dt;
            float derivative = (previousDerivative - err) * dt;
            float output = Kp * err + Ki * integral + Kd * derivative;
            previousIntegral = integral;
            previousDerivative = derivative;
            previousError = err;
            return output;
        }

    }

    [AttributeUsage(AttributeTargets.Method)]
    public class UiButton : Attribute
    {
        public Brush Bg;
        public Brush Fg;
        public string Name;
        public bool isToggle;

        public UiButton(string name, string Foreground = "Black", string Background = "LightGray", bool isToggle = false)
        {
            Name = name;
            Fg = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Foreground));
            Bg = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Background));
        }
    }

}