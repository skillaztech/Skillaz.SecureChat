namespace chat

open System
open System.Collections.Generic
open System.Net
open System.Net.Sockets
open System.Text
open System.Text.Json
open System.Threading.Tasks
open Avalonia.Input
open Elmish
open Avalonia.FuncUI
open Avalonia.Media
open Avalonia.Controls
open Avalonia.FuncUI.DSL
open Avalonia.Layout
open chat.Message
open AppSettings

module Chat =
    
    type LocalMessage = {
        Message: ChatMessage
        IsMe: bool
    }
    
    type ConnectedEndpoint = {
        MachineName: string
        Ip: IPEndPoint
        TcpClient: TcpClient
    }
    
    type UdpPackagePayload = {
        MachineName: string
        SecretHash: string
        UdpMark: string
    }
    
    type Model = {
        AppSettings: AppSettingsJson.Root
        CurrentMachineName: string
        TcpListener: TcpListener
        UdpClient: UdpClient
        UdpMark: string
        MessageInput: string
        MessagesList: LocalMessage list
        TcpConnections: ConnectedEndpoint list
    }
        
    type Msg =
        | UdpSendPackage
        | UdpPackageReceived of byte[] * IPEndPoint
        | RemoteTcpClientConnected of TcpClient
        | HelloMessageReceived of TcpClient * string
        | RemoteChatMessageReceived of ChatMessage * TcpClient
        | TextChanged of string
        | AppendLocalMessage of LocalMessage
        | SendMessage
        | HealthCheckConnectedEndpoints
        
    let healthCheckSubscription dispatch =
        let rec tick dispatch = async {
            Msg.HealthCheckConnectedEndpoints |> dispatch
            do! Task.Delay(1000) |> Async.AwaitTask
            do! tick dispatch
        }
        tick dispatch |> Async.Start
        
    let udpSubscription listener dispatch =
        let invoke (payload:byte[]) endpoint =
            Msg.UdpPackageReceived (payload, endpoint) |> dispatch |> ignore
        P2PNetwork.listenForUdpPackage listener invoke |> Async.Start
        
    let handleTcpPackage dispatch packageType read client =
        match packageType with
        | P2PNetwork.TcpPackage.Ping -> ()
        | P2PNetwork.TcpPackage.Hello bytes ->
            let name = Encoding.UTF8.GetString(bytes, 0, read)
            Msg.HelloMessageReceived (client, name) |> dispatch
        | P2PNetwork.TcpPackage.Message bytes ->
            let json = Encoding.UTF8.GetString(bytes, 0, read)
            let msg = JsonSerializer.Deserialize<ChatMessage>(json)
            Msg.RemoteChatMessageReceived (msg, client) |> dispatch
        
    let tcpConnectionsSubscription listener dispatch =
        let handle tcp =
            Msg.RemoteTcpClientConnected tcp |> dispatch
        P2PNetwork.listenForTcpConnection listener handle |> Async.Start
        
    let tcpPackagesSubscription tcpClient dispatch =
        let handle = handleTcpPackage dispatch
        P2PNetwork.listenForTcpPackages tcpClient handle |> Async.Start
    
    let init appSettings =
        
        let model = {
            AppSettings = appSettings
            CurrentMachineName =
                if String.IsNullOrWhiteSpace(appSettings.MachineName)
                then Environment.MachineName
                else appSettings.MachineName
            TcpListener = P2PNetwork.tcpListener IPAddress.Any appSettings.ListenerPort
            UdpClient = P2PNetwork.udpClient IPAddress.Any appSettings.ListenerPort
            UdpMark = Guid.NewGuid().ToString()
            TcpConnections = []
            MessageInput = ""
            MessagesList = []
        }
        
        model.TcpListener.Start()
        
        let cmd = Cmd.batch [
            Cmd.ofSub <| healthCheckSubscription 
            Cmd.ofSub <| udpSubscription model.UdpClient
            Cmd.ofSub <| tcpConnectionsSubscription model.TcpListener
            Cmd.ofMsg <| UdpSendPackage
        ]
        
        model, cmd
    
    let update msg model =
        match msg with
        | UdpSendPackage ->
            let json = JsonSerializer.Serialize({ MachineName = model.CurrentMachineName; SecretHash = model.AppSettings.SecretHash; UdpMark = model.UdpMark })
            let payload = Encoding.UTF8.GetBytes(json)
            model.UdpClient.Send(payload, payload.Length, IPEndPoint(IPAddress.Broadcast, model.AppSettings.ListenerPort)) |> ignore
            model, Cmd.none
        | UdpPackageReceived (payload, ip) ->
            let payload = Encoding.UTF8.GetString(payload)
            let package = JsonSerializer.Deserialize<UdpPackagePayload>(payload)
            if package.SecretHash = model.AppSettings.SecretHash && package.UdpMark <> model.UdpMark
            then
                if model.TcpConnections |> List.exists (fun o -> o.Ip = ip) |> not
                then
                    let tcpClient = P2PNetwork.tcpClient ip.Address ip.Port model.AppSettings.ClientPort
                    P2PNetwork.tcpSendHello tcpClient Environment.MachineName
                    
                    let connectionEndpoint = {
                        MachineName = package.MachineName
                        Ip = ip
                        TcpClient = tcpClient
                    }
                    
                    { model with TcpConnections = connectionEndpoint :: model.TcpConnections }, Cmd.ofSub <| tcpPackagesSubscription tcpClient
                else model, Cmd.none
            else model, Cmd.none
        | RemoteTcpClientConnected tcpClient ->
            match tcpClient.Client.RemoteEndPoint with
            | :? IPEndPoint as rIp ->
                let connectedEndpoint = {
                    MachineName = rIp.ToString()
                    Ip = rIp
                    TcpClient = tcpClient
                }
                { model with TcpConnections = connectedEndpoint :: model.TcpConnections }, Cmd.ofSub <| tcpPackagesSubscription tcpClient
            | _ -> model, Cmd.none
        | HelloMessageReceived (tcpClient, machineName) ->
            match tcpClient.Client.RemoteEndPoint with
            | :? IPEndPoint as ip ->
                let connections =
                    model.TcpConnections
                    |> List.map (fun conn ->
                        if conn.Ip = ip then
                            { conn with MachineName = machineName }
                        else conn
                    )
                { model with TcpConnections = connections }, Cmd.none
            | _ -> model, Cmd.none
        | RemoteChatMessageReceived (m, client) ->
            let isMe =
                match client.Client.LocalEndPoint, client.Client.RemoteEndPoint with
                | (:? IPEndPoint as local), (:? IPEndPoint as remote) -> local.Address = remote.Address
                | _ -> false
            match isMe with
            | true -> model, Cmd.none
            | false -> model, Cmd.ofMsg <| AppendLocalMessage { Message = m; IsMe = isMe }
        | SendMessage ->                
            let newMsg = {
                Sender = model.CurrentMachineName
                DateTime = DateTime.Now
                MessageText = model.MessageInput
            }
            model.TcpConnections
            |> List.iter (fun ce ->
                P2PNetwork.tcpSendAsJson ce.TcpClient newMsg
            )
            { model with MessageInput = "";  },  Cmd.ofMsg <| AppendLocalMessage { Message = newMsg; IsMe = true }
        | HealthCheckConnectedEndpoints ->
            let successfullyPingedEndpoints, unsuccessfullyPingedEndpoints =
                model.TcpConnections
                |> List.partition (fun ce ->
                    try
                        P2PNetwork.tcpSendPing ce.TcpClient
                        true
                    with
                    | _ ->
                        false
                )
                
            unsuccessfullyPingedEndpoints
            |> List.iter (fun ep ->
                ep.TcpClient.Dispose()
            )
            
            { model with TcpConnections = successfullyPingedEndpoints; }, Cmd.none
        | AppendLocalMessage m ->
            { model with MessagesList = m :: model.MessagesList }, Cmd.none
        | TextChanged t ->
            { model with MessageInput = t }, Cmd.none

    let view model dispatch =
        Grid.create [
            Grid.classes [ "main-container" ]
            Grid.columnDefinitions "10, 150, 5, 6*, 5, Auto, 10"
            Grid.rowDefinitions "10, *, 5, Auto, 10"
            Grid.children [
                Border.create [
                    Border.column 1
                    Border.row 1
                    Border.rowSpan 3
                    Border.classes [ "border-connections" ]
                    Border.child (
                        ScrollViewer.create [
                            ScrollViewer.column 1
                            ScrollViewer.row 1
                            ScrollViewer.rowSpan 3
                            ScrollViewer.margin 10
                            ScrollViewer.content (
                                StackPanel.create [
                                    StackPanel.spacing 10
                                    StackPanel.orientation Orientation.Vertical
                                    StackPanel.children (
                                        [
                                            TextBlock.create [
                                                TextBlock.classes [ "label-connections" ]
                                                TextBlock.fontSize 11
                                                TextBlock.margin (0, 0, 0, 5)
                                                TextBlock.fontStyle FontStyle.Italic
                                                TextBlock.text "В сети: "
                                            ]
                                            TextBlock.create [
                                                TextBlock.classes [ "connection"; "local" ]
                                                TextBlock.fontSize 13
                                                TextBlock.text model.CurrentMachineName
                                            ]
                                        ]
                                        @ (model.TcpConnections
                                            |> List.map (fun connection ->
                                                TextBlock.create [
                                                    TextBlock.classes [ "connection"; "remote" ]
                                                    TextBlock.fontSize 13
                                                    TextBlock.text <| connection.MachineName
                                                ])
                                        )
                                    )
                                ]
                            )
                        ]
                    )
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
                                                if m.IsMe
                                                then
                                                    Border.margin (50, 2, 2, 2)
                                                else
                                                    Border.margin (2, 2, 50, 2)
                                                Border.child (
                                                    StackPanel.create [
                                                        StackPanel.children [
                                                            TextBlock.create [
                                                                TextBlock.classes [ "chat-msg-sender" ]
                                                                TextBlock.focusable false
                                                                TextBlock.textWrapping TextWrapping.Wrap
                                                                TextBlock.fontSize 10
                                                                TextBlock.margin 8
                                                                TextBlock.fontStyle FontStyle.Italic
                                                                TextBlock.verticalAlignment VerticalAlignment.Center
                                                                if m.IsMe
                                                                then TextBlock.textAlignment TextAlignment.Right
                                                                TextBlock.text $"{m.Message.Sender} - {m.Message.DateTime.ToShortTimeString()}"
                                                            ]
                                                            TextBox.create [
                                                                TextBlock.classes [ "chat-msg-text" ]
                                                                TextBox.focusable false
                                                                TextBox.textWrapping TextWrapping.Wrap
                                                                TextBox.isReadOnly true
                                                                TextBox.fontSize 12
                                                                TextBox.margin 4
                                                                TextBox.verticalContentAlignment VerticalAlignment.Center
                                                                TextBox.verticalAlignment VerticalAlignment.Center
                                                                if m.IsMe
                                                                then TextBox.textAlignment TextAlignment.Right
                                                                TextBox.text $"{m.Message.MessageText}"
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
                    TextBox.row 3
                    TextBox.watermark "Введите сообщение..."
                    TextBox.acceptsReturn true
                    TextBox.textWrapping TextWrapping.Wrap
                    TextBox.text model.MessageInput
                    TextBox.onKeyDown (fun o -> if o.Key = Key.Enter && o.KeyModifiers = KeyModifiers.None then dispatch SendMessage; o.Handled <- true)
                    TextBox.onTextChanged(fun text -> dispatch <| TextChanged text)
                ]
                Button.create [
                    Button.classes [ "button-msg-send" ]
                    Button.column 5
                    Button.row 3
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