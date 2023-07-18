module Skillaz.SecureChat.AcceptanceTests.ArgsBuilder

open System
open System.IO
open Avalonia.Controls.ApplicationLifetimes
open Skillaz.SecureChat.AcceptanceTests.TestConfigStorage
open Skillaz.SecureChat.ChatArgs
open Skillaz.SecureChat.Domain.Domain
open Skillaz.SecureChat.INetworkProvider
open Skillaz.SecureChat.P2P

let mkArgs =
    let userSettings = {
        UserId = Guid.NewGuid().ToString()
        Name = Guid.NewGuid().ToString()
        SecretCode = Random.Shared.Next(100000, 999999)
    }
    
    let localSocketsFolder = "./tests-sockets/local"
    let remoteSocketsFolder = "./tests-sockets/remote"
    Directory.CreateDirectory(localSocketsFolder) |> ignore
    Directory.CreateDirectory(remoteSocketsFolder) |> ignore
    
    {
    ApplicationLifetime = {
        new IControlledApplicationLifetime with
        member _.add_Startup(value) = ()
        member _.remove_Startup(value) = ()
        member _.add_Exit(value) = ()
        member _.remove_Exit(value) = ()
        member _.Shutdown(exitCode) = ()
    }
    ProcessDirectory = ""
    ConfigStorage = TestConfigStorage(userSettings)
    AppSettings = {
        MaxChatMessageLength = 3000
        ListenerTcpPort = 20392
        ClientTcpPort = 20392
        KnownRemotePeers = []
        LogLevel = "Fatal"
    }
    UserSettings = userSettings
    UnixSocketsFolderPath = localSocketsFolder
    UnixSocketFilePath = "local.socket"
    NetworkProvider = {
                new INetworkProvider with
                    member this.RemoteListener = {
                        new INetworkListener with
                            member this.Socket = DefaultSockets.remoteListener
                            member this.IsBound = DefaultSockets.remoteListener.IsBound
                            member this.StartListen() = DefaultSockets.remoteListener.Listen()
                            member this.Bind() = UnixSocket.tryBindTo $"{remoteSocketsFolder}/remote.socket" DefaultSockets.remoteListener
                            member this.GenerateClient _ = UnixSocket.client
                            member this.Connect _ socket = UnixSocket.connectSocket socket $"{remoteSocketsFolder}/remote/remote.socket"}
                    member this.LocalListener = {
                        new INetworkListener with
                            member this.Socket = DefaultSockets.localListener
                            member this.IsBound = DefaultSockets.localListener.IsBound
                            member this.StartListen() = DefaultSockets.localListener.Listen()
                            member this.Bind() = UnixSocket.tryBindTo $"{localSocketsFolder}/local.socket" DefaultSockets.localListener
                            member this.GenerateClient _ = UnixSocket.client
                            member this.Connect _ socket = UnixSocket.connectSocket socket $"{localSocketsFolder}/local.socket"
                    }
    } 
}