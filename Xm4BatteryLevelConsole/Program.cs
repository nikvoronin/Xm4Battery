using LanguageExt;
using WmiPnp;
using WmiPnp.Xm4;

Xm4Entity
    .Create()
    .IfSome( xm4 => {
        Console.WriteLine( "Press any key to stop polling..." );
        while ( !Console.KeyAvailable ) {
            Console.Write( $"\r[{DateTime.Now:T}] Battery Level: {xm4.BatteryLevel}%" );

            Thread.Sleep( 1000 );
        }
    } );

//PnpEntity
//    .ByFriendlyName(
//        Xm4Entity.PnpEntity_FriendlyName )
//    .IfSome( e => ProcessEntity( e ) );

void ProcessEntity( Some<PnpEntity> xm4entity )
{
    var xm4 = xm4entity.Value;

    Console.WriteLine( $"--> {xm4.Name}: {xm4.Description}" );
    var pr = xm4.GetDeviceProperty( PnpEntity.DeviceProperty_BatteryLevel );

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
