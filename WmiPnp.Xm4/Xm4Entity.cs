using FluentResults;

namespace WmiPnp.Xm4
{
    public class Xm4Entity
    {
        private readonly PnpEntity _xm4;
        private readonly PnpEntity _handsFree;

        // TODO: create options with public access
        private static readonly TimeSpan BatteryLevel_UpdateInterval = TimeSpan.FromMinutes( 5 );

        private Xm4Entity( PnpEntity handsFree, PnpEntity xm4 )
        {
            _handsFree = handsFree;
            _xm4 = xm4;
        }

        public static Result<Xm4Entity> Create()
            => CreateBy(
                HandsFree_PnpEntity_FriendlyName,
                Headphones_PnpEntity_FriendlyName );

        public static Result<Xm4Entity> CreateBy(
            string handsfreeNameExact,
            string headphonesNameExact )
        {
            var hfResult =
                PnpEntity
                .ByFriendlyName(
                    handsfreeNameExact
                    ?? HandsFree_PnpEntity_FriendlyName );

            if ( hfResult.IsFailed )
                return Result.Fail( $"Can not create {HandsFree_PnpEntity_FriendlyName} entity" );

            var xm4result =
                PnpEntity
                .ByFriendlyName(
                    headphonesNameExact
                    ?? Headphones_PnpEntity_FriendlyName );

            if ( xm4result.IsFailed )
                return Result.Fail( $"Can not create {Headphones_PnpEntity_FriendlyName} entity" );

            return
                new Xm4Entity(
                    hfResult.Value,
                    xm4result.Value );
        }

        public static Result<Xm4Entity> CreateUnsafe(
            PnpEntity batteryEntity,
            PnpEntity stateEntity )
            => new Xm4Entity(
                batteryEntity,
                stateEntity );

        private DateTimeOffset _batteryLevel_lastUpdate = DateTimeOffset.MinValue;
        private byte _batteryLevelCached = 0;
        public int BatteryLevel {
            get {
                var update =
                    ( DateTimeOffset.UtcNow - _batteryLevel_lastUpdate ) > BatteryLevel_UpdateInterval
                    || _batteryLevelCached < 1;

                if ( update ) {
                    var batteryLevel =
                        _handsFree.GetDeviceProperty(
                            PnpEntity.DeviceProperty_BatteryLevel )
                        .Value;

                    _batteryLevelCached = (byte)( batteryLevel.Data ?? 0 );
                    _batteryLevel_lastUpdate = DateTimeOffset.UtcNow;
                }

                return _batteryLevelCached;
            }
        }

        public bool IsConnected {
            get {
                var connected =
                    _xm4.GetDeviceProperty(
                        PnpEntity.DeviceProperty_IsConnected )
                    .Value;
                return (bool)( connected.Data ?? false );
            }
        }

        public const string HandsFree_PnpEntity_FriendlyName
            = "WH-1000XM4 Hands-Free AG"; // Battery level related
        public const string Headphones_PnpEntity_FriendlyName
            = "WH-1000XM4"; // Headphones state related
    }
}