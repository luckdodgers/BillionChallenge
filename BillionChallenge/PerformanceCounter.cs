using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace BillionChallenge;

public struct PerformanceCounter
{
    public TimeSpan ElapsedTime { get; private set; }
    public int GC_0_cycles { get; private set; }
    public int GC_1_cycles { get; private set; }
    public int GC_2_cycles { get; private set; }
    public long TotalHeapAllocations { get => _totalHeapAllocations; private set => _totalHeapAllocations = value; }
    public TimeSpan Total_GC_pause_ms { get; private set; }
    
    private TimeSpan _gcPauseBefore;
    private long _initialHeapSize;
    private int _gen0Before;
    private int _gen1Before;
    private int _gen2Before;
    private long _startStamp;
    private long _totalHeapAllocations;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Start()
    {
        _gcPauseBefore = GC.GetTotalPauseDuration();
        _initialHeapSize = GC.GetAllocatedBytesForCurrentThread();
        _gen0Before = GC.CollectionCount(0);
        _gen1Before = GC.CollectionCount(1);
        _gen2Before = GC.CollectionCount(2);
        _startStamp = Stopwatch.GetTimestamp();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Stop()
    {
        ElapsedTime = Stopwatch.GetElapsedTime(_startStamp);
        GC_0_cycles = GC.CollectionCount(0) - _gen0Before;
        GC_1_cycles = GC.CollectionCount(1) - _gen1Before;
        GC_2_cycles = GC.CollectionCount(2) - _gen2Before;
        TotalHeapAllocations += GC.GetAllocatedBytesForCurrentThread() - _initialHeapSize;
        Total_GC_pause_ms = GC.GetTotalPauseDuration() - _gcPauseBefore;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void AddHeapAllocations(long bytesAllocated) => 
        Interlocked.Add(ref _totalHeapAllocations, bytesAllocated);
}
