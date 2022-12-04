#include "Material.h"

//--------------------------------------------------------------------//
// 材料
// label - 材料番号
// ee - ヤング率 (縦弾性係数) 
// nu - ポアソン比
// dens - 密度
// hCon - 熱伝導率
// sHeat - 比熱
Material::Material(int _label, double _ee, double _nu, double _dens, double _hCon, double _sHeat) {
    label = _label;
    ee = _ee;
    nu = _nu;
    dens = _dens;
    hCon = _hCon;
    sHeat = _sHeat;
    gg = 0.5 * ee / (1 + nu);	// 横弾性係数
    cv = dens * sHeat;		    // 体積比熱
}

// 平面応力問題の応力 - 歪マトリックスを作成する
void Material::matrix2Dstress(){

    double coef= ee / ( 1 - nu * nu );

    //m2d(0, 0) = coef;
    //m2d(0, 1) = coef * nu;
    //m2d(0, 2) = 0;
    //m2d(1, 0) = coef * nu;
    //m2d(1, 1) = coef;
    //m2d(1, 2) = 0;
    //m2d(2, 0) = 0;
    //m2d(2, 1) = 0;
    //m2d(2, 2) = gg;

}

 /*
// 平面歪問題の応力 - 歪マトリックスを作成する
 void Material::matrix2Dstrain(double out(3, 3)) {
  
    double coef = ee / ( ( 1 + nu ) * ( 1 - 2 * nu ) );

    out(0, 0) = coef * (1 - nu);
    out(0, 1) = coef * nu;
    out(0, 2) = 0;
    out(1, 0) = coef * nu;
    out(1, 1) = nu, coef* (1 - nu);
    out(1, 2) = 0;
    out(2, 0) = 0;
    out(2, 1) = 0;
    out(2, 2) = gg;
}

// 軸対称問題の応力 - 歪マトリックスを作成する
 void Material::matrixAxiSymetric(double out(4, 4)) {

    double coef = ee / ( ( 1 + nu ) * ( 1 - 2 * nu ) );
    double s1 = coef * (1 - nu);
    double s2 = coef * nu;

    out(0, 0) = s1;
    out(0, 1) = s2;
    out(0, 2) = s2;
    out(0, 3) = 0;
    out(1, 0) = s2;
    out(1, 1) = s1;
    out(1, 2) = s2;
    out(1, 3) = 0;
    out(2, 0) = s2;
    out(2, 1) = s2;
    out(2, 2) = s1;
    out(2, 3) = 0;
    out(3, 0) = 0;
    out(3, 1) = 0;
    out(3, 2) = 0;
    out(3, 3) = gg;

}

// 捩り問題の応力 - 歪マトリックスを作成する
 void Material::matrixTorsion(double out(2, 2)) {

     out(0, 0) = gg;
     out(0, 1) = 0;
     out(1, 0) = 0;
     out(1, 1) = gg;
}
 */

// 3次元問題の応力 - 歪マトリックスを作成する
void Material::matrix3D() {

    double coef = ee / ( ( 1 + nu ) * ( 1 - 2 * nu ) );
    double s1 = coef * (1 - nu);
    double s2 = coef * nu;

    //m3d(0, 0) = s1;
    //m3d(0, 1) = s2;
    //m3d(0, 2) = s2;
    //m3d(0, 3) = 0;
    //m3d(0, 4) = 0;
    //m3d(0, 5) = 0;

    //m3d(1, 0) = s2;
    //m3d(1, 1) = s1;
    //m3d(1, 2) = s2;
    //m3d(1, 3) = 0;
    //m3d(1, 4) = 0;
    //m3d(1, 5) = 0;

    //m3d(2, 0) = s2;
    //m3d(2, 1) = s2;
    //m3d(2, 2) = s1;
    //m3d(2, 3) = 0;
    //m3d(2, 4) = 0;
    //m3d(2, 5) = 0;

    //m3d(3, 0) = 0;
    //m3d(3, 1) = 0;
    //m3d(3, 2) = 0;
    //m3d(3, 3) = gg;
    //m3d(3, 4) = 0;
    //m3d(3, 5) = 0;

    //m3d(4, 0) = 0;
    //m3d(4, 1) = 0;
    //m3d(4, 2) = 0;
    //m3d(4, 3) = 0;
    //m3d(4, 4) = gg;
    //m3d(4, 5) = 0;

    //m3d(5, 0) = 0;
    //m3d(5, 1) = 0;
    //m3d(5, 2) = 0;
    //m3d(5, 3) = 0;
    //m3d(5, 4) = 0;
    //m3d(5, 5) = gg;

}

// シェル要素の応力 - 歪マトリックスを作成する
 void Material::matrixShell() {

    double coef = ee / ( 1 - nu * nu );
    double s2 = coef * nu;

    //msh(0, 0) = coef;
    //msh(0, 1) = s2;
    //msh(0, 2) = 0;
    //msh(0, 3) = 0;
    //msh(0, 4) = 0;

    //msh(1, 0) = s2;
    //msh(1, 1) = coef;
    //msh(1, 2) = 0;
    //msh(1, 3) = 0;
    //msh(1, 4) = 0;

    //msh(2, 0) = 0;
    //msh(2, 1) = 0;
    //msh(2, 2) = gg;
    //msh(2, 3) = 0;
    //msh(2, 4) = 0;

    //msh(3, 0) = 0;
    //msh(3, 1) = 0;
    //msh(3, 2) = 0;
    //msh(3, 3) = KS_RECT * gg;
    //msh(3, 4) = 0;

    //msh(4, 0) = 0;
    //msh(4, 1) = 0;
    //msh(4, 2) = 0;
    //msh(4, 3) = 0;
    //msh(4, 4) = KS_RECT * gg;

}

// 材料を表す文字列を返す
 string Material::toString(){

     //return format("Material\t{}\t{}\t{}\t{}\t{}\t{}\t{}", 
     //    label,ee, nu, gg, dens, hCon, sHeat);

     return "Material";
}
