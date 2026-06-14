using System.Text;

namespace BillionChallenge;

public static class Application
{
    public static PerformanceCounter PrintResult(string filePath)
    {
        var performanceCounter = new PerformanceCounter();
        performanceCounter.Start();
        
        var processor = new MeasurementsProcessor(filePath);
        var result = processor.Calculate(ref performanceCounter);
        var orderedResult = result
            .Select(x => (x.Key, x.Value))
            .OrderBy(x => x.Key);
        
        Console.OutputEncoding = Encoding.UTF8;
        foreach (var summary in orderedResult)
        {
            Console.WriteLine(
                $"{summary.Key};{summary.Value.Average:0.0};{summary.Value.Min:0.0};{summary.Value.Max:0.0}");
        }

        performanceCounter.Stop();
        processor.Dispose();
        
        return performanceCounter;
    }
}
