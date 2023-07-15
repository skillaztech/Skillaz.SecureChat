module Skillaz.SecureChat.AcceptanceTests.ArgsBuilder

open System
open Avalonia.Controls.ApplicationLifetimes
open Skillaz.SecureChat.AcceptanceTests.TestConfigStorage
open Skillaz.SecureChat.ChatArgs
open Skillaz.SecureChat.Domain.Domain
open Skillaz.SecureChat.IConfigStorage
open Skillaz.SecureChat.INetworkProvider
open Skillaz.SecureChat.P2P

let mkArgs =
    let userSettings = {
        UserId = Guid.NewGuid().ToString()
        Name = Guid.NewGuid().ToString()
        SecretCode = Random.Shared.Next(100000, 999999)
    }
    
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
    UnixSocketsFolderPath = "./tests"
    UnixSocketFilePath = "test.socket"
    NetworkProvider = {
                new INetworkProvider with
                    member this.RemoteListener = {
                        new INetworkListener with
                            member this.Socket = DefaultSockets.remoteListener
                            member this.IsBound = DefaultSockets.remoteListener.IsBound
                            member this.StartListen() = DefaultSockets.remoteListener.Listen()
                            member this.Bind() = UnixSocket.tryBindTo "./tests/remote.socket" DefaultSockets.remoteListener
                            member this.GenerateClient _ = UnixSocket.client
                            member this.Connect _ socket = UnixSocket.connectSocket socket "./tests/remote.socket"}
                    member this.LocalListener = {
                        new INetworkListener with
                            member this.Socket = DefaultSockets.localListener
                            member this.IsBound = DefaultSockets.localListener.IsBound
                            member this.StartListen() = DefaultSockets.localListener.Listen()
                            member this.Bind() = UnixSocket.tryBindTo "./tests/local.socket" DefaultSockets.localListener
                            member this.GenerateClient _ = UnixSocket.client
                            member this.Connect _ socket = UnixSocket.connectSocket socket "./tests/local.socket"
                    }
    } 
}