namespace Skillaz.SecureChat

open System

module Message =
    type AliveMessage = {
        MachineName: string
        SecretCode: int
        AppMark: string
    }
    type ChatMessage = {
        MessageText: string
        DateTime: DateTime
        SecretCode: int
        AppMark: string
    }

