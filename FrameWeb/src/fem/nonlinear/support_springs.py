"""Per-analysis support history. Trial evaluation has no side effects."""
from dataclasses import asdict

from .hysteresis.slip import evaluate_slip
from .support_laws import initial_state, law_of, parse_support_law


def add_nonlinear_support(boundary, node, direction, definition, location):
    params = parse_support_law(definition, location=location)
    supports = boundary.nonlinear_spring_supports.setdefault(node, {})
    if direction in supports:
        raise ValueError(f'{location}: duplicate nonlinear spring')
    supports[direction] = params


def validate_support_definitions(boundary, mesh):
    """Validate the final merged definitions against the actual mesh layout."""
    if boundary.nonlinear_spring_supports:
        from ..boundary_dofs import BoundaryDofs
        from ..dof import DofLayout
        layout = DofLayout.from_mesh(mesh)
        BoundaryDofs.from_boundary(boundary, layout.size, layout.stride,
                                   layout.node_offsets.__getitem__)


class SupportSprings:
    def __init__(self, definitions):
        self.definitions = dict(definitions)
        self.reset()

    def reset(self):
        self._committed = {dof: initial_state(params)
                           for dof, (_, _, params) in self.definitions.items()}

    def evaluate(self, displacement, direction_hint=None):
        states = {}
        for dof, (node, direction, params) in self.definitions.items():
            law = law_of(params)
            # Slip still resolves through this module's name, as before the registry.
            evaluate = evaluate_slip if law.type == 'slip' else law.evaluate
            try:
                states[dof] = evaluate(
                    params, self._committed[dof], displacement[dof],
                    direction_hint=0 if direction_hint is None else direction_hint[dof])
            except ValueError as error:
                from ..diagnostics import NumericalConditionError
                raise NumericalConditionError(
                    f'nonlinear support ({law.type}) node {node} direction {direction}: {error}',
                    node=node, direction=direction, support_response=self.snapshot(),
                ) from error
        return states

    def commit(self, states):
        # Replace all states atomically, including multi-support trials.
        if states.keys() != self._committed.keys() or not all(
                isinstance(state, law_of(self.definitions[dof][2]).state_type)
                for dof, state in states.items()):
            raise ValueError('Support commit requires one evaluated state per spring')
        self._committed = dict(states)

    def rollback(self):
        # There is no mutable trial history to restore.
        return None

    def snapshot(self, states=None):
        result = {}
        for dof, state in (self._committed if states is None else states).items():
            node, direction, params = self.definitions[dof]
            law = law_of(params)
            points = None if law.points_passed is None else law.points_passed(params, state)
            result.setdefault(node, {})[direction] = dict(type=law.type, **asdict(state), points_passed=points)
        return result


def validate_support_analysis(boundary, analysis_type):
    if boundary.nonlinear_spring_supports and analysis_type != 'material_nonlinear':
        from ..diagnostics import UnsupportedAnalysisError
        raise UnsupportedAnalysisError(
            'Slip and other nonlinear support springs require material_nonlinear analysis',
            analysis_type=analysis_type,
            support_nodes=sorted(boundary.nonlinear_spring_supports),
        )
