using System.Runtime.InteropServices;
using WmiPnp.Xm4;

namespace Xm4Battery;

internal static partial class Program
{
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
                //<= Xm4State.LowPowerLevel => Brushes.Magenta,
                //<= Xm4State.WarningLevel => Brushes.Cyan,
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
                <= DisconnectedLevel =>
                    state.BatteryLevel <= WarningPowerLevel ? "%" : "X",
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
            ih / 2 - sizeS.Height / 2 - .5f );

        return
            Icon.FromHandle(
                icoBitmap.GetHicon() );
    }

    private static readonly float _scalingFactor =
        new Func<float>( () => {
            using Graphics g = Graphics.FromHwnd( IntPtr.Zero );
            return g.DpiX / 96f;
        } )();

    private static readonly Font _notifyIconFont =
        new( "Segoe UI", 12.5f, FontStyle.Regular );

    private static readonly Pen Pens_WhiteSmokeW24 =
        new( Color.WhiteSmoke, _scalingFactor );

    private const int NotifyIconDefault_WidthPx = 20; // at 125% display scaling, 16px ~ 100%
    private const int NotifyIconDefault_HeightPx = 20;

    private const int DisconnectedLevel = 0;
    private const int CriticalPowerLevel = 10;
    private const int LowPowerLevel = 20;
    private const int WarningPowerLevel = 30;
    private const int FullPowerLevel = 100;

    [DllImport( "user32.dll", CharSet = CharSet.Unicode )]
    private static extern bool DestroyIcon( IntPtr handle );
}
