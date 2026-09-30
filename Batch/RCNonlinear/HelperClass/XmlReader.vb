''' <summary> インプットデータから XMLファイルを読み書きするクラス </summary>
Friend Class XmlReader(Of T As New)

    Private _Data As New T
    Public Property Data As T
        Get
            Return Me._Data
        End Get
        Set(value As T)
            _Data = value
        End Set
    End Property


    Public Property XmlData As String
        Get
            Try
                Dim serializer1 As _
                    New System.Xml.Serialization.XmlSerializer(GetType(T))
                Using writer As New System.IO.StringWriter
                    serializer1.Serialize(writer, Me._Data)
                    Return writer.ToString()
                End Using
            Catch ex As Exception
                Return ex.Message
            End Try
        End Get
        Set(value As String)
            Try
                Dim serializer2 As _
                        New System.Xml.Serialization.XmlSerializer(GetType(T))
                Using reader As New System.IO.StringReader(value)
                    Me._Data = serializer2.Deserialize(reader)
                End Using
            Catch ex As Exception
                Throw ex
            End Try
        End Set
    End Property

End Class
