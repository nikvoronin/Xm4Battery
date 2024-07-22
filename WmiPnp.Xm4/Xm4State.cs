namespace WmiPnp.Xm4;

public readonly record struct Xm4State
{
    public readonly bool Connected { get; init; }
    public readonly int BatteryLevel { get; init; }

    public Xm4State()
    {
        Connected = false;
        BatteryLevel = -1;
    }
}
