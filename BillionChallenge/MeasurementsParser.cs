using System.Text;

namespace BillionChallenge;

public static class MeasurementsParser
{
    private const byte NewLine = 0x0A; // \n
    private const byte CarriageReturn = 0x0D; // \r
    private const byte Semicolon = 0x3B;
    
    public static Dictionary<string, Measurements> Create(string filePath)
    {
        using var file = new FileStream(
            filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, FileOptions.SequentialScan);
        var dictionary = new Dictionary<string, Measurements>(16_000);
        
        Span<byte> buffer = new byte[65536];
        int nextBufferStartIndex = 0;
        while (true)
        {
            int bytesRead = file.Read(buffer[nextBufferStartIndex..]);
            if (bytesRead == 0)
            {
                break;
            }
            
            var unprocessedChars = buffer[..(nextBufferStartIndex + bytesRead)];
            while (true)
            {
                int endOfLineIndex = unprocessedChars.IndexOfAny(NewLine, CarriageReturn);
                if (endOfLineIndex == -1)
                {
                    break;
                }

                Span<byte> line = unprocessedChars[..endOfLineIndex];
                ProcessLine(line, dictionary);
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

        return dictionary;
    }

    private static void ProcessLine(Span<byte> line, Dictionary<string, Measurements> resultDictionary)
    {
        if (line.IsEmpty)
        {
            return;
        }

        int semicolon = line.IndexOf(Semicolon);
        var location = Encoding.UTF8.GetString(line[..semicolon]);
        var temperature = double.Parse(line[(semicolon + 1)..]);
        if (!resultDictionary.TryGetValue(location, out var measurements))
        {
            measurements = new Measurements();
        }
        
        measurements.Update(temperature);
        resultDictionary[location] = measurements;
    }
}
