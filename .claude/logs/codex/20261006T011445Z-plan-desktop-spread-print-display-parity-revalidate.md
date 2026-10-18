判定は **軽微な補強後に実装可能** です。桁数の対応、3D要素の `< 999` 分岐、単位変換の維持、既存変更の保護は妥当です。以下を計画に追記すると、受入判定が明確になります。

- **中：文字列の荷重長さの編集契約**  
  [計画のStep 2](C:/Users/sasai/Documents/structural-mcp/FEMPython/.agents/docs/plans/desktop-spread-print-display-parity.md:28) に、カンマ小数環境での表示→編集確定→保存・再読込→計算要求の検証を追加してください。[InputLoadService.cs](C:/Users/sasai/Documents/structural-mcp/FEMPython/FrameWebforCS/components/input/InputLoadService.cs:391) は L1/L2 を `InvariantCulture` で解析し、失敗すると表示用値を0にします。表示と保存文字列を分離する条件が必要です。

- **中：既存の集計前丸めと表示丸めの境界**  
  [Step 4](C:/Users/sasai/Documents/structural-mcp/FEMPython/.agents/docs/plans/desktop-spread-print-display-parity.md:36) の比較対象を「既存の集計後数値」と明記してください。[ResultCombineFsecCoordinator.cs](C:/Users/sasai/Documents/structural-mcp/FEMPython/FrameWebforCS/components/result/ResultCombineFsecCoordinator.cs:164) は集計前に力・長さを丸めます。表示だけの変更では、未丸めの元データから生成したPDFとの数値一致は保証できません。

- **低：テスト実行方法の具体化**  
  [Step 1](C:/Users/sasai/Documents/structural-mcp/FEMPython/.agents/docs/plans/desktop-spread-print-display-parity.md:20) にPDF oracleへのアクセス方法を指定してください。`printManager` は内部クラスで、公開先は `PDF_Manager.Tests` のみです。併せて、STA内のカルチャ設定・復元、既存singletonテストとの直列実行、負のゼロの期待文字列を明記すると安全です。

ファイルは変更していません。静的レビューを実施し、ビルド・テストは実行していません。
