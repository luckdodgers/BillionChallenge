using System.Diagnostics;
using BillionChallenge;

//GC.TryStartNoGCRegion(300_000_000);

long initialHeapSize = GC.GetAllocatedBytesForCurrentThread();
int gen0Before = GC.CollectionCount(0);
int gen1Before = GC.CollectionCount(1);
int gen2Before = GC.CollectionCount(2);

var filePath = args.Length > 0 ? args[0] : Directory.GetCurrentDirectory() + "/measurements3.txt";
var timeStamp = Stopwatch.GetTimestamp();
Application.PrintResult(filePath);
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
Console.WriteLine($"Time consumed: {elapsedTime}");
Console.WriteLine($"Parsing time: {Application.ParsingTime}");
Console.WriteLine($"Calculations time: {Application.CalculationTime}");
Console.WriteLine($"Allocated: {allocated / 1024} kb");
Console.WriteLine($"Current: {currentBytes / 1024} kb");
Console.WriteLine($"Gen0 cycles: {gen0After - gen0Before}");
Console.WriteLine($"Gen1 cycles: {gen1After - gen1Before}");
Console.WriteLine($"Gen2 cycles: {gen2After - gen2Before}");

//GC.EndNoGCRegion();

Console.ReadKey();
