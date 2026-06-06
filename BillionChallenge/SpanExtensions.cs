using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace BillionChallenge;

public static class SpanExtensions
{
    public static unsafe long SimdIndexOf(this ReadOnlySpan<byte> span, byte byteToSearch)
    {
        const int vector256Length = 32;

        if (span.IsEmpty)
        {
            return -1;
        }
        
        fixed (byte* spanStartPointer = &span[0])
        {
            int startIndex = 0;
            while (startIndex < span.Length)
            {
                var searchSpanVector = Unsafe.ReadUnaligned<Vector256<byte>>(spanStartPointer + startIndex);
                var byteToSearchVector = Vector256.Create(byteToSearch);
                var matchingVector = Vector256.Equals(searchSpanVector, byteToSearchVector);
                var bitmask = matchingVector.ExtractMostSignificantBits();
                if (bitmask == 0)
                {
                    startIndex += vector256Length;
                    continue;
                }
            
                return BitOperations.TrailingZeroCount(bitmask) + startIndex;
            }   
        }
        
        return -1;
    }
}
