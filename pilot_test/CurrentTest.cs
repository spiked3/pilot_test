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

namespace pilot_test
{
    public partial class MainWindow 
    {
        [UiButton("NavPlanTest")]
        public void CurrentTest(object sender, RoutedEventArgs e)
        {
            Pilot = Pilot.Factory("192.168.42.1");
            Pilot.OnPilotReceive += Pilot_OnReceive;
            Pilot.Send(new { Cmd = "CONFIG", Geom = new float[] { 336.2F, 450F } });
            Pilot.Send(new { Cmd = "RESET", Hdg = 0.3 });
            Pilot.Send(new { Cmd = "ESC", Value = 1 });
            Pilot.Send(new { Cmd = "ROT", Hdg = 0.0, Pwr = 40.0F });
            Pilot.waitForEvent();
            Pilot.Send(new { Cmd = "MOV", Dist = 1.3, Pwr = 40.0F });
            Pilot.waitForEvent();
            Pilot.Send(new { Cmd = "ROT", Hdg = 89.8, Pwr = 40.0F });
            Pilot.waitForEvent();
            Pilot.Send(new { Cmd = "MOV", Dist = 2.3, Pwr = 40.0F });
            Pilot.waitForEvent();
            Pilot.Send(new { Cmd = "ROT", Hdg = 179.5, Pwr = 40.0F });
            Pilot.waitForEvent();
            Pilot.Send(new { Cmd = "MOV", Dist = 1.4, Pwr = 40.0F });
            Pilot.waitForEvent();
            Pilot.Send(new { Cmd = "ROT", Hdg = -89.9, Pwr = 40.0F });
            Pilot.waitForEvent();
            Pilot.Send(new { Cmd = "MOV", Dist = 2.3, Pwr = 40.0F });
            Pilot.waitForEvent();
            Pilot.Send(new { Cmd = "ROT", Hdg = 2.0, Pwr = 40.0F });
            Pilot.waitForEvent();
            Pilot.Send(new { Cmd = "MOV", Dist = 0.1, Pwr = 40.0F });
            Pilot.waitForEvent();
            Pilot.Send(new { Cmd = "ESC", Value = 0 });

        }
    }
}