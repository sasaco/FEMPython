"""Integration contracts for full and partial member foundations."""

import copy
import numpy as np
import pytest

from fem.file_io import _read_json_model
from fem.legacy_beam import select_case
from fem.model import FemModel
from tests.support.builders.linear_frame import cantilever, run

pytestmark = pytest.mark.integration


def _segments(data, case_id="1"):
    model = FemModel()
    model.read_json_model(_read_json_model(select_case(data, case_id)))
    return sorted(
        (element["member_start"], element["member_end"], element["foundation"])
        for element in model.mesh.elements.values()
    )


def _five_metres():
    data = cantilever()
    data["node"]["2"]["x"] = 5
    return data


def test_ordered_spring_lengths_partition_member_from_i_end():
    data = _five_metres()
    data["fix_member"] = {"1": [
        dict(m=1, length=2, tx=10),
        dict(m=1, length=2, ty=20),
        dict(m=1, length=1, tr=30),
    ]}

    assert _segments(data) == [
        (0, 2, [10, 0, 0, 0]),
        (2, 4, [0, 20, 0, 0]),
        (4, 5, [0, 0, 0, 30]),
    ]


def test_spring_rows_are_applied_in_saved_row_number_order():
    data = _five_metres()
    data["fix_member"] = {"1": [
        dict(row=3, m=1, length=1, tx=30),
        dict(row=1, m=1, length=2, tx=10),
        dict(row=2, m=1, length=2, tx=20),
    ]}
    assert _segments(data) == [
        (0, 2, [10, 0, 0, 0]),
        (2, 4, [20, 0, 0, 0]),
        (4, 5, [30, 0, 0, 0]),
    ]


def test_zero_stiffness_row_advances_cursor_and_short_sum_leaves_unsprung_tail():
    data = _five_metres()
    data["fix_member"] = {"1": [
        dict(m=1, length=1, tx=10),
        dict(m=1, length=2, tx=0),
        dict(m=1, length=1, tx=20),
    ]}

    assert _segments(data) == [
        (0, 1, [10, 0, 0, 0]),
        (1, 3, [0, 0, 0, 0]),
        (3, 4, [20, 0, 0, 0]),
        (4, 5, [0, 0, 0, 0]),
    ]


def test_terminal_blank_covers_remainder_and_all_blank_rows_remain_additive():
    data = _five_metres()
    data["fix_member"] = {"1": [dict(m=1, length=2, tx=10), dict(m=1, ty=20)]}
    assert _segments(data) == [
        (0, 2, [10, 0, 0, 0]),
        (2, 5, [0, 20, 0, 0]),
    ]

    data["fix_member"]["1"] = [dict(m=1, tx=10), dict(m=1, length=None, tx=20)]
    assert _segments(data) == [(0, 5, [30, 0, 0, 0])]


@pytest.mark.parametrize("rows", [
    [dict(m=1, length=0, tx=1)],
    [dict(m=1, length=-1, tx=1)],
    [dict(m=1, length=float("nan"), tx=1)],
    [dict(m=1, length=True, tx=1)],
    [dict(m=1, length=5.01, tx=1)],
    [dict(m=1, length=2, tx=1), dict(m=1, tx=2), dict(m=1, length=1, tx=3)],
    [dict(m=1, length=5, tx=1), dict(m=1, tx=2)],
    [dict(m=1, length=1e-8, tx=1), dict(m=1, length=4.99999999, tx=2)],
])
def test_malformed_spring_intervals_reject_before_mesh_publication(rows):
    data = _five_metres()
    data["fix_member"] = {"1": rows}
    with pytest.raises(ValueError, match="spring|Spring"):
        _segments(data)


def test_float32_length_and_near_coincident_notice_share_one_boundary():
    data = _five_metres()
    data["notice_points"] = [dict(m=1, Points=[2.2])]
    data["fix_member"] = {"1": [
        dict(m=1, length=np.float32(2.2).item(), tx=10),
        dict(m=1, length=np.float32(2.8).item(), tx=20),
    ]}

    segments = _segments(data)
    assert len(segments) == 2
    assert segments[0][1] == pytest.approx(2.2)
    assert segments[1][0] == segments[0][1]
    assert [segment[2][0] for segment in segments] == [10, 20]


def test_spring_boundaries_coexist_with_rigid_load_and_notice_boundaries():
    data = _five_metres()
    data["fix_member"] = {"1": [
        dict(m=1, length=2, tx=10),
        dict(m=1, length=2, tx=20),
        dict(m=1, length=1, tx=30),
    ]}
    data["rigid"] = [dict(m=1, Ilength=1, Jlength=1, e=1)]
    data["notice_points"] = [dict(m=1, Points=[3])]
    data["load"]["1"] = dict(load_member=[
        dict(m=1, mark=2, direction="x", L1=1.5, L2=0.5, P1=1, P2=1)])

    segments = _segments(data)
    assert [segment[0] for segment in segments] + [segments[-1][1]] == [
        0, 1, 1.5, 2, 3, 4, 4.5, 5]
    assert [segment[2][0] for segment in segments] == [10, 10, 10, 20, 20, 30, 30]


def test_partial_axial_foundation_matches_independent_two_member_reference():
    partial = cantilever()
    partial["load"]["1"] = dict(load_node=[dict(n=2, tx=3)])
    partial["fix_member"] = {"1": [
        dict(m=1, length=1, tx=500), dict(m=1, length=1, tx=0)]}
    _, partial_result = run(partial)

    reference = copy.deepcopy(partial)
    reference["node"]["3"] = dict(x=1, y=0, z=0)
    reference["member"] = {
        "1": dict(ni=1, nj=3, e=2, cg=0),
        "2": dict(ni=3, nj=2, e=2, cg=0),
    }
    reference["fix_member"] = {"1": [dict(m=1, tx=500)]}
    _, reference_result = run(reference)

    assert partial_result["node_displacements"][2]["dx"] == pytest.approx(
        reference_result["node_displacements"][2]["dx"], abs=1e-12)


@pytest.mark.material_nonlinear
def test_distributed_axial_foundation_exact_solution():
    d = cantilever()
    d["load"]["1"] = dict(load_node=[dict(n=2, tx=3)])
    d["fix_member"] = {"1": [dict(m=1, tx=500)]}
    _, r = run(d)
    # EA u''=k u; u(0)=0; EA u'(L)=F.
    a = np.sqrt(500 / 2000)
    assert r["node_displacements"][2]["dx"] == pytest.approx(3 * np.tanh(2 * a) / (2000 * a), abs=1e-12)


@pytest.mark.material_nonlinear
def test_foundation_constant_patch_and_linear_nonlinear_route():
    d = cantilever()
    d["fix_node"]["1"].append(dict(n=2, tx=1, ty=1, tz=1, rx=1, ry=1, rz=1))
    d["boundary_conditions"] = {
        "restraints": {str(n): dict(dof=[True] * 6, values=[0, 0.002, 0, 0, 0, 0]) for n in (1, 2)}
    }
    d["fix_member"] = {"1": [dict(m=1, ty=500)]}
    d["load"]["1"] = dict(load_member=[dict(m=1, mark=2, direction="y", P1=1, P2=1)])
    for analysis_type in ("static", "material_nonlinear"):
        d["analysis_type"] = analysis_type
        _, r = run(d)
        steps = r.get("step_results", [r])
        for step in steps:
            for forces in step["element_stresses"].values():
                np.testing.assert_allclose(forces["i_end"], 0, atol=1e-9)
                np.testing.assert_allclose(forces["j_end"], 0, atol=1e-9)
