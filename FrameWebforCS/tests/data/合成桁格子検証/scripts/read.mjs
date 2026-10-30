// result.json から主桁の曲げモーメント (J1, 中央) と支点反力を取り出す。
// 使い方: node read.mjs <result.json> <map.json>  (result.json の隣に summary.json を書く)
import fs from "node:fs";

const r = JSON.parse(fs.readFileSync(process.argv[2], "utf8")).analysisResultSet;
const map = JSON.parse(fs.readFileSync(process.argv[3], "utf8"));
const { NS, STATIONS, girderMembers, cross } = map;
const nid = (line, s) => line * NS + s + 1;
const sOf = (x) => STATIONS.indexOf(x);

const out = [];
for (const res of r.results) {
  const forces = new Map(res.member_section_forces.map((m) => [m.member_id, m.segments]));
  const momentAt = (g, x) => {
    const s = sOf(x);
    const segs = forces.get(String(girderMembers[g][s]));     // member starting at station s
    return -segs[0].i_end.my;                                   // 下側引張を正
  };
  const reac = new Map(res.support_reactions.map((x) => [x.node_id, x.components.fz]));
  const row = { case: res.case_id };
  for (const g of [1, 2]) {
    row[`G${g} J1`] = momentAt(g, 10.25);
    row[`G${g} 中央`] = momentAt(g, 16.5);
    row[`G${g} 反力`] = reac.get(String(nid(g, 0)));
  }
  const cb = forces.get(String(cross[0]))?.[0];
  row["横桁 S(G1-G2)"] = cb ? cb.i_end.fz : null;
  const audit = res.diagnostics?.spatial_loads;
  row["橋面荷重 合計Fz"] = audit ? audit.resultant.z : null;
  out.push(row);
}
console.table(out.map((row) => Object.fromEntries(Object.entries(row).map(([k, v]) => [k, typeof v === "number" ? +v.toFixed(1) : v]))));
fs.writeFileSync(process.argv[2].replace(/result\.json$/, "summary.json"), JSON.stringify(out, null, 1));
