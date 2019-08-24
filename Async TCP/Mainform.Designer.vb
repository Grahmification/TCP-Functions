<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class Mainform
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
        Me.Button_ClientConnect = New System.Windows.Forms.Button()
        Me.btn_send = New System.Windows.Forms.Button()
        Me.Btn_Serverconnect = New System.Windows.Forms.Button()
        Me.RichTextBox1 = New System.Windows.Forms.RichTextBox()
        Me.TextBox_send = New System.Windows.Forms.TextBox()
        Me.SuspendLayout()
        '
        'Button_ClientConnect
        '
        Me.Button_ClientConnect.Location = New System.Drawing.Point(21, 149)
        Me.Button_ClientConnect.Name = "Button_ClientConnect"
        Me.Button_ClientConnect.Size = New System.Drawing.Size(92, 23)
        Me.Button_ClientConnect.TabIndex = 0
        Me.Button_ClientConnect.Text = "Client Connect"
        Me.Button_ClientConnect.UseVisualStyleBackColor = True
        '
        'btn_send
        '
        Me.btn_send.Location = New System.Drawing.Point(21, 178)
        Me.btn_send.Name = "btn_send"
        Me.btn_send.Size = New System.Drawing.Size(92, 23)
        Me.btn_send.TabIndex = 1
        Me.btn_send.Text = "Send"
        Me.btn_send.UseVisualStyleBackColor = True
        '
        'Btn_Serverconnect
        '
        Me.Btn_Serverconnect.Location = New System.Drawing.Point(207, 207)
        Me.Btn_Serverconnect.Name = "Btn_Serverconnect"
        Me.Btn_Serverconnect.Size = New System.Drawing.Size(92, 23)
        Me.Btn_Serverconnect.TabIndex = 2
        Me.Btn_Serverconnect.Text = "Server Connect"
        Me.Btn_Serverconnect.UseVisualStyleBackColor = True
        '
        'RichTextBox1
        '
        Me.RichTextBox1.Location = New System.Drawing.Point(133, 12)
        Me.RichTextBox1.Name = "RichTextBox1"
        Me.RichTextBox1.Size = New System.Drawing.Size(229, 189)
        Me.RichTextBox1.TabIndex = 3
        Me.RichTextBox1.Text = ""
        '
        'TextBox_send
        '
        Me.TextBox_send.Location = New System.Drawing.Point(12, 219)
        Me.TextBox_send.Name = "TextBox_send"
        Me.TextBox_send.Size = New System.Drawing.Size(136, 20)
        Me.TextBox_send.TabIndex = 4
        '
        'Mainform
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(374, 251)
        Me.Controls.Add(Me.TextBox_send)
        Me.Controls.Add(Me.RichTextBox1)
        Me.Controls.Add(Me.Btn_Serverconnect)
        Me.Controls.Add(Me.btn_send)
        Me.Controls.Add(Me.Button_ClientConnect)
        Me.Name = "Mainform"
        Me.Text = "Form1"
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents Button_ClientConnect As System.Windows.Forms.Button
    Friend WithEvents btn_send As System.Windows.Forms.Button
    Friend WithEvents Btn_Serverconnect As System.Windows.Forms.Button
    Friend WithEvents RichTextBox1 As System.Windows.Forms.RichTextBox
    Friend WithEvents TextBox_send As System.Windows.Forms.TextBox

End Class
