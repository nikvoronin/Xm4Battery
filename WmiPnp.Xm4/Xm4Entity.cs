using FluentResults;
using System.Management;

namespace WmiPnp.Xm4;

public sealed class Xm4Entity
{
    private readonly PnpEntity _xm4;
    private readonly PnpEntity _handsFree;

    private Xm4Entity( PnpEntity handsFree, PnpEntity xm4 )
    {
        _handsFree = handsFree;
        _xm4 = xm4;
    }

    public static Result<Xm4Entity> CreateDefault() =>
        Create(
            HandsFree_PnpEntity_FriendlyNameGeneral,
            Headphones_PnpEntity_FriendlyNameGeneral );

    public static Result<Xm4Entity> Create(
        string handsfreeName,
        string headphonesName )
    {
        handsfreeName ??= HandsFree_PnpEntity_FriendlyNameGeneral;
        headphonesName ??= Headphones_PnpEntity_FriendlyNameGeneral;

        var handsfree =
            PnpEntity.ByFriendlyName( handsfreeName );

        if (handsfree.IsFailed)
            return Result.Fail(
                $"Can not create {handsfreeName} entity" );

        var xm4headphones =
            PnpEntity.ByFriendlyName( headphonesName );

        if (xm4headphones.IsFailed)
            return Result.Fail(
                $"Can not create {headphonesName} entity" );

        return
            new Xm4Entity(
                handsfree.Value,
                xm4headphones.Value );
    }

    public static Result<Xm4Entity> CreateUnsafe(
        PnpEntity batteryEntity,
        PnpEntity stateEntity )
        => new Xm4Entity(
            batteryEntity,
            stateEntity );

    public int BatteryLevel {
        get {
            var level =
                _handsFree.GetDeviceProperty(
                    DeviceProperty_BatteryLevel )
                .ValueOrDefault
                ?.Data;

            return level switch {
                _ when level is byte byteData => byteData,
                _ when level is int intData => intData,
                _ => 0
            };
        }
    }

    public bool IsConnected {
        get {
            var connected =
                _xm4.GetDeviceProperty(
                    DeviceProperty_IsConnected )
                .ValueOrDefault;
            return (bool)(connected?.Data ?? false);
        }
    }

    public Result<DateTime> LastConnectedTime {
        get {
            var dtResult =
                _xm4.GetDeviceProperty(
                    DeviceProperty_LastConnectedTime );

            return
                dtResult.IsSuccess
                ? ManagementDateTimeConverter
                    .ToDateTime( dtResult.Value.Data as string )
                    .ToUniversalTime()
                : Result.Fail(
                    "Can not find `LastConnectedTime` property. It is possible the device is still connected." );
        }
    }

    /// <summary>
    /// Find all bluetooth sub-devices related to headphones
    /// </summary>
    private static IEnumerable<PnpEntity> BluetoothDevices =>
        PnpEntity.FindByNameForExactClass(
            name: Headphones_PnpEntity_FriendlyNameGeneral,
            className: Bluetooth_PnpClassName );

    /// <summary>
    /// Try connect already paired bluetooth headphones.
    /// Application have to be runned under the Administrative rights
    /// </summary>
    public static void TryConnect()
    {
        foreach (var bt in BluetoothDevices) {
            bt.Disable();
            bt.Enable();
        }
    }

    /// <summary>
    /// Try disconnect already paired bluetooth headphones.
    /// Application have to be runned under the Administrative rights
    /// </summary>
    public static void TryDisconnect()
    {
        foreach (var bt in BluetoothDevices.Reverse()) { // TODO: Is reverse meaningful?
            bt.Disable();
            Thread.Sleep( 100 ); // TODO: Is this pause meaningful?
            bt.Disable();
        }
    }

    public const string HandsFree_PnpEntity_FriendlyNameGeneral
        = "W_-1000XM_ Hands-Free AG"; // Know how to get battery level
    public const string Headphones_PnpEntity_FriendlyNameGeneral
        = "W_-1000XM_"; // Know how to get headphones' state

    public const string Bluetooth_PnpClassName = "Bluetooth";

    public const string DeviceProperty_BatteryLevel
        = "{104EA319-6EE2-4701-BD47-8DDBF425BBE5} 2";
    public const string DeviceProperty_IsConnected
        = "{83DA6326-97A6-4088-9453-A1923F573B29} 15";

    public const string DEVPKEY_Bluetooth_LastConnectedTime
        = "DEVPKEY_Bluetooth_LastConnectedTime";
    public const string DeviceProperty_LastConnectedTime
        = "{2BD67D8B-8BEB-48D5-87E0-6CDA3428040A} 11";

    public const string DEVPKEY_Bluetooth_LastConnectedTime2
        = "{2BD67D8B-8BEB-48D5-87E0-6CDA3428040A} 5";
}