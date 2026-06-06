using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Text;

namespace BillionChallenge;

public unsafe struct UnsafeSpan(byte* pointer, nuint length) : IEquatable<UnsafeSpan>, IComparable<UnsafeSpan>
{
    private const byte Semicolon = 0x3B;
    
    public readonly byte* Pointer = pointer;
    public nuint Length = length;
    
    public bool Equals(UnsafeSpan other) => SafeSpan.SequenceEqual(other.SafeSpan);
    
    public ReadOnlySpan<byte> SafeSpan => new(Pointer, (int)Length);
    
    // Trims Length to represent location only, returns temperature
    public (UnsafeSpan location, IntPtr temperature) ParseLine()
    {
        int semicolonIndex = (int)SimdIndexOf(Semicolon);
        var temperature = IntParser.Parse(new UnsafeSpan(Pointer + semicolonIndex + 1, Length - (nuint)semicolonIndex - 1));
        var locationSpan = new UnsafeSpan(Pointer, (nuint)semicolonIndex);
        
        return (locationSpan, temperature);
    }
    
    public nuint SimdIndexOf(byte byteToSearch)
    {
        const int vector256Length = 32;
        
        nuint startIndex = 0;
        while (true)
        {
            var searchSpanVector = Unsafe.ReadUnaligned<Vector256<byte>>(Pointer + startIndex);
            var byteToSearchVector = Vector256.Create(byteToSearch);
            var matchingVector = Vector256.Equals(searchSpanVector, byteToSearchVector);
            var bitmask = matchingVector.ExtractMostSignificantBits();
            if (bitmask == 0)
            {
                startIndex += vector256Length;
                continue;
            }
            
            return (nuint)BitOperations.TrailingZeroCount(bitmask) + startIndex;
        }
    }
    
    // FNV-1a
    public override int GetHashCode()
    {
        uint hash = 2166136261;

        for (nuint i = 0; i < Math.Min(4, Length); i++)
        {
            hash ^= Pointer[i];
            hash *= 16777619;
        }

        return (int)hash;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(UnsafeSpan other)
    {
        var minLength = Math.Min(SafeSpan.Length, other.SafeSpan.Length);
        int i = 0;
        
        while (i < minLength)
        { 
            var result = SafeSpan[i] - other.SafeSpan[i];
            if (result != 0)
            {
                return result;
            }

            i++;
        }
        
        return SafeSpan.Length - other.SafeSpan.Length;
    }
    
    public override string ToString() => new((sbyte*)Pointer, 0, (int)Length, Encoding.UTF8);
}
