using System.Text;
using Convert_Manager.Lzh;
using SevenZipExtractor;
using Xunit;

namespace Convert_Manager.Tests;

public class LzhArchiveWriterTests
{
    private static readonly DateTimeOffset Time = new(2026, 10, 22, 1, 2, 3, TimeSpan.Zero);

    public static TheoryData<string, byte[]> Inputs()
    {
        var random = new Random(12345);
        var noise = new byte[200_000];
        random.NextBytes(noise);
        var skewed = new byte[150_000];
        for (int i = 0; i < skewed.Length; i++)
            skewed[i] = (byte)(random.Next(4) == 0 ? random.Next(256) : "FrameG\r\n"[random.Next(8)]);
        var text = new StringBuilder();
        for (int i = 0; i < 3000; i++)
            text.Append(i).Append('\t').Append((i * 0.125).ToString("0.000")).Append(" \r\n");
        return new TheoryData<string, byte[]>
        {
            { "empty", Array.Empty<byte>() },
            { "one", new byte[] { 0x41 } },
            { "repetitive", Encoding.ASCII.GetBytes(string.Concat(Enumerable.Repeat("abc", 40_000))) },
            { "zeros", new byte[70_000] },
            { "noise", noise },
            { "skewed", skewed },
            { "text", Encoding.ASCII.GetBytes(text.ToString()) },
        };
    }

    [Theory]
    [MemberData(nameof(Inputs))]
    public void SevenZipExtractsWhatWasWritten(string name, byte[] data)
    {
        // Compress every input, including ones -lh5- cannot shrink, so the encoder itself is exercised.
        byte[] archive = LzhArchiveWriter.Write(new[] { new LzhEntry(name + ".tmp", data) }, Time, alwaysCompress: true);

        var entry = Assert.Single(LzhTestReader.Read(archive));
        Assert.Equal(data.Length == 0 ? "-lh0-" : "-lh5-", entry.Method);
        Assert.Equal(name + ".tmp", entry.Name);
        Assert.Equal(data.Length, entry.OriginalSize);
        Assert.Equal(LzhTestReader.Crc16(data), entry.Crc);
        Assert.Equal(data, Assert.Single(Extract(archive)).Value);
    }

    [Fact]
    public void LargeInputsUseSeveralLh5Blocks()
    {
        var data = (byte[])Inputs().Single(row => (string)row[0] == "skewed")[1];
        byte[] archive = LzhArchiveWriter.Write(new[] { new LzhEntry("skewed.tmp", data) }, Time);

        var entry = Assert.Single(LzhTestReader.Read(archive));
        Assert.Equal("-lh5-", entry.Method);
        // The first 16-bit block size covers fewer symbols than the whole input needs.
        Assert.Equal(16384, entry.Packed[0] << 8 | entry.Packed[1]);
        Assert.Equal(data, Assert.Single(Extract(archive)).Value);
    }

    [Fact]
    public void HeadersMatchTheLayoutFrameGWrites()
    {
        byte[] small = Encoding.ASCII.GetBytes("0\r\n");
        byte[] text = Encoding.ASCII.GetBytes(string.Concat(Enumerable.Repeat("1\t0.000 \t0.000 \r\n", 20)));
        byte[] archive = LzhArchiveWriter.Write(new[] { new LzhEntry("$12.txt", small), new LzhEntry("$1.txt", text) }, Time);

        var entries = LzhTestReader.Read(archive);
        Assert.Equal(new[] { "$12.txt", "$1.txt" }, entries.Select(entry => entry.Name));
        Assert.Equal("-lh0-", entries[0].Method);
        Assert.Equal(small, entries[0].Packed);
        Assert.Equal("-lh5-", entries[1].Method);
        Assert.True(entries[1].Packed.Length < text.Length);
        foreach (var entry in entries)
        {
            Assert.Equal(25 + entry.Name.Length, entry.HeaderSize);
            Assert.True(entry.ChecksumMatches);
            Assert.Equal(0, entry.Attribute);
            Assert.Equal(0, entry.Level);
            // FrameG stores Unix epoch seconds, not a DOS date and time.
            Assert.Equal((uint)Time.ToUnixTimeSeconds(), entry.Time);
            Assert.Equal(new byte[] { 0x20, 0x00, 0x00 }, entry.Extension);
        }
        Assert.Equal(0, archive[^1]);
        Assert.Equal(archive.Length - 1, entries[^1].End);
    }

    [Theory]
    [InlineData("4_2-FrameG.frd")]
    [InlineData("バネ連衡あり.frd")]
    [InlineData("バネ連衡なし.frd")]
    [InlineData("杭2こ.lzh")]
    public void RepackedSampleArchivesExtractUnchanged(string fileName)
    {
        byte[] original = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "TestData", fileName));
        var files = Extract(original);
        var originalEntries = LzhTestReader.Read(original);

        byte[] repacked = LzhArchiveWriter.Write(
            files.Select(file => new LzhEntry(file.Key, file.Value)).ToArray(), Time);

        var entries = LzhTestReader.Read(repacked);
        Assert.Equal(originalEntries.Select(entry => entry.Name), entries.Select(entry => entry.Name));
        Assert.Equal(originalEntries.Select(entry => entry.Crc), entries.Select(entry => entry.Crc));
        // Entries FrameG stored uncompressed are also stored here: lh5 does not make them smaller.
        Assert.All(originalEntries.Zip(entries), pair =>
            Assert.True(pair.First.Method == "-lh5-" || pair.Second.Method == "-lh0-", pair.First.Name));
        var extracted = Extract(repacked);
        Assert.Equal(files.Keys, extracted.Keys);
        foreach (var file in files)
            Assert.Equal(file.Value, extracted[file.Key]);
    }

    [Fact]
    public void ACorruptedStreamIsCaughtOnlyByTheHeaderCrc()
    {
        byte[] data = Encoding.ASCII.GetBytes(string.Concat(Enumerable.Range(0, 2000).Select(i => $"{i}\t{i * 3}\r\n")));
        byte[] archive = LzhArchiveWriter.Write(new[] { new LzhEntry("data.tmp", data) }, Time);
        var entry = Assert.Single(LzhTestReader.Read(archive));
        archive[entry.DataOffset + entry.Packed.Length / 2] ^= 0x5A;

        // SevenZipExtractor (the forward converter's reader) returns the damaged bytes without an
        // error, so the exporter tests compare the CRC of what they extract with the header.
        byte[]? extracted = null;
        Exception? error = Record.Exception(() => extracted = Extract(archive).Single().Value);
        Assert.True(error != null || LzhTestReader.Crc16(extracted!) != entry.Crc);
    }

    internal static Dictionary<string, byte[]> Extract(byte[] archive)
    {
        var result = new Dictionary<string, byte[]>();
        using var stream = new MemoryStream(archive);
        using var file = new ArchiveFile(stream, SevenZipFormat.Lzh);
        foreach (Entry entry in file.Entries)
        {
            using var output = new MemoryStream();
            entry.Extract(output);
            result.Add(entry.FileName, output.ToArray());
        }
        return result;
    }
}

/// <summary>Independent level-0 LHA header reader used to check what the writer produced.</summary>
internal static class LzhTestReader
{
    internal sealed record Header(string Name, string Method, int HeaderSize, bool ChecksumMatches,
        int OriginalSize, uint Time, int Attribute, int Level, ushort Crc, byte[] Extension,
        int DataOffset, byte[] Packed, int End);

    internal static List<Header> Read(byte[] archive)
    {
        var result = new List<Header>();
        int offset = 0;
        while (archive[offset] != 0)
        {
            int size = archive[offset];
            int sum = 0;
            for (int i = offset + 2; i < offset + 2 + size; i++)
                sum += archive[i];
            int nameLength = archive[offset + 21];
            int packed = BitConverter.ToInt32(archive, offset + 7);
            int dataOffset = offset + 2 + size;
            result.Add(new Header(
                Encoding.ASCII.GetString(archive, offset + 22, nameLength),
                Encoding.ASCII.GetString(archive, offset + 2, 5),
                size,
                (byte)sum == archive[offset + 1],
                BitConverter.ToInt32(archive, offset + 11),
                BitConverter.ToUInt32(archive, offset + 15),
                archive[offset + 19],
                archive[offset + 20],
                BitConverter.ToUInt16(archive, offset + 22 + nameLength),
                archive[(offset + 24 + nameLength)..dataOffset],
                dataOffset,
                archive[dataOffset..(dataOffset + packed)],
                dataOffset + packed));
            offset = dataOffset + packed;
        }
        return result;
    }

    // Bitwise CRC-16/ARC, independent of the table-driven implementation under test.
    internal static ushort Crc16(byte[] data)
    {
        int crc = 0;
        foreach (byte value in data)
        {
            crc ^= value;
            for (int bit = 0; bit < 8; bit++)
                crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xA001 : crc >> 1;
        }
        return (ushort)crc;
    }
}
