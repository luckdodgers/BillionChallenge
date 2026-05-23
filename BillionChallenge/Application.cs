using System.Text;

namespace BillionChallenge;

public static class Application
{
    public static PerformanceCounter PrintResult(string filePath)
    {
        Console.OutputEncoding = Encoding.UTF8;
        
        var processor = new MeasurementsProcessor(filePath);
        var result = processor.Create(out var counter);
        var orderedResult = result
            .Select(x => (x.Key.ToString(), x.Value))
            .OrderBy(x => x.Item1, StringComparer.Ordinal);
        
        foreach (var summary in orderedResult)
        {
            Console.WriteLine(
                $"{summary.Item1};{summary.Value.Min:0.0};{summary.Value.Average:0.0};{summary.Value.Max:0.0}");
        }

        counter.Stop();
        processor.Dispose();
        
        return counter;
    }
}
