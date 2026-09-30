Public Class Input

    Public Shared DATAROWS As Integer = FormSettings.Option_ケース数

    Public Shared FileName As String


    Public Shared Function GetFileName() As String
        Try
            Return System.IO.Path.GetFileName(FileName)
        Catch ex As Exception
            Return FileName
        End Try
    End Function

    ''' <summary>
    ''' true  :データが変更されたので保存が必要
    ''' false :データが変更されていないので保存不要
    ''' </summary>
    ''' <remarks></remarks>
    Public Shared _ChengeDataFlg As Boolean
    Public Shared Property ChengeDataFlg As Boolean
        Get
            Return _ChengeDataFlg
        End Get
        Set(value As Boolean)
            _ChengeDataFlg = value
            '値が変更されたことをイベントで通知
            OnStatasChangedChanged(EventArgs.Empty)
        End Set
    End Property

    ''' <summary>
    ''' true  :データを読み込んだことがあるので読み込み不要
    ''' false :データを読み込んだことがないので読み込みが必要
    ''' </summary>
    ''' <remarks></remarks>
    Private Shared _ReadDBFlg As Boolean = False
    Public Shared Property ReadDBFlg As Boolean
        Get
            Return _ReadDBFlg
        End Get
        Set(value As Boolean)
            _ReadDBFlg = value
            '値が変更されたことをイベントで通知
            OnStatasChangedChanged(EventArgs.Empty)
        End Set
    End Property
    'OnReadDBFlgChanged イベントを発生させます。
    Private Shared Sub OnStatasChangedChanged(e As EventArgs)
        'Shared イベントであるためイベント発生元インスタンスは Nothing
        RaiseEvent StatasChanged(Nothing, e)
    End Sub

    'ReadDBFlg プロパティの値が変更されたときに発生します。
    Public Shared Event StatasChanged As EventHandler



    Public Shared Data As New Datas

    Public Shared Sub Clear()
        Data = New Datas
        ReadDBFlg = False
    End Sub

    Public Shared Function GetXmlData() As String
        Dim InXmldata As New XmlReader(Of Datas)
        Data.Version = System.Diagnostics.FileVersionInfo.GetVersionInfo(
                                 System.Reflection.Assembly.GetExecutingAssembly().Location).FileVersion

        InXmldata.Data = Data
        Return InXmldata.XmlData
    End Function

    Public Shared Sub SetXmData(value As String)
        Dim InXmldata As New XmlReader(Of Datas)
        InXmldata.XmlData = value
        Data = InXmldata.Data

        '読み込んだ後データの最大数が小さくなるとき ReDim する
        Data.SNAP.checkSnapDBlength(Data.CaseName.analysisObject.Length)

    End Sub

    ''' <summary>
    ''' バージョンの変換をする
    ''' </summary>
    Friend Shared Sub applyVersion()

        Dim ver = New Version()
        If Not IsNothing(Input.Data.Version) Then
            ver = New Version(Input.Data.Version)
        End If

        '基礎のver1.1.0.11 から
        If ver < New Version("1.1.0.11") Then
            For Each f In Data.Foundation.基礎照査
                Dim tmp0(0 To 30) As String '杭基礎の制限値
                For i = 0 To 6
                    tmp0(i) = f.dgLimitValue0(i)
                Next
                For i = 7 To 16
                    tmp0(i) = f.dgLimitValue0(i + 1)
                Next
                f.dgLimitValue0 = tmp0
                Dim tmp1(0 To 10) As String '直接基礎の制限値
                For i = 0 To 2
                    tmp1(i) = f.dgLimitValue1(i)
                Next
                For i = 3 To 5
                    tmp1(i) = f.dgLimitValue1(i + 1)
                Next
                For i = 6 To 8
                    tmp1(i) = f.dgLimitValue1(i + 2)
                Next
                f.dgLimitValue1 = tmp1
                '変位の着目点
                If f.FoundationType = 0 Then
                    f.dgDispAtPoints = New List(Of clsAtPoints)
                    f.dgDispAtPoints.Add(f.dgDispAtPoint)
                    f.dgDispAtPoint = New clsAtPoints
                End If
            Next
        End If
    End Sub
End Class

Public Class Datas

    Public cmd As Integer = -1

    Public Version As String

    Sub Datas()
        Dim ver = System.Diagnostics.FileVersionInfo.GetVersionInfo(
                    System.Reflection.Assembly.GetExecutingAssembly().Location)
        Version = ver.FileVersion
    End Sub
    Public CaseName As New CaseNameClass
    Public SNAP As New SNAPClass
    Public Element As New ElementSettingClass
    Public Foundation As New FoundationSettingClass
    Public Requirement As New RequirementClass
    Public Calculation As New CalculationSetting
    Public PileAnchorBar As New PileAnchorBarClass

End Class

''' <summary> インプットデータから XMLファイルを読み書きするクラス </summary>
Friend Class XmlReader(Of T As New)

    Private _Data As New T
    Public Property Data As T
        Get
            Return Me._Data
        End Get
        Set(value As T)
            _Data = value
        End Set
    End Property

    Public Property XmlData As String
        Get
            Try
                Dim serializer1 As _
                    New System.Xml.Serialization.XmlSerializer(GetType(T))
                Using writer As New System.IO.StringWriter
                    serializer1.Serialize(writer, Me._Data)
                    Return writer.ToString()
                End Using
            Catch ex As Exception
                Return ex.Message
            End Try
        End Get
        Set(value As String)
            Try
                Dim serializer2 As _
                        New System.Xml.Serialization.XmlSerializer(GetType(T))
                Using reader As New System.IO.StringReader(value)
                    Me._Data = serializer2.Deserialize(reader)
                End Using
            Catch ex As Exception
                Throw ex
            End Try
        End Set
    End Property

End Class
