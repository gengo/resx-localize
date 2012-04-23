Namespace Info
    Public Class SourceResource

        Private SourceResourceFileName As String

        Private XD As XDocument

        Public ReadOnly Property Keys() As IEnumerable(Of String)
            Get
                Return XD.<root>.<data>.Select(Function(E) E.@name)
            End Get
        End Property

        Sub New(TheSourceResourceFile As String)
            SourceResourceFileName = TheSourceResourceFile

            '' Make sure that the source exists
            If Not IO.File.Exists(TheSourceResourceFile) Then
                Throw New Exception(String.Format("Source resource '{0}' does not exist.", TheSourceResourceFile))
            End If

            '' Load
            XD = XDocument.Load(TheSourceResourceFile)

        End Sub

        Public Function HasKey(TheKey As String) As Boolean
            Return XD.<root>.<data>.Any(Function(D) D.@name = TheKey)
        End Function

        Public Function GetValue(TheKey As String) As String
            Dim SelectedKey = XD.<root>.<data>.SingleOrDefault(Function(D) D.@name = TheKey)
            If SelectedKey Is Nothing Then
                Throw New KeyNotFoundException()
            End If

            Return SelectedKey.<value>.Value()

        End Function

    End Class
End Namespace