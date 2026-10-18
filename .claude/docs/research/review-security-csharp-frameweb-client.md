# Security Review: C# FrameWeb Client Screen-Composition Patch

## Verdict

**CHANGES REQUESTED** — Critical: 0, High: 0, Medium: 1, Low: 3.

The new screen-composition layer preserves the typed project/result/printing
boundaries and introduces no credential, raw-command, raw-PDF, or vulnerable
package path. One file-open resource-boundary defect should be fixed before
merge. Three local hardening/lifecycle findings remain Low.

The retained `FramePrintAzure` / legacy local print host remains outside this
patch's desktop boundary. Its accepted baseline (High: 2, Medium: 2) and
completed-application redistribution **NO-GO** are inherited and are not
included in the counts above.

## Scope and Evidence

- Reviewed all 69 patch entries in
  `.agents/logs/review-diff-csharp-frameweb-client.patch` (1,172,822 bytes;
  SHA-256 `AA4449ABC4A8CA900C2629E12BBB815E6A7B18A32725096D6543F182DB91A42C`),
  plus the current project store, strict JSON contract, print exporter, shell
  lifecycle, and UI tests needed to follow the changed trust boundaries.
- Traced project New/Open/Save/Save As, preset creation, analysis publication,
  external Help launch, typed print preview/PDF export, atomic replacement,
  route/overlay disposal, delayed callbacks, and UI-thread switching.
- `PDF_Manager`'s direct/transitive vulnerable-package scan returned no known
  vulnerable packages. `git diff --check -- FramePrintPDF` exited 0 (one
  line-ending warning only).

## Findings

### [Medium] Project open allocates the complete file before enforcing the 16 MiB limit

- **Evidence:** `FramePrintPDF/PDF_Manager/Shell/MainForm.cs:173-193` accepts a
  user-selected project and dispatches it to `IProjectStore`. The production
  store calls `File.ReadAllBytesAsync` at
  `FramePrintPDF/PDF_Manager.Core/Documents/JsonProjectStore.cs:31`. Only after
  that allocation does `ProjectDocumentJson.Deserialize` enforce
  `DefaultMaxJsonBytes` at
  `FramePrintPDF/PDF_Manager.Core/Documents/ProjectDocumentJson.cs:17,59-63`.
- **Impact:** a malformed or deceptively named very large local/network project
  can force allocation and I/O far beyond the documented 16 MiB budget before
  rejection, causing process memory exhaustion or prolonged unresponsive work.
  The entity/depth/duplicate-property validation is otherwise strict.
- **Recommended fix:** open a `FileStream`, reject an initial length above the
  limit, then perform an exact bounded read of at most `limit + 1` bytes so file
  growth and length races also fail before unbounded allocation. Add exact,
  `+1`, large sparse-file, growth-during-read, cancellation, and network-path
  tests.

### [Low] Help launch checks only the URI scheme, not an explicit target allow-list

- **Evidence:** the changed command handler passes the current constant Help
  URL at `FramePrintPDF/PDF_Manager/Shell/MainForm.cs:1087-1089`, while
  `OpenExternalUri` accepts an arbitrary absolute URI, validates only `https`,
  and invokes the registered shell handler at `MainForm.cs:1155-1168`. No test
  exercises rejected hosts or launch failure/cancellation.
- **Impact:** the current call site is a constant, so no immediate user-input
  injection is present. The reusable boundary nevertheless permits any HTTPS
  host if another caller is added, contrary to the plan's allow-listed typed
  launcher requirement.
- **Recommended fix:** inject a typed launcher that accepts a closed
  `SupportTarget` value and maps it to canonical URI constants. Validate exact
  scheme and IDN host (and reject credentials), and test permitted targets,
  unknown hosts, handler failure, and user cancellation.

### [Low] Atomic project/PDF commits are not anchored to parent-directory identity

- **Evidence:** PDF export resolves destination and sibling temporary paths as
  strings at `FramePrintPDF/PDF_Manager/Shell/MainForm.cs:793-809`, then commits
  later with `File.Replace`/`File.Move` at `MainForm.cs:835-843`. Project save
  follows the same independent path operations at
  `FramePrintPDF/PDF_Manager.Core/Documents/JsonProjectStore.cs:51-72,92-99`.
- **Impact:** normal overwrite/cancellation behavior is atomic. A same-user
  local actor able to swap an ancestor directory or reparse point between
  resolution, temporary creation, and commit could redirect the operation or
  force availability failure. No privilege escalation is demonstrated.
- **Recommended fix:** define a reparse-point policy, require and verify a
  regular parent/destination, bind operations to the verified parent identity,
  and revalidate immediately before commit. Retain same-directory random
  `CreateNew`, durable flush, and primary-exception-preserving cleanup.

### [Low] Delayed UI work can run after surface disposal or outside the STA owner

- **Evidence:** `PrintOverlayControl` posts an unconditional callback that
  requests another preview at
  `FramePrintPDF/PDF_Manager/Shell/ScreenComposition/Surfaces/PrintOverlayControl.cs:419-428`;
  it has no disposed/generation check. Separately, the shell's UI-thread
  awaiter falls back to `ThreadPool.QueueUserWorkItem` when `BeginInvoke`
  cannot use the form handle at
  `FramePrintPDF/PDF_Manager/Shell/MainForm.cs:1403-1418`, even though callers
  use this awaiter as an STA guarantee before mutating shell state.
- **Impact:** rapid overlay close/replacement or handle teardown can publish
  stale preview work, raise disposed-control failures, or resume UI-affine
  continuations on a worker thread. This is a local availability/state-integrity
  risk; no external-code execution path was found.
- **Recommended fix:** cancel/version delayed surface callbacks and return when
  disposed or no longer active. Make failed UI marshaling cancel/fail the
  operation rather than resume it on the thread pool; add close/reopen and
  handle-teardown stress tests with cross-thread checking enabled.

## Positive Controls Verified

- Project JSON is strict, depth/entity/byte bounded after input acquisition,
  rejects unknown and duplicate properties, and validates before publication.
- Presets are closed-enum typed builders; no deserialization or external asset
  input was added.
- Print selection uses typed enums and the existing bounded immutable plan.
  Preview RGB dimensions/length are checked before bitmap allocation, and
  image instances are replaced/disposed.
- PDF output retains same-directory random temporary creation, exclusive
  `CreateNew`, durable flush, atomic replace/move, and cleanup that preserves
  the primary exception.
- No hardcoded credential, token, SQL, HTML/script, raw shell argument, raw PDF
  command, or active desktop reference to the vulnerable legacy print graph was
  introduced.

## Explicit Counts

- Critical findings: 0
- High findings: 0
- Medium findings: 1
- Low findings: 3
- Hardcoded secrets or credentials: 0
- Known vulnerable packages in the typed desktop graph: 0
- New raw command/PDF/HTML injection paths: 0
- Inherited legacy-host findings outside these totals: High 2 / Medium 2
