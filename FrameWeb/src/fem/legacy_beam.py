"""Resolve one legacy case and split members before assembling their loads."""
import copy
import numpy as np


def _spring_rows_by_member(rows):
    grouped = {}
    for spring in rows:
        grouped.setdefault(str(spring['m']), []).append(spring)
    for member_rows in grouped.values():
        member_rows.sort(key=lambda row: int(row.get('row') or 0))
    return grouped


def _blank_spring_length(value):
    return value is None or isinstance(value, str) and not value.strip()


def _spring_intervals(rows, member_id, length, tolerance):
    """Return physical I-end intervals, retaining zero-stiffness cursor rows."""
    if not rows:
        return []

    def stiffness(row):
        values = []
        for key in ('tx', 'ty', 'tz', 'tr'):
            value = row.get(key)
            number = 0. if value is None else float(value)
            if not np.isfinite(number):
                raise ValueError(f'Invalid {key} foundation on member {member_id}')
            values.append(abs(number))
        return np.array(values)

    if all(_blank_spring_length(row.get('length')) for row in rows):
        return [(0., length, stiffness(row)) for row in rows]

    cursor = 0.
    intervals = []
    for index, row in enumerate(rows):
        value = row.get('length')
        if _blank_spring_length(value):
            if index != len(rows)-1:
                raise ValueError(f'Nonterminal blank spring length on member {member_id}')
            end = length
        else:
            try:
                span = float(value) if not isinstance(value, bool) else float('nan')
            except (TypeError, ValueError, OverflowError) as error:
                raise ValueError(f'Invalid spring length on member {member_id}') from error
            if not np.isfinite(span) or span <= 0:
                raise ValueError(f'Invalid spring length on member {member_id}')
            end = cursor + span
            if end > length + tolerance:
                raise ValueError(f'Spring lengths exceed member {member_id}')
            end = min(end, length)
        if end-cursor <= tolerance:
            raise ValueError(f'Zero-length spring interval on member {member_id}')
        intervals.append((cursor, end, stiffness(row)))
        cursor = end
    return intervals


def select_case(data, case_id=None):
    data = copy.deepcopy(data)
    # Keep the original sheets so all cases use one generated element mesh.
    data.setdefault('_all_spring_sheets', data.get('fix_member', {}))
    if not data.get('load'):
        return data
    requested = next(iter(data['load'])) if case_id is None else case_id
    matches = [key for key in data['load'] if str(key) == str(requested)]
    if len(matches) != 1:
        raise ValueError(f'Missing or ambiguous load case {requested}')
    source_case_id = matches[0]
    case_id = str(source_case_id)
    data.setdefault('_all_member_loads', [load for case in data['load'].values()
                                         for load in case.get('load_member', [])])
    case = data['load'][source_case_id]
    data['load'] = {case_id: case}
    for field in ('element', 'fix_node', 'joint', 'fix_member'):
        if data.get(field):
            key = str(case.get(field, 1))
            if key not in data[field]:
                raise ValueError(f'Missing {field} case {key}')
            data[field] = {key: data[field][key]}
    return data


def legacy_shear_correction(data, material_id, member):
    """An omitted element.G disables shear, even with an explicit true flag.

    G-present inputs retain their existing explicit/default choice. Selection
    runs before this function; rigid segments use their assigned material.
    """
    definitions = {int(key): value for case in data.get('element', {}).values()
                   for key, value in case.items()}
    if 'G' not in definitions.get(int(material_id), {}):
        return False
    return member.get('shear_correction', any('nonlinear' in mat for mat in definitions.values()))


def prepare_members(data, model_data):
    mesh, boundary = model_data['mesh'], model_data['boundary']
    members = data.get('member', {})
    if not members:
        return
    case = next(iter(data.get('load', {}).values()), {})
    next_node = max(mesh.nodes)+1
    next_element = max(mesh.elements)+1
    notice = {str(v['m']): sorted(set(v.get('Points', []))) for v in data.get('notice_points', [])}
    joints = next(iter(data.get('joint', {}).values()), [])
    springs = next(iter(data.get('fix_member', {}).values()), [])
    selected_springs = _spring_rows_by_member(springs)
    all_spring_sheets = data.get('_all_spring_sheets', data.get('fix_member', {}))
    all_springs = [_spring_rows_by_member(rows) for rows in all_spring_sheets.values()]
    for mid, member in members.items():
        original = mesh.elements[int(mid)]
        ni, nj = original['nodes']
        start, end = mesh.nodes[ni], mesh.nodes[nj]
        length = float(np.linalg.norm(end-start))
        if length <= 0:
            raise ValueError(f'Zero length member {mid}')
        axis = (end-start)/length
        tolerance = 1e-10*max(1., length)
        member_spring_sheets = [sheet.get(mid, []) for sheet in all_springs]
        # Desktop lengths are serialized from float?, so a mathematically
        # coincident endpoint can differ by a few float32 ulps. Apply this
        # wider tolerance only to members with explicit spring boundaries.
        has_spring_boundaries = any(
            not _blank_spring_length(row.get('length'))
            for rows in member_spring_sheets for row in rows)
        position_tolerance = (1e-6*max(1., length)
                              if has_spring_boundaries else tolerance)
        positions = {0., length}
        spring_intervals = _spring_intervals(
            selected_springs.get(mid, []), mid, length, position_tolerance)
        for rows in member_spring_sheets:
            for left, right, _ in _spring_intervals(
                    rows, mid, length, position_tolerance):
                positions.update((left, right))
        notices = [float(v) for v in notice.get(mid, []) if 0 < v < length]
        positions.update(notices)
        rigid = [v for v in data.get('rigid', []) if str(v['m']) == mid]
        for zone in rigid:
            for p in (float(zone.get('Ilength', 0)), length-float(zone.get('Jlength', 0))):
                if 0 < p < length:
                    positions.add(p)
                    notices.append(p)
        for load in data.get('_all_member_loads', []):
            if str(load.get('m')) != mid:
                continue
            if load.get('mark') == 2:
                a, l2 = float(load.get('L1') or 0), float(load.get('L2') or 0)
                b = a-l2 if l2 < 0 else length-l2
                if 0 <= a < b <= length:
                    positions.update([a,b])
            elif load.get('mark') in (1,11):
                positions.update(float(load.get('L'+i) or 0) for i in ('1','2')
                                 if 0 < float(load.get('L'+i) or 0) < length)
        loads = [v for v in case.get('load_member', []) if str(v['m']) == mid]
        distributed, points, thermal = [], [], 0.
        for load in loads:
            mark = int(load.get('mark', 0))
            if mark == 2:
                a = float(load.get('L1', 0))
                l2 = float(load.get('L2', 0))
                b = a-l2 if l2 < 0 else length-l2
                if abs(a-b) <= tolerance:
                    continue  # Empty interval (e.g. L1 equals the member length).
                if not -tolerance <= a < b <= length+tolerance:
                    raise ValueError(f'Invalid distributed load extent on member {mid}')
                a, b = max(0., a), min(length, b)
                positions.update([a, b])
                distributed.append((a, b, load))
            elif mark in (1, 11):
                for suffix in ('1', '2'):
                    p = float(load.get('L'+suffix, 0))
                    value = float(load.get('P'+suffix, 0))
                    if not 0 <= p <= length:
                        raise ValueError(f'Invalid point load position on member {mid}')
                    if value:
                        positions.add(p)
                        points.append((p, value, load.get('direction', 'y'), mark))
            elif mark == 9:
                thermal += float(load.get('P1', 0))
            elif mark == 0:
                continue  # Disabled editor row, even when stale values remain.
            else:
                raise ValueError(f'Unsupported member load mark {mark}')
        section = model_data['material'].get_bar_parameter(original['section_id'])
        if section is not None and all(getattr(section, name) == 0 for name in ('area', 'Iy', 'Iz', 'J')):
            # A load-transfer member has no internal displacement unknowns.
            # Integrate its actual loading directly onto the ORIGINAL endpoints.
            if rigid or any(str(s['m']) == mid for s in springs):
                raise ValueError('Load-transfer members cannot have rigid zones or foundations')
            from .elements.loaded_bar_element import LoadedBarElement
            transfer = LoadedBarElement(int(mid), [ni, nj], original['material_id'],
                                        original['section_id'], float(member.get('cg') or 0))
            transfer.set_node_coordinates(mesh.nodes)
            endpoint = np.zeros((2, 6))
            for a, b, load in distributed:
                transfer.line_load[:] = 0.
                transfer.set_line_load(load.get('direction', 'y'),
                                       [float(load.get('P1', 0)), float(load.get('P2', 0))])
                q0, q1 = transfer.line_load.T
                total = (b-a)*(q0+q1)/2
                first = a*total+(b-a)**2*(q0+2*q1)/6
                endpoint[0, :4] += total-first/length
                endpoint[1, :4] += first/length
            for p, value, direction, mark in points:
                transfer.line_load[:] = 0.
                transfer.set_line_load(direction, [value, value])
                vector = transfer.line_load[:3, 0]
                offset = 0 if mark == 1 else 3
                endpoint[0, offset:offset+3] += (1-p/length)*vector
                endpoint[1, offset:offset+3] += p/length*vector
            original.update(transfer_load=endpoint.ravel().tolist(), original_id=int(mid),
                            member_start=0., member_end=length, shear_correction=False)
            continue
        # The same physical point may arise through different floating-point
        # expressions (L-Jlength vs notice point). Never create a zero segment.
        unique = []
        for p in sorted(positions):
            if not unique or p-unique[-1] > position_tolerance:
                unique.append(p)
        unique[-1] = length
        positions = set(unique)
        snap = lambda x: min(unique, key=lambda p: abs(p-x))
        distributed = [(snap(a), snap(b), load) for a,b,load in distributed]
        points = [(snap(p), value, direction, mark) for p,value,direction,mark in points]
        coordinates = {}
        active_points = {p for p, *_ in points} | {p for a,b,_ in distributed for p in (a,b)}
        n_count = l_count = 0
        for p in sorted(positions):
            if p == 0:
                node = ni
            elif p == length:
                node = nj
            else:
                xyz = start+p*axis
                node = next((node for node,coord in mesh.nodes.items()
                             if np.linalg.norm(coord-xyz) <= tolerance), None)
                if node is None:
                    node = next_node
                    next_node += 1
                    mesh.add_node(node, xyz)
                if any(abs(p-n) <= position_tolerance for n in notices):
                    n_count += 1
                    label = f'{mid}n{n_count}'
                elif p in active_points:
                    l_count += 1
                    label = f'{mid}l{l_count}'
                else:
                    label = None  # Another load case's subdivision; not a reported point here.
                if str(node) not in data['node']:
                    model_data.setdefault('node_labels', {})[node] = label
            coordinates[p] = node
        spring_intervals = [(snap(left), snap(right), values)
                            for left, right, values in spring_intervals]
        releases = set()
        for joint in joints:
            if str(joint['m']) == mid:
                releases.update(i for i, k in zip((3, 4, 5, 9, 10, 11), ('xi', 'yi', 'zi', 'xj', 'yj', 'zj'))
                                if joint.get(k, 1) == 0)
        positions = sorted(positions)
        for index, (a, b) in enumerate(zip(positions[:-1], positions[1:])):
            eid = int(mid) if index == 0 else next_element
            if index:
                next_element += 1
            props = copy.deepcopy(original)
            foundation = np.zeros(4)
            for left, right, values in spring_intervals:
                if a >= left and b <= right:
                    foundation += values
            mat_id = props['material_id']
            for zone in rigid:
                # Classify against the same coalesced boundaries used to split
                # the member. An equivalent notice coordinate may be a few
                # ulps below L-Jlength; that must not erase the end rigid zone.
                if b <= snap(float(zone.get('Ilength', 0))) or a >= snap(length-float(zone.get('Jlength', 0))):
                    mat_id = int(zone['e'])
            props.update(nodes=[coordinates[a], coordinates[b]], material_id=mat_id, section_id=mat_id,
                         shear_correction=legacy_shear_correction(data, mat_id, member),
                         original_id=int(mid), member_start=a, member_end=b,
                         member_nodes=[ni, nj],
                         releases=[i for i in releases if (i < 6 and a == 0) or (i >= 6 and b == length)],
                         foundation=foundation.tolist(), line_loads=[], temperature=thermal)
            for left, right, load in distributed:
                if a >= left and b <= right:
                    q0, q1 = float(load.get('P1', 0)), float(load.get('P2', 0))
                    values = [q0+(q1-q0)*(x-left)/(right-left) for x in (a, b)]
                    props['line_loads'].append(dict(direction=load.get('direction', 'y'), values=values))
            mesh.elements[eid] = props
        # Local force transformation depends only on original geometry and cg.
        from .elements.bar_element import BEBarElement
        orient = BEBarElement(0, [ni, nj], 1, 1, float(member.get('cg') or 0))
        orient.set_node_coordinates(mesh.nodes)
        for p, value, direction, mark in points:
            direction = {'x': 'Lx', 'y': 'Ly', 'z': 'Lz', 'gx': 'GX', 'gy': 'GY', 'gz': 'GZ'}.get(direction, direction)
            if direction not in ('Lx', 'Ly', 'Lz', 'GX', 'GY', 'GZ'):
                raise ValueError(f'Unknown point load direction: {direction}')
            vector = np.zeros(3)
            vector[('x', 'y', 'z').index(direction[-1].lower())] = value
            if direction.startswith('L'):
                vector = orient.transformation_matrix.T@vector
            force = np.zeros(6)
            if mark == 1:
                force[:3] = vector
            else:
                force[3:] = vector
            boundary.add_load(coordinates[p], force)
    # Splitting and case selection have already been done above.
    model_data.pop('notice_points', None)
    model_data.pop('load', None)
    if data.get('dimension') == 2:
        boundary.auxiliary_restraint_nodes = set()
        for node in mesh.nodes:
            if node not in boundary.restraints:
                boundary.auxiliary_restraint_nodes.add(node)
                boundary.add_restraint(node, [False]*6)
            for i in (2,3,4):
                boundary.restraints[node].dof_restraints[i] = True
