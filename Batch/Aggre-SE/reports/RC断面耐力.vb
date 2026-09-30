Imports CSJ2K.j2k.codestream.HeaderInfo
Imports SNAPDBLib.CSNAPDB

Module RC断面耐力

    Public Function isPossible(ByVal DLinfo As IDLInfo) As Boolean
        Try
            With DLinfo
                Select Case .iType
                    Case 51 'RC矩形
                        If .ShpCtrl.T1 = 0 And .ShpCtrl.T2 = 0 Then
                            Return True
                        Else
                            Return False
                        End If
                    Case 52 'RC円形
                        If .ShpCtrl.R2 = 0 Then
                            Return True
                        Else
                            Return False
                        End If

                    Case 54 'RCT形
                        Return True

                    Case 55 'RC小判型
                        If .ShpCtrl.T1 = 0 And .ShpCtrl.T2 = 0 Then
                            Return True
                        Else
                            Return False
                        End If

                    Case Else
                        Return False
                End Select
            End With
        Catch ex As Exception
            Return False
        End Try
    End Function

    ''' <summary>
    ''' 総括表の備考欄に入力する文字列を生成
    ''' </summary>
    ''' <param name="type"></param>
    ''' <returns></returns>
    Public Function GetVydTypeName(ByVal type As Integer) As String
        Dim result As String = ""
        Try
            Select Case type
                Case -1
                    result = "Vud…h16RC標準Vdd"
                Case 0
                    result = ""
                Case 1 'SNAP準拠
                    result = ""
                Case 2
                    result = "Vud…耐震手引き"
                Case 3
                    result = "Vud…ＪＲ東日本"
                Case 4
                    result = "Vud…運輸機構(梁)"
                Case 5
                    result = "Vud…運輸機構(柱)"
            End Select
            Return result
        Catch ex As Exception
            Return result
        End Try
    End Function

    ''' <summary>
    ''' せん断耐力を算出する
    ''' </summary>
    ''' <param name="type">
    ''' -1:RC標準準拠
    ''' 1:SNAP準拠
    ''' 2:耐震照査の手引き:梁
    ''' 3:JR東日本
    ''' 4:JRTT:梁
    ''' 5:JRTT:柱
    '''  </param>
    ''' <param name="orgVyd">オリジナルのせん断耐力</param>
    ''' <param name="Nd">設計軸方向力</param>
    ''' <param name="Md">設計曲げモーメント</param>
    ''' <param name="DLinfo">断面情報</param>
    ''' <param name="La">せん断スパン</param>
    ''' <returns>せん断耐力</returns>
    Public Function Vyd(ByRef type As Integer,
                        ByVal orgVyd As Double,
                        ByVal Nd As Double,
                        ByVal Md As Double,
                        ByVal Mud As Double,
                        ByVal old_Mud As Double,
                        ByVal DLinfo As IDLInfo,
                        ByVal La As Double,
                        ByRef SIJI As Integer) As Double

        If La = 0 Then
            If type > 1 Then type = 0
            Return orgVyd
        End If
        If RC断面耐力.isPossible(DLinfo) = False Then Return orgVyd

        Dim result As Double = orgVyd
        Dim CalcStrength As New RCNonlinear.Strength

        With DLinfo

            '形状 ---------------------------------------------------------------------------------------------------------
            Dim h As Single     '断面高さ
            Dim b As Single     '断面幅
            Dim bf As Single
            Dim hf As Single
            Dim d As Single     '有効高さ
            Dim Asd As Single   '引張鉄筋の総断面積
            Dim Aw As Single = CalcStrength.Ass(String.Format("D{0}", .StlBar.DiwR)) * .StlBar.HRW
            Dim Ss As Single = .StlBar.SW
            Dim Mo As Single
            Dim alpha As Single = 1 'スラブを考慮したせん断耐力の算定式に出る係数α

            '材料 ---------------------------------------------------------------------------------------------------------
            Dim γc As Single = Convert.ToSingle(.Factar.GC) 'コンクリート材料係数γc
            Dim γs As Single = Convert.ToSingle(.Factar.GR) '鉄筋の材料係数γs
            Dim γbc As Single = Convert.ToSingle(.Factar.GBC) 'コンクリートのせん断に対する部材係数γbc
            Dim γbo As Single = Convert.ToSingle(.Factar.GBOD) 'コンクリートのせん断に対する部材係数γbc
            Dim γbs As Single = Convert.ToSingle(.Factar.GBV) 'せん断補強鉄筋のせん断に対する部材係数γbs
            Dim γbd As Single = Convert.ToSingle(.Factar.GBVD) 'デープビームせん断耐力に対する部材係数γbd
            Dim fck As Single = Convert.ToSingle(.Material.FCK)
            Dim fsy As Single = Convert.ToSingle(.StlBar.SDL)
            Dim fsw As Single = Convert.ToSingle(.StlBar.SDW)
            Dim Ec As Single = Convert.ToSingle(.Material.EC)
            Dim Es As Single = Convert.ToSingle(.StlBar.ES)

            Try
                '断面。鉄筋量の設定 および 断面終局耐力の計算 ---------------------------------------------------------------
                If DLinfo.iType = 51 Then '
#Region "RC矩形"
                    h = .ShpCtrl.H
                    b = .ShpCtrl.B
                    Dim Ass As New List(Of String)
                    Dim Num As New List(Of Single)
                    Dim dt As New List(Of Single)
                    If Md > 0 Then
                        '下側引張
                        For n = 0 To .StlBar.NS - 1
                            Ass.Add(String.Format("D{0}", .StlBar.Item(n).Dia))
                            Num.Add(.StlBar.Item(n).Num)
                            dt.Add(.StlBar.Item(n).Y)
                        Next

                        '有効高さ
                        Dim dd As Single = 0
                        For n = .StlBar.NS - 1 To (.StlBar.NS - .StlBar.DRTV2) Step -1
                            Dim ta = CalcStrength.Ass(Ass(n))   '鉄筋１本当りの断面積
                            Dim tn = Num(n)                     '引張鉄筋本数
                            Asd += ta * tn
                            dd += ta * tn * dt(n)
                        Next
                        d = dd / Asd '鉄筋重心位置

                    Else
                        '上側引張
                        For n = .StlBar.NS - 1 To 0 Step -1
                            Ass.Add(String.Format("D{0}", .StlBar.Item(n).Dia))
                            Num.Add(.StlBar.Item(n).Num)
                            dt.Add(h - .StlBar.Item(n).Y)
                        Next

                        '有効高さ
                        Dim dd As Single = 0
                        For n = .StlBar.NS - 1 To (.StlBar.NS - .StlBar.DRTV1) Step -1
                            Dim ta = CalcStrength.Ass(Ass(n))   '鉄筋１本当りの断面積
                            Dim tn = Num(n)                     '引張鉄筋本数
                            Asd += ta * tn
                            dd += ta * tn * dt(n)
                        Next
                        d = dd / Asd '鉄筋重心位置

                    End If

                    If Mud = 0 Then
                        Mud = CalcStrength.Mud(b, h, Ass, Num, dt, fck, fsy, Ec, Es, 0, γc, γs)
                    ElseIf Mud < 0 Then
                        Mud = Math.Abs(Mud)
                    End If
                    If old_Mud = 0 Then
                        old_Mud = CalcStrength.Mud(b, h, Ass, Num, dt, fck, fsy, Ec, Es, -Nd, γc, γs)
                    ElseIf old_Mud < 0 Then
                        old_Mud = Math.Abs(old_Mud)
                    End If

                    Mo = -Nd * h / 6000
#End Region

                ElseIf DLinfo.iType = 52 Then
#Region "RC円形"
                    '断面の情報を取得・設定
                    Dim R As Single = .ShpCtrl.R * 2 '直径
                    '鉄筋の位置と本数
                    Dim Ass As New List(Of String)
                    Dim Num As New List(Of Single)
                    Dim dt As New List(Of Single)
                    For n = 0 To .StlBar.NS - 1
                        Ass.Add(String.Format("D{0}", .StlBar.Item(n).Dia))
                        Num.Add(.StlBar.Item(n).Num)
                        dt.Add(.StlBar.Item(n).Y)
                    Next
                    '有効高さ
                    Dim A As Single = R ^ 2 * Math.PI / 4
                    h = Math.Sqrt(A)
                    b = h
                    d = h
                    Asd = 0
                    Try '45°の範囲の鉄筋の重心位置
                        Dim dd As Single = 0                    '円の中心から鉄筋重心位置までの距離
                        For n = 0 To .StlBar.NS - 1
                            Dim tn = Num(n) / 4                 '引張鉄筋（45°以内に配置されている鉄筋）本数
                            Dim ta = CalcStrength.Ass(Ass(n))   '鉄筋１本当りの断面積
                            Dim rt = .ShpCtrl.R - dt(n)         '円の中心から鉄筋までの基準距離
                            Dim fd = 360 / Num(n)               '鉄筋１本の角度
                            Dim f1 = 90 - (fd * RoundDown(tn / 2, 0)) '起点鉄筋の角度
                            Dim f2 = 180 - f1                           '終点鉄筋の角度
                            For f = f1 To f2 Step fd
                                Dim th = rt * Math.Sin(Radians(f))
                                Dim tb As Single = 1
                                If f = 45 Or f = 135 Then tb = 0.5
                                dd += th * ta * tb
                            Next
                            Asd += ta * tn
                        Next
                        dd = dd / Asd '円の中心から鉄筋重心位置までの距離
                        d = dd + h / 2
                    Catch
                        d = h
                    End Try
                    If Mud = 0 Then
                        Mud = CalcStrength.Mud_R(R, Ass, Num, dt, fck, fsy, Ec, Es, 0, γc, γs)
                    ElseIf Mud < 0 Then
                        Mud = Math.Abs(Mud)
                    End If
                    If old_Mud = 0 Then
                        old_Mud = CalcStrength.Mud_R(R, Ass, Num, dt, fck, fsy, Ec, Es, -Nd, γc, γs)
                    ElseIf old_Mud < 0 Then
                        old_Mud = Math.Abs(old_Mud)
                    End If

                    Mo = -Nd * R / 8000
#End Region

                ElseIf DLinfo.iType = 54 Then
#Region "RCT形"
                    h = .ShpCtrl.H
                    b = .ShpCtrl.Bw
                    bf = .ShpCtrl.Bf '.ShpCtrl.Be
                    hf = .ShpCtrl.Hf
                    Dim iMd = IIf(.ShpCtrl.iD = -1, -1, 1) * Math.Sign(Md)

                    Dim e2 = Math.Pow(h, 2) * b + Math.Pow(hf, 2) * (bf - b)
                    e2 /= 2 * (bf * hf + b * (h - hf))
                    Dim e1 = h - e2
                    Dim I = (1 / 3) * (b * Math.Pow(e1, 3) + bf * Math.Pow(e2, 3) - (bf - b) * Math.Pow(e2 - hf, 3))
                    Dim A = bf * hf + b * (h - hf)
                    Dim Z As Double

                    Dim Ass As New List(Of String)
                    Dim Num As New List(Of Single)
                    Dim dt As New List(Of Single)
                    If iMd > 0 Then
                        '下側引張
                        For n = 0 To .StlBar.NS - 1
                            Ass.Add(String.Format("D{0}", .StlBar.Item(n).Dia))
                            Num.Add(.StlBar.Item(n).Num)
                            dt.Add(.StlBar.Item(n).Y)
                        Next
                        If Mud = 0 Then
                            Mud = CalcStrength.Mud_T(b, h, bf, hf, Ass, Num, dt, fck, fsy, Ec, Es, 0, γc, γs)
                        ElseIf Mud < 0 Then
                            Mud = Math.Abs(Mud)
                        End If
                        If old_Mud = 0 Then
                            old_Mud = CalcStrength.Mud_T(b, h, bf, hf, Ass, Num, dt, fck, fsy, Ec, Es, -Nd, γc, γs)
                        ElseIf old_Mud < 0 Then
                            old_Mud = Math.Abs(old_Mud)
                        End If

                        '有効高さ
                        Dim dd As Single = 0
                        For n = .StlBar.NS - 1 To (.StlBar.NS - .StlBar.DRTV2) Step -1
                            Dim ta = CalcStrength.Ass(Ass(n))   '鉄筋１本当りの断面積
                            Dim tn = Num(n)                     '引張鉄筋本数
                            Asd += ta * tn
                            dd += ta * tn * dt(n)
                        Next
                        d = dd / Asd '鉄筋重心位置
                        Z = I / e1

                    Else
                        '上側引張
                        For n = .StlBar.NS - 1 To 0 Step -1
                            Ass.Add(String.Format("D{0}", .StlBar.Item(n).Dia))
                            Num.Add(.StlBar.Item(n).Num)
                            dt.Add(h - .StlBar.Item(n).Y)
                        Next

                        If Mud = 0 Then
                            Mud = CalcStrength.Mud(b, h, Ass, Num, dt, fck, fsy, Ec, Es, 0, γc, γs)
                        ElseIf Mud < 0 Then
                            Mud = Math.Abs(Mud)
                        End If
                        If old_Mud = 0 Then
                            old_Mud = CalcStrength.Mud(b, h, Ass, Num, dt, fck, fsy, Ec, Es, -Nd, γc, γs)
                        ElseIf old_Mud < 0 Then
                            old_Mud = Math.Abs(old_Mud)
                        End If

                        '有効高さ
                        Dim dd As Single = 0
                        For n = .StlBar.NS - 1 To (.StlBar.NS - .StlBar.DRTV1) Step -1
                            Dim ta = CalcStrength.Ass(Ass(n))   '鉄筋１本当りの断面積
                            Dim tn = Num(n)                     '引張鉄筋本数
                            Asd += ta * tn
                            dd += ta * tn * dt(n)
                        Next
                        d = dd / Asd '鉄筋重心位置
                        Z = I / e2

                    End If

                    Mo = -Nd * z / A / 1000

#End Region

                ElseIf DLinfo.iType = 55 Then

                    If .ShpCtrl.NK = 1 Then
#Region "短辺方向（横小判）"
                        h = .ShpCtrl.D1
                        Dim D2 = .ShpCtrl.D2
                        Dim A As Single = D2 * h
                        A += Math.Pow(h, 2) * Math.PI / 4
                        b = A / h

                        Dim Ass As New List(Of String)
                        Dim Num As New List(Of Single)
                        Dim dt As New List(Of Single)

                        For n = .StlBar.NS To .StlBar.Item.Length - 1
                            Ass.Add(String.Format("D{0}", .StlBar.Item(n).Dia))
                            Num.Add(.StlBar.Item(n).Num)
                            dt.Add(.StlBar.Item(n).Y)
                        Next

                        '有効高さ
                        Dim dd As Single = 0
                        For n = 0 To Ass.Count - 1
                            Dim ta = CalcStrength.Ass(Ass(n))   '鉄筋１本当りの断面積
                            Dim tn = Num(n)                     '引張鉄筋本数
                            Asd += ta * tn
                            dd += ta * tn * dt(n)
                        Next
                        d = dd / Asd '鉄筋重心位置
                        d = h - d

                        Mud = Math.Abs(Mud)
                        old_Mud = Math.Abs(old_Mud)

                        ' M0 の算定
                        Dim I = Math.PI * Math.Pow(h, 4) / 64
                        I += D2 * Math.Pow(h, 3) / 12
                        Dim y = h / 2
                        Dim Z = I / y

                        Mo = -Nd * Z / A / 1000
#End Region
                    Else
#Region "長い辺方向（縦小判）"
                        '断面の情報を取得・設定
                        b = .ShpCtrl.D1
                        Dim D2 = .ShpCtrl.D2
                        Dim A As Single = D2 * b
                        A += Math.Pow(b, 2) * Math.PI / 4
                        h = A / b

                        '鉄筋の位置と本数
                        Dim Ass As New List(Of String)
                        Dim Num As New List(Of Single)
                        Dim dt As New List(Of Single)
                        For n = 0 To .StlBar.NS - 1
                            Ass.Add(String.Format("D{0}", .StlBar.Item(n).Dia))
                            Num.Add(.StlBar.Item(n).Num)
                            dt.Add(.StlBar.Item(n).Y)
                        Next

                        Mud = Math.Abs(Mud)
                        old_Mud = Math.Abs(old_Mud)

                        '有効高さ
                        '45°の範囲の鉄筋の重心位置
                        Dim d45 = Math.PI * 1 / 4
                        Dim d135 = d45 * 3
                        Asd = 0
                        Dim dd As Single = 0                    '円の中心から鉄筋重心位置までの距離
                        For n = 0 To .StlBar.NS - 1
                            Dim tn = Num(n)
                            Dim ta = CalcStrength.Ass(Ass(n))   '鉄筋１本当りの断面積
                            Dim rt = b / 2 - dt(n)         '円の中心から鉄筋までの基準距離
                            Dim nn = 0 '本数
                            For j As Integer = 1 To tn
                                Dim fd = Math.PI * (j - 1) / (tn - 1)    '鉄筋１本の角度
                                If fd < d45 Or d135 < fd Then Continue For
                                Dim tb As Single = IIf(fd = d45 Or d135 = fd, 0.5, 1)
                                nn += tb
                                Dim th = rt * Math.Sin(fd)
                                dd += th * ta * tb
                            Next
                            Asd += ta * nn
                        Next
                        dd = dd / Asd '円の中心から鉄筋重心位置までの距離
                        d = dd + D2 + (h - D2) / 2

                        ' M0 の算定
                        Dim I = Math.PI * Math.Pow(b, 4) / 64
                        I += D2 * Math.Pow(b, 3) / 6
                        I += Math.PI * Math.Pow(D2, 2) * Math.Pow(b, 2) / 16
                        I += Math.Pow(D2, 3) * b / 12
                        Dim y = (D2 + b) / 2
                        Dim Z = I / y

                        Mo = -Nd * Z / A / 1000
#End Region
                    End If

                Else
                    type = 1 'SNAP準拠
                    Return orgVyd
                End If
            Catch ex As Exception
                type = 1 'SNAP準拠
                Return orgVyd
            End Try

            'せん断耐力の計算 -------------------------------------------------------------------------------------------
            fck = fck / γc
            fsy = fsy / γs
            fsw = fsw / γs
            Try
                Select Case type
                    Case 0
                        'H16 RC標準準拠
                        If La / d >= 2 Then
                            type = 1 'SNAP準拠
                            Return orgVyd
                        Else
                            type = -1 'ディープビーム(RC標準準拠)
                            result = GetVdd(b, d, La, -Nd, fck, Asd, mo, old_Mud, Aw, Ss, γbd)
                            If result = -1 Then
                                type = 1 'SNAP準拠
                                Throw New Exception("平成16年 RC標準準拠 Vddの計算に失敗しました。")
                            End If
                        End If

                    Case 1
                        If La / d >= 2 Then
                            type = 1 'SNAP準拠
                            Return orgVyd
                        End If
                        If SIJI = 2 Then
                            type = 1 'SNAP準拠
                            Return orgVyd
                            ' Vasud を Batch が算定するシチュエーションは無い
                            'type = -3 'ディープビーム R5 RC標準準拠 Vasud
                            ''スラブを考慮するか判定する '''''''''''''
                            'bf = 0
                            'hf = 0
                            ''''''''''''''''''''''''''''''''''''''''
                            'result = GetVasud(b, h, d, La, -Nd, fck, Asd, Mo, Mud, fsw, Aw, Ss, γbs, γbo, bf, hf)
                            'If result = -1 Then
                            '    type = 1 'SNAP準拠
                            '    Throw New Exception("令和5年 RC標準準拠 Vasudの計算に失敗しました。")
                            'End If
                        Else
                            type = -2 'ディープビーム R5 RC標準準拠 ディープビーム Vdd
                            result = 1.14 * GetVdd(b, d, La, -Nd, fck, Asd, mo, Mud, Aw, Ss, γbd)
                            If result = -1 Then
                                type = 1 'SNAP準拠
                                Throw New Exception("令和5年 RC標準準拠 Vddの計算に失敗しました。")
                            End If
                        End If
                        result = Math.Max(result, orgVyd)

                    Case 2 '耐震照査の手引き:梁
                        If La / d >= 2 Then
                            type = 1 'SNAP準拠
                            Return orgVyd
                        ElseIf La / d <= 1 Then
                            type = -1 'ディープビーム(RC標準準拠)
                            result = GetVdd(b, d, La, -Nd, fck, Asd, mo, old_Mud, Aw, Ss, γbd)
                            If result = -1 Then
                                type = 1 'SNAP準拠
                                Throw New Exception("JRTT:梁 Vddの計算に失敗しました。")
                            End If
                        Else
                            Dim Vsd As Single = GetVsd(d, fsw, Aw, Ss, γbs)
                            If Vsd = -1 Then
                                type = 1 'SNAP準拠
                                Throw New Exception("耐震照査の手引き:梁 Vsdの計算に失敗しました。")
                            End If
                            Dim Vcd = GetVcd(b, d, -Nd, fck, Asd, mo, old_Mud, γbc)
                            If Vcd = -1 Then
                                type = 1 'SNAP準拠
                                Throw New Exception("耐震照査の手引き:梁 Vcdの計算に失敗しました。")
                            End If
                            Dim fad = (4 * d / La) - 0.75
                            Dim pw As Single = Aw / (b * Ss)
                            Dim cot = Math.Min(0.44 * (La / d) - 35 * pw + 0.58, 1)
                            result = fad * Vcd + cot * Vsd
                        End If

                    Case 3 'JR東日本
                        If La / d >= 1 Then
                            Dim Vsd As Single = GetVsd(d, fsw, Aw, Ss, γbs)
                            If Vsd = -1 Then
                                type = 1 'SNAP準拠
                                Throw New Exception("JR東日本 Vsdの計算に失敗しました。")
                            End If
                            If La / d >= 2 Then
                                Dim Vcd = GetVcd(b, d, -Nd, fck, Asd, mo, old_Mud, γbc)
                                If Vcd = -1 Then
                                    type = 1 'SNAP準拠
                                    Throw New Exception("JR東日本 Vcdの計算に失敗しました。")
                                End If
                                Dim βa As Single = 0.75 + (1.4 / (La / d))
                                Vcd = Vcd * βa
                                result = Vcd + Vsd
                            Else
                                Dim βd As Single = Math.Min(Math.Pow(1000 / d, 1 / 4), 1.5)
                                Dim pc As Single = Asd / (b * d)
                                Dim βp As Single = Math.Min(Math.Pow(100 * pc, 1 / 3), 1.5)
                                Dim βn As Single = Getβn(Nd, mo, old_Mud)
                                Dim Vcd = 0.76 * Math.Pow(La / d, -1.166) * Math.Pow(fck, 1 / 3) * βd * βp * βn * b * d / γbc / 1000
                                result = Vcd + Vsd
                            End If
                        Else
                            type = 1 'SNAP準拠
                            Return orgVyd
                        End If

                    Case 4 'JRTT:梁
                        If La / d >= 2 Then
                            type = 1 'SNAP準拠
                            Return orgVyd
                        ElseIf La / d <= 1 Then
                            type = -1 'ディープビーム(RC標準準拠)
                            result = GetVdd(b, d, La, -Nd, fck, Asd, mo, old_Mud, Aw, Ss, γbd)
                            If result = -1 Then
                                type = 1 'SNAP準拠
                                Throw New Exception("JRTT:梁 Vddの計算に失敗しました。")
                            End If
                        Else
                            Dim Vsd As Single = GetVsd(d, fsw, Aw, Ss, γbs)
                            If Vsd = -1 Then
                                type = 1 'SNAP準拠
                                Throw New Exception("JRTT:梁 Vsdの計算に失敗しました。")
                            End If
                            Dim Vcd = GetVcd(b, d, -Nd, fck, Asd, 0, old_Mud, γbc) 'βn=0(Mo=0)とする
                            If Vcd = -1 Then
                                type = 1 'SNAP準拠
                                Throw New Exception("JRTT:梁 Vcdの計算に失敗しました。")
                            End If
                            Dim fad = 4 / (La / d) - 0.75
                            Dim pw As Single = Aw / (b * Ss)
                            Dim cot = 0.44 * (La / d) - 35 * pw + 0.58
                            result = fad * Vcd + cot * Vsd
                        End If

                    Case 5 'JRTT:柱
                        If La / d >= 2 Then
                            type = 1 'SNAP準拠
                            Return orgVyd
                        ElseIf La / d <= 1 Then
                            type = -1 'ディープビーム(RC標準準拠)
                            result = GetVdd(b, d, La, -Nd, fck, Asd, mo, old_Mud, Aw, Ss, γbd)
                            If result = -1 Then Throw New Exception("JRTT:柱 Vddの計算に失敗しました。")
                        Else
                            Dim pw As Single = Math.Min(Aw / (b * Ss), 0.0045)
                            Dim Vsd As Single
                            If pw < 0.002 Then
                                Vsd = GetVsd(d, fsw, 0, Ss, γbs)
                                pw = 0
                            Else
                                Vsd = GetVsd(d, fsw, Aw, Ss, γbs)
                            End If
                            If Vsd = -1 Then
                                type = 1 'SNAP準拠
                                Throw New Exception("JRTT:柱 Vsdの計算に失敗しました。")
                            End If
                            Dim Vcd = GetVcd(b, d, -Nd, fck, Asd, mo, old_Mud, γbc)
                            If Vcd = -1 Then
                                type = 1 'SNAP準拠
                                Throw New Exception("JRTT:柱 Vcdの計算に失敗しました。")
                            End If
                            Dim fad = 4 / (La / d) - 0.75
                            Dim cot = 0.44 * (La / d) - 35 * pw + 0.58
                            result = fad * Vcd + cot * Vsd
                        End If
                    Case Else
                        type = 1 'SNAP準拠
                        Return orgVyd
                End Select
            Catch ex As Exception
                Return orgVyd
            End Try
        End With

        Return result

    End Function

    '''''' このせん断耐力は、JR-SNAPの計算結果を使う、Vasud を Batch が算定するシチュエーションは無い
    ''''''''' <summary>
    ''''''''' 両端固定のディープビーム(令和5年 RC標準準拠)
    ''''''''' </summary>
    ''''''''' <returns></returns>
    ''''''Friend Function GetVasud(ByVal b As Single,
    ''''''                        ByVal h As Single,
    ''''''                        ByVal d As Single,
    ''''''                        ByVal L As Single,
    ''''''                        ByVal Nd As Single,
    ''''''                        ByVal fcd As Single,
    ''''''                        ByVal Asd As Single,
    ''''''                        ByVal Mo As Single,
    ''''''                        ByVal Mu As Single,
    ''''''                                           _
    ''''''                        ByVal fwyd As Single,
    ''''''                        ByVal Aw As Single,
    ''''''                        ByVal Ss As Single,
    ''''''                        ByVal γbs As Single,
    ''''''                        ByVal γbo As Single,
    ''''''                                            _
    ''''''                         ByVal bf As Single,
    ''''''                         ByVal hf As Single) As Double
    ''''''    Dim result As Double

    ''''''    Try
    ''''''        ' pw * fwyd / fcd ≦ 0.1 とするのがよい ----
    ''''''        Aw = GetR5Aw(b, fcd, fwyd, Aw, Ss)

    ''''''        ' Vsd の算定 ----
    ''''''        Dim Vsd As Double = 0
    ''''''        Try
    ''''''            fwyd = Math.Min(fwyd, Math.Min(25 * fcd, 800))
    ''''''            Dim Z As Single = d / 1.15

    ''''''            If γbs = 0 Then γbs = 1.1

    ''''''            Vsd = Aw * fwyd / Ss * Z / γbs
    ''''''            Vsd = Vsd / 1000
    ''''''        Catch ex As Exception
    ''''''            Vsd = 0
    ''''''        End Try

    ''''''        ' Vod の算定 ----
    ''''''        Dim Vod As Double = 0
    ''''''        Dim a As Double = 1.0
    ''''''        Try
    ''''''            Dim fvcd As Single = Math.Min(0.2 * Math.Pow(fcd, 1 / 3), 0.72)
    ''''''            Dim focd As Single = 17.4 * fvcd

    ''''''            Dim βd As Single = Math.Min(Math.Pow(1000 / d, 1 / 4), 1.5)
    ''''''            Dim pc As Single = Asd / (b * d)
    ''''''            Dim βp As Single = Math.Min(Math.Pow(100 * pc, 1 / 3), 1.5)

    ''''''            Dim pw = Aw / (b * Ss)
    ''''''            Dim βw As Single = -30 * Math.Pow(pw * fwyd / fcd, 2) + 1.3

    ''''''            If L / h < 1.5 Then
    ''''''                L = 1.5 * h
    ''''''            End If
    ''''''            Dim tanθc = h / (2 * L)

    ''''''            Dim hc = 0.5 * h

    ''''''            Dim βn As Single = Getβn(Nd, Mo, Mu)

    ''''''            If γbo = 0 Then γbo = 1.3

    ''''''            Vod = βd * βp * βn * βw * focd * b * hc * tanθc / γbo
    ''''''            Vod = Vod / 1000

    ''''''            ' スラブを考慮したせん断耐力の算定式に出る係数α の算定 ----
    ''''''            Dim bf_bw = Math.Min(bf / b, 3)
    ''''''            Dim tf_h = Math.Min(hf / h, 0.35)
    ''''''            If tf_h < 0.16 Then
    ''''''                a = 1
    ''''''            Else
    ''''''                a = 1 + 0.38 * Math.Pow(bf_bw - 1, 5 / 4) * Math.Pow(tf_h, 5 / 3) * Math.Pow(pw * fwyd, 1 / 3)
    ''''''            End If

    ''''''        Catch ex As Exception
    ''''''            Vod = 0
    ''''''        End Try

    ''''''        result = Vsd + a * Vod

    ''''''    Catch ex As Exception
    ''''''        result = -1
    ''''''    End Try
    ''''''    Return result
    ''''''End Function

    ''' <summary>
    '''  pw * fwyd / fcd ≦ 0.1 となる Aw を計算
    ''' </summary>
    ''' <param name="b"></param>
    ''' <param name="fcd"></param>
    ''' <param name="fwyd"></param>
    ''' <param name="Aw"></param>
    ''' <param name="Ss"></param>
    ''' <returns></returns>
    Private Function GetR5Aw(ByVal b As Single,
                           ByVal fcd As Single,
                           ByVal fwyd As Single,
                            ByVal Aw As Single,
                            ByVal Ss As Single) As Single

        Dim pw = Aw / (b * Ss)
        If pw * fwyd / fcd > 0.1 Then
            Aw = 0.1 * (b * Ss) * fcd / fwyd
        End If

        Return Aw
    End Function

    ''' <summary>
    ''' βn の計算
    ''' </summary>
    ''' <param name="Nd"></param>
    ''' <param name="Mo"></param>
    ''' <param name="Mu"></param>
    ''' <returns></returns>
    Private Function Getβn(ByVal Nd As Single,
                       ByVal Mo As Single,
                       ByVal Mu As Single) As Single

        Dim Mud As Single = Math.Abs(Mu)

        Dim result As Single = IIf(Nd >= 0,
                       Math.Min(1 + (2 * Mo / Mud), 2),
                       Math.Max(1 + (4 * Mo / Mud), 0))

        Return result
    End Function

    ''' <summary>
    ''' ディープビーム(RC標準準拠)
    ''' </summary>
    ''' <param name="b">幅</param>
    ''' <param name="d">有効高さ</param>
    ''' <param name="La">せん断スパン</param>
    ''' <param name="Nd">軸圧縮力</param>
    ''' <param name="fck">コンクリート強度</param>
    ''' <param name="Asd">引張鉄筋量</param>
    ''' <param name="Mo">Nd * h / 6</param>
    ''' <param name="Mu">終局耐力</param>
    ''' <param name="Aw">帯鉄筋量</param>
    ''' <param name="Ss">帯鉄筋間隔</param>
    ''' <param name="γbd">部材係数</param>
    ''' <returns></returns>
    Private Function GetVdd(ByVal b As Single,
                            ByVal d As Single,
                            ByVal La As Single,
                            ByVal Nd As Single,
                            ByVal fck As Single,
                            ByVal Asd As Single,
                            ByVal Mo As Single,
                            ByVal Mu As Single,
                            ByVal Aw As Single,
                            ByVal Ss As Single,
                            ByVal γbd As Single) As Double
        Dim result As Double
        Try
            Dim βd As Single = Math.Min(Math.Pow(1000 / d, 1 / 4), 1.5)
            Dim βn As Single = Getβn(Nd, Mo, Mu)

            Dim pw As Single = Aw / (b * Ss)
            Dim βw As Single = Math.Max(4.2 * Math.Pow(100 * pw, 1 / 3) * ((La / d) - 0.75) / Math.Sqrt(fck), 0)
            Dim pc As Single = Asd / (b * d)
            Dim βp As Single = Math.Min((1 + Math.Sqrt(100 * pc)) / 2, 1.5)
            Dim βa As Single = 5 / (1 + Math.Pow(La / d, 2))
            Dim fdd As Single = 0.19 * Math.Sqrt(fck)
            result = (βd * βn + βw) * βp * βa * fdd * b * d / γbd
            result = result / 1000

        Catch ex As Exception
            result = -1
        End Try
        Return result
    End Function

    ''' <summary>
    ''' スターラップが負担する船団耐力
    ''' </summary>
    ''' <param name="d">有効高さ</param>
    ''' <param name="fsw">せん断補強鉄筋強度</param>
    ''' <param name="Aw">帯鉄筋量</param>
    ''' <param name="Ss">帯鉄筋間隔</param>
    ''' <param name="γbs">部材係数</param>
    ''' <returns></returns>
    Private Function GetVsd(ByVal d As Single,
                            ByVal fsw As Single,
                            ByVal Aw As Single,
                            ByVal Ss As Single,
                            ByVal γbs As Single) As Double

        Dim result As Double
        Try
            Dim Z As Single = d / 1.15
            result = Aw * fsw / Ss * Z / γbs
            result = result / 1000
        Catch ex As Exception
            result = -1
        End Try
        Return result

    End Function

    ''' <summary>
    ''' コンクリートが負担する船団耐力
    ''' </summary>
    ''' <param name="b">幅</param>
    ''' <param name="d">有効高さ</param>
    ''' <param name="Nd">軸圧縮力</param>
    ''' <param name="fck">コンクリート強度</param>
    ''' <param name="Asd">引張鉄筋量</param>
    ''' <param name="Mo">Nd * h / 6</param>
    ''' <param name="Mu">終局耐力</param>
    ''' <param name="γbc">部材係数</param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Function GetVcd(ByVal b As Single,
                            ByVal d As Single,
                            ByVal Nd As Single,
                            ByVal fck As Single,
                            ByVal Asd As Single,
                            ByVal Mo As Single,
                            ByVal Mu As Single,
                            ByVal γbc As Single) As Double

        Dim result As Double
        Try
            Dim βd As Single = Math.Min(Math.Pow(1000 / d, 1 / 4), 1.5)
            Dim pc As Single = Asd / (b * d)
            Dim βp As Single = Math.Min(Math.Pow(100 * pc, 1 / 3), 1.5)
            Dim βn As Single = Getβn(Nd, Mo, Mu)

            Dim fvcd As Single = Math.Min(0.2 * Math.Pow(fck, 1 / 3), 0.72)
            result = βd * βp * βn * fvcd * b * d / γbc
            result = result / 1000

        Catch ex As Exception
            result = -1
        End Try
        Return result

    End Function

End Module