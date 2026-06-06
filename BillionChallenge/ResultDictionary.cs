using System.Collections;
using System.Runtime.CompilerServices;

namespace BillionChallenge;

public class ResultDictionary : IEnumerable<KeyValuePair<UnsafeSpan, Measurements>>
{
    private const int Capacity = 16384;
    private const int CapacityMask = Capacity - 1;
    private const int MaxElements = 10_000;
    private const int MaxKeyBytes = 100;
    
    private readonly Entry[] _entries = new Entry[Capacity];
    private readonly byte[] _keysArena = new byte[MaxElements * MaxKeyBytes];
    
    private nuint _arenaTopFreeIndex;

    public unsafe ref Measurements GetRefValueOrAddDefault(UnsafeSpan location)
    {
        var keyHash = location.GetHashCode();
        int entriesIndex = keyHash & CapacityMask;

        while (true)
        {
            ref var entry = ref _entries[entriesIndex];
            if (entry.HashCode == 0)
            {
                var offset = _arenaTopFreeIndex;
                fixed (byte* freeBytePointer = &_keysArena[_arenaTopFreeIndex])
                {
                    Unsafe.CopyBlockUnaligned(freeBytePointer, location.Pointer, (uint)location.Length);
                }
                
                _arenaTopFreeIndex += location.Length;
                entry.KeyOffset = offset;
                entry.KeyLength = location.Length;
                entry.HashCode = keyHash;
                
                return ref entry.Value;
            }

            fixed (byte* locationStart = &_keysArena[entry.KeyOffset])
            {
                if (entry.HashCode == keyHash && new UnsafeSpan(locationStart, entry.KeyLength).Equals(location))
                {
                    return ref entry.Value;
                }
            }
 
            entriesIndex = (entriesIndex + 1) & CapacityMask;
        }
    }

    public IEnumerator<KeyValuePair<UnsafeSpan, Measurements>> GetEnumerator() => new Enumerator(this);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private struct Enumerator : IEnumerator<KeyValuePair<UnsafeSpan, Measurements>>
    {
        KeyValuePair<UnsafeSpan, Measurements> IEnumerator<KeyValuePair<UnsafeSpan, Measurements>>.Current => _current;
        object IEnumerator.Current => _current;
        
        private readonly ResultDictionary _dict;
        
        private int _currentIndex;
        private KeyValuePair<UnsafeSpan, Measurements> _current;

        internal Enumerator(ResultDictionary dict)
        {
            _dict  = dict;
            _currentIndex = -1;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            var entries = _dict._entries;
            var index = _currentIndex + 1;
 
            while (index < entries.Length)
            {
                if (entries[index].HashCode != 0)
                {
                    _currentIndex = index;
                    return true;
                }
                index++;
            }
 
            _currentIndex = index;
            _current = GetCurrentEntry();
            return false;
        }
        
        public void Reset()
        {
            _currentIndex = 0;
            _current = default;
        }
        
        public void Dispose() {}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe KeyValuePair<UnsafeSpan, Measurements> GetCurrentEntry()
        {
            ref var entry = ref _dict._entries[_currentIndex];
            fixed (byte* freeBytePointer = &_dict._keysArena[entry.KeyOffset])
            {
                return new KeyValuePair<UnsafeSpan, Measurements>(new UnsafeSpan(freeBytePointer, entry.KeyLength), entry.Value);
            }
        }
    }
}
