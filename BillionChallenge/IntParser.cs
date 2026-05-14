namespace BillionChallenge;

public static class IntParser
{
    private const byte Minus = 0x2D;
    
    public static nint Parse(ReadOnlySpan<byte> bytes)
    {
        nint multiplier = 1;
        nint fractionDigit = bytes[^1] - 48;
        nint lastIntDigit = bytes[^3] - 48;
        nint firstIntDigit = 0;
        
        switch (bytes.Length)
        {
            case 4 when bytes[^4] == Minus:
                multiplier = -1;
                break;
            case 4:
                firstIntDigit = bytes[^4] - 48;
                break;
            case 5:
                firstIntDigit = bytes[^4] - 48;
                multiplier = -1;
                break;
        }
        
        return (fractionDigit + lastIntDigit * 10 + firstIntDigit * 100) * multiplier;
    }
}
