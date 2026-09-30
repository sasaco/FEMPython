Module CalcStrength

    ''' <summary>
    ''' 釣り合い計算
    ''' </summary>
    ''' <param name="x">中立軸</param>
    ''' <param name="φ">ひずみ角度</param>
    ''' <returns>clsSigmaReaction</returns>
    Friend Function CalcStrength(ByVal Indata As InputData,
                                 ByVal x As Double,
                                 ByVal φ As Double) As clsSigmaReaction
        Dim result As New clsSigmaReaction
        Try
            '*** 断面を分割する
            Dim RC As List(Of clsAggregate) = GetRCAggregate(Indata, x)    'ＲＣ断面の分割
            Dim SS As List(Of clsAggregate) = GetSteelAggregate(Indata.Steels)  '鉄筋データ
            Dim ye As Double = Indata.ye    '曲げモーメントを求める位置（ＲＣ断面の重心位置）

            '*** 鉄筋
            Dim εs(0 To SS.Count - 1) As Double
            Dim ys(0 To SS.Count - 1) As Double
            For i = 0 To SS.Count - 1
                '*** 鉄筋各点のひずみ
                ys(i) = x - SS(i).Depth
                εs(i) = φ * ys(i)
                '*** 鉄筋部の応力度
                Dim ie = SS(i).ElasticID
                Dim se = Indata.GetSteelElastic(ie)
                Dim cs As Double = se.σs(εs(i))
                result.st.Add(New clsSigma(SS(i).Depth, cs))
                cs = cs * SS(i).Area
                result.Nd += cs
                result.Md += cs * (ye - SS(i).Depth) 'ys(i)
            Next

            '*** コンクリート
            Dim εc(0 To RC.Count - 1) As Double
            Dim yc(0 To RC.Count - 1) As Double
            For i = 0 To RC.Count - 1
                '*** コンクリート各点のひずみ
                yc(i) = x - RC(i).Depth
                εc(i) = φ * yc(i)
                '*** コンクリート部（圧縮側）の応力度
                Dim ie = RC(i).ElasticID
                Dim se = Indata.GetSectionElastic(ie)
                Dim cc As Double = se.σc(εc(i))
                result.sc.Add(New clsSigma(RC(i).Depth, cc))
                cc = cc * RC(i).Area
                result.Nd += cc
                result.Md += cc * (ye - RC(i).Depth) 'yc(i)
            Next

            result.Md = Math.Round(result.Md / 1000000, 2)
            result.Nd = Math.Round(result.Nd / 1000, 2)
            result.x = x
            result.fi = φ
            Return result
        Catch ex As Exception
            Throw ex
        End Try
    End Function

    ''' <summary>
    ''' ＲＣ断面を分割する
    ''' </summary>
    ''' <param name="Indata">入力データClass InputData</param>
    ''' <param name="x">中立軸までの距離で, ＲＣ断面の引張側はモデル化しない</param>
    ''' <returns>分割後のＲＣ断面の断面積(Area)と深さ(depth)リスト</returns>
    Private Function GetRCAggregate(ByVal Indata As InputData, x As Double) As List(Of clsAggregate)

        Dim result As New List(Of clsAggregate)
        If x > 0 Then
            '計算精度で断面の分割数
            Const NS As Integer = NSVALUE1
            '分割高さの目安
            Dim temp = x / NS
            Try
                Dim top As Double = 0
                Dim bottom As Double = 0
                For Each s1 As clsSection In Indata.Sections
                    top = bottom            'Section の上辺の位置
                    bottom += s1.Height     'Section の底辺の位置
                    '*** 中立軸をまたぐ要素s1を分割する。
                    If bottom >= x Then
                        Dim h As Double = x - top
                        Dim W As Double = s1.GetWidth(h)
                        s1 = New clsSection(h, s1.WTop, W, s1.ElasticID)
                        bottom = x
                    End If
                    '*** 要素s1をNS分割する。
                    Dim n As Integer = Math.Round(s1.Height / temp) '要素の分割数
                    Dim t As Double = s1.Height / n                 '分割高さ
                    For i = 1 To n
                        '*** 分割要素を計算
                        Dim tY As Double = t * (i - 1)              '分割要素の上辺の位置
                        Dim bY As Double = t * i                    '分割要素の下辺の位置
                        Dim WtNew As Double = s1.GetWidth(tY)       '分割要素の上辺の幅
                        Dim WbNew As Double = s1.GetWidth(bY)       '分割要素の下辺の幅
                        Dim s2 = New clsSection(t, WtNew, WbNew, s1.ElasticID)
                        '*** 分割要素を登録
                        Dim depth = top + tY + s2.GetYe             '分割要素の重心位置
                        Dim A As Double = s2.GetArea                '分割要素の面積
                        result.Add(New clsAggregate(depth, A, s1.ElasticID))
                    Next
                    If bottom = x Then Exit For
                Next
            Catch ex As Exception : Throw ex
            End Try
        End If
        Return result
    End Function

    ''' <summary>
    ''' 鋼材のリスト
    ''' </summary>
    ''' <returns>鋼材の断面積(Area)と深さ(depth)リスト</returns>
    Private Function GetSteelAggregate(ByVal Steels As List(Of clsSteel)) As List(Of clsAggregate)
        Dim result As New List(Of clsAggregate)
        Try
            For Each r In Steels
                result.Add(New clsAggregate(r.Depth, r.Area, r.ElasticID))
            Next
        Catch ex As Exception
            Throw ex
        End Try
        Return result
    End Function


    ''' <summary>
    ''' 最大軸圧縮力で、コンクリートのすべてが終局ひずみに達する軸力
    ''' </summary>
    ''' <returns>最大軸圧縮力 As Double</returns>
    ''' <remarks></remarks>
    Public Function MaxNd(ByVal Indata As InputData) As Double
        Dim result As Double = 0
        'コンクリートの終局ひずみ
        Dim cc As Double = Indata.Sc.σc(Indata.Sc.εcud) 'ひずみの最大値
        Dim Ac As Double = Indata.Ac - Indata.Ass
        result = Math.Round(cc * Ac / 1000, 2)
        '鉄筋の降伏ひずみ
        result -= MinNd(Indata)
        Return result
    End Function
    ''' <summary>
    ''' 最小軸引張力で、鉄筋のすべてが降伏ひずみに達する軸力
    ''' </summary>
    ''' <returns>最小軸引張力 As Double</returns>
    ''' <remarks></remarks>
    Public Function MinNd(ByVal Indata As InputData) As Double
        Dim result As Double = 0
        Dim SS As List(Of clsAggregate) = GetSteelAggregate(Indata.Steels)  '鉄筋データ
        '*** 鉄筋
        For i = 0 To SS.Count - 1
            '*** 鉄筋部の応力度
            Dim ie = SS(i).ElasticID
            Dim se = Indata.GetSteelElastic(ie)
            Dim cs As Double = se.σs(se.εsyd) 'ひずみの最大値
            cs = cs * SS(i).Area
            result -= cs
        Next
        result = Math.Round(result / 1000, 2)
        Return result
    End Function


End Module
