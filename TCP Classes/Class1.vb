Imports System.Net
Imports System.Net.Sockets
Imports System.Text
Imports System.IO
Imports System.Xml.Serialization
Imports System.Threading
Imports System.Security.Cryptography

Public Module MainModule



    Public Class ServerConnection
        Inherits TCP_Functions

        Private _client As TcpClient = Nothing

        Public Sub New(ByVal port As Integer)
            Listener_Start(port)
            Server_Connect_Async()
        End Sub
        Public Sub onConnect(ByRef Client As TcpClient, ByVal Asynch As Boolean) Handles MyBase.Connected_Server
            Listener_Stop()
            _client = Client
            'likely want to raise event here
        End Sub

        Public Function Send(ByVal data As Byte()) As Integer
            Return Send_Bytes(data, _client.GetStream())
        End Function
        Public Sub Send_Async(ByVal data As Byte())
            Send_Bytes_Async(data, _client.GetStream)
        End Sub

        Public Function Receive() As Byte()
            Return Receive_Bytes(_client.GetStream())
        End Function
        Public Sub Recieve_Asynch()
            Receive_Bytes_Async(_client.GetStream())
        End Sub

    End Class
    Public Class ClientConnection
        Inherits TCP_Functions

        Private _client As TcpClient = Nothing

        Public Sub New(ByVal IP As String, ByVal port As Integer)
            _client = Client_Connect(IP, port)
        End Sub

        Public Function Send(ByVal data As Byte()) As Integer
            Return Send_Bytes(data, _client.GetStream())
        End Function
        Public Sub Send_Async(ByVal data As Byte())
            Send_Bytes_Async(data, _client.GetStream)
        End Sub

        Public Function Receive() As Byte()
            Return Receive_Bytes(_client.GetStream())
        End Function
        Public Sub Recieve_Asynch()
            Receive_Bytes_Async(_client.GetStream())
        End Sub

    End Class

    Public Class TCP_Functions

        Private Const SendKey As String = "jhjkkjhewrt"
        Private Const CryptKey As String = "45gwwjnr54"

        Public Event Connected_Server(ByRef Client As TcpClient, ByVal Asynch As Boolean)
        Public Event Connected_Client(ByRef Client As TcpClient, ByVal Asynch As Boolean)
        Public Event Disconnecting_Server(ByRef Client As TcpClient, ByRef Stream As NetworkStream)
        Public Event DataReceived(ByRef stream As NetworkStream, ByVal ReceiveData As Byte(), ByVal Asynch As Boolean)
        Public Event DataSent(ByRef stream As NetworkStream, ByVal BytesSent As Integer, ByVal Asynch As Boolean)

        Private Listener As TcpListener
        Private Listening As Boolean = False

        Const bufferSize As Integer = 1024

        Private Const sendtimeout As Integer = 1000 * 5
        Private Const receiveTimeout As Integer = 1000 * 5
        Private Const connectTimeout As Integer = 1000 * 1

        Public Sub Listener_Start(ByVal Port As Integer)
            If Not Listening Then
                Listening = True
                Listener = New TcpListener(IPAddress.Any, Port)
                Listener.Start(50)
            Else
                Listener_Stop()
                Listener_Start(Port)
            End If
        End Sub
        Public Sub Listener_Stop()
            If Listening = True Then
                Listening = False
                Listener.Stop()
            End If
        End Sub
        Public ReadOnly Property ListenState() As Boolean
            Get
                Return Listening
            End Get
        End Property


        Public Function Server_Connect() As Object()

            Dim client As TcpClient = Listener.AcceptTcpClient()
            Dim stream As NetworkStream = client.GetStream()

            Dim output As Object() = {client, stream}
            RaiseEvent Connected_Server(client, False)
            Return output
        End Function
        Public Sub Server_Connect_Async()
            If Listening Then
                Listener.BeginAcceptTcpClient(New AsyncCallback(AddressOf OnAccept), Listener)
            End If
        End Sub
        Private Sub OnAccept(ByVal ar As IAsyncResult)
            Dim Listener As TcpListener = CType(ar.AsyncState, TcpListener)

            If Listening Then
                Dim client As TcpClient = Listener.EndAcceptTcpClient(ar)
                Dim stream As NetworkStream = client.GetStream()
                stream.ReadTimeout = receiveTimeout
                RaiseEvent Connected_Server(client, True)
            End If
        End Sub
        Public Sub Server_Disconnect(ByRef client As TcpClient, ByRef stream As NetworkStream)
            Try
                RaiseEvent Disconnecting_Server(client, stream)
                stream.Close()
                client.Close()
            Catch ex As Exception
            End Try
        End Sub


        Public Function Client_Connect(ByVal IP As String, ByVal Port As Integer) As TcpClient
            Dim TCP As New TcpClient
            Dim result As IAsyncResult = TCP.BeginConnect(IP, Port, Nothing, Nothing)
            Dim connected As Boolean = result.AsyncWaitHandle.WaitOne(connectTimeout)

            If connected = False Then
                Throw New TimeoutException("Connect Timed out")
            End If

            TCP.EndConnect(result)

            RaiseEvent Connected_Client(TCP, False)
            Return TCP
        End Function
        Public Sub Client_Connect_Async(ByVal IP As String, ByVal Port As Integer)
            Dim client As New TcpClient
            Dim result As IAsyncResult = client.BeginConnect(IP, Port, New AsyncCallback(AddressOf OnConnect), client)
        End Sub
        Private Sub OnConnect(ByVal ar As IAsyncResult)
            Dim client As TcpClient = CType(ar.AsyncState, TcpClient)
            client.EndConnect(ar)
            RaiseEvent Connected_Client(client, True)
        End Sub


        Private Class AsyncParam
            Private _Buffer As Byte()
            Private _Stream As NetworkStream

            Public Sub New(ByRef Stream As NetworkStream, ByVal Buffer As Byte())
                _Buffer = Buffer
                _Stream = Stream

            End Sub
            Public Property Buffer As Byte()
                Get
                    Return _Buffer
                End Get
                Set(value As Byte())
                    _Buffer = value
                End Set
            End Property

            Public Property Stream As NetworkStream
                Get
                    Return _Stream
                End Get
                Set(value As NetworkStream)
                    _Stream = value
                End Set
            End Property

        End Class

        Public Function Receive_Bytes(ByRef stream As NetworkStream) As Byte()
            stream.ReadTimeout = receiveTimeout
            '--------------------------- Get Key ---------------------------------------
            Dim receivekey(encrypt(SendKey).Length - 1) As Byte

            Dim totalRead As Integer = 0
            Dim currentRead As Integer = 0

            While totalRead < receivekey.Length
                currentRead = stream.Read(receivekey, totalRead, receivekey.Length - totalRead)
                totalRead += currentRead
            End While

            Dim key As String = decrypt(receivekey)
            If key <> SendKey Then
                Throw New KeyNotFoundException("Incorrect receive key")
            End If

            '----------------------- Get message length --------------------------

            Dim receiveLength(3) As Byte

            totalRead = 0
            currentRead = 0

            While totalRead < receiveLength.Length
                currentRead = stream.Read(receiveLength, totalRead, receiveLength.Length - totalRead)
                totalRead += currentRead
            End While

            Dim messageSize As Integer = BitConverter.ToInt32(receiveLength, 0)

            '----------------------- Get data --------------------------

            totalRead = 0
            currentRead = 0

            Dim receiveData(messageSize - 1) As Byte 'critical for array size 

            While totalRead < receiveData.Length
                currentRead = stream.Read(receiveData, totalRead, receiveData.Length - totalRead)
                totalRead += currentRead
            End While


            RaiseEvent DataReceived(stream, receiveData, False)
            Return receiveData
        End Function
        Public Sub Receive_Bytes_Async(ByRef stream As NetworkStream)
            Dim length As Integer = encrypt(SendKey).Length - 1
            Dim receivekey(length) As Byte
            Dim param As New AsyncParam(stream, receivekey)

            Dim result As IAsyncResult = stream.BeginRead(receivekey, 0, receivekey.Length, New AsyncCallback(AddressOf OnReceive), param)
        End Sub
        Private Sub OnReceive(ByVal ar As IAsyncResult)
            Dim param As AsyncParam = CType(ar.AsyncState, AsyncParam)
            param.Stream.EndRead(ar)
            param.Stream.ReadTimeout = receiveTimeout

            Dim key As String = decrypt(param.Buffer)
            If key <> SendKey Then
                Throw New KeyNotFoundException("Incorrect receive key")
            End If

            Dim receiveLength(3) As Byte

            Dim totalRead As Integer = 0
            Dim currentRead As Integer = 0

            While totalRead < receiveLength.Length
                currentRead = param.Stream.Read(receiveLength, totalRead, receiveLength.Length - totalRead)
                totalRead += currentRead
            End While

            totalRead = 0
            currentRead = 0

            Dim messageSize As Integer = BitConverter.ToInt32(receiveLength, 0)
            Dim receiveData(messageSize - 1) As Byte 'critical for array size 

            While totalRead < receiveData.Length
                currentRead = param.Stream.Read(receiveData, totalRead, receiveData.Length - totalRead)
                totalRead += currentRead
            End While

            param.Buffer = receiveData
            RaiseEvent DataReceived(param.Stream, param.Buffer, True)
        End Sub

        Public Function Send_Bytes(ByVal SendData As Byte(), ByVal Stream As NetworkStream) As Integer
            Stream.WriteTimeout = sendtimeout

            Dim tcpkey As Byte() = encrypt(SendKey)
            Stream.Write(tcpkey, 0, tcpkey.Length)


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
        Public Sub Send_Bytes_Async(ByVal SendData As Byte(), ByVal Stream As NetworkStream)
            Dim tcpkey As Byte() = encrypt(SendKey)
            Dim param As New AsyncParam(Stream, SendData)
            Dim result As IAsyncResult = Stream.BeginWrite(tcpkey, 0, tcpkey.Length, New AsyncCallback(AddressOf OnSend), param)
        End Sub
        Private Sub OnSend(ByVal ar As IAsyncResult)
            Dim param As AsyncParam = CType(ar.AsyncState, AsyncParam)
            param.Stream.WriteTimeout = sendtimeout

            Dim sendLength As Byte() = BitConverter.GetBytes(param.Buffer.Length)

            param.Stream.Write(sendLength, 0, sendLength.Length)

            Dim Totalsent As Integer = 0
            Dim Currentsent As Integer = bufferSize

            While (Totalsent < param.Buffer.Length)

                If (param.Buffer.Length - Totalsent < bufferSize) Then
                    Currentsent = param.Buffer.Length - Totalsent
                End If

                param.Stream.Write(param.Buffer, Totalsent, Currentsent)
                Totalsent += Currentsent
            End While

            RaiseEvent DataSent(param.Stream, Totalsent, True)
        End Sub



        Public Function GetIP(ByVal client As TcpClient) As String
            Dim IPadd As String = Nothing

            Dim ipend As Net.IPEndPoint = client.Client.RemoteEndPoint
            If Not ipend Is Nothing Then
                IPadd = ipend.Address.ToString
            End If

            Return IPadd
        End Function

        Private Function encrypt(ByVal input As Object) As Byte()

            Dim _MemoryStream As New MemoryStream()
            Dim _BinaryFormatter As New System.Runtime.Serialization.Formatters.Binary.BinaryFormatter()
            _BinaryFormatter.Serialize(_MemoryStream, input)
            Dim output As Byte() = _MemoryStream.ToArray()

            Using encryptor As Aes = Aes.Create()
                Dim pdb As New Rfc2898DeriveBytes(CryptKey, New Byte() {&H49, &H76, &H61, &H6E, &H20, &H4D, _
                 &H65, &H64, &H76, &H65, &H64, &H65, _
                 &H76})
                encryptor.Key = pdb.GetBytes(32)
                encryptor.IV = pdb.GetBytes(16)
                Using ms As New MemoryStream()
                    Using cs As New CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write)
                        cs.Write(output, 0, output.Length)
                        cs.Close()
                        output = ms.ToArray
                    End Using
                End Using
            End Using

            Return output

        End Function
        Private Function decrypt(ByVal input As Byte()) As Object

            Using encryptor As Aes = Aes.Create()
                Dim pdb As New Rfc2898DeriveBytes(CryptKey, New Byte() {&H49, &H76, &H61, &H6E, &H20, &H4D, _
                 &H65, &H64, &H76, &H65, &H64, &H65, _
                 &H76})
                encryptor.Key = pdb.GetBytes(32)
                encryptor.IV = pdb.GetBytes(16)
                Using ms As New MemoryStream()
                    Using cs As New CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write)
                        cs.Write(input, 0, input.Length)
                        cs.Close()
                    End Using
                    input = ms.ToArray()
                End Using
            End Using

            Dim _MemoryStream As New MemoryStream()
            _MemoryStream.Write(input, 0, input.Length)
            Dim _BinaryFormatter As New System.Runtime.Serialization.Formatters.Binary.BinaryFormatter()
            _MemoryStream.Seek(0, SeekOrigin.Begin)
            Dim output As Object = _BinaryFormatter.Deserialize(_MemoryStream)

            Return output
        End Function

    End Class

End Module
