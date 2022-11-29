namespace Skillaz.SecureChat

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
open Skillaz.SecureChat.AppSettings
open Skillaz.SecureChat.Message

module Chat =
    
    type PackageType =
        | Alive = 201
        | Message = 202
    
    type LocalMessage = {
        Message: ChatMessage
        IsMe: bool
    }
    
    type ConnectedEndpoint = {
        UniqueConnectionMark: string
        EndPoint: EndPoint
        Client: Socket
    }
    
    type ConnectedApp = {
        AppName: string
        AppMark: string
        ConnectedTill: DateTime
    }
    
    type Model = {
        AppSettings: AppSettingsJson.Root
        CurrentUserName: string
        CurrentAppMark: string
        TcpListener: Socket
        UnixSocketFolder: string
        UnixSocketFilePath: string
        UnixSocketListener: Socket
        MessageInput: string
        MessagesList: LocalMessage list
        SecretCodeVisible: bool
        Connections: ConnectedEndpoint list
        ConnectedApps: ConnectedApp list
        SettingsVisible: bool
    }
        
    type Msg =
        | StartLaunchListenRemoteConnectionsLoop
        | LaunchListenRemoteConnectionsIterationFinished of Result<unit, unit>
        | StartConnectToRemotePeersLoop
        | ConnectToRemotePeersIterationFinished of ConnectedEndpoint list
        | TryConnectToLocalPeers
        | PeersConnected of ConnectedEndpoint list
        | ClientConnected of Socket
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
        | ToggleSecretCodeVisibility
        | ToggleSettingsVisibility
        
    let connectionsSubscription listener dispatch =
        let handle socket =
            Msg.ClientConnected socket |> dispatch
        P2PNetwork.listenSocket listener handle |> Async.Start
        
    let packagesSubscription client dispatch =
        let handleSocketPackage dispatch packageType bytes read socket  =
            let json = Encoding.UTF8.GetString(bytes, 0, read)
            let pt = EnumOfValue(packageType)
            match pt with
            | PackageType.Alive ->
                let msg = JsonSerializer.Deserialize<AliveMessage>(json)
                Msg.AlivePackageReceived (msg, socket) |> dispatch
            | PackageType.Message ->
                let msg = JsonSerializer.Deserialize<ChatMessage>(json)
                Msg.RemoteChatMessageReceived (msg, socket) |> dispatch
            
        let handleSocket = handleSocketPackage dispatch
        
        let rec handlePackages client dispatch = async {
            try
                do! P2PNetwork.listenAndHandleSocketPackage client handleSocket
                do! handlePackages client dispatch
            with
            | e ->
                client.Dispose()
                Logger.warnLogger.Log("handleSocketPackage", $"{e.ToString()}")
        }
        
        handlePackages client dispatch |> Async.Start
    
    let init (appSettings: AppSettingsJson.Root) =
        
        let appMark =
            let appMarkFilePath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "/ssc/", "appmark.ini")
            if File.Exists(appMarkFilePath)
            then File.ReadAllText(appMarkFilePath)
            else
                let mark = Guid.NewGuid().ToString()
                Directory.CreateDirectory(Path.GetDirectoryName(appMarkFilePath)) |> ignore
                File.WriteAllText(appMarkFilePath, mark)
                mark
        
        let unixSocketsFolder =
            if OperatingSystem.IsLinux() || OperatingSystem.IsMacOS()
            then "/tmp/ssc/"
            else Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "/ssc/")
        
        let unixSocketFilePath =
            Path.Join(unixSocketsFolder, $"{Convert.ToBase64String(Encoding.UTF8.GetBytes(appMark))}.socket")
        
        let model = {
            AppSettings = appSettings
            CurrentUserName = Environment.UserName
            CurrentAppMark = appMark
            TcpListener = Tcp.listener
            UnixSocketListener = UnixSocket.listener unixSocketFilePath
            UnixSocketFolder = unixSocketsFolder
            UnixSocketFilePath = unixSocketFilePath
            Connections = []
            ConnectedApps = []
            MessageInput = ""
            MessagesList = []
            SecretCodeVisible = false
            SettingsVisible = false;
        }
        
        model.UnixSocketListener.Listen()
        
        let cmd = Cmd.batch [
            Cmd.ofMsg Msg.StartLaunchListenRemoteConnectionsLoop
            Cmd.ofMsg Msg.StartCleanDeadAppsLoop
            Cmd.ofMsg Msg.StartSendIAmAliveLoop
            Cmd.ofMsg Msg.StartConnectToRemotePeersLoop
            Cmd.ofMsg Msg.TryConnectToLocalPeers
            Cmd.ofSub <| connectionsSubscription model.UnixSocketListener
        ]
        
        model, cmd
    
    let update msg model =
        match msg with
        | StartLaunchListenRemoteConnectionsLoop ->
            let tryListenRemoteConnections _ = async {
                do! Task.Delay(TimeSpan.FromSeconds(2)) |> Async.AwaitTask
                try
                    Tcp.tryBindTo IPAddress.Any model.AppSettings.ListenerPort model.TcpListener
                    model.TcpListener.Listen()
                    return Result.Ok ()
                with
                | e ->
                    Logger.warnLogger.Log(nameof StartLaunchListenRemoteConnectionsLoop, e.ToString())
                    return Result.Error ()
            }
            model, Cmd.OfAsync.perform tryListenRemoteConnections () LaunchListenRemoteConnectionsIterationFinished
        | LaunchListenRemoteConnectionsIterationFinished tcpListener ->
            match tcpListener with
            | Result.Ok _ -> model, Cmd.ofSub <| connectionsSubscription model.TcpListener
            | Result.Error _ -> model, Cmd.ofMsg StartLaunchListenRemoteConnectionsLoop
        | TryConnectToLocalPeers ->
            let connectToLocalPeers _ = async {
                let existingUnixSockets =
                    Directory.GetFiles(model.UnixSocketFolder)
                    |> Array.where (fun o -> o <> model.UnixSocketFilePath)
                return
                    existingUnixSockets
                    |> Array.where (fun socket -> model.Connections |> List.exists (fun x -> x.UniqueConnectionMark = socket) |> not)
                    |> Array.Parallel.map (fun socket ->
                        try
                            let unixSocketClient = UnixSocket.client socket
                            let connectionEndpoint = {
                                UniqueConnectionMark = socket
                                EndPoint = unixSocketClient.RemoteEndPoint
                                Client = unixSocketClient
                            }
                            Some connectionEndpoint
                        with
                        | e -> 
                            Logger.warnLogger.Log(nameof TryConnectToLocalPeers, "Socket: {0} Ex: {1}", socket, e.ToString())
                            None
                    )
                    |> Array.choose id
                    |> List.ofArray
            }
            model, Cmd.OfAsync.perform connectToLocalPeers () PeersConnected
        | StartConnectToRemotePeersLoop ->
            let connectToRemotePeers _ = async {
                do! Task.Delay(TimeSpan.FromSeconds(2)) |> Async.AwaitTask
                return
                    model.AppSettings.KnownPeers
                    |> Array.map IPEndPoint.Parse
                    |> Array.where (fun ep -> model.Connections |> List.exists (fun x -> x.UniqueConnectionMark = ep.ToString()) |> not)
                    |> Array.Parallel.map (fun ep ->
                        try
                            let socket = Tcp.client ep.Address ep.Port model.AppSettings.ClientPort
                            let connectionEndpoint = {
                                UniqueConnectionMark = ep.ToString()
                                EndPoint = ep
                                Client = socket
                            }
                            Some connectionEndpoint
                        with
                        | e -> 
                            Logger.warnLogger.Log(nameof StartConnectToRemotePeersLoop, "IP: {0} Ex: {1}", ep.ToString(), e.ToString())
                            None
                    )
                    |> Array.choose id
                    |> List.ofArray
            }
            
            model, Cmd.OfAsync.perform connectToRemotePeers () ConnectToRemotePeersIterationFinished
        | ConnectToRemotePeersIterationFinished peers ->
            let msgs = Cmd.batch [
                Cmd.ofMsg <| PeersConnected peers
                Cmd.ofMsg StartConnectToRemotePeersLoop
            ]
            model, msgs
        | PeersConnected newlyConnected ->
            let cmds =
                newlyConnected
                |> List.map (fun c ->
                    Cmd.ofSub <| packagesSubscription c.Client
                )
            
            { model with Connections = model.Connections @ newlyConnected }, Cmd.batch cmds
        | ClientConnected socket ->
            let connectedEndpoint = {
                UniqueConnectionMark = socket.RemoteEndPoint.ToString()
                EndPoint = socket.RemoteEndPoint
                Client = socket
            }
            
            { model with Connections = connectedEndpoint :: model.Connections }, Cmd.ofSub <| packagesSubscription socket
        | StartSendIAmAliveLoop ->
            let sendIAmAliveMessageAndGetAvailableConnections _ = async {
                do! Task.Delay(TimeSpan.FromSeconds(1)) |> Async.AwaitTask
                
                return
                    model.Connections
                    |> Array.ofList
                    |> Array.Parallel.map (fun t ->
                        try
                            let msg = {
                                MessageSender = model.CurrentUserName
                                AppMark = model.CurrentAppMark
                                SecretCode = model.AppSettings.SecretCode
                                RetranslationInfo = {
                                    RetranslatedBy = [ model.CurrentAppMark ]
                                }
                            }
                            P2PNetwork.send (EnumToValue(PackageType.Alive)) t.Client msg
                            Some t
                        with
                        | e ->
                            Logger.warnLogger.Log(nameof StartSendIAmAliveLoop, e.ToString())
                            None
                    )
                    |> List.ofArray
                    |> List.choose id
            }
            
            model, Cmd.OfAsync.perform sendIAmAliveMessageAndGetAvailableConnections () IAmAliveSendIterationFinished
        | IAmAliveSendIterationFinished connectedEndpoints ->
            { model with Connections = connectedEndpoints }, Cmd.ofMsg StartSendIAmAliveLoop
        | AlivePackageReceived (msg, client) ->
            match msg.RetranslationInfo.RetranslatedBy |> List.contains model.CurrentAppMark with
            | false ->
                let apps =
                    match model.AppSettings.SecretCode = msg.SecretCode && msg.AppMark <> model.CurrentAppMark with
                    | true ->
                        model.ConnectedApps
                        |> List.upsert
                               (fun o -> o.AppMark = msg.AppMark)
                               { AppName = msg.MessageSender; AppMark = msg.AppMark; ConnectedTill = DateTime.Now.AddSeconds(4) }
                    | false -> model.ConnectedApps
            
                { model with ConnectedApps = apps }, Cmd.ofMsg <| RetranslateAlivePackage msg
            | true ->
                model, Cmd.none
        | RetranslateAlivePackage msg ->
            let msg = { msg with RetranslationInfo = { msg.RetranslationInfo with RetranslatedBy = model.CurrentAppMark :: msg.RetranslationInfo.RetranslatedBy } }
            model.Connections
            |> List.iter (fun conn ->
                P2PNetwork.send (EnumToValue(PackageType.Alive)) conn.Client msg)
            model, Cmd.none
        | SendMessage ->
            if not <| String.IsNullOrWhiteSpace(model.MessageInput)
            then
                let newMsg = {
                    DateTime = DateTime.Now
                    MessageText = model.MessageInput
                    MessageSender = model.CurrentUserName
                    SecretCode = model.AppSettings.SecretCode
                    AppMark = model.CurrentAppMark
                    RetranslationInfo = {
                        RetranslatedBy = [ model.CurrentAppMark ]
                    }
                }
                model.Connections
                |> List.iter (fun ce ->
                    try
                        P2PNetwork.send (EnumToValue(PackageType.Message)) ce.Client newMsg
                    with
                    | e -> ()
                )
                { model with MessageInput = ""; },  Cmd.ofMsg <| AppendLocalMessage { Message = newMsg; IsMe = true }
            else
                model, Cmd.none
        | RemoteChatMessageReceived (msg, client) ->
            match msg.RetranslationInfo.RetranslatedBy |> List.contains model.CurrentAppMark with
            | false ->
                match msg.SecretCode = model.AppSettings.SecretCode with
                | true ->
                    let cmds = [
                        Cmd.ofMsg <| AppendLocalMessage { Message = msg; IsMe = false }
                        Cmd.ofMsg <| RetranslateChatMessage msg
                    ]
                    model, Cmd.batch cmds
                | false ->
                    model, Cmd.ofMsg <| RetranslateChatMessage msg
            | true ->
                model, Cmd.none
        | RetranslateChatMessage msg ->
            let msg = { msg with RetranslationInfo = { msg.RetranslationInfo with RetranslatedBy = model.CurrentAppMark :: msg.RetranslationInfo.RetranslatedBy } }
            model.Connections
            |> List.iter (fun conn ->
                P2PNetwork.send (EnumToValue(PackageType.Message)) conn.Client msg)
            model, Cmd.none
        | StartCleanDeadAppsLoop ->
            let clearDeadConnectedApps _ = async {
                do! Task.Delay(TimeSpan.FromSeconds(2)) |> Async.AwaitTask
                
                return
                    model.ConnectedApps
                    |> List.where (fun o -> o.ConnectedTill > DateTime.Now)
            }
            
            model, Cmd.OfAsync.perform clearDeadConnectedApps () DeadAppsCleanIterationFinished
        | DeadAppsCleanIterationFinished apps ->
            { model with ConnectedApps = apps }, Cmd.ofMsg StartCleanDeadAppsLoop
        | AppendLocalMessage m ->
            { model with MessagesList = m :: model.MessagesList }, Cmd.none
        | TextChanged t ->
            { model with MessageInput = t }, Cmd.none
        | ToggleSecretCodeVisibility ->
            { model with SecretCodeVisible = not model.SecretCodeVisible }, Cmd.none
        | ToggleSettingsVisibility ->
            { model with SettingsVisible = not model.SettingsVisible }, Cmd.none

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
                                        TextBlock.text "В сети:"
                                    ]
                                    StackPanel.create [
                                        StackPanel.spacing 5
                                        StackPanel.orientation Orientation.Horizontal
                                        StackPanel.verticalAlignment VerticalAlignment.Center
                                        StackPanel.children [
                                            onlineIndicator
                                            TextBlock.create [
                                                TextBlock.classes [ "connection"; "local" ]
                                                TextBlock.text Environment.UserName
                                            ]
                                        ]
                                    ]
                                ]
                                @ (model.ConnectedApps
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
                    Grid.columnDefinitions "5, *, 5, Auto, 5"
                    Grid.rowDefinitions "5, Auto, 5"
                    Grid.isVisible model.SettingsVisible
                    Grid.children [
                        TextBlock.create [
                            TextBlock.column 1
                            TextBlock.row 1
                            TextBlock.classes [ "label-secret-code" ]
                            match model.SecretCodeVisible with
                            | true -> TextBlock.text $"Код: {model.AppSettings.SecretCode}"
                            | false -> TextBlock.text "Код: ******"
                        ]
                        Button.create [
                            Button.column 3
                            TextBlock.row 1
                            Button.classes [ "button-show-secret-code" ]
                            Button.content (
                                Path.create [
                                    Shapes.Path.classes [ "button-show-secret-code-icon" ]
                                    match model.SecretCodeVisible with
                                    | true -> Shapes.Path.data "M11.83,9L15,12.16C15,12.11 15,12.05 15,12A3,3 0 0,0 12,9C11.94,9 11.89,9 11.83,9M7.53,9.8L9.08,11.35C9.03,11.56 9,11.77 9,12A3,3 0 0,0 12,15C12.22,15 12.44,14.97 12.65,14.92L14.2,16.47C13.53,16.8 12.79,17 12,17A5,5 0 0,1 7,12C7,11.21 7.2,10.47 7.53,9.8M2,4.27L4.28,6.55L4.73,7C3.08,8.3 1.78,10 1,12C2.73,16.39 7,19.5 12,19.5C13.55,19.5 15.03,19.2 16.38,18.66L16.81,19.08L19.73,22L21,20.73L3.27,3M12,7A5,5 0 0,1 17,12C17,12.64 16.87,13.26 16.64,13.82L19.57,16.75C21.07,15.5 22.27,13.86 23,12C21.27,7.61 17,4.5 12,4.5C10.6,4.5 9.26,4.75 8,5.2L10.17,7.35C10.74,7.13 11.35,7 12,7Z"
                                    | false -> Shapes.Path.data "M12,9A3,3 0 0,0 9,12A3,3 0 0,0 12,15A3,3 0 0,0 15,12A3,3 0 0,0 12,9M12,17A5,5 0 0,1 7,12A5,5 0 0,1 12,7A5,5 0 0,1 17,12A5,5 0 0,1 12,17M12,4.5C7,4.5 2.73,7.61 1,12C2.73,16.39 7,19.5 12,19.5C17,19.5 21.27,16.39 23,12C21.27,7.61 17,4.5 12,4.5Z"
                                ]
                            )
                            Button.onClick (fun _ -> ToggleSecretCodeVisibility |> dispatch)
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
                    TextBlock.row 3
                    TextBlock.fontSize 10
                    TextBlock.margin (6, 2)
                    TextBlock.foreground "Red"
                    TextBlock.verticalAlignment VerticalAlignment.Bottom
                    TextBlock.horizontalAlignment HorizontalAlignment.Right
                    TextBlock.isHitTestVisible false
                    TextBlock.opacity <| (float model.MessageInput.Length) / 3000.0
                    TextBlock.text <| (model.MessageInput.Length.ToString()) + " / 3000"
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