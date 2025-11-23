' Button to skip to 25% of the current song
' Add this button to your form and wire up the Click event to this handler

Private Sub btnSkipTo25_Click(sender As Object, e As EventArgs) Handles btnSkipTo25.Click
    Try
        SyncLock musicReaderLock
            ' Check if we have an active music reader
            If musicReader Is Nothing Then
                LogEvent("Cannot skip: No active music player")
                MessageBox.Show("No song is currently playing", "Skip to 25%", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            ' Get total duration
            Dim totalTime As TimeSpan = musicReader.TotalTime

            ' Check if we have valid duration (some streams may not have known duration)
            If totalTime.TotalMilliseconds <= 0 Then
                LogEvent("Cannot skip: Unknown song duration")
                MessageBox.Show("Cannot skip - song duration is unknown", "Skip to 25%", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            ' Calculate 25% position
            Dim targetPosition As TimeSpan = TimeSpan.FromMilliseconds(totalTime.TotalMilliseconds * 0.25)

            ' Set the new position
            musicReader.CurrentTime = targetPosition

            ' Reset tail silence accumulator since we're jumping in the track
            tailSilentAccumMs = 0

            ' Log the action
            Dim currentFileName As String = If(String.IsNullOrWhiteSpace(musicPath), "Unknown", System.IO.Path.GetFileName(musicPath))
            LogEvent($"⏩ Skipped to 25% of '{currentFileName}' - Position: {targetPosition:mm\:ss} / {totalTime:mm\:ss}")

        End SyncLock

    Catch ex As Exception
        LogErr($"Error skipping to 25%: {ex.Message}")
        MessageBox.Show($"Error skipping to 25%: {ex.Message}", "Skip Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
    End Try
End Sub

' ===== DESIGNER CODE =====
' Add this to your form's Designer.vb file or designer section:
'
' Friend WithEvents btnSkipTo25 As Button
'
' In InitializeComponent():
'
' Me.btnSkipTo25 = New Button()
' Me.btnSkipTo25.Location = New Point(x, y)  ' Set your desired location
' Me.btnSkipTo25.Name = "btnSkipTo25"
' Me.btnSkipTo25.Size = New Size(100, 30)
' Me.btnSkipTo25.Text = "Skip to 25%"
' Me.btnSkipTo25.UseVisualStyleBackColor = True
' Me.Controls.Add(Me.btnSkipTo25)
