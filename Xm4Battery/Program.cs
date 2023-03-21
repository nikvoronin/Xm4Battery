using System.Diagnostics;
using WmiPnp.Xm4;

namespace Xm4Battery
{
    internal static class Program
    {
#pragma warning disable CS8618
        static NotifyIcon _notifyIconControl;
#pragma warning restore CS8618

        [STAThread]
        static int Main()
        {
            ApplicationConfiguration.Initialize();

            var xm4result = Xm4Entity.Create();
            if ( xm4result.IsFailed ) return 1;

            _notifyIconControl = new() {
                Text = AppName,
                Visible = true,
                Icon = CreateIconForLevel( DisconnectedLevel ),
                ContextMenuStrip = CreateContextMenu(),
            };

            Xm4Entity xm4 = xm4result.Value;
            Xm4Poller statePoll = new ( xm4 );
            statePoll.ConnectionChanged += Xm4state_ConnectionChanged;
            statePoll.BatteryLevelChanged += Xm4state_BatteryLevelChanged;
            statePoll.Start();

            Application.Run();

            _notifyIconControl.Visible = false;
            _notifyIconControl.Dispose();
            statePoll.Stop();
            return 0;
        }

        private static ContextMenuStrip CreateContextMenu()
        {
            ContextMenuStrip contextMenu = new ();

            contextMenu.Items.AddRange( new ToolStripItem[] {
                new ToolStripMenuItem( "&About Xm4Battery",
                    null, ( sender, args ) => {
                        try {
                            Process.Start(
                                new ProcessStartInfo(
                                    "cmd", $"/c start {GithubProjectUrl}") {
                                    CreateNoWindow = true });
                        } catch {}
                    } ),

                new ToolStripSeparator(),

                new ToolStripMenuItem( "&Quit",
                    null, ( sender, args ) => {
                        Application.Exit();
                    } ),
            });

            return contextMenu;
        }

        static readonly Font _notifyIconFont
            = new ( "Segoe UI", 16, FontStyle.Regular );

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
                    > 0 and <= 40 => Brushes.Yellow,
                    <= 0 => Brushes.Gray,
                    _ => Brushes.White
                };

            g.FillRectangle(
                brush,
                1, 1, iw - 2, ih - 2 );

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
                , ih / 2 - sizeS.Height / 2 - 1 );

            IntPtr hIcon = icoBitmap.GetHicon();
            Icon icon = Icon.FromHandle( hIcon );

            return icon;
        }

        private static void Xm4state_BatteryLevelChanged( object? sender, int level )
        {
            var xm4 = sender as Xm4Entity;
            var connected = xm4?.IsConnected ?? false;

            UpdateNotifyIcon( xm4!, connected, level );
        }

        private static void Xm4state_ConnectionChanged( object? sender, bool connected )
        {
            var xm4 = sender as Xm4Entity;
            var level = xm4?.BatteryLevel ?? DisconnectedLevel;

            UpdateNotifyIcon( xm4!, connected, level );
        }

        private static void UpdateNotifyIcon(
            Xm4Entity xm4,
            bool connected,
            int level )
        {
            _notifyIconControl.Icon =
                CreateIconForLevel( 
                    connected ? level 
                    : DisconnectedLevel );

            var at =
                connected ? string.Empty
                : $"\n{xm4!.LastConnectedTime.Value:F}";

            _notifyIconControl.Text = $"{AppName} {level}%{at}";
        }

        const int NotifyIconDefault_WidthPx = 32;
        const int NotifyIconDefault_HeightPx = 32;
        const int DisconnectedLevel = 0;
        const string AppName = "XM4 Battery Level";
        const string GithubProjectUrl = "https://github.com/nikvoronin/WmiPnp";
    }
}