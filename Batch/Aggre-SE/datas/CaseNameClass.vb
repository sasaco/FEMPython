Public Class CaseNameClass

    Public dgCaseList_1(0 To Input.DATAROWS - 1) As Integer
    Public dgCaseList_2(0 To Input.DATAROWS - 1) As Integer
    Public dgCaseList_3(0 To Input.DATAROWS - 1) As Integer
    Public dgCaseList_4(0 To Input.DATAROWS - 1) As Integer
    Public dgCaseList_5(0 To Input.DATAROWS - 1) As Integer
    Public dgCaseList_6(0 To Input.DATAROWS - 1) As Integer
    Public dgCaseList_7(0 To Input.DATAROWS - 1) As Integer
    Public dgCaseList_8(0 To Input.DATAROWS - 1) As Integer
    Public dgCaseList_9(0 To Input.DATAROWS - 1) As Integer

    Public analysisObject(0 To Input.DATAROWS - 1) As String

    Public Property CaseNamePath(index As Integer) As String
        Get
            Return Input.Data.SNAP.SnapDBAbsolutePath(index)
        End Get
        Set(value As String)
            Input.Data.SNAP.SnapDBAbsolutePath(index) = value
        End Set
    End Property

    Public ReadOnly Property CaseName(index As Integer) As String
        Get
            Dim strFilePath As String = CaseNamePath(index)
            If Not strFilePath Is Nothing Or strFilePath <> "" Then
                Return System.IO.Path.GetFileNameWithoutExtension(strFilePath)
            Else
                Return ""
            End If
        End Get
    End Property

    Public ReadOnly Property ρm(index As Integer) As String
        Get
            Try
                If Input.Data.SNAP.SnapDB(index) Is Nothing Then Return ""
                Dim result As Single = Input.Data.SNAP.SnapDB(index).Getρm()
                If result > 0 Then
                    Return result.ToString("F1")
                Else
                    Return ""
                End If
            Catch ex As Exception
                Return ""
            End Try
        End Get
    End Property

    Public ReadOnly Property αf(index As Integer) As String
        Get
            Try
                If Input.Data.SNAP.SnapDB(index) Is Nothing Then Return ""
                Dim result As Single = Input.Data.SNAP.SnapDB(index).Getαf()
                If result > 0 Then
                    Return result.ToString("F1")
                Else
                    Return ""
                End If
            Catch ex As Exception
                Return ""
            End Try
        End Get
    End Property


    ''' <summary>
    ''' ケースに一つでもチェックがついていれば True
    ''' </summary>
    ''' <param name="index"></param>
    ''' <returns></returns>
    Public ReadOnly Property CaseAble(index As Integer) As Boolean
        Get
            Try
                If dgCaseList_1(index) +
                dgCaseList_2(index) +
                dgCaseList_3(index) +
                dgCaseList_4(index) +
                dgCaseList_5(index) +
                dgCaseList_6(index) +
                dgCaseList_7(index) +
                dgCaseList_8(index) +
                dgCaseList_9(index) <> 0 Then

                    Return True
                End If

                Return False

            Catch ex As Exception
                Return False
            End Try
        End Get
    End Property

End Class
