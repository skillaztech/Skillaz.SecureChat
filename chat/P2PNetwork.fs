namespace chat

open System
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
        new TcpClient(ip.ToString(), port)
        
    let rec listenForTcpPackage (tcp:TcpListener) dispatch = async {
        let! tcpClient = tcp.AcceptTcpClientAsync() |> Async.AwaitTask
        let networkStream = tcpClient.GetStream()
        
        let lengthBytes = sizeof<int>
        let buffer = Array.zeroCreate lengthBytes
        let! _ = networkStream.ReadAsync(buffer, 0, lengthBytes) |> Async.AwaitTask
        
        let length = BitConverter.ToInt32 buffer
        let buffer = Array.zeroCreate length
        let! read = networkStream.ReadAsync(buffer, 0, length) |> Async.AwaitTask
        
        dispatch buffer read tcpClient
        do! listenForTcpPackage tcp dispatch
    }
    
    let tcpSendAsJson (tcp:TcpClient) payload =
        let stream = tcp.GetStream()
        let msg = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload))
        let length = BitConverter.GetBytes(msg.Length)
        let bytes = Array.append length msg
        stream.Write(bytes)
        stream.Flush()
    
    let udpClient (ip:IPAddress) port =
        new UdpClient(IPEndPoint(ip, port))
    
    let rec listenForUdpPackage (udp:UdpClient) dispatch = async {
        let! receiveResult = udp.ReceiveAsync() |> Async.AwaitTask
        dispatch receiveResult.Buffer receiveResult.RemoteEndPoint
        do! listenForUdpPackage udp dispatch
    }