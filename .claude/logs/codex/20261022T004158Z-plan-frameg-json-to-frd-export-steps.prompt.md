Objective: Decompose this change into an ordered, independently testable implementation plan. Read-only: do not edit files.

Repository root: this working directory (FEMPython). Agent docs live under `.claude/` (not `.agents/`).

## Purpose
`FrameGConverter/Convert_Manager/ConvertManager.cs` converts a FrameG `.frd` (LZH archive of Shift-JIS text files) into FrameWeb input JSON. Build the REVERSE: an arbitrary 2D FrameWeb input JSON (the format FrameWebforCS saves) -> a `.frd` that FrameG can open.

## Decided by the user
- Expose as: (a) a public API in Convert_Manager, (b) a headless runner / CLI command (FrameWebforCS/Headless, e.g. an `export-frd` subcommand), (c) a reverse-convert button in the Convert_Test WinForms app.
- Target input: any 2D FrameWeb JSON. What FrameG can represent is written; what it cannot is an error or a warning. A round trip .frd -> .json -> .frd need only be semantically equal, not byte-equal.
- LZH: implement an -lh5- encoder ourselves (no .NET library writes LZH; SevenZipExtractor only extracts).
- FrameG itself is NOT available to us; the user will open the output in FrameG later. All automated verification must rely on the existing forward converter, the sample archives, and the FrameWeb engine.

## Current state (from investigation; verify paths yourself)
- Convert_Manager: netcoreapp3.1, C# 8, Newtonsoft 13.0.1, SevenZipExtractor 1.0.17. Referenced by FrameWebforCS.csproj:45 (net10.0-windows) and so by Headless. Tests: `FrameGConverter/Convert_Manager.Tests` (net10.0 xunit, links Convert_Test/TestData/**), FrameWebforCS/tests/InputFileLoaderTests.cs, RigidZoneConversionTests.cs. `FrameGConverter/FrameGConverter/` (Azure Function) and `.github/workflows` are dead.
- Sample archives: `FrameGConverter/Convert_Test/TestData/4_2-FrameG.frd` (FrameG 4.6.2), `バネ連衡あり.frd`, `バネ連衡なし.frd`, `杭2こ.lzh` (4.6.3, with pile springs). `TestData/wdata/` is NOT the extraction of 4_2.
- LZH facts from samples: every entry header level 0 (hsize = 25 + namelen, checksum byte, method, packed/orig size LE, time field = Unix epoch seconds, attr 0, level 0, ASCII name, CRC-16/ARC, 3 extension bytes `20 00 00`); -lh5- single block for most files, -lh0- for tiny files; archive ends with one 0x00; entry order = reverse of [KFrame.ini, frvlist.txt, kui_opt.txt, `$*` sorted, `*.tmp` sorted].
- File formats (forward readers in `Convert_Manager/FrameWebForJS/*.cs`): `$N.txt` CRLF tab tables with a count first line; `.tmp` fixed-width Shift-JIS byte records with no separators (Buzai.tmp 244 B incl. member length at 234-243; Buzai_G.tmp 56 B positional by member order; B_Bane 323 B; B_Ketugou 99 B; Chakumoku 1053 B incl. labels; Kajyu 88 B incl. binary Int16 at 71-72 and a train block; K_JituBuzai 124 B with one member load + one node load per record, blank record per case end, negative m2 letter-encoded in load.cs:566-591; Ku_DefNo/Ku_Define2; Ku_Combine/Ku_ComNa; PickUp/PickUpName; Ku_Mix/Ku_MixName). Unread files: $DatVer.TXT, frvlist.txt (INI), KFrame.ini, kui_opt.txt, $9/$11(title at line 4)/$16 GUI settings, $10/$17/$18 per-member lists, $12/$13/$15/$19 = "0", pile grids $7/$8/$14/$20-$23/$N_kuiNum, $101/$102/$1021 (mesh cache?), K_Ressha_S.tmp (train settings).
- Forward mappings that make the reverse ambiguous or lossy: one E/Xp per member (material = member number; FrameWeb shares `e` and allows E per sheet, max 6 sheets); A,Iz multiplied by n; rigid zones positional per member with single A/I; pile interval springs only for members named 杭 with ONE shared interval table ($4/$5/$6) and tip springs merged into fix_node; load mark/sign table (FrameG 1/2 -> y,gy,gx with P negated; 5/6 -> x mark1/2, gy/gx unchanged; 11 -> z negated; 9 temperature; node ty, rz negated) so gy/gx have two encodings with opposite signs; composite loads (Ku_Mix) expanded; fix_node spring == 1 indistinguishable from fixed; joint.cs:92 uses `&&` so pinned-one-end rows are dropped on import (forward bug); field widths: member/node/load numbers 3 bytes, names 32/40/41/46 bytes, define 30 cols, combine 30 terms, pickup 100 cols; forward parsers use culture-sensitive Convert.ToDouble.
- FrameWeb JSON side: no JSON Schema; format defined by FrameWebforCS `providers/InputDataService.cs:238-418`, `components/input/Input*Service.cs`, `calculation/CalculationRequest.cs` (2D projection `Complete2D` :452-485, member-load m1/m2 expansion :245-372, negative node n = imposed displacement :160-175). Saved JSON uses m1/m2, notice_points rows 1-based, element name key `n` (ConvertManager emits `name`, 0-based notice rows, up to 16 sheets, `rate`). FrameWeb-only features: 3D, shell, imposed displacement, LL moving loads with LL_pitch, directions z/gz/r on marks 1/2, shared materials, E differing per sheet, per-member partial springs.
- Headless runner `FrameWebforCS/Headless`: `RunOptions.Parse` accepts only `run`; protocol v1 envelope in Program.cs; reusable ReadInput/ValidateDocument/ValidateOutputDirectory/Publish in HeadlessCalculation.cs (Publish is private); tests in Headless/tests/RunnerTests.cs run the built exe (helper hardcodes `run`). The exe in bin/Release is shared with WebPier and another project: rebuilding needs user confirmation. Exposing it as an MCP tool in the parent repo (C:\Users\sasai\Documents\structural-mcp) is NOT requested.
- Writer patterns to reuse: `FrameWebforCS/calculation/PickupExportFormatter.cs` FixedField (throws on overflow) / FixedNumber; Shift-JIS byte-aware cutting only exists for reading (`comon.cs`); newer converter code throws InvalidDataException with file and item; legacy code sets `message` warnings that Convert_Test Form2 displays.
- Rules: failing test first (`.claude/rules/testing.md`); keep contracts; record design decisions in `.claude/docs/DESIGN.md`; Convert_Manager stays netcoreapp3.1/C# 8.

## Constraints for the plan
- Order steps by dependency; put high-risk/high-uncertainty steps first (LZH encoder, the per-file record writers whose layout is only inferred).
- Each step independently testable; name its verification. The main oracle available is: encode -> decode with SevenZipExtractor (the same reader the forward converter uses) and CRC; per-file writer -> forward reader parse; full round trips (.frd -> json -> .frd -> json equal; FrameWeb-saved json -> .frd -> json compared to the original after normalization; optionally both JSONs through the FrameWeb engine give the same results).
- Propose how the unread template files are produced (embedded template from a 4.6.3 sample with title substitution?) and how unsupported/lossy features are classified (error vs warning) and reported through all three front ends.
- Propose the policy for: member/node renumbering to contiguous ids (Buzai_G is positional), gy/gx sign encoding, pile interval springs, materials shared across members, and whether the forward joint.cs bug should be fixed in this change.
- Keep steps small; do not over-detail work that will be re-decided during implementation.

Output format:
## Implementation Steps (ordered by dependency)
## Verification per Step
## Risks and Mitigations
## Open Questions
