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
        Application.EnableVisualStyles();
#pragma warning disable WFO5001
        Application.SetColorMode( SystemColorMode.System );
#pragma warning restore WFO5001

        AppDomain.CurrentDomain.UnhandledException += ( _, e ) =>
            LogException( (Exception)e.ExceptionObject );

        Application.SetUnhandledExceptionMode( UnhandledExceptionMode.CatchException );
        Application.ThreadException += ( _, e ) => LogException( e.Exception );

        var xm4result = Xm4Entity.CreateDefault();
        if (xm4result.IsFailed)
            return (int)ErrorLevel.Xm4NotFound;

        Xm4Entity xm4 = xm4result.Value;

        var notifyIconCtrl =
            new NotifyIcon {
                Text = NotifyIcon_BatteryLevelTitle,
                Visible = true,
                Icon = CreateXmIcon(
                    new() {
                        BatteryLevel = DisconnectedLevel,
                        Connected = false
                    } ),
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

    private static readonly float _scalingFactor = DesktopScalingFactor();

    private static float DesktopScalingFactor()
    {
        using Graphics g = Graphics.FromHwnd( IntPtr.Zero );
        return g.DpiX / 96f;
    }

    private static readonly Font _notifyIconFont =
        new( "Segoe UI", 12.5f, FontStyle.Regular );

    private static readonly Pen Pens_WhiteSmokeW24 =
        new( Color.WhiteSmoke, _scalingFactor );

    private static Icon CreateXmIcon( Xm4State state )
    {
        int iw = (int)(NotifyIconDefault_WidthPx * _scalingFactor);
        int ih = (int)(NotifyIconDefault_HeightPx * _scalingFactor);

        using Bitmap icoBitmap = new( iw, ih );
        using var g = Graphics.FromImage( icoBitmap );
        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Default;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.Default;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SystemDefault;

        var uiBatteryLevel =
            state.Connected ? state.BatteryLevel
            : DisconnectedLevel;

        // icon background color
        var iconBackgroundBrush =
            uiBatteryLevel switch {
                <= DisconnectedLevel => Brushes.Transparent,
                <= CriticalPowerLevel => Brushes.Red,
                <= LowPowerLevel => Brushes.Orange,
                <= WarningPowerLevel => Brushes.Yellow,
                _ => Brushes.White // 40..100(F)
            };

        // icon text color
        var iconTextBrush =
            uiBatteryLevel switch {
                <= DisconnectedLevel => Brushes.WhiteSmoke,
                <= CriticalPowerLevel => Brushes.White,
                //<= LowPowerLevel => Brushes.Magenta,
                //<= WarningLevel => Brushes.Cyan,
                _ => Brushes.Black
            };

        g.FillRectangle(
            iconBackgroundBrush,
            0, 0, iw, ih );

        g.DrawRectangle(
            Pens_WhiteSmokeW24,
            0, 0, iw - 1, ih - 1 );

        // icon text: battery level or status
        var iconText =
            uiBatteryLevel switch {
                <= DisconnectedLevel => state.BatteryLevel <= WarningPowerLevel ? "%" : "X",
                <= CriticalPowerLevel => "!",
                FullPowerLevel => "F",
                _ => $"{state.BatteryLevel / 10}", // One digit of charge level 1..9
            };

        var sizeS =
            g.MeasureString(
                iconText,
                _notifyIconFont );

        g.DrawString(
            iconText,
            _notifyIconFont,
            iconTextBrush,
            iw / 2 - sizeS.Width / 2 + .5f,
            ih / 2 - sizeS.Height / 2 - 1 );

        return
            Icon.FromHandle(
                icoBitmap.GetHicon() );
    }

    private static void UpdateUi(
        Xm4Entity xm4,
        NotifyIcon notifyIconCtrl,
        Xm4State currentState )
    {
        ArgumentNullException.ThrowIfNull( xm4 );
        ArgumentNullException.ThrowIfNull( notifyIconCtrl );
        ArgumentNullException.ThrowIfNull( currentState );

        var items = notifyIconCtrl.ContextMenuStrip?.Items
            ?? throw new InvalidOperationException(
                "Can not get items of the context menu strip. Context menu is null." );

        if (items[ConnectCtxMenuItemName] is not null
            and var connectCtxMenuItem)
            connectCtxMenuItem.Enabled = !currentState.Connected;

        if (items[DisconnectCtxMenuItemName] is not null
            and var disconnectCtxMenuItemName)
            disconnectCtxMenuItemName.Enabled = currentState.Connected;

        var prevIcon = notifyIconCtrl.Icon;

        notifyIconCtrl.Icon =
            CreateXmIcon( currentState );

        if (prevIcon is not null)
            DestroyIcon( prevIcon.Handle );

        // a race condition may occur and it happens sometimes
        // somewhere between getting state and getting last connected time
        var at =
            (!currentState.Connected
                && xm4.LastConnectedTime.ValueOrDefault is DateTime lastConnectedTime
                && lastConnectedTime > DateTime.MinValue)
            ? $"\n{lastConnectedTime:F}"
            : string.Empty;

        notifyIconCtrl.Text =
            $"{NotifyIcon_BatteryLevelTitle} ⚡{currentState.BatteryLevel}%{at}";
    }

    private static void LogException( Exception exception ) =>
        File.AppendAllText(
            $"{AppName}_{AppVersion}_exceptions.log",
            $"{DateTime.UtcNow:u} {exception}\n" );

    const string ConnectCtxMenuItemName = nameof( ConnectCtxMenuItemName );
    const string DisconnectCtxMenuItemName = nameof( DisconnectCtxMenuItemName );
    const int NotifyIconDefault_WidthPx = 20;
    const int NotifyIconDefault_HeightPx = 20;

    const int DisconnectedLevel = 0;
    const int CriticalPowerLevel = 10;
    const int LowPowerLevel = 20;
    const int WarningPowerLevel = 30;
    const int FullPowerLevel = 100;
    const string NotifyIcon_BatteryLevelTitle = "XM4 Battery Level";

    const string AppName = "Xm4Battery";
    const string AppVersion = "5.7.7-rc1";
    const string GithubProjectUrl = "https://github.com/nikvoronin/Xm4Battery";

    internal enum ErrorLevel
    {
        ExitOk = 0,
        Xm4NotFound = 1
    }

    [DllImport( "user32.dll", CharSet = CharSet.Unicode )]
    static extern bool DestroyIcon( IntPtr handle );
}