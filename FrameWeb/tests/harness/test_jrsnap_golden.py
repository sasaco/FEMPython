"""Frozen JRSNAP golden runs keep their provenance and the observed JRSNAP behavior.

Every golden under tests/data/jrsnap_golden is registered in RUNS with what its
inputs were, so that a new golden cannot land unregistered and a registered one
cannot go missing. The behavior checks of the first (v1) support-spring goldens
are unchanged since 2026-10-18; v2 goldens (2026-10-19, see
tools/jrsnap_golden/freeze.py) are compared with FrameWeb in
tests/integration/test_jrsnap_equivalence_*.py and only have their provenance
checked here.
"""

import hashlib
import json

import pytest

from tests.support.paths import DATA

pytestmark = pytest.mark.oracle

GOLDEN = DATA / "jrsnap_golden"

# JRSNAP force-limit tri-linear sides (K1, F1, K2, F2, K3) from the BLOCK6 rows in tools/jrsnap_golden/make_cases.py.
POS = (1000.0, 10.0, 200.0, 14.0, 50.0)
NEG = (500.0, 5.0, 100.0, 7.0, 0.001)
V1 = {"schema": "jrsnap_golden_v1", "product": "5.2-L03", "load_node": 1, "spring_node": 2, "spring_row": 1, "element": 1}
# key = "<case dir>/<golden stem>". "law" marks the v1 behavior checks below; "unbalance" is the largest
# load minus reported spring force over a load-increment run.
V2 = {"schema": "jrsnap_golden_v2", "product": "5.2-L03"}
# DA edits made to the writer's .ndt before the analysis (tools/jrsnap_golden/ndt_tools.py).
ACCURACY = {"convergence": "accuracy"}
ACCURACY_FTM = {"convergence": "accuracy", "ftm": "1.0E-08"}
RUNS = {
    "support_trilinear01/golden_n100": dict(V1, pos=POS, neg=NEG, load=20.0, unbalance=1.2379),
    "support_trilinear01/golden_n1000": dict(V1, pos=POS, neg=NEG, load=20.0, unbalance=0.3800),
    "support_trilinear02_neg/golden_n100": dict(V1, pos=POS, neg=(500.0, 5.0, 100.0, 7.0, 20.0), load=-10.0),
    "support_trilinear03_softening/golden_n200": dict(
        V1, pos=(1000.0, 10.0, 200.0, 14.0, -20.0), neg=NEG, displacement=0.2
    ),
    # 2026-10-19, equivalence record (docs/report/jrsnap-frameweb-equivalence.md). Step 1: tests/integration/
    # test_jrsnap_equivalence_linear.py; Steps 2-4: test_jrsnap_equivalence_support.py, _member.py.
    "linear01_cantilever/golden_n10": dict(V2, control="load"),
    "linear01_cantilever_moment/golden_n10": dict(V2, control="load"),
    "linear02_moment_disp/golden_n10": dict(V2, control="displacement"),
    # "case" is the make_cases case when the directory is a rerun of it with DA edits.
    "support_trilinear01_f1/golden_n100": dict(V2, control="load", post_processing=ACCURACY, case="support_trilinear01"),
    "support_trilinear01_f1/golden_n1000": dict(V2, control="load", post_processing=ACCURACY, case="support_trilinear01"),
    "support_trilinear02_neg_f1/golden_n100": dict(V2, control="load", post_processing=ACCURACY, case="support_trilinear02_neg"),
    "support_trilinear04_neg_disp/golden_n100": dict(V2, control="displacement"),
    "support_trilinear04_neg_disp_f1/golden_n100": dict(V2, control="displacement", post_processing=ACCURACY,
                                                       case="support_trilinear04_neg_disp"),
    "support_bilinear01/golden_n100": dict(V2, control="displacement", post_processing=ACCURACY),
    "support_multi01/golden_n200": dict(V2, control="displacement", post_processing=ACCURACY),
    "member_mphi01_moment/golden_n100": dict(V2, control="load", post_processing=ACCURACY),
    "member_mphi02_disp/golden_n100": dict(V2, control="displacement", post_processing=ACCURACY_FTM),
    "member_beam001/golden_n100": dict(V2, control="load", post_processing=ACCURACY_FTM),
    "combined01/golden_n100": dict(V2, control="load", post_processing=ACCURACY_FTM),
}
V1_RUNS = [key for key, spec in RUNS.items() if spec["schema"] == "jrsnap_golden_v1"]


def _sha256(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def _path(key):
    case, stem = key.split("/")
    return GOLDEN / case / f"{stem}.json"


def _load(key):
    return json.loads(_path(key).read_text(encoding="utf-8"))


def _skeleton(magnitude, K1, F1, K2, F2, K3):
    """Force magnitude and segment (0, 1, 2) built from force limits."""
    x1 = F1 / K1
    x2 = x1 + (F2 - F1) / K2
    if magnitude <= x1:
        return K1 * magnitude, 0
    if magnitude <= x2:
        return F1 + K2 * (magnitude - x1), 1
    return F2 + K3 * (magnitude - x2), 2


def test_golden_runs_are_registered():
    on_disk = sorted(f"{p.parent.name}/{p.stem}" for p in GOLDEN.glob("*/golden_*.json"))
    assert on_disk == sorted(RUNS)
    assert V1_RUNS == [
        "support_trilinear01/golden_n100",
        "support_trilinear01/golden_n1000",
        "support_trilinear02_neg/golden_n100",
        "support_trilinear03_softening/golden_n200",
    ]


@pytest.mark.parametrize("key", sorted(RUNS))
def test_golden_inputs_match_recorded_hashes(key):
    spec = RUNS[key]
    golden = _load(key)
    assert golden["schema"] == spec["schema"]
    assert golden["case"] == spec.get("case", key.split("/")[0])
    assert golden["provenance"]["jrsnap"]["product"] == spec["product"]
    for record in golden["provenance"]["inputs"].values():
        assert _sha256(_path(key).parent / record["file"]) == record["sha256"]
    if spec["schema"] != "jrsnap_golden_v1":
        assert golden["control"]["mode"] == spec["control"]
        assert golden["control"]["steps"] == golden["load_steps"] == len(golden["steps"]) - 1
        assert golden["provenance"].get("post_processing", {}) == spec.get("post_processing", {})


@pytest.mark.parametrize("key", V1_RUNS)
def test_jrsnap_steps_follow_the_applied_control(key):
    case = RUNS[key]
    golden = _load(key)
    n = golden["load_steps"]
    load_node, spring_node, element = str(case["load_node"]), str(case["spring_node"]), str(case["element"])
    assert [s["step"] for s in golden["steps"]] == list(range(n + 1))
    for step in golden["steps"]:
        joints = step["joints"]
        if "load" in case:
            # Load increment: the member shear equals the applied load (JRSNAP keeps about 1e-5 kN).
            load = case["load"] * step["step"] / n
            assert step["elements"][element]["Si"] == pytest.approx(load, abs=1e-4)
        else:
            # Displacement increment at node 1.
            target = case["displacement"] * step["step"] / n
            assert joints[load_node]["dx"] == pytest.approx(target, abs=1e-10)
        # Rigid member: the loaded tip moves with the spring node.
        assert joints[load_node]["dx"] == pytest.approx(joints[spring_node]["dx"], rel=1e-6, abs=1e-9)
        # JRSNAP rigid supports are springs of about 1e15, not exact constraints.
        assert joints[spring_node]["dy"] == pytest.approx(0, abs=1e-12)
        assert joints[spring_node]["rz"] == pytest.approx(0, abs=1e-12)


@pytest.mark.parametrize("key", V1_RUNS)
def test_jrsnap_reported_spring_force_and_segment(key):
    case = RUNS[key]
    golden = _load(key)
    n = golden["load_steps"]
    spring_row, spring_node, element = str(case["spring_row"]), str(case["spring_node"]), str(case["element"])
    unbalance = []
    for step in golden["steps"]:
        # Spring fields are indexed by support row (row 1 = node 2 X), displacements by joint.
        spring = step["joints"][spring_row]
        dx = step["joints"][spring_node]["dx"]
        side = case["pos"] if dx >= 0 else case["neg"]
        sign = 1.0 if dx >= 0 else -1.0
        _, segment = _skeleton(abs(dx), *side)
        assert spring["bane_flg"] == sign * segment
        # Observed in 5.2-L03 and 5.3-L01: the negative side reports its skeleton at twice the displacement.
        force, _ = _skeleton(abs(dx) * (1.0 if dx >= 0 else 2.0), *side)
        assert spring["jiban_hanryoku"] == pytest.approx(sign * force, rel=1e-12, abs=1e-12)
        if "load" in case:
            unbalance.append(abs(case["load"] * step["step"] / n) - abs(spring["jiban_hanryoku"]))
        else:
            # Displacement control: the member shear balances the spring (within about 4e-5 kN).
            assert step["elements"][element]["Si"] == pytest.approx(spring["jiban_hanryoku"], abs=1e-4)
    if "unbalance" in case:
        # JRSNAP accepts a residual (stiffness-ratio convergence); it never exceeds the load.
        assert min(unbalance) > -1e-6
        assert max(unbalance) == pytest.approx(case["unbalance"], abs=1e-4)


def test_negative_first_limit_is_stored_as_zero():
    for key in V1_RUNS:
        x_spring = _load(key)["stored_supports"][0]
        assert (x_spring["joint"], x_spring["ik"]) == (2, 1)
        assert x_spring["Rmit1_S"] == 10 and x_spring["Rmit2_F"] == 7
        # Input -RMiT1 = 5, but SNAPDBLib reads 0 (5.2-L03 and 5.3-L01 alike).
        assert x_spring["Rmit1_F"] == 0
