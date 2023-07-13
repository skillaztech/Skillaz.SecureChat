module Skillaz.SecureChat.AcceptanceTests.ArgsBuilder

open System
open Avalonia.Controls.ApplicationLifetimes
open Skillaz.SecureChat.ChatArgs
open Skillaz.SecureChat.IConfigStorage
open Skillaz.SecureChat.INetworkProvider
open Skillaz.SecureChat.P2P

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
    NetworkProvider = {
                new INetworkProvider with
                    member this.RemoteListener = {
                        new INetworkListener with
                            member this.Socket = DefaultSockets.remoteListener
                            member this.IsBound = DefaultSockets.remoteListener.IsBound
                            member this.StartListen() = DefaultSockets.remoteListener.Listen()
                            member this.Bind() = UnixSocket.tryBindTo "./tests/test.socket" DefaultSockets.remoteListener
                    }
                    member this.RemoteClientGenerateOnPort port = Tcp.client port
                    member this.RemoteClientConnect address port socket = Tcp.connectSocket address port socket
                    member this.LocalListener = {
                        new INetworkListener with
                            member this.Socket = DefaultSockets.localListener
                            member this.IsBound = DefaultSockets.localListener.IsBound
                            member this.StartListen() = DefaultSockets.localListener.Listen()
                            member this.Bind() = UnixSocket.tryBindTo "./tests/test.socket" DefaultSockets.localListener
                    }
                    member this.LocalClientGenerate() = UnixSocket.client
                    member this.LocalClientConnect socket socketFile = UnixSocket.connectSocket socket socketFile
            } 
}