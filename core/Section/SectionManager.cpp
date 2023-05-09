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

// �f�ʐ�
double SectionManager::area() {
    if (type == "circle") {
        return _CircleSection.area;
    }
    else if (type == "rectangle") {
        return _RectSection.area;
    }
    return 0;
}

// �f�ʂQ�����[�����g
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

// �f�ʂQ���Ƀ��[�����g
double SectionManager::ip() {
    if (type == "circle") {
        return _CircleSection.ip;
    }
    else if (type == "rectangle") {
        return _RectSection.ip;
    }
    return 0;

}




// ����f�␳�W����Ԃ�
double SectionManager::shearCoef() {
    if (type == "circle") {
        return _CircleSection.shearCoef();
    }
    else if (type == "rectangle") {
        return _RectSection.shearCoef();
    }
}

// �c�E���̓x�N�g����Ԃ�
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

// ���ʁE�d�S����̊������[�����g��Ԃ�
VectorXd SectionManager::massInertia(double dens, double l) {
    if (type == "circle") {
        return _CircleSection.massInertia(dens, l);
    }
    else if (type == "rectangle") {
        return _RectSection.massInertia(dens, l);
    }
    return VectorXd(1);

}

// �f�ʂ�\���������Ԃ�
string SectionManager::toString() {
    if (type == "circle") {
        return _CircleSection.toString();
    }
    else if (type == "rectangle") {
        return _RectSection.toString();
    }
    return "";
}