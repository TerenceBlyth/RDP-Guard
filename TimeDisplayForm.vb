Imports System
Imports System.Windows.Forms

Public Class TimeDisplayForm
    Inherits Form

    ' Declare controls
    Private WithEvents timeTextBox As TextBox
    Private WithEvents updateTimer As Timer
    Private timeLabel As Label

    Public Sub New()
        ' Initialize the form
        InitializeComponent()
    End Sub

    Private Sub InitializeComponent()
        ' Set up the form
        Me.Text = "Time Display"
        Me.Size = New System.Drawing.Size(400, 200)
        Me.StartPosition = FormStartPosition.CenterScreen

        ' Create and configure the label
        timeLabel = New Label()
        timeLabel.Text = "Current Time:"
        timeLabel.Location = New System.Drawing.Point(20, 20)
        timeLabel.Size = New System.Drawing.Size(100, 20)
        timeLabel.Font = New System.Drawing.Font("Arial", 10, System.Drawing.FontStyle.Bold)

        ' Create and configure the textbox
        timeTextBox = New TextBox()
        timeTextBox.Location = New System.Drawing.Point(20, 50)
        timeTextBox.Size = New System.Drawing.Size(340, 30)
        timeTextBox.Font = New System.Drawing.Font("Arial", 14, System.Drawing.FontStyle.Regular)
        timeTextBox.ReadOnly = True
        timeTextBox.TextAlign = HorizontalAlignment.Center

        ' Create and configure the timer
        updateTimer = New Timer()
        updateTimer.Interval = 1000  ' Update every 1000 milliseconds (1 second)

        ' Add controls to the form
        Me.Controls.Add(timeLabel)
        Me.Controls.Add(timeTextBox)

        ' Set initial time and start the timer
        UpdateTime()
        updateTimer.Start()
    End Sub

    ' Timer tick event handler - updates the time display
    Private Sub updateTimer_Tick(sender As Object, e As EventArgs) Handles updateTimer.Tick
        UpdateTime()
    End Sub

    ' Method to update the textbox with current time
    Private Sub UpdateTime()
        ' Display time in various formats - choose the one you prefer:

        ' Option 1: Full date and time
        timeTextBox.Text = DateTime.Now.ToString("dddd, MMMM dd, yyyy hh:mm:ss tt")

        ' Option 2: Time only (12-hour format with AM/PM)
        ' timeTextBox.Text = DateTime.Now.ToString("hh:mm:ss tt")

        ' Option 3: Time only (24-hour format)
        ' timeTextBox.Text = DateTime.Now.ToString("HH:mm:ss")

        ' Option 4: Short date and time
        ' timeTextBox.Text = DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss tt")
    End Sub

    ' Clean up resources when form is closing
    Protected Overrides Sub Dispose(disposing As Boolean)
        If disposing Then
            If updateTimer IsNot Nothing Then
                updateTimer.Stop()
                updateTimer.Dispose()
            End If
        End If
        MyBase.Dispose(disposing)
    End Sub

    ' Entry point for the application
    <STAThread()>
    Shared Sub Main()
        Application.EnableVisualStyles()
        Application.SetCompatibleTextRenderingDefault(False)
        Application.Run(New TimeDisplayForm())
    End Sub
End Class
