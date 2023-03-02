using System.Diagnostics;
using WmiPnp.Xm4;

namespace Xm4Battery
{
    internal static class Program
    {
#pragma warning disable CS8618
        static Xm4Poller _xm4state;
        static NotifyIcon _notifyIcon;
        static Icon _disconnectIcon;
#pragma warning restore CS8618

        [STAThread]
        static int Main()
        {
            ApplicationConfiguration.Initialize();

            var xm4result = Xm4Entity.Create();
            if ( xm4result.IsFailed ) return 1;

            _disconnectIcon = CreateLevelIcon();

            _notifyIcon = new() {
                Text = AppName,
                Visible = true,
                Icon = _disconnectIcon,
                ContextMenuStrip = CreateContextMenu(),
            };

            Xm4Entity xm4 = xm4result.Value;
            _xm4state = new Xm4Poller( xm4 );
            _xm4state.ConnectionChanged += Xm4state_ConnectionChanged;
            _xm4state.BatteryLevelChanged += Xm4state_BatteryLevelChanged;
            _xm4state.Start();

            Application.Run();

            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _xm4state.Stop();
            return 0;
        }

        private static ContextMenuStrip CreateContextMenu()
        {
            ContextMenuStrip contextMenu = new ();

            contextMenu.Items.AddRange( new ToolStripItem[] {
                new ToolStripMenuItem( "&About Xm4Battery",
                    null, ( sender, args ) => {
                        Process.Start(
                            new ProcessStartInfo(
                                "cmd", $"/c start {GithubProjectUrl}") { 
                                CreateNoWindow = true });
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

        private static Icon CreateLevelIcon( int level = -1 )
        {
            int iw = 32;
            int ih = 32;

            using Bitmap icoBitmap = new( iw, ih );
            using var g = Graphics.FromImage( icoBitmap );
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var iconText =
                level switch {
                    100 => "F", // Full
                    > 0 and < 100 => level.ToString()[..^1],
                    _ => "X" // disconnected
                };

            var sizeS =
                g.MeasureString(
                    iconText,
                    _notifyIconFont );

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

            var lastConnectedTime = string.Empty;
            var connected = xm4?.IsConnected ?? false;
            if ( connected )
                _notifyIcon.Icon = CreateLevelIcon( level );
            else
                lastConnectedTime = $"\n{xm4!.LastConnectedTime.Value:F}";

            _notifyIcon.Text = $"{AppName} {level}%{lastConnectedTime}";
        }

        private static void Xm4state_ConnectionChanged( object? sender, bool connected )
        {
            var xm4 = sender as Xm4Entity;
            var level = xm4?.BatteryLevel ?? 0;

            _notifyIcon.Icon =
                connected ? CreateLevelIcon( level )
                : _disconnectIcon;

            var lastConnectedTime =
                connected ? string.Empty
                : $"\n{xm4!.LastConnectedTime.Value:F}";

            _notifyIcon.Text = $"{AppName} {level}%{lastConnectedTime}";
        }

        const string AppName = "XM4 Battery Level";
        const string GithubProjectUrl = "https://github.com/nikvoronin/WmiPnp";
    }
}