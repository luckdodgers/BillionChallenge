using System.Runtime.CompilerServices;
using System.Text;

namespace BillionChallenge;

public readonly unsafe struct UnsafeSpan(byte* pointer, nuint length) : IEquatable<UnsafeSpan>, IComparable<UnsafeSpan>
{
    public bool Equals(UnsafeSpan other) => Span.SequenceEqual(other.Span);
    
    private ReadOnlySpan<byte> Span => new(pointer, (int)length);
    
    // FNV-1a
    public override int GetHashCode()
    {
        uint hash = 2166136261;

        for (nuint i = 0; i < Math.Min(4, length); i++)
        {
            hash ^= pointer[i];
            hash *= 16777619;
        }

        return (int)hash;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(UnsafeSpan other)
    {
        var minLength = Math.Min(Span.Length, other.Span.Length);
        int i = 0;
        
        while (i < minLength)
        { 
            var result = Span[i] - other.Span[i];
            if (result != 0)
            {
                return result;
            }

            i++;
        }
        
        return Span.Length - other.Span.Length;
    }
    
    public override string ToString() => new((sbyte*)pointer, 0, (int)length, Encoding.UTF8);
}
