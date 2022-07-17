namespace chat

open System

module Message =
    type Message = {
        Sender: string
        MessageText: string
        DateTime: DateTime
    }

