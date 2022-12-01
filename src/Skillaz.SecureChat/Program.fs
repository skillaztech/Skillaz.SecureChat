﻿namespace Skillaz.SecureChat

open System
open System.IO
open Avalonia.Logging
open Elmish
open Avalonia
open Avalonia.Controls
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.FuncUI
open Avalonia.Themes.Fluent
open Avalonia.FuncUI.Hosts
open Avalonia.FuncUI.Elmish

type MainWindow() as this =
    inherit HostWindow()
    do
        let currentProcessDirectory = Path.GetDirectoryName(Environment.ProcessPath)
        
        base.Title <- "Skillaz Secure Chat"
        base.Width <- 800.0
        base.Height <- 400.0
        base.MinWidth <- 800.0
        base.MinHeight <- 400.0
        base.Icon <- WindowIcon(Path.Join(currentProcessDirectory, "logo.ico"))

        //this.VisualRoot.VisualRoot.Renderer.DrawFps <- true
        //this.VisualRoot.VisualRoot.Renderer.DrawDirtyRects <- true
        
        let appSettings = AppSettings.load (Path.Join(currentProcessDirectory, "appsettings.json"))
        
        Program.mkProgram (fun () -> Chat.init appSettings) Chat.update Chat.view
        |> Program.withHost this
        |> Program.run
        
type App() =
    inherit Application()

    override this.Initialize() =
        this.Styles.Add (FluentTheme(baseUri = null, Mode = FluentThemeMode.Light))
        this.Styles.Load "avares://Skillaz.SecureChat/Styles.xaml"

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
            .LogToTrace(LogEventLevel.Warning)
            .UseSkia()
            .StartWithClassicDesktopLifetime(args)