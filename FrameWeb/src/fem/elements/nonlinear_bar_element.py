"""Small-displacement beam with midpoint section hysteresis (manual 7.21).

Section laws act on strain/curvature, not nodal motion or hinge rotation.
Active bending laws update condensed shear increments from their JR tangents.
"""
from typing import Dict, List, Optional
import copy

import numpy as np

from .bar_element import TBarElement
from .loaded_bar_element import beam_mode_system
from .member_line_loads import MemberLineLoads
from ..load_kinds import INCREMENTAL
from ..nonlinear.hysteresis import (
    HysteresisState, JRStiffnessReductionParams, JRStiffnessReductionModel,
)
from ..nonlinear.beam_section_response import (
    BendingPlaneState, _fixed_intervals, evaluate_bending_plane,
)
from ..nonlinear.axial_bending_response import (
    AxialBendingPlaneState, evaluate_axial_bending_plane,
)
from ..nonlinear.axial_force_table import AxialForceTable
from ..diagnostics import InputValidationError, NumericalConditionError


class NonlinearBarElement(MemberLineLoads, TBarElement):
    """One section state per law, indexed by ``center``.

    ``current_states`` is the candidate from the last force evaluation;
    ``committed_states`` is the only source of loading history. Tangent and
    output evaluations never change either. See the phase-2 plan for units.
    Distributed member loads enter as the elastic beam's consistent end loads,
    as on a linear member; the section laws see only the nodal deformation.
    """

    DOF_MAPPING = {
        'axial': (0, 6), 'torsion': (3, 9),
        'moment_y': (4, 10), 'moment_z': (5, 11),
    }

    def __init__(self, element_id: int, node_ids: List[int], material_id: int,
                 section_id: int, angle: float = 0.0, shear_correction: bool = True):
        super().__init__(element_id, node_ids, material_id, section_id, angle, shear_correction)
        self._reset_line_loads()
        self._cache = None
        self.hysteresis_models: Dict[str, JRStiffnessReductionModel] = {}
        self.axial_force_tables: Dict[str, AxialForceTable] = {}
        self.current_states: Dict[str, Dict[str, HysteresisState]] = {}
        self.committed_states: Dict[str, Dict[str, HysteresisState]] = {}
        self.current_bending_states: Dict[str, BendingPlaneState] = {}
        self.committed_bending_states: Dict[str, BendingPlaneState] = {}
        # (global displacement, local nodal resisting force)
        self._trial_response = None
        self._committed_response = None
        self._trial_section_response = {}
        self._committed_section_response = {}

    def get_name(self) -> str:
        return 'nonlinear_bar'

    def set_hysteresis_model(self, dof: str, params: JRStiffnessReductionParams) -> None:
        """Set N(epsilon), T(twist/length), or M(curvature) for a section mode."""
        if dof not in self.DOF_MAPPING:
            raise ValueError(f'Unknown DOF: {dof}. Valid values: {list(self.DOF_MAPPING)}')
        if dof in self.axial_force_tables:
            raise ValueError(f'{dof} already has an axial-force-dependent law')
        model = JRStiffnessReductionModel(params)
        self.hysteresis_models[dof] = model
        self.current_states[dof] = {'center': model.create_initial_state()}
        self.committed_states[dof] = {'center': model.create_initial_state()}
        if dof in ('moment_y', 'moment_z'):
            self.current_bending_states[dof] = BendingPlaneState.initial(model)
            self.committed_bending_states[dof] = BendingPlaneState.initial(model)
        self._trial_response = self._committed_response = None
        self._trial_section_response = {}
        self._committed_section_response = {}

    def set_axial_force_table(self, dof: str, table: AxialForceTable) -> None:
        """Set an immutable Nd-dependent JR law for one bending axis."""
        if dof not in ('moment_y', 'moment_z'):
            raise ValueError('Axial-force tables apply only to moment_y or moment_z')
        if not isinstance(table, AxialForceTable):
            raise TypeError('table must be an AxialForceTable')
        if dof in self.hysteresis_models:
            raise ValueError(f'{dof} already has a fixed hysteresis law')
        self.axial_force_tables[dof] = table
        initial = AxialBendingPlaneState.initial(table)
        self.current_bending_states[dof] = initial
        self.committed_bending_states[dof] = initial
        self.current_states[dof] = {'center': initial.history.copy()}
        self.committed_states[dof] = {'center': initial.history.copy()}
        self._trial_response = self._committed_response = None
        self._trial_section_response = {}
        self._committed_section_response = {}

    def _section_operators(self):
        """Return constant B and reference rigidities for e = B q_local.

        e = [epsilon, tau, kappa_y, kappa_z, s_y, s_z]. The last two modes
        include the elastic bending/shear flexibility eliminated by the exact
        two-node Timoshenko interpolation; they are not raw shear strains.
        """
        if self.material is None or self.bar_param is None:
            raise ValueError('Material properties not set')
        if self.length is None or not np.isfinite(self.length) or self.length <= 0:
            raise ValueError('Element length must be finite and positive')
        l = self.length
        mat = self.material.materials[self.material_id]
        section = self.bar_param
        e, g, a = mat.E, mat.G, section.area
        if not np.all(np.isfinite([e, g, a])) or min(e, g, a) <= 0:
            raise ValueError('E, G and area must be finite and positive')
        rigidities = np.array([e*a, g*section.J, e*section.Iy, e*section.Iz, 0., 0.])
        if not np.all(np.isfinite(rigidities)) or np.any(rigidities < 0):
            raise ValueError('Section rigidities must be finite and nonnegative')
        b = np.zeros((6, 12))
        for row, (i, j) in enumerate(self.DOF_MAPPING.values()):
            b[row, i], b[row, j] = -1/l, 1/l
        b[4, [1, 7, 5, 11]] = [-1/l, 1/l, -0.5, -0.5]
        b[5, [2, 8, 4, 10]] = [-1/l, 1/l, 0.5, 0.5]
        for row, ei, correction in [(4, e*section.Iz, section.kappa_y),
                                     (5, e*section.Iy, section.kappa_z)]:
            if self.shear_correction:
                if not np.isfinite(correction) or correction <= 0:
                    raise ValueError('Shear correction must be finite and positive')
                ga = correction*g*a
                rigidities[row] = 12*ei*ga / (12*ei + ga*l*l)
            else:
                rigidities[row] = 12*ei/(l*l)
        return b, rigidities

    def _evaluate(self, displacement):
        """Pure evaluation from committed history: f=L B.T p, K=df/dq."""
        u = np.asarray(displacement, dtype=float)
        if u.shape != (12,) or not np.all(np.isfinite(u)):
            raise ValueError('Expected 12 finite element displacements')
        t = self.get_transformation_matrix(12)
        b, rigidities = self._section_operators()
        deformation = b @ (t @ u)
        p = rigidities * deformation
        tangent = np.diag(rigidities)
        states = {}
        bending_states = {}
        section_response = {}
        # N must be evaluated before an Nd-dependent bending axis.  Torsion is
        # independent; fixed bending planes retain the established kernel.
        for row, dof in enumerate(self.DOF_MAPPING):
            if dof not in self.hysteresis_models or dof in ('moment_y', 'moment_z'):
                continue
            model = self.hysteresis_models[dof]
            state = self.committed_states[dof]['center'].copy()
            p[row], tangent[row, row], info = model.get_force_and_stiffness(deformation[row], state)
            states[dof] = {'center': model.update_state(
                deformation[row], p[row], tangent[row, row], state, info)}

        axial_model = self.hysteresis_models.get('axial')

        def axial_path(epsilon):
            """Return the actual piecewise-linear N(epsilon) trial path."""
            if axial_model is None:
                axial_rigidity = rigidities[0]
                return axial_rigidity*epsilon, axial_rigidity, ((1., -axial_rigidity*epsilon),)
            committed_axial = self.committed_states['axial']['center']
            force, stiffness, _ = axial_model.get_force_and_stiffness(epsilon, committed_axial)
            intervals = _fixed_intervals(axial_model, committed_axial, epsilon)
            if not intervals:
                return force, stiffness, ((1., -force),)
            delta = epsilon-committed_axial.current_delta
            running = committed_axial.current_P
            legs = []
            for part in intervals:
                running += part.tangent*(part.end-part.start)
                fraction = (part.end-committed_axial.current_delta)/delta
                legs.append((float(fraction), float(-running)))
            # Preserve the JR evaluator's terminal force convention at an event.
            legs[-1] = (1., float(-force))
            return force, stiffness, tuple(legs)

        def trace_axial_plane(table, committed, epsilon, curvature, shear,
                             *, length, young_modulus, shear_rigidity):
            """Advance bending over every affine leg of a nonlinear N path."""
            _, _, legs = axial_path(epsilon)
            candidate = committed
            response = None
            for fraction, trial_nd in legs:
                trial_curvature = committed.curvature+fraction*(curvature-committed.curvature)
                trial_shear = (committed.shear_deformation
                               + fraction*(shear-committed.shear_deformation))
                response = evaluate_axial_bending_plane(
                    table, candidate, trial_curvature, trial_shear, trial_nd,
                    length=length, young_modulus=young_modulus,
                    shear_rigidity=shear_rigidity, compute_tangent=False,
                )
                candidate = response.state
            return response

        def axial_plane_tangent(table, committed, target, base, *, length,
                                young_modulus, shear_rigidity):
            """Differentiate the complete nonlinear-axial section map."""
            target = np.asarray(target, dtype=float)
            scales = np.array([
                max(abs(target[0]), 1e-3),
                max(point for row in table.rows
                    for side in (row.positive, row.negative)
                    for point in side.curvatures),
                max(abs(target[2]), 1e-3),
            ])
            result = np.empty((2, 3))
            base_values = np.array([base.state.moment, base.state.shear_force])

            def values(candidate):
                trial = trace_axial_plane(
                    table, committed, *candidate, length=length,
                    young_modulus=young_modulus, shear_rigidity=shear_rigidity,
                )
                return np.array([trial.state.moment, trial.state.shear_force])

            for column in range(3):
                step = 1e-7*scales[column]
                plus, minus = target.copy(), target.copy()
                plus[column] += step
                minus[column] -= step
                try:
                    upper = values(plus)
                except InputValidationError as error:
                    if error.details.get('reason') != 'axial_force_out_of_range':
                        raise
                    upper = None
                try:
                    lower = values(minus)
                except InputValidationError as error:
                    if error.details.get('reason') != 'axial_force_out_of_range':
                        raise
                    lower = None
                if upper is not None and lower is not None:
                    result[:, column] = (upper-lower)/(2*step)
                elif upper is not None:
                    result[:, column] = (upper-base_values)/step
                elif lower is not None:
                    result[:, column] = (base_values-lower)/step
                else:
                    raise NumericalConditionError(
                        'Cannot evaluate coupled section tangent',
                        reason='axial_force_tangent_domain',
                    )
            return result

        for row, dof in enumerate(self.DOF_MAPPING):
            if dof not in ('moment_y', 'moment_z'):
                continue
            if dof in self.axial_force_tables:
                table = self.axial_force_tables[dof]
                shear_row = 5 if dof == 'moment_y' else 4
                correction = self.bar_param.kappa_z if shear_row == 5 else self.bar_param.kappa_y
                material = self.material.materials[self.material_id]
                ga = correction*material.G*self.bar_param.area if self.shear_correction else None
                try:
                    if axial_model is None:
                        response = evaluate_axial_bending_plane(
                            table, self.committed_bending_states[dof],
                            deformation[row], deformation[shear_row], -p[0],
                            length=self.length, young_modulus=material.E,
                            shear_rigidity=ga,
                        )
                        plane_tangent = response.tangent.copy()
                        plane_tangent[:, 0] *= -tangent[0, 0]
                    else:
                        response = trace_axial_plane(
                            table, self.committed_bending_states[dof],
                            deformation[0], deformation[row], deformation[shear_row],
                            length=self.length, young_modulus=material.E,
                            shear_rigidity=ga,
                        )
                        plane_tangent = axial_plane_tangent(
                            table, self.committed_bending_states[dof],
                            (deformation[0], deformation[row], deformation[shear_row]),
                            response, length=self.length, young_modulus=material.E,
                            shear_rigidity=ga,
                        )
                except (InputValidationError, NumericalConditionError) as error:
                    error.details.update(element_id=self.element_id, axis=dof[-1])
                    raise
                indices = [row, shear_row]
                p[indices] = [response.state.moment, response.state.shear_force]
                # plane columns are [axial strain, curvature, shear].
                tangent[indices, 0] = plane_tangent[:, 0]
                tangent[np.ix_(indices, indices)] = plane_tangent[:, 1:]
                bending_states[dof] = response.state
                states[dof] = {'center': response.state.history.copy()}
                interpolation = table.interpolate(-p[0])
                section_response[dof[-1]] = {
                    'curvature': response.state.curvature, 'moment': response.state.moment,
                    'shear_deformation': response.state.shear_deformation,
                    'shear_force': response.state.shear_force,
                    'bending_tangent': response.bending_tangent,
                    'effective_inertia': response.effective_inertia,
                    'shear_coefficient': response.shear_coefficient,
                    'branch': response.state.history.branch,
                    'interpolation': {
                        'lower_Nd': interpolation.lower_Nd,
                        'upper_Nd': interpolation.upper_Nd,
                        'fraction': interpolation.fraction,
                    },
                    'skeleton': {
                        'pos': [list(pair) for pair in zip(
                            interpolation.positive.curvatures,
                            interpolation.positive.moments)],
                        'neg': [list(pair) for pair in zip(
                            interpolation.negative.curvatures,
                            interpolation.negative.moments)],
                    },
                }
                continue
            if dof in self.hysteresis_models:
                model = self.hysteresis_models[dof]
                shear_row = 5 if dof == 'moment_y' else 4
                correction = self.bar_param.kappa_z if shear_row == 5 else self.bar_param.kappa_y
                material = self.material.materials[self.material_id]
                ga = correction*material.G*self.bar_param.area if self.shear_correction else None
                try:
                    response = evaluate_bending_plane(
                        model, self.committed_bending_states[dof],
                        deformation[row], deformation[shear_row], length=self.length,
                        young_modulus=material.E, shear_rigidity=ga)
                except NumericalConditionError as error:
                    error.details.update(element_id=self.element_id, axis=dof[-1])
                    raise
                indices = [row, shear_row]
                p[indices] = [response.state.moment, response.state.shear_force]
                tangent[np.ix_(indices, indices)] = response.tangent
                bending_states[dof] = response.state
                states[dof] = {'center': response.state.history.copy()}
                section_response[dof[-1]] = {
                    'curvature': response.state.curvature, 'moment': response.state.moment,
                    'shear_deformation': response.state.shear_deformation,
                    'shear_force': response.state.shear_force,
                    'bending_tangent': response.bending_tangent,
                    'effective_inertia': response.effective_inertia,
                    'shear_coefficient': response.shear_coefficient,
                    'branch': response.state.history.branch,
                    'skeleton': {
                        side: [[getattr(model.params, f'delta_{i}_{side}'),
                                getattr(model.params, f'P_{i}_{side}')]
                               for i in (1, 2, 3, 4)
                               if getattr(model.params, f'delta_{i}_{side}') is not None]
                        for side in ('pos', 'neg')
                    },
                    # GLOSSARY 部材の状態: skeleton points passed by the largest curvature so far.
                    'points_passed': max(
                        sum(extreme >= getattr(model.params, f'delta_{i}_{side}')
                            for i in (1, 2, 3, 4) if getattr(model.params, f'delta_{i}_{side}') is not None)
                        for side, extreme in (('pos', response.state.history.delta_max_pos),
                                              ('neg', response.state.history.delta_max_neg))),
                }
        f_local = self.length * b.T @ p
        k_local = self.length * b.T @ tangent @ b
        for values in section_response.values():
            values.update(N=float(p[0]), Nd=float(-p[0]))
        return f_local, t.T @ k_local @ t, states, bending_states, section_response

    def get_internal_force(self, displacement: np.ndarray) -> np.ndarray:
        f_local, _, states, bending_states, section_response = self._evaluate(displacement)
        self.current_states = states
        self.current_bending_states = bending_states
        self._trial_section_response = section_response
        self._trial_response = (np.array(displacement, copy=True), f_local.copy())
        return self.get_transformation_matrix(12).T @ f_local

    def get_tangent_stiffness_matrix(self, displacement: np.ndarray) -> np.ndarray:
        return self._evaluate(displacement)[1]

    def calculate_forces(self, displacement: np.ndarray) -> Dict[str, np.ndarray]:
        """Local nodal resisting forces [N,Vy,Vz,T,My,Mz], without history changes.

        At the converged displacement use the force accepted by Newton, including
        after commit (re-evaluating a JR branch could change its force).
        """
        response = self._committed_response
        if response is not None and np.array_equal(displacement, response[0]):
            local = response[1]
        else:
            local = self._evaluate(displacement)[0]
        if any(np.any(table) for table in self.line_loads.values()):
            local = local-self.member_load_at(self.load_factor)
        return {'i_end': local[:6].copy(), 'j_end': local[6:].copy()}

    def local_member_loads(self):
        material = self.material.materials[self.material_id]
        return {kind: beam_mode_system(self.length, material, self.bar_param, np.zeros(4),
                                       table, self.shear_correction)[1]
                for kind, table in self.line_loads.items()}

    def get_member_load_vector(self):
        return self.get_member_load_vectors()[INCREMENTAL]

    def commit_state(self) -> None:
        self.committed_states = copy.deepcopy(self.current_states)
        self.committed_bending_states = copy.deepcopy(self.current_bending_states)
        self._committed_response = copy.deepcopy(self._trial_response)
        self._committed_section_response = copy.deepcopy(self._trial_section_response)

    def get_section_response(self):
        """Accepted central bending response, without re-evaluating history."""
        return {'center': copy.deepcopy(self._committed_section_response)}

    def calculate_curvature(self, displacement: np.ndarray) -> Dict[str, float]:
        """Midpoint total curvature about local y/z (1/m), without history changes.

        These are the generalized deformations used by the M-curvature laws,
        including elastic bending on an axis without a hysteresis law.
        """
        u = np.asarray(displacement, dtype=float)
        if u.shape != (12,) or not np.all(np.isfinite(u)):
            raise ValueError('Expected 12 finite element displacements')
        b, _ = self._section_operators()
        deformation = b @ (self.get_transformation_matrix(12) @ u)
        return {'y': float(deformation[2]), 'z': float(deformation[3])}

    def rollback_state(self) -> None:
        self.current_states = copy.deepcopy(self.committed_states)
        self.current_bending_states = copy.deepcopy(self.committed_bending_states)
        self._trial_response = copy.deepcopy(self._committed_response)
        self._trial_section_response = copy.deepcopy(self._committed_section_response)

    def reset_states(self) -> None:
        self.committed_states = {
            dof: {'center': model.create_initial_state()}
            for dof, model in self.hysteresis_models.items()
        }
        self.committed_bending_states = {
            **{dof: BendingPlaneState.initial(model)
               for dof, model in self.hysteresis_models.items()
               if dof in ('moment_y', 'moment_z')},
            **{dof: AxialBendingPlaneState.initial(table)
               for dof, table in self.axial_force_tables.items()},
        }
        self.committed_states.update({
            dof: {'center': state.history.copy()}
            for dof, state in self.committed_bending_states.items()
            if dof in self.axial_force_tables
        })
        self.current_states = copy.deepcopy(self.committed_states)
        self.current_bending_states = copy.deepcopy(self.committed_bending_states)
        self._trial_response = self._committed_response = None
        self._trial_section_response = {}
        self._committed_section_response = {}

    def get_hysteresis_state(self, dof: str) -> Optional[Dict[str, HysteresisState]]:
        """Return a snapshot of the candidate central section state."""
        return copy.deepcopy(self.current_states.get(dof))

    def is_nonlinear(self) -> bool:
        return bool(self.hysteresis_models or self.axial_force_tables)

    def get_nonlinear_dofs(self) -> List[str]:
        return list(dict.fromkeys((*self.hysteresis_models, *self.axial_force_tables)))

    def get_max_displacement(self, dof: str) -> Dict[str, float]:
        """Maximum generalized strain/curvature, not endpoint displacement."""
        if dof not in self.current_states:
            return {}
        state = self.current_states[dof]['center']
        return {'center_pos': state.delta_max_pos, 'center_neg': state.delta_max_neg}
