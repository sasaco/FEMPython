using FarPoint.Win.Spread.CellType;
using System;
using System.Globalization;

namespace FrameWebforCS.components.input
{
    /// <summary>
    /// Formats the same decimal value that the printer reads from saved JSON.
    /// Spread binds float fields directly, whose binary precision can otherwise
    /// change a printed midpoint or add spurious trailing digits.
    /// </summary>
    internal sealed class PrintNumberCellType : GeneralCellType
    {
        private readonly string _format;
        private readonly bool _useDefaultAt999;

        internal PrintNumberCellType(string format, bool useDefaultAt999 = false)
        {
            _format = format;
            _useDefaultAt999 = useDefaultAt999;
        }

        public override string Format(object value)
        {
            double number;
            if (value is float single)
                number = double.Parse(single.ToString("R", CultureInfo.InvariantCulture),
                    CultureInfo.InvariantCulture);
            else if (value is double asDouble)
                number = asDouble;
            else
                return base.Format(value);

            if (double.IsNaN(number)) return string.Empty;
            return _useDefaultAt999 && number >= 999
                ? number.ToString(CultureInfo.CurrentCulture)
                : number.ToString(_format, CultureInfo.CurrentCulture);
        }
    }
}
