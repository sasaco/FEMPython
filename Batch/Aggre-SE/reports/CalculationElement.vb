Public Class CalculationElement

#Region "メンバ変数"

    '入力データ -----------------------------------------------------
    Private _CaseDB As List(Of CalculationCase)

    Public DLNo As Integer = 0

    ''' <summary>部材番号で総括表作成には使わないが、pdf ドキュメントにおけるケース毎の照査表において 表題に印字される番号 </summary>
    Public MNo As Integer = -1

    Public DLinfo As CSNAPDBEx.IDLInfo
    Public 損傷レベルの制限値1 As Integer
    Public 損傷レベルの制限値2 As Integer
    Public 引張側 As Integer
    Public 照査タイプ番号 As Integer

    '計算結果データ -----------------------------------------------------
    Private _断面照査 As cls断面照査

    Public NG部材List As List(Of Integer)

    '何かしらの照査を行うのか判定する
    Public Function Is照査(ca As CalculationCase) As Boolean
        If Is破壊形態の照査(ca) Then Return True
        If Isせん断照査(ca) Then Return True
        If Is損傷照査(ca) Then Return True
        Return False
    End Function


    Private Function Is破壊形態の照査(ca As CalculationCase) As Boolean
        If DLNo < 0 Then Return False 'MFデータはせん断の照査を行わない
        If ca._解析対象(3) = False Then Return False
        If ca._照査対象.Count > 0 Then
            For Each type As Integer In ca._照査対象
                If 照査タイプ番号 = type Then
                    Return True
                    Exit For
                End If
            Next
        Else
            Return True
        End If
        Return False
    End Function

    Private Function Isせん断照査(ca As CalculationCase) As Boolean
        If DLNo < 0 Then Return False 'MFデータはせん断の照査を行わない
        If ca._解析対象(4) = False Then Return False
        If ca._照査対象.Count > 0 Then
            For Each type As Integer In ca._照査対象
                If 照査タイプ番号 = type Then
                    Return True
                    Exit For
                End If
            Next
        Else
            Return True
        End If
        Return False
    End Function

    Private Function Is損傷照査(ca As CalculationCase) As Boolean
        If ca._解析対象(2) = False Then Return False
        If ca._照査対象.Count > 0 Then
            For Each type As Integer In ca._照査対象
                If 照査タイプ番号 = type Then
                    Return True
                    Exit For
                End If
            Next
        Else
            Return True
        End If
        Return False
    End Function

    Private Function IsL1照査(ca As CalculationCase) As Boolean
        If ca._解析対象(5) = False Then Return False
        Return True
    End Function

    Private Function Is引張側照査対象(TargetValue) As Boolean
        Dim result As Boolean = True
        If 引張側 = 0 Then
            result = True
        Else
            If 引張側 = Math.Sign(TargetValue) Then
                result = True
            Else
                result = False
            End If
        End If
        Return result
    End Function

#End Region

#Region "初期設定"

    Sub New(CaseDB As List(Of CalculationCase))
        '入力データ
        _CaseDB = CaseDB
        '解析結果データ
        引張側 = 0
        _断面照査 = Nothing
        NG部材List = New List(Of Integer)
    End Sub

#End Region

#Region "断面入力情報"

    ''' <summary>当該断面がM-θ部材かどうかを調べる </summary>
    ''' <returns>M-θ部材 なら True</returns>
    Public Function isθ() As Boolean
        If DLNo < 0 Then
            'MFデータ
            Dim iK = _CaseDB.First._SNAPDB.InputInfo.APInfo(-DLNo).IK
            If iK = 1 Then
                Return True
            End If
        Else
            If _CaseDB.First._SNAPDB.GetApIK(DLNo) = 1 Then
                Return True
            End If
        End If
        Return False
    End Function


#End Region

#Region "断面照査結果を求める"

    Public ReadOnly Property 断面照査結果 As cls断面照査
        Get
            Try
                If _断面照査 Is Nothing Then
                    _断面照査 = New cls断面照査

                    If Me.損傷レベルの制限値1 = 1 Then
                        '曲げ耐力の照査 -----------------------------------------------
                        Call 曲げ耐力の照査(Me._断面照査)
                        'せん断耐力の照査 -----------------------------------------------
                        Call せん断耐力の照査(Me._断面照査)
                    Else
                        '破壊形態の推定照査 -----------------------------------------------
                        Call 破壊形態の照査(Me._断面照査)
                        If Me._断面照査.破壊形態の照査.安全率 >= 1 Then
                            Me.損傷レベルの制限値1 = -1
                            Me.損傷レベルの制限値2 = -1
                            If Me._断面照査.破壊形態の照査.jadge1 < 1 Then
                                Call せん断破壊の照査(Me._断面照査)
                            End If
                        Else
                            'せん断耐力の照査 -----------------------------------------------
                            Call せん断耐力の照査(Me._断面照査)
                        End If
                        '損傷レベルの照査 -------------------------------------------------
                        Call 損傷レベルの照査(Me._断面照査)
                    End If

                    'L1地震動の照査 --------------------------------------------------
                    Call L1地震動の照査(Me._断面照査)

                End If

                Return _断面照査
            Catch ex As Exception
                Throw ex
            End Try
        End Get
    End Property

    Private Sub 破壊形態の照査(ByRef result As cls断面照査)

        Dim key As Double = 0
        For Each ca In Me._CaseDB

            If Is破壊形態の照査(ca) = False Then Continue For

            Try
                Dim Targets As List(Of Integer)
                If Me.MNo = -1 Then
                    Targets = ca._SNAPDB.GetApNos(DLNo)
                Else
                    Targets = New List(Of Integer)
                    Targets.Add(ca._SNAPDB.InputInfo.MemberInfo(Me.MNo).M)
                End If

                For Each ApNo In Targets
                    If ca._断面設定List(Me.DLNo).断面種別 <> 61 _
                        And ca._断面設定List(Me.DLNo).断面種別 <> 62 _
                        And ca._断面設定List(Me.DLNo).断面種別 <> 300 _
                        And ca._断面設定List(Me.DLNo).断面種別 <> 501 Then


                        Dim tmp破壊 As New cls破壊形態の照査
                        Dim 引張側TargetValue As Integer = 0
                        With ca._SNAPDB.OutputInfo.OutSoukatu(ApNo).Break
                            Dim 部材番号 = ca._SNAPDB.GetMemberNo(ApNo)
                            tmp破壊.決定ケース = String.Format("{0} ({1})", ca.DataID + 1, 部材番号)
                            tmp破壊.最大曲げモーメントMdmax = .Mdmax
                            tmp破壊.設計曲げ耐力Mm = .Mm
                            tmp破壊.Mdmax_Mm = .Mdmax_Mm
                            tmp破壊.jadge1 = .Judge1
                            If ca._SNAPDB.GetApIK(DLNo) = 1 Then
                                'M-θの場合
                                tmp破壊.決定ステップ = .StepNo3.ToString("0")
                                引張側TargetValue = Me.引張側
                                tmp破壊.せん断スパンLa = .La
                                tmp破壊.isθ = True
                                tmp破壊.最大設計せん断力Vdmax = .Vmu
                                tmp破壊.Vdmax時の曲げモーメントMd = .Mm
                                tmp破壊.Vdmax時の設計軸力Nd = .Nd


                                '*** せん断耐力を算定する。 23/04/21 sasa ***
                                Dim 部材本数 = ca._SNAPDB.GetAp(部材番号).ALF
                                Dim Vyd As Double = .Vyd / 部材本数

                                If FormSettings.Option_カスタムせん断耐力 = 0 Then
                                    Vyd = Vyd * 部材本数
                                    tmp破壊.設計せん断耐力Vud = Vyd
                                Else
                                    Dim VydType As Integer = ca._断面設定List(Me.DLNo).せん断耐力種別
                                    If VydType <= 1 Then
                                        'Vyd = .Vyd
                                    Else
                                        Dim VydLa As Integer = ca._断面設定List(Me.DLNo).Vydせん断スパン
                                        Dim SIJI As Integer = ca._断面設定List(Me.DLNo).支持
                                        Dim DLInfo = ca._SNAPDB.DLInfo(ca._SNAPDB.GetDLNo(部材番号))
                                        Dim Nd As Double = .Nd / 部材本数
                                        Dim Md As Double = .Md / 部材本数
                                        Dim Mud As Double = IIf(Md > 0, Math.Abs(Me.DLinfo.Mud1), Math.Abs(Me.DLinfo.Mud2))
                                        If Mud = 0 Then Mud = .Mm
                                        Mud = Mud / 部材本数
                                        Dim old_Mud = .Mm / 部材本数
                                        'Vyd = .Vyd / 部材本数
                                        Vyd = RC断面耐力.Vyd(VydType, Vyd, Nd, Md, Mud, old_Mud, DLInfo, VydLa, SIJI)

                                    End If
                                    Vyd = Vyd * 部材本数
                                    tmp破壊.設計せん断耐力Vud = Vyd
                                    If tmp破壊.設計せん断耐力Vud <= 0 Then
                                        If FormSettings.Option_大量解析プログラム = 0 Then
                                            Throw New Exception("せん断耐力の取得に失敗しました。" + vbLf + String.Format(" - ケース {0} - ステップ{1}", ca.DataName, .StepNo3))
                                        Else
                                            result.破壊形態の照査.備考 = "せん断耐力の取得に失敗しました。" + String.Format(" - ケース {0} - ステップ{1}", ca.DataName, .StepNo3)
                                            Return
                                        End If
                                    End If
                                    tmp破壊.備考 = RC断面耐力.GetVydTypeName(VydType)
                                End If
                                '******************************************************
                                tmp破壊.α = 1
                                tmp破壊.安全率 = .Vmu_Vyd * .Vyd / Vyd
                            Else
                                'M-φの場合
                                tmp破壊.決定ステップ = .StepNo2.ToString("0")
                                引張側TargetValue = .Md
                                tmp破壊.せん断スパンLa = 0
                                tmp破壊.isθ = False
                                tmp破壊.最大設計せん断力Vdmax = .Vdmax
                                tmp破壊.Vdmax時の曲げモーメントMd = .Md
                                tmp破壊.Vdmax時の設計軸力Nd = .Nd
                                '*** せん断耐力を算定する。 23/04/21 sasa ***
                                Dim 部材本数 = ca._SNAPDB.GetAp(部材番号).ALF
                                Dim Vyd As Double = .Vyd / 部材本数

                                If FormSettings.Option_カスタムせん断耐力 = 0 Then
                                    Vyd = Vyd * 部材本数
                                    tmp破壊.設計せん断耐力Vud = Vyd
                                Else
                                    Dim VydType As Integer = ca._断面設定List(Me.DLNo).せん断耐力種別
                                    Dim VydLa As Integer = ca._断面設定List(Me.DLNo).Vydせん断スパン
                                    Dim SIJI As Integer = ca._断面設定List(Me.DLNo).支持

                                    If VydLa = 0 Then ' If VydType = 1 Or VydLa = 0 Then
                                        'Vyd = .Vyd
                                    Else
                                        Dim DLInfo = ca._SNAPDB.DLInfo(ca._SNAPDB.GetDLNo(部材番号))
                                        Dim Nd As Double = .Nd / 部材本数
                                        Dim Md As Double = .Md / 部材本数
                                        Dim Mud As Double = IIf(Md > 0, Math.Abs(Me.DLinfo.Mud1), Math.Abs(Me.DLinfo.Mud2))
                                        If Mud = 0 Then Mud = .Mm
                                        Mud = Mud / 部材本数
                                        Dim old_Mud As Double = .Mm / 部材本数
                                        Dim Vd As Double = .Vdmax / 部材本数
                                        If VydLa < 0 Then
                                            VydLa = Math.Abs(Md / Vd) * 1000
                                        End If
                                        Vyd = RC断面耐力.Vyd(VydType, Vyd, Nd, Md, Mud, old_Mud, DLInfo, VydLa, SIJI)
                                    End If
                                    Vyd = Vyd * 部材本数
                                    tmp破壊.設計せん断耐力Vud = Vyd
                                    If tmp破壊.設計せん断耐力Vud <= 0 Then
                                        If FormSettings.Option_大量解析プログラム = 0 Then
                                            Throw New Exception("せん断耐力の取得に失敗しました。" + vbLf + String.Format(" - ケース {0} - ステップ{1}", ca.DataName, .StepNo1))
                                        Else
                                            result.破壊形態の照査.備考 = "せん断耐力の取得に失敗しました。" + String.Format(" - ケース {0} - ステップ{1}", ca.DataName, .StepNo1)
                                            Return
                                        End If
                                    End If
                                    tmp破壊.備考 = RC断面耐力.GetVydTypeName(VydType)
                                End If
                                '******************************************************

                                tmp破壊.α = .Alpha

                                tmp破壊.安全率 = .A_Vdmax_Vyd * .Vyd / Vyd
                            End If
                        End With
                        If Is引張側照査対象(引張側TargetValue) Then
                            If key < tmp破壊.安全率 Then
                                key = tmp破壊.安全率
                                result.破壊形態の照査 = tmp破壊
                                tmp破壊 = Nothing
                            End If
                        End If
                    End If
                Next
            Catch ex As Exception
                If FormSettings.Option_大量解析プログラム = 0 Then
                    Throw New Exception("破壊形態の照査時にエラーが発生しました" + vbCrLf + ca.DataName + vbCrLf + ex.Message)
                Else
                    result.破壊形態の照査.備考 = "破壊形態の照査時にエラーが発生しました(" + ca.DataName + ")" + ex.Message
                End If
            End Try
        Next

    End Sub

    Private Sub せん断破壊の照査(ByRef result As cls断面照査)
        Dim key As Double = result.せん断破壊の照査.Gami_Vdmax_Vyd
        For Each ca In Me._CaseDB

            If Is破壊形態の照査(ca) = False Then Continue For

            Try
                Dim Targets As List(Of Integer)
                If Me.MNo = -1 Then
                    Targets = ca._SNAPDB.GetApNos(DLNo)
                Else
                    Targets = New List(Of Integer)
                    Targets.Add(ca._SNAPDB.InputInfo.MemberInfo(Me.MNo).M)
                End If

                For Each ApNo In Targets
                    If ca._断面設定List(Me.DLNo).断面種別 <> 61 _
                        And ca._断面設定List(Me.DLNo).断面種別 <> 62 _
                        And ca._断面設定List(Me.DLNo).断面種別 <> 300 _
                        And ca._断面設定List(Me.DLNo).断面種別 <> 501 Then

                        Dim 部材番号 As Integer = ca._SNAPDB.GetMemberNo(ApNo)
                        Dim tmpせん断 As New clsせん断破壊の照査
                        Dim istep As Integer = -1
                        With ca._SNAPDB.OutputInfo.OutSoukatu(ApNo).Sendan

                            '*** せん断耐力を算定する。 16/06/11 sasa ***
                            Dim 部材本数 = ca._SNAPDB.GetAp(部材番号).ALF
                            Dim Vyd As Double = .Vyd / 部材本数
                            If FormSettings.Option_カスタムせん断耐力 = 0 Then
                                ' pass
                            Else
                                Dim VydType As Integer = ca._断面設定List(Me.DLNo).せん断耐力種別
                                Dim VydLa As Integer = ca._断面設定List(Me.DLNo).Vydせん断スパン
                                Dim SIJI = ca._断面設定List(Me.DLNo).支持
                                Dim IsMθ As Boolean = ca._SNAPDB.GetApIK(DLNo) = 1
                                If VydLa = 0 Or (IsMθ = True And VydType <= 1) Then
                                    'Vyd = .Vyd
                                Else
                                    Dim DLInfo = ca._SNAPDB.DLInfo(ca._SNAPDB.GetDLNo(部材番号))
                                    istep = .StepNo2
                                    If istep <= 0 Then istep = .StepNo1
                                    If istep <= 0 Then istep = ca._SNAPDB.OutputInfo.OutSoukatu(ApNo).Break.StepNo2
                                    If istep <= 0 Then istep = ca._SNAPDB.OutputInfo.OutSoukatu(ApNo).Break.StepNo1
                                    If istep <= 0 Then istep = ca._SNAPDB.OutputInfo.OutSoukatu(ApNo).Break.StepNo3

                                    Dim si = ca._SNAPDB.OutputInfo.StepCtrl(istep).MemberItem(部材番号).Si
                                    Dim sj = ca._SNAPDB.OutputInfo.StepCtrl(istep).MemberItem(部材番号).Sj
                                    Dim Nd As Double = 0
                                    Dim Md As Double = 0
                                    If Math.Abs(si) > Math.Abs(sj) Then
                                        Nd = ca._SNAPDB.OutputInfo.StepCtrl(istep).MemberItem(部材番号).Ni / 部材本数
                                        Md = ca._SNAPDB.OutputInfo.StepCtrl(istep).MemberItem(部材番号).Mi / 部材本数
                                        If VydLa < 0 Then
                                            VydLa = Math.Abs(Md / si) * 1000
                                        End If
                                    Else
                                        Nd = ca._SNAPDB.OutputInfo.StepCtrl(istep).MemberItem(部材番号).Nj / 部材本数
                                        Md = ca._SNAPDB.OutputInfo.StepCtrl(istep).MemberItem(部材番号).Mj / 部材本数
                                        If VydLa < 0 Then
                                            VydLa = Math.Abs(Md / sj) * 1000
                                        End If
                                    End If

                                    With ca._SNAPDB.OutputInfo.StepCtrl(istep).MemberItem(部材番号)
                                        Dim Mud As Double = IIf(Md > 0, Math.Abs(Me.DLinfo.Mud1), Math.Abs(Me.DLinfo.Mud2))
                                        If Mud = 0 Then
                                            Mud = IIf(Md > 0, Math.Max(.M_Shousa_S.M2, .M_Shousa_S.M3), Math.Max(.M_Shousa_F.M2, .M_Shousa_F.M3))
                                        End If
                                        Mud = Mud / 部材本数
                                        Dim old_Mud As Double = IIf(Md > 0, Math.Max(.M_Shousa_S.M2, .M_Shousa_S.M3), Math.Max(.M_Shousa_F.M2, .M_Shousa_F.M3))
                                        old_Mud = old_Mud / 部材本数
                                        Vyd = RC断面耐力.Vyd(VydType, .Vyd, Nd, Md, Mud, old_Mud, DLInfo, VydLa, SIJI)
                                    End With

                                End If
                                tmpせん断.備考 = RC断面耐力.GetVydTypeName(VydType)
                            End If
                            '******************************************************
                            Vyd = Vyd * 部材本数

                            If Vyd > 0 Then
                                tmpせん断.決定ケースM = String.Format("{0} ({1})", ca.DataID + 1, 部材番号)
                                tmpせん断.決定ステップM = .StepNo1.ToString("0")
                                tmpせん断.最大曲げモーメントMdmax = .Mdmax
                                tmpせん断.設計曲げ降伏耐力Myd = .Myd
                                tmpせん断.γi1 = .Gami
                                tmpせん断.Gami_Mdmax_Myd = .Gami_Mdmax_Myd
                                tmpせん断.jadge1 = .Judge1

                                tmpせん断.決定ケースV = String.Format("{0} ({1})", ca.DataID + 1, 部材番号)
                                tmpせん断.決定ステップV = .StepNo2.ToString("0")
                                tmpせん断.最大設計せん断力Vdmax = .Vdmax
                                tmpせん断.設計せん断耐力Vud = Vyd
                                tmpせん断.γi2 = .Gami2
                                If tmpせん断.γi2 = 0 Then
                                    tmpせん断.γi2 = .Gami
                                End If
                                tmpせん断.Gami_Vdmax_Vyd = tmpせん断.γi2 * .Vdmax / Vyd
                                tmpせん断.jadge2 = .Judge2
                                If tmpせん断.Gami_Vdmax_Vyd > 1 Then
                                    tmpせん断.jadge2 = 1
                                Else
                                    tmpせん断.jadge2 = 0
                                End If

                                If Is引張側照査対象(tmpせん断.最大曲げモーメントMdmax) Then
                                    '決定ケースの記録 -----------------------------------------------
                                    If key < tmpせん断.Gami_Vdmax_Vyd Then
                                        key = tmpせん断.Gami_Vdmax_Vyd
                                        result.せん断破壊の照査 = tmpせん断
                                    End If
                                    If key < tmpせん断.Gami_Mdmax_Myd Then
                                        key = tmpせん断.Gami_Mdmax_Myd
                                        result.せん断破壊の照査 = tmpせん断
                                    End If
                                    'NG部材の記録 -----------------------------------------------
                                    If tmpせん断.Gami_Vdmax_Vyd > 1 Then
                                        Call SetNG部材(部材番号)
                                    ElseIf tmpせん断.Gami_Mdmax_Myd > 1 Then
                                        Call SetNG部材(部材番号)
                                    End If
                                End If
                            End If
                        End With
                    End If
                Next
            Catch ex As Exception
                If FormSettings.Option_大量解析プログラム = 0 Then
                    Throw New Exception("せん断破壊の照査時にエラーが発生しました" + vbCrLf + ca.DataName + vbCrLf + ex.Message)
                Else
                    result.せん断破壊の照査.備考 = "せん断破壊の照査時にエラーが発生しました(" + ca.DataName + ")" + ex.Message
                End If
            End Try
        Next

    End Sub

    Private Sub せん断耐力の照査(ByRef result As cls断面照査)

        If DLNo < 0 Then Return 'MFデータは照査しない

        Dim key As Double = result.せん断破壊の照査.Gami_Vdmax_Vyd
        For Each ca In Me._CaseDB

            Dim Flg = False
            If Me.損傷レベルの制限値1 < 2 Then
                If Is破壊形態の照査(ca) Then
                    Flg = True
                End If
            End If
            If Isせん断照査(ca) = True Then
                Flg = True
            End If
            If Flg = False Then
                Continue For
            End If

            Try

                Dim MaxStep As Integer = Math.Max(Math.Max(ca.応答値.安全性_最大応答step, ca.応答値.復旧性_最大応答step), ca.応答値.復旧性_L1震度step)

                Dim Targets As List(Of Integer)
                If Me.MNo = -1 Then
                    Targets = ca._SNAPDB.GetMembers(DLNo)
                Else
                    Targets = New List(Of Integer)
                    Targets.Add(Me.MNo)
                End If

                For Each 部材番号 In Targets
                    If ca._断面設定List(Me.DLNo).断面種別 <> 61 _
                        And ca._断面設定List(Me.DLNo).断面種別 <> 62 _
                        And ca._断面設定List(Me.DLNo).断面種別 <> 300 _
                        And ca._断面設定List(Me.DLNo).断面種別 <> 501 Then

                        For istep As Integer = 0 To MaxStep
                            With ca._SNAPDB.OutputInfo.StepCtrl(istep).MemberItem(部材番号)

                                '応答値の集計 -----------------------------------------------
                                Dim Vd As Double = 0
                                Dim Nd As Double = 0
                                Dim Md As Double = 0
                                If Is引張側照査対象(.Mi) = True And Is引張側照査対象(.Mj) = True Then
                                    If Math.Abs(.Si) > Math.Abs(.Sj) Then
                                        Vd = Math.Abs(.Si)
                                        Nd = .Ni
                                        Md = .Mi
                                    Else
                                        Vd = Math.Abs(.Sj)
                                        Nd = .Nj
                                        Md = .Mj
                                    End If
                                ElseIf Is引張側照査対象(.Mi) = True Then
                                    Vd = Math.Abs(.Si)
                                    Nd = .Ni
                                    Md = .Mi
                                ElseIf Is引張側照査対象(.Mj) = True Then
                                    Vd = Math.Abs(.Sj)
                                    Nd = .Nj
                                    Md = .Mj
                                End If

                                If .Vd_T <> 0 Then
                                    Vd = .Vd_T
                                End If

                                If Vd <= 0 Then Continue For


                                Dim tmpせん断 As New clsせん断破壊の照査 With {
                                    .決定ケースV = String.Format("{0} ({1})", ca.DataID + 1, 部材番号),
                                    .決定ステップV = istep,
                                    .最大設計せん断力Vdmax = Vd
                                }

                                '*** せん断耐力を算定する。 23/04/21 sasa ***
                                Dim 部材本数 = ca._SNAPDB.GetAp(部材番号).ALF
                                Dim Vyd As Double = .Vud / 部材本数
                                If Vyd = 0 Then
                                    Vyd = .Vyd / 部材本数
                                End If

                                If FormSettings.Option_カスタムせん断耐力 = 0 Then
                                    Vyd = Vyd * 部材本数
                                    tmpせん断.設計せん断耐力Vud = Vyd
                                Else
                                    Dim VydType As Integer = ca._断面設定List(Me.DLNo).せん断耐力種別
                                    Dim VydLa As Integer = ca._断面設定List(Me.DLNo).Vydせん断スパン
                                    Dim SIJI = ca._断面設定List(Me.DLNo).支持
                                    If VydLa < 0 Then
                                        VydLa = Math.Abs(Md / Vd) * 1000
                                    End If
                                    Dim IsMθ As Boolean = ca._SNAPDB.GetApIK(DLNo) = 1
                                    If VydLa = 0 Or (IsMθ = True And VydType <= 1) Then
                                        'Vyd = Vyd
                                    Else
                                        Dim DLInfo = ca._SNAPDB.DLInfo(ca._SNAPDB.GetDLNo(部材番号))
                                        Nd = Nd / 部材本数
                                        Md = Md / 部材本数

                                        Dim Mud As Double = Math.Abs(IIf(Md > 0, Me.DLinfo.Mud1, Me.DLinfo.Mud2))
                                        If Mud = 0 Then
                                            Mud = IIf(Md > 0, Math.Max(.M_Shousa_S.M2, .M_Shousa_S.M3), Math.Min(.M_Shousa_F.M2, .M_Shousa_F.M3))
                                        End If
                                        Mud = Mud / 部材本数
                                        Dim old_Mud As Double = IIf(Md > 0, Math.Max(.M_Shousa_S.M2, .M_Shousa_S.M3), Math.Min(.M_Shousa_F.M2, .M_Shousa_F.M3))
                                        old_Mud = old_Mud / 部材本数
                                        Vyd = RC断面耐力.Vyd(VydType, Vyd, Nd, Md, Mud, old_Mud, DLInfo, VydLa, SIJI)

                                    End If
                                    Vyd = Vyd * 部材本数
                                    tmpせん断.設計せん断耐力Vud = Vyd
                                    If tmpせん断.設計せん断耐力Vud <= 0 Then
                                        If FormSettings.Option_大量解析プログラム = 0 Then
                                            Throw New Exception("せん断耐力の取得に失敗しました。" + vbLf + String.Format(" - ケース {0} - ステップ{1}", ca.DataName, istep))
                                        Else
                                            result.せん断破壊の照査.備考 = "せん断耐力の取得に失敗しました。" + String.Format(" - ケース {0} - ステップ{1}", ca.DataName, istep)
                                            Return
                                        End If
                                    End If
                                    tmpせん断.備考 = RC断面耐力.GetVydTypeName(VydType)
                                End If
                                '******************************************************
                                tmpせん断.γi2 = ca._SNAPDB.Getγi(DLNo)
                                tmpせん断.Gami_Vdmax_Vyd = tmpせん断.γi2 * tmpせん断.最大設計せん断力Vdmax / tmpせん断.設計せん断耐力Vud
                                tmpせん断.jadge2 = .Vshousa
                                If tmpせん断.Gami_Vdmax_Vyd > 1 Then
                                    tmpせん断.jadge2 = 1
                                Else
                                    tmpせん断.jadge2 = 0
                                End If

                                '決定ケースの記録 -----------------------------------------------
                                If key < tmpせん断.Gami_Vdmax_Vyd Then
                                    key = tmpせん断.Gami_Vdmax_Vyd
                                    result.せん断破壊の照査.Copyせん断照査(tmpせん断)
                                End If
                                'NG部材の記録 -----------------------------------------------
                                If tmpせん断.Gami_Vdmax_Vyd > 1 Then
                                    Call SetNG部材(部材番号)
                                End If
                            End With
                        Next
                    End If
                Next

            Catch ex As Exception
                If FormSettings.Option_大量解析プログラム = 0 Then
                    Throw New Exception("せん断耐力の照査時にエラーが発生しました" + vbCrLf + ca.DataName + vbCrLf + ex.Message)
                Else
                    result.せん断破壊の照査.備考 = "せん断耐力の照査時にエラーが発生しました(" + ca.DataName + ")" + ex.Message
                End If
            End Try
        Next

    End Sub

    Private Sub 曲げ耐力の照査(ByRef result As cls断面照査)
        Dim key As Double = result.せん断破壊の照査.Gami_Mdmax_Myd
        For Each ca In Me._CaseDB
            Try
                If Is損傷照査(ca) = True Then

                    Dim tmpMd照査 = 曲げ耐力の照査(ca, key)
                    If key < tmpMd照査.Gami_Mdmax_Myd Then
                        key = tmpMd照査.Gami_Mdmax_Myd
                        result.せん断破壊の照査.Copy曲げ照査(tmpMd照査)
                    End If

                End If
            Catch ex As Exception
                If FormSettings.Option_大量解析プログラム = 0 Then
                    Throw New Exception("曲げ耐力の照査時にエラーが発生しました" + vbCrLf + ca.DataName + vbCrLf + ex.Message)
                Else
                    result.せん断破壊の照査.備考 = "曲げ耐力の照査時にエラーが発生しました(" + ca.DataName + ")" + ex.Message
                End If
            End Try
        Next

    End Sub

    Private Function 曲げ耐力の照査(ByRef ca As CalculationCase, ByVal key As Double) As clsせん断破壊の照査

        Dim result As New clsせん断破壊の照査
        Try

            Dim MaxStep As Integer = Math.Max(Math.Max(ca.応答値.安全性_最大応答step, ca.応答値.復旧性_最大応答step), ca.応答値.復旧性_L1震度step)

            Dim Targets As List(Of Integer)
            If Me.MNo = -1 Then
                If DLNo < 0 Then
                    Targets = New List(Of Integer)
                    Targets.Add(ca._SNAPDB.GetMemberNo(-DLNo))
                Else
                    Targets = ca._SNAPDB.GetMembers(DLNo)
                End If

            Else
                Targets = New List(Of Integer)
                Targets.Add(Me.MNo)
            End If

            For Each 部材番号 In Targets
                For istep As Integer = 0 To MaxStep
                    With ca._SNAPDB.OutputInfo.StepCtrl(istep).MemberItem(部材番号)
                        '応答値の集計 -----------------------------------------------
                        Dim Md As Double = 0
                        Dim Nd As Double = 0
                        If Is引張側照査対象(.Mi) = True And Is引張側照査対象(.Mj) = True Then
                            If Math.Abs(.Mi) > Math.Abs(.Mj) Then
                                Md = Math.Abs(.Mi)
                                Nd = .Ni
                            Else
                                Md = Math.Abs(.Mj)
                                Nd = .Nj
                            End If
                        ElseIf Is引張側照査対象(.Mi) = True Then
                            Md = Math.Abs(.Mi)
                            Nd = .Ni
                        ElseIf Is引張側照査対象(.Mj) = True Then
                            Md = Math.Abs(.Mj)
                            Nd = .Nj
                        End If
                        '軸力固定値入力がある場合

                        If DLNo < 0 Then
                            'MFデータ
                        Else
                            Dim t As Single = -1 * ca._SNAPDB.DLInfo(ca._SNAPDB.GetDLNo(部材番号)).N
                            If t <> 0 Then Nd = t
                        End If

                        Dim _断面設定index As Integer
                        If DLNo < 0 Then
                            _断面設定index = -DLNo
                        Else
                            Dim MFCount As Integer = ca._SNAPDB.GetMFCount
                            _断面設定index = Me.DLNo + MFCount
                        End If

                        If Md > 0 Then
                            Dim tmpMd照査 As New clsせん断破壊の照査
                            tmpMd照査.決定ケースM = String.Format("{0} ({1})", ca.DataID + 1, 部材番号)
                            tmpMd照査.決定ステップM = istep
                            tmpMd照査.最大曲げモーメントMdmax = Md
                            tmpMd照査.応答軸力N = Nd
                            tmpMd照査.設計曲げ降伏耐力Myd = Math.Abs(.My)
                            If tmpMd照査.設計曲げ降伏耐力Myd = 0 Then
                                If Md >= 0 Then
                                    tmpMd照査.設計曲げ降伏耐力Myd = .M_Shousa_S.M1
                                Else
                                    tmpMd照査.設計曲げ降伏耐力Myd = Math.Abs(.M_Shousa_F.M1)
                                End If
                            End If
                            tmpMd照査.γi1 = ca._SNAPDB.Getγi(DLNo)
                            tmpMd照査.Gami_Mdmax_Myd = tmpMd照査.γi1 * tmpMd照査.最大曲げモーメントMdmax / tmpMd照査.設計曲げ降伏耐力Myd
                            tmpMd照査.jadge1 = IIf(tmpMd照査.Gami_Mdmax_Myd < 1, 0, 1)
                            '決定ケースの記録 -----------------------------------------------
                            If key < tmpMd照査.Gami_Mdmax_Myd Then
                                key = tmpMd照査.Gami_Mdmax_Myd
                                result = tmpMd照査
                            End If
                            'NG部材の記録 -----------------------------------------------
                            If tmpMd照査.Gami_Mdmax_Myd > 1 Then
                                Call SetNG部材(部材番号)
                            End If
                        End If
                    End With
                Next
            Next
            Return result

        Catch ex As Exception
            If FormSettings.Option_大量解析プログラム = 0 Then
                Throw New Exception("曲げ耐力の照査時にエラーが発生しました" + vbCrLf + ca.DataName + vbCrLf + ex.Message)
            Else
                result.備考 = "曲げ耐力の照査時にエラーが発生しました(" + ca.DataName + ")" + ex.Message
                Return result
            End If
        End Try

    End Function

    Private Sub 損傷レベルの照査(ByRef result As cls断面照査)

        Dim key1 As Double = Double.MinValue
        Dim key2 As Double = 0

        For Each ca In Me._CaseDB
            Try
                If Is損傷照査(ca) = True Then

                    Dim γi As Double = ca._SNAPDB.Getγi(DLNo)

                    Dim Targets As List(Of Integer)
                    If Me.MNo = -1 OrElse DLNo < 0 Then
                        Targets = ca._SNAPDB.GetMembers(DLNo)
                    Else
                        Targets = New List(Of Integer)
                        Targets.Add(Me.MNo)
                    End If

                    'L1地震時のみ照査対象とする場合
                    If ca._解析対象(0) = False And ca._解析対象(1) = False Then
                        If IsL1照査(ca) = True Then
                            Dim key3 = result.せん断破壊の照査.Gami_Mdmax_Myd
                            Dim tmpMd照査 = 曲げ耐力の照査(ca, key3)
                            If key3 < tmpMd照査.Gami_Mdmax_Myd Then
                                key3 = tmpMd照査.Gami_Mdmax_Myd
                                result.せん断破壊の照査.Copy曲げ照査(tmpMd照査)
                            End If
                        End If
                    Else
                        Dim st As Integer
                        Dim ed As Integer
                        st = IIf(ca._解析対象(0) = True, 0, 1)
                        ed = IIf(ca._解析対象(1) = True, 1, 0)

                        ' 制限値を.R10 ファイルから取得しておく
                        For i As Integer = st To ed
                            Dim MaxStep As Integer
                            Dim 損傷レベルの制限値 As Integer
                            If i = 0 Then
                                MaxStep = ca.応答値.復旧性_最大応答step
                                損傷レベルの制限値 = Math.Abs(Me.損傷レベルの制限値1)
                            Else
                                MaxStep = ca.応答値.安全性_最大応答step
                                損傷レベルの制限値 = Math.Abs(Me.損傷レベルの制限値2)
                            End If
                            If IsNothing(損傷レベルの制限値) Then
                                Throw New Exception(String.Format("DLNo{0} は、損傷レベルの制限値が正しく入力されていません。照査を省略します。", DLNo))
                            End If
                            Dim Key = String.Format("MaxStep:{0},損傷レベルの制限値:{1}", MaxStep, 損傷レベルの制限値)
                            If Not ca.SonshoDict.ContainsKey(Key) Then
                                ca.SonshoDict.Add(Key, ca._SNAPDB.SonshoR10(MaxStep, 損傷レベルの制限値))
                            End If
                        Next i

                        ' 
                        For Each 部材番号 In Targets

                            Try
                                For i As Integer = st To ed

                                    Dim MaxStep As Integer
                                    Dim 損傷レベルの制限値 As Integer
                                    If i = 0 Then
                                        MaxStep = ca.応答値.復旧性_最大応答step
                                        損傷レベルの制限値 = Math.Abs(Me.損傷レベルの制限値1)
                                    Else
                                        MaxStep = ca.応答値.安全性_最大応答step
                                        損傷レベルの制限値 = Math.Abs(Me.損傷レベルの制限値2)
                                    End If

                                    Dim tmp1 As cls損傷レベル = R10損傷レベルの照査(ca, MaxStep, 部材番号, 損傷レベルの制限値, γi)
                                    If tmp1 Is Nothing Then
                                        tmp1 = DB2損傷レベルの照査(ca, MaxStep, 部材番号, 損傷レベルの制限値, γi)
                                    End If

                                    '安全度が一番大きいものを決定ケースとする。
                                    If tmp1.Limit > 0 Then
                                        Dim Lv = tmp1.IsLevel
                                        If key2 <= Lv Then
                                            If key1 < tmp1.安全度 Then
                                                result.損傷レベル = tmp1
                                                key1 = tmp1.安全度
                                                key2 = Lv
                                            End If
                                            'NG部材の記録 -----------------------------------------------
                                            If tmp1.安全度 > 1 Then
                                                Call SetNG部材(部材番号)
                                            End If
                                        End If
                                    End If

                                Next
                            Catch ex As Exception
                                ' Throw New Exception("部材番号" + 部材番号.ToString() + "の、" + ex.Message)''''制限値φiが 0になる時があるのでスルー
                            End Try
                        Next
                    End If
                End If
            Catch ex As Exception
                If FormSettings.Option_大量解析プログラム = 0 Then
                    Throw New Exception("損傷レベルの照査時にエラーが発生しました" + vbCrLf + ca.DataName + vbCrLf + ex.Message)
                Else
                    result.損傷レベル.備考(0) = "損傷レベルの照査時にエラーが発生しました(" + ca.DataName + ")" + ex.Message
                End If
            End Try
        Next 'ca

    End Sub

    Private Function R10損傷レベルの照査(ca As CalculationCase, istep As Integer, 部材番号 As Integer, 損傷レベルの制限値 As Integer, γi As Double) As cls損傷レベル

        Dim Key = String.Format("MaxStep:{0},損傷レベルの制限値:{1}", istep, 損傷レベルの制限値)

        If Not ca.SonshoDict.ContainsKey(Key) Then
            Return Nothing
        End If
        Dim tmp1 As New cls損傷レベル

        '応答値の集計 -----------------------------------------------
        Try
            Dim OutSokatu As SNAPDBLib.CSNAPDB.IOutSokatu() = ca.SonshoDict(Key)
            If IsNothing(OutSokatu) Then
                Return Nothing
            End If
            Dim Ap = ca._SNAPDB.GetAp(部材番号)
            'Dim MFCount As Integer = ca._SNAPDB.GetMFCount
            'Dim ApNo As Integer = Ap.MeNo + MFCount
            With OutSokatu(Ap.MeNo)
                If Not Is引張側照査対象(.Sonsho.Phid) Then
                    Return Nothing '計算結果と引張側が違った場合何もしない
                End If

                tmp1.決定ケース = String.Format("{0} ({1})", ca.DataID + 1, 部材番号)
                tmp1.決定ステップ = istep
                tmp1.γi = γi
                tmp1.Limit = 損傷レベルの制限値
                tmp1.φr = .Sonsho.Phid
                tmp1.Nd = .Sonsho.Nd
                tmp1.φi(0) = .Sonsho.Phi1
                tmp1.φi(1) = .Sonsho.Phi2
                tmp1.φi(2) = .Sonsho.Phi3
                If Not IsNothing(.Coment) Then
                    tmp1.備考 = .Coment
                End If
            End With
        Catch ex As Exception
            Return Nothing
        End Try

        Return tmp1

    End Function

    Private Function DB2損傷レベルの照査(ca As CalculationCase, MaxStep As Integer, 部材番号 As Integer, 損傷レベルの制限値 As Integer, γi As Double) As cls損傷レベル

        Dim tmp1 As New cls損傷レベル
        Dim key0 As Double = -1

        For istep = MaxStep To MaxStep

            Dim tmp0 As New cls損傷レベル

            With ca._SNAPDB.OutputInfo.StepCtrl(istep).MemberItem(部材番号)

                '応答値の集計 -----------------------------------------------

                tmp0.決定ケース = String.Format("{0} ({1})", ca.DataID + 1, 部材番号)
                tmp0.決定ステップ = istep
                tmp0.γi = γi
                tmp0.Limit = 損傷レベルの制限値
                tmp0.φr = .Outou_FAI_Shousa
                If DLNo < 0 Then
                    tmp0.Nd = 0 'MFデータ
                Else
                    Dim t As Single = -1 * ca._SNAPDB.DLInfo(ca._SNAPDB.GetDLNo(部材番号)).N
                    If t <> 0 Then tmp0.Nd = t
                End If
                If tmp0.Nd = 0 Then
                    '軸力は１つ前のステップ
                    With ca._SNAPDB.OutputInfo.StepCtrl(Math.Max(istep - 1, 0)).MemberItem(部材番号)
                        tmp0.Nd = (.Ni + .Nj) / 2
                    End With
                End If
                tmp0.φi(0) = .FAI_Shousa_S.Fai2
                tmp0.φi(1) = .FAI_Shousa_S.Fai3
                tmp0.φi(2) = .FAI_Shousa_S.Fai4
                If Math.Sign(tmp0.φr) = -1 Then '負
                    tmp0.φi(0) = .FAI_Shousa_F.Fai2
                    tmp0.φi(1) = .FAI_Shousa_F.Fai3
                    tmp0.φi(2) = .FAI_Shousa_F.Fai4
                End If

                'If ca._断面設定List(Me.DLNo).断面種別 <> 61 _
                '        And ca._断面設定List(Me.DLNo).断面種別 <> 62 _
                '        And ca._断面設定List(Me.DLNo).断面種別 <> 300 Then
                '    If Math.Sign(tmp0.φr) = -1 Then '負
                '        tmp0.φi(0) = .FAI_Shousa_F.Fai1
                '    Else
                '        tmp0.φi(0) = .FAI_Shousa_S.Fai1
                '    End If
                'Else
                Select Case .HisenkeiFlg
                    Case 0 '通常部材

                    Case 1 '低せん断スパン
                        tmp0.φi(1) = tmp0.φi(0)
                        tmp0.φi(2) = tmp0.φi(0)
                        tmp0.備考(0) = "低せん断スパン部材"
                    Case 2 '高軸圧縮
                        tmp0.φi(2) = tmp0.φi(1)
                        tmp0.備考(0) = "高軸圧縮発生"
                    Case 3 '高軸引張
                        tmp0.φi(1) = tmp0.φi(2)
                        tmp0.備考(0) = "高軸引張発生"
                    Case 4 'PHC中詰めなし
                        tmp0.φi(2) = tmp0.φi(1)
                End Select
                If tmp0.φi(1) = 0 Then
                    tmp0.φi(1) = tmp0.φi(0)
                End If
                If tmp0.φi(2) = 0 Then
                    tmp0.φi(2) = tmp0.φi(1)
                End If

                ' PHCで高軸圧縮、高軸引張の場合に対応するフラグがないため、独自に備考を追加
                Select Case .HisenkeiFlg
                    Case 4, 5 'PHC
                        If tmp0.備考(0).Length = 0 Then
                            If Math.Abs(tmp0.φi(0) - tmp0.φi(1)) < 0.000001 Then
                                tmp0.備考(0) = "高軸圧縮発生"
                            End If
                            If Math.Abs(tmp0.φi(0)) < 0.000001 Then
                                tmp0.備考(0) = "高軸引張発生"
                            End If
                        End If
                End Select

                ' 低せん断部材のとき φi = 0となり 安全率算出の際の除算に失敗するから
                For i = 1 To 2
                    If tmp0.φi(i) = 0 Then
                        tmp0.φi(i) = tmp0.φi(i - 1)
                    End If
                Next i

                '作用軸力が適用範囲外文字をセット ------------------------------------------------------------------------------
                For Each t In ca.応答値.軸圧縮力適用範囲外List
                    If t.ID = 部材番号 Then
                        If t.iStep <= MaxStep Then
                            If tmp1.備考(0).Length > 0 Then
                                tmp1.備考(1) = "軸力適用範囲外"
                            Else
                                tmp1.備考(0) = "軸力適用範囲外"
                            End If
                        End If
                    End If
                Next

                '決定ケースの記録 -----------------------------------------------
                If Is引張側照査対象(tmp0.φr) Then
                    'φd が一番大きいものを決定ケースとする。
                    Dim Val As Double = Math.Round(Math.Abs(tmp0.φr), 6)

                    If key0 < Val Then
                        'result.損傷レベル = tmp0
                        key0 = Val
                        tmp1 = tmp0

                    End If
                Else
                    '損傷レベルの照査エラー
                End If

            End With
        Next istep

        Return tmp1

    End Function


    Private Sub L1地震動の照査(ByRef result As cls断面照査)

        Dim ca As CalculationCase = Nothing
        Try
            With result
                Dim key As Double = 0
                For Each ca In Me._CaseDB
                    If IsL1照査(ca) = True Then
                        Dim tmp降伏震度 = Get降伏震度(ca, Me.DLNo)
                        If tmp降伏震度 >= 0 Then
                            Dim 安全度 As Double = ca.応答値.復旧性_L1震度 / tmp降伏震度
                            '決定ケースの記録 -----------------------------------------------
                            If key < 安全度 Then
                                key = 安全度
                                .L1地震動.決定ケース = ca.DataID + 1
                                .L1地震動.降伏震度 = tmp降伏震度
                                .L1地震動.L1設計震度 = ca.応答値.復旧性_L1震度
                            End If
                        End If
                    End If
                Next
            End With

            'NG部材の記録 -----------------------------------------------
            For Each ca In Me._CaseDB
                Try
                    If IsL1照査(ca) = True Then
                        Dim iStep As Integer = ca.応答値.復旧性_L1震度step
                        With ca._SNAPDB
                            For Each MNo As Integer In ca._SNAPDB.GetMembers(DLNo)
                                Try
                                    If .OutputInfo.StepCtrl(iStep).MemberItem(MNo).SCFlg > 1 Then
                                        If ca.応答値.震度(iStep) <= ca.応答値.復旧性_L1震度 Then
                                            Call SetNG部材(MNo)
                                        End If
                                    End If
                                Catch ex As Exception
                                    'このブロックのエラーは無視
                                End Try
                            Next
                        End With
                    End If
                Catch ex As Exception
                    'このブロックのエラーは無視
                End Try
            Next

        Catch ex As Exception
            If FormSettings.Option_大量解析プログラム = 0 Then
                If ca Is Nothing Then
                    Throw New Exception("L1地震動の照査にエラーが発生しました" + vbCrLf + ex.Message)
                Else
                    Throw New Exception("L1地震動の照査にエラーが発生しました" + vbCrLf + ca.DataName)
                End If
            Else
                '改行しない
                Throw New Exception(ex.Message)
            End If
        End Try

    End Sub

    Private Function Get降伏震度(ca As CalculationCase, DLNo As Integer) As Double
        Try
            Dim maxStep As Integer = ca.応答値.maxStep

            ' 23/05/17 プッシュオーバー解析において終局後に震度が L1震度を下回ることがある
            '回避策として 荷重変位曲線上のピーク（最大震度）より後ろのstep は対象外にする
            If ca.応答値.最大震度step > 0 Then
                maxStep = ca.応答値.最大震度step
            End If

            With ca._SNAPDB
                Dim members As New List(Of Integer)
                If DLNo < 0 Then
                    members.Add(.GetMemberNo(-DLNo)) 'MFデータ
                Else
                    members = .GetMembers(DLNo)
                End If

                For i As Integer = 0 To maxStep
                    For Each MNo As Integer In members
                        With .OutputInfo.StepCtrl(i).MemberItem(MNo)
                            If .SCFlg > 1 Then
                                If Is引張側照査対象(.Outou_M) Then
                                    Return ca.応答値.震度(i)
                                Else
                                    ' 対象と反対側が降伏したら 探査対象から除外し続ける
                                    members.Remove(MNo)
                                    If members.Count = 0 Then Return -1
                                    Exit For
                                End If
                            End If
                        End With
                    Next
                Next i

            End With
            Return -1
        Catch ex As Exception
            If FormSettings.Option_大量解析プログラム = 0 Then
                Throw New Exception("Get降伏震度でエラーが発生しました" + vbCrLf + ex.Message)
            Else
                '改行しない
                Throw New Exception("Get降伏震度でエラーが発生しました:" + ex.Message)
            End If
        End Try
    End Function

#End Region

#Region "NG部材を登録する"

    Private Sub SetNG部材(mNo As Integer)
        For Each NG In NG部材List
            If NG = mNo Then
                Return
            End If
        Next
        NG部材List.Add(mNo)
    End Sub

#End Region

End Class

Public Class cls断面照査

    Public Const OK As String = "OK"
    Public Const NG As String = "NG"

    Public 破壊形態の照査 As New cls破壊形態の照査
    Public せん断破壊の照査 As New clsせん断破壊の照査
    Public ReadOnly Property 破壊形態の推定検討結果 As String
        Get
            Dim result As String = OK
            If 破壊形態の照査.安全率 >= 1 Then
                result = NG
            End If
            If 破壊形態の照査.jadge1 < 1 Then
                result = OK
                If せん断破壊の照査.Gami_Vdmax_Vyd >= 1 Then
                    result = NG
                ElseIf せん断破壊の照査.Gami_Mdmax_Myd >= 1 Then
                    result = NG
                End If
            End If
            Return result
        End Get
    End Property

    Public 損傷レベル As New cls損傷レベル
    Public ReadOnly Property 損傷レベルの照査結果 As String
        Get
            If 損傷レベル.Limit <= 0 Then Return OK
            If 損傷レベル.Limit >= 損傷レベル.IsLevel Then
                Return OK
            Else
                Return NG
            End If
        End Get
    End Property

    Public L1地震動 As New clsL1地震動

    Public Function 総合的な照査結果() As String
        Dim result As String = OK
        If 破壊形態の推定検討結果 = NG Then result = NG
        If 損傷レベルの照査結果 = NG Then result = NG
        Return result
    End Function

End Class

Public Class cls破壊形態の照査
    Public 決定ケース As String
    Public 決定ステップ As String

    Public 最大曲げモーメントMdmax As Double
    Public 設計曲げ耐力Mm As Double
    Public Mdmax_Mm As Double
    Public jadge1 As Integer

    Public ReadOnly Property MdmaxがMmに達しているか否かの判定 As String
        Get
            Select Case jadge1
                Case 0
                    Return "Mmに達していない"
                Case 1
                    Return "Mmに達している"
                Case Else
                    Return ""
            End Select
        End Get
    End Property

    Public せん断スパンLa As Double
    Public isθ As Boolean
    Public 最大設計せん断力Vdmax As Double
    Public Vdmax時の曲げモーメントMd As Double
    Public Vdmax時の設計軸力Nd As Double
    Public 設計せん断耐力Vud As Double
    Public α As Double
    Public 安全率 As Double
    Public 備考 As String = ""

    Public ReadOnly Property 安全率判定 As String
        Get
            If 安全率 >= 1 Then
                Return "S破壊ﾓｰﾄﾞ"
            Else
                If Me.isθ = True Then
                    Return "M破壊ﾓｰﾄﾞ"
                Else
                    Return "M破壊ﾓｰﾄﾞに準じる"
                End If
            End If
        End Get
    End Property

End Class

Public Class clsせん断破壊の照査
    Public 決定ケースM As String
    Public 決定ステップM As String
    Public 決定ケースV As String
    Public 決定ステップV As String

    Public 最大曲げモーメントMdmax As Double
    Public 応答軸力N As Double
    Public 設計曲げ降伏耐力Myd As Double
    Public γi1 As Double = 1
    Public Gami_Mdmax_Myd As Double
    Public jadge1 As Integer

    Public ReadOnly Property MdmaxがMydに達しているか否かの判定 As String
        Get
            Select Case jadge1
                Case 0
                    Return "降伏以内"
                Case 1
                    Return "降伏超過"
                Case Else
                    If Gami_Mdmax_Myd < 1 Then
                        Return "降伏以内"
                    Else
                        Return "降伏超過"
                    End If
            End Select
        End Get
    End Property

    Public 最大設計せん断力Vdmax As Double
    Public 設計せん断耐力Vud As Double
    Public γi2 As Double = 1
    Public Gami_Vdmax_Vyd As Double = 0
    Public jadge2 As Integer
    Public 備考 As String = ""

    Public ReadOnly Property せん断耐力の照査 As String
        Get
            Select Case jadge2
                Case 0
                    Return "OK"
                Case 1
                    Return "NG"
                Case Else
                    Return ""
            End Select
        End Get
    End Property

    Public Sub Copy曲げ照査(曲げ照査 As clsせん断破壊の照査)
        With 曲げ照査
            Me.決定ケースM = .決定ケースM
            Me.決定ステップM = .決定ステップM
            Me.最大曲げモーメントMdmax = .最大曲げモーメントMdmax
            Me.応答軸力N = .応答軸力N
            Me.設計曲げ降伏耐力Myd = .設計曲げ降伏耐力Myd
            Me.γi1 = .γi1
            Me.Gami_Mdmax_Myd = .Gami_Mdmax_Myd
            Me.jadge1 = .jadge1
        End With
    End Sub
    Public Sub Copyせん断照査(せん断照査 As clsせん断破壊の照査)
        With せん断照査
            Me.決定ケースV = .決定ケースV
            Me.決定ステップV = .決定ステップV
            Me.最大設計せん断力Vdmax = .最大設計せん断力Vdmax
            Me.設計せん断耐力Vud = .設計せん断耐力Vud
            Me.γi2 = .γi2
            Me.Gami_Vdmax_Vyd = .Gami_Vdmax_Vyd
            Me.jadge2 = .jadge2
            Me.備考 = .備考
        End With
    End Sub

End Class

Public Class cls損傷レベル
    Public 決定ケース As String
    Public 決定ステップ As String

    Public Limit As Integer
    Public φr As Double = Double.NaN
    Public Nd As Double
    Public φi() As Double = New Double(0 To 2) {1, 1, 1}
    Public γi As Double
    Public 備考 As String() = New String(3) {"", "", "", ""}

    Public ReadOnly Property 安全度 As Double
        Get
            Try
                Return 安全率(Limit - 1)
            Catch ex As Exception
                Return 0
            End Try
        End Get
    End Property

    Public ReadOnly Property 安全率(index As Integer) As Double
        Get
            Try
                If φi.Count <= index Then
                    Return 0.0001
                End If
                If IsError = False Then
                    Return γi * φr / φi(index)
                Else
                    Throw New Exception("部材曲率の取得に失敗しました。")
                End If
            Catch ex As Exception
                Throw ex
            End Try

        End Get
    End Property

    Public ReadOnly Property IsError As Boolean
        Get
            Dim errFlg As Boolean = False
            Select Case Limit
                Case 1
                    If φi(0) = 0 Then
                        errFlg = True
                    End If
                Case 2
                    If φi(1) = 0 Then
                        errFlg = True
                    End If
                Case 3
                    If φi(2) = 0 Then
                        errFlg = True
                    End If
            End Select
            Return errFlg
        End Get
    End Property
    Public ReadOnly Property IsLevel As Integer
        Get
            If 安全率(2) > 1 Then
                Return 4
            ElseIf 安全率(1) > 1 Then
                Return 3
            ElseIf 安全率(0) > 1 Then
                Return 2
            Else
                Return 1
            End If
        End Get
    End Property


End Class

Public Class clsL1地震動
    Public 決定ケース As Integer

    Public 降伏震度 As Double
    Public L1設計震度 As Double
    Public ReadOnly Property 安全度 As Double
        Get
            If 降伏震度 = 0 Then
                Return 0
            Else
                Return L1設計震度 / 降伏震度
            End If
        End Get
    End Property

    Public ReadOnly Property 判定 As String
        Get
            If 安全度 > 1 Then
                Return "NG"
            Else
                Return "1 / 1"
            End If
        End Get
    End Property

End Class