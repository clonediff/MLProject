using System.Globalization;

namespace NumSharp;

public class Pandas
{
    public static async Task<DataType> ReadCsv(string filePath, params char[] separators)
    {
        Stream fileStream;
        if (!File.Exists(filePath))
        {
            var client = new HttpClient();
            var response = await client.GetAsync(filePath);
            fileStream = await response.Content.ReadAsStreamAsync();
        }
        else
            fileStream = File.Open(filePath, FileMode.Open);

        using var reader = new StreamReader(fileStream);

        var fileContents = await reader.ReadToEndAsync();

        var allLines = fileContents.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Split('\t', StringSplitOptions.RemoveEmptyEntries)).ToArray();
        
        return new DataType
        {
            Header = allLines[0],
            Data = allLines.Skip(1).Select(x => x.Select(y => double.Parse(y, NumberFormatInfo.InvariantInfo)).ToArray()).ToArray()
        };
    }
}