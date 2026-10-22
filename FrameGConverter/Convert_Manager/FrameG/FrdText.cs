using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Convert_Manager.FrameG
{
    /// <summary>Shift-JIS text and number formatting for FrameG's fixed-width and tab-separated files.</summary>
    internal static class FrdText
    {
        private static Encoding shiftJis;

        /// <summary>Shift-JIS that throws instead of writing '?' for characters it cannot encode.</summary>
        public static Encoding ShiftJis
        {
            get
            {
                if (shiftJis == null)
                {
                    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                    shiftJis = Encoding.GetEncoding(932, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
                }
                return shiftJis;
            }
        }

        public static bool CanEncode(string text)
        {
            try
            {
                ShiftJis.GetByteCount(text);
                return true;
            }
            catch (EncoderFallbackException)
            {
                return false;
            }
        }

        public static int ByteCount(string text) => ShiftJis.GetByteCount(text);

        /// <summary>Cuts text to at most maxBytes Shift-JIS bytes without splitting a double-byte character.</summary>
        public static string Truncate(string text, int maxBytes)
        {
            if (ByteCount(text) <= maxBytes)
                return text;
            var result = new StringBuilder();
            int bytes = 0;
            var characters = StringInfo.GetTextElementEnumerator(text);
            while (characters.MoveNext())
            {
                string element = characters.GetTextElement();
                int size = ByteCount(element);
                if (bytes + size > maxBytes)
                    break;
                result.Append(element);
                bytes += size;
            }
            return result.ToString();
        }

        /// <summary>The shortest invariant text that reads back as the same double ("-0" becomes "0").</summary>
        public static string Number(double value) =>
            (value == 0 ? 0.0 : value).ToString("R", CultureInfo.InvariantCulture);

        /// <summary>
        /// Formats a number to fit maxChars, with a leading space for non-negative values when signSpace
        /// is set (FrameG's style). Drops significant digits only when the shortest form does not fit;
        /// returns null when not even one significant digit fits.
        /// </summary>
        public static string FitNumber(double value, int maxChars, bool signSpace, out bool reduced)
        {
            reduced = false;
            if (value == 0)
                value = 0;
            string prefix = signSpace && value >= 0 ? " " : "";
            string text = prefix + Number(value);
            if (text.Length <= maxChars)
                return text;
            for (int digits = 15; digits >= 1; digits--)
            {
                text = prefix + value.ToString("G" + digits.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
                if (text.Length > maxChars)
                    continue;
                reduced = double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture) != value;
                return text;
            }
            return null;
        }

        public static string Integer(int value) => value.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>A fixed-width record: each field is left-aligned and padded with spaces to its byte width.</summary>
    internal sealed class FrdRecord
    {
        private readonly List<byte> bytes = new List<byte>();

        public int Length => bytes.Count;

        public FrdRecord Field(string text, int width)
        {
            byte[] encoded = FrdText.ShiftJis.GetBytes(text ?? "");
            if (encoded.Length > width)
                throw new InvalidOperationException($"\"{text}\" does not fit a {width}-byte field.");
            bytes.AddRange(encoded);
            for (int i = encoded.Length; i < width; i++)
                bytes.Add((byte)' ');
            return this;
        }

        public FrdRecord Blank(int width) => Field("", width);

        public FrdRecord Raw(params byte[] values)
        {
            bytes.AddRange(values);
            return this;
        }

        public void AppendTo(List<byte> output, int expectedLength)
        {
            if (bytes.Count != expectedLength)
                throw new InvalidOperationException($"A record has {bytes.Count} bytes instead of {expectedLength}.");
            output.AddRange(bytes);
        }
    }
}
