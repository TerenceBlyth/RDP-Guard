Imports NAudio.Wave
Imports System.IO

''' <summary>
''' Simple test program to demonstrate silence trimming functionality
''' Run this to verify silence trimming works before integrating into Form1
''' </summary>
Module SilenceTrimmingTest

    Sub Main()
        Console.WriteLine("========================================")
        Console.WriteLine("Silence Trimming Test")
        Console.WriteLine("========================================")
        Console.WriteLine()

        ' Test with an audio file
        Console.Write("Enter path to an audio file (MP3/WAV): ")
        Dim filePath As String = Console.ReadLine()

        If Not File.Exists(filePath) Then
            Console.WriteLine($"ERROR: File not found: {filePath}")
            Console.ReadKey()
            Return
        End If

        Try
            TestSilenceTrimming(filePath)
        Catch ex As Exception
            Console.WriteLine($"ERROR: {ex.Message}")
            Console.WriteLine(ex.StackTrace)
        End Try

        Console.WriteLine()
        Console.WriteLine("Press any key to exit...")
        Console.ReadKey()
    End Sub

    Sub TestSilenceTrimming(filePath As String)
        Console.WriteLine()
        Console.WriteLine($"Testing file: {Path.GetFileName(filePath)}")
        Console.WriteLine()

        ' Open the audio file
        Using reader As New AudioFileReader(filePath)
            Dim originalDuration = reader.TotalTime

            Console.WriteLine($"Original duration: {originalDuration:mm\:ss\.fff}")
            Console.WriteLine()

            ' Test with different threshold settings
            TestWithThreshold(reader, -30.0F, "Aggressive")
            reader.Position = 0

            TestWithThreshold(reader, -40.0F, "Balanced (recommended)")
            reader.Position = 0

            TestWithThreshold(reader, -50.0F, "Conservative")
            reader.Position = 0

            Console.WriteLine()
            Console.WriteLine("Recommendation:")
            Console.WriteLine("- Use -40 dB for most tracks")
            Console.WriteLine("- Use -30 dB if you have lots of trailing silence")
            Console.WriteLine("- Use -50 dB if tracks have quiet fade-outs being cut")
        End Using
    End Sub

    Sub TestWithThreshold(reader As AudioFileReader, thresholdDb As Single, description As String)
        Console.WriteLine($"Testing with {thresholdDb} dB ({description}):")

        ' Reset position
        reader.Position = 0

        ' Create silence trimming provider
        Dim silenceTrimmer As New SilenceTrimmingSampleProvider(
            reader.ToSampleProvider(),
            thresholdDb,
            TimeSpan.FromSeconds(0.5)
        )

        ' Read through the entire stream to find effective duration
        Dim buffer(reader.WaveFormat.SampleRate * reader.WaveFormat.Channels - 1) As Single
        Dim totalSamplesRead As Long = 0
        Dim samplesRead As Integer

        Do
            samplesRead = silenceTrimmer.Read(buffer, 0, buffer.Length)
            totalSamplesRead += samplesRead
        Loop While samplesRead > 0

        ' Calculate trimmed duration
        Dim samplesPerSecond = reader.WaveFormat.SampleRate * reader.WaveFormat.Channels
        Dim trimmedDuration = TimeSpan.FromSeconds(totalSamplesRead / samplesPerSecond)

        ' Calculate how much was trimmed
        Dim silenceTrimmed = reader.TotalTime - trimmedDuration

        Console.WriteLine($"  Trimmed duration: {trimmedDuration:mm\:ss\.fff}")
        Console.WriteLine($"  Silence removed:  {silenceTrimmed:ss\.fff} seconds")
        Console.WriteLine($"  Percentage kept:  {(trimmedDuration.TotalSeconds / reader.TotalTime.TotalSeconds) * 100:F1}%")
        Console.WriteLine()
    End Sub

End Module
