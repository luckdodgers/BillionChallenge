using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace BillionChallenge;

public static class SpanExtensions
{
    public static int SimdIndexOf(this ReadOnlySpan<byte> span, byte byteToSearch)
    {
        int startIndex = 0;
        int iterations = 0;
        Vector<byte> matchVector;
        
        while (true)
        {
            if (startIndex + startIndex + Vector<byte>.Count >= span.Length)
            {
                var result = span[startIndex..].IndexOf(byteToSearch);
                return result != -1 ? result + Vector<byte>.Count * iterations : result;
            }
            
            var spanVector = new Vector<byte>(span[startIndex..(startIndex + Vector<byte>.Count)]);
            var searchVector = new Vector<byte>(byteToSearch);
            matchVector = Vector.Equals(spanVector, searchVector);
            if (!matchVector.Equals(Vector<byte>.Zero))
            {
                break;
            }
            
            startIndex += Vector<byte>.Count;
            iterations++;
        }

        var matchVector256 = matchVector.AsVector256();
        var bitmask = Avx2.MoveMask(matchVector256);
        var trailingZerosCount = BitOperations.TrailingZeroCount(bitmask);

        var test = BitOperations.PopCount((nuint)bitmask);
        
        return startIndex + trailingZerosCount;
    }
}
