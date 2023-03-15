module Skillaz.SecureChat.AcceptanceTests.TestHelpers

open System.Net.Sockets
open Skillaz.SecureChat.Chat
open Skillaz.SecureChat.ChatArgs
open Skillaz.SecureChat.IO.IOsDetector

let emptyArgs =
    {
        Version = "v1.0.0"
        ProcessDirectory = ""
        OsDetector = {
            new IOsDetector with
            member _.IsLinux() = false
            member _.IsMacOS() = false
        }
    }

let emptyModel =
    {
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
            AppSettingsFilePath = ""
            UserSettingsFilePath = ""
            ChatScrollViewOffset = 0
        }