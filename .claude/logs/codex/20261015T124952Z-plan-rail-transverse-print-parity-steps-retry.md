リポジトリ全体を対象とする場合の検証順序です。PowerShellでリポジトリルートから実行します。

1. 差分と対象コンポーネントを確認する。

```powershell
git status --short
git diff --name-only
git diff --cached --name-only
git diff --check
```

2. Pythonの狭い関連テストを先に実行する。

```powershell
uv --directory FrameWeb run --locked --extra dev python -m pytest tests/path/test_file.py -q
```

3. Python全体ゲートを実行する。

```powershell
uv --directory FrameWeb run --locked --extra dev python -m pytest tests -q
```

4. Angularテストを実行する。

```powershell
npm --prefix FrameWebforJS run test -- --watch=false --browsers=ChromeHeadless
```

5. Angularプロダクションビルドを実行する。

```powershell
npm --prefix FrameWebforJS run build
```

6. .NETテストを実行する。

```powershell
dotnet test FrameWeb.sln
```

7. .NETソリューションをビルドする。

```powershell
dotnet build FrameWeb.sln
```

8. `FrameGConverter/` が明示的な対象に含まれる場合だけ、独立ゲートを追加する。

```powershell
dotnet test FrameGConverter/Convert_Manager.Tests/Convert_Manager.Tests.csproj
dotnet build FrameGConverter/FrameGConverter.sln
```

9. `.agents/`、`.codex/`、または `AGENTS.md` が対象なら、エージェント基盤ゲートを実行する。

```powershell
& .agents/check.ps1
```

10. 最後に差分を再確認する。

```powershell
git status --short
git diff --check
git diff
```

各失敗は「製品回帰」「既存失敗」「依存関係または権限などの環境失敗」に分けて記録し、未実行のゲートを成功扱いしません。ファイルは編集していません。
