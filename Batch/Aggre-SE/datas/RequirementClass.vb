Public Class RequirementClass

    'インスタンス化しなくて良いように共有メンバとして宣言する。
    Public _AtPoint As String

    Public _rbSpec(0 To 2) As Boolean
    Public _rbChiiki(0 To 2) As Boolean
    Public _rbJIban(0 To 9) As Boolean
    Public _rbEkijo(0 To 1) As Boolean

    Public dgEkijoCase1(0 To Input.DATAROWS - 1) As Integer
    Public dgEkijoCase21(0 To Input.DATAROWS - 1) As Integer ' 液状化スペクトル１
    Public dgEkijoCase2(0 To Input.DATAROWS - 1) As Integer
    Public cbM65Area As Integer
    Public _tbAlfa As Single

    Public _cbηx As Boolean
    Public _tbηxL1 As Single
    Public _tbηxL2 As Single

    '初期値
    Sub New()
        _AtPoint = ""

        _rbSpec(0) = True
        _rbSpec(1) = False
        _rbSpec(2) = False

        _rbChiiki(0) = True
        _rbChiiki(1) = False
        _rbChiiki(2) = False

        For Each tmpJiban In _rbJIban
            tmpJiban = False
        Next
        _rbJIban(0) = True

        _rbEkijo(0) = True
        _rbEkijo(1) = False

        cbM65Area = 0

        _tbAlfa = 1

        _cbηx = False
        _tbηxL1 = 1
        _tbηxL2 = 1

    End Sub

    'tbηxL1の値を格納します。
    Public Property tbηxL1 As String
        Get
            Try
                If _cbηx = False Then
                    Return ""
                Else
                    Return Format(_tbηxL1, "0.000")
                End If
            Catch
                Return ""
            End Try
        End Get
        Set(value As String)
            Try
                If _cbηx = False Then
                    _tbηxL1 = 1
                Else
                    _tbηxL1 = Convert.ToSingle(value)
                End If
            Catch
                _tbηxL1 = 1
            End Try
        End Set
    End Property

    'tbηxL2の値を格納します。
    Public Property tbηxL2 As String
        Get
            Try
                If _cbηx = False Then
                    Return ""
                Else
                    Return Format(_tbηxL2, "0.000")
                End If
            Catch
                Return ""
            End Try
        End Get
        Set(value As String)
            Try
                If _cbηx = False Then
                    _tbηxL2 = 1
                Else
                    _tbηxL2 = Convert.ToSingle(value)
                End If
            Catch
                _tbηxL2 = 1
            End Try
        End Set
    End Property

    'tbAlfaの値を格納します。
    Public Property tbAlfa As String
        Get
            Return Format(_tbAlfa, "0.000")
        End Get
        Set(value As String)
            If IsNumeric(value) Then
                _tbAlfa = Convert.ToSingle(value)
            End If
        End Set
    End Property

    'tbAtPointの値を格納します。
    Public Property AtPoint() As String
        Get
            Return _AtPoint
        End Get
        Set(ByVal Value As String)
            _AtPoint = Value
        End Set
    End Property

    'rbSpec0-2の値を格納します。
    Public Property rbSpec(index As Integer) As Boolean
        Get
            If UBound(_rbSpec) < 3 Then
                ReDim Preserve _rbSpec(0 To 2)
            End If
            Return _rbSpec(index)
        End Get
        Set(ByVal Value As Boolean)
            _rbSpec(index) = Value
        End Set
    End Property
    '
    'rbChiikiA-Cの値を格納します。
    Public Property rbChiiki(index As Integer) As Boolean
        Get
            Return _rbChiiki(index)
        End Get
        Set(ByVal Value As Boolean)
            _rbChiiki(index) = Value
        End Set
    End Property
    '
    'rbJIban0-5の値を格納します。
    Public Property rbJiban(index As Integer) As Boolean
        Get
            '入力データが小さい配列だった場合
            If index >= _rbJIban.Length Then
                ReDim Preserve _rbJIban(0 To 9)
            End If

            Return _rbJIban(index)
        End Get
        Set(ByVal Value As Boolean)
            _rbJIban(index) = Value
        End Set
    End Property
    '
    'rbEkijo5-20の値を格納します。
    Public Property rbEkijo(index As Integer) As Boolean
        Get
            Return _rbEkijo(index)
        End Get
        Set(ByVal Value As Boolean)
            _rbEkijo(index) = Value
        End Set
    End Property

End Class
