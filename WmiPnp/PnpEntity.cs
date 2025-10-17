using FluentResults;
using System.Management;

namespace WmiPnp;

public sealed class PnpEntity
{
    public readonly string? Name;
    public readonly string? Description;

    public readonly string? ClassGuid;
    public readonly string? DeviceId;
    public readonly string? PnpDeviceId;

    private PnpEntity( ManagementBaseObject entity )
    {
        Name = entity.ValueOf( Name_FieldName );
        Description = entity.ValueOf( Description_FieldName );
        ClassGuid = entity.ValueOf( ClassGuid_FieldName );
        DeviceId = entity.ValueOf( DeviceId_FieldName );
        PnpDeviceId = entity.ValueOf( PnpDeviceId_FieldName );

        _entity = entity as ManagementObject
            ?? throw new NotSupportedException( $"Not a {nameof( ManagementObject )}." );
    }

    /// <summary>
    /// Get values of device properties.
    /// </summary>
    /// <returns>
    /// Collection of device properties with current data or empty.
    /// </returns>
    public IEnumerable<DeviceProperty> GetProperties()
    {
        using ManagementBaseObject inParams =
            _entity.GetMethodParameters( GetDeviceProperties_MethodName );

        using ManagementBaseObject outParams =
            _entity.InvokeMethod( GetDeviceProperties_MethodName, inParams, null );

        var mbos =
            outParams.Properties
            .Cast<PropertyData>()
            .FirstOrDefault()
            ?.Value as ManagementBaseObject[];

        return
            mbos?.Cast<ManagementBaseObject>()
            .Select( p =>
                new DeviceProperty(
                    deviceId: p.ValueOf( DeviceProperty.DeviceID_PropertyField ),
                    key: p.ValueOf( DeviceProperty.Key_PropertyField ),
                    keyName: p.ValueOf( DeviceProperty.KeyName_PropertyField ),
                    type: (uint)p.GetPropertyValue( DeviceProperty.Type_PropertyField ),
                    data: p.GetPropertyValue( DeviceProperty.Data_PropertyField )
                    ) )
            ?? [];
    }

    /// <summary>
    /// Get device property.
    /// </summary>
    /// <param name="deviceProperty">
    /// An instanec of <see cref="DeviceProperty"/>.
    /// </param>
    /// <return>
    /// A device property with the same id
    /// but with updated <see cref="DeviceProperty.Data"/> field.
    /// </return>
    public Result<DeviceProperty> GetDeviceProperty(
        DeviceProperty deviceProperty )
        => GetDeviceProperty( deviceProperty.Key );

    /// <summary>
    /// Get device property.
    /// </summary>
    /// <param name="key">
    /// <see cref="DeviceProperty.Key"/> or <see cref="DeviceProperty.KeyName"/>.
    /// </param>
    public Result<DeviceProperty> GetDeviceProperty( string key )
    {
        var args = new object[] { new string[] { key }, null! };
        try {
            _entity.InvokeMethod( GetDeviceProperties_MethodName, args );
        }
        catch (ManagementException e) {
            return Result.Fail(
                new Error(
                    $"Entity not found or wrong key. Exception when invoke method {GetDeviceProperties_MethodName}" )
                .CausedBy( e ) );
        }

        using ManagementBaseObject? ss = (args[1] as ManagementBaseObject[])?[0];
        if (ss is null)
            return Result.Fail(
                $"Method {GetDeviceProperties_MethodName} returns nothing." );

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

        if (noValidDataValue)
            return Result.Fail(
                $"No valid data value: type={typeValue}; data:`{dataValue}`." );

        DeviceProperty dp = new(
            deviceId: ss.ValueOf( DeviceProperty.DeviceID_PropertyField ),
            key: ss.ValueOf( DeviceProperty.Key_PropertyField ),
            type: typeValue,
            data: dataValue
        );

        // This prevents memory leaks and handle exhaustion
        // caused by the GC not cleaning up resources for an extended period.
        GC.Collect();
        GC.WaitForFullGCComplete( TimeSpan.FromMilliseconds( 100 ) );

        return dp;
    }

    /// <summary>
    /// Enable device.
    /// </summary>
    /// <remarks>
    /// System Administrator right needed.
    /// </remarks>
    /// <returns>
    /// <see cref="Result"> of operation.
    /// </returns>
    public Result Enable()
    {
        try {
            _entity.InvokeMethod( Enable_MethodName, null, null );
        }
        catch (ManagementException e) {
            return Result.Fail(
                new Error( $"_entity not found or wrong key. Exception when invoke method {Enable_MethodName}" )
                .CausedBy( e ) );
        }

        return Result.Ok();
    }

    /// <summary>
    /// Disable device.
    /// </summary>
    /// <remarks>
    /// System Administrator right needed.
    /// </remarks>
    /// <returns>
    /// <see cref="Result"> of operation.
    /// </returns>
    public Result Disable()
    {
        try {
            _entity.InvokeMethod( Disable_MethodName, null, null );
        }
        catch (ManagementException e) {
            return Result.Fail(
                new Error( $"Entity not found or wrong key. Exception when invoke method {Disable_MethodName}" )
                .CausedBy( e ) );
        }

        return Result.Ok();
    }

    public static Result<PnpEntity> EntityOrNone( string where )
    {
        Result<PnpEntity> entity =
            Result.Fail( $"No entity WHERE=`{where}`" );

        try {
            using var searcher =
                new ManagementObjectSearcher(
                    Select_Win32PnpEntity_Where
                    + where );

            using var collection = searcher.Get();

            var mo =
                collection
                .Cast<ManagementBaseObject>()
                .FirstOrDefault();

            var deviceFound = mo is not null;
            if (deviceFound)
                entity = Result.Ok( new PnpEntity( mo! ) );
        }
        catch { }

        return entity;
    }

    public static IEnumerable<PnpEntity> EntitiesOrNone( string where )
    {
        IEnumerable<PnpEntity> entities = [];

        try {
            using var searcher =
                new ManagementObjectSearcher(
                    Select_Win32PnpEntity_Where
                    + where );

            using var collection = searcher.Get();

            entities =
                collection
                .Cast<ManagementBaseObject>()
                .Select( mo => new PnpEntity( mo ) );
        }
        catch { }

        return entities;
    }

    /// <summary>
    /// Find exact one entity for given name
    /// </summary>
    /// <param name="name">Full name of a device</param>
    /// <returns>PnpEntity or Fail</returns>
    public static Result<PnpEntity> ByFriendlyName( string name ) =>
        EntityOrNone( where: $"{Name_FieldName} LIKE '{name}'" );

    /// <summary>
    /// Find entity by exact equal device id
    /// </summary>
    /// <param name="id">DeviceID or PNPDeviceID</param>
    /// <param name="duplicateSlashes">Duplicate slashes by default, so '\' becomes '\\'.</param>
    /// <returns>PnpEntity or Fail</returns>
    public static Result<PnpEntity> ByDeviceId( string id, bool duplicateSlashes = true )
    {
        if (duplicateSlashes)
            id = id.Replace( "\\", "\\\\" );

        return EntityOrNone(
            where: $"{DeviceId_FieldName}='{id}' OR {PnpDeviceId_FieldName}='{id}'" );
    }

    /// <summary>
    /// Find one or more entities by given name
    /// </summary>
    /// <param name="name">Part of the device name</param>
    /// <returns>List of found entities or empty list</returns>
    public static IEnumerable<PnpEntity> FindByFriendlyName( string name ) =>
        EntitiesOrNone( where: $"{Name_FieldName} LIKE '%{name}%'" );

    /// <summary>
    /// Find one or more entitiesby given name within Bluetooth class
    /// </summary>
    /// <param name="name">Part of the device name</param>
    /// <param name="className">Exact PNPClass name of the devices</param>
    /// <returns>List of found entities or empty list</returns>
    public static IEnumerable<PnpEntity> FindByNameForExactClass(
        string name,
        string className )
        => EntitiesOrNone(
            where: $"PNPClass = '{className}' AND {Name_FieldName} LIKE '%{name}%'" );

    private readonly ManagementObject _entity;

    const string Select_Win32PnpEntity_Where =
       "Select Name,Description,ClassGuid,DeviceID,PNPDeviceID From Win32_PnPEntity WHERE ";

    public const string Name_FieldName = "Name";
    public const string Description_FieldName = "Description";
    public const string ClassGuid_FieldName = "ClassGuid";
    public const string DeviceId_FieldName = "DeviceID";
    public const string PnpDeviceId_FieldName = "PNPDeviceID";

    public const string GetDeviceProperties_MethodName = "GetDeviceProperties";
    public const string Enable_MethodName = "Enable";
    public const string Disable_MethodName = "Disable";
}

file static class ManagementObjectExtensions
{
    public static string ValueOf(
        this ManagementBaseObject o,
        string name )
        => (string)o.GetPropertyValue( name );
}
