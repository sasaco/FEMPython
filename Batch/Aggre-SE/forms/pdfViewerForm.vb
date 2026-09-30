Imports iTextSharp.text.pdf
Imports System.IO

Public Class pdfViewerForm

    Private ReadedFileName As String
    Public Sub New(filename As String)
        InitializeComponent()
        Try
            PdfViewer1.SetDocument(filename)
        Catch ex As Exception
            ReadedFileName = ""
        Finally
            ReadedFileName = filename
        End Try
    End Sub
    Public Sub New(InputFile As List(Of String), filename As String)
        InitializeComponent()
        If CombineWithPDF(InputFile, filename) = True Then
            PdfViewer1.SetDocument(filename)
            Try
                PdfViewer1.SetDocument(filename)
            Catch ex As Exception
                ReadedFileName = ""
            Finally
                ReadedFileName = filename
            End Try
        Else
            ReadedFileName = ""
        End If
    End Sub

    Public Sub ShowPdf()
        Try
            Dim oProc As New Process
            oProc.StartInfo.FileName = ReadedFileName
            oProc.Start()
            oProc.WaitForExit()
        Catch ex As Exception

        End Try
    End Sub

    Private Function CombineWithPDF(InputFile As List(Of String), OutputFileName As String) As Boolean

        Try
            ' 入力用PDFファイル
            Dim readers(0 To InputFile.Count - 1) As PdfReader

            ' 出力用PDFオブジェクトの生成
            Using dc As iTextSharp.text.Document = New iTextSharp.text.Document()
                ' 出力用PDFファイルのオープン
                Dim fs1 As FileStream = New FileStream(OutputFileName, FileMode.Create, FileAccess.Write)
                ' 出力用PDFオブジェクトとPDFファイルの関連付け
                Dim wr1 = PdfWriter.GetInstance(dc, fs1)
                ' PDF出力開始
                dc.Open()
                ' PdfContentByte取得
                Dim pcb As PdfContentByte = wr1.DirectContent()

                ' 入力用PDFファイルのオープン
                For i = 0 To InputFile.Count - 1
                    readers(i) = New PdfReader(InputFile(i))
                Next

                ' 入力用PDFファイル毎の処理
                For Each rd In readers
                    ' 入力用PDFファイルのページ数取得
                    Dim pn As Integer = rd.NumberOfPages()
                    ' PDF出力処理
                    For i As Integer = 1 To pn
                        ' ページサイズ設定
                        dc.SetPageSize(rd.GetPageSizeWithRotation(i))
                        ' 改ページ
                        dc.NewPage()
                        ' ページ取得
                        Dim pip As PdfImportedPage = wr1.GetImportedPage(rd, i)
                        ' ページ出力
                        If rd.GetPageRotation(i) = 0 Then
                            ' 回転しない
                            pcb.AddTemplate(pip, 1.0F, 0, 0, 1.0F, 0, 0)
                        Else
                            ' 回転する
                            pcb.AddTemplate(pip, 0, -1.0F, 1.0F, 0, 0, _
                                rd.GetPageSizeWithRotation(i).Height())
                        End If
                    Next i
                Next
                dc.Close()
            End Using
            ' 入力用PDFファイルのクローズ
            For Each rd In readers
                rd.Close()
                rd = Nothing
            Next
        Catch ex As Exception
            Throw ex
        End Try
        Return True
    End Function

End Class