namespace WmiPnp;

public class DeviceProperty : IEquatable<DeviceProperty>
{
    public readonly string DeviceId; // ex. BTHENUM\{0000111E-0000-1000-8000-00805F9B34FB}_VID&0002054C_PID&0D58\8&37022D96&0&F84E17FE9B55_C00000000
    public readonly string Key; // ex. {540B947E-8B40-45BC-A8A2-6A0B894CBDA2} 8
    public readonly string KeyName; // ex. DEVPKEY_Device_InLocalMachineContainer
    public readonly uint Type; // See DataType
    public readonly object? Data;

    public DeviceProperty(
        string deviceId,
        string key,
        string keyName,
        uint type,

        object? data = null )
    {
        DeviceId = deviceId;
        Key = key;
        Type = type;

        KeyName = keyName ?? key;
        Data = data;
    }

    public DeviceProperty(
        string deviceId,
        string key,
        uint type,

        object? data = null )
    {
        DeviceId = deviceId;
        Key = key;
        Type = type;

        KeyName = key;
        Data = data;
    }

    #region Equality comparison
    /// <inheritdoc/>
    public override bool Equals( object? obj ) =>
        obj is DeviceProperty e
        && Equals( e );

    /// <inheritdoc/>
    public bool Equals( DeviceProperty? other ) =>
        string.Equals( other?.DeviceId, DeviceId, StringComparison.Ordinal )
        && string.Equals( other?.Key, Key, StringComparison.Ordinal );

    /// <inheritdoc/>
    public override int GetHashCode() =>
        HashCode.Combine( DeviceId, Key );

    /// <inheritdoc/>
    public static bool operator ==( DeviceProperty left, DeviceProperty right ) =>
        left.Equals( right );

    /// <inheritdoc/>
    public static bool operator !=( DeviceProperty left, DeviceProperty right ) =>
        !(left == right);
    #endregion

    public const string Type_PropertyField = "Type";
    public const string DeviceID_PropertyField = "DeviceID";
    public const string Key_PropertyField = "key";
    public const string KeyName_PropertyField = "keyName";
    public const string Data_PropertyField = "Data";
}

public static class DevicePropertiesExtensions
{
    /// <summary>
    /// Clone existing <see cref="DeviceProperty"/>
    /// with updated Data field.
    /// </summary>
    /// <param name="deviceProperty">
    /// <see cref="DeviceProperty"/>.
    /// </param>
    /// <param name="data">
    /// Data object.
    /// </param>
    /// <returns>
    /// A new <see cref="DeviceProperty"/> with updated data.
    /// </returns>
    public static DeviceProperty With(
        this DeviceProperty deviceProperty,
        object? data )
        => new(
            deviceProperty.DeviceId,
            deviceProperty.Key,
            deviceProperty.KeyName,
            deviceProperty.Type,
            data );

}