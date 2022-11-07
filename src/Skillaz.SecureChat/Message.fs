namespace Skillaz.SecureChat

open System

module Message =
    type HelloMessage = {
        MachineName: string
        SecretCode: int
        AppMark: string
    }
    type ChatMessage = {
        Sender: string
        MessageText: string
        DateTime: DateTime
        AppMark: string
    }

