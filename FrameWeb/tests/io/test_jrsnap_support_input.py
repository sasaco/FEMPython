"""One normalized JRSNAP support definition for every input route."""
import json

import pytest

from fem.file_io import _read_json_model, model_to_jsonable, result_to_jsonable, write_model
from fem.model import FemModel
from fem.nonlinear.hysteresis.jrsnap_support import parse_jrsnap_support
from tests.support.builders.jrsnap_support import (
    bilinear_definition, jrsnap_model_data, legacy_jrsnap_data, trilinear_definition)
from tests.support.builders.slip_support import slip_definition

pytestmark = pytest.mark.integration

OMITTED = {key: value for key, value in trilinear_definition().items()
           if key not in ('K1_neg', 'K2_neg', 'K3_neg')}


def _python_model(definition, direction='x'):
    raw = jrsnap_model_data(direction)
    raw['boundary_conditions'].pop('nonlinear_spring_supports')
    model = FemModel()
    model.read_json_model(_read_json_model(raw))
    model.add_nonlinear_spring_support(30, direction, definition)
    return model


def _hash(raw):
    model = FemModel()
    model.read_json_model(_read_json_model(raw))
    return result_to_jsonable(model._run_solver_snapshot())['metadata']['input_sha256']


@pytest.mark.parametrize('definition', [trilinear_definition(), OMITTED, bilinear_definition()])
def test_legacy_normalized_and_python_routes_agree_and_roundtrip(definition):
    expected = parse_jrsnap_support(definition)
    routes = [
        _read_json_model(jrsnap_model_data(definition=definition))['boundary'],
        _read_json_model(legacy_jrsnap_data(definition))['boundary'],
        _python_model(definition).boundary,
    ]
    for boundary in routes:
        assert boundary.nonlinear_spring_supports[30]['x'] == expected
    data = _read_json_model(legacy_jrsnap_data(definition))
    saved = model_to_jsonable(data)
    assert saved['boundary_conditions']['nonlinear_spring_supports']['30']['x'] == expected.to_dict()
    loaded = _read_json_model(json.loads(json.dumps(saved)))
    assert loaded['boundary'].nonlinear_spring_supports[30]['x'] == expected


def test_omitted_negative_stiffness_has_the_explicit_hash_and_values_change_it():
    inherited = dict(trilinear_definition(), K1_neg=1000., K2_neg=200., K3_neg=-100.)
    reference = _hash(jrsnap_model_data(definition=inherited))
    assert _hash(jrsnap_model_data(definition=OMITTED)) == reference
    assert _hash(jrsnap_model_data(definition=dict(inherited, F2_neg=7.5))) != reference
    assert _hash(jrsnap_model_data()) != reference


@pytest.mark.parametrize('direction', ['x', 'y', 'z', 'rx', 'ry', 'rz'])
def test_python_api_rejects_duplicates_atomically(direction):
    model = _python_model(trilinear_definition(), direction)
    before = dict(model.boundary.nonlinear_spring_supports[30])
    with pytest.raises(ValueError, match='30.*'+direction):
        model.add_nonlinear_spring_support(30, direction, bilinear_definition())
    with pytest.raises(ValueError, match='30.*'+direction):
        model.add_nonlinear_spring_support(30, direction, slip_definition())
    assert model.boundary.nonlinear_spring_supports[30] == before


@pytest.mark.parametrize('conflict', ['fixed', 'prescribed', 'linear'])
def test_conflicting_supports_are_rejected(conflict):
    raw = jrsnap_model_data()
    bc = raw['boundary_conditions']
    if conflict == 'linear':
        bc['spring_supports'] = {'30': {'x': 2}}
    else:
        bc['restraints']['30']['dof'][0] = True
        if conflict == 'prescribed':
            bc['restraints']['30']['values'] = [.02, 0, 0, 0, 0, 0]
    with pytest.raises(ValueError, match='30.*x'):
        _read_json_model(raw)


@pytest.mark.parametrize('first', [True, False])
def test_legacy_rows_with_fixed_or_slip_cannot_overwrite_jrsnap(first):
    for other in (1, slip_definition()):
        raw = legacy_jrsnap_data()
        support = raw['fix_node']['1'][1]
        competing = dict(n=30, tx=other)
        raw['fix_node']['1'] = [support, competing] if first else [competing, support]
        with pytest.raises(ValueError, match='30.*x'):
            _read_json_model(raw)


@pytest.mark.parametrize('bad', [
    dict(OMITTED, F1_neg=0.), dict(OMITTED, K1_pos=0.), dict(OMITTED, K3_pos=float('inf')),
    dict(bilinear_definition(), K2=1.), dict(type='jrsnap_trilinear'), dict(type='jrsnap'),
])
def test_bad_definitions_have_the_location_on_every_route(bad):
    raw = jrsnap_model_data(definition=bad)
    with pytest.raises(ValueError, match='30.*x'):
        _read_json_model(raw)
    with pytest.raises(ValueError, match=r'30.*x.*\(tx\)'):
        _read_json_model(legacy_jrsnap_data(bad))
    with pytest.raises(ValueError, match='30.*x'):
        _python_model(bad)


@pytest.mark.parametrize('value', [0, 1, .5, -2])
def test_numeric_legacy_support_compatibility(value):
    raw = legacy_jrsnap_data()
    raw['fix_node']['1'][1]['tx'] = value
    bc = _read_json_model(raw)['boundary']
    assert bc.restraints[30].dof_restraints[0] == (value == 1)
    assert bc.spring_supports.get(30, {}).get('x') == (abs(value) if value not in (0, 1) else None)
    assert 30 not in bc.nonlinear_spring_supports


def test_three_dof_model_rejects_rotational_support():
    raw = jrsnap_model_data()
    raw['nodes'].update({'50': [0, 1, 0], '70': [0, 0, 1]})
    raw['elements'] = {'5': dict(type='tetra', nodes=[10, 30, 50, 70], material_id=1)}
    raw['boundary_conditions']['nonlinear_spring_supports'] = {'30': {'rz': trilinear_definition()}}
    with pytest.raises(ValueError, match='30.*rz.*3 DOFs'):
        _read_json_model(raw)


def test_fw3_fails_before_overwriting_file(tmp_path):
    target = tmp_path/'protected.fw3'
    target.write_text('original', encoding='utf8')
    with pytest.raises(ValueError, match='JSON'):
        write_model(_read_json_model(jrsnap_model_data()), str(target))
    assert target.read_text(encoding='utf8') == 'original'


@pytest.mark.parametrize('definition', [trilinear_definition(), bilinear_definition()])
def test_old_app_support_routes_explicitly_reject_objects(definition):
    from app.components.support import Support
    from app.inputDataUtils import make_supports
    from fem.models.fa_support import FA_Support
    with pytest.raises(ValueError, match='FemModel'):
        Support(0, definition, 0, 0, 0, 0, 0, False)
    with pytest.raises(ValueError, match='FemModel'):
        FA_Support(0, False, False, False, False, False, False, definition, 0, 0, 0, 0, 0)
    with pytest.raises(ValueError, match='FemModel'):
        make_supports([dict(n=1, tx=definition)], [], 3)
