using System.Globalization;

namespace FrameWebforCS.components.result;

internal static class ResultPrintNumberFormat
{
    internal static string Displacement(double value) =>
        value.ToString("F4", CultureInfo.CurrentCulture);

    internal static string Force(double value) =>
        value.ToString("F2", CultureInfo.CurrentCulture);

    internal static string Station(double value) =>
        value.ToString("F3", CultureInfo.CurrentCulture);
}
