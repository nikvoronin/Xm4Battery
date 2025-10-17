namespace WmiPnp.Xm4;

using StateChangedHandler = Action<Xm4State, Xm4State>;

public class Xm4Poller : IDisposable
{
    public Xm4Poller(
        Xm4Entity xm4,
        StateChangedHandler? stateChangedHandler )
    {
        _xm4 = xm4 ?? throw new ArgumentNullException( nameof( xm4 ) );
        _stateChangedHandler = stateChangedHandler;

    }

    public void Dispose() => Stop();

    public void Start()
    {
        if (_cts is not null) return;

        _cts = new();
        Thread thread = new( ThreadWorker );
        thread.SetApartmentState( ApartmentState.STA );
        thread.Start( _cts.Token );
    }

    private void ThreadWorker( object? o )
    {
        CancellationToken token =
            (CancellationToken)(o ?? CancellationToken.None);

        Xm4State lastState = new();

        DateTimeOffset lastUpdatedTime = DateTimeOffset.MinValue;
        TimeSpan currentUpdateInterval = TimeSpan.FromSeconds( 1 );

        while (!token.IsCancellationRequested) {
            Xm4State currentState = lastState with {
                Connected = _xm4.IsConnected
            };

            var connectionChanged =
                currentState.Connected != lastState.Connected;
            if (connectionChanged)
                currentUpdateInterval = TimeSpan.FromSeconds( 1 );

            if (token.IsCancellationRequested) break;

            var updateBatteryLevel =
                (DateTimeOffset.UtcNow - lastUpdatedTime) > currentUpdateInterval
                || lastState.BatteryLevel < 1
                || connectionChanged;

            if (updateBatteryLevel) {
                // Pause a little after headphones connected ('connection' is true)
                // but before start updating battery level
                if (currentUpdateInterval < BatteryLevel_UpdateInterval)
                    currentUpdateInterval *= LinearBackoffFactor;

                currentState = currentState with {
                    BatteryLevel = _xm4.BatteryLevel
                };

                if (currentState != lastState)
                    _stateChangedHandler?.Invoke( lastState, currentState );

                lastUpdatedTime = DateTimeOffset.UtcNow;
                lastState = currentState;
            }

            Thread.Sleep( PollingInterval );
        }
    }

    public void Stop() => _cts?.Cancel();

    // TODO: create options with public access
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds( 1 );
    private static readonly TimeSpan BatteryLevel_UpdateInterval = TimeSpan.FromMinutes( 1 );
    private const int LinearBackoffFactor = 2;

    private readonly Xm4Entity _xm4;
    private CancellationTokenSource? _cts;
    private readonly StateChangedHandler? _stateChangedHandler;
}
