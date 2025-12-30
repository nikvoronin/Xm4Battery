using WmiPnp.Xm4;

namespace Xm4Battery;

internal static partial class Program
{
    [STAThread]
    static int Main()
    {
        ApplicationConfiguration.Initialize();
        Application.SetHighDpiMode( HighDpiMode.PerMonitorV2 );
        Application.EnableVisualStyles();
        Application.SetColorMode( SystemColorMode.Dark );

        AppDomain.CurrentDomain.UnhandledException += ( _, e ) =>
            LogException( (Exception)e.ExceptionObject );

        Application.SetUnhandledExceptionMode( UnhandledExceptionMode.CatchException );
        Application.ThreadException += ( _, e ) => LogException( e.Exception );

    TryAgain:
        var xm4result = Xm4Entity.CreateDefault();
        if (xm4result.IsFailed) {
            var dialogResult =
                MessageBox.Show(
                    """
                    Please pair your headphones with this laptop first, then restart the application.

                    Try again?
                    """,
                    "Headphones Not Detected",
                    MessageBoxButtons.RetryCancel,
                    MessageBoxIcon.Exclamation );

            if (dialogResult == DialogResult.Retry)
                goto TryAgain;

            return (int)ErrorLevel.Xm4NotFound;
        }

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

    private const string ConnectCtxMenuItemName = nameof( ConnectCtxMenuItemName );
    private const string DisconnectCtxMenuItemName = nameof( DisconnectCtxMenuItemName );
    private const string LaunchAtStartupMenuItemName = nameof( LaunchAtStartupMenuItemName );
    private const string NotifyIcon_BatteryLevelTitle = "XM4 Battery Level";

    internal const string AppName = "Xm4Battery";
    private const string AppVersion = "5.12.30";
    private const string GithubProjectUrl = "https://github.com/nikvoronin/Xm4Battery";

    internal enum ErrorLevel
    {
        ExitOk = 0,
        Xm4NotFound = 1
    }
}