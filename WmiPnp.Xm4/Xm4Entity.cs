using LanguageExt;

namespace WmiPnp.Xm4
{
    public class Xm4Entity
    {
        private PnpEntity _xm4;

        private Xm4Entity( PnpEntity xm4 )
        { 
            _xm4 = xm4;
            _batteryLevel =
                new( () =>
                    _xm4.GetDeviceProperty( DeviceProperty_BatteryLevel )
                    .Some( dp => dp )
                    .None( () => throw new InvalidOperationException( "Can not call battery level function" ) ) );
        }

        public static Option<Xm4Entity> Create()
            => PnpEntity
            .ByFriendlyName( PnpEntity_FriendlyName )
            .Some( xm4 => (Option<Xm4Entity>)new Xm4Entity( xm4 ) )
            .None( Option<Xm4Entity>.None );

        private DateTimeOffset _batteryLevel_lastAccess = DateTimeOffset.MinValue;
        private readonly Lazy<DeviceProperty> _batteryLevel;
        private byte _batteryLevelCached = 0;
        public int BatteryLevel {
            get {
                var update =
                    ( DateTimeOffset.UtcNow - _batteryLevel_lastAccess ) > BatteryLevel_UpdateInterval
                    || _batteryLevelCached < 1;

                if ( update ) {
                    _xm4.UpdateProperty( _batteryLevel.Value );

                    _batteryLevelCached = (byte)( _batteryLevel.Value.Data ?? 0 );
                    _batteryLevel_lastAccess = DateTimeOffset.UtcNow;
                }

                return _batteryLevelCached;
            }
        }

        static readonly TimeSpan BatteryLevel_UpdateInterval = TimeSpan.FromMinutes( 5 );
        const string PnpEntity_FriendlyName = "WH-1000XM4 Hands-Free AG";
        const string DeviceProperty_BatteryLevel = "{104EA319-6EE2-4701-BD47-8DDBF425BBE5} 2";
        const string DeviceProperty_IsConnected = "{83DA6326-97A6-4088-9453-A1923F573B29} 15";
        
        const string DEVPKEY_Bluetooth_LastConnectedTime = "{2BD67D8B-8BEB-48D5-87E0-6CDA3428040A} 11";
        const string DEVPKEY_Bluetooth_LastConnectedTime2 = "{2BD67D8B-8BEB-48D5-87E0-6CDA3428040A} 5";
    }
}