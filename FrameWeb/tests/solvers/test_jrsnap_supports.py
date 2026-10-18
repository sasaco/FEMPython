"""Equilibrium, zero and negative stiffness of JRSNAP multi-linear supports."""
import json

import numpy as np
import pytest

from fem.equilibrium import NonlinearConvergenceError
from fem.file_io import _read_json_model
from fem.model import FemModel
from fem.solver import Solver
from tests.support.builders.jrsnap_support import (
    FORCES, TARGETS, bilinear_definition, jrsnap_model_data, trilinear_definition)
from tests.support.builders.nonlinear_solver import axial_bar, CubicBar
from tests.support.paths import DATA

pytestmark = pytest.mark.integration

ZERO_POSITIONS = [0., .008, .038, .038, .038, .038, .03, -.008, -.036, -.036, -.036, -.036,
                  -.03, .008, .018, .049]


@pytest.mark.parametrize('direction, kb', [('x', 3000), ('y', 3000), ('z', 3000),
                                        ('rx', 400), ('ry', 4000), ('rz', 4000)])
def test_parallel_beam_support_balance_both_controls(direction, kb):
    raw = jrsnap_model_data(direction)
    expected = kb*np.array(TARGETS)+FORCES
    model = FemModel()
    model.read_json_model(_read_json_model(raw))
    controlled = model._run_solver_snapshot()
    assert controlled['analysis_type'] == 'material_nonlinear'
    force_name = {'x': 'fx', 'y': 'fy', 'z': 'fz', 'rx': 'mx', 'ry': 'my', 'rz': 'mz'}[direction]
    index = ['x', 'y', 'z', 'rx', 'ry', 'rz'].index(direction)
    for target, force, zero, factor, step in zip(TARGETS, FORCES, ZERO_POSITIONS, expected,
                                                 controlled['step_results'], strict=True):
        response = step['support_response'][30][direction]
        assert step['lambda'] == pytest.approx(factor, abs=1e-9)
        assert response['force'] == pytest.approx(force, abs=1e-10)
        assert response['zero_position'] == pytest.approx(zero, abs=1e-12)
        assert step['reaction_forces'][30][force_name] == pytest.approx(-force, abs=1e-9)
        assert step['displacement'][6+index] == pytest.approx(target, abs=1e-12)
        assert step['element_stresses'][5]['j_end'][index] == pytest.approx(kb*target, abs=1e-9)
    # Load control crosses four reversals and the K3 < 0 softening (kb + K3 > 0).
    raw['analysis_params'] = {'load_factors': expected.tolist(), 'tolerance': 1e-10}
    model.read_json_model(_read_json_model(raw))
    loaded = model._run_solver_snapshot()
    np.testing.assert_allclose([s['displacement'][6+index] for s in loaded['step_results']],
                               TARGETS, atol=1e-12)
    assert max(s['iterations'] for s in loaded['step_results']) <= 10
    assert loaded['support_response'][30][direction]['force'] == pytest.approx(11.)
    assert loaded['support_response'][30][direction]['branch'] == 'skeleton_pos'
    assert loaded['metadata']['solver']['relative_residual'] < 1e-10


class EmptyBar(CubicBar):
    def get_internal_force(self, u):
        return np.zeros(12)

    def get_tangent_stiffness_matrix(self, u):
        return np.zeros((12, 12))


def spring_only_system(definition):
    args = axial_bar()
    args[2].loads.clear()
    args[2].add_load(2, [1, 0, 0, 0, 0, 0])
    args[2].nonlinear_spring_supports = {2: {'x': definition}}
    args[3][1] = EmptyBar()
    return args


def test_displacement_control_descends_to_zero_force_and_stays_inactive():
    solver = Solver()
    result = solver.solve(*spring_only_system(trilinear_definition()), analysis_type='material_nonlinear',
                          displacement_control={'node': 2, 'dof': 'dx', 'targets': [.02, .05, .1, .17, .2, .19]})
    assert [s['lambda'] for s in result['step_results']] == pytest.approx([12, 12, 7, 0, 0, -5], abs=1e-10)
    assert [s['support_response'][2]['x']['branch'] for s in result['step_results']] == [
        'skeleton_pos', 'skeleton_pos', 'skeleton_pos', 'zero_pos', 'zero_pos', 'plateau_neg']


@pytest.mark.parametrize('definition, factors, branch, accepted', [
    (bilinear_definition(), [5., 10.], 'plateau_pos', .005),
    # After yielding at .02 (p = .008), -F1_neg is a plateau reached at -.002.
    (trilinear_definition(), [12., -5.], 'plateau_neg', .02),
])
def test_load_control_rejects_arrival_on_a_zero_tangent_and_rolls_back(definition, factors, branch, accepted):
    args = spring_only_system(definition)
    solver = Solver()
    with pytest.raises(NonlinearConvergenceError) as caught:
        solver.solve(*args, analysis_type='material_nonlinear', load_factors=factors)
    assert caught.value.step == 2
    response = caught.value.details['support_response'][2]['x']
    assert (response['tangent'], response['branch']) == (0., branch)
    assert solver.load_factor == factors[0]
    assert len(solver.step_results) == 1
    assert solver.displacement[6] == pytest.approx(accepted)
    committed = solver.support_springs.snapshot()[2]['x']
    assert (committed['deformation'], committed['force'], committed['type']) == (
        pytest.approx(accepted), pytest.approx(factors[0]), definition['type'])


def test_load_control_beyond_the_capacity_fails():
    with pytest.raises(NonlinearConvergenceError):
        Solver().solve(*spring_only_system(bilinear_definition()), analysis_type='material_nonlinear',
                       load_factors=[5., 11.])


def test_beam_stabilizes_a_one_side_cut_off_support():
    args = axial_bar()
    args[2].loads.clear()
    args[2].add_load(2, [1, 0, 0, 0, 0, 0])
    args[2].nonlinear_spring_supports = {2: {'x': dict(trilinear_definition(), F1_pos=0., F2_pos=0., K3_pos=0.)}}
    result = Solver().solve(*args, analysis_type='material_nonlinear', load_factors=[30., -21.])
    # EA/L = 3000: the cut-off side gives 3000*.01 = 30; then -21 = 3000*delta - F1_neg.
    assert [s['displacement'][6] for s in result['step_results']] == pytest.approx([.01, -16/3000])
    assert [s['support_response'][2]['x']['force'] for s in result['step_results']] == pytest.approx([0., -5.])


@pytest.mark.parametrize('changes, tangent', [({}, 500.), (dict(F1_neg=0., F2_neg=0.), 1000.)])
def test_unloaded_spring_dof_passes_the_rank_check(changes, tangent):
    args = axial_bar()
    args[2].loads.clear()
    args[2].add_restraint(2, [False, False, True, True, True, True])
    args[2].add_load(2, [0, 1, 0, 0, 0, 0])
    args[2].spring_supports = {2: {'y': 100.}}
    args[2].nonlinear_spring_supports = {2: {'x': dict(trilinear_definition(), **changes)}}
    args[3][1] = EmptyBar()
    result = Solver().solve(*args, analysis_type='material_nonlinear', load_factors=[1., 2.])
    assert [s['displacement'][7] for s in result['step_results']] == pytest.approx([.01, .02])
    response = result['support_response'][2]['x']
    assert (response['deformation'], response['force'], response['tangent']) == (0., 0., tangent)


def test_spring_only_displacement_control_matches_the_frozen_jrsnap_softening_run():
    golden = json.loads((DATA/'jrsnap_golden/support_trilinear03_softening/golden_n200.json')
                        .read_text(encoding='utf-8'))
    steps = golden['steps'][1:]
    definition = dict(trilinear_definition(), K3_pos=-20., K3_neg=.001)
    result = Solver().solve(*spring_only_system(definition), analysis_type='material_nonlinear',
                            displacement_control={'node': 2, 'dof': 'dx',
                                                  'targets': [s['joints']['2']['dx'] for s in steps]})
    for step, reference in zip(result['step_results'], steps, strict=True):
        assert step['lambda'] == pytest.approx(reference['joints']['1']['jiban_hanryoku'], abs=1e-4)
        assert step['lambda'] == pytest.approx(reference['elements']['1']['Si'], abs=1e-4)
