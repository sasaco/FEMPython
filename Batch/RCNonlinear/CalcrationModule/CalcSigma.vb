Module CalcSigma

    Public Function CalcReaction(ByVal Indata As InputData) As clsSigmaReaction
        Dim result As New clsSigmaReaction
        Try
            '中立軸の範囲をチェックする。*** *** *** *** *** *** *** *** *** ***
            If Indata.Md > 0 Then
                Dim LimitMyd = CalcMyd.Myd(Indata)
                Dim LimitMmd = CalcMmd.Mmd(Indata)
                Select Case Indata.Md
                    Case Is < LimitMyd.Md
                        result = fsearch(Indata, LimitMyd.fi, 0)
                    Case Is = LimitMyd.Md
                        result = LimitMyd
                    Case Is < LimitMmd.Md
                        result = fsearch(Indata, LimitMmd.fi, 0)
                    Case Is = LimitMmd.Md
                        result = LimitMmd
                    Case Else
                        Throw New Exception("設計曲げモーメントがMmdを超えています。応力度の計算はできません。")
                End Select
            Else
                Return New clsSigmaReaction()
            End If
        Catch ex As Exception
            Throw ex
        End Try
        Return result
    End Function

    ''' <summary>
    ''' 曲率および中立軸をトライアルし、解を見つける関数
    ''' </summary>
    ''' <param name="Indata">インプットデータ</param>
    ''' <param name="maxf">曲率の最大値</param>
    ''' <param name="minf">曲率の最小値</param>
    ''' <returns>clsSigmaReaction</returns>
    Private Function fsearch(ByVal Indata As InputData,
                             ByVal maxf As Double, ByVal minf As Double,
                             Optional ByRef n As Long = 0) As clsSigmaReaction
        Dim result As New clsSigmaReaction
        Try
            Dim f As Double
            f = (maxf + minf) / 2
            result = xsearch(Indata, 0, Indata.Hc, 0, f)

            '*** 釣り合い計算によって求められた軸力と設計軸力を比較して 曲率fの仮定を調整する。
            If result Is Nothing Then
                '中立軸のトライアルに失敗しました。原因を確認してください。
                Return Nothing
            ElseIf result.Md = 0 Then
                '曲率および中立軸をトライアル中にMd=0 になりました。原因を確認してください。
                Return Nothing
            ElseIf n > PublicConfig.LIMITERVALUE2 Then
                If Math.Abs(1 - Indata.Md / result.Md) < PublicConfig.LIMITERVALUE1 Then
                    'トライアル回数がリミットに達していても、ある程度答えが近ければ現状を結果として返す
                    Return result
                Else
                    '曲率のトライアルに失敗しました。原因を確認してください。
                    Return Nothing
                End If
            ElseIf Indata.Md < result.Md Then
                n += 1
                result = fsearch(Indata, f, minf, n)
            ElseIf Indata.Md > result.Md Then
                n += 1
                result = fsearch(Indata, maxf, f, n)
            ElseIf result.Md = Indata.Md Then
                Return result
            End If
        Catch ex As Exception
            Throw ex
        End Try
        Return result
    End Function

    ''' <summary>
    ''' 中立軸をトライアルし、解を見つける関数
    ''' </summary>
    ''' <param name="Indata">インプットデータ</param>
    ''' <param name="x">中立軸の初期値</param>
    ''' <param name="w">中立軸をトライアルする幅</param>
    ''' <param name="d">前回のトライアル時に中立軸を増やした(=1)のか減らした(=-1)のか</param>
    ''' <param name="f">曲率</param>
    ''' <returns>clsSigmaReaction</returns>
    Private Function xsearch(ByVal Indata As InputData,
                             ByVal x As Double, ByVal w As Double, ByVal d As Integer,
                             ByVal f As Double,
                             Optional ByRef n As Long = 0) As clsSigmaReaction
        Dim result As clsSigmaReaction
        Try
            '*** 釣り合い計算
            result = CalcStrength.CalcStrength(Indata, x, f)

            '*** 釣り合い計算によって求められた軸力と設計軸力を比較して 中立軸xの仮定を調整する。
            If n > PublicConfig.LIMITERVALUE2 Then
                If Math.Abs(1 - Indata.Nd / result.Nd) < PublicConfig.LIMITERVALUE1 Then
                    'トライアル回数がリミットに達していても、ある程度答えが近ければ現状を結果として返す
                    Return result
                Else
                    '中立軸のトライアルに失敗しました。原因を確認してください。
                    Return Nothing
                End If
            ElseIf Indata.Nd < result.Nd Then
                If d <> -1 Then w = w / 2
                d = -1
                x = x - w
                n += 1
                result = xsearch(Indata, x, w, d, f, n)
            ElseIf Indata.Nd > result.Nd Then
                If d <> 1 Then w = w / 2
                d = 1
                x = x + w
                n += 1
                result = xsearch(Indata, x, w, d, f, n)
            End If

        Catch ex As Exception
            Throw ex
        End Try
        Return result
    End Function


End Module
