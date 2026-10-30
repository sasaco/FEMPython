"""各ケースの旧 JSON を FemModel で解き、runner と同じ形の result.json にまとめる。
使い方: uv run --project FrameWeb --locked python drive.py <out result.json> <case json>...
ケース番号は <case json> の名前の末尾 "_case<N>.json" から読む (onecase.mjs の出力)。"""
import json, sys, time

from fem import FemModel

results = []
for path in sys.argv[2:]:
    case_id = path.rsplit("_case", 1)[1].split(".")[0]
    t = time.perf_counter()
    model = FemModel(); model.load_model(path)
    result = model.run("static")["results"][0]
    result["case_id"] = case_id
    results.append(result)
    print(f"case {case_id}: {time.perf_counter() - t:.1f} s", flush=True)
json.dump({"analysisResultSet": {"results": results}}, open(sys.argv[1], "w", encoding="utf-8"))
