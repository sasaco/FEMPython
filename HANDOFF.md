# Handoff: bridge deck loads (橋面荷重) — design-example verification and the quadratic-time fix

Date: 2026-10-30
Branch: `main`. HEAD `1edcda8`, an automatic turn-end commit that holds the first version of the verification folder.
Previous handoff (JRSNAP equivalence), preserved byte for byte, SHA-256 `864dab51…e844`: `FrameWeb/docs/plans/archive/handoff-jrsnap-equivalence-20261019.md`

This file is the work request for a fresh session. Read it fully before acting.

> **Status (2026-10-30, second session): all "Next steps" are done.** Commit 1 (`fix(spatial_loads): …`) holds the first fix and the PDF move. The second session then did steps 2–5:
> - **2** (TDD): three more quadratic or per-point costs removed (per-point re-validation, collinear outline triangulation, boundary self-intersection sweep). 0.25 m model 145 s → 66 s, results identical (243,874 numbers, max diff 0). Six new tests in `FrameWeb/tests/spatial_loads/test_geometry.py`; spatial gate 344 passed, integration gate 161 passed.
> - **3**: runner sections `bridge_definitions`, `bridge_loads`, `bridge_audit` (no bridge-load diagram: the desktop captures it from the 3D view). Codes `BRIDGE_LOADS_REQUIRE_3D`, `BRIDGE_LOADS_REQUIRED`. Runner tests 79 passed; Release exe rebuilt. structural-mcp's `fempython_calculate` accepts all runner sections (default unchanged); `npm test` 74 passed.
> - **4**: `.claude/skills/headless-runner/SKILL.md` and structural-mcp `skills/structural-fempython/SKILL.md`.
> - **5**: recorded as 未確認 in the verification README ("設計例との差を調べたこと"): the 87.6 mm deflection matches simple-beam theory with the example's own I and Ms (88 mm); a cross beam about 0.7× as stiff reproduces Mvd and the cross-beam force but not G2–G4 influence values.
>
> - **Third round** (after the user asked for more speed, TDD): quadrature points evaluated together (`shape_values`/`natural_coordinates`/`StripCell.intensity` accept (n, 2) points; parallelograms inverted in closed form), integrands return only the touched components (`integrate_pieces` component indices and `size`), node-overlap check by sweep (`close_point_pairs`), line split no longer computes the intervals twice. 0.25 m model 66 s → 33 s (FemModel), runner 141 s → 40 s. Differences are roundoff only (max relative 2.6×10⁻¹⁴). `test_independent_audits_reject_corrupted_basis` was adapted to corrupt the basis the same way for arrays of points. Spatial gate 356 passed, integration gate 161 passed.
>
> The sections below are the original request, kept for the record.

The full Japanese record of the verification is `FrameWebforCS/tests/data/合成桁格子検証/README.md`. It covers:

- the model;
- the load cases;
- the comparison tables;
- the problems found and the fix;
- how to rebuild everything.

Read it next.

---

## Goal

1. Check that FEMPython's 3D bridge deck loads (`bridge_loads`: fixed-position line and area loads distributed to structural nodes) reproduce a published grid analysis:
   - Source: `FrameWebforCS/tests/data/合成桁格子検証/合成桁の設計例と解説.pdf`, PDF pages 36–40 (printed pages 24–28, §5.1–5.4.1).
   - Structure: a 4-girder composite I-girder bridge, span 33 m, with one load-distributing cross beam at midspan.
2. Fix the performance defect the verification exposed: bridge-load preparation that is quadratic in the triangle count.

Both are done. What is left is listed under "Next steps". It is optional and needs the user's choice.

## State of the working tree (not committed — ask the user before committing)

| Path | Change |
|---|---|
| `FrameWeb/src/fem/spatial_loads/validation.py` | Fix 1. Without a loading mesh, `prepare_panel` leaves `domain_cells = ()`; `prepare_geometry` clips each cell only against itself (`panel.domain_cells or (cell,)`) |
| `FrameWeb/src/fem/spatial_loads/geometry.py` | Fix 2. New `bounding_box_pairs` (x-sweep that keeps pairs whose bounding boxes overlap with positive width on both axes); `validate_partition` clips only those pairs |
| `FrameWeb/tests/spatial_loads/test_geometry.py` | 4 regression tests (see "Verified facts") |
| `FrameWebforCS/tests/data/合成桁格子検証/README.md` | Fix recorded; `drive.py patch` removed; PDF path updated |
| `.../scripts/drive.py` | The `patch` argument is removed (now `drive.py <out> <case json>...`) |
| `.../scripts/make-visual.mjs`, `.../合成桁格子_橋面荷重検証.json` | New: input file to open in FrameWebforCS for a visual check |
| `.../results/runner_0.25m_after_fix.json`, `.../results/time_0.25m_none_after_fix.log` | New: timings after the fix |
| `FrameWebforCS/tests/data/合成桁の設計例と解説.pdf` → `.../合成桁格子検証/合成桁の設計例と解説.pdf` | Moved by the user (94 MB). Git shows it as deleted at the old path and untracked at the new one |
| `HANDOFF.md`, `FrameWeb/docs/plans/archive/handoff-jrsnap-equivalence-20261019.md` | This handoff and the archived previous one |

## Verified facts (2026-10-30)

### Verification

These are measured, with the command and file noted.

- **Load distribution matches the example exactly.** The FEMPython audit `diagnostics.spatial_loads.resultant.z` equals hand totals for every case: −2824.2 kN pre-composite, −522.2 kN post-composite.
  - The post-composite G1 line load back-calculated from the G1 moments is 7.214 kN/m. The lever rule gives 7.218 kN/m, and the same back-calculation on the example's values gives 7.22 kN/m.
  - Source: `results/summary_*.json`, produced by `scripts/read.mjs`.
- **Section forces** (0.25 m model, no shear deformation, `results/summary_0.25m_none_femmodel.json`):
  - Ms within ±0.4%.
  - Mv (= Mvd + ML) within ±0.8%.
  - S(R) 835 / 845 against 837 / 845.
  - Mvd off by −4.9% / +4.3% (G1 / G2 midspan). The cross beam carries 61.5 kN here; the example implies 58.6 kN. The example does not state its grid assumptions. Adding cross-beam shear deformation (`SHEAR=cross`) reduces the gap to −3.1% / +2.7%.
  - The README has the full table.
- **Live load positions are an assumption.** FEMPython has no worst-position search, and the example does not give its placement. The positions used are in `scripts/live.json` and README §荷重.
- **Out of scope, unexplained:** pre-composite dead-load deflection at midspan is 87 mm here against the example's 93 mm (printed page 104). Post-composite deflection agrees (6 / 5 mm).

### The performance fix

- **Before.** Runner run time:
  - 140 triangles (2.75 m stations), 12 cases: 25 s.
  - 360 triangles (1.0 m): `CANCELED: Calculation was canceled or exceeded ten minutes.` (`results/runner_1.0m_canceled.json`).
  - 1,320 triangles (0.25 m): also canceled.
- **Profile** (cProfile, `scripts/profile_case.py`):
  - Before Fix 1, 78,400 `clip_polygon` calls in `prepare_geometry` for 140 triangles.
  - After Fix 1, 876,364 calls in `validate_partition` for 1,320 triangles.
- **After both fixes:**
  - 0.25 m model, 12 cases: runner 141 s (`results/runner_0.25m_after_fix.json`); `drive.py` (FemModel directly) 145 s.
  - Results are identical to the pre-fix results, maximum difference 0.
  - Area-load cases still take 16–21 s each (quadrature and panel preparation).
- **Regression tests**, all in `FrameWeb/tests/spatial_loads/test_geometry.py`:
  - `test_area_clipping_without_loading_mesh_scales_with_cell_count`: failed before the fix with 12,842 calls against a limit of < 400.
  - `test_partition_overlap_check_scales_with_cell_count`: failed before with 3,160 calls against < 400.
  - `test_area_pieces_without_loading_mesh_cover_each_cell_once`.
  - `test_bounding_box_pairs_match_brute_force_including_touching_boxes`: random integer triangles, so degenerate and touching boxes are covered. An earlier version of the sweep failed this test on zero-width boxes, and it was fixed.
- **Gates after the fix:**
  - `uv --directory FrameWeb run --locked --extra dev python -m pytest tests/spatial_loads -q` → **339 passed**.
  - `uv --directory FrameWeb run --locked --extra dev python -m pytest tests/integration/test_spatial_general_plane.py tests/integration/test_spatial_load_acceptance.py tests/integration/test_spatial_loads.py tests/io/test_spatial_loads.py tests/io/test_bridge_load_cases.py -q` → **161 passed**.
  - The full Python suite was **not** run. The previous handoff recorded 6 known benchmark failures unrelated to this work: `metadata` / `node_displacements` KeyErrors.
- **Why skipping pairs is safe** (reasoning, not a test): pairs whose bounding boxes are disjoint or only touch have zero true overlap. A tolerance-clipped sliver is at most about `eps × length` ≤ `eps × scale`, which is the rejection threshold. The "overlapping cell interiors" branch is not reachable by any existing test. Earlier checks reject overlapping meshes first: edge orientation and non-manifold edges, and boundary self-intersection in `validate_polygon`.

### Desktop input file

`合成桁格子_橋面荷重検証.json` (2.75 m model, 12 cases, 6 combinations named with the example's values).

- It went through `InputDataService.JsonDataOpen` → `GetSaveJson` → `CalculationRequestBuilder.FromSavedJson`, using a temporary console project in the scratchpad with assembly name `FrameWebforCS.Tests` for internals access. All counts were unchanged.
- The re-saved file solved by the runner matched the earlier results (max difference 5×10⁻¹³).
- **Nobody has opened it in the GUI yet.** Visual checking is the user's task.

### Environment notes (measured)

- **Reading the PDF.** Text is not extractable: `pdftotext` lacks the Japanese CMaps, and the OCR text layer is poor. Render pages with `uv run --with pymupdf python -I scripts/render_pages.py <pdf> <out dir> <first> <last> 2` and read the PNGs.
- **Runner** (`FrameWebforCS/Headless/bin/Release/net10.0-windows/FrameWebforCS.Headless.exe run`):
  - 3D models need `--generate-pik false`, or it fails with `PIK_REQUIRES_2D`.
  - `load_diagram` is 2D only (`LOAD_DIAGRAM_REQUIRES_2D`).
  - There is no `--pdf-sections` value for bridge-load pages.
  - The output directory must be empty or absent (`OUTPUT_NOT_EMPTY`).
  - The run is capped at 10 minutes.
  - The runner loads Python from `FrameWeb/src` at run time, so Python fixes need no rebuild.
- **Model conventions.** Girder moment = `my` of the member starting at the station, sign-flipped to sagging-positive. Combination results report sagging as negative `my`.
- **Joints.** In `joint` rows, 0 means released.
- **Members.** Legacy JSON members accept `shear_correction: true`. The shear area is `5/6 × A`, so `A = web area / (5/6)`.

## Prohibitions and rules

- Do not change expected (example) values or derive them from FEMPython output. Do not loosen the comparisons in the README.
- Do not edit or move the PDF. The user placed it.
- Ask the user before committing. The tree is sometimes auto-committed at turn end with the reply as the message; see memory `turn-end-auto-commit`.
- Windows PowerShell from the repository root. Python only through `uv --directory FrameWeb run --locked --extra dev ...` or `uv run --project FrameWeb --locked ...`.
- Do not recreate `.agents/`. Skills that say `.agents/...` now live under `.claude/`.
- Reply to the user in plain Japanese.

## Next steps (ask the user which, before planning)

1. **Commit** the fix, the tests and the README/data updates. Ask about the message and whether the 94 MB PDF move should be committed. The PDF was previously tracked at the old path.
2. **Remaining slowness** (optional): 16–21 s per area-load case at 1,320 triangles.
   - Profile with `scripts/profile_case.py` on a case file made by `scripts/onecase.mjs`.
   - Candidates seen in the last profile: quadrature (`integrate_pieces`, about 16 s in a Db case), `validate_polygon` on the long outer boundary (O(n²) segment checks), and `triangulate_simple` of the outer boundary per strip.
   - Acceptance: a counting or scaling regression test like the existing ones; spatial gates unchanged (339 / 161 passed); identical results on `models/model_0.25m_none.json`.
3. **Runner bridge-load PDF sections**: add `--pdf-sections` values for `BridgeDefinitions` / `BridgeLoads` / `BridgeLoadDiagram` / `BridgeAudit`.
   - Entry point: `FrameWebforCS/Headless/HeadlessCalculation.cs:169`.
   - `PrintProjection` already supports these options.
   - This touches the Headless contract and the structural-mcp README. Needs the user's approval.
4. **Skill notes**: add `bridge_loads` authoring to `.claude/skills/headless-runner/SKILL.md`: format, `--generate-pik false`, audit keys, the overhang technique, and the 2D-only load diagram.
5. **Open questions about the example** (only if the user wants them pursued):
   - the Mvd gap from grid stiffness assumptions;
   - the 93 vs 87 mm pre-composite deflection.

   Both need assumptions the PDF does not state. Report them as 未確認 rather than tuning the model to match.

## Out of scope unless the user adds it

- Worst-position (moving) live-load search.
- Changing bridge-load semantics, for example allowing extrapolation outside the panel.
- FrameWebforJS.
- JRSNAP work, which is in the archived handoff.
