using System.Windows.Forms.DataVisualization.Charting;

namespace GettingStarted.Notebooks;

public class Notebook1 : INotebook
{
    public Task Execute()
    {
        #region Init

        var a = 1;
        var b = 2;

        var step = .005;
        var x = Helpers.Range(-1, 1, step);
        var noise = .1;
        var y = Helpers.NormalDistribution(0, noise, x.Length) + x * a + b;
        Helpers.SaveChart(new[]
        {
            new ChartOptions(x, y, SeriesChartType.Point)
        }, "NormalPoints.png");

        #endregion

        #region Model: y = a*x + 0

        var slopes = Helpers.Range(-4, 4 + 0.01, 0.4);
        var yy = slopes.Reshape(-1, 1) * x;

        var slopesChartOption = new ChartOptions[slopes.Length + 1];
        slopesChartOption[0] = new ChartOptions(x, y, SeriesChartType.Point);
        for (var i = 0; i < slopes.Length; i++)
            slopesChartOption[i + 1] = new ChartOptions(x, yy[i], SeriesChartType.Spline);
        Helpers.SaveChart(slopesChartOption, "Slopes-A.png");

        #endregion

        #region Mean squares cost function J = (y - truth)^2 and partial derivative dJ/da:
        
        var diff = yy - y;
        var sq = diff * diff;

        var sum = sq.Sum(1);
        var deriva = (diff * x).Sum(1);

        Helpers.SaveChart(new[]
        {
            new ChartOptions(slopes, sum, SeriesChartType.Spline),
            new ChartOptions(slopes, deriva, SeriesChartType.Spline)
        }, "SqDeriva-A.png");

        #endregion

        #region Model: y = 0*x + b

        yy = 0 * x + slopes.Reshape(-1, 1);
        slopesChartOption = new ChartOptions[slopes.Length + 1];
        slopesChartOption[0] = new ChartOptions(x, y, SeriesChartType.Point);
        for (var i = 0; i < slopes.Length; i++)
            slopesChartOption[i + 1] = new ChartOptions(x, yy[i], SeriesChartType.Spline);
        Helpers.SaveChart(slopesChartOption, "Slopes-B.png");

        #endregion

        #region Mean squares cost function J = (y - truth)^2 and partial derivative dJ/db:

        diff = yy - y;
        sq = diff * diff;
        sum = sq.Sum(1);
        deriva = diff.Sum(1);
        
        Helpers.SaveChart(new[]
        {
            new ChartOptions(slopes, sum, SeriesChartType.Spline),
            new ChartOptions(slopes, deriva, SeriesChartType.Spline)
        }, "SqDeriva-B.png");

        #endregion

        return Task.CompletedTask;
    }
}