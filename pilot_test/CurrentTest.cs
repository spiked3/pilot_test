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
using NDesk.Options;
using Spiked3;

namespace pilot_test
{
    public partial class MainWindow 
    {
        public void CurrentTestX(object sender, RoutedEventArgs e)
        {
            //Pilot = Pilot.Factory("192.168.42.1");
            //Pilot = Pilot.Factory("127.0.0.1");
            Pilot = Pilot.Factory("com3");
            Pilot.OnPilotReceive += Pilot_OnReceive;

            Pilot.Send(new { Cmd = "SRVO", Value = 10 });
            System.Threading.Thread.Sleep(500);
            Pilot.Send(new { Cmd = "SRVO", Value = 90 });
            System.Threading.Thread.Sleep(500);
            Pilot.Send(new { Cmd = "SRVO", Value = 170 });
            System.Threading.Thread.Sleep(500);

            Pilot.Send(new { Cmd = "SRVO", Value = 90 });
        }

        [UiButton("NavPlanTest")]
        public void CurrentTest(object sender, RoutedEventArgs e)
        {
            try {
                Pilot = Pilot.Factory("192.168.42.1");
                //Pilot = Pilot.Factory("127.0.0.1");
                //Pilot = Pilot.Factory("com15");
                Pilot.OnPilotReceive += Pilot_OnReceive;

                Pilot.Send(new { Cmd = "CONFIG", TPM = 353, MMX = 450, StrRv = -1 });
                Pilot.Send(new { Cmd = "CONFIG", M1 = new int[] { 1, -1 }, M2 = new int[] { -1, 1 } });
                Pilot.Send(new { Cmd = "CONFIG", HPID = new float[] { 75f, .8f, .04f } });

                Pilot.Send(new { Cmd = "RESET" });
                Pilot.Send(new { Cmd = "ESC", Value = 1 });

                Pilot.Send(new { Cmd = "MOV", Dist = 1, Pwr = 40 });
                Pilot.waitForEvent(); Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate { })); // doEvents

                //var hdgTo0 = 180;
                float hdgTo0 = (float)(Math.Atan2(X, -Y) * 180 / Math.PI);
                Pilot.Send(new { Cmd = "ROT", Hdg = hdgTo0 });
                Pilot.waitForEvent(); Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate { })); // doEvents

                float distTo0 = (float)(Math.Sqrt(X * X + Y * Y));
                Pilot.Send(new { Cmd = "MOV", Dist = distTo0, Hdg = hdgTo0, Pwr = 40.0F });
                Pilot.waitForEvent(); Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate { })); // doEvents

                Pilot.Send(new { Cmd = "ROT", Hdg = 0.0 });
                Pilot.waitForEvent(); Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate { })); // doEvents

                Pilot.Send(new { Cmd = "ESC", Value = 0 });
            }
            catch (TimeoutException)
            {
                Trace.WriteLine("Timeout waiting for event");
                Pilot.Send(new { Cmd = "ESC", Value = 0 });
                Pilot.Send(new { Cmd = "MOV", M1 = 0, M2 = 0 });
            }
        }
    }
}