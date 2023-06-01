namespace WmiPnp.Xm4
{
    public class Xm4Poller : IDisposable
    {
        // TODO: create options with public access
        private static readonly TimeSpan PollInterval
            = TimeSpan.FromSeconds( 1 );
        private static readonly TimeSpan BatteryLevel_UpdateInterval
            = TimeSpan.FromMinutes( 1 );
        private const int LinearBackoffFactor = 2;


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
            bool connected = false;
            DateTimeOffset lastUpdatedTime = DateTimeOffset.MinValue;
            TimeSpan currentUpdateInterval = TimeSpan.FromSeconds( 1 );

            while ( !token.IsCancellationRequested ) {
                var currentConnection = _xm4.IsConnected;

                var connectionChanged =
                    connected != currentConnection;
                if (connectionChanged) {
                    currentUpdateInterval = TimeSpan.FromSeconds( 1 );
                    OnConnectionChanged( currentConnection );
                }

                connected = currentConnection;

                if ( token.IsCancellationRequested ) break;

                var updateBatteryLevel =
                    ( DateTimeOffset.UtcNow - lastUpdatedTime ) > currentUpdateInterval
                    || batteryLevel < 1
                    || connectionChanged;

                if ( updateBatteryLevel ) {
                    // Pause a little after headphones connected ('connection' is true)
                    // but before start updating battery level
                    if (currentUpdateInterval < BatteryLevel_UpdateInterval)
                        currentUpdateInterval *= LinearBackoffFactor;

                    var currentLevel = _xm4.BatteryLevel;

                    if ( batteryLevel != currentLevel )
                        OnBatteryLevelChanged( currentLevel );

                    batteryLevel = currentLevel;

                    lastUpdatedTime = DateTimeOffset.UtcNow;
                }

                Thread.Sleep( PollInterval );
            }
        }

        public void Stop() => _cts?.Cancel();

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
