"""Distributed member loads of a beam, grouped by load kind (fem.load_kinds).

Each kind keeps its own local (4, 2) table of [axial, y, z, torsion] values at
the i and j ends. The element applies kind.factor(load_factor) to each kind's
consistent load, so a beam without kinds keeps load_factor*f exactly.
"""
import numpy as np

from ..load_kinds import INCREMENTAL

_DIRECTIONS = {'x': 'Lx', 'y': 'Ly', 'z': 'Lz', 'r': 'Lr', 'gx': 'GX', 'gy': 'GY', 'gz': 'GZ'}


class MemberLineLoads:
    """Mixin for beams; requires transformation_matrix and a _cache attribute."""

    def _reset_line_loads(self):
        self.line_loads = {INCREMENTAL: np.zeros((4, 2))}
        self.load_factor = 1.

    @property
    def line_load(self):
        """The incremental table, edited in place by load-transfer preprocessing."""
        return self.line_loads[INCREMENTAL]

    def set_line_load(self, direction, values, kind=INCREMENTAL):
        direction = _DIRECTIONS.get(direction, direction)
        table = self.line_loads.setdefault(kind, np.zeros((4, 2)))
        if direction in ('GX', 'GY', 'GZ'):
            loads = np.zeros((3, 2))
            loads[('GX', 'GY', 'GZ').index(direction)] = values
            table[:3] += self.transformation_matrix @ loads
        elif direction in ('Lx', 'Ly', 'Lz', 'Lr'):
            table[('Lx', 'Ly', 'Lz', 'Lr').index(direction)] += values
        else:
            raise ValueError(f'Unknown beam load direction: {direction}')
        self._cache = None

    def local_member_loads(self):
        """Local consistent end loads {kind: f(12)} at their full input value."""
        raise NotImplementedError

    def member_load_at(self, load_factor):
        """Local consistent end load applied at load_factor, summed over kinds."""
        terms = [kind.factor(load_factor)*f for kind, f in self.local_member_loads().items()]
        total = terms[0]
        for term in terms[1:]:
            total = total+term
        return total

    def get_member_load_vectors(self):
        """Global consistent end loads {kind: f(12)} for the solver's load pattern."""
        t = self.get_transformation_matrix(12)
        return {kind: t.T@f for kind, f in self.local_member_loads().items()}
