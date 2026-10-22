## Validation Result (PASS / NEEDS_REVISION)

**NEEDS_REVISION.** The plan addresses most prior findings, but it still needs explicit handling for result-bearing saved JSON and reproducible independent LZH verification.

## Missing Coverage

- **`resultDimension` is not accounted for.** FrameWeb’s saved JSON adds `resultDimension` when it saves results. The plan warns on `result` but does not name `resultDimension`; because unknown top-level keys are errors, a valid result-bearing save could be rejected. Add it to the recognized keys and define whether it is dropped with a warning. [InputDataService.cs](C:/Users/sasai/Documents/structural-mcp/FEMPython/FrameWebforCS/providers/InputDataService.cs:412) [Plan](C:/Users/sasai/Documents/structural-mcp/FEMPython/.claude/docs/plans/frameg-json-to-frd-export.md:87)

- **“Empty `bridge_loads`” needs a precise definition and test.** A normal save serializes it with `version`, empty `panels` and `paths` arrays, and an empty `cases` object. The plan accepts empty sections, but should explicitly test this canonical shape so the metadata does not make it look nonempty. Also specify the JSON paths for legacy spatial-load forms. [InputBridgeLoadService.cs](C:/Users/sasai/Documents/structural-mcp/FEMPython/FrameWebforCS/components/input/InputBridgeLoadService.cs:163) [Plan](C:/Users/sasai/Documents/structural-mcp/FEMPython/.claude/docs/plans/frameg-json-to-frd-export.md:87)

- **The independent `lzh.py` check is still not reproducible.** The plan says the decoder stays outside the repository and cannot be run again. Record its source/version, script hash, exact command, and output in durable evidence, or retain a redistributable verifier with the tests. [Plan](C:/Users/sasai/Documents/structural-mcp/FEMPython/.claude/docs/plans/frameg-json-to-frd-export.md:52)

- **Pin the timestamp representation.** The plan specifies Unix epoch seconds, while conventional level-0 LHA readers interpret the timestamp as DOS time/date. The sample header appears to use a different representation, so the plan may be intentionally matching FrameG’s files; however, the direct header test omits the timestamp and the FrameG checklist does not check it. Assert the raw sample-compatible encoding and include a FrameG date check. [libarchive’s level-0 reader](https://fuchsia.googlesource.com/third_party/libarchive/%2B/872a761591806bbbf069e057d9dc0301f338f08d/libarchive/archive_read_support_format_lha.c) [Plan](C:/Users/sasai/Documents/structural-mcp/FEMPython/.claude/docs/plans/frameg-json-to-frd-export.md:47)

- **Specify generated text-file framing.** Step 2 gives fixed record widths but not the record separator, final newline, or whether stated byte offsets exclude separators. Define these and assert complete file bytes for deterministic output.

## Ordering Problems

The overall dependency order is sound: encoding and record layouts precede archive assembly, followed by round trips and the two integrations. Add the timestamp and text-file framing checks before Step 7, so the archive tests lock down the format before it is consumed by later steps.

## Integration Risks

The earlier findings are mostly resolved in the plan: it adds spatial-load rejection, signed `m1/m2` remapping tests, direct CRC checks, Headless protocol mapping, and a FrameG checklist. It explicitly decides the `gy/gx` encoding and treats pile interval springs as unsupported. I verified that `RepositoryRoot()` walks upward until it finds `FrameGConverter`, so the round-trip tests do not need a `.csproj` change. [InputFileLoaderTests.cs](C:/Users/sasai/Documents/structural-mcp/FEMPython/FrameWebforCS/tests/InputFileLoaderTests.cs:81)

The spatial-load rule still needs exact JSON paths and the canonical empty shape noted above. The listed Open Questions are not implementation blockers: material-mark and template-version choices have defaults and are checked at handoff, while the `gy/gx` decision and pile-spring exclusion are explicit.

The supplied repository contract points to `.agents/rules/`, `.agents/STATE.md`, and `.agents/skills/context-loader/`, which are absent from this checkout; I used the supplied `AGENTS.md` and inspected the relevant source files. No files were edited and no tests were run.

## Revised Steps (if NEEDS_REVISION)

1. In Step 1, make the header test assert the supplied timestamp’s exact sample-compatible bytes and add the date to the FrameG handoff checklist.
2. In Step 2, specify record offsets and framing, including CRLF and trailing-newline behavior, and test whole-file bytes.
3. In Step 4, list the full accepted top-level key set, including optional `resultDimension`; define semantic emptiness for canonical `bridge_loads`; and identify legacy spatial-load JSON paths.
4. Preserve reproducible evidence for the independent LZH decoder: source/version, script hash, command, and result.
