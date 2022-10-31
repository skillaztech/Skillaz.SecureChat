namespace Skillaz.SecureChat

open System

module Message =
    type HelloMessage = {
        MachineName: string
        SecretCode: int
        TcpMark: Guid
    }
    type ChatMessage = {
        Sender: string
        MessageText: string
        DateTime: DateTime
    }

