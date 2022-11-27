//--------------------------------------------------------------------//
// ファイル読み込み・書き込み
#include "FemDataModel.h";

#include <string>
#include <vector>
using namespace std;
using std::vector;



/// <summary>
/// // FEMデータを読み込む
/// </summary>
/// <param name="s">データ文字列のリスト</param>
/// <returns></returns>
string readFemModel(vector<string> s)
{
    auto model = FemDataModel();
    model.clear();
    /*
    var mesh = model.mesh, bc = model.bc, res = [];
    for (var i = 0; i < s.length; i++) {
        var ss = s[i].trim().replace(/ \t / g, ' ').split(/ \s + / );
        if (ss.length > 0) {
            var keyWord = ss[0].toLowerCase();
            // 材料データ
            if ((keyWord == 'material') && (ss.length > 7)) {
                model.materials.push
                (new Material(parseInt(ss[1]), parseFloat(ss[2]),
                    parseFloat(ss[3]), parseFloat(ss[5]),
                    parseFloat(ss[6]), parseFloat(ss[7])));
            }
            // シェルパラメータ
            else if ((keyWord == 'shellparameter') && (ss.length > 2)) {
                model.shellParams.push
                (new ShellParameter(parseInt(ss[1]), parseFloat(ss[2])));
            }
            // 梁パラメータ
            else if ((keyWord == 'barparameter') && (ss.length > 4)) {
                model.barParams.push(new BarParameter
                (parseInt(ss[1]), ss[2], ss.slice(3, ss.length)));
            }
            // 局所座標系
            else if ((keyWord == 'coordinates') && (ss.length > 10)) {
                model.coordinates.push(readCoordinates(ss));
            }
            // 節点
            else if ((keyWord == 'node') && (ss.length > 4)) {
                mesh.nodes.push(new FENode(parseInt(ss[1]), parseFloat(ss[2]),
                    parseFloat(ss[3]),
                    parseFloat(ss[4])));
            }
            // 要素
            else if ((keyWord == 'bebarelement') && (ss.length > 5)) {
                if (ss.length < 8) {
                    mesh.elements.push(new BEBarElement
                    (parseInt(ss[1]), parseInt(ss[2]), parseInt(ss[3]),
                        readVertex(ss, 4, 2)));
                }
                else {
                    mesh.elements.push(new BEBarElement
                    (parseInt(ss[1]), parseInt(ss[2]), parseInt(ss[3]),
                        readVertex(ss, 4, 2),
                        new THREE.Vector3().set(parseFloat(ss[6]),
                            parseFloat(ss[7]),
                            parseFloat(ss[8]))));
                }
            }
            else if ((keyWord == 'tbarelement') && (ss.length > 5)) {
                if (ss.length < 8) {
                    mesh.elements.push(new TBarElement
                    (parseInt(ss[1]), parseInt(ss[2]), parseInt(ss[3]),
                        readVertex(ss, 4, 2)));
                }
                else {
                    mesh.elements.push(new TBarElement
                    (parseInt(ss[1]), parseInt(ss[2]), parseInt(ss[3]),
                        readVertex(ss, 4, 2),
                        new THREE.Vector3().set(parseFloat(ss[6]),
                            parseFloat(ss[7]),
                            parseFloat(ss[8]))));
                }
            }
            else if ((keyWord == 'trielement1') && (ss.length > 6)) {
                mesh.elements.push(new TriElement1
                (parseInt(ss[1]), parseInt(ss[2]), parseInt(ss[3]),
                    readVertex(ss, 4, 3)));
            }
            else if ((keyWord == 'quadelement1') && (ss.length > 7)) {
                mesh.elements.push(new QuadElement1
                (parseInt(ss[1]), parseInt(ss[2]), parseInt(ss[3]),
                    readVertex(ss, 4, 4)));
            }
            else if ((keyWord == 'tetraelement1') && (ss.length > 6)) {
                mesh.elements.push(new TetraElement1
                (parseInt(ss[1]), parseInt(ss[2]), readVertex(ss, 3, 4)));
            }
            else if ((keyWord == 'wedgeelement1') && (ss.length > 8)) {
                mesh.elements.push(new WedgeElement1
                (parseInt(ss[1]), parseInt(ss[2]), readVertex(ss, 3, 6)));
            }
            else if ((keyWord == 'hexaelement1') && (ss.length > 10)) {
                mesh.elements.push(new HexaElement1
                (parseInt(ss[1]), parseInt(ss[2]), readVertex(ss, 3, 8)));
            }
            else if ((keyWord == 'hexaelement1wt') && (ss.length > 10)) {
                mesh.elements.push(new HexaElement1WT
                (parseInt(ss[1]), parseInt(ss[2]), readVertex(ss, 3, 8)));
            }
            else if ((keyWord == 'tetraelement2') && (ss.length > 12)) {
                mesh.elements.push(new TetraElement2
                (parseInt(ss[1]), parseInt(ss[2]), readVertex(ss, 3, 10)));
            }
            else if ((keyWord == 'wedgeelement2') && (ss.length > 17)) {
                mesh.elements.push(new WedgeElement2
                (parseInt(ss[1]), parseInt(ss[2]), readVertex(ss, 3, 15)));
            }
            else if ((keyWord == 'hexaelement2') && (ss.length > 22)) {
                mesh.elements.push(new HexaElement2
                (parseInt(ss[1]), parseInt(ss[2]), readVertex(ss, 3, 20)));
            }
            // 境界条件
            else if ((keyWord == 'restraint') && (ss.length > 7)) {
                var rest = readRestraint(ss);
                if (rest != = null) bc.restraints.push(rest);
            }
            else if ((keyWord == 'load') && (ss.length > 4)) {
                bc.loads.push(readLoad(ss));
            }
            else if ((keyWord == 'pressure') && (ss.length > 3)) {
                bc.pressures.push
                (new Pressure(parseInt(ss[1]), ss[2].toUpperCase(),
                    parseFloat(ss[3])));
            }
            else if ((keyWord == 'temperature') && (ss.length > 2)) {
                bc.temperature.push
                (new Temperature(parseInt(ss[1]), parseFloat(ss[2])));
            }
            else if ((keyWord == 'htc') && (ss.length > 4)) {
                bc.htcs.push
                (new HeatTransferBound(parseInt(ss[1]), ss[2].toUpperCase(),
                    parseFloat(ss[3]), parseFloat(ss[4])));
            }
            // 計算結果
            else if ((keyWord == 'resulttype') && (ss.length > 1)) {
                if (ss[1].toLowerCase() == 'element') {
                    model.result.type = ELEMENT_DATA;
                }
                else {
                    model.result.type = NODE_DATA;
                }
            }
            else if (((keyWord == 'eigenvalue') && (ss.length > 2)) ||
                ((keyWord == 'displacement') && (ss.length > 7)) ||
                ((keyWord == 'strain1') && (ss.length > 7)) ||
                ((keyWord == 'stress1') && (ss.length > 7)) ||
                ((keyWord == 'strenergy1') && (ss.length > 2)) ||
                ((keyWord == 'strain2') && (ss.length > 7)) ||
                ((keyWord == 'stress2') && (ss.length > 7)) ||
                ((keyWord == 'strenergy2') && (ss.length > 2)) ||
                ((keyWord == 'temp') && (ss.length > 2))) {
                res.push(ss);
            }
        }
    }
    model.init();
    initObject();
    if (res.length > 0) {
        readFemResult(res);
    }
    */

    return "OK";
}


