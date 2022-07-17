namespace chat

open System.Net
open System.Net.Sockets
open System.Text
open System.Text.Json

module P2PNetwork =
    let tcpListener ip port =
        let tcp = TcpListener(ip, port)
        tcp.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true)
        tcp
    let tcpClient (ip:IPAddress) port =
        new TcpClient(IPEndPoint(ip, port))
        
    let rec listenForTcpPackage (tcp:TcpListener) dispatch = async {
        tcp.Start()
        let! tcpClient = tcp.AcceptTcpClientAsync() |> Async.AwaitTask
        let networkStream = tcpClient.GetStream()
        let buffer = Array.zeroCreate tcpClient.ReceiveBufferSize
        let read = networkStream.Read(buffer, 0, tcpClient.ReceiveBufferSize)
        dispatch buffer read
        do! listenForTcpPackage tcp dispatch
    }
    
    let tcpSendAsJson (tcp:TcpClient) payload =
        use stream = tcp.GetStream()
        let message = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload))
        stream.Write(message)
        stream.Flush()
    
    let udpClient (ip:IPAddress) port =
        new UdpClient(IPEndPoint(ip, port))
    
    let rec listenForUdpPackage (udp:UdpClient) dispatch = async {
        let! receiveResult = udp.ReceiveAsync() |> Async.AwaitTask
        dispatch receiveResult.Buffer receiveResult.RemoteEndPoint
        do! listenForUdpPackage udp dispatch
    }