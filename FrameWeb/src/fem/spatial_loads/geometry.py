"""Planar geometry for spatial loads, independent of elements and solvers.

Lengths are measured in model units. Cross products are compared with a
length tolerance times a representative length, never with a bare length.
Polygon clipping preserves the supplied boundary; no convex hull is inferred.
"""
from dataclasses import dataclass

import numpy as np

from .definitions import GeometryTolerance


def cross(a, b):
    return a[0] * b[1] - a[1] * b[0]


def points_array(points, dimension=2, label='geometry'):
    result = np.array(points, dtype=float, copy=True)
    if (result.ndim != 2 or result.shape[1] != dimension
            or not np.all(np.isfinite(result))):
        raise ValueError(f'{label}: expected finite {dimension}D points')
    return result


def frozen_points(points):
    return tuple(tuple(float(v) for v in p) for p in points)


def signed_area(points):
    p = np.asarray(points, dtype=float)
    if len(p) < 3:
        return 0.
    # Translation avoids cancellation for models far from the global origin.
    q = p - p[0]
    return float(sum(cross(a, b) for a, b in zip(q, np.roll(q, -1, axis=0))) / 2)


def point_on_segment(p, a, b, eps):
    edge = b - a
    length = np.linalg.norm(edge)
    if length <= eps:
        return np.linalg.norm(p - a) <= eps
    return (abs(cross(edge, p - a)) <= eps * length
            and -eps * length <= np.dot(p - a, edge) <= length**2 + eps * length)


def segments_intersect(a, b, c, d, eps):
    if any(point_on_segment(p, u, v, eps)
           for p, u, v in ((a, c, d), (b, c, d), (c, a, b), (d, a, b))):
        return True
    return (cross(b - a, c - a) * cross(b - a, d - a) < 0
            and cross(d - c, a - c) * cross(d - c, b - c) < 0)


def _touching_box_pairs(low, high):
    """Sorted index pairs of 2D boxes that overlap or touch (an x sweep)."""
    order = np.argsort(low[:, 0], kind='stable')
    pairs = []
    for position, i in enumerate(order):
        for j in order[position + 1:]:
            if low[j, 0] > high[i, 0]:
                break
            if low[j, 1] <= high[i, 1] and low[i, 1] <= high[j, 1]:
                pairs.append((int(min(i, j)), int(max(i, j))))
    return sorted(pairs)


def _nearby_edge_pairs(p, eps):
    """Sorted index pairs of ring edges whose bounding boxes lie within 2 eps.

    segments_intersect accepts a point at most eps across and eps beyond an
    edge, i.e. at most eps * sqrt(2) outside its box per axis, so farther
    pairs cannot intersect. Touching and zero-width boxes are kept.
    """
    ends = np.roll(p, -1, axis=0)
    return _touching_box_pairs(np.minimum(p, ends) - eps, np.maximum(p, ends) + eps)


def close_point_pairs(points, eps):
    """Sorted index pairs of points at most eps apart, without comparing every pair."""
    p = np.asarray(points, dtype=float)
    return [(i, j) for i, j in _touching_box_pairs(p - eps, p + eps)
            if np.linalg.norm(p[i] - p[j]) <= eps]


def validate_polygon(points, eps, label='polygon', *, strict=False, convex=True):
    """Validate a simple polygon, optionally convex; return its orientation."""
    p = points_array(points, label=label)
    if len(p) < 3:
        raise ValueError(f'{label}: at least three vertices are required')
    lengths = np.linalg.norm(np.roll(p, -1, axis=0) - p, axis=1)
    if np.any(lengths <= eps):
        raise ValueError(f'{label}: degenerate edge')
    for i, j in _nearby_edge_pairs(p, eps):
        if j == i + 1 or (i == 0 and j == len(p) - 1):
            continue
        if segments_intersect(p[i], p[(i + 1) % len(p)],
                              p[j], p[(j + 1) % len(p)], eps):
            raise ValueError(f'{label}: self-intersecting boundary')
    area = signed_area(p)
    scale = np.linalg.norm(np.ptp(p, axis=0))
    if abs(area) <= eps * scale:
        raise ValueError(f'{label}: degenerate area')
    orientation = 1 if area > 0 else -1
    for i in range(len(p)):
        u, v = p[i] - p[i - 1], p[(i + 1) % len(p)] - p[i]
        turn = orientation * cross(u, v)
        threshold = eps * max(np.linalg.norm(u), np.linalg.norm(v))
        if convex and (turn < -threshold or (strict and turn <= threshold)):
            raise ValueError(f'{label}: non-convex or degenerate polygon')
        if abs(turn) <= threshold and np.dot(u, v) < 0:
            raise ValueError(f'{label}: overlapping adjacent edges')
    return orientation


def bounding_box_pairs(polygons):
    """Return sorted index pairs whose bounding boxes overlap with positive area.

    Boxes that are disjoint or only touch cannot hold a positive-area overlap,
    so a sweep along x avoids clipping every pair of a large partition.
    """
    boxes = [(np.min(p, axis=0), np.max(p, axis=0)) for p in map(np.asarray, polygons)]
    order = sorted(range(len(boxes)), key=lambda k: boxes[k][0][0])
    pairs = []
    for position, i in enumerate(order):
        low, high = boxes[i]
        for j in order[position + 1:]:
            if boxes[j][0][0] >= high[0]:
                break
            if all(min(high[k], boxes[j][1][k]) > max(low[k], boxes[j][0][k]) for k in (0, 1)):
                pairs.append((min(i, j), max(i, j)))
    return sorted(pairs)


def contains_point(polygon, point, eps):
    p = np.asarray(polygon)
    sign = 1 if signed_area(p) > 0 else -1
    return all(sign * cross(b - a, point - a) >= -eps * np.linalg.norm(b - a)
               for a, b in zip(p, np.roll(p, -1, axis=0)))


def contains_points(polygon, points, eps):
    """contains_point for many points of one convex polygon: one boolean per point."""
    p, q = np.asarray(polygon, dtype=float), np.asarray(points, dtype=float)
    sign = 1 if signed_area(p) > 0 else -1
    edge = np.roll(p, -1, axis=0) - p
    offset = q[:, None, :] - p[None, :, :]
    turn = edge[None, :, 0] * offset[..., 1] - edge[None, :, 1] * offset[..., 0]
    return np.all(sign * turn >= -eps * np.linalg.norm(edge, axis=1), axis=1)


def contains_simple(polygon, point, eps):
    """Even/odd containment for a validated simple ring, including its edges."""
    p, point = np.asarray(polygon), np.asarray(point)
    inside = False
    for a, b in zip(p, np.roll(p, -1, axis=0)):
        if point_on_segment(point, a, b, eps):
            return True
        if (a[1] > point[1]) != (b[1] > point[1]):
            x = a[0] + (point[1] - a[1]) * (b[0] - a[0]) / (b[1] - a[1])
            if point[0] < x:
                inside = not inside
    return inside


def _clean_polygon(points, eps):
    out = []
    for p in points:
        if not out or np.linalg.norm(p - out[-1]) > eps:
            out.append(p)
    if len(out) > 1 and np.linalg.norm(out[0] - out[-1]) <= eps:
        out.pop()
    return np.array(out, dtype=float).reshape((-1, 2))


def clip_polygon(subject, clip, eps):
    """Intersect two validated convex polygons using exact half planes."""
    result = np.asarray(subject, dtype=float)
    boundary = np.asarray(clip, dtype=float)
    sign = 1 if signed_area(boundary) > 0 else -1
    for a, b in zip(boundary, np.roll(boundary, -1, axis=0)):
        if len(result) < 3:
            return np.empty((0, 2))
        output = []
        for start, end in zip(result, np.roll(result, -1, axis=0)):
            f0 = sign * cross(b - a, start - a)
            f1 = sign * cross(b - a, end - a)
            if (f0 >= 0) != (f1 >= 0):
                output.append(start + f0 / (f0 - f1) * (end - start))
            if f1 >= 0:
                output.append(end)
        result = _clean_polygon(output, eps)
    return result


def triangulate_convex(polygon, eps):
    """Fan triangulation for integration only, not a new interpolation mesh."""
    p = np.asarray(polygon)
    if len(p) < 3:
        return ()
    scale = np.linalg.norm(np.ptp(p, axis=0))
    return tuple(frozen_points(t) for i in range(1, len(p) - 1)
                 if abs(signed_area(t := p[[0, i, i + 1]])) > eps * scale)


def _drop_collinear(p, eps):
    """Ring indices without vertices lying on the segment between their neighbours.

    Ear clipping drops such vertices without a triangle anyway; doing it first in
    one stack pass keeps a long outline with many collinear vertices linear.
    """
    kept = []
    for index in range(len(p)):
        kept.append(index)
        while len(kept) >= 3 and point_on_segment(p[kept[-2]], p[kept[-3]], p[kept[-1]], eps):
            del kept[-2]
    while len(kept) > 3:
        if point_on_segment(p[kept[-1]], p[kept[-2]], p[kept[0]], eps):
            del kept[-1]
        elif point_on_segment(p[kept[0]], p[kept[-1]], p[kept[1]], eps):
            del kept[0]
        else:
            break
    return kept


def triangulate_simple(polygon, eps, label='polygon'):
    """Ear clipping of a validated simple ring, solely for domain integration."""
    p = np.asarray(polygon)
    sign = validate_polygon(p, eps, label, convex=False)
    remaining = _drop_collinear(p, eps)
    if len(remaining) < 3:
        raise ValueError(f'{label}: polygon triangulation failed')
    triangles = []
    while len(remaining) > 3:
        for i, node in enumerate(remaining):
            a, c = remaining[i - 1], remaining[(i + 1) % len(remaining)]
            if point_on_segment(p[node], p[a], p[c], eps):
                remaining.pop(i)
                break
            triangle = p[[a, node, c]]
            scale = max(np.linalg.norm(p[node] - p[a]), np.linalg.norm(p[c] - p[node]))
            if sign * cross(p[node] - p[a], p[c] - p[node]) <= eps * scale:
                continue
            if any(contains_point(triangle, p[j], eps) for j in remaining if j not in (a, node, c)):
                continue
            triangles.append(frozen_points(triangle))
            remaining.pop(i)
            break
        else:
            raise ValueError(f'{label}: polygon triangulation failed')
    triangles.append(frozen_points(p[remaining]))
    return tuple(triangles)


@dataclass(frozen=True)
class PlanarFrame:
    origin: tuple
    axis_u: tuple
    axis_v: tuple
    normal: tuple
    scale: float
    eps: float

    @classmethod
    def from_points(cls, points, tolerance=GeometryTolerance(), label='panel'):
        p = points_array(points, 3, label)
        if len(p) < 3:
            raise ValueError(f'{label}: at least three points are required')
        q = p - p[0]
        scale = float(np.linalg.norm(np.ptp(q, axis=0)))
        if not np.isfinite(scale):
            raise ValueError(f'{label}: length scale overflows')
        eps = tolerance.length(scale)
        if scale <= eps:
            raise ValueError(f'{label}: degenerate length scale')
        # Long baselines avoid choosing nearly coincident first vertices.
        u = q[np.argmax(np.linalg.norm(q, axis=1))]
        u = u / np.linalg.norm(u)
        normals = np.cross(u, q)
        normal = normals[np.argmax(np.linalg.norm(normals, axis=1))]
        if np.linalg.norm(normal) <= eps:
            raise ValueError(f'{label}: collinear points')
        normal = normal / np.linalg.norm(normal)
        if np.max(np.abs(q @ normal)) > eps:
            raise ValueError(f'{label}: non-planar points')
        return cls(tuple(p[0]), tuple(u), tuple(np.cross(normal, u)),
                   tuple(normal), scale, eps)

    def project(self, points, label='path'):
        p = points_array(points, 3, label) - self.origin
        if np.any(np.abs(p @ self.normal) > self.eps):
            raise ValueError(f'{label}: points leave the panel plane')
        return np.column_stack((p @ self.axis_u, p @ self.axis_v))

    def lift(self, points):
        p = np.asarray(points)
        return np.asarray(self.origin) + p[..., 0, None] * self.axis_u + p[..., 1, None] * self.axis_v


@dataclass(frozen=True)
class PanelCell:
    key: int
    node_ids: tuple
    points: tuple
    element_id: int | None = None


@dataclass(frozen=True)
class PanelGeometry:
    panel_id: int
    frame: PlanarFrame
    cells: tuple[PanelCell, ...]
    boundary: tuple
    holes: tuple = ()
    domain_cells: tuple[PanelCell, ...] = ()


def validate_partition(cells, eps, label, *, holes=()):
    """Require oriented, conforming connected cells and explicitly declared holes.

    Cells stay convex T3/Q4; their union and its boundary rings may be nonconvex.
    Hole rings use node IDs, cyclic order and the opposite winding to the outer
    ring. Connectivity is authoritative: metadata never cuts a hole in a cell.
    """
    edges, points = {}, {}
    sign = None
    for index, cell in enumerate(cells):
        orientation = validate_polygon(cell.points, eps, f'{label} cell {cell.key}', strict=True)
        if sign is not None and sign != orientation:
            raise ValueError(f'{label}: inconsistent cell orientation')
        sign = orientation
        for node, point in zip(cell.node_ids, cell.points):
            points[node] = point
        for a, b in zip(cell.node_ids, cell.node_ids[1:] + cell.node_ids[:1]):
            entries = edges.setdefault(tuple(sorted((a, b))), [])
            entries.append((index, a, b))
            if len(entries) > 2:
                raise ValueError(f'{label}: non-manifold edge')
    adjacency = [set() for _ in cells]
    boundary_edges = []
    for entries in edges.values():
        if len(entries) == 1:
            boundary_edges.append(entries[0][1:])
        else:
            (i, a, b), (j, c, d) = entries
            if (a, b) != (d, c):
                raise ValueError(f'{label}: overlapping or inverted cells')
            adjacency[i].add(j)
            adjacency[j].add(i)
    visited, pending = set(), [0]
    while pending:
        i = pending.pop()
        if i not in visited:
            visited.add(i)
            pending.extend(adjacency[i] - visited)
    if len(visited) != len(cells):
        raise ValueError(f'{label}: disconnected or non-conforming cells')
    successors = {}
    for a, b in boundary_edges:
        if a in successors:
            raise ValueError(f'{label}: branching boundary')
        successors[a] = b
    if not successors or len(set(successors.values())) != len(successors):
        raise ValueError(f'{label}: invalid boundary')
    rings, unused = [], set(successors)
    while unused:
        start = min(unused)
        ring, current = [], start
        while current not in ring:
            if current not in unused:
                raise ValueError(f'{label}: open or branching boundary')
            ring.append(current)
            unused.remove(current)
            current = successors[current]
        if current != start:
            raise ValueError(f'{label}: invalid boundary ring')
        rings.append(tuple(ring))
    outer, inner = [], []
    for ring in rings:
        polygon = tuple(points[n] for n in ring)
        orientation = validate_polygon(polygon, eps, f'{label} boundary', convex=False)
        (outer if orientation == sign else inner).append(ring)
    if len(outer) != 1:
        raise ValueError(f'{label}: multiple outer boundaries')
    def cyclic(ring):
        i = ring.index(min(ring))
        return ring[i:] + ring[:i]
    if holes is not None and sorted(cyclic(ring) for ring in inner) != sorted(cyclic(ring) for ring in holes):
        raise ValueError(f'{label}: declared holes must match topology and opposite winding')
    boundary = tuple(points[n] for n in outer[0])
    polygons = [tuple(points[n] for n in ring) for ring in (outer + inner)]
    for hole in polygons[1:]:
        if not contains_simple(boundary, hole[0], eps):
            raise ValueError(f'{label}: hole outside outer boundary')
    for i, first in enumerate(polygons):
        for second in polygons[i + 1:]:
            if any(segments_intersect(np.array(a), np.array(b), np.array(c), np.array(d), eps)
                   for a, b in zip(first, first[1:] + first[:1])
                   for c, d in zip(second, second[1:] + second[:1])):
                raise ValueError(f'{label}: touching or intersecting hole boundaries')
            if i > 0 and (contains_simple(first, second[0], eps) or contains_simple(second, first[0], eps)):
                raise ValueError(f'{label}: nested holes')
    scale = np.linalg.norm(np.ptp(np.array(boundary), axis=0))
    for i, j in bounding_box_pairs([cell.points for cell in cells]):
        overlap = clip_polygon(cells[i].points, cells[j].points, eps)
        if abs(signed_area(overlap)) > eps * scale:
            raise ValueError(f'{label}: overlapping cell interiors')
    if abs(sum(abs(signed_area(c.points)) for c in cells)
           - abs(signed_area(boundary)) + sum(abs(signed_area(p)) for p in polygons[1:])) > eps * scale:
        raise ValueError(f'{label}: cells do not cover the boundary')
    return boundary


@dataclass(frozen=True)
class LinePiece:
    cell_index: int
    start: tuple
    end: tuple
    s0: float
    s1: float


def _segment_interval(polygon, start, end, eps):
    p = np.asarray(polygon)
    sign = 1 if signed_area(p) > 0 else -1
    low, high = 0., 1.
    for a, b in zip(p, np.roll(p, -1, axis=0)):
        edge = b - a
        f0, f1 = sign * cross(edge, start - a), sign * cross(edge, end - a)
        threshold = eps * np.linalg.norm(edge)
        if abs(f1 - f0) <= threshold:
            if min(f0, f1) < -threshold:
                return None
            continue
        t = -f0 / (f1 - f0)
        if f1 > f0:
            low = max(low, t)
        else:
            high = min(high, t)
    return (low, high) if high > low else None


def split_line(panel, points, parameters, label='line'):
    """Split at every cell edge; a boundary interval belongs to the lowest key."""
    p = np.asarray(points)
    eps = panel.frame.eps
    pieces = []
    for i, (a, b) in enumerate(zip(p, p[1:])):
        length = np.linalg.norm(b - a)
        intervals = [(j, interval) for j, cell in enumerate(panel.cells)
                     if (interval := _segment_interval(cell.points, a, b, eps)) is not None]
        # Without a loading mesh the structural cells are the loading domain.
        domain_intervals = intervals if not panel.domain_cells else [
            (j, interval) for j, cell in enumerate(panel.domain_cells)
            if (interval := _segment_interval(cell.points, a, b, eps)) is not None]
        cuts = sorted({0., 1., *(t for _, pair in intervals for t in pair),
                       *(t for _, pair in domain_intervals for t in pair)})
        unique = [cuts[0]]
        for t in cuts[1:]:
            if t - unique[-1] > eps / length:
                unique.append(t)
        unique[-1] = 1.
        for t0, t1 in zip(unique, unique[1:]):
            mid = (t0 + t1) / 2
            owners = [j for j, (lo, hi) in intervals if lo - eps / length <= mid <= hi + eps / length]
            domain_owners = [j for j, (lo, hi) in domain_intervals
                             if lo - eps / length <= mid <= hi + eps / length]
            if not owners or not domain_owners:
                raise ValueError(f'{label}: line leaves panel {panel.panel_id}; extrapolation is forbidden')
            owner = min(owners, key=lambda j: panel.cells[j].key)
            ds = parameters[i + 1] - parameters[i]
            pieces.append(LinePiece(owner, tuple(a + t0 * (b - a)), tuple(a + t1 * (b - a)),
                                    parameters[i] + t0 * ds, parameters[i] + t1 * ds))
    return tuple(pieces)
