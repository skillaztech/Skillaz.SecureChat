namespace Skillaz.SecureChat

open System

module Message =
    type ChatMessage = {
        Sender: string
        MessageText: string
        DateTime: DateTime
    }

