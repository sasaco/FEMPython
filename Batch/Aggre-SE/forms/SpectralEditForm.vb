Public Class SpectralEditForm

    Public resutValue As Double = 0

    Private Sub SpectralEditForm_Shown(sender As Object, e As EventArgs) Handles Me.Shown
        TextBox1.Focus()
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        Try
            resutValue = Convert.ToDouble(TextBox1.Text)
        Catch ex As Exception
            resutValue = 0
        Finally
            Me.Close()
        End Try
    End Sub

    Private Sub TextBox1_KeyDown(sender As Object, e As KeyEventArgs) Handles TextBox1.KeyDown
        'F1キーが押されたか調べる
        If e.KeyData = Keys.Return Then
            Button1_Click(Nothing, Nothing)
        End If
    End Sub


End Class