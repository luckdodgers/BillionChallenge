namespace BillionChallenge;

public static class MeasurementsGenerator
{
    public static List<Measurement> Create(string filePath)
    {
        var measurements = new List<Measurement>(1_000_001);
        var lines = File.ReadLines(filePath);
        foreach (var line in lines)
        {
            var measurementValues = line.Split(';');
            var measurement = new Measurement(measurementValues[0], double.Parse(measurementValues[1]));
            measurements.Add(measurement);
        }
        
        return measurements;
    }
}
