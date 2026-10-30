# 1 ケースの旧 JSON を FemModel で解き、時間の内訳を出す。使い方: uv run --project FrameWeb --locked python profile_case.py <case json>
import cProfile, pstats, sys, time
from fem import FemModel
m = FemModel()
t = time.perf_counter(); m.load_model(sys.argv[1]); print("load", round(time.perf_counter() - t, 2))
pr = cProfile.Profile(); pr.enable(); t = time.perf_counter()
res = m.run("static")
print("run", round(time.perf_counter() - t, 2))
pr.disable(); pstats.Stats(pr).sort_stats("cumulative").print_stats(25)
