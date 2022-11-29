#pragma once

#include "Vector3R.h"
#include "Strain.h"
#include "Stress.h"
#include "EigenValue.h"

#include<map> 
#include<string>
#include<vector>
using namespace std;
using std::string;
using std::vector;

class Result {

private:


    // データ型
    int NONE = -1;		    // 空データ
    int DISPLACEMENT = 0;	    // 変位
    int STRAIN = 1;		    // 歪
    int STRESS = 2;		    // 応力
    int S_ENERGY = 3;		    // 歪エネルギー密度
    int TEMPERATURE = 4;	    // 温度

    // 成分
    int X = 0;		        // x成分
    int Y = 1;		        // y成分
    int Z = 2;		        // z成分
    int RX = 3;		        // x軸回転成分
    int RY = 4;		        // y軸回転成分
    int RZ = 5;		        // z軸回転成分
    int XY = 3;		        // xyせん断成分
    int YZ = 4;		        // yzせん断成分
    int ZX = 5;		        // zxせん断成分
    int MAGNITUDE = 6;	    // 大きさ
    int MAX_PRINCIPAL = 7;	// 最大主成分
    int MIN_PRINCIPAL = 8;	// 最小主成分
    int MID_PRINCIPAL = 9;	// 中間主成分
    int MAX_SHARE = 10;	    // 最大せん断成分
    int VON_MISES = 11;	    // ミーゼス応力
    int SHIFT = 12;		    // 成分シフト量

    // 変位の成分
    string DISP_COMPONENT[4] = { "Mag.", "x", "y", "z" };

    string DISP2_COMPONENT[7] = { "Mag.", "x", "y", "z", "rotx", "roty", "rotz" };
    // 歪の成分
    string STRAIN_COMPONENT[10] = { "Max.prin.", "Min.prin.", "Mid.prin.", "Max.share",
                                        "x", "y", "z", "xy", "yz", "zx" };
    // 応力の成分
    string STRESS_COMPONENT[11] = { "Max.prin.", "Min.prin.", "Mid.prin.",
                                        "Max.share", "Von mises",
                                        "x", "y", "z", "xy", "yz", "zx" };
    // 歪エネルギー密度の成分
    string  ENERGY_COMPONENT[1] = {"Energy"};

    map<string, int> COMP_MAP {
        {"Mag." , MAGNITUDE},
        {"x" , X},
        {"y" , Y},
        {"z" , Z},
        {"rotx" , RX},
        {"roty" , RY},
        {"rotz" , RZ},
        {"xy" , XY},
        {"yz" , YZ},
        {"zx" , ZX},
        {"Max.prin." , MAX_PRINCIPAL},
        {"Min.prin." , MIN_PRINCIPAL},
        {"Mid.prin." , MID_PRINCIPAL},
        {"Max.share" , MAX_SHARE},
        {"Von mises" , VON_MISES},
        {"Energy" , 0},
        {"x 1" , X},
        {"y 1" , Y},
        {"z 1" , Z},
        {"xy 1" , XY},
        {"yz 1" , YZ},
        {"zx 1" , ZX},
        {"Max.prin. 1" , MAX_PRINCIPAL},
        {"Min.prin. 1" , MIN_PRINCIPAL},
        {"Mid.prin. 1" , MID_PRINCIPAL},
        {"Max.share 1" , MAX_SHARE},
        {"Von mises 1" , VON_MISES},
        {"Energy 1" , 0},
        {"x 2" , X + SHIFT},
        {"y 2" , Y + SHIFT},
        {"z 2" , Z + SHIFT},
        {"xy 2" , XY + SHIFT},
        {"yz 2" , YZ + SHIFT},
        {"zx 2" , ZX + SHIFT},
        {"Max.prin. 2" , MAX_PRINCIPAL + SHIFT},
        {"Min.prin. 2" , MIN_PRINCIPAL + SHIFT},
        {"Mid.prin. 2" , MID_PRINCIPAL + SHIFT},
        {"Max.share 2" , MAX_SHARE + SHIFT},
        {"Von mises 2" , VON_MISES + SHIFT},
        {"Energy 2" , 1}
    };

    double EIG_EPS = 1e-10;           // 固有値計算の収束閾値
    int ELEMENT_DATA = 1;		        // 要素データ
    string VIBRATION = "Vibration";	// 振動解析
    string BUCKLING = "Buckling";	    // 座屈解析

    
    vector<Vector3R> displacement;  // 変位
    double dispMax;		            // 変位の大きさの最大値
    double angleMax;		        // 回転角の大きさの最大値
    vector<Strain> strain1;		    // 節点歪
    vector<Stress> stress1;		    // 節点応力
    vector<double> sEnergy1;		// 節点歪エネルギー密度
    vector<Strain> strain2;
    vector<Stress> stress2;
    vector<double> sEnergy2;
    vector<double> temperature;		// 節点温度
    double tempMax;		            // 温度の最大値
    vector<EigenValue> eigenValue;	// 固有値データ
    bool calculated;	            // 計算前＝計算結果無し
    vector<double> value;		    // コンター図データ
    double minValue;		        // コンター図データ最小値
    double maxValue;		        // コンター図データ最大値

public:
    int NODE_DATA = 0;		// 節点データ
    int type;		                // データ保持形態：節点データ

    Result();
    void clear();
};

