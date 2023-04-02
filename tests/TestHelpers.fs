module Skillaz.SecureChat.AcceptanceTests.TestHelpers

open System.Net.Sockets
open Avalonia.Controls.ApplicationLifetimes
open Skillaz.SecureChat.Chat
open Skillaz.SecureChat.ChatArgs

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
    ConfigStorage = failwith "todo"
    AppSettings = failwith "todo"
    UserSettings = failwith "todo"
    UnixSocketsFolderPath = failwith "todo"
    UnixSocketsFileName = failwith "todo"
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