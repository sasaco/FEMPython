## 計画書：原宿ボックス 0404 の印刷の体裁を FrameG の見本に合わせる（残りの作業）

### 目的

FrameG（WinFrameG 4.3.6）の見本 `FrameGConverter/Convert_Test/TestData/原宿ボックス_0404/` を、FrameWebforCS で計算して FramePrintPDF で印刷した帳票の**既定の体裁**にする（`.claude/docs/DESIGN.md` 2026-10-28）。
これまでの記録は `.claude/docs/research/harajuku-0404-parity-report.md`。この計画は、その残り（入力の頁・COMBINE と PICK UP の数表・荷重図・断面力図・フラグの撤去）を扱う。

### 制約（合意済み。蒸し返さない）

- 流れ：`.frd` →（`ConvertManager`）→ 保存形式の JSON → 計算 → `PrintProjection.Build` → `DirectPdfGenerator`。
- 比べ方：計算書の付記（`4-n`・`5-n`、章・節の見出し、断面力図の 23 頁）は除く。頁割り・文字列・フォント・色は一致、位置は ±1 mm、加えて頁を重ねた目視。
- 用紙が横向き・A3 でも、FrameG の体裁を A4 縦の座標（pt）のまま選んだ用紙に置く（2026-10-29 決定）。頁送りは頁の高さから求める。横向きで 2 ケースを並べる体裁は消す。文字の頁は左 71.6 pt・行の下端 ≈ 778.6 pt（`PdfDocument.FrameGLeft`・`FrameGBottom`）。図の頁は W3・W4 で決める。
- 文字は MS 明朝 9.06 pt、数表の行の間隔 9 pt。
- FrameG の版の文字列（`DataVer 4.3.6`・バナー）は出さない。`SHARE` は `SHEAR` で出す。
- ja は見本を再現、en は同じ配置で日本語の文字だけ英訳。3D は変えない。**Web 版（FramePrintAzure）の 2D も FrameG の体裁にする**（2026-10-29 にユーザーが「Web 版は変えない」を改めた）。ただし Web 版そのものは考えない（Web 版のための手直しも確かめもしない。2026-10-29 決定）。`cn` は `en` と同じ決まり（配置は同じ、日本語の文字だけ中国語に）。
- 符号は変えない（2D は y 下向き。数表・PIK は M = mz、S = −fy、N = fx）。COMBINE の変位（T5）と値の最後の桁の丸め（N5）は対象外。
- 試験を弱めない。以前の見本（線路直角方向）を前提にした試験を直すときは、試験ごとの理由をこの計画書に書く。既知の失敗 `ResultPageActivationTests.CombinedPagePublishesWhenInitiallyShownAfterRouteSelection` は無視。
- Git：`.claude/skills/headless-runner/SKILL.md` の未コミットの変更はユーザーのもの（含めない・戻さない）。push しない。共用の Release の Headless exe は作り直さない（`-c Debug`）。

### 完了条件

1. この計画書の全項目に ✅、知見・設計の考え方・Tips がある。
2. `denzan.pdf` が `見本_電算出力.pdf` と同じ頁割り（T5 の COMBINE 変位の頁を除く）で、入力の写し・MEMBER FORCES・変位量・COMBINE・PICK UP の全頁で対の語の位置差が 1 mm 以内、文字列・フォント・色が一致（例外：N5、`SHARE`→`SHEAR`、T5、付記）。
3. `load-diagram.pdf` が 7 頁・同じケースの組で、文字は 1 mm 以内、線・矢印・寸法は重ねた画像で一致。
4. `section-diagram.pdf` が 44 頁（付記の 23 頁を除く）で頁割り・頁頭・色が一致、図は重ねた画像で一致。値の文字は 1 つの置き方の規則に従い、FrameG 自身の置き方が揃わない所の 2 mm 前後のずれは認める（2026-10-29 決定 Q11）。
5. 余白が文字の頁・図の頁とも見本と ±1 mm（`margins.py`）。
6. `referenceResultLayout` のフラグが無くなり、FrameG の体裁が 2D の既定。
7. 試験が全て通る（既知の 1 件を除く）。書き直した試験の理由がこの計画書にある。
8. 数値が後退しない：基本ケースの断面力 0.01 以内、PIK の不一致 27 行のまま。
9. DESIGN.md・報告書・関係 README を更新、区切りごとにコミット（push なし）。

### 進め方と担当

リーダー（この会話の主エージェント）が計画・共有のファイル・統合・検証・報告を持ち、W1〜W4 は作業者が**別々の git worktree**で並行に進める。

| 作業者 | 項目 | worktree / branch |
|---|---|---|
| A | W1 入力の頁 | `C:\tmp\fp-wt\A` / `wip/harajuku-w1-input` |
| B | W2 COMBINE・PICK UP | `C:\tmp\fp-wt\B` / `wip/harajuku-w2-envelope` |
| C | W3 荷重図、W6 1 頁 1 ケースの選択 | `C:\tmp\fp-wt\C` / `wip/harajuku-w3-load` |
| D | W4 断面力図 | `C:\tmp\fp-wt\D` / `wip/harajuku-w4-section` |
| リーダー | 統合・W5・報告 | `main` |

worktree にする理由：同じ作業ツリーで 4 人が `dotnet build` すると `obj/` を取り合う。各 worktree の `FrameWeb/.venv` は main の `.venv` へのジャンクション（計算の Python はそこから読む）。

#### ファイルの持ち主

| 持ち主 | ファイル |
|---|---|
| A | `FramePrintPDF/PDF_Manager/Printing/PrintInput/*.cs`、新しい `PrintInput/FrameGInputLayout.cs`、`FrameGConverter/Convert_Manager/**`（着目点の名前・ﾏｰｸの変換）、自分の新しい試験ファイル |
| B | `PrintResult/ResultFsecCombine.cs`・`ResultFsecPickUp.cs`、新しい `PrintResult/FrameGEnvelopeLayout.cs`、自分の新しい試験ファイル |
| C | `Printing/PrintDiagram/DiagramInput.cs`・`DiagramMemberLoad.cs`・`DiagramNodalLoad.cs`・`DiagramDimension.cs`・`XMemberLoad_*.cs`・`XNodalLoad.cs`・`XDistance.cs`、`FrameWebforCS/providers/printing/` の荷重図の投影（`PrintProjection.cs` の荷重図の部分はリーダーに頼む）、自分の新しい試験ファイル |
| D | `PrintDiagram/DiagramResult.cs`・`CanonicalSectionDiagram.cs`、`FrameWebforCS/providers/printing/PrintDiagramProjection.cs`、自分の新しい試験ファイル |
| リーダー | `PdfDocument.cs`・`PrintInput.cs`（FramePrintPDF 直下）・`PrintData.cs`・`ReferenceResultLayout.cs`・`PrintProjection.cs`・`PrintResultMetadata.cs`・既存の試験ファイル（`TransverseReportCompositionTests.cs` など）・文書 |

- 図の頁の余白は、作業者 C・D がそれぞれの入口（`DiagramInput.printPDF`・`DiagramResult.printPDF`）で `document.Margine` を設定する。W5 のときにリーダーが `PdfDocument` へ寄せるか決める。
- 既存の試験を直す必要があるときは、作業者は**直してよいが**、試験の名前と理由を返す（リーダーが計画書に書く）。他の作業者の持ち物の試験には触らない。
- 新しい体裁は今の `data.referenceResultLayout`（2D・縦）の条件の中に書く。W5 でその条件を外す。

#### 共有の足場（リーダーが先に入れた）

- `ReferenceResultLayout.Wrap`・`ReportFont`・`Number` を `internal` にした（新しいファイルから使う）。

### 作業項目

#### W1 入力の頁（作業者 A）　見本 `見本_電算出力.pdf` 1〜4 頁

- [x] ✅ W1-1 格点 DATA：縦 1 列（No・X・Y）。
- [x] ✅ W1-2 部材 DATA：格点番号・部材長・ﾏｰｸ・部材名・部材数・E・膨張係数・A・I（材料の表は別に出さない）。ﾏｰｸの欄は空（2026-10-29 決定：保存形式に足さない。比較の例外）。
- [x] ✅ W1-3 剛域データ（部材 DATA（剛域データ））。
- [x] ✅ W1-4 部材バネ DATA：TYPE ごとに 2 列（Kv・Ku）。
- [x] ✅ W1-5 着目点：名前の欄は空（2026-10-29 決定：保存形式に足さない・変換も変えない。比較の例外）。
- [x] ✅ W1-6 基本荷重 DATA：構造系条件の欄。
- [x] ✅ W1-7 実荷重：V/H・M/S の欄と通し番号「(...1)」。
- [x] ✅ W1-8 DEFINE、COMBINE（係数を下の行に）、PickUp。
- [x] ✅ W1-9 頁割り 4 頁、右端からはみ出さない。

確かめ方：`layout_compare.py 見本 1..4 出力 1..4 --detail` で 1 mm 超 0、`margins.py`。

結果（`52cef81`、作業者 A の `2a87dec` を取り込み）：入力の写しは 5 頁 → 4 頁で見本と同じ頁割り。対の語は 274・261・291・312 で 1 mm 超 0。見本にだけある語は 1 頁のﾏｰｸ 20 個と 2 頁の着目点の名前 19 個（決定どおり空欄）、出力にだけある「剛域」2 個は見本で隣の名前とつながった語。余白の差 0.3 mm 以内。新しいファイル `PrintInput/FrameGInputLayout.cs`、試験 `FrameGInputLayoutTests.cs`（9 件）。共有の `PrintInput.cs` は 2D の入力の節の前に `FrameGInputLayout.Applies` の分岐を 1 つ足しただけ（FrameG は節の順序が違い材料の表も出さないので、節を丸ごと置き換える）。試験 `dotnet test FramePrintPDF/PDF_Manager.Tests`、変換に触れたら `FrameGConverter/Convert_Manager.Tests`。

#### W2 COMBINE・PICK UP の数表（作業者 B）　見本 21〜128 頁

- [x] ✅ W2-1 1 つの COMBINE／PICKUP につき MOMENT・SHEAR・AXIAL の MAX・MIN の 6 頁（各 1 頁）。
- [x] ✅ W2-2 CASE の欄 `( 1+2+3+4+5+8-10 )`、PICK UP は `( 2 :1+2+3+4+8-10 )`。
- [x] ✅ W2-3 見出し（`COMBINE  1  名称`、下線、`MOMENT MAX.`、列見出し `DIST. M S N CASE` / `COM: CASE`）の位置。
- [x] ✅ W2-4 `SHARE` を `SHEAR` に（見本との比較ではこの語だけ例外）。

確かめ方：`page_map.py` で COMBINE・PICK UP の全頁が 1 対 1、各頁 1 mm 超 0。

結果（`eef4d77`、作業者 B の `01d1884` を取り込み）：見本 21〜128 頁と出力 94〜201 頁が 1 対 1（108 頁）、1 mm 超 0。例外は値の 0.01 の丸め 1552 行と、同点で別のケースを採った 57 行（その頁で見ている力は 0.01 以内で一致し、一緒に書く M・N と CASE だけが違う。N4 と同じ計算の側の差）と、その行の閉じ括弧の 15 語。余白の差は最大 0.2 mm。電算出力は 261 頁 → 201 頁。新しいファイル `PrintResult/FrameGEnvelopeLayout.cs`、試験 `FrameGEnvelopeLayoutTests.cs`（11 件）。

#### W3 荷重図（作業者 C）　見本 `見本_荷重図.pdf` 1〜7 頁

- [x] ✅ W3-1 頁の組：1 頁に 1〜2 ケース（3・4、5・6、7・8、9・10 を同じ頁に）。組み方の規則を見本から決める。
- [x] ✅ W3-2 頁頭の題名を出さない。左の余白の回転した `題名 (DataVer 4.3.6) P n` も出さず、`4-n` と同じく比較から外す（2026-10-29 決定）。
- [x] ✅ W3-3 ケースの見出し `Case 3  名称  記号` の位置・フォント（FrameG 自身の出力。比較に含める）。
- [x] ✅ W3-4 荷重の値と寸法を図の外に横書き。部材軸方向の分布荷重は `q =11.00`。節点荷重の矢印と値。
- [x] ✅ W3-5 図の大きさ・位置・余白。

確かめ方：文字は `layout_compare.py`、線・矢印は重ねた画像（差分画像の道具を足す）。

結果（`48cb2c0`、作業者 C の `30690cc` を取り込み）：7 頁、ケースの組は見本と同じ（ハーネスは荷重図を `SplitHorizontal` と `LoadDiagramSingleCaseIds: ["1","2"]` で出す）。7 頁とも 1 mm 超 0。1 mm 内に相手の無い線の画素は 3 頁 33（ケース 4 の右の寸法の補助線）・4 頁 3 だけ。余白は白い塗り（付記）を除いて 0.3 mm 以内。新しいファイル `PrintDiagram/FrameGLoadDiagram.cs`、試験 `FrameGLoadDiagramTests.cs`（9 件）・`LoadDiagramSingleCaseProjectionTests.cs`（3 件）。共有の変更：`PrintData.cs`（荷重図の JSON に `rigid` とフラグ）、`PrintProjection.cs`（`PrintSelection.LoadDiagramSingleCaseIds` → `single_layout_cases`）。「1 頁に 1 ケース」をどこから受け取るかはユーザーに確認中。

#### W4 断面力図（作業者 D）　見本 `見本_断面力図.pdf` 1〜22・24〜45 頁

- [x] ✅ W4-1 S1 頁頭は題名だけ（`TITLE :` なし）、`Case 1 固定死荷重:D1`（記号つき）。
- [x] ✅ W4-2 S2 COMBINE の頁に `COMBINE 1 名称`。
- [x] ✅ W4-3 S3 図を小さめ、値は部材端・最大値など限られた点だけ小さな文字。
- [x] ✅ W4-4 S4 文字は全て青、題は細字。
- [x] ✅ W4-5 S5 軸力図を閉じた骨組の内側に描く。
- [x] ✅ W4-6 図の頁の余白。

確かめ方：頁頭の文字は `layout_compare.py`、図は重ねた画像。

結果（`1b46b17`、作業者 D の `ede2993` を取り込み）：44 頁、頁割り・頁頭（題名、`Case 1 固定死荷重:D1`、`COMBINE 1 名称`）・色（全て青の MS 明朝の細字）は見本どおり。重ね画像で 1 mm 内に相手の無い画素は見本→出力 32,792／出力→見本 16,200（変更前 627,970／1,219,736。見本側は頁ごとに付記約 400）。値の文字の 1 mm 超は 44 頁で 290 語（語数 1900。近い順に対にすると 213）。残りは FrameG の置き方が左右で揃わない所（剛域の面の値のずらし方）、剛域側のせん断力の描く側、基本ケースの N の値の選び方、変位図の i 端の値（各 2 mm 前後）。W4-5（S5）は「閉じた骨組の内側」ではなく「+N を部材の法線（`transform.Normal`）側」が FrameG の規則（見本 25 頁で COMBINE 1 の上床版の圧縮が外側に出る）。新しいファイル `PrintDiagram/CanonicalSectionDiagram.FrameG.cs`（partial）、試験 `FrameGSectionDiagramTests.cs`（8 件）・`FrameGSectionDiagramProjectionTests.cs`（3 件）。共有の変更：`PdfDocument.cs` に `OwnPageHeader`・`Title`。

#### W6 荷重図の「1 頁に 1 ケース」の選択（作業者 C、2026-10-29 決定 Q9 (b)）

- [x] ✅ W6-1 デスクトップの印刷の画面に、荷重図で 1 頁に 1 ケースにするケースを選ぶ欄（`PrintSelection.LoadDiagramSingleCaseIds`）。
- [x] ✅ W6-2 Headless の `run` に同じ選択の引数。
- [x] ✅ W6-3 試験と README（`headless-runner` の SKILL.md はユーザーの未コミットの変更があるので触らない）。

`.frd` の書式（`$16.txt` の `1.2`）は保存データに写さない（ユーザーの決定）。

結果（`97423df`、作業者 C の `e8a5daf`）：印刷の画面に「荷重図で 1 頁に 1 ケース (分割配置のとき)」（2D だけ、荷重図に印があり上下か左右の分割のときだけ選べる。何も選ばなければ全ケースを 2 つずつ）。Headless は `--load-diagram-single-cases 1,2`（`load` のキーのカンマ区切り。分割の配置でないと `INVALID_ARGUMENT`、文書に無いキーは `LOAD_DIAGRAM_UNKNOWN_CASE`）。0404 で `--load-diagram-layout split_horizontal --load-diagram-single-cases 1,2` にすると見本と同じ 7 頁。Headless の README を更新。`.claude/skills/headless-runner/SKILL.md` はユーザーの未コミットの変更があるので触っていない（書くべき 2 文はユーザーに渡した）。試験：PrintUi.Tests 32（新 3）、Headless.Tests 74（Debug）。

#### W5 `referenceResultLayout` の撤去（リーダー、W1〜W4 の統合後）

- [x] ✅ W5-1 `PrintProjection.cs` が付けるフラグと `PrintData.referenceResultLayout` を撤去し、2D は常に FrameG の体裁。
- [x] ✅ W5-2 `PdfDocument.cs`・`PrintInput.cs`・`PrintResult/*.cs`・`PrintResultMetadata.cs` の分岐を整理。横向きの対の体裁（landscape paired-case）を消す。
- [x] ✅ W5-3 試験の書き直し（理由を下の表に）。

結果（`e6fd37c`）：
- FramePrintPDF は次元だけで体裁を決める（`PdfDocument.frameG = dimension == 2`、`FrameGInputLayout.Applies`、`DiagramInput`、`CanonicalSectionDiagram`）。`PrintData.referenceResultLayout`・`ReferenceResultLayout.Enabled`・各 `Result*` の `referenceLayout` を消した。FrameWebforCS の投影（`PrintProjection`・`PrintResultMetadata`・`PrintDiagramProjection`）はフラグを付けない。
- 横向き・A3 でも FrameG の座標・9.06 pt（`ReportFont`・`TableFont`・余白・頁頭）。横向きで 2 ケースを並べる体裁（`ReferenceResultLayout.Forces`、横向きの `Displacements`、`CaseTitle`、PICK UP の古い分岐）を消した。
- 正規の断面力図は FrameWebforCS が 2D でしか作らない（`PrintProjection.cs:702`）ので、従来の描画（`DrawCaption`・`Panel`・`DrawHeading`・`DrawForces`・`DrawDisplacements`・`LabelPlacement`・着目点の札）を消した。
- 図の頁の頭：`PdfDocument.BeginOwnHeaderPages(ref indexPage)`／`EndOwnHeaderPages()`。触っていない頁は置き換え、そうでなければ改頁し、頁頭（`TITLE :`）を出さない。荷重図の `BlankPage`（頁の中身を消す）と断面力図の `Pages.Remove` をこれに寄せた。
- 統合で見つけて直した不具合（`feb56ca`）：FrameG の部材バネの表が部材ごとに最初の行しか出さず、FrameWeb の部分区間（`length`）や同じ部材の複数行を黙って落としていた → そのときは全ての行を出す元の表にする（試験 `PartialSpringIntervalsKeepTheGenericTableWithEveryRow`）。FrameG の変位図の線が 0.4 m ごとの折れ点だけを通り、間の解いた点（着目点）の値と最大値を落としていた → 線は折れ点と解いた点の両方を通り、最大値もそこから採る。地盤バネの部材の値は見本どおり折れ点だけ（試験 `CanonicalDisplacementUsesSolvedInteriorStations…` の点を 1.0 m に戻した）。
- 確かめ：PDF_Manager.Tests 157/157、FrameWebforCS.Tests 615/616（既知の 1 件）、Headless.Tests 74/74（Debug）。電算出力と荷重図は統合前の出力と語・位置が全く同じ。断面力図は変位図の 4 頁だけ変わり、見本との数（1 mm 超・重ね画像）は同じ。PIK の比較の記録は統合前（`pik5.json`）と同一（27 行は FrameG 自身の丸め誤差）。

#### 統合（リーダー）

- [x] ✅ I-1 作業者ごとに差分を読み、試験と比較を流し直してから main に取り込み、コミット。
- [x] ✅ I-2 数値の後退が無いこと（電算出力の語・位置が統合前と同一、`pik_compare.py` の記録が `pik5.json` と同一）。
- [ ] I-3 DESIGN.md・報告書・GLOSSARY・README を更新。

### 状況

- 2026-10-29（続き）：W1〜W6・W5 を main に統合（`e6fd37c`・`97423df`・`feb56ca`）。データの落ちの有無を独立のレビューで確認中。

- 2026-10-29：計画書を作成。基準の出力 `.tmp/harajuku-0404-parity/20261029T000216/base/`（HEAD `f29add9`）：電算 261 頁（見本 128。COMBINE 変位の 73 頁と COMBINE・PICK UP の頁の増えを含む）、荷重図 11 頁（見本 7）、断面力図 44 頁（見本 45、付記 1 頁を除けば同数）。

### 新しく分かったこと

- 変換後の JSON に部材のﾏｰｸ（G・A・B1・B2・C）と着目点の名前が無い（`member` は `ni`・`nj`・`e` だけ、`notice_points` は `m`・`Points` だけ）。部材名は断面（`element`）の `n`。
- `PrintProjection.Build` は入力の節（`node`・`member`・`element`・`notice_points` など）を保存形式の JSON からそのまま写す（`PrintProjection.cs:222`）。保存形式に足した項目は印刷側で読めば届く。
- W5 の前提：`PDF_Manager` の `PrintInput` は FrameWebforCS（`DirectPdfGenerator.Generate`）のほかに Web 版 `FramePrintAzure/Function1.cs`・`Function2.cs` と `PDF_Test*` からも呼ばれ、Web 版の JSON にはフラグが無い。フラグを消して「2D なら常に FrameG」にすると Web 版の 2D が変わり、制約（Web 版は変えない）に反する。DESIGN.md 2026-10-28 の捨てた案に「FrameWebforCS だけが出すメタデータ無しで Web・3D に体裁を当てる」がある。→ 2026-10-29 にユーザーが決定：Web 版も含めて 2D は全て FrameG の体裁（入口で切り替える案は捨てた）。W5 ではフラグを消し、2D なら FrameG の体裁にする。Web 版のことは考えない。

- COMBINE・PICK UP の見本の寸法（pt。x は文字の左端、値は列の右端）：

  | 項目 | COMBINE | PICK UP |
  |---|---|---|
  | 見出し `COMBINE n  名称` | 88.6 | 91.9 |
  | 下線（太さ 0.12、文字の下端 + 1.1） | 81.5 から | 84.8 から |
  | 力の名 `MOMENT MAX.` | 276.6 | 266.4 |
  | 列見出し DIST. / M / S / N | 181.4 / 250.2 / 304.3 / 358.6 | 171.2 / 240.0 / 294.1 / 348.5 |
  | CASE の欄 | 393.6 | 383.5 |
  | 部材の見出し | 105.6 | 91.9 |
  | 着目点のラベル（右揃え） | 159.8 | 149.6 |
  | 値の右端 DIST / M / S / N | 202.9 / 267.5 / 321.6 / 376.0 | 192.7 / 257.4 / 311.4 / 365.8 |

  行の間隔 8.975 pt、見出し・力の名・列見出し・部材の見出し・部材の最初の行の間は 1.5 行。単位の行は無い。下線の右端は FrameG が比例幅で測っているらしく規則が一つに決まらないので「`COMBINE n` の幅 + 5.7」（誤差 0.7 pt 以内）。列見出しは COMBINE が `CASE`、PICK UP が `COM:    CASE`（空白 4 つ）。力の名は 6 文字の欄に左詰め（`SHEAR  MAX.`）。
- CASE の欄：DEFINE で「無し」を選んだもの（0）を省き、2 つ目から +・- を付ける。PICK UP は「採った COMBINE の番号 :その式の中のケース」。COMBINE は `caseStr`、PICK UP は `comb` の式（項ごとに最後の `*` の後）から作る。
- 見本は全部 1 頁に収まり頁送りの例が無い。変位量と同じ規則（残りに収まらない部材は次の頁から、1 頁より長い部材は見出しを繰り返す）にした。
- 罠：見本の値の文字は末尾に空白が付く（`"0.000 "`）。`layout_compare.py` は span の幅を字数で割るので、右端は「span の右端 − 1 字」で読む。試験の PDF は `fsecCombineName` が無いと COMBINE の節が出ない（`PrintData.cs:119`）。
- W5 で見ること：縦の条件を外すと `ReferenceResultLayout.ReportFont` が横向きで 10 pt にする。FrameG の体裁では向きに関係なく 9.06 pt に。古い横向きの分岐（`ReferenceResultLayout.Forces`）は `SHARE` のまま（W5 で消す）。

- 入力の写しの見本の寸法（pt、文字は基準線）：最初の見出し 87.5、行 8.975、半行の空きを入れた行 13.4625、頁の下の限り 782（778.6 まで印字）。節の間（前の最後の行から次の見出し）：格点→部材 31.5、部材→剛域 27、剛域→バネ 27、バネ→着目点 27、着目点→基本荷重 31.3、基本荷重→実荷重 27、実荷重→DEFINE 44.8、DEFINE→COMBINE 29.7、COMBINE→PickUp 31.5。行の間隔は DEFINE 11.66、PickUp 13.48、COMBINE は 2 行で 1 組。
- FrameG の入力の写しの振る舞い：格点の Y は y 上向きで出す（保存形式の 2D は y 下向きなので印字のときだけ符号を変える）。着目点には入力に無い剛域の端（部材 6・9 の 0.613、部材 10 の 0.100・4.650、名前「剛域」）を足す。基本荷重・DEFINE の表は行の途中で頁を変え見出しを出し直す。部材バネは TYPE 2 つの組を同じ頁に収める。実荷重はケースの最後の行 + 18 pt が入らなければ次の頁に送り、次の頁では「実荷重 DATA」を出さず種類の見出しを 96.5 から。通し番号は部材荷重と節点荷重で別に数える。ﾏｰｸは部材軸方向（x）が FrameWeb の mark + 4、`gy` は V、`gx` は H（P の符号を戻す）、`ms` は M/S の欄。値が 0 なら空欄。A・I が「0.0000」の欄に収まらない（剛域の 1000）ときは整数で左寄せ。COMBINE の割増係数は FrameWeb に無いので常に 1.0000。見本の「Ｃase」の C は全角。
- ﾏｰｸが無いので、ﾏｰｸが変わる行の前の半行の空きは「部材名（材料名 `n`）が変わる所」で空ける。この見本では空く所がﾏｰｸのときと同じ。
- ﾏｰｸは `.frd` の `Buzai.tmp`（1 行 244 バイト、Xp の後の 5 バイト。変換は `element.cs` で `[NonSerialized]`）、着目点の名前は `Chakumoku.tmp`（1 行 1053 バイト、454 バイト目から 12 バイトずつ）にある。保存形式に足してもデスクトップで開いて保存すると落ちる（`clsElement`・`clsNoticePoints` が決まった項目しか持たない）。決定（2026-10-29）で足さない。
- 支点・結合・シェルは見本に無いので、データがあるときだけ今の表のまま剛域の後・バネと着目点の後に出す（合否は判定しない）。節点荷重の F.X・M の列の右端（261.4・397.0）は推定。
- 罠：見本の 1 つの文字のかたまりに離れた見出しがつながっていることがある（「割増係数実荷重記号」など）。PyMuPDF の `rawdict` で 1 文字ずつの位置を出し、隙間で分けて位置を求める。文字は `XStringFormats.BaseLineLeft`/`BaseLineRight` で見本の基準線にそのまま置く。

- 中国語（`cn`）：入力の写しの見出しを中国語にした（`dfed8f9`。訳は既存の `case "cn":` の語を優先し、日本語の字が混じる既存の訳「要件荷重」「開始」「結尾」は「构件载重」「开始」「结尾」に）。ただしデスクトップの投影 `PrintProjection.ValidateSelection`（`PrintProjection.cs:253`）が ja・en 以外を拒むので、FrameWebforCS・Headless から cn は出せない（FramePrintPDF の中だけで動く）。`TITLE :` の行は `PdfDocument.cs` が MS 明朝で書く。

- 荷重図の頁の組み方は FrameG 自身の設定：`.frd` の `$16.txt` の 4 行目 `1.2`（小数点の前が段数で 1 なら 2 段書き、後ろが 1 頁 1 ケースにする先頭のケースの数）、7 行目 `40 40` は縮尺 1/40、11 行目 `1` はフォントが MS ゴシック（読み方は `KAJYU_ZU23.cs` の `K荷重図INIT_Frame_23`）。FramePrintPDF の上下 2 分割と `single_layout_cases` がもともと同じ仕組み。
- 荷重図の寸法（pt）：見出しは ` Ｃase 3  名称　記号`（MS ゴシック 9.66、x 78.0、基線 83.8／443.3）。最初の頁だけ ` 荷 重 図`（11.2 pt。見本では計算書の白い四角に隠れるが文字は残る）を出し見出しが 8.4 下がる。1 頁 1 ケースは横 335.28 に収め（1/40）中心 (312.48, 456.12)、2 段書きは横 223.56・縦 134.04 に収め中心 x 298.98、y 298.86／672.06。線は骨組 0.84、剛域の部材 1.44、剛域データの部分 1.68。見本の文字は送りが 1.04 % 広い（大きさを 1/1.0104、横を 1.0104² 倍）。
- 荷重図の帯の高さ（K = 16.62 pt、B は基準の荷重）：一様な荷重 h = K·1.2·(P/B)^0.2 / ln(B·10/ln10/P)、水平の部材で B/P > 3 なら (B/P)^0.2 倍。鉛直の部材の台形は (P/B)^0.8·1.2·1.3·1.3。B は部材に直角な荷重の最大値で、最も下の水平の部材の荷重を除く。M/S の対は高さに M、B に S を含める（`Ws=`・`Wm=` の 2 行）。積み方：帯と部材の隙間は水平 4.2・鉛直 6.84、寸法線は帯の外側から「帯の高さ + 6」（2 行目があれば + 10.7）、寸法線から次の帯まで 15.6、範囲が重なる荷重は外へ積む。範囲指定の荷重の寸法は全体の 1 本、1 本の部材の全長だけの荷重には寸法を付けない。
- 断面力図の寸法：題名 x 80.4・基線 89.2（8.86 pt）、ケースの見出し x 80.4・117.4（9.66 pt、先頭に半角空白）、図の題 8.16 pt、値 6.70 pt。図の領域は 135.3 から用紙の下 28.3 まで、上下 2 段（各 339.2）、図の題は段の上から 4.9・x 83.3、右の余白 39.9。骨組は 1:70（「段の 0.44 以内に収まる縮尺を分母 1 桁に切り上げ」が見本に合う推定）。骨組の線は剛域 1.08・ほか 0.72、曲線 0.48 で最大値を 10 mm。
- 断面力図の値を出す点：基本ケースは部材端と剛域の面（隣の部材と同じ節点では 2 つ重ねる）、COMBINE は端だけで最大・最小の大きい方（剛域の面は出さない）、M の部材内の極値は 1 つ（S・N には出さない）。値は曲線の端から 3.8 外（内側に読むなら 3.4）、梁は上向きの縦書き、壁は横書き。FrameG は重なった値をずらさない。変位図は剛域でない部分を 0.4 m ごと（最大 10 分割）、地盤ばねのある部材は折れ点ごと、ほかは i 端と最大値。
- 罠：`layout_compare.py` は同じ文字を並び順で対にするので、同じ値が並ぶ図では入れ違って大きなずれに見える（近い順の対も見る）。`margins.py` は見本の付記の白い四角も数える（作業者 C の `margins2.py` は白い塗りを除く）。解析は記号の無いケースに仮の記号 `LoadSymbol` を付けるので、図では出さない。荷重図のゴシックの PDF 上のフォント名が文字化けした名前になる（字形は同じ。既存の図も同じ、`JapaneseFontResolver` 側）。
- W5 で整理すること：荷重図は頁頭の `TITLE :` を頁の中身を消して白紙に戻し（`BlankPage`）、断面力図は最初の頁を `Pages.Remove` で取り除いて作り直している。`PdfDocument` に図の頁の頭を持たせる正式な口を作って両方をそれに寄せる。

### 設計の考え方

- 作業者の新しい体裁は、`ReferenceResultLayout.FrameGDisplacements` と同じく「見本の座標（pt）を定数で持ち、右揃えの値を列の右端に置く」書き方にする。表の自動の幅の計算（`Table`）は使わない。

### 書き直した試験と理由

| 試験 | 理由 |
|---|---|
| `TransverseReportCompositionTests`：`ReferenceDisplacementPairsAdjacentCasesWithoutRepeatingValues`・`ReferenceForcesPairCasesAndRepeatMemberCaptionAfterPageBreak`・`ReferencePickupPairsSignedModesWithCorrelatedSourcesAndExpressions`・`ReferenceTwelveNodeDisplacementUsesThreePairsPerPage`・`WeightedPickupExpressionUsesMeasuredSourceWidthWithoutUnnecessaryWrap`（W5、消した） | 横向きで 2 ケースを並べる体裁と係数つきの PICK UP の欄を確かめていた。その体裁は消した（決定 Q2）。FrameG の変位量・断面力・COMBINE／PICK UP は `FrameGEnvelopeLayoutTests` と同じファイルの縦の試験が確かめる。 |
| 同 `LandscapeTableAndDiagramFontKeepTheirOriginalSizes` → `LandscapeTableUsesFrameGFontAndDiagramFontKeepsItsSize`（W5） | 横向きも FrameG の文字（9.06 pt）と頁番号の無い頁頭。図の文字 8 pt はそのまま確かめる。 |
| 同 `PortraitReferenceGlobalTitleMatchesReferenceAndSuppressesPageNumber`（W5） | フラグを切った 2D の頁頭はもう無い（3D は `ThreeDimensionalInputKeepsLegacyTitleAndTableFont` が確かめる）。横向きでも左上から同じ距離に `TITLE :`、頁番号なしを確かめるように。 |
| 同 `ReferenceLongCaseCaptionWrapsWithinItsPanelAndRetainsText`（W5） | 横向きも FrameG の `CASE  1  名称`。横向きの広い欄でも折り返すように題名を長くした。 |
| 同 `LongMemberCaptionWrapsAndRepeatsEveryLineOnContinuationPages`（W5） | 2 ケースを並べた左右の欄から、FrameG の 1 ケースの部材の見出しの欄（146.1 pt）に。頁ごとに見出しを繰り返すことは同じ。 |
| 同 `ShortNodeIdsLeaveRoomForJointHeadingInBothBaseAndCombinationPanels`（W5） | FrameG は `JOINT` と `NO.` を別の行に書く。`JOINT` が `X-DISP.` に重ならないことを、基本ケース 1 つと COMBINE の左右 2 つで確かめる。 |
| `FrameGInputLayoutTests.WithoutTheReferenceFlagTheGenericTablesRemain` → `ThreeDimensionalInputKeepsTheGenericTables`（W5） | フラグが無くなったので、元の表が残るのは 3D。 |
| `FrameGLoadDiagramTests.OtherLayoutsKeepTheEarlierLoadDiagram`（W5、消した） | 2D の従来の荷重図はもう無い（2D は全て FrameG）。 |
| `CanonicalSectionDiagramTests` の 10 件（W5、作業者 D） | 全て残して FrameG の体裁の期待に書き直した。頁数・空白頁が無いこと・表の後の図が次の頁から・データの検査・数値を変えないこと・縮尺の明示・曲線の側（N は Q10）・節点の選択が札だけを絞ることは同じ。名前を変えたもの：`…UsesOneScaleForBothAxes…`（FrameG は両軸で縮尺が一つ）、`…FollowsTheMemberNormalAndKeepsNegativeLabels`、`CanonicalBasicCaseLabelsEveryMemberEndWhileAnEnvelopePrintsAnEqualValueOnceAtOnePlace`（従来の「近くの同じ値を間引く」は FrameG では基本ケースと包絡で振る舞いが違う）。消した Assert は `…RoundoffOscillationFlood` の「0.04 が 1 つ」だけ（FrameG は部材内の極値を大きい方 1 つしか書かない。代わりに 0.04・-0.04・9.99 が出ないことを確かめる）。 |
| `TransversePrintMetadataTests`・`SectionDiagramProjectionTests`（W5） | 投影がフラグ（`referenceResultLayout`・`referenceLayout`）を付けないことを確かめるように。 || `CanonicalSectionDiagramTests` の 10 件（W4） | 共通の `Config` を `referenceLayout = false` にして従来の体裁の試験として残した（`TITLE :`・軸ごとの縮尺・ラベルの間引きを前提にしている）。FrameG の体裁は `FrameGSectionDiagramTests` で確かめる。W5 で従来の体裁を消すときに見直す。 |
| `SectionDiagramProjectionTests.DiagramContractPreservesTableStationsAndAddsRawGeometricDisplacements`（W4） | COMBINE・PICKUP の見出しを見本の `COMBINE 1 …`・`PICKUP 1 …` に。 |
| `TransverseReportCompositionTests` の `PortraitReferenceInputOnlyNodeTableUsesReferenceBodyFont`・`PortraitReferenceMixedInputAndReactionUsesSharedFontOnLaterPage`・`PortraitReferenceMixedInputAndForceUseSameFontAcrossPages`（W1） | 2D・縦の入力の写しが FrameG の見出し（en では `NODE DATA`）になったので、探す文字を `Node Data` から `NODE DATA` に。確かめること（参照のフォントの大きさ）は同じ。 |
| `TransverseReportCompositionTests.PortraitPickupReferenceCaptionAndColumnsUseCentralGrid`（W2） | 以前の見本の CASE の書き方 `(6:1.1*1+1.367*4+5)` を前提にしていた。今回の見本の `( 6 :1+4+5 )` に直した（位置の範囲はそのまま）。 |
| `TransverseReportCompositionTests.PortraitPickupUsesSeparateOrderedExtremumFlowsAndPrintsSelectedCases`（旧 `…AndPreservesWeightedSources`、W2） | 今回の見本は係数を出さず、`SHARE` を `SHEAR` にする。`( 6 :1+4+5 )`・`SHARE` が出ないこと・`SHEAR  MAX.` を確かめる。 |
| `TransverseReportCompositionTests.PortraitMinOnlyPickupKeepsCorrelatedVectorAndWrappedSourceAcrossPages`（W2） | CASE を `( 77 :CASE_A001+…+CASE_A070 )` に。CASE の欄が x = 383.5 になったので折り返しを拾う下限を 385 → 380、列見出しを `COM:    CASE` に。頁をまたぐ折返しは今も確かめる。 |

### Tips

- COMBINE・PICK UP の 108 頁は `tools/envelope_compare.py <見本> <denzan.pdf> [--detail]` で一度に比べられる（見出しで頁を対応させ、行を丸め・同点・その他に分ける。4 秒）。
- 比較の道具は `.tmp/harajuku-0404-parity/tools/`。PyMuPDF は `uv run --no-project --with pymupdf --with pillow python -X utf8 -I tools/<script>.py ...`（`-I` は `PYTHONIOENCODING` を無視するので `-X utf8` が要る）。PowerShell では `[Console]::OutputEncoding=[Text.Encoding]::UTF8` も。
- PDF を作る：`dotnet build .tmp/harajuku-0404-parity/harness-src/Verification.csproj` の後、`.tmp/harajuku-0404-parity/harness-src/bin/Debug/net10.0-windows/FrameWebforCS.Tests.exe "<R>/source/0404_完成時.frd" "<R>/<out>" "0404_完成時FrameG"`。
