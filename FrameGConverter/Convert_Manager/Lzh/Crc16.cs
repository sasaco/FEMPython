namespace Convert_Manager.Lzh
{
    /// <summary>CRC-16/ARC (reflected polynomial 0xA001, initial value 0) used by LHA headers.</summary>
    internal static class Crc16
    {
        private static readonly ushort[] Table = CreateTable();

        public static ushort Compute(byte[] data)
        {
            ushort crc = 0;
            foreach (byte value in data)
                crc = (ushort)((crc >> 8) ^ Table[(crc ^ value) & 0xFF]);
            return crc;
        }

        private static ushort[] CreateTable()
        {
            var table = new ushort[256];
            for (int i = 0; i < table.Length; i++)
            {
                int crc = i;
                for (int bit = 0; bit < 8; bit++)
                    crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xA001 : crc >> 1;
                table[i] = (ushort)crc;
            }
            return table;
        }
    }
}
