namespace Skillaz.SecureChat

open System.Net
open System.Net.Sockets

module Tcp =
    
    let listener =
        let tcp = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
        tcp.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true)
        tcp
        
    let tryBindTo (ip:IPAddress) port (tcp:Socket) =
        let ep = IPEndPoint(ip, port)
        tcp.Bind(ep)
        tcp
    
    let client (ip:IPAddress) port localPort =
        let socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
        socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true)
        socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true)
        socket.Bind(IPEndPoint(IPAddress.Any, localPort))
        
        P2PNetwork.connectSocket socket <| IPEndPoint(ip, port)

