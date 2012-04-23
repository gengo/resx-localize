Namespace Info
    Public Class TargetResource

        Private TargetResourceFileName As String

        Private XD As XDocument

        Public ReadOnly Property Keys() As IEnumerable(Of String)
            Get
                Return XD.<root>.<data>.Select(Function(E) E.@name)
            End Get
        End Property

        Sub New(TheSourceResourceFile As String, TheTargetResourceFile As String)
            TargetResourceFileName = TheTargetResourceFile

            '' Make sure that the source exists
            If Not IO.File.Exists(TheSourceResourceFile) Then
                Throw New Exception(String.Format("Source resource '{0}' does not exist.", TheSourceResourceFile))
            End If

            Dim SR = New Info.SourceResource(TheSourceResourceFile)

            Dim TargetResource = New IO.FileInfo(TheTargetResourceFile)

            If Not TargetResource.Directory.Exists Then
                '' Create target resource directory
                TargetResource.Directory.Create()
            End If

            If Not TargetResource.Exists Then
                '' Create target resource from source
                CreateTargetFromSource(TheSourceResourceFile, TheTargetResourceFile)

                '' Load target
                XD = XDocument.Load(TheTargetResourceFile)

            Else
                '' Target exists

                '' Load target
                XD = XDocument.Load(TheTargetResourceFile)

                '' Make sure that the keys are the same
                If Not SR.Keys.SequenceEqual(Me.Keys) Then
                    '' Keys have changed, re-create target

                    IO.File.Delete(TheTargetResourceFile)
                    CreateTargetFromSource(TheSourceResourceFile, TheTargetResourceFile)

                    '' Load fresh target
                    Dim XD2 = XDocument.Load(TheTargetResourceFile)

                    '' Fill fresh target with old data
                    For Each D In XD.<root>.<data>
                        Dim Name = D.@name

                        Dim NewD = XD2.<root>.<data>.Where(Function(D2) D2.@name = Name)

                        '' Copy the old value to the new file
                        If NewD.Any() Then
                            NewD.<value>.First().Value = D.<value>.Value
                        End If

                    Next

                    '' Save the updated values
                    XD2.Save(TheTargetResourceFile)

                    '' Load target
                    XD = XDocument.Load(TheTargetResourceFile)

                End If
            End If

        End Sub

        Private Sub CreateTargetFromSource(TheSourceResourceFile As String, TheTargetResourceFile As String)
            '' Create target resource from source
            Dim XD = XDocument.Load(TheSourceResourceFile)

            For Each Value In XD.<root>.<data>.<value>
                Value.Value = ""
            Next

            '' Save the cleaned up source into the target
            XD.Save(TheTargetResourceFile)

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

        Public Sub SetValue(TheKey As String, TheValue As String)
            Dim SelectedKey = XD.<root>.<data>.SingleOrDefault(Function(D) D.@name = TheKey)
            If SelectedKey Is Nothing Then
                Throw New KeyNotFoundException()
            End If

            Dim SelectedValue = SelectedKey.<value>.SingleOrDefault()
            If SelectedValue Is Nothing Then
                Throw New FormatException()
            End If

            SelectedValue.Value = TheValue

            Save()
        End Sub

        Private Sub Save()
            XD.Save(TargetResourceFileName)
        End Sub

    End Class
End Namespace