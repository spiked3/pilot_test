using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace pilot_test
{
    public partial class MainWindow : Window
    {
        string broker = "127.0.0.1";
        //string broker = "192.168.1.30";      // pi

        MqttClient Mq;
        private SerialPort Serial;

        int recvIdx = 0;
        byte[] recvbuf = new byte[1024];

// ----------------------------------------

        void MqttOpen()
        {
            Mq = new MqttClient(broker);
            Mq.MqttMsgPublishReceived += MqttMsgPublishReceived;
            Mq.Connect("PC");
            Trace.WriteLine($"Connected to MQTT @{broker}","1");
            Mq.Subscribe(new string[] { "robot1/#" }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
        }

        private void MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            switch (e.Topic)
            {
                case "robot1":
                    {
                        dynamic j = JsonConvert.DeserializeObject(System.Text.Encoding.UTF8.GetString(e.Message));
                        if (j != null)
                        {
                            string type = (string)j["T"];
                            if (type.Equals("Heartbeat"))
                                Dispatcher.InvokeAsync(() => { HeartBeat(j); }, DispatcherPriority.Render);
                        }
                        else
                        {
                            Trace.WriteLine(System.Text.Encoding.UTF8.GetString(e.Message).Trim(new char[] { '\n', '\r' }));
                        }
                    }
                    break;

                default:
                    //
                    break;
            }
        }

        void MqttClose()
        {
            Mq.Disconnect();
            Trace.WriteLine("MQTT disconnected","2");
        }

        private void SerialClose()
        {
                if (Serial != null && Serial.IsOpen)
                {
                    Serial.Close();
                    SerialIsOpen = Serial.IsOpen;
                }
            Trace.WriteLine("Serial closed");
        }

        private void SerialOpen()
        {
            Serial = new SerialPort("com7", 115200);
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
            Trace.WriteLine($"Serial opened={Serial.IsOpen} on {Serial.PortName}","2");
        }


        void AppSerialDataEvent(byte[] received)
        {
            foreach (var b in received)
            {
                if (b == '\n')
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
                    // +++ atempt recovery
                    System.Diagnostics.Debugger.Break();    // overflow
                }
            }
        }

        void SerialHandler(SerialPort port)
        {
            byte[] buffer = new byte[1024];
            Action kickoffRead = null;
            kickoffRead = delegate
            {
                port.BaseStream.BeginRead(buffer, 0, buffer.Length, delegate (IAsyncResult ar)
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

        void SerialSend(string t)
        {
            if (Serial != null && Serial.IsOpen)
            {
                Trace.WriteLine("com<-" + t);
                Serial.WriteLine(t);
                DoEvents();
            }
        }

        public static void SendPilot(dynamic j)
        {
            string jsn = JsonConvert.SerializeObject(j);
            if (_instance?.Serial?.IsOpen ?? false)
                _instance?.SerialSend(jsn);
            if (_instance?.Mq?.IsConnected ?? false)
                _instance?.Mq.Publish("robot1/Cmd", UTF8Encoding.ASCII.GetBytes(jsn));
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
