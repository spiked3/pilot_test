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

namespace pilot_test
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        static MainWindow _theInstance;

        #region dp

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


        public float Adjustment
        {
            get { return (float)GetValue(AdjustmentProperty); }
            set { SetValue(AdjustmentProperty, value); }
        }
        public static readonly DependencyProperty AdjustmentProperty =
            DependencyProperty.Register("Adjustment", typeof(float), typeof(MainWindow), new PropertyMetadata(0F));

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

        public OxyPlot.PlotModel M1PlotModel
        {
            get { return (OxyPlot.PlotModel)GetValue(M1PlotModelProperty); }
            set { SetValue(M1PlotModelProperty, value); }
        }
        public static readonly DependencyProperty M1PlotModelProperty =
            DependencyProperty.Register("M1PlotModel", typeof(OxyPlot.PlotModel), typeof(MainWindow), 
                new PropertyMetadata(new OxyPlot.PlotModel { LegendBorder=OxyPlot.OxyColors.Black }));

        public OxyPlot.PlotModel M2PlotModel
        {
            get { return (OxyPlot.PlotModel)GetValue(M2PlotModelProperty); }
            set { SetValue(M2PlotModelProperty, value); }
        }
        public static readonly DependencyProperty M2PlotModelProperty =
            DependencyProperty.Register("M2PlotModel", typeof(OxyPlot.PlotModel), typeof(MainWindow),
                new PropertyMetadata(new OxyPlot.PlotModel { LegendBorder = OxyPlot.OxyColors.Black }));

        public OxyPlot.PlotModel M3PlotModel
        {
            get { return (OxyPlot.PlotModel)GetValue(M3PlotModelProperty); }
            set { SetValue(M3PlotModelProperty, value); }
        }
        public static readonly DependencyProperty M3PlotModelProperty =
            DependencyProperty.Register("M3PlotModel", typeof(OxyPlot.PlotModel), typeof(MainWindow),
                new PropertyMetadata(new OxyPlot.PlotModel { LegendBorder = OxyPlot.OxyColors.Black }));

        public OxyPlot.PlotModel M4PlotModel
        {
            get { return (OxyPlot.PlotModel)GetValue(M4PlotModelProperty); }
            set { SetValue(M4PlotModelProperty, value); }
        }
        public static readonly DependencyProperty M4PlotModelProperty =
            DependencyProperty.Register("M4PlotModel", typeof(OxyPlot.PlotModel), typeof(MainWindow),
                new PropertyMetadata(new OxyPlot.PlotModel { LegendBorder = OxyPlot.OxyColors.Black }));

        public float MotorMax
        {
            get { return (float)GetValue(MotorMaxProperty); }
            set { SetValue(MotorMaxProperty, value); }
        }
        public static readonly DependencyProperty MotorMaxProperty =
            DependencyProperty.Register("MotorMax", typeof(float), typeof(MainWindow), new PropertyMetadata(Settings.Default.MotorMax));

        public float TurnH
        {
            get { return (float)GetValue(TurnHProperty); }
            set { SetValue(TurnHProperty, value); }
        }
        public static readonly DependencyProperty TurnHProperty =
            DependencyProperty.Register("TurnH", typeof(float), typeof(MainWindow));

        public float TurnPwr
        {
            get { return (float)GetValue(TurnPwrProperty); }
            set { SetValue(TurnPwrProperty, value); }
        }
        public static readonly DependencyProperty TurnPwrProperty =
            DependencyProperty.Register("TurnPwr", typeof(float), typeof(MainWindow));

        #endregion

        Pilot Pilot;

        float Kp_steer = 1f, Ki_steer = 0f, Kd_steer = 0f;

        const float travelThreshold = 0.1F;
        const float turnThreshold = 10F;
        const float turnBase = 20F;
        bool cancelFlag;

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

            if (Width == 0 || Height == 0)
            {
                Width = 640;
                Height = 480;
            }
        }

        void SaveVars()
        {
            Settings.Default.Width = (float)Width;
            Settings.Default.Height = (float)Height;
            Settings.Default.Top = (float)Top;
            Settings.Default.Left = (float)Left;

            Settings.Default.MotorMax = MotorMax;
            Settings.Default.Kp = Kp;
            Settings.Default.Ki = Ki;
            Settings.Default.Kd = Kd;
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

            // oxy
            M1PlotModel.Axes.Add(new OxyPlot.Axes.DateTimeAxis { });
            M1PlotModel.Axes.Add(new OxyPlot.Axes.LinearAxis { });
            M1PlotModel.Series.Add(new OxyPlot.Series.LineSeries { Title = "Tgt" });
            M1PlotModel.Series.Add(new OxyPlot.Series.LineSeries { Title = "Vel" });
            //M1PlotModel.Series.Add(new OxyPlot.Series.LineSeries { Title = "Pwr" });
            M2PlotModel.Axes.Add(new OxyPlot.Axes.DateTimeAxis { });
            M2PlotModel.Axes.Add(new OxyPlot.Axes.LinearAxis { });
            M2PlotModel.Series.Add(new OxyPlot.Series.LineSeries { Title = "Tgt" });
            M2PlotModel.Series.Add(new OxyPlot.Series.LineSeries { Title = "Vel" });
            //M2PlotModel.Series.Add(new OxyPlot.Series.LineSeries { Title = "Pwr" });

            M3PlotModel.Axes.Add(new OxyPlot.Axes.DateTimeAxis { });
            M3PlotModel.Axes.Add(new OxyPlot.Axes.LinearAxis { });
            M3PlotModel.Series.Add(new OxyPlot.Series.LineSeries { Title = "Fb" });
            M4PlotModel.Axes.Add(new OxyPlot.Axes.DateTimeAxis { });
            M4PlotModel.Axes.Add(new OxyPlot.Axes.LinearAxis { });
            M4PlotModel.Series.Add(new OxyPlot.Series.LineSeries { Title = "Fb" });

            Joy1.JoystickMovedListeners += GamepadHandler;
        }

        private void Pilot_OnReceive(dynamic json)
        {
            switch ((string)(json["T"]))
            {
                case "Heartbeat":
                    Dispatcher.InvokeAsync(() => { ReceivedHeartBeat(json); }, DispatcherPriority.Render);
                    break;
                case "Pose":
                    Dispatcher.InvokeAsync(() => { ReceivedPose(json); }, DispatcherPriority.Render);
                    break;
                case "Moved":
                    Dispatcher.InvokeAsync(() => { ReceivedMoved(json); }, DispatcherPriority.Render);
                    break;
            }
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

        // yeah yeah, I know; everyone thinks do events is bad,
        // but this is test software not production. we generate a lot of UI traffic in our click handler we want to see
        // without resorting to threads
        void DoEvents()
        {
            Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate { }));
        }
        private void ReceivedHeartBeat(dynamic j)
        {
            //return;
            var t = DateTimeAxis.ToDouble(DateTime.Now);
            const int maxPoints = 100;

            LineSeries m1t = M1PlotModel.Series[0] as LineSeries;
            LineSeries m1v = M1PlotModel.Series[1] as LineSeries;
            //LineSeries m1p = M1PlotModel.Series[2] as LineSeries;
            LineSeries m2t = M2PlotModel.Series[0] as LineSeries;
            LineSeries m2v = M2PlotModel.Series[1] as LineSeries;
            //LineSeries m2p = M2PlotModel.Series[2] as LineSeries;
            LineSeries fb1 = M3PlotModel.Series[0] as LineSeries;
            LineSeries fb2 = M4PlotModel.Series[0] as LineSeries;

            foreach (LineSeries l in new [] {m1t, m1v, m2t, m2v, fb1, fb2 })
                if (l.Points.Count > maxPoints) l.Points.RemoveAt(0);

            m1t.Points.Add(new DataPoint(t, (double)j["T1"]));
            m1v.Points.Add(new DataPoint(t, (double)j["V1"]));
            //m1p.Points.Add(new DataPoint(t, (double)j["P1"]));
            m2t.Points.Add(new DataPoint(t, (double)j["T2"]));
            m2v.Points.Add(new DataPoint(t, (double)j["V2"]));
            //m2p.Points.Add(new DataPoint(t, (double)j["P2"]));
            fb1.Points.Add(new DataPoint(t, (int)j["F1"]));
            fb2.Points.Add(new DataPoint(t, (int)j["F2"]));

            OxyM1.InvalidatePlot();
            OxyM2.InvalidatePlot();
            OxyM3.InvalidatePlot();
            OxyM4.InvalidatePlot();
        }

        // ---------------------- commands

        [UiButton("Serial", "Black", "White")]
        public void ToggleButton_Serial(object sender, RoutedEventArgs e)
        {
            if (Pilot != null)
                Pilot.OnReceive -= Pilot_OnReceive;
            Trace.WriteLine("::ToggleButton_Serial");
            Pilot = Pilot.Factory("com7");
            Pilot.OnReceive += Pilot_OnReceive;
            CommStatus = Pilot.CommStatus;
        }

        [UiButton("MQTT", "Black", "White")]
        public void ToggleButton_MQTT(object sender, RoutedEventArgs e)
        {
            if (Pilot != null)
                Pilot.OnReceive -= Pilot_OnReceive;
            Trace.WriteLine("::ToggleButton_MQTT");
            //Pilot = Pilot.Factory("192.168.1.30");
            Pilot = Pilot.Factory("127.0.0.1");
            Pilot.OnReceive += Pilot_OnReceive;
            CommStatus = Pilot.CommStatus;
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

        [UiButton("PID")]
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
            // old: SendPilot(new { Cmd = "Geom", TPR = 60, Diam = 175.0F, Base = 220.0F, mMax = 450 } });
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

        [UiButton("HB 500")]
        public void Hb500_Click(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("::Button_Hb500");
            Pilot.Send(new { Cmd = "Heartbeat", Value = 1, Int = 500 });
        }

        [UiButton("HB 2000")]
        public void Hb2000_Click(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("::Button_Hb2000");
            Pilot.Send(new { Cmd = "Heartbeat", Value = 1, Int = 2000 });
        }

        private void Turn_Click(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("::Turn_Click");
            Pilot.Send(new { Cmd = "Pwr", M1 = -TurnPwr, M2 = TurnPwr, hStop = TurnH });
        }

        [UiButton("Bumper", "Black", "White", isToggle = true)]
        public void ToggleButton_Bumper(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("::ToggleButton_Bumper");
            int OnOff = (sender as ToggleButton).IsChecked ?? false ? 1 : 0;
            Pilot.Send(new { Cmd = "Bumper", Value = OnOff });
        }

        public float MotorPower
        {
            get { return (float)GetValue(MotorPowerProperty); }
            set { SetValue(MotorPowerProperty, value); }
        }
        public static readonly DependencyProperty MotorPowerProperty =
            DependencyProperty.Register("MotorPower", typeof(float), typeof(MainWindow), new PropertyMetadata(OnMotorPowerChanged));

        static void OnMotorPowerChanged(DependencyObject source, DependencyPropertyChangedEventArgs ea)
        {
            float p = (float)source.GetValue(MotorPowerProperty);
            Trace.WriteLine($"New power={p}", "3");            
            _theInstance.Dispatcher.Invoke(()=> { _theInstance.Pilot.Send(new { Cmd = "Pwr", M1 = p, M2 = p }); } );
        }

        [UiButton("Cancel Move", "White", "Red")]
        public void Cancel(object sender, RoutedEventArgs e)
        {
            cancelFlag = true;
        }

        [UiButton("Straight 1M", "White", "Magenta")]
        public void Straight_1M(object sender, RoutedEventArgs e)
        {
            float distGoal = 1F;
            Trace.WriteLine("::Straight_1M");
            float startX = X, startY = Y, startH = H;

            DateTime lastTime = DateTime.Now;
            previousIntegral = previousDerivative = previousError = 0F;

            for (cancelFlag = false; !cancelFlag;)
            {
                DateTime nowTime = DateTime.Now;
                TimeSpan elapsed = nowTime - lastTime;
                float min = distGoal - travelThreshold,
                    dist = Math.Abs(distance(startX, X, startY, Y));
                bool arrived = dist >= min;
                if (arrived)
                {
                    Trace.WriteLine($" Arrive");
                    Pilot.Send(new { Cmd = "Pwr", M1 = 0, M2 = 0 });
                    break;
                }

                Adjustment = constrain(Adjustment, -10, 10);
                Trace.WriteLine($" Adjust {Adjustment} M1({40 - Adjustment}) M1({40 + Adjustment})");
                Pilot.Send(new { Cmd = "Pwr", M1 = 40.0 - Adjustment, M2 = 40.0 + Adjustment });
                DoEvents();
                System.Threading.Thread.Sleep(100);
                DoEvents();
                lastTime = nowTime;
            }
        }


        [UiButton("Back 1M", "White", "Magenta")]
        public void Back_1M(object sender, RoutedEventArgs e)
        {
            float distGoal = 1F;
            Trace.WriteLine("::Back_1M");
            float startX = X, startY = Y, startH = H;

            DateTime lastTime = DateTime.Now;
            previousIntegral = previousDerivative = previousError = 0F;

            for (cancelFlag = false; !cancelFlag;)
            {
                DateTime nowTime = DateTime.Now;
                TimeSpan elapsed = nowTime - lastTime;
                float min = distGoal - travelThreshold,
                    dist = Math.Abs(distance(startX, X, startY, Y));
                bool arrived = dist >= min;
                if (arrived)
                {
                    Trace.WriteLine($" Arrive");
                    Pilot.Send(new { Cmd = "Pwr", M1 = 0, M2 = 0 });
                    break;
                }

                Adjustment = -simplePid(startH - H, Kp_steer, Ki_steer, Kd_steer, elapsed);
                Adjustment = constrain(Adjustment, -10, 10);
                Trace.WriteLine($" Adjust {Adjustment} M1({-40 - Adjustment}) M1({-40 + Adjustment})");
                Pilot.Send(new { Cmd = "Pwr", M1 = -40.0 - Adjustment, M2 = -40.0 + Adjustment });
                DoEvents();
                System.Threading.Thread.Sleep(100);
                DoEvents();
                lastTime = nowTime;
            }
        }

        private void ReceivedMoved(dynamic j)
        {
            string v = j["Value"].asBoolean() ? "True" : "False";
            Trace.WriteLine($"ReceivedMoved ({v})");
        }

        private float constrain(float adjust, int v1, int v2)
        {
            return Math.Max(Math.Min(v2, adjust), v1);
        }

        static float distance(float startX, float x, float startY, float y)
        {
            return (float)(Math.Sqrt((x - startX) * (x - startX) + (y - startY) * (y - startY)));
        }

        float previousIntegral, previousDerivative, previousError;
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