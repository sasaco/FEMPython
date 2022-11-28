#pragma once
class Result
{

private:
    /*
    // データ型
    var NONE = -1;		// 空データ
    var DISPLACEMENT = 0;	// 変位
    var STRAIN = 1;		// 歪
    var STRESS = 2;		// 応力
    var S_ENERGY = 3;		// 歪エネルギー密度
    var TEMPERATURE = 4;	// 温度
    // 成分
    var X = 0;		// x成分
    var Y = 1;		// y成分
    var Z = 2;		// z成分
    var RX = 3;		// x軸回転成分
    var RY = 4;		// y軸回転成分
    var RZ = 5;		// z軸回転成分
    var XY = 3;		// xyせん断成分
    var YZ = 4;		// yzせん断成分
    var ZX = 5;		// zxせん断成分
    var MAGNITUDE = 6;	// 大きさ
    var MAX_PRINCIPAL = 7;	// 最大主成分
    var MIN_PRINCIPAL = 8;	// 最小主成分
    var MID_PRINCIPAL = 9;	// 中間主成分
    var MAX_SHARE = 10;	// 最大せん断成分
    var VON_MISES = 11;	// ミーゼス応力
    var SHIFT = 12;		// 成分シフト量
    // 変位の成分
    var DISP_COMPONENT = ['Mag.', 'x', 'y', 'z'];
    var DISP2_COMPONENT = ['Mag.', 'x', 'y', 'z', 'rotx', 'roty', 'rotz'];
    // 歪の成分
    var STRAIN_COMPONENT = ['Max.prin.', 'Min.prin.', 'Mid.prin.',
        'Max.share',
        'x', 'y', 'z', 'xy', 'yz', 'zx'];
    // 応力の成分
    var STRESS_COMPONENT = ['Max.prin.', 'Min.prin.', 'Mid.prin.',
        'Max.share', 'Von mises',
        'x', 'y', 'z', 'xy', 'yz', 'zx'];
    // 歪エネルギー密度の成分
    var ENERGY_COMPONENT = ['Energy'];
    var COMP_MAP = { 'Mag.':MAGNITUDE,'x' : X,'y' : Y,'z' : Z,
                  'rotx' : RX,'roty' : RY,'rotz' : RZ,'xy' : XY,'yz' : YZ,'zx' : ZX,
                  'Max.prin.' : MAX_PRINCIPAL,'Min.prin.' : MIN_PRINCIPAL,
                  'Mid.prin.' : MID_PRINCIPAL,'Max.share' : MAX_SHARE,
                  'Von mises' : VON_MISES,'Energy' : 0,
                  'x 1' : X,'y 1' : Y,'z 1' : Z,'xy 1' : XY,'yz 1' : YZ,'zx 1' : ZX,
                  'Max.prin. 1' : MAX_PRINCIPAL,'Min.prin. 1' : MIN_PRINCIPAL,
                  'Mid.prin. 1' : MID_PRINCIPAL,'Max.share 1' : MAX_SHARE,
                  'Von mises 1' : VON_MISES,'Energy 1' : 0,
                  'x 2' : X + SHIFT,'y 2' : Y + SHIFT,'z 2' : Z + SHIFT,
                  'xy 2' : XY + SHIFT,'yz 2' : YZ + SHIFT,'zx 2' : ZX + SHIFT,
                  'Max.prin. 2' : MAX_PRINCIPAL + SHIFT,
                  'Min.prin. 2' : MIN_PRINCIPAL + SHIFT,
                  'Mid.prin. 2' : MID_PRINCIPAL + SHIFT,
                  'Max.share 2' : MAX_SHARE + SHIFT,
                  'Von mises 2' : VON_MISES + SHIFT,'Energy 2' : 1 };
    var EIG_EPS = 1e-10;		// 固有値計算の収束閾値
    var NODE_DATA = 0;		// 節点データ
    var ELEMENT_DATA = 1;		// 要素データ
    var VIBRATION = 'Vibration';	// 振動解析
    var BUCKLING = 'Buckling';	// 座屈解析
    */

public:
    Result();
    void clear();
};

