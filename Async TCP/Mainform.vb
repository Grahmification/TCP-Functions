Imports System.Net
Imports System.Net.Sockets
Imports System.Threading
Imports System.Text
Imports System.Text.ASCIIEncoding

Public Class Mainform

    Dim WithEvents TCP As New TCP_Functions
    Dim Port As Integer = 8000
    Dim IP As String = "127.0.0.1"

    Dim clientstream As NetworkStream

    Private Sub Button_ClientConnect_Click(sender As Object, e As EventArgs) Handles Button_ClientConnect.Click
        clientstream = TCP.Client_Connect(IP, Port)
    End Sub

    Private Sub btn_send_Click(sender As Object, e As EventArgs) Handles btn_send.Click
        TCP.Send_Bytes(Encoding.ASCII.GetBytes(TextBox_send.Text), clientstream)
    End Sub

    Private Sub Btn_Serverconnect_Click(sender As Object, e As EventArgs) Handles Btn_Serverconnect.Click
        TCP.Server_Connect(Port)
    End Sub
    Delegate Sub Connected_del(ByRef Client As TcpClient, ByRef Stream As NetworkStream)
    Private Sub Connected(ByRef Client As TcpClient, ByRef Stream As NetworkStream) Handles TCP.ClientConnected
        If Me.InvokeRequired Then
            Invoke(New Connected_del(AddressOf Connected), Client, Stream)
        Else
            While True
                Dim data As String = Encoding.ASCII.GetString(TCP.Receive_Bytes(Stream))
                RichTextBox1.AppendText(data)
            End While
        End If

        
    End Sub
End Class

Public Class StateObject
    Public workSocket As Socket = Nothing
    Public Const BufferSize As Integer = 256
    Public buffer(BufferSize) As Byte
    Public sb As New StringBuilder
End Class
Public Class AsynchClient1

    Private Const port As Integer = 11000

    Private Shared connectDone As New ManualResetEvent(False)
    Private Shared sendDone As New ManualResetEvent(False)
    Private Shared receiveDone As New ManualResetEvent(False)

    Private Shared response As String = String.Empty

    Public Shared Sub Main()
        Dim ipHostInfo As IPHostEntry = Dns.Resolve(Dns.GetHostName())
        Dim ip As IPAddress = ipHostInfo.AddressList(0)
        Dim remoteEP As New IPEndPoint(ip, port)

        Dim client As New Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
        client.BeginConnect(remoteEP, New AsyncCallback(AddressOf ConnectCallback), client)
        connectDone.WaitOne()


        Send(client, "This is a test<EOF>")
        sendDone.WaitOne()

        Receive(client)
        receiveDone.WaitOne()


        Console.WriteLine("Response received : {0}", response)

        client.Shutdown(SocketShutdown.Both)
        client.Close()

    End Sub
    Private Shared Sub ConnectCallback(ByVal ar As IAsyncResult)
        Try
            Dim client As Socket = CType(ar.AsyncState, Socket)
            client.EndConnect(ar)
            Console.WriteLine("Socket connected to {0}", client.RemoteEndPoint.ToString())
            connectDone.Set()
        Catch
        End Try

    End Sub

    Private Shared Sub Receive(ByVal client As Socket)

        Dim state As New StateObject
        state.workSocket = client
        client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, New AsyncCallback(AddressOf ReceiveCallback), state)

    End Sub
    Private Shared Sub ReceiveCallback(ByVal ar As IAsyncResult)

        Dim state As StateObject = CType(ar.AsyncState, StateObject)
        Dim client As Socket = state.workSocket

        Dim bytesRead As Integer = client.EndReceive(ar)

        If bytesRead > 0 Then

            state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead))

            client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, New AsyncCallback(AddressOf ReceiveCallback), state)
        Else

            If state.sb.Length > 1 Then
                response = state.sb.ToString()
            End If

            receiveDone.Set()
        End If
    End Sub

    Private Shared Sub Send(ByVal client As Socket, ByVal data As String)

        Dim byteData As Byte() = Encoding.ASCII.GetBytes(data)
        client.BeginSend(byteData, 0, byteData.Length, 0, New AsyncCallback(AddressOf SendCallback), client)

    End Sub 'Send
    Private Shared Sub SendCallback(ByVal ar As IAsyncResult)

        Dim client As Socket = CType(ar.AsyncState, Socket)
        Dim bytesSent As Integer = client.EndSend(ar)
        Console.WriteLine("Sent {0} bytes to server.", bytesSent)
        sendDone.Set()
    End Sub

End Class
Public Class frmClient
    Dim client As Socket
    Dim host As String = "127.0.0.1"
    Dim port As Integer = "6969"
    Private Sub Connect()
        client = New Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
        Dim IP As IPAddress = IPAddress.Parse(host)
        Dim xIpEndPoint As IPEndPoint = New IPEndPoint(IP, port)
        client.BeginConnect(xIpEndPoint, New AsyncCallback(AddressOf OnConnect), Nothing)

    End Sub
    Private Sub Send(ByVal Message As String)
        Dim bytes As Byte() = ASCII.GetBytes(Message)
        client.BeginSend(bytes, 0, bytes.Length, SocketFlags.None, New AsyncCallback(AddressOf OnSend), client)
    End Sub
    Private Sub OnConnect(ByVal ar As IAsyncResult)
        client.EndConnect(ar)
        MessageBox.Show("Connected")
    End Sub
    Private Sub OnSend(ByVal ar As IAsyncResult)
        client.EndSend(ar)
    End Sub

End Class
Public Class frmServer
    Dim server As Socket
    Dim client As Socket
    Dim bytes As Byte() = New Byte(1023) {}
    Private Sub RecieveConnect()
        server = New Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
        Dim xEndpoint As IPEndPoint = New IPEndPoint(IPAddress.Any, 6969)
        server.Bind(xEndpoint)
        server.Listen(2)
        server.BeginAccept(New AsyncCallback(AddressOf OnAccept), vbNull)
    End Sub
    Private Sub OnAccept(ByVal ar As IAsyncResult)
        client = server.EndAccept(ar)
        client.BeginReceive(bytes, 0, bytes.Length, SocketFlags.None, New AsyncCallback(AddressOf OnRecieve), client)
    End Sub
    Private Sub OnRecieve(ByVal ar As IAsyncResult)
        client = ar.AsyncState
        client.EndReceive(ar)
        client.BeginReceive(bytes, 0, bytes.Length, SocketFlags.None, New AsyncCallback(AddressOf OnRecieve), client)
        Dim message As String = System.Text.ASCIIEncoding.ASCII.GetString(bytes)
        MessageBox.Show(message)
    End Sub
End Class


Public Class ReceiveData
    Public Buffer As Byte()
    Public Stream As NetworkStream
End Class
Public Class TCP_Functions

    Public Event ClientConnected(ByRef Client As TcpClient, ByRef Stream As NetworkStream)
    Private Listener As TcpListener
    Const bufferSize As Integer = 1024
    Public Shared DoneReceive As New ManualResetEvent(False)

    Public Sub Server_Connect(ByVal port As Integer)
        Listener = New TcpListener(IPAddress.Any, port)
        Listener.Start(50)
        Listener.BeginAcceptTcpClient(New AsyncCallback(AddressOf OnAccept), vbNull)
    End Sub
    Private Sub OnAccept(ByVal ar As IAsyncResult)
        Dim client As TcpClient = Listener.EndAcceptTcpClient(ar)
        Dim stream As NetworkStream = client.GetStream()
        RaiseEvent ClientConnected(client, stream)
        Listener.Stop()
    End Sub
    Public Sub Server_Disconnect(ByRef client As TcpClient, ByRef stream As NetworkStream)
        Try
            stream.Close()
            client.Close()
            Listener.Stop()
        Catch ex As Exception
        End Try
    End Sub

    Public Function Receive_Bytes_Old(ByRef stream As NetworkStream) As Byte()
        Dim receiveLength(3) As Byte

        Dim totalRead As Integer = 0
        Dim currentRead As Integer = 0

        While totalRead < receiveLength.Length
            currentRead = stream.Read(receiveLength, totalRead, receiveLength.Length - totalRead)
            totalRead += currentRead
        End While

        totalRead = 0
        currentRead = 0
        Dim messageSize As Integer

        Try
            messageSize = BitConverter.ToInt32(receiveLength, 0)
        Catch ex As Exception
            Return {0, 0}
        End Try

        Dim receiveData(messageSize - 1) As Byte 'critical for array size 

        While totalRead < receiveData.Length
            currentRead = stream.Read(receiveData, totalRead, receiveData.Length - totalRead)
            totalRead += currentRead
        End While

        Return receiveData
    End Function

    Public Function Receive_Bytes(ByRef stream As NetworkStream) As Byte()
        Dim param As New ReceiveData
        Dim receiveLength(3) As Byte
        param.Buffer = receiveLength
        param.Stream = stream

        DoneReceive.Reset()

        Dim result As IAsyncResult
        result = stream.BeginRead(receiveLength, 0, receiveLength.Length, New AsyncCallback(AddressOf LengthRecieved), param)
        DoneReceive.WaitOne()
        Dim output As ReceiveData = result.AsyncState
        Return output.Buffer
    End Function
    Private Sub LengthRecieved(ByVal ar As IAsyncResult)
        Dim param As ReceiveData = CType(ar.AsyncState, ReceiveData)
        Dim stream As NetworkStream = param.Stream
        Dim receiveLength As Byte() = param.Buffer
        stream.EndRead(ar)

        Dim messageSize As Integer
        Try
            messageSize = BitConverter.ToInt32(receiveLength, 0)
        Catch ex As Exception
            Return
        End Try

        Dim receivedata(messageSize - 1) As Byte
        param.Buffer = receivedata

        stream.BeginRead(receivedata, 0, messageSize, New AsyncCallback(AddressOf DataReceived), param)
    End Sub
    Private Sub DataReceived(ByVal ar As IAsyncResult)
        Dim param As ReceiveData = CType(ar.AsyncState, ReceiveData)
        Dim stream As NetworkStream = param.Stream
        Dim receiveData As Byte() = param.Buffer
        stream.EndRead(ar)
        DoneReceive.Set()
    End Sub

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

