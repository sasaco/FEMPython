"""Hand tables and frozen JRSNAP runs for the JRSNAP multi-linear support law (manual 7.22)."""
from dataclasses import FrozenInstanceError
import json

import numpy as np
import pytest

from fem.nonlinear.hysteresis.jrsnap_support import (
    JrsnapSupportState, evaluate_jrsnap_support, parse_jrsnap_support)
from tests.support.paths import DATA

pytestmark = pytest.mark.unit

TRILINEAR = dict(type='jrsnap_trilinear',
                 K1_pos=1000., F1_pos=10., K2_pos=200., F2_pos=14., K3_pos=-100.,
                 K1_neg=500., F1_neg=5., K2_neg=100., F2_neg=7., K3_neg=0.)

# Manual 7.22 hand table: (deformation, force, zero-force position, branch).
TABLE = [
    (.005, 5., 0., 'elastic'),
    (.02, 12., .008, 'skeleton_pos'),
    (.05, 12., .038, 'skeleton_pos'),
    (.044, 6., .038, 'elastic'),
    (.038, 0., .038, 'elastic'),
    (.03, -4., .038, 'elastic'),
    (.02, -5., .03, 'plateau_neg'),
    (-.02, -6., -.008, 'skeleton_neg'),
    (-.05, -7., -.036, 'skeleton_neg'),
    (-.04, -2., -.036, 'elastic'),
    (-.036, 0., -.036, 'elastic'),
    (-.03, 6., -.036, 'elastic'),
    (-.02, 10., -.03, 'plateau_pos'),
    # The degradation memory D+ = Q+(.05) = 12 caps the skeleton (14 without it).
    (.02, 12., .008, 'plateau_pos'),
    (.03, 12., .018, 'plateau_pos'),
    (.06, 11., .049, 'skeleton_pos'),
]


def trilinear(**changes):
    return parse_jrsnap_support({**TRILINEAR, **changes})


def run(params, path, state=None):
    state = JrsnapSupportState.initial(params) if state is None else state
    states = []
    for delta in path:
        state = evaluate_jrsnap_support(params, state, delta)
        states.append(state)
    return states


def work(params, state, path):
    """Trapezoids between the end points of affine pieces are exact integrals."""
    total = 0.
    for delta in path:
        new = evaluate_jrsnap_support(params, state, delta)
        total += .5*(state.force+new.force)*(delta-state.deformation)
        state = new
    return total, state


@pytest.mark.parametrize('scale', [1., 1e-3, 1e3])
def test_hand_calculated_trilinear_history_and_unit_conversion(scale):
    params = parse_jrsnap_support({
        key: value/scale if key.startswith('K') else value for key, value in TRILINEAR.items()})
    states = run(params, [delta*scale for delta, *_ in TABLE])
    for state, (delta, force, zero, branch) in zip(states, TABLE):
        assert state.force == pytest.approx(force, abs=1e-12), delta
        assert state.zero_position == pytest.approx(zero*scale, abs=1e-15*scale), delta
        assert state.branch == branch, delta
    assert states[-1].delta_max_pos == pytest.approx(.06*scale)
    assert states[-1].delta_max_neg == pytest.approx(-.05*scale)


def test_hand_table_tangents_follow_the_branch_and_force_sign():
    params = trilinear()
    tangents = [state.tangent for state in run(params, [delta for delta, *_ in TABLE])]
    assert tangents == [1000., 200., -100., 1000., 500., 500., 0., 100., 0.,
                        500., 1000., 1000., 0., 0., 0., -100.]


def test_closed_loop_inside_extrema_dissipates_positive_work():
    params = trilinear()
    state = run(params, [delta for delta, *_ in TABLE[:9]])[-1]
    assert (state.deformation, state.force, state.zero_position) == pytest.approx((-.05, -7., -.036))
    assert evaluate_jrsnap_support(params, state, -.026).force == pytest.approx(10.)
    # -.05 -> -.02 reaches F1+ = 10 at -.026; returning, E = -10 meets L = -7.
    total, end = work(params, state, [-.036, -.026, -.02, -.03, -.044, -.05])
    assert total == pytest.approx(.102)
    assert end.force == pytest.approx(-7.)
    assert end.zero_position == pytest.approx(-.036)
    # A loop that never touches U or L stays on the elastic lines and does no work.
    start = evaluate_jrsnap_support(params, end, -.036)
    elastic, _ = work(params, start, [-.03, -.036, -.045, -.036])
    assert elastic == pytest.approx(0., abs=1e-15)


def test_asymmetric_bilinear_is_elastic_perfectly_plastic():
    params = parse_jrsnap_support(dict(type='jrsnap_bilinear', K1=1000., F1_pos=10., F1_neg=5.))
    states = run(params, [.005, .02, .015, -.01, 0., .03, .1])
    assert [s.force for s in states] == pytest.approx([5., 10., 5., -5., 5., 10., 10.])
    assert [s.branch for s in states] == ['elastic', 'plateau_pos', 'elastic', 'plateau_neg',
                                         'elastic', 'plateau_pos', 'plateau_pos']
    assert [s.tangent for s in states] == [1000., 0., 1000., 0., 1000., 0., 0.]
    assert states[3].zero_position == pytest.approx(-.005)


def test_softening_to_zero_force_leaves_the_side_inactive():
    params = trilinear()
    states = run(params, [.1, .17, .2, .195, .25, .3])
    assert [s.force for s in states] == pytest.approx([7., 0., 0., -2.5, 0., 0.], abs=1e-12)
    assert [s.branch for s in states] == ['skeleton_pos', 'zero_pos', 'zero_pos', 'elastic',
                                         'zero_pos', 'zero_pos']
    assert [s.tangent for s in states] == [-100., 0., 0., 500., 0., 0.]
    assert states[-1].zero_position == pytest.approx(.3)


def test_cut_off_side_moves_the_zero_force_position_without_resistance():
    params = trilinear(F1_pos=0., F2_pos=0., K3_pos=0.)
    assert JrsnapSupportState.initial(params).tangent == 500.
    states = run(params, [.01, .005, -.02, 0.])
    assert [s.force for s in states] == pytest.approx([0., -2.5, -6., 0.])
    assert [s.zero_position for s in states] == pytest.approx([.01, .01, -.008, 0.])
    assert [s.branch for s in states] == ['zero_pos', 'elastic', 'skeleton_neg', 'zero_pos']
    bilinear = parse_jrsnap_support(dict(type='jrsnap_bilinear', K1=1000., F1_pos=10., F1_neg=0.))
    states = run(bilinear, [.02, -.01, 0., .005])
    assert [s.force for s in states] == pytest.approx([10., 0., 10., 10.])
    assert [s.branch for s in states] == ['plateau_pos', 'zero_neg', 'plateau_pos', 'plateau_pos']


def _skeleton(magnitude, K1, F1, K2, F2, K3):
    """Independent force-limit skeleton magnitude for monotone loading."""
    x1 = F1/K1
    x2 = x1+(F2-F1)/K2
    if magnitude <= x1:
        return K1*magnitude
    if magnitude <= x2:
        return F1+K2*(magnitude-x1)
    return max(0., F2+K3*(magnitude-x2))


POS = (1000., 10., 200., 14., -100.)
NEG = (500., 5., 100., 7., 0.)


@pytest.mark.parametrize('sign, side', [(1, POS), (-1, NEG)])
def test_monotone_loading_follows_the_skeleton_including_break_points(sign, side):
    params = trilinear()
    grid = sorted({*np.linspace(0., .25, 51), .01, .03, .17})
    for delta in grid:
        direct = evaluate_jrsnap_support(params, JrsnapSupportState.initial(params), sign*delta)
        assert direct.force == pytest.approx(sign*_skeleton(delta, *side), abs=1e-12)
    states = run(params, [sign*delta for delta in grid])
    assert [s.force for s in states] == pytest.approx([sign*_skeleton(d, *side) for d in grid], abs=1e-12)


def _limits(params, state, delta):
    """Independent U and L from manual 7.22 for the committed extrema of state."""
    pos = (params.K1_pos, params.F1_pos, params.K2_pos, params.F2_pos, params.K3_pos)
    neg = (params.K1_neg, params.F1_neg, params.K2_neg, params.F2_neg, params.K3_neg)

    def limit(side, extreme, y):
        K1, F1, K2, F2, K3 = side
        x2 = F1/K1+(F2-F1)/K2
        memory = _skeleton(extreme, *side) if K3 < 0 and extreme > x2 else float('inf')
        return min(memory, F1 if y < F1/K1 else _skeleton(y, *side))

    return (-limit(neg, -state.delta_max_neg, -delta), limit(pos, state.delta_max_pos, delta))


def test_random_paths_stay_within_limits_and_are_subdivision_invariant():
    params = trilinear()
    rng = np.random.default_rng(722)
    targets = np.cumsum(rng.normal(0., .03, 60))
    direct = divided = JrsnapSupportState.initial(params)
    for target in targets:
        for value in np.linspace(divided.deformation, target, 7)[1:]:
            lower, upper = _limits(params, divided, value)
            divided = evaluate_jrsnap_support(params, divided, value)
            assert lower-1e-12 <= divided.force <= upper+1e-12
        direct = evaluate_jrsnap_support(params, direct, target)
        assert direct.force == pytest.approx(divided.force, abs=1e-11)
        assert direct.zero_position == pytest.approx(divided.zero_position, abs=1e-14)
        assert (direct.delta_max_pos, direct.delta_max_neg) == (divided.delta_max_pos, divided.delta_max_neg)
        assert direct.tangent == pytest.approx(divided.tangent)


def test_trial_is_pure_and_zero_step_returns_the_committed_state():
    params = trilinear()
    initial = JrsnapSupportState.initial(params)
    assert initial.tangent == 500.
    oversized = evaluate_jrsnap_support(params, initial, 10.)
    accepted = evaluate_jrsnap_support(params, initial, .02)
    assert initial == JrsnapSupportState.initial(params)
    assert oversized.delta_max_pos == 10.
    assert accepted.delta_max_neg == 0.
    assert evaluate_jrsnap_support(params, accepted, .02) is accepted
    assert evaluate_jrsnap_support(params, initial, .02) == accepted
    with pytest.raises(FrozenInstanceError):
        accepted.force = 99.


def test_direction_hint_selects_the_one_sided_tangent_without_moving_the_state():
    params = trilinear()
    at_break = run(params, [.01])[-1]
    assert at_break.force == 10.
    assert evaluate_jrsnap_support(params, at_break, .01, direction_hint=1).tangent == 200.
    reverse = evaluate_jrsnap_support(params, at_break, .01, direction_hint=-1)
    assert (reverse.force, reverse.tangent, reverse.branch) == (10., 1000., 'elastic')
    assert reverse.zero_position == at_break.zero_position
    initial = JrsnapSupportState.initial(params)
    assert evaluate_jrsnap_support(params, initial, 0., direction_hint=1).tangent == 1000.
    assert evaluate_jrsnap_support(params, initial, 0., direction_hint=-1).tangent == 500.
    on_limit = run(params, [-.02])[-1]
    assert evaluate_jrsnap_support(params, on_limit, -.02, direction_hint=-1).tangent == 100.
    assert evaluate_jrsnap_support(params, on_limit, -.02, direction_hint=1).tangent == 500.


@pytest.mark.parametrize('prefix, delta, tangent', [
    ([.05], .044, 1000.), ([.05], .03, 500.), ([.05], -.02, 100.), ([.05], -.07, 0.),
    # Reloading after .05 is capped flat by D+ = 12 between .02 and .03.
    ([.05, .038, -.01], .025, 0.)])
def test_tangent_is_the_force_derivative_inside_each_branch(prefix, delta, tangent):
    params = trilinear()
    state = run(params, prefix)[-1]
    h = 1e-7
    response = evaluate_jrsnap_support(params, state, delta)
    derivative = (evaluate_jrsnap_support(params, state, delta+h).force
                  - evaluate_jrsnap_support(params, state, delta-h).force)/(2*h)
    assert response.tangent == tangent
    assert derivative == pytest.approx(tangent, abs=1e-6)


def test_omitted_negative_stiffness_equals_the_explicit_positive_value():
    omitted = {key: value for key, value in TRILINEAR.items() if key not in ('K1_neg', 'K2_neg', 'K3_neg')}
    explicit = {**omitted, 'K1_neg': 1000., 'K2_neg': 200., 'K3_neg': -100.}
    assert parse_jrsnap_support(omitted) == parse_jrsnap_support(explicit)
    assert parse_jrsnap_support(omitted).to_dict() == explicit
    assert trilinear().to_dict() == TRILINEAR
    bilinear = dict(type='jrsnap_bilinear', K1=800., F1_pos=8., F1_neg=4.)
    parsed = parse_jrsnap_support(bilinear)
    assert parsed.to_dict() == bilinear
    assert parse_jrsnap_support(parsed) is parsed


@pytest.mark.parametrize('changes', [
    dict(K1_pos=0.), dict(K2_neg=0.), dict(K1_pos=-1.), dict(K2_pos=1001.), dict(K3_neg=501.),
    dict(F1_pos=-1.), dict(F2_neg=4.), dict(F1_neg=0.), dict(F1_neg=0., F2_neg=0., K3_neg=1.),
    dict(F1_pos=0., F2_pos=0., K3_pos=0., F1_neg=0., F2_neg=0.),
    dict(K1_pos=True), dict(F1_pos='10'), dict(K3_pos=float('nan')), dict(F2_pos=float('inf')),
    dict(K1_pos=1e-300, K2_pos=1e-300, K3_pos=0., F1_pos=1e300, F2_pos=1e300), dict(gap=0.),
])
def test_invalid_trilinear_definitions_are_rejected_with_location(changes):
    with pytest.raises(ValueError, match='here'):
        parse_jrsnap_support({**TRILINEAR, **changes}, location='here')


def test_missing_keys_and_inherited_cut_off_stiffness_are_explained():
    for key in ('F1_neg', 'F2_neg', 'K3_pos'):
        with pytest.raises(ValueError, match='here.*'+key):
            parse_jrsnap_support({k: v for k, v in TRILINEAR.items() if k != key}, location='here')
    inherited = {**TRILINEAR, 'K3_pos': 50., 'F1_neg': 0., 'F2_neg': 0.}
    del inherited['K3_neg']
    with pytest.raises(ValueError, match='K3_neg'):
        parse_jrsnap_support(inherited)
    with pytest.raises(ValueError, match='F1=0'):
        parse_jrsnap_support({**TRILINEAR, 'K1_neg': 0.})


@pytest.mark.parametrize('value', [
    dict(type='jrsnap_bilinear', K1=0., F1_pos=1., F1_neg=1.),
    dict(type='jrsnap_bilinear', K1=1., F1_pos=0., F1_neg=0.),
    dict(type='jrsnap_bilinear', K1=1., F1_pos=1.),
    dict(type='jrsnap_bilinear', K1=1., F1_pos=1., F1_neg=1., K2=1.),
    dict(type='jrsnap_bilinear', K1_pos=1., F1_pos=1., F1_neg=1.),
    dict(type='slip', K1=1., K2=0., delta_1=1.), [], None,
])
def test_invalid_bilinear_definitions_are_rejected_with_location(value):
    with pytest.raises(ValueError, match='here'):
        parse_jrsnap_support(value, location='here')


@pytest.mark.parametrize('value', [float('nan'), float('inf'), True, '.01'])
def test_invalid_trial_is_rejected(value):
    params = trilinear()
    with pytest.raises(ValueError):
        evaluate_jrsnap_support(params, JrsnapSupportState.initial(params), value)


# Frozen JRSNAP 5.2-L03 runs (tests/data/jrsnap_golden/README.md).
GOLDEN = DATA/'jrsnap_golden'
BLOCK6 = ('K1_pos', 'F1_pos', 'F1_neg', 'K2_pos', 'F2_pos', 'F2_neg', 'K3_pos', 'K1_neg', 'K2_neg', 'K3_neg')


def _golden(case, run_name):
    golden = json.loads((GOLDEN/case/f'{run_name}.json').read_text(encoding='utf-8'))
    ndu = (GOLDEN/case/golden['provenance']['inputs']['ndu']['file']).read_bytes().decode('cp932')
    row = next(line for line in ndu.splitlines() if line.startswith('SuppotInfo1='))
    # BLOCK6: iS, iE, iK, then the ten spring values in JRSNAP order.
    values = [float(value) for value in row.split('=', 1)[1].split(',')[3:]]
    params = parse_jrsnap_support(dict(type='jrsnap_trilinear', **dict(zip(BLOCK6, values))))
    # Support fields are indexed by support row (row 1 = node 2 X), displacements by joint.
    path = [(s['joints']['2']['dx'], s['joints']['1']['jiban_hanryoku'], s['joints']['1']['bane_flg'])
            for s in golden['steps']]
    return params, path


def _segment(params, state):
    """JRSNAP bane_flg (0, ±1, ±2) from the branch and the side's second break point."""
    if state.branch == 'elastic':
        return 0
    side = 'pos' if state.branch.endswith('pos') else 'neg'
    sign = 1 if side == 'pos' else -1
    K1, F1, K2, F2 = (getattr(params, f'{k}_{side}') for k in ('K1', 'F1', 'K2', 'F2'))
    x2 = F1/K1+(F2-F1)/K2
    assert state.branch == f'skeleton_{side}'
    return sign*(1 if abs(state.deformation) < x2 else 2)


@pytest.mark.oracle
@pytest.mark.parametrize('case, run_name', [('support_trilinear01', 'golden_n100'),
                                            ('support_trilinear01', 'golden_n1000'),
                                            ('support_trilinear03_softening', 'golden_n200')])
def test_positive_side_reproduces_the_frozen_jrsnap_spring_force(case, run_name):
    params, path = _golden(case, run_name)
    state = JrsnapSupportState.initial(params)
    for dx, force, segment in path:
        state = evaluate_jrsnap_support(params, state, dx)
        assert state.force == pytest.approx(force, rel=1e-12, abs=1e-12)
        assert _segment(params, state) == segment


@pytest.mark.oracle
def test_negative_side_switches_segments_where_jrsnap_does():
    # JRSNAP reports F_neg(2|dx|) on this side; only the break positions are compared.
    params, path = _golden('support_trilinear02_neg', 'golden_n100')
    state = JrsnapSupportState.initial(params)
    for dx, _, segment in path:
        state = evaluate_jrsnap_support(params, state, dx)
        assert _segment(params, state) == segment
