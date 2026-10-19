"""The JRSNAP golden tools regenerate the frozen inputs and edit .ndt files as documented."""

import pytest

from tests.support.paths import DATA
from tools.jrsnap_golden import make_cases, ndt_tools

GOLDEN = DATA / "jrsnap_golden"
FROZEN_NDU = [
    ("support_trilinear01", "support_trilinear01", 100),
    ("support_trilinear01", "support_trilinear01_n1000", 1000),
    ("support_trilinear02_neg", "support_trilinear02_neg", 100),
    ("support_trilinear03_softening", "support_trilinear03_softening", 200),
]


@pytest.mark.parametrize("case, stem, steps", FROZEN_NDU, ids=[f[1] for f in FROZEN_NDU])
def test_legacy_cases_regenerate_the_frozen_ndu_byte_for_byte(case, stem, steps):
    assert make_cases.build(case, steps, stem) == (GOLDEN / case / f"{stem}.ndu").read_bytes()


# The accuracy-priority reruns analyze the frozen .ndu (copied under its own name) with column 70 of the .ndt set.
@pytest.mark.parametrize("case, stem, steps, directory", [
    ("support_trilinear01", "support_trilinear01", 100, "support_trilinear01_f1"),
    ("support_trilinear01", "support_trilinear01_n1000", 1000, "support_trilinear01_f1"),
    ("support_trilinear02_neg", "support_trilinear02_neg", 100, "support_trilinear02_neg_f1"),
])
def test_accuracy_reruns_use_the_frozen_ndu(case, stem, steps, directory):
    assert (GOLDEN / directory / f"{stem}.ndu").read_bytes() == (GOLDEN / case / f"{stem}.ndu").read_bytes()
    assert (GOLDEN / directory / f"{stem}.ndt").read_bytes() == (GOLDEN / case / f"{stem}.ndt").read_bytes()
    analyzed = (GOLDEN / directory / f"{stem}_f1.ndt").read_bytes()
    assert analyzed == ndt_tools.set_convergence((GOLDEN / case / f"{stem}.ndt").read_bytes(), "accuracy")


# Every published model case regenerates its .ndu byte for byte, and its analyzed .ndt is the writer's plus the
# recorded DA edits.
PUBLISHED_MODELS = [
    ("linear01_cantilever", "linear01_cantilever", {}),
    ("linear01_cantilever_moment", "linear01_cantilever_moment", {}),
    ("linear02_moment_disp", "linear02_moment_disp", {}),
    ("support_trilinear04_neg_disp", "support_trilinear04_neg_disp", {}),
    ("support_trilinear04_neg_disp", "support_trilinear04_neg_disp_f1", {"convergence": "accuracy"}),
    ("support_bilinear01", "support_bilinear01", {"convergence": "accuracy"}),
    ("support_multi01", "support_multi01", {"convergence": "accuracy"}),
    ("member_mphi01_moment", "member_mphi01_moment", {"convergence": "accuracy"}),
    ("member_mphi02_disp", "member_mphi02_disp", {"convergence": "accuracy", "ftm": "1.0E-08"}),
    ("member_beam001", "member_beam001", {"convergence": "accuracy", "ftm": "1.0E-08"}),
    ("combined01", "combined01", {"convergence": "accuracy", "ftm": "1.0E-08"}),
]


@pytest.mark.parametrize("case, stem, edits", PUBLISHED_MODELS, ids=[m[1] for m in PUBLISHED_MODELS])
def test_published_models_regenerate_their_inputs(case, stem, edits):
    directory = GOLDEN / stem
    assert make_cases.build_model(make_cases.MODELS[case], stem) == (directory / f"{stem}.ndu").read_bytes()
    if not edits:
        return
    writer = (directory / f"{stem}.writer.ndt").read_bytes()
    expected = writer
    if "convergence" in edits:
        expected = ndt_tools.set_convergence(expected, edits["convergence"])
    if "ftm" in edits:
        expected = ndt_tools.set_ftm(expected, edits["ftm"])
    assert (directory / f"{stem}.ndt").read_bytes() == expected


def test_models_build_one_row_per_item():
    model = make_cases.MODELS["support_multi01"]
    text = make_cases.build_model(model, "support_multi01").decode("cp932")
    lines = dict(line.split("=", 1) for line in text.splitlines() if "=" in line)
    assert lines["JointNum"] == "3" and lines["JointXY3"] == "2,0,3"
    assert lines["MemberNum"] == "2" and lines["MemberInfo2"] == "2,3,1" and lines["MemberLeng2"] == "1"
    assert lines["ElementNum"] == "2"
    assert lines["ElementInfo2"] == "1,0,0,0,2,3,0,0,1000,1000,26.5,0,0,2"
    assert (lines["SuppotNum"], lines["SuppotRow"]) == ("9", "9")
    assert lines["SuppotInfo1"] == " ,1,1,1000,10,5,200,14,7,50,500,100,0.001"
    assert lines["SuppotInfo3"] == " ,3,1,1000,10,5, , , , , , , "
    assert lines["SuppotInfo9"] == " ,3,3, , , , , , , , , , "
    assert lines["JLoadInfo1"] == "1, ,3,1,30, , , "
    assert lines["DataInfo"] == "1,1,200,3,200,200, , , ,999,999,2,2,1,1,2,1,1,0.1,0.001,0,1,0,0,0,0"
    assert lines["NonLinNum"] == "0,0,0" and "NonLinFix1" not in lines
    assert [k for k in lines if k.startswith("JibanShogenInfo")] == ["JibanShogenInfo1", "JibanShogenInfo2"]
    assert [k for k in lines if k.startswith("Suppot_ChokuKisoCaseNo")] == [f"Suppot_ChokuKisoCaseNo{k}" for k in range(1, 10)]
    assert [k for k in lines if k.startswith("Member_BunpuBane")] == ["Member_BunpuBane1", "Member_BunpuBane2"]
    assert [k for k in lines if k.startswith("JibanTeikouKeisuu")] == ["JibanTeikouKeisuu1", "JibanTeikouKeisuu2", "JibanTeikouKeisuu3"]
    assert [k for k in lines if k.startswith("SesfPiIJ")] == ["SesfPiIJ1", "SesfPiIJ2"]


def test_member_model_writes_the_fixed_m_phi_table_like_beam001():
    text = make_cases.build_model(make_cases.MODELS["member_mphi01_moment"], "member_mphi01_moment").decode("cp932")
    lines = dict(line.split("=", 1) for line in text.splitlines() if "=" in line)
    assert lines["NonLinNum"] == "1,0,1"
    assert lines["NonLinInfo1"] == "2,0,2,1,1,0,0,0,0,"
    assert lines["NonLinFix1"] == "10,16,22,14,0.001,0.004,0.01,0.018,0.001,0.004,0.01,0.018,0,0,0,0,0,0,0,0,0,0,0,0,"
    assert lines["ElementInfo1"] == "3,0,0,0,1,2,1,0,1000,0.0004,25,0,0,1"
    assert lines["JLoadInfo1"] == "1, ,1,1, , ,21, "
    assert lines["SuppotRow"] == "3"
    # Flags B=on, A=off, C=off, D=on: the writer maps field 12 to column 32 and field 13 to column 31.
    assert lines["DataInfo"] == "1,2, , ,100,100, ,100,100,999,999,1,2,2,1,2,1,1, ,0.001,0,1,0,0,0,0"


def test_number_formats_like_the_screen():
    assert [make_cases.number(v) for v in (1, 1.0, -10, 26.5, 1e-5, 0.0001, 0.1, 1e-8, 2.5e-7)] == [
        "1", "1", "-10", "26.5", "1E-05", "0.0001", "0.1", "1E-08", "2.5E-07",
    ]


def test_set_convergence_pads_the_first_da_line_to_column_70_and_changes_nothing_else():
    ndt = (GOLDEN / "support_trilinear01" / "support_trilinear01.ndt").read_bytes()
    assert ndt_tools.convergence_flag(ndt) is None
    edited = ndt_tools.set_convergence(ndt, "accuracy")
    assert ndt_tools.convergence_flag(edited) == "1"
    before, after = ndt.split(b"\r\n"), edited.split(b"\r\n")
    assert len(before) == len(after)
    changed = [i for i, (a, b) in enumerate(zip(before, after)) if a != b]
    assert len(changed) == 1
    index = changed[0]
    assert before[index - 1].startswith(b"DA")
    assert after[index] == before[index].ljust(69, b" ") + b"1"
    padded = list(before)
    padded[index] = before[index].ljust(70, b" ")
    assert ndt_tools.set_convergence(edited, "speed") == b"\r\n".join(padded)


def test_da_fields_are_replaced_right_aligned_in_their_columns():
    ndt = (GOLDEN / "support_trilinear03_softening" / "support_trilinear03_softening.ndt").read_bytes()
    assert ndt_tools.da_field(ndt, ndt_tools.FTM_FIELD) == "0.001"
    assert ndt_tools.da_field(ndt, ndt_tools.DSMAX_FIELD) == "0.200"
    edited = ndt_tools.set_dsmax(ndt_tools.set_ftm(ndt, "1.0E-08"), "0.0004")
    assert ndt_tools.da_field(edited, ndt_tools.FTM_FIELD) == "1.0E-08"
    assert ndt_tools.da_field(edited, ndt_tools.DSMAX_FIELD) == "0.0004"
    before, after = ndt.split(b"\r\n"), edited.split(b"\r\n")
    changed = [i for i, (a, b) in enumerate(zip(before, after)) if a != b]
    assert len(changed) == 2 and before[changed[0] - 1].startswith(b"DA")
    for i in changed:
        assert len(after[i]) == len(before[i])
        assert after[i][:10] == before[i][:10] and after[i][20:] == before[i][20:]
    with pytest.raises(ValueError):
        ndt_tools.set_ftm(ndt, "12345678901")
