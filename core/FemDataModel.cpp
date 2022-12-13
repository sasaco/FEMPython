#include "FemDataModel.h"


FemDataModel::FemDataModel() {
    mesh = MeshModel();		    // メッシュモデル
    bc = BoundaryCondition();	// 境界条件
    // solver = Solver();		    // 連立方程式求解オブジェクト
    result = Result();          // 計算結果
    hasShellBar = false;		// シェル要素または梁要素を含まない
}

// データを消去する
void FemDataModel::clear(){
    
    materials.clear();
    shellParams.clear();
    barParams.clear();
    coordinates.clear();
    mesh.clear();
    bc.clear();
    result.clear();
    result.type= result.NODE_DATA;
    
};

// モデルを初期化する
void FemDataModel::init(){

  // solver.method= solver.ILUCG_METHOD;   // デフォルトは反復解法
  auto mats = materials;
  sort(mats.begin(), mats.end(),
      [](const Material& o1, const Material& o2)
      {
          return o1.label < o2.label;
      });

  mesh.init();
  bc.init();

  reNumbering();
  resetMaterialLabel();
  resetParameterLabel();
  resetCoordinates();

  mesh.checkChirality();
  mesh.getFreeFaces();
  mesh.getFaceEdges();

}


// 節点・要素ポインタを設定する
void FemDataModel::reNumbering(){

    auto nodes = mesh.nodes;
    auto elements = mesh.elements;

    //vector<int> map1(nodes.size());
    map<int, int> map1;

    for(int i=0;i<nodes.size();i++){
        map1[nodes[i].label]=i;
    }

    for(int i=0;i<elements.size();i++){
        resetNodes(map1, elements[i]);
    }

    for(int i=0;i<bc.restraints.size();i++){
        resetNodePointer(map1, bc.restraints[i].node);
    }
    for(int i=0;i<bc.loads.size();i++){
        resetNodePointer(map1, bc.loads[i].node);
    }
    for(int i=0;i<bc.temperature.size();i++){
        resetNodePointer(map1, bc.temperature[i].node);
    }

    map<int, int> map2;
    for(int i=0;i<elements.size();i++){
        map2[elements[i].label()] = i;
    }

    for(int i=0;i<bc.pressures.size();i++){
        resetElementPointer(map2, bc.pressures[i].element);
    }
    for(int i=0;i<bc.htcs.size();i++){
        resetElementPointer(map2, bc.htcs[i].element);
    }
}

// 節点集合の節点ラベルを再設定する
// map - ラベルマップ
// s - 節点集合
void FemDataModel::resetNodes(map<int, int> map, ElementManager &s) {
    vector<int> nodes = s.nodes();
    vector<int> tmp;
    for (int i = 0; i < nodes.size(); i++) {
        if (map.count(nodes[i])) {
            tmp.push_back(map[nodes[i]]);
            //nodes[i] = map[nodes[i]];
        }
        else {
            throw (format("節点番号{}は存在しません", nodes[i]));
        }
    }
    s.setNodes(tmp);
}

// 節点ポインタを再設定する
// map - ラベルマップ
// bc - 境界条件
void FemDataModel::resetNodePointer(map<int, int> map, int &node) {
    if ( map.count(node) > 0) {
        node = map[node];
    }
    else {
        throw (format("節点番号{}は存在しません", node));
    }
}

// 要素ポインタを再設定する
// map - ラベルマップ
// bc - 境界条件
void FemDataModel::resetElementPointer(map<int, int> map, int &element) {
    if (map.count(element) > 0) {
        element = map[element];
    }
    else {
        throw (format("要素番号{}は存在しません", element));
    }
}

// 材料ポインタを設定する
void FemDataModel::resetMaterialLabel(){

  if(materials.size()==0){
    materials.push_back(Material(1, 1, 0.3, 1, 1, 1));
  }
  map<int, int> map;
  auto elements = mesh.elements;

  for(int i=0;i<materials.size();i++){
    map[materials[i].label]=i;
  }
  for(int i=0;i<elements.size();i++){
    int mat = elements[i].material();
    if (map.count(mat) > 0) {
        elements[i].setMaterial(map[mat]);
    }
    else{
        throw (format("材料番号{}は存在しません", mat));
    }
  }
}

// シェルパラメータ・梁パラメータのポインタを設定する
void FemDataModel::resetParameterLabel(){

    if((shellParams.size()==0) && (barParams.size()==0)){
        hasShellBar=false;
        return;
    }

    map<int, int> map1;
    map<int, int> map2;
    auto elements = mesh.elements;
    int shellbars = 0;

    for(int i=0;i<shellParams.size();i++){
        map1[shellParams[i].label]=i;
    }
    for(int i=0;i<barParams.size();i++){
        map2[barParams[i].label]=i;
    }

    for(int i=0;i<elements.size();i++){

        if(elements[i].isShell()){
            int param = elements[i].param();
            if (map1.count(param) > 0) {
      	        elements[i].setParam(map1[param]);
      	        shellbars++;
            }
            else{
                throw (format("パラメータ番号{}は存在しません", param));
            }
        }
        else if(elements[i].isBar()){
            int param = elements[i].param();

            if (map2.count(param) > 0) {
            elements[i].setParam(map2[param]);
            shellbars++;
            }
            else{
                throw (format("パラメータ番号{}は存在しません", param));
            }
        }
    }
    hasShellBar=(shellbars>0);

    if(hasShellBar){		// シェル要素・梁要素を含む場合は直接解法
        // solver.method= solver.LU_METHOD;
    }
}

// 局所座標系を設定する
void FemDataModel::resetCoordinates(){

  if(coordinates.size()==0){
    return;
  }

  map<int, Coordinates> map;
  for(int i=0;i<coordinates.size();i++){
    map[coordinates[i].label]=coordinates[i];
  }

  for(int i=0;i<bc.restraints.size();i++){
      resetCoordinatesPointer<Restraint>(map, bc.restraints[i]);
  }
  for(int i=0;i<bc.loads.size();i++){
      resetCoordinatesPointer<Load>(map, bc.loads[i]);
  }
}

// 局所座標系を再設定する
// map - ラベルマップ
// bc - 境界条件
template <typename T>
void FemDataModel::resetCoordinatesPointer(map<int, Coordinates> map, T &bc) {
    if (bc.coords < 0) {
        // 何もしない
        return;
    }
    bool hasFind = false;
    Coordinates cod;
    for (int i = 0; i < map.size(); i++) {
        if (i == map[i].label) {
            cod = map[i];
            hasFind = true;
            break;
        }
    }

    if (hasFind) {
        bc.coords = cod.label;
        cod.toGlobal(bc.x, bc.globalX);
    }
    else {
        throw (format("局所座標系番号{}存在しません", bc.coords));
    }
}


// 節点の自由度を設定する
int FemDataModel::setNodeDoF(){

    int nodeCount = (int)mesh.nodes.size();
    int elemCount = (int)mesh.elements.size();
    bc.dof.clear();
    for(int i=0;i<nodeCount;i++){
        bc.dof.push_back(3);
    }
    for(int i=0;i<elemCount;i++){
        auto elem = mesh.elements[i];
        auto nodes = elem.nodes();
        if(elem.isShell() || elem.isBar()) {	// シェル要素・梁要素
            int count=elem.nodeCount();
            for(int j=0;j<count;j++){
                bc.dof[nodes[j]] = 6;
            }
        }
    }
    return bc.setPointerStructure(nodeCount);
}




// 要素歪・応力・歪エネルギー密度を計算する
void FemDataModel::calculateElementStress() {
    auto nodes = mesh.nodes;
    // p = [], v = [], s;
    auto elems = mesh.elements;
    int elemCount = (int)elems.size();
    result.initStrainAndStress(elemCount);
    vector<FENode> p;
    vector<Vector3R> v;
    for (int i = 0; i < elemCount; i++) {
        auto elem = elems[i];
        auto en = elem.nodes();
        for (int j = 0; j < en.size(); j++) {
            int index = en[j];
            p.push_back(nodes[index]);
            v.push_back(result.displacement[index]);
        }
        auto material = materials[elem.material()];
        if (elem.isShell()) {
            MatrixXd m2d = material.matrix2Dstress();
            MatrixXd msh = material.matrixShell();
            auto sp = shellParams[elem.param()];
            MatrixXd mmat;
            if (elem.getName() == "TriElement1") {
                mmat = m2d;
            }
            else {
                mmat = msh;
            }
            auto s = elem.elementStrainStress(p, v, mmat, sp);
            result.addStructureData(i, get<0>(s), get<1>(s), get<2>(s), get<3>(s), get<4>(s), get<5>(s));
        }
        else if (elem.isBar()) {
            auto sect = barParams[elem.param()].section();
            auto s = elem.elementStrainStress(p, v, material, sect);
            result.addStructureData(i, get<0>(s), get<1>(s), get<2>(s), get<3>(s), get<4>(s), get<5>(s));
        }
        else {
            MatrixXd m3d = material.matrix3D();
            auto s = elem.elementStrainStress(p, v, m3d);
            result.addStructureData(i, get<0>(s), get<1>(s), get<2>(s), get<0>(s), get<1>(s), get<2>(s));
        }
    }
}



// 節点歪・応力・歪エネルギー密度を計算する
void FemDataModel::calculateNodeStress() {
    auto nodes = mesh.nodes;
    int nodeCount = (int)nodes.size();
    auto elems = mesh.elements;
    int elemCount = (int)elems.size();
    VectorXd angle = VectorXd::Zero(nodeCount);
    vector<FENode> p;
    vector<Vector3R> v;
    result.initStrainAndStress(nodeCount);
    for (int i = 0; i < elemCount; i++) {
        auto elem = elems[i];
        auto en = elem.nodes();
        for (int j = 0; j < en.size(); j++) {
            int index = en[j];
            p.push_back(nodes[index]);
            v.push_back(result.displacement[index]);
        }
        auto material = materials[elem.material()];
        auto m2d = material.matrix2Dstress();
        auto msh = material.matrixShell();
        auto ea = elem.angle(p);
        if (elem.isShell()) {
            auto sp = shellParams[elem.param()];
            MatrixXd mmat;
            if (elem.getName() == "TriElement1") {
                mmat = m2d;
            }
            else {
                mmat = msh;
            }
            auto s = elem.strainStress(p, v, mmat, sp);
            auto eps1 = get<0>(s);
            auto str1 = get<1>(s);
            auto se1 = get<2>(s);
            auto eps2 = get<3>(s);
            auto str2 = get<4>(s);
            auto se2 = get<5>(s);
            for (int j = 0; j < en.size(); j++) {
                auto eaj = ea[j];
                eps1[j].mul(eaj);
                eps2[j].mul(eaj);
                str1[j].mul(eaj);
                str2[j].mul(eaj);
                se1[j] *= eaj;
                se2[j] *= eaj;
                result.addStructureData(en[j], eps1[j], str1[j], se1[j],
                    eps2[j], str2[j], se2[j]);
                angle(en[j]) += eaj;
            }
        }
        else if (elem.isBar()) {
            auto sect = barParams[elem.param()].section();
            auto s = elem.strainStress(p, v, material, sect);
            auto eps1 = get<0>(s);
            auto str1 = get<1>(s);
            auto se1 = get<2>(s);
            auto eps2 = get<3>(s);
            auto str2 = get<4>(s);
            auto se2 = get<5>(s);
            for (int j = 0; j < en.size(); j++) {
                int index = en[j];
                result.addStructureData(index, eps1[j], str1[j], se1[j],
                    eps2[j], str2[j], se2[j]);
                angle(index) += 1;
            }
        }
        else {
            auto m3d = material.matrix3D();
            auto s = elem.strainStress(p, v, m3d);
            auto eps1 = get<0>(s);
            auto str1 = get<1>(s);
            auto se1 = get<2>(s);
            for (int j = 0; j < en.size(); j++) {
                auto eaj = ea[j];
                eps1[j].mul(eaj);
                str1[j].mul(eaj);
                se1[j] *= eaj;
                result.addStructureData(en[j], eps1[j], str1[j], se1[j],
                    eps1[j], str1[j], se1[j]);
                angle(en[j]) += eaj;
            }
        }
    }
    for (int i = 0; i < nodeCount; i++) {
        if (angle(i) != 0) {
            result.mulStructureData(i, 1 / angle(i));
        }
    }
};


