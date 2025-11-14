# Xm4Battery

Battery level of WH-1000XM4 headphones and other series models, based on the WMI wrapper for Plug-and-Play devices.

> WMI = Windows Management Interface.

The primary goal of the project is to get battery level of `WH-1000XM4` headphones. Perhaps `Xm4Battery` might also works with similar models of headphones such as WH-1000XM4, WF-1000XM4 or WH-1000XM3-5-6-etc.

![emoji_flash_bullet_battery_level_v23-5-2](https://user-images.githubusercontent.com/11328666/235766399-44585bee-0e8f-4d21-b96a-81b58b9e83d2.jpg)

- [Desktop Application](#desktop-application)
    - [User interface](#user-interface)
    - [Tray icon mods](#tray-icon-mods)
- [Xm4Poller](#xm4poller)
    - [Start and stop device polling](#start-and-stop-device-polling)
    - [Xm4State](#xm4state)
- [Xm4Entity](#xm4entity)
    - [Create XM4 instance](#create-xm4-instance)
    - [Is connected or not?](#is-connected-or-not)
    - [What was the last connected time?](#what-was-the-last-connected-time)
    - [Headphones battery level](#headphones-battery-level)
    - [Re/Connect already paired](#reconnect-already-paired)
- [PnpEntity](#pnpentity)
    - [How To find PNP-device?](#how-to-find-pnp-device)
    - [Get and update a specific property of a device](#get-and-update-a-specific-property-of-a-device)
    - [Enumerate all properties of device](#enumerate-all-properties-of-device)
    - [Enable or disable device](#enable-or-disable-device)
- [Device specific properties](#device-specific-properties)
- [XM4 related properties](#xm4-related-properties)
- [Windows Radio](#windows-radio)
- [References](#references)

## Desktop Application

This Windows Forms application runs as a tray icon with no main window.\
The ready-to-run version is available in the [Latest Release](https://github.com/nikvoronin/Xm4Battery/releases/latest) section.

__System requirements:__ Windows 10 x64, [.NET Desktop Runtime 10.0](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) LTS

ℹ️ Before starting the application, pair your headphones with your laptop.

| Headphones  | Win 10 | Win 11  |
| ----------- | ------ | ------- |
| WH-1000_XM6 | ?      | ?       |
| WH-1000_XM5 | ?      | ?       |
| WH-1000_XM4 | Yes    | Yes     |
| WH-1000_XM3 | Yes    | ?       |

### User interface

- __F__ - 100% fully charged
- __4..9__ - 40..90%
- __3__ yellow - 30%
- __2__ orange - 20%
- __!__ red - 10%
- __X__ - headphones disconnected.
- __%__ - headphones disconnected, the last known battery level was low (30% or lower).

When the headphones are disconnected, a tooltip displays their last known battery level and the date and time of their most recent connection.

🐭 `Right Mouse Button` opens a context menu:

- __Connect__ - tries connect already paired headphones. ⚠
- __Disconnect__ - tries disconnect headphones (not unpair, just disconnect). ⚠
- __Launch at Startup__ - toggles whether the application automatically starts when Windows boots up. When ✅enabled, the application will launch automatically upon system startup.
- __About__ - leads to this page.
- __Quit__ - closes and unloads application at all.

>⚠ __Connect / Disconnect__ items appear if the app is run as an administrator.\
>⚠* These functions may cause system artefacts or unusual behavior of Volume Control, Sound Mixer, Bluetooth Device Manager, etc.\
>⚠** Especially the Disconnect item. Connect is a law-abiding one.

### Tray icon mods

The real icon size is 20x20 pixels. It is automatically scaled by system depend on display scaling factor.

>The app icon is currently adjusted to 125% display scaling. Other scaling factors may lead to uglifying tray icon.

Icon text color and background are defined in the `CreateXmIcon` method:

```csharp
// icon background color
var iconBackgroundBrush =
    uiBatteryLevel switch {
        <= DisconnectedLevel => Brushes.Transparent,
        <= CriticalPowerLevel => Brushes.Red,
        <= LowPowerLevel => Brushes.Orange,
        <= WarningPowerLevel => Brushes.Yellow,
        _ => Brushes.White // 40..100(F)
    };

// icon text color
var iconTextBrush =
    uiBatteryLevel switch {
        <= DisconnectedLevel => Brushes.WhiteSmoke,
        <= CriticalPowerLevel => Brushes.White,
        //<= LowPowerLevel => Brushes.Magenta,
        //<= WarningLevel => Brushes.Cyan,
        _ => Brushes.Black
    };
```

Font of the notification icon text (battery level or headphones status):

```csharp
static readonly Font _notifyIconFont =
    new( "Segoe UI", 12.5f, FontStyle.Regular );
```

## Xm4Poller

Automatically updates status of headphones.

### Start and stop device polling

```csharp
var xm4result = Xm4Entity.Create();
if ( xm4result.IsFailed ) return 1;

Xm4Entity xm4 = xm4result.Value;

var statePoller = new Xm4Poller ( 
    xm4,
    ( previousState, newState ) => {
        // this handler is called when xm4 state changed:
        // connection status or/and battery charge level.
        // previousState <> newState - always unequal!
        UpdateUi_ForExample(newState);
    } );

statePoller.Start();

// starts main loop of window-less WinForms app
Application.Run();

// application was closed, quit
statePoller.Stop();
```

### Xm4State

```csharp
namespace WmiPnp.Xm4;

public record Xm4State
{
    public bool Connected   // true if connected, false - otherwise.
    public int BatteryLevel // battery charge level
```

## Xm4Entity

### Create XM4 instance

```csharp
var xm4result = Xm4Entity.CreateDefault();
if ( xm4result.IsFailed ) return; // headphones did not found at all

Xm4Entity _xm4 = xm4result.Value;
```

### Is connected or not?

```csharp
...
bool connected = _xm4.IsConnected;
```

### What was the last connected time?

We cannot determine the last connection time while the headphones are online and already connected. This property is only valid when the headphones are DISconnected.

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

### Headphones battery level

It retrieves the current battery level when the headphones are connected; otherwise, when they are disconnected, it returns the last known level.

```csharp
int level = _xm4.BatteryLevel;
```

### Re/Connect already paired

When headphones are used with multiple devices - such as a laptop, PC, or smartphone - you may need to reconnect them periodically. In this scenario, the headphones are already paired but currently disconnected.

For such cases, the `WmiPnp` module includes experimental methods: `Xm4Entity.TryConnect()` and the highly unstable `Xm4Entity.TryDisconnect()`. Both require the application to run with administrator privileges; otherwise, they are silently ignored.

If you're curious about completely turning off Bluetooth, see the section on [Windows Radio](#windows-radio).

## PnpEntity

First, we need to know either the `name` or the `device id` of the target device - or at least a partial match of its name.

- __ByFriendlyName__ - exact a friendly name.
- __ByDeviceId__ - exact a device id, like `{GUID} pid`.
- __FindByFriendlyName__ - a part of a friendly name. Returns a list of founded devices `IEnumerable<PnpEntity>` or empty list otherwise.
- __FindByNameForExactClass__ - same as `FindByFriendlyName` but with exact class name equality.
- __EntityOrNone__ - a `where` part of WQL request to retrieve exact a single device only.
- __EntitiesOrNone__ - a `where` part of WQL request to retrieve zero, one or several devices at once.

All of these methods return either an instance of `PnpEntity` or a `Result.Fail` if the specified device is not found.

### How To find PNP-device?

```csharp
Result<PnpEntity> result =
    PnpEntity.ByFriendlyName( "The Bluetooth Device #42" );

if ( result.IsSuccess ) { // device found
    PnpEntity btDevice = result.Value;
    ...
}
```

### Get and update a specific property of a device

```csharp
...
PnpEntity btDevice = result.Value;

while ( !Console.KeyAvailable ) {
    Result<DeviceProperty> propertyResult =
        btDevice.GetDeviceProperty(
            Xm4Entity.DeviceProperty_IsConnected );

    if ( propertyResult.IsSuccess ) {
        DeviceProperty dp = propertyResult.Value;
        bool connected = (bool)(dp.Data ?? false);

        Console.WriteLine(
            $"{btDevice.Name} is {(connected ? "connected" : "disconnected")}" );
    }

    // wait a little before the next attempt
    Thread.Sleep( TimeSpan.FromSeconds( 1 ) );
}
```

### Enumerate all properties of device

```csharp
...
PnpEntity btDevice = result.Value;

IEnumerable<DeviceProperty> properties = btDevice.GetProperties();

foreach( var p in properties ) {
    Console.WriteLine( $"{p.KeyName}: {p.Data}" );
    ...
}
```

### Enable or disable device

Some devices can be enabled or disabled.

```csharp
...
PnpEntity btDevice = result.Value;

btDevice.Disable();
btDevice.Enable();
```

## Device specific properties

> Key = {GUID} pid

<!-- omit in toc -->
### Battery level

- Key = `{104EA319-6EE2-4701-BD47-8DDBF425BBE5} 2`
- Type = 3 (Byte)

`Data` is in percents

<!-- omit in toc -->
### Is connected or not

- Key = `{83DA6326-97A6-4088-9453-A1923F573B29} 15`
- Type = 17 (Boolean)

Data = False → device is disconnected

<!-- omit in toc -->
### Last arrival date

- Key = `{83DA6326-97A6-4088-9453-A1923F573B29} 102`
- KeyName = DEVPKEY_Device_LastArrivalDate
- Type = 16 (FileTime)

Data = 20230131090906.098359+180 → 2023 Jan 31, 9:09:06 GMT+3

<!-- omit in toc -->
### Last removal date

Key = {83da6326-97a6-4088-9453-a1923f573b29} 103

## XM4 related properties

- `WH-1000XM4 Hands-Free AG` - exact name for PnpEntity to get a __BATTERY LEVEL__ only.
- `WH-1000XM4` - exact name for PnpEntity to get a __STATE__ of the xm4.

> The app actually uses naming templates such as `W_-1000XM_` to abstract and match various headphone models (e.g., WH-1000XM3, WF-1000XM4, etc.).

<!-- omit in toc -->
### DEVPKEY_Device_DevNodeStatus

> Instead of using these bit flags, we can use the [Is Connected](#is-connected-or-not) property to retrieve the connection status of the XM4.

- Key = `{4340A6C5-93FA-4706-972C-7B648008A5A7} 2`
- KeyName = DEVPKEY_Device_DevNodeStatus
- Type = 7 (Uint32)

- Connected = 25190410 (fall bit#25): value & 0x20000 == 0
- Disconnected = 58744842 (set bit#25): value & 0x20000 == 0x20000

<!-- omit in toc -->
### DEVPKEY_Bluetooth_LastConnectedTime

This is the only property that provides the last connection date and time of the headphones, and it is available only when the headphones are DISconnected.

- Key = `{2BD67D8B-8BEB-48D5-87E0-6CDA3428040A} 11`
- KeyName = DEVPKEY_Bluetooth_LastConnectedTime
- Type = 16 (FileTime)

For ex.: Data = 20230131090906.098359+180 → 2023 Jan 31, 9:09:06, GMT+3

<!-- omit in toc -->
### ?Last connected time

Contains the same data as the [DEVPKEY_Bluetooth_LastConnectedTime](#devpkey_bluetooth_lastconnectedtime) property and behaves identically.

- Key = `{2BD67D8B-8BEB-48D5-87E0-6CDA3428040A} 5`
- Type = 16 (FileTime)

## Windows Radio

<!-- omit in toc -->
### Preparation

It is possible to use UWP APIs from a desktop application by setting the `TargetFramework` in your `YourProject.csproj` file to a Windows-specific .NET version, such as `netX.x-windows10.0.xxxxx.x`.

For example:

```xml
<PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net6.0-windows10.0.17763.0</TargetFramework>
    ...
```

<!-- omit in toc -->
### Switch system bluetooth on and off

With this setup, we can now use the `Windows.Devices.Radios` namespace:

```csharp
using Windows.Devices.Radios;
```

>⚠ Be aware: this can completely turn off the system Bluetooth radio - not just enable or disable it.\
>⚠ Use at your own risk!

```csharp
public static async Task OsEnableBluetooth() =>
    InternalBluetoothState( enable: true );

public static async Task OsDisableBluetooth() =>
    InternalBluetoothState( enable: false );

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
        : RadioState.Off );
}
```

> We can also use `Windows.Devices.Bluetooth` namespace or even `Windows.Devices.***` for other peripheral devices.

## References

- [Sony Headphones Client](https://github.com/Plutoberth/SonyHeadphonesClient/) `public archive`. This project features a PC alternative for the mobile-only Sony Headphones app. `C++` `Windows` `Linux` `macOS`
- [Enumerating windows device](https://www.codeproject.com/articles/14412/enumerating-windows-device). Enumerating the device using the SetupDi* API provided with WinXP. CodeProject // 17 Jun 2006
- [How to get the details for each enumerated device?](https://social.msdn.microsoft.com/Forums/en-US/65086709-cee8-4efa-a794-b32979abb0ea/how-to-get-the-details-for-each-enumerated-device?forum=vbgeneral) MSDN, Archived Forums 421-440. `Visual Basic`
- [Query battery level for WH-1000XM4 wireless headphones](https://gist.github.com/nikvoronin/e8fc8a1631dd0e851f1ab821d0e3cf01). GitHub gist. `PowerShell`
- [Enable/disable already paired bluetooth devices](https://stackoverflow.com/questions/62502414/how-to-connect-to-a-paired-audio-bluetooth-device-using-windows-uwp-api/71539568#71539568). StackOverflow. How to connect to a paired audio Bluetooth device using Windows UWP API? `PowerShell`
- [Talking to robots (or other devices) using Bluetooth from a Windows Runtime app](https://blogs.windows.com/windowsdeveloper/2014/05/07/talking-to-robots-or-other-devices-using-bluetooth-from-a-windows-runtime-app/). `Windows.Devices.Bluetooth.Rfcomm` namespace // May 7, 2014
- [My Bluetooth headset can now be switched on and off from the command line](https://superuser.com/a/1815325). `PowerShell`
