Imports System.Windows

Public Class SelectFoundationDB
    Public FileName As String

    Public Lv1Rvd As Double
    Public Lv1Rud As Double
    Public Lv1δL As Double
    Public Lv1θL As Double

    Public Lv2Rvd As Double
    Public Lv2δL As Double
    Public Lv2θL As Double

    Public AzenRvd As Double
    Public AzenδL As Double
    Public AzenθL As Double


    Private CsvItems As List(Of String())                'CSVの各項目を表す配列

    Sub New(_FileName As String)
        InitializeComponent()
        Me.FileName = _FileName
        Me.Lv1Rvd = 0
        Me.Lv1Rud = 0
        Me.Lv1δL = 0
        Me.Lv1θL = 0
        Me.Lv2Rvd = 0
        Me.Lv2δL = 0
        Me.Lv2θL = 0
        Me.AzenRvd = 0
        Me.AzenδL = 0
        Me.AzenθL = 0
    End Sub

    Private Sub SelectFoundationDB_Load(sender As Object, e As EventArgs) Handles Me.Load
        'ファイルの読み込みに失敗したら中止を返す
        If ReadSdrFile(Me.FileName) = False Then
            Me.DialogResult = Forms.DialogResult.Abort
            Me.Close()
            Return
        End If
        '画面の初期化
        Me.ComboBox1.SelectedIndex = 0
        Me.LabelVer.Text = Me.CsvItems(0)(0)
        Me.LabelTitle.Text = Me.CsvItems(1)(0)

        Dim CsvItems2 = Me.CsvItems(2)
        Dim CsvItems3 = Me.CsvItems(3)

        Me.FpSpread1_Sheet1.RowCount = 8
        Dim Cells = Me.FpSpread1_Sheet1.Cells
        Cells(0, 0).Value = CsvItems2(0)
        Cells(1, 0).Value = CsvItems2(1)
        Cells(2, 0).Value = CsvItems2(2)
        Cells(3, 0).Value = CsvItems2(3)
        Cells(4, 0).Value = CsvItems2(4)
        Cells(5, 0).Value = CsvItems2(5)
        Cells(6, 0).Value = CsvItems2(6)
        Cells(7, 0).Value = CsvItems2(7)

        Cells(0, 1).Value = CsvItems3(0)
        Cells(1, 1).Value = CsvItems3(1)
        Cells(2, 1).Value = CsvItems3(2)
        Cells(3, 1).Value = CsvItems3(3)
        Cells(4, 1).Value = CsvItems3(4)
        Cells(5, 1).Value = CsvItems3(5)
        Cells(6, 1).Value = CsvItems3(6)
        Cells(7, 1).Value = CsvItems3(7)

    End Sub


    'Sdrファイルの読み込み
    Private Function ReadSdrFile(FileName As String) As Boolean
        Try
            'ファイルが存在しなければFalseを返す
            If System.IO.File.Exists(Me.FileName) = False Then Return False
            'ファイルの読み込み
            Me.CsvItems = New List(Of String())
            Using Reader As New IO.StreamReader(FileName, System.Text.Encoding.GetEncoding("Shift-JIS"))
                Dim Line As String = Reader.ReadLine    'CSVの最初の一行
                Do Until IsNothing(Line)
                    Me.CsvItems.Add(Line.Split(","))    '一行を, (カンマ)で区切って項目ごとに分解
                    Line = Reader.ReadLine              '次の行を読み込む。
                Loop
            End Using
            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function

    Private Sub OKButton_Click(sender As Object, e As EventArgs) Handles OKButton.Click
        Try
            Me.DialogResult = SetValue()
        Catch ex As Exception
            Me.DialogResult = Forms.DialogResult.Abort
        Finally
            Me.Close()
        End Try

    End Sub

    Private Sub CancelButton_Click(sender As Object, e As EventArgs) Handles CancelBtn.Click
        Me.DialogResult = Forms.DialogResult.Cancel
        Me.Close()
    End Sub

    Private Function SetValue() As Integer
        Dim result As Integer = Forms.DialogResult.Abort
        Try
            Dim row As Integer = 0
            Dim tmp As Integer = 0
            Dim ver53 = False

            Select Case Me.ComboBox1.SelectedIndex
                Case 0 '橋軸方向
                    row = SerchRow(Me.CsvItems, "橋軸方向", row)
                Case 1 '橋軸直角方向
                    row = SerchRow(Me.CsvItems, "直角方向", row)
            End Select

            ' ---------------------------------------------------------------
            row = SerchRow(Me.CsvItems, "設計鉛直支持力の算定", row)

            tmp = SerchRow(Me.CsvItems, "残留鉛直変位(L1)", row)
            row = IIf(tmp > 0, tmp, SerchRow(Me.CsvItems, "安定レベル1(地震時)", row)) ' ver 5.3 対応
            Me.Lv1Rvd = Me.CsvItems(row)(6)

            tmp = SerchRow(Me.CsvItems, "残留鉛直変位(L2)", row)
            row = IIf(tmp > 0, tmp, SerchRow(Me.CsvItems, "安定レベル2", row)) ' ver 5.3 対応
            Me.Lv2Rvd = Me.CsvItems(row)(6)

            tmp = SerchRow(Me.CsvItems, "安定レベル3", row) ' ver 5.3 対応
            If tmp > 0 Then
                row = tmp
                Me.AzenRvd = Me.CsvItems(row)(6)
                ver53 = True
            End If

            ' ---------------------------------------------------------------
            row = SerchRow(Me.CsvItems, "設計引き抜き抵抗力の算定", row)

            tmp = SerchRow(Me.CsvItems, "残留鉛直変位(L1)", row)
            row = IIf(tmp > 0, tmp, SerchRow(Me.CsvItems, "安定レベル1(地震時)", row)) ' ver 5.3 対応
            Me.Lv1Rud = IIf(UBound(Me.CsvItems(row), 1) > 5, Me.CsvItems(row)(5), Me.CsvItems(row)(4))


            ' ---------------------------------------------------------------
            row = SerchRow(Me.CsvItems, "水平変位の照査", row)

            tmp = SerchRow(Me.CsvItems, "残留鉛直変位(L1)", row)
            row = IIf(tmp > 0, tmp, SerchRow(Me.CsvItems, "安定レベル1(地震時)", row)) ' ver 5.3 対応
            Me.Lv1δL = Me.CsvItems(row)(1)

            tmp = SerchRow(Me.CsvItems, "残留鉛直変位(L2)", row)
            row = IIf(tmp > 0, tmp, SerchRow(Me.CsvItems, "安定レベル2", row)) ' ver 5.3 対応
            Me.Lv2δL = Me.CsvItems(row)(1)

            tmp = SerchRow(Me.CsvItems, "安定レベル3", row) ' ver 5.3 対応
            If tmp > 0 Then
                row = tmp
                Me.AzenδL = Me.CsvItems(row)(1)
                ver53 = True
            End If

            ' ---------------------------------------------------------------
            row = SerchRow(Me.CsvItems, "回転角の照査", row)

            tmp = SerchRow(Me.CsvItems, "残留鉛直変位(L1)", row)
            row = IIf(tmp > 0, tmp, SerchRow(Me.CsvItems, "安定レベル1(地震時)", row)) ' ver 5.3 対応
            Me.Lv1θL = Me.CsvItems(row)(1)

            tmp = SerchRow(Me.CsvItems, "残留鉛直変位(L2)", row)
            row = IIf(tmp > 0, tmp, SerchRow(Me.CsvItems, "安定レベル2", row)) ' ver 5.3 対応
            Me.Lv2θL = Me.CsvItems(row)(1)

            tmp = SerchRow(Me.CsvItems, "安定レベル3", row) ' ver 5.3 対応
            If tmp > 0 Then
                row = tmp
                Me.AzenθL = Me.CsvItems(row)(1)
                ver53 = True
            End If

            ' ---------------------------------------------------------------
            If ver53 Then
                'ver 5.3 対応
                Dim FileName_withoutPath = System.IO.Path.GetFileName(Me.FileName)
                If InStr(FileName_withoutPath, "液状化") Then
                    '液状化
                    result = Forms.DialogResult.Yes
                Else
                    '液状化以外
                    result = Forms.DialogResult.OK
                End If
                Return result
            End If

            ' ---------------------------------------------------------------
            Dim row1 As Integer = SerchRow(Me.CsvItems, "地震時", row)
            Dim row2 As Integer = SerchRow(Me.CsvItems, "液状化時", row)
            If row1 > 0 Then
                '液状化以外
                result = Forms.DialogResult.OK
                row = row1
            ElseIf row2 > 0 Then
                '液状化
                result = Forms.DialogResult.Yes
                row = row2
            End If

            ' ver 5.3未満 ------------------------------------------------------------
            row = SerchRow(Me.CsvItems, "設計鉛直支持力の算定", row)

            Dim r = SerchRow(Me.CsvItems, "終局時修正係数", row)
            If r <= 0 Then
                row = SerchRow(Me.CsvItems, "先端地盤修正係数", row)
            Else
                row = r
            End If
            row += 1
            Me.AzenRvd = Me.CsvItems(row)(6)

            row = SerchRow(Me.CsvItems, "水平変位の照査", row)
            row = SerchRow(Me.CsvItems, "設計限界値δL", row)
            row += 1
            Me.AzenδL = Me.CsvItems(row)(0)

            row = SerchRow(Me.CsvItems, "回転角の照査", row)
            row = SerchRow(Me.CsvItems, "設計限界値θL", row)
            row += 1
            Me.AzenθL = Me.CsvItems(row)(0)

        Catch ex As Exception
            Throw ex
        End Try

        Return result

    End Function

    Private Function SerchRow(CsvItems As List(Of String()), targetChr As String, Optional StartRow As Integer = 0) As Integer
        For r = StartRow To Me.CsvItems.Count - 1
            If InStr(Me.CsvItems(r)(0), targetChr) Then
                Return r
            End If
        Next
        Return -1
    End Function

End Class