"""M-theta skeletons entered as rotations (GLOSSARY: 骨格曲線).

JRSNAP enters an M-theta member as moments M1..M4 and rotations theta1..theta4.
FrameWeb's nonlinear bar works with curvature, so the engine divides each
rotation by the length of the member that uses the material:
phi_k = theta_k / L. The P7 model was written with phi_k = theta_k / 0.010 by
make_models.py, so its rotation form must give the same analysis.
"""

import copy
import json

import pytest

from fem.analysis_result_sets import build_analysis_result_set
from fem.diagnostics import InputValidationError
from tests.support.paths import ROOT

pytestmark = [pytest.mark.integration, pytest.mark.material_nonlinear]

P7 = ROOT.parent / "FrameWebforCS" / "tests" / "data" / "JRSNAP検証" / "models" / "線路方向_Case1.json"
M_THETA_LENGTH = 0.010


def _rotation_form(curvature_form: dict, length: float) -> dict:
    rotation = {key: value for key, value in curvature_form.items() if not key.startswith("delta_")}
    rotation["skeleton"] = "m_theta"
    for k in range(1, 5):
        rotation[f"theta_{k}"] = curvature_form[f"delta_{k}"]*length
    return rotation


def _states(data: dict) -> list[dict]:
    return build_analysis_result_set(data)["results"]


def _reactions(state: dict) -> list[float]:
    return [value for row in state["support_reactions"] for value in row["components"].values()]


@pytest.mark.slow
def test_p7_in_rotation_form_matches_its_curvature_form():
    curvature = json.loads(P7.read_text(encoding="utf-8"))
    rotation = copy.deepcopy(curvature)
    material = rotation["element"]["1"]["2"]
    material["nonlinear"] = _rotation_form(material["nonlinear"], M_THETA_LENGTH)

    expected, actual = _states(curvature), _states(rotation)

    assert len(actual) == len(expected) == 301
    for left, right in zip(expected, actual, strict=True):
        assert right["state"]["load_factor"] == pytest.approx(left["state"]["load_factor"], rel=1e-9)
        assert _reactions(right) == pytest.approx(_reactions(left), rel=1e-7, abs=1e-6)


def _two_cantilevers(nonlinear_by_member: dict[str, dict]) -> dict:
    """Two vertical cantilevers (L = 1 and 2) pushed sideways by the same tip load."""
    elements = {str(index): {"E": 10000, "nu": 0.25, "A": 1, "Iy": 1, "Iz": 1, "J": 1,
                             "nonlinear": nonlinear}
                for index, nonlinear in enumerate(nonlinear_by_member.values(), 1)}
    material_of = {member: (str(index) if len(elements) > 1 else "1")
                   for index, member in enumerate(nonlinear_by_member, 1)}
    if len(set(json.dumps(n, sort_keys=True) for n in nonlinear_by_member.values())) == 1:
        elements = {"1": elements["1"]}
        material_of = {member: "1" for member in nonlinear_by_member}
    return {
        "dimension": 2,
        "nonlinear": True,
        "node": {"1": {"x": 0, "y": 0, "z": 0}, "2": {"x": 0, "y": 1, "z": 0},
                 "3": {"x": 5, "y": 0, "z": 0}, "4": {"x": 5, "y": 2, "z": 0}},
        "member": {"1": {"ni": 1, "nj": 2, "e": int(material_of["1"])},
                   "2": {"ni": 3, "nj": 4, "e": int(material_of["2"])}},
        "element": {"1": elements},
        "fix_node": {"1": [{"n": 1, "tx": 1, "ty": 1, "rz": 1}, {"n": 3, "tx": 1, "ty": 1, "rz": 1}]},
        "load": {"1": {"name": "push", "kind": "incremental",
                       "load_node": [{"n": 2, "tx": 1}, {"n": 4, "tx": 1}]}},
        "nonlinear_combine": {"1": {"row": 1, "name": "push", "C1": 1,
                                    "load_factors": [2.0, 4.0, 6.0, 8.0]}},
    }


ROTATION = {"type": "jr_stiffness_reduction", "symmetric": True, "hysteresis_dofs": ["moment_z"],
            "skeleton": "m_theta", "theta_1": 0.0005, "theta_2": 0.002, "theta_3": 0.01,
            "P_1": 5, "P_2": 8, "P_3": 10, "beta": 0}


def _curvature(length: float) -> dict:
    form = {key: value for key, value in ROTATION.items() if not key.startswith("theta_") and key != "skeleton"}
    form.update({f"delta_{k}": ROTATION[f"theta_{k}"]/length for k in (1, 2, 3)})
    return form


def test_each_member_divides_the_rotations_by_its_own_length():
    shared = _states(_two_cantilevers({"1": ROTATION, "2": ROTATION}))
    separate = _states(_two_cantilevers({"1": _curvature(1.0), "2": _curvature(2.0)}))

    for left, right in zip(separate, shared, strict=True):
        assert _reactions(right) == pytest.approx(_reactions(left), rel=1e-9, abs=1e-9)
        displacements = [row["components"] for row in right["node_displacements"]]
        expected = [row["components"] for row in left["node_displacements"]]
        assert displacements == pytest.approx(expected, rel=1e-9, abs=1e-12)


@pytest.mark.parametrize("edit, message", [
    (lambda n: n.update(skeleton="m_kappa"), "skeleton"),
    (lambda n: n.pop("theta_2"), "theta_2"),
    (lambda n: n.update(delta_1=0.1), "delta_1"),
    (lambda n: n.pop("skeleton"), "theta_1"),
])
def test_an_inconsistent_rotation_skeleton_is_rejected(edit, message):
    nonlinear = copy.deepcopy(ROTATION)
    edit(nonlinear)
    with pytest.raises((InputValidationError, ValueError), match=message):
        build_analysis_result_set(_two_cantilevers({"1": nonlinear, "2": nonlinear}))
