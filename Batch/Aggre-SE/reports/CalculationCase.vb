Imports Aggre

Public Class CalculationCase

#Region "SNAPデータ"

    Public _SNAPDB As CSNAPDBEx

#End Region

#Region "メンバ変数"

    '入力データ -----------------------------------------------------
    Public DataName As String
    Public DataID As Integer
    Public _照査対象 As List(Of Integer)
    Public _解析対象(0 To 8) As Boolean
    Private _着目点 As Integer
    Public _地域別補正係数 As Single
    Public _地盤区分 As Integer
    Public _液状化区分 As Integer
    Public _M65エリアの設定 As Integer
    Private _震度の低減係数α As Single
    Public _Is液状化L1ケース As Boolean
    Public _Is液状化L21ケース As Boolean
    Public _Is液状化L22ケース As Boolean
    Public _スペクトルの種類 As Short

    Public _不整形地盤の影響 As Boolean
    Public _L1不整形地盤の係数ηx As Single
    Public _L2不整形地盤の係数ηx As Single

    Public _断面設定List As Dictionary(Of Integer, cls部材設定) 'key=断面(DL)番号, value=cls部材設定
    Public _CFList As List(Of CalculationFoundation)

    '計算結果データ -----------------------------------------------------
    Private _応答値 As cls応答値
    Private _応答step指定1 As Integer
    Private _応答step指定2 As Integer

    ''' <summary> 損傷レベルの照査は重い読み込み処理を含むので, 記録しておく</summary>
    Public SonshoDict As Dictionary(Of String, SNAPDBLib.CSNAPDB.IOutSokatu())


    Public ErrorMessage As String = ""

    Public ReadOnly Property IsPrintOut As Boolean
        Get
            For Each c In _解析対象
                If c = True Then
                    Return True '全ケース出力対象とする。
                End If
            Next
            Return False
        End Get
    End Property


#End Region

#Region "初期設定"
    Protected Sub New()
    End Sub

    Sub New(SNAPDB As CSNAPDBEx)
        '入力データ
        _SNAPDB = SNAPDB
        _照査対象 = New List(Of Integer)
        _断面設定List = New Dictionary(Of Integer, cls部材設定)
        _CFList = New List(Of CalculationFoundation)
        '解析結果データ
        _応答値 = Nothing
        _応答step指定1 = -1
        SonshoDict = New Dictionary(Of String, SNAPDBLib.CSNAPDB.IOutSokatu())
    End Sub

    Public WriteOnly Property 震度の低減係数α As String
        Set(value As String)
            Try
                If IsNumeric(value) Then
                    _震度の低減係数α = Convert.ToInt32(value)
                Else
                    _震度の低減係数α = 1.0
                End If
            Catch ex As Exception
                _震度の低減係数α = 1.0
            End Try
        End Set
    End Property

    Public Sub Set照査対象(value As String)
        Try
            If value Is Nothing Then Return
            Dim stArrayData1 As String() = value.Split(",")
            For Each s As String In stArrayData1
                Dim s1 As String
                Dim s2 As String

                If InStr(s, "-") Then
                    Dim stArrayData2 As String() = value.Split("-")
                    s1 = stArrayData2.First
                    s2 = stArrayData2.Last
                Else
                    s1 = s
                    s2 = s1
                End If

                If IsNumeric(s1) AndAlso IsNumeric(s2) Then
                    Dim stNo As Integer = Convert.ToInt32(s1)
                    Dim edNo As Integer = Convert.ToInt32(s2)
                    For i = stNo To edNo
                        _照査対象.Add(i)
                    Next
                End If

                If FormSettings.Option_指定ステップを応答値とする = 1 Then
                    If InStr(s, "step") Then
                        Dim tmp As String = s.Replace("step", "").Trim()
                        Try
                            Me._応答step指定2 = Integer.Parse(tmp)
                            If Me._応答step指定1 = -1 Then
                                Me._応答step指定1 = Me._応答step指定2
                            End If
                        Catch
                            Me._応答step指定2 = -1
                        End Try
                    End If
                End If

            Next

        Catch ex As Exception
            Throw ex
        End Try
    End Sub

    Public Property 着目点 As String
        Get
            Return _着目点.ToString()
        End Get
        Set(value As String)
            If IsNumeric(value) Then
                If value > 0 Then
                    _着目点 = Convert.ToInt32(value)
                ElseIf _SNAPDB.InputInfo.KihonInfo.JNo > 0 Then
                    _着目点 = _SNAPDB.InputInfo.KihonInfo.JNo
                ElseIf _SNAPDB.InputInfo.KihonInfo.KaisekiFlg = 1 Then
                    Throw New Exception("着目点の入力が適切ではありません。")
                Else
                    _着目点 = 1
                End If
            Else
                Throw New Exception("着目点の入力が適切ではありません。")
            End If
        End Set
    End Property

    Public Sub Set地域(rbChiiki As Boolean())
        If rbChiiki(0) = True And rbChiiki(1) = False And rbChiiki(2) = False Then
            'A地域
            _地域別補正係数 = 1.0
        ElseIf rbChiiki(0) = False And rbChiiki(1) = True And rbChiiki(2) = False Then
            'B地域
            _地域別補正係数 = 0.85
        ElseIf rbChiiki(0) = False And rbChiiki(1) = False And rbChiiki(2) = True Then
            'C地域
            _地域別補正係数 = 0.7
        Else
            Throw New Exception("地域別補正係数の入力に問題があります。")
        End If
    End Sub

    Public Sub Set地盤区分(rbJIban As Boolean())

        If rbJIban(0) = True Then
            _地盤区分 = 0
        ElseIf rbJIban(1) = True Then
            _地盤区分 = 1
        ElseIf rbJIban(2) = True Then
            _地盤区分 = 2
        ElseIf rbJIban(3) = True Then
            _地盤区分 = 3
        ElseIf rbJIban(4) = True Then
            _地盤区分 = 4
        ElseIf rbJIban(5) = True Then
            _地盤区分 = 5
        ElseIf rbJIban(9) = True Then
            _地盤区分 = 9
        Else
            Throw New Exception("地盤区分の入力に問題があります。")
        End If
    End Sub

    Public Sub Set液状化区分(rbEkijo As Boolean())
        If rbEkijo.Count <> 2 Then
            Throw New Exception("液状化区分の入力に問題があります。")
        ElseIf rbEkijo(0) = True Then
            _液状化区分 = 5
        ElseIf rbEkijo(1) = True Then
            _液状化区分 = 20
        Else
            _液状化区分 = 0
        End If
    End Sub

    Public Sub SetIs液状化ケース(dgEkijoCase1 As Integer, dgEkijoCase21 As Integer, dgEkijoCase22 As Integer)
        _Is液状化L1ケース = (dgEkijoCase1 = -1)
        _Is液状化L21ケース = (dgEkijoCase21 = -1)
        _Is液状化L22ケース = (dgEkijoCase22 = -1)
    End Sub

    Public Sub Set部材設定(dgLimitLevel As Integer(),
                           dgLimitLevel_title As String(),
                           dgLimitLevel_type As Integer(),
                           dgLimitLevel_VydType As Integer(),
                           dgLimitLevel_La() As Single,
                           dgLimitLevel_SIJI As Integer())

        Dim i As Integer = 1
        Dim MFCount As Integer = _SNAPDB.GetMFCount

        Try
            For Each e In dgLimitLevel_type
                Dim b As New cls部材設定
                b.構造物種類 = e
                b.タイトル = dgLimitLevel_title(e - 1)

                ' 0:h16 RC標準準拠
                ' 1:SNAP準拠
                ' 2:耐震照査の手引き:梁
                ' 3:JR東日本
                ' 4:JRTT:梁
                ' 5:JRTT:柱
                b.せん断耐力種別 = dgLimitLevel_VydType(i - 1)
                If Input.Data.Element.versionCheck Then
                    'ver5.2 以後は【2:耐震照査の手引き:梁】がなく、番号が繰り上がっているため調整する
                    If b.せん断耐力種別 > 2 Then b.せん断耐力種別 += 1
                ElseIf b.せん断耐力種別 = 1 Then
                    '1 は R5 RC標準のせん断耐力式であるため昔のバージョンのデータは 1 -> 0 に変換する
                    b.せん断耐力種別 = 0
                End If

                b.Vydせん断スパン = dgLimitLevel_La(i - 1)
                b.支持 = dgLimitLevel_SIJI(i - 1)

                If i > MFCount Then
                    Dim DLNo As Integer = i - MFCount
                    Dim DLInfo = _SNAPDB.DLInfo(DLNo)
                    If IsNothing(b.タイトル) OrElse b.タイトル.Trim = "" Then
                        b.タイトル = DLInfo.Title
                    End If
                    If IsNothing(b.タイトル) = False Then
                        b.タイトル = b.タイトル.Trim
                    End If
                    b.断面種別 = DLInfo.iType
                Else
                    b.タイトル = "MFデータ"
                    b.断面種別 = -1
                End If
                _断面設定List.Add(i, b)

                i += 1
            Next
        Catch ex As Exception
            Throw New Exception(String.Format("断面(DL)番号{0} に断面情報が設定されていません。", i))
        End Try
    End Sub

    Public Sub Setスペクトル種類(rbSpec As Boolean())
        If rbSpec.Count < 3 Then
            'Throw New Exception("スペクトル種類の入力に問題があります。")
            ReDim Preserve rbSpec(0 To 2)
        End If

        If rbSpec(0) = True Then
            _スペクトルの種類 = 3 'スペクトルⅠ,2 大きいほうの応答値で設計する
        ElseIf rbSpec(1) = True Then
            _スペクトルの種類 = 1 'スペクトルⅠ
        ElseIf rbSpec(2) = True Then
            _スペクトルの種類 = 2 'スペクトル2
        Else
            _スペクトルの種類 = 0
        End If
    End Sub

    Public Sub Set基礎設定(CalculationFoundationList As List(Of CalculationFoundation))

        Me._CFList = CalculationFoundationList

        'For Each cf In _CFList
        '    If IsNothing(cf.反力照査要素番号List) = False Then
        '        For Each p In cf.反力照査要素番号List
        '            If _SNAPDB.InputInfo.KihonInfo.MeNum < p OrElse p < 1 Then
        '                Throw New Exception("基礎の反力照査に用いる要素番号の入力に誤りがあります。" _
        '                                    + vbLf + String.Format("要素番号{0} は 入力データにありません。", p))
        '            End If
        '        Next
        '        If _SNAPDB.InputInfo.KihonInfo.JoNum < cf.変位照査節点番号 OrElse cf.変位照査節点番号 < 1 Then
        '            Throw New Exception("基礎の変位照査に用いる節点番号の入力に誤りがあります。" _
        '                                + vbLf + String.Format("節点番号{0} は 入力データにありません。", cf.変位照査節点番号))
        '        End If
        '    End If
        'Next
    End Sub

#End Region

#Region "応答値を求める"

    Public ReadOnly Property 応答値 As cls応答値
        Get
            Dim result As New cls応答値
            Try
                If _応答値 Is Nothing Then

                    '変位量・震度の集計 -------------------------------------------------------
                    With _SNAPDB
                        result.maxStep = .maxStep
                        ReDim result.変位量(0 To result.maxStep)
                        ReDim result.震度(0 To result.maxStep)
                        ReDim result.回転角(0 To result.maxStep)

                        For i As Integer = 0 To result.maxStep
                            result.変位量(i) = .OutputInfo.StepCtrl(i).JointItem(_着目点).DeltaX * 1000 'mm
                            result.回転角(i) = .OutputInfo.StepCtrl(i).JointItem(_着目点).DeltaT * 1000 '‰rad
                            If _SNAPDB.InputInfo.KihonInfo.KaisekiFlg = 1 Then
                                result.震度(i) = Math.Round(.OutputInfo.StepCtrl(i).LoStepNo / 1000, 3)
                            Else
                                result.震度(i) = Math.Round(.InputInfo.KihonInfo.JALF * .OutputInfo.StepCtrl(i).LoStepNo / .InputInfo.KihonInfo.MDV, 3)
                            End If
                        Next
                        '変位量は初期変位を減じる
                        For i As Integer = 1 To result.maxStep
                            result.変位量(i) = Math.Abs(Math.Round(result.変位量(i) - result.変位量(0), 3))
                            result.回転角(i) = Math.Abs(Math.Round(result.回転角(i) - result.回転角(0), 3))
                        Next
                        result.変位量(0) = 0
                        result.回転角(0) = 0


                        '降伏点の設定
                        Call 部材の降伏判定(_SNAPDB, result)
                        Call 直接基礎の降伏判定(_SNAPDB, result)
                        Call 杭基礎の降伏判定(_SNAPDB, result)
                        Call 最大震度点の設定(_SNAPDB, result)
                        Call 軸力適用範囲の判定(_SNAPDB, result)
                    End With

                    '応答値の算定 -------------------------------------------------------------
                    With result
                        If _SNAPDB.InputInfo.KihonInfo.KaisekiFlg = 1 Then
                            '*** 変位増分解析 ***
                            '降伏点の設定
                            Call Set降伏点(result)

                            Select Case _SNAPDB.InputInfo.KihonInfo.KisoType

                                Case 5, 6, 7, 8 '擁壁の場合（初期降伏点を降伏点とする, エネルギー一定則で応答値を求める。）
                                    '*** 初期降伏点
                                    Dim pKhy = New clsPoint(.変位量(.初期降伏step), .震度(.初期降伏step))
                                    Dim pKeq As clsPoint = pKhy

                                    '*** (Khm+Khy)/2 点
                                    .pKhb = pKhy

                                    '*** 各点
                                    .全体系折曲点震度 = Math.Round(pKeq.y, 3)
                                    .等価固有周期 = 0
                                    .降伏変位 = Math.Round(pKeq.x, 1)

                                    '*** 復旧性
                                    If _解析対象(0) = True Then

                                        .復旧性_線形最大震度 = Get線形最大震度(_スペクトルの種類, _地域別補正係数)
                                        .復旧性_応答塑性率 = 0

                                        If Me._応答step指定1 > 0 Then
                                            .復旧性_最大応答step = Me._応答step指定1
                                            .復旧性_最大応答変位 = .変位量(.復旧性_最大応答step)
                                            .復旧性_最大応答震度 = .震度(.復旧性_最大応答step)
                                        Else
                                            If Me._不整形地盤の影響 = True Then
                                                .全体系折曲点震度_不整形影響 = .全体系折曲点震度 / _L2不整形地盤の係数ηx
                                                If .復旧性_線形最大震度 > .全体系折曲点震度_不整形影響 Then
                                                    .復旧性_C点 = .降伏変位 / .全体系折曲点震度_不整形影響 * .復旧性_線形最大震度
                                                    .復旧性_ABC = .復旧性_線形最大震度 * .復旧性_C点 / 2
                                                    .復旧性_最大応答変位 = .復旧性_ABC / .全体系折曲点震度_不整形影響 + .降伏変位 / 2 '(.降伏変位 / 2) * (((.復旧性_線形最大震度 / .全体系折曲点震度_不整形影響) ^ 2) + 1)
                                                    .復旧性_最大応答変位 = Math.Round(.復旧性_最大応答変位, 1)
                                                    .復旧性_最大応答step = GetStepFrom変位(result, .復旧性_最大応答変位)
                                                Else
                                                    .復旧性_最大応答step = GetStepFrom震度(result, .復旧性_線形最大震度)
                                                    .復旧性_最大応答変位 = .変位量(.復旧性_最大応答step)
                                                End If
                                            Else
                                                If .復旧性_線形最大震度 > .全体系折曲点震度 Then
                                                    .復旧性_C点 = .降伏変位 / .全体系折曲点震度 * .復旧性_線形最大震度
                                                    .復旧性_ABC = .復旧性_線形最大震度 * .復旧性_C点 / 2
                                                    .復旧性_最大応答変位 = .復旧性_ABC / .全体系折曲点震度 + .降伏変位 / 2 '(.降伏変位 / 2) * (((.復旧性_線形最大震度 / .全体系折曲点震度) ^ 2) + 1)
                                                    .復旧性_最大応答変位 = Math.Round(.復旧性_最大応答変位, 1)
                                                    .復旧性_最大応答step = GetStepFrom変位(result, .復旧性_最大応答変位)
                                                Else
                                                    .復旧性_最大応答step = GetStepFrom震度(result, .復旧性_線形最大震度)
                                                    .復旧性_最大応答変位 = .変位量(.復旧性_最大応答step)
                                                End If
                                            End If
                                            If .復旧性_最大応答step < 0 Then
                                                Throw New Exception("このデータは応答変位量に達していません" _
                                                                                            + vbLf _
                                                                                            + String.Format("{0}mm 以上の変位量で解析してください", .復旧性_最大応答変位))
                                            End If
                                            .復旧性_最大応答震度 = .震度(.復旧性_最大応答step)
                                        End If

                                    End If

                                    If _解析対象(5) = True Then
                                        .復旧性_L1震度 = Get線形最大震度(0)
                                        If Me._不整形地盤の影響 = True Then
                                            .復旧性_L1震度_不整形影響無 = .復旧性_L1震度
                                            .復旧性_L1震度 *= _L1不整形地盤の係数ηx
                                        End If
                                        .復旧性_L1震度step = GetStepFrom震度(result, .復旧性_L1震度)
                                        .復旧性_L1震度stepF = GetStepFrom震度F(result, .復旧性_L1震度)
                                    End If

                                    If _解析対象(1) = True Then

                                        '安全性 応答値の設定
                                        .安全性_線形最大震度 = Get線形最大震度(_スペクトルの種類)
                                        .安全性_応答塑性率 = 0

                                        If Me._応答step指定2 > 0 Then
                                            .安全性_最大応答step = Me._応答step指定2
                                            .安全性_最大応答変位 = .変位量(.安全性_最大応答step)
                                            .安全性_最大応答震度 = .震度(.安全性_最大応答step)
                                        Else
                                            If Me._不整形地盤の影響 = True Then
                                                .全体系折曲点震度_不整形影響 = .全体系折曲点震度 / _L2不整形地盤の係数ηx
                                                If .安全性_線形最大震度 > .全体系折曲点震度_不整形影響 Then
                                                    .安全性_C点 = .降伏変位 / .全体系折曲点震度_不整形影響 * .安全性_線形最大震度
                                                    .安全性_ABC = .安全性_線形最大震度 * .安全性_C点 / 2
                                                    .安全性_最大応答変位 = .安全性_ABC / .全体系折曲点震度_不整形影響 + .降伏変位 / 2 ' (.降伏変位 / 2) * (((.安全性_線形最大震度 / .全体系折曲点震度_不整形影響) ^ 2) + 1)
                                                    .安全性_最大応答変位 = Math.Round(.安全性_最大応答変位, 1)
                                                    .安全性_最大応答step = GetStepFrom変位(result, .安全性_最大応答変位)
                                                Else
                                                    .安全性_最大応答step = GetStepFrom震度(result, .安全性_線形最大震度)
                                                    .安全性_最大応答変位 = .変位量(.安全性_最大応答step)
                                                End If
                                            Else
                                                If .安全性_線形最大震度 > .全体系折曲点震度 Then
                                                    .安全性_C点 = .降伏変位 / .全体系折曲点震度 * .安全性_線形最大震度
                                                    .安全性_ABC = .安全性_線形最大震度 * .安全性_C点 / 2
                                                    .安全性_最大応答変位 = .安全性_ABC / .全体系折曲点震度 + .降伏変位 / 2 ' (.降伏変位 / 2) * (((.安全性_線形最大震度 / .全体系折曲点震度) ^ 2) + 1)
                                                    .安全性_最大応答変位 = Math.Round(.安全性_最大応答変位, 1)
                                                    .安全性_最大応答step = GetStepFrom変位(result, .安全性_最大応答変位)
                                                Else
                                                    .安全性_最大応答step = GetStepFrom震度(result, .安全性_線形最大震度)
                                                    .安全性_最大応答変位 = .変位量(.安全性_最大応答step)
                                                End If
                                            End If
                                            If .安全性_最大応答step < 0 Then
                                                Throw New Exception("このデータは応答変位量に達していません" _
                                                                                            + vbLf _
                                                                                            + String.Format("{0}mm 以上の変位量で解析してください", .安全性_最大応答変位))
                                            End If
                                            .安全性_最大応答震度 = .震度(.安全性_最大応答step)

                                        End If
                                    End If

                                Case Else

                                    Dim pKeq As clsPoint = 全体系折曲点(result)

                                    If FormSettings.Option_初期降伏震度を用いた応答変位 = 1 Then
                                        '初期降伏点を降伏点とする
                                        Dim pKhy = New clsPoint(.変位量(.初期降伏step), .震度(.初期降伏step))
                                        pKeq = pKhy
                                    End If

                                    .全体系折曲点震度 = Math.Round(pKeq.y, 3)
                                    '.降伏変位 = Math.Round(pKeq.x, 1)'固有周期を求める時の変位はラウンドしない。15/03/17 打合せより、JRSNAPに合わせた
                                    .等価固有周期 = Math.Round(2 * Math.Sqrt((pKeq.x / 1000) / .全体系折曲点震度), 3)
                                    .降伏変位 = Math.Round(pKeq.x, 1)
                                    If Me._不整形地盤の影響 = True Then result.全体系折曲点震度_不整形影響 = result.全体系折曲点震度 / _L2不整形地盤の係数ηx

                                    Dim エラーメッセージ済フラグ As Boolean = False

                                    If _地盤区分 <> 9 Then
                                        If _解析対象(0) = True Then
                                            '復旧性 応答値の設定
                                            If Me._応答step指定1 > 0 Then
                                                .復旧性_スペクトルの種類 = Me._スペクトルの種類
                                                .復旧性_最大応答step = Me._応答step指定1
                                                .復旧性_最大応答変位 = .変位量(.復旧性_最大応答step)
                                                .復旧性_最大応答震度 = .震度(.復旧性_最大応答step)
                                                .復旧性_応答塑性率 = .復旧性_最大応答変位 / .降伏変位
                                            Else
                                                Try
                                                    If Me._スペクトルの種類 = 3 Then
                                                        .復旧性_応答塑性率 = Get塑性率(result, _地域別補正係数, "復旧性")
                                                        .復旧性_スペクトルの種類 = Me._スペクトルの種類
                                                        Me._スペクトルの種類 = 3
                                                    Else
                                                        .復旧性_応答塑性率 = Get塑性率(result, _地域別補正係数, "復旧性")
                                                    End If
                                                Catch ex As Exception
                                                    Throw ex
                                                End Try
                                                .復旧性_最大応答変位 = .復旧性_応答塑性率 * .降伏変位
                                                .復旧性_最大応答変位 = Math.Round(.復旧性_最大応答変位, 1)
                                                .復旧性_最大応答step = GetStepFrom変位(result, .復旧性_最大応答変位)
                                                .復旧性_最大応答震度 = -1
                                                If FormSettings.Option_弾性応答加速度L2 = 1 Then
                                                    Dim tmpKhr = GetL22地震動(.等価固有周期)
                                                    Dim tmpstep = GetStepFrom震度(result, tmpKhr)
                                                    If tmpstep > 0 Then
                                                        If .復旧性_最大応答step < 0 OrElse
                                                                                       tmpstep < .復旧性_最大応答step Then
                                                            .復旧性_最大応答震度 = tmpKhr
                                                            .復旧性_最大応答step = tmpstep
                                                            .復旧性_最大応答変位_弾性応答無 = .復旧性_最大応答変位
                                                            .復旧性_最大応答変位 = .変位量(tmpstep)
                                                        End If
                                                    End If
                                                End If
                                                If .復旧性_最大応答step < 0 Then
                                                    If エラーメッセージ済フラグ = False Then
                                                        If FormSettings.Option_大量解析プログラム = 0 Then
                                                            Dim message As String = _SNAPDB.DataPath
                                                            message += vbLf + "このデータは応答変位量に達していません"
                                                            message += vbLf + String.Format("{0}mm 以上の変位量で解析してください", .復旧性_最大応答変位)
                                                            MsgBox(message)
                                                        Else
                                                            Throw New Exception(String.Format("{0}mm 以上の変位量で解析してください", .復旧性_最大応答変位))
                                                        End If
                                                        エラーメッセージ済フラグ = True
                                                    End If
                                                    .復旧性_最大応答step = result.maxStep
                                                End If
                                                If .復旧性_最大応答震度 = -1 Then
                                                    .復旧性_最大応答震度 = .震度(.復旧性_最大応答step)
                                                End If
                                            End If

                                        End If

                                        If _解析対象(5) = True Then
                                            .復旧性_L1震度 = GetL1地震動(.等価固有周期)
                                            If Me._不整形地盤の影響 = True Then
                                                .復旧性_L1震度_不整形影響無 = .復旧性_L1震度
                                                .復旧性_L1震度 *= _L1不整形地盤の係数ηx
                                            End If
                                            .復旧性_L1震度step = GetStepFrom震度(result, .復旧性_L1震度)
                                            .復旧性_L1震度stepF = GetStepFrom震度F(result, .復旧性_L1震度)
                                        End If

                                        If _解析対象(1) = True Then
                                            '安全性 応答値の設定
                                            If Me._応答step指定2 > 0 Then
                                                .安全性_スペクトルの種類 = Me._スペクトルの種類
                                                .安全性_最大応答step = Me._応答step指定2
                                                .安全性_最大応答変位 = .変位量(.安全性_最大応答step)
                                                .安全性_最大応答震度 = .震度(.安全性_最大応答step)
                                                .安全性_応答塑性率 = .安全性_最大応答変位 / .降伏変位
                                            Else
                                                Try
                                                    If Me._スペクトルの種類 = 3 Then
                                                        .安全性_応答塑性率 = Get塑性率(result, 1, "安全性")
                                                        .安全性_スペクトルの種類 = Me._スペクトルの種類
                                                        Me._スペクトルの種類 = 3
                                                    Else
                                                        .安全性_応答塑性率 = Get塑性率(result, 1, "安全性")
                                                    End If
                                                Catch ex As Exception
                                                    Throw ex
                                                End Try
                                                .安全性_最大応答変位 = .安全性_応答塑性率 * .降伏変位
                                                .安全性_最大応答変位 = Math.Round(.安全性_最大応答変位, 1)
                                                .安全性_最大応答step = GetStepFrom変位(result, .安全性_最大応答変位)
                                                .安全性_最大応答震度 = -1
                                                If FormSettings.Option_弾性応答加速度L2 = 1 Then
                                                    Dim tmpKhr = GetL22地震動(.等価固有周期)
                                                    Dim tmpstep = GetStepFrom震度(result, tmpKhr)
                                                    If tmpstep > 0 Then
                                                        If .安全性_最大応答step < 0 OrElse
                                                                                      tmpstep < .安全性_最大応答step Then
                                                            .安全性_最大応答震度 = tmpKhr
                                                            .安全性_最大応答step = tmpstep
                                                            .安全性_最大応答変位_弾性応答無 = .安全性_最大応答変位
                                                            .安全性_最大応答変位 = .変位量(tmpstep)
                                                        End If
                                                    End If
                                                End If
                                                If .安全性_最大応答step < 0 Then
                                                    If エラーメッセージ済フラグ = False Then
                                                        If FormSettings.Option_大量解析プログラム = 0 Then
                                                            Dim message As String = _SNAPDB.DataPath
                                                            message += vbLf + "このデータは応答変位量に達していません"
                                                            message += vbLf + String.Format("{0}mm 以上の変位量で解析してください", .安全性_最大応答変位)
                                                            MsgBox(message)
                                                        Else
                                                            Throw New Exception(String.Format("{0}mm 以上の変位量で解析してください", .安全性_最大応答変位))
                                                        End If
                                                        エラーメッセージ済フラグ = True
                                                    End If
                                                    .安全性_最大応答step = result.maxStep
                                                End If
                                                If .安全性_最大応答震度 = -1 Then
                                                    .安全性_最大応答震度 = .震度(.安全性_最大応答step)
                                                End If
                                            End If
                                        End If
                                    Else
                                        '任意スペクトル
                                        If _解析対象(0) = True OrElse _解析対象(1) = True Then
                                            Dim μ As Double = Get塑性率(result)
                                            Using specForm As New SpectralEditForm
                                                With specForm
                                                    .TextBox5.Text = _SNAPDB.DataPath
                                                    .TextBox2.Text = result.等価固有周期
                                                    .TextBox3.Text = IIf(result.全体系折曲点震度_不整形影響 = -1, result.全体系折曲点震度, result.全体系折曲点震度_不整形影響)
                                                    Select Case result.降伏部材情報.Get構造物種類
                                                        Case 0 : .TextBox4.Text = "上部構造(RC, SRC系)"
                                                        Case 1 : .TextBox4.Text = "上部構造(S系)"
                                                        Case 2 : .TextBox4.Text = "基礎構造物(杭ケーソン)"
                                                        Case 3 : .TextBox4.Text = "基礎構造物(直接基礎)"
                                                        Case 4 : .TextBox4.Text = "抗土圧構造物(RC壁体・杭基礎)"
                                                        Case 5 : .TextBox4.Text = "抗土圧構造物(直接基礎) "
                                                    End Select
                                                    If _Is液状化L21ケース = True OrElse _Is液状化L22ケース = True Then
                                                        .TextBox1.Text = Get塑性率(result, _地域別補正係数)
                                                    End If
                                                    .ShowDialog()
                                                End With
                                                μ = specForm.resutValue
                                            End Using
                                            If μ <= 0 Then μ = 1
                                            If _解析対象(0) = True Then
                                                '復旧姓
                                                .復旧性_応答塑性率 = μ
                                                .復旧性_スペクトルの種類 = Me._スペクトルの種類
                                                .復旧性_最大応答変位 = .復旧性_応答塑性率 * .降伏変位
                                                .復旧性_最大応答変位 = Math.Round(.復旧性_最大応答変位, 1)
                                                .復旧性_最大応答step = GetStepFrom変位(result, .復旧性_最大応答変位)
                                                If .復旧性_最大応答step < 0 Then
                                                    If FormSettings.Option_大量解析プログラム = 0 Then
                                                        Throw New Exception("このデータは応答変位量に達していません" _
                                                                                                      + vbLf _
                                                                                                      + String.Format("{0}mm 以上の変位量で解析してください", .復旧性_最大応答変位))
                                                    Else
                                                        Throw New Exception(String.Format("{0}mm 以上の変位量で解析してください", .復旧性_最大応答変位))
                                                    End If
                                                End If
                                                .復旧性_最大応答震度 = .震度(.復旧性_最大応答step)

                                            End If
                                            If _解析対象(1) = True Then
                                                '安全姓
                                                .安全性_応答塑性率 = μ
                                                .安全性_スペクトルの種類 = Me._スペクトルの種類
                                                .安全性_最大応答変位 = .安全性_応答塑性率 * .降伏変位
                                                .安全性_最大応答変位 = Math.Round(.安全性_最大応答変位, 1)
                                                .安全性_最大応答step = GetStepFrom変位(result, .安全性_最大応答変位)
                                                If .安全性_最大応答step < 0 Then
                                                    If FormSettings.Option_大量解析プログラム = 0 Then
                                                        Throw New Exception("このデータは応答変位量に達していません" _
                                                                                                    + vbLf _
                                                                                                    + String.Format("{0}mm 以上の変位量で解析してください", .安全性_最大応答変位))
                                                    Else
                                                        Throw New Exception(String.Format("{0}mm 以上の変位量で解析してください", .安全性_最大応答変位))
                                                    End If
                                                End If
                                                .安全性_最大応答震度 = .震度(.安全性_最大応答step)
                                            End If
                                        End If
                                    End If
                            End Select
                        Else
                            '*** 荷重増分解析 ***
                            If _解析対象(0) = True Then
                                If Me._応答step指定1 > 0 Then
                                    .復旧性_最大応答step = Me._応答step指定1
                                    .復旧性_最大応答変位 = .変位量(.復旧性_最大応答step)
                                    .復旧性_最大応答震度 = .震度(.復旧性_最大応答step)
                                Else
                                    .復旧性_最大応答変位 = .変位量.Last
                                    .復旧性_最大応答step = _SNAPDB.InputInfo.KihonInfo.LastStep
                                    .復旧性_最大応答震度 = .震度.Last
                                End If
                            End If
                            If _解析対象(1) = True Then
                                If Me._応答step指定2 > 0 Then
                                    .安全性_最大応答step = Me._応答step指定2
                                    .安全性_最大応答変位 = .変位量(.安全性_最大応答step)
                                    .安全性_最大応答震度 = .震度(.安全性_最大応答step)
                                Else
                                    .安全性_最大応答変位 = .変位量.Last
                                    .安全性_最大応答step = _SNAPDB.InputInfo.KihonInfo.LastStep
                                    .安全性_最大応答震度 = .震度.Last
                                End If
                            End If
                            If _解析対象(5) = True Then
                                .復旧性_L1震度 = .震度.Last
                                .復旧性_L1震度step = _SNAPDB.InputInfo.KihonInfo.LastStep
                                .復旧性_L1震度stepF = _SNAPDB.InputInfo.KihonInfo.LastStep
                            End If
                        End If
                    End With

                    _応答値 = result
                End If
                Return _応答値
            Catch ex As Exception
                result.ErrorMessage = ex.Message
                Return result
            End Try
        End Get
    End Property

    ''' <summary>
    ''' 作用軸力が適用範囲外文字をセット
    ''' </summary>
    ''' <param name="SNAPDB"></param>
    ''' <param name="result"></param>
    Private Sub 軸力適用範囲の判定(ByVal SNAPDB As CSNAPDBEx, ByRef result As Aggre.cls応答値)
        Dim strDirectoryName = System.IO.Path.GetDirectoryName(SNAPDB.DataPath)
        Dim strFilePath = strDirectoryName + "\" + System.IO.Path.GetFileNameWithoutExtension(SNAPDB.DataPath) + ".EMG"
        If System.IO.File.Exists(strFilePath) Then
            Using Reader As New IO.StreamReader(strFilePath, System.Text.Encoding.GetEncoding("Shift-JIS"))
                Try
                    Dim Line As String = Reader.ReadLine
                    Do Until IsNothing(Line)
                        If InStr(Line, "作用軸力が適用範囲外") Then
                            Line = Reader.ReadLine              '次の行を読み込む。
                            Dim NGStep As Integer = Convert.ToInt32(Left(Line, 5))
                            Dim NGAp As Integer = Convert.ToInt32(Mid(Line, 6, 5))
                            Dim NGMember As Integer = SNAPDB.GetMemberNo(NGAp)
                            result.軸圧縮力適用範囲外List.Add(New StepAndTarget(NGStep, NGMember))
                        End If
                        Line = Reader.ReadLine              '次の行を読み込む。
                    Loop
                Catch ex As Exception
                End Try
            End Using
        End If
    End Sub

    Private Sub 部材の降伏判定(ByVal SNAPDB As CSNAPDBEx, ByRef result As Aggre.cls応答値)
        With _SNAPDB
            Dim maxStep As Integer = result.maxStep
            result.部材降伏step = Integer.MaxValue
            result.部材降伏番号 = -1
            For i As Integer = 0 To maxStep
                For j = 1 To .OutputInfo.StepCtrl(i).MemberItem.Count - 1
                    Dim m = .OutputInfo.StepCtrl(i).MemberItem(j)
                    If m.SCFlg > 1 Then
                        result.部材降伏step = i
                        result.部材降伏番号 = j
                        Return
                    End If
                Next
            Next i
        End With

    End Sub

    Private Sub 杭基礎の降伏判定(ByVal SNAPDB As CSNAPDBEx, ByRef result As Aggre.cls応答値)

        Dim key1 As Integer = result.maxStep
        Dim key2 As Integer = result.maxStep
        Dim key3 As Integer = result.maxStep
        Dim key4 As Integer = result.maxStep
        For Each cf In _CFList
            If cf._FoundationType <> 0 Then
                Continue For
            End If
            If IsNothing(cf.反力照査要素番号List) = False Then
                '杭鉛直力に関する照査
                For k = 0 To cf.反力照査要素番号List.Count - 1
                    Dim mNo As Integer = cf.反力照査要素番号List(k)
                    Dim ALF As Double = cf.反力照査部材数List(k)
                    If mNo < 0 Then Exit For
                    For istep As Integer = 0 To key1
                        Dim Rvd = cf.杭基礎L1鉛直支持力の検討(Me, istep, mNo, ALF)
                        If Rvd.安全度 > 1 Then
                            key1 = istep
                            Exit For
                        End If
                        Rvd = cf.杭基礎復旧性2鉛直支持の照査(Me, istep, mNo, ALF)
                        If Rvd.安全度 > 1 Then
                            key1 = istep
                            Exit For
                        End If
                        Rvd = cf.杭基礎安全性鉛直支持の照査(Me, istep, mNo, ALF)
                        If Rvd.安全度 > 1 Then
                            key1 = istep
                            Exit For
                        End If
                    Next

                    For istep As Integer = 0 To key2
                        Dim Rhd = cf.杭基礎L1引抜き抵抗力の照査(Me, istep, mNo, ALF)
                        If Rhd.安全度 > 1 Then
                            key2 = istep
                            Exit For
                        End If
                    Next
                Next
            End If

            If IsNothing(cf.変位照査節点番号List) = False Then
                '変位に関する照査
                For k = 0 To cf.変位照査節点番号List.Count - 1
                    Dim pNo As Integer = cf.変位照査節点番号List(k)
                    Dim _Direction As Integer = cf.変位照査距離List(k)

                    For istep As Integer = 0 To key3
                        If pNo < 0 Then Exit For
                        Dim δd = cf.杭基礎L1水平変位の照査(Me, istep, pNo, _Direction)
                        If δd.安全度 > 1 Then
                            key3 = istep
                            Exit For
                        End If
                        δd = cf.杭基礎復旧性2水平変位の照査(Me, istep, pNo, _Direction)
                        If δd.安全度 > 1 Then
                            key3 = istep
                            Exit For
                        End If
                        δd = cf.杭基礎安全性水平変位の照査(Me, istep, pNo, _Direction)
                        If δd.安全度 > 1 Then
                            key3 = istep
                            Exit For
                        End If
                    Next
                    For istep As Integer = 0 To key4
                        If pNo < 0 Then Exit For
                        Dim θd = cf.杭基礎L1回転角の照査(Me, istep, pNo)
                        If θd.安全度 > 1 Then
                            key4 = istep
                            Exit For
                        End If
                        θd = cf.杭基礎復旧性2回転角の照査(Me, istep, pNo)
                        If θd.安全度 > 1 Then
                            key4 = istep
                            Exit For
                        End If
                        θd = cf.杭基礎安全性回転角の照査(Me, istep, pNo)
                        If θd.安全度 > 1 Then
                            key4 = istep
                            Exit For
                        End If
                    Next
                Next
            End If
        Next
        result.杭基礎降伏step = Math.Min(Math.Min(key1, key2), Math.Min(key3, key4))
        Select Case result.杭基礎降伏step
            Case Integer.MaxValue
                result.杭基礎降伏種類 = "降伏しない"
            Case key1
                result.杭基礎降伏種類 = "押込支持降伏"
            Case key2
                result.杭基礎降伏種類 = "引抜支持降伏"
            Case key3
                result.杭基礎降伏種類 = "水平変位超過"
            Case key4
                result.杭基礎降伏種類 = "回転変位超過"
        End Select
    End Sub

    Private Sub 直接基礎の降伏判定(ByVal SNAPDB As CSNAPDBEx, ByRef result As Aggre.cls応答値)
        With _SNAPDB
            Dim key As Integer = result.maxStep
            Dim JoNo As Integer = -1
            Dim flg As Boolean = False
            For Each cf In _CFList
                If cf._FoundationType <> 1 Then
                    Continue For
                End If
                If IsNothing(cf.変位照査節点番号List) Then
                    Continue For
                End If
                If (cf.変位照査節点番号List.Count() = 0) Then
                    Continue For
                End If

                '着目点に直接基礎の情報が入力されているか調べる
                flg = False
                Dim pNo = cf.変位照査節点番号List.First
                For Each f In .OutputInfo.OutChokukiso
                    If pNo = f.JoNo Then
                        flg = True
                        Exit For
                    End If
                Next
                '着目点に直接基礎の情報が入力されていれば降伏震度を調べる
                If flg = True Then
                    '0ステップから L1の支持力を超過するステップを探す
                    For istep As Integer = 0 To key
                        '鉛直支持力の超過状況を調べる
                        If cf.直接基礎L1鉛直支持力の検討(Me, istep, pNo).安全度 >= 1 Then
                            key = istep
                            JoNo = pNo
                            Exit For
                        End If

                        '水平支持力の超過状況を調べる
                        If cf.直接基礎L1水平支持力の検討(Me, istep, pNo).安全度 >= 1 Then
                            key = istep
                            JoNo = pNo
                            Exit For
                        End If
                        If cf.直接基礎復旧性2水平支持力の検討(Me, istep, pNo).安全度 >= 1 Then
                            key = istep
                            JoNo = pNo
                            Exit For
                        End If
                        If cf.直接基礎安全性水平支持力の検討(Me, istep, pNo).安全度 >= 1 Then
                            key = istep
                            JoNo = pNo
                            Exit For
                        End If
                        '残留傾斜の超過状況を調べる
                        If cf.直接基礎L1残留傾斜の検討(Me, istep, pNo).安全度 >= 1 Then
                            key = istep
                            JoNo = pNo
                            Exit For
                        End If
                        If cf.直接基礎復旧性2回転角の検討(Me, istep, pNo).安全度 >= 1 Then
                            key = istep
                            JoNo = pNo
                            Exit For
                        End If
                        If cf.直接基礎安全性回転角の検討(Me, istep, pNo).安全度 >= 1 Then
                            key = istep
                            JoNo = pNo
                            Exit For
                        End If
                    Next
                End If
            Next

            result.直接基礎降伏番号 = JoNo
            result.直接基礎降伏step = key

        End With
    End Sub

    Private Sub Set降伏点(ByRef result As cls応答値)
        With result
            If .直接基礎降伏step < 0 Then
                .初期降伏step = Math.Min(.部材降伏step, .杭基礎降伏step)
            Else
                .初期降伏step = Math.Min(.部材降伏step, Math.Min(.杭基礎降伏step, .直接基礎降伏step))
            End If

            If .初期降伏step = 0 Then
                Dim message As String = ""
                Select Case .初期降伏step
                    Case .部材降伏step
                        '部材先行降伏
                        message = String.Format("部材番号{0} が降伏しました", .部材降伏番号)
                    Case .杭基礎降伏step
                        '杭の支持力先行降伏
                        message = String.Format("杭の {0} が制限値に達しました", .杭基礎降伏種類)
                    Case .直接基礎降伏step
                        '直接基礎先行降伏
                        message = "直接基礎が降伏しました"
                End Select
                Throw New Exception("この解析ケースは Step0 で" + vbLf + message + vbLf + "応答値を設定できません")
            End If

            Select Case .初期降伏step
                Case Integer.MaxValue
                    Throw New Exception("この解析ケースはどの部材も降伏していません。" + vbLf + "応答値を設定できません")
                Case .部材降伏step
                    '部材先行降伏
                    .初期降伏震度 = .震度(.部材降伏step)
                    Dim ApNo As Integer = _SNAPDB.GetMFNo(.部材降伏番号)
                    If ApNo > 0 Then
                        .降伏部材情報 = _断面設定List(ApNo)
                    Else
                        .降伏部材情報 = _断面設定List(_SNAPDB.GetDLNo(.部材降伏番号))
                    End If
                    Select Case _SNAPDB.InputInfo.KihonInfo.KisoType
                        Case Is > 9
                            .降伏部材情報.構造物種類 = 5
                    End Select
                Case .杭基礎降伏step
                    '杭の支持力先行降伏
                    .初期降伏震度 = .震度(.杭基礎降伏step)
                    .降伏部材情報 = New cls部材設定
                    Select Case _SNAPDB.InputInfo.KihonInfo.KisoType
                        Case Is < 5
                            .降伏部材情報.構造物種類 = 4
                        Case Else '擁壁の場合
                            .降伏部材情報.構造物種類 = 5
                    End Select
                    .降伏部材情報.断面種別 = -1
                    .降伏部材情報.タイトル = .杭基礎降伏種類
                Case .直接基礎降伏step
                    '直接基礎先行降伏
                    .初期降伏震度 = .震度(.直接基礎降伏step)
                    .降伏部材情報 = New cls部材設定
                    .降伏部材情報.構造物種類 = 10
                    .降伏部材情報.断面種別 = -1
                    .降伏部材情報.タイトル = "直接基礎"
            End Select

            If result.最大震度step < result.初期降伏step Then
                Throw New Exception("このデータは部材降伏点が最大震度点以上です。")
            End If

        End With
    End Sub

    Private Sub 最大震度点の設定(ByVal SNAPDB As CSNAPDBEx, ByRef result As Aggre.cls応答値)
        With _SNAPDB
            Dim maxStep As Integer = result.maxStep
            Dim key As Double = -1
            Dim istep As Integer = maxStep
            For i As Integer = 0 To maxStep
                If key <= result.震度(i) Then
                    key = result.震度(i)
                    istep = i
                End If
            Next
            result.最大震度step = istep
            result.最大震度 = result.震度(istep)
        End With
    End Sub

    Private Function 全体系折曲点(ByVal 応答値 As cls応答値) As clsPoint

        With 応答値
            '*** 原点
            Dim pKh0 = New clsPoint(.変位量(0), .震度(0))
            '*** 初期降伏点
            Dim pKhy = New clsPoint(.変位量(.初期降伏step), .震度(.初期降伏step))
            '*** 最大点
            Dim pKhm = New clsPoint(.変位量(.最大震度step), .震度(.最大震度step))

            '*** (Khm+Khy)/2 点
            応答値.pKhb = New clsPoint
            応答値.pKhb.y = (pKhm.y + pKhy.y) / 2
            For i = 1 To .maxStep
                If 応答値.pKhb.y <= .震度(i) Then
                    応答値.pKhb.x = ((応答値.pKhb.y - .震度(i - 1)) * (.変位量(i) - .変位量(i - 1)) / (.震度(i) - .震度(i - 1))) + .変位量(i - 1)
                    応答値.pKhbStep = GetStepFrom震度F(応答値, 応答値.pKhb.y)
                    Exit For
                End If
            Next

            '*** 全体系折曲点
            Dim result = clsPoint.直線の交点(pKh0, pKhy, 応答値.pKhb, pKhm)
            If result.x < 0 OrElse result.y < 0 Then
                result = pKhy
            End If
            Return result
        End With

    End Function

    Private Function Get線形最大震度(スペクトルの種類 As Short, Optional 地域別補正係数 As Single = 1) As Double

        Dim result As Double = 0
        '' 0:G0地盤
        ''    :
        '' 5:G5地盤
        '' 6:液状化指数PL(5＜PL≦20)
        '' 7:液状化指数PL(20＜PL)
        Dim 地盤種別 As Short
        If _Is液状化L22ケース = False Then
            地盤種別 = _地盤区分
        Else
            Select Case _液状化区分
                Case 5
                    地盤種別 = 6
                Case 20
                    地盤種別 = 7
            End Select
            地域別補正係数 = 1 '液状化では地域別補正係数を考慮しない
        End If


        Select Case スペクトルの種類
            Case 0 'L1地震動
                result = 0.2
            Case 1 'スペクトルⅠ
                '【H24年 耐震標準 付属図  7.9.3 278ページ】
                '【H24年 耐震標準 解説図  6.4.6 47ページ】
                Select Case 地盤種別
                    Case 6 '液状化指数PL(5＜PL≦20)
                        result = 236.5 / 980
                    Case 7 '液状化指数PL(20＜PL)
                        result = 167.7 / 980
                    Case Else
                        result = 地域別補正係数 * 524 / 980
                End Select
            Case 2, 3 'スペクトル2
                '【H24年 耐震標準 付属表 7.7.1 271ページ】
                '【H24年 耐震標準 付属図  7.9.3 278ページ】
                '【H24年 耐震標準 解説図  6.4.6 47ページ】
                Select Case 地盤種別
                    Case 0
                        result = 地域別補正係数 * 708.3 / 980
                    Case 1
                        result = 地域別補正係数 * 943.9 / 980
                    Case 2
                        result = 地域別補正係数 * 1028.6 / 980
                    Case 3
                        result = 地域別補正係数 * 872.3 / 980
                    Case 4
                        result = 地域別補正係数 * 788.9 / 980
                    Case 5
                        result = 地域別補正係数 * 664.6 / 980
                    Case 6 '液状化指数PL(5＜PL≦20)
                        result = 392.8 / 980
                    Case 7 '液状化指数PL(20＜PL)
                        result = 312.4 / 980
                    Case Else
                        result = 地域別補正係数 * 944 / 980
                End Select
        End Select

        Return Math.Round(result, 3)

    End Function

    ''' <summary>
    ''' 安全性の塑性率は、Aspect-SE ファイルの存在の有無を確認してから塑性率を返す
    ''' </summary>
    ''' <param name="応答値"></param>
    ''' <returns></returns>
    Private Function GetAspectSE塑性率(ByVal 応答値 As cls応答値, AspectSE名 As String) As Double

        ' ファイル名を生成する ////////////////////////////////////////////////////////////////
        Dim strDirectoryName = System.IO.Path.GetDirectoryName(Me._SNAPDB.DataPath)
        Dim μFileName As List(Of String) = New List(Of String)()
        μFileName.Add(strDirectoryName + "\Aspect-SE_L2-")

        Select Case Me._スペクトルの種類
            Case 1 ':スペクトルⅠ
                μFileName(0) += "spc1_"
            Case 2 ':スペクトル2
                μFileName(0) += "spc2_"
            Case 3 ':スペクトルⅠ,2 大きいほう
                μFileName.Add(μFileName(0))
                μFileName(0) += "spc1_"
                μFileName(1) += "spc2_"
        End Select

        For i As Integer = 0 To μFileName.Count - 1
            Dim fn As String = μFileName(i)
            fn += String.Format("G{0}", Me._地盤区分)

            Select Case 応答値.降伏部材情報.Get構造物種類
                Case 0 ':上部構造(RC,SRC系)
                    fn += "（RC）"
                Case 1 ':上部構造(S系)
                    fn += "（S）"
                Case 2 ':基礎構造物(杭ケーソン)
                    fn += "（杭基礎）"
                Case 3 ':基礎構造物(直接基礎)
                    fn += "（直接基礎）"
                Case 4 ':抗土圧構造物(RC壁体・杭基礎)
                    fn += "（橋台杭基礎）"
                Case 5 ':抗土圧構造物(直接基礎)
                    fn += "（橋台直接基礎）"
            End Select

            Dim f1 As String = fn + ".csv"
            Dim f2 As String = fn + AspectSE名 + ".csv"
            If System.IO.File.Exists(f2) Then
                μFileName(i) = f2
            Else
                μFileName(i) = f1
            End If
        Next i


        'ファイルが存在しない場合 ////////////////////////////////////////////////////////////
        Dim flg As Boolean = False
        Dim MUE As Double = 0 'この塑性率と大きい方を返す
        '3:スペクトルⅠ,2 大きいほう で片方のファイルが存在しない場合
        If Me._スペクトルの種類 = 3 Then
            Dim f1Flg = System.IO.File.Exists(μFileName(0))
            Dim f2Flg = System.IO.File.Exists(μFileName(1))
            '両方ない場合
            If f1Flg = False And f2Flg = False Then
                Return -1
            End If
            'スペクトルⅠだけがない場合
            If f1Flg = False Then
                μFileName.RemoveAt(0)
            End If
            'スペクトル2だけがない場合
            If f2Flg = False Then
                μFileName.RemoveAt(1)
            End If
        Else
            If System.IO.File.Exists(μFileName(0)) = False Then
                Return -1
            End If
        End If

        '塑性率を読み取り返す //////////////////////////////////////////////////////////////////////////
        Return Me.getAspectSE(応答値, μFileName)

    End Function

    Private Function Get塑性率(ByVal 応答値 As cls応答値, Optional 地域別補正係数 As Single = 1, Optional AspectSE名 As String = "") As Double

        If FormSettings.Option_任意スペクトル = 1 Then
            If _Is液状化L21ケース = False AndAlso _Is液状化L22ケース = False Then
                ' 独自
                Dim re As Double = GetAspectSE塑性率(応答値, AspectSE名)
                If re > 0 Then
                    Return re
                End If
            End If
        End If

        '塑性率の算定条件 --------------------------------------------------------
        Dim 降伏震度 As Single = IIf(応答値.全体系折曲点震度_不整形影響 = -1, 応答値.全体系折曲点震度, 応答値.全体系折曲点震度_不整形影響)

        Dim 震度の低減係数α As Single = _震度の低減係数α

        '' 0:上部構造(RC,SRC系)
        '' 1:上部構造(S系)
        '' 2:基礎構造物(杭ケーソン)
        '' 3:基礎構造物(直接基礎)
        '' 4:抗土圧構造物(RC壁体・杭基礎)
        '' 5:抗土圧構造物(直接基礎)
        Dim 構造物種類 As Short = 応答値.降伏部材情報.Get構造物種類


        '' 0:G0地盤
        ''    :
        '' 5:G5地盤
        '' 6:液状化指数PL(5＜PL≦20)
        '' 7:液状化指数PL(20＜PL)
        Dim 地盤種別 As Short
        If _Is液状化L21ケース = False AndAlso _Is液状化L22ケース = False Then
            地盤種別 = _地盤区分
        Else
            Select Case _液状化区分
                Case 5
                    地盤種別 = 6
                Case 20
                    地盤種別 = 7
            End Select
            地域別補正係数 = 1 '液状化では地域別補正係数を考慮しない
            '降伏震度 = Math.Max(降伏震度, 0.2)
        End If


        '' 1:スペクトルⅠ
        '' 2:スペクトル2
        '' 3:スペクトルⅠ,2 大きいほう
        Dim スペクトルの種類 As Short = _スペクトルの種類
        Dim iスペクトルの種類 As Short()
        If _スペクトルの種類 = 3 Then
            iスペクトルの種類 = New Short() {1, 2}
        Else
            iスペクトルの種類 = New Short() {_スペクトルの種類}
        End If
        If AspectSE名 = "復旧性" Then
            iスペクトルの種類 = New Short() {2} '復旧性はスペクトル2
        End If

        If _Is液状化L21ケース = True AndAlso _Is液状化L22ケース = True Then
            スペクトルの種類 = 3
        ElseIf _Is液状化L21ケース = True Then
            スペクトルの種類 = 1
            iスペクトルの種類 = New Short() {1}
        ElseIf _Is液状化L22ケース = True Then
            スペクトルの種類 = 2
            iスペクトルの種類 = New Short() {2}
        End If


        '' 0:存在しない
        '' 1:存在する
        '' 2:不明
        Dim M65以上の震源エリアの存在 As Short
        Select Case _M65エリアの設定
            Case 0
                M65以上の震源エリアの存在 = 2
            Case 1
                M65以上の震源エリアの存在 = 1
            Case 2
                M65以上の震源エリアの存在 = 0
        End Select

        '塑性率の算定 ------------------------------------------------------------
        If 地盤種別 = 9 Then Return 1

        Dim result As Double = 0

        Dim Str As String
        Try

            For Each i In iスペクトルの種類

                Str = _SNAPDB.CalcOutouMyu(構造物種類,
                                      地盤種別,
                                      i,
                                      M65以上の震源エリアの存在,
                                      地域別補正係数,
                                      震度の低減係数α,
                                      応答値.等価固有周期,
                                      降伏震度) '応答値.全体系折曲点震度)
                Dim tmp As Double
                    If Double.TryParse(Str, tmp) = False Then
                        If InStr(Str, "降伏せず") Then
                            tmp = 1
                        ElseIf InStr(Str, "10.00>μ") Then
                            Dim message As String = "以下の条件で塑性率の取得に失敗しました。"
                            message += vbLf + "--------------- 条件 -----------------------"
                            message += vbLf + String.Format("構造物種別: {0}", 構造物種類)
                            message += vbLf + String.Format("地盤種別: {0}", 地盤種別)
                            message += vbLf + String.Format("スペクトルの種類: {0}", スペクトルの種類)
                            message += vbLf + String.Format("M65以上の震源エリアの存在: {0}", M65以上の震源エリアの存在)
                            message += vbLf + String.Format("地域別補正係数: {0}", 地域別補正係数)
                            message += vbLf + String.Format("震度の低減係数α: {0}", 震度の低減係数α)
                            message += vbLf + String.Format("等価固有周期: {0}", 応答値.等価固有周期)
                            message += vbLf + String.Format("全体系折曲点震度: {0}", 降伏震度)
                            message += vbLf + "--------------- エラーメッセージ ------------"
                            message += vbLf + Str
                            message += vbLf + "---------------------------------------------"
                            message += vbLf + "μ=10.00 として計算を続行します。"
                            If FormSettings.Option_大量解析プログラム = 0 Then
                                MsgBox(message)
                            Else
                                Throw New Exception(Str)
                            End If
                            tmp = 10
                        Else
                            Throw New Exception(Str)
                        End If
                    End If


                    If result < tmp Then
                        result = tmp
                        _スペクトルの種類 = CShort(i)
                    End If

                Next i
                Return result

        Catch ex As Exception
            Dim message As String = "以下の条件で塑性率の取得に失敗しました。"
            message += vbLf + "--------------- 条件 -----------------------"
            message += vbLf + String.Format("構造物種別{0}: ", 構造物種類)
            message += vbLf + String.Format("地盤種別{0}: ", 地盤種別)
            message += vbLf + String.Format("スペクトルの種類{0}: ", スペクトルの種類)
            message += vbLf + String.Format("M65以上の震源エリアの存在{0}: ", M65以上の震源エリアの存在)
            message += vbLf + String.Format("地域別補正係数{0}: ", 地域別補正係数)
            message += vbLf + String.Format("震度の低減係数α{0}: ", 震度の低減係数α)
            message += vbLf + String.Format("等価固有周期{0}: ", 応答値.等価固有周期)
            message += vbLf + String.Format("全体系折曲点震度{0}: ", 降伏震度)
            message += vbLf + "--------------- エラーメッセージ ------------"
            message += vbLf + ex.Message
            message += vbLf + "---------------------------------------------"
            message += vbLf + "販売元へお問い合わせください。"
            If FormSettings.Option_大量解析プログラム = 0 Then
                MsgBox(message)
            Else
                Throw New Exception(ex.Message)
            End If

        End Try

        Return result

    End Function

    Private Function GetL22地震動(ByVal T As Double) As Double

        Dim gal As Double = 0
        Dim kh As Double = 0
        If _Is液状化L21ケース = False AndAlso _Is液状化L22ケース = False Then
            '通常地盤
            Select Case _地盤区分
                Case 0
                    Select Case T
                        Case Is < 0.5
                            gal = 1650
                        Case Else
                            gal = 750 * Math.Pow(T, -1.137)
                    End Select
                Case 1
                    Select Case T
                        Case Is < 0.5
                            gal = 2200
                        Case Else
                            gal = 1000 * Math.Pow(T, -1.137)
                    End Select
                Case 2
                    Select Case T
                        Case Is < 0.25
                            gal = 6401.95 * Math.Pow(T, 0.65)
                        Case Is <= 0.65
                            gal = 2600
                        Case Else
                            gal = 1593.15 * Math.Pow(T, -1.137)
                    End Select
                Case 3
                    Select Case T
                        Case Is < 0.35
                            gal = 4550.76 * Math.Pow(T, 0.65)
                        Case Is <= 0.8
                            gal = 2300
                        Case Else
                            gal = 1784.6 * Math.Pow(T, -1.137)
                    End Select
                Case 4
                    Select Case T
                        Case Is < 0.35
                            gal = 3561.46 * Math.Pow(T, 0.65)
                        Case Is <= 1.1
                            gal = 1800
                        Case Else
                            gal = 2006.02 * Math.Pow(T, -1.137)
                    End Select
                Case 5
                    Select Case T
                        Case Is < 0.35
                            gal = 2572.17 * Math.Pow(T, 0.65)
                        Case Is <= 1.6
                            gal = 1300
                        Case Else
                            gal = 2218.34 * Math.Pow(T, -1.137)
                    End Select
            End Select
            kh = _地域別補正係数 * gal / 980
        Else
            'L2液状化
            Select Case _液状化区分
                Case 5
                    Select Case T
                        Case Is < 1.2
                            gal = 750
                        Case Else
                            gal = 922.76 * Math.Pow(T, -1.137)
                    End Select
                Case 20
                    Select Case T
                        Case Is < 1.5
                            gal = 750
                        Case Else
                            gal = 872.12 * Math.Pow(T, -1.137)
                    End Select
            End Select
            kh = gal / 980
        End If
        Return kh
    End Function

    Private Function GetL1地震動(ByVal T As Double) As Double
        Dim gal As Double = 0
        Dim kh As Double = 0
        If _Is液状化L1ケース = False Then
            '通常地盤
            Select Case _地盤区分
                Case 0
                    Select Case T
                        Case Is < 0.2
                            gal = 460 * Math.Pow(T, 0.44)
                        Case Is <= 1.4
                            gal = 200
                        Case Else
                            gal = 280 * Math.Pow(T, -1)
                    End Select
                Case 1
                    Select Case T
                        Case Is < 0.2
                            gal = 508 * Math.Pow(T, 0.44)
                        Case Is <= 1.4
                            gal = 250
                        Case Else
                            gal = 350 * Math.Pow(T, -1)
                    End Select
                Case 2
                    Select Case T
                        Case Is < 0.15
                            gal = 691 * Math.Pow(T, 0.44)
                        Case Is <= 1.4
                            gal = 300
                        Case Else
                            gal = 420 * Math.Pow(T, -1)
                    End Select
                Case 3
                    Select Case T
                        Case Is < 0.18
                            gal = 744 * Math.Pow(T, 0.44)
                        Case Is <= 1.4
                            gal = 350
                        Case Else
                            gal = 490 * Math.Pow(T, -1)
                    End Select
                Case 4
                    Select Case T
                        Case Is < 0.25
                            gal = 681 * Math.Pow(T, 0.44)
                        Case Is <= 1.4
                            gal = 370
                        Case Else
                            gal = 518 * Math.Pow(T, -1)
                    End Select
                Case 5
                    Select Case T
                        Case Is < 0.4
                            gal = 599 * Math.Pow(T, 0.44)
                        Case Is <= 1.6
                            gal = 400
                        Case Else
                            gal = 640 * Math.Pow(T, -1)
                    End Select
            End Select
            kh = _地域別補正係数 * gal / 980
        Else
            'L1液状化
            Select Case T
                Case Is < 0.4
                    gal = 507 * Math.Pow(T, 0.424)
                Case Is <= 2.25
                    gal = 350
                Case Else
                    gal = 1250 * Math.Pow(T, -1.57)
            End Select
            kh = gal / 980
        End If
        Return kh
    End Function

    Private Function GetStepFrom変位(ByVal 応答値 As cls応答値, 変位 As Double) As Integer
        With 応答値
            Dim istep As Integer = 0
            For Each d In .変位量
                If d >= 変位 Then
                    Return istep
                End If
                istep += 1
            Next
            Return -1
        End With
    End Function

    Private Function GetStepFrom震度(ByVal 応答値 As cls応答値, 震度 As Double) As Integer
        With 応答値
            Dim istep As Integer = 0
            For Each kh In .震度
                If kh >= 震度 Then
                    Return istep
                End If
                istep += 1
            Next
            Return -1
        End With
    End Function

    Private Function GetStepFrom震度F(ByVal 応答値 As cls応答値, 震度 As Double) As Single
        With 応答値
            Dim istep As Single = 0
            For i = 1 To .maxStep
                If .震度(i) >= 震度 Then
                    istep = ((震度 - .震度(i - 1)) / (.震度(i) - .震度(i - 1))) + (i - 1) '換算ステップ数
                    Return istep
                End If
            Next
            Return -1
        End With
    End Function

#End Region

    ''' <summary>
    ''' 塑性率を csv ファイルから読み取り計算する
    ''' </summary>
    ''' <param name="応答値"></param>
    ''' <param name="μFileName"></param>
    ''' <returns></returns>
    Private Function getAspectSE(応答値 As cls応答値, μFileName As List(Of String)) As Double

        Dim D1 As New SNAPDBLib.CSNAPDB

        '塑性率の算定条件 --------------------------------------------------------
        Dim Teq As Double = 応答値.等価固有周期
        Dim Khy As Double = IIf(応答値.全体系折曲点震度_不整形影響 = -1, 応答値.全体系折曲点震度, 応答値.全体系折曲点震度_不整形影響)

        Dim MUE As Double = 0

        For Each filename In μFileName

            Dim Str As String = D1.CalcOutouMyuAspect(filename, Teq, Khy)
            Dim tmp As Double

            If Double.TryParse(Str, tmp) = False Then
                If InStr(Str, "降伏せず") Then
                    tmp = 1
                Else
                    tmp = 10
                End If
            End If

            If MUE < tmp Then
                MUE = tmp
            End If
        Next

        Return MUE

    End Function

#Region "スペクトルの補完を自力でやっていたのを SNAPDBLib に変えたので もう要らない"

    '  Private Function Readスペクトル式(ByVal filename As String, muePoints As Dictionary(Of Integer, List(Of clsPoint))) As Boolean

    '    Try
    '      'ファイルの内容をすべて取得
    '      Dim lines() As String
    '      Using sr = New System.IO.StreamReader(filename,
    '        System.Text.Encoding.GetEncoding("shift_jis"))
    '        Dim s = sr.ReadToEnd()
    '        lines = s.Split(vbCrLf)
    '      End Using

    '      ' 不要な改行とスペースを削除する
    '      For i As Long = 0 To lines.Count - 1
    '        lines(i) = lines(i).Replace(vbLf, "").Trim()
    '      Next

    '      ' 塑性率設定数を取得
    '      Dim tmp = lines(4).Split(",")
    '      Dim mueCount As Integer
    '      If Integer.TryParse(tmp(1), mueCount) = False Then
    '        Throw New Exception("If Integer.TryParse(tmp(1), mueCount) = False Thenでエラー")
    '      End If

    '      Dim row As Long = 5
    '      For i As Integer = 1 To mueCount
    '        tmp = lines(row).Split(",")
    '        Dim mue As Integer
    '        If Integer.TryParse(tmp(1), mue) = False Then
    '          Throw New Exception("If Integer.TryParse(tmp(1), mue) = False Then でエラー")
    '        End If
    '        Dim dataCount As Integer
    '        If Integer.TryParse(tmp(3), dataCount) = False Then
    '          Throw New Exception("If Integer.TryParse(tmp(3), dataCount) = False Then でエラー")
    '        End If
    '        row += 2

    '        Dim pointList As New List(Of clsPoint)
    '        For j As Integer = 1 To dataCount
    '          tmp = lines(row).Split(",")
    '          Dim T As Double
    '          If Double.TryParse(tmp(0), T) = False Then
    '            Throw New Exception("If Integer.TryParse(tmp(0), T) = False Then でエラー")
    '          End If
    '          Dim Kh As Double
    '          If Double.TryParse(tmp(1), Kh) = False Then
    '            Throw New Exception("If Integer.TryParse(tmp(1), Kh) = False Then でエラー")
    '          End If
    '          Dim data As New clsPoint(T, Kh)
    '          pointList.Add(data)
    '          row += 1
    '        Next
    '        muePoints.Add(mue, pointList)
    '      Next
    '    Catch ex As Exception
    '      Return False
    '    End Try

    '    Return True

    '  End Function

    '  Private Function スペクトル補間(muePoints As Dictionary(Of Integer, List(Of clsPoint)), kh As Single, Teq As Single) As Single
    '    '               μ=1～10,  勾配 , 0-始 1-? 2-A 3-B
    '    '    Dim sngμ式(1 To 10, 1 To 3, 0 To 3) As Single

    '    On Error GoTo ErrorHandle

    '    Dim Miu As New Dictionary(Of Integer, Double)

    '    ' 固有周期 Teq の 各μ(1～10) の Kh を求める ////////////////////////
    '    For Each dataList In muePoints
    '      Dim mue As Integer = dataList.Key
    '      Dim i As New clsPoint
    '      Dim j As New clsPoint
    '      For Each point In dataList.Value
    '        j = point
    '        If point.x > Teq Then Exit For
    '        i = point
    '      Next
    '      Dim ix As Double = Math.Log10(i.x)
    '      Dim iy As Double = Math.Log10(i.y)
    '      Dim jx As Double = Math.Log10(j.x)
    '      Dim jy As Double = Math.Log10(j.y)
    '      ' 対数式を計算する log(y) = a * log(x) + b
    '      Dim b As Double = (jx * iy / ix - jy) / (jx / ix - 1)
    '      Dim a As Double = (iy - b) / ix

    '      Dim k As Double = Math.Pow(10, a * Math.Log10(Teq) + b)
    '      Miu.Add(mue, k)
    '    Next

    '    ' 塑性率を求める /////////////////////////////////////////////////
    '    Dim R3 As Single
    '    Dim M1 As Single
    '    Dim M2 As Single
    '    Dim MM As Single = 0F

    '    For I = 1 To Miu.Count - 1
    '      Dim r1 As Integer = Miu.Keys(I - 1)
    '      Dim r2 As Integer = Miu.Keys(I)
    '      If Miu(r2) <= kh Then
    '        M1 = Math.Log(Miu(r1)) / Math.Log(10)
    '        M2 = Math.Log(Miu(r2)) / Math.Log(10)
    '        R3 = Math.Log(kh) / Math.Log(10)
    '        MM = Math.Round(r2 - ((R3 - M2) / (M1 - M2)), 2)
    '        Exit For
    '      End If
    '    Next

    '    ' 塑性率の適用範囲外の場合に対応する /////////////////////////////////
    '    If MM = 0 Then
    '      If Miu.Last.Value > kh Then
    '        MM = 10
    '      End If
    '    End If

    '    Return MM
    '    Exit Function

    'ErrorHandle:
    '    Call MsgBox("ERROR-スペクトルの補間")
    '    Return -1

    '  End Function

#End Region


End Class

Public Class clsPoint

    Public x As Double
    Public y As Double

    Sub New()

    End Sub

    Sub New(_x As Double, _y As Double)
        x = _x
        y = _y
    End Sub

    Public Shared Function 直線の交点(P1 As clsPoint, P2 As clsPoint, P3 As clsPoint, P4 As clsPoint) As clsPoint
        Dim result As New clsPoint
        Try
            Dim A = P2.y - P1.y
            Dim B = P1.x - P2.x
            Dim U = (P2.y - P1.y) * P1.x - (P2.x - P1.x) * P1.y
            Dim C = P4.y - P3.y
            Dim D = P3.x - P4.x
            Dim V = (P4.y - P3.y) * P3.x - (P4.x - P3.x) * P3.y
            result.x = (D * U - B * V) / (A * D - B * C)
            result.y = (A * V - C * U) / (A * D - B * C)
            Return result
        Catch ex As Exception
            Throw ex
        End Try
    End Function

End Class

Public Class cls部材設定

    ''' <summary>
    ''' 1～3 上部構造物
    ''' 4    基礎構造物
    ''' 5    抗土圧構造物
    ''' </summary>
    Public 構造物種類 As Integer

    ''' <summary>
    '''  51: RC 矩形/中空矩形
    '''  52: RC 円形/円環
    '''  54: RC Ｔ形
    '''  55: RC 小判形/中空小判
    ''' 201:SRC 矩形
    ''' 202:SRC 円形
    ''' 204:SRC Ｔ形
    ''' 151:PHC杭
    ''' 300:コンクリート充填鋼管
    '''  61:S鋼 矩形
    '''  62:S鋼 円形
    ''' 501:鋼管杭接合部材
    ''' 400:SC杭
    ''' </summary>
    Public 断面種別 As Integer

    ''' <summary>
    ''' 0:h16 RC標準準拠
    ''' 1:SNAP準拠
    ''' 2:耐震照査の手引き:梁
    ''' 3:JR東日本
    ''' 4:JRTT:梁
    ''' 5:JRTT:柱
    ''' </summary>
    Public せん断耐力種別 As Integer

    Public Vydせん断スパン As Double

    ''' <summary>
    ''' R5年 RC標準から導入された端部支持条件
    ''' 0:単純支持
    ''' 1:片持ち支持
    ''' 2:両端固定
    ''' 9:Vyd
    ''' </summary>
    Public 支持 As Integer

    ''' <summary>
    ''' 0:上部構造(RC,SRC系)
    ''' 1:上部構造(S系)
    ''' 2:基礎構造物(杭ケーソン)
    ''' 3:基礎構造物(直接基礎)
    ''' 4:抗土圧構造物(RC壁体・杭基礎)
    ''' 5:抗土圧構造物(直接基礎)
    ''' </summary>
    Public Function Get構造物種類() As Short
        Dim result As Short = -1
        Select Case 構造物種類
            Case Is <= 3
                If 断面種別 = 61 OrElse 断面種別 = 62 OrElse 断面種別 = 501 OrElse 断面種別 = 400 Then
                    ' 上部構造(S系)
                    result = 1
                Else
                    ' 上部構造(RC,SRC系)
                    result = 0
                End If
            Case 4
                ' 基礎構造物(杭ケーソン)
                result = 2
            Case 5
                ' 抗土圧構造物(RC壁体・杭基礎)
                result = 4
            Case 10
                ' 基礎構造物(直接基礎)
                result = 3
            Case 100
                ' 抗土圧構造物(直接基礎)
                result = 5
        End Select
        Return result
    End Function

    Public タイトル As String = ""

End Class

Public Class cls応答値

    Public ErrorMessage As String = ""
    Public maxStep As Integer
    Public 変位量() As Double
    Public 震度() As Double
    Public 回転角() As Double

    Public 部材降伏step As Integer
    Public 部材降伏番号 As Integer

    Public 直接基礎降伏step As Integer
    Public 直接基礎降伏番号 As Integer

    Public 杭基礎降伏step As Integer
    Public 杭基礎降伏種類 As String

    Public 最大震度step As Integer
    Public 最大震度 As Double
    Public pKhb As clsPoint
    Public pKhbStep As Single

    Public 初期降伏震度 As Double = -1
    Public 初期降伏step As Integer
    Public 降伏部材情報 As cls部材設定
    Public 降伏変位 As Double = -1

    Public 全体系折曲点震度_不整形影響 As Double = -1
    Public 全体系折曲点震度 As Double = -1
    Public 等価固有周期 As Double = -1

    Public 復旧性_線形最大震度 As Double = -1
    Public 復旧性_C点 As Double = -1
    Public 復旧性_ABC As Double = -1

    Public 復旧性_応答塑性率 As Double = -1
    Public 復旧性_最大応答変位_弾性応答無 As Double = -1
    Public 復旧性_最大応答変位 As Double
    Public 復旧性_最大応答震度 As Double
    Public 復旧性_最大応答step As Integer
    Public 復旧性_スペクトルの種類 As Integer

    Public 復旧性_L1震度_不整形影響無 As Double = -1
    Public 復旧性_L1震度 As Double = -1
    Public 復旧性_L1震度step As Integer
    Public 復旧性_L1震度stepF As Single

    Public 安全性_線形最大震度 As Double = -1
    Public 安全性_C点 As Double = -1
    Public 安全性_ABC As Double = -1

    Public 安全性_応答塑性率 As Double = -1
    Public 安全性_最大応答変位_弾性応答無 As Double = -1
    Public 安全性_最大応答変位 As Double
    Public 安全性_最大応答震度 As Double
    Public 安全性_最大応答step As Integer
    Public 安全性_スペクトルの種類 As Integer

    Public 軸圧縮力適用範囲外List As List(Of StepAndTarget) = New List(Of StepAndTarget)()

End Class

Public Class StepAndTarget
    Public iStep As Integer
    Public ID As Integer

    Sub New(_iStep As Integer, _ID As Integer)
        Me.iStep = _iStep
        Me.ID = _ID
    End Sub

End Class

Public Class CalculationCaseEx
    Inherits CalculationCase

    Public ReadOnly Property Get地盤区分Str As String
        Get
            Dim result As String = ""
            Try
                If _地盤区分 = 9 Then
                    result = "任意スペクトル"
                Else
                    result = String.Format("G{0}地盤", _地盤区分)
                End If
                If _地域別補正係数 <> 1 Then
                    result += String.Format(" (地域別係数:{0:F2})", _地域別補正係数)
                End If
                Select Case _スペクトルの種類
                    Case 1
                        result += ", スペクトルⅠ"
                    Case 2
                        result += ", スペクトル2"
                    Case 3
                        result += ", スペクトルⅠ,2 大きいほう"
                    Case Else
                        result += String.Format(", スペクトル{0}", _スペクトルの種類)
                End Select

                If _Is液状化L1ケース = True OrElse _Is液状化L21ケース = True OrElse _Is液状化L22ケース = True Then
                    result += ", 液状化：" + IIf(_液状化区分 = 5, "5≦PL＜20", "20＜PL")
                End If
            Catch ex As Exception
            End Try
            Return result
        End Get
    End Property


End Class