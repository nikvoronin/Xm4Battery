using LanguageExt;
using System.Management;
using System.Xml.Linq;
using WmiPnp.Extensions;

namespace WmiPnp;

public class PnpEntity
{
    public string? Name;
    public string? Description;

    public string? ClassGuid;
    public string? DeviceId;
    public string? PnpDeviceId;

    private ManagementObject? _entity = null;

    /// <summary>
    /// Get device property
    /// </summary>
    /// <param name="key">Device property --key or --keyName</param>
    /// <returns></returns>
    public Option<DeviceProperty> GetDeviceProperty( string key )
    {
        ArgumentNullException.ThrowIfNull( _entity ); // TODO do not allow mandatory fields to be a null

        var args = new object[] { new string[] { key }, null! };
        try {
            _entity.InvokeMethod( "GetDeviceProperties", args );
        }
        catch ( ManagementException ) {
            // Not found or wrong key
            return Option<DeviceProperty>.None;
        }

        ManagementBaseObject? ss = ( args[1] as ManagementBaseObject[] )?[0];
        if (ss is null) return Option<DeviceProperty>.None;

        var ps =
            ss.Properties
            .Cast<PropertyData>()
            .Select( x => new KeyValuePair<string, object>( x.Name, x.Value ) );

        var d = new Dictionary<string, object>( ps );

        var t = (uint)ss.GetPropertyValue( DeviceProperty.Type_PropertyField );
        var noValue =
            t == (uint)CimType.None;
        if ( noValue ) return Option<DeviceProperty>.None;

        DeviceProperty dp =
            new(
                deviceId: ss.ValueOf( DeviceProperty.DeviceID_PropertyField ),
                key: ss.ValueOf( DeviceProperty.Key_PropertyField ),
                type: t
            );

        _ = d.TryGetValue(
            DeviceProperty.Data_PropertyField,
            out dp.Data );

        return dp;
    }

    private static Option<PnpEntity> EntityOrNone( string where )
    {
        Option<PnpEntity> entity = Option<PnpEntity>.None;

        try {
            var searcher =
                new ManagementObjectSearcher(
                    Select_Win32PnpEntity_Where
                    + where );

            var collection = searcher.Get();

            var mo =
                collection
                .Cast<ManagementBaseObject>()
                .FirstOrDefault();

            var deviceFound = mo is not null;
            if ( deviceFound )
                entity = ToPnpEntity( mo! );
        }
        catch { }

        return entity;
    }

    /// <summary>
    /// Find exact one entity by given name
    /// </summary>
    /// <param name="name">The name or part of its for entities</param>
    /// <returns>PNP entity or None</returns>
    public static Option<PnpEntity> ByFriendlyName( string name )
        => EntityOrNone( where: $"Name='{name}'" );

    /// <summary>
    /// Find entity by exact equal device id
    /// </summary>
    /// <param name="id">DeviceID or PNPDeviceID</param>
    /// <returns>PnpEntity or None</returns>
    public static Option<PnpEntity> ByDeviceId( string id )
    {
        id = id.Replace( "\\", "\\\\" );

        return
            EntityOrNone( 
                where: $"DeviceID='{id}' OR PNPDeviceId='{id}'" );
    }

    /// <summary>
    /// Find one or more entities by given name
    /// </summary>
    /// <param name="name">The name or part of its for entities</param>
    /// <returns>List of found entities or empty list</returns>
    public static IEnumerable<PnpEntity> LikeFriendlyName( string name )
    {
        IEnumerable<PnpEntity> entities = List.empty<PnpEntity>();

        try {
            var searcher =
                new ManagementObjectSearcher(
                    Select_Win32PnpEntity_Where
                    + $"Name LIKE '%{name}%'" );

            var collection = searcher.Get();

            entities =
                collection
                .Cast<ManagementBaseObject>()
                .Select( o => ToPnpEntity(o) );
        }
        catch { }

        return entities;
    }

    private static PnpEntity ToPnpEntity(
        ManagementBaseObject entity )
        => new() {
            Name = entity.ValueOf( "Name" ),
            Description = entity.ValueOf( "Description" ),
            ClassGuid = entity.ValueOf( "ClassGuid" ),
            DeviceId = entity.ValueOf( "DeviceID" ),
            PnpDeviceId = entity.ValueOf( "PNPDeviceID" ),
            _entity =
                entity as ManagementObject
                ?? throw new NotSupportedException( "Not a ManagementObject." ),
        };

    const string Select_Win32PnpEntity_Where =
       "Select Name,Description,ClassGuid,DeviceID,PNPDeviceID From Win32_PnPEntity WHERE ";
}