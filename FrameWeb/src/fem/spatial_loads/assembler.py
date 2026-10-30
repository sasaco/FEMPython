"""Pure compilation of spatial distributions into structural translation DOFs.

Physical integration pieces retain their shell/triangle owner. The independent
force and moment integrands share the load-wide quadrature error budget with
the nodal basis integrands. No element or BoundaryCondition is modified.
"""
from dataclasses import asdict, dataclass
from numbers import Integral

import numpy as np

from ..dof import DofLayout
from .geometry import signed_area
from .interpolation import shape_values
from .quadrature import integrate_pieces
from .validation import prepare_geometry


@dataclass(frozen=True)
class AssemblyTolerance:
    """Force and moment absolute tolerances use the model's respective units."""
    force_absolute: float = 1e-10
    moment_absolute: float = 1e-10
    relative: float = 1e-9
    max_depth: int = 8

    def __post_init__(self):
        for name in ('force_absolute', 'moment_absolute', 'relative'):
            value = getattr(self, name)
            if isinstance(value, (bool, np.bool_)) or not np.isscalar(value):
                raise ValueError(f'assembly {name} must be a finite nonnegative number')
            try:
                valid = np.isfinite(value) and value >= 0
            except TypeError:
                valid = False
            if not valid:
                raise ValueError(f'assembly {name} must be a finite nonnegative number')
        if self.relative == 0 and (self.force_absolute == 0 or self.moment_absolute == 0):
            raise ValueError('assembly force and moment tolerances must be nonzero')
        if isinstance(self.max_depth, (bool, np.bool_)) or not isinstance(self.max_depth, Integral) or self.max_depth < 0:
            raise ValueError('assembly max_depth must be a nonnegative integer')


def _tuple(values):
    return tuple(float(v) for v in values)


@dataclass(frozen=True)
class CellLoadContribution:
    load_id: int
    panel_id: int
    cell_key: int
    element_id: int | None
    node_ids: tuple[int, ...]
    # All three global force components; no applied nodal couples.
    nodal_forces: tuple[tuple[float, float, float], ...]


@dataclass(frozen=True)
class LoadAudit:
    load_id: int
    panel_id: int
    feature: str
    resultant: tuple
    moment: tuple
    nodal_resultant: tuple
    nodal_moment: tuple
    force_scale: float
    moment_scale: float
    force_error: float
    moment_error: float
    estimated_nodal_error: tuple
    estimated_resultant_error: tuple
    estimated_moment_error: tuple
    evaluations: int
    subdivisions: int
    integrated_length: float
    clipped_area: float


@dataclass(frozen=True)
class SpatialLoadContribution:
    """Immutable per-analysis snapshot; ``to_dict`` returns independent data."""
    node_ids: tuple[int, ...]
    stride: int
    dof_loads: tuple[float, ...]
    cells: tuple[CellLoadContribution, ...]
    loads: tuple[LoadAudit, ...]
    resultant: tuple
    moment: tuple
    nodal_resultant: tuple
    nodal_moment: tuple
    force_scale: float
    moment_scale: float
    force_error: float
    moment_error: float
    warnings: tuple[str, ...] = ()

    def shell_load_vectors(self):
        """Fresh global-coordinate element vectors, in structural node order."""
        result = {}
        for cell in self.cells:
            if cell.element_id is not None:
                vector = result.setdefault(cell.element_id, np.zeros((len(cell.node_ids), 6)))
                vector[:, :3] += cell.nodal_forces
        return {key: value.ravel() for key, value in result.items()}

    def to_dict(self):
        result = asdict(self)
        result['shell_element_loads'] = {
            key: value.tolist() for key, value in self.shell_load_vectors().items()
        }
        result['node_loads'] = {
            node: list(self.dof_loads[i*self.stride:(i+1)*self.stride])
            for i, node in enumerate(self.node_ids)
        }
        return result


def _affine(vertices):
    p = np.asarray(vertices)
    if len(p) == 3:
        return True
    # Only roundoff is accepted here: geometric acceptance tolerance cannot
    # justify replacing a distorted Q4 inverse by a polynomial quadrature.
    q = p - p[0]
    return np.linalg.norm(q[2] - q[1] - q[3]) <= 16*np.finfo(float).eps*np.linalg.norm(np.ptp(p, axis=0))


def _nodal_audit(node_forces, mesh):
    force = np.zeros(3)
    moment = np.zeros(3)
    for node, value in node_forces.items():
        force += value
        moment += np.cross(mesh.nodes[node], value)
    return force, moment


def _check_conservation(force, moment, nodal_force, nodal_moment, force_scale, moment_scale, tolerance, label):
    values = np.r_[force, moment, nodal_force, nodal_moment, force_scale, moment_scale]
    if not np.all(np.isfinite(values)):
        raise ValueError(f'{label}: non-finite load assembly or audit')
    force_error = float(np.max(np.abs(force - nodal_force)))
    moment_error = float(np.max(np.abs(moment - nodal_moment)))
    if force_error > tolerance.force_absolute + tolerance.relative * force_scale:
        raise ValueError(f'{label}: resultant conservation failed')
    if moment_error > tolerance.moment_absolute + tolerance.relative * moment_scale:
        raise ValueError(f'{label}: moment conservation failed')
    return force_error, moment_error


def _compile_load(prepared, mesh, tolerance):
    load, panel = prepared.load, prepared.panel
    label = f'load {load.id} panel {load.panel_id}'
    cells = panel.cells
    offsets = np.cumsum([0, *(len(c.node_ids) for c in cells)])
    count = int(offsets[-1])
    if load.direction.mode == 'normal':
        sign = 1 if signed_area(panel.cells[0].points) > 0 else -1
        load_direction = sign * np.asarray(panel.frame.normal)
    else:
        load_direction = np.asarray(load.direction.vector)
    pieces = []
    active = set()

    def integrand(cell_index, intensity):
        active.add(cell_index)
        cell = cells[cell_index]

        def evaluate(point):
            q = intensity(point)
            value = np.zeros(count + 6)
            value[offsets[cell_index]:offsets[cell_index + 1]] = (
                # Cells were validated by prepare_panel; points are still contained.
                shape_values(cell.points, point, panel.frame.eps, label, validated=True) * q
            )
            # Evaluated from the physical load position, not interpolated nodes.
            position = panel.frame.lift(point)
            force = load_direction * q
            value[count:count + 3] = force
            value[count + 3:] = np.cross(position, force)
            return value
        return evaluate

    length, area = 0., 0.
    if prepared.line_pieces:
        p0, p1 = load.end_intensities[0]
        for piece in prepared.line_pieces:
            a, b = np.asarray(piece.start), np.asarray(piece.end)
            direction = b - a

            def intensity(point, a=a, direction=direction, piece=piece):
                t = np.dot(point - a, direction) / np.dot(direction, direction)
                s = piece.s0 + t * (piece.s1 - piece.s0)
                return (1-s)*p0 + s*p1

            pieces.append((integrand(piece.cell_index, intensity), (a, b)))
            length += float(np.linalg.norm(direction))
        kind, degree = 'line', 3
    else:
        for piece in prepared.area_pieces:
            strip = prepared.strip_cells[piece.strip_index]
            pieces.append((integrand(piece.cell_index,
                                     lambda point, strip=strip: strip.intensity(
                                         point, panel.frame.eps, label, validated=True)),
                           piece.triangle))
            area += abs(signed_area(piece.triangle))
        kind, degree = 'triangle', 4
    if any(not _affine(cells[i].points) for i in active) or any(
            not _affine(strip.points) for strip in prepared.strip_cells):
        degree = None
    # Nodal absolute error is divided across all cell/node components; neither
    # a new cell nor a new strip piece receives another full absolute budget.
    absolute = np.r_[np.full(count, tolerance.force_absolute / max(count, 1)),
                     np.full(3, tolerance.force_absolute), np.full(3, tolerance.moment_absolute)]
    integral = integrate_pieces(pieces, kind=kind, degree=degree, atol=absolute,
                                rtol=tolerance.relative, max_depth=tolerance.max_depth, label=label)
    contributions, node_forces = [], {}
    for i in sorted(active):
        cell = cells[i]
        forces = tuple(_tuple(load_direction * q) for q in integral.value[offsets[i]:offsets[i+1]])
        contributions.append(CellLoadContribution(load.id, load.panel_id, cell.key, cell.element_id,
                                                  cell.node_ids, forces))
        for node, force in zip(cell.node_ids, forces):
            node_forces.setdefault(node, np.zeros(3))[:] += force
    force, moment = integral.value[count:count+3], integral.value[count+3:]
    nodal_force, nodal_moment = _nodal_audit(node_forces, mesh)
    # Constant unit direction: norm of the three absolute component integrals
    # equals integral(abs(q)), including in-plane forces and signed cancellation.
    force_scale = float(np.linalg.norm(integral.absolute_integral[count:count+3]))
    arm = max(float(np.linalg.norm(np.cross(mesh.nodes[n], load_direction))) for n in node_forces)
    moment_scale = force_scale * arm
    errors = _check_conservation(force, moment, nodal_force, nodal_moment,
                                 force_scale, moment_scale, tolerance, label)
    audit = LoadAudit(load.id, load.panel_id, load.feature, _tuple(force), _tuple(moment),
                      _tuple(nodal_force), _tuple(nodal_moment), force_scale, moment_scale, *errors,
                      _tuple(integral.estimated_error[:count]), _tuple(integral.estimated_error[count:count+3]),
                      _tuple(integral.estimated_error[count+3:]), integral.evaluations, integral.subdivisions,
                      length, float(area))
    return tuple(contributions), audit


def compile_spatial_loads(definitions, mesh, *, tolerance=None):
    """Validate and compile all loads without altering input or structural state.

    Public static analysis invokes this once per solve before stiffness assembly.
    """
    tolerance = tolerance if tolerance is not None else AssemblyTolerance()
    if not isinstance(tolerance, AssemblyTolerance):
        raise ValueError('tolerance must be AssemblyTolerance')
    prepared = prepare_geometry(definitions, mesh)
    layout = DofLayout.from_mesh(mesh)
    vector = np.zeros(layout.size)
    cells, audits = [], []
    for item in prepared:
        contributions, audit = _compile_load(item, mesh, tolerance)
        cells.extend(contributions)
        audits.append(audit)
        for cell in contributions:
            for node, force in zip(cell.node_ids, cell.nodal_forces):
                start = layout.node_offsets[node]
                vector[start:start + 3] += force
    node_forces = {node: vector[start:start+3] for node, start in layout.node_offsets.items()}
    nodal_force, nodal_moment = _nodal_audit(node_forces, mesh)
    force = np.sum([a.resultant for a in audits], axis=0) if audits else np.zeros(3)
    moment = np.sum([a.moment for a in audits], axis=0) if audits else np.zeros(3)
    force_scale = sum(a.force_scale for a in audits)
    moment_scale = sum(a.moment_scale for a in audits)
    label = f'spatial loads {[a.load_id for a in audits]} panels {sorted({a.panel_id for a in audits})}'
    errors = _check_conservation(force, moment, nodal_force, nodal_moment,
                                 force_scale, moment_scale, tolerance, label)
    return SpatialLoadContribution(tuple(layout.node_offsets), layout.stride, _tuple(vector),
                                    tuple(cells), tuple(audits), _tuple(force), _tuple(moment),
                                    _tuple(nodal_force), _tuple(nodal_moment), force_scale, moment_scale, *errors)
