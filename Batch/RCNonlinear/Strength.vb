''' <summary>断面の耐力を計算するクラス</summary>
Public Class Strength

    Public Function Mud(b As Single, h As Single, Ass As List(Of String), n As List(Of Single), dt As List(Of Single), fck As Single, _
                        fsy As Single, Ec As Single, Es As Single, Nd As Single, γc As Single, γs As Single) As Single

        Dim result As clsSigmaReaction
        Try
            Dim inputData As InputData = Me.Init(b, h, Ass, n, dt, fck, fsy, Ec, Es, Nd, γc, γs)
            If inputData Is Nothing Then
                Return -1
            End If
            result = CalcMmd.Mmd(inputData)
            If result Is Nothing Then
                Return -1
            End If
        Catch ex As Exception
                Return -1
        End Try

        Return result.Md
    End Function

    Public Function Mud_T(b As Single, h As Single, bf As Double, hf As Double, Ass As List(Of String), n As List(Of Single), _
                        dt As List(Of Single), fck As Single, fsy As Single, Ec As Single, Es As Single, Nd As Single, _
                        γc As Single, γs As Single) As Single

        Dim result As clsSigmaReaction
        Try
            Dim inputData As InputData = Me.Init_T(b, h, CSng(bf), CSng(hf), Ass, n, _
                dt, fck, fsy, Ec, Es, Nd, _
                γc, γs)
            If inputData Is Nothing Then
                Return -1
            End If
            result = CalcMmd.Mmd(inputData)
            If result Is Nothing Then
                Return -1
            End If
        Catch ex As Exception
                Return -1
        End Try
        Return result.Md
    End Function

    ''' <summary>
    ''' 円形の終局耐力の計算
    ''' </summary>
    ''' <param name="R">外形(直径)</param>
    ''' <param name="Ass">鉄筋１本辺りの断面積</param>
    ''' <param name="n">鉄筋本数</param>
    ''' <param name="dt">鉄筋かぶり</param>
    ''' <param name="fck">材料係数を考慮したコンクリート強度</param>
    ''' <param name="fsy">材料係数を考慮した鉄筋強度</param>
    ''' <param name="Ec">各種係数を考慮したコンクリートの弾性係数</param>
    ''' <param name="Es">各種係数を考慮した鉄筋の弾性係数</param>
    ''' <param name="Nd">軸圧縮力</param>
    ''' <returns>エラーの場合 -1 を返す</returns>
    ''' <remarks></remarks>
    Public Function Mud_R(R As Single, Ass As List(Of String), n As List(Of Single), dt As List(Of Single), fck As Single, fsy As Single, _
                          Ec As Single, Es As Single, Nd As Single, γc As Single, γs As Single) As Single

        Dim result As clsSigmaReaction
        Try
            Dim inputData As InputData = Me.Init_R(R, Ass, n, dt, fck, fsy, Ec, Es, Nd, γc, γs)
            If inputData Is Nothing Then
                Return -1
            End If
            result = CalcMmd.Mmd(inputData)
            If result Is Nothing Then
                Return -1
            End If
        Catch ex As Exception
            Return -1
        End Try
        Return result.Md

    End Function

    Public Function Myd_R(R As Single, Ass As List(Of String), n As List(Of Single), dt As List(Of Single), fck As Single, fsy As Single, _
                          Ec As Single, Es As Single, Nd As Single, γc As Single, γs As Single) As Single
        Dim result As clsSigmaReaction
        Try
            Dim inputData As InputData = Me.Init_R(R, Ass, n, dt, fck, fsy, Ec, Es, Nd, γc, γs)
            If inputData Is Nothing Then
                Return -1
            End If
            result = CalcMyd.Myd(inputData)
            If result Is Nothing Then
                Return -1
            End If
        Catch ex As Exception
                Return -1
        End Try
        Return result.Md
    End Function

    Private Function Init(b As Single, h As Single, Ass As List(Of String), n As List(Of Single), dt As List(Of Single), fck As Single, _
                          fsy As Single, Ec As Single, Es As Single, Nd As Single, γc As Single, γs As Single) As InputData


        Dim result As New InputData()
        Try
            Dim listSection As New List(Of clsSection)()
            Dim listSectionElastic As New List(Of clsSectionElastic)()
            Dim listSteel As New List(Of clsSteel)()
            Dim listSteelElastic As New List(Of clsSteelElastic)()
            Try
                Dim Section As New clsSection()
                Section.Height = CDbl(h)
                Section.WTop = CDbl(b)
                Section.WBottom = CDbl(b)
                Section.ElasticID = "c"
                listSection.Add(Section)
            Catch ex As Exception
                Return Nothing
            End Try
            Try
                Dim SectionElastic As New clsSectionElastic()
                SectionElastic.fck = CDbl(fck)
                SectionElastic.rc = CDbl(γc)
                SectionElastic.Ec = CDbl(Ec)
                SectionElastic.dmax = 0.0
                SectionElastic.rm = 1.0
                SectionElastic.ElasticID = "c"
                listSectionElastic.Add(SectionElastic)
            Catch ex As Exception
                Return Nothing
            End Try

            Try
                Dim num As Integer = Ass.Count - 1
                For i As Integer = 0 To num
                    Dim Steel As New clsSteel()
                    Steel.Depth = CDbl(dt(i))
                    Steel.i = Ass(i)
                    Steel.n = CDbl(n(i))
                    Steel.ElasticID = "s"
                    listSteel.Add(Steel)
                Next
            Catch ex As Exception
                Return Nothing
            End Try
            Try
                Dim SteelElastic As New clsSteelElastic()
                SteelElastic.fsk = CDbl(fsy)
                SteelElastic.rs = CDbl(γs)
                SteelElastic.Es = CDbl(Es)
                SteelElastic.rm = 1.0
                SteelElastic.c = 0.0
                SteelElastic.pw = 0.0
                SteelElastic.kw = 0.0
                SteelElastic.ElasticID = "s"
                listSteelElastic.Add(SteelElastic)
            Catch ex As Exception
                Return Nothing
            End Try
            Try
                result.Nd = CDbl(Nd)
                result.La = 0.0
                result.Sections = listSection
                result.SectionElastic = listSectionElastic
                result.Steels = listSteel
                result.SteelElastic = listSteelElastic
            Catch ex As Exception
                Return Nothing
            End Try
        Catch ex As Exception
                Return Nothing
        End Try
        Return result

    End Function

    Private Function Init_T(b As Single, h As Single, bf As Single, hf As Single, Ass As List(Of String), n As List(Of Single), _
                            dt As List(Of Single), fck As Single, fsy As Single, Ec As Single, Es As Single, Nd As Single, _
                            γc As Single, γs As Single) As InputData

        Dim result As New InputData
        Try
            Dim listSection As New List(Of clsSection)
            Dim listSectionElastic As New List(Of clsSectionElastic)
            Dim listSteel As New List(Of clsSteel)
            Dim listSteelElastic As New List(Of clsSteelElastic)
            Try
                Dim Section1 As New clsSection
                Section1.Height = CDbl(hf)
                Section1.WTop = CDbl(bf)
                Section1.WBottom = CDbl(bf)
                Section1.ElasticID = "c"
                listSection.Add(Section1)
                Dim Section2 As New clsSection
                Section2.Height = CDbl(h - hf)
                Section2.WTop = CDbl(b)
                Section2.WBottom = CDbl(b)
                Section2.ElasticID = "c"
                listSection.Add(Section2)
            Catch ex As Exception
                Return Nothing
            End Try
            Try
                Dim SectionElastic As New clsSectionElastic
                SectionElastic.fck = CDbl(fck)
                SectionElastic.rc = CDbl(γc)
                SectionElastic.Ec = CDbl(Ec)
                SectionElastic.dmax = 0.0
                SectionElastic.rm = 1.0
                SectionElastic.ElasticID = "c"
                listSectionElastic.Add(SectionElastic)
            Catch ex As Exception
                Return Nothing
            End Try
            Try
                Dim num As Integer = Ass.Count - 1
                For i As Integer = 0 To num
                    Dim Steel As New clsSteel
                    Steel.Depth = CDbl(dt(i))
                    Steel.i = Ass(i)
                    Steel.n = CDbl(n(i))
                    Steel.ElasticID = "s"
                    listSteel.Add(Steel)
                Next
            Catch ex As Exception
                Return Nothing
            End Try
            Try
                Dim SteelElastic As New clsSteelElastic
                SteelElastic.fsk = CDbl(fsy)
                SteelElastic.rs = CDbl(γs)
                SteelElastic.Es = CDbl(Es)
                SteelElastic.rm = 1.0
                SteelElastic.c = 0.0
                SteelElastic.pw = 0.0
                SteelElastic.kw = 0.0
                SteelElastic.ElasticID = "s"
                listSteelElastic.Add(SteelElastic)
            Catch ex As Exception
                Return Nothing
            End Try
            Try
                result.Nd = CDbl(Nd)
                result.La = 0.0
                result.Sections = listSection
                result.SectionElastic = listSectionElastic
                result.Steels = listSteel
                result.SteelElastic = listSteelElastic
            Catch ex As Exception
                Return Nothing
            End Try

        Catch ex As Exception
                Return Nothing
        End Try
        Return result
    End Function

    Private Function Init_R(R As Single, Ass As List(Of String), n As List(Of Single), dt As List(Of Single), _
                           fck As Single, fsy As Single, Ec As Single, Es As Single, Nd As Single, γc As Single, γs As Single) As InputData

        Dim Indata As New InputData
        Try
            Dim listSection As New List(Of clsSection)
            Dim listSectionElastic As New List(Of clsSectionElastic)
            Dim listSteel As New List(Of clsSteel)
            Dim listSteelElastic As New List(Of clsSteelElastic)

            'RC断面要素(Sections As List(Of clsSection))の設定 --------------------------
            Try
                Dim olddeg As Single = 0
                Dim steps = 180 / PublicConfig.RCOUNT
                For deg As Single = steps To 180 Step steps
                    Dim Sect As New clsSection
                    With Sect
                        .Height = (Math.Cos(Radians(olddeg)) - Math.Cos(Radians(deg))) * R / 2  '断面高さ
                        .WTop = Math.Sin(Radians(olddeg)) * R   '断面幅（上辺）
                        .WBottom = Math.Sin(Radians(deg)) * R   '断面幅（底辺
                        .ElasticID = "c"                        '材料番号
                    End With
                    listSection.Add(Sect)
                    olddeg = deg
                Next
            Catch ex As Exception
                Return Nothing
            End Try

            'RCの材料(SectionElastic As List(Of clsSectionElastic))の設定 --------------------------
            Try
                Dim SectElastic As New clsSectionElastic
                With SectElastic
                    .fck = fck          '材料強度fck
                    .rc = γc            '材料係数
                    .Ec = Ec            'ヤング率（弾性係数）
                    .dmax = 0           '粗骨材の最大寸法
                    .rm = 1             '材料修正係数
                    .ElasticID = "c"    '材料番号
                End With
                listSectionElastic.Add(SectElastic)
            Catch ex As Exception
                Return Nothing
            End Try

            '鉄筋要素(Steels As List(Of clsSteel)))の設定 --------------------------
            Try
                For i段数 = 0 To Ass.Count - 1

                    Dim Rt As Single = R - (dt(i段数) * 2)  '鉄筋直径
                    Dim steps As Single = 360 / n(i段数)    '鉄筋角度間隔

                    For deg As Single = 0 To 360 - steps Step steps
                        Dim St As New clsSteel
                        With St
                            .Depth = (Rt / 2) - (Math.Cos(Radians(deg)) * Rt / 2) + dt(i段数)  '深さ位置
                            .i = Ass(i段数)                     '鋼材
                            .n = 1                              '鋼材の本数
                            If deg >= 135 And deg <= 225 Then   '鋼材の引張降伏着目Flag
                                .IsTensionBar = True
                            Else
                                .IsTensionBar = False
                            End If
                            .ElasticID = "s"            '材料番号
                        End With
                        listSteel.Add(St)
                    Next
                Next
            Catch ex As Exception
                Return Nothing
            End Try

            '鋼材の材料(SteelElastic As List(Of clsSteelElastic))の設定 --------------------------
            Try
                Dim StElastic As New clsSteelElastic
                With StElastic
                    .fsk = fsy          '材料強度fsk
                    .rs = γs           '材料係数γs
                    .Es = Es            'ヤング率（弾性係数）
                    .rm = 1             '材料修正係数
                    .c = 0              '引張鉄筋の中心間隔
                    .pw = 0             '帯鉄筋比
                    .kw = 0             '帯鉄筋強度を考慮する係数
                    .ElasticID = "s"    '材料番号
                End With
                listSteelElastic.Add(StElastic)
            Catch ex As Exception
                Return Nothing
            End Try
            Try
                With Indata
                    .Nd = Nd
                    .La = 0
                    .Sections = listSection
                    .SectionElastic = listSectionElastic
                    .Steels = listSteel
                    .SteelElastic = listSteelElastic
                End With
            Catch ex As Exception
                Return Nothing
            End Try
        Catch ex As Exception
            Return Nothing
        End Try
        Return Indata
    End Function

    ''' <summary>
    ''' 弧度法 を ラジアンに変換
    ''' </summary>
    ''' <param name="Degrees">角度(°)</param>
    ''' <returns>ラジアン</returns>
    ''' <remarks></remarks>
    Private Function Radians(Degrees As Single) As Single
        Return Degrees * Math.PI / 180
    End Function


    ''' <summary>
    ''' 鋼材断面積を返す
    ''' </summary>
    ''' <param name="Dia">
    ''' 鋼材名称 
    ''' 　異形鉄筋 "D32" 
    ''' 　丸鋼 "R29" 
    ''' 　断面積任意指定 "794.2"
    ''' </param>
    ''' <returns>鋼材断面積</returns>
    ''' <remarks></remarks>
    Public Function Ass(Dia As String) As Double
        Dim SteelInfo As New clsSteel
        SteelInfo.i = Dia
        SteelInfo.n = 1
        Return SteelInfo.Area
    End Function

End Class

