"""Steps 3 and 4 of the JRSNAP equivalence record: fixed M-phi members, alone and with a rotational spring.

Goldens (JRSNAP 5.2-L03, accuracy-priority method; tests/data/jrsnap_golden/member_*, combined01):

- ``member_mphi01_moment``: a 1 m column with one fixed M-phi element (M = 10, 16, 22, 14 kN*m at phi = 0.001,
  0.004, 0.010, 0.018; E*I = M1/phi1 = 1e4) under a tip moment, load increments to 21 kN*m. The moment is constant
  along the element, so the evaluation position plays no part. FrameWeb's section response lies exactly on the
  skeleton; JRSNAP's reported curvature deviates from the same skeleton by up to 0.395% (its .LOG shows the
  stiffness-reduction trials stop at 10 per step, 未収斂 at steps 49-51). Recorded as "does not match".
- ``member_mphi02_disp``: the same section as a 0.1 m element at the base of a 1 m column, tip X displacement
  increment. JRSNAP evaluates the section at the element end with the larger moment (``Outou_M`` = ``Mj``) and
  reports the end curvature of a linear curvature field (``FAI`` = rz2 / (0.95 * 0.1)); FrameWeb evaluates the
  element midpoint (curvature = rz2 / 0.1). The rotations agree (kinematics of the rigid upper part), the shear
  does not.
- ``member_beam001``: beam001's layout (9.8 + 0.1 + 0.1 m) with the fixed M-phi table of beam001.ndu and E*I =
  M1/phi1; the same evaluation-position difference.
- ``combined01``: member_mphi01 with a tri-linear rotational spring at the base. The section matches FrameWeb to
  3.1e-4; the spring force lags the load by up to 0.454 kN*m (JRSNAP .LOG: 支点状態未収斂 in 147 trials).

Tolerances were fixed before the comparison (docs/report/jrsnap-frameweb-equivalence.md §3): curvature,
rotation and displacement relative 2e-3 (FTM = 0.001 runs; the 1.0E-08 edit changed nothing), moments 1e-6 kN*m,
segment flags exact. FrameWeb's own skeleton check uses 1e-10 (Newton to 1e-10).
"""

import json

import pytest

from fem.file_io import _read_json_model
from fem.model import FemModel
from tests.support.paths import DATA

pytestmark = [pytest.mark.integration, pytest.mark.oracle, pytest.mark.material_nonlinear]

GOLDEN = DATA / "jrsnap_golden"
E = 2.5e7
MPHI = dict(type="jr_stiffness_reduction", delta_1=0.001, delta_2=0.004, delta_3=0.010, delta_4=0.018,
            P_1=10., P_2=16., P_3=22., P_4=14., symmetric=True, hysteresis_dofs=["moment_z"])
POINTS = ((10., 0.001), (16., 0.004), (22., 0.010), (14., 0.018))
BEAM001_POINTS = ((1000., 1e-5), (3000., 1e-4), (5000., 1e-3), (3000., 2e-3))
ROTATION = dict(type="jrsnap_trilinear", K1_pos=5000., F1_pos=8., K2_pos=1000., F2_pos=12., K3_pos=100.,
                K1_neg=5000., F1_neg=8., K2_neg=1000., F2_neg=12., K3_neg=100.)
REL = 2e-3


def skeleton(moment, points):
    """Hand calculation: curvature on the tetra-linear skeleton (K4 continues past the 4th point)."""
    (M1, p1), (M2, p2), (M3, p3), (M4, p4) = points
    if moment <= M1:
        return moment * p1 / M1
    if moment <= M2:
        return p1 + (moment - M1) * (p2 - p1) / (M2 - M1)
    if moment <= M3:
        return p2 + (moment - M2) * (p3 - p2) / (M3 - M2)
    return p3 + (moment - M3) * (p4 - p3) / (M4 - M3)


def moment_of(kappa, points):
    """Hand calculation: the skeleton moment at a curvature (single-valued, unlike the inverse past the peak)."""
    (M1, p1), (M2, p2), (M3, p3), (M4, p4) = points
    if kappa <= p1:
        return kappa * M1 / p1
    if kappa <= p2:
        return M1 + (kappa - p1) * (M2 - M1) / (p2 - p1)
    if kappa <= p3:
        return M2 + (kappa - p2) * (M3 - M2) / (p3 - p2)
    return M3 + (kappa - p3) * (M4 - M3) / (p4 - p3)


def _golden(case):
    return json.loads((GOLDEN / case / "golden_n100.json").read_text(encoding="utf-8"))


def _run(raw):
    model = FemModel()
    model.read_json_model(_read_json_model(raw))
    return model._run_solver_snapshot()


def _column(nonlinear_element, load, analysis):
    """A 1 m column, node 1 at the tip, node 2 at the base."""
    return {
        "dimension": 2,
        "node": {"1": {"x": 0., "y": -1., "z": 0.}, "2": {"x": 0., "y": 0., "z": 0.}},
        "member": {"1": {"ni": 1, "nj": 2, "e": 1}},
        "element": {"1": {"1": nonlinear_element}},
        "fix_node": {"1": [dict(n=2, tx=1, ty=1, rz=1)]},
        "load": {"1": {"fix_node": 1, "load_node": [dict(n=1, **load)]}},
        "analysis_params": analysis,
    }


SECTION = {"E": E, "A": 1000., "Iy": 0.0004, "Iz": 0.0004, "J": 1., "nu": .2, "nonlinear": MPHI}
LOAD_STEPS = {"load_factors": [k / 100 for k in range(1, 101)], "tolerance": 1e-10, "max_iterations": 200}


# --- 3a: constant moment, load increments --------------------------------------------------------------------

def test_frameweb_section_lies_on_the_skeleton_under_a_constant_moment():
    result = _run(_column(SECTION, dict(rz=21.), LOAD_STEPS))
    assert len(result["step_results"]) == 100
    for step in result["step_results"]:
        moment = 21. * step["step"] / 100
        kappa = skeleton(moment, POINTS)
        section = step["section_response"][1]["center"]["z"]
        assert abs(section["moment"]) == pytest.approx(moment, rel=1e-10)
        assert abs(step["curvature"][1]["z"]) == pytest.approx(kappa, rel=1e-10)
        assert step["node_displacements"][1]["rz"] == pytest.approx(kappa, rel=1e-10)       # kappa * L
        assert step["node_displacements"][1]["dx"] == pytest.approx(kappa / 2, rel=1e-10)   # kappa * L^2 / 2


def test_jrsnap_section_deviates_from_its_skeleton_after_the_first_break_point():
    """Measured: JRSNAP's response curvature is below the skeleton by 0.395% at step 48 (M = 10.08, just past
    M1) and by 0.086% at step 100; the tip rotation is 0.497% below kappa * L at step 48. Its member state SCFlg
    is 1 up to M2 and 2 beyond (1 = before yield, 2 = after yield: it does not mark the first break point)."""
    golden = _golden("member_mphi01_moment")
    assert golden["member_info"][0]["E"] == 25000000 and golden["member_info"][0]["I"] == 0.0004
    assert golden["member_info"][0]["M"] == 1  # linked to M-phi table 1
    final = golden["steps"][100]["elements"]["1"]
    assert final["M_Kaiseki_S"] == {"M1": 10, "M2": 16, "M3": 22, "M4": 14}
    assert final["FAI_Kaiseki_S"] == pytest.approx({"Fai1": 0.001, "Fai2": 0.004, "Fai3": 0.01, "Fai4": 0.018}, rel=1e-12)
    worst_curvature = worst_rotation = (0.0, None)
    flags = []
    for step in golden["steps"][1:]:
        element = step["elements"]["1"]
        moment = 21. * step["step"] / 100
        kappa = skeleton(moment, POINTS)
        assert element["Mi"] == pytest.approx(moment, abs=1e-6) and element["Mj"] == pytest.approx(moment, abs=1e-6)
        assert element["Outou_M"] == pytest.approx(moment, abs=1e-6)
        worst_curvature = max(worst_curvature, (abs(element["Outou_FAI_Kaiseki"] - kappa) / kappa, step["step"]))
        worst_rotation = max(worst_rotation, (abs(step["joints"]["1"]["rz"] - kappa) / kappa, step["step"]))
        flags.append(element["SCFlg"])
    assert worst_curvature[1] == 48 and worst_curvature[0] == pytest.approx(0.00395, abs=5e-5)
    assert worst_rotation[1] == 48 and worst_rotation[0] == pytest.approx(0.00497, abs=5e-5)
    assert worst_curvature[0] > REL  # does not match within the fixed tolerance
    assert flags[:76] == [1] * 76 and flags[76:] == [2] * 24
    final_kappa = golden["steps"][100]["elements"]["1"]["Outou_FAI_Kaiseki"]
    assert final_kappa == pytest.approx(0.009, rel=1e-3) and final_kappa != pytest.approx(0.009, rel=1e-6)


# --- 3b: 0.1 m element at the base, tip displacement increment -----------------------------------------------

def test_jrsnap_evaluates_the_base_element_at_its_end_and_frameweb_at_its_midpoint():
    golden = _golden("member_mphi02_disp")
    assert golden["kihon"]["DSMAX"] == 0.002 and golden["kihon"]["JNo"] == 1 and golden["kihon"]["DDV"] == 100
    steps = golden["steps"][1:]
    for step in steps:
        element = step["elements"]["2"]
        assert element["Outou_M"] == pytest.approx(element["Mj"], abs=1e-9)               # the end with the larger moment
        assert element["Mi"] == pytest.approx(0.9 * element["Mj"], rel=1e-6)                # linear moment, V * 0.9 / V * 1.0
        assert element["Outou_FAI_Kaiseki"] == pytest.approx(step["joints"]["2"]["rz"] / 0.095, rel=1e-5)  # to 3e-6
    # On the second branch (step 10) the end moment is the skeleton moment of the reported curvature.
    tenth = steps[9]["elements"]["2"]
    assert tenth["Outou_M"] == pytest.approx(10 + (tenth["Outou_FAI_Kaiseki"] - 0.001) * 2000, rel=1e-6)
    peak = max(steps, key=lambda s: s["elements"]["2"]["Si"])
    assert peak["step"] == 48 and peak["elements"]["2"]["Si"] == pytest.approx(21.904, abs=1e-3)
    assert steps[-1]["elements"]["2"]["Si"] == pytest.approx(10.574, abs=1e-3)  # descending branch reached
    assert [s["elements"]["2"]["SCFlg"] for s in steps][-1] == 4

    raw = {
        "dimension": 2,
        "node": {"1": {"x": 0., "y": -1., "z": 0.}, "2": {"x": 0., "y": -0.1, "z": 0.}, "3": {"x": 0., "y": 0., "z": 0.}},
        "member": {"1": {"ni": 1, "nj": 2, "e": 1}, "2": {"ni": 2, "nj": 3, "e": 2}},
        "element": {"1": {"1": {"E": E, "A": 1000., "Iy": 1000., "Iz": 1000., "J": 1., "nu": .2}, "2": SECTION}},
        "fix_node": {"1": [dict(n=3, tx=1, ty=1, rz=1)]},
        "load": {"1": {"fix_node": 1, "load_node": [dict(n=1, tx=10.)]}},
        "analysis_params": {"displacement_control": {"node": 1, "dof": "dx", "targets": [0.00002 * k for k in range(1, 101)]},
                            "tolerance": 1e-8, "max_iterations": 200},
    }
    result = _run(raw)
    assert len(result["step_results"]) == 100
    worst_shear = (0.0, None)
    for step, jr in zip(result["step_results"], steps):
        section = step["section_response"][2]["center"]["z"]
        shear = step["lambda"] * 10.
        # FrameWeb: midpoint moment V * 0.95 on the skeleton (past the peak the inverse is two-valued, so the
        # moment is checked at the curvature), uniform curvature rz2 / 0.1.
        assert abs(section["moment"]) == pytest.approx(shear * 0.95, rel=1e-8)
        assert abs(section["moment"]) == pytest.approx(moment_of(abs(step["curvature"][2]["z"]), POINTS), rel=1e-8)
        assert step["node_displacements"][2]["rz"] == pytest.approx(abs(step["curvature"][2]["z"]) * 0.1, rel=1e-8)
        # The rotation at the top of the base element is set by the prescribed tip displacement in both programs.
        assert step["node_displacements"][2]["rz"] == pytest.approx(jr["joints"]["2"]["rz"], rel=1e-5)
        # JRSNAP's end curvature is 1/0.95 of FrameWeb's midpoint curvature.
        assert jr["elements"]["2"]["Outou_FAI_Kaiseki"] == pytest.approx(abs(step["curvature"][2]["z"]) / 0.95, rel=1e-5)
        worst_shear = max(worst_shear, (abs(shear - jr["elements"]["2"]["Si"]) / jr["elements"]["2"]["Si"], jr["step"]))
    assert worst_shear[0] > REL  # the shear does not match: 8.4% at step 100
    assert worst_shear == (pytest.approx(0.0918, abs=1e-3), 100)


# --- 3c: beam001 under the same conditions -------------------------------------------------------------------

def test_beam001_jrsnap_end_moment_is_4950_and_frameweb_midpoint_4925():
    golden = _golden("member_beam001")
    assert golden["member_info"][1] == dict(golden["member_info"][1], A=1000, E=25000000, I=4, M=1, Itan=2, Jtan=3)
    steps = golden["steps"][1:]
    worst_law = (0.0, None)
    for step in steps:
        element = step["elements"]["2"]
        load = 5. * step["step"]
        assert element["Mj"] == pytest.approx(load * 9.9, rel=1e-8)
        assert element["Mi"] == pytest.approx(load * 9.8, rel=1e-8)
        assert element["Outou_M"] == pytest.approx(element["Mj"], abs=1e-9)
        worst_law = max(worst_law, (abs(element["Outou_FAI_Kaiseki"] - skeleton(element["Mj"], BEAM001_POINTS))
                                    / skeleton(element["Mj"], BEAM001_POINTS), step["step"]))
    assert steps[-1]["elements"]["2"]["Outou_M"] == pytest.approx(4950., abs=1e-6)
    assert steps[-1]["elements"]["2"]["Outou_FAI_Kaiseki"] == pytest.approx(0.000981706, rel=1e-5)
    assert steps[-1]["joints"]["1"]["dx"] == pytest.approx(0.00096764, rel=1e-4)
    assert worst_law == (pytest.approx(0.01103, abs=1e-4), 61)  # JRSNAP is 1.1% off its own skeleton just past M2

    nonlinear = dict(type="jr_stiffness_reduction", delta_1=1e-5, delta_2=1e-4, delta_3=1e-3, delta_4=2e-3,
                     P_1=1000., P_2=3000., P_3=5000., P_4=3000., symmetric=True, hysteresis_dofs=["moment_z"])
    raw = {
        "dimension": 2,
        "node": {"1": {"x": 0., "y": -10., "z": 0.}, "2": {"x": 0., "y": -0.2, "z": 0.}, "3": {"x": 0., "y": -0.1, "z": 0.},
                 "4": {"x": 0., "y": 0., "z": 0.}},
        "member": {"1": {"ni": 1, "nj": 2, "e": 1}, "2": {"ni": 2, "nj": 3, "e": 2}, "3": {"ni": 3, "nj": 4, "e": 1}},
        "element": {"1": {"1": {"E": 2.65e7, "A": 1000., "Iy": 1000., "Iz": 1000., "J": 1., "nu": .2},
                          "2": {"E": E, "A": 1000., "Iy": 4., "Iz": 4., "J": 1., "nu": .2, "nonlinear": nonlinear}}},
        "fix_node": {"1": [dict(n=4, tx=1, ty=1, rz=1)]},
        "load": {"1": {"fix_node": 1, "load_node": [dict(n=1, tx=500.)]}},
        "analysis_params": LOAD_STEPS,
    }
    result = _run(raw)
    assert len(result["step_results"]) == 100
    worst_dx = (0.0, None)
    for step, jr in zip(result["step_results"], steps):
        load = 5. * jr["step"]
        section = step["section_response"][2]["center"]["z"]
        assert abs(section["moment"]) == pytest.approx(load * 9.85, rel=1e-8)
        assert abs(step["curvature"][2]["z"]) == pytest.approx(skeleton(load * 9.85, BEAM001_POINTS), rel=1e-8)
        worst_dx = max(worst_dx, (abs(step["node_displacements"][1]["dx"] - jr["joints"]["1"]["dx"]) / jr["joints"]["1"]["dx"], jr["step"]))
    final = result["step_results"][-1]
    assert abs(final["section_response"][2]["center"]["z"]["moment"]) == pytest.approx(4925., rel=1e-8)
    assert final["node_displacements"][1]["dx"] == pytest.approx(0.00095787, rel=1e-4)
    assert worst_dx == (pytest.approx(0.0453, abs=1e-3), 61)
    assert worst_dx[0] > REL


# --- 4: member and rotational spring together ---------------------------------------------------------------

def test_combined_member_and_rotational_spring():
    golden = _golden("combined01")
    stored = golden["stored_supports"][0]
    assert (stored["joint"], stored["ik"], stored["K1_S"], stored["Rmit1_S"], stored["K2_S"], stored["Rmit2_S"], stored["K3_S"]) == (2, 3, 5000, 8, 1000, 12, 100)
    steps = golden["steps"][1:]
    raw = _column(SECTION, dict(rz=21.), LOAD_STEPS)
    raw["fix_node"] = {"1": [dict(n=2, tx=1, ty=1, rz=ROTATION)]}
    result = _run(raw)
    assert len(result["step_results"]) == 100
    worst_curvature = worst_spring = worst_rotation = (0.0, None)
    for step, jr in zip(result["step_results"], steps):
        moment = 21. * jr["step"] / 100
        element, spring = jr["elements"]["1"], jr["joints"]["1"]
        fw_spring = step["support_response"][2]["rz"]
        # FrameWeb: section and spring both on their skeletons at the applied moment.
        assert abs(step["curvature"][1]["z"]) == pytest.approx(skeleton(moment, POINTS), rel=1e-10)
        assert fw_spring["force"] == pytest.approx(moment, rel=1e-10)
        # JRSNAP: the moment is exact, the section curvature within 3.1e-4, the spring force lags the load.
        assert element["Mi"] == pytest.approx(moment, abs=1e-6)
        worst_curvature = max(worst_curvature, (abs(element["Outou_FAI_Kaiseki"] - abs(step["curvature"][1]["z"])) / abs(step["curvature"][1]["z"]), jr["step"]))
        worst_spring = max(worst_spring, (moment - spring["jiban_hanryoku"], jr["step"]))
        worst_rotation = max(worst_rotation, (abs(jr["joints"]["2"]["rz"] - step["node_displacements"][2]["rz"]) / step["node_displacements"][2]["rz"], jr["step"]))
    assert worst_curvature[0] < REL and worst_curvature == (pytest.approx(3.12e-4, abs=2e-5), 48)
    assert worst_spring == (pytest.approx(0.4539, abs=1e-3), 100)
    assert worst_rotation[0] > REL and worst_rotation == (pytest.approx(0.0476, abs=1e-3), 79)
    # The spring yields at step 39 (8.19 > 8 kN*m) and reaches its third segment at step 58 (12.18 > 12).
    assert [s["joints"]["1"]["bane_flg"] for s in steps][37:40] == [0, 1, 1]
    assert [s["joints"]["1"]["bane_flg"] for s in steps][56:59] == [1, 2, 2]
