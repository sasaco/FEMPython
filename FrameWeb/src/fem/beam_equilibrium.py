"""Recover statically determinate linear branches without subtracting stiff K*u.

Only an unsupported leaf with exactly one remaining member can determine an
end action. Peel these leaves, transporting both force and moment to the next
node. Stop at supports, foundations, nonlinear elements, shells, and cycles.
The displacement/constitutive solution is retained and checked independently.
"""
from collections import deque
import copy

import numpy as np

from .elements.loaded_bar_element import LoadedBarElement


def recover_free_branches(coords, elements, forces, loads, blocked, factor, tolerance):
    result = copy.deepcopy(forces)
    constrained = (blocked if isinstance(blocked, dict) else
                   {node: set(range(6)) for node in blocked})
    blocked = {node for node, dofs in constrained.items() if len(dofs) == 6}
    incident = {node: set() for node in coords}
    eligible = {}
    for key, element in elements.items():
        if (isinstance(element, LoadedBarElement) and
                not np.any(element.foundation) and key in forces):
            eligible[key] = element
            for node in element.node_ids:
                incident[node].add(key)
        else:
            blocked.update(element.node_ids)
    remaining = {node: np.asarray(loads.get(node, np.zeros(6)), dtype=float).copy()
                 for node in coords}
    # Use the structure's force scale, as the global equilibrium solve does.
    # A lightly loaded/short leaf can otherwise fail a local-relative test
    # despite an acceptable global residual on the connected stiff structure.
    force_scale = 1.
    for key, element in eligible.items():
        units = np.tile(np.r_[np.ones(3), np.full(3, 1/element.length)], 2)
        raw = np.r_[forces[key]['i_end'], forces[key]['j_end']]
        force_scale = max(force_scale, np.max(np.abs(raw*units)))
        for node in element.node_ids:
            force_scale = max(force_scale, np.max(np.abs(remaining[node]*units[:6])))
    queue = deque(node for node in coords if node not in blocked and len(incident[node]) == 1)
    recovered = []
    while queue:
        node = queue.popleft()
        if node in blocked or len(incident[node]) != 1:
            continue
        key = next(iter(incident[node]))
        element = eligible[key]
        ni, nj = element.node_ids
        other = nj if node == ni else ni
        # These are elastic nodal actions: consistent member loads are already
        # in the global external vector. They obey homogeneous rigid-body work.
        near = remaining[node].copy()
        # A partially supported leaf determines only its free components.
        # Retain constitutive actions in restrained directions (including the
        # auxiliary out-of-plane restraints of a planar frame).
        raw_local = np.r_[forces[key]['i_end'], forces[key]['j_end']]
        raw_elastic = raw_local + element.member_load_at(factor)
        raw_global = element.get_transformation_matrix(12).T@raw_elastic
        offset = 0 if node == ni else 6
        for dof in constrained.get(node, ()):
            near[dof] = raw_global[offset+dof]
        far = -near.copy()
        far[3:] -= np.cross(np.asarray(coords[node])-coords[other], near[:3])
        global_force = np.r_[near, far] if node == ni else np.r_[far, near]
        local = element.get_transformation_matrix(12) @ global_force
        local -= element.member_load_at(factor)
        previous = np.r_[forces[key]['i_end'], forces[key]['j_end']]
        # Compare forces and moments in force units. A recovery cannot turn an
        # unconverged constitutive solution into an accepted result.
        units = np.tile(np.r_[np.ones(3), np.full(3, 1/element.length)], 2)
        if np.max(np.abs((local-previous)*units)) > 10*tolerance*force_scale:
            raise ValueError(f'Beam {key} equilibrium recovery exceeds constitutive tolerance')
        result[key] = dict(i_end=local[:6].copy(), j_end=local[6:].copy())
        recovered.append(key)
        remaining[other] -= far
        incident[node].remove(key)
        incident[other].remove(key)
        if other not in blocked and len(incident[other]) == 1:
            queue.append(other)
    return result, recovered
