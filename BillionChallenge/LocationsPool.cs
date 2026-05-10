namespace BillionChallenge;

public class LocationsPool
{
    private Dictionary<string, string> _locations = new(10_000);
    
    public string GetOrAdd(Span<char> locationSpan)
    {
        var lookup = _locations.GetAlternateLookup<ReadOnlySpan<char>>();
        if (!lookup.TryGetValue(locationSpan, out var location))
        {
            location = new string(locationSpan);
            _locations[location] = location;
        }
        
        return location;
    }
}
