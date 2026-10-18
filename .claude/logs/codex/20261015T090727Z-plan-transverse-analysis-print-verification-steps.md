## Implementation Steps (ordered by dependency)

1. **参照物を固定・事前検査する**
   - JSONのSHA-256、更新日時、PDF 3冊のSHA-256・メタデータ・ページ寸法・ページ数をmanifest化する。
   - 座標付きテキスト抽出と全ページraster化が可能か確認し、既知の構成（基本15、PICK UP 45、変位量4ページ）を照合する。

2. **検証ハーネスの実行可能性と製品経路同一性を確認する**
   - `.tmp/transverse-analysis-print-verification/` に private `net10.0-windows` / x64 console harnessを置く。
   - `AssemblyName=FrameWebforCS.Tests` とし、`FromSavedJson → snapshot → CalculateAsync → Deserialize → Build → PrintProjection → DirectPdfGenerator` の製品経路を変更せず利用する。
   - 既知smokeとstale-cache検査を先行実施する。製品CLIやAngular、converterは変更しない。

3. **対応表・単位・選択条件を確定する**
   - 公開トポロジーと剛域・notice・spring分割後の内部IDを対応付け、元の節点・部材選択へ復元する。
   - CASE、COM:CASE、PICKUP、部材端・同一X位置の側、DIST、単位を明示した対応表を作る。
   - 力2桁、DIST 3桁、変位4桁、X/Y=mm、回転=mRadを比較契約として固定する。

4. **一度のfresh解析から3冊を生成する**
   - 保存JSONを一度だけ読み、strict `AnalysisResultSet` を構築する。
   - `[SectionForce]`、`[PickupSectionForce]`、`[Displacement, CombinedDisplacement]` を明示的な用紙・向き・元節点選択で生成する。
   - 全CASE/PICKUP/COMBINEの存在、空・skipケースを記録する。欠落ケースをゼロで合成せず、PICKUP displacementも含めない。

5. **座標ベースの全行パーサーとcoverage検査を作る**
   - 参照・生成PDFの列を座標で復元し、semantic keyのmultisetとして保持する。
   - 重複、ページ、source ID/provenance、同一Xの両側、station、M-MAX、PICKUPのM/S/Nと採用COM:CASEを保持する。
   - 未解析文字、列衝突、missing/extra行を黙って除外しない。

6. **数値・由来を全行比較する**
   - まず印字文字列と桁数を比較し、次にfresh raw値が各印字丸め区間に入るか確認する。
   - base full precisionから合計・極値・PICKUP採用値を独立再計算する。
   - signed zero、同値tie、provenance差、対応不能、missing/extraを別分類で出力する。大きな相対許容差は導入しない。

7. **ページ構成と視覚差を検査する**
   - 全ページをraster化し、代表ページに加えて数値・行・ページ構成の差がある全ページを目視確認する。
   - ページ順、表題、CASE組合せ、列、改ページ、行欠落・重なりを報告する。
   - 未承認のpixel閾値は設けず、PDF byte hash差だけでは失敗としない。

8. **証拠を集約し、不変条件を確認する**
   - manifests、raw結果、3生成PDF、抽出行、row diff、rastersをprivate artifact配下に保存する。
   - `.agents/docs/research/transverse-analysis-print-verification-report.md` に一致・差異・coverage・視覚所見・解析上の制約をリンク付きで記録する。
   - 元JSONの事後SHA-256が `c2798b3c…6553` と一致すること、製品ソースが変更されていないことを確認し、`.agents/check.ps1` を実行する。

## Verification per Step

1. 3冊の寸法・ページ数・構成が既知情報と一致し、全ページの抽出/raster結果がmanifestに載る。
2. harnessが既知smokeを製品経路と同じ結果で通し、入力コピー変更後に旧結果を再利用しない。
3. 参照側の全CASE・部材・節点・stationが一意または明示的な重複として解決され、単位と印字精度が確定する。
4. 一回のfresh解析から実在する3 PDFが生成され、期待集合との差とempty/skipped理由が列挙される。
5. 各PDFの全ページ・全対象表について、抽出行数とsemantic key multisetが追跡可能で、未解析行がゼロまたは明示される。
6. 各差分にページ、semantic key、参照値、生成印字値、raw値、丸め判定、provenanceが付く。
7. 全ページrasterが存在し、代表ページと全差分ページに目視所見がある。
8. 報告書から全証拠へ到達でき、入力hash不変、対象外ソース無変更、agent-infrastructure gate成功を確認できる。

## Risks and Mitigations

- 入力が参照PDFより新しいため、差を即座に不具合扱いせず、モデル差・解析差・印字差・構成差に分類する。
- `RunOptions` はCOMBINE変位を要求できないため、friend assemblyから既存の内部projectionを使い、production CLIは拡張しない。
- 内部分割IDの誤対応は最大リスクなので、数値比較より先にトポロジー対応表とcoverageを検証する。
- 空CASEは参照との欠落を生み得るため、skip理由を記録し、synthetic zero caseで隠さない。
- Legacy PDFは列順や文字配置が不安定なため、テキスト順ではなく座標とsemantic keyで解析する。
- 丸め、signed zero、tieは数値誤差と混同せず、独立カテゴリで報告する。
- 日本語パスはUTF-8スクリプトファイルを使い、PowerShellから日本語コードを直接pipeしない。
- 視覚許容値が未指定なので、自動合否を捏造せず、差分を明示して判断材料を残す。
- 本検証は再現性・対応関係・印字比較であり、数値工学上の承認を意味しない。

## Open Questions

- なし。視覚差の許容値は未指定のまま全差分を報告し、修復・工学承認・Angular/converter変更は対象外とする。
