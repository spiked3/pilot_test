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

namespace pilot_test
{
    public partial class MainWindow : Window
    {
        private SerialPort Serial;

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
                Serial.ErrorReceived += Serial_ErrorReceived;
                Serial.DataReceived += Serial_DataReceived;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }

        void Serial_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            //System.Diagnostics.Debugger.Break();
            Trace.WriteLine(e.EventType);
        }

        int recvIdx = 0;
        byte[] recvbuf = new byte[2048];

        void DoLine(string line)
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
                Trace.WriteLine("com->" + line);
        }

        private void Serial_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            while (Serial.BytesToRead > 0)
            {
                int c = Serial.ReadByte();

                if (c == '\r')
                    continue;
                else if (c == '\n')
                {
                    recvbuf[recvIdx] = 0;
                    DoLine(Encoding.UTF8.GetString(recvbuf, 0, recvIdx));
                    recvIdx = 0;
                    continue;
                }
                else
                    recvbuf[recvIdx] = (byte)c;

                recvIdx++;
                if (recvIdx >= recvbuf.Length)
                {
                    System.Diagnostics.Debugger.Break();    // overflow
                    // +++ atempt recovery
                }
            }
        }

        void raiseAppSerialDataEvent(byte[] received)
        {

        }

        void newSerialHandler(SerialPort port)
        {
            byte[] buffer = new byte[2048];
            Action kickoffRead = null;
            kickoffRead = delegate
            {
                port.BaseStream.BeginRead(buffer, 0, buffer.Length, delegate(IAsyncResult ar)
                {
                    try
                    {
                        int actualLength = port.BaseStream.EndRead(ar);
                        byte[] received = new byte[actualLength];
                        Buffer.BlockCopy(buffer, 0, received, 0, actualLength);
                        raiseAppSerialDataEvent(received);
                    }
                    catch (IOException exc)
                    {
                        System.Diagnostics.Debugger.Break();
                    }
                    kickoffRead();
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

        private void Button_Test1(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("::Button_Test1");
            //SerialSend(@"{""Topic"":""Cmd/robot1"",""T"":""Cmd"",""Cmd"":""Test1""}");
            SerialSend(@"{""T"" : ""Cmd"", ""Cmd"" : ""Esc"", ""Value"" : 1}");
            SerialSend(@"{""T"" : ""Cmd"", ""Cmd"" : ""Power"", ""Value"" : 40}");
            System.Threading.Thread.Sleep(2000);
            SerialSend(@"{""T"" : ""Cmd"", ""Cmd"" : ""Power"", ""Value"" : 0}");
            SerialSend(@"{""T"" : ""Cmd"", ""Cmd"" : ""Esc"", ""Value"" : 0}");
        }

        private void Button_CloseSerial(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("::Button_CloseSerial");
            if (Serial != null && Serial.IsOpen)
            {
                Serial.DataReceived -= Serial_DataReceived;
                Serial.Close();
            }
        }

        // yeah yeah, I know; everyone thinks do events is bad,
        // but this is test software not production. we generate a lot of UI traffic in our click handler we want to see
        // without resorting to threads
        void DoEvents()
        {
            Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate { }));
        }

        private void Button_HbOff(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("::Button_HbOff");
            SerialSend(@"{""Topic"":""Cmd/robot1"",""T"":""Cmd"",""Cmd"":""Heartbeat"",""Value"":0}");
        }

        private void Button_Hb500(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("::Button_Hb500");
            SerialSend(@"{""Topic"":""Cmd/robot1"",""T"":""Cmd"",""Cmd"":""Heartbeat"",""Value"":1,""Int"":500}");
        }

        private void Button_Hb2000(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("::Button_Hb2000");
            SerialSend(@"{""Topic"":""Cmd/robot1"",""T"":""Cmd"",""Cmd"":""Heartbeat"",""Value"":1,""Int"":2000}");
        }

        private void Button_MMax100(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("::Button_MMax100");
            SerialSend(@"{""Topic"":""Cmd/robot1"",""T"":""Cmd"",""Cmd"":""MMax"",""Value"":100}");
        }

        private void Button_MMax80(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("::Button_MMax80");
            SerialSend(@"{""Topic"":""Cmd/robot1"",""T"":""Cmd"",""Cmd"":""MMax"",""Value"":80}");
        }

        private void Button_Sweep(object sender, RoutedEventArgs e)
        {
            const int dly = 250;
            Trace.WriteLine("::Button_Sweep");
            SerialSend(@"{""Topic"":""Cmd/robot1"",""T"":""Cmd"",""Cmd"":""Esc"",""Value"":1}");

            for (int i = 10; i <= 100; i += 10)
            {
                SerialSend(@"{""Topic"":""Cmd/robot1"",""T"":""Cmd"",""Cmd"":""Power"",""Value"":" + i + "}");
                System.Threading.Thread.Sleep(dly);
            }

            for (int i = 100; i >= -100; i -= 10)
            {
                SerialSend(@"{""Topic"":""Cmd/robot1"",""T"":""Cmd"",""Cmd"":""Power"",""Value"":" + i + "}");
                System.Threading.Thread.Sleep(dly);
            }
            for (int i = -100; i <= 0; i += 10)
            {
                SerialSend(@"{""Topic"":""Cmd/robot1"",""T"":""Cmd"",""Cmd"":""Power"",""Value"":" + i + "}");
                System.Threading.Thread.Sleep(dly);
            }

            SerialSend(@"{""Topic"":""Cmd/robot1"",""T"":""Cmd"",""Cmd"":""Esc"",""Value"":0}");
        }

        private void Button_ResetPose(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("::Button_ResetPose");
            SerialSend(@"{""Topic"":""Cmd/robot1"",""T"":""Cmd"",""Cmd"":""Reset""}");
        }

		private void Button_EStop(object sender, RoutedEventArgs e)
		{
			SerialSend(@"{""Topic"":""Cmd/robot1"",""T"":""Cmd"",""Cmd"":""Esc"",""Value"":0}");
			SerialSend(@"{""Topic"":""Cmd/robot1"",""T"":""Cmd"",""Cmd"":""Power"",""Value"":0}");
		}

		private void ToggleButton_Esc(object sender, RoutedEventArgs e)
		{
			Trace.WriteLine("::ToggleButton_Esc");
			int OnOff = (sender as ToggleButton).IsChecked ?? false ? 1 : 0;
			SerialSend(@"{""Topic"":""Cmd/robot1"",""T"":""Cmd"",""Cmd"":""Esc"",""Value"":" + OnOff + "}");
		}

		private void ToggleButton_Bumper(object sender, RoutedEventArgs e)
		{
			Trace.WriteLine("::ToggleButton_Bumper");
			int OnOff = (sender as ToggleButton).IsChecked ?? false ? 1 : 0;
			SerialSend(@"{""Topic"":""Cmd/robot1"",""T"":""Cmd"",""Cmd"":""Bumper"",""Value"":" + OnOff + "}");
		}
	}
}