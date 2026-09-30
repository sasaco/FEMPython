Public Class CalculationSetting

    Public _rbProduct(0 To 1) As Boolean

    Public Sub New()
        _rbProduct(0) = True
        _rbProduct(1) = False
    End Sub

    'rbProduct0-1の値を格納します。
    Public Property rbProduct(index As Integer) As Boolean
        Get
            Return _rbProduct(index)
        End Get
        Set(ByVal Value As Boolean)
            _rbProduct(index) = Value
        End Set
    End Property

    Public Function ProductStyle() As Integer

        For i = 0 To _rbProduct.Count - 1
            If _rbProduct(i) = True Then
                Return i
            End If
        Next i
        Return -1

    End Function

End Class
