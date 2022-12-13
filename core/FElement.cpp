#include "FElement.h"

//--------------------------------------------------------------------//
// 要素
// label - 要素ラベル
// material - 材料のインデックス
// nodes - 節点番号
FElement::FElement() : Nodes() {
    label = -1;
    material = -1;
    isShell = false;        // シェル要素ではない
    isBar = false;
}
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

    int size1 = (int)b.rows();
    int size2 = (int)d.rows();

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

    VectorXd result(count + dof);

    for (int i = 0; i < count; i++) {

        double* ux = u[i].x;

        for (int j = 0; j < dof; j++) {
            int index = i + j;
            result(index) = ux[j];
        }
    }

    return result;
}


// 節点変位を局所座標系・1次元配列に変換する
// u - 節点変位
// d - 方向余弦マトリックス
VectorXd FElement::toLocalArray(vector<Vector3R> u, MatrixXd d) {

    VectorXd v(12);

    int index = 0;
    for (int i = 0; i < 2; i++) {
        double* ux = u[i].x;
        for (int j = 0; j < 3; j++) {
            v(index) = d(0, j) * ux[0] + d(1, j) * ux[1] + d(2, j) * ux[2];
        }
        for (int j = 0; j < 3; j++) {
            v(index) = d(0, j) * ux[3] + d(1, j) * ux[4] + d(2, j) * ux[5];
        }
    }

    return v;
}

// 方向余弦マトリックスを返す
// p - 頂点座標
// axis - 断面基準方向ベクトル
MatrixXd FElement::dirMatrix(vector<FENode> p, Vector3 axis) {

    vector<Vector3> vec(p.size());
    for (int i = 0; i < p.size(); i++) {
        vec[i] = Vector3(p[i].x, p[i].y, p[i].z);
    }

    vector<Vector3> v = dirVectors(vec, axis);

    MatrixXd result(3, 3);
    result(0, 0) = v[0].x;
    result(0, 1) = v[1].x;
    result(0, 2) = v[2].x;
    
    result(1, 0) = v[0].y;
    result(1, 1) = v[1].y;
    result(1, 2) = v[2].y;
    
    result(2, 0) = v[0].z;
    result(2, 1) = v[1].z;
    result(2, 2) = v[2].z;

    return result;
}

MatrixXd FElement::dirMatrix(vector<FENode> p) {
    // この関数を呼ばれる時は axis を使わないはずだからダミーの axis を用意する
    Vector3 axis;
    return dirMatrix(p, axis);
}


// 方向余弦マトリックスを返す
// p - 頂点座標
// axis - 断面基準方向ベクトル
vector<Vector3> FElement::dirVectors(vector<Vector3> p, Vector3 axis) {

    vector<Vector3> result(3);

    if (p.size() == 2) {		// 梁要素
        Vector3 v0 = p[0];
        Vector3 v1 = p[1].clone().sub(v0).normalize();
        
        double dt = v1.dot(axis);

        auto v2 = Vector3(axis.x - dt * v1.x, axis.y - dt * v1.y, axis.z - dt * v1.z);
        if (v2.lengthSq() > 0) {
            v2.normalize();
        }

        if (v2.lengthSq() == 0) {
            if (abs(v1.x) < abs(v1.y)) {
                v2.set(1 - v1.x * v1.x, -v1.x * v1.y, -v1.x * v1.z).normalize();
            }
            else {
                v2.set(-v1.y * v1.x, 1 - v1.y * v1.y, -v1.y * v1.z).normalize();
            }
        }
        auto v3 = Vector3().crossVectors(v1, v2);

        result[0] = v1;
        result[1] = v2;
        result[2] = v3;

        return result;
    }
    else if (p.size() > 2) {		// シェル要素
        Vector3 v3 = normalVector(p);
        Vector3 v2 = p[1].clone().sub(p[0]);
        v2 = v3.clone().cross(v2).normalize();
        Vector3 v1 = v2.clone().cross(v3);

        result[0] = v1;
        result[1] = v2;
        result[2] = v3;

        return result;

    }

    throw exception("error on FElement::dirVectors");
}

Vector3 FElement::normalVector(vector<FENode> p) {

    vector<Vector3> v(p.size());
    for (int i = 0; i < p.size(); i++) {
        v[i] = Vector3(p[i].x, p[i].y, p[i].z);
    }

    return normalVector(v);
}

Vector3 FElement::normalVector(vector<Vector3> v) {

    if (v.size() < 3) {
        throw exception("error on FElement::normalVector");
    }
    else if ((v.size() == 3) || (v.size() == 6)) {
        auto v1 = Vector3().subVectors(v[1], v[0]);
        auto v2 = Vector3().subVectors(v[2], v[0]);
        auto v3 = v1.cross(v2).normalize();
        return v3;
    }
    else if ((v.size() == 4) || (v.size() == 8)) {
        auto v1 = Vector3().subVectors(v[2], v[0]);
        auto v2 = Vector3().subVectors(v[3], v[1]);
        auto v3 = v1.cross(v2).normalize();
        return v3;
    }
    else {
        double vx = 0, vy = 0, vz = 0;
        for (int i = 0; i < v.size(); i++) {

            Vector3 p1 = v[(i + 1) % v.size()];
            Vector3 p2 = v[(i + 2) % v.size()];

            auto v1 = Vector3().subVectors(p1, v[i]);
            auto v2 = Vector3().subVectors(p2, v[i]);
            auto norm = v1.cross(v2);

            vx += norm.x;
            vy += norm.y;
            vz += norm.z;
        }
        auto v3 = Vector3(vx, vy, vz).normalize();
        return v3;
    }

    return Vector3();
}

// 剛性マトリックスの方向を修正する
// d - 方向余弦マトリックス
// k - 剛性マトリックス
void FElement::toDir3(MatrixXd d, MatrixXd k) {

    MatrixXd a = MatrixXd::Zero(3, 3);

    for (int i = 0; i < k.size(); i += 3) {
        for (int j = 0; j < k.row(i).size(); j += 3) {
            for (int i1 = 0; i1 < 3; i1++) {
                 for (int j1 = 0; j1 < 3; j1++) {
                    double s = 0;
                    for (int ii = 0; ii < 3; ii++) {
                        s += d(i, ii) * k(i + ii, j + j1);
                    }
                    a(i, j1) = s;
                }
            }
            for (int i1 = 0; i1 < 3; i1++) {
                VectorXd ai = a.row(i1);
                VectorXd ki = k.row(i + i1);
                for (int j1 = 0; j1 < 3; j1++) {
                    ki(j + j1) = ai.dot(d.row(j1));
                }
            }
        }
    }
}

