''' <summary>
''' コンクリートの材料情報
''' </summary>
Public Class clsSectionElastic

    ''' <summary>材料強度fck </summary>
    Public fck As Double

    ''' <summary>材料係数γc </summary>
    Public rc As Double

    ''' <summary>ヤング率（弾性係数）</summary>
    Public Ec As Double

    ''' <summary>粗骨材の最大寸法</summary>
    Public dmax As Double

    ''' <summary>材料修正係数</summary>
    Public rm As Double

    ''' <summary>材料番号</summary>
    Public ElasticID As String

    ''' <summary>σcの算定方法</summary>
    Public σcMode As Integer


    Sub New()
        fck = 24
        rc = 1.0
        Ec = 25
        dmax = 25
        rm = 1.0
        ElasticID = ""
        σcMode = 1
    End Sub

    Public ReadOnly Property εcud As Double
        Get
            Return Math.Max(Math.Min((155 - fck) / 30000, 0.0035), 0.0025)
        End Get
    End Property

    Public ReadOnly Property kc As Double
        Get
            Return Math.Max(Math.Min((1 - 0.003 * fck), 0.85), 0)
        End Get
    End Property

    Public ReadOnly Property σc(ByVal εc As Double) As Double
        Get
            Dim result As Double = 0
            Select Case σcMode
                Case 0
                    'ヤング係数から求める
                    result = Ec * εc * 1000
                Case 1
                    '【鉄道標準】
                    Dim fcd As Double = fck / rc
                    Select Case εc
                        Case Is < 0
                            result = 0
                        Case Is < 0.002
                            result = kc * fcd * (εc / 0.002) * (2 - (εc / 0.002))
                        Case Else
                            result = kc * fcd
                    End Select
            End Select
            Return result

        End Get
    End Property

    Public ReadOnly Property ftk As Double
        Get
            Return 0.23 * ((fck * rm) ^ (2 / 3))
        End Get
    End Property
End Class
