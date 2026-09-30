Public Class clsReactions

    ''' <summary>軸力</summary>
    Public Nd As Double
    ''' <summary>ひび割れ点</summary>
    Public C As clsReaction
    ''' <summary>鉄筋降伏点</summary>
    Public Y As clsReactionY
    ''' <summary>終局点</summary>
    Public M As clsReactionM
    ''' <summary>破断点</summary>
    Public N As clsReactionN

End Class

Public Class clsReaction
    ''' <summary>曲げ耐力</summary>
    Public Mi As Double
    ''' <summary>曲率</summary>
    Public fi As Double
    ''' <summary>回転角</summary>
    Public ri As Double
End Class

Public Class clsReactionY
    Inherits clsReaction
    ''' <summary>中立軸</summary>
    Public x As Double
    ''' <summary>軸方向鉄筋の部材接合部からの伸出しによる回転角</summary>
    Public ry1 As Double
    ''' <summary>く体変形による変位</summary>
    Public ly0 As Double
End Class

Public Class clsReactionM
    Inherits clsReaction
    ''' <summary>中立軸</summary>
    Public x As Double
    ''' <summary>軸方向鉄筋の部材接合部からの伸出しによる回転角</summary>
    Public rm1 As Double
    ''' <summary>塑性ヒンジ部の回転角</summary>
    Public rpm As Double
    ''' <summary>塑性ヒンジ部の変位</summary>
    Public lmp As Double
    ''' <summary>く体変形による変位</summary>
    Public lm0 As Double
    ''' <summary>釣り合い軸力</summary>
    Public Nb As Double
End Class

Public Class clsReactionN
    Inherits clsReaction
    ''' <summary>軸方向鉄筋の部材接合部からの伸出しによる回転角</summary>
    Public rn1 As Double
    ''' <summary>塑性ヒンジ部の回転角</summary>
    Public rpn As Double
    ''' <summary>塑性ヒンジ部の変位</summary>
    Public lnp As Double
    ''' <summary>く体変形による変位</summary>
    Public ln0 As Double
End Class




