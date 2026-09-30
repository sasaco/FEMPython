Public Class MenuControl

	Public Sub New()

		' この呼び出しは、Windows フォーム デザイナで必要です。
		InitializeComponent()

		' InitializeComponent() 呼び出しの後で初期化を追加します。

	End Sub
	''' <summary>
	''' メニューの画像を設定
	''' </summary>
	Public WriteOnly Property Image() As Image
		Set(ByVal value As Image)
			pic.Image = value
		End Set
	End Property
	''' <summary>
	''' メニュー画面に表示するメッセージ内容を設定
	''' </summary>
	Public WriteOnly Property Message() As String
		Set(ByVal value As String)
			textMessage.Text = value
		End Set
	End Property
End Class
