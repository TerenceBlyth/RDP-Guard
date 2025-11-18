Imports NAudio.Wave

''' <summary>
''' Trims silence from the end of audio tracks by detecting when the audio level
''' stays below a threshold for a specified duration at the end of the file.
''' </summary>
Public Class SilenceTrimmingSampleProvider
    Implements ISampleProvider

    Private ReadOnly source As ISampleProvider
    Private ReadOnly silenceThreshold As Single
    Private ReadOnly minimumSilenceDuration As TimeSpan
    Private ReadOnly lookAheadSamples As Integer
    Private lookAheadBuffer As Single()
    Private bufferPosition As Integer
    Private bufferFilled As Integer
    Private hasMoreSamples As Boolean
    Private isFinished As Boolean

    ''' <summary>
    ''' Creates a new SilenceTrimmingSampleProvider
    ''' </summary>
    ''' <param name="source">Source audio provider</param>
    ''' <param name="silenceThresholdDb">Silence threshold in decibels (e.g., -40dB). More negative = more sensitive.</param>
    ''' <param name="minimumSilenceDuration">Minimum duration of silence to trim (e.g., 0.5 seconds)</param>
    Public Sub New(source As ISampleProvider,
                   Optional silenceThresholdDb As Single = -40.0F,
                   Optional minimumSilenceDuration As TimeSpan = Nothing)

        Me.source = source

        ' Convert dB to linear amplitude (0 dB = 1.0, -40 dB ≈ 0.01)
        Me.silenceThreshold = CSng(Math.Pow(10, silenceThresholdDb / 20.0))

        ' Default to 0.5 seconds if not specified
        If minimumSilenceDuration = Nothing Then
            minimumSilenceDuration = TimeSpan.FromSeconds(0.5)
        End If
        Me.minimumSilenceDuration = minimumSilenceDuration

        ' Calculate look-ahead buffer size (samples needed to detect minimum silence duration)
        Dim samplesPerSecond = source.WaveFormat.SampleRate * source.WaveFormat.Channels
        Me.lookAheadSamples = CInt(samplesPerSecond * minimumSilenceDuration.TotalSeconds)

        ' Create buffer for look-ahead
        Me.lookAheadBuffer = New Single(lookAheadSamples - 1) {}
        Me.bufferPosition = 0
        Me.bufferFilled = 0
        Me.hasMoreSamples = True
        Me.isFinished = False
    End Sub

    Public ReadOnly Property WaveFormat As WaveFormat Implements ISampleProvider.WaveFormat
        Get
            Return source.WaveFormat
        End Get
    End Property

    Public Function Read(buffer() As Single, offset As Integer, count As Integer) As Integer Implements ISampleProvider.Read
        If isFinished Then
            Return 0
        End If

        Dim samplesRead As Integer = 0

        ' Fill the output buffer
        While samplesRead < count
            ' If we need more data in our look-ahead buffer, fill it
            If bufferPosition >= bufferFilled Then
                bufferFilled = source.Read(lookAheadBuffer, 0, lookAheadSamples)
                bufferPosition = 0

                If bufferFilled = 0 Then
                    ' End of source stream
                    hasMoreSamples = False
                    isFinished = True
                    Exit While
                End If
            End If

            ' Check if the current look-ahead buffer contains only silence
            If hasMoreSamples AndAlso IsBufferSilent() Then
                ' We've detected silence that continues to the end of the file
                ' Stop reading here to trim the silence
                isFinished = True
                Exit While
            End If

            ' Copy from look-ahead buffer to output
            Dim samplesToCopy = Math.Min(count - samplesRead, bufferFilled - bufferPosition)
            Array.Copy(lookAheadBuffer, bufferPosition, buffer, offset + samplesRead, samplesToCopy)

            bufferPosition += samplesToCopy
            samplesRead += samplesToCopy
        End While

        Return samplesRead
    End Function

    ''' <summary>
    ''' Checks if the current look-ahead buffer contains only silence
    ''' </summary>
    Private Function IsBufferSilent() As Boolean
        ' Check all samples in the buffer
        For i As Integer = bufferPosition To bufferFilled - 1
            If Math.Abs(lookAheadBuffer(i)) > silenceThreshold Then
                Return False ' Found audio above threshold
            End If
        Next

        ' All samples are below threshold = silence detected
        Return True
    End Function

    ''' <summary>
    ''' Gets the current silence threshold in decibels
    ''' </summary>
    Public ReadOnly Property SilenceThresholdDb As Single
        Get
            Return CSng(20.0 * Math.Log10(silenceThreshold))
        End Get
    End Property

    ''' <summary>
    ''' Gets the minimum silence duration being detected
    ''' </summary>
    Public ReadOnly Property MinimumSilenceDuration As TimeSpan
        Get
            Return Me.minimumSilenceDuration
        End Get
    End Property
End Class
