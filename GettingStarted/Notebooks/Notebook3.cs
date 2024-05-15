using NumSharp;

namespace GettingStarted.Notebooks;

public class Notebook3 : INotebook
{
    public async Task Execute()
    {
        var data = await Pandas.ReadCsv("https://drive.google.com/uc?export=download&id=1aRZ-eh0IEsZW6YT_5oP-7kV7649MKaza",
            '\t');
        
        Console.WriteLine(data);
        Console.WriteLine(data["Chirping rate", "Temperature"]);
        Console.WriteLine(data.Describe());
    }
}