using FluentResults;
using System.Management;

namespace WmiPnp;

public class PnpEntity
{
    public string? Name;
    public string? Description;

    public string? ClassGuid;
    public string? DeviceId;
    public string? PnpDeviceId;

    private ManagementObject? _entity = null;
    public IEnumerable<DeviceProperty> Properties { get; private set; }
        = Enumerable.Empty<DeviceProperty>();

    /// <summary>
    /// Update and store device properties in <see cref="Properties" /> field
    /// </summary>
    /// <returns>List of device properties with current data</returns>
    public IEnumerable<DeviceProperty> UpdateProperties()
    {
        ArgumentNullException.ThrowIfNull( _entity ); // TODO do not allow mandatory fields to be a null

        ManagementBaseObject inParams =
            _entity.GetMethodParameters( GetDeviceProperties_MethodName );

        ManagementBaseObject outParams =
            _entity.InvokeMethod( GetDeviceProperties_MethodName, inParams, null );

        var enumerator = outParams.Properties.GetEnumerator();
        enumerator.MoveNext();
        var mbos = enumerator.Current.Value as ManagementBaseObject[];
        Properties =
            mbos
            ?.Cast<ManagementBaseObject>()
            .Select( p =>
                new DeviceProperty (
                    deviceId: p.ValueOf( DeviceProperty.DeviceID_PropertyField ),
                    key: p.ValueOf( DeviceProperty.Key_PropertyField ),
                    keyName: p.ValueOf( DeviceProperty.KeyName_PropertyField ),
                    type: (uint)p.GetPropertyValue( DeviceProperty.Type_PropertyField ),
                    data: p.GetPropertyValue( DeviceProperty.Data_PropertyField ) 
                    )
                )
            ?? Enumerable.Empty<DeviceProperty>();

        return Properties;
    }

    public Result<DeviceProperty> UpdateProperty( DeviceProperty deviceProperty )
    {
        var result = GetDeviceProperty( deviceProperty.Key );

        deviceProperty.Data =
            result.ValueOrDefault?.Data;

        return
            result.IsSuccess ? deviceProperty
            : result;
    }

    /// <summary>
    /// Get device property
    /// </summary>
    /// <param name="key">Device property --key or --keyName</param>
    /// <returns></returns>
    public Result<DeviceProperty> GetDeviceProperty( string key )
    {
        ArgumentNullException.ThrowIfNull( _entity ); // TODO do not allow mandatory fields to be a null

        var args = new object[] { new string[] { key }, null! };
        try {
            _entity.InvokeMethod( GetDeviceProperties_MethodName, args );
        }
        catch ( ManagementException e ) {
            return
                Result.Fail(
                    new Error( $"Entity not found or wrong key. Exception when invoke method {GetDeviceProperties_MethodName}" )
                    .CausedBy( e ) );
        }

        ManagementBaseObject? ss = ( args[1] as ManagementBaseObject[] )?[0];
        if ( ss is null )
            return Result.Fail( $"Method {GetDeviceProperties_MethodName} returns nothing." );

        var ps =
            new Dictionary<string, object>(
                ss.Properties
                .Cast<PropertyData>()
                .Select( x => new KeyValuePair<string, object>( x.Name, x.Value ) ) );

        var typeValue = (uint)ss.GetPropertyValue( DeviceProperty.Type_PropertyField );
        _ = ps.TryGetValue(
            DeviceProperty.Data_PropertyField,
            out var dataValue );

        var noValidDataValue =
            typeValue == (uint)DataType.Empty
            || dataValue is null;

        if ( noValidDataValue )
            return Result.Fail( $"No valid data value: type={typeValue}; data:`{dataValue}`." );

        DeviceProperty dp =
            new(
                deviceId: ss.ValueOf( DeviceProperty.DeviceID_PropertyField ),
                key: ss.ValueOf( DeviceProperty.Key_PropertyField ),
                type: typeValue,
                data: dataValue
            );

        return dp;
    }

    private static Result<PnpEntity> EntityOrNone( string where )
    {
        Result<PnpEntity> entity = Result.Fail( $"No entity WHERE=`{where}`" );

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
    public static Result<PnpEntity> ByFriendlyName( string name )
        => EntityOrNone( where: $"{Name_FieldName}='{name}'" );

    /// <summary>
    /// Find entity by exact equal device id
    /// </summary>
    /// <param name="id">DeviceID or PNPDeviceID</param>
    /// <param name="duplicateSlashes">Duplicate slashes by default, so '\' becomes '\\'.</param>
    /// <returns>PnpEntity or None</returns>
    public static Result<PnpEntity> ByDeviceId( string id, bool duplicateSlashes = true )
    {
        if ( duplicateSlashes )
            id = id.Replace( "\\", "\\\\" );

        return
            EntityOrNone(
                where: $"{DeviceId_FieldName}='{id}' OR {PnpDeviceId_FieldName}='{id}'" );
    }

    /// <summary>
    /// Find one or more entities by given name
    /// </summary>
    /// <param name="name">The name or part of its for entities</param>
    /// <returns>List of found entities or empty list</returns>
    public static IEnumerable<PnpEntity> LikeFriendlyName( string name )
    {
        IEnumerable<PnpEntity> entities = Enumerable.Empty<PnpEntity>();

        try {
            var searcher =
                new ManagementObjectSearcher(
                    Select_Win32PnpEntity_Where
                    + $"{Name_FieldName} LIKE '%{name}%'" );

            var collection = searcher.Get();

            entities =
                collection
                .Cast<ManagementBaseObject>()
                .Select( o => ToPnpEntity( o ) );
        }
        catch { }

        return entities;
    }

    private static PnpEntity ToPnpEntity(
        ManagementBaseObject entity )
        => new() {
            Name = entity.ValueOf( Name_FieldName ),
            Description = entity.ValueOf( Description_FieldName ),
            ClassGuid = entity.ValueOf( ClassGuid_FieldName ),
            DeviceId = entity.ValueOf( DeviceId_FieldName ),
            PnpDeviceId = entity.ValueOf( PnpDeviceId_FieldName ),
            _entity =
                entity as ManagementObject
                ?? throw new NotSupportedException( "Not a ManagementObject." ),
        };

    const string Select_Win32PnpEntity_Where =
       "Select Name,Description,ClassGuid,DeviceID,PNPDeviceID From Win32_PnPEntity WHERE ";

    public const string Name_FieldName = "Name";
    public const string Description_FieldName = "Description";
    public const string ClassGuid_FieldName = "ClassGuid";
    public const string DeviceId_FieldName = "DeviceID";
    public const string PnpDeviceId_FieldName = "PNPDeviceID";

    public const string GetDeviceProperties_MethodName = "GetDeviceProperties";

    public const string DeviceProperty_BatteryLevel
        = "{104EA319-6EE2-4701-BD47-8DDBF425BBE5} 2";
    public const string DeviceProperty_IsConnected
        = "{83DA6326-97A6-4088-9453-A1923F573B29} 15";

    public const string DEVPKEY_Bluetooth_LastConnectedTime
        = "{2BD67D8B-8BEB-48D5-87E0-6CDA3428040A} 11";
    public const string DEVPKEY_Bluetooth_LastConnectedTime2
        = "{2BD67D8B-8BEB-48D5-87E0-6CDA3428040A} 5";
}

public static class ManagementObjectExtensions
{
    public static string ValueOf(
        this ManagementBaseObject o,
        string name )
        => (string)o.GetPropertyValue( name );
}
