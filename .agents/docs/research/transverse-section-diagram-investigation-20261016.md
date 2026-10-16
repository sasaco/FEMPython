# Transverse Section Diagram Failure Investigation

Date: 2026-10-16  
Repository HEAD: `c9e7448bf592e27e0b8bd61fee948e93b30e3861`  
Private evidence: `.tmp/section-diagram-investigation/20261016T180325/`

## Finding

The current supplied input calculates successfully. Its basic section-force diagram fails in the production PDF generator with:

```text
System.FormatException: The input string '1/Static/0' was not in a correct format.
PDF_Manager.Printing.DiagramResult.printFsec(...), DiagramResult.cs:219
```

The desktop projection supplies canonical case/state keys, while the legacy diagram renderer converts them to integers. Removing this exception alone does not repair the diagrams: station identities and member-run semantics also disagree, and the resulting layout differs substantially from the supplied reference.

This investigation made no product source changes and did not overwrite the external JSON/PDF. Reproductions invoked the same production calculation/projection/PDF classes as the desktop; no GUI clicking or physical printing was performed.

## Input and reference identity

| File | Bytes | SHA-256 |
|---|---:|---|
| Current `02_pre/05_線路直角方向の計算/線路直角方向.json` | 44,097 | `0fa811b08228f32af2fdf9dcb2b4d255452653662a3d4b3d6d6c873de64255d4` |
| `03_analysis/05_線路直角方向の計算/応力図.pdf` | 76,575 | `27339edacd427c8f73a6458ffbb72101e49da728af0cdee8c038838c8c57335c` |

Both files are under `K:\レールウェイコンサルタント\26_ピット\計算書\`. The current input differs from the previous `c2798...` investigation input in exactly 22 scalar values: 11 CASE 10 load row IDs and their 11 layout references. This run uses the current external original, not the previous private corrected-input copy. Full differences are retained in `input-differences-from-previous.json`.

The reference has 30 portrait pages, each 595 x 842 pt. All pages were rendered and viewed as three contact sheets; pages 1, 2, 21 and 22 were also inspected at full rendered size. Its organization is:

| Pages | Content |
|---|---|
| 1-20 | CASE 1-10, two pages per case: moment/shear above and below on the first page, axial/displacement on the second. |
| 21-30 | PICKUP 1-5, two pages per pickup: moment/shear on the first, axial on the second; maximum/minimum envelopes share each diagram. |

The reference places the piles downward, uses a broad horizontal beam relative to pile depth, black diagram lines, blue labels, and a title/case heading on each page. It has no leading title-only page. Complete extracted text/drawing counts and page images are in `visual-reference/`.

## Fresh production reproduction

The private `harness/Investigation.csproj` references the current desktop project and calls:

`CalculationRequestBuilder.FromSavedJson` -> `PythonCalculationRuntime.CalculateAsync` -> `AnalysisResultSetJson.Deserialize` -> `CalculationDerivedPresenter.Build` -> `PrintProjection.Build` -> `DirectPdfGenerator.Generate`.

All **10 static cases** solved once, with **67 topology nodes, 11 members, and zero solver warnings**. Print selections use Japanese, A4 portrait, title `線路直角方向`, `SplitHorizontal`, default components, and unspecified X/Y scales. The selections, request, fresh canonical result, projected payloads, full exceptions and summary are retained in `output/`.

| Production selection | Result |
|---|---|
| `SectionDiagram` | `FormatException` on `1/Static/0`. |
| `SectionDiagram` + `PickupSectionDiagram` | Same exception. |
| `PickupSectionDiagram` | Serializes 11 pages, but page 1 contains only the title. Remaining pages have model frames and headings, with no force curves or numeric force labels. |
| `CombinedSectionDiagram` | Serializes 21 pages with the same missing force curves/labels and initial title-only page. |

The successful PDF serializations are not successful diagram outputs. Every pickup/combined page was checked for text and vector paths; representative pages were rendered and visually inspected.

Reproduce from the repository root in PowerShell, choosing a new output directory when preserving the evidence:

```powershell
dotnet run --project .tmp/section-diagram-investigation/20261016T180325/harness/Investigation.csproj -c Release -- .tmp/section-diagram-investigation/20261016T180325/input.json <new-output-directory>
```

## Confirmed data-contract defects

1. **Case identity:** `FrameWebforCS/providers/printing/PrintProjection.cs:444` writes `page.Key`, e.g. `1/Static/0`. `FramePrintPDF/PDF_Manager/Printing/PrintDiagram/DiagramResult.cs:219` passes it to `Convert.ToInt32`. `PrintData.cs:184` builds the child diagram payload without forwarding `fsecName`; the renderer instead uses integer-keyed load names. This is the directly observed exception.

2. **Point identity:** `PrintProjection.cs:433` writes `n = stationId`, e.g. `S0`, alongside member ID and physical distance `l`. `DiagramResult.cs:524` treats nonempty `n` as an input node ID and silently skips missing nodes at line 526. Across the 10 basic cases, **none of the 770 projected force-row station IDs is an input node ID**. PICKUP/COMBINE rows use the same incompatible convention.

3. **Polyline continuity:** all 770 basic force rows carry a nonempty member ID. `DiagramResult.cs:500` treats every such row as the start of a new member run and clears the previous point at lines 508-509. The historical renderer expects the member only at the start of a run, with blank member/node fields for intermediate points. Correct coordinates alone therefore do not provide continuous force polylines.

4. **Envelope list ownership:** `DiagramResult.cs:166-177` aliases each minimum list and appends the maximum list to it. A repair must avoid mutating these lists and must keep the two envelope paths distinct. This is a code finding; the currently missing curves prevent a meaningful visual envelope comparison.

The 2D route does not use THREE viewport capture: `PrintProjection.MakeDiagramRequests` returns without capture requests at lines 739-743. The fault lies in the desktop-to-PDF diagram boundary, not in a captured screen image.

## Diagnostic isolation without product edits

`diagnostic_payloads.py` creates private copies of the projected basic-diagram payload. These are experiments, not a proposed production adapter. All force component values remain unchanged.

| Experiment | Observed result |
|---|---|
| Replace only static case keys with their numeric case IDs; align displacement keys | PDF generation succeeds: 22 pages. First force page has 46 black frame paths, **zero force labels and zero blue force paths**. |
| Also express force points by member distance and legacy run markers | PDF generation succeeds: 22 pages. The same page has 328 drawing paths including blue force lines, and 150 numeric labels. It remains inverted, compressed horizontally and crowded. |

The first experiment proves that integer conversion is only the first defect. The second isolates point/run incompatibility and exposes the remaining presentation defects. The experimental PDFs in `diagnostics/` must not be delivered as corrected engineering reports.

## Layout and direction differences

| Finding | Evidence and cause |
|---|---|
| Piles are upward instead of downward | `PrintProjection.cs:218` supplies fixed `ver = "2.5.12"`; `PrintData.cs:268-270` uses only its presence to select the coordinate convention. `DiagramFrame.cs:113` sets `isOlderVer2 = -1`; lines 343/345 then invert the current negative-Y pile coordinates. Confirmed in generated images. |
| Beam is very narrow | Input extent is 3.012 m wide and 25.501 m high. With no explicit scale, `DiagramFrame.cs:227-239` selects the smaller fit and equalizes X/Y scales. The reference visibly uses a different horizontal/vertical display ratio. Its exact scale settings are not stored in this input and have not been recovered. |
| Leading title-only page | `PrintInput.cs:99` creates the first page; lines 324-326 unconditionally add another page whenever diagram screens exist, including diagram-only jobs. Observed in production pickup/combined PDFs. |
| Trailing basic blank page | The split loop advances `i += 2` but `DiagramResult.cs:291` checks `i == output.Count - 1`. With four components, the final check cannot succeed. The diagnostic basic PDF has 22 pages instead of the reference's 20 basic pages. |
| Title and case overlap | Reference portrait title placement in `PdfDocument.cs:209-214` leaves current Y = 88.7 pt; `Text.PrtTextLoadName` uses Y minus 22, placing CASE at 66.7 pt near the TITLE at 70.7 pt. Visible on generated page 2. |
| Later title placement is stale | `DiagramResult.cs:582` assigns `TitleArea`; page creation does not clear it, while `Text.PrtText` prefers it. Titles can appear at the previous diagram-heading position. |
| Dense labels and different colors | Projection sets every station's `dummy` to false (`PrintProjection.cs:437`), and the renderer labels every nonzero value (`DiagramResult.cs:552-558`), including tiny values formatted as zero. Diagnostic page 2 visibly overlaps many labels. Force lines are blue and labels black, unlike the supplied reference. |

## Recommended repair boundary and acceptance

Repair the diagram-specific projection/renderer contract in `FrameWebforCS/providers/printing/` and `FramePrintPDF/PDF_Manager/`. Carry case identity/display names, member/station positions, continuous runs and envelope side explicitly for diagrams. Preserve canonical case/station IDs, raw force values and shared table/export contracts. Stripping the suffix from every case key is not a general solution: nonlinear/modal state identity and arbitrary identifiers must remain distinguishable.

Then repair diagram page lifecycle, coordinate convention, separate X/Y display scales, per-page title/case/component headings, force signs and label placement. Reference matching requires actual generated-PDF checks, not only JSON projection assertions. Existing `PrintProjectionTests` primarily verify vector settings; `TransverseFoundationDiagramTests` checks the row-filter helper through reflection and does not run complete diagram rendering.

Acceptance for this supplied model should include: no exception; all 10 basic cases and 5 pickups; the reference's 20 + 10 page organization without empty pages; moment/shear/axial/displacement and both pickup envelopes present; piles downward; readable labels, correct headings and deliberate X/Y scaling; plus a separate check of numerical/sign/envelope-source agreement. A repair must test zero-force diagrams, original/end/interior stations, combined selection with tables, and multiple state identities.

No complete numerical equivalence to `応力図.pdf` is claimed in this investigation. The existing table comparisons concern different artifacts, and the current original input has changed since the previous run. The findings here establish the print failure and additional visible diagram defects; they do not justify changing solver results to match a historical PDF.
