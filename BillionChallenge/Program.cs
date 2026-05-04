using System.Diagnostics;
using BillionChallenge;

//GC.TryStartNoGCRegion(300_000_000);

long initialHeapSize = GC.GetAllocatedBytesForCurrentThread();
int gen0Before = GC.CollectionCount(0);
int gen1Before = GC.CollectionCount(1);
int gen2Before = GC.CollectionCount(2);

var timeStamp = Stopwatch.GetTimestamp();
Application.PrintResult();
var elapsedTime = Stopwatch.GetElapsedTime(timeStamp);

long endHeapSize = GC.GetAllocatedBytesForCurrentThread();
long allocated = endHeapSize - initialHeapSize;
long currentBytes = GC.GetTotalMemory(forceFullCollection: false);
int gen0After = GC.CollectionCount(0);
int gen1After = GC.CollectionCount(1);
int gen2After = GC.CollectionCount(2);

Console.WriteLine();
Console.WriteLine("----");
Console.WriteLine();
Console.WriteLine($"Time consumed: {elapsedTime} ms");
Console.WriteLine($"Allocated: {allocated / 1024} kb");
Console.WriteLine($"Current: {currentBytes / 1024} kb");
Console.WriteLine($"Gen0 cycles: {gen0After - gen0Before}");
Console.WriteLine($"Gen1 cycles: {gen1After - gen1Before}");
Console.WriteLine($"Gen2 cycles: {gen2After - gen2Before}");

//GC.EndNoGCRegion();

Console.ReadKey();
