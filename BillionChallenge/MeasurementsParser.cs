using System.IO.MemoryMappedFiles;
using System.Text;

namespace BillionChallenge;

public static class MeasurementsParser
{
    private const byte NewLine = 0x0A; // \n
    private const byte CarriageReturn = 0x0D; // \r
    private const byte Semicolon = 0x3B;
    
    public static Dictionary<string, Measurements> Create(string filePath, out PerformanceCounter performanceCounter)
    {
        performanceCounter = new PerformanceCounter();
        performanceCounter.Start();
        
        using var file = new FileStream(
            filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 65536, FileOptions.SequentialScan);
        using var mmf = MemoryMappedFile.CreateFromFile(
            file, null, 0, MemoryMappedFileAccess.Read, HandleInheritability.None, true);
        
        var chunks = GetChunks(file);
        var result = chunks
            .AsParallel()
            .Select<Chunk, (Dictionary<string, Measurements> measurementsDictionary, long bytesAllocated)>(x => 
                ProcessChunk(mmf, x))
            .Aggregate((aggregated, chunk) =>
            {
                foreach (var summary in chunk.measurementsDictionary)
                {
                    if (!aggregated.measurementsDictionary.TryGetValue(summary.Key, out var measurements))
                    {
                        measurements = new Measurements();
                    }
                
                    measurements.Merge(summary.Value);
                    aggregated.measurementsDictionary[summary.Key] = measurements;
                }
                
                aggregated.bytesAllocated += chunk.bytesAllocated;
                
                return aggregated;
            });
        
        performanceCounter.AddHeapAllocations(result.bytesAllocated);
        
        return result.measurementsDictionary;
    }
    
    private static List<Chunk> GetChunks(FileStream file)
    {
        var chunks = new List<Chunk>(Environment.ProcessorCount);
        var chunkSize = file.Length / Environment.ProcessorCount;
        long endByteIndex = -1;
        
        for (var coreNumber = 0; coreNumber < Environment.ProcessorCount; coreNumber++)
        {
            var startByteIndex = endByteIndex + 1;
            while (IsNewLineOrDefaultByte(file, startByteIndex))
            {
                startByteIndex++;
            }

            endByteIndex = startByteIndex + chunkSize;
            
            while (!IsNewLineOrDefaultByte(file, endByteIndex) && endByteIndex < file.Length)
            {
                endByteIndex++;
            }

            endByteIndex--;

            var length = endByteIndex + 1 - startByteIndex;
            var chunkIndexes = new Chunk(startByteIndex, length);
            chunks.Add(chunkIndexes);
        }
        
        return chunks;
    }

    private static (Dictionary<string, Measurements> result, long bytesAllocated) ProcessChunk(
        MemoryMappedFile mmf, Chunk chunk)
    {
        var initialHeapSize = GC.GetAllocatedBytesForCurrentThread();
        
        using var viewStream = mmf.CreateViewStream(0, 0, MemoryMappedFileAccess.Read);
        viewStream.Seek(chunk.StartPosition, SeekOrigin.Begin);
        var dictionary = new Dictionary<string, Measurements>(16_000);
        var locationsPool = new LocationsPool();

        Span<byte> buffer = new byte[32768];
        int nextBufferStartIndex = 0;
        long bytesRemaining = chunk.Length;
        
        while (true)
        {
            var bytesToRead = (int)Math.Min(buffer.Length - nextBufferStartIndex, bytesRemaining);
            var bytesRead = viewStream.Read(buffer[nextBufferStartIndex..(nextBufferStartIndex + bytesToRead)]);
            if (bytesRead == 0)
            {
                break;
            }
            
            bytesRemaining -= bytesRead;
            var unprocessedChars = buffer[..(nextBufferStartIndex + bytesRead)];
            
            while (true)
            {
                int endOfLineIndex = unprocessedChars.SimdIndexOf(NewLine);
                if (endOfLineIndex == -1)
                {
                    break;
                }

                Span<byte> line = unprocessedChars[..endOfLineIndex];
                ProcessLine(line, locationsPool, dictionary);
                
                int skip = endOfLineIndex + 1;
                if (skip < unprocessedChars.Length && unprocessedChars[skip] == NewLine)
                {
                    skip++;
                }
                
                unprocessedChars = unprocessedChars[skip..];
            }
            
            unprocessedChars.CopyTo(buffer);
            nextBufferStartIndex = unprocessedChars.Length;
        }
        
        var finalHeapSize = GC.GetAllocatedBytesForCurrentThread();

        return (dictionary, finalHeapSize - initialHeapSize);
    }

    private static void ProcessLine(
        Span<byte> line, LocationsPool locationsPool, Dictionary<string, Measurements> resultDictionary)
    {
        if (line.IsEmpty)
        {
            return;
        }

        int semicolon = line.IndexOf(Semicolon);
        Span<char> locationChars = stackalloc char[line[..semicolon].Length];
        Encoding.UTF8.GetChars(line[..semicolon], locationChars);
        var location = locationsPool.GetOrAdd(locationChars);
        var temperature = IntParser.Parse(line[(semicolon + 1)..]);
        
        if (!resultDictionary.TryGetValue(location, out var measurements))
        {
            measurements = new Measurements();
        }
        
        measurements.Update(temperature);
        resultDictionary[location] = measurements;
    }
    
    private static bool IsNewLineOrDefaultByte(FileStream file, long index)
    {
        file.Seek(index, SeekOrigin.Begin);
        var @byte = file.ReadByte();
        
        return @byte is NewLine or CarriageReturn or 0;
    }
}
