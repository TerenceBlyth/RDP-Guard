' ============================================================================
' VB.NET TEXTBOX TIME DISPLAY - CODE EXAMPLES
' ============================================================================
' This file contains various code snippets for displaying time in a TextBox
' ============================================================================

' ----------------------------------------------------------------------------
' EXAMPLE 1: Simple Time Display (Add this code to your form)
' ----------------------------------------------------------------------------
' 1. Add a TextBox named "txtTime" to your form
' 2. Add a Timer named "Timer1" to your form
' 3. Set Timer1.Interval = 1000 (for 1 second updates)
' 4. Set Timer1.Enabled = True
' 5. Add this code to your form:

Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick
    txtTime.Text = DateTime.Now.ToString("hh:mm:ss tt")
End Sub


' ----------------------------------------------------------------------------
' EXAMPLE 2: Display Time on Form Load
' ----------------------------------------------------------------------------
Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
    ' Set initial time
    txtTime.Text = DateTime.Now.ToString("hh:mm:ss tt")

    ' Start timer for continuous updates
    Timer1.Interval = 1000
    Timer1.Start()
End Sub


' ----------------------------------------------------------------------------
' EXAMPLE 3: Different Time Format Options
' ----------------------------------------------------------------------------
Private Sub ShowDifferentTimeFormats()
    ' 12-hour format with AM/PM
    txtTime.Text = DateTime.Now.ToString("hh:mm:ss tt")
    ' Example output: 02:30:45 PM

    ' 24-hour format
    txtTime.Text = DateTime.Now.ToString("HH:mm:ss")
    ' Example output: 14:30:45

    ' Full date and time
    txtTime.Text = DateTime.Now.ToString("dddd, MMMM dd, yyyy hh:mm:ss tt")
    ' Example output: Monday, November 17, 2025 02:30:45 PM

    ' Short date and time
    txtTime.Text = DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss tt")
    ' Example output: 11/17/2025 02:30:45 PM

    ' Time only (no seconds)
    txtTime.Text = DateTime.Now.ToString("hh:mm tt")
    ' Example output: 02:30 PM

    ' Custom format
    txtTime.Text = DateTime.Now.ToString("hh:mm:ss tt 'on' MM/dd/yyyy")
    ' Example output: 02:30:45 PM on 11/17/2025
End Sub


' ----------------------------------------------------------------------------
' EXAMPLE 4: Button to Update Time Manually
' ----------------------------------------------------------------------------
Private Sub btnUpdateTime_Click(sender As Object, e As EventArgs) Handles btnUpdateTime.Click
    txtTime.Text = DateTime.Now.ToString("hh:mm:ss tt")
End Sub


' ----------------------------------------------------------------------------
' EXAMPLE 5: Complete Form Class with Time Display
' ----------------------------------------------------------------------------
Public Class frmTimeDisplay
    Private Sub frmTimeDisplay_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' Initialize textbox properties
        txtTime.Font = New Font("Arial", 14, FontStyle.Bold)
        txtTime.TextAlign = HorizontalAlignment.Center
        txtTime.ReadOnly = True

        ' Set up and start timer
        Timer1.Interval = 1000  ' 1 second
        Timer1.Enabled = True

        ' Display initial time
        UpdateTimeDisplay()
    End Sub

    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick
        UpdateTimeDisplay()
    End Sub

    Private Sub UpdateTimeDisplay()
        txtTime.Text = DateTime.Now.ToString("hh:mm:ss tt")
    End Sub
End Class


' ----------------------------------------------------------------------------
' EXAMPLE 6: Using TimeOfDay Property
' ----------------------------------------------------------------------------
Private Sub ShowTimeOfDay()
    ' Using TimeOfDay property
    txtTime.Text = DateTime.Now.TimeOfDay.ToString()
    ' Example output: 14:30:45.1234567
End Sub


' ----------------------------------------------------------------------------
' EXAMPLE 7: Formatted Time with Milliseconds
' ----------------------------------------------------------------------------
Private Sub ShowTimeWithMilliseconds()
    txtTime.Text = DateTime.Now.ToString("hh:mm:ss.fff tt")
    ' Example output: 02:30:45.123 PM
End Sub


' ----------------------------------------------------------------------------
' COMMON DATETIME FORMAT SPECIFIERS
' ----------------------------------------------------------------------------
' h  - Hour (12-hour, 1-12)
' hh - Hour (12-hour, 01-12)
' H  - Hour (24-hour, 0-23)
' HH - Hour (24-hour, 00-23)
' m  - Minute (0-59)
' mm - Minute (00-59)
' s  - Second (0-59)
' ss - Second (00-59)
' tt - AM/PM designator
' fff - Milliseconds
' dddd - Full day name (Monday)
' ddd - Abbreviated day name (Mon)
' MMMM - Full month name (January)
' MMM - Abbreviated month name (Jan)
' MM - Month (01-12)
' dd - Day (01-31)
' yyyy - Year (2025)
' yy - Year (25)
' ----------------------------------------------------------------------------
