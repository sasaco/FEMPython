"""Registry of grounded nonlinear support-spring laws, keyed by definition type."""
from dataclasses import dataclass
from typing import Callable

from .hysteresis.jrsnap_support import (
    BILINEAR, TRILINEAR, JrsnapSupportParams, JrsnapSupportState,
    evaluate_jrsnap_support, jrsnap_support_points_passed, parse_jrsnap_support)
from .hysteresis.slip import SlipSpringParams, SlipSpringState, evaluate_slip, parse_slip_spring


@dataclass(frozen=True)
class SupportLaw:
    type: str
    params_type: type
    state_type: type
    parse: Callable
    evaluate: Callable
    model_id: str
    # Skeleton points passed by the largest deformation so far (GLOSSARY: 部材の状態);
    # None for a law without a skeleton (slip).
    points_passed: Callable | None = None


LAWS = {law.type: law for law in (
    SupportLaw('slip', SlipSpringParams, SlipSpringState, parse_slip_spring, evaluate_slip, 'slip_v1'),
    SupportLaw(BILINEAR, JrsnapSupportParams, JrsnapSupportState, parse_jrsnap_support,
               evaluate_jrsnap_support, 'jrsnap_bilinear_v1', jrsnap_support_points_passed),
    SupportLaw(TRILINEAR, JrsnapSupportParams, JrsnapSupportState, parse_jrsnap_support,
               evaluate_jrsnap_support, 'jrsnap_trilinear_v1', jrsnap_support_points_passed),
)}


def law_of(params):
    """The law of parsed parameters; slip parameters carry no type field."""
    return LAWS['slip' if isinstance(params, SlipSpringParams) else params.type]


def parse_support_law(value, *, location):
    """Dispatch a raw definition or parameter instance to its strict parser."""
    if isinstance(value, (SlipSpringParams, JrsnapSupportParams)):
        return value
    law = LAWS.get(value.get('type')) if isinstance(value, dict) else None
    if law is None:
        raise ValueError(f'{location}: expected a nonlinear support object with type '
                         + ', '.join(f'"{name}"' for name in LAWS))
    return law.parse(value, location=location)


def initial_state(params):
    return law_of(params).state_type.initial(params)


def support_models(definitions):
    """Sorted unique model IDs of the laws used by the given definitions."""
    return sorted({law_of(params).model_id for params in definitions})
