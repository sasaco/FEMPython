Public Class VersionForm

    Public Sub New()

        ' この呼び出しはデザイナーで必要です。
        InitializeComponent()

        ' InitializeComponent() 呼び出しの後で初期化を追加します。
        Dim ver As System.Diagnostics.FileVersionInfo = _
            System.Diagnostics.FileVersionInfo.GetVersionInfo( _
            System.Reflection.Assembly.GetExecutingAssembly().Location)

        Me.txtVer.Text = ver.FileVersion

    End Sub


End Class