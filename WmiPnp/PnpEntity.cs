using LanguageExt;
using System.Management;
using WmiPnp.Extensions;

namespace WmiPnp;

public class PnpEntity
{
    public string? Name;
    public string? Description;

    public string? ClassGuid;
    public string? DeviceId;
    public string? PnpDeviceId;

    /// <summary>
    /// Find one or more entities by given name
    /// </summary>
    /// <param name="name">The name or part of its for entities</param>
    /// <returns>List of found entities or empty list</returns>
    public static IEnumerable<PnpEntity> ByFriendlyName( string name )
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
                .Select( o => o.ToPnpEntity() );
        }
        catch { }

        return entities;
    }

    /// <summary>
    /// Find entity by exact equal device id
    /// </summary>
    /// <param name="id">DeviceID or PNPDeviceID</param>
    /// <returns>PnpEntity or None</returns>
    public static Option<PnpEntity> ByDeviceId( string id )
    {
        Option<PnpEntity> entity = Option<PnpEntity>.None;

        try {
            id = id.Replace( "\\", "\\\\" );

            var searcher =
                new ManagementObjectSearcher(
                    Select_Win32PnpEntity_Where
                    + $"DeviceID='{id}' OR PNPDeviceId='{id}'" );

            var collection = searcher.Get();

            entity =
                collection
                .Cast<ManagementBaseObject?>()
                .FirstOrDefault()
                ?.ToPnpEntity();
        }
        catch { }

        return entity;
    }

    const string Select_Win32PnpEntity_Where =
       "Select Name,Description,ClassGuid,DeviceID,PNPDeviceID From Win32_PnPEntity WHERE ";
}