// gen.mjs のモデルから、FrameWebforCS で開いて目で確かめるための入力ファイルを作る。
// ケース名・記号を整え、設計例の Mv (=Mvd+ML) と S(R) (=Rs+Rvd+Rvl) に当たる組合せを足す。
// 使い方: node make-visual.mjs <gen.mjs のモデル (SHEAR=none)> <出力 .json>
import fs from "node:fs";

const [src, out] = process.argv.slice(2);
const d = JSON.parse(fs.readFileSync(src, "utf8"));

const cases = {
  1: ["D", "合成前死荷重 Db（鋼桁）"],
  2: ["D", "合成後死荷重 Da（合成断面 n=7）"],
  3: ["P", "横分配の確認：G1 中央に 100 kN"],
  4: ["P", "横分配の確認：G2 中央に 100 kN"],
  5: ["P", "横分配の確認：G3 中央に 100 kN"],
  6: ["P", "横分配の確認：G4 中央に 100 kN"],
  7: ["L", "活荷重 G1 中央の曲げ最大（p1 x=11.5～21.5）"],
  8: ["L", "活荷重 G1 J1 の曲げ最大（p1 x=7.144～17.144）"],
  9: ["L", "活荷重 G2 中央の曲げ最大（p1 x=11.5～21.5）"],
  10: ["L", "活荷重 G2 J1 の曲げ最大（p1 x=7.144～17.144）"],
  11: ["L", "活荷重 G1 支点反力最大（p1 x=0～10）"],
  12: ["L", "活荷重 G2 支点反力最大（p1 x=0～10）"],
};
for (const [id, [symbol, name]] of Object.entries(cases)) {
  const load = d.load[id];
  if (!load) throw new Error(`load case ${id} is missing`);
  Object.assign(load, { symbol, name, rate: 1, LL_pitch: 0 });
}
for (const member of Object.values(d.member)) delete member.shear_correction;   // SHEAR=none と同じ

d.define = Object.fromEntries(Object.keys(cases).map((id) => [id, { row: Number(id), C1: Number(id) }]));
const combines = [
  ["Mv G1 中央（Da＋L）　設計例 3981", { C2: 1, C7: 1 }],
  ["Mv G1 J1（Da＋L）　設計例 3278", { C2: 1, C8: 1 }],
  ["Mv G2 中央（Da＋L）　設計例 3360", { C2: 1, C9: 1 }],
  ["Mv G2 J1（Da＋L）　設計例 2987", { C2: 1, C10: 1 }],
  ["S(R) G1（Db＋Da＋L）　設計例 837", { C1: 1, C2: 1, C11: 1 }],
  ["S(R) G2（Db＋Da＋L）　設計例 845", { C1: 1, C2: 1, C12: 1 }],
];
d.combine = Object.fromEntries(combines.map(([name, factors], i) => [String(i + 1), { row: i + 1, name, ...factors }]));
d.pickup = {};
d.three = { camera: { x: 16.5, y: -30, z: 25, zoom: 1 }, target: { x: 16.5, y: 3.825, z: 0 } };

fs.writeFileSync(out, JSON.stringify(d, null, 1));
console.log(`${out}: nodes ${Object.keys(d.node).length}, members ${Object.keys(d.member).length}, cases ${Object.keys(d.load).length}, combines ${combines.length}`);
