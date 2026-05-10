using BillionChallenge;

var filePath = args.Length > 0 ? args[0] : Directory.GetCurrentDirectory() + "/measurements3.txt";

var counter = Application.PrintResult(filePath);

Console.WriteLine();
Console.WriteLine("----");
Console.WriteLine();
Console.WriteLine($"Time consumed: {counter.ElapsedTime}");
Console.WriteLine($"Allocated: {counter.TotalHeapAllocations / 1024} kb");
Console.WriteLine($"Total GC pause: {counter.Total_GC_pause_ms.TotalMilliseconds} ms");
Console.WriteLine($"Gen0 cycles: {counter.GC_0_cycles}");
Console.WriteLine($"Gen1 cycles: {counter.GC_1_cycles}");
Console.WriteLine($"Gen2 cycles: {counter.GC_2_cycles}");

Console.ReadKey();
