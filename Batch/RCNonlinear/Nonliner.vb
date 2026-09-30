''' <summary>断面の非線形特性を計算するクラス</summary>
Public Class Nonliner

    ''' <summary>断面の非線形特性を計算する</summary>
    ''' <param name="strData">入力データ Xml形式</param>
    ''' <returns>計算結果データ Xml形式</returns>
    Public Function CalcNonlinearity(ByVal strData As String) As String
        Try
            '入力データ(Xml形式)を読み込む
            Dim InXmldata As New XmlReader(Of InputData)
            InXmldata.XmlData = strData
            Dim Indata As InputData = InXmldata.Data

            '計算
            Dim outData As New XmlReader(Of OutputData)
            outData.Data = Calcrate(Indata)

            '結果データを書きだす(Xml形式)
            Dim result = outData.XmlData
            Return result
        Catch ex As Exception
            Throw ex
        End Try
    End Function

    ''' <summary>断面の非線形特性を計算する</summary>
    Private Function Calcrate(ByVal Indata As InputData) As OutputData
        Dim result As New OutputData
        If Double.IsNaN(Indata.Md) Then
            '耐力計算
            Try
                If Double.IsNaN(Indata.Nd) Then
                    '軸力が指定されていない場合
                    '軸力変動を考慮する。
                    '以下４点間の軸力をNDCOUNT分割した設計軸力で計算する
                    Dim Ny = CalcStrength.MinNd(Indata) '①軸引張耐力Ny(軸力のみによって全鉄筋降伏する引張力)
                    Dim Nyd = Ny / 1.15 '照査下限値
                    Dim Ncr = CalcMcd.Ncr(Indata)       '②軸引張ひび割れNcr(軸力のみによってひび割れ発生する引張力)
                    Dim Nbd = CalcMmd.Nbd(Indata)       '③釣合い軸力Nb（引張鉄筋が降伏するのと同時にコンクリートの圧縮ひずみが終局に達する時の軸力)
                    Dim Nu = CalcStrength.MaxNd(Indata) '④軸圧縮耐力Nu(軸力のみによってコンクリートの圧縮ひずみが終局に達する時の圧縮力)
                    Dim Nud = Nu / 1.3  '照査上限値
   
                    Dim Nd() = SetIntermediateValueOfArray((NDCOUNT - 1), Nyd, Ncr, Nbd, Nud, 0, -Ny)
                    For i = 0 To NDCOUNT - 1
                        Indata.Nd = Math.Round(Nd(i), 2)
                        Dim re = Nonlinear(Indata)
                        result.Reactions.Add(re)
                    Next

                Else
                    '軸力固定値入力。
                    result.Reactions.Add(Nonlinear(Indata))
                End If
            Catch ex As Exception
                Throw ex
            End Try
        Else
            '応力度照査
            Try
                If Double.IsNaN(Indata.Nd) Then Indata.Nd = 0
                result.ResultSigma = CalcSigma.CalcReaction(Indata)
            Catch ex As Exception
                Throw ex
            End Try
        End If

        Return result
    End Function

    Private Function Nonlinear(ByVal Indata As InputData) As clsReactions
        Dim reaction As New clsReactions
        With reaction
            Try
                .Nd = Indata.Nd
                .C = CalcMcd.φcd(Indata)
                .Y = CalcMyd.φyd(Indata, .C)
                .M = CalcMmd.φmd(Indata, .C, .Y)
                .N = CalcMnd.φnd(Indata, .C, .Y, .M)
            Catch ex As Exception
                Throw ex
            End Try
        End With
        Return reaction
    End Function

    ''' <summary>
    ''' 指定の個数に分割した配列を作成する。
    ''' </summary>
    ''' <param name="Size">分割する数</param>
    ''' <param name="Values">分割する基準となる値</param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Function SetIntermediateValueOfArray(ByVal Size As Integer, ParamArray Values() As Double) As Double()


        Dim result(Size) As Double
        Try
            '最大値と最小値を探索し、分割幅の目安dnを求める
            Dim dn As Double '分割幅の目安
            Dim Ary() As Double = Values
            Dim ub As Integer = UBound(Ary, 1)
            Dim lb As Integer = LBound(Ary, 1)
            Array.Sort(Ary)
            dn = (Ary(ub) - Ary(lb)) / Size

            '最大値と最小値を探索し、分割幅の目安dnを求める
            Dim k As Integer = 0
            For i As Integer = lb To ub - 1
                Dim up As Integer = Ary(i + 1)
                Dim lo As Integer = Ary(i)
                Dim m As Integer = Math.Round((up - lo) / dn, 0)
                If m + k > Size Then
                    m = Size - k
                End If
                Dim dm As Double = (up - lo) / m
                For j As Integer = 0 To m
                    result(j + k) = lo + (dm * j)
                Next j
                k += m
            Next i

        Catch ex As Exception
            Throw ex
        End Try

        Return result
    End Function
End Class
