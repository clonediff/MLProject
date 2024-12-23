using System.Windows.Forms.DataVisualization.Charting;
using NumSharp;

namespace GettingStarted.Notebooks;

public class GradientDescentNotebook : INotebook
{
    private void Plot(string fileName, ArrayWrapper x, ArrayWrapper y, double theta0, double theta1)
    {
        var yPredict = theta0 + x * theta1;
        Helpers.SaveChart(new[]
        {
            new ChartOptions(x, y, SeriesChartType.Point),
            new ChartOptions(x, yPredict, SeriesChartType.Spline)
        }, fileName);
    }

    private (double t0, double t1, int iter, List<double> trainErrors, List<double> valError) GradientDescent(double alpha,
        ArrayWrapper x, ArrayWrapper y,
        ArrayWrapper xTest, ArrayWrapper yTest,
        double epsilon = 1e-4,
        int maxInter = 10_000)
    {
        var converged = false;
        var iter = 0;
        var m = x.Shape[0];

        var t0 = 40.0;
        var t1 = 100.0;

        var J = 0.0;
        for (var i = 0; i < m; i++)
            J += (t0 + t1 * x[i] - y[i]) * (t0 + t1 * x[i] - y[i]);

        var trainErrors = new List<double>();
        var valErrors = new List<double>();

        var printIters = new HashSet<int>()
        {
            0, 1, 2, 3, 5, 10, 20, 30, 50, 75, 100, 125, 150, 200, 250, 300, 450, 500
        };
        while (!converged)
        {
            if (printIters.Contains(iter)) Plot($"Iteration-{iter}.png", x, y, t0, t1);

            var grad0 = 1.0 / m * Enumerable.Range(0, m).Select(i => t0 + t1 * x[i] - y[i]).Sum(e => e);
            var grad1 = 1.0 / m * Enumerable.Range(0, m).Select(i => (t0 + t1 * x[i] - y[i]) * x[i]).Sum(e => e);

            t0 = t0 - alpha * grad0;
            t1 = t1 - alpha * grad1;

            var error = Enumerable.Range(0, m).Select(i => (t0 + t1 * x[i] - y[i]) * (t0 + t1 * x[i] - y[i])).Sum(e => e);
            trainErrors.Add(error);

            var valError = Enumerable.Range(0, xTest.Shape[0])
                .Select(i => (t0 + t1 * xTest[i] - yTest[i]) * (t0 + t1 * xTest[i] - yTest[i])).Sum(e => e);
            valErrors.Add(valError);

            if (Math.Abs(J - error) <= epsilon)
            {
                Console.WriteLine($"Converged, iterations: {iter}!!!");
                converged = true;
            }

            J = error;
            iter++;

            if (iter == maxInter)
            {
                Console.WriteLine($"Max interactions exceeded!");
                converged = true;
            }
        }
        Plot("Result.png", x, y, t0, t1);
        return (t0, t1, iter, trainErrors, valErrors);
    }
    
    public Task Execute()
    {
        #region Init
        
        var (xAll, yAll) = Helpers.MakeRegression(500, 1, 15);
        var (x, xTest, y, yTest) = Helpers.Split(xAll, yAll, 0.25, 42);
        
        Helpers.SaveChart(new []
        {
            new ChartOptions(x, y, SeriesChartType.Point),
            new ChartOptions(xTest, yTest, SeriesChartType.Point)
        }, "InitPoints.png");

        #endregion

        #region Call gradient decent, and get intercept(=theta0) and slope(=theta1)

        var alpha = 0.2;
        var epsilon = 0.005;

        var (theta0, theta1, nIter, terrs, verrs) = GradientDescent(alpha, x, y, xTest, yTest, epsilon, 1000);

        Console.WriteLine($"theta0={theta0}; theta1={theta1}");

        #endregion

        #region MyRegion

        Helpers.SaveChart(new[] { new ChartOptions(Helpers.Range(0, nIter, 1), terrs.ToArray(), SeriesChartType.Spline) }, "TrainErrorResult.png");

        #endregion

        return Task.CompletedTask;
    }
}