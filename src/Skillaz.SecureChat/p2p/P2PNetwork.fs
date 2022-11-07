namespace Skillaz.SecureChat

open System
open System.Net
open System.Net.Sockets
open System.Text
open System.Text.Json
open System.Threading.Tasks

module P2PNetwork =
    
    type SscPackage =
        | Ping
        | Message of byte[]
        | Hello of byte[]
    
    let connectSocket (socket:Socket) (endpoint:EndPoint) =
        let connectTask = socket.ConnectAsync(endpoint)
        let cancelConnectByTimeoutTask = Task.Delay 500
        let timeoutTask = Task.WhenAny [| connectTask; cancelConnectByTimeoutTask |]
        
        timeoutTask |> Async.AwaitTask |> Async.RunSynchronously |> Async.AwaitTask |> Async.RunSynchronously
        
        if cancelConnectByTimeoutTask.IsCompleted
        then raise <| TimeoutException "Connection timed out"
        
        socket
        
    let rec listenSocket (socket:Socket) invoke = async {
        use! tcpClient = socket.AcceptAsync() |> Async.AwaitTask
        invoke tcpClient
        do! listenSocket socket invoke
    }
    
    let rec listenSocketPackages (tcpClient:Socket) invoke = async {
        if tcpClient.Connected then
            try
                let networkStream = new NetworkStream(tcpClient)
                
                let length = sizeof<int>
                let packageTypeBuffer = Array.zeroCreate length
                let! read = networkStream.ReadAsync(packageTypeBuffer, 0, length) |> Async.AwaitTask
                
                let packageType = BitConverter.ToInt32 packageTypeBuffer
                
                match packageType with
                | 210 ->
                    invoke SscPackage.Ping read tcpClient
                | pt ->
                    let length = sizeof<int>
                    let packageLengthBuffer = Array.zeroCreate length
                    let! _ = networkStream.ReadAsync(packageLengthBuffer, 0, length) |> Async.AwaitTask
                    
                    let length = BitConverter.ToInt32 packageLengthBuffer
                    let packagePayloadBuffer = Array.zeroCreate length
                    let! read = networkStream.ReadAsync(packagePayloadBuffer, 0, length) |> Async.AwaitTask
                    
                    match pt with
                    | 202 ->
                        invoke (SscPackage.Hello packagePayloadBuffer) read tcpClient
                    | 201 ->
                        invoke (SscPackage.Message packagePayloadBuffer) read tcpClient
                    | _ -> ()
                
                networkStream.Flush()
            with
            | _ -> ()
            
            do! listenSocketPackages tcpClient invoke
    }
    
    let private sendJsonPkg (socket:Socket) (pkgType:int) payload =
        let packageType = BitConverter.GetBytes(pkgType) |> List.ofArray
        let msg = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload)) |> List.ofArray
        let length = BitConverter.GetBytes(msg.Length) |> List.ofArray
        let bytes = packageType @ length @ msg |> Array.ofList
        socket.Send(bytes) |> ignore
        
    let sendPing (socket:Socket) =
        let packageType = BitConverter.GetBytes(210)
        socket.Send(packageType) |> ignore
        
    let sendHello (socket:Socket) payload =
        sendJsonPkg socket 202 payload
        
    let sendMessage (socket:Socket) payload =
        sendJsonPkg socket 201 payload
