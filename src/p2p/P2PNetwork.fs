namespace Skillaz.SecureChat

open System
open System.Net
open System.Net.Sockets
open System.Text
open System.Text.Json
open System.Threading.Tasks

module P2PNetwork =
    
    let defaultSocketTimeoutMs = 500
    
    let connectSocket (socket:Socket) (endpoint:EndPoint) =
        let connectTask = socket.ConnectAsync(endpoint)
        let cancelConnectByTimeoutTask = Task.Delay defaultSocketTimeoutMs
        let timeoutTask = Task.WhenAny [| connectTask; cancelConnectByTimeoutTask |]
        
        timeoutTask |> Async.AwaitTask |> Async.RunSynchronously |> Async.AwaitTask |> Async.RunSynchronously
        
        if cancelConnectByTimeoutTask.IsCompleted
        then raise <| TimeoutException "Connection timed out"
        
        socket
        
    let rec listenSocket (socket:Socket) invoke = async {
        use! tcpClient = socket.AcceptAsync() |> Async.AwaitTask
        
        tcpClient.SendTimeout <- defaultSocketTimeoutMs
        
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
        
        let rec readTillTheEnd length read buffer (stream: NetworkStream) = async {
            let! readCurrentBatch = stream.ReadAsync(buffer, read, (length - read)) |> Async.AwaitTask
            let alreadyRead = read + readCurrentBatch
            if alreadyRead = length
            then return length
            else return! readTillTheEnd length alreadyRead buffer stream
        }
        
        let! read = readTillTheEnd length 0 packagePayloadBuffer networkStream
        
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
