namespace BillionChallenge;

public static class Application
{
    public static void PrintResult()
    {
        var measurements = MeasurementsGenerator.Create();
        var groupedMeasurements = measurements.GroupBy(x => x.Location);

        foreach (var measurement in groupedMeasurements)
        {
            var location = measurement.Key;
            var min = measurement.Min(x => x.Temperature);
            var max = measurement.Max(x => x.Temperature);
            var mean = measurement.Sum(x => x.Temperature) / measurement.Count();
    
            Console.WriteLine($"{location};{min};{mean};{max}");
        }
    }
}
