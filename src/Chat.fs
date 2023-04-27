namespace Skillaz.SecureChat

open System.Runtime.ExceptionServices
open Avalonia
open Avalonia.Media
open FSharp.Core.LanguagePrimitives
open System
open System.IO
open Avalonia.FuncUI.DSL
open System.Net
open System.Net.Sockets
open System.Text
open System.Text.Json
open System.Threading.Tasks
open Avalonia.Controls.Primitives
open Avalonia.Input
open Avalonia.Layout
open Elmish
open Avalonia.FuncUI
open Avalonia.Controls
open Skillaz.SecureChat.ChatArgs
open Skillaz.SecureChat.Domain.Domain
open Skillaz.SecureChat.P2P

module Chat =
    let logger = Logger.nlogger
    
    type PackageType =
        | Alive = 201
        | Message = 202
    
    type LocalMessage = {
        Message: ChatMessage
        IsMe: bool
    }
    
    type ConnectedEndpoint = {
        ConnectionId: string
        EndPoint: EndPoint
        Client: Socket
    }
    
    type ConnectedApp = {
        AppName: string
        UserId: string
        ConnectedTill: DateTime
    }
    
    type Model = {
        Args: ChatArgs
        UserName: string
        UserNameValidationErrors: string list
        SecretCode: int
        KnownPeers: IPEndPoint list
        ListenerPort: int
        ClientPort: int
        UserId: string
        MaxChatMessageLength: int
        TcpListener: Socket
        UnixSocketFolder: string
        UnixSocketFilePath: string
        UnixSocketListener: Socket
        MessageInput: string
        MessagesList: LocalMessage list
        Connections: ConnectedEndpoint list
        ConnectedUsers: ConnectedApp list
        SettingsVisible: bool
        ChatScrollViewOffset: float
        MessagesListHashSet: Set<int>
    }
        
    type Msg =
        | WaitThenSend of int * Msg
        | StartLaunchListenRemoteConnectionsLoop
        | LaunchListenRemoteConnectionsIterationFinished of Result<unit, unit>
        | StartLaunchListenLocalConnectionsLoop
        | LaunchListenLocalConnectionsIterationFinished of Result<unit, unit>
        | StartConnectToRemotePeersLoop
        | ConnectToRemotePeersIterationFinished of ConnectedEndpoint list
        | StartConnectToLocalPeersLoop
        | ConnectToLocalPeersIterationFinished of ConnectedEndpoint list
        | ClientsConnected of ConnectedEndpoint list
        | ClientDisconnected of Socket
        | StartCleanDeadAppsLoop
        | DeadAppsCleanIterationFinished of ConnectedApp list
        | StartSendIAmAliveLoop
        | IAmAliveSendIterationFinished of ConnectedEndpoint list
        | AlivePackageReceived of AliveMessage * Socket
        | RetranslateAlivePackage of AliveMessage
        | RemoteChatMessageReceived of ChatMessage * Socket
        | RetranslateChatMessage of ChatMessage
        | TextChanged of string
        | AppendLocalMessage of LocalMessage
        | SendMessage
        | ToggleSettingsVisibility
        | SecretCodeChanged of int
        | UserNameChanged of string
        | SaveUserSettingsToConfig
        | ScrollChatToEnd
        
    let connectionsSubscription listener dispatch =
        let handle (socket:Socket) =
            logger.Info $"[connectionsSubscription] New connection entering listener {socket.LocalEndPoint} from {socket.RemoteEndPoint}..."
            
            let connectedEndpoint = {
                ConnectionId = $"{socket.RemoteEndPoint}"
                EndPoint = socket.RemoteEndPoint
                Client = socket
            }
            
            Msg.ClientsConnected [connectedEndpoint] |> dispatch
        P2PNetwork.listenSocket listener handle |> Async.Start

    type Exception with
        member this.Reraise () = // https://github.com/fsharp/fslang-suggestions/issues/660
            (ExceptionDispatchInfo.Capture this).Throw ()
            Unchecked.defaultof<_>
        
    let packagesSubscription client dispatch =
        let handleSocketPackage dispatch packageType bytes read socket =
            
            let json = Encoding.UTF8.GetString(bytes, 0, read)
            let pt = EnumOfValue(packageType)
            match pt with
            | PackageType.Alive ->
                let msg = JsonSerializer.Deserialize<AliveMessage>(json)
                Msg.AlivePackageReceived (msg, socket) |> dispatch
            | PackageType.Message ->
                let msg = JsonSerializer.Deserialize<ChatMessage>(json)
                Msg.RemoteChatMessageReceived (msg, socket) |> dispatch
            | _ -> ()
            
        let handleSocket = handleSocketPackage dispatch
        
        let rec handlePackages client dispatch = async {
            try
                do! P2PNetwork.listenAndHandleSocketPackage client handleSocket
                do! handlePackages client dispatch
            with
            | :? SocketException as e ->
                logger.WarnException e $"[packagesSubscription] Failed to handle package from {client.RemoteEndPoint} as SocketException. Stopping handling packages..."
                dispatch <| ClientDisconnected client
            | :? IOException as e ->
                logger.WarnException e $"[packagesSubscription] Failed to handle package from {client.RemoteEndPoint} as IOException. Stopping handling packages..."
                dispatch <| ClientDisconnected client
            | :? AggregateException as e ->
                let criticalExceptions =
                    e.InnerExceptions
                    |> List.ofSeq
                    |> List.where (fun o -> o :? SocketException || o :? IOException)
                if criticalExceptions |> List.isEmpty
                then
                    logger.WarnException e $"[packagesSubscription] Failed to handle package from {client.RemoteEndPoint} as safe exception."
                else
                    logger.WarnException e $"[packagesSubscription] Failed to handle package from {client.RemoteEndPoint} as SocketException or IOException. Stopping handling packages..."
                    dispatch <| ClientDisconnected client
            | e ->
                logger.WarnException e $"[packagesSubscription] Failed to handle package from {client.RemoteEndPoint}"
                
        }
        
        handlePackages client dispatch |> Async.Start
    
    let init (args: ChatArgs) =
        
        let unixSocketListener = UnixSocket.listener
        let tcpListener = Tcp.listener
        
        let exitHandler _ _ =
            try                
                logger.Info $"[exitHandler] Disposing tcp listener: {tcpListener.LocalEndPoint}"
                tcpListener.Dispose()
            with
            | e ->
                logger.FatalException e "[exitHandler] Application exiting failed."
                
        args.ApplicationLifetime.Exit.AddHandler(exitHandler)
        
        let model = {
            Args = args
            KnownPeers = args.AppSettings.KnownRemotePeers
            ListenerPort = args.AppSettings.ListenerTcpPort
            ClientPort = args.AppSettings.ClientTcpPort
            SecretCode = args.UserSettings.SecretCode
            UserName = args.UserSettings.Name
            UserNameValidationErrors = []
            UserId = args.UserSettings.UserId.ToString()
            MaxChatMessageLength = args.AppSettings.MaxChatMessageLength
            TcpListener = tcpListener
            UnixSocketListener = unixSocketListener
            UnixSocketFolder = args.UnixSocketsFolderPath
            UnixSocketFilePath = args.UnixSocketFilePath
            Connections = []
            ConnectedUsers = []
            MessageInput = ""
            MessagesList = []
            MessagesListHashSet = Set.empty
            SettingsVisible = false
            ChatScrollViewOffset = 0
        }
        
        let cmd = Cmd.batch [
            Cmd.ofMsg Msg.StartLaunchListenRemoteConnectionsLoop
            Cmd.ofMsg Msg.StartLaunchListenLocalConnectionsLoop
            Cmd.ofMsg Msg.StartCleanDeadAppsLoop
            Cmd.ofMsg Msg.StartSendIAmAliveLoop
            Cmd.ofMsg Msg.StartConnectToRemotePeersLoop
            Cmd.ofMsg Msg.StartConnectToLocalPeersLoop
        ]
        
        model, cmd
    
    let update msg model =
        
        logger.Trace $"[update] Handling message {msg} with model state {model}..."
        
        match msg with
        | WaitThenSend (msToWait, msg) ->
            let waitTask ms = async {
                do! Task.Delay(TimeSpan.FromMilliseconds(ms)) |> Async.AwaitTask
            }
            model, Cmd.OfAsync.perform waitTask msToWait (fun _ -> msg)
        | StartLaunchListenRemoteConnectionsLoop ->
            let tryListenRemoteConnections _ = async {
                if not model.TcpListener.IsBound
                then
                    try
                        logger.Debug $"[StartLaunchListenRemoteConnectionsLoop] Try to bind and start listening for tcp remote connections on port {model.ListenerPort}..."
                        
                        Tcp.tryBindTo IPAddress.Any model.ListenerPort model.TcpListener
                        model.TcpListener.Listen()
                        
                        logger.Info $"[StartLaunchListenRemoteConnectionsLoop] Tcp listener started on port {model.ListenerPort}"
                        
                        return Result.Ok ()
                    with
                    | e ->
                        logger.DebugException e $"[StartLaunchListenRemoteConnectionsLoop] Failed to bind or start listening tcp remote connections on port {model.ListenerPort}"
                        
                        return Result.Error ()
                else
                    logger.Debug $"[StartLaunchListenRemoteConnectionsLoop] Already listening remote tcp connections. No additional actions required."
                    
                    return Result.Error ()
            }
            model, Cmd.OfAsync.perform tryListenRemoteConnections () LaunchListenRemoteConnectionsIterationFinished
        | LaunchListenRemoteConnectionsIterationFinished tcpListener ->
            match tcpListener with
            | Result.Ok _ ->
                logger.Info $"[LaunchListenRemoteConnectionsIterationFinished] Launching listening remote subscriptions..."
                
                let cmds = Cmd.batch [
                    Cmd.ofSub <| connectionsSubscription model.TcpListener
                    Cmd.ofMsg <| WaitThenSend (2000, StartLaunchListenRemoteConnectionsLoop)
                ]
                model, cmds
            | Result.Error _ ->
                model, Cmd.ofMsg <| WaitThenSend (2000, StartLaunchListenRemoteConnectionsLoop)
        | StartLaunchListenLocalConnectionsLoop ->
            let tryListenLocalConnections _ = async {
                if model.TcpListener.IsBound
                then
                    if not model.UnixSocketListener.IsBound
                    then
                        try
                            logger.Debug $"[StartLaunchListenLocalConnectionsLoop] Try to bind and start listening for unix socket local connections on file {model.UnixSocketFilePath}..."
                            
                            UnixSocket.tryBindTo model.UnixSocketFilePath model.UnixSocketListener
                            model.UnixSocketListener.Listen()
                            
                            logger.Info $"[StartLaunchListenLocalConnectionsLoop] Unix socket listener started on file {model.UnixSocketFilePath}"
                            return Result.Ok ()
                        with
                        | :? ObjectDisposedException as e ->
                            logger.DebugException e $"[StartLaunchListenRemoteConnectionsLoop] Unix socket possibly already has been disposed. Deleting unix socket file for recreation in future..."
                            
                            File.Delete(model.UnixSocketFilePath)
                            
                            return Result.Error ()
                        | e ->
                            logger.DebugException e $"[StartLaunchListenLocalConnectionsLoop] Failed to bind or start listening unix socket local connections on file {model.UnixSocketFilePath}"
                            return Result.Error ()
                    else
                        logger.Debug $"[StartLaunchListenLocalConnectionsLoop] Already listening local unix socket connections. No additional actions required."
                        return Result.Error ()
                else
                    logger.Debug $"[StartLaunchListenLocalConnectionsLoop] Remote tcp listener is unbounded. No need to listen local peers."
                    return Result.Error ()
            }
            model, Cmd.OfAsync.perform tryListenLocalConnections () LaunchListenLocalConnectionsIterationFinished
        | LaunchListenLocalConnectionsIterationFinished res  ->
            match res with
            | Result.Ok _ ->
                logger.Info $"[LaunchListenLocalConnectionsIterationFinished] Launching listening local subscriptions..."
                
                let cmds = Cmd.batch [
                    Cmd.ofSub <| connectionsSubscription model.UnixSocketListener
                    Cmd.ofMsg <| WaitThenSend (2000, StartLaunchListenLocalConnectionsLoop)
                ]
                model, cmds
            | Result.Error _ ->
                model, Cmd.ofMsg <| WaitThenSend (2000, StartLaunchListenLocalConnectionsLoop)
        | StartConnectToLocalPeersLoop ->
            let connectToLocalPeers _ = async {
                if not model.UnixSocketListener.IsBound
                then
                    logger.Debug $"[TryConnectToLocalPeers] Searching for local peers with unix socket open in folder {model.UnixSocketFolder}..."
                    
                    let otherNonConnectedUnixSocketFiles =
                        Directory.GetFiles(model.UnixSocketFolder)
                        |> List.ofArray
                        |> List.where (fun socket -> model.Connections |> List.exists (fun x -> x.ConnectionId = socket) |> not)
                        
                    if otherNonConnectedUnixSocketFiles |> List.length > 0
                    then logger.Debug $"[TryConnectToLocalPeers] Other non-connected unix sockets found. Connecting to {otherNonConnectedUnixSocketFiles}..."
                    else logger.Debug $"[TryConnectToLocalPeers] No other non-connected unix sockets found. Skipping..."
                    
                    return
                        otherNonConnectedUnixSocketFiles
                        |> Array.ofList
                        |> Array.Parallel.map (fun socketFile ->
                            let socket = UnixSocket.client
                            logger.Debug $"[TryConnectToLocalPeers] Connecting to local unix socket {socketFile}..."
                            
                            try
                                let unixSocketClient = UnixSocket.connectSocket socket socketFile
                                let connectionEndpoint = {
                                    ConnectionId = socketFile
                                    EndPoint = unixSocketClient.RemoteEndPoint
                                    Client = unixSocketClient
                                }
                                Some connectionEndpoint
                            with
                            | e ->
                                logger.DebugException e $"[TryConnectToLocalPeers] Failed to connect to local unix socket {socketFile}. Disposing socket..."
                                socket.Dispose()
                                None
                        )
                        |> Array.choose id
                        |> List.ofArray
                else
                    logger.Debug $"[TryConnectToLocalPeers] Current app instance does not need to connect to local peers because tcp listener not bounded."
                    
                    return []
            }
            model, Cmd.OfAsync.perform connectToLocalPeers () ConnectToLocalPeersIterationFinished
        | ConnectToLocalPeersIterationFinished peers ->
            
            logger.Debug $"[ConnectToLocalPeersIterationFinished] Connecting to local peers {peers} finished. Launching subscriptions..."
            
            let msgs = Cmd.batch [
                Cmd.ofMsg <| ClientsConnected peers
                Cmd.ofMsg <| WaitThenSend (2000, StartConnectToLocalPeersLoop)
            ]
            model, msgs
        | StartConnectToRemotePeersLoop ->
            let connectToRemotePeers _ = async {
                if model.TcpListener.IsBound
                then
                    logger.Debug $"[StartConnectToRemotePeersLoop] Defining non-connected known peers..."
                    
                    let nonConnectedRemotePeers =
                        model.KnownPeers
                        |> List.where (fun ep -> model.Connections |> List.exists (fun x -> x.ConnectionId = ep.ToString()) |> not)
                        
                    if nonConnectedRemotePeers |> List.length > 0
                    then logger.Debug $"[StartConnectToRemotePeersLoop] Non-connected known peers found {nonConnectedRemotePeers}. Connecting..."
                    else logger.Debug $"[StartConnectToRemotePeersLoop] All known peers already connected. Skipping..."
                    
                    return
                        nonConnectedRemotePeers
                        |> Array.ofList
                        |> Array.Parallel.map (fun ep ->
                            let socket = Tcp.client model.ClientPort
                            
                            logger.Debug $"[StartConnectToRemotePeersLoop] Connecting to remote tcp endpoint {ep} from {socket.LocalEndPoint}..."
                            
                            try
                                let connectedSocket = Tcp.connectSocket ep.Address ep.Port socket
                                let connectionEndpoint = {
                                    ConnectionId = ep.ToString()
                                    EndPoint = ep
                                    Client = connectedSocket
                                }
                                Some connectionEndpoint
                            with
                            | e ->
                                logger.DebugException e $"[StartConnectToRemotePeersLoop] Connection to remote tcp endpoint {ep} failed. Disposing socket..."
                                socket.Dispose()
                                None
                        )
                        |> Array.choose id
                        |> List.ofArray
                else
                    logger.Debug $"[StartConnectToRemotePeersLoop] Current app instance does not need to connect to remote peers because tcp listener not bounded."
                    
                    return []
            }
            
            model, Cmd.OfAsync.perform connectToRemotePeers () ConnectToRemotePeersIterationFinished
        | ConnectToRemotePeersIterationFinished peers ->
            
            logger.Debug $"[ConnectToRemotePeersIterationFinished] Connecting to remote peers {peers} finished. Launching subscriptions..."
            
            let msgs = Cmd.batch [
                Cmd.ofMsg <| ClientsConnected peers
                Cmd.ofMsg <| WaitThenSend (2000, StartConnectToRemotePeersLoop)
            ]
            model, msgs
        | ClientsConnected newlyConnected ->
            let cmds =
                newlyConnected
                |> List.map (fun c ->
                    logger.Info $"[ClientsConnected] Peer connected {c}. Launch packages subscription..."
                    Cmd.ofSub <| packagesSubscription c.Client
                )
            
            { model with Connections = model.Connections @ newlyConnected }, Cmd.batch cmds
        | ClientDisconnected socket ->
            
            logger.Info $"[ClientDisconnected] Closing connection from {socket.LocalEndPoint} to {socket.RemoteEndPoint}..."
            
            let connections =
                model.Connections
                |> List.where (fun o -> socket.RemoteEndPoint <> null && o.ConnectionId <> socket.RemoteEndPoint.ToString())
                
            socket.Dispose()
            
            { model with Connections = connections }, Cmd.none
        | StartSendIAmAliveLoop ->
            let sendIAmAliveMessageAndGetAvailableConnections _ = async {
                
                logger.Debug $"[StartSendIAmAliveLoop] Sending I am alive package to all connections..."
                
                return
                    model.Connections
                    |> Array.ofList
                    |> Array.Parallel.map (fun conn ->
                        try
                            let msg = {
                                MessageSender = model.UserName
                                UserId = model.UserId
                                SecretCode = model.SecretCode
                                RetranslationInfo = {
                                    RetranslatedBy = [ model.UserId ]
                                }
                            }
                            P2PNetwork.send (EnumToValue(PackageType.Alive)) conn.Client msg
                            Some conn
                        with
                        | e ->
                            logger.DebugException e $"[StartSendIAmAliveLoop] Failed to send I am alive package {msg} to {conn}"
                            
                            None
                    )
                    |> List.ofArray
                    |> List.choose id
            }
            
            model, Cmd.OfAsync.perform sendIAmAliveMessageAndGetAvailableConnections () IAmAliveSendIterationFinished
        | IAmAliveSendIterationFinished connectedEndpoints ->
            { model with Connections = connectedEndpoints }, Cmd.ofMsg <| WaitThenSend (1000, StartSendIAmAliveLoop)
        | AlivePackageReceived (msg, client) ->
            
            logger.Debug $"[AlivePackageReceived] Alive package {msg} received from {client.RemoteEndPoint}"
            
            let retranslatedByCurrentInstance = msg.RetranslationInfo.RetranslatedBy |> List.contains model.UserId
            
            if retranslatedByCurrentInstance
            then
                model, Cmd.none
            else
                logger.Debug $"[AlivePackageReceived] I am alive package {msg} not being retranslated by this app. Retranslating..."
                
                let apps =
                    if model.SecretCode = msg.SecretCode && msg.UserId <> model.UserId
                    then
                        logger.Debug $"[AlivePackageReceived] I am alive package {msg} for me. Updating connection lifetime..."
                        
                        model.ConnectedUsers
                        |> List.upsert
                               (fun o -> o.UserId = msg.UserId)
                               { AppName = msg.MessageSender; UserId = msg.UserId; ConnectedTill = DateTime.Now.AddSeconds(4) }
                               
                    else
                        model.ConnectedUsers
                { model with ConnectedUsers = apps }, Cmd.ofMsg <| RetranslateAlivePackage msg                
        | RetranslateAlivePackage msg ->
            let msg = { msg with RetranslationInfo = { msg.RetranslationInfo with RetranslatedBy = model.UserId :: msg.RetranslationInfo.RetranslatedBy } }
            model.Connections
            |> List.iter (fun conn ->
                try
                    P2PNetwork.send (EnumToValue(PackageType.Alive)) conn.Client msg
                with
                | e ->
                    logger.DebugException e $"[RetranslateAlivePackage] Failed to retranslate alive package to {conn}"
            )
            model, Cmd.none
        | SendMessage ->
            if not <| String.IsNullOrWhiteSpace(model.MessageInput)
            then
                let newMsg = {
                    DateTime = DateTime.Now
                    MessageText = model.MessageInput
                    MessageSender = model.UserName
                    SecretCode = model.SecretCode
                    UserId = model.UserId
                    RetranslationInfo = {
                        RetranslatedBy = [ model.UserId ]
                    }
                }
                
                logger.Info $"[SendMessage] Sending new message {newMsg} to all connected clients %A{model.Connections}..."
                
                model.Connections
                |> List.iter (fun ce ->
                    try
                        P2PNetwork.send (EnumToValue(PackageType.Message)) ce.Client newMsg
                    with
                    | e ->
                        logger.InfoException e $"[SendMessage] Failed to send message {msg} package to connection {ce}"
                )
                { model with MessageInput = ""; },  Cmd.ofMsg <| AppendLocalMessage { Message = newMsg; IsMe = true }
            else
                model, Cmd.none
        | RemoteChatMessageReceived (msg, client) ->
            
            logger.Debug $"[RemoteChatMessageReceived] Chat message {msg} package received by {client.RemoteEndPoint}"
            
            let retranslatedByCurrentInstance = msg.RetranslationInfo.RetranslatedBy |> List.contains model.UserId
            let receivedMessageAlreadyPresentsHere = model.MessagesListHashSet |> Set.contains (msg.GetHalfHashCode())
            
            if retranslatedByCurrentInstance || receivedMessageAlreadyPresentsHere
            then
                model, Cmd.none
            else
                logger.Debug $"[RemoteChatMessageReceived] Chat message {msg} by {client.RemoteEndPoint} not being retranslated by this app. Retranslating..."
                
                if msg.SecretCode = model.SecretCode
                then
                    logger.Info $"[RemoteChatMessageReceived] Chat message {msg} for me by {client.RemoteEndPoint}. Append message to messages list..."
                        
                    let cmds = [
                        Cmd.ofMsg <| AppendLocalMessage { Message = msg; IsMe = false }
                        Cmd.ofMsg <| RetranslateChatMessage msg
                    ]
                    model, Cmd.batch cmds
                else
                    model, Cmd.ofMsg <| RetranslateChatMessage msg
        | RetranslateChatMessage msg ->
            let msg = { msg with RetranslationInfo = { msg.RetranslationInfo with RetranslatedBy = model.UserId :: msg.RetranslationInfo.RetranslatedBy } }
            model.Connections
            |> List.iter (fun conn ->
                try
                    P2PNetwork.send (EnumToValue(PackageType.Message)) conn.Client msg
                with
                | e ->
                    logger.DebugException e $"[RetranslateChatMessage] Failed to retranslate chat message package to {conn}"
            )
            model, Cmd.none
        | StartCleanDeadAppsLoop ->
            let clearDeadConnectedApps _ = async {
                
                let aliveConnections, deadConnections =
                    model.ConnectedUsers
                    |> List.partition (fun o -> o.ConnectedTill > DateTime.Now)
                
                logger.Debug $"[StartCleanDeadAppsLoop] Cleaning dead users {deadConnections}... Only {aliveConnections} stands alive"
                  
                return aliveConnections
            }
            
            model, Cmd.OfAsync.perform clearDeadConnectedApps () DeadAppsCleanIterationFinished
        | DeadAppsCleanIterationFinished apps ->
            { model with ConnectedUsers = apps }, Cmd.ofMsg <| WaitThenSend (2000, StartCleanDeadAppsLoop)
        | AppendLocalMessage m ->
            let msg = WaitThenSend (0, ScrollChatToEnd) // Delay 0 because rendering pipeline can't update vertical offset correctly without async message between frames
            let verticalOffset = Double.MaxValue // Vertical offset must be different between frames because of caching inside Avalonia.FuncUI library
            let model = {
                model with
                    MessagesList = model.MessagesList @ [m]
                    MessagesListHashSet = model.MessagesListHashSet |> Set.add (m.Message.GetHalfHashCode())
                    ChatScrollViewOffset = verticalOffset
            }
            model, Cmd.ofMsg msg
        | ScrollChatToEnd ->
            { model with ChatScrollViewOffset = Double.PositiveInfinity }, Cmd.none
        | TextChanged t ->
            { model with MessageInput = t }, Cmd.none
        | ToggleSettingsVisibility ->
            { model with SettingsVisible = not model.SettingsVisible }, Cmd.none
        | SecretCodeChanged secretCode ->            
            { model with SecretCode = secretCode }, Cmd.none
        | UserNameChanged userName ->
            let validationErrors = []
            
            let validationErrors = validationErrors |> List.appendIf (userName.Length < 5) "Слишком маленькое имя пользователя"
            let validationErrors = validationErrors |> List.appendIf (userName.Length > 64) "Слишком большое имя пользователя"
            
            if validationErrors.Length > 0
            then { model with UserNameValidationErrors = validationErrors }, Cmd.none
            else { model with UserName = userName; UserNameValidationErrors = validationErrors }, Cmd.none
        
        | SaveUserSettingsToConfig ->
            let userSettings = {
                UserId = model.UserId
                Name = model.UserName
                SecretCode = model.SecretCode
            }
            
            try
                model.Args.ConfigStorage.SaveUserSettings userSettings
                logger.Info $"User settings are changed and saved: {userSettings}"
            with
            | e ->
                logger.ErrorException e $"Can't update user settings: {userSettings}"
            
            model, Cmd.none

    let view model dispatch =
        Grid.create [
            Grid.classes [ "main-container" ]
            Grid.columnDefinitions "10, 180, 5, 6*, 5, Auto, 10"
            Grid.rowDefinitions "10, *, 5, Auto, 5 Auto, 10"
            Grid.children [
                ScrollViewer.create [
                    ScrollViewer.column 1
                    ScrollViewer.row 1
                    ScrollViewer.horizontalScrollBarVisibility ScrollBarVisibility.Auto
                    ScrollViewer.content (
                        StackPanel.create [
                            StackPanel.spacing 10
                            StackPanel.orientation Orientation.Vertical
                            StackPanel.children (
                                let onlineIndicator =
                                    Path.create [
                                        Shapes.Path.classes [ "online-indicator" ]
                                        Shapes.Path.data "M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2Z"
                                    ]
                                [
                                    TextBlock.create [
                                        TextBlock.classes [ "label-connections" ]
                                        TextBlock.text $"В сети:"
                                    ]
                                    StackPanel.create [
                                        StackPanel.spacing 5
                                        StackPanel.orientation Orientation.Horizontal
                                        StackPanel.verticalAlignment VerticalAlignment.Center
                                        StackPanel.children [
                                            onlineIndicator
                                            TextBlock.create [
                                                TextBlock.classes [ "connection"; "local" ]
                                                TextBlock.text model.UserName
                                            ]
                                        ]
                                    ]
                                ]
                                @ (model.ConnectedUsers
                                    |> List.map (fun app ->
                                        StackPanel.create [
                                            StackPanel.spacing 5
                                            StackPanel.orientation Orientation.Vertical
                                            StackPanel.children [
                                                StackPanel.create [
                                                    StackPanel.spacing 5
                                                    StackPanel.orientation Orientation.Horizontal
                                                    StackPanel.verticalAlignment VerticalAlignment.Center
                                                    StackPanel.children [
                                                        onlineIndicator
                                                        TextBlock.create [
                                                            TextBlock.classes [ "connection"; "remote" ]
                                                            TextBlock.text app.AppName
                                                        ]
                                                    ]
                                                ]
                                            ]
                                        ]
                                    )
                                )
                            )
                        ]
                    )
                ]
                
                Grid.create [
                    Grid.column 1
                    Grid.row 3
                    Grid.background "#fafafa"
                    Grid.columnDefinitions "5, Auto, 5, *, 5"
                    Grid.rowDefinitions "5, Auto, 5 Auto, 5, Auto, 5"
                    Grid.isVisible model.SettingsVisible
                    Grid.children [
                        TextBlock.create [
                            TextBlock.column 1
                            TextBlock.columnSpan 3
                            TextBlock.row 1
                            TextBlock.verticalAlignment VerticalAlignment.Center
                            TextBlock.isVisible (model.UserNameValidationErrors.Length > 0)
                            TextBlock.text <| String.Join("\n", (model.UserNameValidationErrors |> Array.ofList))
                            TextBlock.textWrapping TextWrapping.Wrap
                            TextBlock.foreground "Red"
                        ]
                        TextBlock.create [
                            TextBlock.column 1
                            TextBlock.row 3
                            TextBlock.verticalAlignment VerticalAlignment.Center
                            TextBlock.text $"Код:"
                        ]
                        NumericUpDown.create [
                            NumericUpDown.column 3
                            NumericUpDown.row 3
                            NumericUpDown.allowSpin false
                            NumericUpDown.showButtonSpinner false
                            NumericUpDown.minimum 100000
                            NumericUpDown.maximum 999999
                            NumericUpDown.value model.SecretCode
                            NumericUpDown.onTextChanged (
                                fun o ->
                                    let parseSuccess, parsedValue = Int32.TryParse(o)
                                    if parseSuccess
                                    then parsedValue |> SecretCodeChanged |> dispatch
                            )
                            NumericUpDown.onLostFocus (
                                fun _ ->
                                    SaveUserSettingsToConfig |> dispatch
                            )
                        ]
                        
                        TextBlock.create [
                            TextBlock.column 1
                            TextBlock.row 5
                            TextBlock.verticalAlignment VerticalAlignment.Center
                            TextBlock.text $"Имя:"
                        ]
                        TextBox.create [
                            TextBox.column 3
                            TextBox.row 5
                            TextBox.textWrapping TextWrapping.NoWrap
                            TextBox.maxLength 32
                            TextBox.text model.UserName
                            TextBox.onTextChanged(fun text -> UserNameChanged text |> dispatch)
                            TextBox.onLostFocus (fun _ -> SaveUserSettingsToConfig |> dispatch)
                            if model.UserNameValidationErrors.Length > 0
                            then
                                TextBox.classes ["invalid"]
                        ]
                    ]
                ]
                
                Grid.create [
                    Grid.column 1
                    Grid.row 5
                    Grid.children [
                        Button.create [
                            Button.column 0
                            Button.classes [ "button-show-settings" ]
                            Button.content (
                                Path.create [
                                    Shapes.Path.classes [ "button-show-settings-icon" ]
                                    Shapes.Path.data "M12,8A4,4 0 0,1 16,12A4,4 0 0,1 12,16A4,4 0 0,1 8,12A4,4 0 0,1 12,8M12,10A2,2 0 0,0 10,12A2,2 0 0,0 12,14A2,2 0 0,0 14,12A2,2 0 0,0 12,10M10,22C9.75,22 9.54,21.82 9.5,21.58L9.13,18.93C8.5,18.68 7.96,18.34 7.44,17.94L4.95,18.95C4.73,19.03 4.46,18.95 4.34,18.73L2.34,15.27C2.21,15.05 2.27,14.78 2.46,14.63L4.57,12.97L4.5,12L4.57,11L2.46,9.37C2.27,9.22 2.21,8.95 2.34,8.73L4.34,5.27C4.46,5.05 4.73,4.96 4.95,5.05L7.44,6.05C7.96,5.66 8.5,5.32 9.13,5.07L9.5,2.42C9.54,2.18 9.75,2 10,2H14C14.25,2 14.46,2.18 14.5,2.42L14.87,5.07C15.5,5.32 16.04,5.66 16.56,6.05L19.05,5.05C19.27,4.96 19.54,5.05 19.66,5.27L21.66,8.73C21.79,8.95 21.73,9.22 21.54,9.37L19.43,11L19.5,12L19.43,13L21.54,14.63C21.73,14.78 21.79,15.05 21.66,15.27L19.66,18.73C19.54,18.95 19.27,19.04 19.05,18.95L16.56,17.95C16.04,18.34 15.5,18.68 14.87,18.93L14.5,21.58C14.46,21.82 14.25,22 14,22H10M11.25,4L10.88,6.61C9.68,6.86 8.62,7.5 7.85,8.39L5.44,7.35L4.69,8.65L6.8,10.2C6.4,11.37 6.4,12.64 6.8,13.8L4.68,15.36L5.43,16.66L7.86,15.62C8.63,16.5 9.68,17.14 10.87,17.38L11.24,20H12.76L13.13,17.39C14.32,17.14 15.37,16.5 16.14,15.62L18.57,16.66L19.32,15.36L17.2,13.81C17.6,12.64 17.6,11.37 17.2,10.2L19.31,8.65L18.56,7.35L16.15,8.39C15.38,7.5 14.32,6.86 13.12,6.62L12.75,4H11.25Z"
                                ]
                            )
                            Button.onClick (fun _ -> ToggleSettingsVisibility |> dispatch)
                        ]
                    ]
                ]
                
                Border.create [
                    Border.column 3
                    Border.columnSpan 3
                    Border.row 1
                    Border.rowSpan 3
                    Border.classes [ "border-chat" ]
                    Border.child (
                        ScrollViewer.create [
                            ScrollViewer.content (
                                ItemsRepeater.create [
                                    ItemsRepeater.margin 10
                                    ItemsRepeater.itemTemplate (
                                        let dt m =
                                            Border.create [
                                                let classes = [ "border-chat-msg" ] @ (if m.IsMe then [ "me" ] else [])
                                                Border.classes classes
                                                Border.child (
                                                    StackPanel.create [
                                                        StackPanel.children [
                                                            TextBox.create [
                                                                let classes = [ "chat-msg-text" ] @ if m.IsMe then [ "me" ] else [ ]
                                                                TextBlock.classes classes
                                                                TextBox.text $"{m.Message.MessageText}"
                                                            ]
                                                            TextBlock.create [
                                                                let classes = [ "chat-msg-sender" ] @ if m.IsMe then [ "me" ] else [ ]
                                                                TextBlock.classes classes
                                                                TextBlock.text $"{m.Message.MessageSender}        {m.Message.DateTime.ToShortDateString()} {m.Message.DateTime.ToShortTimeString()}"
                                                            ]
                                                        ]
                                                    ]
                                                )
                                            ]
                                        DataTemplateView<LocalMessage>.create dt
                                    )
                                    
                                    ItemsRepeater.dataItems model.MessagesList
                                ]
                            )
                            ScrollViewer.offset <| Vector(Double.NegativeInfinity, model.ChatScrollViewOffset)
                        ]
                    )
                ]
                
                TextBox.create [
                    TextBox.classes [ "textbox-msg-input" ]
                    TextBox.column 3
                    TextBox.row 5
                    TextBox.watermark "Введите сообщение..."
                    TextBox.text model.MessageInput
                    TextBox.onKeyDown (fun o ->
                        if o.Key = Key.Enter
                           && o.KeyModifiers = KeyModifiers.None
                        then
                            dispatch SendMessage
                            o.Handled <- true
                    )
                    TextBox.onTextChanged(fun text -> dispatch <| TextChanged text)
                ]
                
                TextBlock.create [
                    TextBlock.column 3
                    TextBlock.row 5
                    TextBlock.fontSize 10
                    TextBlock.margin (6, 2)
                    TextBlock.foreground "Red"
                    TextBlock.verticalAlignment VerticalAlignment.Bottom
                    TextBlock.horizontalAlignment HorizontalAlignment.Right
                    TextBlock.isHitTestVisible false
                    TextBlock.opacity <| (float model.MessageInput.Length) / (float model.MaxChatMessageLength)
                    TextBlock.text <| (model.MessageInput.Length.ToString()) + $" / {model.MaxChatMessageLength}"
                ]
                
                Button.create [
                    Button.classes [ "button-msg-send" ]
                    Button.column 5
                    Button.row 5
                    Button.content (
                        Path.create [
                            Shapes.Path.classes [ "button-msg-send-icon" ]
                            Shapes.Path.data "M2,21L23,12L2,3V10L17,12L2,14V21Z"
                        ]
                    )
                    Button.isEnabled (not <| String.IsNullOrWhiteSpace(model.MessageInput))
                    Button.onClick (fun _ -> dispatch SendMessage)
                ]
            ]
        ]