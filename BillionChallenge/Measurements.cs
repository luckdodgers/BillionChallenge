namespace BillionChallenge;

public struct Measurements
{
    private nuint _count;
    private nint _sum;
    private nint _min;
    private nint _max;
    
    public double Average => _sum / (nint)_count * 0.1;
    public double Min => _min * 0.1;
    public double Max => _max * 0.1;
    
    public void Update(nint value)
    {
        _count++;
        _sum += value;
        
        if (value < _min)
        {
            _min = value;
        }
        
        if (value > _max)
        {
            _max = value;
        }
    }
    
    public void Merge(Measurements measurementsToMerge)
    {
        _count += measurementsToMerge._count;
        _sum += measurementsToMerge._sum;

        if (measurementsToMerge._min < _min)
        {
            _min = measurementsToMerge._min;
        }

        if (measurementsToMerge._max > _max)
        {
            _max = measurementsToMerge._max;
        }
    }
}
