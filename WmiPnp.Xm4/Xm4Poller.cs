namespace WmiPnp.Xm4
{
    public class Xm4Poller : IDisposable
    {
        // TODO: create options with public access
        private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds( 1 );

        private readonly Xm4Entity _xm4;
        private CancellationTokenSource? _cts = null;

        public event EventHandler<bool>? ConnectionChanged;
        public event EventHandler<int>? BatteryLevelChanged;

        public Xm4Poller( Xm4Entity xm4 )
        {
            _xm4 = xm4 ?? throw new ArgumentNullException( nameof( xm4 ) );
        }

        public void Dispose() => Stop();

        public void Start()
        {
            if ( _cts is not null ) return;

            _cts = new();
            Thread thread = new( ThreadWorker );
            thread.Start( _cts.Token );
        }

        private void ThreadWorker( object? o )
        {
            CancellationToken token =
                (CancellationToken)( o ?? CancellationToken.None );

            int batteryLevel = -1;
            bool connection = false;
            while ( !token.IsCancellationRequested ) {
                var currentLevel = _xm4.BatteryLevel;

                var batteryLevelChanged =
                    batteryLevel != currentLevel;
                if ( batteryLevelChanged )
                    OnBatteryLevelChanged( currentLevel );

                batteryLevel = currentLevel;

                if ( token.IsCancellationRequested ) break;

                var currentConnection = _xm4.IsConnected;

                var connectionChanged =
                    connection != currentConnection;
                if ( connectionChanged )
                    OnConnectionChanged( currentConnection );

                connection = currentConnection;

                Thread.Sleep( PollInterval );
            }
        }

        public void Stop()
        {
            _cts?.Cancel();
        }

        private void OnConnectionChanged( bool connected )
        {
            var eh = ConnectionChanged;
            eh?.Invoke( _xm4, connected );
        }

        private void OnBatteryLevelChanged( int level )
        {
            var eh = BatteryLevelChanged;
            eh?.Invoke( _xm4, level );
        }

    }
}
