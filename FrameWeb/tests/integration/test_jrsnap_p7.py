"""P7 railway pier: FrameWeb reproduces the JRSNAP static nonlinear analyses.

The JRSNAP 5.2-L03 inputs (.ndt), the values read from its results (DB1), the
FrameWeb models and the scripts that make and compare them are in
FrameWebforCS/tests/data/JRSNAP検証 (README.md there records the translation
and every difference). Each Case is solved with fixed, incremental and limited
loads through nonlinear_combine under displacement control, and compared with
JRSNAP state by state at the same control displacement.
"""

import json
import sys

import pytest

from tests.support.paths import ROOT

pytestmark = [pytest.mark.integration, pytest.mark.oracle, pytest.mark.material_nonlinear, pytest.mark.slow]

FOLDER = ROOT.parent / "FrameWebforCS" / "tests" / "data" / "JRSNAP検証"
sys.path.insert(0, str(FOLDER / "scripts"))
from compare import TOLERANCE, solve, summarize, table  # noqa: E402
from make_models import CASES  # noqa: E402

QUANTITIES = ("base_shear", "foundation_moment", "foundation_rotation", "mtheta_moment", "top_dx")
# Past the yield point the M-theta skeleton is almost flat, and JRSNAP's rotation
# leaves its own skeleton there; FrameWeb's lies on it (README, 差を調べたこと).
JRSNAP_OFF_SKELETON = {"線路方向_Case1", "線路方向_Case2"}


@pytest.mark.parametrize("case", CASES)
def test_frameweb_reproduces_the_jrsnap_pushover(case):
    model = json.loads((FOLDER / "models" / f"{case}.json").read_text(encoding="utf-8"))
    reference = json.loads((FOLDER / "reference" / f"{case}.json").read_text(encoding="utf-8"))
    states = solve(model)
    assert len(states) == len(reference["steps"])
    result = summarize(table(model, reference, states), reference)

    for name in QUANTITIES:
        assert result[name]["max_difference"] <= TOLERANCE, (name, result[name])
    rotation = result["mtheta_rotation"]
    assert rotation["frameweb_off_own_skeleton"] < 1e-12
    if case in JRSNAP_OFF_SKELETON:
        assert rotation["max_difference"] > TOLERANCE
        assert rotation["max_difference"] < rotation["jrsnap_off_own_skeleton"]
    else:
        assert rotation["max_difference"] <= TOLERANCE, rotation

    events = result["events"]
    assert events["foundation_second_branch"]["frameweb"] == events["foundation_second_branch"]["jrsnap"]
    assert events["mtheta_past_point_1"]["frameweb"] == events["mtheta_past_point_1"]["jrsnap"]
    for point in ("mtheta_past_point_2", "mtheta_past_point_3"):
        frameweb, jrsnap = events[point]["frameweb"], events[point]["jrsnap"]
        assert (frameweb is None) == (jrsnap is None)
        if jrsnap is not None:
            assert 0 <= frameweb - jrsnap <= 2, (point, frameweb, jrsnap)
