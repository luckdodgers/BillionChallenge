using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;
using Microsoft.Win32.SafeHandles;

namespace BillionChallenge;

public class MeasurementsProcessor : IDisposable
{
    private readonly nint _pointer;
    private readonly FileStream _fileStream;
    private readonly MemoryMappedFile _mmf;
    private readonly MemoryMappedViewAccessor _accessor;
    private readonly SafeMemoryMappedViewHandle _safeHandle;

    private const byte NewLine = 0x0A; // \n
    private const byte Semicolon = 0x3B;

    public unsafe MeasurementsProcessor(string filePath)
    {
        _fileStream = new FileStream(
            filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 65536, FileOptions.SequentialScan);
        _mmf = MemoryMappedFile.CreateFromFile(
            _fileStream, null, 0, MemoryMappedFileAccess.Read, HandleInheritability.None, true);
        _accessor = _mmf.CreateViewAccessor(0, _fileStream.Length, MemoryMappedFileAccess.Read);
        _safeHandle = _accessor.SafeMemoryMappedViewHandle;
        
        byte* ptr = null;
        _safeHandle.AcquirePointer(ref ptr);
        _pointer = (nint)(ptr + _accessor.PointerOffset);
    }
    
    public Dictionary<UnsafeSpan, Measurements> Create(out PerformanceCounter performanceCounter)
    {
        performanceCounter = new PerformanceCounter();
        performanceCounter.Start();
        
        var chunks = GetChunks(_fileStream);
        var result = chunks
            .AsParallel()
            .WithDegreeOfParallelism(Environment.ProcessorCount)
            .Select<Chunk, (Dictionary<UnsafeSpan, Measurements> measurementsDictionary, long bytesAllocated)>(ProcessChunk)
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
        long endByteIndex = -2;
        
        for (var coreNumber = 0; coreNumber < Environment.ProcessorCount; coreNumber++)
        {
            var startByteIndex = endByteIndex + 2;
            endByteIndex = startByteIndex + chunkSize;
            
            while (endByteIndex < file.Length && !IsNewLineOrDefaultByte(file, endByteIndex))
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

    private unsafe (Dictionary<UnsafeSpan, Measurements> result, long bytesAllocated) ProcessChunk(Chunk chunk)
    {
        var initialHeapSize = GC.GetAllocatedBytesForCurrentThread();
        var ptr = (byte*)_pointer + (nint)chunk.StartPosition;
        var initialPtr = ptr;
        
        var dictionary = new Dictionary<UnsafeSpan, Measurements>(16_000);
        long bytesRead = 0;

        while (true)
        {
            var bytesLeftToRead = chunk.Length - bytesRead;
            if (bytesLeftToRead <= 0)
            {
                break;
            }
            var bytesToRead = (int)Math.Min(150, bytesLeftToRead);
            var buffer = new Span<byte>(ptr, bytesToRead);
            if (buffer[0] == 0)
            {
                break;
            }
            var newLineIndex = buffer.SimdIndexOf(NewLine);
            var foundNewLine = newLineIndex != -1;
            if (!foundNewLine)
            {
                newLineIndex = buffer.Length;
            }
            newLineIndex = newLineIndex == -1 ? buffer.Length : newLineIndex;
            var lineSpan = buffer[..newLineIndex];
            
            ProcessLine(lineSpan, dictionary);
            
            bytesRead += lineSpan.Length;
            if (foundNewLine)
            {
                bytesRead++;
            }
            
            ptr = initialPtr + (int)bytesRead;
        }
        
        var finalHeapSize = GC.GetAllocatedBytesForCurrentThread();

        return (dictionary, finalHeapSize - initialHeapSize);
    }

    private static unsafe void ProcessLine(Span<byte> line, Dictionary<UnsafeSpan, Measurements> resultDictionary)
    {
        int semicolon = line.IndexOf(Semicolon);
        var pointer = Unsafe.AsPointer(ref line[0]);
        var locationSpan = new UnsafeSpan((byte*)pointer, (uint)semicolon);
        var temperature = IntParser.Parse(line[(semicolon + 1)..]);
        
        if (!resultDictionary.TryGetValue(locationSpan, out var measurements))
        {
            measurements = new Measurements();
        }
        
        measurements.Update(temperature);
        resultDictionary[locationSpan] = measurements;
    }
    
    private static bool IsNewLineOrDefaultByte(FileStream file, long index)
    {
        file.Seek(index, SeekOrigin.Begin);
        var @byte = file.ReadByte();
        
        return @byte is NewLine or 0;
    }

    public void Dispose()
    {
        _safeHandle.ReleasePointer();
        _safeHandle.Dispose();
        _accessor.Dispose();
        _mmf.Dispose();
        _fileStream.Dispose();
    }
}
