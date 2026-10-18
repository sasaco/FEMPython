Objective: Decompose partial member spring intervals into an ordered, independently testable implementation plan.
Context:
- User wants cumulative spring intervals from member I end: a 5 m member with rows of lengths [2,2,1] occupies [0,2],[2,4],[4,5]. A blank length means whole-member spring for legacy input. Mixed blank/explicit rows is pending user clarification.
- Scope: WinForms spring editor/persistence/calculation request, Python legacy FEM preprocessing before solver assembly, original-input PDF table; assess desktop viewport and PDF load diagram consistency. No implementation now. Preserve concurrent uncommitted PDF/direct-print and viewport changes.
- Current state: InputFixMemberComponent.cs:108-125,140-150 labels column 1 but leaves it unbound; InputFixMemberService.cs:11-26 has no length, 50-54 snapshot omits it, 115-125 saves rows. CalculationRequest.cs:383-405 rebuilds spring rows without length and drops zero-stiffness rows; gap rows therefore disappear. InputDataService.cs:125-142 captures calculation snapshot; PythonCalculationRuntime.cs:203-225 sends to build_analysis_result_set.
- Actual rigid split is Python FrameWeb/src/fem/legacy_beam.py:65-70,142-150,190-207; InputRigidZoneService.cs:93-107 only serializes inputs. legacy_beam.py:180-207 sums selected spring stiffness and assigns to every generated segment. Multiple cases need union spring boundaries analogous to _all_member_loads at legacy_beam.py:16-17,71-81 because analysis_result_sets.py:56-61 rejects different topologies. Load-transfer beams reject foundations at legacy_beam.py:112-117.
- PDF InputFixMember.cs:11-57 parses only m/tx/ty/tz/tr and :79-329 prints 2D/3D tables. PrintInput.cs:95-114,397-415 gates input tables; PDF load diagrams use DiagramSpring.cs/XSpring.cs and currently draw full-member springs, but are a distinct mode. Desktop ThreeConstraintsService.cs:117-130 draws full-member springs. Existing PDF and viewport files have concurrent dirty edits.
- Tests: FrameWebforCS/tests/CalculationRequestTests.cs, FrameWeb/tests/integration/test_beam_foundation.py, test_result_projection.py, FramePrintPDF/PDF_Manager.Tests/ (currently concurrent work). Need backward compatibility, exact/full/under/over total length, zero gap, interleaved rows, rigid/load intersections, multi-case topology, print output.
Constraints:
- Order by dependencies, put high-risk/uncertain steps first, name independent verification for each.
- Explain why Python preparation is the correct segment-creation boundary despite the request saying before engine handoff; flag if literal C# pre-split is necessary.
- Preserve original public member IDs and existing saved JSON; length is additive/nullable. Do not make unapproved external component changes.
- Keep unknown mixed blank/explicit semantics as open question, not guessed behavior.
Output format:
## Implementation Steps (ordered by dependency)
## Verification per Step
## Risks and Mitigations
## Open Questions
