Public Class PDFViewer

    Private FilePath As String

    Public Sub SetDocument(filename As String)
        FilePath = filename
    End Sub

    Private Sub PDFViewer_Loaded(sender As Object, e As System.Windows.RoutedEventArgs) Handles Me.Loaded
        Try
            WPFpdfViewer.LoadDocument(FilePath)
        Catch ex As Exception
            '何もしない
        End Try
    End Sub



End Class
