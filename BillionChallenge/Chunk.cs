namespace BillionChallenge;

public readonly struct Chunk(long startPosition, long length)
{
    public readonly long StartPosition = startPosition;
    public readonly long Length = length;
}
