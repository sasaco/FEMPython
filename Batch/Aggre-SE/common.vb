Imports Aggre
Imports Aggre.PDFReport

Public Module common

    Public Function TempPath() As String

        Dim SystemDrivePath As String
        Try
            SystemDrivePath = Environ("SystemDrive")
            If SystemDrivePath = "" Then SystemDrivePath = "C:"
        Catch ex As Exception
            SystemDrivePath = "C:"
        End Try

        Dim strPath As String = SystemDrivePath & "\nwl\Docu-SE"

        Try
            If System.IO.Directory.Exists(strPath) = False Then
                System.IO.Directory.CreateDirectory(strPath)
            End If
            Return strPath
        Catch
            Try
                Dim fallbackPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "nwl",
                    "Docu-SE")
                If System.IO.Directory.Exists(fallbackPath) = False Then
                    System.IO.Directory.CreateDirectory(fallbackPath)
                End If
                Return fallbackPath
            Catch
                Return System.IO.Path.GetTempPath()
            End Try
        End Try

    End Function



    ''' ------------------------------------------------------------------------
    ''' <summary>
    '''     指定した精度の数値に切り捨てします。</summary>
    ''' <param name="dValue">
    '''     丸め対象の倍精度浮動小数点数。</param>
    ''' <param name="iDigits">
    '''     戻り値の有効桁数の精度。</param>
    ''' <returns>
    '''     iDigits に等しい精度の数値に切り捨てられた数値。</returns>
    ''' ------------------------------------------------------------------------
    Public Function RoundDown(ByVal dValue As Double, ByVal iDigits As Integer) As Double
        Dim dCoef As Double = System.Math.Pow(10, iDigits)

        If dValue > 0 Then
            Return System.Math.Floor(dValue * dCoef) / dCoef
        Else
            Return System.Math.Ceiling(dValue * dCoef) / dCoef
        End If
    End Function

    ''' ------------------------------------------------------------------------
    ''' <summary>
    '''     指定した精度の数値に切り上げします。</summary>
    ''' <param name="dValue">
    '''     丸め対象の倍精度浮動小数点数。</param>
    ''' <param name="iDigits">
    '''     戻り値の有効桁数の精度。</param>
    ''' <returns>
    '''     iDigits に等しい精度の数値に切り上げられた数値。</returns>
    ''' ------------------------------------------------------------------------
    Public Function RoundUp(ByVal dValue As Double, ByVal iDigits As Integer) As Double
        Dim dCoef As Double = System.Math.Pow(10, iDigits)

        If dValue > 0 Then
            Return System.Math.Ceiling(dValue * dCoef) / dCoef
        Else
            Return System.Math.Floor(dValue * dCoef) / dCoef
        End If
    End Function

    ''' <summary>
    ''' 弧度法 を ラジアンに変換
    ''' </summary>
    ''' <param name="Degrees">角度(°)</param>
    ''' <returns>ラジアン</returns>
    ''' <remarks></remarks>
    Public Function Radians(Degrees As Single) As Single
        Return Degrees * Math.PI / 180
    End Function

End Module
