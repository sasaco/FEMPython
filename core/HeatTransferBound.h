#include "ElementBorderBound.h";

#include <string>
#include <vector>
using namespace std;         //  –¼‘O‹óŠÔŽw’è


class HeatTransferBound : public ElementBorderBound {

private:

public:
    int element;
    double htc;
    double outTemp;

    HeatTransferBound(int _element, string face, double _htc, double _outTemp);

    string toString(vecor<T> elems);

};