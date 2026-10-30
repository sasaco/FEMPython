// 合成桁の設計例と解説 pp.36-40 (5.1-5.4) の格子モデルを FEMPython の保存形式 JSON に書く。
// 使い方: [DX=0.25] [SHEAR=none|cross|all] [LIVE_SPEC="$(cat live.json)"] node gen.mjs <out.json> [主桁のねじり定数 m4]
//   DX: 主桁の節点間隔 (m)。J1・中央・J2 の節点は必ず置く。SHEAR: せん断変形を入れる部材 (cross=横桁, all=主桁と横桁)。
//   LIVE_SPEC: 活荷重ケースの並び (live.json)。<out>.map.json に節点・部材番号の対応を書く (read.mjs が読む)。
//   説明は ../README.md。
import fs from "node:fs";

const out = process.argv[2];
const girderJ = Number(process.argv[3] ?? 1e-8);
const L = 33.0, DX = Number(process.env.DX ?? 0.25);
const STATIONS = [...new Set([...Array(Math.round(L / DX) + 1).keys()].map((i) => +(i * DX).toFixed(6))
  .concat([10.25, 16.5, 22.75]).filter((x) => x <= L))].sort((a, b) => a - b);
const NS = STATIONS.length;
const LINES = [-1.025, 0, 2.55, 5.1, 7.65, 8.675];                // E, G1..G4, E'
const lineName = ["E", "G1", "G2", "G3", "G4", "E2"];
const nid = (line, s) => line * NS + s + 1;
const xs = (s) => STATIONS[s];
const sCross = STATIONS.indexOf(16.5);
const E = 2.0e8, G = 7.7e7, cm4 = 1e-8;

const node = {};
LINES.forEach((y, l) => { for (let s = 0; s < NS; s++) node[nid(l, s)] = { x: xs(s), y, z: 0 }; });

// element ids: 1 G1 Sec-2, 2 G1 Sec-1, 3 G2 Sec-2, 4 G2 Sec-1, 5 横桁, 6 張出し伝達(G1->G2), 7 張出し片持ち
const SHEAR = process.env.SHEAR ?? "none";
const props = (I, name, A = 0.04, J = girderJ) => ({ E, G, Xp: 1.2e-5, A, J, Iy: I, Iz: I, n: name });
const webA = (cm2) => cm2 * 1e-4 / (5 / 6);
const steel = { 1: props(1834000 * cm4, "G1 Sec-2 鋼桁", webA(150.8)), 2: props(1979000 * cm4, "G1 Sec-1 鋼桁", webA(150.4)),
  3: props(1719000 * cm4, "G2 Sec-2 鋼桁", webA(151.0)), 4: props(1888000 * cm4, "G2 Sec-1 鋼桁", webA(150.5)) };
const comp = { 1: props(5313000 * cm4, "G1 Sec-2 合成n=7", webA(150.8)), 2: props(5318000 * cm4, "G1 Sec-1 合成n=7", webA(150.4)),
  3: props(5495000 * cm4, "G2 Sec-2 合成n=7", webA(151.0)), 4: props(5498000 * cm4, "G2 Sec-1 合成n=7", webA(150.5)) };
const common = { 5: props(428961 * cm4, "荷重分配横桁", webA(115.2), 1e-6),
  6: props(0.1, "張出し荷重の伝達(てこ)", 1, 0.1), 7: props(0.1, "張出し片持ち", 1, 0.1) };
const element = { 1: { ...steel, ...common }, 2: { ...comp, ...common } };

const member = {}; let mid = 0; const girderMembers = {};
const addMember = (ni, nj, e) => {
  const shear = SHEAR === "all" ? e <= 5 : SHEAR === "cross" ? e === 5 : false;
  member[++mid] = { ni, nj, e, cg: 0, shear_correction: shear }; return mid;
};
for (let g = 1; g <= 4; g++) {
  girderMembers[g] = [];
  const outer = g === 1 || g === 4;
  for (let s = 0; s < NS - 1; s++) {
    const xm = (xs(s) + xs(s + 1)) / 2;
    const sec1 = xm > 10.25 && xm < 22.75;
    const e = outer ? (sec1 ? 2 : 1) : (sec1 ? 4 : 3);
    girderMembers[g].push(addMember(nid(g, s), nid(g, s + 1), e));
  }
}
const cross = [addMember(nid(1, sCross), nid(2, sCross), 5), addMember(nid(2, sCross), nid(3, sCross), 5),
  addMember(nid(3, sCross), nid(4, sCross), 5)];
const strips = [], cants = [];
for (let s = 0; s < NS; s++) {
  strips.push([addMember(nid(1, s), nid(2, s), 6), "j"]);   // G1 剛結, G2 ピン
  strips.push([addMember(nid(4, s), nid(3, s), 6), "j"]);   // G4 剛結, G3 ピン
  cants.push(addMember(nid(0, s), nid(1, s), 7));           // E -> G1 剛結
  cants.push(addMember(nid(5, s), nid(4, s), 7));           // E2 -> G4 剛結
}
const release = (m, end) => ({ m: String(m), xi: end === "i" ? 0 : 1, yi: end === "i" ? 0 : 1, zi: end === "i" ? 0 : 1,
  xj: end === "j" ? 0 : 1, yj: end === "j" ? 0 : 1, zj: end === "j" ? 0 : 1 });
const joint = { 1: [release(cross[0], "i"), release(cross[2], "j"), ...strips.map(([m, end]) => release(m, end))]
  .map((row, i) => ({ row: i + 1, ...row })) };

const fixRows = [];
for (let g = 1; g <= 4; g++) for (const s of [0, NS - 1])
  fixRows.push({ row: fixRows.length + 1, n: String(nid(g, s)), tx: s === 0 ? 1 : null, ty: 1, tz: 1, rx: 1, ry: null, rz: null });
const fix_node = { 1: fixRows };

// 橋面: 全節点の三角形
const tri = [];
for (let l = 0; l < LINES.length - 1; l++) for (let s = 0; s < NS - 1; s++) {
  const a = nid(l, s), b = nid(l, s + 1), c = nid(l + 1, s + 1), d = nid(l + 1, s);
  tri.push([a, b, c], [a, c, d]);
}
const panel = { id: 1, name: "床版全幅", nodes: Object.keys(node).map(Number), triangles: tri };
const paths = []; const pathId = new Map();
const path = (y, x0 = 0, x1 = L) => {
  const key = `${y}|${x0}|${x1}`;
  if (!pathId.has(key)) { paths.push({ id: paths.length + 1, name: `y=${y} x=${x0}-${x1}`, points: [[x0, y, 0], [x1, y, 0]] });
    pathId.set(key, paths.length); }
  return pathId.get(key);
};
const down = { mode: "global", vector: [0, 0, 1] };
const line = (name, y, w, x0, x1) => ({ name, path_ids: [path(y, x0, x1)], end_intensities: [[-w, -w]], direction: down });
const area = (name, y0, y1, w, x0, x1) => ({ name, path_ids: [path(y0, x0, x1), path(y1, x0, x1)],
  end_intensities: [[-w, -w], [-w, -w]], direction: down });
const [yE, yG1, yG2, yG3, yG4, yE2] = LINES;

const cases = {}; const load = {};
const addCase = (name, elementSet, loads, extra = {}) => {
  const id = String(Object.keys(load).length + 1);
  load[id] = { name, symbol: "D", element: elementSet, fix_node: 1, joint: 1, ...extra };
  if (loads.length) cases[id] = loads.map((item, i) => ({ id: i + 1, panel_id: 1, ...item }));
  return id;
};

// 5.2.2 合成前死荷重 (鋼桁)
addCase("合成前死荷重 Db", 1, [
  area("床版 5.39", yE, yE2, 5.39, 0, L), area("型枠 1.00", yE, yE2, 1.00, 0, L),
  line("ハンチ G1", yG1, 1.23, 0, L), line("ハンチ G4", yG4, 1.23, 0, L),
  line("ハンチ G2", yG2, 1.75, 0, L), line("ハンチ G3", yG3, 1.75, 0, L),
  ...[yG1, yG2, yG3, yG4].map((y, i) => line(`鋼重 G${i + 1}`, y, 4.41, 0, L)),
]);
// 5.2.3 合成後死荷重 (合成断面)
addCase("合成後死荷重 Da", 2, [
  area("舗装 1.69", yE + 0.6, yE2 - 0.6, 1.69, 0, L),
  line("地覆 左", yE + 0.3, 4.78, 0, L), line("地覆 右", yE2 - 0.3, 4.78, 0, L),
  line("防護柵 左", yE + 0.25, 0.50, 0, L), line("防護柵 右", yE2 - 0.25, 0.50, 0, L),
  line("添架物", (yE + yE2) / 2, 0.60, 0, L), area("型枠撤去 -1.00", yE, yE2, -1.00, 0, L),
]);
// 横分配の影響値: 支間中央の各主桁に 100 kN
for (let g = 1; g <= 4; g++)
  addCase(`影響値 P=100kN G${g}中央`, 2, [], { load_node: [{ row: 1, n: String(nid(g, sCross)), tx: 0, ty: 0, tz: -100, rx: 0, ry: 0, rz: 0 }] });

// 活荷重 (B活荷重, 衝撃 i=0.241): 引数で載荷位置を与える
const imp = 1.241;
const liveSpec = JSON.parse(process.env.LIVE_SPEC ?? "[]");
for (const spec of liveSpec) {
  const items = [];
  for (const [y0, y1, factor] of spec.strips) {
    items.push(area(`p2 ${factor}`, y0, y1, 3.5 * imp * factor, 0, L));
    items.push(area(`p1 ${factor}`, y0, y1, spec.p1 * imp * factor, spec.x0, spec.x0 + 10));
  }
  addCase(spec.name, 2, items);
}

const saved = { ver: "2.5.12", dimension: 3, node, member, element, joint, fix_node, notice_points: [], load,
  define: {}, combine: {}, pickup: {}, bridge_loads: { version: 1, panels: [panel], paths, cases } };
fs.writeFileSync(out, JSON.stringify(saved));
fs.writeFileSync(out.replace(/\.json$/, ".map.json"), JSON.stringify({ NS, STATIONS, LINES, girderMembers, cross }));
console.log(`nodes ${Object.keys(node).length}, members ${mid}, triangles ${tri.length}, cases ${Object.keys(load).length}`);
