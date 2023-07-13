module Skillaz.SecureChat.AcceptanceTests.ModelBuilder

open System.Net.Sockets
open Skillaz.SecureChat.Chat

let mkModel args = {
    Args = args
    KnownPeers = []
    ListenerPort = 0
    ClientPort = 0
    SecretCode = 0
    UserName = ""
    UserNameValidationErrors = []
    UserId = ""
    MaxChatMessageLength = 0
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