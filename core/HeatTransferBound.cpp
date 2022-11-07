//--------------------------------------------------------------------//
// �M�`�B���E����
// element - �v�f���x��
// face - �v�f���E��
// htc - �M�`�B��
// outTemp - �O�����x
var HeatTransferBound = function(element, face, htc, outTemp) {
    ElementBorderBound.call(this, element, face);
    this.htc = htc;
    this.outTemp = outTemp;
};

// �M�`�B���E������\���������Ԃ�
// elems - �v�f
HeatTransferBound.prototype.toString = function(elems) {
    return 'HTC\t' + elems[this.element].label.toString(10) + '\t' +
        this.face + '\t' + this.htc + '\t' + this.outTemp;
};