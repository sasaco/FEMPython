"""Polynomial and independent native-coordinate integration oracles."""
from math import exp, sqrt

import numpy as np
import pytest

from fem.spatial_loads import SpatialLoad, SpatialLoadDefinitions, SpatialLoadPath
from fem.spatial_loads.geometry import signed_area
from fem.spatial_loads.interpolation import shape_values
from fem.spatial_loads.quadrature import integrate_lines, integrate_triangles, line_rule, triangle_rule
from fem.spatial_loads.validation import prepare_geometry
from tests.support.builders.spatial_loads import geometry_model
from tests.support.oracles.spatial_loads import (
    rectangle_q4_loads, trapezoid_diagonal_line, trapezoid_reference, unit_triangle_monomial,
)

pytestmark = pytest.mark.unit


@pytest.mark.parametrize('degree', range(10))
def test_line_polynomial_degree_selects_exact_rule(degree):
    result = integrate_lines(lambda p: p[0]**degree, [[(0, 0), (1, 0)]], degree=degree)
    assert result.value == pytest.approx(1/(degree+1), rel=2e-14)
    assert result.subdivisions == 0


@pytest.mark.parametrize('px,py', [(i, d-i) for d in range(7) for i in range(d+1)])
def test_triangle_monomials_match_factorial_closed_form(px, py):
    result = integrate_triangles(lambda p: p[0]**px * p[1]**py,
                                 [[(0, 0), (1, 0), (0, 1)]], degree=px+py)
    assert result.value == pytest.approx(unit_triangle_monomial(px, py), rel=2e-14)


@pytest.mark.parametrize('reverse', [False, True])
def test_physical_weights_include_area_and_length_once(reverse):
    triangle = [(10, 20), (14, 20), (10, 23)]
    points, weights = triangle_rule(triangle[::-1] if reverse else triangle, order=3)
    assert sum(weights) == pytest.approx(6)
    np.testing.assert_allclose(weights @ points, [68, 126])
    points, weights = line_rule((10, 20), (13, 24))
    assert sum(weights) == pytest.approx(5)
    np.testing.assert_allclose(weights @ points, [57.5, 110])


def test_affine_q4_times_linear_line_load_requires_cubic_precision():
    square = [(0, 0), (1, 0), (1, 1), (0, 1)]
    result = integrate_lines(lambda p: shape_values(square, p, 1e-10) * (1+2*p[0]),
                             [[(0, 0), (1, 1)]], degree=3)
    np.testing.assert_allclose(result.value, sqrt(2) * np.array([.5, 1/3, 5/6, 1/3]), atol=1e-14)


@pytest.mark.parametrize('coefficients', [(2, 0, 0, 0), (1, 2, 3, 0), (1, 2, 3, 2), (0, 0, 0, 0)])
@pytest.mark.parametrize('reverse', [False, True])
def test_affine_q4_area_load_matches_independent_symbolic_integral(coefficients, reverse):
    mesh, panel = geometry_model(shell=True)
    a, b, c, d = coefficients
    paths = [SpatialLoadPath(1, [(0, 0, 0), (2, 0, 0)]),
             SpatialLoadPath(2, [(0, 2, 0), (2, 2, 0)])]
    intensities = [(a, a+2*b), (a+2*c, a+2*b+2*c+4*d)]
    if reverse:
        paths = [SpatialLoadPath(p.id, p.points[::-1]) for p in paths]
        intensities = [pair[::-1] for pair in intensities]
    load = SpatialLoad(9, 7, (1, 2), intensities)
    prepared, = prepare_geometry(SpatialLoadDefinitions((panel,), paths, (load,)), mesh)
    result = np.zeros(4)
    for piece in prepared.area_pieces:
        cell = prepared.panel.cells[piece.cell_index]
        strip = prepared.strip_cells[piece.strip_index]
        result += integrate_triangles(
            lambda p: shape_values(cell.points, p, 1e-10) * strip.intensity(p, 1e-10),
            [piece.triangle], degree=4).value
    np.testing.assert_allclose(result, rectangle_q4_loads(2, 2, coefficients), rtol=2e-14, atol=1e-14)


@pytest.mark.parametrize('q4', [False, True])
@pytest.mark.parametrize('reverse', [False, True])
def test_distorted_q4_and_t3_transverse_field_match_independent_native_integral(q4, reverse):
    # T3 enclosing the strip and Q4 coincident with it exercise different
    # interpolation maps. The load field stays identical in physical space.
    vertices = [(0, 0), (2, 0), (3, 1), (0, 1)] if q4 else [(0, 0), (6, 0), (0, 3)]
    mesh, panel = geometry_model(points=vertices, cells=[tuple(range(1, len(vertices)+1))], shell=q4)
    paths = [SpatialLoadPath(1, [(0, 0, 0), (2, 0, 0)]),
             SpatialLoadPath(2, [(0, 1, 0), (3, 1, 0)])]
    values = [(1, 3), (4, 10)]
    if reverse:
        paths[1] = SpatialLoadPath(2, paths[1].points[::-1])
        values[1] = values[1][::-1]
    load = SpatialLoad(9, 7, (1, 2), values)
    prepared, = prepare_geometry(SpatialLoadDefinitions((panel,), paths, (load,)), mesh)
    result = np.zeros(len(vertices))
    for piece in prepared.area_pieces:
        cell = prepared.panel.cells[piece.cell_index]
        strip = prepared.strip_cells[piece.strip_index]
        integral = integrate_triangles(
            lambda p: shape_values(cell.points, p, 1e-10) * strip.intensity(p, 1e-10),
            [piece.triangle], atol=1e-12, rtol=1e-11, label='load 9 panel 7')
        result += integral.value
    np.testing.assert_allclose(result, trapezoid_reference(q4=q4), rtol=2e-12, atol=2e-12)


def test_nonpolynomial_line_refines_and_matches_exponential_integral():
    result = integrate_lines(lambda p: exp(12*p[0]), [[(0, 0), (1, 0)]], atol=1e-9, rtol=1e-11)
    assert result.value == pytest.approx((exp(12)-1)/12, rel=2e-12)
    assert result.subdivisions > 0
    assert result.estimated_error <= 1e-9 + 1e-11*abs(result.value)


def test_distorted_q4_line_matches_independent_rational_integral():
    vertices = [(0, 0), (2, 0), (3, 1), (0, 1)]
    result = integrate_lines(lambda p: shape_values(vertices, p, 1e-10)*(1+2*p[1]),
                             [[(0, 0), (3, 1)]], atol=1e-12, rtol=1e-11)
    np.testing.assert_allclose(result.value, trapezoid_diagonal_line(), rtol=1e-12, atol=1e-12)


def test_relative_error_scale_survives_positive_negative_cancellation():
    result = integrate_lines(lambda p: 1e6*(p[0]-.5), [[(0, 0), (.5, 0)], [(.5, 0), (1, 0)]],
                             atol=0, rtol=1e-12)
    assert result.value == pytest.approx(0, abs=1e-9)
    assert result.absolute_integral == pytest.approx(250000)
    assert result.estimated_error <= 1e-12*result.absolute_integral


def test_nonpolynomial_triangle_refines_and_matches_closed_form():
    result = integrate_triangles(lambda p: exp(8*p[0]), [[(0, 0), (1, 0), (0, 1)]],
                                 atol=1e-11, rtol=1e-10)
    assert result.value == pytest.approx((exp(8)-9)/64, rel=2e-12)
    assert result.subdivisions > 0


def test_global_error_budget_is_not_granted_to_each_triangle():
    triangles = [[(0, 0), (1, 0), (0, 1)], [(1, 0), (1, 1), (0, 1)]]
    result = integrate_triangles(lambda p: np.array([exp(8*p[0]), 1e-6*exp(8*p[1])]), triangles,
                                 atol=np.array([1e-10, 1e-16]), rtol=0)
    np.testing.assert_allclose(result.value, [(exp(8)-1)/8, 1e-6*(exp(8)-1)/8], rtol=2e-13)
    assert np.all(result.estimated_error <= [1e-10, 1e-16])


@pytest.mark.parametrize('kind', ['line', 'area'])
def test_nonconvergence_rejects_with_load_and_panel_ids(kind):
    integration = integrate_lines if kind == 'line' else integrate_triangles
    domain = [[(0, 0), (1, 0)]] if kind == 'line' else [[(0, 0), (1, 0), (0, 1)]]
    with pytest.raises(ValueError, match='load 9 panel 7.*did not converge'):
        integration(lambda p: exp(12*p[0]), domain, atol=1e-15, rtol=0, max_depth=0, label='load 9 panel 7')


@pytest.mark.parametrize('kwargs', [dict(degree=-1), dict(atol=0, rtol=0), dict(atol=-1),
                                  dict(rtol=np.inf), dict(max_depth=-1)])
def test_invalid_integration_settings_are_rejected(kwargs):
    with pytest.raises(ValueError):
        integrate_lines(lambda p: 1., [[(0, 0), (1, 0)]], **kwargs)


def test_nonfinite_integrand_and_degenerate_domain_are_rejected():
    with pytest.raises(ValueError, match='non-finite'):
        integrate_lines(lambda p: np.nan, [[(0, 0), (1, 0)]])
    with pytest.raises(ValueError, match='degenerate'):
        integrate_triangles(lambda p: 1., [[(0, 0), (1, 0), (2, 0)]])


@pytest.mark.parametrize('degree', [2, None])
def test_pieces_with_component_indices_scatter_into_one_shared_total(degree):
    """A piece may return only the components it touches; totals and budget are shared."""
    from fem.spatial_loads.quadrature import integrate_pieces

    # Two segments: x**2 into components (0, 2) and exp(x) into (1, 2). Closed forms:
    # int_0^1 x**2 = 1/3, int_0^1 exp(x) = e - 1.
    square = lambda points: np.column_stack((points[:, 0]**2, points[:, 0]**2))
    growth = lambda points: np.column_stack((np.exp(points[:, 0]), np.exp(points[:, 0])))
    pieces = [(square, [(0, 0), (1, 0)], (0, 2)), (growth, [(0, 1), (1, 1)], (1, 2))]
    result = integrate_pieces(pieces, kind='line', degree=degree, atol=1e-12, rtol=0, batch=True, size=3)
    if degree is not None:   # a fixed rule is exact only for the polynomial; check that one
        assert result.value[0] == pytest.approx(1/3, abs=1e-14)
        return
    np.testing.assert_allclose(result.value, [1/3, exp(1) - 1, 1/3 + exp(1) - 1], rtol=0, atol=1e-11)
    assert np.all(result.estimated_error <= 1e-12)
    np.testing.assert_allclose(result.absolute_integral, result.value, rtol=1e-12)
