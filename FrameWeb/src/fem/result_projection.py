"""Project solver-native accepted states into canonical AnalysisResult rows."""

from __future__ import annotations

from collections.abc import Mapping, Sequence
from itertools import pairwise
import math
from typing import Any

import numpy as np

from .result_topology import (
    map_solver_nodes_to_public,
    member_element_ids,
    public_element_id_map,
)
from .solver_results import (
    canonicalize_modal_solution,
    normalize_solver_snapshots,
)


_DISPLACEMENT_COMPONENTS = ("dx", "dy", "dz", "rx", "ry", "rz")
_REACTION_COMPONENTS = ("fx", "fy", "fz", "mx", "my", "mz")


def project_analysis_results(
    model: Any,
    solver_result: Mapping[str, Any],
    topology: Mapping[str, Any],
    case_id: str,
    support_node_ids: Sequence[str],
) -> list[dict[str, Any]]:
    """Project one isolated case into independent canonical result snapshots."""
    case_id = str(case_id)
    if not case_id:
        raise ValueError("Canonical case_id must be non-empty")
    node_map = map_solver_nodes_to_public(model, topology)
    public_to_solver = {public: solver for solver, public in node_map.items()}
    topology_node_ids = [str(row["node_id"]) for row in topology["nodes"]]
    if len(public_to_solver) != len(topology_node_ids):
        raise ValueError("Canonical topology node IDs are not unique")
    supports = [str(node) for node in support_node_ids]
    if len(set(supports)) != len(supports) or any(node not in public_to_solver for node in supports):
        raise ValueError("Case support_node_ids must be unique canonical topology nodes")

    analysis_type = solver_result.get("analysis_type", "static")
    if analysis_type == "modal":
        return _project_modal_results(
            model, solver_result, topology, case_id, node_map, public_to_solver
        )

    warnings = getattr(model.solver, "analysis_warnings", ())
    snapshots = normalize_solver_snapshots(solver_result, warnings=warnings)
    return [
        {
            "case_id": case_id,
            "state": dict(snapshot.state),
            "node_displacements": _project_node_values(
                snapshot.values, "node_displacements", topology_node_ids, public_to_solver,
                _DISPLACEMENT_COMPONENTS,
            ),
            "support_reactions": _project_node_values(
                snapshot.values, "reaction_forces", supports, public_to_solver,
                _REACTION_COMPONENTS,
            ),
            "member_section_forces": _project_member_forces(
                model, snapshot.values, topology
            ),
            "shell_results": _project_shell_results(model, snapshot.values, topology),
            "solid_results": _project_solid_results(model, snapshot.values, topology),
            "diagnostics": _project_static_diagnostics(model, snapshot, node_map),
        }
        for snapshot in snapshots
    ]


def _project_static_diagnostics(model, snapshot, node_map):
    diagnostics = dict(snapshot.diagnostics)
    contribution = getattr(model.solver, 'spatial_load_contribution', None)
    if snapshot.state['kind'] != 'static' or contribution is None:
        return diagnostics
    vector_fields = ('resultant', 'moment', 'nodal_resultant', 'nodal_moment')
    scalar_fields = ('force_error', 'moment_error')

    def totals(value):
        return {
            **{name: _named(_triple(getattr(value, name), name), ('x', 'y', 'z'))
               for name in vector_fields},
            **{name: _finite(getattr(value, name), name) for name in scalar_fields},
        }

    node_loads = []
    for index, node in enumerate(contribution.node_ids):
        if node not in node_map:
            raise ValueError(f'Spatial audit node {node} is missing from public topology')
        force = contribution.dof_loads[index*contribution.stride:index*contribution.stride+3]
        node_loads.append({'node_id': node_map[node], 'force': _named(_triple(force, 'node force'), ('x', 'y', 'z'))})
    diagnostics['spatial_loads'] = {
        **totals(contribution),
        'node_loads': node_loads,
        'loads': [dict(load_id=item.load_id, panel_id=item.panel_id, feature=item.feature,
                       integrated_length=item.integrated_length, clipped_area=item.clipped_area,
                       **totals(item)) for item in contribution.loads],
    }
    return diagnostics


def _project_node_values(
    snapshot: Mapping[str, Any],
    field: str,
    public_ids: Sequence[str],
    public_to_solver: Mapping[str, int],
    components: Sequence[str],
) -> list[dict[str, Any]]:
    source = _mapping(snapshot, field)
    rows = []
    for public_id in public_ids:
        solver_id = public_to_solver[public_id]
        values = _mapping_item(source, solver_id, field)
        rows.append(
            {
                "node_id": public_id,
                "components": {
                    name: _finite(values.get(name, 0.0), f"{field}.{public_id}.{name}")
                    for name in components
                },
            }
        )
    return rows


def _project_member_forces(model, snapshot, topology):
    stresses = _mapping(snapshot, "element_stresses")
    projected = []
    for member in topology["members"]:
        member_id = str(member["member_id"])
        stations = member["stations"]
        elements = member_element_ids(model, member_id)
        extents = [(_element_extent(model, member, element_id), element_id) for element_id in elements]
        tolerance = 1e-8 * max(1.0, float(stations[-1]["position"]))
        segments = []
        for index, (station_i, station_j) in enumerate(pairwise(stations)):
            start = float(station_i["position"])
            end = float(station_j["position"])
            contained = [
                (extent, element_id)
                for extent, element_id in extents
                if extent[0] >= start - tolerance and extent[1] <= end + tolerance
            ]
            contained.sort(key=lambda item: (item[0][0], item[0][1], item[1]))
            if not contained:
                raise ValueError(
                    f"Canonical member {member_id} segment {station_i['station_id']}-"
                    f"{station_j['station_id']} has no solved element"
                )
            first = _mapping_item(stresses, contained[0][1], "element_stresses")
            last = _mapping_item(stresses, contained[-1][1], "element_stresses")
            segments.append(
                {
                    "segment_id": f"S{index}-S{index + 1}",
                    "station_i": str(station_i["station_id"]),
                    "station_j": str(station_j["station_id"]),
                    "length": _finite(end - start, f"member {member_id} segment length"),
                    "i_end": _canonical_i_end(first.get("i_end")),
                    "j_end": _canonical_j_end(last.get("j_end")),
                }
            )
        projected.append({"member_id": member_id, "segments": segments})
    return projected


def _element_extent(model, member, element_id):
    properties = model.mesh.elements[element_id]
    if "member_start" in properties and "member_end" in properties:
        return float(properties["member_start"]), float(properties["member_end"])
    origin = np.array(
        [member["local_frame"]["origin"][name] for name in ("x", "y", "z")],
        dtype=float,
    )
    axis = np.array(
        [member["local_frame"]["x_axis"][name] for name in ("x", "y", "z")],
        dtype=float,
    )
    positions = [
        float((np.asarray(model.mesh.nodes[node], dtype=float) - origin) @ axis)
        for node in properties["nodes"]
    ]
    return min(positions), max(positions)


def _canonical_i_end(value):
    fx, fy, fz, mx, my, mz = _six(value, "i_end")
    return {"fx": -fx, "fy": fy, "fz": fz, "mx": -mx, "my": -my, "mz": mz}


def _canonical_j_end(value):
    fx, fy, fz, mx, my, mz = _six(value, "j_end")
    return {"fx": fx, "fy": -fy, "fz": -fz, "mx": mx, "my": my, "mz": -mz}


def _project_shell_results(model, snapshot, topology):
    if not topology["shell_elements"]:
        return []
    source = _mapping(snapshot, "shell_results")
    public_to_solver = public_element_id_map(model, topology, "shell")
    projected = []
    for public in topology["shell_elements"]:
        public_id = str(public["element_id"])
        solver_id = public_to_solver[public_id]
        values = _mapping_item(source, solver_id, "shell_results")
        resultants = _mapping(values, "resultants")
        membrane = _triple(resultants.get("membrane"), f"shell {public_id} membrane")
        moment = _triple(resultants.get("moment"), f"shell {public_id} moment")
        shear = _pair(resultants.get("shear"), f"shell {public_id} shear")
        thickness = _finite(
            model.elements[solver_id].thickness, f"shell {public_id} thickness"
        )
        if thickness <= 0:
            raise ValueError(f"Shell {public_id} thickness must be positive")
        average = membrane / thickness
        bending = 6.0 * moment / thickness**2
        projected.append(
            {
                "element_id": public_id,
                "locations": [
                    {
                        "location_id": "element_average",
                        "membrane_force": _named(membrane, ("nx", "ny", "nxy")),
                        "bending_moment": _named(moment, ("mx", "my", "mxy")),
                        "transverse_shear": _named(shear, ("qx", "qy")),
                        "top_stress": _named(average + bending, ("sx", "sy", "txy")),
                        "bottom_stress": _named(average - bending, ("sx", "sy", "txy")),
                    }
                ],
            }
        )
    return projected


def _project_solid_results(model, snapshot, topology):
    if not topology["solid_elements"]:
        return []
    source = _mapping(snapshot, "element_stresses")
    public_to_solver = public_element_id_map(model, topology, "solid")
    projected = []
    for public in topology["solid_elements"]:
        public_id = str(public["element_id"])
        solver_id = public_to_solver[public_id]
        values = _mapping_item(source, solver_id, "element_stresses")
        stresses = np.asarray(values.get("stress"), dtype=float)
        strains = np.asarray(values.get("strain"), dtype=float)
        locations = public["result_locations"]
        expected_shape = (len(locations), 6)
        if stresses.shape != expected_shape or strains.shape != expected_shape:
            raise ValueError(
                f"Solid {public_id} result rows do not match canonical result locations"
            )
        if not np.isfinite(stresses).all() or not np.isfinite(strains).all():
            raise ValueError(f"Solid {public_id} result values must be finite")
        projected.append(
            {
                "element_id": public_id,
                "locations": [
                    {
                        "location_id": str(location["location_id"]),
                        "stress": _named(stress, ("sx", "sy", "sz", "txy", "tyz", "tzx")),
                        "strain": _named(strain, ("ex", "ey", "ez", "gxy", "gyz", "gzx")),
                    }
                    for location, stress, strain in zip(
                        locations, stresses, strains, strict=True
                    )
                ],
            }
        )
    return projected


def _project_modal_results(
    model, solver_result, topology, case_id, solver_to_public, public_to_solver
):
    lexical_solver_nodes = [
        public_to_solver[public]
        for public in sorted(public_to_solver)
    ]
    values, vectors, groups, zero_tolerance = canonicalize_modal_solution(
        model, solver_result, lexical_solver_nodes
    )
    topology_node_ids = [str(row["node_id"]) for row in topology["nodes"]]
    public_order_solver_nodes = [public_to_solver[node] for node in topology_node_ids]
    results = []
    for index, (eigenvalue, group) in enumerate(zip(values, groups, strict=True)):
        formatted = model.solver._format_node_displacements(vectors[:, index], model.mesh)
        results.append(
            {
                "case_id": case_id,
                "state": {
                    "kind": "mode",
                    "index": index,
                    "eigenvalue": float(eigenvalue),
                    "frequency": float(math.sqrt(eigenvalue) / (2 * math.pi)),
                    "degeneracy_group": int(group),
                },
                "node_mode_shapes": [
                    {
                        "node_id": public_id,
                        "components": {
                            name: _finite(
                                formatted[solver_id].get(name, 0.0),
                                f"mode {index} node {public_id} {name}",
                            )
                            for name in _DISPLACEMENT_COMPONENTS
                        },
                    }
                    for public_id, solver_id in zip(
                        topology_node_ids, public_order_solver_nodes, strict=True
                    )
                ],
                "diagnostics": {
                    "warnings": [str(value) for value in model.solver.analysis_warnings],
                    "normalization": "mass",
                    "eigenvalue_tolerance": zero_tolerance,
                    "degeneracy_relative_tolerance": 1e-8,
                },
            }
        )
    return results


def _mapping(container, key):
    if not isinstance(container, Mapping):
        raise ValueError(f"Analysis snapshot is not an object while reading {key}")
    value = container.get(key)
    if not isinstance(value, Mapping):
        raise ValueError(f"Analysis snapshot is missing {key}")
    return value


def _mapping_item(container, key, label):
    value = container.get(key, container.get(str(key)))
    if not isinstance(value, Mapping):
        raise ValueError(f"{label} is missing entry {key}")
    return value


def _six(value, label):
    array = np.asarray(value, dtype=float)
    if array.shape != (6,) or not np.isfinite(array).all():
        raise ValueError(f"Beam result {label} must contain six finite values")
    return tuple(map(float, array))


def _triple(value, label):
    return _fixed_array(value, 3, label)


def _pair(value, label):
    return _fixed_array(value, 2, label)


def _fixed_array(value, size, label):
    array = np.asarray(value, dtype=float)
    if array.shape != (size,) or not np.isfinite(array).all():
        raise ValueError(f"{label} must contain {size} finite values")
    return array


def _named(values, names):
    return {name: float(value) for name, value in zip(names, values, strict=True)}


def _finite(value, label):
    if isinstance(value, (bool, np.bool_)):
        raise ValueError(f"{label} must be a finite number")
    try:
        result = float(value)
    except (TypeError, ValueError) as error:
        raise ValueError(f"{label} must be a finite number") from error
    if not math.isfinite(result):
        raise ValueError(f"{label} must be a finite number")
    return result
