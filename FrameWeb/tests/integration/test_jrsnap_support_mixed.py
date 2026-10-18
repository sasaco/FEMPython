import json

import numpy as np
import pytest

from fem.file_io import _read_json_model
from fem.model import FemModel
from tests.support.builders.input_routes import python_axial
from tests.support.builders.jrsnap_support import (
    FORCES, TARGETS, bilinear_definition, legacy_jrsnap_data, trilinear_definition)
from tests.support.paths import DATA

pytestmark = pytest.mark.integration


# Bilinear K1=1000, F1=10/5 on .005, .02, .05, .044, .038 (zero-force position .04 after .05).
LAWS = [(trilinear_definition(), FORCES[:5], 'jrsnap_trilinear_v1'),
        (bilinear_definition(), [5., 10., 10., 4., -2.], 'jrsnap_bilinear_v1')]


@pytest.mark.parametrize('kind, kb', [('tetra', 200.), ('shell', 1000/.9375*.1)])
@pytest.mark.parametrize('with_beam', [False, True])
@pytest.mark.parametrize('definition, forces, model_id', LAWS)
def test_linear_shell_solid_and_beam_mix(kind, kb, with_beam, definition, forces, model_id):
    # Right unit tetra: V=1/6, Dxxxx=1200, dN2/dx=1 => kxx=200.
    # Unit right plane-stress triangle: t*A=.1, Dxxxx=1000/(1-.25^2).
    nodes = {'10': [0, 0, 0], '30': [1, 0, 0], '50': [0, 1, 0]}
    if kind == 'tetra':
        nodes['70'] = [0, 0, 1]
    elements = {'5': dict(type=kind, nodes=[int(n) for n in nodes], material_id=1, thickness=.2)}
    stride = 6 if kind == 'shell' or with_beam else 3
    restraints = {n: {'dof': [True]*stride} for n in nodes}
    restraints['30']['dof'][0] = False
    raw = dict(nodes=nodes, elements=elements,
               materials={'1': dict(name='reference', E=1000, nu=.25)},
               boundary_conditions=dict(restraints=restraints, loads={'30': [1, 0, 0, 0, 0, 0]},
                   nonlinear_spring_supports={'30': {'x': definition}}),
               analysis_params={'displacement_control': {'node': 30, 'dof': 'dx', 'targets': TARGETS[:5]}})
    if with_beam:
        raw['elements']['6'] = dict(type='bar', nodes=[10, 30], material_id=1, section_id=1)
        raw['bar_parameters'] = {'1': dict(area=3, Iy=1, Iz=1, J=1)}
        kb += 3000.
    model = FemModel()
    model.read_json_model(_read_json_model(raw))
    result = model._run_solver_snapshot()
    assert model.solver.layout.stride == stride
    assert [s['lambda'] for s in result['step_results']] == pytest.approx(kb*np.array(TARGETS[:5])+forces)
    assert result['reaction_forces'][30]['fx'] == pytest.approx(-forces[-1], abs=1e-9)
    assert result['metadata']['analysis']['support_models'] == [model_id]


def test_jr_axial_beam_and_jrsnap_support_use_independent_histories():
    model = python_axial(force=1)
    model.add_nonlinear_spring_support(30, 'x', dict(bilinear_definition(), F1_pos=2., F1_neg=1.))
    model.analysis_params['displacement_control'] = dict(node=30, dof='dx', targets=[.001, .004, .012])
    result = model._run_solver_snapshot()
    # JR N(.0005,.002,.006) = 5,12,18. Bilinear F(.001,.004,.012) = 1,2,2.
    assert [s['lambda'] for s in result['step_results']] == pytest.approx([6, 14, 20])
    assert result['element_stresses'][7]['j_end'][0] == pytest.approx(18)
    assert result['support_response'][30]['x']['force'] == pytest.approx(2.)
    assert result['support_response'][30]['x']['branch'] == 'plateau_pos'


@pytest.mark.parametrize('key, dof, kb', [('tx', 'dx', 3000.), ('ty', 'dy', 3000.), ('rz', 'rz', 4000.)])
def test_two_dimensional_legacy_model_in_each_plane_direction(key, dof, kb):
    raw = legacy_jrsnap_data()
    raw['dimension'] = 2
    support = dict(n=30, tx=1, ty=1, rz=1)
    support[key] = trilinear_definition()
    raw['fix_node']['1'] = [dict(n=10, tx=1, ty=1, rz=1), support]
    raw['load']['1']['load_node'] = [{'n': 30, key: 1.}]
    raw['analysis_params']['displacement_control'].update(dof=dof)
    model = FemModel()
    model.read_json_model(_read_json_model(raw))
    result = model._run_solver_snapshot()
    direction = {'tx': 'x', 'ty': 'y', 'rz': 'rz'}[key]
    assert [s['lambda'] for s in result['step_results']] == pytest.approx(kb*np.array(TARGETS)+FORCES)
    assert [s['support_response'][30][direction]['force'] for s in result['step_results']] == pytest.approx(
        FORCES, abs=1e-9)


def test_two_dimensional_rigid_member_matches_the_frozen_jrsnap_softening_run():
    """JRSNAP case 03: displacement increments at node 1 of a rigid member on a node-2 X spring."""
    golden = json.loads((DATA/'jrsnap_golden/support_trilinear03_softening/golden_n200.json')
                        .read_text(encoding='utf-8'))
    steps = golden['steps'][1:]
    raw = {
        'dimension': 2,
        'node': {'1': {'x': 0, 'y': -1, 'z': 0}, '2': {'x': 0, 'y': 0, 'z': 0}},
        'member': {'1': {'ni': 1, 'nj': 2, 'e': 1}},
        # Practically rigid: P/(3EI) moves the spring force by at most 1000*14/3e8 = 5e-5 kN.
        'element': {'1': {'1': {'E': 1e8, 'A': 1., 'Iy': 1., 'Iz': 1., 'J': 1., 'nu': .2}}},
        'fix_node': {'1': [dict(n=2, tx=dict(trilinear_definition(), K3_pos=-20., K3_neg=.001), ty=1, rz=1)]},
        'load': {'1': {'fix_node': 1, 'load_node': [dict(n=1, tx=1.)]}},
        'analysis_params': {'displacement_control': {
            'node': 1, 'dof': 'dx', 'targets': [s['joints']['1']['dx'] for s in steps]},
            # A stiffness ratio of 3e5 leaves a rounding-level relative residual near 1e-10.
            'tolerance': 1e-8},
    }
    model = FemModel()
    model.read_json_model(_read_json_model(raw))
    result = model._run_solver_snapshot()
    for step, reference in zip(result['step_results'], steps, strict=True):
        spring = step['support_response'][2]['x']['force']
        assert step['lambda'] == pytest.approx(reference['elements']['1']['Si'], abs=1e-4)
        assert spring == pytest.approx(reference['joints']['1']['jiban_hanryoku'], abs=1e-4)
        assert step['reaction_forces'][2]['fx'] == pytest.approx(-spring, abs=1e-9)

