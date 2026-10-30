"""Physical line/triangle quadrature with componentwise global error budgets.

The callback supplies the entire integrand (e.g. shape times load plus force
and moment audit components). Weights already contain physical ds or dA;
callers must not multiply by a shell Jacobian again.
"""
from dataclasses import dataclass
from functools import lru_cache
from numbers import Integral

import numpy as np
from numpy.polynomial.legendre import leggauss

from .geometry import cross, points_array


@lru_cache(maxsize=32)
def _gauss(order):
    x, w = leggauss(order)
    return (x + 1) / 2, w / 2


def _order(order):
    if isinstance(order, bool) or not isinstance(order, Integral) or order < 1:
        raise ValueError('quadrature order must be a positive integer')


def line_rule(start, end, order=2):
    _order(order)
    a, b = points_array([start, end])
    length = np.linalg.norm(b - a)
    if not np.isfinite(length) or length <= 0:
        raise ValueError('quadrature: degenerate line')
    x, w = _gauss(order)
    return a + x[:, None] * (b - a), w * length


def triangle_rule(vertices, order=4):
    """Duffy tensor Gauss, exact through total polynomial degree 2*order-2."""
    _order(order)
    p = points_array(vertices)
    if len(p) != 3:
        raise ValueError('quadrature: expected a triangle')
    a, b, c = p
    jacobian = abs(cross(b - a, c - a))
    if not np.isfinite(jacobian) or jacobian <= 0:
        raise ValueError('quadrature: degenerate triangle')
    x, w = _gauss(order)
    u, v = np.meshgrid(x, x, indexing='ij')
    weights = w[:, None] * w[None, :] * (1 - u) * jacobian
    points = a + u[..., None] * (b - a) + ((1-u)*v)[..., None] * (c - a)
    return points.reshape((-1, 2)), weights.ravel()


@dataclass(frozen=True)
class IntegralResult:
    value: np.ndarray
    estimated_error: np.ndarray
    absolute_integral: np.ndarray
    evaluations: int
    subdivisions: int


def _evaluate(function, rule, label, batch=False):
    points, weights = rule
    if batch:
        values = np.asarray(function(points), dtype=float)
        if values.ndim == 0 or len(values) != len(points):
            raise ValueError(f'{label}: a batch integrand must return one row per point')
    else:
        values = np.asarray([function(point) for point in points], dtype=float)
    if not np.all(np.isfinite(values)):
        raise ValueError(f'{label}: non-finite quadrature integrand')
    result = np.tensordot(weights, values, axes=(0, 0))
    magnitude = np.tensordot(weights, np.abs(values), axes=(0, 0))
    if not np.all(np.isfinite(result)) or not np.all(np.isfinite(magnitude)):
        raise ValueError(f'{label}: quadrature overflow')
    return result, magnitude


def _children(domain, kind):
    if kind == 'line':
        a, b = domain
        mid = (a + b) / 2
        return ((a, mid), (mid, b))
    a, b, c = domain
    ab, bc, ca = (a+b)/2, (b+c)/2, (c+a)/2
    return ((a, ab, ca), (ab, b, bc), (ca, bc, c), (ab, bc, ca))


def _scatter(parts, indices, size, label):
    """Sum per-piece results; with component indices, each piece adds into its own slots."""
    if indices is None:
        return np.sum(parts, axis=0)
    total = np.zeros(size)
    for part, index in zip(parts, indices):
        if np.shape(part) != (len(index),):
            raise ValueError(f'{label}: an integrand must return one value per component index')
        total[index] += part
    return total


def _integrate(function, domains, kind, *, degree, atol, rtol, max_depth, label, batch=False,
               indices=None, size=None):
    if not isinstance(max_depth, Integral) or isinstance(max_depth, bool) or max_depth < 0:
        raise ValueError(f'{label}: max_depth must be a nonnegative integer')
    absolute, relative = np.asarray(atol, dtype=float), np.asarray(rtol, dtype=float)
    if (not np.all(np.isfinite(absolute)) or not np.all(np.isfinite(relative))
            or np.any(absolute < 0) or np.any(relative < 0)
            or np.any((absolute == 0) & (relative == 0))):
        raise ValueError(f'{label}: quadrature tolerances must be finite, nonnegative and nonzero')
    if degree is not None and (isinstance(degree, bool) or not isinstance(degree, Integral) or degree < 0):
        raise ValueError(f'{label}: polynomial degree must be a nonnegative integer')
    domains = [points_array(d) for d in domains]
    if not domains:
        raise ValueError(f'{label}: empty integration domain')
    functions = [function] * len(domains) if callable(function) else list(function)
    if len(functions) != len(domains):
        raise ValueError(f'{label}: each domain needs its own integrand')
    rule = (lambda d, n: line_rule(*d, n)) if kind == 'line' else triangle_rule
    if degree is not None:
        order = max(1, (degree + (2 if kind == 'line' else 3)) // 2)
        results = [_evaluate(f, rule(d, order), label, batch) for f, d in zip(functions, domains)]
        value = _scatter([r[0] for r in results], indices, size, label)
        magnitude = _scatter([r[1] for r in results], indices, size, label)
        if not np.all(np.isfinite(value)) or not np.all(np.isfinite(magnitude)):
            raise ValueError(f'{label}: summed quadrature overflow')
        return IntegralResult(value, np.zeros_like(value), magnitude,
                              len(domains) * order**(1 if kind == 'line' else 2), 0)
    evaluations, subdivisions = 0, 0

    def estimate(function, domain, depth, index):
        nonlocal evaluations
        low, _ = _evaluate(function, rule(domain, 4), label, batch)
        high, magnitude = _evaluate(function, rule(domain, 8), label, batch)
        evaluations += 12 if kind == 'line' else 80
        return (domain, depth, high, np.abs(high - low), magnitude, function, index)

    leaves = [estimate(f, d, 0, i) for f, d, i in zip(functions, domains, indices or [None] * len(domains))]
    leaf_indices = lambda: None if indices is None else [leaf[6] for leaf in leaves]
    while True:
        value = _scatter([leaf[2] for leaf in leaves], leaf_indices(), size, label)
        error = _scatter([leaf[3] for leaf in leaves], leaf_indices(), size, label)
        magnitude = _scatter([leaf[4] for leaf in leaves], leaf_indices(), size, label)
        if not all(np.all(np.isfinite(v)) for v in (value, error, magnitude)):
            raise ValueError(f'{label}: summed quadrature overflow')
        # The error is summed across ALL pieces before acceptance. Each
        # component has its own units. Integral(abs(f)) prevents cancellation
        # of positive and negative loads from collapsing the relative scale.
        budget = absolute + relative * magnitude
        if budget.shape != np.shape(value) and budget.shape != ():
            raise ValueError(f'{label}: tolerance shape differs from integrand')
        if np.all(error <= budget):
            return IntegralResult(value, error, magnitude, evaluations, subdivisions)
        candidates = [i for i, leaf in enumerate(leaves) if leaf[1] < max_depth]
        if not candidates:
            raise ValueError(f'{label}: quadrature did not converge at subdivision limit {max_depth}')
        tiny = np.finfo(float).tiny
        def share(i):
            local = budget if indices is None or budget.shape == () else budget[leaves[i][6]]
            return float(np.max(leaves[i][3] / np.maximum(local, tiny)))
        index = max(candidates, key=share)
        domain, depth, _, _, _, function, components = leaves.pop(index)
        leaves.extend(estimate(function, child, depth + 1, components) for child in _children(domain, kind))
        subdivisions += 1


def integrate_lines(function, segments, *, degree=None, atol=1e-10, rtol=1e-9,
                    max_depth=10, label='line load'):
    """Integrate all segments against one shared componentwise error budget."""
    return _integrate(function, segments, 'line', degree=degree, atol=atol, rtol=rtol,
                      max_depth=max_depth, label=label)


def integrate_triangles(function, triangles, *, degree=None, atol=1e-10, rtol=1e-9,
                        max_depth=8, label='area load'):
    """Integrate all triangles against one shared componentwise error budget."""
    return _integrate(function, triangles, 'triangle', degree=degree, atol=atol, rtol=rtol,
                      max_depth=max_depth, label=label)


def integrate_pieces(pieces, *, kind, degree=None, atol=1e-10, rtol=1e-9,
                     max_depth=8, label='spatial load', batch=False, size=None):
    """Integrate (callback, physical domain) pairs with ONE load-wide budget.

    Callbacks must return the same component layout. A piece retains its
    structural cell and load-field owner throughout adaptive refinement.
    With ``batch`` a callback takes all (n, 2) points of a rule at once and
    returns one row per point. A piece given as (callback, domain, component
    indices) returns only those components of a total of length ``size``;
    the totals and the shared error budget are the same as for full rows.
    """
    if kind not in ('line', 'triangle'):
        raise ValueError(f'{label}: integration kind must be line or triangle')
    pieces = tuple(pieces)
    indices = None
    if pieces and all(len(p) == 3 for p in pieces):
        if isinstance(size, bool) or not isinstance(size, Integral) or size < 1:
            raise ValueError(f'{label}: component indices require a positive size')
        indices = [np.asarray(p[2], dtype=int) for p in pieces]
        if any(i.ndim != 1 or len(np.unique(i)) != len(i) or np.any((i < 0) | (i >= size)) for i in indices):
            raise ValueError(f'{label}: component indices must be distinct and below size')
    elif any(len(p) != 2 for p in pieces) or size is not None:
        raise ValueError(f'{label}: give component indices for every piece, with size, or for none')
    return _integrate(tuple(p[0] for p in pieces), [p[1] for p in pieces], kind,
                      degree=degree, atol=atol, rtol=rtol, max_depth=max_depth, label=label, batch=batch,
                      indices=indices, size=size)
