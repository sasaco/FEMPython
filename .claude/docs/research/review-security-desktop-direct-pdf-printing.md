# Security Review: Desktop Direct PDF Printing

## Scope and evidence

Reviewed the scoped patch and current implementations of `DirectPdfGenerator`, `PrintInput`, `Printing/PdfDocument`, `PrintBase3dDiagram`, `PrintProjection`, the WinForms modal, and the active desktop package graph. `dotnet list FrameWebforCS/FrameWebforCS.csproj package --vulnerable --include-transitive --no-restore` reported no vulnerable packages from the configured sources. This is a code and dependency review; printer behavior and the reported modal assertion are being handled by the implementation team.

## Findings

### Medium — The page and cancellation limits do not bound composition

- **Location:** `FramePrintPDF/PDF_Manager/DirectPdfGenerator.cs:27`, `FramePrintPDF/PDF_Manager/Printing/PdfDocument.cs:154-157,191-207`, `FramePrintPDF/PDF_Manager/PrintInput.cs:77-93,369,380-381`.
- **Evidence:** The generator passes `MaxPages = 1000` only to `GetPDFBytes`. `PrintInput.printPDF` has already built all tables and diagrams by then. Every `NewPage` calls `document.AddPage()` without checking page count or cancellation. The generator checks cancellation only before and after `GetPdfBytes`, so the Cancel button cannot stop a long composition. If composition or the page-count check throws, the `document.Close()` in the serialization `finally` is never reached.
- **Impact:** A large imported document can consume unbounded composition time and memory despite the advertised 1000-page limit; repeated cancellation does not release work promptly.
- **Fix:** Propagate limits and `CancellationToken` into the legacy composition boundary. Check both before each `AddPage` and within long row/image loops. Put the whole compose-and-serialize lifecycle under a `try/finally` that closes/disposes the partially built PDF on all failures. Keep an overload for legacy callers if needed. Verify an over-limit fixture aborts before page 1001 and a canceled long job releases the document.

### Medium — Preview generations accumulate up to 128 MiB each on disk

- **Location:** `FrameWebforCS/components/print/PrintDialogForm.cs:334-337,345-365,432-449`.
- **Evidence:** Every Preview click creates a new PDF in the modal's temp directory. Successful and failed navigation paths retain every file until the modal closes; the comment at line 365 explicitly postpones deletion. The byte limit is per PDF, and the number of generations in one modal is unlimited.
- **Impact:** Repeated previews, including previews of large documents, can exhaust the user's disk. Abnormal application exit leaves the accumulated PDFs in `%TEMP%`.
- **Fix:** Keep only files still needed by WebView2 and the current preview. Prune old generations after navigation releases them, remove failed/canceled `nextPath` files, and cap retained temp bytes or generation count if WebView2 holds a file open. Add a repeated-generation test that observes bounded disk usage.

### Low — A failed temp-directory cleanup leaves printable document data behind

- **Location:** `FrameWebforCS/components/print/PrintDialogForm.cs:432-449`.
- **Evidence:** `CleanupPreviewFiles` retries deletion for approximately 15 seconds, then writes a trace and stops. There is no cleanup at the next app startup. PDFs can contain the full model and calculation results. The directory has a random name but no explicit retention policy after process crash or a long WebView2 file lock.
- **Fix:** Clean orphaned app-owned print directories on startup or next print session using a strict name/age check, and consider a longer deferred cleanup for locked files. Keep cleanup confined to the app-owned temp namespace.

## Other observations

The generated diagram path validates Base64 byte length and decoded pixel count before image loading. The projection uses typed JSON construction and validates print options, scales, component names, and captured PNG headers. The temp PDF path is built from a random file name inside an app-created directory; the Save operation creates a random same-directory temporary file and replaces the chosen destination via `File.Move`.

## Conclusion

The initial review found no Critical or High findings, two Medium findings, and one Low finding. The remediation verification below closes all three.

## Remediation verification (2026-09-30)

- **Medium page/cancellation/lifecycle — resolved.** `DirectPdfGenerator.cs:24-33` now waits on a cancellable `SemaphoreSlim` and passes the token and page limit into `PrintInput`. `PdfDocument.cs:177-184` checks cancellation and the 1000-page ceiling before `AddPage`; table, text, and 3D diagram loops contain cancellation checkpoints. `PrintInput.cs:633-637` disposes a partially composed document on exceptions, while `PrintInput.cs:71-75` disposes the successful document after serialization. The over-page, output-limit, and in-flight cancellation tests check `ActiveDocumentCount` returns to baseline.
- **Medium preview accumulation — resolved.** `PrintDialogForm.cs:383-401` prunes inactive PDFs and rejects another generation once two retained PDFs or 256 MiB would be exceeded. `DisplayAsync` prunes after success and failure. The capacity test covers a locked retired file and subsequent deletion.
- **Low orphan cleanup — resolved with a one-day retention window.** `PrintDialogForm.cs:489-531` scans at modal construction and deletes old app-named directories only after acquiring their exclusive lease and rejecting unexpected entries. The active-session and orphan tests cover this boundary. Crash remnants may remain until the next print session after one day; they are no longer indefinitely retained by normal reuse.

Verification: `dotnet test FramePrintPDF/PDF_Manager.Tests/PDF_Manager.Tests.csproj --no-restore -v quiet` passed 10/10; `dotnet test FrameWebforCS/tests/PrintUi.Tests/PrintUi.Tests.csproj --no-restore -v quiet` passed 7/7. Both emitted existing WindowsBase version-conflict warnings; no test failures. `git diff --check` returned exit 0. Final finding count: Critical 0 / High 0 / Medium 0 / Low 0 for this focused review.
