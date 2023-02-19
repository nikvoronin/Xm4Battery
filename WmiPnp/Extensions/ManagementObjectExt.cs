using System.Management;

namespace WmiPnp.Extensions;

public static class ManagementObjectExt
{
    public static string ValueOf(
        this ManagementBaseObject o,
        string name)
        => (string)o.GetPropertyValue(name);
}
