namespace chat

open System
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
        Message: Message
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
    }
    
    type Model = {
        AppSettings: AppSettingsJson.Root
        TcpListener: TcpListener
        UdpClient: UdpClient
        MessageInput: string
        MessagesList: LocalMessage list
        ConnectedEndpoints: ConnectedEndpoint list
    }
        
    type Msg =
        | UdpSendPackage
        | UdpPackageReceived of byte[] * IPEndPoint
        | RemoteMessageReceived of Message * TcpClient
        | TextChanged of string
        | AppendLocalMessage of LocalMessage
        | SendMessage
        | HealthCheckConnectedEndpoints
        
    let healthCheckSubscription dispatch =
        let rec tick dispatch = async {
            Msg.HealthCheckConnectedEndpoints |> dispatch
            do! Task.Delay(2000) |> Async.AwaitTask
            do! tick dispatch
        }
        tick dispatch |> Async.Start
        
    let udpSubscription listener dispatch =
        let invoke (payload:byte[]) endpoint =
            Msg.UdpPackageReceived (payload, endpoint) |> dispatch |> ignore
        P2PNetwork.listenForUdpPackage listener invoke |> Async.Start
        
    let tcpSubscription listener dispatch =
        let invoke msgType read client =
            match msgType with
            | P2PNetwork.TcpPackage.Ping -> ()
            | P2PNetwork.TcpPackage.Message bytes ->
                let json = Encoding.UTF8.GetString(bytes, 0, read)
                let msg = JsonSerializer.Deserialize<Message>(json)
                Msg.RemoteMessageReceived (msg, client) |> dispatch |> ignore
        P2PNetwork.listenForTcpPackage listener invoke |> Async.Start
    
    let init appSettings =
        
        let model = {
            AppSettings = appSettings
            TcpListener = P2PNetwork.tcpListener IPAddress.Any appSettings.ListenerPort
            UdpClient = P2PNetwork.udpClient IPAddress.Any appSettings.ListenerPort
            ConnectedEndpoints = []
            MessageInput = ""
            MessagesList = []
        }
        
        model.TcpListener.Start()
        
        let cmd = Cmd.batch [
            Cmd.ofSub <| healthCheckSubscription 
            Cmd.ofSub <| udpSubscription model.UdpClient
            Cmd.ofSub <| tcpSubscription model.TcpListener
            Cmd.ofMsg <| UdpSendPackage
        ]
        
        model, cmd
    
    let update msg model =
        match msg with
        | UdpSendPackage ->
            let json = JsonSerializer.Serialize({ MachineName = model.AppSettings.MachineName; SecretHash = model.AppSettings.SecretHash })
            let payload = Encoding.UTF8.GetBytes(json)
            model.UdpClient.Send(payload, payload.Length, IPEndPoint(IPAddress.Broadcast, model.AppSettings.ListenerPort)) |> ignore
            model, Cmd.none
        | UdpPackageReceived (payload, ip) ->
            let payload = Encoding.UTF8.GetString(payload)
            let package = JsonSerializer.Deserialize<UdpPackagePayload>(payload)
            if package.SecretHash = model.AppSettings.SecretHash
            then
                if model.ConnectedEndpoints |> List.exists (fun o -> o.Ip = ip) |> not
                then
                    let connectionEndpoint = {
                        MachineName = package.MachineName
                        Ip = ip
                        TcpClient = P2PNetwork.tcpClient ip.Address ip.Port model.AppSettings.ClientPort
                    }
                    { model with ConnectedEndpoints = connectionEndpoint :: model.ConnectedEndpoints }, Cmd.ofMsg UdpSendPackage
                else model, Cmd.none
            else model, Cmd.none
        | RemoteMessageReceived (m, client) ->
            let isMe =
                match client.Client.LocalEndPoint, client.Client.RemoteEndPoint with
                | (:? IPEndPoint as local), (:? IPEndPoint as remote) -> local.Address = remote.Address
                | _ -> false
            match isMe with
            | true -> model, Cmd.none
            | false -> model, Cmd.ofMsg <| AppendLocalMessage { Message = m; IsMe = isMe }
        | SendMessage ->
            let newMsg = {
                Sender = model.AppSettings.MachineName
                DateTime = DateTime.Now
                MessageText = model.MessageInput
            }
            model.ConnectedEndpoints
            |> List.iter (fun ce ->
                P2PNetwork.tcpSendAsJson ce.TcpClient newMsg
            )
            { model with MessageInput = "";  },  Cmd.ofMsg <| AppendLocalMessage { Message = newMsg; IsMe = true }
        | HealthCheckConnectedEndpoints ->
            let successfullyPingedEndpoints =
                model.ConnectedEndpoints
                |> List.where (fun ce ->
                    try
                        P2PNetwork.tcpSendPing ce.TcpClient
                        true
                    with
                    | _ ->
                        ce.TcpClient.Dispose()
                        false
                )
            { model with ConnectedEndpoints = successfullyPingedEndpoints }, Cmd.none
        | AppendLocalMessage m ->
            { model with MessagesList = model.MessagesList @ [m] }, Cmd.none
        | TextChanged t ->
            { model with MessageInput = t }, Cmd.none

    let view model dispatch =
        Grid.create [
            Grid.columnDefinitions "10, 120, 5, 6*, 5, Auto, 10"
            Grid.rowDefinitions "10, *, 5, Auto, 10"
            Grid.children [
                ScrollViewer.create [
                    ScrollViewer.column 1
                    ScrollViewer.row 1
                    ScrollViewer.rowSpan 3
                    ScrollViewer.content (
                        StackPanel.create [
                            StackPanel.spacing 10
                            StackPanel.orientation Orientation.Vertical
                            StackPanel.children (
                                TextBlock.create [ TextBlock.text "В сети: " ]
                                :: (model.ConnectedEndpoints
                                    |> List.map (fun connection ->
                                        TextBlock.create [
                                            TextBlock.fontSize 12
                                            TextBlock.text <| connection.MachineName
                                        ])
                                )
                            )
                        ]
                    )
                ]
                
                Border.create [
                    Border.column 3
                    Border.columnSpan 3
                    Border.row 1
                    Border.borderThickness 2
                    Border.cornerRadius 2
                    Border.child (
                        ScrollViewer.create [
                            ScrollViewer.content (
                                ItemsRepeater.create [
                                    ItemsRepeater.itemTemplate (
                                        let dt m =
                                            Border.create [
                                                Border.borderThickness 2
                                                Border.cornerRadius 10
                                                if m.IsMe
                                                then Border.background "#9CF4FF"
                                                else Border.background "#A9FFDD"
                                                Border.child (
                                                    StackPanel.create [
                                                        StackPanel.children [
                                                            TextBlock.create [
                                                                TextBlock.background "Transparent"
                                                                TextBlock.focusable false
                                                                TextBlock.textWrapping TextWrapping.Wrap
                                                                TextBlock.fontSize 10
                                                                TextBlock.margin 8
                                                                TextBlock.fontStyle FontStyle.Italic
                                                                TextBlock.verticalAlignment VerticalAlignment.Center
                                                                if m.IsMe
                                                                then TextBlock.textAlignment TextAlignment.Right
                                                                TextBlock.text $"{m.Message.Sender}"
                                                            ]
                                                            TextBox.create [
                                                                TextBox.background "Transparent"
                                                                TextBox.borderThickness 0
                                                                TextBox.focusable false
                                                                TextBox.selectionBrush Brushes.LightBlue
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
                    TextBox.column 3
                    TextBox.row 3
                    TextBox.watermark "Введите сообщение..."
                    TextBox.acceptsReturn true
                    TextBox.textWrapping TextWrapping.Wrap
                    TextBox.text model.MessageInput
                    TextBox.maxHeight 200
                    TextBox.onKeyDown (fun o -> if o.Key = Key.Enter && o.KeyModifiers = KeyModifiers.None then dispatch SendMessage; o.Handled <- true)
                    TextBox.onTextChanged(fun text -> dispatch <| TextChanged text)
                ]
                Button.create [
                    Button.column 5
                    Button.row 3
                    Button.width 64
                    Button.verticalAlignment VerticalAlignment.Bottom
                    Button.horizontalAlignment HorizontalAlignment.Center
                    Button.horizontalContentAlignment HorizontalAlignment.Center
                    Button.content ">"
                    Button.isEnabled (model.ConnectedEndpoints.Length > 0)
                    Button.onClick (fun _ -> dispatch SendMessage)
                ]
            ]
        ]