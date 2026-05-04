using System.Diagnostics;

namespace BillionChallenge;

public static class Application
{
    public static TimeSpan ParsingTime = TimeSpan.Zero;
    public static TimeSpan CalculationTime = TimeSpan.Zero;
    
    public static void PrintResult(string filePath)
    {
        var parsingStart = Stopwatch.GetTimestamp();
        
        var measurements = MeasurementsGenerator.Create(filePath);
        var groupedMeasurements = measurements.GroupBy(x => x.Location);
        
        ParsingTime = Stopwatch.GetElapsedTime(parsingStart);
        var calculationTime = Stopwatch.GetTimestamp();
        
        foreach (var measurement in groupedMeasurements)
        {
            var location = measurement.Key;
            var min = measurement.Min(x => x.Temperature);
            var max = measurement.Max(x => x.Temperature);
            var mean = measurement.Sum(x => x.Temperature) / measurement.Count();

            Console.WriteLine($"{location};{min};{mean};{max}");
        }
        
        CalculationTime = Stopwatch.GetElapsedTime(calculationTime);
    }
}
