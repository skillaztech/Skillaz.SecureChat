namespace Skillaz.SecureChat

open System

module Message =
    type AliveMessage = {
        MessageSender: string
        SecretCode: int
        AppMark: string
    }
    type ChatMessage = {
        MessageText: string
        MessageSender: string
        DateTime: DateTime
        SecretCode: int
        AppMark: string
    }

