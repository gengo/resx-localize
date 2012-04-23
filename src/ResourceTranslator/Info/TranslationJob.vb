Namespace Info
    Public Class TranslationJob

        Public Class TargetLanguage
            Public Code As String
            Public ForceSend As Boolean
            Public UsePreferredTranslators As Boolean

            Public Shared Function Parse(TargetLanguage As XElement) As TargetLanguage
                Dim R = New TargetLanguage

                If TargetLanguage.@ForceSend <> "" Then
                    Boolean.TryParse(TargetLanguage.@ForceSend, R.ForceSend)
                End If

                If TargetLanguage.@UsePreferredTranslators <> "" Then
                    Boolean.TryParse(TargetLanguage.@UsePreferredTranslators, R.UsePreferredTranslators)
                End If

                R.Code = TargetLanguage.Value

                Return R

            End Function

        End Class

        Public ReadOnly Job As XDocument
        Public ReadOnly SourceLanguage As String
        Public ReadOnly TargetLanguages As IEnumerable(Of TargetLanguage)
        Public ReadOnly TranslatorsComment As String

        Sub New(TheJobFileName As String)
            Job = XDocument.Load(TheJobFileName)

            SourceLanguage = Job.<TranslationJob>.<SourceLanguage>.Value
            TargetLanguages = Job.<TranslationJob>.<TargetLanguages>.<TargetLanguage>.Select(Function(E) TargetLanguage.Parse(E))
            TranslatorsComment = Job.<TranslationJob>.<TranslatorsComment>.Value

        End Sub

    End Class
End Namespace
