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
    Console.WriteLine( $"\nLast connected time: {xm4.LastConnectedTime}" );
}
