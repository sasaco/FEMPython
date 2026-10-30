"""T3/Q4 interpolation and normalized-arc-length ruled load strips."""
from dataclasses import dataclass

import numpy as np

from ..shape_functions import q4_derivatives, q4_shape, t3_shape

from .geometry import (
    PanelCell, contains_point, contains_points, contains_simple, cross, frozen_points, points_array,
    segments_intersect, validate_partition, validate_polygon,
)

# Geometry tolerance defines acceptance, not the desired interpolation
# accuracy: force conservation requires solving to near machine precision.
_RESIDUAL_TOL = 64 * np.finfo(float).eps


def _many_points(points, label):
    if points.ndim != 2 or points.shape[1] != 2 or not np.all(np.isfinite(points)):
        raise ValueError(f'{label}: expected finite points')
    return points


def _is_parallelogram(p):
    """Roundoff-only test (as the assembler's affine test): the bilinear term vanishes."""
    q = p - p[0]
    return np.linalg.norm(q[2] - q[1] - q[3]) <= 16 * np.finfo(float).eps * np.linalg.norm(np.ptp(p, axis=0))


def _natural_coordinates_of_points(p, points, eps, label, validated):
    """Many points of one Q4: a parallelogram maps affinely and is inverted directly."""
    if len(p) != 4:
        raise ValueError(f'{label}: expected four vertices and finite points')
    _many_points(points, label)
    if not validated:
        validate_polygon(p, eps, label, strict=True)
    if not np.all(contains_points(p, points, eps)):
        raise ValueError(f'{label}: point outside cell; extrapolation is forbidden')
    if not _is_parallelogram(p):
        return np.array([natural_coordinates(p, point, eps, label, validated=True)
                         for point in points]).reshape((-1, 2))
    scale = np.linalg.norm(np.ptp(p, axis=0))
    q, target = (p - p[0]) / scale, (points - p[0]) / scale
    # The bilinear map of a parallelogram is x = centre + J (r, s), J constant.
    jacobian = q.T @ q4_derivatives((0., 0.))
    centre = q4_shape((0., 0.)) @ q
    try:
        natural = np.linalg.solve(jacobian, (target - centre).T).T
    except np.linalg.LinAlgError as error:
        raise ValueError(f'{label}: singular inverse mapping') from error
    inverse_scale = np.linalg.norm(np.linalg.inv(jacobian), ord=np.inf)
    natural_tol = (eps / scale + _RESIDUAL_TOL) * inverse_scale
    if np.any(np.abs(natural) > 1 + natural_tol):
        raise ValueError(f'{label}: natural coordinates outside cell')
    return natural


def natural_coordinates(vertices, point, eps, label='Q4', *, validated=False):
    """Invert a convex Q4, checking residual and containment without clamping.

    Newton equations use translated, scaled coordinates. Backtracking retains
    descent for strongly distorted convex cells; failure is explicit.
    ``validated`` skips the strict convexity check for a cell that already
    passed it (a prepared panel cell or strip); containment is still checked.
    An (n, 2) array of points returns (n, 2) natural coordinates.
    """
    p = points_array(vertices, label=label)
    point = np.asarray(point, dtype=float)
    if point.ndim == 2:
        return _natural_coordinates_of_points(p, point, eps, label, validated)
    if len(p) != 4 or point.shape != (2,) or not np.all(np.isfinite(point)):
        raise ValueError(f'{label}: expected four vertices and a finite point')
    if not validated:
        validate_polygon(p, eps, label, strict=True)
    if not contains_point(p, point, eps):
        raise ValueError(f'{label}: point outside cell; extrapolation is forbidden')
    scale = np.linalg.norm(np.ptp(p, axis=0))
    q, target = (p - p[0]) / scale, (point - p[0]) / scale
    residual_tol = _RESIDUAL_TOL
    natural = np.zeros(2)
    for _ in range(40):
        residual = q4_shape(natural) @ q - target
        if np.linalg.norm(residual) <= residual_tol:
            jacobian = q.T @ q4_derivatives(natural)
            # Convert physical tolerance to a dimensionless local tolerance.
            inverse_scale = np.linalg.norm(np.linalg.inv(jacobian), ord=np.inf)
            natural_tol = (eps / scale + residual_tol) * inverse_scale
            if np.any(np.abs(natural) > 1 + natural_tol):
                raise ValueError(f'{label}: natural coordinates outside cell')
            return natural
        jacobian = q.T @ q4_derivatives(natural)
        try:
            step = np.linalg.solve(jacobian, residual)
        except np.linalg.LinAlgError as error:
            raise ValueError(f'{label}: singular inverse mapping') from error
        factor = 1.
        while factor >= 1 / 1024:
            trial = natural - factor * step
            if np.linalg.norm(q4_shape(trial) @ q - target) < np.linalg.norm(residual):
                natural = trial
                break
            factor /= 2
        else:
            break
    raise ValueError(f'{label}: inverse mapping did not converge')


def _shape_values_of_points(p, points, eps, label, validated):
    _many_points(points, label)
    if len(p) == 4:
        r, s = natural_coordinates(p, points, eps, label, validated=validated).T
        return q4_shape((r, s)).T
    if len(p) != 3:
        raise ValueError(f'{label}: only T3 and Q4 cells are supported')
    if not validated:
        validate_polygon(p, eps, label, strict=True)
    if not np.all(contains_points(p, points, eps)):
        raise ValueError(f'{label}: point outside cell; extrapolation is forbidden')
    scale = np.linalg.norm(np.ptp(p, axis=0))
    r, s = np.linalg.solve(((p[1:] - p[0]) / scale).T, ((points - p[0]) / scale).T)
    return t3_shape((r, s)).T


def shape_values(vertices, point, eps, label='cell', *, validated=False):
    """Nodal weights at a point, or an (n, nodes) array for an (n, 2) array of points."""
    p = points_array(vertices, label=label)
    point = np.asarray(point, dtype=float)
    if point.ndim == 2:
        return _shape_values_of_points(p, point, eps, label, validated)
    if point.shape != (2,) or not np.all(np.isfinite(point)):
        raise ValueError(f'{label}: expected a finite point')
    if len(p) == 4:
        return q4_shape(natural_coordinates(p, point, eps, label, validated=validated))
    if len(p) != 3:
        raise ValueError(f'{label}: only T3 and Q4 cells are supported')
    if not validated:
        validate_polygon(p, eps, label, strict=True)
    if not contains_point(p, point, eps):
        raise ValueError(f'{label}: point outside cell; extrapolation is forbidden')
    scale = np.linalg.norm(np.ptp(p, axis=0))
    uv = np.linalg.solve(((p[1:] - p[0]) / scale).T, (point - p[0]) / scale)
    return t3_shape(uv)


@dataclass(frozen=True)
class ProjectedPath:
    path_id: int
    points: tuple
    parameters: tuple

    def at(self, s):
        if not 0 <= s <= 1:
            raise ValueError(f'path {self.path_id}: normalized arc length outside [0, 1]')
        i = min(np.searchsorted(self.parameters, s, side='right') - 1, len(self.points) - 2)
        t = (s - self.parameters[i]) / (self.parameters[i + 1] - self.parameters[i])
        return (1 - t) * np.array(self.points[i]) + t * np.array(self.points[i + 1])

    def reversed(self):
        return ProjectedPath(self.path_id, self.points[::-1],
                             tuple(1 - s for s in self.parameters[::-1]))


def project_path(path, panel, label=None):
    label = label or f'panel {panel.panel_id} path {path.id}'
    p = panel.frame.project(path.points, label)
    eps = panel.frame.eps
    for i in range(len(p)):
        for j in range(i + 1, len(p)):
            if np.linalg.norm(p[i] - p[j]) <= eps:
                raise ValueError(f'{label}: repeated points or zero-length segment')
    for i in range(len(p) - 1):
        if i + 2 < len(p):
            u, v = p[i + 1] - p[i], p[i + 2] - p[i + 1]
            if abs(cross(u, v)) <= eps * max(np.linalg.norm(u), np.linalg.norm(v)) and np.dot(u, v) < 0:
                raise ValueError(f'{label}: overlapping adjacent segments')
        for j in range(i + 2, len(p) - 1):
            if segments_intersect(p[i], p[i + 1], p[j], p[j + 1], eps):
                raise ValueError(f'{label}: self-intersecting path')
    if any(not contains_simple(panel.boundary, point, eps) for point in p):
        raise ValueError(f'{label}: path leaves panel; extrapolation is forbidden')
    total = path.cumulative_lengths[-1]
    return ProjectedPath(path.id, frozen_points(p), tuple(s / total for s in path.cumulative_lengths))


@dataclass(frozen=True)
class StripCell:
    points: tuple
    s0: float
    s1: float
    intensities: tuple

    def intensity(self, point, eps, label='strip', *, validated=False):
        # One point gives a number; an (n, 2) array of points gives n values.
        r, v = np.moveaxis(natural_coordinates(self.points, point, eps, label, validated=validated), -1, 0)
        s = self.s0 + (r + 1) * .5 * (self.s1 - self.s0)
        t = (v + 1) * .5
        (p11, p12), (p21, p22) = self.intensities
        return (1 - t) * ((1 - s) * p11 + s * p12) + t * ((1 - s) * p21 + s * p22)


def build_strip(first, second, intensities, eps, label='area load'):
    """Align path two and its intensities, then validate the full ruled strip."""
    a, b = np.array(first.points), np.array(second.points)
    direct = np.linalg.norm(a[0] - b[0])**2 + np.linalg.norm(a[-1] - b[-1])**2
    reverse = np.linalg.norm(a[0] - b[-1])**2 + np.linalg.norm(a[-1] - b[0])**2
    scale = np.linalg.norm(np.ptp(np.vstack((a, b)), axis=0))
    if abs(direct - reverse) <= eps * scale:
        raise ValueError(f'{label}: ambiguous path correspondence')
    values = tuple(tuple(pair) for pair in intensities)
    if reverse < direct:
        second = second.reversed()
        values = (values[0], values[1][::-1])
    # Identical normalized knots from different length arithmetic may differ
    # by a few ulps. Merge only roundoff, not user geometry tolerance.
    knots = []
    for s in sorted(set(first.parameters + second.parameters)):
        if not knots or s - knots[-1] > 32 * np.finfo(float).eps:
            knots.append(s)
    knots[-1] = 1.
    cells, topology = [], []
    for i, (s0, s1) in enumerate(zip(knots, knots[1:])):
        vertices = frozen_points([first.at(s0), first.at(s1), second.at(s1), second.at(s0)])
        cells.append(StripCell(vertices, s0, s1, values))
        topology.append(PanelCell(i, (2*i, 2*i+2, 2*i+3, 2*i+1), vertices))
    validate_partition(tuple(topology), eps, label)
    return tuple(cells)
