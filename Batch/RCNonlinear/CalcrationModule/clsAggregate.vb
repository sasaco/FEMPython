Public Class clsAggregate

    ''' <summary>深さ位置</summary>
    Public Depth As Double
    ''' <summary>断面積</summary>
    Public Area As Double
    ''' <summary>材料番号</summary>
    Public ElasticID As String

    Sub New()
    End Sub
    Sub New(ByVal _Depth As Double, ByVal _Area As Double, _ElasticID As String)
        Depth = _Depth
        Area = _Area
        ElasticID = _ElasticID
    End Sub

End Class

