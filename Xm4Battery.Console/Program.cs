using System.Security.Principal;
using WmiPnp.Xm4;

var xm4result = Xm4Entity.Create();
if (xm4result.IsFailed) return;

Xm4Entity xm4 = xm4result.Value;
if (!xm4.IsConnected) {
    Console.WriteLine( $"Last connected time: {xm4.LastConnectedTime.Value}.\n" );

    var administrator =
        new WindowsPrincipal( WindowsIdentity.GetCurrent() )
        .IsInRole( WindowsBuiltInRole.Administrator );

    if (administrator) {
        Console.WriteLine( "Admin here: re/connecting headphones..." );

        Xm4Entity.TryConnect();
    }
}

var wave = new string( ' ', 18 );
Console.WriteLine( "Press any key to stop polling..." );
while (!Console.KeyAvailable) {
    var status =
        xm4.IsConnected ? $"{DateTime.Now:T}"
        : "Disconnected / Last Known";

    wave =
        xm4.IsConnected
        ? (Random.Shared.Next( 0, 2 ) == 0 ? ">" : " ") + wave[..^1]
        : new( ' ', 18 );

    Console.Write( $"\r[{status}] Battery Level: {xm4.BatteryLevel}%  {wave}" );

    Thread.Sleep( TimeSpan.FromSeconds( 1 ) );
}
