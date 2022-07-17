namespace chat

open System
open System.Net
open System.Net.Sockets
open System.Text
open System.Text.Json

module P2PNetwork =
    
    type TcpPackage =
        | Ping
        | Message of byte[]
    
    let tcpListener ip port =
        let tcp = TcpListener(ip, port)
        tcp.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true)
        tcp
        
    let tcpClient (ip:IPAddress) port =
        let tcp = new TcpClient(ip.ToString(), port)
        tcp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true)
        tcp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseUnicastPort, true)
        
    let rec listenForTcpPackage (tcp:TcpListener) invoke = async {
        let! tcpClient = tcp.AcceptTcpClientAsync() |> Async.AwaitTask
        let networkStream = tcpClient.GetStream()
        
        let length = sizeof<int>
        let packageTypeBuffer = Array.zeroCreate length
        let! _ = networkStream.ReadAsync(packageTypeBuffer, 0, length) |> Async.AwaitTask
        
        let packageType = BitConverter.ToInt32 packageTypeBuffer
        
        do! async {
            match packageType with
            | 0 ->
                invoke TcpPackage.Ping 0 tcpClient
            | 1 ->
                let length = sizeof<int>
                let packageLengthBuffer = Array.zeroCreate length
                let! _ = networkStream.ReadAsync(packageLengthBuffer, 0, length) |> Async.AwaitTask
                
                let length = BitConverter.ToInt32 packageLengthBuffer
                let packagePayloadBuffer = Array.zeroCreate length
                let! read = networkStream.ReadAsync(packagePayloadBuffer, 0, length) |> Async.AwaitTask
                
                invoke (TcpPackage.Message packagePayloadBuffer) read tcpClient
            | _ -> failwith "Unknown TCP packet"
        }
        
        do! listenForTcpPackage tcp invoke
    }
    
    let tcpSendPing (tcp:TcpClient) =
        use stream = tcp.GetStream()
        let packageType = BitConverter.GetBytes(0)
        stream.Write(packageType)
        stream.Flush()
    
    let tcpSendAsJson (tcp:TcpClient) payload =
        use stream = tcp.GetStream()
        let msg = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload)) |> List.ofArray
        let packageType = BitConverter.GetBytes(1) |> List.ofArray
        let length = BitConverter.GetBytes(msg.Length) |> List.ofArray
        let bytes = packageType @ length @ msg |> Array.ofList
        stream.Write(bytes)
        stream.Flush()
    
    let udpClient (ip:IPAddress) port =
        new UdpClient(IPEndPoint(ip, port))
    
    let rec listenForUdpPackage (udp:UdpClient) dispatch = async {
        let! receiveResult = udp.ReceiveAsync() |> Async.AwaitTask
        dispatch receiveResult.Buffer receiveResult.RemoteEndPoint
        do! listenForUdpPackage udp dispatch
    }