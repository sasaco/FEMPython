"""Write JRSNAP 5.2 inputs (.ndu) for the nodal-spring golden cases from tests/data/snap/beam001.ndu.

Every case isolates one nonlinear nodal spring: a rigid linear member 1-2,
node 2 restrained in Y and rotation, and a JRSNAP tri-linear X spring at node 2.
A horizontal load at node 1 is applied by load increments, or used as the
load pattern of a displacement-increment analysis controlled at node 1.

    uv run --locked --extra dev python -m tools.jrsnap_golden.make_cases CASE OUT.ndu [--steps N]
"""
import argparse
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
TEMPLATE = ROOT / 'tests/data/snap/beam001.ndu'

# JRSNAP BLOCK6 tri-linear order: iS, iE, iK, +SP1, +RMiT1, -RMiT1, +SP2, +RMiT2, -RMiT2, +SP3, -SP1, -SP2, -SP3
CASES = {
    'support_trilinear01': {
        'title': '節点ばねトリリニア 単調載荷',
        'spring': ' ,2,1,1000,10,5,200,14,7,50,500,100,0.001',
        'load': 20,
    },
    'support_trilinear02_neg': {
        'title': '節点ばねトリリニア 負側載荷',
        'spring': ' ,2,1,1000,10,5,200,14,7,50,500,100,20',
        'load': -10,
    },
    'support_trilinear03_softening': {
        'title': '節点ばねトリリニア 下降勾配 変位増分',
        'spring': ' ,2,1,1000,10,5,200,14,7,-20,500,100,0.001',
        'load': 20,
        # Displacement increment at node 1 up to DSMAX (m).
        'displacement': 0.2,
    },
}

# Rows repeated per joint/element in the template; JRSNAP 5.2 requires one per item.
DROP = re.compile(r'^(JointXY[3-9]|JibanShogenInfo[2-9]|MemberDvid1-[2-9]|ElementInfo[2-9]|'
                  r'ElementTyakumokuNum[2-9]|SHABUZAI_JointXY[3-9]|NonLinInfo\d|NonLinFix\d|'
                  r'Suppot_ChokuKisoCaseNo[4-9])=')


def data_info(steps, displacement):
    # Horizontal-spring and material nonlinearity off (2 -> flag 9), nodal springs on (1 -> flag 0).
    flags = '999,999,2,2,1,1,2,1,1'
    if displacement is None:
        # Load increment: MDV = LiMiT3 = steps.
        return f'1,2, , ,{steps},{steps}, ,{steps},{steps},{flags}, ,0.001,0,1,0,0,0,0'
    # Displacement increment: DDV = steps, JNO = 1, MDV = LiMiT3 = steps, DSMAX = displacement.
    return f'1,1,{steps},1,{steps},{steps}, , , ,{flags},{displacement},0.001,0,1,0,0,0,0'


def build(case, steps, name):
    spec = CASES[case]
    replace = {
        'Virsion': '5.2.3.S.4',
        'DataName': name,
        'DataTitle1': spec['title'],
        'DataInfo': data_info(steps, spec.get('displacement')),
        'JointOrg': '2', 'JointNum': '2',
        'JointXY1': '0,-1,1', 'JointXY2': '0,0,2',
        'MemberNum': '1', 'MemberInfo1': '1,2,1', 'MemberLeng1': '1', 'MemberDvid1-1': '1,1',
        'ElementNum': '1', 'ElementInfo1': '1,0,0,0,1,2,0,0,1000,1000,26.5,0,0,1',
        'NonLinNum': '0,0,0',
        'SuppotNum': '3', 'SuppotRow': '3',
        'SuppotInfo1': spec['spring'],
        'SuppotInfo2': ' ,2,2, , , , , , , , , , ',
        'SuppotInfo3': ' ,2,3, , , , , , , , , , ',
        'LoadTitle1': '水平荷重',
        'JLoadInfo1': f"1, ,1,1,{spec['load']}, , , ",
    }
    lines = []
    for line in TEMPLATE.read_bytes().decode('cp932').splitlines():
        if DROP.match(line):
            continue
        key = line.split('=', 1)[0]
        lines.append(f'{key}={replace[key]}' if key in replace else line)
    return ('\r\n'.join(lines) + '\r\n').encode('cp932')


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('case', choices=sorted(CASES))
    parser.add_argument('output', type=Path)
    parser.add_argument('--steps', type=int, default=100)
    args = parser.parse_args()
    args.output.write_bytes(build(args.case, args.steps, args.output.stem))


if __name__ == '__main__':
    main()
