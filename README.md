# WMI for Plug-and-Play Devices

> WMI = Windows Management Interface.

The primary goal is to get battery level of WH-1000XM4 headphones.

- [How To Find PNP Device](#how-to-find-pnp-device)
  - [PnpEntity](#pnpentity)
- [Device Properties](#device-properties)
  - [Get / Update Specific Property](#get--update-specific-property)
  - [Get Em All](#get-em-all)
- [Specific Device Properties](#specific-device-properties)
  - [Battery Level](#battery-level)
  - [Is Connected](#is-connected)
  - [Last Arrival Date](#last-arrival-date)
  - [Last Removal Date](#last-removal-date)
- [XM4 Specific Properties](#xm4-specific-properties)
  - [DEVPKEY\_Device\_DevNodeStatus](#devpkey_device_devnodestatus)
  - [DEVPKEY\_Bluetooth\_LastConnectedTime](#devpkey_bluetooth_lastconnectedtime)
  - [?Last Connected Time](#last-connected-time)
- [Biblio](#biblio)

## How To Find PNP Device

First we should know the `name` or `device id` of the device we фку working with or at least a part of the device name.

- ByFriendlyName ( exact a friendly name )
- ByDeviceId ( exact a device id, like {GUID} pid )
- LikeFriendlyName ( a part of a friendly name ) - returns a list of found devices: `IEnumerable<PnpEntity>` or empty list.

All of methods produce instances of `PnpEntity` or `Result.Fail` if the given device was not found.

### PnpEntity

```csharp
Result<PnpEntity> result =
    PnpEntity.ByFriendlyName( "The Bluetooth Device #42" );

if ( result.IsSuccess ) {
    PnpEntity btDevice = result.Value;
    ...
}
```

## Device Properties

### Get / Update Specific Property

```csharp
...
PnpEntity btDevice = result.Value;

Result<DeviceProperty> propertyResult =
    btDevice.GetDeviceProperty( PnpEntity.DeviceProperty_IsConnected );

if ( propertyResult.IsSuccess ) {
    DeviceProperty dp = propertyResult.Value;

    while ( !Console.KeyAvailable ) {
        bool connected = (bool)(dp.Data ?? false);

        Console.WriteLine(
            $"{btDevice.Name} is {(connected ? "connected" : "disconnected")}" );

        btDevice.UpdateProperty( dp );
    }    
}
```

### Get Em All

```csharp
...
PnpEntity btDevice = result.Value;

IEnumerable<DeviceProperty> properties = btDevice.UpdateProperties();

foreach( var p in properties ) {
    Console.WriteLine( $"{p.KeyName}: {p.Data}" );
    ...
}
```

The same but with a cached list of the last update result

```csharp
_ = btDevice.UpdateProperties();

foreach( var p in btDevice.Properties ) {
    ...
```

## Specific Device Properties

> Key = {GUID} pid

### Battery Level

- Key = `{104EA319-6EE2-4701-BD47-8DDBF425BBE5} 2`
- Type = 3 (Byte)

`Data` is in percents

### Is Connected

- Key = `{83DA6326-97A6-4088-9453-A1923F573B29} 15`
- Type = 17 (Boolean)

Data = False → device is disconnected

### Last Arrival Date

- Key = `{83DA6326-97A6-4088-9453-A1923F573B29} 102`
- KeyName = DEVPKEY_Device_LastArrivalDate
- Type = 16 (FileTime)

Data = 20230131090906.098359+180 → 2023 Jan 31, 9:09:06 GMT+3

### Last Removal Date

Key = {83da6326-97a6-4088-9453-a1923f573b29} 103

## XM4 Specific Properties

- `WH-1000XM4 Hands-Free AG` - exact name for PnpEntity to get a **BATTERY LEVEL** --only-- of the xm4.
- `WH-1000XM4` - exact name for PnpEntity to get a **STATE** of the xm4.

### DEVPKEY_Device_DevNodeStatus

> Instead of this bit flags, we can use [Is Connected](#is-connected) property to retrieve a connection status of xm4.

- Key = `{4340A6C5-93FA-4706-972C-7B648008A5A7} 2`
- KeyName = DEVPKEY_Device_DevNodeStatus
- Type = 7 (Uint32)

- Connected = 25190410 (fall bit#25): value & 0x20000 == 0
- Disconnected = 58744842 (set bit#25): value & 0x20000 == 0x20000

### DEVPKEY_Bluetooth_LastConnectedTime

This is only property to retrieve a last connection date-time of xm4. This property does not present when headphones are connected.

- Key = `{2BD67D8B-8BEB-48D5-87E0-6CDA3428040A} 11`
- KeyName = DEVPKEY_Bluetooth_LastConnectedTime
- Type = 16 (FileTime)

For ex.: Data = 20230131090906.098359+180 → 2023 Jan 31, 9:09:06, GMT+3

### ?Last Connected Time

Contains the same data as the [DEVPKEY_Bluetooth_LastConnectedTime](#devpkey_bluetooth_lastconnectedtime) property. Same behavior.

- Key = `{2BD67D8B-8BEB-48D5-87E0-6CDA3428040A} 5`
- Type = 16 (FileTime)

## Biblio

- [Enumerating windows device](https://www.codeproject.com/articles/14412/enumerating-windows-device). Enumerating the device using the SetupDi* API provided with WinXP. CodeProject // 17 Jun 2006
- [How to get the details for each enumerated device?](https://social.msdn.microsoft.com/Forums/en-US/65086709-cee8-4efa-a794-b32979abb0ea/how-to-get-the-details-for-each-enumerated-device?forum=vbgeneral) MSDN, Archived Forums 421-440, Visual Basic.
- [Query battery level for WH-1000XM4 wireless headphones](https://gist.github.com/nikvoronin/e8fc8a1631dd0e851f1ab821d0e3cf01) by PowerShell script.
