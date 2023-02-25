using LanguageExt;

namespace WmiPnp.Xm4
{
    public class Xm4Entity
    {
        private readonly PnpEntity _xm4;

        // TODO: create options with public access
        private static readonly TimeSpan BatteryLevel_UpdateInterval = TimeSpan.FromMinutes( 5 );

        private Xm4Entity( PnpEntity xm4 )
        {
            _xm4 = xm4;
            _batteryLevel =
                new( () =>
                    _xm4.GetDeviceProperty( PnpEntity.DeviceProperty_BatteryLevel )
                    .Some( dp => dp )
                    .None( () => throw new InvalidOperationException( "Can not call battery level function" ) ) );
        }

        public static Option<Xm4Entity> Create()
            => PnpEntity
            .ByFriendlyName( PnpEntity_FriendlyName )
            .Some( xm4 => (Option<Xm4Entity>)new Xm4Entity( xm4 ) )
            .None( Option<Xm4Entity>.None );

        public static Option<Xm4Entity> CreateBy( string friendlyNameExact )
            => PnpEntity
            .ByFriendlyName( friendlyNameExact ?? PnpEntity_FriendlyName )
            .Some( xm4 => (Option<Xm4Entity>)new Xm4Entity( xm4 ) )
            .None( Option<Xm4Entity>.None );

        public static Option<Xm4Entity> CreateUnsafe( Some<PnpEntity> e )
            => new Xm4Entity( e );

        private DateTimeOffset _batteryLevel_lastUpdate = DateTimeOffset.MinValue;
        private readonly Lazy<DeviceProperty> _batteryLevel;
        private byte _batteryLevelCached = 0;
        public int BatteryLevel {
            get {
                var update =
                    ( DateTimeOffset.UtcNow - _batteryLevel_lastUpdate ) > BatteryLevel_UpdateInterval
                    || _batteryLevelCached < 1;

                if ( update ) {
                    _xm4.UpdateProperty( _batteryLevel.Value );

                    _batteryLevelCached = (byte)( _batteryLevel.Value.Data ?? 0 );
                    _batteryLevel_lastUpdate = DateTimeOffset.UtcNow;
                }

                return _batteryLevelCached;
            }
        }
        
        public const string PnpEntity_FriendlyName
            = "WH-1000XM4 Hands-Free AG";
    }
}