using WmiPnp;

const string BluetoothDevice_FriendlyName = "WH-1000XM4 Hands-Free AG";
const string BluetoothDevice_BatteryLevelKey = "{104EA319-6EE2-4701-BD47-8DDBF425BBE5} 2";

var entity =
    PnpEntity
    .ByFriendlyName( 
        BluetoothDevice_FriendlyName );

entity
.IfSome( e => {
    var xm4 =
        PnpEntity
        .ByDeviceId( e.DeviceId! )
        .IfSome( xm4 => {
            Console.WriteLine( $"--> {xm4?.Name}: {xm4?.Description}" );
            var pr = xm4.GetDeviceProperty( BluetoothDevice_BatteryLevelKey );

            Console.WriteLine(
                pr
                .Some( x => x.Data )
                .None( () => "[x] Key not found" ) );
        } );
} );

