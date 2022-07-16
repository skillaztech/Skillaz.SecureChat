namespace chat

open System

module Message =
    type Message = {
        Sender: string
        Message: string
        DateTime: DateTime
    }

