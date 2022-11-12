// lΚΜ2vfΜί_ΜΜ,Ε,ΔΐW
var TETRA2_NODE = [[0, 0, 0], [1, 0, 0], [0, 1, 0], [0, 0, 1], [0.5, 0, 0], [0.5, 0.5, 0],
[0, 0.5, 0], [0, 0, 0.5], [0.5, 0, 0.5], [0, 0.5, 0.5]];
// lΚΜ2vfΜΟͺ_ΜΜ,Ε,ΔΐW,dέW
var TETRA2_INT = [[GTETRA2[0], GTETRA2[0], GTETRA2[0], C1_24],
[GTETRA2[1], GTETRA2[0], GTETRA2[0], C1_24],
[GTETRA2[0], GTETRA2[1], GTETRA2[0], C1_24],
[GTETRA2[0], GTETRA2[0], GTETRA2[1], C1_24]];
// Ά`1vfΜί_ΜΜ,Ε,ΔΐW
var WEDGE1_NODE = [[0, 0, -1], [1, 0, -1], [0, 1, -1], [0, 0, 1], [1, 0, 1], [0, 1, 1]];
// Ά`1vfΜΟͺ_ΜΜ,Ε,ΔΐW,dέW
var WEDGE1_INT = [[C1_3, C1_3, GX2[0], 0.5], [C1_3, C1_3, GX2[1], 0.5]];
// Ά`2vfΜί_ΜΜ,Ε,ΔΐW
var WEDGE2_NODE = [[0, 0, -1], [1, 0, -1], [0, 1, -1], [0, 0, 1], [1, 0, 1], [0, 1, 1],
[0.5, 0, -1], [0.5, 0.5, -1], [0, 0.5, -1],
[0.5, 0, 1], [0.5, 0.5, 1], [0, 0.5, 1],
[0, 0, 0], [1, 0, 0], [0, 1, 0]];
// Ά`2vfΜΟͺ_ΜΜ,Ε,ΔΐW,dέW
var WEDGE2_INT = [[GTRI2[0], GTRI2[0], GX3[0], C1_6 * GW3[0]],
[GTRI2[1], GTRI2[0], GX3[0], C1_6 * GW3[0]],
[GTRI2[0], GTRI2[1], GX3[0], C1_6 * GW3[0]],
[GTRI2[0], GTRI2[0], GX3[1], C1_6 * GW3[1]],
[GTRI2[1], GTRI2[0], GX3[1], C1_6 * GW3[1]],
[GTRI2[0], GTRI2[1], GX3[1], C1_6 * GW3[1]],
[GTRI2[0], GTRI2[0], GX3[2], C1_6 * GW3[2]],
[GTRI2[1], GTRI2[0], GX3[2], C1_6 * GW3[2]],
[GTRI2[0], GTRI2[1], GX3[2], C1_6 * GW3[2]]];
// ZΚΜ1vfΜί_ΜΜ,Ε,ΔΐW
var HEXA1_NODE = [[-1, -1, -1], [1, -1, -1], [1, 1, -1], [-1, 1, -1],
[-1, -1, 1], [1, -1, 1], [1, 1, 1], [-1, 1, 1]];
// ZΚΜ1vfΜΟͺ_ΜΜ,Ε,ΔΐW,dέW
var HEXA1_INT = [[GX2[0], GX2[0], GX2[0], 1], [GX2[1], GX2[0], GX2[0], 1],
[GX2[0], GX2[1], GX2[0], 1], [GX2[1], GX2[1], GX2[0], 1],
[GX2[0], GX2[0], GX2[1], 1], [GX2[1], GX2[0], GX2[1], 1],
[GX2[0], GX2[1], GX2[1], 1], [GX2[1], GX2[1], GX2[1], 1]];
// ZΚΜ2vfΜί_ΜΜ,Ε,ΔΐW
var HEXA2_NODE = [[-1, -1, -1], [1, -1, -1], [1, 1, -1], [-1, 1, -1],
[-1, -1, 1], [1, -1, 1], [1, 1, 1], [-1, 1, 1],
[0, -1, -1], [1, 0, -1], [0, 1, -1], [-1, 0, -1],
[0, -1, 1], [1, 0, 1], [0, 1, 1], [-1, 0, 1],
[-1, -1, 0], [1, -1, 0], [1, 1, 0], [-1, 1, 0]];
// ZΚΜ2vfΜΟͺ_ΜΜ,Ε,ΔΐW,dέW
var HEXA2_INT = (function() {
    var a = [];
    for (var k = 0; k < 3; k++) {
        for (var j = 0; j < 3; j++) {
            for (var i = 0; i < 3; i++) {
                a.push([GX3[i], GX3[j], GX3[k], GW3[i] * GW3[j] * GW3[k]]);
            }
        }
    }
    return a;
}());
// ZΚΜ1vfΜΏΚ}gbNXW
var HEXA1_MASS_BASE = (function() {
    var v = [], abs = Math.abs;
    for (var i = 0; i < 8; i++) {
        v[i] = [];
        for (var j = 0; j < 8; j++) {
            var s = abs(HEXA1_NODE[i][0] - HEXA1_NODE[j][0]) +
                abs(HEXA1_NODE[i][1] - HEXA1_NODE[j][1]) +
                abs(HEXA1_NODE[i][2] - HEXA1_NODE[j][2]);
            v[i][j] = Math.pow(0.5, 0.5 * s) / 27;
        }
    }
    return v;
}());

//--------------------------------------------------------------------//
// \bhvf
// label - vfx
// material - ήΏΜCfbNX
// nodes - ί_Τ
// nodeP - ί_ΜΜ,Ε,ΔΐW
// intP - Οͺ_ΜΜ,Ε,ΔΐW,dέW
var SolidElement = function(label, material, nodes, nodeP, intP) {
    FElement.call(this, label, material, nodes);
    this.nodeP = nodeP;
    this.intP = intP;
};

// RrsρπΤ·
// p - vfί_
// sf - `σΦsρ
SolidElement.prototype.jacobianMatrix = function(p, sf) {
    var count = this.nodeCount(), jac = [0, 0, 0, 0, 0, 0, 0, 0, 0];
    for (var i = 0; i < count; i++) {
        var sfi = sf[i], pix = p[i].x, piy = p[i].y, piz = p[i].z;
        for (var j = 0; j < 3; j++) {
            var sfij = sfi[j + 1];
            jac[j] += sfij * pix;
            jac[j + 3] += sfij * piy;
            jac[j + 6] += sfij * piz;
        }
    }
    return new THREE.Matrix3().fromArray(jac);
};

// `σΦΜωz [ dNi/dx dNi/dy dNi/dz ] πΤ·
// p - vfί_
// ja - Rrsρ
// sf - `σΦsρ
SolidElement.prototype.grad = function(p, ja, sf) {
    var count = this.nodeCount(), gr = [];
    var ji = new THREE.Matrix3().getInverse(ja, true).elements;
    for (var i = 0; i < count; i++) {
        gr[i] = [ji[0] * sf[i][1] + ji[3] * sf[i][2] + ji[6] * sf[i][3],
            ji[1] * sf[i][1] + ji[4] * sf[i][2] + ji[7] * sf[i][3],
            ji[2] * sf[i][1] + ji[5] * sf[i][2] + ji[8] * sf[i][3]];
    }
    return gr;
};

// c - ΟΚ}gbNXΜ]usρπΤ·
// grad - `σΦΜωz
SolidElement.prototype.strainMatrix = function(grad) {
    var count = this.nodeCount(), m = numeric.rep([3 * count, 6], 0);
    for (var i = 0; i < count; i++) {
        var i3 = 3 * i, gr = grad[i];
        m[i3][0] = gr[0];
        m[i3 + 1][1] = gr[1];
        m[i3 + 2][2] = gr[2];
        m[i3][3] = gr[1];
        m[i3 + 1][3] = gr[0];
        m[i3 + 1][4] = gr[2];
        m[i3 + 2][4] = gr[1];
        m[i3][5] = gr[2];
        m[i3 + 2][5] = gr[0];
    }
    return m;
};

// Οͺ_Μ`σΦ}gbNX [ NiNj ] πΤ·
// p - vfί_
// x - Μ,Ε,ΔΐW
// w - dέW
SolidElement.prototype.shapePart = function(p, x, w) {
    var sf = this.shapeFunction(x[0], x[1], x[2]);
    var ja = this.jacobianMatrix(p, sf);
    var count = this.nodeCount(), matrix = [];
    var coef = w * Math.abs(ja.determinant());
    for (var i = 0; i < count; i++) {
        var matr = [], cf2 = coef * sf[i][0];
        for (var j = 0; j < count; j++) {
            matr[j] = cf2 * sf[j][0];
        }
        matrix[i] = matr;
    }
    return matrix;
};

// Οͺ_ΜgU}gbNX [ ήNiEήNj ] πΤ·
// p - vfί_
// x - Μ,Ε,ΔΐW
// w - dέW
SolidElement.prototype.gradPart = function(p, x, w) {
    var sf = this.shapeFunction(x[0], x[1], x[2]);
    var ja = this.jacobianMatrix(p, sf);
    var gr = this.grad(p, ja, sf);
    var count = this.nodeCount(), matrix = [];
    var coef = w * Math.abs(ja.determinant());
    for (var i = 0; i < count; i++) {
        var matr = [], gri = gr[i];
        var c1 = coef * gri[0], c2 = coef * gri[1], c3 = coef * gri[2];
        for (var j = 0; j < count; j++) {
            var grj = gr[j];
            matr[j] = c1 * grj[0] + c2 * grj[1] + c3 * grj[2];
        }
        matrix[i] = matr;
    }
    return matrix;
};

// ΏΚ}gbNXπΤ·
// p - vfί_
// dens - ήΏΜ§x
SolidElement.prototype.massMatrix = function(p, dens) {
    var count = this.nodeCount(), m = numeric.rep([3 * count, 3 * count], 0);
    for (var i = 0; i < this.intP.length; i++) {
        var sf = this.shapeFunction(this.intP[i][0], this.intP[i][1],
            this.intP[i][2]);
        var ja = this.jacobianMatrix(p, sf);
        var coef = this.intP[i][3] * dens * Math.abs(ja.determinant());
        for (var i1 = 0; i1 < count; i1++) {
            for (var j1 = 0; j1 < count; j1++) {
                var value = coef * sf[i1][0] * sf[j1][0], i3 = 3 * i1, j3 = 3 * j1;
                m[i3][j3] += value;
                m[i3 + 1][j3 + 1] += value;
                m[i3 + 2][j3 + 2] += value;
            }
        }
    }
    return m;
};

// «}gbNXπΤ·
// p - vfί_
// d1 - Ν - c}gbNX
SolidElement.prototype.stiffnessMatrix = function(p, d1) {
    var count = 3 * this.nodeCount(), kk = numeric.rep([count, count], 0);
    for (var i = 0; i < this.intP.length; i++) {
        var ip = this.intP[i];
        var sf = this.shapeFunction(ip[0], ip[1], ip[2]);
        var ja = this.jacobianMatrix(p, sf);
        var ks = this.stiffPart(d1, this.strainMatrix(this.grad(p, ja, sf)),
            ip[3] * Math.abs(ja.determinant()));
        addMatrix(kk, ks);
    }
    return kk;
};

// `σΦ}gbNX [ ηNiNjdV ] πΤ·
// p - vfί_
// coef - W
SolidElement.prototype.shapeFunctionMatrix = function(p, coef) {
    var count = this.nodeCount(), s = numeric.rep([count, count], 0);
    for (var i = 0; i < this.intP.length; i++) {
        addMatrix(s, this.shapePart(p, this.intP[i], coef * this.intP[i][3]));
    }
    return s;
};

// gU}gbNX [ ηήNiEήNjdV ] πΤ·
// p - vfί_
// coef - W
SolidElement.prototype.gradMatrix = function(p, coef) {
    var count = this.nodeCount(), g = numeric.rep([count, count], 0);
    for (var i = 0; i < this.intP.length; i++) {
        addMatrix(g, this.gradPart(p, this.intP[i], coef * this.intP[i][3]));
    }
    return g;
};

// τ½«}gbNXπΤ·
// p - vfί_
// u - ί_ΟΚ
// d1 - Ν - c}gbNX
SolidElement.prototype.geomStiffnessMatrix = function(p, u, d1) {
    var count = this.nodeCount(), kk = numeric.rep([3 * count, 3 * count], 0);
    var v = this.toArray(u, 3);
    for (var i = 0; i < this.intP.length; i++) {
        var ip = this.intP[i];
        var sf = this.shapeFunction(ip[0], ip[1], ip[2]);
        var ja = this.jacobianMatrix(p, sf);
        var gr = this.grad(p, ja, sf);
        var sm = this.strainMatrix(gr);
        var str = numeric.dotMV(d1, numeric.dotVM(v, sm));
        var w = ip[3] * Math.abs(ja.determinant());
        for (var i1 = 0; i1 < count; i1++) {
            var i3 = 3 * i1, gri = gr[i1];
            for (var j1 = 0; j1 < count; j1++) {
                var j3 = 3 * j1, grj = gr[j1];
                var s = w * (gri[0] * (str[0] * grj[0] + str[3] * grj[1] + str[5] * grj[2]) +
                    gri[1] * (str[3] * grj[0] + str[1] * grj[1] + str[4] * grj[2]) +
                    gri[2] * (str[5] * grj[0] + str[4] * grj[1] + str[2] * grj[2]));
                kk[i3][j3] += s;
                kk[i3 + 1][j3 + 1] += s;
                kk[i3 + 2][j3 + 2] += s;
            }
        }
    }
    return kk;
};

// ί_cEΝπΤ·
// p - vfί_
// u - ί_ΟΚ
// d1 - Ν - c}gbNX
SolidElement.prototype.strainStress = function(p, u, d1) {
    var count = this.nodeCount(), v = this.toArray(u, 3);
    var strain = [], stress = [], energy = [];
    for (var i = 0; i < count; i++) {
        var eps = this.strainPart(p, v, this.nodeP[i]);
        strain[i] = new Strain(eps);
        var str = numeric.dotMV(d1, eps);
        stress[i] = new Stress(str);
        energy[i] = 0.5 * strain[i].innerProduct(stress[i]);
    }
    return[strain, stress, energy];
};

// vfΰΜcxNgπΤ·
// p - vfί_
// v - ί_ΟΚxNg
// x - Μ,Ε,ΔΐW
SolidElement.prototype.strainPart = function(p, v, x) {
    var sf = this.shapeFunction(x[0], x[1], x[2]);
    var ja = this.jacobianMatrix(p, sf);
    var sm = this.strainMatrix(this.grad(p, ja, sf));
    return numeric.dotVM(v, sm);
};

// vfcEΝπΤ·
// p - vfί_
// u - ί_ΟΚ
// d1 - Ν - c}gbNX
SolidElement.prototype.elementStrainStress = function(p, u, d1) {
    var v = this.toArray(u, 3), cf = 1 / this.intP.length;
    var strain = [0, 0, 0, 0, 0, 0], stress = [0, 0, 0, 0, 0, 0], energy = 0;
    for (var i = 0; i < this.intP.length; i++) {
        var eps = this.strainPart(p, v, this.intP[i]);
        strain = numeric.add(strain, eps);
        var str = numeric.dotMV(d1, eps);
        stress = numeric.add(stress, str);
        energy += numeric.dotVV(eps, str);
    }
    strain = numeric.mul(strain, cf);
    stress = numeric.mul(stress, cf);
    energy *= 0.5 * cf;
    return[new Strain(strain), new Stress(stress), energy];
};

// vfπ\·ΆρπΤ·
// materials - ήΏ
// p - ί_
SolidElement.prototype.toString = function(materials, p) {
    var s = this.getName() + '\t' + this.label.toString(10) + '\t' +
        materials[this.material].label.toString(10);
    for (var i = 0; i < this.nodes.length; i++) {
        s += '\t' + p[this.nodes[i]].label.toString(10);
    }
    return s;
};