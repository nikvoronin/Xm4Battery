# WMI for Plug-and-Play Devices

> WMI = Windows Management Interface.

The primary goal of the project is to get battery level of `WH-1000XM4` headphones. Perhaps the app might also work with similar models of headphones such as WH-1000XM3, WF-1000XM3 or WF-1000XM4.

![emoji_flash_bullet_battery_level_v23-5-2](https://user-images.githubusercontent.com/11328666/235766399-44585bee-0e8f-4d21-b96a-81b58b9e83d2.jpg)

- [Xm4Battery Application](#xm4battery-application)
  - [User Interface](#user-interface)
  - [Tray Icon Mods](#tray-icon-mods)
- [Xm4Poller](#xm4poller)
  - [Start-Stop Polling](#start-stop-polling)
  - [Connection Changed](#connection-changed)
  - [Battery Level Changed](#battery-level-changed)
- [Xm4Entity](#xm4entity)
  - [Create XM4 Instance](#create-xm4-instance)
  - [Is Connected or Not?](#is-connected-or-not)
  - [What Is The Last Connected Time?](#what-is-the-last-connected-time)
  - [Headphones Battery Level](#headphones-battery-level)
- [PnpEntity](#pnpentity)
  - [How To Find PNP Device?](#how-to-find-pnp-device)
  - [Get / Update Specific Device Property](#get--update-specific-device-property)
  - [Enumerate Device Properties](#enumerate-device-properties)
- [Device Specific Properties](#device-specific-properties)
- [XM4 Related Properties](#xm4-related-properties)
- [Windows Radio](#windows-radio)
- [Links](#links)

## Xm4Battery Application

The Windows Forms, window-less and trayicon application. Ready to run app is available under the [Latest Release](https://github.com/nikvoronin/WmiPnp/releases/latest) section.

System requirements: Windows 10 x64, .NET 6.0.

### User Interface

- **F** - 100% or fully charged
- **9..4** - 90..40%
- **3** yellow - 30%
- **2** orange - 20%
- **1** red - 10%
- **X** - headphones disconnected, gray background. Tooltip shows the last known battery level and the last connected date/time.

`Right Mouse Button` opens a context menu:

- Connect - tries connect already paired headphones. ⚠
- Disconnect - tries disconnect headphones (not unpair, just disconnect). ⚠
- About - leads to this page.
- Quit - closes and unloads application at all.

>⚠ **Connect / Disconnect** items appear if the app is run as an administrator.\
>⚠ These functions may cause system artefacts or unusual behavior of Volume Control, Sound Mixer, Bluetooth Device Manager, etc.\
>⚠ Especially the Disconnect item. Connect is a law-abiding one.

### Tray Icon Mods

Background color is defined in the `CreateIconForLevel` method:

```csharp
var brush =
    level switch {
        > 0 and <= 10 => Brushes.Red,
        > 0 and <= 20 => Brushes.Orange,
        > 0 and <= 30 => Brushes.Yellow,
        <= 0 => Brushes.Gray,
        _ => Brushes.White
    };
```

Font for notification icon text (battery level or headphones status):

```csharp
static readonly Font _notifyIconFont
    = new ( "Segoe UI", 16, FontStyle.Regular );
```

## Xm4Poller

Automatically updates status of headphones.

### Start-Stop Polling

```csharp
var xm4result = Xm4Entity.Create();
if ( xm4result.IsFailed ) return 1;

Xm4Entity xm4 = xm4result.Value;

Xm4Poller statePoll = new ( xm4 );
statePoll.ConnectionChanged += Xm4state_ConnectionChanged;
statePoll.BatteryLevelChanged += Xm4state_BatteryLevelChanged;
statePoll.Start();

Application.Run(); // run WinForms app, for ex

// application was closed, quit
statePoll.Stop();
```

### Connection Changed

```csharp
private static void Xm4state_ConnectionChanged( object? sender, bool connected )
{
    var xm4 = sender as Xm4Entity;
    ...
```

### Battery Level Changed

```csharp
private static void Xm4state_BatteryLevelChanged( object? sender, int batteryLevel )
{
    var xm4 = sender as Xm4Entity;
    ...
```

## Xm4Entity

### Create XM4 Instance

```csharp
var xm4result = Xm4Entity.Create();
if ( xm4result.IsFailed ) return; // headphones did not found at all

Xm4Entity _xm4 = xm4result.Value;
```

### Is Connected or Not?

```csharp
...
bool connected = _xm4.IsConnected;
```

### What Is The Last Connected Time?

We don't know how to get the last connected time if headphones is online and already connected. This property is valid only if headphones are DISconnected.

```csharp
Result<DateTime> dt = _xm4.LastConnectedTime;
```

```csharp
bool disconnected = !_xm4.IsConnected;
if ( disconnected )
    Console.WriteLine( $"Last connected time: {_xm4.LastConnectedTime.Value}.\n" );
else
    var it_is_true = _xm4.LastConnectedTime.IsFailed; // can not get the last connected time
```

### Headphones Battery Level

It can get the actual battery level if headphones are connected. Otherwise, headphones are DISconnected, it returns the last known level.

```csharp
int level = _xm4.BatteryLevel;
```

## PnpEntity

First, we should know the `name` or `device id` of the device we are working with or at least a part of the device name.

- ByFriendlyName ( exact a friendly name )
- ByDeviceId ( exact a device id, like `{GUID} pid` )
- LikeFriendlyName ( a part of a friendly name ) - returns a list of founded devices `IEnumerable<PnpEntity>` or empty list otherwise.

All of methods produce instances of `PnpEntity` or `Result.Fail` if the given device was not found.

### How To Find PNP Device?

```csharp
Result<PnpEntity> result =
    PnpEntity.ByFriendlyName( "The Bluetooth Device #42" );

if ( result.IsSuccess ) { // device found
    PnpEntity btDevice = result.Value;
    ...
}
```

### Get / Update Specific Device Property

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

### Enumerate Device Properties

```csharp
...
PnpEntity btDevice = result.Value;

IEnumerable<DeviceProperty> properties = btDevice.UpdateProperties();

foreach( var p in properties ) {
    Console.WriteLine( $"{p.KeyName}: {p.Data}" );
    ...
}
```

Same thing but with a cached list of the last updated properties

```csharp
_ = btDevice.UpdateProperties();

foreach( var p in btDevice.Properties ) {
    ...
```

## Device Specific Properties

> Key = {GUID} pid

<!-- omit in toc -->
### Battery Level

- Key = `{104EA319-6EE2-4701-BD47-8DDBF425BBE5} 2`
- Type = 3 (Byte)

`Data` is in percents

<!-- omit in toc -->
### Is Connected

- Key = `{83DA6326-97A6-4088-9453-A1923F573B29} 15`
- Type = 17 (Boolean)

Data = False → device is disconnected

<!-- omit in toc -->
### Last Arrival Date

- Key = `{83DA6326-97A6-4088-9453-A1923F573B29} 102`
- KeyName = DEVPKEY_Device_LastArrivalDate
- Type = 16 (FileTime)

Data = 20230131090906.098359+180 → 2023 Jan 31, 9:09:06 GMT+3

<!-- omit in toc -->
### Last Removal Date

Key = {83da6326-97a6-4088-9453-a1923f573b29} 103

## XM4 Related Properties

- `WH-1000XM4 Hands-Free AG` - exact name for PnpEntity to get a **BATTERY LEVEL** only.
- `WH-1000XM4` - exact name for PnpEntity to get a **STATE** of the xm4.

> Actually, the app utilize templates like `W_-1000XM_` to generalize model of headphones (WH-1000XM3, WF-1000XM4, etc.)

<!-- omit in toc -->
### DEVPKEY_Device_DevNodeStatus

> Instead of this bit flags, we can use [Is Connected](#is-connected) property to retrieve a connection status of xm4.

- Key = `{4340A6C5-93FA-4706-972C-7B648008A5A7} 2`
- KeyName = DEVPKEY_Device_DevNodeStatus
- Type = 7 (Uint32)

- Connected = 25190410 (fall bit#25): value & 0x20000 == 0
- Disconnected = 58744842 (set bit#25): value & 0x20000 == 0x20000

<!-- omit in toc -->
### DEVPKEY_Bluetooth_LastConnectedTime

This is only property to retrieve the last connection date-time of headphones. This property appears only when headphones are DISconnected.

- Key = `{2BD67D8B-8BEB-48D5-87E0-6CDA3428040A} 11`
- KeyName = DEVPKEY_Bluetooth_LastConnectedTime
- Type = 16 (FileTime)

For ex.: Data = 20230131090906.098359+180 → 2023 Jan 31, 9:09:06, GMT+3

<!-- omit in toc -->
### ?Last Connected Time

Contains the same data as the [DEVPKEY_Bluetooth_LastConnectedTime](#devpkey_bluetooth_lastconnectedtime) property. Same behavior.

- Key = `{2BD67D8B-8BEB-48D5-87E0-6CDA3428040A} 5`
- Type = 16 (FileTime)

## Windows Radio

<!-- omit in toc -->
### Preparation

There is a way to use UWP functions from desktop application. Just setup a `TargetFramework` in `YourProject.csproj` to use specific version of dotNet-framework-windows-only like: `netX.x-windows10.0.xxxxx.x`. For example:

```xml
<PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net6.0-windows10.0.17763.0</TargetFramework>
    ...
```

<!-- omit in toc -->
### Switch system bluetooth on and off

Now we can use `Windows.Devices.Radios` namespace:

```csharp
using Windows.Devices.Radios;
```

>⚠ Be aware, this one could switch off system bluetooth radio **at all** (not only enable or disable).\
>⚠ Use this on your own risk!

```csharp
public static async Task OsEnableBluetooth()
    => InternalBluetoothState( enable: true );

public static async Task OsDisableBluetooth()
    => InternalBluetoothState( enable: false );

private async Task InternalBluetoothState( bool enable )
{
    var result = await Radio.RequestAccessAsync();
    if (result != RadioAccessStatus.Allowed) return;

    var bluetooth =
        (await Radio.GetRadiosAsync())
        .FirstOrDefault(
            radio => radio.Kind == RadioKind.Bluetooth );

    await bluetooth?.SetStateAsync(
        enable ? RadioState.On
        : RadioState.Off
        );
}
```

<!-- omit in toc -->
### Notes

We can also use `Windows.Devices.Bluetooth` namespace or even `Windows.Devices.***` for other peripheral devices.

## Links

- [Enumerating windows device](https://www.codeproject.com/articles/14412/enumerating-windows-device). Enumerating the device using the SetupDi* API provided with WinXP. CodeProject // 17 Jun 2006
- Visual Basic: [How to get the details for each enumerated device?](https://social.msdn.microsoft.com/Forums/en-US/65086709-cee8-4efa-a794-b32979abb0ea/how-to-get-the-details-for-each-enumerated-device?forum=vbgeneral) MSDN, Archived Forums 421-440.
- PowerShell: [Query battery level for WH-1000XM4 wireless headphones](https://gist.github.com/nikvoronin/e8fc8a1631dd0e851f1ab821d0e3cf01). GitHub gist.
- PowerShell: [Enable/disable already paired bluetooth devices](https://stackoverflow.com/questions/62502414/how-to-connect-to-a-paired-audio-bluetooth-device-using-windows-uwp-api/71539568#71539568). StackOverflow. How to connect to a paired audio Bluetooth device using Windows UWP API?
