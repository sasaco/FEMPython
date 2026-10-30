"""Shared static analysis flow, independent physical roots and API boundaries."""

import numpy as np
import pytest
from scipy.sparse import csr_matrix

from fem.nonlinear.nonlinear_solver import NonlinearConvergenceError, NonlinearSolver
from fem.solver import Solver
from tests.support.builders.nonlinear_solver import CappedBar, CubicBar, axial_bar
from tests.support.builders.input_routes import axial_json, json_model

pytestmark = [pytest.mark.unit, pytest.mark.material_nonlinear]


@pytest.mark.parametrize("analysis", ["static", "material_nonlinear"])
def test_common_entry_obeys_axial_hand_solution(analysis):
    solver = Solver()
    result = solver.solve(*axial_bar(), analysis_type=analysis, n_steps=3)
    assert result["node_displacements"][2]["dx"] == pytest.approx(0.01)
    assert result["reaction_forces"][1]["fx"] == pytest.approx(-30)
    assert len(solver.step_results) == (1 if analysis == "static" else 3)
    if analysis == "static":
        assert set(result) == {"displacement", "displacement_correction", "node_displacements", "reaction_forces"}


def test_static_does_not_enter_newton(monkeypatch):
    def unexpected(*args, **kwargs):
        pytest.fail("constant stiffness must use the direct equilibrium solve")
    solver = Solver()
    monkeypatch.setattr(solver, "_newton_raphson_iteration", unexpected)
    solver.solve(*axial_bar(), analysis_type="static", n_steps=10)
    assert solver.convergence_history == []


def test_one_step_nonlinear_still_iterates_to_independent_cubic_root():
    args = axial_bar()
    args[2].loads.clear()
    args[2].add_load(2, [2, 0, 0, 0, 0, 0])
    args[3][1] = CubicBar()
    result = Solver().solve(*args, analysis_type="material_nonlinear", n_steps=1)
    assert result["displacement"][6] == pytest.approx(1, abs=1e-7)
    assert result["step_results"][0]["iterations"] > 1


def test_failed_step_restores_load_factor_force_and_allows_reuse():
    args = axial_bar()
    element = CappedBar()
    element.load_factor = 0.0
    args[3][1] = element
    args[2].loads.clear()
    args[2].add_load(2, [3, 0, 0, 0, 0, 0])
    solver = Solver()
    accepted = []
    with pytest.raises(NonlinearConvergenceError) as error:
        solver.solve(*args, analysis_type="material_nonlinear", n_steps=3, callback=accepted.append)
    assert error.value.step == 2
    assert element.load_factor == pytest.approx(1/3)
    assert solver._last_internal_force[6] == pytest.approx(1)
    np.testing.assert_array_equal(solver.displacement, accepted[0]["displacement"])
    result = solver.solve(*axial_bar())
    assert result["displacement"][6] == pytest.approx(0.01)
    assert solver.convergence_history == []
    assert len(solver.step_results) == 1


def test_model_uses_common_solver_and_final_snapshots_are_independent(monkeypatch):
    model = json_model(axial_json())
    calls = []
    original = model.solver.solve
    def record(*args, **kwargs):
        calls.append(kwargs.get("analysis_type", "static"))
        return original(*args, **kwargs)
    monkeypatch.setattr(model.solver, "solve", record)
    result = model._run_solver_snapshot()
    assert calls == ["material_nonlinear"]
    assert model.nonlinear_solver.displacement is model.solver.displacement
    last = result["step_results"][-1]
    assert not np.shares_memory(result["displacement"], last["displacement"])
    assert result["reaction_forces"] is not last["reaction_forces"]
    assert result["element_stresses"] is not last["element_stresses"]
    for key, values in result["element_stresses"].items():
        for end in ("i_end", "j_end"):
            assert not np.shares_memory(values[end], last["element_stresses"][key][end])


def test_legacy_solve_dispatches_to_nonlinear_and_exposes_shared_state():
    solver = NonlinearSolver()
    result = solver.solve(*axial_bar(), n_steps=2)
    assert result["analysis_type"] == "material_nonlinear"
    assert len(result["step_results"]) == 2
    assert solver.displacement[6] == pytest.approx(0.01)


def test_newton_linear_algebra_does_not_replace_accepted_displacement():
    solver = NonlinearSolver()
    solver.displacement = np.array([7.0])
    increment = solver._solve_newton_system(csr_matrix([[2.0]]), np.array([4.0]))
    np.testing.assert_array_equal(increment, [2.0])
    np.testing.assert_array_equal(solver.displacement, [7.0])


def test_newton_system_accepts_a_backward_stable_solve_with_a_stiff_short_link():
    """A 0.01 m link is ~1e14 times stiffer than its support, and a displacement
    control step has a right side of 1e-3. Rounding in K @ du is then larger than
    1e-8*|R| although the solve is exact to machine precision (backward error)."""
    from fractions import Fraction

    k, soft, ground = 3.5e14, 1e3, 7.0
    stiffness = np.array([[k + soft, -k], [-k, k + ground]])
    rhs = np.array([1e-3, 0.0])
    increment = NonlinearSolver()._solve_newton_system(csr_matrix(stiffness), rhs)
    backward = (np.abs(stiffness @ increment - rhs).max()
                / (np.abs(stiffness) @ np.abs(increment) + np.abs(rhs)).max())
    assert backward < 1e-15
    # The forward error is bounded by condition number (~k/soft) times rounding.
    det = Fraction(k + soft)*Fraction(k + ground) - Fraction(k)**2
    exact = [float(Fraction(k + ground)*Fraction(1e-3)/det), float(Fraction(k)*Fraction(1e-3)/det)]
    np.testing.assert_allclose(increment, exact, rtol=4*k/soft*np.finfo(float).eps)


class TranslationElement:
    """Three independent grounded unit springs, arbitrary external node ID."""
    node_ids = [307]
    def get_dof_per_node(self):
        return 3
    def get_stiffness_matrix(self):
        return np.diag([2.0, 3.0, 4.0])
    def get_mass_matrix(self):
        return np.eye(3)


def translation_model():
    from fem.mesh import MeshModel
    from fem.material import Material
    from fem.boundary_condition import BoundaryCondition
    mesh = MeshModel()
    mesh.add_node(307, [0, 0, 0])
    mesh.add_element(8, "tetra", [307], 1)
    boundary = BoundaryCondition()
    boundary.add_load(307, [2, 6, 12])
    return mesh, Material(), boundary, {8: TranslationElement()}


def test_internal_six_key_format_and_legacy_three_key_projection():
    solver = Solver()
    result = solver.solve(*translation_model(), analysis_type="material_nonlinear", n_steps=1)
    assert result["node_displacements"][307] == pytest.approx(dict(dx=1, dy=2, dz=3, rx=0, ry=0, rz=0))
    old = NonlinearSolver().solve_nonlinear(*translation_model(), n_steps=1)
    assert set(old["node_displacements"][307]) == {"dx", "dy", "dz"}
    assert set(old["step_results"][0]["node_displacements"][307]) == {"dx", "dy", "dz"}


def test_assembly_layout_does_not_depend_on_load_assembly():
    mesh, material, _, elements = translation_model()
    solver = Solver()
    u = np.array([1., 2., 3.])
    np.testing.assert_array_equal(solver._assemble_internal_forces(mesh, elements, u, 3), [2, 6, 12])
    np.testing.assert_array_equal(solver._assemble_tangent_stiffness(mesh, material, elements, u, 3).toarray(), np.diag([2, 3, 4]))
    np.testing.assert_array_equal(solver.create_mass_matrix(mesh, material, elements).toarray(), np.eye(3))


def test_modal_still_solves_generalized_eigenproblem():
    result = Solver().eigenvalue_analysis(*translation_model(), n_modes=1)
    assert result["eigenvalues"][0] == pytest.approx(2)
    assert result["frequencies"][0] == pytest.approx(np.sqrt(2)/(2*np.pi))
    assert set(result["modes"][0]) == {307}


def test_same_material_elements_restart_history_after_failure_and_type_switch():
    model = json_model(axial_json(force=30))
    model._create_elements()
    model._set_element_coordinates()
    args = (model.mesh, model.material, model.boundary, model.elements)
    solver = Solver()
    with pytest.raises(NonlinearConvergenceError):
        solver.solve(*args, analysis_type="material_nonlinear", load_factors=[0.4, 1])
    element = model.elements[7]
    assert element.committed_states["axial"]["center"].current_P == pytest.approx(12)
    model.boundary.loads.clear()
    model.add_load(30, fx=12)
    for analysis, expected in [("static", .0024), ("material_nonlinear", .004)] * 2:
        result = solver.solve(*args, analysis_type=analysis)
        assert result["displacement"][6] == pytest.approx(expected, abs=1e-12)
        assert result["reaction_forces"][10]["fx"] == pytest.approx(-12, abs=1e-9)
        if analysis == "static":
            assert element._committed_response is None


def test_snapshot_reads_once_and_callback_mutation_cannot_change_results(monkeypatch):
    from copy import deepcopy
    model = json_model(axial_json())
    model._create_elements()
    model._set_element_coordinates()
    element = model.elements[7]
    calls = []
    original = element.calculate_forces
    def read(u):
        before = deepcopy(element.committed_states)
        calls.append(u.copy())
        result = original(u)
        assert element.current_states == element.committed_states == before
        return result
    monkeypatch.setattr(element, "calculate_forces", read)
    def mutate(payload):
        payload["displacement"][:] = 99
        payload["reaction_forces"][10]["fx"] = 99
        payload["element_stresses"][7]["j_end"][:] = 99
    result = model.solver.solve(model.mesh, model.material, model.boundary, model.elements,
                                analysis_type="material_nonlinear", n_steps=3, callback=mutate)
    assert len(calls) == 3
    assert result["displacement"][6] == pytest.approx(.004)
    assert result["reaction_forces"][10]["fx"] == pytest.approx(-12)
    assert result["step_results"][-1]["element_stresses"][7]["j_end"][0] == pytest.approx(12)


@pytest.mark.parametrize("analysis", ["static", "material_nonlinear"])
@pytest.mark.parametrize("legacy", [False, True])
def test_shared_spring_and_prescribed_displacement_balance(analysis, legacy):
    # Root moves .02; tip spring 2000, bar 3000 and tip load 40:
    # 3000*(u-.02)+2000*u=40 -> u=.02, root reaction 0, spring reaction -40.
    args = axial_bar(prescribed=None, spring=2000 if legacy else None)
    boundary = args[2]
    boundary.add_restraint(1, [True]*6, [.02, 0, 0, 0, 0, 0])
    if not legacy:
        boundary.spring_supports = {2: {'x': 2000.0}}
    boundary.loads.clear()
    boundary.add_load(2, [40, 0, 0, 0, 0, 0])
    result = Solver().solve(*args, analysis_type=analysis, n_steps=2)
    assert result["displacement"][6] == pytest.approx(.02, abs=1e-14)
    assert result["reaction_forces"][1]["fx"] == pytest.approx(0, abs=1e-10)
    assert result["reaction_forces"][2]["fx"] == pytest.approx(-40, abs=1e-10)


def test_legacy_callback_error_leaves_accepted_step_and_stops():
    class StopCallback(Exception):
        pass
    solver = NonlinearSolver()
    def stop(step):
        assert step["step"] == 1
        assert set(step["node_displacements"][307]) == {"dx", "dy", "dz"}
        raise StopCallback
    with pytest.raises(StopCallback):
        solver.solve_nonlinear(*translation_model(), n_steps=2, callback=stop)
    assert len(solver.step_results) == 1
    np.testing.assert_allclose(solver.displacement, [.5, 1, 1.5])
    assert solver.load_factor == .5


def test_invalid_reanalysis_does_not_expose_previous_solver_state():
    solver = Solver()
    solver.solve(*axial_bar())
    with pytest.raises(ValueError, match="positive integer"):
        solver.solve(*axial_bar(), analysis_type="material_nonlinear", max_iter=0)
    assert solver.displacement is solver.displacement_correction is None
    assert solver.step_results == solver.convergence_history == []


@pytest.mark.parametrize("analysis", ["static", "material_nonlinear"])
def test_mixed_element_widths_share_layout_after_three_dof_analysis(analysis):
    solver = Solver()
    solver.solve(*translation_model())
    args = axial_bar()
    mesh, _, boundary, elements = args
    mesh.add_node(307, [0, 1, 0])
    mesh.nodes = dict(reversed(list(mesh.nodes.items())))
    mesh.add_element(8, "tetra", [307], 1)
    elements[8] = TranslationElement()
    boundary.add_restraint(307, [False]*3 + [True]*3)
    boundary.add_load(307, [2, 6, 12, 0, 0, 0])
    result = solver.solve(*args, analysis_type=analysis, n_steps=2)
    assert solver.layout.stride == 6
    assert result["node_displacements"][307] == pytest.approx(dict(dx=1, dy=2, dz=3, rx=0, ry=0, rz=0))
    assert result["node_displacements"][2]["dx"] == pytest.approx(.01)
    assert result["reaction_forces"][1]["fx"] == pytest.approx(-30)
