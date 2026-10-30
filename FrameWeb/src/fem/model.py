"""
FEM解析の統合モデルクラス
JavaScript版のFemDataModelに対応し、新モジュールを統合
"""
from typing import Dict, Any, List, Optional, Union
from copy import deepcopy
import logging
import numpy as np
from .mesh import MeshModel
from .boundary_condition import BoundaryCondition
from .material import Material, MaterialProperty, ShellParameter, BarParameter, NonlinearMaterialProperty
from .section import Section
from .solver import Solver
from .solver_results import (
    build_result_metadata,
    legacy_nonlinear_result,
    normalize_model_metadata,
)
from .capabilities import validate_analysis_capabilities
from .spatial_loads import SpatialLoadDefinitions
from .spatial_loads.validation import validate_references as validate_spatial_references
from .diagnostics import InputValidationError, UnsupportedAnalysisError
from .nonlinear import NonlinearSolver
from .nonlinear.hysteresis import JRStiffnessReductionParams
from .nonlinear.axial_force_table import AxialForceTable
from .nonlinear.component_laws import laws_from_dict
from .file_io import read_model, write_model, read_result, write_result
from .elements import (
    BarElement, BEBarElement, TBarElement,
    ShellElement, SolidElement, AdvancedElement
)
from .elements.nonlinear_bar_element import NonlinearBarElement
from .load_kinds import row_kind
from .result_processor import ResultProcessor
import math


logger = logging.getLogger(__name__)


class FemModel:
    """FEM解析の統合モデルクラス（Facade）"""
    
    def __init__(self):
        """FEMモデルを初期化"""
        self.mesh = MeshModel()
        self.boundary = BoundaryCondition()
        self.material = Material()
        self.section = Section()
        self.solver = Solver()
        self.nonlinear_solver = NonlinearSolver(self.solver)  # 旧APIからも同じ解析状態を参照
        self.elements: Dict[int, Any] = {}
        # Public state contains only a validated AnalysisResultSet.  The
        # solver-native dictionary is deliberately kept behind the internal
        # snapshot boundary used by projection and numerical tests.
        self.results: Optional[Dict[str, Any]] = None
        self._solver_snapshot: Optional[Dict[str, Any]] = None
        self.analysis_type = None
        self.analysis_params = {
            'n_load_steps': 10, 'max_iterations': 50,
            'tolerance': 1e-6, 'n_modes': 10,
        }
        self.model_metadata = normalize_model_metadata(None)
        self.name: str = "Untitled Model"
        self.description: str = ""
        
    def load_model(self, file_path: str) -> None:
        """モデルファイルを読み込んでFEMモデルを構築
        
        Args:
            file_path: モデルファイルのパス
        """
        # ファイル読み込み
        self.results = None
        self._solver_snapshot = None
        model_data = read_model(file_path)
        
        return self.read_json_model(model_data)


    def read_json_model(self, model_data: Dict[str, Any]) -> None:
        self.results = None
        self._solver_snapshot = None
        self.analysis_type = model_data.get('analysis_type')
        self.model_metadata = normalize_model_metadata(model_data.get('model_metadata'))
        # データの設定（ここで初期節点数と要素数を記録）
        initial_node_count = len(model_data.get('mesh', MeshModel()).nodes)
        initial_elem_count = len(model_data.get('mesh', MeshModel()).elements)

        self.node_labels = model_data.get('node_labels', {})
        self.mesh = model_data.get('mesh', MeshModel())
        self.boundary = model_data.get('boundary', BoundaryCondition())
        self.material = model_data.get('material', Material())
        self.section = model_data.get('section', Section())

        # 解析パラメータの設定（JSONのloadセクションから読み込み）
        if 'analysis_params' in model_data:
            self.analysis_params = model_data['analysis_params']
        else:
            self.analysis_params = {
                'n_load_steps': 10,
                'max_iterations': 50,
                'tolerance': 1e-6,
                'n_modes': 10,
            }
        
        # notice_pointsの処理
        if 'notice_points' in model_data:
            logger.info("着目点による要素分割の前: 節点数=%d, 要素数=%d", len(self.mesh.nodes), len(self.mesh.elements))
            self.add_notice_points(model_data['notice_points'])
            logger.info("着目点による要素分割の後: 節点数=%d, 要素数=%d", len(self.mesh.nodes), len(self.mesh.elements))
        
        # 分布荷重と集中荷重による要素分割
        if 'load' in model_data:
            logger.info("load_modelメソッド: 荷重データを処理開始")
            # すべての荷重ケースから荷重データを抽出
            all_loads = self._extract_all_loads(model_data['load'])
            
            # 分布荷重による要素分割
            if all_loads['distributed']:
                logger.info("分布荷重による要素分割の前: 節点数=%d, 要素数=%d", len(self.mesh.nodes), len(self.mesh.elements))
                logger.info("分布荷重による要素分割: %d個の分布荷重を処理", len(all_loads['distributed']))
                
                # 具体的な分布荷重データを表示
                for i, load in enumerate(all_loads['distributed'][:5]):  # 最初の5件のみ表示
                    logger.debug("分布荷重%d: 要素%s, L1=%s, L2=%s", i + 1, load['element_id'], load['start_position'], load['end_position'])
                if len(all_loads['distributed']) > 5:
                    logger.debug("ほか %d個の分布荷重", len(all_loads['distributed']) - 5)
                
                self._divide_element_by_distributed_loads(all_loads['distributed'])
                logger.info("分布荷重による要素分割の後: 節点数=%d, 要素数=%d", len(self.mesh.nodes), len(self.mesh.elements))
                
            # 集中荷重による要素分割
            if all_loads['concentrated']:
                logger.info("集中荷重による要素分割の前: 節点数=%d, 要素数=%d", len(self.mesh.nodes), len(self.mesh.elements))
                logger.info("集中荷重による要素分割: %d個の集中荷重を処理", len(all_loads['concentrated']))
                
                # 具体的な集中荷重データを表示
                for i, load in enumerate(all_loads['concentrated'][:5]):  # 最初の5件のみ表示
                    logger.debug("集中荷重%d: 要素%s, 位置=%s", i + 1, load['element_id'], load['position'])
                if len(all_loads['concentrated']) > 5:
                    logger.debug("ほか %d個の集中荷重", len(all_loads['concentrated']) - 5)
                
                self._divide_element_by_concentrated_loads(all_loads['concentrated'])
                logger.info("集中荷重による要素分割の後: 節点数=%d, 要素数=%d", len(self.mesh.nodes), len(self.mesh.elements))
            
            logger.info("load_modelメソッド: 荷重データの処理完了")
        
        # 要素の作成
        self._create_elements()
        
        # 要約情報の出力
        logger.info(
            "要素分割まとめ: 初期=%d節点/%d要素, 最終=%d節点/%d要素, 追加=%d節点/%d要素",
            initial_node_count, initial_elem_count, len(self.mesh.nodes), len(self.mesh.elements),
            len(self.mesh.nodes) - initial_node_count, len(self.mesh.elements) - initial_elem_count,
        )
        
        # 要素タイプの内訳
        elem_types = {}
        for elem_data in self.mesh.elements.values():
            elem_type = elem_data['type']
            elem_types[elem_type] = elem_types.get(elem_type, 0) + 1
            
        logger.info("要素タイプの内訳")
        for elem_type, count in elem_types.items():
            logger.info("%s: %d個", elem_type, count)

    
    def save_model(self, file_path: str) -> None:
        """モデルをファイルに保存
        
        Args:
            file_path: 保存先ファイルパス
        """
        model_data = {
            'mesh': self.mesh,
            'boundary': self.boundary,
            'material': self.material,
            'section': self.section,
            'analysis_type': self.analysis_type,
            'analysis_params': self.analysis_params,
            'model_metadata': self.model_metadata,
        }
        write_model(model_data, file_path)
        
    def set_spatial_loads(self, definitions: SpatialLoadDefinitions) -> None:
        """Replace unexpanded spatial input after structural-reference validation.

        Import definitions from ``fem.spatial_loads``. Linear static analysis
        accepts planar panels with nonconvex boundaries and declared holes.
        An explicit LocalPlane enables inclined panels; LoadDirection selects
        a unit global direction or the oriented panel normal. Defaults are XY/+Z.
        Line intensity has units force/length; area intensity force/length**2.
        Negative intensity acts in -Z. Geometry and conservation are checked
        on every ``run()`` before stiffness assembly. See the Wiki spatial-loads
        page for topology, JSON examples, output and unsupported combinations.
        """
        validate_spatial_references(definitions, self.mesh)
        self.boundary.spatial_loads = definitions
        self.results = None
        self._solver_snapshot = None

    def add_node(self, node_id: int, x: float, y: float, z: float) -> None:
        """節点を追加
        
        Args:
            node_id: 節点ID
            x, y, z: 座標
        """
        self.mesh.add_node(node_id, [x, y, z])
        
    def add_element(self, elem_id: int, elem_type: str, node_ids: List[int],
                   material_id: int, **kwargs) -> None:
        """要素を追加
        
        Args:
            elem_id: 要素ID
            elem_type: 要素タイプ
            node_ids: 構成節点ID
            material_id: 材料ID
            **kwargs: その他のパラメータ
        """
        self.mesh.add_element(elem_id, elem_type, node_ids, material_id, **kwargs)
        
        # 要素インスタンスを作成
        self._create_element_instance(elem_id)
        
    def add_material(self, material_id: int, name: str, E: float, nu: float,
                    density: Optional[float] = None, **kwargs) -> None:
        """材料を追加
        
        Args:
            material_id: 材料ID
            name: 材料名
            E: ヤング率
            nu: ポアソン比
            density: 密度
            **kwargs: その他のパラメータ
        """
        mat_prop = MaterialProperty(
            name=name,
            E=E,
            nu=nu,
            density=density,
            **kwargs
        )
        self.material.add_material(material_id, mat_prop)
        
    def add_restraint(self, node_id: int, dx: bool = False, dy: bool = False,
                     dz: bool = False, rx: bool = False, ry: bool = False,
                     rz: bool = False, values: Optional[List[float]] = None) -> None:
        """拘束条件を追加
        
        Args:
            node_id: 節点ID
            dx, dy, dz: 並進拘束フラグ
            rx, ry, rz: 回転拘束フラグ
            values: 強制変位値
        """
        dof_restraints = [dx, dy, dz, rx, ry, rz]
        self.boundary.add_restraint(node_id, dof_restraints, values)
        
    def add_load(self, node_id: int, fx: float = 0, fy: float = 0, fz: float = 0,
                mx: float = 0, my: float = 0, mz: float = 0) -> None:
        """節点荷重を追加
        
        Args:
            node_id: 節点ID
            fx, fy, fz: 力成分
            mx, my, mz: モーメント成分
        """
        forces = [fx, fy, fz, mx, my, mz]
        self.boundary.add_load(node_id, forces)
        
    def add_spring_support(self, node_id: int, direction: str, stiffness: float) -> None:
        """バネ支点を追加
        
        Args:
            node_id: 節点ID
            direction: 方向 ('x', 'y', 'z', 'rx', 'ry', 'rz')
            stiffness: バネ定数
        """
        if direction in self.boundary.nonlinear_spring_supports.get(node_id, {}):
            raise ValueError(f'node {node_id} direction {direction}: conflicting nonlinear spring')
        # 既存の拘束条件を取得または新規作成
        restraint = self.boundary.get_restraint(node_id)
        if restraint is None:
            # 新規に拘束条件を作成（すべてFalse=自由）
            self.boundary.add_restraint(node_id, [False] * 6, None)
            restraint = self.boundary.get_restraint(node_id)
            
        # SpringSupportオブジェクトを作成して追加
        if not hasattr(self.boundary, 'spring_supports'):
            self.boundary.spring_supports = {}
            
        if node_id not in self.boundary.spring_supports:
            self.boundary.spring_supports[node_id] = {}
            
        self.boundary.spring_supports[node_id][direction] = stiffness

    def add_slip_spring_support(self, node_id: int, direction: str,
                                K1: float, K2: float, delta_1: float) -> None:
        """Add a global grounded slip spring; all parameters use consistent units."""
        self.add_nonlinear_spring_support(
            node_id, direction, dict(type='slip', K1=K1, K2=K2, delta_1=delta_1))

    def add_nonlinear_spring_support(self, node_id: int, direction: str, definition) -> None:
        """Add a global grounded nonlinear support spring.

        definition is the same object as in JSON (type "slip", "jrsnap_bilinear"
        or "jrsnap_trilinear") or its parsed parameters; units are consistent.
        """
        from .nonlinear.support_springs import add_nonlinear_support, validate_support_definitions
        location = f'nonlinear_spring_supports node {node_id} direction {direction}'
        if isinstance(node_id, (bool, np.bool_)) or not isinstance(node_id, (int, np.integer)):
            raise ValueError(f'{location}: node must be an integer')
        if not isinstance(direction, str):
            raise ValueError(f'{location}: direction must be a string')
        candidate = deepcopy(self.boundary)
        add_nonlinear_support(candidate, node_id, direction, definition, location)
        validate_support_definitions(candidate, self.mesh)
        self.boundary.nonlinear_spring_supports = candidate.nonlinear_spring_supports
        
    def add_distributed_load(self, element_id: int, direction: str, 
                           value_i: float, value_j: float) -> None:
        """分布荷重を追加
        
        Args:
            element_id: 要素ID
            direction: 荷重方向 ('local_x', 'local_y', 'local_z', 'local_r', 
                                 'global_x', 'global_y', 'global_z')
            value_i: i端の荷重値
            value_j: j端の荷重値
        """
        # 方向の変換（旧形式との互換性のため）
        direction_map = {
            'local_x': 'Lx',
            'local_y': 'Ly', 
            'local_z': 'Lz',
            'local_r': 'Lr',
            'global_x': 'GX',
            'global_y': 'GY',
            'global_z': 'GZ'
        }
        load_type = direction_map.get(direction, direction)
        
        values = [value_i, value_j]
        if element_id in self.mesh.elements and self.mesh.elements[element_id]['type'] in ('bar','beam'):
            self.mesh.elements[element_id].setdefault('line_loads', []).append(dict(direction=load_type, values=values))
        else:
            self.boundary.add_distributed_load(element_id, load_type, values)
        
    def add_temperature_load(self, element_id: int, temperature: float) -> None:
        """温度荷重を追加
        
        Args:
            element_id: 要素ID
            temperature: 温度変化
        """
        if not np.isfinite(temperature):
            raise ValueError('Temperature must be finite')
        self.mesh.elements[element_id]['temperature'] = float(temperature)
        
    def add_forced_displacement(self, node_id: int, dx: float = 0, dy: float = 0, 
                              dz: float = 0, rx: float = 0, ry: float = 0, 
                              rz: float = 0) -> None:
        """強制変位を追加
        
        Args:
            node_id: 節点ID
            dx, dy, dz: 変位成分
            rx, ry, rz: 回転成分
        """
        # 強制変位は拘束条件として扱う
        values = [dx, dy, dz, rx, ry, rz]
        # 非ゼロの成分を拘束として設定
        dof_restraints = [v != 0 for v in values]
        
        # 既存の拘束条件と結合
        existing = self.boundary.get_restraint(node_id)
        if existing:
            # 既存の拘束と結合
            for i in range(6):
                if values[i] != 0:
                    existing.dof_restraints[i] = True
                    existing.values[i] = values[i]
        else:
            self.boundary.add_restraint(node_id, dof_restraints, values)
            
    def add_distributed_spring(self, element_id: int, dx: float = 0, dy: float = 0,
                             dz: float = 0, rx: float = 0) -> None:
        """分布バネを追加
        
        Args:
            element_id: 要素ID
            dx, dy, dz: 各方向の分布バネ定数
            rx: 回転方向の分布バネ定数
        """
        values = np.asarray([dx, dy, dz, rx], dtype=float)
        if not np.all(np.isfinite(values)) or np.any(values < 0):
            raise ValueError('Distributed spring stiffness must be finite and nonnegative')
        self.mesh.elements[element_id]['foundation'] = values.tolist()
        
    def add_joint_condition(self, element_id: int, *args, **kwargs) -> None:
        """結合条件を追加
        
        Args:
            element_id: 要素ID
            *args, **kwargs: 結合条件のパラメータ
        """
        names = ('xi','yi','zi','xj','yj','zj')
        if len(args) > 6 or set(kwargs)-set(names):
            raise ValueError('Joint accepts xi, yi, zi, xj, yj, zj (0 released, 1 connected)')
        values = dict(zip(names,args))
        values.update(kwargs)
        if any(v not in (0,1) for v in values.values()):
            raise ValueError('Joint flags must be 0 or 1')
        self.mesh.elements[element_id]['releases'] = [i for i,k in zip((3,4,5,9,10,11),names)
                                                     if values.get(k,1) == 0]
        
    def run(self, analysis_type: Optional[str] = None) -> Dict[str, Any]:
        """Run this programmatic model and return the sole public result contract."""
        # Local import keeps the application service pointed inward without
        # making model construction depend on result orchestration at import time.
        from .analysis_result_sets import _build_model_analysis_result_set

        self.results = None
        try:
            result_set = _build_model_analysis_result_set(self, analysis_type)
        except Exception:
            self.results = None
            raise
        self.results = result_set
        return result_set

    def _run_solver_snapshot(self, analysis_type: Optional[str] = None) -> Dict[str, Any]:
        """Internal solver/post-processing snapshot used by canonical projection."""
        self._solver_snapshot = None
        self.solver.clear_spatial_load_state()
        try:
            return self._run(analysis_type)
        except np.linalg.LinAlgError:
            self._solver_snapshot = None
            raise
        except ValueError as error:
            self._solver_snapshot = None
            if getattr(error, 'error_code', None):
                raise
            raise InputValidationError(str(error)) from error
        except Exception:
            self._solver_snapshot = None
            raise
        finally:
            if self._solver_snapshot is None:
                self.solver.clear_spatial_load_state()

    def _run(self, analysis_type: Optional[str] = None) -> Dict[str, Any]:
        """解析を実行

        Args:
            analysis_type: 解析タイプ
                - 'static': 線形静解析
                - 'modal': モード解析
                - 'material_nonlinear': 材料非線形解析

        Note:
            解析パラメータはJSONファイルのloadセクションで指定:
                - n_load_steps: 荷重増分ステップ数（material_nonlinear用）
                - max_iterations: 最大反復回数（material_nonlinear用）
                - tolerance: 収束判定許容差（material_nonlinear用）
                - n_modes: 固有モード数（modal用）
                - load_factors: 順番どおりに載荷する係数列（material_nonlinear用）。
                  指定時はn_load_stepsの等間隔列に代えて使用する。
                - displacement_control: node、dofとtargetまたはtargetsで指定する変位制御。
                  荷重係数を未知数として負勾配を追跡する。load_factorsとは排他。
            analysis_type省略時は入力の指定を使い、それもなければ
            nonlinear_barまたは非線形支持ばね（スリップ・JRSNAP互換）を含む場合material_nonlinear、それ以外はstatic。

        Returns:
            解析結果
        """
        # 再解析が失敗した際に、前回の結果を今回の結果として残さない。
        if not self.mesh.elements:
            raise InputValidationError(
                'Model must contain structural elements (missing topology)'
            )
        if analysis_type is None:
            analysis_type = self.analysis_type
        if analysis_type is None:
            analysis_type = ('material_nonlinear' if self.boundary.nonlinear_spring_supports
                            or any(e['type'] == 'nonlinear_bar' for e in self.mesh.elements.values()) else 'static')
        if analysis_type not in ('static', 'modal', 'material_nonlinear'):
            raise UnsupportedAnalysisError(
                f'Unknown analysis type: {analysis_type}',
                analysis_type=analysis_type,
            )
        # Recheck references after user mesh edits, before element constructors
        # read coordinates, so spatial errors retain their panel/node IDs.
        if self.boundary.spatial_loads.loads:
            validate_spatial_references(self.boundary.spatial_loads, self.mesh)
        # 要素の作成（要素分割後に再実行が必要なため毎回実行）
        self._create_elements()

        # 未対応の要素・解析・荷重組合せを、行列組立より前にID付きで拒否する。
        validate_analysis_capabilities(
            self.mesh.elements, self.elements, self.boundary, analysis_type
        )

        # 節点座標を要素に設定
        self._set_element_coordinates()

        if analysis_type == 'modal':
            self._solver_snapshot = self.solver.eigenvalue_analysis(
                self.mesh, self.material, self.boundary, self.elements,
                n_modes=self.analysis_params.get('n_modes', 10)
            )
        else:
            self._solver_snapshot = self.solver.solve(
                self.mesh, self.material, self.boundary, self.elements,
                analysis_type=analysis_type,
                n_steps=self.analysis_params.get('n_load_steps', 10),
                max_iter=self.analysis_params.get('max_iterations', 50),
                tol=self.analysis_params.get('tolerance', 1e-6),
                load_factors=(self.analysis_params.get('load_factors')
                              if analysis_type == 'material_nonlinear' else None),
                displacement_control=(self.analysis_params.get('displacement_control')
                                      if analysis_type == 'material_nonlinear' else None)
            )
            if analysis_type == 'material_nonlinear':
                legacy_nonlinear_result(self._solver_snapshot, self.solver.layout.stride)

        # 結果の後処理
        self._solver_snapshot['analysis_type'] = analysis_type
        try:
            self._post_process_results()
        except Exception:
            self._solver_snapshot = None
            raise

        self._solver_snapshot['metadata'] = build_result_metadata(
            self, analysis_type, self._solver_snapshot
        )

        return self._solver_snapshot

    def add_nonlinear_material(
        self,
        material_id: int,
        name: str,
        E: float,
        delta_1: float,
        delta_2: float,
        delta_3: float,
        P_1: float,
        P_2: float,
        P_3: float,
        beta: float = 0.4,
        K_min: Optional[float] = None,
        symmetric: bool = True,
        delta_1_neg: Optional[float] = None,
        delta_2_neg: Optional[float] = None,
        delta_3_neg: Optional[float] = None,
        P_1_neg: Optional[float] = None,
        P_2_neg: Optional[float] = None,
        P_3_neg: Optional[float] = None,
        nu: float = 0.2,
        density: Optional[float] = None,
        shear_modulus: Optional[float] = None,
        delta_4: Optional[float] = None,
        P_4: Optional[float] = None,
        delta_4_neg: Optional[float] = None,
        P_4_neg: Optional[float] = None
    ) -> None:
        """非線形材料を追加

        JR総研剛性低減RC型のスケルトンカーブパラメータを定義

        Args:
            material_id: 材料ID
            name: 材料名
            E: 初期ヤング率
            delta_1, delta_2, delta_3: 一般化ひずみの折れ点（正側）。
                axialは軸ひずみ、moment_y/zは曲率、torsionはねじり率。
                絶対端部変位・集中ヒンジ回転ではない（理論7.21節）。
            P_1, P_2, P_3: 対応する軸力または断面モーメント（正側）
            delta_4, P_4: K4を定める正側参照点。両方省略時はK4=0。
            beta: 剛性低減係数（デフォルト: 0.4）
            K_min: 戻り剛性下限値（省略時は初期剛性の1%）
            symmetric: 対称スケルトンカーブを使用するか
            delta_1_neg, ...: 負側パラメータ（symmetric=Falseの場合に使用）
            nu: ポアソン比
            density: 密度
            shear_modulus: 基準Gの明示値。省略時はE/(2*(1+nu))。
        """
        if symmetric:
            mat = NonlinearMaterialProperty(
                name=name, E=E, nu=nu,
                delta_1_pos=delta_1, delta_2_pos=delta_2, delta_3_pos=delta_3,
                P_1_pos=P_1, P_2_pos=P_2, P_3_pos=P_3,
                delta_4_pos=delta_4, P_4_pos=P_4,
                beta=beta, K_min=K_min, density=density
            )
        else:
            mat = NonlinearMaterialProperty(
                name=name, E=E, nu=nu,
                delta_1_pos=delta_1, delta_2_pos=delta_2, delta_3_pos=delta_3,
                P_1_pos=P_1, P_2_pos=P_2, P_3_pos=P_3,
                delta_1_neg=delta_1_neg, delta_2_neg=delta_2_neg, delta_3_neg=delta_3_neg,
                P_1_neg=P_1_neg, P_2_neg=P_2_neg, P_3_neg=P_3_neg,
                delta_4_pos=delta_4, P_4_pos=P_4,
                delta_4_neg=delta_4_neg, P_4_neg=P_4_neg,
                beta=beta, K_min=K_min, density=density
            )

        self.material.add_nonlinear_material(material_id, mat)

        # 線形解析用のMaterialPropertyも追加（互換性のため）
        linear_mat = MaterialProperty(name=name, E=E, nu=nu, density=density,
                                      shear_modulus=shear_modulus)
        self.material.add_material(material_id, linear_mat)

    def add_nonlinear_material_laws(
        self,
        material_id: int,
        name: str,
        E: float,
        laws: Dict[str, Dict[str, Any]],
        nu: float = 0.2,
        density: Optional[float] = None,
        shear_modulus: Optional[float] = None,
    ) -> None:
        """Add component-specific nonlinear laws, including Nd tables.

        Law keys define their application axes. Nd-dependent definitions are
        limited to ``moment_y`` and ``moment_z``; fixed JR definitions may be
        assigned independently to any supported component.
        """
        parsed = laws_from_dict(laws)
        linear = MaterialProperty(
            name=name, E=E, nu=nu, density=density,
            shear_modulus=shear_modulus,
        )
        self.material.add_material(material_id, linear)
        self.material.add_nonlinear_laws(material_id, parsed)

    def add_nonlinear_bar_element(
        self,
        elem_id: int,
        node_ids: List[int],
        material_id: int,
        section_id: int,
        hysteresis_dofs: Optional[List[str]] = None,
        angle: float = 0.0,
        shear_correction: bool = True
    ) -> None:
        """非線形梁要素を追加

        Args:
            elem_id: 要素ID
            node_ids: 構成節点ID
            material_id: 材料ID
            section_id: 断面ID
            hysteresis_dofs: 非線形を適用する自由度のリスト
                例: ['moment_y'], ['axial', 'moment_y', 'moment_z']
            angle: 要素座標軸の回転角
            shear_correction: せん断変形を考慮するか
        """
        if hysteresis_dofs is None:
            hysteresis_dofs = list(self.material.get_nonlinear_laws(material_id))
        self.mesh.add_element(
            elem_id, 'nonlinear_bar', node_ids, material_id,
            section_id=section_id, angle=angle,
            shear_correction=shear_correction,
            hysteresis_dofs=hysteresis_dofs
        )

    def get_results(self) -> Optional[Dict[str, Any]]:
        """Return the validated public AnalysisResultSet, or ``None``."""
        if self.results is not None:
            from .result_contracts import validate_analysis_result_set

            validate_analysis_result_set(self.results)
        return self.results

    def get_node_displacement(
        self,
        node_id: Union[int, str],
        *,
        case_id: Optional[str] = None,
        state_index: Optional[int] = None,
    ) -> Optional[Dict[str, float]]:
        """Select canonical displacement components for one result state.

        A single static state or the final nonlinear state is selected by
        default.  ``case_id`` is required for multi-case result sets, and
        ``state_index`` is required when no unique final state exists (for
        example, modal results).  Modal shapes are not displacements.
        """
        result = self._select_public_result(case_id=case_id, state_index=state_index)
        if result is None or 'node_displacements' not in result:
            return None

        public_id = str(node_id)
        for row in result['node_displacements']:
            if row['node_id'] == public_id:
                return row['components']
        return None

    def get_element_stress(
        self,
        elem_id: Union[int, str],
        *,
        case_id: Optional[str] = None,
        state_index: Optional[int] = None,
    ) -> Optional[Dict[str, Any]]:
        """Select one canonical member, shell, or solid result row.

        Member rows contain section forces; shell and solid rows contain the
        stress/resultant fields defined by AnalysisResultSet v1.
        """
        from .result_contracts import ResultContractError

        result = self._select_public_result(case_id=case_id, state_index=state_index)
        if result is None:
            return None

        public_id = str(elem_id)
        matches = [
            row
            for collection, key in (
                ('member_section_forces', 'member_id'),
                ('shell_results', 'element_id'),
                ('solid_results', 'element_id'),
            )
            for row in result.get(collection, ())
            if row[key] == public_id
        ]
        if len(matches) > 1:
            raise ResultContractError(
                f"Element ID {public_id!r} is ambiguous across canonical result categories"
            )
        return matches[0] if matches else None

    def _select_public_result(
        self,
        *,
        case_id: Optional[str],
        state_index: Optional[int],
    ) -> Optional[Dict[str, Any]]:
        """Resolve one validated AnalysisResult row for public accessors."""
        from .result_contracts import ResultContractError

        result_set = self.get_results()
        if result_set is None:
            return None
        case_ids = [case['case_id'] for case in result_set['cases']]
        if case_id is None:
            if len(case_ids) != 1:
                raise ResultContractError(
                    "case_id is required when selecting from a multi-case result set"
                )
            case_id = case_ids[0]
        elif case_id not in case_ids:
            return None

        matches = [row for row in result_set['results'] if row['case_id'] == case_id]
        if state_index is not None:
            matches = [row for row in matches if row['state']['index'] == state_index]
            return matches[0] if matches else None
        if len(matches) == 1:
            return matches[0]
        finals = [row for row in matches if row['state'].get('is_final') is True]
        if len(finals) == 1:
            return finals[0]
        raise ResultContractError(
            "state_index is required when the selected case has no unique final result"
        )
        
    def save_results(self, file_path: str) -> None:
        """Validate and save the public AnalysisResultSet as JSON."""
        result_set = self.get_results()
        if result_set is None:
            raise ValueError("No results to save")

        write_result(result_set, file_path)

    def load_results(self, file_path: str) -> None:
        """Load and validate a public AnalysisResultSet from JSON."""
        from .result_contracts import validate_analysis_result_set

        result_set = read_result(file_path)
        validate_analysis_result_set(result_set)
        self.results = result_set
        
    def _create_elements(self) -> None:
        """メッシュデータから要素インスタンスを作成"""
        self.elements.clear()
        
        for elem_id, elem_data in self.mesh.elements.items():
            self._create_element_instance(elem_id)
            
    def _create_element_instance(self, elem_id: int) -> None:
        """単一の要素インスタンスを作成"""
        elem_data = self.mesh.elements.get(elem_id)
        if not elem_data:
            return
            
        elem_type = elem_data['type']
        node_ids = elem_data['nodes']
        material_id = elem_data['material_id']
        
        # ✅ V0技術資産の要素タイプ名を認識（V0互換性確保）
        v0_shell_elements = {
            'TriElement1': 'shell',     # 3節点三角形Shell要素
            'QuadElement1': 'shell',    # 4節点四角形Shell要素
            'ShellElement': 'shell'     # 汎用Shell要素
        }
        
        v0_bar_elements = {
            'BarElement': 'bar',        # 棒要素
            'BeamElement': 'bar',       # 梁要素
            'TrussElement': 'bar'       # トラス要素  
        }
        
        v0_solid_elements = {
            'TetraElement1': 'tetra', 'WedgeElement1': 'wedge', 'HexaElement1': 'hexa',
            'TetraElement2': 'tetra2', 'WedgeElement2': 'wedge2', 'HexaElement2': 'hexa2',
            'TetraElement': 'tetra',    # 四面体要素
            'HexaElement': 'hexa',      # 六面体要素
            'WedgeElement': 'wedge'     # くさび要素
        }
        
        # V0要素タイプ名を標準名に変換
        if elem_type in v0_shell_elements:
            elem_type = v0_shell_elements[elem_type]
            logger.debug(f"V0互換: {elem_data['type']} -> {elem_type} (要素ID: {elem_id})")
        elif elem_type in v0_bar_elements:
            elem_type = v0_bar_elements[elem_type]
        elif elem_type in v0_solid_elements:
            elem_type = v0_solid_elements[elem_type]
        
        # 要素タイプに応じてインスタンスを作成
        if elem_type == 'bar' or elem_type == 'beam':
            section_id = elem_data.get('section_id', 1)
            angle = elem_data.get('angle', 0)
            shear_correction = elem_data.get('shear_correction', True)
            
            if shear_correction and not (elem_data.get('releases') or any(elem_data.get('foundation', []))
                                         or elem_data.get('line_loads') or elem_data.get('temperature')):
                element = TBarElement(elem_id, node_ids, material_id, 
                                    section_id, angle, shear_correction)
            else:
                from .elements.loaded_bar_element import LoadedBarElement
                element = LoadedBarElement(elem_id, node_ids, material_id, section_id, angle,
                    releases=elem_data.get('releases', []),
                    foundation=elem_data.get('foundation', [0.,0.,0.,0.]), shear_correction=shear_correction)
                                     
            # 材料とパラメータを設定
            bar_param = self._get_bar_parameter(section_id, material_id)
            element.set_material_properties(self.material, bar_param)
            
        elif elem_type == 'shell':
            thickness = elem_data.get('thickness', 0.01)
            
            # ✅ V0技術移植: TriElement1とQuadElement1の自動判定
            original_type = elem_data['type']  # 元の要素タイプ名を保持
            
            if original_type == 'TriElement1':
                # 三角形要素として明示的に作成
                if len(node_ids) != 3:
                    raise ValueError(f"TriElement1 must have exactly 3 nodes, got {len(node_ids)} nodes (element {elem_id})")
                element = ShellElement(elem_id, node_ids, material_id, thickness)
                logger.debug(f"V0互換: TriElement1作成 (要素ID: {elem_id}, 節点: {node_ids})")
                
            elif original_type == 'QuadElement1' or original_type == 'ShellElement':
                # 四角形要素として作成
                if len(node_ids) != 4:
                    # 3節点の場合は三角形要素として処理
                    if len(node_ids) == 3:
                        logger.debug(f"V0互換: QuadElement1が3節点のため三角形として処理 (要素ID: {elem_id})")
                    else:
                        raise ValueError(f"QuadElement1 must have 3 or 4 nodes, got {len(node_ids)} nodes (element {elem_id})")
                element = ShellElement(elem_id, node_ids, material_id, thickness)
                logger.debug(f"V0互換: {original_type}作成 (要素ID: {elem_id}, 節点: {node_ids})")
                
            else:
                # 汎用Shell要素として作成（節点数による自動判定）
                element = ShellElement(elem_id, node_ids, material_id, thickness)
                
            # 材料とパラメータを設定
            formulation = elem_data.get('formulation', 'dkt' if original_type == 'TriElement1' else 'mindlin')
            if formulation not in ('mindlin', 'dkt') or (formulation == 'dkt' and len(node_ids) != 3):
                raise ValueError('Shell formulation must be mindlin, or dkt for a triangle')
            element.formulation = formulation
            shell_param = ShellParameter(thickness=thickness, material_id=material_id)
            element.set_material_properties(self.material, shell_param)
            
        elif elem_type in ['tetra', 'tet', 'hexa', 'hex']:
            element = SolidElement.create_element(elem_type, elem_id, 
                                                node_ids, material_id)
            element.set_material_properties(self.material)

        elif elem_type in ('tetra2', 'wedge2', 'hexa2'):
            from .elements.quadratic_solid import QuadraticSolidElement
            element = QuadraticSolidElement(elem_id, node_ids, material_id, elem_type)
            element.set_material_properties(self.material)
            
        elif AdvancedElement.is_advanced_element(elem_type):
            element = AdvancedElement.create_element(elem_type, elem_id,
                                                   node_ids, material_id)
            if hasattr(element, 'set_material_properties'):
                element.set_material_properties(self.material)
            else:
                # Legacy pyramid/hexa20 stubs lack the setter. Keep them
                # constructible so capability preflight can report the actual
                # unsupported analysis with the element ID.
                element.material = self.material

        elif elem_type == 'nonlinear_bar':
            # 非線形梁要素の作成
            section_id = elem_data.get('section_id', 1)
            angle = elem_data.get('angle', 0)
            shear_correction = elem_data.get('shear_correction', True)
            hysteresis_dofs = elem_data.get('hysteresis_dofs', [])

            element = NonlinearBarElement(
                elem_id, node_ids, material_id,
                section_id, angle, shear_correction
            )

            # 材料とパラメータを設定
            bar_param = self._get_bar_parameter(section_id, material_id)
            element.set_material_properties(self.material, bar_param)

            # 非線形材料から履歴パラメータを設定
            nl_mat = self.material.get_nonlinear_material(material_id)
            laws = self.material.get_nonlinear_laws(material_id)
            if not hysteresis_dofs and laws:
                hysteresis_dofs = list(laws)
                elem_data['hysteresis_dofs'] = hysteresis_dofs
            if (nl_mat is None and not laws) or not hysteresis_dofs:
                raise ValueError('nonlinear_bar requires a nonlinear material and hysteresis_dofs')
            if laws:
                if set(hysteresis_dofs) != set(laws):
                    raise ValueError('hysteresis_dofs must exactly match component law keys')
                for dof in hysteresis_dofs:
                    law = laws[dof]
                    if isinstance(law, AxialForceTable):
                        element.set_axial_force_table(dof, law)
                    else:
                        element.set_hysteresis_model(dof, law)
            if nl_mat is not None and hysteresis_dofs:
                if laws:
                    raise ValueError('Fixed and component-specific laws cannot be mixed')
                params = JRStiffnessReductionParams(
                    delta_1_pos=nl_mat.delta_1_pos,
                    delta_2_pos=nl_mat.delta_2_pos,
                    delta_3_pos=nl_mat.delta_3_pos,
                    P_1_pos=nl_mat.P_1_pos,
                    P_2_pos=nl_mat.P_2_pos,
                    P_3_pos=nl_mat.P_3_pos,
                    delta_4_pos=nl_mat.delta_4_pos,
                    P_4_pos=nl_mat.P_4_pos,
                    delta_1_neg=nl_mat.delta_1_neg,
                    delta_2_neg=nl_mat.delta_2_neg,
                    delta_3_neg=nl_mat.delta_3_neg,
                    P_1_neg=nl_mat.P_1_neg,
                    P_2_neg=nl_mat.P_2_neg,
                    P_3_neg=nl_mat.P_3_neg,
                    delta_4_neg=nl_mat.delta_4_neg,
                    P_4_neg=nl_mat.P_4_neg,
                    beta=nl_mat.beta,
                    K_min=nl_mat.K_min
                )

                for dof in hysteresis_dofs:
                    element.set_hysteresis_model(dof, params)

        else:
            raise ValueError(f"Unknown element type: {elem_type} (original: {elem_data.get('type', 'N/A')})")
            
        element.set_node_coordinates(self.mesh.nodes)
        if isinstance(element, NonlinearBarElement):
            if elem_data.get('releases') or any(elem_data.get('foundation', [])) or elem_data.get('temperature'):
                raise ValueError('Member releases, foundation and temperature loads require a linear beam')
            for load in elem_data.get('line_loads', []):
                element.set_line_load(load['direction'], load['values'], row_kind(load))
        elif hasattr(element, 'set_line_load'):
            element.transfer_load = np.asarray(elem_data.get('transfer_load', np.zeros(12)), dtype=float)
            for load in elem_data.get('line_loads', []):
                element.set_line_load(load['direction'], load['values'], row_kind(load))
            element.temperature_strain = (self.material.materials[material_id].alpha or 0.) * elem_data.get('temperature', 0.)
        elif elem_data.get('releases') or any(elem_data.get('foundation', [])) or elem_data.get('line_loads') or elem_data.get('temperature'):
            raise ValueError('Member releases, foundation and distributed loads require a Bernoulli beam')
        self.elements[elem_id] = element
        
    def _get_bar_parameter(self, section_id: int, material_id: int) -> BarParameter:
        """断面からbar要素パラメータを取得または作成"""
        # 既存のパラメータを確認
        bar_param = self.material.get_bar_parameter(section_id)
        if bar_param:
            return bar_param
            
        # 断面から新規作成
        section_data = self.section.create_bar_parameter(section_id, material_id)
        bar_param = BarParameter(**section_data)
        self.material.add_bar_parameter(section_id, bar_param)
        
        return bar_param
        
    def _set_element_coordinates(self) -> None:
        """要素に節点座標を設定"""
        node_coords = {node_id: coords for node_id, coords in self.mesh.nodes.items()}
        
        for element in self.elements.values():
            element.set_node_coordinates(node_coords)
            
    def _post_process_results(self) -> None:
        """Recover complete physical output for every accepted solver state."""
        from .elements.loaded_bar_element import LoadedBarElement
        solver_snapshot = self._solver_snapshot
        if solver_snapshot is None:
            return
        if 'displacement' not in solver_snapshot:
            return

        analysis_type = solver_snapshot.get('analysis_type')
        snapshots = solver_snapshot.get('step_results', [solver_snapshot])
        for snapshot in snapshots:
            self._post_process_snapshot(snapshot, analysis_type, LoadedBarElement)

        if 'step_results' in solver_snapshot:
            last = snapshots[-1]
            for name in ('element_stresses', 'shell_results', 'legacy_shell_results'):
                if name in last:
                    solver_snapshot[name] = deepcopy(last[name])

        if analysis_type in ('static', 'material_nonlinear'):
            layout = self.solver.layout
            self._recover_beam_end_forces(layout.stride, layout.node_offsets)

    def _post_process_snapshot(self, snapshot, analysis_type, loaded_bar_type):
        """Recover bar, shell, and solid values for one immutable displacement."""
        from .elements.shell_postprocess import legacy_shell_view

        displacement = np.asarray(snapshot['displacement'])
        layout = self.solver.layout
        existing_stresses = snapshot.get('element_stresses', {})
        element_stresses = {}
        shell_results = {}
        legacy_shell_results = {}
        for elem_id, element in self.elements.items():
            indices = layout.element_dofs(elem_id, element)
            elem_disp = displacement[indices]
            if hasattr(element, 'calculate_forces'):
                if analysis_type == 'material_nonlinear':
                    if elem_id not in existing_stresses:
                        raise ValueError(
                            f'Accepted nonlinear snapshot is missing beam element {elem_id}'
                        )
                    element_stresses[elem_id] = deepcopy(existing_stresses[elem_id])
                elif 'precise_end_forces' in snapshot:
                    element_stresses[elem_id] = deepcopy(snapshot['precise_end_forces'][elem_id])
                elif analysis_type == 'static' and isinstance(element, NonlinearBarElement):
                    # Explicit static analysis uses the reference elastic law.
                    element_stresses[elem_id] = TBarElement.calculate_forces(
                        element, np.array(elem_disp)
                    )
                elif ('displacement_correction' in snapshot and
                        isinstance(element, loaded_bar_type)):
                    element_stresses[elem_id] = element.calculate_forces(
                        np.array(elem_disp),
                        displacement_correction=snapshot['displacement_correction'][indices],
                    )
                else:
                    element_stresses[elem_id] = element.calculate_forces(
                        np.array(elem_disp)
                    )
                continue
            element_stresses[elem_id] = element.calculate_stress_strain(
                np.array(elem_disp)
            )
            if isinstance(element, ShellElement):
                shell_results[elem_id] = element.calculate_shell_results(
                    np.array(elem_disp)
                )
                legacy_shell_results[len(legacy_shell_results)] = legacy_shell_view(
                    element, np.array(elem_disp)
                )
        snapshot['element_stresses'] = element_stresses
        if shell_results:
            snapshot['shell_results'] = shell_results
            snapshot['legacy_shell_results'] = legacy_shell_results

    def _recover_beam_end_forces(self, stride, node_offsets):
        """Keep constitutive snapshots and expose equilibrium-recovered branches."""
        from .beam_equilibrium import recover_free_branches
        if stride != 6:
            return
        solver_snapshot = self._solver_snapshot
        if solver_snapshot is None:
            return
        solver = self.solver
        total = solver.load_vector
        resolved = solver._resolve_boundary_dofs(self.boundary, len(total), stride)
        blocked_dofs = resolved.prescribed.keys() | resolved.springs.keys() | resolved.nonlinear_springs.keys()
        blocked = {n: {i for i in range(stride) if start+i in blocked_dofs}
                   for n, start in node_offsets.items()}
        tolerance = self.analysis_params.get('tolerance', 1e-6) if solver_snapshot['analysis_type'] == 'material_nonlinear' else 1e-8
        snapshots = solver_snapshot.get('step_results', [solver_snapshot])
        for snapshot in snapshots:
            factor = snapshot.get('lambda', 1.)
            applied = solver.load_pattern.at(factor)
            loads = {n: applied[start:start+stride] for n, start in node_offsets.items()}
            raw = snapshot['element_stresses']
            if 'precise_end_forces' in snapshot:
                # These actions already satisfy the reassembled 70-digit
                # equations. Float load peeling can destroy tiny thermal or
                # short-segment forces that this solve deliberately retained.
                snapshot['constitutive_element_stresses'] = deepcopy(raw)
                continue
            forces, ids = recover_free_branches(self.mesh.nodes, self.elements, raw,
                                                loads, blocked, factor, tolerance)
            if not ids:
                continue
            snapshot['constitutive_element_stresses'] = raw
            snapshot['element_stresses'] = forces
            snapshot['force_recovery'] = dict(method='free_branch_equilibrium', elements=ids)
            # Apply exactly the same correction to supported-node reactions.
            reactions = snapshot['reaction_forces']
            for key in ids:
                element = self.elements[key]
                change = np.r_[forces[key]['i_end']-raw[key]['i_end'],
                               forces[key]['j_end']-raw[key]['j_end']]
                change = element.get_transformation_matrix(12).T@change
                for end, node in enumerate(element.node_ids):
                    for i, name in enumerate(('fx', 'fy', 'fz', 'mx', 'my', 'mz')):
                        if name in reactions.get(node, {}):
                            reactions[node][name] += change[6*end+i]
        if 'step_results' in solver_snapshot:
            last = snapshots[-1]
            for name in ('element_stresses', 'constitutive_element_stresses', 'force_recovery', 'reaction_forces'):
                if name in last:
                    solver_snapshot[name] = deepcopy(last[name])
            
    def get_model_info(self) -> Dict[str, Any]:
        """モデル情報を取得
        
        Returns:
            モデル情報の辞書
        """
        info = {
            'name': self.name,
            'description': self.description,
            'n_nodes': len(self.mesh.nodes),
            'n_elements': len(self.mesh.elements),
            'n_materials': len(self.material.materials),
            'n_restraints': len(self.boundary.restraints),
            'n_loads': len(self.boundary.loads),
            'element_types': self._get_element_type_summary()
        }
        
        return info
        
    def _get_element_type_summary(self) -> Dict[str, int]:
        """要素タイプごとの個数を集計"""
        summary = {}
        
        for elem_data in self.mesh.elements.values():
            elem_type = elem_data['type']
            summary[elem_type] = summary.get(elem_type, 0) + 1
            
        return summary
        
    def run_static_analysis(self) -> Dict[str, Any]:
        """静的解析を実行（run メソッドのエイリアス）
        
        Returns:
            解析結果
        """
        return self.run('static')
        
    def run_modal_analysis(self, n_modes: int = 10) -> Dict[str, Any]:
        """モード解析を実行（run メソッドのエイリアス）
        
        Args:
            n_modes: 求める固有モード数
            
        Returns:
            解析結果
        """
        # n_modes パラメータを一時的に設定
        missing = object()
        original_n_modes = self.analysis_params.get('n_modes', missing)
        self.analysis_params['n_modes'] = n_modes
        
        try:
            result = self.run('modal')
        finally:
            if original_n_modes is missing:
                self.analysis_params.pop('n_modes', None)
            else:
                self.analysis_params['n_modes'] = original_n_modes
            
        return result
        
    def add_notice_points(self, notice_points: List[Dict[str, Any]]) -> None:
        """着目点で梁要素を分割する
        
        Args:
            notice_points: 着目点データのリスト
        """
        for notice_point in notice_points:
            # 部材番号の取得
            if 'm' not in notice_point:
                continue
                
            member_id = int(notice_point['m'])
            points = notice_point.get('Points', [])
            
            if not points:
                continue
                
            # 該当する要素を探す
            target_elements = []
            for elem_id, elem_data in self.mesh.elements.items():
                # JSONのmemberプロパティまたは要素IDから部材を特定
                # 旧形式では要素IDが部材IDと対応している場合が多い
                if (elem_data.get('member_id') == member_id or 
                    elem_id == member_id):
                    target_elements.append(elem_id)
            
            # 分割対象の要素を処理
            for elem_id in target_elements:
                self._divide_element_by_points(elem_id, points)
                
    def _divide_element_by_points(self, elem_id: int, points: List[float]) -> None:
        """指定された点で要素を分割する
        
        Args:
            elem_id: 要素ID
            points: 分割点のリスト（要素のi端からの距離）
        """
        if elem_id not in self.mesh.elements:
            logger.warning(f"分割対象の要素{elem_id}が見つかりません")
            return
            
        elem_data = self.mesh.elements[elem_id]
        
        # bar要素のみを対象とする
        if elem_data['type'] != 'bar':
            logger.warning(f"要素{elem_id}はbar要素ではないため分割をスキップします")
            return
            
        node_ids = elem_data['nodes']
        if len(node_ids) != 2:
            logger.warning(f"要素{elem_id}のノード数が2ではないため分割をスキップします")
            return
            
        # 要素の両端節点を取得
        node_i = self.mesh.nodes[node_ids[0]]
        node_j = self.mesh.nodes[node_ids[1]]
        
        # 要素の長さを計算
        dx = node_j[0] - node_i[0]
        dy = node_j[1] - node_i[1]
        dz = node_j[2] - node_i[2]
        element_length = math.sqrt(dx*dx + dy*dy + dz*dz)
        
        # pointsをソートして重複を除去（浮動小数点精度問題対応）
        # 許容誤差を使用した重複除去と境界値チェック
        tolerance = element_length * 1e-9  # 要素長の10億分の1を許容誤差とする
        boundary_tolerance = element_length * 1e-6  # 境界値判定用の許容誤差（より大きめ）
        
        # V1レベルの厳密な境界値チェック
        valid_points = []
        for p in points:
            # 境界値の厳密チェック：開始点・終点近くを除外
            if p <= boundary_tolerance:
                logger.debug(f"分割点{p:.6f}は開始点に近すぎるため除外（許容誤差: {boundary_tolerance:.6f}）")
                continue
            if p >= element_length - boundary_tolerance:
                logger.debug(f"分割点{p:.6f}は終点に近すぎるため除外（要素長: {element_length:.6f}, 許容誤差: {boundary_tolerance:.6f}）")
                continue
            valid_points.append(p)
        
        # 許容誤差による重複除去
        sorted_points = []
        for point in sorted(valid_points):
            # 既存の分割位置と近すぎる場合は追加しない
            too_close = False
            for existing_point in sorted_points:
                if abs(point - existing_point) < tolerance:
                    too_close = True
                    logger.debug(f"分割点{point:.6f}は既存点{existing_point:.6f}に近すぎるため除外（許容誤差: {tolerance:.6f}）")
                    break
            if not too_close:
                sorted_points.append(point)
        
        if not sorted_points:
            logger.debug(f"要素{elem_id}: 有効な分割位置がないため分割をスキップします")
            return
        
        # 新しい節点IDを生成（既存の最大ID + 1から開始）
        max_node_id = max(self.mesh.nodes.keys()) if self.mesh.nodes else 0
        max_elem_id = max(self.mesh.elements.keys()) if self.mesh.elements else 0
        
        # 分割点に新しい節点を作成（旧実装同様に既存節点との重複チェックを実装）
        new_node_ids = []
        for i, point in enumerate(sorted_points):
            # 分割点の座標を計算
            ratio = point / element_length
            new_x = node_i[0] + dx * ratio
            new_y = node_i[1] + dy * ratio
            new_z = node_i[2] + dz * ratio
            
            # 既存の節点で同じ位置のものがないかチェック
            existing_node_id = self._find_node_by_coordinates(new_x, new_y, new_z)
            
            if existing_node_id is not None:
                # 既存の節点があればそれを使用
                new_node_ids.append(existing_node_id)
                logger.debug(f"分割点{i+1}: 既存節点{existing_node_id}を使用")
            else:
                # 新しい節点を追加
                new_node_id = max_node_id + 1
                self.mesh.add_node(new_node_id, [new_x, new_y, new_z])
                new_node_ids.append(new_node_id)
                max_node_id = new_node_id
                logger.debug(f"分割点{i+1}: 新規節点{new_node_id}を作成")
        
        # 元の要素を削除
        original_elem_data = self.mesh.elements.pop(elem_id)
        
        # 新しい要素群を作成
        all_node_ids = [node_ids[0]] + new_node_ids + [node_ids[1]]
        
        # V1レベルの安全性チェック：隣接節点の重複確認
        for i in range(len(all_node_ids) - 1):
            if all_node_ids[i] == all_node_ids[i + 1]:
                logger.warning(f"隣接節点が重複しています（節点{all_node_ids[i]}）。要素分割をキャンセルします")
                # 元の要素を復元
                self.mesh.elements[elem_id] = original_elem_data
                return
        
        # デバッグ出力
        logger.debug(f"要素分割: 要素{elem_id}を{len(sorted_points)}個の点で分割、新規節点{len([n for n in new_node_ids if n > max(self.mesh.nodes.keys()) - len(new_node_ids)])}個を追加")
        logger.debug(f"元の要素: 節点{node_ids[0]}→節点{node_ids[1]}, 長さ={element_length:.4f}")
        for i, point in enumerate(sorted_points):
            logger.debug(f"分割点{i+1}: i端から{point:.4f}, 節点ID={new_node_ids[i]}")
        
        created_elements = []
        for i in range(len(all_node_ids) - 1):
            new_elem_id = max_elem_id + i + 1
            
            # 元の要素データをコピーし、節点情報を更新
            new_elem_data = {
                'type': original_elem_data['type'],
                'nodes': [all_node_ids[i], all_node_ids[i + 1]],
                'material_id': original_elem_data.get('material_id', 1),
                'section_id': original_elem_data.get('section_id', 1),  # section_idを明示的に継承
                'angle': original_elem_data.get('angle', 0),  # angleも継承
                'shear_correction': original_elem_data.get('shear_correction', True),  # shear_correctionも継承
                'original_id': elem_id  # 元の要素IDを記録（分割された要素の追跡用）
            }
            
            # その他の属性もコピー（ただし既に設定した属性は除外）
            excluded_keys = {'type', 'nodes', 'material_id', 'section_id', 'angle', 'shear_correction'}
            for k, v in original_elem_data.items():
                if k not in excluded_keys:
                    new_elem_data[k] = v
            
            # 新しい要素を追加
            self.mesh.elements[new_elem_id] = new_elem_data
            created_elements.append(new_elem_id)
            max_elem_id = new_elem_id
            
        logger.debug(f"作成された要素: {created_elements}")
        logger.debug("要素分割詳細")
        for elem_id in created_elements:
            elem_data = self.mesh.elements[elem_id]
            logger.debug(f"要素{elem_id}: 節点{elem_data['nodes']}, material_id={elem_data['material_id']}, section_id={elem_data['section_id']}")
    
    def _find_node_by_coordinates(self, x: float, y: float, z: float, 
                                 tolerance: float = 1e-6) -> Optional[int]:
        """座標から既存の節点を検索する（旧実装のfind_nodeIndex相当）
        
        Args:
            x, y, z: 検索する座標
            tolerance: 座標の許容誤差（旧実装では完全一致なので使用しない）
            
        Returns:
            見つかった節点ID（見つからない場合はNone）
        """
        # 旧実装と完全一致させるため、完全一致（==）で比較
        for node_id, coords in self.mesh.nodes.items():
            if (coords[0] == x and coords[1] == y and coords[2] == z):
                return node_id
        return None
        
    def _divide_element_by_distributed_loads(self, distributed_loads: List[Dict[str, Any]]) -> None:
        """分布荷重の作用位置で要素を分割する
        
        Args:
            distributed_loads: 分布荷重データのリスト
        """
        logger.debug(f"分布荷重分割: {len(distributed_loads)}個の分布荷重を処理開始")
        
        # 要素ごとに分割位置をまとめる
        element_split_positions = {}
        
        logger.debug(f"対象bar要素数: {len([e for e in self.mesh.elements.values() if e['type'] == 'bar'])}個")
        
        # 分布荷重の両端位置を処理（旧実装に完全一致）
        processed_loads = 0
        for i, load in enumerate(distributed_loads):  # 全てをデバッグ対象に変更
            element_id = load.get('element_id')
            dist1 = load.get('start_position', 0.0)  # L1
            dist2 = load.get('end_position', 0.0)    # L2
            
            # 重要な分布荷重（部材14、15、9、12でL2が正値）を詳細デバッグ
            is_important = (element_id in [14, 15, 9, 12] and 
                          (dist1 > 0 or dist2 > 0) and 
                          not (dist1 == 0.0 and dist2 == 0.0))
            
            if i < 10 or is_important:  # 最初の10個または重要なケースを詳細表示
                logger.debug(f"分布荷重{i+1}: 要素{element_id}, L1={dist1}, L2={dist2}")
                if is_important:
                    logger.debug("重要な分布荷重を検出")
            
            if element_id is None:
                if i < 10 or is_important:
                    logger.debug("スキップ: element_idが無効")
                continue
                
            # 元の要素IDに対応する現在の要素を探す（集中荷重処理と同じロジック）
            target_elements = []
            for current_id, elem_data in self.mesh.elements.items():
                if elem_data['type'] == 'bar' and \
                   (current_id == element_id or 
                    elem_data.get('original_id') == element_id or
                    elem_data.get('member_id') == element_id):
                    target_elements.append(current_id)
            
            if i < 10 or is_important:
                logger.debug(f"対応する現在の要素: {target_elements}")
            
            if not target_elements:
                if i < 10 or is_important:
                    logger.debug("スキップ: 対応する要素が見つからない")
                continue
            
            # 各現在の要素を処理
            for current_id in target_elements:
                elem_data = self.mesh.elements[current_id]
                node_ids = elem_data['nodes']
                if len(node_ids) != 2:
                    continue
                    
                # 要素の長さを計算
                node_i = self.mesh.nodes[node_ids[0]]
                node_j = self.mesh.nodes[node_ids[1]]
                dx = node_j[0] - node_i[0]
                dy = node_j[1] - node_i[1]
                dz = node_j[2] - node_i[2]
                element_length = math.sqrt(dx*dx + dy*dy + dz*dz)
                
                # 旧実装と同じ処理: L2が負値の場合は荷重幅を表すのでj端からの距離に変換
                processed_dist2 = dist2
                if dist2 < 0:
                    processed_dist2 = element_length - dist1 - abs(dist2)
                    if i < 10 or is_important:
                        logger.debug(f"L2負値変換: {dist2} → {processed_dist2}")
                
                # 旧実装と同じ分割条件
                condition_met = (dist1 >= 0) and (processed_dist2 >= 0) and (dist1 + processed_dist2 < element_length)
                
                if i < 10 or is_important:
                    logger.debug(f"要素{current_id} (長さ{element_length:.4f}): 分割条件={condition_met}")
                    logger.debug(f"詳細: dist1={dist1}, processed_dist2={processed_dist2}, dist1+processed_dist2={dist1+processed_dist2}")
                
                if condition_met:
                    # 分割位置のリストに追加
                    if current_id not in element_split_positions:
                        element_split_positions[current_id] = set()
                    
                    # 旧実装と同じ分割位置
                    if dist1 > 0:
                        element_split_positions[current_id].add(dist1)
                        if i < 10 or is_important:
                            logger.debug(f"要素{current_id}: L1={dist1}を分割位置に追加")
                        
                    if processed_dist2 > 0:
                        # 旧実装: memTmp.leng - dist2 位置で分割
                        split_pos = element_length - processed_dist2
                        element_split_positions[current_id].add(split_pos)
                        if i < 10 or is_important:
                            logger.debug(f"要素{current_id}: L2変換位置={split_pos}（要素長{element_length} - dist2({processed_dist2})）を分割位置に追加")
                else:
                    if i < 10 or is_important:
                        logger.debug(f"要素{current_id}: 分割条件不適合")
                
            processed_loads += 1
        
        if len(distributed_loads) > 10:
            logger.debug(f"ほか {len(distributed_loads) - processed_loads}個の分布荷重も処理済み")
        
        logger.debug(f"分割対象要素数: {len(element_split_positions)}個")
        for elem_id, positions in list(element_split_positions.items())[:3]:
            logger.debug(f"要素{elem_id}: {sorted(list(positions))}で分割予定")
        
        # 各要素を一度に分割
        for element_id, positions in element_split_positions.items():
            positions_list = sorted(list(positions))
            if positions_list:
                logger.debug(f"分布荷重による分割: 要素{element_id}を{len(positions_list)}箇所で分割 - {positions_list}")
                self._divide_element_by_points(element_id, positions_list)
            else:
                logger.debug(f"要素{element_id}: 有効な分割位置がないため分割スキップ")

    def _divide_element_by_concentrated_loads(self, concentrated_loads: List[Dict[str, Any]]) -> None:
        """集中荷重の作用位置で要素を分割する
        
        Args:
            concentrated_loads: 集中荷重データのリスト
        """
        logger.debug(f"集中荷重分割: {len(concentrated_loads)}個の集中荷重を処理開始")
        
        # 各要素を1回だけ処理するため、要素IDと分割位置のマップを作成
        element_split_positions = {}
        
        for i, load in enumerate(concentrated_loads[:5]):  # 最初の5個のみ詳細表示
            # 要素IDの取得
            if 'element_id' not in load:
                logger.debug(f"集中荷重{i+1}: element_idキーが無い - スキップ")
                continue
                
            element_id = load['element_id']
            pos = load.get('position', 0.0)
            
            logger.debug(f"集中荷重{i+1}: 要素{element_id}, 位置={pos}")
            
            # 同じ要素の分割位置をまとめる
            if element_id not in element_split_positions:
                element_split_positions[element_id] = []
            element_split_positions[element_id].append(pos)
            logger.debug(f"位置{pos}を要素{element_id}の分割リストに追加")
        
        if len(concentrated_loads) > 5:
            logger.debug(f"ほか {len(concentrated_loads) - 5}個の集中荷重も同様に処理")
        
        logger.debug(f"分割対象要素数: {len(element_split_positions)}個")
        for elem_id, positions in list(element_split_positions.items())[:3]:
            logger.debug(f"要素{elem_id}: {positions}で分割予定")
        
        # 要素ごとに一度に分割
        for element_id, positions in element_split_positions.items():
            # 元の要素IDに対応する現在の要素を探す
            target_elements = []
            for current_id, elem_data in self.mesh.elements.items():
                if elem_data['type'] == 'bar' and \
                   (current_id == element_id or 
                    elem_data.get('original_id') == element_id or
                    elem_data.get('member_id') == element_id):
                    target_elements.append(current_id)
            
            logger.debug(f"要素{element_id}に対応する対象要素: {target_elements}")
            
            if not target_elements:
                logger.warning(f"集中荷重の対象部材{element_id}が見つかりません")
                continue
                
            # 各要素を処理
            for current_id in target_elements:
                # 要素が存在しない場合（既に分割されて削除された可能性）
                if current_id not in self.mesh.elements:
                    logger.debug(f"要素{current_id}: 既に削除済み - スキップ")
                    continue
                    
                elem_data = self.mesh.elements[current_id]
                if elem_data['type'] != 'bar':
                    logger.debug(f"要素{current_id}: bar要素ではない - スキップ")
                    continue
                    
                node_ids = elem_data['nodes']
                if len(node_ids) != 2:
                    logger.debug(f"要素{current_id}: ノード数が2ではない - スキップ")
                    continue
                    
                # 要素の長さを計算
                node_i = self.mesh.nodes[node_ids[0]]
                node_j = self.mesh.nodes[node_ids[1]]
                dx = node_j[0] - node_i[0]
                dy = node_j[1] - node_i[1]
                dz = node_j[2] - node_i[2]
                element_length = math.sqrt(dx*dx + dy*dy + dz*dz)
                
                # この要素内にある分割位置を抽出
                valid_positions = [p for p in positions if 0 < p < element_length]
                
                logger.debug(f"要素{current_id} (長さ{element_length:.4f}): 位置{positions} → 有効位置{valid_positions}")
                
                if valid_positions:
                    logger.debug(f"集中荷重による分割: 要素{current_id}を{len(valid_positions)}箇所で分割")
                    self._divide_element_by_points(current_id, valid_positions)
                else:
                    logger.debug(f"要素{current_id}: 有効な分割位置がないため分割スキップ")

    def _extract_all_loads(self, load_data: Dict[str, Any]) -> Dict[str, List]:
        """すべての荷重ケースから荷重データを抽出
        
        Args:
            load_data: モデルデータのload部分
            
        Returns:
            抽出された荷重データの辞書
        """
        distributed_loads = []
        concentrated_loads = []
        
        logger.debug(f"荷重データの抽出開始: {len(load_data)}個の荷重ケース")
        
        # 全荷重ケースをループ
        for case_id, case_data in load_data.items():
            if 'load_member' not in case_data:
                continue
                
            logger.debug(f"荷重ケース{case_id}を処理中")
            case_distributed = []
            case_concentrated = []
            
            # このケースの要素荷重をループ
            for load_data in case_data['load_member']:
                if 'm' not in load_data:
                    continue
                    
                element_id = int(load_data['m'])
                
                # mark=2は分布荷重
                if load_data.get('mark') == 2:
                    start_pos = float(load_data.get('L1', 0.0))
                    end_pos = float(load_data.get('L2', 0.0))
                    
                    # L2が負の場合は荷重幅を表す
                    # 荷重の幅がわかるのは後で要素長を計算してからなので、
                    # 現時点では変換せずに負値のまま保持
                    
                    case_distributed.append({
                        'element_id': element_id,
                        'start_position': start_pos,
                        'end_position': end_pos,
                        'case_id': case_id
                    })
                    
                # mark=1,11は集中荷重
                elif load_data.get('mark') in [1, 11]:
                    # 旧実装のdevide_byElemLoadsと同様に、位置情報のみで分割判定（P値は無視）
                    debug_count = 0
                    for key in ['L1', 'L2']:
                        if key not in load_data or not load_data[key]:
                            continue
                            
                        position = float(load_data[key])
                        
                        # 詳細なデバッグ出力（最初の10個のみ）
                        if debug_count < 10:
                            logger.debug(f"集中荷重分割 要素{element_id}, {key}={position}")
                            debug_count += 1
                        
                        if position <= 0:
                            if debug_count <= 10:
                                logger.debug(f"位置={position}≤0のため除外")
                            continue
                            
                        case_concentrated.append({
                            'element_id': element_id,
                            'position': position,
                            'case_id': case_id
                        })
                        
                        if debug_count <= 10:
                            logger.debug(f"追加: 要素{element_id}, 位置={position}")
                    
                    # 最初の10個のみ詳細表示
                    if len(case_concentrated) > 10:
                        break
            
            if case_distributed:
                logger.debug(f"分布荷重: {len(case_distributed)}個を抽出")
                distributed_loads.extend(case_distributed)
                
            if case_concentrated:
                logger.debug(f"集中荷重: {len(case_concentrated)}個を抽出")
                concentrated_loads.extend(case_concentrated)
        
        logger.debug(f"荷重データの抽出完了: 分布荷重{len(distributed_loads)}個, 集中荷重{len(concentrated_loads)}個")
        
        return {
            'distributed': distributed_loads,
            'concentrated': concentrated_loads
        }
