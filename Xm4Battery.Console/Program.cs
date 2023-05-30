using WmiPnp.Xm4;

var xm4result = Xm4Entity.Create();
if ( xm4result.IsFailed ) return;

Xm4Entity xm4 = xm4result.Value;
if (!xm4.IsConnected) {
    Console.WriteLine( $"Last connected time: {xm4.LastConnectedTime.Value}.\n" );

    xm4.TryConnect();
}

int sx = 0;
Console.WriteLine( "Press any key to stop polling..." );
while ( !Console.KeyAvailable ) {
    var status =
        xm4.IsConnected ? $"{DateTime.Now:T}"
        : "Disconnected / Last Known";

    var scr = xm4.IsConnected ? new string( ' ', 17 ).Insert( sx, ">" ) : string.Empty;
    sx = sx >= scr.Length - 1 ? 0 : sx + 1;

    Console.Write( $"\r[{status}] Battery Level: {xm4.BatteryLevel}% {scr}" );

    Thread.Sleep( 1000 );
}
