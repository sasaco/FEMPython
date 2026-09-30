Public Class MyPictureBox

    Public Const pading = 20 'pixcel
    Public Const radius = 4 'pixcel

    Public PointX As Dictionary(Of Integer, Double)
    Public PointY As Dictionary(Of Integer, Double)
    Public PrintPointNo As Dictionary(Of Integer, String)

    Public MemberNum As Integer
    Public MemberNo() As Integer
    Public PrintMemberNo() As String
    Public IPointNo() As Integer
    Public JPointNo() As Integer

    Private _maxXValue As Double
    Private _minXValue As Double
    Private _maxYValue As Double
    Private _minYValue As Double

    Public Sub New()

        ' この呼び出しはデザイナーで必要です。
        InitializeComponent()
        _maxXValue = Nothing
        _minXValue = Nothing
        _maxYValue = Nothing
        _minYValue = Nothing

        PointX = New Dictionary(Of Integer, Double)
        PointY = New Dictionary(Of Integer, Double)
        PrintPointNo = New Dictionary(Of Integer, String)

        MemberNum = 0
        Erase MemberNo
        Erase PrintMemberNo
        Erase IPointNo
        Erase JPointNo

    End Sub

    Public Function GetImage(width As Integer, height As Integer) As Bitmap

        Me.PictureBox1.Width = width
        Me.PictureBox1.Height = height

        '画像を保存するためのImage(ここではBitmap)オブジェクトを作成する
        Dim saveImg As New Bitmap(width, height)
        saveImg.SetResolution(100, 100)
        'ImageオブジェクトのGraphicsオブジェクトを作成する
        Dim g As Graphics = Graphics.FromImage(saveImg)
        Dim e As New PaintEventArgs(g, New Rectangle(0, 0, Me.PictureBox1.Width, Me.PictureBox1.Height))
        'Imageオブジェクトに画像と文字列を描画する
        Call PictureBox1_Paint(Me, e)
        'Graphicsオブジェクトを解放する
        g.Dispose()

        'Imageオブジェクトをファイルに保存する
        Return saveImg

    End Function


    Private Sub PictureBox1_Paint(sender As Object, e As PaintEventArgs) Handles PictureBox1.Paint

        '節点番号を描画する。
        Try
            'キーを配列に変換する
            For i = 1 To PointX.Count - 1
                Dim key As Integer = PointX.Keys()(i)
                Dim x As Integer = CInt((PointX(key) - MinX()) * ScaleX + pading) + MarginX
                Dim y As Integer = CInt((PointY(key) - MinY()) * ScaleY + pading) + MarginY
                If PrintPointNo.ContainsKey(key) = True Then
                    e.Graphics.DrawString(PrintPointNo(key), New Font("Arial", 10), Brushes.Red, x, y)
                    e.Graphics.FillEllipse(Brushes.Red, x - 2, y - 2, radius, radius)
                Else
                    e.Graphics.FillEllipse(Brushes.Black, x - 2, y - 2, radius, radius)
                End If
            Next
        Catch ex As Exception

        End Try

        '要素を描画する。
        Try
            For m = 1 To MemberNum
                Dim ix As Integer = CInt((PointX(IPointNo(m)) - MinX()) * ScaleX + pading) + MarginX
                Dim iy As Integer = CInt((PointY(IPointNo(m)) - MinY()) * ScaleY + pading) + MarginY
                Dim jx As Integer = CInt((PointX(JPointNo(m)) - MinX()) * ScaleX + pading) + MarginX
                Dim jy As Integer = CInt((PointY(JPointNo(m)) - MinY()) * ScaleY + pading) + MarginY
                If PrintMemberNo(m) <> "" Then
                    'e.Graphics.DrawString(PrintMemberNo(m), New Font("Arial", 12), Brushes.Red, (ix + jx) / 2, (iy + jy) / 2)
                    e.Graphics.DrawString(PrintMemberNo(m), New Font("ＭＳ ゴシック", 12), Brushes.Red, (ix + jx) / 2, (iy + jy) / 2)
                    e.Graphics.DrawLine(Pens.Red, ix, iy, jx, jy)
                Else
                    e.Graphics.DrawLine(Pens.Black, ix, iy, jx, jy)
                End If
            Next
        Catch ex As Exception

        End Try
    End Sub

    Public Function GetImageForPDF(width As Integer, height As Integer) As Bitmap

        Me.PictureBox1.Width = width
        Me.PictureBox1.Height = height

        '画像を保存するためのImage(ここではBitmap)オブジェクトを作成する
        Dim saveImg As New Bitmap(width, height)
        saveImg.SetResolution(100, 100)
        'ImageオブジェクトのGraphicsオブジェクトを作成する
        Dim g As Graphics = Graphics.FromImage(saveImg)
        Dim e As New PaintEventArgs(g, New Rectangle(0, 0, Me.PictureBox1.Width, Me.PictureBox1.Height))
        'Imageオブジェクトに画像と文字列を描画する
        Call PictureBox1_PaintForPDF(Me, e)
        'Graphicsオブジェクトを解放する
        g.Dispose()

        'Imageオブジェクトをファイルに保存する
        Return saveImg


    End Function


    Private Sub PictureBox1_PaintForPDF(sender As Object, e As PaintEventArgs) Handles PictureBox1.Paint

        '節点番号を描画する。
        Try
            'キーを配列に変換する
            For i = 1 To PointX.Count - 1
                Dim key As Integer = PointX.Keys()(i)
                Dim x As Integer = CInt((PointX(key) - MinX()) * ScaleX + pading) + MarginX
                Dim y As Integer = CInt((PointY(key) - MinY()) * ScaleY + pading) + MarginY
                If PrintPointNo.ContainsKey(key) = True Then
                    e.Graphics.DrawString(PrintPointNo(key), New Font("Arial", 10), Brushes.Red, x, y)
                    e.Graphics.FillEllipse(Brushes.Red, x - 2, y - 2, radius, radius)
                Else
                    e.Graphics.FillEllipse(Brushes.Black, x - 2, y - 2, radius, radius)
                End If
            Next
        Catch ex As Exception

        End Try

        '要素を描画する。
        Try
            For m = 1 To MemberNum
                Dim ix As Integer = CInt((PointX(IPointNo(m)) - MinX()) * ScaleX + pading) + MarginX
                Dim iy As Integer = CInt((PointY(IPointNo(m)) - MinY()) * ScaleY + pading) + MarginY
                Dim jx As Integer = CInt((PointX(JPointNo(m)) - MinX()) * ScaleX + pading) + MarginX
                Dim jy As Integer = CInt((PointY(JPointNo(m)) - MinY()) * ScaleY + pading) + MarginY
                If PrintMemberNo(m) <> "" Then
                    'e.Graphics.DrawString(PrintMemberNo(m), New Font("Arial", 12), Brushes.Red, (ix + jx) / 2, (iy + jy) / 2)
                    e.Graphics.DrawString(PrintMemberNo(m), New Font("ＭＳ ゴシック", 12), Brushes.Red, (ix + jx) / 2, (iy + jy) / 2)
                    e.Graphics.DrawLine(Pens.Red, ix, iy, jx, jy)
                Else
                    e.Graphics.DrawLine(Pens.Black, ix, iy, jx, jy)
                End If
            Next
        Catch ex As Exception

        End Try
    End Sub

#Region "距離計算"

    Public Function MaxXDistance() As Double
        Return MaxX() - MinX()
    End Function

    Private ReadOnly Property MaxX() As Double
        Get
            _maxXValue = Double.MinValue
            For Each p In PointX
                _maxXValue = Math.Max(_maxXValue, p.Value)
            Next
            Return _maxXValue
        End Get
    End Property

    Private ReadOnly Property MinX() As Double
        Get
            _minXValue = Double.MaxValue
            For Each p In PointX
                _minXValue = Math.Min(_minXValue, p.Value)
            Next
            Return _minXValue
        End Get
    End Property

    Public Function MaxYDistance() As Double
        Return MaxY() - MinY()
    End Function

    Private ReadOnly Property MaxY() As Double
        Get
            _maxYValue = Double.MinValue
            For Each p In PointY
                _maxYValue = Math.Max(_maxYValue, p.Value)
            Next
            Return _maxYValue
        End Get
    End Property

    Private ReadOnly Property MinY() As Double
        Get
            _minYValue = Double.MaxValue
            For Each p In PointY
                _minYValue = Math.Min(_minYValue, p.Value)
            Next
            Return _minYValue
        End Get
    End Property

    Private ReadOnly Property ScaleX As Double
        Get
            If MaxXDistance() = 0 Then
                Return 1
            Else
                Return (Me.PictureBox1.Width - (pading * 2)) / MaxXDistance()
            End If
        End Get
    End Property
    Private ReadOnly Property MarginX As Double
        Get
            If MaxXDistance() = 0 Then
                Return Me.PictureBox1.Width / 2 - pading
            Else
                Return 0
            End If
        End Get
    End Property
    Private ReadOnly Property ScaleY As Double
        Get
            If MaxYDistance() = 0 Then
                Return 1
            Else
                Return (Me.PictureBox1.Height - (pading * 2)) / MaxYDistance()
            End If
        End Get
    End Property
    Private ReadOnly Property MarginY As Double
        Get
            If MaxYDistance() = 0 Then
                Return Me.PictureBox1.Height / 2 - pading
            Else
                Return 0
            End If
        End Get
    End Property
#End Region

End Class
