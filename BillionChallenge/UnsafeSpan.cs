using System.Text;

namespace BillionChallenge;

public readonly unsafe struct UnsafeSpan(byte* pointer, nuint length) : IEquatable<UnsafeSpan>
{
    public readonly byte* Pointer = pointer;
    public readonly nuint Length = length;
    
    public ReadOnlySpan<byte> Span => new(Pointer, (int)Length);

    public bool Equals(UnsafeSpan other) => Span.SequenceEqual(other.Span);

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

    public override string ToString() => new((sbyte*)Pointer, 0, (int)Length, Encoding.UTF8);
}
