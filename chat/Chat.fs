namespace chat

open System
open System.Net
open System.Net.Sockets
open System.Text
open System.Text.Json
open Elmish
open Avalonia.FuncUI
open Avalonia.Media
open Avalonia.Controls
open Avalonia.FuncUI.DSL
open Avalonia.Layout
open chat.Message

module Chat =
    
    type Model = {
        CurrentUser: string
        MessageInput: string
        MessagesList: Message list
        TcpEndpoint: IPEndPoint option
    }
        
    type Msg =
        | UdpSendPackage of UdpClient * int
        | UdpPackageReceived of byte[] * IPEndPoint
        | MessageReceived of Message
        | TextChanged of string
        | SendMessage
        
    let udpSubscription listener =
        fun dispatch ->
            let invoke (payload:byte[]) endpoint =
                Msg.UdpPackageReceived (payload, endpoint) |> dispatch |> ignore
            P2PNetwork.listenForUdpPackage listener invoke |> Async.Start
        
    let tcpSubscription (ip:IPEndPoint) =
        fun dispatch ->
            let invoke buf read =
                let json = Encoding.UTF8.GetString(buf, 0, read)
                let msg = JsonSerializer.Deserialize<Message>(json)
                Msg.MessageReceived msg |> dispatch |> ignore
            
            let listener = P2PNetwork.tcpListener ip.Address ip.Port
            P2PNetwork.listenForTcpPackage listener invoke |> Async.Start
    
    let init =
        let model = {
            TcpEndpoint = None
            CurrentUser = "Me"
            MessageInput = ""
            MessagesList = [
                { Sender = "Me"; Message = "Hello"; DateTime = DateTime() }
                { Sender = "Hime"; Message = "There\nasdasdad\nasdasdasd\nasdasdads"; DateTime = DateTime() }
                { Sender = "Hime"; Message = "There\nasdasdad\nasdasdasd\nasdasdads"; DateTime = DateTime() }
                { Sender = "Hime"; Message = "There\nasdasdad\nasdasdasd\nasdasdads"; DateTime = DateTime() }
                { Sender = "Hime"; Message = "There\nasdasdad\nasdasdasd\nasdasdads"; DateTime = DateTime() }
                { Sender = "Hime"; Message = "There\nasdasdad\nasdasdasd\nasdasdads"; DateTime = DateTime() }
                { Sender = "Hime"; Message = "There\nasdasdad\nasdasdasd\nasdasdads"; DateTime = DateTime() }
                { Sender = "Hime"; Message = "There\nasdasdad\nasdasdasd\nasdasdads"; DateTime = DateTime() }
            ]
        }
        
        let port = 63211
        let udpClient = P2PNetwork.udpClient IPAddress.Any port
        
        let cmd = Cmd.batch [
            Cmd.ofSub <| udpSubscription udpClient
            Cmd.ofMsg <| UdpSendPackage (udpClient, port)
        ]
        
        model, cmd
    
    let update msg model =
        match msg with
        | UdpSendPackage (client, port) ->
            let payload = Encoding.UTF8.GetBytes("") // TODO: Inject secret
            client.Send(payload, payload.Length, IPEndPoint(IPAddress.Broadcast, port)) |> ignore
            model, Cmd.none
        | UdpPackageReceived (payload, ip) ->
            let payload = Encoding.UTF8.GetString(payload)
            if payload = "" // TODO: Parse secret
            then { model with TcpEndpoint = Some ip }, Cmd.ofSub <| tcpSubscription ip
            else model, Cmd.none
        | MessageReceived m ->
            { model with MessagesList = model.MessagesList @ [m] }, Cmd.none
        | SendMessage ->
            let newMsg = {
                Sender = model.CurrentUser
                DateTime = DateTime.Now
                Message = model.MessageInput
            }
            let ip = model.TcpEndpoint.Value
            use client = P2PNetwork.tcpClient ip.Address ip.Port
            P2PNetwork.tcpSendAsJson client newMsg
            { model with MessageInput = ""; MessagesList = model.MessagesList @ [newMsg] }, Cmd.none
        | TextChanged t ->
            { model with MessageInput = t }, Cmd.none

    let view model dispatch =
        Grid.create [
            Grid.columnDefinitions "10, *, 5, Auto, 10"
            Grid.rowDefinitions "10, Auto, 5, *, 5, Auto, 10"
            Grid.children [
                Border.create [
                    Border.column 1
                    Border.columnSpan 3
                    Border.row 3
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
                                                if m.Sender = model.CurrentUser
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
                                                                if m.Sender = model.CurrentUser
                                                                then TextBlock.textAlignment TextAlignment.Right
                                                                TextBlock.text $"{m.Sender}"
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
                                                                if m.Sender = model.CurrentUser
                                                                then TextBox.textAlignment TextAlignment.Right
                                                                TextBox.text $"{m.Message}"
                                                            ]
                                                        ]
                                                    ]
                                                )
                                            ]
                                            
                                        DataTemplateView<Message>.create dt
                                    )
                                    
                                    ItemsRepeater.dataItems model.MessagesList
                                ]
                            )
                        ]
                    )
                ]
                
                TextBox.create [
                    TextBox.column 1
                    TextBox.row 5
                    TextBox.watermark "Введите сообщение..."
                    TextBox.acceptsReturn true
                    TextBox.textWrapping TextWrapping.Wrap
                    TextBox.text model.MessageInput
                    TextBox.maxHeight 200
                    
                    TextBox.onTextChanged(fun text -> dispatch <| TextChanged text)
                ]
                Button.create [
                    Button.column 3
                    Button.row 5
                    Button.width 64
                    Button.verticalAlignment VerticalAlignment.Bottom
                    Button.horizontalAlignment HorizontalAlignment.Center
                    Button.horizontalContentAlignment HorizontalAlignment.Center
                    Button.content ">"
                    Button.isEnabled model.TcpEndpoint.IsSome
                    Button.onClick (fun _ -> dispatch SendMessage)
                ]
            ]
        ]