#include "Solver.h"

//--------------------------------------------------------------------//
// 連立方程式求解オブジェクト
Solver::Solver() {
    //matrix = [];		// 行列
    //matrix2 = [];		// 第２行列
    //vector = [];		// ベクトル
    dof = 0;			// モデル自由度
    method = LU_METHOD;	// 方程式解法
};

// データを消去する
void Solver::clear() {
    //this.matrix.length = 0;
    //this.matrix2.length = 0;
    //this.vector.length = 0;
    dof = 0;
};

// 静解析をする
void Solver::calculate() {
    bool calc = false;
    if ((bc.temperature.size() > 0) || (bc.htcs.size() > 0)) {
        dof = (int)mesh.nodes.size();
        bc.setPointerHeat(dof);
        createHeatMatrix();
        auto tmp = solve();
        result.setTemperature(bc, tmp, (int)mesh.nodes.size());
        calc = true;
    }
    if (bc.restraints.size() > 0) {
        dof = setNodeDoF();
        createStiffnessMatrix();
        auto d = solve();
        result.setDisplacement(bc, d, (int)mesh.nodes.size());
        if (result.type == result.ELEMENT_DATA) {
            calculateElementStress();
        }
        else {
            calculateNodeStress();
        }
        calc = true;
    }
    if (!calc) {
        throw("拘束条件不足のため計算できません");
    }
    //var t1=new Date().getTime();
    //console.log('Calculation time:'+(t1-t0)+'ms');
};

// 伝熱マトリックスを作成する
MatrixXd Solver::heatMatrix() {

    int dof = (int)mesh.nodes.size();
    MatrixXd matrix(dof, dof);

    for (int i = 0; i < mesh.elements.size(); i++) {
        auto elem = mesh.elements[i];
        int count = elem.nodeCount();
        auto mat = materials[elem.material()];
        auto h = mat.hCon;
        MatrixXd ls;
        if (elem.isShell()) {
            int param = elem.param();
            auto sp = shellParams[param];
            auto nodes = mesh.getNodes(elem);
            ls = elem.gradMatrix(nodes, h, sp);
        }
        else if (elem.isBar()) {
            int param = elem.param();
            auto sect = barParams[param].section();
            auto nodes = mesh.getNodes(elem);
            ls = elem.gradMatrix(nodes, h, sect);
        }
        else {
            auto nodes = mesh.getNodes(elem);
            ls = elem.gradMatrix(nodes, h);
        }

        for (int i1 = 0; i1 < count; i1++) {
            auto n = elem.nodes();
            for (int j1 = 0; j1 < count; j1++) {
                matrix(n[i1], n[j1]) += ls(i1, j1);
            }
        }
    }
    return matrix;
}

// 熱境界条件ベクトルを作成する
// matrix - 伝熱マトリックス
VectorXd Solver::tempVector(MatrixXd matrix) {
    auto htcs = bc.htcs;

    VectorXd vector = VectorXd::Zero(mesh.nodes.size());
    /*
    for (int i = 0; i < htcs.size(); i++) {
        auto elem = model.mesh.elements[htcs[i].element];
        auto border = htcs[i].getBorder(elem);
        var p = model.mesh.getNodes(border);
        var h = htcs[i].htc;
        if (border.isEdge) {
            var sp = model.shellParams[elem.param];
            h *= sp.thickness;
        }
        var hm = border.shapeFunctionMatrix(p, h);
        var hv = border.shapeFunctionVector(p, h * htcs[i].outTemp);
        var count = border.nodeCount();
        for (var i1 = 0; i1 < count; i1++) {
            var mrow = matrix[border.nodes[i1]], hrow = hm[i1];
            for (var j1 = 0; j1 < count; j1++) {
                if (border.nodes[j1] in mrow) {
                    mrow[border.nodes[j1]] += hrow[j1];
                }
                else {
                    mrow[border.nodes[j1]] = hrow[j1];
                }
            }
            vector[border.nodes[i1]] += hv[i1];
        }
    }
    */
    return vector;
}

// 熱計算のマトリックス・ベクトルを計算する
void Solver::createHeatMatrix() {

    auto bcList = bc.bcList;
    vector<int> reducedList;
    for (int i = 0; i < bcList.size(); i++) {
        if (bcList[i] < 0) {
            reducedList.push_back(i);
        }
    }

    // 伝熱マトリックス・熱境界条件ベクトルの作成
    auto matrix1 = heatMatrix();
    auto vector1 = tempVector(matrix1);

    // 拘束自由度を除去する
    for (int i = 0; i < bcList.size(); i++) {
        if (bcList[i] >= 0) {
            auto t = bc.temperature[bcList[i]];
            for (int j = 0; j < vector1.size(); j++) {
                vector1(j) -= t.t * matrix1(j, i);
            }
        }
    }
    extruct(matrix1, vector1, reducedList);
}


// 剛性マトリックスを作成する
// dof - モデル自由度
MatrixXd Solver::stiffnessMatrix(int dof) {

    const auto elements = mesh.elements;
    
    MatrixXd matrix(dof, dof);
    MatrixXd km;
    double kmax = 0;

    for (int i = 0; i < elements.size(); i++) {
        auto elem = elements[i];
        auto material = materials[elem.material()];

        if (elem.isShell()) {
            auto sp = shellParams[elem.param()];
            if (elem.getName() == "TriElement1") {
                auto m2d = material.matrix2Dstress();
                km = elem.stiffnessMatrix(mesh.getNodes(elem), m2d, sp);
            }
            else {
                auto msh = material.matrixShell();
                km = elem.stiffnessMatrix(mesh.getNodes(elem), msh, sp);
            }
            kmax = setElementMatrix(elem, 6, matrix, km, kmax);
        }
        else if (elem.isBar()) {
            auto sect = barParams[elem.param()].section();
            km = elem.stiffnessMatrix(mesh.getNodes(elem), material, sect);
            kmax = setElementMatrix(elem, 6, matrix, km, kmax);
        }
        else {
            auto m3d = material.matrix3D();
            km = elem.stiffnessMatrix(mesh.getNodes(elem), m3d);
            kmax = setElementMatrix(elem, 3, matrix, km, kmax);
        }
    }
    // 座標変換
    auto rests = bc.restraints;
    auto index = bc.nodeIndex;
    auto bcdof = bc.dof;
    for (int i = 0; i < rests.size(); i++) {
        auto ri = rests[i];
        if (ri.coords) {
            // ri.coords.transMatrix(matrix, dof, index[ri.node], bcdof[i]);
        }
    }
    // 絶対値が小さい成分を除去する
    //auto eps = PRECISION * kmax;
    //for (int i = 0; i < dof; i++) {
    //    var mrow = matrix[i];
    //    for (j in mrow) {
    //        if (mrow.hasOwnProperty(j)) {
    //            j = parseInt(j);
    //            if (Math.abs(mrow[j]) < eps) {
    //                delete mrow[j];
    //            }
    //        }
    //    }
    //}
    return matrix;
}



// 荷重ベクトルを作成する
// dof - モデル自由度
VectorXd Solver::loadVector(int dof) {
    auto loads = bc.loads;
    auto press = bc.pressures;
    VectorXd vector = VectorXd::Zero(dof);
    auto index = bc.nodeIndex;
    auto bcdof = bc.dof;
    for (int i = 0; i < loads.size(); i++) {
        auto ld = loads[i];
        auto nd = ld.node;
        auto ldx = ld.globalX;
        auto ldof = bcdof[nd];
        auto index0 = index[nd];
        for (int j = 0; j < ldof; j++) {
            vector(index0 + j) = ldx[j];
        }
    }
    for (int i = 0; i < press.size(); i++) {
        /*
        auto border = press[i].getBorder();
        (model.mesh.elements[press[i].element]);
        auto p = model.mesh.getNodes(border);
        auto ps = border.shapeFunctionVector(p, press[i].press);
        auto norm = normalVector(p);
        auto count = border.nodeCount();
        for (int j = 0; j < count; j++) {
            auto index0 = index[border.nodes[j]];
            vector[index0] -= ps[j] * norm.x;
            vector[index0 + 1] -= ps[j] * norm.y;
            vector[index0 + 2] -= ps[j] * norm.z;
        }
        */
    }
    auto rests = bc.restraints;
    for (int i = 0; i < rests.size(); i++) {
        auto ri = rests[i];
        if (ri.coords) {
            // ri.coords.transVector(vector, dof, index[ri.node], bcdof[i]);
        }
    }
    return vector;
}



// 剛性マトリックス・荷重ベクトルを作成する
void Solver::createStiffnessMatrix() {

    auto bcList = bc.bcList;

    vector<int> reducedList;
    for (int i = 0; i < bcList.size(); i++) {
        if (bcList[i] < 0) {
            reducedList.push_back(i);
        }
    }

    // 剛性マトリックス・荷重ベクトルの作成
    MatrixXd matrix1 = stiffnessMatrix(dof);
    VectorXd vector1 = loadVector(dof);

    // 拘束自由度を除去する
    for (int i = 0; i < bcList.size(); i++) {
        if (bcList[i] >= 0) {
            auto rx = bc.getRestDisp(bcList[i]);
            for (int j = 0; j < vector1.size(); j++) {
                vector1(j) -= rx * matrix1(j, i);
            }
        }
    }
    extruct(matrix1, vector1, reducedList);
}


// 要素のマトリックスを設定する
// element - 要素
// dof - 自由度
// matrix - 全体剛性マトリックス
// km - 要素の剛性マトリックス
// kmax - 成分の絶対値の最大値
double Solver::setElementMatrix(ElementManager element, int dof, MatrixXd &matrix,MatrixXd km, double kmax) {
   
    int nodeCount = element.nodeCount();
    auto index = bc.nodeIndex;
    auto nodes = element.nodes();

    for (int i = 0; i < nodeCount; i++) {
        int row0 = index[nodes[i]];
        int i0 = dof * i;
        for (int j = 0; j < nodeCount; j++) {
            int column0 = index[nodes[j]];
            int j0 = dof * j;
            for (int i1 = 0; i1 < dof; i1++) {
                for (int j1 = 0; j1 < dof; j1++) {
                    int cj1 = column0 + j1;
                    matrix(row0 + i1, cj1) += km(i0 + i1, j0 + j1);
                    kmax = max(kmax, abs(matrix(row0 + i1, cj1)));
                }
            }
        }
    }
    return kmax;
}



// 行列の一部を抽出する
// matrix1, vector1 - 元のマトリックス,ベクトル
// list - 抽出部分のリスト
void Solver::extruct(MatrixXd matrix1, VectorXd vector1, vector<int> list) {
    // 
    // https://eigen.tuxfamily.org/dox/group__TutorialSlicingIndexing.html
    int count = list.size();
    _vector.resize(count);
    _matrix.resize(count, count);
    
    int cot = matrix1.rows();

    std::vector<T> matrix_triplets;
    std::vector<T> vector_triplets;

    for (int i = 0; i < count; i++) {
        int a = list[i];
        double value = vector1(a);
        _vector(i) = value;
        if(abs(value) > 0.000000000001)
            vector_triplets.emplace_back(a, 0, value);

        for (int j = 0; j < count; j++) {
            int b = list[j];
             value = matrix1(a, b);
             _matrix(i, j) = value;
            // std::vectorに要素を入れていく
             if (abs(value) > 0.000000000001)
                 matrix_triplets.emplace_back(a, b, value);
        }
    }
    matrix_.resize(cot, cot);
    matrix_.setFromTriplets(matrix_triplets.begin(), matrix_triplets.end());
    vector_.resize(cot, 1);
    vector_.setFromTriplets(vector_triplets.begin(), vector_triplets.end());
}

// 連立方程式を解く
VectorXd Solver::solve() {
    
    Eigen::SparseQR< Eigen::SparseMatrix<double>, Eigen::COLAMDOrdering<int> > solver;  // solverオブジェクトを構築する。
    solver.compute(matrix_);

    if (solver.info() != Eigen::Success) {
        std::cerr << "decomposition failed" << std::endl;
    }
    VectorXd x = solver.solve(vector_);


    Eigen::BiCGSTAB<MatrixXd> bicg;
    // Eigen::ConjugateGradient<MatrixXd> cg;
    bicg.compute(_matrix);
    VectorXd xx = bicg.solve(_vector);


    VectorXd result = _matrix.partialPivLu().solve(_vector);
    // VectorXd result = _matrix.fullPivLu().solve(_vector);
    return result;
    //switch (method) {
    //    case LU_METHOD:
    //    case ILUCG_METHOD:
    //}


};

