"""Normalized supports with separate spring assembly and constraint elimination.

Element internal forces and tangents never include grounded support springs.
Add their contributions once, then apply the control strategy's constraints.
"""

from dataclasses import dataclass, field
from math import fsum

import numpy as np
from scipy.sparse import lil_matrix


SPRING_DIRECTIONS = {'x': 0, 'y': 1, 'z': 2, 'rx': 3, 'ry': 4, 'rz': 5}


def spring_force(u, springs, correction=None):
    """Positive restoring force; a support reaction has the opposite sign."""
    force = np.zeros_like(u)
    for dof, stiffness in springs.items():
        displacement = u[dof] if correction is None else u[dof] + correction[dof]
        force[dof] = stiffness * displacement
    return force


@dataclass
class BoundaryDofs:
    size: int
    stride: int
    prescribed: dict
    springs: dict
    nonlinear_springs: dict = field(default_factory=dict)
    support_state: object = field(default=None, repr=False)
    free: np.ndarray = field(init=False, repr=False)

    def __post_init__(self):
        self.prescribed = dict(self.prescribed)
        self.springs = dict(self.springs)
        self.nonlinear_springs = dict(self.nonlinear_springs)
        if self.nonlinear_springs and self.support_state is None:
            from .nonlinear.support_springs import SupportSprings
            self.support_state = SupportSprings(self.nonlinear_springs)
        free = np.ones(self.size, dtype=bool)
        free[list(self.prescribed)] = False
        self.free = np.flatnonzero(free)

    @classmethod
    def from_boundary(cls, boundary, size, stride, node_start):
        """Resolve external node IDs and the legacy abs(value)>1000 convention."""
        from .nonlinear.support_laws import parse_support_law
        if stride not in (3, 6):
            raise ValueError('max_dof_per_node must be 3 or 6')
        prescribed, springs = {}, {}
        for node_id, restraint in boundary.restraints.items():
            base = node_start(node_id)
            if base < 0 or base + stride > size:
                raise ValueError(f'Restraint node {node_id} is outside the DOF vector')
            for i, fixed in enumerate(restraint.dof_restraints[:stride]):
                if fixed:
                    value = restraint.get_value(i)
                    if not np.isfinite(value):
                        raise ValueError(f'Non-finite restraint value at node {node_id}')
                    if abs(value) > 1000:
                        springs[base + i] = abs(value)
                    else:
                        prescribed[base + i] = value
        for node_id, supports in getattr(boundary, 'spring_supports', {}).items():
            base = node_start(node_id)
            if base < 0 or base + stride > size:
                raise ValueError(f'Spring node {node_id} is outside the DOF vector')
            for direction, stiffness in supports.items():
                if direction not in SPRING_DIRECTIONS or SPRING_DIRECTIONS[direction] >= stride:
                    raise ValueError(f'Invalid spring direction: {direction}')
                if not np.isfinite(stiffness) or stiffness <= 0:
                    raise ValueError('Spring stiffness must be finite and positive')
                dof = base + SPRING_DIRECTIONS[direction]
                if dof in prescribed or dof in springs:
                    raise ValueError('Conflicting restraint and spring at same DOF')
                springs[dof] = stiffness
        nonlinear = {}
        for node_id, supports in getattr(boundary, 'nonlinear_spring_supports', {}).items():
            if not isinstance(supports, dict):
                raise ValueError(f'nonlinear_spring_supports node {node_id}: expected direction object')
            for direction, definition in supports.items():
                location = f'nonlinear_spring_supports node {node_id} direction {direction}'
                try:
                    base = node_start(node_id)
                except (KeyError, ValueError) as error:
                    raise ValueError(f'{location}: unknown node') from error
                if base < 0 or base + stride > size:
                    raise ValueError(f'{location}: node is outside the DOF vector')
                if direction not in SPRING_DIRECTIONS or SPRING_DIRECTIONS[direction] >= stride:
                    raise ValueError(f'{location}: invalid direction for {stride} DOFs')
                dof = base + SPRING_DIRECTIONS[direction]
                if dof in prescribed or dof in springs or dof in nonlinear:
                    raise ValueError(f'{location}: conflicting restraint or spring at same DOF')
                nonlinear[dof] = (node_id, direction, parse_support_law(definition, location=location))
        return cls(size, stride, prescribed, springs, nonlinear)

    def add_spring_stiffness(self, tangent, u=None, *, direction_hint=None):
        supported = lil_matrix(tangent, copy=True)
        for dof, stiffness in self.springs.items():
            supported[dof, dof] += stiffness
        if self.support_state is not None:
            if u is None:
                raise ValueError('Nonlinear support tangent requires a displacement')
            for dof, state in self.support_state.evaluate(u, direction_hint).items():
                supported[dof, dof] += state.tangent
        return supported.tocsr()

    def spring_force(self, u, correction=None):
        force = spring_force(u, self.springs, correction)
        if self.support_state is not None:
            for dof, state in self.support_state.evaluate(u if correction is None else u+correction).items():
                force[dof] += state.force
        return force

    def residual(self, structural_residual, u, correction=None):
        """Subtract support forces from external minus element internal forces.

        Keep constrained components until the caller projects or eliminates them.
        With two-part displacements, retain the low part during cancellation.
        """
        residual = np.array(structural_residual, dtype=float, copy=True)
        if correction is None:
            residual -= self.spring_force(u)
        else:
            for dof, stiffness in self.springs.items():
                residual[dof] = fsum([residual[dof], -stiffness*u[dof],
                                      -stiffness*correction[dof]])
            if self.support_state is not None:
                for dof, state in self.support_state.evaluate(u+correction).items():
                    residual[dof] -= state.force
        return residual

    def project(self, vector):
        projected = vector.copy()
        projected[list(self.prescribed)] = 0.0
        return projected

    def constrain(self, supported_tangent, residual, u, *, load_factor=1., penalty_scale=None):
        """Constrain increments after spring assembly; never add spring terms."""
        matrix = lil_matrix(supported_tangent, copy=True)
        rhs = np.array(residual, dtype=float, copy=True)
        if self.prescribed:
            indices = list(self.prescribed)
            increments = np.array([load_factor*self.prescribed[d]-u[d] for d in indices])
            if penalty_scale is not None:
                for dof, value in zip(indices, increments):
                    matrix[dof, dof] = penalty_scale
                    rhs[dof] = penalty_scale*value
            else:
                rhs -= matrix.tocsr()[:, indices] @ increments
                matrix[:, indices] = 0
                matrix[indices, :] = 0
                for dof, value in zip(indices, increments):
                    matrix[dof, dof] = 1.
                    rhs[dof] = value
        return matrix.tocsr(), rhs

    def reactions(self, internal, external, u, *, correction=None, precise=None):
        """Element balance at supports, retaining direct-solve reaction precision.

        Nonlinear reactions use accepted element forces, not tangent times u.
        Direct solves retain their existing compensated support reaction rule.
        """
        reaction = internal-external if precise is None else precise.copy()
        if correction is not None:
            force = self.spring_force(u, correction)
            for dof in self.springs:
                reaction[dof] = -force[dof]
        return reaction
