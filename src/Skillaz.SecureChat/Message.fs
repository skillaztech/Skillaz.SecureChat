namespace Skillaz.SecureChat

open System

module Message =
    type RetranslationInfo = {
        RetranslatedBy: string list
    }
    
    type AliveMessage = {
        MessageSender: string
        SecretCode: int
        AppMark: string
        RetranslationInfo: RetranslationInfo
    }
    
    type ChatMessage = {
        MessageText: string
        MessageSender: string
        DateTime: DateTime
        SecretCode: int
        AppMark: string
        RetranslationInfo: RetranslationInfo
    }

