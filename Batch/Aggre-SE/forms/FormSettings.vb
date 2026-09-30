Public Class FormSettings

    ' オプション購入機能

    ''' <summary>杭の抵抗モーメント図作成機能</summary>
    Friend Shared Property Option_杭の抵抗モーメント図作成機能 As Integer = 0

    ''' <summary>ユーザーが決めた任意の応答塑性率の直接入力機能</summary>
    Friend Shared Property Option_応答塑性率の直接入力 As Integer = 0

    ''' <summary>液状化による低減係数を考慮する連携機能</summary>
    Friend Shared Property Option_液状化による低減係数を考慮する連携機能 As Integer = 0

    ''' <summary>総研設計標準以外のせん断耐力計算機能</summary>
    Friend Shared Property Option_総研設計標準以外のせん断耐力 As Integer = 0

    ''' <summary>
    ''' <para>不整形地盤の影響オプション</para>
    ''' <para>0・・・・ 不整形地盤の影響を考慮しない</para>
    ''' <para>1・・・・ 不整形地盤の影響を考慮する</para>
    ''' <para>2・・・・ 液状化の場合は、不整形地盤の影響を考慮しない</para>
    ''' </summary>
    Friend Shared Property Option_不整形地盤 As Integer = 0


    ''' <summary> G0～G5地盤以外に任意のスペクトル </summary>
    Friend Shared Property Option_任意スペクトル As Integer = 0







    ' 購入機能以外のオプション


    ''' <summary>デバッグ</summary>
    Public Shared Property バージョンチェック As Boolean = True

    ''' <summary>解析ケース数</summary>
    Public Shared Property Option_ケース数 As Integer = 50

    ''' <summary>
    ''' 独自せん断耐力の計算オプション
    ''' <para>運輸機構 10の位</para>
    ''' <para>ＪＲ東 1の位</para>
    ''' <para>両方ともONにする =11</para>
    ''' </summary>
    Public Shared Property Option_カスタムせん断耐力 As Integer = 0

    ''' <summary>SNAPファイルを相対パスで記録するオプション</summary>
    Public Shared Property Option_相対パス As Integer = 1


    ''' <summary>ピックアップファイルを作成するケースを選択するオプション</summary>
    Friend Shared Property Option_PickUpFileCase As Integer = 0

    ''' <summary>各解析ケースの照査表を出力するオプション</summary>
    Friend Shared Property Option_各解析ケースの結果出力 As Integer = 0

    ''' <summary>荷重変位曲線にせん断降伏点を出力するオプション</summary>
    Friend Shared Property Option_せん断降伏点の出力 As Integer = 0

    ''' <summary>杭の抵抗モーメント図に1.5倍の線を描くオプション</summary>
    Friend Shared Property Option_杭の抵抗モーメント図に15倍の線を描く As Integer = 0

    ''' <summary>段落し前の杭の抵抗モーメントを 杭頭と同じ断面で耐力線を描く</summary>
    Friend Shared Property Option_杭の抵抗モーメント図は杭頭本数 As Integer = 0

    ''' <summary>段落し前の杭の抵抗モーメントを 杭頭と同じ断面で耐力線を描く</summary>
    Friend Shared Property Option_杭頭の軸力でモーメント図を描く As Integer = 0

    ''' <summary>応答値をL2弾性応答加速度を最大震度とする</summary>
    Friend Shared Property Option_弾性応答加速度L2 As Integer = 0

    ''' <summary>総括表標準出力の時は１部材１列</summary>
    Friend Shared Property Option_標準総括表は１列 As Integer = 1

    ''' <summary>初期降伏震度を用いた応答変位</summary>
    Friend Shared Property Option_初期降伏震度を用いた応答変位 As Integer = 0

    ''' <summary>指定ステップを応答値とする</summary>
    Friend Shared Property Option_指定ステップを応答値とする As Integer = 1

    ''' <summary>指定ステップを応答値とする</summary>
    Friend Shared Property Option_大量解析プログラム As Integer = 0

    ''' <summary>液状化スペクトルⅠとⅡを明確に分ける</summary>
    Friend Shared Property Option_液状化スペクトルⅠⅡ As Integer = 0

    Public Shared ReadOnly Property ReadOnlyColor As Color
        Get
            Return SystemColors.Control
        End Get
    End Property

    Public Shared ReadOnly Property HighlightColor As Color
        Get
            Return SystemColors.Highlight
        End Get
    End Property
    Public Shared ReadOnly Property HighlightTextColor As Color
        Get
            Return SystemColors.HighlightText
        End Get
    End Property

    Public Shared ReadOnly Property ControlTextColor As Color
        Get
            Return SystemColors.ControlText
        End Get
    End Property
    Public Shared ReadOnly Property WindowColor As Color
        Get
            Return SystemColors.Window
        End Get
    End Property
#Region "ファイルの履歴"

    Private Const RecentFilesCount As Integer = 5

    Private Shared _RecentFiles As New List(Of String)

    Public Shared Function GetRecentFileList() As List(Of String)
        Return _RecentFiles
    End Function

    Public Shared ReadOnly Property GetRecentFile(id As Integer) As String
        Get
            If _RecentFiles.Count > id Then
                Return _RecentFiles(id)
            Else
                Return ""
            End If
        End Get
    End Property


    Public Shared Function RecentFile_Add(FileName As String) As Boolean

        Dim result As Boolean = False

        If FileName Is Nothing Then Return False
        If FileName.Trim.Length = 0 Then Return False

        '重複するファイルの履歴は消去する。
        Try
            _RecentFiles.Remove(FileName)
        Catch ex As Exception
        End Try

        Try
            '最大保持数を超えた場合は先頭行を消去
            If _RecentFiles.Count >= RecentFilesCount Then
                _RecentFiles.RemoveAt(0)
            End If
            _RecentFiles.Add(FileName)
            result = True
        Catch ex As Exception
            result = False
        End Try
        Return result
    End Function

    Public Shared Sub SaveRecentFiles()
        Dim RecentFileName As String = common.TempPath + "\RecentFiles.txt"
        Try
            Dim Value As String = ListOfStringToGetXmlData(_RecentFiles)
            '書き込むファイルが既に存在している場合は、上書きする
            Dim sw As New System.IO.StreamWriter(RecentFileName,
                False,
                System.Text.Encoding.GetEncoding("shift_jis"))
            'resultの内容を書き込む
            sw.Write(Value)
            '閉じる
            sw.Close()
        Catch ex As Exception

        End Try
    End Sub

    Public Shared Function ReadRecentFileList() As List(Of String)
        Dim result As New List(Of String)
        Try
            Dim RecentFileName As String = common.TempPath + "\RecentFiles.txt"
            If System.IO.File.Exists(RecentFileName) Then
                Dim sr As New System.IO.StreamReader(RecentFileName,
                    System.Text.Encoding.GetEncoding("shift_jis"))
                Dim s As String = sr.ReadToEnd()
                sr.Close()

                _RecentFiles = GetXmDataToListOfString(s)
                For Each FilePath In _RecentFiles
                    result.Add(FilePath)
                Next
                Return result
            End If
        Catch ex As Exception
        End Try
        Return result
    End Function

    Private Shared Function ListOfStringToGetXmlData(value As List(Of String)) As String
        Dim InXmldata As New XmlReader(Of List(Of String))
        InXmldata.Data = value
        Return InXmldata.XmlData
    End Function

    Private Shared Function GetXmDataToListOfString(value As String) As List(Of String)
        Dim InXmldata As New XmlReader(Of List(Of String))
        InXmldata.XmlData = value
        Return InXmldata.Data
    End Function

#End Region

End Class

