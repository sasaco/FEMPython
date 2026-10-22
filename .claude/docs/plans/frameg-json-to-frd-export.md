## Implementation Plan: FrameG JSON to FRD Export

### Purpose
Add the reverse of `FrameGConverter/Convert_Manager/ConvertManager.cs`: write a FrameG `.frd` (a level-0 LZH archive of Shift-JIS text files) from any 2D FrameWeb input JSON in the format FrameWebforCS saves. FrameG can represent part of what FrameWeb can. A model that FrameG cannot represent produces an error and no output. Information lost without changing the analysis produces a warning. The converter is available as a Convert_Manager API, as an `export-frd` command of the headless runner, and as a button in the Convert_Test app.

### Scope
- New files:
  - `FrameGConverter/Convert_Manager/Lzh/LzhArchiveWriter.cs`: level-0 headers, `-lh0-`/`-lh5-`, CRC-16/ARC, terminator.
  - `FrameGConverter/Convert_Manager/Lzh/Lh5Encoder.cs`.
  - `FrameGConverter/Convert_Manager/FrameG/` (name to be settled in step 4):
    - `FrdExporter.cs`: the public API.
    - `FrdExportDiagnostics.cs`: error and warning codes, plus the JSON path of the item each one refers to.
    - `ShiftJisRecordWriter.cs`: fixed-width fields measured in Shift-JIS bytes; overflow throws.
    - One writer per FrameG file group: members (`Buzai.tmp`, `Buzai_G.tmp`), supports (`$2.txt`, `$3.txt`), springs (`B_Bane.tmp`), joints (`B_Ketugou.tmp`), notice points (`Chakumoku.tmp`), loads (`Kajyu.tmp`, `K_JituBuzai.tmp`, `Ku_Mix*.tmp`), DEFINE (`Ku_DefNo`/`Ku_Define2`), COMBINE (`Ku_Combine`/`Ku_ComNa`), PICKUP (`PickUp`/`PickUpName`), and the per-member lists (`$10`/`$17`/`$18.txt`).
  - `FrameGConverter/tools/lzh_verify.py`: an independent LZH decoder and CRC checker, standard library only, used as a test oracle in step 1.
  - `FrameGConverter/Convert_Manager/FrameG/Templates/`: embedded resources for the files the forward converter does not read: `$DatVer.TXT`, `frvlist.txt`, `KFrame.ini`, `kui_opt.txt`, `$9.txt`, `$11.txt` (the title is substituted), `$16.txt`, the `"0"` tables, and the empty pile tables. They are taken from a 4.6.3 sample without piles (`バネ連衡なし.frd`).
  - Tests:
    - `FrameGConverter/Convert_Manager.Tests/LzhArchiveWriterTests.cs`
    - `FrameGConverter/Convert_Manager.Tests/FrdRecordWriterTests.cs`
    - `FrameGConverter/Convert_Manager.Tests/FrdExporterTests.cs`
    - `FrameWebforCS/tests/FrdExportRoundTripTests.cs`
- Modified files:
  - `FrameGConverter/Convert_Manager/Convert_Manager.csproj`: embedded resources.
  - `FrameWebforCS/Headless/RunOptions.cs`, `Program.cs` and `HeadlessCalculation.cs`: add the `export-frd` command. It reuses `ReadInput`, `ValidateDocument`, `ValidateOutputDirectory` and `Publish`; `Publish` becomes internal.
  - `FrameWebforCS/Headless/tests/RunnerTests.cs`: the child-process helper takes the command as a parameter.
  - `FrameWebforCS/Headless/README.md`.
  - `FrameGConverter/Convert_Test/Form1.cs` and `Form1.Designer.cs`: JSON → FRD button, `*.json` dialog, routing dropped files by extension, warnings shown in `Form2`.
  - `FrameGConverter/README.md`.
  - `.claude/docs/DESIGN.md`: the export contract. The agent documents live under `.claude/`, and `.agents/` was removed on 2026-10-19 and must not be recreated. The design-tracker writer still targets `.agents/docs/DESIGN.md`, so edit `.claude/docs/DESIGN.md` directly, in the style of its existing entries.
  - `.claude/skills/headless-runner/SKILL.md`.
- Dependencies:
  - Newtonsoft.Json (already referenced). Convert_Manager stays netcoreapp3.1 / C# 8, because FrameWebforCS references the project (`FrameWebforCS.csproj:45`).
  - The forward readers in `Convert_Manager/FrameWebForJS/*.cs` and SevenZipExtractor serve as the test oracle.
  - `FrameWebforCS/providers/InputFileLoader.cs`, `InputDataService.GetSaveJson` and `calculation/CalculationRequest.cs` (`CalculationRequestBuilder.FromSavedJson`) are used for the round trips.
- Out of scope:
  - Using FrameG itself; the user opens the output in FrameG later.
  - An MCP tool in the parent `structural-mcp` repo.
  - Fixing the forward converter.
  - Train loads (FrameG mark 14 / `K_Ressha_S.tmp`).
  - Byte-identical round trips.

### Implementation Steps

The LZH encoder and the record layouts, which so far are only inferred, come first. Each writer is tested by reading its output back with the existing forward reader before the archive is assembled.

#### Step 1: LZH level-0 archive writer and lh5 encoder
- [ ] Write the archive the way the samples are written:
  - level-0 headers (`hsize = 25 + name length`, checksum byte, sizes in little-endian, the time field in Unix epoch seconds, attribute 0, ASCII name, CRC-16/ARC, extension bytes `20 00 00`);
  - a single `0x00` terminator;
  - `-lh0-` when compression does not shrink the file.
  - The timestamp can be supplied by the caller, so the output is deterministic.
- [ ] Implement the `-lh5-` encoder: 8 KiB window LZSS, then Huffman-coded blocks with the C/P/T tables. Blocks of more than 65535 symbols are split.
**Verification**:
- Encode, then decode with SevenZipExtractor (`SevenZipFormat.Lzh`, the reader the forward converter uses). Check the bytes and CRC for empty, 1-byte, highly repetitive, random and multi-block inputs, and for every entry of the four sample archives after re-packing them.
- Check the headers and the CRC directly, without relying on SevenZipExtractor: a small header reader in the test project parses every level-0 header (size, checksum byte, method, sizes, name, CRC). The test computes CRC-16/ARC from the original bytes and compares it with the header. Another test corrupts one byte of a packed stream and checks that SevenZipExtractor reports the error.
- The header test also checks that the time field holds the supplied time as Unix epoch seconds, exactly as in the samples, and not as a DOS date and time.
- Decode with an independent decoder as well. Add the investigation's decoder as `FrameGConverter/tools/lzh_verify.py`, standard library only. It parses level-0 headers, decodes `-lh0-`/`-lh5-` and checks CRCs. Run it from the repository root with `uv run --project FrameWeb --locked --extra dev python FrameGConverter/tools/lzh_verify.py <archive>` on the sample archives and on the exported archives.

#### Step 2: Shift-JIS fixed-width writer and the inferred record layouts
- [ ] Add a field writer: Shift-JIS byte width, left-aligned with space padding, the sign-space number style seen in the samples (`" 1.0000"`), invariant culture, and an exception that names the field when a value does not fit. Do not silently truncate. Use a strict encoder that rejects characters with no Shift-JIS form.
- [ ] Settle how each file is framed, as the samples show it:
  - `$N.txt`: a count line, then tab-separated rows; every line ends in CRLF, including the last.
  - `.tmp`: fixed-length records with no separator, no final newline and no EOF byte. The byte offsets below count from the start of each record.
- [ ] Settle the record layouts that are only inferred:
  - `Buzai.tmp`: 244 bytes, with the member length at bytes 234-243.
  - `Buzai_G.tmp`: 56 bytes, matched to members by position, J-end length before I-end length.
  - `Kajyu.tmp`: 88 bytes, with the Int16 at bytes 71-72 written as `00 00` and a blank train-load block.
  - `K_JituBuzai.tmp`: 124 bytes, one member load and one node load per record, a blank record at the end of each case, the letter code for negative `m2` (the inverse of `load.cs:566-591`), and the 5-byte tail on even cases.
  - `Chakumoku.tmp`: 1053 bytes, with blank labels.
- [ ] Each layout gets a test that runs the matching forward reader on a hand-made record and checks it under both `ja-JP` and `en-US` current cultures. Some forward readers parse with `Convert.ToDouble`, which depends on the culture.
**Verification**:
- `dotnet test FrameGConverter/Convert_Manager.Tests -c Release --filter FrdRecordWriter`: the forward reader recovers every value, and every record has the expected byte length.
- Overflowing names and numbers raise the field error.
- For at least one file of each kind, the whole file is compared byte for byte with a file built by hand from the sample.

#### Step 3: Templates and the archive file list
- [ ] Compare the three 4.6.3 samples and split their files into four groups: template, title-substituted, generated from the model, and pile-only.
  - Embed the template files.
  - Generate `$10/$17/$18.txt` from the member list.
  - Write the pile tables (`$4-$8`, `$14`, `$20-$23`, `$0_kuiNum`) as the empty or `"0"` form used by the sample without piles.
  - Omit `$101/$102/$1021.TXT` (the suspected mesh cache), unless the comparison shows they are required.
- [ ] Settle the entry order as the reverse of [`KFrame.ini`, `frvlist.txt`, `kui_opt.txt`, `$*` in ordinal upper-case order, `*.tmp` in ordinal upper-case order].
**Verification**:
- A test pins the file list and order against `バネ連衡なし.frd`.
- A test checks that replacing the title in `$11.txt` keeps every other line unchanged and fits the field in bytes.

#### Step 4: Normalization, pre-checks and the diagnostics contract
- [ ] Read the saved FrameWeb JSON with Newtonsoft `JObject`.
  - A missing `dimension` means 3, which is an error.
  - Load cases may use `m1/m2` or `m`.
  - Notice-point rows may start at 0 or 1, so ConvertManager's own output is accepted too.
- [ ] Define `FrdExportResult`: the `.frd` bytes, a list of diagnostics `{severity, code, jsonPath, message}`, and the mapping of renumbered IDs. If any diagnostic is an error, no bytes are returned.
- [ ] These are **errors**:
  - 3D, `shell`, a node load with a negative `n` (imposed displacement), an `LL` moving load, and `direction` z/gz/r on marks 1/2;
  - spatial loads (`CalculationRequest.cs:33`):
    - `bridge_loads` that is not empty. "Empty" is the shape a normal save writes: `version`, empty `panels` and `paths` arrays, and an empty `cases` object (`InputBridgeLoadService.cs:163`). A test covers that shape;
    - `spatial_loads` at the top level and `load.<case>.load_inf`, or anything the projection would turn into spatial loads. Each error names its JSON path;
  - every other top-level key that is not on the list of known sections. The known sections are `dimension`, `node`, `member`, `element`, `rigid`, `fix_node`, `fix_member`, `joint`, `notice_points`, `load`, `define`, `combine`, `pickup`, `shell`, `bridge_loads`, `load_intensity_layout`, `three`, `result` and `resultDimension`; the list is checked against `InputDataService.GetSaveJson` (`InputDataService.cs:373-418`) during the step;
  - members that share a material but have different E or Xp in some sheet, or a material whose E or Xp differs between sheets;
  - more than 6 material sheets, more than 30 DEFINE columns, more than 30 COMBINE terms, more than 100 PICKUP columns, or more than 50 notice points on one member;
  - numbers that overflow their field (999 for 3-byte IDs);
  - `fix_member` rows with `length`: pile interval springs. They stay outside this change; see Risks & Considerations;
  - broken references and values that are not finite.
- [ ] These are **warnings**:
  - renumbering of nodes and members;
  - fields dropped because they do not affect a 2D analysis (`z`, `cg`, G/J/Iy, 3D degrees of freedom);
  - DEFINE names, the `three`, `result` and `resultDimension` sections, and `load_intensity_layout`.
- [ ] Record the contract in `DESIGN.md`.
**Verification**:
- `FrdExporterTests`: one test per error and per warning code, asserting the code and the JSON path.
- Every error case returns no bytes.

#### Step 5: Writers for nodes, members, materials, supports, springs, joints, rigid zones and notice points
- [ ] Renumber nodes and members to contiguous IDs from 1, in their original numeric order, only when they are not already contiguous. Update every reference. `Buzai_G.tmp` follows the same member order.
- [ ] Expand shared materials to one material per member, with `n` blank so the forward reader does not multiply A and Iz again. Copy E and Xp from the material, and A and Iz per sheet.
- [ ] Write a FrameG material mark (C/G/H/A1) by a fixed rule, decided in this step and recorded in `DESIGN.md`.
- [ ] Look up the A and I of a rigid zone from the `e` of its `rigid` row. Rigid zones whose A or I differ between sheets are an error.
- [ ] Write a support value of 1 as fixed, 0 as free, and any other value as a spring. FrameWeb uses the same meaning, so this is not an error.
**Verification**:
- Run each file through its forward reader and compare the nodes, members, materials, supports, springs, joints, rigid zones and notice points with the input, after normalizing the renumbered IDs, the 16 sheets versus 6, and whether notice-point rows start at 0 or 1.

#### Step 6: Writers for loads, DEFINE, COMBINE and PICKUP
- [ ] Gather the load-sign table into one place. It is the inverse of `load.cs:483-557`:

  | FrameWeb load | FrameG mark | FrameG direction | Sign |
  |---|---|---|---|
  | y, mark 1/2 | 1/2 | blank | negated |
  | x, mark 1/2 | 5/6 | blank | kept |
  | z, mark 11 | 11 | — | negated |
  | temperature, mark 9 | 9 | — | kept |
  | gy/gx, mark 1/2 | 1/2 | V/H | negated, the same convention as y (see Risks & Considerations) |
  | node load tx | — | — | kept |
  | node load ty, rz | — | — | negated |

- [ ] Copy the `L1`/`L2` strings unchanged, including the `-` meaning "relative to the previous row" and a negative L2 meaning the loaded width.
- [ ] Do not expand `m1/m2` ranges into single members: `K_JituBuzai.tmp` has its own m1/m2. They are member references with a sign that has its own meaning (`CalculationRequest.cs:245-258`). Map the absolute value through the member renumbering and keep the sign. Renumbering keeps the original order, so a range still covers the same members. A reference to a member that does not exist is an error.
- [ ] Pair the node load and the member load that share a `row` number into one record.
- [ ] Renumber the cases 1..N and remap the DEFINE references to match.
- [ ] Write `Ku_Mix` and `Ku_MixName` as one blank record each.
- [ ] Write the COMBINE rate as `1.0000` and blank symbols for COMBINE and PICKUP.
**Verification**:
- Run the files through the forward `load`, `define`, `combine` and `pickup` readers, and check the signs, directions, ranges, row pairing, the end of each case, and the DEFINE, COMBINE and PICKUP references.
- Test negative `m2` values (−5, −10, −14, −999) separately.
- Test node loads whose `tx`, `ty` and `rz` all have different non-zero values, and check each sign separately.
- Test member IDs with gaps together with an `m1/m2` range and a negative `m2`: after renumbering, the forward reader and `CalculationRequestBuilder` expand to the same members.

#### Step 7: Archive assembly and the public API
- [ ] Add `FrdExporter.Export(string json, FrdExportOptions options)`. The options are the title and the timestamp. The method returns `FrdExportResult`, and it never returns a partial archive.
- [ ] Callers must register `CodePagesEncodingProvider`, as they already do for `ConvertManager`. The exporter checks for it and fails clearly if it is missing.
**Verification**:
- `FrdExporterTests`: open the exported archive with `new ConvertManager(stream)` and check every entry's CRC. The JSON round trip (export, then forward-convert) matches the input after normalization.
- Exporting the same input twice with the same options gives identical bytes.

#### Step 8: Round trips through FrameWebforCS
- [ ] The samples are found the way the existing tests find them: `RepositoryRoot()` walks up the directories (`InputFileLoaderTests.cs:81`). No change to the csproj is needed.
- [ ] `FrdExportRoundTripTests`, round trip 1:
  1. Open `4_2-FrameG.frd` and `バネ連衡なし.frd` with `InputFileLoader.Open`.
  2. Save the model with `GetSaveJson`.
  3. Export it to `.frd`.
  4. Open the `.frd` again.
  5. Compare the two JSON files after normalization.
  6. Compare `CalculationRequestBuilder.FromSavedJson` for the two, which checks that the analysis input is the same.
- [ ] Round trip 2: write a 2D JSON in FrameWeb that uses features FrameG lacks (shared materials, IDs with gaps, gy loads, combine and pickup). Export it, read it back with `InputFileLoader.Open`, and compare the `CalculationRequest`.
- [ ] Check that `バネ連衡あり.frd` and `杭2こ.lzh` now fail export with the pile-spring error code, because of the decision on pile springs.
**Verification**:
- `dotnet test FrameWebforCS/tests -c Release --filter "FrdExportRoundTrip|InputFileLoader|RigidZoneConversion"` passes.
- The existing tests are not changed.

#### Step 9: Headless `export-frd` command
- [ ] Add `RunOptions.Parse` support for `export-frd --input <abs> --output-dir <abs> [--title <text>]`, with its own options record. `run` keeps its arguments unchanged.
- [ ] Publish `model.frd`, with its SHA-256, in the existing protocol v1 envelope, without changing the protocol (`Program.cs:7-23`, `Headless/README.md:115`):
  - engine `fempython`;
  - `engineeringStatus` as now: `not_checked` on success, `not_applicable` on failure;
  - each warning as an entry of the existing `messages[]`, with the code and the JSON path in the text;
  - errors as `ok: false` with `RunnerError` code `FRD_EXPORT_UNSUPPORTED`. Its `message` lists the first diagnostics (code and JSON path) and their total count.
  - Nothing is written to the output directory on error.
- [ ] Update the headless README and the `headless-runner` skill.
**Verification**:
- Add `RunnerTests` cases for:
  - success: the artifact and its SHA-256, and the warnings in `messages[]`;
  - unsupported input: the error code, the diagnostics in the message, `engineeringStatus`, and no artifacts;
  - bad arguments.
- Run the existing `run` cases as well.
- **Ask the user before building**, because `bin/Release` is shared with WebPier and 関内EVピット.

#### Step 10: Convert_Test button
- [ ] Add a "JSON → FRD" button and a `*.json` dialog to the panel, and route dropped files by extension.
- [ ] Write the `.frd` next to the input file. Show the warnings and errors in `Form2`. On error, write nothing.
**Verification**:
- `dotnet build FrameGConverter/FrameGConverter.sln -c Release` succeeds; the existing NETSDK1138 warning remains.
- Converting a JSON exported in step 8 by hand shows the warnings and produces a `.frd`. The forward button can open that `.frd` again.

#### Step 11: Hand over the FrameG check
- [ ] Write the files the user will open in FrameG to `.tmp/frd-export-check/`: the 4_2 and バネ連衡なし round trips and the JSON from round trip 2. Add a short checklist, one item per group of records written:
  - the file opens, and its title and file date are right;
  - nodes, members, material sheets and material marks;
  - supports and spring values;
  - member springs, joints and rigid zones;
  - notice points;
  - load cases, with the signs of y, x, gy/gx, temperature and node loads;
  - DEFINE, COMBINE and PICKUP;
  - the calculation runs.
**Verification**:
- The listed files exist and their SHA-256 values are recorded in the handoff.
- FrameG acceptance stays open until the user reports back.

### Risks & Considerations
- **FrameG cannot be run here.** All automated checks use the forward converter, SevenZipExtractor and FrameWebforCS. Things FrameG might do differently remain risks until step 11:
  - rejecting `-lh5-` streams from another encoder;
  - needing `$101/$102/$1021.TXT`;
  - reading the Int16 in `Kajyu.tmp` or the material mark differently.
  Keep the output close to the samples: the same methods, order, header bytes and templates.
- **The forward converter loses or misreads information**, so passing the round trip through it does not prove FrameG reads the file correctly:
  - `joint.cs:92` drops a joint row when one end is blank. The exporter always writes both ends, so it is not affected.
  - The gy/gx encoding is ambiguous.
  - Composite loads are expanded.
  - Train loads are ignored.
  Record these in `DESIGN.md` and leave the forward converter unchanged.
- **Culture.** Some forward readers parse with `Convert.ToDouble`, which follows the current culture. The writer always uses `.`, and the step 2 tests run under both cultures.
- **Renumbering.** Nodes and members are renumbered only when their IDs have gaps, and each renumbering is reported as a warning with the ID mapping. More than 999 nodes or members after renumbering is an error.
- **Shared runner exe.** Building `FrameWebforCS/Headless` overwrites the `bin/Release` exe that WebPier and 関内EVピット use. Ask the user before building.
- **Target framework.** The Convert_Manager code must stay at C# 8 / netcoreapp3.1. No records, `init`, `JsonNode` or collection expressions.
- **Stale CI.** `FrameGConverter/.github/workflows` and the `FrameGConverter/FrameGConverter` Azure Function are dead. Leave them unchanged.

- **gy/gx loads (decided in this plan; the user approves it with the plan).** The forward converter reads FrameG mark 1/2 + V/H as gy/gx with P negated, and mark 5/6 + V/H as gy/gx with P kept. One of those readings is probably wrong in FrameG.
  - The exporter writes gy/gx as mark 1/2 + V/H with P negated, the same convention as y. Both encodings pass the round trip through the forward converter, so only FrameG can tell which one is right.
  - The user checks the sign in FrameG in step 11. If it is wrong, the change is one row of the step 6 sign table.
- **Pile interval springs (`fix_member` with `length`) are outside this change (decided in this plan).** FrameG represents them only on members whose name contains 杭, through one shared interval table (`$4/$5/$6`) and the pile grids (`$7/$8/$14/$20-$23`, `$N_kuiNum`), whose meaning is not known.
  - They are an error in this change. Pile export becomes a separate task once the meaning of the grid files is known.
  - Consequence: the two pile samples (`バネ連衡あり.frd`, `杭2こ.lzh`) cannot make the .frd → .json → .frd round trip.

### Open Questions
None of these blocks implementation.
- **Material mark C/G/H/A1 (resolved in step 5; the user confirms in FrameG).** Which mark the exporter writes does not change FrameWeb's analysis. Whether it changes FrameG's checks or printouts is unknown.
- **Version of the templates.** The templates follow FrameG 4.6.3 (`$DatVer` 4.0603). Confirm in step 11 that the user's FrameG opens a file of that version.
