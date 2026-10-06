"""Desktop bridge loading is 3D, case-local and audited in public coordinates."""
from copy import deepcopy
import json
import math
from pathlib import Path

import pytest

from fem.analysis_result_sets import build_analysis_result_set
from fem.file_io import _read_json_model, model_to_jsonable
from fem.legacy_beam import select_case
from fem.result_contracts import ResultContractError, validate_analysis_result_set
from tests.support.builders.spatial_loads import legacy_panel


def bridge_request(*, area=False):
    data = legacy_panel(area=area)
    spatial = model_to_jsonable(_read_json_model(data))['spatial_loads']
    data['load']['1'].pop('load_inf')
    data['load']['1']['spatial_loads'] = spatial
    data['dimension'] = 3
    data['boundary_conditions'] = {'restraints': {
        key: {'dof': [True]*6} for key in data['node']}}
    return data


@pytest.mark.parametrize('area,total', [(False, 30), (True, 50)])
def test_bridge_only_and_mixed_cases_are_isolated(area, total):
    data = bridge_request(area=area)
    data['load']['2'] = {'load_node': [{'n': 2, 'tz': -1}]}
    data['load']['3'] = deepcopy(data['load']['1'])
    data['load']['3']['load_node'] = []
    data['load']['3']['spatial_loads']['loads'][0]['end_intensities'] = [
        [-2*value for value in pair]
        for pair in data['load']['3']['spatial_loads']['loads'][0]['end_intensities']]
    original = deepcopy(data)
    result = build_analysis_result_set(data)
    assert data == original
    reactions = [sum(row['components']['fz'] for row in item['support_reactions'])
                 for item in result['results']]
    assert reactions == pytest.approx([3-total, 1, 2*total])
    first, ordinary, third = result['results']
    assert 'spatial_loads' not in ordinary['diagnostics']
    for item, expected in ((first, total), (third, -2*total)):
        audit = item['diagnostics']['spatial_loads']
        assert audit['resultant'] == pytest.approx(dict(x=0, y=0, z=expected))
        assert audit['nodal_resultant'] == pytest.approx(audit['resultant'])
        assert sum(n['force']['z'] for n in audit['node_loads']) == pytest.approx(expected)
        assert audit['force_error'] < 1e-10
        load = audit['loads'][0]
        assert load['feature'] == ('spatial_area' if area else 'spatial_line')
        assert load['clipped_area' if area else 'integrated_length'] == pytest.approx(2)


def test_case_local_shell_ids_are_remapped_and_root_ids_keep_existing_semantics():
    data = bridge_request()
    data['shell'] = {'1': dict(nodes=[1, 2, 3, 4], e=1, t=.1)}
    spatial = data['load']['1']['spatial_loads']
    spatial['panels'][0].update(elements=[1], triangles=[])
    parsed = _read_json_model(data)
    assert parsed['boundary'].spatial_loads.panels[0].elements == (5,)
    assert parsed['mesh'].elements[1]['type'] == 'bar'
    assert parsed['mesh'].elements[5]['shell_id'] == 1
    build_analysis_result_set(data)
    data['spatial_loads'] = data['load']['1'].pop('spatial_loads')
    with pytest.raises(ValueError, match='shell element 1'):
        _read_json_model(data)
    data['spatial_loads']['panels'][0]['elements'] = [5]
    assert _read_json_model(data)['boundary'].spatial_loads.panels[0].elements == (5,)


@pytest.mark.parametrize('other', ['root', 'legacy'])
def test_ambiguous_representations_fail(other):
    data = bridge_request()
    if other == 'root':
        data['spatial_loads'] = deepcopy(data['load']['1']['spatial_loads'])
    else:
        data['load']['1']['load_inf'] = [dict(L1=1, P11=1, P12=1)]
    with pytest.raises(ValueError, match='both'):
        _read_json_model(data)


@pytest.mark.parametrize('representation', ['case', 'root', 'legacy', 'normalized'])
def test_explicit_2d_spatial_requests_are_rejected(representation):
    data = bridge_request()
    if representation == 'root':
        data['spatial_loads'] = data['load']['1'].pop('spatial_loads')
    elif representation == 'legacy':
        data = legacy_panel()
    elif representation == 'normalized':
        data = model_to_jsonable(_read_json_model(data))
    data['dimension'] = 2
    with pytest.raises(ValueError, match='3D'):
        _read_json_model(data)


def test_other_case_is_not_parsed_and_existing_root_behavior_is_preserved():
    data = bridge_request()
    data['load']['2'] = {'spatial_loads': {'invalid': True}}
    assert len(_read_json_model(select_case(data, '1'))['boundary'].spatial_loads.loads) == 1
    data['load']['2'] = {'load_node': [{'n': 2, 'tz': -1}]}
    data['spatial_loads'] = data['load']['1'].pop('spatial_loads')
    result = build_analysis_result_set(data)
    assert [sum(row['components']['fz'] for row in item['support_reactions'])
            for item in result['results']] == pytest.approx([-27, -29])


def test_inclined_plane_and_public_generated_node_ids_are_preserved_in_audit():
    data = bridge_request()
    cosine, sine = math.cos(.4), math.sin(.4)
    for node in data['node'].values():
        node['y'], node['z'] = node['y']*cosine, node['y']*sine
    spatial = data['load']['1']['spatial_loads']
    spatial['panels'][0]['plane'] = dict(origin=[0, 0, 0], axis_u=[1, 0, 0], axis_v=[0, cosine, sine])
    for path in spatial['paths']:
        path['points'] = [[x, y*cosine, y*sine] for x, y, _ in path['points']]
    spatial['loads'][0]['direction'] = dict(mode='normal')
    data['notice_points'] = [{'m': '1', 'Points': [1]}]
    result = build_analysis_result_set(data)
    audit = result['results'][0]['diagnostics']['spatial_loads']
    assert audit['resultant'] == pytest.approx(dict(x=0, y=-30*sine, z=30*cosine))
    assert {row['node_id'] for row in audit['node_loads']} == {
        row['node_id'] for row in result['topology']['nodes']}
    assert any(node['generated'] for node in result['topology']['nodes'])
    assert audit['loads'][0]['integrated_length'] == pytest.approx(2)


@pytest.fixture
def audited_result():
    return build_analysis_result_set(bridge_request())


@pytest.mark.parametrize('mutation', [
    lambda a: a['node_loads'][0].update(node_id='absent'),
    lambda a: a['node_loads'].append(deepcopy(a['node_loads'][0])),
    lambda a: a['resultant'].update(z=float('nan')),
    lambda a: a.update(force_error=-1),
    lambda a: a['loads'][0].update(feature='unknown'),
    lambda a: a['loads'].append(deepcopy(a['loads'][0])),
    lambda a: a['loads'][0].update(integrated_length=float('inf')),
    lambda a: a.update(extra=1),
])
def test_audit_contract_rejects_malformed_data(audited_result, mutation):
    mutation(audited_result['results'][0]['diagnostics']['spatial_loads'])
    with pytest.raises(ResultContractError):
        validate_analysis_result_set(audited_result)


@pytest.mark.parametrize('fixture', ['nonlinear-steps', 'modal'])
def test_nonstatic_audit_is_rejected(audited_result, fixture):
    path = Path(__file__).parents[1] / 'data' / 'contracts' / 'positive' / f'{fixture}.json'
    data = json.loads(path.read_text(encoding='utf8'))
    data['results'][0]['diagnostics']['spatial_loads'] = audited_result['results'][0]['diagnostics']['spatial_loads']
    with pytest.raises(ResultContractError, match='extra'):
        validate_analysis_result_set(data)


def test_null_audit_is_rejected(audited_result):
    audited_result['results'][0]['diagnostics']['spatial_loads'] = None
    with pytest.raises(ResultContractError):
        validate_analysis_result_set(audited_result)
