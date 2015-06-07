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

namespace pilot_test
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        
        static MainWindow _instance;

        #region dp

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
                new PropertyMetadata(new OxyPlot.PlotModel { LegendBorder=OxyPlot.OxyColors.Black }));

        public bool? SerialIsOpen
        {
            get { return Serial != null && Serial.IsOpen; }
            set { SetValue(SerialIsOpenProperty, value); }
        }
        public static readonly DependencyProperty SerialIsOpenProperty =
            DependencyProperty.Register("SerialIsOpen", typeof(bool?), typeof(MainWindow), new PropertyMetadata(false));

        public float MotorMax
        {
            get { return (float)GetValue(MotorMaxProperty); }
            set { SetValue(MotorMaxProperty, value); }
        }
        public static readonly DependencyProperty MotorMaxProperty =
            DependencyProperty.Register("MotorMax", typeof(float), typeof(MainWindow), new PropertyMetadata(Settings.Default.MotorMax));

        #endregion

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
            InitializeComponent();
            _instance = this;
            motorPanel1.MotorPower = 0;

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
            Settings.Default.Kp = motorPanel1.Kp;
            Settings.Default.Ki = motorPanel1.Ki;
            Settings.Default.Kd = motorPanel1.Kd;
            Settings.Default.Save();
        }

        void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ButtonBase b;
            spiked3.Console.MessageLevel = 4;   // default
            Trace.WriteLine("Pilot v2 Test / QA", "+");

            foreach (MemberInfo mi in GetType().GetMembers())
            {
                var attrs = mi.GetCustomAttributes(typeof(UiButton), true);
                if (attrs.Length > 0)
                {
                    var attr = attrs[0] as UiButton;
                    if (attr.isToggle)
                        b = new ToggleButton { Content = attr.Name, Foreground = attr.Fg, Background = attr.Bg };
                    else
                        b = new Button { Content = attr.Name, Foreground = attr.Fg, Background = attr.Bg };
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
            M1PlotModel.Series.Add(new OxyPlot.Series.LineSeries { Title = "Pwr" });
            M2PlotModel.Axes.Add(new OxyPlot.Axes.DateTimeAxis { });
            M2PlotModel.Axes.Add(new OxyPlot.Axes.LinearAxis { });
            M2PlotModel.Series.Add(new OxyPlot.Series.LineSeries { Title = "Tgt" });
            M2PlotModel.Series.Add(new OxyPlot.Series.LineSeries { Title = "Vel" });
            M2PlotModel.Series.Add(new OxyPlot.Series.LineSeries { Title = "Pwr" });

            Joy1.JoystickMovedListeners += GamepadHandler;
        }

        double lastJoyM1, lastJoyM2;

        private void GamepadHandler(rChordata.DiamondPoint p)
        {
            if (p.Left != lastJoyM1 || p.Right != lastJoyM2)
            {
                SendPilot(new { Cmd = "Pwr", M1 = p.Left, M2 = p.Right });
                lastJoyM1 = p.Left;
                lastJoyM2 = p.Right;
            }
        }

        void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (Serial != null && Serial.IsOpen)
                Serial.Close();
            if (Mq != null && Mq.IsConnected)
                MqttClose();
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

        // ---------------------- things

        void ProcessLine(string line)
        {
            Trace.WriteLine("com->" + line.Trim(new char[] { '\r', '\n' }));
            try
            {
                dynamic j = JsonConvert.DeserializeObject(line);
                if (j != null)
                {
                    string type = j["T"];
                    if (type.Equals("Heartbeat"))
                        Dispatcher.InvokeAsync(() => { HeartBeat(j); });
                    if (type.Equals("Pose"))
                        Dispatcher.InvokeAsync(() => { ReceivePose(j); });
                }
            }
            catch (Exception)
            {
            }
        }

        private void HeartBeat(dynamic j)
        {
#if false
            return;
#endif
            const int maxPoints = 100;

            DateTime nowTime = DateTime.Now;
            LineSeries m1t = M1PlotModel.Series[0] as LineSeries;
            LineSeries m1v = M1PlotModel.Series[1] as LineSeries;
            LineSeries m1p = M1PlotModel.Series[2] as LineSeries;
            LineSeries m2t = M2PlotModel.Series[0] as LineSeries;
            LineSeries m2v = M2PlotModel.Series[1] as LineSeries;
            LineSeries m2p = M2PlotModel.Series[2] as LineSeries;

            if (m1t.Points.Count > maxPoints) m1t.Points.RemoveAt(0);
            if (m1v.Points.Count > maxPoints) m1v.Points.RemoveAt(0);
            if (m1p.Points.Count > maxPoints) m1p.Points.RemoveAt(0);
            if (m2t.Points.Count > maxPoints) m2t.Points.RemoveAt(0);
            if (m2v.Points.Count > maxPoints) m2v.Points.RemoveAt(0);
            if (m2p.Points.Count > maxPoints) m2p.Points.RemoveAt(0);

            m1t.Points.Add(new DataPoint(DateTimeAxis.ToDouble(nowTime), (double)j["T1"]));
            m1v.Points.Add(new DataPoint(DateTimeAxis.ToDouble(nowTime), (double)j["V1"]));
            m1p.Points.Add(new DataPoint(DateTimeAxis.ToDouble(nowTime), (double)j["P1"]));
            m2t.Points.Add(new DataPoint(DateTimeAxis.ToDouble(nowTime), (double)j["T2"]));
            m2v.Points.Add(new DataPoint(DateTimeAxis.ToDouble(nowTime), (double)j["V2"]));
            m2p.Points.Add(new DataPoint(DateTimeAxis.ToDouble(nowTime), (double)j["P2"]));

            OxyM1.InvalidatePlot();
            OxyM2.InvalidatePlot();            
        }

        // ---------------------- commands

        [UiButton("Serial", "Black", "White", isToggle = true)]
        public void ToggleButton_Serial(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("::ToggleButton_Serial");
            if ((sender as ToggleButton).IsChecked ?? false)
                SerialOpen();
            else
                SerialClose();
        }

        [UiButton("MQTT", "Black", "White", isToggle = true)]
        public void ToggleButton_MQTT(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("::ToggleButton_MQTT");
            if ((sender as ToggleButton).IsChecked ?? false)
                MqttOpen();
            else
                MqttClose();
        }

        [UiButton("ESTOP", "White", "Red")]
        public void EStop_Click(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("::EStop_Click");
            SendPilot(new { Cmd = "Esc", Value = 0 });
            SendPilot(new { Cmd = "Pwr", M1 = 0, M2 = 0 });
        }

        [UiButton("Esc", "Black", "White", isToggle = true)]
        public void ToggleButton_Esc(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("::ToggleButton_Esc");
            int OnOff = (sender as ToggleButton).IsChecked ?? false ? 1 : 0;
            SendPilot(new { Cmd = "Esc", Value = OnOff });
        }

        [UiButton("Reset", "Black", "Yellow")]
        public void ResetPose_Click(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("::Button_ResetPose");
            SendPilot(new { Cmd = "Reset" });
        }

        [UiButton("PID")]
        public void Pid_Click(object sender, RoutedEventArgs e)
        {
            // critical you include the decimal point (json decoding rqmt)
            Trace.WriteLine("::Pid_Click");
            SendPilot(new { Cmd = "Config", PID = new float[] { motorPanel1.Kp, motorPanel1.Ki, motorPanel1.Kd }});
        }

        [UiButton("Geom")]
        public void Geom_Click(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("::Geom_Click");
            // old: SendPilot(new { Cmd = "Geom", TPR = 60, Diam = 175.0F, Base = 220.0F, mMax = 450 } });
            // new: ticks per meter, motormax ticks per second 
            SendPilot(new { Cmd = "Config", Geom = new float[] { (float)( (1000 / (Math.PI * 175) * 60) ),  500F } });
        }

        [UiButton("Cali")]
        public void Cali_Click(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("::Cali_Click");
            SendPilot(new { Cmd = "Config" , MPU = new float[] { -333, -3632, 2311, -1062, 28, -11 } });
        }

        [UiButton("Power")]
        public void Power_Click(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("::Power_Click");
            SendPilot(new { Cmd = "Pwr", M1 = motorPanel1.MotorPower, M2 = motorPanel1.MotorPower });
        }

        [UiButton("HB Off")]
        public void HbOff_Click(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("::Button_HbOff");
            SendPilot(new { Cmd = "Heartbeat", Value = 0, Int = 2000 });
        }

        [UiButton("HB 500")]
        public void Hb500_Click(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("::Button_Hb500");
            SendPilot(new { Cmd = "Heartbeat", Value = 1, Int = 500 });
        }

        [UiButton("HB 2000")]
        public void Hb2000_Click(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("::Button_Hb2000");
            SendPilot(new { Cmd = "Heartbeat", Value = 1, Int = 2000 });
        }

        [UiButton("Bumper", "Black", "White", isToggle = true)]
        public void ToggleButton_Bumper(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("::ToggleButton_Bumper");
            int OnOff = (sender as ToggleButton).IsChecked ?? false ? 1 : 0;
            SendPilot(new { Cmd = "Bumper", Value = OnOff });
        }

        //[UiButton("Rel Hdg")]
        //public void HdgRel_Click(object sender, RoutedEventArgs e)
        //{
        //    Trace.WriteLine("::HdgRel_Click");
        //    SendPilot(new { Cmd = "Rot", Rel = 45 });
        //}

        //[UiButton("Travel")]
        //public void Travel_Click(object sender, RoutedEventArgs e)
        //{
        //    Trace.WriteLine("::Travel_Click");
        //    SendPilot(new { Cmd = "Travel", Dist = 2.0, Spd = 50.0 });
        //}
    }
}