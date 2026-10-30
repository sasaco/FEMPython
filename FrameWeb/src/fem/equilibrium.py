"""Equilibrium strategies; numerical policies are deliberately distinct.

Direct algebra uses diagonal scaling and compensated refinement; Newton algebra
uses row-maximum scaling, a rank check and the existing increment residual bound.
The algebra functions return values without mutating accepted solver state.
"""
import logging
import warnings
from typing import Any, Dict
import numpy as np
from scipy.sparse import bmat, csr_matrix, diags
from scipy.sparse.linalg import splu, MatrixRankWarning
from .mesh import MeshModel
from .material import Material
from .boundary_condition import BoundaryCondition
from .load_kinds import LoadPattern
from .precision import sparse_product, add_correction
from .convergence import (
    evaluate_equilibrium,
    generalized_displacement_norm,
    generalized_force_norm,
)
from .diagnostics import (
    InputValidationError,
    NumericalConditionError,
    StructuralMechanismError,
)

logger = logging.getLogger(__name__)

class NonlinearConvergenceError(RuntimeError):
    """荷重ステップ失敗。displacementは最後に収束した変位のコピー。"""

    error_code = 'nonlinear_nonconvergence'
    error_category = 'convergence'
    http_status = 422

    def __init__(self, step: int, load_factor: float, displacement: np.ndarray):
        super().__init__(f'Nonlinear analysis did not converge at step {step} '
                         f'(load factor {load_factor:g})')
        self.step = step
        self.load_factor = load_factor
        self.displacement = displacement.copy()
        self.details = {'step': step, 'load_factor': load_factor}

def solve_direct_system(K: csr_matrix, F: np.ndarray, *, precision_floor=0.) -> tuple[np.ndarray, np.ndarray]:
    """Equilibrated direct solve; a mechanism is an error even at zero load."""
    from scipy.sparse.linalg import splu
    K = K.tocsr()
    if not np.all(np.isfinite(K.data)) or not np.all(np.isfinite(F)):
        raise InputValidationError('Linear system must be finite')
    diagonal = np.abs(K.diagonal())
    if np.any(diagonal <= 0):
        raise StructuralMechanismError(
            'Singular stiffness matrix: zero diagonal',
            matrix_dofs=np.flatnonzero(diagonal <= 0).tolist(),
        )
    scale = 1/np.sqrt(diagonal)
    D = diags(scale)
    equilibrated = (D@K@D).tocsc()
    try:
        lu = splu(equilibrated)
    except RuntimeError as error:
        raise StructuralMechanismError('Singular stiffness matrix') from error
    pivots = np.abs(lu.U.diagonal())
    if np.min(pivots) <= np.finfo(float).eps * K.shape[0] * max(1., np.max(pivots)):
        raise NumericalConditionError(
            'Singular stiffness matrix: numerical rank deficiency'
        )
    if precision_floor and np.min(pivots) <= precision_floor*np.max(pivots):
        raise NumericalConditionError('Linear frame requires high precision')
    u = scale*lu.solve(scale*F)
    # Keep the displacement's low part and product rounding errors during
    # refinement. Ordinary K*u cannot resolve small support reactions.
    low = np.zeros_like(u)
    for _ in range(4):
        residual = F-sparse_product(K, u, low)
        u, low = add_correction(u, low, scale*lu.solve(scale*residual))
    residual = sparse_product(K, u, low)-F
    bound = np.linalg.norm(np.abs(equilibrated)@np.abs(u/scale)+np.abs(scale*F), ord=np.inf)
    if not np.all(np.isfinite(u)) or np.linalg.norm(scale*residual, ord=np.inf) > 1e-10*max(bound, np.finfo(float).tiny):
        raise NumericalConditionError('Linear system failed equilibrium check')
    return u, low

def solve_newton_system(K: csr_matrix, R: np.ndarray) -> np.ndarray:
    """対称スケーリング後に直接解法で解く。人工剛性による正則化は行わない。"""
    if not np.all(np.isfinite(K.data)) or not np.all(np.isfinite(R)):
        raise InputValidationError('Non-finite Newton equation')
    row_scale = np.asarray(abs(K).max(axis=1).toarray()).ravel()
    if np.any(row_scale == 0):
        raise StructuralMechanismError(
            'Singular Newton stiffness: zero row',
            matrix_dofs=np.flatnonzero(row_scale == 0).tolist(),
        )
    scaling = 1.0 / np.sqrt(row_scale)
    D = diags(scaling)
    try:
        with warnings.catch_warnings():
            warnings.simplefilter('error', MatrixRankWarning)
            factorization = splu((D @ K @ D).tocsc())
            pivots = np.abs(factorization.U.diagonal())
            rank_floor = np.finfo(float).eps * K.shape[0] * np.max(pivots)
            if np.min(pivots) <= rank_floor:
                raise NumericalConditionError('Numerically singular Newton stiffness')
            scaled_solution = factorization.solve(scaling * R)
    except (MatrixRankWarning, RuntimeError) as error:
        raise StructuralMechanismError('Singular Newton stiffness') from error
    du = scaling * scaled_solution
    if not np.all(np.isfinite(du)):
        raise NumericalConditionError('Non-finite Newton increment')
    # Componentwise backward error: rounding in K @ du scales with |K| |du|,
    # which a stiff short element makes far larger than a small right side.
    error = np.linalg.norm(K @ du - R, ord=np.inf)
    bound = np.linalg.norm(abs(K) @ np.abs(du) + np.abs(R), ord=np.inf)
    if error > 1e-8 * max(bound, 1.0):
        raise NumericalConditionError('Newton equation residual exceeds tolerance')
    return du

def direct_step(self, mesh, boundary, elements, F, factor):
    self.precise_end_forces = None
    self.precise_reactions = None
    try:
        return _direct_step(self, mesh, boundary, elements, F, factor)
    except ValueError as error:
        from .elements.loaded_bar_element import LoadedBarElement
        if not all(isinstance(e, LoadedBarElement) for e in elements.values()):
            raise
        if str(error) not in ('Singular stiffness matrix: numerical rank deficiency',
                              'Singular stiffness matrix',
                              'Linear frame requires high precision',
                              'Linear frame failed constitutive equilibrium refinement'):
            raise
        from .linear_precision import solve_precise_frame
        from .axial_interpolation import generated_point_basis, interpolate_output
        K_mod,F_mod=self.apply_boundary_conditions(self.assembled_stiffness,F,boundary,self.layout.stride,load_factor=factor)
        empty=np.asarray(abs(K_mod).sum(axis=1)).ravel()==0
        absent=empty & (np.arange(len(F_mod))%self.layout.stride>=3) & (F_mod==0)
        basis,records=generated_point_basis(self,mesh,boundary,elements,F_mod,absent)
        u,low,internal,forces,reactions=solve_precise_frame(self,mesh,boundary,elements,F,factor,basis,absent)
        # Retain the constitutive actions evaluated before rounding displacement
        # to two floats. Recomputing K*u here would lose the recovered digits.
        self.precise_end_forces=forces
        self.precise_reactions=reactions
        self.interpolated_displacements=records
        interpolate_output(self,u,low,records)
        return u,internal,low,1


def _direct_step(self, mesh, boundary, elements, F, factor):
    """One structural solve, retaining the existing compensated refinements."""
    K = self.assembled_stiffness
    from .elements.loaded_bar_element import LoadedBarElement
    # A conservative precision selector, not a second definition of singularity.
    # Reserve ten relative digits in the scaled factorization. The pivot ratio
    # is only an inexpensive warning estimate; high precision still verifies
    # rank and equilibrium independently, without adding stiffness.
    precision_floor = np.finfo(float).eps / 1e-10 if all(
        isinstance(e, LoadedBarElement) for e in elements.values()) else 0.
    if precision_floor:
        for element in elements.values():
            mat = element.material.materials[element.material_id]
            p = element.bar_param
            rigidities = (mat.E*p.area, mat.E*p.Iz, mat.E*p.Iy, mat.G*p.J)
            for mode, (spring, rigidity) in enumerate(zip(element.foundation, rigidities)):
                power = 4 if mode in (1, 2) else 2
                if spring and rigidity and spring*element.length**power/rigidity < precision_floor:
                    # A local foundation term can disappear even if other
                    # members keep the assembled system well conditioned.
                    raise ValueError('Linear frame requires high precision')
    # 境界条件の適用
    stride = self.layout.stride
    K_mod, F_mod = self.apply_boundary_conditions(K, F, boundary, stride, load_factor=factor)

    # 線形方程式を解く
    # A rotation released by every incident beam is absent from the model,
    # not a structural mechanism. Eliminate only exactly empty, unloaded
    # rotational rows; free translations and coupled rigid modes still fail.
    empty = np.asarray(np.abs(K_mod).sum(axis=1)).ravel() == 0
    absent = empty & (np.arange(len(F_mod)) % stride >= 3) & (F_mod == 0)
    active = np.flatnonzero(~absent)
    from .axial_interpolation import generated_point_basis, interpolate_output
    basis, interpolated = generated_point_basis(self, mesh, boundary, elements, F_mod, absent)
    self.interpolated_displacements = interpolated
    if basis is not None:
        reduced, reduced_low = solve_direct_system(basis.T@K_mod@basis, basis.T@F_mod, precision_floor=precision_floor)
        u, low = basis@reduced, basis@reduced_low
    elif np.any(absent):
        u = np.zeros(len(F_mod))
        u[active], active_low = solve_direct_system(K_mod[active][:, active], F_mod[active], precision_floor=precision_floor)
        low = np.zeros_like(u)
        low[active] = active_low
    else:
        u, low = solve_direct_system(K_mod, F_mod, precision_floor=precision_floor)

    correction, internal = _refine_beam_equilibrium(self, mesh, boundary, elements, K_mod, F, u, low, absent, basis)
    if precision_floor:
        for _,element,indices in self.layout.elements(elements):
            k,load=element._local_system()
            transform=element.get_transformation_matrix(12)
            magnitude=np.abs(k)@(np.abs(transform@u[indices])+np.abs(transform@correction[indices]))
            magnitude+=np.abs(element.load_factor*load)
            end=element.calculate_forces(u[indices],displacement_correction=correction[indices])
            action=np.r_[end['i_end'],end['j_end']]
            if np.any(np.finfo(float).eps*magnitude > 1e-12+1e-10*np.abs(action)):
                # Small actions formed by cancellation deserve extra digits
                # even when the assembled matrix itself is well conditioned.
                raise ValueError('Linear frame requires high precision')
    interpolate_output(self, u, correction, interpolated)

    return u, internal, correction, 1

def _refine_beam_equilibrium(self, mesh, boundary, elements, constrained_k, loads, u, low, absent, basis=None):
    """Refine linear frames with element forces and two-part displacements.

    The assembled matrix is only the correction operator. Its large
    diagonal terms can lose the short member's small deformation, so the
    residual is evaluated from the constitutive element kinematics.
    """
    from math import fsum
    from scipy.sparse.linalg import splu
    from .elements.loaded_bar_element import LoadedBarElement
    if not elements or not all(isinstance(e, LoadedBarElement) for e in elements.values()):
        if getattr(self, 'assembled_stiffness_correction', None) is not None:
            return _refine_stiffness_parts(self, boundary, constrained_k, loads, u, low, absent)
        return low, sparse_product(self.assembled_stiffness, u, low)
    resolved = self._resolve_boundary_dofs(boundary, len(u), 6)
    free = resolved.free[~absent[resolved.free]]
    if basis is not None:
        basis = basis.tolil()
        basis[list(resolved.prescribed), :] = 0.
        basis = basis.tocsr()
        basis = basis[:, np.flatnonzero(np.asarray(abs(basis).sum(axis=0)).ravel())]
    indices = {key: ix for key, _, ix in self.layout.elements(elements)}
    lu = None
    for iteration in range(16):
        terms = [[] for _ in u]
        force_scale = max(1., np.max(np.abs(loads)))
        for key, element in elements.items():
            ix = indices[key]
            values = element.get_internal_force(u[ix], displacement_correction=low[ix])
            force_scale = max(force_scale, np.max(np.abs(values)))
            for dof, value in zip(ix, values):
                terms[dof].append(value)
        internal = np.array([fsum(row) for row in terms])
        residual = resolved.residual(loads-internal, u, correction=low)
        projected = residual[free] if basis is None else basis.T@residual
        if not len(projected) or np.max(np.abs(projected)) <= 1e-11*force_scale:
            return low, internal
        if lu is None:
            k = constrained_k[free][:, free] if basis is None else basis.T@constrained_k@basis
            scale = 1/np.sqrt(np.abs(k.diagonal()))
            lu = splu((diags(scale)@k@diags(scale)).tocsc())
        delta = scale*lu.solve(scale*projected)
        # Error-free TwoSum retains the part rounded off by u += delta.
        if basis is None:
            u[free], low[free] = add_correction(u[free], low[free], delta)
        else:
            u[:], low[:] = add_correction(u, low, basis@delta)
    raise ValueError('Linear frame failed constitutive equilibrium refinement')

def _refine_stiffness_parts(self, boundary, constrained_k, loads, u, low, absent):
    from scipy.sparse.linalg import splu
    resolved = self._resolve_boundary_dofs(boundary, len(u), self.layout.stride)
    free = resolved.free[~absent[resolved.free]]
    lu = None
    for _ in range(16):
        internal = sparse_product(self.assembled_stiffness, u, low)
        internal += sparse_product(self.assembled_stiffness_correction, u, low)
        residual = resolved.residual(loads-internal, u, correction=low)
        # Loads or reactions on prescribed DOFs must not hide an error on
        # an independently loaded free DOF.
        scale_force = max(1., max(np.abs(loads[free]), default=0.))
        if not len(free) or np.max(np.abs(residual[free])) <= 1e-11*scale_force:
            return low, internal
        if lu is None:
            k = constrained_k[free][:, free]
            scale = 1/np.sqrt(np.abs(k.diagonal()))
            lu = splu((diags(scale)@k@diags(scale)).tocsc())
        u[free], low[free] = add_correction(u[free], low[free], scale*lu.solve(scale*residual[free]))
    raise ValueError('Quadratic solid failed compensated stiffness equilibrium refinement')

def newton_iteration(
    self,
    mesh: MeshModel,
    material: Material,
    boundary: BoundaryCondition,
    elements: Dict[int, Any],
    u_init: np.ndarray,
    F_ext: np.ndarray,
    max_iter: int,
    tol: float,
    max_dof_per_node: int,
    load_factor: float = 1.0
) -> tuple:
    """Newton-Raphson反復を実行

    Args:
        mesh: メッシュデータ
        material: 材料データ
        boundary: 境界条件
        elements: 要素辞書
        u_init: 初期変位ベクトル
        F_ext: 外力ベクトル
        max_iter: 最大反復回数
        tol: 収束許容差
        max_dof_per_node: 節点あたり最大自由度

    Returns:
        (converged, u, n_iter): 収束フラグ、変位、反復回数
    """
    u = u_init.copy()
    du = np.zeros_like(u)
    length = self.characteristic_length
    resolved = self._resolve_boundary_dofs(boundary, len(u), max_dof_per_node)
    # このステップの強制変位を先に満たす。反復ごとに加算しない。
    for dof, value in resolved.prescribed.items():
        u[dof] = load_factor * value

    for iteration in range(max_iter):
        # 内力ベクトルの組み立て
        F_int = self._assemble_internal_forces(mesh, elements, u, max_dof_per_node)

        # 残差ベクトル
        R = F_ext - F_int
        if not np.all(np.isfinite(R)):
            return False, u, iteration + 1
        equilibrium = evaluate_equilibrium(
            resolved, F_ext, F_int, u, length=length, reference_load=self.load_vector)
        R_norm, F_norm = equilibrium.residual_norm, equilibrium.residual_scale
        relative_residual = equilibrium.relative_residual

        # 変位増分ノルム（初回以降）
        if iteration > 0:
            du_norm = generalized_displacement_norm(
                du, stride=max_dof_per_node, length=length
            )
            u_norm = max(
                generalized_displacement_norm(
                    u, stride=max_dof_per_node, length=length
                ),
                1.0,
            )
            relative_du = du_norm / u_norm
        else:
            relative_du = float('inf')

        # 収束履歴を記録
        self.convergence_history.append({
            'iteration': iteration + 1,
            'residual_norm': R_norm,
            'residual_scale': F_norm,
            'relative_residual': relative_residual,
            'increment_norm': du_norm if iteration > 0 else None,
            'solution_norm': u_norm if iteration > 0 else None,
            'relative_du': relative_du if iteration > 0 else None
        })

        message = f"Iter {iteration + 1}: |R|/|F| = {relative_residual:.2e}"
        if iteration > 0:
            message += f", |du|/|u| = {relative_du:.2e}"
        logger.debug(message)

        # 収束判定
        if relative_residual < tol:
            if iteration == 0 or relative_du < tol:
                # Zero residual does not establish uniqueness in an unrestrained
                # system. Check the constrained tangent before accepting it.
                if iteration == 0 or resolved.nonlinear_springs:
                    tangent = self._assemble_tangent_stiffness(
                        mesh, material, elements, u, max_dof_per_node)
                    constrained, rhs = resolved.constrain(
                        resolved.add_spring_stiffness(tangent, u), equilibrium.residual, u,
                        load_factor=load_factor)
                    try:
                        self._solve_newton_system(constrained, rhs)
                    except ValueError:
                        return False, u, iteration + 1
                logger.debug("Converged at iteration %d", iteration + 1)
                self._last_internal_force = F_int.copy()
                return True, u, iteration + 1

        # 接線剛性行列の組み立て
        K_tan = self._assemble_tangent_stiffness(mesh, material, elements, u, max_dof_per_node)

        # 境界条件の適用
        K_mod, R_mod = resolved.constrain(
            resolved.add_spring_stiffness(K_tan, u, direction_hint=equilibrium.residual), equilibrium.residual, u,
            load_factor=load_factor)

        # 変位増分の計算
        try:
            du = self._solve_newton_system(K_mod, R_mod)
        except ValueError as e:
            logger.debug("Newton linear solve failed: %s", e)
            return False, u, iteration + 1

        # A reversal starts with the tangent stored at the committed point.
        # Its full Newton step can jump across both skeletons and oscillate.
        # Backtrack using free-DOF equilibrium, always from committed history.
        for backtrack in range(24):
            increment = du * (0.5 ** backtrack)
            candidate = u + increment
            candidate_force = self._assemble_internal_forces(
                mesh, elements, candidate, max_dof_per_node)
            candidate_norm = generalized_force_norm(
                resolved.project(resolved.residual(F_ext-candidate_force, candidate)),
                stride=max_dof_per_node,
                length=length,
            )
            if (candidate_norm < tol * F_norm or
                    candidate_norm <= (1 - 1e-4 * (0.5 ** backtrack)) * R_norm):
                u, du = candidate, increment
                break
        else:
            return False, u, iteration + 1

    # 最大反復数に到達
    logger.info("Newton iteration did not converge in %d iterations", max_iter)
    return False, u, max_iter


def displacement_control_iteration(
    self,
    mesh: MeshModel,
    material: Material,
    boundary: BoundaryCondition,
    elements: Dict[int, Any],
    u_init: np.ndarray,
    load_pattern: LoadPattern,
    control_dof: int,
    target: float,
    initial_load_factor: float,
    max_iter: int,
    tol: float,
    max_dof_per_node: int,
) -> tuple:
    """Solve equilibrium with one displacement constraint and unknown lambda.

    The Newton system is ``[K, -P; c.T, 0] [du, dlambda] = [R, g]`` with
    P = dF/dlambda of the load pattern (fixed loads do not vary; a limited
    load stops at its limit). It remains regular at a simple load limit point
    and permits a negative structural tangent. Snap-back still requires
    arc-length control.
    """
    u = u_init.copy()
    load_factor = float(initial_load_factor)
    length = self.characteristic_length
    resolved = self._resolve_boundary_dofs(
        boundary, len(u), max_dof_per_node
    )
    active = resolved.free
    if control_dof not in set(active):
        raise ValueError('Displacement control DOF must be free')
    control_column = int(np.flatnonzero(active == control_dof)[0])
    selector = np.zeros(len(active))
    selector[control_column] = 1.0

    def augmented_tangent(displacement, factor, direction_hint=None):
        tangent = self._assemble_tangent_stiffness(
            mesh, material, elements, displacement, max_dof_per_node)
        supported = resolved.add_spring_stiffness(tangent, displacement, direction_hint=direction_hint)
        rate = load_pattern.rate(factor)
        return bmat([
            [supported[active][:, active], csr_matrix(-rate[active, None])],
            [csr_matrix(selector[None, :]), csr_matrix((1, 1))],
        ], format='csr')

    du = np.zeros_like(u)
    dlambda = 0.0

    for iteration in range(max_iter):
        F_int = self._assemble_internal_forces(mesh, elements, u, max_dof_per_node)
        equilibrium = evaluate_equilibrium(
            resolved, load_pattern.at(load_factor), F_int, u, length=length,
            reference_load=load_pattern.reference, dof_indices=active)
        residual_free = equilibrium.residual[active]
        constraint = target-u[control_dof]
        residual_norm, force_scale = equilibrium.residual_norm, equilibrium.residual_scale
        control_scale = length if control_dof % max_dof_per_node < 3 else 1.0
        displacement_scale = max(
            abs(target)/control_scale, abs(u[control_dof])/control_scale, 1.0
        )
        relative_residual = equilibrium.relative_residual
        relative_constraint = abs(constraint)/control_scale/displacement_scale
        if iteration:
            du_norm = generalized_displacement_norm(
                du, stride=max_dof_per_node, length=length
            )
            u_norm = max(
                generalized_displacement_norm(
                    u, stride=max_dof_per_node, length=length
                ),
                1.0,
            )
            relative_du = max(
                du_norm/u_norm,
                abs(dlambda)/max(abs(load_factor), 1.0),
            )
        else:
            relative_du = float('inf')
        self.convergence_history.append({
            'iteration': iteration+1,
            'residual_norm': residual_norm,
            'residual_scale': force_scale,
            'relative_residual': float(relative_residual),
            'control_residual': float(constraint),
            'relative_control_residual': float(relative_constraint),
            'increment_norm': float(du_norm) if iteration else None,
            'solution_norm': float(u_norm) if iteration else None,
            'relative_du': float(relative_du) if iteration else None,
            'load_factor': float(load_factor),
        })

        if max(relative_residual, relative_constraint) < tol:
            if iteration == 0 or relative_du < tol:
                # As in load control, a zero residual is accepted only after
                # proving that the augmented tangent has numerical rank.
                if iteration == 0 or resolved.nonlinear_springs:
                    augmented = augmented_tangent(u, load_factor)
                    try:
                        self._solve_newton_system(augmented, np.zeros(len(active)+1))
                    except ValueError:
                        return False, u, iteration+1, load_factor
                self._last_internal_force = F_int.copy()
                return True, u, iteration+1, load_factor

        predictor = equilibrium.residual.copy()
        predictor[control_dof] = constraint
        augmented = augmented_tangent(u, load_factor, predictor)
        rhs = np.r_[residual_free, constraint]
        try:
            increment = self._solve_newton_system(augmented, rhs)
        except ValueError:
            return False, u, iteration+1, load_factor
        du = np.zeros_like(u)
        du[active] = increment[:-1]
        dlambda = float(increment[-1])
        u += du
        load_factor += dlambda
        if not np.all(np.isfinite(u)) or not np.isfinite(load_factor):
            return False, u, iteration+1, load_factor

    return False, u, max_iter, load_factor
