# Handoff: JRSNAP互換 バイリニア・トリリニア支持ばね（FrameWeb）

Date: 2026-10-18
Branch: `sasa/nonli`（HEAD `639fcb4`、Steps 1–8 are uncommitted on top of it）
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

- **Steps 0–8 are implemented (2026-10-18), uncommitted.** Ask the user before committing. The record is `FrameWeb/docs/report/jrsnap-support-implementation.md`; the plan's checkboxes are ticked.
  - Law: `src/fem/nonlinear/hysteresis/jrsnap_support.py`. Registry: `src/fem/nonlinear/support_laws.py`. Generic API: `FemModel.add_nonlinear_spring_support()`.
  - Theory manual §7.22 (and the TOC), Wiki (data-structures, nonlinear-analysis, python-api, file-formats, results), capabilities (`verified`), test ledgers and the example `docs/examples/jrsnap-trilinear-support.json` are written.
  - New tests: 6 files, 126 cases. The JRSNAP goldens match: positive-side force to rel 1e-12 and the segments; the case 03 global displacement-control solution within 1e-4 kN, both spring-only and with a 2D rigid member.
  - Slip is unchanged: the example hash `ee070fb7…`, `support_models==['slip_v1']`, and the existing slip test files pass unmodified. Slip still resolves `evaluate_slip` through `support_springs`, because one existing test monkeypatches it.
- Full gate (`check_test_results`): 3,484 passed, 6 failed, 4 errors. All 10 are in `tests/validation` (mesh_convergence, performance, unit_invariance): a `KeyError` on `node_displacements`/`metadata` from `model.run()` results. They fail identically at HEAD `639fcb4`, as does `check_wiki` `examples.md / distributed-beam`. This is pre-existing and unrelated to supports, and has not been fixed.
- One hand-table row was settled during implementation: the second `.02` is `plateau_pos` with tangent 0, because D+ = Q+(.02) = 12 and the cap is flat beyond it. The forces are as planned.

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

1. Review the diff (`git status`; all changes are under `FrameWeb/`) and ask the user whether to commit.
2. **Optional follow-ups** (plan Open Questions 1, 3, 4):
   - ask RTRI about the negative-side F_neg(2|dx|) and −RMiT1=0;
   - add direct `.ndt` input to JRSNAPHeadless to test convergence flag F=1;
   - add a JRSNAP run with reversals, if one becomes available.
