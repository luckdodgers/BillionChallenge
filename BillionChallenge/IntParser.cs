using System.Runtime.CompilerServices;

namespace BillionChallenge;

public static class IntParser
{
    private const byte Minus = 0x2D;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static nint Parse(UnsafeSpan span)
    {
        nint multiplier = 1;
        nint fractionDigit = span.IndexFromEnd(1) - 48;
        nint lastIntDigit = span.IndexFromEnd(3) - 48;
        nint firstIntDigit = 0;
        
        switch (span.Length)
        {
            case 4 when span.IndexFromEnd(4) == Minus:
                multiplier = -1;
                break;
            case 4:
                firstIntDigit = span.IndexFromEnd(4) - 48;
                break;
            case 5:
                firstIntDigit = span.IndexFromEnd(4) - 48;
                multiplier = -1;
                break;
        }

        var result = (fractionDigit + lastIntDigit * 10 + firstIntDigit * 100) * multiplier;

        return result;
    }
}
