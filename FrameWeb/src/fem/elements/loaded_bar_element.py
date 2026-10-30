"""Linear beam boundary-value solution, with consistent loads and releases.

The state transition integrates equilibrium and compatibility exactly on each
constant-property segment. No penalty stiffness is used for hinges/foundations.
"""
import numpy as np
import math
from scipy.linalg import expm

from .bar_element import BEBarElement
from .member_line_loads import MemberLineLoads
from ..load_kinds import INCREMENTAL
from ..precision import product_error as _product_error


def _transform_parts(matrix, high, low):
    result, correction = np.zeros(len(high)), np.zeros(len(high))
    for i, row in enumerate(matrix):
        terms, errors = [], []
        for a, b, c in zip(row, high, low):
            if a == 0:
                continue
            product = a*b
            terms.append(product)
            errors.extend([_product_error(a, b, product), a*c])
        result[i] = math.fsum(terms)
        correction[i] = math.fsum([*terms, -result[i], *errors])
    return result, correction


def boundary_map(length, rigidity, foundation=0., bending=False, q=(0., 0.), shear_rigidity=None):
    """Return end stiffness and consistent load from an independent ODE BVP.

    Axial state (u,N): u'=N/EA, N'=k*u-q.
    Bending state (v,theta,M,V): v'=theta, theta'=M/EI,
    M'=V, V'=q-k*v. End actions are (V_i,-M_i,-V_j,M_j).
    Two extra states represent q(x)=q_i+(q_j-q_i)*x/L.
    """
    if foundation > 0:
        growth = length*(foundation/rigidity)**(.25 if bending else .5)
        if bending and shear_rigidity is not None:
            growth += length*np.sqrt(foundation/shear_rigidity)
        if growth > 4:
            # Exponentials contain growing and decaying modes. Form the exact
            # map on a short interval, then double via static condensation;
            # never form exp(growth) on a long supported member.
            levels = math.ceil(math.log2(growth/4))
            short = length/(2**levels)
            k, f0 = boundary_map(short, rigidity, foundation, bending, (1.,0.), shear_rigidity)
            _, f1 = boundary_map(short, rigidity, foundation, bending, (0.,1.), shear_rigidity)
            width = 2 if bending else 1
            for _ in range(levels):
                joined = np.zeros((3*width, 3*width))
                joined[:2*width,:2*width] += k
                joined[width:,width:] += k
                # Two basis loads for q(0), q(L). At the shared midpoint the
                # pressure is their arithmetic mean, including signed loads.
                loads = np.zeros((3*width, 2))
                loads[:2*width,0] += f0+.5*f1
                loads[width:,0] += .5*f0
                loads[:2*width,1] += .5*f1
                loads[width:,1] += .5*f0+f1
                outer = list(range(width))+list(range(2*width,3*width))
                inner = list(range(width,2*width))
                coupling = joined[np.ix_(outer,inner)]
                block = joined[np.ix_(inner,inner)]
                k = joined[np.ix_(outer,outer)]-coupling@np.linalg.solve(block,coupling.T)
                reduced = loads[outer]-coupling@np.linalg.solve(block,loads[inner])
                f0, f1 = reduced.T
            return (k+k.T)/2, q[0]*f0+q[1]*f1
    if foundation == 0 and (not bending or shear_rigidity is None):
        # Polynomial solution avoids an ill-scaled matrix exponential for
        # short, very stiff segments. These are exact consistent loads.
        l, q0, q1 = length, q[0], q[1]
        if not bending:
            return (rigidity/l*np.array([[1., -1.], [-1., 1.]]),
                    l/6*np.array([2*q0+q1, q0+2*q1]))
        stiffness = rigidity/l**3*np.array([
            [12., 6*l, -12., 6*l],
            [6*l, 4*l*l, -6*l, 2*l*l],
            [-12., -6*l, 12., -6*l],
            [6*l, 2*l*l, -6*l, 4*l*l]])
        load = np.array([l*(7*q0+3*q1)/20, l*l*(3*q0+2*q1)/60,
                         l*(3*q0+7*q1)/20, -l*l*(2*q0+3*q1)/60])
        return stiffness, load
    n = 4 if bending else 2
    a = np.zeros((n+2, n+2))
    if bending:
        a[0, 1], a[1, 2], a[2, 3] = 1, 1/rigidity, 1
        if shear_rigidity is not None:
            a[0, 3] = -1/shear_rigidity
        a[3, 0], a[3, n] = -foundation, 1
        displacement = [0, 1]
        force = [3, 2]
        signs_i, signs_j = [1, -1], [-1, 1]
    else:
        a[0, 1], a[1, 0], a[1, n] = 1/rigidity, foundation, -1
        displacement, force = [0], [1]
        signs_i, signs_j = [-1], [1]
    a[n, n+1] = 1
    transition = expm(a*length)
    t = transition[:n, :n]
    offset = transition[:n, n:] @ [q[0], (q[1]-q[0])/length]
    c = np.vstack([np.eye(n)[displacement], t[displacement]])
    h = np.r_[np.zeros(len(displacement)), offset[displacement]]
    g = np.vstack([np.eye(n)[force]*np.array(signs_i)[:, None],
                   t[force]*np.array(signs_j)[:, None]])
    f0 = np.r_[np.zeros(len(force)), offset[force]*signs_j]
    k = np.linalg.solve(c.T, g.T).T
    fixed = f0-k@h
    return (k+k.T)/2, -fixed


def beam_mode_system(length, material, section, foundation, line_load, shear_correction):
    """Local stiffness and consistent end load of the axial/bending/torsion modes."""
    k, f = np.zeros((12, 12)), np.zeros(12)
    modes = [(0, [0, 6], material.E*section.area, False, [1, 1]),
             (1, [1, 5, 7, 11], material.E*section.Iz, True, [1, 1, 1, 1]),
             (2, [2, 4, 8, 10], material.E*section.Iy, True, [1, -1, 1, -1]),
             (3, [3, 9], material.G*section.J, False, [1, 1])]
    for mode, indices, rigidity, bending, signs in modes:
        if rigidity == 0 and not foundation[mode] and not np.any(line_load[mode]):
            continue
        if rigidity <= 0:
            raise ValueError('A loaded or supported beam mode requires positive rigidity')
        km, fm = boundary_map(length, rigidity, foundation[mode], bending, line_load[mode],
                               material.G*section.area*(section.kappa_y if mode == 1 else section.kappa_z)
                               if bending and shear_correction else None)
        signs = np.array(signs)
        k[np.ix_(indices, indices)] = km*signs[:, None]*signs[None, :]
        f[indices] = fm*signs
    return k, f


class LoadedBarElement(MemberLineLoads, BEBarElement):
    """Linear beam with optional shear, foundation, loads and end releases."""
    def __init__(self, *args, releases=(), foundation=(0., 0., 0., 0.), shear_correction=False, **kwargs):
        super().__init__(*args, **kwargs)
        self.releases = sorted(set(releases))
        self.foundation = np.asarray(foundation, dtype=float)
        self.shear_correction = shear_correction
        self._reset_line_loads()
        self.temperature_strain = 0.
        self.transfer_load = np.zeros(12)
        self._cache = None

    def set_node_coordinates(self, coordinates):
        super().set_node_coordinates(coordinates)
        self._cache = None

    def _local_system(self):
        """Local stiffness and the incremental consistent end load."""
        k, loads = self._local_loads()
        return k, loads[INCREMENTAL]

    def local_member_loads(self):
        return self._local_loads()[1]

    def _local_loads(self):
        if self._cache is not None:
            return self._cache
        m = self.material.materials[self.material_id]
        p = self.bar_param
        if all(getattr(p, name) == 0 for name in ('area', 'Iy', 'Iz', 'J')):
            if np.any(self.foundation):
                raise ValueError('Load-transfer members cannot have foundations')
            if set(self.line_loads) != {INCREMENTAL}:
                raise ValueError('Load-transfer members carry only incremental loads')
            f = self.transfer_load.copy()
            q0, q1 = self.line_load.T
            f[:4] += self.length*(2*q0+q1)/6
            f[6:10] += self.length*(q0+2*q1)/6
            self._cache = np.zeros((12, 12)), {INCREMENTAL: f}
            return self._cache
        loads = {}
        for kind, line_load in self.line_loads.items():
            k, loads[kind] = beam_mode_system(self.length, m, p, self.foundation, line_load,
                                              self.shear_correction)
        loads[INCREMENTAL][[0, 6]] += m.E*p.area*self.temperature_strain*np.array([-1, 1])
        if self.releases:
            r = self.releases
            # Released rotations are internal unknowns; static condensation
            # applies to the load vector as well as the stiffness.
            block = k[np.ix_(r, r)]
            inverse = np.linalg.pinv(block, rcond=1e-13)
            for kind, f in loads.items():
                if not np.allclose(block@inverse@f[r], f[r], rtol=1e-10, atol=1e-12):
                    raise ValueError('Unbalanced load on released beam mode')
                f = f-k[:, r]@inverse@f[r]
                f[r] = 0.
                loads[kind] = f
            k = k-k[:, r]@inverse@k[r, :]
            k[r, :], k[:, r] = 0., 0.
        self._cache = k, loads
        return self._cache

    def get_stiffness_matrix(self):
        k, _ = self._local_system()
        t = self.get_transformation_matrix(12)
        return t.T@k@t

    def get_member_load_vector(self):
        return self.get_transformation_matrix(12).T@self._local_system()[1]

    def _elastic_local_force(self, displacement, displacement_correction=None):
        k, _ = self._local_system()
        if displacement_correction is not None:
            # Keep the low part separate through cancellation. Adding it to
            # the public nodal displacement first would discard it again.
            t = self.get_transformation_matrix(12)
            high, low = _transform_parts(t, displacement, displacement_correction)
            if not np.any(self.foundation):
                for j in range(6):
                    terms = [high[j+6], -high[j], low[j+6], -low[j]]
                    if j == 1:
                        product = self.length*high[5]
                        terms += [-product, -_product_error(self.length, high[5], product), -self.length*low[5]]
                    elif j == 2:
                        product = self.length*high[4]
                        terms += [product, _product_error(self.length, high[4], product), self.length*low[4]]
                    low[j+6] = math.fsum(terms)
                low[:6] = 0.
                return k@low
            force, remainder = _transform_parts(k, high, low)
            return force+remainder
        deformation = self.get_transformation_matrix(12)@displacement
        if not np.any(self.foundation):
            # Subtract a rigid motion before multiplying large stiffnesses.
            # This is a kinematic identity, not clipping small end forces.
            translation, rotation = deformation[:3].copy(), deformation[3:6].copy()
            deformation[6:9] -= translation + np.cross(rotation, [self.length, 0., 0.])
            deformation[9:12] -= rotation
            deformation[:6] = 0.
        return k@deformation

    def get_internal_force(self, displacement, displacement_correction=None):
        # Consistent loads are already included in the solver's external load.
        return self.get_transformation_matrix(12).T@self._elastic_local_force(displacement, displacement_correction)

    def calculate_forces(self, displacement, displacement_correction=None):
        local = (self._elastic_local_force(displacement, displacement_correction)
                 - self.member_load_at(self.load_factor))
        return dict(i_end=local[:6].copy(), j_end=local[6:].copy())
