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
using NDesk.Options;

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
                new PropertyMetadata(new OxyPilot(new[] { "T1", "V1", "I1", "D1", "PW1" })
                { LegendBorder = OxyColors.Black }));

        public OxyPilot oxy2Model
        {
            get { return (OxyPilot)GetValue(oxy2ModelProperty); }
            set { SetValue(oxy2ModelProperty, value); }
        }
        public static readonly DependencyProperty oxy2ModelProperty =
            DependencyProperty.Register("oxy2Model", typeof(OxyPilot), typeof(MainWindow),
                new PropertyMetadata(new OxyPilot(new[] { "T2", "V2", "I2", "D2", "PW2" })
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

        public string tbPiIP
        {
            get { return (string)GetValue(tbPiIPProperty); }
            set { SetValue(tbPiIPProperty, value); }
        }
        public static readonly DependencyProperty tbPiIPProperty =
            DependencyProperty.Register("tbPiIP", typeof(string), typeof(MainWindow), new PropertyMetadata("192.168.1.19"));

        #endregion

        string mqttBroker = "127.0.0.1";

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

            var p = new OptionSet
            {
                   { "mqtt=", (v) => { mqttBroker = v; } },
            };

            p.Parse(Environment.GetCommandLineArgs());

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

        private void Pilot_OnReceive(dynamic json)
        {
            switch ((string)(json["T"]))
            {
                case "Log":
                    Dispatcher.InvokeAsync(() => { RecieveLog(json); });
                    break;
                case "Heartbeat":
                    Dispatcher.InvokeAsync(() => { oxy1Model.Append(json); oxy1.InvalidatePlot(); oxy2Model.Append(json); oxy2.InvalidatePlot(); });
                    break;
                case "Pose":
                    Dispatcher.InvokeAsync(() => { ReceivedPose(json); });
                    break;
                case "Event":
                    Dispatcher.InvokeAsync(() => { ReceivedEvent(json); });
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
                Pilot = Pilot.Factory(mqttBroker);
                Pilot.OnPilotReceive += Pilot_OnReceive;
                CommStatus = Pilot.CommStatus;
            }
            else if (Pilot != null)
            {
                Pilot.OnPilotReceive -= Pilot_OnReceive;
                Pilot.Close();
            }
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
            Pilot.Send(new { Cmd = "Config", Geom = new float[] { 336.2F,  450F } });
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
            Pilot.Send(new { Cmd = "Heartbeat", Value = 1, Int = 500 });
        }

        [UiButton("HB slow")]
        public void HbSlow_Click(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("::HbSlow_Click");
            Pilot.Send(new { Cmd = "Heartbeat", Value = 1, Int = 2000 });
        }

        static void OnMotorPowerChanged(DependencyObject source, DependencyPropertyChangedEventArgs ea)
        {
            float p = (float)source.GetValue(MotorPowerProperty);
            Trace.WriteLine($"New power={p}", "3");            
            _theInstance.Dispatcher.Invoke(()=> { _theInstance.Pilot.Send(new { Cmd = "Pwr", M1 = p, M2 = p }); } );
        }

        private void ReceivedEvent(dynamic j)
        {
            int i = j["Value"];
            string e = j["Event"];
            Trace.WriteLine($"Received Event ({e}) Value ({i})");
        }

        //private float constrain(float v, int mi, int ma)
        //{
        //    return Math.Max(Math.Min(ma, v), mi);
        //}

        //static float distance(float startX, float x, float startY, float y)
        //{
        //    return (float)(Math.Sqrt((x - startX) * (x - startX) + (y - startY) * (y - startY)));
        //}

        private void Power0_Click(object sender, RoutedEventArgs e)
        {
            Pilot.Send(new { Cmd = "Pwr", M1 = 0, M2 = 0 });
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