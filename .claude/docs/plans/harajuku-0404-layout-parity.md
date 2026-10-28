## 計画書：原宿ボックス 0404 の印刷の体裁を FrameG の見本に合わせる（残りの作業）

### 目的

FrameG（WinFrameG 4.3.6）の見本 `FrameGConverter/Convert_Test/TestData/原宿ボックス_0404/` を、FrameWebforCS で計算して FramePrintPDF で印刷した帳票の**既定の体裁**にする（`.claude/docs/DESIGN.md` 2026-10-28）。
これまでの記録は `.claude/docs/research/harajuku-0404-parity-report.md`。この計画は、その残り（入力の頁・COMBINE と PICK UP の数表・荷重図・断面力図・フラグの撤去）を扱う。

### 制約（合意済み。蒸し返さない）

- 流れ：`.frd` →（`ConvertManager`）→ 保存形式の JSON → 計算 → `PrintProjection.Build` → `DirectPdfGenerator`。
- 比べ方：計算書の付記（`4-n`・`5-n`、章・節の見出し、断面力図の 23 頁）は除く。頁割り・文字列・フォント・色は一致、位置は ±1 mm、加えて頁を重ねた目視。
- 余白も見本に合わせる。文字の頁は左 71.6 pt・行の下端 ≈ 778.6 pt（`PdfDocument.FrameGLeft`・`FrameGBottom`）。図の頁は W3・W4 で決める。
- 文字は MS 明朝 9.06 pt、数表の行の間隔 9 pt。
- FrameG の版の文字列（`DataVer 4.3.6`・バナー）は出さない。`SHARE` は `SHEAR` で出す。
- ja は見本を再現、en は同じ配置で日本語の文字だけ英訳。3D・Web 版（FramePrintAzure）は変えない。
- 符号は変えない（2D は y 下向き。数表・PIK は M = mz、S = −fy、N = fx）。COMBINE の変位（T5）と値の最後の桁の丸め（N5）は対象外。
- 試験を弱めない。以前の見本（線路直角方向）を前提にした試験を直すときは、試験ごとの理由をこの計画書に書く。既知の失敗 `ResultPageActivationTests.CombinedPagePublishesWhenInitiallyShownAfterRouteSelection` は無視。
- Git：`.claude/skills/headless-runner/SKILL.md` の未コミットの変更はユーザーのもの（含めない・戻さない）。push しない。共用の Release の Headless exe は作り直さない（`-c Debug`）。

### 完了条件

1. この計画書の全項目に ✅、知見・設計の考え方・Tips がある。
2. `denzan.pdf` が `見本_電算出力.pdf` と同じ頁割り（T5 の COMBINE 変位の頁を除く）で、入力の写し・MEMBER FORCES・変位量・COMBINE・PICK UP の全頁で対の語の位置差が 1 mm 以内、文字列・フォント・色が一致（例外：N5、`SHARE`→`SHEAR`、T5、付記）。
3. `load-diagram.pdf` が 7 頁・同じケースの組で、文字は 1 mm 以内、線・矢印・寸法は重ねた画像で一致。
4. `section-diagram.pdf` が 44 頁（付記の 23 頁を除く）で頁割り・頁頭・色が一致、図は重ねた画像で一致。
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
| C | W3 荷重図 | `C:\tmp\fp-wt\C` / `wip/harajuku-w3-load` |
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

- [ ] W1-1 格点 DATA：縦 1 列（No・X・Y）。
- [ ] W1-2 部材 DATA：格点番号・部材長・ﾏｰｸ・部材名・部材数・E・膨張係数・A・I（材料の表は別に出さない）。
- [ ] W1-3 剛域データ（部材 DATA（剛域データ））。
- [ ] W1-4 部材バネ DATA：TYPE ごとに 2 列（Kv・Ku）。
- [ ] W1-5 着目点：名前（左壁前面など）。`.frd` にあり変換で落ちているなら変換を直す。
- [ ] W1-6 基本荷重 DATA：構造系条件の欄。
- [ ] W1-7 実荷重：V/H・M/S の欄と通し番号「(...1)」。
- [ ] W1-8 DEFINE、COMBINE（係数を下の行に）、PickUp。
- [ ] W1-9 頁割り 4 頁、右端からはみ出さない。

確かめ方：`layout_compare.py 見本 1..4 出力 1..4 --detail` で 1 mm 超 0、`margins.py`。試験 `dotnet test FramePrintPDF/PDF_Manager.Tests`、変換に触れたら `FrameGConverter/Convert_Manager.Tests`。

#### W2 COMBINE・PICK UP の数表（作業者 B）　見本 21〜128 頁

- [ ] W2-1 1 つの COMBINE／PICKUP につき MOMENT・SHEAR・AXIAL の MAX・MIN の 6 頁（各 1 頁）。
- [ ] W2-2 CASE の欄 `( 1+2+3+4+5+8-10 )`、PICK UP は `( 2 :1+2+3+4+8-10 )`。
- [ ] W2-3 見出し（`COMBINE  1  名称`、下線、`MOMENT MAX.`、列見出し `DIST. M S N CASE` / `COM: CASE`）の位置。
- [ ] W2-4 `SHARE` を `SHEAR` に（見本との比較ではこの語だけ例外）。

確かめ方：`page_map.py` で COMBINE・PICK UP の全頁が 1 対 1、各頁 1 mm 超 0。

#### W3 荷重図（作業者 C）　見本 `見本_荷重図.pdf` 1〜7 頁

- [ ] W3-1 頁の組：1 頁に 1〜2 ケース（3・4、5・6、7・8、9・10 を同じ頁に）。組み方の規則を見本から決める。
- [ ] W3-2 頁頭の題名を出さず、左の余白に回転した `題名 P n`（`(DataVer 4.3.6)` は出さない）。
- [ ] W3-3 ケースの見出し `Case 3  名称  記号` の位置・フォント。
- [ ] W3-4 荷重の値と寸法を図の外に横書き。部材軸方向の分布荷重は `q =11.00`。節点荷重の矢印と値。
- [ ] W3-5 図の大きさ・位置・余白。

確かめ方：文字は `layout_compare.py`、線・矢印は重ねた画像（差分画像の道具を足す）。

#### W4 断面力図（作業者 D）　見本 `見本_断面力図.pdf` 1〜22・24〜45 頁

- [ ] W4-1 S1 頁頭は題名だけ（`TITLE :` なし）、`Case 1 固定死荷重:D1`（記号つき）。
- [ ] W4-2 S2 COMBINE の頁に `COMBINE 1 名称`。
- [ ] W4-3 S3 図を小さめ、値は部材端・最大値など限られた点だけ小さな文字。
- [ ] W4-4 S4 文字は全て青、題は細字。
- [ ] W4-5 S5 軸力図を閉じた骨組の内側に描く。
- [ ] W4-6 図の頁の余白。

確かめ方：頁頭の文字は `layout_compare.py`、図は重ねた画像。

#### W5 `referenceResultLayout` の撤去（リーダー、W1〜W4 の統合後）

- [ ] W5-1 `PrintProjection.cs` が付けるフラグと `PrintData.referenceResultLayout` を撤去し、2D は常に FrameG の体裁。
- [ ] W5-2 `PdfDocument.cs`・`PrintInput.cs`・`PrintResult/*.cs`・`PrintResultMetadata.cs` の分岐を整理。横向きの対の体裁（landscape paired-case）を消す。
- [ ] W5-3 試験の書き直し（理由を下の表に）。

#### 統合（リーダー）

- [ ] I-1 作業者ごとに差分を読み、試験と比較を流し直してから main に取り込み、コミット。
- [ ] I-2 数値の後退が無いこと（`member_forces.py`、`pik_compare.py`）。
- [ ] I-3 DESIGN.md・報告書・GLOSSARY・README を更新。

### 状況

- 2026-10-29：計画書を作成。基準の出力 `.tmp/harajuku-0404-parity/20261029T000216/base/`（HEAD `f29add9`）：電算 261 頁（見本 128。COMBINE 変位の 73 頁と COMBINE・PICK UP の頁の増えを含む）、荷重図 11 頁（見本 7）、断面力図 44 頁（見本 45、付記 1 頁を除けば同数）。

### 新しく分かったこと

- 変換後の JSON に部材のﾏｰｸ（G・A・B1・B2・C）と着目点の名前が無い（`member` は `ni`・`nj`・`e` だけ、`notice_points` は `m`・`Points` だけ）。部材名は断面（`element`）の `n`。
- `PrintProjection.Build` は入力の節（`node`・`member`・`element`・`notice_points` など）を保存形式の JSON からそのまま写す（`PrintProjection.cs:222`）。保存形式に足した項目は印刷側で読めば届く。

### 設計の考え方

- 作業者の新しい体裁は、`ReferenceResultLayout.FrameGDisplacements` と同じく「見本の座標（pt）を定数で持ち、右揃えの値を列の右端に置く」書き方にする。表の自動の幅の計算（`Table`）は使わない。

### 書き直した試験と理由

| 試験 | 理由 |
|---|---|

### Tips

- 比較の道具は `.tmp/harajuku-0404-parity/tools/`。PyMuPDF は `uv run --no-project --with pymupdf --with pillow python -X utf8 -I tools/<script>.py ...`（`-I` は `PYTHONIOENCODING` を無視するので `-X utf8` が要る）。PowerShell では `[Console]::OutputEncoding=[Text.Encoding]::UTF8` も。
- PDF を作る：`dotnet build .tmp/harajuku-0404-parity/harness-src/Verification.csproj` の後、`.tmp/harajuku-0404-parity/harness-src/bin/Debug/net10.0-windows/FrameWebforCS.Tests.exe "<R>/source/0404_完成時.frd" "<R>/<out>" "0404_完成時FrameG"`。
