namespace BillionChallenge;

public class MeasurementsProcessor : IDisposable
{
    private readonly string _filePath;
    private readonly FileStream _fileStream;

    private const byte NewLine = 0x0A; // \n

    public MeasurementsProcessor(string filePath)
    {
        _filePath = filePath;
        _fileStream = new FileStream(
            filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 65536, FileOptions.SequentialScan);
    }
    
    public ResultDictionary Create(out PerformanceCounter performanceCounter)
    {
        performanceCounter = new PerformanceCounter();
        performanceCounter.Start();
        
        var chunks = GetChunks(_fileStream);
        var result = chunks
            .AsParallel()
#if DEBUG
            .WithDegreeOfParallelism(1)
#endif
            .Select<Chunk, (ResultDictionary resultDictionary, long bytesAllocated)>(ProcessChunk)
            .Aggregate((aggregated, chunk) =>
            {
                foreach (var chunkSummary in chunk.resultDictionary)
                {
                    ref var measurements = ref aggregated.resultDictionary.GetRefValueOrAddDefault(chunkSummary.Key);
                    measurements.Merge(chunkSummary.Value);
                }
                
                aggregated.bytesAllocated += chunk.bytesAllocated;
                
                return aggregated;
            });
        
        performanceCounter.AddHeapAllocations(result.bytesAllocated);
        
        return result.resultDictionary;
    }
    
    private static List<Chunk> GetChunks(FileStream file)
    {
        var chunks = new List<Chunk>(Environment.ProcessorCount);
        var chunkSize = file.Length / Environment.ProcessorCount;
        nint endByteIndex = -2;
        
        for (var coreNumber = 0; coreNumber < Environment.ProcessorCount; coreNumber++)
        {
            nint startByteIndex = endByteIndex + 2;
            endByteIndex = startByteIndex + (nint)chunkSize;
            
            while (endByteIndex < file.Length && !IsNewLine(file, endByteIndex))
            {
                endByteIndex++;
            }

            var length = endByteIndex + 1 - startByteIndex;
            var chunkIndexes = new Chunk((nuint)startByteIndex, (nuint)length);
            chunks.Add(chunkIndexes);
        }
        
        return chunks;
    }

    private unsafe (ResultDictionary result, long bytesAllocated) ProcessChunk(Chunk chunk)
    {
        var initialHeapSize = GC.GetAllocatedBytesForCurrentThread();
        
        const long bufferSize = 4096;
        using var fileHandle = File.OpenHandle(
            _filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, FileOptions.SequentialScan);
        
        var resultDictionary = new ResultDictionary();
        var buffer = new byte[bufferSize];
        var bytesLeft = (long)chunk.Length;
        var startIndex = (long)chunk.StartPosition;

        fixed (byte* segmentPtr = &buffer[0])
        {
            while (bytesLeft > 0)
            {
                var bytesToRead = Math.Min(bufferSize, bytesLeft);
                var bufferSpan = buffer.AsSpan(0, (int)bytesToRead);
                
                RandomAccess.Read(fileHandle, bufferSpan, startIndex);
                var endlineIndex = bufferSpan.SimdIndexOf(NewLine);
                var lineSpan = new UnsafeSpan(segmentPtr, (nuint)endlineIndex);
                var parsedLine = lineSpan.ParseLine();
                
                ref var measurements = ref resultDictionary.GetRefValueOrAddDefault(parsedLine.location);
                measurements.Update(parsedLine.temperature);

                startIndex += endlineIndex + 1;
                bytesLeft -= endlineIndex + 1;
            }   
        }
        
        var finalHeapSize = GC.GetAllocatedBytesForCurrentThread();

        return (resultDictionary, finalHeapSize - initialHeapSize);
    }
    
    private static bool IsNewLine(FileStream file, long index)
    {
        file.Seek(index, SeekOrigin.Begin);
        var @byte = file.ReadByte();
        
        return @byte is NewLine;
    }

    public void Dispose()
    {
        _fileStream.Dispose();
    }
}
