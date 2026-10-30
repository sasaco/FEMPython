"""
材料物性を管理するモジュール
JavaScript版のMaterial機能に対応
非線形材料（JR総研剛性低減RC型など）もサポート
"""
from typing import Any, Dict, Optional, List
import numpy as np
from dataclasses import dataclass


@dataclass
class MaterialProperty:
    """材料物性の基本クラス"""
    name: str
    E: float  # ヤング率
    nu: float  # ポアソン比
    density: Optional[float] = None  # 密度
    alpha: Optional[float] = None  # 線膨張係数
    k: Optional[float] = None  # 熱伝導率
    c: Optional[float] = None  # 比熱
    shear_modulus: Optional[float] = None  # 明示G（省略時はE,nuから計算）

    def __post_init__(self):
        if self.shear_modulus is not None and (
                not np.isfinite(self.shear_modulus) or self.shear_modulus <= 0):
            raise ValueError('Shear modulus must be finite and positive')
    
    @property
    def G(self) -> float:
        """せん断弾性係数を計算"""
        return self.shear_modulus if self.shear_modulus is not None else self.E / (2 * (1 + self.nu))
        
    @property
    def bulk_modulus(self) -> float:
        """体積弾性率を計算"""
        return self.E / (3 * (1 - 2 * self.nu))


@dataclass
class ShellParameter:
    """シェル要素用パラメータ"""
    thickness: float  # 板厚
    integration_points: int = 5  # 板厚方向の積分点数
    offset: float = 0.0  # オフセット値
    material_id: Optional[int] = None
    
    def get_integration_points(self) -> List[float]:
        """板厚方向の積分点座標を取得"""
        if self.integration_points == 5:
            # Gauss-Lobatto 5点
            points = np.array([-1.0, -0.6546536707, 0.0, 0.6546536707, 1.0])
        elif self.integration_points == 3:
            # Gauss 3点
            points = np.array([-0.7745966692, 0.0, 0.7745966692])
        else:
            # 等間隔
            points = np.linspace(-1.0, 1.0, self.integration_points)
        return points * self.thickness / 2.0


@dataclass
class BarParameter:
    """梁要素用パラメータ"""
    area: float  # 断面積
    Iy: float  # 断面二次モーメント (y軸周り)
    Iz: float  # 断面二次モーメント (z軸周り)
    J: Optional[float] = None  # ねじり定数
    kappa_y: Optional[float] = None  # せん断補正係数 (y方向)
    kappa_z: Optional[float] = None  # せん断補正係数 (z方向)
    offset_y: float = 0.0  # y方向オフセット
    offset_z: float = 0.0  # z方向オフセット
    material_id: Optional[int] = None

    def __post_init__(self):
        """初期化後の処理"""
        # None値をデフォルト値で置き換え
        if self.Iy is None:
            self.Iy = 0.0  # shell要素用のデフォルト値
        if self.Iz is None:
            self.Iz = 0.0  # shell要素用のデフォルト値

        if self.J is None:
            # 円形断面と仮定してねじり定数を推定
            # Iy, Izがゼロの場合（shell要素など）はゼロとする
            self.J = self.Iy + self.Iz if (self.Iy > 0 or self.Iz > 0) else 0.0
        if self.kappa_y is None:
            self.kappa_y = 5.0 / 6.0  # デフォルト値
        if self.kappa_z is None:
            self.kappa_z = 5.0 / 6.0  # デフォルト値


@dataclass
class NonlinearMaterialProperty:
    """非線形材料物性（JR総研剛性低減RC型用）

    4折線スケルトンカーブと剛性低減パラメータを定義

    NonlinearBarElementではδは軸ひずみ（axial）、中央曲率（moment_y/z）、
    ねじり率（torsion）。Pは対応する軸力または断面モーメント。
    曲率・ねじり率はrad/長さ、モーメントは力×長さ。集中ヒンジ回転ではない。
    キーは旧入力と共通だが、絶対端部変位としての旧計算結果は引き継がない。

    Attributes:
        name: 材料名
        E: 初期ヤング率
        nu: ポアソン比
        delta_1_pos〜P_3_pos: 正側の第1〜3骨格点
        delta_4_pos, P_4_pos: 正側の第4勾配を定める参照点（両方省略可）
        delta_1_neg〜P_4_neg: 負側パラメータ（省略時は正側と同じ）
        beta: 剛性低減係数
        K_min: 戻り剛性下限値
        density: 密度
    """
    name: str
    E: float              # 初期ヤング率
    nu: float             # ポアソン比

    # スケルトンカーブパラメータ（正側）
    delta_1_pos: float    # ひび割れ一般化ひずみ（適用先により曲率・ねじり率）
    delta_2_pos: float    # 降伏一般化ひずみ
    delta_3_pos: float    # 終局一般化ひずみ
    P_1_pos: float        # ひび割れ断面力
    P_2_pos: float        # 降伏断面力
    P_3_pos: float        # 終局断面力

    # スケルトンカーブパラメータ（負側、省略時は正側と同じ）
    delta_1_neg: Optional[float] = None
    delta_2_neg: Optional[float] = None
    delta_3_neg: Optional[float] = None
    P_1_neg: Optional[float] = None
    P_2_neg: Optional[float] = None
    P_3_neg: Optional[float] = None
    # 剛性低減パラメータ
    beta: float = 0.4     # 剛性低減係数
    K_min: Optional[float] = None  # 戻り剛性下限値（省略時は自動計算）

    # その他
    density: Optional[float] = None

    # 第4勾配の参照点（既存の位置引数互換のため末尾へ追加）
    delta_4_pos: Optional[float] = None
    P_4_pos: Optional[float] = None
    delta_4_neg: Optional[float] = None
    P_4_neg: Optional[float] = None
    # True when delta_* hold JRSNAP M-theta rotations; each member divides them by its
    # own length (curvature = rotation / L) when its law is built.
    rotation_skeleton: bool = False

    def __post_init__(self):
        """負側パラメータが省略された場合は正側と同じ値を設定"""
        from .nonlinear.hysteresis import JRStiffnessReductionParams

        if not np.isfinite(self.E) or self.E <= 0:
            raise ValueError('E must be finite and positive')
        if not np.isfinite(self.nu) or not -1 < self.nu < .5:
            raise ValueError('nu must be finite and between -1 and 0.5')
        if self.density is not None and (not np.isfinite(self.density) or self.density < 0):
            raise ValueError('density must be finite and nonnegative')
        if self.delta_1_neg is None:
            self.delta_1_neg = self.delta_1_pos
        if self.delta_2_neg is None:
            self.delta_2_neg = self.delta_2_pos
        if self.delta_3_neg is None:
            self.delta_3_neg = self.delta_3_pos
        if self.P_1_neg is None:
            self.P_1_neg = self.P_1_pos
        if self.P_2_neg is None:
            self.P_2_neg = self.P_2_pos
        if self.P_3_neg is None:
            self.P_3_neg = self.P_3_pos
        if self.delta_4_neg is None:
            self.delta_4_neg = self.delta_4_pos
        if self.P_4_neg is None:
            self.P_4_neg = self.P_4_pos
        # Validate before deriving a default floor (including division by δ1).
        params = JRStiffnessReductionParams(**{
            name: getattr(self, name)
            for name in JRStiffnessReductionParams.__dataclass_fields__
        })
        self.K_min = params.K_min

    @property
    def G(self) -> float:
        """せん断弾性係数"""
        return self.E / (2 * (1 + self.nu))


class Material:
    """材料データベースを管理するクラス"""

    def __init__(self):
        self.materials: Dict[int, MaterialProperty] = {}
        self.nonlinear_materials: Dict[int, NonlinearMaterialProperty] = {}  # 非線形材料
        # Immutable component-specific fixed laws and Nd-dependent bending
        # tables; runtime history remains element-owned.
        self.nonlinear_laws: Dict[int, Dict[str, Any]] = {}
        self.shell_params: Dict[int, ShellParameter] = {}
        self.bar_params: Dict[int, BarParameter] = {}
        self._initialize_default_materials()
        
    def _initialize_default_materials(self) -> None:
        """デフォルト材料を初期化"""
        # 鋼材
        self.add_material(1, MaterialProperty(
            name="Steel",
            E=206e9,  # Pa
            nu=0.3,
            density=7850.0,  # kg/m^3
            alpha=1.2e-5,  # 1/K
            k=50.0,  # W/(m·K)
            c=450.0  # J/(kg·K)
        ))
        
        # アルミニウム
        self.add_material(2, MaterialProperty(
            name="Aluminum",
            E=70e9,
            nu=0.33,
            density=2700.0,
            alpha=2.3e-5,
            k=237.0,
            c=900.0
        ))
        
        # コンクリート
        self.add_material(3, MaterialProperty(
            name="Concrete",
            E=30e9,
            nu=0.2,
            density=2400.0,
            alpha=1.0e-5,
            k=1.7,
            c=880.0
        ))
        
    def add_material(self, material_id: int, material: MaterialProperty) -> None:
        """材料を追加"""
        self.materials[material_id] = material
        
    def add_shell_parameter(self, param_id: int, param: ShellParameter) -> None:
        """シェルパラメータを追加"""
        self.shell_params[param_id] = param
        
    def add_bar_parameter(self, param_id: int, param: BarParameter) -> None:
        """梁パラメータを追加"""
        self.bar_params[param_id] = param
        
    def get_material(self, material_id: int) -> Optional[MaterialProperty]:
        """材料を取得"""
        return self.materials.get(material_id)
        
    def get_shell_parameter(self, param_id: int) -> Optional[ShellParameter]:
        """シェルパラメータを取得"""
        return self.shell_params.get(param_id)
        
    def get_bar_parameter(self, param_id: int) -> Optional[BarParameter]:
        """梁パラメータを取得"""
        return self.bar_params.get(param_id)

    def add_nonlinear_material(
        self,
        material_id: int,
        material: NonlinearMaterialProperty
    ) -> None:
        """非線形材料を追加

        Args:
            material_id: 材料ID
            material: 非線形材料プロパティ
        """
        self.nonlinear_materials[material_id] = material

    def add_nonlinear_laws(self, material_id: int, laws: Dict[str, Any]) -> None:
        """Register validated component laws without sharing runtime state."""
        if not isinstance(laws, dict) or not laws:
            raise ValueError('laws must be a nonempty object')
        self.nonlinear_laws[material_id] = dict(laws)

    def get_nonlinear_laws(self, material_id: int) -> Dict[str, Any]:
        return self.nonlinear_laws.get(material_id, {})

    def get_nonlinear_material(
        self,
        material_id: int
    ) -> Optional[NonlinearMaterialProperty]:
        """非線形材料を取得

        Args:
            material_id: 材料ID

        Returns:
            非線形材料プロパティ、存在しない場合はNone
        """
        return self.nonlinear_materials.get(material_id)

    def is_nonlinear_material(self, material_id: int) -> bool:
        """非線形材料かどうかを判定

        Args:
            material_id: 材料ID

        Returns:
            非線形材料の場合True
        """
        return material_id in self.nonlinear_materials or material_id in self.nonlinear_laws
        
    def get_elastic_matrix_3d(self, material_id: int) -> np.ndarray:
        """3次元弾性マトリックスを取得"""
        mat = self.get_material(material_id)
        if not mat:
            raise ValueError(f"Material ID {material_id} not found")
            
        E, nu = mat.E, mat.nu
        factor = E / ((1 + nu) * (1 - 2 * nu))
        
        D = np.zeros((6, 6))
        # 正規応力成分
        D[0:3, 0:3] = (1 - nu) * np.eye(3)
        D[0, 1] = D[1, 0] = nu
        D[0, 2] = D[2, 0] = nu
        D[1, 2] = D[2, 1] = nu
        # せん断応力成分
        D[3:6, 3:6] = (1 - 2 * nu) / 2 * np.eye(3)
        
        return factor * D
        
    def get_elastic_matrix_plane_stress(self, material_id: int) -> np.ndarray:
        """平面応力状態の弾性マトリックスを取得"""
        mat = self.get_material(material_id)
        if not mat:
            raise ValueError(f"Material ID {material_id} not found")
            
        E, nu = mat.E, mat.nu
        factor = E / (1 - nu * nu)
        
        D = np.array([
            [1, nu, 0],
            [nu, 1, 0],
            [0, 0, (1 - nu) / 2]
        ])
        
        return factor * D
        
    def get_elastic_matrix_plane_strain(self, material_id: int) -> np.ndarray:
        """平面ひずみ状態の弾性マトリックスを取得"""
        mat = self.get_material(material_id)
        if not mat:
            raise ValueError(f"Material ID {material_id} not found")
            
        E, nu = mat.E, mat.nu
        factor = E / ((1 + nu) * (1 - 2 * nu))
        
        D = np.array([
            [1 - nu, nu, 0],
            [nu, 1 - nu, 0],
            [0, 0, (1 - 2 * nu) / 2]
        ])
        
        return factor * D
