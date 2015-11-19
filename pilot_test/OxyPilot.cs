using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using spiked3;
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
        const int pointLimit = 150;

        public OxyPilot()
        {
            Axes.Add(new DateTimeAxis { });
            Axes.Add(new LinearAxis { });
        }

        public void Append(IPlotView plot, dynamic j)
        {
            // the first message (with data) sets the keys to plot
            if (Series.Count == 0)
            {
                List<string> keys = Util.GetMemberNames(j, true);
                keys.Remove("T");
                keys.Remove("Time");
                keys.Remove("Flag");
                foreach (string k in keys)
                    Series.Add(new LineSeries { Title = k });
            }

            foreach (LineSeries ls in Series)
            { 
                while (ls.Points.Count > pointLimit)
                    ls.Points.RemoveAt(0);
                try
                {
                    ls.Points.Add(new DataPoint((double)j.Time, (double)j[ls.Title])) ;
                }
                catch (Exception)
                {
                    Series.Remove(ls);
                    break;
                }
            }
            plot.InvalidatePlot();
        }

        internal void Reset(dynamic j)
        {
            Series.Clear();
        }
    }
}
