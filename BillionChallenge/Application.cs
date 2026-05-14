namespace BillionChallenge;

public static class Application
{
    public static PerformanceCounter PrintResult(string filePath)
    {
        var locationsSummaryDictionary = MeasurementsParser.Create(filePath, out var counter);
        
        foreach (var summary in locationsSummaryDictionary)
        {
            Console.WriteLine($"{summary.Key};{(double)summary.Value.Min / 10};{summary.Value.Average / 10};{(double)summary.Value.Max / 10}");
        }
        
        counter.Stop();
        return counter;
    }
}
