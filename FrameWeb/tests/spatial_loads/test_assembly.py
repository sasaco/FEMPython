"""Conservative DOF assembly checked against independent physical integrals."""
from copy import deepcopy
from dataclasses import FrozenInstanceError, replace
from math import sqrt

import numpy as np
import pytest

from fem.spatial_loads import (
    AssemblyTolerance, SpatialLoad, SpatialLoadDefinitions, SpatialLoadPanel,
    SpatialLoadPath, compile_spatial_loads,
)
from fem.spatial_loads import assembler
from tests.support.builders.spatial_loads import geometry_model, square_definitions
from tests.support.oracles.spatial_loads import (
    rectangle_distribution, rectangle_q4_loads, rectangle_t3_loads,
    trapezoid_diagonal_line, trapezoid_reference,
)

pytestmark = pytest.mark.unit


def vertical(result):
    matrix = np.asarray(result.dof_loads).reshape(-1, result.stride)
    np.testing.assert_array_equal(matrix[:, [0, 1, *range(3, result.stride)]], 0.)
    return matrix[:, 2]


@pytest.mark.parametrize('shell', [False, True])
@pytest.mark.parametrize('coefficients', [(2, 0, 0, 0), (1, 2, 3, 0), (1, 2, 3, 2), (-2, 2, -3, 2), (0, 0, 0, 0)])
@pytest.mark.parametrize('reverse', [(), (0,), (1,), (0, 1)])
def test_area_nodal_distribution_and_audits_match_closed_integrals(shell, coefficients, reverse):
    mesh, panel = geometry_model(shell=shell)
    definitions = square_definitions(panel, coefficients=coefficients, reverse=reverse)
    result = compile_spatial_loads(definitions, mesh)
    oracle = rectangle_q4_loads if shell else rectangle_t3_loads
    np.testing.assert_allclose(vertical(result), oracle(2, 2, coefficients), atol=3e-14, rtol=2e-14)
    force, moment = rectangle_distribution(2, 2, coefficients)
    np.testing.assert_allclose(result.resultant, force, atol=6e-14)
    np.testing.assert_allclose(result.moment, moment, atol=6e-14)
    assert result.loads[0].clipped_area == pytest.approx(4.)
    assert result.loads[0].integrated_length == 0.
    assert result.loads[0].subdivisions == 0
    assert all((cell.element_id is not None) == shell for cell in result.cells)


@pytest.mark.parametrize('shell', [False, True])
@pytest.mark.parametrize('reverse', [False, True])
def test_boundary_line_linear_distribution_has_exact_endpoint_shares(shell, reverse):
    mesh, panel = geometry_model(shell=shell)
    result = compile_spatial_loads(square_definitions(panel, area=False, coefficients=(10, 5, 0, 0),
                                                      reverse=(0,) if reverse else ()), mesh)
    np.testing.assert_allclose(vertical(result), [40/3, 50/3, 0, 0], atol=1e-14)
    np.testing.assert_allclose(result.resultant, [0, 0, 30], atol=1e-14)
    np.testing.assert_allclose(result.moment, [0, -100/3, 0], atol=1e-14)
    assert result.loads[0].integrated_length == pytest.approx(2)


@pytest.mark.parametrize('shell', [False, True])
def test_shared_diagonal_is_loaded_once_without_replacing_q4_basis(shell):
    mesh, panel = geometry_model(shell=shell)
    definitions = SpatialLoadDefinitions((panel,), (SpatialLoadPath(1, [(0, 0, 0), (2, 2, 0)]),),
                                         (SpatialLoad(9, 7, (1,), ((2, 2),)),))
    result = compile_spatial_loads(definitions, mesh)
    expected = np.array([4/3, 2/3, 4/3, 2/3]) if shell else np.array([2, 0, 2, 0])
    np.testing.assert_allclose(vertical(result), sqrt(2)*expected, atol=2e-14)
    assert result.loads[0].integrated_length == pytest.approx(2*sqrt(2))


@pytest.mark.parametrize('shell', [False, True])
@pytest.mark.parametrize('area', [False, True])
@pytest.mark.parametrize('factor', [0, -2.5, 1e-12, 1e9])
def test_zero_proportionality_and_addition_include_audits(shell, area, factor):
    mesh, panel = geometry_model(shell=shell)
    definitions = square_definitions(panel, area=area, coefficients=(-2, 2, 1, 0))
    original = compile_spatial_loads(definitions, mesh)
    load = definitions.loads[0]
    scaled = replace(load, id=10, end_intensities=tuple(tuple(factor*p for p in pair) for pair in load.end_intensities))
    single = compile_spatial_loads(replace(definitions, loads=(scaled,)), mesh)
    combined = compile_spatial_loads(replace(definitions, loads=(load, scaled)), mesh)
    roundoff = 32*np.finfo(float).eps*single.force_scale
    np.testing.assert_allclose(single.dof_loads, factor*np.asarray(original.dof_loads), atol=roundoff, rtol=3e-14)
    np.testing.assert_allclose(combined.dof_loads, np.asarray(original.dof_loads)+single.dof_loads, atol=1e-13)
    assert combined.force_scale == pytest.approx(original.force_scale+single.force_scale)
    if original.force_scale:
        assert original.force_scale > 0
    assert combined.force_error <= 1e-10 + 1e-9*combined.force_scale
    assert combined.moment_error <= 1e-10 + 1e-9*combined.moment_scale


@pytest.mark.parametrize('q4', [False, True])
def test_nonpolynomial_area_assembly_matches_independent_native_integration(q4):
    vertices = [(0, 0), (2, 0), (3, 1), (0, 1)] if q4 else [(0, 0), (6, 0), (0, 3)]
    mesh, panel = geometry_model(vertices, [tuple(range(1, len(vertices)+1))], shell=q4)
    paths = (SpatialLoadPath(1, [(0, 0, 0), (2, 0, 0)]), SpatialLoadPath(2, [(0, 1, 0), (3, 1, 0)]))
    definitions = SpatialLoadDefinitions((panel,), paths, (SpatialLoad(9, 7, (1, 2), ((1, 3), (4, 10))),))
    result = compile_spatial_loads(definitions, mesh)
    np.testing.assert_allclose(vertical(result), trapezoid_reference(q4=q4), atol=3e-12, rtol=3e-12)
    assert result.loads[0].clipped_area == pytest.approx(2.5)
    assert result.loads[0].evaluations >= 160


def test_nonpolynomial_line_and_nonconvergence_rejection():
    mesh, panel = geometry_model([(0, 0), (2, 0), (3, 1), (0, 1)], shell=True)
    definitions = SpatialLoadDefinitions((panel,), (SpatialLoadPath(1, [(0, 0, 0), (3, 1, 0)]),),
                                         (SpatialLoad(9, 7, (1,), ((1, 3),)),))
    result = compile_spatial_loads(definitions, mesh)
    np.testing.assert_allclose(vertical(result), trapezoid_diagonal_line(), rtol=2e-12, atol=1e-12)
    with pytest.raises(ValueError, match='load 9 panel 7.*did not converge'):
        compile_spatial_loads(definitions, mesh, tolerance=AssemblyTolerance(1e-15, 1e-15, 0, 0))


@pytest.mark.parametrize('scale,offset', [(1e-4, (0, 0, 0)), (1e4, (0, 0, 0)), (1, (1e9, -2e9, 3))])
@pytest.mark.parametrize('area', [False, True])
def test_translation_and_length_scaling_preserve_dimensions(scale, offset, area):
    mesh, panel = geometry_model()
    original = compile_spatial_loads(square_definitions(panel, area=area), mesh)
    for node, point in mesh.nodes.items():
        mesh.nodes[node] = tuple(offset[i]+scale*point[i] for i in range(3))
    result = compile_spatial_loads(square_definitions(panel, area=area, offset=offset, scale=scale), mesh)
    power = scale**(2 if area else 1)
    np.testing.assert_allclose(vertical(result), power*vertical(original), rtol=3e-14)
    expected_moment = power*scale*np.asarray(original.moment) + np.cross(offset, power*np.asarray(original.resultant))
    np.testing.assert_allclose(result.moment, expected_moment, rtol=3e-14, atol=1e-20)


def test_compact_nonconsecutive_node_ids_and_three_dof_layout():
    mesh, panel = geometry_model()
    mapping = {1: 91, 2: 4, 3: 200, 4: 33}
    mesh.nodes = {mapping[n]: p for n, p in mesh.nodes.items()}
    for element in mesh.elements.values():
        element['nodes'] = [mapping[n] for n in element['nodes']]
        element['type'] = 'tetra'
    panel = SpatialLoadPanel(7, tuple(mesh.nodes), triangles=[tuple(mapping[n] for n in c) for c in panel.triangles])
    result = compile_spatial_loads(square_definitions(panel), mesh)
    assert result.node_ids == (4, 33, 91, 200)
    assert result.stride == 3
    np.testing.assert_allclose(vertical(result), [2/3, 2/3, 4/3, 4/3])


@pytest.mark.parametrize('shell', [False, True])
def test_compilation_is_pure_repeatable_and_snapshot_is_detached(shell):
    mesh, panel = geometry_model(shell=shell)
    definitions = square_definitions(panel)
    before = deepcopy((mesh.nodes, mesh.elements, definitions))
    result = compile_spatial_loads(definitions, mesh)
    assert compile_spatial_loads(definitions, mesh) == result
    for node in mesh.nodes:
        np.testing.assert_array_equal(mesh.nodes[node], before[0][node])
    assert mesh.elements == before[1]
    assert definitions == before[2]
    with pytest.raises(FrozenInstanceError):
        result.dof_loads = ()
    snapshot = result.to_dict()
    snapshot['node_loads'][1][2] = 99
    if shell:
        snapshot['shell_element_loads'][1][2] = 99
    assert compile_spatial_loads(definitions, mesh).to_dict() == result.to_dict()


@pytest.mark.parametrize('defect,reason', [('force', 'resultant'), ('moment', 'moment')])
def test_independent_audits_reject_corrupted_basis(monkeypatch, defect, reason):
    mesh, panel = geometry_model(shell=True)
    basis = assembler.shape_values

    def broken(*args, **kwargs):
        # Nodes are the last axis: one point gives (nodes,), many points (n, nodes).
        result = basis(*args, **kwargs)
        result[..., 0] += .01
        if defect == 'moment':
            result[..., 1] -= .01
        return result

    monkeypatch.setattr(assembler, 'shape_values', broken)
    with pytest.raises(ValueError, match=f'load 9 panel 7.*{reason} conservation failed'):
        compile_spatial_loads(square_definitions(panel), mesh)


@pytest.mark.parametrize('kwargs', [dict(force_absolute=-1), dict(moment_absolute=np.inf), dict(relative=True),
                                  dict(relative=0, force_absolute=0), dict(max_depth=-1), dict(max_depth=True)])
def test_assembly_tolerances_reject_invalid_settings(kwargs):
    with pytest.raises(ValueError, match='assembly'):
        AssemblyTolerance(**kwargs)


@pytest.mark.parametrize('reverse', [False, True])
def test_t3_shell_cells_keep_structural_node_order_and_direct_contributions(reverse):
    mesh, panel = geometry_model(cells=[(1, 2, 3), (1, 3, 4)], shell=True, reverse=reverse)
    result = compile_spatial_loads(square_definitions(panel), mesh)
    np.testing.assert_allclose(vertical(result), [4/3, 2/3, 4/3, 2/3], atol=1e-14)
    for cell in result.cells:
        assert cell.node_ids == tuple(mesh.elements[cell.element_id]['nodes'])
        values = result.shell_load_vectors()[cell.element_id].reshape(-1, 6)
        np.testing.assert_allclose(values[:, 2], 2/3, atol=1e-14)


def test_two_panels_on_same_shell_are_additive_without_losing_load_provenance():
    mesh, panel = geometry_model(shell=True)
    definitions = square_definitions(panel)
    second = replace(panel, id=8)
    second_load = replace(definitions.loads[0], id=10, panel_id=8)
    result = compile_spatial_loads(replace(definitions, panels=(panel, second),
                                          loads=(*definitions.loads, second_load)), mesh)
    np.testing.assert_allclose(vertical(result), [2, 2, 2, 2])
    np.testing.assert_allclose(result.shell_load_vectors()[1].reshape(-1, 6)[:, 2], 2)
    assert {(c.load_id, c.panel_id) for c in result.cells} == {(9, 7), (10, 8)}


def test_multiple_line_segments_use_cumulative_length_intensities():
    mesh, panel = geometry_model(shell=True)
    # Length 1 along +X followed by length 2 along +Y. q grows from 0 to 6.
    definitions = SpatialLoadDefinitions((panel,), (SpatialLoadPath(1, [(0, 0, 0), (1, 0, 0), (1, 2, 0)]),),
                                         (SpatialLoad(9, 7, (1,), ((0, 6),)),))
    result = compile_spatial_loads(definitions, mesh)
    np.testing.assert_allclose(result.resultant, [0, 0, 9], atol=1e-14)
    np.testing.assert_allclose(result.moment, [28/3, -26/3, 0], atol=1e-14)
    assert result.loads[0].integrated_length == pytest.approx(3)


def test_merged_strip_knots_and_multiple_q4_cells_match_unpartitioned_field():
    mesh, panel = geometry_model(points=[(0, 0), (1, 0), (2, 0), (0, 2), (1, 2), (2, 2)],
                                 cells=[(1, 2, 5, 4), (2, 3, 6, 5)], shell=True)
    paths = (SpatialLoadPath(1, [(0, 0, 0), (.5, 0, 0), (2, 0, 0)]),
             SpatialLoadPath(2, [(0, 2, 0), (1.5, 2, 0), (2, 2, 0)]))
    load = SpatialLoad(9, 7, (1, 2), ((1, 5), (7, 19)))
    result = compile_spatial_loads(SpatialLoadDefinitions((panel,), paths, (load,)), mesh)
    expected_left = rectangle_q4_loads(1, 2, (1, 2, 3, 2))
    expected_right = rectangle_q4_loads(1, 2, (3, 2, 5, 2))
    expected = [expected_left[0], expected_left[1]+expected_right[0], expected_right[1],
                expected_left[3], expected_left[2]+expected_right[3], expected_right[2]]
    np.testing.assert_allclose(vertical(result), expected, atol=3e-14)
    assert result.loads[0].clipped_area == pytest.approx(4)


def test_piecewise_adaptive_quadrature_shares_the_absolute_error_budget():
    from fem.spatial_loads.quadrature import integrate_pieces
    from math import exp
    # Different callbacks/owners with cancellation exercise the same path used
    # by compilation. Sum of per-piece error must satisfy one absolute bound.
    pieces = [(lambda p: exp(8*p[0]), [(0, 0), (1, 0)]),
              (lambda p: -exp(8*p[0])/2, [(0, 1), (1, 1)])]
    result = integrate_pieces(pieces, kind='line', atol=1e-10, rtol=0)
    assert result.value == pytest.approx((exp(8)-1)/16, rel=2e-13)
    assert result.estimated_error <= 1e-10
    assert result.subdivisions > 0


def test_cancelled_total_force_keeps_nonzero_scale_and_pure_couple():
    mesh, panel = geometry_model(shell=True)
    definitions = square_definitions(panel, coefficients=(-1, 1, 0, 0))
    result = compile_spatial_loads(definitions, mesh)
    np.testing.assert_allclose(result.resultant, 0, atol=2e-15)
    np.testing.assert_allclose(result.moment, [0, -4/3, 0], atol=2e-15)
    assert result.force_scale > 1.5
    assert result.moment_scale > 0
