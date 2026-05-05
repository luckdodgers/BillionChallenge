namespace BillionChallenge;

public readonly struct Measurement(string location, double temperature)
{
    public readonly string Location = location;
    public readonly double Temperature = temperature;
}
