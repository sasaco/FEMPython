"""HTTP and orchestration contracts for the sole AnalysisResultSet response."""

from __future__ import annotations

import copy
import json

import pytest
from flask import Response
from main import app

from fem import analysis_result_sets
from fem.diagnostics import InputValidationError
from fem.result_contracts import (
    MAX_ITERATIONS_PER_STEP,
    MAX_NONLINEAR_STEPS_PER_CASE,
    validate_analysis_result_set,
)
from tests.support.builders.input_routes import axial_json

pytestmark = pytest.mark.integration


def _restraints() -> list[dict[str, int]]:
    return [
        {"n": 10, "tx": 1, "ty": 1, "tz": 1, "rx": 1, "ry": 1, "rz": 1},
        {"n": 30, "tx": 0, "ty": 1, "tz": 1, "rx": 1, "ry": 1, "rz": 1},
    ]


def _legacy_two_case_beam() -> dict[str, object]:
    material_1 = {"E": 10_000, "G": 4_000, "A": 1, "Iy": 1, "Iz": 1, "J": 1}
    material_2 = {**material_1, "E": 20_000}
    return {
        "node": {
            "30": {"x": 2, "y": 0, "z": 0},
            "10": {"x": 0, "y": 0, "z": 0},
        },
        "member": {"7": {"ni": 10, "nj": 30, "e": 1, "cg": 0}},
        "notice_points": [{"m": 7, "Points": [0.5]}],
        "element": {"1": {"1": material_1}, "2": {"1": material_2}},
        "fix_node": {"1": _restraints(), "2": _restraints()},
        "load": {
            "positive": {
                "name": "Positive case",
                "symbol": "P",
                "element": 1,
                "fix_node": 1,
                "rate": 1,
                "load_node": [{"n": 30, "tx": 4}],
            },
            "negative-scaled": {
                "element": 2,
                "fix_node": 2,
                "rate": 2.5,
                "load_node": [{"n": 30, "tx": -6}],
            },
        },
    }


def _post(data: dict[str, object], accept: str | None = None) -> Response:
    headers = {"Accept": accept} if accept is not None else {}
    return app.test_client().post(
        "/",
        data=json.dumps(data),
        content_type="application/json",
        headers=headers,
    )


def _with_case_count(data: dict[str, object], case_count: int) -> dict[str, object]:
    case = next(iter(data["load"].values()))
    data["load"] = {
        f"case-{index}": copy.deepcopy(case) for index in range(case_count)
    }
    return data


def _node_components(result: dict, field: str, node_id: str) -> dict[str, float]:
    return next(row["components"] for row in result[field] if row["node_id"] == node_id)


@pytest.mark.parametrize(
    "accept",
    [None, "application/json", "application/vnd.frameweb.legacy-cases-v1+json", "application/vnd.frameweb.legacy-cases-v2+json"],
)
def test_accept_never_negotiates_an_alternate_success_shape(accept: str | None) -> None:
    response = _post(_legacy_two_case_beam(), accept)
    assert response.status_code == 200
    assert response.content_type == "application/json; charset=utf-8"
    body = response.get_json()
    validate_analysis_result_set(body)
    assert body["kind"] == "analysis_result_set"
    assert "disg" not in json.dumps(body)
    assert "fsec" not in json.dumps(body)


def test_legacy_cases_are_solved_in_insertion_order_without_rate_scaling() -> None:
    data = _legacy_two_case_beam()
    original = copy.deepcopy(data)
    response = _post(data)
    assert response.status_code == 200, response.get_data(as_text=True)
    body = response.get_json()
    assert data == original
    assert [case["case_id"] for case in body["cases"]] == ["positive", "negative-scaled"]
    assert body["cases"][0]["name"] == "Positive case"
    assert body["cases"][0]["symbol"] == "P"
    assert body["cases"][1]["name"] == "negative-scaled"
    assert [result["case_id"] for result in body["results"]] == ["positive", "negative-scaled"]
    positive, negative = body["results"]
    assert _node_components(positive, "node_displacements", "30")["dx"] == pytest.approx(0.0008)
    assert _node_components(negative, "node_displacements", "30")["dx"] == pytest.approx(-0.0006)
    assert _node_components(positive, "support_reactions", "10")["fx"] == pytest.approx(-4)
    assert _node_components(negative, "support_reactions", "10")["fx"] == pytest.approx(6)
    assert negative["member_section_forces"][0]["segments"][0]["i_end"]["fx"] == pytest.approx(-6)


def test_later_invalid_case_is_atomic() -> None:
    data = _legacy_two_case_beam()
    data["load"]["broken-later"] = {
        "element": 999,
        "fix_node": 1,
        "load_node": [{"n": 30, "tx": 1}],
    }
    response = _post(data)
    assert response.status_code != 200
    body = response.get_json()
    assert body["converged"] is False
    assert body["details"]["case_id"] == "broken-later"
    assert "results" not in body


def test_isolated_case_topology_mismatch_is_an_atomic_internal_failure(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    actual_builder = analysis_result_sets.build_result_topology
    calls = 0

    def mismatching_builder(source_data: dict, model: object) -> dict:
        nonlocal calls
        topology = actual_builder(source_data, model)
        calls += 1
        if calls == 2:
            topology["nodes"][0]["coordinates"]["x"] += 1
        return topology

    monkeypatch.setattr(
        analysis_result_sets, "build_result_topology", mismatching_builder
    )
    response = _post(_legacy_two_case_beam())
    assert response.status_code == 500
    body = response.get_json()
    assert body["converged"] is False
    assert "results" not in body


def test_more_than_256_legacy_cases_enumerate_in_order_without_solving() -> None:
    case_inputs = analysis_result_sets._enumerate_cases(
        _with_case_count(_legacy_two_case_beam(), 257)
    )
    assert [case.case_id for case in case_inputs] == [
        f"case-{index}" for index in range(257)
    ]
    analysis_result_sets._validate_request_work_budget(case_inputs)


def test_large_case_collection_still_rejects_malformed_case() -> None:
    data = _with_case_count(_legacy_two_case_beam(), 257)
    data["load"]["case-256"].update(
        analysis_type="material_nonlinear", max_iterations=0
    )
    with pytest.raises(InputValidationError, match="max_iterations must be a positive integer") as error:
        analysis_result_sets._validate_request_work_budget(
            analysis_result_sets._enumerate_cases(data)
        )
    assert error.value.details["case_id"] == "case-256"


def _assert_work_budget_rejected_before_model_creation(
    monkeypatch: pytest.MonkeyPatch, data: dict[str, object]
) -> dict:
    model_created = False
    model_data_read = False

    class UnexpectedFemModel:
        def __init__(self) -> None:
            nonlocal model_created
            model_created = True

    def unexpected_read(_: dict) -> dict:
        nonlocal model_data_read
        model_data_read = True
        return {}

    monkeypatch.setattr(analysis_result_sets, "FemModel", UnexpectedFemModel)
    monkeypatch.setattr(analysis_result_sets, "_read_json_model", unexpected_read)
    response = _post(data)
    assert response.status_code == 400
    assert model_created is False
    assert model_data_read is False
    return response.get_json()


def test_top_level_nonlinear_step_limit_rejects_before_model_creation(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    data = _legacy_two_case_beam()
    data["analysis_type"] = "material_nonlinear"
    data["analysis_params"] = {
        "n_load_steps": MAX_NONLINEAR_STEPS_PER_CASE + 1,
    }
    body = _assert_work_budget_rejected_before_model_creation(monkeypatch, data)
    assert body["error_code"] == "invalid_input"
    assert body["details"] == {
        "case_id": "positive",
        "budget": "nonlinear_steps",
        "requested": MAX_NONLINEAR_STEPS_PER_CASE + 1,
        "limit": MAX_NONLINEAR_STEPS_PER_CASE,
    }


def test_modern_max_iteration_limit_rejects_before_model_creation(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    data = axial_json()
    data["analysis_type"] = "material_nonlinear"
    data["analysis_params"] = {"max_iterations": MAX_ITERATIONS_PER_STEP + 1}
    body = _assert_work_budget_rejected_before_model_creation(monkeypatch, data)
    assert body["details"]["case_id"] == "1"
    assert body["details"]["budget"] == "max_iterations"


@pytest.mark.parametrize(
    "schedule",
    [
        {"load_factors": [1.0] * (MAX_NONLINEAR_STEPS_PER_CASE + 1)},
        {
            "displacement_control": {
                "node": 30,
                "dof": "dx",
                "targets": [0.001] * (MAX_NONLINEAR_STEPS_PER_CASE + 1),
            }
        },
    ],
    ids=["load-factors", "displacement-targets"],
)
def test_explicit_nonlinear_schedule_limit_rejects_before_model_creation(
    monkeypatch: pytest.MonkeyPatch, schedule: dict[str, object]
) -> None:
    data = _legacy_two_case_beam()
    data["load"]["positive"].update(
        analysis_type="material_nonlinear",
        **schedule,
    )
    body = _assert_work_budget_rejected_before_model_creation(monkeypatch, data)
    assert body["details"]["case_id"] == "positive"
    assert body["details"]["budget"] == "nonlinear_steps"


@pytest.mark.parametrize(
    "analysis_type, settings",
    [
        ("material_nonlinear", {"n_load_steps": 2, "max_iterations": MAX_ITERATIONS_PER_STEP}),
        ("modal", {"n_modes": 40}),
    ],
)
def test_request_work_has_no_aggregate_ceiling(
    analysis_type: str, settings: dict[str, int]
) -> None:
    data = _with_case_count(_legacy_two_case_beam(), 257)
    for case in data["load"].values():
        case.update(analysis_type=analysis_type, **settings)
    analysis_result_sets._validate_request_work_budget(
        analysis_result_sets._enumerate_cases(data)
    )


def test_work_budget_boundaries_are_inclusive() -> None:
    nonlinear = _legacy_two_case_beam()
    nonlinear["load"] = {"at-limit": nonlinear["load"]["positive"]}
    nonlinear["load"]["at-limit"].update(
        analysis_type="material_nonlinear",
        load_factors=[1.0] * MAX_NONLINEAR_STEPS_PER_CASE,
        max_iterations=MAX_ITERATIONS_PER_STEP + 1,
    )
    nonlinear["analysis_params"] = {"max_iterations": MAX_ITERATIONS_PER_STEP}
    analysis_result_sets._validate_request_work_budget(
        analysis_result_sets._enumerate_cases(nonlinear)
    )

    max_iterations = _legacy_two_case_beam()
    max_iterations["load"] = {
        "at-limit": max_iterations["load"]["positive"]
    }
    max_iterations["load"]["at-limit"].update(
        analysis_type="material_nonlinear",
        n_load_steps=1,
        max_iterations=MAX_ITERATIONS_PER_STEP,
    )
    analysis_result_sets._validate_request_work_budget(
        analysis_result_sets._enumerate_cases(max_iterations)
    )



def test_modern_input_remains_exactly_one_case_named_one() -> None:
    response = app.test_client().post("/", json=axial_json())
    assert response.status_code == 200
    body = response.get_json()
    assert [(case["case_id"], case["name"], case["symbol"]) for case in body["cases"]] == [("1", "1", "1")]
    assert {result["case_id"] for result in body["results"]} == {"1"}


def test_top_level_analysis_settings_override_each_legacy_case() -> None:
    data = _legacy_two_case_beam()
    data["load"]["positive"]["analysis_type"] = "material_nonlinear"
    data["analysis_type"] = "static"
    data["analysis_params"] = {"n_load_steps": 1}
    response = _post(data)
    assert response.status_code == 200
    body = response.get_json()
    assert [case["analysis_type"] for case in body["cases"]] == ["static", "static"]
    assert [result["state"] for result in body["results"]] == [
        {"kind": "static", "index": 0},
        {"kind": "static", "index": 0},
    ]


def test_case_analysis_settings_support_ordered_mixed_analysis_types() -> None:
    data = _legacy_two_case_beam()
    data["load"]["positive"].update(
        analysis_type="material_nonlinear", n_load_steps=2
    )
    data["load"]["negative-scaled"]["analysis_type"] = "static"
    response = _post(data)
    assert response.status_code == 200, response.get_data(as_text=True)
    body = response.get_json()
    assert [case["analysis_type"] for case in body["cases"]] == [
        "material_nonlinear",
        "static",
    ]
    assert [result["state"] for result in body["results"]] == [
        {"kind": "load_step", "index": 0, "load_factor": 0.5, "is_final": False},
        {"kind": "load_step", "index": 1, "load_factor": 1.0, "is_final": True},
        {"kind": "static", "index": 0},
    ]


def test_case_specific_supports_are_not_promoted_to_shared_topology() -> None:
    data = _legacy_two_case_beam()
    data["dimension"] = 2
    data["fix_node"]["2"] = [data["fix_node"]["2"][0]]
    response = _post(data)
    assert response.status_code == 200
    body = response.get_json()
    assert [case["support_node_ids"] for case in body["cases"]] == [
        ["30", "10"],
        ["10"],
    ]
    assert "support_node_ids" not in body["topology"]
