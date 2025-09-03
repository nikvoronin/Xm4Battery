using Microsoft.Win32;
using System.Security;

namespace Xm4Battery;

internal class SysRegistry
{
    public static bool IsInSystemStartup()
    {
        try {
            using var key = Registry.CurrentUser.OpenSubKey( RegistryAutoRunPath );

            return
                key?.GetValue( RegistryAppKeyName )
                is not null;
        }
        catch {
            return false;
        }
    }

    public static void ToggleLaunchAtStartup()
    {
        try {
            using var key =
                Registry.CurrentUser.OpenSubKey(
                    RegistryAutoRunPath,
                    true );

            var launchedAtStartup =
                key?.GetValue( RegistryAppKeyName )
                is not null;

            if (launchedAtStartup)
                key?.DeleteValue( RegistryAppKeyName );
            else {
                var appExePath = Application.ExecutablePath;

                if (!File.Exists( appExePath )) return;

                key?.SetValue(
                    RegistryAppKeyName,
                    $"\"{appExePath}\"" );
            }
        }
        catch (Exception e)
        when (e is ObjectDisposedException
            or SecurityException
            or UnauthorizedAccessException
            or IOException) { }
    }

    private const string RegistryAppKeyName = Program.AppName;
    private const string RegistryAutoRunPath =
        @"Software\Microsoft\Windows\CurrentVersion\Run";
}
