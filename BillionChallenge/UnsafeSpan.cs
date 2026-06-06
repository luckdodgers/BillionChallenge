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
    
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public int CompareTo(UnsafeSpan other)
    {
        nuint minLength = Length < other.Length ? Length : other.Length;
        nuint i = 0;
        
        if (minLength >= 32)
        {
            nuint limit = minLength - 32;
            while (i <= limit)
            {
                var a = Unsafe.ReadUnaligned<Vector256<byte>>(Pointer + i);
                var b = Unsafe.ReadUnaligned<Vector256<byte>>(other.Pointer + i);

                var eq = Vector256.Equals(a, b);
                uint mask = ~eq.ExtractMostSignificantBits();

                if (mask != 0)
                {
                    int lane = BitOperations.TrailingZeroCount(mask);
                    return Pointer[i + (nuint)lane] - other.Pointer[i + (nuint)lane];
                }

                i += 32;
            }
        }
        
        if (i + 16 <= minLength)
        {
            var a = Unsafe.ReadUnaligned<Vector128<byte>>(Pointer + i);
            var b = Unsafe.ReadUnaligned<Vector128<byte>>(other.Pointer + i);
        
            var eq = Vector128.Equals(a, b);
            uint mask = (~(uint)eq.ExtractMostSignificantBits()) & 0xFFFF;
        
            if (mask != 0)
            {
                int lane = BitOperations.TrailingZeroCount(mask);
                return Pointer[i + (nuint)lane] - other.Pointer[i + (nuint)lane];
            }
        
            i += 16;
        }
        
        while (i < minLength)
        {
            int diff = Pointer[i] - other.Pointer[i];
            if (diff != 0) return diff;
            i++;
        }

        return (int)Length - (int)other.Length;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public bool UnsafeEquals(UnsafeSpan other)
    {
        if (Length != other.Length)
            return false;

        nuint length = Length;
        nuint i = 0;
        
        if (length >= 32)
        {
            nuint limit = length - 32;
            while (i <= limit)
            {
                var a = Unsafe.ReadUnaligned<Vector256<byte>>(Pointer + i);
                var b = Unsafe.ReadUnaligned<Vector256<byte>>(other.Pointer + i);
                if (Vector256.Equals(a, b).ExtractMostSignificantBits() != 0xFFFFFFFF)
                    return false;

                i += 32;
            }
        }
        
        if (i + 16 <= length)
        {
            var a = Unsafe.ReadUnaligned<Vector128<byte>>(Pointer + i);
            var b = Unsafe.ReadUnaligned<Vector128<byte>>(other.Pointer + i);

            if ((Vector128.Equals(a, b).ExtractMostSignificantBits() & 0xFFFF) != 0xFFFF)
                return false;

            i += 16;
        }
        
        while (i < length)
        {
            if (Pointer[i] != other.Pointer[i])
                return false;
            i++;
        }

        return true;
    }
    
    public override string ToString() => new((sbyte*)Pointer, 0, (int)Length, Encoding.UTF8);
}
