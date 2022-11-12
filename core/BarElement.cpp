var I_YMZ = [1, 5, 7, 11];	// y軸方向,z軸周り成分のインデックス
var I_ZMY = [2, 4, 8, 10];	// z軸方向,y軸周り成分のインデックス

//--------------------------------------------------------------------//
// 梁要素
// label - 要素ラベル
// material - 材料のインデックス
// param - 梁パラメータのインデックス
// nodes - 節点番号
// axis - 断面基準方向ベクトル
var BarElement = function(label, material, param, nodes, axis) {
    FElement.call(this, label, material, nodes);
    this.param = param;
    this.isBar = true;
    if ((axis != = null) && (axis != = undefined)) axis.normalize();
    this.axis = axis;
};

// 節点数を返す
BarElement.prototype.nodeCount = function() {
    return 2;
};

// 要素境界数を返す
BarElement.prototype.borderCount = function() {
    return 1;
};

// 要素境界を返す
// element - 要素ラベル
// index - 要素境界のインデックス
BarElement.prototype.border = function(element, index) {
    var p = this.nodes;
    switch (index) {
    default:
        return null;
    case 0:
        return new EdgeBorder1(element, [p[0], p[1]]);
    }
};

// 要素境界辺を返す 梁要素では要素境界と同じ
// element - 要素ラベル
// index - 要素境界辺のインデックス
BarElement.prototype.borderEdge = function(element, index) {
    return this.border(element, index);
};

// 要素節点の角度を返す 梁要素では１
// p - 要素節点
BarElement.prototype.angle = function(p) {
    return[1, 1];
};

// 剛性マトリックスを返す
// p - 要素節点
// material - 材料
// sect - 梁断面パラメータ
BarElement.prototype.stiffnessMatrix = function(p, material, sect) {
    var kk = numeric.rep([12, 12], 0), l = p[0].distanceTo(p[1]);
    var d = dirMatrix(p, this.axis);
    var kx = material.ee * sect.area / l, kt = material.gg * sect.ip / l;
    kk[0][0] = kx;
    kk[0][6] = -kx;
    kk[6][0] = -kx;
    kk[6][6] = kx;
    kk[3][3] = kt;
    kk[3][9] = -kt;
    kk[9][3] = -kt;
    kk[9][9] = kt;
    var km = this.stiffBend(l, material, sect);
    for (var i = 0; i < 4; i++) {
        var kk1 = kk[I_YMZ[i]], kk2 = kk[I_ZMY[i]], kmi1 = km[i], kmi2 = km[i + 4];
        for (var j = 0; j < 4; j++) {
            kk1[I_YMZ[j]] = kmi1[j];
            kk2[I_ZMY[j]] = kmi2[j];
        }
    }
    toDir3(d, kk);
    return kk;
};

// 拡散マトリックス [ ∫∇Ni・∇NjdV ] を返す
// p - 要素節点
// coef - 係数
// sect - 梁断面パラメータ
BarElement.prototype.gradMatrix = function(p, coef, sect) {
    var c = coef * sect.area / p[0].distanceTo(p[1]);
    return [[c, -c], [-c, c]];
};

// 幾何剛性マトリックスを返す
// p - 要素節点
// u - 節点変位
// material - 材料
// sect - 梁断面パラメータ
BarElement.prototype.geomStiffnessMatrix = function(p, u, material, sect) {
    var l2 = p[0].distanceToSquared(p[1]), d = dirMatrix(p, this.axis);
    var v = this.toLocalArray(u, d), kk = numeric.rep([12, 12], 0);
    var sx = material.ee * sect.area * (v[6] - v[0]) / l2;
    for (var i = 0; i < 3; i++) {
        kk[i][i] = sx;
        kk[i + 6][i] = -sx;
        kk[i][i + 6] = -sx;
        kk[i + 6][i + 6] = sx;
    }
    toDir3(d, kk);
    return kk;
};

// 節点歪・応力を返す
// p - 要素節点
// u - 節点変位
// material - 材料
// sect - 梁断面パラメータ
BarElement.prototype.strainStress = function(p, u, material, sect) {
    var l = p[0].distanceTo(p[1]), d = dirMatrix(p, this.axis);
    var v = this.toLocalArray(u, d);
    var strain1 = [], stress1 = [], energy1 = [], strain2 = [], stress2 = [], energy2 = [];
    var ex = (v[6] - v[0]) / l, thd = (v[9] - v[3]) / l;
    var ks = this.bendCurveShare(v, l, material, sect);
    var kpy = ks[0], kpz = ks[1], sy = ks[2], sz = ks[3];
    for (var i = 0; i < 2; i++) {
        var str = sect.strainStress(material, ex, thd, kpy[i], kpz[i], sy, sz);
        strain1[i] = new Strain(str[0]);
        stress1[i] = new Stress(str[1]);
        strain2[i] = new Strain(str[2]);
        stress2[i] = new Stress(str[3]);
        strain1[i].rotate(d);
        stress1[i].rotate(d);
        strain2[i].rotate(d);
        stress2[i].rotate(d);
        energy1[i] = 0.5 * strain1[i].innerProduct(stress1[i]);
        energy2[i] = 0.5 * strain2[i].innerProduct(stress2[i]);
    }
    return[strain1, stress1, energy1, strain2, stress2, energy2];
};

// 要素歪・応力を返す
// p - 要素節点
// u - 節点変位
// material - 材料
// sect - 梁断面パラメータ
BarElement.prototype.elementStrainStress = function(p, u, material, sect) {
    var l = p[0].distanceTo(p[1]), d = dirMatrix(p, this.axis);
    var v = this.toLocalArray(u, d), i;
    var str = [];
    var ex = (v[6] - v[0]) / l, thd = (v[9] - v[3]) / l;
    var ks = this.bendCurveShare(v, l, material, sect);
    var kpy = ks[0], kpz = ks[1], sy = ks[2], sz = ks[3];
    for (i = 0; i < 2; i++) {
        str[i] = sect.strainStress(material, ex, thd, kpy[i], kpz[i], sy, sz);
    }
    var strain1 = new Strain
    (numeric.mul(0.5, numeric.add(str[0][0], str[1][0])));
    var stress1 = new Stress
    (numeric.mul(0.5, numeric.add(str[0][1], str[1][1])));
    var strain2 = new Strain
    (numeric.mul(0.5, numeric.add(str[0][2], str[1][2])));
    var stress2 = new Stress
    (numeric.mul(0.5, numeric.add(str[0][3], str[1][3])));
    strain1.rotate(d);
    stress1.rotate(d);
    strain2.rotate(d);
    stress2.rotate(d);
    var energy1 = 0.5 * strain1.innerProduct(stress1);
    var energy2 = 0.5 * strain2.innerProduct(stress2);
    return[strain1, stress1, energy1, strain2, stress2, energy2];
};

// 要素を表す文字列を返す
// materials - 材料
// params - 梁パラメータ
// p - 節点
BarElement.prototype.toString = function(materials, params, p) {
    var s = this.getName() + '\t' + this.label.toString(10) + '\t' +
        materials[this.material].label.toString(10) + '\t' +
        params[this.param].label.toString(10);
    for (var i = 0; i < this.nodes.length; i++) {
        s += '\t' + p[this.nodes[i]].label.toString(10);
    }
    return s;
};
