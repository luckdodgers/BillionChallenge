using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace BillionChallenge;

public class ResultDictionary : IEnumerable<KeyValuePair<UnsafeSpan, Measurements>>
{
    private const int Capacity = 16384;
    private const int CapacityMask = Capacity - 1;
    private const int MaxElements = 10_000;
    private const int MaxKeyBytes = 100;
    
    private readonly Entry[] _entries = new Entry[Capacity];
    private readonly byte[] _keysArena = GC.AllocateArray<byte>(MaxElements * MaxKeyBytes, pinned: true);
    private readonly unsafe byte* _arenaPointer;
    
    private nuint _arenaTopFreeIndex;

    public unsafe ResultDictionary()
    {
        _arenaPointer = (byte*)Unsafe.AsPointer(ref _keysArena[0]);
    }

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
                Unsafe.CopyBlockUnaligned(_arenaPointer + _arenaTopFreeIndex, location.Pointer, (uint)location.Length);
                
                _arenaTopFreeIndex += location.Length;
                entry.KeyOffset = offset;
                entry.KeyLength = location.Length;
                entry.HashCode = keyHash;
                
                return ref entry.Value;
            }

            if (entry.HashCode == keyHash && 
                entry.KeyLength == location.Length && 
                new UnsafeSpan(_arenaPointer + entry.KeyOffset, entry.KeyLength).UnsafeEquals(location))
            {
                return ref entry.Value;
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
                    _current = GetCurrentEntry();
                    return true;
                }
                index++;
            }
 
            _currentIndex = index;
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
            return new KeyValuePair<UnsafeSpan, Measurements>(
                new UnsafeSpan(_dict._arenaPointer + entry.KeyOffset, entry.KeyLength), entry.Value);
        }
    }
}
