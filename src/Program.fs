﻿namespace Skillaz.SecureChat

open System
open System.IO
open System.Net
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
open NLog
open Skillaz.SecureChat.ChatArgs
open Skillaz.SecureChat.Domain.Domain
open Skillaz.SecureChat.IConfigStorage

type MainWindow(lifeTime:IControlledApplicationLifetime) as this =
    inherit HostWindow()
    do
        let logger = Logger.nlogger
        
        let assembly = Assembly.GetExecutingAssembly()
        let version = assembly.GetName().Version
        let versionStr = $"v{version.Major}.{version.Minor}.{version.Build}"
        let currentProcessDirectory = Path.GetDirectoryName(assembly.Location)
        
        logger.Info $"[MainWindow] Start app into {currentProcessDirectory}"
        logger.Info $"[init] Version: {versionStr}"
        
        base.Title <- $"Skillaz Secure Chat {versionStr}"
        base.Width <- 800.0
        base.Height <- 400.0
        base.MinWidth <- 800.0
        base.MinHeight <- 400.0
        base.Icon <- WindowIcon(Path.Join(currentProcessDirectory, "logo.ico"))

        //this.VisualRoot.VisualRoot.Renderer.DrawFps <- true
        //this.VisualRoot.VisualRoot.Renderer.DrawDirtyRects <- true
        
        let userSettingsFilePath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "/ssc/", "usersettings.yaml")
                
        let userSettings =
            let userSettings = FileConfiguration.FileUserSettings()
            if not <| File.Exists(userSettingsFilePath)
            then
                logger.Info $"[MainWindow] User settings file does not exists in path {userSettingsFilePath}, creating..."
                
                Directory.CreateDirectory(Path.GetDirectoryName(userSettingsFilePath)) |> ignore
                
                userSettings.UserId <- Guid.NewGuid()
                userSettings.Name <- Environment.UserName
                userSettings.SecretCode <- Random.Shared.Next(100000, 999999)
                userSettings.LogLevel <- "Info"
                
                userSettings.Save(userSettingsFilePath)

            logger.Info $"[MainWindow] Loading user settings from {userSettingsFilePath}..."

            try
                userSettings.Load(userSettingsFilePath)
            with
            | e ->
                logger.FatalException e $"[MainWindow] User settings loading from {userSettingsFilePath} failed with an error. Exiting..."
                reraise()

            logger.Info $"[MainWindow] User settings loaded from path {userSettingsFilePath}. "

            if userSettings.Name = String.Empty then userSettings.Name <- Environment.UserName
            if userSettings.SecretCode = 0 then userSettings.SecretCode <- Random.Shared.Next(100000, 999999)
            if userSettings.UserId = Guid.Empty then userSettings.UserId <- Guid.NewGuid()

            let logLevelFromUserSettings =
                match userSettings.LogLevel with
                | "Trace" -> LogLevel.Trace
                | "Debug" -> LogLevel.Debug
                | "Info" -> LogLevel.Info
                | "Warn" -> LogLevel.Warn
                | "Error" -> LogLevel.Error
                | "Fatal" -> LogLevel.Fatal
                | _ -> LogLevel.Info

            LogManager.Configuration.LoggingRules
            |> Seq.iter (fun o -> o.SetLoggingLevels(logLevelFromUserSettings, LogLevel.Fatal))
            LogManager.ReconfigExistingLoggers()

            logger.Info $"[MainWindow] Log level from user settings enabled {logLevelFromUserSettings}"
            
            logger.Info $"[MainWindow] Loaded user settings {userSettings}"

            {
                UserId = userSettings.UserId.ToString()
                Name = userSettings.Name
                SecretCode = userSettings.SecretCode
            }
            
        let appSettings =
            let appSettings = FileConfiguration.FileAppSettings()
            let appSettingsFilePath = Path.Join(currentProcessDirectory, "appsettings.yaml")
            if File.Exists(appSettingsFilePath)
            then
                try
                    logger.Info $"[MainWindow] Loading application settings from {appSettingsFilePath}"
                    appSettings.Load(appSettingsFilePath)
                with
                | e ->
                    logger.FatalException e "[MainWindow] Application settings loading failed with an exception. Loading defaults."

            if appSettings.MaxChatMessageLength = 0 then appSettings.MaxChatMessageLength <- 3000
            if appSettings.ClientTcpPort = 0 then appSettings.ClientTcpPort <- 63211
            if appSettings.ListenerTcpPort = 0 then appSettings.ListenerTcpPort <- 63211
            
            logger.Info $"[MainWindow] Loaded application settings: {appSettings}"

            {
                MaxChatMessageLength = appSettings.MaxChatMessageLength
                ListenerTcpPort = appSettings.ListenerTcpPort
                ClientTcpPort = appSettings.ClientTcpPort
                KnownRemotePeers = appSettings.KnownRemotePeers |> Seq.map IPEndPoint.Parse |> List.ofSeq
            }
            
        let unixSocketsFolderPath =
            let path =
                if OperatingSystem.IsLinux() || OperatingSystem.IsMacOS()
                then "/tmp/ssc/"
                else Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "/ssc/")
                
            logger.Debug $"[MainWindow] Directory for unix sockets chosen as {path}"
            
            path
            
        let unixSocketFileName = $"{Environment.UserName}-{userSettings.UserId}.socket"
        let unixSocketFilePath = Path.Join(unixSocketsFolderPath, unixSocketFileName)
            
        logger.Info $"[MainWindow] Unix socket file path for current user selected as {unixSocketFilePath}"
        
        let args = {
            ApplicationLifetime = lifeTime
            ProcessDirectory = currentProcessDirectory
            AppSettings = appSettings
            UserSettings = userSettings
            UnixSocketsFolderPath = unixSocketsFolderPath
            UnixSocketFilePath = unixSocketFilePath
            ConfigStorage = {
                new IConfigStorage with
                    member this.SaveUserSettings userSettings =
                        let us = FileConfiguration.FileUserSettings()
                        us.Load(userSettingsFilePath)
                        us.Name <- userSettings.Name
                        us.SecretCode <- userSettings.SecretCode
                        us.Save()
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