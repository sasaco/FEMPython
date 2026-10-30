"""Read the parts of a JRSNAP 5.2 analysis input (.ndt) that the P7 pier uses.

The .ndt is Shift-JIS text in fixed-width cards. Only the cards and fields that
P7 needs are read; anything else in those cards is rejected rather than skipped,
so a different model cannot be translated silently. Field positions follow the
JRSNAP manual (DA and LOAD cards) and the skill structural-jrsnap-ndu.
"""

from __future__ import annotations

from dataclasses import dataclass, field
from pathlib import Path

# Increment flag of a load row (LOAD card): 0 fixed, 1 incremental, 4 limited.
FLAG_KINDS = {0: "fixed", 1: "incremental", 4: "limited"}


@dataclass
class NodeLoad:
    node: int
    flag: int
    H: float
    N: float
    M: float


@dataclass
class MemberLoad:
    element: int
    flag: int
    load_type: int  # iT: 1 perpendicular (horizontal on a column), 4 parallel (self weight)
    w_i: float
    w_j: float


@dataclass
class LoadCase:
    number: int
    name: str
    node_loads: list[NodeLoad] = field(default_factory=list)
    member_loads: list[MemberLoad] = field(default_factory=list)


@dataclass
class Ndt:
    title: str
    joints: dict[int, tuple[float, float]]
    # element -> (i, j, type, A, I, E); type 1 is an M-theta element without A, I, E.
    elements: dict[int, tuple[int, int, int, float | None, float | None, float | None]]
    springs: dict[tuple[int, int], float]  # (joint, dof 1 x / 2 y) -> stiffness
    footing: dict[str, float]  # direct-foundation rotational spring (SUPPORT kind 6)
    footing_joint: int
    rho_m: float
    alpha_f: float
    steps: int
    target: float
    control_joint: int
    mdv: int
    limit3: int
    jalf: float
    cases: list[LoadCase]


def _number(text: str) -> float | None:
    text = text.strip()
    return float(text) if text else None


def _blocks(lines: list[str]) -> dict[str, list[str]]:
    """Split into the named cards that end with END (LOAD ends with CASE END)."""
    blocks: dict[str, list[str]] = {}
    index = 0
    while index < len(lines):
        head = lines[index].split()
        name = head[0] if head else ""
        if name in ("DIMENSION", "DA", "JOINT", "MEMBER", "SUPPORT", "AP", "COUNT"):
            end = next(i for i in range(index+1, len(lines)) if lines[i].strip() == "END")
            blocks[name] = lines[index:end]
            index = end+1
        elif name == "DL":
            end = next(i for i in range(index+1, len(lines)) if lines[i].startswith("AP"))
            blocks[name] = lines[index:end]
            index = end
        elif name == "LOAD":
            end = next(i for i in range(index+1, len(lines)) if lines[i].strip() == "CASE END")
            blocks[name] = lines[index:end]
            index = end+1
        else:
            index += 1
    return blocks


def read_ndt(path: Path) -> Ndt:
    lines = path.read_bytes().decode("shift_jis").splitlines()
    blocks = _blocks(lines)

    da = blocks["DA"]
    alpha_f = float(da[0][4:].split()[0])
    control = da[2]
    steps, target = int(control[0:10]), float(control[10:20])
    control_joint, mdv, limit3 = int(control[20:25]), int(control[25:30]), int(control[30:35])

    joints = {int(row.split()[0]): (float(row.split()[1]), float(row.split()[2]))
              for row in blocks["JOINT"][1:]}

    elements = {}
    for row in blocks["MEMBER"][2:]:
        values = row.split()
        number, i, j, kind = (int(v) for v in values[:4])
        if kind == 0:
            elements[number] = (i, j, 0, float(values[4]), float(values[5]), float(values[6]))
        elif kind == 1:
            elements[number] = (i, j, 1, None, None, None)
        else:
            raise ValueError(f"Unsupported member type {kind} for element {number}")

    springs, footing, footing_joint = {}, {}, None
    for row in blocks["SUPPORT"][2:]:
        values = row.split()
        joint, dof = int(values[0]), int(values[1])
        if dof in (1, 2) and len(values) == 3:
            springs[joint, dof] = float(values[2])
        elif dof == 6:
            names = ("iSOiL", "B", "L", "kv", "C", "Df", "gamma1", "gamma2", "phi", "KR2", "KR3")
            footing = dict(zip(names, map(float, values[2:])))
            footing_joint = joint
        else:
            raise ValueError(f"Unsupported SUPPORT row: {row!r}")

    rho_m = float(blocks["DL"][0].split()[1])

    load = blocks["LOAD"]
    jalf = float(load[0][20:30])
    cases: list[LoadCase] = []
    section = None
    for row in load[1:]:
        text = row.strip()
        if text.startswith("CASE-"):
            head = row[:40].split()[0]
            cases.append(LoadCase(int(head[5:]), row[40:].strip()))
        elif text in ("MEMBER LOAD", "JOINT LOAD"):
            section = text
        elif text == "END":
            section = None
        elif section == "JOINT LOAD":
            cases[-1].node_loads.append(NodeLoad(
                int(row[0:10]), int(row[10:15]),
                _number(row[15:25]) or 0., _number(row[25:35]) or 0., _number(row[35:45]) or 0.))
        elif section == "MEMBER LOAD":
            if row[15:35].strip():
                raise ValueError(f"Partial member loads (AML1/AML2) are not supported: {row!r}")
            w_i = _number(row[35:45])
            w_j = _number(row[45:55])
            cases[-1].member_loads.append(MemberLoad(
                int(row[0:10]), int(row[10:12]), int(row[12:15]), w_i, w_i if w_j is None else w_j))
        else:
            raise ValueError(f"Unexpected LOAD row: {row!r}")

    return Ndt(
        title=blocks["COUNT"][0][5:].strip(), joints=joints, elements=elements,
        springs=springs, footing=footing, footing_joint=footing_joint,
        rho_m=rho_m, alpha_f=alpha_f, steps=steps, target=target,
        control_joint=control_joint, mdv=mdv, limit3=limit3, jalf=jalf, cases=cases,
    )
