"""Select one nonlinear_combine row as a single analysis case.

A row (GLOSSARY: 非線形解析の組合せ) has the shape of a ``combine`` row, but its
coefficients ``C<n>`` refer to basic load cases directly, and it is solved as
one static nonlinear analysis instead of superposing linear results. Each basic
case states whether it is a fixed, incremental or limited load (``kind`` and
``limit``). The selected rows of every referenced basic case are scaled by their
coefficient, tagged with that kind and joined into one load case, so the
existing single-case reading path applies them.
"""

from __future__ import annotations

import copy
import re
from collections.abc import Mapping
from typing import Any

from .legacy_beam import select_case
from .load_kinds import INCREMENTAL, ROW_KEY, LoadKind

KEY = 'nonlinear_combine'
ANALYSIS_KEYS = ('n_load_steps', 'max_iterations', 'tolerance', 'load_factors',
                 'displacement_control')
_LABEL_KEYS = ('row', 'name', 'symbol')
_COEFFICIENT = re.compile(r'C(\d+)')
_SHEETS = ('element', 'fix_node', 'joint', 'fix_member')
_NODE_COMPONENTS = ('tx', 'ty', 'tz', 'rx', 'ry', 'rz')
_MEMBER_VALUES = ('P1', 'P2')


def has_combinations(data: Mapping[str, Any]) -> bool:
    rows = data.get(KEY)
    return isinstance(rows, Mapping) and bool(rows)


def basic_case_kind(case_id: str, case: Mapping[str, Any]) -> LoadKind:
    try:
        return LoadKind.from_json(case)
    except ValueError as error:
        raise ValueError(f'Basic case {case_id}: {error}') from error


def reject_kinds_without_combinations(load_cases: Mapping[str, Any]) -> None:
    """Kinds only mean something inside a nonlinear_combine row."""
    for case_id, case in load_cases.items():
        if isinstance(case, Mapping) and ('kind' in case or 'limit' in case):
            raise ValueError(f'Load case {case_id}: kind and limit require nonlinear_combine')


def select_combination(data: dict[str, Any], row_id: Any) -> dict[str, Any]:
    """Return ``data`` reduced to the one load case that row ``row_id`` applies."""
    row = data[KEY][row_id]
    if not isinstance(row, Mapping):
        raise ValueError(f'nonlinear_combine row {row_id} must be an object')
    coefficients = {}
    unknown = []
    for key, value in row.items():
        match = _COEFFICIENT.fullmatch(key)
        if match:
            if isinstance(value, bool) or not isinstance(value, (int, float)):
                raise ValueError(f'nonlinear_combine row {row_id}: {key} must be a number')
            if value:
                coefficients[match.group(1)] = float(value)
        elif key not in _LABEL_KEYS and key not in ANALYSIS_KEYS:
            unknown.append(key)
    if unknown:
        raise ValueError(f'Unknown nonlinear_combine row {row_id} keys: {sorted(unknown)}')
    if not coefficients:
        raise ValueError(f'nonlinear_combine row {row_id} applies no basic case')

    load_cases = data.get('load') or {}
    selected = []
    for number, coefficient in coefficients.items():
        matches = [key for key in load_cases if str(key) == number]
        if len(matches) != 1:
            raise ValueError(f'nonlinear_combine row {row_id}: basic case {number} does not exist')
        case = load_cases[matches[0]]
        selected.append((case, coefficient, basic_case_kind(number, case)))
    for field in _SHEETS:
        sheets = {str(case.get(field, 1)) for case, _, _ in selected}
        if len(sheets) > 1:
            raise ValueError(f'nonlinear_combine row {row_id}: the basic cases must use '
                             f'the same {field} sheet, not {sorted(sheets)}')

    first = selected[0][0]
    combined = {
        'name': row.get('name', str(row_id)),
        'analysis_type': 'material_nonlinear',
        **{field: first[field] for field in _SHEETS if field in first},
        **{key: row[key] for key in ANALYSIS_KEYS if key in row},
        'load_node': [_scaled(item, coefficient, kind, _NODE_COMPONENTS)
                      for case, coefficient, kind in selected
                      for item in case.get('load_node', [])],
        'load_member': [_scaled(item, coefficient, kind, _MEMBER_VALUES)
                        for case, coefficient, kind in selected
                        for item in case.get('load_member', [])],
    }
    result = copy.deepcopy({key: value for key, value in data.items() if key != KEY})
    # Every row splits members at every basic case's load positions, as the
    # basic cases do, so all rows share one result topology.
    result['_all_member_loads'] = [load for case in load_cases.values()
                                   for load in case.get('load_member', [])]
    result['load'] = {str(row_id): combined}
    return select_case(result, str(row_id))


def _scaled(item: Mapping[str, Any], coefficient: float, kind: LoadKind,
            fields: tuple[str, ...]) -> dict[str, Any]:
    scaled = dict(item)
    for field in fields:
        if scaled.get(field) not in (None, ''):
            scaled[field] = coefficient*float(scaled[field])
    if kind != INCREMENTAL:
        scaled[ROW_KEY] = kind.to_json()
    return scaled
