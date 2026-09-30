"""Canonical AnalysisResultSet topology and physical-result projection."""

from __future__ import annotations

import copy

import numpy as np
import pytest

from fem.analysis_result_sets import build_analysis_result_set
from fem.file_io import _read_json_model, result_to_jsonable
from fem.model import FemModel
from fem.result_contracts import ResultContractError, validate_analysis_result_set
from fem.result_projection import project_analysis_results
from fem.result_topology import build_result_topology
from tests.support.builders.input_routes import axial_json, json_model
from tests.support.builders.quadratic_solid import TET, make_model


pytestmark = pytest.mark.integration


def _validate_single_case(topology, result, support_node_ids, analysis_type):
    root = {
        "kind": "analysis_result_set",
        "schema_version": "1.0",
        "units": {
            "system": "consistent_user_defined",
            "length": "unspecified",
            "force": "unspecified",
            "mass": "unspecified",
            "time": "unspecified",
        },
        "coordinate_system": {
            "name": "global_cartesian",
            "handedness": "right",
            "axes": ["x", "y", "z"],
        },
        "cases": [
            {
                "case_id": "1",
                "name": "1",
                "symbol": "1",
                "analysis_type": analysis_type,
                "support_node_ids": support_node_ids,
            }
        ],
        "topology": topology,
        "results": [result],
    }
    validate_analysis_result_set(root)


def _modern_source(model: FemModel) -> dict:
    return {
        "nodes": {str(node): list(map(float, coordinates)) for node, coordinates in model.mesh.nodes.items()},
        "elements": {
            str(element): {
                key: copy.deepcopy(value)
                for key, value in properties.items()
                if key not in {"original_id", "member_start", "member_end", "member_nodes"}
            }
            for element, properties in model.mesh.elements.items()
        },
    }


def _legacy_beam() -> dict:
    material = {"E": 10_000, "G": 4_000, "A": 1, "Iy": 1, "Iz": 1, "J": 1}
    supports = [
        {"n": 10, "tx": 1, "ty": 1, "tz": 1, "rx": 1, "ry": 1, "rz": 1},
        {"n": 30, "tx": 0, "ty": 1, "tz": 1, "rx": 1, "ry": 1, "rz": 1},
    ]
    return {
        "node": {
            "30": {"x": 2, "y": 0, "z": 0},
            "10": {"x": 0, "y": 0, "z": 0},
        },
        "member": {"7": {"ni": 10, "nj": 30, "e": 1, "cg": 0}},
        "notice_points": [{"m": 7, "Points": [0.5]}],
        "element": {"1": {"1": material}},
        "fix_node": {"1": supports},
        "load": {
            "first": {
                "element": 1,
                "fix_node": 1,
                "load_node": [{"n": 30, "tx": 4}],
            },
            "station-from-another-case": {
                "element": 1,
                "fix_node": 1,
                "load_member": [
                    {"m": 7, "mark": 1, "direction": "x", "L1": 1.5, "P1": 0}
                ],
            },
        },
    }


def test_legacy_topology_uses_original_ids_and_request_wide_stations():
    from fem.legacy_beam import select_case

    source = _legacy_beam()
    selected = select_case(source, "first")
    model = FemModel()
    model.read_json_model(_read_json_model(selected))
    result = result_to_jsonable(model._run_solver_snapshot("static"))

    topology = build_result_topology(source, model)

    assert [node["node_id"] for node in topology["nodes"]] == [
        "30",
        "10",
        "generated:7:S1",
        "generated:7:S2",
    ]
    assert topology["nodes"][2]["source_node_id"] is None
    assert topology["members"][0]["member_id"] == "7"
    assert topology["members"][0]["node_i"] == "10"
    assert topology["members"][0]["node_j"] == "30"
    assert topology["members"][0]["stations"] == [
        {"station_id": "S0", "position": 0.0},
        {"station_id": "S1", "position": 0.5},
        {"station_id": "S2", "position": 1.5},
        {"station_id": "S3", "position": 2.0},
    ]

    projected = project_analysis_results(
        model, result, topology, "first", ["30", "10"]
    )
    assert len(projected) == 1
    snapshot = projected[0]
    assert snapshot["state"] == {"kind": "static", "index": 0}
    assert [row["node_id"] for row in snapshot["node_displacements"]] == [
        "30",
        "10",
        "generated:7:S1",
        "generated:7:S2",
    ]
    assert [row["node_id"] for row in snapshot["support_reactions"]] == ["30", "10"]
    assert [segment["length"] for segment in snapshot["member_section_forces"][0]["segments"]] == pytest.approx(
        [0.5, 1.0, 0.5]
    )
    for segment in snapshot["member_section_forces"][0]["segments"]:
        assert segment["i_end"]["fx"] == pytest.approx(4)
        assert segment["j_end"]["fx"] == pytest.approx(4)


def test_spring_case_boundaries_share_canonical_topology_but_keep_case_stiffness():
    from fem.legacy_beam import select_case

    source = _legacy_beam()
    source["fix_member"] = {
        "1": [
            {"m": 7, "length": 1, "tx": 10},
            {"m": 7, "length": 1, "tx": 0},
        ],
        "2": [
            {"m": 7, "length": 0.75, "ty": 0},
            {"m": 7, "length": 1, "ty": 20},
            {"m": 7, "length": 0.25, "ty": 0},
        ],
    }
    source["load"]["first"]["fix_member"] = 1
    source["load"]["station-from-another-case"]["fix_member"] = 2

    segments_by_case = []
    for case_id in source["load"]:
        model = FemModel()
        model.read_json_model(_read_json_model(select_case(source, case_id)))
        segments_by_case.append(sorted(
            (element["member_start"], element["member_end"], element["foundation"])
            for element in model.mesh.elements.values()
        ))

    expected_stations = [0, 0.5, 0.75, 1, 1.5, 1.75, 2]
    assert [[segment[0] for segment in segments] + [2] for segments in segments_by_case] == [
        expected_stations, expected_stations]
    assert [segment[2][0] for segment in segments_by_case[0]] == [10, 10, 10, 0, 0, 0]
    assert [segment[2][1] for segment in segments_by_case[1]] == [0, 0, 20, 20, 20, 0]

    result_set = build_analysis_result_set(source)
    assert [case["case_id"] for case in result_set["cases"]] == [
        "first", "station-from-another-case"]
    assert [station["position"] for station in result_set["topology"]["members"][0]["stations"]] == [
        *expected_stations]
    validate_analysis_result_set(result_set)


def test_public_model_run_returns_only_the_canonical_result_set():
    model = json_model(axial_json(force=12))

    result_set = model.run("material_nonlinear")

    validate_analysis_result_set(result_set)
    assert set(result_set) == {
        "kind",
        "schema_version",
        "units",
        "coordinate_system",
        "cases",
        "topology",
        "results",
    }
    assert result_set["kind"] == "analysis_result_set"
    assert [row["state"]["kind"] for row in result_set["results"]] == ["load_step"] * 4
    assert all("step_results" not in row for row in result_set["results"])
    assert model.results is result_set
    assert model.get_results() is result_set
    assert "step_results" in model._solver_snapshot
    assert model.get_node_displacement(30)["dx"] == pytest.approx(0.004)
    member = model.get_element_stress(7)
    assert member["member_id"] == "7"
    assert member["segments"][0]["i_end"]["fx"] == pytest.approx(12)


def test_public_run_reconstructs_generated_stations_for_loaded_programmatic_model():
    from fem.legacy_beam import select_case

    source = _legacy_beam()
    model = FemModel()
    model.read_json_model(_read_json_model(select_case(source, "first")))

    result_set = model.run("static")

    assert [node["node_id"] for node in result_set["topology"]["nodes"]] == [
        "30",
        "10",
        "generated:7:S1",
        "generated:7:S2",
    ]
    assert [member["member_id"] for member in result_set["topology"]["members"]] == [
        "7"
    ]
    validate_analysis_result_set(result_set)


def test_public_supports_exclude_restraint_rows_with_no_fixed_dof():
    model = json_model(axial_json(force=12))
    model.add_restraint(30, False, False, False, False, False, False)

    result_set = model.run("material_nonlinear")

    assert result_set["cases"][0]["support_node_ids"] == ["10"]
    assert all(
        [row["node_id"] for row in result["support_reactions"]] == ["10"]
        for result in result_set["results"]
    )


def test_legacy_result_case_excludes_all_zero_support_rows():
    source = _legacy_beam()
    source["fix_node"]["1"] = [
        {"n": 10, "tx": 1, "ty": 1, "tz": 1, "rx": 1, "ry": 1, "rz": 1},
        {"n": 30, "tx": 0, "ty": 0, "tz": 0, "rx": 0, "ry": 0, "rz": 0},
    ]

    result_set = build_analysis_result_set(source)

    assert [case["support_node_ids"] for case in result_set["cases"]] == [
        ["10"],
        ["10"],
    ]
    assert all(
        [row["node_id"] for row in result["support_reactions"]] == ["10"]
        for result in result_set["results"]
    )


def test_public_model_run_classifies_invalid_internal_analysis_type_as_contract_error(
    monkeypatch,
):
    model = json_model(axial_json(force=12))
    monkeypatch.setattr(
        model,
        "_run_solver_snapshot",
        lambda analysis_type=None: {"analysis_type": "unexpected"},
    )

    with pytest.raises(ResultContractError, match="Internal solver snapshot"):
        model.run()


def test_every_nonlinear_step_is_a_complete_independent_snapshot():
    source = axial_json(force=12)
    model = json_model(source)
    solver_result = result_to_jsonable(model._run_solver_snapshot("material_nonlinear"))
    topology = build_result_topology(source, model)

    snapshots = project_analysis_results(model, solver_result, topology, "1", ["10"])

    assert len(snapshots) == 4
    assert [row["state"]["index"] for row in snapshots] == [0, 1, 2, 3]
    assert [row["state"]["load_factor"] for row in snapshots] == pytest.approx(
        [0.25, 0.5, 0.75, 1.0]
    )
    assert [row["state"]["is_final"] for row in snapshots] == [False, False, False, True]
    assert all("step_results" not in row for row in snapshots)
    assert all("node_displacements" in row and "member_section_forces" in row for row in snapshots)
    assert snapshots[-1]["node_displacements"][1]["components"]["dx"] == pytest.approx(0.004)
    assert snapshots[-1]["support_reactions"][0]["components"]["fx"] == pytest.approx(-12)
    assert snapshots[-1]["member_section_forces"][0]["segments"][0]["i_end"]["fx"] == pytest.approx(12)

    diagnostic_iterations = [
        iteration
        for snapshot in snapshots
        for iteration in snapshot["diagnostics"]["iterations"]
    ]
    assert len(diagnostic_iterations) == len(model.solver.convergence_history)
    assert all(set(row) == {"index", "residual_norm", "correction_norm", "converged"} for row in diagnostic_iterations)
    assert all(snapshot["diagnostics"]["iterations"][-1]["converged"] for snapshot in snapshots)


def test_shell_projection_uses_serialized_local_frame_and_weighted_average():
    model = FemModel()
    for node, coordinates in {
        10: [0, 0, 0],
        20: [2, 0, 0],
        30: [2, 3, 0],
        40: [0, 3, 0],
    }.items():
        model.add_node(node, *coordinates)
        model.add_restraint(node, True, True, True, True, True, True)
        model.add_forced_displacement(node, dx=0.002 * coordinates[0])
    model.add_material(1, "test", 1200, 0.2)
    model.add_element(17, "shell", [10, 20, 30, 40], 1, thickness=0.2)
    source = _modern_source(model)
    solver_result = result_to_jsonable(model._run_solver_snapshot("static"))
    topology = build_result_topology(source, model)

    projected = project_analysis_results(
        model, solver_result, topology, "1", ["10", "20", "30", "40"]
    )[0]

    assert topology["shell_elements"][0]["element_type"] == "quadrilateral4"
    assert topology["shell_elements"][0]["result_locations"] == [
        {"location_id": "element_average", "kind": "element_average"}
    ]
    frame = topology["shell_elements"][0]["local_frame"]
    assert frame["x_axis"] == pytest.approx({"x": 1, "y": 0, "z": 0})
    assert frame["y_axis"] == pytest.approx({"x": 0, "y": 1, "z": 0})
    shell = projected["shell_results"][0]["locations"][0]
    raw = solver_result["shell_results"]["17"]["resultants"]
    assert shell["membrane_force"] == pytest.approx(
        dict(zip(("nx", "ny", "nxy"), raw["membrane"], strict=True))
    )
    assert shell["top_stress"]["sx"] == pytest.approx(
        shell["membrane_force"]["nx"] / 0.2
        + 6 * shell["bending_moment"]["mx"] / 0.2**2
    )
    _validate_single_case(
        topology, projected, ["10", "20", "30", "40"], "static"
    )


def test_solid_projection_covers_topology_gauss_points_in_formulation_order():
    model, element = make_model("tetra2", TET)
    for node, coordinates in model.mesh.nodes.items():
        model.add_restraint(node, True, True, True)
        model.add_forced_displacement(
            node,
            dx=0.001 * coordinates[0],
            dy=0.002 * coordinates[1],
            dz=-0.001 * coordinates[2],
        )
    source = _modern_source(model)
    solver_result = result_to_jsonable(model._run_solver_snapshot("static"))
    topology = build_result_topology(source, model)
    support_ids = [str(node) for node in model.mesh.nodes]

    projected = project_analysis_results(
        model, solver_result, topology, "1", support_ids
    )[0]

    solid_topology = topology["solid_elements"][0]
    solid_result = projected["solid_results"][0]
    assert solid_topology["element_type"] == "tetra10"
    assert [row["location_id"] for row in solid_topology["result_locations"]] == [
        "GP0",
        "GP1",
        "GP2",
        "GP3",
    ]
    assert [row["location_id"] for row in solid_result["locations"]] == [
        "GP0",
        "GP1",
        "GP2",
        "GP3",
    ]
    for location in solid_result["locations"]:
        assert location["stress"] == pytest.approx(
            {"sx": 1.6, "sy": 2.4, "sz": 0, "txy": 0, "tyz": 0, "tzx": 0},
            abs=1e-11,
        )
    assert len(element.get_gauss_points()[0]) == len(solid_result["locations"])
    _validate_single_case(topology, projected, support_ids, "static")


def test_modal_projection_emits_only_canonical_mass_normalized_mode_shape():
    model = FemModel()
    coordinates = [[0, 0, 0], [2, 0, 0], [0, 3, 0]]
    for index, xyz in enumerate(coordinates):
        node = 10 + 3 * index
        model.add_node(node, *xyz)
    model.add_material(1, "reference", E=1000.0, nu=0.25, density=2.0)
    model.add_element(8, "TriElement1", list(model.mesh.nodes), 1, thickness=0.2, formulation="dkt")
    for index, node in enumerate(model.mesh.nodes):
        model.add_restraint(node, dx=index != 1, dy=True, dz=True, rx=True, ry=True, rz=True)
    model.analysis_params["n_modes"] = 1
    source = _modern_source(model)
    solver_result = result_to_jsonable(model._run_solver_snapshot("modal"))
    topology = build_result_topology(source, model)

    projected = project_analysis_results(model, solver_result, topology, "1", ["10", "13", "16"])

    assert len(projected) == 1
    mode = projected[0]
    assert set(mode) == {"case_id", "state", "node_mode_shapes", "diagnostics"}
    assert mode["state"]["kind"] == "mode"
    assert mode["state"]["eigenvalue"] == pytest.approx(800)
    assert mode["state"]["frequency"] == pytest.approx(np.sqrt(800) / (2 * np.pi))
    assert mode["state"]["degeneracy_group"] == 0
    assert [row["node_id"] for row in mode["node_mode_shapes"]] == ["10", "13", "16"]
    assert mode["diagnostics"]["normalization"] == "mass"
    assert mode["diagnostics"]["degeneracy_relative_tolerance"] == 1e-8
    _validate_single_case(topology, mode, ["10", "13", "16"], "modal")


def test_result_set_builder_validates_repeated_degenerate_modal_projection():
    from fem.analysis_result_sets import build_analysis_result_set

    source = {
        "nodes": {"1": [0, 0, 0], "2": [2, 0, 0]},
        "elements": {
            "7": {
                "type": "bar",
                "nodes": [1, 2],
                "material_id": 1,
                "section_id": 1,
                "shear_correction": False,
            }
        },
        "materials": {
            "1": {"name": "test", "E": 2000, "nu": 0.25, "density": 2}
        },
        "bar_parameters": {"1": {"area": 3, "Iy": 1, "Iz": 1, "J": 1}},
        "boundary_conditions": {
            "restraints": {
                "1": {"dof": [True] * 6},
                "2": {"dof": [True, False, False, True, True, True]},
            }
        },
        "analysis_type": "modal",
        "analysis_params": {"n_modes": 2},
    }

    first = build_analysis_result_set(copy.deepcopy(source))
    second = build_analysis_result_set(copy.deepcopy(source))

    assert first == second
    assert [result["state"]["degeneracy_group"] for result in first["results"]] == [0, 0]
    first_mode = first["results"][0]["node_mode_shapes"][1]["components"]
    second_mode = first["results"][1]["node_mode_shapes"][1]["components"]
    assert first_mode["dy"] > 0
    assert first_mode["dz"] == pytest.approx(0, abs=1e-12)
    assert second_mode["dy"] == pytest.approx(0, abs=1e-12)
    assert second_mode["dz"] > 0
