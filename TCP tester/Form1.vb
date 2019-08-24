Imports System.Net.Sockets
Imports System.Net
Imports System.Text
Imports System.IO

Imports System.Threading


Public Class Form1
    Dim TCP As New TCP_Functions
    Delegate Sub SetTextCallback(ByVal [text] As String)
    Public Sub SetText(ByVal [text] As String)

        ' InvokeRequired required compares the thread ID of the
        ' calling thread to the thread ID of the creating thread.
        ' If these threads are different, it returns true.
        If Me.RichTextBox1.InvokeRequired Then
            Dim d As New SetTextCallback(AddressOf SetText)
            Me.Invoke(d, New Object() {[text]})
        Else
            Me.RichTextBox1.AppendText([text] & vbNewLine)
        End If
    End Sub

    Private Sub Button_send_Click(sender As Object, e As EventArgs) Handles Button_send.Click
        TCP.SendBytes(TCP.StringToBytes(TextBox1.Text), TextBox_RemoteIP.Text, CInt(TextBox_RemotePort.Text))
    End Sub

    Private Sub Button_receive_Click(sender As Object, e As EventArgs) Handles Button_receive.Click
        Dim T1 As New Thread(AddressOf receive)
        T1.Start(CInt(TextBox_ListenPort.Text))
    End Sub


    Private Sub receive(ByVal port As Integer)
        Dim message As String = TCP.BytesToString(TCP.ReceiveBytes(port))
        RichTextBox1.AppendText(message)
        RichTextBox1.AppendText(vbNewLine)
    End Sub


    Private Sub Button_SendFile_Click(sender As Object, e As EventArgs) Handles Button_SendFile.Click

        Dim ofd As New OpenFileDialog
        ofd.ShowDialog()
        Dim path As String = ofd.FileName

        'RichTextBox1.AppendText("Sending file...")
        'RichTextBox1.AppendText(vbNewLine)
        SetText("sending file...")

        TCP.SendBytes(TCP.FileToBytes(path), TextBox_RemoteIP.Text, CInt(TextBox_RemotePort.Text))

        SetText("File Sent.")
    End Sub

    Private Sub Button_ReceiveFile_Click(sender As Object, e As EventArgs) Handles Button_ReceiveFile.Click
        Dim T2 As New Thread(AddressOf ReceiveFile)
        T2.SetApartmentState(ApartmentState.STA)
        T2.Start(CInt(TextBox_ListenPort.Text))
    End Sub

    Private Sub ReceiveFile(ByVal port As Integer)

        Dim bytearr As Byte() = Me.ReceiveBytes(port)
        Dim sfd As New SaveFileDialog
        sfd.ShowDialog()

        Dim path As String = sfd.FileName

        TCP.BytesToFile(bytearr, path)

    End Sub

    Function ReceiveBytes(ByVal Port As Integer) As Byte()

        SetText("listening for connection...")
        Dim Listener As New TcpListener(IPAddress.Any, Port)
        Listener.Start(100)

        Dim client As TcpClient = Listener.AcceptTcpClient()
        Dim stream As NetworkStream = client.GetStream()
        Listener.Stop()
        SetText("starting download...")
        Dim receiveLength(3) As Byte

        Dim totalRead As Integer = 0
        Dim currentRead As Integer = 0

        While totalRead < receiveLength.Length
            currentRead = stream.Read(receiveLength, totalRead, receiveLength.Length - totalRead)
            totalRead += currentRead
        End While

        totalRead = 0
        currentRead = 0
        Dim receiveData(BitConverter.ToInt32(receiveLength, 0) - 1) As Byte 'critical for array size 

        While totalRead < receiveData.Length
            currentRead = stream.Read(receiveData, totalRead, receiveData.Length - totalRead)
            totalRead += currentRead
        End While
        SetText("complete")
        stream.Close()
        client.Close()


        Return receiveData
    End Function

End Class

Public Class TCP_Functions



    Const bufferSize As Integer = 1024
    Function SendBytes(ByVal SendData As Byte(), ByVal IP As String, ByVal Port As Integer) As Integer

        Dim TCP As New TcpClient
        TCP.Connect(IP, Port)
        Dim stream As NetworkStream = TCP.GetStream()

        Dim sendLength As Byte() = BitConverter.GetBytes(SendData.Length)

        stream.Write(sendLength, 0, sendLength.Length)

        Dim Totalsent As Integer = 0
        Dim Currentsent As Integer = bufferSize

        While (Totalsent < SendData.Length)

            If (SendData.Length - Totalsent < bufferSize) Then
                Currentsent = SendData.Length - Totalsent
            End If

            stream.Write(SendData, Totalsent, Currentsent)
            Totalsent += Currentsent
        End While

        Return Totalsent 'return # of bytes sent
    End Function

    Function ReceiveBytes(ByVal Port As Integer) As Byte()
    
        Dim Listener As New TcpListener(IPAddress.Any, Port)
        Listener.Start(100)

        Dim client As TcpClient = Listener.AcceptTcpClient()
        Dim stream As NetworkStream = client.GetStream()

        Listener.Stop()

        Dim receiveLength(3) As Byte

        Dim totalRead As Integer = 0
        Dim currentRead As Integer = 0

        While totalRead < receiveLength.Length
            currentRead = stream.Read(receiveLength, totalRead, receiveLength.Length - totalRead)
            totalRead += currentRead
        End While

        totalRead = 0
        currentRead = 0
        Dim receiveData(BitConverter.ToInt32(receiveLength, 0) - 1) As Byte 'critical for array size 

        While totalRead < receiveData.Length
            currentRead = stream.Read(receiveData, totalRead, receiveData.Length - totalRead)
            totalRead += currentRead
        End While

        stream.Close()
        client.Close()


        Return receiveData
    End Function

    Function FileToBytes(Path As String) As Byte()

        Dim fullPath As String = Path '& "\" & FileName

        Dim Fs As New FileStream(fullPath, FileMode.Open, FileAccess.Read)

        Dim output(Fs.Length - 1) As Byte

        Fs.Read(output, 0, Fs.Length)
        Fs.Close()

        Return output
    End Function

    Function BytesToFile(ByteArr As Byte(), Path As String) As Boolean

        Dim fullPath As String = Path '& "\" & FileName

        Dim Fs As New FileStream(Path, FileMode.Create, FileAccess.Write)
        Fs.Write(ByteArr, 0, ByteArr.Length)
        Fs.Close()

        Return True
    End Function

    Function StringToBytes(Input As String) As Byte()
        Dim byteArr() As Byte = Encoding.ASCII.GetBytes(Input)
        Return byteArr
    End Function

    Function BytesToString(Input As Byte()) As String
        Dim output As String = Encoding.ASCII.GetString(Input)
        Return output
    End Function


End Class

Public Class TCP_Function2

    Const bufferSize As Integer = 1024

    Public Function Server_Connect(ByVal Port As Integer) As Object()

        Dim Listener As New TcpListener(IPAddress.Any, Port)
        Listener.Start(100)

        Dim client As TcpClient = Listener.AcceptTcpClient()
        Dim stream As NetworkStream = client.GetStream()

        Listener.Stop()

        Dim output As Object() = {client, stream}
        Return output
    End Function

    Public Sub Server_Disconnect(ByRef client As TcpClient, ByRef stream As NetworkStream)
        stream.Close()
        client.Close()
    End Sub

    Public Function Receive_Bytes(ByVal stream As NetworkStream) As Byte()
        Dim receiveLength(3) As Byte

        Dim totalRead As Integer = 0
        Dim currentRead As Integer = 0

        While totalRead < receiveLength.Length
            currentRead = stream.Read(receiveLength, totalRead, receiveLength.Length - totalRead)
            totalRead += currentRead
        End While

        totalRead = 0
        currentRead = 0
        Dim receiveData(BitConverter.ToInt32(receiveLength, 0) - 1) As Byte 'critical for array size 

        While totalRead < receiveData.Length
            currentRead = stream.Read(receiveData, totalRead, receiveData.Length - totalRead)
            totalRead += currentRead
        End While

        Return receiveData
    End Function

    Public Function Client_Connect(ByVal IP As String, ByVal Port As Integer) As Object
        Dim TCP As New TcpClient
        TCP.Connect(IP, Port)
        Dim stream As NetworkStream = TCP.GetStream()
        Return stream
    End Function

    Public Function Send_Bytes(ByVal SendData As Byte(), ByVal Stream As NetworkStream) As Integer
        Dim sendLength As Byte() = BitConverter.GetBytes(SendData.Length)

        Stream.Write(sendLength, 0, sendLength.Length)

        Dim Totalsent As Integer = 0
        Dim Currentsent As Integer = bufferSize

        While (Totalsent < SendData.Length)

            If (SendData.Length - Totalsent < bufferSize) Then
                Currentsent = SendData.Length - Totalsent
            End If

            Stream.Write(SendData, Totalsent, Currentsent)
            Totalsent += Currentsent
        End While

        Return Totalsent 'return # of bytes sent
    End Function

End Class