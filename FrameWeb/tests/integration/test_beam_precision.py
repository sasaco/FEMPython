"""integration / beam precision contracts."""

import json
from pathlib import Path

import numpy as np
import pytest

from fem.file_io import _read_json_model
from fem.legacy_beam import select_case
from fem.model import FemModel

pytestmark = pytest.mark.integration


@pytest.mark.parametrize("point", [1e-7, 2 - 1e-7])
def test_extremely_short_segment_preserves_cantilever_solution(point):
    from tests.support.builders.linear_frame import cantilever, run

    data = cantilever()
    data["notice_points"] = [dict(m=1, Points=[point])]
    model, result = run(data)
    assert result["node_displacements"][2]["dy"] == pytest.approx(0.002, abs=1e-12)
    assert result["reaction_forces"][1]["fy"] == pytest.approx(-3, abs=1e-10)
    assert result["reaction_forces"][1]["mz"] == pytest.approx(-6, abs=1e-10)
    for key, element in model.elements.items():
        force = result["constitutive_element_stresses"][key]
        assert force["i_end"][1] == pytest.approx(-3, abs=1e-10)
        assert force["j_end"][1] == pytest.approx(3, abs=1e-10)


def test_precise_frame_keeps_a_weak_support_spring_and_balanced_member_load():
    from tests.support.builders.linear_frame import cantilever, run

    data = cantilever()
    data["element"]["1"]["2"].update(E=2e12)
    data["fix_node"]["1"] = [dict(n=1, tx=0.1, ty=1, tz=1, rx=1, ry=1, rz=1)]
    data["notice_points"] = [dict(m=1, Points=[1.9999999])]
    data["load"]["1"] = dict(load_member=[dict(m=1, mark=2, direction="x", P1=1, P2=1)])
    model, result = run(data)
    assert result["node_displacements"][1]["dx"] == pytest.approx(20, rel=1e-12)
    assert result["reaction_forces"][1]["fx"] == pytest.approx(-2, abs=1e-12)
    assert result["constitutive_element_stresses"][1]["i_end"][0] == pytest.approx(
        -2, abs=1e-10
    )


def test_high_precision_does_not_regularize_an_unrestrained_rigid_rotation():
    from tests.support.builders.linear_frame import cantilever, run

    data = cantilever()
    data["fix_node"]["1"] = [dict(n=1, tx=1, ty=1, tz=1)]
    data["notice_points"] = [dict(m=1, Points=[1.9999999])]
    with pytest.raises(ValueError, match="Singular"):
        run(data)


@pytest.mark.parametrize("length", [2.0, 0.002])
def test_stiff_beam_on_weak_foundation_preserves_rigid_translation(length):
    from tests.support.builders.linear_frame import cantilever, run

    data = cantilever()
    data["node"]["2"]["x"] = length
    data["element"]["1"]["2"].update(E=2e12, Iz=1000.0)
    data["fix_node"]["1"] = [dict(n=1, tx=1, tz=1, rx=1, ry=1)]
    data["fix_member"] = {"1": [dict(m=1, ty=0.1)]}
    data["load"]["1"] = dict(
        load_member=[dict(m=1, mark=2, direction="y", P1=0.3, P2=0.3)]
    )
    _, result = run(data)
    for node in (1, 2):
        assert result["node_displacements"][node]["dy"] == pytest.approx(3.0, abs=1e-10)
        assert result["node_displacements"][node]["rz"] == pytest.approx(0.0, abs=1e-10)
    raw = result["element_stresses"][1]
    np.testing.assert_allclose(raw["i_end"], 0.0, atol=1e-10)
    np.testing.assert_allclose(raw["j_end"], 0.0, atol=1e-10)


def test_precise_snapshots_are_cleared_before_reusing_model():
    from tests.support.builders.linear_frame import cantilever, run

    data = cantilever()
    data["notice_points"] = [dict(m=1, Points=[1.9999999])]
    model, first = run(data)
    assert "precise_end_forces" in first
    model.boundary.add_load(2, [0, 3, 0, 0, 0, 0])
    second = model._run_solver_snapshot()
    assert second["node_displacements"][2]["dy"] == pytest.approx(0.004, abs=1e-12)
    assert second["element_stresses"][1]["i_end"][1] == pytest.approx(-6, abs=1e-10)
    model.read_json_model(_read_json_model(cantilever()))
    ordinary = model._run_solver_snapshot()
    assert "precise_end_forces" not in ordinary
    assert ordinary["node_displacements"][2]["dy"] == pytest.approx(0.002, abs=1e-12)


@pytest.mark.parametrize(
    "length,zone,notice", [(2.2300000000000004, 0.125, 2.105), (2.62, 0.05, 2.57)]
)
def test_rigid_end_uses_the_coalesced_notice_boundary(length, zone, notice):
    from tests.support.builders.linear_frame import cantilever, run

    data = cantilever()
    data["node"]["2"]["x"] = length
    data["rigid"] = [dict(m=1, Ilength=0, Jlength=zone, e=1)]
    data["notice_points"] = [dict(m=1, Points=[notice])]
    model, result = run(data)
    expected = 3 * ((length**3 - zone**3) / (3 * 1000 * 4) + zone**3 / (3 * 1000 * 9))
    assert result["node_displacements"][2]["dy"] == pytest.approx(expected, abs=1e-13)
    final = max(model.mesh.elements.values(), key=lambda e: e["member_end"])
    assert final["material_id"] == 1


def test_precise_tiny_axial_load_survives_thermal_force_postprocessing():
    from tests.support.builders.linear_frame import cantilever, run

    data = cantilever()
    data["element"]["1"]["2"].update(E=2.65e10, A=1, Xp=1e-5)
    data["load"]["1"] = dict(
        load_node=[dict(n=2, tx=1e-8)],
        load_member=[dict(m=1, mark=9, direction="x", P1=15)],
    )
    _, result = run(data)
    assert result["element_stresses"][1]["j_end"][0] == pytest.approx(1e-8, abs=1e-16)
    assert result["reaction_forces"][1]["fx"] == pytest.approx(-1e-8, abs=1e-16)


def test_short_oblique_child_keeps_the_original_member_axis():
    from tests.support.builders.linear_frame import cantilever, run

    data = cantilever()
    data["node"]["2"] = dict(x=4.5, y=0.001, z=0)
    length = np.hypot(4.5, 0.001)
    direction = np.array([-0.001, 4.5, 0]) / length
    data["notice_points"] = [dict(m=1, Points=[4.5])]
    data["load"]["1"] = {
        "load_node": [dict(n=2, **dict(zip(("tx", "ty", "tz"), 3 * direction)))]
    }
    _, result = run(data)
    for force in result["element_stresses"].values():
        assert force["i_end"][1] == pytest.approx(-3, abs=1e-12)
        assert force["j_end"][1] == pytest.approx(3, abs=1e-12)
    expected = 3 * length**3 / (3 * 1000 * 4) * direction
    for key, value in zip(("dx", "dy", "dz"), expected):
        assert result["node_displacements"][2][key] == pytest.approx(value, abs=1e-13)


@pytest.mark.material_nonlinear
@pytest.mark.parametrize(
    "direction", [(1.0, 0.0, 0.0), (0.6, 0.8, 0.0), (0.0, 0.6, 0.8)]
)
def test_public_axial_force_survives_prescribed_rigid_translation(direction):
    from tests.support.builders.linear_frame import cantilever

    direction = np.array(direction)
    data = cantilever()
    for n, x in [("1", 0.0), ("2", 2.0)]:
        data["node"][n] = dict(zip(("x", "y", "z"), direction * x))
    data["load"]["1"]["load_node"] = [
        dict(n=2, **dict(zip(("tx", "ty", "tz"), 1e-8 * direction)))
    ]
    data["element"]["1"]["2"].update(E=2.65e10, A=1.0, Iy=1.0, Iz=1.0, J=1.0, G=1e10)
    m = FemModel()
    m.read_json_model(_read_json_model(data))
    m.add_forced_displacement(1, dx=0.75, dy=0.5, dz=0.25)
    result = m._run_solver_snapshot()
    raw = result["constitutive_element_stresses"][1]
    assert raw["j_end"][0] == pytest.approx(1e-8, rel=1e-8, abs=1e-16)
    assert np.any(result["displacement_correction"])
    for name, value in zip(("fx", "fy", "fz"), -1e-8 * direction):
        assert result["reaction_forces"][1][name] == pytest.approx(value, abs=1e-16)


@pytest.mark.material_nonlinear
@pytest.mark.parametrize("case", ["1", "2"])
def test_short_segment_sample_reaches_constitutive_equilibrium(case):
    data = json.loads(
        Path("tests/data/bar/2D_Sample04.json").read_text(encoding="utf8")
    )
    m = FemModel()
    m.read_json_model(_read_json_model(select_case(data, case)))
    result = m._run_solver_snapshot()
    # No embedded legacy numbers: independently require nodal equilibrium
    # of the unmodified constitutive forces, before free-branch recovery.
    force = np.zeros_like(result["displacement"])
    raw = result["constitutive_element_stresses"]
    for key, element in m.elements.items():
        indices = [
            m.solver._node_dof_start(n, 6) + i
            for n in element.node_ids
            for i in range(6)
        ]
        section = np.r_[raw[key]["i_end"], raw[key]["j_end"]]
        force[indices] += element.get_transformation_matrix(12).T @ section
    prescribed, springs = m.solver._get_boundary_dofs(m.boundary, len(force), 6)
    for i, stiffness in springs.items():
        force[i] += stiffness * result["displacement"][i]
    nodal = np.zeros_like(force)
    for n, load in m.boundary.loads.items():
        start = m.solver._node_dof_start(n, 6)
        nodal[start : start + 6] += load.forces
    free = [i for i in range(len(force)) if i not in prescribed]
    np.testing.assert_allclose(force[free], nodal[free], rtol=0, atol=1e-8)


# FrameWebforJS #714: positions arrive rounded to a millimetre, so a point load
# within half a millimetre of the member length acts at the j-end node.
@pytest.mark.parametrize("start, end", [(1.1, 3.3), (0, 2.1996)])
def test_point_load_typed_at_the_member_length_acts_at_the_j_end(start, end):
    from tests.support.builders.linear_frame import cantilever, run

    data = cantilever()
    data["node"]["1"].update(x=start)
    data["node"]["2"].update(x=end)
    data["load"]["1"] = dict(load_member=[dict(m=1, mark=1, direction="gy", L1=2.2, P1=10)])
    _, result = run(data)
    assert result["reaction_forces"][1]["fy"] == pytest.approx(-10, abs=1e-10)
    assert result["reaction_forces"][1]["mz"] == pytest.approx(-10 * (end - start), abs=1e-10)


def test_point_load_beyond_the_member_length_is_rejected():
    from tests.support.builders.linear_frame import cantilever, run

    data = cantilever()
    data["load"]["1"] = dict(load_member=[dict(m=1, mark=1, direction="gy", L1=2.001, P1=10)])
    with pytest.raises(ValueError, match="Invalid point load position"):
        run(data)
