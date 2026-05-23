using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace BillionChallenge;

public static class SpanExtensions
{
    public const int Vector256Length = 32;
    
    public static int SimdIndexOf(this ReadOnlySpan<byte> span, byte byteToSearch)
    {
        int startIndex = 0;
        Vector256<byte> matchVector;
        
        while (true)
        {
            if (startIndex + startIndex + Vector256Length >= span.Length)
            {
                return span.IndexOf(byteToSearch);
            }
            
            var spanVector = Vector256.Create(span[startIndex..(startIndex + Vector256Length)]);
            var searchVector = Vector256.Create(byteToSearch);
            matchVector = Avx2.CompareEqual(spanVector, searchVector);
            if (!matchVector.Equals(Vector256<byte>.Zero))
            {
                break;
            }
            
            startIndex += Vector256Length;
        }
        
        var bitmask = Avx2.MoveMask(matchVector);
        var trailingZerosCount = BitOperations.TrailingZeroCount(bitmask);
        
        return startIndex + trailingZerosCount;
    }
}
