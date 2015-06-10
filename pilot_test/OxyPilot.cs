using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pilot_test
{
    public class OxyPilot : PlotModel
    {
        const int pointLimit = 100;

        public OxyPilot(string[] keys)
        {
            Axes.Add(new OxyPlot.Axes.DateTimeAxis { });
            Axes.Add(new OxyPlot.Axes.LinearAxis { });
            foreach (string k in keys)
                Series.Add(new LineSeries { Title = k });
        }

        public void Append(dynamic j)
        {
            var t = DateTimeAxis.ToDouble(DateTime.Now);

            foreach (LineSeries ls in Series)
            { 
                while (ls.Points.Count > pointLimit)
                    ls.Points.RemoveAt(0);
                try
                {
                    ls.Points.Add( new DataPoint(t, (float)j[ls.Title])) ;
                }
                catch (Exception)
                {
                    System.Diagnostics.Debugger.Break();
                }
            }
            // +++ would be nice if we could invalidate plot that we are attached to here            
        }
    }
}
