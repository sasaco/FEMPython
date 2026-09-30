Public Class FoundationSettingClass

    Public 基礎照査 As New List(Of clsFoundation)

End Class

Public Class clsFoundation

    ''' <summary>基礎形式</summary>
    Private _FoundationType As Integer
    Public Property FoundationType As Integer
        Get
            Return _FoundationType
        End Get
        Set(value As Integer)
            _FoundationType = value
        End Set
    End Property
    ''' <summary>杭基礎の場合のファイル</summary>
    Public sdtFilePath As String
    ''' <summary>照査位置</summary>
    Public dgReactAtPoint As New List(Of clsAtPoints)
    Public dgDispAtPoint As New clsAtPoints '直接基礎の変位の着目点
    Public dgDispAtPoints As New List(Of clsAtPoints)  '杭基礎の変位の着目点
    ''' <summary>制限値</summary>
    Public dgLimitValue0(0 To 30) As String '杭基礎の制限値
    Public dgLimitValue1(0 To 10) As String '直接基礎の制限値

    Public Function IsEnable() As Boolean

        Dim i As Integer

        Dim result As Boolean = False

        For Each d In dgDispAtPoints
            If Int32.TryParse(d.pointNo, i) Then
                If i >= 0 Then
                    result = True
                    Exit For
                End If
            End If
        Next
        If Int32.TryParse(dgDispAtPoint.pointNo, i) Then
            If i >= 0 Then result = True
        End If

        For Each r In dgReactAtPoint
            If Int32.TryParse(r.pointNo, i) Then
                If i >= 0 Then
                    result = True
                    Exit For
                End If
            End If
        Next

        Return result
    End Function

End Class

Public Class clsAtPoints
    ''' <summary>節点番号</summary>
    Private _pointNo As Integer
    Public Property pointNo As String
        Get
            If _pointNo <= 0 Then
                Return ""
            Else
                Return _pointNo.ToString()
            End If
        End Get
        Set(value As String)
            ' Nothingではない、かつ空白ではない場合
            If IsNumeric(value) Then
                _pointNo = Convert.ToInt32(value)
            Else
                _pointNo = 0
            End If
        End Set
    End Property

    ''' <summary>始点からの距離</summary>
    Public _Direction As Double
    Public Property Direction As String
        Get
            Return _Direction.ToString("F3")
        End Get
        Set(value As String)
            If IsNumeric(value) Then
                _Direction = Convert.ToDouble(value)
            Else
                _Direction = 0
            End If
        End Set
    End Property
End Class