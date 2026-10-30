"""Exact geometry invariants, independent of structural analysis."""
import numpy as np
import pytest

from fem.spatial_loads import GeometryTolerance, SpatialLoad, SpatialLoadDefinitions, SpatialLoadPath
from fem.spatial_loads.geometry import (
    PlanarFrame, bounding_box_pairs, clip_polygon, signed_area, triangulate_convex,
)
from fem.spatial_loads.interpolation import natural_coordinates, q4_shape, shape_values
from fem.spatial_loads.validation import prepare_geometry, prepare_panel
from tests.support.builders.spatial_loads import geometry_model

pytestmark = pytest.mark.unit


@pytest.mark.parametrize('reverse', [False, True])
@pytest.mark.parametrize('scale', [1e-4, 1, 1e5])
def test_plane_projection_is_stable_translated_scaled_and_rotated(reverse, scale):
    origin = np.array([100, -200, 300]) * scale
    u = np.array([1, 2, 3]) / np.sqrt(14)
    v = np.cross(u, [0, 0, 1]); v /= np.linalg.norm(v)
    points = origin + scale * np.array([0*u, 2*u, 2*u+3*v, 3*v])
    frame = PlanarFrame.from_points(points[::-1] if reverse else points)
    local = frame.project(points)
    np.testing.assert_allclose(frame.lift(local), points, rtol=0, atol=scale*1e-12)
    assert abs(signed_area(local)) == pytest.approx(6 * scale**2)


@pytest.mark.parametrize('reverse', [False, True])
@pytest.mark.parametrize('shell', [False, True])
def test_partition_area_and_boundary_do_not_depend_on_global_orientation(reverse, shell):
    mesh, definition = geometry_model(shell=shell, reverse=reverse)
    panel = prepare_panel(definition, mesh)
    assert abs(signed_area(panel.boundary)) == pytest.approx(4)
    assert sum(abs(signed_area(c.points)) for c in panel.cells) == pytest.approx(4)
    assert [c.element_id is not None for c in panel.cells] == [shell] * len(panel.cells)


@pytest.mark.parametrize('reverse', [False, True])
def test_clip_has_exact_area_and_first_moments_without_convex_hull_expansion(reverse):
    subject = [(1, -1), (3, -1), (3, 1), (1, 1)]
    square = [(0, 0), (2, 0), (2, 2), (0, 2)]
    polygon = clip_polygon(subject[::-1] if reverse else subject, square, 1e-10)
    assert abs(signed_area(polygon)) == pytest.approx(1)
    triangles = triangulate_convex(polygon, 1e-10)
    area = sum(abs(signed_area(t)) for t in triangles)
    centroid = sum(abs(signed_area(t)) * np.mean(t, axis=0) for t in triangles) / area
    np.testing.assert_allclose(centroid, [1.5, .5])
    assert len(clip_polygon(subject, [(5, 5), (6, 5), (6, 6), (5, 6)], 1e-10)) == 0


@pytest.mark.parametrize('point', [(0, 0), (.3, .2), (1, 0), (.5, .5)])
@pytest.mark.parametrize('reverse', [False, True])
def test_triangle_basis_reproduces_affine_coordinates_and_boundary(point, reverse):
    vertices = np.array([(0, 0), (1, 0), (0, 1)])
    if reverse:
        vertices = vertices[::-1]
    weights = shape_values(vertices, point, 1e-10)
    assert sum(weights) == pytest.approx(1)
    np.testing.assert_allclose(weights @ vertices, point, atol=1e-14)


@pytest.mark.parametrize('natural', [(-1, -1), (1, 1), (-.7, .4), (0, 0), (1, -.3)])
@pytest.mark.parametrize('scale,offset', [(1, 0), (1e-5, 0), (1e4, 1e8)])
def test_distorted_q4_inverse_reproduces_coordinates(natural, scale, offset):
    vertices = np.array([(0, 0), (2, 0), (3, 1), (.1, 2)]) * scale + offset
    point = q4_shape(natural) @ vertices
    result = natural_coordinates(vertices, point, scale * 1e-9)
    np.testing.assert_allclose(result, natural, atol=2e-11, rtol=0)
    np.testing.assert_allclose(shape_values(vertices, point, scale*1e-9) @ vertices, point, atol=scale*1e-11)


@pytest.mark.parametrize('vertices', [[(0, 0), (1, 0), (0, 1)], [(0, 0), (1, 0), (1, 1), (0, 1)]])
def test_interpolation_rejects_extrapolation(vertices):
    with pytest.raises(ValueError, match='outside'):
        shape_values(vertices, (2, .5), 1e-10, 'panel 7 cell 1')


@pytest.mark.parametrize('points,expected', [
    ([(0, 0), (2, 2)], [(0, 2**.5*2)]),
    ([(0, 1), (2, 1)], [(0, 1), (1, 2)]),
    ([(0, 0), (2, 0)], [(0, 2)]),
    ([(0, .5), (1, .5), (1, 1.5)], None),
])
@pytest.mark.parametrize('reverse', [False, True])
def test_line_intervals_cover_path_once_including_shared_edges(points, expected, reverse):
    mesh, panel = geometry_model()
    path = SpatialLoadPath(3, [(*p, 0) for p in (points[::-1] if reverse else points)])
    load = SpatialLoad(9, 7, (3,), ((2, 4),))
    prepared, = prepare_geometry(SpatialLoadDefinitions((panel,), (path,), (load,)), mesh)
    pieces = prepared.line_pieces
    assert sum(np.linalg.norm(np.array(p.end) - p.start) for p in pieces) == pytest.approx(path.cumulative_lengths[-1])
    assert pieces[0].s0 == 0 and pieces[-1].s1 == 1
    for a, b in zip(pieces, pieces[1:]):
        assert a.s1 == pytest.approx(b.s0)
    if expected is not None:
        assert len(pieces) == len(expected)
    if points == [(0, 0), (2, 2)]:
        assert {p.cell_index for p in pieces} == {0}


def test_tiny_geometry_uses_configured_length_tolerance():
    mesh, definition = geometry_model(points=[(0, 0), (1e-8, 0), (0, 1e-8)], cells=[(1, 2, 3)],
                                      tolerance=GeometryTolerance(1e-14, 1e-9))
    assert abs(signed_area(prepare_panel(definition, mesh).boundary)) == pytest.approx(5e-17)


def _strip_grid(columns, rows=2):
    """Unit-square cells, each split into two triangles: a long deck-like panel."""
    points = [(x, y) for y in range(rows + 1) for x in range(columns + 1)]
    node = lambda x, y: y * (columns + 1) + x + 1
    cells = [cell for y in range(rows) for x in range(columns)
             for cell in ((node(x, y), node(x + 1, y), node(x + 1, y + 1)),
                          (node(x, y), node(x + 1, y + 1), node(x, y + 1)))]
    return geometry_model(points=points, cells=cells)


def _full_area_load(columns, rows=2):
    paths = (SpatialLoadPath(1, ((0, 0, 0), (columns, 0, 0))),
             SpatialLoadPath(2, ((0, rows, 0), (columns, rows, 0))))
    return paths, SpatialLoad(9, 7, (1, 2), ((-1, -1), (-1, -1)))


def test_area_clipping_without_loading_mesh_scales_with_cell_count(monkeypatch):
    """Structural cells do not overlap, so each one is clipped only against itself."""
    import fem.spatial_loads.validation as validation

    calls = 0
    original = validation.clip_polygon

    def counting_clip(*args):
        nonlocal calls
        calls += 1
        return original(*args)

    monkeypatch.setattr(validation, 'clip_polygon', counting_clip)
    columns = 20
    mesh, panel = _strip_grid(columns)
    paths, load = _full_area_load(columns)
    prepared, = prepare_geometry(SpatialLoadDefinitions((panel,), paths, (load,)), mesh)
    cells = len(prepared.panel.cells)
    assert cells == 4 * columns
    assert calls < 5 * cells


def test_area_pieces_without_loading_mesh_cover_each_cell_once():
    columns = 6
    mesh, panel = _strip_grid(columns)
    paths, load = _full_area_load(columns)
    prepared, = prepare_geometry(SpatialLoadDefinitions((panel,), paths, (load,)), mesh)
    area = {}
    for piece in prepared.area_pieces:
        area[piece.cell_index] = area.get(piece.cell_index, 0) + abs(signed_area(piece.triangle))
    assert sorted(area) == list(range(len(prepared.panel.cells)))
    assert all(value == pytest.approx(.5) for value in area.values())


def test_partition_overlap_check_scales_with_cell_count(monkeypatch):
    """Only cells with overlapping bounding boxes need an exact overlap clip."""
    import fem.spatial_loads.geometry as geometry

    calls = 0
    original = geometry.clip_polygon

    def counting_clip(*args):
        nonlocal calls
        calls += 1
        return original(*args)

    monkeypatch.setattr(geometry, 'clip_polygon', counting_clip)
    mesh, panel = _strip_grid(20)
    cells = len(prepare_panel(panel, mesh).cells)
    assert calls < 5 * cells


def test_bounding_box_pairs_match_brute_force_including_touching_boxes():
    rng = np.random.default_rng(7)
    polygons = [tuple(map(tuple, rng.integers(0, 6, size=(3, 2)).astype(float))) for _ in range(60)]
    boxes = [(np.min(p, axis=0), np.max(p, axis=0)) for p in map(np.array, polygons)]
    expected = [(i, j) for i in range(len(boxes)) for j in range(i + 1, len(boxes))
                if all(min(boxes[i][1][k], boxes[j][1][k]) > max(boxes[i][0][k], boxes[j][0][k]) for k in (0, 1))]
    assert bounding_box_pairs(polygons) == expected
