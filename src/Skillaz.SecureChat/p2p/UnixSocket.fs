namespace Skillaz.SecureChat

open System
open System.IO
open System.Net
open System.Net.Sockets
open System.Threading.Tasks
open Skillaz.SecureChat.P2PNetwork

module UnixSocket =
    
    let private connectSocket (socket:Socket) (endpoint:EndPoint) =
        let connectTask = socket.ConnectAsync(endpoint)
        let cancelConnectByTimeoutTask = Task.Delay 500
        let timeoutTask = Task.WhenAny [| connectTask; cancelConnectByTimeoutTask |]
        
        timeoutTask |> Async.AwaitTask |> Async.RunSynchronously |> Async.AwaitTask |> Async.RunSynchronously
        
        if cancelConnectByTimeoutTask.IsCompleted
        then raise <| TimeoutException "Connection timed out"
        
        socket
    
    let unixSocketListener (path:string) =
        Directory.CreateDirectory(Path.GetDirectoryName(path)) |> ignore
        File.Delete(path)
        let socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP)
        socket.Bind(UnixDomainSocketEndPoint(path))
        socket
        
    let unixSocketClient path =
        let socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified)
        connectSocket socket <| UnixDomainSocketEndPoint(path)
                
    let rec listenForSocketConnection (socket:Socket) invoke = async {
        let! socket = socket.AcceptAsync() |> Async.AwaitTask
        invoke socket
        do! listenForSocketConnection socket invoke
    }
    
    let rec listenForSocketPackages (socket:Socket) invoke = async {
        if socket.Connected then
            try
                let networkStream = new NetworkStream(socket)
                
                let length = sizeof<int>
                let packageTypeBuffer = Array.zeroCreate length
                let! read = networkStream.ReadAsync(packageTypeBuffer, 0, length) |> Async.AwaitTask
                
                let packageType = BitConverter.ToInt32 packageTypeBuffer
                
                match packageType with
                | 210 ->
                    invoke TcpPackage.Ping read
                | pt ->
                    let length = sizeof<int>
                    let packageLengthBuffer = Array.zeroCreate length
                    let! _ = networkStream.ReadAsync(packageLengthBuffer, 0, length) |> Async.AwaitTask
                    
                    let length = BitConverter.ToInt32 packageLengthBuffer
                    let packagePayloadBuffer = Array.zeroCreate length
                    let! read = networkStream.ReadAsync(packagePayloadBuffer, 0, length) |> Async.AwaitTask
                    
                    match pt with
                    | 202 ->
                        invoke (TcpPackage.Hello packagePayloadBuffer) read
                    | 201 ->
                        invoke (TcpPackage.Message packagePayloadBuffer) read
                    | _ -> ()
                
                networkStream.Flush()
            with
            | e -> ()
            
            do! listenForSocketPackages socket invoke
    }