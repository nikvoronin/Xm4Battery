# WMI for Plug-and-Play Devices

> WMI = Windows Management Interface.

The primary goal of the project is to get battery level of `WH-1000XM4` headphones. Perhaps `Xm4Battery` might also works with similar models of headphones such as WH-1000XM3, WF-1000XM3 or WF-1000XM4.

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
  - [Re/Connect Already Paired](#reconnect-already-paired)
- [PnpEntity](#pnpentity)
  - [How To Find PNP Device?](#how-to-find-pnp-device)
  - [Get / Update Specific Device Property](#get--update-specific-device-property)
  - [Enumerate Device Properties](#enumerate-device-properties)
  - [Enable-Disable Device](#enable-disable-device)
- [Device Specific Properties](#device-specific-properties)
- [XM4 Related Properties](#xm4-related-properties)
- [Windows Radio](#windows-radio)
- [References](#references)

## Xm4Battery Application

The Windows Forms, trayiconed and window-less application at once.\
Ready to run app is available under the [Latest Release](https://github.com/nikvoronin/WmiPnp/releases/latest) section.

__System requirements:__ Windows 10 x64, [.NET Desktop Runtime 8.0](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)

| Headphones  | Win 10 | Win 11  |
| ----------- | ------ | ------- |
| WH-1000_XM4 | Yes    | Yes?    |
| WH-1000_XM3 | Yes    | Unknown |

### User Interface

- __F__ - 100% fully charged
- __4..9__ - 40..90%
- __3__ yellow - 30%
- __2__ orange - 20%
- __!__ red - 10%
- __X__ - headphones disconnected, transparent background. A tooltip displays the last known battery level and the last known date/time of the headphone connection.

`Right Mouse Button` opens a context menu:

- Connect - tries connect already paired headphones. ⚠
- Disconnect - tries disconnect headphones (not unpair, just disconnect). ⚠
- About - leads to this page.
- Quit - closes and unloads application at all.

>⚠ __Connect / Disconnect__ items appear if the app is run as an administrator.\
>⚠ These functions may cause system artefacts or unusual behavior of Volume Control, Sound Mixer, Bluetooth Device Manager, etc.\
>⚠ Especially the Disconnect item. Connect is a law-abiding one.

### Tray Icon Mods

The real icon size is 256x256 pixels. It is automatically scaled by system depend on display scaling factor.

>The app icon is currently adjusted to 125% display scale. Other scale factors may lead to uglifying tray icon.

Icon text color and background are defined in the `CreateIconForLevel` method:

```csharp
// icon background color
var iconBackgroundBrush =
    level switch {
        <= DisconnectedLevel => Brushes.Transparent,
        <= 10 => Brushes.Red,
        <= 20 => Brushes.Orange,
        <= 30 => Brushes.Yellow,
        _ => Brushes.White // 40..100(F)
    };

// icon text color
var iconTextBrush =
    level switch {
        <= DisconnectedLevel => Brushes.WhiteSmoke,
        //<= 10 => Brushes.Magenta,
        //<= 20 => Brushes.Cyan,
        _ => Brushes.Black
    };
```

Font of the notification icon text (battery level or headphones status):

```csharp
static readonly Font _notifyIconFont
    = new ( "Segoe UI", 124, FontStyle.Regular );
```

## Xm4Poller

Automatically updates status of headphones.

### Start-Stop Polling

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

public record  Xm4State
{
    public bool Connected   // true if connected, false - otherwise.
    public int BatteryLevel // battery charge level
```

## Xm4Entity

### Create XM4 Instance

```csharp
var xm4result = Xm4Entity.CreateDefault();
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

### Re/Connect Already Paired

When headphones are used with multiple sources (laptop, pc, smartphone, etc) you have to reconnect headphones from time to time. So headphones are already paired but disconnected. In this case `WmiPnp` has experimental `Xm4Entity.TryConnect()` and very unstable `Xm4Entity.TryDisconnect()`. Both want the application run as administrator. Otherwise these functions are ignored.

If you are curious to find out about turn off bluetooth at all, see topic about [Windows Radio](#windows-radio).

## PnpEntity

First, we should know a `name` or `device id` of the device we are working with or at least a part of the device name.

- ByFriendlyName - exact a friendly name.
- ByDeviceId - exact a device id, like `{GUID} pid`.
- FindByFriendlyName - a part of a friendly name. Returns a list of founded devices `IEnumerable<PnpEntity>` or empty list otherwise.
- FindByNameForExactClass - same as `FindByFriendlyName` but with exact class name equality.
- EntityOrNone - a `where` part of WQL request to retrieve exact a single device only.
- EntitiesOrNone - a `where` part of WQL request to retrieve zero, one or several devices at once.

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
    btDevice.GetDeviceProperty(
        Xm4Entity.DeviceProperty_IsConnected );

if ( propertyResult.IsSuccess ) {
    DeviceProperty dp = propertyResult.Value;

    while ( !Console.KeyAvailable ) {
        bool updated = btDevice.TryGetDeviceProperty( dp.Key, out dp );
        if (updated) {
            bool connected = (bool)(dp.Data ?? false);

            Console.WriteLine(
                $"{btDevice.Name} is {(connected ? "connected" : "disconnected")}" );
        }

        // wait a little before the next attempt
        Thread.Sleep( TimeSpan.FromSeconds( 1 ) );
    }
}
```

### Enumerate Device Properties

```csharp
...
PnpEntity btDevice = result.Value;

IEnumerable<DeviceProperty> properties = btDevice.GetProperties();

foreach( var p in properties ) {
    Console.WriteLine( $"{p.KeyName}: {p.Data}" );
    ...
}
```

### Enable-Disable Device

Some devices could be enabled or disabled.

```csharp
...
PnpEntity btDevice = result.Value;

btDevice.Disable();
btDevice.Enable();
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

- `WH-1000XM4 Hands-Free AG` - exact name for PnpEntity to get a __BATTERY LEVEL__ only.
- `WH-1000XM4` - exact name for PnpEntity to get a __STATE__ of the xm4.

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

>⚠ Be aware, this one could switch off system bluetooth radio __at all__ (not only enable or disable).\
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

- [Enumerating windows device](https://www.codeproject.com/articles/14412/enumerating-windows-device). Enumerating the device using the SetupDi* API provided with WinXP. CodeProject // 17 Jun 2006
- [How to get the details for each enumerated device?](https://social.msdn.microsoft.com/Forums/en-US/65086709-cee8-4efa-a794-b32979abb0ea/how-to-get-the-details-for-each-enumerated-device?forum=vbgeneral) MSDN, Archived Forums 421-440. `Visual Basic`
- [Query battery level for WH-1000XM4 wireless headphones](https://gist.github.com/nikvoronin/e8fc8a1631dd0e851f1ab821d0e3cf01). GitHub gist. `PowerShell`
- [Enable/disable already paired bluetooth devices](https://stackoverflow.com/questions/62502414/how-to-connect-to-a-paired-audio-bluetooth-device-using-windows-uwp-api/71539568#71539568). StackOverflow. How to connect to a paired audio Bluetooth device using Windows UWP API? `PowerShell`
- [Talking to robots (or other devices) using Bluetooth from a Windows Runtime app](https://blogs.windows.com/windowsdeveloper/2014/05/07/talking-to-robots-or-other-devices-using-bluetooth-from-a-windows-runtime-app/). `Windows.Devices.Bluetooth.Rfcomm` namespace // May 7, 2014
- [My Bluetooth headset can now be switched on and off from the command line](https://superuser.com/a/1815325). `PowerShell`
