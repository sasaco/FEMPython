Public Class CalculationFoundation

#Region "メンバ変数"

    '入力データ -----------------------------------------------------
    ''' <summary>基礎のタイプ</summary>
    ''' <remarks>0=杭基礎, 1=直接基礎</remarks>
    Public _FoundationType As Integer '0=杭基礎, 1=直接基礎
    Private _CaseDB As List(Of CalculationCase)

    Public 変位照査節点番号List As List(Of Integer)
    Public 変位照査距離List As List(Of Double)

    Public 反力照査要素番号List As List(Of Integer)
    Public 反力照査部材数List As List(Of Double)

    Public 杭基礎制限値(0 To 30) As Double
    Public 直接基礎制限値(0 To 10) As Double

    '計算結果データ -----------------------------------------------------
    Private _安定計算結果 As cls安定計算

    Private Function Is安定計算(ca As CalculationCase) As Boolean
        If ca._解析対象(6) = False Then Return False
        Return True
    End Function

#End Region

#Region "初期設定"

    Sub New(基礎のタイプ As Integer, CaseDB As List(Of CalculationCase))
        '入力データ
        _CaseDB = CaseDB
        _FoundationType = 基礎のタイプ
        変位照査節点番号List = New List(Of Integer)
        変位照査距離List = New List(Of Double)
        Select Case _FoundationType
            Case 0
                反力照査要素番号List = New List(Of Integer)
                反力照査部材数List = New List(Of Double)
                For Each l In 杭基礎制限値
                    l = -1
                Next
            Case 1
                For Each l In 直接基礎制限値
                    l = -1
                Next
        End Select
        '解析結果データ
        _安定計算結果 = Nothing
    End Sub

#End Region

#Region "安定計算を行う"

    Public ReadOnly Property 安定計算結果 As cls安定計算
        Get
            If _安定計算結果 Is Nothing Then
                _安定計算結果 = New cls安定計算
                Select Case _FoundationType
                    Case 0
                        Call 杭基礎安定計算(_安定計算結果)
                    Case 1
                        Call 直接基礎安定計算(_安定計算結果)
                End Select
            End If
            Return _安定計算結果
        End Get
    End Property


    Private Sub 杭基礎安定計算(ByRef result As cls安定計算)

        Try
            Dim tmp復旧性1 As New cls杭基礎Type1
            Dim tmp復旧性2 As New cls杭基礎Type2
            Dim tmp安全性 As New cls杭基礎Type2

            '反力に関するもの ------------------------------------------------------------------------------
            For k = 0 To Me.反力照査要素番号List.Count - 1
                Dim mNo As Integer = Me.反力照査要素番号List(k)
                Dim ALF As Double = Me.反力照査部材数List(k)
                Dim tmp復旧性1鉛直支持 As New ValueType1
                Dim tmp復旧性1引抜き力 As New ValueType1
                Dim tmp復旧性2鉛直支持 As New ValueType1
                Dim tmp安全性_鉛直支持 As New ValueType1

                For Each ca In Me._CaseDB
                    If mNo < 0 Then Exit For
                    If Is安定計算(ca) = True Then
                        If ca._解析対象(5) = True Then
                            '1.復旧性（性能レベル１）
                            For istep = 0 To ca.応答値.復旧性_L1震度step
                                '(1)設計鉛直支持力の照査
                                Dim tmp1 = 杭基礎L1鉛直支持力の検討(ca, istep, mNo, ALF)
                                If tmp1.安全度 > tmp復旧性1鉛直支持.安全度 Then
                                    tmp復旧性1鉛直支持 = tmp1
                                End If
                                '(2)設計引抜き抵抗力の照査
                                Dim tmp2 = 杭基礎L1引抜き抵抗力の照査(ca, istep, mNo, ALF)
                                If tmp2.安全度 > tmp復旧性1引抜き力.安全度 Then
                                    tmp復旧性1引抜き力 = tmp2
                                End If
                            Next
                        End If
                        If ca._解析対象(0) = True Then
                            '2.復旧性（性能レベル2））
                            For istep = 0 To ca.応答値.復旧性_最大応答step
                                '(1)設計鉛直支持力の照査結果
                                Dim tmp1 = 杭基礎復旧性2鉛直支持の照査(ca, istep, mNo, ALF)
                                If tmp1.安全度 > tmp復旧性2鉛直支持.安全度 Then
                                    tmp復旧性2鉛直支持 = tmp1
                                End If
                            Next
                        End If
                        If ca._解析対象(1) = True Then
                            '3.安全性
                            For istep = 0 To ca.応答値.安全性_最大応答step
                                '(1)設計鉛直支持力の照査結果
                                Dim tmp2 = 杭基礎安全性鉛直支持の照査(ca, istep, mNo, ALF)
                                If tmp2.安全度 > tmp安全性_鉛直支持.安全度 Then
                                    tmp安全性_鉛直支持 = tmp2
                                End If
                            Next
                        End If 'Is安定計算(ca) = True Then
                    End If
                Next 'For Each ca In Me._CaseDB
                tmp復旧性1.設計鉛直支持力の照査.Add(tmp復旧性1鉛直支持)
                tmp復旧性1.設計引抜き抵抗力の照査.Add(tmp復旧性1引抜き力)
                tmp復旧性2.設計鉛直支持力の照査.Add(tmp復旧性2鉛直支持)
                tmp安全性.設計鉛直支持力の照査.Add(tmp安全性_鉛直支持)
            Next ' For Each mNo In Me.反力照査要素番号List



            '変位に関するもの ------------------------------------------------------------------------------
            For k = 0 To Me.変位照査節点番号List.Count - 1
                Dim pNo As Integer = Me.変位照査節点番号List(k)
                Dim _Direction As Double = Me.変位照査距離List(k)
                Dim tmp復旧性1水平変位 As New ValueType2
                Dim tmp復旧性1回転照査 As New ValueType2
                Dim tmp復旧性2水平変位 As New ValueType2
                Dim tmp復旧性2回転照査 As New ValueType2
                Dim tmp安全性_水平変位 As New ValueType2
                Dim tmp安全性_回転照査 As New ValueType2

                For Each ca In Me._CaseDB
                    If pNo < 0 Then Exit For
                    If Is安定計算(ca) = True Then
                        If ca._解析対象(5) = True Then
                            '1.復旧性（性能レベル1）
                            For istep = 0 To ca.応答値.復旧性_L1震度step
                                '　(3)水平変位の照査結果
                                Dim tmp1 = 杭基礎L1水平変位の照査(ca, istep, pNo, _Direction)
                                If tmp1.安全度 > tmp復旧性1水平変位.安全度 Then
                                    tmp復旧性1水平変位 = tmp1
                                End If
                                '　(4)回転角の照査結果
                                Dim tmp2 = 杭基礎L1回転角の照査(ca, istep, pNo)
                                If tmp2.安全度 > tmp復旧性1回転照査.安全度 Then
                                    tmp復旧性1回転照査 = tmp2
                                End If
                            Next
                        End If
                        If ca._解析対象(0) = True Then
                            '2.復旧性（性能レベル2）
                            For istep = 0 To ca.応答値.復旧性_最大応答step
                                '　(3)水平変位の照査結果
                                Dim tmp1 = 杭基礎復旧性2水平変位の照査(ca, istep, pNo, _Direction)
                                If tmp1.安全度 > tmp復旧性2水平変位.安全度 Then
                                    tmp復旧性2水平変位 = tmp1
                                End If
                                '　(4)回転角の照査結果
                                Dim tmp2 = 杭基礎復旧性2回転角の照査(ca, istep, pNo)
                                If tmp2.安全度 > tmp復旧性2回転照査.安全度 Then
                                    tmp復旧性2回転照査 = tmp2
                                End If
                            Next
                        End If
                        If ca._解析対象(1) = True Then
                            '3.安全性
                            For istep = 0 To ca.応答値.安全性_最大応答step
                                '　(3)水平変位の照査結果
                                Dim tmp1 = 杭基礎安全性水平変位の照査(ca, istep, pNo, _Direction)
                                If tmp1.安全度 > tmp安全性_水平変位.安全度 Then
                                    tmp安全性_水平変位 = tmp1
                                End If
                                '　(4)回転角の照査結果
                                Dim tmp2 = 杭基礎安全性回転角の照査(ca, istep, pNo)
                                If tmp2.安全度 > tmp安全性_回転照査.安全度 Then
                                    tmp安全性_回転照査 = tmp2
                                End If
                            Next
                        End If
                    End If 'Is安定計算(ca) = True Then
                Next 'For Each ca In Me._CaseDB
                tmp復旧性1.水平変位の照査.Add(tmp復旧性1水平変位)
                tmp復旧性1.回転角の照査.Add(tmp復旧性1回転照査)
                tmp復旧性2.水平変位の照査.Add(tmp復旧性2水平変位)
                tmp復旧性2.回転角の照査.Add(tmp復旧性2回転照査)
                tmp安全性.水平変位の照査.Add(tmp安全性_水平変位)
                tmp安全性.回転角の照査.Add(tmp安全性_回転照査)
            Next ' For Each mNo In Me.変位照査節点番号List

            '計算結果を格納 ------------------------------------------------------------------------------
            With result.杭基礎
                .復旧性性能レベル1 = tmp復旧性1
                .復旧性性能レベル2 = tmp復旧性2
                .安全性 = tmp安全性
            End With
        Catch ex As Exception
            If FormSettings.Option_大量解析プログラム = 0 Then
                Throw ex
            Else
                result.杭基礎.ErrorMessage = ex.Message
            End If
        End Try
    End Sub

    Public Function 杭基礎L1鉛直支持力の検討(ca As CalculationCase, istep As Integer, mNo As Integer, ALF As Double) As ValueType1
        Dim tmpValue As New ValueType1
        With tmpValue
            .決定ケース = ca.DataID
            .決定ステップ = istep
            .着目番号 = mNo
            .構造解析係数γa = 1
            .応答値 = -ca._SNAPDB.OutputInfo.StepCtrl(istep).MemberItem(mNo).Ni / ALF
            If ca._Is液状化L1ケース Then
                .限界値 = Math.Abs(GetLimit(Me.杭基礎制限値(10), Me.杭基礎制限値(0))) * ca._SNAPDB.Getαf
            ElseIf ca._Is液状化L21ケース Then
                .限界値 = Math.Abs(GetLimit(Me.杭基礎制限値(17), Me.杭基礎制限値(0))) * ca._SNAPDB.Getαf
            ElseIf ca._Is液状化L22ケース Then
                .限界値 = Math.Abs(GetLimit(Me.杭基礎制限値(24), Me.杭基礎制限値(0))) * ca._SNAPDB.Getαf
            Else
                .限界値 = Math.Abs(GetLimit(Me.杭基礎制限値(0))) * ca._SNAPDB.Getαf
            End If
            .構造物係数γi = 1
        End With
        Return tmpValue
    End Function
    Public Function 杭基礎L1引抜き抵抗力の照査(ca As CalculationCase, istep As Integer, mNo As Integer, ALF As Double) As ValueType1
        Dim tmpValue As New ValueType1
        With tmpValue
            .決定ケース = ca.DataID
            .決定ステップ = istep
            .着目番号 = mNo
            .構造解析係数γa = 1
            .応答値 = ca._SNAPDB.OutputInfo.StepCtrl(istep).MemberItem(mNo).Ni / ALF
            Dim Is液状化 = False
            If ca._Is液状化L1ケース Then
                .限界値 = Math.Abs(GetLimit(Me.杭基礎制限値(11), Me.杭基礎制限値(1))) * ca._SNAPDB.Getαf
            ElseIf ca._Is液状化L21ケース Then
                .限界値 = Math.Abs(GetLimit(Me.杭基礎制限値(18), Me.杭基礎制限値(1))) * ca._SNAPDB.Getαf
            ElseIf ca._Is液状化L22ケース Then
                .限界値 = Math.Abs(GetLimit(Me.杭基礎制限値(25), Me.杭基礎制限値(1))) * ca._SNAPDB.Getαf
            Else
                .限界値 = Math.Abs(GetLimit(Me.杭基礎制限値(1))) * ca._SNAPDB.Getαf
            End If
            .構造物係数γi = 1
        End With
        Return tmpValue
    End Function
    Public Function 杭基礎L1水平変位の照査(ca As CalculationCase, istep As Integer, pNo As Integer, 変位照査距離 As Double) As ValueType2
        Dim tmpValue As New ValueType2

        With tmpValue
            .決定ケース = ca.DataID
            .決定ステップ = istep
            .着目番号 = pNo
            Dim 水平変位 As Double = Math.Abs(ca._SNAPDB.OutputInfo.StepCtrl(istep).JointItem(pNo).DeltaX) * 1000
            '応答変位法等で地盤の変位が入力されていた場合、地盤の変位分は安定照査上の変位から除外する。
            水平変位 -= Math.Abs(ca._SNAPDB.OutputInfo.StepCtrl(istep).JointItem(pNo).DeltaG) * 1000

            Dim 回転角 As Double = Math.Abs(ca._SNAPDB.OutputInfo.StepCtrl(istep).JointItem(pNo).DeltaT)
            .応答値 = 水平変位 + 回転角 * 変位照査距離
            If ca._Is液状化L1ケース Then
                .限界値 = Math.Abs(GetLimit(Me.杭基礎制限値(12), Me.杭基礎制限値(2)))
            ElseIf ca._Is液状化L21ケース Then
                .限界値 = Math.Abs(GetLimit(Me.杭基礎制限値(19), Me.杭基礎制限値(2)))
            ElseIf ca._Is液状化L22ケース Then
                .限界値 = Math.Abs(GetLimit(Me.杭基礎制限値(26), Me.杭基礎制限値(2)))
            Else
                .限界値 = Math.Abs(GetLimit(Me.杭基礎制限値(2)))
            End If
        End With
        Return tmpValue
    End Function
    Public Function 杭基礎L1回転角の照査(ca As CalculationCase, istep As Integer, pNo As Integer) As ValueType2
        Dim tmpValue As New ValueType2

        With tmpValue
            .決定ケース = ca.DataID
            .決定ステップ = istep
            .着目番号 = pNo
            .応答値 = Math.Abs(ca._SNAPDB.OutputInfo.StepCtrl(istep).JointItem(pNo).DeltaT) * 1000
            If ca._Is液状化L1ケース Then
                .限界値 = Math.Abs(GetLimit(Me.杭基礎制限値(13), Me.杭基礎制限値(3)))
            ElseIf ca._Is液状化L21ケース Then
                .限界値 = Math.Abs(GetLimit(Me.杭基礎制限値(20), Me.杭基礎制限値(3)))
            ElseIf ca._Is液状化L22ケース Then
                .限界値 = Math.Abs(GetLimit(Me.杭基礎制限値(27), Me.杭基礎制限値(3)))
            Else
                .限界値 = Math.Abs(GetLimit(Me.杭基礎制限値(3)))
            End If
        End With
        Return tmpValue
    End Function
    Public Function 杭基礎復旧性2鉛直支持の照査(ca As CalculationCase, istep As Integer, mNo As Integer, ALF As Double) As ValueType1
        Dim tmpValue As New ValueType1
        With tmpValue
            .決定ケース = ca.DataID
            .決定ステップ = istep
            .着目番号 = mNo
            .構造解析係数γa = 1
            .応答値 = -ca._SNAPDB.OutputInfo.StepCtrl(istep).MemberItem(mNo).Ni / ALF
            If ca._Is液状化L21ケース = True Or ca._Is液状化L22ケース = True Then
                .限界値 = Math.Abs(GetLimit(Me.杭基礎制限値(14), Me.杭基礎制限値(4))) * ca._SNAPDB.Getαf
            Else
                .限界値 = Math.Abs(GetLimit(Me.杭基礎制限値(4))) * ca._SNAPDB.Getαf
            End If
            .構造物係数γi = 1
        End With
        Return tmpValue
    End Function
    Public Function 杭基礎安全性鉛直支持の照査(ca As CalculationCase, istep As Integer, mNo As Integer, ALF As Double) As ValueType1
        Dim tmpValue As New ValueType1
        With tmpValue
            .決定ケース = ca.DataID
            .決定ステップ = istep
            .着目番号 = mNo
            .構造解析係数γa = 1
            .応答値 = -ca._SNAPDB.OutputInfo.StepCtrl(istep).MemberItem(mNo).Ni / ALF
            If ca._Is液状化L21ケース Then
                .限界値 = Math.Abs(GetLimit(Me.杭基礎制限値(21), Me.杭基礎制限値(7))) * ca._SNAPDB.Getαf
            ElseIf ca._Is液状化L22ケース Then
                .限界値 = Math.Abs(GetLimit(Me.杭基礎制限値(28), Me.杭基礎制限値(7))) * ca._SNAPDB.Getαf
            Else
                .限界値 = Math.Abs(GetLimit(Me.杭基礎制限値(7))) * ca._SNAPDB.Getαf
            End If
            .構造物係数γi = 1
        End With
        Return tmpValue
    End Function
    Public Function 杭基礎復旧性2水平変位の照査(ca As CalculationCase, istep As Integer, pNo As Integer, 変位照査距離 As Double) As ValueType2
        Dim tmpValue As New ValueType2
        With tmpValue
            .決定ケース = ca.DataID
            .決定ステップ = istep
            .着目番号 = pNo
            Dim 水平変位 As Double = Math.Abs(ca._SNAPDB.OutputInfo.StepCtrl(istep).JointItem(pNo).DeltaX) * 1000
            '応答変位法等で地盤の変位が入力されていた場合、地盤の変位分は安定照査上の変位から除外する。
            水平変位 -= Math.Abs(ca._SNAPDB.OutputInfo.StepCtrl(istep).JointItem(pNo).DeltaG) * 1000
            Dim 回転角 As Double = Math.Abs(ca._SNAPDB.OutputInfo.StepCtrl(istep).JointItem(pNo).DeltaT)
            .応答値 = 水平変位 + 回転角 * 変位照査距離
            If ca._Is液状化L21ケース = True Or ca._Is液状化L22ケース = True Then
                .限界値 = Math.Abs(GetLimit(Me.杭基礎制限値(15), Me.杭基礎制限値(5)))
            Else
                .限界値 = Math.Abs(GetLimit(Me.杭基礎制限値(5)))
            End If
        End With
        Return tmpValue
    End Function
    Public Function 杭基礎復旧性2回転角の照査(ca As CalculationCase, istep As Integer, pNo As Integer) As ValueType2
        Dim tmpValue As New ValueType2
        With tmpValue
            .決定ケース = ca.DataID
            .決定ステップ = istep
            .着目番号 = pNo
            .応答値 = Math.Abs(ca._SNAPDB.OutputInfo.StepCtrl(istep).JointItem(pNo).DeltaT) * 1000
            If ca._Is液状化L21ケース = True Or ca._Is液状化L22ケース = True Then
                .限界値 = Math.Abs(GetLimit(Me.杭基礎制限値(16), Me.杭基礎制限値(6)))
            Else
                .限界値 = Math.Abs(GetLimit(Me.杭基礎制限値(6)))
            End If
        End With
        Return tmpValue
    End Function
    Public Function 杭基礎安全性水平変位の照査(ca As CalculationCase, istep As Integer, pNo As Integer, 変位照査距離 As Double) As ValueType2
        Dim tmpValue As New ValueType2
        With tmpValue
            .決定ケース = ca.DataID
            .決定ステップ = istep
            .着目番号 = pNo
            Dim 水平変位 As Double = Math.Abs(ca._SNAPDB.OutputInfo.StepCtrl(istep).JointItem(pNo).DeltaX) * 1000
            '応答変位法等で地盤の変位が入力されていた場合、地盤の変位分は安定照査上の変位から除外する。
            水平変位 -= Math.Abs(ca._SNAPDB.OutputInfo.StepCtrl(istep).JointItem(pNo).DeltaG) * 1000
            Dim 回転角 As Double = Math.Abs(ca._SNAPDB.OutputInfo.StepCtrl(istep).JointItem(pNo).DeltaT)
            .応答値 = 水平変位 + 回転角 * 変位照査距離
            If ca._Is液状化L21ケース Then
                .限界値 = Math.Abs(GetLimit(Me.杭基礎制限値(22), Me.杭基礎制限値(8)))
            ElseIf ca._Is液状化L22ケース Then
                .限界値 = Math.Abs(GetLimit(Me.杭基礎制限値(29), Me.杭基礎制限値(8)))
            Else
                .限界値 = Math.Abs(GetLimit(Me.杭基礎制限値(8)))
            End If
        End With
        Return tmpValue
    End Function
    Public Function 杭基礎安全性回転角の照査(ca As CalculationCase, istep As Integer, pNo As Integer) As ValueType2
        Dim tmpValue As New ValueType2
        With tmpValue
            .決定ケース = ca.DataID
            .決定ステップ = istep
            .着目番号 = pNo
            .応答値 = Math.Abs(ca._SNAPDB.OutputInfo.StepCtrl(istep).JointItem(pNo).DeltaT) * 1000
            If ca._Is液状化L21ケース Then
                .限界値 = Math.Abs(GetLimit(Me.杭基礎制限値(23), Me.杭基礎制限値(9)))
            ElseIf ca._Is液状化L22ケース Then
                .限界値 = Math.Abs(GetLimit(Me.杭基礎制限値(30), Me.杭基礎制限値(9)))
            Else
                .限界値 = Math.Abs(GetLimit(Me.杭基礎制限値(9)))
            End If
        End With
        Return tmpValue
    End Function


    Private Sub 直接基礎安定計算(ByRef result As cls安定計算)
        Try
            Dim tmp復旧性1 As New cls直接基礎Type1
            Dim tmp復旧性2 As New cls直接基礎Type2
            Dim tmp安全性 As New cls直接基礎Type2

            Dim pNo As Integer = 変位照査節点番号List.First

            For Each ca In Me._CaseDB
                If Is安定計算(ca) = True Then

                    Dim 初期変位 As Double = ca._SNAPDB.OutputInfo.StepCtrl(0).JointItem(pNo).DeltaX
                    Dim 初期回転 As Double = ca._SNAPDB.OutputInfo.StepCtrl(0).JointItem(pNo).DeltaT

                    Dim L1降伏Step As Integer = ca._SNAPDB.maxStep

                    '1.復旧性（性能レベル1）
                    For istep = ca._SNAPDB.maxStep To 0 Step -1
                        '(1)設計鉛直支持力の照査結果
                        Dim tmp1 As ValueType1 = 直接基礎L1鉛直支持力の検討(ca, istep, pNo)
                        '(2)設計水平支持力の照査結果
                        Dim tmp2 As ValueType1 = 直接基礎L1水平支持力の検討(ca, istep, pNo)
                        '(3)残留傾斜の照査結果
                        Dim tmp3 As ValueType1 = 直接基礎L1残留傾斜の検討(ca, istep, pNo)

                        If Math.Max(tmp1.安全度, Math.Max(tmp2.安全度, tmp3.安全度)) > 1 Then
                            L1降伏Step = Math.Min(L1降伏Step, istep)
                        End If

                        If ca._解析対象(5) = True Then
                            If istep <= ca.応答値.復旧性_L1震度step Then
                                '(1)設計鉛直支持力の照査結果
                                If tmp1.安全度 >= tmp復旧性1.設計鉛直支持力の照査.安全度 Then
                                    tmp復旧性1.設計鉛直支持力の照査 = tmp1
                                End If
                                If tmp2.安全度 >= tmp復旧性1.設計水平支持力の照査.安全度 Then
                                    tmp復旧性1.設計水平支持力の照査 = tmp2
                                End If
                                If tmp3.安全度 >= tmp復旧性1.残留傾斜の照査.安全度 Then
                                    tmp復旧性1.残留傾斜の照査 = tmp3
                                End If
                            End If
                        End If
                    Next

                    If ca._解析対象(0) = True Then
                        '2.復旧性（性能レベル2）
                        For istep = Math.Min(ca.応答値.復旧性_最大応答step, ca._SNAPDB.maxStep) To 0 Step -1
                            Try
                                '底面塑性化率の照査は降伏していることが前提
                                If L1降伏Step <= istep Then
                                    '(1)底面塑性化率の照査結果
                                    Dim tmp1 As ValueType2 = 直接基礎復旧性2底面塑性化率の検討(ca, istep, pNo)
                                    If tmp1.安全度 >= tmp復旧性2.底面塑性化率の照査.安全度 Then
                                        tmp復旧性2.底面塑性化率の照査 = tmp1
                                    End If
                                    '(3)回転角の照査結果
                                    Dim tmp3 As ValueType2 = 直接基礎復旧性2回転角の検討(ca, istep, pNo)
                                    If tmp3.安全度 >= tmp復旧性2.回転角の照査.安全度 Then
                                        tmp復旧性2.回転角の照査 = tmp3
                                    End If
                                End If
                                '(2)設計水平支持力の照査結果
                                Dim tmp2 As ValueType1 = 直接基礎復旧性2水平支持力の検討(ca, istep, pNo)
                                If tmp2.安全度 >= tmp復旧性2.設計水平支持力の照査.安全度 Then
                                    tmp復旧性2.設計水平支持力の照査 = tmp2
                                End If
                            Catch ex As Exception
                                If FormSettings.Option_大量解析プログラム = 0 Then
                                    Throw ex
                                Else
                                    result.直接基礎.ErrorMessage = ex.Message
                                End If
                            End Try
                        Next 'istep
                    End If
                    If ca._解析対象(1) = True Then
                        '3.安全性
                        Try
                            For istep = Math.Min(ca.応答値.安全性_最大応答step, ca._SNAPDB.maxStep) To 0 Step -1
                                '底面塑性化率の照査は降伏していることが前提
                                If L1降伏Step <= istep Then
                                    '(1)底面塑性化率の照査結果
                                    Dim tmp1 As ValueType2 = 直接基礎安全性底面塑性化率の検討(ca, istep, pNo)
                                    If tmp1.安全度 >= tmp安全性.底面塑性化率の照査.安全度 Then
                                        tmp安全性.底面塑性化率の照査 = tmp1
                                    End If
                                    '(3)回転角の照査結果
                                    Dim tmp3 As ValueType2 = 直接基礎安全性回転角の検討(ca, istep, pNo)
                                    If tmp3.安全度 >= tmp安全性.回転角の照査.安全度 Then
                                        tmp安全性.回転角の照査 = tmp3
                                    End If
                                End If
                                '(2)設計水平支持力の照査結果
                                Dim tmp2 As ValueType1 = 直接基礎安全性水平支持力の検討(ca, istep, pNo)
                                If tmp2.安全度 >= tmp安全性.設計水平支持力の照査.安全度 Then
                                    tmp安全性.設計水平支持力の照査 = tmp2
                                End If
                            Next 'istep
                        Catch ex As Exception
                            If FormSettings.Option_大量解析プログラム = 0 Then
                                Throw ex
                            Else
                                result.直接基礎.ErrorMessage = ex.Message
                            End If
                        End Try
                    End If
                End If
            Next 'ca

            '計算結果を格納 ------------------------------------------------------------------------------
            With result.直接基礎
                .復旧性性能レベル1 = tmp復旧性1
                .復旧性性能レベル2 = tmp復旧性2
                .安全性 = tmp安全性
            End With
        Catch ex As Exception
            If FormSettings.Option_大量解析プログラム = 0 Then
                Throw ex
            Else
                result.直接基礎.ErrorMessage = ex.Message
            End If
        End Try
    End Sub

    Public Function 直接基礎L1鉛直支持力の検討(ca As CalculationCase, istep As Integer, pNo As Integer) As ValueType1
        Dim tmp1 As New ValueType1
        With tmp1
            .決定ケース = ca.DataID
            .決定ステップ = istep
            .着目番号 = pNo
            .構造解析係数γa = 1
            .応答値 = ca._SNAPDB.ChokukisoVd(istep, pNo, 1)
            .限界値 = Math.Abs(GetLimit(Me.直接基礎制限値(0), ca._SNAPDB.ChokukisoRvd(istep, pNo, 1, ca._SNAPDB.InputInfo.KihonInfo.KisoType))) * ca._SNAPDB.Getαf
            .構造物係数γi = 1
        End With
        Return tmp1
    End Function
    Public Function 直接基礎L1水平支持力の検討(ca As CalculationCase, istep As Integer, pNo As Integer) As ValueType1
        Dim tmp2 As New ValueType1
        With tmp2
            .決定ケース = ca.DataID
            .決定ステップ = istep
            .着目番号 = pNo
            .構造解析係数γa = 1
            .応答値 = Math.Abs(ca._SNAPDB.ChokukisoHd(istep, pNo, 1))
            .限界値 = Math.Abs(GetLimit(Me.直接基礎制限値(1), ca._SNAPDB.ChokukisoRhd(istep, pNo, 1))) * ca._SNAPDB.Getαf
            .構造物係数γi = 1
        End With
        Return tmp2
    End Function
    Public Function 直接基礎L1残留傾斜の検討(ca As CalculationCase, istep As Integer, pNo As Integer) As ValueType1
        Dim tmp3 As New ValueType1
        With tmp3
            .決定ケース = ca.DataID
            .決定ステップ = istep
            .着目番号 = pNo
            .構造解析係数γa = 1
            .応答値 = Math.Abs(ca._SNAPDB.ChokukisoMd(istep, pNo))
            .限界値 = Math.Abs(GetLimit(Me.直接基礎制限値(2), ca._SNAPDB.ChokukisoMmd(istep, pNo, 1))) * ca._SNAPDB.Getαf
            .構造物係数γi = 1
        End With
        Return tmp3
    End Function
    Public Function 直接基礎復旧性2底面塑性化率の検討(ca As CalculationCase, istep As Integer, pNo As Integer) As ValueType2
        Dim tmp1 As New ValueType2
        With tmp1
            .決定ケース = ca.DataID
            .決定ステップ = istep
            .着目番号 = pNo
            .応答値 = ca._SNAPDB.ChokukisoXf_B(istep, pNo, 2)
            .限界値 = Math.Abs(GetLimit(Me.直接基礎制限値(4), ca._SNAPDB.ChokukisoXFLd(istep, pNo, 2)))
        End With
        Return tmp1
    End Function
    Public Function 直接基礎復旧性2水平支持力の検討(ca As CalculationCase, istep As Integer, pNo As Integer) As ValueType1
        Dim tmp2 As New ValueType1
        With tmp2
            .決定ケース = ca.DataID
            .決定ステップ = istep
            .着目番号 = pNo
            .構造解析係数γa = 1
            .応答値 = Math.Abs(ca._SNAPDB.ChokukisoHd(istep, pNo, 2))
            .限界値 = Math.Abs(GetLimit(Me.直接基礎制限値(5), ca._SNAPDB.ChokukisoRhd(istep, pNo, 2))) * ca._SNAPDB.Getαf
            .構造物係数γi = 1
        End With
        Return tmp2
    End Function
    Public Function 直接基礎復旧性2回転角の検討(ca As CalculationCase, istep As Integer, pNo As Integer) As ValueType2
        Dim tmp3 As New ValueType2
        With tmp3
            .決定ケース = ca.DataID
            .決定ステップ = istep
            .着目番号 = pNo
            .応答値 = Math.Abs(ca._SNAPDB.Chokukisoθd(istep, pNo, 2))
            .限界値 = Math.Abs(GetLimit(Me.直接基礎制限値(6), ca._SNAPDB.ChokukisoθL(istep, pNo, 2)))
        End With
        Return tmp3
    End Function
    Public Function 直接基礎安全性底面塑性化率の検討(ca As CalculationCase, istep As Integer, pNo As Integer) As ValueType2
        Dim tmp1 As New ValueType2
        With tmp1
            .決定ケース = ca.DataID
            .決定ステップ = istep
            .着目番号 = pNo
            .応答値 = ca._SNAPDB.ChokukisoXf_B(istep, pNo, 3)
            .限界値 = Math.Abs(GetLimit(Me.直接基礎制限値(8), ca._SNAPDB.ChokukisoXFLd(istep, pNo, 3)))
        End With
        Return tmp1
    End Function
    Public Function 直接基礎安全性水平支持力の検討(ca As CalculationCase, istep As Integer, pNo As Integer) As ValueType1
        Dim tmp2 As New ValueType1
        With tmp2
            .決定ケース = ca.DataID
            .決定ステップ = istep
            .着目番号 = pNo
            .構造解析係数γa = 1
            .応答値 = Math.Abs(ca._SNAPDB.ChokukisoHd(istep, pNo, 3))
            .限界値 = Math.Abs(GetLimit(Me.直接基礎制限値(9), ca._SNAPDB.ChokukisoRhd(istep, pNo, 3))) * ca._SNAPDB.Getαf
            .構造物係数γi = 1
        End With
        Return tmp2
    End Function
    Public Function 直接基礎安全性回転角の検討(ca As CalculationCase, istep As Integer, pNo As Integer) As ValueType2
        Dim tmp3 As New ValueType2
        With tmp3
            .決定ケース = ca.DataID
            .決定ステップ = istep
            .着目番号 = pNo
            .応答値 = Math.Abs(ca._SNAPDB.Chokukisoθd(istep, pNo, 3))
            .限界値 = Math.Abs(GetLimit(Me.直接基礎制限値(10), ca._SNAPDB.ChokukisoθL(istep, pNo, 3)))
        End With
        Return tmp3
    End Function

    Private Function GetLimit(ParamArray values() As Object) As Double

        For Each v In values
            Dim value As Double
            If Double.TryParse(v, value) Then
                If value <> 0 Then
                    Return value
                End If
            End If
        Next

        Return 0

    End Function

#End Region

End Class

Public Class cls安定計算
    Public 杭基礎 As New cls杭基礎安定計算
    Public 直接基礎 As New cls直接基礎安定計算
End Class

Public Class cls杭基礎安定計算
    Public 復旧性性能レベル1 As New cls杭基礎Type1
    Public 復旧性性能レベル2 As New cls杭基礎Type2
    Public 安全性 As New cls杭基礎Type2
    Public ErrorMessage As String = ""
End Class

Public Class cls直接基礎安定計算
    Public 復旧性性能レベル1 As New cls直接基礎Type1
    Public 復旧性性能レベル2 As New cls直接基礎Type2
    Public 安全性 As New cls直接基礎Type2
    Public ErrorMessage As String = ""
End Class

Public Class cls杭基礎Type1
    Public 設計鉛直支持力の照査 As New List(Of ValueType1)
    Public 設計引抜き抵抗力の照査 As New List(Of ValueType1)
    Public 水平変位の照査 As New List(Of ValueType2)
    Public 回転角の照査 As New List(Of ValueType2)
End Class

Public Class cls杭基礎Type2
    Public 設計鉛直支持力の照査 As New List(Of ValueType1)
    Public 水平変位の照査 As New List(Of ValueType2)
    Public 回転角の照査 As New List(Of ValueType2)
End Class

Public Class cls直接基礎Type1
    Public 設計鉛直支持力の照査 As New ValueType1
    Public 設計水平支持力の照査 As New ValueType1
    Public 残留傾斜の照査 As New ValueType1
End Class

Public Class cls直接基礎Type2
    Public 底面塑性化率の照査 As New ValueType2
    Public 設計水平支持力の照査 As New ValueType1
    Public 回転角の照査 As New ValueType2
End Class

Public Class ValueType1
    Public 決定ケース As Integer
    Public 決定ステップ As Integer
    Public 着目番号 As Integer
    Public 構造解析係数γa As Double
    Public 応答値 As Double
    Public 限界値 As Double
    Public 構造物係数γi As Double
    Public ReadOnly Property 安全度 As Double
        Get
            If 限界値 = 0 OrElse Double.IsNaN(限界値) Then
                Return Double.MinValue
            Else
                Return 構造物係数γi * 応答値 / 限界値
            End If
        End Get
    End Property
    Sub New()
        決定ケース = -1
        決定ステップ = -1
        着目番号 = -1
        構造解析係数γa = -1
        応答値 = -1
        限界値 = Double.NaN
        構造物係数γi = -1
    End Sub
End Class

Public Class ValueType2
    Public 決定ケース As Integer
    Public 決定ステップ As Integer
    Public 着目番号 As Integer
    Public 応答値 As Double
    Public 限界値 As Double
    Public ReadOnly Property 安全度 As Double
        Get
            If 限界値 = 0 OrElse Double.IsNaN(限界値) Then
                Return Double.MinValue
            Else
                Return 応答値 / 限界値
            End If
        End Get
    End Property
    Sub New()
        決定ケース = -1
        決定ステップ = -1
        着目番号 = -1
        応答値 = -1
        限界値 = Double.NaN
    End Sub
End Class

