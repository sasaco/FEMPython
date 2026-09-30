''' <summary>
''' 台形要素
''' </summary>
Public Class clsSection

    ''' <summary>断面高さ</summary>
    Public Height As Double
    ''' <summary>断面幅（上辺）</summary>
    Public WTop As Double
    ''' <summary>断面幅（底辺）</summary>
    Public WBottom As Double
    ''' <summary>材料番号</summary>
    Public ElasticID As String

    Public Sub New()
        Height = 0
        WTop = 0
        WBottom = 0
        ElasticID = ""
    End Sub
    Public Sub New(ByVal _Height As Double, ByVal _WTop As Double, ByVal _WBottom As Double, ByVal _ElasticID As String)
        Height = _Height
        WTop = _WTop
        WBottom = _WBottom
        ElasticID = _ElasticID
    End Sub

    ''' <summary>
    ''' 台形の面積を求める
    ''' </summary>
    ''' <returns>台形の面積</returns>
    Public Function GetArea() As Double
        Return Height * (WTop + WBottom) / 2
    End Function

    ''' <summary>
    ''' 台形の中間点の幅を求める
    ''' </summary>
    ''' <param name="h">分割位置（上辺からの距離）</param>
    ''' <returns>分割位置の幅</returns>
    Public Function GetWidth(h As Double) As Double
        Return WTop + h * (WBottom - WTop) / Height
    End Function

    ''' <summary>
    ''' 台形の重心位置(上辺からの距離)を求める
    ''' </summary>
    ''' <returns>台形の重心位置</returns>
    Public Function GetYe() As Double
        Return ((WTop + (2 * WBottom)) / (WTop + WBottom)) * (Height / 3)
    End Function
End Class

