"""Frozen JRSNAP golden runs keep their provenance and the observed JRSNAP behavior."""

import hashlib
import json

import pytest

from tests.support.paths import DATA

pytestmark = pytest.mark.oracle

GOLDEN = DATA / "jrsnap_golden"
RUNS = sorted(GOLDEN.glob("*/golden_*.json"))

# JRSNAP force-limit tri-linear sides (K1, F1, K2, F2, K3) from the BLOCK6 rows in tools/jrsnap_golden/make_cases.py.
POS = (1000.0, 10.0, 200.0, 14.0, 50.0)
CASES = {
    "support_trilinear01": {"pos": POS, "neg": (500.0, 5.0, 100.0, 7.0, 0.001), "load": 20.0},
    "support_trilinear02_neg": {"pos": POS, "neg": (500.0, 5.0, 100.0, 7.0, 20.0), "load": -10.0},
    "support_trilinear03_softening": {
        "pos": (1000.0, 10.0, 200.0, 14.0, -20.0),
        "neg": (500.0, 5.0, 100.0, 7.0, 0.001),
        "displacement": 0.2,
    },
}
# Largest load minus reported spring force over each load-increment run.
UNBALANCE = {
    "support_trilinear01/golden_n100": 1.2379,
    "support_trilinear01/golden_n1000": 0.3800,
}


def _sha256(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def _load(path):
    return json.loads(path.read_text(encoding="utf-8"))


def _skeleton(magnitude, K1, F1, K2, F2, K3):
    """Force magnitude and segment (0, 1, 2) built from force limits."""
    x1 = F1 / K1
    x2 = x1 + (F2 - F1) / K2
    if magnitude <= x1:
        return K1 * magnitude, 0
    if magnitude <= x2:
        return F1 + K2 * (magnitude - x1), 1
    return F2 + K3 * (magnitude - x2), 2


def _key(path):
    return f"{path.parent.name}/{path.stem}"


def test_golden_runs_are_present():
    assert [_key(p) for p in RUNS] == [
        "support_trilinear01/golden_n100",
        "support_trilinear01/golden_n1000",
        "support_trilinear02_neg/golden_n100",
        "support_trilinear03_softening/golden_n200",
    ]


@pytest.mark.parametrize("path", RUNS, ids=_key)
def test_golden_inputs_match_recorded_hashes(path):
    golden = _load(path)
    assert golden["schema"] == "jrsnap_golden_v1"
    assert golden["case"] == path.parent.name
    assert golden["provenance"]["jrsnap"]["product"] == "5.2-L03"
    for record in golden["provenance"]["inputs"].values():
        assert _sha256(path.parent / record["file"]) == record["sha256"]


@pytest.mark.parametrize("path", RUNS, ids=_key)
def test_jrsnap_steps_follow_the_applied_control(path):
    golden = _load(path)
    case = CASES[golden["case"]]
    n = golden["load_steps"]
    assert [s["step"] for s in golden["steps"]] == list(range(n + 1))
    for step in golden["steps"]:
        joints = step["joints"]
        if "load" in case:
            # Load increment: the member shear equals the applied load (JRSNAP keeps about 1e-5 kN).
            load = case["load"] * step["step"] / n
            assert step["elements"]["1"]["Si"] == pytest.approx(load, abs=1e-4)
        else:
            # Displacement increment at node 1.
            target = case["displacement"] * step["step"] / n
            assert joints["1"]["dx"] == pytest.approx(target, abs=1e-10)
        # Rigid member: the loaded tip moves with the spring node.
        assert joints["1"]["dx"] == pytest.approx(joints["2"]["dx"], rel=1e-6, abs=1e-9)
        # JRSNAP rigid supports are springs of about 1e15, not exact constraints.
        assert joints["2"]["dy"] == pytest.approx(0, abs=1e-12)
        assert joints["2"]["rz"] == pytest.approx(0, abs=1e-12)


@pytest.mark.parametrize("path", RUNS, ids=_key)
def test_jrsnap_reported_spring_force_and_segment(path):
    golden = _load(path)
    case = CASES[golden["case"]]
    n = golden["load_steps"]
    unbalance = []
    for step in golden["steps"]:
        # Spring fields are indexed by support row (row 1 = node 2 X), displacements by joint.
        spring = step["joints"]["1"]
        dx = step["joints"]["2"]["dx"]
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
            assert step["elements"]["1"]["Si"] == pytest.approx(spring["jiban_hanryoku"], abs=1e-4)
    if _key(path) in UNBALANCE:
        # JRSNAP accepts a residual (stiffness-ratio convergence); it never exceeds the load.
        assert min(unbalance) > -1e-6
        assert max(unbalance) == pytest.approx(UNBALANCE[_key(path)], abs=1e-4)


def test_negative_first_limit_is_stored_as_zero():
    for path in RUNS:
        x_spring = _load(path)["stored_supports"][0]
        assert (x_spring["joint"], x_spring["ik"]) == (2, 1)
        assert x_spring["Rmit1_S"] == 10 and x_spring["Rmit2_F"] == 7
        # Input -RMiT1 = 5, but SNAPDBLib reads 0 (5.2-L03 and 5.3-L01 alike).
        assert x_spring["Rmit1_F"] == 0
