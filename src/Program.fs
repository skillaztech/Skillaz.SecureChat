namespace Skillaz.SecureChat

open System
open System.IO
open System.Reflection
open Avalonia.Logging
open Elmish
open Avalonia
open Avalonia.Controls
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.FuncUI
open Avalonia.Themes.Fluent
open Avalonia.FuncUI.Hosts
open Avalonia.FuncUI.Elmish
open Skillaz.SecureChat.ChatArgs
open Skillaz.SecureChat.IO.IOsDetector

type MainWindow(lifeTime:IControlledApplicationLifetime) as this =
    inherit HostWindow()
    do
        let assembly = Assembly.GetExecutingAssembly()
        let version = assembly.GetName().Version
        let versionStr = $"v{version.Major}.{version.Minor}.{version.Build}"
        let currentProcessDirectory = Path.GetDirectoryName(assembly.Location)
        
        base.Title <- $"Skillaz Secure Chat {versionStr}"
        base.Width <- 800.0
        base.Height <- 400.0
        base.MinWidth <- 800.0
        base.MinHeight <- 400.0
        base.Icon <- WindowIcon(Path.Join(currentProcessDirectory, "logo.ico"))

        //this.VisualRoot.VisualRoot.Renderer.DrawFps <- true
        //this.VisualRoot.VisualRoot.Renderer.DrawDirtyRects <- true
        
        let args = {
            ApplicationLifetime = lifeTime
            Version = versionStr
            ProcessDirectory = currentProcessDirectory
            OsDetector = {
                new IOsDetector with
                    member this.IsLinux() = OperatingSystem.IsLinux()
                    member this.IsMacOS() = OperatingSystem.IsMacOS()
            }
        }
        
        Program.mkProgram (fun () -> Chat.init args) Chat.update Chat.view
        |> Program.withHost this
        |> Program.withErrorHandler (fun (msg, e) -> Logger.nlogger.FatalException e $"{msg}")
        |> Program.run
        
type App() =
    inherit Application()

    override this.Initialize() =
        this.Styles.Add (FluentTheme(baseUri = null, Mode = FluentThemeMode.Light))
        this.Styles.Load "avares://Skillaz.SecureChat/Styles.xaml"

    override this.OnFrameworkInitializationCompleted() =
        match this.ApplicationLifetime with
        | :? IClassicDesktopStyleApplicationLifetime as desktopLifetime ->
            desktopLifetime.MainWindow <- MainWindow(desktopLifetime)
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