#include "FENode.h";
#include "Element.h";

#include <vector>
using namespace std;
using std::vector;


class MeshModel {

    private:
        vector<FENode> nodes;		// 節点
        vector<FElement> elements;  // 要素

    public:
        MeshModel();

        template <typename T>
        vector<FENode> getNodes(T s);

        void clear();
};

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
MeshModel.prototype.init=function(){
  this.nodes.sort(compareLabel);
  bounds.set();
};

