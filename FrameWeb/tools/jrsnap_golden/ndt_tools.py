"""Edit a JRSNAP .ndt that the writer produced, for settings the screen and the .ndu cannot express.

Column 70 of the first DA line is the convergence-method flag F (JRSNAP 操作説明書 付2-13):
blank (or 0) selects the speed-priority method, 1 the accuracy-priority (従来) method.
The writer leaves the line 65 characters long, so the column is padded in.

    uv run --locked --extra dev python -m tools.jrsnap_golden.ndt_tools convergence IN.ndt OUT.ndt --method accuracy
"""
import argparse
from pathlib import Path

CONVERGENCE_COLUMN = 70
METHODS = {'speed': ' ', 'accuracy': '1'}
# DA block fields (付2-12, 付2-16): line 1 columns 11-20 FTM (剛性低下率収斂係数), line 2 columns 11-20 DSMAX.
# The writer prints both with three decimals, so values such as 1E-08 or 3.77E-04 need this edit.
FTM_FIELD = (1, 11, 20)
DSMAX_FIELD = (2, 11, 20)
# DA line 1 columns 31-34: the nonlinearity flags A, B, C, D (付2-13; 0 = considered, 9 = not).
FLAGS_FIELD = (1, 31, 34)


def _da_lines(ndt):
    lines = ndt.split(b'\r\n')
    try:
        header = next(i for i, line in enumerate(lines) if line.startswith(b'DA'))
    except StopIteration:
        raise ValueError('no DA block in the .ndt') from None
    return lines, header


def set_da_field(ndt, field, text):
    """The .ndt bytes with the DA field (line number in the block, first and last column) replaced by ``text``
    right-aligned; the line is padded when it is shorter."""
    line_no, first_col, last_col = field
    width = last_col - first_col + 1
    if len(text) > width:
        raise ValueError(f'{text!r} does not fit in {width} columns')
    lines, header = _da_lines(ndt)
    line = lines[header + line_no]
    if len(line) < last_col:
        line = line.ljust(last_col, b' ')
    lines[header + line_no] = line[:first_col - 1] + text.rjust(width).encode('ascii') + line[last_col:]
    return b'\r\n'.join(lines)


def set_convergence(ndt, method):
    """The .ndt bytes with column 70 of the first DA line set for ``method``; nothing else changes."""
    return set_da_field(ndt, (1, CONVERGENCE_COLUMN, CONVERGENCE_COLUMN), METHODS[method])


def set_ftm(ndt, value):
    """The .ndt bytes with the stiffness-reduction convergence factor written as ``value`` (text, e.g. '1.0E-08')."""
    return set_da_field(ndt, FTM_FIELD, value)


def set_dsmax(ndt, value):
    """The .ndt bytes with the displacement-increment target written as ``value`` (text)."""
    return set_da_field(ndt, DSMAX_FIELD, value)


def da_field(ndt, field):
    """The text of a DA field as written (stripped; None when the line is shorter)."""
    line_no, first_col, last_col = field
    lines, header = _da_lines(ndt)
    line = lines[header + line_no]
    return line[first_col - 1:last_col].decode('ascii').strip() if len(line) >= first_col else None


def convergence_flag(ndt):
    """Column 70 of the first DA line as written (None when the line is shorter)."""
    lines, header = _da_lines(ndt)
    first = lines[header + 1]
    return chr(first[CONVERGENCE_COLUMN - 1]) if len(first) >= CONVERGENCE_COLUMN else None


def main():
    parser = argparse.ArgumentParser()
    sub = parser.add_subparsers(dest='command', required=True)
    conv = sub.add_parser('convergence', help='set column 70 of the first DA line')
    conv.add_argument('input', type=Path)
    conv.add_argument('output', type=Path)
    conv.add_argument('--method', choices=sorted(METHODS), required=True)
    da = sub.add_parser('da', help='edit DA fields the writer rounds')
    da.add_argument('input', type=Path)
    da.add_argument('output', type=Path)
    da.add_argument('--convergence', choices=sorted(METHODS))
    da.add_argument('--ftm', help='text for columns 11-20 of DA line 1, e.g. 1.0E-08')
    da.add_argument('--dsmax', help='text for columns 11-20 of DA line 2, e.g. 0.0004')
    da.add_argument('--flags', help='the four flag columns 31-34 of DA line 1, e.g. 9090')
    args = parser.parse_args()
    ndt = args.input.read_bytes()
    if args.command == 'convergence':
        ndt = set_convergence(ndt, args.method)
    else:
        if args.convergence:
            ndt = set_convergence(ndt, args.convergence)
        if args.ftm:
            ndt = set_ftm(ndt, args.ftm)
        if args.dsmax:
            ndt = set_dsmax(ndt, args.dsmax)
        if args.flags:
            if len(args.flags) != 4 or set(args.flags) - set('09'):
                raise SystemExit('--flags must be four characters of 0 or 9')
            ndt = set_da_field(ndt, FLAGS_FIELD, args.flags)
    args.output.write_bytes(ndt)


if __name__ == '__main__':
    main()
