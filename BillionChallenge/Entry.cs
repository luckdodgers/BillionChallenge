using System.Runtime.InteropServices;

namespace BillionChallenge;

[StructLayout(LayoutKind.Sequential)]
public struct Entry
{
    public int HashCode;
    public nuint KeyOffset;
    public nuint KeyLength;
    public Measurements Value;
    private int _padding;
}
