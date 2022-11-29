namespace Skillaz.SecureChat

open System
open System.Net
open System.Net.Sockets
open System.Text
open System.Text.Json
open System.Threading.Tasks

module P2PNetwork =
    
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
    
    let listenAndHandleSocketPackage (tcpClient:Socket) invoke = async {
        let networkStream = new NetworkStream(tcpClient)
            
        let length = sizeof<int>
        let packageTypeBuffer = Array.zeroCreate length
        let! _ = networkStream.ReadAsync(packageTypeBuffer, 0, length) |> Async.AwaitTask
        
        let packageType = BitConverter.ToInt32 packageTypeBuffer
        
        let length = sizeof<int>
        let packageLengthBuffer = Array.zeroCreate length
        let! _ = networkStream.ReadAsync(packageLengthBuffer, 0, length) |> Async.AwaitTask
        
        let length = BitConverter.ToInt32 packageLengthBuffer
        let packagePayloadBuffer = Array.zeroCreate length
        let! read = networkStream.ReadAsync(packagePayloadBuffer, 0, length) |> Async.AwaitTask
        
        invoke packageType packagePayloadBuffer read tcpClient
        
        networkStream.Flush()
    }
    
    let private sendJsonPkg (socket:Socket) (pkgType:int) payload =
        let packageType = BitConverter.GetBytes(pkgType) |> List.ofArray
        let msg = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload)) |> List.ofArray
        let length = BitConverter.GetBytes(msg.Length) |> List.ofArray
        let bytes = packageType @ length @ msg |> Array.ofList
        socket.Send(bytes) |> ignore
        
    let send packageType (socket:Socket) payload =
        sendJsonPkg socket packageType payload
