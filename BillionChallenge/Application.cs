namespace BillionChallenge;

public static class Application
{
    public static PerformanceCounter PrintResult(string filePath)
    {
        var locationsSummaryDictionary = MeasurementsParser.Create(filePath, out var counter);
        
        foreach (var summary in locationsSummaryDictionary)
        {
            Console.WriteLine($"{summary.Key};{summary.Value.Min};{summary.Value.Average};{summary.Value.Max}");
        }
        
        counter.Stop();
        return counter;
    }
}
