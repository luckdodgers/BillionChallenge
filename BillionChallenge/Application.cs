namespace BillionChallenge;

public static class Application
{
    public static PerformanceCounter PrintResult(string filePath)
    {
        PerformanceCounter counter;
        var processor = new MeasurementsProcessor(filePath);
        try
        {
            var result = processor.Create(out counter);
            foreach (var summary in result)
            {
                Console.WriteLine(
                    $"{summary.Key.ToString()};{(double)summary.Value.Min / 10};{summary.Value.Average / 10};{(double)summary.Value.Max / 10}");
            }

            counter.Stop();
        }
        finally
        {
            processor.Dispose();
        }
        
        return counter;
    }
}
