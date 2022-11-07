namespace Skillaz.SecureChat

open System.IO
open System.Net.Sockets
open Skillaz.SecureChat.P2PNetwork

module UnixSocket =
    
    let listener (path:string) =
        Directory.CreateDirectory(Path.GetDirectoryName(path)) |> ignore
        File.Delete(path)
        let socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP)
        socket.Bind(UnixDomainSocketEndPoint(path))
        socket
        
    let client path =
        let socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP)
        connectSocket socket <| UnixDomainSocketEndPoint(path)