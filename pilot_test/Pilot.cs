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
    public class Pilot
    {
        MqttClient Mq;
        private SerialPort Serial;

        int recvIdx = 0;
        byte[] recvbuf = new byte[1024];

        public string CommStatus { get; internal set; }

        public delegate void ReceiveHandler(dynamic json);
        public event ReceiveHandler OnReceive;

        public static Pilot Factory(string uri)     // not really a uri, yet
        {
            // +++ ideally verify serial port is a pilot
            // if serial opens fail, try local and remote mqtt (in that order)

            Pilot p = new Pilot();
            
            //string broker = "127.0.0.1";
            //string broker = "192.168.1.30";      // pi

            if (uri.Contains("com"))
                p.SerialOpen(uri);
            else
                p.MqttOpen(uri);

            StringBuilder b = new StringBuilder();
            if (p.Serial != null && p.Serial.IsOpen)
                b.Append($"{p.Serial.PortName} open");
            if (p.Mq != null && p.Mq.IsConnected)
                b.Append($"Mqtt ({uri}) connected");
            p.CommStatus = b.ToString();

            return p;
        }

        private Pilot()
        { }

        void MqttOpen(string c)
        {
            Mq = new MqttClient(c);
            Mq.MqttMsgPublishReceived += MqttMsgPublishReceived;
            Mq.Connect("PC");
            Trace.WriteLine($"Connected to MQTT @ {c}", "1");
            Mq.Subscribe(new string[] { "robot1/#" }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
        }

        private void MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            switch (e.Topic)
            {
                case "robot1":
                    string j = System.Text.Encoding.UTF8.GetString(e.Message);
                    if (j.StartsWith("//!"))
                        Trace.WriteLine(j.Trim() + "\r\n","error");
                    else if (j.StartsWith("/"))
                        Trace.WriteLine(j.Trim() + "\r\n","1");
                    else if (OnReceive != null)
                            OnReceive(JsonConvert.DeserializeObject(System.Text.Encoding.UTF8.GetString(e.Message)));
                    break;
            }
        }

        void MqttClose()
        {
            Mq.Disconnect();
            Trace.WriteLine("MQTT disconnected", "2");
        }

        private void SerialClose()
        {
            if (Serial != null && Serial.IsOpen)
                Serial.Close();
            Trace.WriteLine("Serial closed");
        }

        private void SerialOpen(string c)
        {
            Serial = new SerialPort(c, 115200);
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
            Trace.WriteLine($"Serial opened={Serial.IsOpen} on {Serial.PortName}", "2");
        }

        void AppSerialDataEvent(byte[] received)
        {
            foreach (var b in received)
            {
                if (b == '\n')
                {
                    recvbuf[recvIdx] = 0;
                    string line = Encoding.UTF8.GetString(recvbuf, 0, recvIdx); // makes a copy
                    Trace.WriteLine("com->" + line.Trim(new char[] { '\r', '\n' }));
                    if (OnReceive != null)
                        OnReceive(JsonConvert.DeserializeObject(line));
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
                //Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate { })); // doevents
            }
        }

        public void Send(dynamic j)
        {
            string jsn = JsonConvert.SerializeObject(j);
            if (Serial?.IsOpen ?? false)
               SerialSend(jsn);
            if (Mq?.IsConnected ?? false)
                Mq.Publish("robot1/Cmd", UTF8Encoding.ASCII.GetBytes(jsn));
        }

        internal void Close()
        {
            if (Serial != null && Serial.IsOpen)
                Serial.Close();
            if (Mq != null && Mq.IsConnected)
                MqttClose();
        }
    }
}
