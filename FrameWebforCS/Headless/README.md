# FrameWebforCS.Headless

One noninteractive Windows x64 process performs the same saved-document request
projection, Python calculation, strict result validation, DEFINE/COMBINE/PICKUP
aggregation, PIK/node CSV formatting, and PDF projection/composition as FrameWebforCS.
No application window, message loop, web server, or browser is started.

## Build and runtime

Run from the FEMPython repository root:

```powershell
uv --directory FrameWeb sync --locked --extra dev
dotnet build FrameWebforCS/Headless/FrameWebforCS.Headless.csproj -c Release
dotnet test FrameWebforCS/Headless/tests/FrameWebforCS.Headless.Tests.csproj -c Release
```

The executable is
`FrameWebforCS/Headless/bin/Release/net10.0-windows/FrameWebforCS.Headless.exe`.
It requires .NET 10 Desktop Runtime x64 and the existing `FrameWeb/.venv` Python
environment. The existing `PythonEnvironment` discovers the FEMPython root by
walking upward from the executable directory, reads `.venv/pyvenv.cfg`, and loads
`FrameWeb/src`. Keep this repository layout when running the development build;
copying only the executable elsewhere is unsupported. No extra Python environment
variables are required. The project currently references the desktop assembly to
share its implementation, including its existing build dependencies.

## Command

```powershell
& FrameWebforCS/Headless/bin/Release/net10.0-windows/FrameWebforCS.Headless.exe run `
  --input 'C:\jobs\input.json' `
  --output-dir 'C:\jobs\output' `
  --generate-pdf true `
  --generate-pik true `
  --pdf-sections input,section_force,pickup_section_force,displacement,pickup_displacement
```

Both paths must be absolute. The output directory must be absent or empty and
must not traverse a symlink/junction. The runner never overwrites an existing
output. `--generate-pdf` and `--generate-pik` default to `true`; section selection defaults to
all five names shown above. Optional `--generate-pickup-displacement-csv` and
`--generate-pickup-reaction-csv` default to `false`. Flags accept exactly `true` or `false`; repeated and
unknown options/sections fail. PDF sections may be any nonempty distinct subset
of those five names. Sections are rendered in the existing PDF composer's order.

To request only the calculation result and both node PICKUP CSVs (2D or 3D):

```powershell
& FrameWebforCS/Headless/bin/Release/net10.0-windows/FrameWebforCS.Headless.exe run `
  --input 'C:\jobs\input.json' --output-dir 'C:\jobs\output' `
  --generate-pdf false --generate-pik false `
  --generate-pickup-displacement-csv true --generate-pickup-reaction-csv true
```

The node CSV flags are independent: either can be requested alone or together,
and both can accompany PIK/PDF. They reuse `PickupNodeExportFormatter`; a node
export does not require member section-force results. Missing or inconsistent
data for a requested quantity fails the entire job before publication.

Input is the complete FrameWebforCS saved JSON document, encoded as UTF-8 (an
optional UTF-8 BOM is accepted). Saved calculation results are ignored and every
request is recalculated. Duplicate JSON properties, malformed documents, missing
PICKUP definitions when requested, and unresolved derived-result references fail
before output publication. A `.pik` requires dimension 2; for dimension 3, use
`--generate-pik false`. The existing 3D CSV formatter is not exposed as `.pik`.
Saved load cases with no effective load rows retain the desktop behavior: the
request projection omits them and the derived presenter ignores their contribution.
The runner reports `EMPTY_LOAD_CASE_SKIPPED` warnings, a summary count, and the
complete `skippedLoadCases` list in `result.json`; it does not invent zero results.

## Protocol and artifacts

Stdout contains exactly one UTF-8 JSON envelope with `protocolVersion: 1`,
`engine: "fempython"`, `engineVersion`, `readiness: "experimental"`, `ok`,
`executionStatus` (`success` or `failed`), `engineeringStatus`, `summary`,
`messages`, `artifacts`, and `errors`. .NET/Python diagnostics go to stderr.
Success exits 0; any failure exits 1 and returns an empty artifact manifest.
`engineeringStatus: "not_checked"` means calculation/export completed without
certifying structural design acceptance; failure uses `not_applicable`.

| File | Condition | Media type | Contents |
|---|---|---|---|
| `result.json` | Always on success | `application/json` | `frameweb_analysis` schema 1 wrapper with dimension, canonical AnalysisResultSet v1 in `analysisResultSet`, and all derived definitions/combinations/pickups in `derived` |
| `pickup.pik` | `--generate-pik true` | `text/plain; charset=utf-8` | Existing 2D fixed-width correlated PICKUP section-force format, UTF-8 without BOM |
| `report.pdf` | `--generate-pdf true` | `application/pdf` | Existing Japanese report composer with the requested sections |
| `pickup-displacement.csv` | `--generate-pickup-displacement-csv true` | `text/csv; charset=utf-8` | Raw displacement PICKUP vectors, UTF-8 without BOM; artifact kind `pickup-displacement` |
| `pickup-reaction.csv` | `--generate-pickup-reaction-csv true` | `text/csv; charset=utf-8` | Raw support-reaction PICKUP vectors, UTF-8 without BOM; artifact kind `pickup-reaction` |

Both node CSVs use 17 columns: `pickup_id,focus_component,node_id,max_combine_id,min_combine_id`,
then six `max_*` and six `min_*` unit-bearing component columns. Displacement
components are `dx,dy,dz,rx,ry,rz`; reaction components are `fx,fy,fz,mx,my,mz`.
Translation uses the result length unit, rotation `rad`, force the result force
unit, and moment `force*length`. Values retain raw result units and round-trip
precision; they are not the rounded mm/mmrad display values. 2D emits three focus
modes (`dx,dy,rz` or `fx,fy,mz`) and 3D emits all six; each row keeps both complete
six-component vectors and their selected COMBINE IDs. Rows follow PICKUP order,
focus order, then topology node order; reactions include only reaction nodes.

Every artifact entry contains `kind`, `mediaType`, `relativePath`, `bytes`, and a
lowercase hexadecimal SHA-256 `sha256`. All files are generated and validated
before staging/publication. Consumers must only use artifacts from a successful
envelope and validate their size/hash. The MCP server can accept client-read JSON
and return these artifacts; the client then saves them at its chosen paths.

Input is limited to 32 MiB, each artifact to 128 MiB, and a run to ten minutes.
The existing PDF generator also limits its projected input to 64 MiB and output
to 1,000 pages. There is no additional analysis-case-count cap. Cancellation is
checked at stage boundaries and within PDF generation. A native CPython solve
cannot be interrupted safely in process: the runner returns cancellation and
exits, while an MCP host should also enforce its own process deadline.
Force-terminating a process can leave its private job directory; a failed or
missing success envelope never authorizes publication of those files.

## Verification

The focused suite invokes real child processes and checks default/disabled
exports, fresh results despite a stale saved result, a known axial displacement,
PIK/PDF signatures, artifact sizes/hashes, malformed options/JSON, 3D PIK
rejection, missing derived terms, and preservation of existing output files.
Node CSV tests cover 2D/3D, separate/combined exports, full correlated vectors and
COMBINE IDs against the fresh result, UTF-8 without BOM, units, ordering, optional
defaults, shell models without member results, and no publication on CSV failure.
The real transverse-direction client model was also calculated using a private
repository job directory: 36 topology nodes, 11 members, 9 load cases, 5 pickups;
both requested export formats were generated. Client source files and requested
final destination files are not modified by tests.
