"""
有限要素法のソルバーモジュール
JavaScript版のSolver機能に対応
"""
from typing import Dict, Any, List, Tuple, Optional
from copy import deepcopy
import numpy as np
from scipy.linalg import eigh
from scipy.sparse import csr_matrix
from scipy.sparse.linalg import ArpackNoConvergence, eigsh
from .mesh import MeshModel
from .boundary_condition import BoundaryCondition
from .boundary_dofs import BoundaryDofs, SPRING_DIRECTIONS, spring_force
from .material import Material
from .dof import DofLayout
from .diagnostics import ModalConvergenceError, UnsupportedAnalysisError
from .convergence import characteristic_length
from .equilibrium import (
    NonlinearConvergenceError, direct_step, displacement_control_iteration,
    newton_iteration, solve_direct_system, solve_newton_system,
)
from .load_kinds import INCREMENTAL, LoadPattern
from .solver_results import snapshot, final_result
from .spatial_loads import compile_spatial_loads


class Solver:
    """FEM解析のソルバークラス"""
    
    DEFAULT_N_STEPS = 10
    DEFAULT_MAX_ITER = 50
    DEFAULT_TOL = 1e-6

    def __init__(self):
        self._reset_analysis_state()

    def _reset_analysis_state(self):
        self.clear_spatial_load_state()
        self.assembled_stiffness = None
        self.assembled_stiffness_correction = None
        self.assembled_mass = None
        self.load_vector = None
        self.load_pattern = None
        self.displacement = None
        self.displacement_correction = None
        self.eigenvalues = None
        self.eigenvectors = None
        self.layout = None
        self._layout_signature = None
        self._node_dof_offsets = None
        self.convergence_history = []
        self.step_results = []
        self._last_internal_force = None
        self.load_factor = 0.0
        self.precise_end_forces = None
        self.precise_reactions = None
        self.interpolated_displacements = {}
        self.analysis_warnings = []
        self.characteristic_length = None
        self.characteristic_length_source = None
        self.support_springs = None

    def clear_spatial_load_state(self):
        """Discard compiled data, including on preflight or solve failure."""
        if getattr(self, 'spatial_load_contribution', None) is not None:
            self.load_vector = None
            self.load_pattern = None
        for result in getattr(self, 'step_results', ()):
            result.pop('spatial_load_contribution', None)
            result.pop('element_nodal_equilibrium_forces', None)
        self.spatial_load_contribution = None
        self._shell_direct_loads = {}

    def _validate_spatial_analysis(self, boundary, analysis_type):
        definitions = boundary.spatial_loads
        if definitions.loads and analysis_type != 'static':
            raise UnsupportedAnalysisError(
                'Spatial loads are limited to static analysis.', analysis_type=analysis_type,
                features=sorted({load.feature for load in definitions.loads}),
                panel_ids=sorted({load.panel_id for load in definitions.loads}),
                load_ids=[load.id for load in definitions.loads],
            )

    def _set_dof_layout(self, mesh: MeshModel) -> None:
        signature = (tuple(sorted(mesh.nodes)), tuple(
            (key, e.get('type', 'bar'), tuple(e['nodes'])) for key, e in mesh.elements.items()))
        if self.layout is None or signature != self._layout_signature:
            self.layout = DofLayout.from_mesh(mesh)
            self._layout_signature = signature
            self._node_dof_offsets = self.layout.node_offsets
        geometry_length = characteristic_length(mesh)
        self.characteristic_length = geometry_length or 1.0
        self.characteristic_length_source = (
            'geometry_span' if geometry_length is not None else 'point_model_fallback'
        )

    def _node_dof_start(self, node_id: int, stride: int) -> int:
        if self._node_dof_offsets is None:
            # 行列だけを直接渡す既存APIでは、1始まりの連続節点を仮定する。
            return (node_id - 1) * stride
        if node_id not in self._node_dof_offsets:
            raise ValueError(f'Unknown node {node_id}')
        return self._node_dof_offsets[node_id]
        
    def create_stiffness_matrix(self, mesh, material, elements):
        self._set_dof_layout(mesh)
        self.assembled_stiffness_correction = None
        if any(hasattr(e, 'get_stiffness_matrix_parts') for e in elements.values()):
            return self._assemble_stiffness_parts(mesh, elements)
        self.assembled_stiffness = self.layout.assemble_matrix(
            elements, lambda element, indices: element.get_stiffness_matrix())
        return self.assembled_stiffness

    def _assemble_stiffness_parts(self, mesh, elements):
        """Preserve both element-integration and assembly rounding tails."""
        from math import fsum
        self._set_dof_layout(mesh)
        rows = [{} for _ in range(self.layout.size)]
        for _, element, indices in self.layout.elements(elements):
            if hasattr(element, 'get_stiffness_matrix_parts'):
                high, low = element.get_stiffness_matrix_parts()
            else:
                high = element.get_stiffness_matrix(); low = np.zeros_like(high)
            for i, dof_i in enumerate(indices):
                row = rows[dof_i]
                for j, dof_j in enumerate(indices):
                    a, b = float(high[i,j]), float(low[i,j])
                    if not a and not b: continue
                    old, tail = row.get(dof_j, (0., 0.))
                    total = fsum([old, tail, a, b])
                    row[dof_j] = (total, fsum([old, tail, a, b, -total]))
        indices, highs, lows, offsets = [], [], [], [0]
        for row in rows:
            for j, (high, low) in sorted(row.items()):
                indices.append(j); highs.append(high); lows.append(low)
            offsets.append(len(indices))
        shape = (len(rows), len(rows))
        self.assembled_stiffness = csr_matrix((highs, indices, offsets), shape=shape)
        self.assembled_stiffness_correction = csr_matrix((lows, indices, offsets), shape=shape)
        return self.assembled_stiffness
        
    def create_mass_matrix(self, mesh, material, elements):
        self._set_dof_layout(mesh)
        self.assembled_mass = self.layout.assemble_matrix(
            elements, lambda element, indices: element.get_mass_matrix())
        return self.assembled_mass

    def assemble_load_vector(self, mesh: MeshModel, boundary: BoundaryCondition,
                           elements: Dict[int, Any]) -> np.ndarray:
        """荷重ベクトルを組み立て（すべての荷重を入力値のまま足したもの）

        区別ごとのベクトルは self.load_pattern に残す。

        Args:
            mesh: メッシュデータ
            boundary: 境界条件
            elements: 要素オブジェクトの辞書

        Returns:
            荷重ベクトル
        """
        return self.assemble_load_pattern(mesh, boundary, elements).reference

    def assemble_load_pattern(self, mesh: MeshModel, boundary: BoundaryCondition,
                              elements: Dict[int, Any]) -> LoadPattern:
        """固定・増分・限定増分の区別ごとに荷重ベクトルを組み立てる"""
        self.clear_spatial_load_state()
        self.load_vector = None
        self.load_pattern = None
        self._set_dof_layout(mesh)
        contribution = (compile_spatial_loads(boundary.spatial_loads, mesh)
                        if boundary.spatial_loads.loads else None)
        shell_direct = {}
        max_dof_per_node = self.layout.stride
        n_dof = self.layout.size
        vectors = {INCREMENTAL: np.zeros(n_dof)}

        # 節点荷重の適用
        for kind, loads in boundary.load_sets.items():
            vector = vectors.setdefault(kind, np.zeros(n_dof))
            for node_id, load in loads.items():
                base_dof = self._node_dof_start(node_id, max_dof_per_node)
                for i in range(min(max_dof_per_node, len(load.forces))):
                    if base_dof + i < n_dof:  # 範囲チェック追加
                        vector[base_dof + i] += load.forces[i]

        # 分布荷重の適用
        for _, element, indices in self.layout.elements(elements):
            if hasattr(element, 'get_member_load_vectors'):
                for kind, load in element.get_member_load_vectors().items():
                    self.layout.add_vector(vectors.setdefault(kind, np.zeros(n_dof)), indices, load)
            elif hasattr(element, 'get_member_load_vector'):
                self.layout.add_vector(vectors[INCREMENTAL], indices, element.get_member_load_vector())

        # 以下の荷重は区別を持たず、増分荷重として扱う
        F = vectors[INCREMENTAL]

        for dist_load in boundary.distributed_loads:
            elem_id = dist_load.element_id
            if elem_id not in elements:
                continue
                
            element = elements[elem_id]
            
            # 要素の等価節点荷重を計算
            equiv_loads = element.get_equivalent_nodal_loads(
                dist_load.load_type, dist_load.values, dist_load.face
            )
            
            indices = self.layout.element_dofs(elem_id, element)
            self.layout.add_vector(F, indices[:len(equiv_loads)], equiv_loads)
            if contribution is not None and hasattr(element, 'calculate_shell_results'):
                shell_direct.setdefault(elem_id, np.zeros(element.get_matrix_size()))[:] += equiv_loads
        
        # 面圧荷重の適用（V0のloadVector関数の面圧処理を移植）
        for pressure in boundary.pressures:
            elem_id = pressure.element_id
            if elem_id not in elements:
                continue
                
            element = elements[elem_id]
            
            # 要素の等価節点荷重を計算（面圧専用）
            equiv_loads = element.get_equivalent_nodal_loads(
                'pressure', [pressure.pressure], pressure.face
            )
            
            indices = self.layout.element_dofs(elem_id, element)
            self.layout.add_vector(F, indices[:len(equiv_loads)], equiv_loads)
            if contribution is not None and hasattr(element, 'calculate_shell_results'):
                shell_direct.setdefault(elem_id, np.zeros(element.get_matrix_size()))[:] += equiv_loads

        if contribution is not None:
            F += contribution.dof_loads
            for elem_id, values in contribution.shell_load_vectors().items():
                shell_direct.setdefault(elem_id, np.zeros_like(values))[:] += values
        if not all(np.all(np.isfinite(vector)) for vector in vectors.values()):
            raise ValueError('Loads must be finite')
        self.spatial_load_contribution = contribution
        self._shell_direct_loads = shell_direct
        self.load_pattern = LoadPattern(vectors)
        self.load_vector = self.load_pattern.reference
        return self.load_pattern
        
    @staticmethod
    def _get_max_dof_per_node(mesh: MeshModel) -> int:
        return DofLayout.stride_for(mesh)

    def _get_boundary_dofs(self, boundary: BoundaryCondition, n_dof: int,
                           max_dof_per_node: int) -> Tuple[Dict[int, float], Dict[int, float]]:
        """Compatibility view of the normalized boundary conditions."""
        resolved = self._resolve_boundary_dofs(boundary, n_dof, max_dof_per_node)
        return resolved.prescribed, resolved.springs

    def _resolve_boundary_dofs(self, boundary, n_dof, stride):
        # Resolve per operation/step so reanalysis and edited boundaries cannot
        # reuse a stale mapping. Iterations keep their own normalized snapshot.
        resolved = BoundaryDofs.from_boundary(
            boundary, n_dof, stride, lambda node: self._node_dof_start(node, stride))
        if self.support_springs is not None:
            if self.support_springs.definitions != resolved.nonlinear_springs:
                raise ValueError('Nonlinear support definitions changed during analysis; rerun solve')
            resolved.support_state = self.support_springs
        return resolved

    def _prepare_displacement_control(self, control, boundary, n_steps):
        """Validate public displacement-control input and resolve its global DOF."""
        if not isinstance(control, dict):
            raise ValueError('displacement_control must be an object')
        allowed = {'node', 'dof', 'target', 'targets'}
        if set(control) - allowed or 'node' not in control or 'dof' not in control:
            raise ValueError('displacement_control requires node, dof, and target(s)')
        node = control['node']
        if isinstance(node, (bool, np.bool_)) or not isinstance(node, (int, np.integer)):
            raise ValueError('displacement_control node must be an integer')
        if node not in self.layout.node_offsets:
            raise ValueError(f'Unknown displacement-control node {node}')
        names = {'dx': 0, 'dy': 1, 'dz': 2, 'rx': 3, 'ry': 4, 'rz': 5}
        name = control['dof']
        if not isinstance(name, str) or name not in names or names[name] >= self.layout.stride:
            raise ValueError(f'Invalid displacement-control DOF: {name}')
        dof = self.layout.node_offsets[node]+names[name]
        prescribed, _ = self._get_boundary_dofs(
            boundary, self.layout.size, self.layout.stride
        )
        if dof in prescribed:
            raise ValueError('Displacement-control DOF must not be restrained')
        if any(value != 0 for value in prescribed.values()):
            raise ValueError(
                'Displacement control currently requires zero prescribed support motion'
            )
        has_target, has_targets = 'target' in control, 'targets' in control
        if has_target == has_targets:
            raise ValueError('Specify exactly one of target or targets')
        if has_targets:
            try:
                targets = np.asarray(control['targets'], dtype=float)
            except (TypeError, ValueError) as error:
                raise ValueError('displacement-control targets must be numeric') from error
        else:
            target = control['target']
            if (isinstance(target, (bool, np.bool_)) or
                    not isinstance(target, (int, float, np.integer, np.floating))):
                raise ValueError('displacement-control target must be numeric')
            targets = np.linspace(float(target)/n_steps, float(target), n_steps)
        if targets.ndim != 1 or len(targets) == 0 or not np.all(np.isfinite(targets)):
            raise ValueError('displacement-control targets must be a nonempty finite sequence')
        return {
            'node': int(node), 'dof_name': name, 'dof': dof,
            'targets': targets.astype(float, copy=False),
        }

    def apply_boundary_conditions(self, K: csr_matrix, F: np.ndarray,
                                  boundary: BoundaryCondition, max_dof_per_node: int = 6,
                                  current_displacement: Optional[np.ndarray] = None,
                                  load_factor: float = 1.0,
                                  penalty: bool = False) -> Tuple[csr_matrix, np.ndarray]:
        """固定DOFを対称消去する。Newtonでは目標変位との差を拘束する。

        Fは構造要素の外力−内力（静解析では外力）。支持ばねの内力はここで引く。
        penalty=Trueは旧APIとの互換用。現在の固有値解析は自由DOFへ縮約する。
        """
        resolved = self._resolve_boundary_dofs(boundary, len(F), max_dof_per_node)
        u = np.zeros(len(F)) if current_displacement is None else current_displacement
        scale = max(float(np.max(np.abs(K.diagonal()))), 1.0)*1e15 if penalty else None
        return resolved.constrain(
            resolved.add_spring_stiffness(K, u), resolved.residual(F, u), u,
            load_factor=load_factor, penalty_scale=scale)
        
    def solve_linear_system(self, K, F):
        """Legacy stateful boundary around pure compensated direct algebra."""
        self.displacement, self.displacement_correction = solve_direct_system(K, F)
        return self.displacement

    _solve_newton_system = staticmethod(solve_newton_system)

    def _newton_raphson_iteration(self, *args, **kwargs):
        return newton_iteration(self, *args, **kwargs)

    def _displacement_control_iteration(self, *args, **kwargs):
        return displacement_control_iteration(self, *args, **kwargs)

    def solve_nonlinear(self, mesh, material, boundary, elements,
                        n_steps=DEFAULT_N_STEPS, max_iter=DEFAULT_MAX_ITER,
                        tol=DEFAULT_TOL, callback=None, load_factors=None,
                        displacement_control=None):
        """Legacy method name; all analysis work belongs to solve()."""
        from .solver_results import legacy_nonlinear_result
        result = self.solve(mesh, material, boundary, elements,
                            analysis_type='material_nonlinear', n_steps=n_steps,
                            max_iter=max_iter, tol=tol, callback=callback,
                            load_factors=load_factors,
                            displacement_control=displacement_control)
        return legacy_nonlinear_result(result, self.layout.stride)

    def solve(self, mesh: MeshModel, material: Material, boundary: BoundaryCondition,
              elements: Dict[int, Any], *, analysis_type='static',
              n_steps=DEFAULT_N_STEPS, max_iter=DEFAULT_MAX_ITER,
              tol=DEFAULT_TOL, callback=None, load_factors=None,
              displacement_control=None):
        """Solve with per-call spatial compilation and discard it on failure."""
        try:
            return self._solve(mesh, material, boundary, elements, analysis_type=analysis_type,
                               n_steps=n_steps, max_iter=max_iter, tol=tol, callback=callback,
                               load_factors=load_factors, displacement_control=displacement_control)
        except Exception:
            self.clear_spatial_load_state()
            raise

    def _solve(self, mesh: MeshModel, material: Material, boundary: BoundaryCondition,
               elements: Dict[int, Any], *, analysis_type='static',
               n_steps=DEFAULT_N_STEPS, max_iter=DEFAULT_MAX_ITER,
               tol=DEFAULT_TOL, callback=None, load_factors=None,
               displacement_control=None):
        """Shared static flow; analysis type selects material law and equilibrium.

        The original four positional arguments select reference-elastic static
        analysis. Nonlinear controls are independent of the number of steps.
        Modal analysis continues to use eigenvalue_analysis(). Each call starts
        a new analysis; elements implementing reset_states start fresh history.
        """
        self._reset_analysis_state()
        self._validate_spatial_analysis(boundary, analysis_type)
        from .nonlinear.support_springs import validate_support_analysis
        validate_support_analysis(boundary, analysis_type)
        if analysis_type not in ('static', 'material_nonlinear'):
            raise UnsupportedAnalysisError(
                f'Unknown static analysis type: {analysis_type}',
                analysis_type=analysis_type,
            )
        nonlinear = analysis_type == 'material_nonlinear'
        if nonlinear:
            for name, value in [('n_steps', n_steps), ('max_iter', max_iter)]:
                if isinstance(value, (bool, np.bool_)) or not isinstance(value, (int, np.integer)) or value <= 0:
                    raise ValueError(f'{name} must be a positive integer')
            if not np.isfinite(tol) or tol <= 0:
                raise ValueError('tol must be finite and positive')
            if displacement_control is not None and load_factors is not None:
                raise ValueError('load_factors and displacement_control are mutually exclusive')
            if displacement_control is None:
                factors = (np.arange(1, n_steps + 1, dtype=float) / n_steps
                           if load_factors is None else np.asarray(load_factors, dtype=float))
                if factors.ndim != 1 or len(factors) == 0 or not np.all(np.isfinite(factors)):
                    raise ValueError('load_factors must be a nonempty finite sequence')
            else:
                factors = None
        else:
            if load_factors is not None or displacement_control is not None:
                raise ValueError('nonlinear controls are only supported for material_nonlinear')
            factors = [1.0]

        self._set_dof_layout(mesh)
        stride = self.layout.stride
        resolved = self._resolve_boundary_dofs(boundary, self.layout.size, stride)
        self.support_springs = resolved.support_state
        for element in elements.values():
            if hasattr(element, 'reset_states'):
                element.reset_states()
            if hasattr(element, 'load_factor'):
                element.load_factor = 0.0 if nonlinear else 1.0
        if boundary.spatial_loads.loads:
            # Geometry, quadrature and conservation must pass before K assembly.
            pattern = self.assemble_load_pattern(mesh, boundary, elements)
            self.create_stiffness_matrix(mesh, material, elements)
        else:
            if not nonlinear:
                self.create_stiffness_matrix(mesh, material, elements)
            pattern = self.assemble_load_pattern(mesh, boundary, elements)
        control = None
        if displacement_control is not None:
            control = self._prepare_displacement_control(
                displacement_control, boundary, n_steps
            )
            prescribed, _ = self._get_boundary_dofs(boundary, self.layout.size, stride)
            free = np.array([i for i in range(self.layout.size) if i not in prescribed])
            if np.linalg.norm(pattern.varying[free]) == 0:
                raise ValueError('Displacement control requires a nonzero free-DOF load pattern')
        u = np.zeros(self.layout.size)
        self.displacement = u.copy()

        schedule = ([(float(factor), None) for factor in factors] if control is None
                    else [(None, float(target)) for target in control['targets']])
        if control is not None and pattern.has_fixed:
            # The first state carries the fixed loads only; the control
            # targets are displacements measured from that state.
            schedule.insert(0, (0.0, None))
        control_origin = 0.0
        for step, (factor, target) in enumerate(schedule, 1):
            if target is not None:
                factor = self.load_factor
                target += control_origin
            force = pattern.at(factor)
            committed_u = u.copy()
            committed_force = deepcopy(self._last_internal_force)
            previous_factors = {key: e.load_factor for key, e in elements.items()
                                if hasattr(e, 'load_factor')}
            for key in previous_factors:
                elements[key].load_factor = factor
            history_start = len(self.convergence_history)
            try:
                if nonlinear:
                    if target is None:
                        converged, u, iterations = self._newton_raphson_iteration(
                            mesh, material, boundary, elements, u, force,
                            max_iter, tol, stride, factor)
                    else:
                        converged, u, iterations, factor = self._displacement_control_iteration(
                            mesh, material, boundary, elements, u, pattern,
                            control['dof'], target, factor, max_iter, tol, stride)
                        force = pattern.at(factor)
                    if not converged:
                        raise NonlinearConvergenceError(step, factor, committed_u)
                    support_trial = (self.support_springs.evaluate(u)
                                     if self.support_springs is not None else None)
                    solution = (u, self._last_internal_force, None, iterations)
                else:
                    solution = direct_step(self, mesh, boundary, elements, force, factor)
                    u = solution[0]
            except Exception as error:
                if hasattr(error, 'details'):
                    error.details.update(step=step, load_factor=float(factor))
                    if self.support_springs is not None:
                        try:
                            states = self.support_springs.evaluate(u)
                            error.details['support_response'] = self.support_springs.snapshot(states)
                        except ValueError:
                            error.details['support_response'] = self.support_springs.snapshot()
                if self.support_springs is not None:
                    self.support_springs.rollback()
                self.displacement = committed_u.copy()
                self._last_internal_force = committed_force
                self.displacement_correction = None
                for key, element in elements.items():
                    if nonlinear and hasattr(element, 'rollback_state'):
                        element.rollback_state()
                    if key in previous_factors:
                        element.load_factor = previous_factors[key]
                raise
            finally:
                for record in self.convergence_history[history_start:]:
                    record.update(step=step, **{'lambda': factor})

            if nonlinear:
                if self.support_springs is not None:
                    self.support_springs.commit(support_trial)
                for key in previous_factors:
                    elements[key].load_factor = factor
                for element in elements.values():
                    if hasattr(element, 'commit_state'):
                        element.commit_state()
            self.displacement = u.copy()
            self._last_internal_force = solution[1].copy()
            self.displacement_correction = None if solution[2] is None else solution[2].copy()
            self.load_factor = factor
            accepted = snapshot(self, mesh, boundary, elements, solution, force, step, factor, nonlinear)
            if control is not None:
                accepted.update(
                    control_mode='displacement',
                    control_node=control['node'],
                    control_dof=control['dof_name'],
                    control_displacement=float(u[control['dof']]),
                )
                if target is None:
                    control_origin = float(u[control['dof']])
            self.step_results.append(accepted)
            if callback is not None:
                callback(deepcopy(accepted) if not nonlinear else
                         self._legacy_callback_snapshot(accepted))
        return final_result(self, nonlinear)

    def _legacy_callback_snapshot(self, accepted):
        from .solver_results import legacy_nonlinear_result
        # Callback payloads retain the old 3DOF schema without exposing storage.
        result = deepcopy(accepted)
        legacy_nonlinear_result(dict(node_displacements={}, step_results=[result]), self.layout.stride)
        return result

    def eigenvalue_analysis(self, mesh: MeshModel, material: Material,
                          boundary: BoundaryCondition, elements: Dict[int, Any],
                          n_modes: int = 10) -> Dict[str, Any]:
        """Solve the constrained generalized eigenproblem for requested low modes."""
        self._reset_analysis_state()
        self._validate_spatial_analysis(boundary, 'modal')
        from .nonlinear.support_springs import validate_support_analysis
        validate_support_analysis(boundary, 'modal')
        if (isinstance(n_modes, (bool, np.bool_)) or
                not isinstance(n_modes, (int, np.integer)) or n_modes <= 0):
            raise ValueError('n_modes must be a positive integer')

        K = self.create_stiffness_matrix(mesh, material, elements).tocsr()
        M = self.create_mass_matrix(mesh, material, elements).tocsr()
        if K.shape != M.shape or K.shape[0] == 0:
            raise ValueError('Stiffness and mass matrices must have the same nonzero shape')
        if not np.isfinite(K.data).all() or not np.isfinite(M.data).all():
            raise ValueError('Stiffness and mass matrices must be finite')

        resolved = self._resolve_boundary_dofs(
            boundary, self.layout.size, self.layout.stride)
        supported_stiffness = resolved.add_spring_stiffness(K)
        free_dofs = resolved.free
        if len(free_dofs) == 0:
            raise ValueError('Modal analysis has no free DOFs after applying restraints')

        reduced_stiffness = supported_stiffness[free_dofs][:, free_dofs].tocsr()
        reduced_mass = M[free_dofs][:, free_dofs].tocsr()
        mass_diagonal = np.abs(reduced_mass.diagonal())
        mass_scale = float(np.max(mass_diagonal, initial=0.0))
        if mass_scale == 0:
            raise ValueError('Modal analysis has no mass on any free DOF')
        mass_tolerance = np.finfo(float).eps * max(1, len(free_dofs)) * mass_scale
        dynamic = mass_diagonal > mass_tolerance
        if not np.all(dynamic):
            massless_rows = reduced_mass[~dynamic]
            if massless_rows.nnz and np.max(np.abs(massless_rows.data)) > mass_tolerance:
                raise ValueError('Mass matrix couples nominally massless free DOFs')
            raise ValueError(
                'Modal analysis contains massless free DOFs; restrain them or provide inertia')

        available = len(free_dofs)
        if n_modes > available:
            raise ValueError(
                f'Modal analysis requested {n_modes} modes but only {available} are available')

        if n_modes == available:
            try:
                eigenvalues, reduced_vectors = eigh(
                    reduced_stiffness.toarray(), reduced_mass.toarray(), check_finite=True)
            except np.linalg.LinAlgError as exc:
                raise ValueError('Reduced mass matrix must be positive definite') from exc
        else:
            try:
                eigenvalues, reduced_vectors = eigsh(
                    reduced_stiffness, k=n_modes, M=reduced_mass,
                    which='SA', maxiter=3000, tol=1e-9)
            except ArpackNoConvergence:
                try:
                    eigenvalues, reduced_vectors = eigsh(
                        reduced_stiffness, k=n_modes, M=reduced_mass,
                        sigma=0.0, which='LM', maxiter=5000, tol=1e-9)
                except (ArpackNoConvergence, RuntimeError, ValueError) as retry_error:
                    raise ModalConvergenceError(
                        f'Modal analysis did not converge for {n_modes} requested modes',
                        analysis_type='modal', requested_modes=n_modes) \
                        from retry_error

        order = np.argsort(eigenvalues)[:n_modes]
        eigenvalues = np.asarray(eigenvalues[order], dtype=float)
        reduced_vectors = np.asarray(reduced_vectors[:, order], dtype=float)

        eigenvalue_scale = (
            float(np.max(np.abs(reduced_stiffness.diagonal()), initial=0.0)) /
            mass_scale)
        zero_tolerance = max(
            np.finfo(float).eps * available * eigenvalue_scale,
            1e-14 * eigenvalue_scale)
        if np.any(eigenvalues < -zero_tolerance):
            value = float(np.min(eigenvalues))
            raise ValueError(
                f'Modal analysis found a significant negative eigenvalue: {value:.6g}')
        eigenvalues[np.abs(eigenvalues) <= zero_tolerance] = 0.0

        for mode in range(n_modes):
            vector = reduced_vectors[:, mode]
            mass_norm_squared = float(vector @ (reduced_mass @ vector))
            if not np.isfinite(mass_norm_squared) or mass_norm_squared <= 0:
                raise ValueError('Eigenvector has a nonpositive mass norm')
            reduced_vectors[:, mode] = vector / np.sqrt(mass_norm_squared)

        eigenvectors = np.zeros((self.layout.size, n_modes))
        eigenvectors[free_dofs, :] = reduced_vectors
        for mode in range(n_modes):
            pivot = int(np.argmax(np.abs(eigenvectors[:, mode])))
            if eigenvectors[pivot, mode] < 0:
                eigenvectors[:, mode] *= -1
                reduced_vectors[:, mode] *= -1

        residuals = []
        tiny = np.finfo(float).tiny
        for mode, eigenvalue in enumerate(eigenvalues):
            vector = reduced_vectors[:, mode]
            stiffness_action = reduced_stiffness @ vector
            mass_action = reduced_mass @ vector
            denominator = (
                np.linalg.norm(stiffness_action) +
                abs(eigenvalue) * np.linalg.norm(mass_action))
            residuals.append(float(
                np.linalg.norm(stiffness_action - eigenvalue * mass_action) /
                max(denominator, tiny)))
        gram = reduced_vectors.T @ (reduced_mass @ reduced_vectors)
        orthogonality_error = float(np.max(np.abs(gram - np.eye(n_modes))))

        omega = np.sqrt(eigenvalues)
        frequency = omega / (2 * np.pi)
        period = np.full_like(frequency, np.inf)
        positive = eigenvalues > zero_tolerance
        period[positive] = 1.0 / frequency[positive]

        self.eigenvalues = eigenvalues
        self.eigenvectors = eigenvectors
        return {
            'n_modes': n_modes,
            'eigenvalues': eigenvalues,
            'eigenvectors': eigenvectors,
            'frequencies': frequency,
            'periods': period,
            'eigenpair_residuals': residuals,
            'mass_orthogonality_error': orthogonality_error,
            'modes': self._format_eigenmodes(eigenvectors, mesh)
        }

    def _format_node_displacements(self, u, mesh):
        """Canonical six-component output; legacy projections live at API edges."""
        self._set_dof_layout(mesh)
        formatted = self.layout.format_displacements(u)
        return {node: formatted[node] for node in mesh.nodes}

    def _calculate_reaction_forces(self, K, u, F, boundary, max_dof_per_node=6):
        return self._format_reactions(K @ u - F, boundary, max_dof_per_node)

    def _format_reactions(self, reaction: np.ndarray, boundary: BoundaryCondition,
                          stride: int) -> Dict[int, Dict[str, float]]:
        """構造要素内力−外力。ばね支持では -k*u と等しい。"""
        names = ('fx', 'fy', 'fz', 'mx', 'my', 'mz')
        result = {}
        for node_id, restraint in boundary.restraints.items():
            if node_id in getattr(boundary, 'auxiliary_restraint_nodes', set()):
                continue
            values = {names[i]: float(reaction[self._node_dof_start(node_id, stride) + i])
                      for i, fixed in enumerate(restraint.dof_restraints[:stride]) if fixed}
            if values:
                result[node_id] = values
        supports_by_node = {node: dict(values) for node, values in boundary.spring_supports.items()}
        for node, values in boundary.nonlinear_spring_supports.items():
            supports_by_node.setdefault(node, {}).update(values)
        for node_id, supports in supports_by_node.items():
            values = result.setdefault(node_id, {})
            for direction in supports:
                i = SPRING_DIRECTIONS[direction]
                values[names[i]] = float(reaction[self._node_dof_start(node_id, stride) + i])
        return result

    def _element_curvatures(self, elements, u, stride):
        """Snapshot accepted total midpoint curvature of nonlinear beams only."""
        result = {}
        for elem_id, element in elements.items():
            if hasattr(element, 'calculate_curvature') and element.is_nonlinear():
                indices = self.layout.element_dofs(elem_id, element)
                result[elem_id] = element.calculate_curvature(u[indices])
        return result

    def _element_end_forces(self, elements, u, stride):
        """Snapshot accepted end forces after commit, never advance constitutive state."""
        result = {}
        for elem_id, element in elements.items():
            if hasattr(element, 'calculate_forces'):
                indices = self.layout.element_dofs(elem_id, element)
                result[elem_id] = element.calculate_forces(u[indices])
        return result

    def _assemble_internal_forces(self, mesh, elements, u, max_dof_per_node):
        self._set_dof_layout(mesh)
        force = np.zeros(self.layout.size)
        for _, element, indices in self.layout.elements(elements):
            local = u[indices]
            values = (element.get_internal_force(local) if hasattr(element, 'get_internal_force')
                      else element.get_stiffness_matrix() @ local)
            self.layout.add_vector(force, indices, values)
        return force

    def _assemble_tangent_stiffness(self, mesh, material, elements, u, max_dof_per_node):
        self._set_dof_layout(mesh)
        def evaluate(element, indices):
            return (element.get_tangent_stiffness_matrix(u[indices])
                    if hasattr(element, 'get_tangent_stiffness_matrix')
                    else element.get_stiffness_matrix())
        return self.layout.assemble_matrix(elements, evaluate)

    def _spring_force(self, u, springs):
        return spring_force(u, springs)

    def _apply_bc_to_residual(self, R, boundary, max_dof_per_node):
        return self._resolve_boundary_dofs(boundary, len(R), max_dof_per_node).project(R)

    def _equilibrium_residual(self, R, u, boundary, stride):
        resolved = self._resolve_boundary_dofs(boundary, len(R), stride)
        return resolved.project(resolved.residual(R, u))

    def _format_eigenmodes(self, eigenvectors: np.ndarray, mesh: MeshModel) -> List[Dict[int, Dict[str, float]]]:
        """固有モードを整形
        
        Args:
            eigenvectors: 固有ベクトル行列
            mesh: メッシュデータ
            
        Returns:
            各モードの節点変位の辞書のリスト
        """
        modes = []
        n_modes = eigenvectors.shape[1]
        
        for mode_idx in range(n_modes):
            mode_vector = eigenvectors[:, mode_idx]
            mode_displacements = self._format_node_displacements(mode_vector, mesh)
            modes.append(mode_displacements)
            
        return modes
