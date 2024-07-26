namespace WmiPnp.Xm4;

public record Xm4State
{
    public bool Connected { get; init; }
    public int BatteryLevel { get; init; }

    public Xm4State()
    {
        Connected = false;
        BatteryLevel = -1;
    }
}
