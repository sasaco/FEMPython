//--------------------------------------------------------------------//
// �ʈ�����
// element - �v�f���x��
// face - �v�f���E��
// press - �ʈ�
var Pressure = function(element, face, press) {
    ElementBorderBound.call(this, element, face);
    this.press = press;
};

// �ʈ�������\���������Ԃ�
// elems - �v�f
Pressure.prototype.toString = function(elems) {
    return 'Pressure\t' + elems[this.element].label.toString(10) + '\t' +
        this.face + '\t' + this.press;
};