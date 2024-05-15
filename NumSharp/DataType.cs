using System.Collections.Immutable;
using System.Globalization;
using System.Text;

namespace NumSharp;

public class DataType
{
    private static ImmutableDictionary<string, Func<DataType, Func<int, double>>> RowCalculators =
        ImmutableDictionary.CreateRange(new[]
        {
            new KeyValuePair<string, Func<DataType, Func<int, double>>>("count", x => x.Count),
            new KeyValuePair<string, Func<DataType, Func<int, double>>>("mean", x => x.Mean),
            new KeyValuePair<string, Func<DataType, Func<int, double>>>("std", x => x.Std),
            new KeyValuePair<string, Func<DataType, Func<int, double>>>("min", x => x.Min),
            new KeyValuePair<string, Func<DataType, Func<int, double>>>("25%", x => x.Quartile1),
            new KeyValuePair<string, Func<DataType, Func<int, double>>>("50%", x => x.Median),
            new KeyValuePair<string, Func<DataType, Func<int, double>>>("75%", x => x.Quartile3),
            new KeyValuePair<string, Func<DataType, Func<int, double>>>("max", x => x.Max)
        });
    
    private string[] _header;
    private Dictionary<string, int> _headerHelper;
    
    public string[] Header
    {
        get => _header;
        init
        {
            _header = value;
            _headerHelper = new();
            for (var i = 0; i < value.Length; i++)
                _headerHelper[value[i]] = i;
        }
    }
    public double[][] Data { get; init; }
    private string[]? Rows { get; init; }

    public DataType this[params string[] keys]
    {
        get => new DataType()
        {
            Header = keys,
            Data = Data.Select(x => keys.Select(key => x[_headerHelper[key]]).ToArray()).ToArray()
        };
    }

    public override string ToString()
    {
        return GetTable(Rows ?? Enumerable.Range(0, Data.Length).Select(x => x.ToString()).ToArray());
    }

    public DataType Describe(params string[] rows)
    {
        if (rows.Length == 0)
            rows = new[] { "count", "mean", "std", "min", "25%", "50%", "75%", "max" };

        var res = new double[rows.Length][];
        for (var i = 0; i < rows.Length; i++)
        {
            res[i] = new double[_header.Length];

            for (var j = 0; j < _header.Length; j++)
                res[i][j] = RowCalculators[rows[i]](this)(j);
        }

        return new DataType
        {
            Header = _header,
            Data = res,
            Rows = rows
        };
    }

    double Count(int columnNum) => Data.Length;

    double Min(int columnNum) => Data.Min(x => x[columnNum]);

    double Max(int columnNum) => Data.Max(x => x[columnNum]);

    double Mean(int columnNum) => Data.Sum(x => x[columnNum]) / Count(columnNum);

    double Std(int columnNum)
    {
        var mean = Mean(columnNum);
        return Math.Sqrt(Data.Sum(x => (x[columnNum] - mean) * (x[columnNum] - mean)) / (Count(columnNum) - 1));
    }

    double Quartile1(int columnNum)
    {
        // var leftHalfCount = (count - 1) / 2 + 1;     4k + 1: => 2k + 1        4k + 2: => 2k + 1       4k + 3: => 2k + 2       4k + 4: => 2k + 2
        // var quartileLeft = leftHalfCount / 2;        4k + 1: => k             4k + 2: => k            4k + 3: => k + 1        4k + 4: => k + 1
        // var quartileRight = (leftHalfCount + 1) / 2; 4k + 1: => k + 1         4k + 2: => k + 1        4k + 3: => k + 1        4k + 4: => k + 1
        var count = (int)Count(columnNum);

        var qLeft = (count - 1) / 4 + ((count - 1) % 4 + 1) / 2;
        var qRight = (count - 1) / 4 + 1;

        var sorted = SortedData(columnNum);
        return (sorted[qLeft] + sorted[qRight]) / 2;
    }

    double Median(int columnNum)
    {
        var count = (int)Count(columnNum);
        var medianLeft = (count - 1) / 2;
        var medianRight = count / 2;
        var sorted = SortedData(columnNum);
        return (sorted[medianLeft] + sorted[medianRight]) / 2;
    }

    double Quartile3(int columnNum)
    {
        // var offset = count / 2;                              4k + 1: => 2k        4k + 2: => 2k + 1   4k + 3: => 2k + 1   4k + 4: => 2k + 2
        // var rightCount = count - offset;                     4k + 1: => 2k + 1    4k + 2: => 2k + 1   4k + 3: => 2k + 2   4k + 4: => 2k + 2
        // var quartileLeft = offset + rightCount / 2;          4k + 1: => 3k        4k + 2: => 3k + 1   4k + 3: => 3k + 2   4k + 4: => 3k + 3
        // var quartileRight = offset + (rightCount + 1) / 2;   4k + 1: => 3k + 1    4k + 2: => 3k + 2   4k + 3: => 3k + 2   4k + 4: => 
        var count = (int)Count(columnNum);

        var qLeft = count / 2 + (count - 1) / 4 + ((count - 1) % 4 + 1) / 2;
        var qRight = count / 2 + (count - 1) / 4 + 1;

        var sorted = SortedData(columnNum);
        return (sorted[qLeft] + sorted[qRight]) / 2;
    }

    double[] SortedData(int columnNum) => Data.Select(x => x[columnNum]).OrderBy(x => x).ToArray();
    
    // 0 1 2 3 4 5 6 7 8 9 10 11 12 13 14                   15 / 4 = 3.75                       15 % 4 = 3
    // 0    (3+4)    7    (10+11)      14                   1: 3.75     2: 7.5      3: 11.25
    // 0     3.5     7     10.5        14                   1: 3 4      2: 7 7      3: 10 11

    // 0 1 2 3 4 5 6 7 8 9 10 11 12 13                      14 / 4 = 3.5                        14 % 4 = 2
    // 0     3    (6+7)    10       13                      1: 3.5      2: 7        3: 10.5
    // 0   3.25    6.5    9.75      13                      1: 3        2: 6 7      3: 10

    // 0 1 2 3 4 5 6 7 8 9 10 11 12                         13 / 4 = 3.25                       13 % 4 = 1
    // 0     3     6     9       12                         1: 3.25     2: 6.5      3: 9.75
    // 0     3     6     9       12                         1: 3        2: 6        3: 9
    
    // 0 1 2 3 4 5 6 7 8 9 10 11                            12 / 4 = 3                          12 % 4 = 0
    // 0  (2+3) (5+6) (8+9)   11                            1: 3        2: 6        3: 9
    // 0   2.75  5.5   8.25   11                            1: 2 3      2: 5 6      3: 8 9

    private string GetTable(string[] rows)
    {
        var res = new StringBuilder();

        var columnLength = new int[_header.Length + 1];
        columnLength[0] = (Data.Length - 1).ToString().Length + 2;
        for (var i = 0; i < _header.Length; i++)
        {
            columnLength[i + 1] = _header[i].Length;
            for (var j = 0; j < Data.Length; j++)
                columnLength[i + 1] = Math.Max(columnLength[i + 1], Data[j][i].ToString(CultureInfo.InvariantCulture).Length);
        }

        for (var i = 0; i < columnLength.Length; i++)
            columnLength[i] += 2;

        res.Append(GetCenterString("", columnLength[0]));
        for (var i = 0; i < _header.Length; i++)
            res.Append($"\t{GetCenterString(_header[i], columnLength[i + 1])}");
        res.AppendLine();

        for (var i = 0; i < Data.Length; i++)
        {
            res.Append(GetCenterString(rows[i], columnLength[0]));
            for (var j = 0; j < _header.Length; j++)
                res.AppendFormat($"\t{GetCenterString(Data[i][j].ToString(CultureInfo.InvariantCulture), columnLength[j + 1])}");
            res.AppendLine();
        }
        
        return res.ToString();
    }

    private string GetCenterString(string str, int length)
    {
        var leftPad = (length - str.Length) / 2 + str.Length;
        return str.PadLeft(leftPad).PadRight(length);
    }
}