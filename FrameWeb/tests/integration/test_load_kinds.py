"""Fixed, incremental and limited loads through nonlinear_combine.

In the elastic range a combined nonlinear analysis must equal the superposition
F_fixed + lambda*F_incremental + min(lambda/limit, 1)*F_limited of the basic cases
solved one by one. The member is a nonlinear_bar whose first skeleton slope is
its elastic EI, so every state below stays on that slope.
"""

from __future__ import annotations

import copy

import numpy as np
import pytest

from fem.analysis_result_sets import build_analysis_result_set
from fem.diagnostics import InputValidationError
from fem.load_kinds import INCREMENTAL, LoadKind

pytestmark = pytest.mark.integration

LIMIT = 0.5
FACTORS = [0.0, 0.25, 0.5, 0.75, 1.0]


def _model() -> dict:
    """Cantilever 10->30 (L=2, EI=10000) with a nonlinear moment_z law."""
    return {
        "node": {"10": {"x": 0, "y": 0, "z": 0}, "30": {"x": 2, "y": 0, "z": 0}},
        "member": {"7": {"ni": 10, "nj": 30, "e": 1, "cg": 0}},
        "element": {"1": {"1": {
            "E": 10000, "nu": 0.25, "A": 1, "Iy": 1, "Iz": 1, "J": 1,
            "nonlinear": {
                "type": "jr_stiffness_reduction",
                "delta_1": 0.001, "delta_2": 0.004, "delta_3": 0.01,
                "P_1": 10, "P_2": 16, "P_3": 22, "beta": 0,
                "hysteresis_dofs": ["moment_z"],
            },
        }}},
        "fix_node": {"1": [{"n": 10, "tx": 1, "ty": 1, "tz": 1, "rx": 1, "ry": 1, "rz": 1}]},
        "load": {
            "1": {"name": "dead", "kind": "fixed",
                  "load_node": [{"n": 30, "ty": 1}]},
            "2": {"name": "inertia", "kind": "incremental",
                  "load_node": [{"n": 30, "ty": 1}],
                  "load_member": [{"m": 7, "mark": 2, "direction": "y",
                                   "L1": 0, "L2": 0, "P1": 0.5, "P2": 0.5}]},
            "3": {"name": "live inertia", "kind": "limited", "limit": LIMIT,
                  "load_node": [{"n": 30, "ty": 2}]},
            "4": {"name": "deck", "kind": "fixed",
                  "load_member": [{"m": 7, "mark": 2, "direction": "y",
                                   "L1": 0, "L2": 0, "P1": 0.25, "P2": 0.25}]},
        },
        "nonlinear_combine": {
            "1": {"row": 1, "name": "Case1", "C1": 1, "C2": 1, "C3": 1, "C4": 2,
                  "load_factors": FACTORS, "tolerance": 1e-10},
        },
    }


def _basic_case_results() -> dict[str, dict]:
    """Final elastic state of each basic case solved alone (lambda = 1)."""
    data = _model()
    del data["nonlinear_combine"]
    for case in data["load"].values():
        case.pop("kind")
        case.pop("limit", None)
        case["tolerance"] = 1e-10
    result_set = build_analysis_result_set(data)
    finals = {}
    for result in result_set["results"]:
        if result["state"]["is_final"]:
            finals[result["case_id"]] = result
    return finals


def _vector(result: dict) -> np.ndarray:
    """Displacements, reactions and member end forces as one flat vector."""
    values = []
    for row in result["node_displacements"]:
        values.extend(row["components"].values())
    for row in result["support_reactions"]:
        values.extend(row["components"].values())
    for member in result["member_section_forces"]:
        for segment in member["segments"]:
            values.extend(segment["i_end"].values())
            values.extend(segment["j_end"].values())
    return np.array(values, dtype=float)


def test_load_kind_factor_and_rate():
    fixed, limited = LoadKind("fixed"), LoadKind("limited", 0.1)
    assert [fixed.factor(x) for x in (0, 0.5)] == [1.0, 1.0]
    assert [INCREMENTAL.factor(x) for x in (0, 0.5)] == [0, 0.5]
    assert [limited.factor(x) for x in (0.05, 0.1, 0.3)] == [pytest.approx(0.5), 1.0, 1.0]
    assert [limited.rate(x) for x in (0.05, 0.1, 0.3)] == [10.0, 0.0, 0.0]
    assert fixed.rate(0.3) == 0.0 and INCREMENTAL.rate(0.3) == 1.0


def test_combination_equals_superposition_of_basic_cases():
    basic = {key: _vector(value) for key, value in _basic_case_results().items()}
    result_set = build_analysis_result_set(_model())

    assert [(case["case_id"], case["name"], case["analysis_type"])
            for case in result_set["cases"]] == [("1", "Case1", "material_nonlinear")]
    states = [r for r in result_set["results"] if r["state"]["kind"] == "load_step"]
    assert [r["state"]["load_factor"] for r in states] == FACTORS
    for result in states:
        lam = result["state"]["load_factor"]
        expected = (basic["1"] + 2*basic["4"] + lam*basic["2"]
                    + min(lam/LIMIT, 1)*basic["3"])
        np.testing.assert_allclose(_vector(result), expected, rtol=1e-9, atol=1e-9)


def test_displacement_control_starts_from_the_fixed_load_state():
    """The first state carries the fixed loads only; targets are measured from it."""
    data = _model()
    row = data["nonlinear_combine"]["1"]
    del row["load_factors"]
    targets = [0.0005, 0.001, 0.002]
    row["displacement_control"] = {"node": 30, "dof": "dy", "targets": targets}
    basic = _basic_case_results()

    def tip(result):
        return next(r["components"]["dy"] for r in result["node_displacements"]
                    if r["node_id"] == "30")

    states = [r for r in build_analysis_result_set(data)["results"]
              if r["state"]["kind"] == "load_step"]
    origin = tip(basic["1"]) + 2*tip(basic["4"])
    assert states[0]["state"]["load_factor"] == 0.0
    assert tip(states[0]) == pytest.approx(origin, rel=1e-9)
    increment, limited = tip(basic["2"]), tip(basic["3"])
    for result, target in zip(states[1:], targets):
        lam = result["state"]["load_factor"]
        assert tip(result) == pytest.approx(origin+target, rel=1e-9)
        assert lam*increment + min(lam/LIMIT, 1)*limited == pytest.approx(target, rel=1e-9)
    # The last target lies beyond the limit, where only case 2 still grows.
    assert states[-1]["state"]["load_factor"] > LIMIT


def test_nonlinear_bar_carries_member_loads_like_a_linear_member():
    """Below the first skeleton point a nonlinear_bar is the elastic member."""
    basic = _basic_case_results()
    linear = _model()
    del linear["nonlinear_combine"]
    del linear["element"]["1"]["1"]["nonlinear"]
    for case in linear["load"].values():
        case.pop("kind")
        case.pop("limit", None)
    reference = {r["case_id"]: r for r in build_analysis_result_set(linear)["results"]}
    for case_id in ("2", "4"):
        np.testing.assert_allclose(_vector(basic[case_id]), _vector(reference[case_id]),
                                   rtol=1e-9, atol=1e-12)


def test_kind_without_nonlinear_combine_is_rejected():
    data = _model()
    del data["nonlinear_combine"]
    with pytest.raises(InputValidationError, match="nonlinear_combine"):
        build_analysis_result_set(data)


@pytest.mark.parametrize("edit, message", [
    (lambda d: d["load"]["1"].update(kind="permanent"), "Unknown load kind"),
    (lambda d: d["load"]["3"].pop("limit"), "finite positive limit"),
    (lambda d: d["load"]["3"].update(limit=0), "finite positive limit"),
    (lambda d: d["load"]["1"].update(limit=0.5), "Only a limited load"),
    (lambda d: d["nonlinear_combine"]["1"].update(C9=1), "basic case 9"),
    (lambda d: d["nonlinear_combine"]["1"].update(target=1), "Unknown nonlinear_combine"),
    (lambda d: d["load"]["2"].update(fix_node=2), "same fix_node"),
])
def test_invalid_combination_is_rejected(edit, message):
    data = _model()
    data["fix_node"]["2"] = copy.deepcopy(data["fix_node"]["1"])
    edit(data)
    with pytest.raises(InputValidationError, match=message):
        build_analysis_result_set(data)


def test_temperature_load_must_be_incremental():
    data = _model()
    data["load"]["1"]["load_member"] = [{"m": 7, "mark": 9, "P1": 10}]
    with pytest.raises(InputValidationError, match="Temperature"):
        build_analysis_result_set(data)
