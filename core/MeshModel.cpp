#include "MeshModel.h"

//--------------------------------------------------------------------//
// メッシュモデル
MeshModel::MeshModel(){
  clear();
};


// 節点を返す
// s - 節点集合
vector<FENode> MeshModel::getNodes(ElementManager s){

    auto inodes = s.nodes();
    vector<FENode> p;

    for(int i=0;i< inodes.size();i++){
        auto n = nodes[inodes[i]];
        p.push_back(n);
    }
    return p;
}


// データを消去する
void MeshModel::clear(){
    nodes.clear();    // 節点
    elements.clear();	// 要素
};


// モデルを初期化する
void MeshModel::init(){

    sort(nodes.begin(), nodes.end(), 
        [](const FENode& o1, const FENode& o2)
        {
            return o1.label < o2.label;
        });


};


/// <summary>
/// 要素の鏡像向きを揃える
/// </summary>
void MeshModel::checkChirality() {
    //for (int i = 0; i < elements.size(); i++) {
    //    auto elem = elements[i];
    //    if (!elem.isShell() && !elem.isBar()) {
    //        auto pe = getNodes(elem);
    //        auto pf = getNodes(elem.border(i, 0));
    //        auto n1 = normalVector(pf);
    //        auto n2 = center(pe).sub(center(pf));
    //        if (n1.dot(n2) > 0) {
    //            elem.mirror();
    //        }
    //    }
    //}
}

/// <summary>
/// // 表面を取り出す
/// </summary>
void MeshModel::getFreeFaces() {
    //var elems = this.elements, i;
    //if (elems.length == = 0) return;
    //this.freeFaces.length = 0;
    //var border = [];
    //for (i = 0; i < elems.length; i++) {
    //    if (elems[i].isShell) {
    //        this.freeFaces.push(elems[i].border(i, 0));
    //    }
    //    else if (!elems[i].isBar) {
    //        var count = elems[i].borderCount();
    //        for (var j = 0; j < count; j++) {
    //            border.push(elems[i].border(i, j));
    //        }
    //    }
    //}
    //if (border.length > 0) {
    //    border.sort(function(b1, b2) { return b1.compare(b2); });
    //    var addsw = true, beforeEb = border[0];
    //    for (i = 1; i < border.length; i++) {
    //        var eb = border[i];
    //        if (beforeEb.compare(eb) == = 0) {
    //            addsw = false;
    //        }
    //        else {
    //            if (addsw) this.freeFaces.push(beforeEb);
    //            beforeEb = eb;
    //            addsw = true;
    //        }
    //    }
    //    if (addsw) this.freeFaces.push(beforeEb);
    //}
}

/// <summary>
/// 表面の要素辺を取り出す
/// </summary>
void MeshModel::getFaceEdges() {
    //if (this.freeFaces.length == = 0) return;
    //this.faceEdges.length = 0;
    //var edges = [], i;
    //for (i = 0; i < this.freeFaces.length; i++) {
    //    var nds = this.freeFaces[i].cycleNodes();
    //    for (var j = 0; j < nds.length; j++) {
    //        edges.push(new EdgeBorder1(i, [nds[j], nds[(j + 1) % nds.length]]));
    //    }
    //}
    //if (edges.length > 0) {
    //    edges.sort(function(b1, b2) { return b1.compare(b2); });
    //    var beforeEdge = edges[0];
    //    this.faceEdges.push(beforeEdge);
    //    for (i = 1; i < edges.length; i++) {
    //        var edge = edges[i];
    //        if (beforeEdge.compare(edge) != = 0) {
    //            this.faceEdges.push(edge);
    //            beforeEdge = edge;
    //        }
    //    }
    //}
}
