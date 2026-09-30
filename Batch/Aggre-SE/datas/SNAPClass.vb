Imports System.ComponentModel

Public Class SNAPClass

    Public SnapDBPath(0 To Input.DATAROWS - 1) As String
    Public Property SnapDBAbsolutePath(id As Integer) As String
        Get
            If Not Input.FileName Is Nothing AndAlso System.IO.File.Exists(Input.FileName) Then

                'データが多い場合の処理
                If SnapDBPath.Length <= id Then ReDim Preserve SnapDBPath(0 To Input.DATAROWS - 1)

                If System.IO.Path.IsPathRooted(SnapDBPath(id)) Then
                    '絶対パスです
                    Return SnapDBPath(id)
                Else
                    '相対パスです
                    Dim basePath As String = System.IO.Path.GetDirectoryName(Input.FileName)
                    basePath += "/"
                    Dim filePath As String = SnapDBPath(id)
                    If filePath Is Nothing Then Return Nothing
                    If filePath = "" Then Return Nothing
                    '"%"を"%25"に変換しておく（デコード対策）
                    basePath = basePath.Replace("%", "%25")
                    filePath = filePath.Replace("%", "%25")
                    '絶対パスを取得する
                    Dim u1 As New Uri(basePath)
                    Dim u2 As New Uri(u1, filePath)
                    Dim absolutePath As String = u2.LocalPath
                    '"%25"を"%"に戻す
                    absolutePath = absolutePath.Replace("%25", "%")
                    '結果を返す
                    Return absolutePath
                End If
            Else
                '基準となるファイルがない場合そのまま返す
                Return SnapDBPath(id)
            End If
        End Get
        Set(value As String)
            If value = "" Then
                SnapDBPath(id) = Nothing
                Return
            End If
            If System.IO.Path.IsPathRooted(value) Then
                '絶対パスです
                If FormSettings.Option_相対パス > 0 Then
                    If String.IsNullOrEmpty(Input.FileName) Then
                        SnapDBPath(id) = value
                        Return
                    End If
                    Dim basePath As String = Input.FileName
                    Dim filePath As String = value
                    '"%"を"%25"に変換しておく（デコード対策）
                    basePath = basePath.Replace("%", "%25")
                    filePath = filePath.Replace("%", "%25")
                    '相対パスを取得する
                    Dim u1 As New Uri(basePath)
                    Dim u2 As New Uri(filePath)
                    Dim relativeUri As Uri = u1.MakeRelativeUri(u2)
                    Dim relativePath As String = relativeUri.ToString()
                    'URLデコードする（エンコード対策）
                    relativePath = Uri.UnescapeDataString(relativePath)
                    '"%25"を"%"に戻す
                    relativePath = relativePath.Replace("%25", "%")
                    '結果を表示する
                    SnapDBPath(id) = relativePath
                Else
                    SnapDBPath(id) = value
                End If
            Else
                '相対パスです
                If FormSettings.Option_相対パス > 0 Then
                    SnapDBPath(id) = value
                Else
                    If Not Input.FileName Is Nothing AndAlso System.IO.File.Exists(Input.FileName) Then
                        Dim basePath As String = System.IO.Path.GetDirectoryName(Input.FileName)
                        basePath += "/"
                        Dim filePath As String = value
                        '"%"を"%25"に変換しておく（デコード対策）
                        basePath = basePath.Replace("%", "%25")
                        filePath = filePath.Replace("%", "%25")
                        '絶対パスを取得する
                        Dim u1 As New Uri(basePath)
                        Dim u2 As New Uri(u1, filePath)
                        Dim absolutePath As String = u2.LocalPath
                        '"%25"を"%"に戻す
                        absolutePath = absolutePath.Replace("%25", "%")
                        SnapDBPath(id) = absolutePath
                    Else
                        '基準となるファイルがない場合そのままの状態で保持
                        SnapDBPath(id) = value
                    End If
                End If
            End If
        End Set
    End Property

    Private _SnapDB(0 To Input.DATAROWS - 1) As CSNAPDBEx

    Public Sub checkSnapDBlength(count As Integer)

        If Input.DATAROWS < count Then
            Input.DATAROWS = count
        End If

        If _SnapDB.Length < Input.DATAROWS Then
            ReDim Preserve _SnapDB(0 To Input.DATAROWS - 1)
        End If
        If SnapDBPath.Length < Input.DATAROWS Then
            ReDim Preserve SnapDBPath(0 To Input.DATAROWS - 1)
        End If

    End Sub


    Public Function SnapDB(index As Integer) As CSNAPDBEx
        If _SnapDB.Length < index Then
            Return Nothing
        End If
        Return _SnapDB(index)
    End Function

    Public Function SnapDB() As CSNAPDBEx
        For Each sd In _SnapDB
            If Not sd Is Nothing Then
                Return sd
            End If
        Next
        Return Nothing
    End Function

    Public Function SetSnapDB(index As Integer, DB1File As String, Optional IsInput As Boolean = True) As Boolean
        Try
            ' ファイルが存在しているかどうか確認する
            If System.IO.File.Exists(DB1File) Then
                _SnapDB(index) = New CSNAPDBEx(DB1File, IsInput)
                Return _SnapDB(index).isOperation
            End If
            Return False
        Catch ex As Exception
            Return False
        End Try
    End Function

    ''' <summary>
    ''' 解析前に読み込む
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    ''' <remarks></remarks>
    Public Sub ReSetSnapDB(ByVal sender As Object, ByVal e As DoWorkEventArgs)


        Try
            Dim CaseName As CaseNameClass = e.Argument

            Dim bw As BackgroundWorker = DirectCast(sender, BackgroundWorker)
            For i = 0 To _SnapDB.Count - 1
                Dim per As Integer = ((i + 1) / _SnapDB.Count) * 100
                bw.ReportProgress(per, String.Format("計算結果データ読み込み中... {0}/{1}", i + 1, _SnapDB.Count))

                '解析ケース画面のチェックマークに何もチェックが無ければ読み込まない
                If CaseName.CaseAble(i) = False Then
                    Continue For
                End If

                Dim data As CSNAPDBEx = _SnapDB(i)
                Dim FilePath As String = SnapDBAbsolutePath(i)
                If Not IsNothing(FilePath) Then
                    If FilePath.Trim.Length > 0 Then
                        data.SetSnapDB(FilePath, False)
                    End If
                End If

            Next

        Catch ex As Exception
            Input.ReadDBFlg = False
        End Try
    End Sub


    Public Sub ReSetSnapDB(ByVal oldFileName As String, ByVal newFileName As String)

        Try
            For i = 0 To _SnapDB.Count - 1
                Dim data As CSNAPDBEx = _SnapDB(i)

                '古いパスでファイルパスを取得
                Input.FileName = oldFileName
                Dim FilePath As String = SnapDBAbsolutePath(i)

                '新しい相対パスを設定
                Input.FileName = newFileName
                If Not IsNothing(FilePath) Then
                    If FilePath.Trim.Length > 0 Then
                        SnapDBAbsolutePath(i) = FilePath
                    End If
                End If
            Next

        Catch ex As Exception
            Input.ReadDBFlg = False
        End Try

        Input.FileName = newFileName
    End Sub


    ''' <summary>
    ''' 最初に見つかったデータのバージョンを返す
    ''' </summary>
    ''' <returns></returns>
    Public Function SnapVersion() As SNAPDBLib.CSNAPDB.ISysemVersion
        For Each sd In _SnapDB
            If Not sd Is Nothing Then
                Return sd.InputInfo.KihonInfo.Version
            End If
        Next
        Return New SNAPDBLib.CSNAPDB.ISysemVersion()

    End Function



    Public Sub DeltSnapDB(index As Integer)
        Try
            _SnapDB(index) = Nothing
            SnapDBPath(index) = Nothing
        Catch ex As Exception
        End Try

    End Sub

End Class

Public Class CSNAPDBEx
    Inherits SNAPDBLib.CSNAPDB

#Region "メンバ変数"

    Public DataPath As String

    'オブジェクトの作成
    Public isOperation As Boolean
    Public isChokukisoOperation As Boolean
    Public Chokukiso As List(Of ChokukisoCSV)

    Public Structure ChokukisoCSV
        Public JoNo As Integer
        Public Lv1() As 直接基礎React1
        Public Lv2() As 直接基礎React2
        Public Lv3() As 直接基礎React2
    End Structure

#End Region

#Region "初期化処理"

    Sub New(DataPath As String, Optional IsInput As Boolean = True)
        isOperation = False
        SetSnapDB(DataPath, IsInput)

        isChokukisoOperation = False
        Chokukiso = Nothing
        SetChokukisoDB(Output_ChokukisoCSVFileName(DataPath))
    End Sub

    Public Sub SetSnapDB(DataPath As String, Optional IsInput As Boolean = True)
        Try
            If System.IO.File.Exists(DataPath) Then
                Me.DataPath = DataPath
                Dim ErrNo As Integer = 0
                If IsInput = True Then
                    ErrNo = MyBase.DataReadInput(DataPath) 'インプット時点では、読み込み速度を上げる（予定）
                Else
                    ErrNo = MyBase.DataRead(DataPath)
                End If
                Select Case ErrNo
                    Case 0
                        If FormSettings.バージョンチェック = False Then
                            isOperation = True
                            Return
                        Else
                            '連携バージョンチェック機能
                            Dim ver = MyBase.InputInfo.KihonInfo.Version
                            If ver.Major >= 1 Then
                                If ver.Major > 1 Then
                                    isOperation = True
                                    Return
                                End If
                                If ver.Minor >= 2 Then
                                    If ver.Minor > 2 Then
                                        isOperation = True
                                        Return
                                    End If
                                    If ver.Revision >= 1 Then
                                        isOperation = True
                                        Return
                                    End If
                                End If
                            End If

                            MsgBox("連携するJRSNAPのバージョンが最新ではありません。" + vbLf _
                                   + "最新版を使用して連携してください",, "データ連携")
                            isOperation = False
                            Return
                        End If

                    Case -2
                        'MsgBox("警告：" + vbLf _
                        '       + DataPath + vbLf _
                        MsgBox("連携するJRSNAPのバージョンが最新ではありません。" + vbLf _
                                   + "最新版を使用して連携してください",, "データ連携")
                        isOperation = False
                        Return
                    Case Else
                        MsgBox("データの読み取りに失敗しました。" + vbLf _
                               + DataPath + vbLf _
                               + "は、有効なデータではありません。")
                        isOperation = False
                        Return
                End Select


            Else
                isOperation = False
            End If
        Catch ex As Exception
            isOperation = False
        End Try
    End Sub

    Private Sub SetChokukisoDB(DataPath As String)
        Try
            If System.IO.File.Exists(DataPath) Then
                Call DataReadChokukiso(DataPath)
                isChokukisoOperation = True
            Else
                isChokukisoOperation = False
            End If
        Catch ex As Exception
            isChokukisoOperation = False
        End Try
    End Sub

    Private Sub DataReadChokukiso(DataPath As String)
        Chokukiso = New List(Of ChokukisoCSV)
        Using Reader As New IO.StreamReader(DataPath, System.Text.Encoding.GetEncoding("Shift-JIS"))
            Dim C = New ChokukisoCSV
            ReDim C.Lv1(0 To maxStep())
            ReDim C.Lv2(0 To maxStep())
            ReDim C.Lv3(0 To maxStep())

            Try
                Dim Line As String = Reader.ReadLine
                Do Until IsNothing(Line)
                    '残留変位(性能レベル１) -----------------------------------------
                    For i = 2 To 3
                        Line = Reader.ReadLine()
                    Next
                    C.JoNo = Line.Split(",")(1)
                    For istep = 0 To Me.maxStep
                        Line = Reader.ReadLine()
                        Dim Items() As String = Line.Split(",")
                        C.Lv1(istep) = New 直接基礎React1(Items)
                    Next 'istep

                    '残留変位(性能レベル２) -----------------------------------------
                    For i = 2 To 3
                        Line = Reader.ReadLine()
                    Next
                    C.JoNo = Line.Split(",")(1)
                    For istep = 0 To Me.maxStep
                        Line = Reader.ReadLine()
                        Dim Items() As String = Line.Split(",")
                        C.Lv2(istep) = New 直接基礎React2(Items)
                    Next

                    '安定(地震時) ---------------------------------------------------
                    For i = 2 To 3
                        Line = Reader.ReadLine()
                    Next
                    C.JoNo = Line.Split(",")(1)
                    For istep = 0 To Me.maxStep
                        Line = Reader.ReadLine()
                        Dim Items() As String = Line.Split(",")
                        C.Lv3(istep) = New 直接基礎React2(Items)
                    Next

                    'データ登録 ---------------------------------------------------
                    Chokukiso.Add(C)
                Loop
            Catch ex As Exception
            End Try
        End Using


    End Sub

    Private Function Output_ChokukisoCSVFileName(Datapath As String) As String
        ' ディレクトリ名を取得する
        Dim FileNameWithoutExtension As String = System.IO.Path.GetFileNameWithoutExtension(Datapath)
        ' 拡張子を含まないファイル名を取得する
        Dim DirectoryName As String = System.IO.Path.GetDirectoryName(Datapath)
        Return DirectoryName + "\" + FileNameWithoutExtension + "_Output_Chokukiso.csv"
    End Function

#End Region

#Region "入力データの取得"

    Public Function maxStep() As Integer
        Return InputInfo.KihonInfo.LastStep
    End Function

    Public Function Get変位増分解析() As Integer
        Return InputInfo.KihonInfo.KaisekiFlg
    End Function

    Function Get着目点() As Integer
        Return InputInfo.KihonInfo.JNo
    End Function

    Public Function GetJointCount() As Integer
        Return InputInfo.KihonInfo.JoNum
    End Function

    Public Function GetJoint(pNo As Integer) As IJointInfo
        Return InputInfo.JointInfo(pNo)
    End Function

    Public Function GetMemberCount() As Integer
        Return InputInfo.KihonInfo.MeNum
    End Function

    Public Function GetMember(index As Integer) As IMemberInfo
        Return InputInfo.MemberInfo(index)
    End Function

    Public Function GetMemberLength(index As Integer) As Single
        Dim iNo As Integer = GetMember(index).Itan
        Dim jNo As Integer = GetMember(index).Jtan
        Dim i = GetJoint(iNo)
        Dim j = GetJoint(jNo)
        Return Math.Sqrt(Math.Pow(j.X - i.X, 2) + Math.Pow(j.Y - i.Y, 2))
    End Function

    Public Function GetMFCount() As Integer
        Return InputInfo.KihonInfo.MFNum
    End Function

    Public Function GetDLCount() As Integer
        Return InputInfo.DLInfo.Count - 1
    End Function

    Public Function DLInfo(ByVal index As Integer) As IDLInfo
        Return InputInfo.DLInfo(index)
    End Function

    Public Function GetMFNo(mNo As Integer) As Integer
        Dim result As Integer = 0
        Dim ApNo = GetMember(mNo).M
        If GetMFCount() >= ApNo Then
            result = ApNo
        End If
        Return result
    End Function

    Public Function GetDLNo(mNo As Integer) As Integer
        Dim result As Integer = 0
        Dim ApNo = GetMember(mNo).M
        For Each Ap In InputInfo.APInfo
            If Ap.MeNo = ApNo Then
                result = Ap.DLNO
                Exit For
            End If
        Next
        Return result
    End Function

    Public Function GetAp(mNo As Integer) As IAPInfo
        Dim ApNo = GetMember(mNo).M
        Dim result As IAPInfo
        result = Nothing
        For Each Ap In InputInfo.APInfo
            If Ap.MeNo = ApNo Then
                result = Ap
                Exit For
            End If
        Next
        Return result
    End Function

    Public Function GetApIK(DLNo As Integer) As Integer
        For Each Ap In InputInfo.APInfo
            If Ap.DLNO = DLNo Then
                Return Ap.IK
            End If
        Next
        Return -1
    End Function

    Public Function GetChokukisoJoint() As List(Of Integer)
        Dim result As New List(Of Integer)
        If OutputInfo.OutChokukiso Is Nothing Then
            For Each ch In InputInfo.SupportInfo
                If ch.JoNO <> 0 And ch.IK = 6 Then
                    result.Add(ch.JoNO)
                End If
            Next
        Else
            For Each ch In OutputInfo.OutChokukiso
                If ch.JoNo <> 0 Then
                    result.Add(ch.JoNo)
                End If
            Next
        End If
        Return result
    End Function

    Public Function Getγi(DLNo As Integer) As Double
        Dim result As Double = 1
        'DLinfo.???
        Return result
    End Function

    Public Function GetMembers(DLNo As Integer) As List(Of Integer)
        Dim result As New List(Of Integer)
        If DLNo < 0 Then
            result.Add(GetMemberNo(-DLNo))
        Else
            For mNo = 1 To InputInfo.KihonInfo.MeNum
                If GetDLNo(mNo) = DLNo Then
                    result.Add(mNo)
                End If
            Next
        End If
        Return result
    End Function


    Public Function GetApNos(DLNo As Integer) As List(Of Integer)
        Dim result As New List(Of Integer)
        For Each mNo In GetMembers(DLNo)
            Dim ApNo As Integer = InputInfo.MemberInfo(mNo).M
            If ApNo > 0 Then
                result.Add(ApNo)
            End If
        Next
        Return result
    End Function

    Public Function GetMemberNo(ApNo As Integer) As Integer
        For No = 1 To GetMemberCount()
            If ApNo = InputInfo.MemberInfo(No).M Then
                Return No
            End If
        Next
        Return -1
    End Function

    Public Function Getαf() As Double
        Return InputInfo.KihonInfo.Alphaf
    End Function

    Public Function Getρm() As Double
        Return InputInfo.KihonInfo.Rhom
    End Function

#End Region

#Region "結果データの取得"

    Public Function SonshoR10(StepNum As Integer, MLevel As Integer) As SNAPDBLib.CSNAPDB.IOutSokatu()

        Dim DirectoryName As String = System.IO.Path.GetDirectoryName(Me.DataPath)
        Dim FileNameWithoutExtension As String = System.IO.Path.GetFileNameWithoutExtension(Me.DataPath) ' 拡張子を含まないファイル名を取得する
        Dim StrFilePath = DirectoryName + "\" + FileNameWithoutExtension + ".R10"
        If Not System.IO.File.Exists(StrFilePath) Then
            Return Nothing
        End If

        Dim errorMsg As String = ""
        Try
            If MyBase.DataReadR10(StrFilePath, StepNum, 3, 2, errorMsg) < 0 Then
                Return Nothing
            End If
        Catch ex As Exception
            Return Nothing
        End Try
        Return MyBase.OutputInfo.OutSoukatu.Clone()
    End Function


    ''' <summary>直接基礎 安定計算用の曲げモーメントを求める</summary>
    ''' <param name="istep">ステップ</param>
    ''' <param name="pNo">節点番号</param>
    ''' <param name="Lv">
    ''' 1:復旧性レベル1
    ''' 2:復旧性レベル2
    ''' 3:安全性
    ''' </param>
    ''' <remarks>直接基礎 安定計算用の鉛直力</remarks>
    Public Function ChokukisoMd(ByVal istep As Integer, pNo As Integer, Optional Lv As Integer = 1) As Double

        If IsNothing(Chokukiso) OrElse Chokukiso.Count = 0 Then
            Dim Md As Single
            For No As Integer = 1 To InputInfo.SupportInfo.Count - 1
                If InputInfo.SupportInfo(No).JoNO = pNo Then
                    If InputInfo.SupportInfo(No).IK = 6 OrElse InputInfo.SupportInfo(No).IK = 3 Then
                        Md += OutputInfo.StepCtrl(istep).JointItem(No).JibanHanryoku
                    End If
                End If
            Next
            Return Math.Abs(Md)
        Else
            For Each ch In Chokukiso
                If ch.JoNo = pNo Then
                    Return ch.Lv1(istep).Md
                End If
            Next
        End If
        Return Double.NaN
    End Function

    ''' <summary>直接基礎 安定計算用の鉛直力を求める</summary>
    ''' <param name="istep">ステップ</param>
    ''' <param name="pNo">節点番号</param>
    ''' <param name="Lv">
    ''' 1:復旧性レベル1
    ''' 2:復旧性レベル2
    ''' 3:安全性
    ''' </param>
    ''' <remarks>直接基礎 安定計算用の鉛直力</remarks>
    Public Function ChokukisoVd(ByVal istep As Integer, pNo As Integer, Optional Lv As Integer = 1) As Double

        If IsNothing(Chokukiso) OrElse Chokukiso.Count = 0 Then
            Dim Vd As Single
            For No As Integer = 1 To InputInfo.SupportInfo.Count - 1
                If InputInfo.SupportInfo(No).JoNO = pNo AndAlso InputInfo.SupportInfo(No).IK = 2 Then
                    Vd += OutputInfo.StepCtrl(istep).JointItem(No).JibanHanryoku
                End If
            Next
            Return Vd
        Else
            For Each ch In Chokukiso
                If ch.JoNo = pNo Then
                    Select Case Lv
                        Case 1
                            Return ch.Lv1(istep).Vd
                        Case 2
                            Return ch.Lv2(istep).Vd
                        Case 3
                            Return ch.Lv3(istep).Vd
                    End Select
                End If
            Next
        End If
        Return Double.NaN
    End Function

    ''' <summary>直接基礎 安定計算用の水平力を求める</summary>
    ''' <param name="istep">ステップ</param>
    ''' <param name="pNo">節点番号</param>
    ''' <param name="Lv">
    ''' 1:復旧性レベル1
    ''' 2:復旧性レベル2
    ''' 3:安全性
    ''' </param>
    ''' <remarks>直接基礎 安定計算用の水平力</remarks>
    Public Function ChokukisoHd(ByVal istep As Integer, pNo As Integer, Lv As Integer) As Double
        If IsNothing(Chokukiso) OrElse Chokukiso.Count = 0 Then
            Dim Hd As Single = 0
            '設計水平力は、すべての水平バネ反力の合計とする。
            For No As Integer = 1 To InputInfo.SupportInfo.Count - 1
                If InputInfo.SupportInfo(No).JoNO = pNo Then
                    If InputInfo.SupportInfo(No).IK = 1 Then
                        Hd += OutputInfo.StepCtrl(istep).JointItem(No).JibanHanryoku
                    End If
                End If
                If InputInfo.SupportInfo(No).IK = 4 Then
                    Hd += OutputInfo.StepCtrl(istep).JointItem(No).JibanHanryoku
                End If
            Next
            Return Hd
        Else
            For Each ch In Chokukiso
                If ch.JoNo = pNo Then
                    Select Case Lv
                        Case 1
                            Return ch.Lv1(istep).Hd
                        Case 2
                            Return ch.Lv2(istep).Hd
                        Case 3
                            Return ch.Lv3(istep).Hd
                    End Select
                End If
            Next
        End If
        Return Double.NaN
    End Function

    ''' <summary>直接基礎 安定計算用の鉛直支持力を求める</summary>
    ''' <param name="istep">ステップ</param>
    ''' <param name="pNo">節点番号</param>
    ''' <param name="Lv">
    ''' 1:復旧性レベル1
    ''' 2:復旧性レベル2
    ''' 3:安全性
    ''' </param>
    ''' <remarks>直接基礎 安定計算用の鉛直支持力</remarks>
    Public Function ChokukisoRvd(istep As Integer, pNo As Integer, Lv As Integer, Optional type As Integer = 0) As Double

        If IsNothing(Chokukiso) OrElse Chokukiso.Count = 0 Then
            Dim A As Double
            'If type = 6 Then
            '  'type = 6（擁壁の直接基礎の場合）は基礎の奥行き幅を 1.00m とする。
            '  A = ChokukisoA(istep, pNo, Lv, 1.0)
            'Else
            A = ChokukisoA(istep, pNo, Lv)
            'End If
            Dim r2Df = ChokukisoR2Df(pNo)
            Dim Qd = ChokukisoQdLimit(pNo)
            Dim Rvd As Single
            Dim qvd As Single
            For Each ch In OutputInfo.OutChokukiso
                If ch.JoNo = pNo Then
                    Dim j As Integer = Math.Min(istep + 1, ch.ChokuItem.Count - 1)
                    qvd = ch.ChokuItem(j).qvd
                    Rvd = ch.ChokuItem(j).Rvd
                    Exit For
                End If
            Next
            Select Case Lv
                Case 1
                    'L1地震動
                    qvd = qvd * 0.83
                    Rvd = Math.Min(qvd + r2Df, Qd) * A
                    Return Rvd
                Case 2 'L2地震動
                    Return Rvd
                Case Else
                    Throw New Exception("Lv の値がありません")
            End Select
        Else
            For Each ch In Chokukiso
                If ch.JoNo = pNo Then
                    Return ch.Lv1(istep).Rvd
                End If
            Next
        End If
        Return Double.NaN

    End Function

    ''' <summary>直接基礎 安定計算用の水平支持力を求める</summary>
    ''' <param name="istep">ステップ</param>
    ''' <param name="pNo">節点番号</param>
    ''' <param name="Lv">
    ''' 1:復旧性レベル1
    ''' 2:復旧性レベル2
    ''' 3:安全性
    ''' </param>
    ''' <remarks>直接基礎 安定計算用の水平支持力</remarks>
    Public Function ChokukisoRhd(istep As Integer, pNo As Integer, Lv As Integer) As Double
        If IsNothing(Chokukiso) OrElse Chokukiso.Count = 0 Then
            Dim Rhp As Single = 0
            ''対象基礎以外のすべてのバネ支点の上限値を水平支持力に加算する。
            ''【令和7年改訂版基礎標準 8.3.2.4 設計水平支持力】より廃止
            'For Each ch In InputInfo.SupportInfo
            '    If ch.IK = 1 Then 'Or ch.IK = 4 Then
            '        If ch.JoNO <> pNo Then
            '            If ch.Rmit1_S < 9999999999 Then
            '                Rhp += ch.Rmit1_S
            '            End If
            '        End If
            '    End If
            'Next
            Dim fr As Single
            If Lv = 1 Then
                fr = 0.83 'L1地震動
            Else
                fr = 1 'L2地震動
            End If
            For Each ch In InputInfo.SupportInfo
                If ch.JoNO = pNo AndAlso ch.IK = 6 Then
                    '【H24基礎標準(解 13.2.2.4-2) p152】
                    'Rhb = Vd tanδb + A' c'
                    Dim Vd = ChokukisoVd(istep, pNo, Lv)
                    Dim δb = ch.FAI
                    Dim tanδb = Math.Tan((Math.PI / 180) * δb)
                    Dim A = ChokukisoA(istep, pNo, Lv)
                    Dim c = ChokukisoC(istep, pNo, Lv)
                    Select Case ch.Soil
                        Case 1 '砂質土
                            Return fr * (Rhp + Vd * tanδb)
                        Case 2 '粘性土
                            Return fr * (Rhp + A * c)
                        Case 3 '砂質土と粘性土の相互層
                            Return fr * (Rhp + Math.Min(Vd * tanδb, A * c))
                        Case 4 '岩盤
                            Return fr * (Rhp + Vd * tanδb + A * c)
                    End Select
                End If
            Next
        Else
            For Each ch In Chokukiso
                If ch.JoNo = pNo Then
                    Select Case Lv
                        Case 1
                            Return ch.Lv1(istep).Rhd
                        Case 2
                            Return ch.Lv2(istep).Rhd
                        Case 3
                            Return ch.Lv3(istep).Rhd
                    End Select
                End If
            Next
        End If
        Return Double.NaN
    End Function

    ''' <summary>直接基礎 安定計算用の最大抵抗ﾓｰﾒﾝﾄを求める</summary>
    ''' <param name="istep">ステップ</param>
    ''' <param name="pNo">節点番号</param>
    ''' <param name="Lv">
    ''' 1:復旧性レベル1
    ''' 2:復旧性レベル2
    ''' 3:安全性
    ''' </param>
    ''' <remarks>直接基礎 安定計算用の最大抵抗ﾓｰﾒﾝﾄ</remarks>
    Public Function ChokukisoMmd(istep As Integer, pNo As Integer, Lv As Integer) As Double
        If IsNothing(Chokukiso) OrElse Chokukiso.Count = 0 Then
            Dim r2Df = ChokukisoR2Df(pNo)
            Dim Vd = ChokukisoVd(istep, pNo, Lv)
            Dim B = ChokukisoB(pNo)
            Dim L = ChokukisoL(pNo)
            Dim qvd As Single
            Dim qd As Single
            Dim Mmd As Single
            For Each ch In OutputInfo.OutChokukiso
                If ch.JoNo = pNo Then
                    Dim j As Integer = Math.Min(istep + 1, ch.ChokuItem.Count - 1)
                    qvd = ch.ChokuItem(j).qvd
                    Mmd = ch.ChokuItem(j).Mmd
                    Exit For
                End If
            Next
            Select Case Lv
                Case 1
                    'L1地震動
                    qvd = qvd * 0.83
                    qd = qvd + r2Df
                    Mmd = (B * Vd / 2) - ((Vd ^ 2) / (2 * qd * L))
                    Return Mmd
                Case 2 'L2地震動
                    Return Mmd
                Case Else
                    Throw New Exception("Lv の値がありません")
            End Select
        Else
            For Each ch In Chokukiso
                If ch.JoNo = pNo Then
                    Return ch.Lv1(istep).Mmd
                End If
            Next
        End If
        Return Double.NaN
    End Function

    ''' <summary>直接基礎 安定計算用の底面塑性化率の制限値を求める</summary>
    ''' <param name="istep">ステップ</param>
    ''' <param name="pNo">節点番号</param>
    ''' <param name="Lv">
    ''' 2:復旧性レベル2
    ''' 3:安全性
    ''' </param>
    ''' <remarks>直接基礎 安定計算用の底面塑性化率の制限値</remarks>
    Public Function ChokukisoXFLd(istep As Integer, pNo As Integer, Lv As Integer) As Double
        If IsNothing(Chokukiso) OrElse Chokukiso.Count = 0 Then
            Dim B = ChokukisoB(istep, pNo, Lv)
            Select Case Lv
                Case 2
                    Return 16.7 'B / 6
                Case 3
                    Return 25 'B / 4
            End Select
        Else
            For Each ch In Chokukiso
                If ch.JoNo = pNo Then
                    Select Case Lv
                        Case 2
                            Return ch.Lv2(istep).限界値V
                        Case 3
                            Return ch.Lv3(istep).限界値V
                    End Select
                End If
            Next
        End If
        Return Double.NaN
    End Function

    ''' <summary>直接基礎 安定計算用の底面塑性化率を求める</summary>
    ''' <param name="istep">ステップ</param>
    ''' <param name="pNo">節点番号</param>
    ''' <param name="Lv">
    ''' 2:復旧性レベル2
    ''' 3:安全性
    ''' </param>
    ''' <remarks>直接基礎 安定計算用の底面塑性化率</remarks>
    Public Function ChokukisoXf_B(istep As Integer, pNo As Integer, Lv As Integer) As Double

        If IsNothing(Chokukiso) OrElse Chokukiso.Count = 0 Then
            Dim sosei As Single
            For Each ch In OutputInfo.OutChokukiso

                If ch.JoNo = pNo Then
                    Dim j As Integer = Math.Min(istep + 1, ch.ChokuItem.Count - 1)

                    Dim Vd = Me.ChokukisoVd(j, pNo, 2)
                    Dim Rvd = Me.ChokukisoRvd(j, pNo, 2)
                    If Vd = Double.NaN Or Rvd = Double.NaN Then
                        sosei = ch.ChokuItem(j).Sosei
                    Else
                        If Vd < Rvd Then
                            sosei = ch.ChokuItem(j).Sosei
                        Else
                            ' Rvd到達以降は、qdの算出だけRvd到達ステップの応答値で計算するようにして、照査する際の応答値は当該ステップを使用して照査しています。
                            For i = j To 0 Step -1
                                Dim qd = ch.ChokuItem(i).qd
                                Vd = Me.ChokukisoVd(i, pNo, 2)
                                Dim L = ChokukisoL(pNo)
                                Dim B = ChokukisoB(pNo)
                                sosei = Vd / (qd * L * B)
                                Exit For
                                'End If
                            Next
                        End If
                    End If

                    Return sosei * 100

                End If
            Next
        Else
            For Each ch In Chokukiso
                If ch.JoNo = pNo Then
                    Select Case Lv
                        Case 2
                            Return ch.Lv2(istep).Xf_B
                        Case 3
                            Return ch.Lv3(istep).Xf_B
                    End Select
                End If
            Next
        End If
        Return Double.NaN
    End Function

    ''' <summary>直接基礎 安定計算用の基礎幅(底面塑性化率用)を求める</summary>
    ''' <param name="istep">ステップ</param>
    ''' <param name="pNo">節点番号</param>
    ''' <param name="Lv">
    ''' 2:復旧性レベル2
    ''' 3:安全性
    ''' </param>
    ''' <remarks>直接基礎 安定計算用の基礎幅(底面塑性化率用)</remarks>
    Public Function ChokukisoB(istep As Integer, pNo As Integer, Lv As Integer) As Double
        If IsNothing(Chokukiso) OrElse Chokukiso.Count = 0 Then
            Return ChokukisoB(pNo)
        Else
            For Each ch In Chokukiso
                If ch.JoNo = pNo Then
                    Select Case Lv
                        Case 2
                            Return ch.Lv2(istep).Xf_B * ch.Lv2(istep).B
                        Case 3
                            Return ch.Lv3(istep).Xf_B * ch.Lv3(istep).B
                    End Select
                End If
            Next
        End If
        Return 0
    End Function
    ''' <summary>直接基礎 安定計算用の作用方向基礎幅を求める</summary>
    ''' <param name="pNo">節点番号</param>
    Public Function ChokukisoB(pNo As Integer) As Double
        For Each ch In InputInfo.SupportInfo
            If ch.JoNO = pNo AndAlso ch.IK = 6 Then
                Return ch.B
            End If
        Next
        Return 0
    End Function
    ''' <summary>直接基礎 安定計算用の直角方向基礎幅を求める</summary>
    ''' <param name="pNo">節点番号</param>
    Public Function ChokukisoL(pNo As Integer) As Double
        For Each ch In InputInfo.SupportInfo
            If ch.JoNO = pNo AndAlso ch.IK = 6 Then
                Return ch.L
            End If
        Next
        Return 0
    End Function
    ''' <summary>直接基礎 安定計算用のγ2・Dfを求める</summary>
    ''' <param name="pNo">節点番号</param>
    Public Function ChokukisoR2Df(pNo As Integer) As Double
        For Each ch In InputInfo.SupportInfo
            If ch.JoNO = pNo AndAlso ch.IK = 6 Then
                Return ch.GAM2 * ch.DF
            End If
        Next
        Return 0
    End Function
    ''' <summary>直接基礎 岩盤用の設計鉛直支持力度の制限値qd(kN/m2)</summary>
    ''' <param name="pNo">節点番号</param>
    Public Function ChokukisoQdLimit(pNo As Integer) As Double
        Dim result As Double = 0
        For Each ch In InputInfo.SupportInfo
            If ch.JoNO = pNo AndAlso ch.IK = 6 Then
                result = ch.Qd
                Exit For
            End If
        Next
        If result <= 0 Then
            result = Double.MaxValue
        End If
        Return result
    End Function
    ''' <summary>直接基礎 安定計算用の基礎面積(底面塑性化率用)を求める</summary>
    ''' <param name="istep">ステップ</param>
    ''' <param name="pNo">節点番号</param>
    ''' <param name="Lv">
    ''' 2:復旧性レベル1
    ''' 2:復旧性レベル2
    ''' 3:安全性
    ''' </param>
    ''' <remarks>直接基礎 安定計算用の基礎面積(底面塑性化率用)</remarks>
    Public Function ChokukisoA(istep As Integer, pNo As Integer, Lv As Integer, Optional LL As Double = 0) As Double
        If IsNothing(Chokukiso) OrElse Chokukiso.Count = 0 Then
            For Each ch In OutputInfo.OutChokukiso
                If ch.JoNo = pNo Then
                    'A = Be * L
                    'Be = B - 2ex
                    'ex = M/V
                    Dim M = ChokukisoMd(istep, pNo, Lv)
                    Dim V = ChokukisoVd(istep, pNo, Lv)
                    Dim ex = M / V
                    Dim B = ChokukisoB(istep, pNo, Lv)
                    Dim Be = B - 2 * ex
                    Dim L As Double
                    If LL = 0 Then
                        L = ChokukisoL(pNo)
                    Else
                        L = LL
                    End If
                    Return Be * L
                End If
            Next
        Else
            For Each ch In Chokukiso
                If ch.JoNo = pNo Then
                    Select Case Lv
                        Case 1
                            Return ch.Lv1(istep).Av
                        Case 2
                            Return ch.Lv2(istep).Av
                        Case 3
                            Return ch.Lv3(istep).Av
                    End Select
                End If
            Next
        End If
        Return 0
    End Function

    ''' <summary>直接基礎 安定計算用の粘着力を求める</summary>
    ''' <param name="istep">ステップ</param>
    ''' <param name="pNo">節点番号</param>
    ''' <param name="Lv">
    ''' 2:復旧性レベル1
    ''' 2:復旧性レベル2
    ''' 3:安全性
    ''' </param>
    ''' <remarks>直接基礎 安定計算用の粘着力</remarks>
    Public Function ChokukisoC(istep As Integer, pNo As Integer, Lv As Integer) As Double
        If IsNothing(Chokukiso) OrElse Chokukiso.Count = 0 Then
            For Each ch In InputInfo.SupportInfo
                If ch.JoNO = pNo AndAlso ch.IK = 6 Then
                    Return ch.C
                End If
            Next
        Else
            For Each ch In Chokukiso
                If ch.JoNo = pNo Then
                    Select Case Lv
                        Case 1
                            Return ch.Lv1(istep).Cv
                        Case 2
                            Return ch.Lv2(istep).Cv
                        Case 3
                            Return ch.Lv3(istep).Cv
                    End Select
                End If
            Next
        End If
        Return 0
    End Function

    ''' <summary>直接基礎 安定計算用の回転角を求める</summary>
    ''' <param name="istep">ステップ</param>
    ''' <param name="pNo">節点番号</param>
    ''' <param name="Lv">
    ''' 2:復旧性レベル2
    ''' 3:安全性
    ''' </param>
    ''' <remarks>直接基礎 安定計算用の回転角</remarks>
    Public Function Chokukisoθd(istep As Integer, pNo As Integer, Lv As Integer) As Double
        If IsNothing(Chokukiso) OrElse Chokukiso.Count = 0 Then
            Return OutputInfo.StepCtrl(istep).JointItem(pNo).DeltaT * 1000
        Else
            For Each ch In Chokukiso
                If ch.JoNo = pNo Then
                    Select Case Lv
                        Case 2
                            Return ch.Lv2(istep).回転角
                        Case 3
                            Return ch.Lv3(istep).回転角
                    End Select
                End If
            Next
        End If
        Return Double.NaN
    End Function

    ''' <summary>直接基礎 安定計算用の回転角の限界値</summary>
    ''' <param name="istep">ステップ</param>
    ''' <param name="pNo">節点番号</param>
    ''' <param name="Lv">
    ''' 2:復旧性レベル2
    ''' 3:安全性
    ''' </param>
    ''' <remarks>直接基礎 安定計算用の回転角の限界値</remarks>
    Public Function ChokukisoθL(istep As Integer, pNo As Integer, Lv As Integer) As Double
        If IsNothing(Chokukiso) OrElse Chokukiso.Count = 0 Then
            Select Case Lv
                Case 2
                    Return 20
                Case 3
                    Return 30
            End Select
        Else
            For Each ch In Chokukiso
                If ch.JoNo = pNo Then
                    Select Case Lv
                        Case 2
                            Return ch.Lv2(istep).限界値R
                        Case 3
                            Return ch.Lv3(istep).限界値R
                    End Select
                End If
            Next
        End If
        Return Double.NaN
    End Function
#End Region

End Class

'''<summary>
''' 残留変位のパラメータ
''' 復旧性レベル１
''' </summary>
Public Class 直接基礎React1
    Inherits 直接基礎React
    Public Rvd As Double
    Public Vd_Rvd As Double

    Public Md As Double
    Public Mmd As Double
    Public Md_Mmd As Double

    Sub New(Items() As String)
        istep = Items(0)
        種別 = Items(1)
        fr = Items(2)
        B = Items(3)
        L = Items(4)
        Be1 = Items(5)
        Be2 = Items(6)
        Df = Items(7)
        γe1 = Items(8)
        γe2 = Items(9)
        δ = Items(10)
        φ = Items(11)
        Cv = Items(12)
        Ic = Items(13)
        Iγ = Items(14)
        Iq = Items(15)
        αb = Items(16)
        βb = Items(17)
        Nc = Items(18)
        Nγ = Items(19)
        Nq = Items(20)
        qvd = Items(21)
        qd = Items(22)
        Av = Items(23)
        Vd = Items(24)
        Rvd = Items(25)
        Vd_Rvd = Items(26)

        Hd = Items(28)
        δb = Items(29)
        Ah = Items(30)
        Ch = Items(31)
        Rhb = Items(32)
        Rhp = Items(33)
        Rhd = Items(34)
        Hd_Rhd = Items(35)
        Md = Items(36)
        Mmd = Items(37)
        Md_Mmd = Items(38)
    End Sub

End Class

'''<summary>
''' 残留変位のパラメータ
''' 復旧性レベル２
''' 安全性
''' </summary>
Public Class 直接基礎React2
    Inherits 直接基礎React
    Public Xf_B As Double
    Public 限界値V As Double
    Public 安全度V As Double

    Public 回転角 As Double
    Public 限界値R As Double
    Public 安全度R As Double

    Sub New(Items() As String)
        istep = Items(0)
        種別 = Items(1)
        fr = Items(2)
        B = Items(3)
        L = Items(4)
        Be1 = Items(5)
        Be2 = Items(6)
        Df = Items(7)
        γe1 = Items(8)
        γe2 = Items(9)
        δ = Items(10)
        φ = Items(11)
        Cv = Items(12)
        Ic = Items(13)
        Iγ = Items(14)
        Iq = Items(15)
        αb = Items(16)
        βb = Items(17)
        Nc = Items(18)
        Nγ = Items(19)
        Nq = Items(20)
        qvd = Items(21)
        qd = Items(22)
        Av = Items(23)
        Vd = Items(24)
        Xf_B = Items(25)
        限界値V = Items(26)
        安全度V = Items(27)

        Hd = Items(28)
        δb = Items(29)
        Ah = Items(30)
        Ch = Items(31)
        Rhb = Items(32)
        Rhp = Items(33)
        Rhd = Items(34)
        Hd_Rhd = Items(35)
        回転角 = Items(36)
        限界値R = Items(37)
        安全度R = Items(38)
    End Sub
End Class

'''<summary>
''' 残留変位のパラメータ基本クラス
''' </summary>
Public Class 直接基礎React
    Public istep As Integer
    '鉛直変位
    Public 種別 As Integer
    Public fr As Double
    Public B As Double
    Public L As Double
    Public Be1 As Double
    Public Be2 As Double
    Public Df As Double
    Public γe1 As Double
    Public γe2 As Double
    Public δ As Double
    Public φ As Double
    Public Cv As Double
    Public Ic As Double
    Public Iγ As Double
    Public Iq As Double
    Public αb As Double
    Public βb As Double
    Public Nc As Double
    Public Nγ As Double
    Public Nq As Double
    Public qvd As Double
    Public qd As Double
    Public Av As Double
    Public Vd As Double
    '水平変位
    Public Hd As Double
    Public δb As Double
    Public Ah As Double
    Public Ch As Double
    Public Rhb As Double
    Public Rhp As Double
    Public Rhd As Double
    Public Hd_Rhd As Double
End Class