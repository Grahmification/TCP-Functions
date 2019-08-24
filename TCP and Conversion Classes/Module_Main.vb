Imports System.Net
Imports System.Net.Sockets
Imports System.Text
Imports System.IO
Imports System.Xml.Serialization
Imports System.Threading
Imports System.Security.Cryptography
Module Module_Main

#Region "v1"
    Public Class TCP_Functions_BEST

        Private Const SendKey As String = "jhjkkjhewrt"
        Private Const CryptKey As String = "45gwwjnr54"

        Public Event Connected_Server(ByRef Client As TcpClient, ByRef Stream As NetworkStream, ByVal Asynch As Boolean)
        Public Event Connected_Client(ByRef Stream As NetworkStream, ByVal Asynch As Boolean)
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
            RaiseEvent Connected_Server(client, stream, False)
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
                RaiseEvent Connected_Server(client, stream, True)
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


        Public Function Client_Connect(ByVal IP As String, ByVal Port As Integer) As NetworkStream
            Dim TCP As New TcpClient
            Dim result As IAsyncResult = TCP.BeginConnect(IP, Port, Nothing, Nothing)
            Dim connected As Boolean = result.AsyncWaitHandle.WaitOne(connectTimeout)

            If connected = False Then
                Throw New TimeoutException("Connect Timed out")
            End If

            TCP.EndConnect(result)

            Dim stream As NetworkStream = TCP.GetStream()
            RaiseEvent Connected_Client(stream, False)
            Return stream
        End Function
        Public Sub Client_Connect_Async(ByVal IP As String, ByVal Port As Integer)
            Dim client As New TcpClient
            Dim result As IAsyncResult = client.BeginConnect(IP, Port, New AsyncCallback(AddressOf OnConnect), client)
        End Sub
        Private Sub OnConnect(ByVal ar As IAsyncResult)
            Dim client As TcpClient = CType(ar.AsyncState, TcpClient)
            client.EndConnect(ar)
            Dim stream As NetworkStream = client.GetStream
            RaiseEvent Connected_Client(stream, True)
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

#End Region

    Public Class V1

        Shared Connections As New Multiple_connections_class
        Public Class Multiple_connections_class

            Private Class Connection_Class
                Private _stream As NetworkStream
                Private _client As TcpClient
                Private _IP As String = Nothing
                Private _ServerConnection As Boolean

                Public Sub New(ByVal client As TcpClient, ByRef stream As NetworkStream, ByVal ServerConnection As Boolean)
                    _stream = stream
                    _client = client
                    _ServerConnection = ServerConnection

                    Dim IPadd As String = Nothing
                    Dim ipend As Net.IPEndPoint = client.Client.RemoteEndPoint
                    If Not ipend Is Nothing Then
                        IPadd = ipend.Address.ToString
                    End If
                    _IP = IPadd
                End Sub
                Public ReadOnly Property Stream As NetworkStream
                    Get
                        Return _stream
                    End Get
                End Property
                Public ReadOnly Property Client As TcpClient
                    Get
                        Return _client
                    End Get
                End Property
                Public ReadOnly Property IP As String
                    Get
                        Return _IP
                    End Get
                End Property
                Public ReadOnly Property ServerConnection As Boolean
                    Get
                        Return _ServerConnection
                    End Get
                End Property

            End Class
            Private _connections As Dictionary(Of Integer, Connection_Class) = New Dictionary(Of Integer, Connection_Class)

            Public Function Add(ByRef client As TcpClient, ByRef stream As NetworkStream, ByVal ServerConnection As Boolean) As Integer
                Dim tmp As New Connection_Class(client, stream, ServerConnection)
                Dim index As Integer = 0
                While _connections.Keys.Contains(index)
                    index += 1
                End While
                _connections.Add(index, tmp)
                Return index
            End Function
            Public Sub Remove(ByVal index As Integer)
                _connections.Remove(index)
            End Sub
            Public Sub RemoveAll()
                _connections.Clear()
            End Sub
            Public ReadOnly Property Count As Integer
                Get
                    Return _connections.Count
                End Get
            End Property

            Public ReadOnly Property Stream(ByVal index As Integer) As NetworkStream
                Get
                    Return _connections(index).Stream
                End Get
            End Property
            Public ReadOnly Property Client(ByVal index As Integer) As TcpClient
                Get
                    Return _connections(index).Client
                End Get
            End Property
            Public ReadOnly Property IP(ByVal index As Integer) As String
                Get
                    Return _connections(index).IP
                End Get
            End Property
            Public ReadOnly Property ServerConnection(ByVal index As Integer) As Boolean
                Get
                    Return _connections(index).ServerConnection
                End Get
            End Property



        End Class
        Public Class TCP_Functions

            Private Const SendKey As String = "jhjkkjhewrt"
            Private Const CryptKey As String = "45gwwjnr54"
            Private Const Disconnectkey As String = "Disconnectuser"

            Private Const bufferSize As Integer = 1024

            Private Const sendtimeout As Integer = 1000 * 5
            Private Const receiveTimeout As Integer = 1000 * 5
            Private Const connectTimeout As Integer = 1000 * 1

            Private Listener As TcpListener
            Private Listening As Boolean = False

            Public Event Connected_Server(ByVal Index As Integer, ByVal Asynch As Boolean)
            Public Event Connected_Client(ByVal Index As Integer, ByVal Asynch As Boolean)
            Public Event Disconnecting(ByVal index As Integer, ByVal IP As String)
            Public Event DataReceived(ByVal index As Integer, ByVal ReceiveData As Byte(), ByVal Asynch As Boolean)
            Public Event DataSent(ByVal index As Integer, ByVal BytesSent As Integer, ByVal Asynch As Boolean)

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

            Public Function Server_Connect() As Integer
                If Listening Then
                    Dim client As TcpClient = Listener.AcceptTcpClient()
                    Dim stream As NetworkStream = client.GetStream()

                    Dim output As Integer = Connections.Add(client, stream, True)
                    RaiseEvent Connected_Server(output, False)
                    Return output
                Else
                    Return -1
                End If
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
                    Dim index As Integer = Connections.Add(client, stream, True)
                    RaiseEvent Connected_Server(index, True)
                End If
            End Sub

            Public Function Client_Connect(ByVal IP As String, ByVal Port As Integer) As Integer
                Dim TCP As New TcpClient
                Dim result As IAsyncResult = TCP.BeginConnect(IP, Port, Nothing, Nothing)
                Dim connected As Boolean = result.AsyncWaitHandle.WaitOne(connectTimeout)

                If connected = False Then
                    Throw New TimeoutException("Connect Timed out")
                End If

                TCP.EndConnect(result)

                Dim stream As NetworkStream = TCP.GetStream()
                Dim index As Integer = Connections.Add(TCP, stream, False)
                RaiseEvent Connected_Client(index, False)
                Return index
            End Function
            Public Sub Client_Connect_Async(ByVal IP As String, ByVal Port As Integer)
                Dim client As New TcpClient
                Dim result As IAsyncResult = client.BeginConnect(IP, Port, New AsyncCallback(AddressOf OnConnect), client)
            End Sub
            Private Sub OnConnect(ByVal ar As IAsyncResult)
                Dim client As TcpClient = CType(ar.AsyncState, TcpClient)
                client.EndConnect(ar)
                Dim stream As NetworkStream = client.GetStream
                Dim index As Integer = Connections.Add(client, stream, False)
                RaiseEvent Connected_Client(index, True)
            End Sub

            Public Sub Disconnect(ByVal index As Integer)
                Try
                    RaiseEvent Disconnecting(index, Connections.IP(index))

                    If Connections.Client(index).Connected Then
                        Connections.Stream(index).Close()
                        Connections.Client(index).Close()
                    End If
                Catch ex As Exception
                Finally
                    Connections.Remove(index)
                End Try
            End Sub

            Private Class AsyncParam
                Private _Buffer As Byte()
                Private _index As Integer

                Public Sub New(ByVal index As Integer, ByVal Buffer As Byte())
                    _Buffer = Buffer
                    _index = index

                End Sub
                Public Property Buffer As Byte()
                    Get
                        Return _Buffer
                    End Get
                    Set(value As Byte())
                        _Buffer = value
                    End Set
                End Property
                Public Property Index As Integer
                    Get
                        Return _index
                    End Get
                    Set(value As Integer)
                        _index = value
                    End Set
                End Property

            End Class

            Public Function Receive_Bytes(ByVal index As Integer) As Byte()
                Dim stream As NetworkStream = Connections.Stream(index)
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

                RaiseEvent DataReceived(index, receiveData, False)
                Return receiveData
            End Function
            Public Sub Receive_Bytes_Async(ByVal index As Integer)
                Dim stream As NetworkStream = Connections.Stream(index)
                Dim length As Integer = encrypt(SendKey).Length - 1
                Dim receivekey(length) As Byte
                Dim param As New AsyncParam(index, receivekey)

                Dim result As IAsyncResult = stream.BeginRead(receivekey, 0, receivekey.Length, New AsyncCallback(AddressOf OnReceive), param)
            End Sub
            Private Sub OnReceive(ByVal ar As IAsyncResult)
                Dim param As AsyncParam = CType(ar.AsyncState, AsyncParam)
                Dim stream As NetworkStream = Connections.Stream(param.Index)
                stream.EndRead(ar)
                stream.ReadTimeout = receiveTimeout

                Dim key As String = decrypt(param.Buffer)
                If key <> SendKey Then
                    Throw New KeyNotFoundException("Incorrect receive key")
                End If

                Dim receiveLength(3) As Byte

                Dim totalRead As Integer = 0
                Dim currentRead As Integer = 0

                While totalRead < receiveLength.Length
                    currentRead = stream.Read(receiveLength, totalRead, receiveLength.Length - totalRead)
                    totalRead += currentRead
                End While

                totalRead = 0
                currentRead = 0

                Dim messageSize As Integer = BitConverter.ToInt32(receiveLength, 0)
                Dim receiveData(messageSize - 1) As Byte 'critical for array size 

                While totalRead < receiveData.Length
                    currentRead = stream.Read(receiveData, totalRead, receiveData.Length - totalRead)
                    totalRead += currentRead
                End While

                param.Buffer = receiveData
                RaiseEvent DataReceived(param.Index, param.Buffer, True)
            End Sub

            Public Function Send_Bytes(ByVal SendData As Byte(), ByVal index As Integer) As Integer
                Dim stream As NetworkStream = Connections.Stream(index)
                stream.WriteTimeout = sendtimeout

                Dim tcpkey As Byte() = encrypt(SendKey)
                stream.Write(tcpkey, 0, tcpkey.Length)


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

                RaiseEvent DataSent(index, Totalsent, False)
                Return Totalsent 'return # of bytes sent
            End Function
            Public Sub Send_Bytes_Async(ByVal SendData As Byte(), ByVal index As Integer)
                Dim stream As NetworkStream = Connections.Stream(index)
                Dim tcpkey As Byte() = encrypt(SendKey)
                Dim param As New AsyncParam(index, SendData)
                Dim result As IAsyncResult = stream.BeginWrite(tcpkey, 0, tcpkey.Length, New AsyncCallback(AddressOf OnSend), param)
            End Sub
            Private Sub OnSend(ByVal ar As IAsyncResult)
                Dim param As AsyncParam = CType(ar.AsyncState, AsyncParam)
                Dim stream As NetworkStream = Connections.Stream(param.Index)
                stream.WriteTimeout = sendtimeout

                Dim sendLength As Byte() = BitConverter.GetBytes(param.Buffer.Length)

                stream.Write(sendLength, 0, sendLength.Length)

                Dim Totalsent As Integer = 0
                Dim Currentsent As Integer = bufferSize

                While (Totalsent < param.Buffer.Length)

                    If (param.Buffer.Length - Totalsent < bufferSize) Then
                        Currentsent = param.Buffer.Length - Totalsent
                    End If

                    stream.Write(param.Buffer, Totalsent, Currentsent)
                    Totalsent += Currentsent
                End While

                RaiseEvent DataSent(param.Index, Totalsent, True)
            End Sub

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

    End Class

    Public Class V2

        Shared Connections As New Multiple_connections_class
        Public Class Multiple_connections_class

            Private Class Connection_Class
                Private _client As TcpClient
                Private _IP As String = Nothing
                Private _ServerConnection As Boolean

                Public Sub New(ByVal client As TcpClient, ByVal ServerConnection As Boolean)
                    _client = client
                    _ServerConnection = ServerConnection

                    Dim IPadd As String = Nothing
                    Dim ipend As Net.IPEndPoint = client.Client.RemoteEndPoint
                    If Not ipend Is Nothing Then
                        IPadd = ipend.Address.ToString
                    End If
                    _IP = IPadd
                End Sub
                Public ReadOnly Property Client As TcpClient
                    Get
                        Return _client
                    End Get
                End Property
                Public ReadOnly Property IP As String
                    Get
                        Return _IP
                    End Get
                End Property
                Public ReadOnly Property ServerConnection As Boolean
                    Get
                        Return _ServerConnection
                    End Get
                End Property

            End Class
            Private _connections As Dictionary(Of Integer, Connection_Class) = New Dictionary(Of Integer, Connection_Class)

            Public Function Add(ByRef client As TcpClient, ByVal ServerConnection As Boolean) As Integer
                Dim tmp As New Connection_Class(client, ServerConnection)
                Dim index As Integer = 0
                While _connections.Keys.Contains(index)
                    index += 1
                End While
                _connections.Add(index, tmp)
                Return index
            End Function
            Public Sub Remove(ByVal index As Integer)
                _connections.Remove(index)
            End Sub
            Public Sub RemoveAll()
                _connections.Clear()
            End Sub
            Public ReadOnly Property Count As Integer
                Get
                    Return _connections.Count
                End Get
            End Property


            Public ReadOnly Property Client(ByVal index As Integer) As TcpClient
                Get
                    Return _connections(index).Client
                End Get
            End Property
            Public ReadOnly Property IP(ByVal index As Integer) As String
                Get
                    Return _connections(index).IP
                End Get
            End Property
            Public ReadOnly Property ServerConnection(ByVal index As Integer) As Boolean
                Get
                    Return _connections(index).ServerConnection
                End Get
            End Property



        End Class
        Public Class TCP_Functions

            Private Const SendKey As String = "jhjkkjhewrt"
            Private Const CryptKey As String = "45gwwjnr54"
            Private Const Disconnectkey As String = "Disconnectuser"

            Private Const bufferSize As Integer = 1024

            Private Const sendtimeout As Integer = 1000 * 5
            Private Const receiveTimeout As Integer = 1000 * 5
            Private Const connectTimeout As Integer = 1000 * 1

            Private Listener As TcpListener
            Private Listening As Boolean = False

            Public Event Connected_Server(ByVal Index As Integer, ByVal Asynch As Boolean)
            Public Event Connected_Client(ByVal Index As Integer, ByVal Asynch As Boolean)
            Public Event Disconnecting(ByVal index As Integer, ByVal IP As String)
            Public Event DataReceived(ByVal index As Integer, ByVal ReceiveData As Byte(), ByVal Asynch As Boolean)
            Public Event DataSent(ByVal index As Integer, ByVal BytesSent As Integer, ByVal Asynch As Boolean)
            Public Event TCPError(ByVal ex As Exception, ByVal method As Reflection.MethodInfo)

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

            Public Function Server_Connect() As Integer
                If Listening Then
                    Dim client As TcpClient = Listener.AcceptTcpClient()
                    Dim output As Integer = Connections.Add(client, True)
                    RaiseEvent Connected_Server(output, False)
                    Return output
                Else
                    Return -1
                End If
            End Function
            Public Sub Server_Connect_Async()
                If Listening Then
                    Listener.BeginAcceptTcpClient(New AsyncCallback(AddressOf OnAccept), Listener)
                End If
            End Sub
            Private Sub OnAccept(ByVal ar As IAsyncResult)
                Try
                    Dim Listener As TcpListener = CType(ar.AsyncState, TcpListener)

                    If Listening Then
                        Dim client As TcpClient = Listener.EndAcceptTcpClient(ar)
                        Dim index As Integer = Connections.Add(client, True)
                        RaiseEvent Connected_Server(index, True)
                    End If
                Catch ex As Exception
                    RaiseEvent TCPError(ex, Reflection.MethodInfo.GetCurrentMethod)
                End Try
            End Sub

            Public Function Client_Connect(ByVal IP As String, ByVal Port As Integer) As Integer
                Dim TCP As New TcpClient
                Dim result As IAsyncResult = TCP.BeginConnect(IP, Port, Nothing, Nothing)
                Dim connected As Boolean = result.AsyncWaitHandle.WaitOne(connectTimeout)

                If connected = False Then
                    Throw New TimeoutException("Connect Timed out")
                End If

                TCP.EndConnect(result)

                Dim index As Integer = Connections.Add(TCP, False)
                RaiseEvent Connected_Client(index, False)
                Return index
            End Function
            Public Sub Client_Connect_Async(ByVal IP As String, ByVal Port As Integer)
                Dim client As New TcpClient
                Dim result As IAsyncResult = client.BeginConnect(IP, Port, New AsyncCallback(AddressOf OnConnect), client)
            End Sub
            Private Sub OnConnect(ByVal ar As IAsyncResult)
                Try
                    Dim client As TcpClient = CType(ar.AsyncState, TcpClient)
                    client.EndConnect(ar)
                    Dim index As Integer = Connections.Add(client, False)
                    RaiseEvent Connected_Client(index, True)
                Catch ex As Exception
                    RaiseEvent TCPError(ex, Reflection.MethodInfo.GetCurrentMethod)
                End Try
            End Sub

            Public Sub Disconnect(ByVal index As Integer)
                Try
                    RaiseEvent Disconnecting(index, Connections.IP(index))

                    If Connections.ServerConnection(index) = True Then
                        Send_Bytes(Encoding.ASCII.GetBytes(Disconnectkey), index)
                    End If

                    If Connections.Client(index).Connected Then
                        Connections.Client(index).GetStream.Close()
                        Connections.Client(index).Close()
                    End If
                Catch ex As Exception
                Finally
                    Connections.Remove(index)
                End Try
            End Sub

            Private Class AsyncParam
                Private _Buffer As Byte()
                Private _index As Integer

                Public Sub New(ByVal index As Integer, ByVal Buffer As Byte())
                    _Buffer = Buffer
                    _index = index

                End Sub
                Public Property Buffer As Byte()
                    Get
                        Return _Buffer
                    End Get
                    Set(value As Byte())
                        _Buffer = value
                    End Set
                End Property
                Public Property Index As Integer
                    Get
                        Return _index
                    End Get
                    Set(value As Integer)
                        _index = value
                    End Set
                End Property

            End Class

            Public Function Receive_Bytes(ByVal index As Integer) As Byte()
                Dim stream As NetworkStream = Connections.Client(index).GetStream
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

                If Encoding.ASCII.GetString(receiveData) = Disconnectkey Then
                    Disconnect(index)
                    Return Nothing
                End If

                RaiseEvent DataReceived(index, receiveData, False)
                Return receiveData
            End Function
            Public Sub Receive_Bytes_Async(ByVal index As Integer)
                Dim length As Integer = encrypt(SendKey).Length - 1
                Dim receivekey(length) As Byte
                Dim param As New AsyncParam(index, receivekey)
                Dim result As IAsyncResult = Connections.Client(index).GetStream.BeginRead(receivekey, 0, receivekey.Length, New AsyncCallback(AddressOf OnReceive), param)
            End Sub
            Private Sub OnReceive(ByVal ar As IAsyncResult)
                Try
                    Dim param As AsyncParam = CType(ar.AsyncState, AsyncParam)
                    Dim stream As NetworkStream = Connections.Client(param.Index).GetStream
                    stream.EndRead(ar)
                    stream.ReadTimeout = receiveTimeout

                    Dim key As String = decrypt(param.Buffer)
                    If key <> SendKey Then
                        Throw New KeyNotFoundException("Incorrect receive key")
                    End If

                    Dim receiveLength(3) As Byte

                    Dim totalRead As Integer = 0
                    Dim currentRead As Integer = 0

                    While totalRead < receiveLength.Length
                        currentRead = stream.Read(receiveLength, totalRead, receiveLength.Length - totalRead)
                        totalRead += currentRead
                    End While

                    totalRead = 0
                    currentRead = 0

                    Dim messageSize As Integer = BitConverter.ToInt32(receiveLength, 0)
                    Dim receiveData(messageSize - 1) As Byte 'critical for array size 

                    While totalRead < receiveData.Length
                        currentRead = stream.Read(receiveData, totalRead, receiveData.Length - totalRead)
                        totalRead += currentRead
                    End While

                    '-------------------------- If message is disconnectkey ------------------------------

                    If Encoding.ASCII.GetString(receiveData) = Disconnectkey Then
                        Disconnect(param.Index)
                        Return
                    End If

                    param.Buffer = receiveData
                    RaiseEvent DataReceived(param.Index, param.Buffer, True)
                Catch ex As Exception
                    RaiseEvent TCPError(ex, Reflection.MethodInfo.GetCurrentMethod)
                End Try
            End Sub

            Public Function Send_Bytes(ByVal SendData As Byte(), ByVal index As Integer) As Integer
                Dim stream As NetworkStream = Connections.Client(index).GetStream
                stream.WriteTimeout = sendtimeout

                Dim tcpkey As Byte() = encrypt(SendKey)
                stream.Write(tcpkey, 0, tcpkey.Length)


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

                RaiseEvent DataSent(index, Totalsent, False)
                Return Totalsent 'return # of bytes sent
            End Function
            Public Sub Send_Bytes_Async(ByVal SendData As Byte(), ByVal index As Integer)
                Dim stream As NetworkStream = Connections.Client(index).GetStream
                Dim tcpkey As Byte() = encrypt(SendKey)
                Dim param As New AsyncParam(index, SendData)
                Dim result As IAsyncResult = stream.BeginWrite(tcpkey, 0, tcpkey.Length, New AsyncCallback(AddressOf OnSend), param)
            End Sub
            Private Sub OnSend(ByVal ar As IAsyncResult)
                Try
                    Dim param As AsyncParam = CType(ar.AsyncState, AsyncParam)
                    Dim stream As NetworkStream = Connections.Client(param.Index).GetStream
                    stream.WriteTimeout = sendtimeout

                    Dim sendLength As Byte() = BitConverter.GetBytes(param.Buffer.Length)

                    stream.Write(sendLength, 0, sendLength.Length)

                    Dim Totalsent As Integer = 0
                    Dim Currentsent As Integer = bufferSize

                    While (Totalsent < param.Buffer.Length)

                        If (param.Buffer.Length - Totalsent < bufferSize) Then
                            Currentsent = param.Buffer.Length - Totalsent
                        End If

                        stream.Write(param.Buffer, Totalsent, Currentsent)
                        Totalsent += Currentsent
                    End While

                    RaiseEvent DataSent(param.Index, Totalsent, True)
                Catch ex As Exception
                    RaiseEvent TCPError(ex, Reflection.MethodInfo.GetCurrentMethod)
                End Try
            End Sub

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

    End Class
    Public Class Conversions

        Private _EncryptionKey As String = "MAKV2SPBNI99212"
        Private _EncryptBytes As Boolean = True
        Private _EncryptXML As Boolean = True

        Public Property EncryptBytes As Boolean
            Get
                Return _EncryptBytes
            End Get
            Set(value As Boolean)
                _EncryptBytes = value
            End Set
        End Property
        Public Property EncryptXML As Boolean
            Get
                Return _EncryptXML
            End Get
            Set(value As Boolean)
                _EncryptXML = value
            End Set
        End Property
        Public ReadOnly Property EncryptionKey As String
            Get
                Return _EncryptionKey
            End Get
        End Property

        Public Sub ObjectToXML(ByVal SaveData As Object, ByVal SavePath As String, Optional ByVal FileName As String = Nothing)
            If _EncryptXML Then
                Dim encrypted As Byte() = encrypt(SaveData, _EncryptionKey)
                SaveData = New Object
                SaveData = encrypted
            End If

            If FileName <> Nothing Then
                SavePath = SavePath & "\" & FileName
            End If

            Dim ser As New XmlSerializer(SaveData.GetType)
            Dim fs As New FileStream(SavePath, FileMode.Create)
            ser.Serialize(fs, SaveData)
            fs.Close()
        End Sub
        Public Sub XMLToObject(ByVal LoadPath As String, ByRef LoadData As Object)
            If File.Exists(LoadPath) Then
                If _EncryptXML Then
                    Dim encrypted_data As Byte() = Nothing
                    Dim ser As New XmlSerializer(encrypted_data.GetType)
                    Dim fs As New FileStream(LoadPath, FileMode.OpenOrCreate)
                    encrypted_data = DirectCast(ser.Deserialize(fs), Byte())
                    fs.Close()

                    LoadData = decrypt(encrypted_data, _EncryptionKey)
                Else '-------------------- run normal code ----------------------------
                    Dim ser As New XmlSerializer(LoadData.GetType)
                    Dim fs As New FileStream(LoadPath, FileMode.OpenOrCreate)
                    LoadData = DirectCast(ser.Deserialize(fs), Object)
                    fs.Close()
                End If

            End If
        End Sub

        Public Function FileToBytes(Path As String) As Byte()

            Dim fullPath As String = Path '& "\" & FileName

            Dim Fs As New FileStream(fullPath, FileMode.Open, FileAccess.Read)

            Dim output(Fs.Length - 1) As Byte

            Fs.Read(output, 0, Fs.Length)
            Fs.Close()

            Return output
        End Function
        Public Function BytesToFile(ByteArr As Byte(), Path As String, Optional ByVal FileName As String = Nothing) As Boolean

            Dim fullPath As String

            If FileName = Nothing Then
                fullPath = Path
            Else
                fullPath = Path & "\" & FileName
            End If

            Dim Fs As New FileStream(fullPath, FileMode.Create, FileAccess.Write)
            Fs.Write(ByteArr, 0, ByteArr.Length)
            Fs.Close()

            Return True
        End Function

        Public Function ObjectToBytes(ByVal Input As Object) As Byte()
            If _EncryptBytes Then
                Return encrypt(Input, _EncryptionKey)
            Else
                Dim MS As New MemoryStream()
                Dim BF As New System.Runtime.Serialization.Formatters.Binary.BinaryFormatter()
                BF.Serialize(MS, Input)
                Return MS.ToArray()
            End If
        End Function
        Public Function BytesToObject(ByVal Input As Byte()) As Object
            If _EncryptBytes Then
                Return decrypt(Input, _EncryptionKey)
            Else
                Dim MS As New MemoryStream()
                MS.Write(Input, 0, Input.Length)
                Dim BF As New System.Runtime.Serialization.Formatters.Binary.BinaryFormatter()
                MS.Seek(0, SeekOrigin.Begin)
                Return BF.Deserialize(MS)
            End If
        End Function

        Private Function encrypt(ByVal input As Object, ByVal EncryptionKey As String) As Byte()

            Dim _MemoryStream As New MemoryStream()
            Dim _BinaryFormatter As New System.Runtime.Serialization.Formatters.Binary.BinaryFormatter()
            _BinaryFormatter.Serialize(_MemoryStream, input)
            Dim output As Byte() = _MemoryStream.ToArray()



            Using encryptor As Aes = Aes.Create()
                Dim pdb As New Rfc2898DeriveBytes(EncryptionKey, New Byte() {&H49, &H76, &H61, &H6E, &H20, &H4D, _
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
        Private Function decrypt(ByVal input As Byte(), ByVal EncryptionKey As String) As Object

            Using encryptor As Aes = Aes.Create()
                Dim pdb As New Rfc2898DeriveBytes(EncryptionKey, New Byte() {&H49, &H76, &H61, &H6E, &H20, &H4D, _
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
