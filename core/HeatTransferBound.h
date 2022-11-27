#include "ElementBorderBound.h";
#include "ElementManager.hpp"

#include <string>
#include <vector>
using namespace std;         //  名前空間指定
using std::vector;

class HeatTransferBound : public ElementBorderBound {

private:

public:
    int element;
    double htc;
    double outTemp;

    HeatTransferBound(int _element, string face, double _htc, double _outTemp);

    string toString(vector<ElementManager> elems);

};