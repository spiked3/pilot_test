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
        private PilotSerial Serial;


        public string CommStatus { get; internal set; }

        public delegate void ReceiveHandler(dynamic json);
        public event ReceiveHandler OnPilotReceive;

        public static Pilot Factory(string uri)     // not really a uri, yet
        {
            // +++ ideally verify serial port is a pilot

            Pilot p = new Pilot();

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
            Mq.Connect("pTest");
            Trace.WriteLine($"Connected to MQTT @ {c}", "1");
            Mq.Subscribe(new string[] { "robot1/#" }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
        }

        private void MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            switch (e.Topic)
            {
                case "robot1":
                    string j = System.Text.Encoding.UTF8.GetString(e.Message);
                    //if (j.StartsWith("//!"))
                    //    Trace.WriteLine(j.Trim() + "\r\n", "error");
                    //else if (j.StartsWith("//"))
                    //    Trace.WriteLine(j.Trim() + "\r\n", "+");
                    //else
                    if (OnPilotReceive != null)
                        OnPilotReceive(JsonConvert.DeserializeObject(System.Text.Encoding.UTF8.GetString(e.Message)));
                    break;
            }
        }

        void MqttClose()
        {
            Mq.Disconnect();
            Trace.WriteLine("MQTT disconnected", "3");
        }

        private void SerialClose()
        {
            if (Serial != null && Serial.IsOpen)
                Serial.Close();
            Trace.WriteLine("Serial closed", "3");
        }


        private void SerialOpen(string c)
        {
            Serial = new PilotSerial(c, 115200);
            try
            {
                Serial.Open();
                Serial.WriteTimeout = 50;
                Serial.OnReceive += Serial_OnReceive;
                Serial.Start();
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
            Trace.WriteLine($"Serial opened({Serial.IsOpen}) on {Serial.PortName}", "2");
        }

        private void Serial_OnReceive(dynamic json)
        {
            if (OnPilotReceive != null)    // bubble
                OnPilotReceive(json);
        }

        void SerialSend(string t)
        {
            if (Serial?.IsOpen ?? false)
            {
                Trace.WriteLine("com<-" + t);
                Serial.WriteLine(t);
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
            if (Serial?.IsOpen ?? false)
                Serial.Close();
            if (Mq?.IsConnected ?? false)
                MqttClose();
        }
    }
}
