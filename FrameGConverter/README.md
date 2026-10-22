# FrameGConverter

FrameG のデータファイル（`.frd`）と FrameWeb の入力 JSON を相互に変換するライブラリ（`Convert_Manager`）と、
その試験画面（`Convert_Test`、実行ファイル名 `FrameGConverter.exe`）。

## .frd → JSON（`ConvertManager`）

`new ConvertManager(stream).getJsonString()`。FrameWebforCS は `.frd` を開くときにこれを使う
（`FrameWebforCS/providers/InputFileLoader.cs`）。呼ぶ側で `CodePagesEncodingProvider` を登録しておく。

## JSON → .frd（`Convert_Manager.FrameG.FrdExporter`）

```csharp
FrdExportResult result = FrdExporter.Export(json, new FrdExportOptions { Title = "門型ラーメン" });
if (result.Succeeded)
    File.WriteAllBytes(path, result.Frd);
foreach (FrdDiagnostic diagnostic in result.Diagnostics)   // エラーと警告。Code・Path（JSON の位置）・Message
    Console.WriteLine(diagnostic);
```

- 入力は FrameWebforCS が保存する 2D の JSON（`dimension: 2`）。`ConvertManager` の出力も読めるが、
  `dimension` が無いので足してから渡す。
- FrameG で表せないもの（3D、パネル、橋面・空間荷重、強制変位、移動荷重 LL、列車荷重、区間を指定した部材ばね（杭ばね）、
  シートで E・Xp が違う材料、欄に収まらない値など）は `FRD_*` のエラーになり、`.frd` を作らない。
- 書き出せても FrameG に残らないもの（番号の振り直し、3D の項目、長すぎる名前、DEFINE の名前など）は警告になる。
- 節点・部材・荷重ケースは 1 からの連番に振り直し、対応を `NodeNumbers`・`MemberNumbers`・`LoadCaseNumbers` で返す。
- 材料は部材ごとに 1 つ。`gy`・`gx` の荷重は mark 1/2 ＋ V/H、P の符号を反転して書く。
- 書出しの決まりは `.claude/docs/DESIGN.md`（2026-10-22 の行）、計画と経緯は
  `.claude/docs/plans/frameg-json-to-frd-export.md`。

Headless runner からは `FrameWebforCS.Headless.exe export-frd`（`FrameWebforCS/Headless/README.md`）、
試験画面からは「JSON → FRD」ボタンか `.json` のドロップで使える。試験画面は同じ名前の `.frd` があると
上書きせず `名前_FrameWeb.frd` に書く。

## 確かめ方

```powershell
dotnet test FrameGConverter/Convert_Manager.Tests/Convert_Manager.Tests.csproj -c Release
dotnet build FrameGConverter/FrameGConverter.sln -c Release
# 書き出した .frd を独立した復号器で確かめる（標準ライブラリだけ）
uv run --project FrameWeb --locked --extra dev python FrameGConverter/tools/lzh_verify.py <file.frd>
```

`.frd` は LZH（レベル 0 ヘッダー、`-lh5-`・`-lh0-`）の書庫で、中身は Shift-JIS の固定長レコード（`*.tmp`）と
タブ区切りの表（`$N.txt`）。`Convert_Test/TestData` の見本のうち、杭のないものは `4_2-FrameG.frd`（FrameG 4.6.2）だけ。
