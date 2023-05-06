module Skillaz.SecureChat.AcceptanceTests.ArgsBuilder

open System
open Avalonia.Controls.ApplicationLifetimes
open Skillaz.SecureChat.ChatArgs
open Skillaz.SecureChat.IConfigStorage

let mkArgs = {
    ApplicationLifetime = {
        new IControlledApplicationLifetime with
        member _.add_Startup(value) = ()
        member _.remove_Startup(value) = ()
        member _.add_Exit(value) = ()
        member _.remove_Exit(value) = ()
        member _.Shutdown(exitCode) = ()
    }
    ProcessDirectory = ""
    ConfigStorage = {
        new IConfigStorage with
        member _.SaveUserSettings(userSettings) = ()
    }
    AppSettings = {
        MaxChatMessageLength = 3000
        ListenerTcpPort = 20392
        ClientTcpPort = 20392
        KnownRemotePeers = []
        LogLevel = "Fatal"
    }
    UserSettings = {
        UserId = Guid.NewGuid().ToString()
        Name = Guid.NewGuid().ToString()
        SecretCode = Random.Shared.Next(100000, 999999)
    }
    UnixSocketsFolderPath = "./tests"
    UnixSocketFilePath = "test.socket"
}