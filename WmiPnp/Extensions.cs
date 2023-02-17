using System.Management;

namespace WmiPnp
{
    public static class Extensions
    {
        public static PnpEntity ToPnpEntity(
            this ManagementBaseObject get )
            => new() {
                Name = get.ValueOf( "Name" ),
                Description = get.ValueOf( "Description" ),
                ClassGuid = get.ValueOf( "ClassGuid" ),
                DeviceId = get.ValueOf( "DeviceID" ),
                PnpDeviceId = get.ValueOf( "PNPDeviceID" ),
            };

        public static string ValueOf(
            this ManagementBaseObject o,
            string name )
            => (string)o.GetPropertyValue( name );
    }
}
