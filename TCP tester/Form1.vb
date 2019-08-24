Imports System.Net.Sockets
Imports System.Net
Imports System.Text
Imports System.IO
Imports System.Threading


Public Class Form1
    Dim counter As Integer = 0
    'Dim serverStream As NetworkStream
    'Dim serverClient As TcpClient

    Dim clientStream As NetworkStream

    Dim TCP As New TCP_Function2

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

    Private Sub CheckBox1_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox1.CheckedChanged
        If CheckBox1.Checked = True Then
            Dim T1 As New Thread(AddressOf Receive)
            T1.Start()
        End If
    End Sub

    Private Sub Receive()

        Dim rets As Object() = TCP.Server_Connect(CInt(TextBox_ListenPort.Text))
        Dim serverClient As TcpClient = rets(0)
        Dim serverStream As NetworkStream = rets(1)

        Dim T1 As New Thread(AddressOf Receive2)
        T1.Start(serverStream)

        counter += 1

        If CheckBox1.Checked = True And counter <> 2 Then
            Call Receive()
        End If

    End Sub

    Private Sub Receive2(ByVal serverStream As NetworkStream)
        While CheckBox1.Checked = True
            Dim bytearr As Byte() = TCP.Receive_Bytes(ServerStream)
            Dim retstr As String = TCP.BytesToString(bytearr)
            SetText(retstr)
        End While
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        clientStream = TCP.Client_Connect(TextBox_RemoteIP.Text, CInt(TextBox_RemotePort.Text))
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        TCP.Send_Bytes(TCP.StringToBytes(TextBox4.Text), clientStream)
    End Sub
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

    Function StringToBytes(Input As String) As Byte()
        Dim byteArr() As Byte = Encoding.ASCII.GetBytes(Input)
        Return byteArr
    End Function

    Function BytesToString(Input As Byte()) As String
        Dim output As String = Encoding.ASCII.GetString(Input)
        Return output
    End Function

End Class