using WmiPnp;

const string BluetoothDevice_FriendlyName = "WH-1000XM4";

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
