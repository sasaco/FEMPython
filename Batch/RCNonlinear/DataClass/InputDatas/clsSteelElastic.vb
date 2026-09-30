''' <summary>
''' 鉄筋の材料情報
''' </summary>
Public Class clsSteelElastic

    ''' <summary>材料強度fsk </summary>
    Public fsk As Double

    ''' <summary>材料係数γs </summary>
    Public rs As Double

    ''' <summary>ヤング率（弾性係数）</summary>
    Public Es As Double

    ''' <summary>材料修正係数</summary>
    Public rm As Double

    ''' <summary>引張鉄筋の中心間隔</summary>
    Public c As Double

    ''' <summary>帯鉄筋比</summary>
    Public pw As Double

    ''' <summary>帯鉄筋強度を考慮する係数</summary>
    Public kw As Double

    ''' <summary>材料番号</summary>
    Public ElasticID As String

    Sub New()
        fsk = 345
        rs = 1.0
        Es = 200
        rm = 1.0
        c = 100
        pw = 0
        kw = 0
        ElasticID = ""
    End Sub

    ''' <summary>引張鉄筋の降伏ひずみ </summary>
    Public ReadOnly Property εsyd As Double
        Get
            Return fsyd / Es / 1000
        End Get
    End Property

    Public ReadOnly Property fsyd As Double
        Get
            Return rm * fsk / rs
        End Get
    End Property

    Public ReadOnly Property σs(ByVal εs As Double) As Double
        Get
            Dim result As Double = 0
            If Math.Abs(εs) < εsyd Then
                result = Es * εs * 1000
            Else
                result = Math.Sign(εs) * fsyd
            End If
            Return result
        End Get
    End Property


End Class
