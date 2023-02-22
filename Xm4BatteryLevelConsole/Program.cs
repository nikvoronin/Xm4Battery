using LanguageExt;
using WmiPnp;

const string BluetoothDevice_FriendlyName = "WH-1000XM4 Hands-Free AG";
const string BluetoothDevice_BatteryLevelKey = "{104EA319-6EE2-4701-BD47-8DDBF425BBE5} 2";

PnpEntity
    .ByFriendlyName(
        BluetoothDevice_FriendlyName )
    .IfSome( e => ProcessEntity( e ) );

void ProcessEntity( Some<PnpEntity> xm4entity )
{
    var xm4 = xm4entity.Value;

    Console.WriteLine( $"--> {xm4.Name}: {xm4.Description}" );
    var pr = xm4.GetDeviceProperty( BluetoothDevice_BatteryLevelKey );

    foreach ( var p in xm4.UpdateProperties() )
        Console.WriteLine( $"{p.KeyName}: {p.Data}" );

    Console.WriteLine();

    while ( !Console.KeyAvailable ) {
        var level =
            pr
            .Some( dp => {
                xm4.UpdateProperty( dp );
                return dp.Data;
            } )
            .None( () => "[x] Key not found" );

        Console.Write( $"\rBattery Level: {level}%" );

        Thread.Sleep( 1000 );
    }

    Console.WriteLine();
}
