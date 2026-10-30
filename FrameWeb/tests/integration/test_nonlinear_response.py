"""Load-step states of nonlinear members and support springs (GLOSSARY: 部材の状態).

Each load_step result reports, per hysteresis component, the engine's branch,
deformation and force, and how many skeleton points the largest deformation so
far has passed (JRSNAP's member state; Aggre-SE finds the yield point from it).
The P7 oracle steps are recorded in FrameWebforCS/tests/data/JRSNAP検証/README.md:
FrameWeb passes the M-theta skeleton's 2nd point (yield) at step 229, and the
footing's rotational spring passes its 1st and 2nd points at JRSNAP's steps 13
and 57.
"""

import copy
import json

import pytest

from fem.analysis_result_sets import build_analysis_result_set
from fem.result_contracts import ResultContractError, validate_analysis_result_set
from tests.support.paths import ROOT

pytestmark = [pytest.mark.integration, pytest.mark.material_nonlinear]

P7 = ROOT.parent / "FrameWebforCS" / "tests" / "data" / "JRSNAP検証" / "models" / "線路方向_Case1.json"


def _first_index(results, rows_key, id_key, row_id, component, points):
    for result in results:
        rows = result["nonlinear_response"][rows_key]
        state = next(row for row in rows if row[id_key] == row_id)["components"][component]
        if state["points_passed"] >= points:
            return result["state"]["index"]
    return None


@pytest.fixture(scope="module")
def p7_result_set():
    return build_analysis_result_set(json.loads(P7.read_text(encoding="utf-8")))


@pytest.mark.slow
def test_p7_member_and_spring_states_pass_the_recorded_points(p7_result_set):
    results = p7_result_set["results"]

    assert _first_index(results, "members", "member_id", "6", "moment_z", 2) == 229
    assert _first_index(results, "supports", "node_id", "9", "rz", 1) == 13
    assert _first_index(results, "supports", "node_id", "9", "rz", 2) == 57


@pytest.mark.slow
def test_p7_states_report_the_section_and_spring_values(p7_result_set):
    final = p7_result_set["results"][-1]
    member = final["nonlinear_response"]["members"]
    support = final["nonlinear_response"]["supports"]

    assert [row["member_id"] for row in member] == ["6"]
    moment = member[0]["components"]["moment_z"]
    assert moment["branch"] == "skeleton"
    assert moment["force"] == pytest.approx(59045.1, abs=0.1)
    assert [row["node_id"] for row in support] == ["9"]
    rotation = support[0]["components"]["rz"]
    assert rotation["branch"] == "skeleton_neg"
    assert rotation["force"] == pytest.approx(-79846.4, abs=0.1)


@pytest.mark.slow
@pytest.mark.parametrize("edit", [
    lambda response: response["members"][0]["components"]["moment_z"].update(points_passed=5),
    lambda response: response["supports"][0]["components"]["rz"].pop("branch"),
    lambda response: response["members"][0].update(member_id="99"),
])
def test_the_contract_rejects_a_malformed_state(p7_result_set, edit):
    broken = copy.deepcopy(p7_result_set)
    edit(broken["results"][-1]["nonlinear_response"])
    with pytest.raises(ResultContractError):
        validate_analysis_result_set(broken)
