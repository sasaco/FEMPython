''' <summary>
''' Ｙ点の曲げモーメントＭy と 部材角φy を計算するモジュール
''' </summary>
Module CalcMyd
    Dim Limitter As Long

    ''' <summary>Ｙ点の曲げモーメントＭy と 部材角φy</summary>
    ''' <param name="Indata">入力データClass InputData</param>
    ''' <returns>結果データClass clsReactions</returns>
    Public Function φyd(ByVal Indata As InputData, CC As clsReaction) As clsReactionY
        Dim result As New clsReactionY
        Try
            '軸力が小さいときをチェックする。 
            If Indata.Nd < CalcStrength.MinNd(Indata) Then
                Return result '軸力が小さすぎ 解なし
            End If

            'My
            Dim σ = Myd(Indata)
            result.Mi = σ.Md
            result.x = σ.x

            'φy = εry／(d・x)
            result.fi = σ.fi
        Catch ex As Exception
            Throw ex
        End Try

        If result.Mi <= 0 Then Return result

        'θy
        If Double.IsNaN(Indata.La) = False Then
            '*** 変位量δy0 から回転角θyを求める。
            Try
                '計算精度で断面の分割数
                Const NS As Integer = NSVALUE2

                ''先端～Mc部(Lc区間)の変位量
                Dim Lc As Double = 0 ' Indata.La * CC.Mi / result.Mi
                'Dim θc = Lc * CC.Mi / (3 * Indata.Sc.Ec * Indata.Ie)
                'result.δy0 = θc * Lc

                'Mc部～基部(Ly区間)の変位量
                Dim Ly As Double = Indata.La - Lc
                If Ly > 0 Then
                    Dim h As Double = Ly / NS    '分割高さ
                    Dim f(0 To NS) As Double
                    Dim y(0 To NS) As Double
                    For i = 0 To NS
                        y(i) = h * i + Lc
                        Indata.Md = Math.Round(result.Mi * y(i) / Indata.La, 2)
                        Dim re
                        If i = 0 Then
                            re = New clsSigmaReaction
                        Else
                            re = CalcSigma.CalcReaction(Indata)
                        End If
                        f(i) = re.fi
                    Next
                    For i = 1 To NS
                        Dim dy As Double = y(i) - y(i - 1)
                        result.ly0 += (f(i) * y(i) + f(i - 1) * y(i - 1)) * dy / 2
                    Next
                    result.ri = result.ly0 / Indata.La
                End If
            Catch ex As Exception : Throw ex
            End Try

            '*** 方向鉄筋の部材接合部からの伸出しによる回転角θ1 を求める。
            Try
                Dim φ(0 To 2) As Double        '各段 引張鉄筋径
                Dim depth(0 To 2) As Double     '各段 引張鉄筋の位置
                Dim n As Integer = 0            '引張鉄筋段数
                For i = Indata.Steels.Count - 1 To 0 Step -1
                    If Indata.Steels(i).IsTensionBar = True Then
                        φ(n) = Indata.Steels(i).D
                        depth(n) = Indata.Steels(i).Depth
                        n += 1
                        If n >= 3 Then Exit For
                    End If
                Next
                Dim Cs As Double = Indata.Se.c    '引張鉄筋の中心間隔
                Dim Cs2 As Double = depth(0) - depth(1)     '引張鉄筋の段間隔
                '鉄筋間隔の影響を表す係数
                Dim α As Double = 0
                Select Case n
                    Case 3
                        '三段配筋以上の場合
                        α = 1 + 0.9 * Math.Exp(0.45 * (1 - Cs / φ(0))) _
                               + 0.6 * Math.Exp(0.45 * (1 - Cs2 / φ(1)))
                    Case 2
                        '二段配筋の場合
                        α = 1 + 0.9 * Math.Exp(0.45 * (1 - Cs / φ(0))) _
                              + 0.45 * Math.Exp(0.45 * (1 - Cs2 / φ(1)))
                    Case Else
                        '一段配筋の場合
                        α = 1 + 0.9 * Math.Exp(0.45 * (1 - Cs / φ(0)))
                End Select
                '引張鉄筋の降伏ひずみ
                Dim εsyd As Double = Indata.Se.εsyd
                '降伏時の部材接合部からの軸方向鉄筋の伸出し量(mm)
                Dim dLy As Double = 7.4 * α * εsyd * (6 + 3500 * εsyd) * φ(0) / (Indata.Sc.fck ^ (2 / 3))

                result.ry1 = dLy / (Indata.dt - result.x)
            Catch ex As Exception : Throw ex
            End Try
        End If

        Return result
    End Function

    ''' <summary>
    ''' 引張鉄筋が降伏するときの曲げモーメントとする。
    ''' なお、複数段の引張鉄筋の降伏は、複数段の図心位置のひずみが鉄筋の降伏ひずみに達した時点とする
    ''' </summary>
    Public Function Myd(ByVal Indata As InputData) As clsSigmaReaction

        Dim result As New clsSigmaReaction

        '*** 降伏着目位置の鉄筋応力度が降伏ひずみεsydになる曲げモーメントMyd を計算する。
        Try
            Indata.Nd = Math.Round(Indata.Nd, 2)
            Dim maxx As Double = Indata.dt
            Dim minx As Double = 0
            If xcheck(Indata, maxx, minx) Then
                result = xsearch(Indata, maxx, minx, 0)
            End If
        Catch ex As Exception
            Throw ex
        End Try
        Return result
    End Function

    ''' <summary>
    ''' 釣り合い計算
    ''' </summary>
    ''' <param name="Indata">入力データ</param>
    ''' <param name="maxx">中立軸の最大値</param>
    ''' <param name="minx">中立軸の最小値</param>
    ''' <returns>clsSigmaReaction</returns>
    Private Function xsearch(ByVal Indata As InputData, ByVal maxx As Double, ByVal minx As Double,
                             ByRef n As Long) As clsSigmaReaction

        '*** 降伏点
        Dim εsyd As Double = Indata.Se.εsyd
        '*** 降伏着目位置dt
        Dim dt As Double = Indata.dt
        '*** 中立軸
        Dim x As Double
        x = (maxx + minx) / 2
        '*** 降伏ひずみ から ひずみ角度 φ を設定する
        Dim f As Double = εsyd / (dt - x)

        '*** 釣り合い計算
        Dim result = CalcStrength.CalcStrength(Indata, x, f)

        '*** 釣り合い計算によって求められた軸力と設計軸力を比較して 中立軸xの仮定を調整する。
        If Indata.Nd < result.Nd Then
            n += 1
            If n > PublicConfig.LIMITERVALUE2 Then
                If Math.Abs(1 - Indata.Nd / result.Nd) < PublicConfig.LIMITERVALUE1 Then
                    'トライアル回数がリミットに達していても、ある程度答えが近ければ現状を結果として返す
                    Return result
                Else
                    '中立軸のトライアルに失敗しました。原因を確認してください。
                    Return Nothing
                End If
            End If
            result = xsearch(Indata, x, minx, n)
        ElseIf Indata.Nd > result.Nd Then
            n += 1
            If n > PublicConfig.LIMITERVALUE2 Then
                If Math.Abs(1 - Indata.Nd / result.Nd) < PublicConfig.LIMITERVALUE1 Then
                    'トライアル回数がリミットに達していても、ある程度答えが近ければ現状を結果として返す
                    Return result
                Else
                    '中立軸のトライアルに失敗しました。原因を確認してください。
                    Return Nothing
                End If
            End If
            result = xsearch(Indata, maxx, x, n)
        ElseIf Indata.Nd = result.Nd Then
            Return result
        End If
        Return result
    End Function

    Private Function xcheck(ByVal Indata As InputData, ByRef maxx As Double, ByRef minx As Double) As Boolean
        Try
            '*** 降伏点
            Dim εsyd As Double = Indata.Se.εsyd
            '*** 降伏着目位置dt
            Dim dt As Double = Indata.dt
            '*** 中立軸
            Dim x As Double
            '*** ひずみ角度
            Dim f As Double
            '*** 釣り合い計算結果
            Dim result As clsSigmaReaction

            If Indata.Nd > 0 Then
                '軸力が大きいときをチェックする。*** *** *** *** *** *** *** *** *** *** 
                x = maxx - LIMITERVALUE1
                f = εsyd / (dt - x)
                result = CalcStrength.CalcStrength(Indata, x, f)
                If Indata.Nd > result.Nd Then
                    Return False '軸力が大きすぎ 解なし
                End If
            Else
                '断面外に中立軸があるか確認する。
                For i As Integer = 1 To LIMITERVALUE2
                    x = minx
                    f = εsyd / (dt - x)
                    result = CalcStrength.CalcStrength(Indata, x, f)
                    If Indata.Nd < result.Nd Then
                        maxx = minx
                        minx = minx - (Indata.dt * i)
                    Else
                        Exit For
                    End If
                Next i
            End If
        Catch ex As Exception
            Throw ex
        End Try
        Return True
    End Function

End Module
