#include "SEBarElement.h"

SEBarElement::SEBarElement() : BarElement() {}
SEBarElement::SEBarElement(string label, vector<string> nodes, string material)
    : BarElement()
{

}

string SEBarElement::getName()
{
    return string();
}

MatrixXd SEBarElement::stiffBend(double l, Material material, Section sect)
{
    return MatrixXd();
}

MatrixXd SEBarElement::bendCurveShare(VectorXd v, double l, Material material, Section sect)
{
    return MatrixXd();
}
