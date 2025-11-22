Private Sub MonitorMusicLoop()
    Dim consecutiveErrors As Integer = 0

    Do While Not stopLooping
        Try
            Threading.Thread.Sleep(500)

            ' ⭐ HARD BLOCK during news sequence
            If Threading.Volatile.Read(newsSequenceRunning) = 1 OrElse bPlayingNews Then
                Continue Do
            End If

            SyncLock musicReaderLock
                ' ===== No current reader? Decide how to continue =====
                If musicReader Is Nothing Then
                    ' Grace while output chain is being rebuilt
                    If DateTime.UtcNow < noReaderGraceUntilUtc Then
                        Continue Do
                    End If

                    ' ⛑️ Live entry guard: never exit Live during the first seconds after entering.
                    If bLiveModeActive AndAlso DateTime.UtcNow < liveEnterGuardUntilUtc Then
                        Continue Do
                    End If

                    ' ❌ REMOVED THE PROBLEMATIC BLOCK THAT WAS HERE
                    ' This was preventing monitoring in Live Mode:
                    ' If bLiveModeActive AndAlso (isPlayingScheduledPlaylist OrElse playQueue.Count > 0) Then
                    '     Continue Do
                    ' End If

                    ' If we had a scheduled slice but truly ran dry, drop back to random
                    If isPlayingScheduledPlaylist Then
                        isPlayingScheduledPlaylist = False
                        scheduledItemsRemaining = 0
                        playQueue.Clear()
                        LogEvent("Scheduled slice ended (no reader); returning to random.")
                    End If

                    ' Only now can we consider exiting Live due to a real no-reader condition
                    If bLiveModeActive Then
                        BeginInvoke(Sub() ExitLiveModeSafely("no reader"))
                        Continue Do
                    End If

                    Dim reason As String = ""
                    If GuardedAdvanceAllowed(AdvanceSource.Unknown, reason) Then
                        BeginInvoke(Sub() ReloadRandomPlaylistAndContinue())
                    End If

                    Continue Do
                End If

                ' ===== Read timing =====
                Dim elapsedMs As Integer = 0
                Dim remaining As TimeSpan = TimeSpan.Zero
                Dim total As TimeSpan = TimeSpan.Zero

                Try
                    elapsedMs = CInt(Math.Max(0, musicReader.CurrentTime.TotalMilliseconds))
                    total = musicReader.TotalTime

                    ' ⭐ USE EFFECTIVE REMAINING TIME (accounts for outro trim)
                    remaining = GetEffectiveRemainingTime(musicReader, musicPath)

                Catch
                    ' Ignore transient timing exceptions
                End Try

                ' ===== HARD GUARD during VO / news / cool-off =====
                Dim reasonBlock As String = ""
                If Not GuardedAdvanceAllowed(AdvanceSource.Unknown, reasonBlock) Then
                    tailSilentAccumMs = 0
                    Continue Do
                End If

                ' Unknown length (streams): skip end logic
                If total.TotalMilliseconds <= 0 Then
                    Continue Do
                End If

                ' ===== EARLY-ADVANCE ON TAIL SILENCE =====
                If elapsedMs >= minTrackPlayMs AndAlso
       remaining.TotalSeconds <= 60 AndAlso
       tailSilentAccumMs >= tailSilenceMinMs AndAlso
       remaining > TimeSpan.FromMilliseconds(500) Then

                    ' ⭐ NEW: Prevent multiple tail-silence dequeues
                    Dim nowUtc = DateTime.UtcNow
                    If (nowUtc - lastNearEndDequeueUtc).TotalSeconds < 5.0 Then
                        Continue Do
                    End If

                    Dim ctx As String = $"tail={tailSilentAccumMs}ms thr={tailSilenceMinMs}ms rem={remaining.TotalMilliseconds:F0}ms"
                    LogEvent($"Early-advance: {ctx}")
                    tailSilentAccumMs = 0

                    Dim nextTrack As String = Nothing
                    Dim haveNext As Boolean = False

                    ' ⭐ PEEK instead of dequeue
                    If isPlayingScheduledPlaylist AndAlso playQueue.Count > 0 Then
                        nextTrack = playQueue.Peek()
                        haveNext = True
                    ElseIf playQueue.Count > 0 Then
                        nextTrack = playQueue.Peek()
                        haveNext = True
                    End If




                    If haveNext AndAlso Not String.IsNullOrWhiteSpace(nextTrack) Then
                        ' ⭐ Check if advance is allowed BEFORE dequeuing
                        Dim advReason As String = ""
                        If GuardedAdvanceAllowed(AdvanceSource.MonitorTailSilence, advReason) Then
                            ' ⭐ MARK DEQUEUE TIME BEFORE DEQUEUING
                            lastNearEndDequeueUtc = nowUtc

                            ' NOW safe to dequeue
                            If isPlayingScheduledPlaylist Then
                                playQueue.Dequeue()
                                scheduledItemsRemaining -= 1
                                If scheduledItemsRemaining <= 0 Then isPlayingScheduledPlaylist = False

                                If bLiveModeActive Then
                                    Dim nextLid = GetLiveLidForPath(nextTrack)
                                    LogEvent("─────────────────────────────────────────────")
                                    LogEvent($"Live: ⏭️  DEQUEUED (tail-silence)")
                                    LogEvent($"  LID: {nextLid}")
                                    LogEvent($"  File: {System.IO.Path.GetFileName(nextTrack)}")
                                    LogEvent($"  Remaining after dequeue: {playQueue.Count}")
                                    LogEvent($"  scheduledItemsRemaining: {scheduledItemsRemaining}")
                                    LogEvent("─────────────────────────────────────────────")
                                End If
                            Else
                                playQueue.Dequeue()
                            End If

                            If bLiveModeActive Then
                                LogLiveQueueState("After tail-silence dequeue")
                            End If

                            BeginInvoke(Sub() RequestAdvance(AdvanceSource.MonitorTailSilence, nextTrack, ctx))
                        Else
                            LogEvent($"Tail-silence advance blocked: {advReason} - track NOT dequeued")
                        End If
                    Else
                        ' ✅ No next item: finish scheduled mode cleanly (if any) before reloading random
                        If isPlayingScheduledPlaylist Then
                            isPlayingScheduledPlaylist = False
                            scheduledItemsRemaining = 0
                            playQueue.Clear()
                            LogEvent("Scheduled slice finished; switching back to random (tail-silence branch).")
                        End If

                        ' ==== LIVE HOOK: if we were in Live Mode and ran out of items, exit Live ====
                        If bLiveModeActive Then
                            BeginInvoke(Sub() ExitLiveModeSafely("Live finished (tail-silence)"))
                            Continue Do
                        End If

                        BeginInvoke(Sub() ReloadRandomPlaylistAndContinue())
                    End If

                    Continue Do
                End If

                ' ===== PRE-LOAD NEXT TRACK (15 seconds before end) =====
                If remaining.TotalSeconds <= 15.0 AndAlso remaining.TotalSeconds > 14.0 Then
                    If String.IsNullOrWhiteSpace(nextTrackPath) Then
                        ' Pre-load next track into reader2
                        Dim nextTrack As String = Nothing

                        If isPlayingScheduledPlaylist AndAlso playQueue.Count > 0 Then
                            nextTrack = playQueue.Peek()  ' Don't dequeue yet
                        ElseIf playQueue.Count > 0 Then
                            nextTrack = playQueue.Peek()
                        End If

                        If Not String.IsNullOrWhiteSpace(nextTrack) Then
                            LogEvent($"Pre-loading next track at 15s: {System.IO.Path.GetFileName(nextTrack)}")
                            BeginInvoke(Sub() PreLoadNextTrack(nextTrack))
                        End If
                    End If
                End If

                ' ===== NEAR-END DETECTION =====
                Dim nearEnd As Boolean = False
                Try
                    nearEnd = (remaining.TotalSeconds < 4.0)
                Catch
                    nearEnd = False
                End Try

                If nearEnd Then
                    ' Check if next track is pre-loaded
                    If Not String.IsNullOrWhiteSpace(nextTrackPath) Then
                        ' ⭐ GAPLESS SWITCH: Next track is pre-loaded, perform seamless switch
                        Dim nowUtc = DateTime.UtcNow
                        If (nowUtc - lastNearEndDequeueUtc).TotalSeconds >= 5.0 Then
                            lastNearEndDequeueUtc = nowUtc

                            ' DETAILED LOGGING FOR LIVE MODE
                            If bLiveModeActive Then
                                LogEvent("═══════════════════════════════════════════")
                                LogEvent("LIVE MODE: GAPLESS SWITCH (pre-loaded track)")
                                LogEvent($"  Current LID: {currentLiveLid}")
                                LogEvent($"  Current Track: {System.IO.Path.GetFileName(musicPath)}")
                                LogEvent($"  Pre-loaded: {System.IO.Path.GetFileName(nextTrackPath)}")
                                LogEvent($"  Queue count: {playQueue.Count}")
                                LogEvent($"  Remaining time: {remaining.TotalSeconds:F1}s")
                                LogEvent("═══════════════════════════════════════════")
                            End If

                            ' Dequeue the item we pre-loaded
                            If isPlayingScheduledPlaylist AndAlso playQueue.Count > 0 Then
                                playQueue.Dequeue()
                                scheduledItemsRemaining -= 1
                                If scheduledItemsRemaining <= 0 Then isPlayingScheduledPlaylist = False

                                If bLiveModeActive Then
                                    Dim nextLid = GetLiveLidForPath(nextTrackPath)
                                    LogEvent("─────────────────────────────────────────────")
                                    LogEvent($"Live: ⏭️  DEQUEUED (gapless switch)")
                                    LogEvent($"  Next LID: {nextLid}")
                                    LogEvent($"  Next File: {System.IO.Path.GetFileName(nextTrackPath)}")
                                    LogEvent($"  Remaining in queue: {playQueue.Count}")
                                    LogEvent($"  scheduledItemsRemaining: {scheduledItemsRemaining}")
                                    LogEvent("─────────────────────────────────────────────")
                                End If
                            ElseIf playQueue.Count > 0 Then
                                playQueue.Dequeue()
                            End If

                            If bLiveModeActive Then
                                LogLiveQueueState("After gapless switch dequeue")
                            End If

                            ' Perform gapless switch
                            BeginInvoke(Sub() SwitchToPreLoadedTrack())
                        End If
                    Else
                        ' ⭐ FALLBACK: No pre-load available - use old method
                        ' ⭐ NEW: Prevent multiple near-end dequeues
                        Dim nowUtc = DateTime.UtcNow
                        If (nowUtc - lastNearEndDequeueUtc).TotalSeconds < 5.0 Then
                            Continue Do
                        End If

                        ' DETAILED LOGGING FOR LIVE MODE
                        If bLiveModeActive Then
                            LogEvent("═══════════════════════════════════════════")
                            LogEvent("LIVE MODE: NEAR-END DETECTED (fallback mode)")
                            LogEvent($"  Current LID: {currentLiveLid}")
                            LogEvent($"  Current Track: {System.IO.Path.GetFileName(musicPath)}")
                            LogEvent($"  Queue count: {playQueue.Count}")
                            LogEvent($"  scheduledItemsRemaining: {scheduledItemsRemaining}")
                            LogEvent($"  isPlayingScheduledPlaylist: {isPlayingScheduledPlaylist}")
                            LogEvent($"  Remaining time: {remaining.TotalSeconds:F1}s")
                            LogEvent("═══════════════════════════════════════════")
                        End If

                        Dim nextTrack As String = Nothing
                        Dim haveNext As Boolean = False

                        ' ⭐ PEEK instead of dequeue
                        If isPlayingScheduledPlaylist AndAlso playQueue.Count > 0 Then
                            nextTrack = playQueue.Peek()
                            haveNext = True

                            If bLiveModeActive Then
                                Dim nextLid = GetLiveLidForPath(nextTrack)
                                LogEvent("─────────────────────────────────────────────")
                                LogEvent($"Live: 👁️ PEEKED NEXT TRACK (not dequeued yet)")
                                LogEvent($"  Next LID: {nextLid}")
                                LogEvent($"  Next File: {System.IO.Path.GetFileName(nextTrack)}")
                                LogEvent($"  Queue count (still contains this): {playQueue.Count}")
                                LogEvent("─────────────────────────────────────────────")
                            End If

                        ElseIf playQueue.Count > 0 Then
                            nextTrack = playQueue.Peek()
                            haveNext = True
                        End If

                        If haveNext AndAlso Not String.IsNullOrWhiteSpace(nextTrack) Then
                            ' ⭐ Check if advance is allowed BEFORE dequeuing
                            Dim advReason As String = ""
                            If GuardedAdvanceAllowed(AdvanceSource.MonitorNearEnd, advReason) Then
                                ' ⭐ MARK DEQUEUE TIME BEFORE DEQUEUING
                                lastNearEndDequeueUtc = nowUtc

                                ' NOW safe to dequeue
                                If isPlayingScheduledPlaylist Then
                                    playQueue.Dequeue()
                                    scheduledItemsRemaining -= 1
                                    If scheduledItemsRemaining <= 0 Then isPlayingScheduledPlaylist = False

                                    If bLiveModeActive Then
                                        Dim nextLid = GetLiveLidForPath(nextTrack)
                                        LogEvent("─────────────────────────────────────────────")
                                        LogEvent($"Live: ⏭️  DEQUEUED NEXT TRACK (near-end fallback)")
                                        LogEvent($"  Next LID: {nextLid}")
                                        LogEvent($"  Next File: {System.IO.Path.GetFileName(nextTrack)}")
                                        LogEvent($"  Full Path: {nextTrack}")
                                        LogEvent($"  Remaining in queue after dequeue: {playQueue.Count}")
                                        LogEvent($"  scheduledItemsRemaining after decrement: {scheduledItemsRemaining}")
                                        LogEvent($"  isPlayingScheduledPlaylist: {isPlayingScheduledPlaylist}")
                                        LogEvent("─────────────────────────────────────────────")
                                    End If
                                Else
                                    playQueue.Dequeue()
                                End If

                                If bLiveModeActive Then
                                    LogLiveQueueState("After near-end dequeue (fallback)")
                                End If

                                Dim ctx As String = $"rem={remaining.TotalMilliseconds:F0}ms"
                                BeginInvoke(Sub() RequestAdvance(AdvanceSource.MonitorNearEnd, nextTrack, ctx))
                            Else
                                LogEvent($"Near-end advance blocked: {advReason} - track NOT dequeued")
                            End If
                        Else
                            ' ✅ No next item: finish scheduled mode cleanly (if any) before reloading random
                            If isPlayingScheduledPlaylist Then
                                isPlayingScheduledPlaylist = False
                                scheduledItemsRemaining = 0
                                playQueue.Clear()
                                LogEvent("Scheduled slice finished; switching back to random (near-end branch).")
                            End If

                            ' ==== LIVE HOOK: if we were in Live Mode and ran out of items, exit Live ====
                            If bLiveModeActive Then
                                LogEvent("═══════════════════════════════════════════")
                                LogEvent("LIVE MODE: QUEUE EXHAUSTED (near-end)")
                                LogEvent($"  Current LID: {currentLiveLid}")
                                LogEvent($"  playQueue.Count: {playQueue.Count}")
                                LogEvent($"  scheduledItemsRemaining: {scheduledItemsRemaining}")
                                LogEvent($"  All Live tracks completed")
                                LogEvent("  Exiting Live Mode...")
                                LogEvent("═══════════════════════════════════════════")
                                BeginInvoke(Sub() ExitLiveModeSafely("Live finished (near-end)"))
                                Continue Do
                            End If

                            BeginInvoke(Sub() ReloadRandomPlaylistAndContinue())
                        End If
                    End If

                    consecutiveErrors = 0
                    Continue Do
                End If
            End SyncLock

        Catch ex As Exception
            consecutiveErrors += 1
            If consecutiveErrors > 5 Then
                LogErr("Music loop monitor failed after 5 errors: " & ex.Message)
                Exit Do
            End If
        End Try
    Loop

    LogEvent("Music loop monitor exited")
End Sub
