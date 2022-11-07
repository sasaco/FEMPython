//--------------------------------------------------------------------//
// �S������
// node - �ߓ_���x��
// coords - �Ǐ����W�n
// restx,resty,restz - x,y,z�����̍S���̗L��
// x,y,z - �����ψʂ�x,y,z����
// restrx,restry,restrz - x,y,z�����̉�]�S���̗L��
// rx,ry,rz - �����ψʂ�x,y,z�������]�p
var Restraint = function(node, coords, restx, resty, restz, x, y, z,
    restrx, restry, restrz, rx, ry, rz) {
    Vector3R.call(this, x, y, z, rx, ry, rz);
    this.node = node;
    this.coords = coords;
    this.rest = [restx, resty, restz, restrx, restry, restrz];
    this.globalX = this.x;
};

// �S��������\���������Ԃ�
// nodes - �ߓ_
Restraint.prototype.toString = function(nodes) {
    var s = 'Restraint\t' + nodes[this.node].label.toString(10);
    for (var i = 0; i < 6; i++) {
        if (this.rest[i]) {
            s += '\t1\t' + this.x[i];
        }
        else {
            s += '\t0\t' + this.x[i];
        }
    }
    if (this.coords) {
        s += '\t' + this.coords.label.toString(10);
    }
    return s;
};