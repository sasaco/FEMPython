"""Public transport and saved-model history against the manual 7.22 hand table."""
import base64
import gzip
import json
from pathlib import Path

import pytest

from fem.file_io import _read_json_model, result_to_jsonable
from fem.model import FemModel
from fem.nonlinear.hysteresis.jrsnap_support import parse_jrsnap_support
from main import app
from tests.integration._canonical_results import (
    assert_step_diagnostics,
    load_step_results,
    node_components,
    reaction_components,
)
from tests.support.builders.jrsnap_support import (
    FORCES, jrsnap_model_data, legacy_jrsnap_data, trilinear_definition)

pytestmark = pytest.mark.regression

EXAMPLE = Path(__file__).resolve().parents[2]/'docs/examples/jrsnap-trilinear-support.json'


def assert_history(result):
    for force, step in zip(FORCES, result['step_results'], strict=True):
        response = step['support_response']['30']['x']
        assert response['type'] == 'jrsnap_trilinear'
        assert response['force'] == pytest.approx(force, abs=1e-9)
        assert step['reaction_forces']['30']['fx'] == pytest.approx(-force, abs=1e-9)
        assert step['lambda'] == pytest.approx(3000*response['deformation']+force, abs=1e-9)
    assert result['support_response'] == result['step_results'][-1]['support_response']
    assert result['metadata']['analysis']['support_models'] == ['jrsnap_trilinear_v1']


@pytest.mark.parametrize('legacy', [False, True])
@pytest.mark.parametrize('compressed', [False, True])
def test_http_history_including_compressed_transport(legacy, compressed):
    raw = legacy_jrsnap_data() if legacy else jrsnap_model_data()
    client = app.test_client()
    if compressed:
        data = base64.b64encode(json.dumps(list(gzip.compress(json.dumps(raw).encode()))).encode())
        reply = client.post('/', data=data, headers={'Content-Encoding': 'gzip'})
        assert reply.status_code == 200, reply.data
        result = json.loads(gzip.decompress(base64.b64decode(reply.data)))
    else:
        reply = client.post('/', json=raw)
        assert reply.status_code == 200, reply.data
        result = reply.get_json()
    steps = load_step_results(result)
    assert len(steps) == len(FORCES)
    for force, step in zip(FORCES, steps, strict=True):
        deformation = node_components(step)['30']['dx']
        assert reaction_components(step)['30']['fx'] == pytest.approx(-force, abs=1e-9)
        assert step['state']['load_factor'] == pytest.approx(3000*deformation+force, abs=1e-9)
        assert_step_diagnostics(step)


def test_python_file_result_roundtrip_reanalysis_and_hash(tmp_path):
    raw = jrsnap_model_data()
    raw['boundary_conditions'].pop('nonlinear_spring_supports')
    model = FemModel()
    model.read_json_model(_read_json_model(raw))
    model.add_nonlinear_spring_support(30, 'x', trilinear_definition())
    expected = result_to_jsonable(model._run_solver_snapshot())
    assert_history(expected)
    public_result = model.run()
    model.save_model(str(tmp_path/'model.json'))
    model.save_results(str(tmp_path/'result.json'))
    assert json.loads((tmp_path/'result.json').read_text(encoding='utf8')) == public_result
    restored = FemModel()
    restored.load_model(str(tmp_path/'model.json'))
    actual = result_to_jsonable(restored._run_solver_snapshot())
    assert actual['support_response'] == expected['support_response']
    assert actual['metadata']['input_sha256'] == expected['metadata']['input_sha256']
    assert_history(actual)
    # A changed limit changes the input hash.
    restored.boundary.nonlinear_spring_supports[30]['x'] = parse_jrsnap_support(
        dict(trilinear_definition(), F2_pos=15.))
    assert restored._run_solver_snapshot()['metadata']['input_sha256'] != expected['metadata']['input_sha256']


@pytest.mark.parametrize('kind', ['static', 'modal'])
def test_http_rejects_explicit_incompatible_analysis(kind):
    data = jrsnap_model_data()
    data['analysis_type'] = kind
    reply = app.test_client().post('/', json=data)
    assert reply.status_code == 400
    assert reply.get_json()['error_code'] == 'unsupported_analysis'
    assert reply.get_json()['details']['support_nodes'] == [30]


def test_documented_complete_example():
    model = FemModel()
    model.load_model(str(EXAMPLE))
    assert_history(result_to_jsonable(model._run_solver_snapshot()))
    assert json.loads(EXAMPLE.read_text(encoding='utf-8')) == jrsnap_model_data()
