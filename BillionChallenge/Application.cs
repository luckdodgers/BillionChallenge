namespace BillionChallenge;

public static class Application
{
    public static void PrintResult(string filePath)
    {
        var locationsSummaryDictionary = MeasurementsParser.Create(filePath);
        
        foreach (var summary in locationsSummaryDictionary)
        {
            Console.WriteLine($"{summary.Key};{summary.Value.Min};{summary.Value.Average};{summary.Value.Max}");
        }
    }
}
