## Implementation Plan: C# Node THREE Lifecycle

### Purpose
Port the JS application's node-owned THREE objects to `FrameWebforCS/`. Add `ThreeService.cs` as the C# counterpart of JS `three.service.ts`: it coordinates input mode changes, file replacement, and data edits, delegating node geometry and selection to `ThreeNodesService.cs` in this first slice.

### Scope
- New files: `FrameWebforCS/three/ThreeService.cs` as the display coordinator corresponding to JS `three.service.ts`; `FrameWebforCS/three/ThreeNodesService.cs` for node markers and selection; focused coordinator and node scene tests under `FrameWebforCS.Tests/`.
- Modified files: `FrameWebforCS/components/input/InputNodesService.cs` (node snapshot and edit notification), `FrameWebforCS/providers/InputDataService.cs` (successful file replacement notification), `FrameWebforCS/AppRoutingModule.cs` and `FrameWebforCS/components/menu/SidebarComponent.cs` (input mode key notification), `FrameWebforCS/three/SceneService.cs` and `FrameWebforCS/three/ThreeComponent.cs` (attach and drive one coordinator), and `FrameWebforCS/AppComponent.cs` only if needed to dispose it on close.
- Dependencies: reuse the existing `THREE`/`THREE.OpenGL` references and the current `GLControl`; do not add another viewport or change the saved input format.
- Included: node sphere creation/update/removal by stable node ID, node selection highlight, input-mode behavior, full rebuild after a successful file open, and prompt update after node edits. JS `getNodeJson(0)` treats missing coordinate components as zero for display; keep stored nullable values unchanged.
- Excluded: member, panel, restraint, load, result, and visible node-number objects; grid-selection synchronization; new camera behavior or 2D/3D switching functionality. Existing view settings must continue to work.

### Implementation Steps

Implement the node object, then let `ThreeService` connect the three events that control it: input-mode change, file replacement, and node edit. `SceneService` remains responsible for scene/camera/renderer; `ThreeService` owns display decisions and entity services.

#### Step 1: Define the node object and mode contract
- [ ] Trace the JS `three-nodes.service.ts` marker/selection behavior and `three.service.ts` node mode policy, then record the C# node object's inputs and operations: replace all nodes, change one node, select/clear, and apply mode.
- [ ] Define `ThreeService` as the single recipient of mode, file, and edit notifications and the owner of `ThreeNodesService`. Keep the node-specific sphere and ID logic in `ThreeNodesService`, and GL rendering/camera ownership in `SceneService`.
- [ ] Map the current C# input routes to a node mode state. Match the JS behavior: node markers remain visible as context in other input modes, while node picking and selection emphasis are active in the node input mode. Do not create a member object in this slice.
- [ ] Define a value snapshot from populated node IDs; project a partial X/Y/Z row to display coordinates with zero for missing components without modifying `clsNode` or JSON.
**Verification**: focused tests show stable numeric IDs, empty and partial rows, and the expected node visibility/selection policy for the existing input routes.

#### Step 2: Create the node-owned THREE objects
- [ ] Add `ThreeNodesService`, owned by `ThreeService`, as the sole owner of the node scene root, sphere geometry, marker instances or meshes, and node-ID mapping. Attach its root to the existing scene once.
- [ ] Implement full replacement plus add/move/delete for one ID without duplicates. Do not copy the JS lookup mismatch between bare IDs and names prefixed with `node`.
- [ ] Make the selected marker visually distinct and clear selection when its node is deleted, the document changes, or the node input mode ends. Release replaced and final THREE resources on the GL-owning thread.
**Verification**: scene-level tests cover empty/one/multiple nodes, repeated updates to the same ID, deletion, replacement, and selection reset. A manual check in the existing C# viewport confirms that known coordinates produce visible markers and a click highlights exactly one marker.

#### Step 3: Add the display coordinator and rebuild after file open
- [ ] Create `FrameWebforCS/three/ThreeService.cs`. It owns one `ThreeNodesService`, receives successful file replacement, node edit, and input mode events, and exposes the node selection operation to the viewport. Keep its event subscriptions and pending updates in one place so later member geometry can join without putting orchestration into `SceneService`.
- [ ] Parse and validate incoming node data before changing the current node rows; commit it and publish one file-replacement notification only after the existing file-load sequence succeeds.
- [ ] On replacement, `ThreeService` tells `ThreeNodesService` to discard old node objects and selection and build from the committed new node snapshot. Treat a missing or empty `node` section as an empty node scene. If the file opens before the viewport is ready, apply the latest committed snapshot when it initializes.
- [ ] Keep the current scene when file loading fails; do not publish an intermediate node scene from a partially processed file.
**Verification**: tests cover opening A then B, B with no nodes, opening before/after viewport initialization, and invalid B after valid A. No marker or selection from A remains after a successful B; failed B leaves A visible.

#### Step 4: Reflect input edits and input-mode changes
- [ ] Notify `ThreeService` when `InputNodesService` accepts a node add, coordinate edit, or deletion. The coordinator delegates the latest change to `ThreeNodesService` on the UI thread before the existing render timer's next frame; coalesce a burst of edits and reject queued updates from an older file revision.
- [ ] Pass the accepted sidebar input-mode key through `AppRoutingModule` to `ThreeService`, including route changes that reuse the same input component. The coordinator applies the Step 1 node visibility and selection policy without rebuilding node geometry.
- [ ] Route viewport clicks through `ThreeService` to `ThreeNodesService` for stable-ID selection. Dispose the coordinator and unsubscribe its input/mode/file events when the viewport closes.
**Verification**: edit, delete, paste-like burst, node→member→node route, and file-open-in-each-mode checks show the correct marker positions and selection in the next frame, without duplicates or stale callbacks. Run focused `FrameWebforCS.Tests` tests and `dotnet build FrameWeb.sln`; manually repeat open/edit/mode/close in the existing app to check native rendering and cleanup.

### Risks & Considerations
- `InputNodesService.setNodeJson` currently returns when `node` is missing (`FrameWebforCS/components/input/InputNodesService.cs:71-75`), leaving old nodes. Replacement must clear them, while a failed load must preserve the last committed node scene.
- The input grid holds up to 100,000 blank rows (`InputNodesService.cs:41-64`). Build snapshots from populated IDs and avoid scanning every row on each edit. Choose a renderer representation that remains responsive for the existing input limit.
- The JS node update compares a bare ID to an object named `node` plus ID (`FrameWebforJS/src/app/components/three/geometry/three-nodes.service.ts:102-114`). Keep an explicit ID map in C#.
- `SceneService` already owns the C# scene and renderer (`FrameWebforCS/three/SceneService.cs:37-81`); `ThreeComponent` owns its GLControl event wiring and render timer. Attach one node owner, perform GL work on its UI thread, and release subscriptions/resources on close.
- The new `ThreeService` owns display orchestration; it must not create another renderer or camera. Later `member` and other entity services can be added beneath this coordinator while keeping the node-only implementation bounded.
- In the JS node mode, member and translucent panel objects also appear (`FrameWebforJS/src/app/components/three/three.service.ts:345-352`). They require their own later entity implementations and are outside this node-only plan.

### Open Questions
- None blocking. The user confirmed that this slice includes node bodies and selection display only. Input mode changes are in scope; new 2D/3D switching behavior is not.

### Handoff for Non-Node Viewport Work (2026-09-27)

#### Goal and current progress
- Extend the existing `FrameWebforCS` viewport toward the JS application's entity and mode behavior, starting from this **node-only** slice. The plan's unchecked steps above are the original implementation checklist, not a claim that no code was written. Node creation, replacement, edits, mode-gated picking, selection, and disposal are implemented in `ThreeNodesService.cs` and `ThreeService.cs`; member, panel, restraint, load, result, and node-number objects remain unimplemented in this viewport.
- The focused `FrameWebforCS.Tests` run passed 73/73 after the node scale correction. Native OpenGL appearance and close/reopen behavior have **not** been manually verified; do that before claiming visual parity. This plan does not cover the separate typed desktop viewport under `FramePrintPDF/`.

#### Conventions for the next entity service
1. Compare the corresponding JS `FrameWebforJS/src/app/components/three/geometry/three-*.service.ts` and each branch of `components/three/three.service.ts` (`fileload`, `changeData`, `ChangeMode`, picking/selection) **before** implementing a C# entity. Preserve JS names, responsibility boundaries, and algorithms where practical. When a C# difference is necessary, add a nearby code comment naming the JS symbol, why it differs, and any behavior still missing.
2. Keep `SceneService` responsible for the existing scene, camera, and renderer; keep `ThreeComponent` responsible for the existing `GLControl`, render timer, pointer routing, and GL-thread disposal. `ThreeService` owns display orchestration and entity-service lifetimes. Give each new entity service its own scene root/resources and stable numeric ID map; attach it to the same scene once. Do not create a second viewport, renderer, or camera.
3. Follow the current event path: grid edit notification, successful `InputDataService.FileReplaced` with `DocumentRevision`, or `AppRoutingModule.InputModeChanged` -> pending work in `ThreeService` -> `FlushPending()` immediately before the next render on the GL-owning thread. Coalesce edits, drop pending edits on file replacement, rebuild from the latest committed input snapshot, and unsubscribe/dispose on viewport close. A file opened before GL initialization must appear on the first flush. For members/panels and other node-dependent shapes, a node edit must also invalidate their geometry; the current coordinator updates **only** node markers.
4. C# sidebar mode keys come from `SidebarComponent` (for example, `"node"`), while JS `ChangeMode` uses plural names such as `"nodes"` and `"members"`. Map each route deliberately in the coordinator, including routes that reuse a component. Implement each JS mode's per-entity visibility, opacity, labels, selection, and GUI settings from its own branch; the current `SetNodeMode(bool)` covers node picking/selection only. Node markers stay visible as context outside node input mode, with selection cleared on exit. JS node mode also shows members and translucent panels; those displays are pending.
5. Keep saved input values and JSON contracts separate from display projection. `InputNodesService.getNodeJson()` preserves nullable coordinates for saving; `getNodeJson(0)` supplies JS-compatible display zeros; `GetDisplayNode(id)` is the O(1) single-edit path. Follow each JS input service's semantics for other entities instead of copying the node projection blindly. Parse/validate incoming data before publication, and keep stable IDs across replace, edit, delete, and picking.

#### JS crosswalk and reasons for differences

| JS behavior or symbol | Current C# counterpart | Reason / unfinished parity |
|---|---|---|
| `three.service.ts` owns the entity services and calls `fileload`, `changeData`, `ChangeMode`, `detectObject` | `ThreeService` owns `ThreeNodesService`; `OnFileReplaced`, `OnNodeEdited`, `OnInputModeChanged`, `SelectAt` queue/apply its work | WinForms grid, sidebar, and render timer expose events; GL changes are deferred to `FlushPending`. Other entity owners and mode branches still need wiring. |
| `three-nodes.service.ts` uses `nodeList` with one named `Mesh` per ID | `_root`/`_markers` (`InstancedMesh`) plus `_indexById` in `ThreeNodesService` | The C# grid permits 100,000 rows, so shared instances avoid a draw call per node; explicit IDs also avoid the JS bare-ID versus `"node" + id` lookup mismatch. |
| JS `selectionItem`, `selectChange`, `detectObject`, `nodeSelected$` | `_selection`, `SelectedNodeId`, `Select`, `Pick` return a stable ID | A red overlay represents selection over shared instances. JS's black unselected markers, hover, and grid-selection synchronization are unfinished. C# selects on a short left `MouseUp` to avoid TrackballControls drag selection; JS selects on pointer down. |
| JS `setBaseScale()` and default `onResize()` (`scale = 100`) | `GetBaseScale()` and `RefreshBaseScale()` | Both ignore zero-distance pairs and use `max(maxDistance / 500, minDistance / 50)`, defaulting to 1 without a distinct pair. C# recalculates after a replacement or an edit batch. JS center, `sizeNode`, `scene.setNewHelper`, node scale GUI, and node-number labels are still absent. Exact pair search is O(n^2); optimize only with an equivalent result when large inputs require it. |
| JS `loadInputData()` followed by `three.fileload()` | `InputDataService.JsonDataOpen()` stages nodes, then emits `FileReplaced` after its load sequence | Missing `node` means an empty node scene and a failed load does not publish new node markers. Whole-document rollback is **not** implemented: other input/result services may already have changed if a later step throws. Extend staging/commit for other entities before claiming atomic file loading. |

#### What worked, what did not, and next steps
- Focused scene/coordinator tests cover stable IDs, partial/empty node rows, edits/deletions, file replacement, stale pending edits, mode behavior, and selection cleanup. Keep new tests at the same boundaries, then check the native viewport for actual color, size, depth, picking, and cleanup. The existing node code comments provide a source-level crosswalk and TODOs; retain that practice for new services.
- A default `dotnet test` output directory was locked by a running C# app/Visual Studio. Putting test output outside the repository broke tests that locate `FrameWebforJS` by walking up from `AppContext.BaseDirectory`. A repository-local ignored `obj` output path worked: `dotnet test FrameWebforCS.Tests/FrameWebforCS.Tests.csproj --no-restore -v:q -p:WarningLevel=0 -p:OutputPath=C:\Users\sasai\Documents\FrameWeb3\FrameWebforCS.Tests\obj\node-parity-output\ -maxcpucount:1`. Use this only if the normal output is locked, and run .NET gates sequentially.
- Next: choose one non-node entity, trace its JS input/edit/mode/selection paths, add its scene owner under `ThreeService`, hook both its own edits and node-dependent invalidation, then test replacement/edit/mode/picking/disposal. Repeat per entity; add labels, GUI, and grid selection where that entity needs them. Check the existing C# route key against the JS mode name instead of assuming they match. Keep unrelated `tools/PythonNetDebugProbe/` changes outside this work.
