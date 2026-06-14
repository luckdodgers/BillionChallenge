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

    public void UpdateResultDictionary(ArenaDictionary resultDictionary)
    {
        int semicolonIndex = (int)SimdIndexOf(Semicolon);
        var temperature = IntParser.Parse(new UnsafeSpan(Pointer + semicolonIndex + 1, Length - (nuint)semicolonIndex - 1));
        var locationSpan = new UnsafeSpan(Pointer, (nuint)semicolonIndex);
        ref var measurements = ref resultDictionary.GetRefValueOrAddDefault(locationSpan);
        measurements.Update(temperature);
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
            switch (bitmask)
            {
                case 0:
                    startIndex += vector256Length;
                    continue;
                default:
                    return (nuint)BitOperations.TrailingZeroCount(bitmask) + startIndex;
            }
        }
    }
    
    // FNV-1a
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
                var thisVector = Unsafe.ReadUnaligned<Vector256<byte>>(Pointer + i);
                var otherVector = Unsafe.ReadUnaligned<Vector256<byte>>(other.Pointer + i);

                var equalityVector = Vector256.Equals(thisVector, otherVector);
                uint mask = ~equalityVector.ExtractMostSignificantBits();

                if (mask != 0)
                {
                    int lane = BitOperations.TrailingZeroCount(mask);
                    return Pointer[i + (nuint)lane] - other.Pointer[i + (nuint)lane];
                }

                i += 32;
            }
        }
        
        while (i < minLength)
        {
            int diff = Pointer[i] - other.Pointer[i];
            if (diff != 0)
            {
                return diff;
            }
            i++;
        }

        return (int)Length - (int)other.Length;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public bool Equals(UnsafeSpan other)
    {
        if (Length != other.Length)
        {
            return false;
        }

        nuint length = Length;
        nuint i = 0;
        
        if (length >= 32)
        {
            nuint limit = length - 32;
            while (i <= limit)
            {
                var thisVector = Unsafe.ReadUnaligned<Vector256<byte>>(Pointer + i);
                var otherVector = Unsafe.ReadUnaligned<Vector256<byte>>(other.Pointer + i);
                if (Vector256.Equals(thisVector, otherVector).ExtractMostSignificantBits() != 0xFFFFFFFF)
                {
                    return false;
                }

                i += 32;
            }
        }
        
        while (i < length)
        {
            if (Pointer[i] != other.Pointer[i])
            {
                return false;
            }
            
            i++;
        }

        return true;
    }

    // 1-based index
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte IndexFromEnd(nuint index) => *(Pointer + Length - index);
    
    public override string ToString() => new((sbyte*)Pointer, 0, (int)Length, Encoding.UTF8);
}
