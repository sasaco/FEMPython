"""Typed AnalysisResultSet v1 wire contract and strict semantic validation."""

from __future__ import annotations

import math
from collections.abc import Mapping, Sequence
from typing import Any, Literal, TypedDict, cast


# Per-case nonlinear guardrails retain the largest checked-in example
# (100 steps, 1,000 iterations).
MAX_NONLINEAR_STEPS_PER_CASE = 1_000
MAX_ITERATIONS_PER_STEP = 1_000
DEGENERACY_RELATIVE_TOLERANCE = 1e-8
_FRAME_TOLERANCE = 1e-8
_LENGTH_TOLERANCE = 1e-9


class ResultContractError(RuntimeError):
    """Raised when a would-be success payload violates AnalysisResultSet v1."""


class Vector3(TypedDict):
    x: float
    y: float
    z: float


class DisplacementComponents(TypedDict):
    dx: float
    dy: float
    dz: float
    rx: float
    ry: float
    rz: float


class ForceComponents(TypedDict):
    fx: float
    fy: float
    fz: float
    mx: float
    my: float
    mz: float


class UnitSystem(TypedDict):
    system: str
    length: str
    force: str
    mass: str
    time: str


class CoordinateSystem(TypedDict):
    name: Literal["global_cartesian"]
    handedness: Literal["right"]
    axes: list[Literal["x", "y", "z"]]


class CoordinateFrame(TypedDict):
    origin: Vector3
    x_axis: Vector3
    y_axis: Vector3
    z_axis: Vector3


class ResultCase(TypedDict):
    case_id: str
    name: str
    symbol: str
    analysis_type: Literal["static", "material_nonlinear", "modal"]
    support_node_ids: list[str]


class TopologyNode(TypedDict):
    node_id: str
    coordinates: Vector3
    source_node_id: str | None
    generated: bool


class MemberStation(TypedDict):
    station_id: str
    position: float


class TopologyMember(TypedDict):
    member_id: str
    node_i: str
    node_j: str
    local_frame: CoordinateFrame
    stations: list[MemberStation]


class ResultTopology(TypedDict):
    nodes: list[TopologyNode]
    members: list[TopologyMember]
    shell_elements: list[dict[str, Any]]
    solid_elements: list[dict[str, Any]]


class NodeDisplacement(TypedDict):
    node_id: str
    components: DisplacementComponents


class SupportReaction(TypedDict):
    node_id: str
    components: ForceComponents


class AnalysisResultSet(TypedDict):
    kind: Literal["analysis_result_set"]
    schema_version: Literal["1.0"]
    units: UnitSystem
    coordinate_system: CoordinateSystem
    cases: list[ResultCase]
    topology: ResultTopology
    results: list[dict[str, Any]]


COORDINATE_SYSTEM: CoordinateSystem = {
    "name": "global_cartesian",
    "handedness": "right",
    "axes": ["x", "y", "z"],
}


def validate_analysis_result_set(value: Mapping[str, Any]) -> None:
    """Validate the complete structural and cross-reference v1 contract.

    JSON Schema owns the language-neutral object shape. This function also
    enforces the semantic invariants JSON Schema cannot express: finite values,
    ordered exact coverage, references, case/state order and final markers.
    """

    root = _object(
        value,
        "$",
        {"kind", "schema_version", "units", "coordinate_system", "cases", "topology", "results"},
    )
    if root["kind"] != "analysis_result_set":
        _fail("$.kind", "must equal 'analysis_result_set'")
    if root["schema_version"] != "1.0":
        _fail("$.schema_version", "must equal '1.0'")
    _validate_units(root["units"])
    if root["coordinate_system"] != COORDINATE_SYSTEM:
        _fail("$.coordinate_system", "must be the canonical global right-handed frame")

    topology = _validate_topology(root["topology"])
    cases = _validate_cases(root["cases"], topology["node_ids"])
    _validate_results(root["results"], cases, topology)


def _validate_units(value: Any) -> None:
    units = _object(value, "$.units", {"system", "length", "force", "mass", "time"})
    for key in ("system", "length", "force", "mass", "time"):
        _id(units[key], f"$.units.{key}")


def _validate_topology(value: Any) -> dict[str, Any]:
    topology = _object(
        value,
        "$.topology",
        {"nodes", "members", "shell_elements", "solid_elements"},
    )
    nodes = _array(topology["nodes"], "$.topology.nodes")
    node_ids: list[str] = []
    for index, item in enumerate(nodes):
        path = f"$.topology.nodes[{index}]"
        node = _object(item, path, {"node_id", "coordinates", "source_node_id", "generated"})
        node_ids.append(_id(node["node_id"], f"{path}.node_id"))
        _vector3(node["coordinates"], f"{path}.coordinates")
        source_id = node["source_node_id"]
        if source_id is not None:
            _id(source_id, f"{path}.source_node_id")
        if type(node["generated"]) is not bool:
            _fail(f"{path}.generated", "must be a boolean")
    _unique(node_ids, "$.topology.nodes", "node_id")
    node_set = set(node_ids)

    members = _array(topology["members"], "$.topology.members")
    member_ids: list[str] = []
    member_stations: dict[str, list[tuple[str, float]]] = {}
    for index, item in enumerate(members):
        path = f"$.topology.members[{index}]"
        member = _object(item, path, {"member_id", "node_i", "node_j", "local_frame", "stations"})
        member_id = _id(member["member_id"], f"{path}.member_id")
        member_ids.append(member_id)
        for field in ("node_i", "node_j"):
            reference = _id(member[field], f"{path}.{field}")
            if reference not in node_set:
                _fail(f"{path}.{field}", f"references unknown node {reference!r}")
        if member["node_i"] == member["node_j"]:
            _fail(path, "member endpoints must differ")
        _coordinate_frame(member["local_frame"], f"{path}.local_frame")
        stations = _array(member["stations"], f"{path}.stations")
        if len(stations) < 2:
            _fail(f"{path}.stations", "must contain at least two stations")
        parsed: list[tuple[str, float]] = []
        for station_index, station_value in enumerate(stations):
            station_path = f"{path}.stations[{station_index}]"
            station = _object(station_value, station_path, {"station_id", "position"})
            station_id = _id(station["station_id"], f"{station_path}.station_id")
            if station_id != f"S{station_index}":
                _fail(f"{station_path}.station_id", f"must equal 'S{station_index}'")
            position = _finite(station["position"], f"{station_path}.position", minimum=0)
            if parsed and position <= parsed[-1][1]:
                _fail(f"{station_path}.position", "must be strictly increasing")
            parsed.append((station_id, position))
        member_stations[member_id] = parsed
    _unique(member_ids, "$.topology.members", "member_id")

    shells = _array(topology["shell_elements"], "$.topology.shell_elements")
    shell_ids: list[str] = []
    shell_locations: dict[str, list[str]] = {}
    shell_sizes = {"triangle3": 3, "quadrilateral4": 4}
    for index, item in enumerate(shells):
        path = f"$.topology.shell_elements[{index}]"
        shell = _object(item, path, {"element_id", "element_type", "node_ids", "local_frame", "result_locations"})
        element_id = _id(shell["element_id"], f"{path}.element_id")
        shell_ids.append(element_id)
        element_type = shell["element_type"]
        if element_type not in shell_sizes:
            _fail(f"{path}.element_type", "must be triangle3 or quadrilateral4")
        _node_references(shell["node_ids"], f"{path}.node_ids", node_set, shell_sizes[element_type])
        _coordinate_frame(shell["local_frame"], f"{path}.local_frame")
        locations = _array(shell["result_locations"], f"{path}.result_locations")
        if len(locations) != 1:
            _fail(f"{path}.result_locations", "must contain exactly element_average")
        location = _object(locations[0], f"{path}.result_locations[0]", {"location_id", "kind"})
        if location != {"location_id": "element_average", "kind": "element_average"}:
            _fail(f"{path}.result_locations[0]", "must be element_average")
        shell_locations[element_id] = ["element_average"]
    _unique(shell_ids, "$.topology.shell_elements", "element_id")

    solids = _array(topology["solid_elements"], "$.topology.solid_elements")
    solid_ids: list[str] = []
    solid_locations: dict[str, list[str]] = {}
    solid_sizes = {"tetra4": 4, "wedge6": 6, "hexa8": 8, "tetra10": 10, "wedge15": 15, "hexa20": 20}
    for index, item in enumerate(solids):
        path = f"$.topology.solid_elements[{index}]"
        solid = _object(item, path, {"element_id", "element_type", "node_ids", "coordinate_frame", "result_locations"})
        element_id = _id(solid["element_id"], f"{path}.element_id")
        solid_ids.append(element_id)
        element_type = solid["element_type"]
        if element_type not in solid_sizes:
            _fail(f"{path}.element_type", "is not a supported solid type")
        _node_references(solid["node_ids"], f"{path}.node_ids", node_set, solid_sizes[element_type])
        if solid["coordinate_frame"] != "global":
            _fail(f"{path}.coordinate_frame", "must equal 'global'")
        locations = _array(solid["result_locations"], f"{path}.result_locations")
        if not locations:
            _fail(f"{path}.result_locations", "must not be empty")
        location_ids: list[str] = []
        for location_index, location_value in enumerate(locations):
            location_path = f"{path}.result_locations[{location_index}]"
            location = _object(location_value, location_path, {"location_id", "natural_coordinates"})
            location_id = _id(location["location_id"], f"{location_path}.location_id")
            if location_id != f"GP{location_index}":
                _fail(f"{location_path}.location_id", f"must equal 'GP{location_index}'")
            _natural_coordinates(location["natural_coordinates"], f"{location_path}.natural_coordinates")
            location_ids.append(location_id)
        solid_locations[element_id] = location_ids
    _unique(solid_ids, "$.topology.solid_elements", "element_id")
    return {
        "node_ids": node_ids,
        "member_ids": member_ids,
        "member_stations": member_stations,
        "shell_ids": shell_ids,
        "shell_locations": shell_locations,
        "solid_ids": solid_ids,
        "solid_locations": solid_locations,
    }


def _validate_cases(value: Any, node_ids: list[str]) -> list[dict[str, Any]]:
    cases = _array(value, "$.cases")
    if not cases:
        _fail("$.cases", "must contain at least one case")
    parsed: list[dict[str, Any]] = []
    case_ids: list[str] = []
    node_order = {node_id: index for index, node_id in enumerate(node_ids)}
    for index, item in enumerate(cases):
        path = f"$.cases[{index}]"
        case = _object(item, path, {"case_id", "name", "symbol", "analysis_type", "support_node_ids"})
        case_id = _id(case["case_id"], f"{path}.case_id")
        case_ids.append(case_id)
        _id(case["name"], f"{path}.name")
        _id(case["symbol"], f"{path}.symbol")
        if case["analysis_type"] not in ("static", "material_nonlinear", "modal"):
            _fail(f"{path}.analysis_type", "is not a supported analysis type")
        support_ids = [_id(item, f"{path}.support_node_ids[{item_index}]") for item_index, item in enumerate(_array(case["support_node_ids"], f"{path}.support_node_ids"))]
        _unique(support_ids, f"{path}.support_node_ids", "node ID")
        if any(node_id not in node_order for node_id in support_ids):
            _fail(f"{path}.support_node_ids", "references an unknown topology node")
        if support_ids != sorted(support_ids, key=node_order.__getitem__):
            _fail(f"{path}.support_node_ids", "must follow topology node order")
        parsed.append(cast(dict[str, Any], case))
    _unique(case_ids, "$.cases", "case_id")
    return parsed


def _validate_results(value: Any, cases: list[dict[str, Any]], topology: dict[str, Any]) -> None:
    results = _array(value, "$.results")
    if not results:
        _fail("$.results", "must not be empty")
    case_by_id = {case["case_id"]: case for case in cases}
    case_order = {case["case_id"]: index for index, case in enumerate(cases)}
    grouped: dict[str, list[dict[str, Any]]] = {case["case_id"]: [] for case in cases}
    coordinates: set[tuple[str, str, int]] = set()
    previous_case_index = -1
    for index, result_value in enumerate(results):
        path = f"$.results[{index}]"
        if not isinstance(result_value, Mapping):
            _fail(path, "must be an object")
        case_id = _id(result_value.get("case_id"), f"{path}.case_id")
        if case_id not in case_by_id:
            _fail(f"{path}.case_id", f"references unknown case {case_id!r}")
        current_case_index = case_order[case_id]
        if current_case_index < previous_case_index:
            _fail(path, "results must be in case-major order")
        previous_case_index = current_case_index
        state_value = result_value.get("state")
        if not isinstance(state_value, Mapping):
            _fail(f"{path}.state", "must be an object")
        kind = state_value.get("kind")
        if kind == "static":
            result = _validate_static_result(result_value, path, case_by_id[case_id], topology)
        elif kind == "load_step":
            result = _validate_load_step_result(result_value, path, case_by_id[case_id], topology)
        elif kind == "mode":
            result = _validate_modal_result(result_value, path, case_by_id[case_id], topology)
        else:
            _fail(f"{path}.state.kind", "must be static, load_step, or mode")
        state = cast(Mapping[str, Any], result["state"])
        coordinate = (case_id, cast(str, state["kind"]), cast(int, state["index"]))
        if coordinate in coordinates:
            _fail(path, f"duplicates result coordinate {coordinate!r}")
        coordinates.add(coordinate)
        grouped[case_id].append(result)

    for case in cases:
        case_id = case["case_id"]
        owned = grouped[case_id]
        if not owned:
            _fail("$.results", f"case {case_id!r} has no results")
        analysis_type = case["analysis_type"]
        if analysis_type == "static":
            if len(owned) != 1 or owned[0]["state"] != {"kind": "static", "index": 0}:
                _fail("$.results", f"static case {case_id!r} must have exactly one static result")
        elif analysis_type == "material_nonlinear":
            states = [cast(dict[str, Any], result["state"]) for result in owned]
            if any(state["kind"] != "load_step" for state in states):
                _fail("$.results", f"nonlinear case {case_id!r} may contain only load_step results")
            if [state["index"] for state in states] != list(range(len(states))):
                _fail("$.results", f"nonlinear case {case_id!r} step indices must be consecutive")
            finals = [index for index, state in enumerate(states) if state["is_final"]]
            if finals != [len(states) - 1]:
                _fail("$.results", f"nonlinear case {case_id!r} must mark only its last step final")
        else:
            states = [cast(dict[str, Any], result["state"]) for result in owned]
            if any(state["kind"] != "mode" for state in states):
                _fail("$.results", f"modal case {case_id!r} may contain only mode results")
            if [state["index"] for state in states] != list(range(len(states))):
                _fail("$.results", f"modal case {case_id!r} mode indices must be consecutive")
            eigenvalues = [state["eigenvalue"] for state in states]
            if eigenvalues != sorted(eigenvalues):
                _fail("$.results", f"modal case {case_id!r} eigenvalues must be ascending")
            groups = [state["degeneracy_group"] for state in states]
            if groups[0] != 0 or any(current not in (previous, previous + 1) for previous, current in zip(groups, groups[1:])):
                _fail("$.results", f"modal case {case_id!r} degeneracy groups must be contiguous")


def _validate_static_result(value: Mapping[str, Any], path: str, case: dict[str, Any], topology: dict[str, Any]) -> dict[str, Any]:
    result = _object(value, path, {"case_id", "state", "node_displacements", "support_reactions", "member_section_forces", "shell_results", "solid_results", "diagnostics"})
    state = _object(result["state"], f"{path}.state", {"kind", "index"})
    if state != {"kind": "static", "index": 0}:
        _fail(f"{path}.state", "must equal {'kind': 'static', 'index': 0}")
    if case["analysis_type"] != "static":
        _fail(path, "static result does not match owning case analysis_type")
    _validate_physical_arrays(result, path, case, topology)
    _warnings(result["diagnostics"], f"{path}.diagnostics")
    return cast(dict[str, Any], result)


def _validate_load_step_result(value: Mapping[str, Any], path: str, case: dict[str, Any], topology: dict[str, Any]) -> dict[str, Any]:
    result = _object(value, path, {"case_id", "state", "node_displacements", "support_reactions", "member_section_forces", "shell_results", "solid_results", "diagnostics"})
    state = _object(result["state"], f"{path}.state", {"kind", "index", "load_factor", "is_final"})
    if state["kind"] != "load_step":
        _fail(f"{path}.state.kind", "must equal 'load_step'")
    _integer(state["index"], f"{path}.state.index", minimum=0)
    _finite(state["load_factor"], f"{path}.state.load_factor")
    if type(state["is_final"]) is not bool:
        _fail(f"{path}.state.is_final", "must be a boolean")
    if case["analysis_type"] != "material_nonlinear":
        _fail(path, "load_step result does not match owning case analysis_type")
    _validate_physical_arrays(result, path, case, topology)
    diagnostics = _object(result["diagnostics"], f"{path}.diagnostics", {"warnings", "iterations"})
    _string_array(diagnostics["warnings"], f"{path}.diagnostics.warnings")
    iterations = _array(diagnostics["iterations"], f"{path}.diagnostics.iterations")
    for iteration_index, item in enumerate(iterations):
        iteration_path = f"{path}.diagnostics.iterations[{iteration_index}]"
        iteration = _object(item, iteration_path, {"index", "residual_norm", "correction_norm", "converged"})
        if _integer(iteration["index"], f"{iteration_path}.index", minimum=0) != iteration_index:
            _fail(f"{iteration_path}.index", "must be consecutive from zero")
        _finite(iteration["residual_norm"], f"{iteration_path}.residual_norm", minimum=0)
        _finite(iteration["correction_norm"], f"{iteration_path}.correction_norm", minimum=0)
        if type(iteration["converged"]) is not bool:
            _fail(f"{iteration_path}.converged", "must be a boolean")
    return cast(dict[str, Any], result)


def _validate_modal_result(value: Mapping[str, Any], path: str, case: dict[str, Any], topology: dict[str, Any]) -> dict[str, Any]:
    result = _object(value, path, {"case_id", "state", "node_mode_shapes", "diagnostics"})
    state = _object(result["state"], f"{path}.state", {"kind", "index", "eigenvalue", "frequency", "degeneracy_group"})
    if state["kind"] != "mode":
        _fail(f"{path}.state.kind", "must equal 'mode'")
    _integer(state["index"], f"{path}.state.index", minimum=0)
    _finite(state["eigenvalue"], f"{path}.state.eigenvalue", exclusive_minimum=0)
    _finite(state["frequency"], f"{path}.state.frequency", exclusive_minimum=0)
    _integer(state["degeneracy_group"], f"{path}.state.degeneracy_group", minimum=0)
    if case["analysis_type"] != "modal":
        _fail(path, "mode result does not match owning case analysis_type")
    _node_values(result["node_mode_shapes"], f"{path}.node_mode_shapes", topology["node_ids"], "displacement")
    diagnostics = _object(result["diagnostics"], f"{path}.diagnostics", {"warnings", "normalization", "eigenvalue_tolerance", "degeneracy_relative_tolerance"})
    _string_array(diagnostics["warnings"], f"{path}.diagnostics.warnings")
    if diagnostics["normalization"] != "mass":
        _fail(f"{path}.diagnostics.normalization", "must equal 'mass'")
    _finite(diagnostics["eigenvalue_tolerance"], f"{path}.diagnostics.eigenvalue_tolerance", minimum=0)
    tolerance = _finite(diagnostics["degeneracy_relative_tolerance"], f"{path}.diagnostics.degeneracy_relative_tolerance", minimum=0)
    if tolerance != DEGENERACY_RELATIVE_TOLERANCE:
        _fail(f"{path}.diagnostics.degeneracy_relative_tolerance", f"must equal {DEGENERACY_RELATIVE_TOLERANCE}")
    return cast(dict[str, Any], result)


def _validate_physical_arrays(result: Mapping[str, Any], path: str, case: dict[str, Any], topology: dict[str, Any]) -> None:
    _node_values(result["node_displacements"], f"{path}.node_displacements", topology["node_ids"], "displacement")
    _node_values(result["support_reactions"], f"{path}.support_reactions", case["support_node_ids"], "force")
    members = _array(result["member_section_forces"], f"{path}.member_section_forces")
    member_ids: list[str] = []
    for index, item in enumerate(members):
        member_path = f"{path}.member_section_forces[{index}]"
        member = _object(item, member_path, {"member_id", "segments"})
        member_id = _id(member["member_id"], f"{member_path}.member_id")
        member_ids.append(member_id)
        if member_id not in topology["member_stations"]:
            _fail(f"{member_path}.member_id", "references unknown member")
        stations = topology["member_stations"][member_id]
        segments = _array(member["segments"], f"{member_path}.segments")
        if len(segments) != len(stations) - 1:
            _fail(f"{member_path}.segments", "must cover every consecutive topology station")
        for segment_index, item in enumerate(segments):
            segment_path = f"{member_path}.segments[{segment_index}]"
            segment = _object(item, segment_path, {"segment_id", "station_i", "station_j", "length", "i_end", "j_end"})
            station_i, position_i = stations[segment_index]
            station_j, position_j = stations[segment_index + 1]
            expected_id = f"{station_i}-{station_j}"
            if segment["segment_id"] != expected_id or segment["station_i"] != station_i or segment["station_j"] != station_j:
                _fail(segment_path, f"must identify consecutive segment {expected_id}")
            length = _finite(segment["length"], f"{segment_path}.length", minimum=0)
            if not math.isclose(length, position_j - position_i, rel_tol=_LENGTH_TOLERANCE, abs_tol=_LENGTH_TOLERANCE):
                _fail(f"{segment_path}.length", "does not match topology station distance")
            _force_components(segment["i_end"], f"{segment_path}.i_end")
            _force_components(segment["j_end"], f"{segment_path}.j_end")
    if member_ids != topology["member_ids"]:
        _fail(f"{path}.member_section_forces", "must exactly cover topology members in order")
    _element_results(result["shell_results"], f"{path}.shell_results", topology["shell_ids"], topology["shell_locations"], "shell")
    _element_results(result["solid_results"], f"{path}.solid_results", topology["solid_ids"], topology["solid_locations"], "solid")


def _node_values(value: Any, path: str, expected_ids: list[str], component_kind: str) -> None:
    items = _array(value, path)
    actual_ids: list[str] = []
    for index, item in enumerate(items):
        item_path = f"{path}[{index}]"
        row = _object(item, item_path, {"node_id", "components"})
        actual_ids.append(_id(row["node_id"], f"{item_path}.node_id"))
        if component_kind == "force":
            _force_components(row["components"], f"{item_path}.components")
        else:
            _displacement_components(row["components"], f"{item_path}.components")
    if actual_ids != expected_ids:
        _fail(path, f"must exactly cover node IDs in order: {expected_ids!r}")


def _element_results(value: Any, path: str, expected_ids: list[str], expected_locations: dict[str, list[str]], kind: str) -> None:
    items = _array(value, path)
    actual_ids: list[str] = []
    for index, item in enumerate(items):
        item_path = f"{path}[{index}]"
        row = _object(item, item_path, {"element_id", "locations"})
        element_id = _id(row["element_id"], f"{item_path}.element_id")
        actual_ids.append(element_id)
        if element_id not in expected_locations:
            _fail(f"{item_path}.element_id", "references unknown topology element")
        locations = _array(row["locations"], f"{item_path}.locations")
        actual_location_ids: list[str] = []
        for location_index, location_value in enumerate(locations):
            location_path = f"{item_path}.locations[{location_index}]"
            if kind == "shell":
                location = _object(location_value, location_path, {"location_id", "membrane_force", "bending_moment", "transverse_shear", "top_stress", "bottom_stress"})
                _named_components(location["membrane_force"], f"{location_path}.membrane_force", ("nx", "ny", "nxy"))
                _named_components(location["bending_moment"], f"{location_path}.bending_moment", ("mx", "my", "mxy"))
                _named_components(location["transverse_shear"], f"{location_path}.transverse_shear", ("qx", "qy"))
                _named_components(location["top_stress"], f"{location_path}.top_stress", ("sx", "sy", "txy"))
                _named_components(location["bottom_stress"], f"{location_path}.bottom_stress", ("sx", "sy", "txy"))
            else:
                location = _object(location_value, location_path, {"location_id", "stress", "strain"})
                _named_components(location["stress"], f"{location_path}.stress", ("sx", "sy", "sz", "txy", "tyz", "tzx"))
                _named_components(location["strain"], f"{location_path}.strain", ("ex", "ey", "ez", "gxy", "gyz", "gzx"))
            actual_location_ids.append(_id(location["location_id"], f"{location_path}.location_id"))
        if actual_location_ids != expected_locations[element_id]:
            _fail(f"{item_path}.locations", "must exactly cover topology result locations in order")
    if actual_ids != expected_ids:
        _fail(path, f"must exactly cover topology {kind} elements in order")


def _coordinate_frame(value: Any, path: str) -> None:
    frame = _object(value, path, {"origin", "x_axis", "y_axis", "z_axis"})
    _vector3(frame["origin"], f"{path}.origin")
    axes = [_vector3(frame[name], f"{path}.{name}") for name in ("x_axis", "y_axis", "z_axis")]
    for index, axis in enumerate(axes):
        if not math.isclose(_dot(axis, axis), 1.0, rel_tol=_FRAME_TOLERANCE, abs_tol=_FRAME_TOLERANCE):
            _fail(f"{path}.{('x_axis', 'y_axis', 'z_axis')[index]}", "must be unit length")
    if any(abs(_dot(axes[left], axes[right])) > _FRAME_TOLERANCE for left, right in ((0, 1), (0, 2), (1, 2))):
        _fail(path, "axes must be mutually orthogonal")
    cross = (
        axes[0][1] * axes[1][2] - axes[0][2] * axes[1][1],
        axes[0][2] * axes[1][0] - axes[0][0] * axes[1][2],
        axes[0][0] * axes[1][1] - axes[0][1] * axes[1][0],
    )
    if any(not math.isclose(cross[index], axes[2][index], rel_tol=_FRAME_TOLERANCE, abs_tol=_FRAME_TOLERANCE) for index in range(3)):
        _fail(path, "axes must be right-handed")


def _node_references(value: Any, path: str, node_ids: set[str], count: int) -> None:
    references = [_id(item, f"{path}[{index}]") for index, item in enumerate(_array(value, path))]
    if len(references) != count or len(set(references)) != count:
        _fail(path, f"must contain {count} distinct node IDs")
    if any(reference not in node_ids for reference in references):
        _fail(path, "references an unknown topology node")


def _warnings(value: Any, path: str) -> None:
    diagnostics = _object(value, path, {"warnings"})
    _string_array(diagnostics["warnings"], f"{path}.warnings")


def _string_array(value: Any, path: str) -> None:
    for index, item in enumerate(_array(value, path)):
        if not isinstance(item, str):
            _fail(f"{path}[{index}]", "must be a string")


def _displacement_components(value: Any, path: str) -> None:
    _named_components(value, path, ("dx", "dy", "dz", "rx", "ry", "rz"))


def _force_components(value: Any, path: str) -> None:
    _named_components(value, path, ("fx", "fy", "fz", "mx", "my", "mz"))


def _natural_coordinates(value: Any, path: str) -> None:
    _named_components(value, path, ("xi", "eta", "zeta"))


def _named_components(value: Any, path: str, names: Sequence[str]) -> None:
    components = _object(value, path, set(names))
    for name in names:
        _finite(components[name], f"{path}.{name}")


def _vector3(value: Any, path: str) -> tuple[float, float, float]:
    vector = _object(value, path, {"x", "y", "z"})
    return tuple(_finite(vector[name], f"{path}.{name}") for name in ("x", "y", "z"))  # type: ignore[return-value]


def _dot(left: Sequence[float], right: Sequence[float]) -> float:
    return sum(a * b for a, b in zip(left, right))


def _object(value: Any, path: str, fields: set[str]) -> Mapping[str, Any]:
    if not isinstance(value, Mapping):
        _fail(path, "must be an object")
    actual = set(value)
    if actual != fields:
        missing = sorted(fields - actual)
        extra = sorted(actual - fields)
        detail = []
        if missing:
            detail.append(f"missing {missing!r}")
        if extra:
            detail.append(f"extra {extra!r}")
        _fail(path, "; ".join(detail))
    return value


def _array(value: Any, path: str) -> list[Any]:
    if not isinstance(value, list):
        _fail(path, "must be an array")
    return value


def _id(value: Any, path: str) -> str:
    if not isinstance(value, str) or not value.strip():
        _fail(path, "must be a non-empty string")
    return value


def _integer(value: Any, path: str, *, minimum: int | None = None) -> int:
    if type(value) is not int:
        _fail(path, "must be an integer")
    if minimum is not None and value < minimum:
        _fail(path, f"must be >= {minimum}")
    return value


def _finite(value: Any, path: str, *, minimum: float | None = None, exclusive_minimum: float | None = None) -> float:
    if isinstance(value, bool) or not isinstance(value, (int, float)):
        _fail(path, "must be a number")
    number = float(value)
    if not math.isfinite(number):
        _fail(path, "must be finite")
    if minimum is not None and number < minimum:
        _fail(path, f"must be >= {minimum}")
    if exclusive_minimum is not None and number <= exclusive_minimum:
        _fail(path, f"must be > {exclusive_minimum}")
    return number


def _unique(values: Sequence[str], path: str, label: str) -> None:
    seen: set[str] = set()
    for value in values:
        if value in seen:
            _fail(path, f"duplicate {label} {value!r}")
        seen.add(value)


def _fail(path: str, message: str) -> None:
    raise ResultContractError(f"{path}: {message}")
