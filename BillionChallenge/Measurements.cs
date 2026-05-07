namespace BillionChallenge;

public struct Measurements
{
    public long Count;
    public double Sum;
    public double Min;
    public double Max;
    public double Average => Sum / Count;

    public void Update(double value)
    {
        Count++;
        Sum += value;
        
        if (value < Min)
        {
            Min = value;
        }
        
        if (value > Max)
        {
            Max = value;
        }
    }

    public void Merge(Measurements measurementsToMerge)
    {
        Count += measurementsToMerge.Count;
        Sum += measurementsToMerge.Sum;

        if (measurementsToMerge.Min < Min)
        {
            Min = measurementsToMerge.Min;
        }

        if (measurementsToMerge.Max > Max)
        {
            Max = measurementsToMerge.Max;
        }
    }
}
