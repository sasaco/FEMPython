# Handoff: Does FrameWeb reproduce JRSNAP nonlinear results exactly?

Date: 2026-10-19
Branch: `sasa/nonli`, HEAD `4424552` (working tree clean apart from this file and its archive copy)
Previous handoff (JRSNAP-compatible support springs, preserved byte for byte, SHA-256 `329eb409…f177`): `FrameWeb/docs/plans/archive/handoff-jrsnap-support-springs-20261019.md`

This file is the work request for a fresh session. Read it fully before acting.

---

## Work request

### Goal

Determine, **with evidence**, whether FrameWeb's static nonlinear analysis results match JRSNAP's results for the same model, and record the answer per feature, per case and per quantity.

- Each answer is one of three: **matches** (within a stated tolerance), **does not match** (with the measured difference), or **not verified** (with the reason).
- "Matches exactly" is an answer you must prove, not one you aim for. A well-evidenced "does not match" or "cannot be compared under identical conditions" is a valid outcome.
- Where a comparison passes, freeze it as a regression test, so later sessions can rerun it without JRSNAP.

### Scope: confirm with the user first

Before any comparison work, ask the user which of these is in scope. Do not choose for them.

1. **JRSNAP-compatible support springs** (`jrsnap_bilinear`/`jrsnap_trilinear`, theory manual §7.22). This is implemented and partly compared (see Verified facts). What is left:
   - load-increment control;
   - the negative side;
   - reversals and cyclic paths;
   - the bilinear type against an actual JRSNAP run;
   - multi-member models.
2. **Member material nonlinearity** (M–φ skeletons, including JR RC stiffness reduction K1–K4, axial-force dependence). No same-condition JRSNAP reference exists in the repository yet. See "Member nonlinearity: what is known".
3. Anything else the user names.

### Constraints

- **No unfounded speculation or assertion.**
  - Every statement in your reports, documents, commit messages and replies must carry its evidence: a file path, a command and its output, or a measured number.
  - If you have no evidence, write 未確認 (not verified).
  - A hypothesis must be labelled as a hypothesis (仮説) and must come with the test that would confirm or refute it. Do not present it as a finding.
  - Do not describe JRSNAP's internal algorithm (iteration, convergence, unloading rule) as fact unless a JRSNAP manual or a measured run shows it. Quote the source.
  - Do not say "probably the same" or "should match". Measure it.
- **Expected values never come from FrameWeb output.** They come from frozen JRSNAP results (`.DB1` via SNAPDBLib), hand calculations, or closed forms.
- **Fix tolerances before comparing.** Write down each tolerance and its basis before looking at the result. Never loosen a tolerance, edit a golden value, or round toward zero to make a comparison pass.
- **Do not change product behaviour to imitate JRSNAP without the user's approval.** This covers, for example, the negative-side F_neg(2|dx|) or JRSNAP's load-increment unbalance. A spec change is the user's decision. If a difference looks like a FrameWeb defect, reproduce it, report it with evidence, and ask before fixing.
- **External tools**
  - JRSNAP must be closed during headless runs, because the runner uses shared files in the install directory.
  - Run `--analyze false` first and inspect the `.ndt` before every real analysis.
  - Do not modify anything under `C:\RTRI_Program`.
  - `JRSNAPHeadless` (`C:\Users\sasai\Documents\structural-mcp\JRSNAPHeadless`) is a separate repository. Changing it, for example to add direct `.ndt` input, needs the user's approval.
  - Results from the JRSNAP 5.3-L01 trial version are for diagnosis only and are not frozen (trial licence). This follows the existing golden README.
- **Repository rules**
  - Out of scope unless the user adds it:
    - FrameWebforJS/FrameWebforCS UI and the structural-mcp contract;
    - dynamic analysis;
    - KR2/KR3 auto-derivation and αf.
  - Do not recreate `.agents/`. Skills still say `.agents/...`, but the files now live under `.claude/` (e.g. `.claude/check.ps1`, `.claude/skills/_shared/run_tests.py`). Substitute the prefix.
  - Windows PowerShell from the repository root; Python only through `uv --directory FrameWeb run --locked --extra dev ...`.
  - Ask the user before committing.

### Acceptance criteria

1. **The user has confirmed the scope**, and the confirmation is recorded in the report.
2. **Comparison record** `FrameWeb/docs/report/jrsnap-frameweb-equivalence.md`, in Japanese like the other reports, with one row per (feature × case × quantity × step range):
   - Quantities: displacement, spring force, segment flag, member end forces, reactions, load factor.
   - Each row gives the JRSNAP version, the FrameWeb commit, the control (load or displacement increment), the tolerance and its basis, the maximum measured difference with the step where it occurs, and the verdict (matches / does not match / not verified).
   - Each "does not match" row states its cause with evidence. If the cause is not proven, write 原因未特定 and list what was ruled out.
   - Each "not verified" row states what is missing.
3. **New JRSNAP references are reproducible.**
   - Every new case has its `.ndu`, `.ndt` and frozen `golden_*.json` under `FrameWeb/tests/data/jrsnap_golden/<case>/`.
   - Add input hashes to `tests/harness/test_jrsnap_golden.py`, and add the case and its regeneration command to `tests/data/jrsnap_golden/README.md`.
4. **Each "matches" verdict is pinned by a pytest** that reads only the frozen golden data (no JRSNAP at test time), carries the `oracle` mark where the existing JRSNAP tests do, and fails if the difference exceeds the stated tolerance. Confirm that each new test actually detects a difference, for example by perturbing the expected value, so you know it is not vacuous.
5. **No regressions**
   - The existing JRSNAP-related suites still pass unmodified (baseline 2026-10-19: 140 passed, see the command below).
   - Run the full gate `uv --directory FrameWeb run --locked --extra dev python -m tools.validation.check_test_results`. It takes about 75 minutes; run it in the background. Compare it with the baseline of 3,484 passed and 10 known failures in `tests/validation` (see Verified facts). Any other failure is a regression of your change.
   - `git diff --check` passes.
6. **Documents**
   - Update theory manual §7.22 "JRSNAPと比べるときの注意" (`FrameWeb/docs/理論マニュアル/08_材料非線形モデル.md`, CRLF) and the plan `FrameWeb/docs/plans/support-multilinear-springs.md` Open Questions with the findings that apply.
   - Do not claim more than the record shows.
7. **Final reply to the user**, in Japanese: per scope item, matches / does not match / not verified, each with its evidence path. Then ask whether to commit.

---

## Verified facts (with sources)

Unless noted, these come from `FrameWeb/tests/data/jrsnap_golden/README.md`, `FrameWeb/docs/report/jrsnap-support-implementation.md`, and the previous handoff.

### Frozen JRSNAP references available now

All were run in JRSNAP 5.2-L03 (JRSNAP_Input.exe 5.2.3.1) on 2026-10-18.

- **Model**: 2D, one rigid linear member, node 1 (0,−1) to node 2 (0,0).
- **Supports**: at node 2, Y and rotation are rigid; X is a trilinear nodal spring (BLOCK6 row 1).
- **Load**: X at node 1.
- **Convergence**: column 70 blank, which means speed-priority.

| Case | Spring (+SP1,+RMiT1,−RMiT1,+SP2,+RMiT2,−RMiT2,+SP3,−SP1,−SP2,−SP3) | Control | Files |
|---|---|---|---|
| `support_trilinear01` | 1000,10,5,200,14,7,50,500,100,0.001 | load increment, X=+20 kN | `golden_n100.json`, `golden_n1000.json` |
| `support_trilinear02_neg` | 1000,10,5,200,14,7,50,500,100,20 | load increment, X=−10 kN, 100 steps | `golden_n100.json` |
| `support_trilinear03_softening` | 1000,10,5,200,14,7,**−20**,500,100,0.001 | displacement increment, node 1 to +0.2 m, 200 steps | `golden_n200.json` |

All are **monotonic**. There is no JRSNAP run with reversals and no JRSNAP run of the bilinear type.

### What already matches (measured, pinned by tests)

- **Positive-side law**: in cases 01 (n=100 and n=1000) and 03, feeding JRSNAP's node-2 `dx` path into FrameWeb's law reproduces `jiban_hanryoku` to relative 1e-12 at every step, and the segment flag `bane_flg` also agrees. Tests: `tests/materials/test_jrsnap_support_hysteresis.py`, the golden replay.
- **Global solution under displacement control (case 03, all 200 steps)**: the load factor agrees with the spring force and member shear within 1e-4 kN.
  - This holds for a spring-only model and for a 2D rigid member with E=1e8 and tolerance 1e-8.
  - With E=1e10 the solver did not converge at the default tolerance (relative residual stalls near 1e-10).
  - Tests: `tests/solvers/test_jrsnap_supports.py`, `tests/integration/test_jrsnap_support_mixed.py`.

### What does not match, or is not established

- **Load-increment control**: JRSNAP leaves an unbalanced force.
  - Load minus spring force is up to 1.238 kN (case 01, n=100) and 0.380 kN (n=1000).
  - JRSNAP's displacement is up to 16.5% below the skeleton inverse at P=20 (n=100). FrameWeb's Newton removes the force residual, so its displacements differ by that amount.
  - Two simple emulations, previous-secant and previous-tangent, did not reproduce JRSNAP's path (max 7e-3 m).
  - JRSNAP's iteration rule itself is not documented in the repository (未確認).
- **Accuracy-first convergence** (column 70 `F=1`) could not be set from `.ndu`. Whether it removes the unbalance is 未確認. Testing it needs direct `.ndt` input in JRSNAPHeadless, which is a separate repository and needs the user's approval.
- **Negative side (case 02)**:
  - JRSNAP reports `jiban_hanryoku` = F_neg(2|dx|), matching to 1e-15.
  - `bane_flg` switches at the correct |dx| = 0.01 and 0.03.
  - The stored −RMiT1 reads `Rmit1_F=0`.
  - 5.3-L01 trial gave bit-identical results.
  - Whether this is JRSNAP's spec or a defect is 未確認 (plan Open Question 3, needs the vendor, RTRI). FrameWeb deliberately does **not** replicate it.
- **Unloading and reversal**: FrameWeb's elasto-plastic unloading and degradation memory are FrameWeb's own specification (§7.22 says so). Agreement with JRSNAP is 未確認, because there is no JRSNAP reversal run.
- **Cut-off sides**: JRSNAP's "0 means a very weak spring (0.01)" is not reproduced; FrameWeb rejects K=0 and uses F1=0 for a cut-off side. Identical results for cut-off cases are therefore not expected by design.
- **Rigid supports**: JRSNAP models them as springs of about 1e15, so restrained displacements are about 1e-17, not exactly 0. Account for this in tolerances.

### Member nonlinearity: what is known

Sources: `FrameWeb/docs/report/material-nonlinear-validation-phase4.md` around line 185, and `FrameWeb/docs/plans/material-nonlinear-phase4-handoff.md` around lines 284–300.

- `tests/data/snap/beam001.json` `result` is an **independent Decimal reference for FrameWeb's model, not JRSNAP output** (`docs/report/beam001-reference-values.md`).
- An external JR-SNAP 5.3-L01 `.OUT`/`.FMT` for a beam001-like model existed locally. It differs from the current JSON in several ways:
  - E: 25 kN/mm² vs 26500000;
  - steps: 100 vs 1000;
  - node positions;
  - the external skeleton has a 4th point (P=3000, δ=.002), unlike K4=0 here;
  - the final moment is 4900 vs 4850.
- That record decided not to use it as a same-condition reference until G, the shear model, the section location and the completion rules are reconciled.
- `*.OUT` is gitignored (`FrameWeb/.gitignore:27`), and on 2026-10-19 no `.OUT`/`.FMT`/`.DSP` was found under `FrameWeb/tests/data`. Whether those files still exist elsewhere is 未確認; ask the user.

### Baseline test state (2026-10-19, HEAD `4424552`)

The JRSNAP-related suites gave 140 passed in 5.68 s:

```powershell
uv --directory FrameWeb run --locked --extra dev python -m pytest tests/harness/test_jrsnap_golden.py tests/materials/test_jrsnap_support_hysteresis.py tests/solvers/test_jrsnap_supports.py tests/integration/test_jrsnap_support_mixed.py tests/io/test_jrsnap_support_input.py tests/io/test_support_law_registry.py tests/regression/test_jrsnap_support_history.py -q
```

The full gate on 2026-10-18 gave 3,484 passed, 6 failed and 4 errors.

- All 10 are in `tests/validation`: `test_mesh_convergence.py` (4 errors), `test_performance.py` (2 failed) and `test_unit_invariance.py` (4 failed).
- The cause is a `KeyError` on `node_displacements`/`metadata` when reading `model.run()` results.
- They fail identically at `639fcb4`, checked in a temporary worktree.
- `check_wiki` `examples.md / distributed-beam` fails for the same reason.
- These are not support-related and are out of scope unless the user says otherwise.

## How to run JRSNAP (verified on 2026-10-18)

- **Environment**
  - Installs: `C:\RTRI_Program\JRSNAP(Ver5.2-L03)` and `C:\RTRI_Program\JRSNAP(Ver5.3-L01体験版2)`.
  - Runner config: `JRSNAPHeadless\bin\Release\jrsnap-headless.json`, currently 5.2-L03. Use `--config <abs path>` for 5.3.
  - `SNAPDBLib.dll`: `C:\Users\sasai\Documents\structural-mcp\Batch\Aggre-SE\Headless\bin\Release\SNAPDBLib.dll`.
- **Full pipeline**: `.ndu` generation (`tools.jrsnap_golden.make_cases`), JRSNAP analysis, DB1 extraction (`tools/jrsnap_golden/extract_db1.ps1`, run in **32-bit** PowerShell `%SystemRoot%\SysWOW64\WindowsPowerShell\v1.0\powershell.exe`, which keeps full double precision), and freezing (`tools.jrsnap_golden.freeze`). Commands are in `FrameWeb/tests/data/jrsnap_golden/README.md` "再生成".
- **SNAPDBLib indexing**
  - `JointItem[k].DeltaX/Y/T` is indexed by joint number.
  - `JibanHanryoku` and `BaneFlg` are indexed by support-data row (row 1 = first BLOCK6 row).
  - `MemberItem[e]` has Mi/Ni/Si/Mj/Nj/Sj, signed as JRSNAP defines them.
- **`.ndu` rules** (built from `FrameWeb/tests/data/snap/beam001.ndu`)
  - JRSNAP 5.2 needs exactly one `JibanShogenInfo` row per element. The unmodified beam001.ndu fails with "String ' , , , , , , ' から型 'Short'".
  - `DataInfo` fields 12–15 map to flags A–D (1→0, 2→9).
  - Field 2 is 2 for load increment and 1 for displacement increment.
  - Displacement increment uses DDV=f3, JNO=f4, MDV=f5, DSMAX=f19.
  - `.ndu` files are cp932.
- **Pitfalls**
  - Write JRSNAPHeadless config JSON with Python, not bash `printf` or a heredoc (they drop backslashes).
  - In PowerShell 5.1, `$PSScriptRoot` in a param default is empty.
  - Codex consults (`.claude/skills/_shared/codex_consult.py`) returned HTTP 400 "not supported when using Codex with a ChatGPT account" for both configured models. Use a Plan subagent for independent review instead.

## Suggested order (adjust after the scope answer)

1. Read the files above:
   - the golden README;
   - `jrsnap-support-implementation.md`;
   - theory manual §7.22;
   - `tests/harness/test_jrsnap_golden.py`;
   - `tests/support/builders/jrsnap_support.py`.
2. Rerun the baseline command and confirm 140 passed.
3. Ask the user for the scope, and where any member-nonlinearity JRSNAP outputs are.
4. For each scope item, design the cases and write the tolerances with their basis before running anything. For the springs, candidates are:
   - a reversal path under displacement control;
   - a bilinear run;
   - a multi-spring or multi-member model.
5. Generate the `.ndu`, check the `.ndt` with `--analyze false`, run JRSNAP, extract, and freeze.
6. Compare with FrameWeb. Write the record rows. Pin each "matches" row in a test.
7. Run the regression gates. Update the docs. Reply to the user and ask about committing.
