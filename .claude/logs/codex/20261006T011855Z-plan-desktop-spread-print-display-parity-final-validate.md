判定は **NEEDS_REVISION（修正後に実装可能）** です。書式と単位変換の分離、2D/3Dの対応、100,000行の性能、既存変更の保護は適切です。次の3点を補強してください。

1. **[P1] カルチャ依存のPDF再解析をオラクルに含める必要があります。**  
   [計画:20](C:/Users/sasai/Documents/structural-mcp/FEMPython/.agents/docs/plans/desktop-spread-print-display-parity.md:20) の数値直接比較だけでは、実際の入力経路を検証できません。PDF側のL1は文字列で、`printManager.toString` が現在カルチャで再解析します。例えば `de-DE` では `"1.25"` が125として解釈され得ます。保存文字列→PDFテーブルまでのテストを追加し、既存PDFの誤解析を再現するのか、対象外として記録するのかを明示してください。画面の数値解釈を変えて一致させるべきではありません。

2. **[P2] 既存の丸め契約テストとの競合が未整理です。**  
   [計画:36](C:/Users/sasai/Documents/structural-mcp/FEMPython/.agents/docs/plans/desktop-spread-print-display-parity.md:36) は既存フォーマッターの変更を許容しますが、[ResultCombineFsecCtParityTests.cs:12](C:/Users/sasai/Documents/structural-mcp/FEMPython/FrameWebforCS/tests/ResultCombineFsecCtParityTests.cs:12) は `7.625 → "7.63"` を要求しています。PDFの.NET `F2` は `"7.62"` となるため競合します。影響する既存テストを変更対象と検証対象に加え、表示契約の変更と、維持する集計値・出典の検証を区別してください。

3. **[P2] 最終ゲートにPDFオラクルテストの実行がありません。**  
   [計画:41](C:/Users/sasai/Documents/structural-mcp/FEMPython/.agents/docs/plans/desktop-spread-print-display-parity.md:41) はdesktopテストとビルドのみです。新設する `PrintNumericFormatOracleTests` の実行と、変更したヘルパーを使う既存回帰テストの実行を明記してください。desktop側の固定期待文字列だけでは、PDFとの一致を保証できません。

ソース照合による読み取り専用レビューです。ファイル変更、ビルド、テスト実行は行っていません。
