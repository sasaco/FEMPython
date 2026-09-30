''' <summary>
''' Dictionary クラスからアイテムの存在を確認する作業を省いた拡張クラス
''' </summary>
''' <typeparam name="TKey"></typeparam>
''' <typeparam name="TValue"></typeparam>
Public Class DictionaryEx(Of TKey, TValue As New)
    Inherits Dictionary(Of TKey, TValue)

    Default Public Overloads Property Item(_Key As TKey) As TValue
        Get
            Dim tmp As TValue
            If MyBase.ContainsKey(_Key) Then
                tmp = MyBase.Item(_Key)
            Else
                tmp = New TValue
            End If
            Return tmp
        End Get
        Set(value As TValue)
            If MyBase.ContainsKey(_Key) Then
                MyBase.Item(_Key) = value
            Else
                MyBase.Add(_Key, value)
            End If

        End Set
    End Property

    Public Overloads Sub add(_Key As TKey, _Value As TValue)
        If MyBase.ContainsKey(_Key) Then
            MyBase.Item(_Key) = _Value
        Else
            MyBase.Add(_Key, _Value)
        End If
    End Sub

End Class
