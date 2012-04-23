Namespace Info
    Public Class TranslationJobState

        Private FileName As String

        Public ReadOnly TargetLanguage As String

        Public ReadOnly Keys As New List(Of Key)

        Public Class Key
            Public ReadOnly Name As String

            Public Property Status As String
            Public Property JobId As String
            Public Property GroupId As String

            Public Property SubmitTime As DateTime
            Public Property LastQueryTime As DateTime

            Public Property OriginalValue As String
            Public Property SentValue As String
            Public Property Prefix As String
            Public Property Suffix As String

            Sub New(TheName As String)
                Name = TheName
            End Sub

            Sub New(TheKey As XElement)
                Name = TheKey.@Name
                Status = TheKey.<Status>.Value
                JobId = TheKey.<JobId>.Value
                GroupId = TheKey.<GroupId>.Value
                SubmitTime = DateTime.Parse(TheKey.<SubmitTime>.Value)
                LastQueryTime = DateTime.Parse(TheKey.<LastQueryTime>.Value)
                OriginalValue = TheKey.<OriginalValue>.Value
                Prefix = TheKey.<Prefix>.Value
                Suffix = TheKey.<Suffix>.Value
            End Sub

            Public Function ToXml() As XElement
                Return <Key Name=<%= Name %>>
                           <Status><%= Status %></Status>
                           <JobId><%= JobId %></JobId>
                           <GroupId><%= GroupId %></GroupId>
                           <SubmitTime><%= SubmitTime %></SubmitTime>
                           <LastQueryTime><%= LastQueryTime %></LastQueryTime>
                           <OriginalValue><%= OriginalValue %></OriginalValue>
                           <Prefix><%= Prefix %></Prefix>
                           <Suffix><%= Suffix %></Suffix>
                       </Key>
            End Function

        End Class

        Sub New(TheTargetLanguage As String, TheFileName As String)
            TargetLanguage = TheTargetLanguage
            FileName = TheFileName

            '' Load any existing file
            If IO.File.Exists(TheFileName) Then
                Dim XD = XDocument.Load(TheFileName, LoadOptions.PreserveWhitespace)

                If XD.<TranslationJobState>.<TargetLanguage>.Value <> TheTargetLanguage Then
                    Throw New Exception("Invalid target language in job state.")
                End If

                '' Parse each key
                For Each K In XD.<TranslationJobState>.<Keys>.<Key>
                    Keys.Add(New Key(K))
                Next
            End If
        End Sub

        Private Function ToXml() As XElement
            Return <TranslationJobState>
                       <TargetLanguage><%= TargetLanguage %></TargetLanguage>
                       <Keys>
                           <%= Keys.Select(Function(K) K.ToXml()) %>
                       </Keys>
                   </TranslationJobState>
        End Function

        Public Sub Save()
            Dim XD = New XDocument(ToXml())
            XD.Save(FileName)
        End Sub

    End Class
End Namespace