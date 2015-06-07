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
        const float travelThreshold = 0.5F;
        const float turnThreshold = 2F;
        bool cancelFlag;

        [UiButton("Cancel Move", "White", "Red")]
        public void Cancel(object sender, RoutedEventArgs e)
        {
            cancelFlag = true;
        }

        [UiButton("Straight 1M", "White", "Magenta")]
        public void Straight_1M(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("::Straight_1M");
            float startX = X, startY = Y, startH = H;            
            
            DateTime lastTime = DateTime.Now;
            previousIntegral = previousDerivative = previousError = 0F;

            for (cancelFlag = false; !cancelFlag;)
            {
                DateTime nowTime = DateTime.Now;
                TimeSpan elapsed = nowTime - lastTime;
                if (Math.Abs(distance(startX, X, startY, Y)) > travelThreshold)
                {
                    Trace.WriteLine($" Arrive");
                    SendPilot(new { Cmd = "Pwr", M1 = 0, M2 = 0 });
                    break;
                }

                Adjustment = simplePid(startH - H, 0.1F, 0F, 0F, elapsed);
                Adjustment = constrain(Adjustment, -10, 10);
                Trace.WriteLine($" Adjust {Adjustment} M1({40 - Adjustment}) M1({40 + Adjustment})");
                SendPilot(new { Cmd = "Pwr", M1 = 40.0 - Adjustment, M2 = 40.0 + Adjustment });
                DoEvents();
                System.Threading.Thread.Sleep(100);
                DoEvents();
                lastTime = nowTime;
            }            
        }

        [UiButton("Turn45", "White", "Magenta")]
        public void Turn45(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("::Turn45");
            float goalH = H + 45;
            DateTime lastTime = DateTime.Now;
            for (cancelFlag = false; !cancelFlag;)
            {
                DateTime nowTime = DateTime.Now;
                TimeSpan elapsed = nowTime - lastTime;
                if (Math.Abs(goalH - H) < turnThreshold)
                {
                    Trace.WriteLine($" Arrive");
                    SendPilot(new { Cmd = "Pwr", M1 = 0, M2 = 0 });
                    break;
                }

                Adjustment = (goalH - H) > 0 ? -20 : 20;
                Trace.WriteLine($" Adjust {Adjustment} M1({50.0 - Adjustment}) M1({50.0 + Adjustment})");
                SendPilot(new { Cmd = "Pwr", M1 = 50.0 - Adjustment, M2 = 50.0 + Adjustment });
                DoEvents();
                System.Threading.Thread.Sleep(100);
                DoEvents();
                lastTime = nowTime;
            }            
        }

        private float constrain(float adjust, int v1, int v2)
        {
            return Math.Max(Math.Min(v2, adjust), v1);
        }

        static float distance(float startX, float x, float startY, float y)
        {
            return (float)(Math.Sqrt((x - startX) * (x - startX) + (y - startY) * (y - startY)));
        }

        float previousIntegral, previousDerivative, previousError;
        private float simplePid(float err, float Kp, float Ki, float Kd, TimeSpan elapsed)
        {
            float dt = (float)elapsed.TotalSeconds;
            float integral = (previousIntegral + err) * dt;
            float derivative = (previousDerivative - err) * dt;
            float output = Kp * err + Ki * integral + Kd * derivative;
            previousIntegral = integral;
            previousDerivative = derivative;
            previousError = err;
            return output;
        }
    }
}
