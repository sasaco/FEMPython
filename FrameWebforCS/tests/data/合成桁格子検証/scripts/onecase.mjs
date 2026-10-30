// 保存形式から 1 ケースだけの旧 JSON を作る（load[case].spatial_loads に展開）。
// 使い方: node onecase.mjs <saved> <case> <out>。drive.py が <out> の名前の末尾 "_case<N>.json" からケース番号を読む。
import fs from "node:fs";
const [src, cid, out] = process.argv.slice(2);
const d = JSON.parse(fs.readFileSync(src, "utf8"));
const load = { ...d.load[cid] };
const items = d.bridge_loads.cases[cid] ?? [];
if (items.length) {
  const ids = new Set(items.flatMap((x) => x.path_ids));
  const { name, ...panel } = d.bridge_loads.panels[0];
  load.spatial_loads = { panels: [panel], paths: d.bridge_loads.paths.filter((p) => ids.has(p.id)).map(({ name, ...p }) => p),
    loads: items.map(({ name, ...x }) => x) };
}
const r = { analysis_type: "static", dimension: 3, node: d.node, member: d.member, element: d.element, joint: d.joint,
  fix_node: JSON.parse(JSON.stringify(d.fix_node).replace(/null/g, "0")), load: { [cid]: load } };
fs.writeFileSync(out, JSON.stringify(r));
