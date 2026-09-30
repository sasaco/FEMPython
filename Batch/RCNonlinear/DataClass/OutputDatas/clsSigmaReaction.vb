Public Class clsSigmaReaction

    ''' <summary>中立軸</summary>
    Public x As Double
    ''' <summary>ひずみの角度</summary>
    Public fi As Double
    ''' <summary>曲げモーメント</summary>
    Public Md As Double
    ''' <summary>軸方向力</summary>
    Public Nd As Double

    ''' <summary>
    ''' 各コンクリート分割点の応力度
    ''' key  : 深さ As Double
    ''' Value: 応力度 As Double
    ''' </summary>
    Public sc As New List(Of clsSigma)
    ''' <summary>
    ''' 各鉄筋の応力度
    ''' key  : 深さ As Double
    ''' Value: 応力度 As Double
    ''' </summary>
    Public st As New List(Of clsSigma)

End Class

Public Class clsSigma

    ''' <summary>応力度</summary>
    Public s As Double
    ''' <summary>位置</summary>
    Public Depth As Double

    Sub New()

    End Sub

    Sub New(_depth As Double, _s As Double)
        Depth = _depth
        s = _s
    End Sub

End Class