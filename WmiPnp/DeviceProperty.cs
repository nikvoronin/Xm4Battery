using System.Management;

namespace WmiPnp;

public class DeviceProperty
{
    public string? Data;
    public readonly string? DeviceId;
    public readonly string? Key;
    public readonly string? KeyName; // like DEVPKEY_Device_InLocalMachineContainer
    public readonly CimType Type;

    public DeviceProperty(
        string? data,
        string? deviceId,
        string? key,
        string? keyName,
        CimType type )
    {
        Data = data;
        DeviceId = deviceId;
        Key = key;
        KeyName = keyName;
        Type = type;
    }

    public DeviceProperty(
        string? deviceId,
        string? key,
        string? keyName,
        CimType type )
    {
        DeviceId = deviceId;
        Key = key;
        KeyName = keyName;
        Type = type;
    }
}