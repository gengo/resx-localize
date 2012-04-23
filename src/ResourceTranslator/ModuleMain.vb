Imports ResourceTranslator.My

Module ModuleMain

    Private ShouldSend As Boolean = True
    Private ShouldGet As Boolean = True
    Private ForceGet As Boolean = False

    Sub Main()
        Dim LocationText = Environment.CurrentDirectory

        Dim Args = Environment.GetCommandLineArgs()
        If Args.Length > 1 Then
            Select Case Args(1).ToLowerInvariant()
                Case "/send"
                    ShouldGet = False

                Case "/get"
                    ShouldSend = False

                Case "/getnow"
                    ShouldSend = False
                    ForceGet = True

                Case Else
                    LocationText = Args(1)

            End Select
        End If

        If Not IO.Directory.Exists(LocationText) Then
            Console.WriteLine("{0} does not exit", LocationText)
            Return
        End If

        For Each Resx In IO.Directory.EnumerateFiles(LocationText, "*.resx")
            Dim JobFileName = Resx + ".job.xml"
            If IO.File.Exists(JobFileName) Then
                ProcessJob(Resx, JobFileName)
            End If
        Next

        Console.WriteLine()
        WriteColor(ConsoleColor.Magenta, "Run complete.")
        Console.WriteLine()

        WriteColor(ConsoleColor.White, "Press any key to exit...")
        Console.ReadKey(True)

    End Sub

    Private Sub ProcessJob(TheSourceResourceFileName As String, TheJobFileName As String)
        WriteColor(ConsoleColor.Green, "Starting job for '{0}'...", IO.Path.GetFileName(TheSourceResourceFileName))

        Dim Job = New Info.TranslationJob(TheJobFileName)

        '' Process each language
        For Each TargetLanguage In Job.TargetLanguages
            '' Load target resource
            Dim SourceResource = New IO.FileInfo(TheSourceResourceFileName)

            Dim TargetDirectoryName = SourceResource.Directory.FullName + "\" + TargetLanguage.Code

            Dim TargetResourceFileName = TargetDirectoryName + "\" + IO.Path.GetFileNameWithoutExtension(SourceResource.Name) + "." + TargetLanguage.Code + ".resx"

            Dim SR = New Info.SourceResource(TheSourceResourceFileName)

            Dim TR = New Info.TargetResource(
                     SourceResource.FullName,
                     TargetResourceFileName)

            '' Load job state for resource
            Dim JS = New Info.TranslationJobState(TargetLanguage.Code, TargetResourceFileName + ".jobstate.xml")

            '' Send any new translations
            If ShouldSend Then
                SendTranslations(Job, TargetLanguage, SR, TR, JS)
            End If

            If ShouldGet Then
                RetrieveTranslations(Job, SR, TR, JS)
            End If
            
        Next
    End Sub

    Private Function CreateClient() As MyGengo.MyGengoClient
        If My.Settings.UseSandBox Then
            Return New MyGengo.MyGengoClient(My.Settings.Sandbox_PublicKey, My.Settings.SandBox_PrivateKey, True)
        Else
            Return New MyGengo.MyGengoClient(My.Settings.PublicKey, My.Settings.PrivateKey, False)
        End If
    End Function

    ''' <summary>
    ''' Sends any new translations.
    ''' </summary>
    Private Sub SendTranslations(J As Info.TranslationJob, TL As Info.TranslationJob.TargetLanguage, SR As Info.SourceResource, TR As Info.TargetResource, JS As Info.TranslationJobState)
        '' Find any keys that have not been translated and are not pending
        Dim ToTranslate As New Queue(Of String)()

        For Each KeyNameI In SR.Keys.OrderBy(Function(K) K)
            Dim KeyName = KeyNameI

            Dim IsTranslated = False
            Dim IsInProgress = False

            Dim Key = JS.Keys.FirstOrDefault(Function(K) K.Name = KeyName)
            If Key IsNot Nothing Then
                '' Key found

                Dim SourceText = SR.GetValue(KeyName)

                Dim Prefix = ""
                Dim Suffix = ""
                Dim SourceTextTrimmed = TrimSource(SourceText, Prefix, Suffix)

                '' Check if this key has been already translated
                '' Make sure that the original value has not changed
                IsTranslated = ((Key.Status = "approved" OrElse Key.Status = "saved") AndAlso Key.OriginalValue = SR.GetValue(KeyName) AndAlso Key.Prefix = Prefix AndAlso Key.Suffix = Suffix)

                '' Check if a translation is in progress
                IsInProgress = (Key.Status <> "approved" AndAlso Key.Status <> "saved" AndAlso Key.Status <> "cancelled" AndAlso Key.Status <> "")

            End If

            If Not IsTranslated AndAlso Not IsInProgress Then
                '' We haven't translated this value and we're not translating it now
                '' Queue key to be translated
                ToTranslate.Enqueue(KeyName)
            End If
        Next

        '' Send translations
        If ToTranslate.Any() Then
            Dim ToTranslateKeys As IEnumerable(Of String)

            If My.Settings.Limit > 0 Then
                ToTranslateKeys = ToTranslate.Take(My.Settings.Limit)
            Else
                ToTranslateKeys = ToTranslate
            End If

            WriteColor(ConsoleColor.DarkGray, "  Sending {0} jobs for '{1}'", ToTranslateKeys.Count, JS.TargetLanguage)

            Dim G = CreateClient()

            Dim Tier As MyGengo.Tier
            If Not [Enum].TryParse(My.Settings.Tier, Tier) Then
                WriteColor(ConsoleColor.Yellow, "WARNING: Tier '{0}' is invalid. Assuming 'Machine'.", My.Settings.Tier)
                Tier = MyGengo.Tier.Machine
            End If

            Dim Jobs As New List(Of MyGengo.TranslationJob)(ToTranslateKeys.Count)
            Dim Keys As New List(Of Info.TranslationJobState.Key)(ToTranslateKeys.Count)

            For Each KeyNameI In ToTranslateKeys
                Dim KeyName = KeyNameI

                Dim SourceText = SR.GetValue(KeyName)

                '' Trim any special characters
                Dim Prefix = ""
                Dim Suffix = ""
                Dim SourceTextTrimmed = TrimSource(SourceText, Prefix, Suffix)

                Dim Job = New MyGengo.TranslationJob(KeyName + "." + JS.TargetLanguage, SourceTextTrimmed, J.SourceLanguage, JS.TargetLanguage, Tier)
                Job.Comment = J.TranslatorsComment

                Job.AutoApprove = My.Settings.AutoApprove
                Job.UsePreferredTranslators = TL.UsePreferredTranslators
                Job.ForceNewTranslation = TL.ForceSend

                Jobs.Add(Job)

                Dim Key = JS.Keys.SingleOrDefault(Function(K) K.Name = KeyName)
                If Key Is Nothing Then
                    Key = New Info.TranslationJobState.Key(KeyName)

                    '' Add new key
                    JS.Keys.Add(Key)
                End If

                '' Set key values for query
                Key.OriginalValue = SourceText
                Key.SentValue = SourceTextTrimmed
                Key.Prefix = Prefix
                Key.Suffix = Suffix
                Key.SubmitTime = Now
                Key.LastQueryTime = Now
                Key.JobId = ""
                Key.GroupId = ""

                Keys.Add(Key)

            Next

            Dim PostReply = G.PostTranslationJobs(Jobs.ToArray(), Jobs.Count > 1)

            '' Check response
            If PostReply.<xml>.<opstat>.Value <> "ok" Then
                WriteColor(ConsoleColor.Red, "ERROR: Incorrect opstat returned: {0}", PostReply.<xml>.<opstat>.Value)
                Return
            End If

            '' Store IDs
            Dim GroupId = PostReply.<xml>.<response>.<group_id>.Value()
            Dim Items = PostReply.<xml>.<response>.<jobs>.<item>.<item>

            For Each KeyI In Keys
                Dim Key = KeyI

                Dim Slug = Key.Name + "." + JS.TargetLanguage
                Dim SentValue = Key.SentValue

                Dim Item = Items.FirstOrDefault(Function(E) E.<body_src>.Value = SentValue)
                If Item Is Nothing Then
                    WriteColor(ConsoleColor.Yellow, "WARNING: Response item not found after submitting job: {0}", Slug)
                    Key.Status = ""
                    Continue For
                End If

                Key.GroupId = GroupId
                Key.JobId = Item.<job_id>.Value
                Key.Status = Item.<status>.Value

                If Key.Status = "approved" Then
                    '' We're done with this translation
                    '' Ok to query for it
                    Key.LastQueryTime = Date.MinValue
                End If
            Next

            '' Save state
            JS.Save()

        Else
            WriteColor(ConsoleColor.DarkGray, "  Nothing to translate for '" + JS.TargetLanguage + "'")
        End If

    End Sub

    Private ReadOnly TrimChars As String = "~!@#$%^&*()_+=-`[]\:;'"",/?>< " + vbCr + vbLf + vbTab

    ''' <summary>
    ''' We remove any special formatting from the beginning and end of strings, so as not to confuse the translators.
    ''' </summary>
    Private Function TrimSource(TheSource As String, ByRef ThePrefix As String, ByRef TheSuffix As String) As String
        Dim Trimmed = TheSource.TrimStart(TrimChars.ToCharArray())
        ThePrefix = TheSource.Substring(0, TheSource.Length - Trimmed.Length)

        TheSource = Trimmed

        Trimmed = TheSource.TrimEnd(TrimChars.ToCharArray())
        TheSuffix = TheSource.Substring(Trimmed.Length, TheSource.Length - Trimmed.Length)

        TheSource = Trimmed

        Return TheSource

    End Function

    Private Sub RetrieveTranslations(J As Info.TranslationJob, SR As Info.SourceResource, TR As Info.TargetResource, JS As Info.TranslationJobState)
        '' Determine which keys need to be retrieved
        'Dim Keys = SR.Keys.Intersect(JS.Keys.Where(Function(K) K.Status = "available" OrElse K.Status = "pending").Select(Function(K) K.Name))

        '' Check for statuses that need some user interaction
        Dim WaitingKeys = From K In JS.Keys
                          Where K.Status = "reviewable" OrElse K.Status = "revising"
                          Where SR.Keys.Contains(K.Name)

        For Each WK In WaitingKeys
            WriteColor(ConsoleColor.Yellow, "WARNING: '{0}' is waiting for user approval (status={1})", WK.Name, WK.Status)
        Next

        '' Find all the keys that we need to query
        '' Query for status of keys that have not been saved yet and are translating our current value
        Dim Keys As IEnumerable(Of Info.TranslationJobState.Key) = (From K In JS.Keys
                                                                    Where K.Status <> "saved"
                                                                    Where K.JobId <> ""
                                                                    Where SR.Keys.Contains(K.Name)
                                                                    Where SR.GetValue(K.Name) = K.OriginalValue).ToArray()

        Dim InProgressCount = Keys.Count()

        '' Throttle in non-sandbox mode
        If Not ForceGet AndAlso Not My.Settings.UseSandBox Then
            Keys = Keys.Where(Function(k) k.LastQueryTime < Now - My.Settings.QueryThrottle).ToArray()
        End If

        If My.Settings.Limit > 0 Then
            Keys = Keys.Take(My.Settings.Limit).ToArray()
        End If

        If Not Keys.Any() Then
            WriteColor(ConsoleColor.DarkGray, "  No jobs to check for completion at this time ({0} in progress)", InProgressCount)
            Return
        End If

        '' Set last query time
        For Each Key In Keys
            Key.LastQueryTime = Now
        Next

        '' Save last query times
        JS.Save()

        Dim G = CreateClient()

        Dim JobIds = From Key In Keys
                     Select Key.JobId

        WriteColor(ConsoleColor.DarkGray, "  Retrieving status of {0} jobs for '{1}'", JobIds.Count, JS.TargetLanguage)

        Dim PostReply = G.GetTranslationJobs(JobIds)

        '' Check response
        If PostReply.<xml>.<opstat>.Value <> "ok" Then
            WriteColor(ConsoleColor.Red, "ERROR: Incorrect opstat returned: {0}", PostReply.<xml>.<opstat>.Value)
            Return
        End If

        For Each ItemI In PostReply.<xml>.<response>.<jobs>.<item>
            Dim Item = ItemI

            Dim JobId = Item.<job_id>.Value

            Dim Key = Keys.FirstOrDefault(Function(K) K.JobId = JobId)

            If Key Is Nothing Then
                WriteColor(ConsoleColor.Red, "ERROR: Cannot find key for existing job. (JobId={0})", JobId)
                Continue For
            End If

            Key.Status = Item.<status>.Value()

            '' Save translation if available
            If Key.Status = "approved" Then
                Dim Translation = Item.<body_tgt>.Value()

                '' Re-add prefix and suffix
                Translation = Key.Prefix + Translation + Key.Suffix

                TR.SetValue(Key.Name, Translation)

                Key.Status = "saved"

                WriteColor(ConsoleColor.Cyan, "    Saved translation ({0}): {1}", JS.TargetLanguage, Key.Name)
            End If
            
        Next

        '' Save job state
        JS.Save()

    End Sub

    Private Lock As New Object()

    Private Sub WriteColor(TheColor As ConsoleColor, TheText As String, ParamArray Params() As Object)
        SyncLock Lock
            Console.ForegroundColor = TheColor
            Console.WriteLine(TheText, Params)
            Console.ResetColor()
        End SyncLock
    End Sub

End Module