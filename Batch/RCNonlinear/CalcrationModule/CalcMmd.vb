''' <summary>
''' Ｍ点の曲げモーメントＭm と 部材角φm を計算するモジュール
''' </summary>
Module CalcMmd

    ''' <summary>Ｍ点の曲げモーメントＭm と 部材角φm</summary>
    ''' <param name="Indata">入力データClass InputData</param>
    ''' <returns>結果データClass clsReactions</returns>
    Public Function φmd(ByVal Indata As InputData, CC As clsReaction, YY As clsReactionY) As clsReactionM
        Dim result As New clsReactionM

        Try
            '軸力が大きいときをチェックする。*** *** *** *** *** *** *** *** *** *** 
            If Indata.Nd > CalcStrength.MaxNd(Indata) Then
                Return result '軸力が大きすぎ 解なし
            End If

            'Mm
            Dim σ = Mmd(Indata)
            result.Mi = σ.Md
            result.x = σ.x
            result.Nb = Nbd(Indata)

            'φm = θpm／Lp
            Dim Lp As Double = Indata.Hc    '塑性ヒンジ長さ
            Dim kw As Double = Indata.Se.kw
            Dim pw As Double = Indata.Se.pw
            result.rpm = Math.Min(0.021 * kw * pw + 0.013, 0.04) _
                                / Math.Max(0.79 * Indata.pt + 0.153, 0.78)
            result.fi = result.rpm / Lp

            'θm
            If Double.IsNaN(Indata.La) = False Then
                '*** 変位量δm0から回転角θmを求める。
                Try
                    '計算精度で断面の分割数
                    Const NS As Integer = NSVALUE2

                    '先端～Mc部(Lc区間)の変位量
                    Dim Lc As Double = 0 'Indata.La * CC.Mi / result.Mi
                    'Dim θc = Lc * CC.Mi / (3 * Indata.Sc.Ec * Indata.Ie)
                    'result.δm0 = θc * Lc

                    'Mc部～塑性ヒンジ部(Lm区間)の変位量
                    Dim Lm As Double = Indata.La - Lc - Lp
                    If Lm > 0 Then
                        Dim h As Double = Lm / NS    '分割高さ
                        Dim f(0 To NS) As Double
                        Dim y(0 To NS) As Double
                        For i = 0 To NS
                            y(i) = h * i + Lc
                            Dim tempIndata = Indata
                            tempIndata.Md = Math.Round(result.Mi * y(i) / Indata.La, 2)
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
                            result.lm0 += (f(i) * y(i) + f(i - 1) * y(i - 1)) * dy / 2
                        Next
                    End If
                    '塑性ヒンジ部(Lp区間)の変位量
                    result.lmp = result.rpm * (Indata.La - Lp / 2)

                    result.ri = result.lm0 / Indata.La
                Catch ex As Exception : Throw ex
                End Try

                '*** 方向鉄筋の部材接合部からの伸出しによる回転角θ1 を求める。
                Try
                    Dim N As Double = Indata.Nd

                    result.rm1 = (Math.Min(2.7 * kw * pw + 0.22, 3.7) * (1 - N / result.Nb) + 1) * YY.ry1
                Catch ex As Exception : Throw ex
                End Try

            End If
        Catch ex As Exception
            Throw ex
        End Try

        Return result
    End Function

    ''' <summary>
    ''' 釣り合い軸力(kN)で引張鉄筋が降伏すると同時にコンクリートの縁応力度が終局ひずみεcudになる
    ''' 時の軸方向力
    ''' </summary>
    Public Function Nbd(ByVal Indata As InputData) As Double

        Try
            '*** 断面の基本情報を計算する。
            Dim dt As Double = Indata.dt
            Dim εcud As Double = Indata.Sc.εcud
            Dim εsyd As Double = Indata.Se.εsyd

            '*** 中立軸 x
            Dim x As Double = dt * εcud / (εcud + εsyd)
            '*** ひずみ角度 φ
            Dim f As Double = εcud / x

            '*** 釣り合い計算
            Dim re = CalcStrength.CalcStrength(Indata, x, f)

            Return re.Nd
        Catch ex As Exception
            Throw ex
        End Try
    End Function

    ''' <summary>コンクリートの縁応力度が終局ひずみεcudになる曲げモーメントMmd を計算する。</summary>
    Public Function Mmd(ByVal Indata As InputData) As clsSigmaReaction
        Dim result As New clsSigmaReaction

        '*** コンクリートの縁応力度が終局ひずみεcudになる曲げモーメントMmd を計算する。
        Try
            '*** 釣り合い計算
            Indata.Nd = Math.Round(Indata.Nd, 2)
            Dim maxx As Double = Indata.Hc 'コンクリート断面高さ
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
    Private Function xsearch(ByVal Indata As InputData, ByVal maxx As Double, ByVal minx As Double, ByRef n As Long) As clsSigmaReaction

        '*** 終局ひずみ
        Dim εcud As Double = Indata.Sc.εcud
        '*** 中立軸
        Dim x As Double
        x = (maxx + minx) / 2
        '*** 終局ひずみ から ひずみ角度 φ を設定する
        Dim f As Double = εcud / x
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
            '*** 終局ひずみ
            Dim εcud As Double = Indata.Sc.εcud
            '*** 降伏着目位置dt
            Dim dt As Double = Indata.dt
            '*** 中立軸
            Dim x As Double
            '*** ひずみ角度
            Dim f As Double
            '*** 釣り合い計算結果
            Dim result As clsSigmaReaction

            If Indata.Nd < 0 Then
                '軸力が小さいときをチェックする。*** *** *** *** *** *** *** *** *** *** 
                x = minx + LIMITERVALUE1
                f = εcud / x
                result = CalcStrength.CalcStrength(Indata, x, f)
                If Indata.Nd < result.Nd Then
                    Throw New Exception("軸力が小さすぎます。終局耐力(Mud)の計算はできません。")
                End If

            Else
                '軸力が大きいときをチェックする。*** *** *** *** *** *** *** *** *** *** 
                If Indata.Nd > CalcStrength.MaxNd(Indata) Then
                    Throw New Exception("軸力が大きすぎます。終局耐力(Mud)の計算はできません。")
                End If
                '断面外に中立軸があるか確認する。
                For i As Integer = 1 To LIMITERVALUE2
                    x = maxx
                    f = εcud / x
                    result = CalcStrength.CalcStrength(Indata, x, f)
                    If Indata.Nd > result.Nd Then
                        minx = maxx
                        maxx = maxx + (Indata.Hc * i)
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

    Function Nu(Indata As InputData) As Double
        Throw New NotImplementedException
    End Function

End Module
