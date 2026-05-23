using System.Text;

namespace BillionChallenge;

public readonly unsafe struct UnsafeSpan(byte* pointer, nuint length) : IEquatable<UnsafeSpan>
{
    public bool Equals(UnsafeSpan other) => Span.SequenceEqual(other.Span);
    
    private ReadOnlySpan<byte> Span => new(pointer, (int)length);

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

    public override string ToString() => new((sbyte*)pointer, 0, (int)length, Encoding.UTF8);
}
