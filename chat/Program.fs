namespace chat

open System.Net
open System.Text
open System.Text.Json
open Avalonia
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.Themes.Fluent
open Avalonia.FuncUI.Hosts
open Elmish
open Avalonia.FuncUI.Elmish
open chat.Message

type MainWindow() as this =
    inherit HostWindow()
    do
        base.Title <- "SSC"
        base.Width <- 400.0
        base.Height <- 400.0

        //this.VisualRoot.VisualRoot.Renderer.DrawFps <- true
        //this.VisualRoot.VisualRoot.Renderer.DrawDirtyRects <- true
        
        let p2p model =
            let sub dispatch =
                let invoke buf read =
                    let json = Encoding.UTF8.GetString(buf, 0, read)
                    let msg = JsonSerializer.Deserialize<Message>(json)
                    Chat.Msg.P2pMessageReceived msg |> dispatch
                    ()
                
                let listener = P2PNetwork.listener IPAddress.Loopback 5002
                P2PNetwork.listenForPackage listener invoke |> Async.Start
            Cmd.ofSub sub
        
        Program.mkProgram (fun () -> Chat.init) Chat.update Chat.view
        |> Program.withHost this
        |> Program.withSubscription p2p
        |> Program.withConsoleTrace
        |> Program.run
        
type App() =
    inherit Application()

    override this.Initialize() = 
        this.Styles.Add (FluentTheme(baseUri = null, Mode = FluentThemeMode.Light))

    override this.OnFrameworkInitializationCompleted() =
        match this.ApplicationLifetime with
        | :? IClassicDesktopStyleApplicationLifetime as desktopLifetime ->
            desktopLifetime.MainWindow <- MainWindow()
        | _ -> ()

module Program =

    [<EntryPoint>]
    let main(args: string[]) =
        AppBuilder
            .Configure<App>()
            .UsePlatformDetect()
            .UseSkia()
            .StartWithClassicDesktopLifetime(args)