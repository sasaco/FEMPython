"""Render and verify documentation derived from fem/capabilities.json."""
from __future__ import annotations

import argparse
from pathlib import Path
import sys


ROOT = Path(__file__).resolve().parents[2]
SRC = ROOT / "src"
if str(SRC) not in sys.path:
    sys.path.insert(0, str(SRC))

from fem.capabilities import get_capability_registry  # noqa: E402


START = "<!-- capability-matrix:start -->"
END = "<!-- capability-matrix:end -->"
DOCUMENTS = (ROOT / "README.md", ROOT / "docs" / "wiki" / "elements.md")
STATUS_LABELS = {
    "verified": "検証済み",
    "implemented": "実装済み（個別検証未完了）",
    "unsupported": "**未対応**",
}


def _status(entry):
    return STATUS_LABELS[entry["status"]]


def _available(entries, labels):
    values = []
    for key, entry in entries.items():
        if entry["status"] == "unsupported":
            continue
        suffix = "検証済み" if entry["status"] == "verified" else "実装済み"
        values.append(f"{labels[key]['label_ja']}（{suffix}）")
    return "、".join(values) or "—"


def render_capability_matrix() -> str:
    registry = get_capability_registry()
    analyses = registry["analysis_types"]
    lines = [
        START,
        "この表は配布物に含まれる`fem/capabilities.json`から生成します。",
        "「実装済み」は経路が存在するものの、この組合せに対する個別の力学検証が未完了であることを示します。",
        "荷重・結果欄にない項目は未対応です。JSON正本では未対応項目と理由も明示しています。",
        "空間線荷重・空間面荷重は線形静解析のみです。明示局所平面・全体方向ベクトル・面法線、非凸外周・宣言した穴に対応します。省略時は全体XY／+Zです。",
        "非線形支持ばね（スリップ、JRSNAP互換バイリニア・トリリニア）は材料非線形解析のみです。全体座標の並進3方向・6DOFモデルの回転3方向へ指定できます。支持則ごとの検証状態と組合せはJSON正本の`support_types`を参照してください。",
        "",
        "| 要素（公開名） | 節点数 | 線形静解析 | 材料非線形解析 | 固有値解析 | 質量行列 | 利用できる荷重 | 取得できる結果 |",
        "|---|---:|---|---|---|---|---|---|",
    ]
    for canonical, capability in registry["elements"].items():
        aliases = [alias for alias in capability["aliases"] if alias.islower()]
        public_names = "、".join(f"`{name}`" for name in [canonical, *aliases])
        nodes = "、".join(str(value) for value in capability["node_counts"])
        analysis_statuses = [
            _status(capability["analyses"][name]) for name in analyses
        ]
        loads = _available(capability["loads"], registry["load_types"])
        results = _available(capability["results"], registry["result_types"])
        lines.append(
            f"| {public_names} | {nodes} | {' | '.join(analysis_statuses)} | "
            f"{_status(capability['mass_matrix'])} | {loads} | {results} |"
        )
    lines.extend(["", "状態と全aliasを含む機械可読な定義はPythonからも取得できます。", "", "```python", "from fem import get_capability_registry", "", "capabilities = get_capability_registry()", "```", END])
    return "\n".join(lines)


def _replace_block(source: str, rendered: str, path: Path) -> str:
    if START not in source or END not in source:
        raise ValueError(f"{path}: capability matrix markers are missing")
    prefix, remainder = source.split(START, 1)
    _, suffix = remainder.split(END, 1)
    return prefix + rendered + suffix


def expected_document(path: Path) -> str:
    source = path.read_text(encoding="utf-8")
    return _replace_block(source, render_capability_matrix(), path)


def check_rendered_documents() -> None:
    stale = [path for path in DOCUMENTS if path.read_text(encoding="utf-8") != expected_document(path)]
    if stale:
        names = ", ".join(str(path.relative_to(ROOT)) for path in stale)
        raise ValueError(
            f"Capability documentation is stale: {names}. "
            "Run python -m tools.validation.render_capabilities --write."
        )


def write_documents() -> None:
    for path in DOCUMENTS:
        expected = expected_document(path)
        path.write_text(expected, encoding="utf-8", newline="\n")
        print(f"Updated {path.relative_to(ROOT)}")


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--write", action="store_true", help="update generated blocks")
    args = parser.parse_args()
    if args.write:
        write_documents()
    else:
        check_rendered_documents()
        print("Capability documentation matches fem/capabilities.json.")


if __name__ == "__main__":
    main()
