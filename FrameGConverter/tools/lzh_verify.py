"""Independent LHA archive checker for FrameG .frd files (standard library only).

Parses level-0/1 headers, decodes -lh0- and -lh5- entries with its own decoder,
and checks each entry's CRC-16/ARC and level-0 header checksum. It does not share
code with the C# writer or with SevenZipExtractor, so it can serve as a second
oracle for the exporter.

Usage (from the repository root):
    uv run --project FrameWeb --locked --extra dev python FrameGConverter/tools/lzh_verify.py <archive> [<extract dir>]

Exit status: 0 when every entry decodes and its CRC matches, 1 otherwise.
"""
import os
import struct
import sys


def crc16(data):
    crc = 0
    for value in data:
        crc ^= value
        for _ in range(8):
            crc = (crc >> 1) ^ 0xA001 if crc & 1 else crc >> 1
    return crc


class Bits:
    def __init__(self, data):
        self.data = data
        self.pos = 0

    def bit(self):
        index = self.pos >> 3
        value = (self.data[index] >> (7 - (self.pos & 7))) & 1 if index < len(self.data) else 0
        self.pos += 1
        return value

    def get(self, count):
        value = 0
        for _ in range(count):
            value = (value << 1) | self.bit()
        return value


def build(lengths):
    """Canonical Huffman table: (length, code) -> symbol."""
    table = {}
    code = 0
    for length in range(1, (max(lengths) if lengths else 0) + 1):
        for symbol, symbol_length in enumerate(lengths):
            if symbol_length == length:
                table[(length, code)] = symbol
                code += 1
        code <<= 1
    return table


def decode(bits, table):
    if isinstance(table, tuple):
        return table[1]
    code = 0
    for length in range(1, 40):
        code = (code << 1) | bits.bit()
        symbol = table.get((length, code))
        if symbol is not None:
            return symbol
    raise ValueError('invalid Huffman code')


def read_pt_len(bits, count, count_bits, special):
    n = bits.get(count_bits)
    if n == 0:
        return ('const', bits.get(count_bits))
    if n > count:
        raise ValueError('too many table lengths')
    lengths = [0] * count
    i = 0
    while i < n:
        length = bits.get(3)
        if length == 7:
            while bits.bit():
                length += 1
        lengths[i] = length
        i += 1
        if i == special:
            for _ in range(bits.get(2)):
                lengths[i] = 0
                i += 1
    return build(lengths)


NC, CBIT, NT, TBIT = 510, 9, 19, 5


def read_c_len(bits, table):
    n = bits.get(CBIT)
    if n == 0:
        return ('const', bits.get(CBIT))
    lengths = [0] * NC
    i = 0
    while i < n:
        value = decode(bits, table)
        if value <= 2:
            run = 1 if value == 0 else bits.get(4) + 3 if value == 1 else bits.get(CBIT) + 20
            for _ in range(run):
                lengths[i] = 0
                i += 1
        else:
            lengths[i] = value - 2
            i += 1
    return build(lengths)


def lh5(data, size, dictionary_bits=13, position_bits=4):
    bits = Bits(data)
    out = bytearray()
    remaining = 0
    blocks = 0
    while len(out) < size:
        if remaining == 0:
            remaining = bits.get(16)
            if remaining == 0:
                raise ValueError('empty block')
            blocks += 1
            table = read_pt_len(bits, NT, TBIT, 3)
            characters = read_c_len(bits, table)
            positions = read_pt_len(bits, dictionary_bits + 1, position_bits, -1)
        remaining -= 1
        symbol = decode(bits, characters)
        if symbol < 256:
            out.append(symbol)
            continue
        length = symbol - 256 + 3
        distance = decode(bits, positions)
        if distance != 0:
            distance = (1 << (distance - 1)) + bits.get(distance - 1)
        start = len(out) - distance - 1
        if start < 0:
            raise ValueError('match before the start of the data')
        for k in range(length):
            out.append(out[start + k])
    return bytes(out[:size]), blocks


def verify(path, extract_dir=None):
    archive = open(path, 'rb').read()
    offset = 0
    ok = True
    entries = []
    while True:
        if offset >= len(archive):
            print('  missing 0x00 terminator')
            ok = False
            break
        if archive[offset] == 0:
            trailing = len(archive) - offset - 1
            if trailing:
                print(f'  {trailing} bytes after the terminator')
                ok = False
            break
        level = archive[offset + 20]
        if level not in (0, 1):
            print(f'  unsupported header level {level} at {offset}')
            return False
        size = archive[offset]
        checksum_ok = (sum(archive[offset + 2:offset + 2 + size]) & 0xFF) == archive[offset + 1]
        method = archive[offset + 2:offset + 7].decode('ascii')
        packed, original, time = struct.unpack_from('<III', archive, offset + 7)
        name_length = archive[offset + 21]
        name = archive[offset + 22:offset + 22 + name_length].decode('cp932')
        crc = struct.unpack_from('<H', archive, offset + 22 + name_length)[0]
        data_offset = offset + 2 + size
        if level == 1:
            next_size = struct.unpack_from('<H', archive, data_offset - 2)[0]
            position = data_offset
            while next_size:
                following = struct.unpack_from('<H', archive, position + next_size - 2)[0]
                position += next_size
                next_size = following
            packed -= position - data_offset
            data_offset = position
        data = archive[data_offset:data_offset + packed]
        try:
            if method == '-lh0-':
                content, blocks = data, 0
            elif method == '-lh5-':
                content, blocks = lh5(data, original)
            else:
                raise ValueError(f'unsupported method {method}')
            crc_ok = len(content) == original and crc16(content) == crc
        except (ValueError, IndexError) as error:
            content, blocks, crc_ok = None, None, False
            print(f'  {name}: {error}')
        print(f'  {method} {name} original={original} packed={packed} blocks={blocks} '
              f'time={time} crc=0x{crc:04x} crc_ok={crc_ok} checksum_ok={checksum_ok}')
        ok &= crc_ok and checksum_ok
        entries.append((name, content))
        offset = data_offset + packed
    if extract_dir:
        os.makedirs(extract_dir, exist_ok=True)
        for name, content in entries:
            if content is not None:
                with open(os.path.join(extract_dir, name), 'wb') as file:
                    file.write(content)
    print(f'{path}: {len(entries)} entries, {"OK" if ok else "FAILED"}')
    return ok


if __name__ == '__main__':
    if hasattr(sys.stdout, 'reconfigure'):
        sys.stdout.reconfigure(encoding='utf-8')
    if len(sys.argv) not in (2, 3):
        print(__doc__)
        sys.exit(2)
    sys.exit(0 if verify(sys.argv[1], sys.argv[2] if len(sys.argv) == 3 else None) else 1)
