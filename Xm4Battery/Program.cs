using System.Diagnostics;
using System.Security.Principal;
using WmiPnp.Xm4;

namespace Xm4Battery
{
    internal static class Program
    {
        [STAThread]
        static int Main()
        {
            ApplicationConfiguration.Initialize();

            var xm4result = Xm4Entity.Create();
            if (xm4result.IsFailed)
                return Xm4NotFound_ErrorLevel;

            Xm4Entity xm4 = xm4result.Value;

            NotifyIcon notifyIconCtrl =
                new() {
                    Text = NotifyIcon_BatteryLevelTitle,
                    Visible = true,
                    Icon = CreateIconForLevel( DisconnectedLevel ),
                    ContextMenuStrip = CreateContextMenu(),
                };

            Xm4Poller statePoll = new( xm4 );

            statePoll.ConnectionChanged +=
                ( _, connected ) => {
                    UpdateUi
                        ( xm4
                        , notifyIconCtrl
                        , connectionStatus: connected
                        );
                };

            statePoll.BatteryLevelChanged +=
                ( _, level ) => {
                    UpdateUi
                        ( xm4
                        , notifyIconCtrl
                        , batteryLevel: level
                        );
                };

            statePoll.Start();

            Application.Run();

            notifyIconCtrl.Visible = false;
            notifyIconCtrl.Dispose();
            statePoll.Stop();

            return ExitOk_ErrorLevel;
        }

        private static ContextMenuStrip CreateContextMenu()
        {
            bool runasAdmin =
                new WindowsPrincipal( WindowsIdentity.GetCurrent() )
                .IsInRole( WindowsBuiltInRole.Administrator );

            ContextMenuStrip contextMenu = new();
            contextMenu.Items.AddRange( new ToolStripItem[] {
                new ToolStripMenuItem(
                    "&Connect",
                    null, (_,_) => {
                        Xm4Entity.TryConnect();
                    })
                {
                    Name = ConnectCtxMenuItemName,
                    Enabled = true,
                    Visible = runasAdmin
                },

                new ToolStripMenuItem(
                    "&Disconnect",
                    null, (_,_) => {
                        Xm4Entity.TryDisconnect();
                    } )
                {
                    Name = DisconnectCtxMenuItemName,
                    Enabled = false,
                    Visible = runasAdmin
                },

                new ToolStripSeparator() {
                    Visible = runasAdmin
                },

                new ToolStripMenuItem(
                    $"&About {AppName} {AppVersion}",
                    null, (_,_) => {
                        try {
                            Process.Start(
                                new ProcessStartInfo(
                                    "cmd", $"/c start {GithubProjectUrl}") {
                                    CreateNoWindow = true });
                        } catch {}
                    } ),

                new ToolStripSeparator(),

                new ToolStripMenuItem(
                    "&Quit",
                    null, (_,_) => {
                        Application.Exit();
                    } ),
            } );

            return contextMenu;
        }

        static readonly Font _notifyIconFont
            = new( "Segoe UI", 10, FontStyle.Regular );

        private static Icon CreateIconForLevel( int level )
        {
            const int iw = NotifyIconDefault_WidthPx;
            const int ih = NotifyIconDefault_HeightPx;

            using Bitmap icoBitmap = new( iw, ih );
            using var g = Graphics.FromImage( icoBitmap );
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // icon background
            var brush =
                level switch {
                    > 0 and <= 10 => Brushes.Red,
                    > 0 and <= 20 => Brushes.Orange,
                    > 0 and <= 30 => Brushes.Yellow,
                    <= 0 => Brushes.Gray,
                    _ => Brushes.White
                };

            g.FillRectangle(
                brush,
                0, 0, iw - 1, ih - 1 );

            // icon text: battery level or status
            var iconText =
                level switch {
                    100 => "F", // Fully charged
                    > 0 and < 100 => level.ToString()[..^1],
                    _ => "X" // Disconnected
                };

            var sizeS =
                g.MeasureString(
                    iconText,
                    _notifyIconFont );

            g.DrawString(
                iconText
                , _notifyIconFont
                , Brushes.Black
                , iw / 2 - sizeS.Width / 2
                , ih / 2 - sizeS.Height / 2 );

            Icon icon =
                Icon.FromHandle(
                    icoBitmap.GetHicon() );

            return icon;
        }

        private static void UpdateUi(
            Xm4Entity xm4
            , NotifyIcon notifyIconCtrl
            , bool? connectionStatus = null
            , int? batteryLevel = null )
        {
            var connected =
                connectionStatus
                ?? xm4?.IsConnected
                ?? false;

            var level =
                batteryLevel
                ?? xm4?.BatteryLevel
                ?? DisconnectedLevel;

            var items = notifyIconCtrl.ContextMenuStrip.Items;
            items[ConnectCtxMenuItemName].Enabled = !connected;
            items[DisconnectCtxMenuItemName].Enabled = connected;

            notifyIconCtrl.Icon =
                CreateIconForLevel(
                    connected ? level
                    : DisconnectedLevel );

            var at =
                connected ? string.Empty
                : $"\n{xm4!.LastConnectedTime.Value:F}";

            notifyIconCtrl.Text =
                $"{NotifyIcon_BatteryLevelTitle} ⚡{level}%{at}";
        }

        const string ConnectCtxMenuItemName = nameof( ConnectCtxMenuItemName );
        const string DisconnectCtxMenuItemName = nameof( DisconnectCtxMenuItemName );
        const int NotifyIconDefault_WidthPx = 20;
        const int NotifyIconDefault_HeightPx = 20;

        const int DisconnectedLevel = 0; 
        const string NotifyIcon_BatteryLevelTitle = "XM4 Battery Level";

        const string AppName = "Xm4Battery";
        const string AppVersion = "3.6.6";
        const string GithubProjectUrl = "https://github.com/nikvoronin/WmiPnp";

        const int Xm4NotFound_ErrorLevel = 1;
        const int ExitOk_ErrorLevel = 0;
    }
}