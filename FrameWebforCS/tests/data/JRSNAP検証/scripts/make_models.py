"""Write the FrameWeb input (legacy JSON, dimension 2) of each P7 JRSNAP case.

Usage (repository root):
    uv run --project FrameWeb --locked python <this folder>/make_models.py

For every jrsnap/<case>.ndt with reference/<case>.json this writes
models/<case>.json. The translation rules are listed in README.md; in short:

- Coordinates, displacements and rotations keep JRSNAP's 2D convention
  (y downward), which is also FrameWeb's dimension-2 convention.
- Each LOAD case becomes one basic load case whose kind follows its rows'
  increment flag (0 fixed, 1 incremental, 4 limited with
  limit = LiMiT3/MDV*JALF, where both programs reach the full input value).
  One nonlinear_combine row applies every case with coefficient 1 under
  displacement control of the control joint.
- Lambda is JRSNAP's lo_step_no/MDV*JALF, so the incremental loads are their
  input values at lambda = 1.
- Member loads on the M-theta element are left out: JRSNAP does not apply them.
- The M-theta element is a 0.010 m nonlinear_bar whose moment-curvature
  skeleton is JRSNAP's stored M-theta skeleton with curvature = theta/length.
- The footing joint keeps the linear X and Y springs, and gets a jrsnap_trilinear
  rotational spring: K1 = kv*B^3*L/12, M1 = N*B/6, K2 = K1/KR2, K3 = K1/KR3,
  with the second break moment Mmd measured from the stored states (alpha_f = 1)
  or alpha_f times that of the Case with the same rho_m (alpha_f = 2, where
  JRSNAP never reaches it).
"""

from __future__ import annotations

import json
import math
from pathlib import Path

from ndt import FLAG_KINDS, read_ndt

ROOT = Path(__file__).resolve().parent.parent
CASES = [f"{direction}_Case{n}" for direction in ("線路方向", "線路直角方向") for n in (1, 2, 3, 4)]
# The Case with the same rho_m and alpha_f = 1 whose stored states give Mmd.
MMD_SOURCE = {"Case1": "Case1", "Case2": "Case2", "Case3": "Case1", "Case4": "Case2"}
RIGID_NU = 0.2
MTHETA_AXIAL_STIFFNESS = 1.0e9  # E*A of JRSNAP's M-theta element (measured, see README)
TOLERANCE = 1e-6  # FrameWeb's default; 1e-8 is below the rounding floor of the 0.010 m element
MAX_ITERATIONS = 200


def _kind(case) -> dict:
    flags = {row.flag for row in (*case.node_loads, *case.member_loads)}
    if len(flags) != 1:
        raise ValueError(f"LOAD case {case.number} mixes increment flags {sorted(flags)}")
    return {"kind": FLAG_KINDS[flags.pop()]}


def _member_load(element: int, row, joints, ends) -> dict:
    (xi, yi), (xj, yj) = joints[ends[0]], joints[ends[1]]
    if not math.isclose(xi, xj) or yi >= yj:
        raise ValueError(f"Member loads are translated for columns with i above j only (element {element})")
    if row.load_type == 4:      # parallel to the axis, i -> j positive: downward on a column
        direction, w_i, w_j = "gy", row.w_i, row.w_j
    elif row.load_type == 1:    # perpendicular; a positive value acts in -X on a column (measured)
        direction, w_i, w_j = "gx", -row.w_i, -row.w_j
    else:
        raise ValueError(f"Unsupported member load type iT={row.load_type}")
    return {"m": element, "mark": 2, "direction": direction, "L1": 0, "L2": 0, "P1": w_i, "P2": w_j}


def _rotational_spring(ndt, reference, mmd_reference) -> dict:
    footing = ndt.footing
    k1 = footing["kv"]*footing["B"]**3*footing["L"]/12
    vertical = sum(row.N for case in ndt.cases for row in case.node_loads if FLAG_KINDS[row.flag] == "fixed")
    mtheta = next(n for n, e in ndt.elements.items() if e[2] == 1)
    for case in ndt.cases:
        for row in case.member_loads:
            if FLAG_KINDS[row.flag] == "fixed" and row.load_type == 4 and row.element != mtheta:
                i, j = ndt.elements[row.element][:2]
                length = abs(ndt.joints[j][1]-ndt.joints[i][1])
                vertical += (row.w_i+row.w_j)/2*length
    m1 = vertical*footing["B"]/6
    measured = mmd_reference["foundation_spring"]["Mmd"]
    mmd = measured*ndt.alpha_f
    if ndt.alpha_f != 1 and mmd <= reference["foundation_spring"]["max_abs_moment"]:
        # Only a value JRSNAP never reached can stand in for its unknown alpha_f rule.
        raise ValueError("The provisional Mmd lies inside the moments JRSNAP reached")
    side = {"K1": k1, "F1": m1, "K2": k1/footing["KR2"], "F2": mmd, "K3": k1/footing["KR3"]}
    return {"type": "jrsnap_trilinear",
            **{f"{key}_{s}": value for s in ("pos", "neg") for key, value in side.items()}}


def make_model(case_name: str) -> dict:
    ndt = read_ndt(ROOT/"jrsnap"/f"{case_name}.ndt")
    reference = json.loads((ROOT/"reference"/f"{case_name}.json").read_text(encoding="utf-8"))
    direction, case = case_name.split("_")
    mmd_source = f"{direction}_{MMD_SOURCE[case]}"
    mmd_reference = json.loads((ROOT/"reference"/f"{mmd_source}.json").read_text(encoding="utf-8"))

    mtheta = reference["mtheta"]
    mtheta_element = mtheta["element"]
    i, j = ndt.elements[mtheta_element][:2]
    length = math.dist(ndt.joints[i], ndt.joints[j])
    initial = mtheta["M"][0]/mtheta["theta"][0]*length  # E*I on the first skeleton slope
    materials = {"1": {"E": None, "A": None, "Iy": None, "Iz": None, "J": 1.0, "nu": RIGID_NU}}
    rigid = {element[3:] for element in ndt.elements.values() if element[2] == 0}
    if len(rigid) != 1:
        raise ValueError(f"Expected one linear section, got {sorted(rigid)}")
    ((area, inertia, young),) = rigid
    materials["1"].update(E=young*1e6, A=area, Iy=inertia, Iz=inertia)
    materials["2"] = {
        "E": initial, "A": MTHETA_AXIAL_STIFFNESS/initial, "Iy": 1.0, "Iz": 1.0, "J": 1.0, "nu": RIGID_NU,
        "nonlinear": {
            "type": "jr_stiffness_reduction", "symmetric": True, "hysteresis_dofs": ["moment_z"],
            **{f"delta_{k+1}": mtheta["theta"][k]/length for k in range(4)},
            **{f"P_{k+1}": mtheta["M"][k] for k in range(4)},
        },
    }

    load = {}
    for case in ndt.cases:
        kind = _kind(case)
        if kind["kind"] == "limited":
            kind["limit"] = ndt.limit3/ndt.mdv*ndt.jalf
        load[str(case.number)] = {
            "name": case.name, "symbol": str(case.number), **kind,
            "load_node": [{"n": row.node, "tx": row.H, "ty": row.N, "rz": row.M} for row in case.node_loads],
            "load_member": [_member_load(row.element, row, ndt.joints, ndt.elements[row.element][:2])
                            for row in case.member_loads if row.element != mtheta_element],
        }

    footing_joint = ndt.footing_joint
    support = {"n": footing_joint, "tx": ndt.springs[footing_joint, 1], "ty": ndt.springs[footing_joint, 2],
               "rz": _rotational_spring(ndt, reference, mmd_reference)}
    increment = ndt.target/ndt.steps
    return {
        "dimension": 2,
        "title": f"P7 {ndt.title} {case_name.split('_')[1]} (rho_m {ndt.rho_m}, alpha_f {ndt.alpha_f})",
        "node": {str(n): {"x": x, "y": y, "z": 0.0} for n, (x, y) in sorted(ndt.joints.items())},
        "member": {str(n): {"ni": e[0], "nj": e[1], "e": 2 if e[2] == 1 else 1}
                   for n, e in sorted(ndt.elements.items())},
        "element": {"1": materials},
        "fix_node": {"1": [support]},
        "load": load,
        "nonlinear_combine": {"1": {
            "row": 1, "name": case_name,
            **{f"C{case.number}": 1 for case in ndt.cases},
            "displacement_control": {"node": ndt.control_joint, "dof": "dx",
                                     "targets": [increment*k for k in range(1, ndt.steps+1)]},
            "tolerance": TOLERANCE, "max_iterations": MAX_ITERATIONS,
        }},
    }


def main() -> None:
    (ROOT/"models").mkdir(exist_ok=True)
    for case_name in CASES:
        model = make_model(case_name)
        path = ROOT/"models"/f"{case_name}.json"
        path.write_text(json.dumps(model, ensure_ascii=False, indent=1) + "\n", encoding="utf-8")
        print(path.relative_to(ROOT))


if __name__ == "__main__":
    main()
