using System;
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
using NDesk.Options;
using Spiked3;
using System.Windows.Data;

// todo open separate oxyplot window if/when received telemetry (and elliminate it from UI)

// DoEvents:  Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate { }));

namespace pilot_test
{

    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public string PilotString
        {
            get { return (string)GetValue(PilotStringProperty); }
            set { SetValue(PilotStringProperty, value); }
        }
        public static readonly DependencyProperty PilotStringProperty =
            DependencyProperty.Register("PilotString", typeof(string), typeof(MainWindow), new PropertyMetadata("127.0.0.1"));

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

        public PidData MotorPid
        {
            get { return (PidData)GetValue(MotorPidProperty); }
            set { SetValue(MotorPidProperty, value); }
        }
        public static readonly DependencyProperty MotorPidProperty =
            DependencyProperty.Register("MotorPid", typeof(PidData), typeof(MainWindow), new PropertyMetadata(new PidData()));

        public PidData HdgPid
        {
            get { return (PidData)GetValue(HdgPidProperty); }
            set { SetValue(HdgPidProperty, value); }
        }
        public static readonly DependencyProperty HdgPidProperty =
            DependencyProperty.Register("HdgPid", typeof(PidData), typeof(MainWindow), new PropertyMetadata(new PidData()));

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
                new PropertyMetadata(new OxyPilot() { LegendBorder = OxyColors.Black }));

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

        //---W

        public static MainWindow _theInstance;
        Pilot Pilot;
        
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
            MotorPid.Kp = Settings.Default.MotorKp;
            MotorPid.Ki = Settings.Default.MotorKi;
            MotorPid.Kd = Settings.Default.MotorKd;

            HdgPid.Kp = Settings.Default.HdgKp;
            HdgPid.Ki = Settings.Default.HdgKi;
            HdgPid.Kd = Settings.Default.HdgKd;
            
            InitializeComponent();

            motorPid1.Click += mototPid1_Click;
            hdgPid1.Click += hdgPid1_Click;
            MotorPower = 0;

            var p = new OptionSet
            {
                   { "pilot=", (v) => { PilotString = v; } },
            };

            p.Parse(Environment.GetCommandLineArgs());

            Width = Settings.Default.Width;
            Height = Settings.Default.Height;
            Top = Settings.Default.Top;
            Left = Settings.Default.Left;
            mainGrid.RowDefinitions[0].Height = new GridLength(Settings.Default.Split1);

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
            Settings.Default.Split1 = mainGrid.RowDefinitions[0].Height.Value;

            Settings.Default.MotorKp = MotorPid.Kp;
            Settings.Default.MotorKi = MotorPid.Ki;
            Settings.Default.MotorKd = MotorPid.Kd;

            Settings.Default.HdgKp = HdgPid.Kp;
            Settings.Default.HdgKi = HdgPid.Ki;
            Settings.Default.HdgKd = HdgPid.Kd;

            Settings.Default.TurnH = TurnH;
            Settings.Default.TurnPwr = TurnPwr;

            Settings.Default.Save();
        }

        void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ButtonBase b;
            spiked3.Console.MessageLevel = 4;   // default
            Trace.WriteLine("Pilot Test Program", "+");

            TextBox tb = new TextBox { Width = 96, VerticalContentAlignment = System.Windows.VerticalAlignment.Center };
            tb.SetBinding(TextBox.TextProperty, new Binding { Source = this, Path = new PropertyPath("PilotString") } );
            b = new ToggleButton { Content = tb, Foreground = Brushes.Black, Background = Brushes.White, Style = (Style)FindResource("UiToggle"), Width = 128 };
            b.Click += PilotButton_Click;
            CommandList.Add(b);

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
        }

        private void Pilot_OnReceive(dynamic j)
        {
            Dispatcher.InvokeAsync(() =>
            {
                string t = JsonConvert.SerializeObject(j);
                Trace.WriteLine(t, "5");
            });

            switch ((string)(j.T))
            {
                case "TELEM":
                    if ((string)j.V == "1")
                        Dispatcher.InvokeAsync(() => { oxy1Model.Reset(j);; });
                    break;
                case "Log":
                case "Error":
                case "Debug":
                    Dispatcher.InvokeAsync(() => { RecieveLog(j); });
                    break;
                case "Telemetry":
                    Dispatcher.InvokeAsync(() => { oxy1Model.Append(oxy1, j); });
                    break;
                case "Pose":
                    Dispatcher.InvokeAsync(() => { ReceivedPose(j); });
                    break;
                case "Move":
                case "Rotate":
                case "Bumper":
                    Dispatcher.InvokeAsync(() => { ReceivedEvent(j); });
                    break;
                default:
                    Debugger.Break();
                    throw new NotImplementedException();
            }
        }

        private void RecieveLog(dynamic j)
        {
            bool isError = j.T == "Error";
            bool isDebug = j.T == "Debug";
            string payload = j.V;
            if (!isDebug)
                Trace.WriteLine(payload, isError ? "error" : "+");
            else
                ; // Trace.WriteLine(j, "2");
        }

        private void ReceivedPose(dynamic j)
        {
            X = j.X;
            Y = j.Y;
            H = j.H;
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

        void _T([CallerMemberName] String T = "")
        {
            Trace.WriteLine("::" + T);
        }

        // ---------------------- commands

        public void PilotButton_Click(object sender, RoutedEventArgs e)
        {
            //_T();
            if (((ToggleButton)sender)?.IsChecked ?? false)
            {
                if (Pilot != null)
                {
                    Pilot.OnPilotReceive -= Pilot_OnReceive;
                    Pilot.Close();
                }

                Pilot = Pilot.Factory(PilotString);
                Pilot.OnPilotReceive += Pilot_OnReceive;
            }
            else
            {
                if (Pilot != null)
                {
                    Pilot.Close();
                    Pilot.OnPilotReceive -= Pilot_OnReceive;
                }
            }

            CommStatus = Pilot.CommStatus;

        }

        //[UiButton("Serial", "Black", "White", isToggle = true)]
        //public void toggleButton_Serial(object sender, RoutedEventArgs e)
        //{
        //    //_T();
        //    if ((sender as ToggleButton).IsChecked ?? false)
        //    {
        //        Pilot = Pilot.Factory("com15");
        //        Pilot.OnPilotReceive += Pilot_OnReceive;
        //        CommStatus = Pilot.CommStatus;
        //    }
        //    else if (Pilot != null)
        //    {
        //        Pilot.OnPilotReceive -= Pilot_OnReceive;
        //        Pilot.Close();
        //    }
        //}

        //[UiButton("Pilot", "Black", "White", isToggle = true)]
        //public void toggleButton_MQTT(object sender, RoutedEventArgs e)
        //{
        //    //_T();
        //    if ((sender as ToggleButton).IsChecked ?? false)
        //    {
        //        Pilot = Pilot.Factory(PilotString);
        //        Pilot.OnPilotReceive += Pilot_OnReceive;
        //        CommStatus = Pilot.CommStatus;
        //    }
        //    else if (Pilot != null)
        //    {
        //        Pilot.OnPilotReceive -= Pilot_OnReceive;
        //        Pilot.Close();
        //    }
        //}

        int telemFlag = 0;

        [UiButton("Telem\nCycle", "Black", "White")]
        public void telem_Click(object sender, RoutedEventArgs e)
        {
            _T();
            Pilot.Send(new { Cmd = "TELEM", Flag = (++telemFlag % 5) });
        }

        [UiButton("Esc", "White", "Red", isToggle = true)]
        public void toggleButton_Esc(object sender, RoutedEventArgs e)
        {
            _T();
            int OnOff = (sender as ToggleButton).IsChecked ?? false ? 1 : 0;
            Pilot.Send(new { Cmd = "ESC", Value = OnOff });
        }

        [UiButton("Reset", "Black", "Yellow")]
        public void resetPose_Click(object sender, RoutedEventArgs e)
        {
            _T();
            Pilot.Send(new { Cmd = "RESET" });
            // we should rcv a pose            
        }

        private void hdgPid1_Click(object sender, EventArgs e)
        {
            _T();
            Pilot.Send(new { Cmd = "CONFIG", hPID = new float[] { HdgPid.Kp, HdgPid.Ki, HdgPid.Kd } });
        }

        public void mototPid1_Click(object sender, EventArgs e)
        {
            _T();
            Pilot.Send(new { Cmd = "CONFIG", mPID = new float[] { MotorPid.Kp, MotorPid.Ki, MotorPid.Kd } });
        }

        [UiButton("Config", "Black", "Cyan")]
        public void config_Click(object sender, RoutedEventArgs e)
        {
            _T();
            //Pilot.Send(new { Cmd = "CONFIG", MPU = new int[] { -4526, -136, 1990, 48, -26, -21 } });
            // mikes original
            Pilot.Send(new { Cmd = "CONFIG", TPM = 353, MMX = 450, StrRv = -1 });
            Pilot.Send(new { Cmd = "CONFIG", M1 = new int[] { 1, -1 }, M2 = new int[] { -1, 1 } });
            // daves
            //Pilot.Send(new { Cmd = "CONFIG", TPM = 353, MMX = 450, StrRv = 1 });
            //Pilot.Send(new { Cmd = "CONFIG", M1 = new int[] { 1, 1 }, M2 = new int[] { -1, -1 } });

        }

        [UiButton("TPM Calc", "White", "Orange")]
        public void TpmCalculator(object sender, EventArgs e)
        {
            var d = new TpmCalc { CurrentTPM = 336, Owner = this, WindowStartupLocation = WindowStartupLocation.CenterOwner };
            if (d.ShowDialog() ?? false)
            {
                // actually we dont do anything with the results, up to the user/programmer
            }
        }

        const float RotatePower = 50.0F, MovePower = 40.0F;

        [UiButton("ROT\n180", "White", "Blue")]
        public void rotaTest_Click(object sender, RoutedEventArgs e)
        {
            _T();
            Pilot.Send(new { Cmd = "ROT", Hdg = H + 180.0F, Pwr = RotatePower });
        }

        [UiButton("ROT\n+10", "White", "Blue")]
        public void rotPlus10_Click(object sender, RoutedEventArgs e)
        {
            _T();
            Pilot.Send(new { Cmd = "ROT", Hdg = H + 10.0F, Pwr = RotatePower });
        }

        [UiButton("ROT\n-30", "White", "Blue")]
        public void rotMinus30_Click(object sender, RoutedEventArgs e)
        {
            _T();
            Pilot.Send(new { Cmd = "ROT", Hdg = H - 30.0F, Pwr = RotatePower });
        }

        [UiButton("MOV\n3@H", "White", "Green")]
        public void movTest_Click(object sender, RoutedEventArgs e)
        {
            _T();
            Pilot.Send(new { Cmd = "MOV", Dist = 3, Pwr = 40.0F });
        }

        [UiButton("MOV\n3@0", "White", "Green")]
        public void movTest2_Click(object sender, RoutedEventArgs e)
        {
            _T();
            Pilot.Send(new { Cmd = "MOV", Dist = 3, Pwr = 30.0F, Hdg = 0 });
        }

        [UiButton("ROT\n+90", "White", "Blue")]
        public void rotPlus90(object sender, RoutedEventArgs e)
        {
            _T();
            Pilot.Send(new { Cmd = "ROT", Hdg = H + 90.0F, Pwr = RotatePower });
        }

        [UiButton("ROT\n-90", "White", "Blue")]
        public void rotMinus90(object sender, RoutedEventArgs e)
        {
            _T();
            Pilot.Send(new { Cmd = "ROT", Hdg = H - 90.0F, Pwr = RotatePower });
        }

        public void Power_Click(object sender, RoutedEventArgs e)
        {
            _T();
            Pilot.Send(new { Cmd = "MOV", M1 = MotorPower, M2 = MotorPower });
        }

        static void OnMotorPowerChanged(DependencyObject source, DependencyPropertyChangedEventArgs ea)
        {
            float p = (float)source.GetValue(MotorPowerProperty);
            Trace.WriteLine($"New power={p}", "3");            
            _theInstance.Dispatcher.Invoke(()=> { _theInstance.Pilot.Send(new { Cmd = "MOV", M1 = p, M2 = p }); } );
        }

        private void ReceivedEvent(dynamic j)
        {
            Trace.WriteLine($"Received Event ({j.T}) Value ({j.V})");
        }

        private void Power0_Click(object sender, RoutedEventArgs e)
        {
            _T();
            Pilot.Send(new { Cmd = "MOV", M1 = 0, M2 = 0 });
        }

        [UiButton("Srvo 90", "White", "Orange")]
        public void srvo90(object sender, RoutedEventArgs e)
        {
            _T();
            Pilot.Send(new { Cmd = "SRVO", Value = 90 });
        }

        [UiButton("Srvo 10", "White", "Orange")]
        public void srvo10(object sender, RoutedEventArgs e)
        {
            _T();
            Pilot.Send(new { Cmd = "SRVO", Value = 10 });
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