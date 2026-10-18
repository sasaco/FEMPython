# Result Contract Refactor — Codebase Scan

> **Status: SUPERSEDED DESIGN RECOMMENDATION — current-state evidence only.** The compatibility-first recommendations, contract names, media types, and phased migration below predate the approved pre-release direction and must not be implemented. The normative direction is [the AnalysisResultSet-only plan](../plans/result-contract-refactor.md), [the v1 output contract](analysis-result-set-v1-contract.md), and [DESIGN.md](../DESIGN.md): one `AnalysisResultSet` success root, no `FrameResultSet` or `legacy-cases-v1`, no flat public result response, no compatibility adapter, accepted nonlinear steps as sibling results, and deletion of post-solve `rate` without replacement. This file remains only as a record of discovered current code paths and dependencies.

## Scope

本調査は、解析結果を時間軸の「旧／新」で呼び分けず、次の独立した契約へ分離するための現状確認である。

- `AnalysisResult`: 一つの独立した解析（荷重ケース1件）のcanonical solver result。
- `AnalysisStepResult`: material nonlinear解析における一つの受理済み載荷stepの状態。
- `AnalysisResultSet`: case IDと要求順を保持する複数`AnalysisResult`の集合。
- `FrameCaseResult`: FrameWebforJS表示用の一ケース投影（`disg/reac/fsec/shell_fsec/size`）。
- `FrameResultSet`: 複数`FrameCaseResult`の集合。

既定の単一ケースHTTP responseと、既存の
`application/vnd.frameweb.legacy-cases-v1+json` response bodyは移行中も変更しない。今回の調査はコード変更を含まない。

重要な現状認識は、`AnalysisResultSet`は設計文書上の名称であり、製品コードにはまだ実装されていない点である。現在「legacy cases」として実装されているものはcanonical resultの集合ではなく、全ケースを解析した後に直接FrameWebforJS向けcase mapへ投影した互換responseである。

## Current Data Flow

### Default single-case path

```text
HTTP input
  -> FrameWeb/main.py:FEMPython
  -> fem.file_io._read_json_model
       legacy `node` inputの場合は legacy_beam.select_case(data)
       （case ID未指定なので先頭caseだけを選択）
  -> FemModel.read_json_model
  -> FemModel.run
  -> Solver.solve / eigenvalue_analysis
  -> solver_results.snapshot / final_result
  -> FemModel._post_process_results
  -> metadata追加
  -> result_to_jsonable
  -> application/json response
```

`FemModel.run()`の返値が現在の事実上の`AnalysisResult`である。型は`dict[str, Any]`で、明示的な`TypedDict`やschema classはない。線形、material nonlinear、modalでfield集合が異なる。

### Nonlinear history path

`solver_results.snapshot()`は収束した各載荷stepについて、`step`、`lambda`、`displacement`、`node_displacements`、`reaction_forces`、`iterations`を作成し、nonlinearの場合は`element_stresses`、`curvature`、`section_response`等も加える。`solver_results.final_result()`は最後の受理済みstepから最終状態を複製し、その一つの`AnalysisResult`内へ次を格納する。

```text
AnalysisResult (one case)
├─ final accepted state at top level
├─ step_results: ordered list[AnalysisStepResult]
└─ convergence_history: Newton-iteration records across the steps
```

したがってload case集約とnonlinear step履歴は別の軸である。caseをstepとして扱ったり、stepを擬似caseへ展開してはならない。`convergence_history`も`AnalysisStepResult`そのものではなく、step内部の反復診断である。

`FemModel._post_process_results()`は最終`element_stresses`を最後のstepから複製し、shell resultは各stepにも追加し、beam end-force recovery後には最後のstepの反力・端力をtop-levelへ再同期する。このためtop-levelは「最後に確定した状態」であり、途中stepの最大値ではない。

### Current multi-case compatibility path

```text
Accept: application/vnd.frameweb.legacy-cases-v1+json
  -> main.py:_legacy_cases_requested
  -> legacy_results.solve_legacy_cases
       input load mapを挿入順に走査
       -> legacy_beam.select_case(data, case_id)
       -> fresh FemModel
       -> model.run() == one AnalysisResult
       -> _project_case(data, model, result, rate)
  -> {caseId: {disg, reac, fsec, shell_fsec, size}}
```

この処理は最大256 cases、fresh model/case、case context付きerror、全件完了後だけresponseを返すatomic behaviorを持つ。一方、`solve_legacy_cases()`はcase orchestrationとpresentation projectionを同じ関数で行い、途中のcanonical `AnalysisResult`集合を公開も型定義もしていない。

また、`_project_case()`は`AnalysisResult`だけでは実行できない。元入力のmember/notice/rigid情報、解いた`FemModel`の`node_labels`、mesh上の`original_id/member_start/member_end`、支持条件、caseの`rate`を使用する。そのため、厳密な実装関係は単純な
`AnalysisResultSet -> FrameResultSet`ではなく、内部のcase solution contextから両representationを生成する形になる。

```text
CaseSolutionSet (internal, not wire format)
├─ case_id
├─ AnalysisResult
├─ solved FemModel / projection metadata
└─ compatibility rate
     ├─ serialize -> AnalysisResultSet
     └─ project   -> FrameResultSet
```

### Frontend path

`FrameWebforJS/src/app/app.component.ts`は`legacy-cases-v1`を明示要求し、解凍・JSON parse後、期待case IDsとcase mapの順序・必須mapを検証する。`ResultDataService`は同じ検証を再実行してから、`disg/reac/fsec`を三つのworker系へ渡す。各workerはcase mapを走査してtable/three.js/combine/pickup用配列へ変換する。

frontend内では現在、wire contract、保存ファイル内の`result`、worker入力が同じtop-level case mapを共有している。`LegacyCaseResult`/`LegacyCasesResult`という名前と、多数の`any`が境界を曖昧にしている。保存済みモデル/result fileの読み込みも`ResultDataService.loadResultData()`へ直接流入するため、HTTPだけをenvelope化するときは保存ファイル互換adapterが必要である。

## Contracts

### Contract semantics

| Contract | Cardinality | Purpose | Nonlinear history |
|---|---:|---|---|
| `AnalysisStepResult` | one accepted step | 一つのnonlinear載荷状態 | 自身が履歴要素。caseではない |
| `AnalysisResult` | one analysis/case | solverのcanonical result。既定response | `step_results`と`convergence_history`を内包 |
| `AnalysisResultSet` | many cases | canonical resultsのordered collection | 各caseの`AnalysisResult`内に履歴を保持 |
| `FrameCaseResult` | one case | FrameWebforJS表示projection | v1はfinal stateのみを投影 |
| `FrameResultSet` | many cases | display projectionsのordered collection | step historyは含めない |
| `legacy-cases-v1` body | many cases | deployed compatibility encoding | 現行どおりfinal stateのみ |

`AnalysisResult`は最低でも`analysis_type`で判別するunionとして扱う必要がある。static/material nonlinear/modalは共通fieldを完全には共有しない。特にmodal resultへ`node_displacements/reaction_forces/element_stresses`を一律要求してはならない。

### Recommended wire shapes

要求順を正式な契約にする場合、新しいcanonical media typesはJSON objectのproperty順に依存せず、明示的なarrayを使うべきである。JavaScriptではinteger-like property keysが挿入順と異なる順に列挙され得る。

```json
{
  "schema_version": "1.0",
  "cases": [
    {"case_id": "2", "result": {"analysis_type": "static"}},
    {"case_id": "1", "result": {"analysis_type": "material_nonlinear", "step_results": []}}
  ]
}
```

上記を`application/vnd.frameweb.analysis-result-set-v1+json`の候補とする。`application/vnd.frameweb.frame-result-set-v1+json`も同じordered entry構造で、`result`部分を`FrameCaseResult`にする。frontend境界でこのenvelopeを現在のworker用case mapへ正規化すれば、worker群を一度に書き換える必要はない。

`application/vnd.frameweb.legacy-cases-v1+json`だけは既存clientのため、次のbodyをbyte-level field shapeとして維持する。

```json
{
  "case-id": {
    "disg": {},
    "reac": {},
    "fsec": {},
    "shell_fsec": {},
    "size": 0
  }
}
```

### Rate semantics requiring an explicit decision

現行互換経路はcaseの荷重を一度solveし、その後`disg/reac/fsec`へ`rate`を乗算する。canonical `AnalysisResult`自体はrate適用前である。線形解析では後乗算が等価になり得るが、nonlinear解析では「loadをrate倍して再解析」と「結果をrate倍」は一般に等価でない。

したがってrefactorでは次を固定する必要がある。

- `AnalysisResultSet`はsolverが実際に解いたcanonical resultを返し、presentation用`rate`を暗黙適用しない。
- `FrameResultSet v1`とdeprecated aliasは、互換性のため現行のpost-solve rate投影をexactly once維持する。
- 将来rateを解析荷重係数として扱うなら、別versionでinput semanticsから変更する。今回のrename/refactorに混ぜない。

## Affected Files

### Backend — current owners

| File | Current responsibility | Refactor impact |
|---|---|---|
| `FrameWeb/main.py` | transport decode/encode、Accept判定、default/legacy dispatch | representation enum/registryへ整理。既定pathは不変 |
| `FrameWeb/src/fem/file_io.py` | legacy/modern input parse、JSON-safe result conversion | defaultの先頭case選択を維持。result-set orchestratorから明示case選択済みinputを渡す |
| `FrameWeb/src/fem/legacy_beam.py` | `select_case()`とlegacy beam変換 | case sourceとして再利用。汎用result contractの名前を付けない |
| `FrameWeb/src/fem/model.py` | one model/one analysis、postprocess、metadata | `AnalysisResult` producer。wire集合やFrame projectionは持たせない |
| `FrameWeb/src/fem/solver_results.py` | accepted snapshots、final result、metadata | `AnalysisStepResult`/`AnalysisResult`の事実上のschema owner |
| `FrameWeb/src/fem/legacy_results.py` | validation、全case solve、rate、FrameWebforJS投影 | orchestrationとprojectionを分離し、deprecated wrapperだけ残す |
| `FrameWeb/src/app/result.py` | 旧FEMCalculation系のdisplay TypedDict/projector | 現行`FemModel`経路では参照されない。新contractの定義先として再利用せず、混同を避ける |

### Backend — likely new/changed boundaries

| Likely file | Responsibility |
|---|---|
| `FrameWeb/src/fem/result_contracts.py` | `AnalysisResult` variant、`AnalysisStepResult`、`AnalysisResultSet`、`FrameCaseResult`、`FrameResultSet`の型と境界validation |
| `FrameWeb/src/fem/result_sets.py` | input case列挙、256 guard、fresh model/case、atomic `CaseSolutionSet` orchestration、canonical serialization |
| `FrameWeb/src/fem/frame_results.py` | `CaseSolutionSet`からFrameWebforJS display projection。現`legacy_results.py`のprojection helper移設先 |
| `FrameWeb/src/fem/legacy_results.py` | `solve_legacy_cases()`を維持するthin deprecated adapter。body shapeを変えない |

### Tests and docs

| File | Required coverage |
|---|---|
| `FrameWeb/tests/io/test_legacy_cases_api.py` | alias body/content-type/order/atomicity/rate/256 guardの既存保証を継続 |
| `FrameWeb/tests/io/test_http.py` | default `application/json`のsingle-case schemaが完全に不変であること、新media type/unknown version |
| new `FrameWeb/tests/io/test_result_set_api.py` | ordered array、case ID、canonical per-case result、Frame projection、alias同値 |
| `FrameWeb/tests/solvers/test_nonlinear.py`、`FrameWeb/tests/integration/test_model_contracts.py` | per-case `step_results`、final-state consistency、shell step historyがresult set内でも失われないこと |
| `FrameWeb/docs/wiki/endpoints.md`、`FrameWeb/docs/wiki/results.md` | media types、schema、case vs step、deprecated alias、rate semantics |

### Frontend

| File | Current dependency / likely change |
|---|---|
| `FrameWebforJS/src/app/app.component.ts` | legacy media typeを要求。新`frame-result-set-v1`へ移行し、response normalizerを呼ぶ |
| `FrameWebforJS/src/app/providers/result-data.service.ts` | `LegacyCasesResult` validationとworker dispatch。`FrameResultMap`等のUI内部名へ変更 |
| new `FrameWebforJS/src/app/providers/result-contracts.ts` | wire `FrameResultSet` envelope、deprecated raw case map、normalization/validation |
| `FrameWebforJS/src/app/providers/result-data.service.spec.ts` | canonical envelopeとlegacy saved-result mapの両方、order mismatch、missing fields |
| `result-disg/reac/fsec` services and `*1.worker.ts` | 直ちにwire schemaへ追随させず、normalized `FrameResultMap`のconsumerとして維持可能 |
| `menu.service.ts`、`menu.component.ts`、`preset.component.ts` | 保存済み`result`を読むためlegacy raw map compatibilityが必要 |

## Dependencies

- Pythonのdict insertion order、`json.dumps`、`result_to_jsonable()`のkey string化とNumPy変換が現在のserialization基盤である。ただしcross-languageの正式なorder保証にはarrayが必要である。
- `legacy_beam.select_case()`はdeep copyしてcase固有のelement/fix/joint/member-support定義を絞る。result-set orchestrationはこれを再利用でき、solver core変更は不要である。
- `FrameCaseResult`生成は`AnalysisResult`だけでなく、solved modelと元legacy inputに依存する。wire `AnalysisResultSet`を後から単独変換するAPIにはできない。
- `FemModel.run()`は解析失敗時に`results=None`へ戻し、nonlinear solverもaccepted state/rollback契約を持つ。fresh model/caseと組み合わせることでcase間state isolationを維持できる。
- FrameWebforJSのcombine/pickup/three.js/print経路はworkerが生成した配列へ依存する。HTTP境界でnormalizeすれば下流の大規模改修を避けられる。
- result保存ファイルはHTTPとは別の互換境界である。既存raw case mapを読み続ける必要がある。
- `shell_fsec`は現legacy beam adapterでは常に空であり、shell/solid input自体を拒否する。新名称に変えてもgeneral shell compatibilityを示してはならない。

## Risks

1. **Misnaming risk:** 現`solve_legacy_cases()`をそのまま`solve_analysis_result_set()`へrenameすると、display fieldsをcanonical resultと誤称する。必ずorchestrationとprojectionを分ける。
2. **Order risk:** top-level object mapはnumeric-like case IDsをJavaScriptで再列挙したとき順序が変わり得る。canonical v1はordered arrayにする。deprecated aliasのmap shapeは維持する。
3. **Nonlinear semantic risk:** case `rate`のpost-solve乗算をcanonical resultへ混入すると物理意味が変わる。case aggregationとstep history、およびanalysis factorとdisplay rateを分離する。
4. **Variant schema risk:** static/nonlinear/modalを一つの必須field集合でvalidateすると正しいresultを拒否する。`analysis_type` discriminated validationが必要である。
5. **Projection context risk:** resultだけを保存して後から`fsec`へ投影できると仮定すると、member segmentation、generated node labels、supportsが欠落する。
6. **Compatibility risk:** new media type追加時にdefault response、compressed transport、legacy alias content type/body、保存済みresult fileのいずれかを同時変更しやすい。各境界にgolden contract testが必要である。
7. **Frontend completion risk:** worker errorsは主にconsole logされ、`ResultData.isCalculated`はasync worker完了前にtrueとなる。今回のcontract refactorで少なくともschema failureはdispatch前に止めるが、worker completion semanticsの全面改修は別taskに分けるのが安全である。
8. **Scope risk:** 現multi-case implementationはlegacy beam input専用で、modern `nodes`、shell、solidを拒否する。正式名称だけで汎用対応済みと宣言しない。

複雑度は**MODERATE**である。内部抽出だけならbackend 4〜6 product files + 3〜4 test files、新media typeとfrontend migrationまで含めると概ね10〜14 product/test files + docsとなる。solver algorithm変更は不要だが、wire compatibility、ordering、rate、nonlinear nestingのriskがあるため単一巨大変更にはしない。

## Recommendation

最小でクリーンな境界は、solverを触らず、case execution serviceとrepresentation adaptersを分けることである。

### Phase 1 — Internal extraction, no wire change

1. `result_contracts.py`に契約名を定義する。runtime objectを重いclassへ置換せず、既存dict serializationと互換な`TypedDict`/type aliasesから始める。
2. `result_sets.py`へcase列挙、入力guard、fresh `FemModel`、case context error、atomic completionを移す。内部返値はordered `list[CaseSolution]`とする。
3. `frame_results.py`へ現`_project_case()`以下を移し、`CaseSolution`から`FrameCaseResult`を生成する。
4. `legacy_results.solve_legacy_cases()`は新service + projectorを呼び、従来のtop-level case mapを組み立てるthin adapterとして残す。
5. 既存`test_legacy_cases_api.py`を一切弱めず、bodyの完全互換を確認する。

### Phase 2 — First-class result-set media types

1. `main.py`のboolean selectorを`ResultRepresentation`（single analysis / analysis result set / frame result set / legacy alias）へ置き換える。
2. `analysis-result-set-v1`と`frame-result-set-v1`は`cases` array envelopeを返す。unknown vendor versionは406、defaultは従来のsingle `AnalysisResult`のままにする。
3. material nonlinear caseを2件含むcontract testを追加し、各entryが独立した`step_results`/`convergence_history`を持つこと、Frame v1が各caseのfinal stateだけを投影することを固定する。

### Phase 3 — Frontend migration

1. HTTP clientは`frame-result-set-v1`を要求する。
2. `result-contracts.ts`でwire envelopeをvalidateし、既存worker用`FrameResultMap`へ一度だけnormalizeする。
3. `ResultDataService`とtestsの`LegacyCases*`名称を`FrameResult*`へ変更する。ただし保存済みraw case mapを受けるdeprecated normalizerは残す。
4. browser/Electron compatibility期間後にのみ`legacy-cases-v1` alias廃止を検討する。

この段階分割なら、`AnalysisResultSet`は「旧型式」のrenameではなくcanonical case collectionとして実体化し、`FrameResultSet`は表示projection、`AnalysisStepResult`は一解析内のhistoryとして明確に分離できる。
