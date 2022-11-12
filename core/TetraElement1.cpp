
//--------------------------------------------------------------------//
// 四面体1次要素
// label - 要素ラベル
// material - 材料のインデックス
// nodes - 節点番号
var TetraElement1 = function(label, material, nodes) {
    SolidElement.call(this, label, material, nodes, null, null);
};

// 要素名称を返す
TetraElement1.prototype.getName = function() {
    return 'TetraElement1';
};

// 節点数を返す
TetraElement1.prototype.nodeCount = function() {
    return 4;
};

// 要素境界数を返す
TetraElement1.prototype.borderCount = function() {
    return 4;
};

// 要素境界を返す
// element - 要素ラベル
// index - 要素境界のインデックス
TetraElement1.prototype.border = function(element, index) {
    var p = this.nodes;
    switch (index) {
    default:
        return null;
    case 0:
        return new TriangleBorder1(element, [p[0], p[2], p[1]]);
    case 1:
        return new TriangleBorder1(element, [p[0], p[1], p[3]]);
    case 2:
        return new TriangleBorder1(element, [p[1], p[2], p[3]]);
    case 3:
        return new TriangleBorder1(element, [p[2], p[0], p[3]]);
    }
};

// 要素を鏡像反転する
TetraElement1.prototype.mirror = function() {
    swap(this.nodes, 1, 2);
};

// 要素節点の角度を返す
// p - 要素節点
TetraElement1.prototype.angle = function(p) {
    var th = [];
    for (var i = 0; i < 4; i++) {
        th[i] = solidAngle(p[i], p[(i + 1) % 4], p[(i + 2) % 4], p[(i + 3) % 4]);
    }
    return th;
};

// 形状関数行列 [ Ni dNi/dξ dNi/dη dNi/dζ ] を返す
// xsi,eta,zeta - 要素内部ξ,η,ζ座標
TetraElement1.prototype.shapeFunction = function(xsi, eta, zeta) {
    return [[1 - xsi - eta - zeta, -1, -1, -1], [xsi, 1, 0, 0], [eta, 0, 1, 0],
        [zeta, 0, 0, 1]];
};

// ヤコビアンを返す
// p - 要素節点
TetraElement1.prototype.jacobian = function(p) {
    var p0x = p[0].x, p0y = p[0].y, p0z = p[0].z;
    var j11 = (p[2].y - p0y) * (p[3].z - p0z) - (p[3].y - p0y) * (p[2].z - p0z);
    var j21 = (p[3].y - p0y) * (p[1].z - p0z) - (p[1].y - p0y) * (p[3].z - p0z);
    var j31 = (p[1].y - p0y) * (p[2].z - p0z) - (p[2].y - p0y) * (p[1].z - p0z);
    return (p[1].x - p0x) * j11 + (p[2].x - p0x) * j21 + (p[3].x - p0x) * j31;
};

// 形状関数の勾配 [ dNi/dx dNi/dy dNi/dz ] を返す
// p - 要素節点
// ja - ヤコビアン
TetraElement1.prototype.grad = function(p, ja) {
    var count = this.nodeCount(), gr = [], ji = 1 / ja;
    for (var i = 0; i < count; i++) {
        ji = -ji;
        var i2 = (i + 1) % 4, i3 = (i + 2) % 4, i4 = (i + 3) % 4;
        var p2x = p[i2].x, p2y = p[i2].y, p2z = p[i2].z;
        gr[i] = [ji * ((p[i3].y - p2y) * (p[i4].z - p2z) -
            (p[i4].y - p2y) * (p[i3].z - p2z)),
            ji * ((p[i3].z - p2z) * (p[i4].x - p2x) -
                (p[i4].z - p2z) * (p[i3].x - p2x)),
            ji * ((p[i3].x - p2x) * (p[i4].y - p2y) -
                (p[i4].x - p2x) * (p[i3].y - p2y))];
    }
    return gr;
};

// 質量マトリックスを返す
// p - 要素節点
// dens - 材料の密度
TetraElement1.prototype.massMatrix = function(p, dens) {
    var m = numeric.rep([12, 12], 0);
    var value = dens * this.jacobian(p) / 60, vh = 0.5 * value;
    for (var i = 0; i < 12; i += 3) {
        m[i][i] = value;
        m[i + 1][i + 1] = value;
        m[i + 2][i + 2] = value;
        for (var j = i + 3; j < 12; j += 3) {
            m[i][j] = vh;
            m[i + 1][j + 1] = vh;
            m[i + 2][j + 2] = vh;
            m[j][i] = vh;
            m[j + 1][i + 1] = vh;
            m[j + 2][i + 2] = vh;
        }
    }
    return m;
};

// 剛性マトリックスを返す
// p - 要素節点
// d1 - 応力 - 歪マトリックス
TetraElement1.prototype.stiffnessMatrix = function(p, d1) {
    var ja = this.jacobian(p);
    return this.stiffPart(d1, this.strainMatrix(this.grad(p, ja)),
        C1_6 * Math.abs(ja));
};

// 形状関数マトリックス [ ∫NiNjdV ] を返す
// p - 要素節点
// coef - 係数
TetraElement1.prototype.shapeFunctionMatrix = function(p, coef) {
    var value = 0.1 * C1_6 * coef * Math.abs(this.jacobian(p)), vh = 0.5 * value;
    return [[value, vh, vh, vh], [vh, value, vh, vh], [vh, vh, value, vh],
        [vh, vh, vh, value]];
};

// 拡散マトリックス [ ∫∇Ni・∇NjdV ] を返す
// p - 要素節点
// coef - 係数
TetraElement1.prototype.gradMatrix = function(p, coef) {
    var g = [], ja = this.jacobian(p), gr = this.grad(p, ja);
    var coef2 = C1_6 * coef * Math.abs(ja);
    for (var i = 0; i < 4; i++) {
        g[i] = [];
        for (var j = 0; j < 4; j++) {
            g[i][j] = coef2 * (gr[i][0] * gr[j][0] + gr[i][1] * gr[j][1] +
                gr[i][2] * gr[j][2]);
        }
    }
    return g;
};

// 幾何剛性マトリックスを返す
// p - 要素節点
// u - 節点変位
// d1 - 応力 - 歪マトリックス
TetraElement1.prototype.geomStiffnessMatrix = function(p, u, d1) {
    var count = this.nodeCount(), kk = numeric.rep([3 * count, 3 * count], 0);
    var ja = this.jacobianMatrix(p);
    var gr = this.grad(p, ja);
    var sm = this.strainMatrix(gr);
    var str = numeric.dotMV(d1, numeric.dotVM(this.toArray(u, 3), sm));
    var w = C1_6 * Math.abs(ja);
    for (var i1 = 0; i1 < count; i1++) {
        var i3 = 3 * i1, gri = gr[i1];
        for (var j1 = 0; j1 < count; j1++) {
            var j3 = 3 * j1, grj = gr[j1];
            var s = w * (gri[0] * (str[0] * grj[0] + str[3] * grj[1] + str[5] * grj[2]) +
                gri[1] * (str[3] * grj[0] + str[1] * grj[1] + str[4] * grj[2]) +
                gri[2] * (str[5] * grj[0] + str[4] * grj[1] + str[2] * grj[2]));
            kk[i3][j3] = s;
            kk[i3 + 1][j3 + 1] = s;
            kk[i3 + 2][j3 + 2] = s;
        }
    }
    return kk;
};

// 節点歪・応力を返す
// p - 要素節点
// u - 節点変位
// d1 - 応力 - 歪マトリックス
TetraElement1.prototype.strainStress = function(p, u, d1) {
    var sm = this.strainMatrix(this.grad(p, this.jacobian(p)));
    var eps = numeric.dotVM(this.toArray(u, 3), sm);
    var strain = new Strain(eps);
    var str = numeric.dotMV(d1, eps);
    var stress = new Stress(str);
    var energy = 0.5 * strain.innerProduct(stress);
    return [[strain, strain, strain, strain], [stress, stress, stress, stress],
        [energy, energy, energy, energy]];
};

// 要素歪・応力を返す
// p - 要素節点
// u - 節点変位
// d1 - 応力 - 歪マトリックス
TetraElement1.prototype.elementStrainStress = function(p, u, d1) {
    var sm = this.strainMatrix(this.grad(p, this.jacobian(p)));
    var eps = numeric.dotVM(this.toArray(u, 3), sm);
    var str = numeric.dotMV(d1, eps);
    var energy = 0.5 * numeric.dotVV(eps, str);
    return[new Strain(eps), new Stress(str), energy];
};