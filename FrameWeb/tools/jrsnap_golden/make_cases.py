"""Write JRSNAP 5.2 inputs (.ndu) for the golden cases from tests/data/snap/beam001.ndu.

Two generators share the template.

``LEGACY_CASES`` (2026-10-18) isolate one nonlinear nodal spring: a rigid linear
member 1-2, node 2 restrained in Y and rotation, and a JRSNAP tri-linear X spring
at node 2. A horizontal load at node 1 is applied by load increments, or used as
the load pattern of a displacement-increment analysis controlled at node 1. Their
builder is kept as it was so that the frozen .ndu files regenerate byte for byte.

``MODELS`` (2026-10-19) describe any small plane frame: joints, members divided
into elements, linear or fixed M-phi (BLOCK7) elements, BLOCK6 support rows, joint
loads with X, Y and moment components, load or displacement increments, the
nonlinearity flags and the stiffness-reduction convergence factor FTM. The
convergence method (column 70 of the .ndt) cannot be set from a .ndu; see
ndt_tools.py.

    uv run --locked --extra dev python -m tools.jrsnap_golden.make_cases CASE OUT.ndu [--steps N]
"""
import argparse
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
TEMPLATE = ROOT / 'tests/data/snap/beam001.ndu'
VERSION = '5.2.3.S.4'

# JRSNAP BLOCK6 tri-linear order: iS, iE, iK, +SP1, +RMiT1, -RMiT1, +SP2, +RMiT2, -RMiT2, +SP3, -SP1, -SP2, -SP3
LEGACY_CASES = {
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
CASES = LEGACY_CASES

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
    spec = LEGACY_CASES[case]
    replace = {
        'Virsion': VERSION,
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


# --- General plane-frame models (2026-10-19) -------------------------------------------------------

# Keys whose rows are repeated per item in the template; the model builder writes its own rows.
PER_ITEM = re.compile(r'^(JointXY|MemberTitle|MemberInfo|MemberLeng|MemberOkuyuki|MemberDvid|MemberShogen|MemberKUI|'
                      r'ElementInfo|JibanShogenInfo|ElementTyakumokuNum|SHABUZAI_JointXY|ShabuzaiMeInfo|'
                      r'NonLinInfo|NonLinFix|SuppotInfo|Suppot_ChokuKisoCaseNo|Member_BunpuBane|'
                      r'JLoadInfo|SesfPiIJ)\d')
# The .ndu is read line by line, so every repeated row family must have the count JRSNAP expects:
# JointXY and SHABUZAI_JointXY per joint; Member*, ShabuzaiMeInfo and Member_BunpuBane per member;
# ElementInfo, JibanShogenInfo, ElementTyakumokuNum and SesfPiIJ per element; SuppotInfo and
# Suppot_ChokuKisoCaseNo per support row; JibanTeikouKeisuu always three rows (kept from the template).
# (JRSNAPHeadless/tests/JRSNAP.Headless.Tests/fixtures/線路直角方向.ndu: 8 joints, 7 members and elements, 2 support rows.)
FLAG_ON, FLAG_OFF = 1, 2


def rigid_support(joint, direction):
    """A BLOCK6 row with blank values: JRSNAP's rigid support (about 1e15) in direction 1=X, 2=Y, 3=rotation."""
    return f' ,{joint},{direction}, , , , , , , , , , '


def trilinear_support(joint, direction, K1, F1, F1_neg, K2, F2, F2_neg, K3, K1_neg, K2_neg, K3_neg):
    """A BLOCK6 tri-linear row (+SP1, +RMiT1, -RMiT1, +SP2, +RMiT2, -RMiT2, +SP3, -SP1, -SP2, -SP3)."""
    return f' ,{joint},{direction},{K1},{F1},{F1_neg},{K2},{F2},{F2_neg},{K3},{K1_neg},{K2_neg},{K3_neg}'


def plain_support(joint, direction, K, F, F_neg):
    """A BLOCK6 row with the three values of JRSNAP's ordinary (bi-linear) spring: SP, +RmiT, -RmiT.

    How JRSNAP tells this row from a tri-linear one is checked on the written .ndt (plan 2c).
    """
    return f' ,{joint},{direction},{K},{F},{F_neg}, , , , , , , '


def number(value):
    """A number the way the screen writes it (no trailing .0; exponents as 1E-05)."""
    if isinstance(value, str):
        return value
    value = float(value)
    if value.is_integer() and abs(value) < 1e15:
        return str(int(value))
    text = repr(value)
    if 'e' in text:
        mantissa, exponent = text.split('e')
        return f'{mantissa}E{int(exponent):+03d}'
    return text


def model_data_info(control, flags, ftm):
    """The DataInfo row. Fields: 1, ETP, DDV, JNO, MDV, LiMiT3, LiMiT4, LiMiT6, LiMiT5, LiMiT1, LiMiT2, then the
    nonlinearity flags, DSMAX (field 19), FTM (field 20).

    ``flags`` is keyed by the .ndt DA columns 31-34 (A, B, C, D of 付2-13). The writer puts field 12 into column
    32 and field 13 into column 31 (measured 2026-10-19: fields 2,1,2,1 gave columns "0990"), so B comes first.
    """
    steps = control['steps']
    a, b, c, d = (flags.get(name, FLAG_OFF) for name in 'ABCD')
    tail = f'{b},{a},{c},{d},2,1,1'
    if control['mode'] == 'load':
        # LiMiT3 (field 6) is the step at which LK=4 loads stop increasing (付2-121); MDV by default.
        limit3 = control.get('limit3', steps)
        return f'1,2, , ,{steps},{limit3}, ,{steps},{steps},999,999,{tail}, ,{number(ftm)},0,1,0,0,0,0'
    if control['mode'] != 'displacement':
        raise ValueError(f"control mode {control['mode']!r}")
    return (f"1,1,{steps},{control['node']},{steps},{steps}, , , ,999,999,{tail},"
            f"{number(control['dsmax'])},{number(ftm)},0,1,0,0,0,0")


def element_info(index, element):
    """ElementInfo: kind, 0, 0, 0, i, j, M-phi table, 0, A, I, E, 0, 0, number (kind 1 linear, 3 fixed M-phi)."""
    kind = 3 if element.get('mphi') else 1
    table = element.get('mphi', 0)
    return (f"{kind},0,0,0,{element['i']},{element['j']},{table},0,{number(element['A'])},{number(element['I'])},"
            f"{number(element['E'])},0,0,{index}")


def nonlin_fix(table):
    """NonLinFix: M1..M4, phi1..phi4 (analysis), phi1..phi4 (check), 12 zeros; as in beam001.ndu."""
    moments = ','.join(number(m) for m in table['M'])
    curvatures = ','.join(number(p) for p in table['phi'])
    return f"{moments},{curvatures},{curvatures},{','.join(['0'] * 12)},"


def joint_load(load):
    """JLoadInfo: load case, blank, joint, LK, PX, PY, PM, title (blank).

    LK (付2-121): 1 = increased over MDV steps (default), 4 = increased over LiMiT3 steps and then held,
    0 = constant from the first step.
    """
    px, py, pm = (number(load[key]) if load.get(key) not in (None, '') else ' ' for key in ('px', 'py', 'pm'))
    return f"1, ,{load['node']},{load.get('lk', 1)},{px},{py},{pm}, "


def expand_elements(model):
    """Elements in JRSNAP order: member by member, division by division; the joints between divisions are the
    model's joints, so each member lists its intermediate joints."""
    elements = []
    for member in model['members']:
        path = [member['i'], *member.get('via', []), member['j']]
        for k, (i, j) in enumerate(zip(path, path[1:])):
            spec = dict(member['elements'][k])
            spec.update(i=i, j=j)
            elements.append(spec)
    return elements


def build_model(model, name, steps=None):
    """The .ndu of a MODELS entry (cp932, CRLF)."""
    control = dict(model['control'])
    if steps is not None:
        control['steps'] = steps
    joints = model['joints']
    elements = expand_elements(model)
    tables = model.get('mphi', [])
    supports = model['supports']
    loads = model['loads']

    def length(i, j):
        (xi, yi), (xj, yj) = joints[i - 1], joints[j - 1]
        return ((xj - xi) ** 2 + (yj - yi) ** 2) ** .5

    replace = {
        'Virsion': VERSION, 'DataName': name, 'DataTitle1': model['title'],
        'DataInfo': model_data_info(control, model.get('flags', {}), model.get('ftm', 0.001)),
        'JointOrg': str(len(joints)), 'JointNum': str(len(joints)),
        'MemberNum': str(len(model['members'])),
        'ElementNum': str(len(elements)),
        'NonLinNum': f'{len(tables)},0,{len(tables)}',
        # SuppotNum is the total number of support constraints the .ndt DIMENSION line announces; with fewer than
        # the rows JRSNAP stops with error 612/613 (support_multi01, 2026-10-19).
        'SuppotNum': str(len(supports)), 'SuppotRow': str(len(supports)),
        'LoadTitle1': model.get('load_title', '荷重'),
        'JLoadRow1': str(len(loads)),
    }
    rows = {
        'JointNum': [f'JointXY{k}={number(x)},{number(y)},{k}' for k, (x, y) in enumerate(joints, 1)],
        'MemberNum': [],
        'ElementNum': [f'ElementInfo{k}={element_info(k, e)}' for k, e in enumerate(elements, 1)],
        'JibanCaseNum': [f'JibanShogenInfo{k}= , , , , , , ' for k in range(1, len(elements) + 1)],
        'TyakumokuNum': [f'ElementTyakumokuNum{k}=0,0' for k in range(1, len(elements) + 1)],
        'SHABUZAI_INFO': [f'SHABUZAI_JointXY{k}=0,0,False' for k in range(1, len(joints) + 1)]
                         + [f'ShabuzaiMeInfo{k}=0,0,0,0,0,0,0,0,0,False,False' for k in range(1, len(model['members']) + 1)],
        'NonLinNum': [line for k, t in enumerate(tables, 1)
                      for line in (f'NonLinInfo{k}=2,0,2,1,1,0,0,0,0,', f'NonLinFix{k}={nonlin_fix(t)}')],
        'ShitenCaseNum': [f'SuppotInfo{k}={row}' for k, row in enumerate(supports, 1)],
        'G_intCHOKU_KISO_Link_Num': [f'Suppot_ChokuKisoCaseNo{k}=0' for k in range(1, len(supports) + 1)]
                                    + [f'Member_BunpuBane{k}=0' for k in range(1, len(model['members']) + 1)],
        'JLoadRow1': [f'JLoadInfo{k}={joint_load(load)}' for k, load in enumerate(loads, 1)],
        'SesfNum': [f'SesfPiIJ{k}=0,0,0,0,,' for k in range(1, len(elements) + 1)],
    }
    for k, member in enumerate(model['members'], 1):
        path = [member['i'], *member.get('via', []), member['j']]
        divisions = [length(i, j) for i, j in zip(path, path[1:])]
        rows['MemberNum'] += [f"MemberTitle{k}={member.get('title', '部材名称')}",
                              f"MemberInfo{k}={member['i']},{member['j']},{len(divisions)}",
                              f'MemberLeng{k}={number(sum(divisions))}',
                              f'MemberOkuyuki{k}=1']
        rows['MemberNum'] += [f'MemberDvid{k}-{d}={number(l)},1' for d, l in enumerate(divisions, 1)]
        rows['MemberNum'] += [f'MemberShogen{k}=1']
    # MemberKUI rows follow all members (線路直角方向.ndu lines 79-85), not each member's rows.
    rows['MemberNum'] += [f'MemberKUI{k}=0' for k in range(1, len(model['members']) + 1)]

    lines = []
    for line in TEMPLATE.read_bytes().decode('cp932').splitlines():
        if PER_ITEM.match(line):
            continue
        key = line.split('=', 1)[0]
        lines.append(f'{key}={replace[key]}' if key in replace else line)
        lines.extend(rows.get(key, []))
    return ('\r\n'.join(lines) + '\r\n').encode('cp932')


# Shared values of the equivalence cases (.claude/docs/plans/jrsnap-frameweb-equivalence.md, Steps 1-4).
# The writer prints A, I and E in 10 columns and FTM and DSMAX with three decimals, so every value below is
# chosen to survive that formatting; FTM and DSMAX finer than 0.001 are written into the .ndt with ndt_tools.
E_KN_MM2 = 26.5           # the .ndu E field of the support cases; the DB stores 26500000 (kN/m2) for it
E_MEMBER = 25             # the member and calibration cases: E = 2.5e7 kN/m2
RIGID = dict(A=1000, I=1000, E=E_KN_MM2)
TRILINEAR_A = dict(K1=1000, F1=10, F1_neg=5, K2=200, F2=14, F2_neg=7, K3=50, K1_neg=500, K2_neg=100, K3_neg=0.001)
# Skeleton shared by the member cases: the points of tests/data/snap/jr_k4_displacement_control.json (K4 = -1000).
MPHI = dict(M=(10, 16, 22, 14), phi=(0.001, 0.004, 0.010, 0.018))
# E*I = M1/phi1 = 1e4 kN*m2 exactly (2.5e7 * 4e-4), so that the elastic branch is the same whichever JRSNAP uses.
I_MPHI = 0.0004
# Calibration column: E*I = 250 kN*m2; a tip load of 10 kN bends it 10/(3*250) = 0.013333 m and a tip moment of
# 10 kN*m 10/(2*250) = 0.02 m. A = 0.0007 m2 makes the shear deflection PL/(GA) about 10% of the bending one when
# G = E/2.4, so the run shows whether JRSNAP includes shear deformation.
CALIBRATION = dict(A=0.0007, I=1e-5, E=E_MEMBER)
LINEAR = dict(A=FLAG_OFF, B=FLAG_OFF, C=FLAG_OFF, D=FLAG_ON)

MODELS = {
    'linear01_cantilever': {
        'title': '線形校正 片持ち柱 先端X荷重',
        'joints': [(0, -1), (0, 0)],
        'members': [dict(i=1, j=2, elements=[dict(CALIBRATION)])],
        'supports': [rigid_support(2, 1), rigid_support(2, 2), rigid_support(2, 3)],
        'loads': [dict(node=1, px=10)],
        'control': dict(mode='load', steps=10),
        'flags': LINEAR,
    },
    'linear01_cantilever_moment': {
        'title': '線形校正 片持ち柱 先端モーメント荷重',
        'joints': [(0, -1), (0, 0)],
        'members': [dict(i=1, j=2, elements=[dict(CALIBRATION)])],
        'supports': [rigid_support(2, 1), rigid_support(2, 2), rigid_support(2, 3)],
        'loads': [dict(node=1, pm=10)],
        'control': dict(mode='load', steps=10),
        'flags': LINEAR,
    },
    'linear02_moment_disp': {
        'title': '線形校正 先端モーメントの荷重パターン X変位増分',
        'joints': [(0, -1), (0, 0)],
        'members': [dict(i=1, j=2, elements=[dict(CALIBRATION)])],
        'supports': [rigid_support(2, 1), rigid_support(2, 2), rigid_support(2, 3)],
        'loads': [dict(node=1, pm=10)],
        # Twice the tip displacement of M = 10 (0.02 m), in 10 steps.
        'control': dict(mode='displacement', steps=10, node=1, dsmax=0.04),
        'flags': LINEAR,
    },
    'support_trilinear04_neg_disp': {
        'title': '節点ばねトリリニア 負側 変位増分',
        'joints': [(0, -1), (0, 0)],
        'members': [dict(i=1, j=2, elements=[dict(RIGID)])],
        'supports': [trilinear_support(2, 1, **dict(TRILINEAR_A, K3_neg=20)), rigid_support(2, 2), rigid_support(2, 3)],
        'loads': [dict(node=1, px=-10)],
        'control': dict(mode='displacement', steps=100, node=1, dsmax=-0.1),
        'flags': dict(A=FLAG_OFF, B=FLAG_OFF, C=FLAG_ON, D=FLAG_ON),
    },
    'support_bilinear01': {
        'title': '節点ばね通常（バイリニア） 変位増分',
        'joints': [(0, -1), (0, 0)],
        'members': [dict(i=1, j=2, elements=[dict(RIGID)])],
        'supports': [plain_support(2, 1, 1000, 10, 5), rigid_support(2, 2), rigid_support(2, 3)],
        'loads': [dict(node=1, px=20)],
        'control': dict(mode='displacement', steps=100, node=1, dsmax=0.05),
        'flags': dict(A=FLAG_OFF, B=FLAG_OFF, C=FLAG_ON, D=FLAG_ON),
    },
    'support_multi01': {
        'title': '節点ばね3基 剛な部材2本 変位増分',
        'joints': [(0, 0), (1, 0), (2, 0)],
        'members': [dict(i=1, j=2, elements=[dict(RIGID)]), dict(i=2, j=3, elements=[dict(RIGID)])],
        'supports': [trilinear_support(1, 1, **TRILINEAR_A),
                     trilinear_support(2, 1, **dict(TRILINEAR_A, K3=-20)),
                     plain_support(3, 1, 1000, 10, 5),
                     rigid_support(1, 2), rigid_support(1, 3), rigid_support(2, 2), rigid_support(2, 3),
                     rigid_support(3, 2), rigid_support(3, 3)],
        'loads': [dict(node=3, px=30)],
        'control': dict(mode='displacement', steps=200, node=3, dsmax=0.1),
        'flags': dict(A=FLAG_OFF, B=FLAG_OFF, C=FLAG_ON, D=FLAG_ON),
    },
    # A load reversal (plan Step 2e) was tried as two rows of one load case, +40 kN with LK=4 (LiMiT3 = 50) and
    # -50 kN with LK=1: the writer keeps one JOINT LOAD row per joint and case (2026-10-19, .ndt inspected with
    # --analyze false), so the two kinds would need two load cases and a combination block, which is not built.
    # Member cases: FTM is left at the writer's 0.001 here and set to 1.0E-08 in the .ndt (ndt_tools da --ftm).
    'member_mphi01_moment': {
        'title': '部材M-φ 片持ち柱 先端モーメント 荷重増分',
        'joints': [(0, -1), (0, 0)],
        'members': [dict(i=1, j=2, elements=[dict(A=1000, I=I_MPHI, E=E_MEMBER, mphi=1)])],
        'mphi': [MPHI],
        'supports': [rigid_support(2, 1), rigid_support(2, 2), rigid_support(2, 3)],
        'loads': [dict(node=1, pm=21)],
        'control': dict(mode='load', steps=100),
        'flags': dict(A=FLAG_OFF, B=FLAG_ON, C=FLAG_OFF, D=FLAG_ON),
    },
    # A displacement increment adds a horizontal force at the control node (linear02_moment_disp), so the
    # descending branch is reached with a tip X load pattern and a 0.1 m M-phi element at the base (beam001's
    # layout); the moment varies by 10% over that element, which the record keeps in mind.
    'member_mphi02_disp': {
        'title': '部材M-φ 基部0.1m要素 先端X荷重 X変位増分 下降勾配',
        'joints': [(0, -1), (0, -0.1), (0, 0)],
        'members': [dict(i=1, j=3, via=[2], elements=[dict(A=1000, I=1000, E=E_MEMBER),
                                                      dict(A=1000, I=I_MPHI, E=E_MEMBER, mphi=1)])],
        'mphi': [MPHI],
        'supports': [rigid_support(3, 1), rigid_support(3, 2), rigid_support(3, 3)],
        'loads': [dict(node=1, px=10)],
        # The base moment is V * 1 m; V passes 22 kN at phi3 and then falls. The tip moves about
        # kappa * 0.1 m * 0.95 m, so dsmax = 0.002 reaches kappa of about 0.02 (past phi4 = 0.018).
        'control': dict(mode='displacement', steps=100, node=1, dsmax=0.002),
        'flags': dict(A=FLAG_OFF, B=FLAG_ON, C=FLAG_OFF, D=FLAG_ON),
    },
    # beam001 (tests/data/snap/beam001.ndu) under the same conditions, with A and I given to the M-phi element
    # (付2-34 note 6) and E*I = M1/phi1 = 1e8 exactly (2.5e7 * 4).
    'member_beam001': {
        'title': 'beam001 柱下端固定 9.8+0.1+0.1 固定M-φ',
        'joints': [(0, -10), (0, -0.2), (0, -0.1), (0, 0)],
        'members': [dict(i=1, j=4, via=[2, 3], elements=[dict(RIGID), dict(A=1000, I=4, E=E_MEMBER, mphi=1), dict(RIGID)])],
        'mphi': [dict(M=(1000, 3000, 5000, 3000), phi=(1e-5, 1e-4, 1e-3, 2e-3))],
        'supports': [rigid_support(4, 1), rigid_support(4, 2), rigid_support(4, 3)],
        'loads': [dict(node=1, px=500)],
        'control': dict(mode='load', steps=100),
        'flags': dict(A=FLAG_OFF, B=FLAG_ON, C=FLAG_OFF, D=FLAG_ON),
    },
    'combined01': {
        'title': '部材M-φ + 回転トリリニアばね 先端モーメント 荷重増分',
        'joints': [(0, -1), (0, 0)],
        'members': [dict(i=1, j=2, elements=[dict(A=1000, I=I_MPHI, E=E_MEMBER, mphi=1)])],
        'mphi': [MPHI],
        # The spring is row 1 so that its force and segment are readable (JointItem has joints + 1 entries).
        'supports': [trilinear_support(2, 3, K1=5000, F1=8, F1_neg=8, K2=1000, F2=12, F2_neg=12, K3=100,
                                       K1_neg=5000, K2_neg=1000, K3_neg=100),
                     rigid_support(2, 1), rigid_support(2, 2)],
        'loads': [dict(node=1, pm=21)],
        'control': dict(mode='load', steps=100),
        'flags': dict(A=FLAG_OFF, B=FLAG_ON, C=FLAG_ON, D=FLAG_ON),
    },
}


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('case', choices=sorted(LEGACY_CASES) + sorted(MODELS))
    parser.add_argument('output', type=Path)
    parser.add_argument('--steps', type=int, help='legacy cases: default 100; models: override the spec')
    args = parser.parse_args()
    if args.case in LEGACY_CASES:
        args.output.write_bytes(build(args.case, args.steps or 100, args.output.stem))
    else:
        args.output.write_bytes(build_model(MODELS[args.case], args.output.stem, args.steps))


if __name__ == '__main__':
    main()
