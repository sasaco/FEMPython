using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Convert_Manager.Lzh
{
    internal sealed class LzhEntry
    {
        public LzhEntry(string name, byte[] data)
        {
            Name = name;
            Data = data;
        }

        public string Name { get; }
        public byte[] Data { get; }
    }

    /// <summary>
    /// Writes an LHA archive with level-0 headers laid out as FrameG writes them: the time field
    /// holds Unix epoch seconds, attribute 0, the extension bytes 20 00 00 after the CRC, and a
    /// single 0x00 terminator. An entry is stored (-lh0-) when -lh5- does not make it smaller.
    /// </summary>
    internal static class LzhArchiveWriter
    {
        private const int HeaderFixedSize = 25;   // header bytes after the size and checksum bytes, excluding the name
        private const int MaxHeaderSize = byte.MaxValue;
        private static readonly byte[] Extension = { 0x20, 0x00, 0x00 };

        public static byte[] Write(IReadOnlyList<LzhEntry> entries, DateTimeOffset time) =>
            Write(entries, time, alwaysCompress: false);

        // alwaysCompress keeps -lh5- even when it is larger, so tests can exercise the encoder on any input.
        internal static byte[] Write(IReadOnlyList<LzhEntry> entries, DateTimeOffset time, bool alwaysCompress)
        {
            long seconds = time.ToUnixTimeSeconds();
            if (seconds < 0 || seconds > uint.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(time), "LZH の時刻は 1970 年以降の範囲で指定してください。");

            using (var output = new MemoryStream())
            {
                foreach (LzhEntry entry in entries)
                    WriteEntry(output, entry, (uint)seconds, alwaysCompress);
                output.WriteByte(0);
                return output.ToArray();
            }
        }

        private static void WriteEntry(Stream output, LzhEntry entry, uint time, bool alwaysCompress)
        {
            byte[] name = Encoding.ASCII.GetBytes(entry.Name);
            if (name.Length == 0 || Encoding.ASCII.GetString(name) != entry.Name)
                throw new ArgumentException($"LZH のファイル名 {entry.Name} は ASCII で指定してください。");
            int headerSize = HeaderFixedSize + name.Length;
            if (headerSize > MaxHeaderSize)
                throw new ArgumentException($"LZH のファイル名 {entry.Name} が長すぎます。");

            byte[] packed = Lh5Encoder.Encode(entry.Data);
            string method = "-lh5-";
            if (packed.Length >= entry.Data.Length && !(alwaysCompress && entry.Data.Length > 0))
            {
                packed = entry.Data;
                method = "-lh0-";
            }

            var header = new byte[2 + headerSize];
            header[0] = (byte)headerSize;
            Encoding.ASCII.GetBytes(method, 0, method.Length, header, 2);
            WriteUInt32(header, 7, (uint)packed.Length);
            WriteUInt32(header, 11, (uint)entry.Data.Length);
            WriteUInt32(header, 15, time);
            header[19] = 0;   // attribute
            header[20] = 0;   // header level
            header[21] = (byte)name.Length;
            Array.Copy(name, 0, header, 22, name.Length);
            ushort crc = Crc16.Compute(entry.Data);
            header[22 + name.Length] = (byte)crc;
            header[23 + name.Length] = (byte)(crc >> 8);
            Array.Copy(Extension, 0, header, 24 + name.Length, Extension.Length);

            int checksum = 0;
            for (int i = 2; i < header.Length; i++)
                checksum += header[i];
            header[1] = (byte)checksum;

            output.Write(header, 0, header.Length);
            output.Write(packed, 0, packed.Length);
        }

        private static void WriteUInt32(byte[] buffer, int offset, uint value)
        {
            buffer[offset] = (byte)value;
            buffer[offset + 1] = (byte)(value >> 8);
            buffer[offset + 2] = (byte)(value >> 16);
            buffer[offset + 3] = (byte)(value >> 24);
        }
    }
}
