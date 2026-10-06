"""
FEM解析のファイル入出力モジュール
JavaScript版のFileIO機能に対応
"""
import json
import logging
import os
from typing import Dict, Any, Optional, List
import numpy as np
from dataclasses import asdict
from .mesh import MeshModel
from .boundary_condition import BoundaryCondition
from .material import Material, MaterialProperty, ShellParameter, BarParameter, NonlinearMaterialProperty
from .section import Section, CircleSection, RectSection, ISection, TubeSection
from .spatial_loads.serialization import (
    from_json as spatial_loads_from_json,
    from_legacy as spatial_loads_from_legacy,
    to_json as spatial_loads_to_json,
)
from .nonlinear.component_laws import law_to_dict, laws_from_dict
from .nonlinear.hysteresis.slip import parse_slip_spring
from .nonlinear.support_springs import add_nonlinear_support, validate_support_definitions


logger = logging.getLogger(__name__)


def read_model(file_path: str) -> Dict[str, Any]:
    """モデルファイルを読み込む
    
    Args:
        file_path: ファイルパス（.json, .fw3, .fem形式に対応）
        
    Returns:
        モデルデータの辞書
    """
    ext = os.path.splitext(file_path)[1].lower()
    
    if ext == '.json':
        """JSONフォーマットのモデルを読み込む"""
        with open(file_path, 'r', encoding='utf-8') as f:
            data = json.load(f)        
        return _read_json_model(data)
    elif ext == '.fw3':
        with open(file_path, 'r', encoding='utf-8') as f:
            lines = f.readlines()        
        return _read_fw3_model(lines)
    elif ext == '.fem':
        # ✅ V0互換: .fem形式（V0テストデータ形式）をサポート
        with open(file_path, 'r', encoding='utf-8') as f:
            lines = f.readlines()        
        return _read_fem_model(lines)
    else:
        raise ValueError(f"Unsupported file format: {ext}")


def _read_json_model(data: Dict[str, Any]) -> Dict[str, Any]:
    if not isinstance(data, dict) or not (data.get('node') or data.get('nodes')):
        raise ValueError('Model must contain nodes')
    if data.get('dimension') == 2:
        cases = data.get('load', {})
        spatial_inputs = [data.get('spatial_loads')]
        if isinstance(cases, dict):
            for case in cases.values():
                if isinstance(case, dict):
                    spatial_inputs.extend((case.get('spatial_loads'), case.get('load_inf')))
        if any(value for value in spatial_inputs):
            raise ValueError('Bridge spatial loads require a 3D model (dimension 3)')
    model_data = {
        'mesh': MeshModel(),
        'boundary': BoundaryCondition(),
        'material': Material(),
        'section': Section(),
        'analysis_type': data.get('analysis_type'),
        'analysis_params': data.get('analysis_params', {}),
        'model_metadata': data.get('model_metadata', {}),
    }
    
    # 旧形式のチェック（nodeセクションがある場合）
    if 'node' in data:
        # 旧形式の読み込み処理
        from .legacy_beam import select_case, prepare_members
        data = select_case(data)
        model_data = _read_legacy_json_model(data, model_data)
        prepare_members(data, model_data)
        _read_explicit_boundary(data.get('boundary_conditions', {}), model_data['boundary'])
        validate_support_definitions(model_data['boundary'], model_data['mesh'])
        return model_data
    
    # 新形式のノードデータの読み込み
    if 'nodes' in data:
        for node_id, coords in data['nodes'].items():
            model_data['mesh'].add_node(int(node_id), coords)
            
    # 新形式の要素データの読み込み
    if 'elements' in data:
        for elem_id, elem_data in data['elements'].items():
            model_data['mesh'].add_element(
                int(elem_id),
                elem_data['type'],
                elem_data['nodes'],
                elem_data.get('material_id', 1),
                **{k: v for k, v in elem_data.items() 
                   if k not in ['type', 'nodes', 'material_id']}
            )
            
    # 新形式の材料データの読み込み
    if 'materials' in data:
        for mat_id, mat_data in data['materials'].items():
            model_data['material'].add_material(
                int(mat_id),
                MaterialProperty(**mat_data)
            )
            
    for mat_id, mat_data in data.get('nonlinear_materials', {}).items():
        if isinstance(mat_data, dict) and set(mat_data) == {'laws'}:
            model_data['material'].add_nonlinear_laws(
                int(mat_id), laws_from_dict(mat_data['laws']))
        else:
            model_data['material'].add_nonlinear_material(
                int(mat_id), NonlinearMaterialProperty(**mat_data))
    for section_id, params in data.get('bar_parameters', {}).items():
        model_data['material'].add_bar_parameter(int(section_id), BarParameter(**params))

    # 新形式の境界条件の読み込み
    if 'boundary_conditions' in data:
        bc_data = data['boundary_conditions']
        _read_explicit_boundary(bc_data, model_data['boundary'])
        
        # 拘束条件
        if 'restraints' in bc_data:
            for node_id, restraint in bc_data['restraints'].items():
                model_data['boundary'].add_restraint(
                    int(node_id),
                    restraint['dof'],
                    restraint.get('values')
                )
                
        # 荷重条件
        if 'loads' in bc_data:
            for node_id, forces in bc_data['loads'].items():
                model_data['boundary'].add_load(int(node_id), forces)
                
        # 面圧条件（新規追加）
        if 'pressures' in bc_data:
            for pressure_data in bc_data['pressures']:
                model_data['boundary'].add_pressure(
                    pressure_data['element_id'],
                    pressure_data['face'],
                    pressure_data['pressure']
                )
                
    if 'spatial_loads' in data:
        model_data['boundary'].spatial_loads = spatial_loads_from_json(
            data['spatial_loads'], model_data['mesh'])
    validate_support_definitions(model_data['boundary'], model_data['mesh'])
    return model_data


def _read_explicit_boundary(data, boundary):
    """Explicit restraints and springs, also available with legacy geometry.

    A restraint entry replaces the node's legacy restraint. Springs are separate
    from prescribed displacement and have no magnitude threshold.
    """
    for node, restraint in data.get('restraints', {}).items():
        boundary.add_restraint(int(node), restraint['dof'], restraint.get('values'))
    supports = getattr(boundary, 'spring_supports', {})
    for node, values in data.get('spring_supports', {}).items():
        supports.setdefault(int(node), {}).update(values)
    boundary.spring_supports = supports
    nonlinear = data.get('nonlinear_spring_supports', {})
    if not isinstance(nonlinear, dict):
        raise ValueError('boundary_conditions.nonlinear_spring_supports must be an object')
    for node, values in nonlinear.items():
        if not isinstance(values, dict):
            raise ValueError(f'nonlinear_spring_supports node {node}: expected direction object')
        for direction, definition in values.items():
            add_nonlinear_support(boundary, int(node), direction, definition,
                                   f'boundary_conditions.nonlinear_spring_supports node {node} direction {direction}')
    if 'auxiliary_restraint_nodes' in data:
        boundary.auxiliary_restraint_nodes = {int(node) for node in data['auxiliary_restraint_nodes']}
        for node in boundary.auxiliary_restraint_nodes:
            restraint = boundary.restraints.get(node)
            if (restraint is None or list(restraint.dof_restraints) != [False,False,True,True,True,False]
                    or any(restraint.values) or node in supports
                    or node in boundary.nonlinear_spring_supports):
                raise ValueError('Auxiliary restraint metadata must describe only automatic 2D constraints')


def _read_legacy_json_model(data: Dict[str, Any], model_data: Dict[str, Any]) -> Dict[str, Any]:
    """旧形式のJSONファイルを読み込む"""
    from .legacy_beam import legacy_shear_correction
    
    # Member, shell and solid identifiers occupy separate legacy namespaces.
    next_element_id = max([int(k) for field in ('member','shell','solid')
                           for k in data.get(field, {})] or [0])+1
    legacy_shell_ids = {}
    # nodeセクションの読み込み
    if 'node' in data:
        for node_id, coords in data['node'].items():
            coord_list = [coords['x'], coords['y'], coords['z']]
            model_data['mesh'].add_node(int(node_id), coord_list)
    
    # 非線形材料を使用するmemberを追跡するための一時リスト
    nonlinear_member_info = []

    # memberセクションの読み込み（bar要素として扱う）
    if 'member' in data:
        for member_id, member_data in data['member'].items():
            ni = member_data['ni']  # i端節点
            nj = member_data['nj']  # j端節点
            # 節点IDを整数に変換（文字列の場合）
            if isinstance(ni, str):
                ni = int(ni)
            if isinstance(nj, str):
                nj = int(nj)

            material_id = member_data.get('e', 1)  # 材料/断面ID
            # 材料IDも整数に変換
            if isinstance(material_id, str):
                material_id = int(material_id)

            # 非線形材料を使用する場合は情報を記録（後で処理）
            nonlinear_member_info.append({
                'member_id': int(member_id),
                'material_id': material_id
            })

            model_data['mesh'].add_element(
                int(member_id),
                'bar',
                [ni, nj],
                material_id,
                section_id=material_id, angle=float(member_data.get('cg') or 0),
                shear_correction=legacy_shear_correction(data, material_id, member_data),
                member_id=int(member_id)  # 部材IDを保存
            )
    
    # shellセクションの読み込み（shell要素として扱う）
    if 'shell' in data:
        for shell_id, shell_data in data['shell'].items():
            nodes = shell_data['nodes']  # 4節点のリスト
            # 節点IDリストを文字列から整数に変換
            nodes = [int(node_id) for node_id in nodes]
            
            material_id = shell_data.get('e', 1)  # 材料ID
            # 材料IDも文字列の場合があるので整数に変換
            if isinstance(material_id, str):
                material_id = int(material_id)
            
            # 材料情報からthicknessを取得
            thickness = None
            if 'element' in data:
                for _, elem_defs in data['element'].items():
                    for mat_id_str, elem_def in elem_defs.items():
                        if int(mat_id_str) == material_id:
                            # Legacy section_material.Thickness uses A. An
                            # explicit migrated thickness field takes precedence.
                            thickness = elem_def.get('thickness', elem_def.get('A'))
                            break
            if thickness is None or not np.isfinite(thickness) or thickness <= 0:
                raise ValueError(f'Shell {shell_id} requires finite positive thickness (thickness or A)')
            
            element_id = int(shell_id)
            if element_id in legacy_shell_ids:
                raise ValueError(f'Duplicate normalized legacy shell ID {element_id}')
            if element_id in model_data['mesh'].elements:
                element_id, next_element_id = next_element_id, next_element_id+1
            legacy_shell_ids[int(shell_id)] = element_id
            model_data['mesh'].add_element(
                element_id,
                'shell',
                nodes,
                material_id,
                thickness=thickness,
                formulation=shell_data.get('formulation', 'dkt' if len(nodes) == 3 else 'mindlin'),
                shell_id=int(shell_id)  # shell IDを保存
            )
    
    # solidセクションの読み込み（solid要素として扱う）
    if 'solid' in data:
        for solid_id, solid_data in data['solid'].items():
            nodes = solid_data['nodes']  # 4節点（tetra）, 6節点（wedge）, 8節点（hexa）のリスト
            # 節点IDリストを文字列から整数に変換
            nodes = [int(node_id) for node_id in nodes]
            
            material_id = solid_data.get('e', 1)  # 材料ID
            # 材料IDも文字列の場合があるので整数に変換
            if isinstance(material_id, str):
                material_id = int(material_id)
            
            # typeフィールドが明示的に指定されている場合はそれを使用
            if 'type' in solid_data:
                element_type = solid_data['type']
            else:
                # 節点数により要素タイプを判定（後方互換性）
                element_type = 'tetra' if len(nodes) == 4 else 'hexa' if len(nodes) == 8 else 'wedge' if len(nodes) == 6 else 'solid'
            
            element_id = int(solid_id)
            if element_id in model_data['mesh'].elements:
                element_id, next_element_id = next_element_id, next_element_id+1
            model_data['mesh'].add_element(
                element_id,
                element_type,
                nodes,
                material_id,
                solid_id=int(solid_id)  # solid IDを保存
            )
    
    # 旧形式の材料情報をelementセクションから読み込む
    # 非線形材料データも同時に処理
    nonlinear_materials = {}  # {material_id: {'params': NonlinearMaterialProperty, 'hysteresis_dofs': [...]}}

    beam_material_ids = {int(member.get('e', 1)) for member in data.get('member', {}).values()}
    beam_material_ids.update(int(zone['e']) for zone in data.get('rigid', []))
    if 'element' in data:
        for _, elem_defs in data['element'].items():
            for mat_id_str, elem_def in elem_defs.items():
                material_id = int(mat_id_str)
                mp = MaterialProperty(
                    name=elem_def.get('n', f"Material{material_id}"),
                    E=elem_def['E'],
                    nu=elem_def.get('nu', 0.2 if 'nonlinear' in elem_def else 0.3),
                    density=elem_def.get('den'), alpha=elem_def.get('Xp'),
                    shear_modulus=elem_def.get('G')
                )
                model_data['material'].add_material(material_id, mp)

                # Only beams (including rigid segments) use section properties.
                # Shell A is a legacy thickness alias; solids use their geometry.
                if material_id in beam_material_ids:
                    bp = BarParameter(
                        area=elem_def.get('A', 1.0),
                        Iy=elem_def.get('Iy', 1.0),
                        Iz=elem_def.get('Iz', 1.0),
                        J=elem_def.get('J', 1.0)
                    )
                    model_data['material'].add_bar_parameter(material_id, bp)

                # 非線形材料データの読み込み
                if 'nonlinear' in elem_def:
                    nl_data = elem_def['nonlinear']
                    if 'laws' in nl_data:
                        unknown = set(nl_data)-{'laws', 'hysteresis_dofs'}
                        if unknown:
                            raise ValueError(f'Component laws cannot be mixed with flat fields: {sorted(unknown)}')
                        laws = laws_from_dict(nl_data['laws'])
                        hysteresis_dofs = nl_data.get('hysteresis_dofs', list(laws))
                        if set(hysteresis_dofs) != set(laws):
                            raise ValueError('hysteresis_dofs must exactly match component law keys')
                        model_data['material'].add_nonlinear_laws(material_id, laws)
                        nonlinear_materials[material_id] = {
                            'hysteresis_dofs': hysteresis_dofs,
                        }
                        continue
                    nl_type = nl_data.get('type', 'jr_stiffness_reduction')
                    if nl_type != 'jr_stiffness_reduction':
                        raise ValueError(f'Unknown nonlinear material type: {nl_type}')

                    if nl_type == 'jr_stiffness_reduction':
                        # 対称スケルトンカーブかどうか
                        symmetric = nl_data.get('symmetric', True)

                        # 正側パラメータ
                        delta_1 = nl_data.get('delta_1', 0.001)
                        delta_2 = nl_data.get('delta_2', 0.01)
                        delta_3 = nl_data.get('delta_3', 0.1)
                        P_1 = nl_data.get('P_1', 100.0)
                        P_2 = nl_data.get('P_2', 500.0)
                        P_3 = nl_data.get('P_3', 550.0)
                        delta_4 = nl_data.get('delta_4')
                        P_4 = nl_data.get('P_4')

                        # 負側パラメータ（非対称の場合）
                        if symmetric:
                            delta_1_neg = delta_1
                            delta_2_neg = delta_2
                            delta_3_neg = delta_3
                            P_1_neg = P_1
                            P_2_neg = P_2
                            P_3_neg = P_3
                            delta_4_neg = delta_4
                            P_4_neg = P_4
                        else:
                            delta_1_neg = nl_data.get('delta_1_neg', delta_1)
                            delta_2_neg = nl_data.get('delta_2_neg', delta_2)
                            delta_3_neg = nl_data.get('delta_3_neg', delta_3)
                            P_1_neg = nl_data.get('P_1_neg', P_1)
                            P_2_neg = nl_data.get('P_2_neg', P_2)
                            P_3_neg = nl_data.get('P_3_neg', P_3)
                            delta_4_neg = nl_data.get('delta_4_neg', delta_4)
                            P_4_neg = nl_data.get('P_4_neg', P_4)

                        beta = nl_data.get('beta', 0.4)
                        K_min = nl_data.get('K_min', None)

                        # NonlinearMaterialPropertyを作成
                        nl_mat = NonlinearMaterialProperty(
                            name=elem_def.get('n', f"Nonlinear{material_id}"),
                            E=elem_def['E'],
                            nu=elem_def.get('nu', 0.2),
                            delta_1_pos=delta_1,
                            delta_2_pos=delta_2,
                            delta_3_pos=delta_3,
                            P_1_pos=P_1,
                            P_2_pos=P_2,
                            P_3_pos=P_3,
                            delta_4_pos=delta_4,
                            P_4_pos=P_4,
                            delta_1_neg=delta_1_neg,
                            delta_2_neg=delta_2_neg,
                            delta_3_neg=delta_3_neg,
                            P_1_neg=P_1_neg,
                            P_2_neg=P_2_neg,
                            P_3_neg=P_3_neg,
                            delta_4_neg=delta_4_neg,
                            P_4_neg=P_4_neg,
                            beta=beta,
                            K_min=K_min,
                            density=elem_def.get('den')
                        )

                        model_data['material'].add_nonlinear_material(material_id, nl_mat)

                        # 履歴を適用する自由度
                        hysteresis_dofs = nl_data.get('hysteresis_dofs', ['moment_z'])
                        nonlinear_materials[material_id] = {
                            'hysteresis_dofs': hysteresis_dofs
                        }

                        logger.info(
                            "非線形材料: material_id=%s, P=(%s, %s, %s), "
                            "delta=(%s, %s, %s), hysteresis_dofs=%s",
                            material_id, P_1, P_2, P_3, delta_1, delta_2, delta_3,
                            hysteresis_dofs,
                        )

    # 非線形材料を使用するmemberの要素タイプを'nonlinear_bar'に変更
    for member_info in nonlinear_member_info:
        member_id = member_info['member_id']
        material_id = member_info['material_id']

        if material_id in nonlinear_materials:
            # 要素タイプを'nonlinear_bar'に変更
            if member_id in model_data['mesh'].elements:
                elem_data = model_data['mesh'].elements[member_id]
                elem_data['type'] = 'nonlinear_bar'
                elem_data['section_id'] = material_id
                elem_data['hysteresis_dofs'] = nonlinear_materials[material_id]['hysteresis_dofs']
                # Preserve an explicit per-member shear choice.
                logger.info(
                    "要素%sを非線形要素に変換: material_id=%s, hysteresis_dofs=%s",
                    member_id, material_id, elem_data['hysteresis_dofs'],
                )
    
    # デフォルト材料を追加（材料が一つも読み込まれなかった場合）
    if len(model_data['material'].materials) == 0:
        # デフォルト材料を設定
        default_material = MaterialProperty(
            name="Default Steel",
            E=2.05e11,  # Pa
            nu=0.3,
            density=7850.0  # kg/m³
        )
        model_data['material'].add_material(1, default_material)
    
    # 拘束条件の読み込み（fix_nodeセクション）
    if 'fix_node' in data:
        seen_supports = {}
        for case_id, restraints in data['fix_node'].items():
            if not isinstance(restraints, list):
                continue
                
            for restraint in restraints:
                if 'n' not in restraint:
                    continue
                    
                node_id = int(restraint['n'])
                
                # Numeric legacy values: 0 free, 1 fixed, all others linear springs.
                values = []
                for key, direction in zip(('tx','ty','tz','rx','ry','rz'), ('x','y','z','rx','ry','rz')):
                    value = restraint.get(key, 0)
                    previous = seen_supports.get((node_id, direction), 0)
                    if ((isinstance(value, dict) and previous != 0)
                            or (isinstance(previous, dict) and value != 0)):
                        raise ValueError(f'fix_node[{case_id}] node {node_id} direction {direction}: conflicting support rows')
                    if value != 0:
                        seen_supports[node_id, direction] = value
                    if isinstance(value, dict):
                        add_nonlinear_support(model_data['boundary'], node_id, direction, value,
                                               f'fix_node[{case_id}] node {node_id} direction {direction} ({key})')
                        values.append(0.)
                    else:
                        values.append(float(value))
                if not np.all(np.isfinite(values)):
                    raise ValueError('Support values must be finite')
                model_data['boundary'].add_restraint(node_id, [v == 1 for v in values])
                springs = getattr(model_data['boundary'], 'spring_supports', {})
                for name, value in zip(('x','y','z','rx','ry','rz'), values):
                    if value not in (0, 1):
                        springs.setdefault(node_id, {})[name] = abs(value)
                model_data['boundary'].spring_supports = springs

    if 'notice_points' in data:
        model_data['notice_points'] = data['notice_points']
    
    # 荷重条件の読み込み（loadセクション）
    if 'load' in data:
        # loadセクション全体をmodel_dataに追加（FemModelで要素分割に使用）
        model_data['load'] = data['load']

        # 最初の荷重ケースから荷重データを境界条件に追加
        load_cases = data['load']
        if load_cases:
            # 最初の荷重ケースから解析パラメータを抽出
            first_case_key = list(load_cases.keys())[0]
            first_case = load_cases[first_case_key]
            model_data['analysis_params'] = {
                'n_load_steps': first_case.get('n_load_steps', 10),
                'max_iterations': first_case.get('max_iterations', 50),
                'tolerance': first_case.get('tolerance', 1e-6),
                'n_modes': first_case.get('n_modes', 10),
                'load_factors': first_case.get('load_factors'),
                'displacement_control': first_case.get('displacement_control'),
            }
            if model_data['analysis_type'] is None:
                model_data['analysis_type'] = first_case.get('analysis_type')
            model_data['analysis_params'].update(data.get('analysis_params', {}))
            # 最初の荷重ケースを使用（通常は基本荷重ケース）
            first_case_key = list(load_cases.keys())[0]
            case_data = load_cases[first_case_key]
            
            # 節点荷重の処理（load_node）
            if 'load_node' in case_data and len(case_data['load_node']) > 0:
                for node_load in case_data['load_node']:
                    node_id = int(node_load['n'])
                    forces = [
                        node_load.get('tx', 0.0),  # fx
                        node_load.get('ty', 0.0),  # fy
                        node_load.get('tz', 0.0),  # fz
                        node_load.get('rx', 0.0),  # mx
                        node_load.get('ry', 0.0),  # my
                        node_load.get('rz', 0.0)   # mz
                    ]
                    model_data['boundary'].add_load(node_id, forces)
            
            # 要素荷重の処理（load_member）
            if 'load_member' in case_data and len(case_data['load_member']) > 0:
                # 要素荷重を等価節点荷重に変換して境界条件に追加
                pass  # prepare_members applies loads after member subdivision
    
    model_data['boundary'].spatial_loads = spatial_loads_from_legacy(
        data, model_data['mesh'], legacy_shell_ids)
    return model_data


def _read_fw3_model(lines: list[str]) -> Dict[str, Any]:
    """FW3フォーマットのモデルを読み込む（独自フォーマット）
    
    V0互換性を追加:
    - TriElement1（三角形Shell要素）
    - QuadElement1（四角形Shell要素）
    - その他のV0要素タイプ
    """
    model_data = {
        'mesh': MeshModel(),
        'boundary': BoundaryCondition(),
        'material': Material(),
        'section': Section()
    }
    
       
    mode = None
    for line in lines:
        line = line.strip()
        if not line or line.startswith('#'):
            continue
            
        # セクションの開始を検出
        if line.startswith('*'):
            mode = line[1:].lower()
            continue
            
        # 各セクションのデータを読み込み
        if mode == 'nodes':
            parts = line.split()
            if len(parts) >= 4:
                node_id = int(parts[0])
                coords = [float(parts[i]) for i in range(1, 4)]
                model_data['mesh'].add_node(node_id, coords)
                
        elif mode == 'elements':
            parts = line.split()
            if len(parts) >= 3:
                elem_id = int(parts[0])
                elem_type = parts[1]
                node_ids = [int(parts[i]) for i in range(2, len(parts))]
                
                # ✅ V0技術資産: 要素タイプ別のデフォルト材料設定
                if elem_type in ['TriElement1', 'QuadElement1', 'ShellElement']:
                    # Shell要素の場合、デフォルト厚さを設定
                    model_data['mesh'].add_element(elem_id, elem_type, node_ids, 1, thickness=0.01)
                    logger.info("V0互換: %s要素を読み込み (ID: %s, 節点: %s)", elem_type, elem_id, node_ids)
                elif elem_type in ['BarElement', 'BeamElement', 'TrussElement']:
                    # Bar要素の場合
                    model_data['mesh'].add_element(elem_id, elem_type, node_ids, 1)
                else:
                    # その他の要素
                    model_data['mesh'].add_element(elem_id, elem_type, node_ids, 1)
                
        elif mode == 'restraints':
            parts = line.split()
            if len(parts) >= 2:
                node_id = int(parts[0])
                dof_str = parts[1]
                dof_restraints = [False] * 6
                for i, char in enumerate(dof_str):
                    if i < 6 and char == '1':
                        dof_restraints[i] = True
                model_data['boundary'].add_restraint(node_id, dof_restraints)
                
        elif mode == 'loads':
            parts = line.split()
            if len(parts) >= 7:
                node_id = int(parts[0])
                forces = [float(parts[i]) for i in range(1, 7)]
                model_data['boundary'].add_load(node_id, forces)
                
    return model_data


def _read_fem_model(lines: list[str]) -> Dict[str, Any]:
    """Read the structural V0 format without silently discarding records."""
    from .v0_io import read_v0_model
    return read_v0_model(lines)


def write_model(model_data: Dict[str, Any], file_path: str) -> None:
    """モデルデータをファイルに書き込む
    
    Args:
        model_data: モデルデータ
        file_path: 出力ファイルパス
    """
    ext = os.path.splitext(file_path)[1].lower()
    
    if ext == '.json':
        _write_json_model(model_data, file_path)
    elif ext == '.fw3':
        _write_fw3_model(model_data, file_path)
    else:
        raise ValueError(f"Unsupported file format: {ext}")


def model_to_jsonable(model_data: Dict[str, Any]) -> Dict[str, Any]:
    """Return the canonical JSON model representation used for files and hashes."""
    output_data = {
        k: model_data[k]
        for k in ('analysis_type', 'analysis_params', 'model_metadata')
        if k in model_data
    }
    
    # メッシュデータ
    mesh = model_data.get('mesh')
    if mesh:
        output_data['nodes'] = {
            str(node_id): coords.tolist() 
            for node_id, coords in mesh.nodes.items()
        }
        output_data['elements'] = {
            str(elem_id): elem_data 
            for elem_id, elem_data in mesh.elements.items()
        }
        
    # 材料データ
    material = model_data.get('material')
    if material:
        output_data['materials'] = {
            str(mat_id): {
                'name': mat.name,
                'E': mat.E,
                'nu': mat.nu,
                'density': mat.density,
                'alpha': mat.alpha,
                'k': mat.k,
                'c': mat.c,
                'shear_modulus': mat.shear_modulus,
            }
            for mat_id, mat in material.materials.items()
        }
        output_data['nonlinear_materials'] = {
            **{str(k): asdict(v) for k, v in material.nonlinear_materials.items()},
            **{str(material_id): {
                'laws': {dof: law_to_dict(law) for dof, law in laws.items()}
            }
            for material_id, laws in material.nonlinear_laws.items()
            },
        }
        output_data['bar_parameters'] = {
            str(k): asdict(v) for k, v in material.bar_params.items()}
        
    # 境界条件
    boundary = model_data.get('boundary')
    if boundary:
        output_data['boundary_conditions'] = {}
        if boundary.spatial_loads.has_definitions:
            output_data['spatial_loads'] = spatial_loads_to_json(boundary.spatial_loads)
        output_data['boundary_conditions']['spring_supports'] = getattr(boundary, 'spring_supports', {})
        if boundary.nonlinear_spring_supports:
            output_data['boundary_conditions']['nonlinear_spring_supports'] = {
                str(node): {direction: parse_slip_spring(value).to_dict()
                            for direction, value in supports.items()}
                for node, supports in boundary.nonlinear_spring_supports.items()
            }
        output_data['boundary_conditions']['auxiliary_restraint_nodes'] = sorted(getattr(boundary, 'auxiliary_restraint_nodes', set()))
        
        if boundary.restraints:
            output_data['boundary_conditions']['restraints'] = {
                str(node_id): {
                    'dof': restraint.dof_restraints,
                    'values': restraint.values
                }
                for node_id, restraint in boundary.restraints.items()
            }
            
        if boundary.loads:
            output_data['boundary_conditions']['loads'] = {
                str(node_id): load.forces.tolist()
                for node_id, load in boundary.loads.items()
            }
            
        # 面圧条件の書き込み（新規追加）
        if boundary.pressures:
            output_data['boundary_conditions']['pressures'] = [
                {
                    'element_id': pressure.element_id,
                    'face': pressure.face,
                    'pressure': pressure.pressure
                }
                for pressure in boundary.pressures
            ]
            
    return result_to_jsonable(output_data)


def _write_json_model(model_data: Dict[str, Any], file_path: str) -> None:
    """JSONフォーマットでモデルを書き込む"""
    output_data = model_to_jsonable(model_data)
    with open(file_path, 'w', encoding='utf-8') as f:
        json.dump(output_data, f, indent=2, ensure_ascii=False, allow_nan=False)


def _write_fw3_model(model_data: Dict[str, Any], file_path: str) -> None:
    """FW3フォーマットでモデルを書き込む"""
    boundary = model_data.get('boundary')
    if boundary is not None and boundary.nonlinear_spring_supports:
        raise ValueError('Models with nonlinear support springs must be saved as JSON')
    if boundary is not None and boundary.spatial_loads.has_definitions:
        raise ValueError('Models with spatial loads must be saved as JSON, not FW3')
    with open(file_path, 'w', encoding='utf-8') as f:
        # ヘッダー
        f.write("# FEMPython Model File\n")
        f.write(f"# Generated by fem.file_io module\n\n")
        
        # ノードデータ
        mesh = model_data.get('mesh')
        if mesh and mesh.nodes:
            f.write("*NODES\n")
            for node_id, coords in sorted(mesh.nodes.items()):
                f.write(f"{node_id} {coords[0]:.6f} {coords[1]:.6f} {coords[2]:.6f}\n")
            f.write("\n")
            
        # 要素データ
        if mesh and mesh.elements:
            f.write("*ELEMENTS\n")
            for elem_id, elem_data in sorted(mesh.elements.items()):
                nodes_str = ' '.join(str(n) for n in elem_data['nodes'])
                f.write(f"{elem_id} {elem_data['type']} {nodes_str}\n")
            f.write("\n")
            
        # 境界条件
        boundary = model_data.get('boundary')
        if boundary:
            # 拘束条件
            if boundary.restraints:
                f.write("*RESTRAINTS\n")
                for node_id, restraint in sorted(boundary.restraints.items()):
                    dof_str = ''.join('1' if r else '0' for r in restraint.dof_restraints)
                    f.write(f"{node_id} {dof_str}\n")
                f.write("\n")
                
            # 荷重条件
            if boundary.loads:
                f.write("*LOADS\n")
                for node_id, load in sorted(boundary.loads.items()):
                    forces_str = ' '.join(f"{f:.6f}" for f in load.forces)
                    f.write(f"{node_id} {forces_str}\n")
                    
            # 面圧条件（新規追加）
            if boundary.pressures:
                f.write("*PRESSURES\n")
                for pressure in boundary.pressures:
                    f.write(f"{pressure.element_id} {pressure.face} {pressure.pressure:.6f}\n")


def read_result(file_path: str) -> Dict[str, Any]:
    """結果ファイルを読み込む
    
    Args:
        file_path: 結果ファイルパス
        
    Returns:
        結果データの辞書
    """
    ext = os.path.splitext(file_path)[1].lower()
    
    if ext == '.json':
        with open(file_path, 'r', encoding='utf-8') as f:
            return json.load(f)
    else:
        raise ValueError(f"Unsupported result file format: {ext}")


def result_to_jsonable(obj):
    """Shared finite JSON representation for file and HTTP results."""
    if isinstance(obj, np.ndarray):
        return result_to_jsonable(obj.tolist())
    if isinstance(obj, np.generic):
        return result_to_jsonable(obj.item())
    if isinstance(obj, dict):
        output = {}
        for key, value in obj.items():
            if str(key) == 'periods' and isinstance(value, (list, tuple, np.ndarray)):
                output[str(key)] = [
                    None if isinstance(item, (float, np.floating)) and not np.isfinite(item)
                    else result_to_jsonable(item)
                    for item in value
                ]
            else:
                output[str(key)] = result_to_jsonable(value)
        return output
    if isinstance(obj, (list, tuple)):
        return [result_to_jsonable(v) for v in obj]
    if isinstance(obj, float) and not np.isfinite(obj):
        raise ValueError('Non-finite analysis result')
    return obj


def write_result(result_data: Dict[str, Any], file_path: str) -> None:
    """結果データをファイルに書き込む
    
    Args:
        result_data: 結果データ
        file_path: 出力ファイルパス
    """
    output_data = result_to_jsonable(result_data)
    
    with open(file_path, 'w', encoding='utf-8') as f:
        json.dump(output_data, f, indent=2, ensure_ascii=False, allow_nan=False)


def _vtk_lookup(values: Dict[int, Any], item_id: int):
    if item_id in values:
        return values[item_id]
    return values.get(str(item_id))


def _vtk_vector(value: Any, names: tuple[str, str, str], label: str) -> np.ndarray:
    if isinstance(value, dict):
        if not all(name in value for name in names):
            raise ValueError(f"{label} requires {names}")
        result = np.array([value[name] for name in names], dtype=float)
    else:
        result = np.asarray(value, dtype=float)[:3]
    if result.shape != (3,) or np.isinf(result).any():
        raise ValueError(f"{label} requires three numeric values without infinity")
    return result


def _vtk_symmetric_tensor(value: Any, label: str, *, engineering_shear: bool = False) -> np.ndarray:
    components = np.asarray(value, dtype=float)
    if components.shape != (6,) or np.isinf(components).any():
        raise ValueError(f"{label} requires [xx, yy, zz, xy, yz, zx]")
    xx, yy, zz, xy, yz, zx = components
    if engineering_shear:
        xy, yz, zx = xy / 2, yz / 2, zx / 2
    return np.array([[xx, xy, zx], [xy, yy, yz], [zx, yz, zz]])


def _vtk_point_data(nodes: Dict[int, Any], result_data: Dict[str, Any]) -> Dict[str, Dict[int, Any]]:
    output: Dict[str, Dict[int, Any]] = {"node_id": {node_id: node_id for node_id in nodes}}
    specifications = (
        ("node_displacements", "displacement", ("dx", "dy", "dz")),
        ("reaction_forces", "reaction_force", ("fx", "fy", "fz")),
    )
    for source_name, vtk_name, components in specifications:
        source = result_data.get(source_name)
        if not source:
            continue
        output[vtk_name] = {}
        for node_id in nodes:
            value = _vtk_lookup(source, node_id)
            output[vtk_name][node_id] = (
                np.full(3, np.nan) if value is None
                else _vtk_vector(value, components, f"{source_name}[{node_id}]")
            )
    return output


def _vtk_cell_data(elements: Dict[int, Any], result_data: Dict[str, Any]) -> Dict[str, Dict[int, Any]]:
    element_ids = list(elements)
    output: Dict[str, Dict[int, Any]] = {
        "element_id": {element_id: element_id for element_id in element_ids}
    }

    def scalar_field(name: str):
        return output.setdefault(name, {element_id: np.nan for element_id in element_ids})

    def tensor_field(name: str):
        return output.setdefault(name, {element_id: np.full((3, 3), np.nan) for element_id in element_ids})

    stresses = result_data.get("element_stresses") or {}
    force_names = ("fx", "fy", "fz", "mx", "my", "mz")
    solid_types = {"tetra", "tetra2", "wedge", "wedge2", "hexa", "hexa2"}
    for element_id in element_ids:
        value = _vtk_lookup(stresses, element_id)
        if value is None:
            continue
        element_type = str(elements[element_id].get("type", "")).lower()
        if isinstance(value, dict) and ("i_end" in value or "j_end" in value):
            for end in ("i", "j"):
                forces = np.asarray(value.get(f"{end}_end"), dtype=float)
                if forces.shape != (6,) or not np.isfinite(forces).all():
                    raise ValueError(f"element_stresses[{element_id}].{end}_end requires six finite values")
                for component, number in zip(force_names, forces):
                    scalar_field(f"section_force_{end}_{component}")[element_id] = number
            continue
        if isinstance(value, dict) and element_type in solid_types and "stress" in value:
            stress = np.asarray(value["stress"], dtype=float)
            if stress.ndim != 2 or stress.shape[1] != 6 or not np.isfinite(stress).all():
                raise ValueError(f"element_stresses[{element_id}].stress requires finite Gauss-point rows of six")
            tensor_field("solid_stress_gauss_point_mean")[element_id] = _vtk_symmetric_tensor(
                stress.mean(axis=0), f"element_stresses[{element_id}].stress"
            )
            if "strain" in value:
                strain = np.asarray(value["strain"], dtype=float)
                if strain.ndim != 2 or strain.shape[1] != 6 or not np.isfinite(strain).all():
                    raise ValueError(f"element_stresses[{element_id}].strain requires finite Gauss-point rows of six")
                tensor_field("solid_strain_gauss_point_mean")[element_id] = _vtk_symmetric_tensor(
                    strain.mean(axis=0), f"element_stresses[{element_id}].strain", engineering_shear=True
                )
            continue
        if isinstance(value, dict):
            tensor_keys = ("xx", "yy", "zz", "xy", "yz", "zx")
            if all(key in value for key in tensor_keys):
                tensor_field("stress")[element_id] = _vtk_symmetric_tensor(
                    [value[key] for key in tensor_keys], f"element_stresses[{element_id}]"
                )
                continue
            for component, number in value.items():
                if np.asarray(number).shape == () and np.issubdtype(np.asarray(number).dtype, np.number):
                    scalar_field(f"stress_{component}")[element_id] = float(number)
        elif np.asarray(value).shape == ():
            scalar_field("stress")[element_id] = float(value)

    shells = result_data.get("shell_results") or {}
    resultant_names = {
        "membrane": ("nx", "ny", "nxy"),
        "moment": ("mx", "my", "mxy"),
        "shear": ("qx", "qy"),
    }
    for element_id in element_ids:
        shell = _vtk_lookup(shells, element_id)
        if shell is None:
            continue
        resultants = shell.get("resultants", {})
        for family, components in resultant_names.items():
            values = np.asarray(resultants.get(family), dtype=float)
            if values.shape != (len(components),) or not np.isfinite(values).all():
                raise ValueError(f"shell_results[{element_id}].resultants.{family} has invalid values")
            for component, number in zip(components, values):
                scalar_field(f"shell_{family}_{component}")[element_id] = number
        raw = shell.get("raw_result", {})
        for source, name in (
            ("elemStress1", "shell_stress_surface_1"),
            ("elemStress2", "shell_stress_surface_2"),
            ("elemStrain1", "shell_strain_surface_1"),
            ("elemStrain2", "shell_strain_surface_2"),
        ):
            tensor_field(name)[element_id] = _vtk_symmetric_tensor(
                raw.get(source), f"shell_results[{element_id}].raw_result.{source}"
            )
    return output


def write_vtk(model_data: Dict[str, Any], result_data: Dict[str, Any], file_path: str) -> None:
    """Write supported mesh and named result quantities as Legacy ASCII VTK."""
    from .vtk_writer import VTKWriter

    mesh = model_data.get("mesh")
    if mesh is None:
        raise ValueError("model_dataにmeshが含まれていません")
    with VTKWriter(file_path) as writer:
        writer.write_header()
        writer.write_points(mesh.nodes)
        writer.write_cells(mesh.elements)
        writer.write_point_data(_vtk_point_data(mesh.nodes, result_data))
        writer.write_cell_data(_vtk_cell_data(mesh.elements, result_data))
