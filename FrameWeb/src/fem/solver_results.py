"""Accepted solver snapshots and canonical snapshot normalization."""

from copy import deepcopy
from dataclasses import dataclass
import hashlib
import json
import math
from collections.abc import Mapping, Sequence
from typing import Any

import numpy as np

from ._version import __version__
from .convergence import evaluate_equilibrium


RESULT_SCHEMA_VERSION = "1.0"
DEFAULT_MODEL_METADATA = {
    "coordinate_system": {
        "name": "global_cartesian",
        "axes": ["x", "y", "z"],
    },
    "units": {
        "system": "consistent_user_defined",
        "length": "unspecified",
        "force": "unspecified",
        "mass": "unspecified",
        "time": "unspecified",
    },
}


@dataclass(frozen=True)
class SolverSnapshot:
    """One independent accepted solver state, before public-ID projection."""

    state: dict[str, Any]
    values: Mapping[str, Any]
    diagnostics: dict[str, Any]


def normalize_model_metadata(metadata):
    """Merge user declarations onto the non-SI-assuming public defaults."""
    result = deepcopy(DEFAULT_MODEL_METADATA)
    if metadata:
        if not isinstance(metadata, dict):
            raise ValueError("model_metadata must be an object")
        unknown = set(metadata) - set(DEFAULT_MODEL_METADATA)
        if unknown:
            raise ValueError(f"Unknown model_metadata fields: {sorted(unknown)}")
        for key, value in metadata.items():
            if not isinstance(value, dict):
                raise ValueError(f"model_metadata.{key} must be an object")
            result[key].update(deepcopy(value))
    return result


def _effective_analysis_parameters(analysis_type, parameters):
    names = {
        "static": (),
        "modal": ("n_modes",),
        "material_nonlinear": (
            "n_load_steps", "max_iterations", "tolerance",
            "load_factors", "displacement_control",
        ),
    }[analysis_type]
    return {
        name: deepcopy(parameters[name])
        for name in names
        if name in parameters and parameters[name] is not None
    }


def build_result_metadata(model, analysis_type, solver_snapshot):
    """Build deterministic provenance and final solver diagnostics."""
    from .file_io import model_to_jsonable
    from .nonlinear.support_laws import support_models

    declarations = normalize_model_metadata(model.model_metadata)
    model_data = {
        "mesh": model.mesh,
        "boundary": model.boundary,
        "material": model.material,
        "section": model.section,
        "analysis_type": analysis_type,
        "analysis_params": model.analysis_params,
        "model_metadata": declarations,
    }
    canonical = json.dumps(
        model_to_jsonable(model_data), sort_keys=True, separators=(",", ":"),
        ensure_ascii=False, allow_nan=False,
    ).encode("utf-8")

    solver = model.solver
    history = solver.convergence_history
    step_iterations = [int(step.get("iterations", 0)) for step in solver.step_results]
    residual_norm = residual_scale = relative_residual = None
    if history:
        residual_norm = float(history[-1]["residual_norm"])
        residual_scale = float(history[-1]["residual_scale"])
        relative_residual = float(history[-1]["relative_residual"])
    elif analysis_type == "static" and solver.displacement is not None:
        applied = solver.load_pattern.at(solver.load_factor)
        resolved = solver._resolve_boundary_dofs(
            model.boundary, len(solver.displacement), solver.layout.stride
        )
        equilibrium = evaluate_equilibrium(
            resolved, applied, solver._last_internal_force, solver.displacement,
            length=solver.characteristic_length)
        residual_norm = equilibrium.residual_norm
        residual_scale = equilibrium.residual_scale
        relative_residual = equilibrium.relative_residual
    elif analysis_type == "modal" and solver_snapshot.get("eigenpair_residuals"):
        relative_residual = float(max(solver_snapshot["eigenpair_residuals"]))

    return {
        "schema_version": RESULT_SCHEMA_VERSION,
        "product": {"name": "FEMPython", "version": __version__},
        "input_sha256": hashlib.sha256(canonical).hexdigest(),
        "analysis": {
            "type": analysis_type,
            **({'support_models': support_models(
                    params for supports in model.boundary.nonlinear_spring_supports.values()
                    for params in supports.values())}
               if model.boundary.nonlinear_spring_supports else {}),
            **({'beam_formulation': (
                    'jr_axial_force_updated_inertia_v1'
                    if any(getattr(e, 'axial_force_tables', {}) for e in model.elements.values())
                    else 'jr_updated_inertia_v1')}
               if analysis_type == 'material_nonlinear' and any(
                   getattr(e, 'committed_bending_states', {}) for e in model.elements.values()
               ) else {}),
            "parameters": _effective_analysis_parameters(
                analysis_type, model.analysis_params
            ),
        },
        **declarations,
        "solver": {
            "converged": True,
            "iterations": sum(step_iterations),
            "step_iterations": step_iterations,
            "residual_norm": residual_norm,
            "residual_scale": residual_scale,
            "relative_residual": relative_residual,
            "convergence_measure": {
                "type": "dimensionless_generalized_l2",
                "characteristic_length": solver.characteristic_length,
                "characteristic_length_source": solver.characteristic_length_source,
                "force_components": "[Fx,Fy,Fz,Mx/L,My/L,Mz/L]",
                "displacement_components": "[dx/L,dy/L,dz/L,rx,ry,rz]",
                "residual_absolute_floor": None,
                "displacement_reference_floor": 1.0,
            },
            "warnings": deepcopy(solver.analysis_warnings),
            "high_precision": solver.precise_end_forces is not None,
        },
    }


def normalize_solver_snapshots(
    result: Mapping[str, Any], *, warnings: Sequence[str] = ()
) -> list[SolverSnapshot]:
    """Return a flat, case-local sequence of accepted solver states.

    The low-level solver retains its historical final-result envelope until the
    HTTP cutover.  Canonical consumers use this function and therefore never
    expose that envelope or nested ``step_results``.
    """
    analysis_type = result.get("analysis_type", "static")
    warning_list = [str(warning) for warning in warnings]
    if analysis_type == "material_nonlinear":
        steps = result.get("step_results")
        if not isinstance(steps, Sequence) or isinstance(steps, (str, bytes)) or not steps:
            raise ValueError("Nonlinear analysis has no accepted solver steps")
        snapshots = []
        root_history = result.get("convergence_history", ())
        for index, step in enumerate(steps):
            if not isinstance(step, Mapping):
                raise ValueError(f"Nonlinear step {index} is not an object")
            factor = _finite_number(step.get("lambda"), f"nonlinear step {index} load factor")
            records = step.get("convergence_history")
            if records is None:
                records = [
                    record
                    for record in root_history
                    if isinstance(record, Mapping) and record.get("step") == index + 1
                ]
            iterations = _canonical_iterations(records, index)
            snapshots.append(
                SolverSnapshot(
                    state={
                        "kind": "load_step",
                        "index": index,
                        "load_factor": factor,
                        "is_final": index == len(steps) - 1,
                    },
                    values=step,
                    diagnostics={"warnings": warning_list.copy(), "iterations": iterations},
                )
            )
        return snapshots
    if analysis_type == "modal":
        modes = result.get("modes")
        eigenvalues = result.get("eigenvalues")
        frequencies = result.get("frequencies")
        if not all(
            isinstance(values, Sequence) and not isinstance(values, (str, bytes))
            for values in (modes, eigenvalues, frequencies)
        ):
            raise ValueError("Modal result is missing modes, eigenvalues, or frequencies")
        if not modes or len(modes) != len(eigenvalues) or len(modes) != len(frequencies):
            raise ValueError("Modal result arrays must have the same nonzero length")
        return [
            SolverSnapshot(
                state={
                    "kind": "mode",
                    "index": index,
                    "eigenvalue": _finite_number(eigenvalue, f"mode {index} eigenvalue"),
                    "frequency": _finite_number(frequencies[index], f"mode {index} frequency"),
                },
                values={"node_mode_shapes": modes[index]},
                diagnostics={"warnings": warning_list.copy()},
            )
            for index, eigenvalue in enumerate(eigenvalues)
        ]
    if analysis_type != "static":
        raise ValueError(f"Unsupported solver snapshot analysis type: {analysis_type}")
    return [
        SolverSnapshot(
            state={"kind": "static", "index": 0},
            values=result,
            diagnostics={"warnings": warning_list.copy()},
        )
    ]


def canonicalize_modal_solution(model, result, solver_node_order):
    """Canonicalize modal vectors without changing the eigensolver algorithm.

    Returns ``(eigenvalues, eigenvectors, degeneracy_groups, zero_tolerance)``.
    Vectors are full solver-DOF columns, mass normalized, deterministic within
    degenerate subspaces, and sign canonicalized by public-node lexical order.
    """
    solver = model.solver
    values = np.asarray(result.get("eigenvalues"), dtype=float)
    vectors = np.asarray(result.get("eigenvectors"), dtype=float)
    if values.ndim != 1 or vectors.shape != (solver.layout.size, len(values)):
        raise ValueError("Modal eigenvalue/eigenvector dimensions are inconsistent")
    if not np.isfinite(values).all() or not np.isfinite(vectors).all():
        raise ValueError("Modal eigenpairs must be finite")

    resolved = solver._resolve_boundary_dofs(
        model.boundary, solver.layout.size, solver.layout.stride
    )
    supported_stiffness = resolved.add_spring_stiffness(solver.assembled_stiffness)
    free = np.asarray(resolved.free, dtype=int)
    reduced_stiffness = supported_stiffness[free][:, free]
    reduced_mass = solver.assembled_mass[free][:, free]
    mass_scale = float(np.max(np.abs(reduced_mass.diagonal()), initial=0.0))
    if mass_scale <= 0:
        raise ValueError("Modal canonicalization requires positive free-DOF mass")
    eigenvalue_scale = (
        float(np.max(np.abs(reduced_stiffness.diagonal()), initial=0.0)) / mass_scale
    )
    zero_tolerance = max(
        np.finfo(float).eps * len(free) * eigenvalue_scale,
        1e-14 * eigenvalue_scale,
    )
    if np.any(values <= zero_tolerance):
        raise ValueError(
            "Canonical modal output requires every eigenvalue to be positive "
            f"and greater than {zero_tolerance:.6g}"
        )

    order = np.argsort(values, kind="stable")
    values = values[order]
    vectors = vectors[:, order].copy()
    mass = solver.assembled_mass.tocsr()
    groups = _degeneracy_groups(values, zero_tolerance)
    lexical_dofs = _lexical_dofs(solver, solver_node_order)

    for group in sorted(set(groups)):
        columns = [index for index, value in enumerate(groups) if value == group]
        basis = vectors[:, columns]
        basis = _mass_orthonormalize(basis, mass)
        if len(columns) > 1:
            canonical = []
            for dof in lexical_dofs:
                coefficients = basis.T @ np.asarray(mass[:, dof].toarray()).ravel()
                candidate = basis @ coefficients
                for accepted in canonical:
                    candidate -= accepted * float(accepted @ (mass @ candidate))
                norm = float(candidate @ (mass @ candidate))
                if norm > np.finfo(float).eps * max(1, solver.layout.size):
                    canonical.append(candidate / math.sqrt(norm))
                if len(canonical) == len(columns):
                    break
            if len(canonical) != len(columns):
                raise ValueError("Could not construct a deterministic degenerate modal basis")
            basis = np.column_stack(canonical)
        for local_index in range(basis.shape[1]):
            vector = basis[:, local_index]
            ordered = np.abs(vector[lexical_dofs])
            pivot = lexical_dofs[int(np.argmax(ordered))]
            if vector[pivot] < 0:
                vector *= -1
        vectors[:, columns] = basis
    return values, vectors, groups, float(zero_tolerance)


def _canonical_iterations(records, step_index):
    if not isinstance(records, Sequence) or isinstance(records, (str, bytes)):
        raise ValueError(f"Nonlinear step {step_index} convergence history is not an array")
    canonical = []
    for index, record in enumerate(records):
        if not isinstance(record, Mapping):
            raise ValueError(f"Nonlinear step {step_index} iteration {index} is not an object")
        correction = record.get("increment_norm")
        if correction is None:
            correction = 0.0
        canonical.append(
            {
                "index": index,
                "residual_norm": _finite_number(
                    record.get("residual_norm"),
                    f"nonlinear step {step_index} iteration {index} residual",
                ),
                "correction_norm": _finite_number(
                    correction,
                    f"nonlinear step {step_index} iteration {index} correction",
                ),
                "converged": index == len(records) - 1,
            }
        )
    if not canonical:
        raise ValueError(f"Nonlinear step {step_index} has no convergence diagnostics")
    return canonical


def _finite_number(value, label):
    if isinstance(value, (bool, np.bool_)):
        raise ValueError(f"{label} must be a finite number")
    try:
        result = float(value)
    except (TypeError, ValueError) as error:
        raise ValueError(f"{label} must be a finite number") from error
    if not math.isfinite(result):
        raise ValueError(f"{label} must be a finite number")
    return result


def _degeneracy_groups(values, zero_tolerance):
    relative_tolerance = 1e-8
    groups = [0]
    for previous, current in zip(values[:-1], values[1:]):
        same = abs(current - previous) <= relative_tolerance * max(
            abs(previous), abs(current), zero_tolerance
        )
        groups.append(groups[-1] if same else groups[-1] + 1)
    return groups


def _mass_orthonormalize(vectors, mass):
    accepted = []
    for column in vectors.T:
        vector = column.copy()
        for previous in accepted:
            vector -= previous * float(previous @ (mass @ vector))
        norm = float(vector @ (mass @ vector))
        if not math.isfinite(norm) or norm <= 0:
            raise ValueError("Modal vector has a nonpositive mass norm")
        accepted.append(vector / math.sqrt(norm))
    return np.column_stack(accepted)


def _lexical_dofs(solver, solver_node_order):
    result = []
    for node in solver_node_order:
        start = solver.layout.node_offsets[node]
        result.extend(range(start, start + solver.layout.stride))
    if len(result) != solver.layout.size or len(set(result)) != solver.layout.size:
        raise ValueError("Modal public-node order does not cover every solver DOF")
    return result


def snapshot(solver, mesh, boundary, elements, solution, force, step, factor, nonlinear):
    u, internal, correction, iterations = solution
    resolved = solver._resolve_boundary_dofs(boundary, len(u), solver.layout.stride)
    reaction = resolved.reactions(
        internal, force, u, correction=correction if not nonlinear else None,
        precise=solver.precise_reactions if not nonlinear else None)
    result = dict(
        step=step, **{'lambda': factor}, displacement=u.copy(),
        node_displacements=solver._format_node_displacements(u, mesh),
        reaction_forces=solver._format_reactions(reaction, boundary, solver.layout.stride),
        converged=True, iterations=iterations,
        convergence_history=deepcopy([
            record for record in solver.convergence_history
            if record.get('step') == step
        ]),
    )
    if nonlinear:
        if solver.support_springs is not None:
            result['support_response'] = solver.support_springs.snapshot()
        result['element_stresses'] = solver._element_end_forces(elements, u, solver.layout.stride)
        result['curvature'] = solver._element_curvatures(elements, u, solver.layout.stride)
        result['section_response'] = {
            key: element.get_section_response() for key, element in elements.items()
            if getattr(element, 'committed_bending_states', {})
        }
    elif correction is not None:
        result['displacement_correction'] = correction.copy()
        if getattr(solver, 'interpolated_displacements', {}):
            result['interpolated_displacements'] = deepcopy(solver.interpolated_displacements)
        if getattr(solver, 'precise_end_forces', None) is not None:
            result['precise_end_forces'] = deepcopy(solver.precise_end_forces)
    if solver.spatial_load_contribution is not None:
        result['spatial_load_contribution'] = solver.spatial_load_contribution.to_dict()
        # These are global element-node equilibrium forces. Constitutive shell
        # stresses, stress resultants and edge forces keep their existing meaning.
        equilibrium = {}
        for key, element, indices in solver.layout.elements(elements):
            if hasattr(element, 'calculate_shell_results'):
                stiffness = element.get_stiffness_matrix()
                values = stiffness @ u[indices]
                if correction is not None:
                    values += stiffness @ correction[indices]
                values -= solver._shell_direct_loads.get(key, np.zeros(len(indices)))
                equilibrium[key] = {
                    'node_ids': list(element.node_ids),
                    'forces': values.reshape(-1, 6).tolist(),
                }
        result['element_nodal_equilibrium_forces'] = equilibrium
    return deepcopy(result)


def final_result(solver, nonlinear):
    last = deepcopy(solver.step_results[-1])
    if nonlinear:
        # The historical low-level result has no final element_stresses key;
        # FemModel consumes the accepted end-force snapshot during postprocess.
        keys = ('displacement', 'node_displacements', 'reaction_forces', 'curvature',
                'section_response', 'support_response', 'converged')
        if last.get('control_mode') == 'displacement':
            keys += ('lambda', 'control_mode', 'control_node', 'control_dof',
                     'control_displacement')
        result = {key: last[key] for key in keys if key in last}
        result.update(step_results=deepcopy(solver.step_results),
                      convergence_history=deepcopy(solver.convergence_history),
                      analysis_type='material_nonlinear')
    else:
        keys = ('displacement', 'node_displacements', 'reaction_forces', 'displacement_correction',
                'interpolated_displacements', 'precise_end_forces',
                'spatial_load_contribution', 'element_nodal_equilibrium_forces')
        result = {key: last[key] for key in keys if key in last}
    return result


def legacy_nonlinear_result(result, stride):
    """Keep the existing HTTP/FemModel/NonlinearSolver 3DOF output schema."""
    if stride == 3:
        for item in [result, *result['step_results']]:
            item['node_displacements'] = {
                node: {key: values[key] for key in ('dx', 'dy', 'dz')}
                for node, values in item['node_displacements'].items()
            }
    return result
