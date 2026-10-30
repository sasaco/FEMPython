"""各ケースの旧 JSON を FemModel で解き、runner と同じ形の result.json にまとめる。
使い方: python drive.py <out result.json> <patch|nopatch> <case json>...
patch: 載荷メッシュが無い (domain_cells is cells) とき prepare_geometry の domain ループを当のセルだけにする（検証用、リポジトリは変えない）。"""
import inspect, json, sys, time
import fem.spatial_loads.validation as validation

if sys.argv[2] == "patch":
    source = inspect.getsource(validation.prepare_geometry)
    old = "for domain in panel.domain_cells or panel.cells:"
    assert source.count(old) == 1
    exec(source.replace(old, "for domain in ((cell,) if panel.domain_cells is panel.cells else panel.domain_cells):"), validation.__dict__)
    import fem.spatial_loads.assembler as assembler
    if hasattr(assembler, "prepare_geometry"):
        assembler.prepare_geometry = validation.prepare_geometry

from fem import FemModel
results = []
for path in sys.argv[3:]:
    case_id = path.rsplit("_case", 1)[1].split(".")[0]
    t = time.perf_counter()
    model = FemModel(); model.load_model(path)
    result = model.run("static")["results"][0]
    result["case_id"] = case_id
    results.append(result)
    print(f"case {case_id}: {time.perf_counter() - t:.1f} s", flush=True)
json.dump({"analysisResultSet": {"results": results}}, open(sys.argv[1], "w", encoding="utf-8"))
