実装順は次の6段階が妥当です。製品コードの実装は開始していません。

1. 出力契約をテストで固定  
   2D/3D、変位/反力、17列CSV、単位、全6成分の相関ベクトル、signed max/min、first-wins tie、行順を期待値として定義。実装前に意図した失敗を確認。

2. 純粋なCSVフォーマッターを実装  
   `Derived.Pickups.Displacements/Reactions` を使用し、PICKUP順→focus順→topology node順で出力。欠落ペア、重複/未知ノード、非有限値、station付きノード結果を拒否。  
   検証: formatterテスト単独合格。

3. 保存コマンドを分離  
   保存ダイアログ後に同じ `CalculationResultPresentation` が依然Currentか再確認し、同一ディレクトリの一時ファイルから原子的に置換。失敗時は既存ファイルを保持。  
   検証: stale snapshot、書込み失敗、置換、temp cleanupの自動テスト。

4. メニューを配線  
   既存の断面力出力を維持し、「PICKUP変位CSV」「PICKUP反力CSV」を追加。Derived再構築中は無効化。  
   検証: STAテストで項目の存在、enable/disable、Commit/Clear/再構築イベントを確認。

5. 統合回帰  
   `PickupNodeExportFormatterTests`、保存サービス、メニュー、既存 `PickupExportFormatterTests`、`CalculationDerivedPresenterTests` を実行。

6. コンポーネントゲート  
   `FrameWebforCS` build、全デスクトップテスト、`git diff --check`。既知の4失敗は新規回帰と分離。

固定するCSV契約は、UTF-8 BOMなし、変位・反力を別ファイル、先頭5列＋max/min各6成分です。2Dのfocusは変位 `dx,dy,rz`、反力 `fx,fy,mz`、ただし相関値は6成分すべてを保持します。

既存計画書は [pickup-node-result-exports.md](C:/Users/sasai/Documents/structural-mcp/FEMPython/.agents/docs/plans/pickup-node-result-exports.md:1) にあります。ただし現行版の保存ステップは目視検証のみなので、上記の段階3・4への分割が必要です。現在はread-only環境のため計画書の改訂と必須Codex検証を実行できず、正式な `PASS` はまだ付けていません。
