using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pilot_test
{
    public class OxyPilot : PlotModel
    {
        const int pointLimit = 100;

        public OxyPilot()
        {
            Axes.Add(new OxyPlot.Axes.DateTimeAxis { });
            Axes.Add(new OxyPlot.Axes.LinearAxis { });
            //foreach (string k in keys)
            //    Series.Add(new LineSeries { Title = k });
        }

        //Dictionary<string, LineSeries> SeriesDict = new Dictionary<string, LineSeries>();

        public void Append(dynamic j)
        {
            // todo probably should eventually get time from pilot (but remember to ignore from GetMemberNames)
            var t = DateTimeAxis.ToDouble(DateTime.Now);

            // the first message (with data) sets the keys to plot
            if (Series.Count == 0)
            {
                var m = Util.GetMemberNames(j, true);
                foreach (string k in m)
                    Series.Add(new LineSeries { Title = k });
            }

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
                    //System.Diagnostics.Debugger.Break();
                }
            }
            // +++ would be nice if we could invalidate plot that we are attached to here            
        }
    }
}
