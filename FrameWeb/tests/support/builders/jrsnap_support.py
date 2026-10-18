"""JRSNAP support springs on the parallel-beam models of the slip builder."""
from copy import deepcopy

from tests.support.builders.slip_support import legacy_slip_data, slip_model_data

# Manual 7.22 hand table: the parallel beam adds kb*delta to each support force.
TARGETS = [.005, .02, .05, .044, .038, .03, .02, -.02, -.05, -.04, -.036, -.03, -.02, .02, .03, .06]
FORCES = [5., 12., 12., 6., 0., -4., -5., -6., -7., -2., 0., 6., 10., 12., 12., 11.]


def trilinear_definition():
    return dict(type='jrsnap_trilinear',
                K1_pos=1000., F1_pos=10., K2_pos=200., F2_pos=14., K3_pos=-100.,
                K1_neg=500., F1_neg=5., K2_neg=100., F2_neg=7., K3_neg=0.)


def bilinear_definition():
    return dict(type='jrsnap_bilinear', K1=1000., F1_pos=10., F1_neg=5.)


def jrsnap_model_data(direction='x', definition=None):
    raw = slip_model_data(direction)
    raw['boundary_conditions']['nonlinear_spring_supports']['30'][direction] = (
        trilinear_definition() if definition is None else definition)
    raw['analysis_params']['displacement_control']['targets'] = list(TARGETS)
    return deepcopy(raw)


def legacy_jrsnap_data(definition=None):
    raw = legacy_slip_data()
    raw['fix_node']['1'][1]['tx'] = trilinear_definition() if definition is None else definition
    raw['analysis_params'] = jrsnap_model_data()['analysis_params']
    return raw
