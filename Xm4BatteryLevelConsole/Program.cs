using WmiPnp.Xm4;

var xm4result = Xm4Entity.Create();
if ( xm4result.IsFailed ) return;

var xm4 = xm4result.Value;
Console.WriteLine( "Press any key to stop polling..." );
while ( !Console.KeyAvailable ) {
    var status =
        xm4.IsConnected ? $"{DateTime.Now:T}"
        : "Disconnected / Last Known";

    Console.Write( $"\r[{status}] Battery Level: {xm4.BatteryLevel}%                  " );

    Thread.Sleep( 1000 );
}

if ( !xm4.IsConnected ) {
    Console.WriteLine( $"Last connected time: ???" );
}

//PnpEntity
//    .ByFriendlyName(
//        Xm4Entity.PnpEntity_FriendlyName )
//    .IfSome( e => ProcessEntity( e ) );

//void ProcessEntity( Result<PnpEntity> xm4entity )
//{
//    var xm4 = xm4entity.Value;

//    Console.WriteLine( $"--> {xm4.Name}: {xm4.Description}" );
//    var pr = xm4.GetDeviceProperty( PnpEntity.DeviceProperty_BatteryLevel );

//    foreach ( var p in xm4.UpdateProperties() )
//        Console.WriteLine( $"{p.KeyName}: {p.Data}" );

//    Console.WriteLine();

//    while ( !Console.KeyAvailable ) {
//        var level =
//            pr
//            .Some( dp => {
//                xm4.UpdateProperty( dp );
//                return dp.Data;
//            } )
//            .None( () => "[x] Key not found" );

//        Console.Write( $"\rBattery Level: {level}%" );

//        Thread.Sleep( 1000 );
//    }

//    Console.WriteLine();
//}
