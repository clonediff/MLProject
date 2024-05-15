using System.Drawing;
using System.Reflection.Emit;
using System.Windows.Forms.DataVisualization.Charting;
using MathNet.Numerics.Distributions;
using NumSharp;

namespace GettingStarted;

public static class Helpers
{
    public static ArrayWrapper NormalDistribution(double mean, double stddev, int size)
    {
        var res = new double[size];
        var normal = Normal.WithMeanStdDev(mean, stddev, new Random());
        normal.Samples(res);
        return res;
    }

    public static (ArrayWrapper x, ArrayWrapper y) MakeRegression(int count, double mean, double noise = .1)
    {
        var a = Random.Shared.NextDouble() * 100 - 50;
        var b = Random.Shared.NextDouble() * 100 - 50;
        var step = .005;
        var x = Range(-3, 3, 6.0 / count);
        var y = NormalDistribution(0, noise, x.Length) + x * a + b;
        Console.WriteLine($"Generated regression: b={b}; a={a}");
        return (x, y);
    }

    public static (ArrayWrapper x, ArrayWrapper xTest, ArrayWrapper y, ArrayWrapper yTest) Split(ArrayWrapper xAll,
        ArrayWrapper yAll, double testSize = 0.1, int seed = 0)
    {
        var testLength = (int)(xAll.Length * testSize);
        var x = new double[xAll.Length - testLength];
        var y = new double[yAll.Length - testLength];
        var xTest = new double[testLength];
        var yTest = new double[testLength];
        var rnd = seed == 0 ? new Random() : new Random(seed);
        int mainPointer = 0, testPointer = 0;
        var availableIndexes = Enumerable.Range(0, xAll.Length).ToList();
        for (var i = 0; i < testLength; i++)
        {
            var index = rnd.Next(0, availableIndexes.Count);
            xTest[testPointer] = xAll[index];
            yTest[testPointer] = yAll[index];
            availableIndexes.RemoveAt(index);
            testPointer++;
        }

        for (var i = 0; i < x.Length; i++)
        {
            var index = rnd.Next(0, availableIndexes.Count);
            x[mainPointer] = xAll[index];
            y[mainPointer] = yAll[index];
            availableIndexes.RemoveAt(index);
            mainPointer++;
        }

        return (x, xTest, y, yTest);
    }
    
    public static ArrayWrapper Range(double from, double to, double step)
    {
        var res = new List<double>();
        for (var i = 0; from + i * step < to; i++)
            res.Add(from + i * step);
        return res.ToArray();
    }
    
    public static void SaveChart(ChartOptions[] chartOptions, string imageFileName)
    {
        if (chartOptions.Any(chartOption => chartOption.X.Length != chartOption.Y.Length)) 
            throw new InvalidDataException("X and Y values must be equal length");
        
        var chart = new Chart();
        chart.Size = new Size(640, 320);
        chart.ChartAreas.Add("chartArea");

        for (var i = 0; i < chartOptions.Length; i++)
        {
            chart.Series.Add(i.ToString());
            chart.Series[i.ToString()].ChartType = chartOptions[i].ChartType;

            for (var j = 0; j < chartOptions[i].X.Length; j++)
                chart.Series[i.ToString()].Points.AddXY((double)chartOptions[i].X[j], (double)chartOptions[i].Y[j]);

        }

        var imagesDir = "images";
        if (!Directory.Exists(imagesDir))
            Directory.CreateDirectory(imagesDir);
        chart.SaveImage(Path.Combine(imagesDir, imageFileName), ChartImageFormat.Png);
    }
}