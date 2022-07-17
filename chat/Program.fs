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
        
        Program.mkProgram (fun () -> Chat.init) Chat.update Chat.view
        |> Program.withHost this
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