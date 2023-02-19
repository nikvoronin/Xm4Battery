using WmiPnp;

const string BluetoothDevice_FriendlyName = "WH-1000XM4";
const string BluetoothDevice_BatteryLevelKey = "{104EA319-6EE2-4701-BD47-8DDBF425BBE5} 2";

var entities =
    PnpEntity
    .ByFriendlyName( 
        BluetoothDevice_FriendlyName );

PnpEntity? e = null;
foreach ( var entity in entities ) {
    e ??= entity;
    Console.WriteLine( entity.Name );
}

if ( e is null ) return;

var xm4 =
    PnpEntity
    .ByDeviceId( e.DeviceId! )
    .Some( x => x )
    .None( () => null! );

Console.WriteLine( $"--> {xm4?.Name}: {xm4?.Description}");

var pr = xm4.GetDeviceProperty( BluetoothDevice_BatteryLevelKey );
Console.WriteLine(
    pr
    .Some( x => x.Data)
    .None( () => "[x] Key not fiound") );