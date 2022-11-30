#include "MeshModel.h"

//--------------------------------------------------------------------//
// メッシュモデル
MeshModel::MeshModel(){
  clear();
};


// 節点を返す
// s - 節点集合
template <typename T>
vector<FENode> MeshModel::getNodes(T s){

    vector<FENode> p(s.nodes.size());

  for(int i=0;i< s.nodes.size();i++){
    p[i]= nodes[s.nodes[i]];
  }
  return p;
};


// データを消去する
void MeshModel::clear(){
    nodes.clear();    // 節点
    elements.clear();	// 要素
};


// モデルを初期化する
void MeshModel::init(){

    sort(nodes.begin(), nodes.end(),
        [](FENode o1, FENode o2) -> int {
            if (o1.label < o2.label)        return -1;
            else if (o1.label > o2.label)   return 1;
            else                            return 0;
        });

};

