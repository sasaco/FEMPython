"""Solve each P7 model with FrameWeb and compare every step with JRSNAP.

Usage (repository root):
    uv run --project FrameWeb --locked python <this folder>/compare.py [case ...]

Writes results/<case>.json (the step table) and results/summary.json (the
largest differences per case). Both programs reach the same control
displacement at every step, so the states are compared step by step.
A difference is |FrameWeb - JRSNAP| divided by the largest |JRSNAP| value of
that quantity over the analysis (quantities pass through zero while loading).
"""

from __future__ import annotations

import json
import sys
import time
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT.parents[3]/"FrameWeb"/"src"))

from fem.file_io import _read_json_model  # noqa: E402
from fem.model import FemModel  # noqa: E402
from fem.nonlinear_combine import select_combination  # noqa: E402

from make_models import CASES  # noqa: E402

TOLERANCE = 0.01


def solve(model: dict) -> list[dict]:
    fem = FemModel()
    fem.read_json_model(_read_json_model(select_combination(model, "1")))
    return fem._run_solver_snapshot()["step_results"]


def _footing_member(model: dict, joint: int) -> str:
    (member,) = [key for key, m in model["member"].items() if m["nj"] == joint]
    return member


def table(model: dict, reference: dict, states: list[dict]) -> dict[str, list]:
    """Step series in JRSNAP's signs (FrameWeb's section and reaction signs mapped)."""
    support = model["fix_node"]["1"][0]["n"]
    control = model["nonlinear_combine"]["1"]["displacement_control"]["node"]
    mtheta = str(reference["mtheta"]["element"])
    length = abs(model["node"][str(model["member"][mtheta]["nj"])]["y"]
                 - model["node"][str(model["member"][mtheta]["ni"])]["y"])
    footing_member = _footing_member(model, support)
    top = min(model["node"], key=lambda n: model["node"][n]["y"])
    rows = {name: [] for name in (
        "step", "lambda_frameweb", "lambda_jrsnap", "control_dx", "top_dx",
        "base_shear_frameweb", "base_shear_jrsnap",
        "foundation_moment_frameweb", "foundation_moment_jrsnap",
        "foundation_rotation_frameweb", "foundation_rotation_jrsnap",
        "foundation_branch_frameweb", "foundation_branch_jrsnap",
        "mtheta_moment_frameweb", "mtheta_moment_jrsnap",
        "mtheta_rotation_frameweb", "mtheta_rotation_jrsnap", "mtheta_state_jrsnap",
        "top_dx_frameweb", "top_dx_jrsnap")}
    kihon = reference["kihon"]
    for state, jr in zip(states, reference["steps"], strict=True):
        section = state["section_response"][int(mtheta)]["center"]["z"]
        spring = state["support_response"][support]["rz"]
        rows["step"].append(jr["step"])
        rows["lambda_frameweb"].append(state["lambda"])
        rows["lambda_jrsnap"].append(jr["lo_step_no"]/kihon["MDV"]*kihon["JALF"])
        rows["control_dx"].append(jr["joints"][str(control)][0])
        # Support row 1 is the horizontal spring; its force is minus the reaction.
        rows["base_shear_frameweb"].append(-state["reaction_forces"][support]["fx"])
        rows["base_shear_jrsnap"].append(jr["support_rows"][0][0])
        rows["foundation_moment_frameweb"].append(spring["force"])
        rows["foundation_moment_jrsnap"].append(jr["members"][footing_member][3])
        rows["foundation_rotation_frameweb"].append(state["node_displacements"][support]["rz"])
        rows["foundation_rotation_jrsnap"].append(jr["joints"][str(support)][2])
        rows["foundation_branch_frameweb"].append(spring["branch"])
        rows["foundation_branch_jrsnap"].append(jr["support_rows"][2][1])
        rows["mtheta_moment_frameweb"].append(-section["moment"])
        rows["mtheta_moment_jrsnap"].append(jr["mtheta"]["moment"])
        rows["mtheta_rotation_frameweb"].append(-section["curvature"]*length)
        rows["mtheta_rotation_jrsnap"].append(jr["mtheta"]["theta"])
        rows["mtheta_state_jrsnap"].append(jr["mtheta"]["SCFlg"])
        rows["top_dx_frameweb"].append(state["node_displacements"][int(top)]["dx"])
        rows["top_dx_jrsnap"].append(jr["joints"][top][0])
    del rows["top_dx"]
    return rows


def _inverse_skeleton(moment: float, skeleton: dict) -> float:
    """Rotation of a moment on the rising part of the M-theta skeleton (to M3)."""
    points = [(0.0, 0.0), *zip(skeleton["M"][:3], skeleton["theta"][:3])]
    for (m0, t0), (m1, t1) in zip(points, points[1:]):
        if moment <= m1:
            return t0 + (moment-m0)*(t1-t0)/(m1-m0)
    raise ValueError("moment beyond M3")


def _first(values, condition):
    return next((i for i, value in enumerate(values) if condition(value)), None)


def summarize(rows: dict[str, list], reference: dict) -> dict:
    result = {}
    for name in ("base_shear", "foundation_moment", "foundation_rotation", "mtheta_moment",
                 "mtheta_rotation", "top_dx"):
        fw, jr = rows[f"{name}_frameweb"], rows[f"{name}_jrsnap"]
        peak = max(abs(v) for v in jr)
        errors = [abs(a-b)/peak for a, b in zip(fw, jr)]
        worst = max(range(len(errors)), key=errors.__getitem__)
        result[name] = {"max_difference": errors[worst], "at_step": rows["step"][worst],
                        "final_frameweb": fw[-1], "final_jrsnap": jr[-1], "jrsnap_peak": peak}
    theta = reference["mtheta"]["theta"]
    # How far each program's rotation lies from the rotation its own moment has on
    # the skeleton (rising branches only, where the skeleton can be inverted).
    peak = result["mtheta_rotation"]["jrsnap_peak"]
    for program in ("frameweb", "jrsnap"):
        deviations = [abs(abs(t) - _inverse_skeleton(abs(m), reference["mtheta"]))/peak
                      for m, t in zip(rows[f"mtheta_moment_{program}"], rows[f"mtheta_rotation_{program}"])
                      if abs(t) <= theta[2] and abs(m) <= reference["mtheta"]["M"][2]]
        result["mtheta_rotation"][f"{program}_off_own_skeleton"] = max(deviations)
    result["events"] = {
        f"mtheta_past_point_{k+1}": {
            "frameweb": _first(rows["mtheta_rotation_frameweb"], lambda v, t=theta[k]: abs(v) > t),
            "jrsnap": _first(rows["mtheta_rotation_jrsnap"], lambda v, t=theta[k]: abs(v) > t)}
        for k in (0, 1, 2)}
    result["events"]["foundation_second_branch"] = {
        "frameweb": _first(rows["foundation_branch_frameweb"], lambda b: b != "elastic"),
        "jrsnap": _first(rows["foundation_branch_jrsnap"], lambda f: f != 0)}
    result["peak_base_shear"] = {
        "frameweb": max(rows["base_shear_frameweb"], key=abs),
        "jrsnap": max(rows["base_shear_jrsnap"], key=abs)}
    result["within_tolerance"] = all(result[name]["max_difference"] <= TOLERANCE
                                     for name in ("base_shear", "foundation_moment", "foundation_rotation",
                                                  "mtheta_moment", "mtheta_rotation", "top_dx"))
    return result


def main(names: list[str]) -> None:
    (ROOT/"results").mkdir(exist_ok=True)
    summary_path = ROOT/"results"/"summary.json"
    summary = json.loads(summary_path.read_text(encoding="utf-8")) if summary_path.exists() else {}
    for name in names:
        model = json.loads((ROOT/"models"/f"{name}.json").read_text(encoding="utf-8"))
        reference = json.loads((ROOT/"reference"/f"{name}.json").read_text(encoding="utf-8"))
        start = time.perf_counter()
        states = solve(model)
        seconds = time.perf_counter()-start
        rows = table(model, reference, states)
        (ROOT/"results"/f"{name}.json").write_text(json.dumps(rows, ensure_ascii=False) + "\n", encoding="utf-8")
        summary[name] = {**summarize(rows, reference), "seconds": round(seconds, 1)}
        print(name, "within 1%:", summary[name]["within_tolerance"],
              {k: round(v["max_difference"]*100, 3) for k, v in summary[name].items()
               if isinstance(v, dict) and "max_difference" in v}, f"{seconds:.1f}s")
    summary_path.write_text(json.dumps(dict(sorted(summary.items())), ensure_ascii=False, indent=1) + "\n",
                            encoding="utf-8")


if __name__ == "__main__":
    main(sys.argv[1:] or CASES)
