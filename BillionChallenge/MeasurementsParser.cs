using System.Text;
using StreamReader = System.IO.StreamReader;

namespace BillionChallenge;

public static class MeasurementsParser
{
    private const int BufferSize = 4096;
    
    public static List<Measurement> Create(string filePath)
    {
        var measurements = new List<Measurement>(10_000_000);
        Span<char> buffer = new char[4096];
        int nextBufferStartIndex = 0;

        using var reader = new StreamReader(filePath, Encoding.UTF8, false, BufferSize * 32);
        while (true)
        {
            int charsRead = reader.Read(buffer[nextBufferStartIndex..]);
            if (charsRead == 0)
            {
                break;
            }

            var unprocessedChars = buffer[..(nextBufferStartIndex + charsRead)];
            while (true)
            {
                int endOfLineIndex = unprocessedChars.IndexOfAny('\r', '\n');
                if (endOfLineIndex == -1)
                {
                    break;
                }

                Span<char> line = unprocessedChars[..endOfLineIndex];
                ProcessLine(line, measurements);

                int skip = endOfLineIndex + 1;
                if (skip < unprocessedChars.Length && unprocessedChars[skip] == '\n')
                {
                    skip++;
                }

                unprocessedChars = unprocessedChars[skip..];
            }

            unprocessedChars.CopyTo(buffer);
            nextBufferStartIndex = unprocessedChars.Length;
        }
        
        if (nextBufferStartIndex > 0)
        {
            ProcessLine(buffer[..nextBufferStartIndex], measurements);
        }
        
        return measurements;
    }

    private static void ProcessLine(Span<char> line, List<Measurement> measurements)
    {
        if (line.IsEmpty)
        {
            return;
        }

        int semicolon = line.IndexOf(';');
        var location = line[..semicolon];
        var temperatureSpan = line[(semicolon + 1)..];
        var temperature = double.Parse(temperatureSpan);
        
        measurements.Add(new Measurement(location.ToString(), temperature));
    }
}
