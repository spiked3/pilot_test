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

namespace pilot_test
{
    public partial class MainWindow : Window
    {
        private SerialPort Serial;

        public bool? SerialIsOpen
        {
            get { return Serial != null && Serial.IsOpen; }
            set { SetValue(SerialIsOpenProperty, value); }
        }

        public static readonly DependencyProperty SerialIsOpenProperty =
            DependencyProperty.Register("SerialIsOpen", typeof(bool?), typeof(MainWindow), new PropertyMetadata(false));

        int recvIdx = 0;
        byte[] recvbuf = new byte[1024];

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_OpenSerial(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("::Button_OpenSerial");
            Serial = new SerialPort("com3", 115200);
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
                if (line.StartsWith("SUB"))
                    return;
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
                    case "EVT":
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

            Trace.WriteLine("::Window_Loaded");
            Trace.WriteLine("Pilot v2 Test/QA", "+");
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Button_CloseSerial(sender, null);
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

        private void Button_CloseSerial(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("::Button_CloseSerial");
            if (Serial != null && Serial.IsOpen)
            {
                Serial.Close();
                SerialIsOpen = Serial.IsOpen;
            }
        }

        // yeah yeah, I know; everyone thinks do events is bad,
        // but this is test software not production. we generate a lot of UI traffic in our click handler we want to see
        // without resorting to threads
        void DoEvents()
        {
            Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate { }));
        }

        //----------------------------------------------------------------

        private void Button_Test1(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("::Button_Test1");
            //SerialSend(@"{""T"":""Cmd"",""Cmd"":""Test1""}");
            SerialSend(@"{""T"" : ""Cmd"", ""Cmd"" : ""Esc"", ""Value"" : 1}");
            SerialSend(@"{""T"" : ""Cmd"", ""Cmd"" : ""Power"", ""Value"" : 50}");
            System.Threading.Thread.Sleep(2000);
            SerialSend(@"{""T"" : ""Cmd"", ""Cmd"" : ""Power"", ""Value"" : 0}");
            SerialSend(@"{""T"" : ""Cmd"", ""Cmd"" : ""Esc"", ""Value"" : 0}");
        }

        private void Test2_Click(object sender, RoutedEventArgs e)
        {
            SerialSend(@"{""T"":""Cmd"",""Cmd"":""Test2""}");
        }
        
        //----------------------------------------------------------------

        private void Button_HbOff(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("::Button_HbOff");
            SerialSend(@"{""T"":""Cmd"",""Cmd"":""Heartbeat"",""Value"":0}");
        }

        private void Button_Hb500(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("::Button_Hb500");
            SerialSend(@"{""T"":""Cmd"",""Cmd"":""Heartbeat"",""Value"":1,""Int"":500}");
        }

        private void Button_Hb2000(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("::Button_Hb2000");
            SerialSend(@"{""T"":""Cmd"",""Cmd"":""Heartbeat"",""Value"":1,""Int"":2000}");
        }

        private void Button_MMax100(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("::Button_MMax100");
            SerialSend(@"{""T"":""Cmd"",""Cmd"":""MMax"",""Value"":100}");
        }

        private void Button_MMax80(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("::Button_MMax80");
            SerialSend(@"{""T"":""Cmd"",""Cmd"":""MMax"",""Value"":80}");
        }

        private void Button_Sweep(object sender, RoutedEventArgs e)
        {
            const int dly = 200;
            Trace.WriteLine("::Button_Sweep");
            SerialSend(@"{""T"":""Cmd"",""Cmd"":""Esc"",""Value"":1}");

            for (int i = 10; i <= 100; i += 10)
            {
                SerialSend(@"{""T"":""Cmd"",""Cmd"":""Power"",""Value"":" + i + "}");
                Thread.Sleep(dly);
            }
            for (int i = 100; i >= -100; i -= 10)
            {
                SerialSend(@"{""T"":""Cmd"",""Cmd"":""Power"",""Value"":" + i + "}");
                Thread.Sleep(dly);
            }
            for (int i = -100; i <= 0; i += 10)
            {
                SerialSend(@"{""T"":""Cmd"",""Cmd"":""Power"",""Value"":" + i + "}");
                Thread.Sleep(dly);
            }

            SerialSend(@"{""T"":""Cmd"",""Cmd"":""Esc"",""Value"":0}");
        }

        private void Button_ResetPose(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("::Button_ResetPose");
            SerialSend(@"{""T"":""Cmd"",""Cmd"":""Reset""}");
        }

		private void Button_EStop(object sender, RoutedEventArgs e)
		{
			SerialSend(@"{""T"":""Cmd"",""Cmd"":""Esc"",""Value"":0}");
			SerialSend(@"{""T"":""Cmd"",""Cmd"":""Power"",""Value"":0}");
		}

		private void ToggleButton_Esc(object sender, RoutedEventArgs e)
		{
			Trace.WriteLine("::ToggleButton_Esc");
			int OnOff = (sender as ToggleButton).IsChecked ?? false ? 1 : 0;
			SerialSend(@"{""T"":""Cmd"",""Cmd"":""Esc"",""Value"":" + OnOff + "}");
		}

		private void ToggleButton_Bumper(object sender, RoutedEventArgs e)
		{
			Trace.WriteLine("::ToggleButton_Bumper");
			int OnOff = (sender as ToggleButton).IsChecked ?? false ? 1 : 0;
			SerialSend(@"{""T"":""Cmd"",""Cmd"":""Bumper"",""Value"":" + OnOff + "}");
		}
	}
}