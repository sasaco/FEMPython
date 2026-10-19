"""Step 1 of the JRSNAP equivalence record: the linear calibration column.

JRSNAP 5.2-L03 goldens (tests/data/jrsnap_golden/linear01_*, linear02_*) are compared with the closed form
and with FrameWeb, which fixes, with evidence, the facts the nonlinear comparisons rest on:

- the ``E`` field of the .ndu (25) is 2.5e7 kN/m2 (member_info.E = 25000000) and JRSNAP's bar has no shear
  deformation (the tip displacement is PL^3/3EI to 1e-12);
- a joint moment load (PM) is accepted;
- the sign map between FrameWeb's raw local end forces and JRSNAP's Mi/Si/Mj/Sj, and between FrameWeb's
  reactions and the ``jiban_hanryoku`` of JRSNAP's rigid support rows;
- under a displacement increment JRSNAP scales the load pattern in integer steps of 1/MDV (``lo_step_no``)
  and supplies the rest of the target displacement through a horizontal force at the control node, so a
  displacement increment is only comparable with FrameWeb when that node carries the load pattern itself.

Tolerances were fixed before the comparison (docs/report/jrsnap-frameweb-equivalence.md): relative 1e-8 for the
closed form and FrameWeb (double-precision linear solutions; JRSNAP's rigid supports are 1e15 springs, which
shows up around 1e-12), 1e-9 kN for forces that should vanish.
"""

import json

import pytest

from fem.file_io import _read_json_model
from fem.model import FemModel
from tests.support.paths import DATA

pytestmark = [pytest.mark.integration, pytest.mark.oracle]

GOLDEN = DATA / "jrsnap_golden"
E, A, I, L = 2.5e7, 0.0007, 1e-5, 1.0
EI = E * I
REL = 1e-8
ZERO = 1e-9


def _golden(case):
    return json.loads((GOLDEN / case / "golden_n10.json").read_text(encoding="utf-8"))


def _frameweb(load):
    """The calibration column in FrameWeb (legacy JSON route, no G so no shear deformation)."""
    raw = {
        "dimension": 2,
        "node": {"1": {"x": 0.0, "y": -L, "z": 0.0}, "2": {"x": 0.0, "y": 0.0, "z": 0.0}},
        "member": {"1": {"ni": 1, "nj": 2, "e": 1}},
        "element": {"1": {"1": {"E": E, "A": A, "Iy": I, "Iz": I, "J": 1.0, "nu": 0.2}}},
        "fix_node": {"1": [dict(n=2, tx=1, ty=1, rz=1)]},
        "load": {"1": {"fix_node": 1, "load_node": [dict(n=1, **load)]}},
    }
    model = FemModel()
    model.read_json_model(_read_json_model(raw))
    return model._run_solver_snapshot()


def jrsnap_end_forces(raw):
    """JRSNAP's Mi, Si, Mj, Sj from FrameWeb's raw local end forces [N, Vy, Vz, T, My, Mz] (member along +Y)."""
    i_end, j_end = raw["i_end"], raw["j_end"]
    return {"Mi": i_end[5], "Si": -i_end[1], "Mj": -j_end[5], "Sj": j_end[1]}


@pytest.mark.parametrize("case, load, dx, rz, forces", [
    ("linear01_cantilever", dict(tx=10.0), 10 * L**3 / (3 * EI), 10 * L**2 / (2 * EI), dict(Mi=0.0, Si=10.0, Mj=10.0, Sj=10.0)),
    ("linear01_cantilever_moment", dict(rz=10.0), 10 * L**2 / (2 * EI), 10 * L / EI, dict(Mi=10.0, Si=0.0, Mj=10.0, Sj=0.0)),
])
def test_jrsnap_linear_column_matches_the_closed_form_and_frameweb(case, load, dx, rz, forces):
    golden = _golden(case)
    assert golden["member_info"][0]["E"] == 25000000 and golden["member_info"][0]["A"] == A
    assert golden["kihon"]["HisenkeiFlg_A"] == 9  # the run was linear
    final = golden["steps"][10]
    tip, base = final["joints"]["1"], final["joints"]["2"]
    # Closed form without shear deformation (the shear term PL/GA would be about 10%).
    assert tip["dx"] == pytest.approx(dx, rel=REL)
    assert tip["rz"] == pytest.approx(rz, rel=REL)
    assert abs(base["dx"]) < 1e-12 and abs(base["rz"]) < 1e-12  # rigid supports of about 1e15
    for name, value in forces.items():
        assert final["elements"]["1"][name] == pytest.approx(value, rel=REL, abs=ZERO)
    assert final["elements"]["1"]["Ni"] == pytest.approx(0, abs=ZERO)
    # Every step is proportional (linear run, load factor step/10).
    for step in golden["steps"][1:]:
        assert step["joints"]["1"]["dx"] == pytest.approx(dx * step["step"] / 10, rel=REL)
        assert step["lo_step_no"] == step["step"]

    result = _frameweb(load)
    frameweb_tip = result["node_displacements"][1]
    assert frameweb_tip["dx"] == pytest.approx(tip["dx"], rel=REL)
    assert frameweb_tip["rz"] == pytest.approx(tip["rz"], rel=REL)
    mapped = jrsnap_end_forces(result["element_stresses"][1])
    for name in ("Mi", "Si", "Mj", "Sj"):
        assert mapped[name] == pytest.approx(final["elements"]["1"][name], rel=REL, abs=ZERO)
    # Reactions: FrameWeb's reaction is minus the restoring force JRSNAP reports for the rigid row (row 1 = node 2 X).
    assert -result["reaction_forces"][2]["fx"] == pytest.approx(final["joints"]["1"]["jiban_hanryoku"], rel=REL, abs=ZERO)
    assert result["reaction_forces"][2]["fy"] == pytest.approx(-final["joints"]["2"]["jiban_hanryoku"], abs=ZERO)


def test_jrsnap_displacement_increment_adds_a_force_at_the_control_node():
    """Measured JRSNAP behaviour, not a FrameWeb match: with a tip-moment pattern and X control, each step is
    lambda = lo_step_no/MDV of the pattern plus a horizontal force F = Si at the tip, and the displacement and
    rotation are the superposition of both. FrameWeb's displacement control scales the pattern alone (it would
    give lambda = 2 and no shear at dx = 0.04), so this control mode is recorded as not comparable."""
    golden = _golden("linear02_moment_disp")
    assert golden["control"] == {"mode": "displacement", "steps": 10}
    assert golden["kihon"]["DSMAX"] == 0.04 and golden["kihon"]["JNo"] == 1 and golden["kihon"]["DDV"] == 10
    for step in golden["steps"][1:]:
        tip, element = step["joints"]["1"], step["elements"]["1"]
        lam = step["lo_step_no"] / 10
        force = element["Si"]
        assert element["Mi"] == pytest.approx(10 * lam, rel=1e-6)          # the pattern at integer steps
        assert element["Mj"] == pytest.approx(10 * lam + force * L, rel=1e-6)
        assert tip["dx"] == pytest.approx(0.004 * step["step"], rel=1e-6)   # the target is met
        assert tip["dx"] == pytest.approx(10 * lam * L**2 / (2 * EI) + force * L**3 / (3 * EI), rel=1e-6)
        assert tip["rz"] == pytest.approx(10 * lam * L / EI + force * L**2 / (2 * EI), rel=1e-6)
    assert golden["steps"][10]["lo_step_no"] == 7
    assert golden["steps"][10]["elements"]["1"]["Si"] == pytest.approx(19.5, rel=1e-6)
