"""Load kinds of a static nonlinear analysis and the load they apply at a factor.

A static nonlinear analysis applies, at every step with load factor lambda,

    F(lambda) = F_fixed + lambda*F_incremental + min(lambda/limit, 1)*F_limited

(GLOSSARY: 固定荷重・増分荷重・限定増分荷重). A limited load grows in
proportion to lambda, reaches its full input value at lambda = limit and stays
there, as JRSNAP's limited incremental load (LiMiT3) does. A load without a kind is
incremental, so every analysis without kinds keeps F(lambda) = lambda*F.
"""

from __future__ import annotations

from collections.abc import Mapping
from dataclasses import dataclass
from typing import Any

import numpy as np

FIXED = 'fixed'
INCREMENTAL_NAME = 'incremental'
LIMITED = 'limited'
KIND_NAMES = (FIXED, INCREMENTAL_NAME, LIMITED)

# Internal row key written by nonlinear_combine selection; never public input.
ROW_KEY = '_kind'


@dataclass(frozen=True)
class LoadKind:
    """How one load scales with the load factor lambda."""

    name: str
    limit: float | None = None

    def __post_init__(self):
        if self.name not in KIND_NAMES:
            raise ValueError(f'Unknown load kind: {self.name!r}; expected one of {list(KIND_NAMES)}')
        if self.name == LIMITED:
            if (isinstance(self.limit, bool) or not isinstance(self.limit, (int, float))
                    or not np.isfinite(self.limit) or self.limit <= 0):
                raise ValueError('A limited load requires a finite positive limit')
            object.__setattr__(self, 'limit', float(self.limit))
        elif self.limit is not None:
            raise ValueError(f'Only a limited load has a limit, not a {self.name} load')

    def factor(self, load_factor: float) -> float:
        if self.name == FIXED:
            return 1.0
        if self.name == LIMITED:
            return min(load_factor/self.limit, 1.0)
        return load_factor

    def rate(self, load_factor: float) -> float:
        """d factor / d lambda; a limited load stops at its limit."""
        if self.name == FIXED:
            return 0.0
        if self.name == LIMITED:
            return 1.0/self.limit if load_factor < self.limit else 0.0
        return 1.0

    def to_json(self) -> dict[str, Any]:
        return {'kind': self.name, **({'limit': self.limit} if self.name == LIMITED else {})}

    @classmethod
    def from_json(cls, value: Mapping[str, Any]) -> 'LoadKind':
        if not isinstance(value, Mapping):
            raise ValueError('A load kind must be an object')
        return cls(value.get('kind', INCREMENTAL_NAME), value.get('limit'))


INCREMENTAL = LoadKind(INCREMENTAL_NAME)


def row_kind(row: Mapping[str, Any]) -> LoadKind:
    """The kind tagged on a selected load row, incremental when untagged."""
    return LoadKind.from_json(row[ROW_KEY]) if ROW_KEY in row else INCREMENTAL


class LoadPattern:
    """External load vectors grouped by kind; F(lambda) and dF/dlambda."""

    def __init__(self, vectors: Mapping[LoadKind, np.ndarray]):
        self.vectors = dict(vectors)

    @property
    def reference(self) -> np.ndarray:
        """Every load at its full input value (lambda = 1, no limit)."""
        return _total(self.vectors.values())

    @property
    def has_fixed(self) -> bool:
        return any(kind.name == FIXED for kind in self.vectors)

    @property
    def varying(self) -> np.ndarray:
        """The loads that change with lambda before any limit is reached."""
        return _total(v for kind, v in self.vectors.items() if kind.name != FIXED)

    def at(self, load_factor: float) -> np.ndarray:
        return _total(kind.factor(load_factor)*v for kind, v in self.vectors.items())

    def rate(self, load_factor: float) -> np.ndarray:
        return _total(kind.rate(load_factor)*v for kind, v in self.vectors.items())


def _total(vectors) -> np.ndarray:
    """Sum without a leading 0, so one vector is returned bitwise unchanged."""
    vectors = list(vectors)
    total = vectors[0]
    for vector in vectors[1:]:
        total = total+vector
    return total
