"""Step 2 of the JRSNAP equivalence record: nodal support springs beyond the 2026-10-18 goldens.

Goldens (JRSNAP 5.2-L03, tests/data/jrsnap_golden/*, see tools/jrsnap_golden/make_cases.py for the models):

- ``support_trilinear01_f1``, ``support_trilinear02_neg_f1``: the frozen load-increment cases rerun with the
  accuracy-priority convergence method (column 70 of the .ndt = 1). JRSNAP still leaves an unbalanced force,
  so these rows are recorded as "does not match" with the measured residual; the tests below pin the measured
  JRSNAP behaviour so that the record stays verifiable.
- ``support_trilinear04_neg_disp`` (speed and accuracy priority): the negative side under a displacement
  increment. JRSNAP's member shear is the negative skeleton F_neg(|dx|); only its reported ``jiban_hanryoku``
  is F_neg(2|dx|). FrameWeb's law and displacement control match the member shear.
- ``support_bilinear01``: JRSNAP's ordinary (three-value) spring row is a bilinear spring with a flat plateau;
  it matches ``jrsnap_bilinear``.
- ``support_multi01``: three springs (two tri-linear, one ordinary) on two rigid members under a displacement
  increment; every spring, the axial forces and the load factor match.

Tolerances were fixed before the comparison (docs/report/jrsnap-frameweb-equivalence.md §3): law replay
relative 1e-12, global forces 1e-4 kN, control-node displacement 1e-9 m, spring-node displacement of the
FrameWeb model 1e-7 m (the FrameWeb member is E = 1e8 or EA = 2.65e9, so its own flexibility is 3e-8 m or less),
rigid-support displacements 1e-12 m, segments exact.
"""

import json

import pytest

from fem.file_io import _read_json_model
from fem.model import FemModel
from fem.nonlinear.hysteresis.jrsnap_support import JrsnapSupportState, evaluate_jrsnap_support, parse_jrsnap_support
from tests.support.paths import DATA

pytestmark = [pytest.mark.integration, pytest.mark.oracle]

GOLDEN = DATA / "jrsnap_golden"
TRILINEAR = dict(type="jrsnap_trilinear", K1_pos=1000., F1_pos=10., K2_pos=200., F2_pos=14., K3_pos=50.,
                 K1_neg=500., F1_neg=5., K2_neg=100., F2_neg=7., K3_neg=0.001)
BILINEAR = dict(type="jrsnap_bilinear", K1=1000., F1_pos=10., F1_neg=5.)
FORCE = 1e-4
LAW = dict(rel=1e-12, abs=1e-12)


def _golden(case, name):
    return json.loads((GOLDEN / case / name).read_text(encoding="utf-8"))


def _replay(definition, path):
    params = parse_jrsnap_support(definition)
    state = JrsnapSupportState.initial(params)
    states = []
    for dx in path:
        state = evaluate_jrsnap_support(params, state, dx)
        states.append(state)
    return states


def _segment(state, definition):
    """JRSNAP bane_flg: 0 elastic, ±1/±2 skeleton segments, ±1 for the plateau of an ordinary spring."""
    if state.branch == "elastic":
        return 0
    side = "pos" if state.branch.endswith("pos") else "neg"
    sign = 1 if side == "pos" else -1
    if state.branch == f"plateau_{side}":
        return sign
    K1, F1, K2, F2 = (definition[f"{k}_{side}"] for k in ("K1", "F1", "K2", "F2"))
    x2 = F1 / K1 + (F2 - F1) / K2
    return sign * (1 if abs(state.deformation) < x2 else 2)


def _rigid_member(definition, load, control=None, factors=None, steps=100):
    raw = {
        "dimension": 2,
        "node": {"1": {"x": 0.0, "y": -1.0, "z": 0.0}, "2": {"x": 0.0, "y": 0.0, "z": 0.0}},
        "member": {"1": {"ni": 1, "nj": 2, "e": 1}},
        "element": {"1": {"1": {"E": 1e8, "A": 1., "Iy": 1., "Iz": 1., "J": 1., "nu": .2}}},
        "fix_node": {"1": [dict(n=2, tx=definition, ty=1, rz=1)]},
        "load": {"1": {"fix_node": 1, "load_node": [dict(n=1, tx=load)]}},
        "analysis_params": {"n_load_steps": steps, "tolerance": 1e-8, "max_iterations": 200},
    }
    if control:
        raw["analysis_params"]["displacement_control"] = control
    if factors:
        raw["analysis_params"]["load_factors"] = factors
    model = FemModel()
    model.read_json_model(_read_json_model(raw))
    return model._run_solver_snapshot()


# --- 2a: load increments with the accuracy-priority method ------------------------------------------------

@pytest.mark.parametrize("case, name, load, residual, dx_ratio", [
    ("support_trilinear01_f1", "golden_n100.json", 20.0, 0.14278, 0.14714 / 0.15),
    ("support_trilinear01_f1", "golden_n1000.json", 20.0, 0.14644, 0.14707 / 0.15),
])
def test_accuracy_priority_still_leaves_an_unbalanced_force(case, name, load, residual, dx_ratio):
    """Measured JRSNAP behaviour (not a match): with column 70 = 1 the residual load minus spring force at the last
    step is 0.143 kN (n = 100) and 0.146 kN (n = 1000), down from 1.238 and 0.380 with the speed-priority method,
    and the displacement at P = 20 is 1.9% below the skeleton inverse (0.15 m). The member shear equals the load."""
    golden = _golden(case, name)
    assert golden["provenance"]["post_processing"] == {"convergence": "accuracy"}
    assert any(m["code"] == "CONVERGENCE_METHOD" and "精度優先" in m["text"] for m in golden["provenance"]["engine_messages"])
    n = golden["load_steps"]
    unbalance = [load * s["step"] / n - s["joints"]["1"]["jiban_hanryoku"] for s in golden["steps"][1:]]
    assert max(unbalance) == pytest.approx(residual, abs=1e-4)
    assert min(unbalance) > -1e-6
    last = golden["steps"][-1]
    assert last["elements"]["1"]["Si"] == pytest.approx(load, abs=FORCE)
    assert last["joints"]["2"]["dx"] == pytest.approx(0.15 * dx_ratio, rel=1e-4)
    # The reported spring force is still exactly the positive skeleton at the reported displacement.
    for state, step in zip(_replay(TRILINEAR, [s["joints"]["2"]["dx"] for s in golden["steps"][1:]]), golden["steps"][1:]):
        assert state.force == pytest.approx(step["joints"]["1"]["jiban_hanryoku"], **LAW)


def test_accuracy_priority_keeps_the_negative_side_report_doubled():
    golden = _golden("support_trilinear02_neg_f1", "golden_n100.json")
    definition = dict(TRILINEAR, K3_neg=20.)
    last = golden["steps"][-1]
    assert last["elements"]["1"]["Si"] == pytest.approx(-10.0, abs=FORCE)
    # Reported spring force = F_neg(2|dx|), load - reported = 3.258 kN at the last step.
    doubled = _replay(definition, [2 * s["joints"]["2"]["dx"] for s in golden["steps"][1:]])
    for state, step in zip(doubled, golden["steps"][1:]):
        assert state.force == pytest.approx(step["joints"]["1"]["jiban_hanryoku"], **LAW)
    assert -10.0 - last["joints"]["1"]["jiban_hanryoku"] == pytest.approx(3.2581, abs=1e-4)


# --- 2b: negative side under a displacement increment ------------------------------------------------------

@pytest.mark.parametrize("case", ["support_trilinear04_neg_disp", "support_trilinear04_neg_disp_f1"])
def test_negative_side_law_matches_the_jrsnap_member_shear(case):
    golden = _golden(case, "golden_n100.json")
    definition = dict(TRILINEAR, K3_neg=20.)
    steps = golden["steps"][1:]
    states = _replay(definition, [s["joints"]["2"]["dx"] for s in steps])
    for state, step in zip(states, steps):
        # The structure carries the negative skeleton F_neg(|dx|): the member shear Si is that force.
        assert state.force == pytest.approx(step["elements"]["1"]["Si"], abs=FORCE)
        assert _segment(state, definition) == step["joints"]["1"]["bane_flg"]
        # JRSNAP's report of the same spring is F_neg(2|dx|) (observed since 5.2-L03 case 02; unchanged here).
        assert step["joints"]["1"]["jiban_hanryoku"] == pytest.approx(
            _replay(definition, [2 * step["joints"]["2"]["dx"]])[0].force, **LAW)
    assert [s["joints"]["1"]["bane_flg"] for s in steps][:11] == [0] * 10 + [-1]
    assert steps[-1]["joints"]["1"]["bane_flg"] == -2
    assert steps[-1]["elements"]["1"]["Si"] == pytest.approx(-8.4, abs=FORCE)  # 7 + 20 * (0.1 - 0.03)


@pytest.mark.parametrize("case", ["support_trilinear04_neg_disp", "support_trilinear04_neg_disp_f1"])
def test_negative_side_displacement_control_matches_jrsnap(case):
    golden = _golden(case, "golden_n100.json")
    steps = golden["steps"][1:]
    result = _rigid_member(dict(TRILINEAR, K3_neg=20.), -10.0,
                           control={"node": 1, "dof": "dx", "targets": [-0.001 * k for k in range(1, 101)]})
    assert len(result["step_results"]) == 100
    for step, jr in zip(result["step_results"], steps):
        assert step["lambda"] * -10.0 == pytest.approx(jr["elements"]["1"]["Si"], abs=FORCE)
        assert step["support_response"][2]["x"]["force"] == pytest.approx(jr["elements"]["1"]["Si"], abs=FORCE)
        assert step["node_displacements"][1]["dx"] == pytest.approx(jr["joints"]["1"]["dx"], abs=1e-9)
        assert step["node_displacements"][2]["dx"] == pytest.approx(jr["joints"]["2"]["dx"], abs=1e-7)
        assert abs(jr["joints"]["2"]["dy"]) < 1e-12 and abs(jr["joints"]["2"]["rz"]) < 1e-12


def test_speed_and_accuracy_priority_give_the_same_displacement_increment_results():
    speed = _golden("support_trilinear04_neg_disp", "golden_n100.json")
    accuracy = _golden("support_trilinear04_neg_disp_f1", "golden_n100.json")
    for a, b in zip(speed["steps"], accuracy["steps"]):
        assert a["joints"] == b["joints"]
        assert {k: v for k, v in a["elements"]["1"].items() if not isinstance(v, dict)} == {
            k: v for k, v in b["elements"]["1"].items() if not isinstance(v, dict)}


# --- 2c: the ordinary (bilinear) spring row ----------------------------------------------------------------

def test_ordinary_spring_row_is_a_flat_bilinear_spring_and_matches_jrsnap_bilinear():
    golden = _golden("support_bilinear01", "golden_n100.json")
    stored = golden["stored_supports"][0]
    assert (stored["K1_S"], stored["Rmit1_S"], stored["K2_S"], stored["K3_S"]) == (1000, 10, 0, 0)
    assert stored["Rmit2_S"] > 1e14  # no second limit
    steps = golden["steps"][1:]
    states = _replay(BILINEAR, [s["joints"]["2"]["dx"] for s in steps])
    for state, step in zip(states, steps):
        # JRSNAP reports min(K dx, F1) exactly: the blank second and third slopes are not replaced by 0.01.
        assert step["joints"]["1"]["jiban_hanryoku"] == pytest.approx(min(1000 * step["joints"]["2"]["dx"], 10.0), **LAW)
        assert state.force == pytest.approx(step["joints"]["1"]["jiban_hanryoku"], **LAW)
        assert _segment(state, BILINEAR) == step["joints"]["1"]["bane_flg"]
        assert step["elements"]["1"]["Si"] == pytest.approx(step["joints"]["1"]["jiban_hanryoku"], abs=FORCE)
    assert [s["joints"]["1"]["bane_flg"] for s in steps][19:22] == [0, 1, 1]
    assert {state.branch for state in states} == {"elastic", "plateau_pos"}

    result = _rigid_member(BILINEAR, 20.0, control={"node": 1, "dof": "dx", "targets": [0.0005 * k for k in range(1, 101)]})
    assert len(result["step_results"]) == 100
    for step, jr in zip(result["step_results"], steps):
        assert step["lambda"] * 20.0 == pytest.approx(jr["elements"]["1"]["Si"], abs=FORCE)
        assert step["support_response"][2]["x"]["force"] == pytest.approx(jr["joints"]["1"]["jiban_hanryoku"], abs=FORCE)
        assert step["node_displacements"][1]["dx"] == pytest.approx(jr["joints"]["1"]["dx"], abs=1e-9)
        assert step["node_displacements"][2]["dx"] == pytest.approx(jr["joints"]["2"]["dx"], abs=1e-7)


# --- 2d: three springs on two rigid members ------------------------------------------------------------------

def test_three_springs_on_two_rigid_members_match_jrsnap():
    """Rows 1-3 of BLOCK6 are the X springs of joints 1-3 (tri-linear A, tri-linear with K3 = -20, ordinary), so
    that their forces and segments are readable; the rigid rows follow. The displacement increment is at joint 3.
    Expected values are JRSNAP's jiban_hanryoku per row, its axial forces and bane_flg."""
    golden = _golden("support_multi01", "golden_n200.json")
    assert [(r["joint"], r["ik"]) for r in golden["stored_supports"][:3]] == [(1, 1), (2, 1), (3, 1)]
    assert golden["lengths"]["joint_item"] == 4 and golden["lengths"]["support_info"] == 10
    steps = golden["steps"][1:]
    definitions = {1: TRILINEAR, 2: dict(TRILINEAR, K3_pos=-20.), 3: BILINEAR}
    for node, definition in definitions.items():
        states = _replay(definition, [s["joints"][str(node)]["dx"] for s in steps])
        for state, step in zip(states, steps):
            assert state.force == pytest.approx(step["joints"][str(node)]["jiban_hanryoku"], **LAW)
            assert _segment(state, definition) == step["joints"][str(node)]["bane_flg"]
    last = steps[-1]
    # 14 + 50 * 0.07, 14 - 20 * 0.07, 10 (the joints are 3e-7 m apart: JRSNAP's members are EA = 2.65e10).
    assert [last["joints"][str(n)]["jiban_hanryoku"] for n in (1, 2, 3)] == pytest.approx([17.5, 12.6, 10.0], abs=1e-6)
    assert [last["joints"][str(n)]["bane_flg"] for n in (1, 2, 3)] == [2, 2, 1]

    raw = {
        "dimension": 2,
        "node": {"1": {"x": 0., "y": 0., "z": 0.}, "2": {"x": 1., "y": 0., "z": 0.}, "3": {"x": 2., "y": 0., "z": 0.}},
        "member": {"1": {"ni": 1, "nj": 2, "e": 1}, "2": {"ni": 2, "nj": 3, "e": 1}},
        # EA = 2.65e9 kN: the members stretch 1e-8 m under 30 kN, within the 1e-7 m displacement tolerance.
        "element": {"1": {"1": {"E": 2.65e7, "A": 100., "Iy": 1000., "Iz": 1000., "J": 1., "nu": .2}}},
        "fix_node": {"1": [dict(n=1, tx=TRILINEAR, ty=1, rz=1), dict(n=2, tx=dict(TRILINEAR, K3_pos=-20.), ty=1, rz=1),
                           dict(n=3, tx=BILINEAR, ty=1, rz=1)]},
        "load": {"1": {"fix_node": 1, "load_node": [dict(n=3, tx=30.)]}},
        "analysis_params": {"displacement_control": {"node": 3, "dof": "dx", "targets": [0.0005 * k for k in range(1, 201)]},
                            "tolerance": 1e-8, "max_iterations": 200},
    }
    model = FemModel()
    model.read_json_model(_read_json_model(raw))
    result = model._run_solver_snapshot()
    assert len(result["step_results"]) == 200
    assert result["metadata"]["analysis"]["support_models"] == ["jrsnap_bilinear_v1", "jrsnap_trilinear_v1"]
    control_gap = 0.0
    for step, jr in zip(result["step_results"], steps):
        joints, elements = jr["joints"], jr["elements"]
        for node in (1, 2, 3):
            assert step["support_response"][node]["x"]["force"] == pytest.approx(joints[str(node)]["jiban_hanryoku"], abs=FORCE)
            assert step["node_displacements"][node]["dx"] == pytest.approx(joints[str(node)]["dx"], abs=1e-7)
        # Control node: FrameWeb is at the target; JRSNAP stops up to 4.4e-9 m short of it (step 200), which is
        # outside the 1e-9 m fixed for the control displacement (recorded as such, not loosened).
        assert step["node_displacements"][3]["dx"] == pytest.approx(0.0005 * jr["step"], abs=1e-12)
        control_gap = max(control_gap, abs(joints["3"]["dx"] - 0.0005 * jr["step"]))
        total = sum(joints[str(n)]["jiban_hanryoku"] for n in (1, 2, 3))
        assert step["lambda"] * 30.0 == pytest.approx(total, abs=FORCE)
        # Axial force: JRSNAP's Ni = Nj is tension-positive; FrameWeb's raw local N is -N at the i end and +N at j.
        raw1, raw2 = step["element_stresses"][1], step["element_stresses"][2]
        assert elements["1"]["Ni"] == pytest.approx(-raw1["i_end"][0], abs=FORCE)
        assert elements["1"]["Nj"] == pytest.approx(raw1["j_end"][0], abs=FORCE)
        assert elements["2"]["Nj"] == pytest.approx(raw2["j_end"][0], abs=FORCE)
        assert elements["2"]["Nj"] == pytest.approx(joints["1"]["jiban_hanryoku"] + joints["2"]["jiban_hanryoku"], abs=FORCE)
    assert control_gap == pytest.approx(4.35e-9, abs=1e-10)
