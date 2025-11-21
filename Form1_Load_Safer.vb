Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
    Try
        If bLogDebug = True Then LogEvent("Radio Mixer starting...")
        If bLogDebug = True Then LogEvent("=== Form1_Load START ===")
    Catch ex As Exception
        ' If even logging fails, we can't do much - but don't crash
    End Try

    ' === 1. Initialize Media Foundation ===
    Try
        InitMediaFoundation()
        If bLogDebug = True Then LogEvent("✓ MediaFoundation initialized")
    Catch ex As Exception
        LogErr("Error " & $"InitMediaFoundation failed: {ex.Message}")
        ' Don't return - try to continue
    End Try

    ' === 2. Force Auto Mode ===
    Try
        ForceAutoModeOnStartup()
        If bLogDebug = True Then LogEvent("✓ Auto mode forced")
    Catch ex As Exception
        LogErr("Error " & $"ForceAutoModeOnStartup failed: {ex.Message}")
    End Try

    ' === 3. List Available Audio Devices ===
    Try
        If bLogDebug = True Then LogEvent($"Total audio devices: {WaveOut.DeviceCount}")
        For i = 0 To WaveOut.DeviceCount - 1
            Try
                Dim caps = WaveOut.GetCapabilities(i)
                If bLogDebug = True Then LogEvent($"  Device {i}: {caps.ProductName} ({caps.Channels} channels)")
            Catch ex As Exception
                LogEvent($"  Device {i}: Error reading capabilities - {ex.Message}")
            End Try
        Next
    Catch ex As Exception
        LogErr("Error " & $"Device enumeration failed: {ex.Message}")
    End Try

    ' === 4. Safe Device Selection with Fallback ===
    Dim selectedDevice As Integer = -1
    Try
        Dim devicePreferences() As Integer = {0, -1}  ' Try device 0 first, then default

        For Each preferredDevice In devicePreferences
            Try
                If preferredDevice = -1 Then
                    ' Use default device
                    selectedDevice = -1
                    If bLogDebug = True Then LogEvent("Using default audio device")
                    Exit For
                ElseIf preferredDevice < WaveOut.DeviceCount Then
                    ' Test if device is accessible
                    Dim testCaps = WaveOut.GetCapabilities(preferredDevice)
                    selectedDevice = preferredDevice
                    If bLogDebug = True Then LogEvent($"✓ Selected device {selectedDevice}: {testCaps.ProductName}")
                    Exit For
                End If
            Catch ex As Exception
                LogEvent("Error " & $"Device {preferredDevice} not available: {ex.Message}")
            End Try
        Next
    Catch ex As Exception
        LogErr("Error " & $"Device selection failed: {ex.Message}")
        selectedDevice = -1 ' Fallback to default
    End Try

    ' === 5. Create Audio Output ===
    Try
        If selectedDevice = -1 Then
            ' Use default device (don't specify DeviceNumber)
            output = New WaveOutEvent() With {.DesiredLatency = 100}
            If bLogDebug = True Then LogEvent("✓ Audio output created (default device)")
        Else
            output = New WaveOutEvent() With {
                .DesiredLatency = 100,
                .DeviceNumber = selectedDevice
            }
            Try
                If bLogDebug = True Then LogEvent($"✓ Audio output created: device {selectedDevice} - {WaveOut.GetCapabilities(selectedDevice).ProductName}")
            Catch
                If bLogDebug = True Then LogEvent($"✓ Audio output created: device {selectedDevice}")
            End Try
        End If
    Catch ex As Exception
        LogErr("CRITICAL: Audio output creation failed: " & ex.Message)
        MessageBox.Show("Critical Error: Could not initialize audio output." & vbCrLf & ex.Message, "Audio Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        ' This is critical - but let's still try to continue
    End Try

    ' === 6. Hook Output Events ===
    Try
        If output IsNot Nothing AndAlso Not outputStoppedHooked Then
            AddHandler output.PlaybackStopped, AddressOf OnPlaybackStopped
            outputStoppedHooked = True
            If bLogDebug = True Then LogEvent("✓ PlaybackStopped handler attached")
        End If
    Catch ex As Exception
        LogErr("Error " & $"Failed to hook PlaybackStopped event: {ex.Message}")
    End Try

    ' === 7. Setup Playhead Timer ===
    Try
        AddHandler playheadTimer.Tick, AddressOf OnPlayheadTick
        playheadTimer.Start()
        If bLogDebug = True Then LogEvent("✓ Playhead timer started")
    Catch ex As Exception
        LogErr("Error " & $"Playhead timer failed: {ex.Message}")
    End Try

    ' === 8. Setup DateTime Timer ===
    Try
        AddHandler dateTimeTimer.Tick, AddressOf OnDateTimeTick
        dateTimeTimer.Start()
        If bLogDebug = True Then LogEvent("✓ DateTime timer started")
    Catch ex As Exception
        LogErr("Error " & $"DateTime timer failed: {ex.Message}")
    End Try

    ' === 9. Setup Schedule Check Timer ===
    Try
        AddHandler scheduleCheckTimer.Tick, AddressOf OnScheduleCheckTick
        scheduleCheckTimer.Start()
        If bLogDebug = True Then LogEvent("✓ Schedule check timer started")
    Catch ex As Exception
        LogErr("Error " & $"Schedule check timer failed: {ex.Message}")
    End Try

    ' === 10. Setup Live Watch Timer ===
    Try
        AddHandler liveWatchTimer.Tick, AddressOf OnLiveWatchTick
        liveWatchTimer.Start()
        If bLogDebug = True Then LogEvent("✓ Live watch timer started")
    Catch ex As Exception
        LogErr("Error " & $"Live watch timer failed: {ex.Message}")
    End Try

    ' === 11. Auto-Start Playlist ===
    Try
        If autoStartEnabled Then
            LoadAndStartRandomPlaylist()
            If bLogDebug = True Then LogEvent("✓ Auto-start playlist loaded")
        End If
    Catch ex As Exception
        LogErr("Error " & $"Auto-start playlist failed: {ex.Message}")
    End Try

    ' === 12. Start Hourly Scheduler ===
    Try
        StartHourlyScheduler()
        If bLogDebug = True Then LogEvent("✓ Hourly scheduler started")
    Catch ex As Exception
        LogErr("Error " & $"Hourly scheduler failed: {ex.Message}")
    End Try

    ' === 13. Launch Async Tasks ===
    Try
        Dim _Ignore As Task = LoadRNZNews()
    Catch ex As Exception
        LogErr("Error " & $"LoadRNZNews failed: {ex.Message}")
    End Try

    Try
        Dim _Ignore1 As Task = tbGetTides()
    Catch ex As Exception
        LogErr("Error " & $"tbGetTides failed: {ex.Message}")
    End Try

    Try
        Dim _Ignore2 As Task = EnsureAnnouncementMp3sAsync()
    Catch ex As Exception
        LogErr("Error " & $"EnsureAnnouncementMp3sAsync failed: {ex.Message}")
    End Try

    Try
        Dim _Ignore3 As Task = GetNewsHeadLinesAsync()
    Catch ex As Exception
        LogErr("Error " & $"GetNewsHeadLinesAsync failed: {ex.Message}")
    End Try

    ' === 14. Load Playlists ===
    Try
        LoadPlaylistsIntoCombo()
        If bLogDebug = True Then LogEvent("✓ Playlists loaded into combo")
    Catch ex As Exception
        LogErr("Error " & $"LoadPlaylistsIntoCombo failed: {ex.Message}")
    End Try

    ' === 15. Check FFmpeg ===
    Try
        CheckFFmpegOrWarn()
        If bLogDebug = True Then LogEvent("✓ FFmpeg check completed")
    Catch ex As Exception
        LogErr("Error " & $"CheckFFmpegOrWarn failed: {ex.Message}")
    End Try

    ' === 16. Start Email Notification Timer ===
    Try
        emailNotificationTimer = New System.Windows.Forms.Timer() With {.Interval = 60000}
        AddHandler emailNotificationTimer.Tick, AddressOf OnEmailNotificationTick
        emailNotificationTimer.Start()
        If bLogDebug = True Then LogEvent("✓ Email notification timer started")
    Catch ex As Exception
        LogErr("Error " & $"Email notification timer failed: {ex.Message}")
    End Try

    ' === 17. Final Status ===
    Try
        Me.Text = "Radio Mixer - Ready"
        If bLogDebug = True Then LogEvent("Radio Mixer started successfully")
        If bLogDebug = True Then LogEvent("=== Form1_Load END ===")
    Catch ex As Exception
        LogErr("Error " & $"Setting final status failed: {ex.Message}")
    End Try
End Sub
