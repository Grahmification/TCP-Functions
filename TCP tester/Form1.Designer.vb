<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class Form1
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.TextBox_ListenPort = New System.Windows.Forms.TextBox()
        Me.TextBox_RemotePort = New System.Windows.Forms.TextBox()
        Me.TextBox_RemoteIP = New System.Windows.Forms.TextBox()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.Label3 = New System.Windows.Forms.Label()
        Me.Label4 = New System.Windows.Forms.Label()
        Me.TextBox1 = New System.Windows.Forms.TextBox()
        Me.RichTextBox1 = New System.Windows.Forms.RichTextBox()
        Me.Button_send = New System.Windows.Forms.Button()
        Me.Button_receive = New System.Windows.Forms.Button()
        Me.Button_SendFile = New System.Windows.Forms.Button()
        Me.Button_ReceiveFile = New System.Windows.Forms.Button()
        Me.SuspendLayout()
        '
        'TextBox_ListenPort
        '
        Me.TextBox_ListenPort.Location = New System.Drawing.Point(39, 20)
        Me.TextBox_ListenPort.Name = "TextBox_ListenPort"
        Me.TextBox_ListenPort.Size = New System.Drawing.Size(181, 20)
        Me.TextBox_ListenPort.TabIndex = 0
        Me.TextBox_ListenPort.Text = "8000"
        '
        'TextBox_RemotePort
        '
        Me.TextBox_RemotePort.Location = New System.Drawing.Point(39, 72)
        Me.TextBox_RemotePort.Name = "TextBox_RemotePort"
        Me.TextBox_RemotePort.Size = New System.Drawing.Size(181, 20)
        Me.TextBox_RemotePort.TabIndex = 1
        Me.TextBox_RemotePort.Text = "8000"
        '
        'TextBox_RemoteIP
        '
        Me.TextBox_RemoteIP.Location = New System.Drawing.Point(39, 46)
        Me.TextBox_RemoteIP.Name = "TextBox_RemoteIP"
        Me.TextBox_RemoteIP.Size = New System.Drawing.Size(181, 20)
        Me.TextBox_RemoteIP.TabIndex = 2
        Me.TextBox_RemoteIP.Text = "127.0.0.1"
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Location = New System.Drawing.Point(226, 23)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(57, 13)
        Me.Label2.TabIndex = 4
        Me.Label2.Text = "Listen Port"
        '
        'Label3
        '
        Me.Label3.AutoSize = True
        Me.Label3.Location = New System.Drawing.Point(226, 49)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(57, 13)
        Me.Label3.TabIndex = 5
        Me.Label3.Text = "Remote IP"
        '
        'Label4
        '
        Me.Label4.AutoSize = True
        Me.Label4.Location = New System.Drawing.Point(226, 79)
        Me.Label4.Name = "Label4"
        Me.Label4.Size = New System.Drawing.Size(66, 13)
        Me.Label4.TabIndex = 6
        Me.Label4.Text = "Remote Port"
        '
        'TextBox1
        '
        Me.TextBox1.Location = New System.Drawing.Point(331, 76)
        Me.TextBox1.Name = "TextBox1"
        Me.TextBox1.Size = New System.Drawing.Size(208, 20)
        Me.TextBox1.TabIndex = 7
        '
        'RichTextBox1
        '
        Me.RichTextBox1.Location = New System.Drawing.Point(12, 116)
        Me.RichTextBox1.Name = "RichTextBox1"
        Me.RichTextBox1.Size = New System.Drawing.Size(280, 177)
        Me.RichTextBox1.TabIndex = 8
        Me.RichTextBox1.Text = ""
        '
        'Button_send
        '
        Me.Button_send.Location = New System.Drawing.Point(358, 49)
        Me.Button_send.Name = "Button_send"
        Me.Button_send.Size = New System.Drawing.Size(75, 23)
        Me.Button_send.TabIndex = 9
        Me.Button_send.Text = "Send"
        Me.Button_send.UseVisualStyleBackColor = True
        '
        'Button_receive
        '
        Me.Button_receive.Location = New System.Drawing.Point(439, 49)
        Me.Button_receive.Name = "Button_receive"
        Me.Button_receive.Size = New System.Drawing.Size(75, 23)
        Me.Button_receive.TabIndex = 10
        Me.Button_receive.Text = "Receive"
        Me.Button_receive.UseVisualStyleBackColor = True
        '
        'Button_SendFile
        '
        Me.Button_SendFile.Location = New System.Drawing.Point(391, 157)
        Me.Button_SendFile.Name = "Button_SendFile"
        Me.Button_SendFile.Size = New System.Drawing.Size(75, 23)
        Me.Button_SendFile.TabIndex = 11
        Me.Button_SendFile.Text = "Send File"
        Me.Button_SendFile.UseVisualStyleBackColor = True
        '
        'Button_ReceiveFile
        '
        Me.Button_ReceiveFile.Location = New System.Drawing.Point(391, 186)
        Me.Button_ReceiveFile.Name = "Button_ReceiveFile"
        Me.Button_ReceiveFile.Size = New System.Drawing.Size(75, 23)
        Me.Button_ReceiveFile.TabIndex = 12
        Me.Button_ReceiveFile.Text = "Receive File"
        Me.Button_ReceiveFile.UseVisualStyleBackColor = True
        '
        'Form1
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(561, 307)
        Me.Controls.Add(Me.Button_ReceiveFile)
        Me.Controls.Add(Me.Button_SendFile)
        Me.Controls.Add(Me.Button_receive)
        Me.Controls.Add(Me.Button_send)
        Me.Controls.Add(Me.RichTextBox1)
        Me.Controls.Add(Me.TextBox1)
        Me.Controls.Add(Me.Label4)
        Me.Controls.Add(Me.Label3)
        Me.Controls.Add(Me.Label2)
        Me.Controls.Add(Me.TextBox_RemoteIP)
        Me.Controls.Add(Me.TextBox_RemotePort)
        Me.Controls.Add(Me.TextBox_ListenPort)
        Me.Name = "Form1"
        Me.Text = "Form1"
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents TextBox_ListenPort As System.Windows.Forms.TextBox
    Friend WithEvents TextBox_RemotePort As System.Windows.Forms.TextBox
    Friend WithEvents TextBox_RemoteIP As System.Windows.Forms.TextBox
    Friend WithEvents Label2 As System.Windows.Forms.Label
    Friend WithEvents Label3 As System.Windows.Forms.Label
    Friend WithEvents Label4 As System.Windows.Forms.Label
    Friend WithEvents TextBox1 As System.Windows.Forms.TextBox
    Friend WithEvents RichTextBox1 As System.Windows.Forms.RichTextBox
    Friend WithEvents Button_send As System.Windows.Forms.Button
    Friend WithEvents Button_receive As System.Windows.Forms.Button
    Friend WithEvents Button_SendFile As System.Windows.Forms.Button
    Friend WithEvents Button_ReceiveFile As System.Windows.Forms.Button

End Class
