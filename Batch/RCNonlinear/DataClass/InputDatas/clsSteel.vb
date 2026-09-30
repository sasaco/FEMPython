Public Class clsSteel
    ''' <summary>深さ位置</summary>
    Public Depth As Double

    ''' <summary>
    ''' 鋼材名称 
    ''' 　異形鉄筋 "D32" 
    ''' 　丸鋼 "R29" 
    ''' 　断面積任意指定 "794.2"
    ''' </summary>
    Public i As String

    ''' <summary>鋼材の本数</summary>
    Public n As Double

    ''' <summary>鋼材の引張降伏着目Flag</summary>
    Public IsTensionBar As Boolean

    ''' <summary>材料番号</summary>
    Public ElasticID As String

    Sub New()
        Depth = 0
        i = ""
        n = 0
        IsTensionBar = False
        ElasticID = ""
    End Sub

    ''' <summary>鋼材断面積</summary>
    Public ReadOnly Property Area As Double
        Get
            If InStr(i, "D") Then
                '異形鉄筋
                Dim D As Integer = Convert.ToInt32(Right(i, Len(i) - Len("D")))
                Return GetAs(D) * n
            ElseIf InStr(i, "R") Then
                '丸鋼
                Dim D As Double = Convert.ToDouble(Right(i, Len(i) - Len("R")))
                Return (D ^ 2) * Math.PI / 4 * n
            Else
                '断面積任意指定
                Dim S = System.Text.RegularExpressions.Regex.Replace(i, "[^0-9\.\-]", "") '正規表現で文字列から数字以外を除去
                Dim D As Double = Convert.ToDouble(S)
                Return D * n
            End If
            Return 0
        End Get
    End Property

    ''' <summary>鋼材の直径</summary>
    Public ReadOnly Property D As Double
        Get
            If InStr(i, "D") Then
                '異形鉄筋
                Dim f As Integer = Convert.ToInt32(Right(i, Len(i) - Len("D")))
                Return GetD(f)
            ElseIf InStr(i, "R") Then
                '丸鋼
                Return Right(i, Len(i) - Len("R"))
            End If
            Return 0
        End Get
    End Property

    Private Function GetAs(D As Integer) As Double
        Select Case D
            Case 10 : Return 71.33
            Case 13 : Return 126.7
            Case 16 : Return 198.6
            Case 19 : Return 286.5
            Case 22 : Return 387.1
            Case 25 : Return 506.7
            Case 29 : Return 642.4
            Case 32 : Return 794.2
            Case 35 : Return 956.6
            Case 38 : Return 1140
            Case 41 : Return 1340
            Case 51 : Return 2027
            Case Else : Return (D ^ 2) * Math.PI / 4
        End Select
    End Function
    Private Function GetD(D As Integer) As Double
        Select Case D
            Case 10 : Return 9.53
            Case 13 : Return 12.7
            Case 16 : Return 15.9
            Case 19 : Return 19.1
            Case 22 : Return 22.2
            Case 25 : Return 25.4
            Case 29 : Return 28.6
            Case 32 : Return 31.8
            Case 35 : Return 34.9
            Case 38 : Return 38.1
            Case 41 : Return 41.3
            Case 51 : Return 50.8
            Case Else : Return D
        End Select
    End Function
End Class
