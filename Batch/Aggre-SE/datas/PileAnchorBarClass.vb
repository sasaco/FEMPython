
Public Class PileAnchorBarClass

    Public 段落し情報 As New List(Of clsPileAnchorBar)

End Class


Public Class clsPileAnchorBar

#Region "段落しルール"
    ''' <summary>曲げモーメントに対する余裕率</summary>
    Public Property MudCoefficient As Single = 1.5
    ''' <summary>せん断力に対する余裕率</summary>
    Public Property VudCoefficient As Single = 1.5
#End Region

#Region "段落し前の鉄筋情報"

    ''' <summary>表示用帯鉄筋区間長</summary>
    Public _Section2() As Integer
    Public Property Section2(id As Integer) As String
        Get
            If IsNothing(_Section2) Then
                Return ""
            Else
                Return _Section2(id)
            End If
        End Get
        Set(value As String)
            Try
                _Section2(id) = Val(value)
            Catch
            End Try
        End Set
    End Property
#End Region

#Region "段落し後の鉄筋情報"
    ''' <summary>段落し後の軸方向鉄筋径</summary>
    Public Property D1 As String
        Get
            If _D1 = 0 Then
                Return ""
            Else
                Return _D1
            End If
        End Get
        Set(value As String)
            Dim v As Integer = Val(value)
            _D1 = v
        End Set
    End Property
    Public _D1 As Integer

    ''' <summary>段落し後の軸方向鉄筋本数</summary>
    Public Property n1 As String
        Get
            If _n1 = 0 Then
                Return ""
            Else
                Return _n1
            End If
        End Get
        Set(value As String)
            Dim v As Integer = Val(value)
            _n1 = v
        End Set
    End Property
    Public _n1 As Integer

    ''' <summary>段落し後の帯鉄筋径</summary>
    Public Property D2 As String
        Get
            If _D2 = 0 Then
                Return ""
            Else
                Return _D2
            End If
        End Get
        Set(value As String)
            Dim v As Integer = Val(value)
            _D2 = v
        End Set
    End Property
    Public _D2 As Integer

    ''' <summary>段落し後の帯鉄筋組数</summary>
    Public Property Aw As String
        Get
            If _Aw = 0 Then
                Return ""
            Else
                Return _Aw
            End If
        End Get
        Set(value As String)
            Dim v As Integer = Val(value)
            _Aw = v
        End Set
    End Property
    Public _Aw As Single

    ''' <summary>段落し後の帯鉄筋間隔</summary>
    Public Property Ss As String
        Get
            If _Ss = 0 Then
                Return ""
            Else
                Return _Ss
            End If
        End Get
        Set(value As String)
            Dim v As Integer = Val(value)
            _Ss = v
        End Set
    End Property
    Public _Ss As Single

#End Region

#Region "部材番号"
    ''' <summary>開始部材番号</summary>
    Public dgMemberFirst As New List(Of Integer)
    ''' <summary>終了部材番号</summary>
    Public dgMemberLast As New List(Of Integer)
    ''' <summary>部材の奥行き本数</summary>
    Public dgMemberNumber As New List(Of Single)
#End Region

#Region "定着長他"

    ''' <summary>杭頭定着長</summary>
    Public Property PileHeadLength As String
        Get
            If _PileHeadLength = 0 Then
                Return ""
            Else
                Return _PileHeadLength
            End If
        End Get
        Set(value As String)
            Dim v As Integer = Val(value)
            _PileHeadLength = v
        End Set
    End Property
    Public _PileHeadLength As Integer

    ''' <summary>段落し長さ</summary>
    Public Property ParagraphLength As String
        Get
            If _ParagraphLength = 0 Then
                Return ""
            Else
                Return _ParagraphLength
            End If
        End Get
        Set(value As String)
            Dim v As Integer = Val(value)
            _ParagraphLength = v
        End Set
    End Property
    Public _ParagraphLength As Single

    ''' <summary>定着部のコンクリート強度</summary>
    Public Property ConcreteStrength As String
        Get
            If _ConcreteStrength = 0 Then
                Return ""
            Else
                Return _ConcreteStrength
            End If
        End Get
        Set(value As String)
            Dim v As Integer = Val(value)
            _ConcreteStrength = v
        End Set
    End Property
    Public _ConcreteStrength As Single

    ''' <summary>継手低減係数</summary>
    Public Property ReductionCoefficient As String
        Get
            If _ReductionCoefficient = 0 Then
                Return ""
            Else
                Return _ReductionCoefficient
            End If
        End Get
        Set(value As String)
            Dim v As Single = Val(value)
            _ReductionCoefficient = v
        End Set
    End Property
    Public _ReductionCoefficient As Single

    ''' <summary>段落し定着長</summary>
    Public Property PileMidLength As String
        Get
            If _PileMidLength = 0 Then
                Return ""
            Else
                Return _PileMidLength
            End If
        End Get
        Set(value As String)
            Dim v As Integer = Val(value)
            _PileMidLength = v
        End Set
    End Property
    Public _PileMidLength As Integer

    ''' <summary>設計余裕長</summary>
    Public Property DesignMarginLength As String
        Get
            If _DesignMarginLength = 0 Then
                Return ""
            Else
                Return _DesignMarginLength
            End If
        End Get
        Set(value As String)
            Dim v As Integer = Val(value)
            _DesignMarginLength = v
        End Set
    End Property
    Public _DesignMarginLength As Integer

    ''' <summary>継手を設けてはいけない範囲で、杭頭から</summary>
    Public Property DisabledFromPileHead As String
        Get
            If _DisabledFromPileHead = 0 Then
                Return ""
            Else
                Return _DisabledFromPileHead
            End If
        End Get
        Set(value As String)
            Dim v As Integer = Val(value)
            _DisabledFromPileHead = v
        End Set
    End Property
    Public _DisabledFromPileHead As Integer

    ''' <summary>継手を設けてはいけない範囲で、カットオフ点から上方に</summary>
    Public Property DisabledUpwardFromCutOffPoint As String
        Get
            If _DisabledUpwardFromCutOffPoint = 0 Then
                Return ""
            Else
                Return _DisabledUpwardFromCutOffPoint
            End If
        End Get
        Set(value As String)
            Dim v As Integer = Val(value)
            _DisabledUpwardFromCutOffPoint = v
        End Set
    End Property
    Public _DisabledUpwardFromCutOffPoint As Integer

    ''' <summary>継手を設けてはいけない範囲で、カットオフ点から下方に</summary>
    Public Property DisabledDownwardFromCutOffPoint As String
        Get
            If _DisabledDownwardFromCutOffPoint = 0 Then
                Return ""
            Else
                Return _DisabledDownwardFromCutOffPoint
            End If
        End Get
        Set(value As String)
            Dim v As Integer = Val(value)
            _DisabledDownwardFromCutOffPoint = v
        End Set
    End Property
    Public _DisabledDownwardFromCutOffPoint As Integer

#End Region

End Class
