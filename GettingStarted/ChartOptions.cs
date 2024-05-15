using System.Windows.Forms.DataVisualization.Charting;
using NumSharp;

namespace GettingStarted;

public record ChartOptions(ArrayWrapper X, ArrayWrapper Y, SeriesChartType ChartType);
