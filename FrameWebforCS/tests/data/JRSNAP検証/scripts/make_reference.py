"""Reduce a raw DB1 dump (extract_db1.ps1) to the values this comparison uses.

Usage (repository root):
    uv run --project FrameWeb --locked python <this folder>/make_reference.py <raw.json> <out.json> <ndt>

The output keeps JRSNAP's own signs and units (m, rad, kN, kN*m; y downward):
every step's joint displacements, element end forces, the M-theta element's
state, and the support rows' spring force and branch flag. The M-theta skeleton
and the measured foundation rotational spring (branch points from the stored
steps, see README) are written once.
"""

from __future__ import annotations

import json
import math
import sys
from pathlib import Path

from ndt import read_ndt

DIGITS = 12


def _round(value: float) -> float:
    return float(f"{value:.{DIGITS}g}")


def _mtheta_element(ndt) -> int:
    (number,) = [n for n, element in ndt.elements.items() if element[2] == 1]
    return number


def _foundation(ndt, steps, footing_row):
    """Branch points of the rotational spring, read from the stored states.

    Rotation is the footing joint's rz (the spring's own deformation, origin at
    zero load); the spring moment is the force JRSNAP reports for that support
    row while the spring is loaded monotonically. The second break moment Mmd
    is solved from the stored states on the third branch, using K2 = K1/KR2 and
    K3 = K1/KR3 (the inputs) and the first break point (K1, M1) measured on the
    first branch: on that branch M - K3*theta is the constant Mmd - K3*theta2,
    averaged here over every state until the rotation first turns back.
    """
    joint = ndt.footing_joint
    first = steps[0]
    theta0 = first["joints"][str(joint)][2]
    moment0 = first["support_rows"][footing_row][0]
    k1 = moment0/theta0
    vertical = first["support_rows"][footing_row-1][0]
    m1 = vertical*ndt.footing["B"]/6
    k2, k3 = k1/ndt.footing["KR2"], k1/ndt.footing["KR3"]
    flags = [step["support_rows"][footing_row][1] for step in steps]
    third = next((i for i, flag in enumerate(flags) if abs(flag) == 2), None)
    result = {"joint": joint, "row": footing_row+1, "K1": k1, "M1": m1, "K2": k2, "K3": k3,
              "vertical_reaction": vertical,
              "first_second_branch_step": next((i for i, f in enumerate(flags) if f), None),
              "first_third_branch_step": third, "Mmd": None}
    if third is not None:
        sign = 1.0 if flags[third] > 0 else -1.0
        intercepts, previous = [], -math.inf
        for step in steps[third:]:
            theta = sign*step["joints"][str(joint)][2]
            if theta < previous:
                break
            previous = theta
            intercepts.append(sign*step["support_rows"][footing_row][0]-k3*theta)
        intercept = sum(intercepts)/len(intercepts)
        theta1 = m1/k1
        result["Mmd"] = (intercept+k3*theta1-k3*m1/k2)/(1-k3/k2)
        result["Mmd_states"] = len(intercepts)
        result["Mmd_intercept_spread"] = max(intercepts)-min(intercepts)
    result["max_abs_moment"] = max(abs(step["support_rows"][footing_row][0]) for step in steps)
    return result


def main(raw_path: Path, out_path: Path, ndt_path: Path) -> None:
    raw = json.loads(raw_path.read_text(encoding="utf-8"))
    ndt = read_ndt(ndt_path)
    mtheta = _mtheta_element(ndt)
    rows = raw["lengths"]["support_info"]-1
    steps = []
    for step in raw["steps"]:
        joints = {str(j["joint"]): [_round(j[k]) for k in ("dx", "dy", "rz")] for j in step["joints"]}
        members = {str(m["element"]): [_round(m[k]) for k in ("Mi", "Ni", "Si", "Mj", "Nj", "Sj")]
                   for m in step["members"]}
        element = next(m for m in step["members"] if m["element"] == mtheta)
        # JointItem's spring force and flag are indexed by support row, not by joint.
        support_rows = [[_round(j["jiban_hanryoku"]), int(j["bane_flg"])]
                        for j in step["joints"][:rows]]
        steps.append({
            "step": step["index"], "lo_step_no": step["lo_step_no"],
            "joints": joints, "members": members, "support_rows": support_rows,
            "mtheta": {"SCFlg": element["SCFlg"], "moment": _round(element["Outou_M"]),
                       "theta": _round(element["Outou_FAI_Kaiseki"])},
        })
    skeleton = next(m for m in raw["steps"][0]["members"] if m["element"] == mtheta)
    footing_row = len(ndt.springs)  # the springs come first, the footing row last
    kihon = raw["kihon"]
    reference = {
        "source": {"ndt": ndt_path.name, "jrsnap": "{Major}.{Minor}.{Revision}.{Revision2}".format(**kihon["Version"])},
        "kihon": {k: kihon[k] for k in ("DDV", "DSMAX", "JNo", "MDV", "Limit3", "JALF", "Rhom", "Alphaf", "LastStep")},
        "mtheta": {"element": mtheta,
                   "M": [abs(skeleton["M_Kaiseki_S"][f"M{i}"]) for i in range(1, 5)],
                   "theta": [abs(skeleton["FAI_Kaiseki_S"][f"Fai{i}"]) for i in range(1, 5)]},
        "foundation_spring": _foundation(ndt, steps, footing_row),
        "steps": steps,
    }
    out_path.write_text(json.dumps(reference, ensure_ascii=False, indent=None, separators=(",", ":")) + "\n",
                        encoding="utf-8")


if __name__ == "__main__":
    main(Path(sys.argv[1]), Path(sys.argv[2]), Path(sys.argv[3]))
