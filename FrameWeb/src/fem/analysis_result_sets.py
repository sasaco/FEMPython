"""Application service for atomic ordered AnalysisResultSet construction."""

from __future__ import annotations

from collections.abc import Mapping
from dataclasses import dataclass
from typing import Any

import numpy as np

from .diagnostics import InputValidationError
from .file_io import _read_json_model, result_to_jsonable
from .legacy_beam import select_case
from .model import FemModel
from .result_contracts import (
    COORDINATE_SYSTEM,
    MAX_ITERATIONS_PER_STEP,
    MAX_NONLINEAR_STEPS_PER_CASE,
    AnalysisResultSet,
    ResultCase,
    ResultContractError,
    validate_analysis_result_set,
)
from .result_projection import project_analysis_results
from .result_topology import build_result_topology, public_support_node_ids


@dataclass(frozen=True)
class _CaseInput:
    case_id: str
    name: str
    symbol: str
    selected_data: dict[str, Any]


def build_analysis_result_set(data: dict[str, Any]) -> AnalysisResultSet:
    """Solve all requested cases independently and return the sole success root.

    Only JSON-ready rows are retained between cases. Any selection, solve,
    projection, topology or contract failure aborts the complete response.
    """

    case_inputs = _enumerate_cases(data)
    _validate_request_work_budget(case_inputs)
    canonical_topology: dict[str, Any] | None = None
    canonical_units: dict[str, str] | None = None
    cases: list[ResultCase] = []
    results: list[dict[str, Any]] = []

    for case_input in case_inputs:
        try:
            model_data = _read_json_model(case_input.selected_data)
            model = FemModel()
            model.read_json_model(model_data)
            try:
                candidate_topology = build_result_topology(data, model)
                if canonical_topology is None:
                    canonical_topology = candidate_topology
                elif candidate_topology != canonical_topology:
                    raise ResultContractError(
                        "Isolated case produced a different canonical topology"
                    )

                units = _result_units(model)
                if canonical_units is None:
                    canonical_units = units
                elif units != canonical_units:
                    raise ResultContractError(
                        "Isolated case produced different result units"
                    )

                support_node_ids = public_support_node_ids(model, canonical_topology)
            except ResultContractError:
                raise
            except (KeyError, TypeError, ValueError) as error:
                raise ResultContractError(str(error)) from error

            solver_result = result_to_jsonable(model._run_solver_snapshot())
            analysis_type = solver_result.get("analysis_type")
            if analysis_type not in ("static", "material_nonlinear", "modal"):
                raise ResultContractError(
                    "Internal solver snapshot did not report a supported analysis type"
                )

            try:
                result_case: ResultCase = {
                    "case_id": case_input.case_id,
                    "name": case_input.name,
                    "symbol": case_input.symbol,
                    "analysis_type": analysis_type,
                    "support_node_ids": support_node_ids,
                }
                case_results = project_analysis_results(
                    model,
                    solver_result,
                    canonical_topology,
                    case_input.case_id,
                    support_node_ids,
                )
            except ResultContractError:
                raise
            except (KeyError, TypeError, ValueError) as error:
                raise ResultContractError(str(error)) from error
            cases.append(result_case)
            results.extend(case_results)
        except Exception as error:
            contextual = _with_case_context(error, case_input.case_id)
            if contextual is error:
                raise
            raise contextual from error

    if canonical_topology is None or canonical_units is None:
        raise InputValidationError("Analysis request must contain at least one case")
    result_set: AnalysisResultSet = {
        "kind": "analysis_result_set",
        "schema_version": "1.0",
        "units": canonical_units,
        "coordinate_system": {
            "name": COORDINATE_SYSTEM["name"],
            "handedness": COORDINATE_SYSTEM["handedness"],
            "axes": list(COORDINATE_SYSTEM["axes"]),
        },
        "cases": cases,
        "topology": canonical_topology,
        "results": results,
    }
    validate_analysis_result_set(result_set)
    return result_set


def _build_model_analysis_result_set(
    model: FemModel, analysis_type: str | None = None
) -> AnalysisResultSet:
    """Project one already-constructed programmatic model to the public contract."""

    solver_result = result_to_jsonable(model._run_solver_snapshot(analysis_type))
    effective_type = solver_result.get("analysis_type")
    if effective_type not in ("static", "material_nonlinear", "modal"):
        raise ResultContractError(
            "Internal solver snapshot did not report a supported analysis type"
        )
    try:
        source_data = _programmatic_source_data(model)
        topology = build_result_topology(source_data, model)
        support_node_ids = public_support_node_ids(model, topology)
        result_set: AnalysisResultSet = {
            "kind": "analysis_result_set",
            "schema_version": "1.0",
            "units": _result_units(model),
            "coordinate_system": {
                "name": COORDINATE_SYSTEM["name"],
                "handedness": COORDINATE_SYSTEM["handedness"],
                "axes": list(COORDINATE_SYSTEM["axes"]),
            },
            "cases": [
                {
                    "case_id": "1",
                    "name": "1",
                    "symbol": "1",
                    "analysis_type": effective_type,
                    "support_node_ids": support_node_ids,
                }
            ],
            "topology": topology,
            "results": project_analysis_results(
                model,
                solver_result,
                topology,
                "1",
                support_node_ids,
            ),
        }
        validate_analysis_result_set(result_set)
        return result_set
    except ResultContractError:
        raise
    except (KeyError, TypeError, ValueError) as error:
        raise ResultContractError(str(error)) from error


def _programmatic_source_data(model: FemModel) -> dict[str, Any]:
    """Recover public node identity while excluding solver-created stations."""

    public_nodes: set[int] = set()
    referenced_nodes: set[int] = set()
    for properties in model.mesh.elements.values():
        nodes = [int(node) for node in properties.get("nodes", ())]
        referenced_nodes.update(nodes)
        member_nodes = properties.get("member_nodes")
        if member_nodes is None:
            public_nodes.update(nodes)
        else:
            public_nodes.update(int(node) for node in member_nodes)
    public_nodes.update(set(model.mesh.nodes) - referenced_nodes)
    return {
        "nodes": {
            str(node): list(map(float, model.mesh.nodes[node]))
            for node in model.mesh.nodes
            if node in public_nodes
        }
    }


def _enumerate_cases(data: Any) -> list[_CaseInput]:
    if not isinstance(data, dict):
        raise InputValidationError("Analysis input must be a JSON object")
    if "node" in data:
        load_cases = data.get("load")
        if not isinstance(load_cases, Mapping) or not load_cases:
            raise InputValidationError("Legacy analysis requires at least one load case")
        normalized_ids: set[str] = set()
        result: list[_CaseInput] = []
        for original_id, case_value in load_cases.items():
            case_id = str(original_id)
            if not case_id.strip():
                raise InputValidationError("Legacy load case IDs must be non-empty")
            if case_id in normalized_ids:
                raise InputValidationError(
                    f"Duplicate normalized legacy load case ID: {case_id}"
                )
            normalized_ids.add(case_id)
            if not isinstance(case_value, Mapping):
                raise InputValidationError(
                    f"Legacy load case {case_id} must be an object",
                    case_id=case_id,
                )
            result.append(
                _CaseInput(
                    case_id=case_id,
                    name=_case_label(case_value.get("name"), case_id),
                    symbol=_case_label(case_value.get("symbol"), case_id),
                    selected_data=_select_legacy_case(data, original_id, case_id),
                )
            )
        return result

    if "nodes" in data:
        return [
            _CaseInput(
                case_id="1",
                name="1",
                symbol="1",
                selected_data=data,
            )
        ]
    raise InputValidationError("Model must contain nodes")


def _case_label(value: Any, fallback: str) -> str:
    return value if isinstance(value, str) and value.strip() else fallback


_ANALYSIS_PARAMETER_DEFAULTS: dict[str, Any] = {
    "n_load_steps": 10,
    "max_iterations": 50,
    "n_modes": 10,
    "load_factors": None,
    "displacement_control": None,
}


def _validate_request_work_budget(case_inputs: list[_CaseInput]) -> None:
    """Validate each case's analysis parameters before constructing any FemModel."""

    for case_input in case_inputs:
        _projected_case_work(case_input)


def _projected_case_work(case_input: _CaseInput) -> tuple[int, int]:
    data = case_input.selected_data
    case_id = case_input.case_id
    parameters = dict(_ANALYSIS_PARAMETER_DEFAULTS)
    case_data: Mapping[str, Any] | None = None
    load_cases = data.get("load")
    if isinstance(load_cases, Mapping) and load_cases:
        candidate = next(iter(load_cases.values()))
        if isinstance(candidate, Mapping):
            case_data = candidate
            for name in _ANALYSIS_PARAMETER_DEFAULTS:
                if name in case_data:
                    parameters[name] = case_data[name]

    top_level_parameters = data.get("analysis_params", {})
    if not isinstance(top_level_parameters, Mapping):
        raise InputValidationError(
            f"Analysis case {case_id}: analysis_params must be an object",
            case_id=case_id,
        )
    parameters.update(top_level_parameters)

    analysis_type = data.get("analysis_type")
    if analysis_type is None and case_data is not None:
        analysis_type = case_data.get("analysis_type")
    if analysis_type not in (None, "static", "modal", "material_nonlinear"):
        # The normal model validation owns unsupported analysis diagnostics.
        return 1, 0
    if analysis_type == "static":
        return 1, 0
    if analysis_type == "modal":
        return _positive_integer(parameters.get("n_modes"), "n_modes", case_id), 0

    steps = _nonlinear_step_count(parameters, case_id)
    max_iterations = _positive_integer(
        parameters.get("max_iterations"), "max_iterations", case_id
    )
    if max_iterations > MAX_ITERATIONS_PER_STEP:
        raise _budget_error(
            case_id,
            "max_iterations",
            max_iterations,
            MAX_ITERATIONS_PER_STEP,
        )

    nonlinear_iterations = steps * max_iterations
    if analysis_type == "material_nonlinear":
        return steps, nonlinear_iterations

    # Omitted analysis_type is inferred only after model construction. It can
    # become static or material_nonlinear, so reserve the nonlinear worst case.
    return steps, nonlinear_iterations


def _nonlinear_step_count(parameters: Mapping[str, Any], case_id: str) -> int:
    n_load_steps = _positive_integer(
        parameters.get("n_load_steps"), "n_load_steps", case_id
    )
    load_factors = parameters.get("load_factors")
    displacement_control = parameters.get("displacement_control")
    if load_factors is not None and displacement_control is not None:
        raise InputValidationError(
            f"Analysis case {case_id}: load_factors and displacement_control "
            "are mutually exclusive",
            case_id=case_id,
        )
    if load_factors is not None:
        if not isinstance(load_factors, (list, tuple)) or not load_factors:
            raise InputValidationError(
                f"Analysis case {case_id}: load_factors must be a nonempty sequence",
                case_id=case_id,
            )
        steps = len(load_factors)
    elif isinstance(displacement_control, Mapping) and "targets" in displacement_control:
        targets = displacement_control["targets"]
        if not isinstance(targets, (list, tuple)) or not targets:
            raise InputValidationError(
                f"Analysis case {case_id}: displacement-control targets must be "
                "a nonempty sequence",
                case_id=case_id,
            )
        steps = len(targets)
    else:
        steps = n_load_steps

    if steps > MAX_NONLINEAR_STEPS_PER_CASE:
        raise _budget_error(
            case_id,
            "nonlinear_steps",
            steps,
            MAX_NONLINEAR_STEPS_PER_CASE,
        )
    return steps


def _positive_integer(value: Any, name: str, case_id: str) -> int:
    if isinstance(value, bool) or not isinstance(value, int) or value <= 0:
        raise InputValidationError(
            f"Analysis case {case_id}: {name} must be a positive integer",
            case_id=case_id,
        )
    return value


def _budget_error(
    case_id: str,
    budget: str,
    requested: int,
    limit: int,
) -> InputValidationError:
    return InputValidationError(
        f"Analysis case work budget exceeded at case {case_id}: "
        f"{budget} {requested} exceeds limit {limit}",
        case_id=case_id,
        budget=budget,
        requested=requested,
        limit=limit,
    )


def _select_legacy_case(
    data: dict[str, Any], original_id: Any, case_id: str
) -> dict[str, Any]:
    try:
        return select_case(data, original_id)
    except (KeyError, TypeError, ValueError) as error:
        raise InputValidationError(
            f"Analysis case {case_id}: {error}", case_id=case_id
        ) from error


def _result_units(model: FemModel) -> dict[str, str]:
    units = model.model_metadata.get("units")
    if not isinstance(units, Mapping):
        raise ValueError("Normalized model metadata is missing units")
    names = ("system", "length", "force", "mass", "time")
    return {name: str(units[name]) for name in names}


def _with_case_context(error: Exception, case_id: str) -> Exception:
    if isinstance(error, ResultContractError):
        error.add_note(f"Analysis case: {case_id}")
        return error
    details = getattr(error, "details", None)
    if isinstance(details, dict):
        error.details = {**details, "case_id": case_id}
        return error
    if isinstance(error, np.linalg.LinAlgError):
        error.add_note(f"Analysis case: {case_id}")
        return error
    if isinstance(error, (KeyError, TypeError, ValueError)):
        return InputValidationError(
            f"Analysis case {case_id}: {error}", case_id=case_id
        )
    error.add_note(f"Analysis case: {case_id}")
    return error
