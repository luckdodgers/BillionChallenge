using System.Text;
using StreamReader = System.IO.StreamReader;

namespace BillionChallenge;

public static class MeasurementsParser
{
    private const int BufferSize = 4096;
    
    public static Dictionary<string, Measurements> Create(string filePath)
    {
        var dictionary = new Dictionary<string, Measurements>(10_000);
        
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
                ProcessLine(line, dictionary);

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

        return dictionary;
    }

    private static void ProcessLine(Span<char> line, Dictionary<string, Measurements> resultDictionary)
    {
        if (line.IsEmpty)
        {
            return;
        }

        int semicolon = line.IndexOf(';');
        var location = line[..semicolon];
        var temperatureSpan = line[(semicolon + 1)..];
        var temperature = double.Parse(temperatureSpan);
        if (!resultDictionary.TryGetValue(location.ToString(), out var measurements))
        {
            measurements = new Measurements();
        }
        
        measurements.Update(temperature);
        resultDictionary[location.ToString()] = measurements;
    }
}
