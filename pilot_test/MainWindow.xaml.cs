using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
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

using Newtonsoft.Json;
using System.Windows.Threading;
using System.Windows.Controls.Primitives;
using System.IO;
using System.Threading;
using pilot_test.Properties;

namespace pilot_test
{
    public partial class MainWindow : Window
    {
        private SerialPort Serial;
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
        public bool? SerialIsOpen
        {
            get { return Serial != null && Serial.IsOpen; }
            set { SetValue(SerialIsOpenProperty, value); }
        }
        public static readonly DependencyProperty SerialIsOpenProperty =
            DependencyProperty.Register("SerialIsOpen", typeof(bool?), typeof(MainWindow), new PropertyMetadata(false));

        public float MotorPower
        {
            get { return (float)GetValue(MotorPowerProperty); }
            set { SetValue(MotorPowerProperty, value); }
        }
        public static readonly DependencyProperty MotorPowerProperty =
            DependencyProperty.Register("MotorPower", typeof(float), typeof(MainWindow), new PropertyMetadata(Settings.Default.MotorPower));

        public float MotorMin
        {
            get { return (float)GetValue(MotorMinProperty); }
            set { SetValue(MotorMinProperty, value); }
        }
        public static readonly DependencyProperty MotorMinProperty =
            DependencyProperty.Register("MotorMin", typeof(float), typeof(MainWindow), new PropertyMetadata(Settings.Default.MotorMin));

        public float MotorMax
        {
            get { return (float)GetValue(MotorMaxProperty); }
            set { SetValue(MotorMaxProperty, value); }
        }
        public static readonly DependencyProperty MotorMaxProperty =
            DependencyProperty.Register("MotorMax", typeof(float), typeof(MainWindow), new PropertyMetadata(Settings.Default.MotorMax));


        int recvIdx = 0;
        byte[] recvbuf = new byte[1024];

        public MainWindow()
        {
            InitializeComponent();

            RestoreVars();
        }

        void RestoreVars()
        {
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
            Settings.Default.MotorMin = MotorMin;
            Settings.Default.MotorPower = MotorPower;
            Settings.Default.Kp = Kp;
            Settings.Default.Ki = Ki;
            Settings.Default.Kd = Kd;
            Settings.Default.Save();
        }

        private void Button_Serial(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("::Button_Serial");
            if ((sender as ToggleButton).IsChecked ?? false)
            {
                Serial = new SerialPort("com4", 115200);
                try
                {
                    Serial.Open();
                    Serial.WriteTimeout = 200;
                    SerialHandler(Serial);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex.Message);
                }
                SerialIsOpen = Serial.IsOpen;
            }
            else
            {
                if (Serial != null && Serial.IsOpen)
                {
                    Serial.Close();
                    SerialIsOpen = Serial.IsOpen;
                }
            }
        }

        void ProcessLine(string line)
        {
            // comments ok, echo'd
            if (line.StartsWith("//"))
            {
                Trace.WriteLine("com->" + line);
                return;
            }

            if (line.Length < 3)
            {
                Trace.WriteLine(string.Format("{0} framing error", Serial.PortName), "warn");
                // attempt to recover
                bool recovered = false;
                while (!recovered)
                {
                    DoEvents();
                    try
                    {
                        Serial.ReadTo("\n");
                        recovered = true;
                    }
                    catch (Exception)
                    { }
                }
                return;
            }
            else
            {
                Trace.WriteLine("com->" + line);
                string type = "";
                try
                {
                    dynamic j = JsonConvert.DeserializeObject(line);
                    type = (string)j["T"];
                }
                catch (JsonException je)
                {
                    System.Diagnostics.Trace.WriteLine(je.Message);
                }
                switch (type)
                {
                    case "Log":
                        break;
                    case "???":
                        break;
                }
            }
        }

        void AppSerialDataEvent(byte[] received)
        {
            foreach (var b in received)
            {
                if (b == '\r')
                    continue;
                else if (b == '\n')
                {
                    recvbuf[recvIdx] = 0;
                    string line = Encoding.UTF8.GetString(recvbuf, 0, recvIdx); // makes a copy
                    Dispatcher.InvokeAsync(() => { ProcessLine(line); });
                    recvIdx = 0;
                    continue;
                }
                else
                    recvbuf[recvIdx] = (byte)b;

                recvIdx++;
                if (recvIdx >= recvbuf.Length)
                {
                    System.Diagnostics.Debugger.Break();    // overflow
                    // +++ atempt recovery
                }
            }
        }

        void SerialHandler(SerialPort port)
        {
            byte[] buffer = new byte[1024];
            Action kickoffRead = null;
            kickoffRead = delegate
            {
                port.BaseStream.BeginRead(buffer, 0, buffer.Length, delegate(IAsyncResult ar)
                {
                    if (port.IsOpen)
                    {
                        try
                        {
                            int actualLength = port.BaseStream.EndRead(ar);
                            byte[] received = new byte[actualLength];
                            Buffer.BlockCopy(buffer, 0, received, 0, actualLength);
                            AppSerialDataEvent(received);
                        }
                        catch (Exception exc)
                        {
                            Trace.WriteLine(exc.Message);
                        }
                        kickoffRead();
                    }
                }, null);
                
            };
            kickoffRead();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            spiked3.Console.MessageLevel = 4;   // default

            RestoreVars();

            Trace.WriteLine("::Window_Loaded");
            Trace.WriteLine("Pilot v2 Test/QA", "+");
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (Serial != null && Serial.IsOpen)
                Serial.Close();

            SaveVars();
        }

        void SerialSend(string t)
        {
            if (Serial != null && Serial.IsOpen)
            {
                Trace.WriteLine("com<-" + t);
                Serial.WriteLine(t);
                System.Threading.Thread.Sleep(200);  //  really needed :(
                DoEvents();
            }
        }

        // yeah yeah, I know; everyone thinks do events is bad,
        // but this is test software not production. we generate a lot of UI traffic in our click handler we want to see
        // without resorting to threads
        void DoEvents()
        {
            Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate { }));
        }

        private void Test1_Click(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("::Button_Test1");
            tglEsc.IsChecked = true;

            SerialSend(@"{""T"" : ""Cmd"", ""Cmd"" : ""M"", ""1"" : 50, ""2"" : 50}");  // raw M1/2 Power
            Thread.Sleep(1000);
            SerialSend(@"{""T"" : ""Cmd"", ""Cmd"" : ""M"", ""1"" : 75, ""2"" : 35}");  // raw M1/2 Power
            Thread.Sleep(1000);
            SerialSend(@"{""T"" : ""Cmd"", ""Cmd"" : ""M"", ""1"" : 30, ""2"" : 70}");  // raw M1/2 Power
            Thread.Sleep(1000);
            SerialSend(@"{""T"" : ""Cmd"", ""Cmd"" : ""M"", ""1"" : -50, ""2"" : -50}");  // raw M1/2 Power
            Thread.Sleep(1000);
            SerialSend(@"{""T"" : ""Cmd"", ""Cmd"" : ""M"", ""1"" : 50, ""2"" : -50}");  // raw M1/2 Power
            Thread.Sleep(1000);
            SerialSend(@"{""T"" : ""Cmd"", ""Cmd"" : ""M"", ""1"" : -50, ""2"" : 50}");  // raw M1/2 Power
            Thread.Sleep(1000);
            SerialSend(@"{""T"" : ""Cmd"", ""Cmd"" : ""M"", ""1"" : 0, ""2"" : 0}");  // raw M1/2 Power
        }

        private void M1_Click(object sender, RoutedEventArgs e)
        {
            //SerialSend(@"{""Cmd"":""Test2""}");
            tglEsc.IsChecked = true;
            string t = @"{""Cmd"" : ""M"", ""1"":" + MotorPower.ToString() + "}"; Trace.WriteLine(t);
            SerialSend(t);

            //Thread.Sleep(1000);
            //SerialSend(@"{""Cmd"" : ""M"", ""1"":0}");
        }

        private void Test3_Click(object sender, RoutedEventArgs e)
        {
            // critical you include the decimal point (json decoding rqmt)
            Trace.WriteLine("::Test3_Click");
            string t = @"{""Cmd"" : ""PID"", ""Idx"":0,""P"":" + Kp.ToString() + @",""I"":" + Ki.ToString() + @",""D"":" + Kd.ToString() + "}"; Trace.WriteLine(t);
            SerialSend(t);
        }

        private void Test4_Click(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("::Test4_Click");
            SerialSend(@"{""Cmd"" : ""Geom"", ""TPR"":60,""Diam"":175.,""Base"":220.,""mMax"":450}");
        }

        private void Button_HbOff(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("::Button_HbOff");
            SerialSend(@"{""Cmd"":""Heartbeat"",""Value"":0}");
        }

        private void Button_Hb500(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("::Button_Hb500");
            SerialSend(@"{""Cmd"":""Heartbeat"",""Value"":1,""Int"":500}");
        }

        private void Button_Hb2000(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("::Button_Hb2000");
            SerialSend(@"{""Cmd"":""Heartbeat"",""Value"":1,""Int"":2000}");
        }

        private void Button_ResetPose(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("::Button_ResetPose");
            SerialSend(@"{""Cmd"":""Reset""}");
        }

        private void Button_EStop(object sender, RoutedEventArgs e)
        {
            SerialSend(@"{""Cmd"":""Esc"",""Value"":0}");
            SerialSend(@"{""Cmd"":""M"",""1"":0,""2"":0}");
            tglEsc.IsChecked = false;
        }

		private void ToggleButton_Esc(object sender, RoutedEventArgs e)
		{
			Trace.WriteLine("::ToggleButton_Esc");
			int OnOff = (sender as ToggleButton).IsChecked ?? false ? 1 : 0;
			SerialSend(@"{""Cmd"":""Esc"",""Value"":" + OnOff + "}");
		}

		private void ToggleButton_Bumper(object sender, RoutedEventArgs e)
		{
			Trace.WriteLine("::ToggleButton_Bumper");
			int OnOff = (sender as ToggleButton).IsChecked ?? false ? 1 : 0;
			SerialSend(@"{""Cmd"":""Bumper"",""Value"":" + OnOff + "}");
		}
	}
}