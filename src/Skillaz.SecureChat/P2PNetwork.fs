namespace Skillaz.SecureChat

open System
open System.Net
open System.Net.Sockets
open System.Text
open System.Text.Json

module P2PNetwork =
    
    type TcpPackage =
        | Ping
        | Message of byte[]
        | Hello of byte[]
    
    let tcpListener ip port =
        let tcp = TcpListener(ip, port)
        tcp.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true)
        tcp
        
    let tcpClient (ip:IPAddress) port localPort =
        let tcp = new TcpClient(AddressFamily.InterNetwork)
        tcp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true)
        tcp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true)
        tcp.Client.Bind(IPEndPoint(IPAddress.Any, localPort))
        tcp.SendTimeout <- 2000
        tcp.ReceiveTimeout <- 2000
        tcp.Connect(ip, port)
        tcp
        
    let rec listenForTcpConnection (tcp:TcpListener) invoke = async {
        use! tcpClient = tcp.AcceptTcpClientAsync() |> Async.AwaitTask
        invoke tcpClient
        do! listenForTcpConnection tcp invoke
    }
    
    let rec listenForTcpPackages (tcpClient:TcpClient) invoke = async {
        if tcpClient.Connected then
            try
                let networkStream = tcpClient.GetStream()
                
                let length = sizeof<int>
                let packageTypeBuffer = Array.zeroCreate length
                let! read = networkStream.ReadAsync(packageTypeBuffer, 0, length) |> Async.AwaitTask
                
                let packageType = BitConverter.ToInt32 packageTypeBuffer
                
                match packageType with
                | 210 ->
                    invoke TcpPackage.Ping read tcpClient
                | pt ->
                    let length = sizeof<int>
                    let packageLengthBuffer = Array.zeroCreate length
                    let! _ = networkStream.ReadAsync(packageLengthBuffer, 0, length) |> Async.AwaitTask
                    
                    let length = BitConverter.ToInt32 packageLengthBuffer
                    let packagePayloadBuffer = Array.zeroCreate length
                    let! read = networkStream.ReadAsync(packagePayloadBuffer, 0, length) |> Async.AwaitTask
                    
                    match pt with
                    | 202 ->
                        invoke (TcpPackage.Hello packagePayloadBuffer) read tcpClient
                    | 201 ->
                        invoke (TcpPackage.Message packagePayloadBuffer) read tcpClient
                    | _ -> ()
                
                networkStream.Flush()
            with
            | e -> ()
            
            do! listenForTcpPackages tcpClient invoke
    }
        
    let private tcpSendJsonPkg (tcp:TcpClient) (pkgType:int) payload =
        let stream = tcp.GetStream()
        let packageType = BitConverter.GetBytes(pkgType) |> List.ofArray
        let msg = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload)) |> List.ofArray
        let length = BitConverter.GetBytes(msg.Length) |> List.ofArray
        let bytes = packageType @ length @ msg |> Array.ofList
        stream.Write(bytes)
        stream.Flush()
    
    let tcpSendPing (tcp:TcpClient) =
        let stream = tcp.GetStream()
        let packageType = BitConverter.GetBytes(210)
        stream.Write(packageType)
        stream.Flush()
    
    let tcpSendHello (tcp:TcpClient) payload =
        tcpSendJsonPkg tcp 202 payload
    
    let tcpSendMessage (tcp:TcpClient) payload =
        tcpSendJsonPkg tcp 201 payload
        
    
    let udpClient (ip:IPAddress) port =
        new UdpClient(IPEndPoint(ip, port))
    
    let rec listenForUdpPackage (udp:UdpClient) dispatch = async {
        let! receiveResult = udp.ReceiveAsync() |> Async.AwaitTask
        dispatch receiveResult.Buffer receiveResult.RemoteEndPoint
        do! listenForUdpPackage udp dispatch
    }