Public Class InputData

    ''' <summary>設計曲げモーメント</summary>
    Public Md As Double
    ''' <summary>設計軸方向力</summary>
    Public Nd As Double
    ''' <summary>せん断スパン</summary>
    Public La As Double

    ''' <summary>ＲＣ断面要素</summary>
    Public Sections As List(Of clsSection)
    ''' <summary>ＲＣの材料</summary>
    Public SectionElastic As List(Of clsSectionElastic)
    Public Function GetSectionElastic(ByVal ID As String) As clsSectionElastic
        Dim result As clsSectionElastic = Nothing
        For Each e In SectionElastic
            If e.ElasticID = ID Then
                result = e
                Exit For
            End If
        Next
        Return result
    End Function

    ''' <summary>鋼材要素</summary>
    Public Steels As List(Of clsSteel)
    ''' <summary>鋼材の材料</summary>
    Public SteelElastic As List(Of clsSteelElastic)
    Public Function GetSteelElastic(ByVal ID As String) As clsSteelElastic
        Dim result As clsSteelElastic = Nothing
        For Each e In SteelElastic
            If e.ElasticID = ID Then
                result = e
                Exit For
            End If
        Next
        Return result
    End Function

    ''' <summary>デフォルト値の設定</summary>
    Sub New()
        Md = Double.NaN 'vbEmpty
        Nd = Double.NaN 'vbEmpty
        La = Double.NaN 'vbEmpty
        Sections = New List(Of clsSection)
        SectionElastic = New List(Of clsSectionElastic)
        Steels = New List(Of clsSteel)
        SteelElastic = New List(Of clsSteelElastic)
    End Sub


    ''' <summary>引張鉄筋重心位置</summary>
    Public ReadOnly Property dt As Double
        Get
            Dim yAs As Double = 0
            Dim sAs As Double = 0
            For Each r In Steels
                If r.IsTensionBar = True Then
                    sAs += r.Area
                    yAs += r.Area * r.Depth
                End If
            Next
            Return yAs / sAs
        End Get
    End Property

    ''' <summary>引張鉄筋量</summary>
    Public ReadOnly Property Ast As Double
        Get
            Dim result As Double = 0
            For i = Steels.Count - 1 To 0 Step -1
                If Steels(i).IsTensionBar = True Then
                    result += Steels(i).Area
                End If
            Next
            Return result
        End Get
    End Property

    ''' <summary>鉄筋被りを控除した有効断面積</summary>
    Public ReadOnly Property Ad As Double
        Get
            Dim sect2 As New List(Of clsSection)
            Dim d As Double = Me.dt '有効高さ
            Dim top As Double = 0
            Dim bottom As Double = 0
            For Each s1 As clsSection In Sections
                top = bottom            'Section の上辺の位置
                bottom += s1.Height     'Section の底辺の位置
                '*** 有効高さをまたぐ要素s1を分割する。
                If bottom >= d Then
                    Dim h As Double = d - top
                    Dim W As Double = s1.GetWidth(h)
                    sect2.Add(New clsSection(h, s1.WTop, W, s1.ElasticID))
                    Exit For
                Else
                    sect2.Add(s1)
                End If
            Next
            Dim Area As Double = 0  '有効断面積
            For Each s2 As clsSection In sect2
                Area += s2.GetArea
            Next
            Return Area
        End Get
    End Property

    ''' <summary>引張鉄筋比(%)</summary>
    Public ReadOnly Property pt As Double
        Get
            Return 100 * Ast / Ad
        End Get
    End Property

    ''' <summary>コンクリート断面高さ</summary>
    Public ReadOnly Property Hc As Double
        Get
            If SetModuluFlag = False Then SetModuluFlag = SetModulus()
            Return Me._Hc
        End Get
    End Property
    Private _Hc As Double = 0

    ''' <summary>コンクリートの全断面有効断面二次モーメント</summary>
    Public ReadOnly Property Ic As Double
        Get
            If SetModuluFlag = False Then SetModuluFlag = SetModulus()
            Return Me._Ic
        End Get
    End Property
    Private _Ic As Double = 0

    ''' <summary>コンクリートの全断面有効断面積</summary>
    Public ReadOnly Property Ac As Double
        Get
            If SetModuluFlag = False Then SetModuluFlag = SetModulus()
            Return Me._Ac
        End Get
    End Property
    Private _Ac As Double = 0

    ''' <summary>コンクリート断面の重心位置</summary>
    Public ReadOnly Property ye As Double
        Get
            If SetModuluFlag = False Then SetModuluFlag = SetModulus()
            Return Me._ye
        End Get
    End Property
    Private _ye As Double = 0

    ''' <summary>鉄筋の全断面積</summary>
    Public ReadOnly Property Ass As Double
        Get
            If SetModuluFlag = False Then SetModuluFlag = SetModulus()
            Return Me._Ass
        End Get
    End Property
    Private _Ass As Double = 0

    ''' <summary>コンクリートの換算断面二次モーメント</summary>
    Public ReadOnly Property Ie As Double
        Get
            If SetModuluFlag = False Then SetModuluFlag = SetModulus()
            Return Me._Ie
        End Get
    End Property
    Private _Ie As Double = 0

    ''' <summary>コンクリートの有効断面積</summary>
    Public ReadOnly Property Ae As Double
        Get
            If SetModuluFlag = False Then SetModuluFlag = SetModulus()
            Return Me._Ae
        End Get
    End Property
    Private _Ae As Double = 0

    ''' <summary>断面係数</summary>
    Public ReadOnly Property Z As Double
        Get
            If SetModuluFlag = False Then SetModuluFlag = SetModulus()
            Return Me._Z
        End Get
    End Property
    Private _Z As Double = 0

    ''' <summary>コンクリートの換算基準となる材料 Sc</summary>
    Public ReadOnly Property Sc As clsSectionElastic
        Get
            If SetModuluFlag = False Then SetModuluFlag = SetModulus()
            Return Me._Sc
        End Get
    End Property
    Private _Sc As clsSectionElastic = Nothing

    ''' <summary>鋼材の換算基準となる材料 Se</summary>
    Public ReadOnly Property Se As clsSteelElastic
        Get
            If SetModuluFlag = False Then SetModuluFlag = SetModulus()
            Return Me._Se
        End Get
    End Property
    Private _Se As clsSteelElastic = Nothing

    Private SetModuluFlag As Boolean = False
    ''' <summary>断面の基本情報を計算する</summary>
    ''' <returns>成功=true</returns>
    Private Function SetModulus() As Boolean

        _Sc = GetSectionElastic(Sections.First.ElasticID)   'コンクリートの換算基準となる材料 Sc
        _Se = GetSteelElastic(Steels.First.ElasticID)       '鋼材の換算基準となる材料 Se

        'コンクリート断面高さ_Hc 
        Try
            For Each s In Sections
                _Hc += s.Height
            Next
        Catch ex As Exception : Throw ex
        End Try

        'コンクリートの全断面有効断面積_Ac
        'コンクリート断面の重心位置_ye
        'コンクリートの全断面有効断面二次モーメント_Ic
        Try
            Dim Ay As Double = 0
            Dim depth As Double = 0
            With Sections
                Dim Ai(.Count) As Double
                Dim yi(.Count) As Double
                For i = 0 To .Count - 1
                    Dim s As clsSection = .Item(i)
                    Ai(i) = s.Height * (s.WTop + s.WBottom) / 2
                    Ai(i) = Ai(i) * GetSectionElastic(s.ElasticID).Ec / _Sc.Ec
                    yi(i) = depth + ((s.WTop + 2 * s.WBottom) / (s.WTop + s.WBottom)) * (s.Height / 3)
                    depth += s.Height
                    _Ac += Ai(i)
                    Ay += Ai(i) * yi(i)
                Next
                _ye = Ay / _Ac
                Dim tI As Double = 0
                Dim Ii As Double = 0
                Dim Ia As Double = 0
                For i = 0 To .Count - 1
                    Dim s As clsSection = .Item(i)
                    tI = (s.Height ^ 3) * ((s.WTop ^ 2) + (4 * s.WTop * s.WBottom) + (s.WBottom ^ 2)) / (36 * (s.WTop + s.WBottom))
                    tI = tI * GetSectionElastic(s.ElasticID).Ec / _Sc.Ec
                    Ii += tI
                    Ia += Ai(i) * (_ye - yi(i)) ^ 2
                Next
                _Ic = Ii + Ia
            End With
        Catch ex As Exception : Throw ex
        End Try

        '鉄筋の全断面積_Ass
        Try
            For Each r In Steels
                _Ass += r.Area
            Next
        Catch ex As Exception : Throw ex
        End Try

        'コンクリートの換算断面二次モーメント_Ie
        'コンクリートの有効断面積_Ae
        '断面係数_Z 
        Try
            Dim yc As Double = _ye      '圧縮縁から図芯までの高さ
            Dim ys As Double = _Hc - yc '引張縁から図芯までの高さ
            Dim tI As Double = 0
            Dim I As Double = 0
            For Each r In Steels
                tI = r.Area * (r.Depth - yc) ^ 2
                tI = tI * GetSteelElastic(r.ElasticID).Es / _Se.Es
                I += tI
            Next
            I = I * ((_Se.Es / _Sc.Ec) - 1)
            _Ie = _Ic + I
            _Ae = _Ac - _Ass
            _Z = _Ie / ys
        Catch ex As Exception : Throw ex
        End Try

        Return True
    End Function

End Class
