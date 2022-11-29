#include "FElement.h";

//--------------------------------------------------------------------//
// 要素
// label - 要素ラベル
// material - 材料のインデックス
// nodes - 節点番号
FElement::FElement(int _label, int _material, vector<int> _nodes)
    : Nodes(_nodes) {

    label = _label;
    material = _material;
    isShell = false;        // シェル要素ではない
    isBar = false;		    // 梁要素ではない
};


// 積分点の剛性マトリックスを返す
// d - 応力-歪マトリックス
// b - 歪-変位マトリックスの転置行列
// coef - 係数
MatrixXd FElement::stiffPart(MatrixXd d, MatrixXd b, double coef) {

    int size1 = b.rows();
    int size2 = d.rows();

    MatrixXd k(size1, size2);

    for (int i = 0; i < size1; i++) {

        VectorXd a(size2);

        VectorXd bi = b.row(i);
        for (int j = 0; j < size2; j++) {
            a(j) = coef * bi.dot(d.row(j));
        }

        for (int j = 0; j < size1; j++) {
            k(i, j) = a.dot(b.row(j));
        }
    }
    return k;

}


// 節点変位を1次元配列に変換する
// u - 節点変位
// dof - 節点自由度
VectorXd FElement::toArray(vector<Vector3R> u, int dof) {

    int count = nodeCount();

    VectorXd out(count + dof);

    for (int i = 0; i < count; i++) {

        double* ux = u[i].x;

        for (int j = 0; j < dof; j++) {
            int index = i + j;
            out(index) = ux[j];
        }

    }
};


// 節点変位を局所座標系・1次元配列に変換する
// u - 節点変位
// d - 方向余弦マトリックス
VectorXd FElement::toLocalArray(vector<Vector3R> u, vector<vector<double>> d) {

    VectorXd v(12);

    int index = 0;
    for (int i = 0; i < 2; i++) {
        double* ux = u[i].x;
        for (int j = 0; j < 3; j++) {
            v(index) = d[0][j] * ux[0] + d[1][j] * ux[1] + d[2][j] * ux[2];
        }
        for (int j = 0; j < 3; j++) {
            v(index) = d[0][j] * ux[3] + d[1][j] * ux[4] + d[2][j] * ux[5];
        }
    }
}

// 3x3の行列式を返す
double FElement::determinant(VectorXd ja) {
    return ja.determinant();
}


// 方向余弦マトリックスを返す
// p - 頂点座標
// axis - 断面基準方向ベクトル
MatrixXd FElement::dirMatrix(vector<Vector3> p, VectorXd axis) {

    vector<Vector3> v = dirVectors(p, axis);

    MatrixXd result(3, 3);
    result << v[0].x, v[1].x, v[2].x,
              v[0].y, v[1].y, v[2].y,
              v[0].z, v[1].z, v[2].z;

    return result;
}


// 方向余弦マトリックスを返す
// p - 頂点座標
// axis - 断面基準方向ベクトル
vector<Vector3> FElement::dirVectors(vector<Vector3> p, Vector3 axis) {

    if (p.size() == 2) {		// 梁要素
        Vector3 v0 = p[0];
        Vector3 v1 = p[1].clone().sub(v0).normalize();
        
        auto dt = v1.dot(axis);

        Vector3 v2(axis.x - dt * v1.x, axis.y - dt * v1.y, axis.z - dt * v1.z);
        if (v2.lengthSq() > 0) {
            v2.normalize();
        }

        if (v2.lengthSq() == 0) {
            if (Math.abs(v1.x) < Math.abs(v1.y)) {
                v2.set(1 - v1.x * v1.x, -v1.x * v1.y, -v1.x * v1.z).normalize();
            }
            else {
                v2.set(-v1.y * v1.x, 1 - v1.y * v1.y, -v1.y * v1.z).normalize();
            }
        }
        v3.crossVectors(v1, v2);
        return[v1, v2, v3];
    }
    else if (p.length > 2) {		// シェル要素
        v3 = normalVector(p);
        v2 = p[1].clone().sub(p[0]);
        v2 = v3.clone().cross(v2).normalize();
        v1 = v2.clone().cross(v3);
        return[v1, v2, v3];
    }
    return null;
}


void FElement::normalVector(vector<double> p, double out[3]) {
    if (p.size() < 3) {
        return null;
    }
    else if ((p.size() == 3) || (p.size() == 6)) {
        return new THREE.Vector3().subVectors(p[1], p[0]).cross
        (new THREE.Vector3().subVectors(p[2], p[0])).normalize();
    }
    else if ((p.size() == 4) || (p.size() == 8)) {
        return new THREE.Vector3().subVectors(p[2], p[0]).cross
        (new THREE.Vector3().subVectors(p[3], p[1])).normalize();
    }
    else {
        var vx = 0, vy = 0, vz = 0;
        for (var i = 0; i < p.size(); i++) {
            var p1 = p[(i + 1) % p.size()], p2 = p[(i + 2) % p.size()];
            var norm = new THREE.Vector3().subVectors(p1, p[i]).cross
            (new THREE.Vector3().subVectors(p2, p[i]));
            vx += norm.x;
            vy += norm.y;
            vz += norm.z;
        }
        return new THREE.Vector3(vx, vy, vz).normalize();
    }
}


