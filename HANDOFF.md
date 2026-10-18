# Handoff: JRSNAP互換 バイリニア・トリリニア支持ばね（FrameWeb）

Date: 2026-10-18
Branch: `sasa/calc`（HEAD `6be6a2d`）
Previous handoff (different workstream, preserved byte for byte, SHA-256 `fe88928b…5ada`): `FrameWeb/docs/plans/archive/handoff-rail-transverse-print-parity-20261016.md`

## Goal

Add JRSNAP-compatible nonlinear **support springs** to the FrameWeb Python solver. Material nonlinearity and a symmetric slip support spring (theory manual 7.19) already exist. The new types are `jrsnap_bilinear` and `jrsnap_trilinear`: asymmetric force-limit skeletons equivalent to JRSNAP Ver5.x BLOCK6 nodal springs (+SP1/±RMiT1/+SP2/±RMiT2/+SP3/−SP1..3), with elasto-plastic unloading.

- **In scope (user-approved)**: solver and JSON only. That covers the law, legacy `fix_node` object cells, normalized JSON, the Python API, solver connection, metadata/capabilities, theory manual/wiki, and tests.
- **Out of scope**:
  - FrameWebforJS/FrameWebforCS UI and the structural-mcp contract
  - `fix_member` and dynamic analysis
  - gap, friction and coupled DOFs
  - JRSNAP footing rotation-spring auto-derivation (KR2/KR3) and αf

**The plan is the source of truth:** `FrameWeb/docs/plans/support-multilinear-springs.md` (revision 2).

## Current Progress

- **The plan is written and reviewed.**
  - It passes the `plan-doc` shape validator.
  - An independent reviewer approved it after two revisions; the reviewer was a Plan agent, because the Codex CLI failed (see below).
  - On 2026-10-18 the user approved Step 1 decisions (a)–(e) exactly as recommended:
    - (a) degradation memory D± (no strength recovery after softening);
    - (b) plateau at the first limit force before the opposite side's x1;
    - (c) names `jrsnap_bilinear`/`jrsnap_trilinear` and keys `K1_pos…F2_neg`; `K*_neg` defaults to the positive value; `F*_neg` is required;
    - (d) `jrsnap_bilinear` means only JRSNAP's "normal" spring (shared K1, zero slope after the limit);
    - (e) reject K=0; F1=0 means a cut-off side (trilinear also needs F2=0 and K3≤0); reject both sides cut off; no artificial stiffness.
  - Step 1's remaining work is writing the theory-manual draft (7.22) and the hand tables. The decisions themselves are closed.
- **Step 0, the JRSNAP golden, is done (uncommitted).**
  - `FrameWeb/tests/data/jrsnap_golden/` holds three cases and four frozen runs from JRSNAP 5.2-L03, with `.ndu`/`.ndt` and a README of facts, column meanings and regeneration commands.
    - `support_trilinear01`: positive side, load increment, n=100 and n=1000.
    - `support_trilinear02_neg`: negative side, load increment.
    - `support_trilinear03_softening`: +SP3=−20, displacement increment.
  - `FrameWeb/tools/jrsnap_golden/` holds:
    - `make_cases.py`, which reproduces the frozen `.ndu` byte for byte;
    - `extract_db1.ps1`, a SNAPDBLib dump that must run in 32-bit PowerShell;
    - `freeze.py`.
  - `FrameWeb/tests/harness/test_jrsnap_golden.py` passes 29 tests; `tests/harness`, `tests/io` and `tests/materials` pass 1027.
- **Steps 2–8 (implementation) have not started.** No product source has changed. `slip.py`, `equilibrium.py` and `solver.py` are untouched.

## Key Facts Learned (see golden README for numbers)

- **SNAPDBLib field indexing.** `CSNAPDB.DataRead(db1)` then `OutputInfo.StepCtrl[i].JointItem[k]`:
  - `DeltaX/Y/T` are indexed **by joint number**;
  - `JibanHanryoku` (spring force) and `BaneFlg` (segment 0/1/2, negative −1/−2) are indexed **by support-data row** (row 1 = the first BLOCK6 row);
  - `MemberItem[e]` holds Mi/Ni/Si/Mj/Nj/Sj.
- **The positive skeleton matches the plan exactly** (relative error 2e-16), including the descending K3: x1=F1/K1, x2=x1+(F2−F1)/K2, then K3.
- **JRSNAP load increments leave unbalanced force.** Convergence is judged on the stiffness ratio, so up to 1.24 kN (n=100) remains and displacements come out smaller than the skeleton inverse. Displacement increments track the skeleton exactly. So:
  - compare the law exactly with cases 01 and 03;
  - compare the global solution exactly only under displacement control (case 03, 1e-4 kN);
  - use load control as a reference only.
- **The negative side is inconsistent, and identical in 5.2-L03 and the 5.3-L01 trial.**
  - The reported spring force is F_neg(2|dx|).
  - The stored −RMiT1 reads 0.
  - `BaneFlg` switches at the correct x1/x2.
  - Do not replicate this: the negative side uses the symmetric definition and is graded by the hand tables.
- **Convergence flag F** (column 70, accuracy-first) cannot be set from `.ndu`. Testing it would need a direct `.ndt` input in JRSNAPHeadless.
- **JRSNAP rigid supports are springs of about 1e15**, so restrained displacements come out around 1e-17, not exactly 0.

## What Worked

- Running JRSNAP through `C:\Users\sasai\Documents\structural-mcp\JRSNAPHeadless\bin\Release\JRSNAP.Headless.exe run --input <ndu> --output-dir <empty> --analyze true`. It needs JRSNAP closed, uses the install configured in `jrsnap-headless.json`, and accepts `--config` for 5.3.
- Checking the `.ndt` first with `--analyze false` (no install-dir writes) before every real analysis.
- Building `.ndu` from `FrameWeb/tests/data/snap/beam001.ndu`:
  - JRSNAP 5.2 needs exactly one `JibanShogenInfo` row per element;
  - `DataInfo` fields 12–15 map to flags A–D (1→0, 2→9);
  - field 2 = 2 means load increment and 1 means displacement increment;
  - displacement increment uses DDV=f3, JNO=f4, MDV=f5, DSMAX=f19.
- Loading SNAPDBLib from `Batch/Aggre-SE/Headless/bin/Release/SNAPDBLib.dll` in `%SystemRoot%\SysWOW64\WindowsPowerShell\v1.0\powershell.exe`, which keeps full double precision.
- Running workflow scripts from git HEAD out of the scratchpad, because `.agents/` is deleted in the working tree. Do not recreate `.agents/`. For example, `validate_doc.py` came from `git show HEAD:.agents/skills/_shared/validate_doc.py` and was run with an absolute `--file`.

## What Didn't Work

- **Codex consults** via `codex_consult.py`. Both `gpt-5.6-sol` (the wrapper default) and `gpt-6-astra` (in `~/.codex/config.toml`) return HTTP 400 "not supported when using Codex with a ChatGPT account". A Plan subagent was used as the substitute reviewer.
- **Using beam001.ndu unchanged for 5.2**: it fails with "String ' , , , , , , ' から型 'Short'" because of extra `JibanShogenInfo` rows.
- **Simple secant-lag and tangent-lag models** do not reproduce JRSNAP's load-increment path (7e-3 m max error), so do not try to emulate JRSNAP's iteration.
- **Writing the JRSNAPHeadless config with bash `printf` or a heredoc** loses the backslashes. Write the JSON with Python.
- **PowerShell 5.1 `$PSScriptRoot` in a param default** is empty; resolve defaults in the script body.

## Next Steps

1. Read `FrameWeb/docs/plans/support-multilinear-springs.md` (Steps 0–8, Risks, Open Questions) and `FrameWeb/tests/data/jrsnap_golden/README.md`. Check `git status`. The golden, tools and test are untracked and the plan has uncommitted edits; ask the user before committing.
2. **Finish Step 1.**
   - Draft theory manual §7.22 "JRSNAP互換 多直線支持ばね（弾塑性）", appended after 7.21; do not renumber.
   - Include the formulas, the FrameWeb-supplied unloading rule, the negative-side note and the hand tables (16-point trilinear table already in the plan; bilinear EPP; softening to zero; cut-off; the positive-work closed loop).
3. **Step 2 (TDD).** First write `tests/materials/test_jrsnap_support_hysteresis.py` with the hand tables and the golden replay:
   - feed node-2 `dx` paths from case 01 (n100/n1000) and case 03;
   - assert force = `jiban_hanryoku` (rel 1e-12) and branch ↔ `bane_flg`;
   - for case 02, check segment switch positions only.

   Then implement `src/fem/nonlinear/hysteresis/jrsnap_support.py`. Leave `slip.py` unchanged.
4. Continue with Steps 3–8 as the plan describes:
   - Step 3: pin slip with characterization tests, then add the registry `support_laws.py`.
   - Step 4: solver tests, including case 03 global displacement-control comparison.
   - Step 5: input paths.
   - Step 6: capabilities.
   - Step 7: integration/regression.
   - Step 8: docs and full `uv --directory FrameWeb run --locked --extra dev python -m tools.validation.check_test_results`.
5. **Optional follow-ups** (plan Open Questions 3–4):
   - ask RTRI about the negative-side F_neg(2|dx|) and −RMiT1=0;
   - add direct `.ndt` input to JRSNAPHeadless to test convergence flag F=1.
