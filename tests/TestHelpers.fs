module Skillaz.SecureChat.AcceptanceTests.TestHelpers

open System
open System.Net.Sockets
open Avalonia.Controls.ApplicationLifetimes
open Skillaz.SecureChat.Chat
open Skillaz.SecureChat.ChatArgs
open Skillaz.SecureChat.IConfigStorage

let emptyArgs = {
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

let emptyModel = {
    Args = emptyArgs
    KnownPeers = []
    ListenerPort = 0
    ClientPort = 0
    SecretCode = 0
    UserName = ""
    UserNameValidationErrors = []
    UserId = ""
    MaxChatMessageLength = 0
    TcpListener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) // TODO: Mock
    UnixSocketListener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) // TODO: Mock
    UnixSocketFolder = ""
    UnixSocketFilePath = ""
    Connections = []
    ConnectedUsers = []
    MessageInput = ""
    MessagesList = []
    MessagesListHashSet = Set.empty
    SettingsVisible = false
    ChatScrollViewOffset = 0
}