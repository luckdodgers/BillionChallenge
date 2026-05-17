namespace BillionChallenge;

public struct Measurements
{
    public nint Count;
    public nint Sum;
    public nint Min;
    public nint Max;
    public double Average => Sum / (double)Count;
    
    public void Update(nint value)
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
