#include "SectionManager.h"

SectionManager::SectionManager() {
    type = "";
    _CircleSection = CircleSection();
    _RectSection = RectSection();
}
SectionManager::SectionManager(string _type, vector<double> ss) {
    type = _type;

    if (type == "circle") {
        _CircleSection = CircleSection(ss);
    }
    else if (type == "rectangle") {
        _RectSection = RectSection(ss);
    }
}

// ’f–ÊÏ
double SectionManager::area() {
    if (type == "circle") {
        return _CircleSection.area;
    }
    else if (type == "rectangle") {
        return _RectSection.area;
    }
    return 0;
}

// ’f–Ê‚QŸƒ‚[ƒƒ“ƒg
double SectionManager::iy() {
    if (type == "circle") {
        return _CircleSection.iy;
    }
    else if (type == "rectangle") {
        return _RectSection.iy;
    }
    return 0;

}
double SectionManager::iz() {
    if (type == "circle") {
        return _CircleSection.iz;
    }
    else if (type == "rectangle") {
        return _RectSection.iz;
    }
    return 0;

}

// ’f–Ê‚QŸ‹Éƒ‚[ƒƒ“ƒg
double SectionManager::ip() {
    if (type == "circle") {
        return _CircleSection.ip;
    }
    else if (type == "rectangle") {
        return _RectSection.ip;
    }
    return 0;

}




// ‚¹‚ñ’f•â³ŒW”‚ğ•Ô‚·
double SectionManager::shearCoef() {
    if (type == "circle") {
        return _CircleSection.shearCoef();
    }
    else if (type == "rectangle") {
        return _RectSection.shearCoef();
    }
}

// ˜cE‰—ÍƒxƒNƒgƒ‹‚ğ•Ô‚·
MatrixXd SectionManager::strainStress(Material material, double ex, double thd, double kpy, double kpz,
    double sy, double sz) {
    if (type == "circle") {
        return _CircleSection.strainStress(material, ex, thd, kpy, kpz, sy, sz);
    }
    else if (type == "rectangle") {
        return _RectSection.strainStress(material, ex, thd, kpy, kpz, sy, sz);
    }

    return MatrixXd(1, 1);
}

// ¿—ÊEdSü‚è‚ÌŠµ«ƒ‚[ƒƒ“ƒg‚ğ•Ô‚·
VectorXd SectionManager::massInertia(double dens, double l) {
    if (type == "circle") {
        return _CircleSection.massInertia(dens, l);
    }
    else if (type == "rectangle") {
        return _RectSection.massInertia(dens, l);
    }
    return VectorXd(1);

}

// ’f–Ê‚ğ•\‚·•¶š—ñ‚ğ•Ô‚·
string SectionManager::toString() {
    if (type == "circle") {
        return _CircleSection.toString();
    }
    else if (type == "rectangle") {
        return _RectSection.toString();
    }
    return "";
}