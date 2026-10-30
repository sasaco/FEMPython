"""P7 railway pier: the pushover must not depend on rounding of its inputs.

The desktop stores E and A as float, which changes the M-theta element's E by
about 1e-8 relative. Such a change must not move the analysis to another path:
the peak base shear stays the recorded 10701.4 kN (JRSNAP 10707.6 kN) and every
step converges.
"""

import copy
import json

import pytest

from fem.analysis_result_sets import build_analysis_result_set
from tests.support.paths import ROOT

pytestmark = [pytest.mark.integration, pytest.mark.material_nonlinear, pytest.mark.slow]

MODEL = ROOT.parent / "FrameWebforCS" / "tests" / "data" / "JRSNAP検証" / "models" / "線路方向_Case1.json"
SUPPORT = "9"
M_THETA_MATERIAL = ("1", "2")
RECORDED_PEAK_BASE_SHEAR = 10701.4


def _peak_base_shear(data: dict) -> tuple[int, float]:
    result = build_analysis_result_set(data)
    states = result["results"]
    peak = max(abs(next(row["components"]["fx"] for row in state["support_reactions"]
                        if str(row["node_id"]) == SUPPORT)) for state in states)
    return len(states), peak


@pytest.mark.parametrize("relative_change", [1e-8, -1e-8, 1e-7])
def test_rounding_the_m_theta_stiffness_keeps_the_pushover(relative_change):
    data = json.loads(MODEL.read_text(encoding="utf-8"))
    sheet, material = M_THETA_MATERIAL
    element = data["element"][sheet][material]
    element["E"] *= 1 + relative_change

    steps, peak = _peak_base_shear(copy.deepcopy(data))

    assert steps == 301
    assert peak == pytest.approx(RECORDED_PEAK_BASE_SHEAR, abs=0.1)


def test_the_desktop_float_rounding_keeps_the_pushover():
    """E and A as the desktop writes them (shortest float text) and nu dropped."""
    data = json.loads(MODEL.read_text(encoding="utf-8"))
    for rows in data["element"].values():
        for element in rows.values():
            element.pop("nu", None)
    m_theta = data["element"]["1"]["2"]
    m_theta["E"], m_theta["A"] = 2918180.2, 342.67932

    steps, peak = _peak_base_shear(data)

    assert steps == 301
    assert peak == pytest.approx(RECORDED_PEAK_BASE_SHEAR, abs=0.1)
