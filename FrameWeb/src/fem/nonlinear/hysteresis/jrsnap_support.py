"""JRSNAP-compatible bi-/tri-linear grounded support spring (theory manual 7.22).

The asymmetric force-limit skeleton follows JRSNAP BLOCK6 nodal springs. The
elasto-plastic unloading rule and the degradation memory are FrameWeb's own
specification. Every evaluation starts from an immutable committed state.
Positive force is an internal restoring force; the support reaction is -force.
"""
from dataclasses import dataclass, replace
from math import inf, isfinite, ulp
from numbers import Real

BILINEAR = 'jrsnap_bilinear'
TRILINEAR = 'jrsnap_trilinear'
SIDES = ('pos', 'neg')
TRILINEAR_REQUIRED = ('K1_pos', 'F1_pos', 'K2_pos', 'F2_pos', 'K3_pos', 'F1_neg', 'F2_neg')
TRILINEAR_OPTIONAL = ('K1_neg', 'K2_neg', 'K3_neg')
BILINEAR_KEYS = ('K1', 'F1_pos', 'F1_neg')
CUT_OFF_HINT = 'to cut off a side use F1=0 (trilinear: also F2=0, K3<=0)'


def _finite_number(value, name):
    if isinstance(value, bool) or not isinstance(value, Real) or not isfinite(value):
        raise ValueError(f'{name} must be a finite number (not bool)')
    return float(value)


def _at(a, b):
    # Only rounding-sized event snapping, without a unit-dependent dead band.
    return abs(a-b) <= 4*max(ulp(a), ulp(b))


@dataclass(frozen=True)
class _Side:
    """One side's skeleton magnitude Q(y) for y >= 0, built from force limits."""
    K1: float
    F1: float
    K2: float
    F2: float
    K3: float

    @property
    def cut_off(self):
        return self.F1 == 0

    @property
    def x1(self):
        return self.F1/self.K1

    @property
    def x2(self):
        return self.x1+(self.F2-self.F1)/self.K2

    @property
    def xz(self):
        return self.x2+self.F2/-self.K3 if self.K3 < 0 else inf

    def skeleton(self, y):
        if self.K3 < 0 and (y >= self.xz or _at(y, self.xz)):
            return 0.
        if y <= self.x1:
            return self.K1*y
        if y <= self.x2:
            return self.F1+self.K2*(y-self.x1)
        return max(0., self.F2+self.K3*(y-self.x2))

    def memory(self, extreme):
        """Degradation memory D: no strength recovery after softening."""
        return self.skeleton(extreme) if self.K3 < 0 and extreme > self.x2 else inf

    def limit(self, y, extreme, *, plateau_only):
        """Limit magnitude min(D, F1 or Q(y)), its right derivative and kind."""
        memory = self.memory(extreme)
        before_x1 = y < self.x1 and not _at(y, self.x1)
        base = self.F1 if y < self.x1 else self.skeleton(y)
        if before_x1 or plateau_only:
            slope, kind = 0., 'plateau'
        elif y < self.x2 and not _at(y, self.x2):
            slope, kind = self.K2, 'skeleton'
        elif self.K3 < 0 and (y >= self.xz or _at(y, self.xz)):
            slope, kind = 0., 'skeleton'
        else:
            slope, kind = self.K3, 'skeleton'
        if memory < base and not _at(memory, base):
            value, slope, kind = memory, 0., 'plateau'
        elif isfinite(memory) and _at(memory, base):
            # Right derivative of min(D, base) at a tie.
            value = min(memory, base)
            slope, kind = (slope, kind) if slope < 0 else (0., 'plateau')
        else:
            value = base
        return value, slope, ('zero' if value == 0 else kind)


@dataclass(frozen=True)
class JrsnapSupportParams:
    type: str
    K1_pos: float
    F1_pos: float
    K2_pos: float
    F2_pos: float
    K3_pos: float
    K1_neg: float
    F1_neg: float
    K2_neg: float
    F2_neg: float
    K3_neg: float

    def __post_init__(self):
        if self.type not in (BILINEAR, TRILINEAR):
            raise ValueError(f'type must be "{BILINEAR}" or "{TRILINEAR}"')
        for name in ('K1', 'F1', 'K2', 'F2', 'K3'):
            for side in SIDES:
                key = f'{name}_{side}'
                object.__setattr__(self, key, _finite_number(getattr(self, key), key))
        for side in SIDES:
            K1, F1, K2, F2, K3 = (getattr(self, f'{k}_{side}') for k in ('K1', 'F1', 'K2', 'F2', 'K3'))
            if K1 <= 0 or K2 <= 0:
                raise ValueError(f'K1_{side} and K2_{side} must be > 0 (no artificial stiffness; '
                                 f'{CUT_OFF_HINT})')
            if K2 > K1 or K3 > K1:
                raise ValueError(f'K2_{side} and K3_{side} must not exceed K1_{side}')
            if F1 < 0 or F2 < F1:
                raise ValueError(f'requires 0 <= F1_{side} <= F2_{side}')
            if F1 == 0 and (F2 != 0 or K3 > 0):
                raise ValueError(f'a cut-off side (F1_{side}=0) requires F2_{side}=0 and K3_{side} <= 0')
            if self.type == BILINEAR and (K2 != K1 or F2 != F1 or K3 != 0):
                raise ValueError('jrsnap_bilinear is elastic-perfectly plastic (K2=K1, F2=F1, K3=0)')
            side_law = self.side(side)
            derived = (side_law.x1, side_law.x2, side_law.xz if K3 < 0 else 0.)
            if not all(isfinite(v) for v in (*derived, *(K*x for K in (K1, K2, K3) for x in derived))):
                raise ValueError(f'derived break points of the {side} side must be finite')
        if self.F1_pos == 0 and self.F1_neg == 0:
            raise ValueError('both sides are cut off (F1_pos=F1_neg=0); the spring has no resistance')

    def side(self, side):
        return _Side(*(getattr(self, f'{k}_{side}') for k in ('K1', 'F1', 'K2', 'F2', 'K3')))

    def to_dict(self):
        if self.type == BILINEAR:
            return dict(type=BILINEAR, K1=self.K1_pos, F1_pos=self.F1_pos, F1_neg=self.F1_neg)
        return dict(type=TRILINEAR, **{key: getattr(self, key) for key in (
            'K1_pos', 'F1_pos', 'K2_pos', 'F2_pos', 'K3_pos', 'K1_neg', 'F1_neg', 'K2_neg', 'F2_neg', 'K3_neg')})


def parse_jrsnap_support(value, *, location='jrsnap support spring'):
    """One strict definition parser for legacy JSON, normalized JSON and Python."""
    if isinstance(value, JrsnapSupportParams):
        return value
    if not isinstance(value, dict) or value.get('type') not in (BILINEAR, TRILINEAR):
        raise ValueError(f'{location}: expected a {BILINEAR} or {TRILINEAR} object')
    if value['type'] == BILINEAR:
        if set(value) != {'type', *BILINEAR_KEYS}:
            raise ValueError(f'{location}: {BILINEAR} requires exactly type, {", ".join(BILINEAR_KEYS)}')
        K1, F1_pos, F1_neg = (value[key] for key in BILINEAR_KEYS)
        args = (K1, F1_pos, K1, F1_pos, 0., K1, F1_neg, K1, F1_neg, 0.)
    else:
        keys = set(value)-{'type'}
        missing = sorted(set(TRILINEAR_REQUIRED)-keys)
        unknown = sorted(keys-set(TRILINEAR_REQUIRED)-set(TRILINEAR_OPTIONAL))
        if missing or unknown:
            raise ValueError(f'{location}: {TRILINEAR} '
                             + (f'missing {", ".join(missing)}; ' if missing else '')
                             + (f'unknown {", ".join(unknown)}; ' if unknown else '')
                             + f'requires {", ".join(TRILINEAR_REQUIRED)} '
                             + f'and optional {", ".join(TRILINEAR_OPTIONAL)}')
        # JRSNAP: an omitted negative stiffness takes the positive value.
        args = tuple(value.get(f'{k}_neg', value[f'{k}_pos']) if k.startswith('K') and side == 'neg'
                     else value[f'{k}_{side}']
                     for side in SIDES for k in ('K1', 'F1', 'K2', 'F2', 'K3'))
    try:
        return JrsnapSupportParams(value['type'], *args)
    except ValueError as error:
        if (value['type'] == TRILINEAR and 'K3_neg' not in value and value.get('F1_neg') == 0
                and not isinstance(value.get('K3_pos'), bool)
                and isinstance(value.get('K3_pos'), Real) and value['K3_pos'] > 0):
            raise ValueError(f'{location}: {error}; K3_neg inherits K3_pos={value["K3_pos"]}, '
                             'specify K3_neg explicitly') from error
        raise ValueError(f'{location}: {error}') from error


@dataclass(frozen=True)
class JrsnapSupportState:
    deformation: float = 0.
    force: float = 0.
    tangent: float = 0.
    branch: str = 'elastic'
    direction: int = 0
    zero_position: float = 0.
    delta_max_pos: float = 0.
    delta_max_neg: float = 0.

    @classmethod
    def initial(cls, params):
        # An unloaded spring reports the smaller K1 of its resisting sides (rank check).
        return cls(tangent=min(params.side(s).K1 for s in SIDES if not params.side(s).cut_off))


def evaluate_jrsnap_support(params, committed, deformation, *, direction_hint=0):
    """Return a candidate state without advancing history.

    One evaluation is F = min(E, U) when increasing and max(E, L) when
    decreasing; since U and L are no steeper than K1, it is path-split invariant.
    At zero increment the accepted state is retained. An optional predictor
    direction selects a one-sided tangent, without changing its force.
    """
    delta = _finite_number(deformation, 'JRSNAP support deformation')
    increment = delta-committed.deformation
    if increment == 0 and not direction_hint:
        return committed
    direction = 1 if (increment if increment else direction_hint) > 0 else -1
    pos, neg = params.side('pos'), params.side('neg')
    p = committed.zero_position
    plateau_only = params.type == BILINEAR

    if increment == 0:
        elastic = committed.force
    elif _at(delta, p):
        elastic = 0.
    else:
        elastic = (pos.K1 if delta > p else neg.K1)*(delta-p)
    if direction > 0:
        value, slope, kind = pos.limit(delta, committed.delta_max_pos, plateau_only=plateau_only)
        on_limit = elastic >= value or _at(elastic, value)
        side = 'pos'
    else:
        value, slope, kind = neg.limit(-delta, -committed.delta_max_neg, plateau_only=plateau_only)
        value = -value if value else 0.
        on_limit = elastic <= value or _at(elastic, value)
        side = 'neg'
    if on_limit:
        force, tangent, branch = value, slope, f'{kind}_{side}'
        p = delta-force/(pos.K1 if force >= 0 else neg.K1)
    else:
        force, branch = elastic, 'elastic'
        tangent = (pos.K1 if force > 0 else neg.K1 if force < 0
                   else pos.K1 if direction > 0 else neg.K1)
    if not all(isfinite(v) for v in (force, tangent, p)):
        raise ValueError('Non-finite JRSNAP support response')
    return replace(committed, deformation=delta, force=force, tangent=tangent, branch=branch,
                   direction=direction, zero_position=p,
                   delta_max_pos=max(committed.delta_max_pos, delta),
                   delta_max_neg=min(committed.delta_max_neg, delta))
