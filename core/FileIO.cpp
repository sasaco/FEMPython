//--------------------------------------------------------------------//
// ファイル読み込み・書き込み
#include "FemDataModel.h"

#include <string>
#include <vector>
using namespace std;
using std::vector;



const string WHITESPACE = " \n\r\t\f\v";

string ltrim(const string& s)
{
    size_t start = s.find_first_not_of(WHITESPACE);
    return (start == string::npos) ? "" : s.substr(start);
}

string rtrim(const string& s)
{
    size_t end = s.find_last_not_of(WHITESPACE);
    return (end == string::npos) ? "" : s.substr(0, end + 1);
}

string trim(const string& s) {
    return rtrim(ltrim(s));
}


vector<string> split(string str, char del) {

    vector<string> result;
    stringstream ss{ str };
    string buf;
    while (getline(ss, buf, del)) {
        buf = trim(buf);
        if (buf.size() > 0) {
            result.push_back(buf);
        }
    }

    return result;
}


/// <summary>
/// // FEMデータを読み込む
/// </summary>
/// <param name="s">データ文字列のリスト</param>
/// <returns></returns>
FemDataModel readFemModel(string s)
{
    vector<string> lines = split(s, '\n');

    auto model = FemDataModel();
    model.clear();
    
    MeshModel mesh = model.mesh;
    BoundaryCondition bc = model.bc;

    vector<string> res;

    for (int i = 0; i < lines.size(); i++) {
        
        vector<string> columns = split(lines[i], ' ');

        if (columns.size() > 0) {

            string keyWord = columns[0];
            std::transform(keyWord.begin(), keyWord.end(), keyWord.begin(), ::tolower);

            // 材料データ
            if ((keyWord == "material") && (columns.size() > 7)) {
                auto mat = Material(stoi(columns[1]), stod(columns[2]),
                    stod(columns[3]), stod(columns[5]),
                    stod(columns[6]), stod(columns[7]));
                model.materials.push_back(mat);
            }
            /*
            // シェルパラメータ
            else if ((keyWord == "shellparameter") && (columns.size() > 2)) {
                model.shellParams.push
                (new ShellParameter(stoi(columns[1]), stod(columns[2])));
            }
            // 梁パラメータ
            else if ((keyWord == "barparameter") && (columns.size() > 4)) {
                model.barParams.push_back(new BarParameter
                (stoi(columns[1]), columns[2], ss.slice(3, columns.size())));
            }
            // 局所座標系
            else if ((keyWord == "coordinates") && (columns.size() > 10)) {
                model.coordinates.push_back(readCoordinates(ss));
            }
            */
            // 節点
            else if ((keyWord == "node") && (columns.size() > 4)) {
                auto node = FENode(stoi(columns[1]), stod(columns[2]),
                    stod(columns[3]),
                    stod(columns[4]));
                mesh.nodes.push_back(node);
            }
            // 要素
            /*
            else if ((keyWord == "bebarelement") && (columns.size() > 5)) {
                if (columns.size() < 8) {
                    mesh.elements.push_back(new BEBarElement
                    (stoi(columns[1]), stoi(columns[2]), stoi(columns[3]),
                        readVertex(ss, 4, 2)));
                }
                else {
                    mesh.elements.push_back(new BEBarElement
                    (stoi(columns[1]), stoi(columns[2]), stoi(columns[3]),
                        readVertex(ss, 4, 2),
                        new THREE.Vector3().set(stod(columns[6]),
                            stod(columns[7]),
                            stod(columns[8]))));
                }
            }
            else if ((keyWord == "tbarelement") && (columns.size() > 5)) {
                if (columns.size() < 8) {
                    mesh.elements.push_back(new TBarElement
                    (stoi(columns[1]), stoi(columns[2]), stoi(columns[3]),
                        readVertex(ss, 4, 2)));
                }
                else {
                    mesh.elements.push_back(new TBarElement
                    (stoi(columns[1]), stoi(columns[2]), stoi(columns[3]),
                        readVertex(ss, 4, 2),
                        new THREE.Vector3().set(stod(columns[6]),
                            stod(columns[7]),
                            stod(columns[8]))));
                }
            }
            else if ((keyWord == "trielement1") && (columns.size() > 6)) {
                mesh.elements.push_back(new TriElement1
                (stoi(columns[1]), stoi(columns[2]), stoi(columns[3]),
                    readVertex(ss, 4, 3)));
            }
            else if ((keyWord == "quadelement1") && (columns.size() > 7)) {
                mesh.elements.push_back(new QuadElement1
                (stoi(columns[1]), stoi(columns[2]), stoi(columns[3]),
                    readVertex(ss, 4, 4)));
            }
            else if ((keyWord == "tetraelement1") && (columns.size() > 6)) {
                mesh.elements.push_back(new TetraElement1
                (stoi(columns[1]), stoi(columns[2]), readVertex(ss, 3, 4)));
            }
            else if ((keyWord == "wedgeelement1") && (columns.size() > 8)) {
                mesh.elements.push_back(new WedgeElement1
                (stoi(columns[1]), stoi(columns[2]), readVertex(ss, 3, 6)));
            }
            */
            else if ((keyWord == "hexaelement1") && (columns.size() > 10)) {
                 auto elem = ElementManager("HexaElement1", columns);
                 mesh.elements.push_back(elem);
            }
            /*
            else if ((keyWord == "hexaelement1wt") && (columns.size() > 10)) {
                mesh.elements.push_back(new HexaElement1WT
                (stoi(columns[1]), stoi(columns[2]), readVertex(ss, 3, 8)));
            }
            else if ((keyWord == "tetraelement2") && (columns.size() > 12)) {
                mesh.elements.push_back(new TetraElement2
                (stoi(columns[1]), stoi(columns[2]), readVertex(ss, 3, 10)));
            }
            else if ((keyWord == "wedgeelement2") && (columns.size() > 17)) {
                mesh.elements.push_back(new WedgeElement2
                (stoi(columns[1]), stoi(columns[2]), readVertex(ss, 3, 15)));
            }
            else if ((keyWord == "hexaelement2") && (columns.size() > 22)) {
                mesh.elements.push_back(new HexaElement2
                (stoi(columns[1]), stoi(columns[2]), readVertex(ss, 3, 20)));
            }
            */
            // 境界条件
            else if ((keyWord == "restraint") && (columns.size() > 7)) {
                Restraint rest = Restraint(columns);
                bc.restraints.push_back(rest);
            }
            else if ((keyWord == "load") && (columns.size() > 4)) {
                Load load = Load(columns);
                bc.loads.push_back(load);
            }
            /*
            else if ((keyWord == "pressure") && (columns.size() > 3)) {
                bc.pressures.push
                (new Pressure(stoi(columns[1]), columns[2].toUpperCase(),
                    stod(columns[3])));
            }
            else if ((keyWord == "temperature") && (columns.size() > 2)) {
                bc.temperature.push
                (new Temperature(stoi(columns[1]), stod(columns[2])));
            }
            else if ((keyWord == "htc") && (columns.size() > 4)) {
                bc.htcs.push
                (new HeatTransferBound(stoi(columns[1]), columns[2].toUpperCase(),
                    stod(columns[3]), stod(columns[4])));
            }
            // 計算結果
            else if ((keyWord == "resulttype") && (columns.size() > 1)) {
                if (columns[1].toLowerCase() == "element") {
                    model.result.type = ELEMENT_DATA;
                }
                else {
                    model.result.type = NODE_DATA;
                }
            }
            else if (((keyWord == "eigenvalue") && (columns.size() > 2)) ||
                ((keyWord == "displacement") && (columns.size() > 7)) ||
                ((keyWord == "strain1") && (columns.size() > 7)) ||
                ((keyWord == "stress1") && (columns.size() > 7)) ||
                ((keyWord == "strenergy1") && (columns.size() > 2)) ||
                ((keyWord == "strain2") && (columns.size() > 7)) ||
                ((keyWord == "stress2") && (columns.size() > 7)) ||
                ((keyWord == "strenergy2") && (columns.size() > 2)) ||
                ((keyWord == "temp") && (columns.size() > 2))) {
                res.push_back(ss);
            }
            */
        }
    }
    model.init();

    /*
    initObject();
    if (res.length > 0) {
        readFemResult(res);
    } else {
    */
    // model.calculate();
    // }
    

    return model;
}

