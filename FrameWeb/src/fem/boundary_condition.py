"""
境界条件を管理するモジュール
JavaScript版のBoundaryCondition機能に対応
"""
from typing import Dict, List, Optional, Tuple
import numpy as np
from enum import Enum
from .load_kinds import INCREMENTAL, LoadKind
from .spatial_loads import SpatialLoadDefinitions


class BoundaryType(Enum):
    """境界条件のタイプ"""
    DISPLACEMENT = "displacement"  # 変位拘束
    FORCE = "force"  # 節点荷重
    DISTRIBUTED_LOAD = "distributed"  # 分布荷重
    TEMPERATURE = "temperature"  # 温度境界条件
    HEAT_TRANSFER = "heat_transfer"  # 熱伝達境界条件


class Restraint:
    """拘束条件クラス"""
    
    def __init__(self, node_id: int, dof_restraints: List[bool], 
                 values: Optional[List[float]] = None):
        """
        Args:
            node_id: ノードID
            dof_restraints: 各自由度の拘束フラグ [x, y, z, rx, ry, rz]
            values: 強制変位値（省略時は0）
        """
        self.node_id = node_id
        self.dof_restraints = dof_restraints
        self.values = values or [0.0] * len(dof_restraints)
        
    def is_restrained(self, dof: int) -> bool:
        """指定された自由度が拘束されているか"""
        return self.dof_restraints[dof] if dof < len(self.dof_restraints) else False
        
    def get_value(self, dof: int) -> float:
        """指定された自由度の強制変位値を取得"""
        return self.values[dof] if dof < len(self.values) else 0.0


class Load:
    """荷重条件クラス"""
    
    def __init__(self, node_id: int, forces: List[float]):
        """
        Args:
            node_id: ノードID
            forces: 荷重値 [Fx, Fy, Fz, Mx, My, Mz]
        """
        self.node_id = node_id
        self.forces = np.array(forces, dtype=np.float64)
        
    def get_force(self, dof: int) -> float:
        """指定された自由度の荷重値を取得"""
        return self.forces[dof] if dof < len(self.forces) else 0.0


class DistributedLoad:
    """分布荷重クラス"""
    
    def __init__(self, element_id: int, load_type: str, values: List[float], 
                 face: Optional[int] = None):
        """
        Args:
            element_id: 要素ID
            load_type: 荷重タイプ ('pressure', 'body_force', etc.)
            values: 荷重値
            face: 面番号（シェル/ソリッド要素の場合）
        """
        self.element_id = element_id
        self.load_type = load_type
        self.values = np.array(values)
        self.face = face


class Temperature:
    """温度境界条件クラス"""
    
    def __init__(self, node_id: int, temperature: float):
        """
        Args:
            node_id: ノードID
            temperature: 温度値
        """
        self.node_id = node_id
        self.temperature = temperature


class HeatTransferBound:
    """熱伝達境界条件クラス"""
    
    def __init__(self, element_id: int, face: int, h: float, t_inf: float):
        """
        Args:
            element_id: 要素ID
            face: 面番号
            h: 熱伝達係数
            t_inf: 外部温度
        """
        self.element_id = element_id
        self.face = face
        self.h = h
        self.t_inf = t_inf


class Pressure:
    """面圧条件クラス（V0のPressureクラスに対応）"""
    
    def __init__(self, element_id: int, face: str, pressure: float):
        """
        Args:
            element_id: 要素ID
            face: 面番号（"F1", "F2"など）
            pressure: 面圧値
        """
        self.element_id = element_id
        self.face = face
        self.pressure = pressure
        
    def get_border(self, element):
        """要素の境界を取得（V0のgetBorderメソッドに対応）"""
        if len(self.face) == 2:
            if self.face[0] == 'F':
                face_index = int(self.face[1]) - 1
                return element.border(self.element_id, face_index)
            elif self.face[0] == 'E':
                edge_index = int(self.face[1]) - 1
                return element.border_edge(self.element_id, edge_index)
        return None
        
    def __str__(self):
        """面圧条件を表す文字列を返す（V0のtoStringメソッドに対応）"""
        return f'Pressure\t{self.element_id}\t{self.face}\t{self.pressure}'


class BoundaryCondition:
    """境界条件を管理するクラス"""
    
    def __init__(self):
        self.restraints: Dict[int, Restraint] = {}
        self.spring_supports: Dict[int, dict] = {}
        self.nonlinear_spring_supports: Dict[int, dict] = {}
        # Nodal loads by kind (fem.load_kinds); ``loads`` is the incremental set.
        self.load_sets: Dict[LoadKind, Dict[int, Load]] = {INCREMENTAL: {}}
        self.distributed_loads: List[DistributedLoad] = []
        self.temperatures: Dict[int, Temperature] = {}
        self.heat_transfers: List[HeatTransferBound] = []
        self.pressures: List[Pressure] = []  # 面圧条件リスト（V0に対応）
        self.spatial_loads = SpatialLoadDefinitions()
        
    def add_restraint(self, node_id: int, dof_restraints: List[bool], 
                     values: Optional[List[float]] = None) -> None:
        """拘束条件を追加"""
        self.restraints[node_id] = Restraint(node_id, dof_restraints, values)
        
    @property
    def loads(self) -> Dict[int, Load]:
        """増分荷重の節点荷重（区別のない荷重はすべてここに入る）"""
        return self.load_sets[INCREMENTAL]

    def add_load(self, node_id: int, forces: List[float], kind: LoadKind = INCREMENTAL) -> None:
        """節点荷重を追加"""
        loads = self.load_sets.setdefault(kind, {})
        if node_id in loads:
            # 既存の荷重に加算（float64型として追加）
            loads[node_id].forces += np.array(forces, dtype=np.float64)
        else:
            loads[node_id] = Load(node_id, forces)
            
    def add_distributed_load(self, element_id: int, load_type: str, 
                           values: List[float], face: Optional[int] = None) -> None:
        """分布荷重を追加"""
        self.distributed_loads.append(
            DistributedLoad(element_id, load_type, values, face)
        )
        
    def add_temperature(self, node_id: int, temperature: float) -> None:
        """温度境界条件を追加"""
        self.temperatures[node_id] = Temperature(node_id, temperature)
        
    def add_temperature_load(self, element_id: int, temperature: float) -> None:
        """要素の温度荷重を追加（要素全体に均一な温度変化）
        
        Args:
            element_id: 要素ID
            temperature: 温度変化量
        """
        # 要素温度荷重は分布荷重の一種として扱う
        self.add_distributed_load(element_id, 'temperature', [temperature], None)
        
    def add_heat_transfer(self, element_id: int, face: int, 
                         h: float, t_inf: float) -> None:
        """熱伝達境界条件を追加"""
        self.heat_transfers.append(
            HeatTransferBound(element_id, face, h, t_inf)
        )
        
    def add_pressure(self, element_id: int, face: str, pressure: float) -> None:
        """面圧条件を追加（V0のPressureクラスに対応）"""
        self.pressures.append(Pressure(element_id, face, pressure))
        
    def get_pressures(self) -> List[Pressure]:
        """面圧条件のリストを取得"""
        return self.pressures
        
    def get_restraint(self, node_id: int) -> Optional[Restraint]:
        """指定ノードの拘束条件を取得"""
        return self.restraints.get(node_id)
        
    def get_load(self, node_id: int) -> Optional[Load]:
        """指定ノードの荷重を取得"""
        return self.loads.get(node_id)
        
    def get_restrained_dofs(self) -> List[Tuple[int, int]]:
        """拘束されている自由度のリストを取得
        
        Returns:
            [(node_id, dof), ...] のリスト
        """
        restrained_dofs = []
        for node_id, restraint in self.restraints.items():
            for dof, is_restrained in enumerate(restraint.dof_restraints):
                if is_restrained:
                    restrained_dofs.append((node_id, dof))
        return restrained_dofs
        
    def clear(self) -> None:
        """すべての境界条件をクリア"""
        self.restraints.clear()
        self.spring_supports.clear()
        self.nonlinear_spring_supports.clear()
        self.load_sets = {INCREMENTAL: {}}
        self.distributed_loads.clear()
        self.temperatures.clear()
        self.heat_transfers.clear()
        self.pressures.clear()
        self.spatial_loads = SpatialLoadDefinitions()
