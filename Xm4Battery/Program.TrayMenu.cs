using System.Diagnostics;
using System.Security.Principal;
using WmiPnp.Xm4;

namespace Xm4Battery;

internal static partial class Program
{
    private static ContextMenuStrip CreateContextMenu()
    {
        bool runasAdmin =
            new WindowsPrincipal( WindowsIdentity.GetCurrent() )
            .IsInRole( WindowsBuiltInRole.Administrator );

        ContextMenuStrip contextMenu = new();
        contextMenu.Items.AddRange( [
            new ToolStripMenuItem(
                "&Connect",
                null,
                (_,_) => Xm4Entity.TryConnect() )
            {
                Name = ConnectCtxMenuItemName,
                Enabled = true,
                Visible = runasAdmin
            },

            new ToolStripMenuItem(
                "&Disconnect",
                null,
                (_,_) => Xm4Entity.TryDisconnect() )
            {
                Name = DisconnectCtxMenuItemName,
                Enabled = false,
                Visible = runasAdmin
            },

            new ToolStripSeparator() { Visible = runasAdmin },

            new ToolStripMenuItem(
                "&Launch at Startup",
                null,
                (_,_) => SysRegistry.ToggleLaunchAtStartup() )
            {
                Name = LaunchAtStartupMenuItemName,
                Checked = false
            },

            new ToolStripSeparator(),

            new ToolStripMenuItem(
                $"&About {AppName} {AppVersion}",
                null,
                (_,_) => {
                    try {
                        Process.Start(
                            new ProcessStartInfo(
                                "cmd",
                                $"/c start {GithubProjectUrl}")
                            {
                                CreateNoWindow = true
                            });
                    } catch {}
                } ),

            new ToolStripSeparator(),

            new ToolStripMenuItem(
                "&Quit",
                null,
                (_,_) => Application.Exit() ),
        ] );

        contextMenu.Opening +=
            ( sender, e ) => {
                if ((sender as ContextMenuStrip)?.Items[LaunchAtStartupMenuItemName]
                        is ToolStripMenuItem launchAtStartupCtxMenuItem) {
                    launchAtStartupCtxMenuItem.Checked =
                        SysRegistry.IsInSystemStartup();
                }
            };

        return contextMenu;
    }
}
