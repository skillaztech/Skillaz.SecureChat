namespace skillaz_secure_chat

open System.Net
open System.Net.Sockets

module P2PNetwork =
    let listener ip port =
        let tcp = TcpListener(ip, port)
        tcp.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true)
        tcp
    let client (ip:IPAddress) port =
        new TcpClient(IPEndPoint(ip, port))
        
    let rec listenForPackage (tcp:TcpListener) dispatch = async {
        tcp.Start()
        let! tcpClient = tcp.AcceptTcpClientAsync() |> Async.AwaitTask
        let networkStream = tcpClient.GetStream()
        let buffer = Array.zeroCreate 256
        let read = networkStream.Read(buffer, 0, 256)
        dispatch buffer read
        do! listenForPackage tcp dispatch
    }