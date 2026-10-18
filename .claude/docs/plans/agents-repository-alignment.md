## Implementation Plan: Agents Repository Alignment

### Purpose

他プロジェクトからコピーされた `.agents` を、FrameWeb3 の実体（Windows中心の Python/uv・Angular/npm・.NET モノレポ）に合わせて整理する。コピー元固有の状態・生成キャッシュ・無効な参照を除去し、残す汎用ルール／スキル／検証器が実際の構成を正しく検出・検証できる状態にする。

### Scope

- New files: `PROGRESS.md`、`.agents/repository.toml`（対象コンポーネントとgateのSSOT）、`.agents/check.ps1`（Windows用の正規チェック入口）、`.agents/skills/_shared/verify.ps1`（必要な場合）、`.agents/skills/init/migrate_repository_state.py`、`.agents/tests/test_detect_stack.py`、`.agents/tests/test_run_tests.py`、`.agents/tests/test_migrate_repository_state.py`、`.agents/tests/test_shared_script_contract.py`、選択したランタイムに必要な設定ファイル（例: `.claude/settings.json`）
- Modified files: `AGENTS.md`、`.agents/STATE.md`、`.agents/docs/DESIGN.md`、`.agents/INDEX.md`、`.agents/rules/{language,dev-environment,testing,security,tiers,cli-execution,codex-delegation}.md`、`.agents/skills/init/detect_stack.py`、`.agents/skills/_shared/run_tests.py`、`.agents/skills/_shared/verify.sh`、`.agents/skills/_shared/README.md`、`.agents/skills/catchup/collect_repo_state.py`、`.agents/skills/update-lib-docs/lib_inventory.py`、`.agents/hooks/lint-on-save.py`、`.agents/skills/codex-system/SKILL.md`、`.codex/config.toml`、および確定した共通起動規約に反する参照を `rg` で検出した `.agents` 内文書・スクリプト
- Conditional files: `CLAUDE.md`、`.claude/agents`、`.claude/skills`、`.claude/settings.json`、`.agents/agents/**`、`.agents/hooks/**`、`.agents/docs/CODEX_HANDOFF_PLAYBOOK.md`、`.agents/workflows/antigravity/**` は、Main Agent／マルチランタイム方針に応じて修正・維持・削除を決める
- Deleted files/content: `.agents/skills/**/__pycache__/*.pyc` 12件と空になった `__pycache__`、`.agents/STATE.md:23-294` の TickReplay 作業ブロック、`.agents/docs/DESIGN.md:36-126` の TickReplay/DuckDB/SMA/株取引固有記述、参照グラフ上不要と確定したランタイム固有資産
- Dependencies: 既存の `uv` と `FrameWeb/uv.lock`、`npm` と `FrameWebforJS/package.json`、.NET 8 と `FrameWeb.sln`、PowerShell。製品依存関係やグローバルCLIの追加・更新は行わない
- Out of scope: `FrameWeb/**`、`FrameWebforJS/**`、`FramePrintPDF/**`、`FrameGConverter/**`、`tools/**` の製品挙動・依存関係・APIの変更

### Implementation Steps

以下は、未決事項の確定、実行基盤の修正、状態文書の再構築、不要物の削除、総合検証の順で実施する。

#### Step 1: 方針と変更前ベースラインを固定する

- [ ] Main Agent、保持するランタイム、Claude Codeを残す場合のWindows discovery方式、`.codex` の既定sandbox方針をユーザー承認付きDecision Recordとして確定する
- [ ] `git status --short --untracked-files=all`、対象ファイル一覧、削除候補12件、主要統合ファイルのSHA-256を記録し、同時変更を上書きしない基準を作る
- [ ] `.agents` 内の各ファイルを「FrameWeb3固有」「再利用する汎用資産」「条件付きランタイム資産」「削除対象」に分類し、参照元・参照先を記録する
- [ ] commit `2ee50f4` 全体のrevertは行わず、`.agents` と直接の統合点だけを対象にする
- [ ] **STOP/APPROVAL GATE**: 上記Decision Recordとベースラインをユーザーが承認するまで、Step 2以降の編集・削除を開始しない

**Verification**: ベースライン取得を再実行して同じ対象一覧とハッシュになること、製品ディレクトリが変更対象に含まれていないこと、3つの必須決定に未回答がなく承認日時が記録されていることを確認する。

#### Step 2: 対象コンポーネントを明示し、スタック検出をWindowsモノレポ対応にする

- [ ] `.agents/repository.toml` に、対象を `FrameWeb/pyproject.toml`、`FrameWebforJS/package.json`、`FrameWeb.sln` とsolutionに列挙されたtracked `.csproj`、`scripts/smoke-local.py` に限定して記述する
- [ ] 探索・収集から `.git`、`.venv`、`node_modules`、`dist`、`bin`、`obj`、cache、vendor/submodule、`FrameWebforJS/src/assets/js/rxfire`、`FrameWebforJS/src/assets/js/paramquery`、および `tools/local-tools` の補助packageを明示的に除外する
- [ ] `.agents/skills/init/detect_stack.py` は設定済みrepoでは `.agents/repository.toml` を優先し、各宣言パスの実在と種類を検証する。未設定repo用fallbackは深さと除外規則を持つ有界探索にする
- [ ] Python/uv、Angular/npm、.NET/dotnetを別々のコンポーネントとして返し、manifestごとの作業ディレクトリ・検証コマンドを構造化して出力する
- [ ] `.github/workflows` はディレクトリの存在ではなくtracked workflowファイルがある場合だけCIとして報告する
- [ ] bootstrap検査をStep 1で承認したWindows discovery方式も表現できる契約へ変更する。ただし現状の空 `AGENTS.md` や疑似リンクは、このStepでは「検出できるinvalid状態」として扱い、まだ正常化しない
- [ ] 正常なFrameWeb3、manifest欠落、壊れたmanifest、除外対象だけにmanifestがある状態、空のworkflowディレクトリ、各discovery方式を扱う回帰テストを追加する

**Verification**: `uv run --project FrameWeb --locked --extra dev python .agents/skills/init/detect_stack.py --project-root .` が実stackを報告し、invalid bootstrapは原因別に報告できること。生成物・vendor・補助package・空のGitHub Actionsをコンポーネントとして誤検出せず、追加テストが通ること。

#### Step 3: ルートのエージェント・ブートストラップを正す

- [ ] 0バイトの `AGENTS.md` に、FrameWeb3のリポジトリ境界、言語規約、Python/Angular/.NETそれぞれの作業ディレクトリと正規コマンドを記載する
- [ ] Main Agentと保持ランタイムの決定に従い、`.agents/STATE.md` の `## Main Agent` を正す
- [ ] Claude Codeを残す場合は `CLAUDE.md`、`.claude/agents`、`.claude/skills` をWindowsで実際に機能する方式へ修正し、必要な `.claude/settings.json` を作成する。残さない場合は通常ファイルの疑似リンクとそれを前提にする記述を削除する
- [ ] `.codex/config.toml` の存在しない `TEMPLATE_DESIGN_LOG.md` 参照と、未承認の暗黙的な権限方針を修正する
- [ ] `.agents/rules/language.md` などから参照される見出し・パスがすべて実在するようにする

**Verification**: `AGENTS.md` の必須見出し、全参照パス、選択ランタイムのdiscoveryを検査し、修正済み `detect_stack.py` の stack と bootstrap の両項目がexit 0かつ選択方針どおりになることを確認する。

#### Step 4: 実行ルール、検証器、hookを実際のツールチェーンに揃える

- [ ] `.agents/rules/dev-environment.md` と `.agents/rules/testing.md` を、ルート単一Pythonプロジェクト前提からコンポーネント別コマンドへ書き換える。未導入の ruff、ty、marimo、poe を必須扱いしない
- [ ] `.agents/rules/security.md` の一律 `==` pin推奨を、配布パッケージの互換範囲は `pyproject.toml`、再現可能な開発環境は `uv.lock` で固定する現行方針へ合わせる
- [ ] `.agents/skills/_shared/run_tests.py`、`verify.sh`、`.agents/check.sh` をモノレポ認識に修正する。Windowsの正規入口を `.agents/check.ps1` とし、Unix版を残す場合は同じ検査集合になるようにする
- [ ] `.agents/skills/catchup/collect_repo_state.py` と `.agents/skills/update-lib-docs/lib_inventory.py` にネストしたmanifestと.NETプロジェクトの収集を反映する
- [ ] `.agents/hooks/lint-on-save.py` は、対象コンポーネントで宣言・利用可能なコマンドだけを実行し、hook本体の検証が終わるまでは有効化しない
- [ ] `.agents/skills/codex-system/SKILL.md` の毎セッション `claude update && npm install -g @openai/codex@latest` を削除し、必要なら明示的・手動の保守手順へ移す
- [ ] `rg` で検出した `python3`、`bash`、`mktemp`、Unix仮想環境有効化の実行例を、共通のPowerShell/uv入口または明示した任意Unix経路へ統一する

**Verification**: helper単位のテストで成功・コマンド失敗・ツール欠落・gate未検出を検証し、dry-runがPython、Angular、.NETの3系列を列挙すること。未導入ツールを起動しないこと。

#### Step 5: コピー元の状態を安全にリセットし、FrameWeb3の正規文書を作る

- [ ] `.agents/skills/init/migrate_repository_state.py` を、STATE/DESIGNの両方を入力し、候補を別ファイルへ出す `--dry-run`、個別の `--expect-state-hash` / `--expect-design-hash`、UTF-8厳格decode、文書contract検証、同一ディレクトリ一時ファイル＋atomic replace、途中失敗時の無変更保証を持つ専用移行器として実装する
- [ ] 移行器のテストで、コピー元ブロック除去、保持対象セクション、hash不一致、片方だけの書込失敗、再実行no-op、改行/UTF-8保持を検証する
- [ ] `.agents/STATE.md` のコピー元作業ブロックを専用移行器で除去し、FrameWeb3の薄いRepository Identityだけを作る
- [ ] `.agents/docs/DESIGN.md` の空テンプレートとTickReplay固有記述を専用移行器で置換し、README、各manifest、solution/project構成を根拠にFrameWeb3の目的・スコープ・主要コンポーネント・制約を記述する
- [ ] `PROGRESS.md` を新設し、今回の整備を最初の実在checkpointとして記録する
- [ ] 移行完了後の通常更新だけを既存typed writerへ戻し、古い `/init` やcheckpoint compactionだけでコピー元ブロックを除去しない
- [ ] UTF-8は.NET/Pythonの明示的UTF-8読込で検証し、PowerShell既定表示の文字化けだけを理由に再エンコードしない

**Verification**: まずdry-run candidateとdiffを承認し、直前hash一致時だけapplyする。移行器テスト、`validate_doc.py` の state-doc/design-doc、`load_context.py` を実行し、`missing=[]`、`unreadable=[]`、`design.placeholder=false`、`progress.entries>=1` を確認する。`STATE.md`、`DESIGN.md`、`PROGRESS.md` にコピー元キーワードや存在しないパスがないこと。

#### Step 6: 汎用資産とランタイム固有資産を整理する

- [ ] `.agents/INDEX.md`、skills、agents、hooks、handoff文書、workflowの参照グラフを作り、到達不能・重複・選択ランタイム非対応のものだけを削除する
- [ ] `context-loader`、`init`、`plan`、`feature`、`tdd`、`troubleshoot`、`checkpointing`、`design-tracker` と共有validator/writerは、Step 3〜5の回帰テストを通るものを保持する
- [ ] `.agents/workflows/antigravity/**`、未使用runtime agent、Claude専用hook、`CODEX_HANDOFF_PLAYBOOK.md` はStep 1の方針に従って保持または削除する
- [ ] 最終的に残る集合に合わせて `.agents/INDEX.md` と関連リンクを更新し、削除済みファイルへの参照をなくす

**Verification**: 各保持ファイルがINDEXまたは承認済みworkflowから到達可能で、各削除ファイルへの参照が0件であること。全Markdownリンクとskill参照先が解決すること。

#### Step 7: 生成キャッシュを削除し、再生成を防ぐ

- [ ] `.agents/skills/{_shared,catchup,checkpointing,simplify,team-execute,update-lib-docs}/**/__pycache__/*.pyc` 12件を削除し、空の `__pycache__` ディレクトリも除去する
- [ ] ルート `.gitignore` の既存 `__pycache__/` 規則が対象を覆うことを確認し、重複規則は追加しない
- [ ] キャッシュ不在を受入条件にする検証では `PYTHONDONTWRITEBYTECODE=1` を設定する
- [ ] `.agents/logs/**` は今回の監査ログと通常運用ログを区別し、削除ポリシーをINDEXに明記する。検証証跡は無断で消さない

**Verification**: `.agents` 配下の `*.pyc` と `__pycache__` が0件で、各helperのテストがbytecodeを書かずに通ること。

#### Step 8: 再生成耐性と製品非回帰を確認する

- [ ] 修正済みinit/design/state writerを固定入力で2回実行し、2回目がno-opになることを確認する
- [ ] checkpointは固定時刻のpreviewを比較し、適用時は履歴が追記されても `## Progress Tracker` が1件だけでコピー元状態を再導入しないことを確認する
- [ ] `.agents/tests/test_shared_script_contract.py` を実在する共有script契約の回帰テストとして作成し、`.agents/skills/_shared/README.md` の欠落参照を解消する。不要な契約ならREADME側の参照を削除し、その理由を記録する
- [ ] `.agents/tests` 全体、文書contract、リンク/参照、foreign-reference、キャッシュ不在を「必須agent gate」として常に実行する
- [ ] Step 1のbaselineと、`git diff --name-only`、`git diff --cached --name-only`、`git ls-files --others --exclude-standard` の和集合を比較し、今回新規の製品パスが1件でもあれば失敗するscope gateを `.agents/check.ps1` に実装する
- [ ] FrameWeb3のPython、Angular、.NET、ローカル統合smokeを個別に実行し、環境不足と実装不具合を分けて記録する
- [ ] 最終diffで製品ディレクトリの変更、テスト弱体化、placeholder、無関係な削除がないことを確認する

**Verification**: 必須/条件付きgateを次のように分けて実行する。

- 必須: `.agents/tests`、detector、state/design/plan contracts、参照解決、foreign-reference、scope isolation、cache absence、`git diff --check`
- セットアップ済み製品環境では必須: Python pytest、Angular buildとnon-watch ChromeHeadless test、`dotnet build`
- 環境依存で任意（未実施理由を記録）: 起動サービス・空きポート・ブラウザを要するlocal smoke

```powershell
uv run --project FrameWeb --locked --extra dev python -m pytest .agents/tests -q
uv --directory FrameWeb run --locked --extra dev python -m pytest tests -q
npm --prefix FrameWebforJS run test -- --watch=false --browsers=ChromeHeadless
npm --prefix FrameWebforJS run build
dotnet build FrameWeb.sln
FrameWeb\.venv\Scripts\python.exe scripts/smoke-local.py
& .agents\check.ps1
git diff --check
```

`rg -n -i 'tick.?replay|duckdb|minute-context|daily-context|stocks_daily|SMA25|SMA200|src/tickreplay' .agents/STATE.md .agents/docs/DESIGN.md PROGRESS.md` は「一致なし」（exit 1）を成功条件とする。

### Risks & Considerations

- 現行validatorは見出しの形だけを検証するため、構造上validでも内容が別プロジェクトという問題を検出できない。foreign-reference検査と実在パス検査を別gateにする
- `detect_stack.py` を直す前に `/init` を実行すると、空のstack情報で正規文書を再生成する恐れがある。Step 2完了前はinitを実行しない
- `.agents/repository.toml` や除外規則が曖昧だと、生成済み `obj`、依存package、補助toolを製品コンポーネントと誤認する。宣言パスを優先し、fallback探索を有界にする
- checkpoint compactionは既存の一部セクションを保持する設計なので、コピー元STATEの除去手段として使わない
- WindowsのGit symlinkは権限・`core.symlinks` 設定に依存する。Claude Codeを残す場合は実機discovery試験を必須にする
- Bash版とPowerShell版を両方残すと検査内容が乖離しやすい。Windows専用と決めるなら未検証のBash入口は削除し、クロスプラットフォーム維持なら同一fixtureで両方を検証する
- Angularテストはwatchを無効化しChromeHeadlessを明示する。ローカルsmokeはポート・ブラウザ・サービス前提を満たさない場合があるため、失敗理由を環境不足と回帰に分ける
- コピー元固有状態の削除と同時に有用な汎用skillをまとめて削除しない。削除は参照グラフと選択ランタイムを根拠に個別承認する
- PowerShellの既定文字コード表示はUTF-8テキストを文字化け表示することがある。バイト列の厳格UTF-8 decodeが成功する限り、表示だけを根拠にファイルを書き換えない
- Codexの手順レビューは応答artifactを生成したがwrapperがtimeoutしたため、正式なPASSとして扱わない。計画内容は監査証拠と独立確認を優先する

### Open Questions

- **Step 1のSTOP/APPROVAL GATEで回答必須**: Main Agentを `Codex` にするか `Claude Code` にするか。推奨初期値は、現在の利用実態に合わせた `Codex` 主体で、必要性が確認できた場合だけClaude統合を残す構成
- **Step 1のSTOP/APPROVAL GATEで回答必須**: マルチランタイム構成（`.agents/agents`、hooks、Claude discovery、handoff playbook）を維持するか、Codex中心に縮小するか
- **Claude統合を残す場合、Step 1で回答必須**: Windowsで真のsymlinkを要求するか、通常ファイル／設定ベースのdiscoveryを正式対応として実装するか
- Step 6まで延期可能: `experimental/inactive` の `.agents/workflows/antigravity/**` を将来用に残すか削除するか。推奨は、利用予定がなければ削除
- Step 4で決定: Unix/Bash用の `.agents/check.sh` と `verify.sh` をPowerShell版と併存・同等検証するか、Windows正規入口だけに整理するか
