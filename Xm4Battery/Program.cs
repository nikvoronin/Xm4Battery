using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using WmiPnp.Xm4;

namespace Xm4Battery;

internal static class Program
{
    [STAThread]
    static int Main()
    {
        ApplicationConfiguration.Initialize();
        Application.SetHighDpiMode( HighDpiMode.PerMonitorV2 );

        AppDomain.CurrentDomain.UnhandledException += ( _, e ) =>
            LogException( (Exception)e.ExceptionObject );

        Application.SetUnhandledExceptionMode( UnhandledExceptionMode.CatchException );
        Application.ThreadException += ( _, e ) => LogException( e.Exception );

        var xm4result = Xm4Entity.Create();
        if (xm4result.IsFailed)
            return (int)ErrorLevel.Xm4NotFound;

        Xm4Entity xm4 = xm4result.Value;

        var notifyIconCtrl =
            new NotifyIcon {
                Text = NotifyIcon_BatteryLevelTitle,
                Visible = true,
                Icon = CreateIconForLevel( DisconnectedLevel ),
                ContextMenuStrip = CreateContextMenu()
            };

        var statePoller = new Xm4Poller(
            xm4,
            ( _, state ) => UpdateUi(
                xm4,
                notifyIconCtrl,
                state ) );

        statePoller.Start();
        Application.Run();
        statePoller.Stop();

        notifyIconCtrl.Visible = false;
        var prevIcon = notifyIconCtrl.Icon;
        notifyIconCtrl.Dispose();
        DestroyIcon( prevIcon.Handle );

        return (int)ErrorLevel.ExitOk;
    }

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

            new ToolStripSeparator() {
                Visible = runasAdmin
            },

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

        return contextMenu;
    }

    static readonly Font _notifyIconFont =
        new( "Segoe UI", 124, FontStyle.Regular );

    private static Icon CreateIconForLevel( int level )
    {
        const int iw = NotifyIconDefault_WidthPx;
        const int ih = NotifyIconDefault_HeightPx;

        using Bitmap icoBitmap = new( iw, ih );
        using var g = Graphics.FromImage( icoBitmap );
        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SystemDefault;

        // icon background color
        var iconBackgroundBrush =
            level switch {
                <= DisconnectedLevel => Brushes.Transparent,
                <= 10 => Brushes.Red,
                <= 20 => Brushes.Orange,
                <= 30 => Brushes.Yellow,
                _ => Brushes.White // 40..100(F)
            };

        // icon text color
        var iconTextBrush =
            level switch {
                <= DisconnectedLevel => Brushes.WhiteSmoke,
                //<= 10 => Brushes.Magenta,
                //<= 20 => Brushes.Cyan,
                _ => Brushes.Black
            };

        g.FillRectangle(
            iconBackgroundBrush,
            0, 0, iw, ih );

        g.DrawRectangle(
            Pens_WhiteSmokeW24,
            0, 0, iw, ih );

        // icon text: battery level or status
        var iconText =
            level switch {
                <= DisconnectedLevel => "X",
                <= 10 => "!",
                100 => "F", // Full charged
                _ => level.ToString()[..^1], // One digit of charge level 1..9
            };

        var sizeS =
            g.MeasureString(
                iconText,
                _notifyIconFont );

        g.DrawString(
            iconText,
            _notifyIconFont,
            iconTextBrush,
            iw / 2 - sizeS.Width / 2,
            ih / 2 - sizeS.Height / 2 );

        return
            Icon.FromHandle(
                icoBitmap.GetHicon() );
    }

    private static void UpdateUi(
        Xm4Entity xm4,
        NotifyIcon notifyIconCtrl,
        Xm4State currentState )
    {
        var items = notifyIconCtrl.ContextMenuStrip!.Items;
        items[ConnectCtxMenuItemName]!.Enabled = !currentState.Connected;
        items[DisconnectCtxMenuItemName]!.Enabled = currentState.Connected;

        var prevIcon = notifyIconCtrl.Icon;

        notifyIconCtrl.Icon =
            CreateIconForLevel(
                currentState.Connected ? currentState.BatteryLevel
                : DisconnectedLevel );

        if (prevIcon is not null)
            DestroyIcon( prevIcon.Handle );

        var at =
            currentState.Connected ? string.Empty
            : $"\n{xm4!.LastConnectedTime.Value:F}";

        notifyIconCtrl.Text =
            $"{NotifyIcon_BatteryLevelTitle} ⚡{currentState.BatteryLevel}%{at}";
    }

    private static void LogException( Exception exception ) =>
        File.AppendAllText(
            $"{AppName}_{AppVersion}_exceptions.log",
            $"{DateTime.UtcNow:u} {exception}\n" );

    static readonly Pen Pens_WhiteSmokeW24 =
        new( Color.WhiteSmoke, 24f );

    const string ConnectCtxMenuItemName = nameof( ConnectCtxMenuItemName );
    const string DisconnectCtxMenuItemName = nameof( DisconnectCtxMenuItemName );
    const int NotifyIconDefault_WidthPx = 256;
    const int NotifyIconDefault_HeightPx = 256;

    const int DisconnectedLevel = 0;
    const string NotifyIcon_BatteryLevelTitle = "XM4 Battery Level";

    const string AppName = "Xm4Battery";
    const string AppVersion = "4.7.25";
    const string GithubProjectUrl = "https://github.com/nikvoronin/WmiPnp";

    internal enum ErrorLevel
    {
        ExitOk = 0,
        Xm4NotFound = 1
    }

    [DllImport( "user32.dll", CharSet = CharSet.Unicode )]
    static extern bool DestroyIcon( IntPtr handle );
}
