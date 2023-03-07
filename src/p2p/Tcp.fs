namespace Skillaz.SecureChat

open System.Net
open System.Net.Sockets
open P2PNetwork

module Tcp =
    
    let listener =
        let socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
        socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true)
        socket.SendTimeout <- defaultSocketTimeoutMs
        socket
        
    let tryBindTo (ip:IPAddress) port (tcp:Socket) =
        let ep = IPEndPoint(ip, port)
        tcp.Bind(ep)
    
    let client localPort =
        let socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
        socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true)
        socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true)
        socket.SendTimeout <- defaultSocketTimeoutMs
        socket.Bind(IPEndPoint(IPAddress.Any, localPort))
        socket
        
    let connectSocket (ip:IPAddress) port socket =
        P2PNetwork.connectSocket socket <| IPEndPoint(ip, port)

