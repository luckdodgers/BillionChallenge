namespace BillionChallenge;

public readonly struct Chunk(nuint startPosition, nuint length)
{
    public readonly nuint StartPosition = startPosition;
    public readonly nuint Length = length;
}
