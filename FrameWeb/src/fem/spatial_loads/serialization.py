"""Separate adapters for legacy influence keys and canonical spatial input."""
from collections.abc import Mapping

from .definitions import (
    GeometryTolerance, LoadDirection, LocalPlane, SpatialLoad, SpatialLoadDefinitions, SpatialLoadMeshNode, SpatialLoadPanel,
    SpatialLoadPath, integer_id, sequence,
)
from .validation import validate_references


def _record(value, allowed, required, label):
    if not isinstance(value, Mapping):
        raise ValueError(f'{label}: expected an object')
    unknown = set(value) - set(allowed)
    missing = set(required) - set(value)
    if unknown:
        raise ValueError(f'{label}: unknown keys {sorted(unknown, key=str)}')
    if missing:
        raise ValueError(f'{label}: missing keys {sorted(missing)}')
    return value


def from_json(value, mesh):
    data = _record(value, ('panels', 'paths', 'loads'), (), 'spatial_loads')
    panels, paths, loads = [], [], []
    for item in sequence(data.get('panels', ()), 'panels'):
        item = _record(item, ('id', 'nodes', 'elements', 'triangles', 'holes', 'tolerance', 'plane',
                              'loading_nodes', 'loading_triangles'),
                       ('id', 'nodes'), 'spatial panel')
        label = f"panel {item['id']}"
        tolerance = _record(item.get('tolerance', {}),
                            ('absolute_length', 'relative_length'), (), label + ' tolerance')
        plane = None
        if 'plane' in item:
            fields = ('origin', 'axis_u', 'axis_v')
            plane = LocalPlane(**_record(item['plane'], fields, fields, label + ' plane'))
        loading_nodes = []
        for node in sequence(item.get('loading_nodes', ()), label + ' loading nodes'):
            node = _record(node, ('id', 'point'), ('id', 'point'), label + ' loading node')
            loading_nodes.append(SpatialLoadMeshNode(**node))
        panels.append(SpatialLoadPanel(**{**item, 'tolerance': GeometryTolerance(**tolerance),
                                         'plane': plane, 'loading_nodes': loading_nodes}))
    for item in sequence(data.get('paths', ()), 'paths'):
        item = _record(item, ('id', 'points'), ('id', 'points'), 'spatial path')
        paths.append(SpatialLoadPath(**item))
    for item in sequence(data.get('loads', ()), 'loads'):
        fields = ('id', 'panel_id', 'path_ids', 'end_intensities')
        item = _record(item, (*fields, 'direction'), fields, 'spatial load')
        direction = LoadDirection()
        if 'direction' in item:
            direction = LoadDirection(**_record(item['direction'], ('mode', 'vector'),
                                                ('mode',), f"load {item['id']} direction"))
        loads.append(SpatialLoad(**{**item, 'direction': direction}))
    result = SpatialLoadDefinitions(panels, paths, loads)
    validate_references(result, mesh)
    return result


def to_json(definitions):
    return dict(
        panels=[dict(id=p.id, nodes=list(p.nodes), elements=list(p.elements),
                     triangles=[list(t) for t in p.triangles], holes=[list(ring) for ring in p.holes],
                     **({'plane': dict(origin=list(p.plane.origin), axis_u=list(p.plane.axis_u),
                                       axis_v=list(p.plane.axis_v))} if p.plane is not None else {}),
                     **({'loading_nodes': [dict(id=n.id, point=list(n.point)) for n in p.loading_nodes],
                         'loading_triangles': [list(t) for t in p.loading_triangles]}
                        if p.loading_nodes else {}),
                     tolerance=dict(absolute_length=p.tolerance.absolute_length,
                                    relative_length=p.tolerance.relative_length))
                for p in definitions.panels],
        paths=[dict(id=p.id, points=[list(point) for point in p.points])
               for p in definitions.paths],
        loads=[dict(id=p.id, panel_id=p.panel_id, path_ids=list(p.path_ids),
                    end_intensities=[list(pair) for pair in p.end_intensities],
                    **({'direction': dict(mode=p.direction.mode,
                        **({'vector': list(p.direction.vector)} if p.direction.vector is not None else {}))}
                       if p.direction != LoadDirection() else {}))
               for p in definitions.loads],
    )


def _lookup(records, target, kind):
    if not isinstance(records, Mapping):
        raise ValueError(f'{kind} {target}: expected an ID-indexed object')
    matches = []
    for key, value in records.items():
        # Unreferenced legacy definitions are not parsed or validated.
        try:
            normalized = integer_id(key, kind)
        except ValueError:
            continue
        if normalized == target:
            matches.append(value)
    if len(matches) != 1:
        reason = 'missing' if not matches else 'duplicate normalized ID for'
        raise ValueError(f'{reason} {kind} {target}')
    return matches[0]


def from_legacy(data, mesh, shell_ids):
    """Only the case already chosen by legacy_beam.select_case is consumed."""
    case = next(iter(data.get('load', {}).values()), {})
    records = sequence(case.get('load_inf', ()), 'load_inf')
    if 'spatial_loads' in case:
        if 'spatial_loads' in data or records:
            raise ValueError('Cannot specify both case spatial_loads and root spatial_loads or load_inf')
        return _from_legacy_case_json(case['spatial_loads'], mesh, shell_ids)
    if 'spatial_loads' in data:
        if records:
            raise ValueError('Cannot specify both load_inf and spatial_loads')
        return from_json(data['spatial_loads'], mesh)
    if not records:
        return SpatialLoadDefinitions()
    panel_id = integer_id(case.get('inf_panel', 1), 'panel')
    label = f'panel {panel_id}'
    item = _lookup(data.get('inf_panel', {}), panel_id, 'panel')
    item = _record(item, ('nodes', 'elements', 'triangles', 'holes', 'tolerance', 'name'),
                   ('nodes',), label)
    nodes = tuple(integer_id(n, label) for n in sequence(item['nodes'], label))
    triangles = sequence(item.get('triangles', ()), label)
    elements = tuple(integer_id(e, label) for e in sequence(item.get('elements', ()), label))
    if elements and triangles:
        raise ValueError(f'{label}: specify either shell elements or explicit triangles')
    if elements:
        internal = []
        for eid in elements:
            if eid not in shell_ids:
                raise ValueError(f'{label}: missing legacy shell {eid}')
            internal.append(shell_ids[eid])
        elements = tuple(internal)
    elif not triangles:
        elements = tuple(eid for eid in shell_ids.values()
                         if set(mesh.elements[eid]['nodes']) <= set(nodes))
        if not elements:
            raise ValueError(f'{label}: explicit topology required; no existing shells cover nodes')
    tolerance = _record(item.get('tolerance', {}), ('absolute_length', 'relative_length'),
                        (), label + ' tolerance')
    panel = SpatialLoadPanel(panel_id, nodes, elements, triangles, item.get('holes', ()),
                             GeometryTolerance(**tolerance))
    loads, paths = [], {}
    for index, record in enumerate(records, 1):
        record = _record(record, ('L1', 'P11', 'P12', 'L2', 'P21', 'P22'),
                         ('L1', 'P11', 'P12'), f'load {index}')
        if 'L2' in record and not {'P21', 'P22'} <= set(record):
            raise ValueError(f'load {index}: L2 requires P21 and P22')
        if 'L2' not in record and {'P21', 'P22'} & set(record):
            raise ValueError(f'load {index}: P21/P22 require L2')
        path_ids, intensities = [], []
        for suffix in ('1', '2') if 'L2' in record else ('1',):
            pid = integer_id(record['L' + suffix], f'load {index}')
            path_ids.append(pid)
            intensities.append((record['P' + suffix + '1'], record['P' + suffix + '2']))
            if pid not in paths:
                path = _lookup(data.get('line', {}), pid, 'path')
                path = _record(path, ('position', 'name'), ('position',), f'path {pid}')
                points = []
                for point in sequence(path['position'], f'path {pid}'):
                    point = _record(point, ('x', 'y'), ('x', 'y'), f'path {pid}')
                    points.append((point['x'], point['y'], 0.))
                paths[pid] = SpatialLoadPath(pid, points)
        loads.append(SpatialLoad(index, panel_id, path_ids, intensities))
    result = SpatialLoadDefinitions((panel,), tuple(paths.values()), loads)
    validate_references(result, mesh)
    return result


def _from_legacy_case_json(value, mesh, shell_ids):
    """Case-local normalized input uses the desktop's public shell namespace."""
    data = _record(value, ('panels', 'paths', 'loads'), (), 'spatial_loads')
    panels = []
    for panel in sequence(data.get('panels', ()), 'panels'):
        if not isinstance(panel, Mapping):
            raise ValueError('spatial panel: expected an object')
        mapped = dict(panel)
        if 'elements' in mapped:
            elements = []
            for value in sequence(mapped['elements'], 'panel elements'):
                public_id = integer_id(value, 'legacy shell')
                if public_id not in shell_ids:
                    raise ValueError(f"panel {panel.get('id')}: missing legacy shell {public_id}")
                elements.append(shell_ids[public_id])
            mapped['elements'] = elements
        panels.append(mapped)
    return from_json({**data, 'panels': panels}, mesh)
