using System;
using System.Collections.Generic;
using System.IO;

namespace Convert_Manager.Lzh
{
    /// <summary>
    /// LHA -lh5- compressor: LZSS over an 8 KiB window followed by static Huffman blocks
    /// (the C/P/T tables of the original LHA format, decoded MSB first).
    /// </summary>
    internal static class Lh5Encoder
    {
        private const int DictionaryBits = 13;
        // A distance is stored as distance - 1 in DictionaryBits bits; stay one byte inside the window.
        private const int MaxDistance = (1 << DictionaryBits) - 1;
        private const int MinMatch = 3;
        private const int MaxMatch = 256;
        private const int LiteralCount = 256;
        private const int CharacterCount = LiteralCount + MaxMatch - MinMatch + 1; // NC
        private const int CharacterBits = 9;                                        // CBIT
        private const int PositionCount = DictionaryBits + 1;                       // NP
        private const int PositionBits = 4;                                         // PBIT
        private const int TableCount = 19;                                          // NT
        private const int TableBits = 5;                                            // TBIT
        private const int TableSpecialIndex = 3;
        private const int MaxCodeLength = 16;
        private const int BlockTokens = 16384;
        private const int HashBits = 15;
        private const int MaxChain = 256;

        private struct Token
        {
            public int Code;     // literal byte, or LiteralCount + length - MinMatch
            public int Distance; // match distance (1..MaxDistance); unused for literals
        }

        public static byte[] Encode(byte[] data)
        {
            List<Token> tokens = Tokenize(data);
            var writer = new BitWriter();
            for (int start = 0; start < tokens.Count; start += BlockTokens)
                WriteBlock(writer, tokens, start, Math.Min(BlockTokens, tokens.Count - start));
            return writer.ToArray();
        }

        private static List<Token> Tokenize(byte[] data)
        {
            var tokens = new List<Token>();
            var head = new int[1 << HashBits];
            for (int i = 0; i < head.Length; i++)
                head[i] = -1;
            var previous = new int[data.Length];

            int position = 0;
            while (position < data.Length)
            {
                int bestLength = 0, bestDistance = 0;
                if (position + MinMatch <= data.Length)
                {
                    int limit = Math.Min(MaxMatch, data.Length - position);
                    int candidate = head[Hash(data, position)];
                    for (int chain = 0; candidate >= 0 && position - candidate <= MaxDistance && chain < MaxChain; chain++)
                    {
                        int length = 0;
                        while (length < limit && data[candidate + length] == data[position + length])
                            length++;
                        if (length > bestLength)
                        {
                            bestLength = length;
                            bestDistance = position - candidate;
                            if (length == limit)
                                break;
                        }
                        candidate = previous[candidate];
                    }
                }

                if (bestLength >= MinMatch)
                {
                    tokens.Add(new Token { Code = LiteralCount + bestLength - MinMatch, Distance = bestDistance });
                    for (int i = 0; i < bestLength; i++)
                        Insert(data, head, previous, position + i);
                    position += bestLength;
                }
                else
                {
                    tokens.Add(new Token { Code = data[position] });
                    Insert(data, head, previous, position);
                    position++;
                }
            }
            return tokens;
        }

        private static int Hash(byte[] data, int position) =>
            ((data[position] << 10) ^ (data[position + 1] << 5) ^ data[position + 2]) & ((1 << HashBits) - 1);

        private static void Insert(byte[] data, int[] head, int[] previous, int position)
        {
            if (position + MinMatch > data.Length)
            {
                previous[position] = -1;
                return;
            }
            int hash = Hash(data, position);
            previous[position] = head[hash];
            head[hash] = position;
        }

        private static void WriteBlock(BitWriter writer, List<Token> tokens, int start, int count)
        {
            var characterFrequency = new int[CharacterCount];
            var positionFrequency = new int[PositionCount];
            for (int i = start; i < start + count; i++)
            {
                characterFrequency[tokens[i].Code]++;
                if (tokens[i].Code >= LiteralCount)
                    positionFrequency[PositionSymbol(tokens[i].Distance - 1)]++;
            }

            writer.Write(16, count);

            int[] characterLengths;
            int singleCharacter = SingleSymbol(characterFrequency);
            if (singleCharacter < 0)
            {
                characterLengths = HuffmanLengths(characterFrequency);
                int[] tableFrequency = CountTableFrequencies(characterLengths);
                int[] tableLengths;
                int singleTable = SingleSymbol(tableFrequency);
                if (singleTable < 0)
                {
                    tableLengths = HuffmanLengths(tableFrequency);
                    WriteLengths(writer, tableLengths, TableBits, TableSpecialIndex);
                }
                else
                {
                    tableLengths = new int[TableCount];
                    writer.Write(TableBits, 0);
                    writer.Write(TableBits, singleTable);
                }
                WriteCharacterLengths(writer, characterLengths, tableLengths, CanonicalCodes(tableLengths));
            }
            else
            {
                characterLengths = new int[CharacterCount];
                writer.Write(TableBits, 0);
                writer.Write(TableBits, 0);
                writer.Write(CharacterBits, 0);
                writer.Write(CharacterBits, singleCharacter);
            }

            int[] positionLengths;
            int singlePosition = SingleSymbol(positionFrequency);
            if (singlePosition < 0)
            {
                positionLengths = HuffmanLengths(positionFrequency);
                WriteLengths(writer, positionLengths, PositionBits, -1);
            }
            else
            {
                positionLengths = new int[PositionCount];
                writer.Write(PositionBits, 0);
                writer.Write(PositionBits, singlePosition);
            }

            int[] characterCodes = CanonicalCodes(characterLengths);
            int[] positionCodes = CanonicalCodes(positionLengths);
            for (int i = start; i < start + count; i++)
            {
                Token token = tokens[i];
                writer.Write(characterLengths[token.Code], characterCodes[token.Code]);
                if (token.Code < LiteralCount)
                    continue;
                int stored = token.Distance - 1;
                int symbol = PositionSymbol(stored);
                writer.Write(positionLengths[symbol], positionCodes[symbol]);
                if (symbol > 1)
                    writer.Write(symbol - 1, stored - (1 << (symbol - 1)));
            }
        }

        // 0 for a stored distance of 0, otherwise the bit length of the stored distance.
        private static int PositionSymbol(int stored)
        {
            int symbol = 0;
            while (stored > 0)
            {
                symbol++;
                stored >>= 1;
            }
            return symbol;
        }

        // The symbol when at most one symbol is used (0 when none is), otherwise -1.
        private static int SingleSymbol(int[] frequency)
        {
            int found = -1;
            for (int i = 0; i < frequency.Length; i++)
            {
                if (frequency[i] == 0)
                    continue;
                if (found >= 0)
                    return -1;
                found = i;
            }
            return Math.Max(found, 0);
        }

        private static int[] CountTableFrequencies(int[] characterLengths)
        {
            var frequency = new int[TableCount];
            int n = UsedLength(characterLengths);
            int i = 0;
            while (i < n)
            {
                int length = characterLengths[i++];
                if (length != 0)
                {
                    frequency[length + 2]++;
                    continue;
                }
                int run = 1;
                while (i < n && characterLengths[i] == 0)
                {
                    i++;
                    run++;
                }
                if (run <= 2)
                    frequency[0] += run;
                else if (run <= 18)
                    frequency[1]++;
                else if (run == 19)
                {
                    frequency[0]++;
                    frequency[1]++;
                }
                else
                    frequency[2]++;
            }
            return frequency;
        }

        private static void WriteCharacterLengths(BitWriter writer, int[] characterLengths, int[] tableLengths, int[] tableCodes)
        {
            int n = UsedLength(characterLengths);
            writer.Write(CharacterBits, n);
            int i = 0;
            while (i < n)
            {
                int length = characterLengths[i++];
                if (length != 0)
                {
                    writer.Write(tableLengths[length + 2], tableCodes[length + 2]);
                    continue;
                }
                int run = 1;
                while (i < n && characterLengths[i] == 0)
                {
                    i++;
                    run++;
                }
                if (run <= 2)
                {
                    for (int k = 0; k < run; k++)
                        writer.Write(tableLengths[0], tableCodes[0]);
                }
                else if (run <= 18)
                {
                    writer.Write(tableLengths[1], tableCodes[1]);
                    writer.Write(4, run - 3);
                }
                else if (run == 19)
                {
                    writer.Write(tableLengths[0], tableCodes[0]);
                    writer.Write(tableLengths[1], tableCodes[1]);
                    writer.Write(4, 15);
                }
                else
                {
                    writer.Write(tableLengths[2], tableCodes[2]);
                    writer.Write(CharacterBits, run - 20);
                }
            }
        }

        // Lengths of the T and P tables: 3 bits, or 7 followed by a unary extension.
        private static void WriteLengths(BitWriter writer, int[] lengths, int countBits, int specialIndex)
        {
            int n = UsedLength(lengths);
            writer.Write(countBits, n);
            int i = 0;
            while (i < n)
            {
                int length = lengths[i++];
                if (length <= 6)
                    writer.Write(3, length);
                else
                {
                    writer.Write(3, 7);
                    for (int extra = 7; extra < length; extra++)
                        writer.Write(1, 1);
                    writer.Write(1, 0);
                }
                if (i != specialIndex)
                    continue;
                int zeros = 0;
                while (zeros < 3 && i + zeros < n && lengths[i + zeros] == 0)
                    zeros++;
                writer.Write(2, zeros);
                i += zeros;
            }
        }

        private static int UsedLength(int[] lengths)
        {
            int n = lengths.Length;
            while (n > 0 && lengths[n - 1] == 0)
                n--;
            return n;
        }

        // Huffman code lengths limited to MaxCodeLength; requires at least two used symbols.
        private static int[] HuffmanLengths(int[] frequency)
        {
            var weights = new long[frequency.Length];
            for (int i = 0; i < frequency.Length; i++)
                weights[i] = frequency[i];
            while (true)
            {
                int[] lengths = TreeDepths(weights);
                bool fits = true;
                foreach (int length in lengths)
                    fits &= length <= MaxCodeLength;
                if (fits)
                    return lengths;
                // Flatten the distribution and rebuild; used symbols keep a weight of at least 1.
                for (int i = 0; i < weights.Length; i++)
                    if (weights[i] > 0)
                        weights[i] = (weights[i] + 1) / 2;
            }
        }

        private static int[] TreeDepths(long[] weights)
        {
            var nodeWeight = new List<long>();
            var nodeParent = new List<int>();
            var leafNode = new int[weights.Length];
            var active = new List<int>();
            for (int symbol = 0; symbol < weights.Length; symbol++)
            {
                leafNode[symbol] = -1;
                if (weights[symbol] == 0)
                    continue;
                leafNode[symbol] = nodeWeight.Count;
                active.Add(nodeWeight.Count);
                nodeWeight.Add(weights[symbol]);
                nodeParent.Add(-1);
            }

            while (active.Count > 1)
            {
                int first = TakeLightest(active, nodeWeight);
                int second = TakeLightest(active, nodeWeight);
                int parent = nodeWeight.Count;
                nodeWeight.Add(nodeWeight[first] + nodeWeight[second]);
                nodeParent.Add(-1);
                nodeParent[first] = parent;
                nodeParent[second] = parent;
                active.Add(parent);
            }

            var lengths = new int[weights.Length];
            for (int symbol = 0; symbol < weights.Length; symbol++)
            {
                int depth = 0;
                for (int node = leafNode[symbol]; node >= 0 && nodeParent[node] >= 0; node = nodeParent[node])
                    depth++;
                lengths[symbol] = depth;
            }
            return lengths;
        }

        private static int TakeLightest(List<int> active, List<long> nodeWeight)
        {
            int best = 0;
            for (int i = 1; i < active.Count; i++)
                if (nodeWeight[active[i]] < nodeWeight[active[best]])
                    best = i;
            int node = active[best];
            active.RemoveAt(best);
            return node;
        }

        // Canonical codes in the order LHA decoders rebuild them: by length, then by symbol.
        private static int[] CanonicalCodes(int[] lengths)
        {
            var codes = new int[lengths.Length];
            int code = 0;
            for (int length = 1; length <= MaxCodeLength; length++)
            {
                for (int symbol = 0; symbol < lengths.Length; symbol++)
                    if (lengths[symbol] == length)
                        codes[symbol] = code++;
                code <<= 1;
            }
            return codes;
        }

        private sealed class BitWriter
        {
            private readonly MemoryStream stream = new MemoryStream();
            private int buffer;
            private int count;

            public void Write(int bits, int value)
            {
                for (int i = bits - 1; i >= 0; i--)
                {
                    buffer = (buffer << 1) | ((value >> i) & 1);
                    if (++count < 8)
                        continue;
                    stream.WriteByte((byte)buffer);
                    buffer = 0;
                    count = 0;
                }
            }

            public byte[] ToArray()
            {
                if (count > 0)
                {
                    stream.WriteByte((byte)(buffer << (8 - count)));
                    buffer = 0;
                    count = 0;
                }
                return stream.ToArray();
            }
        }
    }
}
