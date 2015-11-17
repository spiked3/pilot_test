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
            //Pilot = Pilot.Factory("127.0.0.1");
            //Pilot = Pilot.Factory("com15");
            Pilot.OnPilotReceive += Pilot_OnReceive;

            //Pilot.Send(new { Cmd = "CONFIG", MPU = new int[] { -4526, -136, 1990, 48, -26, -21 } });
            Pilot.Send(new { Cmd = "CONFIG", TPM = 336, MMX = 450, StrRv = -1 });
            Pilot.Send(new { Cmd = "CONFIG", M1 = new int[] { 1, -1 }, M2 = new int[] { -1, 1 } });

            Pilot.Send(new { Cmd = "RESET"});
            Pilot.Send(new { Cmd = "ESC", Value = 1 });

            Pilot.Send(new { Cmd = "MOV", Dist = 1.0, Pwr = 40.0F });
            Pilot.waitForEvent();
            Pilot.Send(new { Cmd = "ROT", Hdg = 180.0 });
            Pilot.waitForEvent();
            Pilot.Send(new { Cmd = "MOV", Dist = 1.0, Hdg = 180.0, Pwr = 40.0F });
            Pilot.waitForEvent();
            Pilot.Send(new { Cmd = "ROT", Hdg = 0.0 });
            Pilot.waitForEvent();

            Pilot.Send(new { Cmd = "ESC", Value = 0 });
        }
    }
}