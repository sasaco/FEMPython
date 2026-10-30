"""Slip outputs pinned before the support-law registry, and registry dispatch."""
import json
from pathlib import Path

import numpy as np
import pytest

from fem.file_io import _read_json_model, model_to_jsonable, result_to_jsonable
from fem.model import FemModel
from fem.nonlinear.hysteresis.jrsnap_support import JrsnapSupportParams, parse_jrsnap_support
from fem.nonlinear.hysteresis.slip import SlipSpringParams
from fem.nonlinear.support_springs import SupportSprings
from tests.support.builders.jrsnap_support import jrsnap_model_data, trilinear_definition
from tests.support.builders.slip_support import slip_definition, slip_model_data

pytestmark = pytest.mark.integration

EXAMPLE = Path(__file__).resolve().parents[2]/'docs/examples/slip-support-history.json'
# Recorded before the registry existed (HEAD 639fcb4); the slip contract must not move.
SLIP_EXAMPLE_SHA256 = 'ee070fb77e041ef28ce05e3eddc3f07f31fb81befaeb9de7a031053c25b02a8a'
SLIP_FINAL_RESPONSE = {'30': {'x': {
    'type': 'slip', 'deformation': .04, 'force': 13., 'tangent': 100., 'branch': 'skeleton_pos',
    'direction': 1, 'delta_max_pos': .04, 'delta_max_neg': -.03, 'force_at_max_pos': 13.,
    'force_at_max_neg': -12., 'zero_pos': .027, 'zero_neg': -.018,
    'yielded_pos': True, 'yielded_neg': True,
    # GLOSSARY 部材の状態: a slip spring has no skeleton points.
    'points_passed': None}}}


def _run(raw=None, path=None):
    model = FemModel()
    if path is None:
        model.read_json_model(_read_json_model(raw))
    else:
        model.load_model(str(path))
    return result_to_jsonable(model._run_solver_snapshot())


def test_slip_example_output_hash_and_models_are_unchanged():
    result = _run(path=EXAMPLE)
    assert result['metadata']['input_sha256'] == SLIP_EXAMPLE_SHA256
    assert result['metadata']['analysis']['support_models'] == ['slip_v1']
    response = result['support_response']
    assert response.keys() == SLIP_FINAL_RESPONSE.keys()
    assert list(response['30']['x']) == list(SLIP_FINAL_RESPONSE['30']['x'])
    for key, value in SLIP_FINAL_RESPONSE['30']['x'].items():
        assert response['30']['x'][key] == pytest.approx(value, abs=1e-12), key
    assert _run(slip_model_data())['metadata']['input_sha256'] == SLIP_EXAMPLE_SHA256


def test_slip_normalized_json_is_unchanged():
    saved = model_to_jsonable(_read_json_model(slip_model_data()))
    assert saved['boundary_conditions']['nonlinear_spring_supports'] == {'30': {'x': slip_definition()}}
    assert json.dumps(slip_definition()) == '{"type": "slip", "K1": 1000.0, "K2": 100.0, "delta_1": 0.01}'


@pytest.mark.parametrize('bad', [dict(type='unknown'), dict(K1=1.), dict(type=None), 'slip'])
def test_unknown_type_has_location(bad):
    raw = slip_model_data()
    raw['boundary_conditions']['nonlinear_spring_supports']['30']['x'] = bad
    with pytest.raises(ValueError, match='30.*x'):
        _read_json_model(raw)


def test_runtime_error_names_the_support_and_keeps_the_slip_wording():
    params = SlipSpringParams(1000, 100, .01)
    runtime = SupportSprings({0: (30, 'rz', params)})
    with pytest.raises(ValueError, match='nonlinear support.*30.*rz'):
        runtime.evaluate(np.array([float('nan')]))


def test_commit_requires_each_spring_law_state_type():
    slip = SlipSpringParams(1000, 100, .01)
    jrsnap = parse_jrsnap_support(trilinear_definition())
    runtime = SupportSprings({0: (10, 'x', slip), 1: (30, 'x', jrsnap)})
    trial = runtime.evaluate(np.array([.02, .02]))
    swapped = {0: trial[1], 1: trial[0]}
    with pytest.raises(ValueError, match='one evaluated state per spring'):
        runtime.commit(swapped)
    runtime.commit(trial)
    snapshot = runtime.snapshot()
    assert snapshot[10]['x']['type'] == 'slip'
    assert snapshot[30]['x']['type'] == 'jrsnap_trilinear'
    assert snapshot[30]['x']['force'] == 12.


def test_support_models_list_each_law_once_in_order():
    raw = jrsnap_model_data()
    assert _run(raw)['metadata']['analysis']['support_models'] == ['jrsnap_trilinear_v1']
    raw['nodes']['50'] = [4., 0., 0.]
    raw['elements']['6'] = dict(raw['elements']['5'], nodes=[30, 50])
    raw['boundary_conditions']['restraints']['50'] = {'dof': [False, True, True, True, True, True]}
    raw['boundary_conditions']['nonlinear_spring_supports']['50'] = {'x': slip_definition()}
    raw['analysis_params']['displacement_control']['targets'] = [.005]
    assert _run(raw)['metadata']['analysis']['support_models'] == ['jrsnap_trilinear_v1', 'slip_v1']


def test_parameters_pass_through_the_registry_unchanged():
    from fem.nonlinear.support_laws import parse_support_law
    jrsnap = parse_jrsnap_support(trilinear_definition())
    slip = SlipSpringParams(1000, 100, .01)
    assert parse_support_law(jrsnap, location='here') is jrsnap
    assert parse_support_law(slip, location='here') is slip
    assert isinstance(parse_support_law(trilinear_definition(), location='here'), JrsnapSupportParams)
    with pytest.raises(ValueError, match='here'):
        parse_support_law(object(), location='here')
