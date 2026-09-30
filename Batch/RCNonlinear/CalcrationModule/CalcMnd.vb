''' <summary>
''' Ｎ点の曲げモーメントＭn と 部材角φn を計算するモジュール
''' </summary>
Module CalcMnd

    ''' <summary>Ｎ点の曲げモーメントＭn と 部材角φn</summary>
    ''' <param name="Indata">入力データClass InputData</param>
    ''' <returns>結果データClass clsReactions</returns>
    Public Function φnd(ByVal Indata As InputData, CC As clsReaction, YY As clsReactionY, MM As clsReactionM) As clsReactionN
        Dim result As New clsReactionN

        Try
            'Mn
            If YY.Mi <= 0 Then
                result.Mi = MM.Mi
            Else
                result.Mi = YY.Mi
            End If

            'φn = θpn／Lp
            Dim Lp As Double = Indata.Hc    '塑性ヒンジ長さ
            Dim Kp As Double = -0.1 / MM.Mi
            Dim dθp As Double = Kp * (YY.Mi - MM.Mi)
            result.rpn = MM.rpm + dθp
            result.fi = result.rpn / Lp

            'θn
            If Double.IsNaN(Indata.La) = False Then
                Try
                    '計算精度で断面の分割数
                    Const NS As Integer = NSVALUE2

                    '先端～Mc部(Lc区間)の変位量
                    Dim Lc As Double = 0 ' Indata.La * CC.Mi / result.Mi
                    'Dim θc = Lc * CC.Mi / (3 * Indata.Sc.Ec * Indata.Ie)
                    'result.ln0 = θc * Lc

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
                            result.ln0 += (f(i) * y(i) + f(i - 1) * y(i - 1)) * dy / 2
                        Next
                    End If
                    '塑性ヒンジ部(Lp区間)の変位量
                    result.lnp = result.rpn * (Indata.La - Lp / 2)

                    result.ri = result.ln0 / Indata.La

                    '*** 方向鉄筋の部材接合部からの伸出しによる回転角θ1 を求める。
                    result.rn1 = MM.rm1
                Catch ex As Exception : Throw ex
                End Try
            End If
        Catch ex As Exception : Throw ex
        End Try
        Return result
    End Function


End Module
