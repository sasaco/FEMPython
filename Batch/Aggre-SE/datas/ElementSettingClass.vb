Public Class ElementSettingClass

    Public Function versionCheck() As Boolean
        Dim ver = Input.Data.SNAP.SnapVersion()
        If (ver.Major >= 5 And ver.Minor >= 2) Or ver.Major = 0 Then
            Return True
        End If
        Return False
    End Function

    Public _dgLimitLevel_SIJI() As Integer ' 端部支持条件
    Public _dgLimitLevel_type() As Integer
    Public _dgLimitLevel_La() As Single
    Public _dgLimitLevel_VydType() As Integer

    Public Property dgLimitLevel_SIJI(Index As Integer) As String
        Get
            Try
                Select Case _dgLimitLevel_SIJI(Index)
                    Case 0
                        Return "単純支持"
                    Case 1
                        Return "片持ち"
                    Case 2
                        Return "両端固定"
                    Case Else
                        Return Nothing
                End Select
            Catch
                Return Nothing
            End Try
        End Get
        Set(value As String)
            Try
                Select Case value.Trim()
                    Case "0"
                        _dgLimitLevel_SIJI(Index) = 0
                    Case "1"
                        _dgLimitLevel_SIJI(Index) = 1
                    Case "2"
                        _dgLimitLevel_SIJI(Index) = 2
                    Case Else
                        _dgLimitLevel_SIJI(Index) = -1
                End Select
            Catch
                '無効な入力
            End Try
        End Set
    End Property

    Public Property dgLimitLevel_typeCount As Integer
        Get
            If IsNothing(_dgLimitLevel_type) Then
                Return 0
            Else
                Return _dgLimitLevel_type.Count
            End If
        End Get
        Set(Count As Integer)
            Dim temp1(0 To Count - 1) As Integer
            Dim temp2(0 To Count - 1) As Single
            Dim temp3(0 To Count - 1) As Integer
            Dim temp4(0 To Count - 1) As Integer : For i = 0 To Count - 1 : temp4(i) = -1 : Next
            Try
                Dim n As Integer = Math.Min(Count, _dgLimitLevel_type.Count)
                For i = 0 To n - 1
                    temp1(i) = _dgLimitLevel_type(i)
                Next
            Catch
            End Try
            Try
                Dim n As Integer = Math.Min(Count, _dgLimitLevel_La.Count)
                For i = 0 To n - 1
                    temp2(i) = _dgLimitLevel_La(i)
                Next
            Catch
            End Try
            Try
                Dim n As Integer = Math.Min(Count, _dgLimitLevel_VydType.Count)
                For i = 0 To n - 1
                    temp3(i) = _dgLimitLevel_VydType(i)
                Next
            Catch
            End Try
            Try
                Dim n As Integer = Math.Min(Count, _dgLimitLevel_SIJI.Count)
                For i = 0 To n - 1
                    temp4(i) = _dgLimitLevel_SIJI(i)
                Next
            Catch
            End Try

            Try
                ReDim _dgLimitLevel_type(0 To Count - 1)
                ReDim _dgLimitLevel_La(0 To Count - 1)
                ReDim _dgLimitLevel_VydType(0 To Count - 1)
                ReDim _dgLimitLevel_SIJI(0 To Count - 1) : For i = 0 To Count - 1 : _dgLimitLevel_SIJI(i) = -1 : Next
                For i = 0 To Count - 1
                    _dgLimitLevel_type(i) = temp1(i)
                    _dgLimitLevel_La(i) = temp2(i)
                    _dgLimitLevel_VydType(i) = temp3(i)
                    _dgLimitLevel_SIJI(i) = temp4(i)
                Next
            Catch
            End Try
        End Set
    End Property

    Public Property dgLimitLevel_type(index As Integer) As String
        Get
            Try
                If _dgLimitLevel_type(index) > 0 Then
                    Return _dgLimitLevel_type(index).ToString("0")
                Else
                    Return ""
                End If
            Catch
                Return ""
            End Try
        End Get
        Set(value As String)
            Try
                ' Nothingではない、かつ空白ではない場合
                If Not value Is Nothing AndAlso Not value.Equals("") Then
                    Dim intvalue As Integer = Convert.ToInt32(value)
                    If intvalue > 0 Then
                        _dgLimitLevel_type(index) = intvalue
                    End If
                End If
            Catch
                '無効な入力
            End Try
        End Set
    End Property

    Public Property dgLimitLevel_La(index As Integer) As String
        Get
            Try
                If _dgLimitLevel_La(index) <> 0 Then
                    Return _dgLimitLevel_La(index).ToString
                Else
                    Return ""
                End If
            Catch
                Return ""
            End Try
        End Get
        Set(value As String)
            Try
                ' Nothingではない、かつ空白ではない場合
                If Not value Is Nothing AndAlso Not value.Equals("") Then
                    Dim intvalue As String = Convert.ToSingle(value)
                    _dgLimitLevel_La(index) = intvalue
                Else
                    _dgLimitLevel_La(index) = 0
                End If
            Catch
                '無効な入力
            End Try
        End Set
    End Property

    Public Property dgLimitLevel_VydType(index As Integer) As Integer
        Get
            Try
                If _dgLimitLevel_VydType(index) > 0 Then
                    Return _dgLimitLevel_VydType(index)
                Else
                    Return 1
                End If
            Catch
                Return 1
            End Try
        End Get
        Set(value As Integer)
            Try
                If value > 0 Then
                    _dgLimitLevel_VydType(index) = value
                End If
            Catch
                '無効な入力
            End Try
        End Set
    End Property

    '制限値の入力
    Public dgLimitLevel_title(0 To 4) As String
    Public _dgLimitLevel(0 To 4) As Integer
    Public Property dgLimitLevel1(index As Integer) As String
        Get
            If _dgLimitLevel(index) = 0 Then
                Return ""
            Else
                Return _dgLimitLevel(index).ToString
            End If
        End Get
        Set(value As String)
            Try
                ' Nothingではない、かつ空白ではない場合
                If Not value Is Nothing AndAlso Not value.Equals("") Then
                    _dgLimitLevel(index) = Convert.ToInt32(value)
                Else
                    _dgLimitLevel(index) = 0
                End If
            Catch ex As Exception
                '何もしない
            End Try
        End Set
    End Property

    Public _dgLimitLevel2(0 To 4) As Integer
    Public Property dgLimitLevel2(index As Integer) As String
        Get
            If _dgLimitLevel2(index) = 0 Then
                Return dgLimitLevel1(index)
            Else
                Return _dgLimitLevel2(index).ToString
            End If
        End Get
        Set(value As String)
            Try
                ' Nothingではない、かつ空白ではない場合
                If Not value Is Nothing AndAlso Not value.Equals("") Then
                    _dgLimitLevel2(index) = Convert.ToInt32(value)
                Else
                    _dgLimitLevel2(index) = 0
                End If
            Catch ex As Exception
                '何もしない
            End Try
        End Set
    End Property



End Class
